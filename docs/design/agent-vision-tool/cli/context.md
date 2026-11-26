### Context

![Cli Structure](CliView.svg)

#### Purpose

`Context` handles command-line argument parsing and program output for one tool invocation. Its
single responsibility is to parse the argument list, expose the parsed flags as read-only
properties, own the two output channels (console and log file), and derive the exit code from
whether any errors were reported.

#### Data Model

**_logWriter**: `StreamWriter?` — Log file writer; `null` when logging is not active.

**_hasErrors**: `bool` — Set to `true` on the first `WriteError` call; once set, cannot return
to `false` within the same invocation.

**Version**: `bool` — `true` when `-v` or `--version` was present in the argument list.

**Help**: `bool` — `true` when `-?`, `-h`, or `--help` was present in the argument list.

**Silent**: `bool` — `true` when `--silent` was present in the argument list.

**Validate**: `bool` — `true` when `--validate` was present in the argument list.

**Cameras**: `bool` — `true` when the `cameras` command was present in the argument list.

**Snap**: `bool` — `true` when the `snap` command was present in the argument list.

**Camera**: `string?` — The camera (index, identifier, or name) supplied after `--camera`, or `null`.

**OutputFile**: `string?` — The picture file supplied after `--output`, or `null`.

**Crop**: `CropRectangle?` — The region supplied after `--crop` as `x,y,w,h`, or `null`. The type is the library's
value type; the Context only parses it.

**Width**, **Height**: `int?` — The sizes supplied after `--width` and `--height`, from 1; `null` when not given.

**Fps**: `double?` — The frame rate supplied after `--fps`, a finite number above 0 read with the invariant culture;
`null` when not given.

**WarmupMilliseconds**: `int?` — The warm-up supplied after `--warmup`, from 0 to 60000; `null` when not given, so
the library default applies.

**ResultsFile**: `string?` — Path supplied after `--results` or `--result`, or `null` if
neither flag was present.

**HeadingDepth**: `int` — Heading depth for markdown output; valid range 1–6, default 1;
supplied via `--depth`.

**ExitCode**: `int` (derived) — Returns 1 if `_hasErrors` is true; returns 0 otherwise.

#### Key Methods

**Create**: Factory method that parses arguments and returns a fully initialized `Context`.

- *Parameters*: `string[] args` — raw command-line argument array.
- *Returns*: `Context` — a new instance with all flags set.
- *Preconditions*: `args` is not null.
- *Postconditions*: All flag properties reflect the parsed argument state; the log file is open
  if `--log` was supplied.

Delegates to the private `ArgumentParser` helper to parse flags and the `cameras` and `snap` commands (positional
words, recognized by an ordinal comparison, that may stand among the options and may be given only once) and the
options of the `snap` command (`--camera`, `--output`, `--crop`, `--width`, `--height`, `--fps`, `--warmup`). After
the last argument the parser checks that the commands and options belong together: the `snap` and `cameras`
commands are not combined, a `snap` option is not given without the `snap` command, and the `snap` command has
`--camera` and `--output` (unless help or the version was asked for, so that `snap --help` works). Then it opens the
log file by
calling `OpenLogFile` if `--log` was present. Throws `ArgumentException` for unknown or
malformed arguments; throws `InvalidOperationException` if the log file cannot be opened.

**WriteLine**: Writes a message to standard output and to the log file.

- *Parameters*: `string message` — the message to write.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Message is on stdout (unless `Silent`) and in the log file (if open).

**WriteError**: Writes an error message, sets the error state, and records to the log file.

- *Parameters*: `string message` — the error message.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: `_hasErrors` is true; message is on stderr in red (unless `Silent`) and in
  the log file (if open).

**Dispose**: Disposes the log file writer.

- *Parameters*: None.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: `_logWriter` is disposed and set to null; any buffered log content is
  flushed.

#### Error Handling

`Create` throws `ArgumentException` ("Unsupported argument '{arg}'") for any unrecognized flag
or command, or missing required value, and ("The 'cameras' command can be given only once")
when the `cameras` command is repeated, and likewise for `snap`. It also throws `ArgumentException` for an
invalid option value (a `--crop` that is not four integers `x,y,width,height` with `x` and `y` not negative and
`width` and `height` positive; a `--width`, `--height` below 1; a `--fps` that is not a number above 0; a
`--warmup` outside 0 to 60000), for the `snap` and `cameras` commands together ("Only one command can be given"),
for a `snap` option without the command ("{option} can be used only with the 'snap' command"), and for a `snap`
command without its options ("The 'snap' command requires --camera <index|id|name>" or "... requires --output
<file>"). It throws `InvalidOperationException`
("Failed to open log file '{path}': {detail}") when the `--log` file cannot be opened. Both
exceptions propagate to `Program.Main`.

`WriteLine` and `WriteError` do not throw; they write to whichever output channels are
available.

`Dispose` does not throw; any disposal errors are silently ignored.

#### Dependencies

- **.NET BCL** — `Console`, `StreamWriter`, and `Path` are used for the output channels.
- **CropRectangle** of the AgentVisionCore library — the value type that holds the parsed `--crop` region. No other
  tool units are used.

#### Callers

- **Program** — creates `Context` via `Context.Create` and calls `WriteLine` and `WriteError`.
- **SnapCommand** — reads the `snap` options and calls `WriteLine` and `WriteError`.
- **Validation** — receives `Context` from `Program` and calls `WriteLine` and `WriteError`.
