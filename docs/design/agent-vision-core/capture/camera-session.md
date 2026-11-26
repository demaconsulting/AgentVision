### CameraSession

![Capture Structure](CaptureView.svg)

#### Purpose

`CameraSession` is an open ("hot") camera. Its single responsibility is to turn the stream of frames of a device into
a value that any thread can read at any time: it discards the frames of the warm-up, keeps only the newest frame,
serves it to snapshots without waiting, and releases the device when it is disposed. It also owns the value types of
the subsystem that describe its state and failures: `CameraFrame`, `CameraOpenOptions`, `CameraException` and its four
derived exceptions.

#### Data Model

- **latest** (`CameraFrame?`): the newest frame that arrived after the warm-up. Written with an interlocked exchange by
  the device thread and read with a volatile read by snapshots; null before the first frame and after disposal.
- **fault** (`Exception?`): the first failure that the device reported; written once with a compare-exchange.
- **device** (`IDisposable?`): the handle that stops the device, set when streaming starts.
- **streamStartTimestamp** (long) and **streaming** (volatile bool): the monotonic timestamp at which streaming
  started, valid once the flag is set. Frames before the flag are warm-up.
- **disposed** (int): zero while open, one after disposal; changed only with an interlocked exchange.
- **wakeUp** (manual-reset event): signaled by a frame, a failure, or disposal; never reset and never disposed.
- **options**, **Camera**, **Format**: the validated options, the camera, and the chosen format.

`CameraFrame` (immutable): `Sequence` (the frame number of the camera), `CapturedAt` (wall-clock arrival time), `Age`
(monotonic time since arrival, computed on every read), `EncodedData` (the bytes of a complete image file, shared and
read-only), and `ToImage()`, which decodes on demand through the Imaging subsystem.

`CameraOpenOptions` (immutable record): the requested `Width`, `Height`, and `FramesPerSecond` (all optional), `WarmUp`
(default 1000 ms), `FirstFrameTimeout` (default 5 s), `StaleFrameTimeout` (default 5 s), and `MaxDuration` (one hour).
Its internal `Validate` method rejects a size or rate that is not positive, a rate that is not finite, a negative
warm-up, a wait that is not positive, and any duration above one hour.

Exceptions: `CameraException` (abstract base); `CameraNotFoundException` (selector and the list of available cameras);
`CameraOpenException`; `CameraDisconnectedException`; `CameraTimeoutException`. Each message is written for a person
and names the camera or the selector.

#### Key Methods

**Snapshot**: Returns the newest frame.

- *Postconditions*: the returned frame is the latest frame at the moment of the read; its age does not exceed the
  stale-frame timeout.
- *Algorithm*: throw `ObjectDisposedException` when closed and `CameraDisconnectedException` when the device failed;
  read the latest frame; if there is none, wait (see below); throw `CameraDisconnectedException` when the frame is
  older than the stale-frame timeout; return the frame.
- *Waiting*: loop until the deadline (warm-up plus first-frame timeout after the start of streaming): wait on the event
  for the time that remains; after every wake-up check closed, failed, and a published frame in that order; when
  the deadline has passed without a frame throw `CameraTimeoutException`.

**Dispose**: Idempotent; the first call sets the disposed flag, signals the event, disposes the device handle, and
clears the held frame. It returns after the device has stopped.

**Start** (internal): records the device handle and the start timestamp and sets the streaming flag.

**OnFrameArrived** (internal): the device callback; ignores frames when closed or not yet streaming and frames inside
the warm-up; otherwise publishes a new `CameraFrame` and signals the event.

**OnFaulted** (internal): records the first failure and signals the event.

#### Error Handling

See *Snapshot*. `Dispose` throws nothing of its own; the device handle's `Dispose` is required not to throw.
`OnFrameArrived` and `OnFaulted` reject null arguments. A frame that arrives after disposal is ignored.

#### Dependencies

- **EncodedFrame** and **CameraInfo** and **CameraFormat** of the Cameras subsystem, and **CameraImage** of the Imaging
  subsystem (through `CameraFrame.ToImage`).

#### Callers

`CameraOpener` (creation and callbacks), `CameraSnapper`, and every application that holds a session.
