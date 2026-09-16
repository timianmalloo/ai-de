---
id: proof-atlas-p1-03-pair-preparation
title: "Atlas two-arm runner and post-build freeze preparation"
type: doc
status: review
owner: "@timianmalloo"
tags: [atlas, proof, runner, containment]
links:
  - { to: proof-atlas-p1-03-transition-review, rel: depends-on }
  - { to: proof-atlas-p1-03-uia-transition, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "Preparation only: ten harmless runner controls pass, both required Debug builds succeeded and an 11,625-file post-build manifest verifies; independent runner/SRE review and an execution slot remain required."
---

# Preparation result

**Verified:** two required Debug builds succeeded with zero warnings/errors; the
post-build manifest contains 11,625 files across 1,025 roots and a subsequent full
verification returned `PINS-MATCH`. Ten harmless controls passed. **No dotnet test,
NativeTransition Fact, shown window, HWND/live UIA, focus, capture or desktop input
ran in this unit.** The author does not clear independent runner/SRE review.

Goal: produce one reviewable task-local runner, post-build dependency freeze and
exact future two-arm execution contract. Done when those artifacts and harmless
refusal/containment evidence are frozen for independent review. Not in scope:
execution slot activation, native execution, product/test/shared-runner changes,
canonical/main join or push. Tier T2; fan-out zero; model Astra, retained for process
custody and evidence semantics. Budget: 18 orchestration calls / 25 minutes.

Tree: `C:/Projects/ai-de-test-atlas-p1-03-uia-pair`; branch
`test/atlas-p1-03-uia-pair`. Build source commit:
`1d46d651cd5b6356e05545115757ba7eb7ffbf45`. Reviewed test source remains
`914a755d84f82d381fc0c516ac4e0b2257c61cb256545a904e32da35052099bc`.
`git diff --exit-code` against the base over `src` and `tests` passed. Later runner,
proof and audit commits are distinct from this build-source identity; there is no
self-referential committed manifest and no post-freeze rebuild.

Preparation authority is the user-delegated programme and actual Owner decision,
not a fabricated watcher preparation grant. `req-01M2NYTSRKJZ22SS0KR0PBKAS6` is a
status notice. The reviewed test preparation is CLEAR in the linked independent
receipt; this runner is a new review unit. A fresh watcher **execution** slot is
still mandatory. Existing programme graph is retained, with Conductor's material
profile replan recorded separately at `ff2fb242950449a22137d6f5d5b7e0abe8659498`.

## Frozen files and actual builds

Runner: `docs/proof/records/atlas-p1-03-uia-transition/run_pair.py`.
Runner SHA256: `3e816170418907b5b6d624b6e1f6d29950e9e55754284bc1deb3b1356a3f487c`.

Raw evidence is retained in this tree under `artifacts/atlas-pair-preparation/`.
Those files are local artifacts, not claimed to travel with this Markdown commit.

| Artifact | Observed content |
| --- | --- |
| `build.json` | Both exact commands, build-source commit, timestamps, exit codes and measured durations |
| `build-0.stdout.log`, `build-0.stderr.log` | App.Tests build: success, 0 warnings/errors, 9.625 seconds wall time |
| `build-1.stdout.log`, `build-1.stderr.log` | Daemon build: success, 0 warnings/errors, 0.875 seconds wall time |
| `manifest.json` | Frozen at 2026-09-16T20:55:55.584150Z, after both builds completed |
| `red-controls.json` | Two intended failures: added-file refusal absent and unrelated loading text accepted |
| `containment-inspection.json` | Seven of eight passed; exact-handle exit observation failed |
| `green-freeze-controls.json` | Ten cases, zero failures/errors; named population included |
| `containment-control.json` | Timed-out owned tree, three creation identities, active zero, retained handles exited, outside sentinel survived |
| `controls/*/` | Retained harmless stdout/stderr, process identity and containment records |

Manifest SHA256:
`ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314`.

The one preparation phase executed, in this order:

```powershell
dotnet build tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug
dotnet build src/AiDe.Daemon/AiDe.Daemon.csproj --configuration Debug
```

`prepare` refuses an already-attempted build phase. The runner freezes only after
both commands return successfully. It retains complete built `bin/Debug` trees
(including App/Test/Daemon), project obj/assets/runtimeconfig/dependency files,
resolved NuGet package directories, the selected SDK directory, dotnet host and
all currently installed shared framework directories, source/project inputs,
the runner and its loaded Python modules/native DLLs, the PowerShell directory,
Git command executable, selected WebView runtime directory and loader. There are
no `.WebView2` exclusions. File population, hashes and tool resolution are compared;
added, missing and changed files all refuse. The large frozen set is intentionally
broader than a few sampled assembly hashes.

SDK: 10.0.303; dotnet host: 10.0.11; Python: 3.12.10 x64. Full resolver outputs and
installed runtime catalog are in the manifest. Selected executable identities:

| File | SHA256 |
| --- | --- |
| Test output `AiDe.App.Tests.dll` | `067d29aa1352acc9d2c734a4136ffa70ca4294604729cfaff9890f006dcda30c` |
| Test output `testhost.dll` | `92cbdf52db5249112f9f53f1fcdaf9f9aa3fce467141c688a31a7fe095e4d304` |
| App output `AiDe.App.dll` | `db25116baa8d5471888634b6a950ce2037ac87099dd4705fa841a49eb66dcacd` |
| Daemon output `AiDe.Daemon.dll` | `a3e3fa8b97a86c7b401841a269545a882fb4c33f1b3f9d5090a8c8f1feca4b35` |
| Daemon executable | `89708cc9ed51895c56050e605368dae7351d2661077b2e45fdd0840198fca763` |

The manifest records declared runtime/dependency roots, not a claim to freeze the
entire operating system. Git's command executable is pinned; its complete installed
transitive binary tree is not enumerated. OS services and machine policy are not
claimed immutable. Independent review must assess this external-dependency boundary
before accepting the freeze for execution; do not infer closure from file count.

## WebView profile amendment

Owner admitted a symmetric runner-only amendment after observing that default
WebView UDF creation can write inside the test output tree and share warmed state.
Actual source `WebSurfaceHost.cs` uses `new WebView2()` and default
`EnsureCoreWebView2Async()`. Installed SDK 1.0.3485.44 documents
`WEBVIEW2_USER_DATA_FOLDER` in its Core XML; its native header declares
`GetAvailableCoreWebView2BrowserVersionString`. No product/test source is changed.
[Microsoft's UDF documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/user-data-folder)
explains that controls using the same UDF share a session.

Before each child launch the runner sets the override to a distinct, fresh absolute
`artifacts/atlas-uia-profiles/<pair-label>-A` or `-B` path. It refuses inherited
WEBVIEW2 configuration, reused paths, paths outside its owned profile parent and
overlap with any immutable root. Both start empty; profiles are retained. It does
not change machine settings, delete old profiles or exclude newly created files
from the immutable output manifest. This explicit substitution further limits any
comparison to the historical canonical test.

The actual loader API resolved **153.0.4234.32**, at
`C:/Program Files (x86)/Microsoft/EdgeWebView/Application/153.0.4234.32`.
Browser executable SHA256:
`49b97ce18fd644c0b629de4478e220d8c1a65ae3cb92bdd1c79a789038c741f0`.
Loader SHA256:
`4cd3ffd5f52122432b6537b360c2317f96650928c927bb3caa59d5807da573a9`.
This is resolution evidence, **not evidence that a shown arm consumed it**.

Future arms must produce Job Object-correlated browser identities plus CIM
ExecutablePath, CommandLine and CreationFileTime observations. The parser requires
the selected binary and actual `--user-data-dir` path; absent consumption evidence
refuses. The harmless child control proves environment delivery only. CIM browser
correlation, including cross-API creation-time precision, has not been exercised
against a browser or a harmless CIM surrogate in this unit and is a specific
independent-review gap. No environment variable is promoted into consumption proof.

## Process custody and controls

The shared `bounded_process.py` was inspected. Its stdin gate closes the race between
process creation and Job Object assignment, but its complete API also imposes
memory/process quotas and discards failed stdout. This task-local adaptation retains
the gate and configures only kill-on-close. It does not import or change that helper.

The child command cannot start until its Python gate is assigned to an owned Job
Object. The runner records PID and GetProcessTimes creation FILETIME, retains process
handles, checks membership, captures images, and reconciles observed identities with
job lifetime TotalProcesses. Missing identities fail closed, including descendants
too short-lived to observe. The gate records its direct child's retained-handle
identity. No PID-only or process-name kill is used. Termination addresses the owned
Job Object; pre-assignment failure can terminate only the unreleased gate's retained
Popen handle. Microsoft documents [job membership](https://learn.microsoft.com/en-us/windows/win32/api/jobapi/nf-jobapi-isprocessinjob)
and [lifetime/active accounting](https://github.com/MicrosoftDocs/sdk-api/blob/docs/sdk-api-src/content/winnt/ns-winnt-jobobject_basic_accounting_information.md).

Each future arm has an external **180-second** circuit breaker and at most **30
seconds** to contain and verify owned exits. Slot expiry also stops execution.
These are experimental runner bounds, not changes to product/test timeouts. Raw
stdout/stderr stream to files throughout. The watcher loop samples processes; it
does not poll WPF readiness or retry an oracle. A Job Object is retained until the
zero-active count and exact retained process-handle signals are checked.

Observed harmless controls:

1. Added input refused (observed red before fix).
2. Missing and changed input refused.
3. Unrelated loading text rejected (observed red before fix).
4. Actual Atlas TabItem ancestor accepted; cyclic ancestry rejected.
5. Profile distinctness, freshness, owned path, immutable-root overlap and inherited
   configuration refusals.
6. Child receives the exact UDF environment value; missing browser-consumption
   evidence is refused.
7. Expired slot launches no process/output directory.
8. Missing process identity, forced cleanup, uncontained exit and recorded process
   errors cannot pass the process gate.
9. Normal harmless stdout/stderr remain readable and identities reconcile.
10. A one-second harmless deadline terminates a three-process owned tree, verifies
    all handles exited, and leaves the separate sentinel alive with unchanged birth.

The first containment control failed despite job active=0 because retained process
handles were not yet signaled. The captured JSON distinguishes that condition.
Correction waits on those exact handles within the existing cleanup deadline;
the next control passed. Class → sweep → derive → prevent: process aggregate state
is not completion of each owned resource; sweep normal/timeout paths; derive the
handle-exit requirement; retain the timeout/sentinel control. Conductor owns central
lesson classification. No shared lesson register was edited.

## Exact future execution contract — not executed here

After independent runner/SRE clearance, the watcher must bind the final candidate
commit, runner hash, manifest hash, fixed tree, expiration and exactly one A then
one B. Preparation session must end before the official same-tree execution session
starts. The execution identity is `codex-atlas-p1-03-pair-execution` /
`codex-astra-pair-executor`.

```powershell
python -B docs/proof/records/atlas-p1-03-uia-transition/run_pair.py execute --manifest artifacts/atlas-pair-preparation/manifest.json --manifest-sha256 ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314 --reviewed-commit <INDEPENDENTLY_REVIEWED_RUNNER_COMMIT> --slot <WATCHER_SLOT_ID> --expires-utc <WATCHER_EXPIRY_UTC> --label <UNIQUE_PAIR_LABEL>
```

Slot ID is recorded input, not a cryptographic or connector-backed grant verifier;
Conductor/watcher must supply the actual authorization. No command here activates
a slot. The runner fixes these separate fresh dotnet processes, in this order:

```text
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug --no-build --no-restore --filter FullyQualifiedName=AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_A_NoLoadingTraversal --logger trx;LogFileName=arm.trx --results-directory <pair>/A/trx
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --configuration Debug --no-build --no-restore --filter FullyQualifiedName=AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_B_LoadingTraversal --logger trx;LogFileName=arm.trx --results-directory <pair>/B/trx
```

Arguments are passed as an argv list, not shell text. ATLAS_PROOF_RUN is the unique
pair label plus `-A` or `-B`. Existing pair/arm/profile paths are refused. No rebuild
or retry occurs. Full pins are checked before each arm and after it. Any failure,
expiry, missing identity, unresolved cleanup or changed pin stops the pair.

The reader separates `fixture_valid`, `original_oracle` and `cleanup_valid`:

- Fixture: exact identity, one natural activation/admission/handoff/replacement/
  reader Loaded/tail/reply, ready event, unchanged current child and independent
  before/after WPF packets. Owner remains explicitly not-recorded.
- Treatment B: an untruncated loading census from the owned HWND/process, between
  held/release ticks, with `Loading Code Atlas.` below the actual Code Atlas TabItem.
  Census-called or unrelated text cannot pass. A has no loading census.
- Oracle: actual one-case TRX and unchanged original UIA query events. Passing
  requires all five names, receipt Completed and zero failures. A negative requires
  the original automation stack and exactly one primary failure. Exit code must
  agree; process exit alone is never accepted.
- Cleanup: registry zero, reader-disposed custody, healthy lease release, recorded
  daemon PID mapped to a creation identity, reaped exit zero and Forced=false, plus
  owned Job Object/handle proof. Forced daemon cleanup invalidates.

A normally completed negative can proceed to planned B only if all other gates
pass. The test's negative path does not call its normal-exit wait before cleanup;
therefore it may produce forced cleanup and stop the pair. The runner does not
relax that outcome or repair the reviewed test. Post-query ancestry is observation,
never a replacement/retry oracle.

Pair state is `artifacts/atlas-uia-pairs/<label>/state.json`; raw TRX/streams/process
records are under each A/B directory, with native receipts retained at their
original `artifacts/atlas-uia-transition/<label>-A|B/receipt.json` locations. State
records BEGIN, PINS-BEFORE/PINS-AFTER, END and RELEASE. RELEASE means the runner has
ended; the watcher must independently verify containment and release its own slot.

## Review boundary and remaining work

The ten controls do not cover every receipt-parser mutation or establish native
schema fidelity by execution. Browser CIM consumption and its timestamp matching,
short-lived descendant identity capture, external Git/OS dependency scope and
failure-path record persistence require independent SRE scrutiny. Strict missing
identity or truncated-census refusal may make a shown pair inconclusive; it is not
permission to weaken those gates. The source stays frozen while reviewed.

Applicable testing surfaces: D0/D1 deterministic guards; D2 parsers/population
validation; D4 real filesystem and subprocess boundaries; D6 emitted/consumed
receipts; D7 synthetic census/profile-control substitutes paired with actual source
schemas and explicit unexecuted native limits. No independent hard veto is cleared
by the author. No causal finding follows from preparation.

| Completed | Remaining | Next |
| --- | --- | --- |
| Task-local runner, harmless controls, both builds and verified post-build manifest | Independent runner/SRE clearance, any exact corrections, fresh execution slot and actual browser/provider proof | Review this frozen candidate and declared gaps before admitting execution |
