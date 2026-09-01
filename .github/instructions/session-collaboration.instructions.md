---
applyTo: "**"
---
# You are not the only session working this repository

Two or three agent sessions work `ai-de` at once, in separate git worktrees, at the same time as
you. This file is how you find them. It is **repo-specific**, not part of the AI-Forward Pack, and
it deliberately **states no ownership rules of its own** — it points at the one document that does.

## Before you edit anything shared

**1. Ownership lives in one place: `docs/collaboration/session-contracts.md` §2.**

That file is the single register. It has been since `6db9b6f` (2026-08-29) and the Design session
accepted it at `41e331f`. **§2 is the sole authority on which session owns which file.** If any
other document — including a liveness file, including this one — appears to say something different
about ownership, **§2 wins and the other copy has drifted.**

Do not write a second ownership map. That has already happened once: a session created a parallel
register before reading this one, then reported the two as contradicting. They did, because the
second was an hour old. It cost two sessions a round trip each and is recorded as §8.2.

**2. Liveness lives in `.agents/sessions/`.** Untracked, machine-local, shared by every worktree —
`coord-core.py` resolves it against the primary checkout, so you see it with no commit and no pull.
Read every file there before you start: it says who is running right now, in which worktree, on
what, and what they are blocked on.

**Write your own file there.** Agent name, session id, worktree, branch, status, what you are doing,
what you are waiting on. **No path tables** — ownership is §2's job.

**3. Claim a shared file for the minutes you are editing it.**

```
python docs/ai-forward-pack/scripts/coord-core.py claim --path <file> --wi <work-item> --ttl 300
python docs/ai-forward-pack/scripts/coord-core.py release --path <file>
```

A lease is for the edit, never for expressing an area — `overlaps()` matches by path segment, so a
claim on `src/AiDe.App/**` refuses every file beneath it and blocks everybody. If a claim is
*refused*, that is a **defect signal**: the plan is wrong, not the timing. Talk, do not wait out the
TTL.

## Set your identity, or your edits are not checked at all

The coordination record currently holds decisions logged as `anon` with
`COORD-NOT-CHECKED-IDENTITY` — *"AGENT_SESSION is unset, so this session has no identity to
check"*. Those edits were **not** checked against anyone's lease. They did not fail; they were
simply never examined, which is worse, because the log reads as if they were.

Export both before you work:

```
AGENT_SESSION=<your stable session id>
AGENT_NAME=<a readable name, e.g. copilot-design>
```

`coord check <path>` tells you whether it is actually checking. A `NOT CHECKED` result is not a
pass.

## How to answer another session

**Append a numbered section to `docs/collaboration/session-contracts.md`.** That is the file's own
protocol and every session has followed it — §4a…§4r are Core↔Design exchanges, §7 is the Design
session's accepted response, §8 is the third session joining. **Append your own section; never
rewrite someone else's.** A request made in conversation is a request the next session cannot read.

Do not write into another session's liveness file. Put your asks in your own, addressed to them.

## Open right now, for the Design session

`docs/collaboration/session-contracts.md` **§8.4** lists what is waiting on you, and **§8.3** is a
finding that lands in your files: nine places where Core publishes a bound — `Evidence`,
`FilesSkipped`, `Truncated`, `ScopesReused`, `Disclosures`, `Shortfall`, `Inspect`,
`HealthFindings`, `IsDeclared` — and the surface renders the value without it. Read it as one design
problem with one answer, not nine asks.

## Two conflicts that recur, and their protocol

Every rebase between sessions conflicts on the same files, and never on code (§4b).

| File | Resolution |
|---|---|
| `docs/audit/*.jsonl` | **Append-only. Union by content**, never by id: `python tools/merge-append-only-log.py docs/audit/audit-log.jsonl`. Hand-resolving dropped an entry once (DC-026) |
| `docs/docs-index.js`, `docs/audit/audit-data.js` | **Derived. Regenerate, never merge**: `docs-graph.py derive` and `audit-log.py render`. A hand-merged derived view is valid JSON, has no conflict marker, and is wrong (DC-060) |

After any rebase, run both regenerators and commit the result. `python
tools/verify-derived-views.py` fails if you forget.

**Take defect-class and ADR ids from `python tools/verify-id-allocators.py`, never "highest in the
file plus one."** It reads your working tree and compares against `origin/main`, so it warns while
you are writing rather than after you have committed and cited the id. Six ids collided across
sessions in two days.
