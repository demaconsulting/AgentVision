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
using DemaConsulting.CanvasNet.Canvas;
using DemaConsulting.CanvasNet.Codecs;

namespace DemaConsulting.AgentVision.Core.Tests.Imaging;

/// <summary>
///     Unit tests for the CameraImage unit, which also provide the evidence for the crop and encode functions of
///     the CanvasNet off-the-shelf item.
/// </summary>
/// <remarks>
///     Tests whose names contain <c>RealCanvasNet</c> are the evidence for the CanvasNet requirements. Pictures are
///     reloaded with CanvasNet's own codecs to check what was written.
/// </remarks>
public class CameraImageTests
{
    /// <summary>
    ///     Creates a lossless gradient picture of 40x30 pixels as a camera image.
    /// </summary>
    /// <returns>The image; the caller disposes it.</returns>
    private static CameraImage GradientImage()
    {
        using var source = SyntheticFrames.Gradient(40, 30);
        return CameraImage.Decode(SyntheticFrames.ToPng(source));
    }

    /// <summary>
    ///     Saves an image to a PNG stream and reloads it, to read its pixels.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <returns>The reloaded pixels; the caller disposes them.</returns>
    private static Surface Reload(CameraImage image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, ImageFileFormat.Png);
        stream.Position = 0;
        return PngCodec.Load(stream);
    }

    /// <summary>
    ///     Test that a decoded image reports the size of its picture.
    /// </summary>
    [Fact]
    public void CameraImage_Decode_Frame_ReportsSize()
    {
        // Arrange: a JPEG frame of 64x48
        var frame = SyntheticFrames.SolidJpeg(64, 48, 90);

        // Act: decode it
        using var image = CameraImage.Decode(frame);

        // Assert: the image has the size of the frame
        Assert.Equal((64, 48), (image.Width, image.Height));
    }

    /// <summary>
    ///     Test that cropping returns the pixels of the requested region.
    /// </summary>
    [Fact]
    public void CameraImage_Crop_RealCanvasNet_CopiesRegion()
    {
        // Arrange: a gradient picture, and a region away from the origin
        using var image = GradientImage();

        // Act: crop it and read the result back
        using var cropped = image.Crop(new CropRectangle(10, 5, 8, 6));
        using var pixels = Reload(cropped);

        // Assert: the size is the region's, and the pixels are those of the source at the region's position
        Assert.Equal((8, 6), (cropped.Width, cropped.Height));
        Assert.Equal(((byte)(10 * 4), (byte)(5 * 4), (byte)128), SyntheticFrames.PixelAt(pixels, 0, 0));
        Assert.Equal(((byte)(17 * 4), (byte)(10 * 4), (byte)128), SyntheticFrames.PixelAt(pixels, 7, 5));
    }

    /// <summary>
    ///     Test that a crop that touches the right and bottom edges is accepted.
    /// </summary>
    [Fact]
    public void CameraImage_Crop_ExactlyTheImage_ReturnsWholeImage()
    {
        // Arrange: a picture and a region equal to it
        using var image = GradientImage();

        // Act: crop the whole picture
        using var cropped = image.Crop(new CropRectangle(0, 0, 40, 30));

        // Assert: the full size is kept
        Assert.Equal((40, 30), (cropped.Width, cropped.Height));
    }

    /// <summary>
    ///     Test that a region that reaches past the right or bottom edge is rejected with a clear message.
    /// </summary>
    /// <param name="x">The left edge of the region.</param>
    /// <param name="y">The top edge of the region.</param>
    /// <param name="width">The width of the region.</param>
    /// <param name="height">The height of the region.</param>
    [Theory]
    [InlineData(30, 0, 11, 10)]
    [InlineData(0, 25, 10, 6)]
    [InlineData(40, 0, 1, 1)]
    public void CameraImage_Crop_OutsideImage_ThrowsWithMessage(int x, int y, int width, int height)
    {
        // Arrange: a 40x30 picture
        using var image = GradientImage();

        // Act / Assert: the region is rejected, and the message names the region and the picture size
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => image.Crop(new CropRectangle(x, y, width, height)));
        Assert.Contains($"{x},{y},{width},{height}", exception.Message);
        Assert.Contains("40x30", exception.Message);
    }

    /// <summary>
    ///     Test that a region without area or with a negative origin is rejected.
    /// </summary>
    /// <param name="x">The left edge of the region.</param>
    /// <param name="y">The top edge of the region.</param>
    /// <param name="width">The width of the region.</param>
    /// <param name="height">The height of the region.</param>
    [Theory]
    [InlineData(0, 0, 0, 10)]
    [InlineData(0, 0, 10, 0)]
    [InlineData(0, 0, -5, 10)]
    [InlineData(-1, 0, 10, 10)]
    [InlineData(0, -1, 10, 10)]
    public void CameraImage_Crop_ZeroSizeOrNegative_ThrowsArgumentOutOfRange(int x, int y, int width, int height)
    {
        // Arrange: a picture
        using var image = GradientImage();

        // Act / Assert: the region is rejected
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Crop(new CropRectangle(x, y, width, height)));
    }

    /// <summary>
    ///     Test that the crop is independent of the picture it came from.
    /// </summary>
    [Fact]
    public void CameraImage_Crop_Result_IsIndependentOfSource()
    {
        // Arrange: a picture and a crop of it
        var image = GradientImage();
        using var cropped = image.Crop(new CropRectangle(2, 2, 10, 10));

        // Act: dispose the source and use the crop
        image.Dispose();
        using var pixels = Reload(cropped);

        // Assert: the crop is still complete, and the source can no longer be used
        Assert.Equal((10, 10), (pixels.Width, pixels.Height));
        Assert.Throws<ObjectDisposedException>(() => image.Crop(new CropRectangle(0, 0, 1, 1)));
    }

    /// <summary>
    ///     Test that CanvasNet itself rejects a region outside its surface, which is the behavior the library
    ///     relies on as a second line of defense behind its own check.
    /// </summary>
    [Fact]
    public void CameraImage_Crop_RealCanvasNet_RejectsOutside()
    {
        // Arrange: a CanvasNet surface of 10x10
        using var surface = SyntheticFrames.Solid(10, 10, 1, 2, 3);

        // Act / Assert: CanvasNet rejects a region past the edge and a region without area
        Assert.Throws<ArgumentOutOfRangeException>(() => surface.Crop(5, 5, 6, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => surface.Crop(0, 0, 0, 5));
    }

    /// <summary>
    ///     Test that each supported file type is written, and read back with the same size.
    /// </summary>
    /// <param name="fileName">The name of the file to write.</param>
    [Theory]
    [InlineData("picture.png")]
    [InlineData("picture.jpg")]
    [InlineData("picture.jpeg")]
    [InlineData("picture.bmp")]
    [InlineData("PICTURE.PNG")]
    [InlineData("Picture.JpG")]
    public void CameraImage_Save_Path_RealCanvasNet_RoundTrips(string fileName)
    {
        // Arrange: a picture and a folder for the file
        using var folder = new TempDirectory();
        using var image = GradientImage();
        var path = folder.File(fileName);

        // Act: save by path, and read the file back with CanvasNet's format detection
        image.Save(path);
        using var stream = File.OpenRead(path);
        using var reloaded = ImageDecoder.Load(stream);

        // Assert: the file holds a picture of the same size
        Assert.Equal((40, 30), (reloaded.Width, reloaded.Height));
    }

    /// <summary>
    ///     Test that a PNG file keeps the exact pixels.
    /// </summary>
    [Fact]
    public void CameraImage_Save_Png_RealCanvasNet_KeepsExactPixels()
    {
        // Arrange: a gradient picture
        using var image = GradientImage();

        // Act: save it as PNG to a stream and read it back
        using var reloaded = Reload(image);

        // Assert: the pixels are exactly those of the gradient
        Assert.Equal(((byte)(7 * 4), (byte)(9 * 4), (byte)128), SyntheticFrames.PixelAt(reloaded, 7, 9));
    }

    /// <summary>
    ///     Test that each stream format writes the signature of its file type.
    /// </summary>
    /// <param name="format">The format to write.</param>
    /// <param name="first">The expected first byte of the file.</param>
    /// <param name="second">The expected second byte of the file.</param>
    [Theory]
    [InlineData(ImageFileFormat.Png, 0x89, 0x50)]
    [InlineData(ImageFileFormat.Jpeg, 0xFF, 0xD8)]
    [InlineData(ImageFileFormat.Bmp, 0x42, 0x4D)]
    public void CameraImage_Save_Stream_RealCanvasNet_WritesFormatSignature(ImageFileFormat format, int first, int second)
    {
        // Arrange: a picture and a stream
        using var image = GradientImage();
        using var stream = new MemoryStream();

        // Act: write the picture in the format
        image.Save(stream, format);

        // Assert: the stream starts with the signature of the format
        var bytes = stream.ToArray();
        Assert.Equal([first, second], [bytes[0], bytes[1]]);
    }

    /// <summary>
    ///     Test that an unsupported extension is rejected with a message that lists the supported ones.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    [Theory]
    [InlineData("picture.gif")]
    [InlineData("picture.tiff")]
    [InlineData("picture")]
    [InlineData("picture.")]
    public void CameraImage_Save_UnknownExtension_ThrowsListingSupported(string fileName)
    {
        // Arrange: a picture
        using var folder = new TempDirectory();
        using var image = GradientImage();

        // Act / Assert: the path is rejected, nothing is written, and the message lists the supported extensions
        var exception = Assert.Throws<ArgumentException>(() => image.Save(folder.File(fileName)));
        Assert.Contains(".png", exception.Message);
        Assert.Contains(".jpg", exception.Message);
        Assert.Contains(".bmp", exception.Message);
        Assert.Empty(Directory.GetFiles(folder.Path));
    }

    /// <summary>
    ///     Test that a missing folder is reported as an I/O error.
    /// </summary>
    [Fact]
    public void CameraImage_Save_MissingFolder_ThrowsDirectoryNotFound()
    {
        // Arrange: a path in a folder that does not exist
        using var folder = new TempDirectory();
        using var image = GradientImage();

        // Act / Assert: the save fails with a directory error
        Assert.Throws<DirectoryNotFoundException>(() => image.Save(folder.File(Path.Combine("missing", "a.png"))));
    }

    /// <summary>
    ///     Test that null arguments are rejected.
    /// </summary>
    [Fact]
    public void CameraImage_Save_NullArguments_ThrowsArgumentNullException()
    {
        // Arrange: a picture
        using var image = GradientImage();

        // Act / Assert: a null path and a null stream are rejected
        Assert.Throws<ArgumentNullException>(() => image.Save((string)null!));
        Assert.Throws<ArgumentNullException>(() => image.Save((Stream)null!, ImageFileFormat.Png));
    }

    /// <summary>
    ///     Test that an undefined format value is rejected.
    /// </summary>
    [Fact]
    public void CameraImage_Save_UndefinedFormat_ThrowsArgumentOutOfRange()
    {
        // Arrange: a picture and a stream
        using var image = GradientImage();
        using var stream = new MemoryStream();

        // Act / Assert: a format that does not exist is rejected
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Save(stream, (ImageFileFormat)99));
    }

    /// <summary>
    ///     Test that the extension check reports the format of each supported extension.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="expected">Whether the path has a supported format.</param>
    [Theory]
    [InlineData("a.png", true)]
    [InlineData("dir/a.JPG", true)]
    [InlineData("a.jpeg", true)]
    [InlineData("a.bmp", true)]
    [InlineData("a.webp", false)]
    [InlineData("png", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void CameraImage_TryGetFormat_Path_ReportsSupport(string? path, bool expected)
    {
        // Arrange: no setup; the path is the input

        // Act: ask for the format
        var result = CameraImage.TryGetFormat(path, out _);

        // Assert: only the supported extensions have a format
        Assert.Equal(expected, result);
    }

    /// <summary>
    ///     Test that an image can no longer be used after it is disposed, and that disposing twice is harmless.
    /// </summary>
    [Fact]
    public void CameraImage_Dispose_Twice_IsHarmlessAndImageIsUnusable()
    {
        // Arrange: a picture
        var image = GradientImage();

        // Act: dispose it twice
        image.Dispose();
        var exception = Record.Exception(image.Dispose);

        // Assert: the second dispose does nothing and use fails clearly
        Assert.Null(exception);
        Assert.Throws<ObjectDisposedException>(() => image.Save(new MemoryStream(), ImageFileFormat.Png));
    }
}
