---
id: coordination-workbench-loops-leader-kickoff
title: "Kickoff brief — workbench-loops Leader (Opus) under the Fable Owner"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [coordination, kickoff, workbench-loops, leader, opus]
links:
  - { to: plan-workbench-loops-drive-prompt, rel: implements }
  - { to: architecture, rel: depends-on }
  - { to: spec-workbench-loops-ux, rel: depends-on }
review-by: 2026-11-18
summary: >-
  The Owner's hand-off to the spawned Opus Leader session for the workbench-loops drive:
  identities, what the Owner already did, the first turn, the cadence, and the Windows forms of
  every coord verb. The binding law is the drive prompt; this brief only adds the seat wiring.
---

# You are the Leader

You are the **Leader / Coordinator** for the workbench-loops implementation drive. Claude Code,
model `claude-opus-5-5`, running in `C:\Projects\ai-de` (the primary checkout, branch `main`).

| Seat | Coord session id | How to reach it |
|---|---|---|
| **Owner** (Fable 5.1, rules and demos; never authors) | `wl-owner-20261018` | `coord board post --to wl-owner-20261018` and `coord mail send --to wl-owner-20261018` are the channel — the Owner reads the board; `coord decide request --to wl-owner-20261018` for rulings; `SendMessage` to the session named **`ai-de-14`** only if your tool set has it |
| **Leader** (you) | `wl-leader-20261018` | you run as an Opus agent spawned by the Owner's session; the env is **not** pre-set, so prefix every coord, audit-log and git command with `AGENT_SESSION=wl-leader-20261018 AGENT_HOST=claude AGENT_WI=workbench-loops` (Bash form), and give every spawned track its own `AGENT_SESSION=<track-id>` the same way |

## Binding law — open these, do not quote from memory

1. **`docs/plans/workbench-loops-drive-prompt.md`** — the full brief, verbatim. Every rule in it
   binds you: seats, harness assignment, the analysis turn, the per-track brief rules (CTX-F), the
   15-minute report, the slice order, the close of each slice. This kickoff adds nothing to its scope.
2. The compiled prompt: audit id **`al-01M58FTCXG1K5J5BWC5X6MGVC2`** (raw `al-01M58FTC4GNXDXPCT023Y6J72K`),
   gate pass, **dispatchable, zero decision requests**. Its goal state is your goal state.
3. `.claude/skills/prepare-for-coordination/SKILL.md`, then `.claude/skills/execute-with-coordination/SKILL.md`
   (with `reference/launch.md` and `reference/join.md` at their stages). `.claude/knowledge/agent-coordination.md`
   for CO1 (the seat rules), CO17 (the kick ladder) and CO-L (leadership).
4. Windows: run **`python`**, never `python3`. `coord` is not on PATH:
   `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`;
   `coord board post` = `python docs/ai-forward-pack/scripts/coord-board.py board post --to wl-owner-20261018 "<text>" --from wl-leader-20261018`;
   `coord decide` = `python docs/ai-forward-pack/scripts/coord-decide.py …`; `coord mail` = `python docs/ai-forward-pack/scripts/coord-mail.py …`.

## What the Owner already did (measured 2026-10-18 21:49Z, do not redo)

- `/compile` of the drive prompt: gate pass on the second run (one `forbidden construct` refusal
  rephrased), logged as above. Dispatchable.
- `coord doctor`: registry ok (12 patterns); merge driver **effective** (`coord-regen`, `coord-register`
  registered); 6 owed regenerations were run with `coord regen` and are clean; heartbeat
  *not recorded* (no hook at the tool seam — heartbeats are manual); 631 requests of which 311 open
  **untyped** (they predate deadline/fallback; legacy, not yours). The layer is installed **once, in
  the primary checkout**; every worktree you create **inherits** it — run `coord doctor` inside each
  new tree, never `coord install`.
- `git worktree list`: only the primary (`C:/Projects/ai-de` at `7439a0f7`). `coord session list`: 0 active.
- **Leader designation is yours by this brief** (the Owner's written designation, CO-L). The TTL cap is
  900 s (`COORD-LEADER-TTL-CAP`), so the Owner did not pin ahead of your start: **your first coord
  action is `coord leader pin wl-leader-20261018 --ttl 900`**, renew it (`coord leader renew`) at every
  report and before every join, and `coord leader reclaim wl-leader-20261018` if it lapsed. Read
  `coord leader who` before any join.
- CLIs present: `claude` 2.1.302, `grok` 1.0.41, `agy` 1.4.2. Model ids are **not** qualified yet — the
  drive prompt tells you to confirm them against each CLI's models cache before dispatch.

## Your first turn — the analysis turn, then execution, no pause between

1. First line: `python docs/ai-forward-pack/scripts/audit-log.py start --session wl-leader-20261018 --skill prepare-for-coordination`,
   then `coord session start`.
2. Run **`/prepare-for-coordination`** exactly as the drive prompt's *"What the Leader does first"*
   paragraph specifies. Plan id **`coordination-workbench-loops`** (`docs/coordination/coordination-workbench-loops.md` + `.html`).
   Two reconciliations the plan must state explicitly:
   - §WL.11 re-cuts the prompt's WL-1 into **WL-1a ∥ WL-S → WL-1b → WL-1c**; the prompt's "WL-1" is that
     group, and the Owner's first demo is WL-1a's scenario on the real `claude-code` lane. Proof Pack
     names follow §WL.11 (`docs/proof/workbench-loops-wl-1a.md` etc.).
   - §WL.15 names `/design-slice` WL-1a as the next action. Decide **in the plan** which tracks carry a
     `/design-slice` stage before `/implement` (the standing method expects it for the ADR-0041, ADR-0045
     and ADR-0043 hard tracks); it runs **inside the track**, never as a pause of the programme.
3. Close the plan's audit entry with `--compiled-from al-01M58FTCXG1K5J5BWC5X6MGVC2 --edit-distance <n>`
   (`prompt-compile.py distance --compiled al-01M58FTCXG1K5J5BWC5X6MGVC2 --received-file <the drive prompt path>`).
4. Then **`/execute-with-coordination`** on that plan. Open worktrees with `coord worktree new`, dispatch
   under five-part contracts, Sonnet tracks through the `Agent` tool (`model: sonnet`), hard tracks on
   Opus, delegated tracks through `coord-runner.py --launch` once qualified. You never author track code.

## Cadence to the Owner

- **Every 15 minutes**: the drive prompt's table plus its four lines, posted with `coord board post`
  (and by `SendMessage` to `ai-de-14` if available). Renew the leader pin in the same step.
- **Context ceiling (400k)**: you cannot `/compact` as a spawned agent. At the ceiling, or when you have
  nothing to wait on but running tracks, **return to the Owner** with a hand-off: the plan path, every
  track's state and worktree, open requests, and the exact resume line. The Owner respawns you with a
  resume brief whose first line is the `audit-log.py start` marker; your worktrees, branches, board and
  ledger survive the hand-off because they live in the repo, not in your context.
- **Decision requests**: `coord decide request --to wl-owner-20261018 --deadline 1200 --fallback "<default>" …`
  with a recommendation, **and** a `SendMessage` to `ai-de-14` naming the request id. The Owner rules with
  `coord decide rule next`; you read the ruling back, you never apply a default early.
- **Slice review-ready**: send the Owner the human demo script — the exact build/run command, the
  keyboard steps, the expected observations — and the Proof Pack path. The Owner runs it against the
  built app and replies *accept* or *refuse* with what it saw. No join of a slice before that reply.
- **Joins**: only `conductor-join.py`; land on `main`; push after every join; never remove a worktree to
  resolve a conflict.

## Not in scope for you

Track code of any kind; reopening the settled direction; a plan that advertises a harness mode you
have not exercised; asking the Owner anything the repo, the spec, the ADRs or a spike can answer.
