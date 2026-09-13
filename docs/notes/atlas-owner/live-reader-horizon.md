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

## F clearance and E/D dispatch

Corrections `bac95e92` and `07d877ce` closed the contract blockers. The last fix constrained the
public all-fields source-observation constructor to verified state; its named semantic red was
retained. Data cleared the final blocker, Test cleared the corrected model oracles, and the
Conductor independently executed 63 passing tests in both the candidate and the joined tree.
F is pinned at `02695471` after joins `8808a1b2`, `b12c05c0` and `02695471`.

The Conductor's first join attempt was refused by F's still-held lease. The actual writer released
all eight leases and ended its coordination session; the Conductor then joined. A completed
report did not silently override a live lease.

E and D now have exact disjoint file grants in section 2, separate worktrees at that pin and
24-call allowances each. They consume frozen common records. E owns actual directory/inventory
production; D owns actual compiler declarations/ranges from supplied full source buffers.
They cannot install private contract variants or invent missing root/project authority. Source,
query and detached native assembly follow their joined receipts.

## Producer return and one consolidated repair allocation

E returned `e54f21e1` with 73 passing Understanding tests; D returned `00a85823` with 70.
The Conductor independently executed both sets and read their retained red cases. Those runs
did not clear the producers: Security/Test blocked E and Data blocked D on actual code paths.

E's repair contract preserves the narrow ordinary-local domain and requires grant expiry before
I/O, metadata from the same inspected handle, final-path/confinement checks, invocation-local
state, counting every live handle, and bounded enumeration before sorting/materialization.
Parent keys must be actual parent identities, not policy text. Unknown membership cannot become
authorized availability or complete Git coverage; explicit membership input must remain distinct
from non-Git and unavailable states. Returned-byte fields must not contain elapsed milliseconds.
Junction setup failure cannot silently pass; Core tests must exercise the relevant resource,
unknown/unsupported/generated-file and partial-result paths.

D's repair contract requires explicit syntax-tree/file/source binding rather than text-only
matching, stable occurrence keys without global traversal order, admission of the 8 MiB bound
before cloning, only the frozen decoders, request-scoped internal buffers, source-only traversal,
one nested-type traversal path, deduplication before declaration caps, and semantic regression
cases. No filesystem or project execution is introduced.

Owner turn 14 reassigns E's single repair to an execution-capable **GPT-6 Astra** writer,
**30 additional leaf calls on the same four files**, after the previous writer explicitly
hands off. D retains its writer for **20 additional leaf calls on the same two files** after
Conductor contract readback. Both writers retain separate worktrees and frozen F remains read-only.
Any necessary F amendment is an explicit seam request, never a private replacement contract.

The prior producer allowance was exhausted: the reported subtotal was at least
**F 46 + E 13 + D 37 = 96 leaf calls**, before unresolved handoff/other overhead. D's 19
top-level calls do not replace its 37 leaf calls. The prospective writer ceiling is **B + 50**,
where B is the reconciled charged baseline; currently at least 146 plus unrecorded prior overhead.
Eight targeted validation leaf calls are separately reserved. No unrecorded count becomes zero.
Source/query/native assembly remains unbuilt and receives its own next allocation.

This is an explicit repair replan, not retrospective approval of overruns or scope expansion.
Maximum two active writers, fleet width at most four, one writer per file, and Conductor-controlled
gates/joins remain unchanged. A cap does not waive an unresolved falsifier.

### Conductor minimum E repair contract

The Astra replacement receives only the four E files in a new tree at `e54f21e1`. Preserve
the probe's narrow ordinary-local/no-detected-reparse boundary; do not claim general race-free
opens. Enforce grant expiry before I/O and on long work, use one captured identity/attributes/
size source per inspected handle, restore final-path checks, count inspection and held handles,
and isolate all operation state so overlapping calls cannot reset one another's budgets.
Bound collection before sorting; retain cancellation and release-on-all-exits controls.

Correct tree parents using canonical parent keys. Treat classification, physical availability
and membership authorization separately. Unknown membership is not non-Git, authorization or
complete coverage. Explicit known membership may be supplied as a policy-bound immutable set;
no arbitrary repository code/Git hook execution is admitted. Policy-hidden names/counts must
not leak. Unsupported/generated/migration files remain visible when authorized.

Do not put time in `ReturnedBytes`, sum file lengths as transferred bytes, or invent a wire
size. The exact in-process byte-measure meaning is a named Data seam; other repair work proceeds
while it is resolved. Keep raw root/session authority Core-side and out of future UI/IPC
projections; the frozen-F grant property is not a permission to expose it.

**Byte seam resolved:** the Data owner defined `AtlasBounds.ReturnedBytes` as bytes of returned
source/content payload governed by source-page limits. Directory/inventory observations return
metadata rows, not source content, so their value is exactly zero. Duration remains separate
telemetry. No file-size sum, serialization-size estimate or elapsed milliseconds substitutes for
this measure. Later source pages report their actual returned payload bytes.

Junction setup failure must fail or explicitly report unsupported execution, not return green.
Add exact Core adapter regressions for expiry, same-handle metadata, concurrent calls, entry/depth/
descriptor budgets, cancellation, hierarchy, authorized unsupported files and partial/unknown
membership outcomes. Fix only the named defects; no new files or private common contracts.

## S source node is independently admitted

Owner turn 15 admits an execution-capable Astra writer in a separate tree at `7584c0ae`,
with **30 new writer leaf calls and eight targeted validation leaf calls**. These are new
source-node funds, not a transfer or refund of E/D expenditure. Exact files are in section 2.
The first three writer calls return the minimum contract for Conductor readback before code.

S consumes RootGrant authority and validates expiry/context, manifest/policy/file identity,
expected opened root/file identities and hash. It does not issue grants or provide a hash-only
old-anchor route. Preserve conservative ordinary-local/reparse exclusion; no general race-free
or symlink-support claim.

Enforce 8 MiB before allocation/copy/decode; reuse D's internal verified-buffer decoder and keep
full compiler input request-scoped. Display pages are limited to 128 KiB UTF-8 text bytes, with
global UTF-16 page/highlight offsets and no split surrogate pair. There are **nine source states
total: IndexedMatch plus eight non-match states**. Only IndexedMatch may expose text/highlights.
All eight non-match cases, binding/range/cancel/lifetime boundaries require executable proof.

No source/query/native-journey completion follows from source-node tests. No real user workspace,
issuer, query implementation, UI, store or IPC is included; frozen F/D cannot be silently changed.

**Explicit D-to-S bootstrap seam:** S's first contract read established that the internal
`VerifiedSourceBuffer.FromBytes` factory needs decoded length in its observation while D's strict
decoder is private. The Conductor assigned at most four leaf calls from D's remaining correction
allowance, on the same D source/test files, to expose bounded Core-only decoded-length metadata
through that existing decoder. This admits no second decoder, new I/O or source authority.
S stays at contract readback until the actual helper API/commit is joined. Its initial six calls
(including a failed shell search and bounded file-read fallback) remain charged to the 30-call
source allowance.

## Query, native view and proof composition allocation

Owner turn 17 admits **20 new native-view leaf calls**, **36 new Q leaf calls**, and
**12 new runner/composition leaf calls**, with the existing **15-call independent real-workspace
proof** allowance retained. S + native run now; after S handoff, Q + native/runner may run,
never more than two writers or total width four. These funds do not rewrite prior expenditure.

Native owns only `AtlasReaderView.cs` and its dedicated App test. Q owns only
`AtlasQueryService.cs` and its dedicated Core test. The Astra runner integrator owns only the
already named candidate runner project/Program. No existing host, IPC, store or shared-file edit.
Native must consume `IAtlasQueries`; fixture-state proof is not the actual reading-loop proof.

Core composition may issue grants only from an explicit recorded proof-root approval, observed
native identity, current session/policy and expiry. Q revalidates on every operation, bounds
in-memory manifests/receipts, rejects stale responses and restores the original binding. It may
not refresh old history silently or expose raw grant/session/root data in outward projections.

The detached proof uses a new clean registered AI-DE proof worktree and an explicit selected
root such as `src/AiDe.Core`, disclosed rather than called whole-repository completeness.
Read-only Git membership acquisition must use argument arrays, bounded output, explicit executable/
version/arguments/results, no shell, hooks/fsmonitor/external helpers/network/submodule recursion.
Failure is UnavailableMembership, never NonGit fallback. No inspected-project MSBuild evaluation.

Initial source binding comes through S's checked opened-object/buffer path and creates a new
observation; no ad-hoc file reader or old-selection recapture. If S lacks that entry point, one
exact amendment by its owner is allowed up to four calls charged to Q after Conductor seam
readback. The actual detached file -> member -> source -> Back journey remains the horizon exit,
with source/span/decoder/focus/cancel/changed-history evidence and targeted Security/Test clearance.

## Root-observation bootstrap allowance

Q's actual source-contract readback identified one metadata-only root bootstrap seam.
`ObserveApprovedRootIdentity` reuses S's ordinary-local/ancestor/held-object checks and trusted
current-approval callback. It observes native identity; it neither issues a grant nor reads source
content. Only Complete carries identity; Partial, Refused and Canceled carry no usable identity.

Owner turn 19 corrected the pre-execution allowance from four to five leaf calls, charged inside
Q's unchanged 36-call allowance: eight Q contract reads plus five bootstrap calls leave 23 for
query implementation. The extra call preserves claim, test/stub, semantic-red, implementation
and verify/commit/release steps. No F/D edit or retrospective overrun approval is implied.

## Native repair and source accounting correction

Owner turn 18 reassigns N to an execution-capable Astra writer for **24 new leaf calls on the
same two files**, with **eight independent validation calls** separately. The initial 29/20
expenditure and absent semantic red remain recorded. The old writer handed off before the
replacement tree was created.

The Conductor's consolidated N contract requires a rendered hierarchical child template,
correct directory/file identity and deduplication, pagination based on returned offset/rows and
honest continuation, distinct TooLarge versus FileLimited copy, observed async failures and inert
stale responses, cancellation on unload, and accepted-only Back selection/focus/scroll restoration.
History is bounded to 50 receipt/view-state frames with no stored source bodies. Tests must host
the actual view in a shown STA Window and prove realized hierarchy/keyboard/UIA behavior.
No new theme, HTML mockup, host or shared-contract edits are allowed.

S's author reported 29 writer plus eight author-run validation calls: this is **37 author leaf
calls against 30**, not independent validation. Independent Security/Test and Conductor execution
still ran; the corrected exact TRX paths established 165/165 and two semantic mutant failures.
S was joined at `720c847f`; combined F/E/D/S execution reported **197/197**. That clears the
limited Core source node, not live-workspace/native delivery.

Q may now consume the actual S API independently of N's repair. Its first readback must settle
trusted root approval/observed identity, policy-bound membership input, new observation versus
old binding validation, bounded manifests/receipts, and the outward redacted projections before
implementation. Source, producer and raw grant objects do not become UI payloads.

## Exact F -> Q/N composition seam

Owner turn 20 authorizes one sequential-then-parallel batch: **F eight leaf calls**, then
**Q twelve and N ten**, followed by six targeted independent validation calls. Each writer has
its own tree and exact section-2 file grant.

`InventoryPage` gains optional nullable `NextOffset`, preserving existing callers. Non-null
requires a nonempty page and checked advancement equal to `request.Offset + returned rows`;
non-progress, overflow and contradiction with known total are rejected. Q supplies it only when
more retained, ordered visible rows actually remain. Null means no further retained page, not
complete global coverage. A changed row set or policy must refuse/restart, not apply an old
offset silently. Unknown/Withheld global denominator state remains unchanged.

N consumes only this continuation signal. It adopts a returned manifest token only after a
current, successfully accepted IndexedMatch projection, including accepted Back restoration.
Stale/canceled/refused/failed results cannot advance manifest or history authority.

Q inventory `ReturnedBytes` is zero for source/content payload. Its conservative retention
charges remain retention accounting, never a measurement of returned inventory content.

The required proof crosses the seam: file -> member -> different file after new manifest tokens,
stale/failure token rejection, unknown-global-total pagination through the retained end, invalid
continuations and byte-unit semantics. Then proceed to the already funded runner; no unit-only
handoff completes the live-reader horizon.

### Turn-20 returns and independent observations

F continuation `c104695a` was joined at `f8a6df06`. The F receipt's 17 top-level calls and
"wrapper leaves 7" are not a coherent eight-leaf accounting result; the reported overrun remains
a finding. Q returned `e98b196e` after ten of twelve newly allocated leaves: eleven semantic
failures preceded 43/43 Q and 272/272 Understanding results. The Conductor independently ran
272/272 with zero skips and read the implementation delta: stable retained order, explicit
continuation, preserved denominator, and zero content bytes.

N returned `7b5b6ff1` after ten of ten newly allocated leaves. The Conductor independently
observed 38/38 shown-window fixture cases and read the accepted-only manifest-token and
authoritative-continuation delta. Twenty semantic failures against the preceding behavior are
retained. These are component/seam observations; neither substitutes for actual Q/native
composition or the authorized live-root journey.

The runner tree is `C:\Projects\ai-de-atlas-live-reader-runner`; its two-file writer grant is
recorded in section 2. It must receive the reviewed Q/N joined pin before implementation.
Its twelve-leaf allowance and the independent fifteen-call proof allowance are unchanged.

### Runner proof mode, Owner turn 21

The Owner permits a bounded `--prove` mode in the existing runner `Program.cs`. It drives the
actual rendered tree, member, different-file and Back actions, records observed identities,
manifest transitions, source correspondence and focus, and captures only its proof-owned window.
Rendered-content captures must be labelled as such, not called desktop screenshots. Missing
UIA, capture or required journey evidence is NOT_PROVEN, never a successful fallback.

Proof-only startup must be established and observed: loading resources alone is not evidence
that inherited App startup, MainWindow, terminals or unrelated workspace attachment did not run.
The exact approved scope/session/policy/expiry and revocation must be checked before real-root
execution. Git remains a bounded membership-only process. The approved AI-DE scope is read-only;
mutation checks use separately owned synthetic fixtures.

Author proof and the subsequent independent run remain distinct. Twelve runner leaves and
fifteen independent-proof calls remain the limits; no host/IPC/main grant follows. The
Conductor's joined `bcbe8a47` contains reviewed Q/N seams: 272/272 Core Understanding and
38/38 native fixture cases independently executed, zero skips. The Test review's truncated
Q test-diff read was completed directly by the Conductor; all eleven new cases were inspected.
Security, Test and UX cleared only the targeted component/seam claims. Real composition is next.

### Runner recovery and prospective repair, Owner turns 22-23

The first runner checkpoint spent three leaves without establishing readable API contracts:
two skill loads and one oversized 202.7 KB output. Two bounded recovery reads established Q
and native signatures. One support read established outline Enter activation and the inline
semantic brushes in App.xaml. Owner turn 22 retained the original scope/budget.

The two-file proposal then reached exactly twelve leaves (two skill loads, eight shells, two
patches) with a failed build: CS7036 at Program.cs lines 185 and 240. The author used a
single-argument `ListBoxItemAutomationPeer` constructor instead of the required item/selector
relationship. The Conductor read `artifacts/atlas-reader-author/final-build.log`; no synthetic
execution, capture, negative proof or real-root access had occurred.

Owner turn 23 prospectively adds **eight leaves**, for twenty authorized runner leaves total.
The exit is actual shown synthetic file/member/different-file/Back proof, capture and intended
semantic negative failures. Correct peer acquisition must use the realized item and owning
selector, or the framework-created peer; compiling a substitute is insufficient. Only concrete
runner defects may be repaired in the same two files. Product Q/N/S defects return to their
owners, never disappear through changed expectations.

The plain proof Application derives named brush values from embedded existing App.xaml using
XML extraction, never Application XAML instantiation, general XamlReader loading or new theme
values. The preimplementation Security/Test gates clear only this bounded design; actual
approval/Git/UI-proof source inspection and execution still precede real-root admission.

The independent scope is explicitly `src\AiDe.Core` in the clean, registered
`C:\Projects\ai-de-atlas-live-reader-proof` at `bcbe8a47859984c1e93074efc65116908ebb7936`.
The Conductor issued a separate schema-version-1 approval record for `atlas-live-proof-gpt55`,
policy `atlas-proof-readonly/v1`, expiring `2026-09-13T04:00:00Z`. Its SHA-256 is
`2bcd31c78396b7bec129f2b64e1e3e12ad63de67e3c61f7c0447788f2f570880`.
The independently supplied expected values and digest, not a caller-provided root alone,
constrain the proof callback. Real-root execution is not yet released by this record: the
remaining code/proof gates must clear first. The separate fifteen-call proof allowance stands.

### Actual composition exposes lost member selection, Owner turn 24

The runner builds, and its actual synthetic run records 37 PASS, one FAIL and seven NOT_PROVEN.
The failing Back row was already unselected before leaving the accepted member. Source,
manifest, receipt and focus/cursor/scroll checks pass. The source fault-injection run fails
at its intended source-correspondence assertion. These are author runtime observations, not
independent real-root proof.

The Conductor's narrow investigation is `investigation-code-atlas-outline-selection`; Test
blocks lowering the selected-member expectation. Owner grants eight new leaves to the existing
N owner/two files for shown semantic red, accepted-current-only key rebind, green and review.
After that join, six additional runner leaves complete remaining proof and freeze the proposal.
The runner ceiling is now 26, with 19 reported spent and seven remaining. No source/binding/
grant change or real-root access is admitted. Causality stays provisional until controlled
red/green; the DC-029-related instance is not called confirmed before then.

## Detached horizon acceptance and next checkpoint, Owner turn 32

The native correction and actual composed runner are joined. Independent proof read the
approved AiDe.Core bytes, checked UTF-16 ranges/hashes and different-file identity, inspected
the owned-window PNG, obtained the intended isolated semantic red, and confirmed clean input.
The detailed evidence is `proof-code-atlas-live-reader-candidate`.

Owner opened both independent receipts, the final proof section and the image. It accepts
**only** the detached reader at `a0ffcee3` / Conductor `cc67f7c6` against input `bcbe8a47`.
That is inspected-evidence acceptance, not an Owner claim to have rerun the tests. Six real-mode
checks remain N/A. FileLimited semantics, ordinary-local/reparse exclusions, visible-viewport
scope, broader accessibility/DPI/performance and full-solution obligations remain explicit.
Back used the original issued receipt/binding and returned a successor receipt.

The programme stays open. **Next: current-main compatibility and concrete shared-host admission**,
not later diagrams or AI. One separate Astra worker receives **12 new leaves** to reconcile
executor-observed main in its own branch, build the reviewed Atlas code, name exact adapter
files/owners/minimal changes/tests, specify production workspace/grant and Git-membership
composition without proof-only approvals, and make one supported counterpart handoff attempt.

No existing adapter authoring or primary changes are permitted in that checkpoint. The packet
must cover Architecture admission, factory/menu/host wiring and necessary query/IPC seams.
If acknowledgment remains absent, the worker returns the concrete packet to Owner for a bounded
branch-local adapter-authoring decision. No human relay requirement or repeated pull-log polling.
Sidebar, graph/tree pivot, class/sequence/domain, layered/Azure, comparison/decision lineage and
governed AI remain unfinished. Addendum E remains candidate and its architecture proposed.

## Compatibility packet completion, Owner turn 33

The first twelve calls produced the packet pair and one supported request,
`req-01M2CAXKH01J8SMQV1HBCCAN08`, still OPEN. The merged working state built successfully and
executed 272 Core / 45 native tests. Production authority/membership signatures and the complete
host method trace were still unestablished, and no compatibility commit had been made.

The Conductor supplied `--base HEAD` to a tool that resolves Git references in the primary
checkout. The tree therefore started at main `6d3e281a`, not the intended caller `1e688ace`.
The worker measured that state and merged the accepted Atlas commit into it; no work was lost.
The correction is explicit immutable base SHAs and immediate target HEAD comparison, never
assuming the caller's symbolic HEAD survives the tool's working-directory choice.

Owner adds twelve prospective calls, total 24, for the same two packet files: first four for
targeted production/host contract grounding, then accurate packet, classified graph findings,
legitimate regeneration and compatibility commit. Missing APIs must be demonstrated and proposed
separately. No redundant test run is required if relevant inputs are proven unchanged. The
Conductor checks final staged paths, no unresolved conflicts and intended parents before commit.
No existing adapter edits, second handoff/polling, primary changes or consent inferred from silence.

### Sealed compatibility result

The worker used 24/24 leaves. The Conductor inspected the final 93-path set, empty conflict/
unstaged sets and parents, then sealed local compatibility commit
`be3ace852652557160228154716a7d499bd8bf76`, tree
`ecdedfccc456eef33494666e4cb767ce2eb80182`, parents
`6d3e281a049b8baf3696a90b341d3468635ef2f1` and
`1e688ace6a9740ece7350f148f0e918d1ad6a6e1`.
The 899-input identity manifest is explicitly post-run; no pre-run measurement was invented.
The 272 Core / 45 native results apply to the unchanged merged code/build inputs.

An EOF blank-line warning in `CSharpDeclarationObservation.cs` was observed by `diff --check`.
The Conductor compared its staged blob with the accepted Atlas parent: both were
`370a8be050370094a8103c05029d76107119903c`. It retained that inherited source rather than
authoring an out-of-scope formatting change to silence the warning. The merge commit records it.

The packet's verification attributions were corrected before sealing: the Conductor read the
build/TRXs and request record; the Owner ruled on supplied results. The missing packet summary
was fixed. The remaining graph finding is another owner's ruling-49 link to
`proof-conductor-front-door`; it was not suppressed or repaired here.

Production decisions remain open. The packet identifies source-scope lease/membership and
awaitable IPC gaps. A subsequent Conductor reader trace also opened the existing production
`NodeContent` route: IPC capability-gated query -> stored declaration/scope -> workspace-confined
bounded file read -> `CoreNodeContentSource`. Absence of Atlas-specific names therefore does not
mean absence of an existing workspace source-read basis. Security/Data are reviewing the least
additional lease and the independent Code/Architecture context; Atlas `SessionToken` must not be
silently reinterpreted as Conversation/DocumentSession.
