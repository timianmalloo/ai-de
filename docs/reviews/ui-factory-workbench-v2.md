---
id: review-ui-factory-workbench-v2
title: Notebook workbench elevation — measurements and independent review
type: doc
status: review
owner: "@timianmalloo"
phase: proposal
tags: [ui-review, accessibility, notebook, docking]
links:
  - { to: mockup-factory-workbench-v2, rel: documents }
  - { to: spec-workbench-loops-ux, rel: depends-on }
  - { to: proof-workbench-loops-20261018, rel: relates-to }
review-by: 2026-11-18
summary: Structure-first critique of the October 17 baseline and October 18 elevation, with measured browser checks, independent findings and explicit native proof boundaries.
---

# Outcome

The highest-value change is **one left feature map with a continuous document and peer execution
pane**. This replaces the baseline's tabbed form page and makes both work-first and system-first
journeys available. Independent UX review cleared the exploratory HTML after corrections; source
and provider reviewers separately cleared their scoped contracts. This is not native product PASS.

# Measured before diagnosis

Baseline source was inspected at AI-DE `7e7f7319`, before edits to its historical hub. Graph grounding
followed `proposal-local-software-factory → spec-ai-native-ide / built-inventory-rev102 /
forensic-review-rev102 / kb-spec-first-software-factory`; surface and component files were then opened.
Existing review-suggested flags were retained, not treated as accepted architecture.

| Measure | October 17 baseline | October 18 specimen |
|---|---|---|
| Feature navigation | 3 primary destinations + 9 horizontal subfeatures (source count) | 3 left sections; 4 children per selected section; no right feature tabs |
| Docking bindings | 0 dock/splitter/pointer/drag/separator bindings | Pointer move + keyboard menu/resize, focus/restore, hide and in-window float |
| Editor structure | 1 bordered rounded contenteditable box; 15 button-generation sites + 2 literal button sites around authoring | Continuous document in its own pane; contextual completion/help; rich/source views |
| Execution | Separate Runs table and collapsed Console | Persistent peer run/evaluation surface, immutable input and route; read-only run history |
| Initial rendered controls | Not measured in the old browser session for this review | 43 visible controls across navigation, title bar, editor, run and scope at 1440 × 1000 |
| Initial rendered frame | Not re-qualified | 1440 × 962 px; no page-level horizontal overflow |
| Network/provider calls | Static source had no backend integration | Browser fixture: no HTTP/HTTPS requests in standalone file execution; no provider calls |
| Initial token text contrast | Existing DESIGN.md matrix | Minimum 6.257:1 on dark surface for audited ink roles |
| Three primary views | Not re-qualified | 388 rendered text-node observations, 24 text/background pair observations, zero contrast failures |
| State/density coverage | Existing fixture states | 18 theme/viewport/loop combinations + 10 hard states in the interaction gate |
| Static craft | 1 Minor: flat type hierarchy | 3 Minor reports; no Major/Blocker in the source scan |

Source-site counts and rendered-control counts are different populations and are not subtracted
to manufacture a reduction. The old static detector's small finding count did not prove archetype
fit. The new source scan likewise does not prove the dynamic interaction model.

# Rubric findings and corrections

| Location | Dimension | Severity | Evidence / confidence | Fix and status |
|---|---|---|---|---|
| Baseline navigation | IA / findability | Major | Nine horizontal subfeatures split the feature map; Verified source | Consolidated in the left section hierarchy |
| Baseline authoring | Archetype fit | Major | Bordered form inside page; execution subordinate; Verified source + Inferred task-fit judgment | Notebook occupies pane; execution becomes a peer |
| First v2 navigation/provider selection | Provenance | Major | Independent browser review saw reset/relabel of a waiting run | Run captures route/scope/input; view changes preserve it; regression checks pass |
| First v2 rich/source conversion | Document integrity | Major | JSON fence and blank-line fixture exposed lossy conversion | Unedited source retained; language and internal whitespace preserved in supported projection |
| First v2 completion | Accessibility | Blocker within HTML review | Hidden popup retained expanded/active option; empty results named absent option | Shared ARIA lifecycle; rich/source keyboard completion; independently retested |
| First v2 docking | Keyboard/focus | Blocker within HTML review | Focus remained in hidden menu or BODY | Visible pane/editor focus restoration; independently retested |
| First v2 unknown reference | Evidence truth | Major | Unknown symbol received RunEvent signature/source | Explicit unindexed reference, no substitute; regression check passes |
| First v2 native routes | Capability truth | Major | Generic Allow once appeared for unsupported route | Route-pinned capabilities; native-policy/read-only recovery; agy/Muse tests pass |
| Coordination contract example | Protocol precision | Major | “Deadline” field lacked a deadline; context limit stood in for usage | 480 s deadline, expiry action, usage/tool/fan-out bounds; source reviewer cleared |
| Native execution/UIA/DPI | Proof coverage | Flagged | HTML is not a native runtime | Separate implementation qualification; not waived by browser checks |

## Static craft findings, not suppressions

The detector scanned the nonempty HTML. Three Minor reports remain and are interpreted in context:

- **Hairline border + wide shadow:** only modal/floating elevation uses the existing dialog token;
  docked panes and the continuous document remain flat. Do not remove boundary visibility just to
  satisfy the heuristic.
- **Cramped padding on outer app wrapper:** a native window's title/content/status tiling meets its
  edge. Text-bearing children carry their own insets; adding a padded page around the shell would
  change the medium. Rendered view/overflow checks cover the intended boundary.
- **Flat type hierarchy, 12–22px:** this is the established dense desktop ramp, with 15px prose and
  22px document title, not a marketing hero. The reviewer inspected the focal hierarchy. No
  off-token display scale was introduced to game the detector.

No rule was disabled. Token values and type remain governed by DESIGN.md. The
[token preview](../mockups/workbench-design-language.html) contains 56 hex color values and 38
declared text-pair checks; translucent roles remain in the canonical design language.

# Scorecard and ranked plan

| Dimension | Review outcome | Boundary |
|---|---|---|
| Archetype / IA | Pass for exploration | Both entry paths, three loops, left feature hierarchy |
| Editor / execution continuity | Pass for tested specimen | Real semantic index and complete editor engine remain production work |
| Hard states / recovery | Pass for fixture journeys | Actual adapter event semantics require native qualification |
| HTML keyboard / focus / contrast | Independent review cleared | Not a formal whole-product WCAG certification or UIA proof |
| Craft / focal hierarchy | Direction accepted for exploration | Three documented Minor detector observations remain |
| Provider / coordination truth | Independent scoped reviews cleared | Native sign-in and execution were not invoked |

**Must fix before delivery:** the blocking prototype defects above—completed and retained as
controls. **Should fix next in implementation:** notebook/run identity seam, native editor
qualification, explicit profile bootstrap, and native docking/focus proof. **Worth doing after that:**
larger semantic-completion corpora and measured user-task comparison against CLI workflows.

Simplifier delete-list: remove the 9 horizontal feature destinations; remove the permanently boxed
authoring form; remove the subordinate Console-only execution concept; remove an app-owned auth
broker; remove automatic task creation during system exploration. **net: −13 primary UX obligations**
(nine navigation items plus four rejected concepts; a conceptual count, not a DOM delta).

# Definition-of-done status

Direction, UX layers, token system, complete fixture states, review harness, browser interactions,
static craft, source evidence, independent critique, ranked next steps and linked artifacts are
present. UI-T1/UI-T3/UI-T4 apply; UI-T2 does not. The author did not clear its own accessibility veto.

The native portion of “WCAG 2.2 AA met and evidenced” and “native app surfaces … platform … gates”
remains **unmet for production**, because this task creates HTML exploration. See the
[native proof boundary](../proof/workbench-loops-20261018.html). True multi-cursor, arbitrary Markdown
round-trip, IME/clipboard, OS floating windows, live provider semantics and package trust are not
misrepresented as completed by the prototype.
