### FlashCapCameraSource

![Cameras Structure](CamerasView.svg)

#### Purpose

`FlashCapCameraSource` is the default `ICameraSource`. It asks FlashCap for the video capture devices of the host and
translates each FlashCap descriptor into the library's own camera types, and it opens a described camera by finding its
FlashCap device again and handing it to `FlashCapCameraDevice`. It is the only unit that finds FlashCap descriptors, so
that no FlashCap type ever crosses the `ICameraSource` boundary into the rest of the library or the public API.

#### Data Model

- **enumerate** (private read-only field): a function that returns the FlashCap descriptors of the devices that are
  present. The default constructor sets it to FlashCap's own enumeration; an internal constructor accepts another
  function so that tests can feed descriptors that belong to no real camera through the real translation.

The type holds no other state and does not cache between calls.

#### Key Methods

**Constructor (default)**: Creates a source that enumerates the devices of the host with FlashCap.

**Constructor (function)**: Internal. Creates a source that translates the descriptors the function returns.

- *Parameters*: `Func<IEnumerable<CaptureDeviceDescriptor>> enumerate` - must not be null.
- *Preconditions*: `enumerate` is not null.

**GetCameras**: Gets the cameras that FlashCap reports as present now.

- *Returns*: `IReadOnlyList<CameraInfo>`, in the order FlashCap reports them; empty when it reports none.

**Open**: Opens a camera and starts it streaming.

- *Parameters*: the `CameraInfo` as `GetCameras` described it, one of its `CameraFormat` values, the frame callback, and
  the fault callback; none may be null.
- *Returns*: an `IDisposable` handle that stops and releases the device.
- *Algorithm*: ask FlashCap for the descriptors again (the camera may have been unplugged since it was listed); find the
  descriptor whose identifier (the backend, a colon, and the identity) equals the camera's identifier; find the
  characteristic that is not unknown and translates to exactly the given format (size, pixel format name, and frame
  rate); call `FlashCapCameraDevice.Open` with them.

Mapping of a descriptor to a `CameraInfo`:

| Camera value | Taken from |
| --- | --- |
| `Backend` | the name of the descriptor's device type: `DirectShow`, `MediaFoundation`, `VideoForWindows`, or `V4L2` |
| `Id` | the backend, a colon, and the text of the descriptor's identity (empty text for a missing identity) |
| `Name`, `Description` | the descriptor's name and description |
| `Formats` | the descriptor's characteristics whose pixel format is not unknown |

Mapping of a characteristic to a `CameraFormat`: width and height as reported; pixel format as its name; frame rate
as the `double` value of FlashCap's exact fraction (for example 30000 over 1001 becomes 29.97), where a zero fraction
becomes zero. A pixel format that FlashCap reports as unknown is one it cannot capture, so the characteristic is
dropped; the device is still reported, with an empty format list when nothing remains.

#### Error Handling

FlashCap returns an empty set, not an error, when no device is attached, and so does `GetCameras`. Any other FlashCap
failure during enumeration is not caught or translated; it reaches the caller unchanged. On Linux FlashCap silently
skips video device nodes that it cannot open (for example without permission), so such a node does not appear in the
list. `Open` throws `CameraOpenException` that names the camera when FlashCap no longer lists a device with the
identifier of the camera, or the device no longer offers the format; failures of the device itself are reported by
`FlashCapCameraDevice`. A null argument gives `ArgumentNullException`.

#### Dependencies

- **FlashCap** (OTS): `CaptureDevices`, its `EnumerateDescriptors` extension, `CaptureDeviceDescriptor`,
  `VideoCharacteristics`, `PixelFormats`, and the frame-rate fraction type. See *FlashCap Integration Design*.
- **ICameraSource**: the interface this unit implements; **EncodedFrame** and **FlashCapCameraDevice**, the streaming
  unit that `Open` creates.
- **CameraOpenException** of the Capture subsystem.

#### Callers

`CameraEnumerator` and `CameraOpener` and `CameraSnapper`, through their default constructors.
