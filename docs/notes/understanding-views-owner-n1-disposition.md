---
id: note-understanding-views-owner-n1-disposition
title: "Admit a query-time Core census as D-0 unindexed substrate; skip-list omitted; Python/TS file grain disclosed not rewritten; specify may proceed"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, owner, D-0]
links:
  - { to: note-understanding-views-n1-inventory, rel: depends-on }
  - { to: note-understanding-views-owner-ruling, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-understanding-views-n2-comparables, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  N0 STOP-BEFORE-N5 stands on current facts; D-0 still admitted. Unindexed folders come from a
  query-time Core census (not stored folder facts, not App I/O). Blast radius: specify + later
  one Core query; no extractor provenance rewrite; no Atlas; no main; no D-1…D-6.
---

# Admit a query-time Core census as D-0 unindexed substrate; skip-list omitted; Python/TS file grain disclosed not rewritten; specify may proceed

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** per clause below (Verified | Inferred | Flagged)
- **Made during:** Owner disposition of N1 STOP-BEFORE-N5, session `understanding-views-owner`, 2026-09-14. Predecessor: `note-understanding-views-owner-ruling` validation condition (`:144-146`).

## Evidence opened (not the Conductor's paraphrase)

- `docs/notes/understanding-views-n1-inventory.md` (STOP-BEFORE-N5 `:151-165`).
- `docs/notes/understanding-views-owner-ruling.md` (`:144-146`, E7 `:100-109`).
- `docs/notes/understanding-views-n2-comparables.md` (Rider no-index vs VS Code hide `:46-53`).
- `docs/specs/addendum-c-perspectives.md` §A5 D-0 (`:314-320`).
- `src/AiDe.Core/Store/StoreReader.cs` `FilesToSearch` (`:229-236`, `:242-263`).
- `src/AiDe.Core/Extraction/PythonExtractor.cs` (`:399-404`); `TypeScriptExtractor.cs` (`:780-785`); `BicepExtractor.cs` (`:104-119`); `CSharpExtractor.cs` (`:479-493`).
- `src/AiDe.Core/Extraction/UnanalysedLanguages.cs` Skip + Enumerate (`:48-52`, `:62-89`); `CSharpScopeDiscovery.cs` Skip (`:26-32`).
- `src/AiDe.Core/Projections/IWorkspaceQueries.cs` DC-022 App-must-not-read (`:32-35`, `:55-59`).

## The call

N1’s STOP-BEFORE-N5 is **confirmed**. Existing `artifact_path_id` / scope facts cannot list by project and folder, and cannot distinguish unindexed folders without the App walking disk. N0 forbade inventing folder nodes from path splits. §A5 unindexed is not thinned. D-0 stays the one admitted view.

This note **admits the missing substrate**, reshaped.

### 1. Census — reshape and admit — **Verified**

Admit a **Core-confined, query-time workspace census** as the unindexed substrate, on the same side of DC-022 as `SearchContentAsync` / `UnanalysedLanguages.Enumerate`. The App must not walk disk.

| Decision | Rule |
|---|---|
| **When** | Query time (tree open), **not** stored folder facts / `folder_dim` / census tables this horizon. Disk-now is derived (DM7). `UnanalysedLanguages` already walks at query time. |
| **What it emits** | Directories Core observed under the workspace root. Folder **identity** comes from this census. Do **not** mint folders by splitting `artifact_path_id`. |
| **Indexed files** | Latest-generation assertions joined onto census paths (C# project-relative paths join via `declared_at` / scope location — `StoreReader.ScopeLocation` `:266-285`). Unresolvable paths are **not-recorded**, not guessed folders. |
| **Skip-list directories** (`bin`, `node_modules`, …) | **Omitted** under **one** existing Core skip policy. They are not unindexed (the index is not supposed to cover them) and not not-recorded (the skip is known). Named in the spec so this is not VS Code `files.exclude`. Census **consumes one existing skip set**; it must not copy a fourth `HashSet`. If `CSharpScopeDiscovery.Skip` (`:31-32`), `UnanalysedLanguages.Skip` (`:48-52`), and extractor skips still disagree, architecture unifies them as part of this slice (the `artifacts` disagreement is already DC-022 in `CSharpScopeDiscovery.cs:26-30`). |
| **Unindexed** | A census directory **not** on the skip list, with **zero** indexed artifacts joinable under it. Rider “no index” (`note-understanding-views-n2-comparables` `:39`, `:46`). |
| **Not-recorded** | Census shortfall: IO, permission, or cap. Never a fake folder. Never a complete-looking empty tree. Degrade to not-recorded (IO1). |
| **Bound** | Payload capped; omitted-by-cap is disclosed as not-recorded / omitted count, never silent. |

N1’s proposed “new `IWorkspaceQueries` method over latest assertions **plus** Core census” is admitted **in this shape**. `OverviewAsync` / `GraphAsync` remain the wrong grain (N0 `:55`; N1 `:76-80`).

### 2. Specify (N3) may proceed now — **Verified** (grant)

`/specify` for **D-0 only** may run. It must declare this grain, not the two-clause draft:

> One tree node is exactly one Core-named workspace-relative path: **either** one indexed artifact (a file/document resolved from latest assertions) **or** one census folder (a directory Core observed). Skip-listed directories are not nodes. A census folder’s coverage is `indexed-parent` \| `unindexed` \| `not-recorded`.

Do not specify “one node = indexed artifact OR unindexed folder” only — that erases indexed parent folders and forces illegal path-splits to nest files.

Hard states stay: empty, loading, unindexed, error, no-workspace, **plus** not-recorded shortfall. Activate still `GraphAsync`/`DescribeAsync` or `NodeContentAsync`/`codeviewer` (N0 `:109`). D-1…D-6 remain §A5 deferred. AR3: no allow-list row until the slice that builds D-0.

N2 handoff stands: Rider for unindexed; do not copy VS Code hide; Architecture pane ≠ ADR-0017 Explorer; do not freeze the tree toolkit (N7).

### 3. Python / TypeScript `ScopeId`-as-path — cut extractor rewrite; honesty disclosure this horizon — **Verified**

`PythonExtractor.cs:399-404` and `TypeScriptExtractor.cs:780-785` store `request.ScopeId` as `Provenance.ArtifactPathId` with null source location — **not a file path**. Changing extractors to emit per-file paths is a provenance-grain job (store consumers, `NodeContentAsync` resolution, tests). **Cut** from this horizon.

Specify must state the shortfall so the tree cannot look complete:

- Python/TS **scopes** appear as indexed **folders** (the scope directory).
- Per-file `.py` / `.ts` artifacts are **not recorded** (no fake file rows from a second walk).
- A rendered-surface / query test asserts the disclosure is present when such scopes exist.

Bicep filename-only (`BicepExtractor.cs:104-119`) is the same class: resolve via scope location, never treat the filename as a folder; else not-recorded.

### 4. N5 architecture blocked until census admission — **this note is that admission** — **Verified**

N0: on STOP-BEFORE-N5, do not start N5 until Owner admits a census and a folder grain. **Admitted here.**

- **N5 stays serial after N3 → N4.** Specify does not unlock architecture in parallel.
- **N5 does not wait for another Owner round** unless specify cannot write failing Gherkin for unindexed / not-recorded / skip-omission, or the spike later forbids a toolkit.
- Architecture implements the census as ruled: one new `IWorkspaceQueries` method + IPC operation (N1 `:141-149`); no `overview`/`graph` reuse; no Atlas; no `Understanding/**`.

Join target remains `understanding-views`. `main` not granted. D-1…D-6 not admitted.

### E7 amendment (supersedes N0 model/service cells for D-0)

| Surface | Amendment |
|---|---|
| **store** | Unchanged: no folder table this horizon. Census is **not** stored. |
| **model** | Grain in clause 2. Folder identity = census, not path-split. |
| **service** | One new bounded query: latest indexed artifacts ⨝ census folders. Skip via one existing Core set. |

Floors unchanged: Testing Strategy union, E7, red-first, audit, AR3, Ruling 54 one-at-a-time, ADR-0030 derived menu, DC-022.

## Alternatives dismissed

- **Refuse census / drop unindexed.** Thins §A5. Forbidden.
- **Stored `folder_dim` / census facts this horizon.** Second definition of disk; stale vs tree-open; schema for one consumer. YAGNI; query-time already exists.
- **App-side directory walk.** DC-022; `IWorkspaceQueries.cs:32-35`.
- **Split `artifact_path_id` into folder nodes.** N0 `:146`; Python/TS/Bicep strings are not workspace folder keys (`:399-404`, `:780-785`, `:104-119`).
- **Show `bin`/`node_modules` as unindexed.** Floods the tree; contradicts extraction skip policy; confuses Rider no-index with IntelliJ excluded (N2 `:52-53`).
- **Hide skip-list dirs with no spec rule.** VS Code `files.exclude` — N2 `:47` fails §A5.
- **Rewrite Python/TS provenance now.** Second view-sized job; would steal the D-0 spine. Cut; disclose.
- **Start N5 in parallel with specify.** Decision edge: grain and skip rule must be in the spec first.
- **Admit D-1…D-6, Atlas, or `main`.** Unchanged refusals.

## What the Conductor may do next

1. File this note; audit-log append for session `understanding-views-owner`.
2. **N3 `/specify` for D-0 only**, using the grain and skip/unindexed/not-recorded rules above; N2 comparables as research input.
3. N4 spec review. Then N5 architecture implementing **this** census — not a new substrate.
4. Then the existing serial spine (spike → ui-design → design-slice → core-query → shell-surface → join `understanding-views`).

## What the Conductor must not do

- Author product source, Atlas/`Understanding/**`, or `session-contracts.md`.
- Add allow-list rows for unbuilt kinds; join `main`; admit D-1…D-6.
- Store a folder census; invent folders from `artifact_path_id`; walk disk from App.
- Copy a fourth skip `HashSet`.
- Change Python/TS/Bicep provenance to “complete” the tree.
- Treat `OverviewAsync` as the tree.
- Thin §A5 unindexed, red-first, Testing Strategy, or ADR-0030.
- `coord install` / `coord regen`.

## Validation condition

Holds until a measured query-time census cannot bound without silent omission (architecture returns **numbers**, not a stored-census proposal as a fait accompli). Holds unless specify cannot express skip-omission as a named rule distinct from unindexed. Python/TS file listing re-opens only as its own Owner-admitted extractor slice, not as D-0 creep.

## Promotion rule

Architecture writes **one** ADR for D-0 kind admission + the census query (or two if the kind and the query must split). This note remains the origin of the STOP-BEFORE-N5 disposition. Do not fork ADR-0030.

## Residual (not ruled)

- Query name, DTO, cap values, glyph mapping — N5 + N7.
- Which single skip set is the survivor — architecture, provided there is exactly one.
- Tree toolkit — N7.
- Whether a later horizon stores a census after measured bounds fail — not this note.
