## SnapCommand

![AgentVisionTool Structure](AgentVisionToolView.svg)

### Purpose

`SnapCommand` implements the `snap` command of the tool. Its single responsibility is to turn the options of the
`Context` into one request for the AgentVisionCore library's `CameraSnapper`, run it, and report the saved picture or
the error through the `Context` output channels, so that the exit code says whether the picture was saved. It is a
thin front-end: choosing the camera and the format, the warm-up, decoding, cropping, saving, and closing the camera
belong to the library, so the unit contains no camera logic. It keeps no state about the run and does not depend on
how it was called, so another host of the command (a background service that keeps its own `CameraSnapper`) can call
the same `Run` overload.

### Data Model

`SnapCommand` holds no state. The class is `internal static`; all data is local to `Run` and its private helpers.

### Key Methods

**Run (context)**: Takes one picture with the cameras of the host.

- *Parameters*: `Context context` — the options and the output target.
- *Returns*: `void`.
- *Preconditions*: `context` is not null and has the `--camera` and `--output` values.
- *Postconditions*: Either the line `Saved <file> (<width>x<height>) from <camera name>` has been written, or an error
  has been reported and the exit code of `context` is 1.

Creates a `CameraSnapper` that uses the cameras of the host and calls the overload below.

**Run (context, snapper)**: Takes one picture with the given snapper. The overload exists so that tests can take a
picture from a source that needs no hardware.

- *Parameters*: `Context context` — the options and the output target; `CameraSnapper snapper` — the snapper to use.
- *Returns*: `void`.
- *Preconditions*: neither argument is null; `context` has the camera and the output file.
- *Postconditions*: As above. The snapper has closed the camera before the method returns.

The method maps the options to a `SnapRequest`: the camera and output file; the crop; a `CameraOpenOptions` with the
width, height, and frame rate, and, only when `--warmup` was given, the warm-up in milliseconds (options that were not
given keep the library defaults). It calls `CameraSnapper.Snap` and writes the result line through `Context.WriteLine`.

Output, on success:

```text
Saved frame.png (1280x720) from Integrated Webcam
```

### Error Handling

A null argument is rejected with `ArgumentNullException`. A context without the camera or the output file is rejected
with `InvalidOperationException`; a context created from a command line that asks for the command always has them.

The problems that a user can cause or meet are expected: the library's `CameraException` family (camera not found,
cannot be opened, delivers no frame in time, fails while it is used), `ArgumentException` (unsupported extension, a
name that several cameras share, a crop outside the picture, an invalid option), `IOException` (also a missing
folder), `UnauthorizedAccessException`, and `InvalidDataException` (a damaged frame). Each is written as
`Error: <message>` through `Context.WriteError`, which sets the exit code to 1 and writes to the error channel, the
log file, and, unless `--silent` is given, the console. The runtime appends ` (Parameter '<name>')` to the message of
an `ArgumentException`; the name is a parameter of the library and means nothing on the command line, so it is removed.
Any other exception is a defect: it propagates to `Program.Main`, which writes it to stderr as an unexpected error and
re-throws it, as for the other commands.

### Dependencies

- **CameraSnapper**, **SnapRequest**, **SnapResult**, **CameraOpenOptions**, and the **CameraException** types of the
  AgentVisionCore library (see *Capture Design*): the camera work and its errors.
- **Context** — the options (`Camera`, `OutputFile`, `Crop`, `Width`, `Height`, `Fps`, `WarmupMilliseconds`) and the
  output channels `WriteLine` and `WriteError`, which honor `--silent` and `--log`.

### Callers

- **Program** — calls `Run(context)` when the `snap` command is given.
