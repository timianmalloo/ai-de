---
id: kb-shell-sources
title: "AI-Native IDE Shell — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-ai-native-ide-shell, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The access-dated source list behind the shell-hosting knowledge base, keyed [S1]..[S16],
  separating Microsoft primary documentation from the secondary claims about other products.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | `CreatePseudoConsole` function | primary (vendor docs) | https://learn.microsoft.com/en-us/windows/console/createpseudoconsole | ConPTY API surface, flags |
| S2 | Creating a Pseudoconsole Session | primary | https://learn.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session | Usage pattern, `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`, **deadlock warning** |
| S3 | Pseudoconsoles overview | primary | https://learn.microsoft.com/en-us/windows/console/pseudoconsoles | Concept, **UTF-8 VT output** |
| S4 | What's new in WPF for .NET 10 | primary | https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net100 | WPF .NET 10 status and new APIs |
| S5 | What's new in WPF for .NET 9 | primary | https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net90 | Fluent theme, `ThemeMode` |
| S6 | What's new in .NET 10 | primary | https://learn.microsoft.com/en-us/dotnet/core/whats-new/ | LTS status, release timing |
| S7 | WinUI 3 / Windows App SDK migration docs | primary | https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/ | App SDK 2.4; **no `HwndHost`**, no first-party docking or DataGrid |
| S8 | `EasyWindowsTerminalControl` (NuGet / repo) | primary (package + repo) | https://www.nuget.org/packages/EasyWindowsTerminalControl | MIT wrapper, API surface, WinUI "very alpha" variant, VS usage claim |
| S9 | microsoft/terminal issue #6999 | primary (repo issue) | https://github.com/microsoft/terminal/issues/6999 | **`CI.Microsoft.Terminal.Wpf` is not a supported public API** |
| S10 | Dirkster.AvalonDock repository | primary (repo) | https://github.com/Dirkster99/AvalonDock | MIT, .NET 10 target, v5 features, layout tree, production users |
| S11 | WebView2 process model | primary (vendor docs) | https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/process-model | Environment = browser process; per-origin renderers; consolidation out of scope |
| S12 | `SetVirtualHostNameToFolderMapping` API reference | primary | https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.setvirtualhostnametofoldermapping | Signature, `CoreWebView2` placement, access kinds |
| S13 | Windows Terminal shell integration | primary | https://learn.microsoft.com/en-us/windows/terminal/tutorials/shell-integration | OSC 133 sequences; **marks stable in v1.21** |
| S14 | VS Code terminal shell integration | primary | https://code.visualstudio.com/docs/terminal/shell-integration | OSC 633 extensions, nonce-verified input |
| S15 | `FileSystemWatcher.InternalBufferSize` | primary (.NET API docs) | https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize | **8 KB default**, overflow exception, ≥64 KB guidance |
| S16 | Warp documentation / architecture analysis | secondary | https://docs.warp.dev/ | PTY multiplexing, block UI, multi-agent orchestration — **Inferred** |

## Source-quality notes

- **S1–S15 are Microsoft primary documentation or the project's own repository**, and every API name, buffer
  size, escape sequence and version in this topic is read from them rather than recalled.
- **S16 and the claims about Cursor, JetBrains Fleet, and Amazon Q / Fig are secondary or marketing sources**
  and are labelled **Inferred** throughout.
- Two claims are **Flagged** because a primary fetch failed: AvalonDock v5's release date and the exact
  `CI.Microsoft.Terminal.Wpf` version (the releases and package pages errored). Avalonia's current stable
  version is Flagged for the same reason.
- The assertion that **Visual Studio embeds the same terminal control** appears only in the
  `EasyWindowsTerminalControl` README, with no VS version named and no Microsoft source — **Flagged**, and it
  should not be used to justify a decision.
- The **800–1200 MB four-environment memory figure** is a calculation from the documented process model, not
  a measurement — **Inferred**, and worth measuring before it is quoted.
