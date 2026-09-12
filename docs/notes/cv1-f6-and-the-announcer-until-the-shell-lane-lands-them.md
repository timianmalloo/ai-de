---
id: note-cv1-f6-and-the-announcer-until-the-shell-lane-lands-them
title: "F6 is the session document's own key handler and the document builds its own live region until the Shell lane lands the two registry rows and passes the shell's announcer"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, conversation-lane, cv-1, sc8, sc9, seam, shell-lane, dc-068, dc-072]
links:
  - { to: proof-composer-as-conversation, rel: relates-to }
  - { to: design-session-thread-itemscontrol, rel: relates-to }
  - { to: coordination-addendum-cd, rel: relates-to }
review-by: 2026-12-12
review-suggested: []
summary: >-
  DS-1 P4 puts F6 on the command registry (WorkbenchCommands.cs) and SC9 through the shell's one
  announcer; both files are the Shell lane's this horizon. CV-1 lands the behaviour where it owns
  the file — the document's PreviewKeyDown and an optional announcer parameter with a
  document-owned polite live region as the default — and files the two seam requests. Holds until
  SH-2 merges them; then the interim handler and the default region are deleted.
---

# F6 and the announcer, until the Shell lane lands them

- **Kind:** decision
- **Confidence:** Verified (the register: `docs/collaboration/session-contracts.md` §Shell lane
  owns `WorkbenchCommands.cs`, `MainMenuBuilder.cs`, `CommandPalette.cs`, `WorkbenchShell.cs`)
- **Made during:** `/implement` of CV-1

## The call
1. **F6 / Shift+F6** cycle header → thread → composer → (split) through
   `SessionDocumentSurface.OnPreviewKeyDown` → `CycleRegion(±1)`. The registry rows
   `session.cycleRegion` / `session.cycleRegionBack` (DC-068's catalog · menu · palette) are
   requested from the Shell lane; when they land, the document's handler is deleted and the
   controller dispatches to the active document. No capture surface lives inside the document, so
   DC-072's yield rule has no instance here; the controller's own yield rule applies once the
   commands are ambient.
2. **The announcer.** `SessionDocumentSurface(model, store, announcer = null)`: the shell passes
   its `Announcer` (one across hosts, ADR-0031) — a one-line change at `WorkbenchShell.cs:3033`
   requested from the Shell lane. Until then a null announcer builds a document-owned
   `WorkbenchAnnouncer` over a 0-height `TextBlock` in the document's tree, so SC9 is never
   silent; the two are never both live (the parameter replaces the default, it does not add to it).

## Alternatives dismissed
- Editing the Shell lane's files from this lane — the register's one-owner rule; a write there
  is the plan's fail condition.
- Waiting for SH-2 before landing the keyboard model — the operator is waiting on this slice.
- A `RecordingAnnouncer` default — silent in the product; a live region that nobody hears is
  the DC-011 class.

## Validation condition
Holds until SH-2 merges. When it trips: delete the F6 branch in `OnPreviewKeyDown`, delete
`_ownLiveRegion`, and make the announcer parameter required.

## Promotion rule
Below ADR weight; the seam table in the plan carries it.
