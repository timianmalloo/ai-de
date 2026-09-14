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

### Known inherited sibling and repair scope

Parent subsequently read `tests/AiDe.Core.Tests/Understanding/AtlasReadBudgetTests.cs`.
Its existing `PendingAndOwnedBuffersHaveHardCeilings` method contains the same
assertion-before-active-disposal cleanup order and `Owned <= MaxOwnedBytes` oracle
as the first new candidate. It is a **known pre-existing sibling and prior-art
source**, not an additional file changed by this qualification.

Owner 73 requires this instance to remain explicit and forbids editing it under
the two-file grant. If further queue work is required, the next decision must
compare removing the redundant new fixture and repairing one authoritative
budget-test home against maintaining both. This records an option, not a grant
or an assertion that the existing test's failure paths have been executed.

### Repaired candidate awaiting complete gate disposition

Owner 72 admitted sixteen new repair leaves; the writer reports fourteen used.
Candidate `aa4926e9309ad607d9a1d846de838d0dd7c956b7` remains unjoined. Parent read
both repaired files and replayed **27 executed / 27 passed / zero failed**:
16 focused tests, ten existing `NativePublicationLifetimeOrdering` cases
(including `ownership`) and one existing real `NativeScope`/RESTORE case.
The earlier nine-mode statement came from a source excerpt starting after the
`ownership` attribute; the actual run establishes ten.

New parent receipts are under session files `atlas-e1-repair-independent/`:
`parent-e1-repair.trx`, `idle/matched-idle-drain.json` and ten
`publication/<mode>.json` files. Parent read the matched Restore, ownership,
partial, drain-timeout and completed receipts. The matching Restore records
client completion with one owner/five buffers/one active operation and 16,777,216
owned reservation bytes; after its matching publication drain it records
zero owners/buffers/active operations and the connection's 2,097,152-byte
reservation. The partial case records only four prefix bytes and retains
ownership until actual drain. This is **executed current E0 baseline evidence**,
not incremental E1 metadata proof or a CLR heap measurement.

The new retained red/control TRX has 16 executed, 12 passed and four failed.
Its errors include `Assert.Empty` against a normal partial pair, expected owned
bytes changed to zero, and two missing-row-property cases. Record each by its
actual assertion/input mutation; **four red results do not by themselves prove
the required null/empty, root-preservation or cleanup-failure controls**.
Two unsuccessful restoration runs are also retained; they are not product
failure evidence. The old overwritten receipts remain unavailable.

Owner 73 holds this repaired candidate for the fresh dispositions. All three
have now returned: Test conditional acceptance (3/4 leaves), C# conditional
advisory acceptance (4/4, two wrappers), and Core Security/SRE BLOCK (4/4).
The remaining hard predicate is complete root invariance: preserving only
`scopeToken` does not detect corruption of another root property while the
intended bad row remains present. Such root rejection can still conceal a
row-validation regression. The required comparison excludes the intentionally
mutated `outline` but preserves every other property and value.

C# also found that the `"classifierFlavor":"class"` case inserts null, not its
declared value, and that secondary task faults retain only their type name.
The reviewers recognize the source-level cleanup and nonvacuity improvements;
unexpected success/fault and partial-construction controls remain unobserved.
These are current qualification issues, not demands to implement future E1.

Conductor returned two alternatives to Owner: finish both test homes within
the two-file scope, or explicitly grant the existing budget-test file, delete the
redundant new queue fixture and repair one authoritative home. The latter is
recommended, not authorized here. No extra repair or third-file edit follows
from this record; the candidate remains unjoined.

## Reuse before adding more fixtures

### Consolidation and one-file lifetime discriminator

Owner 74 consolidated budget tests at `44c7db6d0730a22651bc05ad7c1872c13d2418f0`.
Parent replayed 31 tests successfully. The root-isolation gate cleared, but the
new drain helper retained successful reservations until every await completed.
That can prevent the fifth real admission from acquiring one of the first four
slots. FIFO early-failure ownership also remained incomplete. Those findings were
test-helper defects, not production vulnerabilities.

Owner 75 transferred only `AtlasReadBudgetTests.cs` to Core Astra in a new,
verified tree. Owner 76 preserved the failed parent reflection probe as
**AccessDenied, no runtime evidence**; it was not retried. The authorized ordinary
test runner then supplied the missing discriminator.

Candidate `7262566b49f919d0163c86f29ba6278eeb74ef85` changes only that test file.
Parent compared its scope, read the complete file, raw red/control results and
individual green outputs, and independently replayed **37 executed / 37 passed /
zero failed or skipped**. Production and the other two qualification files remain
unchanged from `44c7`. The author reports 14/16 new leaves, no wrappers.

| Claim / oracle | Observed evidence | Confidence / limit |
|---|---|---|
| Original helper cannot drain five real admissions without an outside cancellation | Preserved old helper: one test fails on timeout at `(0,4,1,58720256,0)`; recovery cancels and awaits all work, yielding four successes/one cancellation and final exact zero | Verified counterexample to the test helper; not a production allocation defect |
| Fixed helper releases before a dependent await | Five real admissions: five successes, zero faults, cancellation false and final zero; source disposes inside each successful iteration | Verified bounded five-admission control |
| FIFO owns all captured acquisitions | One deliberate release, then four drained successes and one cancellation; before/after queue tuples asserted, final zero | Verified tested FIFO route, with both tasks retained for cleanup |
| Capacity/refusal probes retain unexpectedly returned resources | Four active/sixteen canceled pending acquisitions accounted; separate successful scope/operation refusal-capture control ends at exact zero | Verified current reservation ledger, not total heap |
| Partial acquisition preserves both accounting and original exception identity | Cases with 0/1/3 operations; operation cleanup leaves only `(1,0,0,2097152,4194304)` until scope disposal, then zero; simultaneous primary/secondary case keeps both same objects | Verified injected-failure controls; no general all-fault guarantee |
| Exception-identity assertions detect replacement, not merely type equality | Same-type replacement mutant: two `Assert.Same` failures, three passing controls, exact cleanup zero throughout | Verified mutation sensitivity of exception-object preservation |

Tuple order is `(Scopes, Active, Pending, Owned, Retained)` and byte quantities
are reservation charges, not measured CLR heap.

Author evidence is retained at
`C:\Projects\ai-de-atlas-e1-budget-lifetime-repair\.artifacts\owner75-budget-lifetime`:
`red-restored/red-five-old-helper-restored.trx`,
`mutant-control/mutant-same-type-replacement.trx`,
`green-final/owner75-green-final.trx`, source snapshots, `candidate.patch` and
`committed-receipt.json`. The earlier no-receipt `--no-restore` success exit was
not counted. Baseline, red, mutant and green snapshots remain separate.

Parent evidence is in session files `atlas-e1-lifetime-independent/`, including
`parent-e1-lifetime.trx` and new `idle/` / `publication/` receipts. The 37 cases
are budget 10, static observation 5, static reader contracts 11, existing
publication modes 10 and real idle-Git/RESTORE 1. Existing native cases remain
**current E0 regression evidence**, not new E1 metadata proof.

Fresh Test, C# and Core Security/SRE reviews are pending against this exact pin.
No candidate join or qualification acceptance follows from the parent replay
alone. Future parent/flavor emission, negotiated opt-in, actual feature payload
adoption, long-file navigation and incremental E1 charges remain outside these
test-only controls.

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
