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
summary: "Four harmless process-image fixtures distinguish synchronized-exit error 31 from live zero-capacity error 122; both collapse to the frozen runner's bare refusal. Diagnostic-loss correction proposed only; historical cause remains unknown."
---

# Process-image investigation after pair 01

## Root-cause overview and evidence boundary

**Verified diagnostic loss:** two distinct controlled native failure states produce
the same maintained `Refused: PROCESS-IMAGE-MISSING`. A synchronized process exit
before the image query produced native error **31**, while a deliberately zero-sized
image buffer against a live process produced **122**. The existing runner discards
both native error codes and the pending PID/birth at this boundary. The historical
pair's missing eighth process and the reason for its image-query failure remain
**unknown**. This investigation demonstrates a mechanism, not that historical cause.

Owner contract `req-01M2P3P146D09MKG08W1WPMX8V` was read and ACKed through official
coordination before any fixture ran. The independently completed documentary
outcome CLEAR at `0d8da79160ec8ce3c18b7e857f5b6f3dda8d7f56` was read through git.
That CLEAR accepts the invalid-attempt interpretation; it grants no runner repair,
readiness, native retry or new slot. The consumed pair slot was released by its
watcher, as recorded in that independent receipt.

Goal: distinguish the observer's failure states with at most four harmless fixtures.
Done when observed distinctions, reproduction limits and the smallest supported
correction are reviewable. Tier T2, no delegates; Astra for native identity/lifetime
semantics. Eight-call / fifteen-minute bound, checkpoint five. The existing programme
graph gains this bounded diagnostic node; no whole-programme replan or new execution
policy is introduced. Surface list: native image-call boundary -> pending identity
and exit observations -> actual failure/finalization record -> this proof -> Owner
and independent reviewer. No product/UI/domain model or compiled input changes.

The investigation tree is
`C:/Projects/ai-de-investigate-atlas-p1-03-process-image`, branch
`investigate/atlas-p1-03-process-image`, based on `f8ad3323`. Session/agent are
`codex-atlas-p1-03-process-image` / `codex-astra-process-image-investigator`.
Scratch lives only in `artifacts/atlas-process-image-investigation/` in that tree.
The original prepared tree and frozen runner/manifests are retained unchanged.

### Four planned fixtures, each executed once

| Fixture | Observed query and same-handle state | Actual frozen-path result |
| --- | --- | --- |
| Live owned Python child | PID 7864, birth 134340698653414844; membership true; wait 258 before/after; image succeeds | `Job.sample` returns and records the Python image |
| Synchronized exit before query | PID 35640, birth 134340698653905578; initially wait 258, then child exits via stdin byte and bounded wait; same handle wait 0 before query; query fails with immediate native error 31; after wait 0 | Bare `PROCESS-IMAGE-MISSING`; diagnostic cause reproduced for this synthetic sequence only |
| Injected image-query failure while live | PID 28488, birth 134340698654327026; zero-capacity buffer substituted for this native call; wait 258 before/after; immediate native error 122 | Identical bare `PROCESS-IMAGE-MISSING`; proves the message does not distinguish exit from this live failure |
| Failure containment/closure | Gate PID 21772; owned child PID 5024/birth 134340698654935019; second image query gets live zero-capacity error 122 | Actual `run_owned` records primary refusal, forced=true, active=0 of total=2, retained sampled handles exited=true; process.json retained |

Fourth-fixture `identities_complete=true`: its direct-child record supplies the
second PID/birth and reconciles the small two-process fixture. This does **not**
reproduce the historical seven-of-eight population or waive that failure. It remains
refused because forced termination and an execution error are present. No daemon,
testhost, WPF/UIA, browser or native Fact was launched by these fixtures.

Every proxy-observed `CloseHandle` returned success; all four fixtures ended with
an empty proxy-tracked handle set. This covers handles opened through the runner's
proxied APIs, not a whole-process or whole-system handle census. Fixtures 1–3 also
observed their harmless child exit. Fixture 4 exercises the actual runner gate,
Job termination, process evidence save and finalization. No global/name-based kill
was used. The fixture fallback is limited to its own retained `Popen` handle.

### Timing, API contracts and substitutions

`spike.py` imports the unchanged runner with bytecode writes disabled. A scratch API
proxy delegates real Win32 calls, records PID/raw birth/Job membership and separately
timed zero-wait observations, and intercepts only the image call under study. For
the exit case it releases the child's stdin gate and waits for that exact child to
exit before the single image query. There are no sleep-based race guesses or repeated
samples of that fixture. For the injected cases it passes a real zero-capacity
argument to the native API; it does not fabricate a Windows error code.

Immediately after a failed native image return, the first action is
`ctypes.get_last_error()`. Only then are the end timestamp and post-query native
observations taken. A successful call records native error as null. Both before
and after observations retain their own ticks; an after-query signaled state is
not backdated into a claim about the failure instant. Membership and raw identity
remain separately represented. One observation row means exactly one intercepted
image call, with immutable nested observations at their recorded times; counts and
distinctions are derived from these rows, not a second authoritative identity list.

Microsoft's [image-query contract](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-queryfullprocessimagenamew)
specifies the query rights, buffer-size parameter and failure return. Its
[last-error contract](https://learn.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-getlasterror)
requires immediate retrieval because later calls may overwrite the thread error.
[Python 3.12 ctypes](https://docs.python.org/3.12/library/ctypes.html#ctypes.get_last_error)
documents the saved thread-local value used with `WinDLL(use_last_error=True)`.
[WaitForSingleObject](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject)
defines zero-time observations: 258 is nonsignaled and 0 signaled.
[CloseHandle](https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle)
supplies the observed closure return contract. No general claim that all exited
processes return error 31 is made from this one controlled observation.

Only fixture 4 substitutes the imported module's `ROOT` in memory so harmless
commands use the investigator tree as cwd. It restores that value afterward and
uses the unchanged runner's actual `_gate` and `run_owned`. Fixtures add observation
calls and controlled scheduling; they are deliberately not timing-faithful native
pair replays. No maintained file, binary, manifest, selector or policy was patched.

## Smallest supported correction proposal — not implemented

The supported correction addresses **diagnostic information loss**, whose source
and effect are directly observed. It does not claim to repair the historical image
failure. At `Job.sample`'s image-query refusal, retain:

1. The pending PID, raw creation identity and already-observed membership result.
2. API name, flags, requested capacity, Boolean result, query timing and the native
   error captured immediately on failure, before clocks or another native call.
3. A separately timestamped same-handle exit-state observation and any failure of
   that observation, explicitly later than the failed image call.
4. These fields in a structured failure diagnostic that survives `run_owned`'s
   primary-error serialization and finalization, with secondary recording failures
   kept separate. Do not insert an image-less row as a complete identity.

Keep current failure/containment behavior, complete-population gate and no-retry
policy. An exited process is not permission to ignore the missing identity. An error
code alone is not cause. The smallest acceptance control must fail on the current
maintained output (both states are indistinguishable), then prove the structured
records distinguish the four admitted cases while preserving closure and refusal.
There is no maintained repair or claimed green regression control in this unit.

## Generalization, sweep and disconfirmation

Class shape: native failure is collapsed into a generic message before its pending
identity and same-thread error become durable. Signature: a native false return
followed by a bare exception; downstream serialization records only type/message.
It survives happy-path and synthetic containment checks because both can succeed
without requiring diagnostic fields. Central class allocation belongs to Conductor.

The bounded source sweep found two image-call sites in the frozen runner:
`Job.sample` line 161 drops pending identity/error before row retention;
`owned_snapshot` line 273 also emits a bare image-unavailable error (its input row
already supplies identity). The second site is a **source-confirmed sibling**, not
an executed fifth fixture. `docs/ai-forward-pack/scripts/bounded_process.py` contains
no image-query site in the inspected search; no change to it is proposed. No
`simplify:` marker was found in these two inspected files. An initial guessed
`tools/bounded_process.py` lookup failed; the actual file was found with `rg --files`.

Class -> sweep -> derive -> prevent: identify the lost native diagnostic, inspect
the two image boundaries, derive immediate structured capture independent of final
identity acceptance, and require the four-fixture diagnostic assertions in any later
admitted repair. Prose alone is not reported as an installed control.

Disconfirmation record:

- The live fixture rules out unconditional failure of this image API/handle-rights
  combination in the current environment.
- The live zero-capacity failure disproves interpreting the bare message as unique
  evidence of process exit.
- The synchronized-exit fixture establishes that exit-before-query can produce the
  bare refusal here; it does not establish it as necessary or as the historical cause.
- Successful proxy-observed closure and actual forced containment distinguish
  diagnostic loss from a demonstrated leak in these four fixtures.
- Nothing identifies the historical eighth process, a Git/provider cause, or the
  original native UIA missing-name failure.

**SRE peer:** retain the failure's native evidence before any later call can replace
it; do not relax containment on a guessed exit race. **Distributed-systems peer:**
lifecycle observations have their own time; identity does not imply liveness.
**Domain-research peer:** API documentation plus the controlled observations support
the distinctions above, not a universal exited-process error code. **Test-author
adversarial check:** the two different native errors and exit states collapse to the
same maintained exception; the proposed diagnostic acceptance fails today. This is
author analysis only. Independent findings review remains required; no hard veto
or repair is self-cleared.

## Phased repair proposal and retained evidence

| Phase | Independently reviewable scope | Acceptance / boundary |
| --- | --- | --- |
| 1 | Owner-admitted diagnostic-only runner correction, including both image-query sites if retained in scope | Four admitted distinctions persist in structured output; immediate-error ordering, primary preservation, closure and fail-closed population gates remain intact |
| 2 | Independent Test/SRE review of that correction and exact raw controls | Reviewer clears its own veto; no native execution implied |
| 3 | Owner decision on any future experiment/slot | Separate explicit admission; no repair or retry authorized by this investigation |

Rollback for a later diagnostic repair is to the frozen runner hash below; it must
not silently reuse or replace the consumed native attempt or its manifest. Any new
runner identity/freeze is a separate reviewed artifact. No build is required merely
to retain diagnostic fields, but this investigation authorizes no implementation.

Raw files in this investigator tree:

- `artifacts/atlas-process-image-investigation/spike.py`: exact four-fixture harness.
- `summary.json`, and `live/result.json`, `exit/result.json`, `inject/result.json`,
  `containment/result.json`: actual outcomes, pending identities, native errors,
  query/observation ticks and proxy closure records.
- `containment/process.json`, direct-child identity and streams: actual frozen
  `run_owned` result; the three direct-child fixture streams are retained too.
- `before-pins.json` / `after-pins.json`: full **11,630-file / 1,030-root** inventories
  match the existing corrected manifest, at 22:04:25.336382Z and 22:04:28.031260Z.

Both pin checks read the original prepared tree's complete declared population.
They also verify this tree's imported runner SHA256
`26567e7b408430ef29d4a44e6c60a1792cb2ddae9b23dd01393e1cc6ea3a3daa`.
Corrected manifest remains
`a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c`;
predecessor remains
`ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314`.
These are byte/population checks, not a fresh runtime resolver or native qualification.
Scratch is ignored local evidence; it does not travel with this Markdown commit.
Planned four fixtures / actual four; no resampling. Audit records actual duration
and calls. Token/spend telemetry is not recorded. Final handoff supplies actual
documentary checks, commit, clean state and release outcomes.

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
