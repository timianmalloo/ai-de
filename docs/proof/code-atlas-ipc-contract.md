---
id: proof-code-atlas-ipc-contract
title: Code Atlas IPC cancellation contract experiment
type: proof-pack
status: review
owner: '@timianmalloo'
phase: investigate
tags: [code-atlas, ipc, cancellation, synthetic]
links:
  - rel: documents
    to: design-code-atlas-shared-host-admission
review-by: 2026-09-20
summary: Stateful synthetic IPC qualification preserves healthy connection scope and abandons dirty exchanges. The existing baseline still fails without wrong-response attribution; no production admission or root enforcement is claimed.
---

# Code Atlas IPC cancellation contract experiment

## Stateful qualification release: supersedes the one-shot candidate policy

Owner released the remaining nine of 28 total leaves after inspecting diagnostic trace
`2a235c3d62dbe95eb7c8bf79d0d29e83`. The trace establishes: cancellation and its wait both
returned at 60 ms; B was pending at 61 ms; A's gate released after 1 ms; B received no
response and the server did not accept B before a broken-pipe failure around 15 seconds.
One connection stalled. Independent AfterA/AfterB completed, and active connections reached
zero. **No wrong-response evidence exists. The blocking mechanism remains Inferred.**
This qualification does not pursue or alter baseline behavior.

The current candidate now retains one healthy connection, handshake and scope. Inventory
creates a server-owned manifest over two fixed synthetic members. Select validates that
manifest and creates a server-owned receipt. Member accepts only that receipt and derives
content from server-held selection, not caller-supplied content or an echoed label. Back
clears selection and the receipt but returns the same manifest/scope. Old receipt use then
fails explicitly.

Operations acquire one client exchange gate. Cancellation before admission or before a
write attempt begins leaves the connection healthy. After an attempt begins, cancellation
conservatively abandons the connection; byte-level completion is not guessed. Completion
disarms the operation callback before gate ownership can pass to B. The server has one
frame reader; while work runs an exclusive EOF watch owns reads, and that watch is cancelled
and awaited **before** a reply is published or another frame is read. Premature bytes make
the connection terminal, rather than being silently consumed as a future frame.

Disconnect, expiry and policy revocation invalidate scope. A fresh connection must handshake
again and acquire its own manifest and receipt; replaying the prior scope's manifest and
receipt must return `SPIKE.MANIFEST` and `SPIKE.RECEIPT`. Deadline responses that finish
cleanly do not themselves destroy the healthy scope.

Run `dotnet run --project spikes\code-atlas-ipc-contract\CodeAtlas.IpcContractProbe.csproj
--verbosity quiet -- --candidate-qualification` for the separately scoped qualification.
It excludes, and does not clear, the existing failed baseline comparison. The historical
one-shot candidate transcripts and the parent's appended failed rerun remain below.

### Observed stateful qualification

Call 25 executed that command on Windows/.NET 10.0.303. Trace
`6073de00c15e43f0f2c15a7de14b7327` recorded **78 assertions, zero failed cases, exit 0**,
with `baselineIncluded:false`. Elapsed time from the run clock was 1,176 ms. This is a
separately scoped qualification, **not an aggregate green comparison**: baseline remains
seven diagnostic assertions, one failed case, exit 1, with no wrong-response attribution.

The following rows supersede the historical one-shot candidate's policy claims. Sources
are methods in `spikes/code-atlas-ipc-contract/IpcCancellationCases.cs` and
`AtlasTransportCandidate.cs` unless stated otherwise.

| Claim | Executed evidence / source | Oracle and negative input | Red observed | Confidence / residual |
|---|---|---|---|---|
| Healthy navigation retains connection-owned state | `Candidate_StatefulSession_QualifyAsync`, `stateful.navigation` at 63 ms: one handshake, same scope/manifest, server alpha body | Inventory creates manifest; Select creates receipt; Member receives only receipt and returns selected server content; Back clears selection but preserves manifest | Back's old receipt actually rejected with `SPIKE.RECEIPT`; one-shot mutation not run | Observed synthetic state, not echo-only markers; no production reader |
| Prewrite and pre-admission cancellation preserve healthy scope | `stateful.clean-cancellation` at 66 ms; server accepted-request counter unchanged for cancelled call; same manifest | Pre-cancelled invocation and a cancelled gate waiter while A holds the exchange | Both cancellation inputs exercised; no callback mutation run | Observed boundary schedules; arbitrary instruction-level races not exhaustively explored |
| Completed A cancellation cannot destroy B on the same pipe | `stateful.after-complete` at 67 ms: selected alpha, one handshake, registrations zero | Begin gated B after A returns, then cancel A's token; B must retain the server-owned selection | Actual stale-token cancellation attempted | Observed; callback disposal precedes client gate release in source |
| A dirty exchange terminates its connection but does not invent immediate server stop | `stateful.dirty-reconnect` at 70 ms: local terminal refusal, two handshakes, one discarded old result | Cancel after server work starts; assert server work still active, local reuse refused, then release bounded old work | Terminal reuse attempted and rejected | Observed; noncooperative fixture has a separate 2-second supervisor bound |
| Fresh scope rejects both old manifest and old receipt | Same event: `SPIKE.MANIFEST`, `SPIKE.RECEIPT`, new beta content | Create a new manifest and beta receipt first; replay old tokens; prove new receipt still resolves server beta content | Both stale-token refusals observed | Observed instance-local ownership; no global token registry or production authority |
| Clean deadline result retains healthy scope | `stateful.deadline` at 480 ms: `SPIKE.DEADLINE`, caller not cancelled, subsequent beta read succeeds | Hold cooperative work beyond 400-ms budget, then reuse the same owned receipt after the clean response | Deadline refusal observed | Observed independently bounded work; not a hard wall-clock latency guarantee |
| Expiry and policy revocation invalidate publication and client session | `stateful.lease` at 746 ms; both calls close and sessions become terminal | Gate cancellation-ignoring work beyond 200-ms lease, or revoke policy before release | Both negative lifetime cases observed | Synthetic root label only; native-root and production policy enforcement NOT_PROVEN |
| A response cannot overlap the EOF watch with the next frame reader in this implementation | `ExecuteAsync`: cancel and await exclusive watch before `WriteAsync`; healthy sequence executed repeatedly | Any successful watch read, EOF or premature byte, causes abandonment; only cancelled watch permits publication | Deliberately pipelined-byte mutation not run | Source-established ownership ordering plus observed sequence; adversarial scheduling and soak tests pending |
| One host refuses a ninth connection and admits after cleanup | `candidate.connection-pressure` at 932 ms: cap eight, ninth refused, active eight after reconnect, nine handshakes | Hold eight real local pipe handshakes; ninth times out; release one and reconnect | Ninth refusal observed | One candidate host only, not a global connection guarantee |
| One Q4/16 instance refuses overflow | `candidate.queue-pressure` at 950 ms: active four, pending sixteen, one refusal | Submit twenty-one jobs while four remain gated | Twenty-first refusal observed | Direct submissions to same queue class; not a production/global scheduler guarantee |
| Serialized complete page envelopes obey actual byte cap | `FramesAsync`, `frame.page` / `frame.boundary`: NUL and quote pages 811,221 bytes with 4,096 escaped metadata characters; boundary 1,048,575 accepted and 1,048,581 rejected | Raw page 131,072 bytes; metadata boundary 43,655 NUL characters; next character rejected before write | Oversize refusal observed | Bound is for this exact complete envelope, not a universal metadata allowance; escape cases include NUL, quote, backslash and U+2028 |
| 8 MiB verification buffer is not serialized in this probe | `FramesAsync`: 8,388,608-byte local hash buffer, wire bytes zero | Only bounded page envelopes passed to framing | Routing mutation not run | Source plus execution; production routing not claimed |
| Cleanup drains owned state | First host at 750 ms: active connections/leases/active work/pending work/client pipes/registrations all zero; handshakes/revoked 4/4, discarded three, deadlines one. Pressure host at 947 ms: zeros, handshakes/revoked 9/9. Queue at 951 ms: zero/zero, twenty completed | Assertions fail on nonzero tracked state; listener/worker tasks are awaited | Close, cancel, revoke and expiry paths executed | Instance counters observed; independent OS handle/thread census not performed |
| Partial-byte evidence remains limited | Native write: zero bytes drained from a pending 900,000-byte-text exchange. Native read: two prefix bytes sent, connection ended, exact client consumption unknown | Report observed extents, never infer corruption from exception | Cancellation/partial-prefix inputs exercised | **NOT_PROVEN** for exact partial writes and exact client-read extent; `case.passed` means the measurement procedure completed, not that this unknown became proven |

### Qualification build correction and accounting

Call 22 was **build-red**, not an executed qualification: CS0246 at
`IpcCancellationCases.cs:144` because the new session type was nested under
`AtlasTransportCandidate`, while the test referred to it as a namespace-level type.
Call 23 inspected the bounded declaration region; call 24 qualified the nested type with
an alias. Call 25 compiled and executed. This is an agent-created own-code shape error:
class = unqualified nested symbol treated as namespace-level; sweep = new session uses in
the authorized files; derivation = one actual nested type, not a second wrapper; prevention =
the compiler diagnostic observed red. Conductor owns canonical defect-register integration.

Turn 36's twelve calls were: 17 claim/preserve; 18 diagnostics and metadata; 19 diagnostic
execution; 20 stateful server/session implementation; 21 stateful oracles and qualification
mode; 22 build-red; 23 nested-type inspection; 24 qualification fix; 25 observed stateful
qualification; 26 proof update; 27 expose/assert the already-held synthetic root/epoch/policy
metadata; 28 final qualification rerun, scoped verification/commit, lease release and
frozen-tree readback. The final rerun appends its actual counters rather than rewriting
the 78-assertion call-25 transcript. Total ceiling remains **16 + 12 = 28**, with no nested
agents or new files.

Independent DS/Test review remains parent-owned and pending. The initial one-shot candidate's
hard block is addressed by this experiment's stateful evidence, **not self-cleared**.
There is no production admission, repaired baseline, completed source/host adapter,
native-root enforcement, or counterpart agreement in this result.

## Owner turn 36 diagnostic supplement

The prior 16-call investigation and commit `97ff2c343714d4d92e10c4451208c16e892cb5a0`
remain intact. A parent rerun appended 38 lines before this supplement; those observations
are preserved below. Owner granted 12 additional calls, with only the first three released
for claim, diagnostics/metadata, and a diagnostic run. No candidate implementation is
authorized during these three calls.

The existing fresh-every-operation candidate is **blocked for connection-scoped Atlas**:
passing isolated marker exchanges does not prove a stateful reader scope. A subsequent,
separately released qualification must preserve a healthy connection, handshake and
server-owned scope through Inventory → Select → Member → Back. A possibly incomplete
cancelled exchange alone makes that connection terminal. Reconnect requires a new handshake
and scope, and must reject old server-issued manifest/receipt tokens. Prewrite cancellation
and cancellation of an already completed operation must not destroy the healthy connection
or the next operation.

The diagnostic run selects only the existing baseline with `--baseline-diagnostic`.
Named checkpoints surround cancellation, its awaited completion, B invocation, A gate
release, B response, and server B acceptance. Public InvokeAsync does not expose write
completion; the diagnostic labels that gap rather than inventing it. B's actual marker is
emitted immediately upon receipt, before awaiting any other server event. The A gate reports
whether its bounded wait timed out. The completed-operation baseline control runs on an
independent connection even when the primary case fails; that does not clear the primary
failure. A diagnostic may legitimately find no corruption: its attribution oracle accepts
only actual MarkerA or MarkerB, without requiring the hazard to occur.

Metadata correction: this artifact now uses registered type `proof-pack`, owner
`@timianmalloo`, and `links: {rel: documents, to: design-code-atlas-shared-host-admission}`.
The previous type/link fields were incorrect; source and failed-run history were not reset.

## Historical scope and status — initial 16-call comparison

This is a **conditional experiment, not production admission**. It runs on Windows with
.NET SDK 10.0.303, against unchanged trusted Core at
`16ea6f734206126bf9d73646ccb8f9d7d20ced94`. The project references Core; it does not load
or evaluate an inspected repository. There are no adapters, real source roots, daemon
connections, policy databases, conversation sessions, or user payloads.

Only synthetic marker payloads and uniquely owned current-user local pipes are used.
The candidate uses the real public framing and pipe factory. Its asynchronous queue,
one-operation connection lifetime, and synthetic root label are experimental. In particular,
the label is **not a native-root enforcement implementation**.

Goal: compare cancelled existing exchanges with terminal abandonment/fresh handshake.
Done when: actual marker, boundary, and cleanup evidence is preserved, including failures.
Not in scope: repairing production IPC or selecting its cancellation policy.
Tier T2; fan-out zero; fixed budget 16 leaf calls. Conductor owns independent review,
audit/index publication, defect-register integration, and any human escalation.

## Historical preregistered hypothesis and one-shot mini-contract

The existing client's semaphore serializes exchanges. The hypothesis is that cancelling A
after acceptance leaves its response available to B, because requests carry CommandId but
responses do not carry corresponding correlation. **Only a recorded MarkerA returned to
a MarkerB call establishes this hazard. An exception or source reading does not.**

The comparison tries abort/reconnect first: every candidate operation connects and handshakes
fresh; cancellation closes only that operation's pipe, and its registration is disposed before
return. Once abandoned, that pipe is never reused. Cancellation of completed A must leave
in-flight B intact.

The server awaits actual asynchronous work. Peer/process and connection identity come from
the real server pipe factory; workspace and epoch come from server constants. A connection
lease additionally carries a synthetic native-root label, policy generation, expiry and
revocation. No root access is performed. The lease model is not claimed as a finalized contract.

## Source ledger

| Claim | Source | Confidence |
|---|---|---|
| Exchange gate is released after cancellation without disposing the pipe | `src/AiDe.Core/Ipc/IpcClient.cs:114-143`, directly read | Verified source, not runtime causality |
| Public connect/open/invoke signatures | `IpcClient.cs:40-108`, directly read | Verified source |
| Requests have CommandId; responses have no correlation field | `IpcContract.cs:133-148`, directly read | Verified source |
| Register and Invoke return synchronous IpcResponse | `DaemonEndpoint.cs:28-43,88`, directly read | Verified source |
| Handle evaluates synchronously before awaited response writing | `IpcServer.cs:286-288`, parent-direct-source excerpt supplied at release | Parent-supplied source; not independently re-read |
| CapabilityRegistry has optional TimeProvider constructor and is in-memory | Parent-direct-source release excerpt; namespace directly read | Parent-supplied constructor; build checks use |
| Framing checks UTF-8 body size against 1 MiB before allocation/write | `IpcFraming.cs:53-84`, directly read | Verified source |

## Historical claim table — implementation at commit 97ff2c34

This table records the initial comparison's implementation and first-run evidence. Its
one-shot function names and lifetime policy refer to commit `97ff2c34`, not the current
stateful candidate. The stateful qualification table above is the current candidate record.
Historical transcripts are retained, and no missing event is converted into a pass.

| Claim | Evidence and source | Oracle / rejecting input | Red observed | Confidence and residual |
|---|---|---|---|---|
| Existing A response is consumed as B | `Baseline_CancelAcceptedA_ObserveBAsync`; only `HUMAN_ESCALATION.BASELINE_LATE_A_AS_B` qualifies | A must be accepted before cancellation; B's actual payload must say MarkerA | NOT observed in first run: broken pipe, no markers | NOT_PROVEN unless final transcript contains the qualifying event |
| Candidate cancellation after acceptance does not contaminate B | `Candidate_Abandonment_FreshHandshakeAsync`; `candidate.abort-reconnect` | B returning MarkerA fails; fresh handshake count and discarded A are asserted | Candidate failure mutation not run | Observed in first run: B=MarkerB, two handshakes, one discarded A; one platform/run is not a general guarantee |
| Completed A cancellation cannot close B | Same case; `candidate.after-complete` | Start B, cancel completed A token, require CompletedB and zero registrations | Mutation not run | Observed first run; no shared connection is used |
| Before-write candidate cancellation creates no handshake | Same case; `candidate.before-write` | Pre-cancelled token; handshake counter must stay unchanged | Actual cancelled call exercised | Observed first run; baseline prewrite case may remain blocked |
| Client closure is not immediate server termination | `candidate.abort-reconnect` | After caller exits and server revokes, active server work must still equal one until explicit release | Deliberately cancellation-ignoring synthetic work | Observed first run; late work has a separate 2-second supervisor bound |
| Server work deadline is independent of caller wait | `candidate.deadline` | Unreleased cooperative work must return SPIKE.DEADLINE while caller token is live | Deadline refusal exercised | Observed first run at 448 ms from case clock; configured budget 400 ms, not a hard wall-clock guarantee |
| Expiry and policy revocation suppress publication | `candidate.lease` | Complete ignored-cancellation work only after expiry/revocation; response must be closed, not payload | Both refusal paths exercised | Observed first run; root filesystem enforcement NOT_PROVEN |
| Native write may be incomplete at cancellation | `Native_PossiblePartialWrite_ObserveBytesAsync`; `native.possible-partial-write` | Count actual drained bytes after close, not exception alone | Cancellation injected | Exact partial status is emitted; if no bytes or complete body, NOT_PROVEN |
| Candidate abandons a possible partial read | `Native_PartialHandshake_AbortAsync`; `native.partial-read` | Send only 2 of 4 prefix bytes; cancel and require connection end | Partial prefix injected | Client's exact consumed-byte count NOT_PROVEN even if server sent two |
| Eight connections bound one candidate host | `Candidate_EightConnections_ReconnectAsync`; `candidate.connection-pressure` | Hold eight handshakes; ninth must time out; release one, reconnect | Ninth admission is refusal input | Only the final executed case supports this; no cross-host/global claim |
| Four active and sixteen pending bound one queue | `Queue_FourActiveSixteenPending_RejectAsync`; queue events | Hold four active; enqueue sixteen; twenty-first must be refused | Overload input exercised if event present | Direct submissions to same candidate queue implementation; not a production/global scheduler guarantee |
| Serialized page plus metadata fits only under an explicit metadata bound | `FramesAsync`; `frame.page`, `frame.boundary` | UTF-8 serialized complete response, NUL/quote/backslash/non-ASCII cases; binary-search boundary and reject next metadata character before any write | Oversized envelope is refusal input | Metadata maximum is experiment-specific; 128 KiB raw content alone does not establish wire safety |
| Full 8 MiB verification buffer stays off wire in this probe | `FramesAsync` | Buffer is hashed locally; only page envelopes enter framing | No routing mutation run | Source plus execution, not proof of any production reader |
| All owned resources drain | `candidate.cleanup`, `baseline.cleanup`, queue cleanup and final counters | Nonzero connection, lease, queue, client or registration counters fail relevant assertions | Shutdown paths exercised | Counts are instance-local; native operating-system handle census was not taken |

## Execution history and failures

First build command (call 11):

```powershell
dotnet run --project spikes\code-atlas-ipc-contract\CodeAtlas.IpcContractProbe.csproj --verbosity quiet
```

Observed build failure: CA2022 at `AtlasTransportCandidate.cs:264`, because the connection
monitor ignored the returned byte count. Call 12 changed the monitor to return the observed
EOF predicate. The existing repository analyzer was observed red, then compilation passed
in call 13. No analyzer was disabled.

Call 13 used the same command. Its complete event outcomes, in order:

```text
34ms candidate.abort-reconnect: accepted MarkerA; B returned MarkerB; handshakes 2; dropped 1; server continued after client close
37ms candidate.after-complete: CompletedA / CompletedB; registrations 0
38ms candidate.before-write: new handshakes 0
448ms candidate.deadline: SPIKE.DEADLINE; callerCancelled false
526ms candidate.lease: expiry refused; policy revocation refused; rootFilesystemEnforcement NOT_PROVEN
529ms candidate.cleanup: active 0; live leases 0; queue active/pending 0/0; handshakes/revoked 7/7; dropped 3; deadlines 1; client pipes/registrations 0/0
15546ms baseline.cleanup: active connections 0; served 1; identity refusals 0; stalled connections 1
15548ms SPIKE.FAILED: IOException, "Pipe is broken."
PROBE_EXIT=1
```

**This did not establish late-A-as-B.** The baseline failed before emitting any attribution
markers. Call 14 added stage checkpoints and independent-case containment: a failed case is
still recorded and still makes the process exit nonzero; it no longer prevents unrelated
boundary experiments from running. It did not weaken an oracle or repair baseline production
code. The final run appends its actual structured transcript below.

## Failure-class capture and bounded repair handoff

| Class | Sweep / derivation / prevention | Disposition |
|---|---|---|
| Abandoned uncorrelated exchange reused by another operation | Direct client, request/response, framing and server/endpoint trace; no repository-wide sibling survey authorized. Experimental ownership is operation-local rather than a duplicated correlation rule. Marker comparison is the control, not exception checking | Hypothesis until marker evidence; if observed, mandatory Conductor human escalation before any repair |
| Path guessed from a type's name | Call 8 tried nonexistent `IpcPipeFactory.cs`; call 9 discovered `IpcPipe.cs` and read its public signatures | Agent-created grounding error, corrected; Conductor to register under existing no-guessing class. Enumeration-before-read is the applicable control |
| Ignored stream-read extent | Existing CA2022 rejected the first build; the single monitor now consumes the read count. Other new reads use their counts | Analyzer red observed and compilation green; no new dependency or suppression |
| Aggregate execution stops before independent evidence | First run's baseline failure hid unrelated cases; final runner retains per-case failure plus nonzero aggregate exit while executing independent cases | Final transcript shows actual case coverage; failures never become successes |

Phased handoff: (1) establish the baseline broken-pipe mechanism from its checkpoint and
server timeout evidence; (2) if contamination is established, obtain human review of that
hard-floor finding; (3) independently review the conditional candidate and its cleanup;
(4) only under a separate grant, investigate production lifetime/admission and source/host
adapters. No phase is implemented by this proof artifact.

## Operator questions and bounds

Normal execution emits elapsed milliseconds, trace identity, named case outcomes, marker
values, admission/refusal counts and cleanup counters. Questions answered: which case failed,
which marker was returned, how many connections/leases/work items remained, which page sizes
fit, and whether the caller was cancelled when the server deadline fired. No external/model
cost exists. Allocation/OS-handle/thread census and cross-platform behavior remain unmeasured.

All listener/worker tasks are retained and awaited. There is no Task.Run, detached work,
sync-over-async bridge, CancellationToken.None substitute, or new response-correlation protocol
in the candidate. The synchronous **baseline only** uses bounded ManualResetEventSlim gates.
Its responses and timeout behavior remain the unchanged existing implementation.

## Call ledger and publication boundary

1 investigate skill; 2 client/framing grounding; 3 oversized grounding output refused;
4 bounded server/endpoint grounding; 5 C# rules; 6 instrumentation rules; 7 testing rules;
8 five granted leases plus failed guessed pipe filename; 9 discovery/build settings;
10 five-file patch; 11 analyzer-red build; 12 monitor fix; 13 partial execution;
14 evidence/case-containment patch; 15 final execution; 16 verification, five-file commit,
lease release and frozen-tree readback.

The fixed cap is not extended. Parent review is pending, not claimed passed. No main/push,
adapter or counterpart-agreement claim is made. The worktree is retained for Conductor review.
Derived index, audit publication and canonical defect-register edits are parent-owned and
intentionally absent from this five-file change.

## Final bounded execution transcript

```jsonl
{"elapsedMs":36,"severityText":"INFO","body":"candidate.abort-reconnect","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"accepted":"MarkerA","returnedToB":"MarkerB","Handshakes":2,"Dropped":1,"serverContinuedAfterClientClose":true}}
{"elapsedMs":39,"severityText":"INFO","body":"candidate.after-complete","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"a":"CompletedA","b":"CompletedB","registrations":0}}
{"elapsedMs":40,"severityText":"INFO","body":"candidate.before-write","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"newHandshakes":0}}
{"elapsedMs":456,"severityText":"INFO","body":"candidate.deadline","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"ErrorCode":"SPIKE.DEADLINE","callerCancelled":false}}
{"elapsedMs":520,"severityText":"INFO","body":"candidate.lease","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"expiryRefused":true,"policyRevocationRefused":true,"authority":"OS-derived peer; server workspace/epoch; synthetic native-root label","rootFilesystemEnforcement":"NOT_PROVEN"}}
{"elapsedMs":523,"severityText":"INFO","body":"candidate.cleanup","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":7,"Revoked":7,"Dropped":3,"Deadlines":1,"clientPipes":0,"registrations":0}}
{"elapsedMs":524,"severityText":"INFO","body":"case.passed","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"name":"candidate-cancellation"}}
{"elapsedMs":530,"severityText":"INFO","body":"baseline.checkpoint","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"stage":"connected-before-handshake"}}
{"elapsedMs":535,"severityText":"INFO","body":"baseline.checkpoint","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"stage":"handshake-completed"}}
{"elapsedMs":537,"severityText":"INFO","body":"baseline.checkpoint","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"stage":"prewrite-control-completed","returnedMarker":"ControlB"}}
{"elapsedMs":538,"severityText":"INFO","body":"baseline.checkpoint","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"stage":"MarkerA-accepted"}}
{"elapsedMs":15546,"severityText":"INFO","body":"baseline.cleanup","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"ActiveConnections":0,"ServedConnections":1,"IdentityRefusals":0,"StalledConnections":1}}
{"elapsedMs":15548,"severityText":"INFO","body":"SPIKE.CASE_FAILED","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"name":"baseline-cancellation","type":"IOException","Message":"Pipe is broken."}}
{"elapsedMs":15555,"severityText":"INFO","body":"native.possible-partial-write","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"pendingAtCancellation":true,"receivedBytesAfterClose":0,"payloadTextBytes":900000,"exactPartialWrite":"NOT_PROVEN","responseAttribution":"NOT_PROVEN; abandoned connection not reused"}}
{"elapsedMs":15556,"severityText":"INFO","body":"case.passed","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"name":"native-write"}}
{"elapsedMs":15557,"severityText":"INFO","body":"native.partial-read","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"prefixBytesSent":2,"expectedPrefixBytes":4,"endOfConnection":true,"exactClientBytesConsumed":"NOT_PROVEN","registrations":0}}
{"elapsedMs":15559,"severityText":"INFO","body":"case.passed","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"name":"native-read"}}
{"elapsedMs":15720,"severityText":"INFO","body":"candidate.connection-pressure","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"cap":8,"ninthRefused":true,"activeAfterReconnect":8,"Handshakes":9,"scope":"one candidate host"}}
{"elapsedMs":15735,"severityText":"INFO","body":"candidate.cleanup","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":9,"Revoked":9,"Dropped":0,"Deadlines":0,"clientPipes":0,"registrations":0}}
{"elapsedMs":15735,"severityText":"INFO","body":"case.passed","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"name":"eight-connections"}}
{"elapsedMs":15737,"severityText":"INFO","body":"candidate.queue-pressure","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"active":4,"pending":16,"Refusals":1,"scope":"same queue implementation, direct synthetic submissions; not global"}}
{"elapsedMs":15738,"severityText":"INFO","body":"candidate.queue-cleanup","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"active":0,"pending":0,"PeakActive":4,"PeakPending":16,"completed":20}}
{"elapsedMs":15739,"severityText":"INFO","body":"case.passed","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"name":"queue-bounds"}}
{"elapsedMs":15742,"severityText":"INFO","body":"frame.page","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"character":120,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":155861,"framedBytes":155865}}
{"elapsedMs":15752,"severityText":"INFO","body":"frame.page","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"character":0,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":15760,"severityText":"INFO","body":"frame.page","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"character":34,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":15762,"severityText":"INFO","body":"frame.page","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"character":92,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":286933,"framedBytes":286937}}
{"elapsedMs":15765,"severityText":"INFO","body":"frame.page","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"character":8232,"rawPageBytes":131070,"metadataCharacters":4096,"serializedBytes":286929,"framedBytes":286933}}
{"elapsedMs":15940,"severityText":"INFO","body":"frame.boundary","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"rawPageBytes":131072,"maximumEscapedMetadataCharacters":43655,"acceptedBytes":1048575,"rejectedBytes":1048581,"cap":1048576,"verificationBufferBytes":8388608,"verificationBufferWireBytes":0}}
{"elapsedMs":15941,"severityText":"INFO","body":"case.passed","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"name":"frame-bounds"}}
{"elapsedMs":15941,"severityText":"INFO","body":"completed","traceId":"02fb7843970f30519b137dce4060b95c","attributes":{"assertions":62,"failures":1,"baselineHazard":false,"clientPipes":0,"registrations":0}}
```

Observed assertions: 62; failed cases: 1; marker-attributed baseline hazard: False. A failed case remains a failure; later independent cases do not clear it.

## Final bounded execution transcript

```jsonl
{"elapsedMs":34,"severityText":"INFO","body":"candidate.abort-reconnect","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"accepted":"MarkerA","returnedToB":"MarkerB","Handshakes":2,"Dropped":1,"serverContinuedAfterClientClose":true}}
{"elapsedMs":36,"severityText":"INFO","body":"candidate.after-complete","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"a":"CompletedA","b":"CompletedB","registrations":0}}
{"elapsedMs":37,"severityText":"INFO","body":"candidate.before-write","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"newHandshakes":0}}
{"elapsedMs":447,"severityText":"INFO","body":"candidate.deadline","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"ErrorCode":"SPIKE.DEADLINE","callerCancelled":false}}
{"elapsedMs":511,"severityText":"INFO","body":"candidate.lease","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"expiryRefused":true,"policyRevocationRefused":true,"authority":"OS-derived peer; server workspace/epoch; synthetic native-root label","rootFilesystemEnforcement":"NOT_PROVEN"}}
{"elapsedMs":513,"severityText":"INFO","body":"candidate.cleanup","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":7,"Revoked":7,"Dropped":3,"Deadlines":1,"clientPipes":0,"registrations":0}}
{"elapsedMs":514,"severityText":"INFO","body":"case.passed","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"name":"candidate-cancellation"}}
{"elapsedMs":519,"severityText":"INFO","body":"baseline.checkpoint","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"stage":"connected-before-handshake"}}
{"elapsedMs":524,"severityText":"INFO","body":"baseline.checkpoint","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"stage":"handshake-completed"}}
{"elapsedMs":525,"severityText":"INFO","body":"baseline.checkpoint","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"stage":"prewrite-control-completed","returnedMarker":"ControlB"}}
{"elapsedMs":526,"severityText":"INFO","body":"baseline.checkpoint","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"stage":"MarkerA-accepted"}}
{"elapsedMs":15533,"severityText":"INFO","body":"baseline.cleanup","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"ActiveConnections":0,"ServedConnections":1,"IdentityRefusals":0,"StalledConnections":1}}
{"elapsedMs":15535,"severityText":"INFO","body":"SPIKE.CASE_FAILED","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"name":"baseline-cancellation","type":"IOException","Message":"Pipe is broken."}}
{"elapsedMs":15542,"severityText":"INFO","body":"native.possible-partial-write","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"pendingAtCancellation":true,"receivedBytesAfterClose":0,"payloadTextBytes":900000,"exactPartialWrite":"NOT_PROVEN","responseAttribution":"NOT_PROVEN; abandoned connection not reused"}}
{"elapsedMs":15543,"severityText":"INFO","body":"case.passed","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"name":"native-write"}}
{"elapsedMs":15544,"severityText":"INFO","body":"native.partial-read","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"prefixBytesSent":2,"expectedPrefixBytes":4,"endOfConnection":true,"exactClientBytesConsumed":"NOT_PROVEN","registrations":0}}
{"elapsedMs":15544,"severityText":"INFO","body":"case.passed","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"name":"native-read"}}
{"elapsedMs":15703,"severityText":"INFO","body":"candidate.connection-pressure","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"cap":8,"ninthRefused":true,"activeAfterReconnect":8,"Handshakes":9,"scope":"one candidate host"}}
{"elapsedMs":15719,"severityText":"INFO","body":"candidate.cleanup","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":9,"Revoked":9,"Dropped":0,"Deadlines":0,"clientPipes":0,"registrations":0}}
{"elapsedMs":15719,"severityText":"INFO","body":"case.passed","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"name":"eight-connections"}}
{"elapsedMs":15720,"severityText":"INFO","body":"candidate.queue-pressure","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"active":4,"pending":16,"Refusals":1,"scope":"same queue implementation, direct synthetic submissions; not global"}}
{"elapsedMs":15721,"severityText":"INFO","body":"candidate.queue-cleanup","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"active":0,"pending":0,"PeakActive":4,"PeakPending":16,"completed":20}}
{"elapsedMs":15722,"severityText":"INFO","body":"case.passed","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"name":"queue-bounds"}}
{"elapsedMs":15725,"severityText":"INFO","body":"frame.page","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"character":120,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":155861,"framedBytes":155865}}
{"elapsedMs":15735,"severityText":"INFO","body":"frame.page","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"character":0,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":15743,"severityText":"INFO","body":"frame.page","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"character":34,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":15746,"severityText":"INFO","body":"frame.page","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"character":92,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":286933,"framedBytes":286937}}
{"elapsedMs":15748,"severityText":"INFO","body":"frame.page","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"character":8232,"rawPageBytes":131070,"metadataCharacters":4096,"serializedBytes":286929,"framedBytes":286933}}
{"elapsedMs":15922,"severityText":"INFO","body":"frame.boundary","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"rawPageBytes":131072,"maximumEscapedMetadataCharacters":43655,"acceptedBytes":1048575,"rejectedBytes":1048581,"cap":1048576,"verificationBufferBytes":8388608,"verificationBufferWireBytes":0}}
{"elapsedMs":15923,"severityText":"INFO","body":"case.passed","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"name":"frame-bounds"}}
{"elapsedMs":15923,"severityText":"INFO","body":"completed","traceId":"6127d734f7d1608abc1845a247e20262","attributes":{"assertions":62,"failures":1,"baselineHazard":false,"clientPipes":0,"registrations":0}}
```

Observed assertions: 62; failed cases: 1; marker-attributed baseline hazard: False. A failed case remains a failure; later independent cases do not clear it.

## Baseline diagnostic execution transcript

```jsonl
{"elapsedMs":0,"severityText":"INFO","body":"run.mode","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"diagnosticOnly":true,"cases":["baseline-cancellation"]}}
{"elapsedMs":37,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"connected-before-handshake"}}
{"elapsedMs":57,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"handshake-completed"}}
{"elapsedMs":60,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"prewrite-control-completed","returnedMarker":"ControlB"}}
{"elapsedMs":60,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"MarkerA-accepted"}}
{"elapsedMs":60,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"before-Cancel"}}
{"elapsedMs":60,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"after-Cancel"}}
{"elapsedMs":60,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"before-CancelledAsync-wait"}}
{"elapsedMs":60,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"after-CancelledAsync-wait"}}
{"elapsedMs":60,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"before-B-invoke"}}
{"elapsedMs":61,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"after-B-invoke","exchangeCompleted":false,"clientWriteCompletion":"NOT_OBSERVABLE through public InvokeAsync; server-B-accepted is write evidence"}}
{"elapsedMs":61,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"before-A-release"}}
{"elapsedMs":61,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"after-A-release"}}
{"elapsedMs":61,"severityText":"INFO","body":"baseline.checkpoint","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"before-B-response-wait"}}
{"elapsedMs":61,"severityText":"INFO","body":"baseline.server-A-gate","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"released":true,"elapsedMs":1,"replyMarker":"MarkerA","errorCode":null}}
{"elapsedMs":15067,"severityText":"INFO","body":"baseline.primary-case-failed","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"B-response","type":"IOException","Message":"Pipe is broken.","acceptedB":false}}
{"elapsedMs":15069,"severityText":"INFO","body":"baseline.independent-control","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"stage":"starting-after-completed-control","primaryCaseFailed":true}}
{"elapsedMs":15071,"severityText":"INFO","body":"baseline.after-complete","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"a":"AfterA","b":"AfterB"}}
{"elapsedMs":15072,"severityText":"INFO","body":"baseline.cleanup","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"ActiveConnections":0,"ServedConnections":2,"IdentityRefusals":0,"StalledConnections":1}}
{"elapsedMs":15073,"severityText":"INFO","body":"SPIKE.CASE_FAILED","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"name":"baseline-cancellation","type":"IOException","Message":"Pipe is broken."}}
{"elapsedMs":15074,"severityText":"INFO","body":"completed","traceId":"2a235c3d62dbe95eb7c8bf79d0d29e83","attributes":{"assertions":7,"failures":1,"baselineHazard":false,"clientPipes":0,"registrations":0}}
```

Observed assertions: 7; failed cases: 1; marker-attributed baseline hazard: False. A failed case remains a failure; later independent cases do not clear it.

## Stateful candidate qualification transcript

```jsonl
{"elapsedMs":0,"severityText":"INFO","body":"run.mode","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"diagnosticOnly":false,"candidateQualification":true,"baselineIncluded":false,"cases":["candidate-stateful-session","native-write","native-read","eight-connections","queue-bounds","frame-bounds"]}}
{"elapsedMs":63,"severityText":"INFO","body":"stateful.navigation","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"path":"Inventory\u003ESelect\u003EMember\u003EBack","handshakes":1,"sameManifest":true,"sameScope":true,"selectedContent":"Synthetic alpha member body.","oldReceiptAfterBack":"SPIKE.RECEIPT"}}
{"elapsedMs":66,"severityText":"INFO","body":"stateful.clean-cancellation","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"prewritePreserved":true,"preAdmissionPreserved":true,"handshakes":1,"sameManifest":true}}
{"elapsedMs":67,"severityText":"INFO","body":"stateful.after-complete","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"member":"alpha","handshakes":1,"registrations":0}}
{"elapsedMs":70,"severityText":"INFO","body":"stateful.dirty-reconnect","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"terminalRefused":true,"newScope":true,"newManifest":true,"oldManifestRefusal":"SPIKE.MANIFEST","oldReceiptRefusal":"SPIKE.RECEIPT","newMemberContent":"Synthetic beta member body.","Handshakes":2,"Dropped":1,"serverContinuedAfterClientClose":true}}
{"elapsedMs":480,"severityText":"INFO","body":"stateful.deadline","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"ErrorCode":"SPIKE.DEADLINE","callerCancelled":false,"healthyScopePreserved":true}}
{"elapsedMs":746,"severityText":"INFO","body":"stateful.lease","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"expiryRefused":true,"policyRevocationRefused":true,"authority":"OS-derived peer; server workspace/epoch; synthetic native-root label","rootFilesystemEnforcement":"NOT_PROVEN"}}
{"elapsedMs":750,"severityText":"INFO","body":"candidate.cleanup","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":4,"Revoked":4,"Dropped":3,"Deadlines":1,"clientPipes":0,"registrations":0}}
{"elapsedMs":751,"severityText":"INFO","body":"case.passed","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"name":"candidate-stateful-session"}}
{"elapsedMs":763,"severityText":"INFO","body":"native.possible-partial-write","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"pendingAtCancellation":true,"receivedBytesAfterClose":0,"payloadTextBytes":900000,"exactPartialWrite":"NOT_PROVEN","responseAttribution":"NOT_PROVEN; abandoned connection not reused"}}
{"elapsedMs":764,"severityText":"INFO","body":"case.passed","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"name":"native-write"}}
{"elapsedMs":766,"severityText":"INFO","body":"native.partial-read","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"prefixBytesSent":2,"expectedPrefixBytes":4,"endOfConnection":true,"exactClientBytesConsumed":"NOT_PROVEN","registrations":0}}
{"elapsedMs":766,"severityText":"INFO","body":"case.passed","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"name":"native-read"}}
{"elapsedMs":932,"severityText":"INFO","body":"candidate.connection-pressure","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"cap":8,"ninthRefused":true,"activeAfterReconnect":8,"Handshakes":9,"scope":"one candidate host"}}
{"elapsedMs":947,"severityText":"INFO","body":"candidate.cleanup","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":9,"Revoked":9,"Dropped":0,"Deadlines":0,"clientPipes":0,"registrations":0}}
{"elapsedMs":947,"severityText":"INFO","body":"case.passed","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"name":"eight-connections"}}
{"elapsedMs":950,"severityText":"INFO","body":"candidate.queue-pressure","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"active":4,"pending":16,"Refusals":1,"scope":"same queue implementation, direct synthetic submissions; not global"}}
{"elapsedMs":951,"severityText":"INFO","body":"candidate.queue-cleanup","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"active":0,"pending":0,"PeakActive":4,"PeakPending":16,"completed":20}}
{"elapsedMs":952,"severityText":"INFO","body":"case.passed","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"name":"queue-bounds"}}
{"elapsedMs":956,"severityText":"INFO","body":"frame.page","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"character":120,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":155861,"framedBytes":155865}}
{"elapsedMs":968,"severityText":"INFO","body":"frame.page","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"character":0,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":977,"severityText":"INFO","body":"frame.page","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"character":34,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":979,"severityText":"INFO","body":"frame.page","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"character":92,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":286933,"framedBytes":286937}}
{"elapsedMs":982,"severityText":"INFO","body":"frame.page","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"character":8232,"rawPageBytes":131070,"metadataCharacters":4096,"serializedBytes":286929,"framedBytes":286933}}
{"elapsedMs":1174,"severityText":"INFO","body":"frame.boundary","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"rawPageBytes":131072,"maximumEscapedMetadataCharacters":43655,"acceptedBytes":1048575,"rejectedBytes":1048581,"cap":1048576,"verificationBufferBytes":8388608,"verificationBufferWireBytes":0}}
{"elapsedMs":1176,"severityText":"INFO","body":"case.passed","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"name":"frame-bounds"}}
{"elapsedMs":1176,"severityText":"INFO","body":"completed","traceId":"6073de00c15e43f0f2c15a7de14b7327","attributes":{"assertions":78,"failures":0,"baselineHazard":false,"clientPipes":0,"registrations":0}}
```

Observed assertions: 78; failed cases: 0; marker-attributed baseline hazard in this run: NOT_RUN. A failed case remains a failure; later independent cases do not clear it.

## Stateful candidate qualification transcript

```jsonl
{"elapsedMs":0,"severityText":"INFO","body":"run.mode","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"diagnosticOnly":false,"candidateQualification":true,"baselineIncluded":false,"cases":["candidate-stateful-session","native-write","native-read","eight-connections","queue-bounds","frame-bounds"]}}
{"elapsedMs":57,"severityText":"INFO","body":"stateful.navigation","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"path":"Inventory\u003ESelect\u003EMember\u003EBack","handshakes":1,"sameManifest":true,"sameScope":true,"selectedContent":"Synthetic alpha member body.","oldReceiptAfterBack":"SPIKE.RECEIPT"}}
{"elapsedMs":60,"severityText":"INFO","body":"stateful.clean-cancellation","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"prewritePreserved":true,"preAdmissionPreserved":true,"handshakes":1,"sameManifest":true}}
{"elapsedMs":61,"severityText":"INFO","body":"stateful.after-complete","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"member":"alpha","handshakes":1,"registrations":0}}
{"elapsedMs":63,"severityText":"INFO","body":"stateful.dirty-reconnect","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"terminalRefused":true,"newScope":true,"newManifest":true,"oldManifestRefusal":"SPIKE.MANIFEST","oldReceiptRefusal":"SPIKE.RECEIPT","newMemberContent":"Synthetic beta member body.","Handshakes":2,"Dropped":1,"serverContinuedAfterClientClose":true}}
{"elapsedMs":472,"severityText":"INFO","body":"stateful.deadline","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"ErrorCode":"SPIKE.DEADLINE","callerCancelled":false,"healthyScopePreserved":true}}
{"elapsedMs":738,"severityText":"INFO","body":"stateful.lease","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"expiryRefused":true,"policyRevocationRefused":true,"authority":"OS-derived peer; server workspace/epoch; synthetic native-root label","rootFilesystemEnforcement":"NOT_PROVEN"}}
{"elapsedMs":741,"severityText":"INFO","body":"candidate.cleanup","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":4,"Revoked":4,"Dropped":3,"Deadlines":1,"clientPipes":0,"registrations":0}}
{"elapsedMs":742,"severityText":"INFO","body":"case.passed","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"name":"candidate-stateful-session"}}
{"elapsedMs":754,"severityText":"INFO","body":"native.possible-partial-write","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"pendingAtCancellation":true,"receivedBytesAfterClose":0,"payloadTextBytes":900000,"exactPartialWrite":"NOT_PROVEN","responseAttribution":"NOT_PROVEN; abandoned connection not reused"}}
{"elapsedMs":755,"severityText":"INFO","body":"case.passed","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"name":"native-write"}}
{"elapsedMs":756,"severityText":"INFO","body":"native.partial-read","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"prefixBytesSent":2,"expectedPrefixBytes":4,"endOfConnection":true,"exactClientBytesConsumed":"NOT_PROVEN","registrations":0}}
{"elapsedMs":757,"severityText":"INFO","body":"case.passed","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"name":"native-read"}}
{"elapsedMs":923,"severityText":"INFO","body":"candidate.connection-pressure","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"cap":8,"ninthRefused":true,"activeAfterReconnect":8,"Handshakes":9,"scope":"one candidate host"}}
{"elapsedMs":938,"severityText":"INFO","body":"candidate.cleanup","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"Active":0,"LiveLeases":0,"queueActive":0,"queuePending":0,"Handshakes":9,"Revoked":9,"Dropped":0,"Deadlines":0,"clientPipes":0,"registrations":0}}
{"elapsedMs":938,"severityText":"INFO","body":"case.passed","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"name":"eight-connections"}}
{"elapsedMs":940,"severityText":"INFO","body":"candidate.queue-pressure","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"active":4,"pending":16,"Refusals":1,"scope":"same queue implementation, direct synthetic submissions; not global"}}
{"elapsedMs":941,"severityText":"INFO","body":"candidate.queue-cleanup","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"active":0,"pending":0,"PeakActive":4,"PeakPending":16,"completed":20}}
{"elapsedMs":941,"severityText":"INFO","body":"case.passed","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"name":"queue-bounds"}}
{"elapsedMs":945,"severityText":"INFO","body":"frame.page","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"character":120,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":155861,"framedBytes":155865}}
{"elapsedMs":956,"severityText":"INFO","body":"frame.page","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"character":0,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":963,"severityText":"INFO","body":"frame.page","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"character":34,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":811221,"framedBytes":811225}}
{"elapsedMs":966,"severityText":"INFO","body":"frame.page","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"character":92,"rawPageBytes":131072,"metadataCharacters":4096,"serializedBytes":286933,"framedBytes":286937}}
{"elapsedMs":968,"severityText":"INFO","body":"frame.page","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"character":8232,"rawPageBytes":131070,"metadataCharacters":4096,"serializedBytes":286929,"framedBytes":286933}}
{"elapsedMs":1146,"severityText":"INFO","body":"frame.boundary","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"rawPageBytes":131072,"maximumEscapedMetadataCharacters":43655,"acceptedBytes":1048575,"rejectedBytes":1048581,"cap":1048576,"verificationBufferBytes":8388608,"verificationBufferWireBytes":0}}
{"elapsedMs":1147,"severityText":"INFO","body":"case.passed","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"name":"frame-bounds"}}
{"elapsedMs":1147,"severityText":"INFO","body":"completed","traceId":"8deb1b1da76cc20479ad0f4b5b2ca1b4","attributes":{"assertions":79,"failures":0,"baselineHazard":false,"clientPipes":0,"registrations":0}}
```

Observed assertions: 79; failed cases: 0; marker-attributed baseline hazard in this run: NOT_RUN. A failed case remains a failure; later independent cases do not clear it.
