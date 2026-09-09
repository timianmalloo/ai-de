---
id: note-conductor-agents-gitignore-deviation
title: "Decision note — decline the pack rev-63 .agents/* ignore lines"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [ai-forward-pack, coordination, gitignore, capture-mandate, updatepack]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: knowledge-hub, rel: relates-to }
review-by: 2026-12-09
summary: >-
  AI-Forward Pack revision 63 instructs every consuming repo to carry `.agents/*` then
  `!.agents/artifacts.yml` in .gitignore. This repo declines: its .agents/decisions/,
  .agents/log/ and .agents/sessions/ are committed loomkeeper contract logs that AGENTS.md
  mandates, and the pack's mechanism would make new episode captures invisible to git —
  a failure this repo has already measured once.
---

# Decision note — decline the pack rev-63 `.agents/*` ignore lines

**Ruled by:** Owner agent (delegated CT20 authority), 2026-09-09. Confidence: **Verified** —
the Owner read `.gitignore` lines 531-544 and re-ran `git check-ignore` itself, including on a
hypothetical `.agents/log/probe.jsonl`.

## Question

`pack-apply.py plan` against source clone `C:\Projects\ai-forward` reports **235 UNCHANGED,
12 KEEP, 3 SKIP, 1 UPDATE**. Both repos are at **pack revision 63**. The single UPDATE is
`.gitignore`: pack-apply wants to add `.agents/*` followed by `!.agents/artifacts.yml`, and the
revision-63 changelog instructs exactly this.

This repository deliberately does not carry those lines.

## Ruling

**Keep the deviation.** Do not apply the pack's `.agents/*` + `!.agents/artifacts.yml` lines.
Record the decision here so pack refreshes stop proposing it and nobody re-argues it.

## Because

- `.agents/decisions/`, `.agents/log/` and `.agents/sessions/` are the **committed loomkeeper
  contract logs** that `AGENTS.md` mandates and `tools/verify-stranded-audit.py` checks.
  `.agents/log/` is `$AIDE_CONTRACT_LOG`, where every episode-close capture is written.
- The `.gitignore` block records a **measured** past failure: a new capture under `.agents/log/`
  was invisible to git while the already-tracked ones looked fine — the capture mandate failing
  silently. The mechanism is that the stock `[Ll]og/` rule excludes the **directory**, so a
  file-level negation can never re-include it.
- The pack's stated invariant is that **the registry travels with the repo**. That invariant
  **holds here by a wider route**, verified: `git check-ignore --quiet .agents/artifacts.yml`
  exits 1 (not ignored); `git ls-files .agents/` returns 18 tracked files;
  `coord-core.py doctor` reports `registry ok - 6 pattern(s)` with both merge drivers declared
  and registered; `pack-doctor.py` reports **PASS** on `coordination`.
- The rejected middle option — apply the pack default, then negate `decisions/`, `log/` and
  `sessions/` — reproduces the exact directory-exclusion trap already measured here.

Applying the pack's letter would break the pack's own capture mandate. Where a pack instruction
and a pack mandate conflict, the mandate wins and the conflict is recorded.

## Scope effect

Freezes `.gitignore` treatment of `.agents/` for this programme. No effect on Phase 1 work.

## For the next `/updatepack`

The declined instruction is **pack revision 63**, coordination area, the entry whose deploy note
reads "Check the target .gitignore carries `.agents/*` then `!.agents/artifacts.yml` in that
order." Cite this note rather than re-deriving the argument. If `pack-apply`'s existing KEEP
mechanism can be taught to carry `.gitignore` as a declared repo-local deviation, register it
there as well; that path was **not** inspected when this ruling was made.
