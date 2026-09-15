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
