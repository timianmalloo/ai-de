---
id: domain-experts
title: "AI-DE Domain Experts — the project's subject-matter lenses"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [personas, domain-experts, roster, graph-visualization, wpf, uml, erm]
links:
  - { to: knowledge-hub, rel: relates-to }
  - { to: kb-graph-experience-and-visualization, rel: documents }
  - { to: kb-wpf-modern-ui-styling, rel: documents }
  - { to: kb-uml-mde-and-4gl, rel: documents }
  - { to: kb-domain-modeling-and-erm, rel: documents }
review-by: 2027-02-27
review-suggested: []
summary: >-
  The three subject-matter expert lenses added to AI-DE's persona swarm — knowledge-graph
  visualization/UX, modern WPF styling, and UML/ERM modelling — each with its lens, seam against the
  general personas, veto, and grounding bases, plus the candidates the gate rejected.
---

# AI-DE Domain Experts

The pack's twenty-three lenses are domain-*general*. This project's domain is an **AI-native IDE that builds
and renders a code+knowledge graph and styles it in a modern WPF shell** — so it needs subject-matter judgment
the general lenses do not carry: *is this graph visualized truthfully, is this WPF styling mechanically correct,
is this UML/ER model notation-valid?* These three experts close that gap. Each conforms to the Persona Operating
Standard (`persona-audit.md` §8) and is deployed as `.claude/agents/<slug>.md` + `.github/agents/<slug>_agent.md`.

**Roster after this addition: 23 general lenses + 3 domain experts = 26.**

## The domain, from repo evidence

- **Code + knowledge graph** — `src/AiDe.*` (WPF app, Core, Daemon), `docs/knowledge/code-knowledge-graphs`,
  `graph-experience-and-visualization`, and the pack's `code-knowledge-graph.md` (Graphify) / `obsidian-lens.md`.
- **Modern WPF shell** — `src/AiDe.App` (.NET 10 WPF), `DESIGN.md` (the workbench design language),
  `docs/knowledge/wpf-modern-ui-styling`, `ai-native-ide-shell`.
- **UML / ERM surfaces** — `docs/knowledge/uml-mde-and-4gl`, `domain-modeling-and-erm`, `diagram-generation`,
  `docs/design/conceptual-model.md`.

**Domain failure map** (where a *domain* error is most expensive/silent/irreversible):
1. A graph view that **misleads** — a hairball, an inferred edge shown as fact, 3D hiding the real structure — is
   silently wrong and erodes trust in the whole tool. *(→ KG-Visualization expert)*
2. WPF styling that is **mechanically broken** — `AllowsTransparency` killing the DWM shadow, effects vanishing
   over a terminal pane — ships looking wrong and is only caught on the real composited window. *(→ WPF-Styling expert)*
3. A UML/ER model that is **notation-invalid or an editable derived view** — misstates a relationship, or lets a
   generated diagram diverge from the code — is the exact failure that killed CASE/MDA. *(→ UML/ERM expert)*

## The three experts added

| Expert | Lens (domain-correct = …) | Veto | Grounding bases |
|---|---|---|---|
| **kg-visualization-ux-expert** | the graph view does not mislead — bounded neighbourhood, stable layout, edge provenance shown, 2D-default, insight from metrics | **Soft** on graph-correctness; **hard escalation** of provenance laundering | `graph-experience-and-visualization`, `editor-and-content-rendering-surfaces`, `diagram-generation`, code-knowledge-graph.md GK6–7 |
| **wpf-styling-expert** | modern-soft WPF is mechanically right — DWM corners+shadow kept, no effects over airspace, shadow budgeted, Fluent-wired, AA-legible | **Advisory**; **hard escalation** of the AllowsTransparency trap & effect-over-airspace | `wpf-modern-ui-styling`, `ai-native-ide-shell`, MS "rounded corners" doc |
| **uml-erm-modelling-expert** | the model is notation-valid and a *view*, not a source — UML/ER semantics correct, C4 level fits, derived views read-only | **Soft** on editable derived views & misleading notation | `uml-mde-and-4gl`, `domain-modeling-and-erm`, `diagram-generation`, UML 2.5.1 / SysML v2 |

### Seams (each expert vs the general lenses)

- **kg-visualization-ux-expert** — vs **Domain Researcher**: DR establishes Sigma.js's/Graphify's *API*; this
  expert judges the *visualization's truthfulness*. vs **UX & Accessibility**: UX-A owns WCAG/state completeness;
  this expert owns graph-truth (hairball, provenance, 2D/3D fit, bounded context). vs **Data & Persistence**: D&P
  owns the graph *store/model*; this expert owns its *rendering*.
- **wpf-styling-expert** — vs **Native Desktop Developer**: NDD owns Windows HIG/packaging/signing; this expert
  owns the *WPF styling mechanics* (DWM opt-in, airspace, effect perf, Fluent wiring). vs **UX & Accessibility**:
  UX-A owns the inclusion floor; this expert owns the *means* to reach it in WPF.
- **uml-erm-modelling-expert** — vs **Data & Persistence Architect**: D&P owns the real schema/migration/aggregate
  invariants; this expert owns the *notation-correctness of the modelling artifacts* and the derived-view rule.
  vs **Domain Researcher**: DR establishes a diagram tool's API; this expert judges the model's semantics.

### Owned domain anti-patterns (recommend for `persona-audit.md` §8.8)

| Anti-pattern | Owner | Shape |
|---|---|---|
| `GRAPH-HAIRBALL` | kg-visualization-ux-expert | a graph view with no progressive disclosure |
| `GRAPH-PROVENANCE-LAUNDERED` | kg-visualization-ux-expert | edges rendered without their `EXTRACTED/INFERRED/AMBIGUOUS` confidence |
| `WPF-TRANSPARENCY-TRAP` | wpf-styling-expert | `AllowsTransparency=True` disabling DWM shadow/corners |
| `WPF-EFFECT-OVER-AIRSPACE` | wpf-styling-expert | WPF effects expected to composite over HwndHost/WebView2 |
| `MODEL-VIEW-EDITABLE` | uml-erm-modelling-expert | a generated diagram treated as an editable source |
| `UML-NOTATION-INVALID` | uml-erm-modelling-expert | notation that misstates the true relationship |

### Convene-when triggers (for `persona-audit.md` §8.7)

- **kg-visualization-ux-expert** — the change renders, navigates, filters, or queries the code/knowledge graph.
- **wpf-styling-expert** — the change styles/themes the WPF shell, window chrome, panes, icons, menu, or theme.
- **uml-erm-modelling-expert** — the change produces or renders a UML or ER model/diagram.

### Casting (which workflows they join — extends `collaborative-personas.md` §5)

| Skill | kg-visualization-ux | wpf-styling | uml-erm-modelling |
|---|---|---|---|
| `/specify` | peer+adversary (graph surfaces) | peer+adversary (facelift) | peer+adversary (UML/ERM surfaces) |
| `/ui-design` | peer+adversary | **peer lead** (WPF means) | peer+adversary |
| `/design` | peer+adversary | peer+adversary | peer+adversary |
| `/implement` | adversary | adversary | adversary |

## Candidates considered and rejected (Simplifier's discipline)

- **"3D Graphics / WebGL Rendering Expert"** — rejected; the graph viz is delegated to established MIT libraries
  (Sigma.js/3d-force-graph), and the *rendering-correctness* judgment folds into kg-visualization-ux-expert
  (2D/3D fit, occlusion). A separate WebGL seat is sprawl.
- **"Monaco / Editor Integration Expert"** — rejected; the editor-and-content-rendering base + the general
  language Developers + wpf-styling-expert (airspace) cover it; no distinct *domain* error class remains.
- **"Ontology / Semantic-Web (RDF/OWL/SPARQL) Expert"** — rejected; AI-DE's graph is a property graph, not a
  formal ontology; no RDF/OWL surface exists in the repo. Revisit only if a formal ontology is introduced.
- **"General Data-Visualization Expert"** — rejected; overlaps `technical-ui-design.md` (TQ) and the UX lenses;
  the graph-specific slice is owned by kg-visualization-ux-expert and the dashboard slice by
  `operational-and-test-dashboards` + UX & Accessibility.
- **"Standalone C4/Architecture-Diagram Expert"** — rejected; folded into uml-erm-modelling-expert (C4 is part of
  its remit) and the existing Enterprise Architect.

## Gate record

`GATE domain-experts · 2026-08-29 · Orchestrator + Product Strategist + Domain Researcher (peers) / Simplifier + Patterns Expert + Tech Lead (adversaries) · exit criteria met: each expert passes the Simplifier test, states its seam vs the Domain Researcher and the nearest general lens, is grounded in cited bases, and has a proportional veto · verdict: PASS-WITH-CONDITIONS (developer confirmation assumed under autopilot; trim any expert whose seat you dispute) · vetoes: none unresolved`

> **Note:** run under autopilot without an interactive developer confirmation step (DoD normally requires it).
> The roster is presented here for confirmation/trim; each expert is independently removable.
