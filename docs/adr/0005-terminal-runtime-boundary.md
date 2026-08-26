---
id: adr-0005-terminal-runtime-boundary
title: "ADR-0005 — Own ConPTY lifecycle behind a renderer-independent runtime"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, terminal, conpty, sessions, wpf]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: kb-ai-native-ide-shell, rel: depends-on }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
summary: >-
  Terminal process and ConPTY lifecycle belong to a stable runtime contract; WPF terminal
  controls and web renderers are replaceable views that may not own agent session state.
---

# ADR-0005: Own ConPTY lifecycle behind a renderer-independent runtime

- **Status:** Proposed
- **Date:** 2026-08-25
- **Deciders:** Product owner, SRE and Desktop review

## Context

The product must host coding-agent terminals in a WPF shell. Available WPF terminal controls are
unsupported CI artifacts, while ConPTY is the stable Windows foundation. The renderer must not
control session identity, process handles, or user dispatch authorization.

## Decision

AI-DE will implement `ITerminalSession` over ConPTY and own process/stream lifecycle in the
Terminal Session Runtime. Renderer choice is deferred behind the contract. ConPTY input and output
service loops are separate; OSC state is advisory/untrusted; terminal output is ephemeral data.

> ### Renderer choice resolved 2026-08-26 — spike S3: **own a WPF renderer**
>
> [`spikes/terminal-renderer`](../../spikes/terminal-renderer/RESULT.md). The deferred choice is now
> made, on the criteria [ADR-0014](0014-accessibility-posture.md) left standing — throughput,
> fidelity, input, licence and integration cost.
>
> - **Embedding an existing WPF terminal control** stays rejected on this ADR's original grounds.
> - **Hosting `xterm.js` in WebView2** is rejected on [spike S4's](../../spikes/webview2-airspace/RESULT.md)
>   evidence: airspace in the default control, a process-killing native crash in the composition
>   control when its pane is floated, and `Focus()` refused in both. A terminal is the surface that
>   most needs keyboard focus, which makes it the worst candidate for that particular defect.
> - **Owning a WPF renderer is viable on throughput.** Full-screen redraw of a 200×50 grid: **6.64 ms
>   p95** via `GlyphRun` per line — a 151 fps ceiling against a 16.7 ms budget. VT scanning runs at
>   **2361×** the architecture's 1 MiB/s output budget, so the parser is not the constraint either.
>
> **One implementation constraint is binding, not advisory.** `FormattedText` per *cell* — the
> natural way to model a grid of independently styled cells, and what a competent implementer writes
> first — measured **142.80 ms p95, 21× slower**, and misses the budget by four times at 7 fps. The
> draw path is `GlyphRun` per line with a cached `GlyphTypeface`.
>
> Nothing measured argues for letting the renderer own session state, so the boundary this ADR draws
> is unchanged.

## Alternatives considered

- **Embed an unsupported terminal control as the session owner:** rejected because control updates
  would change process/session semantics.
- **Use a web renderer as the process owner:** rejected because browser lifecycle and terminal
  process lifetime must be independent.

## Consequences

- **Positive:** terminal renderer can be replaced; explicit session generation and health;
  secure separation of output from graph/audit/context.
- **Negative / accepted trade-off:** AI-DE owns more Windows interop and must solve the WPF
  airspace/docking presentation issue in the renderer spike.
- **Follow-up:** Phase 2 prototypes renderer candidates against this contract and accessibility
  requirements.

## Evidence

`spikes/conpty-foundation` successfully created a pseudo console on the current Windows host
[Verified]. The shell knowledge base verifies separate I/O servicing as a deadlock-avoidance
requirement.
