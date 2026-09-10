---
id: design-session-profiler
title: "Design — /session-profiler"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [session-profiler, telemetry, performance, efficiency, adherence, ai-forward-pack]
links:
  - { to: profile-sp-0001, rel: documents }
  - { to: profile-conductor-phase1, rel: documents }
review-by: 2027-03-09
review-suggested: []
summary: >-
  The measurement design behind /session-profiler: read each harness's own on-disk session store
  (never reason about behavior it did not record), classify drift/tangent/ceremony per flagged turn
  against the transcript, and converge on a findings table (with severity and confidence) plus a
  fixes table (each row naming the pack surface and the control that fails on recurrence). This node
  did not exist in this repo before the first /session-profiler run (docs/profiles/sp-0001/); it is
  created here so that run's own frontmatter links resolve.
---

# Design — `/session-profiler`

This is the design node the skill's own documentation step (`.claude/skills/session-profiler/SKILL.md`,
"Documentation & discoverability") expects every generated profile to link `relates-to` — it did not
exist in this repository before the first profiler run created `docs/profiles/sp-0001/profile.md`,
which left the link dangling. Written now, minimally, so the graph resolves; the full authority for
the skill's behavior remains its own `SKILL.md` and the packs it composes
(`instrumentation-over-inference.md`, `communication-and-task-discipline.md`,
`execution-graph-optimization.md`, `session-worktree-discipline.md`, `continuous-improvement.md`),
not this stub.

## What it measures, and what it refuses to

`session-profile.py` reads each harness's own on-disk session store (`~/.claude`, `~/.copilot`) —
tokens, requests, cache share, sub-agent delegation, tool calls, images, hook timing — and never
infers a number a store did not record. A missing measurement renders `"not recorded"`, never a
plausible substitute (IO8). Text-pattern heuristics (goal-state presence, "council above tier," the
model-family comparison) are explicitly the *Inferred* half of the output and are required to be
confirmed or struck against the actual transcript before they enter a findings table as evidence —
this is the Rigor Protocol's Stage 4 DISCONFIRM step, specialized to telemetry.

## Known limitation, found by the first two runs

Neither run so far (`sp-0001`, the whole-repo pass; `conductor-phase1`, scoped to the Conductor
Phase-1 work) could join the harness's own session-id namespace (a UUID) against the audit log's
human-chosen `session` label for a delegated node — there is no shared key beyond timestamps. A
harness-level goal-state/tier heuristic that scans only the parent turn's visible reply will
therefore under-detect a goal state that was in fact written into a **dispatched sub-agent's own
prompt** (see `docs/profiles/conductor-phase1.md` §3, findings SP-06/SP-09 disconfirmed for that
reason). Closing this gap needs either a shared session identifier written by both stores, or a
detector that also reads the linked audit entry's `goal`/`done_when`/`tier` fields — neither is built
yet.

## Handoff

`/dream` mines `docs/profiles/` into control-upgrade proposals; the fixes proposed against this gap
in `docs/profiles/conductor-phase1.md` are candidates for that pass, not yet built controls.
