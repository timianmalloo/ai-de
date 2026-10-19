---
id: proof-workbench-loops-wl-1a-restored-turns
title: "Evidence — WL-1a T17: a reopened notebook renders its recorded attempts"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  Red and green evidence that a restarted notebook shows one row per recorded attempt in dispatch order, and that a host killed mid-run reopens as delivery unknown with the attempt's last seq and ts and its binding line (Ruling 163).
---

# T17 restored turns (node 1: checks 1 and 2)

Tests: `tests/AiDe.App.Tests/Workbench/Records/AReopenedNotebookRendersItsRecordedAttemptsTests.cs`.

- Check 1 `AReopenedNotebook_RendersOneRowPerRecordedAttempt_InDispatchOrder_AndTheNextTurnContinuesAfterThem`.
  A restart with zero attempts starts at b1. Two attempts reopen as b1 and b2, in dispatch order, with their own words
  and binding lines. A revision saved with no attempt gets no row. The next send is b3, and its attempt is its own.
- Check 2 `AHostKilledMidRun_ReopensAsDeliveryUnknown_WithTheAttemptsLastSeqAndTs_AndItsBindingLine`.
  The process dies at its terminal write (W4). The restarted host sweeps the attempt to `DeliveryUnknown{host_restart}`.
  The reopened row reads `delivery unknown`, with the existing reason sentence, the host's failure line and the binding
  line. Node 2 replaced node 1's seq/ts event line with Ruling 164's HelpText line (below).
- Red: `restored-rows-red.txt` and `restored-outcome-red.txt` hold one run with both failures on assertions
  ("renders 0 rows for 2 recorded attempts", "renders no row for the killed attempt").
- Green: `restored-rows-green.txt` and `restored-outcome-green.txt` hold one run, 11 of 11 passed (the two new tests, plus
  `TheRowsAttemptBindingSurvivesRestartAndSequenceTests` and `TheAttemptHostBindingTests`).
- Suites: `suite-green.txt`, node 2 (App 1124 passed, 1 skipped; Core 3018 passed, 1 skipped; 0 failed). Node 1 was App 1121, Core 3018.

## How

`NotebookRuns.ReadBackAsync` turns each concluded attempt it does not already hold into a thread turn. It works in
attempt `ingress_seq` order, so ordinals continue after the restored rows. Each row is built from the attempt: its
receipt is recorded under the row's ordinal, so the binding line is the attempt's own. Its events come from the existing
`IAttemptHost.SubscribeAsync` and pass through the same fold as a live run (`FoldEvent`). Its state comes from
`AttemptStateFold`. The row's words come from the envelope's `Opened.SourceText`. No store read or IPC op was added.

- The seq/ts line is new copy, so seam request `req-01M59PSMBH1JGMK8SZ8K918AFX` was sent. Ruling 164 answered it; node 2
  built the ruling's copy (below).
- The architecture says `OutcomeUnknown`. The code says `TurnState.DeliveryUnknown`, and rule 14 makes the code win.
- T16b's premise test is turned around: it is now `AfterARestart_TheStoreHoldsTheAttempt_AndTheThreadRestoresItsRow_WithTheLiveBindingLine`.
- The test double `StoreNotebookRevisions` now numbers revisions after the store's head, as the daemon op does. Before,
  a reopened model's first save reused revision 1.

## Residuals

- A restored row that ended `ResponseComplete` reads `completed` (the fold's word). Live, a free-form turn reads
  `answered`, because the goal is not stored on the attempt.
- A restored row has no decorations and no sent bytes (neither is on the receipt).
- Brief check 3 passed on its first run (no red exists); see node 2.

## Node 2 (Ruling 164, brief check 3, the Leader's ordinal check)

- **Ruling 164's line.** Tests `AHostKilledMidRun_ReopensAsDeliveryUnknown_WithTheAttemptsLastSeqAndTs_AndItsBindingLine`
  (assertion rewritten) and `AKilledAttemptWithNoEvents_ReopensAsDeliveryUnknown_WithLastEventNotRecorded`. A
  DeliveryUnknown row's `HelpText` is the reason sentence, a space, then `Last event received: #{seq} at {HH:mm:ss}.`, or
  `Last event received: not recorded.` when the run log holds no event. The row's UIA HelpText is the same string. The
  copy is one const, `TurnItem.LastEventReceived`. Seq/ts are the last event the one fold (`NotebookRuns.FoldEvent`)
  received, read through `NotebookRuns.LastEventOf` and `ThreadFeed.LastEventOf`. DESIGN.md row `RUN4a` sits beside RUN4.
  A DeliveryUnknown row with no attempt bound carries no line, so the bare-model state test keeps its reason sentence.
  Red `last-event-line-red.txt` (both: `Expected ··· "re sending it again. Last event received:" ··· Actual ··· "rn. Open
  the log before sending it again."`); green `last-event-line-green.txt` (31 of 31).
- **Brief check 3.** `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`: from the restored
  row's container, Tab reaches its fold header, Space opens it, and every folded event line is in the container's UIA
  tree. It passed on its first run (`restored-keyboard-uia-first-run-green.txt`). No red exists: restored rows use the
  live row's template, as node 1 inferred. This is a characterization test, not red-first evidence.
- **The Leader's ordinal check.** `ATurnSentBeforeTheReadBackFinishes_WaitsForIt_AndIsB3_AndEveryRowNamesItsOwnAttempt`.
  The host's attempt reads are held, a turn is sent, and then the reads are released. Red (`send-waits-for-read-back-red.txt`):
  `Expected ["b1 first words", "b2 second words", "b3 third words"]`, `Actual ["b1 third words", "b2 first words", "b3
  second words"]`. The fix is in `SessionDocumentSurface.Launch`: when the read-back is not complete, the words,
  decorations and submission are captured, the composer begins the next block, and the turn is admitted (Accept or
  Enqueue) after `LastReadBack` completes (`LastAdmission`). Green `send-waits-for-read-back-green.txt` (5 of 5).
