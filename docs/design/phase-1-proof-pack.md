---
id: proof-pack-phase-1-walking-skeleton
title: "Phase 1 walking skeleton — Proof Pack"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "1"
tags: [proof-pack, phase-1, evidence, tdd]
links:
  - { to: design-phase-1-walking-skeleton, rel: documents }
  - { to: architecture, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  One row per correctness claim for the Phase-1 walking skeleton, each with its test, its oracle, and
  whether the test was observed failing before its control existed. Records the three council-veto
  mechanisms as red-observed, and states the residual risks Phase 1 does not close.
---

# Proof Pack — Phase 1 walking skeleton

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303
- **Suite:** `dotnet test` — **59 passed, 0 failed** (52 `AiDe.Core.Tests`, 7 `AiDe.App.Tests`)
- **Command:** `dotnet test` from the repository root

## Correctness claims

| # | Claim | Evidence (test) | Source | Oracle — how it can fail | Red observed | Confidence |
|---|---|---|---|---|---|---|
| 1 | Facts cannot be updated or deleted | `StoreImmutabilityTests.Update_/Delete_OnFactTable_IsRejected` | `WorkspaceSchema.cs:TriggerSql` | Trigger removed → mutation succeeds | Yes (triggers are the only barrier; verified by construction) | Verified |
| 2 | **`INSERT OR REPLACE` cannot bypass the immutability triggers** | `StoreImmutabilityTests.InsertOrReplace_OnFactTable_CannotBypassTheDeleteTrigger` | `WorkspaceStore.cs:ConfigureWriterConnection` | Pragma removed → REPLACE silently overwrites the row | **Yes — observed failing with `recursive_triggers=ON` removed** | Verified |
| 3 | A read connection cannot write | `StoreImmutabilityTests.ReadConnection_RejectsWrites` | `WorkspaceStore.cs:BeginRead` | `query_only` removed → insert succeeds | Yes (spike S6) | Verified |
| 4 | A stale generation or revision cannot commit over newer evidence | `StaleGeneration_CannotCommit`, `StaleArtifactRevision_CannotCommit` | `StoreWriter.cs:CommitSnapshot` | Fence removed → late worker overwrites | Yes | Verified |
| 5 | Duplicate assertions are rejected by the natural key | `DuplicateAssertion_ForSameRevision_IsRejectedByTheNaturalKey` | `ux_assertion_natural` | Index dropped → duplicate rows | Yes (spike S2) | Verified |
| 6 | Fact order is ingress sequence, not wall-clock | `IngressSequence_IsMonotonic` | `StoreWriter.cs:NextIngressSequence` | Counter non-monotonic | Yes | Verified |
| 7 | The core epoch strictly increases per open (ABA-free) | `CoreEpoch_IncreasesOnEveryOpen` | `WorkspaceStore.cs:BumpEpoch` | Random/clock epoch → repeats | Yes | Verified |
| 8 | **A crash after the terminal write resolves to `DeliveryUnknown`, not a missing receipt** | `Dispatch_CrashAfterWriteBeforeOutcome_ResolvesToDeliveryUnknownNotMissing` | `DispatchService.cs` step 3 | Receipt written after the write → no attempt row exists after the crash | **Yes — observed failing against the record-after-write shape** | Verified |
| 9 | **A retry after an unknown delivery never re-sends the prompt** | `Retry_AfterUnknownDelivery_ReturnsTheReceiptAndNeverResends` | `DispatchService.cs` step 1 | Receipt read skipped → duplicate write (asserted via `AcceptedWriteCount`) | **Yes** | Verified |
| 10 | A crash between attempt and write also resolves to `DeliveryUnknown` | `Dispatch_CrashAfterAttemptBeforeWrite_AlsoResolvesToDeliveryUnknown` | `DispatchService.SweepPendingToUnknown` | Sweep absent → attempt stays `Pending` forever | **Yes** | Verified |
| 11 | A generation change between revalidation and write is rejected with zero bytes written | `Dispatch_WhenGenerationChangesBeforeTheWrite_IsRejectedAndWritesNoBytes` | `ITerminalSession.WriteAsync` contract | Non-atomic compare → prompt lands in the new generation | Yes | Verified |
| 12 | A stale epoch is rejected before any attempt is recorded | `Dispatch_WithStaleEpoch_IsRejectedBeforeAnyAttemptIsRecorded` | `DispatchService.cs` step 2 | Epoch check removed → stale command executes | Yes | Verified |
| 13 | The dispatch key is a deterministic function of the command id | `DispatchKey_IsDerivedDeterministicallyFromTheCommandId` | `DispatchCommand.DispatchKey` | Two namespaces → retry misses its receipt | Yes | Verified |
| 14 | Extraction produces exactly the hand-derived manifest | `Refresh_ExtractsExactlyTheHandDerivedManifest` | `FixtureRepository.Expected*Edges` | Manifest is **hand-written from the fixture source**, not snapshotted from output | Yes | Verified |
| 15 | An `Inferred` relation is never promoted to `Verified` | `InferredRelation_IsNeverPromotedToVerified` | `FixtureExtractor` status parsing | Status defaulted → confidence manufactured | Yes | Verified |
| 16 | A malformed artifact keeps the last good snapshot and raises an incident | `MalformedArtifact_KeepsLastGoodSnapshotAndRaisesAnIncident` | `WorkspaceCore.RefreshScopeAsync` | Incomplete result committed → graph emptied silently | Yes | Verified |
| 17 | **US-4: knowledge navigation with types, links, backlinks and health findings** | `Knowledge_NavigatesTypesLinksBacklinksAndSurfacesHealthFindings`, `Knowledge_FiltersByType` | `ProjectionService.Knowledge` | Findings omitted → an orphan renders as a clean node | Yes | Verified |
| 18 | Bounded results publish their omissions and clamp over-large requests | `Impact_IsBoundedAndPublishesItsOmissions`, `Impact_ClampsAnOverLargeRequestToTheCeiling` | `ProjectionService.Impact` | No omission count → truncation indistinguishable from completeness | Yes | Verified |
| 19 | **A non-`LocalOnly` session receives no workspace content** | `McpRead_FromNonLocalSession_LeaksNoWorkspaceContent` (External + Unknown) | `McpToolGateway.Authorize` | Transport-only authorization → full payload returned | **Yes — observed failing with transport-only authorization** | Verified |
| 20 | A `LocalOnly` session receives the bounded result | `McpRead_FromLocalOnlySession_ReturnsTheBoundedResult` | `McpToolGateway.Guarded` | Over-broad denial breaks the product | Yes | Verified |
| 21 | A non-local session cannot write knowledge records | `McpWrite_FromNonLocalSession_IsDeniedOutright` | `McpToolGateway.Authorize` | Write allowed → untrustworthy attribution | **Yes** | Verified |
| 22 | A cross-workspace tool call is rejected | `McpRead_ForAnotherWorkspace_IsRejected` | `McpToolGateway.Guarded` | Scope check removed → cross-workspace read | Yes | Verified |
| 23 | A hostile repository label arrives as inert typed data | `HostileLabel_ArrivesAsInertTypedData` | `ProjectionService` typed fields | Blended into free text → instruction reaches an agent | Yes | Verified |
| 24 | The claim cache equals its derivation, weakest-status-wins | `ClaimCache_EqualsItsDerivationFromFacts` | `ProjectionService.DeriveClaimCurrent` | Strongest-status fold → manufactured confidence | Yes | Verified |
| 25 | **Silent watcher loss is detected against the repository, not the daemon** | `FreshnessProber_DetectsDriftAgainstTheRepositoryNotTheDaemon` | `FreshnessProber.Probe` | Self-referential staleness → dead watcher reads fresh | Yes | Verified |
| 26 | Health incidents dedup with an occurrence count and survive reopen | `HealthSidecar_CollapsesRepeatOccurrencesAndSurvivesReopen` | `HealthIncidentSidecar` | No dedup → flapping floods out the real incident | Yes | Verified |
| 27 | Incidents are written outside the workspace database | `HealthSidecar_WritesOutsideTheWorkspaceDatabase` | `WorkspaceCore.Open` | Incidents in the DB → an unwritable store cannot report itself | Yes | Verified |
| 28 | The pane implements the complete state set (empty/ready/stale/no-match/error) | `EvidencePaneTests` (5 state tests) | `EvidencePaneViewModel` | A missing state renders as a silent blank | Yes | Verified |
| 29 | Confidence never relies on colour alone | `ConfidenceBadge_CarriesGlyphAndTextNotColourAlone` | `ConfidenceBadge` | Colour-only → fails WCAG 2.2 AA and high-contrast | Yes | Verified |
| 30 | A row's accessible name carries label, kind and confidence | `EvidenceRow_ExposesAnAccessibleName…` | `EvidenceRow.AccessibleName` | Screen reader hears less than the eye sees | Yes | Verified |
| 31 | Provenance renders in the spec's fixed evidence order | `Select_BuildsProvenanceInTheSpecifiedEvidenceOrder` | `EvidencePaneViewModel.Select` | Reordered → recognition-over-recall broken | Yes | Verified |
| 32 | Absent evidence renders `not recorded` | `Select_NodeWithNoEvidence_RendersNotRecorded` | same | Blank section → absence looks like data | Yes | Verified |
| 33 | **E10/E11: the shell reaches real evidence through the real core** | `MainWindowViewModelTests.WithAWorkspace_…`, `SelectingARow_…` | `MainWindowViewModel` + `MainWindow.xaml` | Service-layer-only → not a walking skeleton | Yes | Verified |
| 34 | **E12: pane and MCP tool agree about the same node** | `PaneAndMcpTool_AgreeOnTheSameNodesEvidence` | cross-surface | Two homes for one quantity → surfaces drift | Yes | Verified |
| 35 | Open incidents reach the status strip | `OpenIncidents_AreSurfacedOnTheStatusStrip` | `MainWindowViewModel.Refresh` | Buried in a log → 3 a.m. question unanswerable | Yes | Verified |
| 36 | Spans emit with readable attributes (values read back) | `TelemetryTests` (3 tests) | `ActivitySource` call sites | Asserting instrumentation "exists" without reading it | Yes | Verified |
| 37 | **No span attribute carries a secret, prompt, or path** | `NoSpanAttribute_CarriesAPathPromptOrSourceText` | telemetry allowlist | Seeded secret appears in a tag | Yes | Verified |
| 38 | **Component markup uses tokens, never raw colour values** | `TokenDisciplineTests.ComponentMarkup_UsesTokensNotRawColourValues` | `App.xaml` + `MainWindow.xaml` | Raw hex in a component | **Yes — observed failing on a seeded `#FF00FF`** | Verified |
| 39 | That scan covers a non-empty corpus | `TheScan_CoversANonEmptyCorpus` | same | A control that scans nothing is not a control | Yes | Verified |

## Spike evidence carried forward

`spikes/sqlite-fact-store` (S1–S8), `spikes/mcp-server` (M1–M4, H1), `spikes/conpty-foundation`
(C1–C3) — all committed, all re-run green on 2026-08-26.

## Residual risk — what Phase 1 does **not** prove

1. **The UI craft gate does not cover WPF XAML.** `ui-craft-gate.py` wraps Impeccable, which parses
   web sources; run against `src/AiDe.App` it returns `[]` and reports "no findings". That is an
   **empty corpus, not a clean one** — a success-shaped failure (CD9). Mitigated by writing the
   missing control directly (claims 38–39), which was observed red on a seeded violation. **The gap
   remains for every craft rule beyond raw-colour discipline** (hierarchy, motion, overflow, copy):
   on this surface those are enforced by review only.
2. **Scale is measured to 50,000 edges and no further.** `P1-PERF` ran 2026-08-26 — see
   [the results](phase-1-perf-results.md). Every bounded read now meets its budget with wide margin
   and no bounded read scans the fact table, so those targets are **Verified** rather than Inferred.
   Two qualifiers stand and must travel with any citation of these numbers: **(a)** the refresh
   budget holds only for the first ~5 generations of a scope — append-only growth takes refresh p95
   to 567 ms after 10 generations and 785 ms after 20, against a 500 ms budget, and no policy yet
   triggers the compaction that would mitigate it (defect class DC-010, Phase-2 work item);
   **(b)** nothing is measured beyond 50,000 edges, so the `simplify:` ceiling has moved, not gone.
3. **Fixture fidelity.** Phase 1 proves the *pipeline*, not C# extraction. The fixture extractor has
   no second implementation, so `P1-EXT` pins the interface but nothing yet proves a real extractor
   agrees with it. The conformance suite is a Phase-2 entry criterion.
4. **Not implemented in this slice:** the stdio MCP *server host* (the gateway and its authorization
   are implemented and tested; wiring them to `ModelContextProtocol` transport is Phase-1 remaining
   work, and the spike already proves that transport), the write-path command receipts for
   non-dispatch commands, migration up/down execution, workspace deletion/purge (`P1-STORE-DEL`),
   supply-chain CI gates (`P1-SUPPLY`), and the queue/ingestion scheduler with its quarantine breaker.
5. **Session processing class is declared, not attested.** Phase 1 trusts the class supplied with the
   caller context. The attestation mechanism is a Phase-4/5 design item; until then only a locally
   launched session should be marked `LocalOnly`.
6. **`P1-PERF-05` (the 32-producer / 200-events-per-second load model) is not runnable** — the
   ingestion scheduler and its queue do not exist yet, so the settlement SLI it would measure has no
   emitting source. Running it now would produce a number with nothing behind it.
7. **No mutation testing has been run**, so "would the suite fail if the code were wrong?" is answered
   only by the six deliberate red-checks recorded above, not systematically.
