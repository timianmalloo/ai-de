---
id: proof-workbench-loops-wl-1a-sp2
title: "Evidence — WL-1a spine T3: RunEvent and TurnState vocabulary"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence]
links:
  - { to: adr-0041-execution-attempt-lifecycle, rel: implements }
review-by: "2026-11-18"
summary: >-
  Track T3's red and green evidence for the TurnState extension, the Stopped-to-Interrupted rename and the RunEvent redaction envelope; cited by the WL-1a Proof Pack.
---

# sp2 proof: RunEvent / TurnState vocabulary (T3, wl-t3-sp2)

| Check | Test | Red | Green |
|---|---|---|---|
| TurnState extended, `Stopped` -> `Interrupted` | `RunEventVocabularyTests.TurnState_IsExtendedAndStoppedIsRenamedInterrupted` | `vocab-red.txt` | `vocab-green.txt` |
| Each new value has a word, terminality, announcement | `RunEventVocabularyTests.EachNewTurnState_HasAWordATerminalityAndAnAnnouncement` (7 rows) | n/a (added with the implementation) | `vocab-green.txt` |
| `RequestStop` -> `StopRequested`; only the harness concludes `Interrupted` | `RunEventVocabularyTests.RequestStop_...`, `StopRequested_IsInFlight_...` | n/a (added with the implementation) | `vocab-green.txt` |
| `TurnState` never serialized | `TurnStateIsNeverSerializedTests` (guard + planted-fixture self-test) | `turnstate-never-serialized-red.txt` (planted file in `src/`, since removed) | `turnstate-never-serialized-green.txt` |
| Envelope keys `redactions[{offset,len,pattern_id}]`, `scanned_with` (exact) | `RunEventVocabularyTests.RunEvent_CarriesRedactionsAndScannedWith_UnderTheirExactKeys`, `..._WithoutRedactionFields_DeserializesAsNotScanned` | `vocab-red.txt` | `vocab-green.txt` |
| `native.frame` / `acp.frame` constants | `RunEventVocabularyTests.TheKindVocabulary_...` | `vocab-red.txt` | `vocab-green.txt` |
| Stop path (surface) | `ASendWhileATurnRunsOffersWaitOrParallelTests.AfterStop_TheTurnIsStopRequested_...` | n/a (existing test, semantics changed) | `stop-path-green.txt` |
| Core suite | `dotnet test tests/AiDe.Core.Tests` | | `suite-green.txt` (2795 passed, 1 skipped, 0 failed) |
| App suite | `dotnet test tests/AiDe.App.Tests` | | `suite-app.txt` (1060 passed, 2 failed, both environmental, see below) |

Environmental App failures, unrelated to `TurnState`: `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` ("foreground not held; SendInput would prove another window") failed in both full runs and in isolation (`env-flakes.txt`); `AppWindowCloseLeavesNoTerminalHostTests` ("process table could not be read") failed once in a full run and passed in isolation.

Behaviour change to know: `SessionDocumentSurface` now calls `RunChannelSessionThread.RequestStop` after its own token cancel, so the turn stays `StopRequested` (in flight) until a host acknowledgment arrives. Nothing sends that acknowledgment yet (T5/T8), so a stopped turn blocks later sends in the UI until then.
