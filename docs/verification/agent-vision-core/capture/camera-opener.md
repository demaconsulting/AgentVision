### CameraOpener

#### Verification Approach

`CameraOpener` is verified with unit tests defined in `CameraOpenerTests.cs`. Its collaborators are the camera source,
which the tests replace with a hand-written fake that lists configured cameras, records every open call, and can be told
to fail, and the selector and session units, which are used as they are. No mocking library is needed, and no camera
hardware is involved.

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- A selector or a camera description opens the right camera with the chosen format, and the session reports it.
- Invalid options are rejected before the source is asked anything, and the message names the option.
- A failure of the source becomes an open exception that names the camera and keeps the cause; the library's own
  exceptions pass unchanged; no device is left open after a failure.

#### Test Scenarios

**CameraOpener_Constructor_NullSource_ThrowsArgumentNullException** and
**CameraOpener_Constructor_Default_CreatesOpener**: A null source is rejected naming the `source` parameter; the default
opener can be created without a camera. Each scenario is tested by the test of the same name.

**CameraOpener_Open_Selector_ReturnsSessionForCamera**: Of two cameras the second is opened by name; the session and the
device belong to it, in the chosen format. This scenario is tested by
`CameraOpener_Open_Selector_ReturnsSessionForCamera`.

**CameraOpener_Open_CameraInfo_ReturnsSession**: A camera description is opened with the default options. This scenario
is tested by `CameraOpener_Open_CameraInfo_ReturnsSession`.

**CameraOpenOptions_Default_HasDocumentedValues**: The default options have a one-second warm-up, five-second timeouts,
and no size or rate preference. This scenario is tested by `CameraOpenOptions_Default_HasDocumentedValues`.

**CameraOpener_Open_InvalidOption_ThrowsArgumentOutOfRange**: A negative warm-up, a zero or negative size, a zero or
not-a-number rate, a zero first-frame timeout, a negative stale-frame timeout, and a warm-up of two hours are each
rejected with a message that names the option, before the source is asked anything. This scenario is tested by
`CameraOpener_Open_InvalidOption_ThrowsArgumentOutOfRange`.

**CameraOpener_Open_NullArguments_ThrowsArgumentNullException**: A null selector and a null camera are rejected. This
scenario is tested by `CameraOpener_Open_NullArguments_ThrowsArgumentNullException`.

**CameraOpener_Open_UnknownSelector_ThrowsNotFoundAndOpensNothing**: An unknown name gives a not-found exception and no
device is opened. This scenario is tested by `CameraOpener_Open_UnknownSelector_ThrowsNotFoundAndOpensNothing`.

**CameraOpener_Open_SourceThrows_ThrowsCameraOpenWithInner**: A source that fails with an I/O error gives an open
exception that names the camera, contains the reason, and keeps the failure as the inner exception. This scenario is
tested by `CameraOpener_Open_SourceThrows_ThrowsCameraOpenWithInner`.

**CameraOpener_Open_SourceThrowsCameraException_PassesItOn**: An open exception of the source reaches the caller as the
same object. This scenario is tested by `CameraOpener_Open_SourceThrowsCameraException_PassesItOn`.

**CameraOpener_Open_NoUsableFormat_ThrowsOpenExceptionAndOpensNothing**: A camera with only a 16-bit format fails before
the device is opened. This scenario is tested by `CameraOpener_Open_NoUsableFormat_ThrowsOpenExceptionAndOpensNothing`.

**CameraOpener_Open_RequestedSize_PassesChosenFormatToSource**: A request for 640x480 reaches the source as that format,
and the session reports it. This scenario is tested by `CameraOpener_Open_RequestedSize_PassesChosenFormatToSource`.
