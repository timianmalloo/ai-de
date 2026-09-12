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
  Extracted public surface of AiDe.App.Conductor: 6 types, 18 members, 100% carrying a summary doc comment.
---

# API: `AiDe.App.Conductor`

**6 public types · 18 public members · 100% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `CompositionRootLedger`

*class* — `CompositionRootLedger.cs`

Counts governed-run compositions while it is open — the ledger §F5 clause 5 asserts against when
it says the run was "launched through the same composition root, no second entry point".

**Remarks.** **"One composition root" is unfalsifiable as prose.** It reads identically whether the
UI really calls `RunAsync` or whether a surface quietly assembled a
lane of its own — and N7's floor is explicit that a hand-assembled path is not exit evidence. This
makes the claim a number: one run, one root, `Roots == 1`.





**It counts the attempt, not the success, and that is what makes the falsifier cheap.**
The activity opens before `EngineCatalog.ResolveLaunch` — the first statement in
`RunAsync` that refuses — so two runs with an unknown engine id read
`2` with no adapter, no node and no network. Move the emission below anything throwable and
the falsifier needs a live subscription run to observe, which is a falsifier nobody re-runs.





**It listens rather than instruments.** The idiom, including this reason, is
`TerminalHostingLedger`'s: a counter added inside the thing being
measured is one an edit to that thing can remove with nothing noticing.





**Scoped, because an `ActivityListener` is process-global.** The count
belongs to one exercise, so the ledger is a disposable window over one.

| Member | Summary |
|---|---|
| `string CompositionActivitySource = "aide.conductor.composition"` | The activity source the composition root publishes on. |
| `string GovernedRunComposeActivity = "governed-run.compose"` | The activity `RunAsync` opens for one composition. |
| `long Roots` | How many governed runs were composed since this ledger opened. |
| `CompositionRootLedger Open()` | Opens a ledger. Counting starts here and stops at `Dispose`. |
| `void Dispose()` | Stops counting. A ledger belongs to one exercise, so it is closed with it. |

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





**A read-only turn is the same root with the write half left out** (Ruling 73). The
request carries no lease (`IsReadOnly`), and from that one fact:
the spawn is authorized read-only (`ReadOnly`: the identity gates
unchanged, the goal-block precondition waived), **no worktree is cut** — the ACP session is rooted in the
repository root itself, because the lane cannot write and a throwaway tree would be exactly the
thing the operator must then clean up (Ruling 73's constraint; ADR-0035 roots the compile session
the same way, and outside the repository the constitution does not load) — the session is opened
with `ReadOnlyLaneSession`, no episode is opened and nothing is scored (an episode is
work judged against a done-condition; a Message has none), no seam monitor runs (there is no
lease to monitor), and the permission chooser refuses any edit that arrives anyway. The
composition-root activity is opened once either way, so one send reads one root whichever shape
the turn takes. **Boundary named:** rooted in the repository, the lane loads the repository's
own settings and hooks (`settingSources` user · project · local) — repository content runs
in the lane, the same exposure as opening Claude Code there (ADR-0035 accepts it for the
compile session on the same grounds). After the turn the tree is measured against its own
baseline (`TreeDelta`): a changed tree is Ruling 73 condition (2)'s stop.





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
- **`sink`** — An optional observer of every event this run drains — `RunEventRelay.Publish` is what the Conductor Surface passes. **Null by default, and genuinely inert:** the headless path names no sink and the drain then behaves exactly as it did before this parameter existed.

**Remarks.** **The sink is last, after the cancellation token, on purpose.** The usual .NET ordering
would put a token last, but `ConductorEntry` calls
`RunAsync(request, bound.Token)` positionally and §F5 clause 5 asserts the root count by
ledger — an added parameter must not become a reason to edit the one other caller, because an
edit there is how "one composition root" starts being a claim about two.

## `GovernedRunRequest`

*record* — `GovernedRunRequest.cs`

Everything one governed lane needs to run, as the caller states it.

**Remarks.** **A record rather than a pile of parameters** because the Conductor Surface (deferred by
Ruling 13) and the headless entry must hand `GovernedRunHost` the *same* thing.
A second overload is how a second composition root starts.

| Member | Summary |
|---|---|
| `bool IsReadOnly` | Whether this turn writes nothing — Ruling 73's read-only turn: a Message, or a goal block whose source text named no write scope. |

### `bool IsReadOnly`

Whether this turn writes nothing — Ruling 73's read-only turn: a Message, or a goal block
whose source text named no write scope.

**Remarks.** Derived from the lease's absence, never stored beside it (DM7): the same fact picks the
lane's pin, the spawn's shape and the absent seam monitor, so none can disagree. A lease with
no goal block is not a third shape — the host refuses it by R2 before any engine starts.

## `GovernedRunResult`

*record* — `GovernedRunRequest.cs`

What the governed run did, in the terms its exit evidence is written from.

## `RunEventRelay`

*class* — `RunEventRelay.cs`

Carries the events `GovernedRunHost` drains to a second consumer — the shape
`SessionLane` already takes, so nothing on the console side has to change to receive them.

**Remarks.** **Why a relay and not a second reader.** `AcpEventQueue` is
`SingleReader = true` and the host's own loop is that reader, so "let the console read the
plane's queue too" is impossible by construction rather than merely unwise. The host therefore
*republishes* what it has already drained, and this is the channel it republishes into. The
events are the same instances — nothing is re-normalized, re-sequenced or re-stamped, so the
console cannot hold a second opinion about what arrived (DM7).





**It fits `SessionLane`'s existing constructor.** `Reader` is a
`ChannelReader{T}` of `ObservedRunEvent`, which is exactly the parameter
the lane already declares, so the seam is a new object rather than a changed contract on a type
another node had just finished.





**Unbounded, and that is a bounded risk.** A bounded relay would back-pressure the host
— and therefore the engine — on a console that stopped reading, which turns a closed pane into a
stalled run; an unbounded one holds at most one run's events and is collected with the document
that opened it. What is not acceptable is losing an event silently, because the equality the
sink's oracle asserts would then quietly become an approximation, so a publish that cannot land
is **counted** as `Refused` rather than dropped.

| Member | Summary |
|---|---|
| `ChannelReader<ObservedRunEvent> Reader` | The consumer side — hand this straight to a `SessionLane`. |
| `long Published` | How many events reached the relay. The number the run's own count is compared with. |
| `long Refused` | How many could not. Above zero means the console saw fewer events than the run did, and it says so instead of the two numbers quietly disagreeing. |
| `void Publish(ObservedRunEvent observed)` | The sink itself: pass this as `GovernedRunHost.RunAsync`'s `sink`. |
| `void Complete()` | Closes the relay, which ends the lane's drain once it has read what is already queued. |
| `void Dispose()` | Closes the relay — `Complete`, so a `using` and an explicit end agree. |

### `void Complete()`

Closes the relay, which ends the lane's drain once it has read what is already queued.

**Remarks.** Called when the run ends, not when the document closes: a lane whose run is over should stop
waiting, and a lane whose document is closing is disposed by the document.
