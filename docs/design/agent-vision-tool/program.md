## Program

![AgentVisionTool Structure](AgentVisionToolView.svg)

### Purpose

`Program` is the entry point and execution orchestrator for the AgentVisionTool system. Its single
responsibility is to hand the `help` command to `HelpCommand`, otherwise create a `Context`
from the command-line arguments, dispatch to the appropriate handler based on the parsed flags and command,
and return the exit code.

### Data Model

**Version**: `string` (static property) — The tool version read from
`AssemblyInformationalVersionAttribute` on every access, falling back to `AssemblyVersion`, then
`"0.0.0"`. No caching is applied; callers that need the value more than once should store it
locally.

### Key Methods

**Main**: Entry point for the tool process.

- *Parameters*: `string[] args` — command-line arguments from the host environment.
- *Returns*: `int` — exit code; 0 for success, 1 for expected errors; for the `help` command,
  the exit code of `HelpCommand` (see *Help Command* in the system design).
- *Preconditions*: None.
- *Postconditions*: Exit code reflects whether any errors were reported during execution.

First calls `HelpCommand.Default.TryRun(args, out exitCode)`. The `help` command is not an
option, so `Context.Create` would reject it; when the help command answers, `Main` returns its
exit code. Otherwise creates a `Context` using `Context.Create(args)`, calls `Run(context)`,
and returns `context.ExitCode`. Catches `ArgumentException` and `InvalidOperationException` — writes
`"Error: {message}"` to stderr and returns 1. Catches any other `Exception` — writes
`"Unexpected error: {message}"` to stderr and re-throws so the runtime can record it.

**Run**: Dispatches execution based on parsed flags.

- *Parameters*: `Context context` — the parsed context.
- *Returns*: `void`.
- *Preconditions*: `context` is not null.
- *Postconditions*: Exactly one handler has been called.

Inspects flags in priority order: (1) if `context.Version` is true, calls
`context.WriteLine(Version)` and returns; (2) calls `PrintBanner`; (3) if `context.Help` is
true, calls `PrintHelp` and returns; (4) if `context.Validate` is true, calls
`Validation.Run(context)`; (5) if `context.Cameras` is true, calls `CamerasCommand.Run(context)`;
(6) if `context.Snap` is true, calls `SnapCommand.Run(context)`; (7) otherwise calls `RunToolLogic(context)`.

**PrintBanner**: Writes the tool name, version, and copyright line to `context`.

- *Parameters*: `Context context` — output target.
- *Returns*: `void`.

**PrintHelp**: Writes the usage synopsis (`agentvision [options] [command]` and `agentvision help [topic]`), the
commands table (`cameras` and `snap`), the options of the `snap` command, and the options table to `context`, and ends
with `HelpCommand.Default.Footer`, the pointer to the help command.

- *Parameters*: `Context context` — output target.
- *Returns*: `void`.

**RunToolLogic**: Default behavior when neither an action flag nor a command is given. It writes one line of
guidance that points to the usage text (`agentvision --help`) and the help topics (`agentvision help`) to
`context`. It is not an error, so the exit code stays 0.

- *Parameters*: `Context context` — output target.
- *Returns*: `void`.

### Error Handling

`Main` handles errors at two levels. Expected errors (`ArgumentException` and
`InvalidOperationException`) are written to stderr as `"Error: {message}"` and cause exit
code 1 without a stack trace. Unexpected errors (`Exception`) are written to stderr as
`"Unexpected error: {message}"` and re-thrown so the runtime can record them in event logs.
`Run`, `PrintBanner`, `PrintHelp`, and `RunToolLogic` do not catch exceptions; all errors
propagate to `Main`.

### Dependencies

- **HelpCommand** — `Program.Main` offers the arguments to `HelpCommand.Default.TryRun` before
  the options are parsed, and `PrintHelp` writes `HelpCommand.Default.Footer`.
- **Context** — `Program` reads parsed flags from `Context` and calls `Context.WriteLine` and
  `Context.WriteError` for all output.
- **Validation** — `Program.Run` calls `Validation.Run(context)` when the `--validate` flag is
  set.
- **CamerasCommand** — `Program.Run` calls `CamerasCommand.Run(context)` when the `cameras` command
  is given.
- **SnapCommand** — `Program.Run` calls `SnapCommand.Run(context)` when the `snap` command is given.

### Callers

N/A - entry point, called by the host environment.
