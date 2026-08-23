---
id: kb-shell-sota
title: "AI-Native IDE Shell — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [wpf, winui3, avalonia, conpty, webview2, osc-133, filesystemwatcher]
links:
  - { to: kb-ai-native-ide-shell, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The framework, docking, terminal-embedding, WebView2 and signalling landscape as it actually
  stands — including which pieces are supported APIs and which are CI artefacts.
---

# State of the art — desktop shell hosting

## UI framework

**WPF / .NET 10** — the most mature Windows-only framework for complex tool windows. .NET 10 is **LTS**
(released November 2025, supported to ~November 2028). WPF in .NET 10 gained Fluent theme expansion, Grid
shorthand syntax, clipboard unification, ~4,000 new unit tests and performance work; .NET 9 introduced the
built-in Fluent theme and the `ThemeMode` API. It is the **only** framework where `HwndHost`-based terminal
embedding is battle-tested. *(Verified, [S4][S5][S6])*

**WinUI 3 / Windows App SDK 2.4** (2026-08-13) — Microsoft's strategic direction, with native
DirectComposition/DWM rendering and better Windows 11 fidelity. Against it, for this job: **no first-party
docking control** (only the community `WinUI.Dock`), **no first-party DataGrid**, and **no `HwndHost`**, so
terminal embedding is substantially harder — `EasyWindowsTerminalControl.WinUI` exists and is described as
"very alpha". *(Verified, [S7])*

**Avalonia 11.x** — cross-platform, WPF-like, compositor-based rendering over Skia/Direct2D, with a separate
`Dock` library for IDE-style layout and a `WebViewControl` wrapping WebView2 on Windows. For a Windows-only
shell it adds complexity without gain. *(Verified; current stable version **Flagged** — the releases page
errored during research)*

**MAUI** — mobile-first, no desktop docking ecosystem, `HwndHost`-equivalent content requires heavy interop.
Not a candidate. *(Inferred — no sources found)*

**Electron / Tauri** — Electron ships full Chromium per app (~80–150 MB installer, 150–300 MB idle RAM);
Tauri pairs a Rust core with the OS WebView2 (~2–10 MB installer, 30–50 MB idle). Neither hosts WPF or
WinForms controls, so terminal embedding means xterm.js in a web panel, losing the Windows Terminal
renderer's GPU-accelerated glyph rendering. *(Verified for the architecture; the RAM/installer figures Inferred)*

## Docking

**Dirkster.AvalonDock 5.0** — MIT, targeting .NET 4.8, 9 and 10 (badges confirmed on the repository). New in
v5: MVVM base classes, DI integration (`AddDockLayoutService`), JSON **and** XML layout serialisation via
`ILayoutSerializer` (`AddAvalonDockSerializer<T>()`), a `ToggleDockingManager` sidebar pattern, and a
`LayoutPriority` enum with presets — `BottomFullWidth` (Rider-style), `SidesFullHeight` (VS-Code-style).
Layout is a tree: `LayoutRoot` → `LayoutPanel` → `LayoutDocumentPane` / `LayoutAnchorablePane` →
`LayoutDocument` / `LayoutAnchorable`. Production users include Microsoft Profile Explorer, Stride,
RoslynPad and DAX Studio. *(Verified, [S10]; exact v5 release date **Flagged**)*

**Syncfusion / DevExpress / Telerik** — more polished, commercial. Syncfusion offers a free community licence
below $1M revenue or five developers. *(Verified)*

**Dock (Avalonia)** — the AvalonDock analogue, relevant only if Avalonia is chosen.

## Terminal embedding

**The ConPTY path — the foundation.** `CreatePseudoConsole(size, hInput, hOutput, 0, &hPC)` →
`CreateProcess` with `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` → read `outputReadSide` on a **dedicated thread**
while writing input to `inputWriteSide`; resize with `ResizePseudoConsole`; close with `ClosePseudoConsole`.
Introduced in Windows 10 1809 / SDK 10.0.17763. **Output is always UTF-8-encoded VT sequences.** The MSDN
documentation warns explicitly that servicing both directions on one thread **deadlocks** when the output
buffer fills. This is what Windows Terminal itself uses. *(Verified, [S1][S2][S3])*

**`CI.Microsoft.Terminal.Wpf` / `EasyWindowsTerminalControl`.** The CI-branded package is built from the
Windows Terminal repository's pipeline and is **explicitly not a supported public API** (microsoft/terminal
issue #6999). `EasyWindowsTerminalControl` (MIT) wraps it into a usable XAML control with
`StartupCommandLine`, input/output interception events, `LogConPTYOutput`, theme and colour support, and
detach/reattach — around 6,800 downloads, plus a "very alpha" WinUI 3 variant using a custom `HwndHost`
replacement. Its own disclaimer: the low-level API may break with any Windows Terminal update.
*(Verified, [S8][S9])*

**xterm.js in WebView2** — the alternative: host a WebView2, load xterm.js, bridge via `postMessage` or
`AddHostObjectToScript`. This is what VS Code does (xterm.js in a renderer process, IPC back to the main
process that owns the ConPTY). It trades the Windows Terminal renderer's DirectWrite + GPU atlas glyph
quality for dependency stability and configurability. *(Verified)*

## WebView2

Chromium's multi-process model applies: **one `CoreWebView2Environment` = one browser process** (with its own
GPU and audio service processes). Multiple `WebView2` controls from the *same* environment share that
browser process and get **separate renderer processes per distinct origin**. Multiple environments spawn
multiple browser processes. *(Verified, [S11])*

For a four-pane shell: one environment, one user data folder, one `CoreWebView2Controller` + `CoreWebView2`
pair per pane — and host every pane under the **same virtual host name** so they share an origin and
consolidate renderers. Whether renderers can be consolidated further is documented as *"beyond the scope of
the WebView2 Runtime"*, with no configuration API. *(Verified, [S11]; further consolidation **Open**)*

**Bridging:** `window.chrome.webview.postMessage()` JS→native and
`CoreWebView2.PostWebMessageAsString/AsJson()` native→JS; `AddHostObjectToScript` exposes a COM-visible .NET
object directly to JS without a message round-trip; `ExecuteScriptAsync` for fire-and-forget evaluation.

**Local assets:** `CoreWebView2.SetVirtualHostNameToFolderMapping(hostName, folderPath, accessKind)`, called
after `EnsureCoreWebView2Async()`, serves `folderPath` as `https://<hostName>/…` — the recommended approach
because `file://` carries browser security restrictions. *(Verified, [S12])*

**Distribution:** the **Evergreen** runtime auto-updates, ships with Windows 11 and is auto-deployed to most
Windows 10 devices by the Edge installer; **Fixed Version** bundles ~150 MB with no auto-update. Evergreen is
strongly preferred. *(Verified)*

## Signalling: OSC sequences and the file bus

**OSC 133 / FTCS** — the cross-tool shell-integration standard, arriving in the PTY output stream:

| Sequence | Meaning |
|---|---|
| `ESC]133;A BEL` | prompt start |
| `ESC]133;B BEL` | command line start |
| `ESC]133;C BEL` | command executing |
| `ESC]133;D;<exitcode> BEL` | command finished, with exit status |

Windows Terminal made marks **stable in v1.21**; VS Code has supported them since 2022. **VS Code's OSC 633**
extends the set: `633;E;<commandline>` (captured command), `633;P;<property>=<value>` (CWD, workspace) and
`633;V;<sequence>` (nonce-verified input). Windows Terminal partially implements 633. *(Verified, [S13][S14])*

**`FileSystemWatcher`** — backed by Win32 `ReadDirectoryChangesW`, with a documented default
`InternalBufferSize` of **8,192 bytes**. On overflow the `Error` event fires with
`InternalBufferOverflowException` and **events are silently dropped**. Recommended mitigation: raise to
**≥ 65,536 bytes**, debounce on the subscriber side, and prefer PTY stream parsing where real-time matters.
*(Verified, [S15])*

## Prior art, and what transfers

- **VS Code** — Electron shell; ConPTY via `node-pty`; **xterm.js** renderer in a renderer process; extension
  host in a separate Node process; webviews as sandboxed iframes over `postMessage`. Battle-tested at scale,
  with OSC 133/633 integration. What does **not** transfer: the extension model (each agent becomes an
  extension), JS-land UI (no WPF controls), ~300–400 MB baseline, and a docking layout extensions cannot
  customise. *(Verified)*
- **Warp** — Rust native; **multiplexes the PTY between the shell and an agent system**; command output as
  annotated blocks with per-command metadata; multi-agent orchestration through a cloud control plane. The
  most structurally relevant prior art; not embeddable as a library, and the cloud features are SaaS.
  *(Inferred, [S16])*
- **Visual Studio** — a WPF+WinForms shell embedding the Windows Terminal renderer through `HwndHost`,
  reportedly via the same `CI.Microsoft.Terminal.Wpf` path. Highest-fidelity terminal in a WPF shell, and
  entirely internal. *(Inferred — the VS claim is a secondary citation in the EasyWindowsTerminalControl
  README with no primary source, **Flagged**)*
- **Windows Terminal** — C++/WinRT, ConPTY, DirectWrite renderer, WinUI 3 XAML, pane splits. The gold
  standard for rendering, MIT, and **not embeddable** — there is no stable embedding API. *(Verified)*
- **Cursor** (VS Code fork), **JetBrains Fleet** (Compose/Skia UI, remote backend), **Amazon Q / Fig**
  (shell integration via OSC plus a daemon, overlay near the cursor). None offers an embedding API; Q/Fig is
  notable for augmenting *any* terminal without hosting one. *(Inferred — secondary sources)*
