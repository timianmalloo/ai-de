---
id: design-phase-3-architecture-data-infra
title: "Design: Phase 3 — architecture, data and infrastructure joins"
type: design
status: proposed
owner: "@timianmalloo"
phase: "3"
tags: [design, phase-3, bicep, ddl, domain, joins]
links:
  - { to: architecture, rel: implements }
  - { to: design-phase-2-real-code-and-terminal, rel: refines }
  - { to: review-phase-2-exit, rel: relates-to }
  - { to: adr-0001-derived-evidence-views, rel: depends-on }
review-by: 2027-02-28
summary: >-
  Phase 3 joins C# evidence to infrastructure and schema evidence. Grounded in a real repository
  rather than the phase plan: its schema is EF Core migrations, not DDL files, so the planned "DDL
  parser" would have found nothing. Three components, one of which the phase plan did not have.
---

# Design: Phase 3 — architecture, data and infrastructure joins

**Opened 2026-08-28**, on the handoff from the [Phase-2 exit review](../reviews/phase-2-exit.md).

## Responsibility

The architecture states Phase 3 as: *navigate C4, ERM, domain, and dependency projections across C#,
DDL, and Bicep evidence* — real Bicep adapter, DDL parser, declared bounded-context config, curation
policy.

**The value is the join, not the parsers.** C# evidence already exists and is measured. A Bicep file
read on its own is a resource list anyone can get from the portal. What nothing else answers is
*"this class writes to that table, which lives on that server, which this template deploys"* — and
that sentence spans three extractors.

## Grounding: what a real repository actually contains

Before designing to the phase plan, the plan was checked against a repository that was not written
for this tool (`TheTerrace`, 811 C# files, indexed by Phase 2 into 11,041 assertions).

| Phase plan expected | What is actually there | Consequence |
|---|---|---|
| DDL files to parse | **Zero `.sql` files.** 63 EF Core migration classes in C# | **The planned DDL parser would have found nothing.** Schema evidence is *already inside the C# the extractor reads* — it just is not interpreted as schema |
| Bicep templates | 2 files, `main.bicep` at **677 lines**, heavily parameterised, with `@secure()` parameters | Real but small; parameters and secrets are the interesting part, not resource count |
| Declared bounded contexts | None declared anywhere | The config has to be authored by a human; there is nothing to infer it from |

**This changes the component list.** A DDL parser is deferred in favour of an EF-migration reader,
because that is where the schema of a modern .NET repository lives. Building the planned parser first
would have produced a component with no corpus — which is exactly the failure the phase-2 spikes were
introduced to prevent.

## Components

### Component 1 — Infrastructure extractor (Bicep)

| | |
|---|---|
| **Reads** | `*.bicep` — and, following Phase 2's decision, **as data**. `bicep build` is a compiler invocation on repository-supplied input, which is the D3 shape again |
| **Emits** | `resource`/`module` declarations, their types, and `dependsOn` edges; `param` declarations with their constraints |
| **Discloses** | `bicep-expressions-not-evaluated` — a resource name built from `'${namePrefix}-ai'` is recorded as an expression, never as a guessed literal |
| **Never emits** | The value of anything marked `@secure()`. Not redacted after the fact — never read into the model at all |

`assume: the Bicep grammar is stable enough to read declaratively without the compiler. Confirmed by
parsing TheTerrace's main.bicep and comparing the resource set against az CLI output. Breaks if
expressions must be resolved to answer a join, in which case the join is disclosed as approximate
rather than the compiler being invoked.`

### Component 2 — Schema extractor (EF Core migrations)

Replaces the planned DDL parser. Reads the `Migrations` classes the C# extractor already parses and
interprets `migrationBuilder.CreateTable`/`AddColumn`/`CreateIndex` calls as **schema facts**.

**Why this is not just "the C# extractor with a filter":** the grain differs. The C# extractor's grain
is *one row is one relation between two code symbols*. Schema's grain is *one row is one column of one
table at one migration*. Migrations are **append-only and ordered**, which makes the current schema a
fold over them — the same shape as the fact store itself, which is why it fits without a new table.

| | |
|---|---|
| **Emits** | `Table has_column Column`, `Column has_type Type`, `Table has_index Index`, each carrying the migration that introduced it |
| **Discloses** | `schema-from-migrations-not-database` — this is the schema the code *intends*, not what a server has. They diverge, and pretending otherwise is the exact failure mode of an inferred join |

### Component 3 — The joins, and the curation policy

The reason the other two exist.

| Join | From → to | Status |
|---|---|---|
| Code → schema | A `DbSet<Order>` property → the `Orders` table | **`Inferred`** — EF's naming convention, not a declaration |
| Code → schema (declared) | `[Table("orders")]`, or a Fluent API `ToTable` call | `Verified` — it is written down |
| Schema → infrastructure | A table's server → the Bicep `Microsoft.Sql/servers` resource | **`Inferred`** unless the connection string names it literally |
| Code → infrastructure | An app setting read in C# → the `appSettings` entry in the template | `Verified` when both sides are literals |

**Confidence is the deliverable here, not the edge.** Phase 1's rule stands: a convention-derived
join is `Inferred` and says so. The temptation in Phase 3 is that an inferred join across three
artifacts *looks* more impressive than a verified one inside one — and it is exactly the kind of
claim a user would act on without checking.

**Bounded contexts are declared, never inferred.** There is nothing in a repository that reliably
says where one context ends. A config file authored by a human, validated against the extracted
symbols so a context naming a namespace that does not exist fails loudly.

## What Phase 2 hands over that this depends on

- **The extractor reads project files as data and never runs a build.** Both new extractors inherit
  that constraint; neither may invoke `bicep build` or `dotnet ef`.
- **Disclosures are facts on the scope node.** Both new extractors use the same mechanism, so a
  projection over mixed evidence reports every omission through one path.
- **One scope per (project, framework)** generalises to one scope per artifact set: Bicep scopes are
  per template file, schema scopes per migrations directory.
- **The daemon boundary is measured at ~0.35 ms.** Three extractors instead of one changes indexing
  cost, not query cost.

## Owed before implementation

| # | Item | Why it gates |
|---|---|---|
| 1 | **The Option-B fidelity spike extension**, carried from the Phase-2 exit conditions | Phase 3 multiplies whatever the C# extractor gets wrong across three artifact types |
| 2 | **Spike: Bicep as data** | Whether the declarative read is sufficient, or whether joins need resolved expressions. This is a contract, not an optimisation |
| 3 | **Spike: EF migration reading** | Whether the fold over 63 migrations reconstructs a schema that matches the model snapshot EF itself generates — a comparison with an oracle, like the fidelity spike |
| 4 | **Decision: the bounded-context config format** | It is the one input with no evidence behind it, so it needs an owner |

## Status and next action

| | |
|---|---|
| **Completed** | The phase opened and grounded against a real repository, which changed the component list before any code was written: the planned DDL parser is replaced by an EF-migration reader because the corpus has no DDL. |
| **Remaining** | Everything. Three spikes and one decision are owed before implementation. |
| **Best next action** | **Item 1 — finish the carried Phase-2 fidelity condition.** Phase 3 joins whatever the C# extractor produces to two other artifact types; an error there is multiplied rather than contained. |

---
**Handoff:** → spikes 2 and 3, after the carried Phase-2 condition is closed.
