---
id: adr-0008-shell-host
title: "ADR-0008 — WPF frame with embedded WebView2 as the shell host"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, shell, wpf, webview2, desktop]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: adr-0005-terminal-runtime-boundary, rel: relates-to }
review-by: 2027-02-26
summary: >-
  Records the desktop shell-host decision the earlier draft left implicit: a WPF window frame with an
  embedded WebView2 for visual surfaces and a renderer-independent terminal runtime, with the Phase-2
  renderer/airspace spike as the explicit reversal trigger.
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes" }
---

# ADR-0008: WPF frame with embedded WebView2 as the shell host

- **Status:** Proposed
- **Date:** 2026-08-26
- **Deciders:** Product owner, Enterprise Architect, Desktop review
- **Context spec/architecture:** docs/architecture.md

## Context

The spec reserves the desktop framework, diagram renderer, and terminal component for
`/define-architecture` after spikes (spec §Out of scope; §Flagged risks). The prior architecture draft
treated WPF as a *constraint* and never recorded the shell host as a decision — the highest-lock-in,
longest-lived, Windows-binding choice in the system escaped the alternatives/consequences discipline
every lesser decision received (council finding, Enterprise Architect). The spec's own visual comparables
(xterm.js, React Flow/xyflow, VS Code, Theia) presuppose a web-technology rendering layer for the graph
and diagram surfaces, while the existing baseline is a .NET 10 WPF app and the terminal foundation
(ConPTY, ADR-0005) is native. LOA P1 (cheapest sufficient) and longevity/TCO are in tension: a
web-native shell would fit the visual comparables but discard the WPF baseline and native interop.

## Decision

We will keep **WPF as the window/frame/host** (docking, keyboard routing, pane lifecycle, command
palette, native Windows integration) and render the **visual evidence surfaces inside an embedded
WebView2** control, receiving only inert projection documents under a strict CSP. Terminals render
through the renderer-independent Terminal Session Runtime (ADR-0005), never inside the WebView2. The
graph/diagram renderer *inside* WebView2 (React Flow / G6 / custom) is deferred behind the projection
contract and selected by the Phase-2 spike.

## Alternatives considered

- **Pure-WPF surfaces (no web layer):** rejected because the mature interactive graph/diagram
  ecosystem the spec references is web-technology, and re-implementing it in WPF is large speculative
  cost against an unproven need.
- **Web-native shell (Electron/Tauri/Theia), discard WPF:** rejected for v1 because it throws away the
  supported .NET 10 baseline and complicates native ConPTY/Job-Object interop that the terminal and
  crash-safety design depend on; revisitable if the WebView2 airspace/perf/a11y spike fails.
- **WinUI 3 / Avalonia frame:** rejected for v1 as a lateral platform migration with no proven benefit
  over the WPF baseline; recorded so the option is not silently lost.

## Consequences

- **Positive:** keeps the baseline and native interop; gains the web graph/diagram ecosystem behind a
  contract; the shell host is now an inspectable decision with named alternatives.
- **Negative / accepted trade-offs:** AI-DE owns the WPF↔WebView2 **airspace/focus/DPI** integration and
  a CSP-hardened document channel; Windows-only remains a v1 boundary.
- **Follow-ups / new risks:** WebView2 airspace, input-latency, and screen-reader/keyboard equivalence,
  plus the in-WebView2 graph renderer selection, are a **Phase-2 prototype spike**; a failure of that
  spike is the explicit **reversal trigger** for this ADR (reconsider a web-native shell).

## Evidence

Spec comparables and out-of-scope reservation [Verified]. WebView2 airspace/a11y and terminal-control
risk are documented in the shell knowledge base and remain **Inferred** until the Phase-2 spike; this
ADR records the decision and its reversal trigger rather than asserting the spike's result.
