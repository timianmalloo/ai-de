---
id: proof-architecture-recut-and-session-names
title: "Proof Pack — Rulings 99, 102, 98, 94: unique session names, the wrapping task-class list, the retired fixture revision, and the Architecture re-cut"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, sessions, layout, perspective, architecture, evidence, addendum-c, ruling-94, ruling-98, ruling-99, ruling-102]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: adr-0032-perspective-layout-slots, rel: tested-by }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Lane sessions-r94-r98-r99-r102, four commits in ruling order 99 → 102 → 98 → 94. Every CONDITION
  met by a test observed red then green: session names count up " (2)", " (3)" over the store and
  both captions disambiguate; the task-class list wraps (extent 485.9 > viewport 444.0 was the red);
  a "rev-1" snapshot re-extracts once then the observed HEAD stands; Architecture = Left Graph 0.22 ·
  Center Contexts, Domain · Right/Bottom empty collapsed, the inspector kind retired with a drop
  report naming the ruling. Core 2636 → 2638, App 955 → 961. Operator's extent read: 0.22.
---

# Proof Pack — Rulings 99 · 102 · 98 · 94 (lane `sessions-r94-r98-r99-r102`)

Base `dda140ba`; branch `lane/sessions-r94-r98-r99-r102`. Commits in the ruling order the brief set:

| # | sha | Ruling | Title |
|---|---|---|---|
| 1 | `b2c55f47` | 99 | feat(sessions): session names are unique within a workspace by a counter suffix; Create never refuses |
| 2 | `a026c630` | 102 | fix(sessions): the task-class list wraps its descriptions; no horizontal scrollbar |
| 3 | `ea90be02` | 98 | fix(index): a snapshot stamped with the retired fixture literal is not reusable; one re-extraction, then the observed HEAD |
| 4 | *(this commit)* | 94 | feat(architecture): Left Graph · Center Contexts, Domain · Right empty; the `inspector` kind retires into the Evidence row |

**Tests executed** (`dotnet test --no-build`, read from the summary line, never from an exit code):

| Project | Before (base) | After | Δ |
|---|---|---|---|
| AiDe.Core.Tests | 2636 passed · 1 skipped | 2638 passed · 1 skipped | +2 (R99 rule, R98 reuse) |
| AiDe.App.Tests | 955 passed | 961 passed | +6 (R99 ×3, R102 ×1, R94 ×2 net of the two retired-pair tests replaced) |

`verify-test-run.py --update` was **not** run (the recount is the conductor's at the join).

## Ruling 99 — session names unique by a counter; Create never refuses

| # | Claim | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | Both captions — the session tab's and the Console's — disambiguate for free from the name (CONDITIONS) | `ConsoleSplitPlacementTests.TwoSessionsOpenedOnOneDay_HaveDistinctSessionAndConsoleCaptions` (composed Coding frame, two sheets at 2026-09-14) | `Assert.Equal() Failure: Collections differ ↓ (pos 1) Expected: ["2026-09-14 session", "2026-09-14 session (2)", "Console — 2026-09-14 session", "Console — 2026-09-14 session (2)"] Actual: ["2026-09-14 session", "2026-09-14 session", "Console — 2026-09-14 session", "Console — 2026-09-14 session"]` | passed | Verified |
| 2 | The default name takes " (2)", " (3)" over every session the store holds, not only open tabs | `TheNewSessionSheetTests.TheDefaultNameTakesACounter_WhenASessionOfThatNameAlreadyExistsInTheWorkspace` | `CS0117: 'SessionConfigStore' does not contain a definition for 'ExistingNames' / 'UniqueName'` (the rule did not exist) | passed | Verified |
| 3 | A typed duplicate is suffixed at Create and announced | `TheNewSessionSheetTests.ATypedDuplicateNameIsSuffixedOnCreate_AndTheAnnouncementSaysSo` — the flow's sentence: *A session named “payments” already exists, so this one is “payments (2)”.* | same compile red | passed | Verified |
| 4 | One function for the rule (the later parallel-session slice will call it on the parent's name); a carried counter is not part of the base; never refuses; no sessions root → empty, not a throw | `SessionConfigStoreTests.UniqueName_CountsUpFromTwo_OverEverySessionInTheStore_AndNeverRefuses` | same compile red | passed | Verified |

Surface list reached: store (`SessionConfigStore.ExistingNames`, `UniqueName`) → view model (`NewSessionSheetViewModel` default name and `Create`, `NewSessionResult.RenamedFrom`) → flow announcement (`NewSessionFlow`) → rendered captions (session tab, Console document). No time-of-day; Create never refuses.

## Ruling 102 — the task-class list wraps; no horizontal scrollbar

| # | Claim | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | At the sheet's own width (520, height to content) the list's extent never exceeds its viewport, no horizontal scrollbar is visible, horizontal scrolling is Disabled on the list, and every laid-out description longer than its row is wrapped, not clipped | `TheTaskClassIsChosenNotTypedTests.TheTaskClassDescriptionsWrapToTheList_AndNothingScrollsSideways` | `the list scrolls sideways: extent 485.9 > viewport 444.0` | passed | Verified |

**Oracle substitution, stated:** the ruling's CONDITIONS name `IsTextTrimmed`. That property is .NET Framework 4.8's; on .NET 10 WPF `TextBlock` has none — observed: `error CS1061: 'TextBlock' does not contain a definition for 'IsTextTrimmed'`. The test measures the stronger fact instead: a description whose one-line width (`FormattedText`) exceeds the width it was given must be taller than one line (wrapped), else it is clipped and fails. Rows below the list's `MaxHeight` are virtualized (`ActualWidth` 0) and are excluded from that check by measurement, with a positive control that at least one laid-out description is longer than its row.

Fix: one line — `ScrollViewer.SetHorizontalScrollBarVisibility(taskClass, ScrollBarVisibility.Disabled)`; a ListBox measures its items at infinite width otherwise, so the existing `TextWrapping = Wrap` never fired.

## Ruling 98 — a `rev-1` snapshot is not reusable; one re-extraction, then the observed HEAD

| # | Claim | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | A store whose latest committed snapshot carries `rev-1` (base — `SourceRevision.Base`, since `Stamp` appends `+x<generation>`) indexes as re-extracted, not reused, **exactly once**, and `CurrentSourceRevision()` then equals the passed revision; the third index reuses | `Phase3SurfacingTests.ASnapshotStampedWithTheRetiredFixtureLiteral_IsReExtractedOnce_ThenTheObservedHeadStands` | `Assert.Equal() Failure: Values differ Expected: Tuple (1, 0) Actual: Tuple (0, 1)` (the second index reused) | passed | Verified |
| 2 | A store with a real revision stays reused | `Phase3SurfacingTests.AnUnchangedScopeIsReused_AndTheReuseIsReportedSeparately`, `ForceReachesTheCoreThroughTheCommandSurface` — their fixture revision moved off `rev-1` to an observed-HEAD shape (`51e806f8`); `ForceReachesTheCoreThroughTheCommandSurface` went red under the guard for exactly that reason (`Expected: 1 Actual: 0` reused) and is green on a real revision | n/a | passed | Verified |

The literal is `SourceRevision.RetiredFixtureLiteral` with the comment that it is a retired fixture, not a revision. "Print *not recorded* for the literal" was refused as masking (the ruling). The guard compares the snapshot's **base**.

**Defect class (new; placeholder id — the conductor allocates): DC-217 — *a fix at the writer does not reach a store the old writer already wrote; the reader shows the stored default.*** Shape: a door default (DC-110) is removed from the code path that stamps it, but every record the old path already stamped still carries it, and a reuse/skip guard keyed on inputs (a fingerprint) never opens the stored value — so the fix is invisible on every workspace that predates it. Signature: a release note says "fixed", the operator's screen still shows the old value on the new build (`rev rev-1` on 51e806f8). Control: the reuse guard names the retired value and refuses to reuse it (this ruling's test); the general rule — a retired default is a *migration*, not only a code change, and the migration is the re-read that the guard now forces.

## Ruling 94 — Architecture re-cut; `inspector` retired into the Evidence row

**Condition (1) — the extent, read from the operator's file.** `C:\Users\malla\AppData\Local\AiDe\workspaces\aide.31abcd25046a4df98d65044abb06f0f5\layout.architecture.zones.json` (savedUtc 2026-09-14T16:13:09Z, appVersion 0.3.0), read by this lane: **Left `[graph/canvas]` extent 0.22, collapsed false · Right extent 0.22, collapsed false, no content · Bottom extent 0.22, collapsed true · Center `[contexts (activeIndex 0), domain/classdiagram]`**. The default now uses `WorkbenchLayout.ArchitectureLeftExtent = 0.22` (named so the number is the operator's; it equals `ZoneState.DefaultExtent`). The Right is **collapsed** in the default — the ruling wins over the file.

| # | Claim | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | `ArchitectureDefault()` = `Left:[graph@0]|Right:-/collapsed|Bottom:-/collapsed|Center:[contexts+domain@0]`, Left 0.22, no `view`/`inspector` in it; `view` still admitted; no perspective admits `inspector`; `inspector` ∉ `KnownKinds` | `PerspectiveLayoutSlotTests.TheArchitectureDefault_IsLeftGraph_CenterContextsThenDomain_RightAndBottomEmptyAndCollapsed` | `Assert.Equal() Failure: Strings differ ↓ (pos 6) Expected: "Left:[graph@0]|Right:-/collapsed|Bottom:-"··· Actual: "Left:[evidence@0]|Right:[provenance@0]|Bo"···` | passed | Verified |
| 2 | **Condition (2)**: a pre-94 Architecture envelope (Left Evidence · Right Provenance · Center graph/domain/contexts) restores with Provenance dropped and **reported naming the ruling**, not a crash, the rest intact; the report: *1 pane from your saved layout isn't available in Architecture — Provenance (retired by Ruling 94: its origin, extractor and revision now show under the selected Evidence row).* | `PerspectiveLayoutSlotTests.APreRuling94ArchitectureEnvelopeCarryingProvenance_RestoresWithTheInspectorDroppedAndReported_NamingTheRuling` | `Assert.Equal() Failure: Strings differ ↓ (pos 24) Expected: ···":[evidence@0]|Right:-|Bottom:-|Center:[gr"··· Actual: ···":[evidence@0]|Right:[provenance@0]|Bottom"···` | passed | Verified |
| 3 | ADR-0032 test 1 extended: the golden pre-C Coding envelope still drops 11 — six to Architecture, four to Coordination, and `inspector` with `AdmittedBy == null` and the retirement sentence in the announcement's *also* tail | `ZoneLayoutSlotsTests.RestoringAPreCCodingEnvelope_DropsTheFiveLoomkeeperKinds_AndReportsNamingCoordination` (extended) | red under the retirement before `RestorableKinds` existed: the store dropped the unrestorable kind in silence (observed on the composer probe replay: `dropped=9`, 9 panes announced, no Provenance) | passed; the probe replay reads `dropped=10` again | Verified |
| 4 | The three fields (origin · extractor · rev) render as a second, smaller line under the **selected** Evidence row and nowhere else; unselected rows are one line; a row with no neighbours reads `not recorded`; no section heading anywhere in the pane; no second pane | `EvidenceMasterDetailTests.SelectingARow_ShowsItsOriginExtractorAndRevisionAsASecondMutedLineUnderIt_AndNowhereElse` (realized window at the shell's 13px body) | `Assert.Equal() Failure: Values differ Expected: 2 Actual: 1` (R2's row stayed one line after selection) | passed | Verified |
| 5 | The kind is retired from the product: no row, not in `KnownKinds`, in `RetiredKinds` with a sentence naming Ruling 94, and restorable in exactly one sense (`RestorableKinds` ⊇ `KnownKinds` ∪ retired) | `EvidenceMasterDetailTests.TheInspectorKind_IsRetiredFromTheProduct` | `Assert.DoesNotContain() Failure: Filter matched in collection` (the `inspector` row was present) | passed | Verified |

**"Muted" on a selected row (DC-139 / TC2):** the detail line has **no brush of its own** — the selected row paints the accent ground and hands its content the sunken ink; a leaf pinned to `TextMutedBrush` would sit at 1.13:1 on it (the sheet's description line records that measurement). Hierarchy is by size: 12px under the pane's 13px body (`DESIGN.md:84,171`). The accessible name of a selected row carries the detail too.

**Condition (3) — the Graph pane at 0.22 and the horizontal scrollbar.** Measured from the operator's screenshot (2560×1600): the scrollbar at y≈1118 (display) spans x≈68–490 — the whole Left pane's width **below** the WPF tab header (*Graph ×* at y≈110 shows no scrollbar) — and the header row *Search a node · ← Back · ⌂ Overv…* is clipped at the pane's right edge. That row is the canvas's **own** HTML control strip: `CanvasPage.cs:97-100` (`<header>` with `#search`, `#back`, `#home`, `#fit`) under `header { display: flex; align-items: baseline; gap: 12px; }` (`CanvasPage.cs:34`) with **no `flex-wrap`** — its intrinsic width exceeds ~540px, the body overflows, and the WebView2 document grows a horizontal scrollbar. `#filters` (`:45`) wraps; the header does not. **A finding for the Explore lane, not a blocker** (`CanvasPage.cs` is the canvas's): **DC-218** — *a flex row with no wrap inside a pane whose width is a fraction of the frame overflows the document at the narrow default, and the document — not the pane — grows the scrollbar the operator sees.* Control: `flex-wrap: wrap` on `header` (as `#filters` already has) and a rendered check at the Left's 0.22 extent. Not rendered headlessly here: the canvas is a WebView2 (no headless render in the test host); the measurement is the screenshot's geometry plus the CSS read — **Inferred** as to the exact overflow width, Verified as to the cause's shape.

**The `inspector` sweep (HYG-A) — files touched:**
- Product: `SurfaceContentFactory.cs` (the row, `EvidenceDetail`/`EvidenceDetailContent`/`LoadDetailInto`, the `evidenceSelection` seam and the 40-line FINDING comment deleted; `RetiredKinds`, `RestorableKinds`, `EvidenceRowItem` added; the master gets its inline detail) · `EvidencePaneViewModel.cs` (`EvidenceSelectionSource` deleted — dead once the pair was cut; `SelectedDetailLine` added; the confidence line shows the **base** revision, per `SourceRevision`'s own remark) · `WorkbenchShell.cs` (`WorkspaceDependentPaneKinds`, `RestorableKinds`) · `LayoutPersistence.cs` (the retired caption) · `ZoneLayout.cs` (`ArchitectureDefault`, `ArchitectureLeftExtent`; the legacy combined seed's Provenance surface → `sources/view/Sources`) · `LayoutModel.cs` (the legacy tree seed, same rename) · `PerspectiveShell.cs` (a measurement remark re-dated).
- Tests: the legacy seeds' fixture surface renamed in `LayoutPersistenceTests`, `WorkbenchAdapterTests`, `WorkbenchControllerTests`, `DropTargetResolverTests`, `WorkbenchLayoutTests`, `WorkbenchStoreTests`, `WorkbenchDragReconcileOracleTests`, `ReconcileTests`; arbitrary `"inspector"` kinds replaced in `ZoneBackedLayoutServiceTests`, `ZoneLayoutServiceTests`, `ZoneLayoutStoreTests`, `ZoneLayoutPersistenceTests`; product assertions re-pointed in `WorkbenchShellTests`, `PerspectiveMenuTests` (the row, the count 16 → 15, the literal View table), `PerspectiveShellTests` (the landing is Contexts), `WorkbenchDragCompletedHookTests` (drags re-pointed to the new default), `ZoneLayoutSlotsTests`; `EvidenceMasterDetailTests` rewritten (the pair's oracle withdrawn with the second kind, per the ruling). The composer probe's **replayed 2026-09-11 file keeps its `inspector` surface** (an old file legitimately carries the retired kind) and the probe now hands persistence the shell's `RestorableKinds`.
- Docs: `docs/specs/addendum-c-perspectives.md` (§A7 rows and summary, US-C6's oracle, §B4's Architecture table), `docs/adr/0030`, `0031`, `0032` (test 1 extended). History untouched: the rulings, earlier proofs, `pre-perspectives.zones.json`, §R row 9.
- Residual grep: `src/` has no `"inspector"` surface kind (the one hit is Node's built-in module list, `NodeBuiltinModules.cs:49`) and no Provenance surface; `tests/` keeps `inspector` only where an old saved file is the fixture (the golden envelope, the probe's replay, the pre-94 envelope tests).

## Findings not changed (placeholder ids; the conductor allocates)

| id | Class | Where | Status |
|---|---|---|---|
| DC-217 | A fix at the writer does not reach a store the old writer already wrote; the reader shows the stored default | Ruling 98's cause; controlled by the reuse guard | controlled (this lane) |
| DC-218 | A flex row with no wrap inside a fraction-width pane overflows the document at the narrow default; the document, not the pane, grows the scrollbar | `CanvasPage.cs:34,97-100` | finding for the Explore lane |
| DC-219 | A store drops a kind it cannot restore in silence, so a kind the product retires vanishes from the operator's saved layout with no report unless the shell lists it as restorable | `ZoneLayoutStore.Read` (`restorableKinds` filter, `:174`) — closed for retired kinds by `RestorableKinds`; **still open for a kind a newer build wrote** (dropped silently by an older build) | partially controlled |
| (hygiene, fixed in passing) | A test that builds every kind builds a live ConPTY for `terminal` and never disposed it — the ledger read one `s-terminal` start with no stop on every full App run, on the base too | `SurfaceContentTests.EveryKindTheFactoryClaimsToKnow_…` now disposes what it builds; the stop is on the ledger in isolation, not in the full run — see *Terminal ledger* and DC-220 | fixed at the source; the count still reads 10/9 |
| DC-220 | A process-global diagnostics sink that tests swap makes the ledger's start/stop balance depend on which sink was installed when a line fired | `WorkbenchDiagnostics.Sink`, `tests/AiDe.App.Tests/AssemblyInfo.cs` (the default sink) | finding; mechanism not identified |
| (pre-existing) | `coord-core.py doctor` exits 1 on the base: *regeneration 8 artifact(s) OWED* | register-class; not this lane's | reported |

## Terminal ledger

Read after the full App runs (`%TEMP%\aide-tests\terminal-ledger-<pid>.log`; the file is keyed by pid and appended across processes that reuse one):

| Run | Ledger | Starts | Stops | Unmatched |
|---|---|---|---|---|
| Baseline (base `dda140ba`, 09:46 local) | `terminal-ledger-38784.log` | 10 | 9 | `s-terminal` (`SurfaceContentTests`' every-kind loop built a live ConPTY and never disposed it) |
| Full run after Ruling 94, before the hygiene fix (17:20Z) | `terminal-ledger-28608.log` | 10 | 9 | `s-terminal` |
| Final gate run, App (`verify-test-run.py`, 17:31:13–17:33:09Z) | `terminal-ledger-47196.log` | 10 | 9 | `s-terminal` — started 17:33:05Z, **no stop line** |
| The same gate invocation, a second ledger (17:30:00–17:30:03Z) | `terminal-ledger-37460.log` | 10 | 10 | — (`s-terminal` start and stop `killed`) |
| `SurfaceContentTests` alone, after the fix (17:36:56Z) | `terminal-ledger-12408.log` (tail) | 1 | 1 | — (`s-terminal` stop `killed`) |

**Not equal on the final full run, and stated as such.** In isolation the hygiene fix stops `s-terminal` (the `killed` line is there); in the full run the start is written and the stop is not. `TerminalSurface.Dispose` writes its stop synchronously through `WorkbenchDiagnostics.Sink`, a **process-global** the App tests swap 32 times (30 paired with a restore in a `finally`; the other two are a disposable pair in `TerminalSurfaceStopLineTests`). Parallelization is disabled at the collection level, so the mechanism that loses the stop in the full run and not in isolation is **not identified here** — measured, not modeled: **DC-220** — *a process-global diagnostics sink that tests swap makes the ledger's start/stop balance depend on which sink was installed when a line fired, and a missing stop cannot be told from a leaked shell.* Control candidate: the ledger records through its own sink that no test can displace (a second, additive subscriber), so the count is a count. Left for the conductor; the shell it may leak is one PowerShell per full App run, killed with the test host.

## Residual risk

- The Evidence row's detail line is proven headlessly at 480×400 and the shell's 13px body; the contrast of the 12px line on the selected row's accent ground is asserted by design (no own brush), not measured by a census here — the ShellContrastCensus covers text leaves with brushes; a leaf with none inherits the template's ink.
- `RestorableKinds` closes the silent-store path for retired kinds only (sessions c).
- `IsTextTrimmed` was substituted (stated above); the wrap check runs over the laid-out rows only.
- A **process note**: two edits in this lane went through a shell heredoc (a Python program and a script append) before the rule — a multi-line program is a file, then a run — was re-read; the remaining edits are script files in the scratchpad. Recorded so the profiler's count is explained, not disputed.
