---
id: release-plan-ai-native-ide
title: "AI-DE release, compatibility, and rollback plan"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "0"
tags: [release, rollback, migration, compatibility, supply-chain]
links:
  - { to: architecture, rel: documents }
  - { to: conceptual-model-ai-native-ide, rel: depends-on }
  - { to: threat-model-ai-native-ide, rel: depends-on }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  Defines progressive desktop release rings, default-off migration-dependent features, version
  compatibility, Windows/DPAPI parity, rollback handling, and Phase-1 supply-chain/CI gates for
  AI-DE.
---

# AI-DE release, compatibility, and rollback plan

## Release rings and feature gates

| Ring | Audience | Migration-dependent capability | Promotion criteria | Rollback trigger |
|---|---|---|---|---|
| Internal | ≤5 maintainers on copied fixture and non-production workspaces | Default off; explicitly enabled after migration preflight | All Phase-1 Proof Pack cases, upgrade/rollback runbook, and security/privacy fixtures pass. | Any integrity, restore, unauthorized IPC/MCP, or data-loss failure. |
| Opt-in canary | ≤20 workspace owners who explicitly enroll | Default off until workspace backup, migration, and health preflight pass | Seven days with zero unresolved persisted health incidents, upgrade failures, restore failures, or security/privacy gate failures. | Same as internal plus >10% p95/SLO regression. |
| Percentage | 10% → 25% → 50% of eligible opted-in workspaces, minimum 48h per step | Enabled only for compatible schema/binary pair | Canary criteria plus production-parity evidence and no threshold breach over the prior step. | Error/health threshold breach; pause new migrations. |
| Broad | All eligible workspaces | Default on only after compatibility window | Product owner records promotion sign-off and rollback rehearsal. | Revert to prior binary and read-compatible schema. |

Every migration-dependent feature is configuration-gated and defaults off. The updater never
enables it solely because a binary installed.

## Environment parity matrix

| Dimension | Required qualification |
|---|---|
| Windows / .NET | Supported Windows build, .NET runtime/SDK, and WPF/WebView/ConPTY availability. |
| User profile | Normal profile, redirected profile, unavailable DPAPI key, and restricted workspace directory. |
| Store volume | Empty, 50,000-edge fixture, low-disk, WAL-full, corrupt database, and locked database. |
| Upgrade state | Current, N-1 binary/schema/IPC, interrupted install, failed forward migration, and post-migration writes. |
| Security/privacy | Reparse-point path, hostile repository content, seeded PII/secrets, and no remote telemetry/egress. |

## Compatibility matrix

| Producer | Consumer | Supported during upgrade | Retirement |
|---|---|---|---|
| Shell IPC | Daemon IPC | N and N-1 major protocol with handshake; unsupported version fails closed. | Remove N-1 only after one broad release and no active N-1 health reports. |
| Daemon | SQLite schema | Expand/migrate dual-read then contract; daemon applies schema before writable commands. | Contract only after all supported binaries no longer need old reads. |
| Daemon | Export | Versioned export with N/N-1 import reader. | Retain import reader for one broad release after format retirement. |
| In-process extractor | Daemon | Co-deployed; fail fast on version mismatch. | No compatibility layer until independently hosted. |
| Isolated extractor / MCP | Daemon | Versioned schema/protocol with explicit supported range. | Contract retirement follows its own adapter release window. |

## Rollback and recovery runbook

1. Detect a health-gate failure or ring rollback trigger; stop new migrations and quiesce daemon
   writers. Persist the last accepted command receipt sequence.
2. Preserve a read-only copy of post-migration facts before restoring the pre-migration snapshot.
   The runbook reports those post-migration writes as `recovery-pending`; it never silently loses
   them.
3. Restore the previous binary and compatible snapshot; run store integrity, projection replay,
   IPC handshake, and fixture query checks.
4. If the previous projection passes, reopen read-only first, reconcile repository-derived facts,
   then selectively replay deterministic post-migration facts. Prompt/terminal outcomes that cannot
   be replayed remain explicit `DeliveryUnknown`/incomplete receipts.
5. Record the recovery result, retained limitations, duration, binary/schema versions, and
   operator acknowledgement. A failed restore remains a persisted health incident.

This runbook is rehearsed on copied representative stores before canary promotion. It has RTO 15
minutes for the Phase-1 corpus and RPO zero for rebuildable source facts; non-rebuildable local
draft/receipt loss is reported against the 24-hour backup policy.

## Required CI and release gates before Phase 1 dependencies

1. Central package management and committed lockfiles; restore in locked mode.
2. Exact dependency provenance/licence review, SBOM generation, vulnerability policy gate, and
   no known-exploitable shipped transitive CVE.
3. SHA-pinned GitHub Actions and a provenance review for every added action/tool.
4. Required jobs for P1 Proof Pack tests, migration up/down, backup/restore, query/load budget,
   security/STRIDE negatives, privacy/LINDDUN negatives, accessibility states, MCP schema/tool
   contracts, and documentation graph validation.
5. Release promotion reads actual required-job outcomes and Proof Pack artifacts; a green
   aggregator without those contents is not evidence.

## Release proof

The first release plan is not complete until an internal-ring run produces the Phase-1 Proof Pack,
a migration/rollback rehearsal, SBOM, lockfile verification, and parity matrix record. Until then
the architecture is a proposed design, not a releasable desktop product.
