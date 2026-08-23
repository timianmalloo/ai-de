---
id: kb-uml-glossary
title: "UML, MDE & 4GL — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, mda, uml, dsl, ubiquitous-language]
links:
  - { to: kb-uml-mde-and-4gl, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for the modelling vocabulary — CIM/PIM/PSM, MOF, XMI, OCL, fUML, Alf,
  4GL, round-trip, language workbench — so the history is discussed in its own terms.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **4GL** | A "fourth-generation language" — higher-level and more domain-specific than a general-purpose 3GL. SQL is the enduring example; PowerBuilder, Oracle Forms, Informix-4GL, VB, Delphi and Access are the RAD generation. The taxonomy fell out of favour as general-purpose languages absorbed the abstractions it measured. |
| **ALF** | Action Language for fUML — the textual action language for the executable UML subset. **v1.1, June 2017.** *(Verified, [S8])* |
| **CASE tools** | Computer-Aided Software Engineering — the 1980s generation of diagram-to-code tooling. Fowler's *"Night of the Living CASE Tools"* names MDA as its return. *(Verified, [S13])* |
| **CIM / PIM / PSM** | MDA's three levels: **Computation Independent Model** (domain/business, no computing concepts) → **Platform Independent Model** (logical, technology-neutral) → **Platform Specific Model** (implementation for a named platform). Linked by QVT transformations. *(Verified as the OMG vision, [S11])* |
| **Ecore / EMF** | The Eclipse Modeling Framework's metamodel and code-generation core — the data layer under Xtext, Sirius and Papyrus. *(Verified, [S21])* |
| **Escape hatch** | The route from a model-first platform into general-purpose code (PowerBuilder's PBNI, OutSystems external components, Power Apps + Azure Functions). **When it is used routinely, the model is no longer the product** — the single most reliable failure signature in this domain. *(Inferred)* |
| **fUML** | Foundational UML — the executable subset with defined semantics. **v1.5, June 2021.** *(Verified, [S7])* |
| **Generative design (parametric)** | Solver-driven exploration of a design space against objectives and constraints (CAD/topology optimisation). The interaction shape transfers; the objective function does not, because software architecture has no evaluable fitness measure. |
| **KerML** | The foundational kernel introduced by SysML v2.0 — the layer beneath the systems-modelling language. *(Verified, [S10])* |
| **Language workbench** | A tool for defining and using domain-specific languages, including their editors. **Xtext** is the live open-source example. *(Verified, [S18][S21])* |
| **"Last 10%"** | The low-code failure mode: the final tenth of requirements cannot be expressed in the visual surface and forces the escape hatch, at disproportionate cost. *(Inferred)* |
| **MDA** | Model Driven Architecture — the OMG's CIM→PIM→PSM vision, built on MOF, XMI, OCL and QVT. *(Verified, [S11])* |
| **MDE / MDD** | Model-Driven Engineering / Development — the broader family of which MDA is the OMG's specific instance. |
| **MOF** | Meta Object Facility — the metamodel of the metamodels; UML is defined in terms of it. **v2.5.1, October 2016.** *(Verified, [S3])* |
| **OCL** | Object Constraint Language — declarative constraints over UML/MOF models. **v2.4, February 2014, aligned to UML 2.4.1** rather than to 2.5.1. *(Verified, [S4])* |
| **QVT** | Query/View/Transformation — the OMG model-transformation standard that turns a PIM into a PSM. **v1.3, June 2016.** *(Verified, [S5])* |
| **Round-trip engineering** | Keeping a model and its generated code synchronised in both directions. **Failed structurally**: once generated code is edited, neither artifact is authoritative. *(Inferred)* |
| **Source generator** | A compile-time, deterministic code-generation mechanism (Roslyn source generators, T4). The trustworthy end of generative software design — and, notably, invisible to design-time builds. |
| **Spec-driven development** | The 2020s revival of the MDA claim under LLMs, where *"the specification becomes the maintained artifact, rather than the code"*. *(Verified, quoted, [S19])* |
| **SysML v2** | The September 2025 systems-modelling standard with a textual normative notation, an API, and **diagrams defined as views of the abstract syntax rather than the source of truth**. *(Verified, [S10])* |
| **UML as Sketch / Blueprint / Programming Language** | Fowler's three modes: informal communication (the common case), precise engineering reference, and executable specification (the MDA vision, rare). *(Verified, [S14][S15])* |
| **XMI** | XML Metadata Interchange — the model-interchange serialisation. **v2.5.1, June 2015.** Criticised for providing no *written* syntax for models. *(Verified, [S6])* |
| **Xtext** | Grammar-driven DSL development over EMF — the most directly relevant Eclipse component for anyone building a modelling tool rather than modelling with one. *(Verified, [S21])* |
