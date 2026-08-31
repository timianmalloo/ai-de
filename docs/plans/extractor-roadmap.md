---
id: plan-extractor-roadmap
title: "Extractor roadmap — what reads the repository, and in what order"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "phase-3"
tags: [plan, extractors, coverage, graph, evidence]
links:
  - { to: plan-ai-native-ide-architecture, rel: relates-to }
  - { to: adr-0018-node-content-reader-contract, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Every extractor this product has, every one it does not, and the order the remaining work is worth
  doing in — with the coverage of each measured on a real repository rather than estimated.
---

# Extractor roadmap

The graph is only as good as what reads the repository. This is the standing list: what exists, what
each one can and cannot see, and what is worth building next.

**Every number here was measured on TheTerrace** (64 scopes, ~34,000 assertions) on 2026-08-31. When
a number moves, this file is wrong until somebody re-measures — the harness is
`tools/measure-repositories.py`.

## The rule this list is built on

**A boundary is not a gap.** The product does not index the Python standard library or the .NET base
class library, and saying so is a statement about scope, not a hole in coverage. A gap is something
the product means to read and cannot.

That distinction is not pedantry — it was measured to matter. Python disclosed
`python-imports-not-resolved (246 import(s) name something this scope does not contain)`, which read
as the largest coverage hole in any built extractor and was **prioritised as one on that reading**.
Every one of the 246 was the standard library. After teaching the extractor the difference: **2
genuine unknowns**. The number had been arithmetically right and said something false, and its cost
was not a bad sentence — it was a wrong plan.

## Built

| Extractor | Reads | Emits | Measured coverage |
|---|---|---|---|
| **C#** (Roslyn, no MSBuild) | `.cs` via a `CSharpCompilation` per project/TFM | `has_type`, `declared_in`, `depends_on`, `inherits`, `implements`, `has_member`, `declares_table`, `uses_table` | 4 scopes, 21,298 facts; 1,428 types; 9,854 members |
| **Knowledge** (frontmatter + prose links) | `.md` with graph frontmatter | `has_type`, `node_class`, `declared_in`, `owned_by`, `review_by`, typed links, `links_to` | 39 scopes, 10,508 facts; **877 distinct documents**; 42 prose-link edges |
| **Python** (line-oriented) | `.py` top-level declarations + imports | `has_type`, `declared_in`, `imports` | 5 scopes, 1,286 facts; 2 unknown imports |
| **EF schema** (migrations) | `Migrations/*.cs` folded in order | `has_type`, `declared_in`, `has_column`, `introduced_by` | 1 scope, 761 facts; 64 tables |
| **Bicep** | `.bicep` templates | `has_type`, `declared_in`, `depends_on`, `resource_type`, `api_version`, `parameter_type`, `is_secret`, `is_loop`, `is_conditional` | 2 scopes, 209 facts |
| **TypeScript / JS** (line-oriented) | `.ts .tsx .js .jsx .mjs .cjs`, hand-written only | `has_type`, `declared_in`, `imports`, `is_exported` | 13 scopes, 140 facts; 22 functions, 2 classes, 7 modules; **0 unknown imports** |
| **SQL schema** | `CREATE TABLE` / `ALTER TABLE` folded in file order | `declares_table`, `has_column` | 0 scopes here; exercised on other repositories |

## Not built

Present in `UnanalysedLanguages`, so their absence is **counted and disclosed** rather than silent —
a repository with Go in it says so.

| Language | Status |
|---|---|
| Go, Rust, Java, Kotlin, Ruby, PHP, Swift | Not built |

Nothing is scheduled here. A language earns an extractor when a repository somebody uses contains it,
and the disclosure is what will say so.

## The order the remaining work is worth doing in

Ranked by *what a user cannot currently ask*, not by what is missing in the abstract.

| # | Work | Why it is where it is | Size |
|---|---|---|---|
| **1** | **De-duplicate knowledge scopes, and resolve prose links workspace-wide** | Knowledge scopes NEST: `knowledge:docs` reads everything under it and `knowledge:docs/adr` reads it again, so every knowledge fact is stored ~2.7 times — MEASURED, 2,368 `node_class` rows for **877** distinct documents. Reading each directory's own files fixes it (877 documents preserved, knowledge facts 10,508 → 4,326) **but costs 30 of the 42 prose-link edges**, because a link across directories only resolves for a scope that read both. The two must be done together: resolve against the whole workspace, emit per scope. Attempted and reverted 2026-08-31 rather than ship the regression. | Medium |
| **2** | **C# call edges** | `depends_on` is type-level. "What calls this" is the question a code graph is expected to answer and cannot. | Medium |
| **3** | **Schema changed by raw SQL** | The EF reader folds migrations; `ExecuteSqlRaw` changes the schema and is not read, so the schema can be quietly wrong rather than incomplete. | Medium |
| **4** | **`Describe`'s ordering** | Not an extractor, but it caps a node's facts at 50 ordered `subject, predicate, object` — **alphabetically, not by importance**. 12 of 877 knowledge documents were already over it before any of today's work, so a node's type and owner can fall outside the window while its links fill it. Found by simulating headings against the real store. | Small |
| **5** | **Python nested declarations** | Column-zero only, so a class inside a function is invisible. Bounded and rarely load-bearing. | Small |
| **6** | **Bicep expression evaluation** | Resource names built from expressions stay unevaluated; `count` is indeterminate. Correctly disclosed, and evaluating it means writing an interpreter. | Large, low value |

**Knowledge body analysis and TypeScript were items 1 and 2 and both shipped on 2026-08-31.** What
replaced them at the top is what those two uncovered: the nesting duplication, and the fact that
TypeScript's "gap" was 83% invented facts rather than missing ones.

**3 is a correctness risk rather than a coverage one** — a schema that is quietly wrong beats one that
is honestly incomplete. The rest are boundaries, already disclosed.

## Standing rules for anyone adding one

1. **The disclosure is part of the extractor.** A reader that cannot see something says so, on the
   scope, in its own words, with a count. `UnanalysedLanguages` is the *last* step of adding an
   extractor, not an afterthought — that list has needed the same correction three times.
2. **Distinguish a boundary from a gap** in the disclosure text and in the counts. They are different
   statements and conflating them misdirects the plan.
3. **Do not draw the runtime.** The BCL and the Python standard library are excluded from edges: a
   view centred on `string`, `int`, `sys` and `os` is not a picture of anybody's domain.
4. **Attributes for properties, relations for peers.** `has_member` and `has_column` are attributes;
   emitting them as edges would have added ~10,000 nodes to serve a card layout.
5. **Bump `ScopeFingerprints.ExtractorGeneration`.** Changing what an extractor emits for unchanged
   input means every existing store is stale. `verify-extractor-generation.py` enforces it.
6. **Measure on a real repository before claiming coverage.** Every number in this file came from one,
   and the one time a number here was inferred instead, it was wrong by two orders of magnitude.
