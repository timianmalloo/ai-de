---
id: proof-atlas-behavior-contract
title: "Atlas behavior contract spike evidence"
type: proof-pack
status: proposed
owner: "@timianmalloo"
tags: [atlas, e1, proof, roslyn]
links:
  - { to: design-atlas-behavior-views, rel: relates-to }
review-by: 2026-12-15
summary: "Author evidence for six synthetic Roslyn contract fixtures, retained negative controls, and explicit limits; independent acceptance pending."
---

# Evidence — 2026-09-15

## Outcome and boundaries

**Verified author observation:** the command below completed all six fixture groups
with three negative-first controls. **Flagged:** independent review, Linux
compatibility and project-coverage before/after remain Conductor obligations.
This is evidence for a source-only spike, not E1 product acceptance.

```powershell
dotnet run --project spikes/atlas-behavior-contract/AtlasBehaviorContractSpike.csproj --no-restore
```

Observed output, in order after reflected signatures:

```text
NEGATIVE-FIRST
  NEG-01 type-dedup expected=3 actual=2 oracle=FAIL-AS-EXPECTED
  NEG-02 string-location-order expected=9:1,10:1 actual=10:1,9:1 oracle=FAIL-AS-EXPECTED
  NEG-03 syntax-target-promotion text=d.Go symbol=<null> oracle=FAIL-AS-EXPECTED
FIXTURE repeated-recursive-calls status=OBSERVED occurrences=3 disclosure="static reconstruction — not observed runtime order"
FIXTURE overload-symbols status=OBSERVED occurrences=2 distinctSymbols=2
FIXTURE unknown-dynamic-dispatch status=OBSERVED occurrences=2
FIXTURE branch-loop-conditions status=OBSERVED facts=2
FIXTURE await-cancel-throw-paths status=OBSERVED facts=3
FIXTURE malformed-unsupported-syntax status=OBSERVED parseErrors=1 unsupportedGaps=1
SUMMARY fixture_groups=6 failures=0 disclosure="static reconstruction — not observed runtime order"
```

The full stdout, including exact UTF-16 spans and signatures, is in
[RESULT.md](../../spikes/atlas-behavior-contract/RESULT.md). Output reports runtime
10.0.11 and both Roslyn assemblies 4.14.0.0. `Directory.Packages.props:26` pins
Microsoft.CodeAnalysis.CSharp 4.14.0; the spike's PackageReference has no version.
Source audit of Program.cs finds only parsing, compilation construction, semantic
queries and synthetic fixture observation. There is no Emit or fixture execution.

## Reproduced failure and contract repair

**Verified before repair:** the same command printed three signatures then threw
`reflected signature not found: Microsoft.CodeAnalysis.CSharpExtensions.GetSymbolInfo`.
It never reached the negative or fixture groups. Installed XML at
`Microsoft.CodeAnalysis.CSharp/4.14.0/lib/net8.0/Microsoft.CodeAnalysis.CSharp.xml`
identifies the correct owner at member line 19589, the declared-method overload at
19907 and SourceText ParseText overload at 35722. The repaired reflection checks
print all four contracts. No fixture body needed repair.

The prior author had corrected GetDeclaredSymbol ownership and its base syntax
parameter before this continuation. Those prior failures and the earlier wrong
`tools/coord-core.py` path are reported by the Conductor; they were not reproduced
by this author. Their durations are not inferred. This continuation's audit marker
began at 20:32:12Z after initial diagnosis, so it measures the remaining work only.

## Class → sweep → derive → prevent

**Class:** contract preflight uses a guessed declaring type or overload, blocking
valid SDK calls before behavioral evidence can run. **Sweep:** all four preflight
checks were inspected; GetSymbolInfo owner was wrong, ParseText checked the string
overload although fixtures use SourceText, Create matched, and GetDeclaredSymbol
had already been corrected. **Derive:** inspect installed signatures and match the
contract actually invoked. **Prevent:** retain the failing-on-mismatch reflection
preflight and six-group terminal assertion on every normal harness run. The
observed old namespace failed that same control before the repair.

The shared defect register is outside the four-file grant; the Conductor owns
register reconciliation. Tool discovery uses the observed
`docs/ai-forward-pack/scripts/coord-core.py` path. Process correction: this
continuation's first read preceded its goal frontmatter; that ordering defect is
disclosed, not represented as compliant. The next authored audit includes the
goal and terminal condition. No automatic prevention of that process error is
claimed in this spike.

## Plan and actual scope

Goal: finish the bounded contract spike and evidence. Done when six groups have
observed results and allowed files are committed for review. Tier T1; no author
fan-out. Planned graph: inspect contracts → reproduce preflight failure → repair
exact mismatches → observe fixtures → record evidence/audit → derive → commit.
Actual: one failing preflight, one two-line repair, six completed groups; the final
recording run repeats the same unchanged fixtures to capture exact stdout.

Surface list: synthetic source → syntax/semantic model → console observations →
RESULT and this proof. Store, production service, wire, native UI and compute
reader are out of scope. DDD/data persistence is not introduced by this spike.
Smallest solution: use the centrally installed dependency and existing fixture
harness; no new abstraction, dependency or production path.

## Limits and review handoff

Source occurrences and selected syntax facts are observed, not a CFG or actual
execution. Unsupported goto is a chosen projection gap. The spike does not prove
virtual dispatch, cross-project identity, cancellation delivery, throw propagation,
snapshot validity, budgets, source navigation, or native rendering. A printed
summary is an author assertion backed by the inspected Require calls; independent
review still checks that those calls test the intended contract.

Ruling 121 grants only Program.cs, the spike csproj, RESULT.md and this proof,
plus mandated audit/derived handling. No solution, src, tests or package version
change. Core later acknowledged that CI project coverage is Windows-only; manual
Linux evidence is required and must not be called Linux CI coverage. AIDE_CONTRACT_LOG
and AIDE_SESSION are absent in this author environment; no episode event was
invented. Evidence is named in committed audit artifacts for Conductor capture.

Staging check caught a pre-existing new blank line at the csproj EOF; the blank
line was removed and the same whitespace check rerun. `coord precommit` printed
`0 staged paths - nothing to check` despite this worktree's staged candidate.
That is not coordination verification of these paths; exact claims were used for
edits and the anomalous precommit result is handed to the Conductor.
The actual commit hook subsequently printed `12 staged path(s) checked - all free
or mine`; that is the successful coordination observation for this commit.
