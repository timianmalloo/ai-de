---
id: kb-agentic-session-observability-glossary
title: "Agentic Session Observability - Glossary"
type: glossary
status: draft
owner: "@timianmalloo"
phase: "discovery"
tags: [glossary, ubiquitous-language, watcher]
links:
  - { to: kb-agentic-session-observability, rel: refines }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Ubiquitous language for the watcher domain, separating identities, observations, evaluations,
  coordination records, and promoted learning.
---

# Glossary

- **Watcher** - the substrate participant that registers and observes agent sessions, projects
  coordination state, evaluates work episodes, and curates candidate learning. It is not the
  authority on repository truth.
- **Repository Identity** - the canonical identity of one Git repository, independent of its folder
  display name.
- **Worktree Identity** - one checked-out working tree belonging to a Repository Identity.
- **Terminal Identity** - one terminal process/container generation hosting zero or one active agent
  session at a time.
- **Agent Identity** - the logical agent or configured capability, distinct from the model version
  and from a running session.
- **Agent Session** - one registered, bounded period in which an agent pursues a goal through turns,
  model calls, tool calls, and outputs.
- **Turn** - one user input and the resulting agent activity until the next user input or terminal
  completion.
- **Run / Span** - one observable unit of work, such as planning, a model call, a tool call, or a
  verification action.
- **Trace** - the causal tree of spans for one operation or turn.
- **Trajectory** - an ordered projection of a session or turn used for human reading and process
  evaluation.
- **Registration** - the session-start declaration that binds Agent Session to repository,
  worktree, terminal, agent, model, and goal context.
- **Heartbeat** - liveness evidence that renews a session's ephemeral registration.
- **Lease** - a time-bounded coordination grant. A lease alone does not prevent a stale process from
  acting after expiry.
- **Fencing Token** - a strictly increasing value attached to an exclusive action; the guarded
  resource rejects stale values.
- **Watch** - an edge-triggered notification that state changed; a reconnecting consumer re-reads
  current state rather than assuming it saw every event.
- **Repo Message Board** - an append-only, repository-scoped stream of agent-authored questions,
  decisions, breadcrumbs, replies, and knowledge candidates.
- **Work Episode** - the evaluable grain binding one stated goal and done condition to the actions,
  artifacts, evidence, and outcome of a session interval.
- **Scorecard** - a versioned set of dimension scores with evidence and residual uncertainty.
- **Agentic Score** - a compact summary of the Scorecard. It is a navigation aid, not an
  unquestionable judgment.
- **Daydream Observation** - one watcher-recorded behavior, mistake, insight, or repeated pattern,
  with evidence and confidence.
- **Candidate Lesson** - a generalized claim derived from one or more Daydream Observations but not
  yet promoted into agent guidance.
- **Promoted Learning** - a reviewed candidate that has disconfirming evidence considered, a
  versioned control or instruction, and a measurable expected effect.
- **Context Collapse** - loss of hard-won detail through repeated summarization or rewriting of
  agent guidance.
- **Memory Poisoning** - malicious or erroneous content entering long-term memory and being retrieved
  later as trusted guidance.
- **Not Recorded** - the explicit result when the watcher lacks trustworthy evidence. It is never
  replaced with a plausible value.

