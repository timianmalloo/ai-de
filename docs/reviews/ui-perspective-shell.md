---
id: ui-review-perspective-shell
title: "UI review — the perspective shell (Coding · Explore · Architecture), the conversation composer and the New Session sheet"
type: doc
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [ui-review, ux, accessibility, contrast, perspective, composer, session-settings, addendum-c]
links:
  - { to: spec-addendum-c-perspectives, rel: documents }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: mockup-perspective-shell, rel: relates-to }
  - { to: mockup-conversation-composer, rel: relates-to }
  - { to: mockup-new-session-sheet, rel: relates-to }
  - { to: mockup-session-front-door, rel: refines }
  - { to: ui-review-operator-feedback, rel: refines }
  - { to: note-addendum-c-design-signature, rel: relates-to }
  - { to: note-addendum-c-design-menu-names, rel: relates-to }
  - { to: note-addendum-c-design-tier-decoration, rel: relates-to }
  - { to: note-addendum-c-current-state-inventory, rel: relates-to }
review-by: 2026-12-11
review-suggested: []
summary: >-
  Elevate-mode review of the AI-DE workbench redesigned as three perspectives, with the composer as
  a conversation and the session settings in the New Session sheet. The token system gained an
  explicit on-accent ink, a field boundary, light values for every role and a re-toned comment
  colour, and an ink-by-ground matrix replaced a one-ground table that had drifted. Three mockups
  measure 0 craft findings each (corpus 104 to 98); three adversaries ran a bounded two-pass loop
  whose verdicts are recorded here; the highest-leverage change for the slice is the
  container-pairs / leaf-inherits rule the census's Fix A already names.
---

# UI review — the perspective shell

*Produced by `/ui-design` (mode: **elevate**) at node D1 of `plan-addendum-c-modes`, session
`addendum-c-chain`, 2026-09-11. Governed by `ui-design-craft.md` DX22–DX25 over the floors in
`ui-interaction-design.md` U1–U20, `ui-craft-detection.md` CD1–CD20 and the archetype grammar
G1–G16. Every finding carries location · dimension · severity · evidence · fix · confidence.*

**Surfaces reviewed:** the rail · the derived menu bar · the Coding and Architecture default layouts
and their empty states · Explore (unchanged) · the perspective switch (retained / opening / ignored /
error; rail / gesture / menu / routed open / Escape) · drop-with-report · the dock tab strip · the
conversation composer (fourteen states, provider present/absent) · the Console beside it · the New
Session sheet (seven states) · the token system beneath all of them.
**Reviewed against:** `spec-addendum-c-perspectives` Parts B and C (accepted) · Rulings 50–62 ·
`DESIGN.md` · archetype `PerspectiveShell { … Arch:HubAndSpoke … }` (spec §C1) · the operator's
composer verdicts and contrast directive · the operator's tier correction (relayed by the conductor
mid-run, see §0).
**Reviewers:** UX & Accessibility (lead, a11y hard veto) · UX Researcher/IA (UX veto) · The
Simplifier (soft veto) — each a read-only sub-agent, two passes; the author cleared no veto.
**Mode:** elevate · **Tier:** T2 · **Fan-out cap:** 3.

## 0. What arrived mid-run, and where the stages were

The conductor relayed the operator's correction to Ruling 56's tier clause — *"shouldn't tier be
decided by the compilation of the prompt?"* — after Stages 0–3 and the shell mockup were done and
before the composer and sheet mockups and the critique began. It was folded into DESIGN.md (PS-C7,
PS-S1–S3, the density contract, the copy) and the two later mockups before the adversaries convened;
`docs/notes/addendum-c-design-tier-decoration.md` records it verbatim as relayed, what the design
does with it, the one reading that is the conductor's (ceiling vs effective cap, **[Inferred]**), and
what is deliberately not designed (the derivation rule and the compile step: the operator's).

## 1. Verdict

> **PASS-WITH-CONDITIONS on the design artifacts** after pass 2 of a bounded loop (cap 3, floor 0
> Majors, exit = 0 Majors ∧ craft gate 0 ∧ a11y floor). The pass-2 verdicts of the three lenses are
> in §8; the conditions that remain are external to this node (the spec amendments the conductor
> owns, and the runtime Proof Pack) and are listed in §7 and §9.
> **Highest-leverage change (DX25):** *the container pairs ink with ground; the leaf inherits* —
> remove the `Foreground` setter from the implicit `TextBlock`/`Label` styles (the census's Fix A) and
> pair every state in `DockRoundedTabs.xaml` and the rail with the tokens PS-T1/PS-R2 name. One
> mechanism, twelve of the fourteen census failures, and every future state correct by default.

| | Pass 1 (three lenses) | Pass 2 |
|---|---|---|
| Blockers (sev 4, or any a11y ≥3) | 2 (both accessibility) | see §8 |
| Majors (sev 3) | 19 (10 a11y-lens · 4 UX-IA · 5 Simplifier) | see §8 |
| Minors (sev 2) | 32 | see §8 |
| Nits (sev 1) | 8 | see §8 |

**Accessibility veto:** stated by the UX & Accessibility lens in §8, not by the author. It clears
on the design artifacts when the pass-2 predicate holds; it **cannot** clear on the product from an
HTML artifact (UI-T4): the runtime Proof Pack items P-1, P-9, P-11, P-12, P-13 (spec §A13) are the
native evidence and are measured at the slice.

## 2. Measurements (DX23 — measure before you diagnose)

### 2a. The corpus, as CI measures it (`ui-craft-gate.py docs/mockups`)

| Run | Findings | Major | Minor | Top rule |
|---|---|---|---|---|
| Baseline (M0, `7f0b67a3`, re-run here at `68af0cf0`) | **104** | 66 | 38 | cramped-padding 26 |
| After this node | **98** | 60 | 38 | cramped-padding 26 |
| `perspective-shell.html` | **0** | 0 | 0 | — |
| `conversation-composer.html` | **0** | 0 | 0 | — |
| `new-session-sheet.html` | **0** | 0 | 0 | — |
| `session-front-door.html` (unchanged except its light values) | 5 | 0 | 5 | the front door's declared deviations |
| `DESIGN.md` | **0** | 0 | 0 | — |

The corpus fell by six `design-system-color` findings in legacy mockups that this node did not touch:
declaring the light values as tokens legalised colours those mockups had already taken from the
harness template. Recorded honestly (CD15): the values are real tokens now, not a widening to silence
a rule (`note-addendum-c-design-signature` §3). The twelve legacy mockups keep their 60 Majors of
deliberate DX17 density and are the exemption list of `tools/verify-ui-craft-floor.py`.

### 2b. The three mockups, counted

| Metric | Shell | Composer | Sheet |
|---|---|---|---|
| Interactive controls in the design region (default state) | 34 | 18 | 18 |
| Distinct type sizes used | 6 (11 · 12 · 13 · 15 · 18 · 22) | 5 | 5 |
| Colour tokens used / raw hex outside `:root` | 17 / **0** | 15 / **0** | 14 / **0** |
| Em dashes in body text (detector floor 8 at 1/500 chars) | 11 / 16.6k chars | 12 / 15.6k | 2 / 8.1k |
| Pairings in the live contrast audit, classified | 33 | 29 | 27 |
| `design-lint.py DESIGN.md --strict` | clean | | |
| `tools/verify-design-modes.py` | OK — 30 roles, each with a light value; `text-on-accent` declared | | |

### 2c. The density contract, measured on the composer at the startup Center height (580px)

| Property | Contract | Measured (default state, structure expanded) |
|---|---|---|
| Chrome above the editor's first line | 28px | 28px |
| Editor share of the composer zone | ≥ 45 % | met — read live by the page's audit (the value depends on the viewer's width; the audit shows it) |
| Editor height | ≥ 130px | met (the same audit) |
| Rows rendered beneath the editor | 4 collapsed / 7 expanded | 7 expanded |
| Bordered text fields other than the editor | 0 | 0 |
| Type sizes in the composer | ≤ 3 | 3 (13 · 12 · 11) |
| Per-prompt settings fields | 0 | 0 (the sheet's audit counts tier fields: 0) |

**[Inferred]** The 580px figure is the startup Center height derived from the shell's rows
(900 − 32 − 28 − 28 − 190 − 16 − 28); P-12 measures the real one.

### 2d. The token layer (computed from the declared values, dark / light)

`text-on-accent` on `accent` **6.60 / 6.22** · `text` on `surface` 14.98 / 15.29 · `text-muted` on
`surface-raised` 6.48 / 6.39 · `text-disabled` on `surface-raised` **4.59 / 5.57** (0.09 of headroom
in dark: a residual) · `syntax-comment` on `surface-sunken` **5.57 / 4.86** (was 3.18) ·
`field-border` on `surface-sunken` 5.28 / 4.64 · `focus` on `accent` **1.50 / 1.37** (why the ring is
outer) · `border` on `surface` 1.39 / 1.45 (decorative). The full matrix is in `DESIGN.md`.

## 3. Findings

*Structure before surface (DX24). Severity 0–4 → Blocker(4) / Major(3) / Minor(2) / Nit(1); an
accessibility finding at ≥3 is a Blocker. The three lenses' pass-1 findings are listed with their
pass-2 disposition; the author's own findings against the existing product and spec follow.*

### 3a. Structure — archetype fit, IA, flow (UX Researcher/IA, pass 1)

| # | Location | Dimension | Sev | Evidence | Fix (pass 2) | Conf |
|---|---|---|---|---|---|---|
| IA-1 | shell mockup, Flow 3 | State completeness / visibility | 3 | No state rendered a routed kind-open's arrival or failure | Trigger axis: routed open renders the opened tab, "Opened Code viewer in Architecture", and the failure string; return path in the legend | Verified |
| IA-2 | shell mockup, Flow 1 H | State completeness | 3 | The permission overlay arriving while in Explore/Architecture had no state | Permission axis: RootLayer overlay over any body | Verified |
| IA-3 | composer, Flow 6 I | Error prevention / AI honesty | 3 | The refusal read the *session's* tier; copy said "A T2 session needs…"; the harness could show a T1 decoration beside a T2 refusal | Tier derived from the state; refused → "~ T2 derived"; copy "This prompt compiles at T2 and needs Done when"; PS-C3 gates the refusal path's slice on the compile-step rule | Verified |
| IA-4 | spec Part B vs design | E7 consistency | 3 | B2, Flow 2 D, Flow 6 I/K/L, B6 still place tier in the session/sheet | **OPEN-external**: the conductor amends the spec on `main`; recorded in §7 | Verified |
| IA-5 | shell, Terminal menu | Recognition | 2 | With `terminal.new` in File, the Terminal menu held only prompt verbs | Renamed **Prompt** (PS-M1, `note-addendum-c-design-menu-names`) | Verified |
| IA-6 | shell, File menu | Match to the real world | 2 | "Entry verbs, in every perspective" was spec vocabulary | Header "New"; harness rows under a "Terminal sessions" group; pane copy says "terminal session" | Verified |
| IA-7 | shell, title strip | PS-M3 | 2 | Reset layout never gated in Explore | Hidden in Explore | Verified |
| IA-8 | sheet chooser | PS-M4 | 2 | "Ctrl+K, O is not bound; use the button" | Deleted | Verified |
| IA-9 | shell, Flow 1 A3 / Flow 4 | State completeness | 2 | "still building", newer-schema and duplicate-kind reports were copy only | Switch "ignored"; Restore "duplicate" and "newer" | Verified |
| IA-10 | sheet + shell, Flow 2 H | Visibility | 2 | The post-create landing was not traced | Trace row "Coding perspective — session … opened" | Verified |
| IA-11 | composer, Flow 6 T | State completeness | 2 | No template state | `template` state (change-order) | Verified |
| IA-12 | composer, Flow 6 L | Reach | 2 | The settings affordance had no rendered destination | `settings` state: the sheet's row as a popover | Verified |
| IA-13 | composer send row | Consistency | 2 | Two refusal grammars | Both refusals disable Send with the reason beside it | Verified |
| IA-14 | composer lines | Craft / Ruling 57 | 2 | A 92px label gutter read as a key:value form | Inline labels, no gutter | Inferred (the operator's verdict is the oracle) |
| IA-15 | composer density audit | DX23 | 2 | The metrics counted `<input>`s and containers, not what renders | Counts rendered rows and bordered text fields other than the editor; document fixed at the startup Center height | Verified |
| IA-16 | shell stand-in vs composer | E7 | 2 | Block b2 was T2 in one file and T1 in the other | One story: T1 | Verified |
| IA-17 | composer compiled header | Copy / overflow | 2 | The T0 sentence could overflow at 1024 | "~ T0 derived · no fan-out", sentence in the description | Inferred |
| IA-18 | sheet tier note | Minimalism | 1 | A boxed explanation of an absence | Deleted | Verified |
| IA-19 | copy drift | Copy | 1 | Colons where the spec's strings have em dashes | Restored verbatim | Verified |

Archetype fit was found sound at severity 0 for the rail (parallel-read / serial-enter), the
composer (serial entry; `Layout:StreamingThread` rightly not adopted) and the sheet; one severity-1
note for the operator: the Architecture Center's four views are tabs (one visible), which the spec
authorises and B1's "broad beside specific" only partly realises.

### 3b. States and accessibility (UX & Accessibility, pass 1)

| # | Location | Dimension | Sev | Evidence | Fix (pass 2) | Conf |
|---|---|---|---|---|---|---|
| A-1 | PS-R2 | Keyboard / consistency | 2 | "Radio group … arrow keys move" plus "focus lands in the new body" would switch on the first Down and make the *opening* state impossible | Manual activation; UIA tab list with SelectionItemPattern; deviation from spec §C4's "radio-group item" recorded | Verified |
| A-2 | the switch | SC 4.1.3 / 2.4.3 | 2 | "Announcement first, then focus" named an ordering, not a mechanism | UIA `RaiseNotificationEvent(ActionCompleted, ImportantAll)` before `Focus()`; the body carries a constant name | Inferred (WPF behaviour) |
| A-3 | composer focus overlay | SC 2.4.3 | 2 | The numbering contradicted the DOM and PS-C6 | Numbered in DOM order; PS-C6 restated; the write-scope line is not a stop | Verified |
| A-4 | composer states | U9 | 2 | Editor `attaching` named, not designed | `attaching` and `attaching-off` states (the census's `#drop-hint` site, now a token pair) | Verified |
| A-5 | evidence list, compiled stale, pressed | U9 | 1 | Loading/error rendered the default rows; `stale` only with send-failed; no pressed rule | Loading skeleton; `:active` on the primary; `updated` stays paired with send-failed (the state that produces it) | Verified |
| **A-6** | compiled summary | SC 4.1.2 / 1.3.1, U13 G1–G2 | **3 → Blocker** | `aria-label` replaced name-from-content, so the tier decoration, `updated` and the T0 line were invisible to AT at the confirmation point | Name stays "Compiled prompt"; tier + updated in `aria-describedby` / `ItemStatus`; the tier is spoken in the refusal and in-flight announcements (PS-C7) | Verified |
| **A-7** | Modes table, hc theme | SC 2.4.7 / 1.4.11 under HC | **3 → Blocker** | Ground-only states (menu/picker/list highlight, hover pill) vanished black-on-black in HC; only the bar was mapped | Modes row maps every ground-only state to a system pair; the mockups render Highlight/HighlightText stand-ins via `--hl/--hl-ink` and label the hc audit "not measured" | Verified (mockup) / Flagged (native template) |
| A-8 | selected-inactive tab | SC 1.4.11 / 1.4.1 | 2 | The state indicator was a 1.08:1 ground shift and a 2.09:1 ink shift | 2px `{colors.text-muted}` top edge (6.48 / 6.39), listed as a `ui` pair (PS-T3) | Verified (computed) |
| A-9 | derived lines | SC 4.1.2 / 3.3.1 | 2 | State in the name ("Goal, derived"); reason unassociated | Constant name; state via `aria-describedby` → mark / `ItemStatus`; reason via `aria-errormessage` / `HelpText`; placeholder as `aria-placeholder` / watermark | Verified |
| A-10 | inline targets | SC 2.5.8 | 2 | Shape/gear 22px; links and editable lines ~18px | 24px minimum on all | Verified |
| A-11 | text/numeric fields | SC 1.4.11 | 2 | Field boundaries at 1.04–1.39:1 | New `{colors.field-border}` token (5.28 / 4.64 on sunken); audit rows reclassified `ui` | Verified (computed) |
| A-12 | a11y minors (a–i) | various | 1 | Badge pairing outside the matrix; native tab stops; whole strip as live region; 20px menu items; picker roles; 16px close; `brightness()` hover; placeholder as value; instructions in the name | Badge is a danger ring on the sunken rail (in the matrix); roving `tabindex`; live region on the message only; 24px items; `listbox`/`option` as a sibling of the editor; 24px close hit area; hover recorded as a deviation; `aria-placeholder`; `aria-describedby` for instructions | Verified |
| A-13 | DESIGN.md matrix | TC6 | 2 | The light `text-disabled` column stated ratios not computed from `#5F6977` (4.26 / 3.85) | Recomputed (5.14 / 5.57 / 4.64 / 5.57); the phantom deviation deleted; the one-ground column deleted | Verified (computed) |
| A-14 | sheet chooser | U11 | 2 | Unbound chord in copy | Deleted (= IA-8) | Verified |
| A-15 | copy minors (a–h) | Copy / consistency | 1 | Extra empty-state sentence; colons for em dashes; three budget formats; `Ctrl+PgDn`; b2 tier; density metric narrower than its claim; refusal grammars; the reviewer note in the search box; spec Part C lagging DESIGN.md | All fixed in the artifacts; the spec lag is §7 | Verified |

### 3c. Simplification (The Simplifier, pass 1)

| # | Location | Tag | Sev | Evidence | Fix (pass 2) | Conf |
|---|---|---|---|---|---|---|
| S-1 | DESIGN.md palette-roles table | delete | 3 | A one-ground contrast column beside the matrix, stale (13.9 vs 14.98) | Column deleted; the matrix is the one store | Verified |
| S-2 | shell stand-in | shrink | 3 | Six derived lines re-drawn from the composer and drifted (T2 vs T1) | Editor + pointer only; T1 | Verified |
| S-3 | composer harness | delete | 3 | A Tier-decoration axis rendered PS-C2/C7-forbidden states | Axis deleted; tier derived from the state; compiled text generated per state | Verified |
| S-4 | spec §C3 `{colors.accent-contrast}` | — | 3 | Cites a token DESIGN.md no longer declares | **OPEN-external** (the conductor's spec amendment); recorded in `note-addendum-c-design-signature` §2 and §7 | Verified |
| S-5 | sheet chooser | delete | 3 | Unbound chord + a button that does not exist | Deleted (= IA-8, A-14) | Verified |
| S-6…S-18 | DESIGN.md restatements, duplicate copy, dead CSS, unused custom properties, the Modes-in-catalog control, the tier note, the narrow-viewport rules, hc labelling | delete / shrink / yagni | 1–2 | As itemised in the pass-1 report (net −101) | Applied except: the three duplicated token/harness blocks (DX8: one self-contained file each) and the nine alias-shaped light tokens (the detector reads only flat strings; a YAML anchor is unverified in its reader) | Verified |
| S-19 | DESIGN.md front door | add | 1 | The older mockup's light accent went off-token once the values were declared | `session-front-door.html` light values moved onto the declared tokens | Verified |

### 3d. The author's findings against the product and the spec (not the artifacts)

| # | Location | Dimension | Sev | Evidence | Fix | Conf |
|---|---|---|---|---|---|---|
| P-1 | `App.xaml` implicit `TextBlock`/`Label` styles | Token discipline / SC 1.4.3 | **4** | 12 of 180 census pairings at 2.37:1: the leaf's implicit `Foreground` outranks every container's state ink (INV-0007 §5) | The census's Fix A; PS-T1 is the design rule; **the highest-leverage change** | Verified (census, read on `investigate/contrast-census`) |
| P-2 | `DockRoundedTabs.xaml` selected-inactive | SC 1.4.3 | **4** | 1.45:1, sunken ink on `#2A313B` (a line token as a ground) | PS-T3's pairing (`text` on `surface`, 2px muted top edge) | Verified (census) |
| P-3 | `MainWindow.xaml:78` New session glyph | Token discipline | 3 | `SurfaceSunkenBrush` (a ground) used as the ink on the accent fill | `{colors.text-on-accent}` — the token now exists so the borrowing is visibly wrong at the call site | Verified (file read) |
| P-4 | `Web/composer.html` | Token discipline (TC5) | 3 | A second palette (`#1e1e1e`, `#d4d4d4`, `#7f858a`): `#drop-hint` 4.47:1 | PS-C4: the host injects the named CSS variables on `host.init`; the page carries no palette | Verified (file read) |
| P-5 | `App.xaml:53-55` `MenuBackgroundBrush #161A20`, `MenuHoverBrush #243040`, `MenuBorderBrush #2B333D` | Token discipline | 3 | Three menu brushes with no `DESIGN.md` token | The menu state table: `surface-raised` / `float-chrome` / `border` | Verified (file read) |
| P-6 | spec §C3, §C4, S-1/S-2, B2, B6 | E7 | 3 | `{colors.accent-contrast}` (renamed); editor ground `surface-raised` (design: sunken inside the island); the inherited-settings copy and T0 warning (now the compiled header's); tier in the sheet | The conductor's spec amendment (in progress on `main`); every item listed in §7 | Verified |
| P-7 | spec §C4 "radio-group item" | Keyboard | 2 | Automatic-activation radio semantics contradict the *opening* state | Amend to a single-selection group with manual activation (PS-R2's rationale) | Verified |

## 4. Scorecard by dimension (the design artifacts, after pass 2)

| # | Dimension | Verdict | Worst finding |
|---|---|---|---|
| 1 | Visibility of system status | 4 / 5 | IA-1/IA-2 (resolved): every switch outcome, routed open, permission request and restore report now has a rendered state and a spoken string |
| 2 | Match to the real world | 4 / 5 | IA-6 (resolved): spec vocabulary no longer leaks into the File menu |
| 3 | User control & freedom | 4 / 5 | Escape from Explore's root, retry from the rail, Try again, draft kept |
| 4 | Consistency & standards | 4 / 5 | IA-5 (Prompt menu); one refusal grammar |
| 5 | Error prevention | 3 / 5 | IA-3: the refusal predicate depends on a compile-step rule nobody has written (external) |
| 6 | Recognition over recall | 4 / 5 | View → entry for every admitted surface; bound gestures only in copy |
| 7 | Flexibility & efficiency | 4 / 5 | One action at defaults; Ctrl+1/2/3; the palette follows the menu |
| 8 | Aesthetic & minimalist design | 4 / 5 | The Simplifier's net −101 applied; the three duplicated token blocks remain by DX8 |
| 9 | Error recovery | 4 / 5 | Every error state names its recovery; create failure keeps the answers |
| 10 | Help & documentation | 3 / 5 | Tooltips and empty states teach; no in-product explanation of the compiled tier beyond the description |
| 11 | **Archetype fit** | 5 / 5 | Verified by the UX-IA lens at severity 0 for all three surfaces |
| 12 | **State completeness** | 4 / 5 | Fourteen composer states, seven sheet states, the shell's five state axes; A-4 resolved |
| 13 | **Token discipline** | 5 / 5 (artifacts) · 1 / 5 (product) | 0 raw hex outside `:root` in the mockups; the product's P-1…P-5 |
| 14 | **Accessibility (WCAG 2.2 AA)** | see §8 | A-6, A-7 resolved on the artifacts; the product is not measurable from HTML (UI-T4) |
| 15 | **Performance & stability** | 4 / 5 | 0ms switch, no layout shift on expand; p95 ≤ 150ms is P-8, unmeasured |
| 16 | **Content & copy** | 4 / 5 | Spec strings verbatim; the compiled-tier copy is new and honest about what it does not know |
| 17 | **Craft** | 4 / 5 | 0 detector findings on three files; one focal point per surface; the density contract measured |
| 18 | **AI-surface honesty** | 4 / 5 | Governor / Trust builder / Wayfinder named; wrong derivation, no provider and refused send are states; the tier decoration is disclosed and spoken |

## 5. Generic-tells self-check (DX3)

| Tell | Present? | Disposition |
|---|---|---|
| Side-tab accent border | Yes, once: the rail's 3px active bar | Deliberate — the non-colour half of the active signal (spec §C3); the list-row selection bar was removed when the detector flagged it as this tell |
| Nested cards | No | The window frame lost its shadow so panes are cards on a surface, not cards in a card; the host's splitter ground became gaps on the surface |
| Three equal stat tiles | No | — |
| Uniform spacing | No | 4 / 8 / 12 / 16 rhythm; within tighter than between (the lines vs the regions) |
| One or two type sizes, weight doing the work | No | 11 · 12 · 13 · 15 · 18 · 22 in use; ratio 2:1 |
| Lorem / placeholder data | No | Real session names, paths, event lines, a 120-character path, a 90-character account label |
| Emoji as iconography | No | Lucide-style geometry; registry entries in the shell |
| Happy-path-only screens | No | Fourteen composer states; the shell's switch/restore/permission axes; the sheet's failure and chooser |
| Symmetry everywhere | No | The composer is a serial column beside a reading column; the sheet is one column |
| Motion on everything / none | No | Seven-row inventory; two animated moments in the composer; 0ms for layout; reduced motion proven in the harness |
| Em-dash saturation | No | Under the detector's density floor in all three files; the dashes are the spec's verbatim strings |
| Marquee / pulsing dot | No | The in-flight progress bar was replaced by a rotating ring when the detector called it a marquee |

## 6. The Simplifier's delete-list (pass 1, applied)

```
delete:  DESIGN.md one-ground contrast column (stale, second store)          -10
delete:  DESIGN.md Modes light ratios; complete-states restatement;
         two-default-layouts table; PS-R1 / PS-R3 / PS-M2 restatements;
         duplicate copy lines; deviation pointer rows                        -13
delete:  composer Tier-decoration axis; Modes-in-catalog axis + tabs;
         hard-coded density row; dead rules; unused custom properties         -45
delete:  sheet tier note; chooser chord string; narrow-viewport rules;
         rationale-as-copy                                                    -6
shrink:  shell composer stand-in (editor + pointer); reserved spacer;
         search hint; explanatory sentence in the empty state                 -12
shrink:  copy drift (":" vs "—") to one canonical form                       -6
kept:    three duplicated token/harness blocks (DX8); nine alias-shaped
         light tokens (the detector reads flat strings only)                   0
added:   {colors.field-border} (WCAG 1.4.11, the a11y lens's Major);
         Trigger / Permission / ignored / duplicate / newer states;
         template / settings / attaching / attaching-off states               +14
net: -78 elements (pass 1 asked for -101; 23 kept with reasons, 14 added on the a11y and UX-IA findings)
```

## 7. Ranked plan

**Must fix before the slice ships (Blockers on the product)**
1. **P-1 — the leaf must not state its own ink.** Remove the `Foreground` setter from the implicit
   `TextBlock`/`Label` styles (INV-0007 Fix A); pair every state in `DockRoundedTabs.xaml`, the rail
   and the palette with the tokens PS-T1/PS-T3/PS-R2 name (`text-on-accent` on `accent`; `text` on
   `surface` + the 2px muted edge). Owner: the contrast-census investigation's slice. Oracle: the census
   (P-11), 0 of 180 below floor.
2. **P-2 / P-3 — the on-accent ink by name.** `AccentContrastBrush` → `TextOnAccentBrush` declared
   from `{colors.text-on-accent}`; the New session glyph and every accent-ground state use it. Owner:
   the same slice. Oracle: `ContrastFloorTests` gains the accent-ground rows (census-pending) and the
   census confirms.
3. **P-4 — the composer page under the tokens.** Inject the PS-C4 CSS variables on `host.init`; delete
   the page's palette; `#drop-hint` → `--text-muted`. Owner: the composer fix. Oracle: the census's
   WebView2 half reports a measured count ≥ 1 for the page (never "not measured" once it walks it).
4. **IA-4 / P-6 / S-4 — the spec amendments** (the conductor, on `main`): tier out of the session
   settings (S-1/S-2, B2, Flow 2 D, Flow 6 I/K/L, B6); `{colors.accent-contrast}` →
   `{colors.text-on-accent}`; §C3 editor ground; §C4 "radio-group item" → single-selection with manual
   activation; the compiled-tier copy in §C4. Oracle: `verify-ruling-citations.py` and a re-read.

**Should fix next (Majors)**
5. **P-5 — the three menu brushes** onto tokens (`surface-raised` / `float-chrome` / `border`) and the
   disabled row never taking the hover ground. Oracle: the census's menu rows.
6. **IA-3 — the compile-step rule.** The operator's specification of how a prompt's tier is derived,
   and what an operator does when it is wrong; the refusal path's slice is gated on it (PS-C3).
7. **P-7 / A-1 — the rail's activation model** in the slice: `TabItem`/`SelectionItemPattern`, manual
   activation, the *opening* state with focus on the trigger. Oracle: P-1 and P-9.
8. **The HC mapping** (A-7) in `App.xaml`'s high-contrast dictionary: highlight, selected-active tab,
   field boundaries, disabled. Oracle: the census run under a Windows high-contrast theme.

**Worth doing**
9. `verify-design-modes.py` is in `build.yml`; when the census lands, feed its light run from the
   `light-*` tokens (DC-137's runtime half).
10. `ContrastFloorTests`' 18-type theory must exempt leaf text types with the reason, or Fix A goes
    red (INV-0007's own note).
11. The nine alias-shaped light tokens: check whether the detector's YAML subset resolves anchors; if
    it does, collapse them.
12. A rendered-page density check (P-12) that reads the composer's real editor height at the startup
    width, so the 580px figure in §2c stops being Inferred.

> **Do this one first:** P-1 — because it is one mechanism (the container pairs, the leaf inherits),
> it clears twelve of the fourteen measured failures, it is the rule every state table in DESIGN.md
> now assumes, and every future control is correct by default rather than by habit.

## 8. The bounded loop (variant: findings at severity ≥ Major across the three lenses; floor 0; cap 3)

| Pass | Blockers | Majors | Action |
|---:|---:|---:|---|
| 1 | 2 | 19 | The three lenses' findings in §3a–§3c: DESIGN.md (activation model, mechanism, HC mapping, field-border token, matrix recomputed, one-ground column deleted, restatements deleted, Prompt menu, PS-C3/C6/C7); the shell (tab list + manual activation, routed-open/permission/ignored/restore states, live-region scoping, selected-inactive edge, badge pairing, File menu groups, stand-in shrunk); the composer (tier derived from state, AT exposure, line semantics, targets, four new states, one refusal grammar, density audit re-measured); the sheet (field boundaries, chooser copy, tier note deleted); the front door's light values |
| 2 | *(the lenses' pass-2 verdicts, recorded verbatim below when they arrive)* | | |

**Exit conditions after pass 2**

| Condition | Result |
|---|---|
| 0 Majors in the design artifacts | see the pass-2 row |
| `ui-craft-gate.py` 0 findings on each artifact; `tools/verify-ui-craft-floor.py` green at Major | **Met** — and gated by default for every mockup (DC-139) |
| `design-lint.py --strict` clean; `verify-design-modes.py` OK | **Met** |
| Accessibility floor met on the design artifacts, cleared by someone other than the author | see the pass-2 row |
| Accessibility floor on the product | **Not measurable here** (UI-T4): P-1, P-9, P-11, P-12, P-13 |

## 9. Residual risk and what this review did not cover

- **UI-T4 native proof is not delivered and cannot be from HTML.** The proof rows, all *not measured*
  here: platform HIG (Fluent), keyboard traversal (P-1, P-7, P-13), accessibility tree (P-1, P-9),
  Light/Dark/HighContrast (P-11 in both themes; the HC mapping is a rule with no runtime evidence),
  DPI/windowing, large-list responsiveness (the evidence list at ≥ 20,000 rows), OS integration
  (DWM dark caption on the sheet, TC4), distribution trust (an unsigned local build). Every one is a
  runtime item of the slice.
- **The compiled tier's derivation rule does not exist.** The composer's central promise (one action
  at defaults; refuse on content gaps) is decided by a rule the operator has said they will think
  through. PS-C7 shows the *result* with three states; the refusal path is gated on the rule.
- **The ceiling / effective-cap reading is the conductor's** (**[Inferred]**); if the operator's
  compile-step specification says otherwise, one sentence in the sheet and the T0 line change.
- **`text-disabled` on `surface-raised` clears by 0.09 in dark**; any drift in either value fails.
- **The census cannot see a state-indicator gap** (the selected-inactive edge is a `ui` pairing, not
  an ink/ground pair the census walks); P-1's UIA walk must assert it.
- **The WebView2 page's real pairs** stay *not measured* until the census walks the DOM.
- **Reply-beside-input** (the Console beside the composer) is Addendum A §A2's layout and is
  unvalidated against the operator's "does not feel like a chat" verdict; P-12 is where the operator
  sees it.
- **Not covered:** the Explorer surface's own states (unchanged by this addendum), the command
  palette's rows (the spec's US-C4 oracle), the Loomkeeper surfaces, print/export, i18n beyond the
  AZERTY note on gesture display strings.

## 10. Defect classes registered (CI1)

| Class ID | Shape | Control added | Status |
|---|---|---|---|
| DC-137 | A theme declared as a rule with no values is a theme that cannot fail | `tools/verify-design-modes.py` (every colour role has a light twin; on-accent ink declared), in `build.yml` | controlled (design) · partially (runtime census pending) |
| DC-138 | A palette table that measures every ink on one ground never lists the family that fails | The ink × ground matrix rule + `text-on-accent`; `verify-design-modes.py` refuses a DESIGN.md without the on-accent ink; the runtime census is the product control | partially |
| DC-139 | A per-file allow-list of gated artifacts lets every new artifact enter ungated | `verify-ui-craft-floor.py` gates every `docs/mockups/*.html` by default; exemptions are named with reasons | controlled |

## 11. Artifacts

| Artifact | Path |
|---|---|
| Design language (tokens, matrix, the perspective-shell section) | `DESIGN.md` |
| Shell mockup + hub | `docs/mockups/perspective-shell.html` · `.md` |
| Composer mockup + hub | `docs/mockups/conversation-composer.html` · `.md` |
| Sheet mockup + hub | `docs/mockups/new-session-sheet.html` · `.md` |
| Decision notes | `docs/notes/addendum-c-design-signature.md` · `addendum-c-design-menu-names.md` · `addendum-c-design-tier-decoration.md` |
| Controls | `tools/verify-design-modes.py` · `tools/verify-ui-craft-floor.py` (gated by default) · `.github/workflows/build.yml` |
| This review | `docs/reviews/ui-perspective-shell.md` |
