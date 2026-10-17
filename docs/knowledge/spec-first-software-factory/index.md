---
id: kb-spec-first-software-factory
title: "Spec-first local software factory — research synthesis"
type: knowledge
status: researched
owner: "@timianmalloo"
phase: "research"
tags: [agentic-review, spec-driven-development, factory, desktop]
links:
  - { to: kb-code-knowledge-graphs, rel: relates-to }
  - { to: kb-ai-native-ide-shell, rel: relates-to }
review-by: 2026-11-17
summary: "Spec-first local software factory — research synthesis: dated evidence, confidence and limitations for the local AI-DE workbench evolution."
---

# Research synthesis — 17 October 2026

The canonical framing is **change assurance and work orchestration**, not an autonomous-code
chat dashboard. The user's constraint is explicit: AI-DE remains a local desktop workbench.
This base covers all five requested topics and extends, rather than replaces, existing shell,
code-graph, UML/MDE and operational/test-dashboard research.

## Headline findings

1. **Verified, documented:** review tools now combine contextual analysis, finding verification,
   semantic summaries and retained snapshots. Anthropic's public service, local review,
   ultrareview and open plugin are separate mechanisms; their permissions and scores must not
   be conflated. [Claude service](https://code.claude.com/docs/en/code-review),
   [ultrareview](https://code.claude.com/docs/en/ultrareview),
   [plugin](https://raw.githubusercontent.com/anthropics/claude-code/main/plugins/code-review/README.md).
2. **Verified, documented:** human approvals are product/policy-specific. Anthropic's service
   does not approve PRs; current Copilot docs describe optional approvals in public preview.
   **Inferred:** AI-DE should distinguish assessment, policy obligation and human decision.
   [Copilot](https://docs.github.com/en/copilot/concepts/agent-capabilities/agent-powered-workflows-and-operations/code-review).
3. **Flagged gap:** no validated universal line-count, confidence or coverage cutoff for removing
   human review was established. **Inferred:** route human attention by consequence, uncertainty,
   missing proof and reversibility; calibrate lighter policy against the repository's own outcomes.
   [Anthropic internal practice](https://claude.dev/blog/solving-code-review-at-anthropic/).
4. **Verified, documented:** artifact consistency, implementation convergence and test oracle quality
   are different problems. **Inferred:** use a multi-dimensional drift matrix, never one reassuring
   alignment score. [Spec Kit](https://github.github.com/spec-kit/reference/agentic-sdd.html),
   [Kiro](https://kiro.dev/docs/specs/correctness/).
5. **Inferred from sources and local code:** the missing factory layer is a versioned intent/evidence
   model joining existing sessions, graph facts, tests, assessments and decisions. Rebuilding AI-DE's
   already implemented daemon, terminal and diagram substrate would miss the main opportunity.

## Five requested areas and deliverable map

| Area | Research result | Local implication |
|---|---|---|
| Agentic code review and human escalation | Distinct review mechanisms; review snapshot and guided decisions | Human decision packet with complete-diff escape hatch and omitted-scope disclosure |
| Drift and test gaps | Separate intent, architecture, contract, UX, visual and proof drift | Obligation graph and typed dispositions, not automatic spec rewriting |
| Turn/code summarization | Behavioral delta plus source/test/run identities; diagrams answer different questions | Immutable episode summary with criterion-to-code-to-assertion-to-run trace |
| Copilot, ChatGPT, Grok, Antigravity, Claude, Cursor, JetBrains | Plans, goal supervision, artifacts, walkthroughs and progressive disclosure | Adopt interactions while keeping the desktop/data/execution boundary explicit |
| AI-DE comparison and evolution | Existing graph/session foundations; absent integrated assurance workflows | Three destinations with six contextual journeys and shared evidence |

## Confidence and method

**Verified** means an opened primary source documents a feature or a directly inspected/executed
local artifact establishes a claim. It does not certify vendor efficacy. **Inferred** marks design
proposals. **Flagged** marks weak, conflicting, untested or dated evidence. Vendor telemetry and
research preprints are not independent proof of AI-DE outcomes. Sources were accessed 2026-10-17;
fast-moving documents have a one-month review window. No native deep-research tool was available;
three bounded advisory tracks used primary-page reads, local reconstruction and disconfirmation.

## Design implications — hypotheses, not accepted requirements

Make work items and review decisions primary; keep sessions subordinate. Separate acceptance of
intent, tool authorization and acceptance of a change. Pin every assessment to spec/architecture/
base/head/working-tree identity. Preserve unknown, partial and stale states. Keep code and logs
accessible, and disclose the full coverage of a guided review. Start human review conservatively;
avoid uncalibrated numerical safety claims. The proposal explores these implications.

Read [state of the art](state-of-the-art.md), [comparables](comparables.md),
[reference contracts](references.md), [measures](data-and-constants.md), [glossary](glossary.md),
[open questions](open-questions.md), and the [full source ledger](sources.md).
