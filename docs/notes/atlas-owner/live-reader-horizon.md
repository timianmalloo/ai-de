---
id: note-atlas-live-reader-horizon
title: "Atlas Owner - one codec and a real detached inventory-to-source journey"
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [code-atlas, owner, live-reader, coordination]
links:
  - { to: note-atlas-candidate-first-unit, rel: refines }
  - { to: proof-code-atlas-identity-unit, rel: depends-on }
  - { to: proof-code-atlas-source-safety-join, rel: depends-on }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Owner turn 12 selects the implemented tuple codec, admits a real detached reading-loop
  horizon, and keeps shared adapters/main integration separate from new-file authoring.
---

# Decisions

Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 12:

1. **One canonical logical-ID codec.** Reuse the implemented versioned length-prefixed
   UTF-8/Base64 tuple encoding. Preserve current symbol Values exactly; add explicit domain/version
   tags for file and compilation-scope tuples. Remove the competing proposed JSON-to-hashed-ID
   design before persistence. Persist canonical Values unchanged; any future hash index is
   secondary and must retain full-value equality. Golden vectors cover every domain.
2. **Real inventory -> declaration outline -> bound source -> Back, in a detached native reader.**
   Safe directory enumeration is a prerequisite, not a property inferred from the source-read
   probe. Actual compiler declarations and validated source spans are required; supplied fake
   graph data is not product evidence.
3. **New files, not unacknowledged shared adapters.** SH3's observed merge/clean state reduces
   conflict risk but is not agreement. Keep existing Core/host adapters and CV2 untouched. Record
   required future adapter deltas rather than applying them.

The first two decisions were labelled Verified against opened code/design; the shared-boundary
decision was Inferred from the supplied integration observations. No main-integration permission,
source-safety certification, wire/storage compatibility or integrated E0 acceptance is implied.

## Horizon allowances and gates

| Checkpoint | Allowance | Exit |
|---|---|---|
| Common contract freeze | 10 calls | Codec, enumeration port, manifest/query/selection contracts, bounds and semantic falsifiers returned before dependent implementation. No whole-document redrafting. |
| Enumeration investigation | 25 calls | Synthetic ordinary local directories, mutation/cancel/resource/link cases actually exercised; unsupported cases named. No privilege changes or private corpus. |
| Candidate implementation | 60 calls aggregate | Actual physical inventory and compiler declaration/member journey, validated source and Back; native presentation consumes Core projections, never filesystem/provider access. |
| Independent checkpoint proof | 15 calls | After enumeration admission, explicitly authorized AI-DE workspace read-only; unsupported files visible, identity consistent, changed bytes/Back honest, spans/decoder/focus/selection observed. Detached candidate, not registered-host E0. |

These are new horizon allowances. The preceding candidate's reported 46/60 expenditure and
six-to-ten boundary-close overrun remain recorded; they are not refunded or rewritten.
After common contracts freeze, enumeration/inventory and declarations may use two disjoint
writers. Query/native assembly follows the producer join. Width stays at most four; no worker
fan-out; the Conductor assigns exact files, releases gates and controls local merges.

## Manifest ceiling, not an instruction to create everything

Core Understanding: existing `AtlasIdentity.cs`; new `AtlasIdentityCodec.cs`, `AtlasManifest.cs`,
`AtlasQueryContracts.cs`, `AtlasInventory.cs`, `AtlasDirectoryEnumerator.cs`,
`CSharpDeclarationObservation.cs`, `AtlasSource.cs`, `AtlasQueryService.cs`.
Corresponding `<name>Tests.cs` files are allowed under the dedicated Core test directory,
including updates to `AtlasIdentityTests.cs`.

Native: `AtlasReaderView.cs` in App Understanding and its dedicated
`tests/AiDe.App.Tests/Workbench/Understanding/AtlasReaderViewTests.cs`.
Enumeration probe: four files named in the section-2 exception.
Detached runner: `spikes/code-atlas-reader-candidate/CodeAtlas.ReaderCandidate.csproj` and
`Program.cs`.
Proof: `docs/proof/code-atlas-enumeration-safety.md` and
`docs/proof/code-atlas-live-reader-candidate.md`.

Register each actual subset/writer before dispatch. No unused files. Existing `WorkspaceCore`,
extractor, query/projection, IPC client/operations, factory, host, adapter and layout files remain
outside the grant. Every agent has its own worktree from an observed current-main-plus-reviewed-Atlas
baseline. The primary checkout and Claude's integration authority remain untouched.

## Frozen common contract for the first implementation assignment

The Astra contract author returned a response-only receipt after seven of ten calls. The
Conductor read it and adopted the following narrow Data-lens decisions before dispatch. This
section, not a worker's interpretation of an unfinished whole document, is the common contract.

### One codec

Extract the existing encoding implementation without changing existing symbol Values:
`UTF8-byte-length:Base64(UTF8(component));`, invariant decimal, ordinal values and strict
Unicode validation. Existing nine components remain exactly:
`atlas-identity/v1, type|member, scope, project, framework, language, symbol.Kind, assemblyName,
documentationId`. Do not change partial canonicalization.

New file tuple: `atlas-file/v1, workspace, root, relativePath`.
New compilation tuple: `atlas-compilation-scope/v1, workspace, root, contextKind, projectToken,
frameworkToken, configurationOrProfileToken`.
No JSON/hash identity aliases, trimming, case folding or Unicode normalization. Future storage
keeps the canonical Value unchanged. A hash is not equality.

### Common records and ownership

- **RootGrant:** Core-issued immutable grant/version, workspace/root/policy/session tokens,
  approved absolute root, **expected native root identity** (volume serial + file index),
  expiry. Construction is Core-internal; a UI claim or a root string does not issue authority.
  Enumeration validates the opened object against that pre-existing identity.
- **ObjectIdentity:** volume serial + file index. Size/time/link count are observations, not
  identity. Missing identity is explicit absence/refusal, never an all-zero substitute.
- **DirectoryEntry/Observation:** relative path, file/directory/rejected-link/unavailable kind,
  optional object identity/length/link count, reason; ordered immutable entries, expected/observed
  root, completion, sequence and bounds. No source text.
- **AtlasFileEntry:** file Value, relative/parent path key, kind, classification, observed identity,
  availability and stable reason. Unknown files remain visible where authorized.
- **SourceObservation:** manifest/file/policy, root/file identities, canonical SHA-256, byte length,
  decoder id, decoded UTF-16 length, status and bounds; no retained body.
- **Declaration:** logical symbol Value when established, observation key, file/context/source
  binding, kind/role/display signature, identifier/declaration/body UTF-16 spans and counterpart
  or unresolved reason. A null body span is absent, not zero length.
- **Manifest:** Core-issued token, immutable root/policy/observation identity, files, source
  observations, declarations, completion/coverage/bounds. In-memory only; no compiler objects,
  bodies, database or event stream.
- **Bounds:** requested/effective limits, returned rows/bytes, nullable total and
  Known/Unknown/Withheld denominator state, permitted omissions and limiting dimension.

Use the smallest immutable vocabulary inside the three assigned common files. Records validate
their invariants; no mutable collection escape or valid-looking default. Do not create extra
ports/serialization/navigation files or a general framework.

### Source and compiler data flow

One Core-only, non-serializable request-local **VerifiedSourceBuffer** owns the full verified bytes
and decoded text. The declaration producer consumes the entire exact decoded text from that
buffer, never a UI page and never a second unbound path read. Display pages are projections of
the same validated observation. Ending buffer lifetime does not permit a path-only reload under
an old selection; a later read must revalidate all expected binding components.

Buffer implementation and filesystem calls are subsequent assignments. Common types must not
implement a fake buffer/reader or store text in a manifest merely to make the contract compile.
No text on mismatch/refusal/unknown/unstable/canceled results.

**FileLimited** is explicitly a limited compiler context, with a known trusted reference/options
profile and file/root namespace, not an invented project or TFM. Its keys cannot equal later
true project-compilation keys. Project/TFM display state is "not established". If that profile is
missing/unstable, logical identity is unavailable and observation keys alone support navigation.
Do not pass fictional project/TFM tokens through the existing project-qualified factories as if
they were facts. `SuppliedProjectCompilation` still needs its real approved input adapter.

### Frozen ports and native selection shape

- `IAtlasDirectoryEnumerator.EnumerateAsync(RootGrant, EnumerationLimits, CancellationToken)`
  returns a `DirectoryObservation`; it cannot recapture root identity as authority.
- Declaration observation consumes explicit compilation context, an actual approved
  `CSharpCompilation`, and complete bound decoded sources. It performs no project/FS execution.
- `IAtlasQueries.InventoryAsync(PageRequest, CancellationToken)` returns an inventory page.
- `SelectAsync(SelectionRequest, CancellationToken)` takes manifest/file/optional declaration
  observation key/request sequence, not caller-issued source bindings.
- `RestoreAsync(issuedReceiptToken, requestSequence, CancellationToken)` revalidates a Core-issued
  receipt. Missing/retired receipts are unavailable; changed bytes clear text/anchors rather than
  substituting the latest version.
- `SelectionProjection` includes accepted identities, generation, outline, source result, bounds,
  coverage, limitations and the Core-issued receipt. UI stores only receipt/focus/scroll for Back.

Closed states: Complete/Partial/Canceled/Refused/BudgetExceeded completion;
CSharp/Text/Unknown classification; File/Directory/RejectedLink/Unavailable entries;
Type/Method/Constructor/Property/Accessor declarations and Ordinary/PartialDefinition/
PartialImplementation roles. Source statuses retain the already reviewed closed vocabulary.

### Bounds, oracles and assigned subset

Initial ceilings: 25,000 entries / depth 64 / 128 descriptors / 30 seconds enumeration;
8 MiB full-source verification and 128 KiB UTF-8 display pages; two source reads;
128 rows/page / 16 pending / four active queries; two manifests / 64 MiB; 100 receipts / 2 MiB.
These are named candidate bounds, not measured guarantees. Do not implement queues in foundation.

The first writer owns exactly eight files in section 2: the codec refactor, common manifest/query
records and their tests. Budget is **18 calls of the new aggregate 60**, not a reset of prior
candidate work. No producer implementation, filesystem call, authority-issuing operation,
renderer, store or IPC is in this assignment.

Required semantic oracles: baseline symbol Values unchanged, golden file/context tuples,
hostile Unicode/delimiters, closed/validated immutable records, expected-root binding required,
unknown totals not zero, page requests bounded, FileLimited not project-accurate, no manifest
body retention, and selection requests cannot mint grant/binding authority. Pure port/type
absence is a scope fact, not proof of implemented security or native behavior.

## Foundation return and explicit replan

Foundation `34a9e652` returned eight files and 53 passing tests after 32 calls, exceeding its
18-call allocation. The Conductor independently replayed 53/53 and read the retained one-test
codec mutation red. That evidence does not establish complete common-contract behavior.

Data and Test reviews blocked the proposal: default native identities/limits/pages bypass
validation; row/limit/total consistency is incomplete; the selection uses an outline string,
five-state source enum and mandatory percentage instead of the required structured projection;
failure observations require invented hash/decoder fields; an explicit unknown profile can gain
logical identity; manifest references are not cross-validated. The codec work is retained.

Owner turn 13 authorizes one **16-call correction on the same eight files**, after Conductor
readback of a response-only mini-contract. Semantic falsifiers must fail on the old code before
repair; the parent independently replays the corrected suite. No producer or I/O work is added.

The previous 60-call implementation estimate is explicitly replaced for the next producer
checkpoint: **32 spent + 16 F correction + 24 E inventory/enumeration + 24 D declarations = 96**.
This is not retrospective approval of the overrun. Source/query/native assembly is the next
planned checkpoint, not falsely claimed to fit the remainder. E and D get separate trees and
disjoint files only after corrected common types are pinned; total width remains at most four.

One review evidence correction is also retained: a glob miss was reported as absent TRX files.
Direct absolute-path XML reads confirmed the codec mutant red and 53-test green; the reviewer
withdrew only the missing-artifact finding. The real contract blockers remain.
