---
id: proof-shell-seams-x3
title: "Proof Pack — X-3, the Shell-lane seam requests (CV-1/CV-2) and two status-bar defects"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-cd"
tags: [proof-pack, shell-lane, x-3, addendum-c, addendum-d, seam-request, announcer, workbench, composer, ruling-85, ruling-86]
links:
  - { to: coordination-addendum-cd, rel: implements }
  - { to: proof-composer-as-conversation, rel: refines }
  - { to: proof-mechanical-compile, rel: refines }
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-13
summary: >-
  Evidence for X-3 (Shell lane, conductor `conductor/addendum-c`): the six CV-1/CV-2 seam requests
  (the shared Announcer into the session document; the F6/Shift+F6 registry rows; WebSurfaceHost.Retry();
  WorkbenchDiagnostics as the one writer; BorderStrongBrush and its page role; the Compilation/Resources
  EmbeddedResource glob) and two operator-reported status-bar defects (the doubled "rev rev-1" label,
  resolved per Ruling 85 as an observed-HEAD attach rather than a fixture literal; a pane-move refusal
  that never cleared, resolved per Ruling 86 as a bounded self-clearing dwell on the announcer) — each
  red-first, with the reds named and the gate table at close.
---

# Proof Pack: X-3 — Shell-lane seam requests and two status-bar defects

- **Tier:** T1 (T0 for items 7–8 per Rulings 85/86) · **Fan-out cap:** 2 (reviews read-only) ·
  **Session:** `x-3` · **Author:** `claude-x-3`.
- **Branch:** `side/x3-shell-seams` from `main` `6d3e281a`.
- **Sources:** `docs/proof/composer-as-conversation.md` §Seam requests; `docs/proof/mechanical-compile.md`
  §Seam requests; `docs/notes/addendum-c-council-rulings.md` Rulings 85–86.

## Items — one row each (E7: writer / compute reader named where the surface has one)

| # | Item | Red → Green | Files |
|---|---|---|---|
| 1 | The shell's `Announcer` passed as `SessionDocumentSurface`'s third constructor argument (one announcer across hosts, ADR-0031) | Red: `OpeningTheConsoleSplit_AnnouncesThroughTheShellsAnnouncer_NotAPrivateLiveRegion` failed against the un-fixed call (`shell.Announcer.Last` stayed empty — the document's own private live region absorbed the announcement). Green: after passing `Announcer` at the construction site. | `WorkbenchShell.cs:3165` (writer), `SessionDocumentSurface.cs` (doc comment updated); test: `tests/AiDe.App.Tests/Sessions/TheShellsAnnouncerReachesTheSessionDocumentTests.cs` |
| 2 | `session.cycleRegion` (F6) / `session.cycleRegionBack` (Shift+F6) registry rows, `Admits("session-document")`, routed through `WorkbenchController.SessionRegionCycle` (keyed by the focused surface id) to the focused document's `CycleRegion(±1)` — the document's own key handler stays the fallback (DS-1 P4) | Red: `ExecutingSessionCycleRegion_MovesTheFocusedDocumentForwardOneRegion` / `…Back…` failed with `Assert.True(shell.Execute(...))` returning false (unknown command) before the catalog rows and the controller wiring existed. Green: after both landed. `TheRenderedMenuEqualsTheSpecsLiteralTable_ForEveryPerspective` (existing) went red once the rows appeared in Coding's `_View` menu and was updated to the new literal table (a legitimate table update, not a weakening — the row is real and reachable). `F6_CyclesHeaderThreadComposer_TheSplitOnlyWhenOpen` (existing, drives `CycleRegion` directly) still passes unchanged. **Second red, from the Test Architect review**: the wiring's own synthetic "no session document" refusal (`WorkbenchShell.WireSessionRegionCycle`'s closure, the branch with no open document) was constructed and handed back to the controller, which discards every result on the assumption that `SessionDocumentSurface` always speaks its own refusal — true of a refusal `CycleRegion` itself produces, false of this one, which nothing ever announced (DC-011/SC 4.1.3, a real silent refusal). `ExecutingSessionCycleRegion_WithNoSessionFocused_AnnouncesWhy` failed (`Last` empty) before the closure announced its own refusal; green after. | `WorkbenchCommands.cs` (catalog rows), `WorkbenchController.cs` (`SessionRegionCycle` property + `CycleSessionRegion`), `WorkbenchShell.cs` (`WireSessionRegionCycle`, now announces its own refusal); tests: `tests/AiDe.App.Tests/Sessions/TheRegionCycleCommandsReachTheFocusedDocumentTests.cs` (+1), `tests/AiDe.App.Tests/Workbench/PerspectiveMenuTests.cs` (table updated) |
| 3 | `WebSurfaceHost.Retry()`: re-runs initialisation once after `init-failed`, distinct from the declined re-attach; the composer's `editorerror` Retry button (mockup `session-conversation.html`) wired to it via a named `OnHostInitFailed`/`RetryEditorAsync` pair (≤ 30 lines) | Red: `RetryAfterAFailedStartRunsInitialisationAgain` failed (`Retry` did not exist) before the method landed; `AnInitFailure_ShowsRetryAndNamesTheCauseInStatus` / `ClickingRetry_HidesTheButtonAndClearsTheStatusBeforeRetrying` failed before the button existed (`_retry` field absent). Green after both. A re-attach is still declined (`AFailedStartIsReportedOnceAndNotRetriedOnReattach`, unchanged, still passes). | `WebSurfaceHost.cs` (`Retry`, `_lastAttemptFailed`), `ComposerSurface.cs` (`_retry` button, `OnHostInitFailed`, `RetryEditorAsync`); tests: `tests/AiDe.App.Tests/TheWebSurfacesInitialiseOnceAcrossReparentsTests.cs` (+2), `tests/AiDe.App.Tests/Composer/TheEditorErrorRetryButtonTests.cs` (new) |
| 4 | `WorkbenchDiagnostics.Write` made `internal`; `ThreadDiagnostics` calls it directly instead of duplicating the sink/file-path body (DM7) | Red: `WorkbenchDiagnosticsWrite_IsInternal_…` and `ThreadDiagnostics_CarriesNoPrivateWriterOfItsOwn` fail on the un-fixed tree (`Write` was `private`; `ThreadDiagnostics` carried its own `Write`/`Gate`). Green after the modifier change and the rewrite. Every existing `thread.*`-record test (`TheThreadAnnouncesAndExposesRealPropertiesTests`, `TheThreadIsAFeedOfTurnsTests`, `TheThreadIsChatLikeTests`) still passes — the sink-driven behaviour is unchanged. | `WorkbenchDiagnostics.cs`, `ThreadDiagnostics.cs`; test: `tests/AiDe.App.Tests/ThreadDiagnosticsHasOneWriterTests.cs` |
| 5 | `BorderStrongBrush` (`#7C8896`, DESIGN.md `colors.border-strong`) declared in `App.xaml`; the page role `--border-strong ← BorderStrongBrush` added to `ComposerPageTheme.Roles` (14 roles) | Red: `TheRoleTableHasExactlyThePinnedRoles` (13) failed once the role was added before the count was updated; `EveryRoleResolvesFromTheTokenDictionary_ToASixDigitHex` would have failed had the brush not been declared (would report a role naming a token `App.xaml` does not declare). Green with both landed together. `ShellContrastCensusTests` stays 0-below-floor: `composer.html`'s `.editor` rule still reads `var(--border, …)`, not the new role, so no new rendered pairing exists for the census to measure yet (**finding**, below). | `App.xaml`, `ComposerPageTheme.cs`; test: `tests/AiDe.App.Tests/Composer/ComposerPageThemeTests.cs` (count updated 13→14) |
| 6 | `AiDe.Core.csproj`: `<EmbeddedResource Include="Compilation\Resources\*" />` landed; `CompileContract.cs`'s `HostHeader`/`Template` constants **not** moved | n/a — a build-configuration change (glob matches nothing yet) plus a documentation-only decision recorded in `CompileContract.cs`'s remarks; not a behaviour claim. Verified: `AiDe.Core` builds clean with the glob present (see gate table). | `AiDe.Core.csproj`, `CompileContract.cs` (remarks) |
| 7 | Ruling 85 — `AttachWorkspace`'s `artifactRevision` default changed from the fixture literal `"rev-1"` to `null`, resolved to the workspace's observed `git rev-parse --short HEAD` (`GitFacts.Head`) or the literal `"not recorded"`; `EvidencePaneViewModel.cs`'s `rev {SourceRevision}` format is unchanged (the doubling was entirely the upstream fixture value, not the format) | Red: `ANonGitWorkspace_AttachesTheHonestNotRecorded_NeverTheOldFixture`, `AGitWorkspace_AttachesTheObservedHead_NotAFixtureLiteral`, `AGitWorktree_ReportsItsObservedHead_NotAFixtureLiteral`, `ANonRepository_ReportsNoHead_NeverAGuess` all fail on the un-fixed tree (`GitFacts` had no `Head`; `AttachWorkspace` always resolved to `"rev-1"`). Green after `GitFacts.Head` + the `??=` resolution landed. `ACallerSuppliedRevision_PassesThroughUnchanged` proves a caller that still passes a revision (every test elsewhere) is unaffected. | `WorkbenchShell.cs` (`GitFacts.Head`, `ResolveGitFacts`, `RevisionNotRecorded`, `AttachWorkspace`); tests: `tests/AiDe.App.Tests/SessionIdentityReportsTheRealWorktreeTests.cs` (+2), `tests/AiDe.App.Tests/TheAttachedRevisionIsObservedNotAFixtureTests.cs` (new) |
| 8 | Ruling 86 — a status announcement (refusal included) now clears on the next announcement (unchanged: overwrite), on the next applied layout operation (unchanged: `ApplyAndAnnounce` always announces), or after a bounded dwell (new: 10 s default, `WorkbenchAnnouncer`'s own `DispatcherTimer`, restarted per announcement, stopped by `Clear()`) | Red: `ARefusal_ClearsItselfAfterTheBoundedDwell` fails on the un-fixed `WorkbenchAnnouncer` (no dwell mechanism; the strip holds the refusal forever, matching the reported 09:27–09:33 session). Green after the dwell timer landed. `ManualClear_StillEmptiesTheLineImmediately` proves `workbench.clearStatus`'s behaviour is unchanged; `TheDwell_NeverShortensTheAnnouncedText` proves the dwell never truncates the spoken text; `ASupersedingAnnouncement_ClearsTheRefusalImmediately_BeforeTheDwellElapses` proves overwrite still wins first. | `WorkbenchAnnouncer.cs` (`DefaultDwell`, the internal test-dwell constructor, `_dwellTimer`); test: `tests/AiDe.App.Tests/TheStatusStripClearsOnSupersessionOrDwellTests.cs` (new) |

## Findings for the conductor (not this slice's scope)

1. **Item 5's page wiring.** `src/AiDe.App/Web/composer.html`'s `.editor` rule reads
   `var(--border, #2A313B)`; DESIGN.md's PS-C4 intends the editor's boundary to be
   `--border-strong` (a control-at-rest boundary, not a separator). The role now exists and is
   pushed on `host.init`; the stylesheet has not been repointed. Left out of this slice because it
   is a change to the composer page beyond the Retry button (the brief's explicit fence). No
   contrast-floor regression: the census measures only pairings the page actually renders, and
   this one is not rendered yet.
2. **Item 5's host surface.** The Owner asked which Coding-layout surface hosts
   `EvidencePaneViewModel`'s status (Ruling 85's confidence gap). It is the Evidence master pane
   (`SurfaceContentFactory.EvidenceMasterContent`, kind `"view"`), whose kind row admits only the
   Architecture perspective (`SurfaceContentFactory.Kinds`, `PerspectiveMenuTests.TheAllowListsEqualTheSpecsTable`) —
   Coding's default layout does not include it. So the doubled label was visible whenever an
   operator opened the Evidence pane from Architecture, not from Coding directly.
3. **Item 6's glob.** Matches no file today (no `Compilation/Resources/` directory exists). The
   move itself is deferred to whichever trigger in `CompileContract.cs`'s remarks fires first (a
   template over ~200 lines, or a second family profile).
4. **`WebSurfaceHost.Retry()`'s re-run is not idempotent if `_ensure` succeeds but `_initialise`
   itself throws** (WPF lens, F1): `InitialiseAsync` subscribes several event handlers and
   registers a script every call; a retry that reaches that point twice would double-subscribe.
   The reported defect (WebView2 runtime missing, `_ensure` fails) never reaches this branch, so it
   is latent, not reproducing, and out of this slice's fence (a change to `ComposerSurface`'s
   `InitialiseAsync`, not the seam requested). A future retry surface that can fail mid-`_initialise`
   should give `InitialiseAsync` its own once-guard or split it into idempotent phases.
5. **A silent moment between "Retrying…" and the outcome** (WPF lens, F4): `RetryEditorAsync`
   clears the status to empty before awaiting `Retry()`, which is briefly silent against this
   file's own "never silent" rule (SC6) if the retry takes any real time. Not fixed here — the
   in-scope failure mode (browser missing) fails or succeeds effectively synchronously in the
   host's own guard, so there is nothing to narrate yet; noted for whoever wires the retry to a
   host that can actually be slow.

## Gate table (run at close)

| Gate | Result |
|---|---|
| `dotnet build src/AiDe.Core/AiDe.Core.csproj -p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors |
| `dotnet build src/AiDe.App/AiDe.App.csproj -p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors |
| `dotnet build tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj -p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors |
| `dotnet build tests/AiDe.App.Tests/AiDe.App.Tests.csproj -p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors |
| `dotnet test tests/AiDe.Core.Tests` | 2495 passed, 0 failed, 1 skipped (pre-existing symlink-dependent skip), 2496 total |
| `dotnet test tests/AiDe.App.Tests` | 844 passed, 0 failed, 844 total (840 before the review fold-in added 4) |
| `python tools/run-verify-gates.py` | 35 of 35 gates green (final run, after both merges of `origin/main` and the review fold-in) |
| `python tools/regenerate-derived.py` | every derived view current, all its own sub-gates green |

## New defect classes (placeholders — conductor allocates)

- **DC-179** — a fixture-sized constructor default reaches the one real production call site
  because the real caller passes none (Ruling 85's shape).
- **DC-180** — a status announcement's only clearing path is a manual command, so a silent
  success path leaves an old refusal on screen indefinitely (Ruling 86's shape).
- **DC-181** — a same-assembly diagnostics helper duplicates a sibling's sink/file-path logic
  because the sibling's writer defaults to `private` (DM7's generalisation).

Full text: `docs/lessons/defect-classes.md` (appended, not claimed, per the brief's lease rule).

## Reviews (read-only, per the brief's fan-out cap of 2) — findings folded in before close

- **Test Architect** — reviewed the registry-row test and the two status-bar reds.
  - **BLOCKER, fixed:** `WorkbenchController.CycleSessionRegion` discards the `CanvasFocusResult`
    from `SessionRegionCycle`, relying on the comment "already speaks a landed move or a refusal" —
    true only when the closure resolves a real `SessionDocumentSurface` (which self-announces).
    `WorkbenchShell.WireSessionRegionCycle`'s own synthetic "no session document" refusal announced
    nothing, and the existing `ExecutingSessionCycleRegion_WithNoSessionFocused_IsHandledNotUnknown`
    test only asserted the command returned `true`, never checking the announcer — it could not
    have caught a permanently silent refusal. **Fix:** the closure now announces its own refusal
    before returning it (`WorkbenchShell.cs`); `CycleRegion`'s own refusal path is unchanged and
    still self-announces exactly once (no double-speaking). Red confirmed by reverting the fix and
    re-running `ExecutingSessionCycleRegion_WithNoSessionFocused_AnnouncesWhy` (failed: `Last` was
    `""`, expected `"No session document is focused."`); green after restoring the fix.
  - **MAJOR, fixed:** `GitFacts.Head` (`git rev-parse --short HEAD`) and `GitFacts.Branch`
    (`git rev-parse --abbrev-ref HEAD`) are two adjacent, swappable `Git(...)` calls; every existing
    assertion on `Head` only checked non-empty and `!= "rev-1"` — a branch name satisfies both, so a
    swap would not have been caught. **Fix:** `AGitWorktree_ReportsItsObservedHead_NotAFixtureLiteral`
    now asserts `Head` matches `^[0-9a-f]{4,40}$`, differs from `Branch`, and equals an independent
    `git rev-parse --short HEAD` invocation run directly by the test (not through the method under
    test).
  - Confirmed trustworthy as shipped: `TheStatusStripClearsOnSupersessionOrDwellTests` (drives a
    real `DispatcherTimer` through a real pumped `Dispatcher`, short injected dwells, not a manual
    `Clear()` call) and the happy-path halves of the registry-row test.
- **WPF lens (`BorderStrongBrush`/`Retry()` focus)** — no blocker, no major.
  - **MINOR, fixed:** `WebSurfaceHost.Retry()` left `_lastAttemptFailed` true until the retry's
    `await` resolved, so a second `Retry()` call arriving mid-flight would start a second, concurrent
    initialisation. Fixed by clearing the flag before the `await` rather than after.
  - **MINOR, fixed (test gap):** `ComposerPageThemeTests`'s pinned-value theory
    (`ARoleCarriesItsTokensValue_NotAValueOfItsOwn`) covered only the original INV-0008 eleven roles
    — `--inferred`, `--verified` and the new `--border-strong` were absent, so a mapping drift on
    any of the three (e.g. `--border-strong` pointed back at `BorderBrush`) would still pass every
    other test. Extended with all three rather than only the one this slice added (CI2's
    class-not-instance rule).
  - **MINOR, fixed:** a code comment claimed the Retry button sits "in the mockup's editorerror
    send row"; the mockup actually replaces the editor with an error box and moves focus to Retry.
    Corrected the comment to state the deliberate deviation (a windowed WebView2 makes an overlay
    where the mockup draws it an airspace hazard, ADR-0015) rather than imply fidelity.
  - Confirmed trustworthy: `BorderStrongBrush`'s hex matches `DESIGN.md:23` exactly; its App.xaml
    placement has no resource-ordering hazard; `Retry()`'s re-open of `_initialised` is safe per
    the pinned WebView2 package's own documented `EnsureCoreWebView2Async` contract (a second call
    after failure re-initialises; after success it is a no-op).
  - Not fixed, recorded as findings above (4, 5): `InitialiseAsync`'s non-idempotence on a
    (currently unreached) partial-failure branch; a brief silent gap during `RetryEditorAsync`.
