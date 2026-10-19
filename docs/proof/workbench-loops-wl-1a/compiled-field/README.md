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
  48 px floor. The verified cause is the submit's own reset of the draft (BeginNextTurn on Admit), not layout. Red test
  committed; the fix waits for the Owner.
---

# T28 — the compiled field is empty after a submit

- **Branch:** `wl1a-compiled-field`, off `origin/main` `624b46c4`. Node 1 (`wl-t28-compiled`), `/investigate`. **Red only. No product code was changed. This node stops for the Owner's review.**
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

## Files

- `reproduction.txt` — the numbers, the observed writer, and Ruling 96's tests' own output.
- `compiled-after-submit-red.txt` — `TheCompiledFieldAfterASubmitTests`, 2 failed (red commit `f3bdf334`).
- `suite-node1-core.txt`, `suite-node1-app.txt`, `suite-node1-exits.txt` — the whole Core and App suites at this node's head, run in this tree.
- `handoff.md` — the state and the next step.
