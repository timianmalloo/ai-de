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

## The craft gate — run, and NOT passed

```
python docs/ai-forward-pack/scripts/ui-craft-gate.py --a11y-obligation --gate \
    docs/mockups/first-use-accounts.html
→ exit 1 · Blocker 2 · Minor 6
```

| Severity | Finding | Disposition |
|---|---|---|
| **Blocker** ×2 | `tiny-text` | **Open. Not this file's defect — see below.** |
| Minor ×3 | `repeated-container-text` | Expected: nine scenarios of the same surface repeat by design. |
| Minor | `em-dash-overuse` | Open, in the copy. |
| Minor | `gpt-thin-border-wide-shadow` | Open. |
| Minor | `flat-type-hierarchy` | Open; the lens's own rubric names hierarchy too. |

Five `design-system-color` **Majors** were raised on the first run and are **fixed**: the review
harness chrome restated nine colours as raw hex. It is scaffolding, but it ships inside the committed
artifact, and a review harness that fails the craft floor undermines the review it hosts. It now uses
the same tokens the surface does.

### The two Blockers are a finding against `DESIGN.md`, not against this mockup

The mockup mirrors the committed token scale verbatim. `DESIGN.md:84` reads:

```yaml
scale: [11px, 12px, 13px, 15px, 18px, 22px]
```

and `:750` and `:1101` document 11px deliberately — key labels such as `Ctrl+Enter`, and the event
lines derived under a session turn. The mockup binds that to `--t-xs:11px` and uses it twice in
visible text (`.why code`, `.prov-note`). The detector, run with `--a11y-obligation` as `CD12`
requires, calls each of those a Blocker.

So **two committed controls disagree**: the design system says 11px is the intended floor, and the
deterministic craft detector says 11px under an accessibility obligation is a Blocker. Editing the
mockup off the design system would make the number green and settle nothing — the same conflict would
still be live in the product, wherever `--t-xs` is used.

It is therefore recorded here as open and routed: the **UX & Accessibility lens holds the WCAG veto**
and did not clear its own work, and the type scale is the Owner's to amend if it changes. **This
mockup is not approved.** A clean gate run would have been a floor, never a verdict; a failing one is
not even that.

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
- The two `tiny-text` Blockers, routed above.
- A second pass by a reviewer who is not the author: the lens states explicitly that it does not clear
  its own accessibility veto.
