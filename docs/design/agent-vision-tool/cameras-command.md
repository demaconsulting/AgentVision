## CamerasCommand

![AgentVisionTool Structure](AgentVisionToolView.svg)

### Purpose

`CamerasCommand` implements the `cameras` command of the tool. Its single responsibility is to print the cameras that
the AgentVisionCore library lists, and the video formats each supports, through the `Context` output channels. It is a
thin front-end: discovering the cameras, ordering them, and choosing the formats belong to the library, so the unit
contains no camera logic.

### Data Model

`CamerasCommand` holds no state. The class is `internal static`; all data is local to `Run` and its private helpers.

### Key Methods

**Run (context)**: Lists the cameras of the host.

- *Parameters*: `Context context` — output target.
- *Returns*: `void`.
- *Preconditions*: `context` is not null.
- *Postconditions*: The listing, or the message that no camera was found, has been written to `context`; no error
  has been reported.

Creates a `CameraEnumerator` with the default source and calls the overload below.

**Run (context, enumerator)**: Lists the cameras that the given enumerator reports. The overload exists so that
tests can list cameras from a source that needs no hardware.

- *Parameters*: `Context context` — output target; `CameraEnumerator enumerator` — the enumerator to list.
- *Returns*: `void`.
- *Preconditions*: neither argument is null.
- *Postconditions*: As above.

Output, for no camera:

```text
No cameras found.
```

and for one or more cameras (the index is the position in the library's list):

```text
Cameras found: 1

Camera 0: Integrated Webcam
  Id:          DirectShow:\\?\usb#vid_0c45&pid_674f&mi_00#...
  Backend:     DirectShow
  Description: Integrated Webcam (DirectShow)
  Formats (2):
    1920x1080  JPEG  30 fps
    640x480    YUYV  30 fps
```

The size and pixel-format columns are padded to the longest entry of the camera so that the formats line up. The
frame rate is written with the invariant culture, with at most three decimals and no trailing zeros (30, 29.97). A
camera without a format that can be captured prints `Formats:     none that can be captured`. The command writes
through `Context.WriteLine`, so `--silent` and `--log` apply.

### Error Handling

A null argument is rejected with `ArgumentNullException`. Finding no camera is a normal result: the message is
written and no error is reported, so the exit code stays 0. Failures of the library, such as an exception inside the
capture library, are not caught: they propagate to `Program.Main`, which writes them to stderr as an unexpected error
and re-throws them.

### Dependencies

- **CameraEnumerator**, **CameraInfo**, and **CameraFormat** of the AgentVisionCore library (see *Cameras Design*):
  the list that is printed.
- **Context** — the output channels `WriteLine`, which honor `--silent` and `--log`.

### Callers

- **Program** — calls `Run(context)` when the `cameras` command is given.
