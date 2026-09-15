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

### Current-main preparatory join

Official conductor-join incorporated 901320c4 at merge4473f260, with record commit c2f94cb8. Two site wrapper conflicts contained only data-figure counts; the merged audit/register data was regenerated through tools/regenerate-derived.py. Its actual checks reported current derived views and 14 verified site figures. The official preparatory gate run then stopped at step8: 7 of38 gates failed. These are the original five plus verify-terminal-host-exit-paths and verify-test-run, which found no TRX in the new tree. No recount was requested for this docs-only preparatory merge. Release/push were disabled for preparation and remain mandatory/serialized for final qualification.

Audit merge conservation was independently read back against both parents using the registered coord-core entry_fingerprint semantics (exclude only id and renumbered_from). Before:861 rows,828 distinct payloads. Main parent:713 rows/payloads. Merged:829 rows/payloads; zero payloads missing from either parent. The33 removed full-JSON rows were duplicate ID aliases. Owner additionally checked that those removed IDs have no retained audit or tracked docs/.agents references. This is documented normalization, not lost work; do not restore or remint duplicates. No ownership or framework change was made.

The join tool emitted al-01M2KKHMZ4QQR23YW7S7M17CCX before its gates, with acceptance_met=true despite subsequent step8 failure. A truthful corrective entry is required; no final gate clearance can be inferred from that intermediate entry. The repository-configured commit trailer attributes the join record to Claude; actual executing actor was Codex. Subsequent Codex join calls must provide an accurate trailer rather than inherit that stale identity.

Verified: five blockers reproduced; isolated branch created; user transfer explicit. Inferred: independent repair tracks can reduce the critical path after semantics settle. Flagged: no fixes, independent clearance, Release or publication yet. Main last observed 901320c457cdaaafe3a1bafc9fd6a969b91253aa. E1/E2 is not bundled.
