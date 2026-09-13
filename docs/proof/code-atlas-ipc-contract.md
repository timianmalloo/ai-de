---
id: proof-code-atlas-ipc-contract
title: Code Atlas IPC cancellation contract experiment
type: proof
status: review
owner: Conductor
phase: investigate
tags: [code-atlas, ipc, cancellation, synthetic]
links:
  - rel: documents
    target: design-code-atlas-shared-host-admission
review-by: 2026-09-20
summary: Synthetic comparison of existing IPC cancellation and an isolated abort/reconnect candidate. No production implementation or admission is authorized by this experiment.
---

# Code Atlas IPC cancellation contract experiment

## Scope and status

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

## Preregistered hypothesis and mini-contract

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

## Claim / evidence / oracle / red / confidence / residual

The final transcript below is authoritative for which cases completed in the last run.
The table does not convert a missing event into a pass.

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
