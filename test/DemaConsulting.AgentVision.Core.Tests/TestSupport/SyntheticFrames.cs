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
using DemaConsulting.CanvasNet.Canvas;
using DemaConsulting.CanvasNet.Codecs;

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     Builds synthetic camera frames: real encoded JPEG, PNG, and BMP images that a camera could deliver,
///     generated without a camera.
/// </summary>
/// <remarks>
///     JPEG and PNG frames are written with CanvasNet; BMP frames are written by hand in the layout that FlashCap
///     produces (a file header, a 40-byte info header, and uncompressed pixels, bottom-up or top-down), because the
///     decoder must handle exactly that layout.
/// </remarks>
internal static class SyntheticFrames
{
    /// <summary>
    ///     The first byte of every JPEG marker.
    /// </summary>
    private const byte MarkerPrefix = 0xFF;

    /// <summary>
    ///     Creates a surface of one color.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="red">The red value.</param>
    /// <param name="green">The green value.</param>
    /// <param name="blue">The blue value.</param>
    /// <returns>The surface; the caller disposes it.</returns>
    public static Surface Solid(int width, int height, byte red, byte green, byte blue)
    {
        var surface = new Surface(width, height);
        surface.Clear(new Rgba32(red, green, blue, 255));
        return surface;
    }

    /// <summary>
    ///     Creates a surface whose pixel at (x, y) is (x * 4, y * 4, 128), so that every position is different
    ///     and a crop can be checked pixel by pixel.
    /// </summary>
    /// <param name="width">The width in pixels; at most 64 so that the red value stays a byte.</param>
    /// <param name="height">The height in pixels; at most 64.</param>
    /// <returns>The surface; the caller disposes it.</returns>
    public static Surface Gradient(int width, int height)
    {
        var surface = new Surface(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                surface[x, y] = new Rgba32((byte)(x * 4), (byte)(y * 4), 128, 255);
            }
        }

        return surface;
    }

    /// <summary>
    ///     Encodes a surface as a JPEG file.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <returns>The bytes of the JPEG file.</returns>
    public static byte[] ToJpeg(Surface surface)
    {
        using var stream = new MemoryStream();
        JpegCodec.Save(surface, stream, 95);
        return stream.ToArray();
    }

    /// <summary>
    ///     Encodes a surface as a PNG file.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <returns>The bytes of the PNG file.</returns>
    public static byte[] ToPng(Surface surface)
    {
        using var stream = new MemoryStream();
        PngCodec.Save(surface, stream);
        return stream.ToArray();
    }

    /// <summary>
    ///     Creates a JPEG frame of one color.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="level">The gray level (red, green, and blue).</param>
    /// <returns>The bytes of the JPEG file.</returns>
    public static byte[] SolidJpeg(int width, int height, byte level)
    {
        using var surface = Solid(width, height, level, level, level);
        return ToJpeg(surface);
    }

    /// <summary>
    ///     Creates a JPEG frame of one color, wrapped as an encoded frame.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="level">The gray level.</param>
    /// <param name="frameIndex">The frame number.</param>
    /// <returns>The frame.</returns>
    public static EncodedFrame SolidFrame(int width, int height, byte level, long frameIndex) =>
        new(SolidJpeg(width, height, level), TimeSpan.FromMilliseconds(frameIndex * 33), frameIndex);

    /// <summary>
    ///     Wraps arbitrary bytes as an encoded frame, for tests that never decode the frame.
    /// </summary>
    /// <param name="frameIndex">The frame number, which is also the single byte of data.</param>
    /// <returns>The frame.</returns>
    public static EncodedFrame OpaqueFrame(long frameIndex) =>
        new([(byte)frameIndex], TimeSpan.FromMilliseconds(frameIndex * 33), frameIndex);

    /// <summary>
    ///     Builds a BMP file with the pixel colors that a function gives.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="bitsPerPixel">24 or 32.</param>
    /// <param name="topDown">Whether the rows are stored top-down (negative height in the header).</param>
    /// <param name="color">Gives the (red, green, blue) of the pixel at (x, y).</param>
    /// <param name="alpha">The value of the fourth byte of a 32-bit pixel; cameras often write zero.</param>
    /// <returns>The bytes of the BMP file.</returns>
    public static byte[] Bmp(
        int width,
        int height,
        int bitsPerPixel,
        bool topDown,
        Func<int, int, (byte Red, byte Green, byte Blue)> color,
        byte alpha = 255)
    {
        // Rows are padded to a multiple of four bytes
        var bytesPerPixel = bitsPerPixel / 8;
        var stride = ((width * bytesPerPixel) + 3) / 4 * 4;
        var pixelBytes = stride * height;
        var data = new byte[14 + 40 + pixelBytes];

        // File header: signature, file size, pixel data offset
        data[0] = (byte)'B';
        data[1] = (byte)'M';
        BitConverter.GetBytes(data.Length).CopyTo(data, 2);
        BitConverter.GetBytes(14 + 40).CopyTo(data, 10);

        // Info header: size, width, height (negative when top-down), planes, bits per pixel, BI_RGB
        BitConverter.GetBytes(40).CopyTo(data, 14);
        BitConverter.GetBytes(width).CopyTo(data, 18);
        BitConverter.GetBytes(topDown ? -height : height).CopyTo(data, 22);
        BitConverter.GetBytes((short)1).CopyTo(data, 26);
        BitConverter.GetBytes((short)bitsPerPixel).CopyTo(data, 28);
        BitConverter.GetBytes(pixelBytes).CopyTo(data, 34);

        // Pixels are stored as blue, green, red (and a padding or alpha byte for 32 bits)
        for (var y = 0; y < height; y++)
        {
            var row = topDown ? y : height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var (red, green, blue) = color(x, y);
                var offset = 54 + (row * stride) + (x * bytesPerPixel);
                data[offset] = blue;
                data[offset + 1] = green;
                data[offset + 2] = red;
                if (bytesPerPixel == 4)
                {
                    data[offset + 3] = alpha;
                }
            }
        }

        return data;
    }

    /// <summary>
    ///     Removes the Huffman table segments of a JPEG, as many webcams do in their motion JPEG frames.
    /// </summary>
    /// <param name="jpeg">The bytes of a JPEG file.</param>
    /// <returns>A copy without any DHT segment.</returns>
    public static byte[] StripHuffmanTables(byte[] jpeg)
    {
        var result = new List<byte> { jpeg[0], jpeg[1] };
        var position = 2;
        while (position < jpeg.Length)
        {
            // Everything from the start of scan on is entropy-coded data and is copied as it is
            var marker = jpeg[position + 1];
            if (jpeg[position] != MarkerPrefix || marker == 0xDA)
            {
                result.AddRange(jpeg.Skip(position));
                break;
            }

            var length = (jpeg[position + 2] << 8) | jpeg[position + 3];
            if (marker != 0xC4)
            {
                result.AddRange(jpeg.Skip(position).Take(2 + length));
            }

            position += 2 + length;
        }

        return result.ToArray();
    }

    /// <summary>
    ///     Reads the pixel of a decoded image.
    /// </summary>
    /// <param name="surface">The decoded pixels.</param>
    /// <param name="x">The column.</param>
    /// <param name="y">The row.</param>
    /// <returns>The red, green, and blue values.</returns>
    public static (byte Red, byte Green, byte Blue) PixelAt(Surface surface, int x, int y)
    {
        var pixel = surface[x, y];
        return (pixel.R, pixel.G, pixel.B);
    }
}
