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
  Spike N7 Verified WPF TreeView attachments; product UIA, keyboard, High Contrast,
  DPI and signing stay Flagged until UV-1.
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
| Keyboard traversal works | Pointer required for View source / Reveal / expand | Enter View source; Ctrl+Enter Reveal; arrows; unindexed does not expand | Spike F8/F11/F13 PreviewKeyDown + arrows. Physical Ctrl+Enter **Inferred** (F12) | planned | **Verified** (Enter, arrows) / **Inferred** (Ctrl+Enter) | UV-1 attended or SendInput oracle |
| Accessibility tree is correct | Name is path only; role not TreeItem; unindexed reports Expanded | Name includes kind+coverage; unindexed LeafNode | Spike F4/F5/F9. Product binding **not shipped** | planned | Spike **Verified**; product **Flagged** | |
| Theme/high contrast works | HC selected row vanishes (black on black) | System Highlight / HighlightText on selected row; glyph+word remain | Mockup `hc` is a stand-in; audit *not measured* | planned | **Flagged** | UV-1 HighContrast resources |
| DPI/windowing works | 28px header clips or shrinks below 24×24 at 150% | Header MinHeight 28 DIP; hit ≥24×24 | Spike F7 at default DPI only | planned | **Flagged** (DPI not in spike) | Mixed-DPI UV-1 |
| Large native lists remain responsive | 400 roots realize 400 containers | Recycling virtualization; ~18/400 in 280px | Spike F2/F3 | planned | Spike **Verified**; product must set attached properties | Default TreeView is not virtualizing |
| OS integration is scoped | Tree walks disk from the App process | PROBE-APP-ENUM | Spec probe; not this mockup | planned | **Flagged** | Architecture + UV-0 |
| Distribution trust is handled | Unsigned slice artifact | Existing app signing | Out of D-0 | n/a | **Flagged** (release, not this slice) | |

## 3. WPF-specific rows

| claim | failing input or condition | oracle | evidence | red observed | confidence | residual risk |
|---|---|---|---|---|---|---|
| UIA metadata | Unbound Name = header only | `AutomationProperties.Name` → kind+coverage | Spike F5 | planned | Spike **Verified** | UV-1 ItemContainerStyle |
| Keyboard accelerators | Enter does nothing (platform default) | Tree `PreviewKeyDown` handles Return and Ctrl+Return | Spike F11 | planned | **Verified** (handler shape) | Do not steal composer Ctrl+Enter when tree is not focused |
| HighContrast resources | Raw hex in item template | DynamicResource / system brushes | Not in spike | planned | **Flagged** | |
| 28px full-row hit | `Height=28` on TreeViewItem clips children; default row 16px | Header content `MinHeight=28`, never item Height | Spike F6/F7 | planned | Spike **Verified** | UV-1 template |
| Unindexed non-expanding leaf | Double-click sets IsExpanded with 0 children | Empty Children; swallow second click | Spike F10/F8 | planned | Spike **Verified** | UV-1 must mark Handled |

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
