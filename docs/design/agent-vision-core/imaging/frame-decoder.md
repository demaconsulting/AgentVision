### FrameDecoder

![Imaging Structure](ImagingView.svg)

#### Purpose

`FrameDecoder` decodes the bytes of a camera frame (a JPEG, PNG, or BMP file image) into pixels, and repairs the
defects that camera frames commonly have, so that a frame decodes to the picture the camera saw. It is the one place
that holds the decoding policy: which bytes are accepted, which defects are repaired, and how a failure is reported.
The decoding itself is done by CanvasNet, which is referenced only by this unit and by `CameraImage`. The unit also
holds the standard Huffman tables (`StandardJpegTables`) that the repair inserts.

#### Data Model

The unit is an internal static class without state. `StandardJpegTables` is an internal static class with one constant
value: the bytes of a single DHT (define Huffman table) segment, starting with the marker `FF C4` and its length, that
holds the four standard tables of ITU-T T.81 Annex K (luminance DC, luminance AC, chrominance DC, and chrominance AC):
for each table its class and identifier, the 16 counts, and the symbols. The segment is built once, from the four table
definitions, and the builder rejects a table whose counts do not add up to its symbols.

#### Key Methods

**Decode**: Decodes one frame.

- *Parameters*: the bytes of the frame.
- *Returns*: CanvasNet's pixel surface; the caller owns and disposes it.
- *Algorithm*: reject data shorter than four bytes; apply `EnsureHuffmanTables` and then `NormalizeBitmap`; call
  CanvasNet's decoder on the result; wrap an `InvalidDataException` of CanvasNet in an `InvalidDataException` whose
  message starts with "The camera frame could not be decoded".

**EnsureHuffmanTables** (internal): Repairs a JPEG without Huffman tables.

- *Algorithm*: if the data does not start with `FF D8` return it; walk the marker segments from the start of the file
  (skipping fill bytes, markers without a length, and segments by their length); if a DHT segment is found return the
  data unchanged; at the start-of-scan marker return a new array with the standard segment inserted directly before
  it; if the structure cannot be walked return the data unchanged, so that the decoder reports the problem.

**NormalizeBitmap** (internal): Makes a bitmap readable.

- *Algorithm*: for an uncompressed 24-bit or 32-bit bitmap with a standard header, check that all rows are present;
  for a negative height make a copy with a positive height and the rows in reverse order; for 32 bits set the fourth
  byte of every pixel to 255; a bottom-up 24-bit bitmap and everything else is returned as it is.

#### Error Handling

All failures are `InvalidDataException`: frames that are too short, not recognized, damaged, or truncated. The repairs
never throw; a malformed structure is passed on to the decoder, which reports it. Nothing else is caught.

#### Dependencies

- **CanvasNet** (OTS): the image decoder and the pixel surface. See *CanvasNet Integration Design*.
- **StandardJpegTables**: the tables to insert.

#### Callers

`CameraImage.Decode`, and through it `CameraFrame.ToImage` and `CameraSnapper`.
