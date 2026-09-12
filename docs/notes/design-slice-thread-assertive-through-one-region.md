---
id: note-design-slice-thread-assertive-through-one-region
title: "Assertive announcements go through the one shared announcer as notification urgency (ImportantMostRecent; statuses queued with All), not a second live region — until an attended NVDA run says otherwise"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "Addendum C/D · Conversation lane · DS-1 → CV-1"
tags: [decision-note, accessibility, announcements, wpf, uia, session, thread, sc9, simplify]
links:
  - { to: design-session-thread-itemscontrol, rel: relates-to }
  - { to: ui-review-session-conversation, rel: relates-to }
review-by: 2026-12-11
review-suggested: []
summary: >-
  SC9 needs a lane error, a refusal and a permission request to be assertive. WorkbenchAnnouncer has
  one polite live region and one notification call today; the design adds an Announcement record
  whose Assertive urgency maps to RaiseNotificationEvent(kind, ImportantMostRecent) and whose Status
  maps to (kind, All) — NVDA cancels speech only for MostRecent / ImportantMostRecent (fetched
  source), so the first draft's ImportantAll was inverted — with a per-turn activity id, over the
  same region; a second assertive TextBlock in the shell is the upgrade if NVDA does not hear it.
  The kinds are a Core enum mapped in App (Core has no WPF); the activity id is one constant.
  Blast radius: WorkbenchAnnouncer's signature (additive), CV-1's A2 and the attended A6 row.
---

# Assertive announcements go through the one shared announcer as notification urgency

- **Kind:** assumption (a `simplify:` with its ceiling and trigger)
- **Confidence:** Inferred — the property reads back (`spikes/session-thread/RESULT.md` Q8a–Q8b) but
  audibility was not measured (Q8c: the managed UIA client cannot read `LiveSetting`; NVDA Part B–D
  of `docs/reviews/nvda-workbench-session.md` is unrun since 2026-08-26)
- **Made during:** `/design-slice` of `design-session-thread-itemscontrol` (session `ds-1`, 2026-09-11)

## The call

`IWorkbenchAnnouncer.Announce(Announcement a)` with `Announcement(Text, Urgency, AnnouncementKind Kind)`
— additive; `Announce(string)` stays for the layout's callers. Both enums live in **Core**
(`AiDe.Core` is `net10.0` without WPF — the Simplifier's pass-2 finding; a WPF
`AutomationNotificationKind` cannot live there). The App-side pure `NotificationMapping.For(urgency,
kind)` yields the WPF pair: `Status` → `AutomationNotificationProcessing.All` (queued, never
cancelling the operator's current speech); `Assertive` → `ImportantMostRecent` (interrupting);
`ItemAdded / Completed / Aborted / Other` → the WPF kinds one to one (truthful — the Patterns
Expert's condition; NVDA ignores the kind, Narrator is unmeasured, A6's row is the trigger to
collapse it to the urgency). The activity id is one constant, `"aide.session.thread"` — NVDA's
cancel is global and reads no id, so a per-turn id was a Narrator hint with no measured benefit.
**Why these two and not `ImportantAll`:** NVDA's `event_UIA_notification` cancels speech only for
`MostRecent` and `ImportantMostRecent` (`source/NVDAObjects/UIA/__init__.py`, fetched 2026-09-11 by
the UX & Accessibility lens; re-read at implement) — the first draft mapped assertive to
`ImportantAll` and status to `MostRecent`, which is the inversion. A raise seam on the announcer
captures `(kind, processing, activityId)` so A2 tests the mapping and the announcer, not a mock.
The `LiveSetting` of the region stays `Polite`; no second `TextBlock` is added to the shell.

**Why.** One announcer across hosts is ADR-0031's rule (`:65-68`); the shell's live region is
`WorkbenchShell.cs:115-125`, SH-2's file in this wave; and the notification API carries its own
urgency, so the smallest correct change is a parameter. The ceiling: an AT that ignores the
notification API hears an assertive message politely, and one that honours both channels may hear
a row twice (A6 records "twice" as a result). The trigger: A6 (the attended NVDA rows) —
if the lane error is not heard as an interruption, add the assertive region (a second `TextBlock`
with `LiveSetting = Assertive` beside the polite one, the announcer choosing by kind) as a seam
request to the shell's owner.

## Alternatives dismissed

- A second assertive region now — a write to SH-2's file on an unmeasured need; adds a region every
  document shares for a behaviour no run has shown to be missing.
- Dedupe (the once-only key) inside the announcer — the announcer is shared across documents; the
  key belongs to the thread's policy (`ThreadAnnouncementPolicy`), per document.
- Per-turn live regions (a `LiveSetting=Assertive` `TextBlock` in each waiting turn's reason box) —
  kept **in addition** for the boxed reason (SC10: the reason is the element's own text), but not as
  the announcement channel: a virtualized container may not exist when the event fires.

## Validation condition

Holds until the attended NVDA run (A6) reports the outcome status once and the lane error as an
interruption. If either row is silent, the assertion "notification urgency is enough" is retired and
the second region lands; if both are heard, bump `review-by` and record the measurement in
`nvda-workbench-session.md`.

## Promotion rule

If the announcer's kinds grow (a third urgency, a per-host policy), promote to an ADR on the
announcement channel and link it `supersedes` this note.
