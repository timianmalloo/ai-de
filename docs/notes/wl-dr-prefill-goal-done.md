---
id: note-wl-dr-prefill-goal-done
title: "Decision request — who fills Goal, Done when and Not in scope (AC-W01a): the compile step's model, on by default?"
type: decision-note
status: proposed
owner: "@timianmalloo"
tags: [decision, workbench-loops, composer, compile, ac-w01a, adr-0036, ai-systems]
links:
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: adr-0036-compile-mode-ladder-deployment-gates, rel: relates-to }
  - { to: adr-0042-native-harness-profile-binding, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: "2026-11-19"
summary: >-
  The user wants the goal, done criteria and exclusions to come from an LLM reading of the prompt. The product already
  has that: Addendum D's compile call (CompileCallHost through ComposerSendGate.PrepareAsync) proposes the three
  structure lines as `derived` rows, with keep/edit marks and an eval gate (ADR-0036). It is off because the session
  default is mechanical-only (Ruling 68), and no control on main lets the user select agentic-advisory. The structure
  lines therefore open blank (StructureDeriver.Fake). The recommendation: expose the compile-mode control and default
  new sessions to agentic-advisory when Gate 1 admits, with four conditions. Nothing is built before the Owner rules.
---

# Decision request — who fills Goal, Done when and Not in scope (AC-W01a)

**Asked by:** Sub-Agent `wl-t29-prefill` (track T29, node 1) for Leader `wl-leader3-20261019`. **Rules:** Owner
`wl-owner-20261018`. **Source:** the user, 2026-10-19: *"I should not be the one filling in goal and done criteria — that
should come from the LLM analysis of my prompt."* **Built:** nothing. Every code claim below cites the file and line on
`origin/main` `624b46c4` that this node opened. Any claim not opened is labelled Inferred with an `assume:` line.

## The answer in one paragraph

The derivation the user asks for exists and is wired. The compile call proposes the three lines, the composer shows them
as *? derived* with a Keep control, and an eval harness with fixed floors guards the rung that skips Keep. The user sees
blank lines for two reasons. First, the session default is `mechanical-only` (Ruling 68). Second, main has no control
that changes it. The only caller of `SessionConfigStore.SetCompileMode` is the parallel-session copy. So the decision
is not *which model call to build*. It is *whether the compile step's rung is on by default, and where the user turns it
on*.

## 1. What fills those fields today

| Field | Declared | Who writes it | Prefilled now? |
|---|---|---|---|
| Goal · Done when · Not in scope (the three per-prompt lines) | Wire names `goal`, `done_when`, `not_in_scope` at `src/AiDe.Core/AgentPlane/GoalBlock.cs:64-66`; the per-prompt subset at `src/AiDe.Core/Presentation/Composer/ComposerDraft.cs:145-146`; the draft store `ComposerDraft.SetGoalValue` at `ComposerDraft.cs:223-246` | The user, typing in the structure lines beneath the editor. The line's change handler calls `_draft.SetGoalValue` and `_gate.EditLine` (`src/AiDe.App/Workbench/Composer/ComposerSurface.cs:1537-1547`). The page's field path does the same (`ComposerSurface.cs:853-867`). The draft store restores saved values (`src/AiDe.Core/Presentation/Composer/ComposerDraftStore.cs:91-94`). | **No, under the default mode.** Each line starts from `StructureDeriver.Fake(field)`, which returns `""` (`ComposerSurface.cs:1537`, `:2361-2368`). Its doc comment says it is a fake "until the compile step (CV-2) proposes values" (`ComposerSurface.cs:1527-1530`). **Yes, under an agentic rung.** `PrepareTurnAsync` calls `line.ShowDerived(proposed, …)` for each line the model proposed (`ComposerSurface.cs:795-802`). |
| Tier · Fan-out cap · Budget | `ComposerDraft.SessionSuppliedGoalFields` (`ComposerDraft.cs:136-137`) | Never the user, per prompt. `SetGoalValue` refuses them (`ComposerDraft.cs:237-243`). | **Yes, mechanically.** Tier comes from `ComposerCompiler.Tier(shape, lease patterns)`. The cap is `EffectiveFanOut(tier, ceiling)`. The budget is the session cap or subscription-bounded (`ComposerDraft.cs:306-318`). |
| Selected scope (the write lease) | `LeaseDerivation.Patterns(SourceText)` (`ComposerDraft.cs:310`) | Derived from the user's `@` mentions. Never typed. | **Yes, mechanically.** Access is read-only unless the turn is a goal block with patterns (`src/AiDe.Core/Presentation/Composer/ComposerCompiler.cs:69-73`). |

**The rung that decides which row applies.** `SessionConfig.CompileMode` defaults to `mechanical-only`
(`src/AiDe.Core/Sessions/SessionConfig.cs:132-136`). The gate copies that default (`ComposerSendGate.cs:136`). The
model is called only when `IsAgenticRung` is true (`ComposerSendGate.cs:169`). The session settings show the mode as
**read-only text**, with a fixed help string, *"Mechanical-only: one gesture, you fill the structure."*, whatever the
mode is (`src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs:1344-1350`, `:1413`). The setter
`SessionConfigStore.SetCompileMode` (`src/AiDe.Core/Sessions/SessionConfigStore.cs:342`) has one caller on main, the
parallel-session copy (`src/AiDe.App/Workbench/Sessions/ParallelSessionFlow.cs:120`). Architecture slice C-3 promised a
compile-mode setting in session settings (`docs/architecture.md:978`). Main shows that setting as text only.

**The notebook path is the same composer.** WL-1a saves the draft as a revision and then calls the same
`_gate.PrepareAsync` (`ComposerSurface.cs:778-785`). The session binder opens the draft on the goal-block form
(`src/AiDe.App/Workbench/Sessions/SessionComposerBinder.cs:116`). The revision row stores the body only. It has no
structure columns (`src/AiDe.Core/Store/WorkspaceSchema.cs:174-181`).

## 2. Which call should derive them

**The existing compile call.** No new model call is needed.

- **The path (Verified by reading).** `ComposerSendGate.Compiler` defaults to `CompileCallHost.CompileAsync`
  (`ComposerSendGate.cs:148`). `PrepareAsync` works out the open lines, which are the structure lines `Confirmed()`
  reads as blank (`ComposerSendGate.cs:579`). It skips the call when no line is open (`:582-594`). It builds
  `inputs_sha` and the `compile-prompt/1` text (`:605-606`). It reuses an earlier success with the same `inputs_sha`, at
  zero requests (`:609-621`). Otherwise it makes one call (`:623-630`). Only `CompileOutputValidator.Validate` reads the
  model's text. A tool call or a permission request marks the result `suspect` (`:644-660`).
- **Why not a new call.** ADR-0035 makes `CompileCallHost` the one toolless, pinned composition for this job. ADR-0036
  gates it. A second call that proposed the same three lines would be a second definition of one quantity (DM
  derive-don't-store). It would also bypass both deployment gates and the corpus that Gate 2 is measured on.
- **Cost.** One call per Prepare while any line is open. Zero calls when every line is filled or the inputs are
  unchanged since a success. The call bills the user's subscription through the bound engine and account
  (`src/AiDe.App/Conductor/CompileCallHost.cs:13-22`). It is bounded by `bound_ms = 60000`
  (`src/AiDe.Core/Compilation/PreCompile.cs:63`). The `called` row records cost, or null when the wire reports none
  (`CompileCallHost.cs:61-75`).
  assume: the cost of one real compile prompt is not measured anywhere this node read. P-D5's 618 to 4,608 output tokens
  (`docs/proof/compile-pin-spike.md:351-353`) came from hostile and tool-inviting probe prompts, not compile prompts.
  Confirm: the first `called` rows under agentic-advisory, or `score.py`'s `tokens_out`. If false: the cost per Prepare
  could differ by an order of magnitude either way.
- **Latency.** Bounded at 60 s. The spec's target is 15 s p95, and it is a target, not a floor (Ruling 76;
  `docs/adr/0036-compile-mode-ladder-deployment-gates.md:116-123`). assume: no measured p50 or p95 exists for real compile
  prompts. Confirm: `score.py`'s `latency_ms` after the first 50 measured compiles. If false (if it is slow): the
  two-gesture Send waits that long on every new prompt.
- **Where the output lands.** In the session's envelope store, `<workspace>/.aide/sessions/<id>/envelope-events.jsonl`
  (`src/AiDe.Core/Compilation/EnvelopeStore.cs:18`, `:42`). It is not a `RunEvent` and not a `prompt_revision_fact`
  column. The rows are one `called` receipt (`src/AiDe.Core/Compilation/EnvelopeEvents.cs:168-183`: engine, model
  configured and observed, latency, cost, outcome, `inputs_sha`, `prompt_sha`, contract version, tool calls, permission
  requests, dropped counts) and one `decorated` row per proposed line, with `source: derived`, `call_seq`, `confidence`
  and `grounded_in` (`EnvelopeEvents.cs:88-108`; appended at `ComposerSendGate.cs:684-694`). A Keep or an edit appends
  a `decorated` row with `source: operator` (`ComposerSendGate.cs:736-757`).
- **What carries it to the attempt.** `submitted.projection_sha` hashes the confirmed goal, done-when and not-in-scope
  (`src/AiDe.Core/Compilation/Projection.cs:110-116`, `:182-210`). The attempt's `dispatch_key` is
  `sha256(envelope_id ‖ projection_sha ‖ …)` (`src/AiDe.Core/Dispatch/BoundaryDispatcher.cs:391-398`;
  `src/AiDe.App/Conductor/AttemptHost.cs:164`).
- **Spec stories.** WL: **US-W01 / AC-W01a** (`docs/specs/workbench-loops-ux.md:142-146`). The mechanism is Addendum D:
  §A11 Prepare and its marks table (`docs/specs/addendum-d-compile-step.md:343-357`), **US-D5** (degrades visibly),
  **US-D6** (Prepare shows all, logs every override) and **US-D11** (the eval gate holds the door, in order;
  `addendum-d-compile-step.md:592-596`).

## 3. Which slice owns AC-W01a

§WL.11's table (`docs/architecture.md:1500-1511`) names **no slice by AC-W01a**. The two rows that come closest, quoted:

> **WL-1a** — Real: "… `ComposerSurface` prepare-from-revision; …" (`docs/architecture.md:1503`)

> **WL-2 — intelligent authoring** — Mocked / stubbed seam: "model suggestion layer (fake peer returning typed
> proposals)" (`docs/architecture.md:1507`)

The model-derived structure lines belong to the Addendum D slices, quoted:

> **D-2 — the compile call and the first gate** — "A pinned compile session on the bound engine returns typed proposals
> into Prepare under `agentic-advisory`; …" (`docs/architecture.md:980`)

> **D-3 — admission** — "50 scored + 50 holdout meet the floors; `agentic` selectable; the A6 ring runs"
> (`docs/architecture.md:981`)

> **C-3 — the composer as a conversation** — "… session settings with defaults (ceiling, `budget_cap: none`, compile
> mode, `free-form`) — F-6's home" (`docs/architecture.md:978`)

**Does doing it now move work between slices?** No WL slice work moves. D-2's mechanism is on main (sections 1–2). The
missing piece is C-3's settings control, plus a default that Ruling 68 fixed. D-3 (admission, the `agentic` rung that
skips Keep) still needs a corpus of 100 real advisory envelopes, and turning advisory on is what builds that corpus.
assume: WL-2's "model suggestion layer" means completion and authoring suggestions, not the three structure lines.
Confirm: the Owner's reading of §WL.11. If false: AC-W01a's prefill is WL-2's, and doing it now pulls WL-2 work forward
past WL-1b.

**The conflict, stated as a decision (the brief requires this).** AC-W01a says "derived/prefilled where possible". The
user reads that as model-derived. Addendum D agrees: §A11's *derived* mark is "the model proposed it". The conflict is
with **Ruling 68's default** (`mechanical-only`) and with the missing C-3 control. Under them, "where possible" means
"never, unless the user changes a setting that main does not let them change". The spec is not changed here.

## 4. The determinism and eval question (AI Systems Engineer, Adversary Mode, inline)

The brief names ADR-0042. That ADR defines the **route fingerprint** (`docs/adr/0042-native-harness-profile-binding.md:130-139`).
The eval gate is **ADR-0036**. Following rule 14, this section reads both. The fingerprint's domain is engine, profile,
version, adapter, env-values sha, token ref and account. No prompt or structure text is in it, so a derived line cannot
leak into it (Verified, `0042:131-133`).

```
PERSONA: ai-systems-engineer   MODE: Adversary   TIER: T2
VERDICT: PASS-WITH-CONDITIONS (for option A below)
FINDINGS:
  - [Minor] (Verified) An eval harness exists and the product reads its gates. Gate 1, the pin artifact with the
    installed triple recomputed and the frames recounted, admits agentic-advisory. Gate 2, an admission report over
    50 scored plus 50 holdout envelopes with its floors recomputed from numerators, admits agentic.
    evidence: src/AiDe.Core/Sessions/CompileModeGate.cs:46-83; tools/compile-eval/{score.py,derive-fixtures.py,ring.py};
    ADR-0036:104-132.  fix: none.
  - [Minor] (Verified) The suggested state exists. Under agentic-advisory, Confirmed() ignores derived rows, so an
    unkept line is blank for the shape, the tier rule and the CT19 block at once. Marks: "? derived", "✓ kept",
    "✓ edited", "— fill in", "? derived · suspect".
    evidence: src/AiDe.Core/Compilation/Envelope.cs:68-79; ComposerSurface.cs:2330-2339.  fix: none.
  - [Major] (Verified by reading; runtime Inferred) A kept line does not survive a re-prepare. Keep appends an operator
    row to the prepared envelope (ComposerSendGate.cs:736-737) but never writes the draft. The keep callback calls
    _gate.KeepLine only (ComposerSurface.cs:1543-1546), and Sync states "the draft holds the operator's lines, not the
    model's" (ComposerSurface.cs:2303). A new envelope seeds operator rows only from draft.GoalValues
    (PreCompile.cs:116-125). The reuse cache is in memory (ComposerSendGate.cs:175). So after an edit to the prompt,
    or after a restart, a line the user accepted is open again and is re-derived. It may come back different.
    fix: Keep writes the value to the draft as the user's line (condition C1).
  - [Major] (Inferred) A currentness leak once Ruling 178 lands. CurrentnessFold compares the live projection_sha with
    the attempt's (Folds/CurrentnessFold.cs:40-48). On main, NotebookModel.CurrentnessOf still compares body_sha
    (NotebookModel.cs:210-234). Ruling 178 makes it delegate to the fold (T24). projection_sha hashes the confirmed
    lines (Projection.cs:182-210). If the "live" sha comes from a fresh model call, a re-rolled proposal marks an
    attempt stale with no edit by the user.
    assume: the live sha is computed from the newest prepared envelope's stored rows. Confirm: T24's B-slice code at
    review. If false: false-stale results.  fix: condition C2.
  - [Major] (Verified) Model output can change access. A derived Goal plus Done when flips TurnShape from Message to
    GoalBlock (ComposerDraft.cs:122-125). That raises the tier above T0 and grants the write lease when patterns exist
    (ComposerCompiler.cs:69-73, :105-111). It also turns the terminal TurnState from Answered to Completed
    (NotebookRuns.cs:781). This is gated by a human: Keep under advisory, Send-as-confirmation under agentic
    (two gestures, ComposerSendGate.cs:321-328). The shape-flip-kept ≥ 80 % floor measures it.  fix: none beyond
    keeping agentic behind Gate 2.
  - [Minor] (Verified) The settings help text is constant. "Mechanical-only: one gesture, you fill the structure." is
    shown under every mode (SessionDocumentSurface.cs:1413).  fix: condition C3.
CLEARS-THE-VETO: yes, under C1–C4. Eval harness present: yes (Gate 2 tooling). Determinism guarded: yes under advisory
  (only operator bytes enter projection_sha), and under agentic the derived rows are stored and the fold rebuilds them
  (`aide compile fold`); C2 closes the currentness path. Side effects verified: yes (Keep or Send confirmation).
RESIDUAL RISK: no measured cost or latency for real compile prompts; the confabulated-tool-XML failure
  (compile-pin-spike.md:373-381) is caught as malformed only if the validator reads `<invoke ` (not opened here);
  model drift between Gate 1 runs relies on the triple check.
```

**The eval, stated for the build (from ADR-0036; it already exists):**

- **What it is.** `tools/compile-eval/score.py` folds every `called` and `decorated` row across session files into
  `compile-eval-admission.json` (`compile-eval-admission/1`, `src/AiDe.Core/Sessions/CompileAdmissionGate.cs:36-39`).
  The treatment arm is `opened.compile_mode ∈ {agentic-advisory, agentic}` with a `called` row. The labels are what the
  user did with each proposal.
- **What it measures, per proposal.** Acceptance (kept), missed (absent but filled by the user), emptied, edited, and
  normalised edit distance p50/p95. Also span resolution, schema_fail, degraded by reason, calibration by confidence
  bucket, latency (right-censored), and tokens in and out (ADR-0036 and `docs/proof/compile-call.md:110`).
- **The thresholds (fixed in advance, judged on the holdout, revised only stricter).** `schema_fail ≤ 2 %` ·
  `applied_denied = 0` and `tool_calls = 0` over every `called` row · degraded `(timed_out + malformed + unavailable) ≤
  5 %` · resolving spans `≥ 90 %` · acceptance `≥ 60 %` · missed `≤ 20 %` · emptied `≤ 20 %` · shape-flip kept `≥ 80 %`.
  Latency is reported, not a floor (ADR-0036:107-126). The A6 ring re-runs on any change to
  `(contract, prompt_sha, profile.sha)`, with k ≥ 3 paired medians (US-D11).
- **What gates what.** Gate 1 gates advisory, which shows suggestions and needs Keep. Gate 2 gates `agentic`, where
  Send confirms. On this machine, Gate 1's artifact and frame log are present at `~/.aide/proof/` (listed, dated
  2026-09-13). assume: the recorded triple still equals the installed adapter. Confirm: `CompileModeGate.Evaluate` at
  runtime (CE-0016 when it differs). If false: advisory is unselectable until P-D5 is re-run attended.

## 5. The decision request (the draft the Leader raises)

**Question.** Should the compile step's model fill Goal, Done when and Not in scope by default (AC-W01a), by exposing
the compile-mode setting and making `agentic-advisory` the default for new sessions when Gate 1 admits? That would
amend Ruling 68's `mechanical-only` default.

| | Option | What the user sees | Cost |
|---|---|---|---|
| **A (recommended)** | Expose C-3's compile-mode control (through `SetCompileMode` and the gate). New sessions default to `agentic-advisory` when Gate 1 admits, and fall back to `mechanical-only` with the gate's reason when it does not. Conditions C1–C4 apply. | The first press of Send prepares. The three lines arrive marked *? derived*. The user keeps or edits them, then presses Send again. | One model call per new prompt on the user's subscription, bounded at 60 s. Builds Gate 2's corpus. |
| B | Expose the control only. The default stays `mechanical-only`, and the user opts in per session. | No change until the user finds the setting. | None by default. The corpus grows only from users who opt in. |
| C | Keep the current state, and record that AC-W01a's "where possible" means a template or the user. | Blank lines with *— fill in*. | None. It contradicts the user's stated requirement. |

Rejected outright: a new or separate model call, or a heuristic deriver. Either would be a second definition of the
lines with no eval, and `StructureDeriver`'s contract is "it never invents a plausible value" (`ComposerSurface.cs:2357-2359`).

**Recommendation: A**, with these conditions:
- **C1.** Keep writes the draft, so an accepted line survives a re-prepare and a restart.
- **C2.** Currentness reads `projection_sha` from stored rows, never from a fresh call (bind it to T24's Ruling 178 work).
- **C3.** The settings row shows the real mode, its gate reason, and help text that matches the mode.
- **C4.** The first 50 advisory `called` rows are read for cost and latency before anyone proposes a default for `agentic`.

`agentic` stays behind Gate 2 and is unchanged.

**Reversibility.** High. The mode is a per-session setting, it applies to new envelopes only, and the gate is evaluated
at every read (`CompileModeGate.cs:43-44`). Reverting the default is one constant (`SessionConfig.cs:136`) plus the
ruling. Envelopes already written keep their `opened.compile_mode`, and history stays true.

**Blast radius.** Every new session's composer. Send becomes two gestures under an agentic rung
(`ComposerSendGate.cs:321-328`). One model call per Prepare. The envelope file grows by a `called` row and up to three
`derived` rows per Prepare. The shape can flip from Message to GoalBlock only through Keep. No schema change, no
`RunEvent` change, no change to the route fingerprint.

**Fallback (the default if the Owner does not rule).** B. Expose the control truthfully (C3) and keep `mechanical-only`
as the default. That honours Ruling 68 and leaves the user one setting away from the prefill.

**Owner reads before ruling.** Ruling 68's text, for why `mechanical-only` was made the default; this note's
section 4, for the two Major conditions.
