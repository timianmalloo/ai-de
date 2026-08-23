---
id: kb-domain-glossary
title: "Domain Modelling & ERM — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, ddd, erm, ubiquitous-language]
links:
  - { to: kb-domain-modeling-and-erm, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The ubiquitous language of domain and ER modelling defined precisely — aggregate, bounded
  context, owned entity, shadow property, bitemporal — so the graph and the specs use one word
  per concept.
---

# Glossary — ubiquitous language

| Term | Precise definition |
|---|---|
| **Aggregate** | A cluster of entities and value objects treated as a unit for data changes, with one root entity; **the transactional consistency boundary**; external references reach only the root. *(Evans 2003, Verified [S1][S8])* |
| **Aggregate Root** | The single entity at the top of an aggregate — the sole entry point for external access and state change. *(Verified, [S8])* |
| **Anemic Domain Model** | Objects named after domain nouns containing only getters and setters, with logic displaced into services. **Structurally indistinguishable from a rich model by stereotype detection.** *(Verified, [S9])* |
| **Anticorruption Layer (ACL)** | A translation and isolation layer in a downstream context protecting it from an upstream model. *(Verified, [S1])* |
| **Bitemporal** | Tracking **transaction (system) time** and **valid (application) time** independently. |
| **Bounded Context** | An explicit boundary within which a particular model and ubiquitous language apply. **Organisational knowledge — not extractable from source.** *(Verified, [S7])* |
| **Chen notation** | The original ER notation: rectangles = entities, diamonds = relationships, ellipses = attributes. *(Verified, [S5])* |
| **Conceptual / logical / physical** | The three modelling levels: what exists (no keys or types) → structural shape (attributes, keys, normalisation) → storage (tables, types, indexes). A concept first appearing at the physical level means the conceptual model was authored by the migration. |
| **Conformist** | A context-map pattern where the downstream unconditionally adopts the upstream model. *(Verified, [S1])* |
| **Context Map** | The diagram of all bounded contexts and the integration relationships between them. *(Verified, [S1])* |
| **Crow's Foot / IE** | The dominant ERM notation in tooling: `\|` exactly one · `O` zero · `<` many, combining to `O<` zero-or-more, `\|\|` exactly one, `\|<` one-or-more, `O\|` zero-or-one. *(Verified)* |
| **Data Vault 2.0** | Hubs (business keys), links (relationships), satellites (descriptive and historical attributes). |
| **Domain Event** | A significant occurrence in the domain; a community addition to the canon, incorporated in Evans's 2015 *DDD Reference*. |
| **Entity** | An object with a thread of identity distinct from its attributes — continuity matters. *(Verified, [S1])* |
| **EventStorming** | Brandolini's collaborative workshop method at big-picture, process and design levels. **Produces the invariants and boundaries no extractor can recover.** |
| **IDEF1X** | An ERM notation standardised as FIPS PUB 184 (1993-12-21). **Status Flagged** — widely described as withdrawn, not confirmed from a live NIST page. *(Flagged, [S19])* |
| **Owned entity** (EF Core) | A type mapped as part of its owner rather than as an independent entity — declared by `[Owned]`, `OwnsOne` or `OwnsMany`. **The one unambiguous value-object signal in .NET.** *(Verified, [S16])* |
| **Open Host Service** | An upstream context publishing a protocol for many consumers. *(Verified, [S1])* |
| **SCD type** | The slowly-changing-dimension taxonomy: 0 freeze · 1 overwrite · 2 new row + surrogate · 3 prior-value column · 4 history table · 6 hybrid. *(Verified)* |
| **Shadow property** (EF Core) | A property in the EF model with **no corresponding CLR property** — typically an FK column. Invisible to syntax analysis, visible in `IModel`. *(Verified, [S16])* |
| **Shared Kernel** | An explicitly shared, co-owned subset of the model between two teams. *(Verified, [S1])* |
| **Strategic DDD** | Bounded contexts, context maps, ubiquitous language — **the half the field considers valuable and the half that is not extractable**. *(Verified, [S6])* |
| **System-versioned temporal table** | SQL:2011 mechanism: `PERIOD FOR SYSTEM_TIME(start, end)` plus `SYSTEM_VERSIONING = ON (HISTORY_TABLE = …)`. **Machine-extractable from DDL.** *(Verified, [S18])* |
| **Tactical DDD** | Entity, value object, aggregate, repository, factory, domain service, module — **the half that is extractable and the half most often cargo-culted**. *(Verified, [S6][S9])* |
| **Ubiquitous Language** | The shared vocabulary between domain experts and developers, embedded in the code. *(Verified, [S1])* |
| **Value Object** | An object defined entirely by its attributes — no identity, immutable, interchangeable. *(Verified, [S1])* |
