---
name: AI-DE Workspace
description: Design language for the AI-DE desktop workspace — a dense, calm, evidence-first shell for directing coding agents.
archetype: "PerspectiveShell { Type:OLTP; Arch:HubAndSpoke; Layout:MultiPanelWorkstation; Density:Compact; Nav:Sidebar+CommandPalette+TopBar; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive; Type:Utilitarian; Depth:SoftShadow; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Instant+Confirmed; Motion:Micro; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA+ReducedMotion; x-platform:windows; x-framework:wpf; }"
colors:
  # Dark is the operator's default. Every role below has a light value under the same name with a
  # `light-` prefix (one role, two values — modes over the semantic layer, U4); the high-contrast
  # theme maps roles to Windows system brushes by rule and declares no values here.
  surface: "#12151A"
  surface-raised: "#1A1F26"
  surface-sunken: "#0D1014"
  border: "#2A313B"
  text: "#E4E9EF"
  text-muted: "#98A3B2"
  text-disabled: "#7C8896"
  accent: "#5B9DD9"
  # The ink on an accent ground — the ONLY ink any state may paint on {colors.accent}. The code names
  # it AccentContrastBrush (INV-0008); it shares surface-sunken's value by coincidence, not dependency.
  accent-contrast: "#0D1014"
  # The boundary of a control at rest — an input, a numeric field, a button, a select (WCAG 1.4.11
  # needs 3:1 to identify a control); {colors.border} is decorative (a separator) and cannot carry
  # it. Same value as text-disabled by design: one dim grey for "chrome that is not ink".
  border-strong: "#7C8896"
  verified: "#5FB98F"
  inferred: "#D8A650"
  unverified: "#98A3B2"
  stale: "#D8A650"
  danger: "#E07A6F"
  focus: "#8FC0EA"
  splitter: "#2A313B"
  splitter-hover: "#5B9DD9"
  splitter-keyboard: "#8FC0EA"
  drop-target: "#5B9DD9"
  drop-target-fill: "rgba(91,157,217,0.18)"
  drop-forbidden: "#E07A6F"
  float-chrome: "#232A33"
  locked: "#D8A650"
  scrim: "rgba(0,0,0,0.55)"
  # Syntax palette — a deliberately SEPARATE system for the code-node view (Material Palenight
  # family). Chrome tokens above never colour code; these never colour chrome. Documented here so
  # token discipline is satisfied and the craft detector treats them as intentional, not drift.
  syntax-keyword: "#C792EA"
  syntax-type: "#82AAFF"
  # Re-toned from #5A6472 (3.05–3.18:1 on the dark grounds — below the 4.5:1 text floor for the
  # first code node rendered; spec-addendum-c-perspectives §R row 23). 5.35 / 4.84 / 5.57:1 now.
  syntax-comment: "#808C9A"
  syntax-string: "#C3E88D"
  syntax-highlight: "#B08CD9"
  # ---- Light mode values (the same roles; declared so the light contrast census has something to
  # measure — spec-addendum-c-perspectives §R row 24). Every ink clears 4.5:1 on every light ground.
  light-surface: "#F4F6F8"
  light-surface-raised: "#FFFFFF"
  light-surface-sunken: "#E7EBEF"
  light-border: "#C7CFD8"
  light-text: "#1A1F26"
  light-text-muted: "#55606E"
  light-text-disabled: "#5F6977"
  light-accent: "#27649A"
  light-accent-contrast: "#FFFFFF"
  light-border-strong: "#5F6977"
  light-verified: "#1C724A"
  light-inferred: "#7D5900"
  light-unverified: "#55606E"
  light-stale: "#7D5900"
  light-danger: "#A8372B"
  light-focus: "#1B4F7E"
  light-splitter: "#C7CFD8"
  light-splitter-hover: "#27649A"
  light-splitter-keyboard: "#1B4F7E"
  light-drop-target: "#27649A"
  light-drop-target-fill: "rgba(39,100,154,0.18)"
  light-drop-forbidden: "#A8372B"
  light-float-chrome: "#EEF1F4"
  light-locked: "#7D5900"
  light-scrim: "rgba(0,0,0,0.35)"
  light-syntax-keyword: "#6F35A0"
  light-syntax-type: "#1C579B"
  light-syntax-comment: "#5C6674"
  light-syntax-string: "#38651B"
  light-syntax-highlight: "#6C3FA0"
typography:
  ui: "Segoe UI Variable Text, Segoe UI, system-ui, sans-serif"
  mono: "Cascadia Mono, Consolas, ui-monospace, monospace"
  scale: [11px, 12px, 13px, 15px, 18px, 22px]
  weight-normal: 400
  weight-medium: 600
rounded: { sm: 4px, md: 6px, lg: 10px, island: 12px }
spacing: { scale: [2px, 4px, 6px, 8px, 12px, 16px, 24px, 32px] }
elevation: { flat: none, resting: "0 1px 3px rgba(0,0,0,0.28)", raised: "0 4px 12px rgba(0,0,0,0.34)", dialog: "0 8px 24px rgba(0,0,0,0.5)" }
icon: { sm: 16px, md: 20px, lg: 24px, stroke: 1.5px }
motion: { fast: 150ms, base: 200ms, ease: "cubic-bezier(0.2, 0, 0, 1)" }
---

# AI-DE Workspace — design language

The primary user is an expert operator under cognitive load with several live agent sessions. The
surface must be **dense, not cramped; calm, not passive; powerful, not opaque**. Every pixel of
chrome competes with evidence, so chrome loses.

## Principles

1. **Evidence before ornament.** A screen's focal point is always the selected node's provenance, never
   a decoration. If a pixel does not carry evidence, state, or a control, it is spacing.
2. **Confidence is never colour alone.** `{colors.verified}` / `{colors.inferred}` /
   `{colors.unverified}` always ship with a glyph *and* a word — colour is the third signal, never the
   first. This is a correctness rule, not a preference: a colour-blind operator must read the same
   confidence the palette shows.
3. **Absence is a state.** `not recorded`, `stale`, `omitted`, `unknown` are rendered explicitly in
   `{colors.text-muted}` or `{colors.stale}`. An empty result never renders as a clean success.
4. **Dark-adaptive, not dark-only.** Tokens are defined for a dark ground first (the operator's default),
   with a light and a Windows high-contrast mode carrying the same semantic roles.

## Palette roles

| Token | Role |
|---|---|
| `{colors.text}` | primary evidence text |
| `{colors.text-muted}` | secondary metadata, `not recorded` |
| `{colors.text-disabled}` | the ink of an unavailable control — **a pairing, never an opacity** |
| `{colors.accent}` | selection, focus affordance, links |
| `{colors.verified}` | Verified confidence chip |
| `{colors.inferred}` / `{colors.stale}` | Inferred confidence, stale state |
| `{colors.danger}` | failed extraction, delivery failure |
| `{colors.focus}` | 2px focus ring, always visible |
| `{colors.accent-contrast}` | **the only ink on `{colors.accent}` as a ground** — selected-active tab, checked toggle, selected row, mention chip — 6.6:1 on the accent; no other token is legal on the accent ground (text 2.4, muted 1.1, verified 1.2, danger 1.0). The code's `AccentContrastBrush` (INV-0008, `note-20260911-on-accent-ink-is-its-own-token`); the runtime census `ShellContrastCensusTests` re-measures it. |
| `{colors.border-strong}` | the boundary of a control at rest — input, numeric field, button, select — a `ui` pairing (3:1) on every control ground; `{colors.border}` covers separators only (the CD16 deviation is about separators, never control boundaries) |
| `{colors.border}` | 1px separators; never the only grouping signal — decorative, spacing carries the grouping |

*Ratios live in one place: the ink × ground matrix below (both themes). A one-ground column used to
sit here and had drifted from the values it claimed to measure (13.9 vs 14.98); a second store of the
same number is how a design language declares AA it did not compute (TC6).*

### Ink × ground — every pairing a state may render, both themes

The table above measures every ink on one ground. The family that failed the runtime census
(`INV-0007`, 14 of 180 pairings) was the **accent-as-ground** family — a light ink on `{colors.accent}`
at 2.37:1 — which a one-ground table cannot list. This matrix is the rule: **a state names an ink token
and a ground token from the same row and column, and nothing else.** A cell marked `—` is a pairing no
state may use. Ratios are computed from the token values (dark / light); the runtime census re-measures
them on the composed tree and is the acceptance test (spec-addendum-c-perspectives §C7).

| Ink ↓ · Ground → | `surface` | `surface-raised` | `surface-sunken` | `float-chrome` | `accent` |
|---|---|---|---|---|---|
| `text` | 14.98 / 15.29 | 13.57 / 16.56 | 15.62 / 13.82 | 11.86 / 14.61 | **—** (2.37 / 3.11 — the census failure) |
| `text-muted` | 7.16 / 5.90 | 6.48 / 6.39 | 7.46 / 5.34 | 5.67 / 5.64 | — |
| `text-disabled` | 5.07 / 5.14 | 4.59 / 5.57 | 5.28 / 4.64 | 4.01† / 4.91 | — |
| `accent-contrast` | — | — | — | — | **6.60 / 6.22** |
| `accent` (link, glyph, 3px bar) | 6.33 / 5.74 | 5.73 / 6.22 | 6.60 / 5.19 | 5.01 / 5.49 | — |
| `verified` | 7.69 / 5.45 | 6.96 / 5.91 | 8.01 / 4.93 | 6.08 / 5.21 | — |
| `inferred` / `stale` / `locked` | 8.27 / 5.87 | 7.48 / 6.36 | 8.62 / 5.31 | 6.54 / 5.61 | — |
| `danger` | 6.26 / 5.97 | 5.67 / 6.47 | 6.52 / 5.40 | 4.95 / 5.71 | — |
| `focus` (2px outer ring, 3:1 floor) | 9.49 / 7.86 | 8.59 / 8.51 | 9.89 / 7.11 | 7.51 / 7.51 | **1.50 / 1.37 — outer ring only** (see deviations) |
| `syntax-comment` | 5.35 / 5.37 | 4.84 / 5.82 | 5.57 / 4.86 | — | — |
| `border-strong` (1px control boundary, 3:1 floor) | 5.07 / 5.14 | 4.59 / 5.57 | 5.28 / 4.64 | — | — |
| `focus` as the keyboard indicator on a highlighted row (2px inset ring, 3:1 floor) | 9.49 / 7.86 | **8.59 / 8.51** | 9.89 / 7.11 | 7.51 / 7.51 | — |
| `danger` as a glyph/badge stroke (3:1 floor) | 6.26 / 5.97 | 5.67 / 6.47 | 6.52 / 5.40 | — | — |

† `text-disabled` on `float-chrome` (dark) is 4.01:1, below the text floor. **A disabled menu row
therefore never takes the lifted hover ground**: hovering a disabled item leaves it on
`surface-raised` (4.59 / 5.57) and shows the reason. A disabled rail item does not exist (AR3); a
disabled tab is not a state the dock has. Every other `text-disabled` cell clears 4.5:1 in both themes
(an earlier draft of this row stated light values it had not computed — 4.26 / 3.85 — and built a
deviation on them; recomputed from `{colors.light-text-disabled}`, the deviation is gone). A hovered accent fill (Send, New
session, a primary button) is the accent at `brightness(1.12)` — a derived value, not a token; the WPF
slice uses the Fluent accent-light brush; computed 8.24 / 5.22:1 with `accent-contrast`.

## Type

`{typography.ui}` for all interface text; `{typography.mono}` for identifiers, revisions, paths, and
error codes — anything the operator may need to compare character-by-character. Scale steps are
`{typography.scale}`; the pane uses 13px body with 12px labels at `{typography.weight-medium}` and
0.04em letter-spacing for section headers. Numerals in any aligned column use tabular figures.

## Layout

A user-arranged **multi-panel workstation** at `{spacing.scale}` rhythm: 8px inside a row, 12px between
groups, 16px pane padding. Compact density means 28px list rows and a 28px tab strip — enough for a
24×24 target plus separation, no more. Depth is flat: docked panes carry a 1px border and **no**
shadow, `{elevation.dialog}` is reserved for floating panes and modal confirmations. The window is
always a complete, non-overlapping tiling; only floating panes overlap, and only deliberately.

## Motion

`{motion.fast}` for selection, `{motion.base}` for pane section reveal, both `{motion.ease}`. Delivery
status announces immediately through a live region **independent of motion**. Under
`prefers-reduced-motion` every transition becomes an instant state change with the identical
announcement — reduced motion never reduces information.

## States

Every component ships the complete set — default, hover, focus, active, disabled, **loading, empty,
error**, success, plus first-run and overflow. The per-component matrix lives with the component in
[`docs/design/phase-1-walking-skeleton.md`](docs/design/phase-1-walking-skeleton.md#ui-and-interaction-design);
a component missing its empty or error state is an incomplete component, not a styling gap.

## Modes

| Mode | Ground | Notes |
|---|---|---|
| Dark (default) | `{colors.surface}` | The operator's working default. |
| Light | `{colors.light-surface}` — **every role has a declared `light-*` value** (frontmatter; `tools/verify-design-modes.py` refuses a role without one) | Confidence hues re-picked for AA on a light ground, not naively lightened; the ratios are the matrix's light column. Until 2026-09-11 this row was a rule with no values, so the light contrast census had nothing to measure (spec-addendum-c-perspectives §R row 24). |
| Windows high contrast | system colours | **Every ground-only state maps to a system pair, or it vanishes** (black on black): the active rail bar and the selected-active tab → `SystemColors.Highlight` ground + `HighlightText` ink; a menu, palette, picker or list **highlight** → `Highlight` + `HighlightText`; the hover pill on the rail → no ground change (the focus ring, `SystemColors.ControlText` 2px, is the only indicator); field boundaries → `ControlText`; disabled → `GrayText`. Glyph+text confidence keeps meaning when hue is unavailable. No values are declared here because the values are the OS's; the mockups' `hc` theme uses stand-ins and its audit is labelled *not measured*. |

## Performance budget

Node selection → provenance render p95 <100ms; list filter update p95 <250ms; initial selected-view
render p95 <2s on the approved corpus (spec Part C). A degraded result renders a bounded, labelled
state — never a silent omission.

---

## The workbench (US-9)

The chrome is structural, quiet and obedient. Every pixel of it competes with evidence, so it loses:
1px borders, no gradients, no shadows on docked panes, and **no animation on any layout operation** —
a pane that slides into place is a pane you have to wait for.

### Layout tokens

| Token | Value | Role |
|---|---|---|
| `{colors.splitter}` | `{colors.border}` | The 1px line between panes. At rest it is a border, not a handle. |
| `{colors.splitter-hover}` | `{colors.accent}` | Pointer within the 6px grab zone. The **hit area is 6px; the painted line stays 1px** — a fat line is visual noise, a thin hit target is a usability defect. |
| `{colors.splitter-keyboard}` | `{colors.focus}` | **The edge selected for keyboard resize.** 2px, full length, plus an end-cap marker so the *direction* of travel is legible. This is the one place the workbench draws attention to itself, because a keyboard user cannot see what a pointer user infers from the cursor. |
| `{colors.drop-target}` / `{colors.drop-target-fill}` | accent / 18% accent | The destination a move **will** use, shown before release. |
| `{colors.drop-forbidden}` | `{colors.danger}` | An illegal destination — below minimum size, or the layout is locked. |
| `{colors.locked}` | `{colors.stale}` | Layout-locked indicator in the status strip. Amber, because it is a mode the user must be able to notice they are in. |

### Dock stack states

| State | Treatment |
|---|---|
| docked (default) | 1px `{colors.border}`, `{colors.surface-raised}` ground, tab strip 28px |
| focused | 1px `{colors.accent}` on the stack border + accessible name announced. **The focused pane is always visibly indicated** — Premiere's blue-line idea, which is the one thing that exemplar does better than the rest. |
| floating | `{colors.float-chrome}` title bar, `{elevation.dialog}`. The **only** panes permitted to overlap. |
| collapsed | Reduced to a labelled edge strip. **The surface names remain readable** — collapsing hides a pane, it never erases the knowledge that the pane exists (Eclipse trim stacks, Photoshop icon docks). |
| maximized | Fills the tree; siblings are *temporarily* minimized and remembered as such. Restoring undoes what maximizing did, **never what the user did**. |
| at-minimum | Splitter renders `{colors.drop-forbidden}` for `{motion.fast}` and stops. It does not collapse the pane. |
| locked | Splitters lose their hover affordance; drag is inert; the status strip shows the amber lock. |

### Single-surface stacks keep their tab strip

A stack of one still shows its tab. Hiding it would save 28px and cost the user the surface's name,
its close control, and the drag handle that moves it — and would make the chrome change shape as
surfaces come and go. VS Code and Eclipse both keep it; so do we.

### Motion inventory

| Moment | Duration | Why |
|---|---|---|
| Tab selection | `{motion.fast}` | Confirms the switch without delaying it. |
| Drop-target indicator appear/move | **0ms** | It must track the pointer exactly. Any easing makes it lag the intent it is reporting. |
| Splitter drag | **0ms** | Direct manipulation: the pane edge *is* the pointer. |
| Keyboard resize step | **0ms**, announced | Each arrow press is a discrete committed change. |
| Pane float / dock / collapse / maximize | **0ms** | Layout is structure, not narrative. Animating it costs time on every single operation. |
| Layout switch | **0ms** + announcement | A mode change should be instant and *stated*, not performed. |

**Reduced motion changes nothing here** — there is nothing to reduce. That is the correct outcome
for a workbench, and it is why the motion inventory is short by design rather than by omission.

### Copy (real, in-voice)

- `Move Explore — use arrow keys to choose a destination, Enter to place, Escape to cancel`
- `Resize: left edge of Terminal. Arrow keys adjust. Enter to commit.`
- `Explore moved to the right region.` *(announcement, focus unchanged)*
- `Minimum size reached.`
- `Layout is locked. Unlock to rearrange panes.`
- `Layout "Review" applied — pane geometry and open surfaces.`
- `Restored 5 of 6 panes. "Trace Viewer" is no longer available and was not restored.`
- `Display 2 is not connected. "Terminal 2" was moved onto this display.`
- `Workbench layout could not be read and was reset to the default. Your previous layout file was kept.`

---

## Facelift — soft islands (spec-app-facelift)

The workbench evolves from **strict-flat** to **soft islands**: the same dense, evidence-first, AA-legible
surface, now with gently rounded, subtly-elevated panes that read as separate cards — the JetBrains
New-UI/Islands register (`kb-wpf-modern-ui-styling`). **Density is unchanged; only the surface softens.** The
evolution is three facet moves on the existing archetype (grammar G9), not a new archetype:
`Depth: Flat → SoftShadow` · `rounded.lg 8px → 10px (+ rounded.island 12px)` · `Nav: +MenuBar`.

### What softens, and the hard limits

| Softens | Stays hard |
|---|---|
| Pane corners → `{rounded.island}`; window → **DWM** rounded corners + system shadow | **`AllowsTransparency=False`** (or the DWM shadow/corners die — `WPF-TRANSPARENCY-TRAP`) |
| Docked island panes gain `{elevation.resting}` | **No effect over an `HwndHost`/WebView2 pane** (airspace — `WPF-EFFECT-OVER-AIRSPACE`) |
| Floating panes/dialogs use `{elevation.raised}`/`{elevation.dialog}` | Shadows are **few, static, cached** (`CacheMode=BitmapCache`); GPU stays flat |
| Theme is **Fluent-wired**, accent-tracks the OS | Softened greys still meet **WCAG AA** (contrast audit fails the gate otherwise) |
| Layout still animates at **0ms** | Confidence is still **glyph + word + colour**, never colour alone |

Mica is a **Windows-11 / .NET-10** enhancement on the window backdrop only (gutters), gated on availability
(Flagged) and never assumed to show *through* hosted panes.

### Icon system

One permissive line-icon set (recommend **Fluent System Icons**, MIT), single `{icon.stroke}` weight,
`{icon.md}` default grid (`{icon.sm}` in dense strips, `{icon.lg}` in the menu). **Every icon-only control
carries an accessible name and a tooltip** — an icon is never the sole label. Icons inherit `{colors.text}` /
`{colors.text-muted}`; the accent icon is reserved for the one primary action per surface.

### Menu & command system

A **menu bar** (the six top-level names are fixed by the perspective-shell section's PS-M1) is the
*discovery* path over the existing command palette (Ctrl+K, the *power* path). A **Command** = `{ id, label, icon, shortcut,
enabled-predicate, disabled-reason }`. Menu-item states: default / hover / focus / **disabled-with-reason**
(the reason shows on hover — an inert control never leaves the user guessing) / checked. The menu bar surfaces
the same commands the palette runs; they never diverge.

### New tokens for the graph & model surfaces

| Token | Value | Role |
|---|---|---|
| `provenance.verified` | `{colors.verified}` + ✓ + "Verified" | An `EXTRACTED` edge / observed fact |
| `provenance.inferred` | `{colors.inferred}` + ~ + "Inferred" | An `INFERRED` edge (DI/ORM/dynamic) — **dashed** in diagrams |
| `provenance.flagged` | `{colors.unverified}` + ? + "Flagged" | An `AMBIGUOUS`/unconfirmed edge |
| `relationship.inferred-stroke` | 1px **dashed** `{colors.inferred}` | Inferred UML/ER relationship — never shown as a solid extracted fact |
| `banner.readonly` | `{colors.locked}` strip + lock glyph + "Derived view — read-only" | On every generated UML/ER/graph model view |
| `metric.legend` | perceptually-uniform ramp + unit legend | Betweenness/community overlay — **never rainbow/jet** (TQ3) |

**Provenance is a correctness rule, not decoration:** an inferred edge rendered identically to an extracted one
is `GRAPH-PROVENANCE-LAUNDERED` (kg-visualization-ux-expert, hard escalation). Colour is the third signal; the
glyph and the word carry it when hue is unavailable.

### §4a rendering tokens — bounded reads & emphasis (Core→Design requests)

The Core session's view models admit their own bounds (`EvidenceRead.Shortfall`) and carry a dominant
crossing target (`ContextEdge.DominantTarget`/`DominantCount`) and a declared flag
(`ContextMapView.IsDeclared`). Design renders each as an explicit state — a bounded read must never
look complete, a dominant class must not hide in a grey suffix, and an undeclared map is an empty
state, not a sentence.

| Token | Value | Role |
|---|---|---|
| `count.lower-bound` | `≥ N` in `{typography.mono}` + `capped` chip (`{colors.inferred}`) + tooltip naming the cap | A capped/bounded count — visually distinct from an exact count so a shortfall never reads as complete |
| `count.exact` | `N` in `{typography.mono}`, `{colors.text}` | A complete count (no cap bit) |
| `emphasis.dominant` | chip in `{colors.accent}` at `{typography.weight-medium}`, width ∝ `DominantCount` share | Promotes the dominant crossing class out of the grey suffix |
| `emphasis.dominant-bar` | 3px `{colors.accent}` share bar under the crossing row | The "57 of 72 are ORM" signal, made glanceable |
| `state.not-declared` | first-run empty state: `{icon.lg}` glyph + one line + first-action button | `IsDeclared == false` — an empty state, not a heading + muted paragraph |

**The bounded-read rule is a correctness rule (`EvidenceRead.Shortfall`):** a count that is a lower
bound and one that is exact must be **distinguishable at a glance**. `20,000 results` and `≥ 20,000
results (capped)` are different claims; rendering them identically is the surface inventing the
completeness the read could not establish — the same failure class as provenance laundering.

---

## Loomkeeper Observatory

### Direction brief

**User and state.** A multi-agent technical lead arrives with several repositories and terminals in
motion. They are time-constrained, skeptical of inferred claims, and looking for the one session or
learning decision that needs intervention.

**Job-to-be-done.** See which agent threads are healthy, what they share, how each served its stated
goal, what Loomkeeper could not observe, and which repeated patterns deserve review.

**Archetype.** G6 Multi-Panel Data Terminal, specialized to the existing AI-DE workbench:

`LoomkeeperObservatory { Type:DSS; Arch:SPA; Layout:MultiPanelWorkstation; Density:Compact; Nav:CommandPalette+Sidebar; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive; Type:MonospaceTechnical; Depth:SoftShadow; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Instant+Confirmed; Motion:Micro; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA+HighLegibility; }`

**Defining qualities.**

- **Vigilant, not alarmist.** Attention comes from evidence-backed exceptions, never animated noise.
- **Forensic, not punitive.** Scores open into evidence and disputes; the surface never ranks people.
- **Dense, not cramped.** Linked panels expose many sessions while spacing still expresses groups.

**References.**

- Bloomberg/TradingView: linked multi-panel monitoring and tabular numerical scanning; not their
  financial color semantics.
- Linear: keyboard economy, quiet hierarchy, and fast focus movement; not its visual identity.
- Datadog: monitor-to-drill workflow and persistent system health; not equal-weight bento cards.
- AI-DE workbench: the existing activity rail, soft islands, confidence language, and local-first
  evidence contract.

**Anti-goals.** No leaderboard, gamified grade, crypto dashboard, equal KPI tiles, rainbow heatmap,
chat-first navigation, pulsing status wall, opaque "AI says" judgment, or happy-path-only mock data.

**Personality decisions.**

- **Type:** `{typography.ui}` for reading and `{typography.mono}` for paths, identities, timestamps,
  score points, versions, and units. The UI font keeps density legible; mono marks inspectable facts.
- **Color:** `{colors.accent}` is reserved for focus and selected context; `{colors.verified}`,
  `{colors.inferred}`, `{colors.unverified}`, and `{colors.danger}` retain their evidence meanings.
  Status always carries a glyph and word, so color never decides meaning.
- **Space:** compact `{spacing.scale}` rhythm: tight within a session/evidence group, wider between
  groups and panels. Borders are secondary to spacing and surface shifts.

### Trigger map

| Trigger | Applies | Consequence |
|---|---|---|
| UI-T1 expert quantitative | Yes | Tabular numerals, explicit units, uncertainty, provenance, virtualized large lists, no rainbow/jet. |
| UI-T2 generated assets | No | No generated imagery, personas, or motion are needed. |
| UI-T3 model-facing | Yes | Not Recorded, Advisory, Disputed, Quarantined, Blocked, and Retracted are first-class; evidence and oversight precede feedback/promotion. |
| UI-T4 native desktop | Yes | Windows/Fluent keyboard, focus, treegrid, pane, high-contrast, and reduced-motion conventions govern. |

### Surface hierarchy

The focal point is the **session needing attention**, not the aggregate score. The default composition:

1. **Scope and watcher posture** - repository selector, local-only policy, Watcher Health.
2. **Sessions treegrid** - repository/worktree grouping and the selected session.
3. **Session detail** - Activity, Trace, and Weave Scorecard for the selected Work Episode.
4. **Inspector** - evidence, trust, policy, and version for the selected item.
5. **Peer surfaces** - Message Board, Leaderboard, Daydreams, Configuration, Privacy & Capture, Watcher Health.

**Leaderboard and standing.** The Leaderboard ranks harness, model, and harness-model within one
task class and score schema version; every cell carries cohort size and Evidence Coverage, and a
below-minimum or single-human cell renders **Not Comparable**, never a rank. Ranks, cohorts, trends,
and coverage use `{typography.mono}` tabular numerals. Each watched agent's per-turn standing shows
rank, trend, and one evidence-backed reason per dimension, never a single optimizable target.

**Configuration and credentials.** Configuration sets watched harnesses, models, and repositories,
and the credentials the watcher uses. Credentials render only as masked references, never as plain
values, and never appear in logs, board, scores, or learning. The watcher is local-only by default;
a credential-backed egress path stays off until an explicit opt-in notice is accepted, and `Egress
blocked` is the default state.

**Why Trace remains separate from Activity.** Activity is a chronological, human-readable trajectory;
Trace is a causal parent/child span tree used to diagnose which tool or subagent produced an outcome.
They share source events and answer different questions. The separate tab earns its place by
preserving causality that a flat timeline hides.

**Why the review harness includes constrained viewports.** The production archetype remains
`Viewport:DesktopBound`. Mobile/tablet modes are review instruments that stress pane reflow, focus,
and overflow inside a constrained window; they are not a commitment to ship a mobile layout.

**Why a promoted-learning example appears beside a candidate.** The review artifact must expose both
the pre-promotion gate and the post-promotion Retract/Supersede path in one selectable Daydream state.
Production selection shows one detail at a time.

### Status language

| State | Glyph + word | Token role | Meaning |
|---|---|---|---|
| Verified / Alive | check / `Verified`, solid dot / `Alive` | `{colors.verified}` | Observed evidence or fresh liveness. |
| Advisory | tilde / `Advisory` | `{colors.accent}` | Qualitative assessment; never a correctness fact. |
| Inferred | tilde / `Inferred` | `{colors.inferred}` | Reasoned from evidence, not directly observed. |
| Stale | clock / `Stale` | `{colors.stale}` | Was observed, but the time boundary expired. |
| Not Recorded | question / `Not recorded` | `{colors.unverified}` | Evidence is absent or untrustworthy. |
| Disputed | split arrows / `Disputed` | `{colors.focus}` | A superseding adjudication is open or recorded. |
| Blocked | stop / `Blocked` | `{colors.danger}` | A hard floor failed; no numeric headline. |
| Quarantined | shield / `Quarantined` | `{colors.inferred}` | Untrusted or forged content cannot influence authority. |

Shared hue never carries the distinction: Inferred and Stale use different glyphs, words, and row
semantics even when both use amber.

### Sessions treegrid

One roving tab stop. Up/Down changes row; Left/Right collapses or expands repository/worktree groups;
Home/End moves to boundaries; type-ahead selects a matching identity. Virtualization preserves the
focused session identity and reports set size/position. Selection updates the detail pane without
moving focus.

### Scorecard

The hard-floor strip is first. A complete score reads `73 / 100`; incomplete evidence reads
`58 / 70 observed` and is never rescaled. **Evidence Coverage** is adjacent and independently
labelled. Every dimension row exposes source evidence within two actions. Advisory stability,
rubric/model/schema versions, task class, and residual uncertainty remain visible.

The headline has no pass threshold. `Blocked`, `Not scored`, and `Partial` are states, not low numbers.

### Component state matrix

| Component | Complete states |
|---|---|
| Observatory shell | loading, first-run, ready, watcher offline, partial ingest, error, overflow |
| Sessions treegrid | alive, idle, stale, ended, asserted, conflict, blind spot, shell/not scored, focused virtual row |
| Activity / trace | loading, empty, ready, truncated, quarantined, error |
| Scorecard | scoring, complete, partial, Not Scored, Blocked, Advisory, Disputed, stale-input/recomputing, stale-version |
| Message Board | empty, unanswered, acknowledged, quarantined, failed write/draft preserved, stale read, overflow |
| Daydreams | observation, candidate, needs disconfirm, disconfirmed, promotable, promoted, deferred, rejected, retracted, retraction failed |
| Privacy & Capture | notice, capture off/on, redaction failed/drop confirmed, egress blocked, deletion preview/in-progress/partial/complete |
| Watcher Health | healthy, ingest lag, event gap, adapter degraded, grader unavailable, storage pressure, learning-effect counter-metric, offline |
| Command palette / search | default, loading/slow, no results, error, results |

Every empty state uses the `state.not-declared` shape: one symbol, one true sentence, one first action.

### Motion and status announcements

| Moment | Duration | Purpose |
|---|---|---|
| Row selection/focus | `{motion.fast}` | Confirms context without moving layout. |
| Pane/tab switch | 0ms | Operational navigation should not wait. |
| Score recompute completion | `{motion.fast}` color/weight change only | Shows new evidence without counting-up theater. |
| Watcher-wide failure/recovery | 0ms visual state + polite announcement | The state matters; animation does not. |
| Score/rubric/guidance update notice | 0ms static dismissible notice | Prevents a second animated focal point. |

Watcher-wide failures and completed consequential actions announce once through a polite atomic live
region. Heartbeat ticks and routine row updates never announce individually; they are coalesced.
Reduced motion collapses all transitions to zero while preserving the announcement.

### Required copy

- `Watcher offline - sessions continue, observations are paused.`
- `Not recorded - this session did not publish a goal.`
- `Blocked - the correctness floor failed. Open the evidence before continuing.`
- `Advisory - qualitative assessment from rubric v3.`
- `Evidence coverage 78% - 2 required signals were unavailable.`
- `This score evaluates agent behavior for your improvement. It is not a personnel rating.`
- `Local-only - no work content leaves this device.`
- `Registration rejected - another process claimed this session identity. Review the source or start a new session generation.`
- `Message not posted - the repository board could not be written. Your draft is preserved.`
- `Redaction failed - the captured content was dropped before it was stored.`
- `Promotion needs a disconfirming check and your approval.`
- `Deletion is incomplete - 2 derived records remain. Retry the unfinished steps.`
- `Retraction failed - the prior guidance remains in force. Review the failed projection.`

### Performance and accessibility

The reference corpus and p95 budgets are in `spec-agentic-watcher-substrate`. Lists virtualize without
focus loss; state changes cause no layout shift. WCAG 2.2 AA applies to the Observatory, including
target size, focus not obscured, non-drag alternatives, correct treegrid roles/values, high-contrast
mode, and a table/list equivalent for any graph. All values in aligned columns use tabular figures
and explicit units.

---

## The front door — sheet, composer, canvas strip

*Added by `/ui-design` (elevate) from operator feedback on the running app; the review and its ranked
plan are [`docs/reviews/ui-operator-feedback.md`](docs/reviews/ui-operator-feedback.md) and the mockup
is [`docs/mockups/session-front-door.html`](docs/mockups/session-front-door.html). This section adds
**no new colour**. Every rule below is coverage, placement or copy, because the measurement said the
palette was never the defect.*

### Token coverage is opt-out, not opt-in (the rule this section exists for)

The shell themes its **containers** and leaves its **leaves** to the platform. WPF's platform default
is a light theme, so every control created without an explicit brush renders light-on-dark or
black-on-dark, and every newly added control regresses silently. Measured: seven sites of near-black
ink at **1.15:1** and **1.27:1** against a themed ground, and one site of light ink at **1.22:1** on a
platform-default white list.

| Rule | Statement |
|---|---|
| **TC1** | A theme is a set of **implicit (`TargetType`-only) styles** over the base control set, not a set of per-element brushes. The base set is: `TextBlock`, `TextBox`, `Button`, `ToggleButton`, `ComboBox`, `CheckBox`, `RadioButton`, `Label`, `ListBox`, `ListBoxItem`, `TreeView`, `TreeViewItem`, `TabItem`, `PasswordBox`, `RichTextBox`, `Expander`, `GroupBox`, `Window`. |
| **TC2** | **A partial pairing is worse than none.** Theming a foreground without its background, or the reverse, produces the inverse failure. The `ListBoxItem` foreground was themed and its `ListBox` background was not; the result is `{colors.text}` on platform white. Ink and ground are set together or neither is set. |
| **TC3** | A resource key that does not resolve **fails silently**. A resource reference to a missing key is a no-op: no exception, no log, no visual difference from "not themed yet". Every key a control names must exist, and that is a check, not a habit. |
| **TC4** | A window's **non-client area is not part of the app's theme**. Every top-level window opts its caption into the platform's dark mode explicitly, or it ships the OS scheme against the app's ground. |
| **TC5** | **One palette, one copy.** A value duplicated into a stylesheet, a web asset or a recolour map is a second palette that drifts. Hosted web surfaces receive the tokens; they do not restate them. |
| **TC6** | Contrast is **computed from the rendered pairing**, never asserted in prose. A design artifact that states a ratio it did not measure is the mechanism by which a design language can declare AA while the product ships text at 1.15:1. |

**Disabled is a state, not an opacity.** Compositing `{colors.text-muted}` at 50% over
`{colors.surface-sunken}` measures **2.73:1**, below the 3:1 floor for a meaningful graphic. A disabled
control carries its own token pairing that clears the floor, plus a reason on hover. An unreadable
control is not a gentler way of saying unavailable.

That pairing is `{colors.text-disabled}`, and it is a token because it was already a value: the same
grey appeared three times as a raw literal on the disabled menu-item foreground, which is one palette
copy per site waiting to drift (TC5). It measures **5.1:1 on `{colors.surface}`**, **4.6:1 on
`{colors.surface-raised}`** and **5.3:1 on `{colors.surface-sunken}`** — computed from the resolved
brushes by `ContrastFloorTests`, not asserted here (TC6). It is deliberately dimmer than
`{colors.text-muted}`, so *unavailable* and *secondary* remain different readings, and it clears the
**text** floor rather than merely the graphic one: an operator has to be able to read what it is they
cannot use.

### Required fields — a required answer is explained where it is asked

The session task class is required and has no default, and that is correct: it is the cohort key every
comparison is scoped by, and a guessed class is indistinguishable from a chosen one afterwards. The
failure was never the requirement. It was asking for a closed vocabulary through an open text box, and
explaining the rule in the vocabulary of the subsystem that needs it.

| Rule | Statement |
|---|---|
| **RQ1** | A value whose only use is **exact equality against a set** is entered by choosing from that set. A free-text box for such a value makes a typo indistinguishable from an answer, and a typo here is worse than a default: it forms a cohort of one, renders `Not Comparable`, and silently removes the episode from the cohort it belonged to. |
| **RQ2** | The explanation says **what the choice decides for this operator**, not what the mechanism is called. "A defaulted class ranks in the wrong cohort" names a mechanism. "A session's score is only ever compared against sessions of the same class" names a consequence. |
| **RQ3** | The explanation sits **at the field**, above the control, before the answer is needed. Not in a footnote under the buttons. |
| **RQ4** | **Required-and-undefaulted is a visible state**, `{colors.inferred}` with a glyph and the words *Required, no default*, flipping to `{colors.verified}` and *Answered*. It is never a bare asterisk, and never colour alone. |
| **RQ5** | A **disabled primary action states its reason adjacent to itself**, in the same row, naming the field: *Choose a task class to create the session.* An inert control never leaves the operator guessing. |
| **RQ6** | **Recall is not a default.** The operator's own last answer may be offered as a one-click chip, labelled as a suggestion. Nothing is pre-selected, so the type-level no-default contract and the reflective test that pins it both still hold. |

### The composer is a document, not a text box

The session pane is a **serial-entry task inside a parallel-reading surface**. Reading is parallel and
entering is serial, so the composer needs a focal point of its own. The shipped surface gave its
largest, brightest element to a **read-only** compiled view, which is why the operator reported no
editor. The reference the operator named is the Claude application, and the shape the spec already
asks for is a notebook of blocks.

| Region | Rule |
|---|---|
| **Session header** | 28px. Name, workspace, the answered task class as a chip, backend health. The chip is where the sheet's required answer goes on being useful. |
| **The Score** | Prior blocks, each with its id in `{typography.mono}`, its shape (message or goal block plus tier), its confidence as glyph and word and colour, its event count, and its per-block actions. It scrolls; it is the reading half. |
| **The composer card** | An island at `{rounded.island}` with `{elevation.resting}`, pinned below the Score, bordered in `{colors.accent}` while focused. It owns the submission-shape tabs, the editor, the context recipe, and one primary action. |
| **The editor** | The **largest element in the pane**, on a `{colors.surface-sunken}` ground, with a visible caret, focus on session open, and a placeholder that teaches the first action rather than naming the field. |
| **The context recipe** | Mentions render as removable chips in `{typography.mono}` carrying their pin. What the operator sees is what the conductor receives; an invisible recipe is an unverifiable one. |
| **Compiled view** | A **disclosure inside the card footer**, collapsed by default, on `{colors.surface-sunken}` in `{typography.mono}`. It is read-only, so it is never the most prominent field on the surface. |
| **The send row** | One accent primary carrying its chord. Disabled, it states its reason beside it in `{colors.inferred}`. The lease sentence is that reason, not a free-floating line. |

**Complete states for the composer** *(superseded by the perspective-shell section's state set and
`conversation-composer.html`; kept for the front-door mockup's record)*: first-run, drafting,
staged, sending, streaming, refused / wrong answer, error with recovery, overflow.

### The canvas mode strip — one mode now, N later

| Rule | Statement |
|---|---|
| **MS1** | The strip is **one 28px row whose geometry never changes**. Only its population changes, driven by the mode catalog's size. |
| **MS2** | At **one** mode it renders the **pane title**: 12px, `{typography.weight-medium}`, 0.04em, `{colors.text-muted}`. That is the form every other pane uses, so it reads as native rather than as a tab bar with one tab. |
| **MS3** | At **two or more** it renders the workbench's 28px tab strip, and the **split control appears**, bound to catalog size rather than hard-coded. Registering a mode is adding a row; nothing else edits. |
| **MS4** | Neither form is a degraded version of the other, and the transition is a population change, not a layout change. |
| **MS5** | **This does not contradict "single-surface stacks keep their tab strip".** That rule governs **dock stacks**, where the tab carries the surface's name, its close control and its drag handle. A canvas mode carries none of those; it is a view selector inside one surface, so at one mode the pane title is the honest form. |

### The activity rail — destinations, and one action

| Rule | Statement |
|---|---|
| **AR1** | The rail's group holds **destinations**. A verb does not join it. |
| **AR2** | The **one primary action** sits above the group, separated by a divider, in the accent fill. That is the one place the accent is reserved for a primary action. |
| **AR3** | **A rail item is present only if it does something.** A disabled placeholder announcing a mode that does not exist is dead UI, and at rest it is indistinguishable from the one item that works. |
| **AR4** | **Focus is a ring that is actually drawn.** A focus trigger that changes a border *colour* on a control with zero border *thickness* renders nothing. The ring is its own 2px outline, not a recoloured border. |
| **AR5** | No capability's **only** door is an icon in the rail. Every rail destination also has a catalog command, so it reaches the menu, the palette and a chord. |

### Copy added by this section

- `Required, no default`
- `Choose a task class to create the session.`
- `A session's score is only ever compared against sessions of the same class. Pick the one that matches the work.`
- `Your last session used ui-feedback. One click applies it. It is a suggestion, not a preselection.`
- `Describe the change. Press / for a skill, @ to attach context.` *(superseded by the perspective-shell section's placeholder)*
- `Draft staged, not sent`
- `This session has no blocks yet.`
- `Console is the only canvas mode in this phase.`
- `Explorer: graph and reader`
- `Nothing has run yet. Send the first message and the merged stream appears here.`
- `Block b3 is waiting on a ready agent backend. The draft is preserved and was not sent.`
- `A session opens without a ready backend; a run needs one.`

### Recorded deviations (CD16)

| Deviation | Reason |
|---|---|
| `{colors.border}` measures **1.39:1** on `{colors.surface}`, below the 3:1 floor for a UI component boundary | Already declared in the palette table above: the border is decorative and **spacing carries the grouping**. It is never the only signal separating two regions. Re-stated here because a measurement that looks like a failure needs its disposition beside it. |
| The 28px dense strips trip the detector's *cramped padding* rule | 8px of vertical inset is arithmetically impossible inside a 28px row that also holds a 24px control. The density is the archetype's (`Density:Compact`) and the horizontal inset is a full `{spacing.scale}` step. |
| A composer island sits inside a bordered pane, tripping *nested cards* | The soft-islands register is exactly a card inside a pane. The nesting is one level and it is the facelift's stated direction. |

---

## The perspective shell — three benches, one conversation

*Added by `/ui-design` (elevate) for `spec-addendum-c-perspectives`; the review and its ranked plan are
[`docs/reviews/ui-perspective-shell.md`](docs/reviews/ui-perspective-shell.md) and the mockups are
[`docs/mockups/perspective-shell.html`](docs/mockups/perspective-shell.html),
[`docs/mockups/conversation-composer.html`](docs/mockups/conversation-composer.html) and
[`docs/mockups/new-session-sheet.html`](docs/mockups/new-session-sheet.html). This section adds
**one ink role** (`{colors.accent-contrast}`, now explicit as the only ink on the accent ground), **one boundary token** (`{colors.border-strong}`), **re-tones one** (`{colors.syntax-comment}`), and
declares the **light values** of every role. Its acceptance test is the runtime contrast census, `tests/AiDe.App.Tests/ShellContrastCensusTests.cs` (INV-0008), over the composed shell and the composer page in both themes (spec §C7). Everything else is pairing, placement, rhythm and copy.*

### Direction brief (DX5)

**Who, and in what state.** The operator: one technical lead, one screen, three jobs on one repository,
serially, many times a day. They arrive **in flow** (a terminal live, a console streaming, a permission
request due) and never browsing. Eighty percent of their time is in Coding, talking to a session.

**The job.** Be in **one use case at a time** with the surfaces, the menu and the default arrangement
fitted to it: **J1** drive agentic coding by talking to a session as in a chat; **J2** walk the graph;
**J3** understand the code broad-to-specific. The shell's own job is the switch between them, and the
switch must feel like *turning to a different bench in the same workshop*: instant, stated, lossless.

**Archetype.** The shell's signature is the frontmatter's `PerspectiveShell { … Arch:HubAndSpoke … }`
(spec §C1): the rail is the hub, each perspective a spoke; **the spokes are read in parallel** (three
destinations always visible with their state) **and entered serially** (exactly one body). Verified
against the shape of each task inside it: the Coding and Architecture hosts are parallel-reading
`MultiPanelWorkstation`s; Explore is the explorer's own Spatial-Canvas × Master-Detail; **the composer
is serial entry and takes D1 · Generative Stream Thread as its nearest row** (`Feedback:Generative`,
U13–U15 adopted; `Layout:StreamingThread` not adopted: the thread is the session document, composer
beside canvas, not an auto-scrolling column); the New Session sheet is one short form, `Pacing:UserDriven`
inside a modal, no wizard. The header's previous `Arch:Desktop` was not a grammar value and `Depth:Flat`
lagged the facelift; both corrected here (`docs/notes/addendum-c-design-signature.md`).

**Three adjectives, and their opposites.** **Fitted, not cluttered** (nothing on screen that is not
about the job the operator is in). **Conversational, not clerical** (the composer is a message you
write, whose structure appears beneath it; never a form you fill above a render). **Legible, not dim**
(every ink is a token pair that clears AA in both themes; the operator's directive, verbatim: *"stop
putting dark fonts on dark backgrounds"*). Inherited from the spec: *predictable, not surprising*;
*quiet, not decorated*.

**Named references, and what is taken.** **Eclipse perspectives**: a task-scoped view set with a menu
contribution, switched often; not its chrome (fetched, spec §A11). **VS Code's activity bar**: an
icon-only rail, one active item, tooltip + accessible name; not its colours (fetched, spec §A11).
**Outlook for Windows**: `Ctrl+1 … Ctrl+8` switch its top-level views (Mail, Calendar, People …), which
is the Fluent-side precedent for `Ctrl+1/2/3` as perspective gestures (support.microsoft.com, fetched
2026-09-11 **[Verified]**). **Cursor's chat input**: `@` typed in the one input attaches context with
suggestions as you type (cursor.com/docs, fetched 2026-09-11 **[Verified]**; how a selection renders is
not documented there, so chips are this design's choice **[Inferred]**). **The Claude application**: the
operator's own comparable (*"does not feel like a chat conversation"*): one editor, attachments beneath
the text, settings elsewhere; **not** bubbles, avatars or a centred column. The operator's verdict is the
evidence; the product was not fetched **[Flagged]**. **JetBrains New UI / Islands**: the facelift's
register, unchanged.

**Anti-goals.** Not a dashboard of equal tiles. Not a form of boxes above a render. Not a chat of
bubbles. Not a rail of placeholders (AR3: the 4-mode rail that was tried and deleted). Not dim-on-dark
or light-on-light, in any state, in either theme. Not a slide between destinations (three directions,
no meaning). Not a per-prompt settings field, ever (Ruling 56), and not a tier field anywhere the operator types (the operator's correction of 2026-09-11: tier is compiled, not typed). Not the accent as decoration.

**Constraints.** WPF on Windows (.NET 10), Fluent conventions, UI Automation names and states, a
high-contrast mode that maps to system colours; AvalonDock hosts and the named-zone model unchanged; the
composer editor is a WebView2 page (no effect over it, airspace; tokens reach it as CSS variables);
`Density:Compact` with 28px rows and strips; one screen, startup width; WCAG 2.2 AA is a hard floor with
the runtime census (§C7) as its acceptance test; no generated imagery.

### Personality in three moves (DX6)

- **Type.** `{typography.ui}` for chrome and the conversation; `{typography.mono}` for a path, a write
  scope, a compiled prompt: anything the operator must read character by character before an agent
  acts on it. The composer's editor is UI type, not mono: it is a message, not code.
- **Colour.** One accent, spent on exactly three things: the rail's primary action, the one active
  destination / selected-active tab (one of each on screen at any instant), and links + focus. Semantic
  hues only beside a glyph and a word. In light mode the accent darkens to `{colors.light-accent}` so
  the same three uses clear the floor; nothing is "lightened".
- **Space.** The compact rhythm: **4 within a line, 8 between lines, 12 between regions, 16 pane
  padding**, and within-group visibly tighter than between-group. Lines of text are grouped by spacing
  and a hairline, never by a box: the composer has zero bordered fields at rest.

### Trigger map (mapped at Stage 1)

| Trigger | Fires? | Consequence here |
|---|---|---|
| UI-T1 expert/quantitative | **Narrowly** | The routing shell is not a quantitative surface (its nearest rows are H1/G6 already). TQ2/TQ7 apply to every number it shows: `budget` with its unit and precision, `fan-out 3`, `showing 40 of 212`, `3 panes`: tabular numerals, unit-bearing, a lower bound rendered as one. |
| UI-T2 generated assets | **No** | Nothing under `docs/assets/` is produced; VA1–VA22 inert. |
| UI-T3 fronts a model | **Yes** | The composer fronts the assist deriver (D-5) and the conductor. Wrong derivation, refused send, lane refusal and "no provider" are first-class states; Governor / Trust-builder→Disclosure / Wayfinder named below. |
| UI-T4 native client | **Yes**: `native-desktop` · `windows` · `wpf` · distribution: local unsigned build · a11y API: UI Automation · HIG: Fluent | The HTML mockups are direction evidence only. Native PASS is the runtime Proof Pack (spec §A13 P-1…P-13), measured at the slice; the review lists every row as *not measured* rather than claiming it. |

### "Not chunky", measured (the density contract)

The operator's word is *chunky*; the diagnosis is a set of numbers the mockup meets and the slice must
meet at the shell's startup width (P-12):

| Property | Value | Why |
|---|---|---|
| Chrome above the editor's first line | **28px** (the session header) | One row. The shape control and the settings affordance live in it; nothing else sits above the message. |
| Editor share of the composer zone | **≥ 45 %** of the zone height; never below its declared minimum (5 lines, 130px) | The message is the focal point; everything beneath it is derived from it. |
| Rows beneath the editor | **4 at rest** (structure collapsed to one line · inherited settings · write scope · compiled summary), **7 with the structure expanded**, each **24px** | Lines of 12px type, not boxes. Expanding the structure adds three 24px lines beneath the editor and never moves its top edge. Measured at the shell's startup Center height (≈ 580px). |
| Bordered text fields at rest, other than the editor | **0** | A line of text is grouped by spacing and a hairline, never by a box (DX13). The editor carries a `{colors.border-strong}` boundary because it is an input (1.4.11); nothing beneath it does. |
| Type sizes in the composer | **3**: 13px editor · 12px lines and labels · 11px key labels (`Ctrl+Enter`) | Hierarchy by scale, not weight (DX12); the 15px empty-state heading belongs to the canvas beside it. |
| Animated moments in the composer | **2**: structure expand `{motion.base}`, derived-mark clear `{motion.fast}` | Plus the caret. Nothing else moves (DX19). |
| Per-prompt settings fields | **0** | Ruling 56 as corrected by the operator (2026-09-11): the fan-out **ceiling** and the budget are read from the session and shown as one muted line that links to their one home; **tier is not typed anywhere**: it is a decoration the compile step attaches to the compiled prompt, shown on the compiled disclosure as a derived value the operator sees and confirms at send. |

### The rail: one action, three destinations (AR1–AR5 kept; PS-R1–PS-R4 added)

| Rule | Statement |
|---|---|
| *(composition, active signal)* | The rail's composition (New session above the divider; Coding · Explore · Architecture; Tests reserved and absent) and the active signal (the 3px bar plus the accent glyph; the pill is decorative) are spec §B2 and §C3 and are not restated here. |
| **PS-R2** | The destinations are a **single-selection group with manual activation**: one selected item; **Up/Down move focus only** (nothing switches, nothing announces); **Space, Enter, the bound gesture, or a click switch**; Tab leaves the rail (roving tab-stop); activating the selected item is a no-op that emits nothing. Exposed to UIA as a vertical **tab list** (`TabItem` with `SelectionItemPattern`, `IsSelected` = active), not as radio buttons: a WPF `RadioButton` group selects on arrow, which would switch perspective on the first Down and make the *opening* state ("focus stays on the trigger") impossible. Deviation from spec §C4's "radio-group item", recorded with this rationale; the View menu keeps `menuitemradio`. |
| **PS-R4** | Every rail glyph is a registry entry (`IconAdd`, `IconExplore`, and two new ones, `IconCoding` and `IconArchitecture`, drawn on the `{icon.md}` grid at `{icon.stroke}`), never inline path data. The tooltip is *"Coding — Ctrl+1"* with the keystroke **rendered from the bound `KeyGesture`'s display string**; no bound gesture, no keystroke shown. |

| Rail item state | Ink | Ground | Extra signal |
|---|---|---|---|
| rest | `{colors.text-muted}` | `{colors.surface-sunken}` | none |
| hover | `{colors.text}` | `{colors.surface-raised}` | tooltip |
| focus | as rest/hover | as rest/hover | 2px `{colors.focus}` **outer** ring (`{colors.focus}` on the accent-filled New session item is 1.5:1, so the ring is never inset there) |
| **selected (active)** | `{colors.accent}` | `{colors.surface-raised}` | 3px `{colors.accent}` bar, `aria-selected` / `IsSelected` exposed (SelectionItemPattern) |
| **opening** (first entry) | `{colors.text-muted}` | `{colors.surface-raised}` | progress ring on the glyph; status *"Opening Architecture…"*; focus stays on the trigger |
| **error** (body failed to build) | `{colors.text-muted}` | `{colors.surface-sunken}` | `{colors.danger}` badge glyph; tooltip and `ItemStatus`: *"Couldn't open Architecture: <reason>. Activate to try again."* |
| disabled | *does not exist* | none | AR3: a destination that cannot be entered is removed |
| **New session** rest / hover / focus / pressed | `{colors.accent-contrast}` | `{colors.accent}` (hover: same, `brightness 1.12`) | never disabled: with no workspace the chooser interposes |

### The dock tab strip: the states the census found (PS-T1–PS-T3)

The runtime census measured every selected-active tab caption at **2.37:1** (`text` on `accent`,
an implicit `TextBlock` style overriding the container's ink) and the selected-*inactive* tab at
**1.45:1** (sunken ink on `{colors.border}` used as a ground). Both are the same defect: a state that
named a ground without naming its ink, or an ink without its ground.

| Rule | Statement |
|---|---|
| **PS-T1** | **Every tab state names both an ink and a ground from the matrix above.** A leaf text element never states its own ink: the container pairs, the leaf inherits (INV-0008's Fix A, now merged). **No local ink is ever set under an accent trigger**: when a trigger paints `{colors.accent}` as the ground, the only ink it may name is `{colors.accent-contrast}`, and no child sets one of its own. |
| **PS-T2** | Exactly **one** selected-active tab exists on screen: the tab of the stack that holds focus. It is the only accent-filled tab, and that is what makes it the focused-pane indicator (Premiere's blue line, kept). |
| **PS-T3** | A selected-inactive tab is the document's own ground (`{colors.surface}`) under the strip's raised ground, **with a 2px `{colors.text-muted}` top edge as its ≥ 3:1 state indicator** (6.48 / 6.39:1 on the strip) — the ground shift alone is 1.08:1 and the ink shift 2.09:1, neither of which identifies the state (1.4.11); it never borrows a line token as a ground. |

| Tab state | Ink | Ground | Extra signal |
|---|---|---|---|
| rest | `{colors.text-muted}` | `{colors.surface-raised}` (the strip) | none |
| hover | `{colors.text}` | `{colors.surface-raised}` | close control appears |
| **selected-active** | `{colors.accent-contrast}` | `{colors.accent}` | 6.6 / 6.2:1; one on screen |
| **selected-inactive** | `{colors.text}` | `{colors.surface}` | 2px `{colors.text-muted}` top edge (the `ui` indicator, 6.48 / 6.39:1); 15.0 / 15.3:1 for the caption |
| focus (keyboard) | as state | as state | 2px `{colors.focus}` outer ring |
| dragging | `{colors.text}` | `{colors.float-chrome}` | `{elevation.raised}`; drop target per the layout tokens |
| disabled | *does not exist* | none | a tab you cannot select is a surface that should not be in the stack |

### The menu bar and palette: derived, named, honest (PS-M1–PS-M4)

| Rule | Statement |
|---|---|
| **PS-M1** | Top-level names are **File · Edit · View · Window · Prompt · Help**. Five are the code's names, kept; the sixth was the code's *Terminal*, renamed **Prompt** because, with `terminal.new` and the harness rows moved to File as entry verbs (US-C11), it holds only *Dispatch prompt…* and *New prompt draft* — a Terminal menu with no terminal verb teaches the operator to distrust the menu. The earlier *Graph · Model · Agents* set is retired: the derivation rule (spec §B3) places every allow-list entry under **View**, so a Model menu would need a second placement rule, and harness sessions are entry verbs under File, so an Agents menu would offer what File already does (`docs/notes/addendum-c-design-menu-names.md`). Which menus each perspective has is spec §B3 (Coding: all six; Explore: File · View · Help; Architecture: all but Prompt). |
| **PS-M3** | Structurally inapplicable → **absent**; transiently unavailable → **disabled with a reason** on hover — and a disabled row never takes the hover ground (its ink fails 4.5:1 on `float-chrome`). New session is never disabled. The File menu's harness rows sit under a group caption *Terminal sessions*, so the two nouns the operator meets there (a *session* document; a Claude Code *terminal session*) are told apart by the group, not by the row title the profiles derive. |
| **PS-M4** | An item shows a keystroke only when one is **bound**, rendered from the binding's display string. An unbound chord (`Ctrl+K, X`) is shown and spoken nowhere until a chord handler exists. The palette row for such a command reads its title alone. |

| Menu item state | Ink | Ground |
|---|---|---|
| menu ground / rest | `{colors.text}` | `{colors.surface-raised}` (1px `{colors.border}` frame, `{elevation.raised}`) |
| pointer hover | `{colors.text}` | `{colors.float-chrome}`, the lifted ground (a 1.14 / 1.10:1 step: a courtesy for the pointer, never an indicator) |
| **keyboard-highlighted / focused row** | `{colors.text}` | `{colors.surface-raised}` + a **2px `{colors.focus}` ring drawn inset on the row** (8.59 / 8.51:1) — the ground step is sub-3:1 in both themes, so the ring is the indicator (SC 1.4.11, 2.4.7); the same rule for the palette's and the mention picker's active row, and the design never relies on a Fluent template adding a focus visual of its own |
| **disabled, with reason** | `{colors.text-disabled}` (4.59 / 5.57:1) | `{colors.surface-raised}`; the reason in `{colors.text-muted}` on hover |
| **checked** (the active perspective) | `{colors.text}` + `{colors.accent}` check glyph | `{colors.surface-raised}` |
| keystroke column | `{colors.text-muted}`, `{typography.mono}`, tabular | as row |

### The switch, and what it says

`Transition:HardCut`: the body is the same rectangle; a slide across three destinations reads as travel
that did not happen. The active bar and glyph change over `{motion.fast}`. **The announcement is a
mechanism, not an ordering:** the shell raises a UIA notification
(`AutomationPeer.RaiseNotificationEvent(ActionCompleted, ImportantAll, "<Perspective> perspective…")`)
**before** it calls `Focus()` on the new body's first focusable (Coding: the active document's editor, else
the Left zone's active tab; Explore: the search box, never the canvas; Architecture: the Center's active
tab), and each body carries the constant name *"<Perspective> perspective body"* so the focus move is a
second carrier. A polite live region beside an immediate focus move is exactly the case where a screen
reader speaks the target first and drops the text; P-9 is the falsifier. A routed kind-open announces
*"Opened Class diagram in Architecture"*; its failure announces the failure string and moves nothing.
The window title reads *"<workspace> — <Perspective> — AI-DE"*. The status strip's **message is the live
region** (`role=status` on the text, not on the strip, so the health chip and the Dismiss control never
announce); a long report wraps to two lines and is focusable, never trapped in a tooltip; the count chip
is `{typography.weight-medium}`; Dismiss is keyboard-reachable.

### The two default layouts and their empty states

The layouts are spec §B4 and their copy is spec §C4, rendered verbatim by `perspective-shell.html`;
neither is restated here. The design adds one rule: every empty state is the `state.not-declared`
shape — `{icon.lg}` glyph, one true sentence, one first action — never a heading over a muted
paragraph, and never a second explanatory sentence in front of the first action. Its ink is a
deliberate disposition, not a default: the heading and the first action in `{colors.text}`, the one
sentence in `{colors.text-muted}` (7.16 / 5.90:1 on `surface`) so the action reads first; a label
whose only job is to be read (a line label, a section header) is `{colors.text-muted}` on a ground
where the matrix clears 4.5:1, and `{colors.text}` everywhere else. A disallowed kind
restored from a saved layout is **dropped and reported**, never silently, in the plural forms the spec
fixes; a newer-schema envelope is refused with a report and the file kept; a duplicate one-instance
kind keeps its first copy and reports the second.

### The composer is a conversation (PS-C1–PS-C6)

The front-door section above made the composer *a document, not a text box*. Ruling 57 goes one step
further: **a conversation, not a form.** One editor, structure derived beneath it, the compiled prompt on
demand, settings elsewhere.

| Region (top → bottom) | Height | Ink / ground | Rule |
|---|---|---|---|
| **Session header** | 28px | `{colors.text}` on `{colors.surface-raised}` | Name · task-class chip · **shape control** (Free-form ▾ / template picker, kept from Addendum B `:181`) · the **session-settings affordance** (*Session settings*) · backend health. |
| **Editor** | fills; min 5 lines | `{colors.text}` on `{colors.surface-sunken}` inside the raised island, bounded by 1px `{colors.border-strong}`; placeholder `{colors.text-muted}` | The **largest element in the pane, with the highest-contrast ink** (15.6:1); caret visible; focus on open; placeholder in voice. `@` opens the mention picker at the caret (a listbox of options, a sibling of the editor, never inside it); a mention renders as a `{typography.mono}` chip in the text. Never a system-default white `TextBox`. Spec §C3 says *on `{colors.surface-raised}` (the island)*; the front-door section put the editor on the sunken ground inside the island so the island and the field read as two things — kept, recorded as a deviation for the spec's owner. |
| **Derived structure** | 24px collapsed → 3 × 24px | labels `{colors.text-muted}`, values `{colors.text}`, both on `{colors.surface-raised}` | *Goal · Done when · Not in scope* as editable lines, not boxes; the *derived* mark is `{colors.inferred}` glyph + word; an *invalid* mark is `{colors.danger}` glyph + one-line reason. Expands beneath the editor over `{motion.base}`; **the editor's top edge never moves.** |
| **Inherited settings line** | 24px | `{colors.text-muted}` on raised; the link `{colors.accent}` | *"fan-out ≤ 3 · budget 40,000 tokens — from session settings"*. The text is the link. **No per-prompt override, and no tier here.** The fan-out value is the session's **ceiling**; the effective cap is the compiled tier's cap within it (0 at T0, 2 at T1, the GO7 cap at T2) **[Inferred: the conductor's reconciliation of CT19 with the operator's correction, not yet the operator's word]**. |
| **Write scope line** | 24px (one per pattern) | `{colors.text-muted}`; the pattern in `{typography.mono}` `{colors.text}` | *"Write scope: src/AiDe.Core/Workbench/** — from your mention"*; none: *"Write scope: none yet — mention the files this run may write as @path"*. |
| **Compiled prompt** | 24px collapsed | summary `{colors.text-muted}`; the tier decoration `{colors.inferred}` glyph + word (derived) or `{colors.text}` (confirmed); body `{typography.mono}` `{colors.text}` on `{colors.surface-sunken}` | An `Expander` named *"Compiled prompt"*; collapsed by default; exactly the outgoing text; **no diff, no summary**; header says *updated* when stale. **The tier is a decoration the compile step attaches**: the header reads *"Compiled prompt · T1 derived"* (or *"tier not derived yet"* before the draft settles); the operator sees it and confirms it by sending. How the tier is derived, and the compile step itself, are **not designed here**: the operator will specify them separately. |
| **Send row** | 36px | Send: `{colors.accent-contrast}` on `{colors.accent}` | Send (Ctrl+Enter) · shape badge · a refusal reason beside a disabled Send, in `{colors.inferred}`. |

| Rule | Statement |
|---|---|
| **PS-C1** | **One editor, zero field boxes, zero compiled text at rest.** The composer's visual tree contains no `TextBox` for compiled text and no field widgets until the structure expands, and the expanded structure is lines, not boxes. |
| **PS-C2** | **Send is the confirmation.** A derived line is confirmed by sending or by editing it; there is no separate confirm act, so a prompt at defaults is one action. |
| **PS-C3** | **A refusal marks every gap at once**, inline, with one line of reason each, names the fix, and names the tier that requires it — the *compiled* tier, since the session has none: *"This prompt compiles at T2 and needs Done when."* A missing write scope says what derives one and offers the picker: *"Send needs a write scope. Mention the files or folders this run may write, as @path (for example @src/AiDe.Core/) — the lease is derived from your mentions."* **After any refusal Send is disabled with the reason beside it** until the gap is filled — one refusal grammar, not two. The rule that decides which tier a prompt compiles to is the operator's compile-step specification (not yet written); until it exists the refusal path's slice is gated on it. |
| **PS-C4** | **The page draws from the theme's tokens.** The host pushes, on `host.init`, exactly these eleven CSS custom properties from `Application.Resources` (`ComposerPageTheme.Roles`, merged with INV-0008): `--surface`, `--surface-raised`, `--surface-sunken`, `--text`, `--text-muted`, `--text-disabled`, `--accent`, `--accent-contrast`, `--border`, `--danger`, `--focus`; the stylesheet reads each with the token's declared value as its fallback for the pre-push frame and carries no second palette (TC5). A role the page needs beyond these (`--inferred` for the derived mark, `--verified` for the edited mark, `--border-strong` for the editor's boundary) is one additive row in `Roles`, never a literal. The census (`EveryTextPairingInTheComposerPageClearsItsFloor`, `TheComposerPageDrawsWithTheTokensTheShellPushed`) walks the page's DOM. |
| **PS-C5** | **No provider, no pretence.** With no assist provider the three lines are empty and editable and say *"fill in, or add an assist provider in settings"*; with a provider deriving, the structure shows a one-line skeleton, never a spinner over the editor; a wrong derivation is corrected in place (HAX G9). |
| **PS-C6** | Focus order across the WebView2 boundary, in DOM order: shape control → session settings → **editor** → the structure header (an Expander) → Goal → Done when → Not in scope (each an editable line) → the settings link → the compiled prompt's header → Attach → Mention → **Send**, and back with Shift+Tab. The write-scope line is derived text, not a stop. Escape inside the page never leaves the document (P-13). Each editable line's **accessible name is constant** (*Goal*, *Done when*, *Not in scope*); its state (*derived* / *edited* / *missing*) is `AutomationProperties.ItemStatus` (WPF) / `aria-describedby` → the mark (page); a refusal reason is `HelpText` / `aria-errormessage`; the placeholder *fill in, or add an assist provider in settings* is a watermark, never the value. |
| **PS-C7** | **Tier is compiled, not typed.** No field, chip or setting anywhere lets the operator type a tier. The compiled disclosure carries the derived tier as a decoration with three states: *not derived yet* (before the draft settles, `{colors.text-muted}`), *derived* (`{colors.inferred}` glyph + *T1 derived*), *confirmed* (cleared on send, `{colors.text}`). A T0 derivation in a session whose fan-out ceiling is non-zero says *"T0 derived · no fan-out"* on the line, with the full sentence *"this run uses no fan-out (session ceiling 3)"* in its description. **Exposure:** the expander's accessible name stays *"Compiled prompt"*; the tier state and *updated* are its `AutomationProperties.ItemStatus` (WPF) / `aria-describedby` (page), and the tier is spoken at the confirmation points — the refusal announcement (*"This prompt compiles at T2 and needs Done when"*) and the in-flight announcement (*"Sending block b3 as a goal block, tier T1"*) — so a screen-reader operator hears the decoration before and at the send, not after it. The tier decoration is **never styled as a control**. |

**Complete states.** Spec §C4 names them and `conversation-composer.html` renders them (its State
control); this section adds only what the spec does not have: the editor's **attaching** (a drop
target over the editor, *"Drop to attach"*) and **attaching-off** (*"Attaching files is off for this
session."*, `{colors.text-muted}` on the island — the census's `#drop-hint` site, now a token pair)
states; the compiled prompt's **tier not derived yet · tier derived · tier confirmed**; a
**template** state (the shape control picked *change-order*, its fields rendered as the same derived
lines: prefilled where the text supplies a value, empty-editable where it does not); and a **session
settings** state (the header's affordance opens the sheet's *Session settings* row in a popover — one
control, two doors). The inherited-settings line has **no warning state**: the T0 reading lives on the
compiled header (PS-C7).

### The New Session sheet carries the session settings (PS-S1–PS-S4)

| Rule | Statement |
|---|---|
| **PS-S1** | The **fan-out ceiling** and the **budget** are session settings with defaults, prefilled from workspace policy, editable in the sheet and later from the session header. **Tier is not a session setting and has no field**: the operator's correction of 2026-09-11 (*"shouldn't tier be decided by the compilation of the prompt?"*) overrides Ruling 56's tier clause; tier is a decoration the compile step attaches (PS-C7). The sheet still creates with one click on the defaults; the only undefaulted field remains the task class (RQ1–RQ6). |
| **PS-S2** | The two settings are one **row of two controls** under the heading *Session settings*, each with its unit and its source (*"workspace default"*), never a field per line. Fan-out is labelled as a ceiling (*"Fan-out ceiling"*, *"most sub-agents any turn may convene"*); budget is numeric with tabular figures and a unit suffix (TQ2). |
| **PS-S3** | Derive, don't store: the effective cap of a run is the compiled tier's cap (0 at T0, 2 at T1, the GO7 cap at T2) **bounded by the session's ceiling** **[Inferred: the conductor's reconciliation, awaiting the operator's compile-step specification]**. The sheet therefore explains the ceiling in those words and carries no tier warning; the *"T0 derived: this run uses no fan-out"* line lives on the compiled disclosure, where the tier is known (PS-C7). |
| **PS-S4** | The sheet's non-client caption opts into the platform's dark mode (TC4); every control in it carries a token pairing (TC1–TC2), and every text or numeric field carries a `{colors.border-strong}` boundary so an editable setting is identifiable at rest (1.4.11). Create failure keeps the sheet's values and states the reason beside Create. There is no note explaining the absent tier field: the fan-out ceiling's own sentence says what decides the tier, and explaining an absence is a placeholder in prose. |

### Motion inventory (DX19)

| Moment | Duration | Why |
|---|---|---|
| Perspective body swap | **0ms** + announcement | `Transition:HardCut`; the region is the same rectangle. |
| Rail active bar + glyph | `{motion.fast}` | Confirms the switch without delaying it. |
| Derived structure expand / collapse | `{motion.base}` | Continuity: the lines grow from beneath the editor; the editor never moves. |
| Derived-mark clear on edit/send | `{motion.fast}` colour/weight only | Feedback that the operator's word replaced the system's. |
| Compiled prompt disclose | `{motion.fast}` | Same as every expander in the shell. |
| Menu open, tooltip appear | `{motion.fast}` opacity | Fluent's own timing. |
| Everything else (tab switch, layout, drop-with-report chip) | **0ms** | Layout is structure, not narrative. |

Under reduced motion every row is 0ms and every announcement still fires.

### Copy added by this section

- `Coding — Ctrl+1` · `Explore — graph & reader — Ctrl+2` · `Architecture — Ctrl+3` *(tooltips; the keystroke from the binding)*
- `Coding perspective` · `Explore perspective` · `Architecture perspective` *(accessible names)*
- `Architecture perspective — 4 panes` · `Opened Class diagram in Architecture` · `Coding perspective — session payments extraction opened` *(announcements)*
- `Opening Architecture…` · `Opening Architecture… (still building).`
- `Couldn't open Architecture — the graph service did not answer in 10 s. Activate to try again.`
- `Couldn't open Class diagram — Architecture failed to open: the graph service did not answer in 10 s.`
- `No session open.` · `Or open a recent one from File → Recent sessions.`
- `No view open.` · `Or open Domain, Contexts or Joins from the View menu.`
- `Select an evidence row to see its provenance.` · `Nothing indexed yet. Run Index from the File menu.` · `This row's provenance couldn't be read.`
- `3 panes from your saved layout aren't available in Coding — Domain (class diagram), Graph, Contexts. Architecture opens with Graph, Domain, Contexts and Joins; open anything else from its View menu.`
- `1 pane from your saved layout isn't available in Coding — Graph. Architecture opens with Graph, Domain, Contexts and Joins; open anything else from its View menu.`
- `Your saved layout had no panes Coding can show, so Coding opened with its default layout.`
- `What should this session do? Mention the files it may write as @path.` *(editor placeholder)*
- `Goal · Done when · Not in scope — will appear as you write`
- `fill in, or add an assist provider in settings`
- `fan-out ≤ 3 · budget 40,000 tokens — from session settings`
- `Compiled prompt · ~ T1 derived` · `Compiled prompt · tier not derived yet` · `Compiled prompt — updated · T1 confirmed` · `~ T0 derived · no fan-out` *(description: this run uses no fan-out — session ceiling 3)*
- `Write scope: src/AiDe.Core/Workbench/** — from your mention`
- `Write scope: none yet — mention the files this run may write as @path`
- `Send needs a write scope. Mention the files or folders this run may write, as @path (for example @src/AiDe.Core/) — the lease is derived from your mentions.`
- `This prompt compiles at T2 and needs Done when.` · `This prompt compiles at T2 and needs a Goal.` · `This prompt compiles at T2 and needs Not in scope — the boundary is what the lease is checked against.` · `2 lines need filling — this prompt compiles at T2.`
- `Compiled prompt` · `Compiled prompt — updated` · `Drop to attach` · `Attaching files is off for this session.`
- `Editor starting…` · `Editor couldn't start — WebView2 runtime not found.` + `Retry`
- `Couldn't send — the conductor closed the connection. Your draft is kept.` + `Try again`
- `Sending block b3 as a goal block, tier T1.` *(announcement)*
- `Session settings` · `workspace default` · `Fan-out ceiling` · `Most sub-agents any turn may convene. The compiled tier decides how many it uses, up to this.` · `Budget` · `tokens per session` · `Choose a workspace first` · `Pick a workspace; the New Session sheet follows.`
- `Couldn't create the session — the workspace daemon is not running. Your answers are kept.`
- Budget is always written `40,000 tokens` — thousands separator, unit, one precision everywhere (TQ2).

### AI-UX (U13–U15; the composer only)

**Shape of AI:** the derived structure is a **Governor** (the plan the operator reviews before the action);
the *derived* mark and the **tier decoration on the compiled prompt** are **Trust builders → Disclosure** (what the system produced versus what the operator wrote, and what the compile step attached); the placeholder and the collapsed structure line are **Wayfinders**. **HAX:** G1/G2 (the marks say
what was derived and that it may be wrong), **G9** efficient correction (edit in place, never a modal),
**G11** (with D-5: each derived line names the sentence it came from, on hover/focus). A wrong derivation
is a first-class state; the send is always the operator's explicit act; the compiled disclosure is the
trust builder: no hidden prompt assembly.

### Performance budget (U17)

Retained perspective switch p95 ≤ 150ms (P-8), first entry recorded and reported; no graph query
re-issued, no process restarted, no document re-created on a switch; the derived structure's derivation
runs off the UI thread and never blocks typing; the drop-with-report path delays first paint by no more
than its string formatting; no layout shift when the structure expands (the editor is above it, the
send row is pinned).

### Recorded deviations (CD16)

| Deviation | Reason |
|---|---|
| `{colors.focus}` on `{colors.accent}` is 1.5:1 (dark) / 1.37:1 (light) | The ring is drawn **outside** the accent-filled item, against the rail's sunken ground (9.9 / 7.1:1). A ring inset on the accent item would fail 1.4.11; the outer ring is the rule, not an exception. |
| `{colors.text-disabled}` on `{colors.float-chrome}` (dark) is 4.01:1 | A disabled menu row never takes the lifted hover ground; it stays on `surface-raised` (4.59:1) and shows its reason. |
| `{colors.light-border}` on `{colors.light-surface}` is 1.45:1 | The same decorative deviation as the dark border: spacing carries the grouping; the border is never the only signal. Controls do not rely on it: they carry `{colors.border-strong}`. |
| *Nested cards* and *cramped padding* on the 28px strips | The front-door section's two recorded deviations, unchanged. |
| A hovered accent fill is `brightness(1.12)`, not a token | The value is derived from `{colors.accent}` and clears 8.24 / 5.22:1 with `accent-contrast`; the WPF slice uses the Fluent accent-light brush. Recorded so the non-token ground is a decision. |
| The composer's editor is a WebView2 page and the census cannot walk it yet | Reported as *not measured*, never as a pass (§C7). The mockup's live audit measures the design's pairs; the page's real pairs are measured when the census walks the DOM. |
