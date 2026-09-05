---
id: note-2026-08-30-overnight-surfaces
title: "Overnight run — three-surfaces progress & morning next-steps"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [overnight, class-diagram, code-viewer, prompt-editor, editor-surfaces, status]
links:
  - { to: spec-editor-surfaces, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: adr-0020-class-diagram-architecture, rel: relates-to }
  - { to: adr-0019-code-viewer-renderer, rel: relates-to }
review-by: 2026-11-30
review-suggested: []
summary: >-
  What the autonomous overnight run of 2026-08-30 delivered on the three requested new surfaces
  (class diagram, read-only code viewer, prompt editor) and what remains — mostly Core-gated. All
  increments landed green (App.Tests 171, Core.Tests 735, launch smoke); each is a clean vertical
  slice or a ready-to-substitute component behind a defined seam.
---

# Overnight run — three surfaces

## Landed (all green, clean on main)

| Increment | What shipped | Tests |
|---|---|---|
| **ADR-0019 code-viewer-renderer residual cleared** | Ran the AvalonEdit read-only PoC (spike, disposed): read-only + highlighting works, `TextEditor` is a pure WPF `Control` (no airspace by construction), C#/py/js/sql highlighted built-in, ts/bicep degrade to plain. RoslynPad fallback dropped. | (spike) |
| **ADR-0020 class-diagram-architecture + `ClassHierarchyModel`** | Class diagram = App-side type-hierarchy from the existing graph (`inherits`→generalization, `implements`→realization); members/Mermaid deferred to Phase 2 (Core `has_member`). Pure projection logic. | 8 |
| **Class-diagram surface (Phase 1)** | Native `ClassDiagramSurface` (no WebView2), reachable via **View menu / Ctrl+K,M**; styled type cards, member-less disclosure, external-relation count; grouped by context for scannability. | 2 |
| **Prompt-editor (Phase 2) — completed earlier this session, wired** | Reachable **Terminal menu / Ctrl+K,D**; staged draft, one-way named-session transfer, cross-restart persistence. | 7 |
| **Read-only code viewer component + seam** | `CodeViewerView` (AvalonEdit, read-only, highlight-by-language, shortfall, fallback) + `INodeContentSource` client seam mirroring ADR-0018 node-content-reader-contract + `MockNodeContentSource`. Staged & tested; **not yet wired into the live UI** — see below. | 6 |

## What remains, and why

- **Code viewer live content — Core-gated.** The viewer renders whatever `INodeContentSource` returns,
  but the App must not read files itself (DC-022), so real content needs Core's `NodeContentAsync`
  (ADR-0018 node-content-reader-contract, handed off §4c). The component + seam are staged so that wiring is a **one-line
  substitution** (swap `MockNodeContentSource` for the Core-backed source) plus a selection→viewer
  link. Deliberately not wired to mock content in the live UI overnight: a labelled-sample viewer is
  low value and the meaningful version needs selection plumbing + real content.
- **Class-diagram Phase 2 (members + notation-valid Mermaid) — Core-gated** on a `has_member`
  extractor enhancement (§4c). Phase 1 is member-less by design and says so.
- **Central Package Management — coordinated** (backlogged): adopting `Directory.Packages.props` edits
  every `.csproj` incl. Core-owned ones. Note: this session added `AvalonEdit Version="6.*"` directly
  to `AiDe.App.csproj`; when CPM lands, move it into `Directory.Packages.props`.
- **Graph LOD render** — soft-blocked on a Core `CanvasNode` count field.
- **Knowledge=0 / docs extractor (US-K1)** — Core (in progress: Core added `IsKnowledge` to the graph
  node this session).

## Morning next-steps (in priority order)

1. **Core:** ship `NodeContentAsync` → wire the code viewer (one-line source swap + selection link) —
   the third surface goes live.
2. **Core:** `has_member` extraction → class-diagram Phase 2 (members + Mermaid).
3. **Shared:** decide Central Package Management (coordinated).
4. **Design (unblocked):** a graphical class-diagram render (boxes + connectors) if the card list is
   not enough — a `/ui-design` pass; and wiring the viewer to follow graph selection once content exists.

## Core-boundary touches this session (FYI, recorded in §4c)

Two user commands added to the Core command catalog (`WorkbenchCommands.cs`) with matching menu +
tripwire-test count bumps, per the atomic-command-addition seam the `MainMenuBuilder` comment
documents: `workbench.newPromptDraft` (_Terminal) and `workbench.newClassDiagram` (_View). No
behaviour change to existing commands.
