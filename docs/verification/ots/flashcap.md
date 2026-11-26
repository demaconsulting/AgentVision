## FlashCap Verification

This document provides the verification evidence for the FlashCap OTS software item. Requirements for this OTS item
are defined in the FlashCap OTS Software Requirements document.

### Required Functionality

FlashCap (`AgentVision-OTS-FlashCap-EnumerateDevices` and `AgentVision-OTS-FlashCap-DescribeDevices`) shall enumerate
the video capture devices of the host, returning an empty set without failing when there is none, and shall describe
each device and each of its video formats.

### Verification Approach

FlashCap is a runtime library with no self-validation mode, and there is no dedicated OTS test project, so it is
verified by integration test evidence from the AgentVisionCore test suite, which uses the real package:

- **Real FlashCap tests** (names containing `RealFlashCap`) call the real FlashCap of the machine that runs them. They
  assert invariants, never camera counts, so they pass with and without a camera: enumeration completes, every
  device and format that is reported is complete, and no backend of another operating system appears.
- **Mapping tests** feed descriptors that belong to no real camera, built from FlashCap's own descriptor,
  characteristic, pixel-format, and fraction types, through the real translation of the `FlashCapCameraSource` unit.
  They prove that FlashCap's description of a device and of a format, including the unknown marker and an exact
  fractional frame rate, carries the information that the library relies on.

On a machine without a camera the real FlashCap tests prove that enumeration works and returns an empty set, which is
the case that matters most for build agents. The continuous-integration runs on Windows and on Linux provide the
platform evidence. On a machine that has cameras, the same tests also check every reported device and format.

### Test Scenarios

**Enumeration**: FlashCap enumerates the devices of the host without failing, with or without a camera, and reports
only the backends that belong to the operating system. This scenario is tested by
`FlashCapCameraSource_GetCameras_RealFlashCap_EnumeratesWithoutCamerasPresent` and
`FlashCapCameraSource_GetCameras_RealFlashCap_BackendMatchesPlatform`.

**Device and Format Description**: FlashCap describes each device with its identity, name, description, and backend,
and each format with its pixel format, size, and frame rate; a format that cannot be captured is marked unknown. This
scenario is tested by `FlashCapCameraSource_GetCameras_RealFlashCap_EnumeratesWithoutCamerasPresent` (which checks every
reported device on a machine that has one),
`FlashCapCameraSource_GetCameras_Characteristics_MapSizePixelFormatAndFrameRate`,
`FlashCapCameraSource_GetCameras_UnknownPixelFormat_IsExcluded`, and
`FlashCapCameraSource_GetCameras_FractionalFrameRate_IsReportedAsDouble`.
