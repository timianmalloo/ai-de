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
