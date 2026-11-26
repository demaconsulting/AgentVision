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

namespace DemaConsulting.AgentVision.Core.Tests.Capture;

/// <summary>
///     Unit tests for the CameraSelector unit: which camera and which format a request means.
/// </summary>
public class CameraSelectorTests
{
    /// <summary>
    ///     Creates a camera description for a test.
    /// </summary>
    /// <param name="backend">The backend of the camera.</param>
    /// <param name="id">The identifier of the camera.</param>
    /// <param name="name">The name of the camera.</param>
    /// <param name="formats">The formats of the camera.</param>
    /// <returns>The camera description.</returns>
    private static CameraInfo Camera(string backend, string id, string name, params CameraFormat[] formats) =>
        new(id, name, name + " (" + backend + ")", backend, formats);

    /// <summary>
    ///     Creates a camera with the given formats for the format tests.
    /// </summary>
    /// <param name="formats">The formats of the camera.</param>
    /// <returns>The camera.</returns>
    private static CameraInfo WithFormats(params CameraFormat[] formats) => Camera("V4L2", "V4L2:/dev/video0", "Cam", formats);

    /// <summary>
    ///     The cameras of a Windows machine: one physical camera through two backends, and a second camera.
    /// </summary>
    private static readonly CameraInfo[] Cameras =
    [
        Camera("DirectShow", "DirectShow:a", "Desk Camera"),
        Camera("DirectShow", "DirectShow:b", "Side Camera"),
        Camera("MediaFoundation", "MediaFoundation:a", "Desk Camera"),
        Camera("V4L2", "V4L2:/dev/video4", "7")
    ];

    /// <summary>
    ///     Test that a numeric selector is the position in the list.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_Index_ReturnsCameraAtIndex()
    {
        // Arrange: four cameras

        // Act: select by index
        var camera = CameraSelector.SelectCamera(Cameras, "1");

        // Assert: the camera at that position is returned
        Assert.Equal("DirectShow:b", camera.Id);
    }

    /// <summary>
    ///     Test that an index beyond the list is an error and is not tried as a name.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_IndexOutOfRange_ThrowsNotFoundAndIgnoresName()
    {
        // Arrange: four cameras, one of which is named "7"

        // Act / Assert: the index 7 does not exist, so the camera named "7" is not returned
        var exception = Assert.Throws<CameraNotFoundException>(() => CameraSelector.SelectCamera(Cameras, "7"));
        Assert.Equal("7", exception.Selector);
    }

    /// <summary>
    ///     Test that an exact identifier selects its camera.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_Id_ReturnsCamera()
    {
        // Arrange: four cameras

        // Act: select by identifier
        var camera = CameraSelector.SelectCamera(Cameras, "MediaFoundation:a");

        // Assert: the camera with that identifier is returned, not the DirectShow camera of the same name
        Assert.Equal("MediaFoundation", camera.Backend);
        Assert.Equal("MediaFoundation:a", camera.Id);
    }

    /// <summary>
    ///     Test that a name is matched without regard to letter case.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_Name_IsCaseInsensitive()
    {
        // Arrange: four cameras

        // Act: select by a name in other letter case, with surrounding blanks
        var camera = CameraSelector.SelectCamera(Cameras, "  side CAMERA ");

        // Assert: the camera of that name is returned
        Assert.Equal("DirectShow:b", camera.Id);
    }

    /// <summary>
    ///     Test that a name that several backends report picks the first backend in list order.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_NameOnTwoBackends_PicksFirstBackend()
    {
        // Arrange: "Desk Camera" is reported by DirectShow and by Media Foundation

        // Act: select by the name
        var camera = CameraSelector.SelectCamera(Cameras, "Desk Camera");

        // Assert: the first backend in list order wins
        Assert.Equal("DirectShow:a", camera.Id);
    }

    /// <summary>
    ///     Test that two cameras of one backend with the same name are ambiguous, with advice on what to use.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_NameTwiceOnSameBackend_ThrowsWithGuidance()
    {
        // Arrange: two V4L2 cameras with the same name
        CameraInfo[] cameras =
        [
            Camera("V4L2", "V4L2:/dev/video0", "Twin"),
            Camera("V4L2", "V4L2:/dev/video2", "Twin")
        ];

        // Act / Assert: the name is ambiguous, and the message gives the indexes and identifiers to use instead
        var exception = Assert.Throws<ArgumentException>(() => CameraSelector.SelectCamera(cameras, "Twin"));
        Assert.Contains("0 (V4L2:/dev/video0)", exception.Message);
        Assert.Contains("1 (V4L2:/dev/video2)", exception.Message);
        Assert.Contains("index", exception.Message);
    }

    /// <summary>
    ///     Test that an unknown selector gives a message that lists the available cameras.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_Unknown_MessageListsAvailable()
    {
        // Arrange: four cameras

        // Act / Assert: nothing matches, and the message names the selector and every camera with its index
        var exception = Assert.Throws<CameraNotFoundException>(() => CameraSelector.SelectCamera(Cameras, "Missing"));
        Assert.Contains("'Missing' was not found", exception.Message);
        Assert.Contains("0: Desk Camera (DirectShow)", exception.Message);
        Assert.Contains("3: 7 (V4L2)", exception.Message);
        Assert.Equal(4, exception.Available.Count);
    }

    /// <summary>
    ///     Test that the message says so when the machine has no camera.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_NoCameras_MessageSaysNone()
    {
        // Arrange: no camera

        // Act / Assert: nothing can match, and the message says that no camera is available
        var exception = Assert.Throws<CameraNotFoundException>(() => CameraSelector.SelectCamera([], "0"));
        Assert.Contains("no camera is available", exception.Message);
        Assert.Empty(exception.Available);
    }

    /// <summary>
    ///     Test that empty and null selectors are rejected as argument errors.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectCamera_EmptyOrNull_ThrowsArgumentException()
    {
        // Arrange: four cameras

        // Act / Assert: an empty selector is an argument error, and so is a null one
        Assert.Throws<ArgumentException>(() => CameraSelector.SelectCamera(Cameras, "  "));
        Assert.Throws<ArgumentNullException>(() => CameraSelector.SelectCamera(Cameras, null!));
        Assert.Throws<ArgumentNullException>(() => CameraSelector.SelectCamera(null!, "0"));
    }

    /// <summary>
    ///     Test that without a request the default is the largest size within full HD, with a usable frame rate,
    ///     in the preferred pixel format.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_NoRequest_PrefersHdJpeg()
    {
        // Arrange: a camera with 4K, full HD in two pixel formats, and VGA
        var camera = WithFormats(
            new CameraFormat(3840, 2160, "JPEG", 30),
            new CameraFormat(1920, 1080, "YUYV", 30),
            new CameraFormat(1920, 1080, "JPEG", 30),
            new CameraFormat(640, 480, "JPEG", 30));

        // Act: choose without a request
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions());

        // Assert: full HD JPEG, not 4K and not the raw format
        Assert.Equal(new CameraFormat(1920, 1080, "JPEG", 30), format);
    }

    /// <summary>
    ///     Test that a larger size with a rate below the default minimum loses to a smaller size that is fast enough.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_NoRequest_SkipsFormatsBelowMinimumRate()
    {
        // Arrange: full HD at 5 fps and 720p at 30 fps
        var camera = WithFormats(
            new CameraFormat(1920, 1080, "JPEG", 5),
            new CameraFormat(1280, 720, "JPEG", 30));

        // Act: choose without a request
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions());

        // Assert: the fast format wins
        Assert.Equal(new CameraFormat(1280, 720, "JPEG", 30), format);
    }

    /// <summary>
    ///     Test that a camera that only offers sizes above full HD gets its smallest size that is fast enough.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_Only4kFormats_ChoosesSmallestAtLeast15Fps()
    {
        // Arrange: a camera with only large formats
        var camera = WithFormats(
            new CameraFormat(3840, 2160, "JPEG", 30),
            new CameraFormat(2592, 1944, "JPEG", 30),
            new CameraFormat(2592, 1944, "JPEG", 5));

        // Act: choose without a request
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions());

        // Assert: the smallest fast size
        Assert.Equal(new CameraFormat(2592, 1944, "JPEG", 30), format);
    }

    /// <summary>
    ///     Test that a format with an exact match of the request is chosen.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_ExactRequest_ReturnsExact()
    {
        // Arrange: a camera with three sizes, and a request for one of them
        var camera = WithFormats(
            new CameraFormat(1920, 1080, "JPEG", 30),
            new CameraFormat(1280, 720, "JPEG", 30),
            new CameraFormat(640, 480, "YUYV", 30));

        // Act: choose 640x480
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions { Width = 640, Height = 480 });

        // Assert: the exact size, in the pixel format the camera offers it in
        Assert.Equal(new CameraFormat(640, 480, "YUYV", 30), format);
    }

    /// <summary>
    ///     Test that a size that the camera does not offer gives the nearest size and never fails.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_AbsentSize_ReturnsNearest()
    {
        // Arrange: a camera without 900x700
        var camera = WithFormats(
            new CameraFormat(1920, 1080, "JPEG", 30),
            new CameraFormat(1024, 768, "JPEG", 30),
            new CameraFormat(640, 480, "JPEG", 30));

        // Act: choose 900x700
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions { Width = 900, Height = 700 });

        // Assert: 1024x768 is nearest (192 pixels away; 640x480 is 480 away)
        Assert.Equal(new CameraFormat(1024, 768, "JPEG", 30), format);
    }

    /// <summary>
    ///     Test that an unspecified dimension counts as a match.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_OnlyWidth_IgnoresHeight()
    {
        // Arrange: two sizes, where the one with the exact width has an unusual height
        var camera = WithFormats(
            new CameraFormat(1280, 960, "JPEG", 30),
            new CameraFormat(1000, 720, "JPEG", 30));

        // Act: choose by width only
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions { Width = 1280 });

        // Assert: the exact width wins whatever the height
        Assert.Equal(new CameraFormat(1280, 960, "JPEG", 30), format);
    }

    /// <summary>
    ///     Test that a requested frame rate prefers a format that is not below it, then the closest.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_Fps_PrefersNotBelowThenClosest()
    {
        // Arrange: one size at 15, 24, and 60 fps
        var camera = WithFormats(
            new CameraFormat(1280, 720, "JPEG", 15),
            new CameraFormat(1280, 720, "JPEG", 24),
            new CameraFormat(1280, 720, "JPEG", 60));

        // Act: choose 20 fps and 30 fps
        var at20 = CameraSelector.SelectFormat(camera, new CameraOpenOptions { Width = 1280, Height = 720, FramesPerSecond = 20 });
        var at30 = CameraSelector.SelectFormat(camera, new CameraOpenOptions { Width = 1280, Height = 720, FramesPerSecond = 30 });

        // Assert: 24 is the closest at or above 20, and 60 is the only rate at or above 30
        Assert.Equal(24, at20.FramesPerSecond);
        Assert.Equal(60, at30.FramesPerSecond);
    }

    /// <summary>
    ///     Test that a frame rate request alone keeps the default size policy.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_OnlyFps_KeepsDefaultSizePolicy()
    {
        // Arrange: full HD at 30 fps and VGA at 60 fps
        var camera = WithFormats(
            new CameraFormat(1920, 1080, "JPEG", 30),
            new CameraFormat(640, 480, "JPEG", 60));

        // Act: choose 60 fps
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions { FramesPerSecond = 60 });

        // Assert: the only format that reaches 60 fps
        Assert.Equal(new CameraFormat(640, 480, "JPEG", 60), format);
    }

    /// <summary>
    ///     Test that formats that cannot be converted to a picture are never chosen.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_UnusableFormatsPresent_AreSkipped()
    {
        // Arrange: a camera whose largest format is 16-bit RGB
        var camera = WithFormats(
            new CameraFormat(1920, 1080, "RGB16", 30),
            new CameraFormat(640, 480, "YUYV", 30));

        // Act: choose without a request
        var format = CameraSelector.SelectFormat(camera, new CameraOpenOptions());

        // Assert: the usable format is chosen
        Assert.Equal("YUYV", format.PixelFormat);
    }

    /// <summary>
    ///     Test that a camera with only unusable formats, or none, is an open error that names the camera.
    /// </summary>
    /// <param name="pixelFormat">The pixel format of the only format, or empty for none.</param>
    [Theory]
    [InlineData("RGB16")]
    [InlineData("RGB8")]
    [InlineData("")]
    public void CameraSelector_SelectFormat_NoUsableFormat_ThrowsOpenException(string pixelFormat)
    {
        // Arrange: a camera with only an unusable format, or with no format
        var camera = pixelFormat.Length == 0
            ? WithFormats()
            : WithFormats(new CameraFormat(640, 480, pixelFormat, 30));

        // Act / Assert: no format can be chosen, and the message names the camera
        var exception = Assert.Throws<CameraOpenException>(() => CameraSelector.SelectFormat(camera, new CameraOpenOptions()));
        Assert.Contains("'Cam'", exception.Message);
    }

    /// <summary>
    ///     Test that null arguments are rejected.
    /// </summary>
    [Fact]
    public void CameraSelector_SelectFormat_NullArguments_ThrowsArgumentNullException()
    {
        // Arrange: a camera

        // Act / Assert: null arguments are rejected
        Assert.Throws<ArgumentNullException>(() => CameraSelector.SelectFormat(null!, new CameraOpenOptions()));
        Assert.Throws<ArgumentNullException>(() => CameraSelector.SelectFormat(WithFormats(), null!));
    }
}
