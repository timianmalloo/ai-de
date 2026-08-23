---
id: kb-domain-sota
title: "Domain Modelling & ERM — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [ddd, evans, vernon, eventstorming, erm, ef-core, temporal]
links:
  - { to: kb-domain-modeling-and-erm, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The DDD canon and its strategic/tactical split, how domain stereotypes appear in .NET code,
  collaborative modelling methods, the ERM notation families, and what a DDL parser can and
  cannot recover.
---

# State of the art — domain modelling, DDD and ERM

## The DDD canon

**Evans (2003), the blue book** — tactical building blocks: **Entity** (identity persists through attribute
change), **Value Object** (defined wholly by its attributes, immutable, interchangeable), **Aggregate** (a
cluster of entities and value objects treated as a unit, with one **Aggregate Root** as the sole external
entry point and the transactional boundary), **Repository** (reconstitutes aggregates from storage),
**Factory** (encapsulates complex creation), **Domain Service** (a stateless operation belonging to no
entity), **Module** (a cohesive grouping — roughly a namespace), and **Domain Event** (community addition,
incorporated into Evans's 2015 *DDD Reference*). Plus the idea underneath all of them: **Ubiquitous
Language**, the shared vocabulary embedded in the code. *(Verified, [S1][S2])*

**Vernon's four aggregate rules** — the consensus statement, quoted from dddcommunity.org as rules that
*"spell out the current consensus view of DDD leaders on the style of aggregates"*:

1. **Model true invariants in consistency boundaries** — group only what must be transactionally consistent.
2. **Design small aggregates** — one entity root plus a few value objects by default; add only for a real
   invariant.
3. **Reference other aggregates by identity only** — hold an ID, never an object reference.
4. **Use eventual consistency outside the boundary** — cross-aggregate updates are asynchronous, not
   distributed ACID.

*(Verified, [S3][S4])*

**The full context-map pattern list** (Evans 2003, Parts III–IV; consolidated in the 2015 *DDD Reference*):

| Pattern | What it describes |
|---|---|
| **Partnership** | Two teams with coupled goals coordinating planning and release |
| **Shared Kernel** | An explicitly shared, co-owned subset of the model |
| **Customer–Supplier** | Upstream/downstream where the downstream's needs factor into upstream planning |
| **Conformist** | The downstream simply adopts the upstream model |
| **Anticorruption Layer** | A translation layer protecting a downstream model from an upstream one |
| **Open Host Service** | The upstream publishes a protocol for many consumers |
| **Published Language** | A shared, well-documented interchange language |
| **Separate Ways** | No integration at all — a legitimate choice |
| **Big Ball of Mud** | Naming the region where no model boundary holds |

*(Verified, [S1][S2])*

**The strategic/tactical split, and why it matters here.** Fowler describes Bounded Contexts and the Context
Map as *"a particularly important part of DDD"* and credits Evans as the first to tackle strategic design
*"in any compelling way"*; his recommendation of Vernon (2013) is specifically *"focusing particularly on the
strategic design aspect."* The well-attested community consensus is that teams adopt the tactical patterns
without ever doing the context-mapping work. **The half that is extractable is the half the field values
less.** *(Verified, [S6])*

**The anemic domain model** — Fowler's definition: objects named after domain nouns that contain only
getters and setters, with the domain logic displaced into service classes. It is the confounder for any
structural extraction, because an anemic model and a rich model have the *same* stereotypes.
*(Verified, [S9])*

## Making the domain model explicit in .NET — signal strength

| Mechanism | Detectability | Notes |
|---|---|---|
| **EF Core `[Owned]` / `OwnsOne` / `OwnsMany`** | **Strong — syntactically unambiguous** | The one reliable value-object signal in .NET; produces owned-entity metadata in the model |
| Explicit attributes (`[Aggregate]`, `[ValueObject]`, `[DomainEvent]`) | Strong, if mandated | Requires an adopted attribute package — high precision, adoption friction |
| Marker interfaces (`IAggregateRoot`) / base classes (`Entity<T>`) | **Medium** | Detectable by Roslyn symbol analysis; depends on naming convention. NetArchTest confirms the pattern is real in practice |
| **Namespace convention** (`*.Domain.*`) | **Weak — heuristic only** | Should never produce a node that looks as confident as an `[Owned]`-derived one |
| C# `record` for value objects | Weak | Suggestive, not decisive — records are used for many things |

*(Verified, [S16][S17])*

**EF Core's design-time `IModel` is the richest bridge**, and it works without a database connection:

| API | What it answers |
|---|---|
| `IEntityType.IsOwned()` | Is this an owned (value-object-like) type? |
| `IForeignKey.IsOwnership` | Is this FK an `OwnsOne`/`OwnsMany` relationship rather than an association? |
| `IProperty.IsShadowProperty()` | Is this an FK column with **no CLR property** — invisible to syntax analysis? |
| `IProperty.GetValueConverter()` | Is there a value conversion in play? |
| `IEntityType.GetDeclaredProperties()` | All mapped columns |

*(Verified, [S16])*

**The impedance mismatch, concretely:** one aggregate commonly spans several tables (Order + OrderLine), and
`OwnsMany` maps a value-object collection to a child table with a composite primary key (FK plus a
surrogate). **Naive table-count arithmetic over- or under-counts aggregates depending on mapping strategy**;
the correct approach is to walk ownership navigations and collapse the table set into one aggregate node.
*(Verified, [S16])*

## Collaborative modelling — what no extractor produces

**EventStorming** (Brandolini) at three levels — *big picture*, *process*, *design* — using a sticky-note
colour grammar; **Domain Storytelling**; **Event Modeling**; and **Wardley mapping** as the strategic
complement. What these produce that extraction cannot: **the invariants, the boundaries, and the reasons** —
the *why* behind a model rather than the *what* of it. Any tool in this space should be explicit that it
complements these rather than replacing them. *(Reference; the colour grammar was not fetched — Flagged)*

## ERM

**Chen (1976)** is the origin: rectangles for entities, diamonds for relationships, ellipses for attributes.
*(Verified, [S5])*

**The notation families** — Chen; **Crow's Foot / Information Engineering** (dominant in tooling); Barker;
**IDEF1X** (FIPS PUB 184, issued 1993-12-21, **status Flagged** — the NIST page redirected to the ITL home
and the FIPS programme was substantially wound down in the 2000s; widely *described* as withdrawn, not
confirmed); and UML class diagrams pressed into service as ERDs.

**Crow's Foot semantics, precisely** — the part that is routinely misread:

| Marking | Meaning |
|---|---|
| Single stroke `\|` | exactly one (mandatory) |
| Circle `O` | zero (optional) |
| Crow's foot `<` | many |
| `O<` | zero-or-more |
| `\|\|` | exactly one |
| `\|<` | one-or-more |
| `O\|` | zero-or-one |

*(Verified, [S19]-adjacent tooling docs)*

**Conceptual → logical → physical:** the conceptual level carries entities, relationships and business
rules with no keys or types; the logical adds attributes, keys and normalisation without engine specifics;
the physical adds tables, column types, indexes and partitions. A concept appearing first at the physical
level means the conceptual model was authored by whoever wrote the migration. *(Reference)*

## Normalisation and the modelling schools

**Normal forms** (Codd 1970/1971; Boyce-Codd 1974), stated precisely:

- **1NF** — atomic values, no repeating groups.
- **2NF** — no partial dependency on a composite primary key.
- **3NF** — no transitive dependency.
- **BCNF** — every determinant is a superkey.

**Kimball vs Inmon** — the dimensional (star-schema, conformed-dimension, bottom-up) versus Corporate
Information Factory (normalised enterprise warehouse, top-down) debate. **Data Vault 2.0** — hubs (business
keys), links (relationships), satellites (descriptive and historical attributes). **Anchor modelling** — the
sixth-normal-form extreme.

**Slowly changing dimensions (Kimball & Ross 2013)** — the type taxonomy, stated precisely:

| Type | Behaviour |
|---|---|
| **0** | Freeze — the attribute never changes |
| **1** | Overwrite — history discarded |
| **2** | New row with a new surrogate key — full history |
| **3** | A prior-value column — one step of history |
| **4** | A separate history table |
| **6** | Hybrid of 1 + 2 + 3 |

**Temporal / bitemporal** — transaction (system) time and valid (application) time tracked independently.
**SQL:2011 system-versioned tables** are the standardised mechanism and are **machine-extractable**:
`PERIOD FOR SYSTEM_TIME(start_col, end_col)` plus
`WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = schema.TableHistory))`, supported from **SQL Server 2016
(v13)**, Azure SQL Database and Azure SQL MI. *(Verified, [S18])*

## What a DDL parser can and cannot recover

| Recoverable | Not recoverable |
|---|---|
| Tables, columns, types | Implicit relationships with **no FK constraint** |
| Explicit FK constraints | Polymorphic / discriminator semantics |
| CHECK constraints | Soft-delete flags |
| Indexes | EAV patterns |
| Computed-column expressions | The business meaning of a nullable column |
| Temporal-table syntax | **Whether a FK crosses an aggregate boundary or navigates within one** |

*(Inferred — consensus across sources rather than a single citation)*

The last row is the decisive one: it is the reason the domain side and the data side must be **stitched**
rather than either being used alone. ORM mappings are what carry that information, which is why EF Core's
`IModel` matters more here than any DDL parser.

## The frontier

- **ContextMapper** is the closest comparable: an Eclipse/VS Code DSL (CML) over **manually authored** files,
  with reverse engineering from **Spring Boot annotations** and **Docker Compose** — not from .NET
  stereotypes. Its relationship syntax, `[D,ACL]<-[U,OHS,PL]`, is a compact established notation for
  context-map edges. *(Verified, [S10]–[S15])*
- **Nobody extracts strategic DDD**, because it is not in the code. The open question is whether *validating*
  a declared context map against extracted references is useful enough to be worth building.
