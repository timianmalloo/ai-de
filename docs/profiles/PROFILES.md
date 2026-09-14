---
id: session-profiles
title: "Session profiles"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [profile, session-profiler, index]
links:
  - { to: design-session-profiler, rel: relates-to }
review-by: "2027-03-05"
summary: >-
  Index of /session-profiler runs - each row is one measured pass over the harness telemetry, mined by /dream as findings.
---

# Session profiles

*Each row is one measured pass over the harness telemetry (`session-profile.py`). Mined by `/dream` as findings.*

| id | generated | repos | sessions | findings | top |
|---|---|---|---|---|---|
| [sp-0001](sp-0001/profile.md) | 2026-09-10T00:02:35Z | ai-de | 18 | 98 | SP-01, SP-06, SP-09 |
| [conductor-phase1](conductor-phase1.md) | 2026-09-10T00:15:00Z | ai-de (Conductor Phase 1 scope, curated from sp-0001 + the audit log) | 1 | 8 (2 disconfirmed, 1 struck) | PP1-01, PP1-02, PP1-03 |
| [sp-0002](sp-0002/profile.md) | 2026-09-14T01:07:40Z | ai-de | 4 | 27 | SP-01, SP-06, SP-09 |
| [addendum-cd](addendum-cd.md) | 2026-09-14T01:30:00Z | ai-de (the Addenda C/D programme: conductor 919ba21f + 52 nodes + 114 reviews from the subagents store, curated from sp-0002 + the audit log + the ledger) | 1 (+166 sub-agents) | 14 (8 struck) | AC-01, AC-04, AC-10 |
