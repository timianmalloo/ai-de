---
id: note-2026-08-30-prompt-draft-wiring
title: "Prompt-draft surface — built foundation & the shell-wiring plan"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [prompt-draft, editor-surfaces, dispatch, wiring, phasing]
links:
  - { to: spec-editor-surfaces, rel: relates-to }
  - { to: mockup-editor-surfaces, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  What landed for the prompt-draft surface (spec-editor-surfaces US-ED5–ED7) and the precise shell
  wiring that finishes it. Built + tested this increment: the testable transfer core
  (PromptDraftViewModel, 7 tests), the surface UI (PromptDraftSurface), and the factory kind "prompt".
  The remaining wiring is a self-contained next increment on the central WorkbenchShell dispatch
  choreography, deliberately split out so that change is its own verified step.
---

# Prompt-draft surface — foundation + wiring plan

## Built and verified this increment (green)

- **`PromptDraftViewModel`** — the transfer rules (US-ED5–ED7): staged body, live ready-target list,
  `CanTransfer`/`BlockedReason` gating (no ready target · empty body · already transferred), and a
  **one-way** `TransferAsync` that names its target (selected, or first ready when the selection is
  stale). Fully unit-tested headlessly (`PromptDraftTests`, 7 tests) — dispatch and targets injected.
- **`PromptDraftSurface`** — the UI: staged badge, draft `TextBox`, target picker, Transfer button
  with the stated blocked reason, transferred confirmation (draft goes read-only), saved indicator.
  Exposes `Configure(readyTargets, dispatch, initialBody, persist)` — the shell-wiring seam — and
  `RefreshTargets()`.
- **Factory** — kind `"prompt"` → `PromptDraftSurface`, wrapped as an island like every non-windowed
  pane. Constructible from the layout today.

App.Tests 144 → **151** green.

## The remaining wiring (the next increment — self-contained)

1. **`ReadyPromptTargets()` on `WorkbenchShell`** — enumerate every `TerminalSurface` in the adapter
   (`Adapter` stacks → `ContentFor`), keep those whose readiness is `Ready` (the same
   `ReadinessEvidence`/`Activity` computation the existing `Prompt.Dispatch` lambda already does), and
   project to `PromptTarget(session.SessionId, surface.DisplayName ?? title)`.
2. **`DispatchToAsync(string sessionId, string body)` on `WorkbenchShell`** — generalize the existing
   focused-terminal dispatch to a **named** session. Refactor: store `_dispatch` (`IWorkspaceDispatch`)
   and `_scopeId` as fields (today they are locals in `AttachWorkspace`), extract the ~30-line
   choreography (build `DispatchCommand`, compute readiness, `BoundaryDispatcher.BeginAndWriteAsync`)
   into `private Task<DispatchReceipt> DispatchToSurfaceAsync(TerminalSurface surface, string body)`,
   and have **both** `Prompt.Dispatch` (resolves `FocusedTerminal()`) and `DispatchToAsync` (resolves
   the surface by id from the adapter) call it. Low-risk because it is an *extract-and-share*, not a
   behaviour change to the existing path. Return whether it was accepted (map the receipt).
3. **`BindPromptDrafts()`** — after `Adapter.Render()` (beside `BindCanvas`/`BindTerminalAttention`),
   find each `PromptDraftSurface` and call `Configure(ReadyPromptTargets, DispatchToAsync, storedBody,
   persist)`; also `RefreshTargets()` on terminal attention changes so the picker tracks the live
   ready set. Guard the `+=` like `BindCanvas` does (reconcile reuses surfaces).
4. **`PromptDraftStore`** — mirror `TerminalCustomizationStore` exactly (a JSON sidecar keyed by
   `SurfaceId` beside the layout, best-effort, off the Core model so no schema change) for US-ED5
   cross-restart persistence. `persist` = `store.Save(id, body)`; `storedBody` = `store.TryGet(id)`.
5. **Open a draft** — a `workbench.newPromptDraft` command + a `NewPromptDraftRequested` callback on
   the controller (mirror `terminal.new`/`NewTerminalRequested`), the shell adds a
   `Surface(id, "prompt", "Prompt draft")` to the active stack, renders, binds; add it to the Terminal
   (or a new Compose) menu and the command palette.

## Why split here (confidence: Verified for the built part; Inferred for the plan)

The built part is a clean, tested, landable unit. The wiring touches `WorkbenchShell`'s dispatch
choreography — the most central file in the App — so it is its own verified step rather than tacked
onto a turn that already delivered a green increment (autopilot: do not leave unverified multi-file
work mid-flight). The `Configure` seam means finishing it is a *substitution*, not a redesign.
