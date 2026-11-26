## SnapCommand

### Verification Approach

`SnapCommand` is verified with unit tests defined in `SnapCommandTests.cs`. The tests call the overload that takes a
`CameraSnapper`, built over a hand-written camera source from the test support folder, so no camera hardware is needed.
The fake source reports one camera with two formats and opens fake devices that stream synthetic frames (uncompressed
bitmaps) until they are disposed; it counts the devices that were opened and disposed, and it can fail to open or
report a failure of its device. The tests use a real `Context` and capture the console output, the error output, and
the log file. The AgentVisionCore library is a documented dependency of the unit (see the design), so the real
`CameraSnapper` is used, and the saved files are read back through the library to check their size.

The dispatch through `Program` and the real camera source are covered by the `Program`, `Cli`, and integration tests,
which use a camera that no machine has (index 99), so that their results do not depend on the machine.

### Test Environment

N/A - standard test environment. No camera is required for the automated tests. The manual checklist needs a machine
with a camera.

### Acceptance Criteria

- All unit tests pass with zero failures.
- A picture of the fake camera is saved with the size of the chosen format, and the command reports the file, size,
  and camera name with exit code 0.
- The crop, size, frame rate, and warm-up options reach the library: the small format is opened and the file holds
  only the cropped region.
- An unknown camera, a camera that cannot be opened, a camera that fails, a missing folder, an unsupported extension,
  and a crop outside the picture each give a message that starts with `Error:` on the error channel and exit code 1;
  the message does not show parameter names of the library.
- No camera stays open after a failure, and no camera is opened when the request is wrong before the camera is needed.
- The silent option keeps the result and the errors off the console, and an error still gives exit code 1; the log
  option keeps both in the log file.
- Null arguments are rejected, and a context without the camera or the output file is rejected.

### Test Scenarios

**SnapCommand_Run_FakeCamera_SavesFileAndReportsIt**: The fake camera is snapped by index with no warm-up; the PNG
file has the size of the larger format, the output is `Saved <file> (64x48) from Desk Camera`, the error output is
empty, and the exit code is 0. This scenario is tested by `SnapCommand_Run_FakeCamera_SavesFileAndReportsIt`.

**SnapCommand_Run_CameraByName_SavesFileAndClosesCamera**: The camera is chosen by its name; the file exists and the
one device that was opened was disposed. This scenario is tested by
`SnapCommand_Run_CameraByName_SavesFileAndClosesCamera`.

**SnapCommand_Run_WithCropAndFormat_PassesOptions**: A width, height, and frame rate of the small format and a crop
are given; the source opened the small format, the BMP file holds only the 8x6 region, and the output says so. This
scenario is tested by `SnapCommand_Run_WithCropAndFormat_PassesOptions`.

**SnapCommand_Run_CameraNotFound_WritesErrorAndExitCodeOne**: Camera 99 is requested; the error output names the
camera and lists the available one, the exit code is 1, no device was opened, and no file exists. This scenario is
tested by `SnapCommand_Run_CameraNotFound_WritesErrorAndExitCodeOne`.

**SnapCommand_Run_OpenFailure_WritesErrorAndExitCodeOne**: The source fails to open the camera; the error output
names the camera, the exit code is 1, and no file exists. This scenario is tested by
`SnapCommand_Run_OpenFailure_WritesErrorAndExitCodeOne`.

**SnapCommand_Run_CameraFails_WritesErrorAndReleasesCamera**: The device reports a failure as soon as it is opened and
delivers no frame; the error is reported, no file exists, and the device was disposed. This scenario is tested by
`SnapCommand_Run_CameraFails_WritesErrorAndReleasesCamera`.

**SnapCommand_Run_OutputDirectoryMissing_WritesErrorAndExitCodeOne**: The folder of the output file does not exist;
the error says so, the exit code is 1, and the camera was never opened. This scenario is tested by
`SnapCommand_Run_OutputDirectoryMissing_WritesErrorAndExitCodeOne`.

**SnapCommand_Run_UnsupportedExtension_WritesErrorWithoutParameterSuffix**: The output file is a GIF; the error says
the extension is not supported and lists `.png`, does not contain `(Parameter`, and no camera was opened. This
scenario is tested by `SnapCommand_Run_UnsupportedExtension_WritesErrorWithoutParameterSuffix`.

**SnapCommand_Run_CropOutsidePicture_WritesErrorAndExitCodeOne**: The crop reaches beyond the picture; the error says
the area does not fit, without parameter names, and no camera was opened. This scenario is tested by
`SnapCommand_Run_CropOutsidePicture_WritesErrorAndExitCodeOne`.

**SnapCommand_Run_SilentContext_WritesNothingToConsole**: The context is silent; nothing is written to the console or
the error channel, the exit code is 0, and the file exists. This scenario is tested by
`SnapCommand_Run_SilentContext_WritesNothingToConsole`.

**SnapCommand_Run_SilentContextWithError_StillExitsNonZero**: The context is silent and the camera is unknown; the
error channel is empty and the exit code is 1. This scenario is tested by
`SnapCommand_Run_SilentContextWithError_StillExitsNonZero`.

**SnapCommand_Run_LogFile_ReceivesOutput**: The context is silent and logs; the log file of a successful run holds
the `Saved` line and the log file of a failed run holds the error. This scenario is tested by
`SnapCommand_Run_LogFile_ReceivesOutput`.

**SnapCommand_Run_ContextWithoutCamera_ThrowsInvalidOperationException**: A context that asked for help has no camera;
the command refuses to run. This scenario is tested by
`SnapCommand_Run_ContextWithoutCamera_ThrowsInvalidOperationException`.

**SnapCommand_Run_ContextWithoutOutput_ThrowsInvalidOperationException**: A context that has a camera and asked for
help has no output file; the command refuses to run. This scenario is tested by
`SnapCommand_Run_ContextWithoutOutput_ThrowsInvalidOperationException`.

**SnapCommand_Run_NullContext_ThrowsArgumentNullException**: A null context is passed; an `ArgumentNullException` is
thrown. This scenario is tested by `SnapCommand_Run_NullContext_ThrowsArgumentNullException`.

**SnapCommand_Run_NullSnapper_ThrowsArgumentNullException**: A null snapper is passed; an `ArgumentNullException` is
thrown. This scenario is tested by `SnapCommand_Run_NullSnapper_ThrowsArgumentNullException`.

### Manual Hardware Checklist

The automated tests cannot take a picture of a real camera. On a machine with a camera, before a release that changes
the `snap` command or the capture library, a person checks by hand and records the result:

1. `agentvision cameras` lists the camera, and `agentvision snap --camera 0 --output frame.png` exits 0, prints the
   `Saved` line, and writes a picture that is not dark and shows the scene.
2. The same picture can be taken by the `Id` and by the name of the camera.
3. `--crop` keeps exactly the requested region, and `--width`, `--height`, and `--fps` give a picture of the closest
   format; the printed size is the size of the file.
4. `--warmup 0` gives a darker or less settled picture than the default on a camera that needs the warm-up.
5. While another program holds the camera, the command exits 1 with a message that names the camera, and no file is
   written.
6. After every run, another program (such as the camera app of the operating system) can open the camera: the camera
   is closed.
7. Opening a `VideoForWindows` camera (usually the entry named `Default`) is a known limitation: the command exits 1
   after the open timeout (about 15 seconds) with a message that names the camera, says the backend did not open the
   device, and recommends the `DirectShow` or `MediaFoundation` entry of the same camera. That entry works.
8. A camera whose privacy shutter or switch is closed, or whose access the operating system denied, can deliver a
   placeholder picture (for example a dark blue screen with a crossed camera icon). This is device behavior and not a
   defect; opening the shutter or the privacy setting gives real pictures.
