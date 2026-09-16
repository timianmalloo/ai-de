---
id: proof-atlas-native-diag-01
title: "Atlas one-case native diagnostic at 7ccef6d8"
type: doc
status: completed
owner: "@timianmalloo"
tags: [atlas, proof, native, diagnostic]
links:
  - { to: proof-atlas-five-gates, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "One granted native case passed with ten successful original UIA queries and normal daemon cleanup; canonical qualification and publication remain separate."
---

# Observed result

**Verified: exactly one selected case passed.** Slot `SLOT-CODEX-NATIVE-DIAG-01`
was used once and released. Grant `req-01M2NMVR0E6YCHSXJ2VK2RRCQQ` and its watcher
copy authorize this diagnostic only. Frozen run HEAD:
`7ccef6d8d394e4a3fc94545587d944b13a0a45f1`; main input:
`bcf4959bc0e0e361736e6a179f05b69fcd0500f8`. The registered worktree is
`C:/Projects/ai-de-integration-atlas-five-gates`, branch `integration/atlas-five-gates`.

Goal: observe the existing native case once on the independently reviewed source.
Done when: same-tree preflight, exact selection, raw outcome, cleanup, pins and release
are observed. Not in scope: another diagnostic, changed selector/assert/wait/cleanup,
product repair, canonical acceptance or publication. Native programme tier T2; width
cap four including Astra Owner and Conductor. The peer-contract response remains T1.

The exact filter was
`FullyQualifiedName=AiDe.App.Tests.AtlasDaemonMainWindowProofTests.MainWindow_RealDaemonReplacement_AcknowledgesHealthyReleaseAndPreservesBorrowedClient`
in `tests/AiDe.App.Tests/AiDe.App.Tests.csproj`, normal build-backed Debug. No retry,
alternate root or extra focus/layout/refresh was introduced. Full argv is in `state.json`.

| Observation | Actual result |
| --- | --- |
| Owned runner | PID 7676; 2026-09-16T18:04:07.970866Z to 18:04:23.103353Z; exit 0 |
| TRX | 1 total, 1 executed, 1 passed, 0 failed; exact requested case |
| Native receipt | Completed=true; FailureCount=0 |
| Original UIA calls | 10/10 found; owned HWND 18286092, process 22016 |
| WPF observations | 10 packets; none truncated or unavailable |
| Replacement | Old host 3/view 4 detached, IsLoaded=false, IsVisible=false; new host 15/view 16 attached, loaded and visible |
| Daemons | PIDs 11760 and 1736; normal exit 0, Forced=false, empty stderr |
| Teardown | Borrowed clients usable after window close; fixtures Exists=false |
| Process readback | No matching repo dotnet/testhost/AiDe processes observed after completion |
| Source and binaries | HEAD/source/build inputs unchanged; all three preflight binary hashes match after execution |

The Conductor inspected all three saved 1280x900 images. The member capture shows the
selected Answer method, highlighted source, enabled Back and source/outline bounds. The
file capture shows src/Widget.cs selected and its source. The replacement capture shows
the replacement workspace, disabled Back, empty source and a source-cleared/loading
message. These images establish those displayed states at capture time. They do not
prove full visual quality, all viewports, or the state at every earlier UIA query.

## Owner disposition and limits

Astra Owner read the raw TRX, native receipt and observation packets and approved
proceeding toward a separately granted canonical slot after evidence assembly. No
second diagnostic is warranted by this result. P1-02's cause remains unknown. The
observer's host State="loading" is derived from StatusText; here it coexists with a
loaded, visible reader and successful queries. It does not establish an active loading
placeholder, a stale provider, or private lease/generation identity.

The receipt explicitly leaves same-live-scope server-idle behavior NOT ESTABLISHED and
a deliberately held late-publication race NOT EXERCISED. Private generation and view
backing-lease relations remain not-observed. Historical failed receipts are preserved.
Complete App/Core recounts, static gates, Release, independent combined review and
foreground acceptance/publication remain required.

## Evidence pins

Raw evidence is retained in this registered worktree; none of these paths is invented.

- `artifacts/atlas-five-gates/native-diagnostic/atlas-native-diag-01-7ccef6d8-20260916T180403Z/state.json` — SHA-256 `7a30854ea0cb834612fb5d852a1cbcf7bcc409b531252711d0abdeb1597cf8f3`.

- `artifacts/atlas-five-gates/native-diagnostic/atlas-native-diag-01-7ccef6d8-20260916T180403Z/test.log` — SHA-256 `6dbd688e8549809fa411d510d5125d3feeb20e73703dc172a654587a84f0fd3e`.

- `artifacts/atlas-five-gates/native-diagnostic/atlas-native-diag-01-7ccef6d8-20260916T180403Z/preflight.log` — SHA-256 `8ad5f85124e7f679b277ded9f58991b083994a2da72b088e51863c97bb6dc73c`.

- `artifacts/atlas-five-gates/native-diagnostic/atlas-native-diag-01-7ccef6d8-20260916T180403Z/test-results/native-diag.trx` — SHA-256 `6c911fa84a7acf3c1f0be75d419aaa8005986b9d36b65e0bf348234f65f9f54c`.

- `artifacts/atlas-real-daemon-window-proof/atlas-native-diag-01-7ccef6d8-20260916T180403Z/receipt.json` — SHA-256 `c182ca87937d28468600bbd6e4059a4b2f91779041bbacf802a239ad6cedcbf1`.

- `artifacts/atlas-five-gates/preflight/atlas-native-diag-01-7ccef6d8-20260916T180403Z.json` — SHA-256 `dd694e9decf48d4ad0e5feedadda3616d7186bf740ca7fb1d09ffbb0ad395c1e`.

- `artifacts/atlas-real-daemon-window-proof/atlas-native-diag-01-7ccef6d8-20260916T180403Z/mainwindow-member-normal-default.png` — SHA-256 `5139c951d3b16a8718b06f3edcc4b8c268fb11ffb314ab8c4e5eec37d4b85d72`.

- `artifacts/atlas-real-daemon-window-proof/atlas-native-diag-01-7ccef6d8-20260916T180403Z/mainwindow-replacement-footer.png` — SHA-256 `97832da333acc0e58380fd7f74145dde78f10eda8a145a2eaef16c8b4a762f94`.

- `artifacts/atlas-real-daemon-window-proof/atlas-native-diag-01-7ccef6d8-20260916T180403Z/mainwindow.png` — SHA-256 `20cc5ef6cd999aa71259763fcb38f019cd32640a46c10c0bfdfd3bc51a9869b0`.

## Execution graph and review assembly

The existing optimize-graph remains active. Source review -> frozen diagnostic ->
observed disposition -> evidence-only joins -> canonical slot -> combined review ->
foreground publication is the dependency chain. Grok's r4 response and D1 listing have
no native dependency. The one-case node terminates after its one observed result; a
pass grants no automatic retry or next slot. E1/E2 source work remains behind the
requested integration priority.

After release, review carrier `c7ef2da56070f0d7bc6348ac62d8a4400a5a491e` and preservation
receipt `12c1ddd08202c9b29688dd1acb425300844fa553` were joined through the official
deliberately unqualified path. Both stopped at the recorded pre-recount barrier
(join exit 4 / check 86). Full-content fingerprints from both parents had zero missing
audit/change rows. The carrier's two historical producer rows were admitted by the
Owner as provenance only. No broader Grok source or proposal was imported.

The first join conflicted only on site/collaboration.html and site/index.html figure
values. Exact data-figure normalization showed both parents' authored text identical;
official regeneration supplied the merged values. A later full graph check found the
older preservation receipt's unregistered `verifies` relation. This checkpoint changes
only that relation to the existing `relates-to`; body, verdict and original pinned
receipt remain intact. Final graph validation is required before this checkpoint commits.

Conservation and official join/regen logs are retained under
`artifacts/atlas-five-gates/final-review-assembly/`. This is recurrence handling under
the existing derived-record/metadata controls, not a coordination framework change.
Root corrected two read probes before using their results: the request-list response
is an object containing `requests`, and the figure tool is `tools/verify-site-figures.py`.
The initial empty filter and missing-file read supplied no acceptance evidence.

Planned versus actual: the exact native execution count stayed one. The two review
joins completed with one generated-figure conflict resolution and one metadata finding.
Earlier tool-budget overruns remain recorded; no retrospective cap compliance or
token-cost measurement is claimed. Current close duration uses its actual audit marker;
native process duration uses the raw start/end above, not the later documentation time.

Completed: the one-case diagnostic, inspected evidence, slot release and review joins.
Remaining: canonical qualification, combined review and GHCP publication. Next: freeze
this evidence checkpoint and request the separately serialized canonical slot. The
worktree and raw evidence remain retained for that handoff. AIDE_CONTRACT_LOG is absent
in this environment; no episode event or sink is fabricated.
