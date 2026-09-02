---
id: ui-smoke-test-9-2
title: "Smoke test 9-2 — windowing gaps + terminal native integrity"
type: investigation
status: in-progress
owner: "@timianmalloo"
tags: [ui, ux, windowing, docking, terminal, conpty, legibility, sessions, review]
review-by: 2026-12-01
links:
  - { to: ui-smoke-test-9-1, rel: refines }
summary: >-
  Triage + verified root causes for the 9-2 smoke screenshots: dead arrow keys in the
  Claude Code session (DECCKM), a dark-on-dark provenance/list legibility bug, a docking
  move that relocates a second pane, focus-steal on opening a session, graph-canvas
  sizing, and missing Loomwatcher session registration. Carries the full terminal
  native-integrity plan (input encoding, alt screen, bracketed paste, mouse).
---

# Smoke test 9-2 — windowing gaps + terminal native integrity

The user captured 13 screenshots, each **named as the issue**. This is the triage, the
verified root causes, and the full phased plan for **terminal native integrity** (the
emphasized ask: *"a terminal has full native integrity and experience in terms of hot keys,
key strokes, typing and mouse/arrow-key fidelity"*) alongside the remaining windowing gaps.

Two of the three skills the user chained (`/ui-design` review, `/investigate`, `/visualize`)
converge on the same conclusion: **these are functional defects, not a styling deficit.**
Per `/visualize`'s own guidance (VA5 / the Simplifier), generated imagery would fix none of
them — arrow-key fidelity, docking, focus and legibility are code, not pictures. So the
`/visualize` step is deliberately a no-op here beyond confirming the direction; the value is
in `/investigate` → `/design` → `/implement`.

## 1. Triage — every screenshot, root cause, disposition

Severity: **S3** blocks a core task · **S2** major friction · **S1** polish.

| # | Screenshot (the issue, verbatim) | Area | Root cause (Verified V / Inferred I) | Sev | Status |
|---|---|---|---|---|---|
| 1 | "arrow keys dont work in claude code session so i cannot arrow down to choose forensic review" | Terminal input | **V** — the VtParser ignores **all** DEC private modes, so `ESC [ ? 1 h` (DECCKM) never registers; `TerminalInput.ForKey` then always sends **CSI** arrows (`ESC [ A`) where a full-screen TUI in application mode expects **SS3** (`ESC O A`). Also, special keys were handled on `OnKeyDown` (bubbling), so WPF directional focus could consume an arrow first | S3 | ✅ **fixed (Slice 1)** |
| 2 | "…after opening claud session focus in panes moves to seq diagrams and the explore becomes the selected tab in left view — none of those should have happened" | Docking + focus | **I** — opening a terminal/session triggers a full `Render()`; the Phase-F `RestoreSelection`/`RestoreActive` fix covered *reference documents* but the **session/terminal open path** (`NewAgentTerminalRequested` → open in Bottom) does not preserve the pre-open active surface, so focus lands on the first document and a re-render re-seats each pane's tab | S2 | ▢ planned (T-W1) |
| 3 | "window system still goofy i moved joins from right to left and contexts now moved from left to right in the same move operation" | Docking | **V** — the native-drag reconcile fell back to kind-based `TreeToZones.Convert` when position-mapping was unsure, and Convert re-seats **every** stack by kind, so moving `joins` re-classified `contexts` too | S2 | ✅ **fixed (T-W2)** |
| 4 | "after opening theterrace — black font on dark background hard to read" · "contexts and provenance side by side to show white text vs dark text" · "text for the contexts easy to read … unlike provenance" | Legibility | **V** — the honest-read surfaces (provenance/evidence, board, leaderboard, search) render their rows in a **ListBox** whose item text falls back to the default `ListBoxItem` foreground (a dark system colour), *not* the window's `TextBrush`. Contexts is legible because `ContextMapSurface` builds its own `TextBrush` TextBlocks (Phase B) | S2 | ✅ **fixed (Slice 2)** |
| 5 | "i think graph canvas should expand to use all the space… as i expand the space graph window stays the same" | Graph sizing | **I** — the canvas host (WebView2/HwndHost) is not stretching to fill its pane; a fixed size or a container that doesn't propagate available size | S2 | ▢ planned (T-W3) |
| 6 | "right-click on a node should give me a view source eg. metadata viewer class viewer etc" | Graph UX | **I** — `NodeViewMenu` (Phase C) exists and is wired to the canvas `contextmenu` message; either the running build predates it or the JS→shell path isn't firing for the user. Needs functional confirmation | S2 | ▢ confirm (T-W4) |
| 7 | "i started claude code… message in status still the same… so no explicit registration with loomwatcher" · "i have a terminal and a claude code session running but see the output from sessions" · "when the claude code terminal is opened its just a terminal — see the bottom pane and status message" | Sessions/telemetry | **I** — an agent terminal starts a ConPTY but does not register a Loomwatcher session (no binding emitted), so the Sessions surface and the status line never reflect it. Likely a Core (`WatcherObservationStore` / session registration) concern coordinated with the Core session | S2 | ▢ planned (T-W5, Core-coord) |
| — | "before any workspace or anything open" · "graph looks good in default view" | Baseline | positive/neutral confirmations | — | ✅ ok |

## 2. Fixes landed this run

**Slice 1 — Terminal arrow keys (DECCKM), #1.** The `TerminalScreen` now tracks
`ApplicationCursorKeys`; `VtParser` acts on `ESC [ ? 1 h/l` (a `DispatchPrivateMode` seam that
later slices extend to alt-screen/bracketed-paste/mouse); `TerminalInput.ForKey` encodes the
cursor keys as **SS3** in application mode and **CSI** otherwise; and `TerminalView` moved
special-key handling to **`OnPreviewKeyDown`** so WPF focus navigation cannot eat an arrow or
Tab before it reaches ConPTY. +10 tests (4 Core parser, 6 App input).

**Slice 2 — List legibility (#4).** A global `ListBoxItem` style in `App.xaml` sets the item
foreground to `TextBrush`, fixing provenance, board, leaderboard, search and the command
palette in one place (the class fix — every honest-read ListBox was dark-on-dark). The default
selection trigger still swaps to the highlight text colour when a row is selected.

## 3. The full plan — terminal native integrity (T-T*) + windowing (T-W*)

### Terminal native integrity (the emphasized ask)

| Phase | Scope | Fidelity it delivers | Owner |
|---|---|---|---|
| **T-T1 ✅** | DECCKM application cursor keys + `OnPreviewKeyDown` capture | Arrow/Home/End work in TUIs; keys aren't stolen by WPF nav | App+Core |
| **T-T2 ✅** | More keys: **F1–F12** (SS3/tilde), **Shift+Tab** (`ESC [ Z` back-tab), **Ctrl/Shift+arrows** (modified CSI `ESC [ 1 ; mod X` for word-nav and selection) | Full hotkey/keystroke fidelity | App |
| **T-T3 ✅** | **Bracketed paste** (`ESC [ ? 2004 h`): parser tracks the mode; `TerminalInput.ForPaste` wraps pasted text in `ESC [ 200~ … ESC [ 201~` when on (CRLF→CR normalized); Ctrl+V / Shift+Insert intercepted in the view as paste | Multi-line paste into Claude Code without each line executing | App+Core |
| **T-T4** | **Alternate screen buffer** (`?1049`/`?47`/`?1047`) + **cursor visibility** (`?25`): a second grid the TUI draws on, restored on exit | Full-screen TUIs (Claude Code menus, vim, less) render correctly and restore the shell on exit — the "it's just a terminal / see the output" confusion | Core+App |
| **T-T5** | **Mouse tracking** (`?1000` click, `?1002` drag, `?1003` any-motion, `?1006` SGR): mouse events → `ESC [ < b;x;y M/m`; wire WPF mouse down/up/move/wheel to encode when a mode is on | Mouse fidelity — clicking/scrolling inside a TUI | App+Core |
| **T-T6** | **DECKPAM** application keypad, **DA/DSR** device-status replies (`ESC [ c`, `ESC [ 6 n`), **DECSTBM** scroll region + IL/DL | Programs that probe the terminal or scroll a region behave | Core |

### Windowing (T-W*)

| Phase | Scope | Addresses |
|---|---|---|
| **T-W1 ✅** | Preserve/route focus on a **session/terminal open**: `Adapter.ActivateInView` focuses the newly-opened terminal (you open it to type in it), so focus lands on the session rather than snapping to seq-diagrams or leaving a stale selection | #2 |
| **T-W2 ✅** | Native-drag reconcile no longer falls back to the destructive kind-reclassify: `ReconcileFromView` maps by **position only** and **reverts an unmappable drag** (dragged pane snaps back) instead of re-seating bystander zones. Kind conversion is now persistence-only | #3 |
| **T-W3 ✅** | Graph canvas **fills its pane**: a `ResizeObserver` on the stage re-frames the settled layout (`fit`+`place`) when the pane grows — cheap (view transform only, no re-layout) | #5 |
| **T-W4** | Confirm/repair the **node right-click** viewer menu end-to-end | #6 |
| **T-W5** | **Loomwatcher session registration** for agent terminals (coordinate with Core) so the Sessions surface + status reflect a running Claude Code session | #7 |

## 4. Status

| | |
|---|---|
| **Completed** | T-T1 (arrow keys / DECCKM + preview-key capture) and Slice 2 (list legibility) — landed with tests |
| **Remaining** | T-T2…T-T6 (terminal fidelity), T-W1…T-W5 (windowing/focus/sizing/sessions) |
| **Best next action** | T-T2 (function keys + modified/meta keys) or T-W3 (graph canvas fill — a clean, high-visibility win) |
| **Needs user functional verification** | Arrow keys in Claude Code (T-T1), provenance legibility (Slice 2), and the felt docking/focus behaviour — all beyond headless testing |
