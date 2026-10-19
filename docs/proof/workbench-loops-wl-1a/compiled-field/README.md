---
id: proof-workbench-loops-wl-1a-compiled-field
title: "Investigation: WL-1a T28 — the compiled field is empty after a submit (Ruling 96 recurrence)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, evidence, investigate, composer, ruling-96]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T28 n1's /investigate of the user's report that the compiled prompt did not show in the compiled field after a submit.
  Reproduced on the product composition with a real Ctrl+Enter: the text goes from 69 chars to 0 while the box keeps its
  48 px floor. The verified cause is the submit's own reset of the draft (BeginNextTurn on Admit), not layout. Node 2
  lands Ruling 181: the field keeps the sent turn headed 'Compiled prompt — sent as b{n}', display-only, until the next
  keystroke; the resend guard is proved by a planted-leak mutation; Ruling 96's tests are re-driven through the gesture.
  Node 3: a queued send shows its turn headed 'Compiled prompt — queued as b{n}' under the same guard (mutation red M3), and
  the class gate ComposerContentOraclesDriveTheGestureTests refuses a content oracle that skips the gesture.
---

# T28 — the compiled field is empty after a submit

- **Branch:** `wl1a-compiled-field`, off `origin/main` `624b46c4` (merged with origin/main at 2a89cf0b). Node 1 (`wl-t28-compiled`), `/investigate`, was red only. **Node 2 (§5) is the fix under Ruling 181. Node 3 (§6) is the queued state and the class gate.**
- **Symptom (the user, WL-1a demo, 2026-10-19):** they submitted a prompt, and the compiled prompt did not show in the compiled field.
- **Prior ruling:** Ruling 96 (`docs/notes/addendum-c-council-rulings.md:1626-1638`). Its cause ("the belt not re-measuring on expand") was **Inferred**.

## 1. Root cause (Verified)

A submit consumes the draft, and the compiled field is a live view of the draft. After Ctrl+Enter the field therefore shows the next, empty draft's compile, which is `""`. Nothing in the composer shows the prompt that was sent.

The chain, every link observed on the stack in `compiled-after-submit-red.txt`:

1. `ComposerSurface.OnAcceleratorKey` → `Send` → `SendThroughTheGate` → `ComposerSendGate.Send` raises the sent event → `SessionDocumentSurface.Launch` → `Admit`.
2. `SessionDocumentSurface.cs:672` — `Admit` calls `Composer.BeginNextTurn()` once the turn joins the thread.
3. `ComposerSurface.cs:1136` — `_gate.NextBlock()` (which sets `RenderedView = null`, `ComposerSendGate.cs:230`); `:1138` — `_draft.SetFreeFormText(string.Empty)`.
4. `ComposerSurface.cs:1149` — `RenderCompiledView()`; `:1224-1225` — `_compiled.Text = _gate.RenderView(_draft, _template).Text`, which is `""` for the empty draft. This is the only assignment to `_compiled.Text` in `src/` (grep, 2026-10-19).

**Necessary and sufficient.** Sufficient: the one render that reaches `BeginNextTurn` takes the text from 69 to 0 chars, in both orders of the gesture (the frames are printed). Necessary: `:1225` is the only writer of `_compiled.Text`, and the only render during the gesture that produced 0 chars ran through `BeginNextTurn`. Before it, the render in `Send` (`:621`) showed 69 chars. **Confidence: Verified** (observed). The cause is the same before and after a submit only in the sense that **before a submit there is no defect**: the open field shows the full 69 chars at 48 px.

**The sent bytes are not lost.** They are on b1's own row (`ThreadFeed.cs:664-680`, the *Compiled prompt of b1* disclosure, collapsed by default): `SentBytes` is 69 chars, equal to the sent prompt (observed). The composer's field follows its contract — DESIGN.md:879 says "exactly the outgoing text", and the AutomationName is *"Compiled prompt — exactly what will be sent"* (`ComposerSurface.cs:149`). After a submit the outgoing text is empty. The user expected to see what they had just sent. **The defect is a gap between the field's contract and the user's expectation, plus a field that turns empty with no word about why.** Which way to close it is a product choice for the Owner (§4, phase 0).

## 2. Reproduction (`reproduction.txt`)

Composition: the product's. `NotebookModel` over a real schema-v2 store, `Runs` bound to a real `AttemptHost`, and `SessionDocumentSurface(notebook)` shown in a window (as `WorkbenchShell.cs:3586-3601` builds it). The composer has the goal-block form that `SessionComposerBinder.cs:164` configures, and the default mechanical mode (one press sends; demo step 5). The words arrive through `SetFieldText` (the page's input verb), the send is `OnAcceleratorKey(0x0D, ctrl, keydown)`, and the disclosure opens through its header's toggle peer.

| Moment | IsExpanded | `_compiled.Text.Length` | `_compiled.ActualHeight` |
|---|---|---|---|
| A: typed, before the submit | False | 69 | 0.00 (collapsed) |
| A: after the submit, still closed | False | **0** | 0.00 (collapsed) |
| A: after the submit, opened | True | **0** | 48.00 |
| B: open, typed, before the submit | True | 69 | 48.00 |
| B: open, after the submit | True | **0** | 48.00 |

**The wrong value is `_compiled.Text.Length`.** The height is `CompiledPromptMinHeight` (48, `ComposerSurface.cs:307`) whenever the field is open. `IsExpanded` follows the click.

## 3. Why Ruling 96's control did not catch it — `DC-NEW-wl-t28-compiled`

Condition (2) produced two tests in `tests/AiDe.App.Tests/Sessions/TheWriterKeepsItsRoomTests.cs`:

- `TheCompiledPromptDisclosure_IsOnScreen_AtEveryTurnCount` (`:107-142`). **It drives** the real `SessionDocumentSurface` and the header's real toggle. **It does not drive** any typing or any submit. Its draft is empty, so the oracle `Text == RenderView(draft).Text` (`:139`) holds as `"" == ""`. The output reads "text 0 chars" at 0, 1 and 40 turns (run 2026-10-19). There is no non-empty assertion.
- `TheCompiledPromptDisclosure_OpenedAtTheOperatorsBelt_RendersTheCompiledText` (`:163-220`). **It drives** the toggle and asserts non-empty (69 chars). **It does not drive** the composition or the gesture: it uses a bare `ComposerSurface`, sets the draft directly, and never sends it.

The class: **a content oracle that is set up by fixture mutation never drives the user's gesture.** So the state transition the gesture triggers in the real composition (here, consuming the draft on `Admit`) goes untested. A self-referential oracle that passes on empty rides with it. Ruling 96's condition-1 measurement was taken on the same fixture, so it measured layout, not the gesture. Re-reading Ruling 96's own evidence: the operator's ledger row was at **1 turn** with 248 inputs, so a send had happened before they looked. That fits this cause better than the layout cause. **Inferred**: the screenshot was not re-read here. Sweep, derive and prevent are in `docs/lessons/defect-classes.md` (`DC-NEW-wl-t28-compiled`). The strongest sibling is `Composer/TheComposerRendersItsFieldLevelErrorsTests.cs:381`. It asserts `CompiledView == request.Prompt` after a send, on a bare composer that nothing admits. It certifies the very state the product never shows.

**Competing causes ruled out (observed):** layout or belt (H2: the height is 48 whenever open); the disclosure closing on the submit (H3: `IsExpanded` stays True in order B); the page input not reaching the draft (H4: 69 chars rendered before the submit through `SetFieldText`); an agentic two-press prepare (H5: the demo's default is mechanical, and the turn was admitted on one press).

## 4. Phased repair plan (code + tests)

| Phase | Scope (code + tests) | Failure it removes | Validation | Depends on |
|---|---|---|---|---|
| 0 | **Owner decision** on what the field shows after a submit. **A (recommended):** it keeps the sent compiled prompt, headed *"Compiled prompt — sent as b1"*, until the next keystroke renders the live draft. **B:** an empty state that points to *Compiled prompt of b1*. DESIGN.md:879 and Ruling 96 are amended to match. | The contract gap | A ruling | — |
| 1 | `ComposerSurface.BeginNextTurn`/`RenderCompiledView`: hold the admitted turn's bytes (`request.Prompt`, captured before `NextBlock`) as a display-only "last sent" view while the draft is empty. The ItemStatus says *sent as b1*. The view **must never reach `ComposerSendGate.RenderedView`**: `ComposerSendGate.cs:347` sends `RenderedView ?? RenderView(...)`, so routing the display through it would make a second Ctrl+Enter resend b1. | The empty field after a submit | `TheCompiledFieldAfterASubmitTests` (both facts) red → green. New: the first keystroke after a submit replaces the view with the live compile. A second Ctrl+Enter on the empty draft is refused with "an empty prompt is not a task". A queued send (Ruling 95) and a held send (Ruling 175) show their own state. | Phase 0 (A). Under B, the red test's oracle changes to the empty-state copy (seam raised: `req-01M5AE5QGZMKFC36RA4C93QSPJ`) |
| 2 | Tests: give `IsOnScreen_AtEveryTurnCount` a typed draft through `SetFieldText` and a non-empty assertion. Move `TheCompiledViewOnScreenIsTheWholePromptAndNotASummary:381` onto a `SessionDocumentSurface` (or relabel it pre-admit). | The tautological and gesture-skipping oracles | Each fails on the unfixed code first | Phase 1 |
| 3 | Class prevention: a shared composer-gesture test helper (type / submit / open) and a gate that refuses a composer-content assertion with no non-empty check, or with no gesture on a `SessionDocumentSurface`. | The class (`DC-NEW-wl-t28-compiled`) | The gate is seen failing on the phase-2 tests before they change | Phase 2 |

**Confidence:** root cause **Verified**. Phase 1's design is **Inferred** until phase 0 rules. The resend risk at `ComposerSendGate.cs:347` is **Verified** by reading the code; its test does not exist yet.

**Residual risk / what would change the diagnosis:** this ran in a WPF test host with no WebView2 page. In the running app, `BeginNextTurn` also pushes an empty init to the page (`:1150-1153`). That push cannot repopulate `_compiled`, because `_compiled` is written only at `:1225`, but it was not observed live. A live trace that showed non-empty `_compiled.Text` after a submit would reopen the diagnosis.

## 5. Node 2 — the fix (Ruling 181, Option A)

**What landed** (`src/AiDe.App/Workbench/Composer/ComposerSurface.cs`):
- `BeginNextTurn` reads the gate's view (`RenderedView`) **before** `NextBlock` drops it. When the thread has admitted those bytes as the turn now in flight (`_inFlight.SentBytes` equal, no queued turn), it keeps them as display-only state with the heading `SentHeading(b{n})`.
- `ShowCompiled` is now the only writer of `_compiled.Text`. It writes the box, the header, the field's UIA name and the disclosure's ItemStatus from one const, `SentHeadingFormat = "Compiled prompt — sent as {0}"`. `SetFieldText` (the next keystroke) ends the sent-state. So does any draft that compiles to words (Use as the next draft, an attachment).
- **Resend guard (Ruling 181 (2)).** The sent text is written to the box only. `RenderCompiledView` still renders the live (empty) draft through the gate first, so `RenderedView` is the empty draft's view. A press in the sent-state is refused by the gate as *"an empty prompt is not a task"* (`ComposerSendGate.cs:347` then `:350-354`).
- `DESIGN.md:879` is amended ("after a submit, the text just sent, named as sent"), and a new row follows it for the sent-state, the queued state and the held state.

**Checks (red then green, through the gesture: `SetFieldText`, then `OnAcceleratorKey` Ctrl+Enter, then the toggle peer, read on the real `SessionDocumentSurface`):**

| Check | Tests | Red | Green |
|---|---|---|---|
| A — Ruling 181 (1) | `TheCompiledFieldAfterASubmitTests`: `OpenedAfterACtrlEnterSubmit_…ShowsTheSentPrompt`, `OpenWhileTyping_ThenCtrlEnter_…KeepsTheSentPrompt` (node-1 reds; oracle now also checks the heading), `AfterASubmit_TheNextKeystroke_ReturnsTheFieldToTheLiveViewOfTheNewDraft`, `TheSentHeading_IsTheCopyDesignMdNames` | `A-red.txt` (unfixed code: field empty, 69 chars sent; no DESIGN row) | `ABC-green.txt` |
| B — Ruling 181 (2) | `TheSentPromptIsNeverResentTests`: `ASecondCtrlEnterRightAfterASubmit_WithNoKeystroke_SendsNothing` (no turn, no attempt row, `BlocksSent` and `LastLaunch` unchanged, the gate's refusal), `AQueuedSend_ShowsItsQueuedState_NotTheSentState` (Ruling 95), `AHeldSend_ShowsItsWaitingWords_NotTheSentState_UntilItIsAdmitted` (Ruling 175) | `B-red.txt`: **mutation red**. M1 is the planted leak: the fix with `BeginNextTurn`'s draft clear deleted, so the field is rendered through `RenderedView`. A second press **resent**: turns 2, attempt rows 2 (was 1), blocks sent 2, status "sent". All 3 tests are red. | `ABC-green.txt` |
| C — Ruling 181 (3) | `TheWriterKeepsItsRoomTests.TheCompiledPromptDisclosure_IsOnScreen_AtEveryTurnCount` (a typed draft, non-empty, contains the words); `…OpenedAtTheOperatorsBelt_RendersTheCompiledText` (the composition: typed, Ctrl+Enter to 1 turn, the document sized so the belt reads back 517.13, then the toggle; the oracle is b1's `SentBytes` plus the heading); `TheComposerRendersItsFieldLevelErrorsTests.TheCompiledViewOnScreenAfterASubmit_IsTheWholeSentPromptAndNotASummary` (moved onto a `SessionDocumentSurface` and renamed) | `C-red.txt`: belt and whole-prompt are red on the unfixed code. `C-red-mutation.txt`: M2 (the field is never written) kills the re-driven `IsOnScreen` test at 0, 1 and 40 turns. The **original** test (2a89cf0b) **survives** M2, 3/3 passed. That is the DC-NEW-wl-t28-compiled class, measured. | `ABC-green.txt` |

The gesture helper is `tests/AiDe.App.Tests/Workbench/Composer/ComposerGesture.cs`. It has `Drive`, `Type`, `CtrlEnter`, `Open`, `Field`, `Heading` and `AttemptRows`. n3's class gate can require it.

**Design choice, marked (Inferred, not ruled; superseded by §6 under the Owner's direction):** a **queued** send (Ruling 95) does not show its bytes in the field. The field shows the live, empty new draft under the plain heading, and the status line carries *"b2 queued — sends after b1"*. Ruling 181 names only *sent as b{n}*, and the only other way to show it needs a new copy string (for example "queued as b2"). If the Owner wants that, it is one more heading const. A **held** send keeps its words in the editor, and the field shows their live compile. Once the turn is admitted it reads *sent as b1*.

## 6. Node 3 — the queued state and the class gate (Ruling 181 (1)-(3), the Owner's direction)

**What landed:**
- `ComposerSurface.QueuedHeadingFormat = "Compiled prompt — queued as {0}"` (and `QueuedHeading`). `BeginNextTurn` keeps the gate's view as display-only state when the thread admitted it as **the turn just queued** (`_queued.SentBytes` equal), headed `QueuedHeading(b{n})`; otherwise, as before, as the turn in flight, headed `SentHeading(b{n})`. `ShowCompiled` stays the only writer, so the header, the field's UIA name and the disclosure's ItemStatus carry one string. The next keystroke returns the field to the live view. This supersedes §5's marked design choice (a queued send showed the live, empty draft).
- `DESIGN.md`: the queued-state row *Compiled prompt — after a queued send* follows the sent-state row. The sent row's queued clause now points to it.
- **The class gate** `tests/AiDe.App.Tests/Workbench/Composer/ComposerContentOraclesDriveTheGestureTests.cs` is a test, not a `tools/` gate. A `tools/verify-*.py` gate also needs a workflow line (`verify-project-coverage.py` lists any gate no workflow names), and `.github/workflows` is outside this track's paths. As a test it runs in the App suite.
- **Read on main first (rule 14):** `_queued` is set by `SetQueued` from the thread snapshot during `Admit`'s `Enqueue`, **before** `BeginNextTurn` (`SessionDocumentSurface.Admit`). So the queued turn's `SentBytes` is there to compare. The code matched the brief, and no seam was raised.

**Checks (red then green, through the gesture: `SetFieldText`, Ctrl+Enter through `OnAcceleratorKey` with b1 in flight, the status line's *Wait — send after b1* (`ComposerGesture.CtrlEnterThenWait`), then the toggle peer, on the real `SessionDocumentSurface`):**

| Check | Tests | Red | Green |
|---|---|---|---|
| A — a queued send shows its prompt | `TheCompiledFieldAfterAQueuedSendTests`: `OpenedAfterAQueuedSend_TheCompiledFieldShowsThatTurnsPrompt_HeadedQueuedAsB2`, `AfterAQueuedSend_TheNextKeystroke_ReturnsTheFieldToTheLiveViewOfTheNewDraft`, `TheQueuedHeading_IsTheCopyDesignMdNames` | `n3-A-red.txt` (`13b32da6`), on the unfixed code: the field is 0 chars under the plain heading after the queued send, and b2 holds 69 chars. There is no DESIGN row. | `n3-A-green.txt` (`1874cb5e`): these 3, node 2's 4 sent-state tests and the 3 resend tests, 10 of 10 |
| B — the queued resend guard | `TheSentPromptIsNeverResentTests.ASecondCtrlEnterRightAfterAQueuedSend_WithNoKeystroke_QueuesNothingAndSendsNothing`. It replaces node 2's `AQueuedSend_ShowsItsQueuedState_NotTheSentState`, whose oracle was the old queued state. | `n3-B-red-mutation.txt` (`65bde9a9`): **mutation red**. M3 is the planted leak: the queued submit keeps its words in the draft (`BeginNextTurn`: `if (_queued is null) { clear }`), so the gate's view holds the shown text. The press right after the queue is refused by Ruling 95. After the drain, a press with no keystroke **resent** b2's bytes as b3: 3 turns, 3 attempt rows (was 2), 3 blocks sent, status "sent". M3 was then reverted. | `n3-B-green.txt` (`db516197`), 3 of 3. The press right after the queue reads *"b2 is queued; cancel it or wait."* (2 turns, 1 queued, attempt rows and blocks sent unchanged). After the drain the press reads *"an empty prompt is not a task"*. |
| C — the class gate (DC-NEW-wl-t28-compiled) | `ComposerContentOraclesDriveTheGestureTests`: `EveryComposerFieldContentOracle_DrivesTheUsersGesture_OrIsNamedPreAdmit` (the tree) and `TheGate_RefusesABareComposerContentAssertion_AndAcceptsAGestureDrivenOne` (the self-test: Ruling 96's original shape, a bare composer with its draft set, and a read behind a helper are refused; a layout-only read and a gesture-driven oracle pass) | `n3-C-red.txt` (`d4995b84`): the planted `PlantedBareComposerContentTests` (a bare composer, its draft set, `CompiledView` asserted) is **REFUSED**. 1 failed. | `n3-C-green.txt` (`23c7c461`), with the plant removed: 194 files, 14 tests read composer field content, 3 are named pre-admit, 0 refused, 0 stale. 2 of 2. |

**The gate's rule.** A test reads composer field content when its body, or a same-file helper it calls, reads one of these: `CompiledView`, `_compiled.Text`, the compiled box's `.Text` (the box itself is found by `IsReadOnly && MaxHeight <= CompiledPromptMaxHeight`), the field's UIA name, or `ComposerGesture.Field` / `Heading`. Such a test must run on a `SessionDocumentSurface` (`ComposerGesture.Drive`, `new SessionDocumentSurface`, `ThreadFixtures.Document`) **and** use a `ComposerGesture` verb (`Type`, `CtrlEnter`, `CtrlEnterThenWait`, `Open`). It must not set the draft (`Draft.Set*`, `SwitchTo`, `UseTemplate`) or `CompiledPromptOpen` by assignment. The only exception is a `PreAdmit` entry with a reason. A measure of the box's height alone is not a content read. That narrowing came from the first run: 5 layout tests in `Sessions/TheWriterKeepsItsRoomTests.cs` reach the box only through the `Layout` helper's `ActualHeight`. The 3 `PreAdmit` entries are the pre-send reads that node 1's sweep ruled out: the ~30-line layout non-vacuity checks in `Composer/` and `Sessions/TheWriterKeepsItsRoomTests`, and the decoration test's read before the send. The list may only shrink, and a stale entry fails.

**Not built, or residual:**
- **Inferred, not observed:** after the queue drains and b2 is sent, the field keeps the *queued as b2* heading until a keystroke. `SetQueued` and `SetInFlight` do not re-render the field. Ruling 181 names no transition from queued to sent, and B's test does not assert the heading after the drain. If one is wanted, it is one `ShowCompiled` call when `_queued` clears to the turn now in flight.
- The gate does not mechanize the non-empty check on an equality oracle (DC-NEW's second shape). It is a source scan, so a content read behind a helper in another file is not seen. DC-NEW-wl-t28-compiled's status is therefore `partially-controlled`.
- **Rule 19 lapse:** the T11 join (`join-wls-kinds-migration`) went in flight at 17:32:18Z. This node's four filtered App runs of the gate test (about 17:33–17:37Z) started during it, because I did not re-read the board before each run. Those facts are source scans that open no window and take no desktop hold. The (A) and (B) runs ended before 17:31Z. The whole App suite waited for the join's end.

## 7. Node 4 - the drain heading (Ruling 181, the Owner's direction)

**What landed:** `ComposerSurface.SetQueued(null, ...)` switches a shown queued state to `SentHeading(b{n})` when the turn now in flight is that same turn (same ordinal text and `SentBytes`) and nothing was typed since (`_sent` is still set). One `ShowCompiled` call; the header, UIA name and ItemStatus move together. If the user typed, `_sent` is already null and nothing changes. A queue that is held (b1 failed or stopped) never reaches `SetQueued(null)`, so the heading stays `queued as b2`.

| Check | Test (`TheCompiledFieldAfterTheQueueDrainsTests`) | Red | Green |
|---|---|---|---|
| A - drain to sent | `WhenTheQueueDrainsAndB2IsSent_WithNoKeystroke_TheHeadingSwitchesToSentAsB2`; `WhenTheQueueDrains_AfterTheUserTyped_TheLiveDraftStays` | `n4-red.txt` (`1a5150a0`): the heading read `queued as b2` after the drain. The typed-since fact passes on the unfixed code by design. | `n4-green.txt` (`ed2a655e`), 14 of 14 with the n3 neighbours |
| B - drain refused | `WhenB1Fails_AndTheDrainIsRefused_TheHeadingStaysQueuedAsB2` (stub `FailAtInitialize`; b2 cancelled at the end so the run tail has no queued launch) | `n4-B-red-mutation.txt`: mutation, a switch to sent whenever the turn in flight ends (`SetInFlight(null)`); the test fails, `sent as b2` where `queued as b2` is true. Reverted. | in `n4-green.txt` |
| C - resend guard after the switch | `AfterTheSwitchToSentAsB2_ACtrlEnterWithNoKeystroke_SendsNothing` | `n4-C-red-mutation.txt`: mutation, the switch writes the sent bytes into the draft; a press resent b2 as b3 (3 turns, expected 2). Reverted. | in `n4-green.txt` |

**Suites:** Core 3046 passed, 0 failed, 1 skipped (`suite-node4-core.txt`, before the App-only change). App 1182 passed, 1 failed, 2 skipped (`suite-node4-app.txt`); the failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, the known Ruling 155 (3) shown-window probe refusal, as at n3.

**Residual:** if b2 finishes before the UI takes a snapshot with b2 in flight, the heading would stay `queued as b2`. The stub run held long enough in the test; not forced otherwise. I did not claim `ComposerSurface.cs` with `coord claim` (the call needed `--wi`); no other track touched it.

## Files

- `reproduction.txt` — the numbers, the observed writer, and Ruling 96's tests' own output.
- `compiled-after-submit-red.txt` — `TheCompiledFieldAfterASubmitTests`, 2 failed (red commit `f3bdf334`).
- `suite-node1-core.txt`, `suite-node1-app.txt`, `suite-node1-exits.txt` — the whole Core and App suites at this node's head, run in this tree.
- `handoff.md` — the state and the next step.
- `A-red.txt` — node 2: the 12 new and changed tests on the unfixed code (red commit `eaf8966c`); 9 failed, 3 passed (`IsOnScreen`, which types and does not submit).
- `B-red.txt` — the planted-leak mutation M1, under which 3 of 3 fail and the second press resends.
- `C-red.txt`, `C-red-mutation.txt` — C on the unfixed code; mutation M2 against the re-driven and the original Ruling 96 test.
- `ABC-green.txt` — the 12 tests on the fix (`5da5d032`): 12 of 12 passed.
- `suite-node2.txt` (`suite-node2-core.txt`, `suite-node2-app.txt`) — the whole Core and App suites at the node head.
- `n3-A-red.txt`, `n3-A-green.txt` — node 3, check A: the queued state, red on the unfixed code (`13b32da6`), green on the fix (`1874cb5e`).
- `n3-B-red-mutation.txt`, `n3-B-green.txt` — check B: planted leak M3, under which the press after the drain resends b2 as b3; then green (`db516197`).
- `n3-C-red.txt`, `n3-C-green.txt` — check C: the class gate refusing the planted bare-composer assertion (`d4995b84`), then green on the tree (`23c7c461`).
- `n3-verify-defect-register.txt` — `tools/verify-defect-register.py` after the DC-NEW entry update: OK.
- `suite-node3.txt` (`suite-node3-core.txt`, `suite-node3-app.txt`) — the whole Core and App suites at the node-3 head.
- No "Ruling 115 desktop hold is occupied" result occurred in node 3, so there is no `desktop-hold.txt`. App: 1177/2/2. The 2 failures were re-run alone in `suite-node3-rerun-app.txt`: `ADeadHolder` passes; the SolutionTreeChord probe is refused, "foreground not held" (Ruling 155 (3)).
- `n4-red.txt`, `n4-green.txt`, `n4-B-red-mutation.txt`, `n4-C-red-mutation.txt`, `suite-node4-core.txt`, `suite-node4-app.txt` - node 4, see section 7.
