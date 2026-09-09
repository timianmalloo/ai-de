---
id: proof-conductor-agent-plane
title: "Proof Pack — Conductor agent plane, Phase 1 (N0–N7)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, acp, proof-pack, phase-1, exit-evidence]
links:
  - { to: plan-conductor-programme, rel: tested-by }
  - { to: spec-conductor, rel: tested-by }
  - { to: note-conductor-n7-refactor-oracle, rel: depends-on }
review-by: 2027-03-09
review-suggested: []
summary: >-
  Evidence for Phase 1 of the Conductor spec: the run-event envelope over a real captured ACP frame
  corpus, the engine catalog's three named refusals, the plane services, the bidirectional ACP
  client verified live, one cell with two mode cohorts, leases and seams, and a governed run driven
  through the App-layer composition root against a Max subscription — zero terminal hosting
  asserted as a counter, a pre-declared refactor matched against an oracle committed before the run,
  and the episode scored into a comparable cell.
---

# Proof Pack: Conductor agent plane, Phase 1

> **This file was committed before the exit episode closed, and that ordering is load-bearing.**
> `ClosedEpisodeScoring.EvidenceFor` asks `ProofPackVerifier` whether each declared artifact is a
> real file; an episode declaring none derives no verification path and scores **Not Scored**. The
> pack was opened at `50ed922` with its rows marked *run pending*, the run declared it, and the rows
> were completed afterwards. A pack written after the close credits nothing.

- **Component:** `src/AiDe.Core/AgentPlane/` — `RunEvent`, `AcpRunEventMapper`, `EngineCatalog`,
  `ProviderRegistry`, `GoalBlock`/`SpawnContract`, `WorktreeProvisioner`, `GovernedSessionSource`,
  `AcpPeer`, `AcpLaneClient`, `AcpEngineProcess`, `LaneCohort`, `LeaseAndSeams`, `RunTriage`,
  `TerminalHostingLedger`, `AcpJson` — plus `src/AiDe.App/Conductor/`
  (`GovernedRunHost`, `ConductorEntry`, `GovernedRunRequest`) and the `mode` column
  (`SqliteWatcherObservationStore` v6).
- **Tests:** `tests/AiDe.Core.Tests/AgentPlane/` — **152 tests, Passed 152 / 152**. Full suite
  **2276 / 0** across two projects (`AiDe.App.Tests` 399, `AiDe.Core.Tests` 1877), verified by
  `python tools/verify-test-run.py` in CHECK mode. Build clean: **0 warnings** under
  `TreatWarningsAsErrors`.
- **Spike:** `spikes/acp-subscription-lane/` — the real captured ACP frame corpus, **88 lines**
  across four files (`read` 22, `read.sent` 3, `write` 59, `write.sent` 4), with `PROVENANCE.md`
  recording adapter and CLI versions, command lines, OS and the redaction rule.
- **Exit run:** `spikes/conductor-exit-run/` — the run file, the result, the transcript and the
  governed lane's own diff, from run `run-832a8655` / episode `6c1ed36f` on host `TIMMALLSTRIX`.

## The four-point falsifiable floor

| # | Floor | Result | Where |
| --- | --- | --- | --- |
| 1 | A **named, pre-declared** refactor with its diff oracle written **before** the run | **Met.** Oracle committed at `cfc3932`; run at `0dc56a0`; all seven clauses hold | `docs/notes/conductor-n7-refactor-oracle.md` |
| 2 | "Zero terminal hosting" as a **positive** assertion | **Met.** `terminalHostConstructions: 0`, and the same counter is observed reading **1** for a real ConPTY session | `result.json`; `TerminalHostingLedgerTests` |
| 3 | The scored cell is `IsComparable == true` | **Met.** `segmentIsComparable: true`, `incomparableReason: null`, `taskClass: refactor-extract-helper`, `mode: governed`, verdict `Partial: 15 / 15 observed` — **scored, not `Not Scored`** | `result.json`; store row read back |
| 4 | A latency SLO breach **fails** the exit | **Met, no breach.** p50 **0.022 ms**, p95 **0.0641 ms** over **287** measured events on host **`TIMMALLSTRIX`**, against the recorded 250 ms SLO | `result.json` |
| + | Launched through the **real App-layer composition root**, no second entry point | **Met.** `AiDe.App.exe --conduct` → `App.OnStartup` → `ConductorEntry` → `GovernedRunHost.RunAsync` — the method the deferred Surface will call | `src/AiDe.App/Conductor/` |

## R4-core — "unchanged by diff", over the whole phase

```
git diff --numstat aa48321..HEAD -- src/AiDe.Core/Watcher/ src/AiDe.Core/Dispatch/ src/AiDe.Core/Terminal/
51  2  src/AiDe.Core/Watcher/SqliteWatcherObservationStore.cs
53  0  src/AiDe.Core/Watcher/WatcherObservationStore.cs
```

**`src/AiDe.Core/Dispatch/` and `src/AiDe.Core/Terminal/` are byte-unchanged — zero files.** The
terminal stack gained an *observer*, not a line: `TerminalHostingLedger` subscribes to the
`terminal.start` activity `ConPtyTerminalSession` already emits on the normal path.

**`src/AiDe.Core/Watcher/` is +104 / −2 across two files, and the two removed lines are additive in
effect.** They are, verbatim:

```
-    private const int SchemaVersion = 5;
-            workspace         TEXT    NULL
```

— the schema constant moving 5 → 6, and the last column of `scored_episode_cell` gaining a trailing
comma so `mode TEXT NULL` can follow it. **No existing method body, no existing SQL column's type or
nullability, and no existing call site changed.** What was added: two interface members
(`RecordEpisodeMode`, `FindEpisodeMode`), their SQLite and in-memory implementations, and a v6
expand-only nullable column with no backfill. The watcher gained **callers** —
`LaneScoring.ScoreGoverned` and `LaneScoring.ImportObserved` — and the two store members those
callers need. `ScoringService`, `ClosedEpisodeScoring`, `Leaderboard` and `WeaveScore` are untouched,
which is why a governed episode carrying `weave/1` is evidence the existing scorer ran.

## Claims and evidence

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| Every captured frame round-trips with no field lost | `EveryCapturedFrameRoundTripsWithNoFieldLost` | `AcpRunEventMapper` | all 88 corpus lines, field-for-field | Seen green | Verified | **The corpus bounds the oracle to what was captured, not to what the protocol can send** — see the corpus residual below |
| An unrecognized kind is carried, never rejected | `AKindNeverSeenBeforeIsCarriedRatherThanRejected`, `UnrecognizedAuthStatusTrafficSurvivesWholeInExt` | `RunEvent.Ext` | unknown wire kind namespaced `acp.*`, payload verbatim | Seen green | Verified | Earned its keep on live traffic newer than the corpus (below) |
| No kind without a Phase-1 producer is ever emitted | `NoKindWithoutAPhase1ProducerIsEverEmitted` | mapper's kind table | `block.open`, `plan.submitted`, `council.verdict`, `decision`, `quota.pressure` never appear | Seen green | Verified | §7.2's `file.edit`, `seam.open`, `seam.resolve` also have no Phase-1 producer |
| `parent_agent_id` is carried and always null | `ParentAgentIdIsAlwaysNullInPhase1` | `RunEvent` | null for every mapped frame | Seen green | Verified | Nothing may be built on it until Phase 2 |
| Cost is populated exactly where the wire reports usage | `CostIsPopulatedExactlyWhereTheWireReportsUsage` | `AcpRunEventMapper.CostOf` | `null`, never 0, where no usage | Seen green | Verified | `result._meta.quota` kept verbatim in `ext` for Phase 3 |
| Three engine rows load; one launch path resolves | `ThreeEngineRowsLoadWithTheirPinnedPackages`, `TheAdapterLaunchPathResolvesForClaudeCode` | `EngineCatalog` | pinned package + version per adapter row | Seen green | Verified | `simplify:` marker — upgrade trigger is the first native-ACP spawn |
| Three named refusals: unknown id, non-adapter mode, unobserved entry module | `AnUnknownEngineIdIsRefusedNeverDefaulted`, `ANonAdapterModeIsRefusedWithANamedReason`, `AnAdapterWithNoObservedEntryModuleIsRefusedRatherThanGuessed` | `AP-0001/2/3` | each refusal names its own reason | Seen green | Verified | The codex deferral rests on `AP-0003`, not on mode — the plan's original claim was wrong and was corrected |
| An unknown provider or account is refused, never defaulted | `AnUnknownProviderIdIsRefused`, `ASingleConfiguredProviderIsNotSubstitutedForAnUnknownOne`, `AnUnknownAccountLabelIsRefused` | `ProviderRegistry` | refusal even when exactly one provider is configured | Seen green | Verified | — |
| The goal block's **six** named fields each fail with their own name | `TheSpecNamesExactlySixFields`, `OmittingAnyOneFieldFailsWithAnErrorNamingThatField` (parameterized over all six), `AnEmptyGoalBlockNamesAllSixFields` | `SpawnContract.Validate` | field-level error names that field | Seen green | Verified | — |
| A direct-api Anthropic spawn is refused by ToS, unmaskably | `AnAnthropicDirectApiSpawnIsRejectedWithTheToSReason`, `TheToSRefusalIsNotMaskedByAnIncompleteGoalBlock` | `AP-0009`, checked first | refusal precedes goal-block validation | Seen green | Verified | Applies while a subscription account is configured |
| Spawn fails closed when observed auth is absent or not a subscription | `AnAbsentObservedAuthStatusRefusesTheSpawn`, `AnObservedNonSubscriptionAuthStatusRefusesTheSpawn` | `AP-0010`, `AP-0011` | null observed auth ⇒ refusal | Seen green | Verified | Protects the operator's money, not their permission |
| The auth-label gap is closed by a **declared** correspondence, enforced only where declared | `AnObservedLabelThatContradictsTheRecordedOneRefusesTheSpawn`, `AnObservedLabelThatMatchesTheRecordedOneAuthorizes`, `WithNoRecordedLabelTheCheckIsKindAloneAndTheAccountSaysSo` | `ProviderAccount.ObservedAuthLabel`, `AP-0013` | mismatch refuses; absent label ⇒ kind alone | Seen green | Verified | Exercised live: the exit run declared `"Claude Max"` and authorized |
| A worktree is provisioned on a namespaced branch, with `coord install` **inside** it | `GitReallyCreatesTheTreeOnTheNamespacedBranchAndTheProvisionerFindsIt`, `CoordInstallRunsInsideTheNewTreeNotTheParent` | `WorktreeProvisioner` | real `git worktree add`; install cwd is the new tree | Seen green | Verified | Live run: `coordInstalled: true` |
| Cleanup is fail-safe and removal is opt-in | `AnythingNotProvablySafeIsParkedAndReported`, `AProvablySafeTreeIsRemovedOnlyWhenRemovalWasAskedFor`, `AnUnanswerableUpstreamQueryCountsAsUnpushedWork` | `WorktreeProvisioner.Release` | every non-safe state parks, with the reason | Seen green | Verified | Live run parked: *"it holds uncommitted changes, which exist nowhere else"* |
| The episode opens with attributes **byte-for-byte** equal to the goal block | `TheEpisodeOpensWithAttributesByteForByteEqualToTheGoalBlock`, `TheOpenAttributesAreExactlyTheThreeEpisodeKeys` | `GovernedSessionSource` | no trim, no normalize, no re-encode | Seen green | Verified | — |
| An incomplete goal block leaves **no** session and **no** episode | `AnIncompleteGoalBlockOpensNothing` | validate-before-create | store empty after refusal | Seen green | Verified | R2: no block, no spawn |
| A killed engine closes the episode `Blocked` and **parks** the tree | `AKilledEngineClosesTheEpisodeBlockedAndParksADirtyWorktree`, `AKilledEngineParksEvenACleanWorktree` | `GovernedLane.EngineKilled` | `Blocked`, never `Abandoned`; never deleted | Seen green | Verified | — |
| The `mode` column is **expand-only**; legacy rows read NULL | `ScoredEpisodeModeMigrationTests` (Watcher suite) | store v6 | pre-migration fixture DB: row count and scores byte-identical, `mode` NULL | Seen green | Verified | **No backfill** — inferring "observed" from an absent session record would bucket a guess |
| `id: 0` is a valid inbound request id and is answered | `AnInboundRequestWithIdZeroIsAnsweredRatherThanReadAsANotification` | `AcpPeer` correlation | reply carries `"id":0` | Seen green | Verified | Exists because a real frame had the value (`write.jsonl:12`) |
| A notification is not answered; an unknown method **is** | `ANotificationIsNotAnswered`, `AnUnknownInboundMethodIsAnsweredNotIgnored` | `AcpPeer` | `-32601` for unknown; nothing written for a notification | Seen green | Verified | — |
| One malformed or over-long frame never kills the loop | `AMalformedFrameIsCountedAndTheLoopKeepsReading`, `AnOverLongLineIsRefusedAndTheSplitterResynchronizes`, `AnUnterminatedTailIsNeverParsedAsAFrame`, `AFrameSplitAcrossChunksIsReassembled` | `AcpPeer` splitter | counted, then reading continues | Seen green | Verified | — |
| A dead child fails every pending request with a named reason; a hung one times out | `WhenTheChildDiesMidSessionEveryPendingRequestFailsWithANamedReason`, `AHungChildFailsTheRequestAtTheStatedTimeout` | `AP-0014`, `AP-0015` | no request waits forever | Seen green | Verified | — |
| Disposal reaps the whole process tree | `DisposingTheEngineReapsTheChildRatherThanOrphaningIt` | `AcpEngineProcess.Dispose` | pid no longer running after dispose | Seen green | Verified | Live run: `engineExited: true` |
| The child's id is answerable **after** the lane reaped it | `TheChildsIdIsStillAnswerableAfterTheLaneReapedIt` | `AcpEngineProcess.ProcessId` | equal before and after `Dispose` | **Yes** — the first governed run crashed here (`InvalidOperationException`) | Verified | Found by running, not by reading; see Findings |
| Overflow applies **backpressure**; nothing is dropped silently | `AFullQueueBlocksTheReaderInsteadOfDroppingTheEvent`, `AnAbandonedPublishIsCountedAndNamedRatherThanLostSilently` | `AcpEventQueue` | publish blocks; `Dropped` stays 0; abandonment counted | Seen green | Verified | Live run: 287 published, 0 dropped |
| The run event is observable **before** the response to the next request for the same call id | `TheRunEventIsObservableBeforeTheResponseToTheNextRequestForTheSameCallId` | `AcpPeer` ordering | deterministic ordinal, not a duration | Seen green | Verified | The ordinal is the assertion; the 250 ms is an SLO (DC-107) |
| Latency exists, is a number, and reads "not recorded" rather than 0 | `EveryPublishedEventCarriesANormalizationLatencyThatIsANumber`, `WithNoReceiptTimestampTheLatencyReadsNotRecordedRatherThanZero` | `ObservedRunEvent` | removing the receipt stamp yields `"not recorded"` | Seen green | Verified | p50/p95 are host-bound — see below |
| Every guard the client ships fires | `EveryGuardTheAcpClientShipsFiresWhenTheSelfTestIsRun` | `AiDe.Core.AcpProbe --self-test` | eight guards, each observed firing | Seen green | Verified | Mirrors `src/AiDe.Mcp/Program.cs --self-test` (DC-104) |
| One governed and one observed episode share **one** cell as **two** modes, in either ingest order | `OneGovernedAndOneObservedEpisodeShareOneCellAsTwoModes` (both orders) | `LaneScoring` + real SQLite | one `ScoreSegment`; modes exactly `{governed, observed}` | Seen green | Verified | Asserted at the `ScoreSegment`/store level, **not** through `LeaderboardComposer` — see below |
| Neither stored task class arrived by parameter default | `NeitherStoredTaskClassCameFromAParameterDefault`, `TheGovernedScoringApiRequiresATaskClass` | `LaneScoring` | the **stored** class is read, not the call argument | Seen green | Verified | `"audit-import"` is *comparable*, so a default ranks silently in the wrong cohort |
| A governed episode is scored by the **unchanged** `ScoringService` | `AGovernedEpisodeIsScoredByTheUnchangedScoringService` | scorer's own schema version | stored row carries `weave/1` | Seen green | Verified | R4-core: the watcher gained callers, not semantics |
| The ACP session's cwd **is** the provisioned worktree, on a namespaced branch | `TheSessionOpensInTheProvisionedTreeOnANamespacedBranch` | `AcpLaneClient.NewSessionAsync(ProvisionedWorktree)` | the tree is passed as a type, not a string | Seen green | Verified | Live run: cwd was `lane-repo-agent-claude-code-lane-aec` |
| A T0 run reaches dispatch with zero council lanes and zero plan artifacts | `ATierZeroRunReachesDispatchWithNoCouncilLaneAndNoPlanArtifact`, `ATierTwoRunDoesNotSkipPlanAndCouncil`, `ATierZeroRunThatFansOutDoesNotSkip` | `RunTriage` | stages measured off the run, not implied | Seen green | Verified | Phase 1 has no producer of either, so "zero" is structural |
| An edit outside the lease raises a seam; **an edit inside raises none** | `AnEditOutsideTheLeaseRaisesASeam`, `AnEditInsideTheLeaseRaisesNoSeam`, `ANonEditToolCallRaisesNoSeamEvenOutsideTheLease` | `LeaseMonitor` | the negative control, without which seam-on-everything passes | Seen green | Verified | Recognizes `kind:"edit"` frames only — a shell write is invisible to it (below) |
| One edit is one seam across its four frames | `TheSameEditSeenInSeveralFramesRaisesOneSeam`, `TwoDistinctEditsOutsideTheLeaseAreTwoSeams` | identity `(toolCallId, path)` | four corpus frames ⇒ one seam | Seen green | Verified | Otherwise `seam_resolution_ratio` measures adapter chattiness |
| Closing with an open seam **forces** `Blocked` | `ClosingWithAnOpenSeamForcesBlocked`, `AResolvedSeamRestoresTheRatioAndLetsTheRunCloseAsDeclared` | `GovernedLane.Close` | the declared outcome is overridden, not refused | Seen green | Verified | Live run: 0 raised, ratio 1.0 |
| **Zero terminal hosting**, asserted positively | `AnOpenLedgerCountsARealTerminalHostConstruction` (counter reads **1** for a real ConPTY session), `AClosedLedgerNoLongerCounts` | `TerminalHostingLedger` over `aide.terminal.runtime` / `terminal.start` | the run reads **0** and the counter is shown able to read 1 | Seen green | Verified | Counts the *attempt*: the activity opens before interop, so a failed construction still counts |
| The governed run is launched through the App-layer composition root | `spikes/conductor-exit-run/` (run file, result, transcript) | `AiDe.App.exe --conduct` → `GovernedRunHost.RunAsync` | no hand-assembled harness; one `RunAsync` | Live run, exit code 0 | Verified | The live run is not re-runnable without a subscription and an adapter install — the tests are |
| The pre-declared refactor's diff matches its oracle | `spikes/conductor-exit-run/lane.patch` + commit `0dc56a0` | the governed lane's own worktree | 7 clauses: one `internal AcpJson.Text`; **0** occurrences of the old signature under `src/`; **7** qualified call sites; no `using static`; `GoalBlock.cs` untouched; nothing outside `AgentPlane/`; build clean | Seen green | Verified | The lane left the work **uncommitted** in its parked tree; the patch was applied here |
| The episode was scored into a **comparable** cell as `governed` | store row: `('6c1ed36f…', 'refactor-extract-helper', 'Partial', 'governed', <workspace>, 'Partial: 15 / 15 observed')` | `scored_episode_cell` | `IsComparable == true`, `IncomparableReason == null` | Seen green | Verified | **Only because the run was rooted in a clone — DC-115** |

**Boundary set covered:** unknown/known engine ids · adapter / native / unobserved-entry rows · unknown
provider, unknown account, needs-login, quota-degraded · all six goal-block fields, blank, out-of-range,
empty · direct-api ToS, absent auth, non-subscription auth, matching and contradicting labels ·
worktree add success/failure, coord-install failure, clean/dirty/unpushed/unanswerable-upstream release ·
frame split, unterminated tail, over-long line, malformed line, `id: 0`, unknown method, notification,
unmatched reply, error reply, dead child, hung child, orphan reap · queue within capacity, full, drained,
abandoned, completed · lease inside/outside/outside-the-tree/non-edit/no-location/repeat/mixed · seam open,
resolved, unknown, none · both ingest orders for the one-cell claim.

**Testing Strategy triggers applied:** **D1** (units at every seam), **D6** (the golden ACP frame corpus —
88 real captured lines, not authored events), **A6** (the ACP protocol version is pinned **and the echo is
asserted**), **E11** (compositions through the real store, the real registrar and the real `ScoringService`),
plus a live end-to-end run against a real subscription. No triggered directive was dropped.

**Mutation sense:** the `id: 0` correlation is proven behaviourally — the guard exists because a real
corpus frame carried the value, and a truthiness-keyed table leaves that peer waiting forever. The
terminal counter is proven behaviourally in both directions: **1** with a real ConPTY session inside the
ledger's window, **0** with the ledger closed. The `ProcessId` regression test was written **after**
observing the failure it prevents, not before imagining it.

## Findings — things the run contradicted or discovered

1. **`AcpEngineProcess.ProcessId` threw after disposal.** The first governed run completed the entire
   arc — worktree provisioned, episode opened, prompt answered `end_turn`, episode closed, scorecard
   written — and then died assembling its own result, because `Process.Id` raises
   `InvalidOperationException` once `Process.Dispose` has run. `HasExited` carried exactly this
   reasoning in its own doc comment; the property one line above it did not. **A reasoned-about hazard
   survived one field away from where it was fixed.** Fixed at `fbbb048` with a regression test.
2. **`StartupUri` cannot be withdrawn.** WPF applies it *after* `OnStartup` returns and refuses `null`,
   so the headless branch's `StartupUri = null` threw before any run began. Moved to code.
3. **DC-115 — evidence committed on a lane's branch is invisible to the verifier.** Registered, with
   both halves measured. A governed lane in a *linked worktree* has its session rebound to the parent
   checkout by `RepositoryCorrection`; `ProofPackVerifier` then reads that checkout's working tree,
   which is on `main` and does not carry the pack — so the episode scores **Not Scored** and the
   scorecard makes a statement about the agent when the true statement is about where somebody looked.
   The exit run avoided it by rooting in a clone. **This is the single most consequential thing N7
   found, and it is uncontrolled.**
4. **The lease is enforced over announced edits only.** `LeaseMonitor` recognizes `kind:"edit"` tool
   calls; a write performed through a shell command carries no `locations[]` and raises no seam. The
   exit run's prompt forbade builds, so nothing exercised it — the control is narrower than "an edit
   outside the lease raises a seam" reads.
5. **The two evidence doors disagree.** `AuditLogEpisodeSource.HasProofPackArtifact` credits a declared
   path by **substring**, never touching the filesystem; the governed door requires the file to exist.
   Two definitions of "has evidence", and the stricter one applies to the lane the product drove.
6. **A `Task.Delay(30)` was measured at 17 ms, twice.** It reddened
   `RefreshMetricsTests.AFailedRefreshIsTimedToo` in two of four full suite runs and passed in
   isolation both times — the recurrence threshold, so a class rather than a flake to retry.
   **DC-107's own carve-out is contradicted by it:** the control deliberately permits a *lower* bound
   beneath an injected `Task.Delay`, reasoning that "slower hardware only makes it more true", and
   Windows timer coalescing can complete a delay **early** relative to the clock the code reads. The
   three timing assertions in that file now assert the duration is **recorded at all**, which is what
   their own comments claim to be about; DC-107 moves to `partially-controlled` and records the third
   instance. Not caused by this phase — surfaced by it, because the exit gate is the first thing that
   made a retry unacceptable.

7. **The full gate set found four things eight nodes of subject-scoped gates did not.** Every one
   was this phase's own doing and every one had been green-by-omission since the node that caused it:
   `verify-id-allocators` (twenty `AP-` refusal codes with **no declared allocator** — the shape where
   two sessions each mint `AP-0021`, both files are valid C#, the merge is clean, and two different
   refusals answer to one identifier forever); `verify-bounds-are-enforced` (`FanOutCap` and `Budget`
   named as limits and never compared); `verify-cited-controls` (`GovernedSessionSource` citing
   `IEpisodeSource`, a name that resolves to nothing, inside a sentence claiming enforcement — DC-095);
   and `verify-api-crefs` (`ext` reported as a decapitated `Next`). **This is the argument for the
   "full set once at N7" clause, made by the clause paying off.** The gate policy's own reasoning was
   that a skipped gate looks identical to a run one; four gates were skipped for eight nodes and
   looked identical to green.

## Residual

- **DC-115 is `uncontrolled`.** A governed lane in the operator's own worktree cannot be credited for
  the evidence it commits. The exit run's `IsComparable == true` and `Partial` verdict are true of the
  clone shape and **not** of the worktree shape. Owed a fix before §6.4's shape is used for real.
- **DC-111's control is deferred.** Its shape was met again here: the exit run's transcript is
  `*.log` and fell under a blanket ignore. Handled with a named negation rather than `git add -f`,
  which is the repair, not the control.
- **DC-112 is `partially-controlled` here** until the rev-64/65 pack refresh lands.
- **DC-113 is `uncontrolled`.** Registered the same day. Every gate in this node was run **bare** or
  redirected to a file, by rule rather than by habit — which is still not a control.
- **The unregistered-session capture will not be scored.** An episode with no `SessionRecord` is
  owned by the audit-import path and is never re-scored by `ClosedEpisodeScoring`.
- **Durations are "not recorded" for N0–N3.** The instrument was measured rather than trusted:
  `audit-log.py append` *consumes* the start stamp, so one `start` yields a duration on exactly one
  subsequent `append`. N0–N3 were dispatched without one. **N4 = 1931 s, N5+N6 = 1374 s** are the only
  measured node durations; N7's is recorded by this session's own `start`. Zero is never written for
  the others.
- **The corpus is an incomplete oracle, and that is a finding rather than a flaw.** N4's live runs
  observed `session_info_update`, a discriminator absent from all 88 committed frames; it was carried
  correctly under `ext`. The exit run observed it again, alongside `usage_update` — so `ext`
  preservation earned its keep twice on traffic newer than the corpus, and *"every frame round-trips"*
  is bounded to **what was captured**, never to **what the protocol can send**.
- **`authStatus.account.plan` is not stable.** The corpus recorded `"max"`, N4's live run `"Claude Max"`,
  and the exit run `"max"` again — same pinned adapter, three observations, two values. **Nothing may
  key on it.** The spawn gate keys on `kind` and on the *declared* label correspondence.
- **The model is a declared cohort label, not an observed fact.** The exit run declared
  `claude-code-default` because the adapter reports modes and commands but no model identity that this
  client reads. Phase 3's standings work needs an observed model, and today's value would rank two
  different models in one cohort. Marked **Inferred**.
- **N5's "one cell" is asserted at the `ScoreSegment`/store level, not through `LeaderboardComposer`.**
  The composer's cohort minimum of 5 renders every cell **Not Comparable** with two episodes, so a
  composer-level assertion would prove the opposite of what R4 means. The exit run's comparability is
  likewise `ScoreSegment.IsComparable`, not a rendered rank.
- **Spec §7.2's `file.edit`, `seam.open` and `seam.resolve` have no Phase-1 producer.** Seams are a
  ledger entry, not a run event, because minting one would need a second writer of the per-run `Seq`.
- **The latency numbers are host-bound.** p50 0.022 ms / p95 0.0641 ms on `TIMMALLSTRIX` measure
  *normalization* — receipt to published envelope — inside one process with no I/O. They are strong
  evidence that the 250 ms SLO is not near breach on this machine and **no evidence at all** about a
  loaded machine, a slower one, or a lane whose events cross a process boundary (DC-107).
- **The goal block's `Budget` and `FanOutCap` are validated but NOT enforced.** `SpawnContract`
  refuses a budget of zero or a negative cap, and nothing thereafter counts requests or tokens
  against the budget — Phase 1 has no request meter — and the plane spawns no sub-lane for a cap to
  bound. `verify-bounds-are-enforced` flagged the field-name constants that read as limits; they were
  renamed with a `Key` suffix so the name stops claiming what the code does not do, and the gap is
  recorded here rather than closed. **Enforcement was deliberately not added at this node:** the exit
  run's evidence describes the binary that ran, and quietly shipping a control that could have changed
  its outcome would make the record describe a build nobody executed.
- **One run is one sample.** Two runs were executed; the first found a defect and did not produce a
  result, the second is the exit evidence. Neither is a distribution.
- **The exit run is not re-runnable without a Max subscription and a local adapter install.** The
  behavioural evidence — 152 AgentPlane tests, 2276 in total — is re-runnable with no setup (DC-002).
  `spikes/conductor-exit-run/` is the record of the live run, in the same sense the frame corpus is.
- **The governed lane left its work uncommitted** in a parked worktree; the patch was applied and
  committed here. A lane that commits its own work is Phase 2's `converge`.
- **`verify-site-figures` was measured, not assumed.** Before regeneration it reported **10 stale of
  14** — audit-entry, artifact, ledger, defect-class and public-symbol counts, every one of which this
  phase moved. `tools/regenerate-derived.py` rewrote all ten and the gate now verifies **14 of 14**.
  So the staleness carried into this node was *not* independent of the phase, and nothing was left
  unrepaired; the earlier report of a pre-existing 6-of-14 failure did not reproduce.
- **`docs/audit/audit-data.js` was stale in a way regeneration does not cover.** The committed file
  was produced by an older renderer (its records begin `"id","shortname"`; the current one emits
  `"actor","artifacts","datetime"`), so `verify-derived-views` failed until
  `audit-log.py render` rewrote it. `regenerate-derived.py` does **not** render it — the two derived
  bundles have separate regenerators, and only one of them is in the "regenerate everything" script.
- **Regeneration ran twice in this session and once in the history.** The first run preceded the N7
  audit append, which re-staled the audit-entry figure — the script's own failure text names that
  ordering. The tree was regenerated again after the append and committed once. Recorded rather than
  smoothed: the rule is *append first, then regenerate*.
