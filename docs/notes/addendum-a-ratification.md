---
id: note-addendum-a-ratification
title: "Decision note — Addendum A ratified with cuts; Rulings 15–18"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, addendum-a, ratification, scope-change, session, codemirror, react]
links:
  - { to: note-addendum-a-reconciliation, rel: depends-on }
  - { to: note-conductor-phase1-e18-close, rel: refines }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Addendum A ratified as a scope change, with three cuts: the Phase-1 canvas ships Console
  and Terminal only, the mention picker sources files and graph nodes only, and R14 is a path
  contract rather than a store. React is refused; CodeMirror needs a spike. Phase 1 re-opens
  under amended exit evidence while its Agent Plane close stands.
---

# Decision note — Addendum A ratified with cuts; Rulings 15–18

**Ruled by:** Owner agent, 2026-09-09. Confidence **Verified** — it grepped and read every
file the rulings turn on before ruling, and corrected two things in the material it was given.

## The scope change is admitted, with three cuts

**Ratified**, so the amended goal block stands: Phase 1's exit evidence becomes *"a real
governed run started from File → New Session, composed in the rich composer, streamed in
Console mode, scored end-to-end by the existing Watcher, with zero terminal hosting"*, and
acceptance gains **R13, R14, R15, R16**.

**Cut, because the addendum assigns the requirements to Phase 1 without saying which parts
have anything behind them yet:**

1. **The canvas ships Console and Terminal only.** Artifacts, Profiler and Board register as
   modes when their panes land (Phases 3, 3, 2). *A tab with nothing behind it is dead UI.*
2. **The mention picker sources files and graph nodes only.** Artifact and block sources
   register with their providers. The conductor-drafted goal-block reply is Phase 2 — it needs
   `ConductorHost`.
3. **R14 bullet 1 is a path CONTRACT, not a store.** `session.yaml` lands, the run-log path is
   reserved, and nothing writes a run log anywhere else. `RunLogStore` stays Phase 3 per §12,
   which A8 does not change. Console renders the **in-process** event stream the Phase-1 exit
   run already produced (287 events, p50 0.022 ms) without a persisted log.

### Conditions

- **(a)** Modes and picker sources are **data-driven registrations**, never a hard-coded
  five-tab strip with placeholders.
- **(b)** **CodeMirror 6 is an unfamiliar dependency — the Spike Protocol runs before R15
  depends on it.**
- **(c)** **React is REFUSED.** Main has no React; one composer is one implementer; the
  existing WebView2 host suffices. Re-entry trigger: *a second web surface needing shared
  component state.* **Marked plainly as extending the spec, not reading it** — A5 asserts the
  surface "is already WebView2 + React per spec §3", and that premise is false on main.
- **(d)** "Zero terminal hosting" stays measured on the exit run's `terminalHostConstructions`
  counter. **The Terminal mode existing is not a violation; a terminal being constructed
  during the exit run is.**
- **(e)** R16's "permission within one event cycle" is proven by a **red-first test with a
  synthetic permission event.** The exit run may raise none — Phase 1's seven were all
  lease-approved.

## Ruling 15 — the two type renames happen now

**Amended by Ruling 15a** (`note-addendum-a-ruling-15a-governed-episode`): `GovernedSession`'s
target is `GovernedEpisode`, not `GovernedLane` — `GovernedLane` was already the name of the
pre-existing episode+worktree composite in the same file/namespace, unseen when this ruling was
written. `GovernedLaneSource` below is unaffected.

`GovernedSessionSource` → **`GovernedLaneSource`**, `GovernedSession` → ~~**`GovernedLane`**~~
**`GovernedEpisode`** (see amendment above),
**including the consumer** at `GovernedRunHost.cs:114`. The Watcher-side callee
(`IngestHost.OpenEpisode`, `agent_session_dim`) stays session-named until touched.

A3 defines Lane as *"one agent's participation in a run — ACP-governed or terminal-observed"*,
which is exactly what `GovernedSession` is. The boundary confusion is the one A3 explicitly
accepts, and it is **the same boundary whether the rename happens now or later — only cheaper
now.** v1.0's use of the old name in §6.2/§10/§11 is superseded by A3 and goes in the errata
note.

**Conditions:** the rename carries R14's lint/doc note — a comment at the `GovernedLane` →
`IngestHost` boundary stating the callee's "session" is the Watcher's lane-identity sense,
pending opportunistic migration. **`GovernedLane` must not grow an interface** — Ruling 7's
refusal of `ILane` is untouched because this is a concrete type.

## Ruling 16 — Phase 1 re-opens; its Agent Plane close stands

The **E18 close stands unaltered** as the signed close of the *Agent Plane* delivery. **Phase 1
as a spec phase re-opens** under its amended exit evidence, and R13–R16 form a second delivery
under Phase 1, named by the spec's own words: **"Phase 1 — Session front door."**

*Rewriting the existing close to `Superseded` would make the audit trail say Phase 1 was never
`Completed` when its four clauses were, on evidence the Owner opened.* So the close gains **one
header line, not a rewrite.**

This is **not** the ownerless "Phase 1b" that Ruling 14 retired — the container is Phase 1,
which §12 owns. Ruling 14's re-entry trigger (*"the first time a run must be watched by a
human rather than read from the store"*) is now **fired** by the addendum and retired from
Planned-vs-actual.

**Condition:** the front-door slice gets **its own plan-approval ruling before its first code
node** — it is not authorised by this ruling alone. Ruling 13's condition carries verbatim: the
session document calls **the same composition root** `GovernedRunHost` uses. No second entry
point.

## Ruling 17 — Phase 2 N0 continues; Phase 2 code waits

**N0 continues to close unchanged.** It holds no Phase 2 code — only the DC-115 control, the
curated docs and the profiler, all of which are Phase 1 debt. And the DC-115 control now
**also gates the front-door exit run**, which will be a scored governed run and must not be
scored in a linked worktree until the control exists.

**Phase 2 N1 and every later Phase 2 code node wait behind the front-door E18.**
`ConductorHost` and the session document both rewire the same composition root, so **the
coupling test fails and they do not run in parallel.**

**Order: N0 → rename node → front-door slice → Phase 2 N1.** No Phase 2 node is cut.

**Conditions:** N0 **absorbs nothing** from the addendum (CT22) — the rename lands after N0
closes, and N0's `/document` half is written against the current name and migrates as touched
code. If the front-door exit run roots in a clone rather than a linked worktree, **the DC-115
qualification is carried exactly as Phase 1 carried it, never silently.** A `Not Scored`
verdict under the linked-worktree shape remains an **EvaluatorIntegrity trip that goes to the
human.**

## Ruling 18 — the surface-kind collision, and a false premise in the addendum

The session document kind is **`"session-document"`**, never `"session"` — which would sit one
letter from the existing `"sessions"` and is a defect signature.

**`"sessions"` (the Watcher lane pane) stays untouched:** it is used as `restorableKinds` in
`WorkbenchShell.cs:366,635`, so renaming it is a **saved-layout migration, not a rename**, and
that is outside this change order. A re-entry trigger is recorded instead.

**A5's premise is contradicted by main.** It states the surface "is already WebView2 + React".
WebView2 hosting exists; **React and CodeMirror do not** — no `package.json` in the repo names
either. **R15 therefore carries a new web dependency, not a reuse**, which is why (b) and (c)
above exist.

**Under-specified, to be resolved at front-door plan approval rather than invented now:**
R13's inline engine-native login is provable only against `claude-code` in Phase 1 (codex and
copilot are refused by N2's own test) — *state that in the exit evidence; do not stub two
engines* · A2's "accumulates blocks over days" is R13's draft/mode/layout restore only, not
console history, because there is no run log · A6's canvas split is proven with **Console
beside Terminal**, the only two modes with content.

## Before the front-door slice may start

The Owner requires, and this ruling does **not** grant: the slice's node list **with the
CodeMirror spike result**, the composer's E7 surface list (store → `SessionConfig` →
`NewSessionSheetViewModel` → `SurfaceContentFactory` → `session.yaml` → Recent list), the
red-first tests per R13–R16 bullet, and **a budget estimated from the N4 / N5+N6 / N7 measured
durations.**
