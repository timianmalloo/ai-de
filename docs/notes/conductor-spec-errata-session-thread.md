---
id: note-conductor-spec-errata-session-thread
title: "Spec erratum — Addendum A §A2/§A6/R16 draw the session as composer-beside-canvas; Ruling 74 makes the session document a thread with the Console split on demand"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [conductor, spec, errata, addendum-a, addendum-c, session, thread, console, ruling-74, ruling-21]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: note-conductor-spec-errata-policy, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-session-design-thread-not-panes, rel: relates-to }
  - { to: mockup-session-conversation, rel: relates-to }
review-by: 2027-03-11
summary: >-
  Addendum A's Phase-1 default draws the session document as a composer pane beside an output
  canvas (§A2 line 104, §A6 line 159, the §A6.1 note at line 185, R16 at lines 238-242). Ruling 74
  amends that default: the session document is a Layout:StreamingThread — the turns live in the
  thread with the lane's reply folded beneath the turn that caused it, and the Console split is an
  on-demand view of the same stream opened at a turn. Ruling 21's split and Ruling 45's Console-only
  strip stand. The HTML stays byte-frozen; this note is the correction.
---

# Spec erratum — Addendum A §A2 / §A6 / R16: the session document is a thread; the Console split is on demand (Ruling 74)

**Filed by:** the conductor's errata node (session `errata-74-78`), 2026-09-11, applying **Ruling 74**
(`note-addendum-c-council-rulings`). Confidence: **Verified** — each quoted line was read directly from
`docs/specs/conductor/ai-de-spec-addendum-a-session-experience.html` at its cited line number in this
worktree; the ruling text was read in full.

Per `note-conductor-spec-errata-policy`: the addendum HTML stays byte-intact (its SHA-256 in
`docs/specs/conductor/README.md` is the control); this note is the correction, linked from that README.

## What Ruling 74 says

> Amend Addendum C §C1 so the *session document* carries `Layout:StreamingThread` (the shell stays
> `HubAndSpoke`/`MultiPanelWorkstation`); amend §B2's earlier-turns paragraph and Addendum A
> §A2/§A6/R16's default so the turns live in the thread with the lane's reply folded beneath the turn
> that caused it, and the Console split is an on-demand view of the same stream opened at a turn;
> Addendum D Part C stands as written. Item 2 is a clause of this ruling, not an erratum.

The Owner's BECAUSE names the default this changes — Addendum A `:104` *"composer pane left, output
canvas right"* and R16 `:238-242` — and re-reads Ruling 21's *"the canvas split is IN"* as on-demand.
Ruling 45 (the Console-only strip) and Ruling 21's mechanism are untouched: the split still exists
and still has its oracle, re-pointed (condition 1: the split's rows equal the folded events of every
turn, in order).

## The four spec passages this corrects

**§A2, line 104** (the session opens as a document):

> `  <li>The session opens as a <b>document in the Workbench</b>: composer pane left, output canvas right, in the paired-zone layout from the mockups. The layout is a preset, not a new window type; docking, zones, and persistence behave like every other surface.</li>`

Reads now: the session opens as a **document in the Workbench** whose body is **one thread** — the
turns above, each with its reply folded beneath it, and the composer as the thread's last region; no
pane sits beside it at rest. The layout is still a preset, not a new window type; docking, zones and
persistence behave like every other surface (Ruling 47's maximize-on-create stands).

**§A6, line 159** (the dock document and its preset):

> `<p>A session is a <b>dock document</b> rendered by <code>SurfaceContentFactory</code> into the paired-zone layout preset (composer zone + canvas zone, splitter between). ADR-0017's retain-never-rebuild invariant applies: unparenting the session document (mode switch, tab switch) never disposes the WebView2 surface or any lane. Multiple sessions may be open as sibling documents; exactly one is active for keyboard focus.</p>`

Reads now: a session is a **dock document** rendered by `SurfaceContentFactory` as **the thread**
(`Layout:StreamingThread` — Addendum C §C1 as amended); the paired-zone preset (composer zone +
canvas zone, splitter between) is **the on-demand Console split**, opened from the header at a turn,
not the rest state. ADR-0017's retain-never-rebuild invariant, sibling documents and the single active
document are unchanged.

**§A6.1, line 185** (the canvas modes note):

> `<p class="note">Mode is per-canvas, switchable instantly, and split-able: the canvas zone can split to show Console beside Artifacts. New events pulse the Console tab when it isn't visible; a permission request raises the queue overlay regardless of mode — approvals are never hostage to the selected tab.</p>`

Reads now: the canvas modes are **views of the thread's stream**; the Console is the **reply side of
each turn, folded per turn** (`DESIGN.md` SC7), and the split that shows the merged stream beside the
thread is **on demand** (Ruling 21 honoured as on-demand; the F6 cycle reaches it when open). A
permission request is raised **in the turn that asked**, as an alert that never takes focus — never
hostage to a tab, as before. Ruling 45's Console-only strip is unchanged.

**R16, lines 238–242** (Output canvas modes, P0):

> `<div class="req"><span class="id">R16</span><span class="t">Output canvas modes</span><span class="pill p0">P0</span>`
> `<p>Console default; modes switchable and splittable.</p>`
> `<ul>`
> `  <li>Console renders the merged stream with lane rail and tree filtering (mockups v2 fidelity); default on session open.</li>`
> `  <li>Mode switch is instant and lossless (retain-never-rebuild); canvas zone splits to two modes side by side.</li>`

Reads now: **the thread is the default on session open**; the Console (the merged stream with lane
rail and tree filtering) is the reply side folded under each turn and, as the **split**, an on-demand
view of the same stream opened at a turn — derived from the turns' events, never a second store
(`DESIGN.md` SC1). Mode switch stays instant and lossless (retain-never-rebuild); the canvas zone's
side-by-side split is the on-demand split, not the rest state. Ruling 21's red-first oracle is
**re-pointed, not dropped**: the split's rows equal the folded events of every turn, in order.

## Scope effect

**Freezes** the addendum HTML, as the policy requires. **Records** Ruling 74's default for the
session document: `Layout:StreamingThread`, the reply folded per turn, the split on demand. **Keeps**
Ruling 21 (the split, on demand), Ruling 45 (the Console-only strip), Ruling 47 (maximize on create).
**Amended in step:** Addendum C §C1 and §B2 (in-repo, edited directly); Addendum B `:186`'s Score
outline is superseded by the jump list — `note-conductor-spec-errata-template-control`. Addendum D
Part C stands as written.
