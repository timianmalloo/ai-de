---
id: proof-cross-harness-coordination
title: "Cross-harness coordination: dormant P1 repair evidence and pending runtime gates"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: P1
tags: [coordination, proof-pack, confidence, pending]
links:
  - { to: spec-cross-harness-coordination, rel: documents }
  - { to: design-cross-harness-coordination, rel: documents }
  - { to: plan-cross-harness-coordination-phases, rel: relates-to }
  - { to: investigation-cross-harness-message-delivery, rel: depends-on }
review-by: 2026-10-16
summary: >-
  Records dormant P1 four-finding repair RED/GREEN, actual CLI and killed-correlation-mutant
  evidence with immutable source/test pins. Preserves historical P0 receipts and explicitly
  pending independent re-review, runtime, authority and later-phase gates.
---

# Proof Pack — dormant P1 candidate; independent code gate pending

## Current receipt — four-finding repair, 2026-09-16, Python peer author

**Outcome: F1–F4 repaired and regression-tested in the dormant P1 candidate;
independent re-review pending. Full P1 and runtime activation are not cleared.**
The author reopened official session `xh-p1-responses-b0d0` in the existing
`C:\Projects\ai-de-feature-xh-p1-responses` tree, branch `feature/xh-p1-responses`.
Repair baseline is `ec85de0be8713bbd20d2b2035df2c4c5bf4c8126`.
The limited P0 gate at `62af66ca98ef0fed810b6b07d59f79f2b227176a` remains
**dormant-code-only**. This record does not promote it to full P1 approval.

### Exact scope, surfaces and implementation

The finite worklist was the independent review's four findings, not a new investigation.
Inputs remain original JSONL occurrences; derived folds are not another stored status.
Surfaces reached: synthetic raw ledger → official `read_request_events` →
`_request_event`/`validate_response` → `fold_requests`/`_fold_responses` →
official subprocess `collaborate summary|check`, JSON and text modes.
The existing actionable by-ID CLI and real pinned old-worktree rollback test also ran.
No database, WPF, launcher or endpoint adapter was changed.

* **F1:** duplicate legacy adds compare sorted JSON serialization instead of Python
  dictionary equality. Nested booleans, integers and floats retain distinct serialization;
  object-property ordering remains immaterial and array order remains significant.
  Neither input dictionaries nor original raw history are rewritten.
* **F2:** `_is_enhanced_record` is the shared append/reader/fold discriminator:
  a `schemaVersion` key, regardless of its value, or a string `kind` beginning
  `coordination-v` reserves an envelope for strict validation. Missing, changed,
  unsupported or malformed discriminators on that path refuse `XH.SCHEMA_INVALID`;
  the disabled append path refuses `XH.ENHANCED_DISABLED` before filesystem effects.
  Genuinely markerless legacy extensions remain tolerant. An `eventType` alone is
  not an enhanced marker: reserving every extension name would break that tolerance.
* **F3:** invalid request-ledger records or fold conflicts terminate both collaboration
  commands with logical exit **4**. JSON emits `status: not-checked`, stable `code`,
  `request_errors`, and measured `duration_seconds`; it emits no partial `requests`,
  `findings` or `active_sessions` claimed as checked. Text mode emits the refusal on
  stderr, not an uncaught traceback or `check: OK`. Valid-path output is unchanged.
* **F4:** production correlation logic was already rejecting mismatches. Its missing
  control now independently changes repository, stream, thread, obligation and causation,
  in both event orders. Each retained original reply has `XH.CORRELATION_MISMATCH`,
  unanswered/remaining true, latest disposition/next null, and acceptance/execution false.

### Executed receipts and immutable source pins

All rows below are **Verified by execution**. Counts distinguish selected test methods
from failing subtests. Raw receipts are local, reproducible files under `.agents`;
this committed table preserves their results even when those local files are absent.
The five run-output files are removed during final cleanup rather than committed as
additional authored files. Every per-test fixture was torn down after its run.

| Run / tool receipt | Source | Selected tests | Failing subtests / errors | Logical exit | Measured elapsed |
|---|---|---:|---:|---:|---:|
| Unchanged candidate baseline, shell 683, `.agents\xh-p1-repair-baseline.txt` | `ec85de0b` | 19 | 0 / 0 | 0 | 0.781 s |
| Final RED, shell 687, `.agents\xh-p1-four-findings-red-final.txt` | unchanged `ec85de0b`, new tests | 27 | 21 / 0 | 1 | 1.220 s |
| GREEN, shell 689, `.agents\xh-p1-four-findings-green.txt` | fixed source | 27 | 0 / 0 | 0 | 1.287 s |
| Guard-False mutant, shell 691, `.agents\xh-p1-four-findings-mutant.txt` | fixed source; function mutated in memory only | 27 | 10 / 0 | 1 | 1.218 s |
| Pristine discovery after mutant, shell 692 | fixed on-disk source | 27 | 0 / 0 | 0 | 1.225 s |

`git diff --check` also returned 0 in shell 692. The eight new methods and their
oracles are listed below. Existing 19 methods were retained.

| Control (`ResponseTests` method) | Claim and falsifying oracle | RED evidence / remaining limit |
|---|---|---|
| `test_Fold_NestedJsonTypes_ConflictingAddsRefusedInEitherOrder` | F1: nested `true/1`, `false/0`, `1/1.0`, each in both orders, must raise `XH.EVENT_CONFLICT`; raw bytes and parsed inputs unchanged | 6 failing subtests on `ec85de0b`; GREEN. Does not qualify cross-language canonicalization |
| `test_Fold_ReorderedObjectProperties_AreLegitimateDuplicates` | Object key permutations plus resolve yield exactly one resolved row; original raw file unchanged | Positive preservation control, six permutations; passes before/after, not separately claimed RED |
| `test_ReadAndFold_ReservedMarkers_ExplicitSchemaRefusal` | F2: 13 malformed/unsupported discriminator cases reject at reader and direct fold; raw file unchanged | 6 failing subtests on `ec85de0b`; other cases already rejected. GREEN reaches both direct fold modes for every case |
| `test_ReadAndFold_UnversionedLegacyExtensions_StayTolerant` | Markerless unknown fields/types and extension payload survive; ordinary request stays open | Positive preservation control; passes before/after, not separately claimed RED |
| `test_Append_ReservedMarkers_RejectBeforeFilesystemEffects` | Every reserved marker refuses, with no parent directory/file created | 1 failing subtest on `ec85de0b`; GREEN. No live enhanced append performed |
| `test_Cli_CollaborateConflictingAdds_ExplicitFailureWithoutPartialState` | F3: two actions × JSON/text, stable exit 4 and no traceback/partial checked state | 4 failures: old CLI exit 1 with traceback instead of structured refusal; GREEN through real subprocess |
| `test_Cli_CollaborateUnsupportedEnvelope_ExplicitFailureWithoutPartialState` | F2/F3: unsupported envelope must not disappear into a successful partial summary/check | 4 failures on `ec85de0b`; GREEN through real subprocess |
| `test_Fold_EachCorrelationMismatch_RetainsReplyWithoutAnswering` | F4: all five context dimensions, each independently changed in two event orders | Original guard passes; guard-False mutation yields exactly 10 failing subtests, zero errors |

The earlier pre-P1 run's **22 failures + 22 errors** is historical evidence, not the RED
oracle for these new assertions. In particular, its 17 unsupported-enhanced-API errors
cannot establish any new semantic assertion. This repair's final RED has **zero errors**.
An initial repair-test run (shell 685: 24 failures) included three cascading fixture
failures: a first failing append created the directory shared by subsequent subtests.
Each case now owns a distinct `absent-<index>` path. That test-only correction preceded
the final RED and is not counted as three product defects.

| Pin | Value |
|---|---|
| Supplied pre-P1 parent source Git blob, verified via `ec85de0b^:<path>` | `b2ed495fcf4b6332ddf517aee17144173ac5b96b` |
| Actual repair-baseline source Git blob, verified via `ec85de0b:<path>` | `be4767487c8af98ed9b1468ab8dfb48c5daa9f44` |
| Repair-baseline source SHA-256, executed bytes | `0f16846da8664a3e39bc1eef42d143d04e1f71405210977034be701897bc937b` |
| Fixed source Git blob | `ee3981c854c857884d2af324d7cc0d1de1efdad4` |
| Fixed source SHA-256, executed and Git/LF bytes | `67b6db01231a1f695a1b1e35390530f76681b714996bcc574f1e3b0e91475c77` |
| Final tests Git blob | `e7fdcc8c55fe97611f5a2a1891f09567c9ee5051` |
| Final tests SHA-256, executed Windows bytes in RED/GREEN/mutant | `beba2b5e54683ee3e167d990bb971ac42973294cab85f871c3cdf0fd9d481360` |
| Final tests SHA-256, normalized Git/LF bytes | `705ffb2237a60e777899747593c8278f6e71ca4c1540a447528a4bc1d61b7f36` |
| Unmodified compatibility client commit / source SHA-256 | `94ec9036dd0b72aa5b759badcf21a9e3aba6659b` / `f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf` |

Source path: `docs/ai-forward-pack/scripts/coord-core.py`; tests path:
`docs/ai-forward-pack/scripts/tests/test_coord_responses.py`.
The LF and executed test hashes differ only by checkout line endings; neither is
presented as the other. The compatibility method
`test_Cli_PinnedUnenrolledLegacy_ActualLinkedTreeAndRollback` executes the unmodified
client's actual add→resolve→list from an unenrolled linked tree, compares IDs/payloads
with the upgraded reader, then replaces upgraded files with pinned originals and repeats.
It does **not** prove mixed-client contention, complete-record atomicity or live authority.

Reproduce the candidate and discovery receipts with the existing official test entry:

```powershell
python docs\ai-forward-pack\scripts\tests\test_coord_responses.py --receipt .agents\xh-p1-four-findings-green.txt
python -m unittest discover -s docs\ai-forward-pack\scripts\tests -p test_coord_responses.py -v
```

The mutation run imported that same test module and real official helper, parsed only
`_fold_responses` with stdlib `ast`, selected the **single** `if` whose test contains
all five correlation field names (asserting exactly one match), replaced its test with
`ast.Constant(False)`, compiled the function back into the isolated imported module,
and ran all 27 methods. No production file was edited. All 10 failures belonged to
`test_Fold_EachCorrelationMismatch_RetainsReplyWithoutAnswering`: each field listed
above failed in both `[request-add, coordination-v1]` and reverse order. This is an
observed killed mutant, not a mutation-score claim or a claim about subprocess mutation.

### Class → sweep → derive → prevent; gate and remaining work

| Class | Bounded sweep / derivation | Control |
|---|---|---|
| Host-language equality erases wire types | Legacy duplicate comparison, enhanced canonical bytes and resolution tie-breaking inspected; enhanced bytes already preserve JSON types | F1 nested-type permutation test; use sorted JSON rather than dict equality |
| Marker detection differs across admission and reading | Append, `_request_event` and fold all inspected and moved to one predicate | F2 reserved-marker reader/fold/append matrix; tolerant legacy positive |
| A throwing fold escapes a CLI status boundary | Both collaborate actions/modes inspected and exercised; request-list already catches fold conflicts | F3 actual subprocess tests and explicit not-checked output. Request-resolve's separate pre-existing conflict path is outside this four-finding repair and not qualified here |
| Composite guard lacks independent negative cases | Five correlation operands and both event orders enumerated; positive correlated response remains covered | F4 10-case killed guard-False mutant |
| A failing subtest contaminates later filesystem cases | New append cases swept; shared absent directory replaced by per-case path | Final RED count excludes three cascading fixture failures |
| A lower-level helper has a different root contract from its CLI | Initial audit append used repository root, but the official helper takes the docs root; status read-back found only this run's two untracked entries | Shell 703 asserts the resolved canonical path, exact two owned IDs, original byte-prefix preservation and appended event equality; then removes only the stray files |

These controls and the bounded class sweep are recorded here because the author does
not own the lessons register or site surfaces; conductor owns any register incorporation.
D0/D1/D2/D4/D5-provider/D6 apply to this change. No dependency or API installation was
needed. Failure telemetry was read back from real CLI JSON: stable refusal and measured
duration are emitted by the ordinary failure path, not a debug switch.

**GATE F1–F4 repair · 2026-09-16 · Python peer author · author evidence complete;
independent Security/Test/DS re-review PENDING · no author self-clearance.**
No enhanced writer, send, grant, transfer or launcher was activated.
Remaining: P1 acceptance/consumption/proposal-supersession/full authority and proposal
verification, cross-language digest vectors and unchanged mixed-client qualification;
P2 real SQLite atomicity/migration/rebuild; P3 pinned real endpoint spikes and
arrival/consumption/wake receipts; P4 qualified launcher provenance; P5 cross-surface
conformance and measured SLIs. User approval for all phases remains; this repair
does not request it again. Deferred upstream reuse is recorded in the phase plan only.

Finalization uses the existing audit module's `next_id`, `consume_start`,
`duration_fields` and `append_log` helpers. Its `cmd_append` unconditionally renders
derived pages; using the append-only helpers honors the conductor's explicit prohibition
on derived/site writes. No audit implementation is changed. `AIDE_CONTRACT_LOG` was
absent, so no episode-close capture could be emitted; no evidence path was invented.
The worktree is retained for independent review and integration, not removed while
it carries the only unmerged repair commit.
Official graph audit `al-01M2NPCVC7N695D3BN42ZWZJVF` and repair audit
`al-01M2NPCVC8FYZM78P6ZFGM61G6` are in the canonical `docs/audit/audit-log.jsonl`.
The corrected repair append consumed the original 17:54:38Z start marker and records
**792.0 measured seconds**, excluding the earlier reads as stated in the phase plan.
The audit reports **29/35 calls through its corrected append**; later claim release,
commit and final state verification are lifecycle calls, not unrecorded product work.

## Prior receipt — 2026-09-16, original dormant P1 author (historical)

The P0 account below is historical. This section supersedes its pending-P0 status, not
its unperformed runtime gates. Supplied conductor gate, not authored self-clearance:

**GATE P0-delta · 2026-09-16 · Test/DS · unchanged unenrolled old clients and exact
pinned O08/O10 oracles, dormant-only zero authority, additive existing watcher.db ruling ·
PASS FOR DORMANT P1 ONLY · F1 resolved; authority/runtime floors BLOCKED.**

**Outcome: partial programme delivery, reviewable dormant response-only candidate.**
Worktree `C:\Projects\ai-de-feature-xh-p1-responses`, branch `feature/xh-p1-responses`,
official session `xh-p1-responses-b0d0`, base
`62af66ca98ef0fed810b6b07d59f79f2b227176a`. Created by the official registered worktree
command; no installation/configuration/hooks changed. Source/tests and this Proof Pack/
plan are the only authored surfaces. Audit is append-only. Derived pages and rollups
remain conductor-owned. No source changes in the conductor, main or primary checkout.

### Implemented seam and explicit narrower boundary

`coord-core.py` is still the sole official helper. No extra module or status database.
`read_request_events` → `fold_requests` → `cmd_request` is the exercised reader path.
Ordinary legacy commands retain their arguments and JSON shape. Added
`request list --id <original-id> --actionable --json` reads all statuses for that ID.
The new read is opt-in. It returns original response envelopes, refusal codes, latest
applicable disposition and separate unanswered/remaining/accepted/execution fields.
Production supplies **no** generation fixture map, so typed replies remain visible but
cannot mark an unverified generation answered. A legacy resolve is not typed acceptance.

The dormant schema admits **response-recorded only**, with a closed disposition set,
explicit endpoint generations, exact `(proposal id, revision, sha256)`, correlation,
response supersession and semantic digest. This is not the full immutable AuthorityRef
or ProposalRef verifier. Missing full commit/path/blob proof is not silently approved.
Proposal-superseded, proposal-accepted and recipient-consumed event implementations remain
pending. Response supersession is supported; proposal revision supersession is not.

Synthetic fold callers can supply a generation map to exercise deterministic semantics;
that map is explicitly **not authentication** and is never supplied by the CLI. Every
folded acceptance/execution flag is false. `append_record` rejects enhanced/schema-versioned
records before opening a file, regardless of synthetic verifier content. There is no
activation flag, grant/transfer operation, endpoint send or launch implementation.
Concurrent enhanced attempts all refuse. No enhanced event was appended to a live stream.

Legacy duplicate/reordered add and resolve cannot reopen a resolved request. Unequal
duplicate legacy adds refuse rather than replace. The reader rejects non-object JSON,
duplicate keys, invalid IDs/timestamps, non-finite constants and invalid UTF-8, without
sorting malformed objects. Strict new-envelope validation is separate from tolerant
legacy unknown fields/types. Same typed source key with different semantic bytes produces
`XH.EVENT_CONFLICT`; originals and raw file history are untouched. This is **reader/fold
conflict proof**, not successful idempotent enhanced append receipt proof.

Short `os.write` counts now raise `COORD-SHORT-WRITE`. A one-byte injected append leaves
one byte and fails. There is **no claim that an error atomically repairs or undoes that
partial append**, no fsync/power-loss guarantee, and no cooperative-lock solution to
unchanged-writer races. CLI request errors return nonzero with a stable error code.

### Executed RED → GREEN and immutable pins

Final suite: `docs/ai-forward-pack/scripts/tests/test_coord_responses.py`.
No official Python tests directory/self-test command was found in this vendored scripts
snapshot; this creates the requested discoverable stdlib suite. No C# test is claimed.
Full named GREEN output and summarized RED failures are in tool receipt **shell 639**.
The runner can regenerate full local receipts without output redirection:

```powershell
python docs\ai-forward-pack\scripts\tests\test_coord_responses.py --baseline-receipt .agents\xh-p1-red.txt
python docs\ai-forward-pack\scripts\tests\test_coord_responses.py --receipt .agents\xh-p1-green.txt
python -m unittest discover -s docs\ai-forward-pack\scripts\tests -p test_coord_responses.py -v
```

**Observed:** final pinned baseline ran **19 tests**, exit 1, **22 failing subtests and
22 errors** (3 test methods passed). Candidate discovery ran **19 tests**, **0 failures,
0 errors**, exit 0, **0.784 seconds**. Subtest counts are not test-method counts.
Initial fixture defects (missing pinned `repo_identity.py`, inherited `GIT_CONFIG_*`,
Windows read-only Git objects and a mocked descriptor close) were corrected before this
final comparison. They are not counted as product RED evidence.

| Pin | Observed value |
|---|---|
| Pre-P1 commit | `94ec9036dd0b72aa5b759badcf21a9e3aba6659b` |
| Pre-P1 coord-core Git blob | `b2ed495fcf4b6332ddf517aee17144173ac5b96b` |
| Pre-P1 coord-core SHA-256, asserted by legacy test | `f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf` |
| Pinned coord_ids support blob | `6afe13e87fd37e650e790b2135c119e16b85797c` |
| Pinned repo_identity support blob | `27425b9f40e8f3d2abde90032862b01b4969c78c` |
| Candidate coord-core working-byte SHA-256 | `0f16846da8664a3e39bc1eef42d143d04e1f71405210977034be701897bc937b` |
| Final test working-byte SHA-256 | `7f65bad0c32e6be7894d0e9ccdd0c9abf315f074ec4b4d363ba37c42d06cb7e8` |
| Full final RED receipt SHA-256 (local, regenerable, not committed) | `7fdace563ab49921aad01a1b38459a642c96fbaea350ead9e00e83ba9d250d8f` |

Every name below is a `test_` method of `ResponseTests`. Names are shortened only by
that common prefix. **Verified** means executed locally, not independently accepted.

| Test name | Oracle and RED observation | Confidence / remaining boundary |
|---|---|---|
| Append_ConcurrentEnhancedAttempts_AllRefused | 8 attempts, width 4; baseline succeeds, candidate refuses all without file | Verified denial; no mixed-client activation |
| Append_DiskFailure_Propagates | Injected disk error propagates; both versions pass | Verified existing behavior preserved |
| Append_EnhancedSyntheticVerifier_ZeroEffects | Baseline appends; candidate refuses before file-open/subprocess | Verified append boundary only; full O07 authority adapter pending |
| Append_ShortWrite_FailsWithoutRepairClaim | Baseline reports success; candidate raises and retains exactly one byte | Verified; no atomic repair/durability claim |
| Cli_AllStatusById_ReadsHistoryWithoutAuthority | Baseline rejects flags; candidate exposes exact reply, unknown generation, zero eligibility | Verified official CLI composition/read |
| Cli_PinnedUnenrolledLegacy_ActualLinkedTreeAndRollback | Actual git primary + 2 linked trees; old client add→resolve→list; compare new/old rows and original raw records; restore exact old source/support and repeat | Verified O08 and rollback portion of O10; old client never enrolls/upgrades |
| Fold_ConcurrentResponses_RefusesAmbiguousLatest | Concurrent unchained replies cannot pick arbitrary latest | Verified synthetic semantics |
| Fold_CorrelatedReply_AnswersWithoutAcceptance | Changes requested clears unanswered but never acceptance/execution; inputs unchanged | Verified O01 fixture, not authenticated production answer |
| Fold_DuplicateAndPermutedLegacy_NeverReopens | All 6 permutations of add/resolve/add; baseline reopens | Verified legacy regression |
| Fold_DuplicatePermutedResponses_OneSemanticAnswer | All 6 permutations of parent/reply/retry; one semantic response | Verified O03 fixture |
| Fold_LegacyAddResolve_PreservesPayload | Ordinary resolve preserves question; ACK prose has no acceptance field | Verified existing legacy behavior |
| Fold_StaleProposal_PreservesHistoricalResponse | Different revision/hash stays historical and unanswered | Verified stale response; full O05 acceptance/supersession pending |
| Fold_SupersedingResponse_UsesCausalLatestNotClock | All 6 permutations; later causal reply has earlier clock; deferred retains checkpoint | Verified response supersession only |
| Fold_UnknownOldGeneration_DoesNotApply | Empty generation map and G1→G2 both refuse | Verified response applicability; O06 authenticated consumption pending |
| Read_DigestMutation_Refuses | Alter payload after hashing; no event admitted | Verified Python digest only |
| Read_EnvelopeValidation_RejectsInvalidSchema | 9 invalid schema/type/disposition fixtures; baseline accepts | Verified dormant subset only |
| Read_InvalidUtf8_ReportsAndKeepsNextRecord | Invalid byte followed by valid event; baseline decoder aborts | Verified record-local refusal |
| Read_MalformedObjects_ReportsInsteadOfSorting | 7 malformed/non-finite/duplicate-key cases | Verified parser control |
| Read_SameEventKeyChangedPayload_ExplicitConflict | Changed bytes under same key refuse; all 3 raw records preserved | Verified reader conflict; O09 append retry receipt pending |

### Class → sweep → derive → prevent

**Class:** replay/arrival order used as domain state; ambiguous input normalized into a
plausible record; attempted append reported as completed. Related standing classes are
E2E-F, RIG-C and DC-026. **Sweep:** request reader, fold, append, resolve and list were
traced together. `append_decision` also ignores write count, but it is explicitly best-effort
telemetry, not canonical admission; no claim of repair there. **Derive:** one official
fold computes the read, no second disposition/ACK/ownership store. **Prevent:** the
permutation, malformed corpus and real one-byte write tests above fail on the baseline.
Fixture corrections are CI-ENV/RES-LEAK-TEST shapes: pinned support plus scrubbed Git
environment and read-only-aware teardown now live in the tests. Defect-register editing
was outside author ownership; conductor must incorporate this receipt, not invent a lease.

### Unmet floors and next independent gate

* **Blocker / Verified:** enhanced writer is disabled. O09 successful retry receipts,
  mixed unchanged-writer contention/full-record/conflict safety, cross-language golden
  bytes and complete P1 authority checks are unimplemented/unqualified.
* **Blocker / Flagged:** qualified verifier/human channel and authenticated generations
  remain unavailable. O04 exact valid peer-acceptance positive, full O05 proposal
  supersession, O06 consumption and full O07 authority-entrypoint proof remain pending.
* **Major / Verified:** opt-in read has unknown generations in production. It displays
  historical replies, not an authenticated actionable workflow. No new transport or
  acceptance status is presented as live.
* **Major / Flagged:** full parser/ledger size bounds, invalid-surrogate corpus,
  generation revocation, privacy retention and endpoint behavior are not qualified.
* **Independent code gate pending:** Test/DS/Security review this exact candidate; the
  author does not clear it. P2–P5 remain approved work, not completed work.

Instrumentation: enhanced CLI list emits elapsed seconds, events scanned, explicit disabled
writer and not-qualified generation status on the normal opt-in path. Errors are returned
without raw payload logging. No inference spend, network or endpoint latency is measured
because none is invoked. Full OTel integration and P5 SLIs are not claimed.

Closing audit `al-01M2NMW23RP1R4EC0ZESNW60NF` records **808.00 measured seconds** from
the official start marker and outcome `partial`. The official prompt logger also wrote
`al-01M2NMW20JGD8H8XXRMPCXZA3R`. `audit-log.py selfcheck` reported a goal/budget
presence gap on that prompt-only entry; the implementation entry carries both. This is
a reported tooling/record gap, not a PASS. Official audit append regenerated
`docs/audit/audit-data.js` as a side effect; that generated diff is excluded/restored
because the conductor owns rollups. Local synthetic Git roots and receipt scratch files
are removed after capture; the committed tests regenerate their evidence.
**Minor / Verified process gap:** the closing-audit paragraph was added after its prior
lease was released. The exact Proof Pack lease was reacquired for this correction and
released before commit. It is not represented as continuously leased editing.

**Original author:** Data & Persistence peer/co-author. **Delta:** Python phase-specific
recording of the supplied independent F1–F6 corrections. **Tier:** T2.
**Scope:** five existing authored docs and official append-only audit entries; no solution code,
migration, endpoint probe, new dependency, runtime repair or independent gate clearance.
**GATE P0-delta pending independent review.** The actual human approved implementation
of all P0–P5; that all-six-phase goal remains approved without renewed human phase approval.

## 1. Receipt and immutable evidence

Repository: `timianmalloo/ai-de`.
Source baseline/conductor HEAD: `94ec9036dd0b72aa5b759badcf21a9e3aba6659b`.
Main last-read value supplied by conductor: `bcf4959bc0e0e361736e6a179f05b69fcd0500f8`;
not re-investigated or claimed current.
Branch: `feature/xh-p0-contract`; session: `xh-p0-contract-b0d0`.
P0-delta input HEAD: `a9595585547b7661457847642f799d6ff521a15e`.
The previously ended official session is reopened under the same identity for this
bounded correction; exact-file TTL300 edit claims are released before validation/commit.
Official worktree creation registered `C:\Projects\ai-de-feature-xh-p0-contract`.
Requested path `C:\Projects\ai-de-xh-p0-contract` was not honored by `worktree new`:
`--path` is cleanup-only; `cmd_worktree` derives path from primary basename and branch.
No manual move, installation, config/hook edit or peer-tree edit was used.

Official allocated ADR: `adr-01M2NJ2PQS7X6GE3F559JC4TNP`, under established `docs/adr/`.
All five authored paths are leased at TTL300 only for editing; register/derived paths
are not authored claims. `.agents/artifacts.yml` is classification authority.

| Commit-bound path | Full Git blob | Working-byte SHA-256 at fresh read |
|---|---|---|
| `docs/collaboration/session-contracts.md` | `536352879936b5b10a4b57e71b31f23981ce9b84` | `4e0c3f3532d0a8e0ddddad82477d0d42f2a943a2768710354de1be7bcba8280f` |
| `docs/investigations/cross-harness-message-delivery.md` | `5cdc8be97381f7a00d7b661e8e2983bdc0a92d69` | `b866cbcbfb60429405b3cd78e4fb85361b88c938543f53cb5c6f72fbfe202b04` |
| `docs/ai-forward-pack/scripts/coord-core.py` | `b2ed495fcf4b6332ddf517aee17144173ac5b96b` | `f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf` |

The blob/working hash pins above are local integrity observations, **not trusted-registrar
attestations**. §2 remains the sole authority. A full AuthorityRef additionally requires
verified repository identity, authenticated decision/issuer evidence, scope and a verifier
receipt as specified in the design. This author cannot manufacture those.

The original receipt reported no conductor `graphify-out/graph.json`; the P0-delta
existence check at `C:\Projects\ai-de` returned true. No graph generation/install or
traversal was attempted in this correction. Original document links establish ADR-0023 → implements → architecture-loomkeeper
→ implements → spec-agentic-watcher-substrate → relates-to → session-contracts; ADR-0020
also implements the architecture/spec. The index stores links inside artifact records,
not a top-level `edges` collection; an initial edges lookup returned empty and is not
represented as absence of graph relations. Derived-index freshness after new docs is
conductor-owned and pending, as are security/privacy rollup backlinks.

## 2. Fresh source evidence (not fresh runtime reproduction)

| Source | Verified static fact | Runtime claim boundary |
|---|---|---|
| `coord-core.py:388–442,1786–1830` | Old primary append is unlocked: O_APPEND and one os.write without an fsync/full-byte-count check; reader sorts producer `at`; fold open/resolved; add overwrites same ID; resolve lacks typed revision/generation | A new cooperative lock does not automatically protect the unchanged client. O10 must qualify mixed-client complete-record/conflict safety; no power-loss guarantee. Independent extracted-fold diagnostic is recorded separately below, not CLI/C# execution |
| `CoordinationContractLog.cs:13–105,230–269` | Writer emits contract records; pump rereads entire directory and applies every event | Two-pump post safety still needs O11 |
| `CoordinationContract.cs:259–268,576–628` | Time ordering; ApplyBoardPost calls service per recognized post; no source-key check on that path | Actual repeated-post/restart effects remain Inferred |
| `MessageBoard.cs:127–175` | Instance lock, fresh message ID, count+1; parent/capability validation in service | Two-service cursor loss is an Inferred interleaving until O14 |
| `SqliteWatcherObservationStore.cs:546–617,1262–1276` | Ordinary insert; message PK; repository index; no repository/seq unique constraint or parent FK there | No migration up/down or query plan executed in P0 |
| `WatcherHost.cs:45–73` | Opens `watcher.db` and composes registrar/ingest/pump | Conflicts with shared-store architecture wording; no live binary census |
| `BoardPublisher.cs:70–88`; `BoardTools.cs:76–112` | Drop tombstones; keep newest bounded page; MCP filters sinceSeq then takes newest | Cursor-complete recovery not proven by these views |
| `WatcherBoardPaneViewModel.cs:11–51,68–72` | Row omits source/parent IDs; query reads all board messages | No UI test run or native failure inference |
| `CoordinationContractLogTests.cs:117–130` | Existing rerun test asserts registration counts only | Not proof of post idempotence |
| `MessageBoardTests.cs:100–143` | Tests existing/cross-repo/missing parent, content-free ACK | Not proposal acceptance or migration evidence |

## 3. One confidence ledger

| ID | Finding / severity / confidence | State and clearance evidence owed |
|---|---|---|
| F01 | **Blocker / Inferred:** reliable coordination reuse can violate one-effect/cursor-completeness invariants via replay/two-instance interleaving; static path Verified | OPEN for P2 live delivery. O11–O15 real red tests and independently reviewed repair; author does not clear |
| F02 | **Major / Verified:** ADR-0023 shared-store wording conflicts with `WatcherHost.Open` watcher.db composition | INHERITED ARCHITECTURE DEBT, not claimed conformant. Bounded ruling: additive application/feed/checkpoint caches behind existing watcher observation-store seam in existing watcher.db used by WatcherHost/MCP; requests.jsonl sole canonical source. No new DB/native relocation/workspace.db migration/cross-DB transaction. Data/DS concrete representation coapproval before P2 code; do not expand work |
| F03 | **Blocker / Flagged:** authority verifier/human-channel evidence is not established for the new authority adapter | OPEN for live authority use, not a proven exploit. O04–O07 plus independent Security gate; hash alone cannot clear |
| F04 | **Major / Verified:** old newest-N/read DTO shapes do not encode complete coordination cursor/thread semantics | OPEN for enhanced reads; O18/O26 plus compatibility proof |
| F05 | **Major / Flagged:** endpoint positive conformance and retention/erasure basis not yet established | OPEN; per-endpoint O22 and Privacy/operator review before live sensitive-data admission |
| F06 | **Minor / Verified:** official CLI generated a different worktree path than requested | Documented deviation; use registered generated tree, no bypass |
| F07 | **Minor / Verified:** new index/rollup entries not regenerated by this author; prior graph-absence statement is historical, not current | Conductor checkpoint must derive/validate and report stale/orphan results honestly |
| F08 | **Blocker / Verified (independent F1):** original rollout required upgrading/enrolling every canonical writer, excluding unchanged old-worktree clients | CORRECTED DRAFT, not independently cleared. Unchanged unenrolled official request-add/request-resolve/list remain operational on the same primary requests.jsonl, no enrollment/upgrade. O08/O10 pin unmodified pre-P1 client; enhanced writes disabled until separate coexistence proof |
| F09 | **Major / Verified (independent F3):** P0 admission could be read as full P1/live authority or schema deployment | CORRECTED DRAFT, not independently cleared. P0 admits only compatible dormant fold/envelope/schema-validation and isolated tests after delta review; not deployed SQLite schema, production authority or P1 completion. O07 production-entrypoint zero-side-effect oracle; authenticated fixture/channel and P3/P4 activation BLOCKED |

No runtime red/green result is implied by a static finding. Hard veto applies to
invariant-violating live reuse or an unsafe migration, not to publishing this draft.

## 4. Oracle catalogue — ALL PENDING, unless explicitly BLOCKED

Synthetic fixtures only; approved real-endpoint tests are separate. The test names below
are **specified future tests**, not files already written or passing tests selected by a filter.

| Oracle | Runnable-test specification / meaningful assertion | Phase |
|---|---|---|
| O01 | `Fold_CorrelatedReply_ShowsOnInitiatingThread`: invoke real official fold/read; one negative reply removes unanswered, keeps acceptance false | P1 |
| O02 | `Read_ResolvedThread_ByIdIncludesResponse`: all-status by-ID contains original + response when open-only omits completed work | P1 |
| O03 | `Fold_DuplicateAndPermutedEvents_NeverReopens`: enumerate causal permutations/retry multiplicity; same logical state, no duplicate semantic action | P1 |
| O04 | `Accept_TransportAck_RefusesApproval`: ACK/legacy “approved” text cannot produce acceptance; exact valid synthetic peer pair positive control proves deterministic contract behavior ONLY, no production authority | P1 |
| O05 | `Accept_SupersededHash_RefusesCurrentAcceptance`: H then H2; late H acceptance preserved historically but not applicable | P1 |
| O06 | `Consume_UnknownOrOldGeneration_Refuses`: absent G, G1→G2 restart, alias reuse; zero G2 consumption/action | P1 |
| O07 | `Authorize_NewTransferWithoutHuman_Refuses` / `Production_UnqualifiedVerifier_ZeroPrivilegedEffects`: valid hash but forged verifier, wrong repo/path, Owner prose, no human evidence AND apparently valid synthetic verifier receipt through production entrypoint all deny absent qualified verifier evidence; assert zero grants, transfers, endpoint sends and launches. Isolated synthetic positives prove contract behavior only; authenticated scoped positive fixture is separate | P1 dormant denial/contract tests only; authenticated authority fixture, supported-channel qualification and P3/P4 activation BLOCKED |
| O08 | `Legacy_UnmodifiedUnenrolled_AddResolveList_PreservesOriginals`: pin unmodified pre-P1 official client; run from unenrolled synthetic worktree against the same synthetic primary requests.jsonl; official add→resolve→list with enhanced disabled, and again across rollback; assert original request IDs and payloads at every stage. No enrollment/upgrade or substitute shim | P1 |
| O09 | `Append_SameKeyDifferentPayload_RefusesConflict`: same bytes returns same receipt; changed payload under same key errors without extra write | P1/P2 |
| O10 | `Legacy_RollbackAndMixedClientActivation_GatesEnhancedOnly`: repeat O08 pinned unchanged unenrolled old-worktree add→resolve→list with enhanced disabled/across rollback and original IDs/payloads. Separate pre-activation mixed-client contention, complete-record and conflict-safety tests must include the same unmodified client; torn/short writes and disk failures cannot report false success. Old append is unlocked; new cooperative lock or upgraded shim is not proof. Enhanced writing stays disabled until proof; no legacy client disabled | P1 dormant compatibility tests; separate enhanced-activation qualification pending |
| O11 | `Pump_RegisterPostTwice_OneMessage`: real SQLite and writer/pump path, register+post, two pumps; one stable message and one receipt, not just one registration | P2 |
| O12 | `Import_CommitThenLostAck_RetrySameEffect`: inject crash after commit before receipt; retry returns original mapping/sequence | P2 |
| O13 | `Import_CrashBeforeCommit_NoEffectOrCheckpoint`: crash each write boundary; effect, receipt and checkpoint all-or-none after restart | P2 |
| O14 | `Import_TwoInstancesReaderBetweenCommits_NoSkippedFact`: two services/connections; barrier A write/B waiting, commit A, reader cursor, commit B, reader; both distinct facts observed exactly once | P2 |
| O15 | `Migrate_ExpandOperationalRollback_PreservesAllHistory`: representative real old DB + new events; actual deployer up, old binary/rollback, re-enable/replay; original IDs/payloads and accepted facts identical | P2 |
| O16 | `Import_LateParentAcrossRestart_EventuallyAppliesOnce`: skewed producer times, pending child, restart, parent; new applied feed transition, no orphan or lost consumed cursor | P2 |
| O17 | `Import_UnsupportedVersionOrSourceGap_ExplicitRefusal`: unknown version, malformed line, truncated/changed prefix; no false checkpoint success | P2 |
| O18 | `Read_401Unread_PagesWithoutGaps`: page 200/200/1 ascending; tombstones included as envelopes; no newest-N skip; epoch mismatch explicit reset | P2 |
| O19 | `Replay_TombstoneAndEndedGeneration_NoResurrection`: restart/register/end/late receipt; redacted payload never returns, old generation never revived | P2 |
| O20 | `Queue_LimitPlusOneOutage_PreservesAccepted`: parameterize bytes/items/pending/retry bounds; overflow refused before admission, permanent parent needs-human, disk/DB outage then recovery loses no accepted obligation | P2 |
| O21 | `Deliver_InFlightLimitAndRunningTurn_DefersSafely`: fake time/transport, limit+1, failed/unavailable model, sibling-read refusal; stable retry, no duplicate semantic action, outage recovery | P3 |
| O22 | `Endpoint_RegisteredGeneration_ConsumesAndWakes`: installed/version-pinned foreground/background GHCP/Codex/Grok/Claude, approved low-volume synthetic message, witnessed correct conversation arrival+consumption+supported post-turn wake | P3; all real endpoints BLOCKED pending spikes/availability |
| O23 | `Launch_TwoCallers_AttributesCorrectActor`: same helper under two launcher identities; actor/generation/worktree/candidate/run and PID creation/parent match each caller | P4 |
| O24 | `Start_ReplayedOrExpiredToken_NoSecondRun`: checked token replay returns original; expiry with live holder refuses regrant; PID reuse is not same process | P4 |
| O25 | `Outcome_Failure_DoesNotImplyRelease`: failed END captured; release only after independent holder check; duplicate release stable | P4 |
| O26 | `Conformance_SameCorpus_AllSurfacesAgree`: official queue fold, board projection, MCP and UI expose equal obligations/revisions/negative states at same watermark | P5 |
| O27 | `Sli_PauseTimeout_NoApproval`: fake clocks; total/paused/available duration and outage denominator correct, threshold never creates acceptance | P5 |
| O28 | `Telemetry_Failures_HaveCodesAndNoSecrets`: correlate spans, stable codes, bounded labels; reject raw prose/capability leakage; UI state/craft/accessibility assertions | P5 |

Additional P2 assertions: forbidden fact UPDATE/DELETE fails at store; same-repo parent
FK rejects cross-repo/orphan insertion; SourceEventKey conflict tested at DB boundary;
rebuild equality from source; EXPLAIN QUERY PLAN uses planned indexes for cursor/pending/
thread lookups at 100× corpus; bounded rows/bytes/attempts. These are part of O09/O13/
O15/O16/O20, not optional afterthoughts.

O08/O10 pre-P1 pin: source baseline commit `94ec9036dd0b72aa5b759badcf21a9e3aba6659b`,
official client path `docs/ai-forward-pack/scripts/coord-core.py`, Git blob
`b2ed495fcf4b6332ddf517aee17144173ac5b96b`. P1 must verify and run the unmodified pinned
client with its required pre-P1 support files from the unenrolled synthetic worktree,
not import a changed wrapper or silently reroute it to the upgraded client. The synthetic
primary is shared by those test clients; no real primary requests or endpoints are test data.

**P2 floors unchanged (independent F4):** O11–O20 require real SQLite replay/crash,
two instances with a reader between commits, atomic effect/source-key/receipt/checkpoint,
limits+1/outage recovery, late and permanently missing parents, 401-row paging,
tombstones/version rejection, forbidden mutations, rebuild equality and **actual rollback**.
The static C# replay/cursor risk remains **Inferred**, not upgraded by the Python diagnostic.

### Commands / oracle integrity

P1 should add a discoverable stdlib unittest module for the official Python focal
functions, then execute `python -m unittest discover -s <admitted-test-directory> -v`;
the Proof Pack must name the actual path and nonzero executed case count. No fictional
current test file is supplied here. A directly runnable **red control** for the existing
fold, without live queue writes, is:

```powershell
python -c "import importlib.util,sys; from pathlib import Path; p=Path('docs/ai-forward-pack/scripts/coord-core.py'); sys.path.insert(0,str(p.parent)); s=importlib.util.spec_from_file_location('coord',p); c=importlib.util.module_from_spec(s); s.loader.exec_module(c); q={'kind':'request-add','id':'synthetic-q','at':1}; r={'kind':'request-resolve','id':'synthetic-q','at':2}; assert c.fold_requests([q,r,q])[0]['status']=='resolved', 'transport replay must not reopen'"
```

The command above was specified by the original P0 author and **was not executed by this
delta author**. Supplied independent-review evidence instead executed an **extracted
current fold**, with duplicate-add replay **RED: actual open != expected resolved** and
ordinary legacy add→resolve **PASS**. This is a narrow isolated Python diagnostic,
**not official CLI execution, full O03/O08/O10 evidence or C# reproduction**. P1 still
owes discoverable official-code red→green tests; no tests are run in this correction.

C# seam suite after adding named tests:
`dotnet test tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj --filter "FullyQualifiedName~Watcher"`.
Capture exact selected names, nonzero counts and red→green evidence; that broad command
alone does not prove O11–O20. Use real SQLite/two connections and explicit scheduling
barriers. The existing no-double-register test cannot substitute for O11. No GUI/App/full
qualification is authorized by these commands.

## 5. Operator and rollback questions

1. Which authenticated actual-human decision channel can the trusted registrar verify,
   and how are revocation/supersession and human scope checked offline?
2. Which coexistence mechanism can prove mixed-client contention/complete-record/conflict
   safety without upgrading, enrolling or disabling unchanged legacy clients? Enhanced
   capability enrollment alone and a new cooperative lock cannot protect old unlocked append.
3. Which concrete additive application/feed/checkpoint representation will Data and DS
   coapprove before P2 code behind the existing watcher observation-store seam in existing
   watcher.db? Placement is bounded; inherited ADR-0023 divergence is debt, not conformance.
4. What are pilot queue/byte/attempt bounds, disk reserve, retention durations, policy
   deletion basis and backup expiry? Who owns a permanently missing parent?
5. Which installed endpoint modes can actually wake/consume, and which remain BLOCKED?
6. Who can attest actual holder release after run failure/expiry without touching peer slots?

No unanswered operator question can be resolved by a timeout-to-approval rule.

## 6. Original P0 validation record (historical, not re-executed by this delta)

Executed: official CLI help/start/worktree registration/ADR allocation/exact-path claims;
complete investigation read; sole §2 ownership read; relevant architecture/US4/ADRs;
fresh scoped source and referenced test reads; local commit/blob/working-hash lookup.
No C# runtime tests, migration up/down, query plan, endpoint consumption, latency
percentiles or security authority positive fixture have been executed.

Documentation-only closeout:

* Official `docs-graph.py validate`: **exit 1**, 525 artifacts, **zero schema/link
  problems, zero orphans, zero date-stale artifacts**; five defects are exactly the
  five new artifacts not yet in the derived index. **74 existing review-suggested
  flags** remain. This is not a clean full documentation gate.
* Initial frontmatter validation exposed two unregistered types and one relation;
  corrected using the actual type/relation registry. No unresolved frontmatter defect.
* Actual index artifact links verified the stated ADR→architecture→spec→authority
  traversal. No top-level `edges` assumption remains.
* `git diff --check`: passed on tracked changes. Final staged check also required
  for the new files; no runtime correctness inferred from whitespace.
* Official audit `al-01M2NJNPENFDV441RY54BJFWCQ` and change
  `cl-01M2NJNPKE3B9BWR5FDWRR1PJZ` record **partial / draft**, not authority or
  independent acceptance. `audit-log.py verify`: **763 audit + 156 change,
  zero unreadable lines**.
* Baseline comparison proved each audit register preserved every prior line and
  appended exactly one own entry (762→763; 155→156). Official append/change
  automatically rendered `docs/audit/audit-data.js`; only that own generated change
  was restored, leaving the conductor to regenerate at checkpoint. No site or
  whole-documentation generation was run or committed by this author.
* No source edits, dependencies, runtime tests, migration, endpoint/nonce probes,
  peer slots, observer changes or nested agents.

Conductor must regenerate derived index/audit views/security/privacy rollups and
validate them at checkpoint. Do not label deferred derived data as a clean site build.

## 7. P0-delta textual correction receipt — partial, not acceptance

| Independent item | Exact correction recorded; evidence still owed |
|---|---|
| F1 — Blocker / Verified compatibility design defect | Replaced upgrade-all/serialized-old-command compatibility with unchanged unenrolled old-worktree official request-add/request-resolve/list on the same primary requests.jsonl. Enrollment gates enhanced capabilities only. O08/O10 unmodified pre-P1 add→resolve→list/rollback IDs+payloads, plus separate mixed-client contention/complete-record/conflict safety before enhanced activation; old append unlocked, no upgraded-shim substitute or disabled legacy client |
| F2 — Major / Verified bounded-store ruling | Existing watcher observation-store seam and existing watcher.db used by WatcherHost/MCP; additive application/feed/checkpoint caches only, requests.jsonl sole canonical source. No new DB/native relocation/workspace.db migration/cross-DB transaction. ADR-0023 physical divergence inherited debt, not conformance; Data/DS coapproval of concrete representation before P2 code |
| F3 — Major / Verified admission scoping | Independent P0-delta clearance admits ONLY compatible dormant P1 fold/envelope/schema-validation and isolated tests, not deployed SQLite schema or full P1 completion. All privileged production paths deny absent qualified verifier evidence; O07 includes apparently valid synthetic receipt and zero grants/transfers/sends/launches. Synthetic positives are deterministic contract evidence only; authenticated fixture, channel qualification and P3/P4 activation BLOCKED; prose inert |
| F4 — mandatory P2 floors / narrow diagnostic | O11–O20 and additional store assertions unchanged; static replay/cursor risk remains Inferred. Independent extracted-current-fold duplicate-add RED open!=resolved and ordinary legacy resolution PASS recorded as isolated diagnostic, not CLI/C# evidence or tests executed in this delta |
| F5 — unresolved privacy and endpoint evidence | Live retention/erasure across source/cache/export/endpoint/backup payload copies unresolved. Every foreground/background GHCP/Codex/Grok/Claude positive arrival/consumption/supported-wake cell BLOCKED pending supported availability; fakes do not clear |
| F6 — reporting honesty | Proposed SLIs remain unmeasured; docs index/audit views/security/privacy rollups pending conductor. Official audit records partial draft correction, no acceptance/verification-executed signal manufactured |

This bounded delta makes no source/test changes, executes no runtime tests, and uses no
agents or real endpoints. A shell `rg` discovery attempt was unavailable; bounded Python
source inspection supplied official command syntax instead. Original source/test pins
and validation outputs above are historical, not rerun claims. The ordinary-resolution
PASS belongs solely to the supplied independent isolated diagnostic.

| Phase | Current state |
|---|---|
| P0 | Corrected draft; **GATE P0-delta pending independent review** |
| P1 | Pending implementation; dormant subset only after independent gate; does not complete P1 |
| P2 | Pending; concrete additive representation Data/DS coapproval before code; real-store proof owed |
| P3 | Pending; real endpoint conformance/activation BLOCKED |
| P4 | Pending; qualified authority/launcher activation BLOCKED |
| P5 | Pending; same-corpus/UX/SLI evidence owed, no measured SLIs |

**Next: independent delta reviewer on the exact correction commit, then conductor-admitted
Python P1 author. No self-clearance; no renewed human approval of the already approved phases.**
