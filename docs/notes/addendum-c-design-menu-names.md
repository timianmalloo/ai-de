---
id: note-addendum-c-design-menu-names
title: "The top-level menus are File · Edit · View · Window · Prompt · Help — Terminal renamed to what it holds, the design language's Graph · Model · Agents set retired, and Ctrl+1/2/3 confirmed as the perspective gestures"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, menu, keyboard, gestures, design-language]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-menu-derivation-rule, rel: depends-on }
  - { to: ui-review-perspective-shell, rel: relates-to }
  - { to: mockup-perspective-shell, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Spec §R row 5 handed D1 the top-level menu names (DESIGN.md said File · Edit · View · Graph ·
  Model · Agents · Window · Help; the code says File · Edit · View · Window · Terminal · Help). D1
  keeps five of the code's six and renames Terminal to Prompt, because with the terminal verbs moved
  to File as entry verbs the menu holds only prompt verbs; the derivation rule places every
  allow-list entry under View, so a Model or Graph menu would need a second placement rule. Ctrl+1/2/3
  are confirmed against a fetched Windows precedent (Outlook switches its top-level views with Ctrl+1…Ctrl+8).
---

# Menu names and the perspective gestures

- **Kind:** decision (below ADR weight)
- **Made by:** node D1 (`/ui-design`), session `addendum-c-chain`, 2026-09-11
- **Evidence opened:** `DESIGN.md` §Menu & command system (the eight names); `src/AiDe.App/Workbench/MainMenuBuilder.cs:76-98` (the six names) **[Verified — cited by the spec at §R 5 and re-read]**; spec §B3 (the three-set derivation rule; every allow-list entry under View except `prompt` → Terminal and `terminal` → File); `note-addendum-c-menu-derivation-rule`; Microsoft Support, *Keyboard shortcuts for Outlook* (fetched 2026-09-11): *Mail Ctrl+1 · Calendar Ctrl+2 · … · Tasks Ctrl+8* on Windows **[Verified]**.

## Decision 1 — six names, five of them the code's

**File · Edit · View · Window · Prompt · Help**, present per perspective by the derivation rule
(Coding: all six; Explore: File · View · Help; Architecture: File · Edit · View · Window · Help).
**Terminal → Prompt** (the UX Researcher/IA's pass-2 finding): US-C11 moves `terminal.new` and the
harness rows to File as entry verbs, so the code's Terminal menu would hold only *Dispatch prompt…*
and *New prompt draft* — a Terminal menu with no terminal verb teaches the operator to distrust the
menu (`MainMenuBuilder.cs:129-130`'s own rule). The spec's §B3 oracle table keeps its column's
contents; only the header word changes, which §R row 5 hands to D1.

**Because.** (a) The derivation rule is *the* placement rule: allow-list entries go under View. A
**Model** or **Graph** menu would need a second rule ("Architecture kinds go under Model") — a
per-perspective tuple the rule exists to avoid, and the Simplifier's L9 finding against the spec.
(b) An **Agents** menu would carry the harness rows ("New Claude Code session"), which US-C11 makes
**entry verbs in File** in every perspective; a second placement is the "one placement only" falsifier.
(c) **Prompt** names what the menu holds — dispatching a prompt to a terminal and staging a prompt
draft — now that the terminal verbs themselves live in File. (d) Six names keep `MainMenuTests`'s
shape; the one header rename is a one-word test re-scope that §A12 already plans for.

**Rejected.** *Graph · Model · Agents* (above); renaming Terminal to *Agents* (a terminal is not an
agent; the harness rows left this menu); keeping *Terminal* (the reviewer's recognition finding: an
operator seeking a terminal opens it and finds prompt dispatch). **Falsifier:** an operator who cannot find "New Class
diagram" under View in Architecture within two actions (the spec's reach criterion).

## Decision 2 — `Ctrl+1 · Ctrl+2 · Ctrl+3`, confirmed

Bound single strokes for Coding · Explore · Architecture, on `D1…D3` and `NumPad1…NumPad3`, the
tooltip rendering the binding's display string (so an AZERTY layout shows its own label).

**Because.** The spec proposed them (no digit gesture exists in the catalog) and asked D1 to confirm
against Windows conventions: Outlook for Windows switches its top-level views with `Ctrl+1…Ctrl+8`
**[Verified — fetched]**, which is precisely a perspective switch; VS Code uses `Ctrl+1/2/3` for editor
groups, a different but compatible reading (a numbered destination). **Rejected:** `Ctrl+Shift+1/2/3`
(Outlook on the web's form; a shifted digit is the AZERTY problem doubled); `Ctrl+K, 1/2/3` (an unbound
chord family; the fifth page-one fact says chords are announced, not bound).

**Not decided here.** Whether a chord-prefix handler is ever built (spec non-goal 9); the copy rule
stands: no keystroke is shown or spoken unless bound.
