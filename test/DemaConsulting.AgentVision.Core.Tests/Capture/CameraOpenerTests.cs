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
using DemaConsulting.AgentVision.Core.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Core.Tests.Capture;

/// <summary>
///     Unit tests for the CameraOpener unit: selecting, opening, option checks, and failure translation.
/// </summary>
public class CameraOpenerTests
{
    /// <summary>
    ///     Test that a null source is rejected.
    /// </summary>
    [Fact]
    public void CameraOpener_Constructor_NullSource_ThrowsArgumentNullException()
    {
        // Arrange: no setup; null is the input under test

        // Act / Assert: a null source throws and names the parameter
        var exception = Assert.Throws<ArgumentNullException>(() => new CameraOpener(null!));
        Assert.Equal("source", exception.ParamName);
    }

    /// <summary>
    ///     Test that the default opener can be created without a camera.
    /// </summary>
    [Fact]
    public void CameraOpener_Constructor_Default_CreatesOpener()
    {
        // Arrange / Act: create the default opener, which asks the real FlashCap only when it opens a camera
        var opener = new CameraOpener();

        // Assert: the opener exists
        Assert.NotNull(opener);
    }

    /// <summary>
    ///     Test that a selector opens the camera it names with the format chosen for it.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_Selector_ReturnsSessionForCamera()
    {
        // Arrange: a source with two cameras
        var source = new FakeCameraSource([TestCameras.Camera("Fake:0", "First"), TestCameras.Camera("Fake:1", "Second")]);
        var opener = new CameraOpener(source);

        // Act: open the second camera by name
        using var session = opener.Open("second", TestCameras.QuickOptions());

        // Assert: the session and the device belong to the second camera, in the camera's format
        Assert.Equal("Fake:1", session.Camera.Id);
        var device = Assert.Single(source.Devices);
        Assert.Equal("Fake:1", device.Camera.Id);
        Assert.Equal(session.Format, device.Format);
    }

    /// <summary>
    ///     Test that a described camera can be opened without a selector.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_CameraInfo_ReturnsSession()
    {
        // Arrange: a source and the description of its camera
        var source = TestCameras.Source();
        var opener = new CameraOpener(source);

        // Act: open the camera by its description, with default options
        using var session = opener.Open(source.Cameras[0]);

        // Assert: a session for the camera exists, and its device was opened
        Assert.Equal("Fake:0", session.Camera.Id);
        Assert.Single(source.Devices);
    }

    /// <summary>
    ///     Test that the default options are the documented ones.
    /// </summary>
    [Fact]
    public void CameraOpenOptions_Default_HasDocumentedValues()
    {
        // Arrange / Act: the default options
        var options = new CameraOpenOptions();

        // Assert: one second warm-up, five seconds for the first frame and for staleness, no size preference
        Assert.Equal(TimeSpan.FromMilliseconds(1000), options.WarmUp);
        Assert.Equal(TimeSpan.FromSeconds(5), options.FirstFrameTimeout);
        Assert.Equal(TimeSpan.FromSeconds(5), options.StaleFrameTimeout);
        Assert.Null(options.Width);
        Assert.Null(options.Height);
        Assert.Null(options.FramesPerSecond);
    }

    /// <summary>
    ///     Test that invalid options are rejected before any camera is touched.
    /// </summary>
    /// <param name="option">The case: which option is invalid and how.</param>
    /// <param name="named">The name of the option that the message must mention.</param>
    [Theory]
    [InlineData("WarmUp", "WarmUp")]
    [InlineData("Width", "Width")]
    [InlineData("Height", "Height")]
    [InlineData("FramesPerSecond", "FramesPerSecond")]
    [InlineData("NaN", "FramesPerSecond")]
    [InlineData("FirstFrameTimeout", "FirstFrameTimeout")]
    [InlineData("StaleFrameTimeout", "StaleFrameTimeout")]
    [InlineData("TooLong", "WarmUp")]
    public void CameraOpener_Open_InvalidOption_ThrowsArgumentOutOfRange(string option, string named)
    {
        // Arrange: a source and options with one invalid value
        var source = TestCameras.Source();
        var options = option switch
        {
            "WarmUp" => new CameraOpenOptions { WarmUp = TimeSpan.FromMilliseconds(-1) },
            "Width" => new CameraOpenOptions { Width = 0 },
            "Height" => new CameraOpenOptions { Height = -4 },
            "FramesPerSecond" => new CameraOpenOptions { FramesPerSecond = 0 },
            "NaN" => new CameraOpenOptions { FramesPerSecond = double.NaN },
            "FirstFrameTimeout" => new CameraOpenOptions { FirstFrameTimeout = TimeSpan.Zero },
            "StaleFrameTimeout" => new CameraOpenOptions { StaleFrameTimeout = TimeSpan.FromSeconds(-1) },
            _ => new CameraOpenOptions { WarmUp = TimeSpan.FromHours(2) }
        };

        // Act / Assert: opening is rejected, the message names the option, and the source was not asked for anything
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new CameraOpener(source).Open("0", options));
        Assert.Equal("options", exception.ParamName);
        Assert.Contains(named, exception.Message);
        Assert.Equal(0, source.CallCount);
        Assert.Empty(source.Devices);
    }

    /// <summary>
    ///     Test that a null selector and a null camera are rejected.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_NullArguments_ThrowsArgumentNullException()
    {
        // Arrange: an opener
        var opener = new CameraOpener(TestCameras.Source());

        // Act / Assert: a null selector and a null camera are rejected
        Assert.Throws<ArgumentNullException>(() => opener.Open((string)null!));
        Assert.Throws<ArgumentNullException>(() => opener.Open((CameraInfo)null!));
    }

    /// <summary>
    ///     Test that an unknown camera gives a not-found exception and opens nothing.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_UnknownSelector_ThrowsNotFoundAndOpensNothing()
    {
        // Arrange: a source with one camera
        var source = TestCameras.Source();

        // Act / Assert: an unknown name is not found, and no device was opened
        Assert.Throws<CameraNotFoundException>(() => new CameraOpener(source).Open("Nothing", TestCameras.QuickOptions()));
        Assert.Empty(source.Devices);
    }

    /// <summary>
    ///     Test that a failure of the source becomes an open exception that names the camera and keeps the cause.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_SourceThrows_ThrowsCameraOpenWithInner()
    {
        // Arrange: a source whose camera is held by another application
        var cause = new IOException("device busy");
        var source = TestCameras.Source();
        source.OpenFailure = cause;

        // Act / Assert: the failure is translated, names the camera, and keeps the cause; no device remains open
        var exception = Assert.Throws<CameraOpenException>(() => new CameraOpener(source).Open("0", TestCameras.QuickOptions()));
        Assert.Same(cause, exception.InnerException);
        Assert.Contains("'Fake Camera'", exception.Message);
        Assert.Contains("device busy", exception.Message);
        Assert.Empty(source.Devices);
    }

    /// <summary>
    ///     Test that a library exception of the source is passed on unchanged.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_SourceThrowsCameraException_PassesItOn()
    {
        // Arrange: a source that reports a camera open exception of its own
        var failure = new CameraOpenException("the source says no");
        var source = TestCameras.Source();
        source.OpenFailure = failure;

        // Act / Assert: the same exception object reaches the caller
        var exception = Assert.Throws<CameraOpenException>(() => new CameraOpener(source).Open("0", TestCameras.QuickOptions()));
        Assert.Same(failure, exception);
    }

    /// <summary>
    ///     Test that a camera without a usable format fails before the device is opened.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_NoUsableFormat_ThrowsOpenExceptionAndOpensNothing()
    {
        // Arrange: a camera that only offers a 16-bit format
        var source = new FakeCameraSource([new CameraInfo("Fake:0", "Odd", "Odd", "Fake", [new CameraFormat(640, 480, "RGB16", 30)])]);

        // Act / Assert: opening fails, and the device was never opened
        Assert.Throws<CameraOpenException>(() => new CameraOpener(source).Open("0", TestCameras.QuickOptions()));
        Assert.Empty(source.Devices);
    }

    /// <summary>
    ///     Test that the chosen format is the one passed to the source.
    /// </summary>
    [Fact]
    public void CameraOpener_Open_RequestedSize_PassesChosenFormatToSource()
    {
        // Arrange: a camera with two sizes
        var source = new FakeCameraSource(
        [
            new CameraInfo(
                "Fake:0",
                "Cam",
                "Cam",
                "Fake",
                [new CameraFormat(1280, 720, "JPEG", 30), new CameraFormat(640, 480, "JPEG", 30)])
        ]);
        var options = new CameraOpenOptions { Width = 640, Height = 480, WarmUp = TimeSpan.Zero };

        // Act: open with a request for the smaller size
        using var session = new CameraOpener(source).Open("0", options);

        // Assert: the source opened the smaller size, and the session reports it
        Assert.Equal(new CameraFormat(640, 480, "JPEG", 30), source.LastDevice.Format);
        Assert.Equal(source.LastDevice.Format, session.Format);
    }
}
