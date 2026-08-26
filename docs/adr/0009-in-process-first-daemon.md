---
id: adr-0009-in-process-first-daemon
title: "ADR-0009 — Run the authority core in-process in Phase 1; split to a daemon at Phase 2"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, daemon, phasing, simplicity, lifecycle]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0003-workspace-daemon-boundary, rel: relates-to }
  - { to: adr-0005-terminal-runtime-boundary, rel: relates-to }
  - { to: release-plan-ai-native-ide, rel: relates-to }
review-by: 2027-02-26
summary: >-
  Refines ADR-0003: the Workspace Authority Core is one logical boundary but runs in-process inside the
  shell in Phase 1, splitting to a separate per-workspace daemon process only at Phase 2 when the
  terminal runtime first needs process isolation. The Shell Bootstrap owns the process and upgrade
  lifecycle.
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes" }
---

# ADR-0009: Run the authority core in-process in Phase 1; split to a daemon at Phase 2

- **Status:** Proposed
- **Date:** 2026-08-26
- **Deciders:** Product owner, The Simplifier, Tech Lead, Enterprise Architect
- **Context spec/architecture:** docs/architecture.md

## Context

ADR-0003 makes the workspace authority a clear boundary — one writer, one store, tool authorization.
The prior draft realized that boundary as a **separate daemon process with a full named-pipe
SID/capability/epoch handshake in Phase 1**, before anything needed process isolation: Phase 1 mocks the
terminal/session runtime, so the genuine drivers of a second process (ConPTY ownership surviving a shell
restart; long-lived agent sessions) do not exist until Phase 2 (council finding, The Simplifier —
soft veto; corroborated by Enterprise Architect on premature upgrade machinery). LOA P1 (cheapest
sufficient) and the architecture's own rule — *in-process until isolation is actually needed* — argue
against building process supervision, transport, and an auth protocol with exactly one caller.

## Decision

We will implement the Workspace Authority Core as an **in-process module** in Phase 1, behind the same
command / `IWorkspaceStore` / tool-authorization contracts it will expose over IPC later. It **splits to
a separate per-workspace daemon process at Phase 2**, when the Terminal Session Runtime creates the first
real need for process isolation and restart-survival. The **Shell Bootstrap** owns the installed binary
layout, launches and supervises the core, and (from Phase 2) performs upgrade preflight and rollback and
reaps terminal processes via a Windows Job Object. Because the contracts are identical across the move,
the split is a **deployment substitution, not a redesign**.

## Alternatives considered

- **Separate daemon process from Phase 1 (prior draft):** rejected because it front-loads process
  supervision, a named-pipe auth protocol, reconnection, and P1-SEC transport tests for a boundary with
  one in-process caller and a mocked terminal runtime — ceremony without a driver.
- **All state permanently in the WPF process (never split):** rejected because ConPTY ownership must
  survive a shell/renderer crash and long-lived agent sessions must outlive a UI restart — real drivers
  that arrive at Phase 2.

## Consequences

- **Positive:** a smaller, faster walking skeleton; the invariants (single writer, receipts, path
  containment, tool authorization) are **interface** invariants preserved in-process, not process
  invariants; the auth/transport surface is built exactly when a second principal exists.
- **Negative / accepted trade-offs:** the Phase-1→Phase-2 split must be exercised as a real substitution
  (contract conformance), and the threat model's pipe-ACL/capability controls are **re-cut for the
  in-process boundary** in Phase 1 (fewer controls, because there is no cross-process surface yet) and
  restored at Phase 2.
- **Follow-ups / new risks:** Phase 2 must prove the process split against the same contracts
  (P2 conformance) and stand up the IPC auth protocol, dual-major handshake, and upgrade/rollback with
  P2-UPGRADE-01.

## Evidence

Architecture's stated extractor rule ("in-process until isolation is needed") and the Phase-1 mocked
terminal runtime [Verified from docs/architecture.md]. No new external contract; this is a phasing and
boundary-placement decision.
