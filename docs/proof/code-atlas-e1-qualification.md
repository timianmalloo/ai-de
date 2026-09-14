---
id: proof-code-atlas-e1-qualification
title: "Code Atlas E1 qualification: evidence and unresolved gates"
type: doc
status: draft
owner: "@timianmalloo"
tags: [code-atlas, e1, qualification, tests, evidence]
links:
  - { to: design-code-atlas-e1-static-views, rel: relates-to }
  - { to: spec-addendum-e-code-atlas, rel: relates-to }
  - { to: proof-code-atlas-real-daemon-mainwindow, rel: relates-to }
review-by: 2026-09-21
summary: >
  Held test-only E1 qualification, not product acceptance. Records the actual
  sixteen-test candidate and parent replay, missing integration boundaries,
  independent oracle/cleanup findings and unavailable historical red receipts.
---

# Qualification is held; sixteen green tests are not E1 acceptance

Owner 69 admitted two test files only. Owner 70 funded independent Test, C# and
Core Security/SRE reviews after parent replay. Owner 71 holds the candidate join
and qualification admission. The tests remain in their separate worktree; this
record does not add E1 metadata, diagrams, production code or main integration.

## Frozen candidate and observed execution

| Item | Evidence |
|---|---|
| Candidate | `020f9622457865d358780820a753a1bec0092958` |
| Author tree | `C:\Projects\ai-de-atlas-e1-test-qualification` |
| Base | `c9617fb7d6911731def65a0fadbeaaa405f53eae` |
| Changed files | `tests/AiDe.Core.Tests/Understanding/AtlasStaticReaderContractTests.cs` (153 lines); `AtlasStaticObservationTests.cs` (178 lines) |
| Scope readback | Parent Git comparison found exactly those two additions and no `src` changes |
| Author TRX | Session files: `atlas-e1-test-qualification/atlas-e1-focused.trx` |
| Independent parent TRX | Session files: `atlas-e1-independent/parent-e1-qualification.trx` |
| Actual counters | Both: 16 executed, 16 passed, 0 failed/skipped |
| Parent command | `dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~AtlasStaticReaderContractTests|FullyQualifiedName~AtlasStaticObservationTests" --logger "trx;LogFileName=parent-e1-qualification.trx" --results-directory <session-files>\atlas-e1-independent` |
| Author allowance | 24 leaves; author reports approximately 35 plus unspecified wrapper detail |

The parent read both complete test files and the actual enumerated TRX results,
then reran the candidate before review. These are **Verified execution counts**,
not a claim that each test proves its name. The approximate overrun is the
author's report, not an independently reconciled count. Further author tool use
was stopped; a repair requires a new explicit grant.

Session files above are machine-local retained evidence, not committed files.
The parent session evidence root is
`C:\Users\malla\.copilot\session-state\45bbc625-37e9-40a8-a2d0-8b39402c8e90\files`.

## Actual claim reach and oracles

| Claim | Focal path actually executed | Oracle / boundary | Confidence and residual risk |
|---|---|---|---|
| Current strict request codec refuses unfamiliar E1 fields | `AtlasReaderProjection.DeserializeSelect` with altered fixed legacy requests | Unknown property, including null, must throw | Executed characterization; not capability negotiation or real registration |
| Current selection codec rejects malformed fixtures | Actual Serialize/DeserializeSelection | Numeric/wrong-case kinds, duplicate/unknown fields | Executed; global replacement confounds the row-specific unknown-field oracle |
| Legacy capabilities fixture decodes | DeserializeCapabilities on handwritten JSON | Parsed features equal the fixture | Executed, but not evidence of advertised producer features |
| Local selection validation is request-relative | SerializeSelection then DeserializeSelection with matching and changed requests | Changed requested source limit is rejected | Executed; not actual remote retained-receipt RESTORE |
| Current producer emits observed declarations | `CSharpDeclarationObservation.Observe` over verified-buffer fixtures and actual Roslyn compilations | Selected type/member/role/span checks | Executed synthetic source association; fixture grants/native identities do not prove filesystem authority |
| No fabricated file-limited logical identity | Complete plus `Assert.All` null/reason checks | Missing an expected nonempty result oracle | Held: an empty declaration result can pass |
| Partial occurrences share logical identity | Distinct logical values plus role checks | Missing nonnull logical identity and explicit occurrence-key checks | Held: a single distinct null value can pass |
| Unsupported declaration coexistence | Operator sibling plus nested type/member fixture | Unsupported method limitation and emitted sibling observations | Executed sibling case, not an unsupported intervening-parent algorithm |
| Tiny escaped DTO roundtrip | Serialize/DeserializeSelection, byte counts and constants | Small fixture fits a size inequality | Executed smoke only; prefix incorrectly charged against body limit, no envelope maximum/one-over test |
| Local reservation queue | Four active and sixteen pending operations, cancellation and final zero | Active/pending bounds and final tuple | Executed; held charge inequality accepts zero, failure-path cleanup is incomplete |
| Q/issuer/native publication ownership | Not executed by these new tests | Matching blocked writer and exact retained/drained state required | Unverified for this candidate; earlier E0 evidence remains separate |
| E1 parent/flavor, retained opt-in, incremental metadata charge | Production seams absent | Cannot be replaced by test-only implementations | Open by the test-only grant; not implemented or accepted |

## The historical reds are not retained proof

The author confirmed that the earlier raw TRX was overwritten. Only the final
green TRX is retained. The following account is **author-reported**, not a
reconstructed receipt and not evidence that a product regression was repaired:

| Earlier failure | Correction reported by author |
|---|---|
| `JsonException: Page span must describe UTF-16 text` | Replaced an escaped string too long for its fixed four-unit span; derived UTF-8 bytes from the actual text |
| File-limited reason not found in result limitations | Checked each declaration's `UnresolvedReason` instead |
| Recovery test incorrectly prohibited `Count` | Allowed the actual recovered declaration and checked source spans |

Red-before-green for these assertions is **not recorded**. A future proof must
retain distinct red and green output paths; an assertion corrected to match
observed behavior is not automatically a regression-control demonstration.

## Independent gate dispositions

All three reviews target candidate `020f9622`. Each used four read-only leaves.
C# additionally reported two parallel wrappers. None executed a mutation or
changed the candidate.

| Lens | Verdict | Evidence-backed disposition |
|---|---|---|
| Test Architect | BLOCK | Missing actual registration/remote RESTORE/publication paths; vacuous file-limited oracle; no retained historical semantic reds |
| C# | BLOCK, advisory | Root-and-row mutation confounding; empty/null semantic oracles; disposable input fixtures and queue cleanup; overstated names; wrong body/prefix unit; unused context parameter |
| Core Security/SRE | BLOCK | Cleanup can abort before disposal; wrong-reason row rejection; held charge can be zero; tiny DTO smoke is not frame/publication-bound proof |

These are **test defects and evidence limits**, not findings of new production
vulnerabilities. Test's demand for actual future parent/flavor emission remains a
product gate: Owner 69 explicitly forbids implementing that behavior in this
test-only tranche, including in substitute test helpers. Owner must resolve the
qualification boundary; a reviewer does not expand the author manifest.

## Failure classes and controls required before admission

No canonical defect number is guessed; these named shapes await the existing
Conductor reconciliation process.

| Class | Sweep in this candidate | Derive / prevent |
|---|---|---|
| A mutation reaches several boundaries and fails at the wrong one | Both structural-property cases use global declaration-token replacement | Mutate exactly one outline row; prove the root is unchanged and the base fixture is valid |
| Universal/distinct checks pass with no meaningful values | File-limited empty collection; partial logical values all null | Assert expected occurrences, nonnull logical identity and distinct occurrence keys before relational assertions |
| An assertion aborts owned-resource cleanup | Queue pending-outcome assertions precede active disposal; observation inputs are not disposed | Drain and dispose every owned result before outcome assertions; deterministic fixture disposal including construction failure |
| A plausible number proves the wrong unit or amount | Body plus prefix compared with body limit; held charge only bounded above | Separate units and assert exact fixture charge; maximum/one-over claims need actual boundary cases |
| A test name is broader than its exercised path | Handwritten capabilities, local RESTORE codec, sibling operator and DTO smoke | Name the actual boundary; actual registration/retention/publication remain separately identified |
| Red evidence overwritten by the final green | One TRX path survives | Unique per-run artifacts, with raw outcomes retained rather than recreated |

These are required controls, **not controls yet observed failing and repaired**.

## Reuse before adding more fixtures

Parent inspected existing source while preparing the next bounded decision:
`AtlasProductionAdmissionTests.cs:237-325` uses real `AtlasRuntimeFixture` and
`AtlasRemoteReader`, file/member SELECT windows, repeated original-receipt RESTORE
and request-correlated writer/drain hooks. `AtlasIpcAdmissionTests.cs:183-278`
contains real pipe/native selection and a blocked publication writer across
revoke, expiry, deadline, partial, disconnect, completion and timeout modes.

This is **source-observed reuse potential**, not a new execution receipt. Running
the existing baseline tests can establish current behavior without duplicating
their fixtures. It cannot establish E1 incremental fields that do not exist.
Any repaired candidate still needs independent review and a parent replay.

## Remaining

- Repair the actual candidate oracle/cleanup defects under an explicit grant.
- Keep actual registration, remote retention and publication evidence distinct
  from codec/ledger smoke tests.
- Preserve SP3 long-file/page navigation and incremental new-field SP4 as open.
- Keep E1 sequence/activity, E2 domain/layer/Azure, E3 comparison and E4 AI separate.
- No E1 product acceptance, normative Addendum E, main push or programme closure.
