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

using DemaConsulting.AgentVision.Core.Imaging;
using DemaConsulting.AgentVision.Core.Tests.TestSupport;
using DemaConsulting.CanvasNet.Codecs;

namespace DemaConsulting.AgentVision.Core.Tests.Imaging;

/// <summary>
///     Subsystem tests for the Imaging subsystem: the frame decoder working with the camera image.
/// </summary>
public class ImagingSubsystemTests
{
    /// <summary>
    ///     Test that frames in every format a camera delivers become images of the right size.
    /// </summary>
    [Fact]
    public void ImagingSubsystem_Decode_JpegBmpPngFrames_ProduceImages()
    {
        // Arrange: a JPEG, a PNG, a 24-bit BMP, and a 32-bit BMP frame of the same 32x24 picture
        using var source = SyntheticFrames.Gradient(32, 24);
        byte[][] frames =
        [
            SyntheticFrames.ToJpeg(source),
            SyntheticFrames.ToPng(source),
            SyntheticFrames.Bmp(32, 24, 24, false, (x, y) => ((byte)(x * 4), (byte)(y * 4), 128)),
            SyntheticFrames.Bmp(32, 24, 32, true, (x, y) => ((byte)(x * 4), (byte)(y * 4), 128))
        ];

        // Act: decode every frame
        var sizes = frames.Select(frame =>
        {
            using var image = CameraImage.Decode(frame);
            return (image.Width, image.Height);
        }).ToList();

        // Assert: every frame has the size of the picture
        Assert.All(sizes, size => Assert.Equal((32, 24), size));
    }

    /// <summary>
    ///     Test that a JPEG frame without Huffman tables, as webcams send them, still becomes an image.
    /// </summary>
    [Fact]
    public void ImagingSubsystem_Decode_JpegWithoutHuffmanTables_ProducesImage()
    {
        // Arrange: a JPEG frame without tables
        var frame = SyntheticFrames.StripHuffmanTables(SyntheticFrames.SolidJpeg(48, 32, 120));

        // Act: decode it
        using var image = CameraImage.Decode(frame);

        // Assert: the picture has its size
        Assert.Equal((48, 32), (image.Width, image.Height));
    }

    /// <summary>
    ///     Test that a decoded frame can be cropped to a region.
    /// </summary>
    [Fact]
    public void ImagingSubsystem_Crop_Region_ProducesCroppedImage()
    {
        // Arrange: a decoded frame
        using var image = CameraImage.Decode(SyntheticFrames.SolidJpeg(64, 48, 100));

        // Act: crop a region
        using var cropped = image.Crop(new CropRectangle(8, 4, 20, 10));

        // Assert: the crop has the size of the region
        Assert.Equal((20, 10), (cropped.Width, cropped.Height));
    }

    /// <summary>
    ///     Test that a decoded and cropped frame is saved as PNG and as JPEG files that can be loaded again.
    /// </summary>
    [Fact]
    public void ImagingSubsystem_Save_PngAndJpeg_CanBeReloaded()
    {
        // Arrange: a decoded, cropped frame and a folder
        using var folder = new TempDirectory();
        using var image = CameraImage.Decode(SyntheticFrames.SolidJpeg(64, 48, 100));
        using var cropped = image.Crop(new CropRectangle(8, 4, 20, 10));

        // Act: save it twice, choosing the format by extension, and load both files
        cropped.Save(folder.File("crop.png"));
        cropped.Save(folder.File("crop.jpg"));
        using var png = PngCodec.Load(folder.File("crop.png"));
        using var jpeg = JpegCodec.Load(folder.File("crop.jpg"));

        // Assert: both files hold the cropped picture
        Assert.Equal((20, 10), (png.Width, png.Height));
        Assert.Equal((20, 10), (jpeg.Width, jpeg.Height));
    }
}
