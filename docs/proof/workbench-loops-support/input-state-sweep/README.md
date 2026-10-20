---
id: proof-wl-input-state-sweep
title: "Evidence: T37 - UI tests read the global input state they depend on, and end NOT CHECKED when it is not the default"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, input-state, flaky-tests, defect-class]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  A runtime skip for xUnit 2.9.3 with no new package, a precondition helper for the left button and the foreground
  window, five tests converted, and a recount that names every NotExecuted result with its reason. App: 0 failed.
---

# T37 - the input-state precondition (DC-NEW-wl-t33-input-state, the control)

No input was injected: the helper reads the state and never sets it. No `src/` change, no package or version change.

## S0 - the mechanism (`S0-spike.txt`)
xUnit 2.9.3 has no runtime `Assert.Skip`. What works with no package: a `[Fact]` subclass with an
`[XunitTestCaseDiscoverer]`; its `XunitTestCase` overrides `RunAsync` and wraps the `IMessageBus`; a body that throws
`NotCheckedException` (derived from `XunitException`, so `Sta.Rethrow` keeps it as itself) arrives as `ITestFailed`,
and the bus re-queues it as `TestSkipped(test, message)`. The `.trx` then says `outcome="NotExecuted"` with the
reason in `<ErrorInfo><Message>`; a real assertion in the same class stays `Failed`.

## S1 - the helper (`tests/AiDe.App.Tests/InputStatePrecondition.cs`)
- `InputPreconditions.RequireLeftButtonReleased()`: WPF's `Mouse.PrimaryDevice.LeftButton` decides; `GetAsyncKeyState(VK_LBUTTON)` is printed beside it.
- `InputPreconditions.RequireForeground(window, what)`: `GetForegroundWindow()` against the test's window, read after the test's own focus attempt. Held: the test's own assertions run and can still fail.
- Both read through `IInputStateSource` (the seam); `DesktopInputState` is the real desktop. Reasons start `NOT CHECKED:`.
- Red first: `helper-red.txt` (3 of 5 fail on the skeleton), `helper-green.txt` (5 of 5). Tests: `InputStatePreconditionTests`.

## S2 - conversions (assertions unchanged; each test is now `[CheckedFact]`)
| Test | Where the precondition sits | Result in S5 |
|---|---|---|
| restored-row fold (`AReopenedNotebookRendersItsRecordedAttemptsTests`) | `RequireLeftButtonReleased()` before the Space key | **PASSED** (button `0x0000` before and after) |
| CtrlDigit (`CtrlDigit...Tests`) | `RequireForeground` at the end of `FocusInsideAsync`, after its 20 focus attempts, before the old `InvalidOperationException` | **NOT CHECKED**: foreground `0xa80450`, test window `0x227e07cc` |
| CtrlEnter (`SolutionTreeChordTests`) | the probe's own exit code 2 (its foreground read after its focus attempt) becomes `NotCheckedException` | **NOT CHECKED**: probe said "foreground not held" |
| APrepare (`APrepareShowsItsSuggestedLinesTests`, B4) | `RequireForeground` only when `inEditorBefore` is false, before its unchanged assertion. It fits the helper cleanly. | **PASSED** |
| ComposerHost shell test (`ComposerHostIntegrationTests`, S3) | the probe prints `os keystroke: foreground held=False`; `NotChecked` only when that line is present and no `reached draft=True` line exists | **PASSED** |

## S3 - recall search (`s3-search.sh`, raw hits `S3-hits.txt`)
71 raw hits. 5 tests converted (the four named plus ComposerHost, found by following the probe hits); none of the rest is a precondition asserted as a FAIL.
| Hits | Group | Disposition |
|---|---|---|
| 20 | `tests/AiDe.App.*Probe/Program*.cs` (SolutionTreeProbe, ComposerProbe, CanvasProbe) | probe code, not tests. SolutionTreeProbe and ComposerProbe's `foreground held` line feed the converted CtrlEnter and ComposerHost tests; the other `SetForegroundWindow` calls only act, they assert nothing |
| 9 | the three converted files | converted |
| 6 | `InputStatePrecondition*.cs` (this node) | the helper |
| 29 | `Assert.*(... Keyboard.FocusedElement / IsKeyboardFocus ...)` in PerspectiveRail, ThreadFeed, TheThreadIsAFeedOfTurns, ALiveNotebookFloatsAndRedocks, AReopenedNotebook:247 | listed, not converted: these assert WPF's logical focus after a `Focus()` call, which is the product's behaviour, and they pass here. If the foreground ever breaks them, the failure is a real assertion to read, not a precondition |
| 4 | `IsActive` in `AiDe.Core.Tests` and `ZoneWorkbenchAdapterTests` | not input state (a model/tab flag) |
| 6 | comments (`CanvasFocusIntegrationTests`, `PaneRenderTests`, `Sta.cs`) | prose, no assertion |

## S4 - visibility
`tools/verify-test-run.py` now prints every NotExecuted result with its reason on every run (`not_checked_lines`), green or red; `S5-recount.txt` is that output on the real `.trx` files. No existing check was weakened.
The brief asked for a `--self-test`. I added one (red `NameError`-free stub, then green) and `tools/verify-gate-self-tests.py` refused it (rule 23, premise read: its comment at line 119 says this tool is frozen on purpose and its behaviours are proven from that file's fixtures, which drive the tool's real command line; a `--self-test` flag makes the frozen entry STALE). The premise holds, so the flag is removed and the gate is green again. **Owed, not done (outside my owned paths):** a fixture in `tools/verify-gate-self-tests.py` that plants a NotExecuted `.trx` result and expects the `[NOT CHECKED]` line with its reason. Raised as a seam request.
`expectedSkips` as committed names CtrlDigit and CtrlEnter, and not the fold (it passes here; if it skips, the recount refuses and the join stops).
The App baseline (`minimumTotal` 1274) is untouched, as the brief says; this branch counts 1279 (the 5 helper tests), so the join's re-baseline owes 1279.

## S5 - suites (beside T36)
| | T36 (button released) | T37 |
|---|---|---|
| Core | 3121 passed, 1 skipped, 0 failed | 3122 total, 3121 passed, 1 NotExecuted, 0 failed |
| App failed | 2 (CtrlDigit, CtrlEnter) | **0** |
| App NOT CHECKED | 0 (real-lane skip aside) | CtrlDigit, CtrlEnter (+ the real-projection and real-lane skips, already listed) |
| Fold | passes 0/10 failures | PASSED |
Left button: `0x0000` x10 before and after (`mouse.txt`). `S5-recount.txt`: `verify-test-run: OK`. `S5-recount-first.txt` is the first run, before `expectedSkips` named the two (it stopped on exactly those two, as designed).

## Residuals - what the precondition cannot see
- It reads the state at one instant. A state that changes between the read and the key press is not seen.
- CtrlEnter reads the probe's foreground, not the test host's, because the chord goes through the probe's window.
- The fold ending NOT CHECKED with the button down was not observed (the button never read down in T37); the helper's red covers it through the seam.
- Why the foreground is not held on this machine is not claimed (Inferred only: another window had it).
