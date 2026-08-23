---
id: kb-domain-modeling-and-erm
title: "Domain Modelling, DDD & ERM — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [ddd, aggregates, erm, ef-core, eventstorming, context-mapping]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for extracting a domain and ER model from artifacts: what DDD stereotypes are
  reliably machine-detectable, why bounded contexts are not, and the anemic-domain-model problem
  that no structural heuristic can see through.
---

# Domain Modelling, DDD & ERM — domain knowledge

**Domain & problem:** AI-DE extracts a **domain model** (bounded contexts, aggregates, entities, value
objects, domain events) and an **ER model** (databases, tables, columns, foreign keys) from real repository
artifacts — C# via Roslyn, SQL DDL via ScriptDom — resolving DDD stereotypes from attributes, marker
interfaces or namespace convention, and stitching domain↔data through EF Core mappings.

**Canonical framing:** The field separates **tactical DDD** (entity, value object, aggregate, repository —
the code-level patterns) from **strategic DDD** (bounded contexts, context maps, ubiquitous language — the
boundary-level thinking), and its settled position is that **the strategic half is the valuable one and the
tactical half is the cargo-culted one**. That framing is directly load-bearing for us, because it maps
almost exactly onto what is and is not extractable.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"Definitions, notations and constants" — this
domain's constants are canonical definitions that must be stated precisely.)*

## Headline findings

1. **The extractable/judgement line runs exactly along the tactical/strategic split.** Tactical building
   blocks are tractable to detect; **bounded-context boundaries are not** — they require team and
   organisational knowledge that exists nowhere in the source. Fowler calls Bounded Contexts and the Context
   Map *"a particularly important part of DDD"* and credits Evans with being the first to tackle strategic
   design *"in any compelling way"*. So the half we can extract is the half the field considers less
   valuable. — *(Verified, [S6])*
2. **Vernon's four aggregate rules are the consensus, and they are what an aggregate-boundary detector
   should be checked against:** (1) model true invariants in consistency boundaries; (2) design **small**
   aggregates; (3) reference other aggregates **by identity only**; (4) use eventual consistency outside the
   boundary. dddcommunity.org states these *"spell out the current consensus view of DDD leaders."* — *(Verified, [S3][S4])*
3. **EF Core's `[Owned]` is the one syntactically unambiguous value-object signal in .NET.** Marker
   interfaces (`IAggregateRoot`) and base classes (`Entity<T>`) are detectable by Roslyn symbol analysis but
   depend on convention; **namespace convention is the weakest signal and is a heuristic only**. — *(Verified, [S16][S17])*
4. **EF Core's design-time `IModel` is the richest domain↔data bridge available, and it needs no database.**
   `IEntityType.IsOwned()`, `IForeignKey.IsOwnership`, `IProperty.IsShadowProperty()`,
   `IProperty.GetValueConverter()`, `IEntityType.GetDeclaredProperties()` form a typed, queryable graph —
   including shadow FK properties that exist in the model but not on the CLR type. — *(Verified, [S16])*
5. **Aggregates do not map one-to-one to tables, so table-counting is wrong by construction.** One aggregate
   commonly spans several tables (Order + OrderLine); `OwnsMany` maps a value-object collection to a child
   table with a composite key. **The stitching pass must walk owned-entity navigations to collapse a table
   set into one aggregate node**, or it will over- or under-count depending on mapping strategy. — *(Verified, [S16])*
6. **A DDL parser recovers the physical model and never the conceptual one.** Extractable: tables, columns,
   explicit FK constraints, CHECK constraints, indexes, computed-column expressions, types. **Not
   extractable:** implicit relationships with no FK, polymorphic discriminator semantics, soft-delete flags,
   EAV patterns, the meaning of a nullable column, and — critically — **whether a FK crosses an aggregate
   boundary or navigates within one**. — *(Inferred; industry consensus across sources)*
7. **The anemic domain model is the confounder no structural heuristic sees through.** Fowler defines it as
   objects named after domain nouns containing only getters and setters, with the logic displaced into
   services. **AI-DE cannot distinguish a rich domain model from an anemic one structurally** — behavioural
   richness (method count, encapsulated invariants, absence of public setters) is a weak proxy at best. — *(Verified, [S9])*
8. **ContextMapper is the closest comparable tool, and the gap it leaves is precisely ours.** It is an
   Eclipse/VS Code DSL (CML, Java) operating on **manually authored** files; its reverse-engineering library
   detects bounded contexts from **Spring Boot annotations** and relationships from **Docker Compose** — not
   from DDD stereotypes in .NET source. — *(Verified, [S10]–[S15])*
9. **SQL:2011 system-versioned temporal tables are machine-extractable from DDL**, because the
   `PERIOD FOR SYSTEM_TIME(start, end)` clause and `WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = …))`
   option are explicit syntax ScriptDom parses. Supported from SQL Server 2016 (v13), Azure SQL Database and
   Azure SQL MI. — *(Verified, [S18])*
10. **IDEF1X's standard status could not be confirmed.** FIPS PUB 184 was issued 1993-12-21; the NIST page
    redirected to the ITL home without a document page, and the FIPS programme was substantially wound down
    in the 2000s. It is *widely described* as withdrawn, and the withdrawal was **not confirmed from a live
    NIST page**. — *(Flagged, [S19])*

## Confidence summary

Verified: Vernon's four rules and their consensus status, the full context-map pattern list, EF Core's
`[Owned]` and `IModel` API surface, the aggregate-to-table mapping behaviour, ContextMapper's capabilities
and its Spring-Boot/Docker-Compose reverse-engineering scope, the temporal-table syntax and support matrix,
Fowler's anemic-domain-model definition and his strategic-DDD position, Chen's 1976 paper, and the SCD, normal-form
and Crow's-Foot definitions. Inferred: the DDL extractable/not-extractable boundary (consensus across
sources rather than a single citation); the reliability ranking of stereotype signals. Flagged: **IDEF1X's
withdrawal status**; the Evans *DDD Reference* PDF was fetched but not renderable, so its pattern definitions
come via secondary confirmation.

**Load-bearing Flagged claim:** none gates a decision. The load-bearing *Verified* finding to act on is
that bounded contexts are not extractable — because a design that assumes they are will produce confident,
wrong boundaries.

## Design implications

- **Do not attempt to infer bounded contexts.** They are an organisational fact, not a code fact. Let a
  human declare them (a config file, an assembly attribute, a `.contextmap` artifact) and *validate* the
  declaration against the code — flagging cross-context references — rather than guessing the boundary.
- **Rank the stereotype signals explicitly and carry the rank onto the node.** `[Owned]` and explicit
  attributes are strong; marker interfaces and base classes are medium; namespace convention is weak. A
  `ValueObject` node derived from a namespace should not look identical to one derived from `[Owned]`.
- **Prefer the EF Core `IModel` over syntax walking for the data bridge.** It is design-time, needs no
  database, exposes ownership and shadow properties, and it is the authoritative answer to "which tables
  does this aggregate span". The cost — it executes `OnModelCreating` — is already recorded in the
  extraction knowledge base and is worth paying here.
- **Collapse table sets into aggregate nodes by walking ownership navigations.** Never infer aggregate count
  from table count.
- **Detect and surface the anemic-model signals rather than pretending to classify.** Absence of domain
  events, aggregate roots with no methods, all-public setters — report these as *observations about the
  model's richness*, not as a verdict. This is the honest version of a check that cannot be made reliable.
- **Never render a whole schema as one ER diagram.** tbls generates per-table linked Markdown and SchemaSpy
  an interactive site for exactly this reason. Filter by bounded context or aggregate; a 300-table diagram
  is unreadable regardless of layout quality.
- **Use Vernon's four rules as a lint, not as a classifier.** "This aggregate holds a direct object
  reference to another aggregate root" (rule 3) is a genuinely useful, mechanically checkable finding.
- **Extract temporal tables — the syntax is explicit** and history modelling is exactly the kind of fact a
  domain view should show.
- **Borrow ContextMapper's CML relationship syntax** (`[D,ACL]<-[U,OHS,PL]`) as the notation for context-map
  edges rather than inventing one. It is compact, established, and already tooled.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The definitions in
`references.md` — Vernon's rules, the context-map patterns, the SCD types, the normal forms, the Crow's-Foot
semantics — are stated precisely because paraphrasing them loses the constraint. Note that this topic
composes with the pack's own `domain-and-data-modelling` standard, which governs how *we* model; this base
records what the *field* holds and what is extractable.
