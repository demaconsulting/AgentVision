## CamerasCommand

### Verification Approach

`CamerasCommand` is verified with unit tests defined in `CamerasCommandTests.cs`. The tests call the overload that takes
a `CameraEnumerator`, built over a hand-written camera source from the test support folder, so no camera hardware is
needed. They use a real `Context` and capture the console output and the log file. The AgentVisionCore library is a
documented dependency of the unit (see the design), so the real `CameraEnumerator` is used.

### Test Environment

N/A - standard test environment. No camera is required.

### Acceptance Criteria

- All unit tests pass with zero failures.
- A source without cameras gives the message `No cameras found.` and exit code 0.
- Each camera is printed with its index, name, identifier, backend, description, and aligned formats.
- Frame rates are printed with the invariant culture, at most three decimals, and no trailing zeros.
- The silent option keeps the listing off the console, and the log option keeps it in the log file.
- Null arguments are rejected.

### Test Scenarios

**CamerasCommand_Run_NoCameras_WritesNoCamerasFoundAndExitCodeZero**: The source has no camera; the output is
`No cameras found.` and the exit code of the context is 0. This scenario is tested by
`CamerasCommand_Run_NoCameras_WritesNoCamerasFoundAndExitCodeZero`.

**CamerasCommand_Run_WithCameras_ListsCamerasAndFormats**: The source has two cameras, the first with two formats and
the second with none; the summary line, each camera under its index, the identifier, backend, and description, the
aligned formats, and the statement that the second camera has no format that can be captured are printed. This
scenario is tested by `CamerasCommand_Run_WithCameras_ListsCamerasAndFormats`.

**CamerasCommand_Run_FractionalFrameRate_FormatsWithoutTrailingZeros**: Formats with the frame rates 29.97, 30, and
29.97002997 are printed as `29.97 fps`, `30 fps`, and `29.97 fps`. This scenario is tested by
`CamerasCommand_Run_FractionalFrameRate_FormatsWithoutTrailingZeros`.

**CamerasCommand_Run_SilentContext_WritesNothingToConsole**: The context is silent; nothing is written to the console
and the exit code is 0. This scenario is tested by `CamerasCommand_Run_SilentContext_WritesNothingToConsole`.

**CamerasCommand_Run_LogFile_ReceivesOutput**: The context is silent and has a log file; the log file holds the
listing. This scenario is tested by `CamerasCommand_Run_LogFile_ReceivesOutput`.

**CamerasCommand_Run_NullContext_ThrowsArgumentNullException**: A null context is passed; an `ArgumentNullException`
is thrown. This scenario is tested by `CamerasCommand_Run_NullContext_ThrowsArgumentNullException`.

**CamerasCommand_Run_NullEnumerator_ThrowsArgumentNullException**: A null enumerator is passed; an
`ArgumentNullException` is thrown. This scenario is tested by
`CamerasCommand_Run_NullEnumerator_ThrowsArgumentNullException`.
