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
summary: "Proposed diagnostic-only runner candidate: 19 harmless controls pass after three observed red missing-field failures; both image-query sites retain native error and pending identity. Only runner pin changed; independent review and manifest promotion remain pending."
---

# Image-query diagnostic candidate — not promoted

**Verified author evidence:** both image-query sites now retain immediate native
failure evidence in their maintained serialized output. Nineteen harmless controls
pass; the three new controls first failed on missing diagnostic fields while all
sixteen prior controls passed. No native Fact, GUI/browser process, build or slot
request ran in this unit. Independent review has not cleared this implementation
or its proposed manifest. No historical process identity or cause is inferred.

Goal/done: an independently reviewable diagnostic-only correction at `Job.sample`
and `owned_snapshot`, with red-first serialized-output controls and unchanged
failure, identity and containment policies. Tier T2, fan-out zero, Astra;
twelve-call / eighteen-minute ceiling, checkpoint eight. Session/agent:
`codex-atlas-p1-03-image-diagnostics` / `codex-astra-image-diagnostics-author`.
Conductor registered this new session in the ended prepared tree at `f8ad3323`.
Own primary liveness preceded edits. Owner contract
`req-01M2P4X7X5184R9DN4C28X64W4` was read and ACKed before authoring.

Independent findings receipt
`13c8070dfcb482d24b6fe147a33192bbf5ae8223:docs/proof/atlas-p1-03-process-image-review.md`
was read directly. Its FR-PI-001 requires actual invocation of the acceptance
predicate; FR-PI-002 requires maintained-output assertions with deliberate later
error clobber. The prior four-fixture investigation did not execute the sibling
site; this unit's sibling control is new evidence, not a retroactive fifth fixture.
Existing programme/spec/architecture boundaries remain; no new native execution
node is admitted by this candidate.

## What changed and how the diagnostic is consumed

Surface trace: actual Win32 image query -> `query_process_image` failure fact ->
`Refused.native_diagnostic` -> shared `failure_record` -> either
`process.json.primary_error`/`secondary_errors`, or
`browser-observations.json[*].failure` -> control/read-only reviewer. The existing
acceptance predicates still consume the original error/containment/identity fields.
No product/domain/UI schema changed. One native diagnostic is exactly one failed
image query, with separately timed later observations; it is not an accepted
process identity and is never added to the Job identity population.

`query_process_image` uses the same query rights, flags zero and 32,768-character
buffer. After a failed call, its first action captures `ctypes.get_last_error()`.
Only afterward does it record query-end time and perform the additional zero-time
same-handle exit observation. It preserves:

- `operation`, `site`, and `pending_identity.pid`, raw `creation_filetime`, plus
  already-observed membership and its tick.
- `query.flags`, requested `capacity`, Boolean result, immediate `native_error`,
  start/end ticks.
- `later_exit` with independent start/end ticks, raw wait result and native error
  if that observation fails.
- Nested `secondary_errors` for failed diagnostic observations. They cannot replace
  the original image failure or its captured native error.

The original `Refused` type and messages remain. `Job.sample` retains an image
identity only after successful query return. `owned_snapshot` preserves its original
PID/birth/live/membership checks and image comparison. Browser failure rows retain
their existing `error` field and add the structured `failure`; no consumer is made
more permissive. Secondary diagnostic errors are nested in that primary diagnostic,
distinct from `run_owned`'s separate cleanup-error list.

The immediate-capture contract follows
[Python 3.12 ctypes](https://docs.python.org/3.12/library/ctypes.html#ctypes.get_last_error)
and [Microsoft GetLastError](https://learn.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-getlasterror).
[QueryFullProcessImageNameW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamew)
defines failure retrieval and buffer capacity;
[WaitForSingleObject](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject)
defines the later zero-time state. Later state is not backdated into an assertion
about the exact failure instant.

## Actual red/green controls

Command: `python -B docs/proof/records/atlas-p1-03-uia-transition/run_pair.py selftest
--output artifacts/atlas-image-diagnostics/<red|green-1>.json`.

| Control | Observed red | Observed green |
| --- | --- | --- |
| `test_image_job_serialized_error_survives_later_clobber` | `native_diagnostic` missing from saved process primary error | Real zero-capacity native failure 122 survives a later deliberate saved-error value 9876 in maintained JSON; actual `verify_process_result` refuses |
| `test_image_job_secondary_observation_does_not_replace_primary` | `native_diagnostic` missing | Original error 122 retained; deliberately failed later wait records 6 as secondary, without replacing primary; actual process verifier refuses |
| `test_image_owned_snapshot_serialized_browser_failure_and_refusal` | `failure` missing from saved browser row | Direct sibling path retains error 122 through clobber 9876; maintained browser JSON is read; browser-consumption verifier refuses the error row |

Actual population: red **19 run / 16 passed / 3 errors**, all three at the missing
maintained fields; green **19 run / 19 passed / 0 failures or errors**. The original
sixteen control bodies are unchanged and passed. No dummy failure or acceptance
inferred solely from source is used. The two Job controls invoke
`verify_process_result` against parsed `process.json` and observe `Refused`.

The injection uses a real zero-capacity native call, captures its actual error,
then explicitly restores that error immediately before returning to the maintained
caller. This avoids FR-PI-002's proxy-boundary ambiguity. The later wait deliberately
changes the saved error to 9876, or executes an invalid-handle wait yielding error 6.
The assertions inspect maintained fields, not merely the proxy log. Normal successes
remain covered by existing actual Job/CIM controls.

The sibling fixture feeds a real harmless Python identity directly into the actual
observer queue; failure occurs in `owned_snapshot` before a CIM query. This bypasses
only the browser-image queue filter for the test and launches no browser. It proves
the sibling failure serialization path, not browser runtime consumption.

Both Job fault cases had `forced=true`, `contained=true`, active zero, retained
handles exited and `identities_complete=true`. The existing trusted direct-child
record reconciles these two-process fixtures. The failed sample creates no image
identity; the diagnostic is not a new population row. This is distinct from the
historical seven-of-eight gap and does not relax it. The exact lifecycle/cleanup
implementation remains unchanged outside failure-record serialization.

Raw control directories under `artifacts/atlas-pair-preparation/controls/`:

| Directory | Maintained output SHA256 |
| --- | --- |
| `image-job-secondary-84a4e67df6e54dd48fc5e567bd28705e/run/process.json` | `a7918397429e4080c321f1f6f063fc6053066c240e42cdbaa662268288e4415c` |
| `image-job-clobber-39f41ca2636f4727984bfa082b8bf70f/run/process.json` | `c87e4a4b9bb49fbe22860dcc3de2e5000dd8cea581f501127a170ab4467621c0` |
| `image-owned-snapshot-1afffed88b284fd19c8f07cabc571af3/browser-observations.json` | `5ca90510d6bd385754f9dfb6616517bf7fe2247a5cd47b39ad0e350f2e4bf2a4` |

Each directory also retains `control.json` with the actual injected call and later
clobber/failure. `artifacts/atlas-image-diagnostics/candidate-comparison.json`
records their exact paths, parsed contents and hashes. All raw artifacts are local
to this retained prepared tree; they do not travel with the Markdown commit.

## Proposed freeze and preservation

Before the first runner edit, `preedit-pins.log` recorded full `PINS-MATCH` against
the active corrected manifest. Both predecessor manifests remain byte-identical:

- Original `manifest.json`: `ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314`.
- Existing `manifest-corrected.json`: `a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c`.

**PROPOSED ONLY:**
`artifacts/atlas-pair-preparation/manifest-image-diagnostics-candidate.json`, SHA256
`66ccb1a395d44e34818f21c24c2f0608605f539484452c42890e2c0abcd1ff3e`.
Candidate runner SHA256:
`9615a47ac9b5acb7ce8c4fb09e23bdb0e99d4ebd62aedf804a97944ded86af2b`.
Its `status` is `proposed-not-promoted`; explicit predecessor and delta metadata
require independent review before any promotion. No active manifest was overwritten,
and no execution command or slot is granted by this file.

Full comparison found **11,630 files / 1,030 roots**, no added/missing inputs, and
exactly one changed existing input: the runner. All **11,629** other current inputs
match, comprising the **11,624** original non-runner inputs and all five measured
Git additions. Tool identities match; build source remains `1d46d651`. No compiled
output was rebuilt or changed.

The recorded AST comparison found unchanged `kernel`, `creation`, `gate`, `execute`,
`verify_process_result`, `verify_browser_use`, `profile_environment`,
`validate_receipt`, all sixteen prior control bodies, and `run_owned` apart from its
nested failure serializer. Product/test/tools source diff against `f8ad3323` is
empty. These checks establish preservation within their named boundaries, not native
operability or a complete real-browser outcome.

## Failure analysis, review and close

Class -> sweep -> derive -> prevent: loss of immediate native context was observed
at both image sites; shared capture and serialization now retain it; the three
red-first controls require durable fields and unchanged refusals. Conductor owns
central recurrence classification. No central lesson register was edited.

New failure modes: a later wait can fail or throw, and a later diagnostic clock can
fail. Capture keeps the original error and records secondary evidence separately.
The native later-wait failure is exercised; exceptional Python clock/API-wrapper
throws remain guarded source paths, not separately executed controls. No new retry,
permission, executable dependency or external service was added. Raw process IDs
and image diagnostics remain in the existing local evidence boundary.

**Language-developer peer:** one capture helper and one serializer reach both
maintained consumers. **Test-author adversarial check:** red failures target missing
serialized fields; clobber and failed secondary observation cannot change the saved
122. **SRE peer:** diagnostics remain observations and do not grant identity or
cleanup validity. Independent Test/SRE review is still required; author controls do
not clear its veto or promote the candidate manifest.

No historical UIA-cause, qualification, publication or renewed-slot claim follows.
Even an independent CLEAR does not automatically request another native run.
Budget and measured duration are in the own audit record; token/spend are not
recorded. Official regeneration, graph, source preservation, final clean state and
lease/session release are reported with actual results in the final handoff.

| Completed | Remaining | Best next action |
| --- | --- | --- |
| Both diagnostic sites, maintained red/green controls, proposed runner-only delta | Independent implementation/input comparison review; promotion decision | Review this frozen candidate without native execution or automatic retry |

# Corrected preparation candidate (preceding evidence)

**Verified correction evidence; independent veto remains open.** The independent
BLOCK at `2ab2f0b2bb2ae17ddb9bed5da1da935742bc9873` identified CIM precision loss,
identity-parse cleanup bypass, and an unpinned Git implementation. The Owner also
required a strict negative-oracle parser. This correction runs no build, dotnet
test, native Fact, GUI or browser probe. It preserves the original build source.

Owner request `req-01M2P0VQ6APR7BAG5CBH4ECK2X` was consumed and ACKed. Session
`codex-atlas-p1-03-pair-correction` owns only this proof, the runner and required
records. Goal: correct the four findings without changing compiled inputs. Done
when a preserved successor freeze and meaningful controls are reviewable. Tier T2,
fan-out zero, Astra; 16-call / 25-minute limit. Existing programme graph retains
the review-feedback edge; author evidence does not clear the independent veto.

## Correction evidence and successor identity

All paths below are local to `artifacts/atlas-pair-preparation/` in the named tree.
The old `manifest.json` is retained unchanged. `correction-preedit-verify.log`
records its complete **PINS-MATCH before the first runner edit**.

| Evidence | Actual result |
| --- | --- |
| `correction-red.json` | 13 run: original ten passed; two failures and one error reproduced cleanup, precision and provider-parser findings |
| `correction-green-1.json` | 16 run, zero failures/errors; case names recorded |
| `controls/cim-correlation-*/actual.json` | Real harmless Python process, same retained Job-owned handle around actual CIM query; raw and CIM times retained |
| `controls/malformed-identity-*/observation.json` | Actual decoder fault; final green preserves process.json, contains process tree and closes Job plus retained process handles |
| `controls/identity-read-*/run/process.json` | Missing/unreadable record controls retain refusal and containment |
| `controls/primary-secondary-*/run/process.json` | Deadline remains primary; identity-read and observer-drain errors are separate secondary errors |
| `correction-git/git-dependencies.json` | Actual cmd shim and mingw64 Git process images/modules, hashes and owned containment |
| `manifest-corrected.json` | Explicit successor: 11,630 files / 1,030 roots; all 11,624 old non-runner inputs identical; five measured Git additions |

Current runner SHA256:
`26567e7b408430ef29d4a44e6c60a1792cb2ddae9b23dd01393e1cc6ea3a3daa`.
Current successor manifest SHA256:
`a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c`.
Git evidence SHA256:
`ccb11ed9ab8bfefdef011e23b2e6ae039500b087fa23ddc20e87152e30b75ed5`.
The predecessor hash remains `ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314`.
Successor creation compares the complete old population and every old hash, allowing
only the runner hash to differ, then adds measured Git files. It does not invoke
`prepare`, restore or build. Source/test diff against the build base is empty.

### Actual correction controls

- `test_cim_precision_diagnostic_with_retained_handle_evidence`: unequal CIM time
  is accepted only with exact raw handle correlation. This row is synthetic.
- `test_actual_cim_handle_and_negative_identity_controls`: real harmless CIM query;
  the same live, Job-owned handle verifies PID, raw creation identity and image
  before and after. Wrong PID/birth/image, wrong CIM PID/image, exit during query,
  exited process and unavailable handle refuse. A four-tick CIM mutation is
  deliberately diagnostic only; no rounding enters the safety decision.
- `test_malformed_identity_still_closes_and_records`: actual JSON decoder fault.
- `test_missing_unreadable_identity_and_primary_error_preserved`: real finalization
  with missing/permission errors and an injected drain-record failure.
- `test_provider_failure_is_not_missing_name`: provider exception cannot become
  a negative original oracle even with the original stack substring.
- `test_receipt_schema_positive_negative_and_mutations`: emitted-schema A/B,
  pass/negative fixtures; duplicate packet, event-order, found-state, batch, timing,
  offscreen assertion, observer failure and forced-daemon-cleanup mutations refuse.

The exact negative assertion type is `Xunit.Sdk.NotNullException`, observed in the
historical P1-03 raw receipt and grounded in the unchanged source's `Assert.NotNull`.
It must accompany the last failed original query, the expected name prefix, unique
query IDs, one correlated batch and temporally enclosing WPF packets. Earlier
queries must have succeeded. An offscreen assertion, provider error or observer
failure is not a valid negative. Receipt counters, original owned HWND/process,
publication/cleanup ordering and TRX population must agree. Fixtures are synthetic;
they do not prove a shown arm's schema or its runtime validity.

Git probe: shim PID 5812 / birth 134340673561013339; actual implementation PID 3472 /
birth 134340673561106282. The four-process owned tree reached active zero and all
retained handles exited. The measured additions are `mingw64/bin/git.exe`,
`libiconv-2.dll`, `libintl-8.dll`, `libpcre2-8-0.dll`, `zlib1.dll`. Git reports
2.55.0.windows.2. The harmless command is `hash-object --stdin`, held under the
existing stdin gate and terminated by its bounded Job Object. OS modules were
recorded but are not newly frozen. This is measured module coverage for this command,
not a claim that all optional Git commands/plugins/configuration were exercised.

API grounding: Microsoft documents [module enumeration](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-enumprocessmodulesex),
[module paths](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-getmodulefilenameexw)
and [handle-derived PID](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocessid).
The probe verifies identity on retained Job-owned handles and closes only process
handles; returned module handles are not owned handles to close.

Class → sweep → derive → prevent: DC-211 candidate covers representation differences
being promoted into identity differences; sweep CIM/native comparisons, derive raw
handle authority, retain precision and identity-negative controls. DC-078 covers
evidence parsing replacing the primary failure or skipping cleanup; sweep direct
record reads/finalization, separate primary/secondary records, retain actual-path
fault controls. Parser evidence-shape and dependency-closure findings are captured
here for Conductor's classification; no central register was edited. Unsupported
`git var GIT_TEMPLATE_DIR`, guessed `coord.py`, and nonexistent `coord status`
lookups failed during grounding; their results are not claimed as verification.

Residual review gates: review the retained-handle observer lifetime, cleanup faults,
actual Git dependency boundary and strict parser against source. Actual browser/UDF
consumption, visible publication, provider census and arm outcomes remain unexecuted.
No canonical reproduction or retention-cause inference follows. No whole-branch
canonical merge is admitted. Token/spend telemetry is not recorded; audit duration
and the final call count are the measured cost fields. Administrative close checks
and commit identity are reported in the final handoff.

# Original preparation evidence (historical)

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

The predecessor pinned only Git's command shim. The successor above adds the
observed implementation and modules. Neither manifest claims the whole operating
system, OS services or machine policy immutable. Do not infer closure from file count.

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
correlation is now exercised against a harmless CIM surrogate in the correction
above; actual browser consumption remains unexecuted. CIM time is diagnostic only;
the same retained native handle is authoritative. No environment variable is
promoted into consumption proof.

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
python -B docs/proof/records/atlas-p1-03-uia-transition/run_pair.py execute --manifest artifacts/atlas-pair-preparation/manifest-corrected.json --manifest-sha256 a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c --reviewed-commit <INDEPENDENTLY_REVIEWED_RUNNER_COMMIT> --slot <WATCHER_SLOT_ID> --expires-utc <WATCHER_EXPIRY_UTC> --label <UNIQUE_PAIR_LABEL>
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
  the exact NotNull assertion type, a correlated missing-name original query,
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

The sixteen corrected controls cover the named parser mutations but do not establish
native schema fidelity by execution. Actual browser CIM consumption, retained-handle
lifetime, short-lived descendant capture, Git/OS dependency scope and failure-path
record persistence require independent SRE scrutiny. Strict missing
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
