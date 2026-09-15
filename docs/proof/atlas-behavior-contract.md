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
summary: "Author evidence for the source and structural/page experiments, plus the granted synthetic behavior UI harness, red controls, headless state checks and limits."
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

## E1-M synthetic review harness

### Scope, reproduction and measured results

**Verified author observation, 2026-09-15:** the Human-granted
`docs/mockups/atlas-behavior-views.html` is self-contained and uses literal synthetic
facts. No parser, repository file, service or analyzed code is executed by it. It
reuses DESIGN.md and the harness template. Its design hub and rubric are
`docs/design/atlas-behavior-views.md` §12. No spike/source/package/product edit is
part of E1-M. Author evidence never clears an independent veto.

Open the HTML over `file://` and choose **Run review checks**. For headless
reproduction, add `?checks=1` to that file URI, launch the installed Edge executable
with `--headless=new --disable-gpu --no-sandbox --hide-scrollbars`, a fresh
`--user-data-dir=<temporary directory>`, `--virtual-time-budget=4000`,
`--window-size=1600,1100` and `--dump-dom`. Read the resulting `#check-results`
JSON and `#verdict`; the exit code alone is insufficient.

Executed commands from the behavior worktree:

```powershell
python C:/Users/malla/AppData/Local/Temp/atlas-e1-check.py
python C:/Users/malla/AppData/Local/Temp/atlas-e1-check.py --shots
python tools/verify-mockup-audits.py docs/mockups/atlas-behavior-views.html
python docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups/atlas-behavior-views.html --json
python docs/ai-forward-pack/scripts/design-lint.py DESIGN.md --strict
```

The temporary stdlib Python driver invokes
`C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe`, captures DOM/logs,
parses the real `#check-results` with `HTMLParser`, and fails when that JSON records
failed cases. No Playwright installation was added. The attempted existing Node
tool import failed with `The requested module './index.js' does not provide an
export named 'default'`; the repository's established Edge CLI contract supplied
the bounded dependency-free fallback.

First observed run: four negative controls rejected, 59 checks passed and one
failed. `selection-filter-source-back` reported a null `textContent`: the test
passed a CSS selector to an ID-only lookup. The selector was corrected, then actual
toolbar focus/click, graph-origin restore and filtered-endpoint cases were added.
Visual inspection found a separately clipped graph label; explicit ellipsis was
added without discarding its full accessible name or inspector/list text.

Final observed run:

```json
{"passed":63,"failed":0,"negativeControls":[
 {"name":"drop-graph-fact","reason":"graph/list mismatch"},
 {"name":"corrupt-inspector-binding","reason":"inspector receipt mismatch"},
 {"name":"remove-canceled-recovery","reason":"state recovery canceled"},
 {"name":"hide-page","reason":"zero page box"},
 {"name":"erase-filter-boundary","reason":"filtered relation endpoint absent"}],
 "audit":{"rendered":true,"box":[1280,1281],"pairCount":22,
 "contrastFailures":0,"targetCount":32,"targetFailures":0,
 "smallTargets":[],"consistencyFailures":0,"consistencyErrors":[]}}
```

The 63 normal cases comprise 32 state/view combinations, 18 theme/density/layout
width combinations, three personas, older-peer refusal, filtered list Source/Back,
graph-origin toolbar Source/Back, explicit filtered relation stub, expired-restore
setup and refusal, Arrow/Enter handling, cancel/retry and two motion settings.
Every state/view case reads the live audit. Source preview separately reads its
page/contrast/target audit. The graph-origin case focuses and clicks the toolbar
button before checking Back focus; direct helper invocation is not its substitute.
The fixed filtered fixture expects only f6 for `scheduling`, plus r3 with f5→f6
and an `outside-filter` stub. Removing that boundary marker is rejected.

The 16 states are ready, unselected, loading, empty supported, no matches, partial,
page boundary, unknown target, async/cancel/throw, malformed, unsupported, source
changed, canceled, error, overflow and restore expired. Primary identities and
demo receipts are checked across graph/list/inspector; source highlights the exact
embedded UTF-16 span. Stale Source is disabled and expired Back refuses a latest
equivalent. The 512-character overflow state retains the list and inspector while
showing an explicit diagram limit. State loops are finite fixed lists.

The existing mock sweep reported **1 mockup swept, 0 findings**. This verifies its
console/placeholder/nonzero-page controls, not the full matrix. The nonempty craft
scan reported one **Minor flat-type-hierarchy** finding: sizes 12/13/15/22 px,
ratio 1.8:1. This is retained in the design rubric for independent assessment; no
suppression or token change was used. The unchanged DESIGN.md strict lint reported
`clean - all token references resolve (0 warning(s)).`

Normal-path instrumentation now emits browser-mock render and audit elapsed times,
operation counts and primary volume without a flag. Final observed samples:
render **1.7 ms**, count **148**, seven Sequence facts; audit **0.4 ms**, count
**246**, 22 contrast pairs and 32 targets. These are single in-process samples,
including synchronous rendering/audit work, not browser paint latency or product
p95. The page labels that limit. `window.mockRender`, `window.lastAudit` and
`reviewResults.renderMeasurement` expose the actual values; missing timing is
displayed as `not recorded`.

### Headless visual evidence and limits

Final screenshots were opened and inspected by the author:

- `C:/Users/malla/AppData/Local/Temp/atlas-e1-mock/sequence-ready.png`
- `C:/Users/malla/AppData/Local/Temp/atlas-e1-mock/activity-ready.png`
- `C:/Users/malla/AppData/Local/Temp/atlas-e1-mock/activity-narrow-highcontrast.png`

The first two use an actual 1600 × 1100 browser surface; the final narrow/high-
contrast/partial image uses **1024 × 1600**, shows the ellipsis and the inspector
stacked below the list. Graph/list areas deliberately scroll; a viewport screenshot
does not claim every offscreen row is visible at once. Full JSON and DOM/log output
are alongside these screenshots as `review-checks.json`, `.html` and `.log`.
These are temporary handoff evidence, not committed portable screenshot assets.

Coverage is orthogonal, not the full Cartesian matrix. Keyboard assertions exercise
DOM event handlers; native keyboard delivery, focus adorners, UIA, Windows contrast,
DPI and WPF remain unproved. The browser system-color theme is labeled as such.
Synthetic cancellation and paging do not demonstrate parser cancellation or the
production caps. General loops/syntax, relation display order, accepted foundation,
Core receipts, DTO/wire compatibility, latency percentiles and 16 ms UI work remain
outside this evidence. No runtime ordering or executable CFG is claimed.

Class → sweep → derive → prevent: ID-only lookup misuse can make a journey oracle
fail before checking behavior; all Source selector uses were swept and corrected.
Graph-only checks can omit source focus and missing boundary evidence; both linked
origins and an erased-stub fault are now executed. Long node labels can clip while
the DOM remains consistent; the final narrow screenshot plus explicit ellipsis and
full accessible label cover that shape. Conductor owns shared lesson registration.

Execution graph: existing direction/grant → self-contained fixture/model/surfaces →
red DOM faults → fixed normal matrix → craft/token checks and actual screenshots →
proof/audit/derive → frozen review handoff. No fan-out, visible desktop or new scope.
The independent UX/IA/a11y/Test review and all product gates remain open.
