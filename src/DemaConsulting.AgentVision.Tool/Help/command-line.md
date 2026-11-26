---
title: Command-line options
summary: Every option of the tool, and the order in which a run handles them.
aliases:
  - options
  - flags
tags:
  - reference
order: 2
see-also:
  - cameras
  - snap
  - self-validation
examples:
  - name: usage
    kind: command
    language: shell
    description: Print the short usage text. It ends with a pointer to this help.
    content: '{{tool}} --help'
    expect: "Run '{{tool}} help' for topics and examples."
  - name: cameras
    kind: command
    language: shell
    description: List the cameras. Exits 0, also when there is no camera.
    content: '{{tool}} cameras'
  - name: silent-log
    kind: command
    language: shell
    description: Hide the console output and keep it in a log file.
    content: '{{tool}} --silent --log output.log --version'
  - name: unknown-option
    kind: command
    language: shell
    description: An unknown argument is an error, printed even with --silent. Exits 1.
    content: '{{tool}} --silent --unknown'
    exit: 1
    expect: "Error: Unsupported argument '--unknown'"
---
Options start with `--` (or a single letter after `-`) and may be given in
any order. The commands `cameras` and `snap` may stand among them; give only
one of them.

| Command | Meaning |
| --- | --- |
| `cameras` | List the cameras and their formats; see [[cameras]]. |
| `snap` | Save one picture of a camera to a file; see [[snap]]. |

| Option | Meaning |
| --- | --- |
| `-v`, `--version` | Print only the version. |
| `-?`, `-h`, `--help` | Print the short usage text. |
| `--silent` | Hide console output. Errors still set exit code 1. |
| `--log <file>` | Also write the output to a file. |
| `--validate` | Run the self-tests; see [[self-validation]]. |
| `--results <file>` | Write the self-test results as `.trx` or `.xml`. `--result` also works. |
| `--depth <n>` | First heading level of the self-test report, 1 to 6; default 1. |

These options belong to the `snap` command and are an error without it; see
[[snap]] for what they do.

| Option | Meaning |
| --- | --- |
| `--camera <camera>` | Required. The camera: its index, `Id`, or name. |
| `--output <file>` | Required. The picture file: `.png`, `.jpg`, `.jpeg`, or `.bmp`. |
| `--crop <x,y,w,h>` | Keep only this region of the picture, in pixels. |
| `--width <px>`, `--height <px>` | Prefer this picture size. |
| `--fps <n>` | Prefer this frame rate. |
| `--warmup <ms>` | Discard the frames of the first milliseconds, 0 to 60000; default 1000. |

**Order.** `--version`, when all arguments are valid, prints only the
version and skips everything else. Without it, the tool prints a banner,
then handles the first of `--help`, `--validate`, the `cameras`
command, the `snap` command, or its default behavior, which is one line
that points to the usage text and the help topics. The command that
[[#silent-log]] prints writes the version to the log file only.

**Errors.** An unknown argument, an option without its value or with an
invalid value, a command given twice, the `cameras` and `snap` commands
together, a `snap` option without the `snap` command, or the `snap`
command without `--camera` or `--output` prints `Error: ...` and exits 1
([[#unknown-option]]). These errors are found
before `--silent` and `--log` take effect, so they are always printed and
never logged. Problems that appear while the `snap` command runs, such as an
unknown camera, are reported through the same channels as the command's
output, so `--silent` hides the message and `--log` keeps it; the exit
code is 1 in every case.

**Help.** `help` is a command, not an option, and must be the first
argument. `--silent` and `--log` do not apply to it, and an unknown topic
exits 2.
