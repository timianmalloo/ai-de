---
id: proof-code-atlas-real-daemon-mainwindow
title: "Code Atlas E0 - real daemon, native window and readable default"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [code-atlas, proof, e0, native, daemon, readability]
links:
  - { to: spec-addendum-e-code-atlas, rel: implements }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
  - { to: proof-code-atlas-production-adapters, rel: relates-to }
  - { to: note-atlas-live-reader-horizon, rel: relates-to }
review-by: 2026-12-14
summary: >-
  Owner 63 accepts the bounded E0 real-daemon/native-window reading horizon for the
  observed owned fixture and viewport. Source, member, Back, healthy release,
  replacement and readable normal Center pixels were observed; broader coverage and
  the synthetic workspace-footer discrepancy remain explicitly outside acceptance.
---

# Bounded E0 acceptance, not programme completion

Owner turn 63 accepts the observed real daemon-backed file/member/source/Back journey,
healthy acknowledged release, replacement and normal-default Center readability.
Conductor inspected the actual member-selected PNG, raw lifecycle/geometry receipt,
proof source and an independent parent replay.

This record does **not** register Addendum E as normative, authorize main integration or
push, or complete the wider Code Atlas programme. E1 static views, E2 domain/ER/layer/Azure,
E3 correspondence and E4 AI remain separate phases.

## Accepted scope and revisions

| Item | Observed identity |
|---|---|
| Product source in the proof tree | `f89822cb9d7a0388ba84a5ad650aee68f7e420db` |
| Initial independent proof commit | `2ab95e2b61818d828d5c36d3ac68b72632b906c4` |
| Normal-default readable proof commit | `6bb7646d7205fc60be2a9a0888e9c43ad106ff81` |
| Reviewed Center/wrapping implementation | `6591b5e94a6574bcb43035567e5743e12342f5f0`, joined as `dd84702b` |
| Authored proof surface | `tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs` only |
| Independent input | Two separately owned synthetic Git repositories; `src/Widget.cs`, 84 UTF-8 bytes |
| Window | Actual MainWindow, 1280 x 900; normal Architecture/Code Atlas opener; no maximize |
| Source SHA-256 | `0B58B4CA02428A8671250B4C59019CC5D35E236111CE8BF9AB986783DAD48835` |
| Method declaration extent | UTF-16 start 55, length 26 |
| Identifier highlight | UTF-16 start 66, length 6; `Answer` |

Declaration extent and identifier highlight are different quantities. An earlier proof
assumption equated them; the final oracle checks each against the actual source bytes.

## Claim-to-evidence ledger

| Claim | Evidence and oracle | Confidence / boundary |
|---|---|---|
| Actual daemon/client/reader path, not fake Atlas data | The test launches the built daemon executable, connects actual `WorkspaceClient`, uses `CreateAtlasReader`, and wraps calls only to observe/forward unchanged results | Verified by source and executed run; owned synthetic repositories only |
| Returned and bound source match the owned file | File/member/Back page hashes and the reader's source hash equal the actual 84-byte file hash; declaration/highlight bounds are checked against that file | Verified for this fixture; not every language, file size or project profile |
| Default source and member label are visibly readable | Actual clipped source/glyph geometry plus parent inspection of the member-selected capture below | Verified at 1280 x 900; component geometry separately covers 1440 x 900 |
| Back preserves original binding and selection | Final receipt records `SameBinding=true`, `SameSelection=true`; assertions compare file/declaration and binding tokens | Verified native-Q path; no new authority granted by a receipt |
| Workspace replacement drains through healthy release | Both actual leases are healthy before disposal; forwarding wrappers await normal return from the real lease `DisposeAsync`; the established healthy path awaits `atlas.release` and server scope stop | Verified bounded acknowledgment path, not generic/terminal reader disposal |
| Replaced source does not remain visible | Old source clears before replacement await and stays clear after new selection and dispatcher idle | Verified tested replacement; deliberately held late-publication race not exercised by this window run |
| Old scope/foreign receipt is refused | Actual old/replacement client APIs throw their typed guards; receipt identifies the rejection layer | Verified **local client guard**, not a new server-rejection proof |
| Borrowed general clients survive window close | Two real query round trips succeed after close; proof owner subsequently disposes those clients | Verified query transport; a distinct command method was not invoked |
| Owned lifetimes end | Window closes after awaited ownership cleanup; owned dispatcher body completes; both daemons exit normally and fixtures are deleted | Verified observed paths; no stalled-kernel/general fault guarantee |
| Accessibility observation is scoped to the owned window | MTA UIA client starts from the owned HWND, checks process identity and named controls; no DesktopRoot/private-cache manipulation | Verified observation, not full WCAG or screen-reader certification |

The original clipped-default captures and source-property/UIA-only green were not accepted
as readable-pixel proof. Placement then went red for Right instead of Center and clipped
source; Center went red for the complete label; finite-width wrapping and actual glyph-run
oracles supplied the corresponding green evidence. The older wrong legacy stack-maximize
assertion and forced-cleanup outcomes remain retained as failed proof history.

## Actual readable geometry

All values below are WPF device-independent pixels from the normal-default real-window run.

| Quantity | Observation |
|---|---|
| Client bounds | 1265.333 x 862.667 |
| Code Atlas | Center, width 654.507; intended surface and logical focused stack match |
| Source text viewport | x 713.647, y 128.540, width 256.940, height 572.343 |
| Longest tested source line | width 228.500 |
| Highlight | x 827.897, y 173.860, width 45.700, height 15.107 |
| Selected label viewport | width 176 |
| Rendered wrapped glyph runs | Three; all finite, positive and inside clipping |
| Full text/accessibility | Rendered non-whitespace character hash matches full label; full accessible name preserved |
| Typography | Source and label 13 DIP; no trimming, font reduction or proof-only resizing |

![Actual normal-default Code Atlas source and selected member](assets/code-atlas-mainwindow/normal-default-member.png)

Approved capture SHA-256:
`0A55507A2F367CAF0F19658E4DDC0B51E3D3082E1A9C31C6ACDCD25ED19295F2`.
Conductor read the original image, copied only this approved owned-window file, and
verified the destination hash is identical.

## Execution receipts and binary pins

The independent author run is `owner62-trx1/owner62-run1.trx`: one passed, zero failed.
Its raw `owner62-run1/receipt.json` records normal daemon exits 57944 and 50400, both zero,
and deleted fixtures. The proof author used 47 of 49 allocated calls at that checkpoint.

Conductor separately rebuilt and executed the proof under `ATLAS_PROOF_RUN=parent-owner62-verification`:
one passed, zero failed, with raw normal exits 48360 and 53416, both zero, healthy release
returns and deleted fixtures. Its independently inspected member image also shows the full
source, wrapped label and highlight. This parent replay ran in the proof checkout; the
current-Conductor-tree replay after joining is a separate closing check.

| Author-run binary | SHA-256 |
|---|---|
| Daemon executable | `FAB7906DA6B700BE53977CFC5FD6B86E4470E16A407EBC11B63ACA59E153E974` |
| Daemon assembly | `D9FFB9F3BA2007FBD683E6178817D3CFA2BBA0A08B8533764F23B1C5A6B064B8` |
| Core assembly | `A70252A4B3BA0B0AE9F49B645047565DAC4E73A4C7B26484FAC6C0DEB92BBED1` |
| App assembly | `61808FEF99DF3536A5493C23870C49EE38277FD2517EF70311059830BE09801D` |
| Proof source | `E91ED164228218A85C2835BDFB24BAA1DEF238DB1EED70666E177703E5ABACDA` |

Machine-local raw evidence is retained under the independent proof worktree's
`artifacts/atlas-real-daemon-window-proof/`, including all earlier failed runs.
Parent TRX is retained in the session files at `atlas-real-window-parent/parent-real-window.trx`.
The committed record/capture above do not pretend those raw machine-local receipts are committed.

## Explicit exclusions and follow-up

- **Footer not accepted:** the capture says "No workspace open" for a directly constructed
  synthetic VM despite the real connection/source. Owner 63 requires legitimate initialization/
  refresh and current-tree binary resolution to be checked in a four-call proof follow-up.
  No convenient status-string assignment or product-status fix is authorized.
- **Public same-live-scope idle barrier not established.** In-process request-correlated
  drain/Git-write evidence is separate from this out-of-process release acknowledgment.
- **Deliberately held late-publication race not exercised here.** Its dedicated Core
  counterexamples and repairs remain separate evidence.
- **Distinct command method not invoked.** Borrowed query transport usability is observed.
- **No broad workspace/viewport/accessibility claim.** This is the recorded fixture/window,
  not enterprise-scale class diagrams, every language or a general accessibility certification.
- **No normal profile-writing startup.** The proof uses the legitimate internal constructor
  with owned startup/configuration/resources and actual remote clients.

GATE E0 bounded reading/default readability - Owner turn 63 - ACCEPTED with the above
scope and exclusions. Proof-only joins, current-tree replay and durable capture are permitted;
main/push, normative Addendum E and programme completion are not.
