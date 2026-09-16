---
id: note-understanding-views-n10-patterns
title: "N10 D-0 Solution tree: Patterns Expert PASS; four named patterns hold"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, N10, D-0, solution-tree, patterns]
links:
  - { to: design-solution-tree, rel: relates-to }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: relates-to }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: relates-to }
  - { to: spike-d0-tree-toolkit, rel: depends-on }
  - { to: note-understanding-views-n10-design-acceptance, rel: relates-to }
  - { to: note-understanding-views-owner-n14, rel: depends-on }
review-by: 2027-03-15
review-suggested:
  - { by: design-solution-tree, on: 2026-09-15, reason: "N10 Patterns Expert PASS; wrap-not-twin belongs in the P-table; design stays draft" }
summary: >-
  N10 Patterns Expert (advisory, Adversary Mode) on D-0 Solution tree: PASS.
  Query-time join, derived menu (ADR-0030), WPF TreeView, wrap-VM-not-Presentation-twin
  are the right named patterns. Soft veto not raised. Blast radius: no design Accepted,
  no D-1, no main; SRE still unsat.
---

# N10 D-0 Solution tree: Patterns Expert PASS; four named patterns hold

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** **Verified** on the four named patterns against opened design, ADR-0038, ADR-0030, N7 spike, and the landed types; **Nit** on house "materialized" wording
- **Made during:** N10 design adversarial, Adversary Mode, session `understanding-views-n10`, 2026-09-15. Subject HEAD `ac584d9b08b3bf1a085aaf99f3454faf83e45911` (`understanding-views`). Reviewer did **not** author `docs/design/solution-tree.md`.

This receipt is **Patterns Expert (advisory)** only. Test Architect + Simplifier already sat (`note-understanding-views-n10-design-acceptance`). SRE is unsat. A partial panel does not close the N10 gate (`docs/plans/understanding-views.md` fan-out join). This note does not accept the design.

```
PERSONA: patterns-expert   MODE: Adversary   TIER: T2
VERDICT: PASS   (advisory)
FINDINGS:
  - [Minor] (Verified) Wrap-VM-not-Presentation-twin is the right client-type pattern and is named in VM nesting prose (`design-solution-tree` `:317`), not as its own P-table row. P10 names HierarchicalDataTemplate only.  evidence: SolutionTreeNodeItem wraps SolutionTreeNode (`SolutionTreeSurface.cs:20-28`); `NodeItem_CarriesEverySolutionTreeNodeField_…` `Assert.Same(node, item.Node)`; FieldsSurvive enumerates `AiDe.Core.Presentation` only (`FieldsSurviveTheClientBoundaryTests.cs:155-157`)  fix: on the next design edit, add P13 **Adapter (wrap Core DTO; no Presentation twin)** citing `IWorkspaceQueries` remarks `:16-18` and DC-016. Do not mint `SolutionTreeRow` to feed the Presentation enumerator.
  - [Nit] (Verified) P1 reuses the house `ProjectionService` name "CQRS / Materialized Read Model" (`:192-195`). For D-0 the census is disk-now Type-1, not a stored view. The same row rejects `folder_dim`, so this is wording, not a second store.  evidence: design P1 vs P2; ADR-0038 §5  fix: keep P1 as the house CQRS slot; spell "rebuildable, not stored" so a later slice cannot "complete" to `folder_dim` on the word materialized.
  - [Nit] (Verified) P9 names "Allow-list column + derived join (ADR-0030)". House C8 / ADR-0030 also name **descriptor rows / Smart Enum** and **Table-Driven Method**. The join is the right one.  evidence: ADR-0030 Decision 2–3 `:73-87`; kind row `SurfaceContentFactory.cs:178-182`; `MainMenuBuilder.cs` has zero `solution-tree` strings  fix: cite Smart Enum on the next design edit. Do not hand-write a menu string.
  - [Nit] (Verified) P4 says call `ResolveWithinWorkspace`. The join calls `CandidateWithinWorkspace` then `File.Exists` (`ProjectionService.cs:709-710`). `ResolveWithinWorkspace` is exactly that pair (`:1352-1356`). Not a second containment algorithm.  fix: call the named helper, or leave as equivalent. Do not add a third path function.
  - [Nit] (Verified) Cap → named omit → orphan-drop → byte-shrink is a load-bearing pipeline written as a numbered list (`design-solution-tree` `:288-296`). That is **Pipes and Filters**. Already specified; naming it would make a reorder a pattern break.  fix: optional P-table row. Not a missing control.
CLEARS-THE-VETO: n/a (advisory) — pattern must also clear the Simplifier
RESIDUAL RISK: a later slice mints `folder_dim` from P1's "materialized"; mints `SolutionTreeRow` to satisfy FieldsSurvive; adds `DropRelativePaths` / public census Strategy; copies Graph's proportional shrink onto this total order; puts the tree on Left.
```

**Handoff:** mutual check with the Simplifier — their N10 receipt already **PASS** (`note-understanding-views-n10-design-acceptance` Simplifier block). This lens agrees: remaining complexity is defended. Soft veto **not** raised.

## Evidence opened (not memory)

| Artifact | Opened | Used as |
|---|---|---|
| `docs/design/solution-tree.md` | §Patterns P1–P12 `:219-238`; VM nesting `:313-317`; grain; E7 | subject |
| `docs/adr/0038-d0-solution-tree-census-and-kind.md` | Decision; join; one query record; skip; no second store | authority |
| `docs/adr/0030-perspective-registry-and-allow-lists.md` | Decision 2–3 `:73-87` | derived menu |
| `docs/spikes/d0-tree-toolkit/RESULT.md` | Chosen toolkit; HierarchicalDataTemplate; VM nest | freeze |
| `docs/notes/understanding-views-n10-design-acceptance.md` | TA+Simplifier PASS-WITH-CONDITIONS; panel incomplete | sibling |
| `docs/notes/understanding-views-owner-n14.md` | no D-1; no `main`; spec stays draft | stop |
| `src/AiDe.Core/Projections/SolutionTreeProjection.cs` | grain records; `Pattern: Query-time join`; Compute; WalkCensus reparse comment; `ShrinkRankedPrefix` | P2, P5, P7 |
| `src/AiDe.Core/Projections/ProjectionService.cs` | CQRS remarks `:192-195`; `SolutionTree` join `:687-744`; Graph shrink `:618-665`; `ResolveWithinWorkspace` `:1352-1356` | P1, P2, P4, P7 |
| `src/AiDe.Core/Projections/IWorkspaceQueries.cs` | `SolutionTreeAsync` `:115`; result types are Core's own `:16-18`; not Overview/Graph `:110-113` | seam |
| `src/AiDe.App/Workbench/SurfaceContentFactory.cs` | `Pattern: Derived menu (ADR-0030)` row `:178-182` | P9 |
| `src/AiDe.App/Workbench/SolutionTreeSurface.cs` | wrap VM `:16-28`; `Nest` `:83-123`; `TreeView` + `HierarchicalDataTemplate` `:427-492` | P10, wrap |
| `tests/Shared/FakeWorkspaceQueries.cs` | `SolutionTreeAsync` refuse `:61-66` | P11 |
| `tests/AiDe.App.Tests/Workbench/PerspectiveMenuTests.cs` | allow-list row `("solution-tree", ["architecture"], One)` `:234` | P9 |
| `tests/AiDe.App.Tests/SolutionTreeSurfaceTests.cs` | wrap identity `:237-254`; Nest forest `:92-116` | wrap |
| `tests/AiDe.Core.Tests/FieldsSurviveTheClientBoundaryTests.cs` | Presentation-only enumerator `:155-157`; no `SolutionTreeRow` | wrap vs twin |
| `src/AiDe.App/Workbench/MainMenuBuilder.cs` | grep `solution-tree` = 0 | P9 rejected alt |

## The call

**PASS.** The four named patterns the panel asked about are the right ones. No cargo-cult. No invented load-bearing idiom that should have been a catalog name instead of this shape. Soft veto on unjustified complexity is **not** raised. Unresolved Blocker count: **0**. Unresolved Major count: **0** (the wrap finding is a table-row gap, not a wrong pattern).

`docs/design/solution-tree.md` **stays `status: draft`**. This lens does not mark it Accepted. Owner N14 forbids D-1 and `main`.

### The four (right)

| Asked | Design name | Call | Confidence |
|---|---|---|---|
| Query-time join | P2 Query-time join (DM7; ADR-0038) | **Right.** Census emission ⋈ latest-generation files. Not a stored census. Not path-split folders. Not Overview/Graph reused. | **Verified** — `SolutionTreeProjection` remarks `:51-57`; `ProjectionService.SolutionTree` walks `FilesToSearch` then `Compute`; `IWorkspaceQueries` `:110-113` forbids mapping onto Overview/Graph |
| Derived menu ADR-0030 | P9 Allow-list column + derived join | **Right.** One `solution-tree` row, `Perspectives` = `{Architecture}`, `Instances.One` → Show, `Entry` = `Derived("_View")`. Menu is `PerspectiveMenu.For`. | **Verified** — kind row `:178-182`; PerspectiveMenuTests `:234`; MainMenuBuilder has no `solution-tree` string |
| WPF TreeView | P10 HierarchicalDataTemplate over a VM-nested forest (N7 freeze) | **Right.** Native `TreeView` + `TreeViewItem`. `HierarchicalDataTemplate.ItemsSource` → `Children`. Custom `ListView`/`ItemsControl` (wrong UIA) and WebView2 rejected by the spike. | **Verified** — `BuildTree` `:427-492`; spike RESULT chosen toolkit |
| Wrap VM not Presentation twin | VM nesting `:317` (not a P-row) | **Right.** `SolutionTreeNodeItem` holds the Core `SolutionTreeNode`. `Children` / `IsStale` are presentation. Coverage is not recomputed. `SourceRevision` stays on the result. A Presentation `SolutionTreeRow` would copy identical fields — the defect DC-016 exists to catch, and the house already forbids a parallel view-type set (`IWorkspaceQueries` `:16-18`). | **Verified** — wrap type `:20-28`; `Assert.Same`; FieldsSurvive scanner cannot see App types, which is why a twin would be the wrong fix |

P1 (house CQRS slot) and P2 (disk ⋈ facts) are layered, not duplicates. Graph is CQRS without a disk join. D-0 needs both names.

### Ladder (L1) — climbed, not skipped

YAGNI (no `folder_dim`, no public census/skip types, no Request twin, no Zone freeze) → reuse (`IWorkspaceQueries`, `UnanalysedLanguages.Skip`, containment helper, EnvelopePurge-class reparse test, Graph frame budget, ADR-0030 menu, WPF `TreeView`) → stdlib `Directory.EnumerateDirectories` / `FileAttributes.ReparsePoint` → native WPF `TreeView` → no new dependency.

Rejected catalog names that would have been the wrong pattern:

| Rejected | Why it is the wrong pattern here |
|---|---|
| Strategy / `IDirectorySkipPolicy` | A public skip seam is a second policy. Consume the existing set (P3). |
| Public `IWorkspaceDirectoryCensus` | YAGNI. Internal `Func` is the Test Seam (`simplify:` on the ctor). |
| `SolutionTreeRequest` twin | Fields would be identical. Graph's Request exists because wire fields remap. P8. |
| `DropRelativePaths` on the query | Hides folders behind `Omitted (N)`. Named omit is projection/test-host only. |
| PEAA Materialized View as a table | Second definition of disk-now. Owner N1. P1's *rejected* alternative. |
| Graph proportional+recovery shrink | Ranking here is a total order. Prefix drop is enough (P7 `simplify:`). Opened Graph loop `:618-665` vs `ShrinkRankedPrefix` binary search `:228-267`. |
| Custom `ListView` / `ItemsControl` | UIA List/ListItem, not Tree/TreeItem. Spike F4. |
| WebView2 HTML tree | Wrong host. |
| Presentation `SolutionTreeRow` | Needless twin of Core's DTO. Wrap. |
| D Grounded Synthesizer | Would invent folders. P12 LOA none. |
| Path-split Composite from `artifact_path_id` | Owner forbade; Python/TS paths are not filesystem paths. Nest only by census-folders **present in the DTO**. |

### Invented names — keep, or promote

Nothing load-bearing is an unnamed invention of a catalog pattern.

| House phrase | Catalog name it already is | Action |
|---|---|---|
| Query-time join | DM7 on-the-fly projection (not ETL-time) | Keep P2 |
| Derived menu | ADR-0030 descriptor rows / Smart Enum + Table-Driven Method | Keep P9; cite Smart Enum (Nit) |
| VM-nested forest + HierarchicalDataTemplate | WPF Composite via `HierarchicalDataTemplate` | Keep P10 |
| Wrap Core node | Adapter without a DTO copy | **Promote to a P-row** (Minor) |
| Ranked prefix shrink | Prefix of a total order; `simplify:` already | Keep P7 |
| Refuse-by-default fake | Fail-fast test double (house Special Case of throw) | Keep P11 |
| Omit/cap/orphan/shrink order | Pipes and Filters | Optional name (Nit) |
| Reparse refusal class | Same-class reuse of EnvelopePurge's test | Keep P5 |
| One query record | Don't invent a wire twin | Keep P8 |

`// Pattern:` comments the design promised have landed: Query-time join on `SolutionTreeProjection`; Reparse refusal on the walk; Derived menu on the kind row. **Verified**.

### Soft veto (unjustified complexity) — not raised

The Simplifier already PASSed. Independent check:

- Internal enumerator `Func` vs public walker: cheaper. `simplify:` names the upgrade trigger.
- Ranked-prefix binary search vs Graph's proportional loop: **less** ceremony, justified by total order.
- App `Normalize` / `ParentOf` copies Core internals: public helper would be a new API; `InternalsVisibleTo` is Tests-only. Copy is the ladder.
- Dual-activate node menu plus `PreviewKeyDown`: existing `NodeViewKind` reuse, not a third IPC.
- Coverage 3c bottom-up: grain, not ceremony.

No cargo-cult extra type, extra seam, or extra framework.

## Alternatives dismissed

- **BLOCK.** No unresolved Blocker. A Blocker here would be a mis-applied catalog name (Strategy as the skip policy, stored Materialized View, Presentation twin, Overview-as-tree) or an invented public seam. None of those shipped.
- **PASS-WITH-CONDITIONS and hold N10 on the P-table gap.** Wrap-not-twin is already specified in VM nesting and tested by identity. Promoting it to P13 is a design-prose edit, not a missing pattern.
- **Require a Presentation `SolutionTreeRow` so FieldsSurvive can see it.** That is the cargo-cult the wrap exists to avoid. The App identity-wrap test is the matching control for an App VM.
- **Rename P2 to Materialized View.** Would invite `folder_dim`.
- **Copy Graph's shrink loop.** P7 already rejects it.
- **Admit D-1 / join `main` / mark the design Accepted.** Owner N14. Out.

## What the Conductor may do next

Sit SRE. Keep the design **draft**. Keep D-0 residuals on `understanding-views`. On a later design edit, add P13 wrap-not-twin (Minor). Do not treat this receipt as a full N10 join.

## What the Conductor must not do

Mark `docs/design/solution-tree.md` Accepted from this note. Admit D-1…D-6. Join `main`. Add `IDirectorySkipPolicy` / `IWorkspaceDirectoryCensus` / `SolutionTreeRequest` / `DropRelativePaths` / `SolutionTreeRow` / `folder_dim`. Convene this receipt as a full N10 panel close.

## Validation condition

Holds until a later N10 panel (this receipt + Test Architect/Simplifier + SRE) records a joint close **and** a non-author marks `docs/design/solution-tree.md` Accepted — or until Owner supersedes N14. The P13 table-row gap does not by itself reopen this call.

## Promotion rule

This note is the N10 Patterns Expert receipt. It does not supersede ADR-0038 or ADR-0030. It does not accept the design. A later accept of `design-solution-tree` links `relates-to` this note.

## Gate record

`GATE design · 2026-09-15 · N10 Patterns Expert (advisory) · Adversary Mode · subject ac584d9b docs/design/solution-tree.md · four named patterns Verified · verdict: **PASS** · design **stays draft** · soft veto not raised · unresolved Blocker 0 · panel incomplete (SRE unsat; Test Architect + Simplifier already sat) · authors did not self-clear · reviewer did not accept the design`
