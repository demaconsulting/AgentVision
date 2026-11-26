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

namespace DemaConsulting.AgentVision.Core.Tests.Imaging;

/// <summary>
///     Unit tests for the FrameDecoder unit (including its standard JPEG tables), which also provide the
///     evidence for the decoding functions of the CanvasNet off-the-shelf item.
/// </summary>
/// <remarks>
///     The frames are synthetic but real: JPEG and PNG files written by CanvasNet and BMP files written in the
///     layout FlashCap produces, so the tests run the real decoder without a camera. Tests whose names contain
///     <c>RealCanvasNet</c> are the evidence for the CanvasNet requirements.
/// </remarks>
public class FrameDecoderTests
{
    /// <summary>
    ///     Gives the color of the pixel at (x, y) of the gradient pictures: red grows with x, green with y, and
    ///     blue is constant, so every position has its own color.
    /// </summary>
    private static (byte Red, byte Green, byte Blue) GradientColor(int x, int y) => ((byte)(x * 4), (byte)(y * 4), 128);

    /// <summary>
    ///     Test that a JPEG frame decodes to a surface of the frame's size and color.
    /// </summary>
    [Fact]
    public void FrameDecoder_Decode_Jpeg_RealCanvasNet_ReturnsSurfaceOfFrameSize()
    {
        // Arrange: a 64x48 mid-gray JPEG frame
        var frame = SyntheticFrames.SolidJpeg(64, 48, 150);

        // Act: decode it
        using var surface = FrameDecoder.Decode(frame);

        // Assert: the size is the frame's, and the lossy color is close to the original
        Assert.Equal(64, surface.Width);
        Assert.Equal(48, surface.Height);
        var (red, green, blue) = SyntheticFrames.PixelAt(surface, 32, 24);
        Assert.InRange(red, 145, 155);
        Assert.InRange(green, 145, 155);
        Assert.InRange(blue, 145, 155);
    }

    /// <summary>
    ///     Test that a PNG frame decodes to exactly the pixels that were written.
    /// </summary>
    [Fact]
    public void FrameDecoder_Decode_Png_RealCanvasNet_ReturnsExactPixels()
    {
        // Arrange: a PNG of a gradient, so every position has a different color
        using var source = SyntheticFrames.Gradient(40, 30);
        var frame = SyntheticFrames.ToPng(source);

        // Act: decode it
        using var surface = FrameDecoder.Decode(frame);

        // Assert: size and the pixels at three positions are exact, since PNG is lossless
        Assert.Equal((40, 30), (surface.Width, surface.Height));
        Assert.Equal(GradientColor(0, 0), SyntheticFrames.PixelAt(surface, 0, 0));
        Assert.Equal(GradientColor(5, 7), SyntheticFrames.PixelAt(surface, 5, 7));
        Assert.Equal(GradientColor(39, 29), SyntheticFrames.PixelAt(surface, 39, 29));
    }

    /// <summary>
    ///     Test that the BMP layouts that FlashCap produces all decode with the right pixel at each position.
    /// </summary>
    /// <param name="bitsPerPixel">The bit depth of the BMP.</param>
    /// <param name="topDown">Whether the rows are stored top-down.</param>
    [Theory]
    [InlineData(24, false)]
    [InlineData(24, true)]
    [InlineData(32, false)]
    [InlineData(32, true)]
    public void FrameDecoder_Decode_Bmp_RealCanvasNet_ReturnsPixelsInPlace(int bitsPerPixel, bool topDown)
    {
        // Arrange: a 21-pixel-wide BMP (a width whose rows need padding at 24 bits) of a gradient
        var frame = SyntheticFrames.Bmp(21, 17, bitsPerPixel, topDown, GradientColor);

        // Act: decode it
        using var surface = FrameDecoder.Decode(frame);

        // Assert: the size is right and a pixel off the origin is where it was written, whatever the row order
        Assert.Equal((21, 17), (surface.Width, surface.Height));
        Assert.Equal(GradientColor(5, 7), SyntheticFrames.PixelAt(surface, 5, 7));
        Assert.Equal(GradientColor(20, 16), SyntheticFrames.PixelAt(surface, 20, 16));
    }

    /// <summary>
    ///     Test that a 32-bit bitmap whose unused fourth byte is zero, as cameras write it, decodes to opaque
    ///     pixels, so that the picture is not saved transparent.
    /// </summary>
    [Fact]
    public void FrameDecoder_Decode_Bmp32ZeroFourthByte_RealCanvasNet_ReturnsOpaquePixels()
    {
        // Arrange: a 32-bit bitmap whose fourth byte of every pixel is zero
        var frame = SyntheticFrames.Bmp(8, 8, 32, false, GradientColor, alpha: 0);

        // Act: decode it
        using var surface = FrameDecoder.Decode(frame);

        // Assert: the color is kept and the pixel is opaque
        Assert.Equal(GradientColor(3, 2), SyntheticFrames.PixelAt(surface, 3, 2));
        Assert.Equal(255, surface[3, 2].A);
    }

    /// <summary>
    ///     Test that a JPEG frame without Huffman tables decodes to the same pixels as the original.
    /// </summary>
    [Fact]
    public void FrameDecoder_Decode_JpegWithoutDht_DecodesLikeOriginal()
    {
        // Arrange: a color JPEG, and the same bytes with every Huffman table segment removed
        using var source = SyntheticFrames.Gradient(48, 32);
        var original = SyntheticFrames.ToJpeg(source);
        var stripped = SyntheticFrames.StripHuffmanTables(original);
        Assert.True(stripped.Length < original.Length, "the stripped frame must be shorter");

        // Act: decode both
        using var expected = FrameDecoder.Decode(original);
        using var repaired = FrameDecoder.Decode(stripped);

        // Assert: the repaired frame decodes, to the same size and the same pixels
        Assert.Equal((expected.Width, expected.Height), (repaired.Width, repaired.Height));
        Assert.Equal(SyntheticFrames.PixelAt(expected, 10, 10), SyntheticFrames.PixelAt(repaired, 10, 10));
        Assert.Equal(SyntheticFrames.PixelAt(expected, 40, 25), SyntheticFrames.PixelAt(repaired, 40, 25));
    }

    /// <summary>
    ///     Test that a JPEG that already has Huffman tables is passed on unchanged.
    /// </summary>
    [Fact]
    public void FrameDecoder_EnsureHuffmanTables_JpegWithTables_ReturnsInputUnchanged()
    {
        // Arrange: a JPEG as CanvasNet writes it, with its own tables
        var original = SyntheticFrames.SolidJpeg(16, 16, 100);

        // Act: ask for the tables to be ensured
        var result = FrameDecoder.EnsureHuffmanTables(original);

        // Assert: the bytes are the same, so no table was inserted twice
        Assert.True(result.Span.SequenceEqual(original));
    }

    /// <summary>
    ///     Test that data that is not a JPEG is passed on unchanged.
    /// </summary>
    [Fact]
    public void FrameDecoder_EnsureHuffmanTables_NotJpeg_ReturnsInputUnchanged()
    {
        // Arrange: a PNG frame
        using var source = SyntheticFrames.Solid(8, 8, 1, 2, 3);
        var original = SyntheticFrames.ToPng(source);

        // Act: ask for the tables to be ensured
        var result = FrameDecoder.EnsureHuffmanTables(original);

        // Assert: the bytes are the same
        Assert.True(result.Span.SequenceEqual(original));
    }

    /// <summary>
    ///     Test that the standard tables that are inserted are the tables that CanvasNet's own encoder writes,
    ///     which is why the repaired frames decode to what the camera sent.
    /// </summary>
    [Fact]
    public void StandardJpegTables_Segment_MatchesCanvasNetEncoderTables()
    {
        // Arrange: the tables of a color JPEG that CanvasNet wrote, and the tables of the standard segment
        using var source = SyntheticFrames.Gradient(16, 16);
        var encoderTables = ReadTables(SyntheticFrames.ToJpeg(source));
        var standard = StandardJpegTables.Segment.ToArray();

        // Act: read the tables of the standard segment, which is one DHT segment with four tables
        var standardTables = ReadTables([0xFF, 0xD8, .. standard, 0xFF, 0xDA]);

        // Assert: every table class and identifier that the encoder writes is in the standard segment, identical
        Assert.NotEmpty(encoderTables);
        Assert.Equal(4, standardTables.Count);
        foreach (var (key, table) in encoderTables)
        {
            Assert.Equal(table, standardTables[key]);
        }
    }

    /// <summary>
    ///     Test that bytes that are not an image are reported as invalid data.
    /// </summary>
    [Fact]
    public void FrameDecoder_Decode_Garbage_RealCanvasNet_ThrowsInvalidData()
    {
        // Arrange: bytes that are no image
        var garbage = Enumerable.Range(0, 200).Select(value => (byte)(value * 7)).ToArray();

        // Act / Assert: decoding reports invalid data
        var exception = Assert.Throws<InvalidDataException>(() => FrameDecoder.Decode(garbage));
        Assert.Contains("camera frame", exception.Message);
    }

    /// <summary>
    ///     Test that an empty frame is reported as invalid data.
    /// </summary>
    [Fact]
    public void FrameDecoder_Decode_Empty_ThrowsInvalidData()
    {
        // Arrange: no bytes

        // Act / Assert: decoding reports invalid data and says the frame is too short
        var exception = Assert.Throws<InvalidDataException>(() => FrameDecoder.Decode(Array.Empty<byte>()));
        Assert.Contains("too short", exception.Message);
    }

    /// <summary>
    ///     Test that a frame that was cut short is reported as invalid data.
    /// </summary>
    /// <param name="format">Which frame to cut: jpeg or png.</param>
    [Theory]
    [InlineData("jpeg")]
    [InlineData("png")]
    public void FrameDecoder_Decode_Truncated_ThrowsInvalidData(string format)
    {
        // Arrange: a frame cut to half its length, as a dropped USB packet would leave it
        using var source = SyntheticFrames.Gradient(48, 32);
        var whole = format == "jpeg" ? SyntheticFrames.ToJpeg(source) : SyntheticFrames.ToPng(source);
        var truncated = whole.Take(whole.Length / 2).ToArray();

        // Act / Assert: decoding reports invalid data
        Assert.Throws<InvalidDataException>(() => FrameDecoder.Decode(truncated));
    }

    /// <summary>
    ///     Reads the Huffman tables of a JPEG.
    /// </summary>
    /// <param name="jpeg">The bytes of a JPEG; the walk ends at the start of scan.</param>
    /// <returns>The tables by class and identifier byte; each value is the 16 counts followed by the symbols.</returns>
    private static Dictionary<int, byte[]> ReadTables(byte[] jpeg)
    {
        var tables = new Dictionary<int, byte[]>();
        var position = 2;
        while (position + 3 < jpeg.Length && jpeg[position + 1] != 0xDA)
        {
            var length = (jpeg[position + 2] << 8) | jpeg[position + 3];
            if (jpeg[position + 1] == 0xC4)
            {
                // A DHT segment holds one or more tables: class and identifier, 16 counts, then the symbols
                var cursor = position + 4;
                var end = position + 2 + length;
                while (cursor < end)
                {
                    var key = jpeg[cursor];
                    var counts = jpeg.AsSpan(cursor + 1, 16);
                    var symbolCount = 0;
                    foreach (var count in counts)
                    {
                        symbolCount += count;
                    }

                    tables[key] = jpeg.AsSpan(cursor + 1, 16 + symbolCount).ToArray();
                    cursor += 1 + 16 + symbolCount;
                }
            }

            position += 2 + length;
        }

        return tables;
    }
}
