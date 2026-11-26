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
using DemaConsulting.AgentVision.Core.Tests.TestSupport;
using FlashCap;
using FlashCap.Utilities;

namespace DemaConsulting.AgentVision.Core.Tests.Cameras;

/// <summary>
///     Unit tests for the FlashCapCameraSource class, including the integration tests that provide the
///     evidence for the FlashCap off-the-shelf item.
/// </summary>
/// <remarks>
///     The mapping tests feed descriptors that belong to no real camera through the real translation. The
///     integration tests (names containing <c>RealFlashCap</c>) call the real FlashCap on the machine that
///     runs them; they assert invariants, never counts, so they pass with and without a camera.
/// </remarks>
public class FlashCapCameraSourceTests
{
    /// <summary>
    ///     Creates a source that translates the given descriptors.
    /// </summary>
    /// <param name="descriptors">The descriptors the source finds.</param>
    /// <returns>The source.</returns>
    private static FlashCapCameraSource SourceOf(params CaptureDeviceDescriptor[] descriptors) =>
        new(() => descriptors);

    /// <summary>
    ///     Test that a null enumeration function is rejected.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_Constructor_NullEnumerate_ThrowsArgumentNullException()
    {
        // Arrange: no setup; null is the input under test

        // Act / Assert: a null function throws and names the parameter
        var exception = Assert.Throws<ArgumentNullException>(() => new FlashCapCameraSource(null!));
        Assert.Equal("enumerate", exception.ParamName);
    }

    /// <summary>
    ///     Test that a descriptor's identity, names, and backend are mapped.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_Descriptor_MapsIdNameDescriptionAndBackend()
    {
        // Arrange: a V4L2 descriptor and a DirectShow descriptor with a numeric identity
        var source = SourceOf(
            new FakeCaptureDeviceDescriptor(DeviceTypes.V4L2, "/dev/video0", "Desk Camera", "Desk Camera (V4L2)"),
            new FakeCaptureDeviceDescriptor(DeviceTypes.VideoForWindows, 0, "Default", "Default (VFW)"));

        // Act: get the cameras
        var cameras = source.GetCameras();

        // Assert: the identifier joins the backend and the identity, in the order FlashCap gave
        Assert.Equal(2, cameras.Count);
        Assert.Equal("V4L2:/dev/video0", cameras[0].Id);
        Assert.Equal("Desk Camera", cameras[0].Name);
        Assert.Equal("Desk Camera (V4L2)", cameras[0].Description);
        Assert.Equal("V4L2", cameras[0].Backend);
        Assert.Equal("VideoForWindows:0", cameras[1].Id);
        Assert.Equal("VideoForWindows", cameras[1].Backend);
    }

    /// <summary>
    ///     Test that a descriptor without an identity still gives a camera with an identifier.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_NullIdentity_GivesIdWithBackendOnly()
    {
        // Arrange: a descriptor whose identity is missing
        var source = SourceOf(new FakeCaptureDeviceDescriptor(DeviceTypes.DirectShow, null, "Camera", "Camera"));

        // Act: get the cameras
        var cameras = source.GetCameras();

        // Assert: the camera is reported, with the backend and an empty identity in its identifier
        Assert.Equal("DirectShow:", Assert.Single(cameras).Id);
    }

    /// <summary>
    ///     Test that size, pixel format, and frame rate of each characteristic are mapped.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_Characteristics_MapSizePixelFormatAndFrameRate()
    {
        // Arrange: a descriptor with two characteristics
        var source = SourceOf(new FakeCaptureDeviceDescriptor(
            DeviceTypes.V4L2,
            "/dev/video0",
            "Camera",
            "Camera",
            new VideoCharacteristics(PixelFormats.JPEG, 1920, 1080, new Fraction(30, 1)),
            new VideoCharacteristics(PixelFormats.YUYV, 640, 480, new Fraction(15, 1))));

        // Act: get the cameras
        var formats = source.GetCameras()[0].Formats;

        // Assert: width, height, the pixel format name, and the frame rate are reported
        Assert.Equal(
            [new CameraFormat(1920, 1080, "JPEG", 30), new CameraFormat(640, 480, "YUYV", 15)],
            formats);
    }

    /// <summary>
    ///     Test that a characteristic FlashCap cannot capture is left out while the device is still reported.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_UnknownPixelFormat_IsExcluded()
    {
        // Arrange: one device with a usable and an unknown characteristic, and one with only an unknown one
        var source = SourceOf(
            new FakeCaptureDeviceDescriptor(
                DeviceTypes.V4L2,
                "/dev/video0",
                "Mixed",
                "Mixed",
                new VideoCharacteristics(PixelFormats.Unknown, 1920, 1080, new Fraction(30, 1)),
                new VideoCharacteristics(PixelFormats.NV12, 640, 480, new Fraction(30, 1))),
            new FakeCaptureDeviceDescriptor(
                DeviceTypes.V4L2,
                "/dev/video2",
                "Unusable",
                "Unusable",
                new VideoCharacteristics(PixelFormats.Unknown, 640, 480, new Fraction(30, 1))));

        // Act: get the cameras
        var cameras = source.GetCameras();

        // Assert: the unknown characteristic is not a format, and the device with none is still listed
        Assert.Equal([new CameraFormat(640, 480, "NV12", 30)], cameras[0].Formats);
        Assert.Equal("V4L2:/dev/video2", cameras[1].Id);
        Assert.Empty(cameras[1].Formats);
    }

    /// <summary>
    ///     Test that a fractional frame rate is reported as an exact floating-point value.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_FractionalFrameRate_IsReportedAsDouble()
    {
        // Arrange: a characteristic with the common fractional frame rate 30000/1001 (about 29.97)
        var source = SourceOf(new FakeCaptureDeviceDescriptor(
            DeviceTypes.V4L2,
            "/dev/video0",
            "Camera",
            "Camera",
            new VideoCharacteristics(PixelFormats.YUYV, 1280, 720, new Fraction(30000, 1001))));

        // Act: get the frame rate
        var framesPerSecond = source.GetCameras()[0].Formats[0].FramesPerSecond;

        // Assert: the rate is the quotient of the fraction
        Assert.Equal(30000.0 / 1001.0, framesPerSecond, 6);
    }

    /// <summary>
    ///     Test that a zero frame rate is reported as zero.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_ZeroFrameRate_IsReportedAsZero()
    {
        // Arrange: a characteristic whose frame rate is the zero fraction
        var source = SourceOf(new FakeCaptureDeviceDescriptor(
            DeviceTypes.V4L2,
            "/dev/video0",
            "Camera",
            "Camera",
            new VideoCharacteristics(PixelFormats.YUYV, 640, 480, default)));

        // Act: get the frame rate
        var framesPerSecond = source.GetCameras()[0].Formats[0].FramesPerSecond;

        // Assert: no rate is reported as zero
        Assert.Equal(0.0, framesPerSecond);
    }

    /// <summary>
    ///     Test that no descriptors give an empty list.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_NoDescriptors_ReturnsEmptyList()
    {
        // Arrange: a source that finds no device
        var source = SourceOf();

        // Act: get the cameras
        var cameras = source.GetCameras();

        // Assert: the list is empty, which is a valid answer
        Assert.NotNull(cameras);
        Assert.Empty(cameras);
    }

    /// <summary>
    ///     Integration test with the real FlashCap: enumeration works, with or without a camera, and every
    ///     camera it reports is complete.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_RealFlashCap_EnumeratesWithoutCamerasPresent()
    {
        // Arrange: the default source, which asks the real FlashCap of this machine
        var source = new FlashCapCameraSource();

        // Act: get the cameras; FlashCap reports an empty set, not an error, when there is none
        var cameras = source.GetCameras();

        // Assert: the list exists, and every camera it holds satisfies the reporting invariants
        Assert.NotNull(cameras);
        Assert.All(cameras, camera =>
        {
            Assert.False(string.IsNullOrWhiteSpace(camera.Id));
            Assert.False(string.IsNullOrWhiteSpace(camera.Backend));
            Assert.StartsWith($"{camera.Backend}:", camera.Id);
            Assert.NotNull(camera.Name);
            Assert.NotNull(camera.Formats);
            Assert.All(camera.Formats, format =>
            {
                Assert.True(format.Width > 0);
                Assert.True(format.Height > 0);
                Assert.True(format.FramesPerSecond >= 0);
                Assert.False(string.IsNullOrWhiteSpace(format.PixelFormat));
                Assert.NotEqual("Unknown", format.PixelFormat);
            });
        });
    }

    /// <summary>
    ///     Integration test with the real FlashCap: every backend it reports belongs to the operating system
    ///     the test runs on.
    /// </summary>
    [Fact]
    public void FlashCapCameraSource_GetCameras_RealFlashCap_BackendMatchesPlatform()
    {
        // Arrange: the default source, and the backends each supported operating system may report
        var source = new FlashCapCameraSource();
        var expected = BackendsOfThisPlatform();

        // Act: get the cameras
        var backends = source.GetCameras().Select(camera => camera.Backend).Distinct().ToList();

        // Assert: no backend of another platform appears; the count of cameras is not asserted, because it
        // depends on the machine. Other operating systems are not supported, so they expect no camera.
        Assert.All(backends, backend => Assert.Contains(backend, expected));
    }

    /// <summary>
    ///     Gets the FlashCap backends that the operating system of this test run may report.
    /// </summary>
    /// <returns>The backend names; empty on an unsupported operating system.</returns>
    private static string[] BackendsOfThisPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return ["DirectShow", "MediaFoundation", "VideoForWindows"];
        }

        return OperatingSystem.IsLinux() ? ["V4L2"] : [];
    }
}
