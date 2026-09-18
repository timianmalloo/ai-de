---
id: note-explore-lane-not-landable
title: "lane/p2-repairs-explore is not landable: Ruling 140 changed a product default and sixteen App tests assert the old one"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [landing, ruling-140, architecture-default, app-tests, wind-down]
links:
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-18
summary: >-
  The Explore lane carries P0a, P2, P3, P4, P6, Ruling 140's default change and two mockups, and its
  Core suite is green at 2,781. It cannot land: Ruling 140 moved Architecture's default to
  Center = [Graph, Tree] with Left empty, and sixteen App tests assert the previous default. The
  recount found them; nothing else would have.
---

# The Explore lane is not landable, and why that is the right answer

`lane/p2-repairs-explore` is green where it was measured and red where it was not. Core is
**2,781 Completed**. App is **1,057 executed, 16 failed** — and every one of those sixteen is a
consequence of **Ruling 140**, which deliberately changed what the product opens with.

## What actually happened

Ruling 140 amended Ruling 94: Architecture's default becomes `Center = [Graph (active), Tree]` with
Left empty and collapsed, and Contexts and Domain leave the default while staying admitted. That is
the operator's own instruction — *"the right-side-views for the architecture explorer need to be
Graph and Tree"* — and it is implemented in `ZoneLayout.ArchitectureDefault()`.

When I made that change I wrote a headless Core test for it and recorded a finding: the ruling's own
"default-layout test" lives in `AiDe.App.Tests`, behind a desktop slot, and **the whole 168-test Core
layout suite passed while the default was rewritten underneath it**. I added the Core test to close
that gap.

**I then did not run the App suite.** I had just written down that the coverage lived there.

## The sixteen, by family

| Family | n | What they assert |
|---|---|---|
| `WorkbenchDragCompletedHookTests` | 8 | parameterised on `perspective: "architecture", dragged: "graph", onto: "contexts", expectedBefore: Left` — they drag the Graph *out of Left*, which no longer holds it |
| `SolutionTreeSurfaceTests` | 4 | the Solution tree surface, now a default Center tab rather than an on-demand one |
| `PerspectiveLayoutSlotTests` | 2 | one is literally named `TheArchitectureDefault_IsLeftGraph_CenterContextsThenDomain_RightAndBottomEmptyAndCollapsed` |
| `WorkbenchShellTests` | 1 | composes every surface from the default layout |
| `ShellContrastCensusTests` | 1 | *"the census did not walk the Domain tab in Architecture's Center"* — Domain left the default, so the census walks a different set |

None of these is a defect in the tests. They are correct assertions about a default that a ruling
changed on purpose, and they caught it. That is the App suite doing exactly its job.

## Why the lane stays unlanded

Landing it would add sixteen App failures to `main` in one move, which is precisely what Ruling 112
forbids — a candidate may land on a red trunk **without widening the red set**, and this widens it by
sixteen. The lane is pushed and preserved; nothing is lost by waiting.

## What is owed, precisely

1. **Update the sixteen to the new default.** Mechanical in shape but not blind: each asserts
   something true about the old layout, and the replacement must assert the equivalent truth about
   the new one — *"rewrite the test that asserts the old contract"*, the same move INV-0013's phase 1
   makes. The drag family needs its parameters re-pointed (Graph now starts in Center), not deleted.
2. **Run the App suite in the desktop slot** before claiming it.
3. **`ShellContrastCensusTests` needs a decision, not just an edit**: the census walked Domain because
   Domain was in the default. With Domain admitted-but-not-default, either the census walks the
   admitted set rather than the default set, or it legitimately covers less. That is a coverage
   question and belongs with the Test Architect.

## The lesson, which is the register's own

A product default is a surface, and E7's rule is that a change must reach every surface it touches. I
identified the exact gap in writing — *the coverage lives in the App suite, behind a slot* — and then
treated a green Core run as though it spoke for both. **A gate's green is evidence that the gate
passed, not that its subject is correct**, and I had already written down which gate was not being
run. The recount is what caught it, one step before the landing.
