---
id: note-atlas-delivery-horizon
title: "Code Atlas whole-architecture roadmap and walking-skeleton horizon"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-framing"
tags: [code-atlas, owner-ruling, delivery]
links:
  - { to: note-atlas-next-addendum, rel: depends-on }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Define the complete architecture, but admit the first implementation horizon only through an
  integrated deterministic C# file/type/member/source journey. Later stages require separate admission.
---
# Code Atlas whole-architecture roadmap and walking-skeleton horizon

**Decision source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, 2026-09-12.

**Ruling:** define the whole architecture now; authorize the first implementation horizon only
through an integrated deterministic file-to-type/member-to-source walking skeleton.

**Whole architecture:** revision/dirty-overlay identity -> Core extraction/write pipeline ->
single fact substrate -> bounded projections -> inert Architecture presentation. Share distinct
file/type/member identities, source spans, origin, coverage, selection and Back history.
Define cancellation, unavailable/stale states, revision invalidation, document/decision
correspondence and governed-analysis records without another graph store.

| Stage | Admitted capability / required future admission |
|---|---|
| Walking skeleton | C# first on an actual selectable workspace: project/folder/file inventory, addressable declarations/members, actual source and return history. Unsupported/unindexed files remain visible. Integrated native journey required. |
| Semantic / behavior | Visual-first system/feature views, entry points, concepts + concrete UML, method selection, static sequences/data flow with unresolved steps. Separate design and preceding-exit review. |
| Data / Azure | Evidenced ER cardinalities, code/data/infra joins, named Azure service layers and predicate distinctions. Separate admission. |
| Comparison | Clause -> authorized decision -> implementation; authority/supersession, revision comparison, direction-correct impact, tours and provenance export. Separate admission. |
| Governed AI | Dedicated read-only contract, context preview/bounds, validators, review disposition, reproducible records and independent eval. Separate admission; deterministic operation stays useful without AI. |

**Conditions:** every later stage stays in the delivery ledger with acceptance/dependencies.
No static fixture may close the walking skeleton or programme. Require safe test corpora **and
observed real-workspace runtime journeys**, the Testing Strategy union, E7, red-first and audit.
No E18 programme close is authorized by this framing ruling.

**Confidence:** Verified interpretation of opened proposal/spec evidence. Actual component
contracts and implementation exits remain to be established by execution.
