## FlashCap

This document describes the integration and usage design for the `FlashCap` OTS software item (the `FlashCap` NuGet
package, an open-source library for capturing video from cameras on .NET).

### Purpose

FlashCap discovers the video capture devices of the host and describes them: their identity, name, backend, and the
pixel formats, frame sizes, and frame rates they support. It was chosen because it is a maintained, cross-platform
.NET library that reaches the native camera interfaces of both supported operating systems (DirectShow, Media
Foundation, and Video for Windows on Windows; V4L2 on Linux) without any native library of its own, so the
AgentVision packages stay free of native assets. It is a runtime dependency of the AgentVisionCore library and flows
to consumers as a package dependency.

### Features Used

- **`CaptureDevices`** and its **`EnumerateDescriptors`** extension — enumerate the capture devices of the host.
- **`CaptureDeviceDescriptor`** — `Identity`, `DeviceType`, `Name`, `Description`, and `Characteristics` of one device.
- **`VideoCharacteristics`** — `PixelFormat`, `Width`, `Height`, and `FramesPerSecond` of one video format.
- **`PixelFormats`** — in particular the `Unknown` value, which marks a format FlashCap cannot capture.
- **The frame-rate fraction type** — an exact numerator over denominator, converted to a number.

Not used: opening a device, streaming frames, device controls, and the observable and transcoding features. They are
added together with the capabilities that need them.

### Integration Pattern

Only the `FlashCapCameraSource` unit references FlashCap, and FlashCap types never cross the `ICameraSource`
boundary, so the public API of the library and the rest of the library are independent of it. Creating a
`CaptureDevices` object and enumerating performs no initialization or disposal that this unit must manage. The
package is referenced by the library project, not by the tool, which receives it through the library. FlashCap is not
trim-safe or AOT-safe, so the library is not marked as trim-safe or AOT compatible. FlashCap reports one entry for each
backend that sees a device, so one physical camera can appear more than once; see *Cameras Design*. Upgrading the
package requires re-running the library tests, which are the evidence for the FlashCap requirements, and a review of
this design.
