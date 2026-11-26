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
///     Subsystem tests for the Cameras subsystem: the camera enumerator working with the FlashCap camera
///     source and with a custom source.
/// </summary>
public class CamerasSubsystemTests
{
    /// <summary>
    ///     Test that FlashCap descriptors pass through the source and the enumerator to an ordered list.
    /// </summary>
    [Fact]
    public void CamerasSubsystem_Enumeration_FlashCapDescriptorThroughEnumerator_ReportsOrderedCameras()
    {
        // Arrange: descriptors in an unordered sequence, one characteristic of which FlashCap cannot capture
        var source = new FlashCapCameraSource(() =>
        [
            new FakeCaptureDeviceDescriptor(
                DeviceTypes.V4L2,
                "/dev/video2",
                "Side Camera",
                "Side Camera",
                new VideoCharacteristics(PixelFormats.YUYV, 640, 480, new Fraction(30, 1))),
            new FakeCaptureDeviceDescriptor(
                DeviceTypes.DirectShow,
                "path-a",
                "Desk Camera",
                "Desk Camera (DirectShow)",
                new VideoCharacteristics(PixelFormats.YUYV, 640, 480, new Fraction(30, 1)),
                new VideoCharacteristics(PixelFormats.Unknown, 3840, 2160, new Fraction(30, 1)),
                new VideoCharacteristics(PixelFormats.JPEG, 1920, 1080, new Fraction(30, 1)))
        ]);
        var enumerator = new CameraEnumerator(source);

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the backend order, the identifiers, and the format order and exclusion all apply
        Assert.Equal(["DirectShow:path-a", "V4L2:/dev/video2"], cameras.Select(camera => camera.Id));
        Assert.Equal(
            [new CameraFormat(1920, 1080, "JPEG", 30), new CameraFormat(640, 480, "YUYV", 30)],
            cameras[0].Formats);
    }

    /// <summary>
    ///     Test that FlashCap finding no device gives an empty list through the enumerator.
    /// </summary>
    [Fact]
    public void CamerasSubsystem_Enumeration_FlashCapReportsNone_ReturnsEmptyList()
    {
        // Arrange: a FlashCap source that finds no descriptor
        var enumerator = new CameraEnumerator(new FlashCapCameraSource(() => []));

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the list is empty, and that is not an error
        Assert.Empty(cameras);
    }

    /// <summary>
    ///     Test that a custom source is used instead of FlashCap.
    /// </summary>
    [Fact]
    public void CamerasSubsystem_Enumeration_CustomSource_IsUsedInsteadOfFlashCap()
    {
        // Arrange: a custom source with a camera that FlashCap could never report
        var source = new FakeCameraSource(
        [
            new CameraInfo("Custom:1", "Custom Camera", "Custom Camera", "Custom", [new CameraFormat(320, 240, "RGB24", 10)])
        ]);
        var enumerator = new CameraEnumerator(source);

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: exactly the custom camera is reported, and the custom source was asked once
        Assert.Equal("Custom:1", Assert.Single(cameras).Id);
        Assert.Equal(1, source.CallCount);
    }
}
