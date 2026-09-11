---
id: inv-0008-contrast-floor-passes-while-the-shell-fails
title: "The contrast floor passes while the shell fails: a floor over a population the product does not render"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "facelift"
tags: [ui, contrast, wcag, theme, wpf, avalondock, webview2, census, floors, dc-135, dc-131]
links:
  - { to: ui-review-operator-feedback, rel: refines }
  - { to: adr-0012-docking-shell-library, rel: depends-on }
  - { to: adr-0008-shell-host, rel: depends-on }
  - { to: spec-app-facelift, rel: relates-to }
review-by: 2026-12-01
summary: >-
  The operator reported dark-on-dark and light-on-light text after U2's contrast floor went green.
  Two findings, both measured. (1) The photographed sites — black footer text, a white text box, dim
  mode-strip captions — are the ORIGINAL instance seen on a Release binary built before the fix
  merged: the three Release builds on the machine carry their commit in their informational version,
  and the two that match the screenshot (be68ca1c, 2a363f4f) predate 5213d7bb. (2) On main today a
  census of the shell the product composes — the real App booted out of process, every surface kind
  opened, every menu, the palette, the composer page — finds 180 text pairings and 14 below floor
  that the floor cannot see: twelve at 2.37:1 where the fix's own implicit TextBlock style overrides
  the accent-ground state ink every container sets by inheritance, two disabled controls whose
  DisabledTextBrush never reaches the glyphs, and one page-CSS hint at 4.47:1. The floor measured
  eleven subjects it constructed on a window it built; the product composes a different population.
---

# INV-0008 — The contrast floor passes while the shell fails

- **Status:** Root cause verified · fix proposed · **stopped for review**
- **Severity / tier:** T1 — every surface, every operator, on the first screen
- **Reported by / date:** the operator, 2026-09-11 18:07Z (`al-01M28TDX0G5RGHXKY5QTTFQMPH`, session `conductor-addendum-c`)
- **Related:** `docs/reviews/ui-operator-feedback.md` (U1, §2a — the eleven pairs), `DESIGN.md` §Palette roles, commit `5213d7bb` (U2's floor), `tests/AiDe.App.Tests/ContrastFloorTests.cs`, DC-131, DC-133, DC-135

> Diagnosis only. The census (`tests/AiDe.App.ContrastProbe`, `ShellContrastCensusTests`) is committed
> red as the evidence and the sweep; no fix is made. Investigation worktree `investigate/contrast-census`.
>
> **Id re-issued (DC-013):** this investigation was authored as INV-0007 on `investigate/contrast-census`;
> `verify-id-allocators.py` reported INV-0007 allocated independently there and on
> `investigate/composer-input` (`INV-0007-composer-entry-areas-starved-by-the-compiled-view.md`), which
> landed first on `fix/composer-entry-areas`. Re-issued as **INV-0008** on `fix/contrast-census` (file,
> `id`, the note's link, the test remark); the original audit entry `al-01M28X1SAPJE12TF0FKMEQXHZT` still
> names INV-0007 — the log is append-only, and the implementation entry supersedes it.

## 0. What was measured, and what was not

**Measured (Verified).** The composed shell at `main` `f5c0f740` — the real `AiDe.App.App` booted out
of process with its compiled `App.xaml`, its `MainWindow`, a `WorkbenchShell` over
`ZoneBackedLayoutService` (DC-135), the VS2013 dark dock theme retokenised, every surface kind
`SurfaceContentFactory.Kinds` builds, a session document with its composer configured and refused,
every top-level menu opened, the command palette, and the composer's WebView2 page. 180 text pairings,
each with its ink's provenance from the property system and its ground sampled from rendered pixels.
The informational version of each Release `AiDe.App.dll` on the machine, and whether that commit
contains `5213d7bb`. The pre-fix source at `5213d7bb^` and `4b9dd779`. The necessity experiment
(§4) — the implicit `TextBlock` ink setter removed, the census re-taken, the setter restored.

**Not measured (named).** Which process the operator launched — the shell writes no launch record
(§8, Phase 4). Hover/pressed/selected-inactive trigger states, combo drop-downs, tooltips, context
menus, the New Session sheet, the split canvas state, and terminal cells: each listed in the
census's own omissions table with the reason. The pre-fix binaries were **not run**; the pre-fix
*source* was read and the screenshot compared against what that source can render.

## 1. Symptom

The operator: *"We still have lots of cases of dark/hard-to-read font colors with regards to the tool
background. We need to ensure we have a consistent color palette that works consistently, and stop
putting dark fonts on dark backgrounds and light fonts on light backgrounds."* The screenshot (a
Release build, dark theme, session document beside the graph): the **"Compiled view"** caption, the
**lease** line, the **status** paragraph and the mode-strip captions dim-on-dark; the **compiled-view
`TextBox` a white box with dark text**; **"Attaching files is off for this session."** dim;
**"Lanes"** dim. `ContrastFloorTests` was green on `main` when the report arrived.

Expected: every text pairing ≥ 4.5:1 (3:1 large / UI / disabled per `DESIGN.md`). Actual: see §1a.

### 1a. The reproduction — the census, red on `main`

`ShellContrastCensusTests` (3 facts) launches `AiDe.App.ContrastProbe`, which boots the product and
walks it. On `f5c0f740`, `dotnet test --filter ShellContrastCensusTests`: **3 failed, 0 passed**.
The whole App suite: **554 passed, 3 failed — the three census facts only** (40 s).

```
AiDe.App 1.0.0+f5c0f740fd0488baa00dd2de4320c187f585058e · 180 pairings measured · 14 below floor
- app-style token: 13        (12 WPF sites at 2.37:1, TextBrush on AccentBrush)
- page-css: 1                (#drop-hint "Attaching files is off for this session." 4.47:1)
+ 2 of 2 disabled controls render their ENABLED ink (13.57:1 — clears the floor, loses the state)
```

The full table is in Appendix A, verbatim. The ten worst:

| # | Ratio | Floor | Ink | Ink source | Ground | Surface · element | Mechanism |
|---|---:|---:|---|---|---|---|---|
| 17 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | tab: Graph — selected-active tab caption | app-style token overrides container ink |
| 36 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | tab: Census session | same |
| 40 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | tab: Contexts | same |
| 42 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | tab: Joins | same |
| 54 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | tab: Census prompt | same |
| 55 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | class diagram · checked ToggleButton "Diagram" | same |
| 61–75 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | tabs: classdiagram, sequence, search, codeviewer, diagnostics, session-document | same |
| 80 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` AccentBrush | command palette · selected row "Move pane…" | same |
| 180 | 4.47 | 4.5 | `#7F858A` | page CSS | `#1E1E1E` page CSS | composer page · `div#drop-hint` "Attaching files is off for this session." | page CSS, not tokens |
| 21, 41 | 13.57 | 3.0 | `#E4E9EF` | TextBrush · Style | `#1A1F26` SurfaceRaised | disabled "Attach file…", disabled "Transfer →" | disabled ink never reaches the glyphs |

**Counts by mechanism (all 180):** ink from the implicit leaf style `TextBrush · Style` **93**;
`TextMutedBrush` by template (menu gestures) 32; `TextMutedBrush` by resource reference 25;
page CSS 23; `TextBrush` local 6; `VerifiedBrush` 1. Failing: implicit-leaf-style-over-container-ink
**12** (+2 state losses), page CSS **1**. Inherited 0, platform default 0, hardcoded 0,
opacity-dimmed 0 — at `f5c0f740`, in the rest state.

## 2. Timeline (Change Analysis)

| When (−07:00) | What | Evidence |
|---|---|---|
| 06:40 | U1's review measures eleven failing pairs at 1.15–1.27:1: unstyled ink (platform black) on dark tokens; one inverse (TextBrush on platform white) | `docs/reviews/ui-operator-feedback.md` §2a |
| 07:00 | `5213d7bb` on `feature/ui-implementation`: implicit defaults for 18 base types (**incl. `TextBlock` Foreground=TextBrush**), `DisabledTextBrush`, `ContrastFloorTests` (11 sites + 18-type theory), `ThemeProbe` | `git show 5213d7bb --stat` |
| 09:05 | `be68ca1c` on `main` — **does not contain 5213d7bb** | `git merge-base --is-ancestor` → false |
| **09:23:05** | **`C:\Projects\ai-de\src\AiDe.App\bin\Release\AiDe.App.dll` built — informational version `1.0.0+be68ca1c…`** | `VersionInfo.ProductVersion`, file time |
| 09:43 | `92809f9b` merges `feature/ui-implementation` into `main` — the fix lands | `git log --first-parent` |
| 10:29 | `feature/exit-evidence` Release build — `1.0.0+2a363f4f…`, **does not contain 5213d7bb** | same |
| 10:34 | `f5c0f740` = `main` HEAD (this investigation's base) | |
| 10:56 | `investigate/composer-input` Release build — `1.0.0+f5c0f740…`, contains the fix | same |
| **11:07** | **Operator's report and screenshot** | audit `al-01M28TDX0G5RGHXKY5QTTFQMPH` |
| 11:24– | this investigation; census red on `main`; necessity experiment | §4 |

## 3. System map

```
Application.Resources (App.xaml, compiled)            ← tokens + 18 implicit styles (5213d7bb)
  └ MainWindow (Foreground=TextBrush via implicit Window style)
      ├ Menu / MenuItem templates  → ContentPresenter(RecognizesAccessKey) → AccessText → TextBlock
      ├ DockingManager (VS2013 dark, DockThemeAccents by VALUE, DockRoundedTabs.xaml)
      │    └ LayoutDocumentTabItem template → ContentPresenter#TabTitle
      │           TextElement.Foreground = TextBrush; IsSelected → SurfaceSunkenBrush   (INHERITANCE)
      │           → generated TextBlock  ←── implicit <Style TargetType=TextBlock> Foreground=TextBrush (STYLE)
      ├ ChromeButtonTemplate / ChromeToggleTemplate / ChromeListBoxItemTemplate
      │      IsEnabled=False → Content.TextElement.Foreground = DisabledTextBrush           (INHERITANCE)
      │      IsChecked / IsSelected → Content.TextElement.Foreground = SurfaceSunkenBrush   (INHERITANCE)
      │           → generated TextBlock  ←── the same implicit style                       (STYLE)
      └ Surfaces (ContentControls: Background set but NEVER PAINTED by the default template)
           ├ ComposerSurface: TextBlocks with no Foreground; TextBox; WebView2 → composer.html (own CSS palette)
           └ … every other kind
```

WPF precedence: **local > style trigger > template trigger > style setter > … > inheritance > default.**
A Foreground on a container reaches a string's generated `TextBlock` only by inheritance; an implicit
`TextBlock` style in `Application.Resources` applies inside every template and wins.

## 4. Hypotheses considered — Change Analysis + Ishikawa, then verify-by-data

| Hypothesis | Predicts we'd see… | Evidence for | Evidence against | Verdict |
|---|---|---|---|---|
| H1 The photographed binary predates `5213d7bb` | black footer text (platform `ControlText`), a white `TextBox` (platform default), Console+Terminal mode toggles in platform chrome | Both Release builds that match the screenshot carry `be68ca1c` / `2a363f4f` in their informational version; neither contains the fix. At `5213d7bb^` there is **no** `TextBlock`/`TextBox`/`Window` implicit style and the composer footer sets no Foreground, so its ink is the platform default. At `4b9dd779` the mode strip still has a **Terminal** toggle (Ruling 45 removed it in the same merge) with no implicit `ToggleButton` style. At `f5c0f740` the census measures the compiled-view `TextBox` ground `#0D1014` (sunken) and "Compiled view" at 13.57:1 — the screenshot's white box is **impossible** on HEAD. | The launched process is not recorded; a third build (`f5c0f740`, 10:56) existed 11 min before the report and cannot be excluded by a log. | **Verified** for "a build without the fix renders exactly this"; **Inferred (strong)** for "that is the build the operator ran" |
| H2 HEAD has a runtime path the floor and the census both miss for the photographed sites | the census reproducing dim footer text / a white box on HEAD | — | 180 sites measured on the real App boot; the photographed sites clear at 13.57–15.62:1; `TextBox` ground is the sunken token | **Ruled out** for the photographed sites |
| H3 The floor's population is not the product's (the brief's hypothesis) | sites failing in the composed shell that no `ContrastFloorTests` site covers | 14 failing on HEAD, floor green; every failing site is a **state** (selected, checked, disabled) inside a real template — the floor constructs rest-state controls on a themed *Window* (not an `Application`), never a dock tab, never a checked toggle, and reads the *control's* Foreground property (site 10) rather than the glyphs' | — | **Verified** |
| H4 The failing ink comes from a library default / inherited dock gray | `Inherited` / `Default` / `library-style` provenance rows | — | 0 rows with those sources at HEAD; all 12 read `TextBrush · Style` and the theme brush instance matches by reference | **Ruled out** at HEAD (it *was* the pre-fix mechanism: platform default) |
| H5 A "muted" token on an unmeasured ground | `TextMutedBrush` rows below 4.5 | — | every `TextMutedBrush` row measures 6.48 (raised), 6.83 (menu), 7.46 (sunken) — all clear | **Ruled out** as a floor failure; **kept as a design finding**: "Lanes" is one of these and the operator reads 6.48:1 as too dim (§8, Phase 7) |
| H6 Opacity dimming | rows with Opacity < 1 | the composer's template picker sets 0.75/0.6 on card text | the picker is collapsed with no catalog; 0 rendered rows dimmed | **Ruled out** in the rest state; the picker stays a named omission |
| H7 Page CSS outside the tokens | webview rows below 4.5 | `#drop-hint` `#7F858A` on `#1E1E1E` = 4.47:1; the page's whole palette is VS Code's (`#1E1E1E`/`#D4D4D4`/`#9AA0A6`) not `DESIGN.md`'s | — | **Verified** (1 site; the seam is visible at every ratio) |

### 4a. Necessary and sufficient — the mechanism at HEAD

- **Sufficient:** with the implicit `TextBlock` style's `Foreground` setter present (HEAD), 12 sites
  measure 2.37:1 and the 2 disabled controls render `TextBrush`. Measured (§1a).
- **Necessary:** the setter removed (one line, then restored), the census re-taken in the same host:
  **12 → 0** accent-ground failures; the tab captions read `SurfaceSunkenBrush · Inherited from
  ContentPresenter#TabTitle (ParentTemplateTrigger)`; the menu headers read `TextBrush · Inherited from
  MenuItem (Style)`. Nothing else regressed **except three new failures that the setter was masking**:
  the *selected-inactive* tabs (Explore, Graph, Terminal — the selected tab of a pane that does not hold
  focus) at **1.45:1** — `DockRoundedTabs.xaml`'s `IsSelected` trigger paints the sunken ink for every
  selected tab, but only the selected-*active* tab has the accent ground; the inactive one is
  `#2A313B`. Two defects interlocked: the implicit style hides the tab trigger's bug and creates twelve
  of its own.

## 5. Verified root cause

**Proximate (HEAD).** `App.xaml` (5213d7bb) declares `<Style TargetType="TextBlock">` with
`Foreground={StaticResource TextBrush}` in `Application.Resources`. Every state pairing in the same
file's own chrome templates — `IsChecked`/`IsSelected` → `SurfaceSunkenBrush` on the accent ground,
`IsEnabled=False` → `DisabledTextBrush` — and `DockRoundedTabs.xaml`'s selected-tab ink are all set on
the **container** (`TextElement.Foreground` on a `ContentPresenter`, or `Control.Foreground`) and
reach the glyphs only by **inheritance**. The leaf's implicit style outranks inheritance, so every
string-content `TextBlock` in the shell (93 of 180) renders `TextBrush` regardless of its container's
state: light on accent at 2.37:1, and "disabled" indistinguishable from "enabled".

**Systemic.** The floor measured *subjects the test constructed* — rest-state controls on a `Window`
carrying the theme, never the composed product, never a state, and (site 10) the control's property
rather than the rendered leaf. Its own remark named the class and then built per-site coverage anyway.
A gate whose population is not the product's cannot fail on the product (DC-135's shape, DC-133's
arithmetic): green was evidence the gate passed, not that the shell did.

**The photographed instance.** A Release binary without `5213d7bb` (H1). Not a recurrence of the
fixed class — the same instance, seen on a build from before the fix merged, and reported as a
recurrence because nothing tied the screenshot to a commit. The binary *does* carry its commit
(`AssemblyInformationalVersion` = `1.0.0+<sha>`); the shell never emits it.

## 6. Causes ruled out

- **A runtime difference between the test host and the product** for the photographed sites — the
  census boots the real `App` out of process (H2).
- **Library defaults / inherited dock grays** at HEAD — provenance rows show none (H4). This *was* the
  mechanism before the fix (platform `ControlText`), which is why the screenshot matches a pre-fix build.
- **Muted-token pairings** — every one clears (H5).
- **Opacity** — none rendered (H6).
- **A stale-binary explanation for the HEAD failures** — the 14 are measured on `1.0.0+f5c0f740`.

## 7. Specific fixes for the found instances — proposed, not made

- **Fix A (mechanism, 12 sites + 2 state losses):** the leaf must not state its own ink. Remove the
  `Foreground` setter from the implicit `TextBlock` (and `Label`) style; the root already states it
  (`Window` style, `Foreground=TextBrush`, App.xaml:677) and every container's state ink then reaches
  the glyphs by inheritance. **Why systemic:** it restores the one rule the templates were written
  to — *the container pairs ink with ground; the leaf inherits* — instead of patching twelve triggers.
  **Blast radius:** every `TextBlock` in the shell; the census is the proof it holds (180 sites) and
  the experiment shows exactly three sites change for the worse (Fix B). **Rollback:** restore the
  setter. **Regression tests (red today):** `EveryTextPairingInTheComposedShellClearsItsFloor`,
  `ADisabledControlsInkIsTheDisabledToken`; the 18-type theory in `ContrastFloorTests` must
  exempt leaf text types with the reason, or it goes red on the fix.
- **Fix B (3 sites the experiment uncovered):** `DockRoundedTabs.xaml` — the sunken ink only when
  the tab is selected **and** active (`IsSelected && IsActive`, or `IsLastFocusedDocument`); the
  selected-inactive tab keeps `TextBrush` on `#2A313B` (10.74:1). **Regression test:** the census
  with Fix A applied (Explore/Graph/Terminal captions ≥ 4.5). Record the on-accent pairing in
  `DESIGN.md`'s palette table — `{colors.surface-sunken}` on `{colors.accent}` = 6.6:1 (hand-computed
  from the token values; the census re-measures it) — the table today lists ratios on
  `{colors.surface}` only, and the accent-as-ground family is the one that failed.
- **Fix C (1 site, and the seam):** the composer page under the theme's tokens — CSS custom
  properties (`--surface`, `--surface-raised`, `--surface-sunken`, `--text`, `--text-muted`,
  `--accent`, `--border`, `--danger`) injected by the host on `host.init` from
  `Application.Resources`, the page's rules referencing them with today's literals as fallbacks;
  `#drop-hint` → `--text-muted` (7.0:1 on `--surface`). **Regression test:**
  `EveryTextPairingInTheComposerPageClearsItsFloor`. Overlap: `investigate/composer-input` owns the
  composer's layout defect — recorded, not acted on.
- **Fix D (attribution):** emit the informational version at startup — an `app.start` event in the
  workbench log `{version, commit, configuration}` and the string on the status strip's tooltip /
  Help → About. A screenshot report then starts from the log. **Regression test:** the log carries
  `app.start` with a 40-hex sha on boot (the probe already reads the attribute; the test reads the log).

## 8. Generalization — the failure class

- **Failure class 1 (proposed; next free register id 136 per `verify-id-allocators.py` — "the leaf overrides the container's pairing"):** a theme rule
  applied to a leaf text type (`TextBlock`, `Label`, `AccessText`, `Run`) by an implicit style
  outranks every container-level state pairing (selected / checked / disabled / on-accent) that
  reaches the leaf by inheritance, so all of them revert to the rest-state ink at once.
  *Signature:* `<Style TargetType="TextBlock">` (or `Label`) with a `Foreground` setter in an
  application dictionary, beside templates that set `TextElement.Foreground` on a
  `ContentPresenter`; a census row whose ink source is `Style` inside a state trigger's scope.
  *Why it survives:* the per-site floor reads rest states and control properties; the state
  triggers *look* right in source; WPF reports the value source honestly but nobody reads the leaf.
  *Control (highest holding rung):* the census (automated, over the product), plus a source-level
  rule in `TokenDisciplineTests` — no implicit style for a leaf text type may set `Foreground`.
- **Failure class 2 (DC-135, recurrence 2):** the tests exercise what the product does not
  construct — `ContrastFloorTests` builds its subjects on a themed `Window`; the product composes on
  an `Application`, inside a dock, in states. Count: 11 constructed sites + 18 type checks vs 180
  composed pairings; overlap on the failing family: 0.
- **Failure class 3 (DC-131, instance):** a population report ("lots of cases") closed by a mechanism
  fix (eleven pairings + implicit defaults) with no census; the census is the control and it took the
  attribution column the register demands (mechanism per row).
- **Failure class 4 (proposed; next free register id 137 — "a report against an unattributed binary"):** a screenshot is
  evidence about a *binary*, and the binary is never named; a fixed instance re-enters as a
  recurrence and the next investigation re-derives the mechanism. *Signature:* three Release builds on
  one machine from three commits, none of which the report names; a log with no `app.start`.
  *Control:* Fix D, and the rule that a UI defect report is attributed to `1.0.0+<sha>` before it is
  triaged as new or recurring.

**Broader systemic rule.** *The container pairs ink with ground; the leaf inherits; the census proves
the composition.* Palette rule (the operator's ask): every ink and every ground is a `DESIGN.md`
token; the token table carries every ground the ink is used on (surface, raised, sunken, **accent**,
menu); a state is a *pairing* on the container, never a leaf override or an opacity.

| Candidate sibling (where) | Same class because… | Verdict | Evidence |
|---|---|---|---|
| `ChromeToggleTemplate` `IsChecked` → sunken ink (App.xaml:326) | container ink by inheritance under the leaf style | **confirmed** | row 55 "Diagram" 2.37:1 |
| `ChromeListBoxItemTemplate` `IsSelected` → sunken ink (App.xaml:611) | same | **confirmed** | row 80 "Move pane…" 2.37:1 |
| `ChromeButtonTemplate` / `ChromeToggleTemplate` `IsEnabled=False` → DisabledTextBrush (App.xaml:296, 337) | same, state lost not ratio | **confirmed** | rows 21, 41 `TextBrush · Style` while disabled |
| `ChromeCheckBoxTemplate` / `ChromeRadioButtonTemplate` `IsEnabled=False` (App.xaml:511, 559) | same shape | **confirmed by structure, unrendered** | no disabled check/radio in the composed shell's rest state — the census omissions name it; the same one-line mechanism applies |
| `MenuItem` `IsEnabled=False` → DisabledTextBrush on the *item* (App.xaml:104, 153, 185) | Foreground on the MenuItem, inherited by AccessText's TextBlock | **confirmed by structure** | every menu header/item reads `TextBrush · Style` (rows 1–6, 82–…); no disabled item rendered in first-run — the mechanism is identical |
| `DockRoundedTabs.xaml` `IsSelected` → sunken ink for **every** selected tab | the trigger assumes the accent ground; selected-inactive is `#2A313B` | **confirmed** | experiment: 3 sites at 1.45:1 once the leaf style stops masking it |
| `NewSessionRailButton` / `ExploreRailButton` Foreground (MainWindow.xaml:78, 102) | container Foreground | **ruled out** | content is a `Path` stroked from `Foreground`, not a string — no generated TextBlock |
| `TextMutedBrush` at 47 sites (static sweep) | "muted token never measured" | **ruled out** as failures | every rendered one measures 6.48–7.46:1; kept as the operator-perception finding |
| `PromptBar.cs:45-46` `Brushes.White` ground under `TextBrush` text | hardcoded light ground, light ink → 1.22:1 if hosted | **ruled out for the population, flagged** | `Prompt.Root` is never added to any visual tree — unreachable UI (finding for hygiene, not this class) |
| `ContextMapSurface.cs:321`, `JoinSurface.cs:206`, `MainMenuBuilder.cs:61`, `ZoneRails.cs:128` `?? Brushes.Gray` | a token lookup that degrades to a literal when `Application.Current` is null | **ruled out at runtime, flagged** | the product always has an `Application`; the in-process floor host does not — one more axis on which the floor's population differs |
| `composer.html` / `composer-host.html` palette literals (`#1E1E1E`, `#D4D4D4`, `#9AA0A6`, `#7F858A`) | page CSS outside the tokens | **confirmed** | row 180 at 4.47:1; 23 rows on a ground that is not `{colors.surface}` |
| `TerminalColorScheme` / `TerminalPalette` literals (103 of the 168 literals the static sweep found) | a declared ANSI vocabulary, not a theme leak | **ruled out** | App.xaml's palette comment; the census names the terminal as a separate population |

**Markers harvested (CI9):** no `simplify:` or `assume:` markers exist in App.xaml,
DockRoundedTabs.xaml/.cs, DockThemeAccents.cs, SurfaceChrome.cs, ComposerSurface.cs, composer.html,
ContrastFloorTests.cs, ThemeProbe.cs, TokenDisciplineTests.cs or DESIGN.md. The unmarked belief that
became the bug: *"an implicit style on the leaf is the safe default for the base control set"* —
asserted in 5213d7bb's remarks without opening the precedence table (NG9).

## 9. Phased repair plan

| Phase | Repair item (code + tests) | Failure mode it eliminates | Validation | Depends on |
|---|---|---|---|---|
| 0 (done, this branch) | `tests/AiDe.App.ContrastProbe` + `ShellContrastCensusTests` (3 facts), red on `main`; wired into `AiDe.App.Tests` (so CI's `verify-test-run.py --only AiDe.App.Tests` runs it bare — DC-113) and into `AiDe.sln` | the floor's population ≠ the product's (DC-135, DC-131) | `dotnet test --filter ShellContrastCensusTests` → 3 red on `f5c0f740`; full suite 554/557 | — |
| 1 | **Fix A** — remove `Foreground` from the implicit `TextBlock`/`Label` styles; adjust the 18-type theory (leaf text types inherit — assert the *root* states the ink); + Fix B — `DockRoundedTabs.xaml` selected-**active** trigger; `DESIGN.md` palette table gains the on-accent and on-menu grounds | 12 sites at 2.37:1, 2 lost disabled states, 3 masked sites at 1.45:1 | census facts 1 and 2 green; the experiment's three sites ≥ 4.5; `ContrastFloorTests` green | 0 |
| 2 | **Class prevention in source** — extend `TokenDisciplineTests` (reuse, not a new script; `ui-craft-gate.py` reads an HTML corpus and cannot see XAML): (a) no implicit style for a leaf text type sets `Foreground`; (b) C# literal scan (`Brushes.X` except `Transparent`, `Color.FromRgb`, `new SolidColorBrush(`) outside the declared vocabularies (`TerminalColorScheme`, `TerminalPalette`, `DockThemeAccents`); (c) every `?? Brushes.Gray` fallback is a finding | the leaf-overrides-container shape recurring by authoring; hardcoded inks/grounds | the new checks red-first against `5213d7bb`'s App.xaml and today's `PromptBar.cs`, green after Phase 1 + the `PromptBar` disposition | 1 |
| 3 | **Fix C** — composer page tokens: host injects CSS variables on `host.init`; `composer.html`/`composer-host.html` reference them with fallbacks; `#drop-hint` → `--text-muted` | 1 site at 4.47:1; the second palette (U1's C2) | census fact 3 green; the page rows read `#12151A`/`#E4E9EF`/`#98A3B2` | 0; coordinate with `investigate/composer-input` |
| 4 | **Fix D** — `app.start` in the workbench log with `AssemblyInformationalVersion`; the string on the status strip tooltip / Help → About | the unattributed-binary class (proposed id 137): a screenshot triaged as a recurrence | a boot test reads `app.start` with a 40-hex sha; the probe's report already carries it | — |
| 5 | **Register** the two proposed classes (ids 136 and 137 — never highest-plus-one at that time; re-read the allocator), DC-135 recurrence 2, DC-131 instance; `docs/lessons/defect-classes.md` with the control named as the census + the Phase-2 rule | a lesson as prose | `verify-defect-register.py`, `verify-id-allocators.py` | 1–4 |
| 6 | **Census reach** — open a disabled check/radio and a disabled menu item in the census (the structurally-confirmed siblings) and the hover/selected-inactive states via the same triggers; note the split canvas state | the named omissions that share the mechanism | the omissions table shrinks; each new row measured | 1 |
| 7 | **Palette decision (operator):** `{colors.text-muted}` reads as unreadable at 6.48:1 on raised; propose a lighter muted (e.g. `#A9B3C1` ≈ 8:1, hand-computed) or reserve muted for metadata only — with the census table as the measurement | the operator's perception, not a floor failure | the census re-measured after the token change | — |

> **STOP — human review gate.** The census is committed red as evidence. No fix is made. Approve the
> phases to execute; Phase 1 is one line plus one trigger and the theory adjustment.

## 10. Residual risk & what would change the diagnosis

- **The launched binary.** If the operator ran `investigate-composer-input`'s 10:56 build
  (`1.0.0+f5c0f740`), H1 falls and the photographed sites would need a mechanism the census cannot
  see; the white `TextBox` argues strongly against it. Fix D makes the next report attributable.
- **Trigger states the census does not render** (hover, pressed, selected-inactive, disabled
  check/radio/menu item): confirmed by structure, not by pixels — Phase 6.
- **The ground sampler** takes the median of five points; a text sitting on a gradient or a border
  line is reported with a `(varies …)` note. No row carried one at HEAD.
- **`Application.ResourceAssembly`** is set by reflection in the probe when the public setter
  refuses (measured: WPF reads it before `Main`); a WPF build that renames the field fails the
  probe loudly, never silently.
- **The probe boots the product**: `AIDE_WORKSPACE_ROOT` is cleared, the session lives in a temp
  directory, the window is moved off-screen; `App.OnStartup`'s crash handlers would write to
  `%LOCALAPPDATA%\AiDe\logs` on a crash, as every test host already can.

## 11. Gate record

`GATE investigate · 2026-09-11 · SRE (diagnostician, lead) · Test Architect · UX & Accessibility · WPF styling lens · root cause verified: yes (necessary: setter removed → 12→0; sufficient: setter present → 12 at 2.37) · verdict: PASS with conditions · vetoes→resolution: Test Architect — "a floor that constructs its subjects is not proof of the product" → the census is the regression test and it was seen red; UX&A — the on-accent pairing must enter DESIGN.md's table → Phase 1; WPF lens — the leaf must not state its ink → Fix A, and the selected-inactive tab ground → Fix B; the investigator did not clear the diagnosis alone — the necessity experiment is the disconfirmation.`

---

## Appendix A — the census, verbatim (`f5c0f740`, `AiDe.App.ContrastProbe`, 2026-09-11)

AiDe.App 1.0.0+f5c0f740fd0488baa00dd2de4320c187f585058e · 180 pairings measured · 14 below floor

### Failing, by mechanism

- app-style token: 13
- page-css: 1

### Failing sites

| # | Ratio | Floor | Ink | Ink source | Ground | Ground source | Opacity | Size | Population | Surface | Element | Mechanism | Verdict |
|---|---:|---:|---|---|---|---|---:|---:|---|---|---|---|---|
| 17 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Graph | TextBlock “Graph” | app-style token | **FAILS** |
| 36 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census session | TextBlock “Census session” | app-style token | **FAILS** |
| 40 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Contexts | TextBlock “Contexts” | app-style token | **FAILS** |
| 42 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Joins | TextBlock “Joins” | app-style token | **FAILS** |
| 54 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census prompt | TextBlock “Census prompt” | app-style token | **FAILS** |
| 55 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | Show as a diagram | TextBlock “Diagram” | app-style token | **FAILS** |
| 61 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census classdiagram | TextBlock “Census classdiagram” | app-style token | **FAILS** |
| 64 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census sequence | TextBlock “Census sequence” | app-style token | **FAILS** |
| 68 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census search | TextBlock “Census search” | app-style token | **FAILS** |
| 70 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census codeviewer | TextBlock “Census codeviewer” | app-style token | **FAILS** |
| 73 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census diagnostics | TextBlock “Census diagnostics” | app-style token | **FAILS** |
| 75 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census session-document | TextBlock “Census session-document” | app-style token | **FAILS** |
| 80 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Move pane…” | app-style token | **FAILS** |
| 180 | 4.47 | 4.5 | `#7F858A` | page CSS rgb(127, 133, 138) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div#drop-hint “Attaching files is off for this session.” | page-css | **FAILS** |

### Every site

| # | Ratio | Floor | Ink | Ink source | Ground | Ground source | Opacity | Size | Population | Surface | Element | Mechanism | Verdict |
|---|---:|---:|---|---|---|---|---:|---:|---|---|---|---|---|
| 1 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “File” | app-style token | clears |
| 2 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “Edit” | app-style token | clears |
| 3 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “View” | app-style token | clears |
| 4 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “Window” | app-style token | clears |
| 5 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “Terminal” | app-style token | clears |
| 6 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “Help” | app-style token | clears |
| 7 | 15.62 | 4.5 | `#E4E9EF` | TextBrush · Local | `#0D1014` | SurfaceSunkenBrush | 1.00 | 15 | wpf | (window) | TextBlock “AI-DE desktop workspace” | token | clears |
| 8 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Reset workbench layout | TextBlock “Reset layout” | app-style token | clears |
| 9 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Workbench | TextBlock ‹Explore› in Border “No workspace is open. Open one to see Explore.” | token | clears |
| 10 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Provenance | TextBlock “Provenance” | app-style token | clears |
| 11 | 10.74 | 4.5 | `#E4E9EF` | TextBrush · Style | `#2A313B` | BorderBrush | 1.00 | 13 | wpf | tab: Explore | TextBlock “Explore” | app-style token | clears |
| 12 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Domain | TextBlock “Domain” | app-style token | clears |
| 13 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Sessions | TextBlock “Sessions” | app-style token | clears |
| 14 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Board | TextBlock “Board” | app-style token | clears |
| 15 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Leaderboard | TextBlock “Leaderboard” | app-style token | clears |
| 16 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Ledger | TextBlock “Ledger” | app-style token | clears |
| 17 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Graph | TextBlock “Graph” | app-style token | **FAILS** |
| 18 | 10.74 | 4.5 | `#E4E9EF` | TextBrush · Style | `#2A313B` | BorderBrush | 1.00 | 13 | wpf | tab: Terminal — pwsh | TextBlock “Terminal — pwsh” | app-style token | clears |
| 19 | 7.46 | 4.5 | `#98A3B2` | TextMutedBrush · Local | `#0D1014` | SurfaceSunkenBrush | 1.00 | 13 | wpf | (window) | TextBlock ‹Workspace health› “No workspace open.” | token | clears |
| 20 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Provenance | TextBlock “Provenance” | app-style token | clears |
| 21 | 10.74 | 4.5 | `#E4E9EF` | TextBrush · Style | `#2A313B` | BorderBrush | 1.00 | 13 | wpf | tab: Explore | TextBlock “Explore” | app-style token | clears |
| 22 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Domain | TextBlock “Domain” | app-style token | clears |
| 23 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Sessions | TextBlock “Sessions” | app-style token | clears |
| 24 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Board | TextBlock “Board” | app-style token | clears |
| 25 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Leaderboard | TextBlock “Leaderboard” | app-style token | clears |
| 26 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Ledger | TextBlock “Ledger” | app-style token | clears |
| 27 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | tab: Census daydreams | TextBlock “Census daydreams” | app-style token | clears |
| 28 | 10.74 | 4.5 | `#E4E9EF` | TextBrush · Style | `#2A313B` | BorderBrush | 1.00 | 13 | wpf | tab: Graph | TextBlock “Graph” | app-style token | clears |
| 29 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census session — composer | TextBlock “Send” | app-style token | clears |
| 30 | 13.57 | 3.0 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census session — composer | TextBlock “Attach file…” | disabled | clears |
| 31 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census session — composer | TextBlock “Compiled view” | app-style token | clears |
| 32 | 15.62 | 4.5 | `#E4E9EF` | TextBrush · Style | `#0D1014` | SurfaceSunkenBrush | 1.00 | 13 | wpf | Compiled prompt — exactly what will be sent | TextBox ‹Compiled prompt — exactly what will be sent› “(empty text box)” | app-style token | clears |
| 33 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census session — composer | TextBlock “Lease: not derivable until the draft names so…” | app-style token | clears |
| 34 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 12 | wpf | Census session canvas zone | TextBlock ‹Canvas› “Census session” | token | clears |
| 35 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 12 | wpf | Console | TextBlock “Lanes” | token | clears |
| 36 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census session | TextBlock “Census session” | app-style token | **FAILS** |
| 37 | 10.74 | 4.5 | `#E4E9EF` | TextBrush · Style | `#2A313B` | BorderBrush | 1.00 | 13 | wpf | tab: Terminal — pwsh | TextBlock “Terminal — pwsh” | app-style token | clears |
| 38 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Workbench | TextBlock ‹Provenance› in Border “No workspace is open. Open one to see Provena…” | token | clears |
| 39 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Contexts | TextBlock “No context map. Add docs/bounded-contexts.yam…” | token | clears |
| 40 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Contexts | TextBlock “Contexts” | app-style token | **FAILS** |
| 41 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Joins | TextBlock “No workspace. Open one and index it to see ho…” | token | clears |
| 42 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Joins | TextBlock “Joins” | app-style token | **FAILS** |
| 43 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Workbench | TextBlock ‹Domain› in Border “No workspace is open. Open one to see Domain.” | token | clears |
| 44 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Sessions | TextBlock “Session observation is not available — no wat…” | token | clears |
| 45 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Board | TextBlock “The message board is not available — no watch…” | token | clears |
| 46 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Leaderboard | TextBlock “The leaderboard is not available — no watcher…” | token | clears |
| 47 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Ledger | TextBlock “Work-episode observation is not available.” | token | clears |
| 48 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census daydreams | TextBlock “Daydreams are not available — no watcher stor…” | token | clears |
| 49 | 6.96 | 4.5 | `#5FB98F` | VerifiedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 11 | wpf | Census prompt | TextBlock in Border “staged — not sent” | token | clears |
| 50 | 13.57 | 3.0 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Transfer prompt to the selected session | TextBlock “Transfer →” | disabled | clears |
| 51 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 12 | wpf | Census prompt | TextBlock “to” | token | clears |
| 52 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 12 | wpf | Census prompt | TextBlock “Start or select a ready terminal session firs…” | token | clears |
| 53 | 15.62 | 4.5 | `#E4E9EF` | TextBrush · Style | `#0D1014` | SurfaceSunkenBrush | 1.00 | 13.5 | wpf | Prompt draft | TextBox ‹Prompt draft› “(empty text box)” | app-style token | clears |
| 54 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census prompt | TextBlock “Census prompt” | app-style token | **FAILS** |
| 55 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | Show as a diagram | TextBlock “Diagram” | app-style token | **FAILS** |
| 56 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Hide interfaces | TextBlock “Hide interfaces” | app-style token | clears |
| 57 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Show dependencies | TextBlock “Dependencies” | app-style token | clears |
| 58 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 14 | wpf | Census classdiagram | TextBlock “Class hierarchy” | token | clears |
| 59 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census classdiagram | TextBlock “No classes or interfaces in view.” | token | clears |
| 60 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 12 | wpf | Census classdiagram | TextBlock “Open a workspace with source code to see its …” | token | clears |
| 61 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census classdiagram | TextBlock “Census classdiagram” | app-style token | **FAILS** |
| 62 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 14 | wpf | Census sequence | TextBlock “Sequence diagram” | token | clears |
| 63 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census sequence | TextBlock “Select a node in the graph to see its calls i…” | token | clears |
| 64 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census sequence | TextBlock “Census sequence” | app-style token | **FAILS** |
| 65 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census search | TextBlock “Search workspace” | token | clears |
| 66 | 15.62 | 4.5 | `#E4E9EF` | TextBrush · Local/expr | `#0D1014` | SurfaceSunkenBrush | 1.00 | 13 | wpf | Search the workspace | TextBox ‹Search the workspace› “(empty text box)” | token | clears |
| 67 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census search | TextBlock “Type to search across the workspace — types, …” | token | clears |
| 68 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census search | TextBlock “Census search” | app-style token | **FAILS** |
| 69 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census codeviewer | TextBlock “Select a node to read its source.” | token | clears |
| 70 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census codeviewer | TextBlock “Census codeviewer” | app-style token | **FAILS** |
| 71 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 14 | wpf | Census diagnostics | TextBlock “Census diagnostics” | token | clears |
| 72 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 11.5 | wpf | Census diagnostics | TextBlock “No index has run this session. Run Re-index (…” | token | clears |
| 73 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census diagnostics | TextBlock “Census diagnostics” | app-style token | **FAILS** |
| 74 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Workbench | TextBlock ‹Census session-document› in Border “No session is open. Create one from File → Ne…” | token | clears |
| 75 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | tab: Census session-document | TextBlock “Census session-document” | app-style token | **FAILS** |
| 76 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Census session — composer | TextBlock “engineId: the send was refused: this session …” | app-style token | clears |
| 77 | 7.46 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#0D1014` | SurfaceSunkenBrush | 1.00 | 13 | wpf | (window) | TextBlock ‹Workbench status› “Command palette. 31 layout commands. Type to …” | token | clears |
| 78 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Command palette | TextBlock “Layout commands” | token | clears |
| 79 | 15.62 | 4.5 | `#E4E9EF` | TextBrush · Style | `#0D1014` | SurfaceSunkenBrush | 1.00 | 13 | wpf | Search layout commands | TextBox ‹Search layout commands› “(empty text box)” | app-style token | clears |
| 80 | 2.37 | 4.5 | `#E4E9EF` | TextBrush · Style | `#5B9DD9` | AccentBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Move pane…” | app-style token | **FAILS** |
| 81 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Resize pane…” | app-style token | clears |
| 82 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Float pane” | app-style token | clears |
| 83 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Collapse pane” | app-style token | clears |
| 84 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Maximize pane” | app-style token | clears |
| 85 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Next tab in pane” | app-style token | clears |
| 86 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Previous tab in pane” | app-style token | clears |
| 87 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Move tab left/right” | app-style token | clears |
| 88 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Close surface” | app-style token | clears |
| 89 | 14.98 | 4.5 | `#E4E9EF` | TextBrush · Style | `#12151A` | SurfaceBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Reset workbench layout” | app-style token | clears |
| 90 | 13.57 | 4.5 | `#E4E9EF` | TextBrush · Style | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Layout commands | TextBlock “Clear the status message” | app-style token | clears |
| 91 | 6.48 | 4.5 | `#98A3B2` | TextMutedBrush · Local/expr | `#1A1F26` | SurfaceRaisedBrush | 1.00 | 13 | wpf | Command palette | TextBlock “Up and Down to choose · Enter to run · Escape…” | token | clears |
| 92 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New session…” | app-style token | clears |
| 93 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+N” | template token | clears |
| 94 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Open a repository as a workspace…” | app-style token | clears |
| 95 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, O” | template token | clears |
| 96 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Index C# projects in this workspace” | app-style token | clears |
| 97 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, I” | template token | clears |
| 98 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Re-index everything (ignore the cache)” | app-style token | clears |
| 99 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, Shift+I” | template token | clears |
| 100 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Re-index this workspace” | app-style token | clears |
| 101 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, Ctrl+I” | template token | clears |
| 102 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “Recent sessions” | app-style token | clears |
| 103 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “Recent workspaces” | app-style token | clears |
| 104 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock in AccessText “Exit” | app-style token | clears |
| 105 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Alt+F4” | template token | clears |
| 106 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Move pane…” | app-style token | clears |
| 107 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, M” | template token | clears |
| 108 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Resize pane…” | app-style token | clears |
| 109 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, R” | template token | clears |
| 110 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Explorer: graph and reader” | app-style token | clears |
| 111 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, E” | template token | clears |
| 112 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Focus graph canvas” | app-style token | clears |
| 113 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, G” | template token | clears |
| 114 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Next tab in pane” | app-style token | clears |
| 115 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+PageDown” | template token | clears |
| 116 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Previous tab in pane” | app-style token | clears |
| 117 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+PageUp” | template token | clears |
| 118 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Move tab left/right” | app-style token | clears |
| 119 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+Shift+PageUp/PageDown” | template token | clears |
| 120 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Raise score dispute on the latest scored episode” | app-style token | clears |
| 121 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, Ctrl+U” | template token | clears |
| 122 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Search workspace” | app-style token | clears |
| 123 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, F” | template token | clears |
| 124 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New class diagram” | app-style token | clears |
| 125 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, M” | template token | clears |
| 126 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New sequence diagram” | app-style token | clears |
| 127 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, Q” | template token | clears |
| 128 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New code viewer” | app-style token | clears |
| 129 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, U” | template token | clears |
| 130 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Diagnostics” | app-style token | clears |
| 131 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, D” | template token | clears |
| 132 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Clear the status message” | app-style token | clears |
| 133 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, Ctrl+C” | template token | clears |
| 134 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Float pane” | app-style token | clears |
| 135 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, F” | template token | clears |
| 136 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Collapse pane” | app-style token | clears |
| 137 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, C” | template token | clears |
| 138 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Maximize pane” | app-style token | clears |
| 139 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, Z” | template token | clears |
| 140 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Close surface” | app-style token | clears |
| 141 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+W” | template token | clears |
| 142 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Lock/unlock layout” | app-style token | clears |
| 143 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, L” | template token | clears |
| 144 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Reset workbench layout” | app-style token | clears |
| 145 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, Ctrl+R” | template token | clears |
| 146 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New terminal” | app-style token | clears |
| 147 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, T” | template token | clears |
| 148 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New Claude Code session” | app-style token | clears |
| 149 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, A” | template token | clears |
| 150 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New GitHub Copilot session” | app-style token | clears |
| 151 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, G” | template token | clears |
| 152 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Dispatch prompt to terminal…” | app-style token | clears |
| 153 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, P” | template token | clears |
| 154 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “New prompt draft” | app-style token | clears |
| 155 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, D” | template token | clears |
| 156 | 14.30 | 4.5 | `#E4E9EF` | TextBrush · Style | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Show daemon, health and MCP diagnostics” | app-style token | clears |
| 157 | 6.83 | 4.5 | `#98A3B2` | TextMutedBrush · ParentTemplate | `#161A20` | MenuBackgroundBrush | 1.00 | 12 | wpf | (window) | TextBlock “Ctrl+K, D” | template token | clears |
| 158 | 6.31 | 4.5 | `#9AA0A6` | page CSS rgb(154, 160, 166) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | label “goal” | page-css | clears |
| 159 | 6.79 | 4.5 | `#F48771` | page CSS rgb(244, 135, 113) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | span.required “*” | page-css | clears |
| 160 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div.cm-content “(editable)” | page-css | clears |
| 161 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div.cm-line “(editable)” | page-css | clears |
| 162 | 6.31 | 4.5 | `#9AA0A6` | page CSS rgb(154, 160, 166) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | label “done_when” | page-css | clears |
| 163 | 6.79 | 4.5 | `#F48771` | page CSS rgb(244, 135, 113) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | span.required “*” | page-css | clears |
| 164 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div.cm-content “(editable)” | page-css | clears |
| 165 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div.cm-line “(editable)” | page-css | clears |
| 166 | 6.31 | 4.5 | `#9AA0A6` | page CSS rgb(154, 160, 166) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | label “not_in_scope” | page-css | clears |
| 167 | 6.79 | 4.5 | `#F48771` | page CSS rgb(244, 135, 113) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | span.required “*” | page-css | clears |
| 168 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div.cm-content “(editable)” | page-css | clears |
| 169 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div.cm-line “(editable)” | page-css | clears |
| 170 | 6.31 | 4.5 | `#9AA0A6` | page CSS rgb(154, 160, 166) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | label “tier” | page-css | clears |
| 171 | 6.79 | 4.5 | `#F48771` | page CSS rgb(244, 135, 113) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | span.required “*” | page-css | clears |
| 172 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | select “(select)” | page-css | clears |
| 173 | 6.31 | 4.5 | `#9AA0A6` | page CSS rgb(154, 160, 166) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | label “fan_out_cap” | page-css | clears |
| 174 | 6.79 | 4.5 | `#F48771` | page CSS rgb(244, 135, 113) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | span.required “*” | page-css | clears |
| 175 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | input “(input)” | page-css | clears |
| 176 | 6.31 | 4.5 | `#9AA0A6` | page CSS rgb(154, 160, 166) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | label “budget” | page-css | clears |
| 177 | 6.79 | 4.5 | `#F48771` | page CSS rgb(244, 135, 113) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | span.required “*” | page-css | clears |
| 178 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | input “(input)” | page-css | clears |
| 179 | 10.33 | 4.5 | `#D4D4D4` | page CSS rgb(212, 212, 212) | `#252526` | page CSS | 1.00 | 13 | webview | Census session — composer · https://aide.assets.invalid/composer.html | input “(input)” | page-css | clears |
| 180 | 4.47 | 4.5 | `#7F858A` | page CSS rgb(127, 133, 138) | `#1E1E1E` | page CSS | 1.00 | 11 | webview | Census session — composer · https://aide.assets.invalid/composer.html | div#drop-hint “Attaching files is off for this session.” | page-css | **FAILS** |

### Not measured

| Population | What | Reason |
|---|---|---|
| wpf | session document split state | population change only (MS1); the unsplit modes are each measured |
| wpf | combo drop-downs, tooltips, context menus | Popup visuals are not in the window's visual tree until opened; the composer's template picker is collapsed with no catalog |
| wpf | the New Session sheet | a separate window; ContrastFloorTests site 7 measures it |
| wpf | hover / pressed / selected-inactive states | the census reads the rest state; a trigger-only pairing is unmeasured here |
| terminal | TerminalView cells | GlyphRun renderer over the ANSI palette a child process chooses from; App.xaml's palette table is the pairing set |

### Log

-   skipped 4 text block(s): not visible
- default layout: +19 sites (19 total)
- add daydreams: applied
- add prompt: applied
- add classdiagram: applied
- add sequence: applied
- add search: applied
- add codeviewer: applied
- add diagnostics: applied
- add session-document: applied
- open session: Session “Census session” opened.
-   skipped 11 text block(s): not visible
- activate explore (view): +18 sites (37 total)
-   skipped 11 text block(s): not visible
- activate provenance (inspector): +1 sites (38 total)
-   skipped 11 text block(s): not visible
- activate contexts (contexts): +2 sites (40 total)
-   skipped 10 text block(s): not visible
- activate joins (joins): +2 sites (42 total)
-   skipped 10 text block(s): not visible
- activate graph (canvas): +0 sites (42 total)
-   skipped 10 text block(s): not visible
- activate domain (view): +1 sites (43 total)
-   skipped 10 text block(s): not visible
- activate sessions (sessions): +1 sites (44 total)
-   skipped 10 text block(s): not visible
- activate board (board): +1 sites (45 total)
-   skipped 10 text block(s): not visible
- activate leaderboard (leaderboard): +1 sites (46 total)
-   skipped 10 text block(s): not visible
- activate ledger (ledger): +1 sites (47 total)
-   skipped 10 text block(s): not visible
- activate census:daydreams (daydreams): +1 sites (48 total)
-   skipped 10 text block(s): not visible
- activate census:prompt (prompt): +6 sites (54 total)
-   skipped 11 text block(s): not visible
- activate census:classdiagram (classdiagram): +7 sites (61 total)
-   skipped 12 text block(s): not visible
- activate census:sequence (sequence): +3 sites (64 total)
-   skipped 12 text block(s): not visible
- activate census:search (search): +4 sites (68 total)
-   skipped 13 text block(s): not visible
- activate census:codeviewer (codeviewer): +2 sites (70 total)
-   skipped 13 text block(s): not visible
- activate census:diagnostics (diagnostics): +3 sites (73 total)
-   skipped 13 text block(s): not visible
- activate census:session-document (session-document): +2 sites (75 total)
-   skipped 13 text block(s): not visible
- activate session-document:20260911T000000Z-census (session-document): +0 sites (75 total)
-   skipped 13 text block(s): not visible
- activate terminal-1 (terminal): +0 sites (75 total)
-   skipped 13 text block(s): not visible
- session mode console: +0 sites (75 total)
-   skipped 13 text block(s): not visible
- composer configured + refusal: +1 sites (76 total)
-   skipped 11 text block(s): not visible
- command palette: +15 sites (91 total)
- menu _File: +14 sites (105 total)
- menu _Edit: +4 sites (109 total)
- menu _View: +24 sites (133 total)
- menu _Window: +12 sites (145 total)
- menu _Terminal: +10 sites (155 total)
- menu _Help: +2 sites (157 total)
- composer page ready: True
- webview2 controls in tree: 1
- webview https://aide.assets.invalid/composer.html: 23 text elements

## Appendix B — the necessity experiment (in-process host, implicit `TextBlock` ink setter removed, then restored)

```
167 pairings measured · 4 below floor
- inherited: 3
- page-css: 1

| # | Ratio | Floor | Ink | Ink source | Ground | Ground source | … | Surface | Element | Mechanism | Verdict |
| 12 | 1.45 | 4.5 | #0D1014 | SurfaceSunkenBrush · Inherited from ContentPresenter#TabTitle (ParentTemplateTrigger) | #2A313B | BorderBrush | … | tab: Explore | TextBlock "Explore" | inherited | FAILS |
| 19 | 1.45 | 4.5 | #0D1014 | SurfaceSunkenBrush · Inherited from ContentPresenter#TabTitle (ParentTemplateTrigger) | #2A313B | BorderBrush | … | tab: Graph | TextBlock "Graph" | inherited | FAILS |
| 28 | 1.45 | 4.5 | #0D1014 | SurfaceSunkenBrush · Inherited from ContentPresenter#TabTitle (ParentTemplateTrigger) | #2A313B | BorderBrush | … | tab: Terminal — pwsh | TextBlock "Terminal — pwsh" | inherited | FAILS |
| 167 | 4.47 | 4.5 | #7F858A | page CSS rgb(127, 133, 138) | #1E1E1E | page CSS | … | composer page | div#drop-hint | page-css | FAILS |
```

## Appendix C — the binaries

| Path | Built (−07:00) | `AssemblyInformationalVersion` | Contains `5213d7bb` |
|---|---|---|---|
| `C:\Projects\ai-de\src\AiDe.App\bin\Release\…\AiDe.App.dll` | 09:23:05 | `1.0.0+be68ca1c…` | **no** |
| `C:\Projects\ai-de-feature-exit-evidence\…\AiDe.App.dll` | 10:29:47 | `1.0.0+2a363f4f…` | **no** |
| `C:\Projects\ai-de-investigate-composer-input\…\AiDe.App.dll` | 10:56:20 | `1.0.0+f5c0f740…` | yes |
