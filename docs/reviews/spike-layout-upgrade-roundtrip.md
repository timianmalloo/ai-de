---
id: spike-layout-upgrade-roundtrip
title: "Spike — layout round-trip across an app upgrade"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1b"
tags: [spike, layout, migration, persistence, adr-0012]
links:
  - { to: adr-0013-layout-persistence-envelope, rel: documents }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: design-phase-1b-workbench, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The ADR-0012 round-trip spike, run. It found that the versioned envelope had a version field but no
  migration hook, so the first release to rename a surface would have degraded every saved layout to
  the default. The hook is now implemented and pinned by tests.
---

# Spike — layout round-trip across an app upgrade

- **Run:** 2026-08-26 · .NET 10.0.303
- **Question (ADR-0012):** does a saved layout survive an app upgrade, and does an unresolvable
  entry restore correctly rather than vanishing?

## What the spike found

**ADR-0013 promised a migration hook. The version *field* shipped; the *hook* did not.**

`LayoutStore.Load` handled only `schemaVersion > current` — a file written by a *newer* build. A file
written by an **older** build fell through and was read as if it were current. That is fine exactly
until the schema changes, and then it is not:

> The first release that renames a surface id would find that id missing from the available set,
> drop the surface, and — because the drop cascades through stack and split collapse — **degrade the
> whole layout to the default**. Every user who had arranged their workbench would lose that
> arrangement on upgrade, silently, with an announcement blaming a missing surface.

Confirmed by deleting the migration and re-running: `WasDefaulted` becomes true. **The failure is not
"one pane goes missing" — it is "the entire arrangement resets".**

This was invisible to the existing tests because they all wrote and read at the *same* schema
version. A round-trip test that never crosses a version boundary cannot see a migration defect.

## What was implemented

A migration chain that walks from the file's version up to the build's, one documented step at a time:

- `LayoutMigration(FromVersion, Apply)` operates on the **DTO, not the domain model** — deliberately.
  A migration's job is to read a shape today's model can no longer represent, so running it through
  today's types would defeat the purpose.
- **A gap in the chain is a hard stop**, not a silent "read it anyway": the file is a shape this build
  cannot interpret, and guessing would produce a plausible-but-wrong arrangement. It degrades with
  `AIDE-LAYOUT-VERSION-UNSUPPORTED`, says so, and preserves the original file.
- A migrated layout is **rewritten once on read**, so the cost is paid on the upgrade launch rather
  than on every launch.
- Helpers for the two changes that actually happen in practice — `RenameSurface` and `RemoveSurface`
  — with `RemoveSurface` healing the tree (empty stacks destroyed, single-child splits collapsed) at
  the DTO layer, where the domain model's invariants cannot yet reach.

The chain ships with the v1→v2 rename as its worked example, so the mechanism is exercised rather
than merely present.

## Tests (all in `LayoutUpgradeTests`)

| Test | What it pins |
|---|---|
| `AnOlderLayout_IsMigratedRatherThanSilentlyLosingRenamedSurfaces` | The headline: a v1 file in a v2 app keeps its pane. **Observed red** — without the migration the layout resets to default. |
| `AMigratedLayout_IsRewrittenAtTheCurrentSchemaVersion` | Migration is paid once, not per launch. |
| `ALayoutAlreadyAtTheCurrentVersion_IsNotMigrated` | No gratuitous rewrite of a current file. |
| `AnOlderLayout_WithNoMigrationPath_DegradesRatherThanGuessing` | A chain gap fails closed, keeps the original file. |
| `MigrationsRun_InOrder_AcrossMultipleVersions` | v1→v2→v3 runs in sequence, not just the last step. |

## What this spike does *not* establish

- **AvalonDock's own serializer is still unexercised**, because ADR-0013 deliberately does not use it
  — we persist our model and treat the library's format as an implementation detail. The ADR-0012
  probe question was framed around `JsonLayoutSerializer`; that framing is obsolete, and this is the
  equivalent question for the architecture we actually have.
- **No real upgrade has been performed.** This simulates the version gap in-process. An end-to-end
  install-over-install test belongs with the Phase-2 upgrade work (`P2-UPGRADE-01`).
- Floating-pane display coordinates across machines remain out of scope (layouts are local-device).
