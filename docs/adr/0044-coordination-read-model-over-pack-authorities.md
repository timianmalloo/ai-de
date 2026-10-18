---
id: adr-0044-coordination-read-model-over-pack-authorities
title: "ADR-0044 — Coordination is a read model over the pack's own authorities; commands go through the qualified runner; no C# coordination database"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [architecture, coordination, read-model, ai-forward-pack, conductor, watcher, workbench-loops]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: kb-workbench-loops-20261018, rel: depends-on }
  - { to: adr-0023-watcher-observation-projection, rel: refines }
  - { to: architecture-agent-plane, rel: refines }
  - { to: architecture-loomkeeper, rel: relates-to }
  - { to: adr-0039-workbench-records-durable-representation, rel: relates-to }
review-by: 2027-04-18
summary: >-
  The coordination workspace projects the AI-Forward pack's git refs, request/mail/ruling stores,
  runner manifest and board — plus the Loomkeeper watcher's observations — into one Core read model
  with per-source freshness, relationship from the manifest (never from timestamps), and presence,
  progress and outcome as separate dimensions. Dispatch, enqueue, finish, cancel and join previews
  invoke the pack's runner and join scripts with a compiled audit id; AI-DE never writes a ref or
  mints a leader, ruling or acknowledgment. Projection ships before any command.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# ADR-0044: Coordination — a read model over pack authorities

- **Status:** Accepted — owner ruling 2026-10-18 (@timianmalloo); council record follows. Previously Proposed (council gate 2026-10-18)
- **Date:** 2026-10-18
- **Deciders (Peer Mode):** Enterprise Architect (author), Distributed Systems Architect, Security &
  Identity Architect, SRE
- **Context spec/architecture:** spec US-C01–C02, AC-C02b (owned child vs registered peer), NFR-11;
  research note §Current AI-Forward coordination (source `83a7834`, revision 112); proposal §Bring
  current AI-Forward coordination into the workbench; pack doctrine `agent-coordination.md`
  (CO1–CO17, CO-L).

## Context

The pack is the authority for coordination: the leader is designated by a Git CAS ref with epoch
fencing (`coord-core.py:888`); requests follow sent → received → acked → resolved/expired with an
ACK pinning a blob and rulings as separate artifacts (`coord-core.py:631, 695`); the runner
dispatches from a compiled audit id and a qualified manifest (`coord-runner.py:346, 394`); recovery
never replays interrupted work (`coord-runner.py:871, 1036`); presence is not progress
(`coord-core.py:3772`); usage is absent by default. The installed runner, board, decision helper
and doctrine are byte-identical to source 112 although the manifest says 102, and AI-DE's locally
enhanced `conductor-join.py` durable step recovery must survive pack updates (research note,
Verified by the codex session; the files exist under `docs/ai-forward-pack/scripts/`, inventory
2026-10-18).

AI-DE today has **no** C# reader of `.agents/` or the join/runner state (inventory): the only
coordination read model is the Loomkeeper watcher (`WatcherSessionsQuery`, `FleetAggregator`,
`LivenessProjection`), whose `Alive / Stale / Ended` derives from heartbeat age
(`LivenessProjection.cs:25-29`) and cannot stand in for progress; `RunEvent.ParentAgentId` is
always null (`AcpRunEventMapper.cs:126`); `RunTriage` has stages with no producers. The bridges
that do exist are `AGENT_SESSION` / `AIDE_*` env (`WorkbenchShell.cs:3246-3266`), `coord install`
in a provisioned worktree (`WorktreeProvisioner.cs:194`) and the `loomkeeper/1` contract log
(`<data>/loomkeeper-coord`). The direction note rejects "a second C# leader/request authority".

LOA principles: P2 (deterministic fold), P5 (observed state over claims), P8 (commands idempotent
by the runner's own keys), P11 (the runner's authority, not AI-DE's).

## Decision

1. **A `CoordinationReadModel` in Core** folds, per workspace: the leader ref and epoch; the
   runner manifest (task graph with the five-part contract: objective, artifacts, allowed tools,
   bounds/budgets, termination); claims/seams; requests, ACKs (with the pinned blob and whether it
   is the current artifact), rulings (numbered), overdue decisions; join state and interrupted
   attempts; the board; and the watcher's observations. Every source carries its own
   `observed_at` and a **Not checked** state when unread; nothing is inferred across sources.
2. **Relationship comes from the manifest, never from time or vendor.** `native fleet child`,
   `cross-harness spawned worker` and `independently registered peer` are read from the runner's
   dispatch records and the registrar; `ParentAgentId` is populated **only** for children the
   runner dispatched (the manifest is the parent's witness). A heartbeat with no manifest edge is
   a registered peer, full stop.
3. **Three separate dimensions on every row:** presence (heartbeat freshness), progress (observed
   deltas: artifacts declared, run events, join steps), outcome (the runner's recorded result).
   "Fresh but stalled" is a legal display state (AC-C01b). Stages are **transport complete →
   evidence present → review ready → accepted → integrated**, each from its own witness; the
   workbench never collapses them (AC-C02c).
4. **Commands invoke the pack, with the pack's keys; projection never runs code.** The read model
   is **pure file and git reads**: canonical paths confined under `.agents/`, session ids validated
   by the runner's own rule (`coord-runner.py:189-191`), `git` invoked as argv with
   `--end-of-options` and ref names passed through `git check-ref-format`; no script, no shell,
   no `artifacts.yml` regen command is executed to *display* anything. Dispatch preview and
   dispatch, enqueue, finish, cancel and join preview call `coord-runner.py` / `conductor-join.py`
   / `coord-core.py` by **absolute interpreter and absolute script path from app configuration**,
   sha-checked against the installed pack revision before each invocation (the verify-bundle
   control) — the `coord` PATH shim (`CoordCommand = "coord"`, `ConductorEntry.cs:131`) is not used
   for workbench commands — with the compiled audit id and the session card; AI-DE
   never writes `.agents/` records or refs directly and never mints a leader, ruling or ACK
   (AC-C01c). Agent-authored text (mail, request bodies, manifest objectives, board) renders with
   an "agent-authored, untrusted" origin label, byte-bounded, and never inside a consent control's
   accessible name or description (AC-A01a). **Accepted residual (single-user local tool):** when
   the checked-out tree is a third-party PR head, the pack scripts in `docs/ai-forward-pack/` and
   `.agents/artifacts.yml` are that head's files; AI-DE does not sandbox them. The read model
   records "HEAD is not a user-authored local ref" as a trust fact and shows it beside the
   command controls; the user's own authority decides. An owned child's Stop is the
   runner's process control; a peer's Stop is a *request* through mail — two different buttons
   (AC-C02b). Timeout, notification or a model-seat ruling is never rendered as human consent.
5. **Projection ships before commands.** The read-only graph, requests and recovery views land
   first; qualified commands follow only with the runner's own evidence (stale ACK, leader epoch,
   stalled progress, interrupted join fixtures).
6. **Coordination rows are not workbench records.** They are not written to the workspace store
   (ADR-0039); the read model may keep a labelled, rebuildable cache keyed by source fingerprint.

## Alternatives considered

- **Port `coord-core` to C# and own the state:** rejected — a second authority diverging from the
  protocol other harnesses (Copilot, Codex, Muse) already speak; the doctrine's invariants live in
  the scripts and their tests.
- **Derive parentage from heartbeat timing or model names:** rejected by the spec (AC-C01a) and by
  the watcher's own documented limit.
- **Use `LivenessProjection` as progress:** rejected — presence ≠ progress (`coord-core.py:3772`).
- **Invent a `coord delegate` command:** rejected — the doctrine names it but no parser implements
  it (research note); the runner manifest contract is the real one.

## Consequences

- **Positive:** one truthful graph across native fleets, spawned workers and registered peers;
  the pack remains upgradable because AI-DE holds no competing schema.
- **Negative / accepted trade-offs:** reading `.agents/` and refs is file/git polling with its own
  freshness; the read model must be explicit about what it last read and when; some pack states
  have no C# type yet and will render as raw-with-label until typed.
- **Follow-ups / new risks:** a pack update must not overwrite the enhanced `conductor-join.py`
  (a verify-bundle/updatepack control); the read model's git access is read-only and must be
  tested against a dirty tree and a missing ref.

## Evidence

- Pack scripts present (`conductor-join.py` 750 lines, `coord-runner.py` 1,208, `coord-core.py`
  5,540) and the C# absence of any `.agents/` reader — **Verified** inventory 2026-10-18.
- `LivenessProjection.cs:25-29`, `RunEvent.cs:150-165`, `AcpRunEventMapper.cs:126`,
  `WorktreeProvisioner.cs:194`, `WorkshellShell` identity env — **Verified**.
- Protocol facts at `83a7834` — **Verified by the codex session's source read** (research note);
  re-read before the slice that depends on each line.
