---
id: proof-send-while-running
title: "Proof Pack — Ruling 95: a Send while a turn runs offers Wait (one queued turn) or Parallel (a derived sibling session); Ruling 77(b) reversed"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, composer, sessions, thread, ruling-95, ruling-77, ruling-78, ruling-83, ruling-99, red-first, measurement]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: proof-composer-compiled-prompt-and-console-rows, rel: refines }
  - { to: proof-conductor-front-door, rel: refines }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Lane composer-r95 on main c831113e. Condition 1 measured first: two session documents on one
  workspace each sent "reply with the single word ok" 59 ms apart through the product's own
  composition root against the live claude-code adapter; two engine processes (pids 34340, 57860)
  were alive together in every census sample, node rose 41 → 43, both turns answered in 3.5 s / 3.4 s
  — the run host does not serialise them, so Parallel ships. Wait: one queued turn per session,
  compiled at queue time, drained only after Completed/Answered, waiting with Send now after Stop or
  Failed; Cancel returns its words. Red-first each.
---

# Proof Pack — Ruling 95 (lane `composer-r95`)

Base `c831113e` · branch `lane/composer-r95` · coord session `composer-r95`.

## Goal state (written before the first substantive tool call)

- **Goal:** land Ruling 95 — on Send while a turn runs or waits, the composer's status line offers
  *Wait — send after b1* (one queued turn) or *Start a parallel session* (a derived sibling session).
- **Done when:** condition 1 measured and recorded here before any Parallel code; Wait shipped
  red-first (queued · drained · stopped-with-queued · cancel); the drain never sends after Stop/Failed
  (test); the queued turn's compile spend on its own outcome line (Ruling 78); Parallel shipped or
  refused with the measured reason; DESIGN.md's copy and §B5's STA rows amended; gates green; audit
  entry; `coord session end`.
- **Not in scope:** the account picker (Ruling 105), the New Session sheet
  (`NewSessionSheetViewModel` — the Sessions lane's), the Explore reader, the store.
- **Tier:** T1 · **fan-out:** 0.

## Condition 1 — the measurement, first

**Harness:** `AiDe.App.ComposerProbe --parallel-turns` (`tests/AiDe.App.ComposerProbe/Program.ParallelTurns.cs`).
Two `SessionDocumentSurface`s on one temp workspace (a `git init`'d folder, one empty commit), each
composer **harness-wired** — the probe calls `Composer.Configure` itself with a `ComposerSendContext`
built field-for-field as `SessionComposerBinder.Bind` builds one, from the machine's own
`~/.aide/providers.json` (`claude-code · claude-sonnet-5 · max`, health `quota-degraded`, which still
binds). Everything from `Send()` onward is **product-wired**: `ComposerSendGate.Send` →
`SessionDocumentSurface.Launch` → `GovernedRunHost.RunAsync` (the one composition root). The F5 pack's
"harness-wired vs product-wired" caveat applies to the binding only. Prompt: *"reply with the single
word ok"* (read-only: no lease, no worktree, no episode). Log: `%TEMP%\aide-parallel-turns-1.log`.

| Reading | Session A (`…-91f04456`) | Session B (`…-ac97e205`) |
|---|---|---|
| `Send()` at (UTC) | 17:56:45.2795 | 17:56:45.3389 (**+59 ms**) |
| Turn accepted at | 17:56:45.2927 | 17:56:45.3436 |
| `lane.session-new` row (`ts`) | 17:56:46.9315 (`run-28e41e41`, `lane-69e093ee`) | 17:56:46.8112 (`run-b1de0e45`, `lane-5daba95f`) |
| `session/new` params | `{"cwd":"<temp>","mcpServers":[],"_meta":{"claudeCode":{"options":{"disallowedTools":[…31 tools…],"thinking":{"type":"adaptive","display":"summarized"}}}}}` | byte-identical to A |
| ACP session id | `034c3e4a-c649-4e4f-b1a0-2acb3399ee80` | `2cd044ad-cd31-4a50-8ef8-fa6fe5fbe773` |
| Engine pid (`node`) | **34340** | **57860** |
| `prompt stopReason` | `end_turn` | `end_turn` |
| Outcome line | **answered · 6 tokens · 3 s · 11 events** | **answered · 6 tokens · 3 s · 12 events** |
| Concluded at | +3.5 s (≈ 17:56:48.79) | +3.4 s (≈ 17:56:48.74) |
| Run outcome / failure | `Completed` / none | `Completed` / none |

**Process census** (`Process.GetProcessesByName("node")` every 250 ms on the dispatcher while either
run was open): 14 samples from 17:56:45.355 to 17:56:48.698; **both engine pids alive in all 14**;
node baseline **41**, peak **43**, delta **+2**. Both `lane.session-new` rows precede both outcomes by
~1.9 s; B's `session/new` was recorded 120 ms *before* A's although A was sent first — the two lanes
are interleaved, not ordered.

**Decision:** the run host does **not** serialise two sessions on one workspace — two `session/new`
frames, two engine processes overlapping in time, both outcome lines `answered` within 0.1 s of each
other. **Parallel ships.** Ruling 95's Inferred clause (*two engine processes for one workspace run
concurrently in the App*) is now **Verified** for the read-only shape on this machine.

**Residual (named, not modelled):** the measurement is of two *read-only* turns. A write-shaped turn
cuts a worktree per lane (`WorktreeProvisioner`) — two lanes on one repository would cut two worktrees;
not measured here (the operator's consent covered one short read-only prompt per session), and not a
condition of the ruling.

## Claims

_(filled as each condition lands — red first, then green)_

## Findings not changed (placeholder ids — the conductor allocates)

_(none yet)_

## Gates and counts

_(filled at close)_
