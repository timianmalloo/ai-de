---
id: plan-workbench-loops-20261018
title: Elevate the dockable editor and execution workbench
type: doc
status: draft
owner: "@timianmalloo"
phase: proposal
tags: [plan, ux, editor, coordination]
links:
  - { to: proposal-local-software-factory, rel: refines }
  - { to: mockup-factory-workbench, rel: relates-to }
review-by: 2026-11-18
summary: Evolve the local desktop proposal around three work loops, an immersive editor and execution surface, and current subscription and coordination contracts.
review-suggested: []
---

# Goal and compiled scope

Goal: review and elevate the October 17 proposal and mockup into a dockable local desktop workbench. Done when end-to-end HTML mockups cover Develop/Factory, Review and Understand; navigation is on the left; editor, execution, visualizer and checklist are first-class dockable surfaces; current subscription/coordination evidence is cited; independent review and interaction checks are recorded; and Chrome review links are available. Tier T2; at most three read-only research/review agents. No production implementation, source-pack upgrade, real provider login, API-key setup or automatic repository action is authorized by this design task.

Request clauses: left section subitems (not right-pane feature tabs); preserve dockable shell; prioritize immersive notebook/code-editor authoring and commensurate execution output; compare Claude, ChatGPT/Codex, Copilot, Grok and Cursor; support repo and PR understanding through questions, visualizations and checklists; select subscription-backed local harnesses for all six requested provider families; retire terminal-first prompting while retaining an explicitly opened repo CLI; project current AI-Forward multi-harness and single-harness/subagent coordination into the UX; publish reviewable HTML proposal and complete mockups.

Accepted steering: native harness sign-in and headless bootstrap follow the working benchmark,
without transparent auth negotiation; autocomplete/IntelliSense are core editor requirements;
Start with work and Start with the system are both legitimate first-class entry paths. Neither
path is a straw alternative, and exploration does not require an automatically created work item.

# Execution tree (declared before delegation)

| Node | Owner / capability | Input | Exit / oracle | Depends on |
|---|---|---|---|---|
| G Ground, compile, measure | Root / deterministic mechanics + reasoning | Existing spec, shell, editor, artifacts | Baseline observations and scope captured | — |
| R1 Provider contracts | review_drift_research / reasoning | Official current docs + local interfaces | Sourced capability matrix with unsupported/unknown states | G |
| R2 Desktop/editor comparables | product_ux_research / reasoning | Official UX docs + old mockup | Source-backed patterns and measured baseline critique | G |
| R3 Coordination evolution | solution_forensics / reasoning | Current AI-Forward source and AI-DE seams | Commit-pinned delta and integration contracts | G |
| D Direction and UX flows | Root / reasoning | G + relevant R results | UX alternate/recovery states defined before visual construction | G, R1–R3 |
| B Mockup and proposal | Root / deterministic mechanics + reasoning | D + token system | Self-contained interactive artifacts and migration rationale | D |
| V Independent critique + checks | Read-only reviewer + root mechanics | Rendered B | Findings addressed; bounds and native unknowns explicit | B |
| P Publish views and proof | Root / deterministic mechanics | V | Linked HTML, index, audit, loopback review URLs | V |

All research agents are read-only; root is the only writer. No new worktree is necessary. Data edges join research into design; mutations, approvals and index generation are serialized. Research may finish independently and reports missing evidence rather than inventing support. No reviewer may implement its own proposed fix or clear its own authored surface.

Bounded loops: finite source questions, six provider families, three user loops, enumerated hard states and review findings. Remaining unverified entries/findings decrease to zero or are explicitly Flagged with a product fallback. A circuit breaker (two transient retries, two review correction passes) is a defect signal, not completion. Re-plan if provider authorization is unsupported, native editor feasibility contradicts the proposal, or browser automation is unavailable. Browser fallback must not evade a rejected Chrome launch; provide working loopback URLs for manual Chrome review.

Immovable floors: authoritative UX layer and token system before pixels; baseline measurement; official sources for changing product claims; grounded source contracts; wrong-answer/stale/auth/limit/cancel/error paths; keyboard/contrast/rendered-surface checks; independent accessibility critique; native proof limitations; audit and graph consistency. Surface list: versioned intent/document → session/run/permission facts → adapter events → execution/evidence projections → docked surfaces → review/understanding/checklist actions.

Modeled phase work 8 units, critical span 6, maximum research width 3 (Inferred equal units, not durations). Parallel research removes incidental ordering; shared artifact writes stay with root. No token/spend estimate is presented as measurement. Actual duration is captured by audit markers; the proof records actual gates, rework and unresolved limits.

## Actual delivery

All research branches joined and root remained the only writer. The user's native-auth, IntelliSense
and equal-entry-path additions were integrated before final review. UX disconfirmation required an
additional targeted correction round for hidden focus and unknown-reference metadata; the initial
two-round circuit breaker triggered a re-plan, not a false completion. Provider and coordination
reviews each identified two precise amendments, then cleared the final artifacts.

The final browser gate records 36 passing interaction checks, 18 theme/viewport/loop combinations,
10 hard states and zero browser errors/provider calls. Three rendered layouts provide 388 text-node
and 24 contrast-pair observations, with zero failures. The interactive proposal's embedded source
matches the standalone mockup. Native runtime proof remains explicitly separate. Durations are
measured in the closing audits; tokens/spend are not recorded. See the final linked Proof Pack.
