---
id: investigation-recursive-surface-ownership
title: "Recursive surface ownership parser review block"
type: investigation
status: complete
owner: "@timianmalloo"
tags: [coordination, ownership, parser, verification]
links:
  - { to: proof-recursive-surface-ownership-author, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Reproduces and traces five independent parser failures on author commit 18a4a19f, records the
  necessary and sufficient causes, and defines the same-scope repair that preserves live section 2
  header reuse and exact surface-token jurisdiction.
---

# Recursive surface ownership parser review block

## Root-cause overview

The independent reproducer ran against a byte-identical copy of author commit
`18a4a19f82eff6130c9938bdf401539cd2a8944f`: both gate files hash to
`0f53658b30857222d8207b9b6fdfe706039b6e8b`. It exited 1 with five failures and four passes.
The five failures are parser state and token-jurisdiction defects. Discovery, path identity,
pattern matching, ambiguity, conflict handling, and exception precedence were disconfirmed.

| Reviewer case | Observed red on `18a4a19f` | Necessary parser cause | Sufficient repair |
|---|---|---|---|
| `#### Notes` leaks Core owner | no problem returned | owner reset recognizes only `###`; Core remains active and the later exact row deduplicates against Core's `/**` | after matching `### <owner> owns`, reset owner/table state for any Markdown heading level |
| Design row without Path header ignored | no problem returned | a new owner clears `path_column`; a pipe row then takes `if path_column is None: continue` | diagnose a surface-relevant pipe row when no Path header is active |
| surface row without pipes ignored | no problem returned | malformed-row detection requires a literal `|`; a line beginning with a surface code token has none | while a Path table is active, diagnose a relevant code-token row even with no pipe delimiters |
| unquoted `WorkbenchShell.cs` rejected | malformed surface declaration | `_surface_relevant` treats every string containing the Workbench root as a surface, wider than recursive discovery's exact suffix population | define jurisdiction by `Surface.cs`/`View.cs` tokens and supported `/**` patterns; root membership alone is insufficient |
| indented `## 3` does not end §2 | Core/Design conflict | section start strips whitespace but section-end search uses raw `startswith("## ")` | left-strip before recognizing the next level-two heading |

These predicates are necessary because removing each one recreates its observed branch. They are
sufficient because the same independent nine-case fixture passes after only these changes, while
the full prior self-test, live-register checks, and mutation oracles remain green.

## Disconfirmation record

- **Discovery/path identity ruled out:** every failing fixture's populated file reaches `check`;
  nested discovery and exact identities remain green in the full self-test.
- **Matching ruled out:** the independent `*` segment-boundary case passes; the repaired self-test
  also covers nested `/**` rather than only root `/**`.
- **Ambiguity ruled out:** the duplicate bare filename still reports both exact candidates.
- **Conflict/exception precedence ruled out:** a cross-owner conflict still fails when an exception
  names the same path.
- **Malformed-token handling is not globally absent:** the unbalanced-backtick case already passed;
  the two missing-row states were narrower state-predicate gaps.
- **Marker sweep:** `tools/verify-surface-ownership.py` contains no `simplify:` or `assume:` marker.

## Same-class sweep

The live §2 section contains only `### Core owns`, `### Design owns`, and `### Shared` headings.
Every current declaration row has pipes. It deliberately reuses each owner's earlier Path header
after intervening prose: Core resumes at lines 119–130 and Design at lines 146–166. Therefore the
repair must keep `path_column` across prose and clear it only at headings or section end.

The token sweep found a second width boundary: discovery admits any filename ending in the literal
suffix, including `Surface.cs`, `View.cs`, `_OddView.cs`, and `ÉcranView.cs`. Exact qualified rows
for all four now pass. Recursive directory patterns such as `Workbench/Nested/**` remain relevant
without containing a literal Surface/View filename. Exact non-surface paths such as
`WorkbenchShell.cs` remain outside this gate's jurisdiction.

Root has recorded the recurrence under DC-118 and the enforcement limitation under DC-088 at
`bf6453be`; this bounded branch does not duplicate or edit the shared lesson register.

## Repair and rollback table

| Phase | Change | Failing-first oracle | Exit evidence | Rollback |
|---|---|---|---|---|
| R1 | add the five reviewer cases to the existing self-test | all five fail together on the un-fixed parser | exit 1, 0.9486139 s | revert test-only hunk if Owner rejects the existing contract |
| R2 | align headings, section end, row states, and token jurisdiction | R1 plus the independent reviewer fixture | self-test exit 0; reviewer 9/9 | revert R2 while retaining R1 red evidence |
| R3 | sweep suffix population and recursive-pattern relevance | literal, underscore, non-ASCII and nested `/**` fixtures | included in final self-test exit 0 | revert only the relevance predicate/test additions |
| R4 | independent review and Conductor join | frozen commit plus real Ruling 114 register | pending independent veto and full runner | do not join the author commit |

## Inline adversarial lenses

- **SRE — PASS for diagnosis:** deterministic local fixtures reproduce every symptom; no runtime or
  network dependency obscures the causes. Independent acceptance remains pending.
- **Distributed Systems — N/A:** no concurrency, messaging, retry, or distributed state participates.
- **Data & Persistence — PASS for integrity diagnosis:** §2 remains the sole durable authority and
  no assignment is inferred or persisted by the gate.
- **Domain Researcher — PASS:** surface jurisdiction now equals the discovery language plus its
  approved declaration patterns.
- **Test Architect — BLOCK remains external:** clears only when the independent reviewer accepts the
  frozen repair and the Conductor's integrated mandatory runner passes.

## Status

| | |
|---|---|
| **Completed** | Five-case reproduction, necessary/sufficient cause trace, disconfirmation, sibling sweep, bounded repair, and local/external fixture verification |
| **Remaining** | Independent frozen-commit review and Conductor integration |
| **Best next action** | Freeze and hand the repair commit to the independent reviewer; do not self-clear |

