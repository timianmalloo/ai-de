---
id: kb-factory-sources
title: "Primary source ledger and access dates"
type: knowledge
status: researched
owner: "@timianmalloo"
phase: "research"
tags: [agentic-review, spec-driven-development, factory, desktop]
links:
  - { to: kb-spec-first-software-factory, rel: relates-to }
review-by: 2026-11-17
summary: "Primary source ledger and access dates: dated evidence, confidence and limitations for the local AI-DE workbench evolution."
---

# Source ledger

Accessed 2026-10-17. Sources were opened by the bounded research branches; the coordinator
also directly opened load-bearing review, spec-testing, mutation/retry and vendor UX sources.
**Verified** below means the source documents the feature, not independent efficacy. R20 is
explicitly **Flagged**. Living documents require refresh before an integration is implemented.
No vendor product or cloud review was invoked, purchased or connected during research.

| ID | Primary source | Published/updated | Documented finding and limitation |
|---|---|---|---|
| R01 | [Anthropic internal code review](https://claude.dev/blog/solving-code-review-at-anthropic/) | 2026-10-15 | Risk-directed human review, commit-bound decisions and outcome feedback. Internal observational account; no causal transfer guarantee. |
| R02 | [Claude Code Review service](https://code.claude.com/docs/en/code-review) | living docs | Verified/deduplicated findings; public service does not approve or block PRs. Missing test coverage is not its default correctness target. |
| R03 | [Claude ultrareview](https://code.claude.com/docs/en/ultrareview) | living docs | Explicit cloud review and JSON CLI; preview availability and operating limits are not safety thresholds. |
| R04 | [GitHub Copilot review](https://docs.github.com/en/copilot/concepts/agent-capabilities/agent-powered-workflows-and-operations/code-review) | living docs | Agentic context and optional preview approvals; new commits invalidate approval. Distinct from default advisory review. |
| R05 | [CodeRabbit walkthroughs](https://docs.coderabbit.ai/pr-reviews/walkthroughs) | living docs | Semantic summaries, sequence diagrams and linked-issue gap discussion; generated analysis remains fallible. |
| R06 | [CodeRabbit semantic diffs](https://docs.coderabbit.ai/change-stack/reading-diffs) | living docs | Entity/range review beyond files; code lookup is heuristic, not complete reference resolution. |
| R07 | [CodeRabbit snapshots](https://docs.coderabbit.ai/change-stack/snapshots) | living docs | Pinned reading snapshots and stale-state handling; distinguish historical reading from live actions. |
| R08 | [GitHub ReviewBench](https://github.com/review-bench/ReviewBench) | repository accessed 2026-10-17 | Reproducible PR mirrors and human-reviewed findings; finite curated and AI-heavy discovery corpus is not exhaustive truth. |
| R09 | [Spec Kit agentic SDD](https://github.github.com/spec-kit/reference/agentic-sdd.html) | living docs | Artifact consistency and implementation convergence are different workflows; convergence is not an equivalence proof. |
| R10 | [Spec Kit spec persistence](https://github.github.com/spec-kit/concepts/spec-persistence.html) | living docs | Living specs, historical feature records and flow-back reconciliation require explicit authority conventions. |
| R11 | [Kiro correctness](https://kiro.dev/docs/specs/correctness/) | 2026-08-04 | Requirement-derived properties and executable links; weak properties can pass, and randomized testing is not formal proof. |
| R12 | [Tessl spec verification](https://tessl.io/registry/tessl-labs/spec-driven-development/2.0.1/files/skills/spec-verification/SKILL.md) | version 2.0.1 | Published verification procedure checks targets/test links and contradictory behavior; no measured recall established. |
| R13 | [ArchUnit user guide](https://www.archunit.org/userguide/html/000_Index.html) | version 1.5.1 | Executable dependency/layer/cycle rules. Java-specific implementation; borrow the fitness-function pattern for .NET. |
| R14 | [Stryker mutant states](https://stryker-mutator.io/docs/mutation-testing-elements/mutant-states-and-metrics/) | living docs | Mutation categories and denominators matter; timeouts and invalid/equivalent candidates need separate interpretation. |
| R15 | [Playwright visual comparisons](https://playwright.dev/docs/test-snapshots) | living docs | Reviewed baselines and controlled environments detect rendered differences, not business or accessibility correctness. |
| R16 | [Playwright retries](https://playwright.dev/docs/test-retries) | living docs | First-pass, retry-recovered flaky and failed are distinct classifications; retry recovery is not a clean first pass. |
| R17 | [RovoDev industrial evaluation](https://arxiv.org/html/2601.01129v2) | 2026-01-20 | Actionability/context evaluation in one industrial setting; older model and proxy measures limit generalization. |
| R18 | [Specification grounding and test effectiveness](https://arxiv.org/html/2607.06636v1) | 2026-07-07 | Controlled function-level evidence favors specification-grounded tests; preprint and narrow scope do not establish repository-scale gains. |
| R19 | [Cross-model code review study](https://arxiv.org/html/2607.21656v1) | 2026-07-22 | Model-pair-dependent gains/regressions in static tasks; no universal reviewer ranking or agent-count advantage. |
| R20 | [Developer review responses](https://arxiv.org/html/2607.21997v2) | 2026-07-29 | Flagged: reported collection chronology is inconsistent; do not use comparative headline metrics as current efficacy evidence. |
| R21 | [PR-Agent community repository](https://github.com/The-PR-Agent/pr-agent) | repository accessed 2026-10-17 | Self-hostable community project, distinct from Qodo commercial; popularity is not effectiveness. |
| R22 | [PR-Agent review workflow](https://docs.pr-agent.ai/tools/review/) | living docs | Priority files, failure scenarios, ticket compliance and partial-review disclosure; configuration changes actual coverage. |
| R23 | [Sourcery review anatomy](https://docs.sourcery.ai/reviews/anatomy-of-a-review/) | living docs | Reviewer guides and sequence diagrams; re-review does not necessarily refresh every narrative artifact. |
| R24 | [Anthropic review plugin](https://raw.githubusercontent.com/anthropics/claude-code/main/plugins/code-review/README.md) | repository accessed 2026-10-17 | Open multi-reviewer workflow and confidence filter; this plugin is separate from built-in local/cloud/service products. |
| R25 | [CodeRabbit architecture review](https://docs.coderabbit.ai/security/architecture-review) | living docs | Architecture/trust-boundary review with evidence gaps; explicitly not a repository-wide audit. |
| U01 | [VS Code planning](https://code.visualstudio.com/docs/agents/run/planning) | 2026-10-14 | Reviewable plans and separate plan-only approval versus implementation/tool permissions. |
| U02 | [ChatGPT local environments](https://learn.chatgpt.com/docs/environments/local-environment) | living docs | Local/worktree execution and review actions; cloud is a separate environment. |
| U03 | [ChatGPT long-running work](https://learn.chatgpt.com/docs/long-running-work) | living docs | Outcome, constraints and verification drive goal progress; goals do not expand permissions. |
| U04 | [Grok Plan Mode](https://docs.x.ai/build/features/plan-mode) | 2026-07-21 | Line/range plan feedback. Edit-tool gating excludes shell writes and parent gating does not cover subagents. |
| U05 | [Grok modes/goals](https://docs.x.ai/build/modes-and-commands) | 2026-10-08 | Goal and budget controls and workflow orchestration; documented controls need runtime qualification. |
| U06 | [Antigravity overview](https://antigravity.google/docs/overview) | living docs | Local desktop command-center and companion surfaces; earlier Agent Manager framing is not the complete current product. |
| U07 | [Antigravity artifacts](https://antigravity.google/docs/artifacts) | living docs | Plan/diagram/diff artifacts and inline milestone feedback; artifact existence does not establish truth. |
| U08 | [Antigravity walkthrough](https://antigravity.google/docs/walkthrough) | living docs | Change summaries and visual evidence; pause/review behavior depends on configuration. |
| U09 | [Claude desktop redesign](https://claude.com/resources/articles/claude-code-desktop-redesign) | 2026-04-14 | Parallel sessions and Summary/Normal/Verbose progressive disclosure. |
| U10 | [Claude projects redesign](https://claude.com/resources/articles/projects-redesigned) | 2026-09-17 | Goal coordinator and child-thread inspection; announcement scopes this to a select cloud-session beta. |
| U11 | [Cursor plan mode](https://prod.cursor.com/docs/agent/plan-mode) | living docs | Review/edit plans before Build and retain plan artifacts. |
| U12 | [Cursor Agent Review](https://prod.cursor.com/docs/agent/agent-review) | living docs | Quick/deep review; docs give inconsistent trigger descriptions, so exact automatic timing remains Flagged. |
| U13 | [Cursor cloud canvases](https://cursor.com/changelog/cloud-agent-canvases-and-environments) | 2026-10-16 | Persistent interactive reports/canvases beside agent work; a canvas is not semantic traceability. |
| U14 | [JetBrains Junie](https://www.jetbrains.com/help/ai-assistant/junie-agent.html) | 2026.2 docs | Plan/Goal modes, operation approval, changed-file diff inspection and rollback. Context is bounded, not omniscient project understanding. |
| U15 | [Junie GA guided review](https://blog.jetbrains.com/junie/2026/06/junie-coding-agent-out-of-beta/) | 2026-06 | Announced requirements/design/delivery/testing plans and guided review walkthrough; runtime UX not exercised here. |

Cursor retention supplement: [Canvases](https://cursor.com/docs/agent/tools/canvas) documents saved, reopenable workspace canvases; the U13 changelog establishes the dated cloud release. Accessed2026-10-17.

PR-question addition, accessed2026-10-17: [GitHub PR exploration](https://docs.github.com/en/copilot/tutorials/explore-pull-requests) documents PR/file/line questions and context. Together with the Cursor retention supplement this gives42 distinct primary-source entries. Documented capability is not answer correctness.
