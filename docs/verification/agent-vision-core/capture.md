## Capture

### Verification Approach

The `Capture` subsystem is verified by integration tests defined in `CaptureSubsystemTests.cs`. Each test exercises the
selector, the opener, the session, and the snapper together, with a hand-written camera source that replaces the
capture library: the source lists configured cameras and opens a fake device that the test drives by hand (frames,
failures) or that streams synthetic pictures by itself. The pictures are real JPEG files written with CanvasNet, so the
tests that decode run the real Imaging subsystem. No camera hardware is involved.

Three kinds of evidence complement the camera-less tests, and none of them is linked to a requirement, because a test
that is skipped on machines without a camera cannot be evidence:

- **Hardware smoke tests** (`Hardware/CameraHardwareSmokeTests.cs`), which are opt-in. They run only when the
  environment variable `AGENTVISION_HARDWARE_TESTS` is `1`, which `build.ps1 -All -Hardware` sets, and otherwise they
  report that they are skipped. They open the first camera with the defaults and decode a snapshot, open every camera
  at the same time, open a camera by identifier and by name, open two cameras and close them in either order, check
  that a snapshot of a streaming camera returns in under 50 milliseconds, and save a cropped PNG and a cropped JPEG.
  Cameras of the legacy VideoForWindows backend are left out of these tests (see *Known Limitations* below). A camera
  can be open in one process only, and the test hosts of the two target frameworks run at the same time, so every
  hardware test holds an exclusive lock file (`agentvision-hardware-tests.lock` in the temporary folder) while it runs:
  the tests of all hosts run one after another, and the operating system releases the lock if a host ends abruptly.
- **Manual hardware verification** on the developer machine (see the checklist below), which covers what a test run
  cannot: real drivers, real latency, and real unplugging.
- The behavior of FlashCap with frames is exercised camera-less in the Cameras subsystem tests.

### Test Environment

N/A - standard test environment for the camera-less tests; no camera is required. The hardware smoke tests and the
manual checklist need a machine with at least one camera (two for the tests that open two cameras). The cameras must be
free of other programs, because a camera can be open in one program at a time; the hardware tests of the two target
frameworks share the cameras one test at a time.

### Acceptance Criteria

- All integration tests pass with zero failures.
- A snapshot returns the newest frame that the camera delivered, and an index, an identifier, and a name reach the
  device of the same camera.
- The format request and the default reach the source as the chosen format.
- A frame that arrives during the warm-up is never served.
- Two sessions are independent: closing one stops its device only.
- Concurrent snapshots while frames arrive all succeed.
- Closing a session stops its device, and the closed session refuses snapshots.
- Every failure (unknown camera, open failure, camera that stops, camera without frames) gives its documented exception.
- The one-shot snapshot writes the cropped file and closes the camera.

### Test Scenarios

**CaptureSubsystem_HotFrame_SnapshotAfterEmit_ReturnsNewestFrame**: A camera is opened and delivers two frames; the
snapshot is the second, and it decodes to a picture of the delivered size. This scenario is tested by
`CaptureSubsystem_HotFrame_SnapshotAfterEmit_ReturnsNewestFrame`.

**CaptureSubsystem_Selection_IndexIdName_ReachDeviceOfSameCamera**: The same camera is opened by index, by identifier,
and by a name in other letter case; all three devices belong to that camera. This scenario is tested by
`CaptureSubsystem_Selection_IndexIdName_ReachDeviceOfSameCamera`.

**CaptureSubsystem_FormatSelection_RequestAndDefault_PassChosenFormatToSource**: A camera with three sizes is opened
once with a request for 1280x720 and once without a request; the source receives 1280x720 and 1920x1080. This scenario
is tested by `CaptureSubsystem_FormatSelection_RequestAndDefault_PassChosenFormatToSource`.

**CaptureSubsystem_WarmUp_FramesBeforeWarmUp_AreNotServed**: A camera with a short warm-up delivers a dark frame at
once and a bright frame after the warm-up; the snapshot is the bright frame. This scenario is tested by
`CaptureSubsystem_WarmUp_FramesBeforeWarmUp_AreNotServed`.

**CaptureSubsystem_Independence_TwoSessions_CloseOneKeepsOther**: Two cameras are open; the first is closed; its
device is stopped, the second device is not, and the second session serves its newest frame. This scenario is tested by
`CaptureSubsystem_Independence_TwoSessions_CloseOneKeepsOther`.

**CaptureSubsystem_ThreadSafety_ConcurrentSnapshotsWhileFramesArrive_AllSucceed**: A camera streams pictures by itself
while six threads take snapshots and decode some of them; all threads finish without a failure. This scenario is tested
by `CaptureSubsystem_ThreadSafety_ConcurrentSnapshotsWhileFramesArrive_AllSucceed`.

**CaptureSubsystem_Close_DisposeStopsDevice**: A session is disposed; its device is disposed and the session reports
that it is closed. This scenario is tested by `CaptureSubsystem_Close_DisposeStopsDevice`.

**CaptureSubsystem_Errors_EachFailure_ThrowsDocumentedException**: An unknown camera, a source that fails to open, a
camera that delivers no frame, and a camera that reports a failure give `CameraNotFoundException`,
`CameraOpenException`, `CameraTimeoutException`, and `CameraDisconnectedException`, all derived from
`CameraException`. This scenario is tested by `CaptureSubsystem_Errors_EachFailure_ThrowsDocumentedException`.

**CaptureSubsystem_OneShot_Snap_OpensSavesAndCloses**: The snapper opens a streaming camera once, saves a cropped PNG of
the cropped size, and closes the camera. This scenario is tested by `CaptureSubsystem_OneShot_Snap_OpensSavesAndCloses`.

#### Manual Hardware Checklist

On a machine with cameras, before a release that changes capture, a person runs `pwsh ./build.ps1 -All -Hardware` and
records the result, and then checks by hand with a small program or the tool (the camera options are documented in the
user guide):

1. Every listed camera opens; the first snapshot arrives after about the warm-up; later snapshots are immediate.
2. All cameras can be open at once, each with its own picture, and closing them in any order frees each camera (another
   application, such as the camera app of the operating system, can then open it).
3. A frame from a camera that sends motion JPEG without Huffman tables decodes (a camera is known to need this when its
   frames fail to decode without the repair).
4. Unplugging a camera that is open makes the next snapshots fail with the disconnected exception within the stale-frame
   timeout, and does not stop the other cameras.
5. A camera that another application holds is reported with an open exception that names the camera.
6. A snapshot after the camera was open for a long time (several hours) is still immediate and current.

#### Known Limitations

- **VideoForWindows cameras cannot be opened.** FlashCap's VideoForWindows backend is a legacy backend that needs a
  window message pump, which a console tool and a test host do not have. Opening such a camera (on Windows it is
  usually the entry named `Default`) fails after the 15-second open timeout with an open exception that says the
  backend did not open the device and recommends the DirectShow or MediaFoundation entry of the same camera. The
  hardware smoke tests therefore skip every camera whose backend is `VideoForWindows`, and checklist item 1 applies to
  the DirectShow, MediaFoundation, and V4L2 entries only.
- **A camera can deliver a placeholder picture.** Some laptop cameras stream a "camera off" placeholder (for example a
  dark blue screen with a crossed camera icon) when a privacy shutter or switch is closed, or after the operating
  system revoked camera access, and may deliver real frames only during the first moments. This is the behavior of the
  device or driver and not a defect of the library; the smoke tests only check that a picture decodes.
