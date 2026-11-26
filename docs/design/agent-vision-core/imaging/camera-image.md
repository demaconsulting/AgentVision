### CameraImage

![Imaging Structure](ImagingView.svg)

#### Purpose

`CameraImage` is the library's picture. It hides the imaging library behind a small type that applications can use
without knowing it: it reports its size, crops a region into a new image, and saves as PNG, JPEG, or BMP by the file
extension. It owns its pixels, so it is disposable. The unit also owns two small public types: `CropRectangle` (a
region: position and size in pixels) and `ImageFileFormat` (`Png`, `Jpeg`, `Bmp`).

#### Data Model

- **surface** (private): CanvasNet's pixel surface; null after disposal.
- **Width** and **Height** (read-only): the size, read when the image is created.
- **CropRectangle** (readonly record struct): `X`, `Y`, `Width`, `Height`; not validated on construction, because
  validity depends on the picture it is applied to; its text form is `x,y,width,height`.
- **ImageFileFormat** (enum): the three file formats.

An image is not thread-safe.

#### Key Methods

**Decode** (static): Creates an image from the bytes of a JPEG, PNG, or BMP file through `FrameDecoder`.

**TryGetFormat** (static): Maps a path to a format by its extension (`.png`, `.jpg`, `.jpeg`, `.bmp`; letter case does
not matter); false for anything else, including a null, empty, or extension-less path.

**Crop**: Copies a region into a new image.

- *Preconditions*: the region has a position that is not negative, a size that is positive, and lies inside the picture.
- *Postconditions*: the result owns its own pixels; disposing either image does not affect the other.

**Save (path)**: Writes a file in the format of the extension; the folder must exist and an existing file is
overwritten. The extension is checked before the file is created.

**Save (stream, format)**: Writes the complete file to a stream that is not closed.

Encoding choices: PNG without alpha channel, JPEG at quality 90, BMP with 24 bits per pixel; camera pictures are
opaque, so nothing is lost and the files are smaller.

**Dispose**: Releases the pixels; repeated calls do nothing; any other member then throws `ObjectDisposedException`.

#### Error Handling

An invalid region gives `ArgumentOutOfRangeException` whose message names the region and the size of the picture. An
unsupported extension gives `ArgumentException` that lists the supported extensions, and an undefined format value
gives `ArgumentOutOfRangeException`; neither writes anything. A null path or stream gives `ArgumentNullException`.
File system failures (`DirectoryNotFoundException`, other `IOException`, `UnauthorizedAccessException`) propagate
unchanged. `InvalidDataException` comes from `Decode`.

#### Dependencies

- **FrameDecoder** (decoding) and **CanvasNet** (OTS): the surface, its crop, and the PNG, JPEG, and BMP encoders. See
  *CanvasNet Integration Design*.

#### Callers

`CameraFrame.ToImage` and `CameraSnapper` of the Capture subsystem, and any application that references the library.
