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
