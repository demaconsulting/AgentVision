### FlashCapCameraSource

![Cameras Structure](CamerasView.svg)

#### Purpose

`FlashCapCameraSource` is the default `ICameraSource`. It asks FlashCap for the video capture devices of the host and
translates each FlashCap descriptor into the library's own camera types. It is the only unit that references FlashCap,
so that no FlashCap type ever crosses the `ICameraSource` boundary into the rest of the library or the public API.

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

FlashCap returns an empty set, not an error, when no device is attached, and so does this unit. Any other FlashCap
failure is not caught or translated; it reaches the caller unchanged. On Linux FlashCap silently skips video device
nodes that it cannot open (for example without permission), so such a node does not appear in the list.

#### Dependencies

- **FlashCap** (OTS): `CaptureDevices`, its `EnumerateDescriptors` extension, `CaptureDeviceDescriptor`,
  `VideoCharacteristics`, `PixelFormats`, and the frame-rate fraction type. See *FlashCap Integration Design*.
- **ICameraSource**: the interface this unit implements.

#### Callers

`CameraEnumerator`, through its default constructor.
