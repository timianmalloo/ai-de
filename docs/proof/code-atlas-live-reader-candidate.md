---
id: proof-code-atlas-live-reader-candidate
title: "Code Atlas live-reader candidate - staged execution evidence"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "atlas-live-reader-producers"
tags: [code-atlas, proof-pack, live-reader, worktrees]
links:
  - { to: note-atlas-live-reader-horizon, rel: depends-on }
  - { to: proof-code-atlas-identity-unit, rel: depends-on }
  - { to: proof-code-atlas-enumeration-safety, rel: depends-on }
  - { to: coordination-code-atlas, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Records the cleared common foundation and separate producer dispatch. This ledger does not
  claim a completed native reader: inventory, declarations, source/query/native assembly and
  independent real-workspace proof are recorded by stage as their evidence arrives.
---

# Current evidence boundary

| Stage | State | Evidence / limit |
|---|---|---|
| Identity/binding first unit | Joined | `proof-code-atlas-identity-unit`; pure values, not filesystem or native behavior |
| Directory-enumeration investigation | Joined | `proof-code-atlas-enumeration-safety`; 29 passing cases, two NOT_PROVEN symlink cases; restricted ordinary-local evidence |
| F common foundation | Cleared and joined | Final worker `07d877ce`, Conductor pin `02695471`; 63 independently executed passing tests |
| E inventory producer | Repaired and joined | Candidate `c7f6caf9`, Conductor `d947fcee`; 95 producer tests and 110 combined F/E/D tests independently passed |
| D declaration producer | Repaired and joined | Candidate `0c426cd1`, Conductor `7584c0ae`; 78 independently executed passing tests |
| Source/query/native assembly | Not dispatched | Depends on producer join and its next explicit allocation |
| Independent detached native journey | Not executed | No tree/member/source/Back or real-workspace acceptance claim yet |

## F common foundation

The original eight-file proposal `34a9e652` extracted the shared tuple encoder but did not
satisfy the frozen reading contract. The Conductor independently observed 53 passing tests and
still held the proposal on actual model gaps. Source-only and test reviews identified invalid
defaults, inconsistent bounds, an outline string/five-state source enum, forced numeric coverage,
failure observations requiring fabricated metadata, unknown-profile identity and unchecked
manifest membership.

The same writer produced `bac95e92` and `07d877ce`. Three retained semantic failures targeted:

- `Create_ReturnedRowsExceedLimitOrKnownTotal_Throws`
- `Create_ManifestContradictoryBinding_ThrowsEvenWhenPartial`
- `Create_NonMatchSourceProjectionWithText_ThrowsAndUnknownCoverageHasNoNumericFiction`

The final one-test red was
`Create_PublicSourceConstructorWithNonVerifiedStatus_Throws`. It detected a public
all-fields constructor that bypassed the state-specific factories; the corrected constructor
permits verified observations only.

Final code provides validated reference identities/requests/limits, typed immutable outline and
source projections, explicit unknown/withheld coverage, state-dependent source observations,
reserved-unknown profile handling and cross-reference/binding checks even for partial manifests.
These are passive model/port contracts. They do not implement root issuance, I/O, compiler
collection, queries or native UI.

### Independent execution

The Conductor ran the targeted Understanding test set against the candidate and then the joined
branch:

```powershell
dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~AiDe.Core.Tests.Understanding" --logger "trx;LogFileName=<run>.trx" --results-directory <session-evidence-directory>
```

Both final runs executed **63 tests, 63 passed, zero failed/skipped**. Retained worker evidence:

- `TestResults/atlas-live-core-red/atlas-live-core-mutant-red.trx`: one failed codec golden mutant.
- `TestResults/atlas-live-core-phaseb-red/atlas-live-core-phaseb-semantic-red.trx`: three semantic failures.
- `TestResults/atlas-live-core-phasec-red/atlas-live-core-phasec-red.trx`: the constructor-escape failure.
- `TestResults/atlas-live-core-phasec-green/atlas-live-core-phasec-green.trx`: 63/63.

Parent outputs are retained in this session's `files/atlas-foundation-final` and
`files/atlas-foundation-joined` directories. The original reported missing-TRX finding was
withdrawn after direct absolute-path XML reads: ignored-file glob omission was not absence.

Data reviewer `91c51d36-21fc-4c00-8cf4-57fa50a1cb00` cleared the final constructor blocker;
Test Architect `e8c73a03-3fa2-4a77-8d17-68cdf80188b1` cleared the model-contract oracles.
The Conductor inspected the exact final guard and observed both final runs. No clearance is
promoted to producer or native behavior.

### Join and accounting

Candidate commits joined as `8808a1b2`, `b12c05c0`, `02695471`. A live F lease initially
refused the join; the actual worker released all eight leases and ended its coordination session
before the Conductor retried. The primary checkout was not used for Atlas integration.

The writer reported 32 calls against F's initial 18, followed by ten correction calls and four
constructor-close calls. These reported costs and the original overrun remain visible. Owner
turn 13 allocated a 96-call F/E/D ceiling; budgets are not retroactive acceptance.

## Producer pin and no-overlap contract

E and D both start at `02695471264cfe0ae29f9a5a2784070cdc0f11b4`, in separate registered
worktrees. E owns only inventory/directory-enumerator source and tests; D owns only declaration
observation source and tests. Neither edits frozen F files. The sole section-2 register carries
their exact grants, and the Conductor controls review and local merges.

The next evidence must demonstrate actual authorized file inventory and actual compiler
declarations/ranges from complete hash-bound inputs. Fake graph fixtures, display strings as
identity, fabricated project/TFM context, unbound source rereads and a supplied DTO collection
presented as a live native journey are not accepted substitutes.

## E inventory producer

The initial proposal `e54f21e1` passed 73 tests but was held on actual native/resource/policy
defects. Owner assigned the same four-file scope to a separate execution-capable Astra writer.
The old writer's leases were checked free; a remaining coordination registration was corrected
by that writer using the actual session-end command and state readback.

The replacement's first 30-call proposal retained 92 passing and three failing tests. The bounded
investigation `investigation-code-atlas-native-repair-controls` separated metadata-only handle
sharing from junction teardown. After Owner approval, the exact three failures passed, then all
95 targeted tests passed. The Conductor independently repeated 95/95 at `c7f6caf9`.

The fixed producer enforces current/expiring grants before and during work, expected native root
identity, same-handle metadata, operation-local resource state, bounded collection and descriptor
accounting, typed errors, actual parent keys, explicit known/non-Git/unavailable membership,
and zero returned source-content bytes for metadata results. Unknown membership does not become
authorization. Core keeps raw grant/session/root details; later UI/IPC projections must be redacted.

Security reviewer `3facf06b-883c-4039-a461-51b93a236f11` and Test Architect
`e8c73a03-3fa2-4a77-8d17-68cdf80188b1` cleared bounded candidate Core incorporation with the
ordinary-local/reparse-exclusion, grant-current predicate and private-grant conditions retained.
No symlink or universal race-free claim is made.

The two E commits joined as `5a4cdede` and `d947fcee`. The combined F/E/D run executed
**110 tests, 110 passed, zero failed/skipped**. Parent TRX is in this session's
`files/atlas-producers-joined`; candidate red/intermediate/final artifacts remain in the E
repair tree's `.e-repair-results` directory. Reported replacement expenditure is 30 + 6 leaf
calls, separate from the original E allowance and handoff overhead.

No live-user-workspace, source-content, query/receipt, native UI or E0 completion is asserted.

## D declaration producer

The initial two-file proposal `00a85823` passed 70 tests but was held after code review:
text-only tree mapping, traversal-dependent occurrence keys, uncapped/public buffers and nested
double traversal did not satisfy the frozen contract. The writer's reported 37 leaf calls against
24 remain an overrun, not 19 top-level calls substituted for leaf execution.

Owner authorized one 20-leaf correction. The Conductor read a response-only repair contract before
release. `0c426cd1` implements explicit syntax-tree/file/root/source binding with exact text/hash,
stable own-occurrence keys, an 8 MiB pre-clone limit, admitted BOM-directed decoding, internal
disposable source leases, source-only single traversal and deduplication before cap accounting.
FileLimited remains observation-only and does not become a real project/TFM claim.

The Conductor read seven retained semantic red failures:

- identical-content files remain distinct;
- wrong file/tree mapping rejects;
- unrelated earlier files do not change an occurrence key;
- oversized/BOM-less inputs reject;
- disposed source leases reject use;
- nested/exact-limit traversal is not charged for duplicates;
- unimplemented partial definitions keep their role and limitation.

Candidate and joined runs each executed **78 tests, 78 passed, zero failed/skipped**. The retained
red is `tests/AiDe.Core.Tests/TestResults/atlas-declarations-phaseb-red.trx`; parent results are
in this session's `files/atlas-declarations-repaired` and `files/atlas-declarations-joined`.
Data reviewer `91c51d36-21fc-4c00-8cf4-57fa50a1cb00` cleared the D data-contract blockers.

Commits joined as `0711be09` and `7584c0ae`. The repair was reported as 12/20 leaf calls.
No files outside the two-file grant changed. This proves a non-I/O declaration producer over
supplied bound inputs, not a project loader, filesystem authority, source reader, UI or E0 delivery.
