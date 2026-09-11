---
id: note-addendum-c-design-signature
title: "DESIGN.md's header signature, the on-accent ink, the light values and the comment re-tone — four token-system decisions below ADR weight"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, design-language, tokens, contrast, archetype, light-theme]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: ui-review-perspective-shell, rel: relates-to }
  - { to: mockup-perspective-shell, rel: relates-to }
  - { to: ui-review-operator-feedback, rel: refines }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Records why DESIGN.md's archetype header now reads PerspectiveShell with Arch:HubAndSpoke and
  Depth:SoftShadow (the old Arch:Desktop was not a grammar value; Flat lagged the facelift), why
  accent-contrast is kept under its name with an explicit role (a rename to text-on-accent was made
  and reverted the same day when main merged the code's AccentContrastBrush), why the light theme is
  declared as flat light-* keys under colors:, why syntax-comment moved from #5A6472 to #808C9A, and
  why border-strong exists. Each carries the alternative
  it rejected and the check that would show it wrong.
---

# DESIGN.md's signature and the four token decisions

- **Kind:** decision (below ADR weight; the design language's own header and four tokens)
- **Made by:** node D1 (`/ui-design`, elevate), session `addendum-c-chain`, 2026-09-11
- **Evidence opened:** `DESIGN.md:4-48` (the old header and palette), `.claude/knowledge/ui-archetype-grammar.md` §2 (the EBNF: `ArchitectureV` has no `Desktop`), `spec-addendum-c-perspectives` §C1 (the two findings for D1) and §R rows 16, 23, 24, `INV-0007` on `investigate/contrast-census` §7 Fix B (the on-accent pairing), the detector's `design-system.mjs` (`addColorObject` reads only string values directly under `colors:`), `design-lint.py` (`parse_tokens` reads block children at indent ≤ 4).

## 1. The header signature

**Decision.** `Workbench { … Arch:Desktop; … Depth:Flat; … Feedback:Optimistic; … A11y:WCAG_2.2_AA }`
becomes `PerspectiveShell { Type:OLTP; Arch:HubAndSpoke; Layout:MultiPanelWorkstation; Density:Compact;
Nav:Sidebar+CommandPalette+TopBar; … Depth:SoftShadow; … Feedback:Instant+Confirmed; …
A11y:WCAG_2.2_AA+ReducedMotion; x-platform:windows; x-framework:wpf; }` — the spec's §C1 signature.

**Because.** `Arch:Desktop` is not a value of the grammar's `ArchitectureV` (a novel value must be
`x-Desktop`), so the header was syntactically invalid **[Verified — EBNF read]**; the shell's routing
under Addendum C *is* hub-and-spoke (rail = hub, perspectives = spokes, read in parallel, entered
serially); `Depth:Flat` contradicted the facelift section three screens below it, which moved the
register to soft islands; `Feedback:Optimistic` was never true of a switch that is instant and
confirmed by a live-region announcement. **Rejected:** `Arch:x-Desktop` (keeps the word, says nothing
about routing) and leaving the header alone (a design language whose header a linter would reject).
**Falsifier:** a grammar validator rejecting the new header; a surface in the shell whose routing is
not hub-and-spoke (there is none: the session documents are dock documents inside a spoke).

## 2. `accent-contrast` keeps its name and gains its role (a rename made and reverted)

**Decision.** The token stays `accent-contrast` (`#0D1014` dark, `#FFFFFF` light). Its role is
stated in the palette table in the words `note-20260911-on-accent-ink-is-its-own-token` carries: *the
only ink on `{colors.accent}` as a ground — selected-active tab, checked toggle, selected row, mention
chip — 6.6:1; no other token is legal there.* **History:** D1 first renamed it `text-on-accent`
(pass 1 of this run) so that no site would borrow a ground token as an ink; while the run was in
progress `main` merged INV-0008, whose code names the same value `AccentContrastBrush` and pushes it
to the composer page as `--accent-contrast`. Two names for one role across code and design is the
derive-don't-store defect, so the rename was reverted and the *role wording* — the part that
prevents the borrowing — was kept.

**Because.** The census found the accent-as-ground family failing at 2.37:1 because states named a
ground without naming an ink, and the app's own New session glyph used `SurfaceSunkenBrush` (a
ground token borrowed as an ink) rather than a named on-accent ink **[Verified — `MainWindow.xaml:78`]**.
A name that says *ink on accent* makes the borrowing visibly wrong at the call site; *accent-contrast*
reads as a property of the accent. **Rejected:** keeping both names (two tokens, one value: the
derive-don't-store defect); keeping `accent-contrast` and documenting it (the name is the defect).
**Blast radius:** none now — the spec's §C3 citation, the code, the composer page and the mockups all
say `accent-contrast`. **Falsifier:** a site that pairs `{colors.text}` with `{colors.accent}` — the
census row (`ShellContrastCensusTests`, 180 pairings, 0 below floor on `main` at `cb4a6ebe`).

## 2a. `border-strong`

**Decision.** A boundary token for controls at rest — input, numeric field, button, select —
`#7C8896` dark / `#5F6977` light (5.28 / 4.64:1 on the sunken grounds), the same value as
`text-disabled` by design (one dim grey for chrome that is not ink). **Because.** The UX &
Accessibility lens measured the sheet's and the editor's boundaries at 1.04–1.39:1: a field that
reads as text is not identifiable as a field (SC 1.4.11), and `{colors.border}`'s CD16 deviation
covers separators, not control boundaries (the contrast fix's UX&A condition names the same thing).
**Rejected:** a lighter field ground (a 1.15:1 step, still not a boundary); reusing `text-disabled`
by name (an ink token as a border is the borrowing this note exists to stop). **Falsifier:** a control
whose boundary measures below 3:1 on its ground in either theme.

## 3. Light values as flat `light-*` keys under `colors:`

**Decision.** Every colour role gains a sibling key `light-<role>` under `colors:` with its light
value; the Modes table points at them.

**Because.** Two readers, both mechanical: the detector accepts only string values directly under
`colors:` (a nested `light:` map would be ignored, so a light mockup would report every colour as
*outside DESIGN.md*), and `design-lint.py` treats block children at indent ≤ 4 as tokens (a nested map
would create a token named `light` and collide `surface` with itself). Flat keys are the only form both
accept without editing either script, which the pack forbids ad hoc. **Rejected:** a nested map
(rejected by both readers); a second `DESIGN.light.md` (a second palette, TC5); a sidecar `DESIGN.json`
(the detector supports one, the linter does not). **Cost:** 29 keys. **Side-effect, recorded honestly
(CD15):** declaring the light values legalised six colours the legacy mockups already used from the
harness template, so the corpus baseline moved 104 → 98 findings without any of those mockups changing;
the values are real tokens now, not a widening to silence a rule. **Falsifier:** a light-mode mockup
whose colours the detector still reports as undocumented.

## 4. `syntax-comment` `#5A6472` → `#808C9A`

**Decision.** Re-toned to clear 4.5:1 on all three dark grounds (5.35 / 4.84 / 5.57:1).

**Because.** The old value measured 3.05–3.18:1 (spec §R row 23, reviewer-computed, re-computed here)
and fails the first code node the composer or the code viewer renders. **Rejected:** dispositioning
it in the register as "comments are secondary" — a comment is text and the operator reads it; and
re-using `{colors.text-disabled}`'s value (a comment is not disabled). The new value sits between
`text-disabled` and `text-muted` so *dimmed* stays readable. **Falsifier:** the census row for a
rendered comment token below 4.5:1.

## Not decided here

The high-contrast theme's values (they are the OS's); whether the spec's `{colors.accent-contrast}`
citation is amended by erratum or by the next spec revision (the conductor's).
