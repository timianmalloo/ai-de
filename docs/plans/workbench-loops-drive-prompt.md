---
id: plan-workbench-loops-drive-prompt
title: "Drive prompt — implement the workbench-loops architecture autonomously under coordination"
type: plan
status: proposed
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [plan, prompt, coordination, workbench-loops, implementation]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: adr-0039-workbench-records-durable-representation, rel: depends-on }
  - { to: adr-0041-execution-attempt-lifecycle, rel: depends-on }
  - { to: adr-0045-perspective-set-factory-review-understand, rel: depends-on }
review-by: 2026-11-18
summary: >-
  The paste-ready prompt that starts the multi-day implementation drive: analyse the accepted
  architecture, build the coordination plan, and execute it with an Opus leader, a Fable owner,
  Sonnet for routine coding and Grok / Antigravity workers, reporting a backlog table every
  fifteen minutes. Written 2026-10-18 by the define-architecture run; the prompt below is the
  artifact, the frontmatter is only its graph record.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
  - { by: adr-0045-perspective-set-factory-review-understand, on: 2026-10-18, reason: "Ruling 84's Coordination bench superseded; Loomkeeper kinds move under Factory" }
---

# Drive prompt — workbench loops implementation

Paste everything below the rule into a **new Claude Code session in `C:\Projects\ai-de` on
`main`** (the primary checkout). The Owner seat is this session's model (Fable); the Leader is a
spawned Opus session; nothing below reopens settled direction.

---

/compile then /prepare-for-coordination then /execute-with-coordination — in that order, one turn
each, no pauses between them unless a `DR-n` is genuinely unanswerable from the repo.

**Goal state (CT19).** Goal: implement the workbench-loops evolution defined in
`docs/architecture.md` §Workbench loops (WL.1–WL.15) and ADR-0039…ADR-0045, to the acceptance
criteria of `docs/specs/workbench-loops-ux.md` (30 stories, 90 criteria), slice by slice in the
order WL-0 → WL-1 → WL-2 → WL-3 → WL-4 → WL-5 → WL-6, landing each slice on `main` with its
Proof Pack. Done when: every slice's "E2E checks (red first)" column in §WL.11 has an observed red
then green, each slice's human demo script has been run by the Owner against the built app, and
`docs/proof/workbench-loops-<slice>.md` exists per slice with the artifacts named in its
`episode-close`. Not in scope: a hosted product; general source editing; an OAuth broker;
cross-platform; media ingestion beyond diagrams/code/metadata/checklists; reopening the settled
direction (local desktop, dockable shell, Factory/Review/Understand, first-class notebook +
execution, left navigation, optional terminal, explicit native sign-in, two distinct journeys).
Tier: T2. Fan-out cap: 6 concurrent tracks.

**Operating posture — read this before anything else.** This is a single-user, local developer
tool. The user runs it on their own machine with full authority and their dev environment is
their sandbox. Do **not** over-engineer privacy or security: no threat-model expansion, no new
credential controls, no egress policy work beyond what the ADRs already require (harness-owned
sign-in, sanitized child environment, page cannot send). Keep the structural invariants (they
are correctness, not security): write-ahead attempt rows, immutable revisions, folds not flags,
one run-event reader, no replay after outcome-unknown. Where a spec criterion is a privacy or
security *negative fixture*, write the minimal test that proves the invariant and move on.

**Seats and harnesses (CO-S1).**
- **Owner** — this session (Fable 5.1). Rules on decision requests (`coord decide rule next`),
  validates every slice's human demo against the built app, accepts or refuses joins. Never
  authors track code.
- **Leader / Coordinator** — spawn one Claude Code session on **Opus** (`claude-opus-5-5`),
  pinned with `coord leader pin`. It owns the plan, dispatch, seams, joins and the 15-minute
  report. Never authors track code (execute-with-coordination's first rule).
- **Routine coding tracks** — Claude Code on **Sonnet** (`claude-sonnet-5-5`) for well-specified
  C#/.NET work whose design is fixed by an ADR: schema group + migration (ADR-0039), folds, catalog
  rows and mappers (ADR-0042), page protocol v2 message kinds (ADR-0043), surface kinds and slot
  migration (ADR-0045), test fixtures and probes.
- **Delegated coding tracks on other harnesses** — via `--launch` through `coord-runner.py`
  (ACP for Grok, native stream-json for Antigravity). Use **Grok Build** (`grok agent stdio`,
  model `grok-4.7-build` as the benchmark profile qualifies at `8f514607` — confirm against the
  installed CLI's models cache before dispatch — with `--reasoning-effort high`) for isolated, test-first C# tracks with a crisp contract (e.g. the
  `test_attempt_fact` extractor, the Antigravity/Muse mappers, the `CoordinationReadModel`
  fixtures). Use **Antigravity** (`agy --output-format stream-json`, model id as the benchmark
  profile qualifies at `x-harness-x-model-bench@8f514607`, e.g. `gemini-3.8-flash-high`; verify
  the id against the installed CLI first) for mechanical, high-volume work: regenerating derived
  views, HTML renders, golden-file fixtures, red-first test scaffolds from the §WL.11 E2E
  columns. Antigravity headless has **no interactive permission response** — launch it only with
  a policy-permissions profile (`allow` command/read/write; `deny` mcp/url) and only on tracks
  with no decision edges. Qualify each harness as `enforced` / `observed-only` / `unsupported`
  from what you actually exercise; never advertise a mode you have not run.
- **Claude Code (Opus) for the hard tracks**: the attempt host rewrite with write-ahead +
  `session/cancel` (ADR-0041), the three-DockHost re-row and `SurfaceModelRegistry` (ADR-0045),
  the CodeMirror notebook qualification spike E-1 (ADR-0043).

**What the Leader does first (the analysis turn).** Read, in order and citing what it read:
`docs/architecture.md` §Workbench loops; ADR-0039…0045; the spec's §A4, §A5, §C2 and
"Traceability and verification handoff"; `docs/lessons/defect-classes.md` (CTX-*, WT-*, DC-189,
DC-190, DC-163, DC-221, CAP-P); `docs/reviews/forensic-review-rev102.md` FR-001/FR-002 (slice
WL-0); `coord doctor`; `git worktree list`. Produce, as the `/prepare-for-coordination` plan:
the E7 surface list per slice; the artifact classes touched (derived/register need no
coordination — say so); the track DAG with real dependencies only; the serial spine (schema
group and `RunEvent` vocabulary first — until those interfaces are fixed every track's shape
changes); per-track owner · owned paths · harness · model · tier · fan-out · budget (tool calls,
tokens, wall) · exit evidence · deadline · fallback · termination; and the honest multiplier
(fewer tracks than six if that is the truth). Every row that names an ADR trigger quotes the ADR
line (DC-189).

**Execution rules for every track brief (CTX-F).** First line `audit-log.py start --session
<track-id> --skill implement`; goal and done-when verbatim from the plan row; owned authored
paths; claim for the minutes of the edit; absolute paths, no `EnterWorktree`; multi-line programs
as a file then a run; gate exit status never behind a pipe; context ceiling 400k with split or
`/compact`; convergence condition; exit evidence; not-in-scope naming the neighbouring work. Red
first on every §WL.11 check. A sub-agent's "done" is evidence, not authority — the Leader reads
the state back (tests run, files exist, demo steps performed) before a join. Join only through
`conductor-join.py`; never remove a worktree to resolve a conflict.

**Cadence and reporting — every 15 minutes, without being asked.** The Leader posts to the board
(`coord board post`) and to this session a single table:

| Track | Slice | Harness / model | Owner | State (queued · running · waiting-decision · review-ready · joined · blocked) | Last observed progress (file / test / commit, with time) | Budget used (calls / tokens / wall) | Next action |

followed by one line each for: open decision requests (`DR-n`, age), seam requests open/resolved,
joins landed since last report, and the backlog remaining per slice (count of §WL.11 E2E checks
still red / not started). Presence is not progress: a track with a fresh heartbeat and no new
file, test or commit in 30 minutes is **stalled** and climbs the kick ladder (CO17). A budget
firing is a defect signal, not a termination argument (GO9).

**Speed with rigor.** Execute as quickly and autonomously as possible. Do not ask the user
questions that the repo, the spec, the ADRs or a spike can answer; raise a `DR-n` to the Owner
seat only for a genuine product trade-off, with a recommendation and a default that applies if
unanswered in 20 minutes. Never cut: red-first tests, the E7 surface list, the Proof Pack per
slice, the audit/change-log entries, `tools/regenerate-derived.py` after every docs change, and
the human demo per slice (the Owner runs it). Land on `main` directly when a slice's gate is
green (no PRs, no review queue; verification replaces review). Push after every join.

**Slice order and the first 24 hours.** WL-0 (FR-001/002) and the serial spine (schema group v2 +
migration golden test; `RunEvent` vocabulary extension; `PerspectiveSet` three rows with the build
test) start immediately as the only tracks; parallel tracks open the moment those interfaces are
committed. WL-1 (the walking skeleton, both journeys on the real claude-code lane, keyboard only)
is the first slice the Owner demos. Then WL-2 and WL-3 in parallel (editor vs native profiles —
no shared authored files), then WL-4, WL-5, WL-6 with their own plans refreshed from actuals
(planned vs actual recorded per track, GO19).

**Close of each slice.** `docs/proof/workbench-loops-<slice>.md` from the proof-pack template;
`episode-close` line naming its artifacts; `audit-log.py append` with goal/done-when; change-log
entry if a decision moved; `regenerate-derived.py`; commit; push; next slice.

Start now with `/compile` of this prompt.
