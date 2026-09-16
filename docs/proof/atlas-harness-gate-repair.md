---
id: proof-atlas-harness-gate-repair
title: "Atlas harness diagnostics: original assertions and complete failure records"
type: proof-pack
status: proposed
owner: "@timianmalloo"
tags: [proof, atlas, testing, diagnostics, coordination]
links:
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Executed author red/green and five real-helper mutation controls for assertion identity, original stack and uncapped diagnostics; correlated TCS scanner fixtures and the actual STA census. Independent review remains open."
---

# G4 harness diagnostic repair

## Superseding scanner correction after independent BLOCK

Independent Astra review **BLOCKED** scanner `f43ff83c`: file-wide EDI recognition
allowed a healthy method to exempt a separate unguarded wrapper. The earlier
passing self-test did not cover that correlation boundary. The production helper
and its four passing tests/five rejected source mutations below are unchanged.

The reviewer supplied this exact lexical probe:

```csharp
private static void Healthy(Exception failure) {
    if (failure is Xunit.Sdk.XunitException)
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    throw new InvalidOperationException("retained", failure);
}
private static void Broken(Exception unrelated) {
    throw new InvalidOperationException("broken wrapper", unrelated);
}
```

Executed replay of that probe against frozen and corrected scanners:

```text
f43ff83: problems=[], wrapped_files=1
corrected: one diagnostic for tests/MixedEdiTests.cs, wrapped_files=1
```

The corrected diagnostic is the existing unguarded-wrapper refusal. Exact JSON
is `C:/Users/malla/AppData/Local/Temp/atlas-edi-correction.json`; the reproducible
temporary script is `atlas-edi-correction.py` in the same directory. It loads the
frozen scanner with `git show f43ff83:tools/verify-harness-diagnostics.py`, invokes
both real `check` functions against the same temporary file, and asserts the
old acceptance and new single refusal.

Before changing the rule, three new fixtures were observed red:

```text
verify-harness-diagnostics: SELF-TEST FAILED — scoped EDI verdicts: ['MixedEdiTests.cs', 'UnrelatedCollectionEdiTests.cs', 'UnrelatedOriginalEdiTests.cs']
```

**Repair:** every wrapper match must independently qualify for the new EDI path.
The guard must precede that wrapper in the same innermost callable, with no
intervening reassignment or unrelated statement. The guarded original must be
the wrapped identifier, or the foreach item from the exact wrapped collection.
The actual retained-array foreach plus nonempty-array check remains admitted.
The file-wide EDI presence check no longer exempts a wrapper. Legacy direct
throw-guard behavior and the existing STA denominator are retained.

Seven additional scoped fixtures cover the exact reviewer probe, separate methods
using the same parameter spelling, separate healthy-array and broken methods,
same-method unrelated original/collection, and the two correctly correlated
original/collection positives. Final self-test therefore has **11 TCS fixtures
and nine EDI fixtures**. The earlier two-EDI count below describes the blocked
revision, not this correction.

Final executed commands:

```powershell
python C:/Users/malla/AppData/Local/Temp/atlas-edi-correction.py
python tools/verify-harness-diagnostics.py --self-test
python tools/verify-harness-diagnostics.py
git diff --check
```

All exited 0. Self-test reported the exact mixed, same-name mixed, unrelated
collection and unrelated-original files as planted failures; correlated positive
fixtures were not reported. Normal output remained:

```text
verify-harness-diagnostics: 4 file(s) declare an STA thread = 2 wrapping + 0 plain rethrow(s) + 1 original TCS handoff(s) + 1 whose subject is an exception.
verify-harness-diagnostics: OK — 2 wrapping harness(es), every one rethrows an assertion failure as itself.
```

Class → sweep → derive → prevent: **DC-104/DC-118** recurrence; a file-wide guard
presence check widened a callable-local exception contract. Sweep guard location,
same-spelling parameters and exact wrapped source/collection. Derive an independent
verdict per wrapper for the new EDI path. Prevent with the observed red fixtures
and exact frozen-versus-corrected replay, then retained independent re-review.
This remains a bounded lexical checker, not arbitrary C# control-flow analysis.
No App rerun was performed or needed for this scanner/proof-only correction.
Independent re-review remains required; this author does not clear the BLOCK.

## Goal, authority and boundary

Goal: recognize a valid original-exception TCS handoff and preserve original
assertion identity/stack plus complete retained diagnostics in the actual helper.
Done when the exact source and proof are committed with executed red/green and
mutation evidence for independent review. Tier T2, fan-out 0, budget 24 tool
boundaries. Base: `9301207ee8f1164b53f70fb7f2ead91377f8f22a`.

The exact G4 grant is Conductor's `docs/plans/atlas-five-gates.md`. Authored paths:
`tools/verify-harness-diagnostics.py`, the existing
`tests/AiDe.App.Tests/Workbench/Understanding/AtlasSharedHostAdmissionTests.cs`,
and this receipt. The daemon proof file is read only. No ownership contract,
shared audit/lessons/derived output, other source/test, UIA or product change is
included. Parser repair and advanced main remain separate Conductor work.

The initial blanket App runtime hold was corrected by Conductor after reading
the exact four new test bodies and existing dispatcher. Conductor determined
the prior hold came from superseded liveness, and expressly admitted the exact
headless `StageDiagnostics_` filter under the existing user authorization.
The selected tests use no Window.Show, UIA, foreground window, Core service or
unrelated App case. One calls the existing background STA Dispatcher with
Task.Yield, an assertion and throwing cleanup, then drains/shuts it down.
Full/shown App and native qualification remain GHCP-scheduled work.

## Root causes and repair

**Verified:** the baseline normal gate reports two findings: the actual
`StageDiagnostics.ThrowIfFailed` aggregate wrapper and the unclassified STA
handoff in `AtlasDaemonMainWindowProofTests.RunDispatcherAsync`.
The latter catches the original exception, stores it in a local or forwards it
directly to the same TCS, and awaits that TCS's Task. No daemon source edit is needed.

The scanner adds a bounded lexical category for async STA methods. Every exception
setter must use an unchanged caught exception, directly or through its captured
local, and the same declared, unrebound TCS must be awaited. Wrapped/replaced
exceptions, unrelated arguments/awaits, local null reset, catch-source reassignment
and TCS rebinding are rejected. Setters are checked individually; a healthy setter
or method does not excuse a bad one in the same file. The existing invariant that
a test file cannot contain multiple independent STA creations is retained. The
shared non-test harness exception to that invariant is unchanged.

An EDI guard is recognized only when the checked XunitException identifier is the
identifier passed to `ExceptionDispatchInfo.Capture(...).Throw()`. The existing
wrapper/guard checks are retained; AggregateException is not globally ignored.
This remains a lexical source gate with the stated bounded method/catch shapes,
not a C# compiler, alias analysis or proof of arbitrary async control flow.

**Verified helper defect:** assertions were always demoted into AggregateException,
and complete failure details were available only through a stage stream capped at
160 records and 1,600 detail characters. That stream could lose cleanup evidence.

The helper now retains one record per original exception instance: its first
capture ordinal/category, original Exception, and full type/message/stack snapshot.
Identity deduplication remains; repeated captures are not described as distinct
failures. The capture ordinal advances even for a repeated instance, so first
captures at ordinals 1 and 3 remain 1 and 3 after a duplicate at ordinal 2.
Records are saved independently of the unchanged stage/detail caps. Missing stack
is honestly `not recorded`; no stack is synthesized.

ThrowIfFailed snapshots the originals in retained order. The **first retained
direct XunitException** is rethrown through EDI as the **same instance**, preserving
its message and original stack. It is not labeled a guessed body-primary failure.
If no direct assertion exists, the original exceptions are aggregated without
flattening. An assertion nested inside an aggregate does not become a direct one.

Surface list: caught exception → retained identity/category/ordinal → independent
failure persistence → assertion/aggregate propagation → xUnit/TRX; C# source →
scanner handoff classification → census/CLI result. Existing helper, stdlib Python,
EDI and xUnit are reused. No new dependency, shared harness or production abstraction.

## Exact commands and observed red/green

From `C:/Projects/ai-de-fix-atlas-harness-diagnostics`:

```powershell
python tools/verify-harness-diagnostics.py
python tools/verify-harness-diagnostics.py --self-test
dotnet build tests/AiDe.App.Tests/AiDe.App.Tests.csproj -nologo -v q
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --no-build --filter "FullyQualifiedName~AiDe.App.Tests.AtlasSharedHostAdmissionTests.StageDiagnostics_" --logger "trx;LogFileName=red.trx" --results-directory C:/Users/malla/AppData/Local/Temp/atlas-harness-diagnostics/red -nologo -v q
```

The four tests were written and compiled before changing StageDiagnostics. Initial
build: exit 0, zero warnings/errors. The red TRX was read: total/executed **4**,
passed **1**, failed **3**, skipped **0**, process exit **1**, test duration **46 ms**.

| Exact suffix after `StageDiagnostics_` | Original helper | Final helper |
|---|---|---|
| FirstRetainedDirectAssertionKeepsIdentityAndStack | FAIL: expected XunitException, actual AggregateException | PASS |
| AggregationPreservesOriginalsWithoutFlattening | PASS | PASS |
| PersistsEveryRetainedFailureBeyondStageAndDetailCaps | FAIL: missing first failure ordinal/category | PASS |
| OwnedDispatcherRetainsAssertionAndCleanup | FAIL: expected XunitException, actual AggregateException | PASS |

The red assertion headline was observed as:

```text
Assert.Throws() Failure: Exception type was not an exact match
Expected: typeof(Xunit.Sdk.XunitException)
Actual:   typeof(System.AggregateException)
---- System.AggregateException : Primary and cleanup failures retained separately. (owned dispatcher assertion) (owned dispatcher cleanup)
-------- owned dispatcher assertion
-------- System.IO.IOException : owned dispatcher cleanup
```

After repair, the same filter passed four of four. Five controlled mutations were
then built and run against the same real helper/test file. Each selected exactly
four tests; the temporary driver checked TRX totals, exact failed case names and
exit status. It restored the original source in `finally`, rebuilt and reran the
same four tests after each mutation batch. No unrelated test ran.

| Injected source fault | Expected observed failure(s) | Passed / failed | Exit |
|---|---|---|---|
| Remove EDI assertion guard | Identity/stack; real STA propagation | 2 / 2 | 1 |
| Replace assertion with a new XunitException | Identity/stack; real STA propagation | 2 / 2 | 1 |
| Couple failure persistence to remaining stage capacity | Persistence beyond caps | 3 / 1 | 1 |
| Flatten fallback aggregate | Nested original preservation | 3 / 1 | 1 |
| Rethrow with `throw failure` and reset original stack | Original-stack preservation | 3 / 1 | 1 |
| Restored final source | None | 4 / 0 | 0 |

Final raw test output:

```text
Test run for C:\Projects\ai-de-fix-atlas-harness-diagnostics\tests\AiDe.App.Tests\bin\Debug\net10.0-windows\AiDe.App.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.
WARNING: Overwriting results file: C:\Users\malla\AppData\Local\Temp\atlas-harness-diagnostics\final\final.trx
Results File: C:\Users\malla\AppData\Local\Temp\atlas-harness-diagnostics\final\final.trx
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 46 ms - AiDe.App.Tests.dll (net10.0)
```

Final build: zero warnings/errors, reported **2.42 s**. The recorder measured
**2,603.653 ms** for that build process and **1,101.471 ms** for the final test
process. These are individual local process samples, not a performance claim.
The final TRX replaces the prior restored-final TRX after the separate stack
mutation batch; each mutation retains its own named TRX and raw logs.

## Scanner red-first controls and actual census

Before scanner repair, the new healthy TCS fixture failed as unclassified and the
same-instance EDI guard was rejected. Subsequent falsification exposed local
null-reset and reassigned catch-source gaps; each new control was observed red
before tightening the relevant identity check. Final self-test contains **11 TCS
fixtures** (one healthy and ten malformed/mixed cases) and **two EDI fixtures**
(same identifier accepted, unrelated captured identifier rejected). All original
guard/wrapper/qualified-type/literal-fixture/multiple-STA checks remain.

TCS negatives: wrapped original, wrong awaited task, uncaught argument, replacement
local, local null reset, rebound completion source, reassigned direct catch,
reassigned source before local capture, healthy-plus-bad methods in one file,
and healthy-plus-bad setters in one method. The mixed-method fixture retains the
multiple-independent-STA finding; the mixed-setter fixture prevents an unrelated
healthy path from masking a bad setter.

Final normal raw output, exit **0**, empty stderr:

```text
verify-harness-diagnostics: 4 file(s) declare an STA thread = 2 wrapping + 0 plain rethrow(s) + 1 original TCS handoff(s) + 1 whose subject is an exception.
verify-harness-diagnostics: OK — 2 wrapping harness(es), every one rethrows an assertion failure as itself.
```

Final self-test ends, exit **0**, empty stderr:

```text
verify-harness-diagnostics: self-test OK — unguarded fails, guarded passes, 11 TCS fixtures and 2 EDI fixtures retain correlation, same-file negatives fail, and a literal-throwing fixture is left alone.
```

An independent `os.walk` census over recursive `tests/**/*.cs`, with the existing
literal STA creation token and no path allowlist, found the same **four files**:

| File | STA creations |
|---|---:|
| tests/AiDe.App.Tests/Sta.cs | 2 |
| tests/AiDe.App.Tests/WorkbenchControllerTests.cs | 1 |
| tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs | 1 |
| tests/AiDe.App.Tests/Workbench/Understanding/AtlasSharedHostAdmissionTests.cs | 1 |

There are **five creations**, not four. The temporary recorder initially asserted
file count equaled creation count and failed. Inspection confirmed Sta.cs has no
Fact/Theory and is the existing shared-harness exception. The corrected census
records both quantities rather than erasing that distinction. The gate's own
per-file reporting and existing test-file invariant were not changed for this.
Final scanner and self-test process samples were **114.380 ms** and **54.068 ms**.

## Actual retained evidence

All TRX/raw build/test logs are beneath
`C:/Users/malla/AppData/Local/Temp/atlas-harness-diagnostics/`:
`red/red.trx`, `green/green.trx`, `drop-assertion-guard/drop-assertion-guard.trx`,
`replace-assertion-instance/replace-assertion-instance.trx`,
`couple-failures-to-stage-cap/couple-failures-to-stage-cap.trx`,
`flatten-aggregates/flatten-aggregates.trx`,
`reset-assertion-stack/reset-assertion-stack.trx`, and `final/final.trx`.
`mutation-results.json`, `stack-results.json` and `scanner-and-census.json` contain
actual exits, counts, timings and identities. `normal.log` and `self-test.log`
retain full scanner output, including every planted diagnostic line.

Final generated helper logs in this worktree were opened and read:

- `artifacts/atlas-mainwindow/stage-diagnostics-cap-9374743a141040e3b1e3bd45eaf2ff09.log`
  — 22,842 bytes; 13 omitted stages; two retained failures at ordinal 1
  (`first.assertion`, XunitException) and ordinal 3 (`retained.cleanup`, IOException).
  Both complete >2,000-character messages and original stacks passed exact contains
  assertions; the duplicate capture did not replace the first category.
- `artifacts/atlas-mainwindow/stage-diagnostics-owned-d00ec0b6690441f8831ef620fedd174b.log`
  — 3,564 bytes; zero omitted stages; original assertion and cleanup at ordinals
  1 and 2 with categories `owned.body.primary` and `owned.cleanup-retained-debt`.

These GUID-named diagnostic files are intentional generated evidence under the
existing artifact location, not committed source or peer files. They are retained
for reviewer access. This Markdown retains the durable result summary.

## Class → sweep → derive → prevent and handoff

**Class:** DC-078, a diagnostic wrapper demotes an assertion and names the wrong
cause; DC-079, competing harness conventions need explicit classification;
DC-104/DC-118, a passing scanner can use a narrower identity/context boundary than
the actual contract. Capped stage output was not a complete retained-failure record.

**Sweep:** original and cleanup exceptions, direct versus nested assertions,
capture order, duplicate identity, stage and detail overflow, original stack,
same TCS/local/catch identity, every setter in a recognized method, same-file
healthy/bad paths, existing wrapper guards and independent STA denominator.

**Derive:** retain original diagnostic identities independently of bounded progress
messages. Classify a handoff by correlated source, destination and await rather
than the mere presence of a TCS or a healthy guard elsewhere. Distinguish file
count from thread-creation count and retained failures from repeated captures.

**Prevent:** four executed real-helper tests, five compiled source mutations,
11 TCS fixtures and two EDI fixtures, while retaining the original scanner checks.
The initial local recorder count mistake and Windows rg wildcard invocation were
corrected by inspecting the real census and using `-g` over the directory. A
semicolon in an intermediate aggregate message exposed the pre-existing bounded
WRAPS pattern's string limit; final truthful copy avoids it without changing the
generic wrapper rule or suppressing a finding. Broader C# parsing remains a
limitation, not a claimed acceptance result.

Execution graph: exact cleared ledger → real red reproduction and fixtures →
bounded classifier/helper changes → admitted headless red/green/mutations → actual
logs/census → proof/commit → independent review. The finite failure predicates are
the loop variant. The worker writes no shared audit/lesson/derived artifact;
Conductor serializes these existing-class recurrences and closes the audit.

Remaining: independent source review and integrated qualification on advanced
main, including GHCP's separately scheduled full/shown/native checks. This author
clears no veto. Conductor's episode capture must name this proof; no shared event
is invented by this isolated worker.
