---
id: "note-20260902-session-enlistment-telemetry-gap"
title: "Sessions register but do not enlist with telemetry — the harness/model/spans/liveness gap is Core's, not the App's"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "watcher"
tags: [decision-note, watcher, loomkeeper, sessions, coordination]
links:
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-01
review-suggested: []
summary: >-
  A running agent session shows in the Sessions surface but as a generic ended terminal
  (harness/model Not Recorded, trust Asserted, 0 spans, liveness Ended/Stale). The App threads the
  harness into the coordination identity correctly; the missing model/spans/Verified/Alive is
  harness-telemetry the watcher substrate must receive and record — Core's domain.
---

# Sessions register but do not enlist with telemetry — the gap is Core's harness telemetry, not the App identity wiring

*A decision note (`knowledge-visualization.md` V17): below ADR weight. Written to hand the Core
`feature/agent-watcher-substrate` session a precise, code-grounded starting point, and to record why
the Design session did **not** change App code for it.*

- **Kind:** resolved-question
- **Confidence:** Verified (App-side identity wiring read from source); Inferred (the Core/harness
  emission path, from the observed "Not Recorded / 0 spans / Ended" symptom — not yet traced in the
  watcher store code)
- **Made during:** a Design-session investigation of the user's question *"I can't tell if they are
  enlisted in collaboration with Loomkeeper"* (smoke screenshot, TheTerrace workspace)

## The symptom
Every row in the Sessions surface reads **`Terminal · <repo>/<branch> · Not Recorded · Not Recorded ·
trust Asserted · 0 span(s)`** and is **`× Ended`** (one `~ Stale`) — including while a Claude Code and
a GitHub Copilot session are running. So sessions **register** (they appear) but do not **enlist**:
no harness identity, no model, trust only *Asserted* (never *Verified*), zero spans, and liveness that
never reads *Alive* for a live agent.

## The call — where the gap is, and is not
**The App-side identity wiring is correct and is not the gap.**
`WorkbenchShell.IdentityFor(surfaceId, terminals)` threads the harness into the coordination identity:
it looks up `_harnessBySurface[surfaceId]` (set at `WorkbenchShell.cs:200` when a terminal is launched
*as* an agent, harness chosen not discovered) and returns
`new SessionCoordinationIdentity(..., AgentName: agent, Harness: profile?.HarnessId)`. When the surface
was launched as an agent, the harness **is** present in the identity; a plain terminal correctly has
`Harness: null` ("Not Recorded" is the honest form). The many ended `Terminal · Not Recorded` rows in
the screenshot are **historical plain terminals** from prior runs in that workspace — expected, not a
bug. `SessionRowPresenter` already renders this honestly ("the agent harness isn't emitting telemetry").

**The real gap is in the watcher substrate / harness telemetry — Core's domain.** These four are *not*
carried by the App identity and must be reported back by the running harness and recorded by the store:
1. **Model identity** — the model (e.g. "Opus 4.8") is chosen *inside* the agent; the App cannot know
   it. It must arrive via the harness's shell-integration telemetry into the session record.
2. **Spans** (`0 span(s)`) — `IWatcherObservationStore.TryAppendSpan` is how observed activity lands;
   nothing is appending spans for these sessions, so `SpanCount` stays 0.
3. **Trust `Verified`** — the session is only ever *Asserted*; the OSC-133 nonce / signed readiness
   evidence that would upgrade it to *Verified* (`SessionReadiness`, `ReadinessEvidence`) is not being
   received or recorded.
4. **Liveness `Alive`** — running agents read `Ended`/`Stale`. Liveness is driven by
   `IWatcherObservationStore.UpsertHeartbeat` / `LastHeartbeat` and `LivenessProjection.Evaluate`;
   a live agent must heartbeat for its session to read Alive.

**Suggested Core starting points** (Inferred — trace before acting, `end-to-end-integrity.md` E15):
`WatcherObservationStore` (span/heartbeat/session recording), `LivenessProjection`, `SessionReadiness`
/ `ReadinessEvidence` (Asserted→Verified), and the harness shell-integration that should emit the
model + spans + heartbeats back over the ConPTY. Confirm on the *fixed* launch build (agents now
launch — 902a01d/DC-084), because a freshly-launched agent session is the one that should enlist fully;
the ended historical rows never will.

## Alternatives dismissed
- **Change App `IdentityFor` to guess the model/harness from the executable name** — rejected: the
  code comment and US-13 explicitly forbid guessing identity from the executable ("Absent stays
  absent — never a guess"). Absent is the honest form; the fix is real telemetry, not a guess.
- **Design session fixes the watcher store** — rejected: the store, liveness, readiness and the
  harness emission path are the Core `feature/agent-watcher-substrate` session's owned domain; the
  Design session owns App/UI surfaces and the surface already renders the truth.

## Validation condition
Holds until the Core session either confirms the emission path or the fixed-launch build shows a
freshly-started agent session enlisting with harness + model + spans + Alive. When a real agent
session reads `Verified · N span(s) · Alive` with its harness and model, retire this note.

## Promotion rule
If closing this becomes a multi-artifact change (store schema + harness integration + readiness), it
earns an ADR; write it, link `supersedes` this note, set this note `superseded`.
