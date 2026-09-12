---
id: note-coordination-plan-artifact-type
title: "A coordination plan is graph type `doc` tagged `plan`, in docs/coordination/ — the skill's schema says `type: plan`, which docs-graph.py's TYPES rejects; the headings and columns are what /execute-with-coordination parses, so those stay verbatim"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, coordination, docs-graph, frontmatter, schema]
links:
  - { to: coordination-addendum-cd, rel: relates-to }
  - { to: plan-addendum-c-modes, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  The prepare-for-coordination skill's plan schema opens with `type: plan`. docs-graph.py's TYPES
  list (docs-graph.py:46) has no `plan`, and validate fails frontmatter whose type is unknown
  (:752). The two existing execution graphs in docs/plans/ use `type: doc`. The plan follows that
  convention and carries the `plan` tag; every heading and column of the skill's schema is kept
  verbatim because that, not the type field, is what /execute-with-coordination parses (its SKILL.md
  names the headings and never the type). Location: docs/coordination/, as the skill and the brief
  name it — a new directory, one plan.
---

# A coordination plan is `type: doc`, tagged `plan`

*Below-ADR judgement, node P1, 2026-09-11.*

**Observed.** `docs/ai-forward-pack/scripts/docs-graph.py:46` defines
`TYPES = ["knowledge","glossary","spec","architecture","adr","design","design-language",
"investigation","proof-pack","decision-note","threat-model","privacy-review","api","source","doc",
"index"]`; `:752` returns `unknown type: <t>` as a validate problem for anything else. The
`/prepare-for-coordination` schema begins `type: plan`. `docs/plans/conductor-programme.md` and
`docs/plans/addendum-c-modes.md` (the two execution graphs) carry `type: doc`.
`.claude/skills/execute-with-coordination/SKILL.md` says a plan must "parse against the schema" and
names the file path; it never reads `type` (grep for `type:` in that file: no hit).

**Decided.** `docs/coordination/addendum-cd.md` carries `type: doc` and
`tags: [coordination, worktrees, parallelism, plan, …]`; every schema heading (`## Layer state`,
`## Artifact classes`, `## Tracks`, `## Serial spine`, `## Seams`, `## Struck tracks`,
`## Order of operations`) and every column header is verbatim from the skill. Extra sections
(`## §2 rows, ready to apply`, `## Harness qualification`, `## Disconfirm`, `## Status`) are added
after the schema's, never in place of one.

**Rejected.** Adding `plan` to `TYPES` - a pack-script change in a consuming repository (the
`docs/ai-forward-pack/` copy is the only one here), out of this node's scope and a
`/updatepack` conflict later. Recorded as a finding for the pack: the skill's schema and the graph's
type list disagree.

**Falsifier.** `docs-graph.py validate` reports no `unknown type` problem for the plan (checked at
this node's close).
