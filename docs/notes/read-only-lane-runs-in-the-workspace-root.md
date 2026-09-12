---
id: note-read-only-lane-runs-in-the-workspace-root
title: "A read-only turn runs in the workspace root, opens no episode and is not scored — Ruling 73's Inferred half decided"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, agent-plane, acp, ruling-73, read-only, conversation-lane, cv-0, security]
links:
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: adr-0035-compile-session-binding-and-pin, rel: relates-to }
  - { to: proof-read-only-turn, rel: relates-to }
  - { to: proof-lane-pin-ruling-71, rel: relates-to }
  - { to: coordination-addendum-cd, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Ruling 73 left one point Inferred — whether a read-only turn runs in the workspace itself or in a
  throwaway worktree. CV-0 decides: the workspace root, no worktree, no episode, no score. The pin is
  what makes the tree safe, a throwaway tree is exactly what the operator would have to clean up,
  the REPL reads the operator's live working state, and a Message has no done-condition to judge.
---

# A read-only turn runs in the workspace root

- **Kind:** decision (below ADR weight; the ADR-0035 precedent applies)
- **Confidence:** Verified for the code paths cited; **Inferred** where marked
- **Made during:** `/implement` of track CV-0 (session `cv-0`, `lane/conversation-cv0`)

## The question

Ruling 73 (`note-addendum-c-council-rulings`) admits a read-only turn — every write-capable tool
disallowed, no lease — and marks one thing **Inferred**: *"whether a read-only turn runs in the
workspace itself or in a throwaway worktree — the architecture decides (P1 slice), with the
constraint that it cuts nothing the operator must clean up."* A second question follows from it:
does a read-only turn open a watcher episode and get scored, as a governed run does?

## The decision

1. **The workspace root, no worktree.** `GovernedRunHost.RunReadOnlyAsync` opens the ACP session
   with `cwd = request.RepositoryRoot` through `OpenReadOnlySessionAsync` and never calls the
   provisioner. Reasons, in order of weight:
   - **The constraint itself.** A throwaway worktree is a directory and a branch the operator must
     later remove — the one thing Ruling 73 forbids the turn to leave behind. `coord worktree
     cleanup` is fail-safe and opt-in (WT12), so a tree the turn cut would sit until somebody ran
     it.
   - **The pin makes the tree safe by construction.** What bounds where a lane may write is not
     its `cwd` but its tools; a lane that holds no write-capable tool is read-only wherever it is
     rooted. Rooting it elsewhere would add nothing the pin does not already give and would hide
     the state the operator is asking about.
   - **The REPL reads the live state.** A worktree shows a committed snapshot; the operator's
     question is usually about the uncommitted work in front of them (Ruling 73's *"standard REPL
     loop between the prompt side and the console"*).
   - **The precedent.** ADR-0035 roots the compile session in the repository root for the same
     reason and adds one more: outside the repository the constitution (`CLAUDE.md`, settings) does
     not load, and a lane that answers without the repository's own rules is a worse REPL.
2. **No episode, no score.** A governed episode is work opened from a goal block and judged at
   close against its done-condition (`GovernedLaneSource.Open` validates the block;
   `LaneScoring.ScoreGoverned` scores the outcome by task class). A Message carries no block and
   has nothing to judge; a scopeless goal block cannot produce the artefacts a verdict reads. Opening
   an episode for one shape and not the other would make the read-only turn two things. So
   `RunReadOnlyAsync` opens no `WatcherHost`, no `GovernedLaneSource`, no `LeaseMonitor`, and the
   result reads `Scored: false`, `SegmentIsComparable: false`, `IncomparableReason: "a read-only
   turn opens no episode and is not scored"`, `SessionId`/`EpisodeId`/`WorktreeBranch`/`TaskClass`
   as *not recorded* — never a plausible substitute (IO12).
3. **What is still measured, on the normal path.** The `session/new` frame (`lane.session-new` in
   the workbench log and on the report), `EventsObserved`, `EventKinds`, the normalization latencies,
   the engine pid and exit, the environment findings and every diagnostic line — the same numbers
   the write path takes. The Console receives every event through the sink.

## Consequences

- **Positive:** nothing to clean up; the operator's live tree is what the model reads; the write
  path's composition is untouched (its statements did not move); one root per send either way.
- **Negative / accepted:** a read-only turn is invisible to the Loomkeeper's fleet map and the
  leaderboard (no session, no episode). If the operator wants read-only turns counted, the named
  upgrade is a `turn.read-only` event on the run's own stream — a next step, not this slice.
- **Inferred, and the gap that forces it:** the SDK's `disallowedTools` removes the tool from the
  model's context (`sdk.d.ts:1465-1469`); that a sub-agent cannot regain one is not readable from
  the vendored source (the CLI is a binary), so `Agent`/`Task`/`Workflow`/`RemoteTrigger` are
  pinned off rather than relied on. The attended run in `proof-read-only-turn` is what promotes the
  whole pin from source to wire.

## Falsifier

A read-only turn observed writing — a `Write`/`Edit` frame in the Console, a `permission reject:
READ-ONLY TURN` line in the diagnostics, or a dirty `git status` after the turn — falsifies this
note's premise (2) and stops at the human (Ruling 73 condition 2); the pin is re-spiked.
