---
id: note-atlas-contract-probes
title: "Bounded synthetic Roslyn and isolated contract-test research permitted"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-contract-grounding"
tags: [code-atlas, owner-ruling, spike]
links:
  - { to: note-atlas-e1-identity, rel: depends-on }
  - { to: proof-code-atlas-contract-grounding, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Permits one synthetic Roslyn 4.14 probe batch and isolated existing SDK/Core/store/IPC tests,
  using cached dependencies and ignored worker outputs. This is non-product research, not code admission.
---
# Bounded synthetic Roslyn and isolated contract-test research permitted

**Decision source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, follow-up turn 1,
2026-09-12.

**Ruling:** permit bounded existing SDK/Core/store/IPC tests and one transient Roslyn 4.14
synthetic-source probe before architecture locks the identity scheme.

**Because:** centrally managed Roslyn is pinned to 4.14.0, and K0 leaves source/member identity
and execution of IPC/store invariants incomplete.

**Scope:** temporary probe source/build output in the worker's own ignored artifact/session area.
This is not literally write-free; it is not production implementation dispatch.
Observe documentation IDs and declaration spans for overloads, partial declarations/implementations,
ambiguity/missing IDs and project/scope collisions. Distinguish logical identity from declaration
versions and source-line movement.

**Conditions:** cached dependencies offline; no package upgrade, tracked source/project edit,
private reference corpus copy, user-data/model/cloud call, or shared service/store mutation.
Verify disposable isolation before store/IPC tests, otherwise stop that test.
One probe batch and a targeted existing-suite command; loaded versions, synthetic inputs,
raw output/TRX and failures recorded; tracked files unchanged except the agreed proof report.
Observed API behavior does not itself select the architecture. Native proof and ownership
acknowledgment remain outstanding.
