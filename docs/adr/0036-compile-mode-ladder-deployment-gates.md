---
id: adr-0036-compile-mode-ladder-deployment-gates
title: "ADR-0036 — The compile-mode ladder (mechanical-only → agentic-advisory → agentic) is a set of deployment gates read by the settings model at runtime: the pin-spike artifact with a matching adapter sha, then the 50-scored/50-holdout eval report meeting fixed floors; the eval harness is the AI Systems Engineer's gate and ships before the first agentic rung"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-d"
tags: [architecture, compile, eval, deployment-gate, ai-systems, non-determinism, addendum-d]
links:
  - { to: architecture, rel: implements }
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: depends-on }
  - { to: adr-0035-compile-session-binding-and-pin, rel: depends-on }
  - { to: adr-0019-advisory-evaluator-calibration, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  The agentic compile is the one model-backed capability Addenda C and D add. It ships behind an eval
  gate realised as deployment gates the product reads: no agentic rung is selectable without the P-D5
  artifact (adapter sha equal to the installed adapter's); agentic-advisory projects no derived text
  and feeds the corpus; agentic (Send confirms derived rows) opens only when the holdout report meets
  §A14.4's fixed floors; the A6 prompt-version ring re-runs on any (contract, prompt_sha, profile.sha)
  change; the drift detector re-runs the golden set. Rejected: a feature flag, a build-time switch,
  thresholds judged on the sample they were set from.
---

# ADR-0036: The compile-mode ladder is a set of runtime deployment gates behind an eval

- **Amended 2026-09-11 (Ruling 76, `note-addendum-c-council-rulings`):** the floor table's degraded-rate
  floor takes **X = 5 %** as its first value (the stricter reading), and `latency_p95 ≤ 60,000 ms` is
  struck as a floor — latency is a reported, right-censored measurement until 50 measured compiles set
  a floor from the data. Nothing else in this decision moves.
- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 (`addendum-c-chain`) with the
  **AI Systems Engineer** (veto holder — the agentic stage's eval gate and non-determinism
  containment) in Peer Mode; the Release Engineer's lens applied; attacked at the gate
- **Context spec/architecture:** `spec-addendum-d-compile-step` §A5 (D-D1, D-D2), §A8.4, §A10,
  §A14 (the eval harness), US-D11; Rulings 68, 69; `docs/architecture.md` §Optional model capability
  contract (the five conditions any capability above T0 must meet); ADR-0019 (advisory evaluator
  calibration — the precedent for a model-backed advisory with a measured, never asserted, quality)

## Context

Addendum D admits exactly one agentic decoration in v1 — the three structure lines, requested only
for the open ones (Ruling 69) — behind an eval the AI Systems Engineer's hard veto requires. Ruling
68 fixes the **order**: `mechanical-only` (default) → `agentic-advisory` selectable only after the
P-D5 pin spike's artifact exists with a recorded adapter sha equal to the installed adapter's →
`agentic` only after the first 50 real envelopes are scored and the **next 50 (holdout)** meet
§A14.4's floors — *an order, not a conjunction*; a failed spike is a hard stop, never a fallback.
The architecture's standing capability contract (`docs/architecture.md`) already requires, for any
capability above T0: a versioned prompt and typed response schema; a deterministic verifier plus a
named faithfulness eval; pinned model/provider identity and receipts; a deterministic baseline it
must beat at a stated cost; and A4/A5 golden/rubric evals with an A6 regression gate.

The load-bearing question is **where the gates live**. A gate that lives in a document is a
memoir (CI6); a gate that lives in a feature flag is a boolean somebody flips; the spec asks for a
gate **the product reads** so that a spoofed artifact, a bumped adapter or a missing holdout report
leaves the rung unselectable.

LOA principles: P5 (verification over plausibility), P6 (adversarial validation — the adversarial
fixture set), P10.

## Decision

We will:

1. **Realise the ladder as deployment gates the session-settings model evaluates at runtime**, each
   an artifact the product can read and check, never a flag:
   - **Where the gate artifacts live (the path-resolution rule):** both are **machine-level**,
     read from `~/.aide/proof/` — beside `~/.aide/providers.json`, the home of the machine-level
     provider binding, because the pin is about *this machine's* installed adapter, SDK and CLI and
     the admission report is about *this operator's* corpus, and neither depends on which workspace
     is open. The spike run and the harness write them there; the committed copies under
     `docs/proof/` in this repository are the Proof Pack's citation, never what the product reads —
     **no JSON twin is committed**: the Proof Pack `pp-*.md` cites the machine artifact's sha and its
     numerators/denominators directly (two files per gate to keep in sync was the Simplifier's
     finding; the citation is the record — the Enterprise Architect's "three homes, one identity"
     is met by the sha in the citation). The frame log Gate 1 recounts is **machine-level beside
     the artifact** (`~/.aide/proof/compile-pin-spike.frames.jsonl`), so the gate never depends on
     which workspace is open (the Tech Lead's finding).
   - **Gate 1 — the pin artifact, a staleness gate.** `~/.aide/proof/compile-pin-spike.json` (a
     Channel-A record, cited by the Proof Pack: the **pin triple** — adapter
     version and `dist/acp-agent.js` sha,
     SDK version, CLI binary sha (ADR-0035) — the **committed frame log's** path and sha, the fixture
     repository's settings and `.mcp.json` sha, the run date and OS build; **no machine name**).
     The settings model reads it, compares the recorded triple with the **installed** triple, and
     **recounts `tool_call` frames from the committed frame log itself** (LLM-free) rather than
     trusting the artifact's count; a missing artifact, a triple mismatch, a missing or
     sha-mismatched frame log, or a recount ≠ 0 leaves **both** agentic rungs unselectable with the
     reason naming the spike (US-D11 b1). **What this closes and what it does not:** the triple
     comparison closes *drift* (an adapter, SDK or CLI bump under a stale artifact); it does **not**
     close a *fabricated* artifact and frame log committed by a repository writer — accepted for the
     operator's own repository and recorded (the Security Architect's finding); the recount raises
     the forgery's cost from one field to a consistent frame log.
   - **Gate 2 — the admission report, recomputed by the reader.** `~/.aide/proof/compile-eval-admission.json`
     (written by the operator-run harness `tools/compile-eval/`, cited by the Proof Pack) carries
     **numerators and
     denominators, never verdicts**: every §A14.3 metric over the **holdout**; the **witness of the
     split** — `sample.{envelope_ids, first_at, last_at}` and `holdout.{envelope_ids, first_at,
     last_at}`; `prefix_measured` keyed by `(constitution set-sha, profile.sha)`; and the **gate
     tuple** it was judged on — `(contract_version, prompt_sha, profile.sha, model_configured)`
     (and `model_observed` when the wire recorded it). **The floor table is a host-compiled
     constant** (the `opened.constants` idiom), and the settings model **recomputes every floor from
     the report's numerators and denominators** — a `met` field in the JSON would be a boolean
     somebody writes (the AI Systems Engineer's finding). The floors, fixed in advance and revised
     only upward: `schema_fail ≤ 2 %`; `applied_denied = 0` and `tool_calls = 0` as invariants over
     **every** `called` row; **a degraded-rate floor** `(timed_out + malformed + unavailable) /
     every called row ≤ X %` with **X = 5 % as the first value** (**Ruling 76** — the stricter reading;
     Inferred as a first value under IO7; **its one home is the
     host-compiled floor table** — N, X and Δ are admission constants the settings model owns, not
     compile inputs, so they do not ride `opened.constants`, which carries only what that envelope's
     compile consumed: `k`, `byte_bound`, `bound_ms` — the Simplifier's finding; **N is revised only
     upward, floors only stricter** — for a `≤` floor "upward" would loosen it, the Tech Lead's
     finding) — without it a model that times out a third of its calls passes every
     other floor, because `EffectiveMode` folds those rows to `mechanical`; **latency is a reported
     measurement, not a floor** (**Ruling 76** struck the spec's `latency_p95 ≤ 60,000 ms` floor: with
     `bound_ms = 60000` it was the degraded-rate floor in disguise, two floors on one quantity) —
     p50/p95 over succeeded rows with `n_measured / n_total`, timed-out rows reported as
     **right-censored** (`n_censored` beside `n_measured`, the p95 labelled "≥" when a censored row
     sits at or above it, never a plausible exact number), no latency floor until 50 measured
     compiles, then a floor set from the measurement and revised only stricter (§A16's 15 s p95 is
     a *target*, reported, not a floor); the token thresholds against
     `prefix_measured` under the same key;
     `≥ 90 %` resolving spans; acceptance ≥ 60 %, missed ≤ 20 %, emptied ≤ 20 %, shape-flip kept
     ≥ 80 %. **The treatment arm is defined once:** `opened.compile_mode ∈ {agentic-advisory,
     agentic}` ∧ a `called` row exists, with denominators reported per outcome — so `malformed` and
     `suspect` rows never leave the corpus and cannot be excluded vacuously. **The holdout** is the
     first 50 `called` rows carrying the installed gate tuple after the sample's last row (ordered by
     `opened.at`, tie-broken by `envelope_id`, across session files); rows under another tuple are
     excluded and counted; a purged case is labelled and excluded. The settings model makes
     `agentic` selectable only when the report exists, the split witness verifies (disjoint ids,
     `|holdout| ≥ 50`, `holdout.first_at ≥ sample.last_at`, every holdout row under the installed
     tuple — recomputed from the store's real rows, DC-127; **sessions this process holds open are
     read through their open store's reader, and a sibling file held by another writer is reported
     as `partial: <file> held by a writer`, never as a bare unselectable**), every recomputed floor
     holds (a floor
     with `n_measured < n_total` or an absent `usage` is *partial*, never met), and the tuple equals
     the installed tuple **including the bound model** — a session re-bound to another model of the
     same provider is not admitted on another model's report. **The deterministic baseline
     (capability contract condition 4) is named and its comparison stated:** the baseline is
     `mechanical-only` — empty, editable lines (or the template's values); its acceptance is 0 by
     construction, so `acceptance ≥ 60 %` at `tokens_in + cache_read ≤ prefix_measured + 20k` is the
     "beats the baseline at a stated cost" measurable for the empty baseline; against the template
     baseline the comparison is cross-envelope: the operator's **residual edit distance** from
     `Current` at open to `Confirmed` at Submit must be lower in the treatment arm than in the
     `mechanical-only` control arm by a margin **Δ = 20 % as the first value** (Inferred; in the
     host floor table) — **a report in v1, not a floor**: the absolute
     floors are the gates; the margin is reported beside them with its numerator and denominator
     (`partial` when no control-arm rows exist), and becomes a floor only by a recorded decision
     after the first admission. **The A5 faithfulness judgement is the operator's per-line label on the
     holdout**; span relevance (token overlap) is the automated proxy reported beside it, with no
     floor and labelled as a proxy.
   - **Gate 3 — the A6 prompt-version ring.** Any change to `(contract_version, prompt_sha,
     profile.sha)` invalidates Gate 2 (the triple no longer matches) until the ring is re-run: old vs
     new over the golden set, delta = normalised edit distance to the operator's *confirmed* line
     plus the structural invariants, k ≥ 3 samples per case, paired median; an adversarial regression
     blocks; a common-case regression is a recorded decision. The ring is operator-run (it needs the
     subscription) and is never a CI required check. **A non-regressive ring report re-admits by
     carrying the behavioural floors** (acceptance, missed, emptied, shape-flip — a ring produces
     deltas, not fresh operator labels, so those floors cannot be re-met on it): the report marries
     the carried floors, labelled `carried`, with the invariant, schema, span, degraded and latency
     floors recomputed under the new tuple; the reader accepts `carried` only when the ring's
     paired-median delta is non-regressive on every category. `contract_version` names the
     prompt-template/output-schema **pair**, so a validator schema change bumps it too.
   - **The drift detector demotes on the trigger, and the re-run re-admits.** Any of three triggers
     demotes `agentic` to `agentic-advisory` at the next session open with the reason shown: (i)
     acceptance moving ≥ 10 points over a 20-envelope window; (ii) `model_observed` changing between
     consecutive `called` rows when the wire reports it (a deterministic, zero-cost signal that
     precedes the statistical one; when `model_observed` is *not recorded* only (i) applies and the
     model identity stays Inferred); (iii) a `suspect` compile post-admission (ADR-0035 rule 5 — a
     non-zero `tool_calls` is evidence Gate 1's premise no longer holds here). Demoting on the
     trigger rather than on the golden re-run is deliberate: the re-run needs the operator's
     subscription, and between the trigger and the re-run `agentic` would otherwise stay open on an
     unmeasured model — fail-open for exactly the silent-update case the detector exists for (the AI
     Systems Engineer's finding). The golden re-run is the **re-admission** (Gate 3's ring).
     **The detector has a watermark, so it cannot flap — and no new file:** the watermark is
     `readmitted_at` on the admission report (Gate 2's own artifact); every trigger evaluates only
     rows after it, so a demotion is *computed* at every open from the store's rows (deterministic,
     no stored demotion) and the transition history is the `compile.mode.changed{from, to, trigger}`
     event — a separate mode-ledger file would be a second home for a watermark the report already
     carries (the Simplifier's cut of the SRE's remedy; the SRE's flap is still impossible, because
     after a demotion no trigger applies until a re-admission moves the watermark).
2. **`agentic-advisory` is the corpus-building rung and projects nothing derived**: `Confirmed()`
   skips `derived` rows; a *keep* mints an `operator` row (the eval's label); Send with an unkept
   derived line sends it blank. One in five advisory envelopes is a forced-choice tier
   (`tier_prompt: forced`) for D-D2's subset.
3. **The eval harness ships with the first compile slice and before any agentic rung is
   selectable**: `tools/compile-eval/derive-fixtures.py` (golden set derived mechanically from real
   `envelope-events.jsonl` rows; hand-authored cases tagged `authored` and excluded from admission —
   DC-127), `score.py` (the metrics from the fold; every rate with its denominator; **labels
   de-duplicated by the originating `called` row**, so a `reused` envelope (ADR-0035) never counts
   one model output twice, and latency/token percentiles exclude `reused` rows), `ring.py` (Gate
   3), and the report contract test (US-D11 b3/b4). **Work data never enters Channel A by
   accident:** a golden case written under a tracked path carries `authored: true` or
   `affirmed: {envelope_id, at}` (the operator's per-envelope affirmation), and `derive-fixtures.py`
   **refuses to write to a tracked path without `--affirm`** — the harness otherwise reads the store
   in place and commits only ids and labels (the Security Architect's finding, co-reviewed with
   Privacy). The fake-deriver oracle of Addendum C D-5 is the seam's admission test at the composer.
4. **Non-determinism is contained at four points, each tested:** the typed boundary (ADR-0033 —
   nothing downstream sees raw text), the total tier rule over `(P, L)`, `min(cap(tier), ceiling)`,
   and Send as the human gate; `inputs_sha` reuse after a *succeeded* call makes a re-prepare zero
   requests, and a failed call is never reused. The **deny-list** (`lease`, `task_class`,
   `fan_out_*`, `budget`, `engine`, `model`, `account`, `shape`, `tier`, `template`, `history_*`,
   `constitution_*`, `compile_mode`, `source`, any unknown name) is the whole of what the model
   cannot touch; adding an allow-list name requires (i) its own eval in §A14 form and (ii) a
   `contract_version` bump so `inputs_sha` never reuses pre-admission decorations (Ruling 69
   CONDITIONS).

## Alternatives considered

- **A feature flag (`compile.agentic=true`) or a build-time `#if`:** rejected — a flag is a boolean
  with no memory of *why* it is on; the rung would survive an adapter bump, a prompt edit and a
  provider-side model change, which are exactly the events the gates must catch.
- **Thresholds judged on the same 50 envelopes they were set from:** rejected (the AI Systems
  Engineer's condition at the spec gate) — a sample that sets its own passing mark passes.
- **A CI required check that calls the model:** rejected — it needs the operator's subscription
  (spec v1 §4.2; no direct-API path exists or may be added), and a required check nobody can re-run
  is a check that gets muted (CE: cheaper is never weaker, but un-runnable is not cheaper).
- **Shipping `agentic` first and measuring afterwards ("the operator can always edit"):** rejected —
  prose can mislead the operator (a hostile *Not in scope* that widens), and the veto is explicit: no
  model-backed capability without an eval.
- **A single `eval: true` envelope mode refused by `Project()`** (the AI Systems Engineer's first
  mechanism): superseded at the spec gate by the advisory rung — strictly more conservative (no
  derived text reaches a run pre-admission, no dead sends); reviewed by the AI Systems Engineer at
  `/design-slice`.

## Consequences

- **Positive:** the door is held by artifacts the product verifies, so "passes" means something on
  every machine; the corpus is the product's own record; a regression demotes visibly.
- **Negative / accepted:** two committed proof artifacts must be maintained; the first 100 envelopes
  are a single author's (labelled so); an adapter bump costs a spike re-run before any agentic rung
  returns.
- **Follow-ups / new risks (for `/design-slice` and Privacy's co-review):** `notes` (§A8.3) has no
  storage home and no control-character rule in the spec's DM15 trace — store it on the `called`
  row (≤ 500 chars, control characters stripped, C-Lease scanned) or cut it; `confidence` is
  unbounded in the contract — the schema bounds it to `[0, 1]`, an invalid value reads *not
  recorded* and is excluded from calibration with its count. N = 50, X (the degraded-rate floor),
  Δ (the baseline margin) and the constants (K = 5, 32 KiB, 60 s, 1 s) are first values
  labelled Inferred (IO7) — K and the bounds on `opened.constants` (compile inputs), N/X/Δ in the
  host floor table; N revised only upward, floors only stricter; the spec's `latency_p95 ≤ 60 s`
  floor overlapped X (with the bound at 60 s it was a ≤ 5 % timeout-rate floor in disguise) — the
  finding was ruled: Ruling 76 struck the latency floor and set X = 5 % (the stricter reading);
  the `anthropic@1.0.0` profile does not exist (compiles run with profile `none` until
  `/collectknowledge` authors it — ADR-0037).

## Falsifying tests (US-D11; headless unless named)

1. No `compile-pin-spike.json` → neither agentic rung selectable, reason names the spike; an
   artifact whose recorded triple differs from the installed adapter/SDK/CLI triple → the same; an
   artifact whose `tool_call` count disagrees with its committed frame log, or whose frame log is
   absent or sha-mismatched → the same; a matching artifact with a zero recount → `agentic-advisory`
   selectable and `agentic` not, with the reason naming Gate 2.
2. A `compile-eval-admission.json` whose recomputed floors include any *partial* (an absent
   `usage`, `n_measured < n_total`, fewer than 50 holdout ids, a degraded rate over X = 10 %, a
   sibling session file held by a writer) → `agentic`
   not selectable; a report carrying `met: true` on a floor its own numerator/denominator
   contradicts → not selectable (the reader never reads a verdict); a split witness that fails
   (overlapping ids; `holdout.first_at < sample.last_at`; a holdout row under another tuple) → not
   selectable; every recomputed floor holding and the tuple equal → selectable; a tuple mismatch
   (a prompt edit, a profile bump, **a session re-bound to another model**) → not selectable until
   a ring report under the new tuple replaces it.
3. The harness's output-contract test: every metric carries numerator/denominator over the holdout;
   authored fixtures are labelled and excluded; the two invariants are computed over every `called`
   row; a ring report with k = 1 or a string-equality delta fails.
4. Under `agentic-advisory`, a derived line neither kept nor edited never reaches
   `GovernedRunRequest.Goal`; under `agentic`, Send confirms it (the send-gate test, both rungs).
5. The drift detector: the acceptance trigger alone (a fake scorer, no re-run) demotes `agentic`
   at the next open with the reason shown; a `model_observed` change between two consecutive
   `called` rows demotes; a `suspect` row post-admission demotes; a non-regressive golden re-run
   re-admits with the behavioural floors `carried`; **after a re-admission, the same history does
   not re-demote at the next open** (the watermark is `readmitted_at`), and one
   `compile.mode.changed` event is emitted per transition.
6. Adding a name to the allow-list without a `contract_version` bump fails a test that computes
   `inputs_sha` for a pre-admission envelope and asserts it is not reusable.
7. `derive-fixtures.py` refuses a tracked output path without `--affirm`; a committed case without
   `authored: true` or `affirmed: {envelope_id, at}` fails the contract test; `score.py` counts a
   `reused` envelope's labels once and excludes its `called` row from the latency/token percentiles.

## LOA mapping

The one **T3** capability, behind — named as the Patterns Expert corrected them — a **Promotion /
Admission Gate** (progressive delivery, artifact-verified; *not* Confidence-Calibrated Gating 6.4,
which gates per candidate on an uncertainty score, and no score exists here) whose LOA home is the
**Evaluation Harness (Part IX) and the capability contract**; **Shadow Mode (dark launch)** for
`agentic-advisory` (runs, records, applies nothing); **Deterministic Verifier (3.1)** (the typed
boundary — the enforcement; 2.5 is the contract's intent, which the ACP transport cannot enforce);
**evaluation-harness adversarial cases** (the `authored` fixture rows — static, not a continuous
Red Team Probe 3.4); **Circuit Breaker with manual reset** (the drift detector demotes on a trigger
and re-admits only by an explicit re-run — no automatic half-open probe, deliberately) with a
**high-water-mark cursor** (the watermark); **Receipt Ledger (4.3)**; **Graceful Degradation (6.5)**
(every non-success is a visible mechanical envelope and the send proceeds). Conformance: C1, C3, C5,
C9 (no ungated model output on a deterministic path), C10 (every call has a `called` row; the chain
is queryable from `envelope_id`).

## Evidence

- **Verified:** `spec-addendum-d-compile-step` §A14.1–§A14.6 and its gate record (the AI Systems
  Engineer's PASS-WITH-CONDITIONS, all folded); Ruling 68; `docs/architecture.md` §Optional model
  capability contract (the five conditions).
- **Inferred:** N = 50, the constants, the floors' initial values (IO7 — first values, revised only
  upward).
- **Flagged:** P-D5 not yet run; the `anthropic@1.0.0` profile does not exist.
