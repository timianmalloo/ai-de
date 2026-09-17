---
id: adr-01M2NJ2PQS7X6GE3F559JC4TNP-cross-harness-canonical-stream
title: "Cross-harness coordination uses the official request stream"
type: adr
status: draft
owner: "@timianmalloo"
phase: P0
tags: [coordination, canonical-stream, data, compatibility]
links:
  - { to: spec-cross-harness-coordination, rel: implements }
  - { to: design-cross-harness-coordination, rel: relates-to }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: refines }
  - { to: adr-0023-watcher-observation-projection, rel: relates-to }
  - { to: proof-cross-harness-coordination, rel: tested-by }
review-by: 2026-10-16
summary: >-
  Draft ADR selecting the human-approved canonical official request log, not board dual-write.
  Records the event-log alternative to dimensional authoritative storage, accepted fold cost,
  unchanged-client compatibility, bounded existing-watcher-store additions and inherited debt.
---

# Official request stream; board is a coordination read model

**Status: P0 corrected draft; GATE P0-delta pending independent review.**
Official allocator: `coord-core.py allocate --scheme adr` returned
`adr-01M2NJ2PQS7X6GE3F559JC4TNP`. Established directory is `docs/adr/`, not
`docs/decisions/`. The official ID is intentionally not replaced by a guessed numeric ADR.

## Context

The actual human approved P0–P5 repairs, retaining the investigation's canonical-stream
boundary. ADR-0020 already says AI-Forward sessions reuse coord-core, and US-4 requires
repository-scoped provenance, parents, explicit failure and quarantined board content.
The source has two formats, not two interchangeable replicas: request-add/request-resolve
and loomkeeper/1 board-post. Ownership remains only `session-contracts.md §2`.

## Proposed decision

Use **one primary `.agents/requests.jsonl` through official tooling** for cross-harness
coordination facts and dispositions. Evolve append/read contracts compatibly. Stable
event keys plus payload equality make transport retries idempotent; history is neither
rewritten nor deduplicated. Board/MCP coordination writes go through the same canonical
command; SQLite is a rebuildable projection with atomic effect/receipt/checkpoint.
Native board facts retain their existing path and meaning.

This is the **event-log/event-sourced fold alternative** under DM5/DM13, chosen because
there is already an authoritative append history whose identity/payloads must survive.
Do not replace that history with a new dimensional authority database. Conceptual
dimensions (repository, endpoint generation, revision) remain immutable/versioned
references; changes are appended facts. Any dimensional/analytical view is derived,
never a second source. A current-valued ODS cache is not called an analytical star.

Accepted read cost: initial replay O(n), then bounded incremental fold/current-by-key
reads and cursor seeks; indexes/cache require replay-equality proof. Native newest-200
views are not coordination recovery feeds. No unmeasured hot-path full scan is accepted.

## Alternatives and consequences

* **Board canonical:** rejected; contradicts ADR-0020 and requires migrating queue users,
  authority semantics and historical identities merely to repair correlation.
* **Independent queue/board dual-write:** rejected; creates an atomicity gap and two
  definitions of acceptance. Standard inbox/read-model metadata is not another authority.
* **New normalized/temporal or dimensional canonical DB:** rejected for this scope;
  costly replacement of existing history with no necessary benefit.
* **Only wake/poll adapters:** rejected as complete solution; delivery cannot fix missing
  typed disposition or establish authority.

Positive: fallback remains official filesystem pull even when board/harness is unavailable.
Negative: transitional old readers do not understand enhanced completion; expose that
limitation without disabling ordinary legacy operations. **Unchanged, unenrolled
old-worktree clients retain official request-add/request-resolve/list on the same primary
`requests.jsonl`, without enrollment or upgrade.** Enrollment gates enhanced capabilities
only; no legacy client is disabled. Old coord-core append is unlocked: a new cooperative
lock does not automatically protect it, and an upgraded shim cannot prove coexistence.
O08/O10 pin an unmodified pre-P1 official client in an unenrolled synthetic worktree for
add→resolve→list with enhanced disabled and across rollback, asserting original IDs/payloads.
Enhanced writing stays disabled until separate mixed-client contention, complete-record
and conflict-safety proof exists with the unchanged client.
Authority verification is not supplied by content hashing; independent Security gate
must establish registrar evidence and authenticated actual-human NEW-transfer approval.

## Bounded store ruling and inherited architecture debt

ADR-0023 names the shared workspace fact store, while `WatcherHost.Open:66` opens a
SQLite `watcher.db`. Coordination application/feed/checkpoint caches are additive behind
the **existing watcher observation-store seam in existing `watcher.db` used by
WatcherHost/MCP**; `requests.jsonl` remains the sole canonical coordination source.
No new DB, native relocation, `workspace.db` migration or cross-DB transaction is in scope.
The existing physical-placement divergence is **inherited architecture debt, not claimed
conformant**. Do not expand the programme to resolve it. Data/Persistence and Distributed
Systems must coapprove the concrete additive representation before P2 code.

## Admission is not activation

Actual-human approval of all P0–P5 implementation remains in force; no renewed human phase
approval is requested. Independent P0-delta clearance admits ONLY compatible **dormant P1
fold/envelope/schema-validation and isolated tests**, not deployed SQLite schema; the
dormant subset does not complete P1. All privileged production paths deny absent qualified
verifier evidence. An apparently valid synthetic verifier receipt through the production
entrypoint must yield zero grants/transfers/endpoint sends/launches (O07).
Synthetic positive controls establish deterministic contract behavior, not production
authority. Authenticated authority fixtures, supported-channel qualification and P3/P4
activation remain BLOCKED; repository/peer prose is inert.

## Reversibility

Add reader capability before writer activation; additive cache migration only after
P2 real-store up/operational-down proof. Disable enhanced writer/importer, keep unchanged legacy clients operational, switch to compatible
reads and preserve all accepted facts. Do not restore old status files, delete new
events, dedup old messages or create a second consumed file. Contract/drop is a later,
separately approved change. Release sequences rollout; Data and DS review correctness.

## Evidence and gate

Source/read pins, concrete failure oracles and confidence labels are centralized in
the [Proof Pack](../proof/cross-harness-coordination-proof-pack.md).
This author proposes the ADR and **does not clear its independent authority, data,
DS, Test, Security, Privacy or UX gates**.
P0 is a corrected draft; P1 is pending implementation; P2–P5 are pending. O11–O20 real
SQLite/crash/replay/rollback floors are unchanged; static replay/cursor risks remain
Inferred. Live retention/erasure across payload copies and real foreground/background
GHCP/Codex/Grok/Claude conformance remain unresolved/BLOCKED; fakes do not clear them.
Proposed SLIs are not measurements. Docs index and rollups remain pending with conductor.
