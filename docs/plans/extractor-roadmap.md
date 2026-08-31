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
| **Knowledge** (markdown frontmatter) | `.md` with graph frontmatter | `has_type`, `node_class`, `declared_in`, `owned_by`, `review_by`, typed links | 39 scopes, 10,350 facts; 2,359 documents |
| **Python** (line-oriented) | `.py` top-level declarations + imports | `has_type`, `declared_in`, `imports` | 5 scopes, 1,286 facts; 2 unknown imports |
| **EF schema** (migrations) | `Migrations/*.cs` folded in order | `has_type`, `declared_in`, `has_column`, `introduced_by` | 1 scope, 761 facts; 64 tables |
| **Bicep** | `.bicep` templates | `has_type`, `declared_in`, `depends_on`, `resource_type`, `api_version`, `parameter_type`, `is_secret`, `is_loop`, `is_conditional` | 2 scopes, 209 facts |
| **TypeScript / JS** (line-oriented) | `.ts .tsx .js .jsx .mjs .cjs` exports + imports | `has_type`, `declared_in`, `imports` | 13 scopes, 194 facts |
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
| **1** | **Knowledge body analysis** | 2,359 documents in the graph and **not one fact from their prose** — only frontmatter. The product's premise is that docs hold intent; today it holds their metadata. Headings, terms and code references in the body are the largest unread surface in the repository. | Large |
| **2** | **TypeScript symbol resolution** | 13 scopes produce 194 facts — thinner than Python's 5 scopes producing 1,286. Non-exported symbols are invisible and types are not checked, so a TypeScript-heavy repository gets a graph that understates it. | Large |
| **3** | **C# call edges** | `depends_on` is type-level. "What calls this" is the question a code graph is expected to answer and cannot. | Medium |
| **4** | **Schema changed by raw SQL** | The EF reader folds migrations; `ExecuteSqlRaw` changes the schema and is not read, so the schema can be quietly wrong rather than incomplete. | Medium |
| **5** | **Python nested declarations** | Column-zero only, so a class inside a function is invisible. Bounded and rarely load-bearing. | Small |
| **6** | **Bicep expression evaluation** | Resource names built from expressions stay unevaluated; `count` is indeterminate. Correctly disclosed, and evaluating it means writing an interpreter. | Large, low value |

**1 and 2 are the ones that change what the product can answer.** 3 changes what it can answer about
code specifically. 4 is a correctness risk rather than a coverage one — the others are honest
boundaries that are already disclosed.

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
