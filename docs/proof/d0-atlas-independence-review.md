---
id: proof-d0-atlas-independence-review
title: "Independent review: D0 direct static Atlas boundary"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [d0, atlas, independent-review, static-analysis]
links:
  - { to: proof-d0-atlas-independence, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "BLOCK: a new overload of a named shared port bypasses helper accounting; unrelated conditional code is rejected outside selected D0 members."
---

# Disposition

**BLOCK — Verified.** The normal-build headless suite passes all 20 cases, but an independently executed new-helper mutation escapes the guard. A second mutation demonstrates conditional-policy widening into an unrelated shared-file member. No production defect or existing indirect Atlas use is asserted from these synthetic counterexamples.

Reviewer: codex-astra-d0-review; session: codex-d0-atlas-review-astra. Worktree: `C:/Projects/ai-de-review-d0-atlas-independence-astra`; branch: `review/d0-atlas-independence-astra`. Reviewed commit: `97a9e200b5a5b0047e2b42cae931ffd35db02bc0`. The Conductor provisioned this tree; the reviewer created no other tree or agent and did not edit author/product source.

Frozen `tests/AiDe.App.Tests/SolutionTreeProbeTests.cs` byte SHA256 before and after execution:

`A5338A154CE836CE8D321F012F2C3637280293B33C304AAD12153275CDE3123E`

## Goal, graph, and scope

Goal: independently verify that the finite direct-static D0 guard preserves the D0 implementation boundary while allowing admitted Atlas coexistence. Done when actual code/evidence and independent headless checks support CLEAR or a precise BLOCK receipt. Not in scope: product repair, main/push, native/shown/STA execution, full App/Core qualification, specification rewrites, arbitrary transitive or runtime independence. Tier T2; no child agents.

The bounded optimize-graph pass used this dependency chain: authority and frozen source → real build/headless baseline plus source/evidence inspection → independent discriminating mutations → veto disposition and committed receipt → parent join. The baseline and reading overlap only while the build runs; they share no writes. There is no retry loop. Oracles: missing direct dependencies or unintended shared-port admission must fail; unrelated code outside selected members must remain admitted. Trigger union: Testing Strategy D0, D1/D2 for finite analyzer inputs, D3 for dependency direction, and real Roslyn/build binding for the analysis engine. No mock fidelity claim is involved.

Surface list: store census → solution-tree projection/frame → local/IPC query and registration → factory row → complete surface plus selected shell bind/refresh/populate/activation members → semantic guard and mutation results. The durable domain and product representation do not change. Graph grounding followed the author receipt's links to D0 intent and the session contract; author receipt metadata corrections remain with the Conductor.

The source-of-truth inspection includes ADR0038's D0 invocation contract and rejection of Atlas **as D0**, plus the parent's Owner clarification `cl-01M2KTRQJJ91B7YEVSSTSSNB70`. The review accepts the supplied finite direct-static scope. It does not reinterpret that scope as arbitrary call-graph closure.

# Independent evidence

The review executed, in its own tree:

```text
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter FullyQualifiedName~SolutionTreeProbeTests.ProbeAtlas_ --logger trx;LogFileName=review-baseline.trx --results-directory .artifacts/d0-review -v q
dotnet run --project .artifacts/d0-review/ReviewSpike.csproj -v q
```

The logger value was passed as one quoted shell argument. The first command performed a current normal build; it did not use `--no-build`. No GUI probe executable was launched. The second command was an isolated console reflection spike using a project reference to the built test assembly and its existing Roslyn dependency. It changed Roslyn trees in memory. The frozen test source stayed unchanged.

Local evidence retained in the review tree:

- `.artifacts/d0-review/baseline.log`
- `.artifacts/d0-review/review-baseline.trx`
- `.artifacts/d0-review/ReviewSpike.csproj`
- `.artifacts/d0-review/Program.cs`
- `.artifacts/d0-review/spike.log`

**Verified TRX:** 20 executed, 20 passed, 0 failed, 0 not executed, 0 aborted. All test names were inspected: baseline 1; negative mutations 12; positive coexistence/shared-port cases 4; project classification cases 3. TRX start `2026-09-15T18:05:05.8105178-07:00`, finish `2026-09-15T18:05:13.9125850-07:00`: 8.1020672 seconds. The normal build/test command returned exit 0. This establishes the selected test results, not completeness of their oracle.

**Verified independent reflection results:**

| Input | Roots | Reference observations | Shared symbols | Errors |
| --- | ---: | ---: | ---: | --- |
| Frozen baseline | 26 | 3976 | 58 | 0 |
| Existing `new-helper` negative control | 26 | 3978 | 58 | 1 UNACCOUNTED |
| New Atlas helper named `OpenKind(int)` | 26 | 3978 | 58 | **0 — escaped** |
| Identical new Atlas helper named `FreshReviewHelper(int)` | 26 | 3978 | 58 | 1 UNACCOUNTED |
| Unused conditional Atlas member in factory | 26 | 3976 | 58 | **1 CONDITIONAL — unrelated member refused** |

The spike returns successfully because it records analysis results; its exit is not an acceptance oracle. The table transcribes the actual emitted results, including the escaped mutation.

The reviewer also opened the author's raw TRX files under `C:/Projects/ai-de-fix-d0-atlas-independence/.artifacts/d0-independence/`: `obsolete-red` is 1/1 failed; `semantic-policy-red` is 15 executed, 8 passed, 7 failed; `classification-mutant` is 3/3 failed; `conditional-red` is 1/1 failed; `semantic-frozen` is 20/20 passed with no not-executed cases. These are historical author observations, separate from the reviewer run.

# Findings and veto-clear predicates

## FR-001 — New overload inherits a shared port's admission

**Issue; P1 / Major; Verified. Test Architect BLOCK; architecture/Security boundary review BLOCK.**

Evidence: `tests/AiDe.App.Tests/SolutionTreeProbeTests.cs:313` constructs a member key from containing type plus `symbol.Name`; line 314 admits that key from `SharedMembers`. Parameter types, arity and overload identity are lost. The comment at line 142 promises newly introduced helper calls fail closed, but this key describes an entire overload family.

Independent counterexample: add the following unselected member to `WorkbenchShell` and insert `OpenKind(0);` at the beginning of selected `RefreshSolutionTrees`:

```csharp
private static void OpenKind(int sentinel)
{
    System.GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState));
}
```

The guard reports zero errors. Rename both occurrences to `FreshReviewHelper` without changing the body, and it reports `UNACCOUNTED Method AiDe.App.Workbench.WorkbenchShell.FreshReviewHelper`. This paired check isolates name-family admission from root discovery, compilation failure, or an intentionally admitted existing handoff body. `OpenKind(DockHost, string, bool)` is the inspected existing shared port; the injected `OpenKind(int)` is a new direct helper edge, not that port.

Consequence: a new unreviewed helper can be added under any admitted method name and called directly by D0 without its implementation being inspected or classified. This violates the finite helper-accounting contract without requiring arbitrary transitive closure.

**Clears when:** admitted method/constructor ports have stable exact semantic declaration identities, including overload/arity distinction; the paired new-overload negative and existing-port positive become permanent tests; all 20 current cases retain their intended meaning and pass; independent re-review observes the new-overload mutation rejected. Do not expand the allowlist to admit the counterexample.

## FR-002 — Conditional refusal widens selected-member scope to shared files

**Issue; P2 / Major; Verified. Architecture BLOCK pending scope correction or an explicit Owner decision.**

Evidence: `tests/AiDe.App.Tests/SolutionTreeProbeTests.cs:271-278` checks every conditional directive or disabled-text trivia anywhere in all eight files containing roots. Only two files are complete D0 roots; the six other files deliberately contain unrelated admitted work.

Independent counterexample: add only this unused member to `SurfaceContentFactory`, outside its selected solution-tree row:

```csharp
private static void UnusedConditionalAtlas()
{
#if true
    System.GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState));
#endif
}
```

The D0 reference population remains exactly 3976 and the guard reports one `CONDITIONAL` error for the entire factory file. The frozen suite already admits an unused nonconditional Atlas method in this same class. Thus the new restriction depends on unrelated code's compilation syntax, rather than on whether a D0 root is conditional. The `simplify:` comment records the ceiling but does not itself establish authority to widen the selected-member boundary.

**Clears when:** conditional/disabled-code coverage preserves fail-closed protection for complete D0 files, selected members, conditional regions enclosing or concealing selected roots, and binding-affecting contexts such as conditional using aliases, while accepting unrelated conditional members/registration builders in shared files. Add discriminating positive and negative cases, retaining the active-D0 and disabled-D0 Atlas refusal. Parsing Debug defines alone must not be presented as Release/configuration coverage. Alternatively, the Owner must explicitly decide and scope the broader qualification obligation across those shared surfaces; that decision cannot be inferred from an inline shortcut or a passing current baseline. The reviewer does not select that policy on the Owner's behalf.

# Positive inspection and limits

**Verified:** the existing enum, FileRead, FactoryBuild and RepoRoot method bodies have no changes in the author diff against `247e6b4eaefba92cea250f657159a8768e8fb91f`. The new test constructor only supplies output. The installed central Roslyn CSharp version is 4.14.0. Source/project dependency edits are absent from this repair.

**Verified by opened source:** `ProjectionService.SolutionTree` reads the existing store and constructs `SolutionTreeProjection`; `CandidateWithinWorkspace` uses scope location and filesystem path containment; `WorkspaceClient.QueryAsync` uses the existing IPC envelope; `LocalWorkspaceQueries.SolutionTreeAsync` invokes the projection; the operation registration invokes `projections.SolutionTree`; the factory solution-tree row constructs `SolutionTreeSurface`; selected shell activation routes source/graph operations; `ShowNodeInCodeViewersAsync` uses `NodeContentSource.GetAsync`. These inspected handoffs do not directly use Atlas. The complete-file type declarations inspected in `SolutionTreeSurface.cs` and `SolutionTreeProjection.cs` are not partial.

**Limits / Flagged:** this BLOCK review did not finish a line-by-line audit of every shared handoff or establish generated/partial-source closure under future generator changes. The loader explicitly includes current source plus selected global-using/WPF generated files, not arbitrary generator execution. No claim of full product behavior, rendered UI behavior, arbitrary transitive independence, runtime dispatch independence, or acceptance beyond the finite direct-static guard is made. Re-review must retain these ceilings and inspect remaining relevant handoffs before CLEAR.

Known clerical follow-up belongs to the parent: author receipt `type: proof` must become supported `doc`; its integrated failure path must use `artifacts/atlas-five-gates/failed-slot-p1-01`, not `.artifacts/...`. These two acknowledged corrections are not the source veto.

## Class → sweep → derive → prevent

Class: DC-118, width changes between an exact contract and its transcription. Sweep: named-member admission versus overloads, exact D0 roots versus shared-file trivia, existing fresh-helper refusal, coexistence positives, and the real baseline. Derive: method names are broader than method declarations; containing files are broader than selected members. Prevent: the two executable counterexamples above are the required regression oracles. The parent owns central lesson-register changes and author fixes; the reviewer did neither. These findings are not claimed controlled until the permanent controls pass independent re-review.

# Handoff and instrumentation

The requested disposition is complete as a precise BLOCK. Repair and re-review remain with the Conductor/author. The reviewer writes only this receipt, own official audit append, own liveness, and local scratch/test output. The parent owns the serialized graph/audit derived union and official join. Local scratch remains untracked and must not be silently discarded or joined as product source.

Budget: 16 tool boundaries / 20 minutes declared; final measured count and automatic duration are recorded in the official review audit entry. No retry-until-green, no fan-out, no source repair, no native test, no budget expansion. Tool output truncation required narrower reads during grounding; truncated portions are not credited as observed evidence.

`AIDE_CONTRACT_LOG` was absent. No event destination was invented. Episode evidence: `docs/proof/d0-atlas-independence-review.md`.

Audit CLI correction: the first append was rejected for missing required shortname before writing an entry. The corrected invocation supplies it explicitly. This is a required-argument contract omission; the writer existing required-field rejection is the control. No successful append was repeated.
