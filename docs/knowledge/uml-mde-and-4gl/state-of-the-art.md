---
id: kb-uml-sota
title: "UML, MDE & 4GL — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [uml, mda, sysml, emf, xtext, low-code, dsl]
links:
  - { to: kb-uml-mde-and-4gl, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Where UML, the MDA stack, SysML v2, the Eclipse MDE tooling, generative design and the
  low-code successors of the 4GL actually stand today — with the spec dates that show the
  investment curve.
---

# State of the art — UML, MDE, generative design and 4GL surfaces

## UML today

**Specification:** UML **2.5.1**, formal, **December 2017**, OMG file ID `formal/17-12-05`, superseding UML
2.5 (May 2015, `formal/15-03-01`). *(Verified, [S1][S2])*

**What 2.5 changed:** a restructuring for simplification — the former *Infrastructure* and *Superstructure*
documents were collapsed into one specification, the **compliance chapter and compliance levels were
removed**, and the normative content was reorganised. No new diagram types. 2.5.1 was errata.
*(Verified for the merge and compliance removal; the detail Inferred from OMG version history)*

**The 14 diagram types** — 7 structural (class, object, component, composite structure, package, deployment,
profile) and 7 behavioural (use case, activity, state machine, sequence, communication, timing, interaction
overview). *(Inferred — standard knowledge, consistent with the OMG page; the PDF was not accessible)*

**Adoption, empirically:**

| Study | Year | Sample | Finding |
|---|---|---|---|
| **Petre, "UML in Practice" (ICSE)** | 2013 | **50 engineers, 50 companies** | Five patterns of use; **the most common was no UML at all**; informal sketching dominant among users; **only 2 of 50** used it as a programming language |
| Dobing & Parsons | 2006 | developer survey | Class and use-case diagrams most used; collaboration, timing and composite-structure rarely |
| Störrle (systematic review) | 2017 | literature review | Class and sequence diagrams account for **>80%** of actual UML use in studies |

*(Petre Verified [S12]; the other two **Flagged** — sample sizes not confirmed from primary access)*

**Which types survive:** heavily used — class, sequence, use case, activity. Occasionally — state machine,
component, deployment. Rarely or never — timing, interaction overview, communication, composite structure,
profile, object. As *precise engineering artifacts*, virtually none: most UML produced is whiteboard-level.
*(Inferred from Petre and Fowler's framing; **Flagged** — no post-2020 large-sample survey found)*

**Fowler's three modes**, which remain the clearest taxonomy: **UML as Sketch** (informal communication, the
common case), **UML as Blueprint** (precise forward/reverse engineering reference), **UML as Programming
Language** (executable specification — the MDA vision, rare and niche). *(Verified, [S14][S15])*

## MDA / MDE / MDD

**The specification stack, with its dates — the investment curve is visible in the table:**

| Spec | Version | Date | Status |
|---|---|---|---|
| MOF (Meta Object Facility) | 2.5.1 | October 2016 | Formal |
| XMI (XML Metadata Interchange) | 2.5.1 | June 2015 | Formal |
| **OCL** (Object Constraint Language) | **2.4** | **February 2014** | Formal |
| **QVT** (Query/View/Transformation) | **1.3** | June 2016 | Formal |
| fUML (Foundational UML subset) | 1.5 | June 2021 | Formal |
| ALF (Action Language for fUML) | 1.1 | June 2017 | Formal |

**OCL 2.4 is aligned to UML 2.4.1, not 2.5.1**, and QVT 1.3 predates the 2.5.1 consolidation. A stack whose
constraint language and transformation language both lag its metamodel is not under active investment.
*(Versions Verified [S3]–[S8]; the conclusion Inferred)*

**The MDA architecture:** **CIM** (computation-independent — the domain/business view, no computing
concepts) → **PIM** (platform-independent — logical structure, technology-neutral) → **PSM**
(platform-specific — the implementation view for J2EE, .NET, CORBA…), with **QVT** transformations between
levels. *(Verified as the OMG vision [S11]; the level definitions Inferred from the MDA page)*

**The published critiques**, which are specific rather than vague:

- **Fowler, "Night of the Living CASE Tools"** — UML at the formality MDA requires is not demonstrably more
  productive than a modern programming language; whether UML is computationally complete *in a usable way*
  is questionable; graphical notations are not universally superior to textual ones — *"anyone who has
  compared flow charts to pseudo code can form their own conclusions."* *(Verified, [S13])*
- **Thomas, "UML — Unified or Universal Modeling Language?"** (JOT, January 2003) — committee language design
  produced something *"complex, bloated"*; XMI provided no written syntax; UML/MOF/OCL *"going meta"*
  **deferred rather than solved** the semantic problems; visual languages are *"inherently domain specific"*
  and degrade into *"visual spaghetti"* when pushed past their domain; the xtUML/MDA claim to eliminate
  programmers echoed the earlier CASE-tool promises. *(Verified, [S13]-adjacent, quoted)*
- **Cook (2004)** joins the same line of argument. *(Referenced)*

**Executable UML** — **fUML 1.5** (June 2021) defines the executable subset and **ALF 1.1** (June 2017) its
action language. Both are current-ish by the standard's own timeline and neither is in general use.
*(Verified, [S7][S8])*

**SysML** — v1.7 is the last of the v1 line; **v2.0 (September 2025)** is a clean break: a new **KerML**
foundational kernel, a **normative textual notation**, and a standardised **Systems Modeling API**, with
diagrams defined explicitly as **views of the abstract syntax rather than the source of truth**. This is the
single strongest piece of evidence that the derived-views position is now the mainstream engineering
position rather than a contrarian one. *(Verified, [S9][S10])*

**The Eclipse stack** — **EMF/Ecore** (the metamodel and code-generation core), **Sirius** (graphical
workbench generation), **Xtext** (grammar-driven DSL development over EMF), **Papyrus** (UML/MBSE tooling),
**GMF** (older graphical framework). All maintained; all specialist — used to *build tools and DSLs*, not to
build general applications. *(Verified, [S21])*

## Generative design — two distinct senses, kept apart

**(a) Generative/parametric design in engineering and CAD** — topology optimisation and constraint-driven
generation, where a solver explores a space of geometries against declared objectives and constraints
(Autodesk's work is the reference). What transfers conceptually is the *shape of the interaction*: the human
specifies **goals and constraints**, the machine explores, the human selects. What does not transfer is the
existence of an objective function — software architecture has no evaluable fitness measure. *(Reference)*

**(b) Generative software design** — code generation from models, template and scaffolding generation,
**Roslyn source generators** and **T4** at the compile-time end, and now **LLM-based generation**.
*(Verified for the mechanisms)*

**What is genuinely new with LLMs:** they **tolerate imprecise, under-specified models**. Every prior
generation mechanism — QVT, T4, source generators — required a fully formal input, which is precisely why
the modelling burden ate the productivity gain. LLMs remove that requirement and substitute
non-determinism and correctness uncertainty in its place. That is a real change in kind, and it is also the
mechanism by which the MDA claim is being made again: Thoughtworks' Radar (2025) describes spec-driven tools
where *"the specification becomes the maintained artifact, rather than the code."* *(Verified, [S19])*

## 4GLs and their successors

A **fourth-generation language** was defined by being higher-level and more domain-specific than a
general-purpose 3GL — SQL is the enduring example, alongside FOCUS, Informix-4GL, PowerBuilder, Oracle Forms,
Visual Basic, Delphi and Access. The generational taxonomy fell out of favour because it stopped
discriminating: modern general-purpose languages absorbed the abstractions the taxonomy was measuring.
*(Reference)*

What killed the classic RAD 4GL surfaces was the combination of **proprietary runtimes**, **the web** (which
their form-centric models did not fit), and **the escape-hatch collapse** — every one of them needed a route
into general code (PowerBuilder's PBNI is the canonical example), and once that route was used routinely,
the model was no longer the product. *(Inferred)*

**The modern successors** — Microsoft Power Apps / Power Platform, OutSystems, Mendix, Retool, Appsmith,
Budibase, plus internal-tool builders, spreadsheet-as-programming and Airtable/Notion-style structured
surfaces. The market is real: Gartner reported **$13.8B in 2021, growing 22.6% year over year**, and
predicted 41% of employees outside IT would build technology solutions. The documented limits are the same
three every time: the **"last 10%"**, **vendor lock-in**, and **governance collapse at scale**.
*(Market figures Verified [S20]; the limits Inferred from pattern convergence)*

## Where model-as-source-of-truth actually worked

| Domain | The model | Why it stuck |
|---|---|---|
| API contracts | **OpenAPI**, **Protobuf/gRPC**, **GraphQL SDL** | narrow boundary, computable semantics, textual, compiles to real artifacts |
| Infrastructure | **Terraform**, ARM/Bicep | the model *is* the deployment mechanism — no divergence possible |
| Database schema | **Flyway**, **Liquibase**, **Prisma** | migrations are executable; the model applies itself |
| State machines | **SCXML**, **XState** | formal semantics, executable, small surface |
| Narrow DSLs | domain-specific codegen | the domain constrains the language enough to keep it simple |

**The common properties: narrow scope, computable semantics, textual and version-controllable, directly
executable or compilable, and integrated into the normal developer workflow.** Not one of the successes is a
diagram. *(Verified, [S17][S18])*

## The frontier

- **SysML v2** is the live experiment in "textual model, API, diagrams-as-views" at standards scale.
- **LLM spec-driven development** (Tessl, Amazon Kiro, GitHub spec-kit) is the live experiment in the other
  direction — the specification as the maintained artifact.
- **There is no research literature joining LLMs and MDE.** The thread is entirely practitioner tooling, and
  Thoughtworks' own caution — *"handcrafting detailed rules for AI ultimately doesn't scale"* — is the same
  objection that defeated MDA, arriving early. *(Verified for the absence and the quote, [S19])*
