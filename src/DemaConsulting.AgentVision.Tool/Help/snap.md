---
title: Saving a picture
summary: Save one picture of a camera to a file, optionally cropped.
aliases:
  - capture
  - picture
  - photo
  - screenshot
tags:
  - tutorial
order: 4
see-also:
  - cameras
  - command-line
examples:
  - name: list-then-snap
    kind: snippet
    language: shell
    save-as: list-then-snap.sh
    description: Find the camera, then save one picture of it. Needs a camera.
    content: |
      {{tool}} cameras
      {{tool}} snap --camera 0 --output frame.png
  - name: crop
    kind: snippet
    language: shell
    save-as: crop.sh
    description: Keep only the region that starts at x=100, y=50 and is 400 by 300 pixels. Needs a camera.
    content: '{{tool}} snap --camera 0 --output region.jpg --crop 100,50,400,300'
  - name: format
    kind: snippet
    language: shell
    save-as: format.sh
    description: Prefer a 1280 by 720 picture at 30 frames per second. Needs a camera.
    content: '{{tool}} snap --camera "Integrated Webcam" --output hd.png --width 1280 --height 720 --fps 30'
  - name: warmup
    kind: snippet
    language: shell
    save-as: warmup.sh
    description: Let the camera settle for 2 seconds before the picture is taken. Needs a camera.
    content: '{{tool}} snap --camera 0 --output steady.png --warmup 2000'
  - name: missing-camera
    kind: command
    language: shell
    description: The command needs the camera to use. Exits 1.
    content: '{{tool}} snap --output frame.png'
    exit: 1
    expect: 'requires --camera'
  - name: unknown-camera
    kind: command
    language: shell
    description: A camera that does not exist is an error. Exits 1, and no file is written.
    content: '{{tool}} snap --camera 99 --output frame.png'
    exit: 1
    expect: 'not found'
  - name: bad-extension
    kind: command
    language: shell
    description: The extension of the output file chooses the format, and only some are supported. Exits 1.
    content: '{{tool}} snap --camera 0 --output frame.gif'
    exit: 1
    expect: 'not supported'
---
`{{tool}} snap` opens a camera, waits for it to warm up, saves one picture to
a file, and closes the camera again. The commands that [[#list-then-snap]]
prints find a camera with `cameras` and save one picture of it. On success
the tool prints one line, such as
`Saved frame.png (1280x720) from Integrated Webcam`, and exits 0.

| Option | Meaning |
| --- | --- |
| `--camera <camera>` | Required. The camera: its index as `cameras` lists it, its `Id`, or its name. |
| `--output <file>` | Required. The file to write. It is overwritten, and its folder must exist. |
| `--crop <x,y,w,h>` | Keep only this region: left edge, top edge, width, and height, in pixels. |
| `--width <px>` | Prefer a picture of this width. |
| `--height <px>` | Prefer a picture of this height. |
| `--fps <n>` | Prefer this frame rate, in frames per second. |
| `--warmup <ms>` | Discard the frames of the first milliseconds, 0 to 60000. The default is 1000. |

**Camera.** A number is the position in the list that `cameras` prints. The
number can change when cameras are attached or removed, so a script should use
the `Id`, which is stable, or the name. A name that several cameras share is an
error; use the `Id` then.

**Output file.** The extension chooses the format: `.png`, `.jpg` or `.jpeg`,
and `.bmp`. Any other extension is an error that the command that
[[#bad-extension]] prints shows. The extension and the crop are checked before
the camera is touched, so a wrong command fails at once.

**Crop.** `--crop` takes four whole numbers separated by commas. The region
must lie inside the picture; the command that [[#crop]] prints keeps a 400 by
300 pixel region. If the camera delivers another size than you asked for, the
region is checked against the size that the camera really uses.

**Size and frame rate.** `--width`, `--height`, and `--fps` are preferences,
not demands: the tool opens the format of the camera that comes closest, as the
command that [[#format]] prints. The line that the tool prints shows the size
of the saved picture.

**Warm-up.** Most cameras deliver dark or badly exposed frames for the first
moments, while exposure and white balance settle. The tool discards the frames
of the warm-up, 1 second by default. Raise it for a camera that needs longer,
as [[#warmup]] shows, or use 0 to take the first frame.

**Errors.** A problem prints `Error: ...` on the error channel and exits 1, and
the camera is closed. This covers a missing or unknown camera
([[#missing-camera]] and
[[#unknown-camera]]), a camera that is in use or cannot
be opened, a camera that delivers no frame within 5 seconds after the warm-up,
a missing folder, and a crop outside the picture. The `VideoForWindows` entry
of a camera (usually named `Default`) is a legacy backend that often cannot be
opened; use the `DirectShow` or `MediaFoundation` entry of the same camera. If
every picture is a placeholder image, check the camera's privacy
shutter/switch and the OS camera privacy settings. `--silent` hides the message
and keeps the exit code, and `--log` also writes it to a file. See
[[command-line]] for the other options, and [[cameras]] for what to check when
no camera is listed.
