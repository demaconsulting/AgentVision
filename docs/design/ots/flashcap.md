## FlashCap

This document describes the integration and usage design for the `FlashCap` OTS software item (the `FlashCap` NuGet
package, an open-source library for capturing video from cameras on .NET).

### Purpose

FlashCap discovers the video capture devices of the host and describes them (their identity, name, backend, and the
pixel formats, frame sizes, and frame rates they support), and opens a device, streams its frames, and stops it. It was
chosen because it is a maintained, cross-platform .NET library that reaches the native camera interfaces of both
supported operating systems (DirectShow, Media Foundation, and Video for Windows on Windows; V4L2 on Linux) without any
native library of its own, so the AgentVision packages stay free of native assets. It is a runtime dependency of the
AgentVisionCore library and flows to consumers as a package dependency.

### Features Used

- **`CaptureDevices`** and its **`EnumerateDescriptors`** extension — enumerate the capture devices of the host.
- **`CaptureDeviceDescriptor`** — `Identity`, `DeviceType`, `Name`, `Description`, and `Characteristics` of one device.
- **`VideoCharacteristics`** — `PixelFormat`, `Width`, `Height`, and `FramesPerSecond` of one video format.
- **`PixelFormats`** — in particular the `Unknown` value, which marks a format FlashCap cannot capture.
- **The frame-rate fraction type** — an exact numerator over denominator, converted to a number.
- **The open extension of a descriptor** — opens a device with chosen video characteristics, automatic transcoding, no
  scattering, a queue of one frame, and a callback that receives a pixel buffer scope.
- **`CaptureDevice`** — its start and stop extensions and its disposal, which release the camera.
- **`PixelBuffer`** — the image copy, which returns the frame as the bytes of a complete file: JPEG and PNG frames as
  they are, raw 24-bit and 32-bit frames as a bitmap file with a file header, and YUV frames (YUYV, UYVY, NV12)
  converted to a 24-bit bitmap file; and the frame number and timestamp.

Not used: device controls (exposure, focus, and the like), the observable interface, scattering, one-shot capture, and
the other transcoding choices. They are added together with the capabilities that need them.

### Integration Pattern

Only the `FlashCapCameraSource` and `FlashCapCameraDevice` units reference FlashCap, and FlashCap types never cross the
`ICameraSource` boundary, so the public API of the library and the rest of the library are independent of it. Creating a
`CaptureDevices` object and enumerating performs no initialization or disposal that these units must manage. An opened
device is different: it holds the camera until it is stopped and disposed, so `FlashCapCameraDevice` owns it, stops it
and disposes it in a bounded time, and shuts the frame callback off before it does. FlashCap is asked for a queue of one
frame without scattering, so frames that arrive while the callback is busy are dropped and the consumer sees the newest
frame. FlashCap reports no event when a camera is unplugged on every backend; the Capture subsystem detects it as a
silence of frames. FlashCap copies each frame into a buffer that it reuses, so the callback copies the image out before
it returns. The package is referenced by the library project, not by the tool, which receives it through the library.
FlashCap is not trim-safe or AOT-safe, so the library is not marked as trim-safe or AOT compatible. FlashCap reports one
entry for each backend that sees a device, so one physical camera can appear more than once; see *Cameras Design*.
Upgrading the package requires re-running the library tests, which are the evidence for the FlashCap requirements, and a
review of this design.
