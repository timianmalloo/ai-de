---
id: kb-domain-comparables
title: "Domain Modelling & ERM — comparable methods and tools"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, contextmapper, eventstorming, tbls, archunit]
links:
  - { to: kb-domain-modeling-and-erm, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Modelling methods and tools compared by the question that matters for us — is what this
  produces extractable from artifacts, or does it require human judgement?
---

# Comparable solutions & problem framings

**The column that matters is "extractable?"** — because it separates what a tool can do from what a
workshop must do.

## Methods

| Method | What it models | Extractable from artifacts? | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **Tactical DDD** (Evans) | entities, value objects, aggregates, repositories, domain events | **Partially** — stereotypes yes, correctness no | Gives code a vocabulary; mechanically detectable via attributes and ownership | Cargo-culted without the strategic work; an anemic model has identical stereotypes | Verified [S1][S9] |
| **Strategic DDD** (bounded contexts, context maps) | model boundaries and team relationships | **No** — organisational knowledge, absent from source | The half the field considers valuable | Requires people; cannot be inferred | Verified [S6] |
| **Vernon's aggregate rules** | aggregate boundary discipline | **Rule 3 is checkable** (direct object reference vs identity) | A mechanical lint for a real design error | Rules 1, 2 and 4 need intent | Verified [S3][S4] |
| **EventStorming** (Brandolini) | events, commands, policies, boundaries — the *why* | **No** | Surfaces invariants and boundaries no code contains | Needs a room and domain experts | Reference (colour grammar **Flagged**) |
| **Domain Storytelling / Event Modeling** | narrative flows and their actors | **No** | Shared understanding | Same | Reference |
| **Wardley mapping** | strategic positioning of capabilities | **No** | Complements context mapping | Same | Reference |
| **ERM — conceptual** | entities, relationships, business rules | **No** | The model people reason about | Nothing in DDL carries intent | Reference |
| **ERM — logical / physical** | attributes, keys, tables, indexes | **Yes** | Complete and mechanical | Structure without meaning | Inferred |

## Tools

| Tool | What it does | Input | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **ContextMapper** | DDD context maps and aggregates as a DSL (CML) with generators | **manually authored CML**; reverse-engineering from **Spring Boot annotations** and **Docker Compose** | The only tool modelling strategic DDD as code; compact relationship syntax `[D,ACL]<-[U,OHS,PL]`; generates diagrams and service contracts | Java/Eclipse ecosystem; **no .NET stereotype reverse engineering** — precisely the gap we would fill | Verified [S10]–[S15] |
| **EF Core design-time `IModel`** | the authoritative mapped model | source + `Microsoft.EntityFrameworkCore.Design` | Ownership, shadow properties, value converters, the true table set per aggregate — **without a database** | **Executes `OnModelCreating`** — not purely static | Verified [S16] |
| **NetArchTest / ArchUnitNET** | asserts architectural rules over compiled code | assemblies | Confirms that marker-interface and base-class conventions are real and checkable | Rules must be written by hand; static only | Verified [S17] |
| **ScriptDom / DDL parsers** | the physical model | `.sql` (offline) | Tables, columns, FKs, CHECKs, indexes, computed columns, **temporal syntax** | No implicit relationships; no aggregate boundaries; no intent | Verified [S18]; boundary Inferred |
| **tbls** | per-table linked Markdown documentation | **live DB** | Explicitly avoids one monolithic diagram — generates navigable per-table docs | Not offline | Verified |
| **SchemaSpy / SchemaCrawler** | schema + ER diagrams as an interactive site | **live DB (JDBC)** | Same instinct — interactivity instead of one huge diagram | Not offline | Inferred |
| **DbSchema** | visual schema design and documentation | live DB or reverse-engineered | Visual editing | Commercial; design-time model, not domain model | Inferred |

## The two tools that shape our design most

**ContextMapper** — for what it does *and* for what it does not. It proves that modelling context maps as
version-controlled text with generators is workable, and its reverse-engineering scope (Spring Boot
annotations, Docker Compose) shows that even the tool closest to this problem does not try to infer bounded
contexts from source semantics. That is corroboration, not a gap in their work.

**tbls and SchemaSpy** — both independently reached the same conclusion about large schemas: **do not render
one diagram**. tbls generates per-table Markdown linked by navigation; SchemaSpy generates an interactive
site. Two tools arriving at the same answer is the strongest available evidence that whole-schema ER
rendering is the wrong output.

## Adjacent ideas worth borrowing

- **ContextMapper's CML relationship syntax** — `[D,ACL]<-[U,OHS,PL]` encodes downstream/upstream roles and
  the integration patterns in one line. Adopt it rather than inventing an edge notation.
- **NetArchTest's stance** — architectural conventions are worth *asserting* rather than merely detecting.
  Vernon's rule 3 is exactly this shape: "no aggregate holds a direct reference to another aggregate root"
  is a rule, not an observation.
- **tbls's per-table documents** — navigation over rendering, for anything above a couple of dozen tables.
- **EF Core's ownership metadata as the aggregate oracle** — it is the only place in a .NET codebase where
  "these tables are one thing" is stated by the developer rather than guessed by a tool.
