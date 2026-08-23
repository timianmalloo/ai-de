---
id: kb-uml-open-questions
title: "UML, MDE & 4GL — open questions & the case against the thesis"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, disconfirming, mda-critique]
links:
  - { to: kb-uml-mde-and-4gl, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The five structural forces that have defeated every models-are-the-product generation since
  the 1970s, assessed one by one against the code-derived-views variant — the most important
  disconfirming analysis in this knowledge base.
---

# Open questions & domain failure modes

## Unresolved by research

1. **No post-2020 UML adoption survey exists.** Every adoption claim here rests on Petre's 2013 study of 50
   engineers. The direction of travel (a spec unchanged since 2017, a transformation stack stale since 2016)
   suggests the finding has not reversed, but that is an inference. *(Flagged)*
2. **Dobing & Parsons and Störrle sample sizes** could not be confirmed from primary access. *(Flagged)*
3. **The UML 14-diagram taxonomy** was not read from the specification PDF (access limited); it is standard
   knowledge consistent with the OMG page. *(Inferred)*
4. **There is no peer-reviewed literature on LLM + MDE.** ArXiv searches returned nothing relevant. The
   thread exists only as practitioner tooling. This is an absence of evidence, not evidence of absence — but
   it means the project cannot cite a literature it is extending. *(Flagged)*
5. **Whether SysML v2's approach is succeeding** — it is one year old. Adoption data does not yet exist.
   *(Open)*
6. **Whether low-code's documented limits are quantified anywhere.** The "last 10%", lock-in and governance
   collapse are consistently reported and, as far as this research found, never measured. *(Flagged)*

## The historical failure modes — this domain's real content

Every generation since the 1970s has been defeated by the same five forces. They are worth stating as a
checklist, because any new attempt (including ours) is checked against them.

1. **Irreducible complexity.** Brooks's argument still holds: much of software complexity is *essential*,
   not accidental. Models abstract detail away; real systems need it back.
2. **The escape hatch collapses the claim.** Every model-as-product system eventually needs a route to
   general code — PBNI, OutSystems external components, Power Apps plus Azure Functions. **Once that route
   is used routinely, the model has become documentation.**
3. **Productivity evaporates under maintenance.** Model-first platforms accelerate initial development and
   are documented as slower and more expensive for debugging and evolving complex logic in proprietary
   editors — with lock-in meaning the "model asset" cannot be migrated out.
4. **The workforce votes with its tools.** After decades of CASE, MDA and low-code investment, professional
   developers overwhelmingly use text-based general-purpose languages in text-based IDEs. UML survives as
   informal sketch (Petre 2013).
5. **Model and code diverge.** Any system where both are maintained and independently edited will diverge.
   This is a coordination problem, not a tooling problem — which is why better tools have never fixed it.

## Disconfirming views we deliberately sought

**The strongest case: "models as the product" is a repeatedly failed idea, and this is attempt number six.**

The argument is the cycle itself — CASE (1980s) → 4GL/RAD (early 1990s) → MDA (2000s) → low-code (2010s) →
LLM spec-driven (2020s) — each promising that a higher-level representation would subsume general-purpose
code, each defeated by the five forces above. It is a strong argument precisely because it is *empirical
rather than theoretical*: it is not a prediction, it is a record.

### How the code-derived-views variant fares, force by force

| Force | Applies to us? | Why |
|---|---|---|
| **1. Irreducible complexity** | **Partially** | We do not claim to *remove* complexity, only to make it navigable. The residual risk is that derived views show *what* exists and not *why*, which is where the essential complexity actually lives |
| **2. Escape hatch collapse** | **No** | The code **is** the source. There is nothing to escape *from* — the mechanism that killed every 4GL is structurally absent |
| **3. Maintenance productivity** | **No** | Developers never edit models, so the proprietary-editor maintenance penalty cannot arise |
| **4. Workforce tool resistance** | **No** | Nothing is asked of the developer's workflow. Views are generated from what they already write |
| **5. Model/code divergence** | **No** | The model is *derived*. Divergence is structurally impossible rather than merely discouraged |

**Verdict: the variant is meaningfully different, and four of the five forces do not apply.** Its closest
successful analogues are living-documentation systems (Structurizr, C4 with code-first tooling, Backstage)
and code-intelligence platforms (Sourcegraph) — both of which *work*, which is the encouraging part, and
neither of which is as ambitious as a cross-domain graph, which is the unproven part.

### The residual risks this leaves standing

- **Derived views may not capture what developers actually need.** They show structure; the questions people
  ask are usually about intent. A perfectly accurate class diagram answers a question nobody asked.
- **The value depends entirely on extraction quality.** If the code's structure is poor, the derived model
  is noisy — and *garbage in, faithful garbage out* is not obviously better than no model.
- **Whether graph-level richness beats existing living-documentation tools is unestablished.** Structurizr
  and Backstage already do the cheap 80%. The marginal value of the graph is an empirical question the
  project has not yet answered.
- **We are not extending a literature.** There is none for LLM + MDE. That is freedom and it is also the
  absence of anyone else's mistakes to learn from.

### One more disconfirming note, aimed inward

The LLM spec-driven tools (Tessl, Kiro, spec-kit) are making the **MDA claim again** — *"the specification
becomes the maintained artifact, rather than the code"* — and Thoughtworks' caution that *"handcrafting
detailed rules for AI ultimately doesn't scale"* is the MDA critique arriving early. Our design does **not**
make that claim, and the discipline worth keeping is to notice if it ever starts to. The moment a generated
view becomes editable, or a spec becomes the thing maintained instead of the code, this project has quietly
joined the cycle it was designed to avoid.
