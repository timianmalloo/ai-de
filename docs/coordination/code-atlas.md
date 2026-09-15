---
id: coordination-code-atlas
title: "Coordination plan - Code Atlas first delivery horizon"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [code-atlas, coordination, worktrees, active]
links:
  - { to: spec-addendum-e-code-atlas, rel: implements }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
  - { to: coordination-code-atlas-resume, rel: depends-on }
  - { to: note-atlas-lane-admission, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Recovery separates frozen accepted Atlas integration with current main from retained
  native E1 qualification. Publication remains the parent coordinator's operation after
  integrated proof and independent review; earlier checkpoints remain historical evidence.
---

# Code Atlas coordination - current-main recovery

## Recovery checkpoint, 2026-09-15

This checkpoint supersedes the earlier no-main-integration allocations below under the user's
explicit merge-and-resume instruction. It does not erase their historical constraints or release
later E1/E2/E3/E4 work. Sole ownership authority remains `docs/collaboration/session-contracts.md` §2.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| Integration | Conflict resolutions and this existing plan in `C:\Projects\ai-de-atlas-main-integration`, branch `atlas/main-integration` | Frozen accepted source `92e025ae8cacd0a2fc8580f865a2172c4a9bdeb4`; current main `bab5035e75a10e97e57934891650cd4ddefecd76`; fresh coordination readback | T2 | 0 | 50 tool calls, 400k context ceiling; at most two decreasing-finding repair passes | Preserve both histories and newer session/engine/account/Composer behavior; full suites plus portable/nonportable recount and `--no-run`; regeneration, every gate, Release build; committed candidate and exact residuals, no push | GHCP, explicit identity and exact short leases; current boundaries observed-only |
| Native qualification | Only the five native source/test files listed in §2, in retained `atlas/e1-native-class-view` tree | Existing 26-test checkpoint; separate author, no rebase during integration | T2 | 0 | 40 tool calls per parent plan | Native regression, visual and mutation receipts; exact candidate pin; independent review, no self-acceptance | GHCP native author; shared pull-log coordination with Claude/Grok/Codex, not injected delivery |
| Publication | Parent `copilot-atlas-recovery-b0d0` alone | Both track receipts and required independent reviews | T2 | 0 | Parent plan | Accepted join, current-main boundary check, local/remote SHA readback; preserve primary dirty logs | Serialized by parent; no worker pushes or mutates main |

**Merge boundary observed:** 232 main-only / 156 source-only commits. The first scripted join
reported six conflicts: `docs/lessons/defect-classes.md`, `site/collaboration.html`,
`site/index.html`, `site/model.html`, `src/AiDe.App/Workbench/SurfaceContentFactory.cs`,
and `src/AiDe.App/Workbench/WorkbenchShell.cs`. Site pages are authored, figure-patched HTML:
retain current-main markup and regenerate figures; fully derived views are regenerated, never merged.
Defect-register additions are retained. JSONL registers require full-content union, never ID-only loss.

### Current execution authority: independent accepted integration, then native

The user's 2026-09-15 clarification supersedes the earlier publication dependency in the table
above: **I → IR → MI** qualifies and publishes accepted Atlas plus latest main independently of
unaccepted native work. **Qualified ND → A → R → J** is the later native assembly, combined-tree
review and publication path. Two separately green trees never qualify that later combined tree.
Only closing coordinator `atlas-recovery-closer`
(`ed7d1cd1-d8e4-437a-83bc-f39eb926c352`, retaining `copilot-atlas-recovery-b0d0`)
publishes. Foreground and worker sessions do not mutate primary/main or push.

Original integration used **50/50 calls, BLOCKED**, followed by one separately requested
receipt-routing call. Original native qualification used **40/40, BLOCKED**; the retained pin is
reported as `4a581204`, with restored outcomes **108/111** and **70/71**, not qualification.
The user prospectively admitted **ND24** diagnosis/repair, not a sixth native-file grant.
The integration worker does not inspect, edit or apply that native candidate.

**IRP is a new 24-call allocation**, not retroactive extension of the exhausted 50-call run:
same writer/tree, T2, fan-out 0, 400k context ceiling, at most two decreasing-finding repair passes.
The Stage-1 frame was delivered at **6/24** before ungranted product/test repairs. Latest main
readback is `663c3a8047f41fcb838bb23d793a5fcd7f67473c`; the open merge still has parents
`bab5035e75a10e97e57934891650cd4ddefecd76` and
`92e025ae8cacd0a2fc8580f865a2172c4a9bdeb4`. Finish that merge safely before the official
no-push join of newer main; every join uses the explicit Copilot trailer file.

**Authoritative placement correction applied in IRP:** main's `ZoneId? Zone` is the only
descriptor placement field; Atlas uses `Zone: ZoneId.Center`. `PreferredStackId`, retired
Evidence selection and `inspector` are not resurrected. Both initial and reattach factories retain
`consoleFor` plus `AtlasOwner`. Explicit `intoStackId`, prompt-beside-terminal routing,
repeated-Show singleton focus, and moved/restored placement remain unchanged. This supersedes
the dual-field resolution recorded in the historical blocked checkpoint below.

**Separate remaining failure classes:** accepted Atlas's one-row menu admission versus the
pre-Atlas literal menu oracle; missing owned proof-run environment; compilation-Lease lexical
census versus two unrelated private Atlas lease types; one-time-initialization census versus
contractually admitted tracked attach/detach activation; and three ActivitySource labels outside
the `aide.` privacy boundary. None permits broader allowlists, removed assertions, new Loaded
hooks, or a waiver for known main failures. Additional source/test paths require explicit
authority before edits. No fresh full cohort runs until the known targeted reds are addressed.

`ATLAS_PROOF_RUN` is a fresh alphanumeric/hyphen **label**, not an absolute directory. The
existing test owns `artifacts/atlas-real-daemon-window-proof/<label>`, refuses overwrite and
requires this tree's daemon to be built explicitly. Environment correction does not disable it.
The helper is taken verbatim from main's Claude repair, including its collision-proof and
legacy self-tests; the worker's earlier divergent repair and raw union receipts remain historical.
Site views regenerate only during this phase. Shared owed markers are serviced through official
`coord regen`, never manually cleared. Preserve both §4ac/§9 and newer main's §9/§10 authority
when reconciling; Claude Ruling106 and pre-push announcement remain publication gates.

**IRP authorized-boundary evidence:** the exact main helper blob
`27f7ee8acf7e10404b97a746068b69666acad5cd` was taken from `663c3a8047f41fcb838bb23d793a5fcd7f67473c`;
its real-collision self-test passed under collision-proof and legacy allocation. No competing
helper repair remains. Official `coord regen` reported nothing owed at the execution boundary.
A nonincremental Debug daemon build passed with zero warnings/errors. With fresh owned run label
`atlas-irp-1789483669095237100`, the unchanged `AtlasSharedHostAdmissionTests` and
`AtlasDaemonMainWindowProofTests` passed **40/40, zero skipped**. This includes new Atlas Center,
explicitly moved Atlas preservation, tracked reparent/late-admission cancellation, awaited
replacement/close, and real daemon borrowed-client preservation. Full output and TRX are in
`.artifacts/atlas-main-integration/IRP/atlas-irp-1789483669095237100/`; the test's owned real-window
receipts remain in `artifacts/atlas-real-daemon-window-proof/atlas-irp-1789483669095237100/`.

This clears the missing-environment setup failure for that targeted run, not the remaining five
historical failed tests. No literal menu, mint guard, Loaded guard, private lease type, telemetry
label, or native candidate was edited during this admitted subset. Further repair authority and
latest-main reconciliation remain pending. No new whole cohort, full gate runner or Release build
has run. An isolated merge checkpoint is evidence for review, not accepted publication or a final
qualification freeze; all remaining gates still apply.

### Owner technical scope pending counterpart acknowledgement

The decision-only Owner reviewed the IRP six-call frame. **B-E below are technically approved
but NONEXECUTABLE** until the responsible current §2 owner acknowledges the exact writer and
paths. This section records review conditions, not a second ownership map or an execution grant.
The closer requested Claude's counterpart acknowledgement. Silence, elapsed leases, queued
messages, and the technical ruling itself do not supply it.

- **B, menu:** the exact Architecture-only canonical Atlas row is admitted technically.
  Candidate test path: `tests/AiDe.App.Tests/Workbench/PerspectiveMenuTests.cs`. Retain the
  literal-table assertions and all existing main rows; no broad expected-set relaxation.
- **C, private lease names:** prefer only two meaningful private Atlas lease-type renames and
  references in those same files: `src/AiDe.Core/Ipc/AtlasRemoteReader.cs` and
  `src/AiDe.Core/Understanding/AtlasGitMembership.cs`. Leave the Composer mint guard unchanged.
- **D, privacy coverage:** replace only leading `AiDe.` with `aide.` in the three existing
  ActivitySource literals in `AtlasRemoteReader.cs`,
  `src/AiDe.Core/Understanding/AtlasDirectoryEnumerator.cs`, and
  `src/AiDe.Core/Understanding/AtlasQueryService.cs`. Preserve each remaining semantic suffix,
  all Meter names, span names, tags, runtime behavior and assertions.
- **E, lifecycle guard:** the named test
  `TheWebSurfacesInitialiseOnceAcrossReparentsTests.NoElementOutsideTheHostHooksLoadedForItsOwnInitialisation`
  in `tests/AiDe.App.Tests/TheWebSurfacesInitialiseOnceAcrossReparentsTests.cs` needs an exact,
  acknowledged reconciliation with companion lifecycle negative controls. Distinguish tracked
  per-attach activation from one-time initialization. The controls must reject missing
  cancellation, missing task tracking, disposal of a healthy scope during reparent, and a native
  paint-Loaded hack. Exact companion writer/path acknowledgement remains required; no broad
  allowlist, fresh Loaded hook, renamed assertion or removed assertion is admitted here.

The native paint-Loaded workaround is rejected. NP6 repair belongs to the separately authorized
native writer's own files; integration must not edit the native reader. The released integration
environment repair remains a fresh proof label plus explicit daemon build, never a legacy-file
edit. Already authorized Zone/helper/environment/plan work is unchanged. IRP retains its original
**24-call** allocation; this technical ruling adds neither calls nor implementation authority.
The worker ends the turn when requesting a decision and does not continue past an unresolved
gate in anticipation of a queued response.

### ICD10 - acknowledged C/D, B/E still held

The new ten-call ICD allocation admits only C and D under Ruling115, read from main
`33e9ae7e00e3edb2d44ecec006e5a3c354e69efd`. B/E remain held: their apparent scope admission
contradicts clauses requiring unchanged Claude control source, and the closer is seeking an
explicit clarification. No B/E edit or newer-main join is permitted by the C/D run.

The two private type renames and three ActivitySource prefix changes were applied in the four
admitted Core files, with exact 300-second leases/checks and release before tests. Original raw
guard failures were preserved and parsed. Fresh unchanged Composer guard: **1/1 passed**.
Fresh Core guard/directly coupled selection: **179/182 passed, three failed, zero skipped**;
the privacy-prefix guard passed, but the directory telemetry test's exact-name listener still
subscribes to the old prefix. All three failing parameter cases passed in the original receipt.
No test was altered, and no corrective commit is permitted while these conditions remain red.

The exact additional listener-only test-path request and Ruling113(ii)'s one discovered Atlas
View path are recorded in existing `docs/proof/code-atlas-production-adapters.md`. That is a request
for authority, not an assignment or an assertion relaxation. No GUI, native application, whole
cohort, gate weakening, main mutation or push occurred. Raw ICD receipts remain ignored; the
worker stops at the ten-call handoff with its explicit uncommitted diff and no final qualification.

### IBE12 - execute the acknowledged B/E amendments, preserve proof order

The user's explicit clarification releases B/E amendments: the unchanged-control-source
condition means no weakened guard, not a ban on the exact acknowledged test edits. This is not
another ownership research step. IBE receives twelve prospective calls; ICD remains separately
blocked on its additional directory-listener selector path.

B's two literal amendments are prepared in `PerspectiveMenuTests.cs`: exactly one
Architecture-only/single-instance Atlas row (19 total expected rows) and its exact Architecture
opener. All existing assertions, other rows and other perspectives are unchanged; old raw reds
were read before the edit. Fresh App verification has not yet run under the native desktop hold.

E must replace the path/name-only Loaded rule with production-backed lifecycle observations,
plus new retained negative controls that prove the five requested failures. The old guard is
not removed before that red-first evidence exists. No synthetic-only success, path/type
exception list, native reader edit or Loaded-paint workaround is admitted. Shown lifecycle/App
verification waits for native release and the closer's explicit integration handoff.

**Safety and handoff:** no private proposal/TheTerrace import, dependency or shared-gate weakening,
native dirty-file import, global stash, worktree installation/deletion, resource kill, or worker
publication. MainWindow, IPC admission/security, source grants and borrowed/owned lifetime remain
independent review boundaries. Incompatible semantic choices stop for the parent. New regressions
require red-first proof; unrelated defects and original target proof gaps remain explicit blockers.

**Coordination evidence:** own-tree doctor found effective inherited drivers. Request/liveness
readback at the merge boundary found six active sessions and open Claude/Grok recovery requests;
shared logs are pull-based, not evidence of delivery. Conflict decision request:
`req-01M2JPF6S81A5EKREZPRTATPW4`. The parent alone acknowledges cross-harness agreement and publishes.

**Planned versus actual, blocked checkpoint:** all six conflict paths are resolved. Both optional
`SurfaceKind.Zone` and `PreferredStackId` remain: main's Zone placement is still read by
`OpenReferenceDocument`, while Atlas supplies its existing Center preference. Main's `consoleFor`
and Atlas's `AtlasOwner` coexist in both factory constructions. The stale unused Evidence selection
property is not resurrected. No native candidate file was inspected or changed.

The official register helper initially failed on a real ID collision because the installed pack
removed `_reserve`. The captured red receipt preceded adapting only that helper to the current
`next_id` API. Green full-content verification retained 742 audit entries and 165 change entries
(164 distinct non-ID payloads), reissuing one colliding ID. No shared gate or dependency was weakened.

The full configured join ran once with `--no-push` and the explicit Copilot trailer. The
conflict-marker and defect-register checks passed. Recounts observed:

| suite | total | executed | passed | failed | not executed |
|---|---:|---:|---:|---:|---:|
| App | 1109 | 1109 | 1104 | 5 | 0 |
| Core, whole | 3162 | 3161 | 3160 | 1 | 1 |
| Core, portable | 2818 | 2817 | 2817 | 0 | 1 |
| Core, nonportable | 344 | 344 | 343 | 1 | 0 |

The closing `verify-test-run.py --no-run` returned 1; conductor join returned **4 at recount**.
The split rows are subsets, not additional distinct tests. The configured recount raised floors;
it did not turn failed outcomes green. Primary failures remain:

- `PerspectiveMenuTests.TheAllowListsEqualTheSpecsTable`: 18 expected, 19 observed.
- `PerspectiveMenuTests.TheRenderedMenuEqualsTheSpecsLiteralTable_ForEveryPerspective`:
  Atlas adds `Show code Atlas` to the literal expected table.
- `AtlasDaemonMainWindowProofTests.MainWindow_RealDaemonReplacement_AcknowledgesHealthyReleaseAndPreservesBorrowedClient`:
  `ATLAS_PROOF_RUN` must name an owned receipt directory.
- `TheSendGateSendsWhatProjectionProjectsTests.ALeaseIsMintedAtExactlyTwoNamedSitesAndByNoOtherPattern`:
  AtlasRemoteReader and AtlasGitMembership are additional sites caught by the existing guard.
- `TheWebSurfacesInitialiseOnceAcrossReparentsTests.NoElementOutsideTheHostHooksLoadedForItsOwnInitialisation`:
  AtlasLoadingHost and AtlasReaderView trigger the existing lifetime guard.
- `PrivacyMarkerTests.EveryActivitySource_IsUnderTheAideNamespace`:
  AtlasRemoteReader, AtlasDirectoryEnumerator and AtlasQueryService use names outside `aide.`.
- The symlink purge test is not executed because this host cannot create a file symbolic link.

The full gate runner and Release build were **not reached**, and native qualification remains the
other author's responsibility. Gate weakening, test dropping, and self-approval are not remedies.
Only the allocator compatibility repair was applied; further semantic/control repairs require
parent scope and independent review. Complete join-level subprocess receipts and copied TRX files
are under ignored `.artifacts/atlas-main-integration/` (`test-results/` for the four TRX files).
This is a blocked integration checkpoint, never a ready-to-publish result.

The 50-call worker cap includes grounding, coordination, receipt retention and final handoff;
fan-out remained zero. Remaining findings did not reach zero within that allocation. Publication
and acceptance stay blocked; the parent resumes from exact evidence rather than another blind join.

## Historical isolated-authoring checkpoint

**Current execution checkpoint:** Conductor `acaf4dca` includes the accepted detached reader,
main compatibility, public Core render seam, reviewed Shell component/lifetime repair and
qualification-only native membership mechanism. The cause/fix distinguishes issuing-thread
exit from a namespace change. Production membership remains blocked on retained ownership
when native cleanup times out. The actual Core factory
and real-window handoff remain absent. The **Production convergence checkpoint** below is current;
earlier allocations and decisions are retained as history.

**Current delivery blocker:** `req-01M2B86TXF7SHG61B31P4H4173` is still open.
The native request is a shared pull log, not delivery into Claude's conversation. It blocks
agreement on shared integration, **not the independently admitted authoring below**. The user's
continuation instruction led to `note-atlas-isolated-authoring`; the sole section-2 register now
records that branch-local exception and exact safety writer/files. Existing ownership rows remain.
The new narrowed request is `req-01M2BGHNCM6WRD4ZZMBBFEEB4K`. Neither request is acknowledged.

**Horizon:** E-0, the Owner's first physical inventory/type/member/source native journey.
E-1 through E-4 are roadmap constraints, not work released by this plan.
Metadata uses `type: doc` because the installed graph registry rejects `plan`; the execution-plan
headings/columns below retain the skill's machine-readable schema.

## Layer state

| check | result | meaning |
|---|---|---|
| `coord doctor` in conductor tree | 11 classified patterns; merge drivers effective | Installed per clone; every linked worktree inherits. No reinstall from a worker tree. |
| Shared regeneration marker | 6 owed entries at grounding | Not cleared with `coord regen`: its primary-owned marker must not be deleted as a worker side effect. |
| `pack-doctor` | Coordination PASS; graph has known dangling F5 proof link | Installation state and content readiness are separate. The F5 branch remains frozen. |
| Main at worker creation | `4d396411`, CV-1 merged after SH-2 | Both new worker trees resolve to this revision; reconcile again at shared integration. |
| Atlas code permission | New-file branch-local authoring admitted | Exact exception in section 2; existing shared files and main integration still excluded. |
| Candidate E | Content gates clear with downstream conditions | Candidate/draft only, not normative registration or implementation proof. |
| Architecture | PROPOSED; independent content reviews, Owner choices and final scoping conditions recorded | No source, migration, native or provider admission. |

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| Agreed product source/test files | authored | One registered writer per file | Yes; exact Core/Shell carve-outs required. |
| New Atlas spec/architecture/ADRs/design/proof | authored | Isolated branch, named scope and lease | Yes for authorship; no overlap with C/D documents. |
| `docs/audit/*.jsonl`, coordination logs | register | Existing append-only writers; content union at join | No content-writer lease choreography; never hand-merge away another entry. |
| `docs/docs-index.js`, audit view, API/doc bundle | derived | Existing generators, in dependency order | No manual merge; Conductor regenerates after audit at join. |
| `site/*.html` | authored with derived figure regions | Existing figure updater only | Never classify the whole page derived or overwrite another author's prose. |
| Private proposal branch / TheTerrace | reference only | Read-only; safe summaries only | No import into delivery history or publication. |

## Tracks

The exact source-safety files and the first four-file identity/binding unit are recorded in the
section-2 exception. The safety checkpoint is joined with bounded evidence; the first candidate
writer is dispatched. Other `owns` entries below remain proposed integration responsibilities,
not a grant.
Budgets are Inferred planning circuit breakers. A firing cap reports a finding; it never drops a gate.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| E0-SAFETY | Four exact new probe project/source files and `docs/proof/code-atlas-source-safety.md`, listed in section 2 | Explicit Owner probe admission, now recorded | T2 | 0 | 30 calls including preflight/clarification, one bounded native handle/race batch | Windows opened-object/root/link/replacement/hash/decoder semantics observed; refusal/race falsifiers; no userdata or shared-store mutation | Existing GPT-5.5 writer, new `atlas/e0-source-safety` tree at `4d396411`; dispatched |
| E0-BUILD first unit | Exact `AtlasIdentity.cs`, `AtlasSourceBinding.cs` and their two Core test files recorded in section 2 | Owner turn-9 unit grant; narrow unit model/oracle gates; not the complete E0 document | T2 | 0 | 25 calls within the existing 60-call candidate budget, including at most five unit-design calls | Compiled revision-independent scoped identity, unambiguous encoding, manifest/root/file/hash binding mismatch oracles and observed red/green; no I/O or UI claim | Single GPT-5.5 writer `f4db534a-6b1c-4a34-9f1b-24cde7be2b6f` in `atlas/e0-candidate` at `4d396411`; subsequent inventory/reader/native work requires the next exact assignment |
| SH-INTEGRATION | Existing factory/menu/host/layout files retained by Claude/Shell | Stable E0-BUILD seam and accepted integration request | T2 | per current Claude plan | Set by owning conductor, not invented here | Registry/routing/layout/native-host path reaches the real new content; current SH2 behavior remains intact | Existing acknowledged Claude/Shell lane, its own worktree |
| E0-PROOF | New agreed proof/tests/probe artifacts, not product source | Joined E0-BUILD + SH-INTEGRATION revision | T2 | 0 | 30 calls, one bounded evidence pass plus named repairs | Actual Architecture entry -> file -> member -> source -> Back with UIA/focus/theme/DPI/bounds/stale/unknown states; independent source/wire/store consistency proof | GPT-5.5 proof worker, separate pinned worktree; relevant independent specialist reviewers |

One coherent implementation writer is deliberate: splitting inventory/model/source/selection across
several new writers would create serial schema and adapter seams while paying parallel context cost.
The fleet still separates Owner, Conductor, author, Shell integrator and verification authority.
It does not manufacture concurrent code lanes where dependencies fail the independence test.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| Register E and agree shared integration | Normative registration and existing-file/main authority are not supplied by the branch-local exception | Claude primary conductor + Core/Shell counterparts; Astra Owner for Atlas scope |
| Source identity/manifest/policy contract | Every downstream projection and source read depends on it | Accepted E0 design owner, Data/Security/Test gates |
| Windows source-reader safety choice | Unsafe opened-object/hash policy invalidates the source contract | Native/Security/Core design gate |
| Public wire/capability contract | UI and daemon must agree on bindings, bounds and refusal | Core/daemon owner |
| Shell integration | Existing active files have one owner | Claude/Shell |
| Main convergence | Shared index/HEAD cannot have two integrators | One explicitly agreed integrator; currently Claude |
| Horizon closure | Author reports are evidence, not acceptance | Separate Owner after required independent gates |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| Atlas -> Core | Accept exact inventory/member/source/query/wire source/test carve-out from architecture section 14 | Core authority routed by Claude conductor |
| Atlas -> Shell | Consume stable native content/selection/query seam; Shell owns registry/menu/host changes | Shell owner, not lease expiration |
| Core producer -> native consumer | Versioned manifest/scope/identity/hash/span/coverage/bounds survive every projection/wire hop | Single E0-BUILD author until contract stable; independent tests |
| Atlas -> Conversation, later | Dedicated read-only analysis contract; never compiler-envelope/direct-provider shortcut | Conversation owner only at E-4 admission |
| Workers -> Conductor | Commit/report/proof receipt with exact scope, tests, residuals and no self-acceptance | Conductor joins; Owner adjudicates |

Shared-surface guards are jointly scoped: Atlas prohibits its interpretation path from invoking
compile/run APIs, not those APIs everywhere in shared files. Shell retains its legitimate catalog/
layout operations. Any new scan guard must state root, recursion, tokens and named allowlist.
No guard may require removal of another lane's authorized behavior.

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Separate file/type/member implementers before contracts | Splits one identity and source-binding invariant; every change becomes a seam request. |
| Independent second graph service/store | Duplicates authority, history and privacy state without measured need. |
| Parallel E1/E2/E3/E4 feature lanes now | Owner has not admitted them; E0 identity/source foundation and stage-specific proofs are prerequisites. |
| Atlas edits to shared Shell files in parallel | Existing ownership and main-integration race, not useful parallelism. |
| More workers to overcome missing acknowledgment | Width cannot supply authority or make an unreceived request an agreement. |

## Order of operations

**Active exception:** source-safety implementation and isolated E0 design/additive candidate proceed
now under the exact section-2 grant. The sequence below governs production integration and native
acceptance; step 1 is not a predecessor of every independent authoring node. No existing SH3 file,
project/package file, live data or shared store is touched by the admitted candidate.

The Owner's `note-atlas-candidate-first-unit` also removes the complete E0 design document as a
predecessor of its already specified deterministic identity/binding unit. The remaining detailed
design is input to subsequent work, not an excuse to idle this source writer.

| # | action | cost | why now |
|---|---|---|---|
| 1 | Obtain actual resolution of full native request ID and section-2 updates | External acknowledgment; not an estimated timer | Code permission cannot be inferred. |
| 2 | Reconcile current main, registration, content decisions and exact source/test seams | Bounded read/rebase/design checkpoint | Main has advanced since original source evidence. |
| 3 | Admit E0 design-slice, safety spike and numeric resource budgets | Owner + triggered independent gates | Queue, source, memory, storage and telemetry floors precede code. |
| 4 | Execute E0-SAFETY, then one E0-BUILD writer under TDD | Track budgets above; estimates, not measured speedup | Source safety and one identity contract constrain all consumers. |
| 5 | Join owning Shell integration serially | Owning conductor's agreed budget | Real native entry path closes here, not in a fake factory. |
| 6 | Execute E0-PROOF and independent gates; repair named findings | Bounded proof loop | Code/demo/test evidence must describe the same revision. |
| 7 | Regenerate after audit, read back state and seek Owner horizon closure | Deterministic mechanics | No false completed/private-published/clean-state claim. |
| 8 | Only then ask Owner to admit the next vertical stage | New phase decision | Architecture completeness does not admit every implementation phase. |

## Harness and fan-out contract

Width <=4 across active author/review/decision seats. No autonomous child fan-out by a worker.
Each worktree is separately registered, with its own branch/index; each shell call sets cwd/identity.
Current worker writes and commit-floor checks were observed. `coord doctor`'s older per-harness
edit-boundary qualification is historical and is not promoted to a fresh current-version proof.
No automatic fallback from an enforced boundary to a merely observed one is allowed.

Transient failures are reported and retried only with a named transient cause and bounded backoff;
accepted observations/side effects are not blindly repeated. Join requires all affected hard floors.
One failed track blocks its descendants, not unrelated admitted work. Review repairs drain a finite
named finding list, at most two passes before explicit escalation. No source/authority floor is
silently traded for deadline, token budget or fan-out.

| Completed | Remaining | Best next action |
|---|---|---|
| Owner corrected the blanket freeze; exact safety writer/files recorded and probe dispatched in its own tree. | E0 design and safety results, candidate implementation/proof, actual shared-integration agreement and native-product acceptance. | Join the safety/design receipts and dispatch the exact additive candidate; pursue shared integration separately without human relay or repeated polling. |

## Active live-reader horizon

The earlier first-unit table above is retained as checkpoint history. The first Core unit is now
joined with 38 independently executed tests. The active horizon is Owner turn 12,
`note-atlas-live-reader-horizon`, on baseline `054b8b56`: current main `ca7443e8` plus reviewed
Atlas commits, reconciled only in the Conductor tree.

| Track | Capability | Current state | Bound / dependency |
|---|---|---|---|
| Common contract and F | Reasoning, then independent review | Codec/validated foundation joined; continuation joined `f8a6df06` | One identity authority, full compiler buffer distinct from source page; unknown coverage remains absent |
| Native enumeration investigation | Independent review | Joined `8450ce06`; 29 PASS / 2 NOT_PROVEN | Ordinary-local/reparse exclusion only; symlink privilege not changed |
| E/D/S producers | Reasoning, then independent review | Repaired, reviewed and joined; latest root bootstrap `41dc0501` | Actual inventory, Roslyn observations and bound source; FileLimited is not a project-compilation claim |
| Q/N composition seams | Reasoning, then independent review | Final N `a8897914` joined `6583298e`; independently 272 Core / 45 native cases | Original selection loss fixed; separate peer-oracle corrections use own-HWND MTA client. No query/source/grant change |
| Detached runner | Reasoning | `a0ffcee3` joined `cc67f7c6`; actual synthetic 45 PASS, intended source mutation fails | 26/26 author leaves; two files frozen; author evidence remains distinct |
| Independent native checkpoint | Independent review | Complete: real-root 23 PASS, six N/A; independent bytes/spans/image/intended-red/clean-after receipt | 15 initial leaves plus three evidence leaves and one bookkeeping leaf reported afterward; conservatively 19/21, not 18 by excluding bookkeeping |
| Record and Owner close | Deterministic mechanics, then independent review | Owner turn 32 accepts only the detached horizon | Evidence and capture persist with pins; no shared-host or complete Code Atlas acceptance |
| Current-main shared-host admission | Reasoning and deterministic mechanics | Local compatibility sealed `be3ace85`; packet prepared; 272 Core / 45 native compatible on unchanged inputs | Worker 24/24; one request `req-01M2CAXKH01J8SMQV1HBCCAN08` OPEN. Production lease/membership/async-IPC decisions require review; no existing adapters authored |

Q's turn-20 correction returned after 10/12 newly funded leaves; N after 10/10. F's receipt
reported 17 top-level calls and seven wrapper leaves against eight allocated leaves; that
ambiguity/overrun remains recorded, not relabelled a budget success. The six specialist review
leaves did not include the additional Conductor readback/replay/join calls. The earlier total
must not be interpreted as an all-inclusive orchestration cost.

Compatibility/admission preparation used 24 worker leaves plus separately recorded Conductor
mechanics. At that historical checkpoint the next adapter scope had not yet been admitted.
Owner turns 38-42 subsequently admitted the exact Core/Shell branch-local manifests in
`session-contracts` section 2. The earlier N -> runner -> independent nodes had real data and
gate dependency, so widening cannot shorten that chain. Conductor documentation can proceed
while the runner authors its two files. The existing width-four/no-worker-fan-out contract
stands. Failure drains a named finding list; a fired budget returns to Owner, never silently
widens or skips proof.

### Recorded rework and scope boundary

N's selection correction used 8/8 leaves, then 6/6 for discriminating diagnostics, 4/4 for the
current-provider test, 4/4 for its null-before-selection boundary, and 5/6 for the actual
owned-window MTA client. The product bytes stayed frozen through the test-only corrections.
The shared STA helper, private caches and product expectations were not changed to obtain green.
The source contracts and failed probes remain in the investigation.

Runner used 26/26 leaves. Independent proof used its first 15 and received six more because the
Conductor's output recipe shared a synthetic parent and final source/image/cleanliness checks
were unfinished. Its refusal is retained, never called the intended mutation red.

Primary was observed at `6d3e281a` after CV-2 landed. Both native counterpart requests are still
open. This does not broaden the Atlas baseline or grant existing host/IPC/main integration.

Every delegate has its own worktree and branch. The Conductor controls scope, gate release and
local merges; no worker self-admits. At the historical detached checkpoint no existing
Core/host/IPC adapter or CV2 file was assigned; the later exact exceptions are in section 2.
Observed SH3 ancestry is integration evidence, not acknowledgment. Symlinks remain unproven and
outside the supported input space; no privilege change is permitted to clear that limit.

## Production convergence checkpoint

**Goal:** resolve production admission and lifetime blockers, commit the real Core-to-Shell
handoff, and prove the next integrated journey. **Not in scope:** primary/main changes,
publishing private proposal history, or releasing later diagram/AI stages. T2; total width
four, at most two code writers. Conductor's resumed checkpoint budget is 60 calls, not an
erasure of prior orchestration cost. No measured total runtime estimate is available.

| Node | Capability | Input and exit condition | Dependency and current state |
|---|---|---|---|
| NQ diagnosis/repair | Reasoning | Actual abort cause, then required clone/linked/nested and hostile/ABA controls pass without weakening confinement | Qualified mechanism joined `acaf4dca`; parent pre/post join 350/350, NQ 29/30. Production cleanup condition remains |
| Shell lifetime repair | Reasoning | Factory/dispose/clear/admission fault oracles, recovery and idempotent final drain/disposal | Complete at component level: 44/44; eleven semantic reds, parent 90/90 before and after join; SRE/Test conditions retained |
| Membership gate | Independent review | Narrow Security/Test acceptance of qualified native/admin/currentness evidence | Test PASS; Security conditional qualification, not production clearance. Retained-cleanup correction and six targeted review leaves released |
| Production Core handoff | Reasoning | Actual committed issuer, async transport, remote reader and factory/view-model surface; no callable success stub | Owner 50 funds sixty new C leaves, regular48/108, in fresh verified runtime tree; first leaf closes daemon/path readback. Setup marker exception pending |
| Real-window integration | Reasoning | MainWindow attach/replacement/awaited close consumes the committed Core surface | Depends on Core handoff and repaired Shell owner |
| Integrated proof | Independent review | Real Architecture opener to inventory/file/member/source/Back, replacement/revocation/cancellation/disposal | Depends on joined production source; component and detached proofs do not satisfy it |
| Record/join | Deterministic mechanics | Scoped commits, audit then derived regeneration, exact evidence and remaining boundaries recorded | Conductor owns this; no primary merge or push |

The two repairs are independent: different authored files and worktrees, neither changes the
other's contract, and neither owns a shared exclusive resource. Both gates must succeed before
their descendants are admitted. A failed repair does not cancel the other. Reuse retained
agents; no duplicate exploration or autonomous fan-out. No automatic retry after a semantic
failure; an infrastructure timeout needs an identified transient cause before one retry.

The longest chain is NQ -> membership gate -> Core -> real window -> integrated proof.
In a **unit-node model only**, the six implementation/review nodes have work T1=6 and span
T-infinity=5; two-way scheduling has an upper bound of 5.5 units and cannot beat five units.
This is not a latency measurement. Parallel Shell repair can overlap only the independent
membership work; more workers cannot remove the Core handoff dependency.

The repair worklist is finite: three observed NQ failures and four Shell source findings.
Each pass must resolve a named item or return new discriminating evidence; after two passes
without reduction the Owner must change the plan, not repeat the same work with a larger cap.
Budgets and source ceilings remain prospectively controlled by the Owner.

Evidence and the distinction between executed results and source-reviewed risks are recorded
in `docs/proof/code-atlas-production-adapters.md`.

Owner turn 43 separately allocates eight review leaves: NQ Security/Test and Shell SRE/Test.
Conductor readbacks/replays remain separate. The diagnostic tranche is a decision boundary:
the changed pin/stage/native result must be read before a cause-specific NQ repair is released.

Owner turn 45 prospectively extends the resumed main-line checkpoint from sixty to seventy-two
leaves, without resetting cost: six Shell readback/join, three NQ disposition, three records.
The Shell test-source limitation and local join are resolved. Four review leaves remain for
NQ. The native abort receipt has been read; cause-specific release is with Owner, not silently
inferred from a diagnostic success. The programme and implementation/verification tasks stay open.

Owner turns 47-48 supersede that historical checkpoint: one coherent NQ pass completed the
qualification, then the candidate joined with production cleanup explicitly blocked.
The cumulative main-line ceiling is now 126, prospectively granted after the 102 checkpoint;
six record/join mechanics and eighteen cleanup/review/runtime-estimate-disposition leaves.
No past overrun is reset. Actual implementation, verification and coordination tasks remain open.

### Actual runtime release, Owner turn 50

The cleanup candidate joined as `c90a9cce`; Conductor observed 355/355 before and after.
Runtime starts from the accepted `1e96dd8e` tree in
`C:\Projects\ai-de-atlas-production-core-runtime`, not the predecessor tree's older Shell.
The same C agent has sixty new leaves and the exact section-2 twenty-file ceiling.
Blocking membership pins are operation-critical-section resources, not idle reader-lease
resources. Real connection identity and explicit ownership of new readers are mandatory.

The budget separates author work from eighteen independent review leaves (Security/DS/Test
six each), sixteen conditional Shell handoff leaves and fifteen independent real daemon/
MainWindow proof leaves. The Conductor ceiling is prospectively 184, without resetting the
recorded 128 count or previous overruns. No main/push/private-reference import is granted.
