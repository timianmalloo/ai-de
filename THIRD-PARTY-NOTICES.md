---
id: third-party-notices
title: "Third-Party Notices"
type: doc
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [license, provenance, third-party]
links:
  - { to: project-documents, rel: relates-to }
  - { to: audit-log, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Records the source revision, bundle release, license, and installed surfaces for the Apache-2.0 AI-Forward Pack material included in this MIT-licensed repository.
---

# Third-Party Notices

## AI-Forward Pack

This repository includes installed copies of the AI-Forward Pack and Agent
Knowledge Pack from:

- Source: <https://github.com/timianmalloo/ai-forward>
- Source commit: `07488efcf0a7282c6737120fec7262eba26acb27`
- Bundle version: `2026.08.16.2`
- Bundle revision: `45`
- License: Apache License 2.0
- License copy: [`docs/ai-forward-pack/LICENSE`](docs/ai-forward-pack/LICENSE)

The installed material includes:

- `.claude/`
- `.github/agents/`
- `.github/instructions/`
- `.github/prompts/`
- `.github/workflows/docs-health.yml`
- `docs/ai-forward-pack/`
- `docs/audit/`
- `docs/index.html`
- the managed AI-Forward Pack blocks in `AGENTS.md` and `CLAUDE.md`

Installation relocated files into tool-specific directories, added Copilot
frontmatter wrappers, inserted managed blocks, and pinned the active GitHub
Actions references. Those repository-specific changes do not change the
Apache-2.0 license of the underlying pack material.
