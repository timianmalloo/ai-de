---
id: security-privacy-rollup-rev102
title: "Security, privacy and release evidence rollup — revision 102"
type: doc
status: assessed-with-gaps
owner: "@timianmalloo"
phase: "assessment"
tags: [security, privacy, release, forensic-review]
links:
  - { to: forensic-review-rev102, rel: documents }
  - { to: privacy-review-ai-native-ide, rel: relates-to }
  - { to: threat-model-ai-native-ide, rel: relates-to }
review-by: 2026-11-17
summary: "Current inspected trust boundaries, privacy data paths and unresolved release/assurance evidence; no exploit or compliance certification is asserted."
---

# Current evidence rollup

This source-level rollup complements the historical threat/privacy designs. It is not a full
penetration test, native package audit or assertion of regulatory compliance. No applicable external
compliance profile was supplied; selecting one remains a proposed product decision.

| Boundary / data | Located control | Residual / next proof |
|---|---|---|
| Untrusted repository project XML | CSharpProjectReader reads project data rather than executing repository targets/generators | Language/extraction fidelity and hostile-input coverage remain bounded |
| Workspace authority and local peer | IpcPipe owner ACL / CurrentUserOnly; per-workspace daemon lock; capability and frame contracts | No fresh adversarial pipe suite executed; FR-002 serialized-size defect reproduced |
| Durable workspace facts | Single writer, epoch/generation checks, immutable fact triggers | FR-001 stale same-revision reuse undermines current-content trust despite immutable storage |
| Embedded knowledge HTML | HtmlSandboxPolicy disables script, host objects, messages, navigation and network | Native WebView2 containment proof not re-run here |
| External ACP/provider lane | Host-owned account/target/permissions and typed send/compile boundaries | Local UI does not imply local inference; FR-007 settings-loaded connectors need actual tool qualification |
| Prompt and attachment retention | Separate envelope/event/draft stores; attachment bytes excluded by draft-store contract | Unsent session-draft persistence primitive is not yet composed; retention and expansion policies need explicit specification |
| Watcher/episode observations | Attributed registrar/query paths, evidence pointers and bounded scoring semantics | Forgeable/advisory records are not semantic proof; FR-006 retry state can lose imported evidence |
| Package/release provenance | Current NuGet query reports no vulnerable packages | FR-008 lock/pin contract mismatch; no full JS/SBOM/signing/rollback rehearsal |
| Proposed PR questions and rich authoring | Fixture-only local state, explicit unknowns, version/scope retention, canonical preview, no enabled Send | Prototype is not authorization enforcement or native runtime qualification |

Sources are the production files named in the forensic report and authoring inventory. Core's former
processing-class MCP gate was deliberately superseded by ADR-0022; this review does not label that
intentional design choice an accidental authorization regression. The shipped stdio tool list is
separate from the internal graph gateway. No external provider, PR integration or telemetry receiver
was connected during this review.
