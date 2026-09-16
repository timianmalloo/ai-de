---
id: adr-0038-d0-solution-tree-census-and-kind
title: "ADR-0038 — D-0 Solution tree: one Architecture kind, one query-time census, no second store"
type: adr
status: accepted
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
  Core query-time census join (SolutionTreeAsync / IPC solution-tree / one SolutionTreeQuery).
  File-artifacts resolve through ResolveWithinWorkspace; census does not follow reparse points;
  UV-0 consumes UnanalysedLanguages.Skip. Named drop-set is projection/test-host only, not IPC.
  Status accepted after N6 Security and Tech Lead re-review (conductor recorded).
---

# ADR-0038: D-0 Solution tree — one Architecture kind, one query-time census, no second store

- **Status:** Accepted (2026-09-15). N6 Security re-review PASS (`01a0a588-9dbb-7df3-b50a-2bb41f13564d`); Tech Lead re-review PASS (`01a0a588-9dbb-7df3-b50a-2bc466fff218`). Conductor recorded; authors did not self-clear. Spec `spec-understanding-views` stays draft.
- **Date:** 2026-09-15
- **Deciders (Peer Mode, this turn):** Enterprise Architect (fit/longevity), Data & Persistence Architect (durable representation), Tech Lead (smallest correct). Session `understanding-views-architecture`. N6 Security BLOCK closed in text here; authors do not mark Accepted.
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

We will admit D-0 as one Architecture-only `SurfaceKind` (`solution-tree`) whose payload is one new Core query-time census join (`SolutionTreeAsync` / IPC `solution-tree` / one `SolutionTreeQuery` record). File-artifacts resolve through existing `ResolveWithinWorkspace` (containment + `File.Exists`); the census walk does not follow reparse points (`EnvelopePurge` class); UV-0 consumes `UnanalysedLanguages.Skip` (fail-closed; no tenth `HashSet`; no public skip/census interfaces); integer caps travel on the wire and a Graph-style shrink keeps the payload under `IpcFraming.MaxFrameBytes`; the US-T5c named drop-set lives only on the projection/test host. Coverage is two-valued with ancestor `indexed-parent`; Disclosure `Omitted (N)` derives from one `OmittedByCap`. We will not store a census, walk disk from the App, put `DropRelativePaths` on IPC, reuse `OverviewAsync`/`GraphAsync` as the tree, scaffold D-1…D-6, or freeze the tree toolkit.

## Chosen shape (the remaining honest one)

Core, at tree-open, walks the workspace root on the daemon side of DC-022 without following reparse points, emits census-folders minus `UnanalysedLanguages.Skip`, joins latest-generation file-artifacts through `ResolveWithinWorkspace`, and returns a bounded `SolutionTreeResult` (count caps and frame shrink). The App projects that DTO. Folder identity comes only from the census emission. `declared_at` never mints a folder; it only widens Coverage of a census-emitted path (and ancestors) to `indexed-parent`.

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

**One query record on the seam and the wire** (fields would otherwise be identical — collapse; Core’s own types, `WorkspaceClient` remarks `:28-31`):

```
public sealed record SolutionTreeQuery(
    int MaxCensusFolders = SolutionTreeProjection.DefaultMaxCensusFolders,
    int MaxFileArtifacts = SolutionTreeProjection.DefaultMaxFileArtifacts);

public enum SolutionTreeNodeKind { FileArtifact, CensusFolder }

public enum CensusFolderCoverage { IndexedParent, Unindexed }

public enum SolutionTreeShortfallCause
{
    Io, Permission, Cap, UnresolvablePath, PythonTsPerFile, ReparsePoint
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

No `SolutionTreeRequest` twin. `WorkspaceOperations.Handle<SolutionTreeQuery>`. No `DropRelativePaths` field on the query, the IPC payload, or `IWorkspaceQueries`.

Grain on the wire: one `SolutionTreeNode` is exactly one `(Path, Kind)`. `Path` is workspace-relative, `/` separators, **no trailing slash**, workspace root `""`. Identity, collapse of many assertions to one path, parent-of-file, and `declared_at` equality all use this normalisation **and** `PathComparison.ForThisFileSystem` (`PathComparison.cs:40-42` **[Verified]**). `Coverage` is non-null iff `Kind == CensusFolder`. `NodeId` is the activate handle for `file-artifact` (representative latest-generation subject, same choice as `StoreReader.FilesToSearch` `:242-263` **[Verified]**); null on census-folders. `NodeKind` is `node_dim.node_kind` / `has_type` for glyphs; glyph-to-kind chrome is N8, not this ADR.

**Join (query-time, not stored):**

1. Census: Core walks directories under the workspace root on the daemon side of DC-022. A census-folder exists iff the walk emitted it and `UnanalysedLanguages.Skip` does not name that directory’s name. Skip-listed directories are **not nodes**; they increment `SkipListedDirectoriesOmitted`. **Reparse points / junctions are not followed** — same refusal class as `EnvelopePurge.Resolve` (`EnvelopePurge.cs:77-81` **[Verified]**: `DirectoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)` → do not descend; Disclosure cause `ReparsePoint`, no node for the unobserved target). Do **not** reuse `UnanalysedLanguages.Enumerate` (`:86-110` **[Verified]**): it follows junctions (`Directory.EnumerateDirectories`) and swallows IO/permission (`:97-103`). Census walk is a private/internal method on the projection; **no public** `IWorkspaceDirectoryCensus`.
2. File-artifacts: latest-generation assertions whose path **`ResolveWithinWorkspace` accepts as a file**. That helper (`ProjectionService.cs:1197-1227` **[Verified]**) combines `ScopeLocation` + `artifact_path_id`, `Path.GetFullPath`, separator-terminated prefix with `PathComparison.ForThisFileSystem`, **and** `File.Exists`. UV-0 **calls this existing method** (lift to `internal` if the census lives beside it — not a second containment function). `File.Exists` false → not a file-artifact (covers directory-valued assertions, Python/TS `ScopeId` strings, hostile `..`, missing files). Do not join on raw `ScopeLocation` + `artifact_path_id`. Many assertions that resolve to one path collapse to one node under `PathComparison.ForThisFileSystem`.
3. Coverage of census-folder `P`, computed **bottom-up**: `indexed-parent` iff (a) at least one file-artifact joins under `P` (child or descendant), **or** (b) some scope’s `declared_at` equals `P` under the same normalisation/`PathComparison`, **or** (c) a **descendant census-folder is `indexed-parent`**. Else `unindexed` (non-expanding leaf). (c) is load-bearing: a Python/TS-only ancestor must not hide an indexed descendant.
4. After count-cap / shrink / test-host drop: **drop every file-artifact whose parent census-folder is absent from `Nodes`**. No orphan files under a skipped, unobserved, or cap-omitted parent.
5. Python/TS: extractors store `request.ScopeId` as `Provenance.ArtifactPathId` with null source location (N1 **[Verified]**). `ResolveWithinWorkspace` will not treat those as files. Zero `.py`/`.ts` file-artifact nodes from a second walk. If census emitted `P` and a Python/TS scope `declared_at` equals `P`, Coverage is `indexed-parent` (and ancestors via rule 3c). Disclosure copy (exact): `Python and TypeScript files are not listed individually. The scope folder is indexed.` Cause `PythonTsPerFile`.
6. Bicep filename-only: `ResolveWithinWorkspace` onto an **existing** census-folder; never treat the filename as a folder. Else Disclosure `Not recorded` (cause `UnresolvablePath`), no node.
7. Unresolvable / hostile assertion paths: Disclosure, no invented folder.

**Honest shortfall (never a plausible empty tree):**

| Event | Coverage? | Disclosure |
|---|---|---|
| IOException on a census path | no node for the unobserved path | `Not recorded`, cause `Io` |
| UnauthorizedAccessException | no node for the denied path | `Not recorded`, cause `Permission` |
| Reparse point / junction | no node; do not descend | `Not recorded`, cause `ReparsePoint` |
| Integer cap or frame shrink | named paths absent | `Omitted (N)` **derived from** `OmittedByCap` |
| Skip-listed directory | no node | chrome count `N skip-listed directories omitted` (N on the result; copy is UI) |
| Unresolvable assertion path | no node from that path | `Not recorded`, cause `UnresolvablePath` |
| Python/TS per-file | n/a | exact US-T6 copy, cause `PythonTsPerFile` |

IPC/daemon failure is **not** a Disclosure; it is the existing error+Retry on the query (US-T9). IO/permission tests arrange a real filesystem shortfall on a named path that is not `unindexed_probe` or `bin` (ACL / missing-intermediate), or construct the projection in-process; they do **not** require a public walker interface.

**Bounds (count and bytes):**

- Production count defaults **[Inferred — gap: no measured folder-census cardinality this turn]:** `DefaultMaxCensusFolders = 2_000`; `DefaultMaxFileArtifacts = 5_000` (aligned with `GraphProjection.DefaultMaxNodes = 5_000` **[Verified]** `GraphProjection.cs:191`). Clamp like other projections. Retune the constants when core-query emits counts.
- Count overflow ranking: keep the workspace root; keep indexed-parent folders before unindexed; keep shallower paths before deeper; then ordinal path under `PathComparison.ForThisFileSystem`. This ranking is **not** how tests arrange US-T5c.
- **Byte/frame bound:** count caps are not enough. After the count trim, shrink like `ProjectionService.Graph` (`:596-654` **[Verified]**) until `FramedCost` ≤ `ProjectionService.MaxFramedGraphBytes` (`:251` **[Verified]** = `IpcFraming.MaxFrameBytes` − 64 KiB; frame is `1_048_576` at `IpcFraming.cs:53` **[Verified]**). Nodes dropped by shrink increment `OmittedByCap`. UV-0 adds `SolutionTreeAsync` to `EveryOperationFitsTheFrameTests.AtCeiling` (`:85-126` **[Verified]** — reflective census of `IWorkspaceQueries`).

**One numeric home for cap-omit:** `SolutionTreeResult.OmittedByCap` is the only cap-omit integer (count-cap + shrink + test-host named drop). Disclosure `Omitted (N)` is derived: if `OmittedByCap > 0`, one Cap disclosure with `Count = OmittedByCap` and exact copy `Omitted (N)`; if zero, no Cap disclosure. Do not store a second omit count on a disclosure independently of this field.

**Cap / T5c named drop-set (not on the wire):**

`DropRelativePaths` is **not** a member of `SolutionTreeQuery` and **not** an IPC field. Production cannot hide folders behind `Omitted (N)` by sending a list. The named drop-set for US-T5c (`omit_probe`, `omit_probe_2`) lives on the **projection/test host**: Core tests construct `SolutionTreeProjection` (or `ProjectionService` in-process) with an internal/test-only omit set compared under the same normalisation/`PathComparison`. Daemon IPC tests that need T5c construct that projection; they do not send the list on `solution-tree`. A test that only lowers `MaxCensusFolders` until an alphabetical prefix drops `unindexed_probe` remains the forbidden arrange.

**Registration (N1 §7, opened):** new const + `Handle<SolutionTreeQuery>` arm on `WorkspaceOperations`; `WorkspaceClient.SolutionTreeAsync`; `LocalWorkspaceQueries` adapter; `FakeWorkspaceQueries` virtual refuse. Daemon `WorkspaceOperations.Register(endpoint, projections)` already takes `ProjectionService` — add the arm there.

**Instrumentation (IO1):** on the normal path, emit duration, census-folder count, file-artifact count, indexed-parent count, unindexed count, skip omitted, `OmittedByCap`, framed bytes, shortfall causes, error code. Degrade to “not recorded”, never a plausible wrong number. Latency is recorded, not CI-asserted (ADR-0029).

### 3. Skip set — UV-0 consumes `UnanalysedLanguages.Skip`

Census **consumes the existing** `UnanalysedLanguages.Skip` (`UnanalysedLanguages.cs:48-52` **[Verified]**). It does **not** copy a tenth `HashSet`. It does **not** ship public `IDirectorySkipPolicy` or `IWorkspaceDirectoryCensus`. Visibility of `Skip` may become `internal` so `AiDe.Core` projections can read the same instance; that is not a new set and not a public API.

**Fail-closed in production, not only in tests:** that existing set already contains `bin`, `obj`, `.git`, `node_modules` (and more). UV-0 production wiring always consults it. A test host must not replace it with a set that omits those four; F* `bin/` is omitted because the production skip names `bin`, not because a test injected `bin`.

Opened disagreeing lists remain (N7 may **widen other walkers** onto this same set; UV-0 is not blocked on that):

| Site | Members (directory names) |
|---|---|
| `UnanalysedLanguages.Skip` `:48-52` — **UV-0 binds here** | `bin`, `obj`, `.git`, `.vs`, `node_modules`, `artifacts`, `packages`, `dist`, `build`, `__pycache__`, `.venv`, `venv`, `.tox`, `target`, `vendor` |
| `CSharpScopeDiscovery.Skip` `:31-32` | `bin`, `obj`, `.git`, `node_modules`, `artifacts` |
| `CSharpScopeDiscovery` knowledge walk `:151-154` | Skip **plus** `.vs`, `dist`, `build`, `out`, `__pycache__`, `.venv`, `venv`, `packages`, `artifacts` |
| `CSharpScopeDiscovery` Python walk `:296-299` | Skip **plus** `__pycache__`, `.venv`, `venv`, `.tox`, `site-packages` |
| `ScopeFingerprints.Skip` `:99-102` | `bin`, `obj`, `.git`, `.vs`, `node_modules`, `artifacts`, `packages`, `TestResults` |
| `TypeScriptExtractor.SkippedDirectories` `:296-301` | `.git`, `node_modules`, `bin`, `obj`, `artifacts`, `publish`, `_framework`, `dist`, `build`, `out`, `.next`, `coverage` |
| `PythonExtractor.Files` `:409-412` | `__pycache__`, `.venv`, `venv`, `.tox`, `node_modules`, `.git`, `build`, `dist` |
| `KnowledgeExtractor.AllFiles` `:587-591` | `node_modules`, `bin`, `obj`, `.git`, `.vs`, `dist`, `build`, `out`, `__pycache__`, `.venv`, `venv`, `packages`, `artifacts` |
| `SqlSchemaExtractor.Files` `:367-370` | `node_modules`, `bin`, `obj`, `.git`, `packages` |

### 4. Activate — two existing paths, no third

| Gesture | Existing action | Seam |
|---|---|---|
| Primary (Enter) on file-artifact | View source (`NodeViewKind.Source`) | `NodeContentAsync` → admitted `codeviewer` (ADR-0018 / ADR-0025) |
| Secondary (Ctrl+Enter) on file-artifact | Reveal in graph (`NodeViewKind.GraphNeighbourhood`) | `GraphAsync` / `DescribeAsync` on the node’s `NodeId` |
| Indexed-parent census-folder | Expand / collapse | no content, no graph entity |
| Unindexed census-folder | Leaf; Enter/Right/Left do not expand | no children, no activate |

No Atlas types (`AiDe.Core.Understanding`). No third IPC. Reveal failure / View source failure → error + Retry on **that** surface; tree selection unchanged. App PROBE-FILE-READ: no `File.ReadAllText` / `OpenRead` on the workspace path from App code.

**PROBE-APP-ENUM** is an **App-assembly / non-`IWorkspaceQueries` caller** rule, not a PID rule. ADR-0009 keeps the authority core in-process in Phase 1 (`docs/adr/0009-in-process-first-daemon.md` Decision **[Verified]**): Core census `EnumerateDirectories` may run in the same process as the App. A probe that fails because the PID enumerated is a false red. The oracle is: types in `AiDe.App` (and App test hosts) that are not `IWorkspaceQueries` implementations must not invoke `Directory.EnumerateFileSystemEntries` / `EnumerateDirectories` / `EnumerateFiles` / `GetDirectories` / `GetFiles` / `GetFileSystemEntries` on the workspace root or a descendant. Core’s census inside `LocalWorkspaceQueries` / `ProjectionService` / `SolutionTreeProjection` is allowed, in-process or out.

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
- **Reuse `UnanalysedLanguages.Enumerate` wholesale:** rejected — it follows junctions and swallows IO/permission (`:86-110`); US-T5a/b and Security reparse refusal require a different walk that still **consumes** `UnanalysedLanguages.Skip`.
- **`DropRelativePaths` on `SolutionTreeQuery` / IPC:** rejected at N6 — a hostile (or merely curious) client could hide folders behind `Omitted (N)`. Named drop-set is projection/test-host only.
- **Public `IDirectorySkipPolicy` / `IWorkspaceDirectoryCensus`:** rejected — YAGNI; a public seam is a second skip policy. Consume the existing field; tests construct the projection.
- **Raw `ScopeLocation` + `artifact_path_id` join:** rejected at N6 — skips containment and `File.Exists`; `ResolveWithinWorkspace` is the existing function.
- **Integer-only cap as the T5c arrange:** rejected — Test Architect residual; alphabetical prefix can drop `unindexed_probe`.
- **Count caps without a byte/frame shrink:** rejected — INV-0003 class; `EveryOperationFitsTheFrameTests` exists because item caps overflow bytes.
- **LOA D (Grounded Synthesizer) inventing folders:** rejected — a model must not mint Coverage.

## Consequences

- **Positive:** D-0 can be built as a walking skeleton (Core query reds, then one kind row) without a schema migration, without a second store, and without a hand-written menu. Unindexed, skip-omission, and Not-recorded stay distinct. AR3 holds for D-1…D-6.
- **Negative / accepted:** Python/TS files are not listed individually this horizon (disclosed). Other walkers still disagree with `UnanalysedLanguages.Skip` until N7 widens them. Production folder-cap numbers 2000/5000 stay **Inferred** until measured. US-T5c cannot be arranged over IPC; Core tests construct the projection.
- **Follow-ups / new risks:** N6 re-review (do not self-clear). N7 toolkit spike. N7 may widen other skip lists onto `UnanalysedLanguages.Skip`. N8 `ui-design`. Core-query then shell-surface. `EveryOperationFitsTheFrameTests` must gain a `SolutionTreeAsync` case when the method lands. `CanvasGraphViewModelTests.StubQueries` still implements the interface by hand (not `FakeWorkspaceQueries`) — it will fail to compile until updated.

## E7 surface list (Owner N0 as closed by the spec)

| Surface | This ADR |
|---|---|
| **store** | Existing SQLite: `node_dim`, `evidence_assertion_fact`, scope snapshot facts. No second graph store. No `folder_dim`. |
| **model** | One tree node = `(path, kind)`, `kind ∈ {file-artifact, census-folder}`. Coverage two-valued on census-folders. Disclosure is not Coverage. |
| **service** | `IWorkspaceQueries.SolutionTreeAsync`. Census + `ResolveWithinWorkspace` join. Consumes `UnanalysedLanguages.Skip`. Named drop-set on the projection/test host only. |
| **projection/wire** | IPC `solution-tree` / one `SolutionTreeQuery` (integer caps only) → `SolutionTreeResult`. Count caps **and** frame shrink. `Not recorded` / `Omitted (N)` derived from `OmittedByCap`. |
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
| **UV-0 Core query (walking skeleton, first)** | F* grain: indexed-parent, `unindexed_probe` Unindexed, `bin` absent + skip-count, T5a–c Disclosures, Python/TS honesty, Bicep resolve, frame fit | `SolutionTreeAsync`, IPC integer caps, consume `UnanalysedLanguages.Skip`, `ResolveWithinWorkspace`, no-follow reparse | Shell surface (none yet); no kind row (AR3); T5c omit set on the test-constructed projection | (headless) | US-T1, T2, T3 query-DTO, T4, T5a–c query-DTO (T5c via projection host), T6, T7, T11 minus visual-tree; `EveryOperationFitsTheFrameTests` | UV-1 |
| **UV-1 Shell surface + one kind row (serial after UV-0)** | Show Solution tree derived; visual-tree oracles; activate | One `solution-tree` row `{Architecture}`; surface; Enter / Ctrl+Enter | Toolkit from N7; default-zone choreography may still be design-slice | Open Architecture, Show Solution tree on F* | US-T3/T5 visual-tree, T8, T9, T10, T13 | Proof Pack; join onto `understanding-views` |

Serial. Not this turn’s code. D-1…D-6 stay out.

## Files later slices may touch (seam, not implementation)

**Core-query (UV-0):** `src/AiDe.Core/Projections/IWorkspaceQueries.cs`; new `src/AiDe.Core/Projections/SolutionTreeProjection.cs` (types + compute; test-host omit set as constructor/`internal` parameter, not a query field); `src/AiDe.Core/Projections/ProjectionService.cs` (delegate, activity tags, **call existing `ResolveWithinWorkspace`**, Graph-style shrink); `src/AiDe.Core/Extraction/UnanalysedLanguages.cs` (`Skip` visibility `internal` if needed — same instance, not a copy); `src/AiDe.Core/Ipc/WorkspaceOperations.cs`; `src/AiDe.Core/Ipc/WorkspaceClient.cs`; `src/AiDe.Core/Store/StoreReader.cs` (reuse `FilesToSearch` / `ScopeLocation` only as inputs to `ResolveWithinWorkspace`); `src/AiDe.Daemon/Program.cs` only if registration cannot stay on `WorkspaceOperations.Register`; `tests/Shared/FakeWorkspaceQueries.cs`; `tests/AiDe.Core.Tests/CanvasGraphViewModelTests.cs` (`StubQueries`); `tests/AiDe.Core.Tests/EveryOperationFitsTheFrameTests.cs`; `tests/AiDe.Core.Tests/DaemonOperationsTests.cs`; new Core tests for F* (T5c constructs the projection).

**Shell-surface (UV-1):** `src/AiDe.App/Workbench/SurfaceContentFactory.cs` (one row); new surface type under `src/AiDe.App/Workbench/` (name after N7 toolkit); `src/AiDe.App/Workbench/NodeViewMenu.cs` only if the tree must offer Open-as — default is Enter/Ctrl+Enter mapped to existing `NodeViewKind` values, no new enum member; **not** `MainMenuBuilder` lists (derived); `tests/AiDe.App.Tests/` menu mutation, visual-tree, PROBE-APP-ENUM (App-assembly / non-`IWorkspaceQueries` callers — not PID), PROBE-ATLAS / PROBE-FILE-READ. Default layout (`ZoneLayout`) only if design-slice includes the tree in Architecture’s default — not required to admit the kind.

**Must not touch this horizon:** `src/AiDe.Core/Understanding/**`; Atlas; `session-contracts.md` as a D-0 seam; extractor Python/TS provenance rewrite; Addenda C/D compile types; D-1…D-6 rows.

## N7 spike list (do not execute here)

1. **Tree toolkit** — WPF `TreeView` vs alternative. Spike Protocol (read + run on the installed WPF). Do not freeze a control in this ADR. Exit: a named control that can render F* hard states with UIA Name containing kind/coverage, 28px full-row hit, non-expanding unindexed leaf.
2. **Widen other walkers onto `UnanalysedLanguages.Skip`** (optional after UV-0). UV-0 is already bound to that existing set. N7 may make C# / TS / Python / knowledge / SQL walks consume the same instance so they stop disagreeing. Not a UV-0 blocker. Not a new HashSet.

No unfamiliar/preview SDK is a dependency of this ADR. Do not spike NodeContent / Graph / IPC / `EnvelopePurge` / `ResolveWithinWorkspace` — those are opened Core.

## Falsifying tests (red in UV-0 / UV-1; not this turn)

1. `IWorkspaceQueries` declares `SolutionTreeAsync`; `WorkspaceOperations` catalog contains `solution-tree` and not a mapping onto `overview`/`graph`. *Headless, Core.*
2. F* query DTO: indexed-parent folder + file-artifact; `unindexed_probe` coverage `unindexed`; no `bin` node; skip-count ≥ 1. *Headless, Core.*
3. US-T5c: Core test constructs the projection with named omit `{omit_probe, omit_probe_2}`; those paths absent, `OmittedByCap` ≥ 2, Disclosure `Omitted (N)` derives from it, `unindexed_probe` remains. `SolutionTreeQuery` JSON has no `dropRelativePaths`. A test that only lowers `MaxCensusFolders` and loses `unindexed_probe` fails. *Headless, Core; visual-tree in UV-1.*
4. After the kind row: US-C4 mutation still holds; Architecture menu gains Show Solution tree with no builder-list edit; Coding/Explore do not admit `solution-tree`. *Headless, App. UV-1.*
5. PROBE-APP-ENUM (App assembly / non-`IWorkspaceQueries` callers, not PID) / PROBE-ATLAS / PROBE-FILE-READ. *UV-1.*
6. `EveryOperationFitsTheFrameTests` covers `SolutionTreeAsync`; a hostile census payload shrinks under `MaxFramedGraphBytes`. *Headless, Core. UV-0.*
7. Production skip: F* `bin` omitted with no test-injected skip set. *Headless, Core. UV-0.*
8. Ancestor coverage: a Python/TS-only parent of an indexed-parent descendant is `indexed-parent`, not `unindexed`. *Headless, Core. UV-0.*
9. File-artifact whose parent folder is absent from `Nodes` is absent. *Headless, Core. UV-0.*
10. Census does not descend a junction; Disclosure cause `ReparsePoint`. *Headless, Core. UV-0.*

## N6 close-outs (quoted; authors do not self-clear)

N6 Security **BLOCK**ed. Each major below is closed in this ADR. Status remains **Proposed**.

1. **File-artifact join uses existing `ResolveWithinWorkspace` (containment + `File.Exists`), not raw `ScopeLocation` + `artifact_path_id`. Directory/ScopeId rows are not file-artifact nodes.** Closed: §2 join step 2 cites `ProjectionService.cs:1197-1227`.
2. **Census walk does not follow reparse points / junctions. Cite `EnvelopePurge` as the existing refusal class; do not reuse `UnanalysedLanguages.Enumerate` as-is.** Closed: §2 join step 1 cites `EnvelopePurge.cs:77-81`; Enumerate rejected `:86-110`.
3. **UV-0 skip binding: consume `UnanalysedLanguages.Skip`. Fail-closed: at least `bin`, `node_modules`, `.git`, `obj` omitted in production, not only in tests. Do not copy a tenth HashSet. Do not ship public `IDirectorySkipPolicy` / `IWorkspaceDirectoryCensus`. N7 may widen other walkers.** Closed: §3; Skip `:48-52`.
4. **`DropRelativePaths` is not on `SolutionTreeQuery` / `SolutionTreeRequest` / IPC. Integer caps stay on the wire. Named drop-set for US-T5c lives on the projection/test host. Production cannot hide folders behind `Omitted (N)`.** Closed: §2 one query record; T5c paragraph; no `SolutionTreeRequest`.
5. **PROBE-APP-ENUM is App-assembly / non-`IWorkspaceQueries` callers, not a PID rule (ADR-0009 in-process host).** Closed: §4 PROBE-APP-ENUM paragraph.
6. **Payload: count caps and a byte/frame bound (name `EveryOperationFitsTheFrameTests` / shrink like Graph). Count-only is not enough.** Closed: §2 Bounds; `MaxFramedGraphBytes` `:251`; `IpcFraming.MaxFrameBytes` `:53`.
7. **Identity, collapse, and `declared_at` equality use `PathComparison.ForThisFileSystem` and the same `/` no-trailing-slash normalisation.** Closed: §2 grain paragraph; `PathComparison.cs:40-42`.
8. **Coverage `indexed-parent` also if a descendant census-folder is indexed-parent (Python/TS-only ancestor chain must not hide an indexed descendant).** Closed: §2 join step 3(c).
9. **Drop file-artifacts whose parent census-folder is absent from `Nodes`.** Closed: §2 join step 4.
10. **One numeric home for cap-omit (`OmittedByCap`); Disclosure `Omitted (N)` derives from it.** Closed: §2 “One numeric home”.
11. **Collapse Query/Request to one record if fields stay identical. Caps 2000/5000 stay labelled Inferred.** Closed: one `SolutionTreeQuery`; defaults labelled **Inferred**.

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
| `ResolveWithinWorkspace` containment + `File.Exists` | `ProjectionService.cs` `:1197-1227` | **Verified** |
| Graph DefaultMaxNodes = 5000 | `GraphProjection.cs` `:191` | **Verified** |
| Frame shrink pattern / `MaxFramedGraphBytes` | `ProjectionService.cs` `:596-654`, `:251` | **Verified** |
| `IpcFraming.MaxFrameBytes` = 1 MiB | `IpcFraming.cs` `:53` | **Verified** |
| Reparse refusal class | `EnvelopePurge.cs` `:77-81` | **Verified** |
| `UnanalysedLanguages.Skip` contains bin/obj/.git/node_modules | `UnanalysedLanguages.cs` `:48-52` | **Verified** |
| `PathComparison.ForThisFileSystem` | `PathComparison.cs` `:40-42` | **Verified** |
| ADR-0009 in-process host | `docs/adr/0009-in-process-first-daemon.md` Decision | **Verified** |
| N4 PASS; T5c residual | `note-understanding-views-n4-pass.md` `:47-56` | **Verified** |
| Production folder cap 2000 / file cap 5000 | no measured census cardinality | **Inferred** (gap named) |
| Other walkers vs UV-0 skip | nine lists still disagree | **Flagged** (N7 widen, not UV-0 bind) |
| Toolkit | unfrozen | **Flagged** (N7) |

## Gate

N6 sat and **BLOCK**ed (Security). This amendment repairs the BLOCK in text. Status remains **Proposed**. Authors did not self-clear; a re-review panel is Conductor’s. UV-0 is not implemented this turn.

## Drift

`docs/architecture.md` §C/D.2 and §C/D.12 **excluded** D-0…D-6 from Addenda C/D slices. Those sections are not rewritten as if they now include D-0. §Understanding views / D-0 is the later-horizon admission and points here.
