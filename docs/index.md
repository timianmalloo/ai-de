---
id: docs-map-of-content
title: "AI-DE documentation — map of content"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [moc, index, documentation, navigation]
links:
  - { to: architecture, rel: documents }
  - { to: diagram-component, rel: relates-to }
  - { to: diagram-layers, rel: relates-to }
  - { to: diagram-sequence, rel: relates-to }
  - { to: diagram-class, rel: relates-to }
  - { to: knowledge-hub, rel: relates-to }
review-by: 2027-09-02
summary: >-
  A curated route into the AI-DE documentation for four different readers, rather than a mirror of
  the folder tree. The machine-navigable form is the Docs Explorer.
---

# AI-DE documentation — map of content

This is a **route**, not an inventory. The complete, typed, machine-navigable form of everything
below is the **[Docs Explorer](index.html)** — 304 artifacts with their links, health and review
state. Start here if you are a person; start there if you are looking for something specific.

The public presence over this material is the **project site**, authored in `site/` and published
to the repository's GitHub Pages URL by `.github/workflows/pages.yml`, which serves `site/` at the
root and this folder at `/docs/`.

## If you have five minutes

| Read | Why |
|---|---|
| [Architecture](architecture.md) | The component map, trust boundaries, command protocol and delivery semantics — the document everything else refines. |
| [Component diagram](diagrams/component.md) | What actually depends on what, read from the composition roots. |
| [Conceptual domain model](design/conceptual-model.md) | Bounded contexts, aggregate invariants, and the declared grain of every fact. |

## If you are here for the ideas

**The repository as a model.** A repository is indexed into one append-only fact store, and every
view is a rebuildable projection over it. Code is one surface of fifteen.

- [Spec — AI-native IDE](specs/ai-native-ide.md) · [rendered](specs/ai-native-ide.html)
- [Spec — editor surfaces](specs/editor-surfaces.md) · [knowledge exploration](specs/knowledge-exploration.md) · [UML/ERM surfaces](specs/uml-erm-surfaces.md)
- [Layered architecture — capability tiers](diagrams/layers.md)
- [ADR-0001 — derived evidence views](adr/0001-derived-evidence-views.md) · [ADR-0002 — workspace fact store](adr/0002-workspace-fact-store.md)

**Collaboration you can score.** Several harnesses and models in one repository, coordinating on a
board, compared on evidence, and remembered in a ledger.

- [Spec — Loomkeeper, the agentic watcher substrate](specs/agentic-watcher-substrate.md) · [rendered](specs/agentic-watcher-substrate.html)
- [Architecture — Loomkeeper](architecture/loomkeeper.md)
- Designs: [Weave score](design/watcher-weave-score.md) · [message board](design/watcher-message-board.md) · [board and leaderboard surfaces](design/watcher-board-leaderboard-surfaces.md) · [scoring service](design/watcher-scoring-service.md) · [score dispute](design/watcher-score-dispute.md)
- [Daydream and the seam to the offline Dream](design/watcher-daydream-dream-seam.md) — the online half of continuous improvement, what crosses to the batch pass, how a retraction propagates, and why the pack's script stays an optional integration rather than a runtime dependency. **Proposed; nothing built.**
- [ADR-0020 trusted-registrar-harness-model-identity — trusted registrar, harness and model identity](adr/0020-trusted-registrar-harness-model-identity.md) · [ADR-0019 advisory-evaluator-calibration — advisory evaluator calibration](adr/0019-advisory-evaluator-calibration.md)
- [Session contracts](collaboration/session-contracts.md) — the single ownership register for concurrent agent sessions.

## If you are going to change the code

| Read | Why |
|---|---|
| [API reference](_site/index.html) | The public C# surface with its own doc comments. 1,733 public symbols; members without a comment are listed as gaps. |
| [Class diagram](diagrams/class.md) · [sequence diagrams](diagrams/sequence.md) | The scoring types, and the two flows the product turns on. |
| [Defect classes](lessons/defect-classes.md) | 81 classes of mistake this project has produced, each converted into a test or a gate. The cheapest hour you can spend here. |
| [Testing strategy](../.claude/knowledge/testing-strategy.md) | What counts as proof. |
| [Audit &amp; change ledger](audit/index.html) | What was done, why, and by which prompt. |

## If you are auditing it

- [Threat model](security/threat-model.md) · [privacy review](security/privacy-review.md)
- [Proof packs](proof/) — one per design, recording what was actually executed.
- [Investigations](investigations/) — the ones that changed a decision.
- [Measurements](measurements/) — numbers that were taken rather than reasoned about.

## How this documentation stays honest

Every artifact under `docs/` carries typed YAML frontmatter, and `docs/docs-index.js` is **derived**
from it — never hand-edited. A pull request fails if the derived index disagrees with the
frontmatter it came from, if a typed link dangles, or if an artifact's `review-by` date has passed.
The freshness gate is `docs-graph.py freshness`; the graph validator is `docs-graph.py validate`.

The API reference and the four diagram families are regenerated by `/document`; coverage and
confidence for the last run are in [`_meta.json`](_meta.json).
