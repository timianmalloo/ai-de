---
id: proof-code-atlas-production-adapters
title: "Code Atlas production adapters - evidence and open gates"
type: doc
status: draft
owner: "@timianmalloo"
tags: [code-atlas, proof, ipc, native, lifecycle]
links:
  - { to: spec-addendum-e-code-atlas, rel: implements }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
  - { to: coordination-code-atlas, rel: relates-to }
  - { to: note-atlas-defect-id-reconciliation, rel: relates-to }
review-by: 2026-12-13
summary: >-
  The compiled public render seam and reviewed Shell lifetime component are joined.
  The native membership candidate now has a conditional qualification result, with
  cleanup-timeout accounting still required before production use. Component, compile-only
  and detached proofs are not proof of the real daemon-to-MainWindow journey.
---

# Production adapter checkpoint

**Latest independent-proof scope:** Owner 58 has released the same one-file proof writer
to author and execute the real daemon/MainWindow journey. Healthy acknowledged lease release
during workspace replacement is the external lifetime boundary. A public same-live-scope
idle barrier is not established and is not invented; the earlier in-process idle-Git proof
remains a distinct claim. The first proof attempt produced no execution evidence.

Owner 59 adds a visible-source proof gate after the actual run produced data/lifecycle
evidence. The default 1280-by-900 capture clips the source in a narrow right pane.
Bound text and UIA visibility flags do not prove readable pixels. The same proof may
use the existing rendered focus and Window/Maximize pane route, measure clipped text/
outline viewport geometry, and recapture. No product layout change is admitted, and
default placement remains a separate usability finding.

The first supported-maximize proof failed. The pane remained 247.41 DIP wide, the source
viewport intersection was empty, and only 60.41 DIP of a 218.45-DIP selected label was visible.
The proof focused the rendered files control and raised the menu Click, but did not yet
record logical focused-stack identity or actual layout mutation. Post-action geometry
was not collected because the width assertion ended the run. Owned daemons were forcibly
reaped in that failed run; its cleanup is not the earlier successful normal exit.

Owner turn 60 funds eight diagnostic proof leaves for the real focus/router/layout path.
Click-to-router wiring is established; keyboard focus alone is not logical pane focus.
Both command handling and the intended stack's mutation must be observed, and failed
post-action geometry must be retained. No product resizing or private-state change is
authorized by this investigation.

The next discriminator ruled out missing logical focus and missing command routing:
Code Atlas's surface/`zone-right` were focused, exactly one maximize command was handled,
and the left zone disappeared. The test's legacy `StackState.Maximized` assertion was
invalid for the zone model and is retained as an oracle correction, not proof of layout
failure. Parent read `ZoneLayoutService.Maximize`: it records a memo and collapses other
non-Center zones without enlarging the selected tool-zone extent. Post-action source
geometry remained empty independently of that wrong assertion.

Owner turn 61 therefore authorizes a separate product-default correction: Code Atlas opens
as Center reading content using the existing kind preference/placement override. Explicit
and restored user placement must remain unchanged. The independent proof cannot manufacture
that layout; it follows only a reviewed product commit and must demonstrate readable
default source, selected label and highlight with the real daemon/window journey.

Center-placement measurements now separate two effects. At 1280 and 1440, source text
and highlight fit the respective 256.94/346.54-DIP source viewports with unchanged 13-DIP
fonts. The selected outline label remains clipped: 218.453 DIP required, 171 DIP visible.
Only those two label oracles fail in the 118/120 affected run; the separately executed
Sessions namespace is 121/121. Fixtures and dispatchers cleaned up.

Owner turn 62 permits the one additional ReaderView file for finite-width, untrimmed
outline wrapping. The old single-line glyph rectangle cannot prove the new layout;
actual wrapped glyph lines and accessible/full label text must be checked. No reader
source-grid/font/window-size or Core change is authorized. The candidate remains
uncommitted and unaccepted until combined geometry/lifecycle/placement gates clear.

The final four-file candidate `6591b5e9` now has UX/SRE/Test placement and wrapping
clearance. Parent read the actual rendered glyph geometry and independently ran the
combined UI/Sessions suite, 232/232; after joining as `dd84702b`, the same joined suite
again reports 232/232, zero skipped. At both viewports all source/highlight bounds fit
and the full selected label is represented by non-empty, finite wrapped glyph runs
inside its 176-DIP viewport with unchanged 13-DIP typography and accessible name.

This is not yet the independent actual-daemon pixel verdict. The proof tree adopted
only the reviewed UI commit, preserving its prior uncommitted harness bytes and failed
captures. Eight further proof calls are released for the normal new Center default,
correct wrapped-line geometry and full real-runtime lifecycle replay—no maximization
or private-layout workaround.

This is an incomplete production Proof Pack. Addendum E remains candidate. No primary-main
integration or push is claimed. The private proposal/TheTerrace corpus is not part of this
delivery record.

## Revision and outcome ledger

| Surface | Pin | Observed outcome | Boundary |
|---|---|---|---|
| Joined Conductor | `639be9d389747904b61d6298cc65f96ba6075ade` | Core render seam and reviewed Shell component joined; integrated 90/90 Shell cases | No runtime issuer/factory or actual MainWindow integration |
| Core seam author | `8d091e5197db70093909149a8a8e97eb3116c604` | 326/326 independently repeated; 54 reader tests | Compile-only facade fixture is not operational registration |
| Core membership candidate | `d8d83de8034570365ab9c6665f118a4dcb2aa256` | Author 340/343 total; Conductor independently repeated 14/17 membership, the same three failures | Blocked; not joined, wired or approved for production |
| Original Shell milestone | `dade5c779012ba9e4c1a8934004d05376a3b4732` | 78/78 initially; four lifecycle findings required repair | Historical result, not accepted without its repair |
| Shell lifetime repair | `8e691c6a48a197ece45290a44b8ccd488022d440` | Author 90/90; parent 90/90 before and after join; eleven semantic reds retained | Joined as `639be9d3`, after SRE/Test component gates; no real-window claim |

## Claim and oracle ledger

| Claim | Concrete evidence and source | Oracle / red observed | Confidence and residual risk |
|---|---|---|---|
| Public render DTOs reject the observed invalid source and continuation cases | `AtlasReaderWireTests.cs`; independent `atlas-core-final-seam/core-seam-final.trx`, 326 passed | Parent invalid-input probes rejected for their intended invariant; author semantic reds retained for zero source length, split-surrogate highlight, nonprogress and producer byte inconsistency; final serializer/decoder empty-source red then green | Verified for the executed cases; not transport or runtime admission |
| Non-friend assemblies can consume the public render ports | Non-friend compilation fixture in `AtlasReaderWireTests.cs` | Public port/DTO compilation succeeds; deliberate hidden Core type use produces CS0122 | Verified accessibility boundary only; static-abstract facade signature fixture is not a production factory |
| Current membership candidate is not ready | `.artifacts/atlas-reader/nq-understanding.trx`, then diagnostic TRX/JSON below | Replacement, clone/unpacked and nested/unpacked cases fail; source-root notifications complete with native error 995, zero bytes and no actions | Verified abort receipt. The mechanism causing the abort remains Inferred |
| Some membership defenses execute | Same final TRX: hostile-fsmonitor and packed-ref namespace ABA cases pass | Unguarded hostile fixture must create its marker; guarded capture must not. Packed-ref insertion/deletion must invalidate even when final bytes agree | Verified named cases only; does not clear NQ1/NQ2 overall |
| Repaired Shell lifetime behavior satisfies the bounded fault oracles | `AtlasSharedHostAdmissionTests.cs:16-227`, native tests and `SurfaceContentTests`; parent joined receipt below | Eleven repair semantic reds, then recovery/disposal/diagnostic assertions pass; original two semantic and two mutation reds also retained | Verified named component behavior; real-window integration remains |

Source paths for Core above are under `src/AiDe.Core/Understanding` and
`tests/AiDe.Core.Tests/Understanding`. Shell tests are under
`tests/AiDe.App.Tests/Workbench/Understanding`; factory tests retain their existing path.

## Shell lifetime review and executed repair

The initial SRE review of frozen `dade5c77` identified the following source mechanisms.
The fourth was subsequently qualified by execution: inventory exceptions were already
contained by the reader. The real reproduced new defect was host admission cleanup.

| Finding | Source mechanism | Required falsifying test |
|---|---|---|
| A failed transition can poison later attach and disposal | `AtlasWorkspaceOwner.ReplaceAsync` awaits the previous transition before containment; factory and lease/reader disposal may fault | Throw once from each boundary, then successfully attach a valid reader and await final cleanup |
| One clear callback can prevent the other views and cleanup from running | `InvalidateViews` invokes callbacks without per-callback containment; dispatcher invalidation is tracked without observed fault handling | First of two callbacks throws; second still clears; attach/dispose completes and a stable diagnostic is emitted |
| Final lifetime primitives are not disposed | Disposed-path early return precedes lifetime CTS disposal; admission semaphore has no disposal path | Await final drain and disposal, observe both primitives released, and repeat `DisposeAsync` without a second release |
| Post-assignment load failure leaves stale host state | `AtlasLoadingHost` assigns `ReaderView` before awaited load; error path changes text without clearing activation/registration/view | Fail load after assignment, observe unavailable with null view and released host-owned state, then activate successfully |

The first two were SRE advisory blockers escalated to Owner. The repaired component now
retains lease/reader ownership when disposal fails, retries cleanup on a later transition,
isolates failing clear callbacks, observes publication faults, and drains operations and
active admission before disposing lifetime primitives exactly once.

Conductor opened the exact tests after the Test review's source output was truncated.
Assertions check retained owner identity, disposal-attempt counts, the second clear callback,
pending close before drain, idempotent close, primitive disposal, activation/registration
clearing and successful later admission. The ten initial red messages and the separate
admission red were read directly. The latter expected cancellation but got
`ObjectDisposedException` from the semaphore: it is not evidence of a new inventory failure.

GATE Shell component lifetime - SRE/Test - verdict PASS with conditions: the caller awaits
`DisposeAsync`; failed cleanup retains ownership for retry; real Core factory and MainWindow
composition remain unproved. Parent integrated run: 90 executed, 90 passed, zero skipped.

Owner turn 43 authorizes the same writers to execute a three-leaf diagnostic checkpoint
before nine further NQ repair leaves, and twelve Shell repair leaves. Eight targeted review
leaves are separate. This is permission to produce evidence, not acceptance of either candidate.

## Change reach and remaining gates

| Required surface | State |
|---|---|
| Existing workspace content-read policy basis | Established in the admission design; Atlas does not depend on Conversation/DocumentSession |
| Native source/admin membership/currentness qualification | Blocked as above |
| Core issuer, global admission limits and lease lifetime | Not implemented/proved in this checkpoint |
| Async endpoint/server, isolated persistent remote reader | Synthetic contract direction qualified separately; production path not implemented/proved here |
| Render projection/wire/client types | Compiled seam joined; production transport composition still open |
| Architecture opener/native component | Reviewed component joined; actual MainWindow owner/factory attachment absent |
| Actual MainWindow attach/replacement/final close | Not implemented; requires committed Core factory/view-model handoff |
| Real product file -> member -> source -> Back and revocation | Not proved; detached real-source proof does not substitute |

## Evidence locations and durability limits

Conductor session evidence is retained under the session-state `files` directory:

- `atlas-core-final-seam/core-seam-final.trx`
- `atlas-shell-independent/shell.trx`
- `atlas-shell-independent/factory-existing.trx`
- `atlas-membership-independent/nq-independent.trx` (17 executed, 14 passed, three failed;
  test command exit 1; the source HEAD remained `d8d83de`)
- `atlas-shell-repair-independent/shell-repair-independent.trx` (90/90 before join)
- `atlas-shell-joined/shell-joined.trx` (90/90 after joining the exact scoped Shell commits)

Membership author receipts are retained in the Core execution worktree:
`.artifacts/atlas-reader/nq-first.trx`, `nq-second.trx`, and `nq-understanding.trx`.
The first run had read-only Git-object cleanup failures and a real USN-only ABA miss.
The second had eight snapshot-currentness failures. They are historical failures, not
the final three-error result and not discarded to improve the record.

Shell author receipts remain in its worktree under `artifacts/atlas-shell`:
`atlas-shell-red.trx`, `atlas-shell-mutants.trx`, and `atlas-shell-final.trx`.
Repair receipts add `atlas-shell-repair-red.trx`, `atlas-shell-repair-admission-red.trx`
and `atlas-shell-repair-final.trx`.
These machine-local raw receipts are retained but are not claimed to be committed.
The committed summary names their exact checks and limitations. Both source worktrees
must be kept while they hold unjoined code and evidence.

## Operator questions

The qualification helper emits `atlas.membership.captures` and
`atlas.membership.duration`; its cancellation test observes an emitted capture.
Qualification-only diagnostics now supply the previously missing pin, stage and native
completion result. They do not change the invalidation predicate or publish user paths on
the normal product path.

Production questions remain open: which admission failed, which scope was revoked, how long
an operation took, whether a dirty transport was replaced, and whether final cleanup drained
all operations. Synthetic measurements and component logs cannot answer those questions for
the still-unimplemented production path.

## Native abort diagnostic receipt, not cause acceptance

The first diagnostic patch misplaced two methods inside `try`/`catch` boundaries and did
not compile. `.artifacts/atlas-reader/nq-owner43-diagnostic.log` is retained. Owner turn 44
reallocated two reserved leaves to placement correction and a diagnostic rerun. Compilation
then succeeded and the same three failures reproduced, 14/17, in
`nq-owner44-diagnostic.trx`. The two-file diagnostic diff remains uncommitted and unjoined.

Conductor read that TRX and the four corresponding JSON receipts under
`.artifacts/atlas-reader/nq-owner44-diagnostics`:

| Case / JSON stem | First recorded completion | Result |
|---|---|---|
| Clone / `4db5840d5cf24871b3467874d115dd19` | `after-git-version`, source-root `clone` | Signaled; completion false, native error 995, zero bytes, no notifications |
| Nested / `3ea147a057c240f18bd5eab42f227f05` | `after-git-version`, source-root `clone\src` | Same abort result |
| Replacement / `c38f337f838e4f5ebe8ec6ed49d46aac` | `after-git-version`, source-root `clone` | Same abort result |
| Packed-ref ABA / `92b158c133324b8d82bf5bcc3fc4e28c` | `snapshot-currentness` | Completion true, error zero, 52 bytes, action 1 for `.git\refs\heads\main` |

The three failure receipts each have thirteen signals and zero dropped records. Their
first abort remains visible through final refusal. ABA also records matching admin/ref
notifications of 42, 32 and 20 bytes. This distinguishes an aborted watch from a demonstrated
Git namespace mutation. It does **not** establish why the watch aborted.

NQ is 15/22; seven leaves remain held pending Owner's cause-specific disposition. Regular
Core remains 34/48. No error/overflow is ignored, no namespace is suppressed, and no
Git-dependent production consumer has been admitted.

## Qualified candidate after the coherent pass

The preceding blocked checkpoints are retained as history. Owner turn 47 released a
coherent pass; the writer used ten of eleven leaves, cumulative **NQ 29/30**, with regular
Core still **34/48**. Candidate `c7c941537b34ab342071109c3f32a8b2a697c156` changes only
`AtlasGitMembership.cs` and its tests. It remains unjoined pending the next disposition.

Owner turn 48 subsequently authorizes the qualification-only join after committing these
pending records. That is not production membership clearance: the cleanup-timeout condition
below remains a blocking predecessor of any real consumer.

The records were committed as `39b9b43d`, then qualification source joined as `df17c69a`
and `acaf4dca9e96328711a1517a06964e76eafdd1c5`. Conductor compared both files with the
reviewed `c7c94153` and observed no difference. The joined Understanding run recorded
350 total, 350 passed, zero failed in
`files/atlas-nq-qualified-joined/nq-qualified-joined.trx`. This supersedes the preceding
unjoined status, not the production-cleanup block.

Immutable issuer metadata is captured before thread death. Diagnostic failure is explicit
and does not prevent cleanup. A bounded shared native issuer keeps the issuing thread alive
for the operation lifetime and drains when the last ownership lease is released.

The controlled cause run (`nq-owner47-cause.trx`) preserves the original three failures,
18/21. The final author run is 350/350, including 24 NQ and 54 reader cases. Conductor
independently built and replayed the Understanding set, 350 passed, zero skipped, in
`files/atlas-nq-qualified-independent/nq-qualified-independent.trx`.

| Raw receipt under `.artifacts/atlas-reader` | Observed distinction |
|---|---|
| `nq-owner47-cause/f749a1de3f484eabb05140a1c6308eaf.json` | Issuer 31064 is alive/pending at root pin, then dead/joined with error 995 after Git version; the handle and OVERLAPPED address remain unchanged and owned |
| `nq-owner47-final/fe8a0fc202404ea2ba54ae4569cbf86f.json` | Fixed issuer 8792 stays alive/pending, error 996, across the same boundary with unchanged owned resources |
| `nq-owner47-final/a32457ef3b9a42418654f53ad10f464a.json` | Canceled native creation records closed handle and released OVERLAPPED |

The Test Architect opened the new lifetime oracles and cause/final receipts. The
exited-owner control asserts zero explicit cancellation/disposal before the abort;
live-owner mutation and explicit cancellation are separate controls. The shared issuer
asserts one native thread/two leases, then zero/zero; canceled creation, pre-cancellation,
diagnostic loss, hostile configuration, required repository forms and ABA remain covered.
Conductor's compact raw table did not print the cancellation/disposal columns, so its
printed table alone is not cited as independent observation of those zero counts.
Before the qualification join, Conductor separately asserted the raw paired-stage fields:
the original dead issuer retains the same handle/OVERLAPPED and zero explicit cancel/dispose;
the fixed issuer stays alive/pending with zero cancel/dispose. A mismatch would abort the join.

GATE NQ Test - PASS for the bounded qualification; no runtime/factory/MainWindow claim.
GATE NQ Security - PASS WITH CONDITIONS for qualification evidence only. Production use
still requires explicit cleanup-timeout accounting and confinement of the diagnostic sink.

The Security condition is concrete: if native cancellation does not complete inside the
two-second cleanup window, buffers must remain owned, but `PinSet.Dispose` can stop before
all other safe pins and issuer accounting are released. Before production consumption,
prove that timeout path or retain its unresolved native resources under a bounded, tracked
failure owner while releasing all resources that are safe to release. The current green
suite does not simulate a kernel/driver that refuses to complete cancellation.

Qualification diagnostics contain administrative paths, native handles and notification
names. They must remain qualification-only, never production telemetry, wire errors or
audit content. Membership never grants content permission: the actual adapter still needs
the separate existing-workspace content-eligibility policy.

### Concrete runtime remainder

Resolve the cleanup-timeout production condition, then implement and review the actual
Core scope issuer, global limits, async endpoint/server, isolated remote reader and
committed factory/ViewModel handoff. Shell must consume those exact signatures for real
MainWindow attach/replacement/awaited close. Finally prove the daemon-backed Architecture
file/member/source/Back path with cancellation, revocation, replacement and shutdown.
Neither normative Addendum E acceptance nor main integration is granted by this checkpoint.

Owner turn 48 allocates ten existing regular C leaves to retained-failure cleanup and four
to a code-grounded runtime estimate. Regular ceiling stays 48 and NQ stays 29/30. Six new
targeted review leaves are separate. The next implementation must prove safe-other cleanup,
strong bounded retention of pending native work and reservations, admission debt accounting,
and idempotent recovery after completion, including canceled creation and final issuer drain.
Injected timeout evidence must remain labelled as injection rather than a stalled-kernel run.

## Retained-cleanup candidate and gate

Candidate `7d78e773b2e5b7cc5eec2716a30c0fc9c6773128` introduces a bounded strong cleanup
ledger, retains failed pin and unpublished-creation ownership, attempts safe peers, and
keeps the issuer lease charged if final drain fails. The writer used eight regular cleanup
leaves. Three isolated semantic reds preceded the fix:

| Retained author TRX under `.artifacts/atlas-reader` | Actual pre-fix failure |
|---|---|
| `owner48-red-RetainedPinTimeoutStillReleasesIndependentPins.trx` | Independent `.git\index` remains locked |
| `owner48-red-CanceledCreationCleanupTimeoutRetainsItsIssuerCharge.trx` | Expected one issuer lease, observed zero |
| `owner48-red-FinalIssuerDrainTimeoutDoesNotReclaimTheLease.trx` | Expected one retained lease, observed zero |

Author `owner48-final.trx` is 355/355. Conductor read those three red messages directly,
then independently built/replayed 355/355 in
`files/atlas-cleanup-independent/cleanup-independent.trx`. New source assertions cover
safe-peer release, one retained owner/buffer/issuer lease after pin or creation failure,
no new Git invocation while debt remains, final-drain debt even when live-thread count is
zero, explicit retry to zero, strong ownership after caller loss, and capacity reservation
before native work. These are labelled timeout injections, not a hung-kernel experiment.

GATE retained-cleanup Test - PASS for the fault, charge, GC and retry oracles.
GATE retained-cleanup Security - PASS with native readback and production-boundary conditions.
Conductor completed the missing native ranges: `NativePin` retains failed unpublished cleanup
and closes its independent data handle; `NativeDirectoryChange.Dispose` does not reach the
buffer-free operations when completion times out. This satisfies the stated source readback
condition, not broader Windows fault coverage.

The joined helper remains a qualification mechanism until actual runtime admission is
implemented and reviewed. Qualification diagnostics must not reach production logs/errors.
The existing content-eligibility basis, peer/epoch/root binding, global runtime limits,
async endpoint/server, isolated client and real Core-to-Shell factory are still required.

### Runtime funding is not yet ready

The provisional sequence is admission composition, awaited server/facade, isolated remote
reader, actual factory/ViewModel handoff, then daemon/native integration proof. The writer
estimated 35-60 author/verification leaves, excluding independent reviews, but explicitly
flagged missing method-body and disposal grounding. Owner turn 49 releases the two unused
regular calls to finish those reads and the exact twenty-file reconciliation. No runtime
code may be authored from that provisional estimate.

Owner turn 50 subsequently funds the actual runtime tranche, conditional on the first
literal daemon/bootstrap read at the verified starting pin. This supersedes the funding
hold, not the evidence requirements. The new operational oracle includes ordinary Git
writes while Atlas is idle: qualification pins must not commandeer the developer workspace.
Real Core connection identity and explicit new-reader ownership must survive the ViewModel
and Shell handoff; borrowed interfaces do not silently become owned.

## Actual Core runtime candidate, not yet joined

The fresh runtime tree produced `d811cd829de89adfa4e0ca8fb02b7eeeb6bdad23`, then
`b1c6f74a0c5b0c7775420ac63ad814aa05c9bc0c`, nineteen authorized source/test files.
Author used 58/60 new leaves, regular 106/108; NQ remains 29/30.

The public handoff exists in code, not a compile fixture:

- `WorkspaceClient.CreateAtlasReader(): IAtlasWorkspaceReader` constructs a separately
  owned Atlas connection from the client's actual workspace identity.
- `MainWindowViewModel.AtlasReaderFactory: Func<IAtlasWorkspaceReader>?` is supplied from
  that method independently of the displayed directory name.
- Shell owns each new reader. Borrowed queries/commands remain borrowed. MainWindow was
  not changed in this Core candidate.

Conductor directly read those deltas and compared the frozen generic `IpcClient`,
`IpcFraming`, `IpcContract` and `CapabilityRegistry`: unchanged.

| Evidence | Observed result | Limit |
|---|---|---|
| Author `runtime-core-final.trx` | 437/437 | Author evidence |
| Author `runtime-app-final.trx` | 81/81 | Its selector omits unchanged factory tests |
| Parent `files/atlas-runtime-independent/runtime-core-independent.trx` | 437/437, zero skipped | Includes actual daemon launch/native-Q and IPC regressions |
| Parent `files/atlas-runtime-independent/runtime-app-independent.trx` | 96/96, zero skipped | Adds existing `SurfaceContentTests`; not real MainWindow proof |
| Actual daemon output | Derived pipe listening message; exit zero | The test-generated log is rewritten by reruns; preserve per-run TRXs for provenance |

The author's `runtime-author-receipt.txt` names source/receipt continuity, idle Git writes,
publication/cancellation/drain and charged-buffer controls. Its process measurements are
approximately 90 MB peak working-set increment for the large-source scenario; the 64/16 MiB
figures are charged buffer/encoded reservations, not a total CLR/Roslyn memory bound.

Test conditionally cleared runtime oracles. Parent read the actual IPC and read-budget
tests after the review's source extraction was truncated: peer-scope refusal, epoch change
at commit, EOF drain ownership, unsolicited-byte refusal, deadline framing continuity,
auth parity, duplicate-registration refusal, FIFO cancellation and retained reservations.
DS conditionally cleared the inspected persistent/abort-on-dirty transport and awaited
publication/drain implementation. Its historical 79-assertion spike citation is not counted
as runtime proof.

The runtime Security gate is still blocked on evidence coverage. The prior reviewer
returned an old cleanup review; that PASS was rejected. Its corrective attempt exhausted
the six-call allowance with two failed source reads and a limited factory inspection.
Owner turn 52 funds a fresh six-call, narrowly scoped authority review in a separate clean
tree at `b1c6f74a`. No runtime join or Shell handoff is authorized by an irrelevant review,
and no code vulnerability is asserted merely because that review failed to cover the code.

The fresh six-range authority review completed and returned **BLOCK** on a different,
substantive issue: scope expiry versus an in-flight prepared-response publication.
Conductor traced the actual endpoint/server/issuer path and observed operation disposal
before the response write. The externally visible interleaving is not yet executed;
no disclosure is claimed. See `investigation-code-atlas-publication-lifetime`.
The Core candidate and Shell handoff remain unjoined/unreleased pending that investigation.

The repair candidate `fa89ed06` independently ran 447 Core/IPC and 96 App/factory cases.
Its raw blocked/revoke/partial/drain observations correct the original release-before-write
case. Security clears that narrow ownership repair. DS and Test remain blocked on a new
post-full-write cancellation poll and missing exact resource-counter assertions respectively.
The investigation records these separately; neither the new green runs nor Security's
narrow clearance admits the whole runtime.

The cross-branch ID allocator also detected a bookkeeping collision before integration:
published `origin/main` at `76c6d430a19492ffcd9b126e641ee70cda57ec69` has different DC-177
and DC-178 entries and now reaches numeric maximum 208. The locally allocated cleanup/structural-patch
records must be reissued without discarding either history. The ordinary derived-view gate
does not clear this separate failed allocator check. Owner disposition is pending; no
product-main merge or new ID allocation is inferred.

Owner-54 register-only reconciliation now restores the published 177-208 definitions
verbatim from the pinned revision. The two Atlas meanings and their complete text are
preserved in `note-atlas-defect-id-reconciliation`, keyed by origin revision, old numeric
ID and title. No replacement number is guessed: numeric 209 is already allocated on
`conductor/addendum-c`, so Atlas canonical numbering and full semantic-duplicate
disposition remain pending. The current Conductor register passes the collision check
against the pinned published revision; old worker branches are retained historical copies,
not merged over that reconciled register. Source repair does not wait for pending numbers.

## Conditional Core join: one idle-oracle failure remains

The final publication guards/resource oracles cleared Test and DS, with Security's
ownership/revocation contract unchanged. Conductor joined the four code-only commits;
`4855151f0328bd3c0623e3a5e23f58e2cb9c9248` matches the reviewed nineteen-file candidate.
No old worker register or private history was merged.

`files/atlas-runtime-joined/joined-runtime-core.trx` records 458 passed, one failed, total
459. `NativeScopeReusesConnectionPreservesQReceiptsAndReleasesGitPinsWhileIdle`, line 274,
expected `ChargedOwners == 0` immediately after client Restore completion and observed one.
Conductor read the exact assertion. There is no wait for the matching server publication
drain before it, whereas the server releases resources after its full write.

That supports a timing hypothesis, not yet a final classification. Owner turn 55 releases
six calls to prove the specific request's client-complete/server-drain distinction and
correct only the demonstrated error. Zero ownership after matching drain, real Git
add/commit, stale-scope refusal and reusable receipts remain required. A persistent owner
after drain is not relabelled test timing. App integration gates and Shell remain held.

The request-correlated correction subsequently cleared review and joined as `4cfb8450`.
The current Core and App/factory joined gates are 459/459 and 96/96. That closed the
receive-versus-drain oracle gate and released the separately funded Shell handoff.

## Shown-MainWindow handoff candidate remains uncommitted

The handoff writer exhausted its sixteen new leaves (60 cumulative). Its three-file
MainWindow/WorkbenchShell/test implementation has no commit and is not joined.
`artifacts/atlas-mainwindow/atlas-mainwindow-final.trx` reports 113/115.
Conductor directly read both failed results:

- `Handoff_ShownMainWindow_OpenerReplacementAndCloseUseOwnedLease(false)`: about 30.106 s.
- The same case with `true`: about 30.108 s.
- Both surface `IOException` for locked `watcher.db` from outer fixture deletion at test
  line 157, not a recorded earlier await or assertion.

The actual test owns Core inside an asynchronous dispatcher-pumped body and deletes the
directory outside that body. Primary lifecycle failure versus cleanup masking remains
unverified. This shown fixture uses real borrowed Core queries and fixture Atlas reader
ports; it is not the reserved independent actual-daemon/MainWindow proof.

The next diagnosis must expose the exact pending stage/owner and preserve both primary
and cleanup errors. Skipping cleanup or stretching the timeout cannot establish correct
startup, workspace replacement or awaited closing. All uncommitted source and raw evidence
are retained in `C:\Projects\ai-de-atlas-mainwindow-handoff`.

The Owner-56 discriminator now identifies the pending stage. Both modes have completed
workspace startup but remain at the first ApplicationIdle await; their logs preserve the
primary pumped-body timeout and separate cleanup debt. Conductor read those logs and the
shared Background-priority pump loop. The owned full-dispatcher control completes idle
work, borrowed-query use, Core disposal and fixture deletion in about 154 ms.
The same MainWindow composition under the local full dispatcher is the next controlled
comparison; this is not yet a claim that every product lifecycle path is correct.

## ICD C/D-only corrective attempt, 2026-09-15 - blocked subscriber seam

**Scope and authority.** ICD received ten prospective calls, separate from integration50 and
IRP24. Ruling115 at main `33e9ae7e00e3edb2d44ecec006e5a3c354e69efd` acknowledges exact C/D paths.
B/E remain nonexecutable because the admitted scopes conflict with the unchanged-control-source
conditions; the closer is seeking clarification. No native or GUI work, whole cohort, newer-main
join, owner assignment, gate edit, or publication occurred.

**Exact uncommitted corrective diff.** Only four Core files changed: private `Lease` became
`ReaderLease` in `AtlasRemoteReader.cs`; `NativeWatchIssuer.Lease` became
`NativeWatchIssuer.NativeWatchLease` in `AtlasGitMembership.cs`, including same-file references.
No Composer lease inheritance, wrapping, conversion or allowed mint site was added. Only the
leading `AiDe.` became `aide.` in the three ActivitySource literals in `AtlasRemoteReader.cs`,
`AtlasDirectoryEnumerator.cs`, and `AtlasQueryService.cs`. Meter names, semantic suffixes, spans,
tags, and all existing test/control source remained unchanged. Source leases and explicit checks
preceded the edit; leases were released before the tests.

| Claim | Evidence and oracle | Red observed | Confidence / boundary |
|---|---|---|---|
| Composer mint guard no longer confuses private Atlas lease types | Unchanged `ALeaseIsMintedAtExactlyTwoNamedSitesAndByNoOtherPattern`: 1/1 passed, process exit 0; allowed-site array untouched | Original full-App TRX contains that exact failed test; raw file copied and parsed before the fresh run | Verified for this unchanged source-text control; no new GUI or full-App qualification |
| The three emitters now fall inside the privacy prefix | Unchanged `EveryActivitySource_IsUnderTheAideNamespace` passed in the fresh Core TRX | Original full-Core TRX contains that exact failed test | Verified for the naming guard, not all telemetry consumers |
| Directly coupled tests preserve their behavior | Core selection: 182 executed, 179 passed, **3 failed**, zero skipped; process exit 1 | All three currently failed directory telemetry cases were **Passed** in the original Core TRX | **Not satisfied; corrective commit withheld** |

**Primary failure, not a success-shaped summary.**
`AtlasDirectoryEnumeratorTests.EnumerateAsync_Limits_ReportActualDimensionAndBoundedTelemetry`
fails for `entries`, `depth`, and `descriptors` at its existing `Assert.Single` around line312.
The current test listener around line295 still uses
`source.Name == "AiDe.Core.Understanding.AtlasDirectoryEnumerator"`; the admitted emitter now
uses `"aide.Core.Understanding.AtlasDirectoryEnumerator"`. The three fresh collections are empty.
No assertion, expected bound, listener, or test file was changed to hide this.

**Class / sweep / derive / prevent.** Class: a producer's identity rename leaves a subscriber's
exact-name selector obsolete. Sweep: the three admitted emitter literals and directly coupled
tests were run; the observed mismatch is the one directory listener and its three parameter cases.
Derive: a shared identity would avoid two independently authored spellings, but introducing it is
outside this grant. Prevent: the existing activity-presence assertion detects the mismatch and was
observed red after previously passing; it must remain. Register/test repair is returned as a scope
request rather than silently expanding the admitted four product files.

**Exact requested seam:** authorize only the listener's source-name prefix in
`tests/AiDe.Core.Tests/Understanding/AtlasDirectoryEnumeratorTests.cs` to follow the admitted D
rename, preserving every assertion/expected value. This is not authorized by D's product-literals-only
grant, so no such edit has been made. Until it is admitted and the same selection passes, there is
no C/D corrective commit or qualified freeze.

**Raw evidence** (ignored, old files preserved):
`.artifacts/atlas-main-integration/ICD/01.json` (mint guard),
`02.json` (Core selection), `icd-composer-guard.trx`,
`icd-core-guards-and-coupled.trx`, and copied `original-red-AiDe.App.Tests.trx` /
`original-red-AiDe.Core.Tests.trx`. Both protected guard source files were checked unchanged against
HEAD `e2724fc6cdfac24bb549d5545ac9541c0c711baa` before execution.

**Ruling113(ii) request, not assignment.** Applying the newly landed gate's actual recursive
`Surface.cs` / `View.cs` suffix rule to this accepted Atlas directory yields exactly
`src/AiDe.App/Workbench/Understanding/AtlasReaderView.cs`; `r113-paths.json` records that read-only
inventory. No native candidate was included, no owner inferred, and no gate/allowlist changed.

## IBE B amendment prepared under the desktop hold

The user explicitly clarified that the acknowledged B/E test amendments are executable:
unchanged-control conditions prohibit weaker controls, not those exact amendments. IBE has
twelve prospective calls, separate from ICD10. B adds exactly the Architecture-only,
single-instance `code-atlas` row to `PerspectiveMenuTests`' canonical literal table, taking its
derived expected length from 18 to 19, and adds exactly `Show code Atlas` before `Show graph`
in the Architecture menu literal. Every assertion and every other perspective remains unchanged.
The two exact previous menu-test failures were read from the retained original App TRX before
this edit. No new green runtime result is claimed yet.

The native writer retains the desktop slot. IBE has not run App/shown lifecycle verification
or changed the existing Loaded control. E still requires production-backed count/event-order
observations and retained negative controls for missing cancellation, missing task tracking,
healthy-scope disposal, duplicate one-time initialization, and paint-on-Loaded. A synthetic-only
trace validator or a new path/type exception list would not establish those product claims.
The current `Sta.Pump` lifecycle drivers show windows; replacing the old guard before the new
controls have red-first execution would violate the requested proof sequence.

The additional ICD listener-only test-path seam remains separate from B/E. Source and test
correctives remain uncommitted until their targeted conditions are met. No newer-main join,
native application, relaxed assertion, status-only commit, full cohort or publication occurred.
