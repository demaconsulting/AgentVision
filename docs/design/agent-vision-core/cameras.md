## Cameras

![Cameras Structure](CamerasView.svg)

### Overview

The `Cameras` subsystem lists the video capture devices of the host and the video formats each one supports, and it
starts a camera streaming so that the Capture subsystem can take its frames. It separates two responsibilities so that
each can change and be tested on its own: *discovering and driving* the devices, which depends on the operating system
and on the FlashCap OTS library, and *listing* them, which is policy.

The subsystem contains three units:

- `CameraEnumerator` owns the policy of the camera list (copy, order, de-duplicate, empty is valid) and the public
  camera types `CameraInfo`, `CameraFormat`, and `ICameraSource`.
- `FlashCapCameraSource` is the default source: it translates what FlashCap describes into the camera types and opens a
  described camera.
- `FlashCapCameraDevice` (internal) is one streaming FlashCap device: it delivers each frame as an `EncodedFrame`
  (a public value type that it owns) and stops the device deterministically. `FlashCapCameraSource` and
  `FlashCapCameraDevice` are the only units that reference FlashCap.

The subsystem keeps no state between calls except the devices that it was asked to open, each of which belongs to the
caller of `Open`.

### Interfaces

**CameraEnumerator.Enumerate**: Lists the cameras that are present now.

- *Type*: In-process .NET instance method; part of the public API.
- *Role*: Provider.
- *Contract*: Returns a read-only list of `CameraInfo`, ordered by backend and then identifier, each with its
  `CameraFormat` list ordered largest and fastest first. The list is empty, not an error, when there is no camera. A
  camera's position in the list is its index; its `Id` is its stable identifier.
- *Constraints*: Failures of the source propagate unchanged.

**ICameraSource**: The seam through which cameras are described and opened.

- *Type*: In-process .NET interface; part of the public API.
- *Role*: Consumer (the enumerator and the Capture subsystem call it); applications and tests may provide it.
- *Contract*: `GetCameras` returns the cameras that are available now, in any order, as an empty list when there are
  none. `Open` starts a camera in a given format, delivers every frame of it to a callback as an `EncodedFrame`, and
  returns a handle whose disposal stops the camera and returns when no callback can run any more; it throws
  `CameraOpenException` when the camera cannot be opened.
- *Constraints*: No FlashCap type appears in the interface or in the camera types.

**FlashCap**: The OTS library that discovers and drives the devices.

- *Type*: In-process .NET library calls.
- *Role*: Consumer (only `FlashCapCameraSource` and `FlashCapCameraDevice`).
- *Contract*: See *FlashCap Integration Design*.
- *Constraints*: FlashCap types never leave these two units.

### Design

`CameraEnumerator` holds an `ICameraSource`. Its default constructor creates a `FlashCapCameraSource`, which is the
only place where the default capture library is named; a second constructor accepts any other source, which is how
applications and tests supply cameras without hardware. `Enumerate` asks the source on every call, because cameras can
be attached and removed while an application runs, then copies and orders what the source returned.

`FlashCapCameraSource` asks FlashCap for its device descriptors and maps each one to a `CameraInfo`: the identifier
is the backend name, a colon, and the backend's own identity (an identity is only unique within its backend); the
characteristics FlashCap marks as unknown are dropped, and the rest become `CameraFormat` values. A device with no
usable format is still reported, with an empty format list, so that the user can see that it exists.

To open a camera, `FlashCapCameraSource` asks FlashCap for the descriptors again, finds the one with the identifier of
the camera and the characteristics that match the chosen format exactly, and gives them to `FlashCapCameraDevice`, which
opens and starts the device and delivers its frames. FlashCap is asked to keep at most one frame waiting and to drop
frames while the callback is busy: the library only ever wants the newest frame, so a slow consumer never builds a
backlog.

**Known limitation.** FlashCap reports one entry per backend. On Windows one physical camera usually appears three
times (DirectShow, Media Foundation, and Video for Windows), and the Video for Windows entry named `Default` is not a
real device. Every entry is reported with its backend, and nothing is merged. The Video for Windows backend is a legacy
backend that needs a window message pump, so FlashCap usually cannot open it from a console application (the open times
out); use the DirectShow or Media Foundation entry of the same camera. Choosing between the entries when a
camera is selected by name belongs to `CameraSelector` of the Capture subsystem (the first backend of the list wins),
and a physical camera cannot be opened through two backends at the same time.

**Collaboration.** The caller creates a `CameraEnumerator` and calls `Enumerate`; the enumerator calls
`ICameraSource.GetCameras`; the default source calls FlashCap; the enumerator orders the result. To capture, the
`CameraOpener` of the Capture subsystem calls `ICameraSource.Open`; the default source finds the device and the
`FlashCapCameraDevice` streams it. The enumerator and the source are stateless, so the subsystem is as thread-safe as
the source it uses; every `Open` call produces an independent device.
