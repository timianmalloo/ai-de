---
id: proof-native-ui-solution-tree
title: "Native UI Proof Pack — Solution tree (D-0)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [native-ui, proof-pack, accessibility, keyboard, dpi, wpf, D-0, solution-tree]
links:
  - { to: spec-understanding-views, rel: tested-by }
  - { to: mockup-solution-tree, rel: relates-to }
  - { to: spike-d0-tree-toolkit, rel: depends-on }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: depends-on }
review-by: 2027-03-15
summary: >-
  Native proof pack for the Architecture Solution tree. HTML mockup is direction only.
  Spike N7 Verified WPF TreeView attachments. UV-1 shipped product UIA Name, 28px header,
  recycling virtualization, and unindexed leaf tests. High Contrast, DPI, and signing stay Flagged.
review-suggested:
  - { by: mockup-solution-tree, on: 2026-09-15, reason: "N8 ui-design: Solution tree mockup settles dual-activate, Unindexed chrome, and hard states; HTML is direction only" }
---

# Native UI Proof Pack — Solution tree (D-0)

HTML mockup and `ui-craft-gate.py` **cannot** clear native PASS (UI-T4, DX9a). This pack records what is proven, what is only spiked, and what UV-1 still owes.

## 1. Medium declaration

| Field | Value |
|---|---|
| Medium | native-desktop |
| Platform(s) | windows |
| Framework | WPF (`net10.0-windows`, PresentationFramework 10.0.0.0 — spike F1 **Verified**) |
| Distribution | existing app channel (not this slice) |
| Accessibility API | UI Automation (`TreeViewAutomationPeer` / `TreeViewItemAutomationPeer`) |
| HIG source | Windows / Fluent keyboard and focus; pack native-client-ui-design; spec Part C |

## 2. Required proof rows

| claim | failing input or condition | oracle | evidence | red observed | confidence | residual risk |
|---|---|---|---|---|---|---|
| Platform HIG is honored | Tree uses List/ListItem, or 44px rail rows, or Explorer CRUD | WPF Tree/TreeItem; 28px compact rows; read-only navigator | Spike rejected ListView/ItemsControl (wrong UIA role). Product UV-1 **not run** | planned | Spike **Verified**; product **Flagged** | HTML analogue is not HIG proof |
| Keyboard traversal works | Pointer required for View source / Reveal / expand | Enter View source; Ctrl+Enter Reveal; arrows; unindexed does not expand | Spike F8/F11/F13. UV-1 `HandleKey` Enter/Control; file double-click View source; node menu Reveal. Physical Ctrl+Enter **Inferred** (F12) | yes (Enter, menu, double-click) | **Verified** (Enter, arrows, menu, double-click) / **Inferred** (physical Ctrl+Enter) | SendInput/attended still owed |
| Accessibility tree is correct | Name is path only; role not TreeItem; unindexed reports Expanded | Name includes kind+coverage; unindexed LeafNode | Spike F4/F5/F9. UV-1 `Show_StarDto_…` binds Name | yes (kind-row reds) | Spike **Verified**; product **Verified** (Name) | High Contrast still Flagged |
| Theme/high contrast works | HC selected row vanishes (black on black) | System Highlight / HighlightText on selected row; glyph+word remain | Mockup `hc` is a stand-in; audit *not measured* | planned | **Flagged** | UV-1 HighContrast resources |
| DPI/windowing works | 28px header clips or shrinks below 24×24 at 150% | Header MinHeight 28 DIP; hit ≥24×24 | Spike F7 at default DPI only | planned | **Flagged** (DPI not in this slice) | Mixed-DPI |
| Large native lists remain responsive | 400 roots realize 400 containers | Recycling virtualization; ~18/400 in 280px | Spike F2/F3. UV-1 sets Recycling on the instance | yes | Spike **Verified**; product **Verified** (attached properties) | 400-item live measure not re-run in App.Tests |
| OS integration is scoped | Tree walks disk from the App process | PROBE-APP-ENUM | `SolutionTreeProbeTests.ProbeAppEnum_…` | yes | **Verified** | FILE-READ is a pinned source scan |
| Distribution trust is handled | Unsigned slice artifact | Existing app signing | Out of D-0 | n/a | **Flagged** (release, not this slice) | |

## 3. WPF-specific rows

| claim | failing input or condition | oracle | evidence | red observed | confidence | residual risk |
|---|---|---|---|---|---|---|
| UIA metadata | Unbound Name = header only | `AutomationProperties.Name` → kind+coverage | Spike F5. UV-1 ItemContainerStyle | yes | **Verified** | |
| Keyboard accelerators | Enter does nothing (platform default) | Tree `PreviewKeyDown` handles Return and Ctrl+Return | Spike F11. UV-1 `HandleKey` | yes (Enter) | **Verified** (Enter); Ctrl chord **Inferred** | Do not steal composer Ctrl+Enter when tree is not focused |
| HighContrast resources | Raw hex in item template | DynamicResource / system brushes | Not in spike | planned | **Flagged** | |
| 28px full-row hit | `Height=28` on TreeViewItem clips children; default row 16px | Header content `MinHeight=28`, never item Height | Spike F6/F7. `Header_MinHeightIs28_NotHeightOnTheItem` | yes | **Verified** (default DPI) | |
| Unindexed non-expanding leaf | Double-click sets IsExpanded with 0 children | Empty Children; swallow second click | Spike F10/F8. UV-1 Preview+bubble Handled | yes | **Verified** | |

## 4. Exemplar/license references used

| Exemplar | License posture | Allowed use | Pattern borrowed |
|---|---|---|---|
| JetBrains Rider Project tool window | Reference-only (proprietary UI) | Pattern: unindexed as a labelled state | Not pixels, not yellow-as-only-signal |
| VS Code Explorer `files.exclude` | Reference-only | Anti-pattern: omission without chrome | Skip-count required |
| `microsoft/WPF-Samples` | MIT | DPI / TreeView control samples | Not copied into this mockup |
| Installed WPF TreeView | In-box | The control UV-1 will host | Spike freeze |

## 5. What this pack is not

- A native PASS.
- A substitute for US-T3/T4/T5 visual-tree walks.
- Proof that the HTML `role="tree"` matches `TreeViewAutomationPeer`.
