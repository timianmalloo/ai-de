---
id: proof-wl-chk6-gap-pin
title: "Evidence: T35 - CHK6's gap sentences are pinned against DESIGN.md (DC-NEW-wl-t26-copy)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, implement, copy, design-md, defect-class]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  A Core test reads DESIGN.md's CHK6 clause list and asserts each of the 7 gap sentences AcceptanceFold writes equals a
  clause. 4 clauses were missing from DESIGN.md and were added. The 3 existing clauses already equalled the code.
---

# Evidence: T35 `wl-chk6-gap-pin`

**Test:** `tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceGapSentencesMatchDesignTests.cs` (2 facts). The expected
text comes from DESIGN.md's CHK6 row. The test never reads a sentence out of `AcceptanceFold.cs`; the fold's sentence
is only the value under test.

**Count confirmed at base `b5ff1fdf`:** 7 gap codes, 7 sentences. DESIGN.md listed 3 (evidence-missing,
intent-not-accepted, checklist-empty). The 4 without text: intent-missing, evidence-stale, evidence-not-recorded,
needs-decision. This matches the brief.

**G3 (disagreement):** none. The 3 existing clauses equal the code's text, so no shrink-only list exists.

| Step | File | Result |
|---|---|---|
| Red 1, DESIGN.md at base | `G2-red-missing.txt` | fails naming the 4 codes above |
| G1 + green | `G2-green.txt` | 2 passed |
| Red 2, mutation: "no current evidence." to "no up-to-date evidence." in a scratch edit of `AcceptanceFold.cs` | `G2-red-mutation.txt` | fails naming `evidence-missing` and the orphaned clause; file restored, `git diff --stat src/` empty |
| Whole Core suite | `suite-green.txt` | see the file |

**G1:** four clauses were appended to CHK6's parenthesised list in the same ` · ` form, with `{obligation}`. The cell
still starts with `Ready to accept.`, so the App pin on CHK6 holds. `{n}` is used only by the existing intent clause.
