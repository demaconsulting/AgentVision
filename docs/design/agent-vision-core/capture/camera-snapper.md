### CameraSnapper

![Capture Structure](CaptureView.svg)

#### Purpose

`CameraSnapper` takes one picture with one call: it opens a camera, lets it warm up, takes one frame, optionally crops
it, saves it, and closes the camera. It exists so that the command-line tool stays a thin front-end and so that the
cleanup is written once: whatever goes wrong, the camera is closed. It is the cold path; applications that need many
pictures keep a `CameraSession` instead. It also owns the request and result values, `SnapRequest` and `SnapResult`.

#### Data Model

- **source** (private read-only field): the `ICameraSource` that lists and opens the cameras.
- **SnapRequest** (record): `Camera` (selector text), `OutputPath`, `Crop` (`CropRectangle?`), and `Options`
  (`CameraOpenOptions`, default options when not set).
- **SnapResult** (record): `OutputPath`, `Width` and `Height` of the saved picture, the `Camera`, and the `Format` that
  the camera streamed in.

#### Key Methods

**Snap**: Takes one picture and saves it.

- *Preconditions*: the request and its texts are not null.
- *Postconditions*: the camera is closed; the file exists when the method returns normally.
- *Algorithm*:
  1. Check what does not need the camera: the options; the output path (not empty, supported extension, existing
     folder); the crop (a position that is not negative and a size that is positive).
  2. List the cameras, select the camera and the format with `CameraSelector`, and check that the crop fits the size of
     that format; a crop that does not fit fails here, before the camera is touched.
  3. Open the camera with `CameraOpener` (the session is declared with a `using`, so it is disposed on every path), and
     take a snapshot; the first snapshot waits for the warm-up.
  4. Decode the frame, crop it (the crop is checked again against the real decoded size, because a camera can deliver
     a size other than the one it announced), and save it by the extension of the output path.
  5. Return the result.

#### Error Handling

Argument problems give the standard argument exceptions (`ArgumentNullException`, `ArgumentException`,
`ArgumentOutOfRangeException`, `DirectoryNotFoundException`) before the camera is touched. Camera problems give
`CameraNotFoundException`, `CameraOpenException`, `CameraTimeoutException`, or `CameraDisconnectedException`. A frame
that cannot be decoded gives `InvalidDataException`, and a file that cannot be written gives `IOException` or
`UnauthorizedAccessException`. All of them propagate unchanged; the unit never swallows a failure, and the camera is
closed in every case.

#### Dependencies

- **CameraSelector**, **CameraOpener**, **CameraSession**, **CameraOpenOptions**, and the exceptions of this subsystem.
- **CameraEnumerator** and **ICameraSource** of the Cameras subsystem, and **FlashCapCameraSource** (only in the
  default constructor).
- **CameraImage** and **CropRectangle** of the Imaging subsystem.

#### Callers

Any application that wants one picture. The command-line tool is intended to call it for a `snap` command, which is
added separately and is not part of this design.
