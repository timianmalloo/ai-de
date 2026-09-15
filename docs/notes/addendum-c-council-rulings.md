---
id: note-addendum-c-council-rulings
title: "Decision note — Rulings 50–105: Addendum C's vocabulary, phasing, ADR-0017, the graph substrate, the 80% case, page one, the operator's composer verdicts, and the 2026-09-13 and 2026-09-14 findings ruled"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, ruling, conductor, addendum-c, perspective, ui, docking, explorer]
links:
  - { to: plan-addendum-c-modes, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: spec-knowledge-explorer-mode, rel: relates-to }
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: refines }
review-by: 2026-12-11
summary: >-
  Rulings 50–55 issued before any Addendum C spec was written; 56–62 issued at the spec's gate —
  56 and 57 file the operator's own composer verdicts, 58–62 rule on the reconciliation table;
  80–90 rule on the operator's 2026-09-13 findings, 91 files the F5 merge decision, and 92–105 rule
  on the 2026-09-14 findings (the Explore reader, the Architecture default, Send-while-running,
  the compiled prompt, the engine catalog, the store's fixture revision, session names, F5's close).
  Six rulings the Owner issued before any Addendum C spec was written, on the evidence the conductor
  brought at node R0 of plan-addendum-c-modes. They fix the vocabulary (Perspective), the phasing
  (F5 untouched; code after F5 merges), ADR-0017's fate (retained and amended), the graph model (one
  substrate, two surfaces), the build order under the operator's 80% case, and four page-one facts a
  spec written without them would get wrong.
review-suggested:
  - { by: adr-0017-primary-view-mode, on: 2026-09-11, reason: "ADR-0017 accepted as amended (Ruling 52): the closed set is the Perspective set; a body may be a docking host; second-host clause discharged by spikes/second-dock-host-unparent" }
---

# Decision note — Rulings 50–79

## Provenance

Issued by the **Owner** (persona `.claude/agents/owner.md`, model `fable`) at node **R0** of
`docs/plans/addendum-c-modes.md`, on 2026-09-11, in answer to six questions the conductor put with
verified evidence. Filed verbatim by the conductor (session `conductor-addendum-c`). **Ruling 49**
(F5's exit run is triggered by the operator's own gesture) is the immediate predecessor; it is filed
as `docs/notes/front-door-ruling-49.md` on `feature/exit-evidence` and lands on `main` with that
branch — until then this note's predecessor is on a branch, and Ruling 51 says why that is correct.

The operator's intent these rulings serve, verbatim, is in the audit entry
`al-01M28QJMWGJT5AK438M37KJ9ZT` (the prompt of 2026-09-11 17:17Z).

Files cited are under `C:\projects\ai-de\` at `main` `bb09e294`, except where a path names the F5
worktree.

---

## Ruling 50 — Vocabulary: the concept is a *Perspective*; "canvas mode" and `ShellViewMode` are not renamed by the spec

**RULING:** Name Addendum C's concept **Perspective** (Coding · Explore · Architecture; Tests
reserved), define it on page one in a three-row vocabulary table beside *canvas mode* and *primary
view mode*, and ban the unqualified word "mode" from Addendum C's normative text.

**BECAUSE:** Three meanings already exist — *canvas mode* is per-session, per-canvas, splittable
(`ai-de-spec-addendum-a-session-experience.html:174-185`, R16 at `:238-244`;
`CanvasModeCatalog.cs:21,46`); *primary view mode* is the shell's body swap
(`0017-primary-view-mode.md:79-82`; `ShellModeController.cs:6-11`); the operator's concept is a
task-scoped view-set plus menu contribution, which is exactly the Eclipse *perspective* — a term with
zero collisions in `src/` (only a 3D-projection comment at `CanvasPage.cs:20`). Renaming
`ShellViewMode` now would touch code F5 is measuring (Ruling 51).

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Freezes the terms *canvas mode* (Addendum A) and *primary view mode* (ADR-0017)
as-is; `ShellViewMode` is renamed to `Perspective` only in the commit that implements Ruling 52,
never before.

**CONDITIONS:** Revisit if the spec finds a fourth meaning it must name; the fix is a fourth table
row, not a synonym.

**RECORD AS:** Ruling 50 — Addendum C's concept is "Perspective"; canvas mode and primary view mode
keep their names until Ruling 52's implementation touches `ShellViewMode`.

---

## Ruling 51 — Phasing: F5 stands untouched; Addendum C is the next delivery, specified now, built from `main` after F5 merges

**RULING:** Write Addendum C's spec now in its own worktree (docs only), and start its implementation
branch from `main` only after `feature/exit-evidence` has merged; F5's exit evidence stands exactly as
Addendum A + Ruling 49 wrote it.

**BECAUSE:** F5's pending clauses measure `File → New Session` under the current menu and shell
(`conductor-front-door.md:50,73` in the F5 worktree; Ruling 49 at `front-door-ruling-49.md:28`), and
Addendum C rewrites the menu (`MainMenuBuilder.cs:77-99`) and the rail (`MainWindow.xaml:65-107`) — a
run taken against a changed shell would measure something Addendum A did not specify. The Ruling 49
note exists only on the feature branch (no match for "Ruling 49" under `C:\projects\ai-de\docs`), so
a ruling file on `main` numbered from 50 has a dangling predecessor until F5 lands.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Defers Addendum C code to after F5 merge; admits Addendum C spec work in parallel.
Addendum C does not amend Addendum A's text; it cites §A6.1 as-is.

**CONDITIONS:** This note cites `docs/notes/front-door-ruling-49.md` by path and notes it lands with
`feature/exit-evidence`. If F5 is abandoned rather than merged, re-rule before Addendum C branches.

**RECORD AS:** Ruling 51 — F5 exit evidence stands untouched; Addendum C is the next delivery, spec
in parallel, code from `main` after F5 merges.

---

## Ruling 52 — ADR-0017: retain and amend, do not supersede; a Perspective *is* a primary view mode, and a perspective body may be a docking host with a surface-kind allow-list

**RULING:** Keep ADR-0017's body-content swap and its retain-never-rebuild invariant; amend it so
(a) the closed mode set becomes the Perspective set, (b) a perspective's body content may be a docking
host of its own (Coding = today's workbench host unchanged; Architecture = a second host), (c) each
perspective declares an allow-list over `SurfaceContentFactory.Kinds` expressed as a column on the
existing descriptor rows, and (d) Explore keeps the full-window `ExplorerSurface` and does not become
docked panes.

**BECAUSE:** The ADR already anticipates this — "new full-window surfaces are new modes, not new
shell mechanisms" (`0017-primary-view-mode.md:89-90`) — and its rejection of option A (dock pane:
graph and reader compete) at `:51-56` still holds for the operator's own UC2 description. The
invariant is preserved by construction: the presenter holds every body content alive and only
unparents (`ShellModeController.cs:19-28,68-74`; ADR `:93-98`), so a terminal in Coding keeps running
while Architecture is open, exactly as it does while Explorer is open today.
`knowledge-explorer-mode.md:73-75` (non-goal 5) forbids restructuring the dock from a mode switch,
which this keeps. Ruling 22 requires kinds to be rows, not switch arms
(`SurfaceContentFactory.cs:58-61`), so the allow-list is a row attribute.

**CONFIDENCE:** Verified for the invariant and the ADR's reasons; **Inferred** that a second
AvalonDock host survives unparenting as the first does — the ADR's T1 no-rebuild test (`:98`) must be
re-run against the second host, and this is an extension of the ADR, not a reading of it.

**SCOPE EFFECT:** Cuts "Explorer becomes docked panes". Admits an ADR-0017 amendment (status
`proposed` → accepted-as-amended; it is implemented on `main` yet still `proposed` at `:5,27`).
Layout persistence gains one envelope slot per perspective (ADR-0013 amendment already named at
ADR-0017 `:99-101`); an old envelope carrying a kind a perspective now disallows migrates by
drop-with-report, never crash.

**CONDITIONS:** Falsified if the no-rebuild test fails for a second live docking host (a hidden
`HwndHost` in host 2 restarts); then Architecture's body becomes a non-docking composite and the
ruling is re-issued.

**RECORD AS:** Ruling 52 — ADR-0017 retained and amended: perspectives are primary view modes; a
perspective body may be an allow-listed docking host; Explorer stays full-window.

---

## Ruling 53 — UC2 vs UC3: one graph substrate, two surfaces; no "graph with two modes"

**RULING:** Explore's surface is the existing `ExplorerSurface` (graph + reader, node-walk);
Architecture's graph is a **second `CanvasSurface` instance** scoped by a node-kind filter (code ·
data · architecture) that composes with the Model surface — the same substrate, the same neighbourhood
query, no second graph store, and no in-surface "knowledge/architecture" toggle.

**BECAUSE:** `knowledge-exploration.md:69` (US-K1) commits to *one graph over all artifacts* with
node kinds enumerated at `:56-58`, and `:62` defines UML/ER views as projections of that graph;
`uml-erm-surfaces.md:55` says the Model surface is "not a re-implementation of the graph explorer —
structural views that compose with it", with drill-to-node-walk at `:78` (US-U8) and its IA at
`:90-92`. `ExplorerSurface.cs:20-21` already establishes a second `CanvasSurface` instance as the
pattern (a canvas is never reparented across visual trees). A toggle inside one surface would be a
fourth "mode" (Ruling 50).

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Freezes the graph model; the kind filter is a parameter on the existing
neighbourhood query. The class-diagram scaling defect the operator named routes to US-U7 / US-K10–K11
(aggregation, LOD) on the substrate, not to a new surface.

**CONDITIONS:** Falsified if the projection's node-kind set cannot express "code/data/architecture
only"; that is a substrate finding to fix first, not a reason to fork the graph.

**RECORD AS:** Ruling 53 — one graph substrate; Explore = `ExplorerSurface`, Architecture =
kind-filtered second canvas composing with the Model surface.

---

## Ruling 54 — The 80% case: the spec defines every perspective's allow-list and menu, but admits into Addendum C's build only Coding, the mechanism, and the Architecture views that already have a surface; the rest are named-and-deferred; UC4 is a named non-goal

**RULING:** Order Addendum C as (1) Coding perspective, (2) the Perspective mechanism — rail,
per-perspective menu, allow-lists, persistence slots, (3) Explore unchanged, (4) Architecture with
only its existing surfaces (`classdiagram`, `sequence`, `contexts`) plus the class-diagram scaling
fix; name entry-points view, data-flow, ER diagram, and bicep-derived layer/component diagrams with
acceptance criteria and **defer** them; name Use Case 4 as a non-goal with no rail item.

**BECAUSE:** The operator's priority is explicit ("80% case … where most of our calories must be
spent"). Kinds that exist today are the descriptor rows at `SurfaceContentFactory.cs:107-127` — there
is no ER, entry-point, data-flow, or solution-tree kind, and no `SolutionTree`/`WorkspaceTree` type in
`src/AiDe.App`. `MainWindow.xaml:108-111` (AR3) forbids a rail item that does nothing, so Tests gets
no icon until it exists. Coding's three models are not new scope: session-primary is Addendum A;
terminal-secondary is `TerminalSurface` + `File → New Terminal Session` (a spec invariant, Ruling 45
at `front-door-rulings-45-48.md:77-80`); "hybrid" is Addendum A's **Terminal** canvas row (`:183`),
which re-registers when observed-lane binding exists (Ruling 45 `:79-80`).

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Cuts four of the six understanding views from Addendum C's build (they remain
specified). Cuts UC4 entirely. Cuts any new graph capability in Explore
(`knowledge-explorer-mode.md:66-67`). Coding's allow-list excludes `classdiagram`, `sequence`,
`contexts` per the operator's "no sequence diagram in the agentic coding use case".

**CONDITIONS:** Revisit the deferral list only when a deferred view has a substrate query it can be
built on; an operator request re-admits one at a time, not the set.

**RECORD AS:** Ruling 54 — Addendum C priority: Coding, mechanism, Explore-unchanged,
Architecture-existing-surfaces; four views named-and-deferred; UC4 non-goal.

---

## Ruling 55 — What the spec gets wrong on page one without these four facts

**RULING:** Addendum C must state on page one that (a) Addendum A §A6.1 defines **five** canvas
modes, Terminal included, and the "hybrid" coding model is that row — not a new thing; (b) the
per-perspective menu is **derived** from the active perspective and the allow-listed `Kinds`, the way
the Terminal menu is derived from `AgentReadinessProfiles`, never a second hand-written list; (c) the
"New session" primary action stays visible in every perspective and switches to Coding; (d) the
Coding default layout drops the "Explore" pane and resolves the Domain/Provenance duplication in its
E7 surface list, with the owed "two kinds render different content" test landing in the same slice.

**BECAUSE:** (a) `ai-de-spec-addendum-a-session-experience.html:174,183` — the conductor's summary
listed four. (b) `MainMenuBuilder.cs:77-99` is a static tuple list mixing shell mode, Coding and
Architecture commands; `:89-96` already shows the derived pattern and says why a second list is
wrong. (c) `MainWindow.xaml:70-85` (AR2/AR5) — one door, above the destinations.
(d) `SurfaceContentFactory.cs:70-106` is an open finding: "Explore … duplicates the Explorer rail
mode", Domain wired to `view`, and the control is owed but deliberately not landed.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Admits (d) into Addendum C's Coding slice — it is the default layout of the
perspective being specified, so it cannot be left to "another session". Freezes Addendum A's text.

**CONDITIONS:** (d) is contingent on INV-0006's zone/tree repair having merged; if not, the Coding
default-layout node blocks on it and says so.

**RECORD AS:** Ruling 55 — page-one facts for Addendum C: five canvas modes, derived menus,
New-session in every perspective, Coding layout resolves the Explore/Domain/Provenance duplication.

---

## Residual the Owner did not rule

Whether the Architecture perspective's second docking host shares one `WorkbenchController` command
set or gets its own — a design question for `/design-slice`, not a spec question. Carried to node A1.

## Re-plan checkpoints discharged (plan-addendum-c-modes, Stage 9)

- **① Phasing:** Addendum C does not touch Phase 1's exit; F5b's oracle is unchanged. Discharged.
- **② ADR-0017:** *retained and amended*, which is neither of the two branches the plan named
  ("supersede" or "retain, modes constrain only the workbench"). D1's brief therefore carries the
  amended shape: perspectives as primary view modes, Coding's body = today's host, Architecture's body
  = a second allow-listed host, Explore full-window and unchanged. Discharged with a third outcome.


---

# Rulings 56–62 — issued at the spec's gate (2026-09-11)

## Provenance

Issued by the **Owner** (`fable`) after `spec-addendum-c-perspectives` cleared its four vetoes at gate
pass 3. **Rulings 56 and 57 are filings, not judgements:** they record the operator's own composer
verdicts — audit entries `al-01M28T8C2WEVN4J0D10XQJMJAZ` (the conductor's transcription) and
`al-01M28W210Q8QGKK38QEQ0WR1EB` (the verbatim text) — as numbered rulings so the spec can cite a
number, and answer the one word the operator's text left open ("etc."). Rulings 58–62 dispose of
§R rows 3, 7, 8, 9 and 17 of the spec. Evidence opened by the Owner: Addendum A `:121,:234`,
Addendum B `:135,:176,:181-188,:209,:216`, CT19 (`communication-and-task-discipline.md:93`),
`GoalBlock.cs:127-160`, `ComposerSurface.cs:25-27,284-293`, `LeaseDerivation.cs:49-50`,
`SurfaceContentFactory.cs:70-123,315-341`, `knowledge-exploration.md:93-96`,
`uml-erm-surfaces.md:90-93`, the Ruling 42 note `:71-91`, and the spec at
`:149-175,:298-332,:425-451,:508-529,:570-578,:704-751,:1366-1381,:1569-1583`.

---

## Ruling 56 — PR-A filed: fan-out cap, budget **and tier** are session settings with defaults; no per-prompt field, no per-prompt override

**RULING:** File the operator's verdict (2) as superseding Addendum A `:234` (R15 b2) for fan-out
cap, budget and tier, and Addendum B `:135` and `:209` accordingly: the three are **session settings
with defaults**, set in the New Session sheet and changed only in session settings, inherited by
every prompt; tier is included — "etc." is read as tier, and that reading is now ruled, not inferred.

**BECAUSE:** The operator's words are verbatim in `al-01M28T8C2WEVN4J0D10XQJMJAZ`: *"budget, cap etc.
are not intrinsic to the prompt, they are intrinsic to the session settings"* — a decision, filed
here. Tier cannot stay per-prompt once the cap is per-session: CT19 defines the cap **as a function
of the tier** (*"0 at T0, 2 at T1, the GO7 width cap at T2"*,
`communication-and-task-discipline.md:93`), so a prompt-authored tier beside a session-authored cap
is two definitions of one quantity (DM: derive-don't-store), and the spec's own state list already
carries the symptom as a warning (*"T0 with fan-out 3"*, `:1374-1377`). The contract is unaffected:
`SpawnContract.Validate` requires all six fields tier-blind (`GoalBlock.cs:131-149`) and the compiled
block still carries the session's tier, so the audit `tier` field (AL5b) maps 1:1 as before. CT19's
unit is the agent's *turn*; Addendum A §A3 (`:121`) makes bare "session" the user-facing container —
the product's session is where a turn-level default is authored. **This is an extension of CT19 to
the product, marked as such, not a reading of it.**

**CONFIDENCE:** Verified (operator text, CT19, `Validate`, the spec's rows); the tier reading is the
Owner's ruling on an open word.

**SCOPE EFFECT:** S-1, S-2, S-4 filed. Cuts every per-prompt tier/cap/budget box and every override
state (the spec's *"No override state: the settings have one home"* `:1376-1377` stands). Goal /
Done-when / Not-in-scope stay content fields, refused inline at T2 (S-1; `GoalBlock.cs:121-125`). A
send with empty derived structure is a Message shape (US-C13 shape rule `:715-721`) — that, not a
lower session tier, is how a trivial prompt in a T2 session stays one action. Regression tests
re-scope to the content fields (S-4).

**CONDITIONS:** If the operator answers the tier question directly, their answer is filed over this
ruling. Revisit if the operator is observed changing the session tier prompt-to-prompt — that is the
signal a per-prompt tier was real, and the remedy is re-ruled then, not pre-built now.

**RECORD AS:** Ruling 56 — PR-A filed: tier, fan-out cap and budget are session settings with
defaults, inherited by every prompt, no per-prompt field or override (A `:234`, B `:135`, `:209`
superseded).

---

## Ruling 57 — PR-B filed: the composer is a conversation; S-3 and S-5 superseded, S-8 admitted as an Owner extension under D-6, S-6/S-9 kept, Ruling 42 intact

**RULING:** File the operator's verdict (3) as superseding Addendum B `:183` (template form
rendering) and `:216` ("as a validated form"): the composer is one primary prompt editor, a
template's structure renders as derived, prefilled, inline-editable structure, and the compiled
prompt is on demand only; **admit S-8** (B `:187`'s "reviewable form" for conductor round-trips
becomes the same inline structure), marked as the Owner's extension and phased under D-6; Ruling 42
is untouched.

**BECAUSE:** Verdict (3) verbatim: *"the whole enter-in-text-boxes-and-see-the-render-below is
awful"*. The as-built composer is exactly that: *"The compiled view is a plain text box showing the
whole prompt"* (`ComposerSurface.cs:25-27`) — which contradicts B `:184`'s own word *toggle*, so S-6
is a restoration, not a supersession. S-8 is admitted because B `:187` defines the round-trip as
arriving *"exactly like an assist result"* — the same form mechanism the operator condemned;
refusing S-8 would keep the condemned mechanism alive on one path. It is an extension (the operator
did not name round-trips), and D-6 (`:328-330`) defers the build until a reply-channel seam exists,
so the operator can reverse it at zero cost. Ruling 42 stands: the lease is derived from `@mentions`
in the compiled text (`LeaseDerivation.cs:49-50`, `@(\S+)`; the Ruling 42 note `:73-74,:89-91`), and
the only composer change touching it is the refusal copy at `ComposerSurface.cs:288-291`, which
never says that an `@path` mention is what derives a scope — US-C13 fixes the copy and offers the
picker; who computes the glob is unchanged.

**CONFIDENCE:** Verified for S-3, S-5, S-6, S-9 and Ruling 42; **Inferred** for S-8 (Owner extension
of the verdict to a case the operator did not mention).

**SCOPE EFFECT:** S-3, S-5 filed; S-8 admitted, build gated on D-6 (until then B `:187` as written).
Kept as written: B `:181` shape control (S-9), B `:184` compiled-on-demand (S-6), US-ED5/ED6/ED7
staging. Cuts: any field box above or below the editor, any permanently rendered compiled block, any
`ComposerFieldDescriptor` widget at rest (US-C13 oracle `:711-714`). The layout defect of verdict
(1) stays an investigation finding (`investigate/composer-input`, INV-0007), not spec.

**CONDITIONS:** The operator may overrule S-8 before D-6 is admitted; then B `:187` stands
permanently. Ruling 42 is falsified — and this ruling re-opened — if any composer change lets a
write scope be typed as a pattern rather than derived from a mention.

**RECORD AS:** Ruling 57 — PR-B filed: the composer is a conversation (B `:183`, `:216` superseded;
S-8 admitted as Owner extension under D-6; B `:181`, `:184` kept; Ruling 42 intact).

---

## Ruling 58 — §R row 3: Explore's top-bar view selector loses its structural entries; UML/ER views are Architecture kinds reached by a routed kind-open

**RULING:** Cut the *view selector (graph / UML class / UML component / ERM)* from Explore's IA as an
in-surface control: the three structural entries become kind-opening actions that route to
Architecture per US-C3, and with only "graph" left the selector is dropped, not repointed;
`spec-knowledge-exploration` Part B IA `:96` gets an erratum row citing this ruling.

**BECAUSE:** Ruling 53 bans an in-surface knowledge/architecture toggle and makes UML/ER views
projections owned by the Model surface; `uml-erm-surfaces.md:90-93` gives those views their own IA —
catalog → view master-detail with a C4-level switch — that *"composes with — does not duplicate"*
the explorer. A selector in Explore that renders UML class/component/ERM is the duplicate that clause
forbids and a fourth "mode" under Ruling 50. US-C3 `:520-527` already defines the routed kind-open in
the order Architecture · Coding, with the reading host winning a shared kind. US-K7
(`knowledge-exploration.md:75`) is unaffected: it requires standard notation *when the operator picks
a structural view*, and that pick now lands in Architecture.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Constrains Explore to graph-only (consistent with `spec-knowledge-explorer-mode`
non-goal 1 and Ruling 54's "no new graph capability in Explore"). The 2D/3D toggle, depth control and
metric overlay at `:95-96` are untouched — they are graph controls, not view kinds. Adds one erratum
row to `spec-knowledge-exploration`; the HTML addenda are not touched (Ruling 51).

**CONDITIONS:** Revisit if Architecture's Model surface cannot open scoped to Explore's current
selection — then the drill loses its context and the fix is a scope parameter on the kind-open,
still not a selector in Explore.

**RECORD AS:** Ruling 58 — Explore's view selector cut; UML class/component/ERM are Architecture
kinds opened by US-C3 routing; erratum on `spec-knowledge-exploration` Part B `:96`.

---

## Ruling 59 — §R row 7: `joins` and `codeviewer` admitted to Architecture; `search` and `diagnostics` stay out; D-0 deferred

**RULING:** Admit `joins` and `codeviewer` to the Architecture allow-list — Ruling 54's principle
was *existing surfaces only* and its three names were an enumeration of what was shown to me, not a
closed set — with `codeviewer` at default none (opened from a node) and `joins` in the default only
while it renders real content; confirm the Simplifier's cut of `search` and `diagnostics`, and
confirm D-0 (solution/tree view) as a fifth named-and-deferred view.

**BECAUSE:** Both kinds exist as descriptor rows (`SurfaceContentFactory.cs:112` `JoinSurface`,
`:122` `CodeViewerView`). `joins` renders code/schema/infra joins with Verified vs Inferred — the
operator's UC3 (6) is *"layer and component diagrams derived from the code and from things like
bicep"* (spec `:76-77`), which is the infra join. `codeviewer` is required by US-C3's routing rule
(`:520-527`): "View source" from an Architecture node must not leave the reading host, so the kind
must be admitted there. `search` is an unwired scaffold (`:435`) and `diagnostics` answers no UC3
item (`:437`) — a menu row for what cannot work is AR3's forbidden empty gesture. D-0: no
`SolutionTree`/`WorkspaceTree` type exists (Ruling 54 BECAUSE), so it is deferred the same way as
D-1…D-4 (`:313-319`).

**CONFIDENCE:** Verified for existence, routing and the cuts; **Inferred** that `joins` renders
against a real workspace (the inventory's claim; the Owner did not open `JoinSurface`).

**SCOPE EFFECT:** Ruling 54's enumeration amended to five existing kinds: `classdiagram`,
`sequence`, `contexts`, `joins`, `codeviewer`. No new surface built. D-0 admitted to the allow-list
only in the slice that builds it (`:320-321`).

**CONDITIONS:** If `joins` renders empty against a real workspace, it stays admitted but leaves the
Architecture default (a default tab that is always empty is the Explore-pane defect Ruling 55d just
removed). `search` re-admits when an index is wired, one kind at a time.

**RECORD AS:** Ruling 59 — Architecture admits `joins` and `codeviewer` (existing kinds);
`search`/`diagnostics` cut; D-0 solution tree named-and-deferred.

---

## Ruling 60 — §R row 8: the Loomkeeper kinds are homed in Coding, `sessions` alone in the default; no fleet perspective

**RULING:** Confirm the placement — `sessions`, `board`, `leaderboard`, `ledger`, `daydreams` are
admitted to Coding, only `sessions` in the Coding default, none in Architecture, and `daydreams`
becomes reachable through the derived menu by construction (US-C4).

**BECAUSE:** The kinds exist (`SurfaceContentFactory.cs:113-117`) and observe the *terminal* side of
UC1 — `Sessions`' own empty state sends the operator to the Terminal menu (`:335-336`), which is
Ruling 54's secondary model. A "Fleet" perspective would be a new rail destination outside the closed
Perspective set (Ruling 50) and outside Ruling 54's build; hiding the kinds would drop working
capability. `daydreams` has no command, no menu row and no default-layout slot — in `src/AiDe.App`
the string appears only in `SurfaceContentFactory.cs` and `WorkbenchShell.cs` (grep, Verified) — so a
menu derived from the allow-list (Ruling 55b) is the smallest thing that makes it reachable without
a hand-written list. Architecture excludes them by intent as a reading host (`:448-451`).

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Freezes the Coding default at Left = Terminal sessions, Bottom = one terminal,
Center = empty state (US-C6 `:572-576`). No Loomkeeper kind may be hidden because it is empty at
startup — each keeps its teaching empty state.

**CONDITIONS:** Re-home the five as a set, in one ruling, if the operator names a fleet/observation
use case (a UC5); until then no kind moves individually.

**RECORD AS:** Ruling 60 — Loomkeeper kinds homed in Coding, `sessions` alone in the default,
`daydreams` reachable via the derived menu; no fleet perspective.

---

## Ruling 61 — §R row 9: Provenance is not a Coding surface; the Evidence master-detail belongs to Architecture

**RULING:** Confirm that `view` (Evidence, master) and `inspector` (Provenance, detail) leave Coding
for Architecture (Left / Right), that the selection channel between them is a `/design-slice`
decision, and that the owed "two kinds render different content" test lands in the slice that moves
them.

**BECAUSE:** `SurfaceContentFactory.cs:83-88` records Provenance as *the DETAIL half of a
master-detail screen* whose selection wire was dropped, leaving two byte-identical copies of the
master (`:74-77`); the operator's UC1 names session, CLI and hybrid — no evidence list — and Ruling
54 already excludes the reading kinds from Coding (`:446-447`). Restoring the pair needs a selection
channel between two panes, which the same comment names as a design decision (`:99-101`); US-C6's
positive oracle (`:577-578`) is the owed control from Ruling 55d. What the operator may want *in
Coding* about evidence — "what did the agent change" — is `codeviewer` and `ledger` (both admitted
to Coding, `:433,:436`), not Provenance.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Cuts "Explore", "Domain" and "Provenance" captions from the Coding host (US-C6
falsifier `:574-575`). Architecture default: Left Evidence, Right Provenance (`:439-440`). The
`inspector` kind must render the selected row's detail, never a second list.

**CONDITIONS:** Re-rule only if an evidence *row* (not source or ledger) is shown to be needed inside
a Coding session; the request re-admits `view` alone, never the pair.

**RECORD AS:** Ruling 61 — Provenance is not a Coding surface; `view`/`inspector` are Architecture's
Evidence master-detail, selection channel to `/design-slice`, US-C6 oracle in the same slice.

---

## Ruling 62 — §R row 17: the caption is "Terminal sessions"; A3 needs no erratum; the kind string `sessions` is not renamed

**RULING:** Confirm the caption **"Terminal sessions"** for the `sessions` kind in the Coding
default; Addendum A §A3 needs no erratum because the caption is A3 executed, not amended; the kind
string `sessions` stays as-is.

**BECAUSE:** A3's rule (`ai-de-spec-addendum-a-session-experience.html:121`) already says bare
"session" means the user-facing container **and** that the Watcher's session vocabulary migrates to
*"lane"/"terminal session"* opportunistically — *"new code complies, touched code migrates"*.
Captioning the pane "Terminal sessions" is that migration on the one screen where both meanings
would otherwise meet. The surface lists Loomkeeper-observed terminals
(`SurfaceContentFactory.cs:315-323`). A kind string is a persisted layout key, not UI copy; A3
governs the words the operator reads.

**CONFIDENCE:** Verified for A3 and the caption; the one-letter-away hazard is cited by the spec to
Ruling 18 and not re-opened here.

**SCOPE EFFECT:** US-C6's falsifier (no bare "Sessions" caption in the Coding host, `:575`) stands.
Finding, not new scope: the same surface's empty-state copy says *"No sessions yet. Open a Claude
Code or GitHub Copilot session…"* (`:335-336`) — bare "session" for a terminal session; under A3's
*touched code migrates* rule, that copy changes to "terminal session" in the slice that touches this
surface, and no earlier.

**CONDITIONS:** Falsified if any Coding-host surface other than the session document is ever
captioned with bare "Session(s)"; the fix is the caption, never the rule.

**RECORD AS:** Ruling 62 — `sessions` kind captioned "Terminal sessions" in Coding; A3 executed, no
erratum; kind string unchanged; empty-state copy migrates when touched.

---

## Filing note

All seven are Verified except where marked: Ruling 56's tier reading is an Owner extension of CT19 to
the product session (**the operator was asked the tier question directly; an answer files over
56**); Ruling 57's S-8 is an Owner extension of verdict (3); Ruling 59's "joins renders" is the
inventory's claim.

---

## Ruling 63 — filed over Ruling 56's tier clause: tier is not a session setting; it is a decoration the compile step attaches to the compiled prompt, and the compile step is the operator's to specify

**RULING:** Tier is removed from session settings. It is attached to the compiled prompt by the
**compile step** — the post-processing of the typed prompt — shown to the operator as a derived
decoration and confirmed at send. Fan-out ceiling and budget stay session settings (Ruling 56).
**How tier is derived is open**: the operator has said the compile step has not been thought through
explicitly and will do so; nothing in Addendum C, its design, or its architecture may fix the
derivation before that.

**BECAUSE:** The operator's own words, logged today (session `conductor-addendum-c`, two entries):
*"shouldn't tier be decided by the compilation of the prompt? A key aspect and benefit of being able
to type a prompt and then post-process it would be to decorate it with things like tier."* — and
*"that makes me realize we haven't really thought through the compile step explicitly — maybe I need
to do that."* Ruling 56's CONDITIONS said an operator answer files over it; this is that answer. It
is also more consistent with the spec's own shape rule (US-C13: a send with empty derived structure
is a Message shape — the compile step already decides the *shape*; the tier is the same kind of
decision) than a session-scoped value was.

**CONFIDENCE:** Verified (the operator's words). **Inferred:** the reconciliation with CT19's
tier→cap function — the session's fan-out value is a *ceiling* and the compiled tier's cap applies
within it — is the conductor's reading and is labelled so in the spec (S-1); the operator confirms
or corrects it when the compile step is specified.

**SCOPE EFFECT:** Ruling 56 stands for fan-out ceiling and budget; its tier clause is superseded.
Spec amended: §A6 session settings, S-1, US-C13's session-values criterion, §B2, Flow 6 node L. D1
designs tier as a derived decoration on the compiled-prompt disclosure with a *not derived yet*
state and no header field; it does not design the derivation. A1 must leave a **seam** for the
compile step (typed prompt → compiled prompt + decorations: shape, tier, lease, template) and must
not fix its internals. The compile step is a **named open item for the operator**, carried in the
plan's re-plan checkpoints.

**CONDITIONS:** Revisit when the compile step is specified; if it makes tier operator-typed after
all, S-1 and US-C13 revert to a per-prompt field (never to a session setting).

**RECORD AS:** Ruling 63 — tier is a compile-step decoration, not a session setting; derivation open,
the operator's to specify; fan-out ceiling and budget remain session settings.


---

# Rulings 64–71 — Addendum D filed, two security findings, and the operator's task-class decision (2026-09-11)

## Provenance

Issued by the **Owner** (`fable`) after `spec-addendum-d-compile-step` cleared its three hard vetoes at
gate pass 2. **64–69 file the spec's own proposals PR-D1…PR-D6.** **66 also disposes of finding F-2**
(the lease derives from the render) as a defect to fix now on `main`. **70 files the operator's
decision** that task class is per prompt (audit `al-01M296K4DAJ7H8NP26WC7B135Y`, committed with this
note — the Owner ruled on the relay and conditioned on the commit). **71 rules on F-3** (governed
lanes are not toolless). The Owner numbered F-2's disposal inside 66 so the register has no gap; 72
is not used. Files relied on: the spec; this note; `front-door-rulings-41-42.md`,
`front-door-rulings-43-44.md`; `docs/adr/0028-mode-cohort-not-partition.md`;
`docs/lessons/defect-classes.md`; `ComposerSendGate.cs`; `LeaseDerivation.cs`;
`ComposerCompiler.cs`; `AcpLaneClient.cs`; `GovernedRunHost.cs`; `.claude/settings.json`; the
adapter `spikes/acp-subscription-lane/node_modules/@agentclientprotocol/claude-agent-acp/dist/acp-agent.js`.

---

## Ruling 64 — PR-D1 filed: the session fan-out value is a ceiling; effective fan-out = min(cap(tier), ceiling); tier is overridable in Prepare and that is not "operator-typed"

**RULING:** File PR-D1 (`addendum-d-compile-step.md:827`): the session's fan-out value is a ceiling,
the compiled tier's cap applies within it (`cap(T0)=0, cap(T1)=2, cap(T2)=4`,
`effective = min(cap(tier), Current(ceilings).fan_out)`, a projection computed at Submit, `:309`,
`:262`), tier is the §A9 projection (`:295-311`) with an `operator` override on the compile line as
the only stored tier (`:305` R4), and this override does not trip Ruling 63's revert clause.

**BECAUSE:** Ruling 63 labelled the ceiling/cap reconciliation Inferred pending the compile spec; the
spec now states it as a rule with the cap function sourced to CT19 and GO7 (`:309`). Ruling 63's
condition reverts to a per-prompt *field* only if tier becomes *typed*; a Prepare override is a
control over a derived value that already exists, appears only after compile, has no box and no
required state (`:805` R-10) — that is Ruling 63's "confirmed at send", not Ruling 56's "per-prompt
field". Ruling 56 stands untouched for ceiling and budget: the spec keeps them non-overridable in
Prepare (`:309`, `:804` R-9).

**CONFIDENCE:** Verified (spec `:262, :295-311, :805, :827`; Rulings 56, 63 read).

**SCOPE EFFECT:** Admits the override control and the fourteen enumerated tier inputs (`:307`) as the
test. Cuts, as the spec does (`:297`): fan-out syntax in the text and template-declared tier. Freezes
tier ∈ {T0, T1, T2}; effective never exceeds the ceiling; the ceiling is never raised from Prepare.

**CONDITIONS:** (1) The override stores `source: operator` and the rule's value, so `override rate`
(§A14.3) is measurable — Ruling 56's revisit trigger transfers here as that metric. (2) The
`ceilings` snapshot names the draft's held value as its writer until the session settings exist
(`:123`, F-6); never a compile-time default.

**RECORD AS:** Ruling 64 — PR-D1 filed: session fan-out is a ceiling; effective = min(cap(tier),
ceiling), cap 0/2/4; tier overridable in Prepare, not typed (Ruling 63 confirmed, Ruling 56 intact
for ceiling and budget).

---

## Ruling 65 — PR-D2 filed: the compiler is the session's bound (engine, model, account); Addendum B `:152` superseded for the compile step

**RULING:** File PR-D2 (`:828`): the compile call runs on the session's bound `(engine, model,
account)`; Addendum B `:152`'s separately configured assist provider and its `assist:` node are
superseded for the compile step and not built; B `:162-163`'s one-shot mechanics and discipline are
kept as the `compile.*` events (`:796-797`).

**BECAUSE:** The spec's privacy ground is decisive on its own: the history window is a compile input
(`:460`) and must never cross a provider boundary (`:491` C6) — a second provider would receive the
conversation. The operator's ratifying words (*"the same model that the session is bound to"*) are
cited at `:796`; the ruling does not depend on them. Ruling 57 is intact — B `:184`'s compiled view
is kept (`:799`).

**CONFIDENCE:** Verified for the spec text and C6; Inferred for `ProviderConfiguration.cs:65-66`
(not opened by the Owner).

**SCOPE EFFECT:** Cuts the `assist:` node and any assist-provider settings surface. The `refused`
row (`:323`) stands: the compile passes `SpawnContract.Authorize` exactly as a lane does, so a compile
can never bill an API key while a subscription is configured.

**CONDITIONS:** The conductor commits the session audit entries so `:796`'s citation resolves (F-1);
if the committed words differ, re-file.

**RECORD AS:** Ruling 65 — PR-D2 filed: the compiler is the session's bound (engine, model,
account); Addendum B `:152` superseded for the compile, `assist:` not built, B `:162-163` kept.

---

## Ruling 66 — PR-D3 filed, and F-2 fixed now on `main`: lease derivation runs over the editor's source text only

**RULING:** File PR-D3 (`:829`) **and fix F-2 on `main` now, ahead of any Addendum D slice**:
red-first tests that an attachment body, a template body and a rendered goal-block line carrying
`@src/` derive no pattern while the same mention in the editor text does; then
`ComposerSendGate.cs:164` and the display caller `ComposerSurface.cs:449` both pass the editor's
held source text — the same symbol at both sites — and `LeaseDerivation` is unchanged.

**BECAUSE:** Today `Derive(compiled.Text)` runs over the rendered view (`ComposerSendGate.cs:146-147,
:164`), and `RenderAttachment` inlines `attachment.Text` into that view
(`ComposerCompiler.cs:149-150`); `Mention` is `@(\S+)` over the whole string
(`LeaseDerivation.cs:49-50, :69`). So a file the operator attached, or a template someone else
authored, mints a write scope. **This is a reading of Ruling 42, not an extension:** R19/42's purpose
is *"a lease is never operator-typed"* and *"the paths they referenced with a mention are the paths
they mean"* (`front-door-rulings-41-42.md:93-95`; Ruling 57) — "they" is the operator, and text the
operator did not author is not their reference. Ruling 42 simply never enumerated the render as a
non-source. It is a defect, not a floor trip: the Security veto cleared with this as the ordered fix
(`:478`, `:813`); the displayed lease is computed from the same text as the sent one
(`ComposerSurface.cs:449`) so the operator sees the widened scope before Send; and Phase 1 seams are
validated, not enforced (`:309`, Ruling 26c). It is fixed now because it is two argument changes and
one test, it is C17's own class (`LeaseDerivation.cs:43-47` records the prior widening instance), and
every day it waits is a day the F5 evidence run's lease comes from a render.

**CONFIDENCE:** Verified (both call sites, `RenderAttachment`, the regex, Ruling 42 and its erratum).
Inferred: that no existing test asserts either behaviour — the spec's grep (`:478`).

**SCOPE EFFECT:** Admits one T0 fix on `main`: the tests, the two argument changes, a
`defect-classes.md` entry for the class *"a derivation reads the render instead of the source"*.
Freezes: `LeaseDerivation` API unchanged; the lease stays a projection, never a stored decoration
(`:245`, `:263`); §A13.3's census gate (the `Derive(` argument is the `source_text` symbol, `:469`)
ships with D's C4, not as a blocker to this fix. **F-2 is disposed here; no separate ruling.**

**CONDITIONS:** (1) Red-first: the attachment-body and template-body tests fail on `main` before the
change (record the red run; `:478` marks it Inferred until then). (2) Display and Send derive from
the same symbol — a test asserts `Patterns(x)` at the surface equals `request.Lease.Exclusive` for a
draft with an attachment mention. (3) If the F5 exit run's prompt carries any attachment or template,
it re-runs after this fix; if it carries neither (a one-line write), it is unaffected.

**RECORD AS:** Ruling 66 — PR-D3 filed and F-2 fixed now on main: lease derivation over the editor's
source text only; `ComposerSendGate.cs:164` and `ComposerSurface.cs:449` change argument, red-first
(a reading of Ruling 42, not an extension).

---

## Ruling 67 — PR-D4 filed: mechanical pre-compile on debounce, agentic compile on the Send gesture; two gestures under an agentic rung

**RULING:** File PR-D4 (`:830`, §A10.1 `:315`): the mechanical projections settle live on the
debounced draft in memory and persist nothing; an envelope is opened and the agentic compile runs
only on the Send gesture; under `agentic-advisory` or `agentic` a prompt is two gestures, the second
the confirmation; Addendum C US-C13's *"when the draft settles (debounced), then … derived
structure"* (`addendum-c-perspectives.md:727`) is read as the mechanical path, with the agentic delta
surfaced (`:801` R-6).

**BECAUSE:** A compile per keystroke burns the plan window and multiplies the injection surface
(`:506` D row); one in flight per draft, cancel-on-edit and `inputs_sha` reuse are only coherent on a
gesture. US-C13's one-action property is preserved under `mechanical-only` (`:315`), and the
two-gesture shape is the operator's own *prepare-then-submit* word (`:315`); the *same key* for both
gestures is the author's Inferred choice (`:839`), reversible at zero cost.

**CONFIDENCE:** Verified (spec `:315`, `:506`, C `:725-729`); Inferred for the operator's "prepare"
words (relayed, F-1).

**SCOPE EFFECT:** Cuts a separate *Prepare* command (`:315`). Admits the `Prepare again` control and
the `stale` / `preparing` states. Freezes the 1 s mechanical bound as a defect signal, not a degraded
state.

**CONDITIONS:** (1) Under `mechanical-only`, US-C13's inline derived structure still appears on
debounce — the debounce experience is not lost, only the model call moves. (2) The `same key`
inference is recorded as Inferred in the note and the first operator observation settles it.

**RECORD AS:** Ruling 67 — PR-D4 filed: mechanical pre-compile on debounce; agentic compile and
envelope open on the Send gesture; two gestures under an agentic rung (US-C13 `:727` read as the
mechanical path).

---

## Ruling 68 — PR-D5 filed: the compile-mode ladder, gated by the pin spike, then 50 scored + 50 holdout envelopes

**RULING:** File PR-D5 (`:831`): `compile_mode` defaults to `mechanical-only`; `agentic-advisory` is
selectable only after the P-D5 pin spike's artifact exists with a recorded adapter sha equal to the
installed adapter's (`:486` C1); `agentic` only after the first 50 real envelopes are scored and the
next 50 holdout envelopes meet §A14.4's fixed floors (`:518`), D-D1 an order not a conjunction; the
agentic tier recommendation replaces the rule only under §A14.5 (D-D2).

**BECAUSE:** The pin is verified in adapter source (`acp-agent.js:5883-5884`, `:6008`) and unobserved
on the wire (`:836`); a model-backed capability with no eval is the AI Systems Engineer's hard veto
(`:511`). The floors are fixed before the sample and judged on a holdout (`:518`), which is the only
shape in which "passes" means anything.

**CONFIDENCE:** Verified (spec `:315, :486, :509-518, :836`; adapter `:5883-5884, :6007-6008`).
Inferred for §A14.5's content (cited, not opened).

**SCOPE EFFECT:** Freezes the order: spike → advisory → 50 → holdout 50 → agentic. Freezes
`tool_calls = 0` and `applied_denied = 0` as invariants over every `called` row, unfiltered by
`EffectiveMode` (`:518`). A failed spike is a hard stop for every agentic rung, never a fallback.

**CONDITIONS:** (1) The adapter version is pinned at 0.75.1; a bump re-runs the spike. (2) N = 50 is
Inferred (`:518`) and may be revised only upward. (3) The spike's fixture repository includes
`.mcp.json` with a stdio server so `mcp__*` frames are in the assertion (`:486`). **F-3 is not
disposed here** — it concerns lanes, not the compile session — see Ruling 71.

**RECORD AS:** Ruling 68 — PR-D5 filed: compile mode defaults mechanical-only; advisory after the
observed pin spike; agentic after 50 scored + 50 holdout meet the fixed §A14.4 floors (D-D1 an
order; D-D2 under §A14.5).

---

## Ruling 69 — PR-D6 filed: v1's only agentic decoration is the structure; framing is mechanical; skill relevance and mention suggestions wait for their own evals

**RULING:** File PR-D6 (`:832`): the only `derived` decorations in v1 are `goal`, `done_when`,
`not_in_scope`, requested only for the open lines and skipped when all three are filled (`:260`,
`:289`); family framing is the versioned profile applied as a template (`:242`, `:266`); skill
relevance, mention suggestions, tier recommendation and any model rewrite of the framing are not
built until each is admitted to the allow-list by its own eval (D-D2/D-D3, `:270`).

**BECAUSE:** LOA P1/P2 and DM15 (`:806` R-11): a deterministic template covers ceremony and format
with no hallucination surface, and an abstraction with no reader is a scope cut. The deny-list
(`:289`) makes the allow-list the whole of what a model can touch, so each later admission is one
name plus one eval, not a redesign.

**CONFIDENCE:** Verified (spec `:242, :257-270, :289, :806, :832`).

**SCOPE EFFECT:** Cuts from v1: `tier_recommendation`, `family_framing` (as a model output),
`skill_ref`, `mention_suggestion`. Freezes the allow-list as case-sensitive and the deny-list as
"any unknown name" (`:289`).

**CONDITIONS:** Adding any name to the allow-list requires (i) its eval in §A14 form, (ii) a
`contract_version` bump so `inputs_sha` never reuses pre-admission decorations (`:293`).

**RECORD AS:** Ruling 69 — PR-D6 filed: v1's only agentic decoration is the structure (open lines
only); framing is mechanical; skill relevance, mention suggestions and framing rewrites wait for
their own evals (D-D3).

---

## Ruling 70 — the operator's decision filed: task class is per prompt, a decoration of the compiled envelope; the session carries a default, not a constant

**RULING:** File the operator's decision: task class is chosen **per prompt** — a `mechanical`
decoration on the compiled envelope with `source ∈ {session-default, operator}`, never `derived` —
and the session carries a **default** the operator may set, not a per-session constant; the New
Session sheet's field becomes *default task class* and is **optional at create**, and Send is refused
for a prompt with no effective class (*"choose a task class for this prompt"*).

**BECAUSE:** The operator's words (`al-01M296K4DAJ7H8NP26WC7B135Y`, committed with this note): *"a
session is a conversation … task class seems like it should be something defined for every chat"*.
The code already scores per run, not per session: `GovernedRunHost.cs:172` passes `request.TaskClass`
per episode, and the partition key is `ScoreSegment(Workspace, TaskClass, SchemaVersion)` per scored
episode (`Leaderboard.cs:25`; ADR-0028). What is per-session today is only where the value is
*chosen* (`NewSessionSheetViewModel.cs:85, :166, :253-262`; `MainWindow.xaml.cs:290`;
`ComposerSendGate.cs:30, :162`). DC-110's standing rule — *"the work determines the task class;
never the door"* (`defect-classes.md:4276-4277`) — is *better* served per prompt: the prompt is the
work. Optional-at-create with a refusal at Send keeps Ruling 19's *no default* intent (no
system-chosen class can ever rank) while honouring the operator's objection to deciding before the
conversation exists — **that split is the Owner's extension, marked as such.**

**Supersedes (quoted):** spec `:245` *"Task class: … a setting, not a decoration: chosen on the New
Session sheet, required, no default"*; `:249` delta (3) *"task class moves from decorations to
settings"*; `:264` *"Task class … settings — `task_class` snapshotted on `opened`"*; `:804` R-9
*"task class stays a setting"*; Addendum A's R13/R14 sheet requirement (the sheet's task-class field
becomes an optional default); the exit-evidence clause *"a task class no earlier run has used"*
(`docs/plans/conductor-front-door.md`, §F5 / the Proof Pack's cohort check) now reads **per run**.

**Constrains:** the envelope gains `task_class` as a mechanical decoration with provenance;
`ComposerSendContext.TaskClass` becomes the nullable session default; `GovernedRunRequest.TaskClass`
stays required and non-null (`GovernedRunRequest.cs:19, :37`), filled from the envelope; the cohort
key is read per episode (already true — no Watcher change); the deny-list keeps `task_class` (`:289`)
so the model never sets it; `session.json`'s `TaskClass` is the default, labelled so (`:463`).

**Does not change:** DC-110 — a default is an operator-chosen setting, a per-prompt value is the
operator's; neither is a door default, and the provenance is recorded; ADR-0028 (mode is cohort,
never partition); the Work Episode as scoring unit; `ScoreSegment`.

**CONFIDENCE:** Verified for the code shape and DC-110/ADR-0028; **Inferred** for the operator's
words at the time of ruling (relayed; committed with this note) and for the two clauses the Owner
could not open.

**SCOPE EFFECT:** Admits the per-prompt decoration and the compile-line control; changes the sheet
field to optional default; cuts nothing from the Watcher. **F5 remains valid evidence:** the run
carries one prompt whose class the operator chose for that prompt (the sheet is merely where); under
this ruling its provenance is `session-default`, and `GovernedRunRequest.TaskClass` is already per
request.

**CONDITIONS:** (1) The audit entry is committed with this note so the citation resolves. (2) The F5
Proof Pack states the class and where it was chosen. (3) A per-prompt change never rewrites the
session default silently (DM: derive, don't store) — "use as default" is a separate explicit act.
(4) The conductor files the exact path:line of the exit-evidence clause and Addendum A's R13/R14 in
this note when the F5 Proof Pack lands on `main`.

**RECORD AS:** Ruling 70 — task class is per prompt: a mechanical envelope decoration with provenance
(session-default | operator), the sheet's field an optional default, Send refused without one;
supersedes spec `:245, :249(3), :264, :804`; DC-110 and ADR-0028 unchanged; F5 evidence stands.

---

## Ruling 71 — F-3: governed lanes are not toolless; F5 proceeds only with the lane's shell pinned off and the pin observed; the launch pin is the agent-plane's, the spike is Addendum D's; the allow-list is a finding for the operator

**RULING:** (a) The F5 exit run may proceed on `feature/exit-evidence` @ `757af057` **only after**
`AcpLaneClient.NewSessionAsync` sends `_meta.claudeCode.options.disallowedTools: ["Bash"]` for the
governed lane, a unit test asserts that member on the outgoing `session/new` JSON, the run is
attended, and the run's Proof Pack records the outgoing frame, every observed tool-call name, and
`origin/main`'s sha before and after; (b) the standing control is **both**: the P-D5 wire spike
(Addendum D's C1, precondition for any agentic rung) and a typed `session/new` tools argument on the
one `NewSessionAsync` site (the agent-plane's; delivered on the F5 tree, reused by D's C1 with
`tools: []`); (c) `.claude/settings.json:4` is filed as a **finding for the operator**, not a defect
the Owner rules on.

**BECAUSE:** `session/new` today carries only `cwd` and `mcpServers: []` (`AcpLaneClient.cs:164-167`;
`GovernedRunHost.cs:140`); with no `_meta` the adapter gives the SDK the `claude_code` preset
(`acp-agent.js:5883-5884`, `:6008`), resolves the permission mode from
`settingSources: ["user","project","local"]` (`:5856`, `:5962`), and this repository's project
settings allow `Bash(git push:*)` (`settings.json:4`). The lease bounds file writes the seams observe;
it does not bound tools. The operator's user-level and `local` settings are **not recorded** — the
Owner cannot assume they add nothing, which is why the pin is required rather than an attended run
alone. This is not yet an escalation: no irreversible action has occurred and the plan is approved;
**if a lane pushes, or a `Bash` frame appears despite the pin, that is an irreversible action outside
the plan and goes to the human.** `disallowedTools` is honoured in source (`:6007`) and unobserved on
the wire — the Proof Pack's observed tool-call names are the evidence that closes it for F5; the
spike closes it in general.

**CONFIDENCE:** Verified (`AcpLaneClient.cs`, `GovernedRunHost.cs`, `settings.json`, adapter `:5856,
:5883-5884, :5962, :6007-6008`, branch ref). Not recorded: user/local settings; the *"canUseTool is
not guaranteed"* comment the spec cites at `:5883-5884` — the Owner did not see it at those lines.

**SCOPE EFFECT:** Admits one small agent-plane change (a record `{tools?, disallowedTools?}` on
`NewSessionAsync`, two callers) — not a launch-profile abstraction. Freezes: the Phase 1 governed lane
has no shell until a later ruling admits it with its own spike. Cuts nothing from F5's goal.

**CONDITIONS:** (1) The wire test is red-first. (2) A run whose Proof Pack lacks the frame or the
tool-call names is *not recorded*, not passed. (3) For the operator: `Bash(git push:*)` in the
committed project settings is now reachable by any governed lane in a worktree of this repo; keep or
drop is theirs; the lane control makes the lane independent of it either way. (4) Ruling 66's
condition (3) applies to the same run.

**RECORD AS:** Ruling 71 — F-3: F5 proceeds only with `disallowedTools: ["Bash"]` on the lane's
`session/new`, tested and observed in the Proof Pack; standing control = P-D5 spike (D) + typed tools
argument on `AcpLaneClient.NewSessionAsync` (agent-plane); `settings.json:4` filed as an operator
finding.

---

## Ruling 72 — the operator's decisions filed: budget defaults to the subscription's bound with an optional cap; task class defaults to free-form and changes per prompt; the `git push` auto-allow stays

**RULING:** (a) The session's **budget** is an *optional cap*: its absent state means **bounded by the
subscription**, is the default, and is never a number the operator must type; the operator may
*enforce a cap* deliberately, in the sheet or in session settings. (b) The session's **default task
class is `free-form`** — an explicit, operator-visible value present from the moment a session
opens; any prompt may change its own class (Ruling 70); **Ruling 70's Send refusal for a missing
class is superseded** — no class can be missing. (c) The operator keeps `Bash(git push:*)` in
`.claude/settings.json`; Ruling 71's lane pin is therefore the only control on a governed lane's
shell, and the finding is closed as *decided*, not *fixed*.

**BECAUSE:** The operator's words, verbatim (`al-01M297VC0HTFJP761D9BVE9Z72`): *"budgets should be
max (i.e. limited by my subscription) by default and then optionally I can enforce … I don't know
what I intend to spend often … it is unreasonable for long running work to set a budget proactively
unless in cost capping. When I have a subscription with a cap then I don't need to cap. Also I don't
need to choose a task class … the basic should be free form upon open and then I can change it"* —
and *"1: yes auto-allow"*. These are decisions, filed by the conductor without convening the Owner:
there was nothing to weigh. Consistency: the compiled block still carries six fields and
`SpawnContract.Validate` stays tier-blind and blank-refusing — the budget projection carries the cap
when set and a declared *subscription-bounded* value otherwise (the architecture decides the
representation; derive, don't store); spend is **measured** per turn regardless (IO cost axes, ADR-0029
— recorded, never asserted). A `free-form` class is a legitimate cohort key: it is the operator's
stated default, not a value the door invented (DC-110), and `ScoreSegment(Workspace, TaskClass, …)`
partitions it like any other (ADR-0028 unchanged). Ruling 19's *no system default* intent is met —
the default is the operator's, declared here.

**CONFIDENCE:** Verified (the operator's words; Rulings 56, 70, 71; `SpawnContract.Validate`).
**Inferred:** the exact representation of "subscription-bounded" in the compiled block — A1's to
decide with a falsifying test.

**SCOPE EFFECT:** Supersedes: Ruling 70's *"Send is refused for a prompt with no effective class"*
and its *"optional at create"* framing (the sheet shows `free-form` selected, changeable); Addendum
C's sheet criterion (US-C5: no prefilled numeric budget; the sheet creates on defaults with **zero
required inputs**); Addendum D's budget snapshot (the `ceilings` decoration's `budget` is optional)
and its task-class rows (default `free-form`); D1's `new-session-sheet.html` (budget as a state, not
a number; task class preselected `free-form`) and D2's brief (no refusal state). Ruling 56 stands:
fan-out ceiling and budget are session settings — the budget one now optional. Ruling 71 stands
unchanged.

**CONDITIONS:** (1) If the subscription's own limit is not readable by the product, the budget's
absent state reads *"bounded by your subscription — not measured here"*, never a plausible number.
(2) A per-prompt class change never rewrites the session default silently (Ruling 70 condition 3).
(3) Revisit (a) only if a cost-capping need arrives from the operator — the affordance exists for it.

**RECORD AS:** Ruling 72 — budget is an optional cap defaulting to the subscription's bound; task
class defaults to `free-form` and changes per prompt, no Send refusal; the `git push` auto-allow
stays and Ruling 71's pin is the control.

---

## Ruling 73 — the operator's decisions filed: a turn that writes nothing needs no lease; persistence is the tool's purview; a security control gates only the shape it protects

**RULING:** (a) **A turn that writes nothing needs no lease.** A send whose compiled shape is a
*Message* — or a Goal-block whose source text names no write scope — runs as a **read-only turn**:
the lane is opened with every write-capable tool disallowed through Ruling 71's typed
`session/new` argument (at least `Write`, `Edit`, `MultiEdit`, `NotebookEdit`, `Bash` — the
architecture names the exact set from the SDK's tool list), **no lease is derived and none is
required**, and no worktree needs cutting for it. The lease gate (Ruling 42, C17) applies **only**
to a turn whose compiled shape is a write — a Goal-block with a derived write scope — and there it
stands unchanged: derived from the operator's `@mentions` over the editor's source text (Ruling 66),
never typed as a pattern, never universal. (b) **Session persistence is the tool's purview.** The
conversation, its envelopes and the session's state persist automatically under the tool's own
store (`<workspace>/.aide/sessions/<id>/`, ADR-0034) — the operator never names a file, a scope or a
location for the session to exist or to be resumed. (c) **A security control gates only the shape it
protects.** C17's rule — *a lane with no exclusive write pattern cannot be seam-monitored* — is a rule
about lanes that can write; applying it to a turn that cannot write refused straightforward
conversation for no protection gained. The class is registered with this ruling.

**BECAUSE:** The operator's words, verbatim (`al-01M29D65TJN0ZSKC4AZDZ9YVDC`): *"why wouldn't this be
a standard REPL loop between the prompt side and the console … why do I need to define a file …
this is our security part of the constitution being too restrictive in straightforward scenarios. A
turn that writes nothing needs no lease. But session persistence is important — that should be the
tool's purview not the operator's purview."* The enforceable reading of "writes nothing" is *cannot
write*: the tool does not infer from prose that a turn will not write; it **removes the capability**
and derives no lease, so C17's invariant — no lease ⇔ no write capability — holds by construction
(the pin exists: `LaneSessionOptions`, Ruling 71, delivered on `feature/exit-evidence` at
`246b38a3`). A turn that turns out to need writes fails visibly inside the read-only lane, and the
operator adds a mention (or, later, accepts a suggested one — Ruling 69's deferred decoration); the
compile step's shape rule (US-C13; Addendum D §A9) already distinguishes Message from Goal-block,
and this ruling attaches the lease gate to the *write* shape rather than to every send.
Persistence: ADR-0034's envelope store and Addendum A's session store are the tool's; nothing in
either asks the operator for a path, and nothing may.

**CONFIDENCE:** Verified (the operator's words; Rulings 42, 66, 69, 71; `LeaseDerivation.cs:25`'s
*"no lease means no run"* premise, which this ruling narrows to write turns). **Inferred:** whether
a read-only turn runs in the workspace itself or in a throwaway worktree — the architecture decides
(P1 slice), with the constraint that it cuts nothing the operator must clean up.

**SCOPE EFFECT:** Supersedes: `LeaseDerivation.cs:25` *"no lease means no run"* (→ *no lease means
no write capability*); Addendum D's tier rule row R1 (*"the send is refused separately for the
missing write scope"* → the turn runs read-only); Addendum C US-C13's send refusal for a missing
scope (→ applies to a write-shaped turn only; the read-only turn is the default conversation);
Ruling 42's *"nothing derivable means no lease, and no lease means no run"* clause (the derivation
and the refusal of typed/universal leases stand). Admits: a `read-only` turn shape in the compiled
envelope (a projection, like the others), the write-tools pin as the second use of Ruling 71's
argument (the first is `Bash` off for every governed lane; the compile session's `tools: []` is the
third), and one decoration line state (*"read-only — nothing will be written"*). Constrains D2's
design (the lease segment reads *none — read-only* rather than *not derivable*) and P1's slices
(the read-only turn is Coding's first slice, not its last — it is the 80% case's REPL).

**CONDITIONS:** (1) The read-only lane's tool set is asserted red-first on the outgoing `session/new`
frame and observed in the run's Proof Pack (Ruling 71's shape). (2) If a read-only turn is ever
observed writing (a `Write`/`Edit` frame, a dirty tree after the turn), that is an irreversible act
outside the plan and stops at the human; the pin is re-spiked. (3) The class in (c) is registered
with a control the Security lens runs on every finding: *name the shape the control protects; a
shape without the risk is exempt by construction* — a finding that fails to name its shape is
returned, not applied.

**RECORD AS:** Ruling 73 — a turn that writes nothing needs no lease: Message/no-scope turns run
read-only with write tools disallowed and no lease; the lease gate applies to write-shaped turns
only; persistence is the tool's, never operator-declared; a security control gates only the shape
it protects (class registered).

---

# Rulings 74–78 and the D2/A1 errata batch (2026-09-11)

## Provenance

Issued by the **Owner** (`fable`) on the findings the design node D2 (`ui-review-session-conversation`)
and the architecture node A1 (`note-addendum-cd-architecture-p1-inputs` §6) handed the conductor,
in one batch so the specs absorb both in one errata pass. Evidence the Owner opened: Rulings 70–73,
45, 47; Addendum C §C1/§B2/§C4/US-C12/US-C13 and its errata; Addendum D §A7–§A15, §A22, Parts B/C;
`GoalBlock.cs:127-160`; the adapter 0.75.1 at `acp-agent.js:5881-5884` (and a zero-hit grep for the
quoted comment); ADR-0036 `:96-115`, `:228-237`; `DESIGN.md:1096-1184`, `:937`; D2's review and both
decision notes; Addendum A `:104`, `:159`, `:185`, `:238-242`; Addendum B `:181-188`;
`WorkEpisode.cs:20`. Not opened by the Owner: the audit-log verbatims (relayed), ADR-0031/0033/0035
bodies, adapter `:5274-5279`/`:5856`, the run host's concurrency, the mockup's P-13 trace. The errata
E1–E7 are applied by the conductor's errata node, which cites this note.

---

## Ruling 74 — the session document is a `StreamingThread`; the Console is the reply side of each turn; the split is on demand (D2 items 1, 2)

**RULING:** Amend Addendum C §C1 so the *session document* carries `Layout:StreamingThread` (the
shell stays `HubAndSpoke`/`MultiPanelWorkstation`); amend §B2's earlier-turns paragraph and Addendum
A §A2/§A6/R16's default so the turns live in the thread with the lane's reply folded beneath the turn
that caused it, and the Console split is an on-demand view of the same stream opened at a turn;
Addendum D Part C stands as written. Item 2 is a clause of this ruling, not an erratum.

**BECAUSE:** The two specs contradict each other today — C §C1 `:1288-1291` *"Not adopted …
`Layout:StreamingThread`"* vs D Part C `:782` *"unchanged from Addendum C §C1
(`Layout:StreamingThread` …)"* — so one must move, and the operator's verdicts (as quoted in Ruling
70 and `note-session-design-thread-not-panes` §Why) decide which. Item 2 changes a P0 default
(Addendum A `:104` *"composer pane left, output canvas right"*, R16 `:238-242`) and re-reads Ruling
21's *"the canvas split is IN"* as on-demand; a default-layout change to a P0 requirement is a
ruling, not a wording fix. Ruling 45 (Console-only strip) and Ruling 21's mechanism are untouched —
the split still exists and still has its oracle.

**CONFIDENCE:** Verified for the spec contradiction and the Addendum A default; Inferred for the
operator's verbatims (relayed via the note; consistent with the quote in Ruling 70).

**SCOPE EFFECT:** Admits the thread control (D2 ranked item 1) as the slice's first node. Cuts the
Console pane at rest. Freezes: the split is derived from the turns' events, never a second store
(SC1). Addendum D Part C: no change.

**CONDITIONS:** (1) Ruling 21's red-first oracle is re-pointed, not dropped: the split's rows equal
the folded events of every turn, in order (D2 item 7). (2) The split stays in the F6 cycle and
reachable from the header. (3) Addendum B `:186`'s Score outline is recorded as *superseded by the
jump list*, not silently dropped.

**RECORD AS:** Ruling 74 — the session document adopts `Layout:StreamingThread`; the Console is the
reply side folded per turn; the split is on demand (Ruling 21 honoured as on-demand); C §C1/§B2 and
Addendum A §A2/§A6/R16 amended; D Part C stands.

---

## Ruling 75 — the refusal sentence: a blank Not in scope on a goal block is refused tier-blind; a blank Goal or Done when makes a Message (D2 item 5)

**RULING:** The true sentence is *"This prompt is a goal block and needs Not in scope."* — the only
content-gap refusal that exists. *"compiles at T2 and needs …"* is superseded everywhere it appears
(C §C4 `:1381` *"A T2 session needs Done when."*, US-C13 `:741-747`, `DESIGN.md:937`, D1's
`conversation-composer.html` `refused` state, D2's `gaps` state). A blank Goal or Done when is not
refused: the turn is a Message (D §A12.2 `:406`), shown live on the decoration line and spoken at
Send. Under `agentic-advisory`: an unkept Goal or Done when → Message; an unkept Not in scope with
both others confirmed → refused with *keep* offered; the copy *"they send empty"* is true only for
lines whose blankness yields a Message.

**BECAUSE:** `SpawnContract.Validate` refuses a blank `not_in_scope` for any block with no tier input
(`GoalBlock.cs:131-134`), and D's shape projection makes a block exist only when Goal and Done when
are both non-blank (`:406`; §A9 P; enumerated input (3) *goal filled, done_when blank → R0*). So
"needs Done when" can never fire and "at T2" is never the reason. US-C13's *"empty Goal, Done-when
or Not-in-scope … refused on every gap at once"* is the older reading and is amended by
substitution.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Cuts three refusal strings to one. Amends Flow D-1 `:716` (*T2 content gap* → *goal
block with blank Not in scope*), US-C13's falsifier (*a Goal-block send with a blank Not in scope
succeeds*), and D1's/D2's mockup states (a `gaps` state has exactly one gap). Ruling 73 unchanged: a
goal block with no mention still runs read-only and still needs its three lines.

**CONDITIONS:** (1) The send-gate test asserts both halves: blank Not in scope on a block → refused;
blank Done when → Message, not refused. (2) The shape demotion is announced (SC9's *"…as a message,
tier T0"*) — a silent demotion is the falsifier.

**RECORD AS:** Ruling 75 — the content-gap refusal is *"This prompt is a goal block and needs Not in
scope."*, tier-blind; a blank Goal or Done when yields a Message, never a refusal; *"compiles at T2
and needs…"* superseded in C §C4, US-C13, DESIGN.md and both mockups.

---

## Ruling 76 — one degraded-rate floor; the latency floor is struck as a floor and reported as a measurement (A1 item 8)

**RULING:** The degraded-rate floor `(timed_out + malformed + unavailable) / every called row ≤ X`
stands as the one floor on this quantity, with **X = 5 %** as its first value; §A14.4's
`latency_p95 ≤ 60,000 ms` is struck as a floor and becomes a reported measurement — p50/p95 over
succeeded rows, timed-out rows counted as right-censored beside `n_measured / n_total` — with no
latency floor until 50 measured compiles, then a floor set from the measurement and revised only
stricter.

**BECAUSE:** With `bound_ms = 60000` (§A10.2 `:324`), a p95 ≤ 60 s over every row is a ≤ 5 %
timeout-rate floor in disguise (ADR-0036 `:233-235`), and over succeeded rows only it is vacuous —
no completed call exceeds the bound. Two floors on one quantity is two definitions of one measure
(DM: derive, don't store twice). Taking the stricter of the two readings (5 %, not 10 %) is what
"floors only stricter" (§A14.4; ADR-0036 `:108`) permits; loosening timeouts to 10 % would thin a
floor.

**CONFIDENCE:** Verified for the overlap; Inferred for the number (a first value under IO7, like
N = 50).

**SCOPE EFFECT:** Amends §A14.4 and ADR-0036's floor table; §A16's 15 s p95 stays a target. Nothing
else moves.

**CONDITIONS:** X lives in the host-compiled floor table only (ADR-0036 `:104-106`), never on
`opened.constants`.

**RECORD AS:** Ruling 76 — one degraded-rate floor, X = 5 % first value (the stricter reading);
`latency_p95 ≤ 60 s` struck as a floor, reported censored until measured; §A14.4 and ADR-0036
amended.

---

## Ruling 77 — gestures while something is in flight: no Send-now during `preparing`; one governed run at a time per session (A1 item 7; D2's send-while-running gap)

**RULING:** (a) There is no *Send now* during `preparing`: the gesture stays ignored with its reason
(§A10.1 `:315`; US-D5 `:551`); the operator waits or presses **Cancel**, which yields
`prepared — compiled mechanically — cancelled` and one more gesture sends the mechanical envelope;
Cancel's description reads *"Cancel — send without the model's lines"*. (b) A Send while a turn
runs or waits is refused with a reason naming the turn (*"b1 is running; the next turn waits for
it."*): **one governed run at a time per session** in Phase 1 — an Owner extension, marked,
reversible.

**BECAUSE:** (a) Send is the *confirmation* of a prepared turn (§A11 `:369`), and a prepared turn
does not exist until the call resolves; the mechanical envelope being complete does not make it the
turn the operator will confirm. US-D5's falsifier already names *"a submit of a half-prepared
envelope"*. A third affordance beside Cancel and wait is speculative generality. (b) No spec
sentence exists (D2 §9; the note's Inferred item); a queue is state nobody asked for; the refusal is
the smallest correct rule and costs nothing to reverse.

**CONFIDENCE:** Verified for (a); Inferred for (b) — the run host's concurrency was not read.

**SCOPE EFFECT:** Cuts *Send now*. Admits three refused-gesture states (preparing · running ·
waiting) as rows of the STA test (§B5). Freezes queued sends out of Phase 1.

**CONDITIONS:** (1) Each refused gesture is announced as a status with its reason (SC6), never
silent. (2) If Addendum A's run model later admits parallel runs per session, (b) is re-read as
*"queued after b1"* with a `queued` outcome and nothing else moves.

**RECORD AS:** Ruling 77 — no Send-now during `preparing` (wait or Cancel, then Send); one governed
run at a time per session, a Send while a turn runs or waits refused with the turn named (Owner
extension, reversible).

---

## Ruling 78 — where spend renders, and what an enforced cap does (A1 item 9; D2 item 4's cap)

**RULING:** Spend renders at two grains, both folds over the store and never stored: **per turn** on
the reply side's outcome line (tokens in / cached / out · requests · duration, from the run's
`RunEventCost` and, for the compile, `called.cost` in *what was read*); **per session** in the
session header's budget state (*"12,400 tokens this session · bounded by your subscription"* /
*"38,900 of 40,000 · cap enforced"*). Not on the decoration line — decorations are inputs to a
compile; spend is an outcome. An **enforced cap** (Ruling 72) never refuses: a new turn does not
start once measured session spend has reached the cap without asking in-thread (SC7's `capask`); a
running turn is not stopped by the cap in Phase 1 and its outcome line reports the overrun; the copy
never predicts a turn's spend — *"would pass it"* becomes *"has reached the cap you set; allow this
turn, raise the cap, or stop"*.

**BECAUSE:** Ruling 72 makes spend *measured regardless* and the budget optional, so the header is
where the operator's question "how much this session" is answered against the cap state (IO2 cost
axes); the per-turn number already sits in D2's outcome-line counts (`DESIGN.md:1110`). "Would pass"
is a prediction with no emitting source (IO: never a plausible number); "has reached" is a
measurement. Mid-run stopping needs a pause on the run host the spec has not built — Ruling 26c's
*validated, not enforced* parity holds inside a turn.

**CONFIDENCE:** Verified for the spec and design facts; the cap semantics are an Owner extension of
Ruling 72, marked.

**SCOPE EFFECT:** Admits the header's budget state and the per-turn counts as the two spend
surfaces; cuts a spend segment from the decoration line; freezes mid-run cap stops out of Phase 1.
Amends the sheet's help copy (`new-session-sheet.html:261`, `DESIGN.md:1153`, `:1151`).

**CONDITIONS:** (1) Absent `usage` on the wire renders *not recorded*, never 0 (US-D10). (2) The
cap's help copy states the guarantee truthfully: *"a new turn will not start past it without asking;
a running turn finishes."*

**RECORD AS:** Ruling 78 — spend renders per turn on the outcome line and per session in the
header's budget state, both derived; an enforced cap asks in-thread before a turn starts once
measured spend has reached it, never refuses, never stops a running turn in Phase 1, never predicts.

---

## Errata batch (D2/A1) — the Owner's wording, applied by the conductor's errata node

**E1 · Template control (D2 item 3).** Addendum B `:181`: *"**Template control** in the composer
header: `template: none | <id>@<version>` (searchable picker, grouped by intent, recents first). Per
block; switching preserves content (template → none yields the compiled text for editing; none →
template goes through apply-template)."* B `:186`: *"the jump list (Addendum C §B2) lists each
turn's ordinal · words · outcome; the decoration line carries `<shape>` (message | goal block) and
`template <id> v<n>`."* C §B2 `:971`: *"**Header: template control** | `template: none |
<id>@<version>` — Addendum B `:181` as amended; the session-settings affordance beside it |
`none`"*; `:977`: *"Send (Ctrl+Enter), provenance — no shape badge; the decoration line's `<shape>`
and `template` segments carry it"*. US-C13 and §B2's glossary: *shape* is reserved for Message |
Goal-block (Addendum A R15 b2); *template* is `none | id@version`; *class* is the task class.
**BECAUSE:** `Free-form` (template) · `free-form` (class) · *shape* twice — `DESIGN.md:1178`, the
decoration-line note §Why. **CONFIDENCE:** Verified.

**E2 · The tier control's home (D2 item 4a).** D §B2 `:682`: the compile line carries the *call's*
provenance (model · profile · what was read · cost · Prepare again · *stale*); the tier and its
control sit on the **decoration line** beside the tier's rationale. §B4 wireframe: the `T1
[T0|T1|T2]` row moves to the decoration line. §B5 `:773`: *"The tier control is on the decoration
line, never in the settings line or a field (falsifier: a control in C's 'no override' region; a
tier field)."* `:774` tab order: *editor → structure lines → decoration line (class · tier · lease)
→ compile line (what was read · Prepare again) → settings links → compiled disclosure → Send, no
trap.* US-D6 `:555`: *"exactly one tier control, parented to the decoration line."* **BECAUSE:**
Rulings 63/64 hold unchanged (compiled, overridable in Prepare, never typed); SC2's one-grammar rule
puts a past and a current turn's tier in the same place (`DESIGN.md:1176`). **CONFIDENCE:** Verified
for the rules; the tab-order sequence is Inferred from SC2/SC4 — confirmed against the mockup's
P-13 trace before filing.

**E3 · `task_class` in `inputs_sha` (D2 item 4b).** §A8.4 `:293`: name `task_class` explicitly in
the domain — it is already there as one of *"the mechanical facts"* the prompt carries (§A8.3
`:276`). Add to Flow D-2 `:728`: *a per-prompt class change after Prepare stales the envelope like a
settings change (an `operator` `task_class` row is appended; no new envelope; the next gesture
re-prepares, and under an agentic rung that is one request).* **CONFIDENCE:** Verified.

**E4 · `cancelled` and `reused` (D2 item 4c).** `cancelled` is already row 7 of §A10.2's nine — D2
miscounted; no new outcome. Amend Flow D-1 `:699` so the edge *operator edits text → draft* carries
the `cancelled` string on the abandoned envelope's compile line (§A11 `:345` already allows the
draft state to show *"the last envelope's state"*); D2's fuller string stands. `reused` is **not** a
`called.outcome` (no call was made): add *"… reused, no new request"* to §A11's `prepared(reason)`
list and to §B5's table-driven test as a **tenth string beside the nine outcome strings**; ADR-0036
`:270` already excludes `reused` rows from percentiles. **CONFIDENCE:** Verified.

**E5 · `mode: mechanical-only` unrendered (D2 item 4d).** §A10.2 row 1 `:321`: the string becomes a
provenance fact (`opened.compile_mode`, shown in the provenance disclosure and session settings),
not a compile-line string; under `mechanical-only` with no template the compile line is absent (the
envelope opens and submits on one gesture, §A10.1 `:315`). The STA test keeps nine rows; row 1's
expectation is *compile line absent, provenance shows `mode: mechanical-only`* — the row is
re-pointed, not removed. **CONFIDENCE:** Verified.

**E6 · Outcome vocabulary for *stopped* and *refused before start* (D2 item 4e).** §A12.1 `:397`:
`consumed` carries `outcome` (the episode's `EpisodeOutcome` — `Completed | Abandoned | Superseded |
Blocked`, `WorkEpisode.cs:20`, unchanged) **and** `reason` (a stable code: `completed` ·
`lane_exited{code}` · `stopped_by_operator`); a stopped turn is `Abandoned` + `stopped_by_operator`;
the outcome word on the reply side is a render of the pair. A **refusal before start** is not a
turn: `submitted.accepted: false, refusal: <code>` on the same envelope, no `consumed`, the envelope
stays open and a later `submitted` may be accepted (US-D6 `:556` and US-D7 `:564` already permit
exactly one *accepted* `submitted`). *waiting for you* is a live state, never an outcome. Exact code
names are A1's (ADR-0033). **CONFIDENCE:** Verified for the enum and the event shape; the code names
are Inferred.

**E7 · A1's supersessions (item 6).**
- D §A13.4 C1 `:486` and US-D8 b2 `:570`: drop `_meta.disableBuiltInTools: true` — adapter 0.75.1
  evaluates it only when `tools` is absent (`acp-agent.js:5881-5884`, *"a legacy shorthand for
  tools: []"*); the pin is `tools: []` + `disallowedTools` + `mcpServers: []` + the reject handler
  (ADR-0035's `settings` deny belt).
- D page-one fact 5 and §A13.4 `:482`: drop the quoted comment *"canUseTool is not guaranteed to
  run…"* — zero hits in adapter 0.75.1 (`package.json:6`); cite the mechanism lines A1 names
  (`:5274-5279`, `:5856`) instead — those lines the Owner did not open.
- D §A22 row 5 `:662` *"the lane path is unchanged"* → superseded by Ruling 71 (one typed tools
  argument on `NewSessionAsync`; the governed lane carries `disallowedTools: ["Bash"]`, the
  read-only turn the write set — Ruling 73, the compile session `tools: []`).
- D US-D1 b2 `:529` and the `projection_sha` domain (`:142`, `:413`, `:562`): `opened.task_class` →
  `Current(task_class).value ‖ source`; the falsifier is *a `task_class` row with `source:
  derived`* (the deny-list keeps `task_class` for the model; Ruling 70 gives it `session-default |
  operator`).
- C US-C12 `:695-700`: the switch event gains `outcome` and `error_code` (IO failure-rate axis;
  stable codes per the observability standard).
- C P-7 `:884`, `:680`, `:828`, `:1610`: re-targeted from the terminal HWND to the **WebView2 pages**
  (the composer editor, the Explorer's pages) — ADR-0031 draws the terminal in WPF. **CONFIDENCE:**
  Verified for the adapter facts and the spec lines; ADR-0031's rationale Inferred (not opened).

**RECORD AS (one line for the batch):** Errata batch D2/A1 after Rulings 74–78 — template control
(B `:181`/`:186`, C §B2, US-C13); tier control on the decoration line (D §B2/§B4/§B5, US-D6);
`task_class` named in `inputs_sha` and a class change stales like a setting; `cancelled` stays the
seventh outcome, `reused` a tenth non-outcome string; `mode: mechanical-only` is provenance, not a
compile-line string; `consumed.reason` and `submitted.accepted:false` for stopped /
refused-before-start; `disableBuiltInTools` and the phantom adapter quote dropped; §A22 row 5, US-D1
b2 / `projection_sha`, US-C12 `outcome`/`error_code`, P-7 → WebView2.

**Not ruled, for the record:** IA-17 (a template under an agentic rung) — not put; the smallest
reading is D §A8.1's *"the call is skipped when all three lines are already non-blank"*, so a fully
filled template never calls the model and a partially filled one names only its open lines — no new
rule needed.

---

## Ruling 79 — the operator's decision filed: Ruling 51 relaxed — Addendum C/D code branches from `main` now; F5's tree is frozen for its run; the lane pin is carried onto `main` ahead of the F5 merge

**RULING:** (a) The implementation of Addenda C and D starts from `main` **now**; Ruling 51's
*"code from `main` only after `feature/exit-evidence` merges"* is superseded. (b) `feature/exit-evidence`
is **frozen at `135e05e1`** until the operator's attended exit run: it does not merge `main` again
before the run, its Release build (`1.0.0+135e05e1…`) is the binary the run uses, and its exit
evidence cites that sha — the shell it measures is the shell it was built for, which is what Ruling
51 protected. After the run, F5b merges `main` into the F5 tree (never rebases — DC-128), discharges
the clauses, and merges F5 into `main`. (c) The two lane-pin commits (`246b38a3`, `135e05e1` —
`LaneSessionOptions`, the governed `Bash` pin, the `session/new` frame recorded on the normal path;
code, tests, their notes and Proof Pack; **not** the frozen oracle, **not** `ConductorEntry.cs`) are
**cherry-picked onto `main`** so that CV-0's read-only pin, CV-3's `tools: []` and PD-5 reuse the
one typed argument; the coordination plan's S0 becomes this cherry-pick, and the F5 merge moves to
the converge step.

**BECAUSE:** The operator's words (`al-` entry of 2026-09-11, session `conductor-addendum-c`):
*"keep going — relax ruling 51 … let's get all of this stuff implemented so I can do deeper user
testing with a more complete build."* Ruling 51's reason was that a run taken against a changed
shell would measure something Addendum A did not specify; freezing the F5 tree at a named sha
preserves that guarantee without holding every downstream track behind a gesture the operator has
deferred. The cherry-pick is safe by inspection: the two commits touch `AcpLaneClient.cs`,
`GovernedRunHost.cs`, `WorkbenchDiagnostics.cs`, their tests, docs and derived views — the oracle
`tools/verify-front-door-exit-evidence.py` is not in either (checked with `git show --name-only`).
Their tests ran green on the F5 tree (Core AgentPlane 182/182, App Conductor 12/12) and run again on
`main` at the pick.

**CONFIDENCE:** Verified (the operator's words; the commits' file lists; the tests on the F5 tree).

**SCOPE EFFECT:** Supersedes Ruling 51's ordering clause; keeps its measurement guarantee by the
freeze. The coordination plan (`docs/coordination/addendum-cd.md`) reads: S0 = cherry-pick the lane
pin; S1, S2, W1… unchanged; the F5 merge joins the converge step. F5b's evidence gains one line: the
tree's sha and the fact that it was frozen while `main` moved.

**CONDITIONS:** (1) The cherry-picked tests are green on `main` before anything branches from it.
(2) If the F5 run is taken and its evidence cites any sha but `135e05e1`, the freeze was broken and
the run is *not recorded*. (3) The F5 tree's `git merge main` happens only after the run's
`exit-evidence.json` exists.

**RECORD AS:** Ruling 79 — Ruling 51 relaxed: Addendum C/D code from `main` now; F5's tree frozen at
`135e05e1` for its run; the lane-pin commits cherry-picked onto `main` as S0; the F5 merge moves to
converge.

---

# Rulings 80–87 — the operator's first manual test after CV-2 (2026-09-13)

## Provenance

Issued by the **Owner** (`fable`) on the operator's five screenshot titles (`C:\Users\malla\Downloads\ui findings 9-13-am\`, described by the conductor — **not opened by the Owner**) and two of the conductor's own findings, at `main` `6d3e281a`. The operator's words are the decision; each ruling says how it lands. Sequencing: **87** (T0, first) → **84** (frees Coding's Left) → **83** → **81** → **82** → **80**; **85/86** any time. Not verified by the Owner: which surface hosted the `rev rev-1` status in the operator's layout; the runtime code page the ACP reader used.

The operator's five findings, verbatim (the screenshot titles): *"need to fix text input area... should be a larger window size so that the default isnt scrolling"* · *"console output is too fine grained"* · *"the output should show the conversation and reasoning - just like in CLI - seems like it shows results and tool calls instead"* · *"new sessions should default into the left dock"* · *"the ledger-leaderboard-sessions-board views should be tied to a different left bar icon - coordination"*.

---

## Ruling 80 — the editor's rest height is derived from the thread's need: it fills the body at 0 turns and rests at 280 px once the thread has turns; `ComposerShare` is retired

**RULING:** Operator: *"should be a larger window size so that the default isn't scrolling."* The composer's share of the session document is **computed, not a constant**. With **0 turns** the thread is its two-line empty caption (`DESIGN.md:1144`) and the editor takes the rest of the body — no scrollbar before typing. With **≥ 1 turn** the editor's **rest height is 280 px**; it scrolls only past that; the thread takes the remainder; the 130 px floor remains the floor under a small window.

**BECAUSE:** Today `SessionDocumentSurface.ComposerShare = 0.45` (`:45`) sets `Composer.BeltHeight` (`:884`) regardless of what the thread holds, so an empty thread keeps ~55 % and the editor sits at `EditorFloor = 130` (`ComposerSurface.cs:231`). `DESIGN.md:1087` (*"the editor's top edge equal at 1, 5 and 40 turns"*) says nothing about 0 turns; `:1097` (*"≥ 130px, ≤ 280px then scrolls"*) named 280 as a ceiling reached only by growth — it becomes the rest size. Finding, not scope: `composer.html:35-36` says `min-height: 110px` while the host says 130 — two definitions of one floor (DM-A).

**CONFIDENCE:** Verified (the constants, the belt, the DESIGN rows); the screenshots as described.

**SCOPE EFFECT:** Amends `DESIGN.md:747` (*"≥ 45 % of the zone height"*) and `:1097`; retires `ComposerShare`; keeps CV-1's belt mechanism (DS-1 Q14) with a derived value. **Lane: Conversation** (`SessionDocumentSurface.cs`, `ComposerSurface.cs`, `composer.html`); the two `DESIGN.md` rows by seam request to the Shell lane.

**CONDITIONS:** Red-first at the startup size: 0 turns → no vertical scrollbar, editor height = body − chrome − caption; 1 and 40 turns → equal top edge, editor 280. One floor constant, read by both the host and the page.

**RECORD AS:** Ruling 80 — the editor fills the body at 0 turns and rests at 280 px with turns; `ComposerShare` retired, `DESIGN.md:747/:1097` amended; Conversation lane.

---

## Ruling 81 — the Console's row grain is the message, never the wire chunk; Ruling 74 condition 1 re-pointed to one `Coalesce` both views read

**RULING:** Operator: *"console output is too fine grained."* One pure function, `Coalesce(turn.Events)`, folds consecutive `agent.msg` chunks (and `agent.thought`, Ruling 82) into **one row per message**, breaking at any other kind; a coalesced row carries the first chunk's timestamp, the lane, the joined text and *n chunks*. `tool.call`, `tool.result`, `permission.request` and `acp.*` rows stay one per event. **Ruling 74 condition 1 is amended:** *"the split's rows equal the folded events of every turn, in order"* → *"the split's rows equal `Coalesce(turn.Events)` of every turn, in order, and the thread's reply side renders the same `Coalesce` output."*

**BECAUSE:** `ConsoleSurface.Derive` emits one `Line` per `EventLine` (`ConsoleSurface.cs:125-140`) while `RunChannelSessionThread.Append` already concatenates `agent.msg` into `Reply` (`:132-135`): the two views of one stream use two grains today. `ConsoleStreamModel.TextOf` states the rule — *one derivation, two readers* (`:254-258`, DM7). The raw chunks stay in the run channel: the mapper's *"nothing is dropped, ever"* (`AcpRunEventMapper.cs:22-25`) is untouched; *Open the log* reaches them.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Ruling 74's identity oracle (M1) re-pointed, not dropped; SC1 stands (*"a view of the same stream … never a second store"*). **Lane: Conversation** (`ConsoleSurface.cs`, `RunChannelSessionThread.cs`, `SessionDocumentSurface.cs`).

**CONDITIONS:** (1) M1 red-first: `Rows == heading + Coalesce(events)`. (2) A message interrupted by a tool call is two rows — the boundary is the interleaving. (3) The chunk count is rendered, never asserted.

**RECORD AS:** Ruling 81 — Console rows are messages, not chunks; Ruling 74 condition 1 re-pointed to `Coalesce(turn.Events)`, one function read by thread and split; Conversation lane.

---

## Ruling 82 — "the conversation" is the reply side's items in event order: prose, reasoning, tool call+result, outcome; `agent_thought_chunk` becomes a mapper row; Ruling 74's one-fold rule holds and today's thread violates it

**RULING:** Operator: *"the output should show the conversation and reasoning — just like in CLI — seems like it shows results and tool calls instead."* In ACP vocabulary the conversation per turn is, **interleaved in event order**: (1) assistant prose — `agent_message_chunk` → `agent.msg`, coalesced (Ruling 81), rendered as markdown, no link activation (`DESIGN.md:1116` stands); (2) reasoning — `agent_thought_chunk`, **today unrecognised** (falls to `acp.session.update.agent_thought_chunk` with an empty body, `AcpRunEventMapper.cs:35-40, :97`) → a new row `agent.thought`, coalesced, rendered dim and collapsible, never announced; (3) each `tool.call` with its `tool.result` as one inline item (title · status · detail on demand); (4) the outcome line. `acp.*` frames (`usage_update`, `available_commands_update`, `acp.result`) are **not** conversation — they are Console evidence; usage feeds Spend (Ruling 78).

**BECAUSE:** The thread renders `Reply` as one plain-text blob plus *N events* collapsed (`RunChannelSessionThread.cs:276-295`; `DESIGN.md:1116-1117`) — two projections of the same events, which is exactly *"results and tool calls instead"*. Ruling 74's rule — one fold, rendered in the thread and unfolded in the Console — **holds**; the thread simply does not render the fold. The corpus has **no** thought chunk (`spikes/acp-subscription-lane/frames/PROVENANCE.md:101-104`), so the `agent.thought` row's shape is Inferred from the schema until a frame is captured.

**CONFIDENCE:** Verified for the mapper and the thread; Inferred for the thought frame's shape.

**SCOPE EFFECT:** Amends `DESIGN.md:1116-1117` (the reply side is the conversation; only non-conversation kinds fold). Adds one mapper row — *"adding a kind is adding a row"* — with the 88-frame round-trip kept green. **Lane: Conversation** (mapper row as its AgentPlane seam).

**CONDITIONS:** (1) Capture a real `agent_thought_chunk` frame before the row lands (Spike Protocol). (2) Reasoning text is never spoken (SC9). (3) Ruling 87 lands first.

**RECORD AS:** Ruling 82 — the conversation = prose · reasoning · tool call+result · outcome in event order; `agent.thought` mapper row; thread renders the fold inline, `acp.*` stays Console evidence; Conversation lane.

---

## Ruling 83 — a new session lands docked in Coding's Left zone; Ruling 47's maximize-on-create is superseded by the operator's own gesture

**RULING:** Operator: *"new sessions should default into the left dock."* In Coding a **newly created** session document opens in the **Left** zone, **docked, not maximized**; reopen is unchanged. **Ruling 47's *"ratify maximize-on-create"* is superseded** — the operator's later gesture (moving the maximized document, refused) is the decision. `workbench.maximizePane` (Ctrl+K, Z) remains the on-demand whole-tree.

**BECAUSE:** Ruling 47 filed the operator's earlier *"full window view"*; their newer words and act overrule it. `NewSessionPlacement.GiveItTheWholeTree` (`:57-58`) collapses every sibling zone, and the refusal they hit — `RefusedReconcileAnnouncement` (`WorkbenchShell.cs:1836-1846`) — fires precisely because *"a collapsed tool zone that still holds panes is not rendered, so it is absent from the view the reconcile reads"*: maximize-on-create made every subsequent drag refuse. Ruling 60's frozen default (*"Left = Terminal sessions, Bottom = one terminal, Center = empty state"*) is re-cut with Ruling 84: **Left = session documents (empty until one opens) · Center = empty state · Bottom = one terminal**; US-C6's caption falsifiers stand.

**CONFIDENCE:** Verified (placement, the refusal path, `ZoneLayout.CodingDefault :193-206`); the screenshots as described.

**SCOPE EFFECT:** Ruling 47 condition 1's oracle re-pointed: create → Left, `Maximized == null`; reopen unchanged. The `session-document` zone rule and `CodingDefault` change; `NewSessionPlacement` retires. Finding, not scope: the reconcile's blindness under a maximized stack stays a Shell-lane defect class. **Lane: Shell** (`ZoneLayout.cs`, `SurfaceContentFactory.cs`, `MainWindow.xaml.cs:287-294`; `NewSessionPlacement.cs` by seam with Conversation).

**CONDITIONS:** (1) Coding's Left `DefaultExtent` fits the thread's 96ch measure (`DESIGN.md:1066`) at the startup size — measured. (2) Center's empty copy never reads *"No session open"* while one is open at Left.

**RECORD AS:** Ruling 83 — a new session docks in Coding's Left zone, not maximized; Ruling 47 superseded, its oracle re-pointed; Coding default re-cut with Ruling 84; Shell lane.

---

## Ruling 84 — Coordination is a fourth Perspective (a docking host); the five Loomkeeper kinds move to it as a set; Coding stops admitting them

**RULING:** Operator: *"the ledger-leaderboard-sessions-board views should be tied to a different left bar icon — coordination."* **Coordination is a fourth Perspective**, not a full-window surface: row `("coordination", "Coordination", 4, DockHost, "perspective.coordination")`, Ctrl+4, a rail icon D1 names, host C via `DockHost.Create`, slot file `<layout>.coordination.zones.json`, a derived menu by construction. Allow-list: `sessions`, `board`, `leaderboard`, `ledger`, `daydreams` — **as a set**; **Coding admits none of them.** Default (Inferred; D1 may re-arrange with the operator): Left = Terminal sessions · Center = Ledger · Leaderboard · Message board · Right empty · Bottom collapsed · Daydreams via menu.

**BECAUSE:** Ruling 60's CONDITIONS said exactly this: *"Re-home the five as a set, in one ruling, if the operator names a fleet/observation use case (a UC5)."* The five are dock panes (`SurfaceContentFactory.cs:223-248`); a full-window composite would re-implement the zone layer for one body (ADR-0031's rejected alternative). ADR-0032 rule 1: *"A new host perspective is a new file, never a schema field"*; rule 2's drop-with-report on the pre-C Coding envelope is what removes the operator's centre tabs — reported, naming Coordination. AR3 is met: the item has content. Daydreams is the Owner's extension of the operator's four (same watcher; Ruling 60 binds the set).

**CONFIDENCE:** Verified; the default arrangement Inferred.

**SCOPE EFFECT:** Amends Ruling 50's closed set and Addendum C page one, §A7 (a fifth column), US-C1 (*"exactly three"* → four), §B4, §A7 *"Excluded by intent"*; ADR-0030 (+1 row), ADR-0031 (a third composition), ADR-0032 (a third file). Ruling 60 superseded for placement; Ruling 62's caption unchanged; Tests still reserved. **Lane: Shell — new slice SH-4.**

**CONDITIONS:** US-C2 identity over four bodies; P-4's `private_bytes_delta` measured for host C; ADR-0032 test 1 extended with the five kinds dropped from Coding.

**RECORD AS:** Ruling 84 — Coordination is a fourth perspective (host C, Ctrl+4, own slot); the five Loomkeeper kinds re-homed as a set, Coding stops admitting them; Ruling 50/60, C page one/§A7/US-C1/§B4, ADR-0030/31/32 amended; Shell lane SH-4.

---

## Ruling 85 — `rev rev-1`: the product attaches a fixture revision; the status prints a recorded revision or nothing

**RULING:** The artifact revision the shell attaches is the workspace's **observed HEAD** (`GitFacts`, `WorkbenchShell.cs:2662-2664`) or *not recorded*; `"rev-1"` leaves the product path (`AttachWorkspace(… artifactRevision = "rev-1")`, `WorkbenchShell.cs:596`; `MainWindow.xaml.cs:183-185` passes none). The pane status prints `rev <value>` only for a recorded value, never a placeholder.

**BECAUSE:** `EvidencePaneViewModel.cs:172-173` formats `rev {SourceRevision}`, and `SourceRevision` is the attached revision — so the label doubles a value that already carries the prefix, and the value is a fixture default naming a revision of nothing (DC-110's shape: a door default read as a measurement). Not recorded: which surface hosted this status in a Coding layout that admits no `view` — the slice names the writer.

**CONFIDENCE:** Verified for the format and the default; the host surface not recorded.

**SCOPE EFFECT:** T0. **Lane: Shell** (`WorkbenchShell.cs`, `MainWindow.xaml.cs`; `EvidencePaneViewModel.cs` added to the Shell lane's §2 rows by the conductor).

**CONDITIONS:** Red-first: attaching with no revision renders *rev not recorded*; tests pass a revision explicitly.

**RECORD AS:** Ruling 85 — `rev rev-1` is a fixture default in the product path; attach the observed HEAD or *not recorded*; Shell lane, T0.

---

## Ruling 86 — a refusal status dwells until superseded or for a bounded time; it is not the record

**RULING:** A status announcement (the refusal form included) is **cleared** by the next announcement, by the next applied layout operation, or after a bounded dwell the design sets (Inferred first value: 10 s; a refusal may dwell longer than a confirmation). The live region already spoke it (SC 4.1.3 at speak-time); the record is `WorkbenchDiagnostics.LayoutMutation`, not the strip.

**BECAUSE:** `IWorkbenchAnnouncer.Clear`'s own remark: *"A status message has no natural end — it sits there until something else happens"* (`WorkbenchAnnouncer.cs:37-38`); its only caller is `workbench.clearStatus` (`WorkbenchController.cs:107-120`). Nothing else happened after the operator's refused drag, so the sentence outlived every screenshot.

**CONFIDENCE:** Verified for the mechanism; the screenshots as described.

**SCOPE EFFECT:** T0; no new control, no setting. **Lane: Shell** (`WorkbenchAnnouncer.cs`, the status strip).

**CONDITIONS:** The dwell never truncates the spoken announcement; `Status cleared.` behaviour unchanged.

**RECORD AS:** Ruling 86 — status announcements clear on supersession or after a bounded dwell; the log is the record; Shell lane, T0.
---

## Ruling 87 — mojibake on the reply: the engine process's streams are UTF-8; fix first, red-first

**RULING:** `AcpEngineProcess` sets `StandardInputEncoding`, `StandardOutputEncoding` and `StandardErrorEncoding` to UTF-8 (no BOM) at start; a red-first test round-trips a frame carrying `—` and `§` through the reader.

**BECAUSE:** `â€"` and `Â§` are the UTF-8 bytes of `—` (E2 80 94) and `§` (C2 A7) decoded as a single-byte code page. `ProcessStartInfo` at `AcpEngineProcess.cs:116-124` redirects all three streams and sets **no encoding** (Verified absent); `Output => _process.StandardOutput` (`:76`) reads with the platform default — the console code page on Windows (**Inferred**: not run by the Owner; the symptom matches exactly). ACP is newline-delimited UTF-8 JSON. A defect, not a floor trip: no byte is lost, and nothing durable holds replies (`RunChannelSessionThread` — *"nothing durable"*), so there is nothing to migrate.

**CONFIDENCE:** Verified for the absence; Inferred for the runtime code page.

**SCOPE EFFECT:** T0, lands before Rulings 81/82. **Lane: Conversation** (`AcpEngineProcess.cs` is its file).

**CONDITIONS:** The test fails on `main` before the change (record the red run). Register the class: *a redirected child stream read with the platform default encoding*.

**RECORD AS:** Ruling 87 — engine process streams are UTF-8, red-first with `—`/`§`; class registered; Conversation lane, T0.

---

## Filing note (Rulings 80–87)

- **Not ruled:** which `agent.thought` rendering the CLI's collapsed-thinking idiom maps to — D1's, within Ruling 82's bounds.
- **Finding for the conductor:** `composer.html:35-36` (110 px) vs `ComposerSurface.EditorFloor` (130 px) — one floor, two definitions; folds into Ruling 80's slice.
- **Finding for the Shell lane:** the reconcile refuses every drag while a stack is maximized (Ruling 83 BECAUSE); Ruling 83 removes the trigger, not the blindness.

---

# Rulings 88–89 — D3's two open questions (2026-09-13)

Issued by the **Owner** (`fable`) on D3's review (`docs/reviews/ui-operator-findings-2026-09-13.md` §7–§8) at `main` `560ea825`. Evidence opened: review §2c/§3a/§7/§8, Rulings 74/83/84, `DESIGN.md:1107`, `ZoneLayout.cs:193-208`, `WorkbenchShell.cs:1826-1846`, `ZoneBackedLayoutService.cs:137-139`, `session-conversation.html:1168`. One fact the question did not carry changes Ruling 88's conditions: option (c) is, byte for byte, the zone shape the reconcile refuses.

---

## Ruling 88 — Coding's default Bottom is collapsed (the operator's `Bottom (1)`); the terminal is on demand; the "≥ 3 turns" row becomes a per-viewport measured threshold

**RULING:** Operator's evidence: all five screenshots (09:27–09:33) show *Bottom (1)* collapsed. **(c) adopted**: `CodingDefault` Bottom = one terminal, `Collapsed: true`. (b) not adopted — SH-4.3 struck; (a) stays deleted. Ruling 83's re-cut *"Left = session documents (empty until one opens) · Center = empty state · Bottom = one terminal"* becomes *"… · Bottom = one terminal, collapsed"*. `DESIGN.md:1107` *"≥ 3 at rest"* becomes: *"at 1440 × 900 (session at Left, Bottom collapsed, editor at 280): ≥ 1 at rest; at 2560 × 1600: ≥ 2; the row prints the viewport and threshold it applied"*; the ≥ 2/≥ 1 sub-cases hold at ≥ 1. D3's *"previous turn's outcome line pinned"* is cut — a new mechanism measured at +0 turns.

**BECAUSE:** Ruling 83's logic (the gesture is the decision) and §2c (120 px collapsed, 0 px with the terminal across). (b) is geometry for a terminal the operator does not keep open. But (c) is the shape the reconcile refuses: `WorkbenchShell.cs:1830-1834` *"a collapsed tool zone that still holds panes is not rendered … the surface-set guard … refuses the whole reconcile"*; `ZoneBackedLayoutService.cs:139` `Rendered(z) => !Collapsed && !IsEmpty`. The default would refuse every drag — finding 4 by another route.

**CONFIDENCE:** Verified (refusal path, `CodingDefault :193-208`); the turn counts are mockup measurements — Inferred until the WPF tree measures them.

**SCOPE EFFECT:** F-1 promoted from finding into SH-4.2: `ReconcileTests.ADragWhileAZoneIsCollapsedHoldingPanes_IsApplied`, red-first. L1 → `CodingDefault_IsLeftEmpty_CenterEmpty_BottomOneTerminalCollapsed`; new L6 `AtStartupSizeDockedLeftBottomCollapsed_TheThreadHoldsOneTurn_WithTheEditorAt280` (the IA lens's clear condition). Mockup Layout axis keeps *bottom-collapsed* only. A-9 (30 vs 24 px) stays should-fix-next. **Lane: Shell (SH-4.2)**; CV-5.4's R-oracles read the row's values.

**CONDITIONS:** (1) O-2 passes with the default Bottom collapsed-holding. (2) The terminal is one gesture away. (3) The row's numbers are replaced by measured ones at the join.

**RECORD AS:** Ruling 88 — Coding's default Bottom is collapsed (one terminal, on demand); Ruling 83's re-cut amended; (b) struck; DESIGN.md "≥ 3 turns" row becomes a per-viewport measured threshold; F-1 promoted into SH-4.2; Shell lane.

---

## Ruling 89 — the Console split with the session at Left is a `Console — <session>` document in the Center zone; one per session; it closes with the session

**RULING:** The split is a **document** — kind `console`, identity `console:<sessionId>`, caption *Console — <session>* — opened on demand into the **Center** zone, never inside the Left pane. One per session: a second toggle focuses it. It closes when its session document closes. It is not re-homed when the session moves; if the session is itself in the Center, the Console opens as a sibling tab in that stack. Opening it while the session is maximized restores the tree (Inferred; D1 draws it).

**BECAUSE:** Ruling 74: *"the Console split is an on-demand view of the same stream opened at a turn"* — a view of a document's fold is a document, not a tool pane; a tool zone would outlive its session. The Left pane at 673 px yields a 224 px Console (IA-6, Verified CSS). D3 rendered this home (`session-conversation.html:1168`); the IA lens met it pending my word. The Left pane's geometry is untouched, so `DESIGN.md:1107` *"≥ 1 with the Console split"* holds at 1440 × 900 under Ruling 88.

**CONFIDENCE:** Verified for Ruling 74's text and the mockup; WPF close/move behaviour Inferred until C5.

**SCOPE EFFECT:** C5 asserts: opens in Center as a `console` document; `console:<id>` count ≤ 1; the Left stack never contains it; closing the session closes it. Rulings 21/74 extended, not amended. Lane by seam: the surface's content is CV-5.2 (Conversation); the kind's zone rule/factory row, if one is required, is SH-4.2's (Shell) — CV-5.2's must-not-touch stands.

**CONDITIONS:** No second store — the document reads the thread's `Coalesce` rows (Ruling 81). The Center's empty copy yields to the Console tab while open.

**RECORD AS:** Ruling 89 — the Console split is a `Console — <session>` document in the Center zone, one per session, closing with the session; Rulings 21/74 extended; C5's assertions fixed; content CV-5.2, zone rule SH-4.2.

---

**Filing note (Rulings 88–89).** The IA lens's Blocker (IA-1) clears on Ruling 88; its stated condition ("must measure ≥ 1 turn with the editor at 280") is L6.

**Filing note (SH-4.2, 2026-09-13 — for the conductor's ratification).** Ruling 89's *"a second toggle focuses it"* is honoured by the **verb** (`session.console`, the View menu, a turn's *Open the log*), not by the header's ToggleButton: hosted, the toggle's state is the Console document's open state and its second press **closes** it — the UX & Accessibility lens's Blocker on the slice (a ToggleButton that never releases lies to the UIA Toggle pattern, WCAG 4.1.2). The count of `console:<id>` stays ≤ 1 on every path. The call and its one-line reversal are `docs/notes/console-toggle-closes-on-second-press.md`; the evidence is `docs/proof/coding-recut-left-dock.md` Claim 8. Ruling 88's premise that the collapsed default *"would refuse every drag"* was measured wrong for the Bottom (never pre-seeded, so kept) and right for the Left/Right; the same oracle found a raw-count tie that moved the session — Claim 5 of the same pack.

---

## Ruling 90 — the header's Console toggle closes the Console document on its second press; the verb focuses it (Ruling 89 amended); Ruling 88's premise corrected

Issued by the **Owner** (`fable`) at `main` `3e5b04f6` on SH-4.2's recorded deviation (`docs/notes/console-toggle-closes-on-second-press.md`).

**RULING:** Ratify the deviation. Ruling 89's *"One per session: a second toggle focuses it"* becomes *"One per session: the header toggle's state is the document's open state — a second press closes it; a second run of the verb `session.console` (View menu, palette, chord) or a turn's *Open the log* focuses the one open console."* Ruling 89's substance — count of `console:<id>` ≤ 1 on every path — is unchanged.

**BECAUSE:** The control is a `ToggleButton` (`SessionDocumentSurface.cs:999`), driven from `Checked`/`Unchecked` with `ReflectConsole` non-dispatching (`:1006-1019`, `:230`); a toggle whose second press never releases reports `ToggleState` On while `IToggleProvider.Toggle` changes nothing — a role/value lie under WCAG 4.1.2, the UX & Accessibility lens's Blocker, cleared in loop 2 with the deviation *"acceptable from the UX side, explicitly"* (proof pack §Reviews). The oracle presses through the UIA pattern itself (`ConsoleSplitPlacementTests.cs:39-40`), so the fix is measured, not asserted. The alternative — a plain Button — is a control-type change in a Conversation-lane file beyond SH-4.2's seam, and loses the honest open-state the toggle now carries. **Ruling 88's premise is corrected:** *"the default would refuse every drag"* was wrong for the Bottom (never pre-seeded, so kept — but mis-anchored on a raw-count tie, *Expected Left, Actual Center*) and right for Left/Right; 9 of 10 pre-fix reconcile rows were red (Claim 5, `red-run-core-reconcile.txt`). Ruling 88's outcome stands; its BECAUSE overstated the mechanism.

**CONFIDENCE:** Verified (note, filing note, Claim 5/8, §Reviews, the handlers, the oracle's pattern press).

**SCOPE EFFECT:** Ruling 89 amended as above; Rulings 21/74 unchanged. No plain-Button rework. Finding 5 (`CurrentRegion` reports `Split` for the hosted console) stays a CV-lane finding.

**CONDITIONS:** (1) C5 keeps asserting *unchecking closes* and *the verb focuses, count 1*. (2) The hosted help text stays state-neutral. (3) The decision note's status moves `draft` → accepted, citing this ruling.

**RECORD AS:** Ruling 90 — the Console toggle closes on its second press, the verb focuses (Ruling 89 amended, UIA Toggle honesty); Ruling 88's "refuses every drag" premise corrected (Bottom kept-but-tied, Left/Right refused, 9 of 10 rows red); SH-4.2 deviation ratified.

---

## Ruling 91 — the operator's decision filed: F5 merges to `main` before its exit run; the gesture is performed on the current build (Ruling 79's "run on the frozen tree" superseded)

**RULING (the operator's words, 2026-09-14: "merge F5 and proceed"):** `feature/exit-evidence`
(frozen at `135e05e1`, 21 commits) merges to `main` now — the session-origin stamp, the nine-clause
exit oracle and its CI gate, the front-door Proof Pack with its `RUN-PENDING` clauses intact. The
exit run's gesture (Ruling 49: the operator's own File → New Session; no headless entry) is performed
on the **current** build, so the front-door evidence is about the product the operator uses. The
Proof Pack's clauses 2, 3, 5, 6 and 9 stay `RUN-PENDING` until that gesture; the pack's own words
hold: nothing in it is evidence the front door works until a row says a gesture happened.

**BECAUSE:** The frozen build predates every Addenda C/D slice — the six-field composer, no
perspectives, the pre-Ruling-73 lease. A gesture on it would prove a front door that no longer
exists as shipped. Ruling 79 froze the tree so the run could happen without the join's churn; the
churn is over. Merging first costs one join; running frozen costs an obsolete proof.

**CONFIDENCE:** Verified for the tree's state (`git rev-list --count main..feature/exit-evidence` =
21); the merge's conflicts are the join's to measure.

**SCOPE EFFECT:** Ruling 79's second clause superseded; Ruling 49 unchanged (the gesture remains the
trigger). F5's oracle (`tools/verify-front-door-exit.py` or its equivalent) must run green on the
merged tree in its self-test form and `RUN-PENDING` form.

**RECORD AS:** Ruling 91 — F5 merges to `main` unrun; the exit run's gesture is performed on the
current build; Ruling 79's "run on the frozen tree" superseded; Ruling 49 stands.

---

## Rulings 92–103 — the operator's findings of 2026-09-14 (09:03–09:10; eight screenshots, `C:\Users\malla\Downloads\ui findings 9-14-AM`), ruled on the conductor's evidence brief

*Filed by the conductor verbatim from the Owner's return (2026-09-14). The operator's words: "i worked through the app, looking a lot better here are my findings … consider F5 done. Keep going with next steps." Evidence the Owner opened: Rulings 57/59/61/77/81/82/85; `NodeReaderView.cs:231-263`; `ZoneLayout.cs:249-271`; `ComposerSurface.cs:127-137, 211-222, 957-958, 1491-1500`; `ComposerSendGate.cs:219-229`; `ThreadFeed.DisclosureStyle`; `StoreReader.cs:493-502`; `WorkspaceCore.cs:433-441`; `EngineCatalog.cs`; the three screenshots F-A, F-C, F-E. Screenshots F-B, F-D, F-F and the review-only shot taken as the conductor described them. Findings F-A…F-F are the operator's (their file titles); R-1…R-5 are the conductor's from the review-only screenshot; the evidence brief is in `docs/reviews/ui-operator-findings-2026-09-14.md`.*

---

## Ruling 92 — F-A: the edge row is a left-aligned grid with one baseline

**RULING:** Operator: *"need to fixt the layout of the metadata view."* `EdgeRow` becomes a three-column `Grid` (predicate `Auto` min 104 · target `*` · status `Auto`), all three `TextBlock`s at 12 px on one baseline (`VerticalAlignment=Center`, status in the muted brush, not a smaller size), the `Button` template overridden so its content presenter stretches (`HorizontalContentAlignment=Stretch` is being ignored by the App's centred style — the row must not depend on the style).

**BECAUSE:** `NodeReaderView.cs:233-253` (Verified): a `DockPanel` measures to content and a centred presenter floats it; `Muted(status, 11)` beside `Text(target, 13)` is the superscript the screenshot shows. The `MetaRow`s above are correct because they are not inside a `Button`.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** T0. **Lane: Explore** (`NodeReaderView.cs`). No new control.

**CONDITIONS:** A rendered-surface test asserts every row's predicate `TranslatePoint` X equals the metadata rows' label X (left edge shared), and the three blocks share one `FontSize`.

**RECORD AS:** Ruling 92 — Explore's typed-edge rows are a left-aligned three-column grid on one baseline; Explore lane, T0.

---

## Ruling 93 — F-B: "View source" in Explore renders in the reader pane through `NodeContentAsync`; ADR-0018 is accepted by the slice that builds it

**RULING:** Operator: *"right click on a node … choose view source and see the source in the right viewer eg code or rendered markdown or html."* In Explore the canvas context menu gains **View source** (first item), which renders the node's content **inside `NodeReaderView`'s content area** (replacing the placeholder sentence, which is deleted) — never a routed `codeviewer` pane. Rendering by `RenderKind`: `Code` → syntax-highlighted, reusing the CodeMirror host already in `composer.html` (one WebView2, read-only mode); `Text` whose language is markdown → `ProseMarkdown` (the existing WPF renderer, no link activation); `Html` (new `RenderKind` value, Owner extension of the enum, admitted because ADR-0018's text names it) → rendered in a WebView2 with **script disabled, no navigation, no network** (`NavigateToString`, `IsScriptEnabled=false`), falling back to `Code`/html when that sandbox cannot be asserted; `None` → the shortfall sentence, verbatim. The reader keeps its metadata and edges above the content. ADR-0018 moves to *accepted* in the same slice, with the erratum that its consumer is Explore's reader.

**BECAUSE:** Core already ships the query (`NodeContent`, `ProjectionService.cs:1126`, per the conductor, bounded and honest); the App's `Open as…` routes to Architecture's `codeviewer`, which is US-C3's rule for *structural* views (Ruling 58/59) — the operator wants the content *in the right viewer* of Explore, which is a reading act on the current pane, not a kind-open. The pane's own placeholder promises exactly this. Rendering repository HTML with script enabled would be executing workspace code in the product process; that is a Security floor, so the sandbox is a condition, not a preference.

**CONFIDENCE:** Verified for `NodeReaderView`, the catalog of renderers as the conductor listed them, ADR-0018's status; **Inferred** that the CodeMirror host can be re-used read-only without a second WebView2 (the conductor measures).

**SCOPE EFFECT:** T1 (one `/design-slice`; surface list: `NodeContent` model → `NodeReaderView` → context menu → rendered surface). Cuts: a second, separate viewer pane in Explore; any Explore route to `codeviewer`; `Html` rendering with script. Freezes: `Open as…`'s structural entries unchanged. **Lane: Explore** (Core seam: `NodeContent.RenderKind` +`Html`, one row).

**CONDITIONS:** (1) Red-first rendered-surface test per `RenderKind` including `None`/shortfall. (2) Measure WebView2 private-bytes delta for the reader host (P-4 pattern); if a second WebView2 costs more than the composer's, share one. (3) Explore's menu in the RAISING host is unchanged. (4) The HTML sandbox flags are asserted by a test that navigates a page with `<script>` and observes no execution.

**RECORD AS:** Ruling 93 — Explore's View source renders code / markdown / sandboxed html / shortfall in the reader pane via `NodeContentAsync`; `RenderKind.Html` added; ADR-0018 accepted; Explore lane, T1.

---

## Ruling 94 — F-C: Architecture default = Left Graph · Center Contexts, Domain · Right empty; the `inspector` kind retires and its three fields fold into the Evidence row

**RULING:** Operator: *"this is the default layout i want … AND we should eliminate the provenance tab."* `ArchitectureDefault()` becomes: **Left** = `[Graph]` at the extent in the operator's saved slot; **Center** = `[Contexts (active), Domain]`; **Right** = empty, collapsed; **Bottom** = empty, collapsed. Evidence (`view`) leaves the default, stays admitted (View menu). The **`inspector` kind is retired from the product** — removed from every allow-list, the descriptor row and `SurfaceContentFactory`; its three fields (origin · extractor · rev, `EvidencePaneViewModel.cs:234`) render as a second muted line under the selected Evidence row (a detail-on-select inside the master; no second pane). Ruling 61's "renders the selected row's detail, never a second list" is satisfied by the row; its owed "two kinds render different content" test is withdrawn with the second kind.

**BECAUSE:** The screenshot (Verified) shows exactly Left Graph ~20 % · Center Contexts/Domain · no Right strip · Bottom collapsed, and current `ZoneLayout.cs:251-259` differs on all four zones. "Eliminate" applied to a kind that is only ever a detail half of one pair (Ruling 61 BECAUSE) leaves it with no host — a kind no host admits is dead code (HYG-A), so retirement, not removal from one allow-list. Evidence not in the operator's layout: their words are "default layout", so it leaves the default and nothing more.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Amends Rulings 59 (default) and 61 (pair → master with inline detail); ADR-0032 test 1 extended with `inspector` dropped-with-report from saved envelopes. **Lane: Shell** (`ZoneLayout.cs`, `SurfaceContentFactory.cs`, `EvidencePaneViewModel.cs`). T1 only because of the envelope drop; the layout change itself is T0.

**CONDITIONS:** (1) Left's `DefaultExtent` is **read from the operator's `<layout>.architecture.zones.json`** and recorded in the note — never guessed from the screenshot. (2) A pre-existing saved envelope carrying `inspector` reconciles with a report naming this ruling, not a crash. (3) The Graph pane at that extent shows no horizontal scrollbar in its own header row (the screenshot shows one at y≈1118; if it is the canvas's own control strip, that is a finding for Explore, not a blocker).

**RECORD AS:** Ruling 94 — Architecture default re-cut to the operator's layout (Left Graph · Center Contexts, Domain · Right empty); `inspector` retired product-wide, Provenance fields fold into the Evidence row; Rulings 59/61 amended; Shell lane.

*Conductor's note: the operator's saved slot (`%LOCALAPPDATA%\AiDe\workspaces\aide.31abcd…\layout.architecture.zones.json`, saved 2026-09-14T16:13:09Z) reads Left `graph` extent **0.22**, Center `[contexts (active), domain]`, Right empty extent 0.22 not collapsed, Bottom collapsed — read by the conductor before dispatch; the lane records it again from the file.*

---

## Ruling 95 — F-D: a Send while a turn runs offers Wait (one queued turn) or Parallel (a derived sibling session); Ruling 77(b) reversed as its own condition 2 foresaw

**RULING:** Operator: *"if i submit a second prompt i should be able to have it wait or start a second task-session in parallel."* On Send while a turn is running or waiting, the composer's status line (where the refusal already lives, `ComposerSurface.cs:847-860`) becomes a two-action line: **"b1 is running. [Wait — send after b1] [Start a parallel session]"**; no modifier key, no dialog. Rules:

- **Wait:** the envelope is prepared/compiled *now* (same gate, same bytes, sha recorded) and appears in the thread as a turn row with state **`queued`** ("b2 queued — sends after b1"); the editor clears. **Exactly one** queued turn per session; a further Send while one is queued is refused: "b2 is queued; cancel it or wait." A queued row has two actions: **Cancel** (drops it; `UseAsNextDraft` returns its text to the editor) and nothing else — editing is cancel-and-redraft, never in place. It **sends automatically only when b1 ends Completed/Answered**; on Stop, Failed or cap-ask it stays queued and the status reads "b1 stopped; b2 is waiting — Send it or cancel it", with **Send now** enabled on the row for that case only. It waits through `Waiting` (permission).
- **Parallel:** `SessionConfigStore.Create` with the same workspace, backends and config copied, `origin = "parallel:<parent session id>"`, name per Ruling 99's rule (`"<parent name> (2)"`); opens docked beside the parent (Left stack, Ruling 83); the prompt is its first turn, sent through the new session's own gate (lease derivation and prepare apply unchanged). The parent's draft is consumed; attachments follow the prompt.

**BECAUSE:** Ruling 77(b) was marked "an Owner extension, reversible", and its condition 2 pre-wrote the reversal: *"re-read as queued after b1 with a queued outcome and nothing else moves"*. The operator has now asked. ACP takes one `session/prompt` per session, so Wait is the only in-session shape and Parallel is necessarily a second `session/new` on a second lane. Compiling at queue time keeps "exactly the bytes that will be sent" true for a queued row; compiling at drain time would send bytes the operator never saw.

**CONFIDENCE:** Verified for the refusal path and Ruling 77; **Inferred** that two engine processes for one workspace run concurrently in the App (two sessions were open in the 09:03 screenshot; that both ran turns at once is not recorded).

**SCOPE EFFECT:** T1, one slice, **Lane: Composer** (status line, queued row, drain) with **Sessions/AgentPlane** seam (parallel create). Cuts: a multi-item queue; in-place editing of a queued turn; a modal. Amends Ruling 77(b), §B5's STA rows (+`queued`, +`queued after stop`), DESIGN.md's refusal copy.

**CONDITIONS:** (1) **Measure first:** two sessions on the same workspace each complete a turn concurrently against the real adapter; record process count and both outcome lines. If the run host serialises them, Parallel is refused with that reason until it does not — Wait still ships. (2) Red-first STA rows for queued · drained · stopped-with-queued · cancel. (3) The drain never sends after Stop/Failed (test). (4) Spend for the queued turn's compile is reported on its own outcome line (Ruling 78).

**RECORD AS:** Ruling 95 — Send-while-running offers Wait (one queued, compiled-now turn that drains only on Completed/Answered) or Parallel (derived sibling session, origin `parallel:<id>`); Ruling 77(b) reversed per its condition 2; Composer lane with Sessions seam, T1.

---

## Ruling 96 — F-E: the expanded disclosure must show the compiled text; collapsed at rest stands, but the open state is sticky per session

**RULING:** Operator: *"the compiled prompt thing … is expanded but shows no compiled prompt i expected it there."* Defect: when `IsExpanded` is true the `_compiled` box renders its text at ≥ `CompiledPromptMinHeight`. Ruling 57's collapsed-at-rest is kept (the operator did not ask for open-by-default), but **the disclosure's open state persists for the session document** — opened once, it stays open across turns and reopen, so "expected it there" holds the second time.

**BECAUSE:** `_compiled.Text` is set on every draft change (`ComposerSurface.cs:957-958`, `RenderView` returns `Projection.Compiled`, `ComposerSendGate.cs:227-229`) and a one-line message compiles to non-empty bytes (the gate refuses only whitespace, `:318`), so the text is almost certainly present; `MinimumHeight` adds `CompiledPromptMinHeight` only when open (`:1493`), and the screenshot shows the editor still at its rest height with 37 px under the header — consistent with the belt not re-measuring on expand, so the reader gets 0 px. That is **Inferred**; the measurement is a condition.

**CONFIDENCE:** Verified for the wiring; Inferred for the cause.

**SCOPE EFFECT:** T0. **Lane: Composer.** Ruling 57 unchanged in substance; "on demand" now means "until you close it".

**CONDITIONS:** (1) Before the fix: toggle the disclosure on a one-line message draft and record `_compiled.Text.Length` and `_compiled.ActualHeight` — the note states which was zero. (2) The existing on-screen test extends to the **content**: after expand, `_compiled.ActualHeight ≥ CompiledPromptMinHeight` and `Text == RenderView(draft).Text`. (3) The editor floor (`EditorFloor`) is not violated by the reopened box — INV-0007 stays green.

**RECORD AS:** Ruling 96 — the compiled prompt renders when expanded (content oracle, not header-only); open state is sticky per session; collapsed at rest stands; Composer lane, T0.

*Conductor's note: the operator's run ledger (`workbench-20260914.log`, `composer.layout` rows for `composer:20260914T160128Z-c9c0e19a`) reads composer 489.5 × 517.13, editor 280 at rest, `"compiled": {"width": null, "height": null}` at 248 inputs — `EmitLayout` writes null when `_compiled.IsArrangeValid` is false. The lane reproduces that belt for condition 1.*

---

## Ruling 97 — F-F: the sheet lists every catalog engine with its state; codex → copilot (incl. enterprise host) → gemini → grok, each behind its own Spike; no engine launches on an unobserved path

**RULING:** Operator: *"support enterprise github copilot … and codex"*; *"choose what agent … GHCP Claude Code Codex Grok Gemini."* Split into: **(i)** the New Session sheet lists **every `EngineCatalog` row** with a state — *ready* (account recorded + launch resolvable) · *needs sign-in* (row + adapter observed, no account) · *not configured* (no observed launch path; the refusal reason as the sub-line) — each with a per-engine **Configure…** action that opens the provider's sign-in instruction (copy, not an embedded browser). Nothing is hidden. **(ii)** Catalog rows `gemini` (provider `google`) and `grok` (provider `xai`), both `AcpMode.Deferred` with the refusal "not spiked" until observed. **(iii)** Spikes, **in this order**: **codex** (install `@agentclientprotocol/codex-acp@1.10.0`, observe the entry module, record it — unlocks the *existing* adapter path, smallest win), **copilot** native (`copilot --acp` handshake observed; the `simplify:` trigger has fired, so `ResolveLaunch` gains the Native path and `ANonAdapterModeIsRefusedWithANamedReason` is re-pointed on purpose), **enterprise copilot** = the `copilot` row plus a per-account `host` in `providers.json` (§14.2 erratum note; auth via `gh auth login --hostname <host>` / copilot's own login — **Inferred until spiked**), **gemini**, **grok**. Each spike is a domain-researcher run whose record names the observed command line, handshake, and auth gesture; no launch code lands before its record.

**BECAUSE:** `EngineCatalog.cs:67-71` names this exact upgrade trigger; `:35-38` states the rule that an unobserved entry module is a refusal, never a guess — the spike obligation is the catalog's own doctrine. Hiding unconfigured engines is the sheet asserting a smaller catalog than the product carries; listing them with the refusal reason as their state is the honest form and is the operator's ask.

**CONFIDENCE:** Verified for the catalog and its refusals; Inferred for every engine's native/enterprise auth surface.

**SCOPE EFFECT:** (i) T1, **Lane: Sessions** — lands first, no spike. (ii)+(iii) **Lane: Sessions/AgentPlane**, T2 as a programme, one T1 slice per engine, in the order given. Cuts: an embedded sign-in browser; a fifth `AcpMode`; any engine row without a provider. **Not ruled:** the operator's "acp-mcp" — MCP servers are a per-session config item, not an engine; the conductor asks the operator whether they meant MCP servers in the sheet, and files it separately.

**CONDITIONS:** (1) Per engine, a red-first `ResolveLaunch` test that passes only with the observed path recorded. (2) The sheet's state column is derived from the catalog + `providers.json` at open time, never stored. (3) The enterprise host is a field on the provider account, never on the engine row.

**RECORD AS:** Ruling 97 — the sheet lists every catalog engine with a derived state and a configure action; gemini/grok rows added as Deferred; Spike-then-launch in the order codex → copilot (+enterprise host) → gemini → grok; Sessions/AgentPlane lanes; "acp-mcp" referred back to the operator.

---

## Ruling 98 — R-1: a snapshot stamped with the retired fixture literal is not reusable; one re-extraction, then the observed HEAD

**RULING:** Option (a): `Reusable(probe, scopeId)` returns false when the snapshot's `artifact_revision` base equals the retired literal `"rev-1"`, so the next index re-extracts once and stamps the observed HEAD; the literal is named as a constant with the comment that it is a retired fixture, not a revision. Meanwhile the conductor tells the operator that **Re-index all** clears it today (option c). Option (b) is refused: printing "not recorded" for a value the store does hold is masking.

**BECAUSE:** `StoreReader.cs:493-502` (Verified) reads the stored value; `WorkspaceCore.cs:433-441` (Verified) reuses on an unchanged fingerprint without opening the snapshot's revision, so Ruling 85's fix cannot reach a pre-85 store — the finding is real and bounded to stores written by the fixture-default build.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** T0. **Lane: Store** (`WorkspaceCore.Reusable`). Ruling 85 unchanged.

**CONDITIONS:** Red-first: a store whose latest committed snapshot carries `"rev-1"` indexes as *re-extracted*, not *reused*, exactly once, and `CurrentSourceRevision()` then equals the passed revision. A store with a real revision stays reused.

**RECORD AS:** Ruling 98 — a snapshot stamped with the retired `rev-1` literal is not reusable; one automatic re-extraction; Store lane, T0.

---

## Ruling 99 — R-2: session names are unique within a workspace by a counter suffix; Create never refuses

**RULING:** The default name `yyyy-MM-dd session` (`NewSessionSheetViewModel.cs:144`) gets ` (2)`, ` (3)`, … when a session of that name already exists in the workspace (all sessions in `SessionConfigStore`, not only open tabs); an operator-typed duplicate gets the same suffix on Create, announced. No refusal; no time-of-day — a name is what the operator reads across tabs, and "09:08" reads as a timestamp, not a name. Ruling 95's parallel session uses the same rule on its parent's name.

**BECAUSE:** The screenshot shows two identical tabs and two identical Console tabs (as described); the cause is the format alone. A refusal on Create is friction for a default the operator did not type.

**CONFIDENCE:** Verified for the cause (the conductor opened it); the rule is the Owner's choice.

**SCOPE EFFECT:** T0. **Lane: Sessions.**

**CONDITIONS:** The Console tab's caption derives from the session name, so it disambiguates for free — test both captions.

**RECORD AS:** Ruling 99 — session names unique per workspace by counter suffix, Create never refuses; Sessions lane, T0.

---

## Ruling 100 — R-3: bookkeeping frames are Console-only; the thread's fold counts the rest

**RULING:** `acp.session.update.usage_update` and `available_commands_update` are bookkeeping: they stay Console rows and feed Spend (Ruling 78) but **do not count in, or render inside, the thread's "N events" fold**. Owner extension of Ruling 82, marked; the list is a named constant of two kinds, not a pattern.

**BECAUSE:** Ruling 82 already classes `acp.*` as Console evidence, not conversation; a fold that reads "14 events" with 4 usage rows is a count that misleads about what the agent did. The operator did not flag it, so this rides with Ruling 101's T0 slice and costs one filter.

**CONFIDENCE:** Verified for Ruling 82; the extension is the Owner's.

**SCOPE EFFECT:** T0, same commit as Ruling 101. **Lane: Conversation.** Ruling 74's identity oracle (M1) is over the Console, which is unchanged.

**CONDITIONS:** The Console still shows every frame (M1 green); the fold's count equals events minus the two named kinds (test).

**RECORD AS:** Ruling 100 — `usage_update` / `available_commands_update` are Console-only bookkeeping, excluded from the thread's fold; Conversation lane, T0.

---

## Ruling 101 — R-4: a tool-result row renders its content's first text line and byte count, never its own kind twice

**RULING:** `ConsoleStreamModel.TextOf` reads `content[]` of `ToolCallContent`: the first `text` item's first line plus ` · <n> bytes` (total text bytes); when no item carries text, the body reads `no text content (<n> item(s): <types>)`. The kind is never the body.

**BECAUSE:** `ConsoleStreamModel.cs:269-285` per the conductor (Verified by them): the fallback to the kind is the row saying nothing, which is a plausible-looking non-value (IO: never a plausible wrong number).

**CONFIDENCE:** Verified as reported; the Owner did not open the file.

**SCOPE EFFECT:** T0. **Lane: Conversation** (`ConsoleStreamModel.cs`).

**CONDITIONS:** Red-first against a frame from the 88-frame corpus whose `content` is an array; a frame with only non-text items renders the "no text content" form.

**RECORD AS:** Ruling 101 — tool.result Console rows read `content[]` text (first line · byte count) or say "no text content"; Conversation lane, T0.

---

## Ruling 102 — R-5: the task-class list wraps; no horizontal scrollbar

**RULING:** The sheet's task-class descriptions wrap to the list's width; horizontal scrolling is disabled on that list.

**BECAUSE:** As described (a clipped description is copy the operator cannot read); cosmetic, T0.

**CONFIDENCE:** Flagged — screenshot not opened by the Owner.

**SCOPE EFFECT:** T0. **Lane: Sessions.**

**CONDITIONS:** A rendered test at the sheet's default width finds no description with `IsTextTrimmed` and no horizontal scrollbar visible.

**RECORD AS:** Ruling 102 — task-class descriptions wrap in the New Session sheet; Sessions lane, T0.

---

## Ruling 103 — F5 closed on the operator's word; the record says so and carries no frames

**RULING:** Land `spikes/conductor-front-door-exit-run/exit-evidence.json` with `attended_by: "operator"`, `observed: "the operator's report, 2026-09-14"`, `operator_words: "consider F5 done"` verbatim, `frames: "not recorded"`, `gesture: "File → New Session with @hello.txt"`. `verify-front-door-exit-evidence.py` accepts a frameless record **only** when `attended_by == "operator"` and `operator_words` is non-empty; the `--self-test` is removed from the gate's invocation so it runs bare.

**BECAUSE:** The operator's word is the decision; the record must not dress it as a measurement. Claims and evidence stay in separate columns: the claim column is the operator's sentence, the evidence column is *not recorded*.

**CONFIDENCE:** Verified for the operator's words as quoted; the verifier's current acceptance rule not opened by the Owner.

**SCOPE EFFECT:** Closes F5. No frames are owed.

**CONDITIONS:** The verifier's frameless branch is red-first: a record with `attended_by: "conductor"` and no frames fails.

**RECORD AS:** Ruling 103 — F5 closed on the operator's word; exit-evidence record carries `attended_by: operator`, the words verbatim, frames not recorded; gate runs bare.

*Conductor's reconciliation (2026-09-14, after the ruling): the Owner ruled on the conductor's brief, which said the record's fields were measurements nobody took. After dispatching that brief the conductor found the run's own artefacts: the operator's 09:01 session's `session-events.jsonl` carries `session.open` with `origin: main-menu.new-session` on build `1.0.0+51e806f8`; the 06:01 session carries none (the companion, direct); the envelope's `submitted` row carries `text_sha256` and `projection_sha`; the `consumed` row reads `run-9a0cff77 · Completed · episode_id "not recorded"`; the app ledger shows zero `terminal.start` rows in the run's window and an active `console` mode; the workspace store's `scored_episode_cell` holds no row for the run. The record therefore carries the operator's words verbatim exactly as ruled, AND every clause field a file can answer, each naming its source file, with `"not recorded"` only where no artefact exists (clause 3's scored cell, clause 6's latencies) — the same principle ("claims and evidence in separate columns") applied to better evidence, not a different decision. The gesture is recorded as performed — File → New Session and a prompt beginning "review the last specification we built…" — not as the plan's `@hello.txt` wording. The verifier's attended branch accepts the record on the operator's word and reports each clause's status from the fields present, so the pack's headline reads what happened rather than "passed".*

---

## Landing order (the Owner's; the operator tests in the mornings)

| Morning | Slice | Rulings | Lane | Tier | Spike |
|---|---|---|---|---|---|
| 1 | T0 batch, one commit each, all certain | 92 F-A · 96 F-E · 98 R-1 · 99 R-2 · 101+100 R-4/R-3 · 102 R-5 · 103 F5 | Explore, Composer, Store, Sessions, Conversation | T0 | none |
| 1 (same day) | Architecture default + `inspector` retirement | 94 F-C | Shell | T1 (layout T0) | none |
| 2 | Engine sheet lists every engine with state | 97(i) | Sessions | T1 | none |
| 2 | View source in Explore's reader | 93 F-B | Explore | T1 | none (measure WebView2 cost) |
| 3 | Wait / Parallel | 95 F-D | Composer + Sessions seam | T1 | concurrency measurement first (condition 1) |
| 3→ | Engines | 97(iii) codex → copilot (+enterprise) → gemini → grok | AgentPlane | T1 each | **Spike per engine**, domain-researcher |

**Not ruled / referred to the operator:** "acp-mcp" in F-F (MCP servers vs engines). **Findings for the conductor, not scope:** the Graph pane's horizontal scrollbar in the F-C screenshot (y≈1118); the F-E screenshot also shows the status strip's `rev rev-1` in Coding, confirming R-1 is store-borne, not perspective-borne.

---

## Ruling 104 — first use: the sheet's Configure… installs the pinned adapter on gesture, `adapterInstallRoot` defaults to `~/.aide/adapters`, Create stays enabled with a truthful footer

*Filed by the conductor verbatim from the Owner's return (2026-09-14). The operator's words: "also - consistent with my points on the agent backend needing to be configurable · i tried building and testing the AI-DE app from a different machine and it has no configured agent back end · seems like we missed the 'first use' scenario". Evidence the Owner opened: Ruling 20 (`docs/notes/front-door-council-rulings.md:59-69`); `ProviderConfiguration.cs:116-120` (null when absent), `:150` (`adapterInstallRoot` is `RequiredString`), `:34-39`; `SessionComposerBinder.cs:90-96` (the refusal names no action); `EngineCatalog.cs:78-82, :130`. The Owner read the primary checkout, where Rulings 92–103 were not yet on disk (they were in the conductor's worktree, filed above in this same commit — condition 1 below is met by that order); it ruled on the conductor's summary of 97(i), which matches the filed text.*

**RULING:** Build first use as one T1 slice, "first use", in the Sessions/AgentPlane lane, landing with 97(i) in morning 2: the per-engine Configure… on the New Session sheet is the only first-use surface (no first-run page), the product may run the pinned npm install itself on an explicit gesture, `adapterInstallRoot` becomes optional with the product default `~/.aide/adapters`, and Create stays enabled with a truthful sentence when nothing is ready.

(1) Where and what, for claude-code, in this order:
  a. Prerequisite checks, all before any network, each a row with a result line: `node` on PATH + version, `npm` on PATH + version, `claude` on PATH + version. Missing → the row shows the exact install instruction (URL/command) and the version the spike observed as known-good (node v24.18.0), never a modeled floor.
  b. Adapter root: shown, defaulting to `~/.aide/adapters`; editable; a path inside a git checkout is refused with the reason (this machine's spike path is the defect class).
  c. Install: the product runs `npm install --prefix <root> <package>@<version>` where package and version come ONLY from `EngineCatalog` (never from the file, never from input), on the operator's button press, never at start, never elevated, with stdout/stderr streamed to a visible log and the exit code read; result line names the resolved `<root>/node_modules/<package>/<entry>` that `ResolveLaunch` will look for, and `ResolveLaunch` is what decides "installed". Bounded by a timeout that reports "not recorded", not a guessed state.
  d. Sign in: Ruling 20's engine-native flow, unchanged; re-probe on return.
  e. Write `providers.json`: provider `anthropic` (auth per the erratum), one account with the label the operator typed and `health` = what was observed — `ready` after a returned sign-in, `needs-login` otherwise (no new health value is invented; a "not probed" literal is not in the vocabulary and is not admitted). `engines.claude-code.model` written from the catalog's default model. The file is written when (c) succeeds, so a skipped (d) leaves a valid file whose sheet row reads "needs sign-in" and offers Sign in.
  The shell's existing absence state ("no agent backend is configured") gains one action: open New Session at Configure. That is the entry point on a fresh machine.
(2) `adapterInstallRoot`: optional; absent ⇒ `~/.aide/adapters`; present ⇒ override. The product writes the key only when the operator chose a non-default root. `ProviderConfiguration.cs:34-39` and the §14.2 erratum note are amended to say so (marked as extending the spec, not reading it).
(3) Sheet before configuration: every catalog engine listed with ready / needs sign-in / not configured (97(i)). Create stays enabled; with zero ready engines the sheet's footer reads: "No backend is ready. The session will open; a run will not start until one is configured." The binder's refusal at `SessionComposerBinder.cs:94-95` is amended to name the action: "... Configure a backend from New Session."
(4) Measurement: see CONDITIONS. (5) Lane/tier/order: as proposed. (6) Nothing ruled on "acp-mcp" — the conductor asks the operator directly.

**BECAUSE:** Ruling 20 fixed the doctrine — remediation is an action launched from the sheet, "one screen, not a wizard" — and a first-run page is a second surface for the same three states the sheet already has to render under 97(i); the code shows the product today hard-requires a hand-written file (`:150`) and a hand-installed module (`ResolveLaunch`) and the only refusal (`:90-96`) names no action, which is exactly the operator's fresh-machine failure. A default install root is a derivation (DM: derive, don't store), and it also removes the class the current machine exhibits: product config pointing into the repository's spike tree.

**CONFIDENCE:** Verified for the code and Ruling 20; Inferred for 97(i)'s content (not on disk when the Owner read; it matches); Inferred for the `claude` CLI prerequisite and its install command (adapter README, not spiked).

**SCOPE EFFECT:** Admits one slice "first use" (T1, Sessions/AgentPlane, morning 2, with 97(i)). Cuts: a first-run/onboarding page; any engine other than claude-code in the flow (codex etc. show "not configured" with no Configure action until their own launch is observed); auto-install at start; a health prober; `--ignore-scripts` as a rule (see condition 5). Freezes: package name/version source = `EngineCatalog` only. Defers: nothing.

**CONDITIONS:**
1. Rulings 92–103 are filed on disk before 104 is filed; if the filed 97(i) differs from the conductor's summary, 97 wins and the conductor returns for a one-line reconciliation.
2. Fresh-machine oracle, red-first: a test with USERPROFILE/HOME pointed at an empty temp dir observes (a)–(e) end to end with npm pointed at a recorded/local registry or a pre-packed tarball — no live network in the gate; the assertion is `ResolveLaunch` succeeds against the produced root and `ReadIfPresent` round-trips the written file.
3. Binder refusal names Configure — a red-first string test on `SessionComposerBinder`.
4. One live acceptance on the operator's second machine, recorded in the proof pack with the observed `node`/`npm`/`claude` versions and the install's exit code and duration; not-recorded fields stay "not recorded".
5. The conductor spikes the adapter install once and records whether it works under `--ignore-scripts`; if yes, the product passes it (lifecycle scripts are the residual supply-chain risk and it is named in the slice's risk list either way). Package and version remain pinned literals in `EngineCatalog`.
6. The install instruction text for missing `node` and missing `claude` is copied from the observed upstream source at spike time and cited in the note, not written from memory.
7. The refusal for a root inside a git checkout is tested against this machine's current value (`C:/Projects/ai-de/spikes/acp-subscription-lane`).

**RECORD AS:** Ruling 104 — first use is the sheet's Configure… (no first-run page); the product runs the pinned adapter install on gesture; `adapterInstallRoot` defaults to `~/.aide/adapters`; Create stays enabled with "no backend is ready"; one T1 slice with 97(i), morning 2.

---

## Ruling 105 — the account is the operator-facing unit: session config holds account selections and a default; a per-turn account picker on the composer; Higgsfield out pending a spike; spike order copilot → codex → gemini → grok

*Filed by the conductor verbatim from the Owner's return (2026-09-14). The operator's words, answering the conductor's "acp-mcp" question: "acp=mcp: For the tool backend i need to be able to use: - my claude subscription (my max account in my case) - my Microsoft work account with GCHP (so i can let some of my colleagues try it who may not have a claude account) - My OpenAI, Grok and Gemini subscriptions. For any given session in the tool... I should be able to switch between any of the accounts i listed above as well as Higgsfield". Evidence the Owner opened: Rulings 97 and 104; `ProviderRegistry.cs:48-73, 158-187`; `GovernedRunRequest.cs:37-51`; `SessionConfig.cs:16-27`; `ProviderConfiguration.cs:245-279` — which shows a fact the conductor's brief understated: today the account is chosen only by the file's `engines.<id>.account` key or by being the provider's sole account, and an ambiguous case is refused; the operator has no account control at all.*

**RULING:** Make the **account** the operator-facing unit: a session's config becomes a list of account selections plus one default account (engine derived from the provider via the catalog, never stored twice), the composer's decoration line gains a per-turn account picker whose override is an operator row at Send, the New Session sheet lists accounts (not engines) with derived states and a per-provider Configure…, the per-turn binding the run already carries is the fact row and is written to the ledger's `lane.session-new` — one T1 slice "accounts", Sessions/AgentPlane lane, wave 2 with 97(i)+104; Higgsfield is out pending a named spike; spike order becomes **copilot (+enterprise host) → codex → gemini → grok**.

(1) **Model.** *Dimension* `Account` = (provider, label, health, host?) — `host` per 97 condition 3, on the account, never the engine. Identity is (provider, label); the file already holds this shape (`ProviderRow.Accounts`). *Derived:* an account's engine = the catalog row whose `Provider` matches; if a provider ever carries two catalog engines that is a ruling, not a config knob. *Session config:* `EnabledBackends: string[]` (engine ids) is replaced by `Accounts: [{provider, label}]` and `DefaultAccount: {provider, label}`; engine ids are not stored on the session. *Fact grain:* one row is **one turn's binding** (engine, model, account) — `GovernedRunRequest` already carries it; the ledger's `lane.session-new` row must carry all three so the switch is observable, not inferred. *History rule:* Type-2 by construction — a default change is a `SessionConfig` config-changed event applying to new turns only (the clause-3 rule the file already states); no past turn's binding is ever rewritten. *Migration* expand-migrate-contract: an existing `enabledBackends` engine id maps to its provider's account only when the provider has exactly one; otherwise the session opens with "no default account — choose one", never a guessed label. `ProviderConfiguration.Account` (:245-279) becomes the fallback default only; its "never picks one from many" rule stands. *Cut:* per-turn **model** choice — not asked for; model stays the catalog/file default.

(2) **Surfaces.** Sheet row = account, grouped under provider, engine as the sub-line; a provider with zero accounts still shows one row "no account — Configure…" (97's nothing-hidden doctrine). Row state = the weaker of (launch path, account health): *not configured* when the engine's launch is unobserved regardless of health; *needs sign-in* when launch observed and health is needs-login or no account; *ready* otherwise. Configure… per provider = 104's (a)–(e) generalised, each provider's own prerequisite/install/sign-in rows, engine-native only, no embedded browser. Composer decoration: "This turn · account: max ▾" listing the session's accounts, non-ready ones shown disabled with their state (not hidden), defaulting to `DefaultAccount`; an override is an operator row at Send (the Ruling 63/72 shape), no modal. Session settings sheet changes `DefaultAccount` and the account list; new turns only.

(3) **Higgsfield.** Option (b): out of this ruling, **no catalog row, no account row**, until a spike "higgsfield-surface" records what is observed: whether it exposes an API or an MCP server, its auth gesture, and whether a media turn exists at all. Provisional category, marked **Inferred**: a *tool provider* the session offers to whichever engine runs the turn — not an engine, so the operator's "acp=mcp" I read as "the acp-mcp question is the accounts question", which this ruling answers; MCP servers as a session config item stay unruled (Ruling 19 cut them from the Phase-1 sheet; 97 referred it back; the operator's reply did not reinstate it).

(4) **Order.** Copilot first: the operator stated a purpose ("colleagues who may not have a claude account") and a purpose beats a smallest-win. The copilot spike has two acceptance questions, not one: the `copilot --acp` handshake, **and** a run under an identity holding only the Microsoft work account with no Anthropic login on the machine. Then codex, gemini, grok. A grok spike ending "no agent path observed" is a valid end state; the row stays Deferred with that reason.

(5) **Lane, tier, landing.** As proposed: one T1 slice "accounts" (model + store migration + binder + ledger row + sheet + composer picker + settings sheet), Sessions/AgentPlane, wave 2, with 97(i)+104 — the sheet is built once, as account rows. Each engine's spike-then-launch is its own T1 slice after, in the order above. E7 surface list the slice must reach: `providers.json` reader → `SessionConfig`/`SessionConfigStore` (write, read, migration) → `SessionComposerBinder` → `GovernedRunRequest` (unchanged) → ledger `lane.session-new` → sheet VM → composer decoration → session settings sheet → Console reader of the operator row.

(6) **97(i) superseded.** Yes: 97(i)'s row is now an account and its three states apply per account by the rule in (2); 97's engine states survive only as the launch-path input to that rule. 104(1)(e), which writes one account, is unchanged.

**BECAUSE:** The operator named accounts, not engines, and wants per-session switching; `ProviderConfiguration.cs:245-279` shows the product today gives the operator no account choice at all (file key or singleton, else refusal), while `GovernedRunRequest.cs:41-43` and `ProviderRegistry.cs:73,158` show the per-turn triple already exists on the wire — so this is control over a fact the run already carries, not a new fact. `ProviderRegistry.cs:53-57` already refuses a stored engine→provider duplicate (DM7), which forbids storing engine ids beside account refs. Every turn opening its own ACP session (conductor's verified claim, `GovernedRunHost.cs:405, 430`) means per-turn switching adds no continuity cost the product does not already pay.

**CONFIDENCE:** Verified for the model, the wire, the binder's current behaviour, and Rulings 97/104; Inferred for every non-Anthropic engine's auth surface and for Higgsfield's category.

**SCOPE EFFECT:** Admits: slice "accounts" (T1, wave 2, with 97(i)+104); one T1 slice per engine after; spike "higgsfield-surface". Cuts: per-turn model picker; a health prober; an embedded sign-in browser; any Higgsfield row; storing engine ids on the session; a fifth `AcpMode`. Defers: MCP servers as session config (stays with Ruling 19's cut until the operator asks for it by name); Higgsfield's entry. Freezes: (provider, label) as account identity; `providers.json` as the only account store; labels only, never credentials. Reorders 97(iii): copilot → codex → gemini → grok.

**CONDITIONS:**
1. Red-first, `SessionComposerBinder`: a Send with a picker override produces a `GovernedRunRequest` whose `AccountLabel` is the override and an operator row recording the switch; a Send with no override uses `DefaultAccount`.
2. Red-first, `SessionConfigStore`: changing `DefaultAccount` mid-session leaves every prior turn's recorded binding unchanged and applies to the next turn only; the migration from `enabledBackends` maps a singleton-account provider and refuses to guess otherwise (test both).
3. Red-first: the ledger's `lane.session-new` row carries engine, model, and account label; a Console reader test observes them.
4. Red-first: the sheet's state rule — an account with `ready` health under an engine whose launch is unobserved renders *not configured*; a provider with no accounts renders one Configure row.
5. Copilot spike record names: the observed command line, handshake, the enterprise sign-in gesture actually used, and the outcome of a run under a work-account-only identity — "not recorded" for anything not observed. The same shape for codex (ChatGPT login vs API key), gemini (the `--experimental-acp` flag is Inferred until seen), grok (an honest "no agent path observed" is acceptable).
6. Higgsfield spike record names what was observed (API / MCP / neither) before any category is written into the catalog or the sheet.
7. The conductor records, as a measured gap and not a blocker, what context a compiled turn carries across a same-session engine switch; the number is "not recorded" until measured.
8. `ProviderConfiguration`'s `engines.<id>.account` key is documented as fallback default only; the erratum note is amended and marked as extending the spec.

**RECORD AS:** Ruling 105 — the account is the operator-facing unit: session config holds account selections + a default, engine derived from provider; per-turn account picker on the composer's decoration line, override as an operator row at Send; sheet lists accounts with derived states and per-provider Configure…; binding written per turn to the ledger, Type-2 by construction; Higgsfield out pending spike "higgsfield-surface"; spike order copilot → codex → gemini → grok; one T1 slice "accounts", Sessions/AgentPlane, wave 2 with 97(i)+104; 97(i)'s engine states subsumed.


---

## Ruling 106 — Atlas main integration lands as a fast-forward of a gated, both-histories-preserved candidate

*Filed by the conductor verbatim from the Owner's return (2026-09-15, post-reboot). The operator's words, logged verbatim in the GHCP recovery session's audit log (`al-01M2JP02E3EZSKXFNRAK2QBSYA`, 14:02Z): "1: Lets merge and get main up to date with this work 2: Resume the existing native E1 worktree from its passing 26-test checkpoint and finish candidate qualification. make sure we are actively coordinating across sessions I am going to stress test our coordination by running sessions across GHCP, Grok, Claude Code and Codex". The live request: `req-01M2JP0X9RW2CK2E9N0CRS6MSX` (copilot-astra-atlas-recovery → claude-conductor). Rulings 106–111 were returned together; each is filed in its own section.*

**RULING:** Admit the conductor's conditions (a)–(g) and add (h)–(j); the Atlas candidate lands on `main` only when all ten hold.

**BECAUSE:** The operator's grant is verified (`ai-de-conductor-atlas-recovery/docs/audit/audit-log.jsonl:647`) and it authorizes *integration*, not a rewrite; the live request (`.agents/requests.jsonl:15`) itself forbids force, stash, cleanup and an understanding-views merge, and the recovery liveness already binds delegates not to push. The two audit-log copies diverge at line 647, which proves any "ours/theirs" resolution of a ledger would lose an operator prompt.

**CONFIDENCE:** Verified (grant, request, liveness, ledger divergence); Inferred (candidate contents — branch not yet visible).

**SCOPE EFFECT:** Freezes the landing shape. (a) restated as an invariant: at push, `main`'s tip is an ancestor of the candidate and the push is a fast-forward or a merge commit whose first parent is `main`'s tip — direction of the merge is the delegate's choice. (b) `session-contracts.md` resolved by append — §4ac and §9 both survive. (c) append-only ledgers unioned, derived views regenerated, in that order. (d) *[amended in place by the Owner the same hour, on the conductor's finding that `main` is red — Ruling 112]* `tools/run-verify-gates.py` green in the candidate tree **is necessary, not sufficient**; the candidate's Linux portable, Windows App and nonportable suites are run on the same runner shape as CI, and the receipt pasted per (j) **enumerates the failing set by test name**. The candidate lands only if that set is a subset of `main`'s red set at `bab5035e` (the 13 named in Ruling 112). The accounts / first-use oracle / catalog suites from `b6cce995`/`4094ec4c` remain the proof that newer work survived, with the `EngineCatalogTests` ×4 already in the red set noted as such, not as "passed". (e) announced in `.agents/requests.jsonl` (to `claude-conductor`) and in the integrator's liveness *before* the push. (f) fast-forward or merge commit only; no force-push, no history rewrite. (g) the native five-file E1 slice stays out of this candidate. Added: (h) every append-only ledger (`.agents/requests.jsonl`, `.agents/log/*`, `.agents/decisions/*`, `.agents/sessions/*`, `docs/audit/audit-log.jsonl`) is unioned by `tools/merge-append-only-log.py` with the *primary's dirty copy* as one input, then `tools/regenerate-derived.py`, and the union is checked for the presence of both `al-01M2JP02E3…` and `al-01M2JP7RMQ…`; (i) `tools/verify-id-allocators.py` green — 156 commits will have allocated DC/ADR/ruling ids against an older main; (j) the gate receipts named in (d) are pasted into the landing request as observed output, not "green".

**CONDITIONS:** The candidate diff touches no file under `atlas/e1-native-class-view`'s five-file grant (g). If (i) finds an id collision, the candidate renumbers on its side, never `main`. If the candidate's `session-contracts.md` §2 rows conflict with main's §2, that is a seam decision — bring it back, do not choose.

**RECORD AS:** Ruling 106 — Atlas main integration: fast-forward only, both histories preserved, ledgers unioned from the primary's dirty copy, ids and shipped suites re-proven.

---

## Ruling 107 — Claude Code is a coordination-only writer to `main` during the stress test

**RULING:** Confirm: Claude lands only coordination artifacts on `main`, each after fetch-and-merge of the current tip with gates run, announced in its liveness before the push, and never product code.

**BECAUSE:** The operator's instruction (`al-01M2JP7RMQKNWXGF1R7PSN3HY2`) gives Claude no workstream and a watching mandate; the recovery request asks to be told of "any competing main writer or exact shared-seam change", so Claude's own writes must be visible in the same channel it is arbitrating.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Admits: liveness, `request-resolve` lines, ruling notes in `docs/notes/`, audit entries, and Claude's own §-answers appended to `session-contracts.md`. Cuts: any edit to `src/`, `tests/`, `DESIGN.md`, `docs/lessons/defect-classes.md` (the last is a register, but it allocates ids the Atlas candidate is also allocating — leave it until Ruling 106 lands).

**CONDITIONS:** A Claude write to `session-contracts.md` is a shared-seam change under the recovery request's own wording — announce it by a `request-add` naming the section number *before* the push, not by liveness alone. Claude's landings union the primary's dirty ledgers exactly as Ruling 106(h) requires; a coordination push that drops a ledger line is a DataIntegrity trip, and that goes to the human.

**RECORD AS:** Ruling 107 — Claude is a coordination-only main writer: fetch-merge-gate-announce-push, no product code, seam edits announced by request.

---

## Ruling 108 — Two main-bound programmes: the first landing-intent with a candidate SHA holds the slot

**RULING:** First announced lands first, where "announced" means a `request-add` to `claude-conductor` carrying the candidate SHA and the `main` SHA it was gated against; the second re-merges the new main, re-runs gates and re-announces; the Claude conductor holds the order.

**BECAUSE:** Both the recovery request and the Atlas liveness ask for exactly this pre-announcement; a slot held by intent without a gated SHA cannot be verified and would let a programme reserve `main` indefinitely.

**CONFIDENCE:** Verified (request wording); Inferred (Grok's landing readiness — only the N4 PASS claim was seen).

**SCOPE EFFECT:** Freezes the arbitration rule. The recovery request's "no understanding-views merge" binds the *contents of the Atlas candidate*; it does not bar Grok from landing. Symmetrically Grok's candidate must not touch Atlas paths (its liveness says so).

**CONDITIONS:** Same-window tie (both announce before either lands): the conductor picks the candidate whose gate receipt is already on record; if both, the smaller diff against `main` goes first — and the conductor files the tie-break as a decision note. A programme that lands without an announced SHA has landed against Ruling 106(e); the conductor reports it, does not revert (a revert is a history action — Ruling 109's escalation class).

**RECORD AS:** Ruling 108 — main sequencing: a landing intent is a request with candidate and base SHAs; first such lands first; the second re-merges and re-gates.

---

## Ruling 109 — The interrupted `atlas/e1-native-class-view` tree is its owner's alone; cleanup is report-only

**RULING:** Only the GHCP session owning `atlas/e1-native-class-view` resumes it, re-claiming the expired leases before touching the five files; no other session touches, stashes, cleans or removes it; the Claude conductor runs worktree cleanup in report mode only for the stress test's duration.

**BECAUSE:** The tree holds 1 modified + 4 untracked uncommitted files under leases claimed 2026-09-14 21:53 and never released; the operator's grant names this tree ("resume the existing native E1 worktree from its passing 26-test checkpoint") and the recovery liveness says "no cleanup or forced push is authorized".

**CONFIDENCE:** Verified (grant, liveness); Inferred (tree state — reported by the conductor from `git status`, not opened by the Owner).

**SCOPE EFFECT:** Freezes the tree. The owner should first make a WIP commit on its own branch so the uncommitted work exists somewhere; that is not a landing.

**CONDITIONS:** Any proposal to delete this tree, `git clean` it, or discard the five files before they are committed is an irreversible action outside the approved plan — escalate to the human, do not bring it to the Owner. Expired leases re-claimed by *another* identity are a coordination violation the conductor reports in its liveness.

**RECORD AS:** Ruling 109 — the E1 native tree is resumed only by its owner under re-claimed leases; cleanup is report-only during the stress test.

---

## Ruling 110 — An unregistered session has no standing; the answer is "register first"

**RULING:** A session with no liveness file and no `AGENT_SESSION` identity has no standing: its edits are `COORD-NOT-CHECKED`, nothing is merged from it, and the conductor's reply to any such work is "register first", not a refusal of the work.

**BECAUSE:** The recovery liveness records "no current Codex registration was observed" and the conductor observed none; the standard's transport is repository-visible pull, so an identity that never wrote to the repository cannot have been coordinated with.

**CONFIDENCE:** Verified (recovery liveness); Codex absence is "not recorded", never "Codex is idle".

**SCOPE EFFECT:** none beyond the rule.

**CONDITIONS:** Once a Codex liveness file appears, the conductor posts the standing brief (Rulings 106–109) to it by `request-add`; work Codex did before registering becomes eligible only after it re-announces that work with a branch and SHA. Do not route messages to a guessed Codex identity.

**RECORD AS:** Ruling 110 — no liveness plus no identity is no standing; unregistered work is COORD-NOT-CHECKED and the reply is "register first".

---

## Ruling 111 — Two stale Atlas requests are superseded by the grant; the shared-host admission request is not

**RULING:** Resolve `req-01M2B86TXF7SHG61B31P4H4173` and `req-01M2BGHNCM6WRD4ZZMBBFEEB4K` as superseded by the operator's 2026-09-15 grant, pointing at `req-01M2JP0X9RW2CK2E9N0CRS6MSX` as the live thread; keep `req-01M2CAXKH01J8SMQV1…` open and re-route it to the same thread.

**BECAUSE:** Lines 1 and 6 of `.agents/requests.jsonl` ask for authorization and a handoff of main integration — exactly what the grant gives. Line 7 asks for "exact Shell Architecture/factory/menu/host/layout/disposal admission and Core… production signatures" and says explicitly "not implied agreement from CV2/main cleanliness" — a design seam question the grant does not answer.

**CONFIDENCE:** Verified (all three request texts opened).

**SCOPE EFFECT:** Defers the shared-host admission seam to the integration itself: if the Atlas candidate modifies Shell- or Core-owned files named in §2 (`SurfaceContentFactory.cs`, `WorkbenchShell.cs`, `ZoneLayout*`, `WorkspaceClient*`), that is the "exact shared-seam change" the recovery request promised to announce, and the candidate names those files and signatures in its landing request before Ruling 106 applies. *(Conductor's note at filing: the integrator's own request `req-01M2JPF6S81A5EKREZPRTATPW4` (14:10Z) already names `SurfaceContentFactory.cs` and `WorkbenchShell.cs` among six conflicts — this condition is live.)*

**CONDITIONS:** Line 1 also asks for Addendum E reservation and a §9 append; the conductor confirms in the resolution note whether E is registered, and Ruling 106(b) carries the §9 append. If the candidate's `docs/design/code-atlas-shared-host-admission.md` packet is inside the 156 commits with no Shell-owned file changed, `req-01M2CAXKH0…` closes as "answered by the integration"; otherwise it stays open past the landing.

**RECORD AS:** Ruling 111 — req-…B86TXF and req-…BGHNCM superseded by the 2026-09-15 grant; req-…CAXKH0 (shared-host admission signatures) stays open on the live thread.


---

## Ruling 112 — `main` is red: "green" means no new failure and no lost test; one Claude repair lane; the join fails closed on an open `main-red` issue

*Filed by the conductor verbatim from the Owner's return (2026-09-15). The conductor's finding that prompted it, Verified from CI: `main` has been red on every Build run since `ebe18260` (2026-09-12T17:55Z, the last green) — 66 of the last 100 runs failed; the auto-issue `#13 main is red` has been open since 2026-09-12T18:26Z; every join landed 09-13/09-14 through `bab5035e` landed on a red trunk. The red set grew from 1 failing test (`b4e61022`, 09-12T18:06Z: `Conductor.TheGovernedLaneHasNoShellTests.TheFrameTheLaneWasOpenedWithIsRecordedOnTheReportAndInTheLog`) to 13 at `bab5035e` (run 34929322030, read from its `.trx` artifacts): **Core portable (Linux, 5)** — `AgentPlane.EngineCatalogTests.ADirectExecutableWinsOverAnNpmShimEarlierOnPath`, `…AnNpmShimWithoutItsScriptIsRefusedRatherThanHandedToNode`, `…TheNativeLaunchPathResolvesGeminiThroughTheNpmShimToItsScript`, `…AnNpmShimForACliWithNoObservedNpmLaunchIsRefusedRatherThanRunThroughAShell`, `PromptCompilation.PurgeAndTheSessionDeleteCascadeTests.ASiblingHeldOpenRefusesTheDeleteWholeAndNothingIsOrphaned`; **App (Windows, 8)** — `Shell.CodingsLeftExtentTests` ×4 (`CodingsLeftExtent_HoldsThe96chMeasureAtStartupSize`, `…AtEveryViewportTheDisplayGives(1440×900)`, `AtStartupSizeDockedLeftBottomCollapsed_TheThreadHoldsOneTurn_WithTheEditorAt280` ×2 fixtures), `WorkbenchAdapterTests.EveryTab_IsNamedFromItsSurfaceTitle_NotItsTypeName`, `Sessions.TheWriterKeepsItsRoomTests` ×3 (`AtOneAndFortyTurns_TheEditorRestsAt280_WithEqualTopEdge`, `TheCompiledPromptDisclosure_IsOnScreen_AtEveryTurnCount(40)`, `TheCompiledPromptDisclosure_OpenedAtTheOperatorsBelt_RendersTheCompiledText`) — the last four all "the STA thread did not finish within 60s". Core nonportable: 174 executed, 0 failed; the gates job: green.*

**RULING:** (1) A candidate (Atlas or understanding-views) is landable while `main` is red only if its enumerated failing set is a subset of the 13 tests red at `bab5035e`, every one of the 13 still *executes* in the candidate (none deleted, `[Skip]`ped, filtered or renamed), and the receipt names both sets. (2) Amend Ruling 107 by one exception: the Claude conductor opens `lane/main-red-0915` in its own worktree, scoped to the 13 tests and nothing else, registered by liveness and queued under Ruling 108 like any main-bound programme — it does not pre-empt the Atlas landing. (3) Record the 09-13/09-14 landings as a recurrence of the INV-0005 class; the control is a fail-closed check at the join, not another grounding line.

**BECAUSE:** §4ab (`session-contracts.md:2612–2631`, Verified) already told every session to run `gh issue list --label main-red` at grounding on 2026-09-05, and the joins through `bab5035e` did not — so the class recurred *through* a prose control, which is CI6's memoir shape exactly; a second prose line is not a control. The 13-test set is the conductor's reading of the run's `.trx` artifacts (Inferred by the Owner — not opened; Verified by the conductor); nine of the 13 are `AgentPlane`/`PromptCompilation`/`Shell`/`Sessions` paths that §2 assigns to Claude-owned lanes and no other harness, so no other harness can repair them. The operator's grant orders the merge; a red trunk the operator does not know about is a fact the operator must be told, not a reason to silently hold the merge.

**CONFIDENCE:** Verified (§4ab, INV-0005 exists at `docs/investigations/INV-0005-…`); Verified by the conductor / Inferred by the Owner (the 13-test set, the 66/100 count, issue #13 — CI evidence the Owner cannot open).

**SCOPE EFFECT:** Admits the repair lane as the only product-code exception to Ruling 107. For each of the 13: red-first diagnosis classifying *regression* vs *runner-environment* (the four "STA thread did not finish within 60s" and the 1440×900 extent tests are candidates for the latter — that is a hypothesis, not a finding), then a fix, or a quarantine that carries the issue number, the date, the verbatim failure text and keeps the test executing in a non-blocking ring — never a bare skip (Testing-Strategy floor). Defers: the conductor's own §2 attribution of the 13 paths is to be confirmed by opening §2, not inferred from names. Cuts: no other test touched by the lane.

**CONDITIONS:** (i) The lane's **first** deliverable, before any fix, is the diagnosis of `AnNpmShimForACliWithNoObservedNpmLaunchIsRefusedRatherThanRunThroughAShell`, `AnNpmShimWithoutItsScriptIsRefusedRatherThanHandedToNode` and `ASiblingHeldOpenRefusesTheDeleteWholeAndNothingIsOrphaned` — three "no throw" failures on tests whose names assert a refusal. If any is a real regression it is a Security or DataIntegrity floor trip: the conductor pauses the Ruling 108 landing queue and escalates to the human; the Owner does not rule on that. If runner-environment, say what the environment difference is, measured. (ii) The conductor reports "main has been red since 09-12; issue #13 open" to the operator in its next message, before the Atlas landing. (iii) The control: the join tool (`conductor-join.py` / `run-verify-gates.py` at join — Inferred which; open it) runs `gh issue list --label main-red --json number` and refuses to land unless the landing request cites the open issue number and the enumerated red set; `gh` absent or failing degrades to "not recorded" and the join still refuses, with the reason printed. The `defect-classes.md` entry names the class ("landing on red because the signal existed only as a grounding instruction"), cites §4ab as the control that recurred through, and the join check as the replacement; it is allocated by `tools/verify-id-allocators.py` after Ruling 106 lands, per Ruling 107's cut on that register. (iv) Rule (1) applies identically to Grok's candidate; the "13" is re-enumerated from `main`'s new tip after each landing, so the allowed set only shrinks.

**RECORD AS:** Ruling 112 — main is red (13 tests at bab5035e): candidates may not widen or hide the red set; lane/main-red-0915 is Claude's one product-code exception, refusal-test diagnosis first; INV-0005 recurrence controlled by a fail-closed main-red check at the join.


---

## Ruling 113 — Codex gets a branch-local authoring grant on `tools/verify-surface-ownership.py` and its self-test; ownership stays in §2; the gate may populate, never assign

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 14:35Z). Prompted by Codex's registration (`codex-surface-ownership-conductor`, `conductor/surface-ownership` @ `bab5035e`) and its request `req-01M2JQ113TK7HGE7YKQ4CB…`: "Request authoring tools/verify-surface-ownership.py and its existing self-tests only, plus programme proof/audit. Recursive Surface.cs/View.cs population, repository-relative identity, no invented assignments. Section 2 changes only separately agreed." The Owner opened the tool: line 40 fixes `SURFACES = "src/AiDe.App/Workbench"`, line 77 enumerates with `directory.iterdir()` (non-recursive) keyed by `f.name`, line 61's regex matches bare names in §2 tables, lines 24–26 state the gate "cannot decide who should own a new surface".*

**RULING:** Grant `codex-surface-ownership-conductor` a branch-local authoring grant on exactly `tools/verify-surface-ownership.py` and its `--self-test` / `verify-gate-self-tests.py` entry; the §2 `tools/**` row is unchanged, and the grant lapses when the gate lands on `main` or the stress test ends, whichever is first.

**BECAUSE:** The file is Core-owned (`session-contracts.md:119`), unleased and held by no Claude lane, so the grant costs nothing and its landing is reviewed at the join by a Claude conductor that did not author it. The gate's present shape is verifiably wrong for the tree Atlas is bringing: `iterdir()` at line 77 sees only the top level, and identity by `f.name` would make `Workbench/Understanding/AtlasReaderView.cs` and a top-level `AtlasReaderView.cs` one file. Codex's own request forbids invented assignments, which is the gate docstring's rule (lines 24–26) — the request and the file agree.

**CONFIDENCE:** Verified (§2 row, liveness, tool source); Inferred (the Atlas candidate's exact `Understanding/*View.cs` set — 156 commits not opened).

**SCOPE EFFECT:** Admits (a)–(f) as the conductor proposed — (a) no edit to §2 or any product path: a surface found unowned goes into UNASSIGNED with a reason or comes back as a `request-add`, never an invented assignment; (b) identity is the repository-relative path; (c) `--self-test` kept and extended for the recursive case; (d) lands under Rulings 107-style announce, 108 and 112; (e) the Atlas integrator told now; (f) a §2 change is a request to `claude-conductor` and a ruling from the Owner — with two concrete rules added. (g) Matching: a §2 row cited by repository-relative path matches that path only; a §2 row cited by bare name (line 121's `WorkbenchAdapter.cs` shape) matches only if exactly one populated file bears that name — two or more is a gate failure naming both, never a pick. (h) UNASSIGNED entries are keyed by repository-relative path and each carries the request id or ruling number that is meant to retire it. Cuts: no `--recursive` flag or configurable root — recursion is the behaviour, not an option. Defers: any §2 row for Atlas surfaces to a request from the Atlas integrator and a ruling from the Owner.

**CONDITIONS:** (i) Codex claims an identity-bound lease on the file before editing and announces the landing under Ruling 108 with candidate and base SHAs; the landing receipt shows `verify-gate-self-tests.py` green including the new recursive and ambiguity cases, and the self-test count only rises. (ii) Cross-programme order: whichever of the Atlas candidate and this gate lands second re-merges `main` and carries the reconciliation — if the gate is already on `main`, the Atlas landing request carries UNASSIGNED entries (reason and pending-request id) or a §2 request to the Owner; if Atlas is already on `main`, Codex adds those entries itself with the same reason. The conductor sends this to `copilot-atlas-recovery-b0d0` now, by `request-add`, so it is not learned at the join. (iii) The Codex Owner's rulings bind Codex's programme only; a §2 change, a new UNASSIGNED reason that reads as an assignment, or any edit outside the two granted files is a `request-add` to `claude-conductor` and a ruling here. (iv) The conductor resolves `req-01M2JQ113TK7HGE7YKQ4CB…` citing this ruling; silence is not consent, and this is not silence.

**RECORD AS:** Ruling 113 — Codex granted branch-local authoring of tools/verify-surface-ownership.py and its self-test (recursive, repo-relative identity, ambiguity fails, no invented owners); §2 unchanged; grant lapses at landing or end of stress test; Atlas told now.


---

## Ruling 114 — `Sessions/ProseView.cs` is Design's under the §2:165 row; the path cell is amended, no UNASSIGNED exception

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 14:41Z). Prompted by Codex's `req-01M2JQ3T5VMQWJ59Q3M0ZE…`: "Owner inspection found 17 recursive Surface.cs/View.cs files, including Sessions/ProseView.cs absent from section2 path cells. Existing Sessions grouped row is not a directory grant. Please confirm its existing accountable owner and publish exact section2 row, or explicitly authorize a reasoned temporary UNASSIGNED exception. Codex will not infer Design from adjacency." The conductor's evidence: `ProseView.cs` created in `68dc6ab5` (2026-09-13, "feat(thread): the reply side renders the conversation — prose · reasoning · tool call+result · outcome, in event order (Ruling 82)"), touched by `dabd490e` (review cv-5.3); its doc comment cites Ruling 82. The Owner verified the §2 row at line 165 sits under `### Design owns` (line 132) and already cites Ruling 82.*

**RULING:** Amend the §2 row at `session-contracts.md:165` to add `ProseView.cs` to its path cell; the accountable owner is that row's owner, Design; no UNASSIGNED entry for this file.

**BECAUSE:** The row sits under `### Design owns` (line 132) and already cites Ruling 82; `ProseView.cs:9` names itself as the Ruling 82 renderer; the conductor's `git log` shows it created by the same cv-5 lane that authored the row's `ThreadFeed*`. The gap is a path cell written before the file existed, not an unowned surface — Codex was right not to infer from adjacency, and the evidence that decides it is the ruling cited in the file, not the directory.

**CONFIDENCE:** Verified (heading, row, doc comment); Inferred by the Owner / Verified by the conductor (originating commits `68dc6ab5`/`dabd490e` — `git log --diff-filter=A`).

**SCOPE EFFECT:** Admits one §2 landing that covers the whole set of Codex's unnamed files, not one per file. The Owner requires the originating-commit evidence before ruling on the rest: for each unnamed file, the `git log --diff-filter=A` commit and date, the ruling or ADR its doc comment cites (or "none"), and the lane in the commit message. A file whose doc comment cites a ruling already cited by a §2 row goes into that row by this same reasoning; a file citing nothing, or a ruling no row cites, comes back one by one — those may be genuine unowned surfaces and get a dated UNASSIGNED entry naming the request, not a row. Cuts: no new "Sessions/" directory grant — Codex's reading that the grouped row is not a directory grant is correct and stays so.

**CONDITIONS:** (i) The §2 edit lands under Ruling 107 (announced by `request-add` before push, fetch-merge-gate) and Ruling 108's queue; if the full-set evidence has not arrived before the next landing window closes, ProseView's amendment lands alone and the rest follow in a second ruling. (ii) After the landing, Codex's recursive gate must pass on `main` with zero UNASSIGNED under `Workbench/Sessions/` — a remaining entry there is a finding, not a state. (iii) The conductor resolves `req-01M2JQ3T5VMQWJ59Q3M0ZE…` citing this ruling and asks for the evidence table for the other unnamed files in the same reply.

**RECORD AS:** Ruling 114 — Sessions/ProseView.cs added to the §2:165 Design row (Ruling 82 renderer, same lane); one §2 landing for the set; originating-commit and cited-ruling evidence required per remaining unnamed file.


---

## Ruling 115 — A–E acknowledged as exact branch-local carve-outs; no Claude control is touched, only made to pass; the desktop-serialization hold is accepted

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 15:30Z). Prompted by the Atlas recovery closer's `req-01M2JQMKGC16MV3TH3XK04JYPD` (A), `req-01M2JRH3N90RG40BF42F7X08HN` (B–E) and `req-01M2JS1GRH9N99XT5RDKHV86BA` ("Bounded Owner decision now technically approves exact A/B–E scopes … BUT expressly requires responsible current section 2 owner acknowledgment before additional file edits … This is exact permission request, not new normative owner assignment or any gate waiver"), and `req-01M2JSA07TJ4JSHMZF512904NK` (the interactive-desktop serialization hold). The scopes: **A** native writer, `tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs` — MeasureReading 431/442/445 geometry assertion consumes returned-page/source/highlight agreement, failing-first oracle, no product MainWindow/Core change, no assertion weakening; **B** `tests/AiDe.App.Tests/Workbench/PerspectiveMenuTests.cs` — the canonical literal table gains exactly the Architecture-only code-atlas row (accepted `docs/design/code-atlas-shared-host-admission.md` 581–585), literal assertions retained; **C** `src/AiDe.Core/Ipc/AtlasRemoteReader.cs`, `src/AiDe.Core/Understanding/AtlasGitMembership.cs` — two private reader-lease types renamed so the `new Lease(` mint-site guard does not read them as `ComposerCompilationLease`, guard unchanged, no extra allowed site; **D** three `ActivitySource` labels only (`AtlasRemoteReader.cs`, `AtlasDirectoryEnumerator.cs`, `AtlasQueryService.cs`) `AiDe.` → `aide.`; **E** `tests/AiDe.App.Tests/TheWebSurfacesInitialiseOnceAcrossReparentsTests.cs` — Loaded guard reconciled with the accepted host contract (644–648, TRACKED per-attach activate/deactivate) with companion negative controls, no blanket exception or one-time-init loophole. The Owner verified: `src/AiDe.Core/**` is Core's (`session-contracts.md:86`); `tests/AiDe.App.Tests/**` follows the file under test (`:175`), so B follows `PerspectiveMenu.cs` (Core, `:129`); the mint-site guard is `tests/AiDe.App.Tests/Composer/TheSendGateSendsWhatProjectionProjectsTests.cs:265–275` (a Security C4 oracle scanning `new Lease(` by name at exactly two sites); PrivacyMarkerTests enforces the `aide.` prefix at `:102`.*

**RULING:** Acknowledge scopes A–E as exact branch-local carve-outs (A on the native tree under Ruling 109's owner; B–E on `atlas/main-integration`) with the conductor's conditions (1)–(6) binding — (1) no assertion weakened, no guard changed, no allowlist grown: each of B–E is proven by the existing Claude test/gate going green in the candidate with its own source untouched; (2) A is red-first: the failing oracle is committed before the fix, and the diff touches only the coordinate-frame computation, never the expected values; (3) E's companion negative controls are new tests and stay in the candidate; (4) C's renamed private types are named for what they are and the mint-site guard's allowed set is unchanged — if the guard cannot pass without a new allowed site, that is a request back to the Owner; (5) all five land only inside the Ruling 106/112 contract; (6) ownership unchanged, rows reconciled per Ruling 113(ii) — plus (7)–(9) below; accept the interactive-desktop serialization hold for the stress test.

**BECAUSE:** This is the §2-owner acknowledgment the Astra Owner requires, and every path is Claude's to acknowledge (`:86`, `:129`, `:175`). C and D are the candidate being made to pass two Claude controls — the Security C4 mint-site oracle (`TheSendGateSendsWhatProjectionProjectsTests.cs:265–275`) and the `aide.` privacy prefix (`PrivacyMarkerTests.cs:102`) — with the control files outside the carve-out, which is exactly what Rulings 106 and 111 asked for. None of A–E asks to weaken an assertion, grow an allowlist or add an allowed mint site; the day any of them does, it stops being a carve-out.

**CONFIDENCE:** Verified (§2 rows, the two control files); Inferred (the candidate's diffs — not opened; the "App 1109/5, nonportable 344/1" recount — no names; E's file under test not identified — its owner is Claude either way).

**SCOPE EFFECT:** Admits A–E. Added: (7) C's renamed types must not derive from, wrap, or convert to the composer's `Lease`; the diff shows a type-declaration rename and its construction sites only — disambiguation, not evasion; a guard that then scans by type rather than by name is a Core next step, not today's. (8) D changes string literals only; because these sources are not yet on `main`, no telemetry consumer changes meaning. (9) E's reconciliation names the observation that distinguishes TRACKED per-attach activate/deactivate from one-time init — an observed count or event order, not a type or file exception list. Cuts: nothing beyond the named files; no product `MainWindow`/Core change under A; no new allowed site under C — that comes back as a request. Defers: §2 rows or UNASSIGNED entries for the Atlas files under `src/AiDe.Core/Ipc/`, `src/AiDe.Core/Understanding/`, `tests/…/Understanding/` to Ruling 113(ii), ruled when the integrator brings them.

**CONDITIONS:** (i) The checkpoint recount "App 5 failed, nonportable 1 failed" is not a Ruling 112 receipt until the names are enumerated — the landing request carries them, and each is either in `main`'s red set at `663c3a80` or a new failure the candidate must fix. (ii) Each of B–E is proven by the existing Claude test going green in the candidate with its own source unchanged in the diff; A's failing oracle is a separate earlier commit. (iii) The desktop hold: one shown-window/UIA run at a time across all programmes, start and end announced with PID in `.agents/requests.jsonl`; Claude runs none today; `lane/main-red-0915`, if opened, announces before any App test run — and records whether the four "STA thread did not finish within 60s" failures reproduce under the hold, since concurrent UIA on one desktop is a candidate cause (hypothesis, not finding). (iv) The conductor resolves the three requests citing this ruling; "no response is not consent" is honoured by the resolution, not by silence.

**RECORD AS:** Ruling 115 — Atlas carve-outs A–E acknowledged (native proof-test coordinate frame; PerspectiveMenu row; private lease-type renames; aide. ActivitySource labels; Loaded lifecycle reconciliation with negative controls); no Claude control changed; enumerated names required; desktop serialization hold accepted.


---

## Ruling 116 — D's one coupled listener follows the emitter; `AtlasReaderView.cs` gets a Design row now, extended per accepted slice

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 16:50Z). Prompted by the Atlas recovery closer's `req-01M2JW19S4BPNBT1V9PJJVGSNX`: executing scope D (Ruling 115) turned three previously-green tests red in `tests/AiDe.Core.Tests/Understanding/AtlasDirectoryEnumeratorTests.cs` (`EnumerateAsync_Limits_ReportActualDimensionAndBoundedTelemetry` entries/depth/descriptors) because line 296 selects `source.Name == "AiDe.Core.Understanding.AtlasDirectoryEnumerator"` while the acknowledged emitter is now `aide.…`; and the accepted integration candidate adds exactly one surface file, `src/AiDe.App/Workbench/Understanding/AtlasReaderView.cs` (`public sealed class AtlasReaderView : UserControl`, "the production constructor consumes a Core-owned reader lease"), which the recursive gate landed at `33e9ae7e` will name. Both verified by the Owner on `C:\Projects\ai-de-atlas-main-integration`; no §2 row names any `Understanding/*View.cs` on `main`. Decision note for condition (ii): the conductor ran the landed `verify-surface-ownership.py` on a tree carrying the row without the file — `OK — 17 surface(s), 17 assigned` — so the row lands before the Atlas candidate, by the Claude conductor under Ruling 107.*

**RULING:** (1) Admit `tests/AiDe.Core.Tests/Understanding/AtlasDirectoryEnumeratorTests.cs:296` as D's coupled consumer — the string literal's prefix changes, nothing else in the file. (2) Add a Design-table row for `src/AiDe.App/Workbench/Understanding/AtlasReaderView.cs`, worded as proposed; no UNASSIGNED entry. (3) `AtlasStaticView.cs` joins the same row's path cell in the ruling that accepts the native E1 slice — one ruling per accepted slice, never one per file; `AtlasStaticViewProjection.cs` is not a surface by the gate's own regex and needs no row.

**BECAUSE:** The three red tests are the proof that emitter and listener are one contract — Ruling 115(8)'s "no consumer changes meaning" was about consumers on `main`, and this consumer is inside the candidate, so it must follow or the rename is half-done. `AtlasReaderView` is a WPF `UserControl` over a Core-owned lease, which is the `SearchSurface.cs` shape at `:158` ("Design authored it; Core owns its provider"), so its owner is decidable now by the ProseView reasoning; a dated UNASSIGNED entry for a decidable file would be the gate recording a decision nobody was refusing to make.

**CONFIDENCE:** Verified (the selector, the class header, the absence of a row); Verified by the conductor (the landed gate tolerates a row naming a file not yet on the tree — run, exit 0).

**SCOPE EFFECT:** Admits the fourth D file and one §2 row. Freezes the row's author note: "authored by the Atlas fleet under a branch-local grant" stays in the Why cell so the register never reads as a fleet ownership.

**CONDITIONS:** (i) The diff on `AtlasDirectoryEnumeratorTests.cs` is one line and the three tests are green in the receipt with their expected bounds unchanged. (ii) The row lands under Ruling 107 before the Atlas candidate **only if** the conductor first runs the landed `verify-surface-ownership.py` on a tree carrying the row without the file and it passes; if the gate refuses a row for an absent file, the same row text lands inside the Atlas candidate's §2 append under Ruling 106(b) instead — the wording does not change, only the landing. Report which happened in the decision note. (iii) The conductor resolves `req-01M2JW19S4BPNBT1V9PJJVGSNX` citing this ruling.

**RECORD AS:** Ruling 116 — D's listener selector at AtlasDirectoryEnumeratorTests.cs:296 follows the aide. rename (one literal); AtlasReaderView.cs gets a Design row (Core lease behind it), landed before Atlas if the recursive gate tolerates an absent file, else with it; AtlasStaticView.cs joins the row at slice acceptance.


---

## Ruling 117 — `main`-red: no floor trip on the shipped platform; nine deterministic reds are platform-scoped honestly, four intermittent are quarantined executing; the join closes on the landed SHA's CI result

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 17:12Z). The operator chose option B (diagnose read-only now, fix after the Atlas landing). The diagnosis is `docs/investigations/INV-0012-…`: all 50 Build runs on `main` since the last green swept from their `.trx` artifacts; the 13 red tests at `bab5035e` are 9 deterministic reds each born red at a join (CV-2 `c59eac66`, sh4-2 `3e5b04f6`, engines `9f2044bc`) and 4 intermittent STA timeouts; all 13 pass on a real Windows desktop (38/38 Core, 32/32 App under the desktop hold). The Owner verified the locator (`EngineCatalog.cs:147, :162, :172–174`), the purge design (`PurgeAndTheSessionDeleteCascadeTests.cs:180–181`), `join.json:22`, and caught one false claim in the conductor's proposal: `FakePath.AddExecutable` already calls `MarkExecutable` (`EngineCatalogTests.cs:486`), so the fixture is truthful and the proposed fixture change is cut.*

**RULING:** (a) None of the three refusal tests is a floor trip: on Windows all three invariants hold (local 38/38 and 32/32 under the desktop hold); the Linux behaviours of `ResolveLaunch` (an executable shim is the launch, cannot be refused) and of the delete cascade (no refusal while a sibling holds a file; unlink under the open reader) are **recorded residuals** whose trigger is "Core or the daemon runs off Windows". (b) Per group, in `lane/main-red-0915` after the Atlas landing, as amended below. (c) Order as proposed: EngineCatalog ×4, then Purge, then the eight App tests.

**BECAUSE:** Every deterministic red was born red at a join and never green — a test that never passed on that runner is a wrong-runner test, not a regression; the mechanisms are opened in code, not modeled. The fixture is already truthful (`:486`), so the only honest fix for the four locator tests is to say what they already say in their doc comments — they are Windows scenarios. The purge invariant as written *is* Windows sharing semantics (`:180–181`); making it hold on Linux is a store-semantics change, which is not the smallest correct thing for a lane scoped to thirteen tests.

**CONFIDENCE:** Verified (locator, fixture, purge design, join.json filter); Inferred (that the product ships on Windows only today — the App is WPF, but the Owner did not verify no Linux daemon deployment; if one exists, the purge residual is a DataIntegrity trip and goes to the human, not to this lane).

**SCOPE EFFECT:** Group 1 — `[Trait("Platform","Windows")]` on the four locator tests **plus one portable characterisation test** that states the Linux truth in its name and carries the residual and its trigger in its doc comment, so the residual is a test and not a memoir (CI6); the fixture change is **cut**. Group 2 — `Platform=Windows` on the purge test with the same residual shape; the explicit-lease design ("refusal by design on every OS") is a next step routed to the Data & Persistence Architect as a request, not authored in this lane. Group 3 — the test prints font family and `PixelsPerDip` on the normal path first (a measurement the next CI run records); then pin the fixture font, declared in place, if the runner lacks the UI font, else quarantine per Ruling 112. Group 4 — Ruling 112's quarantine shape (executing, non-blocking ring, issue number, verbatim failure); retry-with-recording refused as proposed. The ~30 one-off UI failures across 50 runs are recorded as a class ("the hosted runner's App suite is flaky as a population") with that measurement; re-ringing the App suite is a next step for the SRE and Test Architect, not this lane.

**CONDITIONS:** (i) Controls under 112(iii): the join's closing audit entry carries the landed SHA's Build run id and result, and a join is not closed while that field reads "not recorded" — a red result reopens it; this is the control that would have caught all three births, and it needs no Linux shape locally. A local Linux run of the portable half (WSL or container) is admitted as a next step, not required. (ii) The lane records, as a defect-class instance, that a mechanism claim ("`AddExecutable` only writes the file") reached the Owner unverified while the file it described was open — the diagnosis was measured, the fix proposal was not. (iii) Each group lands as its own commit with red-first evidence for the characterisation test; nothing outside the 13 plus that one new test is touched.

**RECORD AS:** Ruling 117 — main-red diagnosis: no floor trip on Windows; locator ×4 and purge → Platform=Windows with residuals as tests (fixture change cut, already truthful); extent ×4 measured then pinned or quarantined; STA ×4 quarantined executing; join closes only on the landed SHA's recorded CI result.


---

## Ruling 118 — AI-DE v1's supported platform is Windows, by recorded authority; the Linux no-throws are residuals with no live instance; 112(i) closes for the Atlas landing

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 17:55Z). Prompted by the Atlas recovery closer's `req-01M2JZNPSJTYKRXE2DFXACN8QX` (its Security lens: "Need current Core/Design Owner authoritative SUPPORTED PLATFORM scope … If explicit Windows-only, cite authority … do not invent restriction"). The conductor located the authority and the Owner verified it on `main` `1bcf63f4`: `docs/adr/0008-shell-host.md:67` — "Windows-only remains a v1 boundary."; `src/AiDe.App/AiDe.App.csproj:5` and `src/AiDe.Daemon/AiDe.Daemon.csproj:11` target `net10.0-windows`; `src/AiDe.Core/AiDe.Core.csproj:4` and `src/AiDe.Mcp/AiDe.Mcp.csproj:20` target `net10.0`; a census of `SessionConfigStore|ResolveLaunch|NativeCommandLocator` across `src/` returns 18 files, all in `AiDe.Core` or `AiDe.App` — none in `AiDe.Daemon` or `AiDe.Mcp`.*

**RULING:** (a)–(e) as the conductor proposed, confirmed: the supported platform for v1 is Windows, cited from ADR-0008:67 and the `net10.0-windows` targets of both session-hosting processes; `AiDe.Core`'s `net10.0` is a library property kept honest by the Linux CI half, not a deployment; the held-owner refusal and the shim refusals are Windows invariants of a Windows-only v1; Ruling 112(i) closes for the Atlas landing with no floor trip and no waiver, the 13 remaining the enumerated baseline under 112(1). Amend (c): the tripwire is a **reference-census gate**, not the characterisation tests.

**BECAUSE:** The authority exists and is recorded — an accepted ADR consequence, two csproj lines, and a caller census showing no portable process reaches `SessionConfigStore.Delete`, `EnvelopePurge` or `ResolveLaunch` — so nothing is invented. Ruling 117's characterisation tests document the Linux behaviour but *pass* on Linux; they cannot fire the day `AiDe.Mcp` or a new portable executable starts calling those members, so calling them the tripwire would be a memoir with a test's name (CI6).

**CONFIDENCE:** Verified (ADR line, four target lines, the `src/` census); Inferred (that the Atlas Security lens's "no actual unsafe process run" finding is complete — their observation, not the Owner's).

**SCOPE EFFECT:** Admits one small stdlib gate into `lane/main-red-0915`'s scope, with self-test: fail if any project whose `TargetFramework` lacks `-windows` and whose `OutputType` is an executable references `EnvelopePurge`, `SessionConfigStore.Delete` or `ResolveLaunch`; today it passes on the census above. ADR-0008's follow-ups gain one line naming the residual, its trigger ("a portable process hosts the store or the launcher"), and the gate. Cuts: no platform-behaviour change to the store or the locator now.

**CONDITIONS:** (i) The resolution to `req-01M2JZNPSJTYKRXE2DFXACN8QX` quotes ADR-0008:67 and the four `TargetFramework` lines verbatim, with paths and line numbers, and states the census result ("18 files, all `AiDe.Core`/`AiDe.App`"). (ii) `req-01M2JYV89X…` closes with the same answer **only if** its text asks the platform-scope question — it asked for existing evidence and was resolved with that at 16:57Z; this ruling supplements it. (iii) If a Linux-hosted daemon or MCP process that calls either member is ever proposed, the purge residual is a DataIntegrity question for the human before the design, not after.

**RECORD AS:** Ruling 118 — v1 supported platform is Windows (ADR-0008:67; App and Daemon net10.0-windows; no portable process calls the purge or the launcher); Linux no-throws are recorded residuals with a reference-census gate as the tripwire; 112(i) closed for the Atlas landing.


---

## Ruling 119 — Codex gets a branch-local grant on `tools/verify-audit-log.py` (adding `--self-test`) and the one-line shrink of the frozen list; policy unchanged; valid fixtures minted by the pack's allocator

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 18:05Z). Prompted by Codex's `req-01M2K3PZY3J1DQY7HZN443E81C` (session `codex-audit-gate-conductor`, branch `conductor/audit-gate-self-test` from `bbd1bece`): "prove existing audit-log verifier policy with deterministic self-test in temporary logs/repositories. Request branch-local grant for exactly tools/verify-audit-log.py and removal of its frozen entry from tools/verify-gate-self-tests.py … No allocator, merge framework, live fixture data, product or policy changes." The Owner verified on `main`: the frozen list at `tools/verify-gate-self-tests.py:46–57` ("THIS LIST MAY ONLY SHRINK"; the only accepted edit is removing a name when the gate gains a self-test, detected by the argparse declaration at `:37`); `verify-audit-log.py` validates ids with its own regex at `:64` and imports nothing from the pack; `merge-append-only-log.py:79–88` imports `audit-log.py`'s `next_id` by `importlib` precisely because the two tools once disagreed.*

**RULING:** Grant `conductor/audit-gate-self-test` branch-local authoring of exactly `tools/verify-audit-log.py` (adding `--self-test`) and the removal of `"verify-audit-log.py"` from `KNOWN_WITHOUT_SELF_TEST` in `tools/verify-gate-self-tests.py`, in one commit; §2 unchanged; conditions (a)–(d) as the conductor proposed — (a) the verifier's policy unchanged: the self-test proves what the gate already refuses (duplicate ids, deleted lines, malformed lines, missing/invalid ids) and accepts (legacy `al-NNNN`, ULID-form, plain appends) against temporary logs/repositories only, never the live `docs/audit/*`; (b) red-first per planted defect, the ratchet green with the entry removed; (c) lands under 108 and 112 (enumerated failing set empty and said so), joined by the Claude conductor; (d) identity-bound lease before editing, no lease on append-only or derived artifacts — plus (e) below; the grant lapses at landing or the end of the stress test.

**BECAUSE:** This is Ruling 113's shape on a file with the same ownership (`tools/**`, Core, §2:119), unleased and unheld; the frozen list's own rule makes the paired edit mandatory, not optional. The self-test proves the gate's existing refusals and acceptances in temporary logs, which adds a control to a gate that today has none — the ratchet's stated debt shrinking by one.

**CONFIDENCE:** Verified (frozen list and its rule, the verifier's regex-only validation, the merge tool's allocator import).

**SCOPE EFFECT:** Admits the two files. (e) The self-test's **valid** fixtures — the ULID-form ids it expects the gate to accept — are minted by importing `docs/ai-forward-pack/scripts/audit-log.py`'s `next_id` exactly as `merge-append-only-log.py:79–88` does, so the fixture cannot drift from what the allocator actually mints; the **planted defects** are hand-written literals; the verifier's own regex at `:64` is not replaced by an allocator import — the gate must keep refusing without the pack present, and the self-test is where the two are reconciled. Cuts: no change to `audit-log.py`, `coord_ids.py`, the merge tool, or any live `docs/audit/*` (Codex's own exclusions, confirmed).

**CONDITIONS:** (i) A diff hunk touching any refusal or acceptance branch of the verifier outside the `--self-test` code path is a request back to the Owner, not a carve-out. (ii) Red-first evidence per planted defect (duplicate id, deleted line, malformed line, missing id, invalid id) and per accepted shape (legacy `al-NNNN`, ULID, plain append) is in the landing receipt as observed output; `verify-gate-self-tests.py` is green with the entry removed and its count only rises. (iii) Lands under Rulings 107-style announcement, 108 and 112 (docs/tools only, so the enumerated failing set is empty and says so), joined by the Claude conductor; identity-bound lease before editing; no lease on append-only or derived artifacts. (iv) The conductor resolves `req-01M2K3PZY3J1DQY7HZN443E81C` citing this ruling.

**RECORD AS:** Ruling 119 — Codex granted branch-local authoring of tools/verify-audit-log.py --self-test plus the frozen-list shrink, one commit; policy unchanged; valid ULID fixtures minted via audit-log.py's next_id, defects hand-planted; lapses at landing or end of stress test.


---

## Ruling 120 — Native E1: "qualified at checkpoint, not accepted for main"; it is the second Atlas landing candidate under 108/112; Codex's E1/E2 assignment needs no §2 change until files exist

*Filed by the conductor verbatim from the Owner's return (2026-09-15, 20:10Z). Prompted by the Atlas recovery closer's `req-01M2KARRFRSMB6QMATHCJ46YGF`: the native five-file candidate at `atlas/e1-native-class-view` `46e2f266` (product `4d506ce8`) reports retained-Test NFT8 PASS, 115/115 current guard cohort, 764 hashed inputs with zero mismatch, all mutation/disclosure criteria, UML bounded PASS, C# advisory PASS, UX bounded component PASS, the D&P callback block cleared — "full platform / universal SRE / combined A-R / shipping not claimed" — and asks the Owner to record the bounded acceptance/disposition and Ruling 116's `AtlasStaticView.cs` row timing, and to carry the operator's assignment of the remaining E1 Sequence/Activity and E2 domain/layer/Azure views to Codex through the existing plan and §2 manifest. Decision line, verbatim as the Owner set it: **the native five-file slice at `atlas/e1-native-class-view` `46e2f266` is qualified at checkpoint, not accepted for main.***

**RULING:** (1) Record the native five-file slice at `atlas/e1-native-class-view` `46e2f266` as **qualified at checkpoint, not accepted for main**; it lands, if it lands, as a second Atlas candidate after the accepted integration, with its own Ruling 108 intent (candidate and base SHAs, enumerated failing set against the then-current red set) under Ruling 112, joined by the Claude conductor; the `AtlasStaticView.cs` extension of the `AtlasReaderView.cs` Design row lands with that landing, not now. The Owner does not accept it on the receipts now. (2) No §2 change for Codex's E1/E2 lanes now; rows or UNASSIGNED entries arrive with files under 113(ii); the seam rule and the Atlas closer's sole-writer and desktop-schedule roles stand; Codex lands after Atlas under 108; the assignment is carried in `docs/coordination/code-atlas.md` and Codex's programme docs, not the rulings register.

**BECAUSE:** The receipts (NFT8 PASS, 115/115 guard cohort, zero hash mismatch, UML/C#/UX bounded PASS) are the closer's report — neither the conductor nor the Owner opened them, and a report is evidence, not acceptance; acceptance for `main` is what Ruling 106/112's landing contract *is*, so the slice earns it the same way the accepted integration is earning it, not by a ruling in advance. Ruling 116(3) already fixed the row timing to "the ruling that accepts the slice", and that ruling is the landing, so nothing here changes 116. Codex's lanes hold no surface files yet; a §2 row for a file that does not exist is the shape 116(ii) had to condition on the gate tolerating.

**CONFIDENCE:** Verified (Rulings 106(g), 108, 112, 113(ii), 116(3) as filed; the operator's 14:29Z separation as relayed by the conductor); Inferred (every qualification receipt; that `46e2f266` carries only the five files — the landing intent must say so).

**SCOPE EFFECT:** Freezes the record's wording: the disposition implies nothing about platform breadth, combined A-R, the owned-HWND diagnostic or shipping — "not claimed" stays "not claimed". For Codex: no `src/AiDe.App/Workbench/Understanding/`, `Core/Ipc`, `SurfaceContentFactory.cs`, `WorkbenchShell.cs`, `PerspectiveMenuTests.cs` authoring without a request agreed with the Atlas integrator; no E3/E4, no live Azure, no source execution — as asked.

**CONDITIONS:** (i) The native landing intent enumerates its five files and asserts no other path is touched; a sixth file is a new seam request. (ii) Its receipt is produced under the desktop hold with the runs on the ledger, as today's were. (iii) Codex's first request to author a surface file under `Understanding/` carries the file's intended §2 row text or an UNASSIGNED reason, so 113(ii) is met at the join, not discovered there. (iv) The conductor resolves `req-01M2KARRFRSMB6QMATHCJ46YGF` citing this ruling and copies the disposition line into the decision note verbatim.

**RECORD AS:** Ruling 120 — native E1 slice at 46e2f266 is "qualified at checkpoint, not accepted for main"; second Atlas landing under 108/112 with the AtlasStaticView row at that landing; Codex E1/E2 assignment carried in plan docs, §2 rows arrive with files.
