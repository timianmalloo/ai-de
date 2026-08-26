# Spike result — avalondock-a11y (the ADR-0012 UIA probe)

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303 · `Dirkster.AvalonDock` 5.0.0
- **Command:** `dotnet run --project spikes/avalondock-a11y`
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt)

## What this probes, and why it is not the same question ADR-0012 already answered

ADR-0012 established by reflection that AvalonDock ships **zero `AutomationPeer` types**. That is a
fact about the assembly. It is *not* the same as "what does assistive technology actually see",
because WPF synthesises a generic peer for any `FrameworkElement` that does not supply one — so the
tree could still have been perfectly usable. This spike asks the second question by walking the same
UIA tree Accessibility Insights reads, against a real running AvalonDock window, from a separate
process. A plain WPF `GridSplitter` and `TabControl` sit in the same window as the **control
baseline**, so every finding is a comparison rather than an absolute.

## Findings

**1. The AvalonDock splitter IS in the UIA tree — and is unusable.** (Corrected mid-spike: the first
pass reported "not present", which was a lookup miss on `ClassName`, not a fact. Enumerating every
`Thumb` found it.)

| | AvalonDock splitter | WPF `GridSplitter` (baseline) |
|---|---|---|
| ClassName | `Thumb` | `GridSplitter` |
| Name | **`''` (empty)** | `Baseline WPF GridSplitter` |
| AutomationId | `''` | `BaselineGridSplitter` |
| KeyboardFocusable | **False** | **True** |
| Patterns | `[SynchronizedInput]` | **`[Transform, SynchronizedInput]`** |
| Bounds | 9 × 715 (real, hit-testable) | 1478 × 9 |

So a screen-reader user encounters an **unnamed, unfocusable Thumb with no `Transform` pattern**.
There is nothing to announce, nothing to tab to, and no programmatic way to move it. The baseline in
the same window has all three. **This confirms ADR-0012's central risk empirically: pane resize is
mouse-only, and it is the *only* element in the whole window with that problem.**

**2. A finding ADR-0012 did not anticipate — the tab names are the .NET type names.**

```
TabItem/'AvalonDock.Layout.LayoutDocument'
TabItem/'AvalonDock.Layout.LayoutAnchorable'
```

The host set `Title = "Explore"`, `"Domain"`, `"Provenance"`, `"Health"`. None reached the
accessibility tree. A screen reader announces **"AvalonDock.Layout.LayoutDocument"** — the result of
`ToString()` on the layout model object — for every tab, so all four surfaces sound identical and
none is identifiable. The baseline `TabItem`s in the same window correctly report `Baseline Tab A`
and `Baseline Tab B`.

This is arguably **worse than the splitter gap**: the splitter is one control that can be replaced,
while every surface in the workbench being anonymous defeats navigation entirely. It was invisible to
the reflection probe because it is a *data-binding* defect, not a missing type.

**3. Everything else in the tree is serviceable.** 44 elements; tabs are `Tab`/`TabItem` with correct
control types and selection; buttons, lists and text all appear. Excluding the two findings above,
the tree's *shape* is fine — the defects are naming and one control's peer, not the structure.

**4. Only one element in the entire window exposes `TransformPattern`** — the baseline `GridSplitter`.

## Verdict against ADR-0012

**The ADR's decision stands; its work estimate does not.** AvalonDock remains the right choice on
licence, maintenance, .NET 10 and serialization, and the mitigation shape is unchanged — layout
operations are already `ICommand`s and `DockWidth`/`DockHeight` are settable, so keyboard resize as a
command still satisfies SC 2.1.1. But the accessibility layer is **larger than "add a resize
command"**:

| Work item | Newly known? | Why |
|---|---|---|
| `ResizePane` command + keyboard binding | No — in ADR-0012 | Splitter has no `Transform` and is unfocusable |
| **`AutomationProperties.Name` bound to `Title` on every tab** | **YES — new** | Titles do not reach UIA at all; every surface is announced as its type name |
| Named/labelled splitter for the "which edge" affordance | Partially | The splitter has no Name, so the mockup's "edge selected" state has nothing to announce against |
| Restyle or subclass `LayoutGridResizerControl` for focus | Optional | Only needed if we want a *focusable splitter*; the command path does not require it |

**Recommendation:** keep ADR-0012, and amend its accessibility work item to carry the tab-name
defect as a **Blocker-severity** item rather than folding it under "naming".

## The fix: one obvious approach failed, a second works (both tested)

The spike did not stop at "recommend a fix" — a recommendation that has not been run is a guess.

**Candidate A — a typed `TabItem` style in `DockingManager.Resources` binding
`AutomationProperties.Name` to `Title`. FAILED.** Names remained `AvalonDock.Layout.LayoutDocument`.
The style does not reach the tab items AvalonDock realizes, even though they report
`ClassName='TabItem'`. Recorded because it is the approach anyone would reach for first, and knowing
it fails is worth as much as knowing what works.

**Candidate B — a visual-tree pass on `LayoutUpdated` that sets `AutomationProperties.Name` on each
realized `TabItem` from the `LayoutContent.Title` it is bound to. WORKS**
([`RESULT-raw-fixed.txt`](RESULT-raw-fixed.txt)):

```
TabItem/'Explore'      TabItem/'Domain'
TabItem/'Provenance'   TabItem/'Health'
```

~15 lines, **app-side, no fork required** — which is the load-bearing conclusion: the defect does not
threaten ADR-0012's licence or maintenance position. The cost is that the pass must re-run whenever
the layout changes, so it belongs in the Workbench Layout Service's adapter rather than in a
one-off startup hook.

**Regression control:** assert no automation name in the workbench begins with `AvalonDock.`. This
exact defect would otherwise return silently on any library upgrade, and it is invisible to
reflection — it is a data-binding defect, not a missing type.

## What this spike does *not* establish

- Whether NVDA or JAWS **speak** these names. This reads the UIA tree, which is what those clients
  consume, but it is not a screen-reader session.
- Behaviour of floating windows, auto-hide flyouts, or the docking overlay — none were open during
  the probe.
- Whether the splitter can be made focusable without forking. Not attempted: the command-driven
  resize path in ADR-0012 does not require it, and the spike's job was to size the gap, not close it.
