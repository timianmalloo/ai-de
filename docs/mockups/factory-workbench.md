---
id: mockup-factory-workbench
title: Local software factory — interactive workbench exploration
type: doc
status: exploratory
owner: "@timianmalloo"
tags: [mockup, desktop, factory, review, accessibility]
links:
  - { to: plan-factory-evolution-20261017, rel: depends-on }
  - { to: spec-ai-native-ide, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2027-01-17
summary: An exploratory local desktop interaction model with three destinations, shared scope and evidence, and six working factory/review/understanding journeys; all sample metrics are explicitly synthetic.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# Direction before screens

**October 18 iteration:** [Notebook workbench v2](factory-workbench-v2.html) evolves this baseline
with an immersive editor, peer execution, left feature navigation and working docking interactions.
This page retains the October 17 direction as historical context.

Audience: the technically expert owner coordinating several changes and arriving with uncertainty
about what is complete, what is true, and where their judgment is needed. Job: accept intent,
understand a change's evidence, and make the next bounded decision without reading every log.
**The product remains a local desktop workbench.** This HTML is a portable review artifact, not
a proposed browser product or proof of native WPF accessibility.

Adjectives: calm rather than noisy; precise rather than reassuring; connected rather than fragmented.
References: current AI-DE's graph/tree and source drill-down; VS Code's separate plan acceptance;
Antigravity's reviewable artifacts; Junie's guided review narrative; CodeRabbit's immutable snapshots.
The source ledger is in the companion research. Borrow tasks and interactions, not vendor branding.

Archetype Signature: `KeyboardVelocity { Type:OLTP; Arch:SPA; Layout:MasterDetail;
Density:Compact; Nav:Sidebar+CommandPalette; Viewport:DesktopBound;
Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive; Type:Utilitarian; Depth:Flat;
Sync:LocalFirst; Persistence:Session; Feedback:Confirmed; Motion:None; Pacing:Freeform;
Transition:HardCut; A11y:WCAG_2.2_AA; }`.
Read-only architecture and graph lenses additionally use a bounded visual DSS; accepting a
review decision remains serial. No bento dashboard or decorative whole-repository hairball.

Type: existing Segoe UI/Cascadia tokens keep the artifact recognizable as a Windows workbench.
Color: existing dark/light semantic tokens; confidence and urgency always have words.
Space: compact rows with a single selected object, stable detail area, and an optional evidence drawer.
Anti-goals: model-output progress percentages, six competing top-level applications, a permanently
open terminal, automatic approval based on coverage/confidence, or spec rewriting to erase drift.

## Domain model before UI

Proposed bounded contexts: Intent (WorkItem, SpecRevision, AcceptanceCriterion), Delivery
(ChangeSet, Run), Assurance (Assessment, Finding, EvidenceLink), and Decisions (ReviewDecision).
Existing workspace/entity identities are references, not duplicated models. WorkItem is a small
aggregate whose invariant is that acceptance names one exact spec revision and the required evidence;
assessment and decision aggregates pin base/head/content identity and never mutate a prior verdict.

Grain: one evidence-link row is one producer's claim linking one exact criterion revision to one
production/test artifact version; one run-result row is one test attempt in one environment; one
decision event is one actor's disposition at one baseline. Append-only facts carry changes; stable
entities are dimensions. Derive status, coverage and attention from those facts. Counts are additive
only within a defined disjoint population; durations across parallel runs are non-additive for wall
time; pass/flake/coverage rates are non-additive and retain numerator, denominator and window.
Changes to a criterion's meaning create a new revision. Content changes invalidate relevant evidence
and decisions; a mockup's authority is exploratory until explicitly selected or approved.

## Surface and hard-state inventory

Three destinations share project, scope and selection: Factory; Review; Understand.
Six demonstrations: spec-first work/progress, PR review, test worthiness, graph/code understanding,
architecture review, and rigor backlog. PR and Tests are Review lenses; architecture is an
Understand lens. Code and execution are subordinate evidence, not separate primary destinations.

Every destination renders ready, loading, empty, error, stale, partial, and untrusted-output states.
The harness varies persona, viewport, theme, state and reduced motion. Stale evidence blocks
decision recording. Sample action buttons alter local demonstration state only; no provider calls,
Git operations or production approval occur. The mockup includes keyboard focus, accessible
names, a diagram table alternative, zoom and context drill-down, and visible omission counts.

UI triggers: dense desktop analysis and AI uncertainty states apply. The artifact is HTML, so
native DPI, UI Automation, WPF keyboard, windowing and packaging proof remain unverified and belong
to later implementation. Existing DESIGN.md is reused; no production token or shell change is made.

## Interactive artifact

[Open the factory workbench](factory-workbench.html).

Verification and independent critique are recorded in the final Proof Pack. The design is an
exploration, not accepted requirements or a promise that the proposed backends exist.

## PR questions addition

The reviewer can ask arbitrary questions, use retry/recovery/test examples, inspect answer evidence, ask follow-ups and retain unresolved concerns as proposed review/test-gap records. Answers are pinned fixture demonstrations, never live model claims. Stale input blocks a new answer; source absence is not evidence of author intent.

## Authoring expansion and bounded plan

A1 inspect existing editor/history/wiring → A2 versioned draft model → A3 rich document/stack/preview fixture and migration proposal → A4 independent review and interaction checks. This append stays at TierT2/fan-out3 and does not reopen the completed forensic/source review. Factory now includes Prompt editor: contenteditable rich text, bold/emphasis, acceptance/code blocks, block movement, exact replace preview/undo, context chips, back/forward with scratch preservation, saved revision/branch, comparison and canonical Markdown preview. No true multi-cursor engine, model compilation, production persistence or Send is claimed. The user addition was retained verbatim in the audit driving text.
