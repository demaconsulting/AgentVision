## Cameras

### Verification Approach

The `Cameras` subsystem is verified by integration tests defined in `CamerasSubsystemTests.cs`. Each test exercises
`CameraEnumerator` together with a real `FlashCapCameraSource`, or with a hand-written source that replaces it. The
FlashCap source is fed descriptors that belong to no real camera, built from FlashCap's own descriptor type, through
its internal test seam, so the subsystem is tested without camera hardware and without depending on the cameras of the
machine. The tests that call the real FlashCap of the machine belong to the `FlashCapCameraSource` unit and to the
system tests. The streaming test opens a FlashCap descriptor whose device is a test device that follows FlashCap's real
lifecycle and frame queue, so opening, delivering a frame, and stopping run without a camera.

### Test Environment

N/A - standard test environment. No camera is required.

### Acceptance Criteria

- All integration tests pass with zero failures.
- Descriptors that FlashCap reports in any order are listed as cameras ordered by backend and identifier, with formats
  ordered largest and fastest first and the formats that cannot be captured left out.
- A source that finds no device gives an empty list and no error.
- A custom source replaces FlashCap completely.
- A camera of the FlashCap source can be opened, delivers a frame as a picture of the right size, is stopped when its
  handle is disposed, and the device is started once, stopped once, and disposed once.

### Test Scenarios

**CamerasSubsystem_Enumeration_FlashCapDescriptorThroughEnumerator_ReportsOrderedCameras**: Two descriptors, a V4L2
device listed first and a DirectShow device with an unknown-format characteristic listed second, pass through the
FlashCap source and the enumerator; the DirectShow camera comes first, its identifier is the backend and the device
identity, and its formats are the usable ones, largest first. This scenario is tested by
`CamerasSubsystem_Enumeration_FlashCapDescriptorThroughEnumerator_ReportsOrderedCameras`.

**CamerasSubsystem_Enumeration_FlashCapReportsNone_ReturnsEmptyList**: The FlashCap source finds no descriptor; the
enumerator returns an empty list. This scenario is tested by
`CamerasSubsystem_Enumeration_FlashCapReportsNone_ReturnsEmptyList`.

**CamerasSubsystem_Enumeration_CustomSource_IsUsedInsteadOfFlashCap**: The enumerator is created with a custom source
that reports a camera FlashCap could never report; exactly that camera is listed and the custom source was asked
once. This scenario is tested by `CamerasSubsystem_Enumeration_CustomSource_IsUsedInsteadOfFlashCap`.

**CamerasSubsystem_Streaming_FlashCapFakeDescriptor_DeliversStartAndStop**: A FlashCap source whose descriptor yields a
test device is enumerated, the camera is opened, a JPEG frame is injected, and the handle is disposed; the frame arrives
and decodes to its size, and the device was started, stopped, and disposed once each. This scenario is tested by
`CamerasSubsystem_Streaming_FlashCapFakeDescriptor_DeliversStartAndStop`.
