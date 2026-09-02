---
id: api-aide-core-terminal
title: "API: AiDe.Core.Terminal"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Terminal: 21 types, 81 members, 71% carrying a summary doc comment.
---

# API: `AiDe.Core.Terminal`

**21 public types · 81 public members · 71% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `AgentReadinessProfile`

*record* — `AgentReadinessProfiles.cs`

One agent's readiness marker, and where it came from.

| Member | Summary |
|---|---|
| `bool Launchable` | Whether this profile can offer its own "New … session" command. |
| `string CommandId` | The command id for this harness's session, derived from the executable name. |
| `string CommandIdFor(string agent)` | The one place the id is spelled, so the catalog, the menu and the controller agree. |

## `AgentReadinessProfiles`

*class* — `AgentReadinessProfiles.cs`

Per-agent readiness markers, built in and user-supplied.

**Remarks.** **Why this exists.** The built-in markers are a guess about what an agent's prompt looks
like, and a guess that does not match means the agent is refused forever — a correct refusal that
is also a dead end. Nothing shipped could change that without a rebuild, so the honest fix is to
let the marker be configured where the agent actually runs.





**A bad pattern fails loudly, never open.** A regex that does not compile is reported and
the agent keeps its built-in marker; it never degrades to "assume ready", because the one thing
worse than refusing a ready agent is dispatching into an unready one — the failure
`spikes/agent-dispatch` measured.





**Tuning is measurement, not guesswork.** `LastJudged`
exposes the tail the watcher actually tested, so a user fixing a pattern reads what the agent
printed rather than reasoning about what it probably prints.

| Member | Summary |
|---|---|
| `string FileName = "agent-readiness.json"` | **(gap)** |
| `IReadOnlyList<string> Problems { get; }` | Patterns that were rejected, and why. Surfaced rather than absorbed. |
| `IReadOnlyCollection<AgentReadinessProfile> All` | **(gap)** |
| `AgentReadinessProfiles BuiltIn { get; } = new(` | **(gap)** |
| `AgentReadinessProfiles Load(string? stateDirectory)` | Loads the overrides beside the built-ins. A missing file is the ordinary case. |
| `AgentReadinessProfile? For(string agent)` | The marker for an agent, or null when nothing reports readiness for it. |
| `AgentReadinessWatcher? WatcherFor(string agent)` | A watcher for an agent, or null — the caller must treat null as "cannot establish". |
| `string WriteTemplate(string stateDirectory)` | Writes the current markers as a starting point for the user to edit. |

## `AgentReadinessWatcher`

*class* — `AgentReadinessWatcher.cs`

Watches an agent's SCREEN for the marker that says it is listening.

**Remarks.** **Why this exists.** A shell reports readiness through OSC 133 signed with the session
nonce. An agent CLI reports nothing, so before this it could only ever be REFUSED — a correct
refusal, and a dead end. Measured: a prompt dispatched into Claude Code's first-run trust gate
was consumed by that dialog (`spikes/agent-dispatch`).





**It matches the rendered screen, not the byte stream.** The first version matched the
tail of the output, which for a line-oriented shell is the same question and for an agent is not:
`spikes/agent-readiness` measured a full-screen TUI drawn with absolute cursor addressing,
where the last bytes are wherever the cursor went, not what the user is looking at.





**Through the SAME screen the pane renders** — `erminalScreen` driven by
`tParser`. A second screen model was written for this and then deleted: two models
of one terminal disagree the first time either is fixed, and readiness disagreeing with what the
user is looking at is the whole defect this was built to close.





**It is weaker evidence than the nonce and is labelled as such.** A pattern can match a
line that merely mentions the prompt, and output is in principle forgeable. It establishes that
the agent is *listening* — never that it ACCEPTED anything, which ADR-0007 still gates behind
an authenticated acknowledgement.





**Attention is separate from readiness.** An agent showing a trust gate is not busy and
not ready; it is waiting for a person. Collapsing that into "not ready" leaves the user watching
a pane that refuses and never says why — and the measurement showed that gate is the NORMAL first
screen, not an edge case.

| Member | Summary |
|---|---|
| `AgentReadinessWatcher(string readyPattern, string? attentionPattern = null,` | **(gap)** |
| `bool IsReady { get; private set; }` | True when the marker is on the last drawn line of the screen. |
| `bool NeedsAttention { get; private set; }` | True when the screen is waiting on a person rather than on the agent. |
| `string AttentionLine { get; private set; } = string.Empty` | The line that matched `eedsAttention`, for showing the user. |
| `string LastJudged { get; private set; } = string.Empty` | The screen this watcher last judged. |
| `string Pattern` | The pattern being tested, so a refusal can name the marker that did not match. |
| `void Observe(ReadOnlySpan<char> text)` | Feeds output through the screen, then re-judges it. |
| `IReadOnlyDictionary<string, string> KnownAgents { get; } =` | Well-known prompt markers, so a user does not have to invent one. |
| `IReadOnlyDictionary<string, string> KnownAttention { get; } =` | Screens that are waiting on a person, per agent. |

### `string LastJudged { get; private set; } = string.Empty`

The screen this watcher last judged.

**Remarks.** Tuning a marker by reasoning about what an agent probably prints is how a pattern that never
matches survives. This is the rendered screen, so a user fixing a pattern reads what was
actually on it.

### `IReadOnlyDictionary<string, string> KnownAgents { get; } =`

Well-known prompt markers, so a user does not have to invent one.

**Remarks.** Conservative on purpose: a loose pattern that matches an agent's own prose about prompts
would report readiness mid-answer. **These remain unverified against a READY agent** —
reaching one means answering the trust gate, which this tool will not do on the user's behalf
— so they are the starting point for tuning, not a measured fact.

### `IReadOnlyDictionary<string, string> KnownAttention { get; } =`

Screens that are waiting on a person, per agent.

**Remarks.** Measured, not imagined — `spikes/agent-readiness` captured this exact question, and it
appears even in a directory whose sessions run every day. It is the normal first screen.

## `TerminalSessionRequest`

*record* — `ConPtyTerminalSession.cs`

What a caller must supply to start a real terminal session.

## `ShellIntegrationMode`

*enum* — `ConPtyTerminalSession.cs`

Whether the runtime installs its OSC shell integration into the session's shell.

**Remarks.** Opt-in per session rather than always-on: `ommandLine` may be any executable, and
decorating an arbitrary command with PowerShell arguments would corrupt it.

## `ConPtyTerminalSession`

*class* — `ConPtyTerminalSession.cs`

The real terminal runtime: one ConPTY, one process, one owner loop (ADR-0005).

**Remarks.** **Input and output loops are separate**, which is a correctness requirement rather than
tidiness. ConPTY's pipes are finite: a process producing output faster than we read it blocks on
its own write, and if the same loop were responsible for both reading and writing, a large write
would deadlock against a full output pipe with neither side able to proceed.





**Output is bounded and drops the oldest.** The architecture budgets 1 MiB/s of sustained
output; a reader that falls behind must not be able to grow the queue without limit. Dropping is
therefore a designed state — reported through `Truncated` and
`OutputOverload` — because for a terminal, the *newest* output is the
interesting output and stalling the process to preserve scrollback would be the wrong trade.





**Terminal bytes never leave this object except through `utput`.** They are
not logged, traced, or attached to telemetry, per the spec's privacy rule. The telemetry below
counts bytes; it never carries them.

| Member | Summary |
|---|---|
| `string SessionId { get; }` | **(gap)** |
| `long Generation { get; private set; }` | **(gap)** |
| `SessionProcessingClass ProcessingClass { get; }` | **(gap)** |
| `string ShellIntegrationNonce { get; }` | The secret this session's injected shell integration must echo back in its OSC 133 sequences for them to be believed. |
| `bool HasReadinessEvidence { get; private set; }` | Whether anything AUTHENTICATES this session's claim about its own state. |
| `ChannelReader<TerminalChunk> Output` | **(gap)** |
| `SessionActivity Activity` | **(gap)** |
| `Task<ConPtyTerminalSession> StartAsync(` | Creates the pseudo console, starts the process inside a kill-on-close job, and pumps. |
| `Task<PtyWriteResult> WriteAsync(` | **(gap)** |
| `Task<SessionExit> WaitForExitAsync(CancellationToken cancellationToken)` | **(gap)** |
| `ValueTask ResizeAsync(int columns, int rows, CancellationToken cancellationToken)` | **(gap)** |
| `ValueTask DisposeAsync()` | **(gap)** |

### `string ShellIntegrationNonce { get; }`

The secret this session's injected shell integration must echo back in its OSC 133 sequences
for them to be believed.

**Remarks.** Public because the integration script has to be given it; per-session and in-memory because a
nonce that outlived the session would authenticate a later child's claims. It is not a
credential for anything else — the worst a leak buys is the ability to lie about activity.

### `bool HasReadinessEvidence { get; private set; }`

Whether anything AUTHENTICATES this session's claim about its own state.

**Remarks.** True only with shell integration: OSC 133 signed with the session nonce is what makes
`Ready` a claim rather than an inference. Without it, activity is
derived from output timing — and a quiet agent mid-thought looks exactly like an idle one,
which is how a prompt ends up in a confirmation dialog.

## `EnvironmentHealth`

*class* — `EnvironmentHealth.cs`

Whether the environment a terminal hands its children can actually be carried by them.

**Remarks.** **Reported as "the agent sessions do not have my profile or my environment variables".**
The measurement found something the product did not cause and could not see: this machine's PATH
is **22,297 characters**, and `cmd.exe` silently drops a variable that large. Any child
that runs through cmd — which is every `.cmd` shim, and therefore every npm-installed CLI —
starts with an **empty PATH** and cannot find node, git, or itself.





**Proven necessary and sufficient, and proven not to be ours.** The same shim run from a
plain PowerShell with no part of this product involved also received an empty PATH; trimming PATH
to 1,799 characters made it arrive intact. AI-DE passes the environment correctly — PowerShell
started from the same inherited block reads all 22,297 characters and resolves `claude`.





**What was ours is that it was invisible.** The terminal opened, looked healthy, and the
user's tools were simply absent — a clean surface over a broken environment, which is DC-025
wearing a different hat. This states it, with the number and the remedy, so the failure is
attributable instead of mysterious.





It never edits the user's PATH. A tool that silently rewrites the environment to make
itself work is a tool that has hidden the problem from the person who has to fix it, and PATH is
theirs — the entries causing this belong to another program's build.

| Member | Summary |
|---|---|
| `int CmdVariableLimit = 8151` | The size past which `cmd.exe` stops carrying a variable. |
| `int CmdPairLimit = 8190` | The measured cut-off, on `NAME=VALUE` as a whole — not on the value. |
| `IReadOnlyList<string> Inspect(string? path = null)` | Findings about the whole environment, in words a user can act on. Empty when healthy. |
| `int DeadEntryThreshold = 10` | PATH entries that point at directories which do not exist. |

### `int CmdVariableLimit = 8151`

The size past which `cmd.exe` stops carrying a variable.

**Remarks.** **Bisected, not quoted.** On the reporting machine `cmd.exe` carried a variable of
8,151 characters and dropped one of 8,152 — printing "The input line is too long" and then
losing the value. The documented figure is 8,191; the ~40-character difference is the
variable's own name and the block overhead, so the exact cut-off shifts slightly with the
name. The message still says "may be dropped" because of that, not because the number is
unmeasured.

### `int CmdPairLimit = 8190`

The measured cut-off, on `NAME=VALUE` as a whole — not on the value.

**Remarks.** **Bisected 2026-09-01**, by handing a child a controlled environment and asking the
CHILD what it received (the parent's copy is not evidence — DC-027's own rule). The boundary
is exact and identical at four name lengths:



name len   max value   name + 1 + value
3       8,186              8,190
13       8,176              8,190
40       8,149              8,190
120       8,069              8,190



**So the limit is on the PAIR**, and comparing the value alone is wrong for any name
longer than 39 characters: an 8,150-char value under a 40-char name passes a value-only check
and is dropped by cmd.exe. Latent here — the longest name on the measured machine is 34 — and
it stops being latent the moment something adds longer names, which is exactly what §3 of the
session-registration spec proposes.




`mdVariableLimit` is kept for the PATH message, where it reads as the
budget a user has to get under.

### `IReadOnlyList<string> Inspect(string? path = null)`

Findings about the whole environment, in words a user can act on. Empty when healthy.

**Remarks.** PATH is checked in detail because it is the one whose loss stops tools resolving, but ANY
oversized variable is dropped the same way and by the same mechanism — so every variable is
measured and the others are named together. Checking only the variable that happened to bite
is how the second instance of a class gets found by a user rather than by the tool.

### `int DeadEntryThreshold = 10`

PATH entries that point at directories which do not exist.

**Remarks.** **Caught at twenty, not at a hundred and eighty-seven.** The oversize check only
fires once PATH is past cmd's limit — which is to say, after the damage. This one fires on
the SHAPE: something appended 187 throwaway build directories to a persisted PATH and never
removed them, and every one of them was already gone from disk. Regrowth looks identical and
starts small.





A handful of dead entries is normal — an uninstalled tool, a moved SDK — so this stays
quiet below a threshold. It is looking for a pattern of accumulation, not for tidiness.

## `OscKind`

*enum* — `OscParser.cs`

Which OSC sequence arrived. Says nothing about whether it was believed.

## `OscDisposition`

*enum* — `OscParser.cs`

What the parser did about it.

## `struct`

*record* — `OscParser.cs`

One sequence and its outcome. Deliberately carries no payload text.

**Remarks.** The absent payload is the privacy control, not an omission. This type is what telemetry counts
and what a diagnostic view would show, and terminal text may reach neither — so there is nowhere
on this record for a byte of the child's output to sit.

| Member | Summary |
|---|---|
| `TerminalColor Default` | **(gap)** |
| `TerminalColor FromIndex(int index)` | **(gap)** |
| `TerminalColor FromRgb(byte r, byte g, byte b)` | **(gap)** |
| `TerminalCell Blank` | **(gap)** |
| `TerminalPen Default` | **(gap)** |

## `OscParser`

*class* — `OscParser.cs`

Reads OSC sequences out of a terminal byte stream and turns the authenticated ones into advisory
`essionActivity` claims.

**Remarks.** **Everything here is a claim from an untrusted process.** The child in a terminal is
often the thing being investigated, and it chooses every byte. So the parser's job is not to
interpret OSC faithfully — it is to decide what may be believed. Three rules follow.





**1. State claims need the session nonce.** OSC 133 is public and widely copied; any
process that can print can emit it. The nonce is injected into the shell integration we install,
so a claim carrying it came from that integration and a claim without it came from something
else. Advisory state still drives what the user sees, and a session reporting `Ready`
mid-command is a lie the UI renders faithfully.





**2. Host actions are refused outright, nonce or not** (ADR-0005, threat model boundary
"terminal output → UI"). OSC 52 writes the clipboard and OSC 8 carries a URI; both are actions
taken by *us* on the child's instruction. Sanitising them would presume we can separate a
safe payload from a hostile one when the child chose all of it. There is no clipboard code path
here to reach.





**3. Nothing is retained.** The parser sees every byte the child writes, so anything it
keeps is terminal text living outside the bounded, ephemeral output channel the spec confines it
to. The payload buffer is cleared at every terminator and never leaves this object.





OSC state is **never agent acceptance** (ADR-0007). Nothing here may be read as a user
or an agent having agreed to anything; it reports only that a process is or is not working.





Not thread-safe: one parser belongs to one session's single reader loop.

| Member | Summary |
|---|---|
| `int MaxPayloadBytes = 1024` | Bytes buffered for one sequence before it is abandoned. |
| `OscParser(string sessionNonce)` | **(gap)** |
| `int PendingPayloadBytes` | Bytes currently held for an in-flight sequence. Zero whenever none is in flight. |
| `string NewNonce()` | A fresh session nonce: 128 random bits, hex encoded. |
| `SessionActivity? Consume(ReadOnlySpan<byte> bytes, List<OscEvent> events)` | Feeds one chunk of session output through the scanner. |

### `int MaxPayloadBytes = 1024`

Bytes buffered for one sequence before it is abandoned.

**Remarks.** Without a cap, `ESC ]` followed by an endless stream costs us one byte per byte of
theirs and the ceiling is whatever the child chooses. Every sequence we honour is well under
100 bytes; the headroom is for the ones we refuse.
simplify: a flat byte cap rather than a per-kind budget; ceiling 1 KiB; upgrade trigger = a
sequence we need to honour does not fit.

### `OscParser(string sessionNonce)`

- **`sessionNonce`** — The secret shared with this session's injected shell integration. Must be non-empty: a parser with no nonce would have nothing to check claims against, and failing open there would make the control absent exactly when nonce generation had gone wrong.

### `int PendingPayloadBytes`

Bytes currently held for an in-flight sequence. Zero whenever none is in flight.

**Remarks.** Exists so the retention bound is observable rather than merely intended.

### `SessionActivity? Consume(ReadOnlySpan<byte> bytes, List<OscEvent> events)`

Feeds one chunk of session output through the scanner.

- **`bytes`** — Output exactly as read; never modified and never copied out.
- **`events`** — Appended to, one entry per complete or abandoned sequence.

**Returns.** The last authenticated activity claim in this chunk, or `null` if there was none. Last rather than first because a chunk may hold a whole command's worth of sequences, and the state the session should end up in is the one the shell said most recently.

## `ShellIntegration`

*class* — `ShellIntegration.cs`

Builds the shell-side half of the OSC contract: the script that reports session state, signed
with the session nonce.

**Remarks.** **Why the product ships this rather than asking users to install it.** The nonce is
generated per session and lives only in memory, so no script a user commits to their profile can
ever carry it. The integration has to be composed at session start, by us, from that session's
own secret — which is also what makes the control meaningful: the script is the one thing in the
world that knows the nonce, so a claim carrying it came from the shell we started.





**All of the loop, or none of it.** The parser makes OSC authoritative on the first
authenticated claim, retiring the output heuristic. An integration that marked the prompt
(`D`, `A`, `B`) but not command start (`C`) would therefore pin the session at
`Ready` for the whole duration of every command — a confident wrong answer, and strictly
worse than the coarse signal it replaced. So the script checks it can hook line-accept
**before** it overrides anything, and returns without installing if it cannot. A session with
no integration is a supported outcome; a session with half of one is not.





**Scope.** PowerShell only. It is the shell the product launches by default and the one
with a supported line-accept hook. `cmd.exe` has no equivalent — its prompt is a string, not
a function, and there is nowhere to run code when a command starts — so a cmd session keeps the
heuristic rather than getting a half-loop.

| Member | Summary |
|---|---|
| `string PowerShellScript(string nonce)` | The integration script for one session. |
| `string PowerShellCommandLine(string executablePath, string nonce)` | A command line that launches  with the integration installed and the shell left interactive. |
| `string AgentCommandLine(string shellPath, string agent, string nonce)` | A command line that runs  INSIDE the user's login shell. |

### `string PowerShellScript(string nonce)`

The integration script for one session.

- **`nonce`** — This session's `scParser` nonce. Must be plain lowercase hex.

**Throws `ArgumentException`.** The nonce is not plain hex.

### `string PowerShellCommandLine(string executablePath, string nonce)`

A command line that launches  with the integration
installed and the shell left interactive.

**Remarks.** The script travels as `-EncodedCommand` (UTF-16LE base64) rather than as text. Passing
it literally would put its quotes, semicolons and `$` through two parsers — the Win32
command line and PowerShell's own — and the first apostrophe in a user's prompt would break
it. Base64 has no metacharacters, so there is nothing to escape and nothing to get wrong.

### `string AgentCommandLine(string shellPath, string agent, string nonce)`

A command line that runs  INSIDE the user's login shell.

**Remarks.** **An agent used to be launched directly, and that was the defect.** Reported as
"the agent sessions do not have my profile or my environment variables", and the measurement
found something sharper than a missing profile: a child that is a `.cmd` or `.bat`
shim — which is what every npm-installed CLI is — starts through `cmd.exe`, and
**cmd drops any environment variable past its own limit**. This machine's PATH is 22,297
characters, so a cmd-hosted agent starts with an **empty PATH** and cannot find node, git
or anything else. Measured: a cmd child through this ConPTY reported
`PATH=[]` while PowerShell started from the same inherited block reported all 22,297
characters and resolved `claude`.





**So the agent runs where the user's own commands run.** The login shell loads the
profile — the aliases, functions and variables the request was actually about — resolves
PATHEXT so a `.cmd` or `.ps1` shim works, and handles a long PATH correctly
because it is not cmd. The agent becomes exactly what typing its name in their terminal
does, which is the only definition of "works with my profile" that holds up.





The agent's name is passed as a single-quoted PowerShell string with internal quotes
doubled, and invoked with `&`. A name is not a command line here: quoting it means a
path with a space runs, and a name with an apostrophe cannot end the string early.

## `TerminalActivityState`

*class* — `TerminalActivityState.cs`

Decides which of the session's competing signals owns `essionActivity`.

**Remarks.** There are three signals and they routinely disagree. **Output arriving** is a coarse
heuristic — bytes appeared, so something is presumably working. **An authenticated OSC claim**
is the shell itself reporting what it is doing. **Overload** is our own resource state.





The heuristic and OSC conflict directly: a shell that finishes a command prints its prompt,
and that prompt is output, so the heuristic would immediately flip the session out of the
`Ready` the shell just announced. Whichever signal is applied last wins, which would make the
coarser one authoritative by accident. So the **first authenticated claim makes OSC
authoritative** for the rest of the session, and the heuristic retires — it exists to serve
sessions with no shell integration, and a session that has produced one nonced claim has it.





Overload outranks both, because neither the shell nor a byte count can tell us we have
stopped dropping output — only we know that. `Ended` outranks
everything: a dead process is not `Ready`, whatever the last bytes in the pipe claimed, and
output outliving the process that wrote it is ordinary rather than exotic.





Not thread-safe by design: the ConPTY session already serialises state under its own lock,
and a second lock inside here would be a redundant one to reason about.

| Member | Summary |
|---|---|
| `SessionActivity Current` | The state the session should report right now. |
| `bool OscAuthoritative { get; private set; }` | Has an authenticated OSC claim arrived? Once true the output heuristic no longer applies. |
| `void OnOutput()` | Output arrived. The fallback signal, for sessions with no shell integration. |
| `void OnOscClaim(SessionActivity claimed)` | An OSC claim that carried the session nonce. Advisory, never agent acceptance. |
| `void OnOverload()` | Output is being dropped to stay inside the buffer budget. |
| `void OnEnded()` | The process ended. Final. |

## `TerminalColorKind`

*enum* — `TerminalScreen.cs`

How a cell's colour is specified.

## `struct`

*record* — `TerminalScreen.cs`

A cell colour, as the wire expresses it.

**Remarks.** Deliberately not a rendering type. Keeping the model in *terminal* terms — "palette index 4",
not "this shade of blue" — is what lets the theme decide what index 4 looks like, and what keeps
the whole screen model free of a UI framework. `efault` is a distinct case rather
than a magic index because "whatever the theme's foreground is" is not a colour.

| Member | Summary |
|---|---|
| `TerminalColor Default` | **(gap)** |
| `TerminalColor FromIndex(int index)` | **(gap)** |
| `TerminalColor FromRgb(byte r, byte g, byte b)` | **(gap)** |
| `TerminalCell Blank` | **(gap)** |
| `TerminalPen Default` | **(gap)** |

## `CellAttributes`

*enum* — `TerminalScreen.cs`

Non-colour styling carried by a cell.

## `struct`

*record* — `TerminalScreen.cs`

One character cell.

| Member | Summary |
|---|---|
| `TerminalColor Default` | **(gap)** |
| `TerminalColor FromIndex(int index)` | **(gap)** |
| `TerminalColor FromRgb(byte r, byte g, byte b)` | **(gap)** |
| `TerminalCell Blank` | **(gap)** |
| `TerminalPen Default` | **(gap)** |

## `struct`

*record* — `TerminalScreen.cs`

The style subsequent writes are drawn in — the terminal's current pen.

| Member | Summary |
|---|---|
| `TerminalColor Default` | **(gap)** |
| `TerminalColor FromIndex(int index)` | **(gap)** |
| `TerminalColor FromRgb(byte r, byte g, byte b)` | **(gap)** |
| `TerminalCell Blank` | **(gap)** |
| `TerminalPen Default` | **(gap)** |

## `EraseExtent`

*enum* — `TerminalScreen.cs`

How much of a line or screen an erase covers.

## `TerminalScreen`

*class* — `TerminalScreen.cs`

What a terminal is, once the bytes have been interpreted: a grid of styled cells and a cursor.

**Remarks.** **No WPF, by design.** Every rule here — wrapping, scrolling, what an erase covers,
where the cursor lands — is a data-structure question. Behind a rendering framework each one
would be testable only by drawing pixels and reading them back, and the rules would be verified
approximately or not at all. The renderer draws this; deciding what it contains is this type's
job.





**Scrolling, not scrollback.** When the cursor passes the last row the grid shifts up
and the top row is discarded. History is a separate feature with its own memory budget, and
growing this buffer to provide it would put an unbounded allocation behind an innocuous property
— with the child process choosing how much.
simplify: viewport only; ceiling is one screen; upgrade trigger = scrollback becomes a
requirement, at which point it arrives as a bounded ring beside this, not inside it.





**Nothing here throws on bad input.** Every coordinate is clamped — including the
indexer, not only the mutators — because the values arrive in escape sequences written by an
untrusted process and an exception reachable by printing is a denial of service. A read is as
reachable from that output as a write (the renderer reads the cursor cell every frame), so it
honours the same clamp.





One screen belongs to one session's parser, which writes it on the pump thread while the
renderer reads it on the UI thread. They coordinate through `yncRoot`: a mutation
is made under the lock, and a frame is drawn under the lock, so neither observes the other
half-applied. See `yncRoot` for why the dirty flag alone is not enough.

| Member | Summary |
|---|---|
| `TerminalScreen(int columns, int rows)` | **(gap)** |
| `int Columns { get; private set; }` | **(gap)** |
| `int Rows { get; private set; }` | **(gap)** |
| `int CursorRow { get; private set; }` | **(gap)** |
| `int CursorColumn { get; private set; }` | **(gap)** |
| `TerminalPen Pen { get; set; } = TerminalPen.Default` | The style applied to subsequent writes and erases. |
| `bool IsDirty { get; private set; } = true` | Has anything changed since `learDirty`? |
| `TerminalCell this[int row, int column]` | **(gap)** |
| `object SyncRoot { get; } = new()` | The monitor that coordinates mutation and reads across threads. |
| `TerminalCell? CellUnderCursor()` | The cell under the cursor, or `null` when the cursor is not on a real cell. The cursor legitimately sits off the grid at the **pending-wrap** column (`CursorColumn == Columns`, held after writing the last column until… |
| `void ClearDirty()` | **(gap)** |
| `void Write(string text)` | Writes text at the cursor, wrapping and scrolling as needed. |
| `void Write(char character)` | Writes one character at the cursor. |
| `void CarriageReturn()` | **(gap)** |
| `void LineFeed()` | Moves down one row, scrolling when already at the bottom. The column is unchanged. |
| `void Backspace()` | Moves left one cell without erasing. |
| `void Tab()` | Moves to the next eight-column tab stop, stopping at the last column. |
| `void MoveCursor(int row, int column)` | Places the cursor, clamped into the screen. |
| `void EraseInLine(EraseExtent extent)` | **(gap)** |
| `void EraseInDisplay(EraseExtent extent)` | **(gap)** |
| `void EraseCharacters(int count)` | Erases  cells from the cursor **in place**, without shifting the rest of the line (ECH, `CSI n X`). |
| `void InsertCharacters(int count)` | Inserts  blank cells at the cursor, shifting the rest of the line right and dropping what falls off the end (ICH, `CSI n @`). |
| `void DeleteCharacters(int count)` | Deletes  cells at the cursor, shifting the rest of the line left and blanking the tail (DCH, `CSI n P`). |
| `void Resize(int columns, int rows)` | Resizes the grid, keeping the content that still fits. |

### `bool IsDirty { get; private set; } = true`

Has anything changed since `learDirty`?

**Remarks.** The renderer presents on a timer to coalesce a fast producer into frames. Without this flag
it would redraw a motionless screen at frame rate forever — the cost the coalescing policy
exists to avoid, paid continuously instead of never.

### `object SyncRoot { get; } = new()`

The monitor that coordinates mutation and reads across threads.

**Remarks.** The parser writes this screen on the session's pump thread while the renderer reads it on the
UI thread (the two differ by three orders of magnitude in rate, so marshalling every write to
the UI thread is not affordable — see the surface). "Joined only by the dirty flag" is not a
synchronization primitive: a `esize` swaps `_cells` and updates
`olumns` as two separate writes, and a reader that observes the new column count
against the old array indexes past its end. A writer holds this lock across a mutation; the
renderer holds it across a whole frame, so a frame never sees a half-applied change.

### `void LineFeed()`

Moves down one row, scrolling when already at the bottom. The column is unchanged.

**Remarks.** Column-preserving is correct even though it looks like an omission: a shell that wants column
zero sends CR with the LF, and a terminal that moved the column itself would break every
program that relies on the distinction.

### `void Backspace()`

Moves left one cell without erasing.

**Remarks.** Erasing here would delete twice: a shell removing a character sends BS, space, BS, and a
destructive backspace would consume the character before the space arrived to do it.

### `void EraseCharacters(int count)`

Erases  cells from the cursor **in place**, without shifting the
rest of the line (ECH, `CSI n X`).

**Remarks.** A TUI (Claude Code, less, a shell's line editor) clears a span this way when it repaints a
line. Dropping it — as an unhandled final was — leaves the old glyphs in the grid, and the
full-repaint renderer then faithfully draws them: the "characters painted without proper
refresh" report (smoke 9-1 #16). Erases to the current background, like every other erase.

### `void InsertCharacters(int count)`

Inserts  blank cells at the cursor, shifting the rest of the line right
and dropping what falls off the end (ICH, `CSI n @`).

**Remarks.** Typing into the middle of an existing line uses this; ignoring it overwrites instead of inserting.

### `void DeleteCharacters(int count)`

Deletes  cells at the cursor, shifting the rest of the line left and
blanking the tail (DCH, `CSI n P`).

**Remarks.** Deleting a character mid-line uses this; ignoring it leaves the deleted glyph on screen.

### `void Resize(int columns, int rows)`

Resizes the grid, keeping the content that still fits.

**Remarks.** Content is preserved by position rather than reflowed. Reflowing is what a user expects when
narrowing a window over wrapped prose, and it needs a record of which line breaks were
*wrapped* versus *written* — which this model does not keep, and inventing it here would be
guessing at where text belongs.
simplify: truncate rather than reflow; ceiling is a resize losing off-screen content;
upgrade trigger = the model gains wrapped-line provenance.

## `VtParser`

*class* — `VtParser.cs`

Turns a session's output bytes into screen state: the display half of reading a terminal stream.

**Remarks.** **Separate from `scParser`, and the two are not redundant.** That one reads
the stream for *authenticated state claims* and is a security control whose value depends on
staying small enough to reason about. This one reads the same bytes for *what to draw*. Two
passes cost nothing worth counting — S3 measured a scanner at 2361× the architecture's 1 MiB/s
budget — and merging them would put display concerns inside the control. What this parser owes
OSC is to **skip** it, so a window-title or clipboard sequence never lands on screen as text
the user reads as program output.





**Every byte is hostile input** (D2). The child chooses all of it, so truncated,
over-long and nonsensical sequences are the normal case. Nothing here throws and nothing here
grows without a bound: an exception or an allocation reachable by printing is a denial of service
written in escape codes.





**Decoding is incremental.** UTF-8 characters and escape sequences both get split by
read boundaries, which are chosen by the pipe rather than the child. A parser that decoded each
chunk independently passes every test written as one string and produces replacement characters
against a real 4 KiB read.





Not thread-safe: one parser belongs to one session's reader.

| Member | Summary |
|---|---|
| `VtParser(TerminalScreen screen)` | **(gap)** |
| `void Consume(ReadOnlySpan<byte> bytes)` | Feeds one chunk of session output through the parser. |
