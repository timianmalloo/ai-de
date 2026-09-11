---
id: note-addendum-d-envelope-store
title: "The envelope store is an event-grained, append-only JSONL sidecar per session — the envelope is the fold, the lease is a projection, cost lives on the model call, and history begins at the first append"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, addendum-d, compile, envelope, data-model, dimensional, channel-b]
links:
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: note-addendum-d-compile-step-proposal, rel: relates-to }
review-by: 2027-03-10
review-suggested: []
summary: >-
  The durable representation for Prompt Compilation: one row in
  `<workspace>/.aide/sessions/<session-id>/envelope-events.jsonl` is exactly one event on one
  envelope, keyed (envelope_id, seq); the envelope is the fold; the lease, the CT19 block and every
  count are projections; the craft profile is a Type-2 dimension carried by the pack. Expand-only,
  no backfill, deletion by containment. Blast radius: every compiled turn from the first append on.
---

# The envelope store — event grain, one writer, two projections

- **Kind:** decision
- **Confidence:** Verified for the store facts (`.aide/` git-ignored at `.gitignore:535`; `runs/`
  reserved, `SessionPaths.cs:60-73`; `RunEventCost` at `RunEvent.cs:127-131`; the draft store's
  posture, `ComposerDraftStore.cs:24-48`); **Inferred** for the size trigger (10 MiB) and the
  single-instance-writer assumption
- **Made during:** `/specify` of `spec-addendum-d-compile-step` (node S2), with the Data &
  Persistence Architect in Peer Mode; the DM13 deviation record for choosing the dimensional /
  append-only shape for an operational store

## The call

**Grain (DM8):** one row is exactly one event on one envelope — `opened · decorated · called ·
submitted · consumed` — identified by `(envelope_id, seq)`, recorded when the event occurs; an
envelope is opened only by the Send gesture, so the live pre-compile appends nothing. An envelope
with no `submitted` is abandoned (no event kind is needed for that), and a re-compile opens a new
envelope whose `opened.supersedes` names it. The **envelope** is `Fold(rows by envelope_id)`; the **current value** of a decoration name is
the `decorated` row with the highest `seq` (`source` is provenance, never precedence); the **eval
corpus** and the **Proof Pack summary** are derived projections, never second stores (DM6).

**Why events and not "one row per compiled turn"** (the conductor's candidate): a row per turn is a
document that must be rewritten as Prepare proceeds, and an abandoned turn's model calls — which LOA
P10 requires audited — would never be written. Event grain keeps every append immutable and keeps
cost at the grain where it exists: **one model call yields several decorations**, so cost, latency,
model identity and outcome live on the `called` row and a `derived` decoration points at it by
`call_seq`.

**The lease is not a decoration — nor are the shape, the tier or the effective fan-out.**
`LeaseDerivation.Derive(source_text)` is the one definition of the lease; the rule of §A9 over the
current structure and the source text is the one definition of the tier; a stored copy of any of
them beside its function is one quantity with two homes (register class DM-A) and goes stale on the
first override. `Project()` computes all four at every render and at Submit; only an operator's tier
override is ever stored. The write-scope line and the projection at Submit are the same call.

**The craft profile is a Type-2 dimension** with natural key `family` (Type-0: the catalog's
`Provider`) and version identity `(version, sha)`; an envelope pins `(family, version, sha)` in a
mechanical decoration and reads the same forever.

**Channel B.** The file lives beside `session.json` and `session-events.jsonl`, a sibling of the
reserved `runs/` (nothing writes there — clause 2), under the git-ignored `.aide/`
(`tools/verify-aide-gitignore.py` keeps that true). It holds `source_text` — work data — so it can
never be Channel A. Attachment bodies are never persisted (`{path, bytes, sha256,
outside_workspace}` only — the draft store's precedent).

**Migration:** expand only. No backfill — *history begins at the first append; nothing is
reconstructed from drafts or run logs.* `compiled-envelope/2` is additive: a per-row `schema`, a `/2`
reader folds `/1` rows, new required fields read as "not recorded", and a rolled-back `/1` binary
skips rows it cannot read and never truncates the file. Rollback of the whole feature = stop
appending; the file is inert.

**Retention:** session lifetime; deletion by containment — `aide session purge <id>` deletes
`envelope-events.jsonl` **and nothing else** (`session.json`, `session-events.jsonl` and `runs/`
belong to the Session aggregate and Addendum A's own command), validates `<id>` as one segment of
the session-id grammar under `.aide/sessions/`, prints the session's name, id, workspace root, the
resolved file path, the envelope count and the newest `at` before confirming, and ships *with* the
store (a deferral whose trigger is the store itself would be self-refuting); no per-row delete; no
expiry; plaintext; a size trigger (10 MiB) names when an expiry is designed (D-D5). **One writer per
file:** the store opens it exclusively (`FileShare.None`); a second instance is refused visibly.
**A per-line `prev_sha` chain** over the raw previous line (schema-blind) detects torn writes and
accidental edits — a flipped byte inside a JSON string is still valid JSON and would otherwise fold
into a plausible history; it is corruption detection, not tamper evidence (the Security architect's
T1/T2 split), kept against the Simplifier's soft veto at one hash per line.

## Alternatives dismissed

- `one row per compiled turn, written at the terminal state` — a mutable document; loses abandoned
  turns' model calls; cannot pair a `derived` value with the `operator` value that superseded it
  without rewriting.
- `stored tier / shape / effective-fan-out rows with an inputs DAG and a freshness invariant` — a
  second home guarding the first (the Simplifier's cut, accepted): the projection recomputes them.
- `an abandoned event kind` — "no `submitted`" already says it; a session-close write hook for
  nothing (the Simplifier's cut, accepted).
- `SQLite` — a second engine for one append-only file; the JSONL precedent (`session-events.jsonl`,
  `audit-log.jsonl`) already carries the repo's append-only discipline and its gates.
- `storing the lease, the CT19 block and the context refs as fields` — every one is derivable from
  the fold; storing them is DM7's defect signature.
- `a redactor over source_text before storing` — a redactor that misses is a plausible wrong record
  (IO8); the control is the channel (git-ignored) and the purge, not a filter.

## Validation condition

Holds until/unless a second AI-DE instance writes the same session's file concurrently (then a
cross-process lock or a per-instance file is designed), or a session's file exceeds 10 MiB (then
D-D6), or `runs/` is built (then `consumed` may join to the run log rather than carry `outcome`).

## Promotion rule

This bears load the moment the first slice writes a row: promote to an ADR in
`/define-architecture` of the compile seam; this note remains the origin story.
