---
id: kb-factory-state-of-the-art
title: "Agentic review, drift analysis and evidence summaries"
type: knowledge
status: researched
owner: "@timianmalloo"
phase: "research"
tags: [agentic-review, spec-driven-development, factory, desktop]
links:
  - { to: kb-spec-first-software-factory, rel: relates-to }
review-by: 2026-11-17
summary: "Agentic review, drift analysis and evidence summaries: dated evidence, confidence and limitations for the local AI-DE workbench evolution."
---

# State of the art

## 1. Agentic code review and human attention

**Verified, documented:** current systems go beyond one prompt over a diff: contextual retrieval,
multiple review passes, verification/deduplication, history, guided summaries and structured findings
are common patterns. This describes implementation approaches, not comparative accuracy.
[Claude service](https://code.claude.com/docs/en/code-review),
[PR-Agent](https://docs.pr-agent.ai/tools/review/),
[Sourcery](https://docs.sourcery.ai/reviews/anatomy-of-a-review/).

Anthropic's October 15 account describes internal risk-tiered human review and feedback from
incidents/reverts. **Verified as a vendor report**, not a causal experiment. Its internal merge
policy is distinct from its public review service. **Inferred:** AI-DE should identify the exact
human decision, reason and evidence, rather than merely adding another review comment stream.
[Internal practice](https://claude.dev/blog/solving-code-review-at-anthropic/).

**Verified:** `/code-review ultra` is an explicitly invoked cloud preview; `/ultrareview` can be
an alias when available. The CLI can return structured results. This is not local execution, and
the open plugin's confidence filter is not a calibrated probability of safety.
[Ultrareview](https://code.claude.com/docs/en/ultrareview),
[plugin](https://raw.githubusercontent.com/anthropics/claude-code/main/plugins/code-review/README.md).

**Inferred decision packet:** requirement and constraint; before/after scenario; a small changed
sequence or dependency slice; production/test evidence; executed checks and gaps; required
decision; full-diff access; snapshot identity. Dispositions must distinguish fixed, false positive,
intentional behavior, deferred and superseded. Resolving a comment alone proves none of these.

## 2. Agentic drift analysis

**Verified:** Spec Kit documents separate consistency and convergence workflows; Kiro derives
testable properties from requirements; Tessl publishes spec-target/test-link verification.
These do not establish repository-wide semantic equivalence.
[Spec Kit](https://github.github.com/spec-kit/reference/agentic-sdd.html),
[Kiro](https://kiro.dev/docs/specs/correctness/),
[Tessl](https://tessl.io/registry/tessl-labs/spec-driven-development/2.0.1/files/skills/spec-verification/SKILL.md).

**Inferred comparison matrix:** intent→behavior; architecture→dependency; API/schema→contract;
UX flow→interaction trace; selected mockup→controlled render; required failure mode→assertion;
accepted scope→unmapped semantic change; evidence→exact baseline. Every discrepancy records
expected and observed versions, confidence, consequence and disposition. PoCs/mockups must state
exploratory/selected/approved/superseded authority. Never silently update a spec to match code.
Executable architecture rules complement agent analysis where constraints are formalizable.
[Spec authority](https://github.github.com/spec-kit/concepts/spec-persistence.html),
[fitness functions](https://www.archunit.org/userguide/html/000_Index.html).

## 3. Test worthiness rather than one coverage score

**Verified:** mutation states and retry outcomes require careful denominators. **Inferred:** show
requirement/failure-mode coverage, execution coverage, assertion strength, boundary fidelity,
useful redundancy, runtime efficiency and reliability separately. A surviving mutant is a lead,
not automatically a missing requirement. Retain runner states including invalid and timeout;
record suspected equivalent mutants as separate review dispositions.
A retry-recovered run remains distinguishable from a clean first pass.
[Stryker](https://stryker-mutator.io/docs/mutation-testing-elements/mutant-states-and-metrics/),
[Playwright retries](https://playwright.dev/docs/test-retries).

**Verified, limited research:** a specification-grounded test-generation preprint reports benefits
on small function-level completeness tasks. That supports testing requirement oracles, not a
repository-scale productivity forecast. **Flagged:** the developer-response paper has chronology
inconsistencies and is excluded from quantitative recommendations.
[Grounding study](https://arxiv.org/html/2607.06636v1),
[flagged response study](https://arxiv.org/html/2607.21997v2).

## 4. Evidence-grounded summaries and diagram choice

**Inferred:** a turn summary is a retained evidence product: intent baseline; behavioral delta;
base/head/content identity; affected symbols/contracts/data; test obligations and actual runs;
changed diagrams; gaps; decisions; remaining work. Stable symbols plus file/range/content identity
are stronger than mutable main-branch hyperlinks. CodeRabbit's snapshots/entity views and
Sourcery's walkthroughs provide documented prior art, not evidence that a generated summary is true.
[Snapshots](https://docs.coderabbit.ai/change-stack/snapshots),
[semantic views](https://docs.coderabbit.ai/change-stack/reading-diffs),
[review anatomy](https://docs.sourcery.ai/reviews/anatomy-of-a-review/).

Use C4 for trust/deployment boundaries, sequence for an operation including failure/cancellation,
class/interface views for contracts, ERM for persisted meaning/cardinality/history, impact graphs
for downstream effect and requirement→code→assertion→run traces for proof. Static calls are
not observed runtime order. Inferred associations and ownership are never promoted by drawing them.
Controlled visual comparison is one evidence source, not a business/accessibility oracle.
[Visual comparison limits](https://playwright.dev/docs/test-snapshots).

## Disconfirmation

**Verified within study scope:** cross-model review can improve or degrade depending on pairing;
more agents is not a universal advantage. Industrial actionability findings are setting-specific.
ReviewBench is useful for reproducible evaluation, but curated golden findings are not exhaustive
defects. **Inferred:** measure local escaped defects, dispositions and review effort together.
[Cross-model study](https://arxiv.org/html/2607.21656v1),
[industrial evaluation](https://arxiv.org/html/2601.01129v2),
[ReviewBench](https://github.com/review-bench/ReviewBench).

## Human questions of a PR

**Verified:** GitHub documents asking about a PR, file or selected lines with that context attached. **Inferred:** AI-DE should pin answers to the accepted intent and change, cite production/test/run evidence, preserve uncertainty, and let unresolved answers become proposed review/test questions without granting approval. A missing answer is not proof the author ignored a concern. [Primary source](https://docs.github.com/en/copilot/tutorials/explore-pull-requests).
