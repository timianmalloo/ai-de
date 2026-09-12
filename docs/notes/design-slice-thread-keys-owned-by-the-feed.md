---
id: note-design-slice-thread-keys-owned-by-the-feed
title: "The thread feed owns its navigation keys on the ListBox's default tab scope — the platform's Up/End and Local tab navigation were measured wrong for a variable-height virtualized feed"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "Addendum C/D · Conversation lane · DS-1 → CV-1"
tags: [decision-note, wpf, keyboard, session, thread, feed, virtualization, spike, sc8]
links:
  - { to: design-session-thread-itemscontrol, rel: relates-to }
  - { to: ui-review-session-conversation, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Measured in spikes/session-thread: a ListBox's own Up from the last turn does not move, its End
  lands on the penultimate turn, its PageDown moves by a page, and TabNavigation Local/Continue trap
  Tab at the fold's header while the default, Once, walks the caret's turn and leaves. So ThreadFeed
  owns PageDown/PageUp/Home/End by index from the container AND from any inner control, scrolls
  three text lines on Up/Down, keeps the ListBox defaults (Once, Contained), makes each Expander
  non-focusable (one stop per fold) and never relies on the platform's geometry navigation. Blast
  radius: CV-1's ThreadFeed and its K1–K6 oracles.
---

# The thread feed owns its navigation keys on the `ListBox`'s default tab scope

- **Kind:** decision
- **Confidence:** Verified (`spikes/session-thread/RESULT.md` Q5a–Q5e; one machine, one run per mode)
- **Made during:** `/design-slice` of `design-session-thread-itemscontrol` (session `ds-1`, 2026-09-11)

## The call

`ThreadFeed : ListBox` handles PageDown, PageUp, Home and End itself through one index-based
path (`MoveBy(±1)`, `MoveTo(first | last)`: select → `ScrollIntoView` → `UpdateLayout` → focus the
container) — **from the container and from any inner control** (a button or a toggle does not
handle these keys, so without the scope rule the `ListBox`'s falsified page jump re-enters one Tab
away; only a `ScrollViewer` or a text input keeps its own keys — the UX & Accessibility lens's
pass-2 finding), scrolls the viewport by three text lines on Up / Down (`ScrollToVerticalOffset`;
the panel's 16 px line would take ~150 presses across a 2,400 px turn — the middle of a tall turn
must be reachable by keyboard, the lens's pass-1 finding),
leaves `KeyboardNavigation.TabNavigation = Once` and `DirectionalNavigation = Contained` at their
`ListBox` defaults (Q12), makes each `Expander` non-focusable so a fold is one stop (Q11), and enters
the feed on F6 through `FocusCurrentTurn()` (scroll first, then focus).

**Why.** The spike measured the platform against the design's exact shape (variable-height turns,
pixel scrolling, recycling): the `ListBox`'s PageDown from `b1` selected `b18` of 40; Up from the
last turn left the selection unchanged; End from the first turn landed on `b39` of 40; with
`TabNavigation = Local` or `Continue` a Tab from the fold's header toggle stayed on the toggle
(a trap, WCAG 2.1.2), while `Once` (the default) walked the current turn's stops and left to the
composer; a roving `IsTabStop` binding on inner controls was measured unnecessary under `Once`
(Q11). The APG feed model (`DESIGN.md:1030-1033`, SC8 `:1123`) needs one turn per Page key and
one Tab stop per feed; the platform gives the second by default and the first not at all.

## Alternatives dismissed

- The platform's navigation for Up/Down/Home/End and only PageUp/PageDown overridden — falsified
  (Q5c); two code paths for one gesture family.
- `TabNavigation = Local` with `NodeReaderView`'s explicit `FocusStops` walk (`NodeReaderView.cs:59-95`)
  — that reader's stops cross an `HwndHost`; here every stop is WPF and the default scope suffices.
  Kept as the **named fallback** (a `simplify:` ceiling) if CV-1's K5 is red on the real template.
- A roving `IsTabStop` binding on every container and inner control — measured unnecessary (Q11);
  under `Once` Tab never visits another item's subtree.
- A bare `ItemsControl` with our own current-item property — its containers have no `ListItem` peer
  (Q6c); the `ListBox`'s selection *is* the caret, cheaply.

## Validation condition

Holds until CV-1's K5 re-measures Tab on the real turn template (an `Expander` contributes two stops
unless `IsTabStop=false`; a template with more inner controls may change the count) or a .NET
major bump changes `ListBox` key handling. If `Once` stops walking the current turn's inner stops on
the real tree, fall back to the `NodeReaderView` walk for the inner stops only.

## Promotion rule

If a second feed-shaped surface (the Console split's rows, the watcher board) adopts the same key
ownership, promote to an ADR "feeds own their keys" and link it `supersedes` this note.
