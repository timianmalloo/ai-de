---
id: adr-0012-docking-shell-library
title: "ADR-0012 — Adopt AvalonDock for the workbench shell, with an owned accessibility layer"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, ui, docking, accessibility, wpf, licence]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0008-shell-host, rel: relates-to }
  - { to: spec-ai-native-ide, rel: implements }
review-by: 2027-02-26
summary: >-
  The dockable workbench is built on AvalonDock 5.0.0 (MS-PL, net10.0-windows), whose layout
  serialization is best-in-class — but which ships zero UI Automation peers and a mouse-only
  splitter. Adoption is conditional on an owned accessibility layer (command-driven layout
  operations including resize) and a versioned layout envelope, both specified here.
review-suggested:
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
---

# ADR-0012: Adopt AvalonDock for the workbench shell, with an owned accessibility layer

- **Status:** Proposed
- **Date:** 2026-08-26
- **Deciders:** Product owner, Enterprise Architect, UX & Accessibility, Security & Identity
- **Context spec/architecture:** docs/architecture.md · docs/specs/ai-native-ide.md

## Context

The spec now requires a dockable, resizable, multi-pane workbench (layout tree → dock stack →
tabbed surfaces). Building a docking manager from scratch is not viable: AvalonDock's WPF control
library alone is **172 `.cs` files / ~1.45 MB of C#** (~35–40k lines) excluding its core,
serializers and themes — a multi-month project for a single developer, and squarely the
Gratuitous-Reinvention the Solution-Selection Ladder exists to prevent.

The product is under a **WCAG 2.2 AA obligation** (spec Part C; UX & Accessibility holds the
accessibility hard veto), is single-developer and open-source-friendly, and must **persist and
migrate user layouts across upgrades**. Those three constraints — not feature count — decide this.

## Decision

We will build the workbench shell on **AvalonDock 5.0.0** (`Dirkster.AvalonDock`, MS-PL), and we
will **own an accessibility layer on top of it** as a first-class, tested part of the shell rather
than a later hardening pass. Concretely:

1. **Every layout operation is reachable from the keyboard and the command palette.** AvalonDock
   already exposes 14 operations as `ICommand` (`HideCommand`, `FloatCommand`, `DetachToWindowCommand`,
   `AutoHideCommand`, `DockCommand`, `DockAsDocumentCommand`, `CloseCommand`, `CloseAllButThisCommand`,
   `ActivateCommand`, `NewVertical/HorizontalTabGroupCommand`, `MoveToNext/PreviousTabGroupCommand`, …).
2. **We add the one command AvalonDock lacks: resize.** `DockWidth`/`DockHeight` are public settable
   `GridLength` on `LayoutAnchorablePane`/`LayoutDocumentPane` (verified by execution), so a
   `ResizePane` command mutates them directly. This satisfies SC 2.1.1 — *functionality* must be
   keyboard-operable; the success criterion does not require the splitter widget itself to be
   focusable.
3. **Panes carry `AutomationProperties.Name`** applied through styles, so assistive tech can identify
   a pane even though AvalonDock supplies no peer for it.
4. **Layout is persisted inside our own versioned envelope** — `{ schemaVersion, appVersion, payload }`
   wrapping AvalonDock's DTO — because `LayoutRootDto` has **no version or schema field** (verified
   from source). Without this there is no migration hook later, and layouts are user data we have
   promised an export/recovery path for.

## Alternatives considered

- **Build in-house on WPF primitives:** rejected as a whole — ~35–40k lines. **But partially adopted:**
  WPF's own `GridSplitter` declares `OnKeyDown` *and* `OnCreateAutomationPeer` (both verified),
  giving keyboard resize and a `Transform` pattern for free — exactly what AvalonDock lacks. Its
  behaviour is the model our `ResizePane` command imitates, and it remains the fallback if
  AvalonDock's splitter proves unfixable.
- **Dock (wieslawsoltes), MIT, very active, net10.0:** rejected — **Avalonia only**; a full recursive
  listing of `master` returns zero WPF paths. It would be the best permissive answer *if the UI stack
  itself were reopened*, which ADR-0008 settled on WPF. Recorded so a future Avalonia reconsideration
  starts here.
- **Syncfusion (DockingManager):** rejected as the default despite being **materially more accessible**
  (61 automation-peer types, 44 `OnCreateAutomationPeer` overrides, verified). Its Community Licence
  is free only while revenue < $1M, ≤5 developers, ≤10 employees and < $3M raised — an **eligibility
  cliff** that converts a growth event into an unplanned architecture migration. Its layout format is
  also `Name`-keyed with no specification or versioning, and its documented persistence path still
  teaches `BinaryFormatter`, which throws on .NET 9+. **Named as the fallback if accessibility becomes
  existential and the eligibility terms are met.**
- **Telerik RadDocking:** rejected on cost ($749–1,249/dev/yr). Noted as the **reference standard for
  accessible docking** — documented UIA peer tree, tested with Accessibility Insights, `Ctrl+Tab` /
  `Alt+F7` pane navigation. Our accessibility layer should aim at what Telerik documents.
- **DevExpress / Infragistics / Xceed / MESCIUS:** rejected on per-seat commercial cost.
- **Actipro Docking & MDI** (~$440/dev, perpetual): rejected for now — its `net10.0-windows` TFM exists
  only in `26.1.0-rc.5` (pre-release). **Re-evaluate if that ships GA**; a perpetual licence at that
  price is a better model than any subscription here.
- **Dragablz** (MIT): rejected — dead (last release 2022, max `netcoreapp3.0`) and tab-tearing only,
  not a docking manager.

## Consequences

- **Positive:** a professional docking workbench without owning 40k lines; a serialization layer with
  public DTOs, XML *and* JSON, a custom-serializer hook, and deliberate tolerance for legacy files; a
  free, royalty-free licence; verified `net10.0-windows` support.
- **Negative / accepted trade-offs:**
  - **MS-PL is weak (file-level) copyleft, and GPL-incompatible.** Consuming the binary is
    unrestricted; *forking the source obliges us to keep those files MS-PL*. Fine under MIT/Apache-2.0
    for our own code; a conflict only if AI-DE ever ships under GPL. Recorded so that is a conscious
    future choice, not a surprise.
  - **Accessibility is deferred work we now own**, not a capability the library provides.
  - **Bus factor ≈ 1** — one maintainer wrote 78 of the last ~95 human commits, and the project went
    dark for 32 months (2023-08 → 2026-04) before four releases in 2026.
  - **v5.0.0 is a 14-day-old breaking major** (serialization rewritten, packages renamed, floating
    windows reworked). We pin the version and track issues for a release or two.
- **Follow-ups / new risks:** the accessibility layer needs its own red-first tests (keyboard path to
  every layout operation, including resize); multi-monitor and per-monitor-DPI floating behaviour is
  **Inferred, not verified**; ganged resize is **Flagged**.

## Evidence

Verified **by execution** on .NET 10 SDK 10.0.303 against the shipped 5.0.0 assembly:

- `lib/net10.0-windows7.0/AvalonDock.dll` present; TFM attribute `.NETCoreApp,Version=v10.0`.
- `AutomationPeer`-derived types: **0**. Types overriding `OnCreateAutomationPeer`: **0**.
  Repo-wide `AutomationPeer` search: **0 hits**.
- `public class LayoutGridResizerControl : Thumb` — **public, not sealed, constructible**; the source
  file contains no `OnKeyDown`, `Focusable`, `KeyDown`, or `Automation`.
- `DockWidth` / `DockHeight` are public **settable** `GridLength` on `LayoutAnchorablePane` and
  `LayoutDocumentPane`; `IsMaximized`, `IsVisible`, `FloatingWidth/Height` likewise settable.
- `LayoutAnchorableItem` exposes the 14 `ICommand` layout operations listed above.
- `System.Windows.Controls.GridSplitter` declares **both** `OnKeyDown` and `OnCreateAutomationPeer`.
- `LayoutRootDto` properties are `RootPanel`, `TopSide`, `RightSide`, `LeftSide`, `BottomSide`,
  `FloatingWindows`, `Hidden` — **no version field**.
- Licence: GitHub API `license.spdx_id = "MS-PL"`; the `LICENSE` inside the 5.0.0 `.nupkg` is verbatim
  Ms-PL. Release 5.0.0 published 2026-08-12.

Sources: [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) ·
[Dirkster.AvalonDock on NuGet](https://www.nuget.org/packages/Dirkster.AvalonDock/) ·
[v5.0.0 release notes](https://github.com/Dirkster99/AvalonDock/releases/tag/v5.0.0) ·
[AvalonDock serialization guide](https://github.com/Dirkster99/AvalonDock/blob/master/docs/guides/serialization.md) ·
[FSF licence list (Ms-PL)](https://www.gnu.org/licenses/license-list.html) ·
[Syncfusion Community Licence](https://www.syncfusion.com/products/communitylicense) ·
[Telerik WPF UI Automation support](https://www.telerik.com/products/wpf/documentation/common-information/common-ui-automation) ·
[BinaryFormatter migration guide](https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-migration-guide/)

### UIA probe result (2026-08-26) — the ADR survives, the work item grew

The "does a real UIA client see a usable tree?" spike **has been run** — see
[`spikes/avalondock-a11y/RESULT.md`](../../spikes/avalondock-a11y/RESULT.md). It walked the live UIA
tree of a real AvalonDock window from a separate process, with a plain WPF `GridSplitter` and
`TabControl` in the same window as the control baseline.

**Confirmed:** the splitter is in the tree but is an **unnamed, unfocusable `Thumb` with no
`Transform` pattern**, while the baseline `GridSplitter` beside it has a name, keyboard focus and
`Transform`. It is the only element in the window with that problem. The command-driven resize
mitigation above is therefore necessary and remains sufficient.

**Newly discovered, and not anticipated by this ADR:** every AvalonDock tab reports its **.NET type
name** as its accessible name — `AvalonDock.Layout.LayoutDocument` — rather than its title. All four
surfaces sound identical to a screen reader. This was invisible to the reflection probe because it is
a *data-binding* defect, not a missing type, and it is arguably worse than the splitter gap: one
control can be replaced, but anonymous surfaces defeat navigation entirely.

**Fix established by execution, not proposed:** a typed `TabItem` style binding
`AutomationProperties.Name` to `Title` — the obvious approach — **was tested and does not work**. A
~15-line visual-tree pass that names realized `TabItem`s from their bound `LayoutContent.Title`
**does**, verified in the same probe. It is app-side and requires **no fork**, so the defect does not
threaten this ADR's licence or maintenance position; it belongs in the Workbench Layout Service's
adapter and must re-run on layout change.

**Added control:** a regression test asserting no automation name in the workbench begins with
`AvalonDock.` — this defect would otherwise return silently on any library upgrade.

### Round-trip spike result (2026-08-26) — done, and it found a real defect

Run and recorded in [`docs/reviews/spike-layout-upgrade-roundtrip.md`](../reviews/spike-layout-upgrade-roundtrip.md).
Note the question was **re-framed**: ADR-0013 persists our own model rather than AvalonDock's
serializer, so "does `JsonLayoutSerializer` round-trip" is obsolete; the equivalent question for the
architecture we have is "does a saved layout survive a schema change".

It does not, or did not: the envelope had a version field but **no migration hook**, so the first
release to rename a surface would have degraded **every** saved layout to the default — not lost one
pane, reset the whole arrangement. Fixed with a DTO-level migration chain that fails closed on a gap;
pinned by five tests, the headline one observed red.

### DPI and ganged-resize spike result (2026-08-26)

Recorded in [`docs/reviews/spike-dpi-and-ganged-resize.md`](../reviews/spike-dpi-and-ganged-resize.md).

**Ganged resize: holds.** No two docked panes share area, measured geometrically on the realized
view; weights always sum to 1 so a resize redistributes rather than leaving a gap. **Flagged to
Verified.**

**Per-monitor DPI: found a prerequisite defect in OUR code.** The app was `SYSTEM_AWARE` rather than
`PER_MONITOR_AWARE_V2`, because WPF defaults that way absent a manifest and AI-DE shipped none. A
System-aware app bitmap-stretches any window crossing a DPI boundary, so testing the library's
cross-monitor behaviour in that host would have measured our bug and blamed it on AvalonDock. Fixed
with an `app.manifest`, verified against the running executable.

**Still open:** the cross-monitor transition itself was not run — this machine has one display.
Per-Monitor V2 is the precondition and is now verified; AvalonDock's floating-window behaviour across
a real DPI boundary remains **Inferred** and needs a two-display machine.

### NVDA verification (2026-08-26, partial)

**Part A passed:** NVDA correctly announced each tab by name. The tab-naming defect the UIA probe
found is now verified *heard*, not merely present in the tree — the strongest evidence available for
that claim, and it retires the largest accessibility unknown in this ADR. Parts B, C and D
(announcements without focus movement, blind keyboard operation, spoken refusals) are **not yet run**,
so SC 4.1.3 and SC 2.5.7-in-practice remain unverified by a screen reader.

**Required spikes before Phase-2 implementation accepts this ADR:**

| Unknown | Probe | Cost |
|---|---|---|
| Layout round-trip across an app upgrade | Serialize with `JsonLayoutSerializer`, mutate the DTO, reload with `UnresolvedContentHandling.Hide`, assert the parked item restores | ~½ day |
| Multi-monitor + per-monitor DPI floating | Two monitors at mixed scaling: float, drag across the boundary, save, restart, restore | ~1 h |
| Ganged resize | Three-pane split; drag the shared splitter; observe whether both neighbours move | ~30 min |
