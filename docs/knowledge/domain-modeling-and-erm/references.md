---
id: kb-domain-references
title: "Domain Modelling & ERM — references, definitions and constants"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, ddd, scd, normal-forms, crows-foot, temporal, ef-core]
links:
  - { to: kb-domain-modeling-and-erm, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The load-bearing definitions stated precisely — Vernon's four rules, the context-map patterns,
  the SCD types, the normal forms, Crow's Foot semantics, the temporal syntax and the EF Core
  IModel API.
---

# Reference information

These are **definitions, not summaries**. Paraphrasing them loses the constraint they express.

## Books and papers

- **Evans, Eric.** *Domain-Driven Design: Tackling Complexity in the Heart of Software.* Addison-Wesley,
  2003. ISBN **0-321-12521-5** ("the blue book"). Building blocks in Part II; strategic design in Parts III–IV.
- **Evans, Eric.** *Domain-Driven Design Reference*, 2015 — the consolidated pattern definitions.
  *(Fetched but the PDF was not renderable; pattern definitions confirmed via secondary sources — **Flagged**)*
- **Vernon, Vaughn.** *Implementing Domain-Driven Design.* Addison-Wesley, 2013. ISBN 978-0-321-83457-7.
- **Vernon, Vaughn.** *Effective Aggregate Design* (2011), dddcommunity.org — the source of the four rules.
- **Chen, Peter.** *The Entity-Relationship Model — Toward a Unified View of Data.* ACM TODS, 1976,
  DOI 10.1145/320434.320440.
- **Kimball & Ross.** *The Data Warehouse Toolkit*, 3rd ed., 2013 — the SCD taxonomy.
- **Codd (1970, 1971); Boyce-Codd (1974)** — the normal forms.
- **Fowler, Martin** — *DomainDrivenDesign*, *BoundedContext*, *DDD_Aggregate*, *AnemicDomainModel* blikis.

## Vernon's four aggregate rules (canonical wording)

1. **Model true invariants in consistency boundaries** — group only objects that must be transactionally
   consistent together.
2. **Design small aggregates** — default to one entity root plus a few value objects; add more only if a
   real invariant requires it.
3. **Reference other aggregates by identity only** — hold an ID, not an object reference.
4. **Use eventual consistency outside the boundary** — cross-aggregate updates require asynchronous
   mechanisms, not distributed ACID transactions.

dddcommunity.org states these *"spell out the current consensus view of DDD leaders on the style of
aggregates."* *(Verified, [S3][S4])*

## Context-map patterns (full list)

Partnership · Shared Kernel · Customer–Supplier · Conformist · Anticorruption Layer · Open Host Service ·
Published Language · Separate Ways · Big Ball of Mud. *(Verified, [S1][S2])*

**ContextMapper's CML relationship syntax:** `[D,ACL]<-[U,OHS,PL]` — a downstream with an Anticorruption
Layer consuming an upstream with an Open Host Service and a Published Language. *(Verified, [S15])*

## Slowly changing dimensions (Kimball & Ross 2013)

| Type | Behaviour |
|---|---|
| **0** | Freeze — the attribute never changes |
| **1** | Overwrite — history is discarded |
| **2** | New row with a new surrogate key — full history preserved |
| **3** | A prior-value column — one step of history |
| **4** | A separate history table |
| **6** | Hybrid of 1 + 2 + 3 |

*(Verified)*

## Normal forms (Codd 1970/1971; Boyce-Codd 1974)

- **1NF** — atomic values, no repeating groups.
- **2NF** — no partial dependency on a composite primary key.
- **3NF** — no transitive dependency.
- **BCNF** — every determinant is a superkey.

*(Verified)*

## Crow's Foot / IE notation semantics

| Marking | Meaning |
|---|---|
| Single stroke at the relationship end | exactly one (mandatory "one") |
| Circle | zero (optional) |
| Crow's foot (three lines) | many |
| `O<` | zero-or-more |
| `\|\|` | exactly one |
| `\|<` | one-or-more |
| `O\|` | zero-or-one |

*(Verified)*

## Notation families and their status

| Family | Status |
|---|---|
| **Chen (1976)** | The original — rectangles = entities, diamonds = relationships, ellipses = attributes |
| **Crow's Foot / Information Engineering** | **Dominant in tooling** |
| **Barker** | Oracle lineage |
| **IDEF1X** | **FIPS PUB 184**, issued **1993-12-21**. **Status Flagged** — the NIST page redirected to the ITL home with no document page; the FIPS programme was substantially wound down in the 2000s; widely *described* as withdrawn, **not confirmed from a live NIST page** |
| **UML class diagrams as ERDs** | Common in practice |

*(Chen Verified [S5]; IDEF1X **Flagged** [S19])*

## SQL:2011 system-versioned temporal tables

```sql
PERIOD FOR SYSTEM_TIME (start_col, end_col)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = schema.TableHistory))
```

Two `datetime2` period columns plus a linked history table. Supported from **SQL Server 2016 (v13)**, Azure
SQL Database and Azure SQL Managed Instance. **Machine-extractable** — ScriptDom parses both the
`PERIOD FOR SYSTEM_TIME` clause and the `SYSTEM_VERSIONING` table option. *(Verified, [S18])*

## EF Core design-time `IModel` API

| Member | Answers |
|---|---|
| `IEntityType.IsOwned()` | Is this an owned (value-object-like) type? |
| `IForeignKey.IsOwnership` | Is this FK an `OwnsOne`/`OwnsMany` relationship? |
| `IProperty.IsShadowProperty()` | Is this an FK column with **no CLR property**? |
| `IProperty.GetValueConverter()` | Returns a `ValueConverter` or null |
| `IEntityType.GetDeclaredProperties()` | All mapped columns |

`[Owned]` / `OwnsOne` / `OwnsMany` produce owned-entity metadata that is **unambiguous for value-object
detection**. *(Verified, [S16])*

## The anemic domain model

Fowler's definition: objects named after domain nouns that contain **only getters and setters**, with the
domain logic displaced into service classes. Structurally indistinguishable from a rich model by stereotype
detection alone. *(Verified, [S9])*
