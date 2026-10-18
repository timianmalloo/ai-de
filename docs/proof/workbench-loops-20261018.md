---
id: proof-workbench-loops-20261018
title: Notebook workbench proposal — interaction and review proof
type: proof-pack
status: review
owner: "@timianmalloo"
phase: proposal
tags: [proof, ui, notebook, proposal, research]
links:
  - { to: proposal-workbench-loops, rel: documents }
  - { to: mockup-factory-workbench-v2, rel: documents }
  - { to: spec-workbench-loops-ux, rel: depends-on }
  - { to: review-ui-factory-workbench-v2, rel: relates-to }
review-by: 2026-11-18
summary: Retains 36 passing browser interaction checks, three rendered-view contrast inspections, embedded-proposal parity and independent UX/provider/source reviews. Native implementation remains explicitly unqualified.
---

# Delivered review artifacts

- [Interactive proposal](../proposals/local-software-factory-v2-interactive.html)
- [Canonical proposal](../proposals/local-software-factory-v2.html)
- [Standalone mockup](../mockups/factory-workbench-v2.html), with work/system/PR entry links
- [UX contract](../specs/workbench-loops-ux.html), [source research](../knowledge/workbench-loops-20261018.html)
- [Design token preview](../mockups/workbench-design-language.html)

The source repository baseline is `7e7f73198142cdef0a0816551d412ea67517a93e`. No production C#,
provider configuration or source-pack working files were changed. The three delegated agents were
read-only; root authored and integrated the artifacts. No new worktree was created.

# Executed checks

| Check | Observed result | Evidence |
|---|---|---|
| Headless Chromium interaction gate | **36 passed, 0 failed**, journey endpoint reached, no browser errors, no HTTP/HTTPS calls in standalone fixture execution | [interactions.json](workbench-loops-20261018/interactions.json), `tools/verify-workbench-v2.py` |
| Two entry paths and PR route | Factory/work, Understand/repo and Review/PR reached from interactive proposal | [visual-checks.json](workbench-loops-20261018/visual-checks.json) |
| Rendered text contrast | 388 text-node observations, 24 text/background pair observations across three views, **0 failures** | Same visual-check record |
| Theme/viewport/loop sweep | 18 combinations with nonzero frame and no measured overflow/target failures | Interaction record |
| Hard states | 10 states checked; stale/loading/quota/signed-out prevent new run | Interaction record |
| Source versus embedded mockup | Exact normalized text match; three embedded entry controls work | Visual-check record; `tools/build-workbench-proposal-v2.py` |
| Native route controls | Antigravity and Muse show native-policy/read-only continuation; no invented Allow-once control | Interaction record |
| Token language | `design-lint.py DESIGN.md --strict`: clean, zero warnings | Executed tool output; unchanged token palette/ramp |
| Static craft | Nonempty source scanned; 3 Minor observations, no Major/Blocker | Review artifact explains each, no suppression |
| Loopback boundary | Unknown/private routes `/AGENTS.md` and `/.git/config` return 404; server binds 127.0.0.1 | Visual-check record |

The interaction record includes the SHA-256 of the tested HTML. Checks cover deterministic fixture
behavior, not real provider/model performance or native runtime accessibility.

## Important failure-to-fix observations

Initial generated script syntax errors were observed by a real browser before any interaction
pass was reported. Independent review then reproduced run-reset/provider-relabel bugs, lossy code
projection, stale completion ARIA, hidden-focus docking and invented metadata for an unknown
reference. Each was corrected and retested. The route-specific permission and missing-contract-bound
findings were likewise corrected before final scoped review.

These classes and executable controls are captured in the defect register: DC-039, DC-147, DC-182,
DC-228, UI-REFERENCE-IDENTITY, UI-DOCUMENT-PROJECTION and UI-COMPLETION-ARIA. The interaction gate
fails on an exception or unfinished journey rather than reporting only the completed prefix.

# Independent review

| Reviewer | Scope and observation | Final result |
|---|---|---|
| `/root/product_ux_research` | Source, screenshots and independent headless interactions; keyboard completion, source round-trip, run continuity, focus and unknown reference behavior | PASS for exploratory HTML; final work/system/review/proposal visual sweep found no new blockers |
| `/root/review_drift_research` | Official provider/auth boundaries and final artifact consistency; inspected route-specific permission source | PASS for proposal/fixture; did not invoke providers or claim account qualification |
| `/root/solution_forensics` | Commit-pinned AI-Forward and benchmark source, current AI-DE seams, exact request states and bounded contract | PASS for source/protocol consistency; native behavior separately unverified |

The author did not clear its own accessibility veto. Review verdicts are scoped observations, not
evidence that an LLM adapter or native feature was implemented.

# Visual evidence

[Work-first, dark](workbench-loops-20261018/work-dark.png) ·
[System-first, light](workbench-loops-20261018/system-light.png) ·
[PR review](workbench-loops-20261018/review-dark.png) ·
[Interactive proposal](workbench-loops-20261018/proposal.png).

# Residual limits

The HTML's accounts, runs, PRs and measurements are clearly labeled fixtures. Completion is a local
demonstration index. Contenteditable/source views are bounded review machinery, not the final editor
engine. Native UIA, IME/clipboard/multi-cursor, mixed-DPI floating windows, large documents, live
providers, quota/cancellation semantics and package trust remain the separate
native proof boundary below. Static craft silence is not an acceptance verdict.

Chrome's automation connection and the in-app browser were unavailable in this session. No rejected
direct Chrome launch was retried. Verification used the already-installed headless Chromium test
runtime, and review is served through an artifact-only loopback URL for manual Chrome opening.

## Native proof boundary

**Not a native PASS.**

Medium: native-desktop. Platform: Windows. Framework: existing WPF + WebView2 + AvalonDock.
Accessibility: native UI Automation plus hosted DOM and explicit focus handoff. Distribution:
existing desktop packaging; new release/signing posture is not qualified by this proposal.
HIG: [Windows keyboard interactions](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-interactions)
and [focus navigation](https://learn.microsoft.com/en-us/windows/apps/develop/input/focus-navigation).

| Native claim | Failing input / oracle | Evidence now | Status / residual |
|---|---|---|---|
| Platform interaction fit | Commands cannot be reached by keyboard | HTML keyboard/menu/resize review; real DockHost source inspected | Direction supported; native run needed |
| Focus and UIA | Moving/hiding WebView pane loses focus or name/role | HTML focus regressions corrected | UIA/accessibility inspector recording not performed |
| Rich editor / IME | Composition, paste or undo changes another cell/revision | Bounded rich/source and exact-edit fixtures pass | Native IME, arbitrary Markdown and multi-cursor not qualified |
| Theme / HighContrast | OS resource change hides selection or control | Dark/light/HC-preview HTML checked | OS resource switching and UIA not measured |
| DPI / floating windows | Move between mixed-DPI monitors or restore lost window | HTML in-window float and layout identity checked | Real auxiliary window/DPI proof absent |
| Large documents/lists | 100k-character editor or high-rate event stream stalls input | Small fixture interactions only | Virtualization, batching and latency budgets require measurement |
| OS integration | Terminal/sign-in launch wrong repo or alters policy | Explicit path and native ownership shown as fixtures | Actual launches/credential stores not exercised |
| Provider execution | Denied tool/process exit misread as completed work | Source-grounded capability-specific fixture states | Each selected binary/profile/model must be qualified |
| Packaging trust | Release unsigned or unknown trust posture | No package built in this episode | Signing/SmartScreen/release checks remain required |

These are planned implementation gates, not reasons to pretend the requested HTML exploration is
unfinished. No native markup or production backend was changed, so an XAML token-lint run would not
prove this new surface; it is required when the native slice is built.

# Delivery ledger

The planned tree had three parallel read-only research branches and one writer. All three branches
joined; independent reviewers then reviewed root-authored artifacts. Scope incorporated the user's
three additions without dropping the original objective: explicit native sign-in, core IntelliSense,
and equally legitimate work/system entry paths. Two initial correction rounds exposed a narrower
focus/unknown-metadata issue; the plan was extended for targeted disconfirmation rather than treating
the cap as completion. Final blocking findings are cleared for the HTML/proposal scope.

Durations are captured by the closing skill audit markers. Token cost and spend are not exposed by
this runner and are **not recorded**. This task creates design/research artifacts and a local review
server, not a production deployment or a provider-authentication action.

The desktop contract environment was absent, so no runtime episode registration or scoring is
claimed. The named proof and artifacts are captured through the repository audit. The graph's
100-HTML-surface bound was preserved by folding the small native proof into this pack instead of
weakening the limit or dropping its required rows.
