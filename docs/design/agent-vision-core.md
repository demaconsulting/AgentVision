# AgentVisionCore

![AgentVisionCore Structure](AgentVisionCoreView.svg)

## Architecture

AgentVisionCore is the `DemaConsulting.AgentVision.Core` .NET class library. It is the reusable core of the
AgentVision product, which gives AI agents low-latency access to webcams: the `agentvision` command-line tool (see
the *AgentVisionTool* design) is a thin front-end for it, and any other .NET application may reference it directly.

The library currently consists of the package and assembly identity alone: it contains no subsystems or units, and it
consumes no OTS items at run time. The webcam capabilities that make up the product (keeping several cameras open
so that frames can be captured, cropped, and saved with almost no delay) are added in later changes, each as a
subsystem or unit of this system with its own requirements, design, and verification documents.

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
- *Constraints*: The package contains no native assets and has no runtime package dependencies yet.

**Public API**: The programmatic interface for consuming applications.

- *Type*: .NET assembly.
- *Role*: Provider.
- *Contract*: The assembly is named `DemaConsulting.AgentVision.Core`. It exposes no public types yet; the public API
  is added together with the first camera capability.
- *Constraints*: The assembly never references the tool assembly.

## Dependencies

- **Runtime**: none. The library has no runtime package dependencies yet.
- **Build time**: SourceLink and the SBOM targets package (both private to the build), and the .NET and Sonar
  analyzers (private to the build). None of these flows to consumers of the package.
- **Test time**: xUnit for the test project (see *OTS Software Integration Design*).

## Risk Control Measures

N/A - not a safety-classified software item.

## Data Flow

N/A - the library has no operations yet, so there is no data to flow. The data flow of each capability is documented
in the design of the subsystem or unit that provides it.

## Design Constraints

- Platform: multi-targets net10.0 and net11.0 on Windows and Linux. macOS is not supported.
- Layering: the library never depends on the tool, and camera logic never moves into the tool.
- Public surface: the library owns its public API; types from third-party packages are not exposed by public
  signatures, so a package upgrade can never break consumers.
- Documentation: every public member carries XML documentation, which the build treats as an error when missing.
