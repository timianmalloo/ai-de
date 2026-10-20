---
id: proof-wl-deadholder-repro
title: "Evidence: T34 - ADeadHolder_NeverLocksTheUserOut under full-suite load, NOT REPRODUCED, N=10"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, flake, read-only]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  The full App suite ran 10 times on 0b00e858 with the hang collector. ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds
  passed in all 10. NOT REPRODUCED, N=10. No cause is claimed. The OS left-button state read 0x8000 in every sample taken.
---

# T34 - ADeadHolder under full-suite load

**Result: NOT REPRODUCED, N=10.** No code change, no committed test. Tree `wl-deadholder-repro` at base `0b00e858`.

## Method
- R0: `dotnet build AiDe.sln` once (`build.txt`: 0 warnings, 0 errors). All runs use `--no-build`.
- R1: full `tests/AiDe.App.Tests`, `--blame-hang --blame-hang-timeout 25s --blame-hang-dump-type mini`, trx logger. The board was read before each run. No `USER DEMO IN PROGRESS` post was on it, so no run was held.
- Hang collector check (one filtered run, the test alone): it ran and passed; "All tests finished running, Sequence file will not be generated". A sequence file and dump are written only on a hang. None was produced in any of the 10 runs (the `deadholder-runs` folder holds only the trx and txt files and per-run guid folders).
- ADeadHolder outcome was read from each `app-<k>.trx` (one entry per run, `outcome="Passed"`) and from the `Failed` lines of each console log.

## Code read (Verified, opened this turn)
- Test: `tests/AiDe.App.Tests/Workbench/Records/NoDispatchOutsideTheHostTests.cs:236-261`. It starts `powershell.exe` which opens the lock file with `FileShare.None` and sleeps 120 s. It then loops up to 30 s calling `AttemptHost.OpenAsync(...)` (disposing each success) until `AgentPlaneException` is thrown. It asserts `HostNotSole`, kills the holder, and opens once more.
- Driven code: `src/AiDe.App/Conductor/AttemptHost.cs:387-410`. `FileStream(OpenOrCreate, ReadWrite, FileShare.None)`; an `IOException` becomes `AgentPlaneException(HostNotSole)`; a success runs `SweepAsync` and starts the backstop.
- The awaited task in the loop is `AttemptHost.OpenAsync` (the ledger sweep) and the process start of the holder; the polled file is `<fixture root>/.aide/attempt-host.lock` (`:30`). No failure occurred, so nothing was observed at a timeout.

## Runs
See `runs.txt` (one line per run). Each run: 1271 tests, 2 skipped. Failures per run were the known ones: `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExpos...`, `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_Switch`, and `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` (3 failures in 9 runs, 2 in run 5; runs 1, 5 and 10 were read by name, the rest by count). ADeadHolder passed in 10 of 10.

## Left mouse button state (Owner amendment; observed values only)
Script: `mouse-state.ps1` (separate PowerShell process, `GetAsyncKeyState(0x01)` x10 at 300 ms, `GetSystemMetrics(23)`; no input injected).
- 10:05:27Z (after run 1 ended, before run 2; **not** at my start - the amendment arrived after run 1): `0x8000` x10, SM_SWAPBUTTON=0.
- 11:00:55Z (after run 10, no suite running): `0x8000` x10, SM_SWAPBUTTON=0.
- No failure occurred, so no reading right after a failure exists. The Owner's own reading at 10:00Z was `0x8000`. No cause is given either way.

## Not captured
- Any failure state (awaited task, holder state, lease content): there was no failure.
- A reading before run 1 started (the amendment came later).
- Which of the 3 known failures occurred in runs 2-4 and 6-9 by name (counts only).
- No scratch capture test was written: it applies only after a failure.

assume: the machine state during these 10 runs (a quiet desktop, no join in flight) differs from the conditions of the three recorded failures, which happened during joins and track suites. Confirm: repeat while another App suite or a join recount shares the machine. If false: the rate is low enough that N=10 cannot see it.
