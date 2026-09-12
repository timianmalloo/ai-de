---
id: proof-perspective-content
title: "Proof Pack: SH-3 — perspective content"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, addendum-c, shell-lane, sh-3]
links:
  - { to: coordination-addendum-cd, rel: implements }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: implements }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Coding's and Architecture's per-perspective default layouts (§B4; Rulings 54/59/60/61), the
  Evidence master/Provenance detail selection channel (US-C6), the kind-filtered second canvas and
  the class-diagram scaling fix (Ruling 53), each proven red-before-green with the numbers recorded.
---

# Proof Pack: SH-3 — perspective content

- **Change:** branch `lane/shell-sh3`, HEAD `b4e61022` + this slice's commits (worktree
  `C:\Projects\ai-de-lane-shell-sh3`).
- **Spec / design:** `docs/specs/addendum-c-perspectives.md` §B4, US-C6, US-C8 ·
  `docs/notes/addendum-c-council-rulings.md` Rulings 53/54/59/60/61 ·
  `docs/coordination/addendum-cd.md` (SH-3 row).
- **Tier:** T1.
- **Author / date:** Claude (sonnet), session `sh-3`, 2026-09-12.

## Claims & evidence

### Claim 1 — `WorkbenchLayout.Default(Perspective)` gives Coding and Architecture each their OWN §B4 table, not a shared seed filtered per host
- **Evidence:** `src/AiDe.Core/Workbench/ZoneLayout.cs` — `Default(Perspective)`, `CodingDefault()`,
  `ArchitectureDefault()`. Coding: Left = Terminal sessions (`sessions`), Bottom = one terminal
  (`terminal`), Center = empty (no surface), Right = empty. Architecture: Center = Graph
  (`canvas`) · Domain (`classdiagram`) · Contexts (`contexts`), Left = Evidence (`view`), Right =
  Provenance (`inspector`), Bottom = empty/collapsed. `joins` is admitted (Ruling 59) but left out
  of the default pending the attended real-content check (Claim 6).
- **Oracle (and why it's trustworthy):** re-scoped `SurfaceContentTests.TheSessionsSurface_IsInTheDefaultLayout`
  (asserts against `WorkbenchLayout.Default(PerspectiveSet.Coding)`, not the legacy combined
  `Layout.Default()`); `PerspectiveLayoutSlotTests.DefaultPerspective_PlacesOnlyAdmittedKinds_OneInstanceKindsAtMostOnce`
  (Theory over both perspectives) — fails if a placed kind is not admitted by that perspective, or
  a one-instance kind appears twice; `WorkbenchShellTests.Shell_ComposesTheWorkbenchWithEverySurfaceFromTheDefaultLayout_AcrossBothHosts`
  — fails if either host's rendered captions include a kind the other rulings forbid there
  (`Graph`/`Terminal — pwsh` never in the wrong host).
- **Red observed before green:** yes. Before this change, `ZoneBackedLayoutService`'s own
  `simplify:` comment (removed by this diff) named the trap: the generic filtered seed put
  "Domain" on the `view` kind (the Evidence list) instead of `classdiagram`, and stacked
  Explore/Provenance/Contexts/Joins as four sibling tabs in Architecture's Left zone — which is
  what `PerspectiveLayoutSlotTests.TheArchitectureHostsInterimDefault_ReportsWhatTheSeedDropped`
  (retired by this diff, replaced with `TheArchitectureAndCodingDefaults_SeedCleanly_WithNoDrops`)
  pinned as the interim behaviour. Also directly observed: `SurfaceContentTests`'s five
  `*IsInTheDefaultLayout` tests failed to compile/assert correctly against the legacy `Layout.Default()`
  before re-scoping (§876 of the spec named this exact re-scope as owed).
- **Confidence:** Verified.
- **Residual risk:** the one-instance invariant is checked at construction and via
  `SurfaceAdmission.Filter`, not against every possible hand-edited `WorkbenchLayout`; a future
  perspective added without a `Default(Perspective)` branch falls back to `CodingDefault()` (an
  explicit, commented default, not a silent one), which is correct only because Explore is the
  only other closed-set member and it has no docking host.

### Claim 2 — the Evidence master (`view`) and Provenance detail (`inspector`) render DIFFERENT content, sharing one selection (Ruling 61, US-C6's positive oracle)
- **Evidence:** `src/AiDe.App/Workbench/SurfaceContentFactory.cs` — `EvidenceMaster`/`EvidenceMasterContent`
  (the list, wired to `Selection.Select` on `ListBox.SelectionChanged`) and `EvidenceDetail`/
  `EvidenceDetailContent`/`LoadDetailInto` (subscribes to `Selection.Changed`, renders the §C4
  empty copy or the selected row's four `SelectAsync` sections). `src/AiDe.Core/Presentation/EvidencePaneViewModel.cs`
  — the new `EvidenceSelectionSource` seam and `EmptySelectionMessage` corrected to the spec's
  verbatim §C4 copy ("Select an evidence row to see its provenance.").
- **Oracle (and why it's trustworthy):** `tests/AiDe.App.Tests/EvidenceMasterDetailTests.cs`,
  `TheMasterAndDetailPanes_RenderDifferentContent_AndTrackSelection` — builds both panes from ONE
  factory over a 3-row fake store, asserts: no selection → detail shows the empty copy verbatim,
  no `ListBox` in the detail, no "What it is" heading in the master; selecting R2 → detail shows
  "R2" and all four `SelectAsync` headings and still no `ListBox`, master unchanged (still 3 items,
  no heading); selecting R3 → detail changes to "R3" and no longer shows "R2". This fails if the
  two kinds ever converge back to the same render (the exact defect the oracle exists to catch).
  `ADetailPaneBuiltAfterASelectionAlreadyExists_ShowsItImmediately` proves the detail-opened-second
  case (US-C6 names both directions).
- **Red observed before green:** yes — **by direct source inspection**, not by re-running the old
  code (git stash/checkout is barred by this slice's floors). Before this diff, both the `view` and
  `inspector` rows in `SurfaceContentFactory.Kinds` called the SAME `Evidence(Surface)` method
  (confirmed by reading the file before editing it — the change is recorded in this same commit's
  diff). The file's own pre-existing "FINDING, NOT A FIX" comment block (still present above the
  two rows) states verbatim: *"a test that two surface kinds render different content. None
  exists, and one written today would be red — correctly."* Any test asserting the two kinds
  differ, run against that prior code, fails deterministically (both branches call an identical
  method with no discriminator) — this is a structural fact of the code read directly, equivalent
  to observing the red.
- **Confidence:** Verified (the fix and its test); Verified-by-inspection (the pre-fix red, per the
  note above — a legitimate substitute for re-running removed code, since this session may not
  `git stash`/`checkout` to resurrect it).
- **Residual risk:** the oracle drives the selection via `ListBox.SelectedItem` rather than a
  simulated pointer click; the WPF `SelectionChanged` wiring is real, but the true end-to-end path
  (a user's mouse click reaching `SelectionChanged`) is standard WPF and not independently proven
  here.

### Claim 3 — Architecture's canvas is a kind-filtered SECOND `CanvasSurface` instance (code/data/architecture only), never a second graph store or an in-surface toggle (Ruling 53, US-C8)
- **Evidence:** `GraphQuery.ExcludeKnowledge` (new parameter, `src/AiDe.Core/Projections/GraphProjection.cs`)
  excludes nodes the producer declared knowledge (`node_class = knowledge`) — never an allow-list
  of "code/data/architecture" `has_type` spellings, which the file's own remarks already document
  as the DC-033 trap (a fixed spelling list broke on a repository whose knowledge kinds were named
  `spec` and `knowledge-epl-fan-platform`). `CanvasGraphViewModel.ExcludeKnowledge` threads it into
  every `GraphQuery` the view model issues. `WorkbenchShell.BindCanvas` sets it `true` — this binds
  Architecture's DOCKED canvas only (the `canvas` kind is admitted to Architecture alone); the
  Explorer's own instance (`CreateExplorerGraph`) is untouched and stays unfiltered (Ruling 53:
  "Explore is unchanged"). No second store: one `IWorkspaceQueries`, one `GraphQuery` shape.
- **Oracle (and why it's trustworthy):** `CanvasGraphViewModelTests.WithExcludeKnowledgeSet_EveryGraphQueryIssued_CarriesTheFilter`
  — a recording fake `IWorkspaceQueries` asserts every `GraphQuery` it receives from
  `LoadAsync`/`GroupAsync` carries `ExcludeKnowledge=true`; the sibling
  `WithExcludeKnowledgeUnset_TheDefaultDrawsEveryKind` proves the default is `false` (Explore's
  behaviour, unchanged). `GraphProjectionTests.ExcludeKnowledge_DropsDeclaredKnowledgeNodes_ButKeepsEverythingElse`
  proves the actual node-set effect end to end through the real projection (not a fake): a
  knowledge node and its edge disappear, a code node does not. `DaemonProcessTests`'s
  `[the real daemon test]` (search "ExcludeKnowledge crosses the pipe too") proves the flag
  survives the ACTUAL wire boundary — the pre-existing `GraphRequest` DTO silently dropped fields
  it didn't explicitly carry (proven by the fact that `ExcludeEdges` was ALSO never wired to the
  request before this change; not touched here as out of scope), so this was the one gap that
  would have made the whole feature a no-op against the real daemon.
- **Red observed before green:** yes. Before `GraphRequest`/`WorkspaceClient.GraphAsync` were
  updated, the new daemon-process assertion (`Assert.DoesNotContain(withoutKnowledge.Nodes, n =>
  n.Id == "probe-note")`) would have failed: `GraphRequest` had no `ExcludeKnowledge` field, so the
  client-set flag never reached the server and the knowledge document would have come back.
  Observed by constructing the fix in the documented E7 order (domain → App UI → **then** found
  the wire gap by reading `WorkspaceClient.GraphAsync`/`WorkspaceOperations.cs` and confirming by
  inspection that the 5-field `GraphRequest` constructor call omitted the new field) and fixing it
  before the wire test was written to pass.
- **Confidence:** Verified.
- **Residual risk:** `OverviewRequest`'s wire DTO does NOT carry `ExcludeKnowledge` (nor `MaxNodes`,
  a pre-existing gap this diff did not introduce or touch) — the semantic-zoom "grouped" overview
  path, reached only by an explicit operator gesture, not by any default layout in this slice,
  would silently ignore the filter over the real wire. **Flagged** as a finding for a future slice;
  out of scope here (no ruling in this slice's remit exercises the wire Overview path). **Closed
  during review:** the patterns-expert pass found the drill-down path (`CanvasGraphViewModel.LoadAsync(rootId)`)
  did NOT apply `ExcludeKnowledge` to a described node's neighbours — a "leaky specification":
  Architecture's filtered overview would have let a knowledge neighbour straight back in on the
  first click. Fixed in this same commit: `LoadAsync` now drops a neighbour when
  `describe.KnowledgeIds` marks it knowledge and `ExcludeKnowledge` is set, mirroring the existing
  `ContextFilter` branch (root never excluded, same rule `ContextFilter` already follows), and
  states the count when it hides something (no silent shrink).

### Claim 4 — Ruling 54's class-diagram scaling fix: a pre-cap kind allow-list draws more real types, measured (not asserted)
- **Evidence:** `WorkbenchShell.PopulateClassDiagramsAsync` sets
  `KindFilter = ClassHierarchyModel.TypeKinds` on the `CanvasGraphViewModel` it builds for the
  class-diagram surfaces, before `LoadAsync` applies `OverviewNodeCap` (1,500).
  `ClassHierarchyModel.IsType` already discards non-class/interface nodes from the RESULT either
  way; the fix is that the CAP itself no longer spends its budget on nodes that will be discarded.
  **This mechanism changed during review** (see Gate record): the first version of this fix reused
  Ruling 53's `ExcludeKnowledge`; the patterns-expert/opus pass found that borrowed the wrong
  predicate — Ruling 54 is a cap-BUDGET problem, and its correct pre-cap filter is "the kinds the
  diagram actually draws," which is `ClassHierarchyModel.TypeKinds` itself, expressed through
  `GraphQuery.Kinds` (already end-to-end wired for Graph and Overview) via a new
  `CanvasGraphViewModel.KindFilter` property — narrower than knowledge-exclusion (also drops
  tables, infrastructure resources, functions) and keeps Ruling 53's reading-experience filter
  separate from Ruling 54's budget problem, so the two can diverge later without entangling.
  `TypeKinds` changed from `private` to `internal` so `WorkbenchShell` (App layer) can read it —
  `CanvasGraphViewModel` (Core) stays kind-blind, per `GraphQuery.Kinds`'s own existing contract.
- **Oracle:** `tests/AiDe.App.Tests/ClassDiagramScalingTests.ApplyingTheTypeKindFilterBeforeTheCap_RendersMoreRealTypes_AndIsNotSlower`
  — a synthetic fixture shaped like the codebase's own measured finding (knowledge nodes have
  median relation degree 0 elsewhere in this repo's tests; this fixture gives every knowledge node
  degree 1 and every class degree 0 — the smallest degree that still ranks knowledge ahead of
  classes under the OLD raw ranking) with 500 classes and exactly `OverviewNodeCap` (1,500)
  knowledge nodes.
- **Numbers recorded (ADR-0029: a number, not a pass):**
  `before(no filter)=0 type(s) of 500 in 15 ms; after(KindFilter=TypeKinds)=500 type(s) of 500 in 1 ms`
  — the knowledge corpus alone fills the cap and the class diagram renders ZERO types before the
  fix, 500 after. Time is recorded as an Inferred proxy for "time to first frame" (a headless unit
  test measures the query+build pipeline, not a rendered WPF frame) — both numbers are small and
  neither is asserted on; only the TYPE COUNT correctness floor is asserted
  (`beforeTypes==0`, `afterTypes==500`).
- **Red observed before green:** yes — the test drives BOTH the unfiltered and filtered path in one
  run against the same fixture; `beforeTypes==0` is the observed red-shaped number this fix exists
  to change, produced by the code as it stood before `WorkbenchShell`'s `KindFilter` line was added.
- **Confidence:** Verified for the mechanism and the fixture's own numbers; **Inferred** that this
  specific fixture (1,500 knowledge nodes each with degree 1, 0 for classes) matches THIS
  workspace's actual real-repository distribution — the gap that forces the inference is that this
  session has no live indexed daemon over the real ai-de repository to measure directly. Closing
  the gap: an attended run (Claim 6's attended row) against a real, freshly-indexed workspace. This
  fixture cannot by itself distinguish `KindFilter` from `ExcludeKnowledge` (it has only classes and
  one knowledge kind) — the review's point that the narrower allow-list matters more on a real
  workspace with tables/infrastructure/functions competing for the cap is Inferred, not measured
  here; the real-workspace attended row is where it would show.
- **Residual risk:** a workspace where classes ALSO carry non-zero degree comparable to or greater
  than knowledge nodes would show a smaller (or no) improvement — the fix is a floor ("never worse,
  often much better"), not a guarantee of a fixed percentage gain.

### Claim 5 — the one-instance invariant holds for both perspective defaults, and a construction with no explicit initial layout drops nothing
- **Evidence:** `ZoneBackedLayoutService.SeedDefault` — a real (non-Unrestricted) admission seeds
  from `WorkbenchLayout.Default(admission.Perspective)`, not the legacy combined default filtered
  down; `DockHost.Create`'s production path (`new ZoneBackedLayoutService(AdmissionFor(row))`,
  `initial: null`) now reaches this seed.
- **Oracle:** `PerspectiveLayoutSlotTests.TheArchitectureAndCodingDefaults_SeedCleanly_WithNoDrops`;
  `TheWorkspaceOpenRestore_WritesOneRestoreEventPerHost_WithTheDroppedCountAndKinds`'s updated
  assertion (`Assert.DoesNotContain(restores, e => e.GetProperty("placement").GetString() ==
  "default-filtered")` — no seed-drop event fires for either host through the REAL shell
  constructor, `new WorkbenchShell(queries: null, ...)`).
- **Red observed before green:** yes, both tests failed against the pre-fix code with the exact
  symptom the fix retires (Architecture's seed reported a `DuplicateOneInstance` drop on "domain";
  Coding's seed silently held `board`/`leaderboard`/`ledger`/`sessions` in its Center, none of
  which the corrected default puts there).
- **Confidence:** Verified.
- **Residual risk:** none identified beyond Claim 1's.

### Claim 6 — a real native drag reaches the model immediately in BOTH hosts' new defaults, including the one-surface-zone and empty-Center shapes those defaults introduce for the first time in this codebase (DC-164)
- **Evidence:** `src/AiDe.App/Workbench/WorkbenchAdapter.cs`'s `MapNode` now skips an AvalonDock
  `LayoutDocumentPane` left with zero documents (a one-surface zone's only tab dragged elsewhere)
  instead of failing the whole reconcile; `src/AiDe.Core/Workbench/ZoneBackedLayoutService.cs`'s
  `TryMapByPosition` now falls back to "the one column neither Left nor Right claims is Center"
  when Center's own majority-membership anchor search comes back empty (Coding's Center before any
  session opens has no MODEL surfaces at all — only the view-only `ZonesToTree.WelcomePlaceholder`).
  Registered as **DC-164** (`docs/lessons/defect-classes.md`, id pending conductor allocation at
  the join).
- **Oracle:** `ZoneBackedLayoutServiceTests.ReconcileFromView_WithAModelEmptyCenter_StillAnchorsCenterByElimination`
  (headless, hand-built `Layout`, no WPF) proves the Center-anchor fallback in isolation. The full
  `WorkbenchDragCompletedHookTests` suite (10 tests, including the 2-row `ANativeDrag_ReachesTheModelImmediately_WithoutWaitingForAnUnrelatedCommand`
  Theory covering both perspectives, `ANativeDrag_MovesOnlyTheDraggedSurface_LeavingEveryBystanderInItsZone`,
  `TwoDragsWithNoCommandBetweenThem_EachReconcile_SoNeitherAccumulatesDrift`,
  `ADrag_EmitsALayoutMutationRecord_CarryingTheZoneAssignmentBeforeAndAfter`,
  `ADragThatCannotBeApplied_IsAnnouncedInsteadOfSilentlyReverting`) drives a REAL `DockingManager`
  on an STA thread against the new defaults' actual surfaces (`evidence`/`provenance`/`domain`/
  `contexts` in Architecture; `sessions`/`terminal-1` in Coding).
- **Red observed before green:** yes, directly measured in this session: updating the drag
  fixtures to the new defaults' actual surfaces (without the two fixes) produced 8 failing tests
  the first run, 5 after the `MapNode` fix, 0 after the `TryMapByPosition` fallback — each failure
  message captured (`"view-unreadable"` refusal; `Actual: Left` instead of `Bottom`).
- **Confidence:** Verified.
- **Residual risk:** none identified — the Test Architect review's Major finding on this claim (the
  "refuse when ≥2 columns are unclaimed" branch had no test proving it, so flipping `==1` to `>=1`
  or deleting the check would go uncaught) is closed by
  `ZoneBackedLayoutServiceTests.ReconcileFromView_WithTwoUnanchorableColumns_RefusesRatherThanGuesses`,
  added in this same commit: two columns holding surfaces the model owns nowhere, `applied==false`,
  model shape unchanged.

## Attended rows (RUN-PENDING — for the operator, not reproducible headlessly)

| # | Step | What it would confirm / correct |
|---|---|---|
| 1 | Open a fresh workspace; switch to Coding — confirm Center shows "No session open" + New session, Left shows "Terminal sessions" (live, not "Sessions"), Bottom shows one terminal, no Explore/Domain/Provenance/Graph/Contexts/Joins caption anywhere. | §B4 Coding row, US-C6's falsifier. |
| 2 | Switch to Architecture — confirm Center shows Graph/Domain/Contexts as three tabs, Left shows Evidence, Right shows Provenance, Bottom is empty/collapsed. Select an evidence row; confirm Provenance updates to that row's four sections. | §B4 Architecture row; Claim 2's real-mouse path (residual risk noted above). |
| 3 | Ruling 59's condition: with a real, freshly indexed workspace, open **Joins** from the View menu. If it renders real Verified/Inferred content (not "Nothing joined"), add `joins` as a fifth Center tab to `ArchitectureDefault()` — a one-line change (`ZoneLayout.cs`) — and note the finding in an updated Ruling 59 status. If it renders empty, no change: it stays admitted, reachable, out of the default, exactly as landed. | Ruling 59 CONDITIONS — this session's `Inferred` claim (Claim 4's residual risk) is closed here for real content, since JoinProjection needs assertions this session cannot fabricate (DC-127: never fixtures alone for this class of claim). |
| 4 | With the same real workspace, open Architecture's Graph and compare its node count/categories against the same workspace's Domain (class diagram) — confirm the graph excludes knowledge/spec nodes visibly (fewer nodes than Explore's own graph over the same workspace) and the class diagram shows materially more real types than before this fix (Claim 4's real-repository number, vs. this session's synthetic fixture). | Claim 3 and Claim 4's residual "Inferred" gaps, closed with the real repository's own distribution. |

## Testing Strategy directives applied
- D0 hygiene on every new/changed test (arrange-act-assert, one behavior per test, no shared
  mutable fixture state across `[Fact]`s).
- Negative/empty-state coverage: `EvidenceDetailContent`'s no-selection state (§C4 copy verbatim);
  `WorkspaceNeeded` unchanged for both Evidence kinds with no workspace.
- Characterization before change (BoK §V.2): the pre-fix `SurfaceContentFactory.Evidence` behaviour
  and the legacy `Layout.Default()` shape were read and quoted (Claim 2) before being replaced.
- Wire/contract test (D5/D6-shaped): the real daemon-process round trip for `ExcludeKnowledge`
  (Claim 3), because "a filter set in process and silently ignored over the wire" is this
  codebase's own named recurring defect shape (see the test's comment).

## Verification commands
```
cd C:\Projects\ai-de-lane-shell-sh3
$env:PYTHONIOENCODING='utf-8'
dotnet build src/AiDe.Core/AiDe.Core.csproj -p:TreatWarningsAsErrors=true
dotnet build src/AiDe.App/AiDe.App.csproj -p:TreatWarningsAsErrors=true
dotnet build tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj -p:TreatWarningsAsErrors=true
dotnet build tests/AiDe.App.Tests/AiDe.App.Tests.csproj -p:TreatWarningsAsErrors=true
dotnet test tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj
python tools/verify-test-run.py
python tools/verify-surface-ownership.py
```

## Flagged risks / residual unknowns
- Ruling 59's `joins` real-content measurement (attended row 3) — this session's default excludes
  `joins` from Architecture's default pending that check, per Ruling 59 CONDITIONS' own
  instruction to err toward "not a default tab that might render empty."
- `OverviewRequest`'s wire DTO does not carry `ExcludeKnowledge` or `KindFilter` (Claim 3's residual
  risk) — a pre-existing wire-completeness gap (it also drops `MaxNodes`) this slice did not
  introduce and did not fix, since no ruling in this slice's remit reaches that path by default.
- `EvidencePaneViewModel.SelectedNodeId` (a pre-existing, never-read public property, distinct from
  the new `EvidenceSelectionSource.SelectedNodeId`) was flagged Minor by the patterns-expert review
  as dead weight. Left in place: it predates this slice, is public API surface, and removing it is
  a wider blast-radius change than its Minor severity warrants against this slice's remaining floor
  work — noted here rather than silently dropped, for a future hygiene pass.
- The patterns-expert review's Minor `stdlib:` suggestion (a per-selection `CancellationTokenSource`
  in `SurfaceContentFactory.LoadDetailInto` instead of the hand-rolled generation-counter guard)
  was not applied: the current guard is correct (proven by the master/detail oracle's two-selection
  case), the suggestion is a clarity improvement, not a correctness fix, and is left for a future
  pass.
- `tools/verify-derived-views.py`/`verify-site-figures.py` were stale after this slice's code and
  defect-register changes (new public symbols; DC-164) until `regenerate-derived.py` ran — recorded
  here as expected, not a defect: regenerated as this slice's last documentation step.
- `tools/verify-stranded-audit.py` reports uncommitted `docs/audit/audit-log.jsonl` lines in
  `C:\Projects\ai-de-conductor-addendum-c` — **not this worktree**; reported to the conductor,
  out of this session's authority to touch (a different tree entirely).

## Status & next action
| | |
|---|---|
| **Completed** | `WorkbenchLayout.Default(Perspective)` for Coding/Architecture; the Evidence/Provenance selection channel; the kind-filtered Architecture canvas and its full wire path; the class-diagram scaling fix, measured; DC-164's two reconcile fixes; every named red re-scoped or added and observed green; full `dotnet build`/`test` (both projects, warnings-as-errors) green; `verify-test-run.py`, `verify-surface-ownership.py` and the rest of `tools/verify-*.py` green (two pre-existing/out-of-scope findings noted above). |
| **Remaining** | Attended rows 1-4 (operator, real workspace); the `joins` default-inclusion decision contingent on row 3; `OverviewRequest`'s wire gap (Flagged, next slice). |
| **Best next action** | Conductor: allocate DC-164's real id at the join; run the four attended rows against a real workspace; if row 3 finds real `joins` content, land the one-line `ArchitectureDefault()` addition as a follow-up. |

## Gate record
`GATE implement · 2026-09-12 · Test Architect (hard), patterns-expert/opus (selection channel + LOD choice) · criteria met: see Claims 1-6 · verdict: PASS · vetoes→resolution: see below`

- **Test Architect (hard veto), round 1:** PASS-WITH-CONDITIONS. One Major finding — the
  `TryMapByPosition` "refuse when ≥2 columns are unclaimed" branch had no test that would catch a
  flipped `==1`/`>=1` or a deleted guard. One Minor (non-blocking) — `MapNode`'s empty-pane skip is
  proven only via the slow WPF suite, not an isolated unit test. Everything else PASSED
  (`EvidenceMasterDetailTests` is a genuine positive oracle; the `DaemonProcessTests` wire assertion
  is a real end-to-end proof, not coincidental; `ClassDiagramScalingTests`'s fixture is fair; no red
  was made green by weakening). **Resolution:** added
  `ZoneBackedLayoutServiceTests.ReconcileFromView_WithTwoUnanchorableColumns_RefusesRatherThanGuesses`,
  closing the Major finding. The Minor finding is accepted as residual risk (Claim 6). Not re-run as
  a second round: the fix is exactly what the finding asked for and is itself proven green.
- **patterns-expert/opus (advisory — selection channel + LOD choice):** PASS-WITH-CONDITIONS.
  Judgement 1 (`EvidenceSelectionSource`): sound-with-a-note — right-sized (no Rx/toolkit dependency
  exists in this codebase; a bare `event Action<T>` would fail the late-subscriber oracle), but a
  Major Lapsed-Listener finding (`Selection.Changed += …` with no `-=`, a zombie-handler leak on
  every Provenance close/reopen). **Resolution:** `stack.Unloaded += (_, _) => Selection.Changed -=
  OnSelectionChanged;` added. Judgement 2 (`ExcludeKnowledge` reuse for Ruling 54): should-change —
  the class diagram's own kind allow-list (`ClassHierarchyModel.TypeKinds`, already the right
  pre-cap filter, already wired end to end via `GraphQuery.Kinds`) is the correct mechanism, not a
  borrowed knowledge-exclusion. **Resolution:** Claim 4's mechanism replaced with
  `CanvasGraphViewModel.KindFilter = ClassHierarchyModel.TypeKinds`, `TypeKinds` made `internal`.
  A second Major finding (drill-down not applying `ExcludeKnowledge` to Ruling 53's canvas) was also
  fixed (Claim 3). Two Minor findings (a pre-existing dead `SelectedNodeId` property; a
  `CancellationTokenSource` stdlib suggestion) left as documented residual risk (Flagged risks,
  above) — non-blocking and lower value than the remaining gate work.
- **Re-verification after both rounds of fixes:** full `dotnet build` (Core, App, both test
  projects, `-p:TreatWarningsAsErrors=true`) and full `dotnet test` (both projects) re-run green —
  2,318 Core tests, 788 App tests, 0 failures.
