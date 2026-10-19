# Brief — T22 `wl1b-design` (slice WL-1b) · Claude Code · Opus

**Goal (plan refresh, wl-leader3):** run `/design-slice` for WL-1b and cut it into red-first implementation tracks.
**Done when:** `docs/design/workbench-loops-wl-1b.md` is committed on branch `wl1b-design`. It holds the named patterns
and contracts, the E7 surface list, the WL-1b track cut (owned paths, checks red-first, dependencies), the draft human
demo script and the Proof Pack skeleton `docs/proof/workbench-loops-wl-1b.md`. The Patterns-Expert-vs-Simplifier
review and the Data & Persistence Architect's review are recorded in it.
**Tier** T2 · **fan-out** 0 (personas run inline, not as spawned agents) · **budget** 120 tool calls · 200k · this
is one node · **deadline** 1 h.
**Convergence:** a design doc another track can implement without re-reading the ADRs. **No production code, no tests.**
This node writes Markdown only. WL-1b's tracks run on branches; nothing joins until the user's WL-1a demo passes.

## Binding lines (quoted, DC-189; open them, do not work from this summary)
- `docs/architecture.md` §WL.11, the WL-1b row: "Evaluate one mandatory obligation, link evidence, see acceptance
  blocked with the named gap, record a disposition, accept the declared deliverable" · Real: "`checklist_obligation_fact`
  (+`mandatory`), `evidence_link_fact`, `disposition_fact` (obligation/work/intent kinds), the acceptance-eligibility and
  closure folds, a minimal `checklist` kind" · Mocked: "assessments absent" · Demo: "accept a plan deliverable without
  claiming implementation (AC-W03c)" · E2E red first: "acknowledged ≠ satisfied (a row vs a fold); acceptance blocked
  while a mandatory obligation lacks a current link; saved review state is not acceptance; FK/trigger overhead measured
  P1-PERF-style at 100×".
- ADR-0039 §1 table rows for Evidence link (:83), Checklist obligation (:85, "The row's existence is 'proposed'") and
  Disposition (:86); §3 (:129, :151): acceptance eligibility and closure are **fold-only, never stored**.
- ADR-0045 (:96): "`checklist` arrives with WL-1b".
- Spec `docs/specs/workbench-loops-ux.md` AC-W03a/b/c (:162-164), AC-P02c (:298); the Assurance/Checklist row (:109):
  "Human acknowledgment and demonstrated satisfaction are distinct facts".
- Ruling 156 (addendum-c): the adoption actor is a v3 column landing in the first adopting slice; until then the store
  refuses adoption. **Decide in the design, with the code cited, whether any WL-1b disposition is an adoption.** If one
  is, WL-1b is the first adopting slice and owes the v3 migration (expand-migrate-contract, golden round-trip). If none
  is, say so and cite why.

## Inputs on `main` (read the code; rule 14: the code outranks this brief)
- The tables already exist in schema v2: `src/AiDe.Core/Store/WorkspaceSchema.cs` (:204-207, :293-345). Read their
  columns, CHECKs and triggers. Read `StoreWriter.cs` / `StoreReader.cs` and `src/AiDe.Core/Store/Records/` for any
  writer or reader these rows already have. Do not assume one exists.
- The WL-1a folds `src/AiDe.Core/Workbench/Records/Folds/` (`AttemptStateFold`, `CurrentnessFold`). Acceptance
  eligibility needs currentness ("a current link"), so name how it consumes `CurrentnessFold`.
- `docs/design/workbench-loops-wl-1a.md` and `docs/design/workbench-loops-wl-s.md`, for the doc shape and for D6
  (every switch over `SurfaceEntry` lists Derived/Verb/TurnAction; how a `checklist` kind is admitted).
- The P1-PERF precedent: find the existing `P1-PERF` / 100× measurement in the repo and name the harness WL-1b reuses.

## The cut must respect the live WL-S window
T11 (`wls-kinds-migration`) and T21 (`wls-float`) are still running and own `SurfaceContentFactory.cs`,
`LayoutPersistence.cs`, `ZoneBackedLayoutService.cs` and the `WorkbenchShell.cs` regions they hold. Any WL-1b track that
must edit those files (the `checklist` kind) is scheduled **after** the WL-S joins, and its row says so. Core-only
tracks (folds, store writer/reader, the 100× measurement) depend on nothing in WL-S and may start at once. State each
track's dependencies as real data edges only (GO5).

## Owned authored paths
new `docs/design/workbench-loops-wl-1b.md`, new `docs/proof/workbench-loops-wl-1b.md` (skeleton: frontmatter, the
draft human demo script, empty evidence sections), `{PROOF}/**` (`handoff.md` only).

## Not in scope
Any `.cs`, test or schema change. Any WL-1c item. Re-deciding settled ADRs; a gap in one goes in the design as a
decision request draft (recommendation + default) for the Leader to raise, not a ruling.

## Node-specific overrides of the standing rules
- Rule 1: the skill is `design-slice`, not `implement`. Rule 7 (red first) and the suite run do not apply: this node
  writes no code. Run `python docs/ai-forward-pack/scripts/docs-graph.py validate` before done (rule 13).
- Rule 12: `--skill design-slice`, `--artifact docs/design/workbench-loops-wl-1b.md`.
