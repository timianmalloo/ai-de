---
id: note-d1-108-landing-disclosure
title: "Present-time disclosure: D-1 108 merge f009b6f6 — what was recorded vs not"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, ruling-108, D-1, evidence]
links:
  - { to: spec-entry-points, rel: relates-to }
  - { to: note-understanding-views-owner-d1-mapping-impl, rel: relates-to }
review-by: 2027-03-17
summary: >-
  Honest 2026-09-17 disclosure. Does not backdate a closing receipt. Distinguishes
  git facts from unrecorded gate steps. Does not claim CI green.
---

# D-1 108 landing — present-time disclosure (2026-09-17)

Not a backdated closing receipt. Conductor reported no closing audit entry for `f009b6f6`. That gap is real.

## Recorded facts

| Fact | Evidence |
|---|---|
| Merge on `main` | `f009b6f6` `merge(understanding-views-d1): D-1 listing onto main (Ruling 108)` pushed `1df6f34e..f009b6f6` |
| First parent | `main` at merge time (after `1df6f34e`) |
| Second parent / candidate | `a643ecdf` on `understanding-views-d1` |
| Sequence | Disabled `mapping-unavailable` in landed `EntryPointsSurface` |
| Pre-merge AL5b supersedes | `al-01M2QSED6HYQ40BCPVXVDTCDEH` / `al-01M2QSEDC3B5HADZ18C4ZCJDH6` at `30d82c3b` — **partial / verification-executed false**, not a landing receipt |

## Unrecorded / not claimed

- No `kind:skill` closing audit line for the `f009b6f6` merge itself.
- No retained full 38/38 log **of that merge commit**. Headless `run-verify-gates` on the D-1 tree **before** merge had 3 FAIL (audit-capture, derived-views, stranded-audit). Hygiene commit `a643ecdf` closed audit-capture + derived-views on the candidate. **stranded-audit** named peer `atlas-view-spikes` — not in our delta; we still merged.
- **CI at `f009`:** App TRX for run **35228503081** includes D-1 failures `EntryPointsResult.Disclosures` uncovered and `MutedBrush` undeclared. **Not Core-green. Not App-green.** Repair is this turn on `understanding-views-d1`, not a claim that `f009` was green.

## This note does not

Backdate a receipt, invent a gate pass, or assert `main` tip is green.
