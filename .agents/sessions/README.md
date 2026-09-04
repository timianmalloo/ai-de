# Liveness — who is running right now

**This directory is not a register of ownership. It never states a path table.**

The single register is **`docs/collaboration/session-contracts.md`** — tracked, reviewable, on
`main` since `6db9b6f` (2026-08-29), accepted by the Design session at `41e331f`, and appended to by
every session since. **§2 is the sole authority on file ownership.** If a file in this directory
ever disagrees with §2, **§2 wins and a copy has drifted.**

This directory exists for the one thing a tracked document cannot do: say who is running **right
now**, in which worktree, on what, and what they are blocked on — without needing a commit to
become visible. It is untracked and machine-local, and `coord-core.py` resolves it against the
primary checkout from any worktree (`repo_root()`), so every session sees the same files instantly.

## What a file here says

Agent name · session id · worktree · branch · status · what I am doing right now · what I am
waiting on. That is all. Ownership questions go to §2.

## The other half, which is enforced

`coord claim` / `coord check` over `.agents/log/*.jsonl` — a TTL'd lease (300 s default) that the
PreToolUse hook and `coord precommit` actually refuse on. Leases are for the minutes you are editing
a file, never for expressing an area. A refusal is a **defect signal**: if two sessions collide on a
lease, the plan is wrong, not the timing.

## Why this directory was nearly a second register

Session 3 created it on 2026-09-01 with full ownership tables in it, before having read
`session-contracts.md`, and then reported the two documents as contradicting each other. They did —
because the second one was an hour old. Recorded in §8.2 of the register, with the rule that came
out of it. **Two definitions of one quantity is the defect signature, and a new session writing its
own register is how you get one.**

## Live sessions

| File | Session | Worktree |
|---|---|---|
| `claude-core.md` | Session 1 — core capabilities | `ai-de-session-phase3-pane-probes` |
| `claude-ui-experience.md` | Session 3 — UI & experience | `ai-de-feature-ui-experience-refinement` |
| _(none yet)_ | Session 2 — main UI, `copilot-design-4d24d94a` | `ai-de-facelift` |
