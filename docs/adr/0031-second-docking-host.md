---
id: adr-0031-second-docking-host
title: "ADR-0031 — Architecture's body is a second AvalonDock host composed as a DockHost unit (manager · adapter · zone service · controller · rails · persistence) under one presenter; one controller per host, one catalog, one factory"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [architecture, ui-shell, docking, avalondock, perspective, workbench, addendum-c]
links:
  - { to: architecture, rel: implements }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: adr-0017-primary-view-mode, rel: refines }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: adr-0021-named-dock-zones, rel: relates-to }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: note-addendum-cd-web-surface-host-sharing, rel: relates-to }
review-by: 2027-03-11
review-suggested:
  - { by: adr-0017-primary-view-mode, on: 2026-09-11, reason: "ADR-0017 accepted as amended (Ruling 52): the closed set is the Perspective set; a body may be a docking host; second-host clause discharged by spikes/second-dock-host-unparent" }
summary: >-
  The Architecture perspective's body is a second AvalonDock DockingManager with its own
  ZoneBackedLayoutService, WorkbenchAdapter, WorkbenchController, ZoneRails and LayoutPersistence —
  the DockHost unit WorkbenchShell composes today for Coding, extracted and composed twice — under the
  ADR-0017 presenter, sharing one SurfaceContentFactory, one command catalog, one announcer and one
  palette. The Owner's residual (one WorkbenchController or two) is decided: one per host; a
  shell-level PerspectiveShell routes commands to the active host. Verified by
  spikes/second-dock-host-unparent.
---

# ADR-0031: Architecture's body is a second docking host — a `DockHost` unit composed twice, one controller per host

- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 (`addendum-c-chain`); the
  Patterns Expert and Tech Lead in Peer Mode; the Simplifier and Test Architect at the gate
- **Context spec/architecture:** `spec-addendum-c-perspectives` §A6 (Perspective Layout aggregate),
  §B2, US-C2, US-C8; Ruling 52 (b), Ruling 53; the Owner's residual "one `WorkbenchController` or
  two — carried to A1" (`note-addendum-c-council-rulings` §Residual)

## Context

Ruling 52 makes Architecture's body **a second host** of the same docking library (ADR-0012), the
same zone model (ADR-0021), holding only the kinds ADR-0030 admits there. Today the one host is
composed inside `WorkbenchShell` (2,967 lines): `Service = new ZoneBackedLayoutService()`
(`WorkbenchShell.cs:113`), `Manager = new DockingManager()` (`:164`), `Adapter = new
WorkbenchAdapter(Manager, Service, surface => _factory.Create(surface))` (`:171`), `Controller = new
WorkbenchController(Service, Announcer)` (`:172`), `_rails = new ZoneRails(Manager, …)` (`:178`),
`Persistence = new LayoutPersistence(…)` (`:387`, `:664`) — and beside them everything
**workspace-level**: the `SurfaceContentFactory` (`:138`), the watcher, the session documents the
shell owns (`:60-68`), dispatch, the canvas binding (`BindCanvas`, `:1221`) **[Verified]**.
`WorkbenchController` is bound to one `ILayoutService` and carries per-host state — `FocusedStackId`,
`FocusedSurfaceId`, a `KeyboardResizeSession` over that service (`WorkbenchController.cs:17-25`)
**[Verified]**.

The retain-never-rebuild invariant must hold for the second host. Ruling 52 labelled that
**Inferred**; discharged by `spikes/second-dock-host-unparent` — the result is narrated once, in
ADR-0017's amendment **[Verified — run, exit 0]**.

LOA principles in play: P2 (the host is deterministic UI), P7 (state lives at the edges — each host's
layout state lives in its own service and slot, never in the presenter).

## Decision

We will:

1. **Extract the per-host composition into a `DockHost` unit** — `{DockingManager, WorkbenchAdapter,
   ZoneBackedLayoutService, WorkbenchController, ZoneRails, LayoutPersistence}` — built by one
   factory method from the shared `SurfaceContentFactory`, the shared `WorkbenchAnnouncer` and a
   **perspective row** (ADR-0030), and **compose it twice**: host A (Coding — today's instances,
   behaviour unchanged) and host B (Architecture). `WorkbenchShell` keeps everything workspace-level
   and owns both units.
2. **One `WorkbenchController` per host** (the Owner's residual, decided). The controller's job is
   layout-command dispatch over *one* service with per-host focus and resize state; a second host is a
   second controller over its own service. **One command catalog, one palette, one menu builder**: a
   shell-level **`PerspectiveShell`** presenter (ADR-0017's `ShellModeController`, generalised to the
   three-row set) holds the active perspective, the three bodies, the *previous-perspective* slot
   (US-C1), and **routes** `Execute(id)`: body-conditional commands go to the active host's controller;
   entry verbs (`session.new`, `terminal.new`, the harness rows) go to **host A and then activate
   Coding as one transaction, document first** (US-C5, US-C11); perspective commands switch; a routed
   kind-open resolves via ADR-0030 and opens in the resolved host. **The opener delegates' fate
   is named** (the Tech Lead's finding — the extraction's largest per-controller surface):
   `WorkbenchController` carries thirteen `*Requested` opener delegates (`WorkbenchController.cs:
   47-58, 470-503`) wired in `WorkbenchShell.cs:200-359` and `MainWindow.xaml.cs:92-100`; with two
   controllers they **collapse to one routed `OpenKind(kind)`** — ADR-0030's `Resolve` — **wired
   once in the shell**; the falsifier is a per-kind delegate re-wired on host B's controller (a test
   over the fake controllers). The window's shell-level delegates (`NewSessionRequested`,
   `WorkspaceOpen`) move to the presenter, wired once. **Bound (the Tech Lead's ruling on the
   Simplifier's alternative — routing in `WorkbenchShell` via an `ActiveHost` member):** the
   presenter already holds the mode, and the alternative puts the same logic in `MainWindow`; so
   the router stays in the presenter with this bound — active/previous, the three bodies,
   `Execute(id)` = resolve the host then delegate to `WorkbenchController.Execute`, the three
   perspective commands, the entry-verb rule — and **a `switch` on any other command id inside the
   presenter is the falsifier**. **INV-0009's first class is this rule's first red:** a dock document
   opened while Explore is the body must switch the body to the admitting host, document first —
   the `DocumentOpening` seam the fix adds on `WorkbenchShell` is the walking skeleton of this
   routing (ADR-0017 amendment clause 4), and `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` is
   the test that stays green through the C-1 extraction. Two adjacent INV-0009 findings the slice
   carries: **INV-0009's second class** — a session created through the workspace chooser with no workspace open is
   bound to a workspace the window has not opened; the operator ruled *the chooser opens the
   workspace, then creates* (`NewSessionFlow` runs `OpenWorkspaceAtAsync(root)` before `opened`),
   never a refusal; and **the reopen path's binder** — `BindComposer` leaves `MainWindow` for
   `Workbench.Sessions.SessionComposerBinder.Bind(shell, config, providers, workspace,
   affirmation)`, the **one** construction site New, Reopen and restore share (still one registry /
   send-context / attachment-gate site — Ruling 47), which is also where `ComposerSendContext.
   TaskClass` is populated from the session's `default_task_class` (ADR-0033 rule 4).
3. **The allow-list is enforced by each host's layout service at open, restore and every mutation**
   (the Perspective Layout aggregate's invariant): `ZoneBackedLayoutService` gains the admitted-kind
   set as a constructor argument and refuses an inadmissible surface with a reported result — the
   same `RestoreResult`/announcement channel that reports a dropped surface today (US-C3, US-C9).
4. **Host B's default layout** is §B4's (Center: Graph · Domain · Contexts · Joins as tabs; Left:
   Evidence; Right: Provenance; Bottom: collapsed) built by the same `WorkbenchLayout.Default()`
   successor host A uses, parameterised by the perspective row. The Architecture graph is a **second
   `CanvasSurface` instance with the kind filter `code · data · architecture` on its neighbourhood
   query** (Ruling 53) — the same substrate and query, no second store; the `view`/`inspector` pair
   gains its selection-source seam so `inspector` renders the selected row's detail (US-C6, Ruling 61).
5. **Web surfaces in host B share `WebSurfaceHost`** (DC-138's once-gate keyed to the surface's
   lifetime) — the spike observed `Loaded` firing twice on host B's first attach before any presenter
   cycle; see `note-addendum-cd-web-surface-host-sharing`.

## Alternatives considered

- **One `WorkbenchController` whose `Service` is swapped on perspective switch:** rejected — the
  controller holds a `KeyboardResizeSession` bound to a service and per-host focused ids; a switch
  mid-flow would leave a resize session over the wrong service and a focused stack id from the other
  host (the boundary set names "switch requested mid tab-drag" and "during a lazy build"). Two thin
  controllers are cheaper than a controller that knows about switching.
- **A controller per perspective including Explore (a null-object controller for a non-host):**
  rejected — a scaffold for a body that has no service; the presenter's body-kind rule already says
  "no host commands in Explore" (ADR-0030 rule 3).
- **Architecture as a non-docking composite (a fixed grid of the admitted surfaces):** rejected —
  Ruling 52's fallback *only if* the second host restarts a hidden `HwndHost`; the spike shows it does
  not. A composite would also duplicate the zone model's collapse/float/keyboard layer (ADR-0012's
  owned accessibility) for one body.
- **One `DockingManager` whose layout is swapped per perspective (`Manager.Layout = …`):** rejected —
  AvalonDock re-parents every pane on a layout replacement (DC-138), so a switch would tear down
  Coding's panes into a new layout object each time: the exact rebuild the invariant forbids, and
  the Explorer swap's whole reason for being a *content* swap.
- **Extending `WorkbenchShell` in place with `ManagerB`, `ServiceB`, … fields:** rejected — twelve
  parallel fields in a 3,000-line class; the `DockHost` unit is the same code with one constructor,
  and the second composition is what forces the extraction to be honest (a member that host B cannot
  take is a workspace-level member, not a host one).

## Consequences

- **Positive:** the second host is *composition*, not new mechanism; every layout test that runs
  against `ZoneBackedLayoutService` runs unchanged against host B's service; the Coding host's
  instances and behaviour are byte-for-byte today's (the refactor is proven by the existing
  `ZoneLayoutPersistenceTests`/`LayoutPersistenceTests` staying green and by US-C2's identity test).
- **Negative / accepted:** a second live `DockingManager` and its retained panes hold resources while
  hidden (ADR-0017's accepted cost, doubled and bounded by the admitted-kind set); the
  `WorkbenchShell` refactor is the largest single edit in Addendum C and is P1's first Coding node
  after the perspective mechanism (Ruling 54's order: Coding, mechanism, Explore, Architecture).
- **Follow-ups / new risks:** P-7 (a bound gesture pressed with focus inside a hosted HWND may never
  reach WPF) is a runtime item the slice measures — **re-targeted**: the spec's premise names the
  terminal, but the terminal is WPF-drawn (`TerminalSurface : ContentControl`,
  `TerminalView : FrameworkElement`; `grep HwndHost src` names no terminal file); the real `HwndHost`s
  are the WebView2 pages (the Explore body, host B's canvas, the class diagram, the composer), so P-7
  presses `Ctrl+1/2/3` with focus **inside a WebView2 page** (Explore's body; host B's canvas) with
  the terminal as a second row (the Test Architect's finding; `ComposerSurface.cs:736` already
  republishes `AcceleratorKeyPressed`, which is the mechanism to reuse). If it fails, the web
  surface forwards the perspective gestures — `/design-slice`'s decision, not this ADR's. The
  hidden host's footprint is **measured, not accepted blind**: P-4 records the `msedgewebview2`
  process count and the app's private bytes before host B's first entry, after it, and after three
  cycles, and the `first_entry` switch event carries `private_bytes_delta` (the SRE's finding). The
  DC-135 ratio (tests over
  `LayoutService` vs `ZoneBackedLayoutService`, 70:19 at the inventory) is a standing risk this
  refactor must not widen: every new host test constructs the zone-backed service.

## Falsifying tests

1. **Identity, three bodies (US-C2, headless):** the presenter holds host A, host B and the Explore
   surface; cycling Coding → Architecture → Explore → Coding yields reference-identical objects and
   no factory invoked twice. Re-scopes `ExplorerModeTests` to the three-row set.
2. **P-4 (runtime Proof Pack):** a class diagram in host B keeps its selected node id and scroll
   offset across a full cycle; the `WebSurfaceHost` of every web surface in host B reads
   `InitialisationsStarted == 1` after the cycle (in the product, where `WebSurfaceHost` is the
   gate — the spike's "once" was enforced by its own guard, so its load-bearing oracles are
   `CoreWebView2` identity, page state, scroll and the raw HWND's identity); the WebView2 process
   count and private bytes are recorded before/after first entry and after three cycles. The spike is
   this test's green path for the presenter mechanism; the slice re-runs it against the product,
   including two shapes the spike did not cover — a live WebView2 in the Explore body as well as in
   host B, and host B's WebView2 in a **non-selected** tab across a cycle.
3. **Routing (headless):** with Architecture active, `Execute("workbench.moveSurface")` reaches host
   B's controller and not host A's (a recording fake per host); `Execute("terminal.new")` opens the
   terminal in host A's Bottom zone *and then* activates Coding, in that order, as one transaction;
   a create failure leaves the active perspective unchanged (US-C5, US-C11).
4. **Allow-list at the service (headless):** any path placing a `sequence` surface into host A's
   service — command, restore, programmatic add — is refused and reported; the host never contains
   it (US-C3 b1).
5. **Substrate (headless):** a recording `FakeWorkspaceQueries` sees the kind filter
   `code · data · architecture` on every `GraphQuery` host B's canvas issues; no second store type
   exists (US-C8).

## LOA mapping

T0. Patterns (named as the Patterns Expert corrected them): **Factory Method (parameterised)**
returning a **`DockHost` record** — `DockHost.Create(perspectiveRow, sharedFactory, announcer)`,
**decided over "call the six constructors twice"** (the Tech Lead's ruling: twice-six is twelve
fields plus the wiring block twice in a 2,967-line class; the record gives DC-135 a denominator and
makes "a member host B cannot take" mechanical; bound: a positional record plus `Create`/`Dispose`,
no behaviour); no Builder (no optional parts, no stepwise construction) and not GoF Composite (no
part–whole tree with a uniform component interface); **Module** (the presenter owns two units); **Presenter (body-content
swap)** (ADR-0017's lineage); **State + Command Router** for `PerspectiveShell` (behaviour varies
with the active row; hosts A and B never interact, so it is not a Mediator); the entry-verb sequence
(document first, then Coding; a failure leaves the perspective unchanged) is a **fail-fast ordered
command**, not a transaction; **Table-Driven Method** (default layout and admitted kinds per
perspective row). C7 fallback: a host whose body fails to build leaves the other perspectives usable
and the rail item in its error state (Addendum C §A9 Reliability).

## Evidence

- **Verified (run):** `spikes/second-dock-host-unparent/RESULT.md` — exit 0; all checks pass.
- **Verified (read):** `WorkbenchShell.cs:113, 138, 164, 171-178, 387, 664, 1221` (the composition);
  `WorkbenchController.cs:17-25` (service binding, focus and resize state);
  `MainWindow.xaml.cs:57-100` (the presenter wiring and the delegate hooks); `TerminalSurface.cs:32`
  (`ContentControl` — WPF-drawn, not an `HwndHost`); `ExplorerModeTests.cs:21-60` (the `Border`
  stand-in T1).
- **Rulings:** 52 (b), 53, 55d, 59–61; the Owner's residual.
