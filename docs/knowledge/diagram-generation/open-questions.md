---
id: kb-diagrams-open-questions
title: "Diagram Generation — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, determinism, layout-stability]
links:
  - { to: kb-diagram-generation, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The unsettled questions (SVG determinism, Mermaid's published version, TALA's licence), the
  ways generated-diagram pipelines reliably fail, and the sought counter-argument that
  generated diagrams communicate worse than hand-drawn ones.
---

# Open questions & domain failure modes

## Unresolved by research

1. **Is rendered SVG byte-deterministic?** This gates the plan to commit generated diagrams. `d2` and
   Graphviz `dot` are expected deterministic for identical input; Mermaid-CLI renders through
   Puppeteer/Chromium, where font metrics reach the SVG geometry, so output plausibly differs across Chrome
   versions. **Settle empirically**: render the same source twice on two Chrome versions and diff.
   *(Flagged)*
2. **What is Mermaid's published npm version?** The monorepo root says `10.2.4`; `packages/mermaid/CHANGELOG.md`
   says `11.17.0`. Pin from `npm view mermaid version` at build time, not from either file. *(Flagged, [S1][S2])*
3. **What is TALA's licence?** D2's best layout engine for architecture diagrams is a separate proprietary
   binary whose terms were not fetched. Read `terrastruct/TALA`'s LICENSE before using it anywhere. *(Flagged)*
4. **Does `elkjs` genuinely run in a Web Worker?** It is claimed and it is the main argument for choosing ELK
   over dagre in an interactive pane, but it was not confirmed from the README here. *(Flagged)*
5. **How is layout stability actually achieved for a regenerating diagram?** Graphviz `pin` and commercial
   incremental layout are the known answers; whether pinning by stable node ID across regenerations is
   workable at our sizes is untested. *(Open)*
6. **Do we generate Structurizr DSL and export, or generate each renderer's DSL directly?** Export costs a
   Java toolchain in the pipeline; direct generation costs C4 correctness and N generators. *(Open)*
7. **How do PlantUML and D2 carry alt text?** Mermaid has `accTitle`/`accDescr`; no equivalent was found for
   the others, which makes accessibility a renderer-selection criterion. *(Flagged — absence, not confirmed)*

## Known failure modes of this domain

- **The regenerated-diagram shuffle.** A one-node change re-runs the layout and moves everything. The
  diagram is correct, the diff is enormous, and the reader has to re-learn the picture. This is the most
  likely way a generate-on-save pipeline becomes unloved.
- **Committing non-deterministic renders.** If SVG output varies across renderer versions, every CI run
  produces a diff that is pure noise, and real changes hide inside it. The failure is quiet: everything is
  green and nobody trusts the diagrams.
- **`LEGACY` in production.** A PlantUML server or CI job left on the default profile can read arbitrary
  local files and fetch arbitrary URLs from diagram source. Diagram source in this project is *generated*,
  which lowers but does not remove the risk — anything that can influence the generator can influence the
  renderer. *(Verified, [S7])*
- **Over-generation.** Automated tooling emits *everything*, and complete component-level diagrams of real
  systems are unreadable. The discipline of leaving things out has no automated equivalent, and Simon Brown
  explicitly advises against the code level. *(Verified, [S11][S12])*
- **The wrong renderer for the size.** Putting a 2,000-node dependency graph through a DSL→SVG pipeline
  produces a file nothing can render usefully. The correct answer is the interactive tier with filtering,
  and no DSL pipeline reviewed offers "N hops from X".
- **Mistaking Mermaid's C4 syntax for the C4 model.** It renders C4-shaped boxes without enforcing C4's
  rules, so a generator with a bug can emit a hierarchy violation that renders perfectly. *(Inferred)*
- **Licence surprise at packaging time.** MPL-2.0 (D2), EPL (Graphviz, elkjs) and GPL (PlantUML full build)
  are all in the default toolchain for this domain. Discovering that at ship time is expensive.

## Disconfirming views we deliberately sought

**The strongest counter-argument: generated diagrams communicate worse than hand-drawn ones, and the
"never goes stale" claim is weaker than it sounds.**

1. **Layout optimises the wrong thing.** Automated layout minimises crossings and edge length; a human
   architect clusters by team ownership, business capability, or trust boundary. A Sugiyama layout clusters
   by graph-theoretic rank, which is not a meaning anyone holds in their head. Structurizr itself provides
   manual layout overrides, and the C4 site's own examples are hand-tuned — a tacit admission.
2. **Spatial arrangement carries meaning.** The classic argument (Larkin & Simon 1987) is that a diagram's
   power comes from its spatial organisation. If the arrangement is computed rather than designed, proximity
   is arbitrary — and readers will still read meaning into it. A misleading diagram is worse than none.
   *(Flagged — cited via the research summary, not fetched)*
3. **Mental-map destruction is a real cost**, and the mitigations (pinning, incremental layout) are either
   manual configuration or commercial.
4. **Over-generation.** Completeness is the enemy of communication at L3 and below.
5. **"Diagrams in code don't go stale" is only true if the source is derived.** Hand-authored DSL in a repo
   drifts exactly like a PNG; the tooling does not enforce freshness, the *process* does.

**How it fared:** points 1–4 survive fully and are the reason this base recommends budgeting for layout
stability, refusing L4 diagrams, and moving large graphs to the interactive tier. Point 5 **fails against
our specific design and is the strongest argument for it**: the objection is precisely that hand-authored
DSL drifts — and this project's core claim is that the DSL is generated from extracted artifacts, so drift
is structurally impossible rather than merely discouraged. That is the one place where "generated" is
strictly better than "authored", and it is worth stating plainly because it is the project's actual thesis.

The residual risk the objection leaves standing: **derived-and-fresh is not the same as derived-and-useful.**
A perfectly current diagram whose layout scrambles weekly, or which shows all 400 components, is fresh and
unread. Freshness was never the hard part.
