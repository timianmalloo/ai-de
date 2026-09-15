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
summary: "Author evidence for the retained six source groups and four Owner-frozen structural/page fixtures with independent expected tables, subject faults and limits."
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

## Structural/page experiment — 2026-09-15

**Owner option B — relation-order qualification (2026-09-15).** The experiment
qualifies relation **set equality** over identity, endpoints, kind, arm, predicate,
anchor and confidence, plus closure, directional stubs and page recomposition.
Program.cs sorts relations lexicographically by ID to canonicalize comparisons;
this is not evidence for the design's endpoint-ordinal/kind-rank/arm-index display
order. The auxiliary endpoint display key and the display-order oracle remain open.
Primary ordinal and auxiliary inventory/order checks retain their existing scope.
No source, fixture, expectation or recorded output changes accompany this
qualification, and no product gate is cleared. Independent review owns the ledger
amendment; this author does not substitute ID sorting for the missing display test.

This extension supersedes the earlier no-paging statement only for the four frozen
synthetic literals. It preserves all prior evidence and leaves product semantics,
native acceptance and production contracts open. **Verified author run:**

```text
STRUCTURAL B primary=4 auxiliary=5 nodes=9 edges=9
STRUCTURAL L primary=2 auxiliary=4 nodes=6 edges=6
STRUCTURAL T primary=5 auxiliary=5 nodes=10 edges=10
STRUCTURAL G primary=2 auxiliary=3 nodes=5 edges=5
groups=4 primary=13 auxiliary=17 nodes=30 edges=30 pages=28 recompositions=16
fault_oracle_rejections=10 distinct_subject_faults=8
```

These are the counts observed by the initial complete run, not an independent PASS
verdict. Its measured structural section took 62.756 ms including faults and console
output. Later final recording adds explicit full graph receipts and byte counts;
its own measured duration is in RESULT. Neither observation is a performance cohort.

Run from the behavior worktree:

```powershell
dotnet run --project spikes/atlas-behavior-contract/AtlasBehaviorContractSpike.csproj --no-restore
```

[RESULT.md](../../spikes/atlas-behavior-contract/RESULT.md) carries the full final
stdout: retained six source groups/three negatives, all ten subject-fault receipts,
four SHA256-pinned literals, 13 primary identities, all auxiliaries/relations,
28 page closure/stub/byte records, 16 recompositions, D5 and five invalid requests.

### Authority, construction and falsifiers

The independent expected-result author froze ledger `205d5da4` at
`docs/proof/atlas-views-plan-review.md` in the independent design-review tree.
Owner `4a81eb11ba3d190d59a2680be98be9ee0ca9238a` supplies D1-D5 over design
`83e1139b`. This author copied the four literal sources and independently specified
node/edge/closure tables into Program.cs. Direct Roslyn-only discovery established
the literal spans and ChildNodes index paths **before** graph authoring; that
temporary discovery function was removed after those values were frozen.
No fixture expectation changed in response to the graph's output.

The subject traverses only the selected method's admitted syntax, binds invocation
targets through the existing semantic model, creates owner-defined regions and
relations, then projects primary windows with region closure and directional stubs.
The expected-result adapter consumes only the fixed literal tables and explicit
per-primary closure lists. Its complete graph is independent of subject output.
Comparison includes identity, span/path, ordinal, target, predicate, confidence and
edge metadata, not counts alone. Fixed source hashes prevent drift outside the
selected method from silently changing the fixture contract.

Negative-first faults ran through the subject before structural positive reporting:

| Fault | Dependent oracle rejected it |
|---|---|
| Drop branch-region relation | B fixed relation set; recomposed relation set |
| Drop small-page cross-window relation | Exact directional stub set; recomposed relation set |
| Add page coordinates to identity | Recomposition primary identity set |
| Always refuse | D5 B3 cap-4 must publish |
| Omit B.true closure | D5 B3 cap-3 must refuse with no publication |
| Ignore auxiliary cap | Same non-tautological cap refusal |
| Traverse Local body | G exact primary identity/anchor set |
| Invent runtime edge | B closed structural relation set |

Ten dedicated OracleFailure catches are required; unrelated exceptions fail the run.
Normal D5 checks require B3 cap 3 to refuse auxiliary-first, B3 cap 4 to publish four
auxiliaries, and B1 cap 3 to publish three. Invalid limits 0/129, offsets -1/4 and
foreign observation all refuse and publish nothing. No Core receipt/token exists
in this experimental PageContent API; no production authority is claimed.

Class → sweep → derive → prevent: occurrence identity polluted by page coordinates,
lost boundary relations and pruned mandatory closure can survive aggregate count
checks. The sweep covers every fixed primary, auxiliary and edge over sizes
1/2/7/128. The rule is independent identity/evidence equality plus closed relation
sets and non-tautological cap controls. Executed subject faults demonstrate the
controls fail on those shapes. Generalization beyond these four literals is not
proved; the shared defect register remains Conductor-owned outside this grant.

### Boundaries, plan and handoff

Scope remains the four existing Ruling-121 paths plus required audit/derived files.
No csproj/package, design, product source, tests, wire, UI, new fixture path or native
run is added. Analyzed literals are parsed/semantically queried, never emitted or
executed. This adds no repository scan, live data, Bicep or external service.

Graph: frozen Owner/ledger → literal identity discovery → independent tables and
bounded subject → red subject faults → normal exact page/recomposition controls →
evidence/audit → regeneration/check → commit → independent review. Actual first
complete run reached all exact totals without changing expectations. Final recording
adds source hash checks, measured byte output and full graph receipts. The closed
fixture/page/fault lists are the termination variant; no open-ended search or fan-out.
Instrumentation begins on the normal structural path and records total elapsed ms,
volume, byte counts and actual caught failure classes. Audit marker is separate.

Remaining: independent review of this revised source, Conductor Linux and Windows
coverage timing, P0.7 semantic acceptance, production source authority and bounds,
global truncation/cap-gap states, parser stress/cancellation, unknown-target paging,
native and performance evidence. Four-literal feasibility never closes those gates.
AIDE session/contract environment remains absent; Conductor must name this existing
proof path in its valid episode capture. No forged episode receipt is written.
