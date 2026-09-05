---
id: "note-20260902-two-decisions-the-loop-waits-on"
title: "One question the agent loop waits on, asked twice"
type: decision-note
status: accepted
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
| the **agent supplies** its own deterministic signals | a standing rests on self-report, and ADR-0019 advisory-evaluator-calibration's entire anti-Goodhart concern arrives at once |
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

---

# Built — 2026-09-02, both halves

The question this note recorded is answered and the work it blocked has landed. What follows is what
the answer turned into, and the one part of it that only became visible while building.

## The loop closes

`ClosedEpisodeScoring.Run` scores every closed episode of a **registered** session that has no
scorecard, on the watcher tick. `TheAgentCollaborationCircuitTests` now walks the whole chain
headless — register → declare → close → scored → standing on disk — and the two assertions that
pinned the absence went red exactly as they were written to, then became assertions about the
standing rather than about its absence.

| Link | State |
|---|---|
| agent launches | works |
| registers through the contract log | works |
| observed | works |
| board read and write | works |
| **episode scored** | **works — on the tick, not at close** |
| **standing delivered to the agent** | **works, and now reachable** |

**On the tick rather than in `CloseEpisode`,** because closing an episode is a declaration and
scoring it is a judgement; coupling them would make the agent's own close line the thing that
produced its score, and the two would then fail together. **Registered sessions only,** which is what
keeps the two scoring producers disjoint: an audit-imported episode has no `SessionRecord`, so the
tick can never re-score one under a different task class.

## What the agent actually receives, and why that is the honest answer

**Not Scored, with its reason.** A contract-declared episode carries no Proof Pack, so there is no
verification path to observe; `DeterministicSignalsDeriver` was already honest about that — acceptance
stays null rather than "met", requirements stay 0 so those dimensions render Not-Recorded — and the
wiring had to preserve that rather than invent it.

It is emphatically **not a low score**. A derived-signals path returning 0 for "nothing observed"
would be a statement about the agent where only a statement about the evidence is warranted, and it
would be indistinguishable from a real failure. That is now DC-098.

## Workspace-keying turned out to be about the repository, not the checkout

The owner's answer was that the score sits in the product's store but is **tied to the workspace**,
because how an agent works is partly a product of the repository's directives. Making that real
surfaced a distinction the codebase had no word for.

`RepositoryIdentity.CanonicalPath` is the obvious key and it is already correct on the live path —
`WorkbenchShell.ResolveGitFacts` takes `--git-common-dir`'s **parent**, so a linked worktree and its
primary checkout both answer with the primary path. Measured in this repository's own two trees
rather than argued from the type's documentation, which describes something narrower (DC-094).

Keying on the **checkout** instead would have failed twice, and the second failure is the instructive
one: splitting one repository's cohort across its worktrees shrinks every leaderboard cell until it
falls under the minimum and renders **Not Comparable** — which is the de-anonymisation guard. A
privacy protection firing correctly for a reason that is not privacy, with the surface looking right
throughout.

`WorkspaceKey.From` is now the single place that decides, it delegates to
`RepositoryIdentity.Canonicalise` rather than reimplementing it, and
`TheWorkspaceKeyIsTheRepositoryTests` builds a real linked worktree and asserts the two resolve to
one repository.

**What is still not enforced:** nothing checks that a registrant's `repo.path` *is* repository-scoped.
Our shell sends the repository; an externally-registering agent composes its own attributes and could
send its worktree, reintroducing the split silently. That is DC-092's shape, it is a real gap, and it
is deliberately **not** closed here — normalising versus rejecting a worktree-shaped `repo.path` is a
contract decision, not a wiring one.

## The segment is one type, for a reason worth keeping

`ScoreSegment(WorkspaceKey?, TaskClass, SchemaVersion)`. `TaskClass` and `SchemaVersion` already sat
adjacent in `ScoredEpisode`, in `Leaderboard`, and in the standing's trend filter; a third string of
the same type would have made a reordered triple compile and pass, in the values that reach a surface
and get read as meaning something. As one type the two filter predicates collapse into one equality,
and every call site broke at once when it changed — which is what a good breaking change does.

The **comparability rule lives on the segment**, not in the composer, because two consumers already
ask it. An incomparable segment is scored and delivered and ranks nowhere, and `AgentStanding` gained
`NotComparableReason` so "no rank" never arrives without its cause.

The contract carries **no task class** — only a goal and a done-condition — so an agent's segment is
`Unclassified`. Pooling every undeclared episode would compare a spike against a refactor and read
the difference as an agent improving, which is the error segmentation exists to prevent.

## Daydream: the predicted problem was the opposite of the real one

The concern raised before wiring `DaydreamRecorder` was that every agent episode would carry the
**same** Not-Scored signature, so one useless pattern would dominate the recurrence report.

Measured instead of assumed, and refuted: an agent's Not-Scored episode trips **no floor** and records
**no shortfall**, so its signature is *unremarkable* and the recorder declines it. The reason is the
same honesty that produces the verdict — nothing was observed, so no floor can trip; every dimension
is Not-Recorded, and a Not-Recorded dimension has a null rubric and cannot fall short.

So the risk is not that Daydream drowns; it is that it hears nothing from the agent path at all.
`WhatDaydreamSeesInAnAgentEpisodeTests` records that, and its assertions are written to fail the day
an agent episode carries evidence — that red is the signal the signature has become informative.

The call site went into `ScoringService.ScoreAndRecord` rather than the tick pass, because that is the
single place a `ScoredEpisode` comes into existence and both producers pass through it. The producer
feeding the record today is **audit-import**, which reads committed Proof Packs and does trip floors.

## Store

`watcher.db` v3 → v4: `workspace` nullable, expand-only. A pre-v4 row reads back with an **absent**
workspace and is excluded from cells rather than backfilled with a path nobody observed — a backfill
never guesses. SQLite has no conditional `ADD COLUMN`, so the migration runner's idempotency claim,
previously true only because every prior migration happened to be `CREATE TABLE IF NOT EXISTS`, is
now enforced by a column guard (DC-096).
