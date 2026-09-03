---
id: "note-20260902-two-decisions-the-loop-waits-on"
title: "One question the agent loop waits on, asked twice"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "phase-3"
tags: [decision-note, watcher, daydream, collaboration, scoring, scope]
links:
  - { to: adr-0019-advisory-evaluator-calibration, rel: relates-to }
links-suggested: []
review-by: 2027-03-02
review-suggested: []
summary: >-
  The agent collaboration loop is complete except for one link, and the Daydream vertical is complete
  except for one, and both are the same question: how far outside its own store does this product
  reach. Written down unchosen, with the options and what each costs, because choosing while the
  owner was away would have been choosing for them and calling it wiring.
---

# One question the loop waits on, asked twice

Two sessions worked the collaboration and Daydream tracks in parallel on 2026-09-02. Both reached a
stop, independently, and the stops turned out to be the same question.

## Where the collaboration loop actually ends

Proven as a **chain**, not merely at each seam — `TheAgentCollaborationCircuitTests`, headless, with
no shell and no UI, driven the way an agent drives it:

| Link | State |
|---|---|
| agent launches | works — confirmed in the running product (DC-083, DC-084) |
| registers through the contract log | works as a circuit |
| observed | works as a circuit |
| board read and write | works as a circuit |
| **episode scored** | **not reached from the agent path** |
| standing delivered to the agent | works, and is unreachable until the above |

**Scoring has exactly one producer** — `WatcherHost.ImportAndScoreEpisodesFromAuditLog` — which reads
AI-DE's own audit log. The identifier spaces cannot meet even in principle:
`AuditLogEpisodeSource` takes `SessionId` from the log's `session` field, while `TrustedRegistrar`
mints a fresh id for a registered agent. So an agent that registers and declares work produces a
closed episode, no scorecard, and no standing — permanently.

Nothing is broken. `IngestHost.CloseEpisode` closes an episode and returns, which is correct: a
declaration is not a quality judgement. `StandingPublisher` writes nothing when there is nothing to
say, which is also correct. The chain simply has no link at that point, and no seam test could show
that.

## The three answers, and why none is wiring

| Option | What it makes a score mean |
|---|---|
| the **agent supplies** its own deterministic signals | a standing rests on self-report, and ADR-0019's entire anti-Goodhart concern arrives at once |
| the **watcher derives** them | from spans and board posts — the only observations there are, and neither is evidence of outcome |
| scoring stays **audit-only** | honest, and quietly narrows US-16 to AI-DE's own sessions rather than to agents |

These are three different products. The middle one is the only that keeps the score an observation,
and it is also the one with no evidence to observe.

## The same question, from the Daydream side

D5's spike falsified its own design: `dream.py`'s `load_corpus` reads five fixed paths and `cmd_run`
takes only `--root/--session/--days`. There is no inbox and no extension point, so the proposed
outbound half — AI-DE emitting candidates into the pack's corpus — has no consumer.

The revision is narrower: only a **promoted** learning crosses, as a human-validated
`MitigationRecord`. But writing even that means **AI-DE writing content into the user's repository**,
where today it reads repositories and writes only its own store.

## Why they are one decision

> **How far outside its own store does this product reach?**

Scoring an agent's episode means accepting evidence the product did not observe. Emitting a learning
means writing where the product does not own. Both extend the boundary; both are currently at "the
product observes, and keeps what it observed".

Deciding them separately is how a product ends up with an inconsistent answer to a question nobody
noticed it was answering twice.

## What is already true, so the decision is not urgent

Everything else works. Both tracks are green — 385 App, 1,526 Core, 19 gates — with the absence
**pinned by tests that fail the day it closes**, and a remark saying those tests should be rewritten
to assert the standing rather than deleted. An absence nobody has pinned is indistinguishable from
one nobody has noticed, and this repository spent two days learning that shape.

---

# Answered — 2026-09-02, by the owner

Both, plus a third that reframes the question rather than settling it.

## 1 · Scoring: the product scores as an **observer**

> "scoring an agent's episode is essential as an observer … and the score must sit in the product
> but tied to the workspace because the way an agent works will be a product of the repo specific
> directives etc"

Of the three answers above, **the watcher derives** — not agent self-report, and not audit-only. The
scorecard lives in the product's own store, **keyed to the workspace**, because how an agent behaves
is partly a function of the repository it is working in: its directives, its conventions, its gates.
A score detached from that context would be comparing agents across conditions that are not alike,
which is the same error the leaderboard already refuses to make across task classes.

**Still open, and now an implementation question rather than a product-identity one:** *which*
observations become deterministic signals. Spans and board posts are observations but neither is
evidence of outcome. That is `ai-de-a7`'s to answer in the scoring slice, and it is a smaller
question than it was an hour ago — it no longer decides what the product is.

## 2 · Daydream is repo-local, and the product writes it there

> "day dreaming is specific to a repo and should be maintained in the repo … i.e the product should
> write to the repo … dreaming is the act of learning across repos"

This is a **cleaner split than the seam design had**, and it renames the halves correctly:

| | Scope | Lives | Written by |
|---|---|---|---|
| **Daydream** | One repository | **In the repository** | The product |
| **Dream** | Across repositories | The pack's fleet store | The offline pass, human-gated |

A lesson about *this* repository belongs *with* that repository — the same reason
`defect-classes.md` is committed rather than kept in a tool's private store. It survives a machine
change, travels with a clone, and is reviewable in a pull request.

So `design-watcher-daydream-dream-seam` needs revising again: its emit half was scoped to "only a
promoted learning crosses, as a MitigationRecord". That was the right answer to the question *may
the product write into a repository*, which has now been answered differently — and a **larger**
answer, so the narrow seam is no longer the constraint it was built around.

## 3 · The boundary rule — provenance, not permission

> "to the user the product and the agents are one holistic experience and the agents are writing
> into the repo all the time … the difference is that we want to make sure that things generated by
> the agents are always updated by the agents … but things generated by the product can also be
> written to a repo / workspace"

The question this note asked was *how far outside its own store does the product reach*. The answer
is that the boundary was drawn in the wrong place. It is not **who may write** — the product and its
agents are one experience to the user, and agents write into the repository continuously.

**It is who maintains what they wrote.**

- Generated by an **agent** → updated by an **agent**. The product must not silently rewrite it.
- Generated by the **product** → the product owns it, and may write it into the repository.

Which makes the real obligation **marking provenance**, so nobody has to guess. A product-written
artifact must say so, in itself, where an agent or a human editing it will see it — the same reason
a derived view carries a "regenerate, do not hand-edit" header. That is now the constraint, and it
is a smaller and more tractable one than the boundary we thought we were asking about.

## What this unblocks

| Was blocked on | Now |
|---|---|
| Scoring an agent's episode (`C-scoring`) | Unblocked — observer-derived, workspace-keyed. `ai-de-a7`. |
| Daydream's outbound half (`D5`) | Unblocked, and **redirected**: repo-local persistence rather than a narrow seam. |
| C3–C5 | Never blocked on this; held only because the break mattered more. |
