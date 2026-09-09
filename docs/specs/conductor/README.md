---
id: spec-conductor
title: "AI-DE Conductor — specification inputs"
summary: "Provenance and authority for the Conductor spec v1.0 and mockups v2: what was ingested, its checksums, the decisions the spec locks, and the phasing that governs delivery."
type: spec
status: in-review
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, acp, coordination, weave, routing]
links:
  - { to: spec-ai-native-ide, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: depends-on }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-09
---

# AI-DE Conductor — specification inputs

Provenance for the two source artifacts in this directory.

| File | Role | Authority | SHA-256 |
| --- | --- | --- | --- |
| `ai-de-conductor-spec-v1.html` | Specification v1.0-draft, dated 2026-09-09 | **Authoritative.** Supersedes the proposal and the mockups wherever they conflict. | `8aac1ce1375c619438278329c0cae8c0f34681088bc98bc70fde7329d0172a49` |
| `ai-de-conductor-mockups-v2.html` | UX intent (Score/Performance, coordination board, Agent Profiler, routing) | Subordinate to the spec. | `72e0fec4aa98d70aba1dfc9d5f9dd0bffdfb326309f97d8b7d26169fce0ff29b` |

Ingested 2026-09-09 from the operator's `Downloads` directory, unmodified. The
spec's own provenance line records its sources as `timianmalloo/ai-de`,
`timianmalloo/ai-forward` and `timianmalloo/cfd-bench`, read at HEAD of `main`
on 2026-09-09.

Decisions locked by the spec before this programme starts:

- Model access is **subscription-first**; API keys are the recorded, off-by-default exception (§4.2, N2).
- The conductor runs as a **headless Claude Code session** on the Max account (§5.1).
- **Antigravity is deferred** to a later spike; it enters as an observed lane when it lands (N3).
- **No change to the Weave schema** — `weave/1` dimensions, weights, floors and the leaderboard partition are pinned (N6).
- **No replacement of the AI-Forward Pack** — AI-DE mechanizes its discipline and must not fork its semantics (N7).

Phasing (§12) is the delivery contract. Phase 1 ships the Agent Plane
(R1, R2, R4-core); later phases proceed only on an explicit ruling.
