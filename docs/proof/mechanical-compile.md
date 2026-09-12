---
id: proof-mechanical-compile
title: "Proof Pack — CV-2, the mechanical compile: the append-only envelope store, the five event records, the fold and its projections, PreCompile, Projection.Project as the one producer of the sent bytes, Prepare's editable derived lines, purge, and the compile contract shipped inert"
type: proof-pack
status: accepted
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
  Evidence for CV-2 on the Conversation lane (Addendum D slice D-1 with D-0 folded): the
  append-only, exclusively written, sha-chained envelope store with its eraser beside it; the five
  event records and the fold's projections; PreCompile on the mechanical rung; Projection.Project as
  the one producer of the sent bytes (GovernedRunRequest byte-identical to three literal goldens
  observed on the pre-change gate); the fourteen tier inputs; the censuses (five source scans, code
  lines only, named by path and count); Prepare's tier and class controls with provenance; the
  task_class_source cohort column (v7) and its leaderboard reader; the compile contract shipped
  inert; three hard-veto reviews and the Simplifier cleared or overridden in writing; the three
  attended rows RUN-PENDING with their steps.
---

# Proof Pack: CV-2 — the mechanical compile

- **Tier:** T2 · **Fan-out cap:** 3 (persona reviews) · **Session:** `cv-2` · **Author:** `claude-cv-2`.

## E7 change-surface list (written before coding; ticked at close)

| Surface | Change | Writer | Compute reader | Status |
|---|---|---|---|---|
| store | `EnvelopeStore` (`compiled-envelope/1`; one file per session; `Append` + reader; `prev_sha` chain; `FileShare.None`; the append-failed latch) | `ComposerSendGate.Send` (opened · decorated · submitted), `SessionDocumentSurface` (consumed) | `EnvelopeStore.Read` / `ReadFile` → `Fold`; `aide compile fold` | ✔ |
| event records | `Opened · Decorated · Called · Submitted · Consumed` (append-only facts) | as above | `Envelope.Fold` | ✔ |
| fold / projections | `Envelope.Current`, `Confirmed`, `EffectiveMode`, `Outcome`; derive, never store | — | `Projection.Project`, Prepare, `aide compile fold` | ✔ |
| `PreCompile` | the mechanical rung: `ceilings` snapshot (writer named), `task_class` (`session-default` / `operator`), `family_profile` (none), `template_applied`, `attachments` (refs), the structure lines, the tier override | the Send gesture | `Projection.Project` | ✔ |
| `Projection.Project` | the ONE producer of the sent bytes; tier by §A9 + R4; cap; budget; lease from `opened.source_text`; `projection_sha` | — | `ComposerSendGate.Send`, `ComposerCompiler.Decorations`, `Cli/CompileFold.cs` | ✔ (three named sites) |
| wire | `GovernedRunRequest` byte-identical (fourteen parameters by name and type; C16 at two sites) | `ComposerSendGate.Send` | `GovernedRunHost` (unchanged) | ✔ |
| client | `ComposerSendGate.Send` via `Project()`; `ComposerSendContext.TaskClass` from `default_task_class` (non-null) | `SessionComposerBinder` | `ComposerSurface` | ✔ |
| UI | Prepare: the tier control and the class control on the decoration line (editable, with provenance), the compiled disclosure rendering `Current`, the degraded reason on the settings line, the purge button in the settings popup | `ComposerSurface`, `SessionDocumentSurface` | the operator; the E7 consistency test | ✔ (headless STA; the attended run pending) |
| compute reader | `task_class_source` (expand-only, v6→v7) read by `ScoredEpisode.TaskClassSource` and `LeaderboardCell.DefaultedClass` / `ChosenClass`; `aide compile fold`; the purge (document + CLI) | `SessionDocumentSurface.StampTaskClassSource` (the provenance captured at launch) / the CLI | `Leaderboard`, the eval, the operator | ✔ (the stamp proven headlessly over a temp watcher; the real scored run is the attended row) |
| tests + census + ledger | the reds below; the censuses (five source scans, code lines only); the terminal ledger (read at the join) | — | — | ✔ |

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
| **The E7 consistency test across surfaces** | no chain existed | for one prompt: the decoration shown (tier T1 · *operator (rule said T2)* · class *defect* · lease `src/A/** · src/B/**` · goal block), the disclosure, the bytes sent (the tier section T1, cap 2, the class, the lease), the row stored (`Project(fold)` equal on every field; `projection_sha` and `text_sha256` equal to the `submitted` row) — agree; the column's provenance (`operator`) is asserted at its source in that test (the refused run scores nothing) and its wiring — the provenance captured with the ordinal at launch, stamped on a scored cell through the run host's watcher composition — is proven headlessly over a temp `watcher.db` (`TheClassProvenanceCapturedAtLaunchIsStampedOnTheScoredCell`); the real scored run is the attended row; the refused run's `consumed{lane_exited, not recorded}` lands | `ForOnePromptTheDecorationTheRowTheBytesAndTheColumnAgree` |
| the inert contract (§A17 fixtures, authored) | tests written after the code; **reds observed by mutation**: the mention scan disabled → `AMentionBearingValueOrNotesIsRefusedNotStripped` red; the header dropped → `ThePromptBeginsWithTheHostHeader…` red | 23 green | `TheTypedBoundaryIsInertButRealTests` |
| P-D2's CI floor | — | `aide compile fold` over 6 envelopes the gate wrote: 6/6 rebuilt sha equal; a rewritten sha → 1/2, exit 3; a held file → `[CE-0002]`, exit 3 | `TheCompileVerbsFoldAndPurgeTests` |
| the reviews' conditions (Stage 4) | each a red observed on the pre-fix code: the store did not latch after a failed write; a derived row named `ceilings` was admitted; a nested `body` slipped the deny-list; `\n` passed the type check; a slot token in the source text was substituted; the sibling-held Session delete removed the envelope file first; the RunBudget precondition missed a `.Budget.` read; the sha theory had 5 of 12 members isolated; no `new Lease(` census; no `using static` census | the latch (`[CE-0013]` until reopen, a faulting stream through `OpenWith`); every non-mechanical setting-name refused; the four-scalar attachment allow-list; every control/format character refused; one-pass slots; probe → tombstone → delete; the precondition + the guard window; the 12-row theory; the two-site `new Lease(` census + three zero-hit patterns; the six-token negative census; the eraser beside the writer (`PurgeCompileHistory`) | `AnAppendThatThrowsAfterOpenLatchesTheStoreUntilItIsReopened`, `ANonMechanicalRowNamedASettingARefOrAProjectionIsRefused`, `AnAttachmentValueOutsideTheFourScalarsIsRefused`, `AControlOrFormatCharacterInAValueIsATypeFail`, `ASlotTokenInsideTheSourceTextIsNeverSubstituted`, `ASiblingHeldOpenRefusesTheDeleteWholeAndNothingIsOrphaned`, `EachMemberOfTheShaDomainMovesTheShaAlone`, `ALeaseIsMintedAtExactlyTwoNamedSitesAndByNoOtherPattern`, `NoFileImportsTheProjectionOrTheDerivationStaticallyOrTakesThemAsAMethodGroup`, `TheDocumentPurgesItsOwnCompileHistoryAndRecordsAgainAfterwards`, `TheClassProvenanceCapturedAtLaunchIsStampedOnTheScoredCell` |
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
- **The latch (the D&P lens's condition):** a write that throws after a good open (disk full mid-row)
  leaves the tail unknown, so the store refuses every later append `[CE-0013]` for the life of the
  open; a reopen walks the real tail (the partial row is a torn last line, counted; the next seq is
  unique; the chain continues over the torn bytes). Proven with a faulting stream through the one
  internal seam, `EnvelopeStore.OpenWith`.
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
- **Deletion:** the eraser ships beside the writer — `SessionDocumentSurface.PurgeCompileHistory`
  (the settings popup's button; the identity shown, a confirmation asked, the one file removed, the
  store reopened) and `aide session purge <id>` (headless) both go through `EnvelopePurge`, which
  validates the id as one segment before a path is formed, refuses a junctioned directory and a
  symlinked file, prints name · id · workspace · file · rows · count · newest, and re-validates the
  path at `Execute`. The Session aggregate's `Delete` **probes** the envelope file exclusively
  (refused whole by name while a writer holds it — nothing removed), **moves the directory to a
  tombstone** (a directory move fails while any file inside is open — the directory-level lock; a
  writer opening afterwards refuses `NoSessionDirectory`), deletes the envelope file in the
  tombstone, then the rest. The D&P lens found the first order (acquire with `DeleteOnClose`, release,
  recursive delete) left a window; the tombstone closes it.

## The censuses — four-part statements (GO14a)

1. **`Projection.Project(`** — root `src/`; recursive, every `*.cs` skipping `bin/` and `obj/`, **code
   lines only** (a line whose first non-blank characters are `//` is prose); token `Projection.Project(`;
   allowlist exactly `AiDe.Core/Presentation/Composer/ComposerCompiler.cs` (1 call: the decoration
   line's render), `AiDe.App/Workbench/Composer/ComposerSendGate.cs` (2 calls: `RenderView` and
   `Send` — the two ends of C15's window through one function, the Simplifier's condition),
   `AiDe.App/Cli/CompileFold.cs` (1). Asserted by name and count. The Shell lane writes none →
   jointly satisfiable.
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
| the conductor (`tools/verify-id-allocators.py`) | declare the `CE-` family — the compile step's stable refusal codes — beside `AP-` and `TS-`: `{"prefix": "CE", "path": "src/AiDe.Core/Compilation/EnvelopeStoreException.cs", "kind": "heading", "pattern": r'^\s*public const string \w+ = "(CE-\d+)";', "what": "compile-step refusal codes", "contiguous": True}` | `verify-id-allocators.py` reports the family as undeclared (15 ids); `tools/**` is not the lane's — the gate stays red-by-seam until the entry lands |
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

## Reviews (Stage 4 — Adversary Mode; the author never clears its own veto)

| Lens | Loop 1 | Loop 2 |
|---|---|---|
| Data & Persistence Architect (hard, the store and the migrations) | PASS-WITH-CONDITIONS — one Major (no latch after a failed write), four Minors (operator-scoped DM7 refusal; the schema-equality test never exercises `ALTER TABLE` — pre-existing since v4; the release→delete window in the cascade; the stamp read at completion), four nits | **PASS** — the latch cleared (its falsifying input is the test); DM7 refusal for every non-mechanical source cleared; the cascade cleared, *now Verified on Windows* (probe → tombstone → delete); the stamp race cleared (captured at launch); the `ALTER TABLE` finding accepted as recorded debt with its named control (compare `pragma_table_info` + indexes per table, seed from a hand-written v1 DDL; correct the three "same sqlite_master text" comments) — a finding for the conductor, the file being the watcher's; the idempotent stamp (`IS NULL OR = $source`, a different value refused and said) and the DDL label ("a copy of the envelope's provenance, rebuildable by join while the envelope exists") applied after the verdict. Residuals recorded: a tombstone left by a crash mid-delete is unswept (holds `source_text`; a `session list` sweep is Addendum A's); mechanical rows named like a projection are admitted (a mechanical allow-set is the next control); the ≥20-real-envelope rebuild and non-ASCII/attachment codec round-trips are the attended row. |
| Security & Identity Architect (hard, the new surface) | BLOCK — the writer shipped without a reachable eraser (the CLI dispatch a seam request); Majors: the `new Lease(` census absent, no symlinked-file fixture, the fold's report in `%TEMP%`; Minors: the attachment deny-list, derived rows named like settings, `\n` admitted, sequential slot substitution, `Execute` trusting the plan | **PASS-WITH-CONDITIONS** — the Blocker cleared (the eraser beside the writer, `PurgeCompileHistory` from the settings popup; the CLI dispatch may stay a seam request); all four Majors and six Minors cleared by line. Conditions: **C-1** one green run of the file-symlink test on a privileged host, recorded here with the host's privilege state — until then the spec's symlinked-file claim is **Inferred** (this host lacks `SeCreateSymbolicLinkPrivilege`; the test skips with that reason; the directory-junction refusal runs); **C-2** `Execute` re-checks the file's reparse bit (applied); **C-3** the confirmation runs outside the envelope lock (applied — the identity is read from the held store, the act alone takes the gate). Privacy co-review asked on the absolute `DisplayPath` of an outside-workspace attachment (spec-accepted by §A13.5 rule 2; recorded). |
| Test Architect (hard, the censuses and the byte-identical test) | PASS-WITH-CONDITIONS — Majors: the `RunBudget` precondition a leak, `using static` unseen, the sha domain 5/12 isolated, the broken-at before-N branch untested, the column's wiring untested; Minors: the goldens' commit named twice, two goldens not field-complete, derived rows under mechanical-only, the vacuous cwd half | **PASS-WITH-CONDITIONS** — every loop-1 condition cleared by line; the one condition (the tested binaries predated five committed sources) is met by the close's gate run: both suites rebuilt and run through `verify-test-run.py` (App 820, Core 2495 + 1 skipped). Residuals recorded: `Projection.Project  (` with two spaces is outside the six tokens; the attended rows. |
| The Simplifier (the record / projection split) | soft BLOCK — two Majors: **the sent bytes had two producers** (`RenderView` compiled through the draft's own block while `Project` rendered again, and `Send` checked the two agreed) and **the Presentation↔Compilation cycle** (`ComposerCompiler.Decorations` calls `Project()` against ADR-0033 rule 1's "referenced by nothing in Core"); a delete-list of `net: -200 lines possible` | **the first Major fixed**: `RenderView` now IS `Projection.Project(PreCompile.Live(input), draft, template).Compiled` — one producer at both ends of C15's window (the census counts the gate's two calls); `ToGoalBlock()`'s tier stays for the form engine's field-level validation — the same rule function over the same inputs, and the E7 test asserts the block equals the projection's. **The second Major overridden in writing:** the plan's Seams row names `Presentation/Composer/ComposerCompiler.cs` as a `Projection.Project(` call site (the guard this slice must state as the plan states it), and ADR-0033 rule 1 itself places the render in Presentation — rule 1's "referenced by nothing in Core" is superseded by rule 2's named render site; recorded as a finding for the ADR. **Applied from the delete-list (−96 lines):** `ToRunEvent`, `CompileEventKinds.Stages`/`ModeChanged`, `Stage()`, `Envelope.LastSeq`, `EnvelopeEventKinds.All`, `DenyList`, `TierPrompt`, `Bytes`, the stale `simplify:` marker on `Tier`, `SubmittedEnvelope`'s five unread members, `Persist`+`Record` → one `TryAppend` folding the stamped rows (no whole-store re-read), `EnvelopeStore.Tiers` → `IsTier`, the in-memory store's second dictionary, `PurgePlan.FileExists`, the per-call `JsonSerializerOptions`, the codec's `"not recorded"` literal, `DocumentClosed` → `document_closed`, `DecorationSources` aliased to `TaskClasses.Sources`, the `simplify:` marker on the contract now stating its ceiling, the codec's rationale corrected (the lenient reader, not the writer's order). **Declined with rationale:** `Opened.Constants` (the spec pins the constants on `opened` so an abandoned envelope — no `submitted`, no `projector_version` — still reads them); `_renderingControls` (a programmatic `SelectedItem` set raises `SelectionChanged`; the guard is the re-entry stop, nine lines); `CliEntry.RunAsync` as a `Task` (the shape `App.OnStartup`'s dispatch awaits, as the conductor's); the store constructor's field copies and the two index switches (cosmetic; the Walk is a private record of one open); `CompiledProjection.Shape` as a string (the decoration row's value is a string; one spelling). The inert contract's ≈395 lines stay with the rationale on record. |

## Attended rows — RUN-PENDING (the operator's next manual test; each with its steps and its consequence on red)

| # | Row | Steps | Green when | Red means |
|---|---|---|---|---|
| A-1 | **Send a prompt and diff the compiled disclosure against the sent bytes** (C15 + the envelope as its third witness; US-D1) | 1. Open a real session (New Session, a workspace with a provider file). 2. In the goal-block form type a message with two mentions (`touch @src/A/ and @src/B/ …`), fill Goal · Done when · Not in scope. 3. On the decoration line set the **tier control** to `T1` and the **class control** to `defect`. 4. Expand **Compiled prompt** and copy its text. 5. Press Send. 6. Open `<workspace>/.aide/sessions/<id>/envelope-events.jsonl`; read the last `submitted` row's `text_sha256`. 7. `sha256` the copied disclosure (PowerShell: `(Get-FileHash -InputStream ([IO.MemoryStream]::new([Text.Encoding]::UTF8.GetBytes($text))) -Algorithm SHA256).Hash.ToLower()`). | the two hashes are equal; the `tier` row in the file reads `T1` with `source: operator`; the `task_class` row reads `defect` / `operator`; the lane's console shows the same `## tier` / `## fan_out_cap 2` block | a byte differs between what was read and what was sent (C15 broken); or the row's provenance differs from the control (E7 broken) |
| A-2 | **Purge and confirm** (US-D13) | 1. With the session open, **Session settings → Purge compile history…**; read the confirmation (name · id · workspace · file · rows · envelopes · newest) — decline once and confirm the file is still there. 2. Confirm; read the announcement *compile history purged — N envelope(s) removed*. 3. Confirm `envelope-events.jsonl` is gone and `session.json` / `session-events.jsonl` are not. 4. Send one more prompt; confirm the file exists again with one envelope. 5. Headless: `AiDe.App.exe session purge <id> --workspace <root> --yes` — **needs the App.OnStartup dispatch (seam request)**; until it lands, run `dotnet test … --filter TheCompileVerbsFoldAndPurgeTests` as the verb's evidence. | steps 1–4 as described; the popup's *Compile history* row reads *history purged; recording again* | the confirmation names a count alone (DC-120); a sibling file is gone; the document cannot record after the purge |
| A-3 | **`aide compile fold` from ≥ 20 real rows** (P-D2) | 1. Send ≥ 20 prompts across a session (mix Message and goal block, an override, a class choice). 2. Close the document (the writer releases the file). 3. `AiDe.App.exe compile fold --workspace <root> --session <id>` (or, until the dispatch lands, the test `CompileFoldRecomputesEveryProjectionShaFromTheRowsAndTheyAllMatch` over the real file copied into its fixture). 4. Read `<session dir>/compile-fold.json`: `rebuild.matched == rebuild.compared == 20+`. | 100 % matched, exit 0 | a mismatch (exit 3) — a `projection_sha` domain member the product writes and the rebuild does not read |

## Gate record (bare, stop on the first red)

| Gate | Result |
|---|---|
| `dotnet build` Core + App + both test projects `-p:TreatWarningsAsErrors=true` | 0 errors, 0 warnings, each |
| `python tools/verify-test-run.py` (runs both suites with trx into `artifacts/test-results/`; verifies) | OK — App 820 executed (floor 798), Core 2495 executed (floor 2361) + 1 skipped with its reason (the file-symlink fact; no privilege on this host); outcome Completed for both |
| `python tools/run-verify-gates.py` (35 gates) | 34 green after regeneration; **1 red-by-seam: `verify-id-allocators.py`** — the `CE-` family (15 compile-step refusal codes) needs its FAMILIES entry in `tools/verify-id-allocators.py`, a file outside the lane (the entry is in the seam requests above); every other gate green incl. `verify-r14b2-session-naming.py` (after `SessionPurge` → `PurgeHistoryVerb`), `verify-no-new-console-launches.py`, `verify-terminal-host-exit-paths.py`, `verify-ui-craft-floor.py` (the page untouched) |
| `regenerate-derived.py` after the audit entry | (see the audit entry's line) |
| Release build `ProductVersion` | (see the report) |
