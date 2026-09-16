---
id: proof-atlas-p1-03-uia-transition
title: "Atlas P1-03 loading traversal: experimental preparation"
type: doc
status: review
owner: "@timianmalloo"
tags: [atlas, proof, native, diagnostic]
links:
  - { to: proof-atlas-five-gates, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "Preparation only: 43 selected non-GUI controls passed; two fresh-process native arms are authored but unexecuted and require independent review and a separate execution grant."
---

# Result and authority

**Verified: 43 selected non-GUI cases passed, comprising 21 preserved observer
cases and 22 transition controls. No shown arm ran.** This is preparation evidence,
not P1-03 causation, visible-publication evidence, or an association result.

Goal: implement the frozen two-arm design as reviewable diagnostic preparation.
Done when: exact source/proof, red-first controls, selected inventory/TRXs,
preservation and loaded runtime identities are captured for independent review.
Not in scope: shown execution, product repair, full qualification, canonical/main
source publication, runner changes or new isolation policy. Tier T2; fan-out zero.
The programme's existing dependency graph remains: frozen design → preparation
controls → independent Test/SRE review → separately granted binary freeze/slot →
one fresh-process execution per arm. Authoring does not clear the later nodes.

Preparation grant: `req-01M2NWGFJG89QF0KFBA9W8YGF6`. Document-only independent
verdict: `req-01M2NT8TTYERHMRAFEYJC3Y6TC`, consumed in
`req-01M2NV3TGZ058CQE3W5RS1D7NM`; PASS-WITH-CONDITIONS applies to design B1–B3,
not this implementation. Frozen design blob:
`3b6c8153018d1199e669cd519ba1c0bc65547a3d`.

Owned tree: `C:/Projects/ai-de-test-atlas-p1-03-uia-transition`, branch
`test/atlas-p1-03-uia-transition`, base
`ebfe076125a1200345b806519b733104b3e06ea7`.
Session `codex-atlas-p1-03-transition-author`; agent `codex-astra-transition-author`.
**Experiment-only branch:** do not merge its Facts/helpers wholesale into
canonical/main. Ordinary full App discovery would violate their fresh-process,
unique-label execution contract. No skip or opt-in policy was added.

## Evidence and exact selected command

All raw paths below are relative to that owned tree and retained locally; they are
not claimed to travel with this Markdown commit.

| Evidence | Observed result |
| --- | --- |
| `artifacts/atlas-transition-preparation/red/red.trx` | 23 executed, 21 passed, 2 failed, 0 skipped |
| `artifacts/atlas-transition-preparation/green-freeze/green-freeze.trx` | 43 executed, 43 passed, 0 failed/skipped |
| `artifacts/atlas-transition-preparation/list-tests.txt` | 43 discovered selected names; exact set equality with final TRX |
| `artifacts/atlas-transition-preparation/verification.json` | Full per-case names/outcomes, hashes, preservation comparison and runtime identities |
| `artifacts/atlas-transition-preparation/runtime.json` | Actual loaded assembly paths, versions, SHA256 and module IDs |

```powershell
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug --filter 'FullyQualifiedName~AiDe.App.Tests.AtlasDaemonMainWindowProofTests.TransitionControl_|FullyQualifiedName~AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeObserver_NonGui_' --logger 'trx;LogFileName=green-freeze.trx' --results-directory artifacts/atlas-transition-preparation/green-freeze
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug --no-build --no-restore --list-tests --filter 'FullyQualifiedName~AiDe.App.Tests.AtlasDaemonMainWindowProofTests.TransitionControl_|FullyQualifiedName~AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeObserver_NonGui_'
```

Red SHA256: `d00d3179d4a00bf8fc5c9d483ffb588d765bd3dab6f326ab06565aa5e692bd74`.
Final green SHA256: `1a164dc366082b1ee45a92a590c1654d04b397a98a2ebe38d0e3e3549bfe6d55`.
Final source SHA256: `914a755d84f82d381fc0c516ac4e0b2257c61cb256545a904e32da35052099bc`.
Base source SHA256: `6af54fc651f355fbf22a4b17ecb256007fe6fa4ca8061b382cf61a45eb59841a`.

The red tests failed on the intended defects: completion inside the final status
notification (`Assert.False` saw true), and clearing a candidate after failed
pre-handoff disposal (`Assert.Same` saw null). Minimal fixes deferred readiness to
the dispatcher tail and retained failed-disposal custody. The red source/binary
snapshot was not separately pinned; the actual red TRX is retained.

All original declarations are unchanged. From the first original NativeObserver
Fact through EOF, the base and new source compare exactly after newline
normalization. The insertion boundary loses one blank line. Thus all canonical
selectors, results, assertions, waits, cleanup and 21 old control cases are
preserved; this is stronger than merely counting the old tests.

## Controls and what they establish

The 22 new selected cases cover:

- Actual unshown host/view publication: final notification remains incomplete
  before the Background tail; partial collection updates and loading text do not
  establish readiness; the complete actual two-row inventory is checked.
- Cancellation, disposal, replacement, duplicate status, incomplete tail, missing
  final notification, Unloaded and extra Loaded; disposed callbacks cannot add
  later tails. An unshown object fails the actual visible/Loaded predicate.
- Process-scoped noncapturing Loaded class callback: a synthetic routed event
  observes class-before-instance order; disposing the scoped registry makes later
  callbacks inert. This does not prove natural Loaded on a visible window.
- Cancellation before acquisition, while Held and after transfer; serialized
  concurrent disposal; failed pre-handoff custody and explicit recovery; failed
  post-handoff disposal through the actual owner.
- A queued test synchronization context pauses the actual owner continuation
  after handoff. Cancellation then forces its canceled-candidate disposal branch;
  failed disposal remains recoverable by that owner. This context is used only in
  the non-GUI control, never in a shown arm.
- Shared cleanup sequence: injected window/daemon cleanup failures are recorded,
  subsequent cleanup executes, the receipt is saved and an original exception
  retains its identity. The arm's final FailureCount assertion rejects cleanup-only
  failures. Filesystem/sink failure itself is not established by these controls.
- Actual runtime identity is emitted without Show, HWND creation, live UIA,
  focus, capture or desktop input in any selected transition control.

The positive unshown publication controls substitute `surface => true` to isolate
notification and synchronous setter ordering. The negative unshown control uses
the real visible/Loaded predicate and fails it. Neither substitutes for shown proof.
The static class registration has no claimed unregister operation; its scoped
dictionary releases the window/callback, and fresh process exit ends registration.

## Emitted contract for later runner review

The authored, **unexecuted** Facts are
`AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_A_NoLoadingTraversal`
and `AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_B_LoadingTraversal`.
Each requires a fresh test process and a unique nonempty ASCII alphanumeric/hyphen
`ATLAS_PROOF_RUN`; existing evidence directories are refused. Receipt location:
`artifacts/atlas-uia-transition/<ATLAS_PROOF_RUN>/receipt.json`.

Both arms use actual owned Git input, daemon, WorkspaceClient, MainWindowViewModel,
the real Architecture dock/menu/host path, and natural Loaded admission. The same
decorator holds the real admitted candidate before returning it to the owner.
Only B performs `TransitionCensusAsync` while the current child remains the exact
loading TextBlock; both then release once. Neither calls explicit ActivateAsync,
refreshes/invalidates automation peers, retries the original query, or sleeps.
The two arms differ deliberately only in that pre-release traversal. Temporal cost
of the treatment is intrinsic to it; no compensating delay is inserted in A.

| Event/field | Runner oracle and limit |
| --- | --- |
| `transition.identity` | Arm, Run, ProcessId, RepositoryRoot, SourceCommit, loaded Runtime/Assemblies and daemon executable hash; `UiaWpfOwner=not-recorded` |
| `transition.input` | Actual synthetic `src/Widget.cs` byte hash |
| `transition.host-loaded` | Count, Tick and dispatcher thread; require one natural host Loaded |
| `transition.held` | OwnHwnd, Count, Admissions, Handoffs, ContentType/Text, Tick; reader is null and exact current child is loading |
| `transition.uia-census`, Phase `loading-treatment` | B only, before release; owned HWND/process, raw parent ordinals/runtime IDs/names, limits/truncation; no WPF-owner inference |
| `transition.release` | Tick and LoadingTraversal treatment flag |
| `transition.publication` | Content replacement, each status notification, tail scheduling/entry, ready/rejected; counts, thread/apartment/ticks/current-child relation |
| `transition.pre-oracle` | One host Loaded/admission/handoff/replacement/reader Loaded/tail/inventory reply; owner identity remains not-recorded |
| existing `uia.find-first.original` and original assertions | Unchanged owned-HWND root and five exact name queries; first failure remains primary |
| existing `observer.wpf` | Independent before/after original-query current host/view relationships |
| `transition.uia-census`, Phase `after-original-oracle` | Both arms, after original oracle; bounded provider ancestry with explicit truncation; observer exceptions are recorded |
| `transition.cleanup` | RegistryCount, custody state sequence, Handoffs and Tick; expected registry zero and owner-driven disposal |
| `transition.lease-release-start/returned`, `transition.reader-disposed` | Actual shared observed wrapper's healthy release and completion events |
| `transition.daemon-normal-exit`, `transition.daemon-reaped` | Actual PID, exit code, listening evidence, forced flag and output hashes; forced cleanup invalidates receipt |
| top-level Completed, FailureCount | Completed true only with zero recorded failures; do not infer success from test process exit alone |

The raw census caps nodes at 128 and depth at 12, with a 250 ms soft budget checked
between provider calls. A provider call can block beyond that budget. The reused
dispatcher body has its existing 30-second outer wait; daemon exit has its existing
15-second bound and owned-PID cleanup. A separately reviewed watcher containment
contract is still required. Owned synthetic repositories are retained for review;
this new diagnostic does not delete its raw input/evidence directory.

Future execution request must name these exact two fully qualified Fact names,
one fixed count per fresh process, unique labels, the fixed tree, and
`--configuration Debug --no-build --no-restore`. No such command ran here.
The future runner must freeze **all consumed App/Test/Daemon dependencies and
installed runtime pins**, not merely the subset emitted by identity. No rebuild
between arms. Existing canonical `native-preflight.py` is not reused or modified.
Independent implementation review, full binary manifest, containment, execution
oracles and shown-slot grant remain unresolved gates.

## Runtime, corrections, cost and interpretation

Observed runtime: .NET 10.0.11. WindowsBase and PresentationFramework assembly
versions are 10.0.0.0, loaded from the installed WindowsDesktop 10.0.11 directory.
WindowsBase SHA256: `42E05225D944BDE54E4F2CBE0142105FCE822C574267B6F8E305EE8C45C664E8`.
PresentationFramework SHA256: `FA9AA188F9C92DC95B4E56F303B1875244BF45786733948C79034FE95FB0CD86`.
Final tested assembly SHA256: `B9EBFEC34B80D30D04A29B5915EB82F394AA9B80BA856D7AD2F7FA9675B4DEBB`.
Full loaded identities and module IDs are in runtime.json and verification.json.

Seven selected build/test invocations occurred: one compile-only failure (nullable
tuple mismatch), then six executing runs totaling 224 case executions. Preserved
TRXs are red, green-controls, green-controls-2, green-controls-3, green-final and
green-freeze. Counts progressed 23/21, 38/37, 39/39, 40/40, 41/41, 43/43
(executed/passed). The final TRX reports 789 ms test duration. Two list-only
invocations recorded 41 and then the final 43 names; the saved inventory is final.

Corrections are retained rather than recast as product findings. The concurrent
disposal control initially asserted before its asynchronous continuation entered;
it now awaits the explicit DisposalStarted signal. This is the existing DC-222
ordering class: sweep the new asynchronous assertions, derive an event-based
completion control, prevent recurrence with that selected test. The initial source
preservation assertion incorrectly included an insertion-boundary blank line; the
comparison now explicitly reports that whitespace normalization. Parent review
caught cleanup-finally replacement of an original exception; the shared cleanup
sequence and two fault-injection cases prevent that shape on this diagnostic path.
This is the existing DC-078 class (harness replaces the diagnostic reason): sweep
both arm-finally paths, derive the shared failure-recording cleanup sequence, and
prevent recurrence with the two executable controls. These controls were observed
green after the correction; **no cleanup red run is claimed**. The two-case red
baseline above predates this correction. Conductor owns the shared-register
recurrence capture; no central lesson file is authored by this unit.
The primary liveness file was missing during authoring; it was created at close
and labels that delay explicitly. The audit start marker and assignment prompt
were present. No AIDE_SESSION/AIDE_CONTRACT_LOG was available for an episode-close
channel, so no such event is fabricated.
The administrative close refused a missing required audit `shortname` at call 24
and stopped before regeneration/staging/commit. Conductor authorized one bounded
administrative correction. Actual orchestration cost is **25/24**, an overrun and
defect signal; the original budget was not raised. This correction adds the named
field and verifies persisted prompt/goal/done_when/artifact arrays before official
regeneration. No source or test execution is added by the recovery.

If valid A passes and B fails, the permitted claim is treatment association on
these frozen inputs. Both passing leaves the hypothesis unestablished. Both
failing means loading traversal was not necessary in this pair; opposite outcomes
remain opposite association. Missing/extra transitions, pins, uncontained cleanup,
or an unreached oracle invalidate the pair. `UiaWpfOwner=not-recorded` prevents a
retention-cause claim. Natural activation differs from canonical explicit
activation, and historical pass/fail assemblies differed: no canonical reproduction,
cohort causation or P1-03 root-cause resolution is claimed.

Completed: reviewable preparation and non-GUI evidence. Remaining: independent
Test/SRE implementation review and all later runtime/slot gates. Next: review the
frozen source and emitted contract, then decide whether to authorize a binary
freeze and separately contained two-arm execution.
