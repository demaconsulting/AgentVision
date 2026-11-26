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

AgentVision is under active development. The library is only a package skeleton so far, and the tool offers
the standard options, built-in help, and self-validation but no webcam commands yet. The capture features
described above are planned, and this guide documents only what exists in the current release.

## Scope

This user guide covers:

- Installation instructions for the tool and the library
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

When invoked with no arguments, the tool displays the banner and copyright notice, followed by a short
description of AgentVision and a pointer to the usage and help topics:

```bash
agentvision
```

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

Total Tests: 2
Passed: 2
Failed: 0
```

### Validation Tests

Each test proves specific functionality works correctly:

- **`AgentVisionTool_VersionDisplay`** - `--version` outputs a valid version string.
- **`AgentVisionTool_HelpDisplay`** - `--help` outputs usage and options information.

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

Unrecognized arguments cause the tool to print an error message to standard error and exit
with a non-zero exit code. For example:

```text
Error: Unsupported argument '--unknown'
```

This behavior enables automated scripts and CI/CD pipelines to detect and surface
misconfiguration failures automatically. Argument errors are found before `--silent` and `--log`
take effect, so they are always printed and never logged.

# Command-Line Options

The following command-line options are supported:

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

## Example 2: Self-Validation with Results

```bash
agentvision --validate --results validation-results.trx
```

## Example 3: Silent Mode with Logging

```bash
agentvision --silent --log tool-output.log
```

## References

N/A
