## Cameras

![Cameras Structure](CamerasView.svg)

### Overview

The `Cameras` subsystem lists the video capture devices of the host and the video formats each one supports. It is the
first capability of the AgentVisionCore library and the foundation for the capabilities that open a camera, which are
added later. It separates two responsibilities so that each can change and be tested on its own: *discovering* the
devices, which depends on the operating system and on the FlashCap OTS library, and *listing* them, which is policy.

The subsystem contains two units:

- `CameraEnumerator` owns the policy of the camera list (copy, order, de-duplicate, empty is valid) and the public
  camera types `CameraInfo`, `CameraFormat`, and `ICameraSource`.
- `FlashCapCameraSource` is the default source: the only unit that references FlashCap. It translates what FlashCap
  describes into the camera types.

The subsystem keeps no state between calls, opens no camera, and starts no capture.

### Interfaces

**CameraEnumerator.Enumerate**: Lists the cameras that are present now.

- *Type*: In-process .NET instance method; part of the public API.
- *Role*: Provider.
- *Contract*: Returns a read-only list of `CameraInfo`, ordered by backend and then identifier, each with its
  `CameraFormat` list ordered largest and fastest first. The list is empty, not an error, when there is no camera. A
  camera's position in the list is its index; its `Id` is its stable identifier.
- *Constraints*: Failures of the source propagate unchanged.

**ICameraSource.GetCameras**: The seam through which cameras are described.

- *Type*: In-process .NET interface method; part of the public API.
- *Role*: Consumer (the enumerator calls it); applications and tests may provide it.
- *Contract*: Returns the cameras that are available now, in any order, as an empty list when there are none.
- *Constraints*: No FlashCap type appears in the interface or in the camera types.

**FlashCap**: The OTS library that discovers the devices.

- *Type*: In-process .NET library calls.
- *Role*: Consumer (only `FlashCapCameraSource`).
- *Contract*: See *FlashCap Integration Design*.
- *Constraints*: FlashCap types never leave `FlashCapCameraSource`.

### Design

`CameraEnumerator` holds an `ICameraSource`. Its default constructor creates a `FlashCapCameraSource`, which is the
only place where the default capture library is named; a second constructor accepts any other source, which is how
applications and tests supply cameras without hardware. `Enumerate` asks the source on every call, because cameras
can be attached and removed while an application runs, then copies and orders what the source returned.

`FlashCapCameraSource` asks FlashCap for its device descriptors and maps each one to a `CameraInfo`: the identifier
is the backend name, a colon, and the backend's own identity (an identity is only unique within its backend); the
characteristics FlashCap marks as unknown are dropped, and the rest become `CameraFormat` values. A device with no
usable format is still reported, with an empty format list, so that the user can see that it exists.

**Known limitation.** FlashCap reports one entry per backend. On Windows one physical camera usually appears three
times (DirectShow, Media Foundation, and Video for Windows), and the Video for Windows entry named `Default` is not a
real device. Every entry is reported with its backend, and nothing is merged. Choosing between the entries belongs to
the code that opens a camera, which a later change adds.

**Collaboration.** The caller creates a `CameraEnumerator` and calls `Enumerate`; the enumerator calls
`ICameraSource.GetCameras`; the default source calls FlashCap; the enumerator orders the result. Both units are
stateless, so the subsystem is as thread-safe as the source it uses.
