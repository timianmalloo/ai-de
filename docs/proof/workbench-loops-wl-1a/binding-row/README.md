---
id: proof-workbench-loops-wl-1a-binding-row
title: "Evidence — WL-1a T16: the turn row shows its attempt binding"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  Red and green evidence that the turn row's provenance disclosure and its UIA HelpText show the attempt's revision, request and route (Rulings 161, 162).
---

# T16 binding row

Line: `Sent from revision {n} · request {first 7 of projection_sha} · via {route}`; `Attempt not recorded` with no attempt; a missing field reads `not recorded`.

- `binding-row-red.txt`: 11 of 14 fail on assertions (stubs return the fingerprint / empty string).
- `binding-row-green.txt`: 14 of 14 pass.
- `suite-green.txt`: Core 3018 pass; App 1113 pass plus one timing test that failed in the full run and passed alone.

Tests: `tests/AiDe.App.Tests/Workbench/Records/TheTurnRowShowsItsAttemptBindingTests.cs`.
No HTML view is rendered (surface cap).
