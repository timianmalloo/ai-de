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
| **C#** (Roslyn, no MSBuild) | `.cs` via a `CSharpCompilation` per project/TFM | `has_type`, `declared_in`, `depends_on`, `inherits`, `implements`, `has_member`, **`calls`**, `declares_table`, `uses_table` | 4 scopes; 1,428 types; 9,854 members; **1,492 call edges** |
| **Knowledge** (frontmatter + prose links) | `.md` with graph frontmatter | `has_type`, `node_class`, `declared_in`, `owned_by`, `review_by`, typed links, `links_to` | 39 scopes; **878 documents, each extracted exactly once**; 42 prose-link edges resolved workspace-wide |
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
| **1** | **The graph pane's payload budget** | This is now the binding constraint on every extractor, and it moved from theory to arithmetic today. Call edges alone left `graph:default` with **18,496 bytes** of a 1 MiB frame; only because knowledge de-duplication landed in the same change did the combined result come back to **195,896**. Two more edge families and the surface starts dropping nodes to carry them — MEASURED once already, 2,630 drawn nodes falling to 1,471. The graph needs a bounded view (the overview/LOD path exists and is unbound to the canvas) or a way to ask for edges by kind, **before** anything else is added to it. | Medium |
| **2** | **C# extraction time** | Binding every method body took the index from **5.9s to 15.5s** on TheTerrace — inside the 60-second per-scope budget, and a 2.6x regression that will grow with the repository. The honest prefilter (skip invocations whose name matches nothing declared in source) was rejected during the work because it folds the boundary into the gap (DC-050); a better one needs finding rather than assuming. | Medium |
| **3** | **Schema changed by raw SQL** | *Investigated 2026-08-31 and NOT built.* The EF reader cannot fold `migrationBuilder.Sql`, and the disclosure now says how much that costs: **4 of 23** raw statements in `Up` methods carry DDL. Measured further — the one raw statement that adds a column is followed by a raw statement dropping the same one, so the net effect on TheTerrace's graph is **zero and the schema shown is correct**. Worth building when a repository is found where raw SQL adds a column and keeps it; building it now would be a fold for a measured zero. | Medium, unproven |
| **5** | **Python nested declarations** | Column-zero only, so a class inside a function is invisible. Bounded and rarely load-bearing. | Small |
| **6** | **Bicep expression evaluation** | Resource names built from expressions stay unevaluated; `count` is indeterminate. Correctly disclosed, and evaluating it means writing an interpreter. | Large, low value |

**Four extractor items have shipped in two days** — knowledge body analysis, TypeScript precision,
knowledge de-duplication, and C# call edges. What sits at the top now is not an extractor at all, and
that is the finding: **the constraint has moved from what can be read to what can be carried.** Every
one of those four made the graph payload larger, and the last two were only compatible because one of
them made it smaller.

The pattern worth keeping: each item was ranked by a number, and three of the four changed rank once
the number was measured. Python's "largest gap" was 99% boundary; TypeScript's was 83% invention;
method-level call edges were ruled out by a payload arithmetic done before any code was written.

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
