---
id: proof-code-atlas-e1-qualification
title: "Code Atlas E1 qualification: evidence and unresolved gates"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [code-atlas, e1, qualification, tests, evidence]
links:
  - { to: design-code-atlas-e1-static-views, rel: relates-to }
  - { to: spec-addendum-e-code-atlas, rel: relates-to }
  - { to: proof-code-atlas-real-daemon-mainwindow, rel: relates-to }
review-by: 2026-09-21
summary: >
  Owner 78 accepts the bounded current-test qualification after independent
  gates and a joined 37-case replay. Preserves rejected intermediate candidates,
  actual lifetime counterexamples and missing E1 product contracts. The
  2026-09-15 native recovery remains unqualified after restored-source UIA and
  legacy geometry failures; this does not revoke the earlier Core qualification.
---

# Current-test qualification accepted; E1 product behavior remains unproved

Owner 78 admits the test-only qualification chain after the final independent
clearances. Conductor joined it and executed the same 37 selected cases:
37 passed, zero failed/skipped. The aggregate diff contains exactly the three
authorized test files and no product changes. Earlier holds and failed candidates
below are historical evidence, not current acceptance statements.

| Joined change | Original candidate | Conductor commit |
|---|---|---|
| Initial characterization | `020f9622457865d358780820a753a1bec0092958` | `2a05bbaf` |
| First repair | `aa4926e9309ad607d9a1d846de838d0dd7c956b7` | `3ca57f89` |
| Budget-test consolidation | `44c7db6d0730a22651bc05ad7c1872c13d2418f0` | `b79e662c` |
| Lifetime discriminator and repair | `7262566b49f919d0163c86f29ba6278eeb74ef85` | `b91d4bb5b0f59352b7f90b94e0b2733a647452dc` |

The joined TRX is retained under session files
`atlas-e1-qualification-joined/joined-e1-qualification.trx`, with independent
`idle/` and `publication/` diagnostics. Its five-admission case has five
successes, no cancellation/faults and final zero. Partial/simultaneous failure
outputs preserve original exception identity and exact residual/final accounting.
The parent read those actual outputs before closing the qualification node.

GATE current test-only qualification - Owner 78 + final independent Test,
Core Security/SRE and C# dispositions - ACCEPTED within the recorded boundary.
This does not implement E1 metadata or diagrams, grant main integration, make
Addendum E normative or complete the programme. Owner's next Core authoring
grant is separate from this acceptance.

## Initial frozen candidate and observed execution (historical hold)

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

All three final reviews have returned against that exact pin:

| Reviewer | Inspected source / execution | Verdict / actual reported usage |
|---|---|---|
| Test Architect | Budget tests 1-180, 181-339; parent and author green, old-helper red and replacement-exception control receipts | Conditional PASS for current test-only lifetime qualification; 3/4 leaves, one wrapper |
| Core Security/SRE | Budget tests 1-170, 171-339; parent budget outputs, old-helper red and replacement-exception failures/cleanup | PASS; final resource-lifetime veto clears; 4/4 leaves |
| C# | Budget tests 1-190, 191-339; parent green, old-helper red and replacement-exception receipts | PASS advisory; no remaining scoped C# repair; 4/4 leaves, two wrappers |

GATE current test-only qualification - final candidate `7262566b` - all reported
current root-isolation and lifetime predicates clear. The reviewers did not
reopen accepted row controls or claim that reservation counters measure heap.
Parent replay alone did not clear these gates; the independent dispositions did.

At this review checkpoint, Owner's test-chain join and the next bounded E1 grant
were requested, not assumed. Owner 78's later join is recorded at the top.
Future parent/flavor emission, negotiated opt-in, actual feature payload adoption,
long-file navigation and incremental E1 charges remain outside these test-only
controls. They need their own production implementation and evidence.

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

- The current qualification repairs and joined replay are complete within scope.
- Keep actual registration, remote retention and publication evidence distinct
  from codec/ledger smoke tests.
- Preserve SP3 long-file/page navigation and incremental new-field SP4 as open.
- Keep E1 sequence/activity, E2 domain/layer/Azure, E3 comparison and E4 AI separate.
- No E1 product acceptance, normative Addendum E, main push or programme closure.

## Native recovery qualification — 2026-09-15: blocked, not frozen

Session `atlas-e1-native-class-view`, agent
`copilot-astra-native-e1-recovery`, retained worktree
`C:\Projects\ai-de-atlas-e1-native-class-view`, branch
`atlas/e1-native-class-view`, base HEAD
`4a5812044a38a6fe365fb7929104f51dbc973fc5`.
No replacement implementation, merge, rebase, push, destructive Git restoration,
dependency change or inspected-user-repository mutation was performed.
The original five product/test files survive byte-for-byte. No qualified
candidate commit was made: the required restored scoped green was not obtained.
Independent parent gates remain required; this record is not acceptance.

### Evidence, including failed controls

All native TRX files below are in `.artifacts/atlas-e1/`. Names containing
`green` were chosen before execution and **do not describe their outcome**.

| Receipt | Executed / passed / failed / skipped | What it establishes |
|---|---|---|
| `e1-native-proof.trx` | 26 / 26 / 0 / 0 | Original retained checkpoint, untouched |
| `e1-recovery-baseline.trx` | 71 / 71 / 0 / 0 | Initial ordinary reader/native replay |
| `e1-mutation-parent-red.trx` | 1 / 0 / 1 / 0 | Direct-parent comparison mutation is detected |
| `e1-mutation-window-red.trx` | 1 / 0 / 1 / 0 | Global source-window mutation is detected |
| `e1-recovery-final-green.trx` | 111 / 106 / 5 / 0 | Failed restoration attempt: stale compiled mutant plus missing legacy receipt environment |
| `e1-recovery-restored-green.trx` | 111 / 108 / 3 / 0 | Non-incremental restored build: native UIA/Back and legacy geometry failures |
| `e1-restored-scoped-control.trx` | 71 / 70 / 1 / 0 | Original focused scope still fails native 1180-DIP UIA lookup; expanded-suite concurrency is not a sufficient explanation |

The 71-case filter is
`FullyQualifiedName~AtlasReader|FullyQualifiedName~AtlasStatic`.
The 111-case filter additionally includes
`FullyQualifiedName~AtlasSharedHostAdmission|FullyQualifiedName~AtlasDaemonMainWindowProof`.
Project: `tests/AiDe.App.Tests/AiDe.App.Tests.csproj`, Debug,
SDK pin `10.0.303`, `net10.0-windows`.
The restored rebuild used `dotnet build --no-restore --no-incremental
-p:BuildInParallel=false`: zero warnings and zero errors.
Subsequent restored runs used `dotnet test --no-build --no-restore`.
The legacy proof received `ATLAS_PROOF_RUN=e1-recovery-20260915-qualification`;
its raw evidence is under
`artifacts/atlas-real-daemon-window-proof/e1-recovery-20260915-qualification/`.

### Discriminating mutations and exact restoration

The existing `tools/mutation-replay.py` was inspected but not used for mutation:
its restore path calls `git checkout --`, which cannot preserve this dirty,
partly untracked candidate. Before mutation, all five complete files were copied
to `.artifacts/atlas-e1/recovery-source-backup/`. Each injected file was restored
in `finally` from its exact backup, and all five SHA-256 values were compared.

| Fault | Oracle and observed semantic red | Restored control |
|---|---|---|
| `AtlasStaticViewProjection.cs:65`: direct-parent `==` changed to `!=` | `Projection_DuplicateNamesRemainOccurrencesWithDirectParentOnly`, test line 19: expected `m2`, actual `m1` | Original bytes restored; this test passes in subsequent restored runs |
| `AtlasReaderView.cs:238`: declaration window start changed to `0` | `FarMember_UsesIssuedGlobalUtf16Window`, test line 192: expected `40000`, actual `0` | Original bytes restored; this test and source-content checks pass after forced rebuild |

No mutation score or exhaustive fault coverage is claimed. The two faults ran
separately, with a parseable one-test/one-failure TRX required for each.
An important qualification-harness defect was observed: `Copy-Item` preserved the
backup's old modification time, so the immediate incremental build retained the
mutant assembly although the source checksum was correct. Source time was
`2026-09-15T05:15:01.3003403Z`, whereas the mutant App DLL time was
`2026-09-15T14:11:16.4408389Z`. A non-incremental rebuild removed the mutation
failures. Byte restoration alone is not a rebuilt-binary proof.
The first repair pass removed stale-binary and missing-environment failures;
the subsequent focused control did not clear the newly exposed native failure.
No production repair was attempted on an inferred root cause.

### Unresolved findings and exact seam

- **[Blocker] (Verified) Test/qualification gate:** restored focused execution is
  70/71. `AtlasStaticCompositionTests.cs:250` fails to find the named UIA button
  in the 1180-DIP journey, called from line 125. The expanded restored run also
  observed `Class` expected / `Source` actual at line 160 after Back. The fixture
  awaits `InvokePattern.Invoke()` and then reads `CurrentOperation`; asynchronous
  click-dispatch completion is a plausible missing synchronization boundary,
  **not a verified root cause**. Establish the actual event/publication boundary
  and a deterministic failing control before changing the scoped test/product.
  Escalation: parent Test Architect / distributed-systems reviewer.
- **[Major] (Verified) Legacy geometry seam:** the restored legacy
  `AtlasDaemonMainWindowProofTests.cs:431,442,445` feeds global source offsets
  into the member-local AvalonEdit document. Execution throws for offset 48 in
  a document of length 26. Its source/highlight measurement must agree with the
  returned page and rebase global highlights, as the new native helper does.
  This file is outside the five-file grant and was not edited.
- **[Major] (Flagged) Native UX handoff:** actual 1440-DIP and local-15-DIP
  screenshots show the selected `Compartments` tab as pale text on white.
  Numeric contrast was not measured; full-label geometry does not establish
  selected-tab readability. The relevant native construction is
  `AtlasStaticView.cs:31-35,62-68`. UX must determine and qualify the smallest
  local correction, without changing shell/chrome.
- **[Minor] (Flagged) Observability handoff:** `AtlasStaticView.cs:161-162`
  emits formatted `Trace.TraceInformation`, not a demonstrated trace-correlated
  structured event. SRE must determine the local-convention disposition and
  missing runtime evidence; no new logger/dependency was introduced here.

Shared coordinator request:
`req-01M2JPT423KF50YT0DYJ6WE6NH`, addressed to
`copilot-atlas-recovery-b0d0`, path
`tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs`.
It reports the ungranted geometry seam and native failures before any such edit.
Shared liveness and requests were re-read at the qualification boundary.
The recovery is stopped rather than widening scope or retrying to manufacture
a green receipt.

### Actual environment and visual coverage

The successful baseline's three real-daemon native receipts observed
1180/1280/1440 by 900 DIP, **native DPI 144 and WPF DPI 144**,
`HighContrast=false`, `ClientAreaAnimation=true`. The 1180-DIP case used the
repository light-token override; the other two used dark resources.
OS text scale was not changed or measured as a numeric setting.
The separate larger-label fixture used a local `FontSize=15`, **not OS scaling**.
No other hardware DPI, monitor transition, high-contrast-on, animation-off,
screen-reader or physical keyboard-input coverage is claimed.
UIA invocation plus routed Enter is not a hardware Enter test.

The baseline receipts assert current-page 128+23 paging, honest `OutsidePage`
parents, source/member token parity, retained request preference, Back state and
focus, stale restore refusal, CRLF/non-BMP input and a far source span starting
at UTF-16 36590. They record glyph and source/highlight rectangles within the
measured viewport. These are successful observed journeys, not a blanket claim
that the later nondeterministic failures are harmless.

Actually displayed and inspected during this recovery:

- `native-1440-20260915140910057-9ddb2864ec164439939ce546875bde7e/class.png`,
  SHA-256 `633586C6182C5058016435B504320D859A894D08680F7079EBA223BD0DF701EE`.
- `states-0f11107e1a1546e6ac48b2c2fcb811a1/long-labels-15dip.png`,
  SHA-256 `6B26DC48CF4DEF5CD6EDBB15669F48DE83DBCD8D4E0C6E770291D6A8F094AA20`.

Both are relative to `.artifacts/atlas-e1/`. Requested 1180-DIP class and
1280-DIP far-source image views were not supplied by the tool because of its
one-image limit; their JSON geometry/capture receipts were read, but those
image pixels are **not** claimed inspected. The original capture folders and
raw passing receipt remain intact.

### Checksums for retained source and raw evidence

| File under its granted product/test directory | SHA-256 |
|---|---|
| `AtlasReaderView.cs` | `48408746563FAB04AFB62D0C86E9EEFAAF2AE4AF337A8C75DE5A4654824BD187` |
| `AtlasStaticView.cs` | `48BA20BC178356CBE6C590A6E36D02AC148FAB0E3BDDEADA228D36FEBA54D6F1` |
| `AtlasStaticViewProjection.cs` | `3746CC81D13048AE8292CB1E2A025B9F197EB2A59D58C69BDC450FEF2D995B62` |
| `AtlasStaticViewTests.cs` | `7A4D5FBD71D5FFFEC760793A6CB56111E56A61965D9D53E13A8BB44C6BCD786C` |
| `AtlasStaticCompositionTests.cs` | `742AEF4A9E73188D3AD140420FADED6A5D6DBAA308F9FA2F2660D290565E5FA5` |

| Receipt | SHA-256 |
|---|---|
| `e1-native-proof.trx` | `3AB8DC6F7FFF65F039283167D0C47F5FA1651AA60913D8E9FAAC20227E2EA439` |
| `e1-recovery-baseline.trx` | `497F1923A14699AB1AE6AD261F6897D290AED838B8B5E760AD1806404A188CE3` |
| `e1-mutation-parent-red.trx` | `1DA3F71713660C70430DAB1DC14EBFB2EBA4FD0F45A63FE15032C0D1765449B2` |
| `e1-mutation-window-red.trx` | `D2ED7B5BC6C28992B37FDC284B7DA206DD959AF717BED39ACA835B871DFB0CCB` |
| `e1-recovery-final-green.trx` | `C9955B76BB8CA4849BA93DA1115FBDE6656420ABB970679D3FD377478D0296FF` |
| `e1-recovery-restored-green.trx` | `1A00665BB4D384008E7C9E77D6038FFC5D9DEC526C4DF4F83D8F835864CE734F` |
| `e1-restored-scoped-control.trx` | `049609BFB6CF3F1BA1EB05B5D9CEF3A7F8A1E7689A6629EABE0D5307E422658B` |

Post-rebuild assembly hashes: App
`149D9BAA1066CC0D2E5B076471EDB090526A84634639909572B1A6996794FBAC`;
Core `3CD54A20EDB4AA70094CF934491E990DAE95FDE55FA455F50413370F44268767`;
Daemon `A1829F5087F0530F96E9F3B25D5EF40792E86AEEFDF3FE9D7978361D6C6BFEAB`.

Definition-of-done is not met: restored native determinism, complete visual
inspection, native readability, independent Test/UX/UML/C#/SRE clearances,
instrumentation qualification and a passing frozen candidate remain open.
The inherited 497/497 bounded Core/fixture acceptance is input, not UI acceptance.

## ND narrow repair phase — prospective 24 calls, still not frozen

The closer authorized a separate prospective 24-call phase, same writer, tree,
five-file grant and T2/fanout 0. It started at `2026-09-15T14:30:57Z`.
This is not a reset of the preceding exhausted 40-call qualification.
The checkpoint was sent directly to closer
`ed7d1cd1-d8e4-437a-83bc-f39eb926c352` at call 6, with substantive causal findings
at call 13. No legacy geometry-test permission was received or exercised.

### Distinct hypotheses and executed causal controls

The historical 1180-DIP null lookup happened before selection and therefore
cannot be explained solely by Back/restore completion. The historical 1440-DIP
Back failure had a provider-return record but no completed restore before its
assertion. Diagnostic instrumentation separately records WPF loaded/visible/
enabled state and dimensions, actual versus requested HWND, UIA process/handle,
button names and bounds, first lookup, provider return and routed Click.

All nine invocations in the three diagnostic real-pipe journeys recorded
`ClicksObserved=0` at provider return; the actual routed Click followed.
**Provider return is demonstrably not a routed-click completion barrier.**
`Invocation_ProviderReturnedBeforeClick_RemainsPending` controls that boundary
without timing guesses: the injected provider returns immediately, the routed
Click is withheld, and the invocation must remain pending. It failed at
`AtlasStaticViewTests.cs:210` against provider-return-only semantics.

The repair is confined to the test harness:
`AtlasStaticCompositionTests.cs:247`, `InvokeAndObserveClickAsync`, registers a
`TaskCompletionSource` before invocation, awaits the Click with a five-second
failure timeout, and detaches in `finally`. Callers then await the reader's
actual `CurrentOperation`. No sleep, retry-until-green or idle-as-operation
completion was introduced. The existing list's routed Enter remains synchronous
event dispatch and uses the actual reader operation afterward.

For the 1180 predicate, first `FindFirst` now precedes diagnostic enumeration;
that original result remains the assertion target. The post-census lookup is
diagnostic only, **never a fallback**. Both were found in the observed ND runs.
This does not establish the cause of the historical missing element, and no
selector/concurrency repair or root-cause clearance is claimed.

### Rendered contrast: separate measured defect and local repair

The runtime observation records effective foreground, opacity, value source,
template source and serialized actual template, named painted parts, and exact
rendered glyph-interior/background pixel coordinates.
The observed native `DefaultStyle` template contains `innerBorder` with literal
`#FFFFFFFF`; its selected trigger raises opacity to 1. The outer `mainBorder`
uses a background template binding. Both actual border backgrounds reported
`ParentTemplate` value source. Thus resetting the parent background cannot
correct this painted white layer.

- Dark 1280/1440: actual glyph `#FFE4E9EF`, background `#FFFFFFFF`, opacity 1,
  contrast **1.2208762543898204:1**.
- Light 1180 control: actual glyph `#FF1A1F26` on white,
  **16.562753363157324:1**; the dark-token hypothesis is not applied to light.
- A rendered contrast assertion failed for both dark journeys in
  `nd-predicate-red.trx`, then passed after the repair.

`AtlasStaticView.cs:92-105` now creates its two presentation tabs through
`CreatePresentationTab`. On load it overrides only the observed template part's
Background with the existing `SurfaceSunkenBrush` dynamic resource. The native
template, selection, keyboard, focus and disabled triggers are not replaced.
The bounded template-part assumption is explicit at line 98; the rendered
pixel gate is the control for drift, not a promise about other Windows themes.

Measured after repair: dark background `#FF0D1014`, **15.61815863664934:1**;
light background `#FFE7EBEF`, **13.82399654695408:1**. The selected Compartments
header is measured; both tabs receive the local override, but this is not a
complete selected/unselected/hover/disabled/high-contrast state-matrix proof.
Hardware DPI and OS text-scale limits from the earlier section remain.

Pre-repair template/paint receipts:
`native-1280-20260915143837953-71413dcfa78e4ae4a8311b034e564381/receipt.json` and
`native-1440-20260915143842397-a9d18bb4c4564bc7bdf4a325aef6d692/receipt.json`,
relative to `.artifacts/atlas-e1/`.
The repaired 1440-DIP capture was actually displayed and inspected:
`native-1440-20260915144146715-0731cd722c26457289213232bf7c885e/class.png`,
SHA-256 `A968DA13D45C9F4354E041185FFFD02F878684F3FBB51861622FEC179A038F0B`.
The selected header is now visibly legible on the dark fill.

### Retained stale actions, with a single-fault discriminator

`RetainedActions_AfterInvalidation_DoNotRequestOrResurrectSource`
(`AtlasStaticViewTests.cs:216-264`) retains the old compartment button and list,
with a real source/binding/highlight and selected occurrence. It cancels the
synthetic lease and unloads the reader through the real Window content lifecycle.
It then raises old routed Click and Enter actions, deliberately bypassing normal
disabled-control input filtering. It asserts unchanged SELECT count, empty
source/highlights, null binding and null current selection.
This proves the tested **cancellation plus unload/deactivation** path; it is not
misrepresented as a still-mounted real remote-revocation or hardware-input proof.

Single fault: remove only `ClearPresentation()` from `AtlasReaderView.Deactivate`.
The retained action caused SELECT count **3 instead of 2**, failing the assertion
at `AtlasStaticViewTests.cs:257`. Thus the test executes an old callback capable
of producing the prohibited request when the control is absent.
The mutation was restored from the exact new-candidate backup in `finally`,
then a non-incremental build and the focused suite were run. This is a new
post-hoc mutation control, not a relabelling of the historical three TDD reds.

Both original `.artifacts/atlas-e1/recovery-source-backup/` and original 26/26
proof remain intact. New candidate backups are separately retained in
`.artifacts/atlas-e1/nd-pre-mutation-source/`. All five post-restoration file
hashes matched those backups; no mutant is left in source or the rebuilt DLL.

### Runs, current blocker and stopping decision

All TRX files below are under `.artifacts/atlas-e1/`.

| Receipt | Executed / passed / failed | Meaning |
|---|---|---|
| `nd-diagnostic.trx` | 3 / 3 / 0 | Real native event-order and paint observations; not root-cause clearance for historical lookup failure |
| `nd-predicate-red.trx` | 5 / 2 / 3 | Causal click-barrier assertion and two actual dark-contrast assertions fail |
| `nd-targeted-green.trx` | 5 / 5 / 0 | Demonstrated barrier/fill repair plus retained-action control pass |
| `nd-stale-action-mutation-red.trx` | 1 / 0 / 1 | Missing deactivation clearing permits an extra SELECT |
| `nd-restored-scoped.trx` | 73 / 72 / 1 | Restored non-incremental build, focused reader/native regression; not green |

The remaining observed failure is **list focus after Back** in the 1440-DIP
journey, `AtlasStaticCompositionTests.cs:191`:
`DeclarationList.IsKeyboardFocusWithin` was false. The immediately preceding
mode, page 128 and occurrence-token assertions passed. This is not the old
provider-return barrier assertion and is not silently classified as concurrency.
Its cause requires a distinct focus-owner/activation/publication discriminator.
Raw failing native evidence:
`native-1440-20260915144258492-c5f754e5afc64f5ebd706b0da1ec9fbd/receipt.json`.
No rerun was performed to manufacture a green receipt.

Named repair pass 1 reduced the three targeted failing assertions to zero.
The subsequent restored regression exposed the separate focus predicate.
The phase stops for closer disposition rather than make a second speculative
repair within the closing budget. No full 111-case cohort, legacy-test edit,
freeze, commit, merge, rebase or push occurred.
The sixth-file permission request remains
`req-01M2JQMKGC16MV3TH3XK04JYPD`. Final independent gates and any later combined
native/main-tree qualification remain open.

Recurrence/control recorded here under the narrow grant: a provider-return
checkpoint substituted for event completion; parent tokens did not control a
native selected-template paint layer; byte restoration needed an explicit
rebuild; disabled-state checks needed retained-action fault injection.
No broader notes or defect-register edits were made.

### Exact ND hashes

| Candidate file | SHA-256 |
|---|---|
| `AtlasReaderView.cs` | `48408746563FAB04AFB62D0C86E9EEFAAF2AE4AF337A8C75DE5A4654824BD187` |
| `AtlasStaticViewProjection.cs` | `3746CC81D13048AE8292CB1E2A025B9F197EB2A59D58C69BDC450FEF2D995B62` |
| `AtlasStaticView.cs` | `066C6895E4D13AFFFDE152E00F2E65D5A8DF9DDCEF833F5320B046E7C477B297` |
| `AtlasStaticViewTests.cs` | `68DF175AE7D947D3C3CCE9787E2DC9E258AF2DF7473C05D8E6A4C59BAB7E3092` |
| `AtlasStaticCompositionTests.cs` | `6EB9E1BFFFB1992483A9D423D67F0AE6D2F86A50257FED6282376DBBAB7C80B4` |

Rebuilt App DLL:
`56987BEC5CB5C60E16EC99CD2E30D7D65F91E512EF37BFD55FECE66E3D25537E`.
Rebuilt App.Tests DLL:
`B4C6C0245E20C2A5B05C5D077429F6B6FCFF9DC29E654E38058BF9C477721F79`.
Mutated Reader hash (not the restored source):
`A31F738AE0D32DA5D49B601639F34C8E1ECDAA040EF95B71DB63E7EA4364454C`.

| Receipt | SHA-256 |
|---|---|
| `nd-diagnostic.trx` | `BF2007951C7C34DEAB0C9A9E0C93DBCFEFDD61305DD61EBDBD6A252CBAC41E1D` |
| `nd-predicate-red.trx` | `D4A6E17EC2A33E132B86201F036420DF0E6C73078AADA12715BF664118BCA240` |
| `nd-targeted-green.trx` | `07BD365C0C0099407433CB1BD1360FADADED5C762F3A176B7C2902D6391C0EB2` |
| `nd-stale-action-mutation-red.trx` | `0DCE36074E8F7093CE1E8C986A09E94B56A783EB5D17E926BA765A94A882EFF7` |
| `nd-restored-scoped.trx` | `E49729B4F9BA7C5C2631CCE968A81C1A37CBDE8CC937D83BCC5A0FA8BDE72AE4` |

## NP6 paint-policy correction and focus evidence

Owner prospectively authorized six calls, separate from ND and the original
qualification. Start: `2026-09-15T14:55:34Z`. Scope remains the five native files
and existing proof/audit. The legacy geometry carve-out is not executable until
the responsible owner acknowledges it; no legacy edit is made.

**Correction to ND's paint implementation:** the measured contrast defect and
pixel improvement remain evidence, but ND's `TabItem.Loaded` workaround was
rejected by the lifecycle guard. NP6 removes that new hook. A private native
`TabItem` specialization applies the same bounded paint override from
`OnApplyTemplate`, after `base.OnApplyTemplate`, and explicitly retains the
existing implicit `TabItem` style through a resource reference. The default
native template, keyboard, selection, focus and disabled triggers are retained.
The numeric rendered gate remains; admitting a native `TabItem` subtype is not
permission to weaken the contrast assertion.

No focus repair is presumed. Source readback establishes that `GoBackAsync`
awaits Restore before calling synchronous `RestoreViewState`; the latter calls
the static view's focus restoration directly. NP6 adds observations immediately
before both Back focus assertions: actual keyboard element/type/name and owning
HWND, logical focus type, owned-window activation, list/reader/window focus,
current-operation state and restored presentation/page/occurrence/status.
These are current observations, not reconstructed evidence about historical
failures. The 1180 lookup cause remains unestablished.

This section is registered before execution. NP6's targeted receipt is
`.artifacts/atlas-e1/np6-targeted.trx`; actual focus/paint records stay in each
new native `receipt.json`. The closing `implement-atlas-np6-paint-focus` audit
records the observed outcome and hashes, not an assumed green here. Existing
numeric contrast and retained stale-action assertions, original raw 26-case
proof, separate mutation reds and backup hashes remain preserved.

**Process correction:** a decision request is a gate. End the turn after sending
it; do not act on older queued continuation while the requested decision is
unanswered. NP6 consumes the queued rejection before editing. Exact 300-second
leases and explicit checks precede this edit, and leases are released before
long tests. No continuity is inferred from merely retaining the same identity.

## NA10 A-only coordinate-frame repair: committed red checkpoint

Ruling 115 and the responsible current Core/Design owner's resolution of
`req-01M2JQMKGC16MV3TH3XK04JYPD` explicitly acknowledge only the legacy proof
test's coordinate-frame computation. The native writer does not execute B-E.
All expected values, readability/geometry assertions, product MainWindow/Core,
existing failure baselines and final independent gates remain unchanged.

The failing oracle already exists in separate earlier commit
`a8e09f355e6785e7b0f24b009cc64e7f9785fc54` and is unchanged in WIP checkpoint
`7c8cecafe5858965b4c7cf415df2e0aedabd6d8c`.
`AtlasDaemonMainWindowProofTests.cs` SHA-256 before the fix:
`C9827383FB6A4375DDD225817F4EC0587E9742E2D52043F9317B789DA7813CB5`.
An executed Git diff against that WIP commit reported no test-source change.

**Red observed before any fix:** explicit non-incremental daemon and test builds
both passed with zero warnings/errors. One fresh owned shown-window run executed
the committed
`MainWindow_RealDaemonReplacement_AcknowledgesHealthyReleaseAndPreservesBorrowedClient`.
It failed exactly at `MeasureReading` (`SourceRect`, lines 431/442): global/full
source offset **48** was passed into a member-local document of length **26**.
This is the admitted semantic coordinate failure, not setup, cleanup, a
manufactured expected-value change or a newly introduced oracle.

- Raw TRX: `.artifacts/atlas-e1/na10-red.trx`, one executed/failed, zero skipped.
  SHA-256 `0DBF9E11F107F86697DF699D90AFC3579C7A39CE34D06807592EEEDD7AF750AB`.
- Raw output: `na10-red.stdout.txt`, `na10-red.stderr.txt`, and
  `na10-red-run.json`, under the same directory.
- Native receipt/capture directory:
  `artifacts/atlas-real-daemon-window-proof/na10-red-ee829bcff7494bfcb0d80bdcf80fb842/`.
- Actual dotnet test-runner PID **18120**; start
  `2026-09-15T15:42:34.3285093Z`, end `2026-09-15T15:42:38.7154695Z`, exit 1.
  This PID is the runner, not an invented GUI-host PID.
- Shared run-start/end record: `req-01M2JVQXWGJ7T8CPZA0AJDESD4`.
  The end announcement was delayed by a PowerShell expression error after the
  runner had already exited. It was then explicitly resolved; no second red
  run occurred. The JSON retains the precise UTC instants.

The frame mismatch is established from both source and execution. The caller
passes the actual full source and issued highlight spans. `MeasureReading`
currently traverses every line of that full source and uses global highlight
offsets directly, although AvalonEdit now holds the returned bounded page.
The proposed repair binds the frame to the actual returned page, checks that
its full-source substring equals both returned and rendered text, and subtracts
only the page start from issued highlight offsets. No clamp, expected-value
change, smaller viewport or weaker readability threshold is permitted.

This evidence section is committed **before** that coordinate-only repair.
The raw receipts remain unstaged, and the original 26-case/NP6/mutation evidence
and backups remain untouched. This red checkpoint is not qualification.

## NQ18 bounded qualification checkpoint — 113/113, residual gates open

NQ18 started at `2026-09-15T16:19:34Z` with 18 prospective calls, separate
from earlier allocations. It did not restart NA or rerun its accepted
coordinate-frame repair. The starting pin was
`df4ad2892fd6b6b98642373a8b9d04e1bd34a994`.

### Single-fault proof, with source and binary restoration

The only permanent code change is one additional token equality assertion in
`FarMember_UsesIssuedGlobalUtf16Window`, checking the actual issued declaration
token passed to the request. No expected value was relaxed and no product
repair was made.

| Control | Single fault and failing assertion | Evidence |
|---|---|---|
| Back mode | Force `GoBackAsync` to set Class instead of the saved mode; expected Source, actual Class at `AtlasStaticViewTests.cs:153` | `nq18-back-mode-red.trx`: 1/1 failed; restored `nq18-back-mode-restored.trx`: 1/1 passed |
| Token routing | Replace only `ReaderRequest`'s declaration token argument with null; expected `"far"`, actual null at `AtlasStaticViewTests.cs:193` | `nq18-token-route-red.trx`: 1/1 failed; same assertion passes in final 113-case run |

The exact selected tests and `AtlasStaticTestHost.RunAsync` were opened before
running them while integration held the desktop: they construct an STA
dispatcher and reader, **not a Window, shown surface, daemon or UIA client**.
Both mutations were restored in `finally` from separate complete backups.
Reader SHA-256 after each restore:
`48408746563FAB04AFB62D0C86E9EEFAAF2AE4AF337A8C75DE5A4654824BD187`.
Each trusted subsequent green followed a non-incremental test-project rebuild.
No mutant was retained across the desktop wait.

The earlier meaningful offset, duplicate-name/direct-parent and stale-action
mutation receipts remain reusable evidence. They are not relabelled historical
TDD or counted as new NQ18 executions.
**Back-page, Back-focus and off-page-parent single-fault discrimination remain
unexecuted.** Passing real journeys cover their assertions, but do not fill
those requested mutation gaps. No mutation score or complete fault coverage is
claimed.

### One predeclared final scoped run

After the integrator's explicit release and closer's grant, NQ18 ran exactly one
restored native/reader/shared-host/legacy cohort:

```text
FullyQualifiedName~AtlasReader|FullyQualifiedName~AtlasStatic|
FullyQualifiedName~AtlasSharedHostAdmission|FullyQualifiedName~AtlasDaemonMainWindowProof
```

The prior 111-case selection plus two added tests produced **113 executed,
113 passed, zero failed/errors/skipped/timeouts/aborts**. This is a native scoped
cohort, not the whole application suite or a new-main combined-tree gate.
The actual legacy receipt also reports `Completed=true`, `FailureCount=0`.
Explicit daemon and test-project non-incremental builds passed with zero
warnings/errors. No retry-to-green occurred.

- Raw TRX: `.artifacts/atlas-e1/nq18-final-scoped.trx`.
- All 113 full test names, outcomes, start/end/duration and error fields:
  `.artifacts/atlas-e1/nq18-final-named-results.json`.
- Full stdout/stderr: `nq18-final.stdout.txt`, `nq18-final.stderr.txt`.
- Run, source/binary hashes and exact process observations:
  `nq18-final-run.json`; native state/focus/paint/capture observations:
  `nq18-final-observations.json`, under the same artifact directory.
- Legacy raw receipt:
  `artifacts/atlas-real-daemon-window-proof/nq18-final-aec9f149bd894c818472dd56b81ef513/receipt.json`.

Actual dotnet runner PID **31464**, start
`2026-09-15T16:24:42.4671421Z`, end `2026-09-15T16:25:32.3295201Z`, exit 0.
Shared start/end request `req-01M2JY52RW7C6XPZA2528RK0XR` was resolved with
desktop release. Actual runtime PIDs decoded from receipts were
`3796,16396,26376,27876,28192,34660`; all were absent at run completion.
The closer's observed integration runner ended at `16:17:22.931Z`, before this
run. This proves the announced I/N sequencing, not an OS desktop lock or
exclusion of human/other-harness activity.
Historical 1180 lookup and 1440 focus causes remain unknown: a controlled
green is not retroactive causality.

### Actual versus unavailable image inspection

Six NP6 class/far-source captures were requested one at a time. Five tool
responses did not supply pixels because of their image limit. **Only this NP6
1440-DIP far-source image was actually supplied and inspected in NQ18:**

`native-1440-20260915145916531-3e90b547901d4ea29196bfcc0351f0b9/far-source.png`,
relative to `.artifacts/atlas-e1/`, SHA-256
`773A93D571F103C84A7F8C6148C8CCB344FC155A8396141BE8456DD5BF89B963`.
The M140 source line, selected identifier highlight, selected outline occurrence
and page/bounds disclosure are visible. This image predates the NQ18 final run.

The other five requested NP6 images are **not** claimed visually inspected.
Their capture hashes were read from receipts, not substituted for pixels.
The final run generated fresh native class/far-source images and local 15-DIP
hard-state images in:

- `native-1180-20260915162520364-4e67909d32994c24bc7cfbbb810470de`
- `native-1280-20260915162507020-b1625a7d5248415ab9b3adf8e7502456`
- `native-1440-20260915162514096-bcc5007bebaf40289d3f9eea3c8945ed`
- `states-5b657f98ae7f4ffe9a503ba70200be66`

These directories are under `.artifacts/atlas-e1/`; their image paths/hashes and
geometry measurements are retained in the raw receipts. **The fresh pixels were
not independently inspected in NQ18.** The admitted environment remains bounded:
144 native/WPF DPI, high contrast off, animation on; local 15-DIP font sizing is
not current OS text-scale evidence. No full E1/hardware/OS-scale acceptance.

### NQ18 hashes and closure limits

| Evidence | SHA-256 |
|---|---|
| Back-mode red TRX | `40240528C3A2C8EECEB801CC02A3D40104230B2AEC4CB2E75123BE9E18DB2323` |
| Back-mode restored TRX | `615DF1D73DAC3CE506DAEECFB8B44C0B8093475F0FA32B56DB543AEA762D7985` |
| Token-route red TRX | `D589D03E4E82301AF7310254BF6BF21C0E76A7CB674F1B438093BBE04078AAD1` |
| Final 113-case TRX | `7410171DED6B568C9C7C91557C9F25CD87AA9A92442230885067463C4B92DC20` |
| Full named results JSON | `3C87445157D30DE7AF19DE33DBD7457A44859E6E9B3CCAF48FC38158680A67F8` |
| Rebuilt App DLL | `D288AE9DCDA7B73BE6AD4FFB0C4EC90AA8D783FC6B677F9F9EB9518196D14DDC` |
| Rebuilt App.Tests DLL | `EF7D98F07DE8A897B46F4E5B70437F056E7F58E06ABCA4856F27F771D118F840` |

Back-mode mutant Reader hash:
`3AB61050F1CE7A2F5BEA722E2AE3DF2118220446C4F0599A535C7B45DD904B05`;
token-route mutant Reader hash:
`BFD3B664765293DAB9FED5958386F1B7AE284C9255E920D8C406905B5DAC8C2E`.
Neither remains in the restored source/binary.

Final working-source hashes (stable across the final run):

| File | SHA-256 |
|---|---|
| `AtlasReaderView.cs` | `48408746563FAB04AFB62D0C86E9EEFAAF2AE4AF337A8C75DE5A4654824BD187` |
| `AtlasStaticView.cs` | `A6BC21418FA00B6BDE95C531DD7F16B38A8168B44522638E678DFC5C7BC31DCA` |
| `AtlasStaticViewProjection.cs` | `3746CC81D13048AE8292CB1E2A025B9F197EB2A59D58C69BDC450FEF2D995B62` |
| `AtlasStaticViewTests.cs` | `768B0FC09C54FDE9D81C9095806A7B5115FAB3DA41D74EF52B031555DD8B51CE` |
| `AtlasStaticCompositionTests.cs` | `7C9FC49F1AC2B7A4B1322FE43AED8FF95937E27D153CE186084A876B94C23377` |

NQ18 preserves this evidence as a bounded qualification checkpoint, **not a
qualified freeze**. The remaining three single-fault controls, unavailable/
uninspected pixels, historical causes and independent combined A/R plus
UX/UML/C#/SRE/DS/Security/Owner gates remain explicit. No unproven product repair,
Core/factory/chrome/dependency change, rebase, main mutation or push occurred.
Original raw 26-case proof, older reds, mutations and backups remain unstaged.

## NM12: remaining three fault controls discharged; rendering gate still open

NM12 is a separate 12-call allocation, started at `2026-09-15T16:51:32Z`,
with exactly three cases. It makes **no permanent product or test-source
change**. The prior NQ18 113-case receipt is preserved, not rerun or relabelled.
The sole reusable harness is the owned ignored artifact
`.artifacts/atlas-e1/nm12-mutations.py`, SHA-256
`E56D8D756696985BCC2167F60D34667362B67E8540934AFE6346D732068DD361`.

The worklist is explicit and finite. Each case verifies source/test hashes,
requires exactly one matching mutation site, resolves the exact assertion line
from the unchanged test, writes a complete backup and live-mutation marker,
and applies only one fault. A red must match the named method, source assertion
line and semantic message for every selected case; missing TRX/count, a survivor,
wrong assertion, setup/build failure or active runtime stops the worklist.
Restoration is in `finally`, byte-checked, and followed by a non-incremental
build before the matching green. Successful cases are not repeated on resume.

### Exact fault/oracle matrix

| Case | One injected fault | Named semantic red | Restored green |
|---|---|---|---|
| Off-page parent/classifier admission | In `AtlasStaticViewProjection.Create`, replace only `?? classifiers.FirstOrDefault();` with `?? rows.FirstOrDefault();` | `Projection_OffPageParentIsNotBorrowedFromPreviousPage`, `AtlasStaticViewTests.cs:93`, `Assert.Null(next.Classifier)`: actual orphan Method Member, UTF-16 40000-40003, OutsidePage. 1/1 failed | 1/1 passed |
| Back page | In `GoBackAsync`, replace only `_outlinePageOffset = frame.OutlinePageOffset;` with `_outlinePageOffset = 0;` | Real-pipe journey, `AtlasStaticCompositionTests.cs:191`: expected 128, actual 0. All three viewport cases failed this exact assertion | 3/3 passed |
| Originating Back focus | In `AtlasStaticView.RestoreFocus`, replace only `return button.Focus();` with `return _classifiers.Focus();` | Real-pipe journey, `AtlasStaticCompositionTests.cs:164`: originating compartment-button focus predicate not matched. All three viewport cases failed this exact assertion | 3/3 passed |

The off-page fault promotes an off-page orphan into a classifier fallback; it
does not claim to exercise a historical cache that this stateless projection
does not have. The focus fault changes the focus destination, not the saved
token or mode. These are separate post-hoc mutation controls, not rewrites of
historical TDD evidence. No assertion or expected value was weakened.

The off-page test and its DTO helpers were opened and proven non-shown before
execution: no Window, Show, UIA, daemon or dispatcher is used by that selected
pure test. It ran while integration held the desktop. The harness then stopped
with source restored, no live marker and exactly two pending GUI cases.

### Desktop grant, sequential runs and release

The two GUI cases ran only after the closer's explicit grant:
`closer-ed7d1cd1-NM12-after-ER18-PID30732-20260915T170450Z`.
The reported integration runner ended at `17:04:50.621990Z`; the first NM12 shown
runner was observed launched at `17:13:33.048768Z`.
No integration/native overlap or OS-wide human exclusion is inferred beyond
the observed handoff contract.

| Run | Actual runner PID | Launcher-observed start UTC | Exit-observed UTC | Shared start/end request |
|---|---|---|---|---|
| Back-page red | 14588 | 17:13:33.048768 | 17:13:54.636370 | `req-01M2K0YGN8VBPCRHX0DRB37YSX` |
| Back-page green | 28336 | 17:13:59.516594 | 17:14:22.841971 | `req-01M2K0ZAG8EBWXJSQ0S15Q5GDE` |
| Back-focus red | 25008 | 17:14:32.606229 | 17:14:49.788090 | `req-01M2K10ATD3F55X51V4EJCR0HN` |
| Back-focus green | 21600 | 17:14:56.941263 | 17:15:21.031231 | `req-01M2K112JWC9EEGG3BE6A60E1Y` |

All dates are 2026-09-15. These are measured launcher observations, not invented
OS process-creation timestamps. All four shared requests were resolved with
their actual runner, native receipt paths and PID census. After the last run,
known runtime PIDs `6400,11656,30168,32348` were all absent. Every shown-run
record has `desktop_released=true`; the desktop is released to the closer.
No further shown run or whole cohort was launched.

### Exact receipts, restoration and reusable harness output

All case files are under `.artifacts/atlas-e1/nm12-mutations/<case>/`:
`red.trx`, `green.trx`, `red-run.json`, `green-run.json`, build/test logs and
the complete original source backup. The run JSON contains full named results,
exact assertion stacks, counters, runtime/desktop observations and hashes.
The aggregate `progress.json` reports
`three-controls-complete-not-product-acceptance`, `remaining=0`, SHA-256
`8B6F3D9EF3B9A5879E3691BEC251A56C60C183DC621E9B69AF073ED210122F89`.

| Case | Red TRX SHA-256 | Restored green TRX SHA-256 |
|---|---|---|
| Off-page | `E3E374979CCAE49A4449398FE013382915C0536F381A5E30B86CE4C6D2E48EF0` | `EE9FCF9EC52C5DFD7FF135ED0A5577D373F1AC8D34D7F0EDBF19037F2A7BFBD0` |
| Back page | `83AB99DF930D5B77677202A2145A7838D2C1CF7298A7B1871BCA3AF2CBEDE755` | `C7D35894B1173818B0EDB9C0E73C0BDD3ED1CA0C410E30929E2D9D8367722D59` |
| Back focus | `76AA8A62412CB689DA5078518AFAF5E85AFDC895A1AB439CFFD64855B2553ECF` | `B3DEE862D5B830BCED01B73D6D1B4FC46933684E1115EBCE07FB13E6ADAC5B2B` |

| Source | Original/restored SHA-256 | Mutant SHA-256 |
|---|---|---|
| Projection | `3746CC81D13048AE8292CB1E2A025B9F197EB2A59D58C69BDC450FEF2D995B62` | `771F3B1A10111B1AE411CA936B2F1BF19D5B22657CD0D35EEE8824A16C62607A` |
| Reader | `48408746563FAB04AFB62D0C86E9EEFAAF2AE4AF337A8C75DE5A4654824BD187` | `358AC2D30C3FEB18439B6337784030FA7F97EDB90E7B50FE4ADB1214857FF0A9` |
| Static view | `A6BC21418FA00B6BDE95C531DD7F16B38A8168B44522638E678DFC5C7BC31DCA` | `C6F3E005545B3B2654A7EB2C1E6A4CF03733A97619DDBE23615C0EA20357F282` |

The unchanged unit-test hash is
`768B0FC09C54FDE9D81C9095806A7B5115FAB3DA41D74EF52B031555DD8B51CE`;
composition-test hash is
`7C9FC49F1AC2B7A4B1322FE43AED8FF95937E27D153CE186084A876B94C23377`.
After each restoration, non-incremental builds produced App DLL hash
`86409741D53E4F20C8CC24578BD97EBB95BA43E22338B1FBC0466977C5A7ACAE`
and App.Tests DLL hash
`D67938AAE0C238464ABFBEA3B026E8DA078A53511ED411C88A12BD02D751B428`.
The live-mutation marker is absent and the tracked source diff is empty.
Source leases were claimed/checked before mutation and restoration, then
released before builds/tests; evidence leases are separate.

### Remaining independent rendering and admission gates

The three missing fault controls are now discharged within the stated
boundaries. **This is not native product acceptance.** The independent root
pixel review reports a Major required-disclosure clipping defect at 1180 DIP,
confirmed on the final NQ18 capture:
`native-1180-20260915162520364-4e67909d32994c24bc7cfbbb810470de/class.png`
under `.artifacts/atlas-e1/`. The final “e” of “Calls and lifetime relationships
are” is not visible at the right edge. This is attributed to the independent
root's actual pixel inspection; NM12 made no image-view attempt or layout fix,
and does not infer its cause.

A separate bounded rendered-glyph versus ancestor-clipped-viewport control and
full-wording repair remain required. Missing pixel coverage, hardware/OS-scale
limits, historical lookup/focus uncertainty and the final independent combined
A/R plus UX/UML/C#/SRE/DS/Security/Owner gates remain open. Original 26-case,
NQ18 113-case, historical TDD and earlier mutation receipts remain unchanged and
unstaged. No Core/factory/chrome/dependency edit, rebase, main update or push
occurred.
