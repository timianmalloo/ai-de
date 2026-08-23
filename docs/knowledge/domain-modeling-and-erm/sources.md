---
id: kb-domain-sources
title: "Domain Modelling & ERM — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-domain-modeling-and-erm, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The access-dated source list behind the domain-modelling and ERM knowledge base, keyed
  [S1]..[S19], separating established books from fetched documentation.
---

# Sources

Web sources accessed **2026-08-23**; books are cited by edition and ISBN. Citation keys `[Sn]` are used
throughout this topic.

| # | Title | Type | URL / identifier | Used for |
|---|---|---|---|---|
| S1 | Evans, *Domain-Driven Design* (2003) | book | ISBN 0-321-12521-5 | The canon: building blocks, context-map patterns, ubiquitous language |
| S2 | Evans, *DDD Reference* (2015) | primary (PDF) | domainlanguage.com/…/DDD_Reference_2015-03.pdf | Consolidated pattern definitions — **fetched but not renderable (Flagged)** |
| S3 | Vernon, *Implementing Domain-Driven Design* (2013) | book | ISBN 978-0-321-83457-7 | The four aggregate rules (chapter 10) |
| S4 | Vernon, *Effective Aggregate Design* (2011) | primary (article) | https://dddcommunity.org/library/vernon_2011/ | The four rules and the **"current consensus view"** statement (quoted) |
| S5 | Chen, *The Entity-Relationship Model* (1976) | academic | DOI 10.1145/320434.320440 | ERM origin and Chen notation |
| S6 | Fowler — *DomainDrivenDesign* | commentary | https://martinfowler.com/bliki/DomainDrivenDesign.html | Strategic DDD as *"a particularly important part"*; the Vernon recommendation |
| S7 | Fowler — *BoundedContext* | commentary | https://martinfowler.com/bliki/BoundedContext.html | Bounded-context definition |
| S8 | Fowler — *DDD_Aggregate* | commentary | https://martinfowler.com/bliki/DDD_Aggregate.html | Aggregate definition and the transaction-boundary rule |
| S9 | Fowler — *AnemicDomainModel* | commentary | https://martinfowler.com/bliki/AnemicDomainModel.html | **The confounder for structural extraction** |
| S10 | ContextMapper — home | primary (docs) | https://contextmapper.org/docs/home/ | Architecture and positioning |
| S11 | ContextMapper — language reference | primary | https://contextmapper.org/docs/language-reference/ | Tactical DDD patterns in CML |
| S12 | ContextMapper — reverse engineering | primary | https://contextmapper.org/docs/reverse-engineering/ | **Spring Boot annotations + Docker Compose scope — the gap** |
| S13 | ContextMapper — aggregate | primary | https://contextmapper.org/docs/aggregate/ | CML aggregate syntax and lifecycle |
| S14 | ContextMapper — bounded context | primary | https://contextmapper.org/docs/bounded-context/ | Bounded-context types in CML |
| S15 | ContextMapper — context map | primary | https://contextmapper.org/docs/context-map/ | Context-map patterns and the `[D,ACL]<-[U,OHS,PL]` syntax |
| S16 | EF Core — owned entities and modelling docs | primary (vendor docs) | https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities · /modeling/ | `[Owned]`, `IModel` API, ownership FKs, shadow properties, aggregate-to-table mapping |
| S17 | NetArchTest | primary (repo) | https://github.com/BenMorris/NetArchTest | Confirms marker-interface/base-class conventions are checkable |
| S18 | SQL Server temporal tables — overview | primary (vendor docs) | https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal/ | `PERIOD FOR SYSTEM_TIME`, `SYSTEM_VERSIONING`, support from SQL Server 2016 |
| S19 | NIST FIPS PUB 184 (IDEF1X) | primary (attempted) | nist.gov | **Fetch redirected to the ITL home — status unconfirmed (Flagged)** |

## Additional works referenced but not fetched

| Work | Use |
|---|---|
| **Kimball & Ross**, *The Data Warehouse Toolkit*, 3rd ed. (2013) | The SCD type taxonomy |
| **Codd (1970, 1971)**, **Boyce-Codd (1974)** | The normal forms |
| **Inmon** — Corporate Information Factory | The counterpart to Kimball in the warehouse debate |
| **Brandolini** — EventStorming | The three levels; **the colour grammar was not fetched (Flagged)** |
| **Domain Storytelling, Event Modeling, Wardley mapping** | Collaborative and strategic complements |
| **tbls, SchemaSpy, SchemaCrawler, DbSchema** | The large-schema rendering argument (tbls Verified in the extraction base; the others Inferred) |

## Source-quality notes

- **Vernon's four rules, the context-map list, the SCD types, the normal forms and the Crow's-Foot semantics
  are stated verbatim rather than paraphrased**, because each is a constraint whose precision is the point.
- The **EF Core `IModel` API names** were read from Microsoft's documentation, not recalled — they are the
  load-bearing mechanism for the domain↔data bridge.
- **The DDL extractable/not-extractable boundary is Inferred**: it reflects consensus across multiple sources
  rather than a single citation, and it is the claim in this base most worth testing against a real schema.
- Two items are **Flagged for failed or unusable fetches**: IDEF1X's status (NIST redirect) and Evans's 2015
  *DDD Reference* (unrenderable PDF). Neither gates a decision.
