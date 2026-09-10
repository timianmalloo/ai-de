---
id: plan-conductor-front-door
title: "Execution graph — Phase 1, Session front door (R13–R16)"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "1"
tags: [execution-graph, conductor, addendum-a, session, composer, canvas, front-door]
links:
  - { to: note-addendum-a-ratification, rel: depends-on }
  - { to: note-addendum-a-reconciliation, rel: depends-on }
  - { to: plan-conductor-programme, rel: refines }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-10
summary: >-
  The second Phase-1 delivery: Addendum A's front door, as six mostly serial nodes. Carries
  the CodeMirror spike result and the one decision it left open - how the web surface is
  hosted - plus the E7 surface list, red-first tests per acceptance bullet, and a budget
  derived from Phase 1's three measured node durations.
---

# Execution graph — Phase 1, Session front door

**Status: awaiting Owner plan-approval.** Ruling 16 requires this slice to have its own
approval ruling before its first code node; it is not authorised by the ratification alone.

## Goal state

- **Goal:** deliver Addendum A's front door — File → New Session, the session document, the
  composer, and the output canvas — as the second Phase-1 delivery.
- **Done when:** *"a real governed run started from File → New Session, composed in the rich
  composer, streamed in Console mode, scored end-to-end by the existing Watcher, with zero
  terminal hosting"*; R13–R16 pass as tests under the ratification's cuts; suite green on main;
  Owner signs the front-door E18.
- **Not in scope:** R17 Artifacts viewer (Phase 3) · Artifacts/Profiler/Board canvas modes ·
  artifact and block mention sources · the conductor-drafted goal-block reply (needs
  `ConductorHost`) · `RunLogStore` (Phase 3) · any big-bang rename of Watcher session
  vocabulary · React.
- **Tier:** T2 · **Width:** 2 at the head, 1 thereafter (justified below).

## The CodeMirror spike result, and the one decision it left open

`spikes/codemirror-composer/` — **YES, with a named cost.** Verified by execution in real
Chromium: plain `new EditorView` with **zero React imports**; markdown live-edit; a fenced JS
block fully highlighted; the `@mention` chip a **genuine `Decoration.replace` + `WidgetType`**
(a real widget replacing source text, not styled text); read-only a first-class facet. MIT
across all 52 packages.

**The cost is hosting, not the editor**, and it is now decidable — I verified both halves:

- `CanvasSurface.cs:211` loads content with **`NavigateToString`**, which **cannot serve
  import-map modules at all**.
- WebView2 **1.0.3485.44** ships `SetVirtualHostNameToFolderMapping`; **nothing in `src/` uses
  it yet**.

| Option | Cost | Risk |
| --- | --- | --- |
| **(a)** Virtual host + generated import map | No bundler; a **second hand-maintained manifest** | The spike measured this: one missing transitive package = **hard runtime failure, no build-time warning** |
| **(b)** Minimal bundler (esbuild, no config) | Introduces a JS toolchain to a repo with **no `package.json` at root, ever** | A build step .NET developers must know about |
| **(c)** Vendor one pre-built ESM bundle, committed | **Zero toolchain**, one file, re-fetchable by a recorded command | A vendored blob; updates are deliberate rather than automatic |

**Recommendation: (c), served over a virtual host.** It matches this repo's existing idiom —
the pack vendors scripts, spikes commit evidence — and it removes the failure the spike actually
measured, which is the second manifest, not the module count. **This is a decision for the Owner
at plan approval, not mine**, and (a) is the honest fallback if a vendored blob is judged
unauditable.

## The nodes

`F0 ∥ F1 → F2 → F3 → F4 → F5`

| Node | Goal | Tier | Model | Est. |
| --- | --- | --- | --- | --- |
| **F0** | Session object + path contract (R14) | T1 | sonnet | ~1400 s |
| **F1** | Web host decision executed + shell | T2 | opus | ~1900 s |
| **F2** | New Session sheet + File menu (R13) | T1 | sonnet | ~1900 s |
| **F3** | Session document + canvas modes (R16) | T1 | sonnet | ~1900 s |
| **F4** | Composer (R15) | T2 | **opus** | ~1900 s |
| **F5** | Exit evidence + Proof Pack | T2 | **opus** | ~2400 s |

**Width 2 only at the head, and only because GO5 genuinely holds there.** F0 is C# session
config and on-disk paths; F1 is WebView2 hosting and a vendored web asset. No shared authored
file, no shared derived surface, and — applying the lesson N0 taught — **separate worktrees**.
Everything after F2 contends on `WorkbenchShell.cs`, `SurfaceContentFactory.cs` and
`MainMenuBuilder.cs`, so it is serial and says so.

### Exit conditions — each names what would make it fail

**F0 — session object and path contract (R14).**
- `session.yaml` is written at `.aide/sessions/<session-id>/session.yaml`; the run-log path
  `.aide/sessions/<session-id>/runs/<run-id>.jsonl` is **reserved and asserted unused** —
  nothing writes a run log anywhere, which is the ratification's cut (`RunLogStore` is Phase 3).
- Backend toggles apply to **new runs only** and emit a session event.
- New event kinds `session.open` / `session.config` ride the **open `kind` string** — no schema
  change, which N1's ruling already bought.
  *Fails if:* anything writes a run log, or a toggle retroactively changes a prior run.

**F1 — web host.**
- The chosen option (Owner's ruling) is executed, and a CodeMirror instance **renders inside the
  real WebView2 host**, not only in headless Chromium.
- **`NavigateToString` remains for the existing canvas** — this must not regress `CanvasSurface`.
  *Fails if:* the existing canvas stops rendering, or the editor works only outside WebView2.

**F2 — New Session sheet + File menu (R13).**
- `Ctrl+N` with an active workspace opens the sheet **pre-bound**; with none, the **workspace
  chooser interposes and cancel aborts cleanly**.
- Agent backends listed **from `EngineCatalog` filtered by `ProviderRegistry` state**, with live
  per-account health — these already exist and must be *read*, not re-modelled.
- Create writes `session.yaml`, opens the document, registers in **Recent sessions**.
- **`File → New Terminal Session` still produces today's terminal session, unchanged** —
  asserted, not assumed.
  *Fails if:* a session can exist unbound to a workspace, or the terminal path changes behaviour.

**F3 — session document + canvas modes (R16).**
- Surface kind is **`"session-document"`** (Ruling 18) — never `"session"`, which sits one letter
  from the existing `"sessions"` and is a defect signature.
- **Console and Terminal only**, registered **data-driven** (Owner condition a) — not a
  hard-coded strip with placeholders.
- **ADR-0017 retain-never-rebuild proven against a LIVE LANE**, the way the ADR does it — mode
  switch and tab switch must not dispose the surface or any lane. *Inspection is not proof.*
- **A permission request surfaces within one event cycle regardless of active mode**, proven
  **red-first with a synthetic permission event** (Owner condition e — the exit run may raise
  none; Phase 1's seven were all lease-approved).
  *Fails if:* unparenting disposes a lane, or a permission is hostage to the selected tab.

**F4 — composer (R15).**
- Markdown-live editing, highlighted fences, and **@-mention chips that materialise a recipe
  line** — files and graph nodes only (ratification cut).
- **Message and Goal-block shapes**; a **T2 goal block missing a CT19 field cannot send**, with a
  field-level error — the same six-field parameterised test shape N3 used, not one omit-case.
- **US-ED5/ED6/ED7 hold**: never sent by editing · one explicit send · draft persists across
  restart · the same draft **transfers one-way to a ready observed lane**.
- **One text stack** — the Source viewer, when it lands, uses this same editor.
  *Fails if:* editing sends, a draft is lost across restart, or the transfer path regresses.

**F5 — exit evidence + Proof Pack.**
The amended done-when, as a falsifiable floor:
1. The run is **started from File → New Session** — not constructed in a test.
2. **Composed in the composer** and **streamed in Console mode**.
3. **Scored end-to-end**, cell `IsComparable == true`.
4. **`terminalHostConstructions == 0`** — and note Owner condition (d): *the Terminal mode
   existing is not a violation; a terminal constructed during the run is.*
5. Launched through **the same composition root `GovernedRunHost` uses** — no second entry point
   (Ruling 13's condition, carried verbatim).
6. **Proof Pack at `docs/proof/conductor-front-door.md`** with **every Residual cell populated**.
7. **DC-115's condition holds:** if the run roots in a clone rather than a linked worktree, the
   qualification is carried **exactly as Phase 1 carried it, never silently**.

## E7 surface list

`session.yaml` (store) → `SessionConfig` → `NewSessionSheetViewModel` → `MainMenuBuilder`
(File → New Session) → `SurfaceContentFactory` (`"session-document"`) → `WorkbenchShell`
(paired-zone placement, `restorableKinds`) → web host → composer → `RunEvent` (`session.open`,
`session.config`) → `GovernedRunHost` (the one composition root) → Recent sessions.

**UI row: present, not deferred.** Phase 1's Agent Plane delivery marked it "deferred to 1b";
this slice is where it lands, so the row is now live rather than carried.

## Budget, derived from measurement rather than carried

Phase 1's measured node durations: **N4 1931 s · N5+N6 1374 s · N7 2404 s** (N0–N3 are *not
recorded*). Front-door estimate ≈ **11,400 s of node time**, sized by shape: F4 is N4-shaped (a
novel dependency), F5 is N7-shaped (evidence), the rest sit near N5+N6.

**Main-line budget: 90 tool calls.** Derived from Phase 1's *measured* per-node conductor cost
(dispatch + independent verification + commit ≈ 8–10) across 6 nodes, plus ~25 for converge and
close, plus contingency. **Phase 1 declared 300 and passed it; Phase 2's N0 ran ~55 against 120.**
The estimate is now built from the shape that actually recurred, not from the front-loaded total.

**The number to plan against, measured at N7: verification cost 2404 s against a 102 s exit run —
23:1.** That is not waste; it is what evidence-not-assertion costs. F5 is budgeted for it.

## Standing constraints carried into every node

Full gate set at **every** node close (the by-subject policy let four gates go unrun for eight
nodes) · gates run **bare**, never piped before `&&` (DC-113) · never `git stash` (DC-053) ·
never `verify-test-run.py --update` as a gate · **no `coord install` in a worktree** (DC-112) ·
`audit-log.py start` first and an entry **with signals** at close (`verify-audit-capture` is a
gate) · **separate worktrees for concurrent nodes** (N0's lesson) · every load-bearing repo fact
in a brief either checked in the same turn or labelled unverified with an instruction to check
(**DC-116**, three instances in one session, all caught by the delegate).

## Re-plan checkpoints

1. **The Owner's hosting ruling** — (a), (b) or (c) changes F1 entirely.
2. **After F1** — if CodeMirror does not render inside the real WebView2 host, the composer's
   base is wrong and R15 re-plans before F4 starts.
3. **DC-115** — still `partially-controlled`; if F5's run must be scored in a linked worktree,
   its remaining cases decide whether that is possible.
