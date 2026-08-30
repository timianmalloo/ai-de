---
id: note-terminal-customization-persistence
title: "Decision — terminal customization persistence & busy-close"
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [terminal, customization, persistence, decision]
links:
  - { to: spec-terminal-sessions, rel: relates-to }
  - { to: inv-0002-terminal-rebuild-kills-sessions, rel: relates-to }
review-by: 2027-02-27
summary: >-
  Resolves the flagged unknowns from spec-terminal-sessions. Persistence-while-open is achieved for
  free by the DC-029 reconcile fix (the surface instance survives re-renders, so its display name,
  colour scheme and tab colour persist). Cross-restart persistence and busy-close confirmation are
  deferred as documented follow-ups.
---

# Decision — terminal customization persistence & busy-close

## Persistence while open — resolved (implemented)

`spec-terminal-sessions` US-7 requires a renamed / recoloured / re-schemed terminal to keep its look
across re-renders **while it is open**. This is now satisfied structurally: the DC-029 reconcile fix
makes `WorkbenchAdapter.Render()` **reuse the same `TerminalSurface` instance** across every layout
mutation, and the customization state (`DisplayName`, `Scheme`, `TabColour`) lives **on that
instance**. So a re-render re-projects the same surface and its customizations are intact — no Core
model change, no separate store. **Confidence: Verified** (the reconcile control test proves the
instance is preserved; BuildPane reads `DisplayName` for the caption).

## Cross-restart persistence — deferred (documented)

Whether rename / colour / scheme survive an app **restart** depends on the per-workspace layout store
(`LayoutStore`, Core-owned), which persists the layout model — and these customizations deliberately
live off the model (on the surface). Persisting them across restart is a **Core** decision: either
extend the `Surface` record with optional display-name/colour/scheme fields, or add a small sidecar
keyed by `SurfaceId`. **Deferred**; v1 requires only persistence-while-open (US-7), which is met.

## Busy-close confirmation — deferred (documented)

`spec-terminal-sessions` flagged whether closing a **running agent** session should confirm first.
Today closing a surface (Ctrl+W / tab close) ends its process via the reconcile disposal path. A
confirmation dialog for a busy session is a **usability nicety**, not a correctness requirement (the
close is intentional and the process ending is the point). **Deferred**; if added, it belongs at the
`workbench.closeSurface` handler, gated on `TerminalSurface.Activity == Busy`.
