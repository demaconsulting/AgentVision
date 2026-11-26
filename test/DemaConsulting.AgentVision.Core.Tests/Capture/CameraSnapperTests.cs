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
///     Unit tests for the CameraSnapper unit: the one-shot open, snap, crop, save, and close.
/// </summary>
public class CameraSnapperTests
{
    /// <summary>
    ///     Test that a null source and a null request are rejected.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_NullArguments_ThrowsArgumentNullException()
    {
        // Arrange: no setup; null is the input under test

        // Act / Assert: a null source and a null request are rejected
        Assert.Throws<ArgumentNullException>(() => new CameraSnapper(null!));
        Assert.Throws<ArgumentNullException>(() => new CameraSnapper(TestCameras.Source()).Snap(null!));
    }

    /// <summary>
    ///     Test that the default snapper can be created without a camera.
    /// </summary>
    [Fact]
    public void CameraSnapper_Constructor_Default_CreatesSnapper()
    {
        // Arrange / Act: create the default snapper, which asks the real FlashCap only when it snaps
        var snapper = new CameraSnapper();

        // Assert: the snapper exists
        Assert.NotNull(snapper);
    }

    /// <summary>
    ///     Test that a snap writes the picture and reports it.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_Valid_WritesImageAndReturnsResult()
    {
        // Arrange: a streaming fake camera and a folder
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var path = folder.File("snap.png");
        var request = new SnapRequest("Fake Camera", path) { Options = TestCameras.QuickOptions() };

        // Act: take the picture
        var result = new CameraSnapper(source).Snap(request);

        // Assert: the file exists with the frame's size, the result reports it, and the camera was closed
        using var saved = PngCodec.Load(path);
        Assert.Equal((64, 48), (saved.Width, saved.Height));
        Assert.Equal(path, result.OutputPath);
        Assert.Equal((64, 48), (result.Width, result.Height));
        Assert.Equal("Fake:0", result.Camera.Id);
        Assert.Equal(new CameraFormat(64, 48, "JPEG", 30), result.Format);
        Assert.True(source.LastDevice.IsDisposed);
    }

    /// <summary>
    ///     Test that a crop is applied before saving.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_WithCrop_SavesCroppedSize()
    {
        // Arrange: a streaming fake camera and a request that keeps a 16x12 region
        using var folder = new TempDirectory();
        var path = folder.File("snap.jpg");
        var request = new SnapRequest("0", path) { Crop = new CropRectangle(8, 4, 16, 12), Options = TestCameras.QuickOptions() };

        // Act: take the picture
        var result = new CameraSnapper(TestCameras.StreamingSource()).Snap(request);

        // Assert: the file and the result have the size of the region
        using var saved = JpegCodec.Load(path);
        Assert.Equal((16, 12), (saved.Width, saved.Height));
        Assert.Equal((16, 12), (result.Width, result.Height));
    }

    /// <summary>
    ///     Test that an unsupported extension is rejected before any camera is touched.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_UnsupportedExtension_ThrowsBeforeOpen()
    {
        // Arrange: a source and a request for a GIF file
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var request = new SnapRequest("0", folder.File("snap.gif"));

        // Act / Assert: the request is rejected with the supported extensions, and the source was not even asked
        var exception = Assert.Throws<ArgumentException>(() => new CameraSnapper(source).Snap(request));
        Assert.Contains(".png", exception.Message);
        Assert.Equal(0, source.CallCount);
        Assert.Empty(source.Devices);
    }

    /// <summary>
    ///     Test that an empty output path is rejected before any camera is touched.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_EmptyOutputPath_ThrowsBeforeOpen()
    {
        // Arrange: a source and a request without an output path
        var source = TestCameras.StreamingSource();

        // Act / Assert: the request is rejected and the source was not asked
        Assert.Throws<ArgumentException>(() => new CameraSnapper(source).Snap(new SnapRequest("0", " ")));
        Assert.Equal(0, source.CallCount);
    }

    /// <summary>
    ///     Test that a missing output folder is rejected before any camera is touched.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_MissingFolder_ThrowsBeforeOpen()
    {
        // Arrange: a source and an output path in a folder that does not exist
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var request = new SnapRequest("0", folder.File(Path.Combine("missing", "snap.png")));

        // Act / Assert: the request is rejected, naming the folder, and no device was opened
        var exception = Assert.Throws<DirectoryNotFoundException>(() => new CameraSnapper(source).Snap(request));
        Assert.Contains("missing", exception.Message);
        Assert.Empty(source.Devices);
    }

    /// <summary>
    ///     Test that a crop outside the format of the camera is rejected before the camera is opened.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_CropOutsideFormat_ThrowsBeforeOpen()
    {
        // Arrange: a camera that delivers 64x48 and a crop that reaches past the right edge
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var request = new SnapRequest("0", folder.File("snap.png")) { Crop = new CropRectangle(60, 0, 10, 10) };

        // Act / Assert: the crop is rejected with the size of the picture, and no device was opened
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new CameraSnapper(source).Snap(request));
        Assert.Contains("64x48", exception.Message);
        Assert.Empty(source.Devices);
    }

    /// <summary>
    ///     Test that a crop that is not valid at all is rejected before the camera is opened.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_InvalidCrop_ThrowsBeforeOpen()
    {
        // Arrange: a crop without area
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var request = new SnapRequest("0", folder.File("snap.png")) { Crop = new CropRectangle(0, 0, 0, 10) };

        // Act / Assert: the crop is rejected and the source was not asked
        Assert.Throws<ArgumentOutOfRangeException>(() => new CameraSnapper(source).Snap(request));
        Assert.Equal(0, source.CallCount);
    }

    /// <summary>
    ///     Test that invalid options are rejected before the camera is opened.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_InvalidOptions_ThrowsBeforeOpen()
    {
        // Arrange: options with a negative warm-up
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var request = new SnapRequest("0", folder.File("snap.png")) { Options = new CameraOpenOptions { WarmUp = TimeSpan.FromSeconds(-1) } };

        // Act / Assert: the options are rejected and the source was not asked
        Assert.Throws<ArgumentOutOfRangeException>(() => new CameraSnapper(source).Snap(request));
        Assert.Equal(0, source.CallCount);
    }

    /// <summary>
    ///     Test that an unknown camera is reported and nothing is written.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_UnknownCamera_ThrowsNotFoundAndWritesNothing()
    {
        // Arrange: a source with one camera
        using var folder = new TempDirectory();
        var request = new SnapRequest("99", folder.File("snap.png"));

        // Act / Assert: the camera is not found, and no file was written
        Assert.Throws<CameraNotFoundException>(() => new CameraSnapper(TestCameras.StreamingSource()).Snap(request));
        Assert.Empty(Directory.GetFiles(folder.Path));
    }

    /// <summary>
    ///     Test that a crop that fits the announced format but not the frame that arrives is rejected, and the
    ///     camera is still closed.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_CropOutsideDecodedFrame_ThrowsAndDisposesDevice()
    {
        // Arrange: a camera that announces 640x480 but delivers 64x48 frames, and a crop that fits the announcement
        using var folder = new TempDirectory();
        var jpeg = SyntheticFrames.SolidJpeg(64, 48, 100);
        var source = new FakeCameraSource(
        [
            new CameraInfo(
                "Fake:0",
                "Liar",
                "Liar",
                "Fake",
                [new CameraFormat(640, 480, "JPEG", 30)])
        ])
        {
            FrameFactory = index => new EncodedFrame((byte[])jpeg.Clone(), TimeSpan.Zero, index)
        };
        var request = new SnapRequest("0", folder.File("snap.png"))
        {
            Crop = new CropRectangle(100, 100, 10, 10),
            Options = TestCameras.QuickOptions()
        };

        // Act / Assert: the crop is rejected against the real size, the camera is closed, and nothing is written
        Assert.Throws<ArgumentOutOfRangeException>(() => new CameraSnapper(source).Snap(request));
        Assert.True(source.LastDevice.IsDisposed);
        Assert.Empty(Directory.GetFiles(folder.Path));
    }

    /// <summary>
    ///     Test that the camera is closed when saving fails.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_SaveFails_StillDisposesDevice()
    {
        // Arrange: an output path that is an existing folder, so that the save fails after the camera was used
        using var folder = new TempDirectory();
        var path = folder.File("snap.png");
        Directory.CreateDirectory(path);
        var source = TestCameras.StreamingSource();
        var request = new SnapRequest("0", path) { Options = TestCameras.QuickOptions() };

        // Act / Assert: the save fails with an access or I/O error, and the camera was closed
        Assert.ThrowsAny<Exception>(() => new CameraSnapper(source).Snap(request));
        Assert.True(source.LastDevice.IsDisposed);
    }

    /// <summary>
    ///     Test that the camera is closed when it delivers no frame.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_Timeout_StillDisposesDevice()
    {
        // Arrange: a camera that never delivers a frame, and a short first-frame timeout
        using var folder = new TempDirectory();
        var source = TestCameras.Source();
        var options = new CameraOpenOptions { WarmUp = TimeSpan.Zero, FirstFrameTimeout = TimeSpan.FromMilliseconds(100) };
        var request = new SnapRequest("0", folder.File("snap.png")) { Options = options };

        // Act / Assert: the snap times out, and the camera was closed
        Assert.Throws<CameraTimeoutException>(() => new CameraSnapper(source).Snap(request));
        Assert.True(source.LastDevice.IsDisposed);
    }

    /// <summary>
    ///     Test that the camera is closed when the frame cannot be decoded.
    /// </summary>
    [Fact]
    public void CameraSnapper_Snap_DamagedFrame_ThrowsInvalidDataAndDisposesDevice()
    {
        // Arrange: a camera that delivers bytes that are no picture
        using var folder = new TempDirectory();
        var source = TestCameras.Source();
        source.FrameFactory = index => SyntheticFrames.OpaqueFrame(index % 200);
        var request = new SnapRequest("0", folder.File("snap.png")) { Options = TestCameras.QuickOptions() };

        // Act / Assert: decoding fails with invalid data, and the camera was closed
        Assert.Throws<InvalidDataException>(() => new CameraSnapper(source).Snap(request));
        Assert.True(source.LastDevice.IsDisposed);
    }
}
