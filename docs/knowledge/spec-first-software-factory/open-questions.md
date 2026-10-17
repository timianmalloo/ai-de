---
id: kb-factory-open-questions
title: "Open questions, counter-evidence and research limits"
type: knowledge
status: researched
owner: "@timianmalloo"
phase: "research"
tags: [agentic-review, spec-driven-development, factory, desktop]
links:
  - { to: kb-spec-first-software-factory, rel: relates-to }
review-by: 2026-11-17
summary: "Open questions, counter-evidence and research limits: dated evidence, confidence and limitations for the local AI-DE workbench evolution."
---

# Open questions

The local desktop constraint is resolved. These are proposal decisions to settle during `/specify`,
not unanswered blockers to producing this exploration.

| Question | Needed for | Recommendation / evidence gap |
|---|---|---|
| Which accepted spec is authoritative when documents disagree? | Drift baseline | Explicit authority/revision selection; never newest filename alone |
| Who may accept which human-review obligations? | Decision policy | Named lenses and explicit override reason; no model-score gate |
| What local outcome record justifies lighter review? | Calibration | Pilot with historical incidents and conservative sampling; no universal target found |
| Which languages have trustworthy symbol/test links? | Product scope | Start with C# and typed evidence; disclose unsupported paths |
| Which external providers may receive source/review packets? | Privacy and integration | Opt-in per boundary with scope/cost; local UI is not local inference |
| Which flaky-test runner/history formats come first? | Test ingestion | Preserve first attempts, retries, environment and observation window |
| How is semantic redundancy established safely? | Test efficiency | Investigation suggestions with counterexamples; never automatic deletion |
| Which native gestures and accessibility requirements survive the new shell? | Desktop implementation | UIA, keyboard, HighContrast, DPI and large-repo proof before release |

## Strongest counter-evidence

**Verified, scoped:** model-pair effects can reverse; industrial review gains may be proxy metrics;
review golden corpora are incomplete; properties can encode the same wrong assumption as code;
screenshots cannot validate semantics; artifact summaries go stale. Read the limitations in
[cross-model study](https://arxiv.org/html/2607.21656v1),
[RovoDev](https://arxiv.org/html/2601.01129v2),
[ReviewBench](https://github.com/review-bench/ReviewBench),
[Kiro](https://kiro.dev/docs/specs/correctness/),
[visual testing](https://playwright.dev/docs/test-snapshots),
[Sourcery re-review](https://docs.sourcery.ai/reviews/anatomy-of-a-review/).

**Flagged:** the developer-response preprint has an internal chronology inconsistency; Cursor's
review trigger docs conflict; vendor UX was not exercised live; no comparable benchmark establishes
the efficacy or review-time savings of the proposed local AI-DE combination. Popularity numbers are
not ranked because a defensible independent ranking was not established.

## Corrections to inherited research

The August UML/MDE research's flagged absence of LLM/MDE literature must not be repeated as a
current universal claim. Newer spec-grounded testing and convergence work is adjacent evidence,
not a direct proof of full UML-to-code semantic equivalence. The current inventory also corrects
stale comments about AI-DE search/sequence and default daemon use, while preserving genuinely
historical architecture sections. See the forensic report for exact code locations.
