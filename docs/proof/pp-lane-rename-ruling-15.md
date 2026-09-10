---
id: proof-lane-rename-ruling-15
title: "Proof Pack — Ruling 15/15a: GovernedSessionSource/GovernedSession rename"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, addendum-a, ruling-15, naming, agent-plane, proof]
links:
  - { to: note-addendum-a-ratification, rel: tested-by }
  - { to: note-addendum-a-ruling-15a-governed-episode, rel: tested-by }
  - { to: note-conductor-spec-errata-lane-rename, rel: relates-to }
  - { to: architecture-agent-plane, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Evidence that the A3 naming repair (GovernedSessionSource -> GovernedLaneSource,
  GovernedSession -> GovernedEpisode per Ruling 15a) is a pure rename: identical test
  counts before/after, clean zero-warning build, and the full verify-*.py + marker-lint
  gate set with exit codes.
---

# Proof Pack — Ruling 15/15a rename

Worktree `C:\Projects\ai-de-chore-lane-rename`, branch `chore/lane-rename`, commit `666a1e2`.

## Behaviour-unchanged evidence

| Check | Before | After | Equal? |
| --- | --- | --- | --- |
| `AiDe.Core.Tests` executed | 1890 | 1890 | Yes |
| `AiDe.App.Tests` executed | 399 | 399 | Yes |
| Total executed | 2289 | 2289 | Yes |

Both runs via `python tools/verify-test-run.py` (CHECK mode, no `--update`), exit 0 both times.

**Finding, not fixed here:** the committed baseline (`verify-test-run`'s own file) records
1877/399. The actual `AiDe.Core.Tests` count was already 1890 — 13 above baseline — **before**
any edit in this session. The gate still passes because it only asserts `executed >= expected`.
This is pre-existing drift on `chore/lane-rename`'s parent history, not something this rename
introduced or moved; the rename's own contribution is exactly zero (1890 == 1890).

## Build

`dotnet build AiDe.sln --nologo -clp:ErrorsOnly` — **Build succeeded. 0 Warning(s). 0 Error(s).**
(`TreatWarningsAsErrors=true` is repo-wide, so 0 warnings is 0 suppressed warnings too.)

## Full gate set, bare, true exit codes

Run individually (never piped — DC-113: a pipe reports the formatter's exit code, not the
gate's; this was caught mid-session when a `| tail` run showed "exit:0" for two gates that are
actually exit 1 when run bare).

| Gate | Exit |
| --- | --- |
| `verify-api-crefs` | 0 |
| `verify-audit-capture` | 0 |
| `verify-audit-log` | 0 |
| `verify-bounds-are-enforced` | 0 |
| `verify-capture-instruction` | 0 |
| `verify-cited-controls` | 0 |
| `verify-defect-register` | 0 |
| `verify-derived-views` | **1** — see finding below |
| `verify-embedded-scripts` | 0 |
| `verify-extractor-generation` | 0 |
| `verify-fixture-derivation` | 0 |
| `verify-gate-self-tests` | 0 |
| `verify-harness-diagnostics` | 0 |
| `verify-id-allocators` | 0 |
| `verify-no-conflict-markers` | 0 |
| `verify-perf-assertions` | 0 |
| `verify-project-coverage` | 0 |
| `verify-published-layout` | 0 |
| `verify-site-figures` | 0 (after `--update`; see below) |
| `verify-standins` | 0 |
| `verify-stranded-audit` | 0 |
| `verify-surface-ownership` | 0 |
| `verify-test-run` | 0 (both before and after runs) |
| `marker-lint.py --gate` | 0 |

## `verify-site-figures` — fixed

Initially reported 6 stale figures (`site/collaboration.html`, `site/index.html`,
`site/model.html`) — audit-entry/artifact/ledger counts that moved because this session's own
work (2 new decision notes, the audit-log `start` entry, this proof artifact) grew the corpus.
Fixed with `python tools/verify-site-figures.py --update`; re-verified clean (exit 0).

## `verify-derived-views` — one residual finding, pre-existing and out of scope

Two of the three views it checks were stale from the rename and are now fixed by running their
real generators (never hand-edited): `docs/docs-index.js` (`docs-graph.py derive`) and the doc
bundle `docs/_meta.json` + `docs/_site/index.html` + `docs/api/*.md` (`build-doc-viewer.py`,
which calls `api-reference.py`).

The third, `docs/audit/audit-data.js`, still reports one stale byte range — the `"project"`
field. Root cause, verified by reading `docs/ai-forward-pack/scripts/audit-log.py`: the CLI's
`render` subcommand takes a top-level `--project` flag, but `verify-derived-views.py`'s own
regeneration call (`["docs/ai-forward-pack/scripts/audit-log.py", "render"]`) never passes it, so
`project_name()` falls back to `os.path.basename(...)` of the working directory's parent — the
**worktree's own directory name** (`ai-de-chore-lane-rename` here), not the canonical project
name committed (`ai-de`). This session's own render was run with `--project ai-de` explicitly
(matching the committed convention) and is correct on disk; the gate's internal comparison
regenerates without that flag and so disagrees with its own committed file, in **any** worktree
not literally named `ai-de` — which, under WT1a (a new session always works in its own worktree,
never the primary checkout), is every session but one. This is a pre-existing gap between the
gate and the mandated worktree-per-session policy, not a defect this rename introduced, and
fixing the gate's own CLI wiring is out of this change order's scope (T1, fan-out cap 0, two
type renames). Flagged here as a finding rather than worked around.
