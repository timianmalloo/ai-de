---
id: note-addendum-a-ruling-15a-governed-episode
title: "Decision note — Ruling 15a: GovernedSession → GovernedEpisode (GovernedLane was already taken)"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, addendum-a, ruling-15, naming, agent-plane]
links:
  - { to: note-addendum-a-ratification, rel: refines }
  - { to: note-addendum-a-reconciliation, rel: relates-to }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Ruling 15 ordered GovernedSession -> GovernedLane, but src/AiDe.Core/AgentPlane/
  GovernedSessionSource.cs already has an unrelated concrete class named GovernedLane
  (the episode+worktree teardown composite) in the same namespace and file - a straight
  rename would not compile (CS0101). Amended: GovernedSession -> GovernedEpisode instead;
  the composite keeps the name GovernedLane untouched.
---

# Decision note — Ruling 15a: `GovernedSession` → `GovernedEpisode`

**Ruled by:** Owner agent (delegated CT20 authority), 2026-09-09. Confidence: **Verified** —
the Owner opened `src/AiDe.Core/AgentPlane/GovernedSessionSource.cs` (both classes) and Ruling
15's text before ruling.

## The collision Ruling 15 was issued without seeing

`src/AiDe.Core/AgentPlane/GovernedSessionSource.cs` already declares, in the **same namespace**
(`AiDe.Core.AgentPlane`) and the **same file**, a distinct concrete class:

```
/// <summary>One governed lane's episode and worktree, torn down together.</summary>
public sealed class GovernedLane
```

(`GovernedSessionSource.cs:190` at the time of this note.) It is constructed as
`new GovernedLane(session, provisioner, worktree, seams)` in
`src/AiDe.App/Conductor/GovernedRunHost.cs:169` and in three test files
(`GovernedSessionSourceTests.cs`, `LeasesRaiseSeamsTests.cs`).

Ruling 15 (`note-addendum-a-ratification`) ordered `GovernedSession` → `GovernedLane`. Executed
literally, this produces **two types named `GovernedLane` in one namespace** — `CS0101`, will
not compile. Neither `note-addendum-a-ratification` nor `note-addendum-a-reconciliation`
mentions the pre-existing composite; Ruling 15's line 69 claim that `GovernedSession` "is
exactly" the A3 Lane was made without it in view.

## Ruling

**Amend Ruling 15**: rename `GovernedSessionSource` → `GovernedLaneSource` as ordered
(unaffected — it collides with nothing). Rename `GovernedSession` → **`GovernedEpisode`**, not
`GovernedLane`. Leave the pre-existing `GovernedLane` composite untouched — name, constructor
parameter names, and the `SessionId` property it and `GovernedEpisode` both carry (Watcher
vocabulary, out of scope per A3's opportunistic-migration rule).

**Because**: the existing `GovernedLane` — episode + worktree + lease seams, owned and torn
down together — is the thing A3's Lane definition ("one agent's participation in a run") names;
moving that name would trade a correct name for a worse one plus churn across
`GovernedRunHost.cs:169` and every test constructor. `GovernedSession` (documented as "one open
governed episode, and the only object that can close it", exposing `EpisodeId` /
`DeclareArtifacts` / `Close`) is named correctly by `GovernedEpisode` — it removes "session"
from the `AgentPlane` vocabulary (the confusion A3 targets) and collides with nothing.
`GovernedLaneSource` stands because it is the lane's entry point (takes `LaneIdentity`,
registers the lane, opens its episode).

## Conditions

1. No interface is introduced on either renamed type — Ruling 7's `ILane` refusal stands, and
   `GovernedEpisode` gets no `IEpisode`.
2. The R14 lint/doc note Ruling 15 required at the rename boundary is placed on
   `GovernedEpisode`'s `SessionId` property, noting the Watcher's `SessionId` vocabulary is
   pending opportunistic migration, not touched now.
3. Test count before == after; zero behaviour change.
4. The spec errata entry for §6.2/§10/§11 records both new names (`GovernedLaneSource`,
   `GovernedEpisode`), not `GovernedLane`.

## Scope effect

**Amends** Ruling 15's rename target for `GovernedSession` only (`GovernedLaneSource` stands
unchanged). **Admits** nothing new: two type renames, one file rename
(`GovernedSessionSource.cs` → `GovernedLaneSource.cs`). **Freezes**: the `GovernedLane`
composite (name, ctor params, `SessionId` property) and everything under `Watcher/`, `Dispatch/`,
`Terminal/`. No follow-on ticket — the composite needs none.
