---
id: review-ui-mockups-craft-gate
title: "Craft-gate review — facelift mockups"
type: doc
status: accepted
owner: "@copilot-design"
phase: "facelift"
tags: [ui-design, craft-gate, review, facelift]
links:
  - { to: mockup-app-facelift, rel: documents }
  - { to: mockup-knowledge-explorer, rel: documents }
  - { to: mockup-uml-erm-surfaces, rel: documents }
review-by: 2026-11-27
summary: >-
  The deterministic UI craft detector (ui-craft-gate.py / Impeccable) run over the five
  facelift mockups: measurement, translated findings, and the ranked plan. Material
  token-discipline and a11y findings were fixed this run; the residue is review-harness
  chrome and deliberate dense-IDE meta.
---

# Craft-gate review — facelift mockups

**Control:** `ui-craft-gate.py` wrapping Impeccable (deterministic, LLM-free), reading `DESIGN.md`
natively. Corpus: all 5 mockups (`app-facelift`, `knowledge-explorer`, `uml-erm-surfaces`,
`context-map-join`, `workbench`). Non-empty corpus confirmed (CD9). **A clean run is a floor,
never a verdict (CD13):** the detector cannot see archetype fit, IA, or whether the hard states
exist — those were judged by the human/adversarial layers and by the approved mockups.

## Measurement (DX23) — before this run's fixes

| Severity | Count |
|---|---|
| Major | 56 |
| Minor | 13 |
| **Total** | **69** |

| Rule | Count | Disposition |
|---|---|---|
| `undersized-ui-text` | 42 | **Review-harness chrome** (persona/theme/viewport switch labels) — CD14, never ships |
| `cramped-padding` | 10 | Dense-IDE meta rows + harness bar — deliberate density (DX17) |
| `tiny-text` | 7 | 11px body in dense meta contexts — deliberate IDE density |
| `design-system-color` | 6 | **FIXED** — 4 were the code-node syntax palette (now documented in DESIGN.md); 2 were a scrim + highlight (now tokenized) |
| `nested-cards` | 3 | Proof/evidence sub-cards — intentional grouping, one level |
| `skipped-heading` | 1 | **FIXED** — `<h5>`→`<h4>` in knowledge-explorer |

## Fixed this run

- **Token discipline (CD12 — Major floor).** The code-node view used a Material-Palenight syntax
  palette (`#C792EA` keyword, `#82AAFF` type, `#5A6472` comment, `#C3E88D` string, `#B08CD9`
  highlight) that was not in `DESIGN.md`. Documented it as an explicit **`syntax:` group** — a
  deliberately separate colour system (chrome tokens never colour code; syntax tokens never
  colour chrome). Added a `scrim` token for the overlay. `design-system-color` → **0** on
  knowledge-explorer; `design-lint.py` stays clean.
- **Accessibility (a11y).** Heading skip `<h3>` → `<h5>` corrected to `<h4>`. `skipped-heading` → **0**.

## Residual (accepted — CD14 / deliberate density)

The remaining ~13 findings per mockup are:
- **Review-harness micro-labels** at 10–11px (persona · viewport · state · theme · motion switch).
  This is review chrome (DX10) and never ships to production; the detector cannot distinguish it
  from the surface (CD14).
- **Dense meta rows** at 11px in the IDE surfaces — a deliberate density calibration for an
  expert audience (DX17: density demands *stronger hierarchy*, not larger type), consistent with
  VS Code / JetBrains meta text.

## Ranked plan

**Must fix:** none outstanding (token-discipline + a11y fixed this run).

**Should fix next:**
1. If the harness is ever promoted beyond review chrome, lift its micro-labels to ≥11px.

**Worth doing:**
2. Re-audit dense meta rows against the 11px floor if a non-expert surface reuses them.

**Highest improvement-to-effort change (done this run):** documenting the syntax palette — one
DESIGN.md edit cleared the entire `design-system-color` cluster and satisfied the token-discipline
floor without changing a single pixel.
