---
id: "note-20260826-council-review-ai-ide-arch"
title: "Ten-persona adversary review of the 2026-08-25 AI-DE architecture found three hard and two soft vetoes"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "0"
tags: [decision-note, architecture, review, personas]
links:
  - { to: architecture, rel: relates-to }
review-by: 2027-02-26
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes" }
summary: >-
  Records the council review that drove the v2 architecture: which persona raised what, the three hard
  vetoes and two soft vetoes, and where each is resolved. Blast radius: it is the input of record for
  every change the v2 architecture makes.
---

# Ten-persona adversary review of the 2026-08-25 AI-DE architecture found three hard and two soft vetoes

*A decision note (`knowledge-visualization.md` V17): the origin story for the v2 architecture's changes.*

- **Kind:** resolved-question
- **Confidence:** Verified (findings cite file:line; two were confirmed by executed probes — the missing
  `spikes/` directory via `git ls-files`/`git log --all`, and the `INSERT OR REPLACE` immutability bypass
  via a run against sqlite3)
- **Made during:** an interactive adversary-mode critique of `docs/architecture.md`, then
  `/define-architecture ai-ide-arch-v2`.

## The call

Ten repo personas independently re-reviewed the 2026-08-25 architecture in Adversary Mode against the
spec, the seven ADRs, the threat model, the privacy review, and the conceptual model. The review did
**not** re-confirm the recorded PASS-WITH-CONDITIONS gate:

- **Hard vetoes (3):** Distributed Systems (at-most-once delivery asserted without a write-ahead
  record — a crash between PTY write and receipt commit lets a conformant retry re-deliver);
  Test Architect (three spikes cited as *Verified* were absent from the tree and git history; US-4
  knowledge navigation had no verification path in any phase); Privacy & Data Governance (MCP read tools
  are an unanalyzed indirect-egress path — an externally-processing agent can forward workspace facts to
  its provider; the LINDDUN review never modelled the flow and the local-only gate arrived four phases
  after the tool).
- **Soft vetoes (2):** Release Engineer (the depended-on release plan was authored but never committed —
  a `.gitignore` `[Rr]elease/` rule had swallowed `docs/release/`; and the binary-rollback path named no
  actor on a user machine); The Simplifier (Phase-1 built a separate daemon process, dual-version IPC,
  fleet-style rollback, and a bespoke telemetry subsystem before any driver existed).
- **Pass-with-conditions (5):** Enterprise (WPF unrecorded as an ADR; archetype drift), Data & Persistence
  (`INSERT OR REPLACE` bypass; receipt grain; two-clock interval trigger; DPAPI recovery), SRE (60s gate
  vs 15-min replay contradiction; orphaned ConPTY; silent watcher loss; incident store; SLI definitions),
  AI Systems (ranker escapes the model gate; agent claim → `Done`; partial byte caps; tool-description
  eval), Security (Phase-1 MCP transport unstated; supply-chain rows absent; outbound injection; dispatch
  TOCTOU).

Each finding's resolution is tabulated in the v2 architecture's **Review resolution** section and the
gate record. The verified internal contradictions (delivery gate vs RTO, prompt-draft retention,
immutability vs REPLACE, fold ordering, receipt grain, two clocks) were each fixed at the source
document.

## Alternatives dismissed

- **Amend the 2026-08-25 draft in place** — dismissed: the changes touch the archetype rationale, the
  daemon boundary, the delivery mechanism, the phasing, and four new ADRs; a supersede is cleaner and
  keeps the review auditable.
- **Accept the recorded gate and defer fixes to `/design`** — dismissed: three hard vetoes and verified
  contradictions are architecture-level, not detailed-design-level.

## Promotion rule

The load-bearing decisions this review forced are already promoted to ADR-0008..0011; this note remains
the review's origin record.
