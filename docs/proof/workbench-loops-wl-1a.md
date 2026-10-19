---
id: proof-workbench-loops-wl-1a
title: "Proof Pack: WL-1a — the revision ↔ attempt seam (walking skeleton)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [proof, workbench-loops, wl-1a, attempt, run-log, stop, sweep, redaction, nfr-11]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0041-execution-attempt-lifecycle, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: implements }
review-by: "2026-11-18"
summary: >-
  The slice Proof Pack for WL-1a: one row per §WL.11 WL-1a check, each citing its evidence folder. The host
  checks (T5) are red-then-green with the recorded real claude-code Stop (Interrupted); the schema checks are
  T2's, the redaction function T7's, the folds T6's and the App binding T8's. The human demo script is left
  for the Leader after T8 joins.
---

# Proof Pack: WL-1a — the revision ↔ attempt seam

- **Change:** branches `wl1a-attempt-host` (T5, nodes 1–2), `wl1a-schema` (T2, on main), `wl1a-runevent` (T3, on
  main), `wl1a-redaction` (T7, on main at `7c54de9e`), `wl1a-folds` (T6), `wl1a-notebook-binding` (T8), joined by
  the Leader.
- **Spec / design:** `docs/architecture.md` §WL.11 (the WL-1a row) · ADR-0039, ADR-0041, ADR-0042 ·
  `docs/design/workbench-loops-wl-1a.md`.
- **Tier:** T2.
- **Author / date:** T5 (`wl-t5-host`), 2026-10-19. Each row's evidence is the cited file; the Leader re-runs at the join.

## Claims & evidence — one row per §WL.11 WL-1a check

| # | Check (§WL.11, verbatim) | Owner | Tests (exact) | Evidence (red → green) | Confidence |
|---|---|---|---|---|---|
| 1 | write-ahead crash injection at W1–W4 → `OutcomeUnknown{cause}`, never resend | T5 n1 | `AttemptHostTests.ACrashInAWindow_SweepsToDeliveryUnknown_AndNeverResends(W1..W4)`; `AttemptGateTests.Sweep_*` | `host/write-ahead-crash-windows-red.txt` → `-green.txt` | Verified |
| 2 | same `dispatch_key` twice → one launch | T5 n1 | `AttemptHostTests.SameDispatchKeyTwice_LaunchesOnce`, `.TwoConcurrentDispatchesOfOneKey_LaunchOnce`; `AttemptGateTests.BeginTwice_*`, `.BeginAgain_*` | `host/dispatch-key-idempotency-*.txt` | Verified |
| 3 | stale-prepare refusal | T5 n1 | `AttemptHostTests.AStalePrepare_IsRejected_AndStartsNoProcess` | `host/stale-prepare-*.txt` | Verified |
| 4 | Stop: `session/cancel` sent → `acp.result cancelled` observed → `Interrupted` | T5 n2 | `AttemptStopTests.Stop_SendsSessionCancel_AndTheEnginesCancelledReplyIsInterrupted`, `.Stop_ThatIsNeverAcknowledged_IsDeliveryUnknownStopNotAcknowledged_AtTheDeadline`, `.APermissionAskedDuringStop_IsAnsweredCancelled_AndRecordedAsPermissionCancelledByStop`; real lane: `AttemptRealLaneStopTests.Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded` | `host/node2-app-red.txt` → `host/node2-app-green.txt`; real run `host/real-lane-stop/` (Interrupted: mark 86, reply seq 143) | Verified |
| 5 | token cancel alone ⇒ not `Interrupted` | T5 n2 | `AttemptStopTests.ATokenCancelAlone_TheHostsOwnTeardown_IsNeverInterrupted`; `RunLogOrderTests.AStopRequestedTurn_StaysStopRequested_WhenAWaitOrAResumeArrives` (ADR-0041 §4 fold precedence) | `host/node2-app-*.txt`, `host/run-log-core-*.txt` | Verified |
| 6 | migration golden round-trip incl. WAL snapshot/restore and fresh-vs-migrated `sqlite_master` equality | T2 | see T2's pack | `workbench-loops-wl-1a/sp1/README.md` | Verified by T2 (cited, not re-run here) |
| 7 | the DM11 forbidden-update test | T2 | see T2's pack | `workbench-loops-wl-1a/sp1/README.md` | Verified by T2 (cited) |
| 8 | the constraint table's violation tests | T2 | see T2's pack | `workbench-loops-wl-1a/sp1/README.md` | Verified by T2 (cited) |
| 9 | **NFR-11 oracle**: `Dispatched.ts`, terminal `ts`, events, bytes, outcome, `usage` or Not recorded | T5 n2 | `AttemptObservabilityTests.EveryAttempt_HasDispatchedAndTerminalTs_EventCount_Bytes_Outcome_AndUsage`, `.AnAttemptWithNoUsage_ReadsNotRecorded_NeverZero` | `host/node2-app-*.txt`; real lane `host/real-lane-stop/measures.json` | Verified |
| 10 | `attempt.sweep{count,last_sweep_at}` emitted even at zero | T5 n2 | `AttemptQueueAndSweepTests.TheStartupSweep_EmitsAttemptSweep_EvenAtZero`, `.TheBackstopTimer_SweepsAndEmits_OnItsInterval`, `.AnEngineExit_SweepsAnAbandonedDispatchedAttempt_ToRelayLost` | `host/node2-app-*.txt` | Verified |
| 11 | redaction fixture: sentinel in a frame ⇒ run-log bytes clean + redaction row | T7 + T5 n2 | function: T7's `FrameRedactionTests`; end to end: `AttemptRedactionTests.ASentinelInAFrame_IsStoredRedacted_WithARedactionRow_AndNoSubscriberSeesIt` | `workbench-loops-wl-1a/redaction/` (T7); `host/redaction-wiring-red.txt` → `-green.txt` | Verified |

Supporting host checks (the seam the rows above run on), red then green in `host/node2-app-*.txt` and
`host/run-log-core-*.txt`: run log append-before-publish (`RunLogOrderTests.EveryEvent_IsInTheRunLog_BeforeThePeerPublishesIt`),
`RunLogUnwritable` (`RunLogOrderTests.ALineThatCannotBeMadeDurable_*`, `AttemptRunLogTests.AnUnwritableRunLogLine_*`),
relay fan-out (`AttemptRunLogTests.TheRelay_WritesTheSameInstanceToEverySubscriber_AndTracksTheHighWaterSeq`),
subscribe + catch-up (`AttemptRunLogTests.ARun_WritesItsRunLog_AndASubscriberSeesEveryEventOnceInOrder`), queue and
Send-now (`AttemptQueueAndSweepTests.AQueuedSuccessor_*`), `fold.duration_ms`
(`AttemptObservabilityTests.AFoldActivity_WritesFoldDurationMs_ToTheRollingLog`), G2/G3 uniqueness and the sole host
(node 1, `host/gate-*.txt`, `host/sole-host-*.txt`).

Slice deliverables owned by other tracks (cited; their folders join with them):

| Deliverable (§WL.11 "Builds") | Owner | Evidence |
|---|---|---|
| `WorkspaceSchema` v2, migration chain, WAL-safe snapshot, the dims and widened facts | T2 | `workbench-loops-wl-1a/sp1/README.md` |
| `RunEvent` envelope fields, `TurnState` +6 | T3 | `workbench-loops-wl-1a/sp2/` |
| folds `AttemptState` + `Currentness` | T6 | T6's evidence folder — not on main at `6c9782ca`; the Leader cites it at T6's join |
| the `session-document` kind re-bound to notebook + attempt; CTS/queue moved to the host | T8 | T8's evidence folder — pending T8's join |

## Oracles — why each is trustworthy

- **Far-end counts.** Prompts, cancels and frames are counted in the engine's own trace (the stub's
  `.aide-stub-<pid>.log`), not by the host that sends them, so a host that lies about what it sent fails.
- **Rows read back.** Every host assertion reads the receipt from the real SQLite store after the terminal commit.
- **Red first.** Every T5 test above failed for its stated reason before the code (the `*-red.txt` files), at commits
  `6aad0db4` (node 1), `1b560f5d` and `07922bfd` (node 2).
- **The real lane.** Check 4 ran once against `claude-agent-acp` 0.75.1 on the operator's Max subscription.

## Residual risk

- **Flagged — personal data in the run log.** The adapter's `_auth/status_update` puts the account email and
  organization in every run log; v1 redaction covers credentials only (`host/real-lane-stop/README.md`, "Finding").
- **Flagged — JSON-escaped secrets.** `FrameRedaction` v1 scans the raw line and does not decode JSON string escapes
  (its own remarks); a secret the adapter writes as `\uXXXX` escapes is not matched.
- **Inferred — one host per workspace** rests on the lock file; a stale lock after an OS handle leak is the residual.
- **Not built in WL-1a:** the design's `aide.attempt` spans (`attempt.begin` / `attempt.terminal`) — the rolling log
  carries `attempt.sweep` and `fold.duration_ms`; the relay retention cap, steer, retry UI and budget are WL-4.
- **One real Stop run** proves the adapter version it ran; a new adapter release re-opens check 4.
- **Closed by T17e: a read-back still running when the shell shuts down.** Before the fix, a read-back that returned
  after `WorkbenchShell.Dispose` wrote its turns into the disposed notebook. A store that was already disposed let an
  `ObjectDisposedException` escape. Both were reproduced with a gate, not a rerun (`workbench-loops-wl-1a/dispose-race/`).
  The demo was only mildly exposed. The kill step (step 19) ends the process and disposes nothing. A normal close would
  have to land inside a read-back of about 23 ms, which is unlikely. Measured in T17d's shell-path test, n=10: host-open
  median 16.5 ms, read-back median 23.25 ms (`workbench-loops-wl-1a/readback/README.md`).

## Human demo script

*Written by the Leader (`wl-leader2-20261019`) after T8, T16/T16b and T17/T17b/T17c joined (Rulings 161–165). Keyboard
only (architecture.md:1503). Every gesture and string below was read from the code at the T17 tip, with file:line in
the Leader's gesture table. Lines marked **rehearse** are gestures whose exact keystroke count is not fixed in code.
GUI steps not yet run by a person are **owed to the user**.*

### 0. Build and launch
Run every step from a **clean worktree of origin/main**, never the primary checkout (`C:\Projects\ai-de`), because joins
change the primary while you walk through. The Owner prepared one for this walk-through at `C:\Projects\ai-de-demo`.
1. `dotnet build AiDe.sln -c Release`
2. Run `src/AiDe.App/bin/Release/net10.0-windows/AiDe.App.exe`. The path was confirmed on disk at 2026-10-19 08:17Z.

### 1. A notebook on the real claude-code lane
3. **Ctrl+1** opens the Factory perspective. **Ctrl+N** opens *New session*; if no workspace is open, a workspace
   chooser comes first.
4. In the sheet, **Tab** to an account under the Anthropic provider's heading and press **Space** to tick it. The
   heading's exact text comes from the account's provider key and was not confirmed in code. Then
   reach its *Default for this session* radio (UIA "Default account: {label}") and press **Space**. The engine
   sub-line reads `claude-code`. Press **Enter** (*Create session*).
5. Type a prompt in the editor. Press **Ctrl+Enter**: one press sends in the default mechanical mode; under an agentic
   compile rung the first press prepares and the second sends ("Preparing… press again when prepared").
6. **Expect:** turn b1 appears and runs on the real lane.

### 2. See the attempt bound to its revision, request and route (Ruling 161)
7. Reach b1's row (**Shift+F6** from the composer, or **Shift+Tab**; **rehearse** which lands on the row). Then
   **Tab** to the *provenance* disclosure (UIA "Provenance of b1") and press **Space**. **Esc** closes it.
8. **Expect:** the first line inside the disclosure, which is also its HelpText, reads
   `Sent from revision 1 · request {7 hex characters} · via claude-code`. The 7 characters are the real fingerprint
   of the request you prepared, never a fixture value.

### 3. Edit, and see the old result marked stale
9. Return to the editor and change the draft.
10. **Expect:** the notices strip above the composer reads *"This output belongs to an earlier draft. Run again to
    refresh it."*. It is also announced politely to a screen reader. b1's provenance line still names revision 1
    and its own request (AC-R01a).

### 4. Stop on the real lane: StopRequested, then stopped by you
11. Send a long turn, b2, e.g. *"Write the numbers from 1 to 400 as English words, one per line. Do not use any
    tools."*. While it runs, reach its row and its **Stop this turn** button (the row's first action; **rehearse**
    whether Shift+Tab lands on it directly). Press **Space**.
12. **Expect:** the outcome first reads *stop requested* (box "Stop requested. Waiting for the harness to
    acknowledge."), then *stopped by you* (box "Stopped by you after {N edits}; {spend}. Send the same turn again as a new
    turn, or open the log."). If the adapter does not acknowledge within 10 s, the honest terminal is *delivery
    unknown* (`stop_not_acknowledged`), which is a correct result, not a failure.

### 5. Hide the pane mid-run; the run continues
13. Send another long turn, b3. While it runs, press **Ctrl+W**.
14. **Expect:** a screen reader announces *"Pane hidden. The run continues."*. The line is spoken, not shown; use
    Narrator (**Win+Ctrl+Enter**) or Accessibility Insights to hear or see it.
15. Reopen: **Alt+F**, then **S** (*Recent sessions*), arrow to the session, **Enter**. **Expect:** b3 is still
    running or has finished. No separate "run continued" message is shown.

### 6. Queue a turn, then cancel it
16. While a turn runs, press **Ctrl+Enter** with new words. **Expect:** the status line offers *"b{n} is running.
    Wait — send after b{n}, or start a parallel session."*. **Tab** to the *Wait — send after* link and press **Enter**
    (**rehearse** the Tab count). The queued row reads *"b{m} queued — sends after b{n}"*.
17. Reach the queued row, **Tab** to **Cancel**, and press **Space**. **Expect:** the outcome reads *cancelled by
    you*, and the words return to the editor. The row's HelpText reads *"b{m} cancelled by you before it was sent."*,
    which you can check in Accessibility Insights.

### 7. Restart, and kill the host mid-run
18. Close the app and start it again (step 2). Reopen the session (**Alt+F → S → the session → Enter**).
    **Expect:** every recorded attempt is a row again, in dispatch order, each with its provenance line (step 7).
    A restored free-form turn that finished reads *completed* (not "answered"; a recorded residual).
19. Send a long turn. While it runs, kill the app from another PowerShell: `Get-Process AiDe.App | Stop-Process -Force`.
    Start the app and reopen the session.
20. **Expect:** that turn's row reads *delivery unknown*. Its HelpText is the reason sentence followed by
    *"Last event received: #{n} at {HH:mm:ss}."* (or *"…: not recorded."* if no event arrived). Focus the row, **Tab**
    to the *{N} events* fold, and press **Space**; its events are listed and present in the UIA tree.
21. **Expect:** a new turn sent now continues the numbering after the restored rows. It never takes b1 again.

### 8. The recorded real-lane Stop run (the second recorded run, for the Proof Pack)
22. **This step spends real adapter usage on your Claude account.** From the demo worktree's root in PowerShell:
    `$env:AIDE_REAL_ADAPTER_ROOT = "C:\Projects\ai-de\spikes\acp-subscription-lane"; dotnet test tests/AiDe.App.Tests --filter "FullyQualifiedName~AttemptRealLaneStopTests"`.
    The path is absolute because the adapter lives in that folder's `node_modules`, which git ignores, so only the
    primary checkout has it installed. To use the folder inside another tree instead, run `npm ci` in that tree's
    `spikes\acp-subscription-lane` first (it installs from the committed lockfile). Without the variable the test
    reports **skipped**, never passed.
23. **Expect:** `AttemptRealLaneStopTests.Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded` passes.
    Its PASS output becomes the second recorded real-lane run in this Proof Pack. Scrub any account email or
    organization from captured files before committing (brief rule 15); the identity gate refuses them.

### 9. Owed from WL-0 (folded into this demo)
24. WL-0's GUI steps 2–4 (`docs/proof/workbench-loops-wl-0.md`, "Human demo script"): reindex after an uncommitted
    edit shows the new fact; the entry-points view on a large repository shows a bounded list with an "Omitted (n)"
    disclosure. Those steps' keys come from the WL-0 script and were not re-read in code for this script.

### Still owed to the user (not observable by the Owner's session)
- Every GUI step above (1–7, 9) needs a person at the window. Each is labelled not observed until run.
- The native IME check for E-1 (WL-2).

## Acceptance (Owner, 2026-10-19)

**WL-1a is ACCEPTED by the user's demo, 2026-10-19.** The user walked the human demo script above in a clean worktree of
origin/main (`C:\Projects\ai-de-demo`, detached at `52f83032`, Release build), between the Owner's `USER DEMO IN PROGRESS`
(15:34Z) and `USER DEMO DONE` (15:46Z) board posts, and reported "all steps passed", restart step 18 included. Ruling 172's
reopening is closed by T17d (`e1280798`, step-8 `a0e33443`) and T17e (`293c18a4`, step-8 `64f713a4`). T17e joined after the
demo tree's base; it changes only a read-back pending at disposal, which no demo step reaches. Recorded by the Leader
(`wl-leader3-20261019`) from the Owner's relay of the user's report; the Leader did not observe the walk-through.

The user raised two findings during the walk-through. They are work items, not acceptance blockers:

- The compiled prompt did not show in the compiled field after a submit. This was a recurrence of Ruling 96. Its root
  cause was verified (T28: the submit empties the draft that the field is a live view of), and it is fixed on main under
  Ruling 181 (`cf31f424`, `367f4de4`).
- The user expects the goal and done-criteria fields to be prefilled from an analysis of the prompt (AC-W01a). Ruling
  180 makes this the default; T30 is building it on branch `wl-prefill`.

**Residual (Ruling 182 (3)):** until WL-2's draft store (ADR-0043), **unsent typing and kept suggestions are lost when
the app restarts.** A revived notebook's composer starts empty (`ComposerSurface.cs:96`, measured by T30 n2 at
`8afa3a58`).

**Correction (2026-10-19, T30 n3):** this line first said that a sent turn's compiled prompt is on its row's disclosure
after a restart. **On main that is false.** T30 n3 measured the restored row's disclosure **empty: 0 of the 263
characters sent** (`19437c6e`, a real red through the shell's revive). The cause is that `NotebookRuns.RestoreAsync`
accepted each restored turn with `string.Empty` (`NotebookRuns.cs:525`), and no store keeps the sent bytes, only their
sha (`EnvelopeEvents.cs:218`). The fix is green on branch `wl-prefill` (`9234fc77`) and not yet joined. It rebuilds the
sent bytes through the send gate's existing projection, and shows them only when their sha equals the one recorded at
send; otherwise the row says *Compiled prompt not recorded*. A turn whose bytes included attachment bodies or a template
cannot be rebuilt, so after a restart its row says *not recorded*. The turn rows themselves, their provenance lines and
their outcomes did survive the restart, which is what WL-1a's step 18 checked.

The history before this acceptance:

**WL-1a was reopened (Ruling 172).** In the running app a reopened notebook never read its attempts back. The shell binds the host asynchronously (`WorkbenchShell.cs:3596`) and builds the document at once, and the read-back ran only when the host was already bound at construction. T17's tests bound the host first, so they passed without exercising the product order. The provisional acceptance below rested on those tests, so its restart step is withdrawn until T17d joins.

**T17d's fix and proof** (`wl1a-readback`; `workbench-loops-wl-1a/readback/README.md`):

- Checks 1–2: the read-back runs when the host binds, before or after construction, once per bind. Check 1 runs through `WorkbenchShell.ReviveRestoredSessionDocuments`. `readback/check1-red.txt` → `check1-green.txt` and `readback/check2-red.txt` → `check2-green.txt`.
- Check 3: Ruling 163's tests now reopen in the shell's order, and a per-site guard fails any read-back test that binds the host first unless the site is named as not a reopen. `readback/check3-red.txt`, `check3-red-per-site.txt` → `check3-green.txt`. Mutation red: `readback/check3-mutation-red.txt`.
- Ruling 175 (node 3): a Send while the turns restore keeps its words, disables Send with the restoring sentence, and admits exactly one turn as b3 (check A). A failed read-back or host open keeps the words with its own cause (check B). Node 2's `NoDispatchOutsideTheHostTests` regression is closed by Ruling 175(5).
- Ruling 175 and the Owner's two corrections (node 4): a restoring Send is never announced as "sent" (`readback/check4-1-*.txt`). A failed read-back reads "Restoring this notebook's turns failed: {cause}. Retry to restore them.", and a failed host open keeps Ruling 159's words (`readback/check4-2-*.txt`). The host-open and read-back durations are emitted on the normal path as `notebook.host-open` and `notebook.read-back`, through `WorkbenchDiagnostics.Write` (`readback/checkC-*.txt`).
- **Measured on this machine (shell-path test, not the running app).** The shell's revive test ran 10 times at fa310512 (`readback/timings-shell-path.txt`). Host-open (bind start to bound): min 15.7, median 16.5, max 18.5 ms. Read-back (2 attempts): min 22.2, median 23.25, max 26.6 ms. The running app writes the same records to the workbench log, so step 18 leaves its values there.
- Whole suites, node 4 at fa310512 (`readback/suite-green.txt`). Core: 3024 passed, 0 failed. App: 1143 passed, 2 failed. One failure is the environmental `SolutionTreeChordTests` ("foreground not held", fails alone too). The other is a teardown failure in `TheRowsAttemptBindingSurvivesRestartAndSequenceTests.ASentTurnsRow_ShowsItsBinding_WhileTheRunIsStillHeld_ThroughTheSurface`: SQLite's `Close` threw from `NotebookFixture.Dispose`. It is **not reproduced**: alone 3/3, and the Records namespace 3 × 89/89 (`readback/teardown-race-rerun-n4.txt`). Its cause is not established. Joining over it is the Leader's call.
- **The human demo's restart step is the one this fixes:** step 18 (section 7, "Restart, and kill the host mid-run"). Walk it after T17d joins. Before that join it fails in the running app.

The provisional acceptance, as the Owner recorded it before Ruling 172:

**WL-1a is ACCEPTED, PROVISIONALLY.** The Owner (`wl-owner-20261018`) ran these on a clean detached tree at `origin/main` `ba2b7e5e`:

- `dotnet build AiDe.sln -c Release`: 0 warnings, 0 errors.
- Core: 3019 passed, 0 failed, 1 skipped.
- App: 1123 passed, 1 skipped. `SolutionTreeChordTests` reported NOT CHECKED in the Owner's environment ("foreground not held", refused alone too). Its evidence is the Leader's solo PASS on the T17 join head (`restored-turns/solution-tree-chord-solo-on-join-head.txt`).
- **Second recorded real-lane run:** `AttemptRealLaneStopTests.Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded` PASSED (1/1, 15 s) with `AIDE_REAL_ADAPTER_ROOT` set to the repository's `spikes/acp-subscription-lane` folder, on 2026-10-19. The run spent real adapter usage on the user's account. The first recorded run is `host/real-lane-stop/` (T5 node 2).

**Not observed: the GUI demo (steps 1–9 above, including WL-0's folded GUI steps 2–4).** The Owner's session has no desktop control, so these steps are owed to the user. If any step fails, WL-1a is reopened.
