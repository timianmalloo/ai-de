---
id: proposal-call-budget-hook
title: "Proposal — a per-agent tool-call budget hook (hard stop for rule 16)"
type: proposal
status: proposed
owner: "@timianmalloo"
tags: [proposal, hooks, budget, go9, workbench-loops]
links:
  - { to: coordination-workbench-loops, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  Not wired. Four Opus nodes in a row overran rule 16's hand-off point, because the cap lives only in the brief. This
  proposes a Claude Code PreToolUse hook that counts each agent's tool calls and denies the next call once a budget is
  reached, with a message telling the agent to hand off. Applying it is a settings change, so the user decides. Until
  then, the Leader's wall-clock kick ladder is the interim control (DC-NEW-wl-leader2-b).
---

# Proposal — per-agent tool-call budget hook

**Status: proposed, NOT applied.** Applying it changes `.claude/settings.json`, so it is the user's decision. The Owner
(`wl-owner-20261018`) and the Leader (`wl-leader2-20261019`) have both declined to make that change themselves.

## The problem (measured)

Rule 16 says a node hands off at 120 tool calls or 200k context. Four Opus nodes in a row went past it. The counts are
harness-measured, from each agent's completion `usage`:

| node | tool calls | context | wall | s / call |
|---|---|---|---|---|
| T5 node 1 | 150 | 346k | 1976 s | 13.2 |
| T8 node 1 | 154 | 285k | 1623 s | 10.5 |
| T5 node 2 | 143 | 330k | 2643 s | 18.5 |
| T8 node 2 | 174 | 311k | 2103 s | 12.1 |
| T8 node 3 | 133 | 254k | 1798 s | 13.5 |

The agents' own counts ran 13–40% low (for example, T8 node 2 reported about 152 against a measured 174), so an agent
cannot enforce its own cap. A brief rule is a memoir (CI6). The control has to sit at the tool seam.

## The proposed control

A `PreToolUse` hook keeps a per-agent counter in the OS temp dir and allows every call until the budget is reached.
From then on it **denies** each call with a reason that tells the agent to commit, write `handoff.md` and stop. Like
`reread-guard.py`, it is **fail-open**: any error in the hook allows the call, so a broken hook never costs work.

### Assumptions (marked; each must be confirmed before applying)

- **assume:** a sub-agent's tool calls reach `PreToolUse` with a field that distinguishes that sub-agent from its
  parent, such as `agent_id`. `docs/ai-forward-pack/hooks/reread-guard.py` documents only `session_id`, `tool_name` and
  `tool_input` for Claude Code. **Confirm:** a spike that logs the payload's keys for one parent call and one sub-agent
  call. **If false:** the hook can only budget the whole session, which would stop the Leader too, so do not apply it.
  Fall back to the kick ladder.
- **assume:** a deny is returned as exit 0 plus
  `{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"..."}}`.
  This is Inferred from the Claude Code hook contract and not executed here. **Confirm:** the same spike, by denying one
  call. **If false:** the agent never sees the reason and keeps retrying.

### Script (`docs/ai-forward-pack/hooks/call-budget.py`, proposed; not added to the tree)

```python
#!/usr/bin/env python3
"""call-budget.py - deny a sub-agent's tool calls past a budget, so a node hands off on time (rule 16).

Fail-open: any error allows the call. The Leader (no agent id) is never budgeted.
Usage: call-budget.py --budget 120
"""
import argparse
import json
import os
import re
import sys
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--budget", type=int, default=120)
    args = parser.parse_args()
    try:
        payload = json.load(sys.stdin)
        agent = payload.get("agent_id")          # assume: present for sub-agent calls only (see proposal)
        if not agent:
            return 0
        key = re.sub(r"[^A-Za-z0-9_.-]", "_", "{0}-{1}".format(payload.get("session_id", ""), agent))[:120]
        path = os.path.join(tempfile.gettempdir(), "ai-forward-call-budget-{0}.json".format(key))
        try:
            with open(path, encoding="utf-8") as fh:
                count = int(json.load(fh).get("count", 0))
        except (OSError, ValueError):
            count = 0
        count += 1
        with open(path, "w", encoding="utf-8") as fh:
            json.dump({"count": count}, fh)
        if count <= args.budget:
            return 0
        reason = ("Tool-call budget reached ({0} of {1}, rule 16). Commit what is green, write handoff.md "
                  "(done / red / next, exact test names), mail the Leader 'handoff <sha>', and stop.").format(count, args.budget)
        json.dump({"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny",
                                          "permissionDecisionReason": reason}}, sys.stdout)
        return 0
    except Exception:  # fail-open: a broken budget hook must never cost a tool call
        return 0


if __name__ == "__main__":
    sys.exit(main())
```

The deny cannot be bypassed by the agent itself. The hand-off actions it asks for are tool calls, though, so in
practice the budget should leave about 10 calls of headroom: set `--budget 130` for a 120-call hand-off target, or
allow `Bash`/`Write` for `handoff.md` and `git commit` past the budget. Choose one when the spike runs.

### Settings snippet (`.claude/settings.json`, to add under `hooks.PreToolUse`; the user applies it)

```json
{
  "matcher": "*",
  "hooks": [
    { "type": "command", "command": "python docs/ai-forward-pack/hooks/call-budget.py --budget 130" }
  ]
}
```

On Windows the command is `python`, never `python3`. The repo's other hooks go through `run-hook.sh dispatch`; route
this one the same way if the pack's dispatcher should own it.

## Interim control (in force now)

The Leader's wall-clock kick ladder, derived from each node's measured seconds per call (table above):
- **The hand-off kick** goes at 120 × (the track's fastest measured s/call). For T8 that is 10.5 s, so about 21 min.
- **`TaskStop`** follows 8 minutes later if the node is still running. That loses only uncommitted work.
- **The rate is re-derived** after every node from its harness `usage`.

## Rollback

Remove the snippet from `.claude/settings.json`. The hook keeps no state outside its temp files.
