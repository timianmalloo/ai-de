---
id: inv-0007-composer-entry-areas-starved-by-the-compiled-view
title: "The composer's entry areas have no room: the read-only compiled view takes the editor's height, and the next render kills the page"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "conductor-front-door"
tags: [composer, webview2, wpf, layout, docking, handshake, observability, session-document, ruling-47]
links:
  - { to: plan-conductor-front-door, rel: refines }
  - { to: note-front-door-rulings-45-48, rel: depends-on }
  - { to: adr-0015-canvas-hosting-and-overlay-strategy, rel: depends-on }
  - { to: adr-0021-named-dock-zones, rel: depends-on }
  - { to: inv-0006-workbench-pane-swap-on-native-tab-drag, rel: relates-to }
review-by: ""
summary: >-
  "I cannot type in the composer" / "I could not see the entry areas" after File → New Session.
  Verified by measurement in the real shell under the operator's recorded arrangement: the
  composer's WebView2 laid out at 0px (F5 tree) and 105–110px (main) of a 485–689px composer,
  because the read-only compiled-view TextBox has no height ceiling, sits in a StackPanel docked
  Bottom, and is measured unconstrained before the editor host gets the remainder — a 28-line
  goal block costs 465px, and editor = composer − compiled − 114px. Capping the compiled view from
  outside the product gave the editor 202px in the same 485px and the run went green (necessity).
  A second, independent defect was found and reproduced: any later Adapter.Render() re-parents the
  WebView2, WPF raises Loaded again, InitialiseAsync navigates the page again, and the router drops
  the new page's editor.ready as a duplicate — no host.init, zero fields, the operator's on-screen
  text gone. The graph canvas shares the Loaded→navigate shape (measured: one render, one reload).
  Two red tests are committed; the fix is not.
---

# Investigation: INV-0007 — the composer's entry areas have no room

- **Status:** Root cause verified · fix proposed · **stopped for human review**
- **Severity / tier:** T1 — the product's primary object (the session composer) is unusable on open
- **Reported by / date:** the operator, 2026-09-11, on the F5 tree (`feature/exit-evidence` @ `729fdb5e`); reproduced on `main` @ `f5c0f740`
- **Related spec/design-slice:** `plan-conductor-front-door` (F4 — the composer, R15/R19; Ruling 47 — maximize-on-create), ADR-0015 (WebView2 hosting), ADR-0021 (zone render)

> **Investigation report — diagnosis only.** The red tests are committed (they are evidence); no
> product line is changed except `InternalsVisibleTo` for the probe. The fix waits for the operator.

## 0. What was measured, and what was not

**Measured (Verified).** Every number below was read from a running instance of the shipped code:
`WorkbenchShell`, its AvalonDock host, `SessionDocumentSurface`, `ComposerSurface`, the shipped
`composer.html`/`composer.mjs`/CodeMirror bundle in a real WebView2 — driven out of process by
`tests/AiDe.App.ComposerProbe` (`--shell`), under the arrangement the operator's log recorded, with
the choreography `MainWindow.NewSession` runs (`OpenSessionDocument` → `BindComposer` → on `main`,
`NewSessionPlacement.GiveItTheWholeTree` + `Adapter.Render()`). The WebView2 WPF wrapper's
behaviour (accelerator forwarding, focus, re-parenting) was read from the decompiled
`Microsoft.Web.WebView2.Wpf` 1.0.3485.44, not recalled.

**Not measured (Inferred, and named as such).** The operator's window size and monitor. The
composer emits **no diagnostics at all** — nothing is logged after the `open-session-document`
line — so the operator's exact editor height is read off their screenshot (~200px editor, ~500px
compiled view) rather than from a log. The rule below is height-independent and was measured at
two heights; the operator's numbers sit on the same line. **The OS keystroke path into a focused
field** was not exercised (the probe holds foreground only sometimes and cannot click); the
operator's screenshot shows text they typed into the goal fields, so typing into a reachable field
works — the symptom is the room, not the keystroke.

## Symptom

- Operator, F5 tree, `dotnet run --project src/AiDe.App -c Release`, workspace open, **File → New
  Session**: *"I cannot type in the composer."* Then, with the screenshot: *"I could not see the entry
  areas."* The screenshot shows a ~200px-tall scroll region with only FAN_OUT_CAP and BUDGET
  visible (goal/done_when/not_in_scope scrolled out above — and typed into), a ~500px read-only
  "Compiled view" box under it, then lease/status/Send.
- Log: `%LOCALAPPDATA%\AiDe\logs\workbench-20260911.log`, last line `2026-09-11T17:41:12Z`
  `layout.mutation · open-session-document · split-beside-graph · session-document:20260911T174112Z-28f7fe97`,
  arrangement zone-left `[graph, domain, explore, sessions]`, zone-center `[ledger, leaderboard,
  board, session-document(old), provenance, contexts, joins, session-document(new)]` active 7,
  zone-bottom `[terminal#b356ea]`. **Nothing after it.**
- `~/.aide/providers.json` present and well-formed (one account, `quota-degraded`, which binds);
  `session.json` shows `EnabledBackends: ["claude-code"]` — so `BindComposer` reached `Configure`
  (Inferred from the files; the composer logs nothing that would confirm it).

**Expected:** six goal-block fields on screen with room to write. **Actual:** a sliver, or nothing.

## Reproduction

`tests/AiDe.App.ComposerProbe --shell [--maximize] [--render-after-mount] [--cap-compiled] [--height N]`,
driven by `ComposerHostIntegrationTests.TheComposersEntryAreasKeepTheirRoomAfterTheNewSessionChoreography`
and `…TheComposerPageSurvivesALaterRender`. Both **observed red on `main` @ `f5c0f740`**:

| run | window | composer | editor host (WebView2) | compiled box | page | exit |
|---|---|---|---|---|---|---|
| `--shell` (F5's choreography: open + Configure) | 1280×800 | 399×485 | **399×0** · HWND 599×0 | 375×465 (28 lines) | mounted: `host.init=1 fields=6 editors=3` | 24 |
| `--shell --height 1400` (clamped to 1084 by the screen) | 1280×1084 | 399×684 | **399×105** (15%) | 375×465 (68%) | mounted, CDP keystroke reached the draft | 24 |
| `--shell --maximize` (main's choreography, Ruling 47) | 1280×800 | 495×689 | **495×110** (16%) | 471×465 (67%) | mounted, keystroke reached the draft | 24 |
| `--shell --cap-compiled` (**necessity**: compiled capped at 35% from outside) | 1280×800 | 399×485 | **399×202** | 375×169 | mounted, typed | **0** |
| `--shell --maximize --render-after-mount` | 1280×800 | 495×689 | 110 | 465 | after one later render: `loaded=2 unloaded=1, navigations +1, editor.ready +1, router drops=2, host.init=0, fields=0, editor text=''`, draft still holds the text | 25 |
| `--shell --render-after-mount --height 1400` (graph zone visible) | 1280×1084 | 399×684 | 105 | 465 | same reset; **canvas sibling: navigations started by the same render = 1** | 25 |

The window screenshot of the `--maximize` state is committed beside this file
(`assets/inv-0007-composer-starved-editor-main-800.png`): GOAL in a 165px scroll region, then a 465px
compiled box. (The probe has no App.xaml resources, so its WPF TextBox is white; the product's is
themed — a probe fidelity gap, not a finding.)

Arithmetic the measurements agree on: **editor = composer − compiled − 114px** (Send/Attach bar,
"Compiled view" label, lease, status, margins), clamped at 0. The compiled TextBox's height is its
content: 24 lines (an *empty* goal block — six `## name` headings, three blank lines each) = 401px;
28 lines = 465px; it grows with every line the operator types.

## Timeline

| when (UTC) | what |
|---|---|
| 17:32–17:41 | `verify-test-run.py` writes hundreds of `terminal.start` lines with fixture ids (`20260910T120000Z-deadbeef`) into the **operator's** log — finding F6, not chased |
| 17:40:40 | `terminal.start terminal-1` — app start |
| 17:40:47 | `layout.mutation workspace-open / restore-zones` — the operator's saved arrangement |
| 17:40:48 | `mcp.config Merged`, `terminal.start terminal#b356ea` |
| 17:41:12.284 | `session.open` in `session-events.jsonl` (origin `main-menu.new-session`, `enabledBackends: [claude-code]`) |
| 17:41:12.311 | `layout.mutation open-session-document split-beside-graph`, document placed 8th in zone-center |
| — | **nothing** — the composer's handshake, layout and input emit no events |

Change analysis: the composer landed with F4/F6 (`feature/composer`); Ruling 47's maximize landed on
`main` after the F5 tree branched (`NewSessionPlacement.cs` does not exist on F5). The starvation is
on both trees (the layout is identical); the maximize only changes the composer's height.

## System map

```mermaid
flowchart TD
  MW[MainWindow.NewSession → opened] --> OSD[Shell.OpenSessionDocument → Adapter.Render #1]
  MW --> BC[BindComposer → ComposerSurface.Configure]
  MW -->|main only| GT[GiveItTheWholeTree → Adapter.Render #2]
  OSD --> SDS[SessionDocumentSurface: Grid · composer col ⟷ canvas col]
  SDS --> CS[ComposerSurface: DockPanel]
  CS --> P[template picker · Dock.Top · Auto]
  CS --> B[Send/Attach bar · Dock.Bottom · Auto]
  CS --> F[footer StackPanel · Dock.Bottom · Auto<br/>label · compiled TextBox (MinHeight 90, no MaxHeight) · lease · status]
  CS --> V[WebView2 · LastChildFill = the remainder]
  F -. measured first, unconstrained .-> V
  V --> L[Loaded → InitialiseAsync → EnsureCoreWebView2 → subscribe → Navigate]
  L --> PG[composer.mjs: editor.ready → router.Ready once → MarkReady → host.init → render fields]
  R[any later Adapter.Render] -->|Manager.Layout replaced → re-parent| V
  V -->|Loaded again| L
```

The DockPanel measures docked children first, each with infinite extent on the docked axis; the
`StackPanel` footer hands its `TextBox` infinite height; the `TextBox` reports its content height;
only then does the last child receive `max(0, remaining)`. The **reader is sized by content and the
writer by what is left** — the inversion.

## Hypotheses considered (fishbone → each verified or ruled out by measurement)

| Hypothesis | Predicts we'd see… | Evidence for | Evidence against | Verdict |
|---|---|---|---|---|
| H1 focus never reaches the WebView2 / a global key handler swallows keys | keys typed with focus in the browser do not change the page | — | decompiled wrapper: only accelerator keys (modifiers, Esc, Enter, F-keys) are forwarded to WPF; plain letters go straight to Chromium. `Shell.Bind`/`Controller.Bind` handle only Enter/Esc/arrows/Up/Down when an overlay or resize is active. Probe: `Keyboard.Focus(view)` → OS focus in `Chrome_WidgetWin_1`, page `hasFocus=true`. Operator's screenshot shows typed text. | ruled out for the symptom; **F5**: entry lands on `BODY`, no field focused |
| H2 `host.init` never arrives (Configure not run / ready before router / DC-134) | `fields=0`, `host.init count=0` after the choreography | — | choreography: `editor.ready posted=1, router drops=0, host.init count=1, fields=6, editors=3` on both choreographies | ruled out for the open; **confirmed as finding 2 after any later render** |
| H3 page or bundle failed to load in Release | `__composerError` set, `editors=0`, navigation failed | — | `navigations completed=1`, `error=''`, `editors=3`, CSP probe green | ruled out |
| H4 editor intentionally non-editable | CDP keystroke into `.cm-content` does not change the draft | — | CDP `q` → page text `q…`, draft and compiled view updated, `router drops=0` | ruled out |
| (a) editor host has no size | `WebView2.ActualHeight` ≈ 0 / far below the compiled box | **0px at 485; 105px at 684; 110px at 689; compiled 465px in all three; cap → 202px and green** | — | **Verified — necessary and sufficient** |
| (b) no contrast | computed contrast of fields < 4.5:1 | boundaries: `.editor` border #3c3c3c on #1e1e1e = **1.51:1**; input bg #252526 on #1e1e1e = 1.13:1; drop-hint 4.47:1 | text 10.33:1, labels 6.31:1 | secondary — **F4**, not the cause |
| (c) never mounted | `fields=0` | — | `fields=6` on open | ruled out on open (see H2) |

RCA method: Ishikawa across code/layout/browser/host/focus, then change analysis between F5 and
`main`, then necessity/sufficiency in one harness.

## Verified root cause

**Finding 1 — the operator's symptom (Verified, necessary and sufficient).**
`ComposerSurface` docks a `StackPanel` footer to the **bottom** of a `DockPanel` and gives the
`WebView2` editor host the remainder. The footer's read-only compiled-view `TextBox` has a
`MinHeight` of 90 and **no `MaxHeight`**; measured with infinite height by the `StackPanel`, it
reports its full content height — 401px for an *empty* goal block, 465px with three short answers —
and that is subtracted from the composer before the editor is measured. Under the operator's
arrangement the composer is 485px tall in an 800px window (F5, no maximize), so the editor host is
laid out at **0px**; maximized (main) it is 689px and the editor gets **110px**. Every line the
operator types makes the compiled view taller and the editor shorter.

- *Sufficient:* the unbounded compiled view, in the real shell, produces 0/105/110px editors — three
  runs, two choreographies, two heights.
- *Necessary:* the same run with the compiled view capped at 35% **from outside the product**
  (`--cap-compiled`) laid the editor out at 202px in the same 485px and exited 0 with six fields
  mounted and a keystroke in the draft.
- *Systemic, not proximate:* the surface sizes its **reader by content and its writer by remainder**.
  Any content-sized reader docked beside a filling writer does this; `MaxHeight` on this one box
  removes the instance, the layout rule removes the class.

**Finding 2 — a second, independent defect (Verified; reproduced, not the operator's report).**
`ComposerSurface` hooks `Loaded += InitialiseAsync`. WPF raises `Loaded` on **every** attach to a
loaded tree, and `WorkbenchAdapter.Render()` replaces `Manager.Layout` wholesale, re-parenting every
pane — so any later render (a pane open, a layout command, a restore) runs `InitialiseAsync` again:
a second `AddScriptToExecuteOnDocumentCreatedAsync`, a second set of `core.*` subscriptions
(`WebMessageReceived` now routes every message twice), and a second `core.Navigate(url)`. The new
document posts `editor.ready`; `ComposerMessageRouter.Ready` drops it (*"this instance already
reported ready"*, `router drops=2`); `MarkReady` never re-runs; no `host.init` reaches the page on
screen; `fields=0`. The host's `_draft` still holds the operator's text; the screen shows none of it,
and nothing is logged. The WebView2 wrapper itself survives re-parenting (`BuildWindowCore →
ReparentController`, decompiled) — the reload is ours. `main`'s own two renders coalesce into one
`Loaded` (`wpf loaded=1`) because they run in one dispatcher operation; the *next* render after the
mount is the one that fires.

## Causes ruled out

See the table: H1 (wrapper source + probe focus reading + operator's typed text), H2 on open
(handshake counts), H3 (navigation/error/editors), H4 (CDP keystroke reached the draft), (b) as the
primary cause (text and labels pass; boundaries fail — recorded as F4).

## Specific fix(es) for this instance — proposed, not made

- **Fix 1 (layout):** the composer sizes the writer first. Replace the DockPanel/StackPanel footer
  with a `Grid` whose rows are picker `Auto` · editor `*` (`MinHeight` ≈ 200) · compiled `Auto`
  with `MaxHeight` bound to 35% of the surface (scrolls inside) · lease/status `Auto` · bar `Auto`.
  The rule, stated: **the editor host is never smaller than the compiled view, and the compiled view
  keeps to ≤ 35% of the composer.** At the ~1000px the operator's screenshot suggests, that leaves
  ≥ 500px for the six fields at rest (three long-text editors at three lines + three native rows +
  labels and gaps ≈ 490px by the page's own CSS).
  *Why systemic:* it fixes the sizing order, not the number of lines. *Blast radius:* one control;
  `PromptDraftSurface` already has the right shape (writer fills, bars fixed). *Rollback:* revert
  the control.
- **Fix 2 (re-attach-safe host):** initialise once — guard `InitialiseAsync` with an
  `_initialised` flag (the wrapper re-parents the controller itself), and make the handshake
  tolerate a genuine reload (crash recovery, `ProcessFailed`): reset the router's readiness on
  `NavigationStarting` or key it by a per-navigation nonce, so a *new document's* `editor.ready` is
  a mount, not a duplicate. Apply the same once-guard to `CanvasSurface` (sibling, measured).
  *Rollback:* revert both surfaces.
- **Failing-first regression tests (committed, red on `main`):**
  `ComposerHostIntegrationTests.TheComposersEntryAreasKeepTheirRoomAfterTheNewSessionChoreography`
  (exit 24, *"the editor host is starved: it has 110px of the composer's 689px while the read-only
  compiled view has 465px (67%, ceiling 35%)"*) and `…TheComposerPageSurvivesALaterRender` (exit 25,
  `host.init count=0 fields=0` after one render). Each asserts the perceivable state and prints the
  numbers it read; neither can pass vacuously (`fields=6 editors=3`, `writer >= reader: True`,
  `reached draft=True`, `after one later render: … fields=6`).

## Generalization — the failure classes

- **Class A — a content-sized reader docked beside a filling writer.** *Shape:* an `Auto`-sized
  (content-measured) element — a `TextBox`/`TextBlock`/`StackPanel` with no `MaxHeight` — is docked
  or given an `Auto` row next to a `*`/`LastChildFill` input host; the reader is measured first,
  unconstrained, and the writer receives what is left. *Signature:* `DockPanel.SetDock(…, Bottom|Top)`
  or `RowDefinition Height=Auto` on a content-growing read-only element whose sibling is the input
  surface; a surface that shrinks as the user types. *Why it survives:* nothing measures rendered
  bounds; the bare-window probe gave the surface 700px, so it never saw the starvation (DC-135's
  shape — the harness composed what the product does not).
- **Class B — one-time initialisation hooked to a per-attach event, behind a once-only gate keyed to
  the wrong lifetime.** *Shape:* `Loaded += InitialiseAsync` on a hosted control in a docking host
  that re-parents on every render; the init subscribes and navigates again; a handshake gate that
  is once-per-*surface* treats the reloaded page's ready as a duplicate; the surface dies silently.
  *Signature:* `Loaded +=` with side effects on a non-`Window` element; `IsReady` that is never
  reset; duplicate event subscriptions after a layout change; a WebView2 page that "resets" after a
  pane open. *Why it survives:* the handshake probe mounts once in a bare window; nothing counts
  navigations or `Loaded`; the composer emits nothing.
- **Broader systemic solution:** (A) a layout-rule test helper — *writer ≥ reader* — run against
  every surface that pairs an input host with a docked reader, in the cheap ring (STA WPF measure,
  no browser), and the rendered bounds emitted at first layout; (B) one re-attach-safe web-surface
  host used by both WebView2 surfaces (the `simplify:` marker in `CanvasSurface.cs:211` names
  exactly this upgrade — two copies of one idiom carried one bug twice), initialising once and
  re-keying readiness per navigation; the probe counts navigations across a render and asserts 0.

| Candidate sibling (where) | Same class because… | Verdict | Evidence |
|---|---|---|---|
| `CanvasSurface.cs:105` `Loaded += InitialiseAsync` → `NavigateToString` | Class B: init on every attach | **confirmed** | `--render-after-mount --height 1400`: *canvas sibling: navigations started by the same render = 1* — the graph reloads on every render (no dead page, because its handshake has no once-gate; it re-fetches instead — the "graph keeps refreshing" the adapter's remarks attribute to `ResizeObserver` has this second cause) |
| `PromptDraftSurface.cs` | Class A candidate: bottom-docked bars beside a TextBox | ruled out | the *writer* (`_text`) is the fill child; docked items are fixed-height bars, not content-sized readers |
| `TerminalSurface` / `TerminalView` | HwndHost-style host beside chrome | ruled out | no `Dock.Bottom`, no `Loaded` init; session started explicitly |
| `ConsoleSurface`, `ExplorerSurface`, `NodeReaderView` | web/reader hosts | ruled out | no content-sized reader docked against a filling input; no `Loaded` init |
| `MainWindow.Loaded`, `TextPromptDialog.window.Loaded` | `Loaded` init | ruled out | a `Window` is never re-parented; `Loaded` fires once |
| `ComposerMessageRouter.Ready` once-gate | Class B's second half | confirmed (part of finding 2) | `router drops=2` after the reload |

**Markers harvested:** one `simplify:` in the subsystem (`CanvasSurface.cs:211`, *two web-hosting
idioms coexist; ceiling: exactly these two surfaces; trigger: a third web surface*). Not past its
ceiling by count, but the class it predicted has arrived by another route — the same defect in both
copies. No `assume:` markers in the composer.

## Phased repair plan

| Phase | Repair item (code + tests) | Failure mode it eliminates | Validation | Depends on |
|---|---|---|---|---|
| 1 | **Writer first.** `ComposerSurface` layout → Grid; editor `*` with a `MinHeight`; compiled view `MaxHeight` = 35% of the surface, scrolling inside; `CompiledShareCeiling`/"writer ≥ reader" as the stated rule. Tests: `TheComposersEntryAreasKeepTheirRoomAfterTheNewSessionChoreography` green; a headless STA layout test in `AiDe.App.Tests` (surface at 485px and 1000px with a 30-line compiled text, no WebView2 init) asserting the rule in the cheap ring. | Finding 1 — the entry areas have no room; the editor shrinks as the draft grows | probe exit 0 with `writer >= reader: True`, `fields=6 editors=3`, `reached draft=True`; STA test red on today's control, green after | — |
| 2 | **Initialise once; readiness per document.** `_initialised` guard in `ComposerSurface.InitialiseAsync` and `CanvasSurface.InitialiseAsync`; router readiness reset on `NavigationStarting` (or a per-navigation nonce carried in `editor.ready`), so a genuine reload re-mounts and a re-parent does nothing. Tests: `TheComposerPageSurvivesALaterRender` green; probe asserts *canvas sibling: navigations … = 0*; router unit test: ready after a declared navigation is a mount, not a drop. | Finding 2 — the next layout command kills the page silently; every message routed N times; the canvas reloads per render | probe exit 0; `router drops=0` after a render; navigations +0 | — (independent of 1) |
| 3 | **Instrumentation on the normal path (IO1–IO12).** `WorkbenchDiagnostics.ComposerLayout` (surface, editor/compiled/composer heights) at first layout and on any layout pass where either changes by > 24px; `WorkbenchDiagnostics.ComposerHandshake` for every transition — `page-ready`, `configured`, `init-pushed`, `ready-dropped`, `navigation-started` (with count), `re-attached`; a `composer.input` counter (accepted `draft.changed`) and `composer.drops`; a status-line sentence when no fields rendered (*"the editor has not reported ready"*), never a blank box. Every value degrades to *not recorded*. Tests: the probe reads the emitted lines and asserts the transitions and the bounds. | The class survived because nothing measured bounds, transitions or input; the operator's log ends at the gesture | log lines present in the probe run; `pack-doctor`/gate unchanged | 1, 2 (emit what they change) |
| 4 | **Class prevention.** (a) the *writer ≥ reader* helper applied to every input-host surface (composer, prompt draft) as a table test; (b) one `WebSurfaceHost` used by composer and canvas (the `simplify:` upgrade), so a third surface cannot repeat Class B; (c) an analyzer-style test that fails on `Loaded +=` with side effects on a non-`Window` element outside the host; (d) register DC classes A and B (ids from `verify-id-allocators.py`) with these controls, observed failing on the un-fixed code. | Recurrence in the next surface | tests red against a deliberately regressed copy, green on the tree | 1, 2 |
| 5 | **Keyboard entry (F5).** Entering the composer by keyboard focuses the first field (as `CanvasFocusRouter` does for the canvas); page: `editor.ready` → focus first `.cm-content` when the host says the surface was entered by keyboard. Test: probe `Keyboard.Focus(view)` → page active is `cm-content`, OS keystroke reaches the draft when foreground is held. | Focus enters the browser and lands on `BODY`; a keyboard user types into nothing | probe line `page active='cm-content'` | 1 |
| 6 | **Page-side contrast floor (F4).** `.editor` border and input backgrounds ≥ 3:1 against the page (WCAG 1.4.11); drop-hint ≥ 4.5:1; the probe's contrast script becomes an assertion; consider feeding `composer.html` to `ui-craft-gate` (it never scans the page). | Fields are imperceptible boxes even when they have room | probe `editor-border-vs-page ≥ 3.0` | — |

> **STOP — human review gate.** `/investigate` ends here. Phases 1–3 are the repair the operator
> asked for; 4 is the control; 5–6 are findings from the same measurement. Approve which phases run.

## Findings not chased (recorded for their owners)

- **F3 — Send refusal names no remedy.** *"no write scope could be derived from this draft"* does not
  say that an `@path` mention satisfies it (UX, Addendum C).
- **F4 — page-side non-text contrast** (numbers above): boundaries 1.51:1 and 1.13:1; drop-hint 4.47:1.
- **F5 — keyboard entry lands on `BODY`** (measured: `foreground held=True, SendInput injected=2,
  reached draft=False, page active='BODY'`).
- **F6 — tests write into the operator's log.** `verify-test-run.py` (17:32–17:41Z) left hundreds of
  `terminal.start` lines with fixture ids in `workbench-20260911.log`; a test run and an operator's
  session share one file.
- **F7 — probe hygiene.** The arrangement's terminal child inherits the probe's console: PowerShell's
  screen-reader warning and CLIXML land in the xunit failure text; the probe should redirect or
  the terminal should not inherit stdio.
- **F8 — the operator's UX verdicts** on mandatory fields and form-then-render (their words, owned
  by Addendum C).
- **Operator questions (answered from evidence where possible):** window size and monitor at
  17:41Z (unknown — completes the arithmetic); whether clicking a *visible* field placed a caret
  (the screenshot's typed text says yes).

## Residual risk & follow-ups

- The operator's exact geometry is Inferred from a screenshot; the mechanism is Verified at two
  heights. **What would change the diagnosis:** an operator run where the editor host measures well
  above the compiled view and the fields are still invisible — Phase 3's bounds line is what would
  show it.
- The probe cannot deliver an OS keystroke into a *clicked* field; the CDP path proves page→host,
  the operator's screenshot proves OS→page for a reachable field.
- INV id allocated as `INV-0007` against `origin/main` and 28 branches; a sibling investigation
  (`investigate/contrast-census`) is live in another worktree and may allocate concurrently
  (DC-013's shape) — re-run `verify-id-allocators.py` at merge.
- Register entries for classes A and B are **proposed here, registered with the fix** (CI1–CI6), ids
  from the allocator then.

## Gate record

`GATE investigate · 2026-09-11 · SRE (lead), Test Architect, Distributed Systems (re-parent/reload ordering), UX & Accessibility (F4/F5) · root cause verified: yes — necessary (cap → 202px, exit 0) and sufficient (0/105/110px across three runs) · second defect verified by reproduction (render → fields=0) · verdict: STOP for review · vetoes→resolution: Test Architect — "the red must be observed in the product's composition, not a bare window": satisfied by the shell probe under the recorded arrangement; SRE — "a fix without the bounds/handshake telemetry is not done": Phase 3 is not optional; investigator did not self-clear the fix — none was made.`
