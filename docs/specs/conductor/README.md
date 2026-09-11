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
  - { to: note-conductor-spec-errata-policy, rel: relates-to }
  - { to: note-conductor-spec-errata-lane-rename, rel: relates-to }
review-by: 2026-12-09
---

# AI-DE Conductor — specification inputs

Provenance for the source artifacts in this directory, and the in-repo addendum that refines them.

| File | Role | Authority | SHA-256 |
| --- | --- | --- | --- |
| `ai-de-conductor-spec-v1.html` | Specification v1.0-draft, dated 2026-09-09 | **Authoritative.** Supersedes the proposal and the mockups wherever they conflict. | `8aac1ce1375c619438278329c0cae8c0f34681088bc98bc70fde7329d0172a49` |
| `ai-de-conductor-mockups-v2.html` | UX intent (Score/Performance, coordination board, Agent Profiler, routing) | Subordinate to the spec. | `72e0fec4aa98d70aba1dfc9d5f9dd0bffdfb326309f97d8b7d26169fce0ff29b` |
| `ai-de-spec-addendum-a-session-experience.html` | **Addendum A — The Session Experience**, dated 2026-09-09 | **Normative. SUPERSEDES v1.0 where they conflict.** Adds R13–R16 to Phase 1, R17 to Phase 3, repairs the "session" vocabulary collision (A3), and rewrites Phase 1's exit evidence. | `f23fe61168e395bbddae8e63c47e18b46875a5c90a21322fe36f258940f3361c` |
| `../addendum-c-perspectives.md` (`spec-addendum-c-perspectives`) | **Addendum C — Perspectives**, dated 2026-09-11 | **Normative** for the perspective set (Coding · Explore · Architecture), the rail, the per-perspective surface allow-lists, the derived menu, the default layouts and the persistence slots. **Does not amend Addendum A's text** (Ruling 51): a conflict with A is surfaced in its §R as a finding for the Owner, and A stands until ruled. Binds Rulings 50–55 (`note-addendum-c-council-rulings`). | *authored in-repo, not ingested — a living markdown artifact with no frozen checksum; its history is git's* |
| `../addendum-d-compile-step.md` (`spec-addendum-d-compile-step`) | **Addendum D — The Compile Step**, dated 2026-09-11 | **Normative** for the compile step: the three columns (settings · compile context · decorations), the Prompt Compilation domain model and the `compiled-envelope/1` event store, the mechanical tier rule, the compile modes and their degradation, Prepare, the projections onto the unchanged `GovernedRunRequest` / `SpawnContract`, and the eval gate the agentic stage ships behind. **Refines Addendum C** (the home of C's D-5; closes Ruling 63's open tier derivation) and **supersedes Addendum B `:152`** (a separately configured assist provider) for the compile step only — quoted in its §R, never edited (Ruling 51's discipline). In review; its proposed rulings are numbered by the Owner. | *authored in-repo; its history is git's* |

The three HTML files were ingested 2026-09-09 from the operator's `Downloads`
directory, unmodified. The spec's own provenance line records its sources as
`timianmalloo/ai-de`, `timianmalloo/ai-forward` and `timianmalloo/cfd-bench`,
read at HEAD of `main` on 2026-09-09. Addendum C is the first addendum authored
in the repository rather than ingested; the errata policy below applies to the
byte-frozen HTML only.

Decisions locked by the spec before this programme starts:

- Model access is **subscription-first**; API keys are the recorded, off-by-default exception (§4.2, N2).
- The conductor runs as a **headless Claude Code session** on the Max account (§5.1).
- **Antigravity is deferred** to a later spike; it enters as an observed lane when it lands (N3).
- **No change to the Weave schema** — `weave/1` dimensions, weights, floors and the leaderboard partition are pinned (N6).
- **No replacement of the AI-Forward Pack** — AI-DE mechanizes its discipline and must not fork its semantics (N7).

**Reading order:** Addendum A wins over v1.0; v1.0 wins over the mockups; all three win over any prompt summarising them. The reconciliation of Addendum A against work already delivered is `note-addendum-a-reconciliation`.

Phasing (§12) is the delivery contract. Phase 1 ships the Agent Plane
(R1, R2, R4-core); later phases proceed only on an explicit ruling.

## Errata

Corrections to the byte-frozen spec HTML, per `note-conductor-spec-errata-policy` (quote the
corrected line, verbatim, with its line number; never edit the HTML):

- `note-conductor-spec-errata-lane-rename` — v1.0 §6.2 (line 260), §10 (line 336), §11
  (line 376) name `GovernedSessionSource`; superseded by `GovernedLaneSource` (Ruling 15/A3).
- `note-conductor-spec-errata-providers-json` — v1.0 §4.3 (line 209) and §14.2 (lines 472–473)
  name `~/.aide/providers.yaml`; this repository reads `~/.aide/providers.json` (Ruling 23's
  ladder applied to the file Ruling 23 named as the open case, under Ruling 36's YAML scope).
