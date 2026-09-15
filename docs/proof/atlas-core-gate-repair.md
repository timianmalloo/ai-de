---
id: proof-atlas-core-gate-repair
title: "Atlas byte-bound and containment gate repair"
type: proof
status: proposed
owner: "@timianmalloo"
tags: [atlas, bounds, containment, regression]
links:
  - { to: plan-atlas-five-gates, rel: depends-on }
  - { to: coordination-atlas-five-gates, rel: relates-to }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Production index boundary proof, two killed mutations, and filesystem comparison reuse; independent review pending."
---

# Contract and scope

Goal: repair only G2 (bounds detector) and G3 (containment comparisons) in the approved Atlas five-gate ledger. Done when the production boundary, refusal before publication, resource release, detector mutations, and focused regressions have measured evidence. Not in scope: UI, native implementation, E1/E2, main publication, broad allowlists, shared audit/lesson/derived writes. Tier T2 per dispatch ledger; fan-out one. The initial conversational tier T1 was corrected to the dispatched Core tier after reading the coordination record.

Author: `codex-astra-core-gates`; session `codex-atlas-core-gates`; tree `C:/Projects/ai-de-fix-atlas-core-gates`; branch `fix/atlas-core-gates`; base `4473f260`. Conductor confirmed the brief contract before source edits. The parent graph and independently cleared plan are reused. Local dependency chain: inspect contracts → confirm semantics → regressions and red gates → isolated mutations → minimum repairs → focused qualification → receipt → independent review. No second planning agent or desktop run.

Surface list: pinned Git index → capture budget/refusal → snapshot entries/association/digest → content admission and member containment → static scanners → tests and evidence. Existing domain invariants and data representation are unchanged; no new entity, persisted measure, schema, endpoint, or UI. Existing capture state, reason, invocation count, duration metrics, and diagnostic sink make the boundary observable.

## Verified implementation

- `AtlasGitMembership.CaptureAsync` calls `CaptureForQualificationAsync`. The latter refuses non-Windows execution with `windows-native-evidence-required` before ordinary-path checks or pin acquisition. This repair does **not** claim a production POSIX membership exploit.
- The original production caller already passed `MaxIndexBytes` (8,388,608) to `NativePin.Digest`. Its `RandomAccess.GetLength` comparison refused oversize input before creating the hash. Baseline boundary tests passed **before** the detector repair. G2 was a scanner false positive, not a missing production clamp.
- The detector retains its original scan root (`src/` tracked C# files), recursion/census, bound-name expressions, direct-enforcement expressions, and existing `OverviewNodeCap` exception. `MaxIndexBytes` receives no reason-only exception. The initial checked seam used global substring matching within that file; independent review blocked it because unrelated classes could supply its evidence. The corrected seam extracts the direct `AtlasGitMembership.CaptureForQualificationAsync` method and the direct `Digest` method of its nested `NativePin` class. It requires adjacent index pin/call statements, exactly one real `index.Digest` invocation, and the digest length guard at the start of the helper, before hashing. Class/member recognition masks comments and literals and rejects missing, ambiguous, or unbalanced scopes. This remains a narrow lexical contract, not general C# control-flow analysis; behavioral tests remain necessary.
- G3 replaces the root equality and separator prefix in `UnderOrSame`, plus the prefix in `AllowsAtlasContent`, with existing `PathComparison.ForThisFileSystem`. It preserves rooted, colon, and traversal refusal. Windows behavior is preserved. Admission cannot reach a case-distinct sibling using rooted or traversal input because those forms are rejected first.
- New admission cases use a real `WorkspaceCore` and store. The nested decoder cases exercise its callable boundary with ordinary relative records: `src`, `src-other`, and `SRC`. The POSIX expected result is encoded in the portable test but was not run on POSIX in this episode.

## Real index boundary oracle

The test constructs a genuine index at exactly 8 MiB and one byte over. It verifies the original SHA-1 checksum, preserves original entries/extensions, adds an uppercase optional `TEST` extension with a big-endian length, and recomputes the checksum. This follows the [Git index format](https://git-scm.com/docs/index-format). The pinned Git executable successfully reads **both** fixtures using actual `ls-files --cached --stage -z --full-name --sparse` before capture. No mocked process or helper-only invocation stands in for production.

The test calls real `CaptureAsync`. At the cap it checks complete membership and the exact SHA-256 index digest. One byte over must return `BudgetExceeded`, `index-byte-budget`, exactly two Git invocations, empty entries, null total/association/digest, and non-current state. It then verifies the native cleanup charge record returns to its baseline and reopens the index for exclusive read/write. `await using` disposes any snapshot even when an assertion fails; existing fixture disposal removes the synthetic repository.

Observed final test output:

```text
INDEX_BOUNDARY bytes=8388609 state=BudgetExceeded reason=index-byte-budget invocations=2
INDEX_BOUNDARY bytes=8388608 state=CandidateComplete reason= invocations=6
```

# Measured results

Commands ran in the author's tree on Windows, .NET target `net10.0`, xUnit 2.9.3, pinned Git `C:\Program Files\Git\cmd\git.exe`, version `git version 2.55.0.windows.2`. No skip or native-support fallback was introduced. TRX files record zero skipped/non-executed tests.

All raw files below exist under `C:/Projects/ai-de-fix-atlas-core-gates/.artifacts/atlas-core-gates/`. They are retained local evidence; this receipt contains the measured results for the committed handoff. Timings are TRX finish minus start, not inferred shell durations.

| Run / raw TRX and corresponding `.log` basename | Exit | Passed / failed / total | Measured seconds |
|---|---:|---:|---:|
| `boundary-baseline` (before production/detector edits) | 0 | 13 / 0 / 13 | 1.6982335 |
| `forwarding-mutant` (`MaxIndexBytes` → `long.MaxValue`) | 1 | 1 / 1 / 2 | 1.5286608 |
| `clamp-mutant` (delete helper length guard) | 1 | 1 / 1 / 2 | 1.5200869 |
| `focused-final` (restored production clamp, final repairs) | 0 | 55 / 0 / 55 | 19.6156036 |

Both mutant failures are the oversize case, with actual output:

```text
Assert.Equal() Failure: Values differ
Expected: BudgetExceeded
Actual:   CandidateComplete
INDEX_BOUNDARY bytes=8388609 state=CandidateComplete reason= invocations=6
```

The at-limit case remained successful under both mutants. The final 55-test filter was exactly:

```text
dotnet test tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~AtlasGitMembershipTests|FullyQualifiedName~AtlasProductionAdmissionTests|FullyQualifiedName~AtlasContentContainmentTests" --logger "trx;LogFileName=focused-final.trx" --results-directory .artifacts/atlas-core-gates -v q
```

The baseline filter selected `CaptureAsync_IndexByteBoundary`, `DecodeMembership_NestedSource`, and `AtlasContentContainmentTests` (13 cases). Both mutation filters selected only `CaptureAsync_IndexByteBoundary` (2 cases). These add 13 test cases: two explicitly Windows-classified native boundary cases and eleven portable decoder/admission cases. Parent integrated qualification owns count regeneration.

| Static check / raw log | Observed result | Exit |
|---|---|---:|
| Initial `python tools/verify-bounds-are-enforced.py` | MaxIndexBytes reported never applied | 1 |
| Initial `python tools/verify-containment-comparisons.py` | Exactly AtlasGitMembership:434 and WorkspaceCore:593 | 1 |
| `gate-forwarding-mutant.log` | Repaired scanner rejects actual unlimited-forwarding mutation | 1 |
| `gate-clamp-mutant.log` | Repaired scanner rejects actual removed-clamp mutation | 1 |
| `bounds-self-test.log` | 9/9 cases passed | 0 |
| `bounds-final.log` | 30 bound constants accepted | 0 |
| `containment-self-test.log` | Existing inline, indirect, missing/incorrect approval, empty-corpus negatives and clean case passed | 0 |
| `containment-final.log` | Every separator containment uses the shared comparison | 0 |

The initial reds are observed tool output; no initial raw log file is claimed. All later named raw logs exist. Final source diff was read after restoring both injected mutations: the only production delta is the three comparator expressions, with the original bound forwarding and guard intact. `git diff --check` passed. The final test run used the same restored source; subsequent detector mutations were restored byte-for-byte to that source.

## Raw TRX SHA-256

```text
boundary-baseline.trx F3E5AD9D25B3736FA8CC503DF64570DE244340D2D07AF7A135F501FEE6CD6BD1
forwarding-mutant.trx 13745B5840D2C14FB66A48AEED7AA4C7607C10ED3B8CF8EC05A5CAD325B4D7E2
clamp-mutant.trx FB10E93D6E47D7E16089C5DACED920018287F5164170E132A8C08CAD45177E81
focused-final.trx 0A8C3AD7AE44AC72CE09D3B922D375EB76572E04ECC4DEC2321012AE00537187
```

# Class → sweep → derive → prevent

Proposed existing-class recurrences for the Conductor's serialized lesson writer:

1. **DC-104:** a control's first failure can describe its own limited recognition rather than a product defect. Sweep: the existing detector scans 30 constants; inspect its one unresolved cap and its original indirect mechanism, then establish the actual cap at the real caller. Derive: retain indirect recognition only when the exact caller and pre-hash guard both exist. Prevent: nine scanner self-tests, actual forwarding/clamp scanner mutants, and the two real production boundary cases. No duplicate product clamp added.
2. **DC-108 path-comparison recurrence:** handwritten host comparisons drift from the shared filesystem policy. Sweep: repository containment detector identifies exactly the two Atlas sites; inspect both input contracts before asserting reachability. Derive: shared comparison at the separator boundary, preserving existing refusal filters. Prevent: existing detector/self-tests plus nested-member and content-admission tests. POSIX runtime remains unmeasured, so no cross-platform execution claim is made.
3. **Coordination correction, DC-118 mechanism:** repeated `--path` flags on one claim invocation selected only the last path. Readback exposed the narrowed claim before source edits. Each remaining path was then claimed in its own invocation and every grant observed. The Conductor receives this recurrence proposal; no new preventive coordination-tool control is claimed or authorized here.

No shared audit, lesson register, or derived files were written. `AIDE_SESSION` and `AIDE_CONTRACT_LOG` were absent, so no contract event was fabricated. Conductor owns serialized audit/evidence capture. Own liveness was written and exact leases are released after the commits.

## Independent-review correction: scoped evidence

The reviewer hard-blocked the initial source commit `74f2ec0e`: `wrong_class` and `live_unbounded_plus_decoy` both passed `index_bound_enforced`. This disproved the original claim that the lexical evidence was connected to the actual call chain. It did not disprove the separate native production boundary tests. The Conductor authorized an eight-call correction limited to the detector and this receipt; production and test source were unchanged.

Red-first evidence: `scoped-review-red.log` contains `FAIL: wrong class`, `FAIL: live unbounded plus decoy`, and `9/11 passed`, exit 1. The old predicate therefore failed both independent reviewer cases before its replacement.

The correction uses a bounded brace-aware direct-member extractor in the same detector file. A search of existing `tools/` Python helpers found no suitable reusable C# class/member extractor. No new path, dependency, or scanning framework was added. It binds the pinned-index call to the actual capture method and the pre-hash guard to the actual nested `NativePin.Digest`; unrelated classes/methods and quoted decoys no longer count. Missing, ambiguous, and unbalanced declarations fail recognition. Unsupported future syntax must be inspected and the narrow recognizer updated; this is not a complete C# parser or control-flow proof.

Observed correction results, all raw logs retained beside the earlier evidence:

| Evidence | Result | Exit |
|---|---|---:|
| `scoped-review-red.log` | Old predicate fails the two new decoy expectations; 9/11 | 1 |
| `scoped-review-green.log` | 15/15 self-tests, including both reviewer decoys, wrong caller/helper class/helper method, raw-string decoy, prior forwarding/clamp/boundary negatives | 0 |
| `scoped-gate-green.log` | Actual repository scan accepts all 30 constants | 0 |
| `scoped-actual-source-mutants.log` | Real source accepted; five in-memory source mutations rejected | 0 |

The last row runs the predicate on the actual production file and mutated copies in memory, without editing production. Raw output:

```text
{'actual': True, 'wrong_class': False, 'live_unbounded_plus_decoy': False, 'deleted_clamp': False, 'wrong_caller_method': False, 'wrong_native_pin_class': False}
```

Class → sweep → derive → prevent: **DC-102/DC-118 recurrence proposal** — lexical evidence from an unrelated scope satisfied a claim about the real producer. Sweep the caller, owning class, nested helper class, helper method, and literal-decoy paths. Derive class/member-scoped evidence with a unique real digest invocation. Prevent with the reviewer decoys and additional scope/literal negatives in the executable self-test, plus actual-source mutant readback. This is an author-created detector defect, corrected after independent review; no author clearance is claimed.

The earlier 9/9 self-test row is historical and was insufficient. The corrected self-test is 15/15. Production/test-source `git diff HEAD` was empty during correction, so the Conductor explicitly retained the earlier 55-test result without rerunning unchanged product tests. The same independent reviewer must still clear the correction.

## Handoff and remaining gates

Source/test commit: `74f2ec0e` (`fix(atlas): prove index bounds and share containment comparisons`). Local execution used the approved sequential dependency chain; no agent fan-out or semantic retry. A 24-tool-boundary ceiling governed the lane; the final proof/lease close is within that ceiling.

Changed authored manifest: `tools/verify-bounds-are-enforced.py`; `src/AiDe.Core/Understanding/AtlasGitMembership.cs`; `src/AiDe.Core/WorkspaceCore.cs`; `tests/AiDe.Core.Tests/Understanding/AtlasGitMembershipTests.cs`; `tests/AiDe.Core.Tests/Understanding/AtlasProductionAdmissionTests.cs`; this receipt.

Independent veto review remains pending; this author does not clear it. Main moved to `bcf4959b` during the lane; no main merge was performed here. Conductor/GHCP own current-main integration, official whole-gate/count qualification, Release, and publication. Raw evidence remains in the worker tree. No additional product behavior, schema, dependency, or UI surface was added.
