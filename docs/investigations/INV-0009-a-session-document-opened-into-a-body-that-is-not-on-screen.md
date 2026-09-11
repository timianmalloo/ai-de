---
id: inv-0009-a-session-document-opened-into-a-body-that-is-not-on-screen
title: "A session document opened into a body that is not on screen: New Session while Explorer is the body, and a reopen that never binds or shows"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "conductor-front-door"
tags: [session-document, composer, explorer-mode, shell-view-mode, docking, layout-restore, observability, ruling-47, dc-147, dc-148, dc-084, dc-040, dc-135]
links:
  - { to: plan-conductor-front-door, rel: refines }
  - { to: adr-0017-primary-view-mode, rel: depends-on }
  - { to: adr-0021-named-dock-zones, rel: depends-on }
  - { to: note-front-door-rulings-45-48, rel: depends-on }
  - { to: inv-0007-composer-entry-areas-starved-by-the-compiled-view, rel: relates-to }
  - { to: inv-0008-contrast-floor-passes-while-the-shell-fails, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: ""
summary: >-
  The operator ran File → New Session twice after opening a workspace and saw nothing; the
  brief attributed it to the restored layout. Replayed from the log's own restore payload in the
  product's docking host under the product's mode controller: the restored arrangement renders the
  new document (green), and the one state the log names by its explorer-graph line — Explorer as
  the window's body — does not (red, exit 30): the document is added to an unparented docking host,
  its composer is configured and never loaded, and the shell announces "opened … maximized" about a
  tree nobody is looking at; putting the workbench back shows it untouched (necessary and
  sufficient). A second red: a reopened session whose surface the restore already placed keeps its
  "No session is open" island and its composer is never bound. A third finding: the operator's
  first document at 22:33:28Z was a New Session through the chooser with no workspace open, whose
  composer rendered and was refused for "no open workspace" — the blank editor they described. Four
  oracles committed (two red); the fix is not made.
---

# INV-0009 — A session document opened into a body that is not on screen

- **Status:** Root causes verified · red oracles committed · **no fix made** — stops for the Owner
- **Severity / tier:** T1 — blocks the F5 exit run; the product's front door shows nothing
- **Reported by / date:** the operator, 2026-09-11 22:34Z; dispatched by the conductor (session `conductor-addendum-c`)
- **Evidence:** `docs/investigations/operator-launch-22-33Z.log.jsonl` (26 lines, 22:33:20Z–22:34:30Z, from the Release build `1.0.0+135e05e1`, whose session and composer code equals `main` `1aadde84`)
- **Related:** INV-0007 (the `--shell` probe), INV-0008 (the census over the real App), Ruling 47, ADR-0017, DC-135, DC-147, DC-148, DC-084, DC-040

> Diagnosis only. The replay (`tests/AiDe.App.ComposerProbe --session-render`) and the four oracles in
> `tests/AiDe.App.Tests/Sessions/ASessionDocumentIsShownWhereTheOperatorIsTests.cs` are committed as
> evidence — two red, two green as controls. Investigation worktree `investigate/session-document-render`.

## 0. What was measured, and what was not

**Measured (Verified).** Every line of the operator's log (§3). The product's own restore path
(`ZoneLayoutStore.Save` → `LayoutPersistence.Restore` → `RestoreZones`) fed the log's
`workspace-open` payload verbatim, and the shell's projected stacks compared equal to it before any
step ran (the probe exits 33 otherwise). The product's `ShellModeController` over the workbench root,
built as `MainWindow` builds it. `MainWindow.NewSession`'s `opened` callback in its order —
`OpenSessionDocument`, `Configure` as `BindComposer` calls it, `GiveItTheWholeTree`, `Render` — and
`ReopenSessionAsync`'s one shell call. WPF's `Loaded`/`Unloaded` on the composer, its `IsLoaded`,
`IsVisible` and size; the workbench root's `IsLoaded`, `IsVisible` and `Parent`; the product's
handshake transitions and `composer.layout` lines on stdout; the page's own field count; the
adapter's content for the reopened surface. Four runs, each printed in full in §4.

**Not measured (named).** The real `MainWindow` was not driven: `NewSession` shows a modal sheet and a
folder chooser, and the mode controller is a private field. The probe composes the controller over a
`ContentControl` as `MainWindow.xaml.cs:57–61` does (read, not executed — **Inferred** that the
`RootLayer` grid around `WorkbenchHost` changes nothing about `Loaded` on a swapped
`ContentControl.Content`; the log's own lines corroborate it in the product, §5). Whether the operator
*clicked* the Explore rail item, pressed its chord, or reached `shell.toggleExplorer` from the palette
is not in the log — only that the mode was entered (§5, question for the operator). The
`repositoryRoot` refusal at 22:33:28Z is not in the log either (`ShowFieldRefusal` writes no
diagnostic); it is inferred from the code path and the single `workspace-open` line (§7, finding C).

## 1. Symptom

The operator launched the Release build, and within seventy seconds: ran `File → New Session`
(22:33:28Z), opened the workspace `C:\Projects\TheTerrace` (22:33:53Z), ran `File → New Session`
twice more (22:34:09Z, 22:34:20Z), saw no session document, and closed the app (22:34:26Z). The
brief described the first document as *"restored from the persisted layout"* with *"a blank editor
area with the compiled box under it"*, and the two later documents as never rendered.

## 2. Reproduction

`tests/AiDe.App.ComposerProbe/Program.SessionRender.cs` — mode `--session-render`. It replays the
launch step by step in the product's docking host; each step is a flag so the run that omits it is
the control for the run that includes it:

| Step | Flag | What the product does |
|---|---|---|
| 22:33:28Z | `--prior-document` | `OpenSessionDocument` on the default layout; `ShowFieldRefusal("repositoryRoot", …)` where `BindComposer`'s first guard refused; `GiveItTheWholeTree`; `Render` |
| 22:33:53Z | *(always)* | the log's restore payload written by `ZoneLayoutStore`, restored by `LayoutPersistence.Restore()` over the shell's real `ZoneBackedLayoutService`; the stacks compared to the log |
| 22:34:00Z | `--explorer` | `ShellModeController.Set(Explorer)` — the product's controller, the product's `ExplorerSurface` factory |
| 22:34:09Z | *(default)* | `OpenSessionDocument` → `Draft.SwitchTo(GoalBlock)` + `Configure(…)` → `GiveItTheWholeTree` → `Render` |
| sibling | `--sibling` | `Controller.Execute("workbench.newCodeViewer")` in the same state |
| necessity | `--return-to-workbench` | `ShellModeController.Set(Workbench)`, then the same measurements again |
| reopen | `--reopen` | `SessionConfigStore(root, "20260911T175821Z-1edfa710").Load()` → `OpenSessionDocument(config)` — `ReopenSessionAsync`'s shell half, for the restored layout's active document |

Exit codes: 30 the new document's composer never entered a rendered tree; 31 it loaded but never
reached `init-pushed` with six fields; 32 the reopened session was not shown as a bound document; 33
the replay did not reach the operator's arrangement.

## 3. Timeline — the log, line by line

| Line | Time | Event | What it says |
|---|---|---|---|
| 1 | 22:33:21.3 | `app.start` Release `135e05e1`, DPI 1.5, 1180×720, dark | the build and the window |
| 2–4 | 22:33:21 | `graph` initialising · re-attached · navigation-started | the default layout's canvas |
| 5 | 22:33:28.85 | `open-session-document split-beside-graph surface=…ba326cf3 active=graph`; stacks: left `[explore,provenance,contexts,joins]`, center `[graph,domain,sessions,board,leaderboard,ledger]`, **right `[session-document:…ba326cf3]`**, bottom `[terminal-1]` | **`File → New Session`, on the default layout.** The id's timestamp half is the minting time (`SessionId.New`, `SessionId.cs:40`): `20260911T223328Z` was minted in this second — this document was **created**, not restored. No `workspace-open` line precedes it, and `AttachWorkspace` always writes one on a first attach (`WorkbenchShell.cs:1723–1748`), so **no workspace was open** |
| 6 | 22:33:28.90 | `composer.layout …ba326cf3` 90×530, `loaded:false` | measured before Loaded |
| 7–10 | 22:33:28.9–29.0 | `graph re-attached`; `composer …ba326cf3` initialising · navigation-started · page-ready | the document rendered; the page mounted |
| 11–12 | 22:33:30–34 | `composer.layout` 140×843 then 436×843, editor 617 px, `loaded:true` | the right zone widened — Ruling 47's maximize collapsed left and bottom |
| — | — | **no `configured`, no `init-pushed`** for `…ba326cf3` — ever | the page mounted with no fields: the blank editor with the compiled box under it (finding C) |
| 13 | 22:33:46 | `graph re-attached` | a render (a click, a resize) |
| 14 | 22:33:53.72 | **`workspace-open restore-zones`**; stacks: left `[graph,domain,explore,sessions]@0`, center `[ledger,leaderboard,board,session-document:…bd59855b,provenance,contexts,joins,session-document:…1edfa710]@7`, bottom `[terminal#b356ea]@0` | the first and only workspace attach; the saved arrangement replaced the layout, **dropping `…ba326cf3`**; the center's active tab is a restored session-document surface with no live session behind it — it renders *"No session is open. Create one from File → New Session."* (`SurfaceContentFactory.cs:179`) |
| 15 | 22:33:54 | `mcp.config Merged C:\Projects\TheTerrace\.mcp.json` | the workspace |
| 16 | 22:33:57 | `graph re-attached` | a render |
| **17–18** | **22:34:00.18** | **`explorer-graph` initialising · navigation-started** | **Explorer mode entered.** `explorer-graph` is built only by `WorkbenchShell.CreateExplorerGraph` (`:1387`), called only by the factory `MainWindow` hands `ShellModeController` (`MainWindow.xaml.cs:60`), which runs only inside `Set(Explorer)` (`ShellModeController.cs:69`); `initialising` is logged from the surface's `Loaded`, which fires only once the Explorer is the body. **From here the docking host is unparented** (`_host.Content = _explorer`) |
| 19 | 22:34:09.13 | `open-session-document split-beside-graph surface=…c5547968 active=graph`; center `[…, session-document:…c5547968]@8` | `File → New Session` #2; the model added it to the center as the active tab |
| 20 | 22:34:10.98 | `composer …c5547968` **`configured` fields 6** — nothing before, nothing after | `BindComposer` ran and `Configure` succeeded; **no `initialising`**: the composer's `Loaded` never fired (`WebSurfaceHost.cs:76`) |
| 21 | 22:34:20.09 | `layout.mutation command refused position-mapping-refused`: `Left:[…]/collapsed | Right:-/collapsed | Bottom:[…]/collapsed | Center:[…+…c5547968@8]` | the reconcile before `File → New Session` #3 read the view: left, right and bottom were collapsed — Ruling 47's maximize at 22:34:09 had applied to the model |
| 22–23 | 22:34:20–21 | `open-session-document tab surface=…b62cbcd3 active=…c5547968`; `configured` fields 6, nothing else | #3, same shape; placed as a tab beside #2 because the view's active surface was #2 |
| 24–26 | 22:34:26.29 | all three composers `disposed`; `…c5547968` and `…b62cbcd3` with `navigations: 0` | the app closed; two browsers were never started |
| — | — | **no `graph re-attached` after line 16** — and no line of any kind from inside the docking host after 22:34:00 | consistent with the host never returning to the tree (weak alone — §4 B0 shows a workbench-mode render of this shape re-attaches nothing either, because the maximize collapses the graph's zone) |

## 4. The four runs (the observations)

All on `main` `1aadde84`, window 1180×720, Debug probe, 2026-09-11 22:56–23:05Z. Every line below
is printed by the probe from measurements; the handshake lines are the product's own.

**B0 — operator's arrangement, workbench as the body** (`--session-render --prior-document`): **exit 0**.
```
prior document (22:33:28Z replay): initialising=1 page-ready=1 configured=0 init-pushed=0 layout-lines=1
  status='repositoryRoot: this window has no open workspace, so a run has no checkout to cut a worktree from'
  zones=zone-center@0[graph,domain,sessions,board,leaderboard,ledger] | zone-right@0[session-document:…8fd31257]
restore (22:33:53Z replay): applied-saved=True zones=zone-left@0[graph,domain,explore,sessions] |
  zone-center@7[ledger,leaderboard,board,session-document:…bd59855b,provenance,contexts,joins,session-document:…1edfa710] | zone-bottom@0[terminal#b356ea]
restored active document: live-document=False renders='No session is open. Create one from File → New Session.'
new session (22:34:09Z replay): announced='Session "probe session" opened. Composer bound to claude-code · probe-model · probe-account. Maximized the center.'
after New Session: mode=Workbench workbench root loaded=True visible=True, composer wpf loaded=1 unloaded=0 isLoaded=True isVisible=True size=453x609,
  transitions initialising=1 navigation-started=1 page-ready=1 configured=1 init-pushed=1, layout-lines=1, page fields=6, graph re-attached by this render=0
```
The restored arrangement, its two restored session documents, the dropped prior document and
Ruling 47's maximize **do not** stop the new document rendering. Hypotheses H1, H2 and H4 of the
brief are disconfirmed in the product's own restored state; H3 (hidden behind a tab) is
disconfirmed by `active in view=session-document:…` and `isVisible=True`.

**B1 — the same, Explorer as the body** (`--session-render --prior-document --explorer --sibling`): **exit 30**.
```
explorer (22:34:00Z replay): mode=Explorer explorer-graph initialising=1 workbench root loaded=False visible=False parent=(none)
new session (22:34:09Z replay): announced='Session "probe session" opened. Composer bound to … Maximized the center.'
after New Session: mode=Explorer workbench root loaded=False visible=False, composer wpf loaded=0 unloaded=0 isLoaded=False isVisible=False size=0x0,
  transitions initialising=0 navigation-started=0 page-ready=0 configured=1 init-pushed=0, layout-lines=0, page fields=(no page), graph re-attached by this render=0
sibling (code viewer, same state): in-layout=True content=Border wpf loaded=0 isLoaded=False isVisible=False
the new session's composer never entered a rendered visual tree: WPF raised no Loaded on it and its browser host never initialised, so the operator saw nothing of the session the shell announced
```
The product's own lines reproduce the log's lines 19–23 exactly: `open-session-document
split-beside-graph` → `configured fields 6` → nothing; then, for the second open, `command refused
position-mapping-refused` with `Left:[…]/collapsed|Right:-/collapsed|Bottom:[…]/collapsed` →
`open-codeviewer tab active=session-document:…` (line 21–22's shape). **Sufficient.**

**B2 — B1, then the workbench returns** (`--session-render --prior-document --explorer --return-to-workbench`): **exit 0**.
```
after New Session:                     mode=Explorer  … composer wpf loaded=0 … initialising=0 … configured=1 init-pushed=0, layout-lines=0
after returning to the workbench:      mode=Workbench workbench root loaded=True visible=True, composer wpf loaded=2 unloaded=0 isLoaded=True isVisible=True size=453x609,
  transitions initialising=1 navigation-started=1 page-ready=1 configured=1 init-pushed=1, layout-lines=1, page fields=6
```
The same composer object, untouched, loaded (twice — the DC-138 re-parent; the once-gate held:
one `initialising`, one `re-attached`, one navigation), measured 453×609, mounted six fields.
**Necessary.**

**A — the reopen** (`--session-render --reopen`): **exit 32**.
```
reopen: announced='Session "Terrace session" opened.'
reopen: pane content=Border live-document-in-view=False renders='No session is open. Create one from File → New Session.' active in view=session-document:…1edfa710
reopen: composer initialising=0 page-ready=0 configured=0 init-pushed=0 layout-lines=0 page fields=(no page) host fields=0 status=''
```
Two facts on one path: the live document registered by the reopen never reached the view (the
adapter reused the island the restore built — `WorkbenchAdapter.cs:238`, reuse unless
`_pendingRebuild` names the id), and nothing configured its composer.

Through `dotnet test`: `Failed: 2, Passed: 2` — `ANewSessionCreatedWhileExplorerIsTheBodyIsShown`
and `AReopenedSessionIsShownAndItsComposerIsBound` red; `ANewSessionRendersInTheOperatorsRestoredArrangement`
and `LeavingExplorerShowsTheSessionCreatedInsideIt` green. INV-0007's
`TheComposersEntryAreasKeepTheirRoomAfterTheNewSessionChoreography` still green on the changed probe.

## 5. Verified root cause — (B) the new session is never shown

**The document was opened into a docking host that was not in any visual tree, because the Explorer
was the window's body, and the shell reported the model's success as the screen's.**

- **Mechanism.** `ShellModeController.Set(Explorer)` replaces `WorkbenchHost.Content` with the
  Explorer surface (`ShellModeController.cs:66–70`; wired at `MainWindow.xaml.cs:57–61`). The
  workbench root is retained but unparented (`Parent=(none)`, `IsLoaded=False`). `session.new`
  stays reachable (menu, rail button `OnNewSessionFromRail`, palette, chord); `NewSession`'s
  `opened` callback runs `OpenSessionDocument` → `OpenReferenceDocument` (`WorkbenchShell.cs:1574`)
  → `Service.Apply(AddSurface)` → `Adapter.Render()`, then `BindComposer` → `Configure` (logs
  `configured`), then `GiveItTheWholeTree` → `Maximize` → `Render`. AvalonDock's layout model is
  updated; no element inside it can raise `Loaded`; `WebSurfaceHost.OnAttachedAsync` never runs;
  the browser never starts. The announcement — composed from `LayoutResult` and the binding — reads
  *opened, bound, maximized*.
- **Evidence in the product.** Line 17 (Explorer entered at 22:34:00Z, by the only code path that
  builds `explorer-graph`); lines 19–20 and 22–23 (`configured` with `navigations: 0` and no
  `initialising`, twice); lines 25–26 (`disposed` with `navigations: 0`). Verified by the replay:
  B0 green, B1 red, B2 green — the body is necessary and sufficient, in the product's restored
  arrangement, through the product's controller.
- **Confidence: Verified** for the mechanism and its reproduction. **Inferred** (from the log, not
  from a screenshot) that this is what the operator experienced at 22:34:09–22:34:26 — the log
  cannot show the screen; it shows the Explorer was entered and never left before close. See the
  question below.

**Competing causes, ruled out.**
- H1 *the maximize collapses the new document's zone or the projection omits it* — no: line 21 and
  B1 show the center holding `…c5547968@8` uncollapsed; B0 renders the document after the same
  maximize.
- H2 *two session documents cannot be projected in one zone; the restored one wins* — no: B0 has
  three in the center and renders the new one.
- H3 *rendered but behind another tab* — no: the new document is the active tab in model and view
  (`active in view=session-document:…`); in B0 it is `isVisible=True`; in B1 nothing in the host
  is visible.
- H4 *the factory hands back the old document for the new surface* — no: `SessionDocumentFor` is
  keyed by the surface id and B0's new composer is the one that mounted (`composer:…73d939ee`).
- *The prior document's residue in `_sessionDocuments`* — no: present in B0, green.
- *The layout-restore path itself* — no: B0 used `LayoutPersistence.Restore()` from a file written
  by `ZoneLayoutStore`, and the stacks matched the log.

**Question for the operator (one):** at 22:34:00Z, did you click the Explore rail item (or its
chord) before pressing New Session — and were you looking at the graph-and-reader view when nothing
appeared? The log says the mode was entered; it cannot say by which door, nor what you saw.

## 6. Verified root cause — (A) a reopened session is neither bound nor shown

Two defects on `MainWindow.ReopenSessionAsync` (`MainWindow.xaml.cs:359–388`), both red in run A:

- **A1 — never bound (DC-084 recurrence).** `BindComposer(NewSessionResult)` is *"the only caller
  of `ComposerSurface.Configure` in the product"* (`:214`), reachable only from `NewSession`'s
  `opened` callback. The reopen path ends at `Shell.OpenSessionDocument(config)` (`:387`). The
  binding needs the session config and the provider file — both available on the reopen path — and
  the sheet's `RoutableBackends`/`TaskClass`, which it does not have and which must be re-derived
  (§4.3's login filter over `config.EnabledBackends`; the task class is per prompt, Ruling 70).
  Measured: `configured=0 init-pushed=0 host fields=0`.
- **A2 — never shown when the surface was restored (DC-040 recurrence).** The saved layout restores
  `session-document:<id>` surfaces before any session is reopened; the factory builds the *"No
  session is open"* island for each (`SurfaceContentFactory.cs:167–184`, by design per
  `SessionDocumentFor`'s remark). The reopen then registers the live document and calls
  `OpenReferenceDocument`, whose `OpenPane` sees the id already open and only *activates* it
  (`ZoneLayoutService.cs:60–63`), and whose `Render` **reuses** the island because nothing
  `Invalidate`d the id (`WorkbenchAdapter.cs:238`). Measured: `live-document-in-view=False`,
  `renders='No session is open…'` after `announced='Session "Terrace session" opened.'`.

**Confidence: Verified** (red oracle, code path opened).

## 7. Finding C — the first document, 22:33:28Z (the brief's premise corrected)

The brief read the first document as *restored from the persisted layout*. It was **created**: its
id was minted in the same second as the mutation (`SessionId.New` stamps `UtcNow`;
`SessionId.cs:37–43`), no workspace had attached (`AttachWorkspace` always writes `workspace-open`
on a first attach and the only one is line 14), and the default layout's `terminal-1` is on line 5
where line 14 has the persisted `terminal#b356ea`. So the operator's first act was `File → New
Session` with no workspace open: `NewSessionFlow.Start` interposed the chooser, the chosen root
bound the session store, the document opened and rendered (lines 6–12), and `BindComposer`'s first
guard refused — *"repositoryRoot: this window has no open workspace"* — because the flow never
opened the chosen workspace in the window (`NewSessionFlow.cs:84–116`; `MainWindow.xaml.cs:242–249`).
The page mounted with no fields; the status line under the compiled box carried the refusal. That
is the blank editor the operator described. The refusal is not in the log (`ShowFieldRefusal`
writes no diagnostic) — the probe's `--prior-document` step reproduces the state and prints the
sentence. Registered as **DC-148**. The premise error itself is NG-shaped (the id's timestamp was in
hand and unread); it is recorded here rather than registered.

## 8. System map — where state lives, and what each surface knows

```
MainWindow ─ owns ShellModeController (which body is showing)      ← only the window knows the mode
   │            └─ WorkbenchHost.Content ∈ { Shell.WorkbenchRoot, ExplorerSurface }
   ├─ NewSession() ─ NewSessionFlow ─ chooser ─ sheet ─ opened: OpenSessionDocument · BindComposer · GiveItTheWholeTree
   └─ ReopenSessionAsync() ─ SessionConfigStore.Load ─ OpenSessionDocument                    ← no bind, no invalidate
WorkbenchShell ─ ZoneBackedLayoutService (the model) ─ WorkbenchAdapter.Render ─ DockingManager (the view)
   ├─ OpenReferenceDocument: Apply(AddSurface) → LayoutMutation(…) → Render → Bind*()       ← does not know the mode
   ├─ _sessionDocuments[id] ─ SurfaceContentFactory.SessionDocument(surface) → document | island
   └─ Announcer ← LayoutResult.Announcement + "Composer bound …" + "Maximized the center."   ← composed from the model
ComposerSurface ─ WebSurfaceHost(owner.Loaded → initialising → navigate) ─ Configure → configured ─ both → init-pushed
```
The feedback the operator relies on — the announcement — is fed by the model; the only signal fed
by the view is the composer's handshake, which is silent when the view is absent. Nothing emits the
mode; nothing emits the maximize; nothing emits a refusal.

## 9. Specific fixes — proposed, not made

| # | Change | Why it addresses the root | Blast radius · rollback | Regression test (seen red) |
|---|---|---|---|---|
| F1 | `WorkbenchShell` gains one seam, `Action? DocumentOpening` (or `Func<bool> EnsureWorkbenchBody`), invoked at the top of `OpenReferenceDocument` and the other `AddSurface`+`Render` commands (terminal, prompt draft). `MainWindow` wires it to `_mode.Set(ShellViewMode.Workbench)` and folds *"Left Explorer."* into the announcement. | A dock document is only ever opened into a body that is on screen; every catalog command that opens one is covered by the one seam, not by per-command checks. | App only; the Explorer is left by a command that needs the workbench, which ADR-0017 permits (a view change, not a rebuild). Rollback: remove the wiring. | `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` (exit 30 today) — and the sibling line in the same run |
| F2 | `OpenSessionDocument`: when the surface id is already in the layout, `Adapter.Invalidate([surfaceId])` before `OpenReferenceDocument`. | The island built at restore is replaced by the live document; the DC-029 reconcile keeps reusing everything else. | App only; one pane rebuilt on reopen. Rollback: remove the call. | `AReopenedSessionIsShownAndItsComposerIsBound` (`live-document-in-view=False` today) |
| F3 | Extract `BindComposer` from `MainWindow` into `Workbench.Sessions.SessionComposerBinder.Bind(shell, config, providers, workspace, affirmation)` taking a `SessionConfig`; `RoutableBackends` derived from `config.EnabledBackends` + the registry's login state (one derivation, shared with the sheet — DM7); `TaskClass` absent on reopen until the operator chooses one (Ruling 70). `NewSession` and `ReopenSessionAsync` both call it; the restore path (Phase 2b) calls it for each restored surface whose `session.json` loads. | The binding lives with the thing bound, reachable from every path that opens a document (DC-084's generalisation). | App; the sheet's `RoutableBackends` computation moves to a shared function — the sheet test suite covers it. Rollback: keep the private method. | the same reopen oracle (`configured=0` today) |
| F4 | `NewSessionFlow`: when the chooser supplies the root, `opened` is preceded by opening that workspace in the window (`OpenWorkspaceAtAsync(root)`), or the flow refuses with *"open the workspace first"* — **Owner's choice**. | The window and the session are bound to the same workspace before the composer is bound; the refusal at 22:33:28Z cannot occur. | App; the chooser path only. Rollback: revert the flow. | a new oracle on `NewSessionFlow` with a chooser: `opened` observes an open workspace (needs the seam F4 introduces) |
| F5 | Instrumentation: `WorkbenchDiagnostics.ShellMode(mode)` on every `Set`; `LayoutMutation("maximize-stack", …, after)` in `GiveItTheWholeTree` (and its refusal); `open-*` mutation lines carry `hostLoaded`/`hostVisible` read from the workbench root; `ShowFieldRefusal` and `Configure` write `session-document.refused` / `session-document.bound` with the field and the session id. | Every question this investigation had to infer — the mode, the maximize, the refusal, whether the host was on screen — is a line in the log on the normal path (IO1–IO4). | App; log volume +4 lines per session open. Rollback: remove the emitters. | `verify-harness-diagnostics.py` (if it enumerates event kinds — check) plus a unit test per emitter |

## 10. Generalization — the failure classes

- **DC-147 (new) — a command mutates the model of a view that is not on screen, and reports the
  model's success as the screen's.** Sweep (`WorkbenchShell.cs`): `OpenReferenceDocument` and its
  five callers (`:335–364` class diagram, sequence, search, code viewer, diagnostics), the terminal
  open (`:268–304`), the prompt draft (`:311–331`), `OpenSessionDocument` (`:2909`) — every one
  applies to the model and renders without consulting the body; the code viewer measured red in
  B1. Ruled out as siblings: the palette (`RootLayer` overlay, on screen in both modes) and the
  Explorer's own reader/graph (they *are* the body). ADR-0017 is silent on commands issued while
  the non-active mode is retained — **a spec gap to surface**, since Ruling 47 delivered "full
  window like the explorer view icon" as maximize-on-create and the operator is evidently using both.
- **DC-148 (new) — a flow acquires a resource by asking the operator, uses it for one half of the
  work, and refuses the other half for lack of that resource.** Sweep: the terminal open with no
  workspace (`WorkbenchShell.cs:268`) announces and degrades rather than refusing — not a sibling;
  `ReopenSessionAsync` opens the workspace first (`:361–370`) — the correct shape, which is the
  evidence the New Session flow could do the same.
- **DC-084 (recurrence 2)** — the binding wired only where the sheet's result exists.
- **DC-040 (recurrence 2)** — the restored pane captured "no document" and was never invalidated.
- **DC-135 (noted, not a recurrence)** — INV-0007's `--shell` probe hosted the workbench as the
  window's content with no mode controller; the state B1 needs was unreachable there. The replay
  builds the controller as the window does; the residual (the real `MainWindow`, its sheet and
  chooser) is named in §0.

**Markers harvested (CI9):** no `simplify:` or `assume:` marker in `MainWindow.xaml.cs`,
`ShellModeController.cs`, `WorkbenchShell.cs`, `Workbench/Sessions/*`, `SurfaceContentFactory.cs`,
`WorkbenchAdapter.cs`, `LayoutPersistence.cs` or `ComposerSurface.cs` (grep, 2026-09-11); the one
marker in `src/AiDe.App` is `TerminalSurface.cs:219`, unrelated.

## 11. Phased repair plan — each phase a one-node T1, code + tests, independently landable

| Phase | Scope (code + tests) | Failure mode eliminated | Validation | Depends on |
|---|---|---|---|---|
| **1** | F1 (the `DocumentOpening` seam + `MainWindow` wiring) + F5's `shell.mode` line and `hostLoaded/hostVisible` on `open-*` mutations | DC-147: a dock document opened while Explorer is the body is invisible and announced as shown | `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` green (incl. the sibling line); `LeavingExplorerShowsTheSessionCreatedInsideIt` and `ANewSessionRendersInTheOperatorsRestoredArrangement` stay green; INV-0007's `--shell` oracles stay green; a `ShellModeController` unit test asserts one `shell.mode` line per `Set` | — |
| **2** | F2 (invalidate on reopen of a restored surface) + F3 (`SessionComposerBinder`, called from `NewSession` and `ReopenSessionAsync`) | DC-040 rec. 2 and DC-084 rec. 2: a reopened session shows the island and has no run binding | `AReopenedSessionIsShownAndItsComposerIsBound` green; the sheet's `RoutableBackends` tests unchanged; `ANewSessionTakesTheWholeTreeTests` (reopen does not maximize) unchanged | 1 (the probe's `--reopen` run must be able to see the pane) |
| **2b** | Bind restored session documents at `workspace-open`: for each `session-document:<id>` surface the restore keeps, `SessionConfigStore(root, id).Load()` → register + bind; the island only when `session.json` is gone | The restored layout's active tab reading *"No session is open"* over a session that exists | a new probe step (`--bind-on-restore`) asserting the restored active document is live and `configured`; red today by construction (`live-document=False` in every run) | 2 |
| **3** | F4 — the Owner's choice: open the chosen workspace before `opened`, or refuse New Session until a workspace is open | DC-148: the chooser-bound session's composer refused for "no open workspace" | a `NewSessionFlow` oracle with a chooser; red until F4 | — (Owner ruling) |
| **4** | F5's remaining emitters: `maximize-stack` mutation (+ refusal), `session-document.bound`/`.refused` | the next report of this family is answerable from the log without a replay | emitter unit tests; the replay's B1 run shows `shell.mode=Explorer` and `hostLoaded=false` on the `open-session-document` line | 1 |
| **5** | ADR-0017 amendment: *"a command that needs the workbench body switches to it"*, with the list of such commands = the catalog entries that open dock documents; Ruling 47's note gains the pointer | the spec silence that let DC-147 pass review | `verify-ruling-citations.py`; the ADR's `review-suggested` propagation | 1 |

## 12. Residual risk — and what would change the diagnosis

- The real `MainWindow` was not executed. If `RootLayer`'s grid or the rail's focus handling makes
  `Loaded` fire on an unparented `WorkbenchHost.Content` (it cannot in WPF — `Loaded` needs a
  `PresentationSource`-rooted tree — but it is stated, not run), B1 would be a harness artefact.
  What would show it: an `initialising` line for `…c5547968` in the operator's log. There is none.
- If the operator did **not** enter Explorer at 22:34:00Z — if some other path can build and load
  `explorer-graph` — the cause of (B) in the product is open again. Sweep: `CreateExplorerGraph`
  has one caller; `ExplorerSurface` one construction site; the surface loads only as the body.
- The reopen oracle asserts *bound and shown*; if the Owner rules that a reopened session should
  stay unbound until the operator chooses a task class (Ruling 70), the oracle's `configured`
  assertion narrows to *shown*, and F3 binds with an empty task class.
- The four oracles cost ~30 s each out of process; they join INV-0007's in the App suite (the
  expected-count baseline is the conductor's separate act, not this node's).

## 13. Gate record

- **SRE & Distributed Systems (adversary):** *does the cause explain all the evidence?* Lines 17–26
  yes; line 21's collapsed zones yes (the maximize applied to the model); the absence of a graph
  re-attach after line 16 is **not** evidence either way (B0 measured 0 in workbench mode too) and is
  not relied on. The `configured` 1.85 s after the mutation on line 19–20 is unexplained and not
  load-bearing (render + provider bind on a Release build; not measured).
- **Test Architect (hard veto):** each fix has a regression test seen red on the unfixed code (F1,
  F2, F3 — exit 30 / 32 through `dotnet test`); F4's oracle needs the seam F4 introduces and is
  named as such; F5's tests are unit-level. Cleared for phases 1–2; phase 3 awaits the ruling.
- **Security:** no trust boundary touched; the binder move (F3) must keep the one-binding-feeds-both
  property (`BindComposer`'s remark) — flagged for the implementer, no veto.
- **Simplifier:** one seam (F1), not a per-command mode check; no new mode, no ADR reopened;
  F3 is a move, not an abstraction.
- **Investigator did not self-certify:** the two red runs and the two green controls are committed
  and reproducible with the commands in §2.

**STOP — for the Owner:** phases to execute; F4's shape (open the chosen workspace vs. refuse); whether
a reopened session binds without a task class.
