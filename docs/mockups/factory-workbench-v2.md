---
id: mockup-factory-workbench-v2
title: AI-DE notebook workbench — interactive iteration two
type: doc
status: exploratory
owner: "@timianmalloo"
phase: proposal
tags: [mockup, desktop, notebook, docking, intellisense]
links:
  - { to: mockup-factory-workbench, rel: refines }
  - { to: spec-workbench-loops-ux, rel: depends-on }
  - { to: proposal-workbench-loops, rel: relates-to }
  - { to: kb-workbench-loops-20261018, rel: depends-on }
review-by: 2026-11-18
summary: Self-contained HTML exploration with two entry paths, three work loops, left feature navigation, an intelligent continuous editor and dockable execution, visualizer and checklist surfaces. All runs and accounts are fixtures.
review-suggested:
  - { by: spec-workbench-loops-ux, on: 2026-10-18, reason: "Promoted prototype UX contract to the full functional evolution specification: distinct work/system journey purposes, closure, authority, bridges and testable requirements. Reconcile implementation and prototype assumptions before adoption." }
---

# Review the workbench

[Start with work](factory-workbench-v2.html?entry=work),
[start with the system](factory-workbench-v2.html?entry=system), or
[review a PR](factory-workbench-v2.html?entry=review).

This is a **local desktop UX exploration**, not a hosted-product replacement. The HTML runs without
CDNs, a build step, provider credentials or network calls. The default specimen shows an intent
document beside its execution. Factory / Review / Understand and all feature children live on the
left; the right-hand workspace contains movable content panes.

## Direction and task shape

Audience: an expert developer coordinating unfinished work and unfamiliar system behavior. The job
is to formulate intent, investigate a question, inspect evidence and make the next bounded decision.
Both work-first and system-first are legitimate arrival states; neither requires adopting the other.

Archetype: existing `PerspectiveShell / MultiPanelWorkstation` with a G3 `ComputationalNotebook`
authoring surface. Deviations: local persistence, explicit user execution and outputs that can dock
beside their source. Reading is parallel; composing and authorizing one request are serial.

Immersive, deliberate, inspectable; not fragmented, obstructive or theatrical. Existing Segoe UI
and Cascadia tokens preserve native familiarity. Neutral surfaces, one action/focus accent and
continuous prose defend the document as the focal point. Existing typography and spacing tokens
provide hierarchy without cards around every paragraph.

References and their specific patterns: Claude desktop panes; ChatGPT/Codex scoped annotations and
source/output; Copilot notebook context; Grok history and plan comments; Cursor queue versus steer.
Their official sources and limits are in the linked research. The new UX does not copy vendor
branding or assume that a vendor's cloud workflow is available locally.

## Working interactions

- Type `@`, `/` or Ctrl+Space for keyboard-driven fixture completion in rich or source editing.
- Inspect known references/signatures; unknown references explicitly lack indexed evidence.
- Preview and apply a multi-edit, then Undo. Save, branch, compare and revisit prompt revisions.
- Select a range or section and preview its exact request; retain explicit selected context.
- Run the fixture; inspect concrete permission, allow once/reject, queue/steer and stop states.
- Change loops or providers while retaining the old run's immutable route, scope and input.
- Dock, resize by keyboard, float inside the preview, focus, hide and restore panes without losing work.
- Ask about a diagram or code contract; keep an unresolved point in a checklist without claiming a test pass.
- Explore the system with no work item, then explicitly promote a chosen investigation to proposed work.
- Review a pinned PR through its notebook and behavior diff; record a reasoned fixture disposition separately.
- Inspect native sign-in for all six requested harness families; the specimen never signs in or collects a key.
- Open the optional terminal explanation at the repository path; no command runs in the HTML.

The review harness changes persona, viewport, hard state, theme, density, capability and reduced
motion. It checks a nonzero frame before layout/contrast/target measurements. Tests additionally
exercise browser behavior; token ratios alone are not a usability or accessibility verdict.

## Limits

The subsequent [functional specification](../specs/workbench-loops-ux.html) defines the two distinct
journeys and their full acceptance obligations. This earlier prototype illustrates the direction;
its fixture checks do not establish conformance to all 90 criteria. Reconcile those criteria when
implementing or extending the mockup.

Fixture completions are not a live repository index. Contenteditable is a bounded interaction
model; production CodeMirror extensions and rich-document semantics must be qualified. Float is
inside this page, not an OS window. Native UIA, IME, clipboard, high contrast, mixed DPI and actual
provider/coordination execution remain implementation proof. The source observer and benchmark
research establish reusable seams, not completed product integration.

See [independent review](../reviews/ui-factory-workbench-v2.md),
[proof](../proof/workbench-loops-20261018.md) and the
[proposal](../proposals/local-software-factory-v2.html).
