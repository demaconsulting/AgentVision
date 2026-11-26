### CameraImage

#### Verification Approach

`CameraImage` is verified with unit tests defined in `CameraImageTests.cs`. The unit wraps CanvasNet, which is the thing
it uses, so the tests run the real CanvasNet on synthetic pictures, save to streams and to a temporary folder, and read
the result back with CanvasNet's own codecs. Tests whose names contain `RealCanvasNet` are also the evidence for the
crop and encode functions of the CanvasNet OTS item (see *CanvasNet Verification*). No camera hardware is involved.

#### Test Environment

N/A - standard test environment. The tests create and delete temporary folders.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- A crop has the size and the pixels of the region, and the whole picture is a valid region.
- A region that is outside the picture, has no area, or has a negative position is rejected, and the message names the
  region and the size.
- A crop is independent of its source.
- Each supported extension (in any letter case) writes a file that loads again with the same size; PNG keeps exact
  pixels; each stream format writes its signature.
- An unsupported extension is rejected with a message that lists the supported ones, and nothing is written.

#### Test Scenarios

**CameraImage_Decode_Frame_ReportsSize**: A JPEG frame of 64x48 decodes to an image of that size. This scenario is
tested by `CameraImage_Decode_Frame_ReportsSize`.

**CameraImage_Crop_RealCanvasNet_CopiesRegion**: A region away from the origin is cropped from a gradient; the size and
the pixels at two positions are those of the source at the region's position. This scenario is tested by
`CameraImage_Crop_RealCanvasNet_CopiesRegion`.

**CameraImage_Crop_ExactlyTheImage_ReturnsWholeImage**: A region equal to the picture is accepted. This scenario is
tested by `CameraImage_Crop_ExactlyTheImage_ReturnsWholeImage`.

**CameraImage_Crop_OutsideImage_ThrowsWithMessage**: Regions past the right edge, past the bottom edge, and starting at
the right edge are rejected; the message contains the region as `x,y,width,height` and the size of the picture. This
scenario is tested by `CameraImage_Crop_OutsideImage_ThrowsWithMessage`.

**CameraImage_Crop_ZeroSizeOrNegative_ThrowsArgumentOutOfRange**: Regions with a zero or negative width or height and
regions with a negative origin are rejected. This scenario is tested by
`CameraImage_Crop_ZeroSizeOrNegative_ThrowsArgumentOutOfRange`.

**CameraImage_Crop_Result_IsIndependentOfSource**: After the source is disposed, the crop is still complete, and the
source can no longer be used. This scenario is tested by `CameraImage_Crop_Result_IsIndependentOfSource`.

**CameraImage_Crop_RealCanvasNet_RejectsOutside**: CanvasNet itself rejects a region past the edge and a region without
area. This scenario is tested by `CameraImage_Crop_RealCanvasNet_RejectsOutside`.

**CameraImage_Save_Path_RealCanvasNet_RoundTrips**: Files named `.png`, `.jpg`, `.jpeg`, `.bmp`, `.PNG`, and `.JpG` are
written and load again with the same size. This scenario is tested by `CameraImage_Save_Path_RealCanvasNet_RoundTrips`.

**CameraImage_Save_Png_RealCanvasNet_KeepsExactPixels**: A saved PNG holds the exact pixels. This scenario is tested by
`CameraImage_Save_Png_RealCanvasNet_KeepsExactPixels`.

**CameraImage_Save_Stream_RealCanvasNet_WritesFormatSignature**: Each stream format starts with the signature of its
file type. This scenario is tested by `CameraImage_Save_Stream_RealCanvasNet_WritesFormatSignature`.

**CameraImage_Save_UnknownExtension_ThrowsListingSupported**: Files named `.gif`, `.tiff`, without an extension, and
with a dot only are rejected, the message lists `.png`, `.jpg`, and `.bmp`, and no file is created. This scenario is
tested by `CameraImage_Save_UnknownExtension_ThrowsListingSupported`.

**CameraImage_Save_MissingFolder_ThrowsDirectoryNotFound**,
**CameraImage_Save_NullArguments_ThrowsArgumentNullException**, and
**CameraImage_Save_UndefinedFormat_ThrowsArgumentOutOfRange**: A path in a folder that does not exist, null arguments,
and an undefined format value are rejected with the documented exceptions. Each scenario is tested by the test of the
same name.

**CameraImage_TryGetFormat_Path_ReportsSupport**: Supported extensions in any letter case have a format; other
extensions, a name without extension, an empty path, and null do not. This scenario is tested by
`CameraImage_TryGetFormat_Path_ReportsSupport`.

**CameraImage_Dispose_Twice_IsHarmlessAndImageIsUnusable**: A second disposal does nothing, and use after disposal gives
an object-disposed exception. This scenario is tested by `CameraImage_Dispose_Twice_IsHarmlessAndImageIsUnusable`.
