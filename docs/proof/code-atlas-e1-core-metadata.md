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
| Test | Initial 4/4 BLOCK; one immutable SP2 completion read identifies an empty-accessor-subset oracle gap, with fixture delivery still blocked. Owner 81 correction/readback follows below |
| C# | Initial 4/4 incomplete; three exact immutable packet views complete the unread paths and return PASS advisory. No runtime or clean-fixture acceptance inferred |
| DS | Original 4/4 clearance withdrawn for wrong physical tree. Four new immutable packet views clear Q/transport/paging within scope; issuer insertion details are not independently source-read in that completion and remain separate Security/C#/parent evidence |

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

## Owner 80 fixture checkpoint: source reproduced, binary gate blocked

The author stopped at the required gate after 16/16 leaves; fixture wiring and
expected hash checks were not changed. Four candidate delivery files remain
uncommitted in `atlas/e1-legacy-fixture`; no completed materializer is claimed.
The author reports 524 own-repository build inputs with licensing/provenance.
Parent read the blocked receipt and independently hashed both output roots and
the exact patched peer source.

| Input / output | Observed identity |
|---|---|
| Source archive | `4FBC05719148617AFA5D9C5D7D67B3CA5D84F0FD1294C575CBC0255941ED5D88` |
| Normalized peer patch | `9639153170D247C8D0587DF09502E82414280D4EA5C77E080944FF18991DE0F4` |
| Peer source, both roots | `6E7AC5C69FD1660B8E19D16BAA8ECB898F0E818599AB11DDD2C3B6668F78DC1B` |
| `prepare-e` Core | `1D12914FA288AFE8C9DCE7C8268EF819083FB6DC1B15D9F959F6D32F75CA9720` |
| `prepare-f` Core | `19A828C5BBA6128200C4CA9B0FEC56BC3DE93558E0DD99F594A3CC602B55AEF7` |
| `prepare-e` peer tests | `DB554AB668F9C5F5BF6E4F2B49338314933E04223214E6ED3417C147BAF493DF` |
| `prepare-f` peer tests | `1099315618CD60B58849557C5027F135A666E8FAFA2C03AEAF091BBC463F219E` |

The two builds do not reproduce each other or the retained reference binary
pair. Their informational versions name baseline `b91d4bb5`, but the generated
SourceLink files, read by the parent, map the ambient fixture checkout to
`cea76307`. The recorded PE observations also contain different absolute PDB
paths for each preparation root. These are observed provenance differences,
**not proof that every differing byte is explained**.

The existing reference hashes remain intact. A deterministic compiler/PDB path
mapping and explicit baseline SourceRoot/SourceLink contract has been requested
from Owner. A new canonical hash pair cannot simply be whatever a build produces:
two fresh roots must agree, source/tool/build provenance must remain pinned, and
real compatibility must execute before any such decision.

Retained evidence:
`C:\Projects\ai-de-atlas-e1-legacy-fixture\.artifacts\owner80\`
contains `blocked-delivery-receipt.json`, `two-root-v3-results.json`,
`binary-path-observations.json`, patch-normalization results and per-root
`preparation.json` / build logs. Ordinary test delivery, hostile-archive
qualification, fixture wiring, independent delivery review and commit remain
unmet. No clean-clone claim is made.

## Separate Owner 81 accessor-oracle correction

Commit `1791f95dde2116efc5e5f5ace1760f3cd10f6474` changes only
`AtlasStaticObservationTests.cs`; it does not alter the product or the fixture
checkpoint. Both accessor loops now require exact identifier multisets:
`["get","set"]` and `["add","remove"]`, without deduplication. Empty and duplicate
subsets exercise the same assertions and produce captured `Assert.Equal`
failures. Parent read the diff and raw controls, then replayed the actual
producer tests: **9 executed / 9 passed / zero failed**.

The parent receipt is session files
`atlas-e1-accessor-independent/parent-accessor-oracle.trx`. One immutable
source/execution packet supplies the separately funded Test readback. Author
usage is 3/3 leaves, separate from the blocked 16/16 fixture pass. Independent
readback and any join remain pending at this checkpoint.
