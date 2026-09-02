---
id: ui-smoke-test-9-1
title: "Smoke-test 9-1 — UI review, contextual-viewer UX, and durable-fix plan"
type: doc
status: draft
owner: "@timianmalloo"
phase: "facelift"
tags: [ui, ux, review, graph, source-viewer, class-diagram, sequence-diagram, contexts, legibility, docking, terminal]
links:
  - { to: design-knowledge-explorer-mode, rel: relates-to }
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: inv-terminal-input-not-local-to-focus, rel: relates-to }
review-by: 2026-12-01
summary: >-
  Review of 15 smoke-test snapshots plus the Claude-Code terminal render complaint. Triages every
  issue to a root cause and disposition, records the two durable fixes landed now (the Source pane
  now follows graph selection, and shows no fabricated source with no workspace), and designs the
  IntelliJ-style contextual-viewer UX the user asked for — right-click a node to open the viewer
  appropriate to its type (source, class diagram, metadata), right-click a method for a sequence
  diagram — as a phased plan across the graph, the explorer, and the diagram surfaces.
---

# Smoke-test 9-1 — UI review, contextual-viewer UX, and durable-fix plan

> Grounding: the 15 snapshots in `smoke test 9-1/` (filenames are the issue titles), the Knowledge
> Explorer design, and the terminal-input investigation. The user ran the build from the
> `ai-de-session-phase3-pane-probes` worktree against `TheTerrace`.

## 1. Triage — every issue, root cause, disposition

Severity: **S3** blocks a core task · **S2** major friction · **S1** polish.

| # | Issue (from the snapshot title) | Category | Root cause (Verified V / Inferred I) | Disposition | Status |
|---|---|---|---|---|---|
| 1 | "select a node to view source but nothing updates in any source tab" | Source viewer | **V** — `BindCanvas` never routed `CanvasSurface.NodeSelected` into the code viewers; §4s left it as "Design's call" | **Fixed this run** — follow selection | ✅ landed |
| 2 | "source worked with no workspace open" | Source viewer | **V** — `PopulateCodeViewersAsync` showed the mock sample when `_queries is null` | **Fixed this run** — no fake source; honest empty state | ✅ landed |
| 3 | "code viewer opened but no source tab; focus moved graph→explore" | Source viewer + focus | **V** (source) / **V** (focus) — viewer opened after a selection was blank; opening a surface re-rendered and AvalonDock lost active-content tracking so focus snapped to the first zone | Source half fixed (A); **focus-steal fixed** — `Render()` preserves the active surface across the layout swap | ✅ landed (A + F) |
| 4 | "source may have been created but hidden in the container till I widened it" | Docking | **V** — `OpenPane`/`MovePane` never floored the destination zone's extent, so a pane into a shrunk/empty tool zone rendered as a sliver | **Fixed this run (F)** — `UsableExtentFor` floors an empty tool zone to `DefaultExtent` on open + drag-in | ✅ landed (F) |
| 5 | "right-click a node → open class diagram / view source / read md / metadata by type" | **Contextual UX** | **V** — no context menu exists on graph nodes | **Design below (§3)** + implement | ▢ planned |
| 6 | "scroll-pan-zoom in class diagram (mouse+trackpad); right-click a method → sequence diagram" | Class diagram | **V** — class diagram has scrollbars only, no pan/zoom; no method context menu | **Design below (§3)** + implement | ▢ planned |
| 7 | "contexts hard to read — and I should go context → class diagram / metadata / graph" | Legibility + nav | **V** — muted body text with compounding `Opacity` (0.6–0.85) on the card descriptions; and no navigation affordance off a context | Legibility fix (§4) + contextual nav (§3) | ▢ planned |
| 8 | "joins hard to read" | Legibility | **V** — same opacity-stacked muted text (`ContextMapSurface` edge members at `Opacity 0.6/0.8`) | Legibility fix (§4) | ▢ planned |
| 9 | "provenance hard to read" | Legibility | **I** — same muted-text pattern on the evidence/provenance surface | Legibility fix (§4) | ▢ planned |
| 10 | "dragged a source tab L→R but the graph moved; two source tabs there and one on left" | Docking | **V** — the native-drag reconcile (`TryMapByPosition`) assigned columns to zones by **raw index** (first→Left, last→Right), so when a drag inserted a column whichever column read first/last was mis-identified, scattering a real zone (explorers→Center, dragged pane→Left) | **Fixed this run** — content-**anchored** mapping: the column holding the majority of a zone's surfaces IS that zone; a split-off/dragged column is placed by its position relative to the Center anchor. 3 tests incl. the exact repro | ✅ landed |
| 11 | "closed one source tab; explorer took focus and both source tabs gone on the right" | Docking + focus | **V** — closing re-rendered and the rebuilt pane defaulted to its first document (AvalonDock resets selection on attach), hiding the surviving tab and defaulting focus to the first zone | **Fixed this run (F)** — `RestoreSelection` re-applies the model's active tab per pane; `RestoreActive` preserves focus | ✅ landed (F) |
| 12 | "view post workspace reload — TheTerrace" | Docking | **I** — layout after opening a workspace (prior complaint: opening should keep the arrangement) | **Investigate** — confirm restore-on-open holds for this path | ▢ planned |
| 13 | "graph on the correct side — where I wanted it" | Docking | positive confirmation | none | ✅ ok |
| 14 | "sequence diagram — no context" | Sequence diagram | **V** — the sequence surface is a scaffold with no real ordered-call feed (Core `Interaction.cs` just landed; not yet wired) | Wire the real feed (§3, depends on Core) | ✅ landed (E) |
| 15 | "what sessions are surfacing in this list" | Sessions | **V** — five near-identical `Terminal — pwsh · Not Recorded · Stale` rows; the list is unclear about what a "session" is and why these appear | **Fixed this run (H)** — legible two-line rows (identity above muted metadata) + colour+glyph liveness chip; a shared telemetry gap stated once, not per row | ✅ landed (H) |
| 16 | Claude-Code terminal: "moving the cursor paints characters without proper refresh" | Terminal render | **V** — the VtParser had no case for the in-place line-editing CSI finals **ECH (`X`), ICH (`@`), DCH (`P`)**; they hit `default: unknown finals dropped`, so a TUI's line rewrite left stale glyphs in the model that the full-repaint renderer then faithfully drew | **Fixed this run (G)** — implemented `EraseCharacters`/`InsertCharacters`/`DeleteCharacters` on `TerminalScreen` + wired `X`/`@`/`P`; 8 tests | ✅ landed (G) |

**Landed this run:** #1, #2, and the source half of #3.

## 2. The durable fixes landed now

**The Source pane follows graph selection (#1, #2, #3-source).** `BindCanvas` now subscribes to
`CanvasSurface.NodeSelected` (idempotent `-=`/`+=`, the join-endpoint idiom) and routes the selected
node's content — through the same `NodeContentSource` the whole app uses (real when a workspace is
open) — into every open code viewer. A viewer opened *after* a selection shows that node
(`_lastSelectedNodeId`), so "opened but blank" is gone. With no workspace, `PopulateCodeViewersAsync`
no longer fabricates a sample; the viewer stays in its honest "Select a node to read its source."
state. Tests: `CodeViewerFollowsSelectionTests` (selection populates the viewer; no-workspace is a
no-op). App 284 green; smoke passed.

## 3. The contextual-viewer UX (the IntelliJ model) — design

The user's core insight, and it is the right one: **a diagram is a *view* opened from an entry point
in the model, not a top-level thing you create blind.** IntelliJ never asks "make a class diagram"
in the abstract — you right-click a class and *show its diagram*, right-click a method and *show its
call/sequence*. The node is the noun; the viewer is the verb. Our current model inverts this: "New
class diagram" is a menu command that opens an empty surface with no entry point (#5, #14, "sequence
diagram — no context").

### 3.1 The user journeys (what we are designing for)

- **Explore → drill in.** The user explores the graph, finds a node, and wants to know more:
  its **source** (if code), its **metadata/edges**, its **class diagram** (if a type), or — from a
  method — its **sequence/activity**. Today selection updates the reader; there is no way to say
  "open *this* as *that*."
- **Diagram → drill deeper.** The user is in a class diagram, sees a method, and wants its **sequence
  diagram** or its **source** — without leaving for the graph and hunting the node again (#6).
- **Context/Join → the model.** The user is reading the Contexts or Joins surface and wants to jump
  to the **class diagram**, the **graph neighbourhood**, or the **metadata** of what they're reading
  (#7). Every analytical surface is a dead end today.

### 3.2 The design — one contextual "Open as…" grammar, everywhere

**A single `NodeViewMenu` built from the node's type, reused by every surface that shows a node.**
Right-clicking any node (in the graph, the explorer, a class-diagram box, a context card, a join row)
opens the same menu, filtered to the viewers that node *supports*:

| Node type | Contextual "Open as…" offers |
|---|---|
| class / interface / record / struct | **Source** · **Class diagram** (rooted here) · Metadata & edges · Graph neighbourhood |
| method / function | **Source** (the member) · **Sequence diagram** (from this call) · Metadata & edges |
| markdown / adr / design / spec / knowledge | **Read** (rendered md) · Metadata · Graph neighbourhood |
| table / column / schema | **Source** (DDL if available) · Metadata · Graph neighbourhood |
| azure-resource / bicep | Metadata · Source (the bicep) · Graph neighbourhood |
| any | Metadata & edges · Reveal in graph |

The menu is **type-driven, from the producer's signal** — `IsKnowledge`, the `has_type` kind — never
a spelling guess (the DC-042 lesson, just fixed for the Knowledge chip). An offer the node does not
support is absent, not disabled-and-confusing.

**Where the chosen view opens.** Reuse the existing dock model: an "Open as source/class-diagram/…"
action opens (or re-uses) the corresponding surface **in the pane the user is working in** — honoring
the earlier "add to where I have focus" complaint — and routes the node into it. The already-landed
selection→viewer plumbing (§2) is the substrate; this adds the explicit "open as" verbs on top of the
implicit "selection follows."

**The reciprocal within a diagram.** In the class diagram, a **method box** carries the same menu
(Source · Sequence diagram · Metadata); a **type header** carries (Source · Graph neighbourhood ·
Metadata). This closes journey 2 without a round-trip to the graph.

### 3.3 Class-diagram interaction (part of #6)

- **Pan/zoom** with mouse and trackpad: wheel = vertical scroll, Shift+wheel = horizontal,
  Ctrl/Cmd+wheel = zoom to cursor, middle-drag or space-drag = pan; keep the scrollbars as a fallback.
  A `ZoomableCanvas`-style transform on the diagram's root, not per-element.
- Right-click a method → **Open as sequence diagram** (§3.2).

### 3.4 Fidelity of the diagrams themselves

- **Class diagram** — the earlier UML-fidelity work stands (compartments, associations/aggregations).
  Continue toward full UML (visibility glyphs, static/abstract once Core emits them).
- **Sequence diagram (#14)** — the scaffold now has a real feed available: Core landed
  `Interaction.cs` (the ordered `(caller, callee, ordinal, kind)` model, §4k). Wiring
  `SequenceModel.Build` to it, entered from a method's "Open as sequence diagram", gives the sequence
  diagram the context it lacks.

## 4. Legibility fix (measured recommendation for #7, #8, #9)

**Root cause:** body text on the Contexts/Joins/Provenance surfaces is the muted token
(`TextMutedBrush #98A3B2`) **with a compounding `Opacity` of 0.6–0.85** stacked on top
(`ContextMapSurface.cs` edge members and "…more" lines; the card descriptions render similarly). Muted
× 0.6 opacity on the card background drops well below WCAG 2.2 AA (U16). Stacking opacity on an
already-muted token is the anti-pattern.

**Fix (a focused `/ui-design` slice, measured against the running surface):** stop stacking opacity on
muted body text — either render descriptions at full opacity in the muted token, or introduce a
distinct `TextBodyBrush` (lighter than muted, e.g. ~#C2CAD6) for card/edge body copy and reserve
`TextMutedBrush` for genuinely secondary labels. Verify each text/surface pairing at ≥4.5:1 in the
mockup harness before landing (DX11). Deliberately **not shipped blind this run** — it must be
measured on the rendered surface, not guessed at headless.

## 5a. Phase F investigation — docking drag / close / focus (#4, #10, #11, #12, #3-focus)

The zone model (ADR-0021) makes every operation **zone-confined** — `ZoneLayoutService` changes only
the source/destination zone, and `WorkbenchLayout.AssertInvariant` refuses a duplicated or lost
surface. So the frame *model* cannot flip. The remaining smoke complaints are at the **model↔render
boundary** (extent, re-render focus), not in the zone algebra:

| # | Root cause | Confidence | Disposition |
|---|---|---|---|
| **4** | `OpenPane`/`MovePane` set `Collapsed=false` on the destination but never floored the **extent** — a pane arriving into an empty tool zone that a prior resize/collapse had shrunk to the 8% minimum rendered as a sliver ("created but hidden till I widened it") | **V** (code path) | **Fixed this run** — `UsableExtentFor` floors an empty tool zone to `DefaultExtent` (22%) on open *and* on drag-in; 3 tests |
| **10** | The native-drag reconcile (`TryMapByPosition`) mapped columns to zones by **raw index** — `column[0]→Left`, `column[last]→Right` — and its guard only checked the surface *set*, not that each zone kept its content. So a drag that inserted a column at the front/back mis-identified a real zone: the explorers were pushed into the Center and the dragged pane took the Left identity (the reported "created in Bottom, dragged to top-right, landed top-left") | **V** (index-based mapping + set-only guard) | **Fixed this run** — content-**anchored**: each zone's anchor is the column holding the majority of its prior surfaces; a split-off/new column is placed by position relative to the Center anchor, so a right-drop lands Right (even when Right was empty) and a left-drop keeps the explorers in Left. 3 tests |
| **11** | Closing the last tab in a zone empties it; `Adapter.Render()` rebuilt `Manager.Layout` wholesale, and because AvalonDock resets a pane's selection on attach the rebuilt pane showed its **first** document — so the surviving tab was hidden and focus defaulted to the first zone (Explorer) | **V** (model→view desync) | **Fixed this run** — `RestoreSelection` applies each zone's model `ActiveIndex` to the realized pane after attach; the surviving tab is shown |
| **3-focus** | Same wholesale-swap: opening a surface re-rendered and AvalonDock's active-content tracking was lost, so focus snapped to the first document instead of staying where the user was | **V** (active-content lost on swap) | **Fixed this run** — `Render()` captures the pre-swap active surface and `RestoreActive` re-focuses it *when the model still agrees it is the active tab* (a user activation still wins); 2 adapter tests |
| **12** | Restore-on-open is exercised by `LayoutPersistence`; no defect reproduced in the model. The complaint reads as a consequence of #10/#11 during the same session, not a separate restore bug | **I** | **Confirm** after #10/#11 land; no separate fix identified. |

**Landed for F:** the #4 extent floor, the **#11/#3-focus focus-preservation**, and **#10** — the
native-drag reconcile now maps columns to zones by **content anchor** (the column holding the majority
of a zone's surfaces IS that zone) instead of raw index, so a drag can no longer scatter a real zone,
a right-drop lands Right (even when Right was empty), and a left-drop keeps the explorers in Left. 3
tests incl. the exact "Bottom→top-right→top-left" repro. **Residual:** the reconcile runs on the next
interaction/command (not instantly on drag-release); wiring a reconcile-on-drag-complete is a small
follow-up that needs functional verification. **#12** (restore-on-open) reads as a consequence of
#10/#11 and should be re-confirmed now those are fixed.

## 5. Sessions surface (#15)

Five rows all read `TheTerrace/workspace · Terminal — pwsh · Not Recorded · Not Recorded · ~ Stale ·
0 span(s) · Asserted`. The list does not answer "what is a session and why is this one here." Design:
give each session a **stable, human identity** (what launched it, when, its state) and distinguish
**alive / stale / ended** visually; an all-"Not Recorded" row is a telemetry gap the list should
state, not repeat five times.

## 6. Phased plan (priority order, for approval)

| Phase | Scope (code + tests) | Addresses | Owner | Depends on |
|---|---|---|---|---|
| **A ✅** | Source pane follows selection; no fake source | #1, #2, #3-source | App | landed |
| **B ✅** | Legibility — de-opacitied muted text; content (context descriptions, join basis) promoted to full `TextBrush`, counts stay muted (ContextMapSurface + JoinSurface) | #7, #8 | App/UX | landed (provenance is XAML — follow-up) |
| **C ✅** | `NodeViewMenu` — type-driven right-click "Open as source/class-diagram/sequence/metadata/reveal"; wired JS→CanvasSurface event→shell menu→actions | #5, #7-nav | App | landed (needs user functional verification) |
| **D ✅** | Class-diagram pan/zoom — wheel scrolls, Shift+wheel horizontal, Ctrl+wheel zoom-to-cursor, middle-drag pans; right-click a **type box** → `NodeViewMenu` "Open as…". Method-level right-click → sequence is unlocked by **E** | #6 | App | landed |
| **E ✅** | Wire `SequenceModel.Build` to Core `Interaction.cs` (`ShowNodeInSequenceDiagramsAsync` → `InteractionAsync` ordered feed → `SequenceModel.Build` → `ShowFor`); Sequence added to a **type's** `NodeViewMenu` options and routed via `OpenNodeView`; `BindSequenceDiagrams` re-fills open panes | #14, #6-seq | App + Core | landed (needs user functional verification of the render + method entry) |
| **F ◐** | Dock drag/close/focus (§5a). **#4** (empty-zone width floor), **#11/#3-focus** (active-tab + focus preserved across `Render()`), and **#10** (native-drag reconcile now content-**anchored**, not index-based — a drag can't scatter a zone; right-drop→Right, left-drop keeps explorers in Left) all fixed with tests. **#12** to re-confirm; a reconcile-on-drag-release refinement remains | #4, #11, #3-focus, #10 (fixed); #12 (confirm) | App+Core | landed the fixes |
| **G ✅** | Terminal render #16 root-caused: the VtParser **dropped** the in-place line-editing CSI finals (ECH `X`, ICH `@`, DCH `P`) a TUI redraws with, leaving stale glyphs the full-repaint renderer drew. Implemented the three ops on `TerminalScreen` + wired them; 8 tests. The renderer/model/pump were verified race-safe (full repaint under `SyncRoot`; `IsDirty=false` only after a full draw) — the fix was the missing parser cases, not coalescing | #16 | Core | landed |
| **H ✅** | Sessions surface #15: replaced the flat `·`-joined `DisplayLabel` with a legible **two-line row** — a stable identity (agent · repo/worktree) above muted metadata (harness · model · trust · spans) — and a **colour+glyph liveness chip** (Verified/Inferred/Unverified brush, glyph+text so it's never colour-alone). A telemetry gap the whole list shares is stated **once** (`SessionRowPresenter.SharedTelemetryNote`), not repeated per row. Pure presenter, 8 tests | #15 | App | landed |

**Recommended next:** **B** (legibility — highest visible-quality-per-effort, affects every
analytical surface) and **C** (the contextual menu — the centerpiece the user asked for), then **D/E**
(diagrams), with **F/G** as investigations.

## 7. Status

| | |
|---|---|
| **Completed** | Phases **A–H** landed. A/B/C/D (source-follows-selection; legibility; `NodeViewMenu` "Open as…"; class-diagram pan/zoom); **E** (sequence diagram → Core interaction feed); **F** (empty-zone width floor #4 fixed; #10/#11/#12/#3-focus root-caused + designed); **G** (terminal ECH/ICH/DCH #16 fixed); **H** (sessions surface #15 — legible chipped rows + shared-gap note) |
| **Remaining** | F's **#12** (re-confirm restore-on-open now #10/#11 are fixed) and a **reconcile-on-drag-release** refinement (the corrected mapping currently applies on the next interaction, not instantly on drop); #9 provenance legibility (**inspected: rows already use full `TextBrush`, not opacity — the inferred cause is absent; needs the actual snapshot**) |
| **Best next action** | Functionally confirm #10/#11/#3-focus in the running app (drag a pane between zones; open/close a tab); then decide whether the drag-release reconcile refinement is worth the risk |
| **Needs user functional verification** | The **rendered** sequence diagram (E), class-diagram pan/zoom + right-click menus (C/D), terminal glyph refresh in Claude Code (G), the sessions chip legibility (H), the **focus/tab preservation** (F #11/#3-focus), and the **drag-to-zone placement** (F #10) — all beyond headless testing |
