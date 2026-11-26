### FlashCapCameraDevice

#### Verification Approach

`FlashCapCameraDevice` is verified with unit tests defined in `FlashCapCameraDeviceTests.cs`. The tests open FlashCap
descriptors that belong to no real camera and whose device is a test device (a subclass of FlashCap's own device type),
so the real FlashCap open, start, stop, and dispose sequence and the real FlashCap frame queue and image conversion run
without a camera. The test device plays the part of a camera driver: a test injects a frame (a motion JPEG frame, a raw
24-bit frame, or a raw YUY2 frame), and FlashCap queues it, converts it, and calls the unit's callback on its own
thread. FlashCap offers no public way to hand a frame to its pixel buffer, so the test device calls FlashCap's internal
copy routine by reflection; this is the single place of the test suite that depends on a FlashCap internal, and it is
confined to the test support code. Tests whose names contain `RealFlashCap` are also the evidence for the open, start,
stop, and frame functions of the FlashCap OTS item (see *FlashCap Verification*).

What a real camera and driver deliver (real frames, real timing, unplugging) is checked by the opt-in hardware smoke
tests and the manual checklist (see *Capture Verification*), and not by these tests.

#### Test Environment

N/A - standard test environment. No camera is required.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- Opening initializes the device with the chosen characteristics and starts it once.
- Disposing stops and disposes the device once, however often it is called.
- A JPEG frame arrives as the same bytes; a raw 24-bit frame and a YUY2 frame arrive as bitmap files that decode to the
  frame's size.
- A failing consumer is reported through the fault callback and not thrown into FlashCap.
- A device that cannot be opened or started gives an open exception that names the camera and carries the cause; a
  device that did not start is disposed.
- A canceled or timed-out open gives an open exception that says the backend did not open the device, with a hint to use
  another entry of the camera when the backend is VideoForWindows.

#### Test Scenarios

**FlashCapCameraDevice_Open_RealFlashCapLifecycle_StartsDevice**: The descriptor yields the test device; it was
initialized with the chosen characteristics, started once, and neither stopped nor disposed. This scenario is tested by
`FlashCapCameraDevice_Open_RealFlashCapLifecycle_StartsDevice`.

**FlashCapCameraDevice_Dispose_RealFlashCapLifecycle_StopsAndDisposes**: Two disposals stop and dispose the device
exactly once. This scenario is tested by `FlashCapCameraDevice_Dispose_RealFlashCapLifecycle_StopsAndDisposes`.

**FlashCapCameraDevice_FrameArrived_RealFlashCapQueue_DeliversEncodedFrame**: A JPEG frame is injected; the callback
receives the same bytes and the frame number. This scenario is tested by
`FlashCapCameraDevice_FrameArrived_RealFlashCapQueue_DeliversEncodedFrame`.

**FlashCapCameraDevice_FrameArrived_RealFlashCapQueueRawFrame_DeliversBitmapFile**: A raw 24-bit frame of 4x2 pixels is
injected; the callback receives a bitmap file that decodes to 4x2. This scenario is tested by
`FlashCapCameraDevice_FrameArrived_RealFlashCapQueueRawFrame_DeliversBitmapFile`.

**FlashCapCameraDevice_FrameArrived_RealFlashCapQueueYuyvFrame_DeliversDecodableBitmap**: A raw YUY2 frame of 64x64
pixels is injected; FlashCap converts it, and the callback receives a bitmap file that decodes to 64x64. This scenario
is tested by `FlashCapCameraDevice_FrameArrived_RealFlashCapQueueYuyvFrame_DeliversDecodableBitmap`.

**FlashCapCameraDevice_FrameArrived_CallbackThrows_ReportsFault**: The frame callback throws; the fault callback
receives the failure. This scenario is tested by `FlashCapCameraDevice_FrameArrived_CallbackThrows_ReportsFault`.

**FlashCapCameraDevice_Open_RealFlashCapDescriptorFails_ThrowsCameraOpen**: The descriptor fails to open its device; an
open exception that names the camera and the reason, with the failure as inner exception, is thrown. This scenario is
tested by `FlashCapCameraDevice_Open_RealFlashCapDescriptorFails_ThrowsCameraOpen`.

**FlashCapCameraDevice_Open_RealFlashCapVideoForWindowsCanceled_ThrowsCameraOpenWithBackendHint**: A VideoForWindows
descriptor whose open is canceled gives an open exception that names the camera, says that the VideoForWindows backend
did not open the device, hints that this legacy backend is often unsupported and that the DirectShow or MediaFoundation
entry should be used, and keeps the cancellation as inner exception. This scenario is tested by
`FlashCapCameraDevice_Open_RealFlashCapVideoForWindowsCanceled_ThrowsCameraOpenWithBackendHint`.

**FlashCapCameraDevice_Open_RealFlashCapOtherBackendCanceled_ThrowsCameraOpenWithoutHint**: A V4L2 descriptor whose open
is canceled gives the same kind of message without the legacy hint. This scenario is tested by
`FlashCapCameraDevice_Open_RealFlashCapOtherBackendCanceled_ThrowsCameraOpenWithoutHint`.

**FlashCapCameraDevice_Open_RealFlashCapStartFails_ThrowsCameraOpenAndDisposesDevice**: The device fails to start; an
open exception that says the camera could not be started is thrown, and the device was disposed. This scenario is tested
by `FlashCapCameraDevice_Open_RealFlashCapStartFails_ThrowsCameraOpenAndDisposesDevice`.
