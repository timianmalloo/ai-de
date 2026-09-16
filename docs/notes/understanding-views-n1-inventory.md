---
id: note-understanding-views-n1-inventory
title: "N1 inventory — D-0 Solution/tree substrate (Architecture kinds, artifact_path_id, unindexed folders)"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, inventory, D-0]
links:
  - { to: note-understanding-views-owner-ruling, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Current-state inventory for D-0. No SolutionTree/WorkspaceTree. OverviewAsync/GraphAsync are the
  wrong grain. artifact_path_id is assertion provenance, not an artifact dimension; several
  extractors do not store file paths. Existing facts cannot emit §A5 unindexed folders without a
  Core-side disk census. STOP-BEFORE-N5.
---

# N1 inventory — D-0 Solution/tree substrate

- **Kind:** investigation (filed as decision-note draft so specify can link it)
- **Confidence:** per clause (Verified | Inferred | Flagged)
- **Made during:** N1 of `plan-understanding-views`, session `understanding-views-inventory`, 2026-09-14
- **Not in scope:** spec, ADRs, UI, Core/App product writes, Atlas, allow-list rows, OverviewAsync-as-tree

Owner ruling (`docs/notes/understanding-views-owner-ruling.md`): admit D-0 only; Atlas paths forbidden; N1 must observe `artifact_path_id` assignment and how unindexed folders are known **without the App reading workspace files** (DC-022). Validation (`:146`): if that grain cannot list artifacts by project and folder, **or** cannot distinguish unindexed folders without the App walking the workspace → stop before N5.

§A5 D-0 admitted-when (`docs/specs/addendum-c-perspectives.md:314-320`): list code/data/architecture artifacts **by project and folder** with a kind glyph; activate → Architecture graph or source; a folder the index has not covered is shown **unindexed**.

## 1. Architecture surface kinds and Perspectives columns — **Verified**

Source: `src/AiDe.App/Workbench/SurfaceContentFactory.cs` `Kinds` (`:135-273`). Allow-list is the `Perspectives` column (`:88-94`). Explore admits no docked kind (`:93`).

| Kind | Title | Perspectives | Instances | Default Architecture? |
|---|---|---|---|---|
| `canvas` | Graph | Architecture only (`:139-143`) | One | Yes — Left (`ZoneLayout.cs:259`) |
| `view` | Evidence | Architecture only (`:151-154`) | One | No — admitted, not default (`ZoneLayout.cs:241-244`) |
| `classdiagram` | Class diagram | Architecture only (`:156-159`) | Many | Yes — Center as "Domain" (`ZoneLayout.cs:263`) |
| `sequence` | Sequence diagram | Architecture only (`:161-164`) | Many | No |
| `contexts` | Contexts | Architecture only (`:166-169`) | One | Yes — Center active (`ZoneLayout.cs:262`) |
| `joins` | Joins | Architecture only (`:173-176`) | One | No — admitted, pending real-content check (`ZoneLayout.cs:251-255`) |
| `codeviewer` | Code viewer | **Coding and Architecture** (`:240-243`) | Many | No |

Not Architecture (same list): `terminal`, `sessions`, `board`, `leaderboard`, `ledger`, `daydreams`, `prompt`, `search` (explicitly kept out of Architecture, `:232`), `diagnostics` (Coding only, `:245-248`), `session-document`, `console`.

Retired: `inspector` (`:289-295`).

Architecture default layout (`src/AiDe.Core/Workbench/ZoneLayout.cs:188-276`): Left = `canvas`; Center = `contexts` + `classdiagram`; Right empty collapsed; Bottom empty collapsed. No tree kind. Perspective row (`Perspectives.cs:72-75`): Architecture is a `DockHost`, landing Center.

## 2. Tree type / ExplorerSurface — **Verified**

`SolutionTree` / `WorkspaceTree`: **absent** from `src/` (grep: no type names; only addendum/ruling prose). Matches §A5 `:315-316` and Owner `:55`.

`ExplorerSurface` **exists** (`src/AiDe.App/Workbench/ExplorerSurface.cs:32-38`): full-window Explore graph+reader (`:18-24`), not a solution tree, not Architecture. Hosted when `PerspectiveSet.Explore` (`MainWindow.xaml.cs:88`, `PerspectiveShell.cs:110`). Explore body is `FullWindow` (`Perspectives.cs:68-70`). **No perspective hosts a project/folder tree.**

## 3. `IWorkspaceQueries` — Overview/Graph wrong grain for D-0 — **Verified** (Owner confirmed)

Interface `src/AiDe.Core/Projections/IWorkspaceQueries.cs:20-105`:

| Method | Role |
|---|---|
| `DescribeAsync` | Neighbourhood of **one** node (`:22`) |
| `ImpactAsync` | Impact of one node (`:24`) |
| `FindAsync` | Term search over nodes (`:26`) |
| `SearchContentAsync` | Lines in workspace files; **App must not read files** (`:28-37`, DC-022) |
| `InteractionAsync` | Ordered calls for sequence (`:39-47`) |
| `KnowledgeAsync` | Knowledge projection (`:49`) |
| `NodeContentAsync` | Content of **one** selected node; App does not read files (`:51-61`) |
| `EvidenceAsync` | Paged assertions (`:63-71`) |
| `GraphAsync` | Workspace as **nodes/edges**, filter then cap (`:73-84`) |
| `PathsAsync` | Routes between two nodes (`:86-94`) |
| `OverviewAsync` | Workspace as **identifier-prefix clusters** (`:96-104`) |

No tree method. Owner `:55` holds.

**Why OverviewAsync is wrong grain — Verified:** `GraphOverview.cs:26-34` (`WorkspaceOverview` = clusters + cluster edges + Depth + omitted clusters); `:77-80` groups by identifier prefix (C# dots / path segments), not project/folder; `:54-60` `MaxClusters` drops groups. `GroupFor` (`:196-207`) splits node **ids**, not `artifact_path_id`. No unindexed state.

**Why GraphAsync is wrong grain — Verified:** `GraphProjection.cs:148-154` returns `GraphNode`/`GraphEdge` with `Omitted` count and disclosures. Filter is kinds/scope/group/external/knowledge (`:110-117`). Group drill-down is overview-cluster id (`:50-51`, `:66-70`), still identifier prefixes. Caps omit nodes; they do not mark folders unindexed. `GraphNode` has Id/Label/Kind/Degree — no path (`:35-36`).

## 4. `artifact_path_id` — **Verified**

### Type and store grain

- Column: `TEXT NOT NULL` on `evidence_assertion_fact` (`WorkspaceSchema.cs:103-118`).
- Assertion grain (`EvidenceAssertion.cs:32-44`; schema `:102`): **one row is exactly one assertion** by one extractor about one (subject, predicate, object) at one artifact revision.
- **`artifact_path_id` is provenance on that assertion, not an artifact identity.** `Provenance.ArtifactPathId` (`EvidenceAssertion.cs:25-30`). Many assertions share one path. There is **no `artifact_dim`** (`WorkspaceSchema.cs:41-128`, Owner `:103`). Empty path is stored and later filtered (`StoreReader.cs:163`, `:249`).

**One `artifact_path_id` value is not exactly one file.** It is the extractor-chosen origin string for **that assertion**. Treating it as a folder tree key is an unstated split (Owner `:146` forbids inventing folder nodes that way).

### Assignment sites (write path)

Written by extractors into `Provenance`, then `StoreWriter.cs:89-97` (`assertion.Provenance.ArtifactPathId`).

| Extractor | What is stored | Workspace-relative file? |
|---|---|---|
| C# declaration | `Path.GetRelativePath(csprojDir, sourceFile)` with `/` (`CSharpExtractor.cs:479-493`) | **Relative to the project directory**, not the workspace root. Combined later with `declared_at` (`ProjectionService.cs:1207`). Fallback: `Path.GetFileName(projectPath)` (`:487`). |
| C# disclosures | project **filename** only (`:110-111`) | No folder. |
| Bicep | `Path.GetFileName(path)` (`BicepExtractor.cs:104-119`) | Filename only. |
| SQL | `Path.GetRelativePath(directory, file)` (`SqlSchemaExtractor.cs:175`, `:219-220`) | Relative to the **sql scope directory**. |
| EF schema | `state.RelativePath(directory)` (`EfSchemaExtractor.cs:190-191`, `:265-266`) | Relative to schema directory. |
| Knowledge documents | relative to knowledge scope dir (`KnowledgeExtractor.cs:245`, `:260-261`) | Relative to that directory. Knowledge **scope** facts use `Path.GetFileName(directory)` (`:199-200`). |
| Fixture | workspace-relative (`FixtureExtractor.cs:121`, `:176-179`) | Yes, vs fixture root. |
| Python | **`request.ScopeId`**, `SourceLocation` null (`PythonExtractor.cs:399-404`) | **Not a file path.** Scope ids are `python:<rel-dir>` (`CSharpScopeDiscovery.cs:104-107`). |
| TypeScript | **`request.ScopeId`** (`TypeScriptExtractor.cs:780-785`) | **Not a file path.** Ids `typescript:<rel-dir>` (`:110-114`). |

Core also writes `declared_at` for the **scope directory** as workspace-relative (`WorkspaceCore.cs:307-328`: subject = scopeId, predicate `declared_at`, object = relative dir or `""` for root). `StoreReader.ScopeLocation` (`:274-285`) reads that. **That is a scope location, not a folder census.**

### Folders vs files

- Schema has no folder table.
- Closest listing: `StoreReader.FilesToSearch` (`:229-264`) — **distinct indexed files** with a `has_type`/`declared_in` assertion and non-empty path; one representative node per `(scope_id, artifact_path_id)`. Remarks (`:233-236`): **extractor-chosen files, not the directory tree**; walking the tree would open skipped dirs.
- Python/TS assertions never put a file path in `artifact_path_id`, so this query cannot list those files by folder.
- C# paths can be split into folders **only by inventing nodes** from `/` segments. Owner forbids that without a stated grain.

## 5. Unindexed folders — **Verified** (no existing folder census); **Inferred** (what a new Core query would need)

No fact, disclosure, or Architecture surface names **folders the index did not cover**:

- Disclosures are extractor **classes** (languages, boundaries, gaps) — `DisclosureKinds.cs:49-80` (`go-not-analysed` style, `python-standard-library-not-indexed`, etc.). None is a folder path.
- `UnanalysedLanguages.Survey` (`:62-83`) walks disk **in Core**, counts files by extension, emits `"go-not-analysed (N file(s))"` — language totals, **not folders**. Skip list (`:48-52`) **omits** `bin`/`node_modules`/… so those folders are never named.
- `CSharpScopeDiscovery.Skip` (`:31-32`) same shape: skipped dirs vanish from scope discovery.
- `scope_snapshot_committed_fact` grain is **one complete snapshot per scope generation** (`WorkspaceSchema.cs:88-100`: `assertion_count`, `complete`) — not folders inside a scope.
- Diagnostics (`DiagnosticsSurface.cs:22-25`; kind Coding-only `SurfaceContentFactory.cs:245-248`) folds disclosure **classes** (`DisclosureSummary.Fold`), not unindexed folders. Not on Architecture default (`ZoneLayout.cs:270-271`).

**§A5 unindexed without the App walking disk:** DC-022 / `IWorkspaceQueries.cs:32-35`, `:55-59` put file access on Core. Existing **store grain cannot** distinguish “folder exists, not indexed” from “folder does not exist”: absence of `artifact_path_id` prefixes is not a folder. A Core query would need a **workspace census on the daemon side** (same authority as `SearchContentAsync` / `UnanalysedLanguages.Enumerate` / index discovery) and must degrade to “not recorded”, never a fake folder (Owner `:106`). That census **is not stored today**.

## 6. Activate/reveal path — **Verified**

Owner compute reader (`:109`): activate → `GraphAsync` / `DescribeAsync` **or** `NodeContentAsync` / admitted `codeviewer`. No third path. No Atlas.

**`DescribeAsync` → `DescribeResult`** (`ProjectionService.cs:73-81`): `NodeView` + neighbour `EdgeView`s + `ResultBounds` + `SourceRevision` + optional `NeighborKinds` / `Members` / `KnowledgeIds`. Neighbourhood cap, not a tree.

**`GraphAsync` → `WorkspaceGraph`** (`GraphProjection.cs:148-154`): nodes, edges, omitted count, disclosures, revision, `DeclaredByKind`. Architecture canvas already asks `ExcludeKnowledge` (`:90-108`).

**`NodeContentAsync` → `NodeContent`** (`NodeContent.cs:52-57`; `ProjectionService.cs:1126-1182`): `NodeId`, `RenderKind` (Code/Text/None/Html), `Language`, `Content` (bounded 256 KiB), `Shortfall`. Resolves via declaring assertion’s scope + `artifact_path_id` (`:1139-1147`, `:1197-1221`). Client does not read files.

IPC already registers `describe`, `graph`, `nodeContent` (`WorkspaceOperations.cs:132-155`, `:198-216`).

## 7. IPC — new query needs a new operation id — **Verified**

- Envelope: `IpcRequest.Operation` (`src/AiDe.Core/Ipc/IpcContract.cs:133-140`). `IpcContract.cs` is **not** the operation catalog.
- Catalog + handlers: `src/AiDe.Core/Ipc/WorkspaceOperations.cs` (`Describe = "describe"`, … `Overview = "overview"`, `:132-158`; `Register` `:174+`).
- Client: `WorkspaceClient` implements `IWorkspaceQueries` (`WorkspaceClient.cs:39`, `:83-182`) by sending those operation strings.
- Daemon: `WorkspaceOperations.Register(endpoint, core.Projections)` (`AiDe.Daemon/Program.cs:184`).
- Unknown `Operation` has no handler (`DaemonEndpoint.Invoke`).

A new `IWorkspaceQueries` method therefore needs: a new `WorkspaceOperations` const, request record, `Register` arm, `WorkspaceClient` method, and `LocalWorkspaceQueries` adapter. Do not reuse `overview` / `graph`.

## 8. STOP-BEFORE-N5 — **yes** — **Verified**

Owner `:146`: stop if `artifact_path_id` / scope facts **cannot** list artifacts by project and folder, **or** cannot distinguish unindexed folders without the App reading the workspace.

| Requirement | Evidence | Holds? |
|---|---|---|
| List artifacts by **project** | Scopes exist (`csharp:name:tfm`, `python:rel`, `sql:rel`, … `CSharpScopeDiscovery.cs:4-8`, `:48-135`); `declared_at` places the scope dir. Indexed **files** for C#/SQL/EF/knowledge/fixture can be joined to a scope via `FilesToSearch`. | Partial — scope ≠ project-and-folder tree; C# project name is csproj stem, not path. |
| List by **folder** | No folder dimension. Paths are inconsistent (C# project-relative; Bicep filename; Python/TS = ScopeId). Folders would be invented by splitting strings. | **No** without unstated splits. |
| Unindexed folders without App disk walk | No folder facts. Disclosures are language/class counts. Skip lists hide dirs. | **No** from current grain. Core *could* census at query time; that is new substrate, not these facts. |

**STOP-BEFORE-N5: yes.** Specify/architecture must not treat `OverviewAsync` as the tree, must not split `artifact_path_id` into folder nodes without an Owner grain, and must not have the App walk the workspace (DC-022). Return to Owner before N5 (core-query) unless Owner admits a Core-side census (and a declared folder grain) as the unindexed substrate.

## What the tree query must read (one sentence)

A **new** `IWorkspaceQueries` method must read latest-generation `evidence_assertion_fact` (path + declaring predicates) joined to `declared_at` and `node_dim.node_kind` for **indexed files by scope**, and — because those facts cannot name unindexed folders — a **Core-confined workspace census** (not App I/O) that degrades to unindexed / not recorded.

## Residual (not invented here)

- Exact DTO, cap, glyph mapping — architecture + spike.
- Whether Python/TS must start emitting file-relative `artifact_path_id` before a honest file tree is possible for those languages.
- Toolkit for the tree control — N7.
