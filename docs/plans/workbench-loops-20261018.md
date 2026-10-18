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

## Specification follow-up — distinct user journeys

Goal: use the proposal to create AI-DE's functional evolution specification while retaining both
entry paths as distinct user journeys. Done when the single three-layer specification contains
testable functional requirements, independent work/system flows and completion criteria, explicit
bridges, conceptual domain invariants, governance/NFRs, traceability and independent gate records,
then is rendered, indexed and committed. Production implementation is out of scope. Tier T2;
read-only fan-out cap 3. Context ceiling: linked proposal/specifications and bounded reviews.
Main-line budget: one specification, targeted corrections and verified handoff. Compilation
`al-01M58AWB64QBBWSWE0HRWYESA5` passed with no assumptions or decision requests.

The completed proposal/mockup baseline was committed as `ca04e18b` before specification authoring.
The existing `spec-workbench-loops-ux` artifact is promoted in place from a prototype UX contract to
the full evolution specification, preserving links and the existing 100-HTML-surface bound. The
earlier prototype remains recoverable in the baseline commit. Planning, review/decision record and
proof use existing linked artifacts rather than proliferating another parallel document bundle.

| Node | Capability / owner | Inputs | Exit / oracle | Dependency |
|---|---|---|---|---|
| S0 Ground and compile | Deterministic mechanics / root | User request, proposal, existing contracts | Traced dispatchable request and explicit reconciliation targets | None |
| S1 Functional/domain review | Independent review / solution_forensics | Proposal, old contracts, conceptual scope | Data invariants, falsifiable criteria and compatibility gaps identified | S0 |
| S2 Journey/UX review | Independent review / product_ux_research | Two user journeys and three-loop proposal | Distinct outcomes, bridges and recovery obligations identified | S0 |
| S3 Identity/AI governance review | Independent review / review_drift_research | Native profiles, permissions and evidence model | Trust/identity/uncertainty boundaries and negative cases identified | S0 |
| S4 Author canonical spec | Reasoning / root | S0–S3 | Functional → UX → UI, concrete Gherkin and traceability | S1–S3 data |
| S5 Disconfirm and resolve | Independent review + root | Complete draft | No unaddressed hard veto; implementation unknowns explicitly bounded | S4 |
| S6 Render/index/commit | Deterministic mechanics / root | Cleared spec | HTML readable; links/IDs/gates pass; committed artifact | S5 |

Root alone writes. Reviewers do not touch files, invoke providers or create worktrees. All three
reports join before the gate. The loop consumes a finite set of requirements and findings; a new
finding must name the failed clause and evidence. Two targeted correction rounds are the circuit
breaker, not a declaration of completion; re-plan if a substantive veto remains. No scope decision
is invented to remove a question. Existing user preferences settle both journeys, native sign-in,
desktop/docking, first-class intelligent editing/execution and optional terminal.

Immovable floors: conceptual model before UI, explicit old/new contract reconciliation, happy and
negative criteria, independent functional/UX/security review, native-proof distinction, graph and
audit integrity. Surface list: user intent → journey context → notebook/revision → run/evidence →
review/investigation disposition → cross-journey link → persisted user-visible continuation.
Modeled phase work 7 equal units, span 5 and maximum review width 3 (Inferred, not duration claims).
Actual duration comes from audit markers; no token or spend count is invented.

### Specification follow-up outcome

The planned three review branches joined and root remained the sole writer. One semantic
correction distinguished an outstanding review record from satisfied acceptance; a separate
rendering correction replaced overlapping flow labels with numbered routes and open legends.
The targeted re-reviews cleared both findings. All functional, UX and security gates passed for
specification scope. No production or provider changes were made.

The deliverable is one three-layer specification with 30 stories, 90 Gherkin criteria, two journey
flows and complete story-to-verification mapping. The existing HTML surface was upgraded in place
with offline navigation and diagrams; its generator is registered in the derived-view gate. A
deliberately stale generated view was rejected before the final fresh-view check. The Proof Pack
records structural and browser observations separately from future native product acceptance.
