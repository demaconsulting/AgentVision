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

using DemaConsulting.CanvasNet.Canvas;
using DemaConsulting.CanvasNet.Codecs;

namespace DemaConsulting.AgentVision.Core.Imaging;

/// <summary>
///     Decodes the image file bytes of a camera frame (JPEG, PNG, or BMP) into pixels, and repairs the defects
///     that webcam frames commonly have.
/// </summary>
/// <remarks>
///     <para>
///         The unit exists so that the decoding policy lives in one place: which bytes are accepted, how a
///         frame with a known defect is repaired (a JPEG without Huffman tables, a top-down bitmap, or a 32-bit
///         bitmap whose unused byte is zero), and how failures are reported. The decoding itself is done by
///         CanvasNet, the only imaging library the product uses.
///     </para>
///     <para>
///         The type is stateless and thread-safe. It never keeps a reference to the frame it is given.
///     </para>
/// </remarks>
internal static class FrameDecoder
{
    /// <summary>
    ///     The first byte of every JPEG marker.
    /// </summary>
    private const byte MarkerPrefix = 0xFF;

    /// <summary>
    ///     The second byte of the start-of-image marker, which starts every JPEG file.
    /// </summary>
    private const byte SoiMarker = 0xD8;

    /// <summary>
    ///     The second byte of the start-of-scan marker, after which the entropy-coded data begins.
    /// </summary>
    private const byte SosMarker = 0xDA;

    /// <summary>
    ///     The second byte of the define-Huffman-table marker.
    /// </summary>
    private const byte DhtMarker = 0xC4;

    /// <summary>
    ///     The second byte of the temporary marker, which has no length field.
    /// </summary>
    private const byte TemMarker = 0x01;

    /// <summary>
    ///     The first of the eight restart markers, which have no length field.
    /// </summary>
    private const byte FirstRestartMarker = 0xD0;

    /// <summary>
    ///     The last of the eight restart markers, which have no length field.
    /// </summary>
    private const byte LastRestartMarker = 0xD7;

    /// <summary>
    ///     The number of bytes of a segment length field, which counts itself.
    /// </summary>
    private const int LengthFieldSize = 2;

    /// <summary>
    ///     The smallest number of bytes in which any supported image signature can be recognized.
    /// </summary>
    private const int MinimumFrameLength = 4;

    /// <summary>
    ///     The size of a bitmap file header plus a standard 40-byte info header.
    /// </summary>
    private const int BmpHeaderSize = 54;

    /// <summary>
    ///     Decodes one camera frame into a pixel surface.
    /// </summary>
    /// <param name="encoded">The complete bytes of a JPEG, PNG, or BMP image file.</param>
    /// <returns>The decoded pixels. The caller owns the surface and must dispose it.</returns>
    /// <exception cref="InvalidDataException">
    ///     Thrown when the bytes are empty, are not a recognized image, or cannot be decoded (for example a
    ///     truncated frame). The message says what is wrong.
    /// </exception>
    /// <remarks>
    ///     A JPEG frame that has no Huffman tables is repaired first, see <see cref="EnsureHuffmanTables"/>.
    ///     Failures of the decoder are reported with the same exception type, wrapped with a message that
    ///     says a camera frame was the input, so a caller handles one kind of failure.
    /// </remarks>
    public static Surface Decode(ReadOnlyMemory<byte> encoded)
    {
        // Reject input that cannot be an image before the decoder sees it, with a message that is specific
        if (encoded.Length < MinimumFrameLength)
        {
            throw new InvalidDataException(
                $"The camera frame is too short to be an image ({encoded.Length} bytes).");
        }

        // Repair a JPEG that omits the Huffman tables, and make a bitmap readable (top-down rows, a fourth byte
        // that is zero); other frames need no repair
        var repaired = NormalizeBitmap(EnsureHuffmanTables(encoded));

        try
        {
            return ImageDecoder.Load(repaired.Span);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"The camera frame could not be decoded: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Returns the frame with the standard Huffman tables inserted when it is a JPEG that has none.
    /// </summary>
    /// <param name="encoded">The bytes of the frame.</param>
    /// <returns>
    ///     The original memory when the frame is not a JPEG, already defines Huffman tables, or cannot be
    ///     scanned; otherwise a new array with the tables inserted directly before the start of scan.
    /// </returns>
    /// <remarks>
    ///     The scan walks the marker segments from the start of the file up to the start of scan, which is
    ///     where the tables must be defined. A malformed segment structure is not an error here: the frame is
    ///     returned unchanged and the decoder reports the problem, so the error message comes from one place.
    /// </remarks>
    internal static ReadOnlyMemory<byte> EnsureHuffmanTables(ReadOnlyMemory<byte> encoded)
    {
        var data = encoded.Span;

        // Only a JPEG (start-of-image marker) can need the tables
        if (data.Length < MinimumFrameLength || data[0] != MarkerPrefix || data[1] != SoiMarker)
        {
            return encoded;
        }

        // Walk the segments after the start-of-image marker until the start of scan
        var position = LengthFieldSize;
        while (position + 1 < data.Length)
        {
            // Every segment starts with a marker prefix; fill bytes (repeated prefixes) may precede the marker
            if (data[position] != MarkerPrefix)
            {
                return encoded;
            }

            while (position + 1 < data.Length && data[position + 1] == MarkerPrefix)
            {
                position++;
            }

            if (position + 1 >= data.Length)
            {
                return encoded;
            }

            var marker = data[position + 1];

            // A Huffman table is defined, so nothing needs repair
            if (marker == DhtMarker)
            {
                return encoded;
            }

            // The start of scan without any table before it: insert the standard tables here
            if (marker == SosMarker)
            {
                return Insert(encoded, position);
            }

            // Markers without a length field are skipped; all others carry a length that includes itself
            if (marker == TemMarker || marker is >= FirstRestartMarker and <= LastRestartMarker)
            {
                position += LengthFieldSize;
                continue;
            }

            if (position + 3 >= data.Length)
            {
                return encoded;
            }

            var length = (data[position + 2] << 8) | data[position + 3];
            if (length < LengthFieldSize)
            {
                return encoded;
            }

            position += LengthFieldSize + length;
        }

        // No start of scan was found; the decoder will report the truncated frame
        return encoded;
    }

    /// <summary>
    ///     Returns the frame as a bitmap that the imaging library reads correctly, when it is an uncompressed
    ///     bitmap that needs it.
    /// </summary>
    /// <param name="encoded">The bytes of the frame.</param>
    /// <returns>
    ///     The original memory when the frame is not a bitmap, is not an uncompressed 24-bit or 32-bit bitmap, is
    ///     too short for its header or pixels, or needs no change (a bottom-up 24-bit bitmap); otherwise a new
    ///     array: a top-down bitmap gets its rows in reverse order and a positive height, and a 32-bit bitmap gets
    ///     an opaque fourth byte in every pixel.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         A bitmap with a negative height stores its rows top-down. Some capture backends deliver such frames
    ///         (raw frames of Video4Linux are top-down), and the imaging library reads only the bottom-up layout.
    ///     </para>
    ///     <para>
    ///         The fourth byte of a 32-bit uncompressed bitmap pixel is unused, and cameras commonly write zero
    ///         there; the imaging library reads it as transparency, which would make the whole picture
    ///         transparent, so it is set to opaque.
    ///     </para>
    ///     <para>
    ///         A bitmap that is malformed is returned unchanged and the decoder reports it.
    ///     </para>
    /// </remarks>
    internal static ReadOnlyMemory<byte> NormalizeBitmap(ReadOnlyMemory<byte> encoded)
    {
        var data = encoded.Span;

        // Only an uncompressed 24-bit or 32-bit bitmap with a standard header is handled
        if (data.Length < BmpHeaderSize || data[0] != (byte)'B' || data[1] != (byte)'M')
        {
            return encoded;
        }

        var pixelOffset = BitConverter.ToInt32(data[10..14]);
        var width = BitConverter.ToInt32(data[18..22]);
        var height = BitConverter.ToInt32(data[22..26]);
        var bitsPerPixel = BitConverter.ToInt16(data[28..30]);
        var compression = BitConverter.ToInt32(data[30..34]);
        if (compression != 0 || bitsPerPixel is not (24 or 32) || width <= 0 || height == 0
            || pixelOffset < BmpHeaderSize)
        {
            return encoded;
        }

        // A bottom-up 24-bit bitmap is read correctly as it is
        var topDown = height < 0;
        if (!topDown && bitsPerPixel == 24)
        {
            return encoded;
        }

        // Each row is padded to a multiple of four bytes; check that all rows are present before copying
        var rows = Math.Abs((long)height);
        var stride = ((((long)width * bitsPerPixel) + 31) / 32) * 4;
        if (pixelOffset + (rows * stride) > data.Length)
        {
            return encoded;
        }

        // Copy the frame; a top-down bitmap gets a positive height and its rows in reverse order
        var result = encoded.ToArray();
        if (topDown)
        {
            BitConverter.TryWriteBytes(result.AsSpan(22, 4), (int)rows);
            for (var row = 0; row < rows; row++)
            {
                data.Slice(pixelOffset + (int)(row * stride), (int)stride)
                    .CopyTo(result.AsSpan(pixelOffset + (int)((rows - 1 - row) * stride), (int)stride));
            }
        }

        // A 32-bit bitmap gets an opaque fourth byte in every pixel, including any row padding (there is none)
        if (bitsPerPixel == 32)
        {
            var end = pixelOffset + (int)(rows * stride);
            for (var index = pixelOffset + 3; index < end; index += 4)
            {
                result[index] = 0xFF;
            }
        }

        return result;
    }

    /// <summary>
    ///     Builds a copy of the frame with the standard Huffman tables inserted at a position.
    /// </summary>
    /// <param name="encoded">The bytes of the frame.</param>
    /// <param name="position">The index of the marker prefix of the start of scan.</param>
    /// <returns>A new array: the bytes before the position, the tables, and the remaining bytes.</returns>
    private static ReadOnlyMemory<byte> Insert(ReadOnlyMemory<byte> encoded, int position)
    {
        var tables = StandardJpegTables.Segment;
        var result = new byte[encoded.Length + tables.Length];
        encoded[..position].CopyTo(result);
        tables.CopyTo(result.AsMemory(position));
        encoded[position..].CopyTo(result.AsMemory(position + tables.Length));
        return result;
    }
}
