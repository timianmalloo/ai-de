---
id: project-documents
title: "Project Documents"
type: index
status: draft
owner: "@timianmalloo"
phase: ""
tags: [documentation, navigation]
links:
  - { to: architecture, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Map of content for AI-DE project-facing documentation, including root onboarding and legal files that remain outside the docs directory.
---

# Project Documents

This is the project-facing Map of Content. Source documents remain in their
original locations; this map makes them reachable without creating parallel
copies.

## Repository root

- [`README.md`](../README.md) - build, run, layout, and licensing guide.
  The current Windows 10 support floor remains an intended, unencoded
  requirement.
- [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) - recorded source
  revision and Apache-2.0 provenance for installed pack material.
- [`LICENSE`](../LICENSE) - MIT license for AI-DE application code.

## Adopted knowledge graph

- [`architecture.md`](architecture.md) - current-state C4 context/container,
  runtime path, tiers, contracts, and known gaps.
- [`knowledge/glossary.md`](knowledge/glossary.md) - governed shared terms.
- [`ai-forward-pack-adoption.md`](ai-forward-pack-adoption.md) - phased gap
  closure plan.
- [`notes/adoption-boundary.md`](notes/adoption-boundary.md) - why this
  bootstrap records current evidence and does not manufacture history.
- [`audit/audit-log.md`](audit/audit-log.md) - durable activity and decision
  history.

## Browsable surfaces

- [`index.html`](index.html) - Docs Explorer over the derived graph.
- [`audit/index.html`](audit/index.html) - audit and change timeline.

## Deliberately outside the project graph

- `docs/ai-forward-pack/**` is installed methodology, templates, and tooling.
- `.claude/**`, `.github/agents/**`, `.github/instructions/**`, and
  `.github/prompts/**` are operational agent configuration.
- `AGENTS.md` and `CLAUDE.md` are always-loaded instruction surfaces.
- JSONL source records and their JavaScript/HTML projections are represented by
  their Markdown hub nodes rather than treated as separate graph artifacts.
