# Conductor — `claude-conductor` (liveness)

**Ownership lives in `docs/collaboration/session-contracts.md` §2 and §8. Nothing here restates it.**

| | |
|---|---|
| **Agent** | `claude-conductor` (Claude Code, Opus 5) |
| **Session id** | `conductor-addendum-c` |
| **Worktree** | `C:/Projects/ai-de-conductor-addendum-c` |
| **Branch** | `conductor/addendum-c` |
| **Based on** | `main` `4b8d379c` |
| **Status** | live |
| **Last updated** | 2026-09-13 (17:20Z) |

## Doing right now

The operator's first manual test of the CV-2 build (2026-09-13 09:27–09:33, five screenshots)
became **Rulings 80–87** (filed, on `main` `4b8d379c`). Live: **D3** (`design/operator-findings-0913`,
`/ui-design` elevate — mockups, DESIGN.md errata, the ranked plan for CV-5 and SH-4) and **X-3**
(`side/x3-shell-seams` — the Shell-lane seam requests, Rulings 85/86). Landed on the conductor
branch, joining with X-3: **X-4** = Ruling 87 (UTF-8 engine streams, DC-177). Next dispatches from
D3's plan: **CV-5** (Rulings 81/82/80 — the conversation) and **SH-4** (Rulings 84/83 — Coordination,
the left dock). CV-3 stays blocked on PD-5's attended spike. Joins in the primary, serialized:
merge → recount → audit → regenerate → commit → `run-verify-gates.py` → push → Release build.
Next free register id: DC-178. No leases held.

## Waiting on

| From | What |
|---|---|
| **X-3** | close → join with X-4 → rebuild |
| **D3** | the ranked plan → dispatch CV-5 ∥ SH-4 |
| **The user** | PD-5's attended ~30-min window (gates CV-3); the F5 exit run (`feature/exit-evidence` @ `135e05e1`); the attended rows of CV-0/CV-1/CV-2 on the next build |

## To peers (Copilot Atlas fleet)

`atlas/*` worktrees are yours; the conductor neither reads nor removes them. Ownership for the
horizon is in `session-contracts.md` §2. Shared files under lease refuse the pre-commit boundary —
claim for the minutes of the edit, default TTL.
