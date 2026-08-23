---
id: kb-domain-open-questions
title: "Domain Modelling & ERM — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, anemic-model, disconfirming]
links:
  - { to: kb-domain-modeling-and-erm, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What research could not settle, how domain extraction fails silently, and the two
  disconfirming cases — against tactical DDD and against ER diagrams of large schemas — both of
  which survive.
---

# Open questions & domain failure modes

## Unresolved by research

1. **IDEF1X's standard status.** FIPS PUB 184 was issued 1993-12-21; the NIST page redirected to the ITL
   home with no document page, and the FIPS programme was substantially wound down in the 2000s. Widely
   *described* as withdrawn; **not confirmed**. *(Flagged, [S19])*
2. **Evans's 2015 *DDD Reference* PDF was fetched but not renderable**, so its consolidated pattern
   definitions are confirmed through secondary sources rather than read directly. *(Flagged)*
3. **The EventStorming colour grammar** was not fetched and is stated only as "a sticky-note colour
   grammar". If a workshop artifact is ever ingested, the exact grammar matters. *(Flagged)*
4. **Is validating a *declared* context map against extracted references useful enough to build?** Since
   boundaries cannot be inferred, the only tractable strategic feature is checking a human-declared map for
   violations. Whether that earns its keep is untested. *(Open)*
5. **How reliably can aggregate boundaries be collapsed from EF ownership navigations** in real codebases
   that mix `OwnsMany`, explicit join entities and table-per-hierarchy? *(Open)*
6. **Can anemic-model signals be made precise enough to report without being wrong?** Method count and
   public-setter ratio are weak proxies for behavioural richness. *(Open)*

## Known failure modes of this domain

- **Inferring bounded contexts.** The most damaging available mistake, because the output *looks* like a
  domain model and is an invention. Boundaries live in team structure and conversation, not in namespaces.
- **Treating an anemic model as a rich one.** The stereotypes are identical. A tool that reports
  "17 aggregates, 43 value objects" over a codebase of data bags has produced a confident, meaningless
  number.
- **Table-count arithmetic.** One aggregate spans several tables; `OwnsMany` adds child tables with
  composite keys. Counting tables as aggregates is wrong in both directions depending on mapping strategy.
- **Namespace convention promoted to fact.** `*.Domain.*` is a heuristic. A `ValueObject` node derived from
  a folder name that looks identical to one derived from `[Owned]` is a lie of omission.
- **Missing implicit relationships.** A `CustomerId` column with no FK constraint is a relationship the DDL
  parser cannot see, and such columns are extremely common in real schemas.
- **Mistaking a FK for an association.** A DDL parser cannot tell whether a foreign key crosses an aggregate
  boundary (which Vernon's rule 3 says should be an identity reference) or navigates within one. Only the
  ORM mapping carries that.
- **Rendering a whole schema.** 300 tables in one diagram is unreadable at any layout quality — which is
  why tbls generates per-table linked documents and SchemaSpy an interactive site.
- **Reverse-engineering an accreted schema and calling it a domain model.** An ORM-migrated schema records
  what the database *has become*, not what anyone designed.

## Disconfirming views we deliberately sought

### 1. The case against tactical DDD

**The argument.** The community consensus — Fowler, and practitioner writing since — holds that adopting
Entity / Value Object / Aggregate classes **without** the strategic work produces complexity with no
benefit. Teams build elaborate domain models that end as anemic data bags precisely because the real
modelling insight (what the invariants are, where the boundaries lie) was never done. Two supporting
strands: **DDD is overkill for CRUD** — Evans himself scopes it to "complex domains", and Transaction Script
outperforms a rich model in simple ones; and **aggregates can be a performance anti-pattern** — Vernon's
rule 3 (reference by identity only) creates the N+1 problem by design, so high-throughput systems
denormalise or bypass aggregate boundaries, and the resulting code does not conform to the textbook patterns
an extractor is looking for.

**How it fared.** **It stands, and it changes what the extractor should output.** The right response is not
to abandon stereotype detection but to **report absences alongside presences**: no domain events, aggregate
roots with no methods, all-public setters, direct object references between roots. Those are the signals
that distinguish a model from a model-shaped data layer — and framing them as *observations about richness*
rather than as a verdict is the only honest form, because the distinction cannot be made reliably.

The third strand also has a design consequence worth stating: **for a genuinely CRUD system, the ER model
may be the only useful output**, and a tool that insists on rendering aggregates over it is adding noise.

### 2. The case that ER diagrams of large schemas are useless

**The argument**, in three parts. **Density** — a 300-table schema fully rendered is unreadable, which is why
tbls generates per-table Markdown linked by navigation and SchemaSpy renders an interactive site rather than
one diagram. **Semantic poverty** — ER diagrams recover structure, not intent; a schema full of
`CustomerId` columns does not reveal which are foreign keys, which are soft references and which are audit
columns. **Drift** — an ORM-migrated schema may reflect no coherent design at all, only accumulated
migration patches, so reverse-engineering it produces a picture of what the database has become.

**How it fared.** **All three stand**, and together they are the strongest argument in this base *for* the
project's core design decision. Part one says: never render the whole schema — filter by bounded context or
aggregate. Part two is the **central argument for stitching**: the physical model is correct and the
conceptual model is absent, so the domain artifacts (C# stereotypes plus EF mappings) must be joined to the
DDL rather than either being used alone. Part three is a caution about expectations: on an accreted schema,
the honest output is "here is what exists", not "here is the domain model".

**The residual risk neither objection removes:** the stitching is only as good as the ORM mapping. Where a
team uses Dapper, raw SQL, or an ORM configured dynamically, the bridge between the domain and the data
model is simply not present in any artifact — and in that case the tool produces two disconnected graphs and
should say so plainly rather than inventing edges between them.
