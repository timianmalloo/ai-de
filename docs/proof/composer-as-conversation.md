---
id: proof-composer-as-conversation
title: "Proof Pack — CV-1, the composer as a conversation: the thread feed per DS-1 (SC8 keys, SC9 announcements, SC10 UIA), the folded Console per turn, the decoration line, and tier / fan-out / budget off the per-prompt form"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, conversation-lane, cv-1, ds-1, session-thread, composer, ruling-56, ruling-63, ruling-72, ruling-74, ruling-77, ruling-78, sc1-sc10, accessibility, virtualization]
links:
  - { to: design-session-thread-itemscontrol, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: proof-read-only-turn, rel: refines }
  - { to: mockup-session-conversation, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: adr-0034-envelope-event-store, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Evidence for CV-1 on the Conversation lane: the session document is a feed of turns above one
  pinned composer (DS-1's FeedList / ThreadFeed over a recycling VirtualizingStackPanel), the
  editor's top edge is equal at 1 / 5 / 40 turns with the editor never below its 130 px floor and
  the thread never starved (both mechanisms measured: the floor on the host, the belt on the
  document), the SC8 keyboard model is a pure decision plus an act with the six owned keys, the SC9
  policy announces each transition once and never a line, the SC10 properties are real over the UIA
  peers, the folded Console per turn and the on-demand split are one list (Ruling 74 condition 1),
  tier / fan-out / budget left the per-prompt form (the tier is §A9's projection on the compiler,
  the cap and budget the session's), the eight named reds and DS-1's headless oracles went red →
  green, and the attended rows (P-11/12/13, A6) are RUN-PENDING with their steps.
---

# Proof Pack: CV-1 — the composer as a conversation

- **Change:** `lane/conversation-cv1` from `main` `11ab0e74`, merged with `main` `b0e092b5` (SH-1,
  INV-0010) mid-slice at the conductor's instruction (no conflicts; `regenerate-derived.py` green).
  **Core:** `Presentation/Sessions/SessionThread.cs` (`ISessionThread`, `ThreadSnapshot`, `TurnView`
  with its two invariants, `DecorationRow`, `OutcomeView`, `Spend`, `EventLine`, `WaitingRequest`,
  `TurnActionKind`, `TurnCopy`), `ThreadAnnouncementPolicy.cs` (`Urgency`, `AnnouncementKind`,
  `Announcement`, the transition function), `RunChannelSessionThread.cs` (CV-1's implementer over
  today's run channel); `Presentation/Composer/ComposerCompiler.cs` (`Tier` — Addendum D §A9 R0–R3,
  `CapOf`, `EffectiveFanOut`, `SettingsLine`, `Decorations`, `LeaseNoneYet`, the `## message`
  section, `TierProjection`), `ComposerDraft.cs` (one editor: `SourceText` is the message in both
  forms; `PerPromptGoalFields` by subtraction; `SetGoalValue` refuses tier / fan_out_cap / budget;
  `Ceilings` from `SessionConfig`; `ToGoalBlock` supplies the three; `ParseBudget` retired),
  `ComposerMessageRouter.cs` (`focus.leave` carries an optional `direction`), `Sessions/BuiltIn/
  goal-block.template.md` (three fields), `Presentation/Sessions/ConsoleStreamModel.cs` (`TextOf`
  made public — one definition of a line's text, two readers; an unowned file, visibility only).
  **App:** `Workbench/Sessions/FeedList.cs`, `ThreadFeed.cs`, `TurnItem.cs`, `FeedKeyDecision.cs`,
  `ThreadText.cs`, `ThreadDiagnostics.cs` (new), `ConsoleSurface.cs` (the split — a `FeedList` over
  heading + line rows), `SessionDocumentSurface.cs` (rewritten: header · outside text · thread ·
  stopped row · composer · the split; `CycleRegion`; the run's sink feeds the fold; Ruling 77 in
  flight; the belt), `CanvasModeCatalog.cs` (one row's lambda, unowned — the E7 retire finding),
  `Workbench/WorkbenchAnnouncer.cs` (`Announce(Announcement)`, `NotificationMapping`, the raise
  seam), `Workbench/Composer/ComposerSurface.cs` (the structure lines, the decoration line, the
  settings line, the compiled disclosure, the send row, `BeginNextTurn`, `UseAsNextDraft`,
  `SetInFlight`, `FocusTarget`, `PageReady`, `EditorFocusTarget`, `StructureLine`,
  `StructureDeriver.Fake`, the DC-137 rule under the new shape), `ComposerSendGate.cs`
  (`NextBlock`, `BlocksSent`; `HasContent` retired), `ComposerPageTheme.cs` (`--inferred`,
  `--verified`), `Web/composer.html` + `composer.mjs` (one editor, the placeholder and description
  from `host.init`, Shift+Tab off the first stop → `focus.leave` backward, focus into the editor on
  window focus). **Tests:** the reds below; DS-1's oracles under
  `tests/AiDe.App.Tests/Sessions/Thread/`; the retired paired-zone / mode-strip / merged-stream
  tests deleted (six files) and three re-scoped; the probes' six-field expectations re-scoped.
- **Spec / design statements this satisfies:** `DESIGN.md` SC1–SC10 (`:1114-1125`), the thread
  contract (`:1079-1092`), the turn regions (`:1103-1112`); DS-1 `docs/design/
  session-thread-itemscontrol.md` §Contracts, §Patterns P1–P9, §SC8, §SC9, §SC10, §Seams, §Test
  plan; Rulings 56, 63, 64, 72, 73, 74, 75, 77, 78; Addendum D §A9 (the mechanical tier rule) and
  §A11 (the marks: *— fill in* · *edited* · *invalid*).
- **Tier:** T2 · **Fan-out cap:** 3 (persona reviews) · **Session:** `cv-1` · **Author:** `claude-cv-1`.

## E7 change-surface list (written before coding; ticked at close)

| Surface | Change | Writer | Compute reader | Node |
|---|---|---|---|---|
| store | none — today's run channel (`RunEventRelay` → the document's sink) and the accepted send; the envelope store is CV-2's | — | — | CV-2 |
| model | `RunChannelSessionThread` folds Accept / Append / Conclude into `TurnView`s; versioned, caught-up snapshots; one raise per applied event | `SessionDocumentSurface.Launch` / `RunOneAsync`'s `Sink` | `ThreadFeed.Apply`, `SessionDocumentSurface.RenderHeader`, `ConsoleSurface.Show`, the policy | CV-1 ✔ |
| service | `ThreadAnnouncementPolicy.Next` (T0) | `ThreadFeed.Apply` | `IWorkbenchAnnouncer.Announce(Announcement)` | CV-1 ✔ |
| projection / wire | in-process; `focus.leave.direction` on the page→host envelope; `placeholder` / `editorHelp` on `host.init` | `composer.mjs` / `ComposerSurface.PushInit` | `ComposerMessageRouter.IsBackward` / the page's `aria-placeholder`, `aria-describedby` | CV-1 ✔ |
| client type | `ThreadSnapshot`, `TurnView`, `DecorationRow`, `OutcomeView`, `Spend`, `EventLine`, `WaitingRequest`, `Announcement`, `Urgency`, `AnnouncementKind` (Core); `TurnItem`, `FeedKeyDecision`, `NotificationMapping`, `TierProjection`, `Ceilings` | as named | `TurnCopy`, the templates | CV-1 ✔ |
| UI | `FeedList`, `ThreadFeed` + the turn template, the split, the jump list, the header's count + spend, the outside text, the stopped row, the document's rows (`Auto · Auto · *` with `* · Auto · Auto` inside) and `CycleRegion`, the composer's lines | as named | L1–L5, K1–K10, U1–U5 | CV-1 ✔ |
| UI — retire | the paired zone, the canvas-mode hosting, `ConsoleSurface`-as-merged-stream, the mode strip — **deleted**; `CanvasModeCatalog` and `SessionDocumentViewModel`'s mode members **kept** because `WorkbenchShell.cs:3023` (Shell lane) constructs the model with the catalog's ids — a finding for the conductor (below) | — | — | CV-1 + conductor |
| compute reader | the policy (transitions); this pack (`thread.layout`); `TurnCopy.SessionSpend` (the header); `ConsoleSurface.Derive` (the identity) | — | — | CV-1 ✔ |
| seams into other nodes | `IWorkbenchAnnouncer.Announce(Announcement)` + `LastRaise` (additive, this slice); the document takes an optional announcer (the shell's is a seam request); F6 as the document's key handler (the registry rows are a seam request); `--border-strong` requested | — | — | below |

## The reds → green

Every row names what was observed red, when, and what made it green. "Old test vs new code" is
the observation for a re-scoped test: the pre-existing assertion was run against the new
implementation and failed, proving it discriminates the two shapes; "mutation" is a deliberate
break of the implementation with the new test in place.

| # | Claim | Test (file · name) | Red observed | Green | Confidence | Residual |
|---|---|---|---|---|---|---|
| 1 | The per-prompt form is the three content lines; a required gap blocks by name on screen with Ruling 75's sentence; the send with nothing typed for tier / fan-out / budget goes through | `Composer/TheComposerRendersItsFieldLevelErrorsTests.ARequiredFieldGapBlocksSendWithAFieldLevelErrorThatIsOnTheScreen` | old test vs new code (first App run: six field names expected, four contract errors; `Expected "✓ edited" Actual "— fill in"` after the first re-scope — the draft → line sync was missing) | 3 marks, 1 contract error (`not_in_scope`), then `T1 · cap 2 · SubscriptionBounded` on the request | Verified | — |
| 2 | The form and the contract name one field set; the contract never names tier / fan_out_cap / budget | `Core.Tests/Composer/TheComposerIsOneValidationMechanismTests.Ruling26b_TheFormEngineAndTheSpawnContractNameOneFieldSetForEveryInput` (+ `Rulings56_63_72_ASessionSuppliedFieldIsRefusedOnTheDraft` ×3, `…AGoalBlockWithNothingTypedForTierCapOrBudgetIsCompleteFromTheSession`) | old test vs new code: `SetGoalValue("tier")` threw `not a per-prompt field` | 8 masks equal; T1 / 2 / subscription-bounded | Verified | — |
| 3 | The goal-block template's fields are the per-prompt set | `Core.Tests/Sessions/GoalBlockTemplateTests.TheTemplatesFieldsAreTheSameSetAsGoalBlockFields` (+ `…InTheOrder…`, `TheTemplateDeclaresNoTierFanOutCapOrBudgetField`, `Compiling…PerPromptWireNames…`) and `BuiltInCatalogIsTranscribedTests.CoreFieldsAreTheMechanicalSlugOfB4sColumn(goal-block)` | old tests vs new template: 5 failed (`Failed: 5, Passed: 91` in the run after the template edit) | deviation iii recorded in the transcription test | Verified | Addendum B §B4 row `:135` still lists six — a finding for the spec's owner |
| 4 | The compiled block carries the session's values and the message; the render is deterministic | `TheComposerIsOneValidationMechanismTests.TheCompileIsDeterministicAcrossRepeatedRuns`, `Ruling34_ARoundTripLosesNoFieldAndCallsNoAssist` | old tests vs new code (threw on tier) | the seven sections in order; `## tier T1`; `SubscriptionBoundedDisplay`; `## message` last | Verified | — |
| 5 | The settings line and the decoration line carry no numeral for an absent cap (Ruling 72) | `Core.Tests/Composer/TheTierIsTheMechanicalRulesProjectionTests.TheSettingsLineReadsBoundedByYourSubscriptionWithNoNumeral`; `TheComposerRendersItsFieldLevelErrorsTests.TheSettingsLineRendersBoundedByYourSubscriptionWithNoNumeral` (rendered) | new tests: red by construction (no `SettingsLine`, no `Decorations` existed) | `int.MaxValue` / `long.MaxValue` absent from the rendered text; *T0 — the ceiling of 2 does not apply to this turn · budget: bounded by your subscription · from session settings* | Verified | — |
| 6 | §A9 is total over P and L; the cap function; the effective fan-out | `TheTierIsTheMechanicalRulesProjectionTests.TheRuleIsTotalOverPAndL` ×6, `TheCapFunction…`, `TheDecorationRowsCarryOneGrammarForEveryTurn`, `ACompiledGoalBlockCarriesTheSessionsValuesAndTheMessage`, `AGoalBlockFormWithABlankGoalCompilesAsTheMessageAlone` | new | R0–R3; min(cap, ceiling) | Verified | R4 (the operator override) and P-D1's fourteen inputs are CV-2's on `Projection.Project` |
| 7 | The `"free-form"` literal census: root `src/`, recursive, token `"free-form"`, allowlist `Watcher/Leaderboard.cs` | `grep -rn '"free-form"' src/ --include=*.cs` at close | zero today; the composer reads `TaskClasses.FreeForm` / `config.DefaultTaskClass` | one hit: `Leaderboard.cs:91` (the constant's home) | Verified | the register's guard row |
| 8 | **A1** — the policy emits each transition once, nothing before catch-up, nothing for history, both legs of running → waiting → running, two outcomes in one snapshot as two, the version gap reported, the D2 subsequence property over four seeds | `Core.Tests/Sessions/Thread/TheThreadAnnouncesByTransitionTests` (10 facts + a 4-row theory) | new; the catch-up rule's first line is the falsifier the forty-replays row names | 14 green | Verified | — |
| 9 | **M4** — one raise per applied event after catch-up; the k-th snapshot is the fold; not caught up before `CatchUp`; a pre-loaded fixture is caught up at 0; a terminal state never moves; the two invariants | `Core.Tests/Sessions/Thread/TheReadModelPublishesOneSnapshotPerAppliedEventTests` (7) | new | 12 raises for 12 events, versions 1..12 | Verified | **D7 pairing open until CV-2's R1** (the envelope-backed twin) |
| 10 | **M2** — absent usage reads *not recorded*, never 0; the header's spend is the sum and names the unmeasured | `…AbsentUsageReadsNotRecordedNeverZero_AndTheHeaderSpendIsTheSum`, `TheNameIsTheOrdinalAndTheFirst120Characters…` | new | *edits not recorded · tokens not recorded · 4 events*; *12,400 tokens this session (1 not recorded) · bounded by your subscription* | Verified | — |
| 11 | **L1** — the editor's top edge equal at 1 / 5 / 40 turns at 1440 and 1024; the editor ≥ 130 px with the 30-line compiled prompt open; the thread ≥ 3 half-turns; the composer within the belt | `App.Tests/Sessions/Thread/TheThreadIsChatLikeTests.TheEditorsTopEdgeIsEqualAt1_5_40Turns_AndNeitherRegionStarves` (1440, 1024) | **mutation** (`_view.MinHeight = EditorFloor` removed): *"1 turns at 1440: the editor host is 0.0 px; the floor is 130"* — Q14's number reproduced | tops equal at 1 / 5 / 40 (tolerance 0.5 px) | Verified | the numbers are this machine's at 1440×832 / 1024×832 (the shell's title + tab strips subtracted); P-12's attended row measures the maximized document |
| 12 | **L2** — 40 turns realize fewer containers than turns; 400 − 40 ≤ 2; p95 layout ratio < 5; the record equals the tree | `…FortyTurns_RealizeFewerContainersThanTurns_TheDeltaTo400IsBounded_AndTheRecordEqualsTheTree` | **mutation** (`IsVirtualizing = false`): *"40 turns realized 40 containers — virtualization is off"* | green in isolation; **one red in the full suite while the verify gates ran concurrently on the same machine** (the p95 ratio) — re-run alone green; a ratio under load is DC-107's class and the row is reported, not asserted absolutely | Verified (count rows) · Inferred (the ratio under load) | the ratio row should run on a quiet machine or move to the Proof Pack's measured section — a finding |
| 13 | **L3** — the follow rule: pinned → the last container in view; mid-thread → the offset never moves | `…AnAppendWhilePinned_BringsTheLastTurnIntoView_AndMidThread_NeverMoves` | new | the last container's bottom ≤ viewport + 1; offset equal to 0.5 px | Verified | — |
| 14 | **L4** — the running turn shows its last four lines; the fold is bounded; the split virtualizes 10,000 rows | `…ARunningTurnShowsItsLastFourLines_TheFoldIsBounded_AndTheSplitVirtualizes` | new | 4 rendered lines of 140; *the other 136, in the Console*; 10,001 rows, < 200 realized | Verified | — |
| 15 | **L5** — under the app dictionary: the feed's ground and ink are the theme's; ≤ 3 controls on a completed turn, no box; the container's 2 px `{colors.focus}` ring; one ring on the fold header, none on the container; recycling carries no fold state (Q13); the 96ch measure equals `FormattedText("0") × 96`; no `Effect` / `Opacity` the thread authors | `…AtRest_TheThreadRendersUnderTheAppTheme_TheRingsAreDrawn_AndRecyclingCarriesNoState` | red: `container.Focus()` returned false under the feed's first entry rule (DC-162) | green after the rule moved to the document | Verified | the airspace census is scoped to the thread's tree; the theme's disabled-button opacity (0.56) is the template's, not the thread's |
| 16 | **K1a** — `Decide` over the whole `Key × ModifierKeys × bool` domain; the six owned keys; Ctrl+PageUp/PageDown `None`; an owner keeps its keys | `TheThreadIsAFeedOfTurnsTests.Decide_ByKeyModifierAndSource_NeverThrows`, `SourceOwnsItsKeys_…` | red: the first `SourceOwnsItsKeys` returned true for a scroller outside the list | the walk must reach the list | Verified | — |
| 17 | **K1b** — PageDown selects AND focuses b2's container; PageUp from b1 stays; End → b40 (the platform's 38, Q5c); PageDown from a fold header is the next turn, never +17; Down from a header scrolls 3 lines | `…FeedKeys_MoveTheCaretAndFocus_OrScroll_ByIndexNeverByGeometry` | new (`HeaderSite` looked up by `FrameworkElement.Name` was empty — DC-161's reading side; `Template.FindName`) | `Δoffset = 3 × 19.5` | Verified | — |
| 18 | **K5** — Tab from the caret's turn walks exactly *Provenance of b1 · Compiled prompt of b1 · 3 events* then leaves to the composer; with provenance and the compiled prompt open, *Use as the next draft* and the named scroller are stops; Shift+Tab lands on the caret's last stop | `…Tab_FromTheCaretsTurn_ReachesExactlyItsStops_ThenLeavesToTheComposer` | red: the unrealized-caret row landed on b3x (the platform's `Once` entry picks a realized container) | the document routes Tab off the header's last stop to `FocusCurrentItem()` (in K6's test: 41 turns, the caret on b1, the viewport at the end → b1's container) | Verified | `Once` walking the inner stops is measured on the real template (Q5e re-measured here) |
| 19 | **K6** — F6 cycles header → thread → composer → (split) → header; empty lands on the outside text; not caught up reads *Restoring …*; a refused region announces *Couldn't reach the Console.* with `THR-0002` | `…F6_CyclesHeaderThreadComposer_TheSplitOnlyWhenOpen` | red: the composer region read null — `TryFocus` on a page-less HWND took Win32 focus and WPF reported nothing (the `EditorFocusTarget` gate on `PageIsReady` is the fix) | 4-region cycle both ways | Verified | the registry rows (`session.cycleRegion` / `…Back`) are a seam request; DC-068's three places are the Shell lane's |
| 20 | **K8** — Escape closes the disclosure holding focus via its header; two open → only the focused one; from the container nothing; never leaves | `…Escape_ClosesTheDisclosureHoldingFocus_ViaItsHeader_AndNeverLeavesTheDocument` | new | focus on *Compiled prompt of b2* after Escape from its scroller | Verified | — |
| 21 | **K9** — the jump list is a view of `Turns`; type-ahead `b17` selects b17; Enter focuses the CONTAINER; *no turns yet* at 0 | `…TheJumpList_TypesAheadOnTheDisplayOrdinal_EnterFocusesTheContainer` | new | `SelectedIndex 16`; the container's `DataContext` is `Rows[16]` | Verified | the popup takes keyboard focus in the off-screen window (P9's fallback not needed) |
| 22 | **K10** — the tail opens the split at the turn's heading, focused; *Console open, at b2.*; PageDown moves by row | `…TheTailButton_OpensTheSplitAtTheTurnsHeading_AndTheSplitIsAFlatListOfRows` | new | heading `ListItem`, `Ordinal 2` | Verified | — |
| 23 | **A2** — the mapping: Status → All, Assertive → ImportantMostRecent; the announcer raises what it maps under `aide.session.thread` | `TheThreadAnnouncesAndExposesRealPropertiesTests.NotificationMapping_QueuesStatuses_InterruptsErrors_AndTheAnnouncerRaisesWhatItMaps` | new | `LastRaise == (ActionAborted, ImportantMostRecent, "aide.session.thread")` | Verified | audibility is A6's |
| 24 | **A3** — 200 lines, a fold toggle, a PageDown: nothing; the outcome after: exactly one | `…EventLines_TheReply_FoldsAndFocusMoves_AreNeverAnnounced_ButTheOutcomeIs` | new | 0 then 1 | Verified | — |
| 25 | **A4** — no `IsDefault`; focus stays on the turn when its button disappears; *Send again* leaves to the editor | `…AnActionKeepsFocusOnTheTurn_NeverIsDefault_AndSendAgainLeavesToTheEditor` | **red: the feed STOPPED on the running → stopped transition** — `THR-0001 'Spin' name cannot be found in the name scope of 'System.Windows.Style'` (DC-161) | `RegisterName("Spin")` on the style's name scope | Verified | — |
| 26 | **U1 · U2 · U5** — a `List` of `ListItem`s through the list peer's children with real positions (35–40 of 40 at the end); names ≤ 120 + …; `ItemStatus` = the decoration line; `HelpText` = the reason sentence on b17; the words / outcome / reply in the Control view; every disclosure named for its turn; every focusable named; ≥ 24 px | `…TheFeedIsAList_EachTurnAListItem_PositionsAreReal_AndItsTextIsInTheControlView` | new (`ResetChildrenCache` needed before the second read) | 6 realized peers = 6 containers | Verified in-process | the out-of-process bridge is P-13's / A6's |
| 27 | **M1** (Ruling 74 condition 1) — the split's rows equal the fold's events in order, grouped by turn | `…TheSplit_TheJumpList_AndTheHeader_AreViewsOfTurns` | new | an identity over `Turns` | Verified | — |
| 28 | **C1** — 500 snapshots off-thread → exactly 500 announcements in order; the render coalesces (< 500 applies); the caret and a focused fold header survive; `Dispose` before the pump applies nothing | `…SnapshotsRaisedOffThread_AreAppliedInVersionOrder_TheRenderCoalesces_ThePolicyDoesNot_AndTheCaretSurvives` | new | 500 / 500 | Verified | — |
| 29 | **C2** — a throwing apply logs `THR-0001`, shows the row outside the scroller, announces once (assertive), a second subscriber still hears the read model | `…AThrowingApply_LogsTHR0001_ShowsTheRowOutsideTheScroller_AnnouncesOnce_AndTheChannelSurvives` | new | — | Verified | — |
| 30 | **S1** — a reply with markup renders as characters; no `Hyperlink`, no invokable | `…AReplyWithMarkup_RendersAsCharacters_NoHyperlink` | new | — | Verified | — |
| 31 | **T1** — `thread.*` records exist and carry no text; *Deny* yields one `thread.action` with its request id | `…ThreadRecordsExist_CarryNoText_AndAStopIsOneActionRecord` | new | — | Verified | — |
| 32 | **Z1** — the Core types live in Core with no WPF reference; `ThreadFeed(ISessionThread, IWorkbenchAnnouncer, string)`; no field references `ConsoleStreamModel` or `AiDe.Core.Compilation` | `…CoreTypesLiveInCore_AndTheFeedReferencesNoStore` | new | — | Verified | method bodies are not inspected |
| 33 | **E11** — a send through the real composition root puts b1 in the thread and its refused run's outcome on the row | `Conductor/ASendLaunchesAGovernedRunTests.PressingSendOnASessionDocumentComposesAGovernedRun` | re-scoped (`SendCount` is per block: 0 after accept; `BlocksSent` 1) | `TurnState.Failed`, *failed*, *1 turn* | Verified | — |
| 34 | Retain-never-rebuild across a tab switch, with a real lane | `Sessions/ATabSwitchRetainsTheThreadAndItsLaneTests` (re-scoped from the mode switch) | the falsifier row stays red by construction | — | Verified | — |
| 35 | The census stays 0 below floor with the new surfaces walked | `ShellContrastCensusTests.EveryTextPairingInTheComposedShellClearsItsFloor` | red: the census's non-vacuity anchors named the old surface (*Compiled view*, *Lease:*) | 169 pairings · **0 below floor**; anchors now *Goal · This turn · bounded by your subscription · Compiled prompt · Nothing has run yet.* | Verified | the turn template is not walked in the census session (no runs); P-11's attended row |
| 36 | The writer keeps its room under the new shape: editor ≥ 130, compiled ≤ 200, editor ≥ compiled, one `composer.layout` line | `Composer/TheWriterKeepsItsRoomTests` (re-scoped from the 35 % share) | red: *expected one composer.layout line, read 0* (the measure toggled the disclosure during Measure) | the chrome is measured part by part | Verified | at 485 px with a four-line refusal the compiled prompt yields to the floor (6 px) — the honest outcome |

**Suite counts (from the run record, DC-160):** App `Failed: 0, Passed: <APP>, Total: <APP>`;
Core `Failed: 0, Passed: <CORE>, Total: <CORE>` — see the closing section; the floors in
`tools/expected-test-counts.json` are the conductor's `verify-test-run.py --update` at the join.

## The states of the mockup reached

| State (`session-conversation.html`) | Reached | How / why not |
|---|---|---|
| `empty` | ✔ | the outside text *Nothing has run yet. Write the first message below…*, the editor the first action (K7), *no turns yet*, the decoration line *class free-form · session default · ~ T0 no goal block · lease none yet — mention… · message*, *Send* |
| `first` (b1 running) | ✔ | a send accepts b1; its last four lines live, *Stop this turn*, *b1 is running; the next turn waits for it.* on a second gesture (Ruling 77) |
| `prepared` / a completed turn | ✔ (shape) | words · decoration line · provenance ▸ · compiled prompt ▸ · hh:mm · outcome line · reply · *N events* folded; the *Prepared* compile line is CV-2's |
| `draft-mech` | ✔ | mechanical-only: the three lines *— fill in*, tier ~T0, shape message, one gesture |
| `gaps` | ✔ | *This prompt is a goal block and needs Not in scope.* on the line, marked *! invalid* |
| `laneerror`, `stopped` | ✔ (run-channel) | a refused / cancelled run concludes Failed / Stopped; the boxed reason and *Send again as a new turn · Open the log* on the last turn; a failed PAST turn folds (b17 in the fixture) |
| `split` | ✔ | the Console beside the thread, *following b<n>* / *at b<n>*, opened from the header or a fold's tail |
| `jump` | ✔ | 40 rows, type-ahead, Enter |
| `overflow` (40 turns) | ✔ | L1/L2 |
| `loading` | partial | the outside text reads *Restoring <name>…* while the read model is not caught up (K6); the skeleton turns are not drawn (WPF: an overlay, DS-1 §Flagged) — CV-2's restore |
| `editorerror` | ✘ | the composer's status line carries *the composer editor could not start: …* (unchanged); the mockup's *Retry* affordance is not built — a finding |
| `permission`, `capask` (*waiting for you*) | ✘ by design | the run host answers permission itself in Phase 1 (`GovernedRunHost.cs:673`) and the cap is validated, not enforced (Ruling 26c) — the read model, the policy and the actions exist and are tested (A1, A4, C1, T1); the run-channel projection never produces `Waiting`; CV-3's channel |
| `preparing`, `cancelled`, `reused`, `whatread`, `stale*`, `mechanical`, `suspect`, `nostructure`, `byyou`, `template`, `advisory`, `refusedtos`, `timedout`, `malformed` | ✘ | the compile line and the agentic rungs — CV-2 (Prepare's states fill the regions this slice built) |
| `classmenu`, `tiermenu`, `override`, `forced` | ✘ | the class and tier as menu controls with *Use the chosen class as the session default* / *Restore the rule's value* — CV-2/CV-3; this slice renders the values as text with their provenance inline |
| `quota`, `sending`, `sendfailed` | ✘ | composer-side refusals before start; the send is synchronous here |
| `settings`, `settings-cap` | partial | the header's *Session settings* Popup shows the four values read-only; editing is CV-3's settings model |
| `attaching`, `attachoff` | unchanged | the attach affordance as before |

## SC8 — the keyboard model as landed

| Gesture | Landed | Test |
|---|---|---|
| open | focus in the editor at `page-ready` once, never over an operator who acted (`_operatorActed` set on any key or mouse press in the document) | K7 attended (P-13); the flag's wiring is `PlaceFocusInTheEditor` |
| F6 / Shift+F6 | the document's `PreviewKeyDown` → `CycleRegion(±1)`: header → thread → composer → split (when open) | K6 |
| Tab into the feed | from the header's last stop (the turn count) the document lands on the caret's container; `Once` otherwise | K6 (the 41-turn row) |
| PageDown / PageUp | `MoveBy(±1)` from the container and any inner control | K1b |
| Up / Down | `Scroll(±3 lines)` | K1b |
| Home / End | `MoveTo(first / last)` by index | K1b |
| Ctrl+Home / Ctrl+End | `Leave(ToHeader / ToEditor)` → the document | K1a (the decision), the document's `OnFeedLeave` |
| Tab inside the turn | provenance ▸ → (*Use as the next draft*) → compiled prompt ▸ → (its scroller) → *N events* → the composer | K5 |
| Shift+Tab from the composer | the page posts `focus.leave` backward → `FocusCurrentItemLast()` | the router test row; P-13 attended for the page leg |
| Enter / Space on a disclosure header | the `Expander`'s | K8 (Escape) / Q7 |
| Escape | closes the disclosure holding focus via its header; a popup closes; never leaves | K8 |
| a turn action | focus to the container first, then the request; *Send again* / *Use as the next draft* leave to the editor | A4 |
| jump list | Enter → `FocusTurn(n)` (the container) | K9 |
| the tail | `OpenConsoleAt` → the split at the heading, focused | K10 |
| Ctrl+PageUp / PageDown | untouched (`None`) | K1a |

## SC9 — the announcement policy as landed

The total table of DS-1 §SC9, implemented in `ThreadAnnouncementPolicy.Transition`: accepted
(status, `ItemAdded`); running → completed / answered (status, `Completed`, *Turn b2 completed:
3 edits, 12,400 tokens, 4 minutes 12 seconds.*); running → lane exited n (assertive, `Aborted`);
running → stopped by you (status, `Aborted`); running → waiting (assertive, `Other`, once per request
id); waiting → running (*continues*, status); → not recorded (status, `Other`); the feed stopped
(assertive, once); the Console toggle (status, every time); a refused region (status, every time);
event lines, the reply, folds and focus moves — never (A3). The mapping to the notification API is
`NotificationMapping.For` (A2) under `aide.session.thread`. Announced after the render (a second
`Background` operation — C1 reads `Applies < raises`).

## Seam requests

| To | Request | Why |
|---|---|---|
| Shell lane (`DESIGN.md`, `App.xaml`) | the three page roles: `--inferred` ← `InferredBrush` and `--verified` ← `VerifiedBrush` are added to `ComposerPageTheme.Roles` (the tokens exist); **`--border-strong` ← `BorderStrongBrush`** needs the token declared in `App.xaml` and the row in `DESIGN.md` before the role is added (a role whose token is missing would make the page draw a fallback the shell never drew — `ComposerPageThemeTests.EveryRoleResolvesFromTheTokenDictionary`) | DS-1 §Seams; the plan's CV-1 → SH-2 row |
| Shell lane (`WorkbenchShell.cs:3033`) | pass `Announcer` as the document's third constructor argument: `new Sessions.SessionDocumentSurface(model, store, Announcer)` | one announcer across hosts (ADR-0031); until then the document builds its own polite live region (a `WorkbenchAnnouncer` over a 0-height `TextBlock`) so SC9 is never silent |
| Shell lane (`WorkbenchCommands.cs`, `MainMenuBuilder.cs`, `CommandPalette.cs`) | two `WorkbenchCommand` rows `session.cycleRegion` (F6) / `session.cycleRegionBack` (Shift+F6) dispatching to the active document's `CycleRegion(±1)` (DC-068's three places) | DS-1 P4; until then the document handles F6 in its own `PreviewKeyDown` (no capture surface lives inside the document, so DC-072's yield rule has no instance) |
| Shell lane (`WorkbenchDiagnostics.cs`) | make `Write` internal (or add the four `Thread*` methods) so `thread.*` records go through the one writer | `ThreadDiagnostics` duplicates the sink / file path today — named DM7 debt |
| conductor (E7 retire row) | `CanvasModeCatalog` and `SessionDocumentViewModel`'s mode / split / preset members are vestigial after Ruling 74; `WorkbenchShell.cs:3023` still constructs the model with the catalog's ids and the ContrastProbe iterates `CanvasModeCatalog.All` — the cut is the Shell lane's and X-1's; this slice edited the catalog's one row lambda to construct the split (unowned file, one line) | DS-1 §E7 |
| conductor | Addendum B §B4 row `goal-block` (`:135`) lists six core fields; the template now declares three (deviation iii in `BuiltInCatalogIsTranscribedTests`) | the spec's owner |
| conductor | `ComposerDraftStore.Load` (no product consumer today — grep) would throw on a drafts file holding `tier` / `fan_out_cap` / `budget` | the store's owner skips retired keys when it gets a consumer |
| conductor | `tools/expected-test-counts.json`: `verify-test-run.py --update` at the join (never the node) | the floors |

## Attended rows — RUN-PENDING (each with its steps and its consequence on red)

| Row | Steps | On red |
|---|---|---|
| **P-11** — the contrast census over the composed thread at 5 and 40 turns, both themes | Run the app from `lane/conversation-cv1` (Release below); create a session; send five turns (a read-only message each is enough); open the Console; run the census (`ShellContrastCensus` with the session open) at 1440×900 in dark and light | a pairing below floor → the token / HC finding goes to `DESIGN.md`'s owner; the thread's five outcome colours have no HC mapping today (DS-1 §Flagged) |
| **P-12** — the §2d rows at 1440×900 with the session maximized | Maximize the document (Ruling 47); read the editor's top edge at 1 / 5 / 40 turns from `thread.layout{composer_top}` in `%LOCALAPPDATA%/AiDe/logs`; count half-visible turns | a top edge that moves → L1's belt / floor re-measured against the shell's real chrome |
| **P-13** — the WebView2 boundary | F6 from inside the page (the accelerator republish); Shift+Tab from the editor's first stop into the feed's last stop; Tab from the last stop of a turn into the editor; PageDown / PageUp / Ctrl+Home / Ctrl+End inside a turn; the jump list closing on a click into the editor; `page-ready` placing focus once and never over a reader on b17 | F6 not arriving → seam (2) (a page-side forward); Shift+Tab not leaving → seam (3); focus stolen → the one-shot flag |
| **A6** — NVDA / Narrator | `docs/reviews/nvda-workbench-session.md` Part B/D extended: the outcome status heard once and queued; the lane error as an interruption; F6 announces the region; PageDown reads *b17, …* + *17 of 40*; is *selected* spoken?; is any row heard twice?; Up / Down as scroll discoverable?; the split's rows one by one; does the kind change what Narrator says? | *twice* → drop the region raise; *selected* → the `FeedListAutomationPeer` override (P1's fallback); silence → the second assertive region (P6's upgrade); *no difference* → collapse the kind to the urgency |
| the reduced-motion toggle | flip *Show animations in Windows* while a turn runs; the ring must stop / never start | the `ReducedMotion` seam's WPF adapter (`SystemParameters.ClientAreaAnimation`) is Inferred |

## Findings (not this slice's scope; recorded)

- The mockup's `editorerror` *Retry* is not built; the status line carries the failure sentence.
- `FeedList.LineHeight` is the 13 px × 1.5 constant; the real line box of the words is not read back.
- The turn's `Lane` reads empty while running (the outcome's lane is set at conclude); the engine id is known at accept and could be shown — a one-line follow-up.
- `IsMotionReduced` is read once per bind; a setting change mid-session re-binds nothing (the attended row above).
- The L2 p95-ratio row is sensitive to machine load (one red in the full suite while the gates ran); it is reported by `thread.layout{layout_ms}` and should not be a CI assertion — DC-107's class, a finding for the Test Architect.

## Gate record

`GATE implement · 2026-09-12 · Test Architect (hard) · UX & Accessibility (hard) · the Simplifier · the WPF styling lens · see the closing section for the verdicts and the gate table.`
