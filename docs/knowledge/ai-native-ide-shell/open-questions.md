---
id: kb-shell-open-questions
title: "AI-Native IDE Shell — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, conpty, webview2, wpf]
links:
  - { to: kb-ai-native-ide-shell, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What could not be settled about shell hosting, the documented ways this class of project
  fails, and the three counter-arguments — WinUI 3, xterm.js and FileSystemWatcher — assessed
  on evidence.
---

# Open questions & domain failure modes

## Unresolved by research

1. **AvalonDock v5's exact release date and changelog** — the README confirms the .NET 10 badge and the v5
   feature set, but the releases page errored during research. *(Flagged)*
2. **The exact current `CI.Microsoft.Terminal.Wpf` version.** A web-search citation showed
   `1.25.260303002`; the package page fetch returned only the dependents table. Since the mitigation for an
   unsupported package is **pinning an exact version**, this specific gap matters. *(Flagged)*
3. **Can WebView2 renderer processes be consolidated beyond origin-based rules?** Microsoft documents this as
   *"beyond the scope of the WebView2 Runtime"* with no configuration API. *(Open)*
4. **Avalonia's current stable version** — releases page errored; the redirect suggested `12.1.1`,
   unconfirmed from page content. *(Flagged)*
5. **Does MAUI have any desktop docking ecosystem?** No sources found; assumed absent. *(Inferred)*
6. **Does Visual Studio actually use `CI.Microsoft.Terminal.Wpf`?** The claim comes from the
   `EasyWindowsTerminalControl` README with no primary source and no VS version named. *(Flagged)*

## Known failure modes of this domain

- **Terminal instability under version drift.** Projects depending on `CI.Microsoft.Terminal.Wpf` break when
  Windows Terminal ships a breaking internal change. Mitigation: pin the exact package version and re-test
  on each Windows Terminal release — which is why finding that version number matters.
- **WebView2 multi-instance memory growth.** Four panes on four environments means four browser processes
  plus four GPU processes — plausibly **800–1200 MB**. Mitigation: one shared environment, one origin.
  *(Mechanism Verified; the figure Inferred)*
- **ConPTY deadlock.** Servicing read and write on one thread deadlocks when the output buffer fills. This
  is in the MSDN warning, and it is the single easiest catastrophic mistake in this domain.
- **Layout persistence corruption.** AvalonDock serialises pane identity by content ID; if view-model
  identifiers change between versions, deserialisation **silently discards** unrecognised panes and the
  user's layout quietly degrades. Mitigation: version the layout schema.
- **`FileSystemWatcher` event loss.** At high write frequency — an agent streaming output to a log — the
  8 KB buffer overflows before the consumer drains it, and the events are gone. Mitigation: 64 KB buffer,
  debounce, and prefer the PTY stream where real-time matters.
- **OSC sequence smuggling.** If agent output contains a raw `ESC]133;D` — for instance an LLM emitting a
  code block that contains one — the host invents a command boundary that never happened. VS Code's OSC 633
  nonce scheme is the published mitigation; without an equivalent, boundaries are advisory.
- **The `HwndHost` airspace problem.** Terminal content renders above all WPF content, so docking drag
  adorners, tooltips and popups over a terminal pane are invisible. This is a classic WPF limitation and it
  will be hit on the first day of docking work, not in some later phase.

## Disconfirming views we deliberately sought

### 1. "WPF is legacy; build on WinUI 3 for future-proofing"

**The claim.** WinUI 3 is Microsoft's strategic platform, uses native composition, renders better on Windows
11, and is where the investment goes. Building on WPF accumulates technical debt.

**Evidence examined.** Windows App SDK 2.4 (August 2026) confirms WinUI 3 is actively shipped, and the
migration guide is maintained. But WPF in .NET 10 received performance work, new APIs and ~4,000 new tests —
which is not the profile of an abandoned framework.

**How it fared.** **Partially credible in general, materially weakened for an IDE shell specifically** by
three concrete gaps: no `HwndHost` (terminal embedding becomes much harder), no first-party docking control,
and no first-party DataGrid. The "legacy" label overstates WPF's status on evidence. **WPF is the more
pragmatic choice today** — and the honest caveat is that this is a *today* judgement that should be
re-examined if WinUI 3 gains a docking story and an `HwndHost` equivalent.

### 2. "Use xterm.js in WebView2 instead of the CI terminal control — it is more stable"

**The claim.** Since `CI.Microsoft.Terminal.Wpf` is an unstable CI artefact, host xterm.js in a WebView2
panel instead; this removes the native dependency risk and is what VS Code already does.

**How it fared.** **Credible, and it is a real fork in the road.** xterm.js is more stable at the dependency
level. The cost is rendering fidelity: Windows Terminal's DirectWrite + GPU atlas renderer produces
ligature-quality text that xterm.js's Canvas/WebGL renderer approaches but does not match. **For an
agent-centric shell where reliability and API stability outrank terminal aesthetics, the xterm.js path is
defensible.** The decision reduces cleanly to: *does terminal rendering quality or dependency stability
matter more here?* — and because the shell already owns the ConPTY lifecycle, the renderer can be swapped
later without redesigning session management. That makes this a deferrable decision, which is the best kind.

### 3. "`FileSystemWatcher` is unreliable; never use it as an event bus"

**The claim.** `ReadDirectoryChangesW` loses events under load and is not a reliable IPC primitive.

**How it fared.** **Accurate as stated and not disqualifying.** The failure is real and documented — an 8 KB
default buffer, silent drops on overflow — and it is avoidable by enlarging the buffer, debouncing, and
treating the file as an **append log** rather than expecting per-event delivery. For low-frequency agent
lifecycle events (task started, task completed) the risk is low; for line-by-line streaming the PTY stream
is strictly better. **The concern is valid, and it argues for using both channels for different frequencies
rather than choosing one.**

## What this adds up to

Three of the four architectural risks in this domain — the unsupported terminal control, WebView2 memory,
and event-bus reliability — are **mitigable by known techniques**, and the fourth (the airspace problem) is
a known WPF limitation with known workarounds. None is a reason to change platform. The one genuinely open
decision is the terminal renderer, and owning ConPTY directly is what keeps it open.
