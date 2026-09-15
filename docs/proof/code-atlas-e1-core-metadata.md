---
id: proof-code-atlas-e1-core-metadata
title: "Code Atlas E1 Core metadata: candidate evidence and delivery gates"
type: doc
status: draft
owner: "@timianmalloo"
tags: [code-atlas, e1, core, compatibility, paging, proof]
links:
  - { to: design-code-atlas-e1-static-views, rel: relates-to }
  - { to: proof-code-atlas-e1-qualification, rel: depends-on }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
review-by: 2026-09-22
summary: >
  Frozen E1 Core candidate with executed metadata, compatibility, paging and
  publication evidence. The supplied-fixture 494-case replay is valid, but
  reproducible legacy-fixture delivery and incomplete reviews still block joining.
---

# Core capability evidence, not native-view or programme acceptance

Owner 78 admitted eleven exact Core/test paths. Candidate
`cea76307fe75b943a945cf837139e6a80ea2595d` is based on the joined qualification
`b91d4bb5b0f59352b7f90b94e0b2733a647452dc`. Parent inspected the aggregate
diff, all production changes and new test flows. Exactly eleven files changed:
702 additions and 35 deletions. All eleven source hashes match the frozen receipt.
No App, daemon, factory, framing, generic-client, dependency, project or store
change occurred.

Parent replayed Core Understanding/IPC with the supplied genuine legacy fixture:
**494 executed, 494 passed, zero failed/skipped**. The unchanged App also built
with zero warnings/errors. These observations do not prove native E1 rendering.
The candidate remains unjoined under Owner 79/80.

## Candidate contract

These describe this frozen candidate, not an already admitted client contract:

- `AtlasSelectRequestDto` adds optional `bool? StaticStructure = null`.
  The remote reader sends true only for explicit true plus advertised
  `static-structure-v1`; otherwise it sends null, omitted from JSON.
- `AtlasOutlineRowDto.Structure` is nullable and omitted when absent. Its
  descriptive metadata carries classifier flavor, parent state/token,
  provenance and a reason.
- Parent tokens are issued only for returned page occurrences. `OutsidePage`
  has no parent token. Namespace/file context is not a declaration parent.
- Immediate parentage comes from verified syntax associations, not names,
  semantic containing types or App span reconstruction. Partial and overloaded
  occurrences remain separate.
- RESTORE uses the normalized original request preference. Malformed opted-in
  metadata fails validation; it does not become a successful legacy response.

## Executed claim-to-evidence ledger

| Claim | Concrete path and oracle | Observation / confidence | Remaining boundary |
|---|---|---|---|
| Producer emits lexical structure | Actual verified-buffer Roslyn observer; expected property/accessor and nested-type keys/flavors | Initial semantic red: producer-derived metadata absent; candidate test passes | Independent SP2 review was incomplete; nonempty accessor-subset oracles remain under review |
| Explicit opt-in has a real codec path | Current DTO/serializer; legacy request mutation with `staticStructure=true` | Initial semantic red: unknown property; candidate passes | Full old/new behavior depends on the genuine frozen peer below |
| Mixed-version SELECT/RESTORE remains legacy-shaped | Actual old-peer process, current registration/remote reader, both client/server roles | Both mixed roles pass; legacy outline rows omit `structure`, not null it | Fixture must be provisioned reproducibly |
| Current preference is not substituted during Back | Real SELECT, opposite-preference SELECT, RESTORE original receipt | Both opted-in and non-opted cases preserve original output preference | Not native UI focus/Back rendering |
| Malformed metadata fails before successful response adoption | Actual frame mutation plus raw codec validation and terminal remote query | Six variants pass; deleting required-flavor validation produces no-exception red | Invented final RESTORE token is not an independent receipt-adoption-state oracle |
| Core pagination crosses the former first page | Actual reader over 151 declarations | 128 + 23 disjoint tokens; off-page parents have no token; original missing-next-offset red retained | Bounded file profile, not enterprise/global completeness |
| Far-member source/Restore uses correct units | Real member after UTF-16 offset 32768, CRLF and non-BMP input | Exact substring and highlight equality; split-scalar refusal; valid `😀` + CR page has 3 UTF-16 units / 5 UTF-8 bytes | Native view navigation/rendering still separate |
| New structure participates in Q pricing | Actual manifest-pricing method, structured versus equivalent unstructured manifests | Removing metadata charge gives expected 9004 / actual 0; restored pricing passes | Conservative storage estimate, not heap measurement |
| Metadata publication retains ownership through writer drain | Real escaped response writer held before completion, matched response drain | Blocked: one native owner/five buffers, 16 MiB owned / 4 MiB retained; after drain: native zero, idle 2 MiB / 4 MiB | This observed held write is not every failure/interleaving |
| Measurement exists on the normal path | Existing Q retained metric plus new declaration-handle charge metric | Actual retained observations 189392 and 4844; handle charges 1023 and 1777; fixed kind label | Counts/charges are not exact CLR/Roslyn memory |

The escaped response measured **788372 bytes including its four-byte prefix**,
131072 source-content bytes and zero metadata-content bytes. It is real escaped
frame evidence below the 1 MiB body ceiling, not an exact-limit/one-over proof.

## Evidence identities and locations

Author root:
`C:\Projects\ai-de-atlas-e1-core-metadata\.artifacts\owner78-core-metadata`.

| Evidence | Retained artifact |
|---|---|
| Final pin, eleven source hashes, recipes and limitations | `author-final-receipt.json` |
| Final author run | `final-core/owner78-final-core.trx` - 494/494 |
| Initial semantic red | `semantic-red/owner78-semantic-red.trx` - 3 failed, 1 passed |
| Charge/schema faults | `charge-schema-red/owner78-charge-schema-red.trx` - 2 failed, 5 passed |
| Source freezes | `final-source/`, `full-v2-source/`, `charge-schema-red-source/`, `legacy-source/` |
| Native evidence | `final-idle/`, `final-publication/` |
| Genuine peer | `legacy-peer-qualified-bin/`, `legacy-peer-qualified-manifest.json`, qualified source/patch |

Earlier exploratory runs have logs/TRXs but not complete contemporaneous source
bundles. They are not promoted to frozen-candidate evidence.

Parent root is session files `atlas-e1-core-independent/`:
`parent-e1-core.trx`, new `idle/` and `publication/` receipts. Parent set
`ATLAS_FROZEN_LEGACY_DIRECTORY` to the retained qualified peer directory and
ran `dotnet test ... --filter "FullyQualifiedName~Understanding|FullyQualifiedName~Ipc"`.
The exact manifest and both fixture hashes were read back:

| Genuine artifact | SHA-256 |
|---|---|
| Legacy Core, based on `b91d4bb5` | `6353AE7ED46937FE3D47CB510BD41D192DA53C0F7E38EF5F5731726E8EB1A0DB` |
| Compiled, executed qualified peer tests | `641BDFADB1834748BBAF758F41508A593EEF300CB0960B286BB57C00623030DA` |

The earlier unqualified peer assembly after a compile failure is explicitly not
the peer witness. Frozen sources/patches and tool/build provenance must accompany
delivery; a binary name alone establishes neither origin nor compatibility.

## Reproducibility gap

The compatibility tests require an existing directory and both hashes. At present
those files are untracked author artifacts. Parent ran the two mixed-peer cases
with an explicit unique non-materialized fixture directory: **two executed,
two failed**, at the expected prerequisite assertion. This is a measured
missing-fixture scenario, **not a literal clean-clone run**.

Its separate retained result is session files
`atlas-e1-core-missing-fixture/missing-legacy-fixture.trx`. It does not invalidate
the fixture-present 494/494 run; it prevents calling that run portable delivery.
No fixture fetch, skip or weakened compatibility check was substituted.

## Review state and exact coverage corrections

| Lens | Current disposition |
|---|---|
| Security, 4/4 | Conditional source clearance; no authority-expansion/malformed-fallback defect in inspected paths. Fixture delivery remains open; invented RESTORE token limits adoption-state claims |
| Data, 4 reported leaves | Conditional model clearance; pinned physical candidate, HEAD checked, not immutable blob reads. Unvalidated native record becomes a concern if reused by external/persistent producers |
| Test, 4/4 | BLOCK on fixture delivery and unread SP2 range after a failed range command |
| C#, 4/4 | Review incomplete after two failed readers and truncation; no established code defect, unread Q/issuer/remote paths |
| DS, 4/4 | Prior clearance withdrawn: physical old runtime tree was read without establishing the candidate pin; no current DS clearance |

Owner 80 funds exact completion reads: DS four, C# three, Test one. Parent
generated session-local packets directly from immutable `cea76307` Git blobs
with commit/blob identity, paths and original line numbers. Packets are in
`atlas-e1-core-review-packets-cea76307/`; reviewers use built-in views rather than
another range script. Prior failed/wrong-tree reads remain recorded, not erased.

## Authorized fixture-delivery checkpoint, not completed work

Owner 80 authorizes a sixteen-leaf source-based preparation checkpoint in the
separate `atlas/e1-legacy-fixture` tree at `cea76307`. Only the materializer,
baseline-source archive, peer patch, manifest and preparation wiring in the
existing compatibility test are writable; exact ownership is in the sole
collaboration register, not duplicated here.

The checkpoint must establish genuine inputs, tools/versions/build settings,
safe extraction and owned output boundaries; prepare in two fresh owned roots;
and execute ordinary tests without the author's artifact/environment prerequisite.
It must preserve expected hashes until reproducibility is established. A
path/SDK/SourceLink-induced hash difference is a finding for an explicit
provenance-preserving decision, not permission to accept arbitrary bytes.
No third-party binary bundle, private corpus, credentials, generated cache,
silent download or muted test is allowed. Extra project/content-copy paths
require a new exact grant.

The source candidate, native view and programme remain unaccepted pending the
completed source gates and fixture-delivery disposition.
