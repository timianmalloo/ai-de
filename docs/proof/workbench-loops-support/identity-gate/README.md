---
id: proof-wl-identity-gate
title: "Proof Pack - the account-identity gate"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [proof-pack, privacy, gate, workbench-loops]
links:
  - { to: architecture, rel: implements }
review-by: 2027-04-19
review-suggested: []
summary: >-
  tools/verify-no-account-identity.py refuses email-shaped strings in tracked docs/proof/ and .agents/
  files, masks the local part in every finding, and proves it can fail with --self-test.
---

# Proof Pack - the account-identity gate

**Gate:** `tools/verify-no-account-identity.py`. It reads only tracked files (`git ls-files`) under
`docs/proof/` and `.agents/`. Each finding prints as `<path>:<line>: <masked address>`.

**Allow-list (exact):** any address at `example.invalid`, `example.com`, `example.org`,
`users.noreply.github.com`; and the single address `noreply@anthropic.com` (the Leader's ruling, matched exactly
and case-insensitively, never its domain). No per-file opt-out. A last label that is not alphabetic
(`tool@1.10.0`) is a package pin, not an address.

| File | What it shows |
|---|---|
| `gate-red.txt`, `self-test-red.txt` | commit 1: the self-test fails against a stub that always passes |
| `anthropic-exact-red.txt` | commit 2: a domain-wide `anthropic.com` allow fails the exact-address case |
| `self-test.txt` | final self-test, exit 0 |
| `gate-green.txt` | the gate on this tree, exit 0 |
| `gate-line.txt` | the gate's row from `python tools/run-verify-gates.py` |

**Finding on main before the exact-address allowance:** seven `.agents/briefs/workbench-loops/*.md` lines carried the
co-author trailer address. It is a public trailer, not an account; the Leader allows that one address.
No real account address was found under `.agents/` or `docs/proof/`.
