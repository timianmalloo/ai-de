---
id: adr-0013-layout-persistence-envelope
title: "ADR-0013 — Persist the workbench layout in an owned versioned envelope, outside the fact store"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, ui, layout, persistence, migration]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: adr-0002-workspace-fact-store, rel: relates-to }
review-by: 2027-02-26
summary: >-
  Workbench layouts are user preference, not evidence. They are stored per workspace beside the fact
  store — never inside it — wrapped in an owned {schemaVersion, appVersion, payload} envelope, and a
  layout that cannot be read degrades to the default arrangement while the original file is kept.
---

# ADR-0013: Persist the workbench layout in an owned versioned envelope, outside the fact store

- **Status:** Proposed
- **Date:** 2026-08-26
- **Deciders:** Product owner, Data & Persistence Architect, UX & Accessibility, Release Engineer
- **Context spec/architecture:** docs/architecture.md · US-9

## Context

US-9 requires arrangements to survive restart, to be saveable as named layouts, and to **degrade to
the default arrangement — never to a broken window** when they cannot be read. Two facts force this
decision rather than leaving it to the shell library:

1. **AvalonDock's `LayoutRootDto` has no version or schema field** (verified from source, ADR-0012).
   A layout persisted in the library's native form has no migration hook, so the *first* breaking
   change to our surface set strands every user's arrangement with no way to detect or repair it.
2. **A layout is not evidence.** The fact store's whole contract is that it holds immutable,
   append-only, provenance-bearing assertions about a repository. Pane geometry is none of those
   things: it is mutable by definition, changes dozens of times a session, and is rebuildable from a
   default. Putting it in the fact store would either violate append-only or force an exemption in
   the one place the architecture has refused to make exemptions.

## Decision

We will persist the workbench layout **per workspace, beside the fact store rather than inside it**,
in a file the Workbench Layout Service owns, shaped as:

```json
{ "schemaVersion": 1, "appVersion": "0.3.0", "savedAt": "...", "payload": { ... } }
```

- `payload` carries the library's serialized layout **as an opaque implementation detail**. Nothing
  outside the Layout Service interprets it, so the shell library can be replaced without changing the
  envelope contract or the migration story.
- **Named layouts** are separate records in the same store, each declaring the axes it captures
  (geometry always; optionally the open surface set and workspace filter) so applying one is a
  predictable mode change.
- **On read**, an unknown `schemaVersion`, a malformed file, or a payload the library rejects all
  produce the same outcome: **start from the default arrangement, tell the user, and preserve the
  original file** (`layout.json.bak`). The product never silently discards a layout and never starts
  in a broken window.
- **On restore**, a surface that no longer exists or a display that is no longer connected is
  reported by name and the remaining surfaces are placed validly — never dropped silently, never left
  off-screen.
- Layouts are **local-device only** (`Persistence:LocalDevice`) and are **excluded from workspace
  export** unless the user explicitly asks, because they are machine- and display-specific.

## Alternatives considered

- **Store layouts in the workspace fact store:** rejected — a mutable, frequently-rewritten preference
  has no place in an append-only evidence ledger, and every write would either break the immutability
  invariant or need an exemption that weakens it everywhere.
- **Persist the library's format directly:** rejected — no version field means no migration hook and
  no way to distinguish "old format" from "corrupt", so both would surface as the same failure.
- **Store layouts in application settings / the registry:** rejected — layouts are per *workspace*,
  not per application, and a user with five workspaces wants five arrangements.
- **Synchronise layouts across machines:** rejected for v1 — floating-pane coordinates and display
  identity are machine-specific, so a synced layout is wrong on arrival more often than it is right.

## Consequences

- **Positive:** the migration hook exists from the first release; the shell library is replaceable
  without touching the persistence contract; the fact store's append-only invariant stays absolute;
  a corrupt layout is a recoverable annoyance rather than an unusable window.
- **Negative / accepted trade-offs:** we own a small migration surface that the library would
  otherwise have owned badly; the envelope adds one indirection between the app and the library's
  serializer.
- **Follow-ups / new risks:** the round-trip test (serialize → mutate the payload → reload with
  unresolved-content handling → assert the parked surface restores) is a Phase-entry criterion, not a
  later hardening pass. Layout files are user data and must appear in the workspace deletion purge.

## Evidence

`LayoutRootDto` field list verified from AvalonDock 5.0.0 source: `RootPanel`, `TopSide`, `RightSide`,
`LeftSide`, `BottomSide`, `FloatingWindows`, `Hidden` — no version field [Verified]. AvalonDock's
`UnresolvedContentHandling.Remove | Hide` gives the primitive for the "surface no longer exists" path
[Verified]. The degradation contract is required by US-9 and by the spec's Portability NFR.
