---
id: proof-wl-fold-diag
title: "Evidence: T33 - the restored-row fold observed on origin/main and on d986e4a4 (no cause claimed)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, diagnosis, read-only, accessibility]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  The real test fails alone on origin/main (aa76bd6d) and on d986e4a4, with the same message, 3 of 3 runs each. A scratch
  capture repeating its steps shows the same state on both: the Space key is handled at the fold, IsPressed turns on then
  off, no click follows, IsExpanded and IsFoldOpen stay false. No difference between the shas was observed. No cause is claimed.
---

# Evidence: T33 `wl-fold-diag`

**Test observed:** `AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`
(`tests/AiDe.App.Tests/Workbench/Records/AReopenedNotebookRendersItsRecordedAttemptsTests.cs:220`). Read-only node: no
`src/` or committed `tests/` change. Machine: this one, 2026-10-20 ~09:30-09:34Z, `dotnet 10.0.303`, window shown at
(-10000, -10000), `IsActive=True`, and the window is the foreground window in every capture.

## What was run

| Step | Tree | Sha | Runs |
|---|---|---|---|
| D1 real test, alone | `C:/Projects/ai-de-wl-fold-diag` | `aa76bd6d` (origin/main at dispatch) | 3, all FAIL "Space on the restored row's fold did not open it" |
| D1 scratch capture, alone | same | `aa76bd6d` | 9 (4 without client UIA calls, 5 with) |
| D2 real test, alone | `rt-wl-fold-diag-d986` (detached, removed after) | `d986e4a4` | 3, all FAIL, same message |
| D2 scratch capture, alone | same | `d986e4a4` | 9 (4 without client UIA calls, 5 with) |

Scratch source: `D1-capture-test.cs.txt`. It copies the real test's setup and steps (reopen, send, reopen, `FocusItem(0)`, Tab
loop, `KeyDown`+`KeyUp` of Space raised on the fold) and records state. **Adaptation at d986e4a4: none.** The same file
compiled and ran unchanged. The scratch test was deleted from `tests/` before the commit; it is not committed there.

Two modes, because a client UIA call needs an `await` on a thread-pool task, and the real test has no `await` between
`FocusItem(0)` and the Tab loop:

- `noclientuia` (the real test's timeline): client UIA calls skipped. Peer-based UIA subtree, focus, `IsExpanded` and key routing recorded.
- `clientuia`: adds `AutomationElement.FocusedElement` and a client `FindFirst` of the fold, called from a thread-pool task.

## The two captures side by side (`noclientuia`, first run each; run files `D1-capture-origin-main-noclientuia.txt`, `D2-capture-d986e4a4-noclientuia.txt`)

After removing object hashes, window handles and the clock text, these two files are identical (`diff` empty). So is `r3`.

| Item | origin/main `aa76bd6d` | `d986e4a4` |
|---|---|---|
| Row events / folded events / fold header | 6 / 4 / "4 events" | 6 / 4 / "4 events" |
| Tab from the row container | 3 stops: "Provenance of b1", "Compiled prompt of b1", "4 events" | same 3 stops |
| UIA subtree, fold, before Space | `Group "4 events"` (class Expander, `ExpandCollapse=Collapsed`, kbFocusable=False) > `Button "4 events"` (class Button, `Toggle=Off`, kbFocusable=True, offscreen=False) > `Text "4 events"` | same |
| UIA subtree, fold, after Space | unchanged: `ExpandCollapse=Collapsed`, `Toggle=Off` | unchanged |
| WPF focus before Space | `Keyboard.FocusedElement` = the fold `ToggleButton`; `IsKeyboardFocused=True`; `Mouse.Captured=null` | same |
| UIA focus before Space (clientuia only) | client FocusedElement = `Group "4 events"` (Expander), `HasKeyboardFocus=True` | same |
| `IsExpanded` (Expander) / `IsChecked` (fold) / `row.IsFoldOpen` before | False / False / False | False / False / False |
| `KeyDown` Space routing | `Handled=True` already at the first handler recorded, the fold; the same at the Expander, the row container, the window | same |
| `KeyUp` Space routing | `Handled=True` at fold, Expander, container, window | same |
| After `KeyDown` | `IsPressed=True`, `IsChecked=False`, `IsFoldOpen=False` | same |
| After `KeyUp` | `IsPressed=False`, `IsChecked=False`, `IsFoldOpen=False` | same |
| After dispatcher drain + `UpdateLayout` (S3) | `IsExpanded=False`, `IsChecked=False`, `IsFoldOpen=False`; `fold.IsMouseCaptured=True`, `Mouse.Captured` = the fold, `Mouse.LeftButton=Pressed`, `Mouse.DirectlyOver` = the fold | same |
| UIA subtree after Space | no new peer; the 4 folded event lines are not in the peer tree | same |

## Where the captures differ

**No difference between the two shas was observed.** One run-to-run variation appears on both shas:

| Variation | origin/main | d986e4a4 |
|---|---|---|
| `noclientuia`: at S3 `Keyboard.FocusedElement=null`, `window.IsKeyboardFocusWithin=False`, `IsMouseCaptured=False` (window still `IsActive=True` and foreground) | 2 of 4 runs | 2 of 4 runs |
| `clientuia`: `Keyboard.FocusedElement` null at the first Tab step, so the Tab loop never started | 1 of 5 runs | 2 of 5 runs |
| `row.IsFoldOpen` after the Space, in every run that reached the Space | False, 8 of 8 | False, 7 of 7 |

The real test never reaches that S3 state: it asserts `IsFoldOpen` after the same drain, and the assertion fails.

## Not captured

- The handler that set `Handled=True`. The record shows `Handled` was already true when the fold's own instance handler ran. `PreviewKeyDown` is not in the record, because the test (and the scratch) raise `KeyDownEvent` and `KeyUpEvent` only.
- A real `SendInput` key path. The test raises events by `RaiseEvent`; so did the capture.
- Any sha between `d986e4a4` and `5d66bd0f`; the WebView2 runtime version at run time; the foreground window's history during the run; another machine.
- Whether `Mouse.Captured` after the Space is an effect of the `RaiseEvent` call or of the host. Not separated.
- The `Preparing`/`Running` turn path (not on the test's path).

## Beliefs, marked

- assume: the `Handled=True` at the fold is set by the platform `ButtonBase` handling of Space on the fold. Confirm by a class-handler order record or a `PreviewKeyDown` record on the fold. If false, some other handler consumes Space before the fold's template toggle acts.
- assume: the intermittent null `Keyboard.FocusedElement` at S3 reflects a focus change in the host window that the capture did not record. Confirm by logging `GotKeyboardFocus` / `LostKeyboardFocus` on the window across the Space. If false, the focus loss comes from the capture's own calls.
- assume: with no sha difference in this capture, the fail-alone reading from T31 is also true at `d986e4a4` on today's machine state, and the T30 recount's pass is not reproduced here. Confirm by running the real test at `d986e4a4` in the T30 recount's own conditions. If false, the recount's pass came from a condition the 3 runs here did not have.

## Files

| File | What it holds |
|---|---|
| `D1-capture-test.cs.txt` | The scratch test's source (never committed under `tests/`) |
| `D1-capture-origin-main-{noclientuia,clientuia}[-rN].txt`, `*.run.log` | Captures and `dotnet test` logs on `aa76bd6d` |
| `D2-capture-d986e4a4-{noclientuia,clientuia}[-rN].txt`, `*.run.log` | The same on `d986e4a4` |
| `D1-real-test-origin-main*.txt`, `D2-real-test-d986e4a4*.txt` | The real test, alone, 3 runs each |
| `ratchet.txt` | The step-8 ratchet result |
