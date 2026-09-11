---
id: adr-0013-layout-persistence-envelope
title: "ADR-0013 — Persist the workbench layout in an owned versioned envelope, outside the fact store"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "0"
tags: [architecture, ui, layout, persistence, migration]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: adr-0002-workspace-fact-store, rel: relates-to }
  - { to: adr-0032-perspective-layout-slots, rel: relates-to }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
review-by: 2027-03-11
summary: >-
  Workbench layouts are user preference, not evidence. They are stored per workspace beside the fact
  store — never inside it — wrapped in an owned {schemaVersion, appVersion, payload} envelope, and a
  layout that cannot be read degrades to the default arrangement while the original file is kept.
review-suggested:
  - { by: adr-0017-primary-view-mode, on: 2026-09-11, reason: "ADR-0017 accepted as amended (Ruling 52): the closed set is the Perspective set; a body may be a docking host; second-host clause discharged by spikes/second-dock-host-unparent" }
---

# ADR-0013: Persist the workbench layout in an owned versioned envelope, outside the fact store

- **Status:** Accepted — **amended 2026-09-11** (see *Amendment* at the end; the decision itself stands)
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

## Amendment — 2026-09-11 (Ruling 52; ADR-0017's named amendment, decided as ADR-0032)

The decision above stands unchanged: layouts live per workspace beside the fact store in an owned
versioned envelope, and an unreadable layout degrades to the default with the original preserved.
Under the Perspective set (ADR-0017 as amended) the envelope is **per host perspective**:

- **One zone-envelope file per host.** Coding keeps today's file (`<layout>.zones.json`,
  `ZoneEnvelope` schema 1, byte-compatible — no migration of its bytes); Architecture gets a sibling
  (`<layout>.architecture.zones.json`) in the same schema. Explore has no slot (Addendum C
  non-goal 5). A new host perspective is a new file, never a schema bump — **expand-only**.
- **Drop-with-report at restore.** A surface whose kind the perspective does not admit (ADR-0030's
  allow-list) is dropped from that slot at restore and **reported by caption and kind, naming the
  perspective that admits it**; it is never carried into another perspective's slot; if the drop
  leaves zero surfaces the perspective's default layout applies and the report says so; a
  one-instance kind saved twice keeps the first. Before the first save that would rewrite a file the
  drop changed, the original is preserved as `<layout>.zones.json.pre-perspectives.bak` — **the
  mechanism (atomic replace with the `.bak` as backup) is stated once, in ADR-0032 rules 3–4**; the
  `layout.json.bak` rule above applied to the migration: the original is preserved for manual
  restoration; a rollback build reads the rewritten Coding file (same schema, fewer surfaces), never
  the `.bak`.
- **A newer schema is refused with a report** (the tree store's rule at `LayoutStore.cs:161-181`),
  not silently discarded — `ZoneLayoutStore.Load`'s null-on-mismatch (`ZoneLayoutStore.cs:103`,
  inventory finding 9) becomes a reported refusal in the slice; a corrupt file likewise; and the
  refused file is preserved as `<file>.bak` by the same ADR-0032 rule 4, so the refusal never
  becomes a silent overwrite at exit.
- **Tested rollback:** a build that predates the amendment reads the post-amendment Coding file
  unchanged (same schema, same store) and never sees the Architecture file — asserted by a golden
  round-trip in the slice (ADR-0032's falsifying test).

Rejected at the amendment, and why: a single envelope with a `slots` map and a schema bump (the zone
store has no migration chain, so every user's arrangement would be discarded on the bump and refused
whole on a rollback); the *named layouts* mechanism above (a perspective slot is implicit and
exclusive, not an arrangement the operator applies). The full record is ADR-0032.

