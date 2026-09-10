---
id: note-conductor-spec-errata-lane-rename
title: "Spec erratum — v1.0 §6.2/§10/§11 name GovernedSessionSource; superseded by Ruling 15/15a"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, spec, errata, addendum-a, ruling-15, naming, agent-plane]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: note-addendum-a-ratification, rel: relates-to }
  - { to: note-addendum-a-ruling-15a-governed-episode, rel: relates-to }
  - { to: note-conductor-spec-errata-policy, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Spec v1.0 names GovernedSessionSource in three places (§6.2, §10, §11). Addendum A3 / Ruling
  15 (amended by Ruling 15a) supersedes the name: GovernedSessionSource -> GovernedLaneSource,
  and the type it returned, GovernedSession, -> GovernedEpisode. The spec HTML stays byte-frozen;
  this note is the correction, per the errata policy.
---

# Spec erratum — v1.0 §6.2/§10/§11 name `GovernedSessionSource`; superseded by Ruling 15/15a

**Filed by:** lane-rename session, 2026-09-10. Confidence: **Verified** — each quoted line was
read directly from `docs/specs/conductor/ai-de-conductor-spec-v1.html` at its cited line number.

Per `note-conductor-spec-errata-policy`: the spec HTML stays byte-intact; this note is the
correction, linked from `docs/specs/conductor/README.md`.

## What changed

Addendum A3 (`docs/notes/addendum-a-ratification.md`, Ruling 15) renames `GovernedSessionSource`
→ `GovernedLaneSource`. Ruling 15a (`note-addendum-a-ruling-15a-governed-episode`) amends the
second half: the type `GovernedSessionSource.Open` returned, `GovernedSession`, renames to
`GovernedEpisode` (not `GovernedLane` — already the name of a pre-existing, unrelated composite
in the same file). Both are new code identifiers; the spec is silent on `GovernedSession` by
name (it never appears in the spec HTML), so only `GovernedSessionSource` needs an erratum.

## The three spec lines this corrects

**§6.2, line 260:**

> `<p><code>GovernedSessionSource</code> implements the same source seam as <code>AuditLogEpisodeSource</code>: it translates run events (§7.2) into episode opens, attribute sets, evidence-path declarations, and closes. Evidence stays observation-not-testimony: lanes declare paths; <code>ProofPackVerifier</code> and the converge gates go and look.</p>`

Reads today as `GovernedLaneSource`. (Separately, `note-conductor-episode-source-seam` already
rules the "same source seam" claim is intent, not an instruction — no `IEpisodeSource` exists or
is invented; this erratum is only about the name.)

**§10, line 336** (module-changes table, `AiDe.Core/Watcher/` row):

> `<tr><td><code>AiDe.Core/Watcher/</code></td><td>Third episode source; cohort attributes; §8.3 derived metrics at close</td><td><code>GovernedSessionSource</code>, <code>RunDerivedMetrics</code>, <code>BenchImportSource</code></td></tr>`

Reads today as `GovernedLaneSource`. (The row's placement under `AiDe.Core/Watcher/` was already
stale before this erratum — the type lives in `AiDe.Core/AgentPlane/`, per this same spec's own
§10 `AiDe.Core/AgentPlane/` row and every file path cited in this note. Recorded here as found,
not corrected — out of scope for a naming erratum.)

**§11, line 376:**

> `<li>A run with a claude-code lane (governed) and a grok-build lane (observed, terminal) produces episodes through <code>GovernedSessionSource</code> and <code>AuditLogEpisodeSource</code> respectively, scored by the unchanged <code>ScoringService</code>.</li>`

Reads today as `GovernedLaneSource`.

## Scope effect

**Supersedes** the name `GovernedSessionSource` at spec v1.0 §6.2 (line 260), §10 (line 336),
§11 (line 376) with `GovernedLaneSource`. **Freezes** the spec HTML — no edit made to it.
**Does not correct** the §10 table's module placement (a pre-existing, separate staleness, noted
but not in scope here). **Admits** no new package, seam, or interface.
