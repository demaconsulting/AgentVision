### FlashCapCameraDevice

![Cameras Structure](CamerasView.svg)

#### Purpose

`FlashCapCameraDevice` is one FlashCap capture device that is streaming. It contains the part of capturing that
cannot be written without FlashCap: opening the device with a "latest frame only" strategy, copying each image out of
FlashCap's reusable buffer into an `EncodedFrame`, and stopping and releasing the device in a bounded time. Everything
that is policy (which camera, which format, warm-up, what a stale camera means) is in the Capture subsystem. The unit is
internal, so no FlashCap type crosses the library's boundary, and it is the owner of the public `EncodedFrame` value:
the bytes of a complete JPEG, PNG, or BMP file, the time the camera reports for the frame, and its frame number.

#### Data Model

- **gate** (private object): serializes the frame callback with disposal.
- **frameArrived**, **faulted** (private read-only delegates): the receivers of the frames and of the failures.
- **device** (`CaptureDevice?`): the FlashCap device, set once before the object is handed out.
- **disposed** (bool, under the gate): whether disposal has started.
- **OpenTimeout** (15 seconds) and **StopTimeout** (5 seconds): the bounds on opening and starting, and on each of the
  two steps of stopping.
- **EncodedFrame** (sealed record): `Data` (bytes), `Timestamp`, and `FrameIndex`.

#### Key Methods

**Open** (static): Opens a descriptor and starts the device.

- *Parameters*: the FlashCap descriptor, the video characteristics, the camera name (for messages), and the two
  callbacks.
- *Returns*: the streaming device; the caller disposes it.
- *Algorithm*: open the descriptor with automatic transcoding, no scattering, and a queue of one frame, which makes
  FlashCap drop frames while the callback is busy, so that only the newest frame is ever waiting; wait for the open
  for at most the open timeout; start the device and wait for that for at most the open timeout. A device that opened
  too late is disposed in the background. A device that opened but did not start is disposed before the failure is
  reported.

**OnPixelBufferArrived** (private): The FlashCap callback. Under the gate: return if disposed; copy the image of the
pixel buffer (FlashCap returns JPEG and PNG as they are, raw 24-bit and 32-bit frames as a bitmap file with a file
header, and YUV frames converted to a 24-bit bitmap file) and pass an `EncodedFrame` to the receiver. A failure of the
copy or of the receiver is passed to the fault receiver and never thrown into FlashCap.

**Dispose**: Idempotent and never throws. Under the gate set the disposed flag (this waits for a callback that is
running and shuts off later ones), then stop the device and dispose it, each step with a bounded wait whose failure is
ignored. It must not be called from inside a callback.

#### Error Handling

Failures to open or start (an exception from FlashCap or a timeout) become `CameraOpenException` with the camera name in
the message and the FlashCap exception as the inner exception. A timeout, or a task that FlashCap cancels because the
backend never answers (whose own message, "A task was canceled.", says nothing), is reported as a backend that did not
open (or start) the device within the open timeout. For the legacy VideoForWindows backend, which needs a window
message pump that a console application does not have, the message adds that this backend is often unsupported and that
the DirectShow or MediaFoundation entry of the same camera should be used. Cleanup failures are ignored on purpose: a
cleanup that
throws would hide the failure that led to it, and the device is being abandoned. FlashCap gives no unplug event on every
backend, so a camera that disappears is detected by the session as a silence of frames, not here.

#### Dependencies

- **FlashCap** (OTS): `CaptureDeviceDescriptor`, its open extension, `CaptureDevice` with its start and stop,
  `PixelBufferScope`, `PixelBuffer` and its image copy, `TranscodeFormats`, and `VideoCharacteristics`. See *FlashCap
  Integration Design*.
- **CameraOpenException** of the Capture subsystem.

#### Callers

`FlashCapCameraSource.Open`.
