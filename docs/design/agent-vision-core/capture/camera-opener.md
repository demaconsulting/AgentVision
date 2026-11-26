### CameraOpener

![Capture Structure](CaptureView.svg)

#### Purpose

`CameraOpener` opens cameras. It turns a request ("camera 1 at about 1280x720") into a streaming `CameraSession`: it
checks the options, selects the camera and the format, starts the device through the camera source, and translates
failures into the library's exceptions. One opener opens any number of cameras, one session each; the sessions are
independent of each other.

#### Data Model

- **source** (private read-only field): the `ICameraSource` that lists and opens the cameras. The default constructor
  sets it to a `FlashCapCameraSource`.

The opener holds no other state; it is as thread-safe as its source.

#### Key Methods

**Constructor (default)** and **Constructor (source)**: Create the opener with the default source or with a given one;
the given source must not be null.

**Open (selector)**: Opens the camera that a selector names.

- *Parameters*: the selector (index, identifier, or name) and optional `CameraOpenOptions` (null means the defaults).
- *Returns*: a streaming `CameraSession`; the caller owns and disposes it.
- *Algorithm*: validate the options; list the cameras through a `CameraEnumerator` over the same source, so that an
  index means what the camera listing shows; select the camera with `CameraSelector.SelectCamera`; continue as in
  *Open (camera)*.

**Open (camera)**: Opens a described camera.

- *Returns*: a streaming `CameraSession`.
- *Algorithm*: validate the options; choose the format with `CameraSelector.SelectFormat`; create the session (which
  is not streaming yet) so that the device can be given the session's frame and fault callbacks; call
  `ICameraSource.Open`; on success tell the session that streaming has started, which starts the warm-up clock, and
  return it.

#### Error Handling

A null camera or selector gives `ArgumentNullException`. An invalid option gives `ArgumentOutOfRangeException` before
the source is asked anything; the parameter name is `options` and the message names the option. A selector without a
match gives `CameraNotFoundException`. A failure of the source is translated: an exception of the library's own
hierarchy passes unchanged, and any other exception (except out-of-memory) becomes a `CameraOpenException` that names
the camera and keeps the original as the inner exception. In every failure the session that was created is disposed,
so no device is left open.

#### Dependencies

- **CameraSelector**, **CameraSession**, and **CameraOpenOptions** of this subsystem.
- **ICameraSource** and **CameraEnumerator** of the Cameras subsystem, and **FlashCapCameraSource** (only in the
  default constructor).

#### Callers

`CameraSnapper`, and any application that references the library.
