---
id: proof-compile-admission-code
title: "Proof Pack — CV-4, admission's code: Gate 2's reader recomputing every floor from num/den, ring.py's A6 re-score/demote/re-admit, the drift watermark readmitted_at, and compile.mode.changed{from,to,trigger}"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-d"
tags: [proof-pack, conversation-lane, cv-4, addendum-d, compile, gate-2, gate-3, eval-harness, drift, adr-0036]
links:
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: adr-0036-compile-mode-ladder-deployment-gates, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: proof-compile-call, rel: refines }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-13
review-suggested: []
summary: >-
  Evidence for CV-4 on the Conversation lane (Addendum D slice D-3, code only): `CompileAdmissionGate`
  reads `compile-eval-admission.json` and recomputes five floors from the report's own
  numerator/denominator pairs — the split witness (holdout of 50, disjoint, ordered), `schema_fail`,
  the `applied_denied`/`tool_calls` invariants, and Ruling 76's degraded-rate floor — never a stored
  verdict, wired into `CompileModeGate`'s Gate 2 branch in place of CV-3's placeholder refusal.
  `tools/compile-eval/ring.py` re-scores on a triple change or a `model_observed`/`model_configured`
  mismatch (CV-3's residual), demotes with `trigger: ring|drift`, and sets the drift watermark
  `readmitted_at` on a later passing re-score. `compile.mode.changed{from,to,trigger}` joins the
  `compile.*` vocabulary and `SetCompileMode` emits it on every real transition. A canonicalisation
  fixture asserts the same SHA-256-over-raw-bytes constant from both the C# and the Python side.
---

# Proof Pack: CV-4 — admission's code (Gate 2's reader, the A6 ring, the drift watermark)

- **Tier:** T1 · **Fan-out cap:** 2 (reviews read-only) · **Session:** `cv-4` · **Author:** `claude-cv-4` · **Base:** `main` `1c29b5d5`.
- **Scope, stated up front:** this track is the *code*. The gate itself is not a track — 50 scored +
  50 holdout **real** envelopes accrue through operator use over weeks; `agentic` becomes selectable
  the day a real `compile-eval-admission.json` meets these floors, a calendar event recorded then.
  No model call was made building this; the harness scores recorded envelopes only.

## E7 change-surface list (written before coding; ticked at close)

| Surface | Change | Writer | Compute reader | Status |
|---|---|---|---|---|
| Gate 2 codes | `CE-0024`–`CE-0029` (unreadable, split-invalid, schema_fail, applied_denied, tool_calls, degraded) | `EnvelopeStoreErrorCodes` (Core/Compilation) | `CompileAdmissionGate` | ✅ |
| Gate 2's reader | new `CompileAdmissionGate.Evaluate`/`EvaluateReport` — parses the report, recomputes 5 floors from `numerator`/`denominator`, never reads `met`/`verdict`/`passed`/`admitted`/`selectable` | `src/AiDe.Core/Sessions/CompileAdmissionGate.cs` | `CompileModeGate.Evaluate`'s Gate 2 branch; `TheAdmissionGateRecomputesFromNumeratorDenominatorTests` | ✅ |
| `CompileModeGate` | the placeholder `AdmissionReportOutstanding`-always refusal replaced with `CompileAdmissionGate.Evaluate(proofDirectory)`; still refuses `CE-0020` when no file exists (US-D11 b2 kept green) | `src/AiDe.Core/Sessions/CompileModeGate.cs` | `TheCompileModeLadderIsGatedTests.AMatchingArtifactWithAZeroRecountAdmitsAdvisoryAndNotAgentic` (unchanged, still green) | ✅ |
| `compile.mode.changed` vocabulary | `CompileEventKinds.ModeChanged`; `CompileModeChangeTriggers` (`operator`\|`gate`\|`ring`\|`drift`); `CompileSignal.ModeChanged(from,to,trigger)` | `src/AiDe.Core/Compilation/CompileSignal.cs` | `TheCompileModeChangedEventTests` | ✅ |
| `SessionConfigStore.SetCompileMode` | gains a `trigger` parameter (default `operator`); emits `ModeChanged` only when the mode value actually changes; rejects a word outside the four | `src/AiDe.Core/Sessions/SessionConfigStore.cs` | `TheCompileModeLadderIsGatedTests` (3 new tests) | ✅ |
| `tools/compile-eval/ring.py` | new: the A6 ring — re-scores via `score.score()` (imported, not reimplemented) on a triple change or a drift suspect; recomputes the same 5 floors independently; demotes/re-admits; append-only `compile.mode.changed` log; watermark state file (`triple`, `mode`, `demoted_at`, `readmitted_at`) | `tools/compile-eval/ring.py` | `--self-test` (8 assertions); the operator (CLI) | ✅ |
| the drift watermark `readmitted_at` | set by `ring.py` on the first passing re-score after a demotion; never consulted for admission (only Gate 2's report is) | `ring.py`'s state file | operator/provenance reading (display use only — no consumer wired in this slice, named below) | ✅ (code); display wiring — not this track |
| `model_observed ≠ model_configured` → drift trigger | CV-3's residual: `ring.py`'s `drift_suspects()` scans `called` rows for a recorded mismatch and treats it as a demotion cause independent of a triple change (`trigger: "drift"`) | `ring.py` | `--self-test`'s drift-trigger assertion | ✅ (in `ring.py`; **not** written into the `called` row's `outcome` field — that write site is `ComposerSendGate.cs`, outside this track's file ownership, named below) |
| canonicalisation fixture | one fixed byte string, one hardcoded SHA-256 hex constant, asserted identical by `CompilePin.Sha256` (C#) and `hashlib.sha256(...).hexdigest()` (`ring.py --self-test`) | both | `TheCanonicalisationFixtureMatchesRingPysHash`; `ring.py --self-test`'s first assertion | ✅ |

**Not written by this slice, named:**
- **The gate-2 settings-row control** (a UI affordance calling `SetCompileMode`) — `SessionDocumentSurface.cs` is CV-5's file; a seam request, below.
- **Writing `outcome: "suspect"` into the `called` row on a `model_observed` mismatch** (ADR-0036's drift-detector item (ii), the deterministic zero-cost trigger) — that write site is `ComposerSendGate.Complete` (`src/AiDe.App/Workbench/Composer/ComposerSendGate.cs`), a file with its own lifetime hand-off (CV-0 → CV-1 → CV-2) that does not include CV-4. `ring.py` detects the mismatch **at scoring time**, by reading the two fields the row already carries (`model_configured`, `model_observed`) — it never rewrites the store (DM7: history is never rewritten). A seam request, below.
- **The full ADR-0036 Gate 3 prompt-version A6 ring** (`(contract_version, prompt_sha, profile.sha)`, old-vs-new over the golden set, k ≥ 3 paired-median samples) — the plan row and this track's brief both name the ring's triggering triple as `(adapter sha, CLI sha, craft-profile sha)`, which is Gate 1's pin identity, not Gate 3's prompt identity. `ring.py`'s docstring records this as a scope note (not silently resolved); building the heavier prompt-version ring is a finding for a future track.
- **The watermark's "evaluate only rows after `readmitted_at`" refinement** (ADR-0036: "every trigger evaluates only rows after it, so a demotion is computed at every open… and no trigger applies until a re-admission moves the watermark") — `ring.py`'s demote/readmit decision compares the *current* re-score's floors against the *current* state, which already cannot flap (proven by the self-test's no-flapping assertion), but does not filter which rows a *future* live session-open check would evaluate — because no such live check exists yet (ADR-0036's "next session open" trigger is session-lifecycle wiring, a different track). Named as a residual, not built here.

## The reds → green

| # | Red (observed) | Green | Evidence |
|---|---|---|---|
| 1 | `CompileAdmissionGate` did not exist — Gate 2 always refused `agentic` by construction (CV-3's placeholder) | `CompileAdmissionGate.Evaluate` reads the report and recomputes; `CompileModeGate`'s Gate 2 branch calls it | `EveryFloorHoldingAdmits` (was unreachable — `agentic` could never be selectable on any machine); `AMatchingArtifactWithAZeroRecountAdmitsAdvisoryAndNotAgentic` stays green (no report → still `CE-0020`, US-D11 b2 unchanged) |
| 2 | A report with `holdout.n = 49` — nothing checked it | `AdmissionSplitInvalid` (`CE-0025`) naming `holdout.n = 49 < 50` | `AHoldoutBelow50Refuses` |
| 3 | A planted overlap between `sample.envelope_ids` and `holdout.envelope_ids` — the sample would be judged as the holdout | `AdmissionSplitInvalid` naming the shared id count | `AnOverlappingSplitRefuses` |
| 4 | `holdout.first_at` before `sample.last_at` — the split's ordering inverted | `AdmissionSplitInvalid` naming "precedes" | `AHoldoutThatPrecedesTheSampleRefuses` |
| 5 | `schema_fail` at 4 % (`2/50`) against the 2 % floor — nothing computed the rate | `AdmissionFloorSchemaFail` (`CE-0026`) naming `2/50 = 4.0% > 2%` | `ASchemaFailRateOverTheFloorRefuses` |
| 6 | `applied_denied` numerator `1` — the invariant not checked | `AdmissionFloorAppliedDenied` (`CE-0027`) naming `1/500` | `AppliedDeniedNonZeroRefuses` |
| 7 | `tool_calls` numerator `3` — the invariant not checked | `AdmissionFloorToolCalls` (`CE-0028`) naming `3/500` | `ToolCallsNonZeroRefuses` |
| 8 | `degraded` at 6 % against Ruling 76's 5 % floor | `AdmissionFloorDegraded` (`CE-0029`) naming `6/100 = 6.0% > 5%` | `ADegradedRateOverTheFloorRefuses` |
| 9 | A zero-denominator rate (`0/0`) — nothing stopped it reading as a vacuous pass | refuses (`AdmissionFloorDegraded`, "no measured rows to compute the rate over") — absence never admits | `AZeroDenominatorNeverPasses` |
| 10 | **The plan's own red:** a report with `verdict: "admit"`, `met: true`, `metrics.schema_fail.passed: true`, `admitted: true`, `selectable: true` planted over an otherwise-failing `schema_fail` — a naive reader that checked any of those keys would admit | still refuses on `AdmissionFloorSchemaFail`; the reader never inspects any of the five planted keys, at any nesting | `APlantedVerdictOverFailingFloorsIsIgnoredAndStillRefuses` |
| 11 | Symmetrically: the same planted keys over an otherwise-**passing** report | still admits (`null`) — a verdict cannot manufacture an admission either, proving the ignoring is total, not one-directional | `APlantedVerdictOverPassingFloorsChangesNothing` |
| 12 | `SetCompileMode` had no `trigger` concept; every write looked identical in the transition history | `trigger` parameter (default `operator`); the closed four-word vocabulary validated; `CompileSignal.ModeChanged` emitted once per real transition, never on a same-mode write | `SetCompileModeDefaultsToOperatorAndEmitsOnlyOnARealTransition`, `SetCompileModeCarriesAnExplicitTrigger`, `SetCompileModeRejectsAnUnknownTrigger` |
| 13 | `ring.py` did not exist — a triple change was never re-scored, a regression never demoted | `run_ring()`: unchanged triple + no drift → no-op; changed triple + passing floors → re-scored, no transition; changed triple + failing floors (12/120 = 10 % degraded > 5 %) → **demoted**, `trigger: ring` | `ring.py --self-test`, assertions 3–5 |
| 14 | After a demotion, an unchanged triple re-checked the same failing evidence and would re-demote (flapping) | a second no-op run with the same (still-failing) triple logs nothing further — the watermark holds | `ring.py --self-test`, assertion 6 ("no flapping") |
| 15 | A later passing re-score after a demotion had nowhere to record "this got fixed" | `readmitted_at` set, `mode` returns to `agentic`, a second `compile.mode.changed{from: agentic-advisory, to: agentic, trigger: ring}` row appended | `ring.py --self-test`, assertion 7 ("re-admit") |
| 16 | **CV-3's residual:** `model_observed ≠ model_configured` on `called` rows was recorded but never acted on | `drift_suspects()` finds the mismatch on an **unchanged** triple and still demotes, `trigger: drift` (proving drift is independent of a triple change, not a re-labelling of the ring trigger) | `ring.py --self-test`, assertion 8 ("drift trigger") |
| 17 | The canonicalisation fixture: no shared constant existed to catch a sha-domain drift between the two languages (a BOM, a trailing newline, upper-casing) | one fixed byte string, one hex constant (`59ca002c…c5488`), asserted equal by `CompilePin.Sha256` (C#, raw bytes → SHA-256 → lowercase hex) and `hashlib.sha256(...).hexdigest()` (Python) | `TheCanonicalisationFixtureMatchesRingPysHash`; `ring.py --self-test`'s first line |

## Where the admission report is read from, and why

`~/.aide/proof/compile-eval-admission.json` — the **same machine-level `~/.aide/proof/` directory**
Gate 1's `compile-pin-spike.json` lives in (ADR-0036's path-resolution rule): the admission report is
about *this operator's corpus*, not about which workspace or checkout is open, so it lives beside the
pin artifact rather than under a repository. `CompileAdmissionGate.Evaluate(proofDirectory)` takes the
same `proofDirectory` `CompileModeGate.Evaluate` already resolves (`CompilePinArtifact.DefaultDirectory`
by default: `%USERPROFILE%\.aide\proof` on this platform), and appends `compile-eval-admission.json`.
No JSON twin is committed under `docs/proof/` for the same reason Gate 1's frame log is not re-copied
per-workspace — the Proof Pack cites the numerators/denominators directly (this file, above), never a
second file to keep in sync.

## The floor set this reader evaluates, and the one explicitly not gated on here

Recomputed, in order, each independently sufficient to refuse: the split witness (holdout ≥ 50,
disjoint from the sample, not preceding it) → `schema_fail ≤ 2 %` → `applied_denied = 0` → `tool_calls
= 0` → `degraded ≤ 5 %` (Ruling 76). This is the plan row's own naming of "every A14.4 floor" for this
reader — the fuller quality bar (`acceptance ≥ 60 %`, `missed ≤ 20 %`, `emptied ≤ 20 %`, `span_resolution
≥ 90 %`, `shape_flip_kept ≥ 80 %`) is §A14.4's harness-quality bar, not named among this track's reds or
its file list, and is left unenforced here — a scope decision (Solution-Selection Ladder: smallest
correct against the task as scoped), not an oversight, recorded so a future reader that widens the gate
does so as a stated change, not a silent one.

## The A6 ring's behaviour on a triple change (the self-test's numbers)

120 synthetic `agentic-advisory` envelopes, one `called` row each (matching `score.py`'s own fixture shape):

| Step | Triple | Corpus | `floors_hold` | Action | `compile.mode.changed` rows so far |
|---|---|---|---|---|---|
| 1 (baseline) | `a…/b…` | clean | holds | `rescored` (establishes `agentic`, no transition — nothing to transition *from*) | 0 |
| 2 | `a…/b…` (unchanged) | clean | — (not re-scored) | `no-op` | 0 |
| 3 | `c…/b…` (adapter sha changed) | clean | holds | `rescored` (mode unchanged) | 0 |
| 4 | `d…/b…` (adapter sha changed again) | 12/120 `timed_out` = 10 % > 5 % | **fails** (`degraded`) | **`demoted`**, `trigger: ring`, `from: agentic`, `to: agentic-advisory` | 1 |
| 5 | `d…/b…` (unchanged) | same regressed corpus | — | `no-op` — **no re-demotion, no second row** | 1 |
| 6 | `e…/b…` (adapter sha changed; corpus recovered) | clean | holds | **`readmitted`**, `trigger: ring`, `from: agentic-advisory`, `to: agentic`; `readmitted_at` set | 2 |
| 7 (separate baseline) | `f…/g…` | clean | holds | `rescored` (establishes `agentic`) | 0 (own log) |
| 8 | `f…/g…` **(unchanged)** | clean, but `model_observed = "claude-opus-5[1m]"` ≠ `model_configured = "claude-sonnet-5"` on every row | holds numerically, but a drift suspect exists | **`demoted`**, `trigger: drift`, `from: agentic`, `to: agentic-advisory` — **on an unchanged triple**, proving drift fires independently of Gate 3's triple check | 1 (own log) |

All eight `ring.py --self-test` assertions pass (including the canonicalisation fixture); output captured verbatim in the gate record below.

## The gate-2 reader refusing a planted verdict (the plan's named red)

```
report = PassingReport(schemaFailNum: 10, schemaFailDen: 50)   // fails on its own — 20% > 2%
report["verdict"] = "admit"; report["met"] = true;
report["metrics"]["schema_fail"]["passed"] = true;
report["admitted"] = true; report["selectable"] = true;

CompileAdmissionGate.Evaluate(dir) → CompileModeRefusal(AdmissionFloorSchemaFail, "…10/50 = 20.0% > 2%…")
```
The refusal's code and reason are unchanged by any of the five planted keys, at top level and nested —
`APlantedVerdictOverFailingFloorsIsIgnoredAndStillRefuses`. The symmetric case (the same keys over an
otherwise-passing report) still admits — `APlantedVerdictOverPassingFloorsChangesNothing` — proving the
ignoring is total: a verdict can neither manufacture nor block an admission.

## The holdout rule (never the sample)

`CompileAdmissionGate`'s `Split` recomputes the witness from the report's own `sample.envelope_ids` /
`holdout.envelope_ids` arrays — never from a summary field. Three ways the rule is defended, each its
own red: `holdout.n < 50` (`AHoldoutBelow50Refuses`), the sample and holdout sharing an id
(`AnOverlappingSplitRefuses` — "the sample would be judged as the holdout" is the refusal's own words),
and the holdout preceding the sample (`AHoldoutThatPrecedesTheSampleRefuses`). `score.py`'s own
`check_contract` defends the same three shapes on the writer's side (`--self-test`'s "red as expected"
assertion, unchanged by this track) — two independent implementations agreeing is the intended
redundancy (DC-127's shape: an eval's floor is never satisfiable by one side alone).

## `CE-` codes added (contiguous; `verify-id-allocators.py`)

| Code | Name | Fires when |
|---|---|---|
| `CE-0024` | `AdmissionReportUnreadable` | bad JSON, wrong `contract`, or missing `sample`/`holdout`/`invariants`/`metrics` |
| `CE-0025` | `AdmissionSplitInvalid` | `holdout.n < 50`, an id shared between sample and holdout, or holdout preceding sample |
| `CE-0026` | `AdmissionFloorSchemaFail` | `schema_fail` over 2 % (or unmeasurable) |
| `CE-0027` | `AdmissionFloorAppliedDenied` | `applied_denied` numerator ≠ 0 |
| `CE-0028` | `AdmissionFloorToolCalls` | `tool_calls` numerator ≠ 0 |
| `CE-0029` | `AdmissionFloorDegraded` | degraded rate over 5 % (Ruling 76), or unmeasurable |

`verify-id-allocators.py` reports the `CE` family at **29**, contiguous, no holes (`CE-0001`–`CE-0029`).

## Findings (recorded; the conductor allocates ids — placeholders)

1. **DC-189 — the ring's triggering triple, as named in the plan row, is not ADR-0036 Gate
   3's triple.** The plan row and this track's own brief name `(adapter sha, CLI sha, craft-profile
   sha)`; ADR-0036's Gate 3 section names `(contract_version, prompt_sha, profile.sha)` with a heavier
   mechanism (golden-set A/B, k ≥ 3 paired-median samples). `ring.py` is built to the former, literally,
   with the discrepancy recorded in its own docstring rather than silently resolved either way. Control:
   a future track builds the prompt-version ring against the ADR's literal triple, or the ADR is amended
   to fold the pin-identity re-score into Gate 3 explicitly. Sweep: `note-addendum-c-council-rulings.md`
   and the ADR itself are the two places this ambiguity could be tightened.
2. **DC-190 — the audit start marker was not set at grounding this run.** `audit-log.py start
   --session cv-4` was not run before the first tool call (this session began by reading the
   coordination plan and spec files before setting up its own environment). `duration_seconds` on this
   entry is therefore not measured from true grounding — reported as not recorded rather than a
   plausible number (IO: degrade to absent, never invent). Control candidate: a harness-level
   `SessionStart` hook that calls `audit-log.py start` unconditionally, so a skill cannot begin
   substantive work before the marker exists (closes the same class the addendum-cd.md plan's own
   worked table names for `duration_seconds` gaps on unplanned nodes).
3. **`model_observed` mismatch detection lives in `ring.py`, not in the `called` row's `outcome`
   field.** ADR-0036's drift-detector item (ii) reads as a deterministic, zero-cost signal evaluated
   live, per call; this track's `drift_suspects()` is a batch scan over already-recorded rows, run when
   the ring runs (operator-triggered), not at call time. The distinction matters for latency: a mismatch
   is caught at the next ring run, not at the moment the mismatched call completes. Recorded for
   whichever track next touches `ComposerSendGate.Complete` (CV-0 → CV-1 → CV-2's file).
4. **Seam requests:** `SessionDocumentSurface.cs` (a future Conversation-lane slice, not this track's
   file): the *Compile mode* settings row's control could call `CompileModeGate.Evaluate` (now Gate-2
   aware) and `SetCompileMode(mode, availability, now, trigger: Operator)`, showing the refusal's reason
   verbatim when a rung is not selectable. `build.yml` (the conductor, per the seams table CV-3 →
   X-1): append `python tools/compile-eval/ring.py --self-test` beside `score.py --self-test` and
   `derive-fixtures.py --self-test` at the end of the gates job.

## Reviews (Stage 4 — Adversary Mode; the author never clears its own veto; read-only, fan-out cap 2)

| Lens | Verdict | Findings and disposition |
|---|---|---|
| AI Systems Engineer (hard) — floors recomputed, holdout never the sample | **PASS** | Every floor traced to a `numerator`/`denominator` pair read fresh from the JSON on each call, never cached, never a boolean. The split witness is recomputed from the raw id arrays, not a summary flag. The zero-denominator case refuses rather than passing vacuously (`AZeroDenominatorNeverPasses`) — an unmeasurable floor is never a met one. The planted-verdict tests (`APlantedVerdictOverFailingFloorsIsIgnoredAndStillRefuses` / `…OverPassingFloorsChangesNothing`) are the correct falsifier shape: both directions tested, not just "a verdict can't force admit". Condition cleared: the scope note distinguishing this reader's five floors from §A14.4's full metric set is stated in both the class doc-comment and this Proof Pack, not left implicit. |
| Test Architect (hard) — the planted-verdict and ring tests | **PASS** | The planted-verdict test plants five differently-spelled verdict-shaped keys (`verdict`, `met`, `passed` nested under a metric, `admitted`, `selectable`) rather than one, closing the "it only checks for `met`" gap. The ring's self-test proves five distinct transitions (baseline → no-op → rescore-no-transition → demote → no-flap → readmit) plus an isolated drift-trigger case with the triple *held constant* — the one test that proves `trigger: drift` is not merely a relabelling of `trigger: ring`. Both suites are red-then-green in this Proof Pack's own reds table, not asserted from memory. Gap noted, not blocking: the ring's self-test is the only exercise of `drift_suspects()` — no C#-side test exercises the Python path (by construction: `ring.py` runs standalone, never called from .NET), so its correctness rests on the Python self-test and this review's read of the code, not a cross-language test. |

Both hard vetoes clear. No Simplifier or Data & Persistence convene was requested for this slice
(no schema or migration surface; `CompileAdmissionGate` and `ring.py` are pure readers of an existing,
already-reviewed report contract) — consistent with the plan row's disconfirm gate, which named these
two lenses as the reviews for this track.

## Budget (planned vs actual — GO19)

Planned 2,945 s / stop 4,127 s. Actual: recorded in the closing audit entry (`duration_seconds`); the
audit start marker was not set at grounding (Finding 2, above), so the measured figure understates true
elapsed time — flagged rather than reported as if measured from the true start.

## Gate record

| Gate | Result |
|---|---|
| `dotnet build` Core, `-p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors |
| `dotnet build` App, `-p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors |
| `dotnet test` `AiDe.Core.Tests`, `-p:TreatWarningsAsErrors=true`, trx into `artifacts/test-results` | **2,615** passed, 1 skipped (pre-existing, unrelated), 0 failed |
| `dotnet test` `AiDe.App.Tests`, `-p:TreatWarningsAsErrors=true`, trx into `artifacts/test-results` | **895** passed, 0 failed (matches the recorded floor exactly — CV-4 adds no App-layer test) |
| `tools/compile-eval/score.py --self-test` (unchanged; sanity) | 3/3 assertions pass |
| `tools/compile-eval/ring.py --self-test` | 8/8 assertions pass (canonicalisation; baseline; no-op; rescore-no-transition; demote/ring; no-flap; readmit/ring; demote/drift) |
| `tools/verify-id-allocators.py` | OK — `CE` family at 29, contiguous, no holes |
| `tools/regenerate-derived.py` | every derived view regenerated and verified current (API reference 3,371 public symbols, doc bundle, site figures, audit render, docs graph) |
| `python tools/run-verify-gates.py` | run after regeneration and the trx placement fix; see the audit entry for the final line |
