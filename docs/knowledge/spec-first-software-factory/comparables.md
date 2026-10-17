---
id: kb-factory-comparables
title: "Current developer UX and open review ecosystems"
type: knowledge
status: researched
owner: "@timianmalloo"
phase: "research"
tags: [agentic-review, spec-driven-development, factory, desktop]
links:
  - { to: kb-spec-first-software-factory, rel: relates-to }
review-by: 2026-11-17
summary: "Current developer UX and open review ecosystems: dated evidence, confidence and limitations for the local AI-DE workbench evolution."
---

# Comparable experiences — 2026-10-17

All product rows are **Verified as current primary documentation**, not hands-on qualification.
The transfer column is **Inferred**. Local desktop execution and cloud inference are separate axes.

| Product | Documented experience | Transfer to AI-DE | Limit |
|---|---|---|---|
| Copilot / VS Code | Editable plans with inline feedback and distinct plan-only/implementation actions | Accept intent before scheduling execution | Plan acceptance is not tool permission; previews differ by surface |
| ChatGPT / Codex | Local/worktree environments, review pane, goal progress and artifacts | Outcome-first progress and one reviewable result | Goal mode does not widen access; hosted work is a different environment |
| Grok Build | Plan comments, goals, budgets and workflows | Bounded plan/run lifecycle | Plan edit gate excludes shell writes and subagents; do not market it as containment |
| Antigravity | Desktop command center, plans/diagrams/diffs/walkthrough artifacts | Evidence objects and milestone review | Artifact claims still require provenance; review pauses are configured |
| Claude Code | Parallel sessions and Summary/Normal/Verbose; goal coordinator beta | Child sessions behind a work item, progressive disclosure | New project coordinator announcement is select cloud beta, not a local guarantee |
| Cursor | Reviewable plans, quick/deep review and interactive canvases | Persistent interactive review packets | Automatic review trigger wording conflicts; canvas is not an oracle |
| JetBrains Junie | Plan/Goal modes, changed-file inspection/rollback and announced guided walkthrough | Review by meaningful behavior and questions | Context/debugging depend on actual IDE support |

Primary sources: [VS Code](https://code.visualstudio.com/docs/agents/run/planning),
[local environments](https://learn.chatgpt.com/docs/environments/local-environment),
[ChatGPT goals](https://learn.chatgpt.com/docs/long-running-work),
[Grok](https://docs.x.ai/build/features/plan-mode),
[Antigravity](https://antigravity.google/docs/artifacts),
[Claude desktop](https://claude.com/resources/articles/claude-code-desktop-redesign),
[Claude projects](https://claude.com/resources/articles/projects-redesigned),
[Cursor canvas release](https://cursor.com/changelog/cloud-agent-canvases-and-environments),
[canvas retention](https://cursor.com/docs/agent/tools/canvas),
[Junie](https://www.jetbrains.com/help/ai-assistant/junie-agent.html),
[Junie announcement](https://blog.jetbrains.com/junie/2026/06/junie-coding-agent-out-of-beta/).

## Open ecosystem and review services

**Verified:** the public Anthropic plugin and community PR-Agent repository make review workflow
mechanics inspectable; ReviewBench offers reproducible PR evaluation; Spec Kit exposes an SDD
workflow. Qodo commercial and the community PR-Agent project are distinct. **Flagged:** no dated,
independent popularity ranking was established, so star counts are not used to rank effectiveness.
[Anthropic plugin](https://raw.githubusercontent.com/anthropics/claude-code/main/plugins/code-review/README.md),
[PR-Agent](https://github.com/The-PR-Agent/pr-agent),
[ReviewBench](https://github.com/review-bench/ReviewBench),
[Spec Kit](https://github.github.com/spec-kit/reference/agentic-sdd.html).

CodeRabbit documents semantic change organization and snapshots; Sourcery documents review
walkthroughs; Tessl and Kiro provide spec/testing patterns. These are complementary patterns,
not evidence that any one hosted service should become AI-DE's mandatory backend.
[CodeRabbit](https://docs.coderabbit.ai/change-stack/reading-diffs),
[Sourcery](https://docs.sourcery.ai/reviews/anatomy-of-a-review/),
[Tessl](https://tessl.io/registry/tessl-labs/spec-driven-development/2.0.1/files/skills/spec-verification/SKILL.md),
[Kiro](https://kiro.dev/docs/specs/correctness/).

## Comparison with actual AI-DE

**Verified source evidence:** four perspectives already exist (`Perspectives.cs:63–91`); the
Coding layout centers session work with a collapsed terminal (`ZoneLayout.cs:219`); Architecture
already hosts Graph/Tree (`:260`). Registered product surfaces include evidence, class/sequence,
contexts, joins, tree, entry points, conversations and compile history (`SurfaceContentFactory.cs`).
The shell's search provider is implemented (`WorkbenchShell.cs:707,2034`), despite stale scaffold
comments. A full PR workbench, test analytics surface, spec acceptance workflow and general rigor
backlog are not registered. The [build inventory](../../reviews/built-inventory-rev102.md) separates
source presence, partial semantics, future specification and mockup-only evidence.

**Inferred opportunities:** work items own intent and evidence; an attention queue opens precise
decisions; one scope selector and evidence drawer serve PR/tests/architecture/rigor; bounded
graphs answer a question; terminal/code remain one drill-down away. Measure time to locate required
decisions and explain a change, not only throughput or reduced comments.

## Rich authoring beyond the CLI

**Verified local source:** CodeMirror6 already supplies Markdown, edit history and host-fed completion mechanisms, but live completion candidates, optional session catalogs and unsent-session persistence are not fully composed. **Inferred direction:** typed authoring blocks, context insertion, prompt revisions and compare/branch around one canonical compiler. Keep undo, draft navigation and submitted envelopes distinct. See the [authoring inventory](../../reviews/authoring-inventory-rev102.md). The official CodeMirror reference fetch returned403 during this pass; no latest upstream multi-cursor contract is promoted from that failure. Qualify the pinned library before implementation.
