---
id: note-workbench-loops-architecture-exhaust
title: "Workbench-loops architecture run — session exhaust: drift found, calls made below ADR weight"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [decision-note, architecture, workbench-loops, drift, spike]
links:
  - { to: architecture, rel: relates-to }
  - { to: spec-workbench-loops-ux, rel: relates-to }
  - { to: adr-0045-perspective-set-factory-review-understand, rel: relates-to }
  - { to: adr-0042-native-harness-profile-binding, rel: relates-to }
review-by: 2027-04-18
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
  - { by: adr-0045-perspective-set-factory-review-understand, on: 2026-10-18, reason: "Ruling 84's Coordination bench superseded; Loomkeeper kinds move under Factory" }
summary: >-
  Records what the 2026-10-18 /define-architecture run found that no ADR carries: four Perspective
  rows in code against ADR-0030's three, the stale IWorkspaceStore name, Grok already catalogued,
  Antigravity sign-in being interactive, Muse self-updating mid-session, and the calls made on
  council composition, slice re-cut and the strict default acceptance policy.
---

# Workbench-loops architecture run — session exhaust

*A decision note (V17): below ADR weight, above chat-scrollback weight.*

- **Kind:** resolved-question / decision
- **Confidence:** Verified for every drift item (file opened or command run); Inferred where marked
- **Made during:** `/define-architecture` of `spec-workbench-loops-ux`, session `define-arch-20261018`

## Drift and findings (each verified)

1. **`PerspectiveSet` has four rows** (Coding, Explore, Architecture, Coordination —
   `src/AiDe.Core/Workbench/Perspectives.cs:63-82`) while ADR-0030 says three and "a fourth fails
   the build test". The code drifted before this run. ADR-0045 supersedes the rows; the Coordination
   row's surfaces are admitted under Factory → Coordination.
2. **`IWorkspaceStore` does not exist**; the code uses the concrete `WorkspaceStore`. ADR-0002 and
   the §Component map name the interface. Corrected in §WL.5; ADR-0002's text left as history.
3. **Grok already has a native ACP catalog row** (`EngineCatalog.cs:287`); Gemini too (:261). The
   research note implied Grok needed a row. Only Antigravity (`agy`) and Muse are missing, and
   `EngineCatalogTests.cs:405` asserts "antigravity" is refused — a deliberate deferral ADR-0042
   reverses behind a qualification gate.
4. **Antigravity has no `login` subcommand** at 1.3.5; sign-in is the interactive first run. The
   binding must launch that run visibly rather than a login command.
5. **Muse self-updated between two read-only probes** (1.4.7 → 1.4.10, minutes apart). Version is
   observed per attempt, never assumed from install (ADR-0042 clause 7).
6. **Stop is synthetic today**: a .NET token cancel yields `TurnState.Stopped` with no native
   `session/cancel` (`SessionDocumentSurface.cs:835`; `AcpLaneClient` has no cancel method). The
   spec forbids exactly this; ADR-0041 makes native acknowledgment the only path to `interrupted`.
7. **The composer page protocol has five kinds and refuses `send`** by test
   (`ComposerVocabulary.cs:20-50`). ADR-0043 keeps that refusal as a floor for protocol v2.

## Calls below ADR weight

- **Council composition.** Fan-out cap 6 (goal state). Seats: Security, Distributed Systems,
  Data & Persistence, Enterprise, SRE, Simplifier. The Test Architect's verification-path check
  was assigned to the SRE seat; the Patterns Expert's standard-idiom check to the Enterprise seat.
  Rationale: the two hard vetoes and the data veto are non-negotiable seats; SRE owns proof
  paths in CI (CE standard); the Simplifier is the adversarial mirror of the ladder.
- **Slice re-cut.** The proposal's slices 0–6 are kept in intent but WL-1 is cut as a true walking
  skeleton (journey + notebook/attempt seam on the real claude-code lane, both journeys, keyboard
  only) rather than "desktop composition" alone, which would be a horizontal layer.
- **Acceptance policy default.** Until the product decision the spec defers is recorded, no human
  disposition covers a mandatory obligation except a `needs-decision` escalation; the fold blocks.
  Validation condition: holds until the owner records the lighter-review policy with the evidence
  the spec requires (reversibility, escaped defects, calibrated outcomes).
- **Transport kinds.** ACP (claude/codex adapters, copilot, grok), Antigravity stream-json, Muse
  exec JSONL — three mappers, one `RunEvent` envelope. Codex `app-server` is not chosen over ACP
  for the first slices (the adapter row exists and is proven); recorded as an alternative.
- **Spike scope.** `spikes/native-harness-presence` probes presence, version and help text only —
  no sign-in, no model call — because a six-family sign-in is user-paced and paid; it is the first
  WL-3 red, not this run's.

## Calls made at the gate (Orchestrator holding the Tech Lead's casting vote)

- **WL-1 re-cut** (Enterprise finding 5, Simplifier finding 20): WL-1a = the revision↔attempt seam
  on the existing Coding host and `session-document` kind; WL-1b = obligations/evidence/acceptance;
  WL-1c = the system journey and bridge; WL-S = the shell re-row, parallel to WL-1a. **`session/cancel`
  stays in WL-1a** (Simplifier and Distributed Systems: today's `Stopped` is synthetic, AC-R03c is a
  floor of the seam) rather than moving to WL-4 (Enterprise's proposal); per-family acknowledgment
  beyond claude stays in WL-4.
- **ADR-0045 split** into (a) re-row and (b) `FullWindow` retirement, both in the one ADR with
  separate build tests; Ruling 84 superseded explicitly; Loomkeeper's bench relocation flagged.
- **Security residual accepted on the user's stated posture**: pack scripts under a third-party PR
  head are not sandboxed; a trust fact is shown instead (ADR-0044 §4). The two Security blockers
  (redaction at ingest; no inherited credentials) are floors the spec already requires and were
  taken in full.
- **Simplifier merges taken in full**: widen the dispatch pair and `prompt_revision_fact`, no
  `obligation_state_fact`, no `journey_link_fact`, no caches, no `SurfaceModelRegistry`, one
  `TurnState`, protocol v2 minimum, `test_attempt_fact`/steer deferred to their producers.
- **WL.9 rewritten** to LOA Part VII criteria after the Enterprise Architect found the principles
  relabelled as criteria (a regression inside one file); C2 budget propagation recorded FAIL until
  WL-4.

## Finding at close: the HTML knowledge-surface cap is full

`docs-graph.py derive` refuses when more than 100 HTML files exist under `docs/` outside the pack
(`SURFACE_LIMIT = 100`, `docs-graph.py:82, :668`). The repo sat at exactly 100 before this run, so the
nine rendered companions (seven ADRs, this note, the drive prompt) tripped it. ADRs in this repo
have never carried HTML companions; the nine were removed and Markdown remains canonical and
indexed. **Next step (pack-level, not this run):** partition or raise the cap in the pack script, or
exclude `adr/` companions by convention — a V19 obligation that cannot be met under a cap is a
control worth re-deciding, not a reason to render less.

## Alternatives dismissed

- Convening eight seats — refused by the fan-out cap; the two folded lenses are named above.
- Writing a separate ADR for Assurance (change packet / test worthiness) — its grains live in
  ADR-0039 and its projections reuse the extractor contract; a separate ADR would restate both.

## Promotion rule

Items 1 and 6 already bear load and are carried by ADR-0045 and ADR-0041. The acceptance-policy
default becomes an ADR when the owner records the policy.
