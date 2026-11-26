---
title: Listing cameras
summary: List the cameras of this machine and the video formats each one supports.
aliases:
  - webcam
  - webcams
  - devices
tags:
  - tutorial
order: 3
see-also:
  - getting-started
  - command-line
examples:
  - name: list
    kind: command
    language: shell
    description: List the cameras and their formats. Exits 0, also when there is no camera.
    content: '{{tool}} cameras'
  - name: log
    kind: command
    language: shell
    description: Hide the console output and keep the list in a log file.
    content: '{{tool}} --silent --log cameras.log cameras'
  - name: repeated
    kind: command
    language: shell
    description: The command can be given only once. Exits 1.
    content: '{{tool}} cameras cameras'
    exit: 1
    expect: "The 'cameras' command can be given only once"
---
`{{tool}} cameras` lists the video capture devices that this
machine reports. The command that [[#list]] prints is all
there is to it. The command only looks at the devices. It does not
start capturing from any of them, so it does not switch a
camera on or disturb a program that is using it.

**Output.** The first line says `Cameras found: <n>`, or
`No cameras found.` when the machine has none. Finding no camera
is a normal answer, not an error, so the exit code is 0 either
way. Each camera is numbered from 0 in the order of the list and
shows:

- `Id`, the stable identifier: the backend, a colon, and the
  identity the backend gives the device. Use it, not the number,
  to refer to a camera, because the number can change when
  cameras are attached or removed.
- `Backend`, the part of the operating system that reported the
  camera.
- `Description`, as the backend reports it.
- `Formats`, one line for each way to capture video: the frame
  size, the pixel format, and the frame rate in frames per
  second. The largest and fastest formats come first. Formats
  that cannot be captured are not listed.

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

**Backends.** Windows can report one physical camera through
several backends: DirectShow, Media Foundation, and Video for
Windows. The same camera may then appear more than once, each
time with its own `Backend` and `Id`. Linux reports cameras
through V4L2, which lists each video device node.

**No camera found.** Check these in order:

1. A camera is attached and enabled, and the operating system
   shows it in its own camera or device settings.
2. No other program holds the camera. Close video call and
   recording programs, and list again.
3. On Linux, the device nodes `/dev/video*` exist, and your
   account may read and write them. Add your account to the
   `video` group and sign in again.
4. A virtual machine or container needs the camera passed
   through to it before the tool can see it.

The listing follows `--silent` and `--log`; the command that
[[#log]] prints keeps it in a file. Giving the command twice is
an error; the command that [[#repeated]] prints shows the
message. See [[command-line]] for the other errors.
