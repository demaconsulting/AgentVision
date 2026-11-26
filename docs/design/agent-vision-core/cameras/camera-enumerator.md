### CameraEnumerator

![Cameras Structure](CamerasView.svg)

#### Purpose

`CameraEnumerator` turns what an `ICameraSource` reports into the list that applications see, and owns the policy of
that list: it takes a private copy, orders the cameras and their formats, removes duplicate formats, and treats an
empty answer as valid. It also owns the public camera types, which are plain values, so that applications can list,
compare, and choose cameras without referencing the capture library that found them.

#### Data Model

- **CameraFormat** (record): `Width` and `Height` (pixels), `PixelFormat` (name such as `JPEG`, `YUYV`, or `NV12`),
  and `FramesPerSecond` (`double`, zero when the camera reports no rate). Two formats are equal when all four values
  are equal.
- **CameraInfo** (record): `Id` (stable identifier, the backend name and the backend's identity joined by a colon),
  `Name`, `Description`, `Backend` (`DirectShow`, `MediaFoundation`, `VideoForWindows`, or `V4L2`), and `Formats`
  (read-only list of `CameraFormat`, empty when none can be captured). Record equality compares `Formats` by
  reference.
- **ICameraSource** (interface): two methods: `GetCameras`, which returns the cameras available now, and `Open`, which
  starts a camera streaming in a format and delivers its frames to a callback (see *FlashCapCameraSource*; the
  enumerator itself only uses `GetCameras`).
- **CameraEnumerator** (class): one read-only field, the source. No other state.

Invariants: the list returned by `Enumerate` is never null, and its cameras and formats are in the documented order
without duplicate formats.

#### Key Methods

**Constructor (default)**: Creates the enumerator with the default source, a `FlashCapCameraSource`.

**Constructor (source)**: Creates the enumerator with the given source.

- *Parameters*: `ICameraSource source` - must not be null.
- *Preconditions*: `source` is not null.
- *Postconditions*: Later `Enumerate` calls use `source`.

**Enumerate**: Lists the cameras that are available now.

- *Returns*: `IReadOnlyList<CameraInfo>`, never null.
- *Postconditions*: The result does not share any list with the source.

Algorithm: ask the source for its cameras; for each camera, order its formats by pixel count (width times height)
descending, then frames per second descending, then pixel format name in ordinal order, after removing duplicates; sort
the cameras by backend and then identifier, both with the ordinal comparison, so the order is the same on every machine
and culture; return the sorted copy as a read-only list. The sort is performed on the private copy, never on the
source's list.

#### Error Handling

A null source is rejected by the constructor with `ArgumentNullException`. A source that returns null breaks its
contract; `Enumerate` throws `InvalidOperationException`. Any other exception of the source (for example a failure
inside the capture library) is not caught or translated and reaches the caller unchanged. An empty list is not an
error and is returned as is.

#### Dependencies

- **ICameraSource**: the source of the camera descriptions.
- **FlashCapCameraSource**: created by the default constructor only; the enumerator never uses a FlashCap type.

#### Callers

The `CamerasCommand` unit of the AgentVisionTool system, and any application that references the library.
