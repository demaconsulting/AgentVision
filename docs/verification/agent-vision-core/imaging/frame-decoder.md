### FrameDecoder

#### Verification Approach

`FrameDecoder` (with its standard JPEG tables) is verified with unit tests defined in `FrameDecoderTests.cs`. The unit
has no collaborator except CanvasNet, which is the thing it uses, so the tests run the real CanvasNet on synthetic
frames: JPEG and PNG files written by CanvasNet, JPEG files whose Huffman tables were removed by the test, and BMP files
built by hand in every layout that FlashCap produces (24-bit and 32-bit, bottom-up and top-down, with a zero fourth
byte). Tests whose names contain `RealCanvasNet` are also the evidence for the decoding functions of the CanvasNet OTS
item (see *CanvasNet Verification*). One test compares the standard tables with the tables that CanvasNet's own encoder
writes, which shows that the tables are the ones that cameras use.

#### Test Environment

N/A - standard test environment. No camera is required.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- JPEG, PNG, and BMP frames decode to the size and colors of the frame; a PNG decodes to the exact pixels.
- Every BMP layout decodes with each pixel in its place, and a zero fourth byte gives opaque pixels.
- A JPEG without Huffman tables decodes to the same pixels as the original; a JPEG with tables and any frame that is not
  a JPEG is passed on unchanged.
- The standard tables equal the tables of CanvasNet's encoder.
- Empty, unrecognized, and truncated frames are reported as invalid data.

#### Test Scenarios

**FrameDecoder_Decode_Jpeg_RealCanvasNet_ReturnsSurfaceOfFrameSize**: A mid-gray JPEG of 64x48 decodes to that size and
a color near the original. This scenario is tested by
`FrameDecoder_Decode_Jpeg_RealCanvasNet_ReturnsSurfaceOfFrameSize`.

**FrameDecoder_Decode_Png_RealCanvasNet_ReturnsExactPixels**: A PNG of a gradient decodes to exactly the pixels written.
This scenario is tested by `FrameDecoder_Decode_Png_RealCanvasNet_ReturnsExactPixels`.

**FrameDecoder_Decode_Bmp_RealCanvasNet_ReturnsPixelsInPlace**: BMP frames of 24 and 32 bits, bottom-up and top-down,
with a row width that needs padding, decode with each pixel where it was written. This scenario is tested by
`FrameDecoder_Decode_Bmp_RealCanvasNet_ReturnsPixelsInPlace`.

**FrameDecoder_Decode_Bmp32ZeroFourthByte_RealCanvasNet_ReturnsOpaquePixels**: A 32-bit BMP whose fourth byte is zero
decodes to opaque pixels with the colors written. This scenario is tested by
`FrameDecoder_Decode_Bmp32ZeroFourthByte_RealCanvasNet_ReturnsOpaquePixels`.

**FrameDecoder_Decode_JpegWithoutDht_DecodesLikeOriginal**: A color JPEG and the same JPEG without Huffman tables decode
to the same size and the same pixels. This scenario is tested by
`FrameDecoder_Decode_JpegWithoutDht_DecodesLikeOriginal`.

**FrameDecoder_EnsureHuffmanTables_JpegWithTables_ReturnsInputUnchanged** and
**FrameDecoder_EnsureHuffmanTables_NotJpeg_ReturnsInputUnchanged**: A JPEG with its own tables and a PNG are returned
byte for byte unchanged. Each scenario is tested by the test of the same name.

**StandardJpegTables_Segment_MatchesCanvasNetEncoderTables**: The four tables of the standard segment equal, byte for
byte, the tables in a color JPEG that CanvasNet wrote. This scenario is tested by
`StandardJpegTables_Segment_MatchesCanvasNetEncoderTables`.

**FrameDecoder_Decode_Garbage_RealCanvasNet_ThrowsInvalidData**: Bytes that are no image give an invalid-data exception
whose message mentions the camera frame. This scenario is tested by
`FrameDecoder_Decode_Garbage_RealCanvasNet_ThrowsInvalidData`.

**FrameDecoder_Decode_Empty_ThrowsInvalidData**: No bytes give an invalid-data exception that says the frame is too
short. This scenario is tested by `FrameDecoder_Decode_Empty_ThrowsInvalidData`.

**FrameDecoder_Decode_Truncated_ThrowsInvalidData**: A JPEG and a PNG cut to half their length each give an invalid-data
exception. This scenario is tested by `FrameDecoder_Decode_Truncated_ThrowsInvalidData`.
