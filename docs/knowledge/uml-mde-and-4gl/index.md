---
id: kb-uml-mde-and-4gl
title: "UML, Model-Driven Engineering & 4GL Design Surfaces — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [uml, mda, mde, sysml, 4gl, low-code, generative-design, dsl]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for "the models are the product" — the fifty-year graveyard of that idea from
  CASE to MDA to low-code, the narrow conditions under which it has actually worked, and why
  the code-derived-views variant escapes four of the five historical failure modes.
---

# UML, Model-Driven Engineering & 4GL Design Surfaces — domain knowledge

**Domain & problem:** AI-DE rests on the thesis *"code is transient; the models are the product"* — visual
models are derived views over a graph extracted from real artifacts, never hand-drawn. This topic exists to
stress-test that thesis against the fifty years of attempts that preceded it.

**Canonical framing:** The field's framing is a **cycle**, and naming it honestly is the point: CASE tools
(1980s) → 4GL/RAD platforms (early 1990s) → OMG's MDA (2000s) → low-code (2010s) → LLM spec-driven tools
(2020s). Each generation claimed a higher-level representation would subsume general-purpose code. Each was
defeated by the same structural forces. **Our framing is deliberately the inverse** — code stays
authoritative and models are *derived* — and that inversion is the whole reason the historical failure modes
may not apply. It should be stated that way, explicitly, rather than as a claim to have solved what nobody
else could.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"Spec versions and survey data".)*

## Headline findings

1. **UML 2.5.1 was published December 2017 (formal/17-12-05) and has not been revised in nearly eight
   years.** UML 2.5 (May 2015) was a *simplification*: it merged Infrastructure and Superstructure into one
   document and **removed the compliance chapter and compliance levels**. 2.5.1 was errata. No new major
   version is in development. — *(Verified, [S1][S2])*
2. **UML is used as a sketch, not as an engineering artifact — and there is an empirical study saying so.**
   Petre's ICSE 2013 work across **50 engineers in 50 companies** found five patterns of use, of which the
   most common was **no UML at all**; those who used it preferred informal whiteboard sketching; **only 2 of
   50** used it as a programming language. — *(Verified, [S12], DOI 10.1145/2486788.2486883)*
3. **The MDA toolchain is visibly stale, and the staleness is measurable in its own specs.** OCL is at
   **2.4 (February 2014)** — aligned to UML *2.4.1*, not 2.5.1 — and QVT at **1.3 (June 2016)** predates the
   2.5.1 consolidation. MOF 2.5.1 (Oct 2016), XMI 2.5.1 (Jun 2015), fUML 1.5 (Jun 2021), ALF 1.1 (Jun 2017).
   A standards stack whose transformation language lags its metamodel by two versions is not being invested
   in. — *(Verified for versions [S3]–[S8]; the inference about investment is Inferred)*
4. **SysML v2.0 (September 2025) is the most interesting counter-signal, and it lands on our side.** It
   breaks from both UML and SysML v1.x, introducing the **KerML** kernel, a **normative textual notation**,
   and a standardised Systems Modeling API — and it states that **diagrams are views of the abstract syntax,
   not the source of truth**. That is our thesis, adopted by a standards body, in systems engineering. — *(Verified, [S10])*
5. **Round-trip engineering failed for a structural reason, not a tooling one.** Once a developer edits
   generated code, the model and the code disagree and **neither is authoritative**. The field converged on
   code-first with derived views; tools still offer round-trip and practitioners rarely use it. — *(Inferred from the convergence of practice, [S13][S14])*
6. **Model-as-source-of-truth has genuinely worked — under three conditions.** The successes (OpenAPI,
   Protobuf/gRPC, Terraform, Flyway/Liquibase, XState, Prisma, GraphQL SDL) share the same three properties:
   the model boundary is **narrow**, the semantics are **computable**, and the model is **textual,
   version-controllable and directly compilable or executable**. Nothing that succeeded was a diagram. — *(Verified, [S17][S18])*
7. **Low-code is commercially large and repeats the 4GL failure modes.** Gartner put the market at **$13.8B
   in 2021, +22.6% YoY**, and predicted 41% of employees outside IT would build technology solutions. The
   documented limits are the classic ones: the **"last 10%"** problem, **vendor lock-in**, and **governance
   collapse at scale**. Building is not the same as maintaining. — *(Verified for the market figures [S20]; the failure modes Inferred from pattern convergence)*
8. **LLM generation is genuinely new in exactly one dimension: it tolerates imprecise, under-specified
   models** — which MDA toolchains could not, since they required full formality. The price is
   non-determinism and correctness uncertainty. Thoughtworks' Radar (2025) describes tools where *"the
   specification becomes the maintained artifact, rather than the code"* — which is the MDA claim revived
   under a new mechanism, and should be recognised as such. — *(Verified, [S19])*
9. **The open-source MDE stack is alive but specialist.** EMF/Ecore, Sirius, Xtext and Papyrus remain
   maintained, largely for tool-building, domain-specific tooling and MBSE rather than general application
   development. Xtext in particular — grammar-driven DSL development over EMF — is the piece most relevant
   to anyone building a modelling tool rather than modelling with one. — *(Verified, [S21])*
10. **There is no research literature on LLM + MDE.** ArXiv searches returned nothing relevant. The
    intellectual thread exists only as practitioner tooling (Tessl, Amazon Kiro, GitHub spec-kit), and
    Thoughtworks flags it as emerging and uncertain, cautioning that *"handcrafting detailed rules for AI
    ultimately doesn't scale"* — a direct echo of the MDA critique. **We are not standing on a literature.** — *(Verified for the absence and the quote [S19]; **Flagged** — absence of evidence)*

## Confidence summary

Verified: all OMG spec versions and dates, Petre's study parameters and finding, SysML v2.0's date and its
views-not-source statement, the Gartner market figures, Fowler's and Thomas's critiques as quoted, the
success cases, and the absence of LLM+MDE literature. Inferred: the UML 14-diagram taxonomy (standard
knowledge, not read from the PDF — access limited); the round-trip failure analysis; low-code failure modes;
the diagram-type survival ranking. Flagged: Dobing & Parsons and Störrle sample sizes (not confirmed from
primary access); no post-2020 large-sample UML survey was found.

**Load-bearing Flagged claim:** the absence of post-2020 UML adoption data. Every adoption claim here rests
on a 2013 study, and thirteen years is a long time in this field — though the direction of travel (spec
staleness, no new version) suggests it has not reversed.

## Design implications

- **State the thesis in its defensible form, not its ambitious one.** "The models are the product" is the
  claim that has failed five times. **"The models are *derived views* and the code remains authoritative"**
  is a different and much better-supported claim. The distinction is not rhetorical — it is precisely what
  avoids four of the five historical failure modes (see `open-questions.md`).
- **Never make a diagram editable.** The moment a view can be edited, the model/code divergence problem
  returns in full and neither artifact is authoritative. The seed architecture's non-goal — *"if a view is
  wrong, fix the code or the query, never the picture"* — is the single most important thing in it.
- **Copy the shape of the successes, not the shape of UML.** OpenAPI, Terraform, Prisma and XState all won by
  being **narrow, computable and textual**. Our generated DSL committed to `docs/diagrams/` has exactly those
  properties; our graph does not need to be a general modelling language and should not try to be.
- **Prioritise the diagram types people actually use.** Class, sequence, use-case and activity are heavily
  used; timing, interaction-overview, communication, composite-structure, profile and object diagrams are
  effectively dead. Generating the dead ones is effort spent on views nobody reads.
- **Read SysML v2 before designing the graph schema.** A standards body has just done the "textual notation
  plus API plus diagrams-as-views" design in the adjacent discipline, with a published kernel. Whether or not
  we adopt any of it, having it not-invented-here would be a poor outcome.
- **Treat "the spec becomes the maintained artifact" claims with the scepticism the history earns.** The
  LLM spec-driven tools are making the MDA claim again. Our design deliberately does not.
- **Expect the escape hatch, and design for it.** Every model-first system eventually needs a route back to
  general code. Ours has one by construction — the code *is* the source — which is why this failure mode
  does not apply, and it is worth writing that down so nobody later "improves" the design by removing it.

## How to use this base

This topic is the **adversarial** one: it exists to make the project's thesis defensible rather than to
support it. Personas and the design skills cite these files as evidence (BoK §III.1), and the
`open-questions.md` disconfirming section should be read by anyone writing the architecture. Refresh if a
post-2020 UML adoption study appears, or if the LLM+MDE literature stops being empty.
