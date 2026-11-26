### CameraEnumerator

#### Verification Approach

`CameraEnumerator` is verified with unit tests defined in `CameraEnumeratorTests.cs`. Its only collaborator is the
`ICameraSource` interface, so the tests replace the source with a hand-written fake that returns a configured list,
counts its calls, and can be told to fail or to break its contract. No mocking library is needed, and the real
FlashCap is never involved.

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- Every camera of the source is reported with all its details.
- Cameras and formats are in the documented order, and a duplicate format is reported once.
- The result does not change when the source's own list changes afterward.
- An empty source gives an empty list.
- A null source, a source that returns no list, and a failing source are handled as documented.

#### Test Scenarios

**CameraEnumerator_Constructor_NullSource_ThrowsArgumentNullException**: A null source is passed to the constructor;
an `ArgumentNullException` that names the `source` parameter is thrown. This scenario is tested by
`CameraEnumerator_Constructor_NullSource_ThrowsArgumentNullException`.

**CameraEnumerator_Enumerate_SourceWithCameras_ReportsEveryCameraWithDetails**: The source has one camera with two
formats; the identifier, name, description, backend, and both formats are reported. This scenario is tested by
`CameraEnumerator_Enumerate_SourceWithCameras_ReportsEveryCameraWithDetails`.

**CameraEnumerator_Enumerate_UnorderedSource_SortsByBackendThenId**: Four cameras of two backends are given in the
wrong order; the backend decides the order first and the identifier decides within a backend. This scenario is tested
by `CameraEnumerator_Enumerate_UnorderedSource_SortsByBackendThenId`.

**CameraEnumerator_Enumerate_UnorderedFormats_SortsLargestAndFastestFirst**: Formats are given in the wrong order for
every sort key; the frame size decides first, then the frame rate, then the pixel format name. This scenario is tested
by `CameraEnumerator_Enumerate_UnorderedFormats_SortsLargestAndFastestFirst`.

**CameraEnumerator_Enumerate_DuplicateFormats_ReportsOnce**: A camera reports the same format twice; it is listed
once. This scenario is tested by `CameraEnumerator_Enumerate_DuplicateFormats_ReportsOnce`.

**CameraEnumerator_Enumerate_EmptySource_ReturnsEmptyList**: The source has no camera; the result is an empty list that
is not null. This scenario is tested by `CameraEnumerator_Enumerate_EmptySource_ReturnsEmptyList`.

**CameraEnumerator_Enumerate_ResultIsIndependentOfSourceList_KeepsReportedCameras**: The source's list is changed and
cleared after `Enumerate` returned; the result still holds the camera that was reported. This scenario is tested by
`CameraEnumerator_Enumerate_ResultIsIndependentOfSourceList_KeepsReportedCameras`.

**CameraEnumerator_Enumerate_CalledTwice_ReturnsEqualResultsFromFreshQueries**: `Enumerate` is called twice; both
results hold the same cameras and formats, and the source was asked both times, because nothing is cached. This
scenario is tested by `CameraEnumerator_Enumerate_CalledTwice_ReturnsEqualResultsFromFreshQueries`.

**CameraEnumerator_Enumerate_SourceReturnsNull_ThrowsInvalidOperationException**: The source breaks its contract by
returning no list; an `InvalidOperationException` is thrown. This scenario is tested by
`CameraEnumerator_Enumerate_SourceReturnsNull_ThrowsInvalidOperationException`.

**CameraEnumerator_Enumerate_SourceThrows_PropagatesTheException**: The source throws; the same exception object
reaches the caller. This scenario is tested by `CameraEnumerator_Enumerate_SourceThrows_PropagatesTheException`.
