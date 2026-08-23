---
id: kb-uml-references
title: "UML, MDE & 4GL — references, spec versions and survey data"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, omg-specs, versions, surveys]
links:
  - { to: kb-uml-mde-and-4gl, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  OMG specification versions and dates read from omg.org, the empirical adoption studies with
  their sample sizes, the market figures, and the critiques with their exact quotations.
---

# Reference information

## OMG specifications — versions and dates

| Spec | Version | Date | Status | File ID |
|---|---|---|---|---|
| **UML** | **2.5.1** | **December 2017** | Formal | `formal/17-12-05` |
| UML (previous) | 2.5 | May 2015 | Formal | `formal/15-03-01` |
| **MOF** — Meta Object Facility | 2.5.1 | October 2016 | Formal | — |
| **XMI** — XML Metadata Interchange | 2.5.1 | June 2015 | Formal | — |
| **OCL** — Object Constraint Language | **2.4** | **February 2014** | Formal | — |
| **QVT** — Query/View/Transformation | **1.3** | June 2016 | Formal | — |
| **fUML** — Foundational UML | 1.5 | June 2021 | Formal | — |
| **ALF** — Action Language for fUML | 1.1 | June 2017 | Formal | — |
| **SysML** v1 (last) | 1.7 | — | Formal | — |
| **SysML v2.0** | **2.0** | **September 2025** | Formal | — |

*(Verified from omg.org spec pages, [S1]–[S10])*

**The staleness signal:** OCL 2.4 is aligned to **UML 2.4.1**, not 2.5.1; QVT 1.3 predates the 2.5.1
consolidation. A metamodel two versions ahead of its own constraint and transformation languages is the
clearest available evidence of reduced investment. *(Versions Verified; the inference Inferred)*

## SysML v2.0 — what changed

A break from both UML and SysML v1.x: a new **KerML** foundational kernel, a **normative textual notation**,
and a standardised **Systems Modeling API**. **Diagrams are explicitly views of the abstract syntax, not the
source of truth.** *(Verified, [S10])*

## MDA architecture

- **CIM** — Computation Independent Model: the domain/business view, containing no computing concepts.
- **PIM** — Platform Independent Model: logical system structure, not bound to a technology platform.
- **PSM** — Platform Specific Model: the implementation view for a specific platform (J2EE, .NET, CORBA…).
- Mappings between levels are expressed as **QVT** transformations.

*(Verified as the OMG vision [S11]; the level definitions Inferred from the MDA page)*

## Empirical adoption data

| Study | Year | Sample | Finding | Confidence |
|---|---|---|---|---|
| **Petre, "UML in Practice", ICSE** (DOI 10.1145/2486788.2486883) | **2013** | **50 engineers across 50 companies** | Five patterns of use; **most common was no UML at all**; informal sketching dominant; **only 2 of 50** used UML as a programming language | **Verified** [S12] |
| Dobing & Parsons | 2006 | developer survey | Class and use-case diagrams most used; collaboration, timing, composite-structure rarely | **Flagged** — sample size not confirmed |
| Störrle, systematic review | 2017 | literature review | Class and sequence diagrams >80% of observed UML use | **Flagged** — not confirmed from primary access |

**No post-2020 large-sample UML adoption survey was found.** Every adoption claim in this topic therefore
rests on 2013 data. *(Flagged)*

## Market data

| Figure | Value | Source |
|---|---|---|
| Low-code development technologies market | **$13.8B (2021)** | Gartner press release, 2021-02-16 |
| Year-over-year growth | **22.6%** | same |
| Predicted share of employees outside IT building technology solutions | **41%** | same |

*(Verified, [S20])*

## The critiques, quoted

**Fowler — "Night of the Living CASE Tools"** *(martinfowler.com, ~2004)*: UML at MDA's required formality is
not demonstrably more productive than a modern programming language; whether UML is computationally complete
*in a usable way* is questionable; and on notation — *"anyone who has compared flow charts to pseudo code
can form their own conclusions."* *(Verified, [S13])*

**Thomas — "UML — Unified or Universal Modeling Language?"** *(Journal of Object Technology, January 2003)*:
committee language design produced something *"complex, bloated"*; XMI provided no written syntax; UML/MOF/OCL
*"going meta"* **deferred rather than solved** the semantic problems; visual languages are *"inherently
domain specific"* and become *"visual spaghetti"* beyond their domain; and the xtUML/MDA promise to eliminate
programmers was an overreach echoing the CASE-tool era. *(Verified, quoted)*

**Fowler's three modes of UML use** — *UML as Sketch*, *UML as Blueprint*, *UML as Programming Language*.
*(Verified, [S14][S15])*

**Thoughtworks Technology Radar (2025), on spec-driven development** — *"the specification becomes the
maintained artifact, rather than the code"*, with the caution that *"handcrafting detailed rules for AI
ultimately doesn't scale."* *(Verified, [S19])*

**Brooks, "No Silver Bullet" (1987)** — much of software complexity is **essential**, not accidental, and no
notation removes it. The foundation of every critique above. *(Referenced; not fetched)*

## Successful model-as-source-of-truth cases

OpenAPI · Protobuf / gRPC · GraphQL SDL · Terraform (and ARM/Bicep) · Flyway / Liquibase / Prisma ·
SCXML / XState. Shared properties: **narrow boundary, computable semantics, textual and version-controllable,
directly executable or compilable, integrated into normal developer workflow.** *(Verified, [S17][S18])*

## The Eclipse MDE stack

**EMF/Ecore** (metamodel + code generation) · **Xtext** (grammar-driven DSLs over EMF) · **Sirius**
(graphical workbench generation) · **Papyrus** (UML/MBSE) · **GMF** (legacy). All maintained; all specialist.
*(Verified, [S21])*
