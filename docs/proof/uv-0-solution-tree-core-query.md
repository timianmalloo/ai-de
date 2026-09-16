---
id: proof-uv-0-solution-tree-core-query
title: "Proof Pack — UV-0 Core SolutionTreeAsync census join"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [proof-pack, D-0, solution-tree, UV-0, census, ipc]
links:
  - { to: spec-understanding-views, rel: tested-by }
  - { to: design-solution-tree, rel: tested-by }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: tested-by }
review-by: 2027-03-15
summary: >-
  UV-0 walking skeleton: Core SolutionTreeAsync / IPC solution-tree, production
  UnanalysedLanguages.Skip, ResolveWithinWorkspace join, T5c omit off the wire.
  F* DTO tests: captured red on empty INTERNAL SolutionTree overload —
  Failed 19 / Passed 6 / Total 25 (docs/proof/uv-0-red-run.txt). T5a/T5b/T5c/Cancel
  are in that fail list. Green: Core+App SolutionTree filters (see body).
---

# Proof Pack: UV-0 SolutionTreeAsync

- **Change:** branch `understanding-views-core-query`
- **Spec / design:** `docs/specs/understanding-views.md` US-T1–T7 / T5a–c DTO · `docs/design/solution-tree.md` · ADR-0038
- **Tier:** T2
- **Author / date:** `/implement` UV-0, session `understanding-views-core-query`, 2026-09-15

## Claims & evidence

### Claim 1: IWorkspaceQueries declares SolutionTreeAsync; IPC id is solution-tree
- **Evidence:** `Catalog_NamesSolutionTreeAndDoesNotMapOntoOverviewOrGraph` passed. Const `WorkspaceOperations.SolutionTree == "solution-tree"`, not `overview`/`graph`. `Handle<SolutionTreeQuery>` registered. `WorkspaceClient.SolutionTreeAsync` sends the query record.
- **Oracle:** catalog test fails if the operation is mapped onto overview/graph or kebab-id drifts.
- **Red observed before green:** compile tax on `StubQueries` / Fake before the method existed; catalog test passed once the const landed.
- **Confidence:** Verified
- **Residual risk:** UV-1 kind row still absent (AR3).

### Claim 2: Fake refuse, StubQueries compile, EveryOperationFitsTheFrameTests covers the method
- **Evidence:** `FakeWorkspaceQueries_RefusesSolutionTreeAsyncByName` throws `NotSupportedException` naming `SolutionTreeAsync`. `CanvasGraphViewModelTests.StubQueries` implements the method. `EveryReadOperationIsCoveredByThisTest` and `NoOperationCanBuildAResponseTheTransportWouldRefuse` passed with `AtCeiling` entry.
- **Oracle:** reflective census of `IWorkspaceQueries` fails if AtCeiling omits the new method; Fake empty-success would hide a wrong call site.
- **Red observed before green:** yes — interface addition without AtCeiling is the control's failing input; AtCeiling was added in the same red slice as the stub.
- **Confidence:** Verified
- **Residual risk:** AtCeiling Hostile() store has no huge on-disk census; shrink is proven by `HostileCensus_ShrinksUnderTheFrame`.

### Claim 3: F* DTO US-T1–T4 (grain, indexed-parent, unindexed_probe, production skip bin)
- **Evidence:** `StarDto_IndexedParentFileArtifact_UnindexedProbe_BinAbsentWithSkipCount`. `src` coverage IndexedParent; `src/Program.cs` file-artifact; `unindexed_probe` Unindexed; no `bin` node; `SkipListedDirectoriesOmitted >= 1`. `ProductionSkip_ContainsBinWithoutATestInjectedSet` reads `UnanalysedLanguages.Skip` (same instance). Collapse, directory-valued, two-folder parent tests passed.
- **Oracle:** empty stub returned `Nodes = []` so Folder() failed (collection empty).
- **Red observed before green:** yes. Captured internal-stub run: **Failed 19, Passed 6, Total 25** (`docs/proof/uv-0-red-run.txt`). `StarDto_…` is in that fail list.
- **Confidence:** Verified
- **Residual risk:** skip-count chrome copy is UV-1 UI.

### Claim 4: T5a/b/c DTO — IO, permission, named omit off the wire
- **Evidence:** T5a hook throws `IOException` on `io_probe` → Disclosure Not recorded cause Io, no node, unindexed_probe remains. T5b `UnauthorizedAccessException` on `omit_probe` → Permission, skip-count ≥ 1. T5c internal omit `{omit_probe, omit_probe_2}` → those paths absent, `OmittedByCap >= 2`, Disclosure `Omitted (N)` Count equals the field, unindexed_probe Unindexed. `QueryJson_HasNoDropRelativePathsField`. Extra JSON `dropRelativePaths` does not omit. `GenericCapBelowCount_MayDropUnindexedProbe_AndThereforeIsNotT5c` documents the forbidden arrange.
- **Oracle:** empty stub had no disclosures / no unindexed_probe. T5c fails if omit is on the query or if integer cap is used as the arrange.
- **Red observed before green:** yes — `NamedOmit_…`, `IoShortfallOnIoProbe_…`, `PermissionShortfallOnOmitProbe_…`, `CancelMidWalk_…` are in `docs/proof/uv-0-red-run.txt` (internal empty stub).
- **Confidence:** Verified
- **Residual risk:** T5b real ACL (D4 preferred) used the enumerator hook; visual-tree is UV-1.

### Claim 5: T6 Python/TS honesty, ancestor 3c, T7 Bicep, reparse, orphans, hostile `..`
- **Evidence:** Python scope `declared_at = pkg` → pkg IndexedParent, zero `.py`/`.ts` file-artifacts, exact US-T6 copy. `outer` of indexed `outer/inner` is IndexedParent. Bicep `main.bicep` under `infra`; unresolvable filename discloses, no minted folder. Junction `junction_probe` not descended, ReparsePoint disclosure. File under omitted parent absent. Hostile `../outside.cs` discloses, no invented folder.
- **Oracle:** each test's failing input is in the design table (fake per-file rows, parent Unindexed, `.bicep` as folder, followed junction).
- **Red observed before green:** yes — PythonTs and Bicep tests in the 19-fail run (empty Nodes).
- **Confidence:** Verified
- **Residual risk:** junction fixture uses `mklink /J` (Windows) / symlink elsewhere.

### Claim 6: Frame shrink and IPC round-trip
- **Evidence:** `HostileCensus_ShrinksUnderTheFrame` — 3000 long-id files, `OmittedByCap > 0`, payload ≤ `IpcFraming.MaxFrameBytes`. `SolutionTree_AgreesWithTheInProcessProjection` round-trips Nodes/Disclosures/skip/cap/revision. Enums travel as declared names (`FileArtifact`, not kebab/number).
- **Oracle:** INV-0003 class; record-level `Assert.Equal` on `IReadOnlyList` is reference equality (existing daemon idiom compares fields).
- **Red observed before green:** HostileCensus asserted `OmittedByCap > 0` against the empty stub. Daemon test added after green of the DTO suite.
- **Confidence:** Verified
- **Residual risk:** production caps 2000/5000 remain Inferred. F* is well below them (`StarCensus` vs `DefaultMax*`); that is not a production cardinality.

### Claim 7: Telemetry tags, no path; cancel is OCE
- **Evidence:** `SolutionTreeSpan_EmitsCountsNotPaths` — tag `projection=solution-tree`, census/file/skip counts, outcome ok, no `path` key, workspace root absent from tag values. `CancelMidWalk_ThrowsOperationCanceled_NotAPartialTree`.
- **Oracle:** fails if a path/prompt tag is added or cancel returns empty success.
- **Red observed before green:** yes — stub emitted no span / did not invoke the hook.
- **Confidence:** Verified
- **Residual risk:** existing `TelemetryTests.NoSpanAttribute_CarriesAPathPromptOrSourceText` does not itself call SolutionTree.

## Test coverage of the boundary set

| Boundary | Test |
|---|---|
| Empty / missing root | Compute returns no invented folders (no dedicated empty-UI test — UV-1 B6) |
| Max / hostile census | `HostileCensus_ShrinksUnderTheFrame`; `EveryOperationFitsTheFrameTests` |
| Malformed / extra JSON | `QueryJson_HasNoDropRelativePathsField`; extra `dropRelativePaths` ignored |
| Hostile `..` | `HostileDotDot_DisclosesAndDoesNotInventAFolder` |
| IO / permission / reparse | T5a, T5b, junction test |
| Cancel | `CancelMidWalk_ThrowsOperationCanceled_NotAPartialTree` |
| Skip fail-closed | production Skip contains bin; F* bin absent without injected set |
| T5c forbidden arrange | generic cap drops `unindexed_probe`; named omit does not |

## Red-before-green (the run)

**Captured red (Verified, 2026-09-15):** internal `ProjectionService.SolutionTree(query, omit, census, ct)` returns empty `Nodes` and does not run the cancel hook. Filter `FullyQualifiedName~SolutionTreeProjectionTests`. Log: `docs/proof/uv-0-red-run.txt`. Stub **reverted**; not committed.

```
Failed!  - Failed:    19, Passed:     6, Skipped:     0, Total:    25
```

T5a (`IoShortfallOnIoProbe_…`), T5b (`PermissionShortfallOnOmitProbe_…`), T5c (`NamedOmit_…`), and `CancelMidWalk_…` are in the fail list. A prior public-1-arg stub produced 14/11 and is superseded.

**Captured green (Verified, 2026-09-15):** `docs/proof/uv-0-green-run.txt` — Core `~SolutionTree` **26 pass**; App `~SolutionTree` **15 pass**.

## E7 surface list (this slice)

| Surface | Done |
|---|---|
| store | no write; FilesToSearch uncapped + AllScopeLocations |
| model | `(path, kind)` records |
| service | `IWorkspaceQueries.SolutionTreeAsync` |
| projection/wire | IPC `solution-tree`, two ints, frame shrink |
| client type | not this slice (UV-1) |
| UI | not this slice |
| compute reader | not this slice (NodeContent / Graph already exist) |

## Residual risk

- UV-1 visual-tree, kind row, activate (landed; native Ctrl+Enter Flagged).
- T5b hook vs real ACL. Caps 2000/5000 Inferred.
- Other extractors' skip lists still disagree (N7 optional).
- ADR-0038 is Accepted (conductor recorded N6 PASS). This pack stays draft.

## Gate record

`GATE implement · 2026-09-15 · UV-0 Core query · T2 · red 19/25 (internal stub, captured) then green 26 Core + 15 App · pack draft`
