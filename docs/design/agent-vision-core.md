# AgentVisionCore

![AgentVisionCore Structure](AgentVisionCoreView.svg)

## Architecture

AgentVisionCore is the `DemaConsulting.AgentVision.Core` .NET class library. It is the reusable core of the
AgentVision product, which gives AI agents low-latency access to webcams: the `agentvision` command-line tool (see
the *AgentVisionTool* design) is a thin front-end for it, and any other .NET application may reference it directly.

The library currently consists of one subsystem, **Cameras**, which enumerates the video capture devices of the host
(see *Cameras Design*). It is made of two units: **CameraEnumerator** owns the policy of the camera list and the
public camera types, and **FlashCapCameraSource** is the only unit that talks to the FlashCap OTS library. The
remaining capabilities of the product (keeping several cameras open so that frames can be captured, cropped, and
saved with almost no delay) are added in later changes, each as a subsystem or unit of this system with its own
requirements, design, and verification documents.

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
- *Constraints*: The package contains no native assets. FlashCap is its only runtime package dependency, and it
  flows to consumers as an ordinary package dependency.

**Public API**: The programmatic interface for consuming applications.

- *Type*: .NET assembly.
- *Role*: Provider.
- *Contract*: The assembly is named `DemaConsulting.AgentVision.Core`. Its public types are in the namespace
  `DemaConsulting.AgentVision.Core.Cameras`: `CameraEnumerator` lists the cameras, `CameraInfo` and `CameraFormat` are
  the plain values it returns, and `ICameraSource` is the seam through which an application supplies its own camera
  descriptions.
- *Constraints*: The assembly never references the tool assembly, and no FlashCap type appears in any public
  signature.

## Dependencies

- **Runtime**: FlashCap, the runtime OTS item that discovers the video capture devices (see *FlashCap Integration
  Design*). Only the *FlashCapCameraSource* unit references it.
- **Build time**: SourceLink and the SBOM targets package (both private to the build), and the .NET and Sonar
  analyzers (private to the build). None of these flows to consumers of the package.
- **Test time**: xUnit for the test project (see *OTS Software Integration Design*).

## Risk Control Measures

N/A - not a safety-classified software item.

## Data Flow

A caller creates a `CameraEnumerator`, optionally with its own `ICameraSource`, and calls `Enumerate`. The enumerator
asks the source for the cameras that are present now. The default source asks FlashCap for its device descriptors and
translates each into a `CameraInfo` with its `CameraFormat` values. The enumerator copies, orders, and de-duplicates
what the source returned and hands the list back. No data is cached between calls, and nothing is written or sent
anywhere. See *Cameras Design* for the details of each step.

## Design Constraints

- Platform: multi-targets net10.0 and net11.0 on Windows and Linux. macOS is not supported.
- Layering: the library never depends on the tool, and camera logic never moves into the tool.
- Public surface: the library owns its public API; types from third-party packages, FlashCap included, are not
  exposed by public signatures, so a package upgrade can never break consumers.
- Trimming: FlashCap is not trim-safe or AOT-safe, so the library is not marked as trim-safe or AOT compatible.
- Documentation: every public member carries XML documentation, which the build treats as an error when missing.
