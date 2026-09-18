---
id: mockup-first-use-accounts
title: "First-use accounts — the New Session sheet and Configure dialog, with the state table Ruling 130 requires before any code"
type: mockup
status: draft
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [ui-design, first-use, accounts, engine-catalog, wcag, ruling-130, ruling-134, dc-228]
links:
  - { to: inv-0013-the-sheet-asks-the-config-not-the-machine, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: review-ui-explore-graph-and-tree, rel: relates-to }
review-by: 2026-12-17
summary: >-
  The UX & Accessibility lens's state table, exact copy and mockup for the New Session accounts block
  and the Configure sheet, authored under /ui-design elevate to discharge Ruling 130's condition that
  the lens rules the copy before any code is written. The mockup renders nine scenarios with a review
  harness. The deterministic craft gate has been run against it and it does NOT pass: two tiny-text
  Blockers remain, and they are findings against DESIGN.md's own type scale rather than against this
  file.
---

# First-use accounts

The surface the operator hit on a clean machine, redesigned. `INV-0013` has the root cause — the sheet
answered *"can this engine launch here?"* from the product's own config file — and `DC-228` has the
class. This is the design half: the state table and the exact strings that **Ruling 130 requires
before any code is written**, and **Ruling 133** repeats as a precondition on Stream Y phases 1–4.

The artifact is `first-use-accounts.html` beside this note: self-contained, dependency-free, opens over
`file://`, with a review harness across persona · viewport · scenario · theme · density · reduced
motion, and nine rendered scenarios including a mixed list where all five rows genuinely differ.

## Provenance, and a limit on it

Authored by the **UX & Accessibility lens** under `/ui-design` elevate. The lens ran **without Bash,
Write or Edit** — it could not create a worktree, write a file, or run the craft gate. It returned the
complete file body as text and said so plainly rather than reporting work it had not done. The
conductor wrote the file to disk verbatim and ran the gate.

That division matters when reading what follows: **the design is the lens's, the gate result is
measured, and the two disagree.**

## The craft gate — run, and now clean at the floor

```
python docs/ai-forward-pack/scripts/ui-craft-gate.py --a11y-obligation --gate     docs/mockups/first-use-accounts.html
→ exit 0 · Minor 6 (advisory: em-dash density, repeated container text across the
  nine scenarios, a thin-border/wide-shadow tell, flat type hierarchy)
```

**It did not start there, and the route is worth recording because the conductor got
the diagnosis wrong first.**

The first run was `exit 1 · Blocker 2 · Major 5`. The five `design-system-color`
Majors were the review harness restating nine colours as raw hex — scaffolding, but
it ships inside the committed artifact, and a harness that fails the craft floor
undermines the review it hosts. Fixed: it now uses the same tokens the surface does.

The two `tiny-text` Blockers the conductor **filed as a conflict between two
committed controls** — the design system saying 11px is the intended floor, the
detector saying 11px under an accessibility obligation is a Blocker — and routed to
the Owner as unresolvable from here.

**Ruling 147 found there was no conflict.** The detector's `tiny-text` fires only on
text **over 20 characters below 12px, outside `kbd` / `code` / `label` contexts**,
and its companion `undersized-ui-text` floors at 11px. A keystroke label is short and
`kbd`-shaped and clears both. `DESIGN.md:750` and `:1101` already said 11px is for
key labels and keystrokes — so the design system's stated use and the detector agree
by construction, and always did.

What actually happened is that **this mockup applied `--t-xs` to running text**: a
provenance note and a screen-reader trace, neither of them a keystroke label. Moving
them to 12px is not editing the mockup off the design system; it is bringing it onto
it. `.why code` keeps `--t-xs`, because `code` is exempt.

Ruling 147 also added the constraint to `DESIGN.md:84` as a clause on the scale
itself, so it travels with the tokens instead of sitting two hundred lines away in
prose.

**Still not approved.** The gate is a floor, never a verdict — it cannot see whether
the archetype fits, whether the state table is right, or whether the copy is true.
Ruling 147(a) requires a **separate** UX & Accessibility instance in Adversary mode
to clear the veto against this gate output; the authoring lens does not clear its own
work, and that clearance is not yet on disk.

## What this is for

Stream Y (the Sessions repairs) cannot start phases 1–4 until the state table and the exact cell
strings are filed — that is Ruling 130 as the Owner wrote it, and Ruling 133 repeats it. The table
covers five accounts × the three states Ruling 130 separates (**installed** · **configured** ·
**ready**) plus absent, and the lens's central argument is that these are **three axes, not one enum**
— which is precisely the collapse `INV-0013` found in the code.

**Ruling 134 governs the `ready` cell** and the lens accepted it: `ready` requires a sign-in the
product observed and recorded, an already-logged-in CLI satisfies it through the product's own gesture
as a no-op confirmation, and there is no prober and no ambient inference. The lens accepts the rule and
disputes one of its current consequences in the footer copy; that dispute is in the report, not
resolved here.

## Owed

- The rubric critique and ranked plan as a committed `docs/reviews/` artifact — the lens produced both
  and could not write them.
- A second pass by a reviewer who is not the author: the lens states explicitly that it does not clear
  its own accessibility veto.
