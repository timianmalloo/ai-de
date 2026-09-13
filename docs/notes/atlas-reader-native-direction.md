---
id: note-atlas-reader-native-direction
title: "Code Atlas native reader - direction and state contract"
type: decision-note
status: proposed
owner: "@timianmalloo"
tags: [code-atlas, native, ui, accessibility]
links:
  - { to: spec-addendum-e-code-atlas, rel: refines }
  - { to: note-atlas-live-reader-horizon, rel: depends-on }
  - { to: proof-code-atlas-live-reader-candidate, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Applies the already approved file-first/altitude proposal and existing DESIGN.md to the
  detached native reader. It does not redesign the shared shell or import private mockup data.
---

# Direction before native controls

**Mode:** create the real native consumer from the approved proposal/mockup direction, not another
concept redesign. The earlier TheTerrace-backed proposal remains reference-only; no private corpus,
screenshots or fixture JSON enters delivery history.

**User:** a developer or architect arriving with an unfamiliar repository and limited attention.
**Job:** find an actual file, step into its actual declarations, inspect exact bound source, then
return without losing selection or mistaking changed bytes for the old observation.
**Adjectives:** dense rather than sparse; calm rather than decorative; traceable rather than opaque.
**References:** Visual Studio Solution Explorer's recognizable folder/file hierarchy; VS Code's
outline beside source and keyboard navigation; AI-DE's existing dense evidence-first pane styling.
Adapt these task conventions, not their branding.

**Archetype:** desktop-bound, read-only multi-panel workstation with hierarchical navigation and
master/detail selection, compact density, keyboard-first input, local explicit feedback and
view-local history. Reading is parallel; this is not a dashboard or data-entry wizard.

**Native medium:** Windows WPF, UI Automation, existing application resource/theme system;
candidate/debug distribution only. Signing/installer trust is not claimed by a detached test runner.
DESIGN.md remains authoritative and unchanged: UI and mono fonts, semantic brushes, focus, disabled
ink, 28px compact rows, restrained separators, Light/Dark/HighContrast behavior. No decorative
imagery, generated UI, new palette or private theme system.

## Trigger mapping

- UI-T1 applies: expert code/evidence surface, counts/bounds/coverage require units and honest
  known/unknown/withheld distinctions. No invented percentage.
- UI-T2 does not apply: no generated imagery/persona/motion.
- UI-T3 does not apply to this slice: deterministic query/source results, no model interaction.
- UI-T4 applies: WPF/native keyboard, UIA, theme, DPI and responsiveness need native evidence.

## Composition and behavior

Toolbar: Back plus concise scope/coverage/status, without a new global menu or sidebar.
Content: recognizable file hierarchy, addressable declaration outline, read-only source.
Use the existing approved source/code control and tokens where reusable. No source I/O, root
grant creation, provider access, compiler work or status calculation in the view.

Inventory pagination/partial state is visible; long names remain inspectable rather than clipped
into indistinguishability. A file's unsupported language/classification remains visible.
Selection uses the actual supplied FileValue/declaration observation key. Back stores only
Core-issued receipt plus local focus/scroll, and invokes RestoreAsync; it does not retain bodies
or invent a previous manifest. Late/canceled replies cannot overwrite newer selection.

## Required state and copy inventory

| State | Visible result / recovery |
|---|---|
| Loading | Stable pane structure, concise “Loading repository files…” or “Loading selection…”; no fake zero count |
| Empty | “No visible files in this scope.” Distinguish a real empty result from unavailable membership |
| Partial / bounded | Show returned rows and the supplied limit/reason; reachable Load more when applicable |
| Unknown / withheld | “Not recorded” / “Withheld” rather than zero or a fabricated percentage |
| FileLimited | “Project/TFM context not established.” Do not label this full-project analysis |
| Unsupported file | Keep file visible; show supplied classification/source limitation, no content request bypass |
| Source IndexedMatch | Exact supplied read-only page, decoder/binding context and valid supplied highlights |
| Changed / unavailable / refused / unstable | Clear old source/highlight; show typed outcome/reason and the permitted retry/selection path |
| Cancellation / late response | Honest canceled/pending state; retain the newer accepted selection |
| Back unavailable | State the supplied expired/retired receipt result; no silent latest substitution |
| Long/overflow content | Scrollable/virtualized panes, stable focus and accessible full labels |

Buttons, tree, outline and source need explicit accessible names and visible focus. Keyboard can
complete the file -> member -> source -> Back path. Text is selectable/copyable; it is not an editor.
No purposeful animation is needed; honor reduced motion without adding motion to prove it exists.

## Proof boundary

Injected query fixtures exercise hard states, late results, UIA and focus, but are not live product
evidence. The separate runner/producer/source/query join must later exercise the authorized real
AI-DE root read-only. Native accessibility cannot be cleared by the HTML mockup or a static scan.
The Conductor obtains independent UX/platform review and records remaining theme/DPI/signing limits.

## N proposal and mandatory rendered repair

Proposal `e0fdb531` returned two files and seven fixture tests without semantic red. The
Conductor/UX gate found that model children were not bound into a native hierarchical template,
directory rows became files, pagination could offer a non-existent next page, TooLarge was
mislabelled FileLimited, stale faults/focus were not inert, and tests hosted a Border rather than
the view. The earlier brief PASS is not code acceptance.

The replacement writer must first observe failures in a shown STA Window hosting
`AtlasReaderView`: realize/expand nested containers, reach a file by keyboard, leave directories
non-activating, inspect actual automation peers, and restore accepted Back selection/focus/scroll.
The view must observe event tasks and cancel on unload. History is 50 receipt/view-state frames,
never full source bodies. Keep each source outcome distinct and align source font size with the
existing token scale.

Known-total pagination uses returned offset plus rows. With unknown/withheld totals and no
explicit continuation in the frozen query contract, do not turn a generic limiting reason into
a promise of more data; show the limitation and withhold that action. Any needed contract
extension returns to the Conductor as an exact Q/N seam, not a private UI inference.
