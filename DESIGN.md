---
name: AI-DE Workspace
description: Design language for the AI-DE desktop workspace — a dense, calm, evidence-first shell for directing coding agents.
archetype: "KeyboardVelocity { Type:OLTP; Arch:Desktop; Layout:MasterDetail; Density:Compact; Nav:CommandPalette+Sidebar; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive; Type:Utilitarian; Depth:Flat; Sync:LocalFirst; Persistence:Session; Feedback:Optimistic; Motion:Micro; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }"
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
typography:
  ui: "Segoe UI Variable Text, Segoe UI, system-ui, sans-serif"
  mono: "Cascadia Mono, Consolas, ui-monospace, monospace"
  scale: [11px, 12px, 13px, 15px, 18px, 22px]
  weight-normal: 400
  weight-medium: 600
rounded: { sm: 3px, md: 5px, lg: 8px }
spacing: { scale: [2px, 4px, 6px, 8px, 12px, 16px, 24px, 32px] }
elevation: { flat: none, raised: "0 1px 2px rgba(0,0,0,0.35)", dialog: "0 8px 24px rgba(0,0,0,0.5)" }
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

Master-detail at `{spacing.scale}` rhythm: 8px inside a row, 12px between groups, 16px pane padding.
Compact density means 28px list rows — enough for a 24×24 target plus separation, no more. Depth is
flat: `{elevation.raised}` for the pane, `{elevation.dialog}` for the dispatch confirmation, nothing else.

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
