---
id: proof-contrast-census
title: "Proof Pack - Contrast census phases 1-5"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "facelift"
tags: [proof-pack, ui, contrast, wcag, census, tokens, telemetry, dc-139, dc-140]
links:
  - { to: inv-0008-contrast-floor-passes-while-the-shell-fails, rel: tested-by }
  - { to: note-20260911-on-accent-ink-is-its-own-token, rel: relates-to }
  - { to: note-20260911-contrast-census-runs-out-of-process, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Evidence that INV-0008's phases 1-5 landed: the composed shell's contrast census goes from 180
  pairings / 14 below floor / 2 disabled-state losses to 180 / 0 / 0 by removing the leaf ink
  override, pairing ink on the containers, an on-accent token (6.60:1), the tab trigger on IsActive,
  and the composer page drawing from tokens the host pushes on host.init; two source rules in
  TokenDisciplineTests (seen red); app.start naming the binary (seen red); DC-139/140/141 registered.
  Full App suite green; every verify-* gate green; the craft floor promoted to gated over
  src/AiDe.App/Web.
---

# Proof Pack: Contrast census phases 1–5

- **Branch / commits:** `fix/contrast-census` — see the closing commit; merge base `main` `7d95f8cd`, merged `origin/main` `5c132902` (the composer fix) before closing.
- **Design authority:** `docs/investigations/INV-0008-contrast-floor-passes-while-the-shell-fails.md` §§5, 7, 8, 9 (re-issued from INV-0007 — the composer investigation landed first on that id).
- **Components:** `src/AiDe.App/App.xaml` (leaf styles, `AccentContrastBrush`, toggle/row on-accent ink), `Workbench/DockRoundedTabs.xaml` (tab ink on `IsActive`), `Workbench/SurfaceChrome.cs` (card pairs ink with ground), `Workbench/Composer/ComposerPageTheme.cs` (new), `Workbench/Composer/ComposerSurface.cs` (+1 additive `theme` field on `host.init`), `Web/composer.html` + `Web/composer.mjs` (custom properties), `Web/composer-host.html` (token values), `Workbench/WorkbenchDiagnostics.cs` (`AppStart`, `CommitOf`), `MainWindow.xaml.cs` (`Loaded` → `app.start`), `tools/verify-ui-craft-floor.py` (`src/AiDe.App/Web` gated).
- **Tests:** `tests/AiDe.App.Tests/ShellContrastCensusTests.cs` (4 facts), `TokenDisciplineTests.cs` (+3), `ContrastFloorTests.cs` (theory branch), `AppStartIsRecordedTests.cs` (3, new), `Composer/ComposerPageThemeTests.cs` (5, new); the probe `tests/AiDe.App.ContrastProbe` (report gains `ShellTheme`, `PageTheme`, `AppStart`).

## The census, before and after

| | Pairings | Below floor | By mechanism | Disabled-state losses |
|---|---:|---:|---|---:|
| `main` `7d95f8cd` (pre-fix) | 180 | **14** | app-style token 13 (2.37:1, `TextBrush` on `AccentBrush`) · page-css 1 (4.47:1, `#drop-hint`) | **2** |
| after Fix A only (intermediate) | 180 | 4 | inherited 3 (1.27:1, `#000000` from `LayoutDocumentPaneControl` — the photographed footer lines) · page-css 1 | 0 |
| after phases 1–3 | 180 | **0** | — | **0** |

Worst-to-best of the repaired sites, measured rendered: active tab `AccentContrastBrush` on `AccentBrush` **6.60:1** · selected-inactive tab `TextBrush` on `BorderBrush` **10.74:1** · checked toggle / selected palette row **6.60:1** · disabled buttons `DisabledTextBrush` on raised **4.59:1** (floor 3.0, provenance `ParentTemplateTrigger`) · "Compiled view" **13.57:1** (inherited from the card) · composer `#drop-hint` and labels **7.16:1** · `span.required` **6.26:1** · editor text **15.62:1**.

## Claims

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| Every text pairing the composed shell renders clears its floor | `ShellContrastCensusTests.EveryTextPairingInTheComposedShellClearsItsFloor` | `App.xaml` leaf styles, `SurfaceChrome.cs:47`, `DockRoundedTabs.xaml` `IsActive` trigger | 0 of 180 below floor, zero tolerance, named sites present (DC-016) | 14 on `7d95f8cd`; 4 after Fix A alone | Verified | hover/pressed, popups not opened (Phase 6) |
| A disabled control's ink is the disabled token | `…ADisabledControlsInkIsTheDisabledToken` | `App.xaml` `IsEnabled=False` triggers reach the glyphs by inheritance | provenance starts `DisabledTextBrush` | 2 of 2 wrong on `7d95f8cd` | Verified | disabled check/radio/menu item unrendered (Phase 6) |
| The composer page clears its floor | `…EveryTextPairingInTheComposerPageClearsItsFloor` | `composer.html` custom properties | 0 of 23 below floor, page measured (not skipped) | 1 on `7d95f8cd` | Verified | field borders 1.39:1 unmeasured by the census (see residuals) |
| The page draws with the tokens the shell pushed, not its fallbacks | `…TheComposerPageDrawsWithTheTokensTheShellPushed` | `ComposerSurface.PushInit` (`theme`), `composer.mjs` `applyTheme` | 11 inline root custom properties equal the shell's push | red with `applyTheme` removed (every role "(not set)") | Verified | — |
| No implicit style on a text leaf states an ink | `TokenDisciplineTests.NoImplicitLeafTextStyle_SetsItsOwnInk` | `App.xaml:390-397` | offenders = 0 across every `.xaml` | red on pre-fix `App.xaml` lines 391, 396 | Verified | keyed styles opted into per site are outside the rule by design |
| A trigger that paints a ground states an ink that clears it | `…EveryTriggerThatPaintsAGround_StatesAnInkThatClearsIt` + `TheSourceRules_FireOnTheShapesThatShippedTheDefect` | `App.xaml` templates | ratio of (trigger ink ∨ rest ink) on the painted token ≥ 4.5 | engine red on planted `IsChecked → AccentBrush`, no ink | Verified | grounds via `TemplateBinding` / `ComponentResourceKey` are the census's, not this rule's |
| Leaf types inherit; the root states the ink | `ContrastFloorTests.TheBaseControlSetHasAnImplicitDefault…` (TextBlock, Label branch) | `App.xaml` `Window` style | no `Foreground` on the leaf style, `Foreground` on `Window` | would fail on the setter's return | Verified | — |
| Every page role resolves from the dictionary to its token's value | `ComposerPageThemeTests` (5) | `ComposerPageTheme.From` | 11/11 roles, `#RRGGBB`, equal to the token colour; null/empty dictionary → empty | seen green (source check, TC3) | Verified | — |
| The shell names its binary on boot | `AppStartIsRecordedTests.TheRecordNamesTheBinary…`, `AVersionWithNoRevision…`, `TheComposedShellEmitsItOnBoot` | `WorkbenchDiagnostics.AppStart`, `MainWindow.xaml.cs` `Loaded` | `evt=app.start`, 40-hex commit = version suffix, DPI/window carried; the real `App` writes it | 3 red with a stub `AppStart` | Verified | not yet on the status strip / Help → About |
| The composer handshake is unchanged by the additive field | `ComposerHostIntegrationTests.TheHandshakePushesExactlyOneHostInitPerMount…` | `ComposerSurface.cs` | one init per mount, both orders | green (unchanged behaviour) | Verified | — |
| The push, not a copy: a sentinel the probe plants on `FocusBrush` reaches the page root | `…TheComposerPageDrawsWithTheTokensTheShellPushed` (sentinel clause) | `ContrastProbe/Program.cs` (`FocusSentinel`), `PushInit`, `applyTheme` | page `--focus` == shell `--focus` == `#010203`, a colour no token declares | red with `applyTheme` removed (first form of the fact) | Verified | — |
| `applyTheme` refuses malformed entries, on the live page | `…TheComposerPageRefusesAMalformedTheme` | `composer.mjs` `applyTheme` guards | root unchanged after `{url(), 42, 5-hex, "--Accent"}` | red with the guard disabled (`--Accent=#000000` on the root) | Verified | — |
| Every stylesheet fallback is its token's declared value; the host-probe page uses only token values | `ComposerPageThemeTests.EveryFallbackInThePageIsItsTokensDeclaredValue` | `composer.html`, `composer-host.html` | every `var(--role, #hex)` has a pushed role and `hex == token`; no `var()` without a fallback; no literal, functional or named colour in the composer page; only token values in the host page | red on a planted `var(--text-muted, #7F858A)`, on `var(--text-muted)` and on `outline-color: white` | Verified | the reader half of E8 for the frame before the push |
| The role table is pinned, not read back from itself | `ComposerPageThemeTests.ARoleCarriesItsTokensValue…` (11 inline rows) + `TheRoleTableHasExactlyThePinnedRoles` | `ComposerPageTheme.Roles` | each `(property, token)` from INV-0008 §7 is in `Roles` and resolves to that token's colour | would fail on `--text-muted → DisabledTextBrush` | Verified | — |
| One `app.start` per boot | `AppStartIsRecordedTests.TheComposedShellEmitsItOnBoot` (`AppStartCount == 1`) | `MainWindow.Loaded` | the probe collects every line; exactly one | would fail on a second emitter | Verified | — |

## Gates (bare, one exit code each, stop on first red)

| Gate | Result |
|---|---|
| `dotnet build src/AiDe.App -p:TreatWarningsAsErrors=true` | 0 |
| `dotnet build tests/AiDe.App.Tests -p:TreatWarningsAsErrors=true` · `tests/AiDe.Core.Tests` | 0 · 0 |
| `dotnet test tests/AiDe.App.Tests` (full) | 600 passed, 0 failed (554 baseline) |
| `python tools/verify-test-run.py` (CHECK mode, never `--update`) | 0 — AiDe.App.Tests 600 ≥ 554, AiDe.Core.Tests 2240 ≥ 2239, both Completed |
| `python tools/verify-*.py` (every one, 31) | 0 each after `regenerate-derived.py` (the two derived-view gates are red until it runs); `verify-ui-craft-floor.py`: `src/AiDe.App/Web` gated at Major, 0 findings |
| `python tools/regenerate-derived.py` | every derived view current |

## E7 surface list (ticked)

`App.xaml` resources ✔ → `DockRoundedTabs.xaml` / chrome templates ✔ → token dictionary (one theme; no light dictionary exists in code — `DESIGN.md`'s Light mode is a declared intent with no runtime dictionary, so there is no second value to carry) ✔ → `composer.html`/`composer.mjs` custom properties ✔ → `host.init` envelope (`theme`, writer `PushInit`, reader `applyTheme`, proven by the inline-property fact) ✔ → `WorkbenchDiagnostics.AppStart` (writer `MainWindow.Loaded`, reader: the probe's report and the operator's log) ✔ → census + `TokenDisciplineTests` + `ContrastFloorTests` ✔ → register DC-139/140/141, DC-135 rec. 2, DC-131 instance ✔. `DESIGN.md` — **not written** (D1 owns it; the row's wording is in the decision note).

## Review (Adversary Mode)

- **UX & Accessibility:** PASS-with-conditions. On-accent pairing confirmed correct and the only legal ink on the accent (text 2.4, muted 1.1, verified 1.2, danger 1.0 — all fail); disabled 4.59:1 correct as a pairing. Conditions (all outside this diff's scope, carried as next steps): (1) the `DESIGN.md` row per the decision note; (2) a non-colour selected indicator on the selected-inactive tab (ground-only at 1.39:1 vs unselected); (3) a `border-strong` token for input boundaries (page and WPF wells at 1.1–1.39:1 — CD16 covers separators, not control boundaries); (4) empty-state and field-label ink → `{colors.text}`, muted reserved for metadata (Phase 7, operator decision); (5) a rule against a *local* ink under an accent-ground trigger (structurally confirmed, not rendered). One Nit fixed: the census's omission line claimed selected-inactive unmeasured while five rows measure it.
- **Test Architect (round 1): BLOCK** — the Blocker was the absent Proof Pack (this file was
  written after the review; the four red runs are now the *Red observed* column). Findings acted
  on in the same turn: the stylesheet reader was unproven (→ `EveryFallbackInThePageIsItsTokensDeclaredValue`,
  seen red on a planted drift); pushed == fallback could hide a page applying its own copy (→ the
  `FocusBrush` sentinel); the role table used `Roles` as its own oracle (→ pinned inline rows);
  the page read could precede the applied init (→ the probe polls `__composerInitCount`);
  `app.start` emitted twice would pass (→ `AppStartCount == 1`, plus `pixelsPerInchY`, `ts`,
  configuration asserted); `applyTheme` had no negative test (→ `TheComposerPageRefusesAMalformedTheme`
  against the live page); the ground rule missed `<Trigger.Setters>` element syntax, Style-level
  rest setters and `TextBlock.Foreground`-qualified inks (→ handled). **Not acted on, recorded:**
  a golden/schema test for the `host.init` envelope and the composer probe reusing the payload
  builder — both live in the composer owner's files (`ComposerSurface.cs`'s handshake,
  `ComposerProbe/Program.cs`); the 22 C# sites that set a leaf ink locally to `TextBrush` (a local
  value would block a container's state pairing the same way — none sits inside an accent-ground
  container today; the census measures them; INV-0008 Phase 2(b)/(c) stays descoped); the
  last-focused-but-inactive document tab is a state the census does not produce (Phase 6);
  "unmeasurable ground" (a `ComponentResourceKey` / `TemplateBinding` ground) stays a census
  matter, not a source finding — making it one would flag every close-button trigger in the tab
  template. **Round 2: PASS-with-conditions — the hard veto clears** (Proof Pack with a
  Red-observed column per claim; the reader rule, sentinel, pinned roles, init-count poll and
  `AppStartCount` closed the Majors that had no verification path). Its three Minors were then
  taken: the malformed-theme fact **seen red with `applyTheme`'s guard disabled** (`--Accent=#000000`
  reached the root) and its two inert entries dropped; the fallback rule widened to `var(--x)` with no
  fallback, hex of any width, functional and named colours — seen red on a planted `var(--text-muted)`
  and `outline-color: white`; the three new ground-rule syntaxes (`<Trigger.Setters>`, `<Setter.Value>`,
  a Style's rest setter) each have a planted counterexample in `TheSourceRules_FireOnTheShapes…`.
  The three soft Majors it carries are the residuals below (host.init schema at the composer owner's
  seam; the C# local leaf inks; the last-focused-but-inactive tab).

## Residuals

- Phases 6 (census reach: disabled check/radio/menu item, hover/pressed, popups, split canvas) and 7 (the muted palette decision) — open, operator-facing.
- Borders: no control measures non-text contrast on either population; the composer page's field boundary is `{colors.border}` at 1.39:1 on the surface. A `border-strong` token is a `DESIGN.md` decision.
- `app.start` is in the log only; the status-strip tooltip / Help → About surface of Fix D is not built.
- `verify-id-allocators.py` notes `investigate/contrast-census` (the superseded investigation branch) still holds INV-0007 — that branch is not this one's to rewrite; deleting it after this merges clears the note.
- The tab trigger's condition (`IsActive`) rests on the theme source at `master` plus the census's measurement on the 5.0.0 package (marked `assume:` in the decision note).
