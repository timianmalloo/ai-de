---
id: adr-0014-accessibility-posture
title: "ADR-0014 — Accessibility is best-effort, not a conformance target, and holds no veto"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "0"
tags: [architecture, accessibility, scope, governance, wcag]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: refines }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: adr-0005-terminal-runtime-boundary, rel: relates-to }
review-by: 2027-02-26
summary: >-
  The product owner has decided AI-DE is not optimising for accessibility. WCAG 2.2 AA is withdrawn
  as a conformance obligation and the UX & Accessibility lens no longer holds a hard veto. Existing
  accessibility work is retained because it is built and passing; it stops being a gate, and every
  artifact that asserted the obligation is corrected so the repository does not claim conformance it
  is not pursuing.
---

# ADR-0014: Accessibility is best-effort, not a conformance target, and holds no veto

- **Status:** Accepted
- **Date:** 2026-08-26
- **Deciders:** Product owner (explicit direction)
- **Context spec/architecture:** docs/specs/ai-native-ide.md Part C · ADR-0012 · ADR-0005

## Context

The spec asserted in six places that AI-DE is "under a WCAG 2.2 AA obligation", and the UX &
Accessibility lens held a **hard veto** derived from it. That obligation was load-bearing in at least
two prior decisions:

- **ADR-0012** chose AvalonDock *plus an owned accessibility layer* specifically because the library
  ships zero `AutomationPeer` types, and justified the extra layer by the obligation.
- **Spike S3** (terminal renderer) was scoped so that "the a11y contract is a hard veto" was its
  primary selection criterion — the renderer would be chosen by which one met a screen-reader
  contract.

The product owner has directed that the accessibility veto be suppressed and that the product is not
optimising for accessibility.

## Decision

**WCAG 2.2 AA is withdrawn as a conformance obligation.** Accessibility is best-effort: worth doing
where it is cheap or already done, never a gate.

1. **The UX & Accessibility lens holds no hard veto on this product.** Its findings are advisory.
2. **No artifact may claim WCAG 2.2 AA conformance**, an accessibility obligation, or a conformance
   level the project is not pursuing. Every such claim is corrected in the same change as this ADR.
3. **Existing accessibility work is retained, not removed.** The automation-name pass, the
   announcement channel, the keyboard command path and their tests are built, passing, and cost
   nothing to keep. Deleting working code to express a change in priority would be pure destruction.
4. **Accessibility no longer drives selection decisions.** S3's renderer criteria are re-weighted to
   fidelity, throughput, input handling, licensing and integration cost.
5. **NVDA verification is closed as not-planned**, rather than left open as a pending obligation.

## Consequences

**What this buys.** S3 stops being gated on a screen-reader contract in a control category the UIA
probe already showed to be weak, which was the most likely source of a Phase-2 redesign. Renderer
selection becomes a straightforward engineering comparison.

**What it costs, stated plainly so it is not rediscovered as a surprise.** The product will not be
usable by someone relying on a screen reader, and will likely fail keyboard-only use in places once
features land that nobody checks. This is a deliberate, recorded product decision for a single-user
local developer tool, not an oversight — and it is the kind of decision that is expensive to reverse
late, because retrofitting accessibility into a rendered surface is materially harder than building
it in.

**What does NOT change.** Keyboard operability is retained as an *ordinary product requirement*
rather than an accessibility one: this is a developer tool whose users work from the keyboard, and
US-9's keyboard paths are already built and tested. The distinction matters — those tests stay green
because they describe how the product works, not because a conformance level demands them.

**ADR-0012 is not invalidated.** Its choice of AvalonDock over the alternatives rested on more than
accessibility (licence, maintenance, layout persistence, floating windows). The owned accessibility
layer it specified is already built; it simply is no longer the justification for the decision.

**Residual risk.** If this product is ever distributed beyond its single user — particularly into any
organisation with a procurement or legal accessibility requirement — this decision must be revisited
*before* that happens, and the retrofit cost will be significant. That trigger is recorded here
because a decision like this is normally discovered, not remembered.

## Alternatives considered

- **Keep the obligation and simply not enforce it:** rejected. It would leave six committed artifacts
  asserting a conformance level the product does not meet — the exact defect class (DC-001/DC-002)
  this repository spent the session eliminating. An unmet stated obligation is worse than a recorded
  decision not to pursue it, because it misleads a reader who has no way to check.
- **Delete the existing accessibility code:** rejected. It is built, passing, and free to keep;
  removing it would reduce quality to express a priority.
- **Downgrade to WCAG 2.2 A:** rejected. The owner's direction is not to optimise for accessibility
  at all, and a lower conformance target is still a target with gates attached.
