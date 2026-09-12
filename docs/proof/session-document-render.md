---
id: proof-session-document-render
title: "Proof Pack - A Session Document Is Shown Where The Operator Is (INV-0009 Phases 1-4)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "conductor-front-door"
tags: [session-document, composer, explorer-mode, shell-view-mode, layout-restore, workspace-chooser, observability, dc-148, dc-149, dc-040, dc-084, proof-pack]
links:
  - { to: inv-0009-a-session-document-opened-into-a-body-that-is-not-on-screen, rel: implements }
  - { to: adr-0017-primary-view-mode, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Proof Pack for INV-0009 phases 1, 2, 2b, 3 and 4 on fix/session-document-render: a dock document
  opens into a body that is on screen (one shell seam, the mode in the log); a reopened session and a
  restored session document are shown with their composers bound through one binder; the workspace
  chooser opens the chosen workspace before it creates; the maximize and the binding are logged.
  Four probe oracles red (exit 30, 32, 34, the chooser refusal) then green; App 635/0, Core 2240/0.
---

# Proof Pack — A Session Document Is Shown Where The Operator Is (INV-0009 Phases 1–4)

- **Change:** branch `fix/session-document-render` — `228339d6` (Phase 1), `d4d39cff` (Phase 2),
  `45c713e5` (Phase 2b), `d2409515` (Phase 3), `58c991ca` (Phase 4), on top of the merge of `main`
  `f92aa810` (`4c19b497`). Not merged to `main`.
- **Spec / design:** `docs/investigations/INV-0009-…md` §9 (F1–F5) and §11 (the phases); the
  operator's ruling on F4: *the chooser opens the chosen workspace and then creates*. ADR-0017
  (retain, never rebuild) holds unchanged.
- **Tier:** T1
- **Author / date:** the INV-0009 implementation node (session `render-fix`), 2026-09-11/12.

## Claims & evidence

### Claim 1: A session created while Explorer is the body is shown — and so is every dock document a catalog command opens (DC-148)
- **Evidence:** `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` (probe `--session-render
  --prior-document --explorer --sibling`): *after New Session: mode=Workbench
  last-mode-trigger=document-opening workbench root loaded=True visible=True, composer … isLoaded=True
  isVisible=True size=453x609, transitions initialising=1 … configured=1 init-pushed=1, layout-lines=1,
  page fields=6*; *sibling (code viewer, same state): in-layout=True … isLoaded=True isVisible=True*.
- **Oracle (and why it's trustworthy):** the replay is the product's docking host under the product's
  `ShellModeController`, from the operator's restored arrangement (the log's payload verbatim; exit 33
  if the shell does not reach it). WPF's own `Loaded` on the composer and the product's own handshake
  lines are the measurement; the verdict fails on `loaded=0 || initialising=0`. The structural half,
  `EveryOpeningCommandPassesThroughTheSeamTests`, scans `WorkbenchShell.cs` (root that file only, no
  recursion, token set `new LayoutOperation.AddSurface(` / `OpeningDocument();`, allowlist empty) and
  asserts the window's and the replay's wiring by one token — the replay cannot pass on a line the
  product lacks (DC-135).
- **Red observed before green:** yes — exit 30 on `main` `1aadde84` (INV-0009 §4 B1) and on the
  merged `4c19b497` at 23:37Z: *"the new session's composer never entered a rendered visual tree: WPF
  raised no Loaded on it and its browser host never initialised"*. Green on `228339d6`.
- **Confidence:** Verified (the replay); Inferred for the real `MainWindow` — its wiring line is
  asserted by the scan, not executed (the window shows modal sheets and cannot be driven headless).
- **Residual risk:** `workspace-open` (the restore) is not an opening command and does not raise the
  seam; a restore performed while Explorer is the body still replaces the layout off screen. The
  INV's sweep did not list it; noted as a next step, not a phase.

### Claim 2: New Session inside Explorer leaves it *because a document opened*, and the mode is in the log
- **Evidence:** `ANewSessionCreatedInsideExplorerLeavesItBecauseADocumentOpened`: *explorer (22:34:00Z
  replay): mode=Explorer explorer-graph initialising=1*, then *after New Session: mode=Workbench
  last-mode-trigger=document-opening*; the explicit return-to-workbench step changes neither the
  trigger nor the composer's `Loaded` count. `Set_ToADifferentMode_WritesOneShellModeLineNamingTheTrigger`
  / `Set_ToTheCurrentMode_WritesNothing` prove the `shell.mode` line (`mode`, `from`, `trigger`) once
  per change and never on a no-op.
- **Oracle:** the trigger is read from the product's `shell.mode` line, not from the probe's own
  bookkeeping; the no-op case proves the seam firing on every open in the workbench writes nothing.
- **Red observed before green:** this oracle **replaces** the INV's necessity half
  (`LeavingExplorerShowsTheSessionCreatedInsideIt`), which asserted the pre-fix state by construction
  (*mode=Explorer … composer wpf loaded=0 … configured=1 init-pushed=0*) and went red on Phase 1 at
  23:41Z — the INV's plan listed it as "stays green", which was not achievable once the state it
  asserts became unreachable. The `shell.mode` tests were written against the emitter and are
  Verified Green, not red-first (noted honestly: their discriminating power rests on the JSON
  assertions, which a deleted emitter fails). The seam scan's discriminating power was measured by
  mutation at 00:17Z: with the prompt draft's `OpeningDocument();` deleted,
  `EveryAddSurfaceInTheShell_IsPrecededByTheSeamWithinItsCommand` failed with *"the opening command at
  offset 18133 adds a surface at offset 18794 without raising DocumentOpening first"*; restored, green.
- **Confidence:** Verified.
- **Residual risk:** the composer's WPF `Loaded` fires twice on the New Session path in the replay
  (`loaded=2 unloaded=0`, both in and out of Explorer) — DC-138's once-gate holds (`initialising=1`,
  one navigation); the double `Loaded` is WPF re-parenting on the second render (the maximize) and
  is not asserted on. Not a regression of this change (the INV's B2 run measured `loaded=2` too).

### Claim 3: A reopened session is shown as a live document and its composer is bound (DC-040 rec. 2, DC-084 rec. 2)
- **Evidence:** `AReopenedSessionIsShownAndItsComposerIsBound` (probe `--session-render --reopen`):
  *reopen: pane content=Border live-document-in-view=True renders='Compiled view'*; *reopen: composer
  initialising=1 page-ready=1 configured=1 init-pushed=1 layout-lines=1 page fields=6 host fields=6*;
  `session-document.bound … "repositoryRoot":"<the run's root>"` on stdout.
- **Oracle:** the pane's content is read from AvalonDock's own tree (`SurfaceContent<SessionDocumentSurface>`),
  the binding from the product's `SessionComposerBinder` over a provider file written into the run's
  own directory (never the machine's `~/.aide/providers.json`), the fields counted in the page. The
  one-construction-site guard (`TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate`) now
  asserts exactly one send context, one attach gate and one `Configure` in the binder, **none** in
  `MainWindow`, and none elsewhere in `src/AiDe.App` — the site moved and did not multiply.
- **Red observed before green:** yes — exit 32 on `4c19b497` at 23:38Z: *"the reopened session was not
  shown as a bound document: live document in the view=False (the pane renders 'No session is
  open…'), composer configured=0"*. Green on `d4d39cff`.
- **Confidence:** Verified for the shell and the binder; the window's call — `ReopenSessionAsync`
  opens, then `BindOnRecord` — is asserted by the source scan
  `TheWindow_RevivesThenRendersThenBinds_AndReopensBound_AndOpensTheChosenWorkspace`, whose
  discriminating power was measured by mutation at 00:17Z (the reopen's bind deleted →
  *"'BindOnRecord(config,' does not follow the previous token"*; restored → green). A bound composer
  is not bound again (`Bind_OfAComposerAlreadyBound_ChangesNothingAndSaysSo`, red at 00:12Z: the
  second call re-bound — a second `Configure` after the page mounted would re-mint the fields and
  push a second `host.init`).
- **Residual risk:** the reopen path binds with **no task class** (`SessionConfig` carries none on
  this branch); `ComposerSendContext.TaskClass` is nullable (Ruling 70's constraint) and `Send` refuses
  by name — *choose a task class for this prompt* — so a reopened session cannot start a run until
  the per-prompt class (Ruling 72, D2's lane) lands. That is the honest state, not a default
  (DC-110). The reopen does not stamp an origin — it never touches the sheet.

### Claim 4: A send with no task class is refused by name, never defaulted
- **Evidence:** `ASendWithNoTaskClassIsRefusedByNameTests` — null and blank refused with *task class*
  in the message and `SendCount == 0`; the companion with `"investigate"` sends and the request
  carries it.
- **Oracle:** the refusal is a member of the outcome, the companion proves the class is the cause.
- **Red observed before green:** yes — the test did not compile against the non-nullable context
  (CS8604 at 23:46Z); after the type change the refusal test is what a deleted guard fails
  (`GovernedRunRequest` would then carry a null class).
- **Confidence:** Verified.
- **Residual risk:** `GovernedRunRequest.TaskClass` remains non-null by declaration only; the gate is
  the one guard.

### Claim 5: At workspace-open, a restored session document is revived and bound; a session that is gone keeps its island; the restore's active tab stays active (Phase 2b)
- **Evidence:** `ARestoredSessionDocumentIsRevivedAndBoundAtWorkspaceOpen` (probe `--session-render
  --bind-on-restore`): *revived=[20260911T175821Z-1edfa710]*, *active document live=True
  renders='Compiled view' active in view=session-document:…1edfa710; gone document live=False
  renders='No session is open…'*, *composer … configured=1 init-pushed=1 … page fields=6*, and the
  `session-document.bound` line for the session.
- **Oracle:** the restored arrangement holds two session-document surfaces; one has a `session.json`
  in the workspace, one does not. The verdict fails on any of: the active one not live, the gone one
  live, `configured=0`, `init-pushed=0`, fields ≠ 6.
- **Red observed before green:** yes, on the binding half — the revive without the bind at 23:53Z:
  exit 34, *active document live=True renders='Compiled view' … configured=0 init-pushed=0 page
  fields=0* (finding C's blank editor, produced deliberately). Before the revive existed, every run
  printed *restored active document: live-document=False renders='No session is open…'*.
- **Confidence:** Verified (the shell's revive and the binder; `RestoredSessionDocumentsAreRevivedTests`
  covers loads / missing / malformed `{` / reads-as-`null` — the last **red at 00:13Z**: the store's
  *"deserialized to null"* `InvalidOperationException` escaped the revive's catch and would have
  thrown out of `AttachWorkspace` at launch; the catch now names it, on the reopen path too).
  `MainWindow.AttachWorkspace`'s order (one `Shell.AttachWorkspace(` site → revive → render →
  `BindOnRecord`) is asserted by the window scan above; `TheMaximizeIsOnTheCreatePathAndNotOnTheReopenPath`
  asserts no maximize on it.
- **Residual risk:** `AttachWorkspace` renders once inside the shell (islands) and once more after
  the revive (live documents) — two renders at workspace open where there was one, and
  `OpenWorkspaceAtAsync` still carries its pre-existing third; not measured as a cost (the
  Simplifier's finding, left for a separate change). A `session.json` that is unreadable keeps the
  island silently (the same conclusion a reopen of it reaches, with an announcement there and none
  here).

### Claim 6: New Session with no workspace open: the chooser's workspace is opened, then the session is created and bound in it; a cancelled chooser creates nothing (DC-149, the Owner's ruling)
- **Evidence:** `TheChooserOpensTheChosenWorkspace_ThenTheSheetBindsToWhatTheWindowReports` (order
  `[choose, open:<root>, sheet:<root>]`, the created config's workspace is the opened one);
  `…_AndAFailedOpenCreatesNothing` (the open path's own sentence, no sheet, no session directory);
  `WithNoWorkspaceTheChooserInterposes_AndCancelAbortsCleanly` (nothing opened);
  `ASessionCreatedThroughTheChooserIsBoundToTheChosenWorkspace` (probe `--session-render --chooser`):
  *chooser: order=[choose,open,sheet:window-root,opened] created=True*, *composer configured=1
  init-pushed=1 page fields=6 status='' bound-to-chosen-root=True*, *chooser cancelled: order=[choose]
  created=False surfaces before=9 after=9 sessions before=1 after=1*.
- **Oracle:** `bound-to-chosen-root` is read from the product's `session-document.bound` line's
  `repositoryRoot` against the chosen directory; the flow is the product's `NewSessionFlow`. The
  window's open path is stood in by a delegate that sets what the window reports (D7: the real path
  reaches a daemon; the stand-in is the one seam the flow depends on, and the real path is exercised
  by the existing workspace-open coverage). The stand-in reports the root in **its own form**
  (upper-cased — the same directory) so the sheet's root is checked against the window's, not the
  chooser's: a flow that bound the sheet to the chosen string (`root = chosen`) reads `sheet:other-root`
  / `sheet:<chooser's form>` and fails — the Test Architect's tautology finding, closed. The window's
  side (`openWorkspace: OpenWorkspaceOrSayWhyAsync` → `await OpenWorkspaceAtAsync(folder)` → `Queries`
  is the fact) is asserted by the window scan.
- **Red observed before green:** the flow oracles did not compile against the flow with no open
  step (CS1739/CS1061 at 23:58Z); the product state they replace is the refusal the
  `--prior-document` step replays and the control oracle asserts (*status='repositoryRoot: this
  window has no open workspace…'*). The probe-level chooser oracle was written after the flow
  change; its first run found a probe defect (the second half ran with the workspace still open —
  exit 35), fixed in the probe.
- **Confidence:** Verified (the flow, the binding, the ordering); Inferred that `OpenWorkspaceAtAsync`
  leaves `Queries` non-null exactly when the workspace opened (read from its code, not executed).
- **Residual risk:** `NewSessionRequested` became a task command (`RunAndAnnounce`), so the
  announcement lands after the sheet closes rather than synchronously; the announcer is the same.
  The origin stamp is unchanged (the sheet creates on the command path) — asserted on
  `feature/exit-evidence`'s guard, not here.

### Claim 7: The maximize and the binding are in the log on the normal path (Phase 4)
- **Evidence:** `TheMaximizeWritesItsOwnMutationLine`: two `layout.mutation` lines, `operation:
  maximize-stack`, `placement: maximized` then `refused`, the `stack` id present in the tree after and
  holding the surface. `TheBinderRecordsWhatItBoundTests`: the bound line carries the session, the
  composer's surface and the repository root; the refused line carries the field (`repositoryRoot`,
  `engineId`) and the reason; each case has none of the other kind.
- **Oracle:** the lines are parsed as JSON; a missing field fails on `GetProperty`; `stack` is
  `null` (recorded as null, never a guess) on every other mutation.
- **Red observed before green:** yes — the maximize test expected 2 lines and found 0 (00:05Z); the
  three binder tests each found an empty collection with the emitters removed (00:08Z), then green
  with them restored (the binder file's diff against `d4d39cff` is empty).
- **Confidence:** Verified.
- **Residual risk:** the `active` field on the maximize line is null (the placement has no adapter);
  the `open-*` lines do not carry `hostLoaded/hostVisible` (INV F5's fourth item) — the `shell.mode`
  line answers the same question and the seam makes the state unreachable.

## Test coverage of the boundary set

| Boundary | Covered by |
|---|---|
| Explorer is the body when a document opens (the instance) | `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` |
| The workbench is already the body (no-op) | `Set_ToTheCurrentMode_WritesNothing`; every workbench-mode oracle |
| A sibling command (code viewer) in the same state | the `--sibling` line of the same oracle |
| A refusal before the add (no profile / no pane) does not swap the body | by construction (the seam is after the guards); the scan asserts the seam precedes the add, not the guard — **Flagged**, no test |
| Restored surface holds the island; reopen | `AReopenedSessionIsShownAndItsComposerIsBound` |
| Restored surface, `session.json` gone / malformed / reads as `null` | `RestoredSessionDocumentsAreRevivedTests` (three) + the *gone document* half of the probe oracle |
| A live document is never invalidated (ADR-0017) | `Invalidate` is inside the branch where no document is registered — **Flagged**, no direct test; `SessionDisposalLedger` tests hold |
| A bound composer is not bound again | `Bind_OfAComposerAlreadyBound_ChangesNothingAndSaysSo` |
| No workspace / no provider file / two routable backends / ambiguous account / no document open | `TheBinderRecordsWhatItBoundTests` (six) |
| Malformed provider file on reopen / restore | `Refuse_FromTheWindow_LandsOnTheComposerAndInTheLog` + the window scan's `BindOnRecord` clause |
| No task class at send / blank / present | `ASendWithNoTaskClassIsRefusedByNameTests` |
| Chooser cancelled / open failed / opened / opened-but-window-reports-none | `TheNewSessionSheetTests` (four) + the probe's cancelled half |
| Second maximize refused | `TheMaximizeWritesItsOwnMutationLine` |
| The window's wiring for 2, 2b and 3 | `TheWindow_RevivesThenRendersThenBinds_AndReopensBound_AndOpensTheChosenWorkspace` (mutation-checked) |

## Change reach & instrumentation

### Operator questions instrumented (IO2 / IO10)

| Operator question | Emitting source | Observed once? |
|---|---|---|
| Which body was on screen when I ran the command? | `shell.mode` (`mode`, `from`, `trigger`) | yes — the replay prints `last-mode-trigger=document-opening` |
| Did the new session's pane take the tree, and which stack? | `layout.mutation` `operation: maximize-stack`, `stack`, `placement: maximized/refused` | yes — `TheMaximizeWritesItsOwnMutationLine` |
| What was the composer bound to? | `session-document.bound` (`session`, `surface`, `repositoryRoot`) | yes — reopen, restore and chooser replays |
| Why was it not bound? | `session-document.refused` (`field`, `reason`) | yes — `TheBinderRecordsWhatItBoundTests` |
| How long did the open take? | not recorded — named gap (IO4); the handshake timestamps bound it | — |

## Failure modes addressed

| Failure mode | Handled in code by | Proven by (test) | or Accepted |
|---|---|---|---|
| Document opened while Explorer is the body | `DocumentOpening` → `Set(Workbench)` | `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` | — |
| Restored pane reuses the island | `RegisterSessionDocument` → `Adapter.Invalidate` | `AReopenedSessionIsShownAndItsComposerIsBound` | — |
| `session.json` missing at revive | `File.Exists` → keep the island | the *gone document* assertion | — |
| `session.json` unreadable at revive (malformed, or reads as `null`) | catch `IOException`/`JsonException`/`InvalidOperationException` → keep the island | `Revive_WithAMalformedSessionFile_KeepsTheIsland`, `Revive_WithASessionFileThatReadsAsNull_KeepsTheIslandRatherThanThrowing` (red) | residual: silent |
| A composer already bound is bound again (reopen after revive) | `SessionComposerBinder.Bind` returns "already bound" | `Bind_OfAComposerAlreadyBound_ChangesNothingAndSaysSo` (red) | — |
| No workspace / no providers / ambiguous engine at bind | `SessionComposerBinder` refusals | `TheBinderRecordsWhatItBoundTests` | — |
| Malformed provider file on New Session | `ReadProviders` → refuse before the sheet | pre-existing (Ruling 47 (b)) | — |
| Malformed provider file on reopen / restore | `SessionComposerBinder.Refuse(…, "providers", reason)` on the shown document | `Refuse_FromTheWindow_LandsOnTheComposerAndInTheLog` | — |
| No task class at send | `ComposerSendGate.Send` refusal | `ASendWithNoTaskClassIsRefusedByNameTests` | — |
| Chooser cancelled | flow returns before any open | `WithNoWorkspaceTheChooserInterposes_AndCancelAbortsCleanly` | — |
| Chosen workspace does not open | flow returns the open path's reason | `…_AndAFailedOpenCreatesNothing` | — |
| Window reports no workspace after a successful open | flow refuses with a sentence naming the folder | `TheChooserOpensTheChosenWorkspace_AndAWindowThatStillReportsNoneCreatesNothing` | — |
| Second maximize while one stands | service refuses; logged as `refused` | `TheMaximizeWritesItsOwnMutationLine` | — |

## Threats addressed (adversarial analysis)

No trust boundary is added. The binder keeps the one-binding-feeds-both property (INV §13's flag
for the implementer): the attach gate's provider id and account label are the same `LaneBinding`
as the send context's — asserted by `TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate`
on the binder file (`binding.Provider.ProviderId`, `AccountLabel: binding.Account.Label`,
`binding.Account.Label)`); its sweep over the rest of `src/AiDe.App` now covers constructions of the
send context, the attach gate and the registry in both spellings, and any `composer.Configure(` /
`Composer.Configure(` — the Test Architect's finding that the sweep proved less than it said. The
provider file the replays read is written into each run's own temp directory, never the operator's.

## Review record (Stage 4)

- **Test Architect (Adversary):** held the veto on the first pass — the window's wiring for Phases
  2/2b/3 had no verification path; the chooser oracle was a tautology by identical stand-in value;
  the revive's catch let a `null` session file throw at launch; the relocated one-site sweep proved
  less than it said; a second bind of one composer was uncovered; three binder exits and the flow's
  "reports no workspace" branch had no test; the INV's `hostLoaded/hostVisible` item was not shipped.
  Every finding but the last is closed above with a red-first test; the last is recorded as a plan
  amendment (the `shell.mode` line answers the question and the seam makes the state unreachable).
- **Simplifier (Adversary):** PASS, net −18 lines possible; taken: the unconditional `Invalidate`
  inside the unregistered branch, `SurfaceIdFor` deriving its prefix from `Kind` (DM7), one on-screen
  guard in the binder, `OpenWorkspaceOrSayWhyAsync` and `BindOnRecord` in the window. Left: the
  pre-existing second render on the open-folder path (a cost, not a correctness defect); the
  `stackId` optional parameter (defended: recorded as null, never a guess); the replay's scenario
  booleans (harness only).

## Privacy findings addressed (LINDDUN-lite)

`session-document.bound` carries the repository root; `session-document.refused` carries the
refusal reason, which may name the provider file's path or the workspace — paths this local log
already carries (`mcp.config`, `layout.mutation`). No draft text, no account credentials, no
prompt content is written. The log stays in `%LOCALAPPDATA%\AiDe\logs`.

## Gates

On the committed tree at `4d7c5bfa` (the review's reds) plus the remark and register edits of the
closing commit, 2026-09-12 00:20–00:35Z:

| Gate | Result |
|---|---|
| `dotnet build` App, App.Tests, Core.Tests — each `-p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors, each |
| `dotnet test tests/AiDe.App.Tests` (full, `--no-build`) | **635 passed, 0 failed, 0 skipped** (1 m 59 s) — 554 baseline + 81 |
| `dotnet test tests/AiDe.Core.Tests` (full, `--no-build`) | **2240 passed, 0 failed, 0 skipped** (1 m 7 s) |
| `tools/verify-test-run.py` (CHECK only; `--update` never run) | OK — App 635 ≥ 554, Core 2240 ≥ 2239, both Completed |
| every other `tools/verify-*.py` (30 scripts) | all OK; `verify-cited-controls` first failed on a backtick mention of the retired oracle's name in a remark (DC-095) — reworded |
| `tools/regenerate-derived.py` | see the closing commit (run after the audit entry) |

## Residuals (not in scope here)

- **Phase 5** — the ADR-0017 amendment (*a command that needs the workbench body switches to it*,
  with the list of such commands) is A1's; `EveryOpeningCommandPassesThroughTheSeamTests` is the
  control it will cite.
- The four legacy mockups' script errors (`h_theme is not defined`) are D2's finding (DC-147).
- The workspace-open restore while Explorer is the body (Claim 1's residual).
- A reopened session's task class (Claim 3's residual) awaits Ruling 72's per-prompt class.
- DC-150 (registered by this run): a relative path handed to a runtime file API from a shell whose
  location was moved resolves against the process's cwd — the primary checkout. Reported, reverted in
  content; no gate fails on the shape yet.
