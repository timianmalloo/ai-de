---
id: kb-uml-sources
title: "UML, MDE & 4GL — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations, omg]
links:
  - { to: kb-uml-mde-and-4gl, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The access-dated source list behind the UML/MDE/4GL knowledge base, keyed [S1]..[S21],
  distinguishing OMG primary specs from commentary and noting where evidence is absent.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | OMG UML specification page | primary (spec) | https://www.omg.org/spec/UML/ | UML 2.5.1 version, date, status |
| S2 | OMG UML 2.5 "About" page | primary | https://www.omg.org/spec/UML/2.5/About-UML | UML 2.5 date (May 2015) |
| S3 | OMG MOF specification page | primary | https://www.omg.org/spec/MOF/ | MOF 2.5.1, October 2016 |
| S4 | OMG OCL specification page | primary | https://www.omg.org/spec/OCL/ | **OCL 2.4, February 2014** — the staleness signal |
| S5 | OMG QVT specification page | primary | https://www.omg.org/spec/QVT/ | QVT 1.3, June 2016 |
| S6 | OMG XMI specification page | primary | https://www.omg.org/spec/XMI/ | XMI 2.5.1, June 2015 |
| S7 | OMG fUML specification page | primary | https://www.omg.org/spec/FUML/ | fUML 1.5, June 2021 |
| S8 | OMG ALF specification page | primary | https://www.omg.org/spec/ALF/ | ALF 1.1, June 2017 |
| S9 | OMG SysML v1.7 page | primary | https://www.omg.org/spec/SysML/1.7/ | Last of the v1 line |
| S10 | OMG SysML v2.0 page + sysml.org FAQ | primary | https://www.omg.org/spec/SysML/ · https://sysml.org/sysml-v2/faq/ | **September 2025**, KerML, textual notation, API, **diagrams-as-views** |
| S11 | OMG MDA homepage | primary | https://www.omg.org/mda/ | The CIM/PIM/PSM vision as still stated |
| S12 | Petre, "UML in Practice", ICSE 2013 | academic (abstract via Scholar) | DOI 10.1145/2486788.2486883 | **50 engineers / 50 companies; five patterns; 2 of 50 as a programming language** |
| S13 | Fowler — *ModelDrivenArchitecture* | commentary | https://martinfowler.com/bliki/ModelDrivenArchitecture.html | The MDA critique; *"Night of the Living CASE Tools"*; the flow-chart-vs-pseudocode quote |
| S14 | Fowler — *UmlMode* | commentary | https://martinfowler.com/bliki/UmlMode.html | Sketch / Blueprint / Programming Language taxonomy |
| S15 | Fowler — *UmlAsSketch* | commentary | https://martinfowler.com/bliki/UmlAsSketch.html | The informal-use pattern |
| S16 | Fowler — *ModelDrivenSoftwareDevelopment* | commentary | https://martinfowler.com/bliki/ModelDrivenSoftwareDevelopment.html | MDSD scepticism |
| S17 | Fowler — *DomainSpecificLanguage* | commentary | https://martinfowler.com/bliki/DomainSpecificLanguage.html | Internal/external DSL taxonomy; why narrow DSLs succeed |
| S18 | Fowler — *Language Workbenches* article | article | https://martinfowler.com/articles/languageWorkbench.html | Language-oriented programming; Xtext's category |
| S19 | Thoughtworks Technology Radar — spec-driven development | primary (vendor research) | https://www.thoughtworks.com/radar/techniques/spec-driven-development | *"the specification becomes the maintained artifact"*; *"handcrafting detailed rules for AI ultimately doesn't scale"* |
| S20 | Gartner press release, 2021-02-16 | primary (analyst) | gartner.com newsroom | **$13.8B market, +22.6% YoY, 41% prediction** |
| S21 | Eclipse EMF / Papyrus / Xtext project pages | primary (project docs) | https://eclipse.dev/emf · /papyrus · /Xtext | Stack liveness and scope |

## Additional works referenced but not fetched

| Work | Use |
|---|---|
| **Brooks, "No Silver Bullet" (1987)** | The essential-vs-accidental complexity argument underlying every critique here |
| **Thomas, "UML — Unified or Universal Modeling Language?"** (JOT, Jan 2003) | Quoted directly for *"complex, bloated"*, *"going meta"*, *"visual spaghetti"* — sourced via [S13]'s context |
| **Cook (2004)** | Joins the MDA critique line |
| **Dobing & Parsons (2006)**, **Störrle (2017)** | Adoption data — **Flagged**, sample sizes unconfirmed |
| **OpenAPI, gRPC, XState, Prisma, Terraform, Flyway/Liquibase docs** | The success cases; the shared properties were verified against [S17][S18] and the projects' own positioning |

## Source-quality notes

- **Every OMG version number and date was read from omg.org**, not recalled. They are the load-bearing facts
  in this topic because the *pattern* of the dates is the evidence.
- **Petre 2013 is the only Verified empirical adoption study**; the other two are Flagged. All adoption
  claims therefore rest on a single 13-year-old sample, and that limitation is stated wherever they are used.
- **Fowler's and Thomas's critiques are commentary, not research** — but they are the canonical, most-cited
  statements of the MDA critique and they are quoted rather than paraphrased.
- The **absence of LLM+MDE literature** is recorded as an absence after explicit search, which is weaker than
  a positive citation and is marked **Flagged** wherever it is relied on.
- The **UML diagram taxonomy** is Inferred: the specification PDF was not accessible during research.
