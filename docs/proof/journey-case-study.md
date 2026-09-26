---
id: proof-journey-case-study
title: "Proof note — journey case study"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "0"
tags: [proof-pack, case-study, audit-log, evidence]
links:
  - { to: journey-case-study, rel: relates-to }
  - { to: plan-journey-case-study, rel: depends-on }
  - { to: audit-log, rel: depends-on }
review-by: 2027-03-26
summary: >-
  How the journey case study’s counts were produced, which links were checked,
  and which claims were deliberately not re-measured.
---

# Proof note — journey case study

The essay is [docs/journey/index.html](../journey/index.html). This note is the measurement record for it. Counts below are from the committed ledger at `88e0c33f`, read before this branch appended anything.

## Census

Both jsonl files parsed with the standard library. Zero decode failures. Prompt identity is the prompt string with whitespace folded to single spaces.

| Quantity | Value |
|---|---|
| Audit rows | 796 |
| Change rows | 155 |
| Prompt entries | 132 |
| Distinct prompt strings | 71 |
| Multiplicity | 50 once, 16 twice, 3 four times, 1 six times, 1 thirty-two times |
| August prompt entries, before 2026-09-01 | 78 entries, 22 distinct |
| September prompt entries | 54 entries, 49 distinct |
| The 32-count string | `do the next steps you have listed provide the standard status and next steps tables afterwards` (all 32 in August) |
| Session ids | 130 |
| Audit span | 2026-08-23T19:13:43Z to 2026-09-18T19:53:30Z |
| Change span | 2026-08-23T20:09:31Z to 2026-09-15T17:55:28Z |

Skill-field counts used on the page: implement 290, execute-with-coordination 93, investigate 68, ui-design 32, specify 19, define-architecture 17, design-slice 14, optimize-graph 12, dream 11, collectknowledge 6, document 5, prepare-for-coordination 4, adddomainexperts 1, adopt 1.

Change kinds: architecture 68, design 51, decision 17, knowledge 15, spec 4.

File counts from `glob` on this commit: 38 ADR markdown files, 20 mockup HTML files, 15 specification markdown files, 74 proof markdown files, 14 `INV-*.md` investigations, 125 notes, 15 `docs/knowledge/*/index.md` topic indexes.

`git log -1` on this commit: `88e0c33f0b7c419b1e64d87686987374285292d8`, 2026-09-18 14:11:20 -0700, subject `merge(canvas-p1): the two false numbers on the canvas`.

## Links

Relative `href` and `src` values in `docs/journey/index.html`, and markdown links in `docs/journey/index.md`, were resolved against the file’s directory. The result of that check is recorded at the bottom of this note after the run. Links on `site/*.html` into `docs/` are written for the Pages layout (`site/` at `/`, `docs/` at `/docs/`) and 404 from a `file://` open of `site/index.html`. That trade is pre-existing and stated in `site/README.md`. The new Journey nav item uses the same shape.

## Freshness

`docs-graph.py freshness` was run after derive. Its summary is appended below. A graph-freshness result is not a re-reading of API doc comments. `/document` was not run. The public-symbol figure on the site was not re-counted by hand.

## Claims not re-measured

- `al-0011`’s “69×” describe-query figure and the 5-of-8 budget failure. Cited as the ledger entry.
- `al-01M2V19J6D5PFZVEADASW69P7P`’s account of CI run 35379686294. Cited as the ledger entry.
- `graphify-out/GRAPH_REPORT.md` is stamped from commit `75198484` (2026-08-23). It was not used.
- `al-0002` names `docs/proof/adoption-proof.md` and a glossary. Neither path exists at `88e0c33f`. The page says so.
- The map of content links `security/threat-model.md` and `security/privacy-review.md`. Those two paths do not resolve. The files present are `docs/security/ai-native-ide-threat-model.md` and `docs/security/ai-native-ide-privacy-review.md`. Pre-existing. Not changed here.
- `AIDE_CONTRACT_LOG` was unset in this session, so no episode-close line was written.

## Worktree

Branch `feature/journey-case-study`, tree `C:\Projects\ai-de-feature-journey-case-study`, base `88e0c33f`. The tree is kept until the branch is merged or cleanup is explicitly requested.

A first `prompt-log add` was executed with the shell still in the primary checkout. Those two audit files were restored with `git checkout` before the prompt was logged again in this worktree.

## Check results

Link check, 26 September 2026, over `docs/journey/index.html` and `docs/journey/index.md`: 105 relative references, 0 missing. Exit 0.

`docs-graph.py derive`: 546 entries written to `docs/docs-index.js`.

`docs-graph.py validate`: 546 artifacts, 0 problems, 0 stale, 0 orphans, 0 index drift, 0 defects. 75 pre-existing `review-suggested` flags, gate left on warn. None of the three new ids (`journey-case-study`, `plan-journey-case-study`, `proof-journey-case-study`) is in that flagged list.

`tools/verify-site-figures.py --update` rewrote 6 bound cells, then a check with no `--update` verified 14 figures. The moves are the ones this branch caused: artifacts 543 → 546 (three new docs), ledger 951 → 953 and audit entries 796 → 798 (the logged prompt plus this skill entry).

The closing audit entry is `al-01M3F412065SY7159SFHJC7J9A`. Its `duration_seconds` is 502.0, measured from the start marker at 2026-09-26T14:57:44Z. That marker was set after grounding reads had already happened, so 502 seconds is the write-and-check portion, not the whole turn.
