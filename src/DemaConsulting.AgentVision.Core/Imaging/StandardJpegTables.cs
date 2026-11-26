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

namespace DemaConsulting.AgentVision.Core.Imaging;

/// <summary>
///     The standard JPEG Huffman tables of ITU-T T.81 Annex K, packaged as one DHT (define Huffman table)
///     segment.
/// </summary>
/// <remarks>
///     <para>
///         Many webcams stream "motion JPEG" frames that leave out the DHT segments, because the format of
///         that stream defines the standard tables as implied. A frame without tables cannot be decoded by a
///         decoder that follows the JPEG file rules, so <see cref="FrameDecoder"/> inserts this segment into
///         such frames. Frames from these cameras are encoded with the Annex K tables, so the repaired frame
///         decodes to the pixels the camera sent.
///     </para>
///     <para>
///         The tables are stored in the form the file uses: for each table, 16 counts (the number of codes of
///         each length from 1 to 16 bits) followed by the symbols in code order. The type holds only constant
///         data and is thread-safe.
///     </para>
/// </remarks>
internal static class StandardJpegTables
{
    /// <summary>
    ///     The first byte of every JPEG marker.
    /// </summary>
    private const byte MarkerPrefix = 0xFF;

    /// <summary>
    ///     The second byte of the define-Huffman-table marker.
    /// </summary>
    private const byte DhtMarker = 0xC4;

    /// <summary>
    ///     The number of bytes of a segment length field, which counts itself.
    /// </summary>
    private const int LengthFieldSize = 2;

    /// <summary>
    ///     The number of code-length counts that start every table.
    /// </summary>
    private const int CountsPerTable = 16;

    /// <summary>
    ///     Table class and identifier byte of the luminance DC table (class 0, identifier 0).
    /// </summary>
    private const byte DcLuminanceId = 0x00;

    /// <summary>
    ///     Table class and identifier byte of the luminance AC table (class 1, identifier 0).
    /// </summary>
    private const byte AcLuminanceId = 0x10;

    /// <summary>
    ///     Table class and identifier byte of the chrominance DC table (class 0, identifier 1).
    /// </summary>
    private const byte DcChrominanceId = 0x01;

    /// <summary>
    ///     Table class and identifier byte of the chrominance AC table (class 1, identifier 1).
    /// </summary>
    private const byte AcChrominanceId = 0x11;

    /// <summary>
    ///     Gets the DHT segment, starting with its marker bytes, ready to be inserted before the start of
    ///     scan of a frame that has no Huffman tables.
    /// </summary>
    public static ReadOnlyMemory<byte> Segment => SegmentBytes;

    /// <summary>
    ///     Assembles the segment from the four table definitions.
    /// </summary>
    /// <returns>The segment bytes: <c>FF C4</c>, the big-endian length, and the four tables.</returns>
    private static byte[] BuildSegment()
    {
        // Concatenate the tables in the conventional order: luminance DC, luminance AC, chrominance DC,
        // chrominance AC
        var payload = new List<byte>();
        AppendTable(payload, DcLuminanceId, [0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0], DcSymbols);
        AppendTable(payload, AcLuminanceId, [0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7D], AcLuminanceSymbols);
        AppendTable(payload, DcChrominanceId, [0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0], DcSymbols);
        AppendTable(payload, AcChrominanceId, [0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77], AcChrominanceSymbols);

        // The length field counts itself, so the marker segment is the payload plus the two length bytes
        var length = payload.Count + LengthFieldSize;
        var segment = new List<byte>(payload.Count + LengthFieldSize + 2) { MarkerPrefix, DhtMarker };
        segment.Add((byte)(length >> 8));
        segment.Add((byte)(length & 0xFF));
        segment.AddRange(payload);
        return segment.ToArray();
    }

    /// <summary>
    ///     Appends one table: its class and identifier byte, its 16 counts, and its symbols.
    /// </summary>
    /// <param name="payload">The payload that receives the table.</param>
    /// <param name="classAndId">The class (high nibble) and identifier (low nibble) of the table.</param>
    /// <param name="counts">The number of codes of each length from 1 to 16 bits.</param>
    /// <param name="symbols">The symbols in code order; their number equals the sum of the counts.</param>
    private static void AppendTable(List<byte> payload, byte classAndId, byte[] counts, byte[] symbols)
    {
        // A table whose counts do not add up to its symbols would corrupt every frame it repairs
        if (counts.Length != CountsPerTable || counts.Sum(count => count) != symbols.Length)
        {
            throw new InvalidOperationException("A standard JPEG Huffman table is inconsistent.");
        }

        payload.Add(classAndId);
        payload.AddRange(counts);
        payload.AddRange(symbols);
    }

    /// <summary>
    ///     The twelve DC symbols (difference categories 0 to 11), shared by the luminance and chrominance tables.
    /// </summary>
    private static readonly byte[] DcSymbols = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

    /// <summary>
    ///     The 162 AC symbols of the luminance table (run length and size pairs) in code order.
    /// </summary>
    private static readonly byte[] AcLuminanceSymbols =
    [
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
        0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08, 0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
        0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
        0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
        0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
        0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
        0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA
    ];

    /// <summary>
    ///     The 162 AC symbols of the chrominance table (run length and size pairs) in code order.
    /// </summary>
    private static readonly byte[] AcChrominanceSymbols =
    [
        0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21, 0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
        0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91, 0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
        0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34, 0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
        0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
        0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
        0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
        0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
        0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
        0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA
    ];

    /// <summary>
    ///     The complete segment: marker, length, and the four standard tables.
    /// </summary>
    /// <remarks>
    ///     Declared after the symbol tables on purpose: static fields are initialized in textual order, and the
    ///     segment is assembled from them.
    /// </remarks>
    private static readonly byte[] SegmentBytes = BuildSegment();
}
