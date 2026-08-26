---
id: adr-0011-session-processing-class-egress
title: "ADR-0011 — Bind MCP tool authorization to the session processing class"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, mcp, privacy, egress, authorization]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0004-mcp-tool-boundary, rel: relates-to }
  - { to: privacy-review-ai-native-ide, rel: implements }
review-by: 2027-02-26
summary: >-
  Refines ADR-0004: MCP read and write authorization is bound to the target session's declared
  data-processing class from Phase 1, so an externally-processing agent cannot pull workspace facts via
  describe/find/impact and forward them to its provider. Closes the unanalyzed indirect-egress path the
  privacy review had not modelled.
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes" }
---

# ADR-0011: Bind MCP tool authorization to the session processing class

- **Status:** Proposed
- **Date:** 2026-08-26
- **Deciders:** Product owner, Privacy & Data Governance, Security & Identity, AI Systems
- **Context spec/architecture:** docs/architecture.md

## Context

The privacy posture is egress-deny-by-default: v1 supports rich prompt transfer only to `LocalOnly`
sessions (privacy review §Agent-session and egress classification). But the **MCP read tools are a second
egress path the review never modelled** (Privacy hard veto): `describe` ships in Phase 1 and returns
workspace-derived facts — source symbols, provenance, coordination author fields, audit references — to
Claude Code / Copilot CLI sessions. An `ExternalProcessing` agent (the normal case) can forward those to
its provider's model context: **indirect model egress with no purpose/basis record**, defeating the
`LocalOnly`-only posture through a side door. Loopback binding does not mitigate — the agent process is
local; its provider is not. The prior draft's local-only policy gate arrived only in Phase 5, four phases
after the tool it must govern (LOA P11: least-privilege delegated identity; P8 at the egress boundary).

## Decision

We will bind **MCP tool authorization to the target session's declared data-processing class**
(`LocalOnly` / `ExternalProcessing` / `UnknownProcessing`) as a **T0 gateway rule from Phase 1**, for
reads and writes:

- `LocalOnly`: bounded reads and permitted writes after normal authorization.
- `ExternalProcessing` / `UnknownProcessing`: rich reads are **denied or served minimum non-sensitive
  metadata only**, and knowledge/coordination writes are denied.

The processing class is an attestable, revalidated contract (privacy review), revalidated immediately
before each tool call and invalidated on session generation / executable / configuration change; an
unverifiable or stale attestation downgrades to `UnknownProcessing` and fails closed. The MCP egress flow
is added to the privacy review's LINDDUN table, and a red-first negative (`P1-MCP-EGRESS`) proves denial
for non-`LocalOnly` callers.

## Alternatives considered

- **Transport-only control (loopback binding):** rejected — it bounds *who connects*, not *where the
  bytes go next*; a local externally-processing agent passes it and still exfiltrates to its provider.
- **Defer the gate to Phase 5 (prior draft):** rejected — the `describe` tool it governs ships in
  Phase 1, so the control must ship with it, not four phases later.
- **Content-scrub every MCP result unconditionally:** rejected as the primary control — it degrades the
  `LocalOnly` experience the product exists for; classification-bound authorization preserves it while
  denying the risky class.

## Consequences

- **Positive:** the `LocalOnly`-only egress posture holds through the MCP surface; the indirect-egress
  flow is modelled and tested; authority follows the delegated-identity boundary.
- **Negative / accepted trade-offs:** every MCP call carries and revalidates a session processing class,
  and an agent whose class cannot be established gets minimum metadata (fail-closed) — a deliberate
  usability cost for the risky/unknown case.
- **Follow-ups / new risks:** the attestation mechanism for a session's processing class is a Phase-4/5
  design item; until then only `LocalOnly` (locally-processing) sessions receive rich results, and all
  else fails closed.

## Evidence

Privacy review egress classification and the spec's egress-deny posture [Verified from docs]. The MCP
stdio tool surface and its bounded results are spike-verified (`spikes/mcp-server`) [Verified]; the
class-binding authorization itself is a Phase-1 control proven red-first by `P1-MCP-EGRESS` [Inferred
until that test runs].
