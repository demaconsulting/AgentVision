## FlashCap Verification

This document provides the verification evidence for the FlashCap OTS software item. Requirements for this OTS item
are defined in the FlashCap OTS Software Requirements document.

### Required Functionality

FlashCap (`AgentVision-OTS-FlashCap-EnumerateDevices` and `AgentVision-OTS-FlashCap-DescribeDevices`) shall enumerate
the video capture devices of the host, returning an empty set without failing when there is none, and shall describe
each device and each of its video formats. FlashCap (`AgentVision-OTS-FlashCap-OpenDevice`,
`AgentVision-OTS-FlashCap-OpenFailure`, and `AgentVision-OTS-FlashCap-DeliverFrames`) shall open a device, start it,
deliver its frames to a callback as complete image files with at most one frame waiting, report a device that cannot be
opened with an exception, and stop and release the device.

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

- **Streaming tests** (`FlashCapCameraDeviceTests.cs` and the open tests of `FlashCapCameraSourceTests.cs`) open
  FlashCap descriptors whose device is a test device, a subclass of FlashCap's own device type that follows FlashCap's
  real lifecycle. FlashCap's real open, start, stop, and dispose run, and so do its real frame queue and image
  conversion: the test device hands a frame to FlashCap's frame processor, which queues it, copies and converts it (JPEG
  as it is, raw 24-bit as a bitmap file, YUY2 converted to a 24-bit bitmap file), and calls the library's callback on
  its own thread. FlashCap offers no public way to fill its pixel buffer, so the test device calls FlashCap's internal
  copy routine by reflection; that is the one place of the test suite that depends on a FlashCap internal, and a
  FlashCap upgrade that changes it is found by these tests failing.

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

**Streaming**: FlashCap opens a device with the chosen characteristics and starts it once, stops and releases it once
when it is disposed, and reports a descriptor that cannot be opened or a device that cannot start with an exception.
This scenario is tested by `FlashCapCameraDevice_Open_RealFlashCapLifecycle_StartsDevice`,
`FlashCapCameraDevice_Dispose_RealFlashCapLifecycle_StopsAndDisposes`,
`FlashCapCameraSource_Open_MatchingDescriptor_OpensWithChosenCharacteristics`,
`FlashCapCameraSource_Open_DisposeHandle_StopsAndDisposesDevice`,
`FlashCapCameraDevice_Open_RealFlashCapDescriptorFails_ThrowsCameraOpen`, and
`FlashCapCameraDevice_Open_RealFlashCapStartFails_ThrowsCameraOpenAndDisposesDevice`.

**Frame Delivery**: FlashCap delivers a JPEG frame as it is, a raw 24-bit frame as a bitmap file, and a YUY2 frame
converted to a bitmap file that decodes to the frame's size. This scenario is tested by
`FlashCapCameraDevice_FrameArrived_RealFlashCapQueue_DeliversEncodedFrame`,
`FlashCapCameraDevice_FrameArrived_RealFlashCapQueueRawFrame_DeliversBitmapFile`, and
`FlashCapCameraDevice_FrameArrived_RealFlashCapQueueYuyvFrame_DeliversDecodableBitmap`.

What a real camera and driver deliver is not covered by these tests: the opt-in hardware smoke tests and the manual
checklist (see *Capture Verification*) check it, and they are not requirement evidence.
