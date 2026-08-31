---
id: proof-terminal-cursor-render-crash
title: "Proof Pack - Terminal cursor render crash fix (DC-041)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, terminal, crash, render, cursor, proof-pack, dc-041, phase-4]
links:
  - { to: investigation-terminal-cursor-render-crash, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the terminal cursor render crash (DC-041) is fixed: CellUnderCursor() returns null at the
  pending-wrap cursor position (the exact index that was one past the end of the cell array), the renderer
  reads through it instead of the raw indexer, and the guard is mutation-verified to reproduce the original
  IndexOutOfRangeException when removed. 3 new tests, full Core 970/0, App 138/0.
---

# Proof Pack: Terminal cursor render crash fix (DC-041)

- **Components:** `src/AiDe.Core/Terminal/TerminalScreen.cs` (`CellUnderCursor()`), `src/AiDe.App/Workbench/TerminalView.cs` (`DrawCursor` reads through it + clamps the drawn rect).
- **Tests:** `tests/AiDe.Core.Tests/TerminalScreenTests.cs` — 3 new (26 total in the class); full `AiDe.Core.Tests` **970/0**, `AiDe.App.Tests` **138/0**; builds clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| In-bounds, CellUnderCursor returns the cell | `CellUnderCursor_InBounds_ReturnsTheCell` | `TerminalScreen.CellUnderCursor` | cursor at (0,2) → non-null blank cell | Seen green | Verified | — |
| At pending-wrap on the bottom row it is null, not a throw (the crash) | `CellUnderCursor_AtPendingWrapOnTheBottomRow_IsNull_NotThrowing` | bounds guard | fill bottom row → cursor at (Rows-1, Columns) → null, no exception | **Yes** — removing the bounds check throws the exact `IndexOutOfRangeException` from the crash stack | Verified | mutation-verified = it reproduces the reported crash |
| Resize clamps a stale cursor (the sibling) | `Resize_Shrink_ClampsTheCursor_SoASubsequentWriteAndReadDoNotThrow` | `Resize` clamp | move to far corner, shrink → cursor in bounds, write+read no throw | Seen green | Verified | characterises the already-safe sibling |

## Testing Strategy triggers applied

- **T1 (pure deterministic model logic):** `CellUnderCursor` and the cursor-position invariants are pure; unit-tested at the boundary that crashed (pending-wrap on the last row).
- **Characterization before change (BoK V.2):** the fix hardens a render read; the regression test pins the exact model state that produced the crash, so the fix cannot silently regress.
- **T1 mutation sense:** the bounds check (the whole fix) was removed, observed to throw the exact `System.IndexOutOfRangeException` the Windows event log recorded, then reverted — the test reproduces the reported crash, not a proxy.
- **D0 hygiene:** deterministic (no WPF, no timing), isolated, focal-call + meaningful assertion.

## Why the App tests could not catch it (and what covers it now)

`DrawCursor` is focus-gated (`if (!IsKeyboardFocused) return;`), and render tests run unfocused, so no
render test ever executed the crashing branch — the crash needs a *focused* terminal in real use. The
model-level regression (`CellUnderCursor` at pending-wrap) is the deterministic control, and the
`DrawCursor` change is a two-line switch to the safe accessor + a rect clamp, reviewed against the crash
stack. DC-041's generalisation (a render path must read untrusted content through a bounds-safe accessor,
never a raw indexer) is the durable control.

## Security / robustness note

Terminal content is **untrusted and unbounded** — any program a user runs can drive the cursor anywhere.
A renderer that crashes the whole IDE on a cursor edge case is a robustness defect against adversarial
content. The fix makes the one crashing read bounds-safe; the sibling (stale cursor after resize) was
already clamped. A broad `OnRender` catch-all was deliberately not added (it would mask future render
bugs now that the root cause and its sibling are swept).

## Residual risk

- **Other future `OnRender` reads** by a cursor/derived position must use `CellUnderCursor()` or a
  bounded loop — DC-041 records the rule; there is no automated focused-render test (the focus gate makes
  it impractical), so the control is the model-level accessor + the registered class.
- **Separate defect (not this fix):** the watcher UX is inert at runtime — the wiring is in the shell
  constructor, but the runtime path (`AttachWorkspace`) drops it. Documented in the investigation report
  as the immediate next step; it is a functional gap, not a crash.
