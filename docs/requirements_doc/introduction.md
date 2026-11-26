# Introduction

This document contains the requirements for the AgentVision project.

## Purpose

AgentVision gives AI agents low-latency access to webcams. It is delivered as the
`DemaConsulting.AgentVision.Core` .NET library and the `DemaConsulting.AgentVision.Tool` .NET
command-line tool (command `agentvision`), which is a thin front-end for the library. This document
specifies what the library and the tool shall do, together with the requirements on the off-the-shelf
software and the repository quality outcomes that support them.

## Scope

This requirements document covers:

- Library package identity, metadata, and independence from the tool
- Command-line interface and options of the tool
- Built-in help
- Self-validation framework
- Test result output formats
- Logging capabilities
- Windows and Linux platform support
- Documentation generation
- CI/CD integration

## Audience

This document is intended for:

- Software developers working on AgentVision
- Quality assurance teams validating requirements
- Project stakeholders reviewing project scope
- Users understanding the tool's capabilities
