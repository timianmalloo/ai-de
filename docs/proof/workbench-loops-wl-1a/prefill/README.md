---
id: proof-workbench-loops-wl-1a-prefill
title: "Evidence: WL-1a T30 — goal and done criteria suggested by the compile step by default (Ruling 180 (1)-(3))"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, evidence, implement, composer, compile-mode, ruling-180, ac-w01a]
links:
  - { to: architecture, rel: implements }
  - { to: note-wl-dr-prefill-goal-done, rel: relates-to }
review-by: "2026-11-19"
summary: >-
  T30 node 1. (A) A new session opens agentic-advisory when Gate 1 admits it, mechanical-only otherwise; never agentic.
  (B) Session settings expose the compile mode as a keyboard combo box through SetCompileMode and the gate, a refused
  rung disabled with the gate's reason, and a line stating the mode in force and why. (C) Keep writes the suggested line
  into the draft, so a re-prepare never re-derives it. Each check red then green through the gesture, with a stubbed
  compile host. The product wiring of (A) and (B) waits on one seam request (two lines outside the owned paths).
---

# T30 n1 — the compile step suggests the structure by default (Ruling 180 (1)-(3))

**Result:** all three checks are green, each observed red first. No test calls a model: the compile host is a stub.
The product does **not** yet reach (A) and (B) on File → New Session and the real settings, because two one-line edits
outside the owned paths wait on seam request `req-01M5ATGTTQS78NG5W7ZMZE8G00` (see *Not done*).

**Residual (Ruling 182 (3)):** until WL-2's draft store (ADR-0043), **unsent typing and kept suggestions are lost when
the app restarts.** A revived notebook's composer starts empty (`ComposerSurface.cs:96`, measured by T30 n2 at
`8afa3a58`). Nothing in the composer says otherwise (node 3 (A)). Whatever was **sent** survives: each sent turn's
compiled prompt, including its goal block, is on the turn row's disclosure after a restart (Rulings 163, 181). Node 3 (B)
proves it; before node 3 the restored row's disclosure was empty.

## Checks

| Check | Ruling | Red | Green | Tests |
|---|---|---|---|---|
| **A** default | 180 (1) | `b30656da` · `A-default-red.txt` (2 of 4 fail: `Expected agentic-advisory, Actual mechanical-only`) | `643fd8d2` · `A-default-green.txt` (37 pass with the neighbouring session tests) | `ANewSessionOpensInAdvisoryWhenGateOneAdmitsTests` (4, Core) |
| **B** control | 180 (1)(2), C3 | `9cc6200d` · `B-control-red.txt` (*the open session settings carry no control named "Compile mode"*) | `d21ff548` · `B-control-green.txt`; suite fix `da4a1066` · `B-control-suite-fix.txt` | `TheCompileModeControlTests` (2, App) |
| **C** kept line | 180 (3), C1 | `717b03c3`, re-red on the binder's goal-block form `253d3970` · `C-kept-line-red.txt` (Goal re-derived: *"Suggestion 2 for goal." marked "? derived"; draft goal ""*) | `a3699749` · `C-kept-line-green.txt` (*"Suggestion 1 for goal." marked "✓ kept"; draft goal "Suggestion 1 for goal."*) | `AKeptSuggestedLineIsKeptTests` (2, App) |

Copy and DESIGN.md rows: `9003d8e8` (`C-keep-help.txt`). Whole suites (`suite-node1.txt`): **Core 3119 passed, 0
failed, 1 skipped; App 1225 passed, 2 failed, 2 skipped.** Both App failures say the foreground was not held
(`SolutionTreeChordTests`, the Ruling 155 (3) probe; `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests`). They were
re-run after 2 min (20:05:37Z) and failed the same way. Neither test touches this track's code. `docs-graph.py validate`:
0 problems, index drift only (`docs-graph-validate.txt`).

## What each test drives

- **A** (`tests/AiDe.Core.Tests/Sessions/ANewSessionOpensInAdvisoryWhenGateOneAdmitsTests.cs`): `SessionConfigStore.Create`
  with the gate's input injected. Admitting: a `CompileModeAvailability` built directly (Gate 2 refused). Refusing:
  `CompileModeGate.Evaluate` over an empty temp proof directory, so the refusal is the gate's own `CE-0016` text. Also:
  every rung admitted still defaults to `agentic-advisory`, never `agentic`; a null ladder writes `mechanical-only`. No
  test reads `~/.aide/proof`.
- **B** (`tests/AiDe.App.Tests/Workbench/Composer/TheCompileModeControlTests.cs`): the product composition
  (`ComposerGesture.Drive`); the header's *Session settings* button through its invoke peer; the combo box named
  *Compile mode* (UIA role ComboBox, focusable); **Up** and **Down** keys routed to it. The admitting case selects
  mechanical-only and back, and reads the stored `session.json` and the gate after each. The refusing case shows the
  gate's reason on the line and on the disabled item, and Down cannot select the disabled rung. Measured lines are in
  `B-control-green.txt`.
- **C** (`tests/AiDe.App.Tests/Workbench/Composer/AKeptSuggestedLineIsKeptTests.cs`): type through the editor's input
  verb, Ctrl+Enter (prepares under advisory), Keep through the *Keep Goal* button's invoke peer, type again, Ctrl+Enter
  (re-prepares). The stub suggests a different value on each call, so a re-derived line is visible. Second test: an unkept
  suggestion never reaches the draft, so `Envelope.Confirmed` and `projection_sha` never see it.

## Code

- `src/AiDe.Core/Sessions/SessionConfig.cs` — `DefaultCompileModeFor(CompileModeAvailability?)`; the record value stays
  mechanical-only for legacy files and callers that cannot read the ladder.
- `src/AiDe.Core/Sessions/SessionConfigStore.cs` — `Create(..., compileModes)` writes that default.
- `src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs` — the settings row: combo box, reason line,
  `ChangeCompileMode` through `SetCompileMode`, then `Composer.Gate.BindSession` for new envelopes; `CompileModeLadder`
  is the gate-input seam (null: only mechanical-only is offered, said by name).
- `src/AiDe.App/Workbench/Composer/ComposerSurface.cs` — the keep callback writes the draft (C1); `StructureLine.KeepHelp`.

## Not done

1. **Product wiring of (A) and (B)** — seam request `req-01M5ATGTTQS78NG5W7ZMZE8G00`, narrowed in mails
   `mail-01M5ATNS8ZAD1N8EWQJVHCM5G5` and `mail-01M5AV7EF4XJS6C3XX232CW9CX`; no reply before the kick. Needed:
   (i) `NewSessionSheetViewModel.Create` (`:556`) passes `compileModes: CompileModeGate.Evaluate(_adapterInstallRoot)`;
   (iii) `ComposerSurface` exposes `AdapterInstallRoot => _context?.AdapterInstallRoot`, so `CompileModeLadder` gets a
   product default. Until then New Session opens mechanical-only and the real settings offer mechanical-only only.
2. **"Survives a restart" (C1)** — not proved. The App does not persist the composer draft across a restart:
   `ComposerDraftStore` (Core) has no caller in `src/AiDe.App` (grep, 2026-10-19). The kept line is in the draft, so it
   will survive once the draft does. The re-prepare half of C1 is proved.
3. The visible mark stays DESIGN.md's *? derived*; the word *suggested* is on Keep's description (UIA help) only.

## Findings

- `ComposerGesture.ConfigureAsTheBinderDoes` (T28's helper) leaves the draft on the free-form shape, but
  `SessionComposerBinder.Bind` switches it to the goal-block form. Under free-form, `PreCompile` ignores goal values, so
  my first red passed for a fixture reason; the tests here switch the shape as the binder does. Not edited (not mine).
- **DC-NEW-wl-t30-prefill** (`docs/lessons/defect-classes.md`): `PresentationSource.CurrentSources` is process-wide;
  the B tests passed alone and failed in the whole suite until filtered to this thread.

## Node 2 — the default reaches the user; the mark reads "suggested"; the window fixture

**Result:** (A), (B) and (C) green, each observed red first; (0) measured and stopped as the brief says. No test calls a
model and no node-2 test reads `~/.aide/proof`.

| Item | Red | Green | Tests |
|---|---|---|---|
| **(0)** C1 restart, measured | — | `8afa3a58` · `c1-restart-measure.txt`, `c1-restart-measure-run.txt` | `AKeptSuggestedLineIsKeptTests.AKeptGoal_IsInNeitherTheStoredRevisionNorTheRevivedDraft_AfterARestartThroughTheShellsRevive` (pins the measured fact) |
| **(A)** settings over the bound adapter | `63843967` · `A-settings-red.txt` (gate asked about `[]`, advisory disabled) | `2cf4172e` · `A-settings-green.txt` | `TheCompileModeControlTests.WithNoLadderInjected_TheSettingsEvaluateTheGateOverTheBoundAdapter_AndOfferAgenticAdvisory` |
| **(A)** File → New Session | `b26f08a2` · `A-new-session-red.txt` (asked `[]`; stored `mechanical-only`) | `7748a0ca` · `A-new-session-green.txt` (37 with the sheet and front-door tests) | `FileNewSessionOpensInTheGatesDefaultTests` (2) |
| **(B)** "suggested" | `4dfb5785` · `B-suggested-red.txt` (mark `? derived`, UIA `derived`) | `138a9419`, rename `f2b382fa` · `B-suggested-green.txt` (mark `? suggested`, UIA `suggested`; kept: `✓ kept`, UIA `kept`) | `AKeptSuggestedLineIsKeptTests.AModelDerivedLine_IsMarkedSuggested_OnScreenAndInUia_AndAKeptLineIsNot` |
| **(C)** own-window fixture | `e230377e` · `C-own-window-red.txt` (read *a concurrent window*'s control) | `f664eddb` · `C-own-window-green.txt` | `TheCompileModeControlTests.TheSettingsQuery_ReadsThisTestsOwnWindow_WithConcurrentWindowsOpen` |

Whole suites (`suite-node2.txt`): **Core 3119 passed, 0 failed, 1 skipped; App 1230 passed, 3 failed, 2 skipped.**
Re-run after 2 min (20:59:34Z): `TheSentPromptIsNeverResentTests` passed (its suite failure was *the composer editor
could not start*); `SolutionTreeChordTests` refused (the Ruling 115 desktop hold was held by the Leader's probe);
`CtrlDigitSwitchesThePerspectiveFromInsideAPageTests` failed, foreground not held. The last two are node 1's two.

**Code:** `NewSessionSheetViewModel.CompileModeGateInput` and the `compileModes:` argument (`Create`); `ComposerSurface`
— `AdapterInstallRoot` getter, `StructureLine.ModelLineMark` (`? suggested`); `SessionDocumentSurface.CompileModeGateInput`,
used when no ladder is injected; `tests/AiDe.App.Tests/OwnWindow.cs`. DESIGN.md: the *Derived structure* row now
defines the mark as the `?` glyph + the word *suggested* (Ruling 180 (3)). `ComposerGesture.cs` was not changed.

## Node 3 — nothing says the draft is saved; what was sent survives; the row states the gate's real reason (Ruling 182)

**Result:** (A), (B) and (C) green, each observed red first. (A)'s red is a **mutation** red, because no saved-state copy exists on the tree. (B)'s red is a **real** red: the restored row's disclosure was empty. No test calls a model, and no node-3 test reads `~/.aide/proof`.

| Check | Ruling | Red | Green | Tests |
|---|---|---|---|---|
| **A** no saved-state copy | 182 (2) | `758fc894` · `A-no-saved-copy-red.txt` (**mutation**: *Kept for later.* planted in `StructureLine.KeepHelp`, not committed; on the tree the same test passed, in `C-gate-reason-red.txt`) | `0599a4da` · `A-no-saved-copy-green.txt` (313 strings across 6 moments, none refused) | `NothingSaysAnUnsentDraftIsSavedTests` (1) |
| **B** a sent goal line survives a restart | 182 (4); 163, 181 | `19437c6e` · `B-sent-survives-red.txt` (**real**: restored row's SentBytes 0 chars, disclosure `""`; 263 chars were sent) | `9234fc77` · `B-sent-survives-green.txt` (disclosure 263 chars, equal to what was sent; 35 with the census and revive tests); **mutation** red `B-byte-change-mutation-red.txt` (one byte changed after the rebuild: the row reads *not recorded*) | `ASentGoalLineSurvivesARestartTests` (2) |
| **C** the gate's real reason | 180 (1), C3 | `7f8d6904` · `C-gate-reason-red.txt` (stored advisory, gate refusing: *"Gate 1 admits it"*) | `468b2566` · `C-gate-reason-green.txt` (5 pass) | `TheCompileModeControlTests.ASessionStoredAdvisory_WhenTheGateNowRefusesIt_TheRowStatesTheRefusal_AndClaimsNoAdmission` |

**What each test drives.** **A:** type, Ctrl+Enter (prepare under advisory), Keep Goal through its invoke peer, then *Session settings*. At each step it reads every product string: text, content, headers, tool tips, UIA name, help text and item status, the status line, the marks and the field descriptors. It also reads the composer page markup (`composer.html`, `composer-host.html`), and it refuses *saved*, *for later*, *persist*, *stored*, *survives* and the like. Text boxes hold the user's or the model's words and are not read. **B:** type, prepare, keep all three lines, send; then the shell over a real `WorkspaceClient` to a daemon revives the pane (`ReviveRestoredSessionDocuments`), and the restored row's disclosure is opened through its header toggle and read. **C:** a session created while Gate 1 admits, then the gate refuses; the settings row is opened through its button.

**Code (seams `req-01M5B3NHP0143W53J11CRKPVAX` and `req-01M5B3TTNQXVZ3C8AEHXNV6G2Y`, granted with conditions):** `ComposerSendGate.SentBytesOf` rebuilds the bytes through the existing `ProjectForSend`, so the census stays at 2 `Projection.Project` calls in that file. It returns them only when their sha256 equals the accepted `submitted.text_sha256`. `SessionDocumentSurface.SubmittedEnvelopes` yields them, and `NotebookRuns.ReadBackAsync` and `RestoreAsync` carry them to `Thread.Accept`, or `NotebookRuns.CompiledPromptNotRecorded` when they do not verify. The read-back tuple's type changed with them (`_submitted` and `ReadBackOnEachBind`). There is no store or schema change. A turn whose bytes carried attachment bodies or a template does not rebuild, so its row reads *not recorded*. `SessionDocumentSurface.CompileModeWhy`: a rung held but now refused states the refusal. DESIGN.md: the restored-turn row and the demoted-rung copy.

**Residual line:** added at the top of this file, in the same words as the WL-1a Proof Pack's (`aa8247ff`).
