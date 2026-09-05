---
id: api-aide-core-ipc
title: "API: AiDe.Core.Ipc"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Ipc: 51 types, 120 members, 64% carrying a summary doc comment.
---

# API: `AiDe.Core.Ipc`

**51 public types · 120 public members · 64% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `Capability`

*record* — `CapabilityRegistry.cs`

A capability the daemon issued, and everything it is bound to.

## `CapabilityCheck`

*record* — `CapabilityRegistry.cs`

Why a capability check failed, or that it passed.

| Member | Summary |
|---|---|
| `CapabilityCheck Valid = new(true, null, null)` | **(gap)** |
| `CapabilityCheck Fail(string code, string reason)` | **(gap)** |

## `CapabilityRegistry`

*class* — `CapabilityRegistry.cs`

Issues, validates and revokes the capabilities that authorize commands on the IPC boundary.

**Remarks.** **In memory only, and deliberately so.** A capability that outlived the daemon would
authorize a caller against a process that no longer exists, and persisting it would create a
file whose theft is equivalent to the authority itself. Restarting the daemon revokes
everything, which is the correct blast radius.





**Bound to four things, checked in a fixed order** — connection, process, workspace,
epoch. Each closes a distinct attack: replaying a token on a second connection; a different
process on the same connection; reaching another workspace's daemon; and acting against state
that has since been replaced. Binding to fewer would make the token a bearer secret, which is
what capability-based authorization exists to avoid.





Comparison is **constant-time**. Token lookup by dictionary is not, so the token is
found by key and then verified by `FixedTimeEquals` — the
dictionary hit only says a record exists, never that the caller's bytes matched it.

| Member | Summary |
|---|---|
| `int Count` | How many capabilities are live — for the health surface, not for authorization. |
| `Capability Issue(IpcPeer peer, string workspaceId, long epoch)` | Issues a capability bound to this peer, workspace and epoch. |
| `CapabilityCheck Validate(` | Validates a presented token against the live connection and the command it accompanies. |
| `bool Revoke(string token)` | Revokes one capability. Idempotent: revoking twice is not an error. |
| `int RevokeConnection(string connectionId)` | Revokes everything issued to a connection — what a disconnect must trigger. |

### `CapabilityCheck Validate(`

Validates a presented token against the live connection and the command it accompanies.

**Remarks.** Returns a typed reason rather than a bool. Which check failed is the difference between "your
session ended" and "that token belongs to another workspace", and an operator who only sees
"denied" cannot tell an expired shell from an attack.

## `DaemonEndpoint`

*class* — `DaemonEndpoint.cs`

The daemon's side of the boundary: handshake, authorization, dispatch to an operation.

**Remarks.** Transport-free on purpose. Everything security-relevant here — version acceptance,
capability binding, the order the checks run in — is decided without a socket, so it can be
tested without one. The named-pipe layer's only job is to establish who the peer is and hand
bytes across; if that layer were also making authorization decisions, those decisions would only
be testable by standing up a pipe.





**Check order is load-bearing:** version, then envelope shape, then workspace, then
capability, then epoch. Each stage assumes the previous one held, and reordering them leaks
information — validating a capability before checking the workspace would tell an unauthorized
caller whether a token is live on a workspace it has no business naming.

| Member | Summary |
|---|---|
| `DaemonEndpoint(` | **(gap)** |
| `string WorkspaceId { get; }` | **(gap)** |
| `void Register(string operation, Func<IpcRequest, IpcPeer, IpcResponse> handler)` | Registers an operation. Unregistered operations are rejected, never guessed at. |
| `IpcResponse OpenWorkspace(IpcRequest request, IpcPeer peer)` | The opening exchange: agree a version and issue a capability, in that order. |
| `IpcResponse Invoke(IpcRequest request, IpcPeer peer)` | Handles a command: every gate, in order, before any operation runs. |

### `IpcResponse OpenWorkspace(IpcRequest request, IpcPeer peer)`

The opening exchange: agree a version and issue a capability, in that order.

**Remarks.** Version is settled BEFORE a capability exists, so a peer speaking an unsupported protocol
never obtains authority — not even briefly. The reverse order would hand out a token and then
discover the holder cannot be understood.

## `IpcClient`

*class* — `IpcClient.cs`

The shell's side of the boundary.

**Remarks.** **Holds the capability and nothing else.** The token exists only in memory here and is
attached to every request; it is never written anywhere, because a capability on disk is a file
whose theft equals the authority it carries.





**Serialises its own requests.** A pipe is one stream, so two overlapping writes would
interleave frames and the daemon would resynchronise on data that was never a length prefix.
One outstanding exchange at a time is not a limitation to work around — it is what makes the
framing sound.

| Member | Summary |
|---|---|
| `bool IsOpen` | The capability this connection holds, once a workspace has been opened. |
| `long Epoch { get; private set; }` | The epoch the daemon reported at handshake, which every request is judged against. |
| `Task<IpcClient> ConnectAsync(` | Connects to the daemon serving . |
| `Task<IpcResponse> OpenWorkspaceAsync(` | Performs the handshake and keeps the capability it returns. |
| `Task<IpcResponse> InvokeAsync(` | Invokes an operation, attaching the held capability. |
| `ValueTask DisposeAsync()` | **(gap)** |

### `Task<IpcResponse> OpenWorkspaceAsync(`

Performs the handshake and keeps the capability it returns.

**Remarks.** The token is stored rather than handed back so a caller cannot accidentally log it, put it in
a span, or pass it somewhere it outlives the connection it is bound to.

## `IpcErrorCodes`

*class* — `IpcContract.cs`

Stable, catalogued failure codes for the IPC boundary.

**Remarks.** Stable strings rather than an enum's numbers because they cross a process boundary and appear in
operator-facing output. A renumbered enum silently changes what a log line means; a renamed
string breaks a test.

| Member | Summary |
|---|---|
| `string UnsupportedVersion = "ipc.unsupported_version"` | **(gap)** |
| `string MalformedEnvelope = "ipc.malformed_envelope"` | **(gap)** |
| `string CapabilityUnknown = "ipc.capability_unknown"` | **(gap)** |
| `string CapabilityRevoked = "ipc.capability_revoked"` | **(gap)** |
| `string CapabilityWrongConnection = "ipc.capability_wrong_connection"` | **(gap)** |
| `string CapabilityWrongProcess = "ipc.capability_wrong_process"` | **(gap)** |
| `string WorkspaceMismatch = "ipc.workspace_mismatch"` | **(gap)** |
| `string EpochStale = "ipc.epoch_stale"` | **(gap)** |
| `string NotAuthorized = "ipc.not_authorized"` | **(gap)** |
| `string WorkspaceLocked = "ipc.workspace_locked"` | Another daemon already serves this workspace. |
| `string TransportClosed = "ipc.transport_closed"` | The daemon went away without answering. A transport fact, deliberately NOT an authorization one: reporting a vanished daemon as "not authorized" sends every investigation to the wrong place, and it briefly did exactly… |
| `string PayloadTooLarge = "ipc.payload_too_large"` | The response is larger than one frame can carry. |
| `string CommandUnknown = "ipc.command_unknown"` | This daemon has no record of the command being asked about. |

### `string PayloadTooLarge = "ipc.payload_too_large"`

The response is larger than one frame can carry.

**Remarks.** INV-0003. Without this code an oversized response threw out of the write path, the serve loop
did not catch that exception type, and the connection closed with no reply — which the client
can only report as `TransportClosed`. "The daemon vanished" and "the answer is too
big to send" need different things from a user, and rendering the second as the first sends
them to look at the daemon.

### `string CommandUnknown = "ipc.command_unknown"`

This daemon has no record of the command being asked about.

**Remarks.** Distinct from a failure of the command itself: "I never started that, or no longer remember
it" is information a caller acts on differently from "it ran and did not work".

## `IpcVersion`

*class* — `IpcContract.cs`

Which IPC majors this build speaks.

**Remarks.** Two majors, never one. During an upgrade a new shell may meet an old daemon or the reverse,
and a single-version boundary makes every upgrade a synchronised restart of both — which is
exactly what the rollback path cannot rely on.





**Never negotiated down silently.** An unsupported version is rejected with
`UnsupportedVersion`. Silent downgrade is how a peer ends up speaking a
protocol neither side chose, and the failure appears far from its cause.

| Member | Summary |
|---|---|
| `int Current = 3` | **(gap)** |
| `int Previous = 2` | The one previous major still accepted, so an upgrade need not be simultaneous. |
| `bool IsSupported(int major)` | **(gap)** |
| `IReadOnlyList<int> Supported` | **(gap)** |

### `int Current = 3`

**Remarks.** **3 carries the payload as JSON, not as a string containing JSON.** Through 2 a payload was
serialised and the resulting TEXT was placed in a string field, so the transport re-escaped
every quote in it — MEASURED at 1.56-1.57x, which is how a 727,244-byte graph became 1,137,104
bytes on the wire and was refused (DC-047). A peer speaking 2 is still understood, because
`IpcPayload` reads either form.

## `IpcPayload`

*class* — `IpcContract.cs`

Reading a payload, in either encoding a peer might have sent.

**Remarks.** From version 3 a payload IS JSON: the envelope carries the value itself, so nothing is
escaped twice and the bytes measured are the bytes sent. Through version 2 the payload was a
string holding JSON text, which the envelope then re-escaped — the encoding that made a graph
inside its byte budget too large for the frame it was budgeted against (DC-047).





**Read tolerantly, write one way.** A JSON string where a value is expected is a version-2
peer, and its text is parsed rather than rejected — that is what keeps `Previous`
a real guarantee instead of a comment. Writing only ever produces the new form: two encodings on
the write side is how a wire format ends up with no single answer to "what does this look like".

| Member | Summary |
|---|---|
| `T? Read<T>(System.Text.Json.JsonElement? payload, System.Text.Json.JsonSerializerOptions options)` | The payload as , or default when there is none. |
| `System.Text.Json.JsonElement From<T>(T value, System.Text.Json.JsonSerializerOptions options)` | A value as a payload. Always the current encoding. |

### `T? Read<T>(System.Text.Json.JsonElement? payload, System.Text.Json.JsonSerializerOptions options)`

The payload as , or default when there is none.

**Throws `JsonException`.** The payload is not valid JSON for T.

## `IpcRequest`

*record* — `IpcContract.cs`

One request across the boundary.

**Remarks.** carries the architecture's idempotency semantics unchanged — the
same id is the same command, and a retry after an unknown outcome must return the existing
receipt rather than acting twice. Phase 1 simply had a shorter path to the same contract.

## `IpcResponse`

*record* — `IpcContract.cs`

One reply. Either  with a payload, or an error code and reason.

| Member | Summary |
|---|---|
| `IpcResponse Success(System.Text.Json.JsonElement? payload = null)` | **(gap)** |
| `IpcResponse Success<T>(T result, System.Text.Json.JsonSerializerOptions options)` | The common case: a result object, carried as JSON rather than as text about JSON. |
| `IpcResponse Error(string code, string reason)` | **(gap)** |
| `IpcResponse UnsupportedVersion(int requested)` | A version rejection, which uniquely carries what this build DOES speak. |

### `IpcResponse UnsupportedVersion(int requested)`

A version rejection, which uniquely carries what this build DOES speak.

**Remarks.** Returning the supported set turns "we disagree" into something a peer can act on: the
bootstrap can decide to upgrade, roll back, or stop. A bare rejection leaves it guessing, and
guessing across a version boundary is how a downgrade loop starts.

## `IpcOpenResult`

*record* — `IpcContract.cs`

What a successful handshake returns.

**Remarks.** **The epoch is here because there is nowhere else it can come from.** Every command
states the epoch it was authored against and the daemon rejects a mismatch — which leaves a shell
that has just connected unable to ask for the epoch, because asking is itself a command subject
to the fence. Returning it from the handshake is the only ordering that terminates.





The alternative — exempting an `epoch` operation from the fence — would put a hole in
the check to work around an ordering problem, and holes in fences are how the next thing gets
exempted too.

## `IpcPeer`

*record* — `IpcContract.cs`

Who is on the other end of a connection, as established by the transport.

**Remarks.** Built by the transport from the authenticated connection, never from anything the caller sends.
A peer that could name its own identity could name someone else's, which is the whole point of
binding a capability to the connection rather than to a claim.

| Member | Summary |
|---|---|
| `CallerPrincipal ToPrincipal()` | **(gap)** |

## `IpcFraming`

*class* — `IpcFraming.cs`

Turns a byte stream into discrete messages: a four-byte big-endian length, then UTF-8.

**Remarks.** **This is where the boundary's hostile-input surface begins.** Everything above assumes
it is handed one whole request at a time, and every one of those assumptions is exactly as true
as this layer. The length prefix is chosen by the peer, so allocating what it asks for would be a
remote memory exhaustion written in one line — the cap is checked *before* any buffer
exists.





**A short read is not an error.** A peer hanging up is how every connection ends, so an
incomplete frame reads as `null` — "no message" — rather than throwing. Only a frame that is
actively malformed (a length that is negative or beyond the cap) is a protocol violation, because
only that requires a peer to have sent something no correct implementation sends.





**Big-endian** because it is the network order every wire format uses, and a boundary is
no place to depend on both ends having the same architecture.

| Member | Summary |
|---|---|
| `int MaxFrameBytes = 1024 * 1024` | The largest frame either side will send or accept. |
| `Task WriteAsync(Stream stream, string message, CancellationToken cancellationToken)` | Writes one framed message. |
| `Task<string?> ReadAsync(Stream stream, CancellationToken cancellationToken)` | Reads one framed message, or `null` when the stream ends. |

### `int MaxFrameBytes = 1024 * 1024`

The largest frame either side will send or accept.

**Remarks.** A cap in the hundreds of megabytes would satisfy every round-trip test and defend
against nothing, so this is deliberately close to what real traffic needs.





**The upgrade trigger FIRED, and the answer was none of the options this marker
listed.** INV-0003: the whole-graph response for a real repository was 1,522,284 bytes, the
write threw, and the connection closed with no reply. The marker offered two ways out — a
bigger frame, or a data lane — and the correct one was a third: the operation did not
legitimately need to carry more. A 2,815-node hairball was never a useful answer, the surface
spec had always said so (US-K2), and the resolution was to bound every response BELOW this
cap and add an aggregated overview. Recorded because a marker that names two exits invites
you to take one of them.





**And its original premise was already false when audited.** It said "a control lane
carries envelopes, not payloads: the largest legitimate message is a command with a small
JSON body". MEASURED on real repositories, ordinary responses are an evidence page at 659,164
bytes, a graph at 475,223 and an overview at 345,507. This lane has carried payloads for some
time; the sentence describing it had not been re-read since it was true.





`simplify: one flat cap rather than per-operation limits; ceiling 1 MiB; upgrade
trigger = a response that is BOUNDED, USEFUL and still over the cap — which is the case the
bounded projections have not produced yet, and the only one that would justify a data
lane.`

### `Task WriteAsync(Stream stream, string message, CancellationToken cancellationToken)`

Writes one framed message.

**Throws `ArgumentException`.** The message exceeds `MaxFrameBytes`.

### `Task<string?> ReadAsync(Stream stream, CancellationToken cancellationToken)`

Reads one framed message, or `null` when the stream ends.

**Throws `InvalidDataException`.** The length prefix is negative or above the cap.

## `IpcPipeName`

*class* — `IpcPipe.cs`

The pipe name for a workspace, derived rather than configured.

**Remarks.** Both ends must agree without talking first, so the name is a pure function of the workspace
path. Deriving it also keeps the path **out of the name**: pipe names are enumerable by any
process on the machine, and a name like `aide.C__Users_someone_clients_acme` would disclose
what a user is working on to anything that can list a directory.





Hashed lowercase-invariant because Windows paths are case-insensitive: the same workspace
reached as `C:\Work` and `c:\work` must be one daemon, not two racing for one store.

| Member | Summary |
|---|---|
| `string ForWorkspace(string workspacePath)` | The pipe name serving . |

## `WorkspaceLock`

*class* — `IpcPipe.cs`

One daemon per workspace, enforced by the operating system rather than by convention.

**Remarks.** Two daemons on one workspace would be two writers to one store, each believing it owns the
epoch. Nothing above this notices — both would work perfectly, and the damage would appear later
as a fact store whose history has two authors. So the lock is taken **before** anything is
opened, and a daemon that cannot take it exits rather than degrading.





A named mutex rather than a lock file, because the kernel releases it when the holder dies
however it dies. A lock file outlives a killed process and needs staleness heuristics — which are
guesses about whether another process is alive, and wrong guesses here mean either a permanently
unopenable workspace or two writers.





**Local, not Global.** The scope of the invariant is one user's session; a machine-wide
name would let one user's daemon block another's, which is a denial of service reachable by
opening a folder.

| Member | Summary |
|---|---|
| `bool TryAcquire(string workspacePath, out WorkspaceLock? held)` | Takes the lock, or reports that another daemon already holds it. |
| `void Dispose()` | **(gap)** |

## `IpcPipeFactory`

*class* — `IpcPipe.cs`

Creates pipe endpoints that only the workspace owner can reach.

**Remarks.** **The ACL is the first of two controls, not the only one.** It stops another user's
process from connecting at all. The server still derives the peer's SID after connecting and
checks it, because defence that exists only in an access-control list is defence that disappears
the moment someone constructs a pipe by a different route — and because a control nothing
verifies is a control nobody notices losing.

| Member | Summary |
|---|---|
| `NamedPipeServerStream CreateServer(string pipeName, int maxInstances)` | A server instance whose ACL admits only the current user. |
| `NamedPipeClientStream CreateClient(string pipeName)` | A client end for . |
| `string OwnerSid()` | The SID of the user this process runs as — the workspace owner. |
| `IpcPeer PeerOf(NamedPipeServerStream pipe, string connectionId)` | Who is on the other end, established from the connection itself. |

### `NamedPipeClientStream CreateClient(string pipeName)`

A client end for .

**Remarks.** `CurrentUserOnly` is the client-side half, and it defends the opposite direction from
the server's ACL: the ACL stops another user reaching our daemon, while this stops us
reaching *theirs*. Without it another user could create a pipe of the expected name first and
harvest whatever a shell sent to what it believed was its own daemon.

It appears only on this end because the framework refuses to combine it with the explicit
server ACL — they are two spellings of one intent, and the server's is the one a test can
read back.

### `IpcPeer PeerOf(NamedPipeServerStream pipe, string connectionId)`

Who is on the other end, established from the connection itself.

**Remarks.** Both values come from the kernel, never from anything the peer sent. A peer that could state
its own identity could state someone else's, which is the entire reason a capability binds to
the connection rather than to a claim.

## `IpcMessage`

*record* — `IpcServer.cs`

What the wire carries: which exchange this is, and its envelope.

**Remarks.** The kind is separate from the operation because `open` is the exchange that *grants*
authority and every other one *spends* it. Making it just another operation name would put
the one unauthenticated entry point in the same table as the authorized ones, one typo away from
being reachable without a capability.

| Member | Summary |
|---|---|
| `string Open = "open"` | **(gap)** |
| `string Invoke = "invoke"` | **(gap)** |

## `IpcServerOptions`

*record* — `IpcServer.cs`

Bounds on what one daemon will accept.

**Remarks.** Every value is a refusal threshold rather than a tuning knob: each one is the point past which
the daemon stops serving rather than degrading, because a boundary that queues without limit has
simply moved the failure somewhere less visible.

| Member | Summary |
|---|---|
| `TimeSpan Response` | How long a single response may take to write before the connection is abandoned. |
| `TimeSpan Idle` | How long the daemon lingers after its last client leaves. |
| `TimeSpan Startup` | How long the daemon waits for its first client before concluding it was orphaned. |

### `TimeSpan Response`

How long a single response may take to write before the connection is abandoned.

**Remarks.** A client that pipelines requests and never reads its responses fills the pipe's buffer; the
daemon then blocks writing, stops reading, and that listener is held for as long as the client
cares to hold it. With a fixed listener pool, enough such clients make the daemon unreachable
to honest shells. This bounds how long one of them can occupy a listener.

### `TimeSpan Idle`

How long the daemon lingers after its last client leaves.

**Remarks.** Long enough to survive a shell restarting — otherwise every restart pays a cold start and
loses warm state — and short enough that a forgotten daemon is not resident indefinitely.

## `IpcServer`

*class* — `IpcServer.cs`

The named-pipe transport: establishes who the peer is, and hands bytes to the endpoint.

**Remarks.** **This layer decides nothing about authorization.** Version acceptance, capability
binding and the order of the checks all live in `DaemonEndpoint`, which is why they
were testable long before this existed. What belongs here is only what cannot be known without a
connection: who the peer is, how many of them there are, and how fast they are asking.





**Identity is established twice, on purpose.** The pipe's ACL admits only the owner, and
then the peer's SID is derived from the connection and checked again. The ACL is not redundant
with the check, nor the check with the ACL: an ACL stops the connection existing, and the check
is what a test can observe — a control that nothing verifies is one nobody notices losing.





**The daemon exits when nobody needs it.** A workspace daemon outliving every shell is
an orphan holding a store lock, and the user has no way to see it or reason about it. So
`RunAsync` returns — rather than looping forever — once the grace period passes with
no client attached.

| Member | Summary |
|---|---|
| `IpcServer(` | **(gap)** |
| `int ActiveConnections` | Connections currently attached. |
| `int ServedConnections` | Connections accepted over this server's life. Never decreases. |
| `long IdentityRefusals { get; private set; }` | Connections closed because the peer was not the workspace owner. |
| `long StalledConnections { get; private set; }` | Connections abandoned because the peer stopped reading its responses. |
| `Task RunAsync(CancellationToken cancellationToken)` | Serves until cancelled, or until the idle grace passes with no client. |

### `IpcServer(`

- **`expectedOwnerSid`** — The SID a peer must present. Defaults to this process's own user, which is the only correct value in production.

**Remarks.** **Why the owner is injectable at all.** The check that a peer's SID matches the
workspace owner cannot fire in a single-user test environment: the ACL admits only this user,
so every peer a test can produce is already the right one. A mutation run confirmed it — the
check could be deleted outright and nothing failed, which makes it an untested control, and
an untested control is not a control.





Varying the *expected* value tests the decision honestly without needing a second
user account: a server told to expect a different owner must refuse the connection it gets.
The alternative was to leave the branch permanently unexercised and say so in a comment,
which is how a security check quietly becomes decoration.

## `IWorkspaceCommands`

*interface* — `IWorkspaceCommands.cs`

The workspace's write surface, however it is reached.

**Remarks.** **Separate from `IWorkspaceQueries` because reads and writes are
not the same kind of thing.** A read can be repeated freely; a write bumps a generation and
commits a snapshot, carries an idempotency key, and is judged against the epoch fence. Folding
them into one interface would put a name on the seam ("queries") that half its members
contradict, and would make every read-only consumer hold a handle that can also mutate.





Both hosting modes satisfy it — the in-process core and the daemon client — for the same
reason the read seam exists: ADR-0009 keeps both, and a UI written against one of them is a UI
that has to be rewritten to get the other.

## `IndexSummary`

*record* — `IWorkspaceCommands.cs`

What an index run found, as the shell reports it.

| Member | Summary |
|---|---|
| `string Describe()` | One sentence for the announcement channel, including what was NOT seen. |
| `string NotAnalysed()` | What was not analysed, as ONE clause — a count and the sharpest example, never the list. |

### `string NotAnalysed()`

What was not analysed, as ONE clause — a count and the sharpest example, never the list.

**Remarks.** **The status line is a line.** This clause used to be every disclosure joined with
commas. Folding them by class took it from 108 to 28, which is a better list and still not a
status message: on a real index it filled roughly four fifths of the window and pushed the
graph into a strip along the top.





**Which one to name is the whole design.** A count alone ("28 boundaries") tells a
reader nothing about whether to care. So the clause names the disclosure with the largest
count, which is where the most unread repository is — and, because gaps sort before
boundaries when counts tie, prefers a thing the product MEANT to read and could not over a
thing it never intended to read (DC-050).





The full list is still in the result, unchanged, for a surface that can hold it.

## `LocalWorkspaceCommands`

*class* — `IWorkspaceCommands.cs`

The write surface applied by a core in this process.

**Remarks.** Takes the refresh as a delegate rather than a `WorkspaceCore` so that what the
in-process mode reports — a completed count, or a failure with its reason — is decided in one
place and testable without a store.

| Member | Summary |
|---|---|
| `Task<IndexSummary> IndexSolutionAsync(` | **(gap)** |
| `Task<ScopeRefreshStatus> RefreshScopeAsync(` | **(gap)** |

## `IWorkspaceDispatch`

*interface* — `IWorkspaceDispatch.cs`

The two durable phases of prompt dispatch, as a caller sees them.

**Remarks.** Separate from `IWorkspaceCommands` because it is a different obligation: a
workspace can answer projections and re-index without being able to record a dispatch, and a
shell that cannot dispatch should discover that by the capability being absent rather than by a
call failing.





**The side effect is deliberately not here.** Writing to the terminal is the shell's
job — it owns the process (D1) — so this interface covers only what must be durable, and
`BeginAndWriteAsync` is what orders the three steps.

## `ScopeRefreshState`

*enum* — `ScopeRefreshService.cs`

Where a scope refresh has got to.

## `ScopeRefreshStatus`

*record* — `ScopeRefreshService.cs`

What a caller learns about a refresh.

**Remarks.** `Failure` is populated on `Failed` and states why. A
refresh that failed silently would leave the last good snapshot rendering with nothing to say it
is now stale — which is the "clean empty success over rotting evidence" this product exists to
avoid.

## `RefreshMetrics`

*record* — `ScopeRefreshService.cs`

What every refresh so far has cost, and how often they happen.

**Remarks.** **This exists to answer a question a design decision is blocked on.**
`docs/notes/note-20260830-sub-scope-incrementality.md` weighs four ways to make re-indexing
incremental below the scope, and refuses to pick one, because the thing that decides it has never
been measured: whether re-indexing is an occasional on-demand cost or something a user waits on
constantly. Optimising a 1.2s operation that runs when asked is a different proposition from
optimising one that runs on every save.





**No rate is computed here.** "Refreshes per hour" from two samples is a number with no
error bar that will be quoted as if it had one. The raw facts — how many, first, last — let a
reader compute it when there is enough of it to mean something, and notice when there is not.

## `RefreshMetricsRequest`

*record* — `ScopeRefreshService.cs`

Asking the daemon what refreshing has cost so far.

## `RefreshRequest`

*record* — `ScopeRefreshService.cs`

Asking the daemon to re-index a scope.

## `RefreshStatusRequest`

*record* — `ScopeRefreshService.cs`

Asking how a previously started refresh is doing.

## `ScopeRefreshService`

*class* — `ScopeRefreshService.cs`

Re-indexing a scope, across the boundary.

**Remarks.** **Started and polled, never awaited on the wire.** A scope has a 60-second budget and
the IPC lane serves one request at a time per connection — so a refresh that answered only when
it finished would hold that connection for a minute, and the daemon's response-write timeout
would abandon it long before. The control lane carries *commands*; a command that starts
long work returns as soon as the work is started.





**The command id is the idempotency key**, exactly as the architecture's command
protocol specifies. Re-sending the same id returns the same job rather than starting a second
extraction — which matters most in the case it exists for: a client that did not see the reply
and retried. Two extractions of one scope would both bump the generation and the loser's work
would be discarded, having cost a full budget.





**Nothing here re-implements ingestion.** The generation fence, the incomplete-result
handling and the snapshot commit all stay in `WorkspaceCore`; this decides only what
crossing the boundary means.

| Member | Summary |
|---|---|
| `ScopeRefreshService(Func<string, string, CancellationToken, Task<int>> refresh)` | **(gap)** |
| `int TrackedJobs` | Jobs currently held, running or finished. |
| `ScopeRefreshStatus Start(string commandId, string scopeId, string artifactRevision)` | Starts a refresh, or returns the job this command id already started. |
| `ScopeRefreshStatus? Status(string commandId)` | How a refresh is doing, or `null` if this daemon has no record of it. |
| `RefreshMetrics Metrics()` | What refreshing has cost so far, on the normal path with no flag to remember. |
| `void Register(DaemonEndpoint endpoint)` | Registers refresh and its status query on the endpoint. |

### `ScopeRefreshService(Func<string, string, CancellationToken, Task<int>> refresh)`

- **`refresh`** — Runs the extraction and returns the assertion count. Injected rather than taking a `WorkspaceCore` so the boundary's behaviour — idempotency, retention, what a failure looks like — is testable without standing up a store and an extractor.

### `ScopeRefreshStatus? Status(string commandId)`

How a refresh is doing, or `null` if this daemon has no record of it.

**Remarks.** Null rather than a synthesised "unknown" state: a job this daemon never started, or one it
has since evicted, are both "I cannot tell you", and inventing a status would let a caller
wait for a result that is never coming.

## `Operations`

*class* — `ScopeRefreshService.cs`

The operation names, so both ends spell them the same way.

| Member | Summary |
|---|---|
| `string Refresh = "refresh"` | **(gap)** |
| `string RefreshStatus = "refresh.status"` | **(gap)** |
| `string RefreshMetrics = "refresh.metrics"` | **(gap)** |

## `DaemonUnavailableException`

*class* — `ShellBootstrap.cs`

Why a shell could not reach a daemon.

## `ShellBootstrap`

*class* — `ShellBootstrap.cs`

Gets the shell a daemon: reach the one that is running, or start one and wait for it.

**Remarks.** **Connect first, launch second, and that order is the whole design.** A workspace has at
most one daemon — enforced by `WorkspaceLock` — so launching first would mean the
second shell on a workspace starts a process whose only job is to discover it is redundant and
exit. Trying the pipe costs a few milliseconds and is right in the common case.





**Launching is racy on purpose, and safe because of the lock.** Two shells opening the
same workspace at the same instant will both fail to connect and both launch; one takes the
workspace lock and serves, the other exits with a stable code. Serialising that with a lock of
our own would put a second mechanism in front of the one that already decides this correctly.





**Failure is reported, never degraded into a silent fallback to in-process.** A shell
that quietly ran the core itself when the daemon would not start would work — and would have
abandoned the trust boundary, the workspace lock and the epoch fence without saying so. The
caller is told, and decides.

| Member | Summary |
|---|---|
| `Task<WorkspaceClient> ConnectOrLaunchAsync(` | Connects to the workspace's daemon, starting one if none answers. |

### `Task<WorkspaceClient> ConnectOrLaunchAsync(`

Connects to the workspace's daemon, starting one if none answers.

- **`workspacePath`** — The workspace root. Determines the pipe name and the lock.
- **`daemonExecutable`** — The daemon build to launch if none is running.
- **`dataDirectory`** — Where a launched daemon should keep this workspace's state. Null leaves it to the daemon's machine-wide default — which is right for the shell and wrong for anything that must not write into the user's profile, such as a test.

## `IpcRequestException`

*class* — `WorkspaceClient.cs`

Raised when the daemon refuses a request, carrying the boundary's stable code.

**Remarks.** An exception rather than a nullable result because these are not outcomes a caller chooses
between — a stale epoch, a revoked capability and an unsupported version are all "this request
did not happen, and you must decide what to do about it". The code is on the exception so a
caller can decide without parsing prose.

| Member | Summary |
|---|---|
| `string Code { get; } = code` | **(gap)** |

## `WorkspaceClient`

*class* — `WorkspaceClient.cs`

The core's read surface, over the boundary, in the same shapes the in-process caller uses.

**Remarks.** **The result types are the core's own** — `DescribeResult` and its
siblings — rather than a parallel set of wire types. A second definition of one result is two
things to keep in step, and the first divergence would appear as a field that is present in
process and missing across the pipe.





**The epoch is carried, not assumed.** Every request states which epoch it was authored
against, and the daemon rejects a mismatch. That is the fence that stops a command reasoning
about state that has since been replaced, and a client that quietly resent the daemon's current
epoch would defeat it while appearing to work.

| Member | Summary |
|---|---|
| `long Epoch` | The epoch this client is bound to. |
| `Task<WorkspaceClient> ConnectAsync(` | Connects, handshakes, and returns a client ready to query. |
| `Task<DescribeResult> DescribeAsync(` | **(gap)** |
| `Task<ImpactResult> ImpactAsync(` | **(gap)** |
| `Task<DispatchBeginResult> DispatchBeginAsync(` | Phase 1 of a dispatch, answered by the daemon that owns the store. |
| `Task<DispatchReceipt> DispatchFinalizeAsync(` | Phase 2 of a dispatch. Idempotent: a retried finalize returns the existing receipt. |
| `Task<RefreshMetrics> RefreshMetricsAsync(CancellationToken cancellationToken)` | What re-indexing has cost in this daemon so far. |
| `Task<WorkspaceOverview> OverviewAsync(OverviewQuery query, CancellationToken cancellationToken)` | **(gap)** |
| `Task<PathResult> PathsAsync(PathQuery query, CancellationToken cancellationToken)` | **(gap)** |
| `Task<WorkspaceGraph> GraphAsync(GraphQuery query, CancellationToken cancellationToken)` | **(gap)** |
| `Task<EvidencePage> EvidenceAsync(` | **(gap)** |
| `Task<FindResult> FindAsync(string term, int maxResults, CancellationToken cancellationToken)` | **(gap)** |
| `Task<ContentSearchResult> SearchContentAsync(` | **(gap)** |
| `Task<InteractionResult> InteractionAsync(` | **(gap)** |
| `Task<KnowledgeResult> KnowledgeAsync(` | **(gap)** |
| `Task<NodeContent> NodeContentAsync(string nodeId, CancellationToken cancellationToken)` | **(gap)** |
| `Task<ScopeRefreshStatus> RefreshScopeAsync(` | Asks the daemon to re-index a scope, and waits for it to finish. |
| `Task<IndexSummary> IndexSolutionAsync(` | Re-reads the daemon's epoch, for a caller recovering from a stale-epoch rejection. |
| `Task<long> EpochAsync(CancellationToken cancellationToken)` | **(gap)** |
| `Task<long> RefreshEpochAsync(CancellationToken cancellationToken)` | **(gap)** |
| `ValueTask DisposeAsync()` | **(gap)** |

### `Task<WorkspaceClient> ConnectAsync(`

Connects, handshakes, and returns a client ready to query.

**Remarks.** The epoch comes from the handshake rather than from the caller: the daemon owns the store and
is the only party that knows it. A caller-supplied epoch would be a guess, and the fence
exists precisely to catch guesses.

### `Task<RefreshMetrics> RefreshMetricsAsync(CancellationToken cancellationToken)`

What re-indexing has cost in this daemon so far.

**Remarks.** The measurement the sub-scope-incrementality decision is blocked on: whether a re-index is an
occasional cost a user asks for, or something they wait on constantly.

### `Task<ScopeRefreshStatus> RefreshScopeAsync(`

Asks the daemon to re-index a scope, and waits for it to finish.

**Remarks.** **Start-then-poll, because the wire cannot hold a 60-second operation.** The lane
serves one request at a time per connection, so a refresh that answered only on completion
would occupy that connection for the whole budget — and the daemon's response-write timeout
would abandon it first.





**One command id for the whole exchange.** It is the idempotency key: if the start
reply is lost and the caller retries, the daemon returns the job it already has rather than
extracting the scope twice.

## `DescribeRequest`

*record* — `WorkspaceOperations.cs`

The four read projections, as they travel across the boundary.

**Remarks.** Explicit request records rather than loose strings: the operation name and its arguments arrive
as one payload from a process we do not control, and a positional or free-form encoding would
have to be validated by hand at every call site.

## `ImpactRequest`

*record* — `WorkspaceOperations.cs`

*No doc comment on this type.* **(gap)**

## `FindRequest`

*record* — `WorkspaceOperations.cs`

*No doc comment on this type.* **(gap)**

## `SearchContentRequest`

*record* — `WorkspaceOperations.cs`

Ask for lines of workspace files containing a term.

## `InteractionRequest`

*record* — `WorkspaceOperations.cs`

Ask for one caller's outgoing calls in order.

## `EvidenceRequest`

*record* — `WorkspaceOperations.cs`

Asks for one page of every current assertion.

## `OverviewRequest`

*record* — `WorkspaceOperations.cs`

Asks for the workspace as groups rather than nodes.

## `PathsRequest`

*record* — `WorkspaceOperations.cs`

Asks how one node reaches another.

## `GraphRequest`

*record* — `WorkspaceOperations.cs`

Asks for the graph — all of it, or the part the filters name.

## `KnowledgeRequest`

*record* — `WorkspaceOperations.cs`

*No doc comment on this type.* **(gap)**

## `NodeContentRequest`

*record* — `WorkspaceOperations.cs`

One node, for the reader that selected it (ADR-0018 node-content-reader-contract).

## `DispatchBeginRequest`

*record* — `WorkspaceOperations.cs`

Phase 1 of a dispatch: make the attempt durable before any byte leaves the shell.

## `DispatchFinalizeRequest`

*record* — `WorkspaceOperations.cs`

Phase 2 of a dispatch: record the outcome the shell observed.

## `IndexSolutionRequest`

*record* — `WorkspaceOperations.cs`

Index every C# scope in the workspace.

**Remarks.** Additive with a default, so a client built before this field still decodes and still means "use
the cache" — which is the safe reading of an absent flag. It exists because an operator must
always be able to say "I do not believe the cache", and until it was reachable that sentence had
no button behind it.

## `DaemonOperations`

*class* — `WorkspaceOperations.cs`

Operations every daemon answers, whatever workspace it serves.

**Remarks.** Separate from the projections because they are about the *daemon* rather than the
workspace's contents: a shell needs them to establish that it is talking to a live peer and which
epoch its commands will be judged against, before it has anything to ask.

| Member | Summary |
|---|---|
| `string Ping = "ping"` | **(gap)** |
| `string Epoch = "epoch"` | **(gap)** |
| `void Register(DaemonEndpoint endpoint, Func<long> epoch)` | Registers them against a live epoch source. |

## `WorkspaceOperations`

*class* — `WorkspaceOperations.cs`

Puts the core's read surface behind the daemon endpoint.

**Remarks.** **This is what the process split was for.** Until now the boundary existed and almost
nothing crossed it: the daemon answered `ping` while the shell called the core in-process,
so the trust boundary was real and unused. Every day that persists, new code is written against
the in-process path and has to be moved later.





**Read projections only, and that is the whole surface today.** Dispatch — writing to a
terminal, staging a prompt — carries the two-phase receipt semantics of ADR-0010, and moving it
across is a separate piece of work with its own failure modes. Naming that here is better than
registering a handler that half-implements it.





**The projections are already bounded** (`ProjectionService` clamps every
limit and reports what it omitted), which is what makes them safe to expose to a caller who
chooses the numbers. Nothing here re-validates: doing so would create a second definition of the
bound, and two definitions of one quantity is a defect signature.

| Member | Summary |
|---|---|
| `string Describe = "describe"` | **(gap)** |
| `string Impact = "impact"` | **(gap)** |
| `string Find = "find"` | **(gap)** |
| `string SearchContent = "search-content"` | Lines in the workspace's own files that contain a term. |
| `string Interaction = "interaction"` | One caller's outgoing calls, in call order — a sequence diagram's feed. |
| `string Knowledge = "knowledge"` | **(gap)** |
| `string NodeContent = "nodeContent"` | One node's content, on demand (ADR-0018 node-content-reader-contract). |
| `string Evidence = "evidence"` | **(gap)** |
| `string Graph = "graph"` | **(gap)** |
| `string Paths = "paths"` | **(gap)** |
| `string Overview = "overview"` | **(gap)** |
| `string DispatchBegin = "dispatch.begin"` | **(gap)** |
| `string DispatchFinalize = "dispatch.finalize"` | **(gap)** |
| `string IndexSolution = "index.solution"` | **(gap)** |
| `JsonSerializerOptions Wire { get; } = new(JsonSerializerDefaults.Web)` | How every payload on this boundary is encoded. |
| `void Register(DaemonEndpoint endpoint, ProjectionService projections)` | Registers the read projections on . |
| `void RegisterDispatch(DaemonEndpoint endpoint, BoundaryDispatcher dispatcher)` | Registers the two durable phases of prompt dispatch (ADR-0010) on . |
| `void RegisterIndex(DaemonEndpoint endpoint, Func<string, bool, CancellationToken, Task<IndexSummary>> index)` | Turns a domain refusal into a stable error response instead of letting it escape.  Registers the workspace-wide C# index on . |

### `string SearchContent = "search-content"`

Lines in the workspace's own files that contain a term.

**Remarks.** Separate from `Find` because it answers a different question and costs a
different amount: Find reads the store, this opens files. A client should be able to offer
the cheap one on every keystroke and the expensive one on demand.

### `JsonSerializerOptions Wire { get; } = new(JsonSerializerDefaults.Web)`

How every payload on this boundary is encoded.

**Remarks.** **Enums travel as strings.** By number, adding a member in the middle of an enum silently
renumbers the ones after it — and with a dual-major handshake designed so an old shell may
meet a new daemon, that is a wire break with no error and no symptom except wrong answers.
A name costs a few bytes and cannot be renumbered.

### `void RegisterDispatch(DaemonEndpoint endpoint, BoundaryDispatcher dispatcher)`

Registers the two durable phases of prompt dispatch (ADR-0010) on .

**Remarks.** Separate from the projection registration because these are the first WRITES on the read
endpoint, and because a daemon can legitimately serve projections without them.

### `void RegisterIndex(DaemonEndpoint endpoint, Func<string, bool, CancellationToken, Task<IndexSummary>> index)`

Turns a domain refusal into a stable error response instead of letting it escape.

Registers the workspace-wide C# index on .

**Remarks.** **Found by a test, and it was worse than it looked.** A stale-epoch dispatch threw a
`WorkspaceStoreException` out of the handler, past `Handle{TRequest}`
— which deliberately guards only decoding — and out of the server's listen loop. One client
holding a stale epoch would have taken the daemon down for *every* shell attached to the
workspace.





**Why the distinction matters and is not a widening of the catch.** `Handle`'s rule
stands: a projection that throws is a defect in us and must not be swallowed. But a stale
epoch is not a defect — it is the expected answer when the core was replaced under a caller,
and the design requires it to come back as a stable denial code. Only
`WorkspaceStoreException` is mapped, because it is the type that carries one;
everything else still escapes.
