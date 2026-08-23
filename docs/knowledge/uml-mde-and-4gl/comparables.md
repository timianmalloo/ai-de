---
id: kb-uml-comparables
title: "UML, MDE & 4GL — comparable approaches across five decades"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, case-tools, mda, low-code, dsl, spec-driven]
links:
  - { to: kb-uml-mde-and-4gl, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Every generation of the models-are-the-product idea, what its model-to-artifact mechanism
  was, where it succeeded, and why it failed — plus the narrow cases that worked and what they
  have in common.
---

# Comparable solutions & problem framings

## The cycle — five generations of the same claim

| Approach | Era | Model → artifact mechanism | Where it succeeded | Why it failed / its limits | Confidence |
|---|---|---|---|---|---|
| **CASE tools** | 1980s | diagram → skeleton code | large regulated projects | Escape hatch to general code; model/code divergence; tool cost. Fowler's *"Night of the Living CASE Tools"* names the recurrence | Inferred; the epithet Verified [S13] |
| **4GL / RAD** (PowerBuilder, Oracle Forms, VB, Delphi, Access, Informix-4GL) | early 1990s | visual form designer + proprietary runtime | forms-over-data business apps, very effectively | Proprietary runtimes; the web didn't fit the form model; **escape hatch** (e.g. PowerBuilder's PBNI) used routinely, at which point the model was no longer the product | Inferred |
| **OMG MDA** (CIM→PIM→PSM via MOF/XMI/QVT) | 2000s | QVT transformations between model levels | MBSE, some embedded/defence | Abstraction not demonstrably higher than a modern language; toolchain complexity without matching gain; **spec stack now stale** (OCL 2014, QVT 2016) | Verified for versions [S3]–[S8]; critique Verified [S13] |
| **Executable UML** (xtUML, fUML 1.5, ALF 1.1) | 2000s–2010s | executable model subset + action language | narrow embedded domains | Required full formality; the modelling burden ate the gain; never general | Verified for spec versions [S7][S8] |
| **Low-code / no-code** (Power Apps, OutSystems, Mendix, Retool, Appsmith, Budibase) | 2010s– | visual composition + hosted runtime | internal tools, workflow, forms; **$13.8B market, +22.6% YoY (2021)** | The **"last 10%"**, **vendor lock-in**, **governance collapse at scale** — the 4GL failure modes with new branding | Market Verified [S20]; limits Inferred |
| **LLM spec-driven** (Tessl, Amazon Kiro, GitHub spec-kit) | 2020s– | natural-language spec → generated code | too early to say | Making the MDA claim again — *"the specification becomes the maintained artifact, rather than the code"*. Thoughtworks cautions *"handcrafting detailed rules for AI ultimately doesn't scale"* | Verified for the quotes [S19]; outcome **unknown** |
| **← code-derived views** (this project) | — | **extract from code → derive views; code stays authoritative** | — | Untested at this richness; nearest successful analogues are living-documentation and code-intelligence tools | Inferred |

## What actually worked — and the three properties they share

| Success | The model | Narrow? | Computable? | Textual & executable? |
|---|---|---|---|---|
| **OpenAPI** | the API contract | ✅ | ✅ | ✅ generates clients/servers |
| **Protobuf / gRPC** | the message + service IDL | ✅ | ✅ | ✅ compiles |
| **GraphQL SDL** | the schema | ✅ | ✅ | ✅ executes |
| **Terraform / Bicep** | the infrastructure | ✅ | ✅ | ✅ **is** the deployment |
| **Flyway / Liquibase / Prisma** | the schema and its migrations | ✅ | ✅ | ✅ applies itself |
| **SCXML / XState** | the state machine | ✅ | ✅ (formal semantics) | ✅ executes |

**Every success is narrow, computable, textual, version-controllable and directly executable — and not one
of them is a diagram.** *(Verified, [S17][S18])*

## Modelling tooling, for reference

| Tool | What it is | Status |
|---|---|---|
| **EMF / Ecore** | the metamodel and code-generation core of Eclipse MDE | maintained, specialist |
| **Xtext** | grammar-driven DSL development over EMF | maintained — the most relevant piece for anyone *building* a modelling tool |
| **Sirius** | generates graphical modelling workbenches | maintained, specialist |
| **Papyrus** | Eclipse UML/MBSE tool | maintained for MBSE |
| **GMF** | older graphical modelling framework | legacy |
| **SysML v2 / KerML** | textual notation + API + diagrams-as-views | **September 2025**, the live experiment |

*(Verified, [S10][S21])*

## Generative design — the two senses, and what transfers

| Sense | Mechanism | Transfers to us? |
|---|---|---|
| **Parametric / topology optimisation** (CAD, Autodesk) | solver explores a geometry space against objectives and constraints | The *interaction shape* does — human states goals and constraints, machine explores, human selects. The **objective function does not**: software architecture has no evaluable fitness measure |
| **Template / scaffolding generation** (T4, Roslyn source generators) | formal input → deterministic output | Directly — and note it is compile-time, deterministic, and therefore trustworthy in a way LLM generation is not |
| **LLM generation** | under-specified natural language → code | The genuinely new capability is **tolerating imprecision**; the cost is non-determinism |

## Adjacent framings worth borrowing

- **SysML v2's stance** — diagrams are views of an abstract syntax with a textual normative notation and an
  API. This is our architecture, standardised, in an adjacent discipline. Read it before finalising the
  graph schema.
- **Fowler's three modes** — sketch / blueprint / programming language. Being explicit about which mode a
  generated view serves prevents building precision nobody needs.
- **The narrow-computable-textual test** — apply it to any part of our own design that starts claiming to be
  a model. If it fails all three, it is documentation, and calling it a model will not make it durable.
