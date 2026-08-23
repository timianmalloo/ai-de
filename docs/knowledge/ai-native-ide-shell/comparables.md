---
id: kb-shell-comparables
title: "AI-Native IDE Shell — comparable hosts"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, vscode, warp, windows-terminal, frameworks]
links:
  - { to: kb-ai-native-ide-shell, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Frameworks, docking libraries, terminal-embedding options and existing agent hosts compared
  against the specific job of running many agent terminals beside derived visual panes.
---

# Comparable solutions & problem framings

## UI frameworks, judged for *this* job

| Framework | Docking | Terminal embedding | Web panes | Verdict for an agent-terminal shell | Confidence |
|---|---|---|---|---|---|
| **WPF / .NET 10** | **AvalonDock v5** (MIT) + three commercial options | **`HwndHost`** — the only battle-tested path | WebView2 first-class | **Best fit.** LTS to ~2028, actively evolved | Verified [S4][S6][S10] |
| **WinUI 3 / App SDK 2.4** | community `WinUI.Dock` only | **no `HwndHost`** — much harder; the WinUI terminal wrapper is "very alpha" | native | Strategic platform, wrong tool today | Verified [S7] |
| **Avalonia 11.x** | separate `Dock` library | possible, less proven | `WebViewControl` wrapping WebView2 on Windows | Cross-platform cost with no Windows-only gain | Verified (stable version Flagged) |
| **MAUI** | none found | heavy interop | — | Not a candidate | Inferred |
| **Electron** | JS-land | xterm.js only | native | 150–300 MB idle; no native controls | Verified arch.; figures Inferred |
| **Tauri** | JS-land | xterm.js only | OS WebView2 | 30–50 MB idle; no native controls | Verified arch.; figures Inferred |

## Docking libraries

| Library | Licence | Targets | Notable |
|---|---|---|---|
| **Dirkster.AvalonDock 5.0** | MIT | .NET 4.8 / 9 / **10** | MVVM base classes; DI (`AddDockLayoutService`); XML **and** JSON serialisation; `LayoutPriority` presets for Rider- and VS-Code-style layouts; used by MS Profile Explorer, Stride, RoslynPad, DAX Studio |
| Syncfusion | commercial (free community under $1M / 5 devs) | WPF | more polished |
| DevExpress, Telerik | commercial | WPF | more polished |
| Dock (Avalonia) | OSS | Avalonia | only if Avalonia |

*(Verified, [S10])*

## Terminal embedding options

| Option | Stability | Rendering fidelity | Notes |
|---|---|---|---|
| **ConPTY directly** | **Stable Windows API** since Win10 1809 / SDK 10.0.17763 | n/a — you own the stream | The foundation under every option. UTF-8 VT out; **separate threads per direction or deadlock** |
| `CI.Microsoft.Terminal.Wpf` | **Unsupported CI artefact** — issue #6999 | **highest** — DirectWrite + GPU atlas | May break on any Windows Terminal update; pin the version |
| `EasyWindowsTerminalControl` | MIT wrapper, inherits the caveat | as above | Clean XAML API, interception events, detach/reattach; WinUI variant "very alpha" |
| **xterm.js in WebView2** | **stable dependency** | good, not identical (Canvas/WebGL) | What VS Code does; more configurable; loses the native renderer |

*(Verified, [S1][S8][S9])*

## Agent/terminal hosts as prior art

| Host | Frames the problem as | Architecture | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **VS Code** | editor with integrated terminal + webview panels | Electron; ConPTY via `node-pty`; xterm.js in a renderer; extension host in a separate Node process; webviews as sandboxed iframes over `postMessage` | Battle-tested at scale; OSC 133/633; huge ecosystem | Each agent is an extension; no native controls; ~300–400 MB baseline; docking not extension-customisable | Verified |
| **Warp** | agent-native terminal; commands as data blocks | Rust native; **PTY multiplexed between shell and agent**; block UI; multi-agent via a cloud control plane | Native performance; first-class multi-agent; agents see live output | Not embeddable; cloud features are SaaS; Windows support came later | Inferred [S16] |
| **Visual Studio** | terminal as a tool window in a WPF shell | `HwndHost` embedding the Windows Terminal renderer, reportedly the same CI package | Highest-fidelity terminal in WPF | Internal, no public API, version-coupled to the installer | Inferred (**Flagged** — secondary claim) |
| **Windows Terminal** | native terminal multiplexer | C++/WinRT, ConPTY, DirectWrite renderer, WinUI 3 XAML | The rendering gold standard; OSC 133 stable in v1.21; MIT | **Not embeddable** — no stable embedding API; IDE layout is not its goal | Verified |
| **Cursor** | AI-first editor | VS Code fork | Familiar ecosystem, ships fast | Electron overhead; one primary agent, not many terminals | Inferred |
| **JetBrains Fleet** | distributed next-gen IDE | Compose/Skia UI, remote workspace backend | Fast start; distributed compute | No embedding API; proprietary; internal terminal | Inferred |
| **Amazon Q / Fig** | agent **in** the prompt | shell integration via OSC + a daemon; native popup overlay | Zero-install; works in **any** terminal | Not a host; cannot add visual panes | Inferred |

**What the table says:** nobody hosts *many agent terminals plus derived visual panes*. VS Code hosts one
terminal well and everything else as extensions; Warp hosts agents in a terminal but is not embeddable;
Windows Terminal renders best and cannot be embedded. The plumbing transfers; the shape does not exist.

## Adjacent ideas worth borrowing

- **Warp's command blocks** — treating each command as a structured record with metadata rather than as
  scrollback text is what makes per-command agent annotation possible at all.
- **VS Code's OSC 633 nonce** — the only published answer to OSC sequence forgery from untrusted output.
- **AvalonDock's `LayoutPriority` presets** — Rider-style and VS-Code-style layouts as first-class options
  rather than as hand-tuned defaults.
- **Amazon Q's posture** — augmenting any terminal rather than hosting one. A reminder that the shell is the
  least differentiating part of the plan, which the seed architecture already concedes by building it last.
