---
id: "note-20260902-the-repo-path-contract"
title: "What happens when a registrant's repo.path names a worktree"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "phase-3"
tags: [decision-note, watcher, registration, collaboration, message-board, scoring]
links:
  - { to: note-20260902-two-decisions-the-loop-waits-on, rel: relates-to }
  - { to: design-watcher-daydream-dream-seam, rel: relates-to }
review-by: 2027-03-02
review-suggested: []
summary: >-
  A registrant supplies its own repo.path, and nothing checks that it names a repository rather than
  a linked worktree. Two sessions argued reject-versus-normalise and both were wrong; the resolution
  is three cases, not two. Recorded because the argument turned on two premises that were checkable
  in seconds and neither side had checked its own.
---

# What happens when a registrant's `repo.path` names a worktree

Settled between two sessions on 2026-09-02, in writing here because a decision reached in a channel
is one the next session cannot read.

## The gap

`OtelSpanMapper.MapRegistration` builds `RepositoryIdentity` from whatever `repo.path` a registrant
sends. AI-DE's own shell sends the **repository** — `ResolveGitFacts` takes `--git-common-dir`'s
parent and carries the worktree separately — but an externally-registering agent composes its own
attributes and may send its **checkout**. Nothing checks. DC-092's shape: a value carrying an
invariant no code enforces.

## Why it matters more than a leaderboard cell

`Repository.CanonicalPath` is not only the scoring key. It is the grouping key in
`FleetAggregator`, the registration guard, and — decisively — the **message board partition**:
`MessageBoard` writes `RepositoryKey` from the binding, and `RequireParent` (`MessageBoard.cs:151`)
**refuses** a reply whose parent sits under a different key, as a cross-repository thread with
`InvalidBinding`.

So an agent that sends its worktree does not merely rank oddly:

- it posts to a partition no one else is on, and
- every reply it makes to anyone else's thread is **rejected**, with an error that says
  *cross-repository* — literally true, and completely misleading to an agent that knows it is in the
  same repository. It reads as the product being broken (DC-078's shape, pointed at an agent).

That is the collaboration loop failing, in the product whose collaboration loop was closed the same
day.

## The two positions, and why both were wrong

**Reject a worktree-shaped `repo.path`** (argued from: normalising means the product silently
rewrites a registrant's claim about itself, and gets it right only by resolving a path on *our*
filesystem — fine for a local agent, a guess for anything else).

**Normalise it** (argued from: rejection removes the agent from observation entirely, taking the
board down with it, to protect a leaderboard cell).

The rejection argument dies on a premise that was checkable in seconds and was not checked:

```
repository root   .git is a DIRECTORY
linked worktree   .git is a FILE containing "gitdir: …/.git/worktrees/<name>"
```

Nothing in the *spelling* of an absolute path distinguishes them. **Detection and correction are the
same read.** So rejection fires only where normalisation would have worked, and is silent in exactly
the case invoked to argue for it — a path we cannot resolve locally, where the registration looks
perfect and the cohort splits anyway.

## The resolution — three cases, because there are three states

| State | Outcome |
|---|---|
| Resolvable, and a repository root | Accept. Today's path. |
| Resolvable, and a **linked worktree** | Key on the repository **and say so** — a registration diagnostic carrying the value sent, the value used, and why. |
| **Not resolvable** | We cannot tell, so we do not pretend to. `WorkspaceKey` null, segment not comparable, the standing says why. |

Case 3 is where the "guess for anything else" worry actually lives, and it is answered by a **stated
absence**, not a rejection — a shape this codebase already has.

Case 2 is normalisation with the silence removed, and the silence was the whole objection. Reading
`gitdir:` out of the registrant's own checkout is *more* evidence than the registrant supplied about
itself; calling that a guess was wrong.

The check belongs at **registration** — trust is decided there, which is why an `episode-open` for
an unregistered session is dropped rather than promoted into a session. Only the verdict was in
dispute, never the location.

## Case 2 needs a channel that does not exist yet

A diagnostic nobody reads makes case 2 into case 1 with extra steps.

The only thing written back to a registrant today is the standing file, and it **cannot carry this**:

- it appears at `<AIDE_CONTRACT_LOG>/standing/<session>.json` only once there is a **scored
  episode**, and a registration correction must be readable **before the agent's first episode** —
  otherwise the agent works a whole episode on a board nobody is on before anything tells it;
- its absence is already load-bearing. `session-collaboration.instructions.md` documents *no file at
  all* as meaning **you have no scored episode yet**, so putting a registration diagnostic there
  would give that absence a second meaning.

So it needs a sibling, written at registration:

```
<AIDE_CONTRACT_LOG>/registration/<session>.json
{ "generated-by": "ai-de/<component>",
  "repositorySent": "…", "repositoryUsed": "…",
  "reason": "linked worktree resolved to its repository" }
```

Three properties, each already agreed elsewhere the same day:

- **Rewritten, not appended** — machine-written directory, machine-read document, the product alone
  reads it back. (The rule: *rewrite what the product alone reads; append to, or leave alone, what a
  person may edit.*)
- **Carries `generated-by`** — the provenance marker's third instance, and the first where a reader
  might genuinely mistake the file for something they should edit.
- **Invisible to the pump** — the coordination pump globs `*.jsonl` with no `SearchOption`, so a
  `.json` in a subdirectory is not ingested. The same placement fact that put the standing file where
  it is.

## What is deliberately not decided

Whether a wrong `repo.path` should ever cause a **refusal** is a product decision with a
user-visible failure mode — an agent made invisible — and nothing above requires one. It goes to the
owner if anyone wants it, not to two sessions agreeing it between themselves.

## What this argument cost, and the observation worth keeping

Both sessions argued from an unchecked premise, and each was refuted by the other **running the
check rather than reading the summary** — three times in one evening, across a projection, a doc
comment, and a vacuously-passing assertion.

One instance is not yet a defect class, and is recorded here rather than in the register so that it
earns an entry only if it recurs: **the board-partition fact was verified earlier in the same
session, by the session that then argued against it.** `Repository.CanonicalPath` had been read and
approved as the board key hours before it was used to argue the opposite. The failure was not
missing evidence; it was evidence gathered and not consulted when it became decisive. If that shape
appears again it should be registered, with the diagnostic question: *have I already looked at this
field today, and for what?*
