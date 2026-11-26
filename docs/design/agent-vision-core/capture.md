## Capture

![Capture Structure](CaptureView.svg)

### Overview

The `Capture` subsystem keeps cameras open ("hot") and hands out their newest frame with almost no delay. It is the
capability that the product exists for: opening a camera takes a second or more, so a long-running application opens
each camera once and then reads frames from memory, from any thread, as often as it wishes. It separates four
responsibilities so that each can be tested without a camera:

- `CameraSelector` decides *which* camera and *which* format a request means (pure functions).
- `CameraOpener` opens a camera: it checks the options, asks the selector, starts the device through an
  `ICameraSource`, and returns a session.
- `CameraSession` is the open camera: it receives the frames of the device, discards the warm-up, publishes the newest
  frame, serves snapshots, and stops the device when it is disposed. It also owns the value types of the subsystem:
  `CameraFrame`, `CameraOpenOptions`, and the exceptions (`CameraException` and its four derived types).
- `CameraSnapper` is the cold path: one call that opens a camera, takes one frame, optionally crops it, saves it, and
  closes the camera again. The command-line tool calls it so that the tool stays a thin front-end.

The subsystem knows no capture library: it reaches cameras only through `ICameraSource`, which belongs to the
Cameras subsystem, and it reaches pictures only through the Imaging subsystem.

### Interfaces

**CameraOpener.Open**: Opens a camera and returns a session.

- *Type*: In-process .NET instance method; part of the public API.
- *Role*: Provider.
- *Contract*: Takes a selector (index, identifier, or name) or a camera description, and optional
  `CameraOpenOptions`; returns a streaming `CameraSession`. Throws `CameraNotFoundException`, `CameraOpenException`,
  `ArgumentException`, or `ArgumentOutOfRangeException` as documented, and leaves no camera open when it throws.
- *Constraints*: The caller owns the session and must dispose it.

**CameraSession.Snapshot**: Returns the newest frame.

- *Type*: In-process .NET instance method; part of the public API.
- *Role*: Provider.
- *Contract*: Returns a `CameraFrame` without waiting, except that the first call waits for the first frame after
  the warm-up. Safe from any number of threads. Throws `CameraTimeoutException`, `CameraDisconnectedException`, or
  `ObjectDisposedException` as documented.
- *Constraints*: The frame is immutable; it stays valid after the session is disposed.

**CameraSnapper.Snap**: Takes one picture and saves it.

- *Type*: In-process .NET instance method; part of the public API.
- *Role*: Provider.
- *Contract*: Takes a `SnapRequest` (camera, output path, optional crop, options) and returns a `SnapResult`. The
  camera is closed before the method returns or throws.
- *Constraints*: Overwrites an existing output file. Writes nothing when it throws before the save.

**ICameraSource.Open**: The seam to the capture library (see *Cameras Design*).

- *Type*: In-process .NET interface method.
- *Role*: Consumer (the opener and the snapper call it).
- *Contract*: Starts a camera in a format and delivers its frames to a callback; the returned handle stops it.

### Design

**Frame path.** The device thread calls `CameraSession.OnFrameArrived` for every frame. The method does the whole of
the work done on that thread: it checks that the session is open and streaming, compares the time since streaming
started with the warm-up (a monotonic stopwatch timestamp), allocates one `CameraFrame` around the bytes, publishes it
with one interlocked write, and signals an event. Nothing is queued and no lock is taken, so the device never waits for
a consumer, and a slow consumer never makes the device fall behind.

**Snapshot path.** `Snapshot` checks for disposal and failure, reads the published frame with one volatile read, and
returns it. Only when no frame has been published yet (the first snapshot, during or right after the warm-up) it waits
on the event, without polling, until a frame, a failure, or the disposal arrives, or until the deadline (warm-up plus
first-frame timeout, counted from the start of streaming) passes. A frame whose age exceeds the stale-frame timeout is
refused with `CameraDisconnectedException`, because a camera that was unplugged leaves its last frame in memory.

**Warm-up.** A frame counts only when it arrives at or after the warm-up, measured from the moment `CameraOpener`
told the session that streaming had started. Frames that arrive while the device is still being opened are warm-up by
definition. The default is one second; zero disables it.

**Format policy.** `CameraSelector` ranks the formats that can be converted to a picture. Without a request the
default is the largest size not above 1920x1080 with at least 15 frames per second, then the highest rate, then JPEG,
YUYV, NV12, UYVY, RGB24, RGB32, ARGB32, PNG in that order. With a request the nearest size wins, then the frame rate
that is not below the request, then the same pixel-format order. A requested size that the camera lacks never fails.

**Disposal.** `CameraSession.Dispose` is idempotent. It marks the session closed, wakes waiting snapshots (which then
throw `ObjectDisposedException`), disposes the device handle (which stops the camera and returns when no callback can
run any more), and drops the held frame.

**Cross-hierarchy dependencies.** The subsystem depends on `ICameraSource`, `CameraInfo`, `CameraFormat`,
`CameraEnumerator`, and `EncodedFrame` of the Cameras subsystem, and on `CameraImage` and `CropRectangle` of the Imaging
subsystem (`CameraFrame.ToImage` and the snapper). The Cameras subsystem in turn throws `CameraOpenException` of this
subsystem, so the two namespaces refer to each other by design: the exception is the one failure vocabulary of the
library. The subsystem tests use a fake `ICameraSource` and the real Imaging subsystem.

**Thread safety.** `CameraOpener` and `CameraSnapper` hold no state besides their source. `CameraSession` is fully
thread-safe. `CameraFrame` and `CameraOpenOptions` are immutable.
