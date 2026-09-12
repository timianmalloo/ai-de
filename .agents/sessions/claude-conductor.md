# Conductor — `claude-conductor` (liveness)

**Ownership lives in `docs/collaboration/session-contracts.md` §2 and §8. Nothing here restates it.**

| | |
|---|---|
| **Agent** | `claude-conductor` (Claude Code, Opus 5) |
| **Session id** | `conductor-addendum-c` |
| **Worktree** | `C:/Projects/ai-de-conductor-addendum-c` |
| **Branch** | `conductor/addendum-c` |
| **Based on** | `main` `5ce4b08e` |
| **Status** | live |
| **Last updated** | 2026-09-12 (19:45Z) |

## Doing right now

Executing `docs/coordination/addendum-cd.md` wave by wave. Landed on `main`: S0–S2, DS-1, SH-1,
SH-2, CV-0, CV-1, X-1, X-2 (INV-0011). Live: **SH-3** (`lane/shell-sh3`, Coding/Architecture
defaults) and **CV-2** (`lane/conversation-cv2`, the mechanical compile + envelope store + Prepare,
dispatched 19:44Z from `5ce4b08e`). Joins happen in the primary checkout (the recorded WT1
exception), serialized, with the whole-suite recount and every gate; a Release build follows each
join that changes what the operator sees. Register ids are allocated at the join (next free:
DC-169). No leases held.

## Waiting on

| From | What |
|---|---|
| **SH-3** | its close report → join → rebuild |
| **CV-2** | its close report → join → rebuild (the operator waits for this build before the next manual test) |
| **The user** | PD-5's attended ~30-min wire spike window (gates CV-3); the F5 exit run on `feature/exit-evidence` @ `135e05e1`; CV-1's attended rows (`docs/proof/composer-as-conversation.md`); CV-0's read-only turn (`docs/proof/read-only-turn.md`) |

## To peers (Copilot Atlas fleet)

`atlas/*` worktrees are yours; the conductor neither reads nor removes them. Ownership for the
horizon is in `session-contracts.md` §2 (Shell lane / Conversation lane / side tracks). Shared
files under lease refuse the pre-commit boundary — claim for the minutes of the edit, default TTL.
