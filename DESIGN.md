---
name: AI-DE Workspace
description: Design language for the AI-DE desktop workspace — a dense, calm, evidence-first shell for directing coding agents.
archetype: "Workbench { Type:OLTP; Arch:Desktop; Layout:MultiPanelWorkstation; Density:Compact; Nav:CommandPalette+Sidebar; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive; Type:Utilitarian; Depth:Flat; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Optimistic; Motion:Micro; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }"
colors:
  surface: "#12151A"
  surface-raised: "#1A1F26"
  surface-sunken: "#0D1014"
  border: "#2A313B"
  text: "#E4E9EF"
  text-muted: "#98A3B2"
  accent: "#5B9DD9"
  accent-contrast: "#0D1014"
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

| Token | Role | Contrast (on `{colors.surface}`) |
|---|---|---|
| `{colors.text}` | primary evidence text | 13.9:1 — AA/AAA body |
| `{colors.text-muted}` | secondary metadata, `not recorded` | 6.4:1 — AA body |
| `{colors.accent}` | selection, focus affordance, links | 6.1:1 — AA body, AA non-text |
| `{colors.verified}` | Verified confidence chip | 7.2:1 |
| `{colors.inferred}` / `{colors.stale}` | Inferred confidence, stale state | 8.3:1 |
| `{colors.danger}` | failed extraction, delivery failure | 5.6:1 — AA body |
| `{colors.focus}` | 2px focus ring, always visible | 8.9:1 against surface and raised |
| `{colors.border}` | 1px separators; never the only grouping signal | 1.6:1 — decorative, spacing carries the grouping |

*Contrast figures are computed against `{colors.surface}` and re-measured by the contrast audit in the
UI craft gate; a token that drops below AA fails the gate, it does not get a waiver.*

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
| Light | inverted roles, same semantics | Confidence hues re-picked for AA on a light ground, not naively lightened. |
| Windows high contrast | system colours | Token roles map to system brushes; glyph+text confidence keeps meaning when hue is unavailable. |

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

A **menu bar** (File · Edit · View · Graph · Model · Agents · Window · Help) is the *discovery* path over the
existing command palette (Cmd/Ctrl-K, the *power* path). A **Command** = `{ id, label, icon, shortcut,
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
