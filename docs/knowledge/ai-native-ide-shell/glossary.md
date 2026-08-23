---
id: kb-shell-glossary
title: "AI-Native IDE Shell — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, conpty, webview2, wpf, ubiquitous-language]
links:
  - { to: kb-ai-native-ide-shell, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for shell-hosting vocabulary — ConPTY, HwndHost, airspace, OSC 133,
  Evergreen runtime, virtual host mapping — so the shell code and its docs agree.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **Airspace problem** | The WPF limitation whereby `HwndHost`-hosted content renders **above all WPF content** in Z-order, so popups, tooltips and docking drag adorners over it are invisible. Requires a phantom or layered window workaround. *(Verified as a known limitation)* |
| **ConPTY** | The Windows pseudoconsole API (`CreatePseudoConsole` / `ResizePseudoConsole` / `ClosePseudoConsole`), Windows 10 1809 / SDK 10.0.17763. Emits **UTF-8 VT sequences**; **read and write must be on separate threads**. *(Verified, [S1])* |
| **`CoreWebView2Environment`** | The WebView2 sharing unit: one environment = one browser process with one user data folder. Controls sharing an environment share that process; renderer processes are **per origin**. *(Verified, [S11])* |
| **Evergreen runtime** | The auto-updating WebView2 runtime that ships with Windows 11 and is auto-deployed to most Windows 10 devices. The alternative is **Fixed Version** (~150 MB bundled, pinned). *(Verified)* |
| **FTCS** | FinalTerm Command Semantics — the origin of the **OSC 133** sequences. |
| **`HwndHost`** | The WPF element that hosts a native Win32 window inside the visual tree. **How terminal controls are embedded in WPF, and it has no WinUI 3 equivalent.** *(Verified, [S7])* |
| **`InternalBufferSize`** | The `FileSystemWatcher` kernel buffer, **default 8,192 bytes**. On overflow the `Error` event fires with `InternalBufferOverflowException` and **changes are lost**. Raise to ≥ 65,536. *(Verified, [S15])* |
| **`LayoutPriority`** | AvalonDock v5's layout preset enum — `BottomFullWidth` (Rider-style), `SidesFullHeight` (VS-Code-style). *(Verified, [S10])* |
| **OSC 133** | The cross-tool shell-integration escape sequences marking prompt (`A`), command-line start (`B`), execution (`C`) and completion with exit code (`D;<exitcode>`). **Stable in Windows Terminal since v1.21.** *(Verified, [S13])* |
| **OSC 633** | VS Code's extension of OSC 133 adding captured command line (`E`), property reporting such as CWD (`P`) and **nonce-verified input** (`V`) — the published defence against sequence forgery. *(Verified, [S14])* |
| **`PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`** | The process-creation attribute binding a child process to a pseudoconsole. *(Verified, [S2])* |
| **Pseudoconsole** | The kernel object presenting a console interface to a child process while handing the host a VT stream. What makes a shell think it has a terminal. *(Verified, [S3])* |
| **Stream tap** | Our term for reading a child agent's PTY output to detect activity — either by parsing OSC sequences (structured, reliable) or by pattern-matching text (fragile). |
| **Virtual host name mapping** | `CoreWebView2.SetVirtualHostNameToFolderMapping(hostName, folderPath, accessKind)` — serves a local folder as `https://<hostName>/…`, giving local assets secure-context semantics that `file://` lacks, and (usefully) putting all panes on one origin. *(Verified, [S12])* |
| **VT sequences** | The escape-code protocol carried in the PTY stream: colour, cursor movement, and OSC application signalling. |
