---
name: agentvision
schema: 2
title: AgentVision Tool
summary: Built-in help of the AgentVision tool for low-latency webcam capture.
---
{{tool}} is the command-line tool of AgentVision, which gives AI agents fast access to webcams. It offers the
`cameras` command, which lists the cameras of the machine and the formats they support, the `snap` command, which
saves one picture of a camera to a file, the standard options, self-validation, and this built-in help. Further
webcam commands are planned and are not available yet.

The commands are `cameras`, `snap`, and `help`. `{{tool}} cameras` lists the cameras; see `{{tool}} help cameras`.
`{{tool}} snap --camera 0 --output frame.png` saves a picture; see `{{tool}} help snap`. The `help` command must be
the first argument and takes topic names, such as `{{tool}} help command-line`.
