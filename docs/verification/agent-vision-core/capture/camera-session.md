### CameraSession

#### Verification Approach

`CameraSession` is verified with unit tests defined in `CameraSessionTests.cs`. The tests open a session through the
opener with a hand-written camera source whose device the test drives by hand: the test delivers frames (small byte
arrays that are never decoded, except in the two `CameraFrame` tests, which use a real JPEG), reports failures, and
checks how often the device was disposed. Time-dependent behavior uses short real durations (a warm-up of a few hundred
milliseconds, a stale-frame limit of 100 milliseconds) and event-based waits with a generous bound, so that a failure is
a failed test and not a hung run; no test depends on how fast the machine is for its result.

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

- All unit tests pass with zero failures.
- The newest frame is served, again and again, until a newer one arrives.
- The first snapshot waits for the first frame after the warm-up and times out with a message that names the camera.
- Frames inside the warm-up are never served; with no warm-up the first frame is.
- Eight threads taking snapshots while frames arrive all succeed.
- Disposal stops the device once, wakes waiting snapshots, and makes later frames harmless.
- A device failure, also while a snapshot waits, and a silence longer than the stale-frame limit give the disconnected
  exception.

#### Test Scenarios

**CameraSession_Snapshot_AfterFrames_ReturnsLatest**: Three frames are delivered; the snapshot is the third, with its
data, a non-negative age, and an arrival time that is not in the future. This scenario is tested by
`CameraSession_Snapshot_AfterFrames_ReturnsLatest`.

**CameraSession_Snapshot_Repeated_ReturnsSameFrameUntilNewArrives**: Two snapshots are the same object; after a new
frame the third snapshot is the new one. This scenario is tested by
`CameraSession_Snapshot_Repeated_ReturnsSameFrameUntilNewArrives`.

**CameraSession_Properties_AfterOpen_ReportCameraAndFormat**: A new session reports its camera and format, is open, and
is not ready. This scenario is tested by `CameraSession_Properties_AfterOpen_ReportCameraAndFormat`.

**CameraSession_Snapshot_FirstFrameArrivesLate_Waits**: A snapshot is started before any frame exists, a frame is
delivered, and the snapshot returns that frame. This scenario is tested by
`CameraSession_Snapshot_FirstFrameArrivesLate_Waits`.

**CameraSession_Snapshot_NoFrame_ThrowsTimeout**: With a first-frame timeout of 100 milliseconds and no frame, the
snapshot throws a timeout exception whose message names the camera. This scenario is tested by
`CameraSession_Snapshot_NoFrame_ThrowsTimeout`.

**CameraSession_Snapshot_WaitsThroughWarmUp_ThenReturnsFirstFrameAfterIt**: A camera that streams by itself is opened
with a 300 millisecond warm-up; the first snapshot returns a frame and took at least about the warm-up. This scenario is
tested by `CameraSession_Snapshot_WaitsThroughWarmUp_ThenReturnsFirstFrameAfterIt`.

**CameraSession_Snapshot_DarkFramesBeforeWarmUp_ServesLaterFrame**: A frame delivered at once is inside the warm-up and
is dropped; a frame delivered after the warm-up is served. This scenario is tested by
`CameraSession_Snapshot_DarkFramesBeforeWarmUp_ServesLaterFrame`.

**CameraSession_Snapshot_ZeroWarmUp_ServesFirstFrame**: With no warm-up the first frame is served. This scenario is
tested by `CameraSession_Snapshot_ZeroWarmUp_ServesFirstFrame`.

**CameraSession_Snapshot_ManyThreads_NoExceptionsAndValidFrames**: Eight threads take 500 snapshots each while another
thread keeps delivering frames; every snapshot returns a frame. This scenario is tested by
`CameraSession_Snapshot_ManyThreads_NoExceptionsAndValidFrames`.

**CameraSession_Dispose_Twice_DisposesDeviceOnce**: Two disposals stop the device exactly once. This scenario is tested
by `CameraSession_Dispose_Twice_DisposesDeviceOnce`.

**CameraSession_Dispose_WhileWaiting_ThrowsObjectDisposed**: A waiting snapshot ends with an object-disposed exception
when the session is disposed. This scenario is tested by `CameraSession_Dispose_WhileWaiting_ThrowsObjectDisposed`.

**CameraSession_Dispose_AfterDispose_FramesIgnoredAndSnapshotFails**: A frame after disposal does no harm, the session
is not ready, snapshots fail, and a frame obtained earlier is still valid. This scenario is tested by
`CameraSession_Dispose_AfterDispose_FramesIgnoredAndSnapshotFails`.

**CameraSession_Snapshot_DeviceFaulted_ThrowsDisconnected**: After a device failure the snapshot throws the disconnected
exception with the failure as inner exception and in the message. This scenario is tested by
`CameraSession_Snapshot_DeviceFaulted_ThrowsDisconnected`.

**CameraSession_Snapshot_FaultWhileWaiting_ThrowsDisconnected**: A failure ends the wait of a snapshot. This scenario is
tested by `CameraSession_Snapshot_FaultWhileWaiting_ThrowsDisconnected`.

**CameraSession_Snapshot_FramesStop_ThrowsDisconnectedAfterStaleTimeout**: With a stale-frame limit of 100 milliseconds
a frame becomes stale; the snapshot reports a disconnected camera. This scenario is tested by
`CameraSession_Snapshot_FramesStop_ThrowsDisconnectedAfterStaleTimeout`.

**CameraFrame_ToImage_JpegFrame_ReturnsImageOfFrameSize** and **CameraFrame_ToImage_DamagedFrame_ThrowsInvalidData**: A
frame that holds a real JPEG decodes to independent images of its size; a frame of arbitrary bytes gives an invalid-data
exception. Each scenario is tested by the test of the same name.
