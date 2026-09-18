---
id: mockup-first-use-accounts
title: "First-use accounts — the New Session sheet and Configure dialog, blocked by an accessibility adversary"
type: doc
status: draft
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [ui-design, first-use, accounts, engine-catalog, wcag, ruling-130, ruling-134, dc-228]
links:
  - { to: inv-0013-the-sheet-asks-the-config-not-the-machine, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: design-first-use-account-states, rel: refines }
  - { to: review-ui-explore-graph-and-tree, rel: relates-to }
review-by: 2026-12-17
summary: >-
  The UX & Accessibility lens's state table, exact copy and mockup for the New Session accounts block
  and the Configure sheet, authored under /ui-design elevate to discharge Ruling 130's condition that
  the lens rules the copy before any code is written. The mockup renders nine scenarios with a review
  harness. The craft gate now exits 0 with three advisory Minors: Ruling 147 found that the two tiny-text
  Blockers were this file applying the 11px keystroke-label token to running text, not a conflict in
  the design system as the conductor first reported. Not approved — a separate accessibility adversary
  still owes the clearance.
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

That division matters when reading what follows: **the design is the lens's and the gate result is
measured** — and the first reading of that measurement, recorded below, was the conductor's and was
wrong.

## The craft gate — run, and now clean at the floor

```
python docs/ai-forward-pack/scripts/ui-craft-gate.py --a11y-obligation --gate     docs/mockups/first-use-accounts.html
→ exit 0 · Minor 3 (em-dash density, repeated container text across the nine scenarios)
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

## BLOCKED TWICE, and this note was part of the reason both times

Two adversary passes under Ruling 147(a). Both refused. The second refusal is the
instructive one, and its headline is a sentence about method rather than about copy:

> **The fixes were applied to the mockup and not to the deliverable.**

Three of the first pass's four Blockers were closed in the HTML and left open in
`docs/design/first-use-account-states.md` — the artifact Ruling 133 gates phases 1–4 on,
and which declares itself the canonical copy source. So two committed files each claimed
canonicity over one set of strings **and disagreed on the operator's own cell**. A coder
starting phase 1 tomorrow, doing exactly what Ruling 133 tells them to, would have opened
the table and rendered `installed — not set up here` behind a `Set up…` button: the precise
string the first pass blocked, in the case the whole investigation was filed about.

The adversary named the pattern, and it is the same one that lost the table for a day:
*fix the rendered artifact, leave the specification.* Twice in one lane, on the same pair
of files, in the same week.

**And this note was stale again when it was read.** It still reported `Minor 6` after the
count had fallen to 3, and still presented three Blockers as standing after they had been
changed. It had flipped from overstating the work to understating it — *"and both times by
describing a state nobody re-observed"*, which is the class this note itself indicts two
paragraphs above. That is three times. The rule that would have prevented all three: **a
note about an artifact is re-read against the artifact before it is committed, not written
from memory of having changed it.**

### What is now done

| Finding | Disposition |
|---|---|
| The table carried every blocked string | **Synced.** S1's cell, its reason and its button; F2's footer; the label help. `not set up here` now appears nowhere in the deliverable. |
| F0 forbade the footer that fixed Blocker 3 | **F0b added** — one clause per blocking axis, in row order, never the sign-in axis for a row blocked on install. |
| F1 asserted "no agent CLI was found" for the shim, which **was** found | **F1 narrowed, F1b added** for the shim case. |
| The sort rule ordered seven words and named one that does not exist | **All eight enumerated**, spelled as the cells spell them, with the configured-above-unconfigured tie-break stated. |
| Two files claiming canonicity | **The table wins**; the mockup now says so and says why. |
| Prefilled account label | **Reverted.** My rationale — "the default the rows already display" — was **false in the case it was built for**: in S1 no row displays a label at all. And a prefill makes an account named `work` indistinguishable afterwards from one the operator chose, which is verbatim this design's own argument for `health` and which Ruling 134 froze. |
| Same state word, two primary actions | **Reverted** the over-applied button swap; the shim row is `Set up…` again, matching its own reason sentence. |
| `#write-why` never updated by the sign-in | **Fixed.** Both inputs drive one gate. The message had read "Confirm the sign-in above" *permanently, after the operator confirmed the sign-in*, inside a live region that therefore announced nothing — the success state of the fix was the one state the fix did not render. |

### Still open, and not to be papered over

The second pass raised holes in the **table itself**, which had never been adversarially
read before: `not-recorded` (S9) has no named producer, so an implementer must invent the
trigger — which is DC-228, the class this deliverable exists to close, surviving inside it.
There is no focus model and no target-size rule for the sign-in gesture Ruling 134 makes
load-bearing. Row buttons still have no per-row accessible name, so five read as
`Edit… Edit… Sign in… Set up… Set up…`. Disabled-versus-enabled collapses entirely in the
high-contrast theme. And **Ruling 147(a) is unsatisfiable by a lens with no shell**: it
requires the clearance to cite the gate output, and no adversary so far could run it.

**Not approved.** A third pass is owed, and it should be given the gate output rather than
a report of it.

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
  and could not write them. **The same failure that lost the state table**; whoever writes them should
  extract from the transcript rather than summarise.
- A second pass by a reviewer who is not the author: the lens states explicitly that it does not clear
  its own accessibility veto.
