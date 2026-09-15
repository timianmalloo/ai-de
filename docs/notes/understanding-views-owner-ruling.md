---
id: note-understanding-views-owner-ruling
title: "Admit D-0 Solution/tree view this horizon; D-0 is not Atlas; D-1…D-6 stay deferred; join onto understanding-views"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, owner]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
  - { to: plan-understanding-views, rel: relates-to }
  - { to: coordination-understanding-views, rel: relates-to }
  - { to: adr-0036-compile-mode-ladder-deployment-gates, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Admit only D-0 (Solution/tree) this cycle as an Architecture pane over indexed artifacts.
  Blast radius: one kind, one allow-list column, one derived-menu pickup, join onto
  understanding-views — not Atlas, not main, not D-1…D-6.
---

# Admit D-0 Solution/tree view this horizon; D-0 is not Atlas; D-1…D-6 stay deferred; join onto understanding-views

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** per clause below (Verified | Inferred | Flagged)
- **Made during:** N0 Owner ruling of `plan-understanding-views`, session `understanding-views-owner`, 2026-09-14. Operator request to start the deferred understanding views, under Ruling 54 CONDITIONS.

## Evidence opened (not the Conductor's paraphrase)

- `docs/specs/addendum-c-perspectives.md` §A5 (`:293-331`) — admission criteria and AR3 never-scaffold.
- `docs/notes/addendum-c-council-rulings.md` Ruling 54 (`:175-203`) and Ruling 59 (`:383-413`).
- `docs/adr/0030-perspective-registry-and-allow-lists.md` Decision 2–3 (`:73-95`).
- `docs/adr/0036-compile-mode-ladder-deployment-gates.md` Decision 3 (`:187-198`).
- `docs/plans/understanding-views.md`; `docs/coordination/understanding-views.md`.
- `src/AiDe.Core/Projections/IWorkspaceQueries.cs` (`:20-105`); `src/AiDe.Core/Store/WorkspaceSchema.cs` (`:41-128`); `src/AiDe.Core/Projections/GraphOverview.cs` (`:1-88`).
- Directory listing of `src/AiDe.Core/` — no `Understanding/` folder in this worktree.

## The call

The operator request is in. Ruling 54 CONDITIONS (`:199-200`): *"Revisit the deferral list only when a deferred view has a substrate query it can be built on; an operator request re-admits one at a time, not the set."* This ruling admits **one** view. It does not re-open the set.

### 1. Admit D-0 Solution/tree view — **Verified**

§A5 D-0 (`:314-320`):

> Also absent today and deferred the same way — **D-0 Solution/tree view** (UC3 model 1: "a solution/tree view of the code, data, architecture artifacts"). No tree type exists (`SolutionTree`/`WorkspaceTree` absent from `src/AiDe.App`, Ruling 54 BECAUSE **[Verified]**). *Admitted when:* Given a workspace, When the tree opens, Then code, data and architecture artifacts are listed by project and folder with a kind glyph, And activating an item reveals it in the Architecture graph or opens its source, And a folder the index has not covered is shown with an "unindexed" state.

Ruling 59 (`:389`, `:397-406`) confirmed D-0 as a fifth named-and-deferred view and: *"D-0 admitted to the allow-list only in the slice that builds it (`:320-321`)."*

Substrate that exists today (opened): `node_dim.node_kind`, `evidence_assertion_fact.artifact_path_id`, scope snapshot facts, extractors, `IWorkspaceQueries` (`GraphAsync`, `DescribeAsync`, `NodeContentAsync`, `OverviewAsync`). Substrate that does **not** exist: a tree method on `IWorkspaceQueries` (`:20-105`); a `SolutionTree`/`WorkspaceTree` type. `OverviewAsync` is the wrong grain — identifier-prefix clusters with `MaxClusters` omission (`GraphOverview.cs:26-65`, `:77-80`), not a project/folder listing with an unindexed state.

D-0 is UC3 item (1). It is the navigator later views activate into. It is the one view this horizon may specify, architect, spike, design, implement, and join.

### 2. Atlas seam — D-0 must not touch Atlas / Understanding source — **Verified** (absence in this tree); **Flagged** (Atlas `session-contracts` not present here)

D-0 is an Architecture-pane tree over **indexed artifacts**. It is not Code Atlas.

**Forbidden paths for this programme:** `src/AiDe.Core/Understanding/**`; Atlas App/spike files. This worktree's `src/AiDe.Core/` has no `Understanding/` directory (listing observed). If a later node thinks it needs Atlas, the Conductor files `coord request add`. Nobody here edits Atlas files, claims Atlas paths, or removes Atlas worktrees.

`core-query` may add **one** bounded read under `src/AiDe.Core/Projections/**` + IPC + tests named by the design. That is not Atlas.

### 3. D-5 and D-6 — keep-deferred this horizon; no code track — **Verified**

§A5 D-5 (`:323-328`): *Admitted when* the fake-deriver oracle holds **and** *"an eval harness scores the real deriver on a fixture corpus … before it ships behind the seam."* ADR-0036 Decision 3 (`:187-198`): *"The eval harness ships with the first compile slice and before any agentic rung is selectable"* and *"The fake-deriver oracle of Addendum C D-5 is the seam's admission test at the composer."* D-5 is the composer aggregate, not an Architecture kind. Blocker confirmed: this horizon does not host that harness.

§A5 D-6 (`:329-331`): *Admitted when:* *"a reply-channel seam exists and a fixture reply carrying a drafted `goal-block` template renders as derived lines with no form widget."* No such seam is in this programme. D-6 stays Addendum B `:187` until that seam exists.

Do not implement D-5 or D-6. Do not add composer kinds. Do not treat ADR-0036 work as in-scope except as this named blocker.

### 4. D-1…D-4 stay named-and-deferred; no allow-list rows — **Verified**

§A5 (`:321-322`): *"Each deferred view is admitted to the Architecture allow-list, and to its derived menu, only in the slice that builds it (AR3) — never scaffolded ahead."*

ADR-0030 Decision 2–3 (`:73-87`): the allow-list is a `Perspectives` column on the App `SurfaceKind` row; *"an empty set fails the build test"*; menu/rail/palette/routing are **derived from the join** — *"never a second hand-written list"* (ADR title/summary `:22-25`; Ruling 55b).

Keep D-1…D-4 in the spec with their §A5 admitted-when paragraphs intact. Do not add their kinds. Do not hand-write menu entries. They remain eligible at N14, one at a time, only when a substrate query exists.

### 5. Join target is `understanding-views`, not `main` — **Verified** (grant)

`conductor-join.py` onto `understanding-views`. `main` stays with the Claude conductor until a later Owner ruling grants integration. Derived files regenerate at join; they are not merged by hand (`plan-understanding-views` F-JOIN).

### 6. N14 next-view rule — **Verified** (adopted from the plan's loop contract)

- One view in flight. The next view does not start until this one has joined, or until Owner writes a stop.
- After N13, Owner runs N14. Nobody else admits a view.
- **Variant:** count of §A5 views this ruling has not disposed (`admit` | `keep-deferred` | `strike`). Must strictly decrease each N14 cycle.
- This ruling disposes: D-0 = admit; D-5 = keep-deferred this horizon; D-6 = keep-deferred this horizon. **Undisposed:** D-1, D-2, D-3, D-4.
- Floor 0. Exit: Owner writes "no further admission this horizon", or every remaining view is disposed.
- Cap 7. A firing cap is a **defect signal**, not permission to continue.
- If D-0's Proof Pack cannot meet the quoted admitted-when, **do not join**; return to Owner. Do not thin §A5 to get a green join.

### 7. E7 surface list for D-0 (amended) — grain **Verified**; query shape **Inferred** until N1

Written before design. Floors not removed: Testing Strategy union, this list, red-first, audit entries, AR3, Ruling 54 one-at-a-time, ADR-0030 derived menu.

| Surface | Ruling |
|---|---|
| **store** | Existing SQLite facts: `node_dim`, `evidence_assertion_fact.artifact_path_id`, scope snapshot facts (`WorkspaceSchema.cs:53-128`). No second graph store (Ruling 53). Live schema v1 has no `artifact_dim` table (`CreateSql` observed). |
| **model** | One tree node is a projection of one indexed artifact **or** one unindexed folder. Grain is declared in `/specify` (DM: "one row is exactly one ______"). Not a `GraphCluster`. |
| **service** | Expect **one new bounded query** on `IWorkspaceQueries`. `OverviewAsync`/`GraphAsync` are not that query. N1 opens `artifact_path_id` assignment and how unindexed folders are observed **without the App reading workspace files** (DC-022; `IWorkspaceQueries.cs:32-35`, `:55-59`). |
| **projection/wire** | IPC, bounded, honest shortfall. Unindexed is a required §A5 state, never silent omission, never a plausible empty tree. Degrade to "not recorded" / unindexed, never a fake folder. |
| **client type** | A tree in Architecture's docking host. Toolkit is N7 Spike Protocol — do not freeze WPF `TreeView` vs anything else in this note. |
| **UI** | Architecture pane. Hard states: empty, loading, unindexed, error, no-workspace. |
| **compute reader** | Activate → existing Architecture graph neighbourhood (`GraphAsync` / `DescribeAsync`) **or** open source (`NodeContentAsync` / admitted `codeviewer`). No third reveal path. No Atlas types. |

Walking skeleton after spec+architecture: Core query (if N1 confirms the new method) → Shell surface → **one** `SurfaceKind` row allow-list membership for D-0. Derived menu picks it up. No second hand-written list.

## Alternatives dismissed

- **Seven-way parallel D-0…D-6.** Ruling 54 one-at-a-time; shared allow-list is an exclusive resource (GO5); AR3 scaffolding. Illegal, not slower.
- **Atlas-as-D-0** (author `Understanding/**` or treat Code Atlas as the solution tree). D-0's admitted-when is indexed code/data/architecture artifacts in the Architecture host. Atlas is a different programme, live fleet, different conductor. Using it would smuggle Addendum E into this horizon.
- **Admit D-5 now** because a compile slice might already exist. §A5 D-5 needs the eval harness **and** the fake-deriver oracle; ADR-0036 places that harness on the agentic compile ladder. Even if files exist under `tools/compile-eval/`, D-5 is still a second view and a different aggregate. One at a time; this cycle is D-0.
- **Admit D-3 (ER) first** because `spec-uml-erm-surfaces` exists. UC3 lists the solution/tree first; D-3's admitted-when is US-U3 against a fixture schema, not a navigator. Smallest correct is D-0.
- **Reuse `OverviewAsync` as the tree.** Wrong grain (clusters, caps, no unindexed folders). Would fail §A5 while looking like progress.
- **Join to `main`.** Owner has not granted integration. Claude conductor owns `main` until a later ruling.
- **Scaffold D-1…D-4 allow-list or menu rows "so the spine is ready".** AR3; ADR-0030 mutation test exists to punish the second list.

## What the Conductor may do next

1. File this note at `docs/notes/understanding-views-owner-ruling.md`, audit-log start/append for session `understanding-views-owner`, claim/release on that path.
2. **N1 ∥ N2** (read-only): inventory Architecture kinds, `IWorkspaceQueries`, Explorer-vs-tree gap, `artifact_path_id` grain; named comparables for a solution/tree. No spec authoring in those tracks.
3. Then **N3 `/specify` for D-0 only**. D-1…D-6 remain §A5 deferred in that spec.
4. Then the serial spine in `docs/coordination/understanding-views.md`: architecture + ADR for the one kind → spike → ui-design → design-slice → core-query → shell-surface (one allow-list column) → Proof Pack → `conductor-join.py` onto `understanding-views`.
5. Then N14 back here.

## What the Conductor must not do

- Specify, design, or implement more than D-0.
- Add allow-list or menu rows for D-1…D-6.
- Author `src/AiDe.Core/Understanding/**`, Atlas App/spike files, or `session-contracts.md`.
- Join to `main`.
- Open a D-5/D-6 code track, a Tests perspective, or a new graph capability in Explore.
- Run `coord install` or `coord regen` from a worktree (coordination plan layer state).
- Remove Atlas worktrees or claim Atlas paths.
- Drop or thin: Testing Strategy union, E7, red-first, audit entries, AR3, Ruling 54 one-at-a-time, ADR-0030 derived menu.
- Treat `OverviewAsync` as D-0's query without a new Owner ruling.
- Start N14 before N13, or start a second view because a harness reminder arrived.

## Validation condition

Holds until N1 reports that `artifact_path_id` / scope facts **cannot** list artifacts by project and folder, or cannot distinguish unindexed folders without the App reading the workspace. On that report: stop the spine before N5, return to Owner, do not invent folder nodes from unstated path splits.

N14 may keep-defer the remaining views this horizon; it may not admit a second view until D-0 has joined (or Owner has refused the join).

## Promotion rule

If the architecture track needs a durable kind-admission record, it writes **one** ADR for D-0's `SurfaceKind` row and links `supersedes` nothing on this note. This note stays the origin of the horizon call. Do not fork a second allow-list rule; ADR-0030 already is that rule.

## Residual (not ruled)

- Exact tree-query name, DTO, and cap — architecture + spike.
- Toolkit for the tree control — N7.
- Whether D-1’s entry-point classification already exists as predicates — N14 evidence, not this cycle.
- Compile-eval harness presence on disk — irrelevant to this horizon’s code tracks.
- Atlas `session-contracts` §2 text — not in this worktree; seam still holds by path ban above.
