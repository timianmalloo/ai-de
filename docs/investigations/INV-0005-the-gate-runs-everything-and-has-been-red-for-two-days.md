---
id: "INV-0005-the-gate-runs-everything-and-has-been-red-for-two-days"
title: "INV-0005 — The gate runs everything, in the wrong order, and has been red for two days"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [ci, testing, gates, efficiency, performance-assertion]
links:
  - { to: defect-classes, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: ""
summary: >-
  A docs-only change runs a 20-minute Windows gate. Measured, the WPF/terminal suite everyone
  suspects is 2.3% of it and mutation replay is 51%. The larger finding is correctness, not cost:
  a wall-clock frame-budget assertion fails deterministically on CI hardware, and because it sits
  before every other gate, 26 gates have not executed on any run since 2026-09-04 — 38 of the last
  40 Build runs are red.
---

# Investigation: INV-0005 — the gate runs everything, in the wrong order, and has been red for two days

- **Status:** Root cause verified · **all five phases implemented and verified green on CI** · one new flake tracked below
- **Opened:** 2026-09-05
- **Trigger:** *"it's odd that we have full test runs on things like terminals and WPF shells for changes like these — should we have a more intentional and efficient test plan?"*

## 1. Symptom, as reported

An ADR renumber and a citation disambiguation — changes that touched only Markdown, comments and
one Python gate — ran the full `Build` workflow: 2,118 tests including the WPF shell and terminal
suites.

## 2. What the measurement actually says

The premise contains an assumption worth testing before acting on it: that the WPF/terminal tests
are the expensive part. **They are not.** Profiled from the CI runner's own `.trx` files and the
GitHub Actions step timings of the last green run (`33792751361`), not from a developer machine:

| Step | CI wall time | Share |
|---|---:|---:|
| **Mutation replay** | **612s** | **51%** |
| Test step (both projects, incl. per-project build) | 317s | 27% |
| Project compilation coverage gate | 82s | 7% |
| Restore + Build | 113s | 10% |
| **All ~24 Python gates combined** | **~31s** | **2.6%** |
| Checkout / setup | ~25s | 2% |
| **Total** | **1,189s (19m49s)** | |

And inside the test step, from the runner's `.trx`:

| Project | Tests | CI wall | Note |
|---|---:|---:|---|
| `AiDe.Core.Tests` | 1,719 | **112.0s** | `net10.0` — portable |
| **`AiDe.App.Tests`** (WPF shell + terminal) | 399 | **27.5s** | `net10.0-windows` — genuinely locked |

**The suspected bottleneck is 2.3% of the run.** [Verified] This is CE1's prediction verbatim —
*the bottleneck is rarely the suspect* — and it means "stop running the WPF tests on docs changes"
would save about 27 seconds of a 20-minute job. The real money is mutation replay, the Core suite,
and the runner multiplier.

Every step above runs on `windows-latest`, which bills at **2×**. Every other workflow in this repo
(`docs-health`, `pages`, `ui-craft`) already uses `ubuntu-latest`. So ~40 billable minutes per push,
of which only the 27.5s WPF suite is actually Windows-locked. [Verified]

## 3. The larger finding: the gate is not slow, it is RED

While profiling, the run history contradicted the premise of the question:

**38 of the last 40 `Build` runs failed. The single success was 2026-09-03T18:49Z.** [Verified]

### 3.1 Root cause RC-1 — a wall-clock assertion measuring the host, not the code

`AiDe.App.Tests.TerminalViewTests.AFullScreenRedraw_StaysInsideTheFrameBudget` asserts
`p95 < 16.67 ms` for a 200×50 full-screen redraw. Sampled across four independent CI runs:

| Run | p95 measured on CI | Budget |
|---|---:|---:|
| `33999538484` | 22.78 ms | 16.67 ms |
| `33996315903` | 21.60 ms | 16.67 ms |
| `33911326214` | 21.25 ms | 16.67 ms |
| `33906311415` | 22.28 ms | 16.67 ms |
| `33903717971` | 17.23 ms | 16.67 ms |

**Necessary:** the same commits pass this test on the developer machine, so the code is not the
differentiator — the host is. **Sufficient:** the assertion compares a wall-clock p95 against a fixed
absolute constant with no hardware normalisation and no environment guard, so slower hardware alone
produces the failure. [Verified]

**And the test is not detecting what it was written to detect.** Its own failure message states the
discriminator: *"S3 measured GlyphRun-per-line at 6.64 ms and FormattedText-per-cell at 142.80 ms, so
a number in the hundreds means the draw path has reverted to per-cell text."* The observed 17–23 ms is
**not** in the hundreds. The draw path is correct; the runner is ~3× slower than the machine the
budget was set on.

The guard band is the defect. The intended signal is a ~20× architectural regression
(6.64 → 142.80 ms). The threshold was placed at 2.5× the good value (6.64 → 16.67 ms). The
CI-versus-developer hardware spread is ~3×. **The noise is larger than the guard band**, so the test
fires on the environment before it could ever fire on the regression.

### 3.2 Root cause RC-2 — the cheap gates run behind the expensive one

`Build` runs steps in file order: build → **test** → 26 Python gates. GitHub Actions skips subsequent
steps once one fails, so on run `33999538484` every one of these was `skipped`:

> Defect-register integrity · Cited-controls · Gate self-test ratchet · Audit capture ratchet · Proof
> Pack capture instruction · Mutation replay · Fixture-derivation · Embedded-script syntax · Audit-log
> id uniqueness · Monotonic id allocator (+ self-test) · Derived views (+ self-test) · Production
> stand-ins (+ self-test) · Stranded audit-log · Surface ownership (+ self-test) · Harness
> diagnostics (+ self-test) · API-cref (+ self-test) · Project compilation coverage · Bounds-are-enforced
> · Extractor generation · Published layout

**26 gates, costing ~31 seconds combined, have not executed on CI since 2026-09-04** because a 172s
test step ahead of them fails first. [Verified] Every id-collision, derived-view and register gate
that this repository relies on has been dark for two days — including, ironically, the gates that
would have caught the ADR collision and the committed conflict markers on their own build.

This is CE4's ring model inverted: the *slowest, most environment-sensitive* check is running in
Ring 0's position, and the fast control suite is gated behind it.

### 3.3 The same class, a second instance

`build.yml` justifies mutation replay as an every-push gate in a comment:

> *"MEASURED at 74s for 18 mutations on this machine, which is why it is an every-push gate and not
> a nightly."*

On the runner it takes **612s — 8.3× the measured figure**, and it is 51% of the job. [Verified] The
scheduling decision rests on a number measured on a developer machine and never re-measured where the
gate runs. Same shape as RC-1: a local magnitude encoded as a portable decision.

## 4. Causes ruled out

| Candidate | Ruled out because |
|---|---|
| A real rendering regression | The failure message's own discriminator says per-cell drawing is ~143 ms; observed is 17–23 ms, consistent with the correct GlyphRun path on ~3× slower hardware. [Verified] |
| Flake / timing noise | Five consecutive runs failed, all in a tight 17.2–22.8 ms band. A flake does not cluster. [Verified] |
| The WPF/terminal suite being too slow (the reported premise) | It is 27.5s of a 1,189s run — 2.3%. [Verified] |
| Too many Python gates | ~31s combined, 2.6% of the run. Removing all of them would save half a minute and lose the entire control suite. [Verified] |
| Test-host crash (DC-012) | `verify-test-run` reports `399 executed / 399 expected`, outcome `Failed` with `1 failed, 0 errored, 0 aborted`. The run completed; a test genuinely failed. [Verified] |

## 5. Marker harvest

`simplify:` and `assume:` markers were harvested across `tools/`, `tests/` and `.github/` per CI9 /
NG4. **None found in the test or CI subsystem** — the only hits are the pack's own instruction files,
which are the definitions rather than markers. So this defect was not predicted in writing; it is a
genuine gap, not a triggered shortcut. [Verified]

## 6. The failure class

Registered as **DC-107 — a magnitude measured on one machine, encoded as a portable threshold**
(see `docs/lessons/defect-classes.md`). Siblings swept and confirmed:

| Sibling | Location | Verdict |
|---|---|---|
| Frame-budget p95 vs 16.67 ms | `TerminalViewTests.AFullScreenRedraw_StaysInsideTheFrameBudget` | **Confirmed** — failing now |
| Mutation replay "74s ⇒ every-push" | `.github/workflows/build.yml` | **Confirmed** — 612s on CI, 8.3× |

The fleet-inherited `PACK-C` (*an assertion encodes a transient magnitude assumption*) is the
**ancestor, not a duplicate**: PACK-C's discriminator is time (a number that decays), DC-107's is
**host** (a number that never was portable). Their controls differ — PACK-C wants re-measurement on a
schedule, DC-107 wants the assertion expressed relative to something measured in the same process, or
moved to a ring where the hardware is fixed.

## 7. Phased repair plan

Each phase is independently landable and independently verifiable. **Nothing below has been
implemented** — this report stops at review.

| # | Phase | Scope (code + tests) | Failure mode eliminated | Validation | Depends on |
|---|---|---|---|---|---|
| **0** | **Unred the gate** | Make the frame-budget assertion environment-relative: measure the per-cell path in the same process and assert the ratio (`glyphRun < perCell / 5`), or gate the absolute budget behind an opt-in `PERF_BUDGET` env var set only on known hardware. Keep a correctness test that always runs. | A hardware-speed difference reads as a code regression; `main` red for 38 runs | The test fails when the draw path is forced to per-cell, and passes on both CI and dev hardware | — |
| **1** | **Reorder: gates before tests** | Move the ~24 Python gate steps (~31s) *above* the test step in `build.yml`. Pure ordering change. | 26 gates dark whenever any test fails | A deliberately broken register on a branch reports the register gate failing, not a skipped step | — |
| **2** | **Split the runner (CE8/CE9)** | Job A `ubuntu-latest`: Python gates + `AiDe.Core.Tests` (`net10.0`). Job B `windows-latest`: `AiDe.App.Tests` (`net10.0-windows`) only. Required check aggregates both, fail-closed (CE5). | Paying 2× for platform-agnostic work | A docs-only PR still reports the required check green *for the right reason* (CE7/E13); billable minutes measured before and after | 1 |
| **3** | **Re-ring mutation replay (CE4)** | Re-measure it *on the runner*, then place it by that number: Ring 1 (merge readiness) or Ring 2 (scheduled), not every push. Update the `build.yml` comment to carry the runner figure, not the laptop figure. | 51% of every push spent on a check justified by an 8.3×-wrong measurement | The re-measured figure is recorded in the workflow; a mutation still reddens the ring it now lives in | 1, 2 |
| **4** | **Class prevention** | A gate that fails when a test asserts a bare wall-clock constant without an environment-relative comparison or an explicit opt-in guard; observed failing on the un-fixed `TerminalViewTests`. | DC-107 recurring silently | `--self-test` proves both directions; run against the pre-fix blob it reports the frame-budget test | 0 |

**Explicitly not recommended: path-filtering the test job on docs-only changes.** It is the obvious
move and it is the wrong one here. It buys ~27s of WPF time, and CE7 warns that a `paths:` filter
feeding a fail-closed aggregator is exactly how a skipped job becomes a silent green. Phases 1–3 are
larger wins with no false-green surface.

## 7a. What happened when phases 0–2 were implemented (2026-09-05)

Approved and landed the same day. Verified on the runner, not inferred.

| Phase | Result |
|---|---|
| 0 — environment-relative frame assertion | Green. Measured 4.30 ms per-line against a 164.24 ms per-cell reference — **38×**, close to S3's 21×, against a 5× floor. Reversion simulated: 0.7×, red. |
| 1 — cost-ascending step order | Green. Run `34000905461` was the **first successful Build since 2026-09-03**. |
| 2 — control suite on Linux | Green. `gates` job completes in **19s**; `build` 672s. They run in parallel, so wall-clock is 672s against 1,189s sequential. |

**Phase 2 was delivered partial.** The Core tests do NOT move to Linux. They target `net10.0`, which
made the lock look like a label; it is not. That project launches `AiDe.Daemon` (`net10.0-windows`)
as a real process and drives ConPTY via `CREATE_NEW_CONSOLE`. The large multiplier win in the plan
was moving those 1,719 tests to a 1× runner, and it is unavailable without splitting the project
into portable and Windows-locked halves. Recorded under CE9 as a cost to remove, not accepted as a
floor. The plan's cost estimate was wrong because it read a `TargetFramework` instead of the
project's references.

**Moving to Linux immediately found a second class, which is the point of moving.** `verify-derived-views`
reddened at once on `docs/_site/index.html`. Three host-dependent primitives across two generators,
registered as **DC-108**:

- `sorted(Path)` — `pathlib` compares Windows paths case-insensitively and POSIX paths
  case-sensitively, so `AiDe.App.md` and `AiDe.App.ViewModels.md` swap order between hosts
- `rglob` — filesystem enumeration order, with members appended per namespace in that order
- `write_text` without `newline=` — CRLF on Windows, LF on Linux

All three are fixed. The class is now controlled **by construction**: the gate runs on Linux while
developers run on Windows, so a cross-platform disagreement in any generator cannot survive a push.

**Two process findings from doing it, both already fixed:**

1. **A gate that detects but does not localise is half a control.** `verify-derived-views` named a
   700 KB file and stopped, so diagnosing meant reproducing the other host. Two hypotheses (line
   endings, then the embedded date/SHA) were wrong before the real cause was found. The gate now
   prints the first diverging byte with context, and on its very next run that output named the
   cause in one line. Same shape as `verify-test-run` reporting *"1 failed"* without naming the test.
2. **The skip-cascade repeated one level down.** Inside the `gates` job, one failing step still
   skipped the twelve behind it, so each diagnosis cost a whole CI round trip — which is why this
   took four cycles rather than one. Every gate step now carries `if: ${{ !cancelled() }}`; a
   failing step still fails the job, so it stays fail-closed, but one run now reports everything.

## 7b. Phases 3–4 and the feedback path (2026-09-05)

| Phase | Result |
|---|---|
| 3 — re-ring mutation replay | **Re-measured on the runner at 401s**, against the `74s on this machine` in the comment justifying it — 5.4× wrong. Now runs when `src/`, `tests/` or a mutation set changed, plus daily and on demand. **Verified live:** a push touching only `.github/` and `tools/` reported `skipped`. |
| 4 — gate the class | `tools/verify-perf-assertions.py`. Clean across 242 test sources; **observed failing on the real pre-fix blob** at `d6ce176`, reporting `TerminalViewTests.cs:173` — the exact line that reddened 38 runs. DC-107 moved to `controlled`. |
| Feedback path | `.github/workflows/main-status.yml` — one self-closing `main-red` issue. |

**Phase 4's first draft was wrong, and the correction is the interesting part.** It fired on three
assertions in `RefreshMetricsTests` of the form `status.DurationMilliseconds >= 30`. Those are
**lower** bounds sitting beneath a `Task.Delay(40)` the test itself injects: they assert the clock
ran at all, and slower hardware only makes them *more* true. Only an **upper** bound puts the machine
in the verdict. The rule was narrowed rather than the three exempted by name — an exemption list is
how a gate stops describing a class and starts describing a codebase.

**Why branch protection was rejected as the feedback path.** It is the obvious answer. It would also
force a pull request in order to fix the very build that is blocking you, against a working agreement
of landing directly on `main` where verification replaces review. The problem was never that red
pushes are allowed — it is that red became invisible the moment its session ended. So the fix pushes
the signal at the actor: one issue, opened when `main` breaks, edited while it stays broken, closed
when it recovers. One issue rather than a comment per failure, because an alert arriving 38 times is
the failure being repeated rather than fixed.

**Its own first firing failed, which is the only reason it is now trustworthy.** `gh label create`
was called without `--repo`, and a `workflow_run` job has no checkout for `gh` to infer one from — so
the label never existed and `gh issue create --label main-red` died a step later. A `|| true` on the
label call had swallowed the real error and moved the symptom. Both fixed; the label now exists in
the repository, so the observed failure cannot recur, and the guard fails loudly instead of
half-working. **Confidence:** the green path is [Verified] — it ran on a green `main` and correctly
did nothing. The red path is [Inferred] past the point that previously broke; the next genuine red
proves it, and now fails loudly if it does not.

## 8. Residual risk / what would change this diagnosis

- The billing API was not consulted; billable-minute figures are **derived from wall-clock × the
  documented 2× Windows multiplier** and are an order-of-magnitude estimate (CE2 permits this,
  labelled). [Inferred]
- The 27.5s / 112.0s test split is from **one** CI run's `.trx`. Contention behaviour under a
  different parallel width was not measured (CE3). [Inferred]
- If the frame-budget test were ever observed failing at a *hundreds*-of-milliseconds p95, RC-1 would
  be wrong and a genuine draw-path regression would be the cause. Nothing in five sampled runs is
  above 23 ms.
- Phase 3's placement was decided from a runner measurement (401s), not the earlier 612s figure —
  which itself came from a differently-loaded run, so treat single-run step timings as ±50%.
- **A new, unrelated flake surfaced, has since RECURRED, and is not fixed here.**
  `TerminalPrivacyTests.ASecretPrintedByATerminal_ReachesNoSpanAttributeAndNoWorkspaceFile` has now
  failed **twice** on CI (2026-09-05, on `ce64c1d` and again on `047d2b8`), passes locally in ~1s,
  and passed on an immediate re-run of the second failure — so it is **intermittent, not
  deterministic**. Two occurrences is this repository's own recurrence threshold, so it is a pattern
  rather than an incident. The test is well built: it refuses to pass vacuously, which is the only
  reason the failure is legible at all.

  **The obvious fix is ruled out by the evidence.** The message is `output completed before
  '<marker>' appeared`, **not** `timed out waiting for '<marker>'` — so the 25s deadline in
  `tests/AiDe.Core.TerminalHost/Program.cs` is never reached and raising any bound would change
  nothing. The ConPTY output channel *completes* before the marker's **second** occurrence arrives,
  and that second occurrence is the terminal echoing the typed command back. The open questions are
  why the channel closes early and whether that echo is reliable on a non-interactive CI console.
  Inflating a bound that was never hit is precisely the shape this whole investigation is about, so
  this is recorded for its own investigation rather than patched.
