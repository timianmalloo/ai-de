---
id: note-addendum-c-persistence-slots
title: "One persistence slot per docking-host perspective; a pre-Addendum-C envelope migrates into the Coding slot; surfaces of a kind Coding does not admit are dropped with a report and not carried to another slot"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, persistence, layout-envelope, migration, adr-0013]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: adr-0013-layout-persistence-envelope, rel: relates-to }
review-by: 2027-03-11
review-suggested:
  - { by: adr-0013-layout-persistence-envelope, on: 2026-09-11, reason: "ADR-0013 amended (Ruling 52, ADR-0032): one zone-envelope file per host perspective; drop-with-report at restore; tested rollback" }
summary: >-
  Ruling 52 says an old envelope carrying a now-disallowed kind migrates by drop-with-report, never
  crash; this note fixes the two things it left open — the old envelope becomes the Coding slot
  (expand, never discard, closing the zone schema's no-migration gap), and dropped surfaces are not
  re-homed into Architecture's slot, which starts from its default. Blast radius: every saved layout.
---

# Persistence slots and the migration of the pre-Addendum-C envelope

- **Kind:** decision
- **Confidence:** Verified for the store facts (`ZoneLayoutStore.cs:103` discards on version mismatch;
  `LayoutStore.cs:161-181` steps a migration chain — inventory §4, finding 9); **Inferred** for the
  no-re-homing choice
- **Made during:** `/specify` of `spec-addendum-c-perspectives` (node S1)

## The call

Two slots — Coding (a zone envelope) and Architecture (a zone envelope) — each written only when its
own arrangement changes, each restored independently. Explore's split ratio and last node stay
in-process (ADR-0017 names an on-disk slot; none is built, and this addendum neither builds nor
reserves it — the migration chain adds a field when that store lands; the Simplifier cut the
reservation).
**A pre-slot envelope is read into the Coding slot** (it *is* today's workbench layout), by an
expand-only step that never discards on version mismatch — the zone schema's wholesale discard
(`ZoneLayoutStore.cs:103`) is the defect this closes, not a behaviour to inherit. Surfaces in it whose
kind Coding does not admit (Graph, Domain, Explore, Provenance, Contexts, Joins in today's default) are
**dropped and reported** by caption, kind and the perspective that admits them (Ruling 52). **They are
not carried into Architecture's slot**; Architecture starts from its default layout, which contains
every one of those surfaces anyway (`spec-addendum-c-perspectives` §B4).

## Alternatives dismissed

- *Re-home dropped surfaces into the admitting perspective's slot* — speculative: it needs a zone
  mapping between two hosts for an arrangement the operator never made in host B, and Architecture's
  default already shows the same surfaces.
- *Discard the old envelope and start every slot from default* — the zone store's current behaviour;
  it throws away every operator's Coding arrangement for a schema bump they did not ask for.
- *One envelope, no slots, perspective as a filter* — a saved Architecture arrangement would be
  filtered out of existence every time Coding saved.

## Validation condition

Holds until a third host or a floating-window redesign (`spec-named-dock-zones` non-goal) changes what
a slot must hold. Re-check when `/design-slice` fixes the envelope's version step.

## Promotion rule

This is the ADR-0013 amendment ADR-0017 already names; A1 records it as such and this note becomes its
origin.
