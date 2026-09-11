---
id: proof-composer-entry-areas
title: "Proof Pack — the composer's entry areas keep their room, and the page survives a render (INV-0007, phases 1–4)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "conductor-front-door"
tags: [composer, webview2, wpf, layout, handshake, observability, proof-pack, inv-0007, dc-136, dc-137]
links:
  - { to: inv-0007-composer-entry-areas-starved-by-the-compiled-view, rel: implements }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-11
summary: >-
  Evidence for INV-0007 phases 1–4: the compiled view is capped to the smaller of 35% of the
  composer and half of what the chrome leaves (set in MeasureOverride, one pass); both WebView2
  surfaces initialise once through one WebSurfaceHost and the router's readiness is per document;
  bounds, every handshake transition and the first accepted keystroke are emitted on the normal
  path. Nine new tests, four of them through the real docking host; every one observed red first or
  red by a named mutation. Probe exit 24 → 0 (editor 110px/465px → 334px/241px) and 25 → 0.
---

# Proof Pack: the composer's entry areas keep their room (INV-0007, phases 1–4)

- **Change:** branch `fix/composer-entry-areas`, on `investigate/composer-input` @ `4b05744b` (= `main` @ `f5c0f740` + INV-0007 and its two red tests)
- **Investigation:** `docs/investigations/INV-0007-composer-entry-areas-starved-by-the-compiled-view.md`
- **Register:** DC-137 (a content-sized reader docked beside a filling writer), DC-138 (one-time initialisation on a per-attach event, behind a once-gate keyed to the wrong lifetime) — *ids as allocated on this branch; `origin/main` spent DC-137 at `0a0a73c1` while this node ran, so both renumber up by one at the merge (the DC family is renumbered in place, never re-issued — `verify-id-allocators.py` line 89, DC-134's precedent)*
- **Tier:** T1 · **Author / date:** the implementation node (session `composer-fix`), 2026-09-11

## Claims & evidence

| # | Claim | Evidence (observed) | Oracle, and why it can fail | Red observed before green | Confidence | Residual |
|---|---|---|---|---|---|---|
| 1 | In the real docking host under the operator's recorded arrangement, the editor host is never smaller than the compiled view and the compiled view keeps to ≤ 35% | `ComposerHostIntegrationTests.TheComposersEntryAreasKeepTheirRoomAfterTheNewSessionChoreography` — probe `--shell --maximize --height 800`: *editor host=334px, compiled view=241px (35% of 689px), writer >= reader: True, fields=6 editors=3, reached draft=True*, exit 0 | the probe reads `ActualHeight` off the live tree after the product's own New Session choreography and exits 24 when the rule fails; its assertions were not weakened (`git diff HEAD` on the test: 0 lines removed) | **Yes** — on the un-fixed tree: exit 24, *110px of 689px, compiled 465px (67%)* | Verified | window 800px only in this run; the rule is height-independent and the fast-ring test covers 485/1000 |
| 2 | The rule holds in the fast ring at 485px and 1000px with the operator's ~30-line text, **and** with a status line wrapped to five lines | `TheWriterKeepsItsRoomTests.TheEditorHostIsNeverSmallerThanTheCompiledViewAndTheCompiledViewKeepsToItsCeiling` ×3 rows, ~220ms total | Measure/Arrange of the real `ComposerSurface` detached, no browser; asserts editor ≥ compiled, share ≤ 0.35 (the test's own constant, equality-checked against the product's), **and** compiled ≥ min(⌊0.35H⌋, ⌊(editor+compiled)/2⌋) − 1 so a reader collapsed to its `MinHeight` is red too | **Yes** — un-fixed: *editor host 0px, compiled 465px of 485px* and *421px, 465px of 1000px*; wrapped-status row red under the share-only ceiling by mutation: *editor host 138px, compiled 169px of 485px* | Verified | widths fixed at 495px; `H < 380px` not covered (the rule's floor is stated in code) |
| 3 | The layout log agrees with the screen, is written once per first layout, and reports a not-arranged part as `null` never 0 | same test: the `composer.layout` line's `editor.height`/`compiled.height`/`composer.height` equal the tree's within 0.5px; `Assert.True(layout.Count == 1)`; `APartWhoseArrangeIsNotValidIsReportedAsNullNotZero` (`InvalidateArrange` on the compiled box → `compiled.height` is JSON null, `editor.height` a number) | reads the Sink; a stale or transient line would disagree with the tree or make the count 2; `GetDouble` on null throws | **Yes** — the first version of the test went red with *no composer.layout diagnostic was emitted* before the emitter existed | Verified | the null path is driven by `InvalidateArrange`, not by a real mid-layout read |
| 4 | A ready that follows a declared navigation is a mount, not a duplicate; the new page's revisions start from 1; a late change from the old page raises no mark | `TheVocabularyIsClosedTests.AReadyAfterADeclaredNavigationIsAMountNotADuplicate` | pure router; asserts `Accepted`, `sink.Ready == 2`, `sink.Fields == 2`, `Dropped == 2` | **Yes** — against a no-op `BeginNavigation` stub: `Assert.False(router.IsReady)` failed at line 122 | Verified | — |
| 5 | A mounted composer survives a later `Adapter.Render()` — no re-navigation, no dropped ready, fields on screen | `ComposerHostIntegrationTests.TheComposerPageSurvivesALaterRender`: *wpf loaded=2 unloaded=1, navigations started=1 (+0), editor.ready posted=1 (+0), router drops=0, host.init count=1 fields=6*, `re-attached` logged, no `message-dropped`, exactly one `navigation-started` for the composer | the probe re-parents through the real docking host and reads the page; exit 25 when fields vanish | **Yes** — un-fixed: exit 25, *navigations +1, editor.ready +1, router drops=2, host.init count=0 fields=0* | Verified | one render; N renders covered by claim 7 |
| 6 | The graph canvas no longer reloads on a render | probe `--shell --render-after-mount --height 1400`: *canvas sibling: navigations started by the same render=0* | the probe subscribes to the canvas core's `NavigationStarting` across the render | **Yes** — INV-0007 measured *= 1* on the un-fixed tree | Verified | not asserted by an xunit test (the 800px runs have no canvas browser); recorded here |
| 7 | Both WebView2 surfaces initialise exactly once across N re-parents, and the log carries one `initialising` and N `re-attached` with the payload keys present | `TheWebSurfacesInitialiseOnceAcrossReparentsTests.TheComposerInitialisesItsBrowserOnceAcrossReparents` / `…TheCanvas…` (real window, 3 re-parents, ~0.5s each) | `InitialisationsStarted == 1`; the log's `re-attached` count is the non-vacuity (0 if `Loaded` never fired) | **Yes, by mutation** — guard removed: *Expected: 1, Actual: 4*, both surfaces | Verified | — |
| 8 | An attach that lands while the runtime is still starting is a re-attach, not a second start | `AnAttachDuringTheRuntimeStartIsAReattachNotASecondStart` — runtime start held on a `TaskCompletionSource`, `Loaded` raised 4×, initialiser ran 0× until release then 1× | deterministic; no window, no timing | **Yes, by mutation** — flag moved to after the await: *Expected: 1, Actual: 4* | Verified | — |
| 9 | A failed start is reported once, with a stable error code, and not retried on re-attach | `AFailedStartIsReportedOnceAndNotRetriedOnReattach` | one `failed` callback, one `init-failed` line with `errorCode == "WEB.INIT_THREW"` and the exception type, three `re-attached` | seen green only (written against the contract after the host existed; the mutation partner is claim 8) | Verified | the null-core path was removed (unreachable after a non-throwing `EnsureCoreWebView2Async`; disposal during the await returns) |
| 10 | A genuine reload (crash recovery's shape) re-mounts the page with the draft the host holds | `ComposerHostIntegrationTests.AGenuineReloadRemountsThePageWithTheDraft`: *remounted=True, navigation-started 1->2, init-pushed 1->2, editor.ready posted +1, router drops +0, host.init count=1 fields=6*, editor text = the draft including the earlier keystroke | through the product surface and the shipped page; exit 26 otherwise | **Yes, by mutation** — `BeginNavigation` wiring removed: exit 26, *init-pushed 1->1, router drops +1, host.init count=0 fields=0*, log: `message-dropped … editor.ready: this instance already reported ready` | Verified | — |
| 11 | A navigation the policy cancels resets nothing | `ComposerHostIntegrationTests.ACancelledNavigationResetsNothing`: *composer navigation-started 1->1, raw NavigationStarting=2, page ready=True, router drops=0, reached draft=True* | the browser really raised the second `NavigationStarting`; the composer really did not count it; exit 27 otherwise | **Yes, by mutation** — cancel early-return removed: exit 27, *navigation-started 1->2, page ready=False, router drops=1, reached draft=False* | Verified | — |
| 12 | No element outside the host hooks `Loaded` for its own initialisation | `NoElementOutsideTheHostHooksLoadedForItsOwnInitialisation` — root `src/AiDe.App`, `*.cs` + `*.xaml`, regex `\bLoaded\s*\+=|\bLoadedEvent\b|\bLoaded="`, allow-list by relative path (two Windows, the host) | text sweep; an allow-list entry is a claim of idempotence made beside it | **Yes** — at HEAD (`git grep`), `CanvasSurface.cs` and `ComposerSurface.cs` matched outside the allow-list | Verified | token-shaped: a hook via reflection or a XAML behavior would pass |
| 13 | The bounds, every handshake transition and the first accepted keystroke are on the normal path, no flag, no re-run | the probe echoes the Sink; `TheComposersEntryAreasKeepTheirRoom…` asserts `composer.layout` and the transitions `initialising`, `navigation-started`, `configured`, `page-ready`, `init-pushed`, `input-received` on the composer's surface id | each line could only be printed by the product; the probe writes none of them | **Yes** — the assertions were added before the emitters and went red on *no such line* in the first STA run; in the probe they are additive over the INV's red tests | Verified | trace ids are absent file-wide (pre-existing, DC-noted below) |
| 14 | The run-binding guard still holds (one registry, one send context, one attachment gate) | `TheRunBindingComesFromTheProviderFileTests.TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate` in the full App run | source sweep over `src/AiDe.App` | n/a (unchanged control) | Verified | — |

## What each phase changed (E7 surface list, ticked)

| Surface | Reached? | Where |
|---|---|---|
| `ComposerSurface` layout | yes | `MeasureOverride`: `_compiled.MaxHeight = ⌊min(0.35·H, (H − chrome)/2)⌋` with chrome measured from the picker, bar, label, lease, status and the footer margin — set before any child is measured, so one pass and no transient starvation |
| handshake state | yes | `WebSurfaceHost` (once-guard set before the first await; `initialising` / `re-attached` / `init-failed`); `ComposerMessageRouter.BeginNavigation()` (Core); `ComposerSurface.OnNavigationStarting` re-keys readiness only for an allowed navigation |
| `composer.mjs` | **not touched** | the page already posts `editor.ready` on mount; the reset is host-side |
| `CanvasSurface` | yes | same host; navigations measured from the core's `NavigationStarting`; the `simplify:` marker at the old `:211` retired (its upgrade is taken) |
| `WorkbenchDiagnostics` | yes | `ComposerLayout` (evt `composer.layout`), `WebSurfaceHandshake` (evt `web-surface.handshake`, transitions listed in its remarks, `errorCode`/`exceptionType` on failure) |
| readers | yes | the STA tests, the once-tests, the probe (Sink echoed to stdout in every mode — the handshake probe no longer writes into the operator's log, INV F6) |
| register | yes | DC-137, DC-138 with class → sweep → derive → prevent |

**Writer/reader per field (E8):** every `composer.layout` field is written by `EmitLayout` and read by `TheWriterKeepsItsRoomTests`; every handshake transition is written by the host or the composer and read by name in a test (`initialising`, `re-attached`, `init-failed`, `navigation-started`, `configured`, `page-ready`, `init-pushed`, `input-received`, `message-dropped`); `disposed` is written on `Dispose` and read by nothing yet — recorded as such, not claimed.

## Testing Strategy triggers applied

- **T1** (pure router readiness): `AReadyAfterADeclaredNavigationIsAMountNotADuplicate`, red-first.
- **T3** (new class composition): the once-tests go through the real surfaces and a real window; the direct host tests through the host's public shape.
- **T7** (event payloads): `composer.layout` parsed and compared with the tree; a `re-attached` line parsed for `evt`, `surface`, and null `navigations`/`inputs`/`drops`; `init-failed` parsed for `errorCode`/`exceptionType`.
- **T8** (a substitute at a boundary): the host's `ensure` seam replaces only the runtime start, for the one property (guard before await) that cannot be proven otherwise; the window-driven tests use the real runtime.
- **D0:** deterministic (no `Task.Delay` in the fast-ring layout tests; `Settle` is one dispatcher fence at `Background`, ordered after `Loaded` by priority); isolated (Sink saved and restored in `finally`; App tests run with parallelisation disabled); temp dirs deleted.

## Gate table (bare, one exit code each)

| Gate | Result |
|---|---|
| `dotnet build src/AiDe.App -p:TreatWarningsAsErrors=true` | 0 |
| `dotnet build tests/AiDe.App.Tests -p:TreatWarningsAsErrors=true` | 0 |
| `dotnet build tests/AiDe.App.ComposerProbe -p:TreatWarningsAsErrors=true` | 0 |
| `dotnet test tests/AiDe.App.Tests` (full, after the last edit) | 567/0 (1 m 19 s) |
| `dotnet test tests/AiDe.Core.Tests --filter Composer\|Session` | 427/0 |
| `python tools/verify-*.py` (30 scripts) | all 0 except: `verify-id-allocators.py` (cross-branch: DC-137 spent on `origin/main` during this node; INV-0007 allocated by three branches — `--this-tree-only` is 0), `verify-no-conflict-markers.py` (markers in `site/index.html`/`site/model.html` at this branch's base `f5c0f740`, fixed on `main` by `0a0a73c1` — DC-137 on main), `verify-derived-views.py`/`verify-site-figures.py` (regenerated after the audit entry) |
| `verify-test-run.py` (CHECK mode) | 0 |

## Deviations from the INV's plan, recorded

- **Phase 1 "editor `MinHeight`"** — dropped: a `MinHeight` on a fill child of a `DockPanel` cannot reclaim room from `Auto`-docked siblings (it overflows the cell instead). The chrome-aware ceiling in `MeasureOverride` is the stronger rule and is tested at the boundary a `MinHeight` was meant for (the wrapped status line).
- **Phase 1 "Grid with star rows"** — not done: same arithmetic as the `DockPanel` (Auto first, star remainder); the rewrite bought nothing the ceiling does not.
- **Phase 3 status sentence** (*"the editor has not reported ready"*) — deferred, named: not in the approved phase-3 list; `navigation-started` with no `init-pushed` after it is the log's form of the same fact.
- **Phase 4(a) table test over every input-host surface** — composer only: DC-137's sweep rules the others out (`PromptDraftSurface`'s writer is the fill child; the rest dock no content-sized reader); a source-level sweep guard for class A was not written because its token set cannot be stated honestly (GO14a) — a residual, not a control.
- **INV Phase 3 `composer.input` writer** — folded into the handshake vocabulary as `input-received` (Simplifier, L9: the reuse rung).

## Residuals

- **Phases 5–6** (keyboard entry lands on `BODY`; page-side contrast floor) — not started, per the approved scope.
- **Class A recurrence in a new surface** — caught only where a layout test is written for it (DC-137 `partially-controlled`).
- **Trace correlation** — no line in `WorkbenchDiagnostics` carries `trace_id`/`span_id` and no production listener subscribes to `aide.workbench` (pre-existing, file-wide; the SRE's note).
- **`disposed` transition** — written, not yet read by a test.
- **Sub-380px composers** — the rule's floor; below it the compiled view's `MinHeight` (90px) wins over the ceiling by WPF's min/max resolution.
- **Two shared test helpers** (a `Descendants<T>` walker with a logical option; a `Repo.Root()`/`Repo.SourceFiles()`) — the Simplifier's sweep across eight `RepoRoot()` copies and four walkers; next step, not this change.
- **DC ids** — DC-137/DC-138 here renumber up by one at the merge; the register's counts line merges to `controlled 68 · partially-controlled 54 · uncontrolled 16`.
