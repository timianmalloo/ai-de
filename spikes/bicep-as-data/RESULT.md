# Phase-3 spike — Bicep read as data, measured against the compiler

**Run 2026-08-28** · Bicep CLI 0.45.15 (oracle only) · corpus: `TheTerrace/infra/main.bicep`, 677 lines
**Re-run:** `az bicep build --file <f>.bicep --outfile oracle.json` then
`dotnet run --project spikes/bicep-as-data -- <f>.bicep oracle.json`

## The question

A contract question, not an optimisation. Phase 2 established that the product does not compile
repository-supplied input (spike D3: an `Exec` in `InitialTargets` needs nothing but a checked-in
file). **`bicep build` is exactly that** — Bicep resolves module references and evaluates template
functions at build time. If the infrastructure extractor needs the compiler, either the design
changes or the principle gets argued away, and it should not be argued away quietly.

## Result

| | Oracle (`az bicep build`) | Read as data | Missing |
|---|---|---|---|
| Resources | 24 | **24** | **0** |
| Distinct resource types | 19 | **19** | **0** |
| Parameters | 18 | **18** | **0** |
| `@secure()` parameters identified | — | **1** (`invitationPepper`) | — |

**The declarative read recovers every resource type and every parameter the compiler produces.**
Bicep can be read as data; Phase 2's no-build principle holds into Phase 3.

## What it does not recover, and why that is the honest answer

**16 of 24 resource names are literals. Eight are expressions** —
`[format('{0}-vnet', parameters('namePrefix'))]` — and only the compiler resolves those.

They are kept **verbatim** and disclosed as expressions rather than guessed. A join that needs a
literal resource name is therefore `Inferred` at best, and must say so. A guessed name would produce
a confident wrong edge between a table and a server, which is worse than an absent one: the user
would act on it.

## What was compared, and what deliberately was not

**Compared by TYPE, not by name.** Every interesting name in a real template is an expression, so
comparing an unresolved expression against a resolved one would measure the compiler rather than the
read. The type is what a join needs.

`simplify: declaration-level regex rather than a Bicep grammar; ceiling is declarations, their types
and their parameters; upgrade trigger = a join requires a resolved expression.`

## Untested

- **Modules.** This template declares none, so the module path is unmeasured — the reader has code
  for it and no evidence.
- `existing` resources, loops (`for`), and conditional resources (`if`).
- A second template (`provider-vault.bicep`) was not compared.

## Consequence

Component 1 of the Phase-3 design proceeds as specified: read `.bicep` as data, emit resources,
types and parameters, never read the value of an `@secure()` parameter, and disclose unresolved
expressions rather than resolving them.
