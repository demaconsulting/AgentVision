## Imaging

### Verification Approach

The `Imaging` subsystem is verified by integration tests defined in `ImagingSubsystemTests.cs`. Each test exercises the
frame decoder together with the camera image and the real CanvasNet, with synthetic frames: JPEG and PNG files written
by CanvasNet, and BMP files built by hand in the layouts that FlashCap produces. Files that the tests save are written
to a temporary folder and loaded again with CanvasNet's own codecs. No camera hardware is involved.

### Test Environment

N/A - standard test environment. The tests create and delete a temporary folder.

### Acceptance Criteria

- All integration tests pass with zero failures.
- JPEG, PNG, 24-bit BMP, and 32-bit BMP frames become images of the right size.
- A JPEG frame without Huffman tables becomes an image.
- A crop has the size of the region.
- A saved PNG and a saved JPEG can be loaded again with the size of the saved picture.

### Test Scenarios

**ImagingSubsystem_Decode_JpegBmpPngFrames_ProduceImages**: A JPEG, a PNG, a bottom-up 24-bit BMP, and a top-down 32-bit
BMP of the same 32x24 picture all decode to images of that size. This scenario is tested by
`ImagingSubsystem_Decode_JpegBmpPngFrames_ProduceImages`.

**ImagingSubsystem_Decode_JpegWithoutHuffmanTables_ProducesImage**: A JPEG from which every Huffman table segment was
removed decodes to an image of its size. This scenario is tested by
`ImagingSubsystem_Decode_JpegWithoutHuffmanTables_ProducesImage`.

**ImagingSubsystem_Crop_Region_ProducesCroppedImage**: A decoded frame is cropped to a region, and the crop has the size
of the region. This scenario is tested by `ImagingSubsystem_Crop_Region_ProducesCroppedImage`.

**ImagingSubsystem_Save_PngAndJpeg_CanBeReloaded**: A decoded and cropped frame is saved as `.png` and as `.jpg`, and
both files load again with the size of the crop. This scenario is tested by
`ImagingSubsystem_Save_PngAndJpeg_CanBeReloaded`.
