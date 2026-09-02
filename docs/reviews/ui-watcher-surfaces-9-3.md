---
id: ui-review-watcher-surfaces-9-3
title: "UI review — watcher surfaces (Sessions / Board / Leaderboard / Ledger) + graph-in-narrow-pane"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "watcher"
tags: [ui-design, review, watcher, sessions, empty-states]
links:
  - { to: defect-classes, rel: relates-to }
  - { to: note-20260902-session-enlistment-telemetry-gap, rel: relates-to }
review-by: 2027-03-01
summary: >-
  /ui-design review + elevate driven by the 2026-09-02 14:55 screen recording. The Sessions surface
  is a wall of undifferentiated ended terminals that buries the live collaboration; the watcher
  empty states are a single muted line in a vast pane; the graph is unusable in a narrow column.
  The elevate lands the Sessions fix (live-first + collapsed history + teaching empty state).
---

# UI review — watcher surfaces + graph-in-narrow-pane (smoke video 2026-09-02 14:55)

*Run of `/ui-design` in **review → elevate** mode against the 4.5-minute recording of the user
exploring TheTerrace workspace: node list → Contexts (graph filter) → Class/Sequence/Source viewers
→ launching Claude Code + Copilot → Sessions/Board/Leaderboard, and instructing Copilot to post to
the Loomkeeper board.* Grounded in `DESIGN.md`, the defect-class register, and the surface code
(read, not recalled — E15).

## 1. Measured before diagnosed (DX23)

| Surface | Measurement | Reading |
|---|---|---|
| **Sessions** | ~16 rows, **15 × Ended + 1 ~ Stale, 0 ✓ Alive**; every row identical identity (`Terminal · TheTerrace/docs/fix-broken-design-links`); all `Not Recorded · Not Recorded · 0 span(s)` | A live-status list dominated by dead, indistinguishable history. The live/stale state is invisible. |
| **Board / Leaderboard / Ledger (empty)** | one muted line top-left ("No board posts yet.") in a ~1000 × 430 px empty pane | Honest but sparse; teaches nothing, no focal point. |
| **Graph in the left column** | ~55% of the ~280 px-wide pane is legend/info text; the graph occupies the lower ~45%, ~230 px tall | Unusable at that width — the chrome dominates the content. |
| **Contexts cards** | stat line clipped ("… · 302 crossin") | Overflow not handled (DX16). |
| **Class diagram** | 40 boxes, method signatures truncated with "…", "+2 more" | Dense; acceptable for the archetype but the truncation loses the signature. |
| **Right stack** | 7+ tabs (Sessions·Board·Leaderboard·Ledger·Class·Sequence·Source) + overflow chevron | Tab crowding; navigation cost. |

## 2. Rubric findings (structure before surface, DX24)

Severity: 4 Blocker · 3 Major · 2 Minor · 1 Nit. An accessibility finding ≥3 is a Blocker (U16).

| # | Location | Dimension | Sev | Evidence | Fix | Confidence |
|---|---|---|---|---|---|---|
| 1 | Sessions | **Archetype/IA + findability** | 3 | 15 identical Ended rows bury the 1 Stale/live row; no ordering, no grouping | Lead with live (Alive→Stale); collapse the Ended history behind a count; teaching empty state | **Verified** (code read + video) |
| 2 | Board/Leaderboard/Ledger/Sessions | **State completeness** (U9/DX9) | 2 | single muted line in a vast empty pane | Teaching empty state (name the first action); Sessions done, others in the plan | Verified |
| 3 | Graph (narrow pane) | **Craft / density-with-hierarchy** (TQ1) | 2 | legend/info dominates; graph tiny under ~320 px | Collapse/condense the legend below a width threshold; floor the canvas height | Verified |
| 4 | Contexts cards | **Overflow** (DX16) | 2 | "302 crossin" clipped | Wrap or abbreviate the stat line; ellipsis with full value in a tooltip | Verified |
| 5 | Right stack | **Navigation** | 1 | 7+ tabs | Consider a viewer group or an overflow affordance; low priority | Inferred |

**Not a UI defect (handed off):** harness/model `Not Recorded`, trust `Asserted`, `0 span(s)`, and
liveness never `Alive` for a running agent are the **watcher telemetry gap** — Core's
`feature/agent-watcher-substrate` (see the enlistment decision note). The *surface* renders the
telemetry honestly; this review does not attempt to fix the data. **The Sessions redesign composes
with that fix**: once real harness/model/liveness land, leading with live sessions is exactly where
they will show.

## 3. Scorecard

| Dimension | Before | After the elevate |
|---|---|---|
| Archetype fit (Sessions as live-status) | ✗ graveyard | ✓ live-first, history collapsed |
| State completeness (Sessions empty) | ✗ blank | ✓ teaching empty state |
| Findability (which session is live?) | ✗ buried | ✓ top of the list |
| Accessibility (WCAG AA, not colour-alone) | ✓ (chip glyph+text) | ✓ (unchanged; Expander is keyboard-operable, named) |
| Empty states (Board/Leaderboard/Ledger) | ✗ sparse | — (in the plan) |
| Graph in narrow pane | ✗ chrome-dominated | — (in the plan) |

## 4. Ranked plan

**Must-fix — DONE this run (highest improvement-to-effort):**
- **Sessions surface: live-first + collapsed history + teaching empty state.** `SessionRowPresenter.Partition`
  (pure, tested) splits Alive+Stale from Ended; the renderer leads with the live rows and collapses the
  Ended pile into a keyboard-operable Expander ("N ended session(s)", collapsed by default); an empty
  store shows a teaching line naming the first action. 4 new tests. **This is the single change that
  most improves the surface the user's collaboration goal depends on.**

**Should-fix next:**
- ✅ **DONE:** Teaching/centered empty states for **Board / Leaderboard / Ledger** — `ListPane` now
  renders a single centred, width-constrained message host for the empty case instead of a stray line
  top-left; the status text carries the teaching copy.
- **Graph legend condensation** under a width threshold + a canvas height floor, so the graph is usable
  in a narrow column.

**Worth doing:**
- Contexts card + class-diagram **overflow** handling (wrap/abbreviate + tooltip with the full value).
- Right-stack **tab overflow** affordance / viewer grouping.

## 5. Residual risk / flagged

- The Sessions redesign is proven headless (partition ordering) and at the rendered surface (live
  ScrollViewer + collapsed Expander), but the *felt* interaction (expanding the history, a session
  transitioning Alive→Ended live) needs functional confirmation in the running app.
- The empty-state and graph-legend items are **not** yet built — flagged in the plan, not this run.
