---
id: api-aide-core-agentplane
title: "API: AiDe.Core.AgentPlane"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.AgentPlane: 57 types, 142 members, 90% carrying a summary doc comment.
---

# API: `AiDe.Core.AgentPlane`

**57 public types · 142 public members · 90% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `AcpEngineProcess`

*class* — `AcpEngineProcess.cs`

The engine child process a governed lane speaks ACP to — started, inspected, and **reaped**.

**Remarks.** **Separate from `AcpPeer` because a process is not a protocol.** The peer
works on any pair of streams, which is what lets the whole framing and correlation contract be
tested without spawning anything. This owns the part that can leak: a live subprocess.





**The environment is inspected before the lane runs**, using DC-027's existing control
rather than a second copy of its rules. That measurement found this machine's PATH at 22,297
characters — past the size `cmd.exe` silently drops — so a child launched through any
`.cmd` shim starts with an empty PATH and cannot find node, git or itself, while everything
on the surface looks healthy. Findings are reported, never repaired: PATH belongs to the person
whose machine it is, and a tool that quietly rewrites it has hidden the problem from the only
person who can fix it.





**Disposal kills the whole tree.** The adapter is `node` running a package that
itself spawns the `claude` CLI, so killing the parent alone leaves the grandchild holding a
credential session open with nothing attached to its stdio.





**And a kill-on-close job is the backstop, because disposal is the GRACEFUL path.**
`using var engine = ...` runs `Dispose`; a host that is killed runs nothing.
This class reasoned carefully about the graceful path - the wait below, and why a kill that is
not waited on is a claim rather than an observation - and left the abnormal one open, which is
DC-123 exactly: an exemplary lifetime comment beside a leak of the same resource class.
MEASURED before the fix, with the host killed rather than closed: the engine and its own child
were both still running five seconds later. The job closes when the host's handle table goes,
however the host dies, and Windows takes the tree with it. Off Windows there is no job and
disposal is the only reaping there is.

| Member | Summary |
|---|---|
| `int ProcessId { get; }` | The child's process id, for a report that has to name what was left behind. |
| `bool HasExited` | Whether the child has ended. |
| `TextReader Output` | The child's stdout — the peer's input. |
| `TextWriter Input` | The child's stdin — the peer's output, and the protocol channel. |
| `IReadOnlyList<string> EnvironmentFindings { get; }` | What the environment inspection found. Empty means healthy, not unmeasured. |
| `AcpEngineProcess Start(` | Starts the engine. |
| `Task WaitForExitAsync(CancellationToken cancellationToken = default)` | Waits for the child to end on its own. |
| `void Dispose()` | Ends the lane's engine, whole tree, and waits until it is actually gone. |

### `int ProcessId { get; }`

The child's process id, for a report that has to name what was left behind.

**Remarks.** Captured at start, so it survives disposal — see the constructor.

### `bool HasExited`

Whether the child has ended.

**Remarks.** Answerable after disposal too. `HasExited` throws once the object is
disposed, which would make "did the lane leave a process behind?" unanswerable at exactly the
moment it is asked.

### `AcpEngineProcess Start(`

Starts the engine.

- **`launch`** — What to run, from `ResolveLaunch`.
- **`workingDirectory`** — Where to run it. Not the ACP session cwd, which is separate.
- **`diagnostics`** — Where the child's stderr and any environment finding go. Defaults to stderr.
- **`inspectEnvironment`** — The environment probe. Defaults to `Inspect` — injectable only so the surfacing can be tested without a broken machine.

**Throws `AgentPlaneException`.** `EngineDidNotStart` when the executable is not there, the operating system refuses, or the child started but could not be put in its job. A named refusal rather than a raw Win32 exception, because "node is not on the PATH" is a fact an operator can act on.

### `void Dispose()`

Ends the lane's engine, whole tree, and waits until it is actually gone.

**Remarks.** The wait is the part that matters: a kill request that is not waited on turns "the
child is dead" into a claim rather than an observation, and the caller has no way to tell
the difference.





**This is the graceful path, and it is no longer the only one.** A host that is
killed never reaches this method; the job handle goes with the host's handle table and
Windows reaps the tree. Closing that handle here is therefore both the release of a handle
and the last line of the same reaping, which is why it runs even when the kill above
threw.

## `AcpAuthStatus`

*class* — `AcpLaneClient.cs`

Reads the adapter's `_auth/status_update` extension frame.

**Remarks.** **An `_`-prefixed extension, so absence is ordinary.** The frame may simply not arrive,
and `null` is what that means. `SpawnContract` refuses on it rather than
assuming a subscription — an environment API key outranks the stored subscription inside the
adapter and bills silently, so "not recorded" is the one answer that must never be optimistic.

| Member | Summary |
|---|---|
| `string Method = "_auth/status_update"` | The wire method, as observed. Vendor-prefixed, and therefore not in schema v1. |
| `ObservedAuthStatus? From(JsonObject? parameters)` | Reads one status update's params, or `null` when it carries no status. |

## `AcpClientCapabilities`

*record* — `AcpLaneClient.cs`

What this client tells the agent it can do for it.

**Remarks.** **Declared honestly, which is the whole point.** A client that advertises
`fs.writeTextFile` and then answers `-32601` has told the agent a lie it will plan
around. Phase 1 implements none of the client-side capabilities, and the adapter's own tools
cover the same ground — the captured corpus shows a file write arriving as a `tool_call`
with `kind:"edit"` plus a permission request, not as `fs/write_text_file`.

| Member | Summary |
|---|---|
| `AcpClientCapabilities PhaseOne = new(false, false, false)` | What Phase 1 actually implements: none of them. |
| `JsonObject ToJson()` | The `clientCapabilities` object, in the shape the adapter reads. |

## `LaneSessionOptions`

*record* — `AcpLaneClient.cs`

The tools argument of `session/new` — what the lane's model may hold — sent through the
adapter's extension slot `_meta.claudeCode.options`.

**Remarks.** **A lane is not toolless by default.** With no `_meta` the adapter hands the SDK
the `claude_code` preset and resolves permissions from the user's, the repository's and the
local settings (adapter 0.75.1, `acp-agent.js:5883-5884`, `:5962`), so the lease
bounds the file writes the seams observe and nothing bounds the tools. This record is the one
place the host says otherwise (Ruling 71; `docs/notes/lane-pin-spike.md`).





**Exactly the two members the adapter spreads into the SDK options**
(`:6007-6008`), and nothing else — the whole `_meta.claudeCode.options` object is
spread into the SDK's options (`:5964`), so a wider record would be a wider reach. An
absent record and an empty one both send the frame every prior run sent.

## `AcpLaneClient`

*class* — `AcpLaneClient.cs`

ACP semantics over the peer: the handshake, the session, the prompt, and the permission answer.

**Remarks.** **Separate from `AcpPeer` on purpose.** The peer owns framing, correlation
and failure; this owns what the messages mean. The split is what lets every framing failure mode
be tested without a handshake, and every handshake rule be tested without a process.





**The permission answer defaults to reject.** That is the spike probe's behaviour and it
is the right default for an unattended lane: a client that silently allows every edit has removed
the governance the plane exists to provide. The choice is a delegate because it is a policy the
caller owns, not a constant.

| Member | Summary |
|---|---|
| `int ProtocolVersion = 1` | The ACP schema revision this client speaks, pinned and **asserted on the way back**. |
| `AcpLaneClient(` | **(gap)** |
| `Task<JsonObject> InitializeAsync(CancellationToken cancellationToken = default)` | Performs the handshake and returns its result, having checked the echoed version. |
| `JsonObject? SessionNewParameters { get; private set; }` | The params of the `session/new` this client sent — the object the peer serialized onto the wire, `_meta` included — or `null` until it has sent one. |
| `Task<string> NewSessionAsync(` | Opens a session rooted at , which **must be absolute**, holding the tools  names — or, with none, whatever the adapter's preset allows. |
| `Task<string> NewSessionAsync(` | Opens the lane's session rooted in its **provisioned worktree** — spec R1 bullet 1. |
| `Task<JsonObject> PromptAsync(string sessionId, string text, CancellationToken cancellationToken = default)` | Sends one prompt and waits for the turn to end, under the prompt bound rather than the handshake one. |

### `int ProtocolVersion = 1`

The ACP schema revision this client speaks, pinned and **asserted on the way back**.

**Remarks.** Schema v2 alpha is in flight and the adapters publish near-daily. Sending a version and not
checking the echo is how a client goes on speaking v1 to a peer that answered v2: every frame
still parses, and the meanings have moved underneath it.

### `AcpLaneClient(`

- **`peer`** — The transport. This client installs itself as its inbound handler.
- **`capabilities`** — What to declare. Defaults to `PhaseOne`.
- **`choosePermission`** — Picks an `optionId` from a permission request's params. Defaults to the reject option.

### `Task<JsonObject> InitializeAsync(CancellationToken cancellationToken = default)`

Performs the handshake and returns its result, having checked the echoed version.

**Throws `AgentPlaneException`.** `ProtocolVersionMismatch` when the peer echoed a different version, or none at all.

### `JsonObject? SessionNewParameters { get; private set; }`

The params of the `session/new` this client sent — the object the peer serialized onto
the wire, `_meta` included — or `null` until it has sent one.

**Remarks.** The outbound mirror of `ObservedAuth`: kept so the host can record the
frame it opened the lane with (Ruling 71 (a)) as what was sent, never as a re-computation of
what should have been.

### `Task<string> NewSessionAsync(`

Opens a session rooted at , which **must be absolute**, holding the
tools  names — or, with none, whatever the adapter's preset allows.

**Throws `AgentPlaneException`.** `SessionCwdNotAbsolute` — refused before the wire, so the reason stays attached to the caller rather than arriving later as a `-32602`.

### `Task<string> NewSessionAsync(`

Opens the lane's session rooted in its **provisioned worktree** — spec R1 bullet 1.

**Remarks.** **The composition lives in the signature.** Each half was already true and neither implied
the other: this client refused a relative `cwd` (true of any absolute path, including the
primary checkout), and the provisioner really cut a tree (true whether or not the lane ever
ran there). A caller holding a `ProvisionedWorktree` passes the tree, not a
string, so a governed lane cannot be rooted anywhere else by picking the wrong path.

## `AcpPeerOptions`

*record* — `AcpPeer.cs`

The bounds an ACP peer runs under. Every one of them is a number a reader can find.

**Remarks.** **Stated, not buried.** A cap or a timeout written inline at its call site is a decision that
only the person who wrote it knows was made. These four are the whole of the peer's resource
story: how much it will buffer for one frame, how many events it will hold before it pushes back,
and how long it will wait for each of the two very different kinds of answer.

| Member | Summary |
|---|---|
| `int DefaultEventQueueCapacity = 1024` | The default event-queue depth. |
| `int DefaultMaxFrameChars = 4 * 1024 * 1024` | The largest single frame the splitter will assemble, in characters. |
| `TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(60)` | How long an ordinary request may go unanswered before the peer calls the child hung. |
| `TimeSpan DefaultPromptTimeout = TimeSpan.FromMinutes(15)` | How long a `session/prompt` may run. |
| `int EventQueueCapacity { get; init; } = DefaultEventQueueCapacity` | How many run events may be queued before the reader is pushed back. |
| `int MaxFrameChars { get; init; } = DefaultMaxFrameChars` | The largest single frame the splitter will assemble. |
| `TimeSpan RequestTimeout { get; init; } = DefaultRequestTimeout` | The default per-request answer bound. |
| `TimeSpan PromptTimeout { get; init; } = DefaultPromptTimeout` | The bound a whole agent turn runs under. |

### `int DefaultEventQueueCapacity = 1024`

The default event-queue depth.

**Remarks.** Matches `IngestHost`'s default for the same reason: it is deep enough that an ordinary
burst never reaches the reader, and shallow enough that a consumer which has genuinely
stopped is felt rather than absorbed into memory.

### `int DefaultMaxFrameChars = 4 * 1024 * 1024`

The largest single frame the splitter will assemble, in characters.

**Remarks.** **Measured against the corpus, not guessed.** The largest frame the pinned adapter
actually wrote across all 88 captured lines is 24,692 characters
(`frames/read.jsonl`), so this is roughly 170x the observed maximum — large enough that
no honest frame reaches it, small enough that a peer emitting an unterminated stream cannot
exhaust memory before anything notices.

### `TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(60)`

How long an ordinary request may go unanswered before the peer calls the child hung.

**Remarks.** `initialize` and `session/new` are handshake calls: the adapter answers them in
milliseconds or it is not going to. Sixty seconds is generous enough to absorb a cold Node
start and a credential-store read, and short enough that a wedged adapter surfaces as a
refusal rather than as a lane that is "still working".

### `TimeSpan DefaultPromptTimeout = TimeSpan.FromMinutes(15)`

How long a `session/prompt` may run.

**Remarks.** A prompt is a whole agent turn — it may compile, run a suite, and edit files — so the
handshake bound is wrong for it by orders of magnitude. Fifteen minutes is a bound, not an
expectation: its purpose is that a lane whose engine has died quietly still ends.

## `ObservedRunEvent`

*record* — `AcpPeer.cs`

One normalized run event, with the measurement of how long normalizing it took.

**Remarks.** **The latency is nullable because the measurement can be absent.** A measurement path
degrades to "not recorded", never to a plausible wrong number (IO12): a zero here would read as
"instantaneous" and would be indistinguishable from "nobody stamped the receipt".





**It is carried on the event rather than accumulated in the peer.** A peer-held list of
every latency is an unbounded allocation for the life of a lane; travelling with the event lets
the consumer keep exactly what it needs — which for the SLO evaluation is a running p50/p95, and
for a surface is nothing at all.

| Member | Summary |
|---|---|
| `string NotRecorded = "not recorded"` | What an absent measurement reads as. Never `"0 ms"`. |
| `ObservedRunEvent From(RunEvent evt, DateTimeOffset? receivedAt, DateTimeOffset normalizedAt)` | Builds the observation, computing the latency only when there is a receipt to compute it from. |
| `string DescribeLatency()` | The latency in words a person or a log line can read. |

## `AcpEventQueue`

*class* — `AcpPeer.cs`

The bounded queue every normalized run event passes through: overflow applies
**backpressure**, and any drop is counted and surfaced.

**Remarks.** **The counting idiom is `IngestHost`'s** (`IngestHost.cs`, the
`itemDropped` callback): a drop is a coverage-gap signal, so it is a number somebody can
read rather than an absence nobody can.





**The overflow policy is deliberately NOT `ConPtyTerminalSession`'s.** That session
uses `DropOldest`, which is right there — `ITerminalSession` states terminal bytes are
ephemeral by contract and must never reach the fact store. ACP events are the opposite: spec
§7.1 calls the run log "the truth", so a silent drop is data loss in the durable record. This
queue blocks the reader instead, which backs the pressure up the pipe to the engine — the engine
stalls on a full stdout buffer, which is exactly the right thing for it to do.





**A drop is still modelled**, because "cannot drop" is not a property a bounded queue
can have: the lane stops and whatever was waiting is lost. What is required is that it is
counted and named when it happens.

| Member | Summary |
|---|---|
| `AcpEventQueue(int capacity, Action<string>? diagnostics = null)` | **(gap)** |
| `int Capacity { get; }` | The configured depth. |
| `long Published` | How many events reached the queue. |
| `long BackpressureWaits` | How many publishes had to wait for room. Every one of them is a visible slow-consumer signal. |
| `long Dropped` | How many events were lost. Any value above zero is a hole in what §7.1 calls the truth. |
| `ChannelReader<ObservedRunEvent> Reader` | The consumer side. |
| `ValueTask PublishAsync(ObservedRunEvent observed, CancellationToken cancellationToken = default)` | Publishes one event, waiting for room rather than discarding anything. |
| `void Complete()` | No more events will be published. A consumer waiting on the reader stops. |

### `AcpEventQueue(int capacity, Action<string>? diagnostics = null)`

- **`capacity`** — How many events may be held before a publish waits.
- **`diagnostics`** — Where a wait or a drop is reported. Defaults to stderr.

### `ValueTask PublishAsync(ObservedRunEvent observed, CancellationToken cancellationToken = default)`

Publishes one event, waiting for room rather than discarding anything.

**Throws `OperationCanceledException`.** The lane is stopping while this event was waiting for room. The loss is counted and named first, so the cancellation never hides it.

## `AcpPeerCounters`

*record* — `AcpPeer.cs`

A snapshot of what the peer has seen. Every field is a fact somebody can act on.

## `AcpPeer`

*class* — `AcpPeer.cs`

A bidirectional ACP peer: newline-delimited JSON-RPC over a child process's stdio, where the
agent calls **us** as often as we call it.

**Remarks.** **A server loop, not a request/response client.** The spike established that the
adapter initiates `session/request_permission`, `fs/read_text_file`,
`fs/write_text_file`, `terminal/create|output|wait_for_exit|kill|release` and
`elicitation/create`. A client that only sends and waits deadlocks the first time the agent
asks it something — which is on the first edit.





**The correlation is the part that had to be designed.** `probe-write.js`
dispatched on `m.id === 1 | 2 | 3`: three literals for three known requests. Two rules
replace it, and both are about presence rather than value —



a frame carrying `method` is **inbound**, whatever its id; everything else is a
reply to something this peer sent;
an inbound frame is a **request** when the `id` *key is present and not JSON
null* — never when the id is "truthy". `frames/write.jsonl:12` is a real
`session/request_permission` with `"id":0`, and a truthiness test answers it never.




Replies are matched through a pending table keyed on the id's canonical text, so ids are
correlated by identity rather than by arrival order — the corpus already answers out of order
relative to nothing, and a real session interleaves.





**Normalization happens before dispatch, and that ordering is load-bearing.** Each frame
becomes a `RunEvent` and is published *before* the peer answers anything. That
is Ruling 11's required ordinal: the run event for a tool call is observable before the response
to the next inbound request for the same call id, on any hardware, with no stopwatch.





**Inherited from `src/AiDe.Mcp/Program.cs`:** stdout is the protocol and every
diagnostic goes to stderr; one bad message never kills the loop; an unknown method is
*answered*, not ignored, because silence looks like a hung peer.

| Member | Summary |
|---|---|
| `AcpPeer(` | **(gap)** |
| `AcpEventQueue Events { get; }` | Every frame this peer read, normalized, in receipt order. |
| `Func<string, JsonObject?, JsonNode?>? InboundHandler { get; set; }` | The handler for inbound requests. Returning `null` means "not handled", which is answered `-32601` rather than ignored. |
| `ObservedAuthStatus? ObservedAuth { get; private set; }` | What the adapter last said about how it is authenticated, or `null` when it has not said. |
| `AcpPeerCounters Counters` | Everything the peer has counted. |
| `Task RunAsync(CancellationToken cancellationToken = default)` | Reads the child's stdout until end of stream, normalizing and dispatching every frame. |
| `Task<JsonObject> RequestAsync(` | Sends a request and waits for the reply that carries its id. |

### `AcpPeer(`

- **`input`** — The child's stdout.
- **`output`** — The child's stdin. This is the protocol channel; nothing else may reach it.
- **`mapper`** — The one ACP-to-envelope mapper. Frames are normalized before they are acted on.
- **`time`** — Stamps receipt and publication. Defaults to the system clock.
- **`options`** — The peer's bounds. Defaults to `AcpPeerOptions`'s stated values.
- **`diagnostics`** — Where everything that is not protocol goes. Defaults to stderr.

### `ObservedAuthStatus? ObservedAuth { get; private set; }`

What the adapter last said about how it is authenticated, or `null` when it has not said.

**Remarks.** Observed off the wire as `_auth/status_update` goes past, because a measurement of where
requests will bill is the only version-robust evidence there is — an environment API key
outranks the stored subscription inside the adapter and bills silently.

### `Task RunAsync(CancellationToken cancellationToken = default)`

Reads the child's stdout until end of stream, normalizing and dispatching every frame.

**Remarks.** **The splitter is hand-rolled rather than `ReadLineAsync`** for exactly one
reason: `ReadLineAsync` will buffer a line of any length, so a child that never emits a
newline is an unbounded allocation with no error. This one enforces
`MaxFrameChars`, resynchronizes at the next newline after refusing
an over-long line, and can report the tail a truncated stream left behind.





**Whatever happens, the lane ends cleanly:** pending requests are failed with a named
reason instead of waiting on a process that has gone, and the event queue is completed so a
consumer stops rather than hangs.

### `Task<JsonObject> RequestAsync(`

Sends a request and waits for the reply that carries its id.

- **`method`** — The ACP method.
- **`parameters`** — Its params, or null.
- **`timeout`** — How long to wait. Defaults to `RequestTimeout`.
- **`cancellationToken`** — Cancels the wait.

**Throws `AgentPlaneException`.** `EngineReturnedError` when the peer answered with an error; `EngineRequestTimedOut` when it did not answer at all; `EngineStreamEnded` when its stdout closed first.

## `AcpRunEventMapper`

*class* — `AcpRunEventMapper.cs`

The **one** mapper from ACP wire frames to `RunEvent`. Pattern: Anti-Corruption
Layer — the single seam that keeps adapter vocabulary out of the plane.

**Remarks.** **One source, one mapper.** Spec §7.2 describes one envelope over four sources; Phase 1
has ACP and nothing else. A second mapper appearing before a second source does is the defined
failure of this node — the envelope would then be a shape two writers agree on by luck rather
than a contract one writer owns.





**Recognition is a table, not a handler per kind.** Four wire shapes have a Phase-1
producer and are projected onto v1 kinds; everything else is namespaced `acp.*` and carried
whole under `Ext`. Adding a kind is adding a row, and an adapter release that invents one
needs no change at all — §7.2's "consumers ignore unknown kinds", implemented rather than
restated.





**Nothing is dropped, ever.** A recognized frame's payload moves to `body` and the
remaining envelope stays in `Ext`; an unrecognized frame goes to `Ext` entire. Either
way every field of the original frame is present exactly once, which is what the captured-corpus
round-trip proves over all 88 frames.





**Identity, ordering and time come from the plane.** ACP frames carry no run id, no
sequence and no timestamp. `Seq` is assigned here and `Ts` is stamped at receipt —
the same discipline as `OtelSpanMapper`'s `recordedAt`, for the same reason: a value
taken from the wire would be a claim, not a measurement.

| Member | Summary |
|---|---|
| `AcpRunEventMapper(string runId, string agentId)` | **(gap)** |
| `string RunId { get; }` | The run every mapped event is stamped with. |
| `string AgentId { get; }` | The agent every mapped event is stamped with. |
| `RunEvent Map(string frameLine, DateTimeOffset receivedAt)` | Maps one newline-delimited ACP frame.  is stamped by the reader, never read from the frame. |

### `AcpRunEventMapper(string runId, string agentId)`

- **`runId`** — The governed run every event from this lane belongs to.
- **`agentId`** — The lane's agent identity.

### `RunEvent Map(string frameLine, DateTimeOffset receivedAt)`

Maps one newline-delimited ACP frame.  is stamped by the reader,
never read from the frame.

**Throws `AgentPlaneException`.** `MalformedFrame` when the line is not a JSON object, so it carries no event at all. Refused rather than swallowed: a line the reader cannot understand is a contract break, and silently returning nothing would make it invisible.

## `AcpMode`

*enum* — `EngineCatalog.cs`

How an engine speaks ACP, per spec §14.2's provider schema — the file this repository reads is
`~/.aide/providers.json` (erratum: `docs/notes/conductor-spec-errata-providers-json.md`),
and its `acp:` key is accepted and never read, because this enum is the catalog's fact.
A closed set on purpose — the spec declares exactly these four, and unlike a run-event
`kind` they do not evolve additively: a fifth would be a new launch path, which is a code
change by definition.

## `EngineRow`

*record* — `EngineCatalog.cs`

One catalog row. **Engines are data, not code paths** (spec §4.1): a row declares how to
speak to an engine, and adding or repairing one is a data change plus at most an adapter shim.

## `EngineLaunch`

*record* — `EngineCatalog.cs`

A resolved process launch: what to run, and with what arguments.

## `EngineCatalog`

*class* — `EngineCatalog.cs`

The engine catalog — three data rows, one launch path.

**Remarks.** **The pins are catalog facts, not spec text.** The spec names no adapter package at all
(verified: zero hits for `zed-industries`, `@zed`, `claude-code-acp`), so package
identity is pinned in `docs/notes/conductor-spec-errata-policy.md` — which is exactly where
§4.1 says such things live. The superseded `@zed-industries/claude-code-acp` last published
at v0.16.2, so reaching for that name from memory silently yields a March build.





**Every refusal names its reason.** An unknown engine, a non-adapter mode, and an
adapter with no observed entry module each fail loudly and differently. A silent default here
would launch the wrong engine, or the right one the wrong way, and look like success.

| Member | Summary |
|---|---|
| `IReadOnlyList<EngineRow> Rows` | The catalog, as data. |
| `EngineRow Find(string engineId)` | Finds a row by id. |
| `EngineLaunch ResolveLaunch(string engineId, string adapterInstallRoot)` | Resolves how to launch an engine. Exactly one path is implemented: an adapter package whose entry module has been observed on a real install. |

### `EngineRow Find(string engineId)`

Finds a row by id.

**Throws `AgentPlaneException`.** `UnknownEngine` — an id the catalog does not carry is refused, never defaulted to the one engine that happens to work today.

### `EngineLaunch ResolveLaunch(string engineId, string adapterInstallRoot)`

Resolves how to launch an engine. Exactly one path is implemented: an adapter package whose
entry module has been observed on a real install.

- **`engineId`** — The catalog id.
- **`adapterInstallRoot`** — The directory whose `node_modules` holds the adapter.

**Throws `AgentPlaneException`.** `UnknownEngine` for an id the catalog does not carry; `LaunchPathNotImplemented` for any mode but `Adapter`; `AdapterEntryModuleNotRecorded` for an adapter whose entry module has never been observed.

## `RunBudget`

*record* — `GoalBlock.cs`

The per-run budget a goal block declares (§14.3 `budget: { requests, tokens }`).

**Remarks.** A budget with no convergence condition is a timer, so the numbers live beside the done-condition
rather than alone. Both must be positive: a zero budget is not a small budget, it is a spawn that
can never do anything, which is a typo rather than an intent.

| Member | Summary |
|---|---|
| `RunBudget SubscriptionBounded = new(Requests: int.MaxValue, Tokens: long.MaxValue)` | The declared value for "no cap chosen — bounded by the subscription instead" (Ruling 72; ADR-0033 §3), never a required number. |
| `bool IsSubscriptionBounded` | Whether this is the declared `SubscriptionBounded` value, by field equality. |

### `RunBudget SubscriptionBounded = new(Requests: int.MaxValue, Tokens: long.MaxValue)`

The declared value for "no cap chosen — bounded by the subscription instead" (Ruling 72;
ADR-0033 §3), never a required number.

**Remarks.** **A maximal, positive value rather than a nullable `RunBudget` or a second
contract shape.** `Validate`'s six fields are tier-blind and
budget is one of them; making it optional there would be a contract change (ADR-0033's
"Alternatives considered"). A declared maximal value keeps `Validate`
byte-identical and turns "no cap" into a value `IsSubscriptionBounded` can read,
rather than an absence a caller must guess at.





**A one-way door.** This value crosses JSON into persisted run and result files
(`ConductorEntry.cs`); every file ever written with `2147483647` /
`9223372036854775807` must read as subscription-bounded forever, so
`IsSubscriptionBounded` is a reader that can never be removed.

### `bool IsSubscriptionBounded`

Whether this is the declared `SubscriptionBounded` value, by field equality.

**Remarks.** **Value equality, not reference equality.** `RunBudget` crosses JSON (a request
or result file deserializes a brand-new instance), so a caller checking
`ReferenceEquals(budget, SubscriptionBounded)` would silently stop working the moment the
value was read back from disk. Records already compare by value, so equality against the
constant is exactly this predicate.

## `GoalBlockFields`

*class* — `GoalBlock.cs`

The six fields spec §14.3 names, by their wire names. The error a caller sees uses these, so the
message and the schema are the same vocabulary.

| Member | Summary |
|---|---|
| `string GoalKey = "goal"` | **(gap)** |
| `string DoneWhenKey = "done_when"` | **(gap)** |
| `string NotInScopeKey = "not_in_scope"` | **(gap)** |
| `string TierKey = "tier"` | **(gap)** |
| `string FanOutCapKey = "fan_out_cap"` | **(gap)** |
| `string BudgetKey = "budget"` | **(gap)** |
| `IReadOnlyList<string> All = [GoalKey, DoneWhenKey, NotInScopeKey, TierKey, FanOutCapKey, BudgetKey]` | All six, in the order §14.3 lists them. |

## `GoalBlock`

*record* — `GoalBlock.cs`

CT19's goal state as a spawn precondition — spec R2 and §14.3.

**Remarks.** **Every field is nullable, deliberately.** A required constructor parameter would move
the check to the compiler for a value that arrives from a tool call at runtime, and the caller
would then be forced to pass *something* — which is how a placeholder goal gets written. The
type carries what was declared; `Validate` decides whether that is a
goal block. It also makes "omitted" expressible, which is what a field-level error needs.





**`FanOutCap` is `int?` rather than `int` for one specific reason:**
zero is the ordinary value — it is what "do not fan out" says — so presence cannot be tested by
truthiness. Defaulting it to zero would silently supply the most common answer and make the field
unforgettable in exactly the wrong way.

## `GoalBlockError`

*record* — `GoalBlock.cs`

One field-level goal-block error. The field is a member, not a substring of prose.

## `ObservedAuthStatus`

*record* — `GoalBlock.cs`

What the ACP adapter reported about how it is authenticated — the `_auth/status_update`
extension frame, observed live in the spike.

**Remarks.** **A measurement, not a configuration reading.** The spike found that
`ANTHROPIC_API_KEY`, an `apiKeyHelper` or a managed key *outrank* the stored
subscription inside the adapter, so a lane can believe it is on a subscription and be billed to
the API — with no direct-API spawn to reject. What the adapter says about itself is the only
version-robust evidence of where the requests will actually bill.





**Absence is representable only as `null` at the call site.** The frame is an
`_`-prefixed extension and may not arrive at all; there is no "unknown" member here, because
the caller's `null` already says it and `SpawnContract` refuses on it.

| Member | Summary |
|---|---|
| `string AccountKind = "account"` | The observed `kind` that means a subscription account, as captured in the spike. |
| `bool IsSubscription` | Whether the adapter says it is on a subscription account. |

## `SpawnRequest`

*record* — `GoalBlock.cs`

Everything a spawn attempt states about itself.

## `Spawn`

*record* — `GoalBlock.cs`

An authorized spawn: a complete goal block, a resolved binding, an observed subscription.

## `SpawnContract`

*class* — `GoalBlock.cs`

The spawn precondition — spec R2 ("no block, no spawn"), §4.2's terms-of-service prohibition, and
the observed-auth gate.

| Member | Summary |
|---|---|
| `IReadOnlyList<GoalBlockError> Validate(GoalBlock? block)` | Every reason this goal block is not one, each naming its own field. Empty means valid. |
| `Spawn Authorize(SpawnRequest request, ProviderRegistry registry)` | Decides whether this spawn may proceed, and returns what it is bound to. |

### `IReadOnlyList<GoalBlockError> Validate(GoalBlock? block)`

Every reason this goal block is not one, each naming its own field. Empty means valid.

**Remarks.** **All errors at once, not the first.** One-at-a-time validation turns a six-field
omission into six round trips, and a conductor retrying a tool call six times looks like a
loop rather than a caller who forgot the schema.





**A blank string is an unwritten field.** A goal of `"   "` and a goal of
`null` want the same answer — the episode would be scored against nothing either way —
and this matches how the coordination contract already reads an attribute. `not_in_scope`
is included in that rule on purpose: CT19 requires the boundary to be *written*, and a
lane with genuinely nothing out of scope can write so.

### `Spawn Authorize(SpawnRequest request, ProviderRegistry registry)`

Decides whether this spawn may proceed, and returns what it is bound to.

**Throws `AgentPlaneException`.** `DirectApiRefusedByToS`, `GoalBlockIncomplete`, `ObservedAuthNotRecorded`, `ObservedAuthNotSubscription`, or any refusal `Bind` raises.

**Remarks.** **The order is the substance.** The terms-of-service prohibition is checked
*first*, before the goal block and before any binding, so it cannot be masked by another
error. If validation ran first, an operator would fix six fields and only then learn the
spawn was never permitted — and a reader would have to prove the prohibition unreachable-by-
accident rather than read it.





**The observed-auth gate fails closed.** A subscription-configured account with no
observed status is refused, never assumed.





**And the observed account is checked, when there is something to check it against.**
The observed label is a display string the adapter chooses ("Claude Max"); the configured
label is an operator's own name for a login ("max-personal"). Neither determines the other, so
the correspondence is *declared* — `ObservedAuthLabel` — and
enforced only where it was declared. Where it was not, the check is the auth *kind*
alone, which is "not recorded" rather than a guessed mapping (IO12). What the declared form
buys is real: an operator with two subscription logins who switches the local CLI between them
gets a refusal instead of a lane that bills and ranks against the wrong account.

## `LaneIdentity`

*record* — `GovernedLaneSource.cs`

Who a governed lane is, in the terms the watcher's registration already speaks.

**Remarks.** **`LaneId` is registered as the terminal id.** A governed lane has no ConPTY, but
the terminal id is the watcher's key for "the host surface this session lives in", and for a
governed lane the lane *is* that surface. It also buys the behaviour a lane wants: the
ingest host adopts an existing session for a known terminal id and bumps its generation, so a
respawned lane continues its own history instead of minting a second session.

## `GovernedLaneSource`

*class* — `GovernedLaneSource.cs`

Opens governed episodes on the live ingest path — spec §6.2's `GovernedSessionSource`,
renamed `GovernedLaneSource` by Ruling 15 (A3): see `note-conductor-spec-errata-lane-rename`.

**Remarks.** **No new seam.** Registration, episode open, artifact declaration and close all go
through `IngestHost` and are capability-verified by `ITrustedRegistrar`, exactly
as `InjectedContractIngest` does for a session that declares its own episodes. An
episode-source interface was declined by ruling until a third implementer exists — the name is
left unwritten deliberately, because a doc comment that cites a type nobody declared reads as a
guarantee (DC-095); two implementations are not evidence of a shape, and the interface would
have to be guessed from one of them.





**The difference from the observed door is authorship, not mechanism** (§6.1). An
observed session *declares* its episode over the coordination log and could in principle
declare anything; a governed lane's episode is opened here, by the runtime, from the goal block
that was a precondition of the spawn. The capability never leaves this object, so the lane cannot
forge an open, a close or an outcome.

| Member | Summary |
|---|---|
| `GovernedLaneSource(IngestHost host)` | **(gap)** |
| `GovernedEpisode Open(LaneIdentity identity, GoalBlock block)` | Registers the lane's session and opens its episode from the goal block. |

### `GovernedEpisode Open(LaneIdentity identity, GoalBlock block)`

Registers the lane's session and opens its episode from the goal block.

**Throws `AgentPlaneException`.** `GoalBlockIncomplete`, naming every missing field.

**Remarks.** **Validated before anything is created.** R2 is "no block, no spawn": an incomplete
block leaves no session and no episode behind, so a rejected spawn is not visible in the store
as a lane that never did anything.





**The attributes are the block's strings, untouched.** Nothing is trimmed,
normalized or re-encoded on the way through — the episode is what the agent is later scored
against, and a helpful transformation here would score it against a goal nobody wrote.

## `GovernedEpisode`

*class* — `GovernedLaneSource.cs`

One open governed episode, and the only object that can close it.

**Remarks.** Renamed from `GovernedSession` by Ruling 15 (A3) / Ruling 15a — the target
`GovernedLane` is not available (see `GovernedLane` below, an unrelated
pre-existing composite of the same name); see `note-addendum-a-ruling-15a-governed-episode`.

| Member | Summary |
|---|---|
| `string SessionId` | The watcher session this lane registered as. |
| `string EpisodeId { get; }` | The episode the goal block opened. |
| `IReadOnlyDictionary<string, string?> OpenAttributes { get; }` | The attributes the episode opened with, exactly as sent. |
| `int DeclareArtifacts(IReadOnlyList<string> paths)` | Records the evidence paths this lane names. Declared, never verified here. |
| `WorkEpisode Close(EpisodeOutcome outcome)` | Closes the episode with its outcome. The declaration is not a quality judgement. |

### `string SessionId`

The watcher session this lane registered as.

**Remarks.** **Boundary note (Ruling 15 / A3).** This "session" is `IngestHost`'s sense of
the word, not this type's: a registered identity carrying a `SessionCapability`, verified
by `ITrustedRegistrar`, that `OpenEpisode` binds an episode to
(backed by the `agent_session_dim` table). It is the Watcher's lane-identity vocabulary
and migrates to "lane" only opportunistically — A3 forbids a big-bang rename, so it stays
session-named here even though this type is now `GovernedEpisode`.

### `IReadOnlyDictionary<string, string?> OpenAttributes { get; }`

The attributes the episode opened with, exactly as sent.

**Remarks.** Exposed so the equality to the goal block is checkable from outside — an invariant only the
implementation can see is one only the implementation can be wrong about.

## `LaneTeardown`

*record* — `GovernedLaneSource.cs`

What a lane teardown did: to the episode, and to the tree.

## `GovernedLane`

*class* — `GovernedLaneSource.cs`

One governed lane's episode and worktree, torn down together.

**Remarks.** **The composition is the point.** Spec R1 requires that killing the engine both closes the
episode `Blocked` and parks a dirty tree; each half is already correct in its own type, and
the failure worth preventing is doing one and not the other — an episode left open reads as a
lane still working, and a deleted tree destroys the only record of what the lane had written when
it died.

| Member | Summary |
|---|---|
| `GovernedLane(` | **(gap)** |
| `LaneTeardown EngineKilled(WorktreeState state)` | The engine process died or was killed: close `Blocked`, and park the tree whatever it holds. |
| `LaneTeardown Close(` | Closes the episode and releases the tree under the fail-safe rule. |

### `GovernedLane(`

- **`session`** — The lane's open episode.
- **`provisioner`** — Who releases the tree.
- **`worktree`** — The tree, or `null` when the lane had none.
- **`seams`** — The lane's lease monitor, when it declared a lease. `null` means no lease was declared — not "no seams found": a lane with no declared scope has nothing to be outside of, and an empty `Lease` is refused rather than read as either extreme.

### `LaneTeardown EngineKilled(WorktreeState state)`

The engine process died or was killed: close `Blocked`, and park the tree whatever it holds.

**Remarks.** **`Blocked`, not `Abandoned`.** Abandonment is a declaration somebody makes;
a kill is something that happened to the lane. Recording it as an abandonment would attribute
a decision to an agent that had none.





**Parked even when clean.** Removal is offered only for a lane that ended merged or
explicitly abandoned, and a killed engine is neither — so this path never asks for one.

### `LaneTeardown Close(`

Closes the episode and releases the tree under the fail-safe rule.

**Remarks.** **An open seam forces `Blocked`, whatever the lane declared** (spec R4 and §8.2:
`seam_resolution_ratio` must be 1.0 before the final close). The outcome is overridden
rather than the close refused: a refusal would leave the episode open, which reads as a lane
still working — the state R1 already decided is the wrong one to leave behind. The override
happens here, at the one place a governed episode closes, so there is no second path that
closes without asking.

## `LaneMode`

*class* — `LaneCohort.cs`

Which door an episode came through — spec §6.1's two coordination modes.

**Remarks.** **A cohort attribute, never a partition axis.** The partition is
`ScoreSegment` and a comparison never crosses it; the mode lives beside the segment so
**one cell holds both cohorts**, which is exactly what R4 requires and what a fourth segment
axis would prevent.





**Constants rather than three literals.** The strings were spelled out at the store, in
its tests, and would have been spelled again at each door. Two definitions of one quantity is a
defect signature (DM7), and the failure it produces here is invisible: an episode stamped
`"Governed"` is a third cohort of one, and the cell still renders.





**Absent is "not recorded", never a default.** There is no member for an unknown mode —
the store answers `null`, which is what every row written before the column existed means.

| Member | Summary |
|---|---|
| `string Governed = "governed"` | An ACP lane the plane drove: the runtime opened and closed the episode. |
| `string Observed = "observed"` | A CLI lane the watcher watched: the session declared its own episode. |

## `LaneScoring`

*class* — `LaneCohort.cs`

The two scoring doors, each stamping the cohort it came through — spec R4.

**Remarks.** **Neither path is new machinery.** The governed door is
`ClosedEpisodeScoring` over a registered session; the observed door is
`ImportAndScoreEpisodesFromAuditLog`. Both run the **unchanged**
`ScoringService`. What this type adds is the two things a caller must not be able to
forget: naming the task class, and stamping which door it was.





**`taskClass` has no default here, deliberately.** Both underlying paths carry one —
`"audit-import"` and `Unclassified` — and only the second is
incomparable. An episode that acquires `"audit-import"` by default therefore does **not**
surface as unranked (`IncomparableReason` never fires for it); it ranks
silently inside a cohort whose name is an implementation detail of an import routine. A required
parameter is the only version of this that cannot be forgotten.

| Member | Summary |
|---|---|
| `string AuditImportDefaultClass = "audit-import"` | The task class `ImportAndScoreEpisodesFromAuditLog` applies when the caller names none — **comparable**, and therefore the dangerous one. |
| `ScoredEpisode ScoreGoverned(` | Scores a governed lane's closed episode under the caller's task class and stamps its cohort. |
| `IReadOnlyList<string> ImportObserved(` | Imports and scores a repository's observed (audit-declared) episodes under the caller's task class, and stamps each one's cohort. Returns the episode ids stamped. |

### `string AuditImportDefaultClass = "audit-import"`

The task class `ImportAndScoreEpisodesFromAuditLog` applies when the
caller names none — **comparable**, and therefore the dangerous one.

**Remarks.** Named as a constant so a test can assert a stored class is not this, rather than describe it.
Retiring the default is owed to Phase 3 (§8.4 controlled task classes); until then this is the
value that must never appear on a cell somebody chose the class for.

### `ScoredEpisode ScoreGoverned(`

Scores a governed lane's closed episode under the caller's task class and stamps its cohort.

- **`taskClass`** — The kind of work. Required — see the type's remarks.

**Throws `AgentPlaneException`.** `GovernedEpisodeNotScored` when the sweep produced no scorecard for this episode — which means it was not closed, or its session was not registered, and a lane whose work scores nowhere is a failure to report rather than absorb.

**Remarks.** **Score, then stamp.** `RecordEpisodeMode` is an
update over an already-scored cell, so stamping first would silently record nothing — and the
episode would rank in the right cell with no cohort, which reads as a pre-migration row.

### `IReadOnlyList<string> ImportObserved(`

Imports and scores a repository's observed (audit-declared) episodes under the caller's task
class, and stamps each one's cohort. Returns the episode ids stamped.

- **`taskClass`** — The kind of work. Required — see the type's remarks.

**Remarks.** **The source is read again for the ids.** The import returns a count, and the cohort stamp
needs identities. Re-parsing the same file is the cost of leaving
`WatcherHost`'s contract alone: R4-core is that the watcher gains callers, not
semantics, and widening a return type to serve one caller is a semantic change.

## `Lease`

*record* — `LeaseAndSeams.cs`

A lane's exclusive write scope — spec §14.3's `lease: { exclusive: [ … ] }`.

**Remarks.** **Repository-relative patterns, matched ordinally.** The patterns are written the way a
person writes them in a plan (`src/Payments/**`), so they are relative and use forward
slashes; a path observed on the wire is absolute and platform-shaped, and
`LeaseMonitor` is where the two meet.





**Case-sensitive, and that direction is deliberate.** On Windows `src/payments/X.cs`
and `src/Payments/X.cs` are one file; here they are two, so the first does not match the
lease and **raises a seam**. The two errors are not symmetric: an extra seam is reported,
read and resolved, while a missed one is an edit that escaped the lease with nothing recorded.
A governance control degrades toward saying something.





**An empty lease is not a lease.** It is refused rather than read as "covers nothing"
(every edit seams — useless) or as "covers everything" (no edit ever seams — worse, because it
looks like it is working). A lane with no declared scope simply has no monitor.

| Member | Summary |
|---|---|
| `bool Covers(string relativePath)` | Whether a repository-relative path falls inside this lease. |

## `Seam`

*record* — `LeaseAndSeams.cs`

One coordination seam: an edit a lane made outside its lease.

**Remarks.** **A ledger entry, not yet a run event.** Spec §7.2 lists `seam.open` and
`seam.resolve` among the v1 kinds, and both are produced by the **conductor** (§5.3
Stage 4, `seam_resolve`), which Phase 1 does not have. Minting a `RunEvent` here
would need a second writer of the per-run `Seq`, and two sequence sources for one run is a
defect with no symptom until the log is read in order. So the seam carries the run, the agent and
the ordinal of the edit that caused it, and Phase 2's conductor is what turns it into an event.

## `LeaseMonitor`

*class* — `LeaseAndSeams.cs`

Watches a governed lane's run events and raises a seam for every edit outside its lease — spec
R4's third bullet and §6.1 ("Leases: enforced").

**Remarks.** **Edits are recognized by the shape that exists.** §7.2 names `file.edit` as a v1
kind, and **no Phase-1 producer emits it**: the captured corpus
(`spikes/acp-subscription-lane/frames/write.jsonl`) shows a file write arriving as a
`tool_call`/`tool_call_update` carrying `kind:"edit"` and a `locations[]`
array, which `AcpRunEventMapper` normalizes to `tool.call`/`tool.result`.
Adding a handler for a kind nothing produces is what N1 forbade; when a producer of
`file.edit` appears, it is a row here, not a redesign.





**One edit is one seam.** The corpus shows the same write four times — a pending
`tool_call` with an *empty* `locations` array, two `tool_call_update`s and a
permission request. Raising per frame would report four violations for one, and
`seam_resolution_ratio` would then be a measure of adapter chattiness. The identity is
`(toolCallId, path)` — the engine's own id for the call, and the file it touched.





**A frame with no path observes nothing.** The pending frame genuinely does not say what
will be written; treating "no path" as "not covered" would make the first announcement of every
edit a violation.

| Member | Summary |
|---|---|
| `LeaseMonitor(Lease lease, string worktreeRoot)` | **(gap)** |
| `IReadOnlyList<Seam> AllSeams` | Every seam raised, in raise order. |
| `IReadOnlyList<Seam> OpenSeams` | The seams still awaiting a ruling. |
| `int RaisedCount` | How many seams this lane raised. |
| `double SeamResolutionRatio` | `seam_resolution_ratio` (spec §8.2): resolved over raised, and **1.0 when none were raised**. |
| `IReadOnlyList<Seam> Observe(RunEvent runEvent)` | Observes one run event, and returns the seams it newly raised (empty for anything that is not an out-of-lease edit). |
| `bool Resolve(string seamId, string ruling)` | Records a ruling on one seam. Returns `false` for an id nobody raised, or one already ruled on. |
| `bool MayCloseCleanly` | Whether the run may close with the outcome it declared (spec §8.2's precondition). |

### `LeaseMonitor(Lease lease, string worktreeRoot)`

- **`lease`** — The lane's exclusive scope.
- **`worktreeRoot`** — The lane's provisioned tree. Observed paths are absolute, lease patterns are relative, and this is what relates them — so a lane's lease is scoped to *its own* tree and an edit anywhere else is outside it by construction.

### `double SeamResolutionRatio`

`seam_resolution_ratio` (spec §8.2): resolved over raised, and **1.0 when none were
raised**.

**Remarks.** A run that never violated its lease has nothing outstanding, and reporting 0 for it would
block every clean run — the empty-numerator error that reads as a working gate.

### `bool Resolve(string seamId, string ruling)`

Records a ruling on one seam. Returns `false` for an id nobody raised, or one already
ruled on.

**Remarks.** **Idempotent and non-overwriting.** A second ruling on one seam would change what the run
closed against after the fact; the first ruling stands and the caller is told it did nothing.

## `BindingRefusal`

*record* — `ProviderConfiguration.cs`

Why a lane could not be bound, as a **field** plus a sentence.

**Remarks.** **A field, never prose alone.** The operator's next action is to edit one line of one file, so
the refusal has to say which line. It is deliberately the same shape as
`AiDe.Core.Presentation.Composer.ComposerFieldError` without being that type: this namespace
is below Presentation and must not reach up into it.

## `ProviderConfiguration`

*class* — `ProviderConfiguration.cs`

`~/.aide/providers.json` — the configured providers, accounts, adapter install root and
per-engine model, as read from the file the operator hand-edits.

**Remarks.** **JSON, and the `.yaml` in spec §14.2 is an erratum.** The spec names
`~/.aide/providers.yaml`; this repository reads `~/.aide/providers.json`. The reason is
the Ruling 36 YAML guard: the only YAML reader in this product is scoped to the template
frontmatter loader, and widening it to a file that carries account identity is a larger security
decision than a config reader is allowed to make on its own. The transcription is otherwise
one-for-one, so a §14.2 file becomes this one by re-punctuating it. Filed as an Addendum erratum
per the Ruling 23 precedent; see `docs/notes/conductor-spec-errata-policy.md`.





**Two fields EXTEND §14.2, and both are marked where they are read.**
`adapterInstallRoot` and `engines.<id>.model` are not in the spec's schema.
`GovernedRunRequest` requires both, §14.2 supplies neither, and §14.2's own answer for
the model — `routing.roles` / `best_fit` — is a routing engine this phase does not
build. They are read here rather than defaulted in code, which is the whole point of the
node.





**Refused, never defaulted, and never silently empty.** A missing file is an absence:
`ReadIfPresent` answers `null` and the shell renders "no agent backend is
configured", which is true. A file that exists and is wrong is a refusal that names the file and
the field — because an empty registry read out of a malformed file renders as the absence state,
which is a wrong claim about a file the operator wrote.





**Health is the operator's record, not a probe.** There is no health prober in this
phase, so `health:` is what the operator observed and wrote down — the posture
`ObservedAuthLabel` already takes. It is required per account rather
than defaulted: a defaulted `ready` is indistinguishable afterwards from an observed one.

| Member | Summary |
|---|---|
| `string FileName = "providers.json"` | The file name, and the erratum's subject: `.json`, not `.yaml`. |
| `string DefaultPath` | Where the file is, by default: `~/.aide/providers.json` (§4.3). |
| `string Path { get; }` | The file this configuration was read from. Every refusal names it. |
| `string AdapterInstallRoot { get; }` | The directory whose `node_modules` holds the ACP adapter. **Extends §14.2.** |
| `ProviderRegistry Registry { get; }` | The providers and accounts, as the registry §4.3 describes. |
| `ProviderConfiguration? ReadIfPresent(string path)` | Reads the file, or answers `null` when there is none. |
| `ProviderConfiguration Read(string path)` | Reads the file. The file must exist. |
| `LaneBinding? Bind(string engineId, out BindingRefusal? refusal)` | Binds one engine to the `(engine, model, account)` triple a run needs, or says which field is missing. |

### `ProviderConfiguration? ReadIfPresent(string path)`

Reads the file, or answers `null` when there is none.

- **`path`** — The file to read. `DefaultPath` when the caller has no reason to differ.

**Throws `AgentPlaneException`.** `ProviderConfigurationMalformed` — the file exists and is wrong. **Never collapsed into `null`:** a caller that could not tell "no file" from "bad file" would render the second as the first.

### `ProviderConfiguration Read(string path)`

Reads the file. The file must exist.

**Throws `AgentPlaneException`.** `ProviderConfigurationMalformed`, naming the file and the field.

### `LaneBinding? Bind(string engineId, out BindingRefusal? refusal)`

Binds one engine to the `(engine, model, account)` triple a run needs, or says which
field is missing.

- **`engineId`** — A catalog engine id — normally the session's one enabled backend.
- **`refusal`** — Why not, when the result is null.

**Remarks.** **Nothing here decides anything the file did not say.** The model comes from
`engines.<id>.model`. The account comes from `engines.<id>.account`, or
from the provider carrying exactly one — which is the file choosing, not this method. Two
accounts and no selection is a refusal, because picking one would be right in every case
anyone checks by hand and wrong in the case that bills the wrong account (DC-110).





**The registry's rules are called, not restated.** Unknown engine, unconfigured
provider, unknown account and `needs-login` are all `ProviderRegistry`'s
refusals; this maps each to the field the operator must edit. A second opinion about what
binds would be a second place for the rule to change.

## `AccountHealth`

*enum* — `ProviderRegistry.cs`

What a per-account health probe observed — spec §4.3's three states, and only those three.

**Remarks.** A closed set, unlike a run-event `kind`: the spec names exactly these, and each has a
different consequence for routing. There is deliberately no `Unknown` member — an account
whose probe has not run yet is not in the registry, which is a different fact from an account
that answered.

## `ProviderAuth`

*enum* — `ProviderRegistry.cs`

How a provider authenticates. §14.2's `auth:` field.

## `ProviderAccount`

*record* — `ProviderRegistry.cs`

One account under a provider. **Accounts are first-class** (§4.3): quota pressure, standings
cohorts and the profiler all key on the account, not on the provider.

## `ProviderRow`

*record* — `ProviderRegistry.cs`

One `providers.json` row (§14.2) — a provider, how it authenticates, and its accounts.

**Remarks.** **It deliberately carries no engine id.** `Provider` already states the
engine → provider mapping, and two definitions of one mapping is a defect signature (DM7): the
day they disagree, a lane binds to one provider's catalog row and another provider's account.

## `LaneBinding`

*record* — `ProviderRegistry.cs`

What a lane is actually bound to: `(engine, model, account)` — never just a provider (§4.3).

## `ProviderRegistry`

*class* — `ProviderRegistry.cs`

The provider registry — spec §4.3. Resolves a lane's `(engine, model, account)` triple, and
refuses everything it cannot resolve.

**Remarks.** **Refused, never defaulted.** A registry that answered a typo with its only configured
provider would bind a lane to somebody else's account, and the first thing to notice would be the
bill. Every lookup here fails loudly, and the message names the value that was not found.





**Constructed from configuration, with no built-in default.** §14.2's example rows carry
account labels like `max-personal`, which are one operator's names for one operator's
logins. A registry that shipped them would assert an account nobody had signed into. The rows
come from `~/.aide/providers.json` — §14.2's `providers.yaml`, with the `.yaml`
filed as an erratum (`docs/notes/conductor-spec-errata-providers-json.md`) — and
`ProviderConfiguration` is the reader. Per-workspace overrides are not built.

| Member | Summary |
|---|---|
| `string DirectApiEngineId = "direct-api"` | The engine id §14.2 gives the direct-API path — the one spec §4.2 forbids for Anthropic while a subscription is configured. It is deliberately **not** an `EngineCatalog` row: Phase 1 implements no direct-API launch pa… |
| `string AnthropicProviderId = "anthropic"` | The provider whose terms of service §4.2 quotes. |
| `ProviderRegistry(IReadOnlyList<ProviderRow> rows)` | **(gap)** |
| `IReadOnlyList<ProviderRow> Rows { get; }` | The configured rows, in configuration order. |
| `ProviderRow Find(string providerId)` | Finds a provider row by id. |
| `LaneBinding Bind(string engineId, string model, string accountLabel)` | Binds a lane to `(engine, model, account)`. |
| `bool HasSubscriptionAccount(string providerId)` | Whether the provider has a configured subscription account — the condition spec §4.2 attaches its direct-API prohibition to. |

### `ProviderRegistry(IReadOnlyList<ProviderRow> rows)`

- **`rows`** — The configured provider rows.

**Throws `AgentPlaneException`.** `UnknownProvider` when two rows claim one provider id — a configuration whose meaning depends on read order, refused rather than resolved by "last wins".

### `ProviderRow Find(string providerId)`

Finds a provider row by id.

**Throws `AgentPlaneException`.** `UnknownProvider` — refused, never defaulted, and least of all when exactly one provider is configured and "they must have meant that one" is tempting.

### `LaneBinding Bind(string engineId, string model, string accountLabel)`

Binds a lane to `(engine, model, account)`.

- **`engineId`** — A catalog engine id. The engine → provider mapping is the catalog's.
- **`model`** — The model this lane runs on.
- **`accountLabel`** — The configured account label.

**Throws `AgentPlaneException`.** `UnknownEngine` for an engine the catalog does not carry; `UnknownProvider` for a catalogued engine whose provider is not configured; `UnknownAccount` for a label the provider does not carry; `AccountNotReady` for an account whose probe says `needs-login`.

### `bool HasSubscriptionAccount(string providerId)`

Whether the provider has a configured subscription account — the condition spec §4.2 attaches
its direct-API prohibition to.

**Remarks.** Health is deliberately not consulted. A subscription that needs a login is still a configured
subscription, and the prohibition is about what the operator has, not about what is reachable
this minute. Reading health here would lift the ban exactly when a login expired.

## `AgentPlaneException`

*class* — `RunEvent.cs`

An agent-plane failure carrying a stable `Code` (Observability Standard O7). The
message is for humans and may change; the code is for machines and search and does not.

**Remarks.** Mirrors `AiDe.Core.Watcher.WatcherException` deliberately rather than sharing a base type:
two exception families with two code ranges keep a caller's `catch` honest about which
subsystem failed, and the duplication is a constructor and a property.

| Member | Summary |
|---|---|
| `AgentPlaneException(string code, string message) : base(message)` | **(gap)** |
| `string Code { get; }` | A stable `AgentPlaneErrorCodes` value. |

## `AgentPlaneErrorCodes`

*class* — `RunEvent.cs`

Stable error codes for the agent plane. Search-key stability is the whole point.

| Member | Summary |
|---|---|
| `string UnknownEngine = "AP-0001"` | An engine id that the catalog does not carry. Never defaulted. |
| `string LaunchPathNotImplemented = "AP-0002"` | An engine whose declared ACP mode is not `adapter`, the only Phase-1 launch path. |
| `string AdapterEntryModuleNotRecorded = "AP-0003"` | An adapter engine whose entry module has never been observed on a real install. |
| `string MalformedFrame = "AP-0004"` | A wire frame that is not a JSON object, so it carries no event at all. |
| `string UnknownProvider = "AP-0005"` | A provider id the registry does not carry. Never defaulted to the one that is configured. |
| `string UnknownAccount = "AP-0006"` | An account label the configured provider does not carry. |
| `string AccountNotReady = "AP-0007"` | An account whose observed health is `needs-login`. Spec §4.3: "the router treats `needs-login` as absent." |
| `string GoalBlockIncomplete = "AP-0008"` | A goal block missing one or more of the six fields spec §14.3 names. |
| `string DirectApiRefusedByToS = "AP-0009"` | An Anthropic direct-api spawn attempted while a subscription account is configured — spec §4.2's prohibition, enforced rather than documented. |
| `string ObservedAuthNotRecorded = "AP-0010"` | No observed auth status for a subscription-configured account. **Fails closed:** absent is "not recorded", and a spawn on "not recorded" is a guess with a bill attached. |
| `string ObservedAuthNotSubscription = "AP-0011"` | An observed auth status that is present and is not a subscription. |
| `string WorktreeProvisionFailed = "AP-0012"` | A worktree that could not be provisioned. Fatal: a governed lane has no shared-checkout fallback. |
| `string ObservedAuthAccountMismatch = "AP-0013"` | An observed auth label that contradicts the one the operator recorded for the configured account. Refused: the lane would bill, rank and report against a different subscription. |
| `string EngineStreamEnded = "AP-0014"` | The engine's stdout ended while a request was outstanding — it exited or was killed. |
| `string EngineRequestTimedOut = "AP-0015"` | The engine never answered a request. A hung child, bounded rather than waited on forever. |
| `string EngineReturnedError = "AP-0016"` | The engine answered with a JSON-RPC error. Its code and message travel on the refusal. |
| `string ProtocolVersionMismatch = "AP-0017"` | The engine echoed a protocol version this client does not speak, or echoed none. Refused: every frame would still parse and the meanings would have moved. |
| `string SessionCwdNotAbsolute = "AP-0018"` | A session cwd that is not absolute. Refused before the wire, so the reason names the caller rather than arriving later as the adapter's own `-32602`. |
| `string EngineDidNotStart = "AP-0019"` | The engine executable could not be started at all. |
| `string GovernedEpisodeNotScored = "AP-0020"` | A governed lane's closed episode produced no scorecard, so it belongs to no cohort. Reported rather than absorbed: an episode that scores nowhere is indistinguishable from a lane that never ran. |
| `string ProviderConfigurationMalformed = "AP-0021"` | `~/.aide/providers.json` exists and is wrong — a missing field, an unknown key, or a value outside a closed set. **Distinct from the file being absent**, which is not an error at all: an absent file is an operator who… |

## `RunEventCost`

*record* — `RunEvent.cs`

The metered cost of one run event, in the spec's §14.1 shape
(`tokens_in · tokens_out · cache_read · requests · credits?`).

**Remarks.** **Null, never zero, when the wire did not say.** Most ACP frames carry no usage at all,
and a zero would read as "this cost nothing" rather than "not recorded" (IO12).




`Credits` is nullable because it belongs to metered lanes only; no Phase-1 engine is
metered, so nothing populates it yet.

## `RunEvent`

*record* — `RunEvent.cs`

The one normalized envelope every run-event source produces — spec §7.2 and §14.1.

**Remarks.** **`Kind` is an open string, not an enum.** The spec states the rule itself:
evolution is "additive only; consumers ignore unknown kinds". An enum would make every new
adapter kind a compile break in every consumer, which converts an additive protocol change into
a breaking one — the exact failure the ACP fidelity-drift risk (§13, risk 1) names. The v1 kinds
are a vocabulary, not a closed set: `agent.msg`, `tool.call`, `tool.result`,
`permission.request` and the rest are what a producer *may* emit, and an unrecognized
wire kind is namespaced (`acp.*`) and carried rather than rejected.





**`ParentAgentId` is carried and never populated in Phase 1.** There is no
Conductor until Phase 2, so nothing can know a parent. The field exists so the envelope is the
spec's shape today rather than a migration tomorrow; building anything on it now would be
building on a value that is always null.





**`Ext` preservation is load-bearing.** Anything the mapper did not recognize
survives verbatim under `Ext`. §13's mitigation for adapter drift is precisely "unknown
events preserved under ext" — a dropped field is silent data loss in what §7.1 calls the truth.

## `RunStage`

*enum* — `RunTriage.cs`

The run lifecycle's stages — spec §5.3.

**Remarks.** An enum rather than an open string, and for the opposite reason `Kind` is a
string: kinds arrive from an adapter that may invent one, whereas the stage list is *this*
product's own lifecycle. A stage nobody defined is a bug in the plane, not an additive protocol
change, so a compile break is the right answer.

## `RunTriage`

*record* — `RunTriage.cs`

Stage-0 triage: whether this run skips plan and council on its way to dispatch — spec §5.3 and
R2's third bullet (line 362).

**Remarks.** **The decision is made from what the block declares, and nothing else.** §5.3 phrases
the skip as "a T0/T1 run (no fan-out, no loop, no gate)". `All` is
the whole of a goal block, and it carries **no loop and no gate field** — so the two inputs
that exist are the tier and the fan-out cap. Guessing at a loop from a goal string would be
inventing a value that decides whether a run gets reviewed. **The trigger to revisit this is a
goal block that gains a field for either**; until then a run whose shape has a loop or a gate is
declared at a tier that does not skip, which is the discipline CT19 already asks for.





**A missing tier or cap does not skip.** Absence degrades toward *more* ceremony,
never less: an unreadable block must not be the cheapest way to avoid a council. It is also
unreachable in practice — `SpawnContract` refuses such a block before any spawn — so
this is the second wall, not the first.





**What "zero council lanes and zero plan artifacts" means here.** Phase 1 has no
conductor, so nothing produces either. The skip is therefore expressed as the stages the run
passes: on the skip path `Plan` and `Council` are
absent, so there is no stage that could produce one. A test that wants the counts measured rather
than implied reads them off the store after the run really dispatches.

| Member | Summary |
|---|---|
| `IReadOnlyList<string> SkipEligibleTiers = ["T0", "T1"]` | The tiers §5.3 names as eligible for the Stage-0 skip. |
| `RunTriage For(GoalBlock block)` | Triages one goal block. |

## `TerminalHostingLedger`

*class* — `TerminalHostingLedger.cs`

Counts terminal-host constructions while it is open — the positive oracle behind spec R1's
"zero terminal hosting" claim.

**Remarks.** **An absence claim with no oracle is not evidence.** "No terminal was hosted" is
unfalsifiable as prose: it reads identically whether the plane avoided the terminal stack or
whether nobody looked. This makes it a number, so the claim is `== 0` against a counter that
can be shown going to 1.





**It listens rather than instruments, and that is the point.**
`ConPtyTerminalSession.StartAsync` already opens a `terminal.start` activity on the
`aide.terminal.runtime` source, unconditionally, before it touches interop — so the
measurement rides the emission that is already on the normal path (IO2) and
`src/AiDe.Core/Terminal/` gains not one line. That matters twice: R4-core requires the
terminal stack to gain callers rather than semantics, and a counter added inside the thing being
measured is one an edit to that thing can remove without any test noticing.





**It counts the attempt, not the success.** The activity opens before the pseudo console
exists, so a construction that then fails still counts. A lane that tried to host a terminal and
failed did not achieve "zero terminal hosting"; it achieved a broken terminal.





**Scoped, because an `ActivityListener` is process-global.** The count
belongs to a run, so the ledger is a disposable window over one. Two overlapping ledgers each see
every start in the process, which is correct for a claim of the form "none happened anywhere
while this ran".

| Member | Summary |
|---|---|
| `string TerminalActivitySource = "aide.terminal.runtime"` | The activity source `ConPtyTerminalSession` publishes on. Observed, not assumed. |
| `string TerminalStartActivity = "terminal.start"` | The activity name it opens for one construction. |
| `string TerminalStopActivity = "terminal.stop"` | The activity name it opens once per session when the session ends — by its child's exit, by disposal, or by a construction that failed after the start was counted (INV-0010). |
| `long Constructions` | How many terminal hosts were constructed since this ledger opened. |
| `long Completions` | How many sessions ended since this ledger opened. `Constructions − Completions` is the number of hosts the runtime still holds — the census-time invariant INV-0010 could not check. |
| `TerminalHostingLedger Open()` | Opens a ledger. Counting starts here and stops at `Dispose`. |
| `void Dispose()` | **(gap)** |

## `ProcessResult`

*record* — `WorktreeProvisioner.cs`

What one child process did. `ExitCode` is the fact; the streams are the reason.

## `IProcessRunner`

*interface* — `WorktreeProvisioner.cs`

The seam over launching a child process, so the plane's decisions are testable without a real
repository on disk.

**Remarks.** The thing worth testing about provisioning is **which directory a command ran in**, and that
is invisible to any test that only checks the result. This interface exists so that assertion can
be made; it is not an abstraction over process launching in general.

## `ProcessRunner`

*class* — `WorktreeProvisioner.cs`

The real runner: a child process, captured streams, a bounded wait.

**Remarks.** **A timeout, not a hope.** A hung `git` or `coord` would otherwise hold a lane
spawn open forever. On timeout the tree is killed and the result reads as a failure with the
reason, which the provisioner then reports rather than guessing past.





**Both streams are read before waiting** — a child that fills the stderr pipe while the
parent waits on exit deadlocks, which is the classic shape of this bug.

| Member | Summary |
|---|---|
| `ProcessRunner(TimeSpan? timeout = null)` | **(gap)** |
| `ProcessResult Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory)` | **(gap)** |

### `ProcessRunner(TimeSpan? timeout = null)`

- **`timeout`** — How long any one command may take. Defaults to 60 seconds.

## `ProvisionedWorktree`

*record* — `WorktreeProvisioner.cs`

A worktree the plane created for one lane.

## `WorktreeState`

*record* — `WorktreeProvisioner.cs`

What a tree is carrying — the two facts that make a deletion unsafe.

## `LaneClosure`

*enum* — `WorktreeProvisioner.cs`

How the lane ended, as far as anyone declared.

## `WorktreeDispositionKind`

*enum* — `WorktreeProvisioner.cs`

What happened to a tree at lane close.

## `WorktreeDisposition`

*record* — `WorktreeProvisioner.cs`

A disposition and the reason it was reached. A parked tree with no cause is one nobody dares delete.

## `WorktreeProvisioner`

*class* — `WorktreeProvisioner.cs`

Provisions and releases a lane's worktree — spec §6.4.

**Remarks.** **Reuse, recorded.** The naming and placement decisions are
`For`'s and are not re-derived here; this type is the execution half
that `WorkbenchShell.ProvisionAgentWorktree` held, moved into Core so a lane can be
provisioned with no App present. The one deliberate behavioural difference: the shell could
always fall back to sharing the workspace and therefore swallowed git's stderr, while a governed
lane has no such fallback — sharing the checkout would put two writers on one index — so a
failure here is fatal and carries git's own reason.





**`coord install` runs inside the new tree.** `.git/config` is per-clone and a
worktree has its own, so an install in the parent leaves the new tree with no merge drivers and
no hooks — and nothing fails: the lane runs, and the first coordination conflict resolves the
wrong way.





**DC-053 / WT13:** a worktree isolates the working tree and the index. `stash`,
`bisect` and notes are repository-global and are *not* isolated by anything here.





**Cleanup is fail-safe and opt-in.** A wrong park costs a stale directory; a wrong
removal costs work that exists nowhere else. So every state but "provably safe AND asked for"
parks, and the reason travels with the disposition.

| Member | Summary |
|---|---|
| `WorktreeProvisioner(IProcessRunner runner, string coordCommand = "coord")` | **(gap)** |
| `ProvisionedWorktree Provision(string repositoryRoot, string harness, string sessionId)` | Cuts a worktree for one lane and installs the coordination layer inside it. |
| `WorktreeState Inspect(ProvisionedWorktree tree)` | Reads what the tree is carrying. |
| `WorktreeDisposition Release(` | Decides what happens to the tree at lane close, and does it. |

### `WorktreeProvisioner(IProcessRunner runner, string coordCommand = "coord")`

- **`runner`** — How child processes are launched.
- **`coordCommand`** — The coordination CLI. Injectable because the pack's scripts are invoked differently per machine — `python3` is not present on Windows, where the shim is what resolves it — and a hard-coded name would make this fail on one platform and pass on the other.

### `ProvisionedWorktree Provision(string repositoryRoot, string harness, string sessionId)`

Cuts a worktree for one lane and installs the coordination layer inside it.

**Throws `AgentPlaneException`.** `WorktreeProvisionFailed` when no sibling location can be derived, or when git refuses. Fatal by design: a governed lane sharing the primary checkout is the isolation failure the tree exists to prevent.

### `WorktreeState Inspect(ProvisionedWorktree tree)`

Reads what the tree is carrying.

**Remarks.** **An unanswerable upstream query counts as unpushed work.** `git rev-list @{u}..HEAD`
fails outright on a branch with no upstream — which is exactly the state a freshly provisioned
agent branch is in. Reading that failure as "nothing unpushed" would delete the one tree whose
commits exist in no other place. Every measurement here degrades toward "unsafe", never toward
a plausible clean answer.

### `WorktreeDisposition Release(`

Decides what happens to the tree at lane close, and does it.

- **`tree`** — The tree.
- **`closure`** — How the lane ended, as declared.
- **`state`** — What the tree is carrying — from `Inspect` or from the caller.
- **`removeWhenSafe`** — Deletion is **opt-in**. Absent this, a provably safe tree is still parked: the product does not delete a person's directory because a lane finished.
