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

using DemaConsulting.AgentVision.Core.Capture;
using DemaConsulting.AgentVision.Core.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Core.Tests.Capture;

/// <summary>
///     Unit tests for the CameraSession unit (and the CameraFrame it holds): holding the latest frame, the
///     warm-up, thread safety, closing, and device failures.
/// </summary>
/// <remarks>
///     The tests drive a fake device by hand. Waits use events and generous bounds; the only sleeps are those
///     that let a short warm-up or stale-frame limit pass, and no test depends on how fast the machine is.
/// </remarks>
public class CameraSessionTests
{
    /// <summary>
    ///     The longest a test waits for something that should happen at once, so that a failure is a failed
    ///     test and not a hung test run.
    /// </summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Test that a snapshot returns the most recent frame.
    /// </summary>
    [Fact]
    public void CameraSession_Snapshot_AfterFrames_ReturnsLatest()
    {
        // Arrange: a session without warm-up that has received three frames
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        foreach (var index in new long[] { 1, 2, 3 })
        {
            source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(index));
        }

        // Act: take a snapshot
        var frame = session.Snapshot();

        // Assert: it is the last frame, with its data and arrival information
        Assert.Equal(3, frame.Sequence);
        Assert.Equal([(byte)3], frame.EncodedData.ToArray());
        Assert.True(frame.Age >= TimeSpan.Zero);
        Assert.True(frame.CapturedAt <= DateTimeOffset.UtcNow);
        Assert.True(session.IsReady);
    }

    /// <summary>
    ///     Test that repeated snapshots return the same frame until a new one arrives.
    /// </summary>
    [Fact]
    public void CameraSession_Snapshot_Repeated_ReturnsSameFrameUntilNewArrives()
    {
        // Arrange: a session with one frame
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));

        // Act: take two snapshots, deliver a new frame, and take a third
        var first = session.Snapshot();
        var second = session.Snapshot();
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(2));
        var third = session.Snapshot();

        // Assert: the first two are the same frame object, the third is the new frame
        Assert.Same(first, second);
        Assert.Equal(2, third.Sequence);
    }

    /// <summary>
    ///     Test that the session reports its camera and the format that was opened.
    /// </summary>
    [Fact]
    public void CameraSession_Properties_AfterOpen_ReportCameraAndFormat()
    {
        // Arrange / Act: open a session
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);

        // Assert: it knows its camera and its format, is open, and is not ready before the first frame
        Assert.Equal("Fake:0", session.Camera.Id);
        Assert.Equal(source.Cameras[0].Formats[0], session.Format);
        Assert.False(session.IsClosed);
        Assert.False(session.IsReady);
    }

    /// <summary>
    ///     Test that the first snapshot waits for the first frame and returns it when it arrives.
    /// </summary>
    [Fact]
    public async Task CameraSession_Snapshot_FirstFrameArrivesLate_Waits()
    {
        // Arrange: a session with no frame yet, and a snapshot that waits on another thread
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        var waiting = Task.Run(() => session.Snapshot(), TestContext.Current.CancellationToken);

        // Act: deliver the first frame (whether the other thread already waits or not, it must get this frame)
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(7));

        // Assert: the waiting snapshot returns that frame
        var frame = await waiting.WaitAsync(Patience, TestContext.Current.CancellationToken);
        Assert.Equal(7, frame.Sequence);
    }

    /// <summary>
    ///     Test that a camera that delivers no frame gives a timeout with a helpful message.
    /// </summary>
    [Fact]
    public void CameraSession_Snapshot_NoFrame_ThrowsTimeout()
    {
        // Arrange: a session that waits only 100 ms for the first frame, and never gets one
        var source = TestCameras.Source();
        var options = new CameraOpenOptions { WarmUp = TimeSpan.Zero, FirstFrameTimeout = TimeSpan.FromMilliseconds(100) };
        using var session = TestCameras.Open(source, options);

        // Act / Assert: the snapshot times out, and the message names the camera
        var exception = Assert.Throws<CameraTimeoutException>(session.Snapshot);
        Assert.Contains("'Fake Camera'", exception.Message);
        Assert.Contains("no frame", exception.Message);
    }

    /// <summary>
    ///     Test that frames inside the warm-up are never served, and a later frame is.
    /// </summary>
    [Fact]
    public async Task CameraSession_Snapshot_DarkFramesBeforeWarmUp_ServesLaterFrame()
    {
        // Arrange: a session with a 400 ms warm-up that receives a "dark" frame at once
        var source = TestCameras.Source();
        var options = new CameraOpenOptions { WarmUp = TimeSpan.FromMilliseconds(400), FirstFrameTimeout = TimeSpan.FromSeconds(10) };
        using var session = TestCameras.Open(source, options);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));
        Assert.False(session.IsReady);

        // Act: let the warm-up pass, deliver a "bright" frame, and take a snapshot
        await Task.Delay(600, TestContext.Current.CancellationToken);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(2));
        var frame = session.Snapshot();

        // Assert: the frame from after the warm-up is served, never the one from inside it
        Assert.Equal(2, frame.Sequence);
    }

    /// <summary>
    ///     Test that the first snapshot waits for the warm-up and the first frame after it.
    /// </summary>
    [Fact]
    public void CameraSession_Snapshot_WaitsThroughWarmUp_ThenReturnsFirstFrameAfterIt()
    {
        // Arrange: a session with a 300 ms warm-up, whose camera streams frames until the session closes
        var source = TestCameras.StreamingSource();
        var options = new CameraOpenOptions { WarmUp = TimeSpan.FromMilliseconds(300), FirstFrameTimeout = TimeSpan.FromSeconds(10) };
        var started = System.Diagnostics.Stopwatch.StartNew();
        using var session = TestCameras.Open(source, options);

        // Act: take a snapshot at once
        var frame = session.Snapshot();

        // Assert: a frame is returned, and it arrived no earlier than the warm-up after the start
        Assert.True(started.Elapsed >= TimeSpan.FromMilliseconds(250), "the snapshot must wait for the warm-up");
        Assert.NotNull(frame);
    }

    /// <summary>
    ///     Test that with no warm-up the first frame is served.
    /// </summary>
    [Fact]
    public void CameraSession_Snapshot_ZeroWarmUp_ServesFirstFrame()
    {
        // Arrange: a session without warm-up
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);

        // Act: deliver one frame and take a snapshot
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));
        var frame = session.Snapshot();

        // Assert: the first frame is served
        Assert.Equal(1, frame.Sequence);
    }

    /// <summary>
    ///     Test that many threads can take snapshots while frames arrive.
    /// </summary>
    [Fact]
    public async Task CameraSession_Snapshot_ManyThreads_NoExceptionsAndValidFrames()
    {
        // Arrange: a session with one frame, and a camera thread that keeps delivering frames
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));
        using var stop = new CancellationTokenSource();
        var camera = Task.Run(
            () =>
            {
                long index = 2;
                while (!stop.IsCancellationRequested)
                {
                    source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(index % 200));
                    index++;
                    Thread.Yield();
                }
            },
            TestContext.Current.CancellationToken);

        // Act: eight threads take 500 snapshots each
        var workers = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(
                () => Enumerable.Range(0, 500).Select(_ => session.Snapshot()).ToList(),
                TestContext.Current.CancellationToken))
            .ToArray();
        var results = await Task.WhenAll(workers).WaitAsync(Patience, TestContext.Current.CancellationToken);
        await stop.CancelAsync();
        await camera.WaitAsync(Patience, TestContext.Current.CancellationToken);

        // Assert: every snapshot of every thread returned a frame with data
        Assert.All(results.SelectMany(frames => frames), frame => Assert.Equal(1, frame.EncodedData.Length));
    }

    /// <summary>
    ///     Test that disposing twice stops the device once.
    /// </summary>
    [Fact]
    public void CameraSession_Dispose_Twice_DisposesDeviceOnce()
    {
        // Arrange: an open session
        var source = TestCameras.Source();
        var session = TestCameras.Open(source);

        // Act: dispose it twice
        session.Dispose();
        session.Dispose();

        // Assert: the device was stopped exactly once, and the session reports that it is closed
        Assert.Equal(1, source.LastDevice.DisposeCount);
        Assert.True(session.IsClosed);
    }

    /// <summary>
    ///     Test that disposing wakes a snapshot that waits, which then fails with an object-disposed exception.
    /// </summary>
    [Fact]
    public async Task CameraSession_Dispose_WhileWaiting_ThrowsObjectDisposed()
    {
        // Arrange: a session without a frame, and a snapshot that waits on another thread
        var source = TestCameras.Source();
        var session = TestCameras.Open(source);
        var waiting = Task.Run(session.Snapshot, TestContext.Current.CancellationToken);

        // Act: dispose the session
        session.Dispose();

        // Assert: the waiting snapshot fails with an object-disposed exception, and does not wait for the timeout
        await Assert.ThrowsAsync<ObjectDisposedException>(() => waiting.WaitAsync(Patience, TestContext.Current.CancellationToken));
    }

    /// <summary>
    ///     Test that frames after disposal are ignored and snapshots fail.
    /// </summary>
    [Fact]
    public void CameraSession_Dispose_AfterDispose_FramesIgnoredAndSnapshotFails()
    {
        // Arrange: a session with a frame, which is then disposed
        var source = TestCameras.Source();
        var session = TestCameras.Open(source);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));
        var kept = session.Snapshot();
        session.Dispose();

        // Act: a late frame arrives
        var exception = Record.Exception(() => source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(2)));

        // Assert: the late frame does no harm, the session is not ready, snapshots fail, and the earlier frame is still valid
        Assert.Null(exception);
        Assert.False(session.IsReady);
        Assert.Throws<ObjectDisposedException>(session.Snapshot);
        Assert.Equal([(byte)1], kept.EncodedData.ToArray());
    }

    /// <summary>
    ///     Test that a device failure makes snapshots fail with a disconnected exception that carries the cause.
    /// </summary>
    [Fact]
    public void CameraSession_Snapshot_DeviceFaulted_ThrowsDisconnected()
    {
        // Arrange: a session with a frame, and a device that then reports a failure
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));
        var cause = new IOException("the cable is gone");
        source.LastDevice.Fault(cause);

        // Act / Assert: snapshots fail, with the cause attached and in the message
        var exception = Assert.Throws<CameraDisconnectedException>(session.Snapshot);
        Assert.Same(cause, exception.InnerException);
        Assert.Contains("the cable is gone", exception.Message);
        Assert.Contains("'Fake Camera'", exception.Message);
    }

    /// <summary>
    ///     Test that a failure that arrives while a snapshot waits ends the wait.
    /// </summary>
    [Fact]
    public async Task CameraSession_Snapshot_FaultWhileWaiting_ThrowsDisconnected()
    {
        // Arrange: a session without a frame, and a snapshot that waits on another thread
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        var waiting = Task.Run(session.Snapshot, TestContext.Current.CancellationToken);

        // Act: the device fails
        source.LastDevice.Fault(new IOException("failure"));

        // Assert: the waiting snapshot fails with a disconnected exception
        await Assert.ThrowsAsync<CameraDisconnectedException>(() => waiting.WaitAsync(Patience, TestContext.Current.CancellationToken));
    }

    /// <summary>
    ///     Test that a camera that stops delivering frames is reported as disconnected after the stale limit.
    /// </summary>
    [Fact]
    public async Task CameraSession_Snapshot_FramesStop_ThrowsDisconnectedAfterStaleTimeout()
    {
        // Arrange: a session that treats a frame older than 100 ms as stale, with one frame
        var source = TestCameras.Source();
        var options = new CameraOpenOptions { WarmUp = TimeSpan.Zero, StaleFrameTimeout = TimeSpan.FromMilliseconds(100) };
        using var session = TestCameras.Open(source, options);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));

        // Act: let the frame become stale
        await Task.Delay(300, TestContext.Current.CancellationToken);

        // Assert: the snapshot reports a disconnected camera instead of serving the old frame
        var exception = Assert.Throws<CameraDisconnectedException>(session.Snapshot);
        Assert.Contains("no new frame", exception.Message);
    }

    /// <summary>
    ///     Test that a frame decodes to an image when its data is a real picture.
    /// </summary>
    [Fact]
    public void CameraFrame_ToImage_JpegFrame_ReturnsImageOfFrameSize()
    {
        // Arrange: a session that received a real JPEG frame
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 128, 1));
        var frame = session.Snapshot();

        // Act: decode the frame twice
        using var first = frame.ToImage();
        using var second = frame.ToImage();

        // Assert: both images have the frame's size and are independent objects
        Assert.Equal((64, 48), (first.Width, first.Height));
        Assert.NotSame(first, second);
    }

    /// <summary>
    ///     Test that a frame with damaged data reports invalid data when it is decoded.
    /// </summary>
    [Fact]
    public void CameraFrame_ToImage_DamagedFrame_ThrowsInvalidData()
    {
        // Arrange: a session whose latest frame is not a picture
        var source = TestCameras.Source();
        using var session = TestCameras.Open(source);
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));
        var frame = session.Snapshot();

        // Act / Assert: decoding fails with invalid data
        Assert.Throws<InvalidDataException>(frame.ToImage);
    }
}
