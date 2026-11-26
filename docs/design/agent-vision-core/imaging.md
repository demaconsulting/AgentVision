## Imaging

![Imaging Structure](ImagingView.svg)

### Overview

The `Imaging` subsystem turns the bytes of a camera frame into a picture that can be cropped and saved. It separates
two responsibilities: *decoding*, which deals with what cameras really send (and which depends on the CanvasNet OTS
library), and *the picture*, the library's own image type with the operations that applications use. The subsystem
contains two units:

- `FrameDecoder` (internal) decodes JPEG, PNG, and BMP frames to pixels, and repairs the defects of camera frames.
- `CameraImage` is the public picture: it reports its size, crops a region into a new image, and saves as PNG, JPEG, or
  BMP by the file extension. It also owns the small public types `CropRectangle` and `ImageFileFormat`.

The subsystem keeps no state between calls and starts no thread. No CanvasNet type appears in a public signature, so a
CanvasNet upgrade cannot break consumers.

### Interfaces

**CameraImage.Decode**: Decodes the bytes of an image file.

- *Type*: In-process .NET static method; part of the public API.
- *Role*: Provider.
- *Contract*: Takes the bytes of a JPEG, PNG, or BMP file and returns an image that the caller disposes; throws
  `InvalidDataException` for data that is empty, unrecognized, or damaged.

**CameraImage.Crop** and **CameraImage.Save**: The operations on a picture.

- *Type*: In-process .NET instance methods; part of the public API.
- *Role*: Provider.
- *Contract*: `Crop` returns a new independent image or throws `ArgumentOutOfRangeException` for a region that does
  not fit; `Save` writes a file by its extension (`.png`, `.jpg`, `.jpeg`, `.bmp`) or a stream in a given format and
  throws `ArgumentException` for any other extension.
- *Constraints*: An image is not thread-safe; separate images may be used on separate threads.

**CanvasNet**: The OTS library that decodes and encodes.

- *Type*: In-process .NET library calls.
- *Role*: Consumer (only `FrameDecoder` and `CameraImage`).
- *Contract*: See *CanvasNet Integration Design*.

### Design

`CameraImage.Decode` calls `FrameDecoder.Decode`, which prepares the bytes and hands them to CanvasNet's decoder that
chooses the codec from the signature of the data. The preparation fixes the three defects that cameras are known for:
a JPEG without Huffman tables gets the standard tables inserted before its start of scan; a bitmap with a negative
height (top-down rows) is turned into a bottom-up bitmap; and a 32-bit bitmap whose unused fourth byte is zero (read
as transparency) gets an opaque fourth byte. Everything else is passed on as it is.

`CameraImage` holds the decoded pixels and its size. `Crop` checks the region itself, so that its message names the
region and the size of the picture, and then lets CanvasNet copy the region into a new, independent surface. `Save`
selects the codec from the extension; pictures from cameras are opaque, so PNG is written without an alpha channel,
BMP with 24 bits per pixel, and JPEG at quality 90.

**Known limitation.** CanvasNet limits a picture to 8192 pixels on each side, which is far above any webcam. A frame
that exceeds it is reported as invalid data.
