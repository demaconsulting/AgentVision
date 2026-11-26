# Introduction

The workflow below is a placeholder: replace its TODO boxes to show how users prepare inputs, run the
tool locally or in a CI pipeline, get its outputs, and use them.

![Placeholder workflow diagram: replace the TODO boxes to show how users prepare inputs, run the tool, get
its outputs, and use them](workflow.svg)

## Purpose

AgentVision gives AI agents fast access to webcams. Its goal is low-latency webcam capture: keeping
multiple cameras open ("hot") so that frames can be captured, cropped, and saved with almost no delay. It is
delivered as two packages:

- **`DemaConsulting.AgentVision.Core`**: a .NET library that owns all camera logic and can be used by any
  .NET application.
- **`DemaConsulting.AgentVision.Tool`**: a .NET tool, command `agentvision`, that exposes AgentVision from the
  command line. It is a thin front-end for the library.

AgentVision is under active development. Listing the cameras of a machine and the video formats they support is
available in both packages. The library can also keep cameras open and hand out their frames, crop them, and save them
as PNG, JPEG, or BMP files; the command-line tool does not capture frames yet, and this guide documents only what exists
in the current release.

## Scope

This user guide covers:

- Installation instructions for the tool and the library
- Listing cameras with the tool and with the library
- Capturing frames with the library: opening cameras, snapshots, cropping, saving, options, and errors
- Usage examples for common tasks
- The built-in `agentvision help` command
- Command-line options reference
- Practical examples for various scenarios

# Continuous Compliance

AgentVision follows the
[Continuous Compliance](https://github.com/demaconsulting/ContinuousCompliance) methodology, which ensures
compliance evidence is generated automatically on every CI run.

## Key Practices

- **Requirements Traceability**: Every requirement is linked to passing tests, and a trace matrix is
  auto-generated on each release
- **Linting Enforcement**: LintMark, TextLint, and yamllint are enforced before any build proceeds
- **Automated Audit Documentation**: Each release ships with generated requirements, justifications,
  trace matrix, and quality reports
- **CodeQL and SonarCloud**: Security and quality analysis runs on every build

# Installation

AgentVision runs on Windows and Linux with .NET 10 or .NET 11.

Install the tool globally using the .NET CLI:

```bash
dotnet tool install -g DemaConsulting.AgentVision.Tool
```

Add the library to a .NET project with:

```bash
dotnet add package DemaConsulting.AgentVision.Core
```

# Usage

## Display Version

Display the tool version:

```bash
agentvision --version
```

## Default Behavior

When invoked with no arguments, the tool displays the banner and copyright notice, followed by one line of
guidance that points to the usage text and the help topics. This is not an error, and the exit code is 0:

```bash
agentvision
```

## Listing Cameras

List the video capture devices that the machine reports, and the video formats each one supports:

```bash
agentvision cameras
```

The command only looks at the devices; it does not start capturing from any of them, so it does not switch a camera on
or disturb a program that is using it. The first line reports how many cameras were found, and each camera is then
listed under its index, which is its position in the list:

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

Each camera shows:

- **Id**: the stable identifier, made of the backend and the identity the backend gives the device. Use it, not
  the index, to refer to a camera, because the index can change when cameras are attached or removed.
- **Backend**: the part of the operating system that reported the camera: `DirectShow`, `MediaFoundation`, or
  `VideoForWindows` on Windows, and `V4L2` on Linux.
- **Description**: the description that the backend reports.
- **Formats**: every way to capture video that the camera offers: the frame size, the pixel format, and the frame
  rate in frames per second, the largest and fastest first. Formats that cannot be captured are not listed.

Windows can report one physical camera through several backends, so the same camera can appear more than once,
each time with its own `Backend` and `Id`; the Video for Windows entry named `Default` is not a real device. The
tool does not merge these entries.

### No Camera Found

When the machine has no camera, the command prints `No cameras found.` and exits with code 0, because finding no
camera is a normal answer on a server or a build agent, and not an error. If you expected a camera, check that:

1. The camera is attached and enabled, and the operating system shows it in its own device settings.
2. No other program holds the camera, such as a video call. Close it and list again.
3. On Linux, the device nodes `/dev/video*` exist, and your account may read and write them. Add your account to
   the `video` group and sign in again.
4. A virtual machine or container has the camera passed through to it.

The listing follows `--silent` and `--log`, so a script can keep it in a file:

```bash
agentvision --silent --log cameras.log cameras
```

## Using the Library

The `DemaConsulting.AgentVision.Core` library lists the same cameras to any .NET application. `CameraEnumerator`
returns plain values, `CameraInfo` for each camera and `CameraFormat` for each format, and no type of the capture
library appears in its public API:

```csharp
using DemaConsulting.AgentVision.Core.Cameras;

var cameras = new CameraEnumerator().Enumerate();
if (cameras.Count == 0)
{
    Console.WriteLine("No cameras found.");
}

foreach (var camera in cameras)
{
    Console.WriteLine($"{camera.Id}: {camera.Name}");
    foreach (var format in camera.Formats)
    {
        Console.WriteLine($"  {format.Width}x{format.Height} {format.PixelFormat} {format.FramesPerSecond} fps");
    }
}
```

`Enumerate` returns an empty list, not an exception, when there is no camera. The cameras are ordered by backend and
identifier, and the formats largest and fastest first, so the same cameras give the same list every time. The list
is a snapshot: call `Enumerate` again to see cameras that were attached or removed.

An application, or a test, can supply its own source of camera descriptions, so that code that depends on the list
runs without camera hardware:

```csharp
public sealed class FixedCameraSource : ICameraSource
{
    public IReadOnlyList<CameraInfo> GetCameras() =>
        [new CameraInfo("Custom:1", "Test Camera", "Test Camera", "Custom", [new CameraFormat(640, 480, "YUYV", 30)])];

    // Only needed to capture; a source that is only listed can refuse it
    public IDisposable Open(CameraInfo camera, CameraFormat format, Action<EncodedFrame> frameArrived, Action<Exception> faulted) =>
        throw new NotSupportedException();
}

var cameras = new CameraEnumerator(new FixedCameraSource()).Enumerate();
```

A source can also stand in for a camera: its `Open` method starts delivering frames (each frame is the bytes of a JPEG,
PNG, or BMP file) to the callback it is given, and returns a handle whose disposal stops them. Pass it to
`new CameraOpener(source)` to run capture code without camera hardware.

## Capturing Frames with the Library

The library keeps a camera open ("hot"): opening a camera takes a second or more, but once it is open, taking a picture
is a read of the newest frame that is already in memory. An application that needs many pictures opens each camera once,
takes snapshots as often as it likes from any thread, and disposes the session when it is done. The camera is released
as soon as the session is disposed.

### Opening a Camera and Taking a Snapshot

```csharp
using DemaConsulting.AgentVision.Core.Capture;

var opener = new CameraOpener();

// "0" is the index that the camera listing shows; an identifier or a name works too
using var session = opener.Open("0");

// The first snapshot waits for the warm-up (one second by default); later snapshots return at once
CameraFrame frame = session.Snapshot();

// The frame is the bytes of a JPEG, PNG, or BMP file: forward them as they are, or decode them into an image
using var image = frame.ToImage();
image.Save("desk.png");
```

`Open` accepts the index of the camera as `agentvision cameras` lists it (a number), the identifier of the camera (the
stable form), or the name of the camera (letter case does not matter). A number is always an index. When Windows
reports one camera through several backends under one name, the first backend of the list is used, and two cameras of
one backend that share a name must be told apart by index or identifier.

A `CameraFrame` is immutable and stays valid after the session is disposed. `Sequence` is the frame number, so two
snapshots with the same number are the same frame; `Age` says how long ago the frame arrived; and `ToImage()` decodes
it (tens of milliseconds for a full-HD picture) on the thread that calls it, so a service can forward the
`EncodedData` bytes without decoding them at all.

### Cropping and Saving

```csharp
using DemaConsulting.AgentVision.Core.Imaging;

using var image = session.Snapshot().ToImage();

// Keep a region: x, y, width, height in pixels from the top-left corner
using var detail = image.Crop(new CropRectangle(100, 50, 320, 240));

detail.Save("detail.png");   // lossless
detail.Save("detail.jpg");   // small (quality 90); .jpeg works too
detail.Save("detail.bmp");   // uncompressed
```

The extension chooses the format (`.png`, `.jpg`, `.jpeg`, and `.bmp`, in any letter case), and any other extension is
refused with a message that lists the supported ones. A crop returns a new image with its own pixels, so the original
can be cropped again, and a region that does not fit inside the picture is refused with a message that names the region
and the size of the picture. `Save(Stream, ImageFileFormat)` writes to a stream instead of a file.

### One Picture with One Call

When one picture is all that is needed, `CameraSnapper` opens the camera, waits for the warm-up, saves the picture, and
closes the camera, also when something fails:

```csharp
var snapper = new CameraSnapper();
var result = snapper.Snap(new SnapRequest("0", "desk.jpg")
{
    Crop = new CropRectangle(100, 50, 320, 240)   // optional
});

Console.WriteLine($"Saved {result.OutputPath} ({result.Width}x{result.Height}) from {result.Camera.Name}");
```

Everything that can be checked without the camera (the file name and extension, the folder, the options, and the crop
against the size of the format that will be used) is checked before the camera is started.

### Several Cameras at Once

Open each camera with the same opener; the sessions are independent, and closing one leaves the others streaming:

```csharp
var opener = new CameraOpener();
using var desk = opener.Open("Desk Camera");
using var door = opener.Open("1");

var deskFrame = desk.Snapshot();
var doorFrame = door.Snapshot();
```

A physical camera that Windows reports through several backends can only be opened once at a time; open it through one
entry.

### Choosing the Format, the Warm-Up, and the Timeouts

Every option is optional. The size and the frame rate are preferences: the camera is opened with the format that comes
closest, and `session.Format` tells which format was chosen.

```csharp
var options = new CameraOpenOptions
{
    Width = 1280,                                   // requested frame size (nearest size wins)
    Height = 720,
    FramesPerSecond = 30,                           // requested frame rate (not below, then closest)
    WarmUp = TimeSpan.FromMilliseconds(1500),       // default 1000 ms; zero disables it
    FirstFrameTimeout = TimeSpan.FromSeconds(10),   // default 5 s: wait for the first frame after the warm-up
    StaleFrameTimeout = TimeSpan.FromSeconds(5)     // default 5 s: a frame this old means the camera stopped
};

using var session = new CameraOpener().Open("0", options);
```

| Option | Default | Meaning |
| --- | --- | --- |
| `Width`, `Height` | none | Requested frame size in pixels. An unspecified dimension counts as a match |
| `FramesPerSecond` | none | Requested frame rate |
| `WarmUp` | 1000 ms | Frames that arrive during this time after streaming starts are discarded |
| `FirstFrameTimeout` | 5 s | How long the first snapshot waits for a frame after the warm-up |
| `StaleFrameTimeout` | 5 s | Age of the newest frame at which the camera counts as disconnected |

Without a request, the library chooses the largest size that is not above 1920x1080 and offers at least 15 frames per
second, then the highest frame rate, and JPEG when the camera offers it (it needs the least USB bandwidth). The warm-up
exists because most webcams deliver dark or unfocused frames for a moment after they start while exposure and white
balance settle; frames inside the warm-up are never served. Durations must not be negative or longer than one hour.

### Using the Library from Many Threads

`CameraOpener`, `CameraSession`, and `CameraFrame` are safe to use from many threads: any number of threads may call
`Snapshot` at the same time, even while frames arrive, and `Dispose` may be called from any thread. A `CameraImage` is
not thread-safe, so give each thread its own image. A background service typically opens its cameras at start,
checks `session.IsReady` to know when the first frame can be read without waiting, and disposes the sessions at stop.

### Errors

Failures are reported with exceptions of one family, `CameraException`, and the standard argument exceptions for
mistakes of the caller. Every message names the camera or the selector and says what to do next.

- `CameraNotFoundException`: no camera matches the index, identifier, or name. Read the message: it lists the
  available cameras (`Selector` and `Available` carry the same information).
- `CameraOpenException`: the camera cannot be opened or started, or offers no usable format. Check that no other
  application holds the camera and that you may use it; the inner exception has the details.
- `CameraTimeoutException`: the camera delivered no frame in time. Try a smaller format or a longer
  `FirstFrameTimeout`, and check that the camera is not covered.
- `CameraDisconnectedException`: the camera reported a failure, or stopped delivering frames. Dispose the session and
  open the camera again.
- `ObjectDisposedException`: the session was disposed. Open the camera again.
- `ArgumentException` and `ArgumentOutOfRangeException`: an option, an index, a crop, or a file name is invalid.
  Correct the argument.
- `InvalidDataException`: a frame is damaged and cannot be decoded. Take another snapshot.

### Notes and Troubleshooting

- **The first snapshot is slow.** It waits for the warm-up. Open the session early, or set `WarmUp` shorter if the
  camera settles quickly.
- **The picture is dark.** Lengthen the warm-up; some cameras need two seconds.
- **A camera cannot be opened.** Another application (a video call, the camera app) may hold it. Close that
  application and try again. On Linux the account must be allowed to use `/dev/video*`, normally through the
  `video` group.
- **Frames from a camera do not decode.** Many motion JPEG cameras omit the Huffman tables from their frames; the
  library inserts the standard tables, so those frames decode normally. A frame that is truly damaged raises an
  `InvalidDataException`.
- **Dispose every session.** An undisposed session keeps its camera, and its light, on until the process ends.
- **Hardware tests.** `pwsh ./build.ps1 -All -Hardware` also runs smoke tests against the cameras of the machine. A
  camera can be open in one program at a time, so the tests of the two target frameworks queue on a lock file in the
  temporary folder and run one after another, and nothing else (an `agentvision` process or another application) may
  use the cameras meanwhile.

## Display Help

Display usage information:

```bash
agentvision --help
```

The usage information ends with a pointer to the built-in help.

## Built-in Help

The `help` command prints help topics that explain each task with worked examples, for people and
for AI agents that have only the installed tool. List the topics, then print one with its examples:

```bash
agentvision help
agentvision help getting-started
```

Add `--search <words>` to find a topic, `--example <name>` to print one example of a topic, and
`--json` to get any help answer as JSON. `help` is a command, not an option: it must be the first
argument, and `--silent` and `--log` do not apply to it. It exits with code 0 on success, and with
code 2 after printing a hint for an unknown topic or a wrong help argument.

## Self-Validation

Self-validation produces a report demonstrating that AgentVision is functioning
correctly. This is useful in regulated industries where tool validation evidence is required.

### Running Validation

To perform self-validation:

```bash
agentvision --validate
```

To save validation results to a file:

```bash
agentvision --validate --results results.trx
```

The `--result` option is an accepted alias for `--results`.

The results file format is determined by the file extension: `.trx` for TRX (MSTest) format,
or `.xml` for JUnit format.

### Heading Depth

Use `--depth <#>` to control the heading level of the validation output (default: `1`).
This is useful when embedding the validation report into a larger markdown document:

```bash
# Embed validation at heading level 2
agentvision --validate --depth 2
```

### Validation Report

The validation report contains the tool version, machine name, operating system version,
.NET runtime version, timestamp, and test results.

Example validation report:

```text
# DEMA Consulting AgentVision Tool

| Information         | Value                                              |
| :------------------ | :------------------------------------------------- |
| Tool Version        | 1.0.0                                              |
| Machine Name        | BUILD-SERVER                                       |
| OS Version          | Ubuntu 22.04.3 LTS                                 |
| DotNet Runtime      | .NET 10.0.0                                        |
| Time Stamp          | 2024-01-15 10:30:00 UTC                            |

✓ AgentVisionTool_VersionDisplay - Passed
✓ AgentVisionTool_HelpDisplay - Passed
✓ AgentVisionTool_CamerasList - Passed

Total Tests: 3
Passed: 3
Failed: 0
```

### Validation Tests

Each test proves specific functionality works correctly:

- **`AgentVisionTool_VersionDisplay`** - `--version` outputs a valid version string.
- **`AgentVisionTool_HelpDisplay`** - `--help` outputs usage and options information.
- **`AgentVisionTool_CamerasList`** - `cameras` exits with code 0 and reports the cameras or that none were
  found. The test passes on a machine without a camera: it proves that the command works, not that a camera is
  present.

## Silent Mode

Suppress console output:

```bash
agentvision --silent
```

## Logging

Write output to a log file:

```bash
agentvision --log output.log
```

## Error Handling

Unrecognized arguments, and a command that is given twice, cause the tool to print an error message to standard
error and exit with a non-zero exit code. For example:

```text
Error: Unsupported argument '--unknown'
```

This behavior enables automated scripts and CI/CD pipelines to detect and surface
misconfiguration failures automatically. Argument errors are found before `--silent` and `--log`
take effect, so they are always printed and never logged.

# Command-Line Options

The following commands and command-line options are supported:

| Command | Description |
| --- | --- |
| `cameras` | List the available cameras and the formats they support |
| `help [topic]` | Display the built-in help topics |

| Option | Description |
| --- | --- |
| `-v`, `--version` | Display version information |
| `-?`, `-h`, `--help` | Display help message |
| `--silent` | Suppress console output |
| `--validate` | Run self-validation |
| `--results <file>`, `--result <file>` | Write validation results to file (TRX or JUnit format) |
| `--depth <#>` | Set heading depth for markdown output (default: 1) |
| `--log <file>` | Write output to log file |

# Examples

## Example 1: Basic Usage

```bash
agentvision
```

## Example 2: List the Cameras

```bash
agentvision cameras
```

## Example 3: Self-Validation with Results

```bash
agentvision --validate --results validation-results.trx
```

## Example 4: Silent Mode with Logging

```bash
agentvision --silent --log tool-output.log
```

## References

N/A
