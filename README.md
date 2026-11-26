# AgentVision

[![GitHub forks][badge-forks]][link-forks]
[![GitHub stars][badge-stars]][link-stars]
[![GitHub contributors][badge-contributors]][link-contributors]
[![License][badge-license]][link-license]
[![Build][badge-build]][link-build]
[![Quality Gate][badge-quality]][link-quality]
[![Security][badge-security]][link-security]
[![NuGet][badge-nuget]][link-nuget]
[![NuGet][badge-nuget-core]][link-nuget-core]

AgentVision gives AI agents fast access to webcams. It is delivered as the `DemaConsulting.AgentVision.Core` .NET
library and the `DemaConsulting.AgentVision.Tool` .NET tool (command `agentvision`), which is a thin command-line
front-end for the library. The goal of the project is low-latency webcam capture: keeping multiple cameras open
("hot") so that frames can be captured, cropped, and saved with almost no delay, using
[FlashCap][link-flashcap] and [DemaConsulting.CanvasNet][link-canvasnet], with the command-line tool optionally
running through a background daemon.

**Status**: under active development. The first camera feature is available: both packages can list the cameras of
a machine and the video formats each one supports (`agentvision cameras`, or `CameraEnumerator` in the library).
Opening a camera and capturing frames are planned and are not available yet.

The diagram below is a placeholder workflow diagram: replace its TODO boxes (in `docs/images/workflow.svg`,
then re-render `docs/images/workflow.png`) to show how users prepare inputs, run the tool, get its outputs,
and use them.

![Placeholder workflow diagram: replace the TODO boxes to show how users prepare inputs, run the tool, get
its outputs, and use them][workflow]

## Features

AgentVision provides:

- **Camera Enumeration**: `agentvision cameras` lists the video capture devices of the machine with their stable
  identifiers, names, and supported formats (resolution, pixel format, and frame rate), using [FlashCap][link-flashcap];
  a machine without a camera is reported plainly and is not an error
- **Standardized Command-Line Interface**: Context class handling common arguments
  (`--version`, `--help`, `--silent`, `--validate`, `--results`, `--depth`, `--log`)
- **Self-Validation**: Built-in validation tests with TRX/JUnit output
- **Built-in Help**: `agentvision help` serves Markdown help cards ([DemaConsulting.HelpCards][link-helpcards])
  with runnable examples, search, and JSON output for AI agents
- **Reusable Library**: `DemaConsulting.AgentVision.Core` is the home of all camera logic and is usable by any
  .NET application without the tool
- **Multi-Platform Support**: Builds and runs on Windows and Linux
- **Multi-Runtime Support**: Targets .NET 10 and 11
- **Comprehensive CI/CD**: GitHub Actions workflows with quality checks, builds, and
  integration tests
- **Linting Enforcement**: LintMark, TextLint, and yamllint enforced on every CI run
- **Continuous Compliance**: Compliance evidence generated automatically on every CI run, following
  the [Continuous Compliance][link-continuous-compliance] methodology
- **SonarCloud Integration**: Quality gate and security analysis on every build
- **Documentation Generation**: Automated build notes, user guide, code quality reports,
  requirements, justifications, and trace matrix
- **Requirements Traceability**: Requirements linked to passing tests with auto-generated trace matrix

## Installation

Install the tool globally using the .NET CLI:

```bash
dotnet tool install -g DemaConsulting.AgentVision.Tool
```

Add the library to a project with:

```bash
dotnet add package DemaConsulting.AgentVision.Core
```

## Usage

```bash
# Default behavior (no arguments) — displays banner, copyright, and a pointer to the help
agentvision

# List the cameras of this machine and the formats they support
agentvision cameras

# Display version
agentvision --version

# Display help
agentvision --help

# List the built-in help topics, then read one
agentvision help
agentvision help getting-started
agentvision help cameras

# Run self-validation
agentvision --validate

# Save validation results (--result is an accepted alias for --results)
agentvision --validate --results results.trx
agentvision --validate --result results.xml

# Set heading depth for embedded validation output
agentvision --validate --depth 2

# Silent mode with logging
agentvision --silent --log output.log cameras
```

## Listing Cameras

`agentvision cameras` lists the video capture devices that the machine reports. It only looks at the devices and does
not start capturing from any of them. It exits with code 0 also when it finds no camera, which is a normal answer on
servers and build agents.

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

When no camera is found, the output is `No cameras found.` Windows can report one physical camera through several
backends (DirectShow, Media Foundation, and Video for Windows), so it may appear more than once, each time with its
own `Id`. On Linux, cameras are reported through V4L2 and your account needs access to the `/dev/video*` devices,
normally through the `video` group. Run `agentvision help cameras` for troubleshooting.

The same list is available to any .NET application through the library:

```csharp
using DemaConsulting.AgentVision.Core.Cameras;

var cameras = new CameraEnumerator().Enumerate();
foreach (var camera in cameras)
{
    Console.WriteLine($"{camera.Id}: {camera.Name} ({camera.Formats.Count} formats)");
}
```

An application or a test can supply its own `ICameraSource` to `new CameraEnumerator(source)` instead, so that
enumeration runs without camera hardware.

## Command-Line Options

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
| `--results <file>`, `--result <file>` | Write results to `.trx` (TRX) or `.xml` (JUnit XML) file. |
| `--depth <#>` | Set heading depth for markdown output (default: 1) |
| `--log <file>` | Write output to log file |

## Error Handling

Unrecognized arguments, and a command that is given twice, cause the tool to print an error message to standard
error and exit with a non-zero exit code. For example:

```text
Error: Unsupported argument '--unknown'
```

This behavior enables CI/CD pipelines to detect and surface misconfiguration failures
automatically.

## Self Validation

Running self-validation produces a report containing the following information:

```text
# DEMA Consulting AgentVision Tool

| Information         | Value                                              |
| :------------------ | :------------------------------------------------- |
| Tool Version        | <version>                                          |
| Machine Name        | <machine-name>                                     |
| OS Version          | <os-version>                                       |
| DotNet Runtime      | <dotnet-runtime-version>                           |
| Time Stamp          | <timestamp> UTC                                    |

✓ AgentVisionTool_VersionDisplay - Passed
✓ AgentVisionTool_HelpDisplay - Passed
✓ AgentVisionTool_CamerasList - Passed

Total Tests: 3
Passed: 3
Failed: 0
```

Each test in the report proves:

- **`AgentVisionTool_VersionDisplay`** - `--version` outputs a valid version string.
- **`AgentVisionTool_HelpDisplay`** - `--help` outputs usage and options information.
- **`AgentVisionTool_CamerasList`** - `cameras` exits with code 0 and reports the cameras or that none were
  found. It passes on a machine without a camera.

Use `--depth <#>` to control the heading level of the validation output (default: `1`).
This is useful when embedding validation output into a larger markdown document:

```bash
# Embed validation at heading level 2
agentvision --validate --depth 2
```

See the [User Guide][link-guide] for more details on the self-validation tests.

On validation failure the tool will exit with a non-zero exit code.

## Documentation

Generated documentation includes:

- **Build Notes**: Release information and changes
- **User Guide**: Comprehensive usage documentation
- **Code Quality Report**: CodeQL and SonarCloud analysis results
- **Requirements**: Functional and non-functional requirements
- **Requirements Justifications**: Detailed requirement rationale
- **Trace Matrix**: Requirements to test traceability

## Contributing

See [CONTRIBUTING.md](https://github.com/demaconsulting/AgentVision/blob/main/CONTRIBUTING.md) for
guidelines on reporting bugs, suggesting features, and submitting pull requests.

## License

Copyright (c) DEMA Consulting. Licensed under the MIT License. See [LICENSE][link-license] for details.

By contributing to this project, you agree that your contributions will be licensed under the MIT License.

<!-- Badge References -->
[workflow]: https://raw.githubusercontent.com/demaconsulting/AgentVision/main/docs/images/workflow.png
[badge-forks]: https://img.shields.io/github/forks/demaconsulting/AgentVision?style=plastic
[badge-stars]: https://img.shields.io/github/stars/demaconsulting/AgentVision?style=plastic
[badge-contributors]: https://img.shields.io/github/contributors/demaconsulting/AgentVision?style=plastic
[badge-license]: https://img.shields.io/github/license/demaconsulting/AgentVision?style=plastic
[badge-build]: https://img.shields.io/github/actions/workflow/status/demaconsulting/AgentVision/build_on_push.yaml?style=plastic
[badge-quality]: https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_AgentVision&metric=alert_status
[badge-security]: https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_AgentVision&metric=security_rating
[badge-nuget]: https://img.shields.io/nuget/v/DemaConsulting.AgentVision.Tool?style=plastic
[badge-nuget-core]: https://img.shields.io/nuget/v/DemaConsulting.AgentVision.Core?style=plastic

<!-- Link References -->
[link-forks]: https://github.com/demaconsulting/AgentVision/network/members
[link-stars]: https://github.com/demaconsulting/AgentVision/stargazers
[link-contributors]: https://github.com/demaconsulting/AgentVision/graphs/contributors
[link-license]: https://github.com/demaconsulting/AgentVision/blob/main/LICENSE
[link-build]: https://github.com/demaconsulting/AgentVision/actions/workflows/build_on_push.yaml
[link-quality]: https://sonarcloud.io/dashboard?id=demaconsulting_AgentVision
[link-security]: https://sonarcloud.io/dashboard?id=demaconsulting_AgentVision
[link-nuget]: https://www.nuget.org/packages/DemaConsulting.AgentVision.Tool
[link-nuget-core]: https://www.nuget.org/packages/DemaConsulting.AgentVision.Core
[link-flashcap]: https://github.com/kekyo/FlashCap
[link-canvasnet]: https://github.com/demaconsulting/CanvasNet
[link-guide]: https://github.com/demaconsulting/AgentVision/blob/main/docs/user_guide/introduction.md
[link-continuous-compliance]: https://github.com/demaconsulting/ContinuousCompliance
[link-helpcards]: https://www.nuget.org/packages/DemaConsulting.HelpCards
