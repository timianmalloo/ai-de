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
  The reopened row reads `delivery unknown`, with the existing reason sentence, the host's failure line, the binding line
  and `Last recorded event: seq {n} at {ts}` taken from the run log.
- Red: `restored-rows-red.txt` and `restored-outcome-red.txt` hold one run with both failures on assertions
  ("renders 0 rows for 2 recorded attempts", "renders no row for the killed attempt").
- Green: `restored-rows-green.txt` and `restored-outcome-green.txt` hold one run, 11 of 11 passed (the two new tests, plus
  `TheRowsAttemptBindingSurvivesRestartAndSequenceTests` and `TheAttemptHostBindingTests`).
- Suites: `suite-green.txt`.

## How

`NotebookRuns.ReadBackAsync` turns each concluded attempt it does not already hold into a thread turn. It works in
attempt `ingress_seq` order, so ordinals continue after the restored rows. Each row is built from the attempt: its
receipt is recorded under the row's ordinal, so the binding line is the attempt's own. Its events come from the existing
`IAttemptHost.SubscribeAsync` and pass through the same fold as a live run (`FoldEvent`). Its state comes from
`AttemptStateFold`. The row's words come from the envelope's `Opened.SourceText`. No store read or IPC op was added.

- The seq/ts line is new copy, so seam request `req-01M59PSMBH1JGMK8SZ8K918AFX` was sent. Its fallback is in force: one
  const each, `NotebookRuns.RestoredLastEvent` and `RestoredNoEvent`.
- The architecture says `OutcomeUnknown`. The code says `TurnState.DeliveryUnknown`, and rule 14 makes the code win.
- T16b's premise test is turned around: it is now `AfterARestart_TheStoreHoldsTheAttempt_AndTheThreadRestoresItsRow_WithTheLiveBindingLine`.
- The test double `StoreNotebookRevisions` now numbers revisions after the store's head, as the daemon op does. Before,
  a reopened model's first save reused revision 1.

## Residuals

- A restored row that ended `ResponseComplete` reads `completed` (the fold's word). Live, a free-form turn reads
  `answered`, because the goal is not stored on the attempt.
- A restored row has no decorations and no sent bytes (neither is on the receipt).
- A turn sent before the read-back finishes takes ordinal 1, ahead of the restored rows. This was not measured.
- Check 3 (keyboard and UIA reach of a restored row's events) is node 2's.
