---
id: review-site-craft-gate
title: "Craft-gate review — the public site"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [review, craft, ui, site, github-pages]
links:
  - { to: docs-map-of-content, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2027-03-02
summary: >-
  The deterministic craft detector run over site/, its 21 findings, what was changed, and the one
  finding accepted with a reason — plus the two gaps the detector cannot see and neither can I.
---

# Craft-gate review — the public site

**Measured before diagnosed.** The detector was run over `site/` before any critique was written,
because a rubric applied to a surface nobody measured produces the findings the author already
believed.

```bash
python docs/ai-forward-pack/scripts/ui-craft-gate.py site --markdown
```

| Run | Major | Minor | Total |
|---|---|---|---|
| First | 8 | 13 | **21** |
| After the fix pass | 0 | 1 | **1** |

## The gate could not run at all, and said so correctly

The first invocation reported *"the detector produced no JSON, so nothing was scanned"* and advised
installing `impeccable` — on a machine that already had it. The detector had run and produced 10 KB
of findings; `ui-craft-gate.py` decoded the subprocess output with `text=True` and no `encoding`,
so on a Windows console it used cp1252 and raised `UnicodeDecodeError` inside the reader thread,
leaving stdout empty.

**The control behaved correctly.** Its empty-output guard exists precisely so that "nothing
scanned" never renders as "nothing wrong" (CD9, R4), and it held. What was wrong was the
*diagnosis* it offered, which pointed at a cause that could not have been true on that machine.

Fixed by pinning `encoding="utf-8", errors="replace"` on the `subprocess.run` call in
`docs/ai-forward-pack/scripts/ui-craft-gate.py`.

**Class, not instance.** This is the same shape as DC-078 (*a harness reports an assertion failure
as a broken machine*): a wrapper that cannot read its tool's output attributes the silence to the
tool's absence. Anywhere a pack script shells out and parses text, the encoding is part of the
contract, not the locale's business. This script was the only `subprocess.run(..., text=True)` in
the pack's script bundle at the time of writing.

## Findings and what was done

| Rule | Sev | Count | Disposition |
|---|---|---|---|
| `low-contrast` | Major | 3 | **Fixed.** The generic `a:hover` rule applied to the skip link, putting `--focus` text on an `--accent` background at 1.5:1. The skip link is the only place on the site where a link sits on a filled surface, and it is the one a keyboard user meets first. `.skip:hover, .skip:focus` now hold `--accent-contrast`. |
| `skipped-heading` | Major | 4 | **Fixed.** Footer section headings were `h4` under an `h2`; they are now `h2`. The surface-demo panel headings were `h5` under an `h3`; they are now `h4`. The collaboration page jumped `h1` → `h3` at the three instruments; an `h2` now names that section, which the page wanted anyway. |
| `all-caps-body` | Major | 1 | **Fixed.** The landing page's uppercase kicker is gone; the same facts are a normal-case line under the lead. |
| `codex-grid-background` | Minor | 4 | **Fixed — the rule was right and my rationale was not.** A two-axis hairline gradient grid tiled at a fixed cell, justified in the stylesheet as reading "like graph paper". That is a recognised generated-UI signature dressed as intent. Removed; the structure comes from the content. |
| `kicker-above-heading` | Minor | 4 | **Fixed.** "Idea one", "Idea two · Loomkeeper" and the three verb chips above the instrument headings are gone. The headings carry themselves and the words that mattered moved into the prose. |
| `side-tab` | Minor | 1 | **Fixed.** The source-quote block had a 2px accent border on its left edge. The citation line already marks it as quoted. |
| `layout-transition` | Minor | 1 | **Fixed.** The dimension meters animated `width` and redraw on every slider input; they now animate `transform: scaleX()` from a left origin. |
| `em-dash-overuse` | Minor | 2 | **Fixed.** 38 in the model page and 29 in the collaboration page, both at saturation density. Reduced to 2 and 6, and the remaining ones are in titles and proper names. This is the tell that matters most on a site arguing it was not generated carelessly. |
| `repeated-container-text` | Minor | 1 | **Accepted, with reason.** `"WeaveScorer"` appears four times inside `div.viewport` on the model page. The viewport is a **tab set**: one panel is visible at a time, and the node's name recurring across surfaces is the demonstration the section exists to make. The detector reads the DOM, which holds all seven panels at once. Not a defect; a limit of a static reader on a tabbed container. |

## What the detector cannot see, and I am not claiming

CD13/CD14: a clean run is a floor, never a verdict. Recorded here so nobody reads "1 finding" as
"reviewed".

- **Whether the copy is true.** Quotations are traced by hand and nothing gates them. The
  *figures* used to be in the same position and no longer are — see below.
- **Whether the JavaScript still matches the C#.** `site/assets/site.js` reimplements `WeaveScorer`,
  `LeaderboardComposer` and `GraderInjectionScanner` so the demos run offline. If a weight or a
  cohort rule changes in C#, nothing fails. *Gap: no test binds the two.* The `README` in `site/`
  says which file is the authority; that is a note, and a note is not a control.
- **Whether the archetype fits.** The site is a reading surface, not a workbench, so the
  application's `Workbench` archetype does not govern it. That judgement was made by a person and
  is not mechanised.

The remaining gaps are the same shape as CI6 — *a lesson recorded as prose is a memoir* — and
neither is claimed as covered.

## The figures gap closed itself by recurring — DC-082

The gap recorded above as "no control binds the site's figures to their sources" was, at the time
of writing, exactly the memoir CI6 warns about. It then proved the point three times in one
session:

| When | What moved |
|---|---|
| First rebase | App test floor 319 → 330 |
| Second rebase | Another session's audit entries; artifact count 303 → 304 |
| Third | Five of nine figures at once — test floor 1,697 → 1,728, ledger 499 → 503, symbols 1,715 → 1,733, audit entries 371 → 375 |

Each correction was itself stale within the turn. That is a class, not an incident, and it is now
**DC-082** with a control: every figure carries `data-figure="<name>"`, and
`tools/verify-site-figures.py` computes each name from its source and compares. `--update` rewrites
them; the Pages workflow runs it **without** `--update`, so a stale page fails the build instead of
publishing.

**Observed failing on the shipped shape** before anything was fixed — its first run named all five
stale figures with both values. It carries two DC-016 guards of its own: it fails when no
`data-figure` element exists at all (dropping the annotations would otherwise pass silently, the
same defect in a different costume), and it fails on a figure name it cannot compute rather than
skipping it. Both were exercised: adding DC-082 to the register moved the defect-class count and
the gate caught it on the next run, which is the shortest possible demonstration that it works.

**Still open:** nothing binds `site/assets/site.js` to the C# it mirrors. That one has no cheap
control — it needs a shared fixture both sides evaluate — and it is not claimed as covered.
