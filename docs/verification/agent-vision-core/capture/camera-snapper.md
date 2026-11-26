### CameraSnapper

#### Verification Approach

`CameraSnapper` is verified with unit tests defined in `CameraSnapperTests.cs`. The tests use a hand-written camera
source that streams synthetic JPEG pictures by itself (or delivers nothing, or frames that are no picture), and a
temporary folder for the output files. The opener, the session, the selector, and the Imaging units are used as they
are. No camera hardware is involved. The tests check files with CanvasNet's own codecs, and check that the fake device
was disposed on every path.

#### Test Environment

N/A - standard test environment. The tests create and delete temporary folders.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- A snap writes a file of the frame's size, or of the crop's size, and reports it with the camera and format.
- A request that can be seen to fail without the camera fails before the source is asked to open anything.
- The camera is closed when the save fails, when no frame arrives, when the frame cannot be decoded, and when a crop
  does not fit the frame that was really delivered.

#### Test Scenarios

**CameraSnapper_Snap_NullArguments_ThrowsArgumentNullException** and
**CameraSnapper_Constructor_Default_CreatesSnapper**: A null source and a null request are rejected; the default snapper
can be created without a camera. Each scenario is tested by the test of the same name.

**CameraSnapper_Snap_Valid_WritesImageAndReturnsResult**: A PNG of 64x48 is written, the result reports the path, the
size, the camera, and the format, and the device was disposed. This scenario is tested by
`CameraSnapper_Snap_Valid_WritesImageAndReturnsResult`.

**CameraSnapper_Snap_WithCrop_SavesCroppedSize**: A crop of 16x12 gives a JPEG and a result of that size. This scenario
is tested by `CameraSnapper_Snap_WithCrop_SavesCroppedSize`.

**CameraSnapper_Snap_UnknownCamera_ThrowsNotFoundAndWritesNothing**: An unknown camera is reported and no file is
written. This scenario is tested by `CameraSnapper_Snap_UnknownCamera_ThrowsNotFoundAndWritesNothing`.

**CameraSnapper_Snap_UnsupportedExtension_ThrowsBeforeOpen**, **CameraSnapper_Snap_EmptyOutputPath_ThrowsBeforeOpen**,
**CameraSnapper_Snap_MissingFolder_ThrowsBeforeOpen**, **CameraSnapper_Snap_CropOutsideFormat_ThrowsBeforeOpen**,
**CameraSnapper_Snap_InvalidCrop_ThrowsBeforeOpen**, and **CameraSnapper_Snap_InvalidOptions_ThrowsBeforeOpen**: A
`.gif` file, an empty path, a folder that does not exist, a crop that reaches past the 64x48 format, a crop without
area, and a negative warm-up are each rejected with the documented exception, and the source was asked for nothing (no
camera list and no device) or, for the crop outside the format, was not asked to open a device. Each scenario is tested
by the test of the same name.

**CameraSnapper_Snap_CropOutsideDecodedFrame_ThrowsAndDisposesDevice**: A camera announces 640x480 but delivers 64x48; a
crop that fits the announcement is rejected against the real size, the device is disposed, and nothing is written. This
scenario is tested by `CameraSnapper_Snap_CropOutsideDecodedFrame_ThrowsAndDisposesDevice`.

**CameraSnapper_Snap_SaveFails_StillDisposesDevice**: The output path names an existing folder; the save fails and the
device is disposed. This scenario is tested by `CameraSnapper_Snap_SaveFails_StillDisposesDevice`.

**CameraSnapper_Snap_Timeout_StillDisposesDevice**: A camera that delivers no frame times out and the device is
disposed. This scenario is tested by `CameraSnapper_Snap_Timeout_StillDisposesDevice`.

**CameraSnapper_Snap_DamagedFrame_ThrowsInvalidDataAndDisposesDevice**: A camera that delivers bytes that are no picture
gives an invalid-data exception and the device is disposed. This scenario is tested by
`CameraSnapper_Snap_DamagedFrame_ThrowsInvalidDataAndDisposesDevice`.
