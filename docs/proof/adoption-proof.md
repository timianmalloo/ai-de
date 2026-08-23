---
id: proof-adoption
title: "AI-DE Adoption Proof Pack"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: ""
tags: [adoption, proof]
links: []
review-by: 2026-11-21
review-suggested: []
summary: >-
  Claim-by-claim evidence that repository publication and the AI-Forward adoption baseline are persistent, graph-valid, buildable, browsable, and honestly scoped.
---

# AI-DE Adoption Proof Pack

## Claims

| Claim | Evidence | Source / version | Oracle | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| The repository exists publicly under the requested account and the published baseline matches local history. | `gh repo view` reported `timianmalloo/ai-de`, `PUBLIC`, default branch `main`; local and remote `main` both resolved to `ef30e96e1386b597ffee3ecef0403b11654fdf9d`. | GitHub CLI 2.96.0; `origin=https://github.com/timianmalloo/ai-de.git` | Missing/private repository, wrong owner, or unequal SHAs fails. | Yes - `gh repo view timianmalloo/ai-de` returned not found before creation. | **Verified** | The adoption commit is not yet represented in this baseline row; the final audit records its pushed SHA. |
| AI-DE application code is MIT-licensed and installed pack material retains Apache-2.0 provenance. | Root `LICENSE`; `THIRD-PARTY-NOTICES.md`; Apache license copy at `docs/ai-forward-pack/LICENSE`; source commit `07488efcf0a7282c6737120fec7262eba26acb27`. | `timianmalloo/ai-forward` bundle `2026.08.16.2`, revision 45 | Missing license copy, unpinned source, or incompatible notice fails. | Yes - the first publication review found no retained pack license/provenance in the repository. | **Verified** | Future pack updates require a new source revision and license check. |
| The recovered architecture describes the code that exists, not an aspirational system. | One WPF `WinExe`; `StartupUri -> MainWindow -> XAML DataContext -> MainWindowViewModel`; no runtime service, persistence, model, or background project. | `AiDe.sln:6-12`; `src/AiDe.App/AiDe.App.csproj:4-8`; `src/AiDe.App/App.xaml:1-5`; `src/AiDe.App/MainWindow.xaml:15-17`; `src/AiDe.App/ViewModels/MainWindowViewModel.cs:3-16` | An additional runtime project, package, integration seam, or mismatched startup path falsifies the claim. | N/A - characterization of existing source, not a new behavior. | **Verified** | WPF startup and binding resolution are not exercised by the current test suite. |
| Every adopted `docs/**` artifact is schema-valid and connected, and the index matches frontmatter. | `docs-graph.py derive --project AI-DE`; `docs-graph.py validate` reported 7 artifacts, 0 problems, 0 orphans, 0 drift, 0 defects. | `docs/ai-forward-pack/scripts/docs-graph.py`, bundle revision 45 | Any invalid frontmatter, dangling/unregistered edge, orphan, duplicate ID, or index drift fails validation. | Yes - initial inventory reported 1 orphaned artifact and missing `docs-index.js`; the pre-derive adopted graph still failed on missing index. | **Verified** | Root README/notices are a documented MOC-only exception and are not freshness-gated by the derived graph. |
| The Docs Explorer renders the adopted graph. | Local HTTP response `200`; headless Chrome DOM contained `data-state="ready"`, project title `AI-DE Docs Explorer`, and `7 artifacts`. | Chrome headless against `docs/index.html` and generated `docs/docs-index.js` | Non-200 response, failed load state, wrong project name, or artifact count other than 7 fails. | Yes - before derivation the Explorer had no index and could not load the graph. | **Verified** | Browser smoke covers loading/catalog output, not every interaction or Mermaid rendering path. |
| The current solution remains buildable after adoption workflow changes. | Restore exit 0; Release build exit 0 with 0 warnings/0 errors; tests exit 0 with 1 passed, 0 failed, 0 skipped. | .NET SDK selected by `global.json`; `AiDe.sln` | Any non-zero command, warning-as-error, or failed/skipped expected test fails. | N/A - existing behavior was characterized before and after documentation changes. | **Verified** | One unit test does not cover WPF composition, bindings, rendering, or accessibility. |
| The active documentation workflow runs when its own YAML or Markdown/docs surfaces change. | PR path set includes `docs/**`, `**/*.md`, and `.github/workflows/docs-health.yml`; local graph validation passes. | `.github/workflows/docs-health.yml` | A relevant PR with no docs-health run, or a failed graph command, falsifies the claim. | Yes - the initial workflow path set excluded its own YAML. | **Inferred** pending GitHub-hosted run | Fresh-runner execution remains to be observed on the adoption commit. |
| Adoption history is durable and parseable. | `audit-log.py verify` reported all existing rows readable. The closing `/adopt` audit and architecture-baseline change entry are required before completion. | `docs/audit/*.jsonl`; audit tool revision 45 | Missing closing entries or any unreadable JSONL row fails. | No - the closing entries are pending by design until the final action. | **Flagged** | Cleared only after commit/push and final audit append. |

## Adversarial record

The first adoption gate returned BLOCK:

- Documentation Steward: lifecycle/status contradictions, root-document
  indexing boundary, incomplete audit bundle, and `/document` prerequisites.
- Test Architect: no derived index/snapshot/Explorer proof or durable Proof
  Pack, and the workflow did not trigger on its own file.
- Simplifier: reciprocal graph edges, inflated tier language, repeated adoption
  prose, duplicated SDK pinning, and an inert foundation check.

The corrections reduced the graph to essential edges, one runtime layer, one
phase table, a documented root-document exception, a derived seven-artifact
index, a health snapshot, and this Proof Pack. The final independent gate and
GitHub-hosted checks remain.

## Gate record

Pending final Documentation Steward, Test Architect, and Simplifier verdicts
after persistent GitHub checks.
