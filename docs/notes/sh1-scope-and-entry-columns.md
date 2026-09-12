---
id: note-sh1-scope-and-entry-columns
title: "SH-1 decisions below ADR weight: a Scope column on the catalog row, an Entry column on the kind row, derived opener ids, and how the four chord collisions were resolved"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, addendum-c, perspective, command-catalog, allow-list, shell-lane]
links:
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: refines }
  - { to: proof-perspective-registry, rel: relates-to }
  - { to: note-addendum-c-design-menu-names, rel: relates-to }
  - { to: coordination-addendum-cd, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Five choices ADR-0030 left to the implementing slice, made and defended here: the catalog row states
  what a command NEEDS (CommandScope) rather than which perspectives list it; the kind row states how it
  reaches the menu (SurfaceEntry: Derived(menu) | Verb(commandId)) so no kind can be unreachable by
  omission; the six per-kind opener commands are retired for derived surface.new/show.<kind> ids; three
  of the four US-C10 collisions vanish by derivation and the fourth is re-lettered; the prompt kind's
  bespoke placement stays a marked arm until a second kind needs one.
---

# SH-1's decisions below ADR weight

- **Kind:** decision (below ADR weight; refines ADR-0030)
- **Made by:** session `sh-1` (`/implement`, Shell lane), 2026-09-12
- **Evidence opened:** ADR-0030 (Decision, Alternatives, LOA mapping); spec §A7, §B3, US-C3, US-C4, US-C10; `note-addendum-c-design-menu-names` (PS-M1, Ctrl+1/2/3); `WorkbenchCommands.cs`, `MainMenuBuilder.cs:69-99` (the static tuple list), `SurfaceContentFactory.cs:107-128`, `WorkbenchController.cs` (the six `New*Requested` seams), `AgentReadinessProfiles.cs:106-107`; the Simplifier's and Patterns Expert's review (recorded in the Proof Pack).

## 1. `CommandScope` — the catalog row states what a command needs, never which perspectives list it

ADR-0030 rejected "a Modes/Perspectives column on the catalog row" and said body-conditional commands are "scoped by a rule on the perspective's body, which no column expresses". The rule still needs an input: *which* commands are host-only, canvas-only, or act on a surface kind. The two homes for that input are a list in the builder (the second list Ruling 55b forbids) or a column on the command's own row. The column states a **precondition** — `Global` · `DockHost` · `GraphCanvas` · `Admits(kind)` — and `PerspectiveMenu` evaluates it against the perspective row's `Body` and the kind rows' allow-lists. Adding a perspective needs no catalog edit, which is the property the ADR wanted; adding a command states its precondition once, required, no default (a defaulted scope would be offered where it cannot work — PS-M3's defect). The Patterns Expert names it a Specification-as-data with a four-op interpreter, not a Ruling-22 arm: `Offered` switches over the four rule shapes, never over a perspective or a kind.

A fourth shape, `GraphCanvas` ("the body holds a graph"), was the first form — carried for one row (`workbench.focusCanvas`) so Explore's full-window graph would keep the command. The UX & Accessibility reviewer showed the row cannot work there: the seam it runs (`CanvasFocus`) is bound to a docked `CanvasSurface` inside the workbench, unparented while Explore is the body, so in Explore the row answered "not ready" — PS-M3's forbidden offer. `GraphCanvas` is gone; `focusCanvas` is `Admits("canvas")`, absent in Explore, whose graph keeps its own Tab cycle with the reader (DC-039). Finding for the spec's §B3 rule 2.

## 2. `SurfaceEntry` — every kind row says how it reaches the menu

US-C3 b5's hole is a *default*, not a type: a kind that reaches the menu by an existing entry verb (`terminal` → `terminal.new`, `session-document` → `session.new`) must say so, or its absence from the derived block is indistinguishable from an omission. The row carries `Entry`: a closed record hierarchy, `Derived(menu)` or `Verb(commandId)`, required, no default; the build test asserts a named verb exists in the catalog, is `Global` and lives in `_File`. Two nullable strings were the first form; the Patterns Expert's review replaced them with the hierarchy so `(null, null)` is unrepresentable, the `?? "_View"` defaults it needed are gone, and `Opener` refuses a `Verb` row by type rather than by convention.

## 3. The six per-kind openers are retired; the derived ids are `surface.new.<kind>` / `surface.show.<kind>`

`workbench.newClassDiagram`, `newSequenceDiagram`, `newSearch`, `newCodeViewer`, `newDiagnostics`, `newPromptDraft` were catalog rows each needing a case, a seam and a method in the controller and a hand-written wiring in the shell — a per-kind list beside the kind rows, the second home the ADR's first rejected alternative names. The derived opener carries the kind in its id; one controller case (`PerspectiveMenu.TryParseOpener`) and one shell method (`OpenKind(kind, showExisting)`) serve every row, present and future, which is what the US-C4 mutation test requires. `Instances.One` kinds get "Show" semantics (focus if open, else open) — `diagnostics` was "one" in §A7 yet its retired command always added a second; the derivation made the row's fact the behaviour.

## 4. The four collisions (US-C10 b2)

Three of the four colliding strings were on retired openers (`Ctrl+K, M` class diagram · `Ctrl+K, D` prompt draft and diagnostics pane · `Ctrl+K, F` search); a derived opener carries no gesture — nothing binds one and an unbound chord is shown nowhere (PS-M4) — so those collisions vanish by construction. The fourth (`Ctrl+K, G`: `focusCanvas` vs "New GitHub Copilot session") is resolved by re-lettering `focusCanvas` to `Ctrl+K, Shift+G` in the Shell lane's own file; the harness row lives in `Core/Terminal`, unowned this horizon, and keeps its letter. Rejected: re-lettering the openers to keep a chord per kind — they would be strings nothing shows, kept for a chord handler that is spec non-goal 9.

## 5. The prompt kind's placement stays a marked arm

`OpenKind` places every derived kind by `DocumentPlacementPolicy` except `prompt`, which opens beside a terminal (its transfer target) — a per-kind fact expressed as a branch, the Ruling-22 class. Kept with a `simplify:` marker: the ceiling is one kind; the trigger is a second kind needing bespoke placement, at which point placement becomes a column on the row read by the policy, and `DocumentPlacementPolicy.DocumentKinds` — a pre-existing hand list of two kinds that this slice's derivation now routes fifteen past — retires with it. Finding for SH-2/SH-3 (placement per host, the defaults).

## 6. A document opens where the operator is; only a full-window body hands over to the initial host

INV-0009's seam (`DocumentOpening` → `Set(Workbench)`) was a no-op from the workbench and a return from the Explorer. Renamed as `Set(Coding)` it became a real switch from Architecture — every derived opener in the reading perspective silently switched the shell to Coding (both reviewers' Blocker). The handler is now guarded on the presenter's body: `if (_mode.Mode.Body == PerspectiveBody.FullWindow) _mode.Set(PerspectiveSet.Coding, "document-opening");` — the pre-Addendum-C meaning, stated on the row's `Body` rather than on a perspective name. The routed open by kind (`PerspectiveMenu.Resolve`, US-C3) is the next slice's transaction; until it lands, "View source" from Explore opens in Coding, as it did before.

## 7. The radio item is not checkable; its peer exposes the Toggle state

A checkable `MenuItem` is toggled by WPF on click, before `Click` is raised and one render frame later for a user click — so "restore the check in the handler" speaks an un-checked state to a screen reader and draws it for a frame; and overriding `OnClick` to raise `Click` alone skips `PreviewClick`, the event `MenuBase` closes the menu on, leaving the popup in menu mode (the UX & Accessibility reviewer's rounds 1 and 2). The item is therefore **not checkable** — WPF's own click pipeline runs untouched — and a custom `MenuItemAutomationPeer` returns itself for the Toggle pattern, whose state reads `IsChecked`, which the presenter alone sets. The test shows the menu, opens View, invokes through the peer, and asserts the popup closes, capture is released, the check stays and no `Unchecked` fires; mutations M21–M23 (peer deleted; made checkable; the override restored) each redden it.

## Not decided here

The rail (SH-2 wires three destinations; the Explore button runs `perspective.explore` as a destination until then); the second host and the allow-list refusal at the service (ADR-0031/0032); whether `Resolve`'s consumer (the routed open transaction) announces the failure branch as US-C3 words it (SH-2).
