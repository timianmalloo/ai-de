---
id: review-code-atlas-spec-content-gates
title: "Code Atlas specification content gates"
type: doc
status: draft
owner: "@timianmalloo"
phase: "atlas-specification"
tags: [code-atlas, gates, security, testing, ux]
links:
  - { to: note-atlas-draft-content-while-blocked, rel: depends-on }
  - { to: review-code-atlas-data-constraints, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Records independent specification-content findings and separates those gates from future
  implementation proof and external ownership/registration admission. Initial gates are blocked;
  corrections are reviewed against pinned revisions, not guessed from the author's report.
---
# Specification content gates

This record concerns draft content, not product implementation, native proof or registration.
The future-feature test oracles must be falsifiable now; their actual red/green executions belong
to the corresponding implementation slice. A speculative promise cannot replace an oracle,
and an unbuilt feature cannot be required to have already passed before architecture is written.

## Initial pinned reviews

| Gate | Reviewer / pinned draft | Verdict | Required correction |
|---|---|---|---|
| Functional/testability | Test Architect GPT-5.5, `e8c73a03-3fa2-4a77-8d17-68cdf80188b1`, c97d0961 | BLOCK | Per-clause oracle/fixture/falsifier/suite matrix; deterministic E0 subset; physical inventory independent of language support; overload/partial/scope/revision identity cases; measured NFR protocol; source-read vs 79/40 executed baseline boundaries. |
| Security/identity/privacy | Security & Identity GPT-5.5, `3facf06b-883c-4039-a461-51b93a236f11`, c97d0961 | BLOCK | Trusted-side boundary/negative-oracle table; audit/session carriers cannot create authority; verified actor/scope/acceptance/supersession; source binding/TOCTOU; context manifest; per-use processing versus publication; inert content and export. |
| UX/IA/UI | UX & Accessibility GPT-5.5, `c4994e72-ff75-4e75-8a5c-2a31100dca18`, e1722a8f | CONCERNS | Phase-scoped state acceptance; precise future native focus/UIA/theme/HC/reduced-motion and hosted-view/list synchronization criteria only where used by that phase. |
| Data/model | Data & Persistence GPT-5.5, `91c51d36-21fc-4c00-8cf4-57fa50a1cb00` | Advisory constraints | See linked data record; no schema or implementation approval. |

## Clarifications preserved

- "Verified source fact" does not require decision authority. Evidence confidence, origin,
  review disposition and authority are orthogonal.
- Human acceptance does not promote an AI inference to extracted/observed origin.
- A root human owner need not have fictitious delegated authority. Model Owner/Conductor roles
  require accepted delegation within their scope.
- The spec names logical policy responsibilities and required outcomes, not premature production
  class signatures.
- Missing source visibility can be policy-required; unsupported language is not itself a reason
  to omit an otherwise authorized physical path.
- A pinned review tree intentionally does not receive author updates until the Conductor moves
  it to the next reviewed revision. An unchanged old review tree is not evidence that repair failed.

## Pending

The author has committed repairs and is incorporating the final UX scoping clauses.
Final content verdicts will cite the corrected revision. External Core/Claude acknowledgment,
registration, source dispatch and native/runtime evidence remain separately pending regardless
of those verdicts.
