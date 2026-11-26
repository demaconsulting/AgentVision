// Copyright (c) DEMA Consulting
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using DemaConsulting.AgentVision.Core.Cameras;
using DemaConsulting.AgentVision.Core.Capture;
using DemaConsulting.AgentVision.Core.Imaging;
using DemaConsulting.AgentVision.Core.Tests.TestSupport;
using DemaConsulting.CanvasNet.Codecs;

namespace DemaConsulting.AgentVision.Core.Tests.Capture;

/// <summary>
///     Subsystem tests for the Capture subsystem: the selector, the opener, the session, and the snapper working
///     together with a fake camera source and the Imaging subsystem for the frames they decode.
/// </summary>
public class CaptureSubsystemTests
{
    /// <summary>
    ///     The longest a test waits for something that should happen at once.
    /// </summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Test that a snapshot returns the newest frame of a hot camera without waiting.
    /// </summary>
    [Fact]
    public void CaptureSubsystem_HotFrame_SnapshotAfterEmit_ReturnsNewestFrame()
    {
        // Arrange: a camera that is open, and two frames delivered after it
        var source = TestCameras.Source();
        using var session = new CameraOpener(source).Open("0", TestCameras.QuickOptions());
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 40, 1));
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 220, 2));

        // Act: take a snapshot and decode it
        var frame = session.Snapshot();
        using var image = frame.ToImage();

        // Assert: the frame is the newest one
        Assert.Equal(2, frame.Sequence);
        Assert.Equal((64, 48), (image.Width, image.Height));
    }

    /// <summary>
    ///     Test that an index, an identifier, and a name open the same camera.
    /// </summary>
    [Fact]
    public void CaptureSubsystem_Selection_IndexIdName_ReachDeviceOfSameCamera()
    {
        // Arrange: a source with two cameras, listed in an order that differs from their position order
        var source = new FakeCameraSource([TestCameras.Camera("Fake:b", "Second"), TestCameras.Camera("Fake:a", "First")]);
        var opener = new CameraOpener(source);

        // Act: open the camera that sorts first in each of the three ways
        using var byIndex = opener.Open("0", TestCameras.QuickOptions());
        using var byId = opener.Open("Fake:a", TestCameras.QuickOptions());
        using var byName = opener.Open("first", TestCameras.QuickOptions());

        // Assert: all three devices belong to the same camera
        Assert.All(source.Devices, device => Assert.Equal("Fake:a", device.Camera.Id));
        Assert.Equal(3, source.Devices.Count);
    }

    /// <summary>
    ///     Test that a requested format and the default are passed to the source.
    /// </summary>
    [Fact]
    public void CaptureSubsystem_FormatSelection_RequestAndDefault_PassChosenFormatToSource()
    {
        // Arrange: a camera with full HD, HD, and VGA formats
        var camera = new CameraInfo(
            "Fake:0",
            "Cam",
            "Cam",
            "Fake",
            [
                new CameraFormat(1920, 1080, "JPEG", 30),
                new CameraFormat(1280, 720, "JPEG", 30),
                new CameraFormat(640, 480, "JPEG", 30)
            ]);
        var source = new FakeCameraSource([camera]);
        var opener = new CameraOpener(source);

        // Act: open once with a request for HD and once without a request
        using var requested = opener.Open("0", new CameraOpenOptions { Width = 1280, Height = 720, WarmUp = TimeSpan.Zero });
        using var defaulted = opener.Open("0", new CameraOpenOptions { WarmUp = TimeSpan.Zero });

        // Assert: the request got HD, the default got full HD
        Assert.Equal(new CameraFormat(1280, 720, "JPEG", 30), source.Devices[0].Format);
        Assert.Equal(new CameraFormat(1920, 1080, "JPEG", 30), source.Devices[1].Format);
    }

    /// <summary>
    ///     Test that frames that arrive during the warm-up are not served.
    /// </summary>
    [Fact]
    public async Task CaptureSubsystem_WarmUp_FramesBeforeWarmUp_AreNotServed()
    {
        // Arrange: a camera with a 400 ms warm-up, and a dark frame delivered at once
        var source = TestCameras.Source();
        var options = new CameraOpenOptions { WarmUp = TimeSpan.FromMilliseconds(400), FirstFrameTimeout = TimeSpan.FromSeconds(10) };
        using var session = new CameraOpener(source).Open("0", options);
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 5, 1));

        // Act: let the warm-up pass, deliver a bright frame, and take a snapshot
        await Task.Delay(600, TestContext.Current.CancellationToken);
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 200, 2));
        var frame = session.Snapshot();

        // Assert: the bright frame is served, never the dark one
        Assert.Equal(2, frame.Sequence);
    }

    /// <summary>
    ///     Test that two cameras can be open at once and that closing one leaves the other streaming.
    /// </summary>
    [Fact]
    public void CaptureSubsystem_Independence_TwoSessions_CloseOneKeepsOther()
    {
        // Arrange: two cameras, both open, with a frame each
        var source = new FakeCameraSource([TestCameras.Camera("Fake:0", "One"), TestCameras.Camera("Fake:1", "Two")]);
        var opener = new CameraOpener(source);
        var one = opener.Open("0", TestCameras.QuickOptions());
        using var two = opener.Open("1", TestCameras.QuickOptions());
        source.Devices[0].Emit(SyntheticFrames.OpaqueFrame(1));
        source.Devices[1].Emit(SyntheticFrames.OpaqueFrame(2));
        Assert.Equal(1, one.Snapshot().Sequence);

        // Act: close the first camera, then deliver another frame to the second
        one.Dispose();
        source.Devices[1].Emit(SyntheticFrames.OpaqueFrame(3));

        // Assert: the first device is stopped and the second keeps streaming
        Assert.True(source.Devices[0].IsDisposed);
        Assert.False(source.Devices[1].IsDisposed);
        Assert.Equal(3, two.Snapshot().Sequence);
        Assert.Throws<ObjectDisposedException>(one.Snapshot);
    }

    /// <summary>
    ///     Test that snapshots from many threads succeed while the camera keeps delivering frames.
    /// </summary>
    [Fact]
    public async Task CaptureSubsystem_ThreadSafety_ConcurrentSnapshotsWhileFramesArrive_AllSucceed()
    {
        // Arrange: a camera that streams real pictures by itself
        var source = TestCameras.StreamingSource();
        using var session = new CameraOpener(source).Open("0", TestCameras.QuickOptions());
        session.Snapshot();

        // Act: six threads take 100 snapshots and decode every tenth
        var workers = Enumerable.Range(0, 6)
            .Select(_ => Task.Run(
                () =>
                {
                    for (var count = 0; count < 100; count++)
                    {
                        var frame = session.Snapshot();
                        if (count % 10 == 0)
                        {
                            using var image = frame.ToImage();
                            Assert.Equal(64, image.Width);
                        }
                    }
                },
                TestContext.Current.CancellationToken))
            .ToArray();

        // Assert: all threads finish without a failure
        await Task.WhenAll(workers).WaitAsync(Patience, TestContext.Current.CancellationToken);
        Assert.All(workers, worker => Assert.True(worker.IsCompletedSuccessfully));
    }

    /// <summary>
    ///     Test that closing a session stops its device.
    /// </summary>
    [Fact]
    public void CaptureSubsystem_Close_DisposeStopsDevice()
    {
        // Arrange: an open session
        var source = TestCameras.Source();
        var session = new CameraOpener(source).Open("0", TestCameras.QuickOptions());
        Assert.False(source.LastDevice.IsDisposed);

        // Act: close it
        session.Dispose();

        // Assert: the device is stopped, and the session is closed
        Assert.True(source.LastDevice.IsDisposed);
        Assert.True(session.IsClosed);
    }

    /// <summary>
    ///     Test that every kind of failure is reported with its documented exception type.
    /// </summary>
    [Fact]
    public void CaptureSubsystem_Errors_EachFailure_ThrowsDocumentedException()
    {
        // Arrange: a source with one camera
        var source = TestCameras.Source();
        var opener = new CameraOpener(source);

        // Act / Assert: an unknown camera, a failing open, a silent camera, and a faulting camera each have their type
        Assert.Throws<CameraNotFoundException>(() => opener.Open("Nope", TestCameras.QuickOptions()));

        source.OpenFailure = new IOException("busy");
        Assert.Throws<CameraOpenException>(() => opener.Open("0", TestCameras.QuickOptions()));
        source.OpenFailure = null;

        using (var silent = opener.Open(
                   "0",
                   new CameraOpenOptions { WarmUp = TimeSpan.Zero, FirstFrameTimeout = TimeSpan.FromMilliseconds(100) }))
        {
            Assert.Throws<CameraTimeoutException>(silent.Snapshot);
        }

        using var faulting = opener.Open("0", TestCameras.QuickOptions());
        source.LastDevice.Fault(new IOException("unplugged"));
        Assert.Throws<CameraDisconnectedException>(faulting.Snapshot);
        Assert.IsAssignableFrom<CameraException>(Record.Exception(faulting.Snapshot));
    }

    /// <summary>
    ///     Test that the one-shot snap opens, saves, and closes the camera.
    /// </summary>
    [Fact]
    public void CaptureSubsystem_OneShot_Snap_OpensSavesAndCloses()
    {
        // Arrange: a streaming camera, a folder, and a request with a crop
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var path = folder.File("one-shot.png");
        var request = new SnapRequest("0", path) { Crop = new CropRectangle(0, 0, 32, 24), Options = TestCameras.QuickOptions() };

        // Act: snap
        var result = new CameraSnapper(source).Snap(request);

        // Assert: the camera was opened once and closed, and the file holds the cropped picture
        Assert.Single(source.Devices);
        Assert.True(source.LastDevice.IsDisposed);
        using var saved = PngCodec.Load(path);
        Assert.Equal((32, 24), (saved.Width, saved.Height));
        Assert.Equal((32, 24), (result.Width, result.Height));
    }
}
