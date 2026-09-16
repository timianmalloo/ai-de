---
id: proof-atlas-five-gates
title: "Atlas five-gate repair evidence"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, proof]
links:
  - { to: plan-atlas-five-gates, rel: depends-on }
review-by: 2026-12-15
summary: "Observed frozen failures, repair controls and qualification receipts; acceptance remains open."
---

# Initial red observation

Base dd9b338f4670f9ce64d0c38a5e7c196664250d9a. Each command below was executed independently with Python in the new conductor tree; each returned exit 1. Results were inspected before product edits.

| Command | Actual failure |
|---|---|
| python tools/verify-audit-capture.py | 18 historical skill rows missing signals or proof artifacts |
| python tools/verify-bounds-are-enforced.py | MaxIndexBytes declaration not recognized as enforced |
| python tools/verify-containment-comparisons.py | OrdinalIgnoreCase at AtlasGitMembership.cs:434 and WorkspaceCore.cs:593 |
| python tools/verify-harness-diagnostics.py | AtlasSharedHostAdmissionTests wraps captured failure without recognized assertion guard; AtlasDaemonMainWindowProofTests STA path unclassified |
| python tools/verify-surface-ownership.py | Historical non-Path table rows at session-contracts.md:136 and :412 rejected as malformed ownership rows |

Historical raw integrated results remain read-only under C:/Projects/ai-de-atlas-main-integration/.artifacts/atlas-main-integration/IJ/atlas-uwq20-1789505979044456000. This record does not promote them to current qualification. Corrective audit al-01M2KFHN7Q0WNJNJP3MS7SGCWJ records blocked acceptance and supersedes al-01M2KEJ2V2PREZ2P1H4JYT3Q83; preserve both.

## Current verdict

### Canonical qualification failure and preserved evidence

SLOT-CODEX-P1-01 runner8556 started2026-09-16T00:17:58.474224Z on HEAD247e6b4e,
sourcefe95be86/basebcf4959b. First completed App TRX contains1165 result elements:
1165 executed,1163 passed,2 failed,0 skipped. Failures:

- `AtlasDaemonMainWindowProofTests.MainWindow_RealDaemonReplacement_AcknowledgesHealthyReleaseAndPreservesBorrowedClient`:
  required `ATLAS_PROOF_RUN` absent. Execution omission, not a product defect. Actual test source
  is tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs.
- `SolutionTreeProbeTests.ProbeAtlas_NoUnderstandingNamespaceAndNoUnderstandingFolder`:
  `src/AiDe.Core/Understanding must not exist for D-0`. This global premise conflicts with
  admitted Atlas coexistence; Owner interpretation and bounded repair are in the coordination record.

The recorded owned process tree was deliberately stopped after reading the completed failed TRX;
runner start identity was checked before termination. End00:23:18.413129Z, exit4294967295 is
termination, not a native gate result. Post-stop process census matching this integration tree was
empty. Explicit END/RELEASE requests: req-01M2KSJ3CYXTF9KYRM78JEC5KE (watcher),
req-01M2KSJ3EPH4N6P5DHSWYD43Y6 (foreground), req-01M2KSJ3GGQ6YXD58J6XSAQVDX (Grok).
No Core TRX was available in the observed results directory; no Core acceptance, final gates,
Release build or main publication follows from this run. No step5 accepted-audit was emitted.

Preserved copies under `artifacts/atlas-five-gates/failed-slot-p1-01/` before any rerun:

| File | Bytes | SHA256 |
|---|---:|---|
| AiDe.App.Tests.trx |1819879|2a1ad107c578f924046a8aa448e6da5ed45a6827633f92002ff3caf570ed4e71|
| canonical-qualification.log |1617|5cd4b23ff5c4b5ff56cde186e6d60eaef9fb150d5e4f974da2adce03e9e9dbcf|
| observed-trx-results.json |1283|137bc3648153306a3d748b1f2a46f1844392ce2f99a30499b5c08a2d433d6f9d|
| qualification-state.json |1295|2c04743351ca6efce01b31b33bb563ccfba2a7bf2d44518767c7d3d5e850e5f0|

Flagged scheduling evidence: Grok reported PID27416 between00:18:35.3395464Z and
00:18:56.1441733Z, overlapping the allocation. Exact command/GUI use was requested through
req-01M2KSD1MJBSTMM2WRKEVMDDHN and req-01M2KSD1PCSM3AT67NZXEC5J6S. No causal claim is made.
The observed two failures already name a setup omission and an incompatible global assertion.

Class → sweep → derive → prevent: reuse DC-118 for a guard whose scope grew beyond its
integration contract, and DC-207 for process-local setup omitted at a new shell boundary.
Sweep covers D0 producer/client/dispatch/UI roots and native proof label/build/root requirements.
The replacement must reject real dependency mutations and missing coverage; execution preflight
must refuse missing/reused setup before launching the expensive run. Controls are pending until
their actual results are recorded; prose alone is not closure.

### Current-main assembly observed, 2026-09-16

**Later native execution-control review:** independent review blocked the initial preflight's
missing post-build dirty-input check. The corrected script repeats the identical input check
before/after build and records both. Three valid cases and fourteen refusals passed, independently
rerun by the reviewer. Preflight SHA256
`a896d129bc167f5792b9ec6927315c365a0d29709154c0109e07fb08b1a9bcfc` is CLEAR.

The external execution wrapper,
`C:/Users/malla/AppData/Local/Temp/atlas-five-gates-assembly/run_qualification_v2.py`, SHA256
`dcdb84dc39abd8d2c5647a887e3e0365989ab08f167a7a94c3af0c138d96f74f`, received independent
Test/SRE CLEAR after adding the reviewed-preflight pin and exact required main base. One explicit
environment reaches preflight and the unchanged canonical join. The wrapper validates the
preflight receipt and binary hashes, preserves new TRXs, stops its owned live process tree on a
failed/skipped/empty suite, records PID/start/end and renews/releases exact short leases. No
review executes a build or grants a slot. Fresh slot and reviewed combined HEAD remain mandatory.

Subsequent record-only correction: `docs/proof/atlas-core-gate-repair.md` used unsupported
frontmatter `type: proof`, the single finding from the actual graph inventory. Owner approved
`type: doc`; evidence body and source/test blobs are unchanged. Its document blob changes and
must not be included in a later claim that all14 original blobs still match byte-for-byte.

Initial native setup prevention: `docs/proof/records/atlas-five-gates/native-preflight.py --self-test`
observed one valid fixture and eight refused cases: missing, empty, absolute-path and non-ASCII
labels; missing session and wrong agent; existing native receipt; missing binary. No GUI/build
was run by these fixtures. The script validates registered identity/root and reviewed HEAD,
builds the same-tree Debug daemon explicitly, writes an independent receipt outside the native
proof directory and records hashes. Independent script review and real build remain pending.

Assembled source checkpoint `fe95be86b08c34f90dd8236819e4216dd8316e9b` on
`integration/atlas-five-gates`, C:/Projects/ai-de-integration-atlas-five-gates, contains all eight
required exact ancestors: conductor b0625686, main bcf4959b, author23151302/47f5f54c/a72eb357/
301bc67a and reviews622ed908/9bd65703. Read-only manifest verification compared14 source/test/proof
Git blobs byte-for-byte with their reviewed revisions; all matched. Actual manifest is retained at
C:/Users/malla/AppData/Local/Temp/atlas-five-gates-assembly/verified-manifest.json. Canonical
join.json still hashes16a55a15c9d56ae365c6a2c9d5729f382a678d00aa9786be23269c508b6b085c.

The official current-main join initially stopped at five conflicts. The user expressly directed
“the conflicts are yours to resolve”, logged as al-01M2KRN5Z67AZVG212ZHDD3GWR. Resolution22dc5b12:

- PerspectiveMenuTests keeps both Show code Atlas and Show solution tree in the actual merged
  SurfaceContentFactory.Kinds order. Neither product entry is removed.
- expected-test-counts retains the unchanged higher Atlas floor1134/3161/2817/344; main's
  1051/2746/2571/175 remains in its parent. No new combined count is guessed; canonical full
  recount must regenerate it and inspect every outcome.
- Full parent texts of site/collaboration.html, site/index.html and site/model.html were proved
  identical outside data-figure values. Official regeneration retained prose and rebuilt figures.

Official staged merges then produced d12bd4d9 (audit), e815d3a9 (ownership), eaa5e3ea (Core),
cbb7c5a3 (harness),1c7206ef (review),fe95be86 (harness review). Owner and independent Test Architect,
Simplifier, SRE and Orchestrator cleared the exact temporary unconditional staging barrier.
Every stage stopped at step4/check exit86 with ATLAS-ASSEMBLY-INCOMPLETE, before recount or
accepted-audit emission. Six logs are retained at the external assembly directory as
stage-{audit,ownership,core,harness,review,harness-review}.log. A Windows stdout encoding failure
interrupted the audit-stage wrapper display after the official run; the saved log was inspected
and proved the intended barrier. Explicit PYTHONIOENCODING=utf-8 corrected subsequent display;
the already-joined audit commit was not replayed. The defect-register check's CRLF-only stat change
had an empty Git content diff and was normalized by staging, not reset/discarded.

Final register sweep used the existing coord-core.entry_fingerprint function against ALL eight
input pins: merged904 audit rows and172 change rows, ZERO missing semantic payloads from any pin.
This preserves both original false-acceptance rows and their truthful superseders. Initial main
merge separately conserved836/761 audit input rows and165/155 change input rows into885/172.

| Focused gate executed on fe95be86 source | Actual result |
|---|---|
| verify-audit-capture.py | PASS318 checked,431 frozen |
| verify-bounds-are-enforced.py | PASS30 constants; reviewed indirect implementation unchanged |
| verify-containment-comparisons.py | PASSall checked comparisons use PathComparison.ForThisFileSystem |
| verify-harness-diagnostics.py | PASS4 STA-bearing files:2 wrapping,1 original TCS,1 exception subject |
| verify-surface-ownership.py | PASS19 discovered,19 assigned,0 awaiting joint decision |

These results clear the original five STATIC failures on the assembled tree; they do not qualify
runtime integration. No full .NET/App/native test or Release build has started in this tree.
Watcher allocation requests req-01M2KR9M0180MSWTCYZ2DW77SM and req-01M2KRV3S1EPSNG3V2VGN4V83B
await an actual checked slot. Final canonical --continue --no-push must execute the unchanged
full/portable/nonportable/no-run/gates/Release union. Independent combined qualification review
and foreground acceptance/publication remain outstanding.

Independent Test Architect/Simplifier/SRE/Orchestrator prequalification review subsequently
returned CLEAR to qualification, explicitly not combined acceptance. It read the assembled
factory/menu union, unchanged D0 dedicated files, reviewed component blobs, six barrier logs and
canonical contract. Root's eight-ancestor and zero-loss conservation checks satisfy its two
remaining readback conditions; the actual watcher slot and runtime evidence remain pending.

The conductor's one prompt-only supplement was preserved in clean branch commitbe1c6711, then its
audit payload was consumed through the supported coord merge-register driver with baseb0625686.
Conservation checked against assembledfe95be86 andbe1c6711:905 audit/172 change rows, zero missing
payloads. This is artifact-register consumption, not a claim thatbe1c6711 is a Git ancestor. The
required eight assembly ancestors remain unchanged, and this supplement changes no source/tests.

Records milestone78d61c04 and shared manifest req-01M2KPD0GV75KDESDM00F6RHGK name all reviewed
components. Official regeneration observed833 audit+165 change entries,555 indexed artifacts,
14 correct site figures,225 valid defect classes,4 matching derived views and no conflict markers.
Its commit hook reported AGENT_SESSION unset: the shell-local identity did not carry from earlier
processes. Therefore no identity-enforced precommit is claimed for78d61c04. Subsequent correction
explicitly sets both identity variables in the same process as the existing staged-path check and
commit. It cannot retroactively establish the original boundary. Current-main qualification is
still pending. The direct producer proposal f073e2a0/blob661c92a3 was received and opened;
its consumer acceptance review is separate from the component repairs and remains pending.

### Linux containment supplement observed by Conductor

Frozen source a4d25b9e was exported with `git archive` (src,tests, Directory.Build.props/rsp,
Directory.Packages.props, global.json), never Windows bin/obj, to
`/home/timmall/.cache/codex-atlas-core-linux-a4d25b9e` in Ubuntu24.04. Existing SDK10.0.303
executed the exact filter `FullyQualifiedName~DecodeMembership_NestedSource|FullyQualifiedName~AtlasContentContainmentTests`.
The first Windows-to-WSL inline invocation failed argument forwarding; it is not accepted test
evidence. A file-based bash invocation with quoted filter/logger and `set -euo pipefail` succeeded.
Actual TRX counters: total11,executed11,passed11,failed0,notExecuted0. All eleven case names/outcomes
were inspected, including `SRC/a.cs`, `src-other/a.cs`, nested relative paths and traversal/rooted
refusals. TRX start15:58:12.2409561-07:00, finish15:58:13.3538271-07:00 (1.112871seconds).
Raw Linux file `.artifacts/portable-containment.trx`; Windows evidence copy
`C:/Users/malla/AppData/Local/Temp/atlas-core-portable-containment.trx`, SHA256
`AA419AEC9436932C8FE05A881EAE0B8FDE29B61E4054BA059D46ABEBE444C960`.
This supplements the Windows55-case proof, not combined-main or native qualification.

### Conductor readback and independent review

G1 commit23151302: actual capture gate270checked/431frozen, audit integrity850audit+165change,
zero duplicate IDs; Conductor reran both and inspected the corrective mapping/payload evidence.
G5 commita72eb357: Conductor reran normal18/18/0 and self-test (eight retained mutants), then
independently enumerated all18paths via os.walk and matched the gate's recursive census.
G2/G3 initial source74f2ec0e/proofa4d25b9e: reviewer independently inspected raw TRXs13/13 baseline,
55/55 final, and two source mutants each1pass/1fail. Conductor inspected the source and proof.
Review c7bb11a5 cleared G1/G3/G5 and BLOCKED G2 decoy placement. Correction7a439285 was also
BLOCKED: an uncalled local-function decoy could mask the renamed unbounded live call. Owner chose
the smaller, fail-closed reviewed-source fingerprint instead of a C# analyzer. Final47f5f54c pins
AtlasGitMembership.cs SHA256 `5993639d5838ccc9a4319f428dbb8dbcc8c7eab71788ae7bfff435f481627dd1`
(60,819 bytes after CRLF-to-LF normalization only). Missing, unreadable or changed source fails;
even comment changes require behavioral requalification and independent review before manual pin
update. Its claim is only “reviewed indirect implementation unchanged.” Conductor reran17/17
self-tests and the normal30-bound scan. Independent receipt622ed908 clears the final component.
Production/tests are unchanged from74f2ec0e, including the Linux supplement's a4d25b9e source.

G4 final301bc67a: Conductor inspected the actual four headless StageDiagnostics_ test bodies and
raw TRXs: red1pass/3fail, green4/4, final4/4. Five compiled mutants failed as expected (drop guard2,
replace instance2, stage-cap coupling1, flatten1, stack reset1 failed cases). The helper retains
original exception instances, first capture ordinal/category and full diagnostics outside capped
stages; the first retained DIRECT XunitException is rethrown by EDI with its identity/stack. This
does not establish body-primary attribution or flatten nested aggregates. Initial file-wide EDI
recognition was independently BLOCKED by an unrelated-method guard. Final scoped recognition has
11 TCS and9 EDI fixtures; normal scan classifies4 STA files (2 wrapping,1 TCS,1 exception subject).
Conductor reran both final scanner commands successfully. Independent9bd65703 clears the component.
Raw TRXs/logs remain at C:/Users/malla/AppData/Local/Temp/atlas-harness-diagnostics/.

All five components now have independent clearance. Advertised main was independently verified as
bcf4959bc0e0e361736e6a179f05b69fcd0500f8; it is NOT an ancestor of root9301207e. It includes D0
product changes. Closer req-01M2KMGS05ZJ5WHGV76KMYVE1K and watcher req-01M2KN65G9ZJATZ9Z6MM6DHKVW
require official current-main reconciliation and cross-surface regression BEFORE combined freeze.
Request req-01M2KNK3JNSKNTS80YHQAXYPGX asks for the actual Codex full/shown slot and outside-P1
conflict owner, or GHCP execution of that reconciliation. No answer, slot or accepted handoff is
inferred. Component milestone req-01M2KP0WCWA1N7WF3EQE7AP4VE is notice sent only.

### Exact component handoff manifest

These pins are reviewed components, NOT a combined candidate. Use the official join tools and
registered append-only merge mechanisms; regenerate after audit writes. No source/docs-only join.

| Lane | Branch / final commit | Exact changed paths | Independent receipt |
|---|---|---|---|
| G1 | fix/atlas-audit-capture / 231513023bc98fd487409b067cd534d93f35d02d | docs/audit/audit-log.jsonl; docs/proof/atlas-audit-capture-repair.md | 622ed908088c9cdb46991cd4734fbdcdfece72ce |
| G2/G3 | fix/atlas-core-gates / 47f5f54cd8b1dbd7b65cca344f0585b712139422 | tools/verify-bounds-are-enforced.py; src/AiDe.Core/Understanding/AtlasGitMembership.cs; src/AiDe.Core/WorkspaceCore.cs; tests/AiDe.Core.Tests/Understanding/AtlasGitMembershipTests.cs; tests/AiDe.Core.Tests/Understanding/AtlasProductionAdmissionTests.cs; docs/proof/atlas-core-gate-repair.md | 622ed908088c9cdb46991cd4734fbdcdfece72ce |
| G5 | fix/atlas-ownership-tables / a72eb35713a4e8c00b0e928cc488bc483162b797 | tools/verify-surface-ownership.py; docs/proof/atlas-ownership-table-repair.md | 622ed908088c9cdb46991cd4734fbdcdfece72ce |
| G4 | fix/atlas-harness-diagnostics / 301bc67a61228a9c944d6ba778db98d241b75087 | tools/verify-harness-diagnostics.py; tests/AiDe.App.Tests/Workbench/Understanding/AtlasSharedHostAdmissionTests.cs; docs/proof/atlas-harness-gate-repair.md | 9bd657031507c25690e0f0f4bbdd18c820a61ed4 |

Core history is74f2ec0e → a4d25b9e → rejected7a439285 → final47f5f54c. Harness history is
f43ff83c → final301bc67a. Core/G5 base4473f260; G1/G4 base9301207e. Review branches
review/atlas-five-gates and review/atlas-harness-gate carry docs/proof/atlas-five-gates-review.md
and docs/proof/atlas-harness-gate-review.md respectively. Author worktrees are retained at
C:/Projects/ai-de-fix-atlas-{audit-capture,core-gates,ownership-tables,harness-diagnostics}.
The audit author's generated audit-data.js remains uncommitted by design; regenerate at join.

### Current-main preparatory join

Official conductor-join incorporated 901320c4 at merge4473f260, with record commit c2f94cb8. Two site wrapper conflicts contained only data-figure counts; the merged audit/register data was regenerated through tools/regenerate-derived.py. Its actual checks reported current derived views and 14 verified site figures. The official preparatory gate run then stopped at step8: 7 of38 gates failed. These are the original five plus verify-terminal-host-exit-paths and verify-test-run, which found no TRX in the new tree. No recount was requested for this docs-only preparatory merge. Release/push were disabled for preparation and remain mandatory/serialized for final qualification.

Audit merge conservation was independently read back against both parents using the registered coord-core entry_fingerprint semantics (exclude only id and renumbered_from). Before:861 rows,828 distinct payloads. Main parent:713 rows/payloads. Merged:829 rows/payloads; zero payloads missing from either parent. The33 removed full-JSON rows were duplicate ID aliases. Owner additionally checked that those removed IDs have no retained audit or tracked docs/.agents references. This is documented normalization, not lost work; do not restore or remint duplicates. No ownership or framework change was made.

The join tool emitted al-01M2KKHMZ4QQR23YW7S7M17CCX before its gates, with acceptance_met=true despite subsequent step8 failure. Corrective entry al-01M2KKSY1HJHP4NBCP8K9VDJEK retains that original and records blocked/acceptance=false. No final gate clearance can be inferred from the intermediate entry. The repository-configured commit trailer attributes the join record to Claude; actual executing actor was Codex. Subsequent Codex join calls must provide an accurate trailer rather than inherit that stale identity.

Verified: five blockers reproduced and repaired on isolated component branches; independent clearance and inspected red/green/mutant evidence exist. Flagged: no combined current-main qualification, Release, acknowledged integration handoff or publication. E1/E2 is not bundled. Its direct handshake is proposal-only, recorded in docs/coordination/atlas-five-gates.md.
