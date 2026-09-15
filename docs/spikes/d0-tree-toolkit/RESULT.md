---
id: spike-d0-tree-toolkit
title: "Spike — D-0 Solution tree toolkit (WPF TreeView)"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [spike, D-0, solution-tree, treeview, wpf, N7, understanding-views]
links:
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: depends-on }
  - { to: spec-understanding-views, rel: depends-on }
review-by: 2027-03-15
summary: >-
  N7 Spike Protocol (read + run on installed WPF net10.0-windows / PresentationFramework
  10.0.0.0 / .NET 10.0.11): freeze WPF TreeView as the D-0 toolkit. HierarchicalDataTemplate
  over a VM-nested forest of the flat SolutionTreeResult.Nodes list. Opt-in recycling
  virtualization, 28px header content (never Height on TreeViewItem), AutomationProperties.Name
  bound to kind+coverage, PreviewKeyDown for Enter / Ctrl+Enter. Custom ListView/ItemsControl
  and WebView2 HTML trees are rejected.
---

# Spike — D-0 Solution tree toolkit

- **Question.** ADR-0038 left the tree control unfrozen. Can installed WPF `TreeView` /
  `TreeViewItem` render F* hard states with UIA Name containing kind+coverage, a 28px full-row
  hit, keyboard Enter / Ctrl+Enter / arrows, virtualization, and a non-expanding unindexed leaf —
  or must UV-1 use a custom `ItemsControl` / `ListView`?
- **Standard.** Spike Protocol (read *and* run). Claims are **Verified** (source opened *and*
  measured), **Inferred** (source or mapping only), or **Flagged** (unresolved).
- **Run.** 2026-09-15 · Windows 11 10.0.26200 · `global.json` SDK 10.0.303 · Windows Desktop
  **10.0.11** · `PresentationFramework` **10.0.0.0** · TFM `net10.0-windows` (`UseWPF`, no WPF
  NuGet). Command: `dotnet run --project spikes/d0-tree-toolkit` (exit 0). Raw:
  [`spikes/d0-tree-toolkit/RESULT-raw.txt`](../../../spikes/d0-tree-toolkit/RESULT-raw.txt).
- **Source opened.** `dotnet/wpf` `release/10.0` `TreeView.cs` (static ctor L27–35; `OnKeyDown`
  L506–590) and `TreeViewItem.cs` (`OnKeyDown`; `CanExpand = HasItems`; double-click toggles
  `IsExpanded` without `CanExpand`). App styles: `src/AiDe.App/App.xaml:725-734`. Tokens:
  `DESIGN.md:26,177`. Spec Part B/C dual-activate, 28px, `{colors.unverified}`.
- **Not done.** Product `src/` (except this throwaway). Atlas. D-1…D-6. UV-0 query. Allow-list
  row. WebView2 tree. Attended physical Ctrl+Enter (see residual). Skip-set widen of other
  extractors (optional; not a blocker).

## Chosen toolkit

**WPF `TreeView` + `TreeViewItem`**, bound with a `HierarchicalDataTemplate` to a **VM-nested
forest** of the flat census DTO. Freeze this control for UV-1. It met every load-bearing facet
once UV-1 sets the attachments below. ADR-0038 forbade freezing it in architecture; this spike
is the read+run evidence that now freezes it.

### Attachments UV-1 must ship (not platform defaults)

| Attachment | Why (measured) |
|---|---|
| `VirtualizingPanel.IsVirtualizing=True`, `VirtualizationMode=Recycling` on the `TreeView` | Default `IsVirtualizing=False`, items panel `StackPanel`, **400/400** realized in a 280px viewport. Opt-in: panel `VirtualizingStackPanel`, **18/400** realized; nested expanded folder **17/400** children, `IsVirtualizing` propagated onto the folder. |
| Header content `MinHeight=28` (e.g. a `Border` around the label), **not** `Height=28` on `TreeViewItem` | Default row **16px**. `MinHeight=28` on the item leaves `PART_Header` at **16px**. `Height=28` on an expanded parent **clips children** (`clips_children=True`; item 28, header 16, child 16). Header `Border.MinHeight=28` → header **28px**, width 461, ≥24×24. |
| `ItemContainerStyle` setter `AutomationProperties.Name` → kind+coverage string | Unbound peer `GetName()` is the header only (`Program.cs`). Bound: `Program.cs file-artifact`, `unindexed_probe census-folder Unindexed`. Native `TreeViewAutomationPeer` / `TreeViewItemAutomationPeer`, control types **Tree** / **TreeItem**. |
| `PreviewKeyDown` on the tree for **Enter** (View source) and **Ctrl+Enter** (Reveal in graph) | Platform does **not** handle `Key.Return` (`TreeView.OnKeyDown` / `TreeViewItem.OnKeyDown`). RaiseEvent `Return` on a file item reached the tree handler (`activate=1`, `Handled` stayed false until we handled it). |
| Unindexed: empty `Children`, never a dummy child; swallow double-click | `HasItems=false` → expander `Visibility=Hidden` (16px slot, not shown), UIA `ExpandCollapseState=LeafNode`. Right / Enter / Left do **not** expand. Double-click **does** set `IsExpanded=true` with zero children (source does not check `CanExpand`). |
| Do not rely on `App.xaml` TreeView styles | `App.xaml:725-734` sets Foreground / Background / BorderBrush only. No height, virtualization, template, or Name. |

`App.xaml` implicit styles stay as the theme floor (TC1). UV-1 adds an `ItemContainerStyle` /
template on the Solution-tree instance, not a second global `TreeViewItem` style that would
reshape Console or contrast-probe trees.

## VM nesting rule (flat DTO → tree)

One `SolutionTreeNode` on the wire is exactly one `(Path, Kind)` (`SolutionTreeResult.Nodes` is
flat). The VM does **not** path-split. It indexes census-folders **present in that list** and
hangs each node under the folder whose path is the parent of its path (`""` for a single-segment
path; no parent for the workspace root `""`). Parent of `src/Program.cs` is `src`; parent of
`src` is `""`. If that parent path is not a census-folder in the DTO, a **file-artifact is
dropped** (ADR-0038 join step 4 — no orphan file) and a **census-folder becomes a forest
root** (still shown; `deep` is not invented from `deep/nested`). `HierarchicalDataTemplate.ItemsSource`
binds to each row’s `Children`. Product equality uses `PathComparison.ForThisFileSystem` and `/`
with no trailing slash (**Inferred** mapping: this PoC used `OrdinalIgnoreCase` as a Windows
stand-in).

Measured: happy F*-shaped set nested `src` + `unindexed_probe` under root, `src/Program.cs` under
`src`, unindexed child count 0; `src/App/Program.cs` dropped and `src/App` not invented;
`deep/nested` survived as a second root and `deep` was not invented.

## Findings (confidence ledger)

| # | Claim | Observed | Label |
|---|---|---|---|
| F1 | Installed WPF is `net10.0-windows` / PresentationFramework 10.0.0.0 / runtime .NET 10.0.11 | `MEASURE runtime.*`; `AiDe.App.csproj:4-8`; `global.json` SDK 10.0.303; no WPF package in `Directory.Packages.props` | **Verified** |
| F2 | `TreeView` default is **not** virtualizing | `IsVirtualizing=False`, panel `StackPanel`, ScrollUnit already `Pixel` (source `TreeView` static ctor L30, L35). 400 root items → 400 containers in 280px | **Verified** |
| F3 | Opt-in recycling virtualizes root and nested children | `VirtualizingStackPanel`; 18/400 root; nested folder 17/400 children; `IsVirtualizing` on the folder `True` (propagation helper in `TreeViewItem`) | **Verified** |
| F4 | Native UIA role is Tree / TreeItem | empty tree `TreeViewAutomationPeer` / `Tree`; item `TreeViewItemAutomationPeer` / `TreeItem`; ExpandCollapse pattern present | **Verified** |
| F5 | UIA Name is Header unless `AutomationProperties.Name` is set | unbound `GetName()=Program.cs`; `ItemContainerStyle` binding → `Program.cs file-artifact` and `unindexed_probe census-folder Unindexed` | **Verified** |
| F6 | Default row is **16px**, not 28px | `default.item_height=16`, `PART_Header=16`. Compact 28px is `DESIGN.md:177`, not the platform | **Verified** |
| F7 | 28px full-row hit is the **header content**, not `TreeViewItem.Height` | Header `Border.MinHeight=28` → header 28, width 461, ≥24×24. Item `Height=28` clips children. Item `MinHeight=28` does not enlarge the header | **Verified** |
| F8 | Unindexed leaf does not expand on Right / Enter / Left | `HasItems=false`; keys `Handled=false` and `IsExpanded` stays false; items stay 0. Indexed-parent Right expands (`Handled=true`) | **Verified** |
| F9 | Unindexed expander is **Hidden**, not absent | Toggle found, `Visibility=Hidden`, width 16 (indent reserved). Parent expander `Visible`. UIA state **LeafNode** (not Collapsed) | **Verified** |
| F10 | Double-click on a leaf sets `IsExpanded=true` | `unindexed.dblclick.is_expanded=True` with 0 children. Source: `OnMouseLeftButtonDown` toggles `IsExpanded` with no `CanExpand` check | **Verified** |
| F11 | Enter is not a platform activate | `TreeView.OnKeyDown` / `TreeViewItem.OnKeyDown` have no `Key.Return`. PreviewKeyDown on the tree saw `Return+None` and our file-artifact handler ran (`activate=1`) | **Verified** |
| F12 | Ctrl+Enter is not swallowed by the control | Source: Control+arrows scroll at `TreeView`; Enter is not in that set; `TreeViewItem` skips Left/Right/Up/Down when Control is down. Live chord not injected (`Keyboard.Modifiers` stayed `None` on RaiseEvent; `KeyGesture.Matches` false; command did not fire) | **Verified** (source + Enter bubbling); **Inferred** (physical Ctrl+Enter) |
| F13 | Arrows move / expand indexed-parent | Down from expanded `src` selected `Program.cs`; Right expanded the folder | **Verified** |
| F14 | `HierarchicalDataTemplate` over nested VM is enough | Bound forest: root/src `HasItems=true`, unindexed `HasItems=false`, expander Hidden, one file child under `src` | **Verified** |
| F15 | Nesting must not path-split | See VM rule. Orphan file dropped; missing intermediate not invented | **Verified** (PoC nest); product `PathComparison` **Inferred** |
| F16 | `App.xaml` TreeView styles are theme-only | `:725-734` | **Verified** |
| F17 | `{colors.unverified}` is `#98A3B2` (same value as `{colors.text-muted}`) | `DESIGN.md:26,15`. Spec Part C still requires word+glyph for Unindexed (colour is the third signal) | **Verified** token; N8 chrome is not this spike |

## Rejected alternatives

| Alternative | Why rejected | Label |
|---|---|---|
| Custom `ListView` + indent | UIA control type **List** / **ListItem**, not Tree / TreeItem (spec Part C WCAG name/role/value). TreeView did **not** fail UIA, keyboard, 28px header, or virtualization | **Verified** |
| Bare `ItemsControl` + indent | `ItemsControlWrapperAutomationPeer`, control type **List**; no `TreeItem` (same cost session-thread Q6c recorded for a feed) | **Verified** |
| WebView2 / HTML tree | Architecture host is WPF (`DESIGN.md` `x-framework:wpf`; spec compatibility). Mockup is N8 direction only. `Microsoft.Web.WebView2 1.0.3485.44` is the code-viewer/composer stack, not D-0 | **Verified** (host); not run as a tree |
| Freeze `TreeView` with **no** attachments | Defaults fail 28px, virtualization, UIA Name kind+coverage, and Enter activate | **Verified** |
| `Height=28` on `TreeViewItem` | Clips expanded children | **Verified** |

## Boundary set exercised

Empty (0 items, Tree peer) · one-level (root files) · nested (root → `src` → `Program.cs`) ·
unindexed leaf (Hidden expander, LeafNode, no expand on keys) · 28px header hit rect · 400-item
virtualization on/off · orphan file / invented-folder nesting cases.

## Residuals (not blockers for freezing TreeView)

- **Physical Ctrl+Enter** — RaiseEvent cannot set `Keyboard.Modifiers` (same limit as
  session-thread Q5f). UV-1 handles `PreviewKeyDown` and reads `Keyboard.Modifiers` on real
  input; a visual-tree test should not treat synthetic RaiseEvent as proof of the chord.
  **Flagged** until UV-1’s attended or SendInput oracle.
- **Skip-set widen** of C# / TS / Python / knowledge / SQL walkers onto `UnanalysedLanguages.Skip`
  — optional after UV-0; ADR already binds UV-0 to that set. **Not this spike’s blocker.**
- **N8** glyph/token chrome (`{colors.unverified}` + word `Unindexed` + glyph). Toolkit is
  independent of the brush.
- **Double-click on unindexed** — UV-1 must mark `Handled` on the second click when
  `HasItems` is false so `IsExpanded` stays false.

## PoC disposal

Throwaway retained at `spikes/d0-tree-toolkit/` (`D0TreeToolkitSpike.csproj`, `Program.cs`,
`RESULT-raw.txt`). **Not** referenced by production; **not** a product test. Re-run is
`dotnet run --project spikes/d0-tree-toolkit`. Knowledge lives in this file.

## Status

| Facet | Result | Confidence |
|---|---|---|
| Toolkit | **WPF `TreeView`** | **Verified** |
| UIA Tree/TreeItem + Name kind+coverage | Native peers + Name binding | **Verified** |
| Keyboard arrows / indexed expand | Native | **Verified** |
| Enter / Ctrl+Enter | App `PreviewKeyDown`; not native. Ctrl+Enter not swallowed | **Verified** / Ctrl chord **Inferred** |
| 28px full-row hit | Header content MinHeight 28 | **Verified** |
| Unindexed non-expanding leaf | Empty children; keys no-op; UIA LeafNode; swallow double-click | **Verified** |
| Virtualization | Opt-in recycling `VirtualizingStackPanel` | **Verified** |
| Flat DTO → nested VM | Parent = census-folder in DTO; no path-split | **Verified** |
| Custom ListView / ItemsControl | Rejected (wrong UIA role) | **Verified** |
| WebView2 HTML tree | Rejected (wrong host) | **Verified** |
| UV-0 skip widen | Residual, optional | **Flagged** |
