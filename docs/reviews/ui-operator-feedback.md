---
id: "ui-review-operator-feedback"
title: "UI review — the session front door, from operator feedback"
type: doc
status: draft
owner: "@claude-ui-elevation"
phase: "phase-1-front-door"
tags: [ui-review, ux, accessibility, contrast, composer, information-architecture]
links:
  - { to: mockup-session-front-door, rel: relates-to }
  - { to: spec-app-facelift, rel: relates-to }
  - { to: review-ui-facelift, rel: refines }
  - { to: review-ui-activity-rail, rel: refines }
review-by: 2026-12-10
summary: >-
  Elevate-mode review of the AI-DE session front door against seven pieces of real operator
  feedback on the running app. Six of the seven are unimplemented specification or a measurable
  defect rather than a matter of taste; the contrast complaint is one systemic cause with
  eleven measured failing pairs. The single highest improvement-to-effort change is a set of
  implicit default styles in one file, which converts theme coverage from opt-in to opt-out.
---

# UI review — the session front door, from operator feedback

*Produced by `/ui-design` (mode: **elevate**). Governed by `ui-design-craft.md` DX22–DX25 over the
floors in `ui-interaction-design.md` U1–U20. Every finding carries location · dimension · severity ·
evidence · fix · confidence.*

**Surfaces reviewed:** the New Session sheet · the session document's composer pane · the canvas mode
strip · the activity rail · the theme layer beneath all of them · the default pane inventory.
**States inspected:** default, first-run/empty, loading, error, refused, overflow, disabled, focused.

**Triggered standards, mapped at Stage 1 (not at the gate):**

| Trigger | Fires? | Why, and what it changed here |
|---|---|---|
| **UI-T1** expert/quantitative | **Yes** | Cohort keys, event counts, ranks and medians. Archetype comes from catalog §G. Drove the tabular-numeral, unit-bearing and **bounded-count** treatments, and made the hard-coded `Verified` badge a *correctness* finding rather than a styling one. |
| **UI-T2** generated assets | **No** | No imagery, persona or motion is generated. Nothing under `docs/assets/` was produced, so VA1–VA22 are inert for this run. |
| **UI-T3** fronts a model | **Yes** | The composer dispatches to agent lanes. Added **refused / wrong-answer** to the state inventory as a first-class state, and made the permission and scope-refusal paths part of the surface rather than an error toast. |
| **UI-T4** native client | **Yes** | WPF, Windows, dark-only, unsigned local build. This is why the HTML mockup is **direction evidence only** and cannot clear native PASS, and why the contrast fix belongs in `App.xaml`, not in the mockup. |

---

## 1. Verdict

**Six of the seven feedback items are unimplemented specification or a measured defect. One is a
genuine design problem that was mine to solve.** Almost nothing here is taste.

The operator wrote seven complaints in file names. Read against the source and the specification, they
resolve as: one systemic theme-coverage defect with eleven measured failing pairs; one editor that is
fully built, vendored and navigated to and has **never initialized once**; one required field asking
for a closed vocabulary through an open text box; three inert rail placeholders beside one working
icon that looks identical to them; three tabs that are literally the same class instantiated three
times; and one canvas mode that hosts a terminal in the phase whose own exit evidence reads *"with
zero terminal hosting"*.

The design language was not the problem. `{colors.text}` on `{colors.surface}` measures **14.98:1**.
Every single failing pair involves a control that never reached a token.

**The single highest improvement-to-effort change:** a set of implicit `TargetType` default styles in
`src/AiDe.App/App.xaml`. One file, no behaviour change, mechanically verifiable, and it clears seven of
the eleven measured failing pairs while converting theme coverage from opt-in to opt-out so the *next*
control is correct by default. **The change the operator will notice first is different** — wiring the
composer — and conflating "highest leverage" with "most visible" would be dishonest, so the plan runs
them in that order and says why.

---

## 2. Measurements (DX23 — measure before you diagnose)

All ratios computed with the WCAG 2.x sRGB relative-luminance formula. Foreground values marked
*platform default* were obtained by instantiating each control in an STA runspace, showing a real
window, forcing layout and reading the resolved dependency properties — not recalled.

### 2a. The eleven failing pairs

| # | Ratio | Floor | Foreground | Background | Site | Class |
|---|---:|---:|---|---|---|---|
| 1 | **1.15** | 4.5 | `#000000` *(platform default)* | `#12151A` | `ComposerSurface.cs:100` — TextBlock **"Compiled view"** | unstyled ink |
| 2 | **1.15** | 4.5 | `#000000` | `#12151A` | `ComposerSurface.cs:81` — **"Lease: not derivable…"** | unstyled ink |
| 3 | **1.15** | 4.5 | `#000000` | `#12151A` | `ComposerSurface.cs:82` — `_status` | unstyled ink |
| 4 | **1.15** | 4.5 | `#000000` | `#12151A` | `CanvasSurface.cs:224` — empty-state text | unstyled ink |
| 5 | **1.15** | 4.5 | `#000000` | `#12151A` | `ConsoleSurface.cs:131,144` — TreeViewItem headers | unstyled ink |
| 6 | **1.27** | 4.5 | `#000000` | `#1A1F26` | `ContextMapSurface.cs:202` — group header | unstyled ink |
| 7 | **1.27** | 4.5 | `#000000` | `#1A1F26` | `NewSessionSheetDialog.cs:138` — CheckBox labels | unstyled ink |
| 8 | **1.22** | 4.5 | `#E4E9EF` | `#FFFFFF` *(platform default)* | `CommandPalette.cs:37` — ListBox | **the inverse** |
| 9 | **1.15** | 4.5 | `#000000` | `#12151A` | `ClassDiagramSurface.cs:788,957` — key `RaisedBrush` **undefined**, reference no-ops | silent no-op |
| 10 | **2.73** | 3.0 | `#525A63` *(muted composited at 50%)* | `#0D1014` | Rail icons, disabled — `MainWindow.xaml:102,121,140` + `App.xaml:271-273` | disabled-as-opacity |
| 11 | **1.39** | 3.0 | `#2A313B` | `#12151A` | `{colors.border}` on `{colors.surface}` | **declared deviation, not a finding** — see §5 |

**The operator's "or vice versa" is pair 8, and it was created by the previous fix.** Commit `7179493`
*"dark-on-dark list text"* added an implicit `ListBoxItem` style that sets `Foreground` and not
`Background` (`App.xaml:69`). The `ListBox` behind it kept the platform's white. A partial pairing
produced the inverse of the defect it repaired. That is the single most instructive measurement here.

### 2b. Passing pairs, recorded so the palette is exonerated

`{colors.text}` on `{colors.surface}` **14.98:1** · `{colors.text-muted}` on `{colors.surface}`
**7.16:1** · `{colors.accent}` **6.33:1** · `{colors.focus}` ring on raised **8.59:1** · enabled rail
icon **7.46:1**. **The tokens are good.** The defect is entirely one of reach.

### 2c. Theme-break measurements (legible, but wrong)

| Ratio | Pair | Site |
|---:|---|---|
| 13.47 | `#DDDDDD` platform button face on `#12151A` | Send, Attach file…, Create session, Cancel, Dismiss, Sign in |
| 18.29 | `#FFFFFF` platform TextBox face on `#12151A` | Compiled view, palette search |
| 12.82 | `#BCDDEE` platform checked-toggle face on `#12151A` | Console / Terminal / Split |
| 16.43 | `#F9F1EF` dialog caption on `#12151A` | New Session sheet — measured from a real screen capture |

These **pass** contrast and **fail** the design system. Reported honestly as theme breaks, not as
accessibility findings. The dialog caption is `#F9F1EF` with `#000000` text and a `#C75050` close
button because `DwmSetWindowAttribute(…, DWMWA_USE_IMMERSIVE_DARK_MODE, …)` is called **only** in
`MainWindow.xaml.cs:309-326`; both dialog windows are plain `new Window` and get the OS scheme.

### 2d. Structural counts

| Measurement | Value | Source |
|---|---|---|
| Implicit (`TargetType`-only) styles that exist | **6**, none for a text or input control | `App.xaml:52,57,69,176,200,220` |
| Base control types with **no** implicit style | **18** | §2a class column |
| Control instantiations with neither `Foreground` nor `Background` | **28**, across 11 files | measured sweep |
| UI declared in XAML vs C# | 3 XAML files; ~111 control instantiations, overwhelmingly code-behind | — |
| Literal hex outside the theme dictionary | **164** occurrences; **three parallel copies of the palette** (`CanvasPage.cs` 45, `composer.html` 18, `composer-host.html` 5, `DockThemeAccents.cs` 20) | — |
| Resource keys referenced but never declared | **2** (`SunkenBrush`, `RaisedBrush`) across 6 sites, failing silently | — |
| Surfaces in the default layout | **11** across 3 zones; **8** hidden behind tabs at launch; Right zone **empty** | `ZoneLayout.cs:131-165` |
| Surfaces with no open command anywhere | **10 of 11** — closing one means Reset layout | — |
| Distinct evidence panes rendering distinct data | **1** (Explore, Provenance and Domain are one class, three times) | `SurfaceContentFactory.cs:70-71` |
| Identical IPC round trips on startup | **3**, for three identical lists | `EvidencePaneViewModel.cs:110-111` |
| Composer `Configure` calls in `src/` | **0** | `ComposerSurface.cs:154-182` |
| `ComposerSendContext` constructions in `src/` | **0** (tests only) | — |
| Themes | **1**. The app is dark-only: no second dictionary, no toggle, no `ThemeMode` | — |

### 2e. Gate measurements

| Gate | Result |
|---|---|
| `design-lint.py DESIGN.md --strict` | **exit 0**, clean, before and after this change |
| `ui-craft-gate.py docs/mockups --markdown` | **exit 0** — and it exited 0 *before* this change too, **with 13 Majors present** |
| `ui-craft-gate.py docs/mockups --gate` | **exit 0** — `--gate` fails only on **Blocker**-mapped findings |
| `ui-craft.yml` | Runs the craft gate **explicitly advisory and non-blocking**, by design, with the reason stated in the workflow |
| `xaml-token-lint.py` | Wired into **no** CI job. Run manually over `src/`: **exit 1, 91 findings** — of which the colour findings are the token *declarations themselves* and four `Background="Transparent"` attributes. **Not one finding corresponds to an actual contrast failure**, and it cannot read C#, where 100% of the defect lives |

---

## 3. Findings

Ordered **structure before surface** (DX24). Severity 4→Blocker, 3→Major, 2→Minor, 1→Nit. An
accessibility finding at ≥3 is a Blocker under U16 regardless of usability impact.

### Structure — archetype fit, information architecture, flow

| # | Location | Dimension | Sev | Evidence | Fix | Conf |
|---|---|---|---|---|---|---|
| **S1** | `App.xaml` | Token discipline | **4** | Only 6 implicit styles exist, none for a text or input control. 18 base types fall back to WPF's light defaults. 28 instantiations currently do. Measured: pairs 1–9. | Implicit `TargetType` defaults for the base set, setting **ink and ground together** (TC2). | Verified |
| **S2** | `ComposerSurface.cs` + `SessionDocumentSurface.cs:74` | Archetype fit | **4** | The pane's largest, brightest element is `_compiled` (`:71-78`), a white `IsReadOnly` TextBox. The serial-entry half of the surface has no focal point at all. | The composer card owns the bottom of the pane; compiled view becomes a disclosure. | Verified |
| **S3** | `SessionDocumentSurface.cs:74`; `ComposerSurface.cs:277,461`; `composer.mjs:208-217` | Flow | **4** | **Two independent defects stacked.** (a) `ComposerSurface.Configure` (`:154-182`) has **zero callers**; the only construction never calls it, and `WorkbenchShell.cs:2801` deliberately drops the run-side inputs (`:2795-2799`). (b) Even configured, the handshake deadlocks: `PushInit` is the only sender of `host.init` and fires only from `MarkReady`, which the router raises only on `editor.ready` (`ComposerMessageRouter.cs:204,236`) — while `composer.mjs:217` posts `editor.ready` **inside** the `host.init` handler. Host waits for page; page waits for host. `CanvasSurface.cs:91` has the `NavigationCompleted` handler that would break it; the composer does not. | Call `Configure`; push `host.init` from `NavigationCompleted`. **Both, or the pane stays blank.** | Verified |
| **S4** | `NewSessionSheetDialog.cs:69` | IA / input model | **4** | `var taskClass = new TextBox { … }` — free text, no options, no placeholder, no autocomplete, for a value whose only use is exact string equality against a cohort key. Gate is non-blank only (`NewSessionSheetViewModel.cs:216-233`). | Bounded picker, nothing pre-selected. | Verified |
| **S5** | `SurfaceContentFactory.cs:70-71`; `EvidencePaneViewModel.cs:110-111` | IA | **4** | `"view"` and `"inspector"` both map to `f.Evidence(s)`. The builder takes no discriminator; the view model takes none either. Explore, Provenance and Domain issue the **identical** `FindAsync("")` and render through the identical template. Three tabs, one class, three identical IPC calls at startup. The strings `"explore"`, `"provenance"` and `"domain"` appear **nowhere** in `src/AiDe.App/`. | See §7 item 9 — each of the three has a real, specified job that lives elsewhere. | Verified |
| **S6** | `EvidencePaneViewModel.cs:116` | Correctness (provenance) | **4** | `ConfidenceBadge.For(VerificationStatus.Verified)` — unconditional, for every row. In a product whose own design language states that an inferred edge rendered identically to an extracted one is `GRAPH-PROVENANCE-LAUNDERED`. The badge is never rendered visually, so it survives **only in the accessibility name** — a false claim made exclusively to screen-reader users. | Render real confidence as glyph + word + colour; never synthesize one. | Verified |
| **S7** | `CanvasModeCatalog.cs:64`; `TerminalSurface.cs:45-46` | Archetype fit / phasing | **3** | The Terminal canvas mode constructs `new TerminalSurface($"session-terminal:{SessionId}")`; that constructor **starts** a ConPTY session. Addendum A A6.1 specifies a view onto *"observed lanes' live terminals"* — terminals that already exist — and Phase 1 binds none. Phase 1's own exit evidence reads *"with zero terminal hosting"*. | **Resolved by Ruling 45** — Console-only. Designed, §6. | Verified |
| **S8** | `ZoneLayout.cs:131-165`; `LayoutMigrations.cs:28-48` | IA | **3** | 11 surfaces, 8 hidden behind tabs at launch, Right zone empty, 10 of 11 with no open command. The migrations show the mechanism: v1→v2 added Joins, v2→v3 added Board + Leaderboard, v3→v4 added Ledger — each appended beside an anchor, never re-designed. The inventory is an accretion log. | A pane budget and a zone job per zone; see §7 item 9. | Verified |
| **S9** | `EvidencePaneViewModel.cs:111` | Correctness (bounded read) | **3** | Passes `ProjectionService.MaxNeighborsCeiling` (**50**) as `FindAsync`'s `maxResults`. `ProjectionService.cs:297-312` documents this exact mistake being found and fixed, and created `MaxSearchResultsCeiling = 20_000`. The sweep reached Contexts and Joins and **missed this pane**. Status line reads `50 item(s)`, indistinguishable from complete. | Use the right ceiling; render the cap as `≥ N (capped)`. **Unswept instance of an already-registered class.** | Verified |

### States and completeness

| # | Location | Dimension | Sev | Evidence | Fix | Conf |
|---|---|---|---|---|---|---|
| **B1** | `ComposerSurface.cs` | State completeness | **3** | No first-run, loading, error or refused state. The screenshot's blank 70% of the pane *is* the empty state, by omission. Under UI-T3 the refused/wrong-answer path is a required state, and it does not exist. | The seven states in DESIGN.md's new composer matrix. | Verified |
| **B2** | `NewSessionSheetViewModel.cs:216-233` | State / copy | **3** | `BlockedReason` exists and is correct, but renders in a footnote under the buttons, in the vocabulary of the ranking subsystem (*"ranks in the wrong cohort"*), 200px from the field it is about. The operator asked "why is it mandatory" while the answer was on screen. | RQ3 + RQ5: consequence copy at the field, reason adjacent to the disabled button. | Verified |
| **B3** | `SurfaceContentFactory.cs:75-78` | Dead surface | **2** | Kind `daydreams` is registered with a full view model behind it, is in no default layout and has no open command. Unreachable in the running product. | Delete or route to it. | Verified |

### Accessibility (U16 — an accessibility finding at ≥3 is a Blocker)

| # | Location | Dimension | Sev | Evidence | Fix | Conf |
|---|---|---|---|---|---|---|
| **A1** | `App.xaml:266-268` + `MainWindow.xaml:84,103,122,141` | WCAG 2.4.7 | **4** | The focus trigger sets `BorderBrush` to `FocusBrush`. Every rail button sets `BorderThickness="0"`, so the ring has zero width and **renders nothing**. `App.xaml:243-245` comments that *"focus draws a visible ring (WCAG 2.4.7)"* — a comment stating a property the code does not have. | The ring is its own 2px outline, not a recoloured border (AR4). | Verified |
| **A2** | 7 sites, §2a pairs 1–7, 9 | WCAG 1.4.3 | **4** | 1.15:1 and 1.27:1. Text is not merely low-contrast; it is invisible. | S1. | Verified |
| **A3** | `CommandPalette.cs:37` | WCAG 1.4.3 | **4** | 1.22:1, light ink on platform white, caused by the previous partial fix. | S1 + TC2. | Verified |
| **A4** | `App.xaml:271-273` | WCAG 1.4.11 | **3** | Disabled = `Opacity 0.5` on `TextMutedBrush` over sunken = **2.73:1** against a 3:1 floor. Hover changes only the background, so hovering never brightens the glyph. | Disabled is a token pairing that clears the floor, not an opacity (DESIGN.md, "Disabled is a state"). | Verified |
| **A5** | `MainWindow.xaml:65-68` | Keyboard | **3** | `TabNavigation="Once"` + `DirectionalNavigation="Cycle"` has exactly **one** participant, because 3 of 4 buttons are disabled and unfocusable. `docs/reviews/ui-activity-rail.md` records this as *done*. It is inert. Separately, `_mode.Toggle()` has **one caller** and no catalog command, so Explorer mode has **no keyboard route at all**. | AR5: a catalog command gives menu + palette + chord in one change. | Verified |
| **A6** | `NewSessionSheetDialog.cs:32`, `TextPromptDialog.cs:32` | Theme / platform | **3** | Neither dialog opts its caption into DWM dark mode; only `MainWindow` does. Measured caption ground `#F9F1EF`. | TC4. | Verified |

### Craft and token discipline (Major minimum under CD12)

| # | Location | Dimension | Sev | Evidence | Fix | Conf |
|---|---|---|---|---|---|---|
| **C1** | 6 sites | Token discipline | **3** | `SunkenBrush` and `RaisedBrush` are referenced and **never declared**. A resource reference to a missing key is a silent no-op — no exception, no log, visually identical to "not themed yet". Produced a white TextBox and a transparent card. | TC3 + a key-existence check. | Verified |
| **C2** | `CanvasPage.cs`, `Web/composer.html`, `Web/composer-host.html`, `DockThemeAccents.cs` | Token discipline | **3** | Three parallel copies of the palette, 88 literals, each against a themed ground, each drifting independently. | TC5. | Verified |
| **C3** | `PromptBar.cs:45-46`; `ContextMapSurface.cs:321`, `JoinSurface.cs:206`, `MainMenuBuilder.cs:60`, `ZoneRails.cs:128` | Token discipline | **3** | `Brushes.White` as a panel ground inside a dark app; `Brushes.Gray` as the **fallback when a token lookup fails** — the failure mode of TC3 made permanent. | TC1/TC5; a failed lookup is a build failure, not a grey. | Verified |
| **C4** | `xaml-token-lint.py`; `ui-craft.yml` | Control | **3** | The only tool pointed at `src/` is wired into no CI job, cannot read `.cs` (where the whole defect lives), has no concept of an implicit style, does no contrast maths, and would pass a reference to an undeclared key. Its 91 findings are dominated by flagging the palette's own declarations. **A control whose signal is noise relative to the defect it exists to catch.** | §7 item 14. | Verified |
| **C5** | `docs/mockups/*.html` | Craft | **2** | Existing mockups state contrast ratios as **static prose** (`accent on menu-bg 7.1:1 AA`) rather than computing them, contrary to the harness template's own instruction. This is the artifact-level form of the same failure as the product's. | Fixed in the new mockup: 16 pairings computed live from resolved styles, recomputed on theme change. | Verified |
| **C6** | `ComposerProbe/Program.cs:154-170` | Test integrity | **3** | The probe posts `host.init` immediately after `NavigationCompleted`, under the comment *"The host pushes the init the page waits for, exactly as the surface does."* **The surface does not.** The harness supplies the message production is missing, and its comment asserts the opposite. This is why S3 was green. | The probe drives the production path, or it proves nothing. | Verified |

**Total at severity ≥ Major, against the running build: 21** (9 structure, 2 state, 6 accessibility,
5 craft counting C6; C5 and B3 are Minor). This number is the *deliverable* — it cannot decrease by my
action, because this node is read-only on `src/`. The loop variant is in §8.

---

## 4. Scorecard by dimension

| Dimension | Score | Note |
|---|---|---|
| Archetype fit | **2 / 5** | The workbench archetype is right. The composer's shape inside it is not: a serial-entry task given no focal point, with the read-only output as the brightest element. |
| Information architecture | **1 / 5** | Three tabs are one class. Eleven surfaces, ten with no door back. The layout is an accretion log. |
| Flow integrity | **1 / 5** | The primary flow — write a block and send it — cannot be completed. The editor never initializes. |
| State completeness | **2 / 5** | Empty, loading, error and refused are absent on the composer. `BlockedReason` and the Wayfinder empty states elsewhere are real and good. |
| Accessibility | **1 / 5** | Two Blockers on contrast, one on focus visibility, one keyboard route that does not exist. Hard veto not cleared. |
| Token discipline | **1 / 5** | Six implicit styles, eighteen types uncovered, three palette copies, two undeclared keys failing silently. |
| Copy | **3 / 5** | Honest and specific throughout, and often good (*"Nothing joined. That is a real answer."*). Its failure is placement and vocabulary, not truth. |
| Motion | **4 / 5** | The 0ms layout inventory is deliberate, documented and correct. Nothing to reduce is the right answer for a workbench. |
| Visual craft | **3 / 5** | The palette, type scale and soft-island direction are genuinely good and measurably AA at the token layer. They just do not reach the controls. |

**UX & Accessibility verdict: BLOCK.** Four accessibility findings at severity ≥3. **The author does
not clear this veto.** It clears when: pairs 1–9 measure ≥4.5:1 in the running app under UI Automation
or an accessibility inspector (not in a mockup, per UI-T4); the rail focus ring is visible on screen;
and Explorer mode has a keyboard route. A clean `ui-craft-gate.py` run does not clear it and never
could — the detector reads `docs/mockups/`, and not one of these findings lives there.

---

## 5. Generic-tells self-check (DX3)

| Tell | Present? | Disposition |
|---|---|---|
| Side-tab accent border | Yes, once | **Deliberate.** The 3px active bar on the rail is the literal VS Code cue and is the non-colour half of the active-state signal. Kept, with the glyph colour and background as the other two signals. |
| Nested cards | One level | **Declared deviation.** The soft-islands register *is* a card inside a pane. |
| Cramped padding on dense strips | Yes, 4 | **Declared deviation.** 8px vertical inset is arithmetically impossible in a 28px row holding a 24px control. `Density:Compact` is the archetype's. |
| Flat type hierarchy | No | 12 / 13 / 15 / 18 / 22 px in use; ratio 1.83:1. Cleared. |
| Em-dash saturation | No | 35 in the first draft, **0** now. Genuine rewrite, not suppression. |
| All-caps small text | **Removed** | The detector flagged four sections. DESIGN.md asks for 12px / 600 / 0.04em section headers and never asked for uppercase. Removing it is a legibility improvement, so it was fixed rather than suppressed. |
| Hairline border + wide soft shadow | **Removed** | The dialog keeps `{elevation.dialog}` and drops its 1px outline. Two separation signals where one does the job. |
| Purposeless motion | No | One animation in the design region: the editor caret, which carries information (this is where typing goes). Everything else is 0ms. Reduced-motion collapses the caret to solid. |
| Centred narrow reading column | No | Rejected explicitly; this is a dense workstation, not a document. |
| Chat bubbles / avatars | No | Rejected explicitly in the anti-goals despite "like the Claude application" — the notebook block, not the chat turn, is the unit. |
| Gradient hero / sparkle button | No | Absent. |

---

## 6. What was designed, per feedback item

**1 — "Not sure what Task Class is here and why it is mandatory."**
**Asked twice in one session** — the standalone screenshot, and again as *"adding a session, still
dont know why the task class exists"*. A question repeated after the answer was on screen is evidence
about the affordance, not about the operator, and it raises this from a copy edit to a control change.
*Diagnosis:* the requirement is correct and load-bearing; the control is wrong for it. A wrong class
costs more than the helper text admits: `Leaderboard.cs:143` scopes by record equality, so one typo
forms a cohort of one, every facet fails `cohort < 5` (`:169`) and renders `Not Comparable`, the real
cohort **silently loses the episode** from its median, and `StandingComposer` finds no predecessor so
the trend renders *absent* (`:320`) — on the one surface whose job is telling an agent whether it is
improving. An uncalibrated class also zeroes the model-judged dimensions (`AdvisoryScoring.cs:205`),
splits one recurring failure into two never-recurring ones in the daydream signature
(`DaydreamObservation.cs:60`), and turns a successful governed run into **exit 1**
(`ConductorEntry.cs:88-93`). The shipped copy warns only about *defaulting*; **typo risk is the larger
exposure and is unmentioned.**
*Direction:* keep it required, keep it undefaulted, and make the answer obvious. Six named options with
one line each saying what the choice compares this session against; the explanation moved to the field
and rewritten in consequence language (RQ2); the required state made visible and flipping to *Answered*;
the disabled Create button carrying its reason in the same row; and the operator's own last answer
offered as a **one-click suggestion that is not a preselection** — which is how this satisfies Addendum
A A4.3's *"one click on sensible defaults"* without violating Ruling 19's *no default*, and keeps the
reflective `AssertNoDefaultFor(typeof(GovernedRunRequest), "TaskClass")` test green.
*For U2:* a bounded picker. The vocabulary source already exists and is **indexed**:
`ix_scored_episode_task (task_class, schema_version)` at `SqliteWatcherObservationStore.cs:1259`. Note
that a controlled vocabulary is already **owed to Phase 3** (`conductor-programme.md:392`,
`LaneCohort.cs:57`, `ADR-0028:79`), so this is bringing an owed thing forward, not inventing one — and
the referenced spec §8.4 does not yet exist.

**2 — the terminal in the session pane (now live under Ruling 45).**
*Diagnosis:* the operator was right, **and right against the specification, not against a ruling**.
A6.1's Terminal row specifies a view onto *existing* observed lanes; `CanvasModeCatalog.cs:64`
constructs a new one and `TerminalSurface.cs:45-46` starts it. Phase 1 binds no observed lanes and its
exit evidence says *"with zero terminal hosting"*. The ratification note already cut Artifacts,
Profiler and Board for exactly this reason — *"a tab with nothing behind it is dead UI"*
(`addendum-a-ratification.md:37`). **Ruling 45 applies that same rule to the row that was left in.**
*Direction:* the strip is **one 28px row whose geometry never changes**; only its population changes.
At one mode it renders the pane title — the form every other pane in the workbench uses — so it reads
as native rather than as a tab bar with one tab. At two or more it renders the standard tab strip and
the split control appears, bound to `CanvasModeCatalog.All.Count`, with no factory edit. Neither form
is a degraded version of the other. All three sizes are rendered simultaneously in the mockup's
**Canvas** surface, and the harness switches the composer's live strip between them.
*For U2:* `BuiltIn` drops to the Console row; the split control's visibility binds to catalog size; the
split **mechanism** stays and is re-proven against a test-registered mode. `TerminalSurface` and the
Dispatch and Terminal trees are untouched.

**3 — "none of these icons do anything."**
*Diagnosis:* **partly refuted, and the refutation matters.** Three of four are
`IsEnabled="False"` placeholders whose tooltips read *"not in this build"*, and the codebase already
documents them as awaiting this decision. The fourth, Explorer, works — and is the **only door to
Explorer mode in the entire product**. Deleting all four, as written, would remove the Knowledge
Explorer. The operator could not tell them apart because at rest the working one is the same 44×44
pill in the same muted foreground; the only differentiator appears *after* you press it.
*Direction:* the rail holds **destinations**; a verb does not join them. New Session takes an
accent-filled slot **above the group, separated by a divider** — the one place DESIGN.md reserves the
accent for a primary action.
*The requested behaviour conflicts with a settled decision, and I did not resolve it silently.* The
Explorer icon swaps `ContentControl.Content` over a **two-value** `ShellViewMode` enum, and it is
full-**body**, not full-window: menu bar, title strip, rail and status strip all remain. A session
today opens as a **dock document**, and A4.4 + ADR-0017 say so explicitly: *"Sessions are dock
documents inside the Workbench, not a third shell mode."* So "a full window view like the explorer
icon does" cannot be delivered by pointing a button at the existing path.
*Recommended resolution:* deliver the felt experience through the **`maximized` dock state DESIGN.md
already defines** — *"fills the tree; siblings are temporarily minimized and remembered as such"* — so
New Session creates the document and maximizes it. Same experience, no third shell mode, no ADR
reopened. **This needs an Owner nod before U2 builds it**; the alternative is extending `ShellViewMode`,
which is a larger change against a settled ADR.
*For U2:* delete three buttons; add the primary; add a catalog command for Explorer.
**`RailButtonsDoNotLieTests` asserts `buttons.Count >= 4` (`:64-66`) and will go red** — after removing
three, only Reset-layout and Explorer remain. That assertion needs rewriting to count *rail* buttons
specifically, not any button in the window.

**4 — "text is not readable… common issue in app."** See §2a and §7 item 1. The systemic cause is
named at the token level in DESIGN.md as **TC1–TC6**.

**5 — "there is no active text editor for me to type into."**
*Diagnosis:* **a real CodeMirror 6 editor is vendored (558 KB), copied to output, and navigated to —
and has never initialized once.** Two stacked defects, §3 finding S3. What the operator saw is the only
element that did paint: the WPF `TextBox` that is the **read-only compiled view**. It is white because
it never got a themed style, and it is the brightest thing on the pane, so it reads as the editor. Both
complaints, 5 and 6, are the same defect seen from two angles.
*For U2:* both halves, plus the probe's false comment (C6).

**6 — "super ugly and not ergonomic… like a notebook or like the Claude application."**
*Diagnosis:* not taste — **unimplemented P0 specification.** A5/R15 already specifies markdown-live
editing with highlighted fences, paste-to-fence, attach, an @-mention picker whose selections
materialize as visible recipe chips, Message and Goal-block submission shapes with round-trip promotion,
staged-send semantics, and *"the block outline (the Score)"*. The operator independently asked for the
thing the spec already requires, and named the same reference the spec names.
*Direction:* **the composer is a document, not a text box.** Direction adjectives: *composed,
instrumented, deliberate* (against *chatty, decorative, eager*). From the Claude app: the composer as a
card that owns the bottom of the pane, attachments as visible chips, one explicit send. **Not** taken:
chat bubbles, avatars, the centred reading column. From notebooks: the **block** as the unit with
per-block actions and an outline. **Not** taken: the execution-count gutter, the per-cell toolbar, the
kernel metaphor. See DESIGN.md's region table and the seven-state matrix.
*One deviation, flagged rather than reinterpreted:* A5 says *"Below the editor: the block outline"*. The
mockup renders **Score above, composer card pinned below**, matching the Claude application the operator
named and the current build. The design does not depend on which way this resolves — the Score is a
distinct region either way — but **this is an erratum candidate for the Owner, not a decision I made.**

**7 — "Explore-Provenance-Domain all seem to show the same information."**
*Diagnosis:* **the operator understated it. They are the same class instantiated three times** —
`"view"` and `"inspector"` both map to `f.Evidence(s)`, the builder takes no discriminator, and all
three issue the identical `FindAsync("")`. Field overlap is 100%. Independently confirmed by the
windowing investigation, which measured the three lists as **byte-identical**.

**The question that had to be answered before recommending removal** — *were they always meant to
differ, or does the IA have three names for one thing?* — resolves, with evidence, to **the first.**
All three were specified to differ, and two of them have working code for the job they lost:

| Pane | Its specified job | Does code for that job exist? | Why it does not run |
|---|---|---|---|
| **Provenance** | The *detail* half of a master-detail screen, `phase-1-walking-skeleton.md:355-357` | **Yes** — `EvidencePaneViewModel.SelectAsync` builds exactly the four specified sections | Its one caller is bound to nothing in the current shell; the workbench list has no selection handler |
| **Domain** | Aggregate roots, entities, value objects, hierarchy, US-2 (`ai-native-ide.md:267-284`) | **Yes** — `ClassDiagramSurface`, kind `classdiagram`, openable by command | The tab captioned Domain is wired to kind `view` |
| **Explore** | Filterable evidence list — the *master* half | Yes, and this is the one that runs | Duplicated by the full-window Explorer rail mode; flagged in `session-contracts.md:301` and still open |

**So the operator's own suggested fix is backwards.** *"Maybe just explore is needed and we get rid of
domain and provenance"* would delete the two panes that have specified, implemented jobs and keep the
one that is genuinely redundant with a surface reached from the rail. **Deleting Provenance and Domain
destroys capability; deleting Explore destroys a duplicate.** The IA does not have three names for one
thing — it has one thing standing where three used to be, because two selection wires were dropped and
nothing failed.

Not redundant by design: **redundant by decay.** Each had a real, specified job that now lives
elsewhere:
- **Provenance** is specified as the *detail* half of a master-detail screen
  (`phase-1-walking-skeleton.md:355-357`). `SelectAsync` implements exactly that and has **one caller**,
  bound to nothing in the current shell. The specified master-detail was split into two sibling **tabs
  in the same stack** — which cannot be master-detail, since only one tab is visible — and then the
  selection wire was dropped, leaving two copies of the master.
- **Domain** is specified in US-2 and that surface **exists** as kind `classdiagram`. The tab captioned
  Domain is wired to `view`.
- **Explore** is semantically redundant with the full-window Explorer rail mode, already flagged in
  `session-contracts.md:301` and still open.

*Direction:* **a pane is a job, not a caption.** Two panes that issue the same query are one pane.
Concretely: Explore leaves the default layout; **Provenance moves to the empty Right zone as the
selection-bound inspector**, which turns two duplicate tabs into the master-detail the spec actually
asked for and fills the one zone that is empty; Domain re-points to `classdiagram`. Net: the Left zone
becomes one evidence list plus Contexts and Joins.

---

## 7. Ranked plan (DX25)

### Must fix — blockers

| # | Change | Why here | Touches |
|---|---|---|---|
| **1** | **Implicit `TargetType` default styles in `App.xaml`** for the 18 base control types, setting **ink and ground together**. | ★ **The single highest improvement-to-effort change.** One file. No behaviour change. Clears 7 of 11 measured failing pairs and, more importantly, converts theme coverage from opt-in to opt-out so the *next* control is correct by default. Everything else on this list is one surface; this is the class. | `App.xaml` |
| **2** | **Wire the composer.** Call `Configure` from `SessionDocumentSurface.cs:74` (and stop dropping the run-side inputs at `WorkbenchShell.cs:2801`), **and** push `host.init` from a `NavigationCompleted` handler the way `CanvasSurface.cs:91` does. | **The change the operator notices first.** Fixing either half alone still leaves a blank pane. Also fix the probe comment at `ComposerProbe/Program.cs:154-170`, which is why this was green. | `SessionDocumentSurface.cs`, `WorkbenchShell.cs`, `ComposerSurface.cs`, the probe |
| **3** | **Task class becomes a bounded picker**, nothing pre-selected, sourced from `ix_scored_episode_task`; RQ2–RQ6 copy. | A typo here is worse than a default and the current copy does not mention it. Keeps the reflective no-default tests green. | `NewSessionSheetDialog.cs`, `NewSessionSheetViewModel.cs` |
| **4** | **The rail focus ring becomes a real 2px outline.** | WCAG 2.4.7. The current trigger recolours a zero-width border, so it renders nothing — and a code comment claims otherwise. | `App.xaml`, `MainWindow.xaml` |
| **5** | **Declare `SunkenBrush` / `RaisedBrush`, or fix the 6 references**, and add a check that every referenced key exists. | A silent no-op is indistinguishable from "not themed yet". ~15 lines of check would have caught both at authoring time. | 6 sites + a control |
| **6** | **Stop hard-coding `Verified`** at `EvidencePaneViewModel.cs:116`. | Provenance laundering, in the product whose design language forbids it by name, told only to screen-reader users. | `EvidencePaneViewModel.cs` |

### Should fix next

| # | Change | Note |
|---|---|---|
| **7** | **Console-only mode strip** per MS1–MS5 (Ruling 45). | `BuiltIn` drops the Terminal row; split visibility binds to catalog size; the split mechanism is re-proven against a test-registered mode. Do not touch `Dispatch/`, `Terminal/` or `TerminalSurface.cs`. |
| **8** | **Rail:** delete three placeholders, add the New Session primary, give Explorer a catalog command. | **Rewrite `RailButtonsDoNotLieTests`' `Count >= 4` guard first** or it goes red for the wrong reason. The full-window question in §6 item 3 needs an Owner nod. |
| **9** | **IA:** Explore out of the default layout; **Provenance to the empty Right zone as the selection-bound inspector**; Domain re-pointed to `classdiagram`. | Turns three identical tabs into the master-detail the spec specified, and fills the only empty zone. Add a test that `view` and `inspector` render different things — none exists. |
| **10** | `MaxSearchResultsCeiling` at `EvidencePaneViewModel.cs:111`; render a cap as `≥ N (capped)`. | Unswept instance of a class the repo already registered and fixed elsewhere. |
| **11** | DWM dark caption for both dialog windows, via a shared factory. | TC4. |
| **12** | The composer's seven hard states. | Depends on 2. |

### Worth doing

| # | Change | Note |
|---|---|---|
| **13** | Collapse the three palette copies; hand tokens to the hosted web surfaces instead of restating them. | TC5. 88 literals. |
| **14** | **Make the controls real.** A C#-aware check for a control instantiated with neither brush nor style; a key-existence check; and either flip `ui-craft.yml` to fail on Majors or record why it does not. | *A lesson recorded as prose is a memoir.* Today the only tool aimed at `src/` runs in no job, cannot read `.cs`, and flags the palette's own declarations. |
| **15** | Delete the unreachable `daydreams` kind. | Or give it a door. |
| **16** | The rest of A5: `@` picker, `/` commands, goal-block shape, the Score's per-block actions. | The mockup shows the target. |

**Simplifier delete-list:** 3 rail placeholder buttons · the Explore pane from the default layout ·
the Terminal canvas mode row · the split control at catalog size 1 · the `daydreams` kind · the
free-floating lease TextBlock (folded into the Send reason) · the permanent Compiled-view field
(becomes a disclosure) · the full-width "Attach file…" bar (becomes a 28px icon button) ·
two of three duplicate evidence panes. **Net: −13 elements, +2** (the New Session primary and the
one-mode pane title).

---

## 8. The bounded loop

The brief's variant is *"rubric findings at severity ≥ major, strictly decreasing each pass"*. Read
literally against the running build it can never decrease, because **this node is read-only on `src/`**
— those 21 findings are the deliverable, not the work product. The variant is therefore measured
against **the artifacts this node produces**, which is the only thing its passes can change.

| Pass | Majors + Blockers in `docs/mockups/session-front-door.html` | Action |
|---:|---:|---|
| 1 | **5** | 4 × all-caps body text, 1 × 11px body text |
| 2 | **0** | Uppercase removed (a real legibility fix, not a suppression); 11px stops carrying sentences. Minors also addressed: em-dashes 35→0, nested card flattened, hairline-plus-shadow resolved. |

**Floor reached at pass 2. The cap did not fire.** Exit conditions:

| Condition | Result |
|---|---|
| No Majors remain in the produced artifacts | **Met** — 0 Major, 0 Blocker; 5 Minors, each a **declared deviation** with a reason in DESIGN.md |
| `ui-craft-gate.py docs/mockups --markdown` exits 0 | **Met — and vacuous.** See below. |
| `design-lint.py DESIGN.md --strict` exits 0 | **Met**, clean |
| Accessibility floor met | **Met for the design artifacts** (16 pairings computed live, all pass in all three themes). **NOT met for the running app** — that is finding-set A1–A6 and the §4 BLOCK verdict, and under UI-T4 no HTML artifact can clear it. |

---

## 9. What in the brief turned out to be false

Every node this session was asked to find the wrong claim in its brief. Nine.

1. **"`ui-craft-gate.py docs/mockups --markdown` exits 0" is not a gate.** Run bare it exits 0
   regardless of findings — it already did, **with 13 Majors present**, before I changed anything.
   Non-zero needs `--gate` *and* a **Blocker**-mapped finding. `ui-craft.yml` runs it deliberately
   advisory and non-blocking and says so. Treating exit 0 as one of three exit conditions gave that
   condition no discriminating power at all. The zero-majors condition was doing all the work.
2. **"It contests Ruling 21."** Ruling 21 is, in full, *"the canvas split is IN"*
   (`front-door-council-rulings.md:71-75`). It says nothing about a session terminal. The ruling
   governing the **mode set** is **Ruling 22**, same file, `:77-90`.
3. **"Addendum A's mode set."** Addendum A A6.1 lists **five** modes. The cut to Console + Terminal is
   in the **ratification note** (`addendum-a-ratification.md:36-37`), not in Addendum A. And **Addendum
   A has no "cut 1"** — it has sections A1–A8; the "cut 1" label is applied retrospectively in
   `addendum-b-ratification.md:54,82`. (This one is inherited by the mid-task correction, which used
   the same label.)
4. **"DC-110 (a defaulted class ranks in the wrong cohort)."** DC-110 is *"A partition value derived
   from the **ingest path** rather than from the work"*. The quoted phrase is `GovernedRunRequest.cs`'s
   comment, cited *by* Ruling 19 as pointing at DC-110. The real class is sharper and more useful: two
   doors each hard-code their own default, and the dangerous one is `"audit-import"`, which is a
   perfectly **comparable** string and therefore ranks dishonestly, while `"unclassified"` at least
   announces itself as unranked.
5. **"There is no 'Split' mode."** The mode set is Console + Terminal (`CanvasModeCatalog.cs:57-66`);
   split is a boolean canvas state. The brief and the screenshot both read the Split button as a third
   mode. It is not one, which is why binding its visibility to catalog size is coherent.
6. **"None of these icons do anything — get rid of them."** One of the four works, and it is the **only
   door to Explorer mode in the product**. Executing the instruction literally deletes the Knowledge
   Explorer.
7. **"…a full-window view the way the explorer view icon does."** The Explorer icon does a
   `ContentControl.Content` swap over a **two-value** enum, and it is full-**body**: the menu bar, title
   strip, rail and status strip all remain. A session is a **dock document** and A4.4 + ADR-0017 state
   it is *"not a third shell mode"*. The request as written conflicts with a settled ADR. §6 item 3
   proposes the resolution rather than taking it.
8. **"which pairs fail, in which theme."** There is no other theme. The app is **dark-only** — no second
   dictionary, no toggle, no `ThemeMode`, no OS-preference read. This changes the fix: there is no
   reason to route through dynamic resources for switchability, which makes item 1 smaller than it
   would otherwise be.
9. **"the composer, just built."** It is built, vendored, copied to output and navigated to — and has
   **never initialized once**. "Just built" describes the host shell around an editor that never starts.

A tenth, about the brief's framing rather than its facts: **the prior review of this very rail
(`docs/reviews/ui-activity-rail.md`) passed it** — tooltips, accessible names, 44px targets, roving
keyboard traversal, contrast *"~5.1:1 active, ~7.6:1 muted"*, gate verdict **PASS**. Every one of those
statements is about **presentation**, and none of them asked whether the buttons *do* anything. The
contrast figures came from **the mockup's readout**, not from the app. That is how a surface can be
reviewed, passed, and still be four dead icons: the rubric measured the layer the artifact could see.

---

## 10. Residual risk and what this review did not cover

- **UI-T4 native proof is not delivered.** No UI Automation tree, no Accessibility Insights run, no DPI
  or multi-monitor check, no keyboard traversal recording, no signing check. The mockup is **direction
  evidence only** and by the trigger table cannot clear native PASS. Every contrast figure in §2a is
  computed from source values and platform defaults, which is stronger than a screenshot but **is not
  the same as reading the rendered surface**. The dialog caption in §2c is the one figure sampled from
  real pixels.
- **The platform-default values were measured on pwsh 7.6 / .NET 9 WPF**, while the app targets
  `net10.0-windows`. Same Aero2 theme; identical values are **Inferred**, corroborated by the
  operator's screenshot showing the same family. If .NET 10 changed a default brush, the *ratios* move
  and the *finding* does not.
- **The task-class vocabulary in the mockup is illustrative.** No authoritative list exists anywhere in
  the repo; the six options shown are drawn from values observed in fixtures and specs. U2 must source
  the real set, and the controlled-vocabulary spec (§8.4) **does not yet exist**.
- **The Score-above-composer placement deviates from A5's sentence** and is filed as an erratum
  candidate, not a decision.
- **The rail's full-window request conflicts with ADR-0017** and needs an Owner nod before U2 builds it.
- **Defect classes are proposed, not registered.** §11. Two other nodes are live in this session and
  `docs/lessons/defect-classes.md` is a shared append-only register; two nodes appending concurrently
  is precisely the parallel-ID collision the register itself documents. Registration belongs at the
  join.
- **Not covered:** the graph canvas, the watcher surfaces, the menu system, the terminal dock, print or
  export, i18n, and the Explorer surface itself.

### 10a. Re-read against the pane-swap defect (INV-0006)

A tab drag re-labels whole zones and has been measured moving eleven bystander surfaces at once. Node
C2 owns the fix. **Tab positions in the feedback screenshots are therefore not reliable evidence of
intent**, so every item was re-read against that. The result:

- **Items 1, 3, 4, 5 and 6 do not depend on tab position at all.** The sheet is a modal; the rail is
  not a dock tab; the contrast findings are token-level; the composer and its mode strip live *inside*
  one document, so the Console / Terminal / Split controls are not dock tabs and the swap cannot
  produce them.
- **Item 7 is source-derived, not screenshot-derived.** Every claim about which pane sits in which zone
  comes from `ZoneLayout.Default()` (`ZoneLayout.cs:131-165`), not from the images. As a check: the
  right-hand group in the screenshots — Graph · Domain · Sessions · Board · Leaderboard · Ledger — is
  **exactly** the Center zone as defined in source, and the left group's Explore · Provenance ·
  Contexts · Joins is exactly the Left zone. No swap is evident in these particular shots.
- **One anomaly, noted and not built on.** The session document appears tabbed into the *Left* stack,
  covering Explore, while `OpenReferenceDocument` places a new surface beside the graph (Center). That
  is consistent with either placement-beside-focus or with **DC-054** (*a new pane placed into the
  focused stack hides the surface that stack already held*), which is already registered. Nothing in
  this review rests on it.
- **One dependency created by my own recommendation.** Item 9 of the plan moves Provenance into the
  **Right zone, which `ZoneLayout.Default()` leaves empty.** That is a statement about the default
  layout definition, which is source and therefore safe — but it must be **re-verified in the running
  shell after C2's fix lands**, because a zone recommendation validated against a shell that mislabels
  zones has been validated against the wrong thing.
- **A documentation discrepancy worth C2's attention:** `docs/reviews/nvda-workbench-session.md:69`
  records the Left zone as *"Explore, Domain, Provenance, Terminal"*, which disagrees with today's
  source on two of four. Either the default changed and the doc did not, or that session recorded a
  swapped layout as if it were the intended one. Not resolved here.
- **Item 2's silent restore is C2's, and I did not design around it.** `LayoutPersistence.Restore()`
  already composes *"Restored your saved workbench arrangement."* and the shell discards it. Nothing in
  this review adds a competing announcement.

---

## 11. Defect classes proposed (CI1 — to register at the join)

1. **A theme applied to containers, so every leaf inherits the platform's opposite theme.** Coverage is
   per-element instead of per-type, so correctness is a habit rather than a default and every new
   control regresses silently. *Control:* a check that fails when a control is instantiated with
   neither an explicit brush pairing nor a style, in every language the UI is written in — including
   the one that is not markup.
2. **A partial pairing produces the inverse defect.** Theming an ink without its ground (or the reverse)
   converts dark-on-dark into light-on-white. The fix for the instance manufactured the mirror of the
   instance. *Control:* the check above treats ink and ground as one unit; setting exactly one is the
   finding.
3. **A handshake in which both peers wait for the other's first message.** Each side is individually
   correct; the defect is the absence of an initiator, and it is invisible to a code reading of either
   side alone. *Control:* an initiation test that drives the **production** path. Here the probe
   supplied the missing message itself while its comment asserted the surface does — so the harness
   hid the defect it existed to catch.
4. **A pane distinguished by its caption rather than by its query.** Two surface kinds map to one
   builder that takes no discriminator; the tabs differ only in their label. *Control:* a test that two
   kinds render different content — which no test in this repo asserts.
5. **A review that clears a control's presentation without establishing that it has a handler.** The
   rubric measured tooltips, targets, focus and contrast, and never asked whether pressing the button
   did anything. *Control:* the existing "buttons do not lie" test is the right idea; it needs to count
   the buttons of the *surface under review*, not any button in the window.

---

## 12. Artifacts

| Artifact | Path |
|---|---|
| Mockup (5 surfaces, harness, **computed** contrast) | `docs/mockups/session-front-door.html` |
| Mockup hub node | `docs/mockups/session-front-door.md` |
| Design language additions (TC1–TC6, RQ1–RQ6, MS1–MS5, AR1–AR5) | `DESIGN.md` |
| This review | `docs/reviews/ui-operator-feedback.md` |
