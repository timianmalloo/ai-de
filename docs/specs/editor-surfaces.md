---
id: spec-editor-surfaces
title: "Editor & Content Surfaces — read-only code viewer & prompt drafts (spec)"
type: spec
status: draft
owner: "@timianmalloo"
phase: ""
tags: [editor, monaco, avalonedit, markdig, code-viewer, prompt-draft, content-rendering, read-only]
links:
  - { to: spec-ai-native-ide, rel: refines }
  - { to: spec-knowledge-explorer-mode, rel: relates-to }
  - { to: spec-terminal-sessions, rel: relates-to }
  - { to: kb-editor-and-content-rendering-surfaces, rel: implements }
  - { to: adr-0018-node-content-reader-contract, rel: depends-on }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Specifies two content surfaces the workbench still lacks: a READ-ONLY code viewer (syntax-highlighted
  source for a selected node/file, never an editor of record) and a PROMPT-DRAFT editor (rich-text
  prompts staged until an explicit transfer to a ready terminal session). Both are read/compose
  surfaces, not a general-purpose code editor (explicitly out of scope in spec-ai-native-ide). The
  code viewer is the render side of the ADR-0018 NodeContentAsync seam; the prompt draft composes with
  the terminal-sessions surface. Grounds the reuse decision (Monaco MIT via WebView2, AvalonEdit MIT
  native, Markdig BSD-2) and names the Design/Core ownership lanes so the surfaces can be built in
  parallel against defined contracts.
---

# Editor & Content Surfaces

- **Tier:** T1 (read/compose surfaces; the read-only invariant, the transfer-safety of a prompt draft,
  and the a11y floor are load-bearing). Above T0 for the correctness/a11y vetoes.
- **Grounding path:** `spec-editor-surfaces → spec-ai-native-ide → knowledge-hub`; evidence from
  `kb-editor-and-content-rendering-surfaces` (comparables, licences); composes with
  `spec-knowledge-explorer-mode` (the reader is the content seam) and `spec-terminal-sessions` (the
  prompt-draft transfer target). Depends on **ADR-0018** (the bounded `NodeContentAsync` contract).

## Part A — Functional (what & why)

**Problem.** The workbench renders a graph and a node *metadata* reader, but it cannot yet show a
node's **source** (you can see that `Order` calls `Customer`, not read `Order.cs`), and it has no
place to **draft a prompt** before sending it to an agent. `spec-ai-native-ide` already scopes both:
it puts *"rich-text prompt drafts that are staged until an explicit transfer to a ready session"* **in
scope**, and it puts *"replacing a general-purpose source-code editor or owning code editing as a
primary workflow"* **out of scope**. This spec fills the two gaps **within** those bounds: a
**read-only** code viewer and a **prompt-draft** editor. Neither is an editor of record for source.

**Two surfaces, two jobs.**

1. **Read-only code viewer (US-ED1–ED4).** Show the syntax-highlighted **source** of a selected node
   or file, read-only, with the analysis the graph already knows (which symbols it declares, who it
   calls) overlaid or linked. It is the *content* half of the Explorer reader (ADR-0018): the reader
   shows metadata + edges today; the viewer renders the code when the node's `RenderKind` is `code`.

2. **Prompt-draft editor (US-ED5–ED8).** Compose a prompt in a dockable pane, keep it **staged**
   (persisted with the layout, not sent), and **transfer** it — on an explicit action — into a chosen
   **ready** terminal/agent session (the `spec-terminal-sessions` surface). Drafting never sends;
   transfer is the only path to a session, and it names its target.

**Core scenario (the one that must work end-to-end).** A user clicks a class node in the graph → the
reader shows its metadata and edges → the user opens *View source* → the **read-only viewer** renders
`Order.cs` with C# highlighting, scrolled to the symbol, and its `calls`/`declares` edges are
navigable back into the graph. Separately, the user drafts a prompt referencing that class, and
transfers it into the "copilot" terminal session — the draft survives a layout save and is only sent
when they press *Transfer*.

**User stories (Gherkin acceptance criteria — falsifiable).**

- **US-ED1 — Read-only by construction.** `Given the code viewer shows a node's source, Then there is
  no edit affordance and no keystroke mutates the content or the file on disk; And the surface
  announces itself as read-only to assistive tech.`
- **US-ED2 — Syntax highlighting per language.** `Given a node whose content RenderKind is 'code' with
  a language, Then the source is highlighted for that language; And an unknown language degrades to
  plain monospaced text, never to an error.`
- **US-ED3 — Bounded, honest content.** `Given a node whose source exceeds the transport bound
  (ADR-0018), Then the viewer shows the first N and an explicit "first N of M — open the file for the
  rest" shortfall, never a truncated frame presented as complete and never an oversized IPC frame.`
- **US-ED4 — Anchored and linked.** `Given a node with a source location, Then the viewer scrolls to
  the symbol; And the node's typed edges remain navigable back into the graph (walk), so the viewer is
  part of the exploration cycle, not a dead end.`
- **US-ED5 — Draft, don't send.** `Given a prompt draft, Then editing it never sends it anywhere; And
  the draft is persisted with the workbench layout (survives save/restore and app restart).`
- **US-ED6 — Explicit transfer to a NAMED ready session.** `Given a draft and at least one ready
  terminal session, When the user transfers, Then they choose the target session by name and the draft
  is delivered to exactly that session's input; And with no ready session the transfer action is
  disabled with a stated reason (never a silent no-op).`
- **US-ED7 — Transfer is one-way and auditable.** `Given a transfer, Then the draft content is
  recorded as an audit prompt (the audit mandate) against the target session; And a transfer is not
  reversible from the draft (the session owns it thereafter).`
- **US-ED8 — Both surfaces have real empty/loading/error states.** `Given the viewer with no selection,
  Then it shows an explicit empty state; Given content still loading, a skeleton; Given a fetch
  failure, an error with a retry — never a blank or a spinner-forever.`

### Non-goals (explicit)

- **Not** a general-purpose source editor: no editing, saving, refactoring, or IntelliSense-driven
  authoring of source (out of scope in `spec-ai-native-ide`). The viewer is read-only, full stop.
- **Not** a second file-content authority: the viewer renders what the **Core** content query returns
  (ADR-0018), it does not read files itself (two authorities disagree — DC-022).
- **Not** a prompt *runner*: the draft editor stages and transfers; the **session** runs it.
- **Not** rich-diff or merge (a later spec if wanted).

### Non-functional (ISO/IEC 25010, applicable)

- **Compatibility / reuse:** prefer an established permissive-licence viewer over hand-rolling
  highlighting (see Part C reuse table). **Security:** rendered source is *data*, never executed; a
  WebView2-hosted viewer treats content as untrusted text (no script execution from source).
  **Performance:** first paint of a bounded node's source within the interaction budget; virtualise
  long files. **Accessibility:** WCAG 2.2 AA — keyboard-navigable, screen-reader-labelled, not
  colour-alone for syntax (tokens also differ by weight/style where load-bearing).

## Part B — UX (how it works)

**Information architecture.** Both are **surfaces** in the existing dock model (US-9), navigated by
tabs, dockable/floatable, persisted with the layout — no new shell concept. The code viewer is the
natural **content expansion of the Explorer reader**: the reader's *View source* opens it (in the
Explorer's reader pane, or a docked viewer tab in the workbench). The prompt draft is a sibling dock
surface with a **transfer** control bound to the live set of ready sessions.

**Flows (happy + unhappy).**
- *View source:* select node → reader → *View source* → viewer renders (loading → content | shortfall
  | error+retry) → walk an edge → back to graph. Unreachable/no-source node → *View source* absent or
  disabled with a reason.
- *Draft & transfer:* new prompt draft → type (staged, autosaved to layout) → *Transfer* → pick a
  named ready session → delivered + audited. No ready session → *Transfer* disabled, tooltip states
  why ("start or select a terminal session first").

**Reuse-first (the collected evidence, `kb-editor-and-content-rendering-surfaces`).** The viewer is a
*reuse* decision, not a build: **Monaco Editor** (MIT, WebView2, VS-Code-parity highlighting, native
read-only + decorations) or **AvalonEdit** (MIT, native, no airspace) for code; **Markdig** (BSD-2) +
`Markdig.Wpf` for native markdown, or Markdown→HTML→WebView2 for rich content (Mermaid/charts). The
`/define-architecture` decision (renderer choice) is deferred to a spike, but the spec fixes the
*constraint*: permissive licence, read-only, airspace-aware.

## Part C — UI (how it looks)

- **Archetype:** the viewer composes the existing technical/reading archetype (dense, legible,
  monospaced, line-numbered); the draft editor is a focused compose surface. Both honour the facelift
  tokens (no arbitrary values), the complete state set (US-ED8), real copy, WCAG 2.2 AA, and the
  performance budget — per `ui-interaction-design.md` (U1–U20). Full visual design is produced at
  `/ui-design` time; this spec fixes intent and acceptance criteria, not pixels.
- **Consistency:** the viewer's syntax palette and the graph's category palette should not fight;
  the draft editor's *Transfer* is a consequential action (names its target, is auditable) and is
  styled as such (U-consequential-action).

## Delivery phasing (vertical slices — set at /define-architecture, sketched here)

- **Phase 1 (viewer, walking skeleton):** render Core's `NodeContentAsync` code content read-only in
  the Explorer reader when `RenderKind = code`, with highlighting, the shortfall state, and the walk
  edges — this is **ADR-0018 Phase 2** for the code kind. Real Core query; Design renders.
- **Phase 2 (prompt draft):** a dockable prompt-draft surface, persisted with the layout, with
  *Transfer to a named ready session* wired to `spec-terminal-sessions` and audited. Largely
  Design/App; the session-input seam already exists.
- **Phase 3 (polish):** markdown/rich content kinds in the viewer (Markdig / HTML-in-WebView2),
  anchor-to-symbol, and cross-surface consistency with the graph.

## Ownership lanes (so these can be built in parallel against contracts)

| Surface | Design/App owns | Core owns (the seam) |
|---|---|---|
| Read-only code viewer | the viewer control (Monaco/AvalonEdit host), highlighting, states, walk wiring | `NodeContentAsync` returning `RenderKind=code` + language + bounded content + `Shortfall` (ADR-0018) — **already handed off** |
| Prompt-draft editor | the draft surface, persistence-with-layout, the *Transfer* action + audit | the session-input seam (`spec-terminal-sessions`, exists) — no new Core work expected |
| Class diagram (separate spec) | the diagram render surface | the class-hierarchy projection query over the code graph (a new Core query, like `OverviewAsync`) |

## Acceptance gate (definition of done for the spec)

- [ ] Three layers present (Functional/UX; UI intent stated, full visual deferred to `/ui-design`) — S1.
- [ ] Read-only invariant (US-ED1) and single-content-authority (non-goal) stated as testable criteria.
- [ ] The `NodeContentAsync` dependency (ADR-0018) is named, not re-invented.
- [ ] The prompt-draft transfer is explicit, named, one-way, and audited (US-ED6/ED7).
- [ ] Reuse constraint recorded (permissive licence; renderer choice deferred to a spike).
- [ ] Ownership lanes defined so parallel build consumes contracts, not races.
