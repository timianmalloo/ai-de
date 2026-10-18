---
id: adr-0043-notebook-editor-codemirror-canonical-source
title: "ADR-0043 — The notebook editor is the existing CodeMirror 6 composer over one canonical Markdown source; rich view is a projection; deterministic assistance is T0 from Core, model suggestions a separate T3 layer"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [architecture, editor, codemirror, webview2, intellisense, notebook, workbench-loops]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: kb-workbench-loops-20261018, rel: depends-on }
  - { to: adr-0008-shell-host, rel: refines }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: refines }
  - { to: adr-0034-envelope-event-store, rel: depends-on }
  - { to: adr-0039-workbench-records-durable-representation, rel: depends-on }
  - { to: adr-0025-code-viewer-renderer, rel: relates-to }
review-by: 2027-04-18
summary: >-
  Both journeys author in the already-shipped CodeMirror 6 / WebView2 composer, extended into a
  continuous Markdown notebook whose only authoritative document is the source text; the rich
  projection is decoration, never a second document. Completion, hover and diagnostics come from
  Core's bounded queries (T0); model rewrites are an optional, previewed T3 layer. The page-to-host
  message protocol grows additively and the page still cannot send. A qualification spike on IME,
  clipboard, multi-selection, undo and 100k characters is the reversal trigger toward a
  schema-governed engine.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# ADR-0043: The notebook editor — CodeMirror 6 over one canonical source

- **Status:** Accepted — owner ruling 2026-10-18 (@timianmalloo); council record follows. Previously Proposed (council gate 2026-10-18)
- **Date:** 2026-10-18
- **Deciders (Peer Mode):** Tech Lead (author), Enterprise Architect, UX & Accessibility
  (consulted), Domain Researcher (editor evidence)
- **Context spec/architecture:** spec US-E01–E04, UX-04/05, UI-02, NFR-02 (editor p95 < 50 ms at
  100k characters), NFR-04 (paired CLI evaluation), AC-E01c (lossless path); research note
  §Editor architecture evidence; defect classes UI-REFERENCE-IDENTITY, UI-DOCUMENT-PROJECTION,
  UI-COMPLETION-ARIA.

## Context

The composer is already a CodeMirror 6 editor hosted in WebView2 (`ComposerSurface.cs:49`;
bundle `Web/vendor/codemirror-composer.bundle.mjs`, sha pinned in `vendor-manifest.json`;
Markdown support in the bundle source — all Verified 2026-10-18). Its page-to-host protocol is
deliberately tiny: five kinds (`editor.ready`, `draft.changed`, `focus.leave`, `attach.offered`,
`metrics`; `ComposerVocabulary.cs:20-39`, `ProtocolVersion = 1`), a refused-names list that includes
`send`, `send.requested` and `run.start` enforced by a test (`ComposerVocabulary.cs:50`), and one
host-to-page message (`host.init`). Prepare and Send are WPF-side (`ComposerSendGate`), so the page
cannot start a run — the "renderer message cannot become an uncontrolled execution path" rule the
proposal restates. The compile step's lease and projection derive from the editor's `source_text`
only (Ruling 66; ADR-0033/0034).

The spec requires one continuous keyboard-editable document with rich and source projections,
structural navigation, search, folding, `@`/`/`/Ctrl+Space completion over indexed files, symbols,
requirements, skills and variables, hover/signature help that shows the *actual* identity or an
explicit non-resolution, multi-selection edits with preview/apply/one-undo, a revision stack with
branch/compare, and an exact request preview. The mockup's contenteditable is "a bounded
interaction model", not evidence that a rich engine is complete. Two real engine paths exist
(research note): CodeMirror with decorations/widgets, or a schema-governed ProseMirror-class
document embedding code editors, whose integration cost is Inferred.

LOA principles: P1 cheapest sufficient tier (deterministic completion first), P2 determinism at
the floor, P5 verification over plausibility (a model suggestion is previewed, never applied).

## Decision

1. **One authoritative document: the Markdown source text.** The notebook revision's `body`
   (ADR-0039) *is* the CodeMirror document. The rich projection is CodeMirror decorations, widgets
   and folding over that text; switching projections, navigating away or reopening never rewrites
   the text (defect class UI-DOCUMENT-PROJECTION). An unsupported structure or paste is disclosed
   before any lossy conversion, and the source projection is always the lossless path (AC-E01c).
2. **Reuse the shipped composer honestly.** What is reused: the WebView2 hosting, the CSP and
   virtual asset host, the pinned vendor bundle, `ComposerSendGate` (Prepare/Send stay WPF-side),
   the message router and the refused-names list. What is **new**: the page bundle (a continuous
   Markdown document instead of the field/template composer — `Fields`, `SetFieldText`,
   `TemplateCards` are retired; a template becomes a saved revision), the draft store's shape,
   and protocol v2. WL-1a needs **zero** new message kinds (v1 suffices for save/prepare from a
   revision). WL-2 adds the minimum: page→host **one** discriminated `query.request {kind:
   completion | hover, offset, prefix}` — offsets into the host-held revision, never a `path`/`uri`
   (`ForbiddenBodyMembers` stays intact) — and host→page `query.result`, `diagnostics` (push) and
   `edits.apply {preview: bool}` (preview and apply share one payload; one-undo is CodeMirror's).
   No `diagnostics.ack`, no separate selection report ("run selection" is requested host→page at
   Prepare, and the host clamps any page-reported range to the revision it holds). Completion
   queries are byte- and rate-bounded with `Dropped` counted; after an `edits.apply` push the host
   verifies the page's next `draft.changed` sha equals the expected post-edit sha. The refused list
   is unchanged: the page never sends, prepares, runs, or names a route; every added kind carries
   an exact-key test and a refused-names review.
3. **Deterministic assistance is T0 and comes from Core.** Completion sources map to existing
   bounded queries: files/symbols → `IWorkspaceQueries.Find` / `SearchContent` / `Describe`
   (`Projections/IWorkspaceQueries.cs:20`); requirements and knowledge → `Knowledge`; skills and
   templates → `TemplateCatalog` (`Sessions/`); prompt variables → the session's settings model. Each
   result carries identity, source revision and freshness; a duplicate name is shown as ambiguous
   with both identities; an unindexed reference is an explicit **unresolved** item — never the first
   match (defect class UI-REFERENCE-IDENTITY). Results are byte-bounded like every Core query.
4. **Model suggestions are a separate T3 layer** — rewrites, next-edit, critique — rendered with a
   distinct origin mark, applied only through preview → accept, carrying their own latency/usage
   disclosure, and routed through the same governed call composition as the compile call
   (ADR-0035: a pinned session with `tools: []`), never through `GovernedRunHost`'s run root.
5. **History is the revision fact.** Save / submit / branch append `notebook_revision_fact`;
   scratch edits live in the draft store until saved; compare renders a diff between two immutable
   revisions; the prompt stack is a read of revisions, not a second history.
6. **Prepare = compile envelope.** "Run document / section / selection" opens an envelope whose
   `source_text` is the selected range of the current revision; the exact serialized request,
   resolved references, scope snapshot, route and limits are the envelope projection shown before
   Send (ADR-0033/0034); its `projection_sha` binds the attempt (ADR-0041).
7. **Qualification gate before default.** The editor becomes the default authoring surface only
   after the native spike passes: IME composition, clipboard round-trip, multi-selection
   edit/undo, language/whitespace round-trip, 100k-character input p95 < 50 ms, UIA name/role for
   the completion popup with no dangling active option (UI-COMPLETION-ARIA). **Reversal trigger:**
   if document semantics (nested structures, embedded code editors) cannot be met cleanly, a new
   ADR adopts a schema-governed engine; until then no second authoritative document is introduced.

## Alternatives considered

- **Schema-governed rich document (ProseMirror-class) now:** rejected for now — it is a second
  document model with Inferred IME/clipboard/undo cost and would duplicate the shipped, pinned,
  CSP-hosted composer; kept as the named reversal path.
- **Native WPF rich-text editor:** rejected — the repo's own ADR-0025 analysis (read-only viewer →
  AvalonEdit) does not transfer: a writable Markdown notebook with completion widgets and
  decorations is what CodeMirror exists for, and the composer already pays the WebView2 cost once.
- **Letting the page call Core directly (a JS bridge to queries):** rejected — the page is inert
  content under CSP; every query goes host-side through the message router so results are
  bounded, logged and never a renderer-initiated execution path.
- **Model-ranked completion by default:** rejected by the existing capability gate (architecture
  §Capability-tier allocation): a reorder-only ranker only after a measured need, never selecting
  what is dropped.

## Consequences

- **Positive:** one editor for both journeys; the "superior to CLI" benchmark has a concrete
  surface; lease derivation, compile and attempt binding all read the same bytes.
- **Negative / accepted trade-offs:** CodeMirror decorations for a notebook feel are real work; the
  page protocol's growth needs the same exactness tests the five kinds have today; a WebView2 pane
  carries the known focus/airspace obligations (ADR-0015) into every perspective that admits it.
- **Follow-ups / new risks:** the qualification spike is the first slice's exit evidence; the
  vendor bundle grows and its sha pin and licence closure (`LICENSES-codemirror.txt`) are re-verified
  at each rebuild.

## Evidence

- `ComposerSurface.cs:49, 1643-1765`, `ComposerVocabulary.cs:20-50`, `ComposerMessageRouter.cs:62-71`,
  `ComposerPageContract.cs:20-50`, `IWorkspaceQueries.cs:20` — **Verified** 2026-10-18.
- CodeMirror decoration/selection primitives — **Verified in source** (research note citations);
  not yet exercised as a notebook in this repo — **Flagged** until the qualification spike.
- NFR-02 editor budget — **Flagged** (unmeasured).
