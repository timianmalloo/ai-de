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
review-by: 2026-12-13
summary: >-
  The compiled public render seam is joined. Membership qualification and Shell lifetime
  findings remain open. Component, compile-only and detached proofs are not proof of the
  real daemon-to-MainWindow journey.
---

# Production adapter checkpoint

This is an incomplete production Proof Pack. Addendum E remains candidate. No primary-main
integration or push is claimed. The private proposal/TheTerrace corpus is not part of this
delivery record.

## Revision and outcome ledger

| Surface | Pin | Observed outcome | Boundary |
|---|---|---|---|
| Joined Conductor | `366167052537c35171f40c6da66c982f8f5d93e5` | Corrected compiled Core render seam joined | No runtime issuer/factory or actual MainWindow integration |
| Core seam author | `8d091e5197db70093909149a8a8e97eb3116c604` | 326/326 independently repeated; 54 reader tests | Compile-only facade fixture is not operational registration |
| Core membership candidate | `d8d83de8034570365ab9c6665f118a4dcb2aa256` | Author 340/343 total; Conductor independently repeated 14/17 membership, the same three failures | Blocked; not joined, wired or approved for production |
| Shell component candidate | `dade5c779012ba9e4c1a8934004d05376a3b4732` | 78/78 independently repeated as 63 native/host plus 15 `SurfaceContentTests` | Four SRE source findings remain; not joined |

## Claim and oracle ledger

| Claim | Concrete evidence and source | Oracle / red observed | Confidence and residual risk |
|---|---|---|---|
| Public render DTOs reject the observed invalid source and continuation cases | `AtlasReaderWireTests.cs`; independent `atlas-core-final-seam/core-seam-final.trx`, 326 passed | Parent invalid-input probes rejected for their intended invariant; author semantic reds retained for zero source length, split-surrogate highlight, nonprogress and producer byte inconsistency; final serializer/decoder empty-source red then green | Verified for the executed cases; not transport or runtime admission |
| Non-friend assemblies can consume the public render ports | Non-friend compilation fixture in `AtlasReaderWireTests.cs` | Public port/DTO compilation succeeds; deliberate hidden Core type use produces CS0122 | Verified accessibility boundary only; static-abstract facade signature fixture is not a production factory |
| Current membership candidate is not ready | `.artifacts/atlas-reader/nq-understanding.trx`, 343 total, 340 passed, three failed | `NativePinsExcludeRootIndexAndLooseRefReplacementAndReleaseOnDispose`; clone/unpacked and nested/unpacked required-form cases fail `native-namespace-notification`, invocation count six | Verified failure. Triggering native pin/event/result was not recorded, so product versus fixture/notification cause is Flagged |
| Some membership defenses execute | Same final TRX: hostile-fsmonitor and packed-ref namespace ABA cases pass | Unguarded hostile fixture must create its marker; guarded capture must not. Packed-ref insertion/deletion must invalidate even when final bytes agree | Verified named cases only; does not clear NQ1/NQ2 overall |
| Shell component behavior passes its current tests | `AtlasSharedHostAdmissionTests.cs`, native tests and existing `SurfaceContentTests`; independent receipts below | Author two semantic-red and two mutation-red cases retained in worker artifacts | Verified tested component behavior; missing fault cases and real-window integration remain |

Source paths for Core above are under `src/AiDe.Core/Understanding` and
`tests/AiDe.Core.Tests/Understanding`. Shell tests are under
`tests/AiDe.App.Tests/Workbench/Understanding`; factory tests retain their existing path.

## Shell lifetime review: findings awaiting executed counterexamples

SRE review of frozen `dade5c77` identifies the following source mechanisms. These are
source-reviewed findings, not yet independently executed causal proofs. No implementation
or green test below is invented.

| Finding | Source mechanism | Required falsifying test |
|---|---|---|
| A failed transition can poison later attach and disposal | `AtlasWorkspaceOwner.ReplaceAsync` awaits the previous transition before containment; factory and lease/reader disposal may fault | Throw once from each boundary, then successfully attach a valid reader and await final cleanup |
| One clear callback can prevent the other views and cleanup from running | `InvalidateViews` invokes callbacks without per-callback containment; dispatcher invalidation is tracked without observed fault handling | First of two callbacks throws; second still clears; attach/dispose completes and a stable diagnostic is emitted |
| Final lifetime primitives are not disposed | Disposed-path early return precedes lifetime CTS disposal; admission semaphore has no disposal path | Await final drain and disposal, observe both primitives released, and repeat `DisposeAsync` without a second release |
| Post-assignment load failure leaves stale host state | `AtlasLoadingHost` assigns `ReaderView` before awaited load; error path changes text without clearing activation/registration/view | Fail load after assignment, observe unavailable with null view and released host-owned state, then activate successfully |

The first two are SRE advisory blockers escalated to the Owner. The real MainWindow/Core
handoff is independently missing, even if all four component findings are corrected.

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
| Architecture opener/native component | Component candidate only |
| Actual MainWindow attach/replacement/final close | Not implemented; requires committed Core factory/view-model handoff |
| Real product file -> member -> source -> Back and revocation | Not proved; detached real-source proof does not substitute |

## Evidence locations and durability limits

Conductor session evidence is retained under the session-state `files` directory:

- `atlas-core-final-seam/core-seam-final.trx`
- `atlas-shell-independent/shell.trx`
- `atlas-shell-independent/factory-existing.trx`
- `atlas-membership-independent/nq-independent.trx` (17 executed, 14 passed, three failed;
  test command exit 1; the source HEAD remained `d8d83de`)

Membership author receipts are retained in the Core execution worktree:
`.artifacts/atlas-reader/nq-first.trx`, `nq-second.trx`, and `nq-understanding.trx`.
The first run had read-only Git-object cleanup failures and a real USN-only ABA miss.
The second had eight snapshot-currentness failures. They are historical failures, not
the final three-error result and not discarded to improve the record.

Shell author receipts remain in its worktree under `artifacts/atlas-shell`:
`atlas-shell-red.trx`, `atlas-shell-mutants.trx`, and `atlas-shell-final.trx`.
These machine-local raw receipts are retained but are not claimed to be committed.
The committed summary names their exact checks and limitations. Both source worktrees
must be kept while they hold unjoined code and evidence.

## Operator questions

The qualification helper emits `atlas.membership.captures` and
`atlas.membership.duration`; its cancellation test observes an emitted capture.
The causing pin and native notification result are currently not recorded, preventing a
causal answer for the three failures. Diagnostic work must close that specific gap without
publishing source paths or changing a failure into complete empty membership.

Production questions remain open: which admission failed, which scope was revoked, how long
an operation took, whether a dirty transport was replaced, and whether final cleanup drained
all operations. Synthetic measurements and component logs cannot answer those questions for
the still-unimplemented production path.
