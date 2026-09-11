---
id: adr-0032-perspective-layout-slots
title: "ADR-0032 — One zone-envelope file per host perspective; the pre-perspective file is Coding's slot byte-for-byte; restore drops an inadmissible kind with a report; rollback is a golden round-trip"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [architecture, ui-shell, layout, persistence, migration, perspective, addendum-c, dm-data-modelling]
links:
  - { to: architecture, rel: implements }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: adr-0013-layout-persistence-envelope, rel: refines }
  - { to: adr-0017-primary-view-mode, rel: refines }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
  - { to: note-addendum-c-persistence-slots, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-11
review-suggested:
  - { by: adr-0017-primary-view-mode, on: 2026-09-11, reason: "ADR-0017 accepted as amended (Ruling 52): the closed set is the Perspective set; a body may be a docking host; second-host clause discharged by spikes/second-dock-host-unparent" }
  - { by: adr-0013-layout-persistence-envelope, on: 2026-09-11, reason: "ADR-0013 amended (Ruling 52, ADR-0032): one zone-envelope file per host perspective; drop-with-report at restore; tested rollback" }
summary: >-
  The ADR-0013 amendment Ruling 52 named, decided: each host perspective persists its zone layout
  in its own file of the existing ZoneEnvelope schema 1 — Coding keeps today's file unchanged,
  Architecture gets a sibling — so there is no schema bump and no in-place migration; a restore drops
  a surface whose kind the perspective does not admit and reports it by caption, kind and the
  perspective that admits it; a one-time .pre-perspectives.bak precedes the first rewriting save;
  a newer schema or a corrupt file is refused with a report. Rejected: a slots map with a schema
  bump; named layouts; an Explore slot.
---

# ADR-0032: One zone-envelope file per host perspective — expand-only, drop-with-report, tested rollback

- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 (`addendum-c-chain`) with the
  **Data & Persistence Architect** in Peer Mode (holds the veto on the store and the migration);
  attacked at the gate
- **Context spec/architecture:** `spec-addendum-c-perspectives` US-C9, §A14 (release lens), Flow 4;
  `note-addendum-c-persistence-slots`; Ruling 52 SCOPE EFFECT; ADR-0013; inventory §4 and finding 9

## Context

ADR-0017 named an ADR-0013 amendment — *a per-mode persistence slot* — and Ruling 52 requires it:
*"Layout persistence gains one envelope slot per perspective … an old envelope carrying a kind a
perspective now disallows migrates by drop-with-report, never crash."* US-C9 sets the criteria:
expand, never discard; drop inadmissible kinds and report them; zero-left → default and say so; host
slots only (no Explore slot); a one-instance kind twice keeps the first; dropped surfaces are never
carried into another slot; each host's arrangement is written and restored independently; a newer
schema is refused with a report.

The facts that decide the shape: the shell persists the **zone** model through `ZoneLayoutStore`
(`ZoneEnvelope` schema **1**) to a sibling file `<layout>.zones.json`
(`LayoutPersistence.cs:52-69`) **[Verified]**; `ZoneLayoutStore.Load` **returns null on any version
mismatch** — no migration chain, unlike the tree store's `Migrations` (`ZoneLayoutStore.cs:103` vs
`LayoutStore.cs:161-181`; inventory finding 9) **[Verified]**; the tree-schema store is wired for
the legacy service the shell no longer runs (`WorkbenchShell.cs:113`) **[Verified]**. So *any*
schema bump on the zone envelope today discards every user's saved arrangement, and a rolled-back
binary would refuse the bumped file whole. DM principles: expand-migrate-contract; a migration never
guesses; the rollback path must be tested, not asserted (ADR-0013's own "round-trip test is a
Phase-entry criterion").

## Decision

We will:

1. **Persist one zone-envelope file per host perspective, in the existing schema.** Coding's slot is
   **today's file, `<layout>.zones.json`, byte-compatible at read** (every save rewrites `savedUtc`
   and `appVersion`, as today) — a pre-Addendum-C envelope *is* the Coding slot, so "read into the
   Coding slot" is a no-op on the file. Architecture's slot is the sibling
   **`<layout>.architecture.zones.json`**, same `ZoneEnvelope` schema 1, same store class, same
   debounce. The file-name suffix is the slot's natural key (the perspective id), mapped by **one
   function, `SlotPathFor(layoutFilePath, perspectiveId)`** (`ZonesPathFor`'s successor,
   `LayoutPersistence.cs:64-69`); Coding's empty suffix is a **grandfathering decision**, recorded
   here, so the map is not uniform and must not be re-derived anywhere else. **A new host
   perspective is a new file, never a schema field** — expand-only by construction. Explore has no
   file (non-goal 5).
2. **Drop-with-report is a restore-time filter owned by the host's layout service** (ADR-0031 rule 3),
   applied to whatever the store returns: a surface whose kind the perspective's allow-list
   (ADR-0030) does not admit, or a second instance of a one-instance kind, is dropped from *that*
   slot and **reported by caption and kind, naming the perspective that admits it** — through the
   `RestoreResult` and the live region (the §C4 strings). Zero left → the perspective's default and
   the report says so. Dropped surfaces are **not** carried into any other slot; Architecture starts
   from its default the first time.
3. **The original is preserved before it is rewritten — atomic replace with a backup, from the
   BCL.** Every zone-envelope save writes a temp file and **`File.Replace(temp, dest, backup)`**
   (or `File.Move(temp, dest, overwrite: true)` when no backup is due) — the original is never
   overwritten until the new file is complete, so a crash mid-write cannot tear `zones.json` (both
   stores write in place today, `ZoneLayoutStore.cs:78`, `LayoutStore.cs:114`; the Patterns
   Expert's finding: a verified copy beside a non-atomic live write still tears the file the product
   reads). The first save after a restore that dropped anything passes
   `<layout>.zones.json.pre-perspectives.bak` as the backup — **once**, guarded by the backup's
   absence — so the pre-perspective bytes are preserved by the same call that replaces them; a
   failed replace **leaves the original unwritten and is reported** (the tree store's precedent at
   `LayoutStore.cs:283-291` is best-effort with `IOException` swallowed — this is the fail-safe half
   it lacks). The `.bak` is never read by the product; it exists for manual restoration. It lands
   beside `layout.json` in the workspace data directory (`WorkbenchShell.cs:388`) — the D&P test
   asserts that directory is git-ignored (**Inferred** until the slice opens `SessionPaths`). Two
   suffixes (`.pre-perspectives.bak` here, `.bak` in rule 4) are kept deliberately: the two events
   can both occur on one file (a pre-perspective restore, then a later refusal), and one suffix
   guarded by absence would either overwrite the earlier backup or skip the later one — the floor
   is the preserved bytes of *both* events (the Simplifier's finding, answered).
4. **Refusal is reported, never silent — and a refused file is never overwritten silently.**
   `ZoneLayoutStore.Load` distinguishes *no file* · *newer schema* · *corrupt* and returns the reason
   with the null; `LayoutPersistence.Restore` reports the reason in the same result it reports a
   drop; and **that slot's first save (debounced or at exit — `LayoutPersistence.cs:143-145, 172`
   save unconditionally today) replaces the refused file with `<file>.bak` as the backup** by the
   same `File.Replace` rule, so ADR-0013's *"never silently discards a layout … preserve the original
   file"* holds on the zone path too (the tree store already does this at `LayoutStore.cs:285`; the
   zone store did not). This closes inventory finding 9's *silent* half for the zone store; the
   missing migration *chain* is not built until the first zone-schema bump needs it (YAGNI — this
   decision creates no such bump), and that bump must add the chain first.
5. **Rollback is a golden round-trip against a frozen reader, not a claim.** A build that predates
   this ADR reads the post-ADR Coding file (the same schema, possibly with fewer surfaces) and never
   sees the Architecture file. The pre-ADR reader is a binary the test cannot run, so the oracle is
   the **DTO contract**: the schema-1 records are positional (`ZoneLayoutStore.cs:10-42`), so a
   *removed* member breaks a rollback build while an *added* one is ignored. The test serialises with
   the new store and deserialises with a **frozen copy of the schema-1 DTO records** (or a committed
   golden file from the pre-ADR build), asserts `schemaVersion == 1` and the arrangement
   round-trips, and a reflection check asserts no constructor parameter was added to or removed
   from those records.

## Alternatives considered

- **One envelope with a `slots: {coding, architecture}` map and `schemaVersion: 2`:** rejected by
  the D&P Architect — the zone store has no migration chain, so v1 files would be discarded on first
  run unless the chain were built now; and a rollback build refuses a v2 file whole, losing the
  Coding arrangement. A map also invites a fourth slot to be added as a field (a schema change) where
  a file is a row.
- **ADR-0013's named layouts as the slot mechanism:** rejected — a named layout is an arrangement
  the operator applies by choice, with declared axes; a perspective slot is implicit, exclusive and
  never chosen; conflating them makes `workbench.resetLayout` ambiguous (US-C9 scopes it to the
  active slot).
- **Migrating the file in place (rewrite `zones.json` as v2 and delete the original):** rejected —
  a migration that destroys its input has no rollback (§A14's release lens); the `.bak` costs one
  copy.
- **An Explore slot reserved now (an empty field for the split ratio and last node):** rejected —
  Addendum C non-goal 5 and the Simplifier's cut at the spec gate; when Explore persistence lands it
  is a new file by rule 1, not a reserved field.
- **A filter at the store (dropping inadmissible kinds inside `ZoneLayoutStore.Load`):** rejected —
  the store already filters by *availability* (`restorableKinds`) and must stay perspective-blind so
  the same class serves every slot; admissibility is the host's invariant (the Perspective Layout
  aggregate), enforced where every other mutation is.

## Consequences

- **Positive:** zero schema change, zero bytes migrated, a rollback that is trivially safe; the
  drop-with-report path is the same code path that reports an unavailable surface today; the store
  class is reused unchanged for the Architecture file.
- **Negative / accepted:** a stale `.pre-perspectives.bak` lingers (one file per workspace, written
  at most once); the Coding file's *content* changes on the first post-drop save (the class diagram
  leaves it), which a rollback build then does not show — recorded as the accepted trade, since the
  `.bak` preserves the original.
- **Follow-ups / new risks:** layout files are user data and must appear in the workspace deletion
  purge (ADR-0013's follow-up) — the Architecture file joins that list; the zone store's missing
  migration chain stays an open finding with a trigger (the first zone-schema bump).

## Falsifying tests (headless on the store and the host's service)

1. A pre-Addendum-C `zones.json` — today's default, saved (`canvas`, **both** `view` instances,
   `inspector`, `contexts`, `joins`, `sessions`, `board`, `leaderboard`, `ledger`, `terminal` —
   `ZoneLayout.cs:131-152`; the default holds no class diagram) **plus one opened `classdiagram`**
   (`WorkbenchShell.cs:337`) — restores into Coding with exactly `canvas`, `view` ×2, `inspector`,
   `contexts`, `joins` and `classdiagram` dropped, the report naming each by caption and kind and
   *Architecture* as the perspective that admits them, the event carrying the dropped count and
   kinds (US-C12 b3); a `classdiagram` or a `view` surviving in the Coding slot, a crash, or a silent
   reset fails.
2. An envelope whose every surface is inadmissible restores the Coding default and reports so; an
   empty host with four empty zones fails.
3. Two surfaces of a one-instance kind **with distinct `surfaceId`s** keep the first and report
   the second (a duplicate `surfaceId` never reaches the service — `ZoneLayout.AssertInvariant`,
   `ZoneLayout.cs:207-211`, refuses the whole file, which test 7 treats as *corrupt, reported*;
   whole-file refusal for one duplicate id is accepted and recorded here).
4. Host A's file and host B's file are written independently (arrange A, switch, arrange B, restart:
   each restores its own); switching perspective never resets the other host's arrangement.
5. The Architecture file is absent after a Coding-era restore (dropped surfaces not carried over);
   Architecture opens with its default.
6. **Rollback golden round-trip:** the post-ADR Coding file deserialises with the frozen schema-1
   DTO copy (or the committed golden) into the same arrangement; the reflection check on the DTO
   constructors passes; the `.pre-perspectives.bak` exists after the first post-drop save and equals
   the original bytes; **with the backup target unwritable, `SaveNow()` leaves the Coding file's
   bytes unchanged and reports** — never writes over the only original; a save interrupted after
   the temp file is written and before the replace leaves the destination intact.
7. A `schemaVersion: 2` file, a corrupt file and a file with a duplicate `surfaceId` are each
   refused *with the reason* in the `RestoreResult`; a null with no reason fails; **refuse →
   `SaveNow()` → the refused bytes exist at `<file>.bak`** before the slot's file is rewritten.

## LOA mapping

T0. Patterns (named as the Patterns Expert corrected them): **Parallel Change — expand only, no
contract phase** (an additive sibling file; no migration occurs, the schema is unchanged),
**Atomic Replace (temp + rename) with backup-before-write** (`System.IO.File.Replace`), **Reported
degradation** (C7). DM: the slot key is a natural key (perspective id) carried by the file name;
history rule Type-1 for the arrangement (a layout is preference, rebuildable from default —
ADR-0013), so no version history is kept beyond the one-time `.bak`.

## Evidence

- **Verified (read):** `LayoutPersistence.cs:52-69, 90-118, 140-160`; `ZoneLayoutStore.cs:53,
  86-118`; `LayoutStore.cs:161-181`; `WorkbenchShell.cs:113`; inventory §4/finding 9.
- **Rulings:** 52 SCOPE EFFECT (one slot per perspective; drop-with-report); Addendum C US-C9's
  criteria; `note-addendum-c-persistence-slots` (dropped surfaces not carried over).
- **Inferred:** that the workspace deletion purge already enumerates layout files (ADR-0013 says it
  must; not re-verified here) — the slice adds the Architecture file to it either way.
