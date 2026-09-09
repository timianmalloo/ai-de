---
id: note-conductor-episode-source-seam
title: "Decision note — spec §6.2 'same source seam' read as the live IngestHost path"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, watcher, episode-source, scoring, simplification]
links:
  - { to: spec-conductor, rel: refines }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-09
summary: >-
  The spec says GovernedSessionSource "implements the same source seam as
  AuditLogEpisodeSource", but no such seam exists — AuditLogEpisodeSource is a static
  batch importer. The Owner ruled the phrase is intent, not an instruction: the governed
  source enters through the live IngestHost path, and no IEpisodeSource interface is
  invented in Phase 1.
---

# Decision note — spec §6.2 "same source seam" read as the live `IngestHost` path

**Ruled by:** Owner agent (delegated CT20 authority), 2026-09-09. Confidence: **Verified** —
the Owner grepped `src/` and opened the cited call sites.

## Question

Spec §6.2 states: *"`GovernedSessionSource` implements the same source seam as
`AuditLogEpisodeSource`."* §10 lists `GovernedSessionSource` under `AiDe.Core/Watcher`.

**No such seam exists.** Verified in `src/`:

- There is **no `IEpisodeSource` interface** anywhere.
- `AuditLogEpisodeSource` is a `public static class` (`AuditLogEpisodeSource.cs:17`), called
  only from `WatcherHost.cs:95,126` as a **batch reader**. It implements nothing.
- `OtlpHttpReceiver` produces no episodes at all; it feeds spans to `IngestHost.Enqueue`.
- The **live** episode verbs are `IngestHost.OpenEpisode` / `CloseEpisode` /
  `DeclareEpisodeArtifacts` (`IngestHost.cs:217,227,249`), driven by
  `CoordinationContract.cs:487,548,550`.
- `ClosedEpisodeScoring.cs:22,89` deliberately **skips episodes with no `SessionRecord`**.

So the spec's §6.1 table pairs governed lanes with a *runtime emitter* and observed lanes with
`AuditLogEpisodeSource` "exactly as today" — a live source and a batch source. The two are
different shapes **by design**, which is itself evidence the §6.2 phrase is loose.

## Ruling

Read §6.2 as **intent**. `GovernedSessionSource` enters through the live path: register a
session, then `IngestHost.OpenEpisode` / `DeclareEpisodeArtifacts` / `CloseEpisode` under a
capability from `ITrustedRegistrar` — landing in the same store and swept by the **unchanged**
`ClosedEpisodeScoring`.

**Do not invent an `IEpisodeSource` interface in Phase 1.**

## Because

A governed source that registers a session is the **only shape the existing sweep scores**
(`ClosedEpisodeScoring` skips sessionless episodes). That is precisely the spec's own sentence
in §10 realised: *"Nothing in `ScoringService`, `WeaveScore`, `Leaderboard` partitioning,
`MessageBoard`, `ProofPackVerifier`, or `EgressGate` changes semantics; they gain callers."*

Inventing an interface over a batch importer and a live streaming source would modify working
code to satisfy a phrase, and would produce an abstraction with two implementers of genuinely
different shapes — the Simplifier's standing objection.

## Scope effect

Cuts the `IEpisodeSource` retrofit from Phase 1. Defers a shared interface until a **third**
implementer exists (`BenchImportSource`, spec §10) and then only as a refactor, never as a
phase obligation.

## Conditions on this ruling

- Before design closes, verify **how audit-imported episodes are scored today**
  (`WatcherHost.cs:95-140`). The Owner read the call sites, not the bodies, and marked
  "they score at import" as **Inferred**. If observed and governed episodes reach
  `ScoringService` by two different entry points, that is a **finding to record**, not a
  reason to reopen this ruling.
- `mode` is added as a **cohort attribute on open**. It must not appear in any partition key —
  `weave/1` is unchanged and out of scope.
