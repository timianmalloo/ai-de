---
id: proof-workbench-loops-wl-1a-restored-turns-handoff
title: "Hand-off — WL-1a T17 node 1 (restored turns)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff]
links:
  - { to: architecture, rel: implements }
  - { to: proof-workbench-loops-wl-1a-restored-turns, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  What T17 node 1 landed (checks 1 and 2, red then green), what is still open (check 3 and the seam request's answer), and the next step for node 2.
---

# T17 node 1 hand-off

## Done (red, then green)

- Check 1: restored rows, one per recorded attempt, in dispatch order. b1 after zero attempts, b3 after two.
- Check 2: a host killed mid-run reopens as `delivery unknown`, with the last seq/ts line and the binding line.
- Commits: red `a9521559`, green `79cc1dea`, README `66dc9c20`. Proof files and their README are in this folder.

- Suites: App 1121 passed, 1 skipped, 0 failed; Core 3018 passed, 1 skipped, 0 failed (`suite-green.txt`).

## Node 2, first item: Ruling 164 (addendum-c, `146b2066`), verbatim from the Leader

Not built in node 1. The seam request `req-01M59PSMBH1JGMK8SZ8K918AFX` is answered by this ruling: the read is granted,
and the copy is set as follows.

- **String:** "Last event received: #{seq} at {HH:mm:ss}." With no event: "Last event received: not recorded." HH:mm:ss is the thread's existing local event-time format (ThreadFeed.cs:483), not ISO-8601, and "#" keeps the word "seq" out of the UI.
- **Placement:** only on a DeliveryUnknown row, after the cause's existing ReasonSentence (DESIGN.md:1380 has the relay-lost one). The HelpText repeats it verbatim.
- **Conditions:** it replaces your RestoredLastEvent const, keeping it as one const; add one DESIGN.md entry beside line 1380; seq/ts come from the existing SubscribeAsync (the read is granted); red tests for a killed-host attempt and for an attempt with no events.

What node 1 has in its place (to be replaced): `NotebookRuns.RestoredLastEvent` = `Last recorded event: seq {0} at {1}`
(ISO-8601 `O`) and `NotebookRuns.RestoredNoEvent`, appended as the row's last stderr event line, not as HelpText. The
assertion that pins it is the last one in
`AHostKilledMidRun_ReopensAsDeliveryUnknown_WithTheAttemptsLastSeqAndTs_AndItsBindingLine`. Node 2 rewrites that
assertion red-first to the ruling's string and HelpText placement, and adds the no-event red test. `DESIGN.md` is not
in T17's owned paths, so its entry needs a claim or a seam request.

## Open

- Check 3 (node 2): a restored row's events must be reachable from the row by keyboard and exposed to UIA. Restored rows
  are ordinary thread turns rendered by the same `TurnItem` and `ThreadFeed`. So the live row's keyboard and UIA tests
  are likely to pass on a restored row the first time they run. This is Inferred and was not run. Node 2 should look for
  a real red, for example the fold of a row with zero events, or the `IsLast` reason box.

## Next step

Node 2: do Ruling 164 first (above). Then write check 3's test over `AReopenedNotebookRendersItsRecordedAttemptsTests`'s reopen helper. Drive it by
keyboard through the real surface, and read the events through UIA.
