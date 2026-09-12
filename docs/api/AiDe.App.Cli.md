---
id: api-aide-app-cli
title: "API: AiDe.App.Cli"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Cli: 3 types, 4 members, 71% carrying a summary doc comment.
---

# API: `AiDe.App.Cli`

**3 public types · 4 public members · 71% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `CliEntry`

*class* — `CliEntry.cs`

The compile step's CLI verbs — `aide compile fold` and `aide session purge` — reached
from the same process the shell runs in, without a window (ADR-0033 rule 2's third
`Project()` site; ADR-0034 rule 6's deletion command).

**Remarks.** **Exit codes are the contract**, as the headless conductor's are: **0** done,
**3** refused or did not complete (the reason is in the output file), **64** the
arguments were wrong. **The output is a file, always**: a `WinExe` has no console to
print to, so every verb writes what it did to `--out` (a default under the temp directory
when none is given) and echoes it to the console when one happens to be attached.





**The dispatch line in `App.OnStartup` is the Design lane's** (a seam request,
recorded in the Proof Pack): `if (Cli.CliEntry.IsRequested(args)) { … RunAsync … }` beside
the conductor's. Until it lands the verbs are reachable headlessly (the tests) and not from the
shell's process arguments.

| Member | Summary |
|---|---|
| `bool IsRequested(IReadOnlyList<string> args)` | Whether these process arguments name one of the compile step's verbs. |
| `Task<int> RunAsync(IReadOnlyList<string> args, TextWriter? console = null, Func<string, bool>? confirm = null)` | Runs the verb. Returns the exit code; the output file's path is written to  when given. |

### `Task<int> RunAsync(IReadOnlyList<string> args, TextWriter? console = null, Func<string, bool>? confirm = null)`

Runs the verb. Returns the exit code; the output file's path is written to  when given.

- **`args`** — The process arguments, verb first.
- **`console`** — Where to echo the output, or null.
- **`confirm`** — The purge's confirmation prompt (the plan's text in, yes/no out); null means `--yes` is required.

## `CompileFold`

*class* — `CompileFold.cs`

`aide compile fold --workspace <root> --session <id> [--out <file>]`: recomputes
`Project(Fold(rows))` from the store's own rows and emits the fold and every envelope's
projection beside the `projection_sha` its `submitted` row recorded — P-D2's rebuild,
computed by the product's one `Project()` (its third named call site), never by a second
fold in Python (ADR-0033 "Alternatives considered").

**Remarks.** The reader opens the file only when no writer holds it (`FileShare.None`): a composer with
the session open is a visible refusal (exit 3), never a partial fold. The rebuild has no held
attachment bodies, so the projection's `prompt` is absent here — `text_sha256` is the
send's witness, `projection_sha` the rebuild's.

| Member | Summary |
|---|---|
| `int Run(IReadOnlyList<string> args, TextWriter? console)` | **(gap)** |

## `PurgeHistoryVerb`

*class* — `PurgeHistoryVerb.cs`

`aide session purge <id> --workspace <root> [--yes] [--out <file>]`: deletes the
session's `envelope-events.jsonl` and nothing else (US-D13; ADR-0034 rule 6). The
confirmation prints the session's name, id, workspace root, the resolved file path, the envelope
count and the newest `at` — an identity, never a count alone (DC-120).

| Member | Summary |
|---|---|
| `int Run(IReadOnlyList<string> args, TextWriter? console, Func<string, bool>? confirm)` | **(gap)** |
