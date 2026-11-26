### CameraSelector

#### Verification Approach

`CameraSelector` is verified with unit tests defined in `CameraSelectorTests.cs`. The unit is a set of pure functions,
so the tests give it plain camera descriptions (a list of four cameras that looks like a Windows machine: one camera
through two backends, a second camera, and a camera whose name is a number) and plain format lists, and nothing is
mocked. No camera, source, or capture library is involved.

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- A number selects by position, an identifier selects exactly, and a name selects without regard to letter case.
- A number that is out of range is never read as a name.
- A name on two backends gives the first backend; a name twice on one backend is rejected with advice.
- A selector without a match gives a message that lists every available camera, or says that none is available.
- The default format, the nearest format for a request, and the rate rules are as designed, and a format that cannot be
  converted is never chosen.

#### Test Scenarios

**CameraSelector_SelectCamera_Index_ReturnsCameraAtIndex**: The selector `1` returns the second camera. This scenario
is tested by `CameraSelector_SelectCamera_Index_ReturnsCameraAtIndex`.

**CameraSelector_SelectCamera_IndexOutOfRange_ThrowsNotFoundAndIgnoresName**: The selector `7` is out of range of four
cameras, one of which is named "7"; a not-found exception is thrown. This scenario is tested by
`CameraSelector_SelectCamera_IndexOutOfRange_ThrowsNotFoundAndIgnoresName`.

**CameraSelector_SelectCamera_Id_ReturnsCamera**: An exact identifier returns its camera, not the camera of the same
name on another backend. This scenario is tested by `CameraSelector_SelectCamera_Id_ReturnsCamera`.

**CameraSelector_SelectCamera_Name_IsCaseInsensitive**: A name in other letter case with surrounding blanks returns its
camera. This scenario is tested by `CameraSelector_SelectCamera_Name_IsCaseInsensitive`.

**CameraSelector_SelectCamera_NameOnTwoBackends_PicksFirstBackend**: A name that DirectShow and Media Foundation both
report returns the DirectShow camera. This scenario is tested by
`CameraSelector_SelectCamera_NameOnTwoBackends_PicksFirstBackend`.

**CameraSelector_SelectCamera_NameTwiceOnSameBackend_ThrowsWithGuidance**: Two cameras of one backend share a name; an
argument exception whose message gives the index and identifier of each is thrown. This scenario is tested by
`CameraSelector_SelectCamera_NameTwiceOnSameBackend_ThrowsWithGuidance`.

**CameraSelector_SelectCamera_Unknown_MessageListsAvailable**: An unknown selector gives a not-found exception whose
message lists the four cameras with index, name, and backend. This scenario is tested by
`CameraSelector_SelectCamera_Unknown_MessageListsAvailable`.

**CameraSelector_SelectCamera_NoCameras_MessageSaysNone**: With no camera the message says that none is available. This
scenario is tested by `CameraSelector_SelectCamera_NoCameras_MessageSaysNone`.

**CameraSelector_SelectCamera_EmptyOrNull_ThrowsArgumentException**: An empty selector, a null selector, and a null list
are rejected. This scenario is tested by `CameraSelector_SelectCamera_EmptyOrNull_ThrowsArgumentException`.

**CameraSelector_SelectFormat_NoRequest_PrefersHdJpeg**: Of 4K, full HD in two pixel formats, and VGA, the full HD JPEG
is chosen. This scenario is tested by `CameraSelector_SelectFormat_NoRequest_PrefersHdJpeg`.

**CameraSelector_SelectFormat_NoRequest_SkipsFormatsBelowMinimumRate**: Full HD at 5 frames per second loses to 720p
at 30. This scenario is tested by `CameraSelector_SelectFormat_NoRequest_SkipsFormatsBelowMinimumRate`.

**CameraSelector_SelectFormat_Only4kFormats_ChoosesSmallestAtLeast15Fps**: A camera with only large sizes gets its
smallest size that is fast enough. This scenario is tested by
`CameraSelector_SelectFormat_Only4kFormats_ChoosesSmallestAtLeast15Fps`.

**CameraSelector_SelectFormat_ExactRequest_ReturnsExact**, **CameraSelector_SelectFormat_AbsentSize_ReturnsNearest**,
**CameraSelector_SelectFormat_OnlyWidth_IgnoresHeight**, **CameraSelector_SelectFormat_Fps_PrefersNotBelowThenClosest**,
and **CameraSelector_SelectFormat_OnlyFps_KeepsDefaultSizePolicy**: The exact size is chosen when offered; the nearest
size when not; an unspecified height does not count; a requested rate prefers the closest rate that is not below it; a
rate request alone leaves the size to the default policy. Each scenario is tested by the test of the same name.

**CameraSelector_SelectFormat_UnusableFormatsPresent_AreSkipped**: A larger 16-bit format is skipped for a usable one.
This scenario is tested by `CameraSelector_SelectFormat_UnusableFormatsPresent_AreSkipped`.

**CameraSelector_SelectFormat_NoUsableFormat_ThrowsOpenException**: A camera with only an unusable format, or with no
format, gives an open exception that names the camera. This scenario is tested by
`CameraSelector_SelectFormat_NoUsableFormat_ThrowsOpenException`.

**CameraSelector_SelectFormat_NullArguments_ThrowsArgumentNullException**: Null arguments are rejected. This scenario is
tested by `CameraSelector_SelectFormat_NullArguments_ThrowsArgumentNullException`.
