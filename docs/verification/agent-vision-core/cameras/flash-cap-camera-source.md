### FlashCapCameraSource

#### Verification Approach

`FlashCapCameraSource` is verified with unit tests defined in `FlashCapCameraSourceTests.cs`, in two groups:

- **Mapping tests** use the internal constructor, which takes a function that returns FlashCap descriptors, and feed it
  descriptors that belong to no real camera. A test subclass of FlashCap's descriptor type provides the identity,
  device type, name, description, and characteristics; opening a device is never part of enumeration, so the subclass
  does not support it. These tests run the real translation without camera hardware.
- **Real FlashCap tests** (names containing `RealFlashCap`) create the default source, which asks the real FlashCap of
  the machine. They assert invariants and never camera counts, so they pass with and without a camera. They are also
  the evidence for the FlashCap OTS item (see *FlashCap Verification*).

#### Test Environment

N/A - standard test environment. A camera is not required; if the machine has cameras, the real FlashCap tests check
them as well. The Windows and Linux runs differ only in the backends that may be reported.

#### Acceptance Criteria

- All unit tests pass with zero failures on every supported platform and runtime.
- The identifier, name, description, and backend of each descriptor are reported, in FlashCap's order.
- Size, pixel format, and frame rate of each usable characteristic are reported; unknown formats are left out and the
  device is still reported.
- A fractional frame rate is reported as an exact number and a zero fraction as zero.
- No descriptor gives an empty list.
- The real FlashCap enumerates without failing, and every backend it reports belongs to the operating system.

#### Test Scenarios

**FlashCapCameraSource_Constructor_NullEnumerate_ThrowsArgumentNullException**: A null function is passed to the
internal constructor; an `ArgumentNullException` that names the `enumerate` parameter is thrown. This scenario is
tested by `FlashCapCameraSource_Constructor_NullEnumerate_ThrowsArgumentNullException`.

**FlashCapCameraSource_GetCameras_Descriptor_MapsIdNameDescriptionAndBackend**: A V4L2 descriptor with a path identity
and a Video for Windows descriptor with a number identity are translated; the identifiers are the backend, a colon,
and the identity, and the names, descriptions, and backends are carried over in order. This scenario is tested by
`FlashCapCameraSource_GetCameras_Descriptor_MapsIdNameDescriptionAndBackend`.

**FlashCapCameraSource_GetCameras_NullIdentity_GivesIdWithBackendOnly**: A descriptor has no identity; the camera is
still reported, with the backend and an empty identity in its identifier. This scenario is tested by
`FlashCapCameraSource_GetCameras_NullIdentity_GivesIdWithBackendOnly`.

**FlashCapCameraSource_GetCameras_Characteristics_MapSizePixelFormatAndFrameRate**: A descriptor with two
characteristics is translated; width, height, pixel format name, and frame rate are reported for each. This scenario
is tested by `FlashCapCameraSource_GetCameras_Characteristics_MapSizePixelFormatAndFrameRate`.

**FlashCapCameraSource_GetCameras_UnknownPixelFormat_IsExcluded**: A characteristic with an unknown pixel format is
left out of its device, and a device with only unknown characteristics is still reported with an empty format list.
This scenario is tested by `FlashCapCameraSource_GetCameras_UnknownPixelFormat_IsExcluded`.

**FlashCapCameraSource_GetCameras_FractionalFrameRate_IsReportedAsDouble**: A frame rate of 30000 over 1001 is reported
as the quotient. This scenario is tested by `FlashCapCameraSource_GetCameras_FractionalFrameRate_IsReportedAsDouble`.

**FlashCapCameraSource_GetCameras_ZeroFrameRate_IsReportedAsZero**: A zero frame-rate fraction is reported as zero.
This scenario is tested by `FlashCapCameraSource_GetCameras_ZeroFrameRate_IsReportedAsZero`.

**FlashCapCameraSource_GetCameras_NoDescriptors_ReturnsEmptyList**: The function returns no descriptor; the result is an
empty list that is not null. This scenario is tested by
`FlashCapCameraSource_GetCameras_NoDescriptors_ReturnsEmptyList`.

**FlashCapCameraSource_GetCameras_RealFlashCap_EnumeratesWithoutCamerasPresent**: The default source enumerates with
the real FlashCap; the list is not null, and every camera reported has a non-empty identifier that starts with its
backend, and every format has positive width and height, a frame rate of at least zero, and a known pixel format. This
scenario is tested by `FlashCapCameraSource_GetCameras_RealFlashCap_EnumeratesWithoutCamerasPresent`.

**FlashCapCameraSource_GetCameras_RealFlashCap_BackendMatchesPlatform**: Every backend that the real FlashCap reports
is one that the operating system of the test run uses: DirectShow, Media Foundation, or Video for Windows on Windows,
and V4L2 on Linux. This scenario is tested by
`FlashCapCameraSource_GetCameras_RealFlashCap_BackendMatchesPlatform`.
