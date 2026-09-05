---
id: adr-0024-credential-backed-grading-egress
title: "ADR-0024 — Credentials are DPAPI local secrets and off-device grading is an opt-in egress path"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "discovery"
tags: [architecture, loomkeeper, security, privacy, egress, credentials, dpapi]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0011-session-processing-class-egress, rel: refines }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Loomkeeper credentials are sealed with DPAPI CurrentUser and never logged or emitted; outbound
  network is denied by default; credential-backed off-device grading is an ADR-0011 ExternalProcessing
  egress path that stays blocked until an explicit, revocable, per-path opt-in reclassifies it.
---

# ADR-0024 credential-backed-grading-egress: Credential-backed grading egress

- **Status:** Accepted (provisional pending spikes S2/S3)
- **Date:** 2026-08-30
- **Deciders:** Product owner, Security & Identity, Privacy & Data Governance, AI Systems
- **Context spec/architecture:** docs/architecture/loomkeeper.md, docs/specs/agentic-watcher-substrate.md

## Context

The user requires configuring the watcher with their own credentials (for example Claude Code or
GitHub Copilot) so a grader or Daydream can, if chosen, use a hosted model. That request touches the
one settled invariant that can leak work off the device: the spec's local-only-by-default posture and
ADR-0011's egress-deny-by-default, class-bound authorization. This is the highest-risk surface in the
subsystem: a credential is a secret, and a credential-backed model call is indirect model egress. It
must be designed to fail closed rather than bolted on later (the ADR-0011 lesson: the gate ships
before the thing it governs).

## Decision

- **Outbound denied by default** at the watcher process boundary; the default state is `Egress
  blocked`. The deny gateway is a **T0 control that ships in Phase 1**, before any component that
  could egress, proven red-first by a negative test.
- **Credentials are local secrets**, sealed with **DPAPI CurrentUser** (the repo's established
  at-rest mechanism). They render only as masked references, never appear in logs, telemetry, board,
  scores, or learning, and are revocable; revocation drops the secret and keeps no derived copy.
- **Credential-backed off-device grading is an `ExternalProcessing` egress path** in ADR-0011 terms.
  It is denied until the user accepts an **explicit, per-path opt-in notice** (purpose, endpoint, data
  classes). Opting in reclassifies *that one grading path only*; every other path stays local-only.
- The processing class is revalidated before each call and fails closed on a stale or unverifiable
  attestation (ADR-0011). Redaction runs before persistence and before any permitted egress.

## Alternatives considered

- **Silently call the hosted model with the user's credential.** Rejected: breaks the local-only
  posture without notice or basis; a Privacy hard veto.
- **A global "allow egress" switch.** Rejected: too coarse — it opens every path; the opt-in is
  per-path so only the chosen grading endpoint is reachable.
- **Store credentials in config or environment.** Rejected: secrets in plaintext config/logs; DPAPI
  seal is the established repo control.
- **Ship credential-backed grading before proving process-level denial (S3).** Rejected: the
  local-only guarantee rests on S3; the credential-backed grader is not built until S3 is green.

## Consequences

- **Positive:** the local-only default holds; egress is explicit, per-path, noticed, and revocable;
  credentials are sealed and never emitted; authority follows the delegated-identity boundary (P11).
- **Negative / accepted trade-off:** off-device grading requires a deliberate opt-in per path — a
  usability cost that is the point of the control.
- **Follow-ups / new risks:** **S3 (process-level outbound denial on Windows/.NET) is unproven and
  load-bearing.** If denial cannot be enforced at the process boundary, the **fallback** is to
  register no outbound network stack at all in v1 and defer credential-backed grading to a later
  version with its own security review. S2 (DPAPI credential lifecycle) is lower risk but still
  spiked before Phase 4.

## Evidence

ADR-0011 egress-deny-by-default and class-bound authorization [Verified from docs]. DPAPI CurrentUser
is the repo's at-rest mechanism (Engineering Governance §4) [Verified from docs]. Process-level
outbound denial (S3) and the DPAPI credential lifecycle (S2) are **not yet spike-verified**
[Flagged — provisional until PoC].
