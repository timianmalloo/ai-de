---
id: ui-review-watcher-observatory
title: "UI Review - Loomkeeper Observatory"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "discovery"
tags: [ui-review, loomkeeper, accessibility, agent-observability]
links:
  - { to: spec-agentic-watcher-substrate, rel: documents }
  - { to: mockup-watcher-observatory, rel: relates-to }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Create-mode review of the Loomkeeper Observatory mockup. The evidence-first G6 structure, hard
  states, score honesty, keyboard treegrid, token discipline, and model-governance controls pass;
  native WPF UI Automation, platform keyboard conventions, system contrast, and multi-monitor DPI
  remain implementation conditions.
---

# UI review - Loomkeeper Observatory

**Mode:** create with adversarial review  
**Surface:** Sessions, Activity, Trace, Weave Scorecard, Message Board, Daydreams, Privacy & Capture,
Watcher Health  
**Reviewed against:** `spec-agentic-watcher-substrate`, `DESIGN.md`, G6 Multi-Panel Data Terminal  
**Reviewers:** UX Researcher/IA, UX & Accessibility, AI Systems Engineer, Native Desktop Developer,
The Simplifier  
**Date:** 2026-08-30

## 1. Verdict

> **PASS-WITH-CONDITIONS.** The mockup is a coherent, evidence-led Observatory rather than a score
> dashboard. The remaining conditions require the native WPF build and real assistive-technology
> evidence.

> **Highest-leverage change completed:** default the surface to the session needing intervention,
> put "Why this needs you" before the score, and keep the score subordinate to hard floors and
> evidence.

| Severity | Open |
|---|---:|
| Blocker | 0 |
| Major | 2 implementation conditions |
| Minor | 4 |
| Nit | 2 |

**Accessibility veto:** PASS-WITH-CONDITIONS. Source and rendered checks establish keyboard selection,
real collapse, one treegrid tab stop, focus styles, reduced motion, token contrast, and target sizes.
The production veto stays conditional on native UI Automation and a real screen-reader pass.

## 2. Measurements

| Metric | Value | Evidence |
|---|---:|---|
| Visible interactive controls in default view | 36 | In-artifact rendered audit |
| Controls below 24 x 24 CSS px | 0 | In-artifact rendered audit |
| Simultaneous primary panes | 3 | Sessions, selected episode, evidence inspector |
| Network calls | 0 | One dependency-free HTML file |
| Competing focal points | 1 | "Why this needs you"; score demoted to supporting evidence |
| Declared type sizes | 6 | `DESIGN.md` type ramp |
| Modes doing the same job | 0 | Surfaces have distinct information jobs |
| Arbitrary design-token references | 0 | `design-lint.py --strict` |
| UI craft detector findings | 0 | `ui-craft-gate.py --gate --a11y-obligation` |
| Worst required audited contrast | 6.26:1 | Blocked/error status on surface; AA body floor 4.5:1 |
| Primary action contrast | 6.62:1 | Accent ink on accent |
| Performance | Not recorded | Static prototype does not prove WPF runtime budgets |

## 3. Findings

| # | Location | Dimension | Sev | Evidence | Recommended fix | Confidence |
|---|---|---|---:|---|---|---|
| 1 | Native WPF build | Accessibility / platform | 3 | HTML ARIA cannot prove WPF UI Automation or Narrator/NVDA output | Map the contract to AutomationPeers/Patterns and capture a real AT trace | Flagged |
| 2 | Native WPF shell | Windows conventions | 3 | Prototype cannot prove F6 pane cycling, Ctrl+Tab, mnemonics, system contrast, PerMonitorV2, or detached-window DPI | Add the native keyboard/windowing contract before implementation and test on mixed-DPI monitors | Flagged |
| 3 | Cross-surface links | Flow / IA | 2 | Board/Daydream links switch surface but do not carry the target selection | Carry session/message/episode identity in navigation | Verified |
| 4 | Empty/error harness | State completeness | 2 | Shell-level empty/error copy is Sessions-oriented even when another surface is selected | Add per-view empty/error content in the production design | Verified |
| 5 | Repository/worktree group rows | Match to model | 2 | Selecting a non-episode group retains the selected episode detail in the static mock | Render a group summary or clear the episode detail | Verified |
| 6 | Virtualized treegrid | Accessibility/performance | 2 | Mock uses static representative rows with `aria-rowcount=50` | Production must maintain live row indices, focus identity, and set size under virtualization | Flagged |
| 7 | Watched-agent feedback | AI-surface honesty | 1 | Agent-safe copy reveals that two held-out signals were omitted | Omit the count; disclose only that held-out signals are withheld | Verified |
| 8 | Top-level surface tabs | Windows keyboard | 1 | Buttons are clickable and focusable but native Ctrl+Tab/F6 behavior is not represented | Specify native shortcuts in WPF | Flagged |

## 4. What passed

- **Archetype fit:** G6 linked panels fit parallel monitoring and evidence drill-down.
- **Flow and IA:** Sessions, Message Board, Daydreams, Privacy, and Watcher Health remain distinct and
  cross-linked; blind spots have remediation; Daydream promotion and retraction are visible.
- **State completeness:** hard states are selectable in the harness, including blocked, disputed,
  recomputing, quarantined, partial deletion, failed retraction, and grader unavailable.
- **Token discipline:** both inward and outward controls are clean.
- **Accessibility floor in the prototype:** keyboard row selection, true collapse, roving tab stop,
  ARIA tab/tabpanel relationships, reduced motion, 24px targets, and computed contrast.
- **AI honesty:** partial scores are not rescaled, blocked floors suppress the headline, model/prompt/
  rubric versions are visible, injection is quarantined, local-grader failure becomes Not Recorded,
  and promotion stays human-gated.
- **Numeric honesty:** tabular figures, explicit units, no rainbow scale, no unsupported gauge.

## 5. Generic-tells self-check

| Tell | Present? | Disposition |
|---|---|---|
| Default violet/indigo gradient | No | Existing restrained evidence palette |
| Everything in identical cards | No | Panes, rows, rules, tabs, and spacing carry distinct hierarchy |
| Three equal stat tiles as opening | No | Sessions requiring attention are the focal point |
| Uniform spacing | No | Tight row rhythm, wider pane/group separation |
| Flat type hierarchy | No | Six-step type system; score deliberately demoted |
| Placeholder copy/data | No | Real repository/session names, goals, errors, and limits |
| Emoji as icon system | No | Inline stroke icons and text glyphs with accessible names |
| Happy-path only | No | Thirteen review states plus per-surface failure examples |
| Symmetry without priority | No | Attention-first selected session and asymmetric inspector |
| Motion everywhere or nowhere | No | Micro selection feedback; operational changes hard-cut |

## 6. Simplifier delete-list

```text
delete:  duplicate local-grader harness control; grader-unavailable is one state, not a second mode.
shrink:  the score from a hero number into supporting evidence beneath the intervention.
yagni:   no personnel leaderboard, auto-promotion, external grader, or consensus cluster.
keep:    Activity and Trace are separate because chronology and causality answer different questions.
keep:    constrained viewport modes are review stress tests; production remains desktop-bound.
net: -1 redundant control; no remaining surface can be deleted without dropping an explicit user goal.
```

## 7. Ranked plan

**Must fix before production**

1. Map the treegrid/tabs/live-region contract to WPF UI Automation and record a real Narrator/NVDA
   trace.
2. Define and prove Windows keyboard/windowing behavior: F6/Shift+F6, Ctrl+Tab, access keys, system
   contrast, PerMonitorV2, and mixed-DPI detached panes.

**Should fix next**

1. Carry selection identity through cross-surface links.
2. Add per-view empty/error states.
3. Render group summaries for repository/worktree rows.
4. Prove virtualized focus and row semantics at the reference corpus.

**Worth doing**

- Remove the held-out-signal count from the watched-agent projection.

> **Do this one first:** define the native WPF accessibility/keyboard contract, because the HTML
> artifact cannot prove the highest-risk implementation boundary.

## 8. Residual risk

- No real WPF renderer, UI Automation tree, screen reader, or mixed-DPI window was exercised.
- The static mockup does not prove p95 performance at 50 sessions / 100,000 spans.
- Model evaluation controls are represented, not implemented; enforcement belongs to the eval
  harness and Test Architect gate.

## 9. Defect-class instances

| Existing class | Instance found during this run | Control |
|---|---|---|
| DC-025 - Absence rendered as success | The first partial state kept a complete `/100` score while evidence was missing | Partial state now renders `earned / observed weight`; rendered and AI review gates check it |
| DC-034 - Affordance present but wired to nothing | The first treegrid moved keyboard focus but Enter/Space did not select the session | Shared `selectRow` path now serves click and keyboard; source re-review confirmed it |

