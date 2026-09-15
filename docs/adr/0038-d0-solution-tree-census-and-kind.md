---
id: adr-0038-d0-solution-tree-census-and-kind
title: "ADR-0038 — D-0 Solution tree: one Architecture kind, one query-time census, no second store"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "understanding-views"
tags: [architecture, D-0, solution-tree, census, allow-list, ipc, adr-0030, understanding-views]
links:
  - { to: architecture, rel: implements }
  - { to: spec-understanding-views, rel: implements }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: refines }
  - { to: adr-0018-node-content-reader-contract, rel: depends-on }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: depends-on }
  - { to: note-understanding-views-n4-pass, rel: depends-on }
  - { to: note-understanding-views-owner-n1-disposition, rel: depends-on }
  - { to: note-understanding-views-owner-ruling, rel: depends-on }
review-by: 2027-03-15
summary: >-
  Admit D-0 as one Architecture-only SurfaceKind (solution-tree) whose payload is one new
  Core query-time census join (SolutionTreeAsync / IPC solution-tree). Coverage is
  indexed-parent | unindexed; Not recorded and Omitted (N) are Disclosure. Cap tests inject
  a named drop-set. Skip policy is consumed, not copied; the member survivor is N7.
  Status proposed — N6 council has not sat.
---

# ADR-0038: D-0 Solution tree — one Architecture kind, one query-time census, no second store

- **Status:** Proposed (2026-09-15). Authors do **not** self-clear. N6 architecture council is a later Conductor panel. Spec `spec-understanding-views` stays draft.
- **Date:** 2026-09-15
- **Deciders (Peer Mode, this turn):** Enterprise Architect (fit/longevity), Data & Persistence Architect (durable representation), Tech Lead (smallest correct). Session `understanding-views-architecture`.
- **Context spec/architecture:** `docs/specs/understanding-views.md`; `docs/architecture.md` §Understanding views / D-0; ADR-0030; Addendum C §A5 AR3.

## Context

Addendum C §A5 names D-0 Solution tree and forbids scaffolding it ahead of the slice that builds it (AR3). Owner admitted D-0 only; N1 forbade treating live facts as a folder tree; N4 PASS (`note-understanding-views-n4-pass`) unblocked architecture. The spec grain (N4 close, not the unclosed Owner Coverage quote) is:

> One tree node is exactly one Core-named workspace-relative path: **either** one indexed artifact (a file/document resolved from latest assertions) **or** one census folder (a directory Core observed).

Identity is `(path, kind)` with `kind ∈ {file-artifact, census-folder}`. Coverage of a census-folder is only `indexed-parent` | `unindexed`. Owner’s `not-recorded` is **Disclosure**, never a Coverage value and never a reason to mint a folder.

Forces in play:

- **AR3 / ADR-0030:** one kind row, `Perspectives` column `{Architecture}`, menu derived from the join of perspective rows and kind rows — never a second hand-written list. An empty Perspectives set fails the build test.
- **DC-022** (`IWorkspaceQueries.cs` remarks on `SearchContentAsync` / `NodeContentAsync`, opened): the App must not read workspace files.
- **Ruling 53:** no second graph store; one substrate, two surfaces.
- **DM7 / DM13:** census is derived disk-now; existing `node_dim`, `evidence_assertion_fact`, scope snapshots stay; no new stored aggregate this horizon.
- **LOA P1:** cheapest sufficient (one query + one kind). **P2:** determinism at the floor (derived menu, one skip policy, query-time join). **P5:** verification over plausibility (Disclosure vs Coverage).
- **US-T5c residual (Test Architect / N4):** F* requires a named drop set (`omit_probe/`, `omit_probe_2/`) while `unindexed_probe` survives. A prefix-integer cap plus alphabetical walk that drops `unindexed_probe` first is a forbidden arrange.

N1 inventory **[Verified]:** `IWorkspaceQueries` (`:20-105`) has no tree method; `OverviewAsync` groups identifier prefixes (`GraphOverview.cs`); `GraphAsync` returns graph nodes with no path. Live schema v1 has no `artifact_dim` / `folder_dim` (`WorkspaceSchema.cs:41-128`).

## Decision

We will admit D-0 as one Architecture-only `SurfaceKind` (`solution-tree`) whose payload is one new Core query-time census join (`SolutionTreeAsync` / IPC `solution-tree`), with Coverage two-valued, honest Disclosure, a test-overridable named drop-set for the cap, one consumed skip policy (member survivor Flagged for N7), and activate only through existing `NodeContentAsync` and `GraphAsync`/`DescribeAsync`. We will not store a census, walk disk from the App, reuse `OverviewAsync`/`GraphAsync` as the tree, scaffold D-1…D-6, or freeze the tree toolkit.

## Chosen shape (the remaining honest one)

Core, at tree-open, walks the workspace root on the daemon side of DC-022, emits census-folders minus the skip policy, joins latest-generation file-artifacts onto those paths, and returns a bounded `SolutionTreeResult`. The App projects that DTO. Folder identity comes only from the census emission. `declared_at` never mints a folder; it only widens Coverage of a census-emitted path to `indexed-parent`.

### 1. Kind admission (described, not implemented this turn)

When the **building slice** (shell-surface, after core-query reds) lands, add **one** row to `SurfaceContentFactory.Kinds` (`src/AiDe.App/Workbench/SurfaceContentFactory.cs:135-273` **[Verified]** — 18 rows today; `inspector` is retired). Do not edit that file in this architecture turn.

| Column | Proposed value |
|---|---|
| `Kind` | `solution-tree` |
| `Title` | `Solution tree` |
| `Summary` | Architecture-pane navigator of `(path, kind)` nodes over Core’s census join. |
| `Perspectives` | `{Architecture}` — the row’s `Perspectives` list contains only `PerspectiveSet.Architecture` (id `architecture`, `Perspectives.cs:72-73` **[Verified]**). Coding, Explore, and Coordination do not admit it. |
| `Instances` | `One` → derived entry is **Show Solution tree**, not New. |
| `Entry` | `Derived("_View")` |
| `Windowed` | `false` |
| `Zone` | not frozen here (design-slice; reachability from Architecture is required; do not replace `canvas`) |

ADR-0030 Decision 2–3 (quoted, not thinned): the allow-list is the `Perspectives` column; an empty set fails the build test; menu, palette, rail and routing are derived from the join of those two row sets — never a second hand-written list. `PerspectiveMenu.For` picks the row up. The US-C4 mutation test remains the oracle. Do not hand-write a menu string. Do not add D-1…D-6 rows (AR3).

Opened type note: ADR-0030 named `IReadOnlySet<string>`; the shipped row is `IReadOnlyList<Perspective> Perspectives` (`SurfaceContentFactory.cs:108-117` **[Verified]**). This ADR describes membership `{Architecture}` on that column, not a type change.

### 2. Census query (named; not implemented this turn)

**Method** (new on `IWorkspaceQueries`; do not reuse `OverviewAsync` / `GraphAsync`):

```
Task<SolutionTreeResult> SolutionTreeAsync(
    SolutionTreeQuery query,
    CancellationToken cancellationToken);
```

**IPC operation id** (new `WorkspaceOperations` const; catalog is `WorkspaceOperations.cs:130-158` **[Verified]**, not `IpcContract.cs`): `solution-tree` (kebab, matching `search-content`).

**Request / query / result types** (Core’s own, same rule as `WorkspaceClient` remarks `:28-31` — no parallel App DTO):

```
public sealed record SolutionTreeQuery(
    int MaxCensusFolders = SolutionTreeProjection.DefaultMaxCensusFolders,
    int MaxFileArtifacts = SolutionTreeProjection.DefaultMaxFileArtifacts,
    IReadOnlyList<string>? DropRelativePaths = null);

public sealed record SolutionTreeRequest(
    int MaxCensusFolders,
    int MaxFileArtifacts,
    IReadOnlyList<string>? DropRelativePaths = null);

public enum SolutionTreeNodeKind { FileArtifact, CensusFolder }

public enum CensusFolderCoverage { IndexedParent, Unindexed }

public enum SolutionTreeShortfallCause
{
    Io, Permission, Cap, UnresolvablePath, PythonTsPerFile
}

public sealed record SolutionTreeNode(
    string Path,
    SolutionTreeNodeKind Kind,
    CensusFolderCoverage? Coverage,
    string? NodeId,
    string? NodeKind);

public sealed record SolutionTreeDisclosure(
    SolutionTreeShortfallCause Cause,
    string Message,
    int? Count,
    string? Path);

public sealed record SolutionTreeResult(
    IReadOnlyList<SolutionTreeNode> Nodes,
    int SkipListedDirectoriesOmitted,
    int OmittedByCap,
    IReadOnlyList<SolutionTreeDisclosure> Disclosures,
    string SourceRevision);
```

Grain on the wire: one `SolutionTreeNode` is exactly one `(Path, Kind)`. `Path` is workspace-relative with `/` separators; workspace root is `""`. `Coverage` is non-null iff `Kind == CensusFolder`. `NodeId` is the activate handle for `file-artifact` (representative latest-generation subject, same choice as `StoreReader.FilesToSearch` `:242-263` **[Verified]**); null on census-folders. `NodeKind` is `node_dim.node_kind` / `has_type` for glyphs; glyph-to-kind chrome is N8, not this ADR.

**Join (query-time, not stored):**

1. Census: Core walks directories under the workspace root (same side of the boundary as `UnanalysedLanguages.Enumerate` / `SearchContentAsync`). A census-folder exists iff the walk emitted it and the skip policy does not name that directory’s name. Skip-listed directories are **not nodes**; they increment `SkipListedDirectoriesOmitted`.
2. File-artifacts: latest-generation assertions (`evidence_assertion_fact` joined to committed snapshot generation) whose resolved path is a **file** Core observed. Resolve C# / SQL / knowledge / fixture paths via `StoreReader.ScopeLocation` (`declared_at`, `:274-285` **[Verified]**) + `artifact_path_id`. Many assertions for one file path collapse to one node. Directory-valued assertions are not file-artifact nodes.
3. Coverage of census-folder `P`: `indexed-parent` iff at least one file-artifact joins under `P` **or** some scope’s `declared_at` equals `P`; else `unindexed` (non-expanding leaf).
4. Python/TS: extractors store `request.ScopeId` as `Provenance.ArtifactPathId` with null source location (`PythonExtractor.cs:399-404`, `TypeScriptExtractor.cs:780-785` per N1 **[Verified]**). Do **not** rewrite extractors this horizon. Zero `.py`/`.ts` file-artifact nodes from a second walk. If census emitted `P` and a Python/TS scope `declared_at` equals `P`, Coverage is `indexed-parent`. Disclosure copy (exact): `Python and TypeScript files are not listed individually. The scope folder is indexed.` Cause `PythonTsPerFile`.
5. Bicep filename-only: resolve via scope location onto an **existing** census-folder; never treat the filename as a folder. Else Disclosure `Not recorded` (cause `UnresolvablePath`), no node.
6. Unresolvable / hostile assertion paths: Disclosure, no invented folder.

**Honest shortfall (never a plausible empty tree):**

| Event | Coverage? | Disclosure |
|---|---|---|
| IOException on a census path | no node for the unobserved path | `Not recorded`, cause `Io` |
| UnauthorizedAccessException | no node for the denied path | `Not recorded`, cause `Permission` |
| Cap / named drop-set | named paths absent | `Omitted (N)` with integer N, cause `Cap` |
| Skip-listed directory | no node | chrome count `N skip-listed directories omitted` (N on the result; copy is UI) |
| Unresolvable assertion path | no node from that path | `Not recorded`, cause `UnresolvablePath` |
| Python/TS per-file | n/a | exact US-T6 copy, cause `PythonTsPerFile` |

IPC/daemon failure is **not** a Disclosure; it is the existing error+Retry on the query (US-T9). `UnanalysedLanguages.Enumerate` swallows IO/permission (`:97-103` **[Verified]**). Census **must not** reuse that enumerator as-is; it consumes the skip **policy**, and the walk’s catch emits Disclosure.

**Bounds:**

- Production defaults **[Inferred — gap: no measured folder-census cardinality this turn]:** `DefaultMaxCensusFolders = 2_000`; `DefaultMaxFileArtifacts = 5_000` (aligned with `GraphProjection.DefaultMaxNodes = 5_000` **[Verified]** `GraphProjection.cs:191`). Clamp like other projections (`ProjectionService` Clamp + ceiling). Retune the constants when core-query emits counts; do not change the drop-set seam to retune.
- Overflow of the integer caps is Disclosure `Omitted (N)`, cause `Cap`. Ranking for production overflow: keep the workspace root; keep indexed-parent folders before unindexed; keep shallower paths before deeper; then ordinal path. This ranking is **not** how tests arrange US-T5c.

**Cap / T5c named drop-set (test-overridable):**

`DropRelativePaths` is the T5c seam. Compared workspace-relative, `/` separators, no trailing slash, `PathComparison.ForThisFileSystem`. When non-null/non-empty, those census-folders are omitted **after** skip-filter and counted in `OmittedByCap`, regardless of walk order or integer cap. Production App callers pass `null`. US-T5c sets `DropRelativePaths` to `omit_probe` and `omit_probe_2` and a `MaxCensusFolders` high enough that `unindexed_probe` survives. A test that lowers `MaxCensusFolders` until an alphabetical prefix drops `unindexed_probe` is the forbidden arrange and fails the spec’s failing-input line.

IO/permission tests arrange a Core census walker seam (`IWorkspaceDirectoryCensus` or equivalent) to fail a named relative path that is not `unindexed_probe` or `bin`.

**Registration (N1 §7, opened):** new const + `SolutionTreeRequest` + `Register` arm on `WorkspaceOperations`; `WorkspaceClient.SolutionTreeAsync`; `LocalWorkspaceQueries` adapter; `FakeWorkspaceQueries` virtual refuse (so adding the method does not churn every stub). Daemon `WorkspaceOperations.Register(endpoint, projections)` already takes `ProjectionService` — add the arm there, not a second Register method.

**Instrumentation (IO1):** on the normal path, emit duration, census-folder count, file-artifact count, indexed-parent count, unindexed count, skip omitted, cap omitted, shortfall causes, error code. Degrade to “not recorded”, never a plausible wrong number. Latency is recorded, not CI-asserted (ADR-0029).

### 3. Skip set — consume one policy; do not freeze the survivor

Census takes **one** `IDirectorySkipPolicy` (or the unified type the N7 spike names). It does **not** copy a fourth `HashSet`.

Opened disagreeing lists **[Verified this turn]:**

| Site | Members (directory names) |
|---|---|
| `CSharpScopeDiscovery.Skip` `:31-32` | `bin`, `obj`, `.git`, `node_modules`, `artifacts` |
| `CSharpScopeDiscovery` knowledge walk `:151-154` | Skip **plus** `.vs`, `dist`, `build`, `out`, `__pycache__`, `.venv`, `venv`, `packages`, `artifacts` |
| `CSharpScopeDiscovery` Python walk `:296-299` | Skip **plus** `__pycache__`, `.venv`, `venv`, `.tox`, `site-packages` |
| `UnanalysedLanguages.Skip` `:48-52` | `bin`, `obj`, `.git`, `.vs`, `node_modules`, `artifacts`, `packages`, `dist`, `build`, `__pycache__`, `.venv`, `venv`, `.tox`, `target`, `vendor` |
| `ScopeFingerprints.Skip` `:99-102` | `bin`, `obj`, `.git`, `.vs`, `node_modules`, `artifacts`, `packages`, `TestResults` |
| `TypeScriptExtractor.SkippedDirectories` `:296-301` | `.git`, `node_modules`, `bin`, `obj`, `artifacts`, `publish`, `_framework`, `dist`, `build`, `out`, `.next`, `coverage` |
| `PythonExtractor.Files` `:409-412` | `__pycache__`, `.venv`, `venv`, `.tox`, `node_modules`, `.git`, `build`, `dist` |
| `KnowledgeExtractor.AllFiles` `:587-591` | `node_modules`, `bin`, `obj`, `.git`, `.vs`, `dist`, `build`, `out`, `__pycache__`, `.venv`, `venv`, `packages`, `artifacts` |
| `SqlSchemaExtractor.Files` `:367-370` | `node_modules`, `bin`, `obj`, `.git`, `packages` |

The `artifacts` disagreement is already named DC-022 in `CSharpScopeDiscovery.cs:26-30`. Picking the smallest list would flood Unindexed with `dist`/`build`/`.venv`. Picking the largest would hide directories some extractors still walk. **Survivor is Flagged.** N7 skip-set spike measures union vs each list on F* (`bin` must omit) and one real workspace; then existing walkers consume the one policy type. Core-query must not ship a copied HashSet while waiting; tests inject a policy that contains `bin`.

### 4. Activate — two existing paths, no third

| Gesture | Existing action | Seam |
|---|---|---|
| Primary (Enter) on file-artifact | View source (`NodeViewKind.Source`) | `NodeContentAsync` → admitted `codeviewer` (ADR-0018 / ADR-0025) |
| Secondary (Ctrl+Enter) on file-artifact | Reveal in graph (`NodeViewKind.GraphNeighbourhood`) | `GraphAsync` / `DescribeAsync` on the node’s `NodeId` |
| Indexed-parent census-folder | Expand / collapse | no content, no graph entity |
| Unindexed census-folder | Leaf; Enter/Right/Left do not expand | no children, no activate |

No Atlas types (`AiDe.Core.Understanding`). No third IPC. Reveal failure / View source failure → error + Retry on **that** surface; tree selection unchanged. App PROBE-FILE-READ: no `File.ReadAllText` / `OpenRead` on the workspace path.

### 5. Durable representation (DM13)

**Bounded contexts:** Evidence and Projection (payload); Shell presentation (surface). No new context.

**Aggregates / invariants:**

- **Solution tree projection** (query-time). Invariant: every visible node is a `(path, kind)` pair as grained; skip-listed directories are not nodes; a census-folder carries exactly one Coverage value; folders are never invented by splitting `artifact_path_id` or from `declared_at`; shortfalls are Disclosure; the App does not walk disk.
- **Perspective Layout** (existing). Invariant: D-0’s kind enters Architecture’s admitted set only in the slice that builds it.
- **Scope Snapshot** (existing). Untouched.

**Store:** existing `node_dim`, `evidence_assertion_fact`, `scope_generation_desired_fact`, `scope_snapshot_committed_fact` (`WorkspaceSchema.cs:53-128` **[Verified]**). Census is derived disk-now (DM7). **No `folder_dim`, no census fact table, no second graph store this horizon.** Python/TS `ScopeId`-as-path extractor rewrite is **cut** (Owner N1). Analytical “tree” is this projection, never a second ODS.

## Alternatives considered

- **Reuse `OverviewAsync` as the tree:** rejected — Owner forbade; wrong grain (`GraphOverview` identifier-prefix clusters, `MaxClusters` omission, no unindexed folders) **[Verified]** N1 `:78`.
- **App walks disk for unindexed folders:** rejected — DC-022; PROBE-APP-ENUM fails.
- **Stored `folder_dim` / census tables this horizon:** rejected — Owner N1; second definition of disk-now; schema for one consumer (YAGNI).
- **Path-split folders from `artifact_path_id`:** rejected — Owner forbade; C# paths are project-relative; Python/TS are not paths.
- **Atlas / `src/AiDe.Core/Understanding/**` as D-0:** rejected — Owner forbade; different programme; PROBE-ATLAS.
- **Scaffold D-1…D-6 kind rows:** rejected — AR3; ADR-0030 mutation test exists to punish the second list.
- **Freeze WPF `TreeView` in this ADR:** rejected — N7 Spike Protocol; spec non-goal 14.
- **Two ADRs (kind vs query):** rejected — N4 close asked for one unless they cannot share; they share: one kind whose only payload is this query.
- **Reuse `UnanalysedLanguages.Enumerate` wholesale:** rejected — it swallows IO/permission; US-T5a/b require Disclosure.
- **Integer-only cap as the T5c arrange:** rejected — Test Architect residual; alphabetical prefix can drop `unindexed_probe`.
- **LOA D (Grounded Synthesizer) inventing folders:** rejected — a model must not mint Coverage.

## Consequences

- **Positive:** D-0 can be built as a walking skeleton (Core query reds, then one kind row) without a schema migration, without a second store, and without a hand-written menu. Unindexed, skip-omission, and Not-recorded stay distinct. AR3 holds for D-1…D-6.
- **Negative / accepted:** Python/TS files are not listed individually this horizon (disclosed). Skip-set member list is Flagged until N7. Production folder-cap number is Inferred until measured. `DropRelativePaths` on the query is a test seam a hostile client could also set — same class as `GraphQuery.MaxNodes`; Disclosure still fires.
- **Follow-ups / new risks:** N6 council (do not self-clear). N7 toolkit spike. N7 skip-set survivor spike. N8 `ui-design`. Core-query then shell-surface. `EveryOperationFitsTheFrameTests` must gain a `SolutionTreeAsync` case when the method lands (reflective census of `IWorkspaceQueries` **[Verified]** `EveryOperationFitsTheFrameTests.cs:19-22`). `CanvasGraphViewModelTests.StubQueries` still implements the interface by hand (not `FakeWorkspaceQueries`) — it will fail to compile until updated.

## E7 surface list (Owner N0 as closed by the spec)

| Surface | This ADR |
|---|---|
| **store** | Existing SQLite: `node_dim`, `evidence_assertion_fact`, scope snapshot facts. No second graph store. No `folder_dim`. |
| **model** | One tree node = `(path, kind)`, `kind ∈ {file-artifact, census-folder}`. Coverage two-valued on census-folders. Disclosure is not Coverage. |
| **service** | `IWorkspaceQueries.SolutionTreeAsync`. Census + latest-generation join. One skip policy. Test-overridable `DropRelativePaths`. |
| **projection/wire** | IPC `solution-tree` / `SolutionTreeRequest` → `SolutionTreeResult`. Bounded. `Not recorded` / `Omitted (N)`. |
| **client type** | Architecture docking-host tree. Toolkit unfrozen (N7). |
| **UI** | Architecture pane. Hard states: empty, loading, unindexed leaf, error, no-workspace, stale-while-refresh; skip-count; US-T6 copy. Empty → Show Graph. |
| **compute reader** | View source → `NodeContentAsync` / `codeviewer`. Reveal in graph → `GraphAsync` / `DescribeAsync`. No third path. No Atlas. |

## LOA — archetype and tier for this addition

- **Product host (unchanged):** F — Copilot Aside Hot Path; composed C (MCP) / bounded D (later, not this addition) / H (external agents). Layout remains MultiPanelWorkstation in the Architecture `DockHost`.
- **This addition:** **no model.** Spec Part A “AI-integrated allocation” is honoured: archetype **none**; tier **T0** deterministic projection (query, skip, cap, Disclosure). Capability-tier N/A for cognition — there is none.
- **Rejected for D-0:** **D Grounded Synthesizer** (would invent folders); **A Cascade Pipeline** as the tree (extractors may cascade internally; the navigator does not); **B Adversarial Ensemble**; **T3/T4** on this path (P1 cheapest sufficient). Compile-step T3 (`CompileCallHost`) is unrelated.

P1 cheapest sufficient: one query + one kind row. P2 determinism: derived menu, query-time join, one skip policy. P3 cognition/execution: no cognition here. P5 verification over plausibility: Coverage vs Disclosure. P8: read-only query; no write. P11: App still cannot read workspace files.

## Vertical phasing (defined whole; not this turn’s code)

| Phase | Proves | Real | Mocked | Human | E2E | Unblocks |
|---|---|---|---|---|---|---|
| **UV-0 Core query (walking skeleton, first)** | F* grain: indexed-parent, `unindexed_probe` Unindexed, `bin` absent + skip-count, T5a–c Disclosures, Python/TS honesty, Bicep resolve, PROBE-APP-ENUM in Core tests | `SolutionTreeAsync`, IPC, skip-policy seam, `DropRelativePaths` | Shell surface (none yet); no kind row (AR3) | (headless) | US-T1, T2, T3 query-DTO, T4, T5a–c query-DTO, T6, T7, T11 minus visual-tree; frame-size test | UV-1 |
| **UV-1 Shell surface + one kind row (serial after UV-0)** | Show Solution tree derived; visual-tree oracles; activate | One `solution-tree` row `{Architecture}`; surface; Enter / Ctrl+Enter | Toolkit from N7; default-zone choreography may still be design-slice | Open Architecture, Show Solution tree on F* | US-T3/T5 visual-tree, T8, T9, T10, T13 | Proof Pack; join onto `understanding-views` |

Serial. Not this turn’s code. D-1…D-6 stay out.

## Files later slices may touch (seam, not implementation)

**Core-query (UV-0):** `src/AiDe.Core/Projections/IWorkspaceQueries.cs`; new `src/AiDe.Core/Projections/SolutionTreeProjection.cs` (types + compute); `src/AiDe.Core/Projections/ProjectionService.cs` (delegate + activity tags); `src/AiDe.Core/Ipc/WorkspaceOperations.cs`; `src/AiDe.Core/Ipc/WorkspaceClient.cs`; `src/AiDe.Core/Store/StoreReader.cs` (reuse `FilesToSearch` / `ScopeLocation`; add helpers only if the join cannot share them); skip-policy type after N7 (existing extraction skip sites listed in §3 — **consume**, do not copy); `src/AiDe.Daemon/Program.cs` only if registration cannot stay on `WorkspaceOperations.Register`; `tests/Shared/FakeWorkspaceQueries.cs`; `tests/AiDe.Core.Tests/CanvasGraphViewModelTests.cs` (`StubQueries`); `tests/AiDe.Core.Tests/EveryOperationFitsTheFrameTests.cs`; `tests/AiDe.Core.Tests/DaemonOperationsTests.cs`; new Core tests for F*.

**Shell-surface (UV-1):** `src/AiDe.App/Workbench/SurfaceContentFactory.cs` (one row); new surface type under `src/AiDe.App/Workbench/` (name after N7 toolkit); `src/AiDe.App/Workbench/NodeViewMenu.cs` only if the tree must offer Open-as — default is Enter/Ctrl+Enter mapped to existing `NodeViewKind` values, no new enum member; **not** `MainMenuBuilder` lists (derived); `tests/AiDe.App.Tests/` menu mutation, visual-tree, PROBE-APP-ENUM / PROBE-ATLAS / PROBE-FILE-READ. Default layout (`ZoneLayout`) only if design-slice includes the tree in Architecture’s default — not required to admit the kind.

**Must not touch this horizon:** `src/AiDe.Core/Understanding/**`; Atlas; `session-contracts.md` as a D-0 seam; extractor Python/TS provenance rewrite; Addenda C/D compile types; D-1…D-6 rows.

## N7 spike list (do not execute here)

1. **Tree toolkit** — WPF `TreeView` vs alternative. Spike Protocol (read + run on the installed WPF). Do not freeze a control in this ADR. Exit: a named control that can render F* hard states with UIA Name containing kind/coverage, 28px full-row hit, non-expanding unindexed leaf.
2. **Skip-set survivor** — Flagged. Measure the nine opened lists on F* and one real workspace. Unify to one policy type existing walkers consume. Census consumes that type. Do not guess the member set in UV-0 production wiring; tests inject `bin`.

No unfamiliar/preview SDK is a dependency of this ADR. Do not spike NodeContent / Graph / IPC — those are opened Core.

## Falsifying tests (red in UV-0 / UV-1; not this turn)

1. `IWorkspaceQueries` declares `SolutionTreeAsync`; `WorkspaceOperations` catalog contains `solution-tree` and not a mapping onto `overview`/`graph`. *Headless, Core.*
2. F* query DTO: indexed-parent folder + file-artifact; `unindexed_probe` coverage `unindexed`; no `bin` node; skip-count ≥ 1. *Headless, Core.*
3. US-T5c: `DropRelativePaths = {omit_probe, omit_probe_2}` omits those paths, `Omitted (N)` N≥2, `unindexed_probe` remains. A test that only lowers `MaxCensusFolders` and loses `unindexed_probe` fails. *Headless, Core; visual-tree in UV-1.*
4. After the kind row: US-C4 mutation still holds; Architecture menu gains Show Solution tree with no builder-list edit; Coding/Explore do not admit `solution-tree`. *Headless, App. UV-1.*
5. PROBE-APP-ENUM / PROBE-ATLAS / PROBE-FILE-READ. *UV-1.*

## Evidence

| Claim | Source opened this turn | Confidence |
|---|---|---|
| Spec grain + N4 close | `docs/specs/understanding-views.md` `:59-67`, `:168-170` | **Verified** |
| AR3 | `docs/specs/addendum-c-perspectives.md` `:321-322` | **Verified** |
| ADR-0030 Decision 2–3 | `docs/adr/0030-perspective-registry-and-allow-lists.md` `:73-80` | **Verified** |
| DC-022 | `IWorkspaceQueries.cs` `:32-35`, `:55-59` | **Verified** |
| No tree method today | `IWorkspaceQueries.cs` `:20-105` | **Verified** |
| IPC catalog | `WorkspaceOperations.cs` `:130-158`, `:174-235` | **Verified** |
| Schema: no folder_dim / artifact_dim | `WorkspaceSchema.cs` `:41-128` | **Verified** |
| SurfaceKind rows + Perspectives column | `SurfaceContentFactory.cs` `:108-273` | **Verified** |
| Perspective id `architecture` | `Perspectives.cs` `:72-73` | **Verified** |
| NodeViewKind.Source / GraphNeighbourhood | `NodeViewMenu.cs` `:6-14`, `:53`, `:68` | **Verified** |
| Skip-list disagreement | extraction files cited in §3 | **Verified** |
| `FilesToSearch` / `ScopeLocation` | `StoreReader.cs` `:229-285` | **Verified** |
| Graph DefaultMaxNodes = 5000 | `GraphProjection.cs` `:191` | **Verified** |
| N4 PASS; T5c residual | `note-understanding-views-n4-pass.md` `:47-56` | **Verified** |
| Production folder cap 2000 | no measured census cardinality | **Inferred** (gap named) |
| Skip-set survivor | nine lists disagree | **Flagged** (N7) |
| Toolkit | unfrozen | **Flagged** (N7) |

## Gate

Stage 4 architect council is **skipped this turn** (N6 is a separate Conductor panel). Status remains **Proposed**. Authors did not self-clear.

## Drift

`docs/architecture.md` §C/D.2 and §C/D.12 **excluded** D-0…D-6 from Addenda C/D slices. Those sections are not rewritten as if they now include D-0. §Understanding views / D-0 is the later-horizon admission and points here.
