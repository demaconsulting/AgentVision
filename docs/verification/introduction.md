# Introduction

This document provides the verification design for AgentVision, which gives AI agents low-latency access to
webcams and is delivered as the `DemaConsulting.AgentVision.Core` .NET library and the
`DemaConsulting.AgentVision.Tool` .NET command-line tool.

## Purpose

The purpose of this document is to describe how each requirement for AgentVision is
verified. For every software item — system, subsystem, and unit — this document names the
verification approach, identifies the test scenarios (including boundary conditions and error
paths), describes what is mocked or stubbed, and maps each requirement to at least one named
test scenario. The document does not restate design; it explains how the design is proven correct.

## Scope

This document covers the verification design for the following software items:

**Local items:**

- **AgentVisionCore** — system verification of the `DemaConsulting.AgentVision.Core` library, with the
  **Cameras**, **Capture**, and **Imaging** subsystems
  - **Cameras** subsystem
    - **CameraEnumerator** — camera list policy and camera types
    - **FlashCapCameraSource** — FlashCap adapter
    - **FlashCapCameraDevice** — one streaming FlashCap device
  - **Capture** subsystem
    - **CameraSelector** — which camera and format a request means
    - **CameraOpener** — opens cameras
    - **CameraSession** — the open camera that holds the newest frame
    - **CameraSnapper** — one-shot snapshot
  - **Imaging** subsystem
    - **FrameDecoder** — decodes camera frames and repairs their defects
    - **CameraImage** — the picture: crop and save
- **AgentVisionTool** — system, subsystem, and unit verification of the command-line tool:
  - **Program** — entry point and execution orchestrator
  - **CamerasCommand** — the `cameras` command
  - **SnapCommand** — the `snap` command
  - **Cli** subsystem
    - **Context** — argument parser and I/O owner
    - **HelpCommand** — built-in help command
  - **SelfTest** subsystem
    - **Validation** — self-validation test runner
  - **Utilities** subsystem
    - **PathHelpers** — safe path combination utilities

**OTS items:**

- **BuildMark** — build-notes documentation tool
- **CanvasNet** — image decoding, cropping, and saving library
- **FileAssert** — document assertion tool
- **FlashCap** — video capture library: device enumeration and streaming
- **HelpCards** — built-in help library
- **Pandoc** — Markdown-to-HTML conversion tool
- **ReqStream** — requirements traceability tool
- **ReviewMark** — file review enforcement tool
- **SarifMark** — SARIF report conversion tool
- **SonarMark** — SonarCloud quality report tool
- **SysML2Tools** — architecture model validation and diagram rendering tool
- **VersionMark** — tool-version documentation tool
- **WeasyPrint** — HTML-to-PDF conversion tool
- **xUnit** — unit-testing framework

The following topics are out of scope:

- Verification documents are not produced for the test projects themselves — they are the
  means of verification, not subjects of it
- Build pipeline CI configuration is excluded
- The internal implementation of OTS software items is excluded; only integration and usage
  are verified

## Folder Layout

The test folder structure mirrors the source subsystem breakdown:

```text
test/
├── DemaConsulting.AgentVision.Core.Tests/  — library tests
└── DemaConsulting.AgentVision.Tool.Tests/  — tool unit and integration tests
```

## Companion Artifact Structure

In-house items have corresponding artifacts in parallel directory trees:

- Requirements: `docs/reqstream/{system}/.../{item}.yaml` (kebab-case)
- Design docs: `docs/design/{system}/.../{item}.md` (kebab-case)
- Verification design: `docs/verification/{system}/.../{item}.md` (kebab-case)
- Source code: `src/{System}/.../{Item}.cs` (PascalCase for C#)
- Tests: `test/{System}.Tests/.../{Item}Tests.cs` (PascalCase for C#)

OTS items have parallel artifacts in:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml` (kebab-case)
- Verification: `docs/verification/ots/{ots-name}.md` (kebab-case)

Review-sets: defined in `.reviewmark.yaml`

## References

- AgentVision Software Design Document
- AgentVision releases (<https://github.com/demaconsulting/AgentVision/releases>)
