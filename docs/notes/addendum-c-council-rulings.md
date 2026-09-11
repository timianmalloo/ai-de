---
id: note-addendum-c-council-rulings
title: "Decision note — Rulings 50–63: Addendum C's vocabulary, phasing, ADR-0017, the graph substrate, the 80% case, page one, and the operator's composer verdicts filed"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, ruling, conductor, addendum-c, perspective, ui, docking, explorer]
links:
  - { to: plan-addendum-c-modes, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: spec-knowledge-explorer-mode, rel: relates-to }
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: refines }
review-by: 2026-12-11
summary: >-
  Rulings 50–55 issued before any Addendum C spec was written; 56–62 issued at the spec's gate —
  56 and 57 file the operator's own composer verdicts, 58–62 rule on the reconciliation table.
  Six rulings the Owner issued before any Addendum C spec was written, on the evidence the conductor
  brought at node R0 of plan-addendum-c-modes. They fix the vocabulary (Perspective), the phasing
  (F5 untouched; code after F5 merges), ADR-0017's fate (retained and amended), the graph model (one
  substrate, two surfaces), the build order under the operator's 80% case, and four page-one facts a
  spec written without them would get wrong.
---

# Decision note — Rulings 50–63

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
