---
id: kb-extraction-open-questions
title: "Code & Infrastructure Extraction — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, source-generators, scip-dotnet]
links:
  - { to: kb-code-and-infra-extraction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The spikes this domain needs — scip-dotnet, source generators, multi-targeting — the ways
  extraction fails silently, and the disconfirming views on tree-sitter, LSIF and DacFx.
---

# Open questions & domain failure modes

## Unresolved by research — each is a spike, not a debate

1. **Is there a usable `scip-dotnet` indexer?** No official one is listed in the Sourcegraph ecosystem as of
   2026-08-23; community tools exist unlisted. This decides whether we consume SCIP for C# or write a Roslyn
   extractor. *(Flagged, [S18])*
2. **Can source-generated code be brought into the semantic model?** Design-time builds do not run
   generators, so Mapperly output, EF compiled models and the .NET 8+ DI source generator are **absent** —
   and the DI generator is precisely what would make registrations statically visible. The documented
   workaround (build first, `GeneratedFilesOutputPath`, add files manually) needs testing per generator.
   *(Flagged)*
3. **How does `MSBuildWorkspace` handle `<TargetFrameworks>`?** By default it loads only the first TFM;
   setting `WorkspaceProperties["TargetFramework"]` targets one, and multiple `OpenSolutionAsync` calls is
   the community workaround. **No definitive documentation page states this.** *(Flagged)*
4. **Can `Microsoft.Build.Sql` build a `.dacpac` without a live SQL Server?** If yes, DacFx's resolved model
   becomes available to an artifact-only pipeline and is strictly better than raw ScriptDOM. *(Flagged)*
5. **Is `bicep jsonrpc` a long-lived daemon or per-request process?** The docs say it avoids cold-start when
   compiling multiple files, implying a persistent server, without stating the lifetime. *(Flagged)*
6. **How far does offline HCL parsing get for Terraform?** `hashicorp/hcl/v2` yields labels and references
   but not provider attribute schemas; `terraform graph` needs `init`. *(Inferred)*
7. **Does ScriptDOM's grammar cover the T-SQL our repositories actually use?** It is grammar-driven, so
   genuinely new syntax lags. *(Inferred from the repo's documented fix workflow)*

## Known failure modes of this domain

- **The silent partial load.** A project fails to resolve — missing NuGet, wrong TFM — and
  `MSBuildWorkspace` continues, emitting a `WorkspaceDiagnostic` nobody checked. The graph is well-formed
  and missing a subsystem. `SkipUnrecognizedProjects = true` avoids hangs and **makes this worse** by
  excluding valid-but-unusual project types too.
- **Approximated edges promoted to facts.** A syntax walker matching `AddScoped<I,T>` produces edges that
  look identical to resolved ones. Because nothing contradicts them, a wrong `IMPLEMENTS` or `PERSISTS_TO`
  edge is permanent and invisible. Carry a confidence attribute and the matched evidence, always.
- **Cold-start cost mistaken for a per-change cost.** 1.5–4 minutes is fine once and fatal per save. Any
  design that reloads the workspace on change has a 2-second target it cannot meet.
- **Registering MSBuildLocator too late.** The failure is a confusing assembly-load error far from the cause,
  because the constraint is about *when types load*, not about call order in the obvious sense.
- **Parsing `.bicep` instead of compiled ARM.** Produces a graph with fewer edges than the compiler already
  computed, and re-implements name resolution to recover them.
- **Trusting `bicep decompile`.** Documented as lossy and best-effort, and it warns on nested templates,
  `copy` loops and conditionals — exactly the constructs whose edges matter most.
- **Treating sqlglot's silence as validation.** It is deliberately lenient; parsing invalid SQL without
  error is a feature, and a schema built from it may contain nonsense that never raised.
- **Adopting LSIF because it is Microsoft-origin and familiar.** Its own specification says it is not a
  symbol database. This is the cheapest possible mistake to avoid and one of the easiest to make.

## Disconfirming views we deliberately sought

**1. "Use tree-sitter for C# extraction — it is faster and never fails to parse."**

Tree-sitter is error-resilient, extremely fast, and has a C# grammar. It also does **no name resolution, no
type binding, and no understanding of generics, attributes or partial classes**. Concretely: it cannot
distinguish `AddScoped<IFoo, Foo>()` from `AddScoped<SomethingElse>()`, because that distinction requires
resolving what the type arguments *are*.

*How it fared:* **rejected as primary, retained as fallback.** For a .NET-first tool, Roslyn's semantic model
is correct despite the load cost. Tree-sitter's genuine place is the uncompilable project — a repository
mid-refactor, a snapshot with missing packages — where a partial structural graph beats no graph. Which
means the extractor design should support *two fidelity levels* rather than one, and mark which produced
each node. *(Verified)*

**2. "LSIF is the standard format; adopt it."**

*How it fared:* **refuted by the specification itself**, quoted: *"The data stored is result data usually
returned from a LSP request. The dump doesn't contain any program symbol information nor does the LSIF
define any symbol semantics… the LSIF therefore doesn't define a symbol database."* LSIF caches LSP answers;
it does not model symbols. SCIP is the right reference. *(Verified, [S16])*

**3. "DacFx is the only reliable SQL schema source."**

*How it fared:* **half-right, and the half matters.** DacFx is authoritative for resolved schema, but it
requires either a live database or a `.sqlproj` build. For a repository holding only Flyway-style `.sql`
migrations — a very common shape — ScriptDOM and sqlglot provide offline parsing DacFx cannot. They are
complementary: choose by what the repository contains, not by which is better in the abstract.

**4. "`bicep decompile` lets us treat ARM JSON and Bicep as interchangeable."**

*How it fared:* **rejected.** Decompilation is explicitly lossy and best-effort, warning on exactly the
constructs that carry topology (nested templates, `copy` loops, conditional resources). The direction that
works is one-way: `.bicep` → ARM JSON, and extract from the JSON.

## What this adds up to

The domain's honest summary is that **extraction fidelity is not uniform and pretending otherwise is the
failure mode**. Types and references are resolved facts. DI, routes and ORM mappings are pattern matches.
Bicep loops and conditionals are unresolved expressions. Tree-sitter output is structure without semantics.
A graph that records all of these at the same confidence is a graph that will be confidently wrong, and the
cheapest countermeasure — a confidence attribute plus the evidence that produced each edge — costs one field.
