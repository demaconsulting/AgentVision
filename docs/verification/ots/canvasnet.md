## CanvasNet Verification

This document provides the verification evidence for the CanvasNet OTS software item. Requirements for this OTS item are
defined in the CanvasNet OTS Software Requirements document.

### Required Functionality

CanvasNet (`AgentVision-OTS-CanvasNet-DecodeFrames`, `AgentVision-OTS-CanvasNet-Crop`, and
`AgentVision-OTS-CanvasNet-Encode`) shall decode JPEG, PNG, and BMP images and report damaged data with an invalid-data
exception, shall copy a region of a picture into an independent picture and reject a region that does not fit, and shall
write PNG, JPEG, and BMP files that can be read back with the same size.

### Verification Approach

CanvasNet is a runtime library with no self-validation mode, and there is no dedicated OTS test project, so it is
verified by integration test evidence from the AgentVisionCore test suite, which uses the real package:

- **Real CanvasNet tests** (names containing `RealCanvasNet`) generate pictures with CanvasNet (solid colors and a
  gradient in which every position is different), and BMP files by hand in the layouts that cameras produce, run them
  through the library's decoding, cropping, and saving, and check the result: sizes, exact pixels for the lossless
  formats, the signature of each file type, and that every written file loads again. They need no camera and no network
  access, so they run on every build, on Windows and on Linux, which is the platform evidence.
- One test calls CanvasNet's crop directly to show that CanvasNet itself rejects a region outside the picture, as the
  library relies on.
- The standard JPEG tables that the library inserts into camera frames are compared with the tables that CanvasNet's own
  encoder writes, which shows that CanvasNet decodes with the tables that cameras use.

What real cameras send is checked by the opt-in hardware smoke tests and the manual checklist (see *Capture
Verification*), which are not requirement evidence.

### Test Scenarios

**Decoding**: CanvasNet decodes a JPEG, a PNG (with exact pixels), and 24-bit and 32-bit BMP files (bottom-up, and the
top-down ones after the library reorders their rows), and reports garbage as invalid data. This scenario is tested by
`FrameDecoder_Decode_Jpeg_RealCanvasNet_ReturnsSurfaceOfFrameSize`,
`FrameDecoder_Decode_Png_RealCanvasNet_ReturnsExactPixels`,
`FrameDecoder_Decode_Bmp_RealCanvasNet_ReturnsPixelsInPlace`,
`FrameDecoder_Decode_Bmp32ZeroFourthByte_RealCanvasNet_ReturnsOpaquePixels`, and
`FrameDecoder_Decode_Garbage_RealCanvasNet_ThrowsInvalidData`.

**Cropping**: CanvasNet copies a region into an independent picture, and rejects a region past the edge and a region
without area. This scenario is tested by `CameraImage_Crop_RealCanvasNet_CopiesRegion` and
`CameraImage_Crop_RealCanvasNet_RejectsOutside`.

**Encoding**: CanvasNet writes PNG, JPEG, and BMP files that load again with the same size, keeps every pixel of a PNG,
and writes the signature of each type. This scenario is tested by `CameraImage_Save_Path_RealCanvasNet_RoundTrips`,
`CameraImage_Save_Png_RealCanvasNet_KeepsExactPixels`, and
`CameraImage_Save_Stream_RealCanvasNet_WritesFormatSignature`.
