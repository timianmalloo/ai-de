---
id: proof-wl-copy-pin
title: "Evidence: T32 - every DESIGN.md copy row is pinned to the literal the code renders (DC-NEW-wl-t26-copy)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, implement, copy, design-md, defect-class]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  One App test reads DESIGN.md and pins 17 copy id rows to the literal the code renders. 10 rows and 23 copy members
  with no match are named findings on shrink-only lists. A one-character change to ChecklistCopy.Empty turns CHK1 red.
  F1 is a comment-only commit in Core.
---

# Evidence: T32 `wl-copy-pin`

**Test:** `tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs` (3 facts). Recount at base `14a55db5`: **27** id rows
(`^\| [A-Z]+[0-9]+[a-z]? \|`); the Leader's 29 was the T26 join's merged tree.

| Proof | File | Result |
|---|---|---|
| Red, empty lists | `P1-red.txt` (commit `8ec94b68`) | 2 of 3 failed, naming 10 rows and 23 members |
| Green | `P1-green.txt` (commit `516ce7d0`) | 3 of 3 |
| Mutation red | `P1-red-mutation.txt` | `ChecklistCopy.Empty` "yet." to "yet!" turned CHK1 red (1 of 3); reverted, `git diff src` empty |

## How a row is compared

The first rendered literal must be a prefix of the row's cell. Any further literal must appear in it. A prefix, not
equality, because a cell may carry a trailing annotation (a ruling, a plural form). A builder with arguments is rendered
for a unique sample, and the sample is replaced back by the row's own placeholder (`{n}`, `{7-char key}`), so the text
around the placeholder is compared. Three rows use another way: RUN2 renders `TurnCopy.ReasonSentence` for a
StopRequested `TurnView`. DOCK4's singular is rendered by the private `LayoutPersistence.KeptPromptsReport(1, 1)` through
reflection (the singular is an inline literal, no constant). RUN4a fills `TurnItem.LastEventReceived`'s `{0}` with the
row's own `#{seq} at {HH:mm:ss}` and with `not recorded`. CHK6 is pinned on `Ready to accept.` only: its three gap
clauses are inline interpolated literals in Core's `AcceptanceFold`, with no constant to pin (a partial pin, noted here).

## Finding list for the Owner

Rows with no code literal (the test names each on `UnmatchedRows`):

| Row | Why unmatched |
|---|---|
| NB1 | no code renders "Draft saved on this device." |
| RUN1 | no code renders "No reasoning summary supplied." |
| RUN3 | no code renders "Response complete. Evidence still needs review." |
| RUN4 | no code renders "Connection lost. The run outcome is unknown." (TurnCopy says "Delivery is unknown: ..." for the nearest state) |
| CHECK1 | no code renders "Acknowledged by you. Execution evidence is still missing." (ChecklistCopy.Acknowledged is a different sentence, CHK2) |
| AUTH1 | no code renders "Signed in does not mean this route is qualified." |
| DEMO1 | no code renders "Interactive prototype. Runs, accounts and evidence are simulated." |
| SC7, SC8, SC9 | not copy: layout, focus-order and announcement rules |

Copy members with no row (named on `UnmatchedMembers`): `ChecklistCopy` AcceptedEarlier, Acknowledge, AddObligation,
Defer, LinkLatestRun, Loading, Mandatory, MarkNeedsDecision, NewObligationName, NotConnected;
`DaemonNotebookRevisions.OpenRefusedCopy`; `SolutionTreeProjection` NotRecordedCopy, PythonTsCopy; `TurnCopy`
DecorationLine, DurationText, EditsText, EventsText, Name, OutcomeWord, QueuedSentence, SessionSpend, SpendText,
SpokenDuration. Each carries its reason in the test. No pinned row disagreed with the code: the 17 pins held on the
first run, so there is no copy mismatch to rule on, only the 7 rows no code renders (NB1, RUN1, RUN3, RUN4, CHECK1, AUTH1, DEMO1).

## F1 and the suites

F1: commit `44dfcaf8`, comment only, `TheTypedBoundaryIsInertButRealTests.cs:243`; cites H4 rows 11-14 of
`docs/proof/workbench-loops-wl-1a/prefill/H4-lexical-ban-sweep.md`. The defect entry's Prevent and Status:
commit `63e4bfa9`.

Suites on head `63e4bfa9`: Core 3119 passed, 0 failed, 1 skipped (`suite-core.txt`). App 1263 passed, 3 failed, 2 skipped
(`suite-app.txt`): the three known from T30 nodes 4-6. Re-run of those three (`suite-app-rerun.txt`): the chord test
passed; `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` and
`ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` failed as before. No src change in this
branch. Ratchet: `ratchet.txt`. Hand-off: [hand-off](handoff.md).
