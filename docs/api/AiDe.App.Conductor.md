---
id: api-aide-app-conductor
title: "API: AiDe.App.Conductor"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Conductor: 4 types, 6 members, 100% carrying a summary doc comment.
---

# API: `AiDe.App.Conductor`

**4 public types · 6 public members · 100% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `ConductorEntry`

*class* — `ConductorEntry.cs`

The headless door onto `GovernedRunHost`: read a run file, run it, write the result.

**Remarks.** **A door, not a second composition root.** Everything here is argument parsing and file
I/O; the wiring lives in `GovernedRunHost` and the deferred Conductor Surface will
call the same method with the same record. The moment this file composes anything itself, the
evidence stops being about the shipped path.





**It writes files rather than printing.** The shell is a `WinExe` and has no console
attached when launched from one, so a run that reported to stdout would report to nothing. A JSON
result and a transcript are also better evidence than scrollback: they can be re-read.

| Member | Summary |
|---|---|
| `string ConductArgument = "--conduct"` | The argument that puts the shell into headless conductor mode. |
| `string OutArgument = "--out"` | Where to write the result. Defaults to the run file's name with `.result.json`. |
| `bool IsRequested(IReadOnlyList<string> args)` | Whether these process arguments ask for a headless governed run. |
| `Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken = default)` | Runs what the file describes and writes the result beside it. Returns the process exit code. |

### `Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken = default)`

Runs what the file describes and writes the result beside it. Returns the process exit code.

**Remarks.** **Exit codes are the contract:** **0** the run completed and was scored into a
comparable cell, **1** the run completed but its exit evidence does not hold, **3** the
run did not complete, **64** the arguments were wrong. A failure that exits 0 is how a
weak run passes.

## `GovernedRunHost`

*class* — `GovernedRunHost.cs`

The App-layer composition root for a governed ACP lane — spec §6.2, and Phase 1's only entry to a
governed run.

**Remarks.** **One composition root, deliberately.** The Conductor Surface is deferred (Ruling 13) and
the headless entry exists now; both call `RunAsync`. N7's floor is explicit that a
hand-assembled harness does not count as exit evidence — a second assembly path would let the
demonstrated run and the shipped run differ in the wiring, which is the one thing the evidence is
about.





**What it composes, in the order the order matters:** catalog → engine process →
peer/client handshake → *observed* auth → spawn authorization → worktree → governed episode
→ ACP session rooted in that worktree → prompt → seams → close → score. The auth observation sits
before the authorization because the gate is on what the adapter says about itself, not on what
the configuration claims; and the worktree is cut after the authorization because a refused spawn
must leave nothing behind.





**It hosts no terminal, and the run says so with a number.**
`TerminalHostingLedger` is opened around the whole run and its count travels on the
result, so the absence is a measurement rather than a sentence.

| Member | Summary |
|---|---|
| `string NotRecorded = "not recorded"` | What every absent measurement reads as. Never zero, never a plausible substitute. |
| `Task<GovernedRunResult> RunAsync(` | Runs one governed lane to completion and scores it. |

### `Task<GovernedRunResult> RunAsync(`

Runs one governed lane to completion and scores it.

- **`request`** — What to run.
- **`cancellationToken`** — Bounds the whole run.

## `GovernedRunRequest`

*record* — `GovernedRunRequest.cs`

Everything one governed lane needs to run, as the caller states it.

**Remarks.** **A record rather than a pile of parameters** because the Conductor Surface (deferred by
Ruling 13) and the headless entry must hand `GovernedRunHost` the *same* thing.
A second overload is how a second composition root starts.

## `GovernedRunResult`

*record* — `GovernedRunRequest.cs`

What the governed run did, in the terms its exit evidence is written from.
