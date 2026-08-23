---
id: note-collectknowledge-session-2026-08-23
title: "Decision note — /collectknowledge run, 2026-08-23"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [decision-note, collectknowledge, session-exhaust]
links:
  - { to: knowledge-hub, rel: relates-to }
review-by: 2027-02-19
review-suggested: []
summary: >-
  Session judgements from the /collectknowledge run that built the ten-topic domain knowledge
  base: the worktree exception, the seven-file template variant, the V16 over-propagation and
  its correction.
---

# Decision note — `/collectknowledge`, 2026-08-23

Judgements made during the run that are below ADR weight but would otherwise be lost.

## 1. Worked in the primary checkout rather than a session worktree

**Decision.** The run wrote directly to `main` in the primary checkout instead of creating a session
worktree.

**Why.** `session-worktree-discipline.md` WT1 makes a worktree the default and WT4 makes the primary
checkout a **recorded exception**. This is that record. `git worktree list` showed a single checkout with no
concurrent sessions; the change is additive and documentation-only (no source, no build); and the user
invoked the skill in this checkout expecting the knowledge base to land here. The companion
agent-coordination work is in a **separate location**, not a worktree of this repository, so there was no
concurrent writer to isolate from.

**What would change this.** Any future session that writes source, or any session run while another agent is
active in this repository, takes a worktree.

*Confidence: Verified (worktree state checked directly).*

## 2. Seven files per topic, not the template's eight

**Decision.** Each topic directory carries `index.md`, `state-of-the-art.md`, `comparables.md`,
`references.md`, `glossary.md`, `open-questions.md`, `sources.md` — folding the template's
`data-and-constants.md` into `references.md` as a "constants" section.

**Why.** The template says to "delete what does not apply". Across all ten topics the domain constants are
**licence terms, spec versions, API names and attribute stability levels** — facts that belong immediately
beside the source that establishes them, not in a separate file where the citation would have to be
repeated. Ten topics × eight files is also seventy-plus graph nodes before any project artifact exists; the
Simplifier gate applies to a knowledge base as much as to code.

**Recorded in each topic's `index.md`** so the deviation is visible where the missing file would have been
expected.

*Confidence: Verified (template read directly).*

## 3. V16 propagation over-fired and was corrected

**What happened.** Running `docs-graph.py flag --changed knowledge-hub` flagged **all twelve** inbound
neighbours as `review-suggested`, including the ten topic bases created in the *same change*.

**Why that is wrong.** V16 exists so that when an artifact changes, things that *already depended on it* get
reviewed. An artifact created in the same commit as its neighbour has nothing to re-review — the flag is
noise, and noise in a review-suggested queue is how the queue stops being read.

**Correction.** Cleared the ten topic flags; **kept the two seed-document flags**, which are genuinely
actionable: the architecture sketch selects Kuzu (archived October 2025) and the coordination specification
relies on TTL-plus-heartbeat leases without a fencing token. Both need revisiting, and the flag is the
correct mechanism for saying so.

**The generalisable lesson.** *Propagate V16 flags to artifacts that existed before the change, not to ones
created by it.* Worth remembering on any skill run that creates a hub plus its children in one pass.

*Confidence: Verified (observed and corrected in this session).*

## 4. Ten topics rather than one base per source document

**Decision.** The seed material named far more technologies than topics. They were grouped into ten domains
by **the decision each would inform** — storage, extraction, protocol, coordination, shell, rendering,
runtime visualisation, modelling, modelling history, cloud — rather than by which document mentioned them.

**Why.** A knowledge base is used at design time by someone asking a question. "Which graph store?" and
"what can Roslyn see?" are different questions needing different evidence, even though both arise from the
same sketch.

*Confidence: Inferred (a structuring judgement, not a fact).*

## 5. Research was delegated in a bounded fan-out of ten

**Decision.** Ten parallel `research` sub-agents, one per topic, rather than serial investigation.

**Why, and the honest accounting.** The topics are genuinely independent — no data edges between them — so
they pass the independence test. The fan-out cap in `execution-graph-optimization.md` GO7 defaults to four;
this run exceeded it deliberately, with the justification that the agents are read-only, run in separate
contexts, and are individually recoverable if rate-limited. All ten returned successfully. The cost is real:
roughly 400 KB of research output, which is the token multiplier the multi-agent literature in
`multi-agent-coordination/` documents — cited here because this run is itself an instance of it.

*Confidence: Verified (all ten agents completed; reports retained in the session workspace).*
