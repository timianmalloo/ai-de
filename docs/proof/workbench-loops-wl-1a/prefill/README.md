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

## Node 4 — the demo script, the cost line, compile-eval (Ruling 180 (4), (6))

**Result:** (0) the demo script is below. (A) the cost line is green, observed red first. (A)'s first-50 recorder is
**not done**: its seam was granted (`req-01M5B9DJWPJBEBCTP2GFJCZVRR`) after the rule-16 kick, so it was not started.
(B) ran the eval's self-tests; the real-corpus run is **NOT RUN**, with the reason and the exact command below. No test
calls a model.

| Item | Red | Green | Tests / output |
|---|---|---|---|
| **(0)** demo script | — | `012edb30` | *Human demo script — the prefill*, below, with its gesture table |
| **(A)** every advisory call's spend reaches the outcome line | `e04fcb02` · `A-cost-line-red.txt` (*b1's outcome line reads "3,406 tokens", not 4,606*: the first call's 1,200 tokens were on no line) | `51d70fa7` · `A-cost-line-green.txt` (4,606 tokens; header *"4,606 tokens this session"*; 14 pass with the spend, kept-line and resend tests) | `EveryAdvisoryCallsSpendReachesTheOutcomeLineTests` (1, App) |
| **(A)** first 50 advisory calls' cost and latency | not started | — | NOT DONE; design granted, in `handoff.md` |
| **(B)** compile-eval | — | `B-compile-eval-run.txt`: `score.py --self-test` 3/3 PASS, `ring.py --self-test` 8/8 PASS | real corpus NOT RUN (below) |

**(A), what changed.** `ComposerSendGate` sums every compile call with at least one request since the last send,
including a call whose envelope a re-prepare abandoned while it ran, and the submission carries that sum to the turn.
Before, it carried only the last envelope's `called.cost`. Defect class `DC-NEW-wl-t30-prefill-c` (`77cf5cd4`). A call
whose wire reports no cost still adds nothing; it is not marked *not recorded* on the line (residual).

**(B), the real corpus: NOT RUN.** This tree has no `envelope-events.jsonl`. The machine's corpus is in the workspaces
the user opened, which are other trees (brief rule 2). `~/.aide/proof` holds no `compile-eval-admission.json`, so Gate 2
was never scored here. Inferred, not counted: fewer than 100 advisory envelopes exist, since the default reached New
Session only at `d986e4a4`. The command, with `--out` kept away from `~/.aide/proof` so a read never admits Gate 2, is
in `B-compile-eval-run.txt`.

### The AI Systems Engineer's conditions (`docs/notes/wl-dr-prefill-goal-done.md` §4), checked

- [x] **C1, re-prepare half.** Keep writes the draft, so a kept line survives the next prepare (node 1 (C),
      `AKeptSuggestedLineIsKeptTests`).
- [ ] **C1, restart half.** Not met, by ruling: Ruling 182 (1) moves it to WL-2's draft store. The residual is stated at
      the top of this file and in demo step 21.
- [ ] **C2.** Currentness reads `projection_sha` from stored rows. Not T30's: the Leader moved it to T26 check D2
      (`req-01M5AEKWQQYP4YK499Y5DN6GPD`, 16:20Z).
- [x] **C3.** The settings row shows the real mode, the gate's reason and help that matches the mode (node 1 (B), node 2
      (A), node 3 (C); `TheCompileModeControlTests`).
- [x] **C4.** The first 50 advisory `called` rows read for cost and latency: the reader is built (node 5 (B)) and its
      self-test passes. The command, run over the user's session directories (never this tree, which holds no
      `envelope-events.jsonl`), is
      `python tools/compile-eval/first-calls.py "<workspace>/.aide/sessions" --out "<a file outside ~/.aide/proof>"`.
      The per-turn spend reaches the outcome line for every call (node 4 (A)). **Not run on the real corpus here**, for
      node 4's reason (`B-compile-eval-run.txt`): the recording is owed at the next desk run with real sessions.
- [x] **Determinism under advisory.** Only operator bytes enter `projection_sha`: an unkept suggestion never reaches the
      draft (node 1 (C), `ASuggestedLineThatIsNotKept_NeverReachesTheDraft`).
- [x] **Model output can change access, kept behind a human gate.** Keep under advisory. `agentic` stays refused until
      Gate 2 admits it (`CompileModeGate.cs:73-79`), and no admission report exists on this machine (listing above).
- [x] **The eval harness exists and runs.** `score.py` and `ring.py` self-tests pass (`B-compile-eval-run.txt`).
- [ ] **Residual: confabulated tool XML.** Read, not tested: the validator parses from the first `{` to the last `}`
      and ignores the text around it (`CompileOutputValidator.cs:54`, `:174-179`). XML with no braces next to a valid
      object is ignored, not marked malformed. Nothing executes text, and `tool_calls` is counted from the wire, so a
      suggestion still needs Keep. No floor counts this case.

## Node 5 — the suggested lines are seen; the first-50 reader; the sheet tests' gate (Ruling 180 (3), (4))

**Result:** (A), (B) and (C) are each green, observed red first. No test calls a model.

| Item | Red | Green | Tests / output |
|---|---|---|---|
| **(A)** a prepare that yields suggested lines shows them (the Owner's shape) | `2eba9709` · `A-shows-suggested-red.txt` (*a prepare that suggested lines left them in the closed fold*) | `dc408cac` · `A-shows-suggested-green.txt` (26/26 with the suggested-line, prepare-walk, degrade, saved-copy and restart neighbours) | `APrepareShowsItsSuggestedLinesTests` (1, App) |
| **(B)** `tools/compile-eval/first-calls.py`, the first 50 advisory calls' cost and latency (C4; seam `req-01M5B9DJWPJBEBCTP2GFJCZVRR`) | `8b57a205` · `B-first-calls-red.txt` (a naive window counted 3 reused rows and read absent cost as 0) | `ed24b2e8` · `B-first-calls-green.txt` (`SELF-TEST PASS`; `score.py` and `ring.py` self-tests still pass, both unchanged) | `--self-test` |
| **(C)** the four sheet test files inject the compile-mode gate | `f2630c47` · `C-sheet-gate-red.txt` (10 sites over an adapter root, 9 with the real gate) | `a39dcefa` · `C-sheet-gate-green.txt` (10 sites, 0 bare; 37/37 with the four files) | `NoSheetTestReadsTheRealProofDirectoryTests` (1, App) |

**(A), what changed.** `PrepareTurnAsync` counts the lines it fills. When there is at least one, it opens the fold and
announces *"{n} suggested lines. Review them before you send."* once, as a status, before *"prepared — press again to
send"*. Nothing on that path moves focus. A suggested line's UIA Name is `? suggested {label}`, and its keep button and
ItemStatus are as before. The test drives type and Ctrl+Enter on the product's composition, checks that focus is in the
editor before and after (WPF focus on the editor's host, or Win32 focus inside its window), counts the announcements,
and walks Tab from the editor through the page's `focus.leave` verb, then WPF's Tab traversal. DESIGN.md's *Derived
structure* row names the string, the name and the rule that only the user closes the fold. Registered as a
**DC-NEW-wl-t28-compiled recurrence** in `docs/lessons/defect-classes.md`, in the fix commit.

**(B), what it reads.** The treatment arm exactly as `score.py:176-177` defines it. Each envelope's `called` rows in seq
order, `reused` rows skipped, the first 50 taken. It reports n, n with cost, n with latency, tokens in+cache and out
(sum, p50, p95), latency p50/p95, outcomes by count, and the envelope ids. A figure the wire did not report is left out
and counted as not measured. With none reported it reads *not recorded*, never 0. It imports `read_rows`, `fold` and
`percentile` from `score.py`. The self-test fixture is 54 advisory `called` rows (51 model calls, 3 reused), 2 model
calls with no cost, and one mechanical-only control. The self-test writes its fixture to a throwaway temp directory
that it deletes.

**(C), what the guard reads.** Every `.cs` under `tests/AiDe.App.Tests` and `tests/AiDe.Core.Tests`. Each sheet built
with a named `adapterInstallRoot:` that is not `null` must set `CompileModeGateInput` in its initializer, and so must
each `NewSessionFlow` with an adapter root. A `Reload(registry, root)` with a root needs every sheet in its file to
carry the injection. The injected gate is `CompileModeGate.Evaluate(root, <root>/proof)`, a directory with no pin
artifact, so Gate 1 refuses the same way on every machine. Limit: an adapter root passed by position is not seen.

**A slip, and an evasion (`0528992b`; undone in node 6).** The (A) green `dc408cac` named the announcement `SuggestedLinesAnnouncement`. The whole
Core suite then failed one test, `ThePrivacyResidualsAreCoveredTests.NoAssistPathExistsAnywhereInTheComposer`, which
refuses the substring `Suggest` in the composer's sources (`suite-core-node5.txt`). The member was renamed
`ReviewLinesAnnouncement`. That rename evaded the gate (DC-NEW-wl-t30-gate-evasion): node 6 renamed it back
and answered the gate (§ Node 6).

**Whole suites (board checked before each App run; no demo, no join in flight):**
- **Core:** at `dc408cac`-plus-(B)-(C) (head `a39dcefa`): 3118 passed, **1 failed** (the slip above), 1 skipped
  (`suite-core-node5.txt`). At head **`0528992b`**: **3119 passed, 0 failed, 1 skipped** (`suite-core-node5-rerun.txt`).
- **App:** at head `a39dcefa`: **1245 passed, 3 failed, 2 skipped** of 1250 (`suite-app-node5.txt`). At head
  **`0528992b`**, (A), its five neighbour classes, the guard and the three failures re-run: **31 passed, 3 failed**
  (`suite-app-node5-rerun.txt`). The three are node 4's:
  - `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests` (*foreground held=False*) and `SolutionTreeChordTests`
    (*foreground not held*): **owed desk runs**, not failures.
  - `AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`
    (*Space on the restored row's fold did not open it*): it failed the same way at node 4 with the branch's only src
    change reverted to main's copy (`suite-app-node4-baseline.txt`). The join's recount decides.

## Human demo script — the prefill

*Written by T30 node 4 (`wl-t30-prefill`) for the Owner, at `71498c77`. Keyboard only. Every gesture, label and string
below was read from the code at that commit; the gesture table under the script gives file:line for each one. Lines
marked **rehearse** are gestures or strings the code does not fix (a Tab count, a model's words), so check them on the
first run. Every GUI step is **owed to the user** until a person runs it.*

### 0. When and where
1. Run this **after** the WL-S walk-through ends (the board's `USER DEMO DONE`). Use a **fresh, clean worktree of
   origin/main at `d986e4a4` or later**, never the primary checkout (`C:\Projects\ai-de`). Build it in Release:
   `dotnet build AiDe.sln -c Release`, then start `src/AiDe.App/bin/Release/net10.0-windows/AiDe.App.exe`.
2. Gate 1 reads `%USERPROFILE%\.aide\proof\compile-pin-spike.json` and the adapter under the providers file's adapter
   root, not the worktree. A fresh worktree therefore needs no `npm ci` for this script.

### 1. A new session opens in agentic-advisory, and the settings row says why
3. **Ctrl+N** opens *New session…* (a workspace chooser comes first if none is open). Tick an Anthropic account, reach
   its *Default for this session* radio (UIA "Default account: {label}") and press **Space**, then **Enter** (*Create
   session*). As in WL-1a step 4.
4. Reach the session header's **Session settings** button (**rehearse** the Tab count from the editor) and press
   **Space**. In the popup, reach the combo box named **Compile mode**.
5. **Expect:** the combo box reads `agentic-advisory`. The line under it reads *"Agentic-advisory: the compile step
   suggests Goal, Done when and Not in scope; each counts only once you keep or edit it. Gate 1 admits it: the
   compile-pin spike matches the installed adapter."* The combo box's UIA HelpText is the same sentence. Open the list
   (**Alt+Down**): the third item reads `agentic — not available`, disabled, and its HelpText is Gate 2's refusal.
   If the line instead says *"This session is set to agentic-advisory, but the gate now refuses it: …"* or
   *"Agentic-advisory is not available: …"*, Gate 1 refused on this machine: record the sentence and stop here.
6. Close the popup (**Esc**, **rehearse**) and return to the editor.

### 2. One prepare, one model call
7. Type a prompt that names a goal and a finish line, e.g. *"Add a unit test for the parser's empty-input case. Done
   when the test fails on the current code. Do not change the parser."*
8. **The next step spends real model usage on your Claude account: one compile call.** It is a toolless call on the
   bound engine and account, bounded at 60 s. The script makes **no other compile call**: step 13's prepare has no open
   line, so it skips the call. Step 15's send then runs the turn on the real lane, like every WL-1a turn.
9. Press **Ctrl+Enter** once. **Expect:** the status line reads *"Preparing… — the model is reading your draft"* and
   the compile line reads *"Preparing…"* with a **Cancel** control. A second Ctrl+Enter now is ignored with *"Preparing…
   — press again when prepared"*. Nothing is sent: the thread has no new row.
10. **Expect, when the call returns:** the status line reads *"prepared — press again to send"*. The compile line reads
    *"Compiled on {model} · {family} · read 0 turns · {tier rationale}"* (the model and rationale words: **rehearse**).
    If the call degraded, the line reads *"compiled mechanically — {reason}"* with **Prepare again**. That is a correct
    result; record it and continue with the lines left blank.

### 3. The suggested lines: edit one, keep the others, read Keep's help
11. **See:** the prepare opened the *"Goal · Done when · Not in scope"* fold by itself (node 5 (A)), and the cursor is
    still in the editor. A screen reader says *"3 suggested lines. Review them before you send."* once, then
    *"prepared — press again to send"* (the count is the number of lines the model proposed). Each proposed line shows
    its value with the mark **`? suggested`** and a **keep** button. Press **Tab** from the editor (**rehearse** the
    count): the stops include each line, named **"? suggested Goal"**, **"? suggested Done when"** and **"? suggested
    Not in scope"** in UIA, and the line's ItemStatus reads `suggested`, without the glyph. The fold stays open; only
    you close it.
12. **Tab** to the **Keep Goal** button. In Accessibility Insights (or Narrator), read its HelpText. **Expect, exactly:**
    *"Keep the suggested line as written. A suggestion counts only once you keep or edit it; a kept line stays in your
    draft until you send it. It is not yet kept when AI-DE restarts."* Press **Space**. **Expect:** the Goal mark reads
    **`✓ kept`** (ItemStatus `kept`), and the keep button is gone. Then **Tab** into the *Done when* line, add a word to
    it, and see its mark read **`✓ edited`**. Keep *Not in scope* the same way as Goal (**Keep Not in scope**). A
    goal block cannot be sent with a blank *Not in scope*.
13. Return to the editor and add one word to the prompt. Press **Ctrl+Enter** once. **Expect:** it prepares again with
    **no model call**: the compile line reads *"structure supplied by you"* (**rehearse**: this is the operator-source
    string; the code reads the source from the projection). **The kept Goal line is still there, still `✓ kept`**, and
    so are the other two. Before node 1 (C) this prepare asked the model again and the Goal came back as a new
    suggestion.
14. Open the composer's **Compiled prompt** disclosure (UIA "Compiled prompt"; **rehearse** the reach). **Expect:** the
    goal block in it carries your three lines, the kept and the edited ones.

### 4. Send, and switch to mechanical-only
15. Press **Ctrl+Enter**. **Expect:** turn b1 joins the thread and runs on the real lane. When it ends, its outcome line
    counts the compile call's tokens with the run's (Ruling 78; node 4 (A) makes every compile call of the turn count,
    not only the last).
16. Open **Session settings** again (step 4). In **Compile mode**, press **Up** to choose `mechanical-only`. **Expect:**
    a screen reader announces *"Compile mode is now mechanical-only for new prompts."*. The line reads *"Mechanical-only:
    one gesture, you fill the structure. Agentic-advisory is available; choose it to have the compile step suggest the
    lines."*. Close the popup.
17. Type a short prompt and press **Ctrl+Enter** once. **Expect:** it sends on one press, with no prepare and no compile
    line, as turn b2. Then type some words in the editor and **do not send them**.

### 5. Restart: what was sent survives; what was not sent does not
18. Close the app (**Alt+F4**) and start it again (step 1). Reopen the session: **Alt+F**, **S** (*Recent sessions*),
    arrow to the session, **Enter**.
19. Reach b1's row (**Shift+F6** or **Shift+Tab** from the composer; **rehearse**), **Tab** to its *compiled prompt*
    disclosure (UIA "Compiled prompt of b1") and press **Space**. **Expect:** the bytes that were sent, rebuilt from the
    stored envelope and shown only because their sha256 equals the one recorded at the send. The goal block is in them,
    with the Goal line you kept. b2's disclosure shows its bytes the same way.
20. **The not-recorded case cannot be produced from the keyboard on this build.** A turn rebuilds only when its bytes
    carried no attachment body and no template. Neither can reach a turn in this demo: the binder passes no template
    catalog, so *Start from template* stays collapsed, and *Attach file…* is off by default with no control that turns
    it on. Such a turn's row would read *"Compiled prompt not recorded: this turn's sent bytes could not be rebuilt and
    verified after the restart."*. The test `ASentGoalLineSurvivesARestartTests` proves that string through a one-byte
    mutation (`B-byte-change-mutation-red.txt`).
21. **Expect, said plainly (Ruling 182 (3)):** the composer is empty. **The words you typed but did not send are gone,
    and so are any kept suggestions of an unsent prompt. They are lost on every restart until WL-2's draft store.**
    Nothing on the composer ever says they were saved (node 3 (A)).

### Findings for the Owner (read from code, not run)
1. **Fixed in node 5 (A): the suggested lines arrived inside a folded section.** At `71498c77` the structure fold was
   collapsed at rest and a prepare only filled the lines, so the user saw *"prepared — press again to send"* with no
   suggestion. The Owner ruled the shape (brief n5 (A)): a prepare that yields suggested lines opens the fold, keeps
   focus in the editor, announces the count once, and each line is a Tab stop named `? suggested …`
   (`ComposerSurface.cs:831-848`, `:2458`). An unkept line still does not count at the second press.
2. **The not-recorded restored row is unreachable through the UI today** (step 20). It becomes reachable when a template
   catalog or an attach control is wired.

### Gesture table (read at `71498c77`)

| Step | Gesture or string | Source |
|---|---|---|
| 1 | Release output path `bin/Release/net10.0-windows` | `src/AiDe.App/AiDe.App.csproj:5` (TargetFramework); path as WL-1a step 2 |
| 2 | Gate 1 reads `<proof dir>/compile-pin-spike.json` and the installed adapter | `src/AiDe.Core/Sessions/CompileModeGate.cs:52-53`, `:61`, `:103-113`; adapter root from the providers file: `src/AiDe.App/Workbench/Sessions/SessionComposerBinder.cs:156`; default `~/.aide/adapters`: `src/AiDe.Core/AgentPlane/FirstUse.cs:31-35` |
| 3 | **Ctrl+N** *New session…* | `src/AiDe.Core/Workbench/WorkbenchCommands.cs:98` |
| 3 | *Default for this session*, UIA "Default account: {label}", *Create session* | `src/AiDe.App/Workbench/Sessions/NewSessionSheetDialog.cs:388`, `:394`, `:189` |
| 3 | A new session's mode is the gate's default | `src/AiDe.Core/Sessions/SessionConfig.cs` `DefaultCompileModeFor` (node 1 (A)); `NewSessionSheetViewModel.CompileModeGateInput` (node 2 (A)) |
| 4 | Header button **Session settings**, a popup toggled by its click | `src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs:1221-1223` |
| 4 | Combo box named **Compile mode** | `SessionDocumentSurface.cs:1525-1528` |
| 5 | Items in ladder order: `mechanical-only`, `agentic-advisory`, `agentic` | `src/AiDe.Core/Sessions/CompileModeGate.cs:49`; `SessionDocumentSurface.cs:1539-1545` |
| 5 | `{mode} — not available`, disabled, HelpText = refusal | `SessionDocumentSurface.cs:1542-1543` |
| 5 | The advisory line's two sentences; the HelpText equals the line | `SessionDocumentSurface.cs:1397`, `:1418`, `:1548-1549` |
| 5 | Demoted: *"This session is set to {mode}, but the gate now refuses it: …"*; refused: *"Agentic-advisory is not available: …"* | `SessionDocumentSurface.cs:1413`, `:1427` |
| 8 | Toolless call on the bound engine and account; bound 60 s | `src/AiDe.App/Workbench/Composer/ComposerSendGate.cs:148`, `:650-651`; `src/AiDe.App/Conductor/CompileCallHost.cs:13-22`; `src/AiDe.Core/Compilation/PreCompile.cs:63` |
| 8, 13 | No call when no line is open | `ComposerSendGate.cs:605-621` |
| 9 | First Ctrl+Enter prepares under an agentic rung | `src/AiDe.App/Workbench/Composer/ComposerSurface.cs:660-670`; Ctrl+Enter = `ComposerSendGate.cs:909-917` |
| 9 | *"Preparing… — the model is reading your draft"*; compile line *"Preparing…"* with Cancel | `ComposerSurface.cs:809-810`, `:852` |
| 9 | Ctrl+Enter while preparing: *"Preparing… — press again when prepared"* | `ComposerSurface.cs:662-666`; `ComposerSendGate.cs:512` |
| 10 | *"prepared — press again to send"* | `ComposerSurface.cs:842-844` |
| 10 | *"Compiled on {model} · {family} · read {k} turns · {rationale}"*, k = 0 here | `src/AiDe.Core/Compilation/CompileLine.cs:49`; `ComposerSendGate.cs:739` |
| 10 | Degraded: *"compiled mechanically — {reason}"* with Prepare again | `CompileLine.cs:53-55`; `ComposerSurface.cs:853` |
| 11 | Fold header *"Goal · Done when · Not in scope"*, UIA "Goal, Done when, Not in scope", collapsed at rest | `ComposerSurface.cs:1663-1672` (at `a39dcefa`) |
| 11 | A prepare with suggested lines opens the fold and announces *"{n} suggested lines. Review them before you send."* once; focus is not moved | `ComposerSurface.cs:831-848`, `:859-860` (`SuggestedLinesAnnouncement`); `APrepareShowsItsSuggestedLinesTests` |
| 11 | A suggested line's UIA Name is `? suggested {label}` | `ComposerSurface.cs:2458` (at `a39dcefa`) |
| 11 | Mark `? suggested`; UIA ItemStatus without the glyph; keep button shown only then | `ComposerSurface.cs:2279`, `:2432`, `:2438-2439` |
| 12 | Button content *keep*, UIA Name "Keep {Goal / Done when / Not in scope}", HelpText = KeepHelp | `ComposerSurface.cs:2313-2315`, `:2442-2448` |
| 12 | KeepHelp, exact | `ComposerSurface.cs:2276` |
| 12 | `✓ kept`; `✓ edited` | `ComposerSurface.cs:2431`, `:2433` |
| 12 | Keep writes the draft | `ComposerSurface.cs:1628-1640` |
| 12 | A blank Not in scope refuses a goal block | `ComposerSendGate.cs:411-420` |
| 13 | An edit to the prompt makes the envelope stale; the next press prepares | `ComposerSendGate.cs:524-525`, `:802`; `ComposerSurface.cs:660` |
| 13 | *"structure supplied by you"* when the call is skipped | `CompileLine.cs:29-33`; `ComposerSendGate.cs:607`, `:615` |
| 14 | Disclosure UIA "Compiled prompt", HelpText *"exactly the bytes that will be sent; no diff, no comments"* | `ComposerSurface.cs:245-246` |
| 15 | The compile's cost is the turn's spend on its outcome line | `ComposerSendGate.cs:490-495`; `SessionDocumentSurface.cs:658`; `src/AiDe.Core/Presentation/Sessions/RunChannelSessionThread.cs:164`; `src/AiDe.Core/Presentation/Sessions/SessionThread.cs:422-423` |
| 16 | **Up** in the combo box selects; announcement *"Compile mode is now {mode} for new prompts."* | `SessionDocumentSurface.cs:1557-1566`, `:1438`; Up/Down driven in `TheCompileModeControlTests` (node 1 (B)) |
| 16 | *"Mechanical-only: one gesture, you fill the structure. Agentic-advisory is available; choose it to have the compile step suggest the lines."* | `SessionDocumentSurface.cs:1399`, `:1428` |
| 17 | One press sends under mechanical-only | `ComposerSurface.cs:660` (the prepare branch is skipped), `:683` |
| 18 | **Alt+F4** (*E_xit*); **Alt+F → S** (*Recent _sessions*) | `src/AiDe.App/Workbench/MainMenuBuilder.cs:256`, `:209` |
| 19 | Disclosure UIA "Compiled prompt of b{n}" | `src/AiDe.App/Workbench/Sessions/TurnItem.cs:200`; `src/AiDe.App/Workbench/Sessions/ThreadFeed.cs:665`, `:673` |
| 19 | Rebuilt bytes shown only on a sha match | `ComposerSendGate.cs:269-288` |
| 20 | The rebuild uses no template and no attachment | `ComposerSendGate.cs:277-281` |
| 20 | No template catalog in the product | `SessionComposerBinder.cs:151-173` (no catalog argument); `ComposerSurface.cs:538`, `:564-565` |
| 20 | Attach off by default; no App caller of `SetAttachEnabled` | `src/AiDe.Core/Sessions/SessionConfig.cs:76`; `src/AiDe.Core/Sessions/SessionConfigStore.cs:320`; `ComposerSurface.cs:1735-1741` |
| 20 | *"Compiled prompt not recorded: …"* | `src/AiDe.App/Workbench/Records/NotebookRuns.cs:504`, `:531` |
| 21 | Unsent draft and kept lines are not restored | Ruling 182; measured in `c1-restart-measure.txt` (node 2 (0)) |
