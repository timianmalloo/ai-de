---
id: proposal-code-atlas
title: "Code Atlas - an evidence-first Architecture workspace"
type: doc
status: draft
owner: "@timianmalloo"
phase: proposal
tags: [proposal, architecture, code-understanding, graph, reverse-engineering, ai, ux]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2026-10-12
summary: >-
  An exploratory proposal and interactive browser mockup for understanding a repository through
  structure, entry points, domain candidates, class models, layers and Azure infrastructure.
  Extends the intent of the existing Architecture perspective without changing the active
  shell/session implementation or promoting AI interpretation to extracted evidence.
---

# Code Atlas: proposal, not an approved specification

Open [the proposal](index.html) and [the interactive mockup](mockup.html).
Both are standalone, local HTML documents. The mockup uses a deliberately fictional repository:
its diagrams and source excerpts are fixtures, not findings about AI-DE.

## Scope and authority

User request, 2026-09-12: explore the code/architecture use case in a separate worktree and
produce an HTML proposal and mockup for iteration. No production implementation is authorized.

Base: `8d54aadc`; branch: `proposal/code-atlas`; worktree:
`C:\Projects\ai-de-proposal-code-atlas`.
Claude remains the primary implementation session. This proposal does not edit `src/`, the
shell's registry, `DESIGN.md`, the accepted specs, CI or Claude's lane-owned artifacts.
Machine-local coordination registers this session; the proposal does not reassign ownership.

**Verified from the accepted spec:** Addendum C already defines three perspectives:
Coding, Explore, Architecture. The Architecture rail destination is part of the shell lane.
This proposal fills the content of that destination; it does not add a fourth perspective.
The mockup's rail icon is an illustration of that destination, not an implemented shell change.

**Grounding path:** `spec-addendum-c-perspectives` links to `spec-uml-erm-surfaces` and
`spec-ai-native-ide`; `docs/architecture.md`, C/D.1-C/D.4, supplies the retained second host
and projection boundary. Addendum C A5 explicitly defers D-0 (solution tree), D-1 (entry points),
D-2 (data flow), D-3 (ER), and D-4 (layers/components/code-infrastructure).
Its D-5 and D-6 belong to the conversation compiler and are not this proposal's work.
The existing graphify query returned truncated, largely documentation-level matches; direct
source inspection is used for capability claims, not an inference that the graph is complete.

Corrections to the earlier status analysis: missing old worktrees do not prove the sessions
ended; an unmentioned path does not prove that it is unowned or safe to change. This proposal
uses the current ownership register and an explicit, new artifact scope instead.

## Part A: the problem and conceptual model

**Job:** A developer enters an unfamiliar repository, discovers how work enters it, follows a
specific behavior, understands the domain and architecture, and leaves with a source-backed
explanation they can challenge. An architect additionally compares intended boundaries with
observed dependencies. A platform engineer follows declared deployment structure without
mistaking it for live Azure state.

The conceptual boundary is **repository understanding**, downstream of the existing workspace
authority. A source-backed symbol/resource is not an AI claim about its role.

| Concept | Meaning / invariant |
|---|---|
| Repository snapshot | A named input revision plus dirty-file overlay identity and extraction coverage. Views never silently combine incompatible snapshots. |
| Evidence assertion | Existing store concept: a located claim from a named extractor or observation. Preserve its provenance and limitations. |
| Interpretation | A candidate explanation, grouping or role that references evidence. Human acceptance is an annotation, not a promotion to observed runtime truth. |
| View specification | Scope, selected IDs, lens, filters, expansion and layout. Derived rendering is never a second source of domain truth. |
| Investigation trail | Ordered navigation and pinned questions/evidence. Changing presentation preserves selected identity and scope. |

No database schema, migration, new execution authority or public API is fixed by this proposal.
If approved, the architecture design must choose durable representations and lifecycle policies.
The current single-writer fact store and bounded projection contracts remain authoritative.

## Part B: IA and flows, before visual design

Two navigation representations share one selection: **Graph / Tree**. A separate **question
lens** determines what to examine: Structure, Entry points, Domain, Classes, Layers, Azure,
and Impact. These are not seven perspectives. An inspector carries source, evidence and
candidate interpretations. A trace is a drill-down from an entry point, not a whole-repository
sequence diagram.

```text
Architecture destination
  -> choose repository snapshot and scope
  -> graph overview OR solution tree
  -> choose a node / entry point
  -> inspect source and evidence
  -> choose a question lens / bounded trace
  -> optional AI context preview -> candidate -> accept annotation / reject / correct
  -> bookmark or export the view and its provenance

Missing index -> explain coverage -> show unindexed placeholder -> retry
No matching files -> distinguish no matches from unsupported language
Unresolved dispatch -> explicit gap -> inspect caller / narrow scope
AI unavailable -> static views stay usable
Source changed -> retain snapshot marked stale -> explicit refresh
```

Graph/tree pivot preserves selected ID, scope, search and navigation history. If a selected
symbol is outside the current lens, show why and offer to reveal it; never select a different
symbol silently. No folder name proves a domain boundary. A public method is not necessarily
a use-case entry point. An IaC dependency is not a runtime request.

## Part C: direction brief

**Medium:** browser review artifact for a future Windows WPF + WebView2 native workstation.
Microsoft Fluent conventions and the existing AI-DE `DESIGN.md` govern the direction.
Native UIA, HWND focus handoff, DPI and screen-reader behavior require later native proof;
HTML evidence cannot clear those claims.

**Archetype:** a linked multi-panel workstation, closest to technical catalog G6, with a
spatial exploration canvas and a solution tree. Not a metrics dashboard or a chat-first agent.
The job is parallel reading and progressive narrowing, not serial form entry.

**Qualities:** precise, not overconfident; dense, not cramped; exploratory, not disorienting.

**Type:** existing Segoe UI stack for navigation; Cascadia/Consolas for identifiers and source.
**Color:** existing dark/light semantic tokens; blue for selection, amber for interpretation;
provenance always has text and line style, never color alone.
**Space:** restrained chrome; a large central diagram; compact navigator; inspectable evidence
at the right. Expand by question, not by loading the entire repository.

**References to examine:** VS Code's Explorer/call hierarchy; Visual Studio Code Map's scope
expansion; JetBrains' class/member drill-down; NDepend's dependency matrix; C4/Structurizr's
level separation; Bicep's visualizer and its deployment-dependency semantics. Borrow interaction
principles only; do not copy brand assets or product screenshots.

**Anti-goals:** no graph hairball; no 3D as the default; no decorative dashboards; no
"AI verified" badge; no invented entry-point completeness; no direct model API or Azure
connection; no changes to Coding or Explore.

**Triggered standards:** UI-T1 expert visualization applies (bounded counts, provenance,
stable layouts, accessible alternatives); UI-T3 applies to the proposed model-assisted surface
(context preview, refusal and wrong-answer correction); UI-T4 describes the intended native
product but native verification is deferred, explicitly; UI-T2 does not apply (no generated
imagery). Reuse `DESIGN.md`, do not edit the Shell lane's token source.

## Prototype acceptance contract

1. A visible Architecture rail destination opens this study; Coding and Explore are clearly
   out-of-scope previews, with an explicit return path.
2. Graph/tree pivot preserves selected fixture identity. A filtered-out selection is explained.
3. Every requested lens has a distinct view and source/evidence inspection. Entry-point tracing
   distinguishes candidate sequence, activity and data-flow semantics.
4. Domain candidates cannot masquerade as confirmed aggregate boundaries. Layers carry
   interpretation disclosure. Azure arrows name deployment or proposed runtime relationships.
5. Class diagrams disclose scope and omitted members; progressive detail is operable.
6. AI work is simulated, opt-in and inspectable; no remote requests or provider credentials.
   Rejected suggestions disappear from the candidate overlay, not from source evidence.
7. Empty, loading, failure, partial, stale, overflow and AI-unavailable states are reviewable;
   recovery is reachable without leaving the study.
8. Keyboard operation, visible focus, named controls, dark/light/high-contrast preview,
   compact/wide widths, reduced motion and a readable diagram alternative are provided.
9. Export describes a synthetic fixture, snapshot, scope and provenance; it makes no claim
   to be a real project analysis.

## Bounded execution graph

| Node | Capability | Input -> exit | Dependency |
|---|---|---|---|
| G | Deterministic mechanics | worktree + spec snapshot -> isolated artifact scope | none |
| R | Reasoning | source + primary references -> capability/gap inventory | G |
| P | Reasoning | user goals + spec -> proposal and agreed-for-prototype IA | G |
| M | Reasoning | IA + existing design tokens -> interactive mockup | P |
| V | Independent review + deterministic mechanics | artifacts -> interaction evidence and dispositioned blockers | R, M |
| C | Deterministic mechanics | evidence -> linked, audited, persistent branch | V |

The naive sequence G-R-P-M-V-C unnecessarily makes mockup work wait for all capability research.
R and P/M are independent: the mockup uses explicit synthetic data and promises no current
product capability. They join before the proposal is finalized. Review is not collapsed into
authoring. Width <=3; no parallel writes to the same artifact. Each delegate has its own bounded
file scope and tool budget. On failure, keep a named gap, do not silently drop a requested lens.
Review corrections drain a finite blocker list; two review passes, then report unresolved risks.

**Inferred planning units, not elapsed measurements:** G=1, R=3, P=2, M=5, V=2, C=1.
Serial work T1=14 units; optimized span Tinf=11 units. With p=2 the Brent upper bound is
12.5 units; maximum removable serial wait is three units, before dispatch overhead.
No claim about realized speedup or native performance follows from this estimate.

## Iteration boundary

The HTML proposal contains the full operation catalogue, static/AI split, staged delivery
suggestions and decisions for the next iteration. Browser checks and review outcomes belong
in `docs/proof/code-atlas-prototype.md`. Approval would authorize a subsequent spec/design
slice, not automatically authorize every operation in the catalogue.
