# AgentVisionCore

![AgentVisionCore Structure](AgentVisionCoreView.svg)

## Architecture

AgentVisionCore is the `DemaConsulting.AgentVision.Core` .NET class library. It is the reusable core of the
AgentVision product, which gives AI agents low-latency access to webcams: the `agentvision` command-line tool (see
the *AgentVisionTool* design) is a thin front-end for it, and any other .NET application may reference it directly.

The library consists of three subsystems:

- **Cameras** enumerates the video capture devices of the host and starts a camera streaming (see *Cameras Design*).
  **CameraEnumerator** owns the policy of the camera list and the public camera types, **FlashCapCameraSource** is
  the default source that finds and opens the devices, and **FlashCapCameraDevice** is one streaming FlashCap device.
  The last two are the only units that talk to the FlashCap OTS library.
- **Capture** keeps cameras open ("hot") so that their newest frame can be read with almost no delay (see *Capture
  Design*). **CameraSelector** decides which camera and format a request means, **CameraOpener** opens a camera,
  **CameraSession** is the open camera that holds the newest frame and serves snapshots from any thread, and
  **CameraSnapper** takes one picture with one call.
- **Imaging** turns a frame into a picture that can be cropped and saved as PNG, JPEG, or BMP (see *Imaging Design*).
  **FrameDecoder** decodes the frame and repairs the defects of camera frames, and **CameraImage** is the picture.
  These are the only units that talk to the CanvasNet OTS library.

The capabilities that follow (a background daemon that keeps cameras open for other processes) are added in later
changes, each as a subsystem or unit of this system with its own requirements, design, and verification documents.

**Layering.** The library owns **all camera logic**. It is self-contained: it never depends on the tool, so any .NET
application can use webcams without it. The command-line tool is only a thin front-end and is not permitted to carry
camera logic of its own.

## External Interfaces

**NuGet Package**: The distribution interface for consumers.

- *Type*: NuGet package.
- *Role*: Provider.
- *Contract*: The package is named `DemaConsulting.AgentVision.Core`, carries the README, icon, MIT license
  expression, and source-link metadata, and ships `net10.0` and `net11.0` assemblies with XML documentation and a
  symbols package.
- *Constraints*: The package contains no native assets. FlashCap and CanvasNet are its runtime package
  dependencies, and they flow to consumers as ordinary package dependencies.

**Public API**: The programmatic interface for consuming applications.

- *Type*: .NET assembly.
- *Role*: Provider.
- *Contract*: The assembly is named `DemaConsulting.AgentVision.Core`. Its public types are in three namespaces.
  `DemaConsulting.AgentVision.Core.Cameras`: `CameraEnumerator` lists the cameras, `CameraInfo` and `CameraFormat` are
  the plain values it returns, `ICameraSource` is the seam through which an application supplies its own cameras, and
  `EncodedFrame` is one frame as the bytes of an image file. `DemaConsulting.AgentVision.Core.Capture`: `CameraOpener`
  opens a camera, `CameraSession` is the open camera (`Snapshot` returns a `CameraFrame`, and `Dispose` stops the
  camera), `CameraOpenOptions` carries the requested format, the warm-up, and the timeouts, `CameraSnapper` takes one
  picture (`SnapRequest` and `SnapResult`), and `CameraException` with its four derived types reports failures.
  `DemaConsulting.AgentVision.Core.Imaging`: `CameraImage` (decode, crop, and save), `CropRectangle`, and
  `ImageFileFormat`.
- *Constraints*: The assembly never references the tool assembly, and no FlashCap or CanvasNet type appears in any
  public signature.

## Dependencies

- **Runtime**: FlashCap, the runtime OTS item that discovers and drives the video capture devices (see *FlashCap
  Integration Design*); only the *FlashCapCameraSource* and *FlashCapCameraDevice* units reference it. CanvasNet, the
  runtime OTS item that decodes, crops, and saves pictures (see *CanvasNet Integration Design*); only the
  *FrameDecoder* and *CameraImage* units reference it.
- **Build time**: SourceLink and the SBOM targets package (both private to the build), and the .NET and Sonar
  analyzers (private to the build). None of these flows to consumers of the package.
- **Test time**: xUnit for the test project (see *OTS Software Integration Design*).

## Risk Control Measures

N/A - not a safety-classified software item.

## Data Flow

*Listing.* A caller creates a `CameraEnumerator`, optionally with its own `ICameraSource`, and calls `Enumerate`. The
enumerator asks the source for the cameras that are present now. The default source asks FlashCap for its device
descriptors and translates each into a `CameraInfo` with its `CameraFormat` values. The enumerator copies, orders, and
de-duplicates what the source returned and hands the list back.

*Capturing.* A caller creates a `CameraOpener` and opens a camera by index, identifier, or name. The opener selects the
camera and a format, and asks the source to start the camera; FlashCap delivers every frame to the `CameraSession` on
its own thread as the bytes of a complete image file. The session drops the frames of the warm-up and keeps the newest
one. Any thread calls `Snapshot` and receives that frame, which it may forward as it is or decode with `ToImage` into a
`CameraImage`, crop, and save. Disposing the session stops the camera. `CameraSnapper` performs the whole sequence once.

No data is cached between listing calls; frames are kept in memory only (one per open camera), and the only thing that
is written anywhere is the picture file that a caller asks to be saved. See *Cameras Design*, *Capture Design*, and
*Imaging Design* for the details of each step.

## Design Constraints

- Platform: multi-targets net10.0 and net11.0 on Windows and Linux. macOS is not supported.
- Layering: the library never depends on the tool, and camera logic never moves into the tool.
- Public surface: the library owns its public API; types from third-party packages, FlashCap included, are not
  exposed by public signatures, so a package upgrade can never break consumers.
- Trimming: FlashCap is not trim-safe or AOT-safe, so the library is not marked as trim-safe or AOT compatible.
- Latency: a snapshot of an open camera is a read of one value and never waits except for the first one; decoding and
  saving cost time that depends on the frame size and are done on the thread of the caller, never on the device thread.
- Packaging: CanvasNet is a pre-release package, so the NuGet warning that a stable package must not depend on a
  pre-release one is suppressed in the library project, with the reason documented there, until CanvasNet is stable.
- Documentation: every public member carries XML documentation, which the build treats as an error when missing.
