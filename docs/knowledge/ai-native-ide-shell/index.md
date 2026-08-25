---
id: kb-ai-native-ide-shell
title: "AI-Native IDE Shell Hosting — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [wpf, dotnet-10, conpty, webview2, avalondock, terminal, osc-133]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2026-11-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  Evidence base for a Windows shell hosting agent terminals beside web-rendered visual panes:
  WPF on .NET 10 versus WinUI 3, the ConPTY foundation, the unsupported terminal control, the
  WebView2 process model, and OSC 133 as the agent signalling channel.
---

# AI-Native IDE Shell Hosting — domain knowledge

**Domain & problem:** A Windows desktop shell hosting several coding-agent terminals (Claude Code, Copilot
CLI, pwsh) side by side with web-rendered visual panes for graph and diagram views. The repository today is
a .NET 10 / C# 14 WPF starter.

**Canonical framing:** The field frames this as **an editor with an integrated terminal** (VS Code, Visual
Studio) or as **a terminal with an agent** (Warp, Amazon Q). Our framing inverts both: **a terminal host
with derived visual panes and no editor at all**. That is genuinely unusual, and it changes which prior art
transfers — VS Code's extension-host architecture largely does not, while its ConPTY-plus-xterm.js terminal
plumbing entirely does.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"APIs, versions and constants".)*

## Headline findings

1. **WPF on .NET 10 is not legacy — it is an actively evolved LTS target.** .NET 10 shipped November 2025
   (LTS, support to ~Nov 2028) and WPF received Fluent theme expansion, Grid shorthand syntax, clipboard
   unification, roughly **4,000 new unit tests**, and performance work. The existing starter is on the right
   platform. — *(Verified, [S4][S5][S6])*
2. **WinUI 3 is the strategically favoured platform and the wrong one for this specific job.** Windows App
   SDK 2.4 shipped 2026-08-13 and WinUI 3 is actively developed — but it has **no first-party docking
   control** (only the community `WinUI.Dock`), **no first-party DataGrid**, and critically **no
   `HwndHost`**, which is exactly how terminal controls are embedded in WPF. — *(Verified, [S7])*
3. **There is no supported first-party WPF terminal control.** The publicly consumable path is
   `CI.Microsoft.Terminal.Wpf` — built from the Windows Terminal CI pipeline, explicitly **not a supported
   public API** (microsoft/terminal issue #6999) — usually consumed through the MIT-licensed
   `EasyWindowsTerminalControl` wrapper, which inherits the caveat that the low-level API may break on any
   Windows Terminal update. — *(Verified, [S8][S9])*
4. **ConPTY is the stable foundation underneath everything.** `CreatePseudoConsole` / `ResizePseudoConsole`
   / `ClosePseudoConsole`, introduced in Windows 10 1809 (SDK 10.0.17763). Output is **always UTF-8-encoded
   VT sequences**, and **input and output must be serviced on separate threads or the session deadlocks** —
   this is documented, not folklore. — *(Verified, [S1][S2][S3])*
5. **Dirkster.AvalonDock v5 is the mature docking answer**: MIT, targeting .NET 4.8 / 9 / 10, with
   first-class MVVM base classes, DI integration (`AddDockLayoutService`), XML *and* JSON layout
   serialisation, and `LayoutPriority` presets matching Rider-style and VS-Code-style arrangements. In
   production use by Microsoft's own Profile Explorer, Stride, RoslynPad and DAX Studio. — *(Verified, [S10])*
6. **The `CoreWebView2Environment` is the memory unit, and getting it wrong is expensive.** One environment
   = one browser process (plus its GPU and audio processes). Four panes on four environments can mean
   **800–1200 MB RAM**; four panes sharing one environment share the browser process, and hosting them under
   the same virtual host name consolidates renderer processes too, because renderers are per-origin. — *(Verified, [S11]; the MB figure Inferred)*
7. **`SetVirtualHostNameToFolderMapping` is on `CoreWebView2`, not `WebView2`,** and requires
   `EnsureCoreWebView2Async()` to have completed. It maps a virtual HTTPS host to a local folder — giving
   local assets full secure-context semantics that `file://` cannot — with a
   `CoreWebView2HostResourceAccessKind` of `Allow` / `DenyCors` / `Deny`. — *(Verified, [S12])*
8. **OSC 133 is the established agent→shell signalling channel and it costs nothing to adopt.** The FTCS
   sequences — `ESC]133;A` prompt start, `;B` command-line start, `;C` executing, `;D;<exitcode>` finished —
   are cross-tool, **stable in Windows Terminal since v1.21**, and supported in VS Code since 2022. They
   arrive in the PTY stream already, so command boundaries and exit codes need no extra IPC. VS Code's
   OSC 633 extends them with command-line capture, CWD reporting and a nonce scheme. — *(Verified, [S13][S14])*
9. **`FileSystemWatcher`'s default buffer is 8 KB and it drops events silently when it overflows** — the
   `Error` event fires with `InternalBufferOverflowException` and the missed changes are simply gone. For an
   agent event bus under heavy write frequency this is a realistic failure; raising `InternalBufferSize` to
   **≥ 64 KB** plus debouncing is the documented mitigation. — *(Verified, [S15])*
10. **Warp is the most structurally relevant prior art**: a Rust-native terminal that **multiplexes the PTY
    between the shell and an agent system**, presents commands as annotated blocks with per-command
    metadata, and orchestrates multiple agents. The block model — separating prompt, command and output
    zones as data rather than as scrollback — is the idea worth studying. — *(Inferred, [S16])*

## Confidence summary

Verified: the ConPTY API surface and its deadlock warning, WPF's .NET 10 status, WinUI 3's missing docking
and `HwndHost`, AvalonDock v5's licence/targets/features, the WebView2 process model and virtual-host API,
OSC 133/633 semantics and Windows Terminal v1.21 stability, `FileSystemWatcher`'s buffer behaviour, and the
`CI.Microsoft.Terminal.Wpf` support status. Inferred: the 800–1200 MB four-environment figure; Warp's
internals; Visual Studio's use of the same terminal control path; Cursor, Fleet and Amazon Q architectures.
Flagged: AvalonDock v5's exact release date (releases page errored), the exact
`CI.Microsoft.Terminal.Wpf` version, Avalonia's current stable version, and whether WebView2 renderer
consolidation can be forced beyond origin rules (documented as out of scope by Microsoft).

**Load-bearing Flagged claim:** the terminal control's version. Since it is an unsupported CI package whose
API may break, pinning an exact version is the mitigation — and we do not currently know which version to
pin.

## Design implications

- **Stay on WPF for the shell.** The "WinUI 3 is the future" argument is real for consumer apps and
  materially weakened here by three specifics: no `HwndHost`, no first-party docking, and WPF still being
  actively developed on an LTS runtime.
- **Build on ConPTY directly, and treat the terminal *control* as a replaceable rendering layer.** ConPTY is
  stable Windows API; `CI.Microsoft.Terminal.Wpf` is not. Owning the PTY lifecycle and stream means the
  renderer can be swapped for xterm.js-in-WebView2 without touching session management.
- **Read and write the PTY on separate threads.** Documented deadlock, trivially avoidable, catastrophic if
  missed.
- **Share exactly one `CoreWebView2Environment` across all visual panes**, and map every pane under the same
  virtual host name so they share an origin and therefore renderer processes. This is the difference between
  a shell that idles at a few hundred megabytes and one that idles at a gigabyte.
- **Parse OSC 133 from the PTY stream as the primary agent-activity signal**, with the file event bus as the
  universal fallback. The sequences are already there, they carry exit codes, and they need no cooperation
  from the agent beyond shell integration.
- **But treat OSC sequences as untrusted input.** An agent that prints a code block containing
  `ESC]133;D` will forge a command boundary. VS Code solved this with OSC 633's nonce; we need an equivalent
  or an explicit acceptance that boundaries are advisory.
- **If the file event bus is used, set `InternalBufferSize ≥ 65536`, debounce, and treat the files as an
  append log** rather than expecting per-event delivery.
- **Version the AvalonDock layout schema.** Layout is serialised by content ID; if view-model identifiers
  change between releases, deserialisation **silently discards** unrecognised panes.
- **Plan for the `HwndHost` airspace problem.** Terminal content rendered through `HwndHost` sits above all
  WPF content in Z-order, so docking drag adorners, tooltips and popups over a terminal pane are invisible
  without a workaround (phantom or layered window). This is a known WPF limitation and it will be met on day
  one of docking work.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The API names and constants in
`references.md` are the ones to quote rather than recall. Refresh when Windows App SDK or Windows Terminal
ship a major version — the terminal-control risk is the one that moves.
