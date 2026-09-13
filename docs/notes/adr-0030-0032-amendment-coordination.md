---
id: note-adr-0030-0032-amendment-coordination
title: "ADR-0030/0031/0032 amended by Ruling 84 — a fourth perspective row, a third host composition, a third slot file; and Ruling 83's re-cut of Coding's default and the session document's zone"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "Addendum C · Shell lane SH-4 (design node D3, 2026-09-13)"
tags: [decision-note, addendum-c, perspective, coordination, docking, layout, persistence, adr-amendment, ruling-83, ruling-84]
links:
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: refines }
  - { to: adr-0031-second-docking-host, rel: refines }
  - { to: adr-0032-perspective-layout-slots, rel: refines }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: ui-review-operator-findings-2026-09-13, rel: relates-to }
  - { to: mockup-perspective-shell, rel: relates-to }
review-by: 2027-03-13
review-suggested:
  - { by: mockup-perspective-shell, on: 2026-09-13, reason: "D3 /ui-design elevate: Coordination as host C (Ruling 84), Coding re-cut (Ruling 83); the body-hiding gating selector fixed" }
summary: >-
  Ruling 84 makes Coordination a fourth Perspective (host C, Ctrl+4) and re-homes the five
  Loomkeeper kinds to it as a set; Ruling 83 docks a new session in Coding's Left zone and re-cuts
  Coding's default. Neither ADR is rewritten: this note records, per ADR, the one row, composition
  or file each gains, the test each extends, and what stays as decided — the amendment the ADRs'
  own rules anticipated ("a new host perspective is a new file, never a schema field").
---

# ADR-0030 / 0031 / 0032 amended by Rulings 83 and 84

- **Kind:** decision (an amendment note; the ADRs stay `accepted` and unchanged in body)
- **Made by:** the Owner, Rulings 83–84 (`docs/notes/addendum-c-council-rulings.md:1372-1402`); recorded by node D3 (`/ui-design` elevate, session `d3-findings`), 2026-09-13
- **Evidence opened:** `Perspectives.cs:51-60` (three rows) · `ZoneLayout.cs:193-206` (`CodingDefault`) · `SurfaceContentFactory.cs:223-248` (the five kinds' `Perspectives: [Coding]`) · `NewSessionPlacement.cs:40-70` (`GiveItTheWholeTree`) · `WorkbenchShell.cs:1836-1846` (`RefusedReconcileAnnouncement`) · the ADRs' own *Falsifying tests* sections — **[Verified — read]**

## What each ADR gains (and keeps)

| ADR | Decided (kept) | Gains by Ruling 84 | Gains by Ruling 83 | The test that extends |
|---|---|---|---|---|
| **ADR-0030** — the perspective registry and the allow-lists | A registry row per perspective; an explicit allow-list set on every kind row with no default; the derived menu; `Resolve` | **One row:** `("coordination", "Coordination", 4, DockHost, "perspective.coordination")`, gesture `Ctrl+4` from the row's `Order`; the five Loomkeeper rows' sets become `[Coordination]` (**Coding admits none**); `Resolve("ledger", Coding) == Coordination` | — | Test 1: *exactly four rows in order Coding · Explore · Architecture · Coordination*; a **fifth** row fails. Test 2: the sets equal §A7 **as amended** (the fifth column). Test 3 (the mutation test) now expects the Loomkeeper entries in **Coordination's** View menu and nowhere else. Test 4: `Resolve("ledger", Coding) == Coordination`. |
| **ADR-0031** — the second docking host | Two hosts under one presenter, one announcer, one focus state; "a third composition is one more `DockHost.Create`" | **A third composition, host C**, created by `DockHost.Create` with the same presenter, announcer and focus state; **P-4's `private_bytes_delta` measured for host C** (Ruling 84 CONDITIONS) | The Coding host's default is **re-cut**: Left = session documents, Center = empty, Bottom = one terminal; the `session-document` kind's zone is **Left**; `NewSessionPlacement.GiveItTheWholeTree` **retires** (a new session is `AddSurface` into Left, `Maximized == null`) | US-C2's identity cycle over **four** bodies (`Assert.Same` on host C); Ruling 47 condition 1's oracle **re-pointed**: create → the session's stack is in `ZoneId.Left` and `Current.Maximized == null`; reopen unchanged. |
| **ADR-0032** — one zone-envelope file per host perspective | Coding's file byte-for-byte; a sibling per host; drop-with-report; refuse-newer; no schema bump | **A third slot file** `<layout>.coordination.zones.json`, schema 1, sibling of the two | `CodingDefault()` re-cut (`ZoneLayout.cs:193-206`): `left = null` (empty until a session opens — an empty `ZoneStack` is not constructible, so the zone's `Content` is `null` and it renders as its collapsed rail), `bottom = terminal-1`, `center = null`; the Left extent is the design language's value (`DESIGN.md`, one owner) | Test 1 extends: a pre-C Coding envelope carrying `sessions`, `board`, `leaderboard`, `ledger` (and, if opened, `daydreams`) restores into Coding with **those five dropped and reported by caption, kind and *Coordination* as the perspective that admits them**, the event carrying the dropped count and kinds; a Loomkeeper kind surviving in the Coding slot fails. Test 2's "restores the Coding default" now means the re-cut default. |

## What this note does not change

- ADR-0032's *rejected alternatives* (a slots map, named layouts, an Explore slot) stay rejected — the third file is the accepted shape's third instance.
- The reconcile's blindness under a maximized stack (`RefusedReconcileAnnouncement`, Ruling 83's BECAUSE) is a **Shell-lane defect class**, not an ADR amendment: Ruling 83 removes the trigger (maximize-on-create), not the cause; the review's ranked plan names the finding for SH-4.
- Ruling 62's caption *Terminal sessions* is unchanged (it moves benches, not names).

## Residual

- The Coordination default arrangement is the Owner's, **Inferred** until the operator runs the next build (the review's attended rows).
- Whether host C's `private_bytes_delta` (P-4) stays within the budget ADR-0031 set for host B is **not measured** until SH-4's Proof Pack.
