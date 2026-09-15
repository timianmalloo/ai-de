---
id: design-solution-tree
title: "D-0 Solution tree — UV-0 Core query then UV-1 Architecture kind"
type: design
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [design, D-0, solution-tree, census, ipc, wpf, treeview, UV-0, UV-1, understanding-views]
links:
  - { to: spec-understanding-views, rel: implements }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: implements }
  - { to: architecture, rel: implements }
  - { to: spike-d0-tree-toolkit, rel: depends-on }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
  - { to: adr-0018-node-content-reader-contract, rel: depends-on }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: adr-0009-in-process-first-daemon, rel: depends-on }
  - { to: note-understanding-views-n1-inventory, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: depends-on }
  - { to: threat-model-ai-native-ide, rel: relates-to }
  - { to: privacy-review-ai-native-ide, rel: relates-to }
review-by: 2027-03-15
summary: >-
  Walking-skeleton design for D-0: UV-0 adds one Core query-time census join
  (SolutionTreeAsync / IPC solution-tree / one SolutionTreeQuery of two ints) with grain
  (path, kind), no folder_dim, T5c omit off the wire; UV-1 then admits one Architecture
  SurfaceKind and a WPF TreeView. Status draft — N10 review is later.
---

<!--
Detailed Component Design — /design-slice N9 (patterns-expert + csharp-developer).
Stage 4 council skipped (N10). Status remains draft. Authors do not self-clear.
-->

# Design: Solution tree (D-0)

- **Status:** Draft (N10 review is later; authors do **not** mark Accepted)
- **Spec / architecture:** [`docs/specs/understanding-views.md`](../specs/understanding-views.md) · [`docs/architecture.md`](../architecture.md) §Understanding views / D-0 · [`docs/adr/0038-d0-solution-tree-census-and-kind.md`](../adr/0038-d0-solution-tree-census-and-kind.md)
- **Delivery phase / vertical slice:** Understanding views **UV-0 Core query** (walking skeleton, first) then **UV-1 Shell surface + one kind row** (serial after UV-0 reds — AR3). Toolkit frozen by N7 spike (`spike-d0-tree-toolkit`). Zone/layout **not** frozen. D-1…D-6, Atlas, extractor rewrites, `main` join: out.
- **Author(s) / date:** N9 `/design-slice` (patterns-expert with csharp-developer), session `understanding-views-design`, 2026-09-15. Git at grounding: `2089e02522bceb0e118beee1583b853d2381e011` (`understanding-views-design`).

## Goal state

- **Goal:** contracts, grain, failure modes, telemetry, test plan, E7 list, and file lists for UV-0 then UV-1 in this file.
- **Done when:** this file exists, quotes ADR-0038 and the spec, names patterns, has a red-first test plan mapped to US-T1–T7 / T5a–c / T11, committed on this branch, status **draft**.
- **Not in scope:** implementing `src/`; Atlas; D-1…D-6; joining `main`; public `IWorkspaceDirectoryCensus`; `DropRelativePaths` on the wire; marking this design accepted; N8 chrome; N10 council; freezing Zone/layout; rewriting extractors.
- **Tier:** T2 · **Fan-out cap:** 0.

## Quoted authorities (not thinned)

**ADR-0038 Decision** (`docs/adr/0038-d0-solution-tree-census-and-kind.md`):

> We will admit D-0 as one Architecture-only `SurfaceKind` (`solution-tree`) whose payload is one new Core query-time census join (`SolutionTreeAsync` / IPC `solution-tree` / one `SolutionTreeQuery` record). File-artifacts resolve through existing `ResolveWithinWorkspace` (containment + `File.Exists`); the census walk does not follow reparse points (`EnvelopePurge` class); UV-0 consumes `UnanalysedLanguages.Skip` (fail-closed; no tenth `HashSet`; no public skip/census interfaces); integer caps travel on the wire and a Graph-style shrink keeps the payload under `IpcFraming.MaxFrameBytes`; the US-T5c named drop-set lives only on the projection/test host. Coverage is two-valued with ancestor `indexed-parent`; Disclosure `Omitted (N)` derives from one `OmittedByCap`. We will not store a census, walk disk from the App, put `DropRelativePaths` on IPC, reuse `OverviewAsync`/`GraphAsync` as the tree, scaffold D-1…D-6, or freeze the tree toolkit.

**Spec grain + N4 close** (`docs/specs/understanding-views.md`):

> One tree node is exactly one Core-named workspace-relative path: **either** one indexed artifact (a file/document resolved from latest assertions) **or** one census folder (a directory Core observed).

> Node identity is the pair `(path, kind)` with `kind ∈ {file-artifact, census-folder}`. Folder **Coverage** is only `indexed-parent` | `unindexed`. Owner’s `not-recorded` is **Disclosure** (projection shortfall), never a census-folder coverage value and never a reason to mint a folder.

**AR3** (`spec-addendum-c-perspectives` §A5, quoted by the spec):

> Each deferred view is admitted to the **Architecture** allow-list, and to its derived menu, only in the slice that builds it (AR3) — never scaffolded ahead.

**DC-022** (`IWorkspaceQueries.cs` remarks, quoted by the spec):

> The App must not read workspace files: two authorities on what a file contains disagree the first time one resolves a path differently (DC-022), and file access belongs on the side of the boundary that can confine it to the workspace.

**N7 freeze** (`docs/spikes/d0-tree-toolkit/RESULT.md`, **Verified** read+run): WPF `TreeView` + `TreeViewItem` with the attachments table in §UI. Custom `ListView` / `ItemsControl` / WebView2 trees rejected.

## Responsibility

**UV-0** is responsible for one Core read: at tree-open, walk the workspace root on the daemon side of DC-022, emit census-folders minus `UnanalysedLanguages.Skip`, join latest-generation file-artifacts through existing `ResolveWithinWorkspace`, bound the payload (count caps **and** frame shrink), and return one `SolutionTreeResult`. Honest shortfalls are Disclosure. Skip-listed directories are not nodes.

**UV-1** is responsible for one Architecture `SurfaceKind` row (`solution-tree`, `Perspectives` = `{Architecture}`, `Instances` = `One`) whose derived menu entry is **Show Solution tree**, and for projecting the DTO onto a WPF `TreeView` with N7 attachments. Activate is two existing paths (View source / Reveal in graph). Hard states are distinct.

It is **not** responsible for: a stored census / `folder_dim`; a public census or skip interface; `DropRelativePaths` on the wire; App disk walks; Atlas / `AiDe.Core.Understanding`; Overview/Graph as the tree; D-1…D-6 rows; `MainMenuBuilder` lists; default Zone/layout choreography; Python/TS per-file extractor rewrite; glyph-to-kind chrome (N8); widening other extractors’ skip lists (N7 optional, not a UV-0 blocker).

**Boundary set:** F\* (indexed-parent + `unindexed_probe` + `omit_probe` + `omit_probe_2` + `io_probe` + `bin`); T5a IO on `io_probe/` (not `unindexed_probe`, not `bin`); T5b ACL on `omit_probe`; T5c named omit of both `omit_probe` dirs with `unindexed_probe` surviving; hostile `..` assertion path; Bicep filename-only resolvable and not; Python/TS `declared_at` = census `P`; reparse/junction; empty / loading / error / no-workspace / stale-while-refresh; IPC failure; frame overflow; extra JSON `dropRelativePaths`; App-assembly enum/read/Atlas probes.

## Data model (settled first — DM1–DM18)

**Bounded contexts.** Evidence and Projection (payload). Shell presentation (surface). No new context. **Verified** ADR-0038 §5.

**Ubiquitous language.** Spec Part A table: Solution tree, tree node `(path, kind)`, file-artifact, census-folder, Coverage (`indexed-parent` | `unindexed`), Disclosure, skip-listed directory. Do not say “indexed folder”. Do not label skip-listed dirs “hidden” or “excluded”.

**Aggregates / the one invariant each protects**

| Aggregate | Root | Invariant |
|---|---|---|
| **Solution tree projection** (query-time, not stored) | `SolutionTreeResult` for one workspace at one instant | Every visible node is exactly one `(path, kind)`; skip-listed directories are not nodes; a census-folder carries exactly one Coverage value; folders are never invented by splitting `artifact_path_id` or from `declared_at`; shortfalls are Disclosure; the App does not walk disk. |
| **Perspective Layout** (existing) | Architecture `DockHost` | D-0’s kind enters Architecture’s admitted set **only** in UV-1 (AR3). |
| **Scope Snapshot** (existing) | `scope_snapshot_committed_fact` | Untouched. |

Other aggregates referenced **by identity only:** file-artifact `NodeId` (representative latest-generation subject, same choice as `StoreReader.FilesToSearch` `:242-263` **Verified**); census-folder has no graph entity.

**Durable representation (DM13).** Existing `node_dim`, `evidence_assertion_fact`, `scope_generation_desired_fact`, `scope_snapshot_committed_fact` (`WorkspaceSchema.cs:41-128` **Verified** — no `artifact_dim`, no `folder_dim`). Census is **derived disk-now** (DM7). **No `folder_dim`, no census fact table, no second graph store this horizon.** Python/TS `ScopeId`-as-path extractor rewrite is **cut**.

**Grain (declared before columns).** One `SolutionTreeNode` is exactly one `(Path, Kind)` with `Kind ∈ {FileArtifact, CensusFolder}`. `Path` is workspace-relative, `/` separators, **no trailing slash**, workspace root `""`. Identity, collapse of many assertions to one path, parent-of-file, and `declared_at` equality use this normalisation **and** `PathComparison.ForThisFileSystem` (`PathComparison.cs:40-42` **Verified**). Map `Path.GetRelativePath(root, root) == "."` to `""` — same line as `WorkspaceCore.LocateScope` `:326` **Verified**.

**Additivity.** `SkipListedDirectoriesOmitted` is **additive** (count of skipped directory encounters). `OmittedByCap` is **additive** (count-cap + shrink + test-host named drop). Coverage is **non-additive** (a folder is one value; summing Coverage is meaningless). Node counts are additive within one result, not across time.

**History rule.** Census is disk-now: **Type-1 on every census attribute** (a recorded decision to discard history — DM10). Assertion join is latest-generation only (existing snapshot facts already carry history). No Type-2 folder dimension this horizon.

**Derive-don’t-store (DM7).** The tree is a rebuildable projection. Equality test: two calls with the same workspace disk + same latest-generation facts + same caps + same omit set produce the same node set under `PathComparison.ForThisFileSystem`. Disclosure `Omitted (N)` is **derived** from `OmittedByCap` (one numeric home). Coverage is derived (rules 3a–c below), never stored.

**Writers / compute readers (DM15).** No new persisted field. Existing assertion writers stay extractors + `StoreWriter`. Compute readers: `SolutionTreeAsync` (this projection); activate uses existing `NodeContentAsync` and `GraphAsync` / `DescribeAsync`.

**Migration.** None. Expand-migrate-contract N/A.

## E7 change-surface list

| Surface | This design | When |
|---|---|---|
| **store** | Existing SQLite only. No `folder_dim`. No census fact table. | — (no write) |
| **model** | One tree node = `(path, kind)`, `kind ∈ {file-artifact, census-folder}`. Coverage two-valued on census-folders (incl. ancestor rule 3c). Disclosure ≠ Coverage. | UV-0 types |
| **service** | `IWorkspaceQueries.SolutionTreeAsync`. Census + `ResolveWithinWorkspace`. Consumes `UnanalysedLanguages.Skip`. Named drop-set on the projection/test host only. | UV-0 |
| **projection/wire** | IPC `solution-tree` / one `SolutionTreeQuery` (two ints) → `SolutionTreeResult`. Count caps **and** frame shrink. `Omitted (N)` from `OmittedByCap`. No `SolutionTreeRequest` twin. No `DropRelativePaths`. | UV-0 |
| **client type** | Architecture docking-host tree. WPF `TreeView` (N7 freeze). VM-nested forest of the flat DTO. | UV-1 |
| **UI** | Architecture pane. Hard states: empty, loading, unindexed leaf, error, no-workspace, stale-while-refresh; skip-count; US-T6 copy. Empty → Show Graph. Zone **not** frozen. | UV-1 |
| **compute reader** | View source → `NodeContentAsync` / `codeviewer`. Reveal in graph → `GraphAsync` / `DescribeAsync`. No third path. No Atlas. | UV-1 |

## Delivery phasing

Serial. UV-1 kind row **must not** land before UV-0 reds exist (AR3).

| Phase | Proves | Real | Mocked | Human | E2E | Unblocks |
|---|---|---|---|---|---|---|
| **UV-0 Core query** | F\* grain: indexed-parent, `unindexed_probe` Unindexed, `bin` absent + skip-count, T5a–c Disclosures, Python/TS honesty, Bicep resolve, frame fit | `SolutionTreeAsync`, IPC integer caps, consume `UnanalysedLanguages.Skip`, `ResolveWithinWorkspace`, no-follow reparse | Shell surface (none); no kind row; T5c omit set on the test-constructed projection | (headless) | US-T1, T2, T3 query-DTO, T4, T5a–c query-DTO (T5c via projection host), T6, T7, T11 minus visual-tree; `EveryOperationFitsTheFrameTests` | UV-1 |
| **UV-1 Shell + one kind row** | Show Solution tree derived; visual-tree oracles; activate | One `solution-tree` row `{Architecture}`; `SolutionTreeSurface`; Enter / Ctrl+Enter | Default-zone choreography still unfrozen; N8 glyph chrome | Open Architecture, Show Solution tree on F\* | US-T3/T5 visual-tree, T8, T9, T10, T13; PROBE-* | Proof Pack; join onto `understanding-views` |

Mock-substitutable seams: `IWorkspaceQueries.SolutionTreeAsync` (Fake refuse / recording stub); UV-0 projection constructor omit set (T5c Core only); internal census-children enumerator (T5a/b when real FS cannot throw). **UV-1 T5c does not construct the projection and does not send `DropRelativePaths`:** App.Tests feeds a Fake/recording stub (or a committed UV-0 golden `SolutionTreeResult` JSON) that already is the omit-set DTO. Do **not** add `InternalsVisibleTo` `AiDe.App.Tests` on Core.

## Contracts

### Exposed

```csharp
// src/AiDe.Core/Projections/IWorkspaceQueries.cs — new method; do not reuse OverviewAsync / GraphAsync
Task<SolutionTreeResult> SolutionTreeAsync(
    SolutionTreeQuery query,
    CancellationToken cancellationToken);

// src/AiDe.Core/Ipc/WorkspaceOperations.cs — catalog, not IpcContract.cs
public const string SolutionTree = "solution-tree"; // kebab, matching search-content

// One query record on the seam AND the wire (ADR-0038: no SolutionTreeRequest twin)
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
    CensusFolderCoverage? Coverage, // non-null iff Kind == CensusFolder
    string? NodeId,                 // file-artifact activate handle; null on census-folders
    string? NodeKind);              // node_dim.node_kind / has_type for glyphs; N8 maps chrome

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

**Guarantees**

- Grain on the wire: one node is one `(Path, Kind)`.
- `Coverage` is null iff `Kind == FileArtifact`.
- `OmittedByCap` is the **only** cap-omit integer. If `OmittedByCap > 0`, exactly one Cap disclosure with `Count = OmittedByCap` and exact copy `Omitted (N)`; if zero, no Cap disclosure.
- Skip-listed directories are absent from `Nodes`. Chrome copy `N skip-listed directories omitted` is UI over `SkipListedDirectoriesOmitted` (absent when N = 0).
- IPC/daemon failure is **not** a Disclosure; it is the existing error+Retry on the query (US-T9).
- Enums travel as strings (`WorkspaceOperations.Wire` already has `JsonStringEnumConverter` **Verified** `:168-172`). Property names follow `JsonSerializerDefaults.Web` (camelCase). Enum **member names** stay as declared (`FileArtifact`, not `file-artifact`).
- `SolutionTreeQuery` JSON has **no** `dropRelativePaths` field. Extra JSON properties are ignored by STJ; they must not omit folders.

**Production caps [Inferred — gap: no measured folder-census cardinality].** `DefaultMaxCensusFolders = 2_000`; `DefaultMaxFileArtifacts = 5_000` (aligned with `GraphProjection.DefaultMaxNodes = 5_000` **Verified** `GraphProjection.cs:191`). Clamp like other projections (`ProjectionService.Clamp` `:1393` **Verified**). Retune when UV-0 emits counts.

### Consumed (each with source + confidence)

| Contract | Source opened | Confidence |
|---|---|---|
| `IWorkspaceQueries` + `LocalWorkspaceQueries` + `WorkspaceClient` | `IWorkspaceQueries.cs:20-154`; `WorkspaceClient.cs:39-182` | **Verified** — add one method; result types are Core’s own (`WorkspaceClient` remarks `:28-31`) |
| `WorkspaceOperations.Handle<TRequest>` + `Register` | `WorkspaceOperations.cs:130-235, 310-331` | **Verified** — `Handle<SolutionTreeQuery>`; const + arm; no Request twin |
| `UnanalysedLanguages.Skip` | `UnanalysedLanguages.cs:48-52` | **Verified** — contains `bin`, `obj`, `.git`, `node_modules` (and more). Lift visibility to `internal` (same instance, not a copy). Do **not** reuse `Enumerate` (`:86-110` follows junctions and swallows IO/permission). |
| `ProjectionService.ResolveWithinWorkspace` | `ProjectionService.cs:1197-1227` | **Verified** — `ScopeLocation` + `artifact_path_id`, `Path.GetFullPath`, separator-terminated prefix, `PathComparison.ForThisFileSystem`, `File.Exists`. Lift `private` → `internal`. Not a second containment function. |
| `StoreReader.FilesToSearch` / `ScopeLocation` | `StoreReader.cs:229-285` | **Verified** — representative node per `(scope_id, artifact_path_id)`; `declared_at` object. Tree join **must not** use `FilesToSearch`’s `LIMIT` as a silent cap (that would omit without `OmittedByCap`). Same SQL, no LIMIT (or a sibling without LIMIT); ranking cap happens in the projection. |
| Graph frame shrink / `MaxFramedGraphBytes` / `FramedCost` | `ProjectionService.cs:251, 596-675, 1360-1368`; `IpcFraming.cs:53` | **Verified** — target = frame 1_048_576 − 64 KiB. Tree shrink: drop lowest-ranked remaining nodes until `FramedCost(result) ≤ MaxFramedGraphBytes` (total order; see Patterns). |
| Reparse refusal class | `EnvelopePurge.cs:77-81` | **Verified** — `DirectoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)` → do not descend. |
| `PathComparison.ForThisFileSystem` | `PathComparison.cs:40-42` | **Verified** |
| Root `.` → `""` | `WorkspaceCore.cs:326` | **Verified** |
| `SurfaceKind` row shape | `SurfaceContentFactory.cs:108-273` | **Verified** — `IReadOnlyList<Perspective> Perspectives`; 18 rows today; `inspector` retired. UV-1 adds one row. |
| `PerspectiveMenu.For` / US-C4 mutation | `PerspectiveMenuTests.cs:495-528`; `MainMenuTests.cs:147-207` | **Verified** — derived Show; no builder-list edit. `TheAllowListsEqualTheSpecsTable` (`:212-244`) must gain the new row at UV-1 (table oracle, not a second menu list). |
| `NodeViewKind.Source` / `GraphNeighbourhood` | `NodeViewMenu.cs:6-14, 44, 68` | **Verified** — no new enum member. |
| WPF TreeView attachments | `docs/spikes/d0-tree-toolkit/RESULT.md` | **Verified** (read+run). Ctrl+Enter physical chord **Inferred** (spike residual). |
| `FakeWorkspaceQueries` virtual refuse | `tests/Shared/FakeWorkspaceQueries.cs:25-63` | **Verified** — linked source; add `SolutionTreeAsync` refuse. |
| `CanvasGraphViewModelTests.StubQueries` | `CanvasGraphViewModelTests.cs:18-80` | **Verified** — still implements `IWorkspaceQueries` by hand; compile tax. |
| `EveryOperationFitsTheFrameTests.AtCeiling` | `:85-146` | **Verified** — reflective census of `IWorkspaceQueries`. |
| Activity source `aide.projection.query` | `ProjectionService.cs:198, 582-584` | **Verified** — tag `projection` = `solution-tree`. |
| ADR-0009 in-process host | ADR-0009 Decision | **Verified** — PROBE-APP-ENUM is App-assembly, not PID. |
| Python/TS `ScopeId` as `ArtifactPathId` | N1 inventory | **Verified** — `python:<rel>` / `typescript:<rel>`; `ResolveWithinWorkspace` will not treat those as files. |
| Bicep filename-only | N1 inventory | **Verified** |

Unfamiliar SDK: **none**. N7 spike already ran. Do not spike NodeContent / Graph / IPC / EnvelopePurge / ResolveWithinWorkspace.

## Patterns (named + justified)

Climbed the Solution-Selection Ladder (L1): YAGNI (no `folder_dim`, no public census/skip types, no Request twin, no Zone freeze) → **reuse in this codebase** (query seam, Skip set, ResolveWithinWorkspace, EnvelopePurge class, Graph shrink budget, derived menu, TreeView) → stdlib (`Directory.EnumerateDirectories`, `FileAttributes.ReparsePoint`) → native WPF TreeView → no new dependency.

| # | Pattern | Why this problem | Rejected alternative |
|---|---|---|---|
| P1 | **CQRS / materialized read model** (`ProjectionService` remarks `:192-195`) | Tree is a rebuildable read over facts + disk-now. | Stored `folder_dim` (second definition of disk-now; Owner N1; YAGNI). |
| P2 | **Query-time join** (DM7 derive-don’t-store) | Census emission ⋈ latest-generation files via `ResolveWithinWorkspace`. | Path-split folders from `artifact_path_id` (Owner forbade; C# paths are project-relative; Python/TS are not paths). |
| P3 | **Existing skip policy, not Strategy** | Consume `UnanalysedLanguages.Skip`. Fail-closed. | Tenth `HashSet`; public `IDirectorySkipPolicy`; reuse `Enumerate` (follows junctions, swallows IO). |
| P4 | **Containment helper, not a second one** | Lift `ResolveWithinWorkspace` to `internal`. | Raw `ScopeLocation` + `artifact_path_id` (N6 BLOCK). |
| P5 | **Reparse refusal class** | Same `FileAttributes.ReparsePoint` test as `EnvelopePurge.Resolve`. | Follow junctions (`Directory.EnumerateDirectories` default). |
| P6 | **Count cap + byte shrink** (INV-0003) | Item caps overflow bytes. | Count-only. |
| P7 | **Ranked prefix shrink** (`simplify:`) | Tree ranking is a **total order**, so drop from the tail until `FramedCost` fits. Ceiling: this stays correct while ranking is total. Upgrade trigger: if ranking becomes non-total (e.g. “keep connected components”), switch to Graph’s proportional+recovery loop (`:618-665`). | Copy Graph’s proportional loop onto a total order (needless). |
| P8 | **One query record** | Fields would otherwise be identical. `Handle<SolutionTreeQuery>`. | `SolutionTreeRequest` twin (Graph has one because wire fields remap). |
| P9 | **Allow-list column + derived join** (ADR-0030) | `Perspectives` = `{Architecture}`; `PerspectiveMenu.For`; `Instances.One` → Show not New. | Hand-written `MainMenuBuilder` string; empty Perspectives set (build test fails). |
| P10 | **HierarchicalDataTemplate over a VM-nested forest** (N7) | Flat DTO on the wire; nest in the VM by parent census-folder **present in the list**. No path-split. | Custom `ListView`/`ItemsControl` (UIA List/ListItem, not Tree/TreeItem — **Verified** spike F4/rejected). WebView2 HTML tree (wrong host). |
| P11 | **Refuse-by-default test double** | `FakeWorkspaceQueries` throws `NotSupportedException` naming the member. | Empty success (hides a wrong call site). |
| P12 | **No model / LOA none** | Spec AI allocation: archetype none; T0 deterministic projection. | D Grounded Synthesizer (would invent folders). |

`// Pattern:` comments to land in code: `Query-time join (DM7; ADR-0038)` on `SolutionTreeProjection.Compute`; `Reparse refusal (EnvelopePurge class)` on the walk; `Derived menu (ADR-0030)` on the kind row.

## Data shapes

### Path normalisation (one function, used everywhere)

```csharp
internal static string NormalizeRelative(string path)
{
    if (string.IsNullOrEmpty(path) || path == ".") return "";
    var s = path.Replace('\\', '/').TrimEnd('/');
    return s == "." ? "" : s;
}
```

Parent of `src/Program.cs` is `src`. Parent of `src` is `""`. Parent of `""` is none. Product equality: `PathComparison.ForThisFileSystem`.

### Census walk (private/internal on `SolutionTreeProjection`; no public `IWorkspaceDirectoryCensus`)

1. If `workspaceRoot` is null/missing: return empty `Nodes`, no invented folders, no plausible empty-success without a no-workspace signal (the App maps “no queries” to no-workspace **before** calling).
2. Emit root `""` as a census-folder when the root exists and is enumerable. Skip set does not omit the root.
3. Iterative stack (do not recurse unbounded). For each directory, `cancellationToken.ThrowIfCancellationRequested()`.
4. Child name in `UnanalysedLanguages.Skip` → **not a node**, `SkipListedDirectoriesOmitted++`, **do not descend**. Production always consults this set. Tests **must not** replace it with a set that omits `bin` / `obj` / `.git` / `node_modules`. F\* `bin/` is omitted because production Skip names `bin`.
5. `DirectoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)` → no node for the unobserved target, do not descend, Disclosure `Not recorded` cause `ReparsePoint`.
6. `IOException` → no node for that path, Disclosure `Not recorded` cause `Io`.
7. `UnauthorizedAccessException` → no node, Disclosure `Not recorded` cause `Permission`.
8. Else emit census-folder and descend.

Do not enumerate files to mint folders. File-artifacts come only from the join.

**Internal enumerator hook (T5a/b seam, not a public walker):**

```csharp
internal delegate IEnumerable<string> CensusChildren(string absoluteDirectory);
// production: Directory.EnumerateDirectories
// T5a: throws IOException for named relative path io_probe (not unindexed_probe, not bin)
// T5b preferred: real ACL deny on omit_probe (D4). Fallback: hook throws UnauthorizedAccessException.
```

`simplify:` one `Func` beats a public `IWorkspaceDirectoryCensus`. Upgrade if a second production walker needs the same hook.

### Join (query-time)

1. **File-artifacts:** latest-generation `(scopeId, artifactPathId, representativeNodeId)` whose `ResolveWithinWorkspace` returns a path with `File.Exists`. Directory-valued assertions, Python/TS `ScopeId` strings, hostile `..`, missing files → not file-artifact nodes. Collapse many assertions that resolve to one path under `PathComparison.ForThisFileSystem`. `NodeId` = representative subject (FilesToSearch choice). `NodeKind` = `ReadNodeKind(nodeId)` (same as `NodeOf`).
2. **Coverage** of census-folder `P`, **bottom-up**: `indexed-parent` iff (a) at least one file-artifact joins under `P` (child or descendant), **or** (b) some scope’s `declared_at` equals `P` under the same normalisation/`PathComparison`, **or** (c) a **descendant census-folder is `indexed-parent`**. Else `unindexed` (non-expanding leaf). (c) is load-bearing: a Python/TS-only ancestor must not hide an indexed descendant.
3. `declared_at` **never mints** a folder. Unresolvable / hostile assertion paths: Disclosure `Not recorded` cause `UnresolvablePath`, no invented folder.
4. **Python/TS:** zero `.py`/`.ts` file-artifact nodes from a second walk (join already excludes ScopeId rows). If census emitted `P` and a Python/TS scope (`scopeId` starts with `python:` or `typescript:` — N1 **Verified**) has `declared_at` equal to `P`, Coverage is `indexed-parent` (ancestors via 3c). Disclosure exact copy: `Python and TypeScript files are not listed individually. The scope folder is indexed.` Cause `PythonTsPerFile`.
5. **Bicep filename-only:** `ResolveWithinWorkspace` onto an **existing** census-folder; never treat the filename as a folder. Else Disclosure `Not recorded` cause `UnresolvablePath`, no node.

### Cap, T5c omit, shrink, orphans

**Order (load-bearing):**

1. Census + join + coverage.
2. **Test-host named drop** (`omitRelativePaths` constructor/`internal` parameter, compared under normalisation/`PathComparison`). Production passes `null`. T5c passes `{omit_probe, omit_probe_2}`. Each dropped census-folder increments `OmittedByCap`. **Not** on `SolutionTreeQuery`. **Not** IPC.
3. **Count cap.** Clamp requested ints. Ranking: keep workspace root; keep indexed-parent folders before unindexed; keep shallower paths before deeper; then ordinal path under `PathComparison.ForThisFileSystem`. This ranking is **not** how tests arrange US-T5c. A test that only lowers `MaxCensusFolders` until an alphabetical prefix drops `unindexed_probe` is the forbidden arrange. Dropped nodes increment `OmittedByCap`. File-artifacts capped separately by `MaxFileArtifacts` after folders.
4. **Drop every file-artifact whose parent census-folder is absent from `Nodes`.** No orphan files under a skipped, unobserved, or cap-omitted parent.
5. **Byte shrink.** `FramedCost(SolutionTreeResult)` mirrors Graph (`Weigh` then full envelope serialise when estimate × 3 > frame). While `FramedCost > MaxFramedGraphBytes` and more than root remains: drop the lowest-ranked node, increment `OmittedByCap`, drop orphan files, remeasure. Terminate when it fits or only root remains (GO12: cap is a circuit breaker, not the termination argument — the ranking prefix is).
6. Derive Cap disclosure from `OmittedByCap`.

### Kind row (UV-1 only)

| Column | Value |
|---|---|
| `Kind` | `solution-tree` |
| `Title` | `Solution tree` |
| `Summary` | Architecture-pane navigator of `(path, kind)` nodes over Core’s census join. |
| `Perspectives` | `[PerspectiveSet.Architecture]` only |
| `Instances` | `One` → **Show Solution tree** |
| `Entry` | `Derived("_View")` |
| `Windowed` | `false` |
| `Zone` | **not frozen** (`null`, like Evidence). Reachability = derived Show (UX-1 ≤ 2 steps). Do not edit `ZoneLayout`. Do not replace `canvas`. |

Place the row in the Architecture block of `Kinds` (row order is menu order — `SurfaceContentFactory.cs:130-134` **Verified**), after existing Architecture kinds, before Coding.

### VM nesting (UV-1; flat DTO → tree)

Index census-folders **present in `Nodes`**. Hang each node under the folder whose path is the parent of its path. If that parent is not a census-folder in the DTO: drop the file-artifact (join step 4 — Core should already have dropped it; VM defends); a census-folder with a missing intermediate becomes a **forest root** (still shown; do not invent `deep` from `deep/nested`). **Verified** spike F15.

`SolutionTreeRow`: `Path`, `Kind`, `Coverage`, `NodeId`, `NodeKind`, `Children`, plus presentation `IsStale`. No behaviour that recomputes Coverage.

## Error & concurrency model

- Projection is **synchronous** inside `ProjectionService` (existing `LocalWorkspaceQueries` uses `Task.FromResult`). Census walk checks `CancellationToken` between directories. Cancel → `OperationCanceledException`, **not** a partial success tree, **not** a Disclosure.
- `Handle<T>` catches **only** JSON decode (`IpcErrorCodes.MalformedEnvelope`). A projection throw is a defect and still escapes (`WorkspaceOperations.cs:306-308` **Verified**). `Refusable` maps `WorkspaceStoreException` only.
- IPC/daemon/transport failures: existing `IpcRequestException` + US-T9 error + Retry. Codes: `ipc.malformed_envelope`, `ipc.epoch_stale`, `ipc.payload_too_large`, `ipc.transport_closed`, … — **no new HTTP**, RFC 9457 N/A.
- Census IO/permission/reparse: Disclosure, result still succeeds.
- Read-only (LOA P8). No write, no retry-at-side-effect. Idempotent: same disk + facts → same DTO.
- Threading: Core walk off the UI thread (existing query path). UV-1 marshals to STA for the `TreeView`. Do not block the UI thread on the census.
- Stale-while-refresh: keep previous rows, mark `Stale`, replace on success; on failure keep previous + error chrome (do not blank to loading — US-T9).
- In-process (ADR-0009) and daemon hosting both supported by the same `IWorkspaceQueries` method.

## Failure-mode analysis

| Failure mode | From which choice | Disposition | How it's addressed | Detection | Test |
|---|---|---|---|---|---|
| Null/missing workspace root | query with no open workspace | prevent (App) | App does not call the query; no-workspace copy | UI state | US-T9 B5 |
| Empty workspace (root-only DTO, no non-root nodes, no shortfall Disclosure) | root-in + B6 | mitigate | Surface empty copy + Show Graph. Do **not** render the root as an Unindexed tree row. | UI empty ≠ Unindexed root | US-T9 B6 |
| Malformed IPC JSON | untrusted payload | detect | `Handle` → `ipc.malformed_envelope` | error code | `DaemonOperationsTests` malformed |
| Extra JSON `dropRelativePaths` | hostile/curious client | prevent | field does not exist; extra properties ignored; folders still present | JSON golden | T5c JSON has no `dropRelativePaths` |
| Integer cap 0 / negative | caller-chosen caps | prevent | `Clamp` like other projections | tags `omitted.by_cap` | clamp facts |
| Count cap drops `unindexed_probe` | alphabetical prefix | prevent (arrange) | T5c uses named omit set, not lowered `MaxCensusFolders` | DTO | US-T5c forbidden arrange fails |
| Frame overflow | long paths × many nodes | mitigate | shrink until `FramedCost ≤ MaxFramedGraphBytes`; `OmittedByCap` | `returned.bytes`; `ipc.payload_too_large` if shrink fails | `EveryOperationFitsTheFrameTests`; hostile census |
| `IOException` mid-walk | real FS | mitigate | no node; Disclosure Io | disclosure cause | US-T5a |
| `UnauthorizedAccessException` | ACL | mitigate | no node; Disclosure Permission | disclosure cause | US-T5b |
| Junction / symlink | `EnumerateDirectories` follows | prevent | ReparsePoint test; no descend | disclosure cause | census does not descend a junction |
| Skip-listed dir as Unindexed | wrong skip / hide-without-rule | prevent | consume production Skip; no node; skip-count | skip count ≥ 1; no `bin` node | US-T4; production skip without injected set |
| Test replaces Skip and omits `bin` | test host | prevent | fail-closed: production set always; tests must not inject a thinner set | F\* `bin` | UV-0 production-skip test |
| Path-split invented folder | `artifact_path_id` segments | prevent | grain: folders only from census emission | DTO | US-T1/T2 failing input |
| `declared_at` mints a folder | coverage rule misread | prevent | rule 2 vs existence | DTO | US-T6 failing input |
| Directory-valued assertion as file | raw join | prevent | `File.Exists` in `ResolveWithinWorkspace` | DTO | US-T1 directory-valued |
| Hostile `..` / escape | assertion path | prevent | containment prefix + `File.Exists` | Disclosure UnresolvablePath | B13 |
| Python/TS fake per-file rows | second walk / ScopeId as path | prevent | no second walk; Resolve fails | zero `.py`/`.ts` file-artifacts; exact US-T6 copy | US-T6 |
| Bicep filename becomes a folder | filename-only provenance | prevent | resolve onto existing census-folder or Disclosure | DTO | US-T7 |
| Python/TS-only ancestor hides indexed descendant | coverage without 3c | prevent | bottom-up 3c | DTO | ancestor coverage test |
| Orphan file under omitted parent | cap/skip after join | prevent | drop files whose parent folder is absent | DTO | orphan-file test |
| Duplicate nodes for one path | many assertions | prevent | collapse under PathComparison | DTO | US-T1 collapse |
| Cancel mid-walk returned as empty success | ignoring CT | prevent | throw OCE; App error+Retry | exception | cancel fact |
| Overview/Graph reused as tree | wrong grain | prevent | new method + new IPC id | catalog | US-T11 |
| App enumerates workspace | DC-022 | prevent | Core-only walk; PROBE-APP-ENUM | probe | US-T11 |
| App reads file for View source | DC-022 | prevent | `NodeContentAsync` only; PROBE-FILE-READ | probe | US-T8/T11 |
| Atlas type loaded | Owner forbid | prevent | no `Understanding/` reference; PROBE-ATLAS | probe | US-T11 |
| Kind row before query | AR3 | prevent | UV-0 first; UV-1 serial | `Kinds` has no row in UV-0 | US-T10 first Given |
| Empty Perspectives set | ADR-0030 | prevent | `{Architecture}`; existing non-empty-set test | build test | PerspectiveMenuTests |
| Hand-written menu string | second list | prevent | `PerspectiveMenu.For` only; **not** `MainMenuBuilder` lists | US-C4 mutation | US-T10 |
| IPC failure shown as empty tree | mixing Disclosure and transport | prevent | US-T9 error+Retry | UI | B8 |
| Refresh blanks to loading | state machine | prevent | stale-while-refresh | UI | B7s |
| Unindexed expands empty children | TreeView default / dummy child | prevent | empty `Children`; swallow double-click (N7 F10) | visual-tree | US-T3 |
| `Height=28` on `TreeViewItem` clips children | wrong 28px attach | prevent | header content `MinHeight=28` (N7 F7) | visual-tree | UI-10 |
| Telemetry emits workspace path | O11 | prevent | counts and enum names only; degrade missing to omit-tag, never a fake 0 for an unmeasured axis | `TelemetryTests` | no path in tags |
| Cap 2000/5000 wrong for a huge repo | unmeasured | accept | labelled **Inferred**; `Omitted (N)` still honest; retune when counts emit | `omitted.by_cap` | residual |
| Other walkers disagree with Skip | nine lists | accept | UV-0 binds Skip; N7 may widen others | skip-count | residual (not UV-0 blocker) |
| Physical Ctrl+Enter unproven by RaiseEvent | WPF test limit | accept until UV-1 SendInput/attended | PreviewKeyDown reads `Keyboard.Modifiers` | — | spike residual |

## Adversarial analysis (STRIDE-lite)

Trust boundaries: (1) App/shell → Core IPC `solution-tree`; (2) Core census → workspace filesystem; (3) Core join → SQLite facts; (4) TreeView → operator.

| Trust boundary | STRIDE threat | Disposition | Control / rationale | Negative test |
|---|---|---|---|---|
| App → Core IPC | S: forged/stale caller | transfer | Existing named-pipe ACL, capability, epoch (`IpcErrorCodes.EpochStale`) | existing daemon epoch tests |
| App → Core IPC | T: `DropRelativePaths` hides folders behind `Omitted (N)` | prevent | Field not on query/IPC; named omit is projection/test-host only (N6) | JSON has no `dropRelativePaths`; sending it does not omit `omit_probe` |
| App → Core IPC | T: map operation onto `overview`/`graph` | prevent | New const `solution-tree`; catalog test | US-T11 |
| App → Core IPC | I: payload describes filesystem to a curious client | accept (residual) | Paths are workspace-relative, already on other reads (`SearchContent`, `NodeContent`). No absolute paths. Same-user desktop residual as threat-model row 1. | no absolute path in DTO |
| App → Core IPC | D: unbounded census | mitigate | count caps + frame shrink; `ipc.payload_too_large` if a result still will not fit | `EveryOperationFitsTheFrameTests` |
| Core → filesystem | T: junction escape / TOCTOU reparse | mitigate | do not follow reparse points (EnvelopePurge class); `ResolveWithinWorkspace` containment + `File.Exists` | junction Disclosure; hostile `..` |
| Core → filesystem | I: skip-listed dirs re-enter as Unindexed | prevent | Skip names are not nodes (US-T4) | no `bin` node; skip-count ≥ 1 |
| Core → filesystem | E: App walks disk (DC-022) | prevent | walk only in Core projection; PROBE-APP-ENUM = App assembly / non-`IWorkspaceQueries` callers, **not PID** (ADR-0009 in-process) | PROBE-APP-ENUM |
| Core → facts | T: path-split folders | prevent | grain | US-T1/T2 |
| TreeView → operator | I: colour-only Unindexed | prevent | word + glyph + `{colors.unverified}` (spec UI-2) | visual-tree US-T3 |
| TreeView → operator | I: skip silence (VS Code copy) | prevent | skip-count chrome, no greyed `bin` row | US-T4 |
| Any | I: path/prompt in span tags | prevent | O11; counts/enum names | `TelemetryTests.NoSpanAttribute_CarriesAPathPromptOrSourceText` |
| Any | E: Atlas `Understanding` types | prevent | no reference; PROBE-ATLAS | PROBE-ATLAS |
| View source | I: App `File.ReadAllText` on workspace path | prevent | `NodeContentAsync`; PROBE-FILE-READ (existing App reads of prompt-draft/session JSON are not workspace source) | PROBE-FILE-READ |

## Privacy analysis (LINDDUN-lite)

Workspace-relative paths already live in Core (`artifact_path_id`, `declared_at`). This component **does not collect a new personal-data category**, does not add egress, and does not send paths to a model (LOA none). Folder names **may** contain usernames; that is existing workspace data.

| Data flow / category | LINDDUN finding | Disposition | Control / rationale | Retention & rights path |
|---|---|---|---|---|
| Workspace-relative paths on the DTO | D: paths in logs/telemetry | mitigate | Span tags are counts and shortfall **cause names**, never `Path`. Degrade to omit-tag, never a plausible wrong number. | Same as workspace facts; no extra retention. Erasure = delete workspace data dir (existing). |
| Census disk-now | L/I: tree reveals on-disk folders the index did not cover | accept | That is the product purpose (Unindexed). Confined to the workspace owner’s machine. | Local workspace only. |
| IPC payload | D: absolute paths | prevent | DTO paths are workspace-relative; `ResolveWithinWorkspace` absolute path does not leave Core. | — |

No new lawful-basis work. Regulatory role remains **Flagged** on the repo privacy review (pre-existing).

## UI & interaction design

Applies at **UV-1**. N8 `/ui-design` still owns glyph-to-kind chrome and any DESIGN.md component-token additions. This design binds the spec Part C tokens and freezes the N7 control. **Deviation from design-slice DoD “update DESIGN.md”:** tokens already exist (`DESIGN.md:9-29,177`); no new primitive. N8 may add component tokens. `ui-craft-gate.py` is named in the UV-1 test plan (CD8) and is not a UV-0 gate.

**Archetype:** B1 Keyboard-Velocity GUI (spec Part C signature, quoted not thinned). Why not H2: H2 is file CRUD; N2 forbids it. Why not C1: graph already is the spatial surface.

**Medium(s) & platform guidelines:** native desktop WPF; Windows / Fluent; pack native-client-ui-design (UIA, keyboard, High Contrast, DPI).

**Tokens used (no arbitrary values):**

| Role | Token |
|---|---|
| Tree ground | `{colors.surface}` / `{colors.surface-raised}` |
| Primary node name | `{colors.text}` |
| Coverage / disclosure / skip-count | `{colors.text-muted}` |
| Unindexed (word + glyph; colour third) | `{colors.unverified}` — **not** `{colors.inferred}` or `{colors.stale}` |
| Not-recorded | `{colors.unverified}` + word `Not recorded` |
| Stale-while-refresh | `{colors.stale}` + word `Stale` |
| Error | `{colors.danger}` |
| Selection | `{colors.accent}` ground, **only** `{colors.accent-contrast}` ink |
| Focus ring | 2px `{colors.focus}` |
| Separators | `{colors.border}`; control bounds `{colors.border-strong}` |
| Type | `{typography.ui}`; `{typography.mono}` only if a path is shown as a path |
| Density | Compact 28px list rows, 24×24 target (`DESIGN.md:177`) |
| Icon | `{icon.sm}` (16px) inside the 28px row; **hit rect is the full row** |

**Key screens / flows:** spec Part B mermaid (Architecture → no-workspace / loading / stale / query → tree or empty+Show Graph or error+Retry; Enter View source; Ctrl+Enter Reveal in graph). Focal point: the selected path.

**Component states (complete set):**

| Component | default | hover/focus | active | disabled | loading | empty | error | success | first-run / overflow |
|---|---|---|---|---|---|---|---|---|---|
| Tree | populated rows | row hover + 2px focus ring | selected accent | — | `Reading the workspace tree…` | `No indexed artifacts or folders to show.` + **Show Graph** | `Could not read the workspace tree.` + **Retry** | populated | first-run = empty/no-workspace; overflow = virtualize + `Omitted (N)` |
| Census-folder indexed-parent | name + coverage | hover/focus | selected; expanded/collapsed | — | — | — | — | — | — |
| Census-folder unindexed | name + `Unindexed`; **leaf** | hover/focus | selected; **not** expanded | — | — | — | must not show Error on Enter/Right | — | Hidden expander (16px slot, not shown) |
| File-artifact | name + kind word | hover/focus | selected; activating | — | — | — | activate error is on the **reveal** surface | opened | — |
| Skip-listed dir | **no component** | — | — | — | — | — | — | — | counted in chrome |
| Retry | default | focus | pressed | disabled in-flight | — | — | — | — | — |
| Show Graph | empty only | focus | pressed | — | — | required | — | — | — |
| No-workspace | `Open a workspace to see its solution tree.` | — | — | — | — | — | — | — | — |

**N7 attachments UV-1 must ship (not platform defaults) — Verified spike:**

| Attachment | Why |
|---|---|
| `VirtualizingPanel.IsVirtualizing=True`, `VirtualizationMode=Recycling` on this `TreeView` | Default is off; 400/400 realized in 280px. Opt-in: 18/400. |
| Header content `MinHeight=28` (Border around the label), **not** `Height=28` on `TreeViewItem` | Item Height clips children; item MinHeight does not enlarge `PART_Header`. |
| `ItemContainerStyle` `AutomationProperties.Name` → kind+coverage string | Unbound Name is header only. Bound: `Program.cs file-artifact`, `unindexed_probe census-folder Unindexed`. Native Tree/TreeItem peers. |
| `PreviewKeyDown` on the tree for Enter (View source) and Ctrl+Enter (Reveal in graph) | Platform does not handle `Key.Return`. |
| Unindexed: empty `Children`, never a dummy child; swallow double-click (`Handled` when `HasItems` is false) | Double-click otherwise sets `IsExpanded=true` with zero children. |
| Do not add a second global `TreeViewItem` style in `App.xaml` | `:725-734` is theme-only; a global style would reshape Console / contrast-probe trees. |

**Motion:** Hard-cut loading → result. Indexed-parent expand may use `{motion.fast}` / `{motion.base}` gated on `prefers-reduced-motion` / Windows animation. Unindexed does not animate expand. No skeleton that looks like a complete tree.

**UI copy (exact):**

- Unindexed: `Unindexed`
- Not recorded: `Not recorded`
- Cap: `Omitted (N)`
- Skip: `N skip-listed directories omitted` (absent when N = 0)
- No-workspace: `Open a workspace to see its solution tree.`
- Loading: `Reading the workspace tree…`
- Stale: `Stale`
- Empty: `No indexed artifacts or folders to show.` Action: `Show Graph`
- Tree error: `Could not read the workspace tree.` Action: `Retry`
- View source error: `Could not open source.` Action: `Retry`
- Reveal error: `Could not reveal in graph.` Action: `Retry`
- Python/TS: `Python and TypeScript files are not listed individually. The scope folder is indexed.`

**Medium adaptation:** Windows WPF only. High Contrast uses the same semantic roles.

**Accessibility (WCAG 2.2 AA) & performance:** role Tree/TreeItem; Name includes kind and coverage; keyboard Up/Down/Left/Right/Enter/Ctrl+Enter; coverage not colour-only; 2px focus ring; full-row 28px hit ≥24×24; virtualize; bounded payload. No CI duration assertion (ADR-0029 / DC-107).

**AI-UX:** N/A — D-0 is not an AI-facing surface.

**Activate (no third path):**

| Gesture | Action | Seam |
|---|---|---|
| Enter on file-artifact | View source | `NodeContentAsync` → admitted `codeviewer` (`NodeViewKind.Source`) |
| Ctrl+Enter on file-artifact | Reveal in graph | `GraphAsync` / `DescribeAsync` on `NodeId` (`NodeViewKind.GraphNeighbourhood`) |
| Indexed-parent Right/Enter | Expand / collapse | no content, no graph entity |
| Unindexed Enter/Right/Left | Leaf; no-op | no children, no activate |

Reveal/View-source failure → error + Retry **on that surface**; tree selection unchanged.

## Telemetry (IO1 / O1–O13)

Unit of work: one `SolutionTreeAsync` invocation. Span: existing `ActivitySource("aide.projection.query")`, `StartActivity("aide.projection.query")`, tag `projection` = `solution-tree` (same as Graph `:582-584` **Verified**). Duration is the Activity’s own; **recorded, not CI-asserted** (ADR-0029).

**Tags (low cardinality — O13). Never a workspace path, never source text (O11).** If a value is unknown, **omit the tag** (not recorded). Do not emit a plausible 0 for an unmeasured axis. A real observed 0 (no skips) **is** recorded.

| Tag | Meaning |
|---|---|
| `projection` | `solution-tree` |
| `returned.census_folders` | count of census-folder nodes |
| `returned.file_artifacts` | count of file-artifact nodes |
| `returned.indexed_parent` | count |
| `returned.unindexed` | count |
| `skip.omitted` | `SkipListedDirectoriesOmitted` |
| `omitted.by_cap` | `OmittedByCap` |
| `returned.bytes` | `FramedCost` |
| `shortfall.causes` | sorted unique enum names, comma-separated (or omit if none) |
| `shrunk.attempts` | shrink rounds (0 if none) |
| `outcome` | `ok` / `canceled` / error code |

**Logs:** structured template inside the Activity so trace context attaches (O1–O2). Example: `Solution tree {CensusFolders} folders {FileArtifacts} files omitted {OmittedByCap}`. ERROR+ only for projection defects / transport, not for Disclosures (O5).

**Error codes:** existing `IpcErrorCodes` + `ProjectionErrorCodes`. No new HTTP problem documents (O8 N/A). Census shortfalls are Disclosures, not error codes.

**Metrics:** Activity tags are the metric source this repo already uses for projections (O10). Do not add a new `Meter` for one query (ladder).

**Load-bearing telemetry tests (O12):** `TelemetryTests` gains a SolutionTree case asserting the named tags on a F\* run **and** the existing privacy test still fails if a path appears. Latency is emitted; no duration-under-constant assert.

## Test plan

**Triggers (union):** T1 (coverage, ranking, normalisation) → D1; T2 (path/JSON space) → D2 as example-based identities (do **not** add FsCheck); T3 (new seam, App must not enumerate) → D3; T4 (real disk, ACL, reparse, `File.Exists`) → D4; T6 (IPC provider) → existing daemon/frame/JSON tests, **not** Pact (not installed; deviation: fidelity via `DaemonOperationsTests` + `EveryOperationFitsTheFrameTests` + JSON golden); T7 (payload) → D6 synthetic golden; T8 (`FakeWorkspaceQueries`) → D7 refuse-not-empty paired with real projection tests. T5/T9–T14 **not** triggered (no HTTP consumer, no model). **D0** on every test.

**Red-first.** UV-0 reds compile/fail before UV-1 kind row (AR3). Query-only green is **not** a pass for US-T3/T5 (spec oracle: DTO **and** visual-tree) — visual-tree lands in UV-1; UV-0 still ships the DTO half.

**F\* (one real-disk workspace):** indexed-parent dir + joinable file; `unindexed_probe/` (not skip-listed, zero joinable files, not `declared_at`, **not `docs/`**); `omit_probe/` + `omit_probe_2/`; **`io_probe/`** (T5a — not skip-listed, not `unindexed_probe`, not `bin`; Core census IO seam fails on this path); `bin/` with files; skip set is production `UnanalysedLanguages.Skip`.

### UV-0 (headless Core) — map to US-T*

| Test | Maps to | Arrange | Assert (falsifying input) |
|---|---|---|---|
| `IWorkspaceQueries` declares `SolutionTreeAsync`; catalog contains `solution-tree` and not a mapping onto `overview`/`graph` | US-T11 | compile + `WorkspaceOperations` const | handler is `overview`/`graph` |
| `FakeWorkspaceQueries.SolutionTreeAsync` virtual refuse | compile tax / D7 | call without override | empty success |
| `CanvasGraphViewModelTests.StubQueries` implements the new method | compile tax | — | project does not compile |
| `EveryOperationFitsTheFrameTests.EveryReadOperationIsCoveredByThisTest` | INV-0003 control | add method without AtCeiling | missing `SolutionTreeAsync` |
| `AtCeiling` SolutionTree + hostile long-name census shrinks under `MaxFramedGraphBytes` | US-T5c class / T11 | many long folder names on disk; `ProjectionService(store, workspaceRoot)` | frame overflow; omit silent |
| F\* DTO: indexed-parent + file-artifact; `unindexed_probe` coverage `unindexed`; no `bin` node; skip-count ≥ 1 | US-T1, T2, T3 DTO, T4 | real F\* | Overview clusters as rows; `bin` as Unindexed; `unindexed_probe` missing |
| Collapse two assertions → one file-artifact | US-T1 | two facts, one path | two nodes |
| Directory-valued assertion is census-folder not file-artifact | US-T1 | `File.Exists` false | file node |
| US-T5a: Core arrange IO on F\* `io_probe/` (not `unindexed_probe`, not `bin`) | US-T5a DTO | F\* includes `io_probe/`; enumerator hook throws `IOException` (or real FS if deterministic) | silent drop; minted folder; labelled Unindexed |
| US-T5b: ACL deny `omit_probe` (not `unindexed_probe`) | US-T5b DTO | real ACL (D4) or hook `UnauthorizedAccessException` | denied dir is a node; `unindexed_probe` missing; skip-count 0 |
| US-T5c: construct projection with omit `{omit_probe, omit_probe_2}` | US-T5c DTO | constructor/`internal` omit set on Core test host only | generic cap-below-count that drops `unindexed_probe`; `omit_probe` still present; truncation with no `Omitted (N)`; cap treated as Coverage `unindexed` |
| `SolutionTreeQuery` JSON has no `dropRelativePaths`; extra field does not omit | US-T5c / N6 | serialize; deserialize with extra property | production hide API |
| Lowering `MaxCensusFolders` until `unindexed_probe` disappears is **not** the T5c arrange (document as forbidden; a guard test that this arrange is **insufficient** to claim T5c) | US-T5c residual | — | using only the integer cap as T5c |
| Production skip: F\* `bin` omitted with **no** test-injected skip set | US-T4 | production Skip | injected `bin` |
| Python/TS: `P` indexed-parent; zero `.py`/`.ts` file-artifacts; exact US-T6 copy | US-T6 | `declared_at` = census `P`; ScopeId artifact path | fake per-file rows; paraphrased copy; `declared_at` minted `P` |
| Ancestor 3c: Python/TS-only parent of indexed-parent descendant is `indexed-parent` | US-T2/T6 | nested dirs | parent `unindexed` |
| Bicep resolvable → file under existing folder; unresolvable → Disclosure, filename ≠ folder | US-T7 | filename-only provenance | folder named for `.bicep` |
| Hostile `..` → Disclosure, no invented folder | B13 | assertion path | escaped node |
| File-artifact whose parent folder is absent is absent | join step 4 | cap-omit parent | orphan file |
| Census does not descend a junction; Disclosure `ReparsePoint` | N6 | real junction | followed target as nodes |
| `Omitted (N)` derives from `OmittedByCap`; no second omit integer | ADR one numeric home | T5c | disclosure count ≠ field |
| Telemetry tags present; no path in tags | IO1 / O11 / O12 | ActivityListener | path/`prompt` in tags; missing tags on happy path |
| Daemon round-trip + malformed payload | T6/D6 | `DaemonOperationsTests` | unregistered operation |
| JSON golden (synthetic F\*-shaped payload) | D6 | committed fixture labelled synthetic | field dropped on wire |
| Path normalisation identity: `.` → `""`; `\` → `/`; no trailing slash; `Normalize(Normalize(p))==Normalize(p)` | D2 | examples | Windows/POSIX comparison uses `PathComparison` |
| Cancel mid-walk throws OCE | concurrency | CT | empty success |

**UV-0 does not:** add a kind row; visual-tree walk; PROBE-APP-ENUM runtime (static IL scan of `AiDe.App` for the PROBE-APP-ENUM API set **may** land in UV-0 as a D3 tripwire — App currently has **zero** such calls **Verified** grep).

### UV-1 (App, after UV-0 green)

**UV-1 T5c visual-tree arrange (Test Architect, load-bearing).** `AiDe.App.Tests` does **not** construct `SolutionTreeProjection` / the internal omit set and does **not** send `DropRelativePaths` (that field is not on the query). Arrange: a `FakeWorkspaceQueries` / recording stub, **or** a committed UV-0 golden `SolutionTreeResult` JSON, that already returns the Core omit-set DTO (`omit_probe` and `omit_probe_2` absent, `OmittedByCap` ≥ 2, Cap disclosure `Omitted (N)`, `unindexed_probe` present coverage `unindexed`). Then: chrome shows `Omitted (N)`; no `omit_probe` row; `unindexed_probe` is an Unindexed leaf. Do **not** add `InternalsVisibleTo` `AiDe.App.Tests` on Core (`AiDe.Core.csproj` stays `AiDe.Core.Tests` only for this seam).

| Test | Maps to | Notes |
|---|---|---|
| Headless visual-tree walk of Solution tree on F\* | US-T3, T5a–b visual half | `Sta.Run` like `ClassDiagramSurfaceTests`. Query-only green is not a pass. T5c is the next row — not this F\* walk with a lowered cap. |
| Unindexed row visible text includes `unindexed_probe` and `Unindexed`; zero child rows; UIA Name includes coverage | US-T3, UI-9 | |
| Skip: no `bin` row; chrome `N skip-listed directories omitted` N ≥ 1 | US-T4, UI-3 | |
| T5a/b Disclosures visible; T3 still holds | US-T5a–b | T5a on F\* `io_probe/`; T5b on `omit_probe`. Core-arranged DTO or Fake that carries that Disclosure. |
| T5c visual-tree: Fake/recording stub or UV-0 golden JSON (Core omit-set DTO). Chrome `Omitted (N)`; no `omit_probe` row; `unindexed_probe` Unindexed leaf. **Not** App constructing the omit set. **Not** `DropRelativePaths` on the wire. | US-T5c | Failing: generic cap drops `unindexed_probe`; `omit_probe` still present; cap as Coverage; query-only green |
| US-T6 / UI-8 exact copy in the tree chrome | US-T6, UI-8 | Failing input: copy absent or paraphrased |
| UIA Name of file-artifact includes kind word (`NodeKind` or `file-artifact`) | US-T1, UI-9 | glyph chrome N8; Name is UV-1 |
| Header hit rect 28px, ≥24×24; not 44px; not 16px chevron-only | UI-10 | N7 F7 |
| Enter → View source (`NodeContentAsync` / `codeviewer`); Ctrl+Enter → Reveal in graph; tree selection unchanged on reveal error + Retry | US-T8 | Do **not** treat synthetic RaiseEvent as proof of the Ctrl chord (spike residual). PreviewKeyDown + `Keyboard.Modifiers` on real input; attended/SendInput when available. |
| Indexed-parent Right expands; unindexed Enter/Right/Left do not; double-click does not expand unindexed | US-T8, UX-5 | N7 F8/F10 |
| Empty / loading / error+Retry / no-workspace / stale-while-refresh distinct | US-T9, UI-1 | |
| US-T9 B6 empty: arrange root-only DTO (census-folder `""` only, no non-root nodes, no shortfall Disclosure). Then empty copy + Show Graph. | US-T9 B6 | Failing input: empty copy replaced by an Unindexed root row |
| Kind row `{Architecture}`; Show Solution tree via `PerspectiveMenu.For`; **no** `MainMenuBuilder` list edit; Coding/Explore/Coordination do not admit; US-C4 mutation still holds; `TheAllowListsEqualTheSpecsTable` updated | US-T10 | |
| PROBE-APP-ENUM (App assembly / non-`IWorkspaceQueries` callers, **not PID**); PROBE-ATLAS; PROBE-FILE-READ | US-T11, B14 | IL/source: `AiDe.App` types must not call `Directory.EnumerateFileSystemEntries` / `EnumerateDirectories` / `EnumerateFiles` / `GetDirectories` / `GetFiles` / `GetFileSystemEntries` / **`DirectoryInfo.EnumerateDirectories` / `EnumerateFileSystemInfos` / `GetFileSystemInfos`**. Core census inside `LocalWorkspaceQueries` / `ProjectionService` / `SolutionTreeProjection` allowed in-process. View source must not `File.ReadAllText`/`OpenRead` the workspace path. No type with namespace prefix `AiDe.Core.Understanding`. |
| Explore body still ADR-0017 graph+reader | US-T13 | |
| `FieldsSurviveTheClientBoundaryTests` pair `SolutionTreeNode` → `SolutionTreeRow` | DC-016 class | `SourceRevision` may be deliberately dropped (chrome, not row) — name it |
| `ui-craft-gate.py` against the built surface | CD8 | accessibility Major-min / token Major-min. N8 may add tokens first; UV-1 still must not use off-token hex. |

## Conformance notes

- **LOA:** product host unchanged (F). This addition: **no model**; archetype **none**; tier **T0** deterministic projection. C1 N/A for cognition. P1 cheapest sufficient (one query + one kind). P2 derived menu, one skip policy, query-time join. P8 read-only. P11 App still cannot read workspace files. P5 Coverage vs Disclosure.
- **C#:** sealed records; `JsonStringEnumConverter`; kebab IPC id; `Task.FromResult` adapter; lift visibility rather than copy; `InternalsVisibleTo` already `AiDe.Core.Tests` (`AiDe.Core.csproj:52` **Verified**). Do **not** add `InternalsVisibleTo` `AiDe.App.Tests` on Core — UV-1 T5c uses a Fake/golden DTO, not the internal omit set.
- **AR3 / ADR-0030:** kind row only in UV-1; menu derived.
- **Deviations:** Stage 4 council skipped (N10) — authors do not self-clear. DESIGN.md not rewritten (N8). Pact not added (T6). Zone/layout not frozen (Owner/user). `docs/security/threat-model.md` template path unused — repo rollup lives in `docs/security/ai-native-ide-threat-model.md` / `ai-native-ide-privacy-review.md`.

## Implementer file lists (seam, not this turn’s code)

### UV-0 Core query — do these first; reds before any kind row

| File | Change |
|---|---|
| `src/AiDe.Core/Projections/IWorkspaceQueries.cs` | `SolutionTreeAsync`; `LocalWorkspaceQueries` adapter passing `CancellationToken` into the walk |
| `src/AiDe.Core/Projections/SolutionTreeProjection.cs` | **new** — types + `Compute`; census walk; omit set as constructor/`internal` parameter; enumerator hook `internal`; **not** a query field |
| `src/AiDe.Core/Projections/ProjectionService.cs` | `SolutionTree`; activity tags; call existing `ResolveWithinWorkspace` (lift `internal`); `FramedCost` overload; ranked shrink |
| `src/AiDe.Core/Extraction/UnanalysedLanguages.cs` | `Skip` visibility `internal` — **same instance**, not a copy |
| `src/AiDe.Core/Ipc/WorkspaceOperations.cs` | const `solution-tree`; `Handle<SolutionTreeQuery>` arm on `Register` |
| `src/AiDe.Core/Ipc/WorkspaceClient.cs` | `SolutionTreeAsync` sends `SolutionTreeQuery` |
| `src/AiDe.Core/Store/StoreReader.cs` | reuse `FilesToSearch` SQL **without silent LIMIT** + `ScopeLocation` as inputs to Resolve |
| `src/AiDe.Daemon/Program.cs` | **only if** registration cannot stay on `WorkspaceOperations.Register` |
| `tests/Shared/FakeWorkspaceQueries.cs` | virtual refuse for `SolutionTreeAsync` |
| `tests/AiDe.Core.Tests/CanvasGraphViewModelTests.cs` | `StubQueries` compile tax |
| `tests/AiDe.Core.Tests/EveryOperationFitsTheFrameTests.cs` | `AtCeiling` entry; hostile census needs a workspace root |
| `tests/AiDe.Core.Tests/DaemonOperationsTests.cs` | new operation |
| `tests/AiDe.Core.Tests/TelemetryTests.cs` | solution-tree tags + privacy |
| new Core tests | F\* T5c constructs the projection; T5a/b Core arrange; reparse; 3c; orphans; JSON; production skip |

### UV-1 Shell — only after UV-0 DTO reds exist

| File | Change |
|---|---|
| `src/AiDe.App/Workbench/SurfaceContentFactory.cs` | **one** row; `Zone` null |
| `src/AiDe.App/Workbench/SolutionTreeSurface.cs` (name) | TreeView + N7 attachments + VM nest |
| `src/AiDe.App/Workbench/NodeViewMenu.cs` | **only if** Open-as must list the tree — default is Enter/Ctrl+Enter mapped to existing `NodeViewKind`; **no new enum member** |
| **not** `MainMenuBuilder` lists | derived |
| **not** `ZoneLayout` | freeze View-menu-only: Ruling 94 Left stays Graph; `solution-tree` admitted, not in `ArchitectureDefault` |
| `tests/AiDe.App.Tests/Workbench/PerspectiveMenuTests.cs` | `TheAllowListsEqualTheSpecsTable` expected row; US-C4 still holds |
| `tests/AiDe.App.Tests/` | visual-tree (T5c via Fake/golden DTO, **not** Core omit ctor); menu mutation; PROBE-APP-ENUM (incl. `DirectoryInfo.EnumerateDirectories` / `EnumerateFileSystemInfos` / `GetFileSystemInfos`) / ATLAS / FILE-READ |
| `tests/AiDe.Core.Tests/FieldsSurviveTheClientBoundaryTests.cs` | node → row pair |

### Must not touch this horizon

`src/AiDe.Core/Understanding/**`; Atlas; `session-contracts.md` as a D-0 seam; extractor Python/TS provenance rewrite; Addenda C/D compile types; D-1…D-6 rows; a tenth Skip HashSet; public `IDirectorySkipPolicy` / `IWorkspaceDirectoryCensus`; `DropRelativePaths` on the wire; `InternalsVisibleTo` `AiDe.App.Tests` on Core.

## Flagged risks & residual unknowns

| Unknown | Label | Disposition |
|---|---|---|
| Production caps 2000/5000 | **Inferred** | Retune when UV-0 emits counts. Honest `Omitted (N)` meanwhile. |
| Empty UI vs root-in | **closed (N10 Test Architect)** | Arrange B6 as root-only DTO + no non-root nodes + no shortfall. Failing input: empty copy replaced by an Unindexed root row. |
| Physical Ctrl+Enter | **Flagged** (spike) | PreviewKeyDown is the control; RaiseEvent is not proof. HandleKey(Control) and the node menu are the tested pointer/keyboard Reveal paths. |
| Other walkers vs Skip | **Flagged** | N7 optional widen; UV-0 binds Skip. |
| Glyph-to-kind map | **closed (N8)** | Three stroke glyphs (folder / file / dashed-folder). UIA Name still includes the kind word. Not a per-`NodeKind` icon set. |
| Default Left vs View-menu-only | **frozen View-menu-only** | Ruling 94 Left stays Graph. `solution-tree` is admitted; not in `ArchitectureDefault`. UX-1 still ≤ 2 steps via Show. |
| T5a deterministic real-FS IOException | **Inferred** | Internal enumerator hook is the named Core seam. |
| Stage 4 / N10 | **open** | This design stays **draft**. Authors do not self-clear. |

**Residual risk:** a later slice “completes” Python/TS by walking files in the App or minting fake rows — out unless Owner admits a separate slice. **Residual risk:** skip-omission as hide-without-a-rule — US-T4 fails that.

## Definition of done (this run)

| Item | Evidence |
|---|---|
| Single responsibility + contracts | §Responsibility, §Contracts |
| Data model first | §Data model — no `folder_dim`; grain `(path, kind)`; derive-don’t-store |
| E7 list | §E7 |
| Phasing | UV-0 then UV-1 serial |
| Consumed contracts established; unfamiliar spiked | N7 RESULT.md; Core opened |
| Patterns named + ladder | §Patterns |
| Failure-mode / STRIDE / LINDDUN | tables |
| UI (medium, tokens, states, copy, a11y) | §UI; DESIGN.md update **deferred N8** |
| ui-craft-gate named | UV-1 test plan |
| Telemetry O1–O13 | §Telemetry |
| Testing Strategy union | §Test plan |
| Hard vetoes resolved by someone other than the author | **unmet — N10**; status draft |
| Confidence ledger | tables + Flagged |
| Status table | below |

## Status & next action

| | |
|---|---|
| **Completed** | N9 repair of N10 Test Architect BLOCK. UV-0 + UV-1 walking skeleton. N8 chrome: kind glyphs, stale glyph, file double-click View source, dual-activate node menu. Zone frozen View-menu-only (Ruling 94). Status remains **draft**. |
| **Remaining** | N10 re-review (authors do **not** self-clear). Physical Ctrl+Enter attended/SendInput. Join recount + `tools/run-verify-gates.py` on `understanding-views`. Not `main`. |
| **Best next action** | N10 Test Architect + remaining lenses re-read this file and the chrome tests. Do not admit D-1. |

## Gate record

`GATE design · 2026-09-15 · N9 author repair (Peer Mode) of N10 Test Architect BLOCK · status **draft, not accepted** · authors do not self-clear · six closes in the test plan (UV-1 T5c Fake/golden; US-T6/UI-8; B6; `io_probe/`; DirectoryInfo PROBE-APP-ENUM; T5c failing inputs).`

Authors did not self-clear.

---

**Handoff:** N10 review → `/implement` UV-0 then UV-1.
