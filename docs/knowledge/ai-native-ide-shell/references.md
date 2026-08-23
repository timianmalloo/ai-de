---
id: kb-shell-references
title: "AI-Native IDE Shell — references, APIs, versions and constants"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, conpty, webview2, osc-133, api-names]
links:
  - { to: kb-ai-native-ide-shell, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The exact API names, sequences, buffer sizes, versions and licences for shell hosting — the
  facts to quote rather than recall.
---

# Reference information

## APIs, versions and constants

### ConPTY

| Item | Value |
|---|---|
| APIs | `CreatePseudoConsole(size, hInput, hOutput, flags, &hPC)` · `ResizePseudoConsole` · `ClosePseudoConsole` |
| Process attribute | `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` |
| Introduced | Windows 10 **1809**, SDK **10.0.17763** |
| Output encoding | **always UTF-8-encoded VT sequences** |
| Threading | **input and output must be serviced on separate threads** — single-threaded servicing deadlocks when the output buffer fills (documented) |

*(Verified, [S1][S2][S3])*

### OSC / VT signalling

| Sequence | Meaning |
|---|---|
| `ESC]133;A BEL` | prompt start |
| `ESC]133;B BEL` | command line start |
| `ESC]133;C BEL` | command executing |
| `ESC]133;D;<exitcode> BEL` | command finished, with exit status |

Windows Terminal: marks **stable since v1.21**. VS Code: supported since 2022, extended by **OSC 633** —
`633;E;<commandline>` (captured command), `633;P;<property>=<value>` (CWD, workspace), `633;V;<sequence>`
(**nonce-verified input**). Windows Terminal partially implements 633. *(Verified, [S13][S14])*

### WebView2

| Item | Value |
|---|---|
| Process unit | **one `CoreWebView2Environment` = one browser process** (+ its GPU and audio processes) |
| Renderer processes | **per distinct origin**, within a shared environment |
| Local assets | `CoreWebView2.SetVirtualHostNameToFolderMapping(hostName, folderPath, accessKind)` — on **`CoreWebView2`**, not `WebView2`; call **after** `EnsureCoreWebView2Async()` |
| Access kinds | `CoreWebView2HostResourceAccessKind.Allow` \| `DenyCors` \| `Deny` |
| JS → native | `window.chrome.webview.postMessage()` |
| Native → JS | `CoreWebView2.PostWebMessageAsString` / `PostWebMessageAsJson`; `ExecuteScriptAsync` |
| Direct object bridge | `AddHostObjectToScript` (COM-visible .NET object, no message round-trip) |
| Distribution | **Evergreen** (auto-updating, ships with Windows 11) or **Fixed Version** (~150 MB bundled, no auto-update) |
| Renderer consolidation beyond origin | **not configurable** — documented as "beyond the scope of the WebView2 Runtime" |

*(Verified, [S11][S12])*

### `FileSystemWatcher`

| Item | Value |
|---|---|
| Backing API | Win32 `ReadDirectoryChangesW` |
| Default `InternalBufferSize` | **8,192 bytes (8 KB)** |
| Overflow behaviour | `Error` event with `InternalBufferOverflowException`; **events silently dropped** |
| Recommended size | **≥ 65,536 bytes (64 KB)**, plus subscriber-side debouncing |

*(Verified, [S15])*

### Platforms and packages

| Item | Value |
|---|---|
| .NET 10 | **LTS**, released **November 2025**, supported to ~November 2028 |
| WPF in .NET 10 | Fluent theme expansion, Grid shorthand syntax, clipboard unification, **~4,000 new unit tests**, performance work |
| WPF in .NET 9 | built-in Fluent theme, `ThemeMode` API |
| Windows App SDK | **2.4**, released **2026-08-13** |
| WinUI 3 gaps | **no first-party docking** (community `WinUI.Dock` only) · **no first-party DataGrid** · **no `HwndHost`** |
| `Dirkster.AvalonDock` | **v5.0**, **MIT**, targets .NET 4.8 / 9 / **10**; MVVM base classes; `AddDockLayoutService`; `AddAvalonDockSerializer<T>()`; `LayoutPriority` (`BottomFullWidth`, `SidesFullHeight`) |
| AvalonDock layout tree | `LayoutRoot` → `LayoutPanel` → `LayoutDocumentPane` / `LayoutAnchorablePane` → `LayoutDocument` / `LayoutAnchorable` |
| `CI.Microsoft.Terminal.Wpf` | built from Windows Terminal CI; **not a supported public API** (microsoft/terminal issue **#6999**); exact current version **Flagged** |
| `EasyWindowsTerminalControl` | **MIT**; `StartupCommandLine`, input/output interception, `LogConPTYOutput`, theming, detach/reattach; ~6,800 downloads; WinUI 3 variant "very alpha" |
| Syncfusion licence | free community licence under $1M revenue **or** 5 developers |

*(Verified, [S4][S5][S6][S7][S8][S9][S10])*

## Primary documentation

- **ConPTY**: `CreatePseudoConsole` reference · "Creating a Pseudoconsole Session" · "Pseudoconsoles overview".
  *(Verified, [S1][S2][S3])*
- **WPF**: "What's new in WPF for .NET 10" and ".NET 9". *(Verified, [S4][S5])*
- **WebView2**: process-model documentation and the `SetVirtualHostNameToFolderMapping` .NET API reference
  (plus the `ICoreWebView2_3` Win32 reference). *(Verified, [S11][S12])*
- **Shell integration**: Windows Terminal shell-integration documentation; VS Code terminal shell-integration
  documentation. *(Verified, [S13][S14])*
- **`FileSystemWatcher`**: .NET API reference, `InternalBufferSize`. *(Verified, [S15])*
- **microsoft/terminal issue #6999** — the statement that the WPF control is not a supported public API.
  *(Verified, [S9])*
