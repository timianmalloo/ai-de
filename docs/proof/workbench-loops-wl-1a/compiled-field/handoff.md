---
id: proof-workbench-loops-wl-1a-compiled-field-handoff
title: "Hand-off — WL-1a T28: the compiled field after a submit"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff, composer, ruling-96, ruling-181]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T28 after node 3: a queued send shows its turn headed 'Compiled prompt — queued as b{n}', display-only, until the next
  keystroke (red then green); a second press right after it queues and sends nothing, and after the drain sends nothing
  (planted-leak mutation red M3); the class gate ComposerContentOraclesDriveTheGestureTests refuses a composer content
  oracle that skips the gesture (red on a plant, green on the tree). Next is T30 (Ruling 180, the prefill).
---

# Hand-off — T28 `wl1a-compiled-field`, after node 4

**Done (node 4).** A queued send's field switches from `queued as b{n}` to `sent as b{n}` when the queue drains and nothing was typed (red `1a5150a0`, green `ed2a655e`; mutation reds for B and C in `n4-*-mutation.txt`). Tests: `TheCompiledFieldAfterTheQueueDrainsTests` (4 facts). Detail in README section 7.

**Red, by design:** none. App suite: only the known `SolutionTreeChordTests` probe refusal (Ruling 155 (3)).

**Open:** the heading would stay queued if b2 completes before any snapshot shows it in flight (not observed).

**Next step:** T30 (Ruling 180, the prefill) on `ComposerSurface.cs`.
