---
id: adr-0033-prompt-compilation-bounded-context
title: "ADR-0033 — Prompt Compilation is a bounded context in AiDe.Core with one seam (typed text → compiled envelope + projections); Project() is the sole assembler at the composer's send site; budget and task class are projected from settings that carry explicit defaults"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-d"
tags: [architecture, compile, composer, envelope, projection, spawn-contract, addendum-d, dm-data-modelling]
links:
  - { to: architecture, rel: implements }
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: spec-addendum-c-perspectives, rel: refines }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: note-addendum-d-compile-trigger, rel: relates-to }
  - { to: note-addendum-d-lease-source-text, rel: relates-to }
  - { to: adr-0034-envelope-event-store, rel: relates-to }
  - { to: adr-0035-compile-session-binding-and-pin, rel: relates-to }
  - { to: adr-0028-mode-cohort-not-partition, rel: refines }
review-by: 2027-03-11
review-suggested: []
summary: >-
  The compile step is a new bounded context, AiDe.Core/Compilation, whose one seam is
  Compile(typed text, settings, context) → compiled envelope, and whose one projection
  Project(Fold(events)) is the sole function that assembles the CT19 block, the lease and the prompt
  for ComposerSendGate.Send — GovernedRunRequest, SpawnContract.Validate, LeaseDerivation and
  TemplateCompiler are unchanged. The operator's two decisions of 2026-09-11 are reflected: budget is
  an optional cap projected onto the unchanged RunBudget as a declared subscription-bounded value when
  absent; the session's default task class is the explicit value free-form, changeable per prompt,
  with no refusal for a missing class. Rejected: a compile inside the composer's WPF surface; a
  second request type for the run; a nullable budget in the contract.
---

# ADR-0033: Prompt Compilation is a bounded context with one seam and one projection

- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 (`addendum-c-chain`); the
  Enterprise Architect, Patterns Expert and **Data & Persistence Architect** in Peer Mode; attacked at
  the gate (D&P and Security hard vetoes)
- **Context spec/architecture:** `spec-addendum-d-compile-step` page one, §A6 (the domain model the
  D&P Architect passed — architected to, not re-modelled), §A7–§A12, US-D1–D3, US-D7, US-D12;
  Rulings 63–70; the operator's decisions of 2026-09-11 relayed by the conductor (audit
  `al-01M297VC0HTFJP761D9BVE9Z72`; being filed by the Owner) **[Verified — the audit entry is
  committed on `main` and its words match the quotations below; merged into this branch at close]**

## Context

Addendum D fixes the domain: **Prompt Compilation** — everything between the operator's typed text
and the run request — with the **Envelope** as its aggregate (the fold of append-only events), the
**craft profile** as a Type-2 dimension, and the lease, shape, tier and effective fan-out as
**projections, never stored** (DM7). Ruling 63 charged A1 with *leaving a seam* for the compile step
and *not fixing its internals*; the spec has since fixed them and A1 now architects **to** them.

What exists today is three unrelated transforms and an assembly, none of which decorates or records:
`ComposerCompiler.Compile` (pure render, `ComposerCompiler.cs:23-27`), `TemplateCompiler.Compile`
(`TemplateCompiler.cs:19-50`), `LeaseDerivation.Derive` (`LeaseDerivation.cs:49-50, 92-104`), and
`ComposerSendGate.Send` building `GovernedRunRequest` from the rendered view plus host-side context
(`ComposerSendGate.cs:155-169`) **[Verified]**. `GovernedRunRequest` has fourteen parameters and
exactly two construction sites, asserted by name
(`C16_ExactlyTwoSitesInTheProductConstructAGovernedRunRequest`,
`TheSendVerbIsHostOwnedTests.cs:189`) **[Verified]**. `GoalBlock.Budget` is `RunBudget(int Requests,
long Tokens)`, and `SpawnContract.Validate` refuses a missing budget and a non-positive one,
tier-blind (`GoalBlock.cs:9, 51-57, 146-157`) **[Verified]**. `ComposerSendContext.TaskClass` is
*"Required, with no default"* (`ComposerSendGate.cs:18, 30`) **[Verified]**.

The load-bearing questions are (1) **where the context's boundary falls** so that the contract to
the agent plane does not move (US-D12), (2) **how many functions may assemble a run request from an
envelope** (DM11 b: `Project()` has one definition and a **closed, named set of call sites** — the
render, Submit, and the eval's `aide compile fold`),
and (3) how the two operator decisions of 2026-09-11 land without changing the contract's shape.
LOA principles: P2, P3 (the model proposes, the typed boundary validates, the projection assembles),
P7 (the envelope store is the state; the model is stateless), P9, P10.

## Decision

We will:

1. **Create the bounded context as `AiDe.Core/Compilation`** (a sibling of `Presentation/Composer`,
   `Sessions` and `AgentPlane`; the assembly boundary is the same, the namespace is the context). It
   owns: `Envelope` and its five event records (`compiled-envelope/1`), `Fold`, `Current`,
   `Confirmed`, `EffectiveMode`, **`Project`**, the mechanical **pre-compile** (`PreCompile`: shape ·
   tier rule §A9 · effective fan-out · lease display · family/profile selection · constitution
   manifest by frontmatter line-scan · history window · template applied · attachments by reference ·
   the `ceilings` and `task_class` snapshots), the **`CompileOutputValidator`** (the typed boundary,
   §A8.4: raw text → JSON → schema → allow-list and open lines → type → mention scan over values and
   `notes` → span resolution → `DerivedDecoration[]`). The mention scan calls **one shared member,
   `LeaseDerivation.HasMention(string)`** — the regex is `private` today (`LeaseDerivation.cs:49`),
   so this is an **additive `internal` member** (reached from `Compilation` in the same assembly;
   `InternalsVisibleTo` for the tests) — **not** `public`, so the US-D12 reflection assertion on
   `LeaseDerivation`'s public signatures stays byte-identical and whitelists nothing; recorded as
   US-D12's single extension of `LeaseDerivation`'s surface: a copied regex would be two
   definitions of the mention grammar, and drift between them is the bypass class; a test asserts
   the validator and `Patterns` share the one `Regex` instance. Then the
   **`CompilePromptAssembler`** (the fixed host header and the `compile-prompt/1` template as
   **embedded resources**, `prompt_sha` over their bytes), and the **`EnvelopeStore`** (ADR-0034).
   It **references** `Presentation/Composer` (`ComposerCompiler` as the render, `LeaseDerivation`),
   `Sessions` (`TemplateCompiler`, `SessionConfig`), `AgentPlane` (`GoalBlock`, `RunBudget`,
   `EngineCatalog.Find(engineId).Provider`) and `Watcher` (the `TaskClasses` vocabulary beside
   `ScoreSegment`) — and is referenced by nothing in Core. The model call itself is the App's
   composition (ADR-0035). **Realised as (a note, not a class-per-noun instruction — the specs'
   own "realised as" rule):** `Envelope` with `Fold` and three query members (`Current`,
   `Confirmed`, `EffectiveMode`); one static `Projection.Project`; one static `PreCompile` (the
   only entry); one static `CompileContract` with `Assemble`, `Validate` and `Version` (the two
   halves of one versioned pair — `contract_version` names the pair); the `EnvelopeStore`. Six
   types, not fourteen.
2. **One seam in, one projection out.** In: `Compile(sourceText, settings, context) → Envelope` —
   the pre-compile appends the mechanical rows; the agentic stage (ADR-0035) appends `called` and
   `derived` rows; Prepare appends `operator` rows. Out: **`Project(Fold(events)) → (Shape, Tier +
   rationale, FanOutCap, GoalBlock, Lease, Prompt, TaskClass, projection_sha)`** — where
   **`projection_sha`'s domain replaces the spec's `opened.task_class` member with
   `Current(task_class).value ‖ Current(task_class).source`** (the spec hashed the snapshot because
   task class was a setting; under Ruling 70 the sent class is the decoration's current value, and
   an `operator` per-prompt change must change the witness — the Test Architect's Blocker at this
   gate; P-D2 and §A12.2 inherit this domain), and **US-D1 b2's falsifier "a `task_class`
   decoration" is superseded** by *"a `task_class` row with `source: derived`"* (the deny-list keeps
   `task_class`; `session-default` and `operator` rows are the design), as is US-D1 b2's clause
   *"the projected `TaskClass` is the context's"* for the `operator` case — spec supersessions quoted
   here as findings for the Owner, the spec text being outside this node's write scope — **the sole
   function that assembles what `ComposerSendGate.Send` puts into `GovernedRunRequest`**, with
   **one constant list of exactly three call-site paths** — the composer render,
   `ComposerSendGate.cs`, `Cli/CompileFold.cs` — asserted by name (DM11 b's census; the Tech Lead's
   condition: never "two" anywhere, or the census goes red when the CLI verb lands). `Send` keeps its
   place as the second named construction site (C16 stays green); it changes *what it passes*, not
   *that it constructs*: `Goal: projection.GoalBlock`, `Lease: projection.Lease`, `Prompt:
   projection.Prompt` (`text_sha256` witnessed), `TaskClass: projection.TaskClass`. **`Project()` is
   the one `LeaseDerivation.Derive(` caller in the product**, and its argument is the
   `opened.source_text` symbol (Ruling 66 — the editor's text; the write-scope line's
   `Patterns(source_text)` is the same call over the same bytes); the census of D §A13.3 is restated
   accordingly: `new Lease(` at two named sites (`LeaseDerivation.cs`, `ConductorEntry.cs`), zero
   hits for the three alternative patterns, and **one `LeaseDerivation.Derive(` call, in
   `Compilation/Projection.cs`, and one `LeaseDerivation.Patterns(` display call, in
   `ComposerSurface.cs`, each with the `source_text` symbol as its argument** (qualified names — a
   bare `Derive(` collides with `DeterministicSignalsDeriver.Derive(`; the argument is checked at
   both sites, which is the F-2 class). (Ruling 66's interim fix on `main` — `ComposerSendGate.cs:164` and
   `ComposerSurface.cs:449` passing the editor's text — is slice C-0; D-1 then moves the one call
   into `Project()`.) `GovernedRunRequest`, `SpawnContract.Validate/Authorize`, `LeaseDerivation`
   (bar the additive `HasMention`) and `TemplateCompiler` are **unchanged in signature** (US-D12's
   reflection assertion).
3. **Budget is an optional cap; its absence is a declared value on the unchanged contract.** The
   session setting is `budget_cap: {requests, tokens} | none` (the operator: *"budgets should be max
   … by default and then optionally I can enforce a cap"*). The envelope's `ceilings` snapshot carries
   `budget_cap` explicitly (`null` means *none chosen*). **`Project()` never reads a live setting:**
   a fold with no well-formed `ceilings` row is *incomplete* — Prepare goes `stale`, the pre-compile
   appends a fresh `ceilings` row (its writer named on `inputs`) and only then does `Project()` run;
   a row lacking the member is skipped and counted like any malformed row, and the fresh row is
   what the fold reads — so `projection_sha` is rebuildable offline by the eval, which has no live
   setting (DM11 b/c; the D&P Architect's Blocker at this gate). `Project()` maps the row onto
   `GoalBlock.Budget` **without changing `RunBudget`'s shape**: a cap projects as
   `new RunBudget(requests, tokens)`; none projects as the **declared constant
   `RunBudget.SubscriptionBounded`** (`Requests = int.MaxValue`, `Tokens = long.MaxValue`) — positive,
   so `Validate` passes tier-blind. **`IsSubscriptionBounded` is a value predicate** (equality on both
   fields — the record crosses JSON at `ConductorEntry.cs:120-122`, so reference identity is lost),
   and **the sentinel never reaches the sent bytes as a number**: `RenderGoalBlock`
   (`ComposerCompiler.cs:83-99`, the prompt the engine reads) renders the case as *"budget: bounded by
   the subscription (no cap declared)"*, Prepare's inherited line as *"budget: bounded by your
   subscription"*, and a cap as *"budget cap 250 requests / 600k tokens"*; a **C16-shaped cap**
   asserts that every `.Requests` / `.Tokens` read on a `RunBudget` in `src/` is in exactly
   `["ComposerCompiler.cs", "ConductorEntry.cs", "GoalBlock.cs", "Projection.cs"]`, that the
   **render** sites (`ComposerCompiler.cs`, `Projection.cs`) are guarded by `IsSubscriptionBounded`,
   and that the two **non-render** reads are the named exemptions — `Validate`'s positivity check
   (`GoalBlock.cs:151-156`, byte-identical by US-D12) and `ConductorEntry`'s field copy
   (`ConductorEntry.cs:122`) — a named-member cap with named exemptions, not a count of zero. **No numeric budget is required
   anywhere, and no compile-time default exists** (F-6 stands: the writer is named on the snapshot).
   **The contract step:** the draft's per-block `budget` field (`ComposerDraft.ParseBudget`,
   `ComposerDraft.cs:178-189`; its widget) is the interim writer — an empty field ⇔ `budget_cap:
   null` ⇔ `SubscriptionBounded` from the first slice, so `Validate` no longer refuses an empty
   draft budget — and is **retired in the same node that lands the session setting** (P1's
   session-settings node), with a census that `ParseBudget` has no caller afterwards: one quantity,
   one home, one writer (DM-A).
   **Spend is measured per turn regardless of a cap** (IO axes): the run's `RunEventCost` from the
   ACP usage frames (`AcpRunEventMapper.cs:172-182`) and the compile's `called.cost` are the
   emitters; a session-level `spend` is a fold of them; *cap utilisation* is computed only when a cap
   exists; absent usage degrades to *not recorded*, never 0; the subscription's own bound is observed
   as the account's `quota-degraded` health (`ProviderRegistry`), so "bounded by the subscription" has
   an observable, not an assumption. Phase 1 **validates, never enforces** a cap (Ruling 26c parity;
   `GoalBlockFields`' own comment).
4. **Task class: the session's default is the explicit value `free-form`; per prompt it is a
   mechanical decoration; nothing refuses a missing class because none can be missing.** The
   operator: *"I do not need to choose a task class - the basic should be free-form upon open, and
   then I can change it."* Two quantities, each with one home: the **vocabulary constant**
   `TaskClasses.FreeForm` (declared beside `ScoreSegment.Unclassified`, `Leaderboard.cs:36`; a
   C16-shaped census asserts the quoted literal `"free-form"` appears in exactly `["Leaderboard.cs"]`
   in `src/` — red at zero today, green at one) and the **session's chosen default**, the session
   config's `default_task_class` (its code home is P1's session-settings node — F-6);
   `ComposerSendContext.TaskClass` is **populated from** that field, never a second literal, stays
   non-null, and has one compute reader: the pre-compile, which writes the `task_class` decoration
   with `source: session-default` from it. The
   envelope's `task_class` decoration is `{value, source ∈ {session-default, operator}}` (Ruling
   70); `Project().TaskClass = Current(task_class)`; `GovernedRunRequest.TaskClass` stays required
   and non-null. The Send refusal Ruling 70's text named (*"choose a task class for this prompt"*)
   is **not built** — overruled by the later operator decision. "Use as default" stays a separate
   explicit act (Ruling 70 condition 3). **This amends ADR-0028, and says so:** ADR-0028's rule is
   *explicitness* — an explicit caller-chosen class at both doors, no door default — and
   `free-form` is, structurally, a default at the composer door (the D&P Architect's finding at this
   gate; DC-110's shape). The operator's decision overrides it for this door with two conditions
   that keep the cohort honest: (i) the default is the operator's, visible on the sheet and the
   session header, and its **provenance is recorded per episode as an expand-only cohort attribute
   `task_class_source ∈ {session-default, operator}` beside `ScoreSegment`, never inside it** (the
   `mode` column's own pattern, ADR-0028), so the board can tell a chosen `free-form` from a
   defaulted one; (ii) `free-form` is a comparable class, so a defaulted episode ranks (unlike
   `Unclassified`) — recorded as the decision, not hidden. ADR-0028 carries an amendment pointer.
   The cohort attribute is, deliberately, a **second home** for the envelope's
   `task_class.source`: the envelope is purgeable work data and scores are not, so the cohort must
   carry its own provenance (the Simplifier's finding, accepted in writing under the D&P
   Architect's condition).
5. **Four stages, two triggers, one gesture** (Ruling 67): the pre-compile runs in memory on the
   debounced draft and persists nothing (bound 1 s — a defect signal, not a degraded state; **in a
   test it is an injected hang guard with a slow fake, never `elapsed < 1000`** —
   `verify-perf-assertions.py`'s rule — and an overrun at runtime emits `compile.stage{stage:
   pre-compile, over_bound: true}`; US-D2 b3's "a CI test" is realised that way, its p95 at P-D4); the
   Send gesture opens the envelope, runs the agentic stage when the mode admits it, and enters
   Prepare; the next gesture submits. `Stale` and `Prepare again` as §A10/§A11.
6. **Instrumented by default** (IO1–IO12, §A10.3): the durable record is the `called`, `submitted`
   and `consumed` rows; `compile.stage{stage, duration_ms, outcome, over_bound?}`,
   `compile.degraded{reason, error_code}` and `compile.mode.changed{from, to, trigger}` run events
   are emitted on the normal path into the session's run-event vocabulary (spec v1 §7.2) — an
   emission, never a second store — **with their correlation keys named and no key pun**: the
   compile's facts are first recorded as attributes on the `compile-call.compose` Activity span
   (`envelope_id`, `call_seq`, `stage`, `outcome`, `error_code` — OpenTelemetry is the data model),
   and a **translator at the seam** builds the Console-stream `RunEvent` from them with `Kind =
   compile.*` (the open vocabulary) and **`Ext = {origin: "compile", envelope_id, call_seq, …}`** —
   `Ext` being the field made for unprojected keys (`RunEvent.cs:150-153`). `RunId` and `AgentId`
   are required (`RunEvent.cs:165-173`) and are set by the translator to the values the session
   document's Console channel uses for host-originated events (`/design-slice` names them); **no
   consumer may infer origin from `RunId`/`AgentId`** — overloading them with an envelope id and a
   literal would be a type pun every ledger must know (the Patterns Expert's finding); origin is
   `Ext.origin`, and a test asserts no ledger counts a `Kind = compile.*` event as a run.
   `called.error_code` is a stable code from the `AP-00xx` family (new codes for
   bound exceeded, schema fail, all dropped, pin mismatch) beside the prose `reason`. **Session
   spend has a home:** `session.spend{requests, tokens_in, tokens_out, cache_read, n_measured /
   n_total}` is folded from the `called` rows and the run's `RunEventCost` — the IO floor; *where*
   it is shown (the session header, or the compile line) is a spec addition for the UX lens and is
   recorded as a finding for the Owner, not decided here (the spec's header row is `ceiling ·
   budget · compile mode`, and its named reader of spend is the profiler). Every percentile with `n_measured / n_total`; `prefix_measured` from the wire,
   never the tokenizer estimate. `app.start` already names the binary (INV-0008 Fix D), so a compile
   report is attributable to a build.

## Alternatives considered

- **The compile step inside `AiDe.App/Workbench/Composer` (beside `ComposerSurface`):** rejected —
  the fold, the projection, the tier rule and the validator are pure and headless; putting them in
  the WPF assembly makes every US-D test STA and couples the eval harness (`tools/compile-eval`) to
  the App. Only the model call's composition is App (ADR-0035).
- **A second request type (`CompiledRunRequest`) carrying the envelope into the agent plane:**
  rejected — US-D12 fixes the contract as unchanged; the plane consumes the CT19 block, a lease and a
  prompt, and needs no envelope; a fifteenth parameter "for the envelope" is the named falsifier.
- **A Python fold/projection in `tools/compile-eval` (a second definition of `Fold`/`Project`):**
  rejected — DM-A; drift between two implementations is undetectable by a rebuild test that runs
  one of them. **The harness calls the product's own entry** — `aide compile fold --session <id>`
  computes `Project(Fold(rows))` from the store's rows and emits both as JSON — and Python only
  scores and reports; `Project()` keeps one definition, and the CLI verb is its **third named call
  site by path** (`AiDe.App/Cli/CompileFold.cs`; the census names all three), so the P-D2 rebuild
  recomputes the projection from rows and is never a hash of a stored projection against itself.
  The verb lands in slice D-1 (the store and the fold) and is an E7 compute-reader row.
- **A nullable/optional `Budget` in `SpawnContract.Validate` for the no-cap case:** rejected —
  a contract change (the six-field rule is tier-blind by design and the conductor expects six);
  a declared maximal `RunBudget` keeps `Validate` byte-identical and makes "no cap" a value the
  conductor can read (`IsSubscriptionBounded`) rather than an absence it must guess at.
- **A string sentinel for budget:** rejected — `RunBudget` is `(int, long)`; a string is a type change.
- **`null` task class with a Send refusal (Ruling 70 as first filed):** superseded by the operator's
  later decision; `free-form` as an explicit default removes the refusal and the null path.
- **Storing the lease / shape / tier / effective fan-out on the envelope:** rejected by the D&P
  Architect at the spec gate (DM-A: one quantity, two homes) — architected to as given.

## Consequences

- **Positive:** the contract to the agent plane is untouched and the census that proves it stays
  green; every compile fact has one definition (`Project`, `Current`, `Confirmed`, `EffectiveMode`,
  `Fold`) shared by render, Submit and the eval; the operator's two decisions land as *values* the
  projection reads, not as new required fields.
- **One-way door, labelled:** `RunBudget.SubscriptionBounded` crosses JSON into persisted run and
  result files (`ConductorEntry.cs:120-122`); every file ever written with `2147483647 /
  9223372036854775807` must read as subscription-bounded forever, so the value predicate is a
  reader that can never be removed (the Enterprise Architect's finding) — unlike the layout files
  (ADR-0032) and the envelope (ADR-0034), whose rollbacks are file-level.
- **Negative / accepted:** `ComposerSendGate.Send` grows a dependency on `Compilation.Project` (the
  one place the two contexts meet); the `SubscriptionBounded` constant is a magic value on the wire,
  mitigated by the named reader and by Phase 1 not enforcing budgets at all.
- **Follow-ups / new risks:** ADR-0028 gains an amendment pointer and its inbound neighbours a
  `review-suggested` flag (V16). The session settings the projection reads (fan-out ceiling,
  `budget_cap`, compile mode, default task class) have **no code home** (`SessionConfig.cs:20-57`,
  F-6) — P1 builds the home in Addendum C's session-settings node; until then the `ceilings` snapshot
  names the draft's held values as its writer. Ruling 66's F-2 fix (the lease from the editor's text)
  is **not yet on `main`** at `a3f760a3` (`ComposerSendGate.cs:164` still derives over
  `compiled.Text`) **[Verified]** — P1 sequences it first (it is C17's class and two argument
  changes).

## Falsifying tests (headless)

1. **US-D12:** `GovernedRunRequest` has fourteen parameters; the public signatures of
   `SpawnContract.Validate`, `LeaseDerivation` and `TemplateCompiler` equal `main`'s by reflection
   (`HasMention` is `internal`, so nothing is whitelisted); `C16_ExactlyTwoSites…` is green.
2. **DM11 (b):** `Projection.Project(` has exactly the three named call sites by path (the
   composer's render, `ComposerSendGate.Send`, `Cli/CompileFold.cs`) — one constant list in the
   test; the paired test renders Prepare from a fold, captures `(shape, tier,
   rationale, cap, budget, task class)`, submits the same fold and asserts the `GoalBlock` equal;
   **the rebuild over an envelope with one `operator` `task_class` row whose value equals the
   session default yields a different `projection_sha` from the same envelope without it** (so only
   `source` moves the sha — a mutation that drops `source` from the domain cannot survive); the
   eval's fold is the product's (`aide compile fold`, the third named `Projection.Project(` site),
   asserted by a golden over real rows.
3. **Budget:** a session with no cap → `Project().GoalBlock.Budget.IsSubscriptionBounded`,
   `Validate` returns no error, the inherited line reads *bounded by your subscription*, and the
   compiled disclosure contains **neither numeral** (`2147483647`, `9223372036854775807`) — the
   rendered case is in words; a cap of `(250, 600000)` projects exactly; a `ceilings` row with
   `budget_cap: null` folds to subscription-bounded; a fold whose `ceilings` row *lacks* the member
   is incomplete → `stale` → a fresh row is appended and **the rebuild (`projection_sha`) holds over
   that envelope**; a `RunBudget(int.MaxValue, long.MaxValue)` read from a request file satisfies the
   predicate (value equality); a `budget` decoration with `source: operator` or `derived` is refused
   (US-D1); the `RunBudget` member-read cap finds exactly the four named files, the render sites
   guarded and the two exemptions unguarded (a fifth file, or an unguarded render, fails);
   after the session-settings node, `ParseBudget` has no caller.
4. **Spend is measured with no cap:** the emitter records a turn's `RunEventCost` when the wire
   carries usage and *not recorded* when it does not — never `0`; cap utilisation is absent (not 0 %)
   when no cap exists (IO11: correct value, correct decline).
5. **Task class:** a new session with nothing chosen sends `TaskClass == TaskClasses.FreeForm` with
   the decoration's `source == session-default`; the segment `ScoreSegment(ws, "free-form", v)` is
   comparable; the episode's `task_class_source` cohort attribute reads `session-default`; a
   per-prompt change to `refactor` reaches that request only, the default stays `free-form`, and
   that episode's attribute reads `operator`; no Send refusal exists for the class (a refusal fails);
   the literal `"free-form"` appears exactly once in `src/`; `ComposerSendContext.TaskClass` equals
   the session config's `default_task_class` (never a second literal).
6. **Tier rule:** the fourteen enumerated inputs of §A9 assert `(tier, rationale)` with
   `structure_source` (P-D1).
7. **Instrumentation:** a `called` row without `latency_ms`/`cost`/`model_observed` (or their
   declared *not recorded* forms), a `0` for a stage that did not run, or a write under `runs/` or
   `session-events.jsonl` fails (US-D10).

## LOA mapping

Archetype: **Advisory channel over a deterministic hot path** (F) — with F's non-blocking clause
**bounded, not upheld, under the agentic rungs** (the Send gesture waits ≤ 60 s for the compile;
recorded as a deviation in `docs/architecture.md` §C/D.7 with its bounds) — the pre-compile and the
projection are 0 % AI; the agentic stage is one T3 call behind a typed boundary and a human gate
(ADR-0035/0036). Tiers: **T0** for `PreCompile`, the tier rule, `Project`, `Fold`, the validator,
the store; **T3** only for the three structure lines. Patterns (named as the Patterns Expert
corrected them): **Bounded Context** (a **Conformist** to `AgentPlane`; a **Shared Kernel** with
`Sessions`), **Event-sourced aggregate (fold)**, **Projection / CQRS read model**,
**Anti-Corruption Layer / Translator** at the seam — `ComposerSendGate.Send` mapping `Projection →
GovernedRunRequest` (the supplier's Published Language; an ACL named makes "a fifteenth parameter"
visibly an ACL leak), and the translator that turns compile-stage span attributes into Console
events (rule 6); **Schema-Constrained Output (2.5)** as the *contract's intent* (the ACP transport
has no JSON mode, so the enforcement is post-hoc) with **Deterministic Verifier (3.1)** as the
enforcement (`CompileOutputValidator`, `Validate`, the tier rule, the lease census); **Special Case**
(Fowler) for `RunBudget.SubscriptionBounded` — the named-member cap is the pattern's guard, not an
apology; **Receipt Ledger (4.3)** (the `called` row). Conformance: C1 (the compile host is
tier-annotated), C3 (every call is a `called` row inside a `compile-call.compose` activity), C4, C5
(no model output reaches a side effect without the validator and Prepare), C9.

## Evidence

- **Verified (read at `a3f760a3`):** `ComposerSendGate.cs:11-36, 120-169`; `GoalBlock.cs:9, 15-34,
  51-57, 127-160`; `ComposerCompiler.cs:23-27`; `LeaseDerivation.cs:49-50, 92-104`;
  `TemplateCompiler.cs:19-50`; `TheSendVerbIsHostOwnedTests.cs:189-213`; `Leaderboard.cs:25-70`
  (`ScoreSegment`, `Unclassified`, `IsComparable`); `AcpRunEventMapper.cs:172-182` (per the spec;
  not re-opened here — **Inferred**).
- **Rulings:** 63, 64, 66, 67, 70; the operator's two decisions of 2026-09-11 (relayed, audit id
  above).
- **Spec:** `spec-addendum-d-compile-step` §A6 (D&P-passed model), §A9 (the tier rule), §A12.2 (the
  projection table, amended here for budget and task class).
