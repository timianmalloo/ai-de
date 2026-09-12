---
id: proof-mechanical-compile
title: "Proof Pack — CV-2, the mechanical compile: the append-only envelope store, the five event records, the fold and its projections, PreCompile, Projection.Project as the one producer of the sent bytes, Prepare's editable derived lines, purge, and the compile contract shipped inert"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "addendum-d"
tags: [proof-pack, conversation-lane, cv-2, addendum-d, compile, envelope-store, projection, prepare, purge, adr-0033, adr-0034, dm-data-modelling]
links:
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: implements }
  - { to: adr-0034-envelope-event-store, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: proof-composer-as-conversation, rel: refines }
  - { to: note-addendum-cd-architecture-p1-inputs, rel: relates-to }
  - { to: adr-0028-mode-cohort-not-partition, rel: relates-to }
  - { to: adr-0037-family-craft-profile-dimension, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Evidence for CV-2 on the Conversation lane (Addendum D slice D-1 with D-0 folded).
---

# Proof Pack: CV-2 — the mechanical compile

- **Tier:** T2 · **Fan-out cap:** 3 (persona reviews) · **Session:** `cv-2` · **Author:** `claude-cv-2`.

## E7 change-surface list (written before coding; ticked at close)

| Surface | Change | Writer | Compute reader | Status |
|---|---|---|---|---|
| store | `EnvelopeStore` (`compiled-envelope/1`; one file per session; `Append` + reader; `prev_sha` chain; `FileShare.None`) | `ComposerSendGate.Send` (opened · decorated · submitted), `SessionDocumentSurface` (consumed) | `EnvelopeStore.Read` / `ReadFile` → `Fold` | pending |
| event records | `Opened · Decorated · Called · Submitted · Consumed` (append-only facts) | as above | `Envelope.Fold` | pending |
| fold / projections | `Envelope.Current`, `Confirmed`, `EffectiveMode`, `Outcome`; derive, never store | — | `Projection.Project`, Prepare, `aide compile fold` | pending |
| `PreCompile` | the mechanical rung: `ceilings` snapshot (writer named), `task_class` (`session-default` / `operator`), `family_profile` (none), `template_applied`, `attachments` (refs), the structure lines, the tier override | the Send gesture | `Projection.Project` | pending |
| `Projection.Project` | the ONE producer of the sent bytes; tier by §A9 + R4; cap; budget; lease from `opened.source_text`; `projection_sha` | — | `ComposerSendGate.Send`, `ComposerCompiler.Decorations`, `Cli/CompileFold.cs` | pending |
| wire | `GovernedRunRequest` byte-identical (fourteen parameters; C16 at two sites) | `ComposerSendGate.Send` | `GovernedRunHost` (unchanged) | pending |
| client | `ComposerSendGate.Send` via `Project()`; `ComposerSendContext.TaskClass` from `default_task_class` (non-null) | `SessionComposerBinder` | `ComposerSurface` | pending |
| UI | Prepare: the tier control and the class control on the decoration line (editable, with provenance), the compiled disclosure rendering `Current`, the degraded reason | `ComposerSurface` | the operator; the E7 consistency test | pending |
| compute reader | `task_class_source` (expand-only, v6→v7) read by `ScoredEpisode.TaskClassSource` and `LeaderboardCell.DefaultedClass`; `aide compile fold`; `aide session purge` | `SessionDocumentSurface` (after the run) / the CLI | `Leaderboard`, the eval, the operator | pending |
| tests + census + ledger | the reds below; the two censuses; the terminal ledger | — | — | pending |

- **Change:** `lane/conversation-cv2` from `main` `5ce4b08e`, merged with `main` `ca7443e8` (SH-3, X-2b,
  DC-170/171) mid-slice at the conductor's instruction (no conflicts). **Core:** `Compilation/`
  (namespace `AiDe.Core.PromptCompilation` — see finding 1): `EnvelopeEvents.cs` (the five records,
  `DecorationSources`, `DecorationNames`, `CallOutcomes`, `ConsumedReasons`, `DroppedCounts`),
  `Envelope.cs` (`Fold`, `Pending`, `Current`, `Confirmed`, `EffectiveMode`, `Outcome`, `EnvelopeFold`),
  `EnvelopeRowCodec.cs` (+ `EnvelopeHash`), `EnvelopeStore.cs`, `EnvelopeStoreException.cs` (`CE-0001…0015`),
  `EnvelopePurge.cs`, `PreCompile.cs`, `Projection.cs`, `CompileSignal.cs` (`EnvelopeIds`,
  `CompileEventKinds`, the translator), `CompileContract.cs`, `CompileOutputValidator.cs`,
  `CompilePromptAssembler.cs`; `Presentation/Composer/ComposerCompiler.cs` (`Decorations` via
  `Project()`, `IsTier`, `Compile(draft, template, block)`), `ComposerDraft.cs` (`TierOverride`,
  `TaskClassChoice`), `LeaseDerivation.cs` (`HasMention`, `MentionRegex` — internal);
  `Sessions/SessionConfigStore.cs` (`Delete`); `Watcher/Leaderboard.cs` (`ScoredEpisode.TaskClassSource`,
  `LeaderboardCell.DefaultedClass` / `ChosenClass`, `TaskClasses.Sources`), `WatcherObservationStore.cs`,
  `SqliteWatcherObservationStore.cs` (v7). **App:** `Workbench/Composer/ComposerSendGate.cs` (`Send` via
  `Project()`; `ComposerSendContext.TaskClass : string`; `BindSession`, `UseEnvelopeStore`,
  `LastSubmission`, `HistoryState`; `SubmittedEnvelope`), `ComposerSurface.cs` (the tier and class
  controls, the history reason on the settings line), `Workbench/Sessions/SessionDocumentSurface.cs`
  (the store's lifetime, `consumed`, the class-source stamp), `SessionComposerBinder.cs` (the default
  class), `Cli/CliEntry.cs`, `CompileFold.cs`, `SessionPurge.cs` (new). **Tests:** the reds below.
- **Spec / design statements this satisfies:** ADR-0033 rules 1–4, 6; ADR-0034 rules 1–4, 6–8;
  spec §A6 (the grain, DM11 a–d, f–h), §A8.3–A8.4 (inert), §A9 (the fourteen inputs), §A11 (the
  tier and class controls; the marks CV-1 built), §A12.1–A12.2, §A13.3 (a, c, d′, e–g), §A13.5,
  §A13.6 (S, T1, R, D, E), US-D1, US-D2, US-D3, US-D4, US-D6 (the override rows), US-D7, US-D12,
  US-D13; Rulings 66, 70, 72, 73, 75.

## The reds → green

| Red (plan row) | Before | After | Test |
|---|---|---|---|
| P-D1's fourteen tier inputs | no `Projection` existed (`CS0234`) | 5 R0 + 21 structure-bearing (7 × operator/template/derived) + R4 ×2 + advisory = 30 green | `TheTierRuleHasFourteenInputsTests` |
| `Projection.Project(` census | red at two sites (`Cli/CompileFold.cs` absent) | three named paths | `ProjectionProjectHasExactlyThreeNamedCallSitesByPath` |
| `LeaseDerivation.Derive(`/`Patterns(` census with `source_text` | five sites (gate ×2, compiler, draft, surface) | one `Derive(` in `Compilation/Projection.cs` over `opened.SourceText`; `Patterns(` at the projection, the draft, the display site | `EveryLeaseDerivationCallSiteIsNamedAndPassesTheSourceTextSymbol` |
| `RunBudget` named-member cap | `PreCompile.cs` read `cap.Requests` unguarded (red) | seven named files, each guarded or exempt | `RunBudgetMemberReadsAreANamedSetWithNamedGuards` |
| ADR-0034 test 1 (append-only by reflection) | `CS0234` | the exact member list; `Update`/`Delete`/`Rewrite`/`Remove`/`Truncate`/`Clear` absent | `ThePublicSurfaceIsAppendAndAReaderOnly` (+ seq, decorated-after-submitted, body, lease-name, operator-settings-name, session-id refusals) |
| ADR-0034 test 2 (two writers; a reader while held) | `CS0234` | one `[CE-0002]` refusal each | `TwoWritersOnOneFileSeeExactlyOneRefusal…` |
| ADR-0034 test 3 (`prev_sha` chain; torn; unknown schema; `/1` after `/2`; outcome not recorded) | `CS0234` | a flipped byte in line 2 → *record broken at line 2*, no envelope past it; reopen → `Append` refused `[CE-0003]`, bytes unchanged, no duplicate key; a `/2` row's key read, the `/1` writer mints seq 3 | `OneFlippedByteMidFile…`, `ReopeningOnABrokenFile…`, `ATornLastLine…`, `AnAcceptedSubmittedWithNoConsumed…` |
| ADR-0034 test 5 (purge; cascade) | `CS0234` | purge removes the file only; `..\..`, `../..`, a non-segment, a junction (a real `mklink /J` fixture) refused before any touch; the Session delete removes the directory, refuses whole while a writer holds the file, and the envelope file is gone before any sibling | `PurgeAndTheSessionDeleteCascadeTests` (10) |
| ADR-0034 test 6 (lifetime) | `CS0234` | open → held; close → a second open succeeds; a locked file degrades with `[CE-0002]` on the settings line and the document opens; no session directory → not recorded, never created; closed with a run in flight → `consumed{document closed}` | `TheDecorationTheRowTheBytesAndTheColumnAgreeTests` (4 of 6) |
| `HasMention` shared regex | no member existed | one `Regex` instance; the only string-taking members are `Derive`, `Patterns` (public), `HasMention`, `ToPattern` (non-public) | `HasMentionAndPatternsShareOneRegexInstance` |
| `projection_sha` domain | no sha existed | an `operator` `task_class` row with the same value moves it; every domain member moves it; a body change does not; the same draft twice is equal | `AnOperatorTaskClassRowWithTheSameValueChangesTheProjectionSha`, `TheShaMovesWithEveryMemberOfItsDomainAndWithNothingElse` |
| `GovernedRunRequest` byte-identical | **observed green on the pre-change gate at `f3e394dc`** (three literal goldens: goal block + mention + attachment, Message, template) | green after `Send` was rewritten — **the same bytes** | `AGoalBlockSendIsByteIdenticalToTheGolden`, `AMessageSend…`, `ATemplateSend…` |
| US-D12 signature stability | — | `SpawnContract` (2), `LeaseDerivation` (2 public), `TemplateCompiler` (1) by reflection; 14 parameters | `ThePublicSignaturesOfTheThreeContractTypesAreUnchanged…` |
| **The E7 consistency test across surfaces** | no chain existed | for one prompt: the decoration shown (tier T1 · *operator (rule said T2)* · class *defect* · lease `src/A/** · src/B/**` · goal block), the disclosure, the bytes sent (the tier section T1, cap 2, the class, the lease), the row stored (`Project(fold)` equal on every field; `projection_sha` and `text_sha256` equal to the `submitted` row), the column's provenance (`operator`) — agree; the refused run's `consumed{lane_exited, not recorded}` lands | `ForOnePromptTheDecorationTheRowTheBytesAndTheColumnAgree` |
| the inert contract (§A17 fixtures, authored) | tests written after the code; **reds observed by mutation**: the mention scan disabled → `AMentionBearingValueOrNotesIsRefusedNotStripped` red; the header dropped → `ThePromptBeginsWithTheHostHeader…` red | 23 green | `TheTypedBoundaryIsInertButRealTests` |
| P-D2's CI floor | — | `aide compile fold` over 6 envelopes the gate wrote: 6/6 rebuilt sha equal; a rewritten sha → 1/2, exit 3; a held file → `[CE-0002]`, exit 3 | `TheCompileVerbsFoldAndPurgeTests` |
| `task_class_source` (v6→v7) | a hand-written v6 fixture; no column | legacy rows read NULL, the v6 columns byte-identical, the v6-shaped reader reads the v7 file whole (the tested rollback), the stamp lands on a scored cell only and survives a re-score, the cell counts | `TaskClassSourceCohortColumnTests` (6); the `mode` position test re-scoped to second-to-last with the reason |

## The fourteen tier inputs (P-D1, §A9) — each asserts `(tier, rationale)`; (6)–(12) ×3 by `structure_source`

| # | Input | P | L | Tier · rule | Rationale asserted |
|---|---|---|---|---|---|
| 1 | empty text | no | 0 | T0 · R0 | *no goal block* |
| 2 | prose, no structure, three mentions | no | 3 | T0 · R0 | *no goal block* |
| 3 | `goal` filled, `done_when` blank, one mention | no | 1 | T0 · R0 | *no goal block* |
| 4 | `goal` blank, `done_when` filled, one mention | no | 1 | T0 · R0 | *no goal block* |
| 5 | both whitespace-only, one mention | no | 1 | T0 · R0 | *no goal block* |
| 6 | both filled, no mention | yes | 0 | T1 · R1 | *goal block filled by {you · the template · the model}, no write scope* |
| 7 | both filled, `@../x` only | yes | 0 | T1 · R1 | as 6 |
| 8 | both filled, `@src/*.cs` only | yes | 0 | T1 · R1 | as 6 |
| 9 | both filled, `@src/A/` | yes | 1 | T1 · R2 | *…, one lease* |
| 10 | both filled, `@src/a.cs @src/a.cs` | yes | 1 | T1 · R2 | *…, one lease* |
| 11 | both filled, `@src/A/ @src/A/ @src/B/` | yes | 2 | T2 · R3 | *…, 2 leases* |
| 12 | both filled, `@src/a.cs @src/b.cs` | yes | 2 | T2 · R3 | *…, 2 leases* |
| 13 | R2 then an operator override to T2 | yes | 1 | T2 · R4 | *operator (rule said T1)*; cap = min(cap(T2), 3) computed |
| 14 | an override to `T9` | — | — | refused | at the draft (`ArgumentOutOfRange`, the prior value stands); a `T9` row in a fold is not an override (the rule stands); at the store `[CE-0011]` |

`structure_source` values asserted: `operator` (an `operator` row), `template` (a `mechanical` row with
`inputs [{writer: template}]`), `derived` (a `derived` row with `call_seq`); under `agentic-advisory` an
unkept `derived` line is blank for the shape, P and the block at once (`Confirmed()`), and kept it projects.

## The byte-identical diff (`GovernedRunRequest`)

The oracle is three **literal** goldens in `TheSendGateSendsWhatProjectionProjectsTests` (the prompt bytes
spelled out, every other field a literal), independent of both implementations. Run 1 — the test file
parked to its three goldens and run against the unchanged gate at `f3e394dc`: **3 passed** (the first
attempt of the goal-block golden failed on one newline of my own transcription — the message section
ends with two newlines, not one — corrected to the observed bytes, which is the point of observing
first). Run 2 — the same goldens against the rewritten gate: **3 passed**. Diff of the bytes: none.
`NoLateBindingOnTheSendPathTests.TheCompiledViewAndTheSentTextAreTheSameBytes` (`Assert.Same`) stays
green because the gate sends the rendered view's own string after asserting it equals the projection's
render — reference identity, not a re-compile that happened to agree.

## The store — as the Data & Persistence lens is asked to accept it

- **Grain (DM8):** one row in `<workspace>/.aide/sessions/<id>/envelope-events.jsonl` is exactly one
  event on one envelope, identified by `(envelope_id, seq)`, recorded when the event occurs; an envelope
  is opened only by the Send gesture (the live pre-compile persists nothing — `PreCompile.Live`).
- **History rule per attribute:** every row immutable (Type-2 by construction; no update/delete/rewrite
  member — reflection); `Current(name)` = highest `seq` for the name; `source` is provenance, never
  precedence; `Confirmed` skips `derived` under `agentic-advisory`; `EffectiveMode` a function of the fold.
  The craft profile (ADR-0037's Type-2 dimension) is **read as `{family, version: null, sha: null}`** —
  `none` until the pack ships one — and not populated.
- **Derive, don't store (DM7):** no lease, shape, rule-computed tier, effective fan-out or count is ever a
  row (`DecorationNames.NeverOperator`; `[CE-0010]`; US-D1's fold assertion). Only what a party authored:
  the operator's text, lines, tier override and class choice; the machine's snapshots and refs.
- **The chain:** `prev_sha` = sha256 of the raw previous line, any schema; the first row's is `""`.
  Broken at N = the first line whose bytes no longer match the next line's claim; rows ≥ N never fold
  (an envelope with any row past N is absent, never partial); `Append` refused `[CE-0003]` for the life
  of the open; the bytes untouched; `aide session purge` is the named recovery. Refuse, not rotate.
- **Schema evolution:** expand-only; the walk reads `(envelope_id, seq)` from every line regardless of
  `schema` or `kind`; a `/1` writer after a `/2` row mints a unique higher `seq` and an unbroken chain;
  unknown schema / kind / torn lines are skipped and counted; a torn last line is newline-terminated
  before the next append. No backfill.
- **The migration (watcher):** `task_class_source TEXT NULL`, v7, no default, declared last; the
  tested rollback is a v6-shaped reader over the v7 file; `AFreshDatabaseAndAMigratedOneHaveTheSameSchema`
  green (both DDL sites carry the column).
- **Deletion:** `aide session purge <id>` deletes the one file after validating the id as one segment,
  refusing junctions/symlinks, printing name · id · workspace · file · count · newest; the Session
  aggregate's `Delete` acquires the envelope file exclusively (`DeleteOnClose`), refuses whole while a
  writer holds it, deletes it under the handle, then removes the siblings (observed: a held sibling
  makes the recursive delete fail with the envelope file already gone).

## The censuses — four-part statements (GO14a)

1. **`Projection.Project(`** — root `src/`; recursive, every `*.cs` skipping `bin/` and `obj/`, **code
   lines only** (a line whose first non-blank characters are `//` is prose); token `Projection.Project(`;
   allowlist exactly `AiDe.Core/Presentation/Composer/ComposerCompiler.cs`,
   `AiDe.App/Workbench/Composer/ComposerSendGate.cs`, `AiDe.App/Cli/CompileFold.cs`. Asserted by name.
   The Shell lane writes none → jointly satisfiable.
2. **`LeaseDerivation.Derive(` / `LeaseDerivation.Patterns(`** — the same root, recursion and code-lines
   rule; tokens both; allowlist by (file, argument): `Derive(` at
   `AiDe.Core/Compilation/Projection.cs` over `opened.SourceText` only; `Patterns(` at
   `AiDe.Core/Compilation/Projection.cs` (`opened.SourceText`), `AiDe.Core/Presentation/Composer/ComposerDraft.cs`
   (`this.SourceText`), `AiDe.App/Workbench/Composer/ComposerSurface.cs` (`_draft.SourceText`) — every
   argument ends in `.SourceText`. **Drift from the plan's Seams row, recorded:** the row named
   `ComposerSendGate.cs` and the display site (the pre-D-1 world); after D-1 the gate calls `Project()`
   and derives nothing itself (ADR-0033 rule 2, which the plan row itself quotes: *"`Send` via
   `Project()`"*). The Shell lane writes none → satisfiable.
3. **`RunBudget` member reads** — root `src/`; recursive as above; tokens `.Requests` / `.Tokens` in a
   file that names `RunBudget` or `BudgetCap`; allowlist seven files each with a named guard
   (`IsSubscriptionBounded`: `ComposerCompiler.cs`, `Projection.cs`, `PreCompile.cs`; the nullable
   `BudgetCap`: `SessionDocumentSurface.cs`, `NewSessionSheetViewModel.cs`; exempt: `GoalBlock.cs`
   (Validate), `ConductorEntry.cs` (the copy)). The ADR named four; S2 and CV-1 added three homes for the
   cap *setting's* display — each guarded, recorded.

## Prepare's states — reached vs not

| State / control | Reached | Evidence / reason |
|---|---|---|
| The tier as an editable derived line with provenance (E2) | yes | `ComposerSurface.TierControl` (rule · T0 · T1 · T2) beside the value text; the rationale *operator (rule said T2)* as provenance; an `operator` `tier` row at Send |
| The class as an editable line (Ruling 70) | yes | `ClassControl` — the session default first, then `TaskClassVocabulary.Offered`; *chosen for this prompt* / *session default* |
| The compiled disclosure rendering `Current` | yes | the disclosure = the sent bytes (`disclosed == request.Prompt`), override included |
| The degraded reason (locked / broken / missing) | yes | on the settings line: *· compile history: [CE-0002] another AI-DE has this session's compile history open (…)*; the settings popup's *Compile history* row |
| Ruling 75's one refusal | yes (CV-1's, unchanged) | `GoalBlockNeedsNotInScope` at the gate |
| Ruling 77's states (a Send while a turn runs) | yes (CV-1's, unchanged) | `SetRefusedGesture` |
| `draft · preparing · prepared · stale` as a state machine; the compile line's ten strings | **not reached** | under `mechanical-only` there is no call, so no `preparing`/`prepared` occurs and the compile line is absent by spec (§A10.2, E5); the only Prepare transition this rung has is the stale refusal (*your draft changed since it was prepared — press again to prepare it*), which is reached. The four-state STA test is CV-3's with the agentic rung. |
| *history purged* on the next open (US-D13) | **not reached** | no fact on disk distinguishes *purged* from *no history yet* without a marker file (a second store); the composer reads *recorded in <path>* with 0 envelopes. A finding for the Owner: a *purged* sentence needs a durable marker or the honest copy stays *no compile history yet*. |
| `structure_source: template` from a template draft | **not reached** | a template draft still sends as a Message (CV-1's rule); reading a template's fields as structure lines is a mapping `TemplateCompiler` does not expose — Remaining; the projection reads a `template`-sourced row correctly when one is in the fold (P-D1 ×3). |
| `history_window`, `constitution` decorations | **not written** | nothing reads them on the mechanical rung; the assembler takes them as inputs (CV-3 writes them when the prompt is assembled). |

## Seam requests

| To | Request | Why |
|---|---|---|
| Design lane (`src/AiDe.App/App.xaml.cs`) / the conductor | one dispatch branch in `App.OnStartup` beside the conductor's: `if (Cli.CliEntry.IsRequested(e?.Args ?? [])) { ShutdownMode = ShutdownMode.OnExplicitShutdown; _ = RunCliAsync(e!.Args); return; }` with `RunCliAsync` = `Shutdown(await Cli.CliEntry.RunAsync(args))` in a `finally` | `aide compile fold` / `aide session purge` are reachable headlessly (the tests) and not yet from the shell's process arguments; the file is not the Conversation lane's |
| the conductor (`AiDe.Core.csproj`) | `<EmbeddedResource Include="Compilation\Resources\*" />` | the host header and `compile-prompt/1` template are `simplify:` string constants in `CompileContract.cs` until the glob lands; `prompt_sha` is unchanged as long as the bytes are |
| Shell lane (`DESIGN.md`) | none needed — the two controls use existing tokens (`TextMutedBrush`, `TextBrush`); no new role | — |

## Findings (not this slice's scope; recorded)

1. **Namespace:** `AiDe.Core.Compilation` collides with Roslyn's `Compilation` type used unqualified in
   `AiDe.Core.Extraction` (seven `CS0118` in files outside the lane); the context is
   `AiDe.Core.PromptCompilation` in the `Compilation/` directory — for ADR-0033's text (register class CV-2 a).
2. **The watcher column is v7, not the plan's v6** (register class CV-2 d).
3. **The Seams row's lease-census allowlist** predates ADR-0033 rule 2 (above).
4. **`ComposerSendContext.TaskClass` is `string`** (was `string?`); the INV-0009 refusal test
   `ASendWithNoTaskClassIsRefusedByNameTests` is retired by Ruling 72 (its successor:
   `AReopenedSessionSendsWithTheSessionsDefaultClassAndSourceSessionDefault`); `MainWindow.xaml.cs` still
   passes `taskClass: null` to the binder, which substitutes the config's default — unchanged, Shell-owned.
5. **The class control offers the vocabulary**, so a class outside `TaskClassVocabulary.Offered` (e.g.
   `refactor`, used by CV-1's fixtures) cannot be chosen from the line — the E7 test found it; the
   vocabulary is provisional (its own remarks) and this is where §8.4 lands.
6. **`"watcher.db"` / `"loomkeeper-coord"` literals** now exist at a third site
   (`SessionDocumentSurface.StampTaskClassSource`, mirroring `GovernedRunHost` and `WorkbenchShell`) —
   a DM7 debt older than this slice; a `WatcherPaths` helper is the fix, in a Core file no lane owns.
7. **The store's walk runs on the UI thread at document open** (`simplify:` — `walk_ms` is emitted on
   `envelope-store.open`; the upgrade trigger is a measured walk over 50 ms).
