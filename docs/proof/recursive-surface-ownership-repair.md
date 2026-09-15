---
id: proof-recursive-surface-ownership-repair
title: "Recursive surface ownership parser repair proof"
type: proof-pack
status: in-progress
owner: "@timianmalloo"
tags: [coordination, ownership, python, verification]
links:
  - { to: investigation-recursive-surface-ownership, rel: implements }
  - { to: proof-recursive-surface-ownership-author, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Records failing-first and green evidence for the five independent parser findings, the expanded
  relevance boundary, and the unchanged real-register outcomes. Independent acceptance remains open.
---

# Proof Pack: recursive surface ownership parser repair

- **Pinned blocked commit:** `18a4a19f82eff6130c9938bdf401539cd2a8944f`
- **Blocked source blob:** `0f53658b30857222d8207b9b6fdfe706039b6e8b`
- **Intermediate five-case repair blob:** `112d2200e7f746087e0c39ab2f8c1fc68f9c6e1b`
- **Final broad-pattern repair blob before commit:** `3b66c0778c131b06bf7fd1adad01c888bf5b6b00`
- **Final mutation-closure source blob:** `ce11ae27eb79b97f54a1cf0fec4e5036f7ad90c4`
- **Investigation:** `investigation-recursive-surface-ownership`
- **Tier:** T1
- **Author/date:** `codex-sol-author`, 2026-09-15

## Evidence

| Claim | Oracle | Red | Green | Confidence |
|---|---|---|---|---|
| all non-owner headings clear ownership state | `#### Notes` after Core `/**` | no problem returned | outside-owner malformed diagnostic | Verified |
| a row without a Path header cannot disappear | Design pipe row after owner reset | no problem returned | headerless-row malformed diagnostic | Verified |
| a row without pipes cannot disappear | code-token row while Path table active | no problem returned | missing-delimiter malformed diagnostic | Verified |
| jurisdiction equals the surface token population | unquoted exact `WorkbenchShell.cs` | false malformed diagnostic | zero problems | Verified |
| indentation cannot extend §2 | two-space `## 3` followed by Design row | false Core/Design conflict | zero problems | Verified |
| original behavior remains protected | complete self-test plus eight source mutants | five new tests red before repair | exit 0 in 1.958 s | Verified |
| independent fixture accepts repaired behavior | reviewer-owned nine-case script | 4/9, exit 1 | 9/9, exit 0 in 0.1275984 s | Verified |
| live header reuse survives | real §2 rows after prose plus real register checks | N/A | base only ProseView red; Conductor 17/17 green | Verified |
| every supported pattern enters jurisdiction before matching | Core `/**` plus Design `/*.cs` | conflict silently absent; exit 1 in 0.9924614 s | cross-owner conflict reported | Verified |

The five added tests ran together before repair and exited 1 in 0.9486139 seconds with these exact
names:

1. `any non-owner heading resets owner context`
2. `surface row without Path header is visible`
3. `surface row without table delimiters is visible`
4. `unrelated Workbench filename stays outside jurisdiction`
5. `indented level-two heading ends section 2`

The final mutation run also observed both reviewer-required one-change mutants exit nonzero inside
the complete self-test:

| Injected mutant | Killing fixture | Observed result |
|---|---|---|
| disable `MARKDOWN_HEADING` reset | `any non-owner heading resets owner context` (`#### Notes`) | mutant subprocess exited nonzero |
| disable the `row_without_pipes` branch | `surface row without table delimiters is visible` | mutant subprocess exited nonzero |

## Final commands

| Command | Exit | Duration | Observed result |
|---|---:|---:|---|
| `python tools/verify-surface-ownership.py --self-test` | 0 | 1.958 s | complete suite and eight source mutants pass |
| reviewer `adversarial_review.py` beside repaired source copy | 0 | 0.1275984 s | 9/9 passed |
| `python -m py_compile tools/verify-surface-ownership.py` | 0 | 0.0837541 s | no output |
| normal CLI on author base | 1 | 0.1132545 s | only `Sessions/ProseView.cs` unowned |
| repaired module against Conductor Ruling 114 tree | 0 | 0.0869847 s | 17 surfaces, 17 owners, zero problems |
| `python tools/verify-gate-self-tests.py` | 0 | 0.0870036 s | 36 gates; frozen debt unchanged |

## Scope and residuals

Only the gate/self-test, this proof, the investigation, audit, and derived documentation are in the
repair scope. No product source, §2 authority, other tool, or lesson register changes.

- **Verified:** all five reviewer cases and the suffix/pattern sibling sweep pass locally.
- **Flagged:** independent review of the frozen repair commit remains mandatory; the author does not
  clear its own veto.
- **Flagged:** the Conductor owns the integrated full runner and final Ruling 114 join.
- **Flagged:** the declared six-call estimate fired after a stale coordination CLI signature and a
  stale mutation-map patch context each required correction. Both failed before repository edits;
  root owns the shared DC118 lesson update.

| | |
|---|---|
| **Completed** | Failing-first repair fixtures, bounded parser repair, full self-test, independent reproducer rerun, and real-register checks |
| **Remaining** | Independent frozen-candidate review and Conductor join |
| **Best next action** | Commit and hand the frozen SHA/source blob to the reviewer |
