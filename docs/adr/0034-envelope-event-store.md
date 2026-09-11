---
id: adr-0034-envelope-event-store
title: "ADR-0034 — The compiled-envelope store is an append-only, exclusively written, sha-chained JSONL sidecar per session, owned by the session document's lifetime; the eval corpus and the Proof Pack are projections; purge deletes the file and the Session's own delete cascades to it"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-d"
tags: [architecture, compile, envelope, store, append-only, channel-b, privacy, addendum-d, dm-data-modelling]
links:
  - { to: architecture, rel: implements }
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: note-addendum-d-envelope-store, rel: supersedes }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: depends-on }
  - { to: adr-0002-workspace-fact-store, rel: relates-to }
  - { to: adr-0013-layout-persistence-envelope, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-11
review-suggested:
  - { by: adr-0013-layout-persistence-envelope, on: 2026-09-11, reason: "ADR-0013 amended (Ruling 52, ADR-0032): one zone-envelope file per host perspective; drop-with-report at restore; tested rollback" }
summary: >-
  Promotes note-addendum-d-envelope-store (its own promotion rule) to the durable-representation
  ADR for Prompt Compilation: one row in <workspace>/.aide/sessions/<id>/envelope-events.jsonl is
  one event on one envelope keyed (envelope_id, seq); the writer is one EnvelopeStore in Core
  opened FileShare.None by the session document that owns the composer; prev_sha chains the raw
  previous line; the eval corpus and the Proof Pack summary are rebuildable projections; expand-only
  schema evolution; aide session purge deletes the file only, and Addendum A's session deletion
  cascades to it by directory containment. Rejected: SQLite, one row per turn, a stored lease, a
  redactor, a shared cross-session file.
---

# ADR-0034: The compiled-envelope store — append-only events, one exclusive writer, projections only

- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 (`addendum-c-chain`) with the
  **Data & Persistence Architect** (veto holder — the store and the migrations) and the **Security &
  Identity Architect** (veto holder — the store's contents) in Peer Mode; attacked at the gate
- **Context spec/architecture:** `spec-addendum-d-compile-step` §A6 (logical model, DM8–DM15),
  §A12.1 (event schema), §A13.5 (privacy posture), US-D7, US-D13, F-12; `note-addendum-d-envelope-store`
  (the origin note, superseded by this record); ADR-0002 (the fact store this is *not* in);
  Addendum A §A3 (the Session aggregate)

## Context

The spec's Data & Persistence Architect passed the model: the **Envelope** is the aggregate, its one
invariant *it only grows* (strictly increasing `seq`; no mutation; no `decorated` after an accepted
`submitted`); the grain is *one row = one event on one envelope, recorded when it occurs*; the lease,
shape, tier, effective fan-out and every count are projections; the craft profile is a Type-2
dimension (ADR-0037). The origin note's promotion rule reads *"promote to an ADR in
`/define-architecture` of the compile seam"* — this is that record. A1 architects to the model and
decides only what the note left to architecture: **where the writer lives, who owns its lifetime,
the exclusive-writer guarantee, the deletion cascade, and the seam to the eval and the Proof Pack.**

Facts: `.aide/` is git-ignored (`.gitignore:535`, `tools/verify-aide-gitignore.py`) and holds
`session.json`, `session-events.jsonl` and the reserved `runs/` (`SessionPaths.cs:60-73`) — per the
spec **[Verified there; not re-opened here — Inferred]**; the shell owns the session documents'
lifetime (`WorkbenchShell.cs:60-68`: *"the SHELL owns their lifetime, not the factory"*)
**[Verified]**; the draft store's precedent for a corrupt sidecar is *lose history, not the session*
(`ComposerDraftStore.cs:126`, per the spec). DM principles: DM6 (one operational store, derived
projections never a second truth), DM8 (grain declared), DM11 (invariants get executed tests), DM13
(the representation is an ADR), expand-migrate-contract.

## Decision

We will:

1. **Place the writer in the bounded context:** `AiDe.Core/Compilation/EnvelopeStore` exposes
   **`Append(EnvelopeEvent)` and a reader** and nothing else (no update, no delete, no rewrite
   member — asserted by reflection). It computes `seq` (one greater than that envelope's last row,
   held in memory by the exclusive writer) and `prev_sha` (sha256 of the raw previous line, any
   schema) on append; a torn final line is newline-terminated before the next append. `Append`
   refuses an `opened` row whose `session_id` differs from the owning directory's segment — the
   session id has two homes (the path and the row) and the containment cascade below assumes they
   agree, so the store enforces it with one comparison.
2. **The session document owns the store's lifetime.** One `EnvelopeStore` per open session
   document — opened (`FileShare.None`) when the document opens, disposed when it closes;
   a second AI-DE instance on the same session is **refused visibly** at open (DM11 h) — the
   composer shows *"another AI-DE has this session's compile history open"* and the session still
   opens with Prepare degraded to mechanical-only (a corrupt or locked sidecar loses history, not the
   session). Readers — purge, export, the eval — open the file only when no writer holds it and are
   refused visibly otherwise, which is what makes *no `submitted` = abandoned* a stable read.
3. **The reader folds defensively, and a broken chain refuses the writer.** An unknown `kind` or
   `schema` and a torn last line are skipped and counted; a broken chain mid-file is reported as
   *"record broken at line N"* and yields no envelope past N; an accepted `submitted` with no
   `consumed` reads *outcome not recorded*. **On open, the writer walks the whole chain first** —
   **reading `(envelope_id, seq)` and the raw line from every line regardless of `schema` or `kind`**
   (the key is in the `/1` envelope and evolution is additive), so a rolled-back `/1` binary opening
   a file with `/2` rows still sees every key; **only the fold skips** what it cannot read. If the
   chain is broken at line N, **`Append` is refused** for the life of that open — Prepare degrades to
   mechanical-only with the reason (*"compile history is broken at line N; purge it to start
   again"*), the file is left intact for inspection, and `aide session purge` is the named recovery.
   A writer that appended past a break, or built its key map from a fold that skips rows, could
   mint an `(envelope_id, seq)` equal to a row it did not read — the grain's key duplicated in the
   file, the aggregate's one invariant broken, and every later row landing where no reader folds it
   (the D&P Architect's Blocker at this gate). Refuse, not rotate: a rotated `.broken-<at>` file
   would be a second history nothing reads. **The walk is measured, not assumed:** it runs off the
   UI thread, and the open emits `envelope-store.open{bytes, rows, envelopes, broken_at, walk_ms}`
   on the normal path — the emitting source D-D5's 10 MiB trigger reads (a trigger with no emitter
   cannot fire). **An `Append` failure after a good open** (disk full, an IO error) degrades the
   session to mechanical-only with the reason and an `error_code`, emits
   `compile.degraded{reason: store-append-failed}`, and the send proceeds — the store's one
   otherwise-silent path, named.
4. **Schema evolution is expand-only** (`compiled-envelope/1` → `/2` additive; a `/2` reader folds
   `/1` rows; a `/1` writer appends after a `/2` row with a unique higher `seq` and an unbroken chain;
   rolled-back binaries skip what they cannot read and never truncate). **No backfill:** history
   begins at the first append.
5. **Two projections, never stores:** the **eval corpus** (`tools/compile-eval/derive-fixtures.py`,
   one row per `(envelope_id, structure line)`) and the **Proof Pack summary** (a committed Channel-A
   artifact that *cites* `envelope_id + projection_sha`) are rebuilt from the fold on demand; export
   is an explicit command over operator-selected envelopes to an operator-named path inside a
   directory the operator picked (no traversal); no redactor.
6. **Deletion, two commands, one containment rule.** `aide session purge <id>` deletes
   **`envelope-events.jsonl` and nothing else**, after validating `<id>` as one segment of the
   session-id grammar resolved under `<workspace>/.aide/sessions/` (junctions and symlinks not
   followed) and printing name · id · workspace root · resolved file path · envelope count · newest
   `at` (`--yes` for non-interactive). **Addendum A's session deletion (the Session aggregate's own
   command) cascades to the envelope file by directory containment** — it removes the session
   directory, and the envelope file lives in it — so an orphaned `source_text` cannot survive its
   session (F-12, answered here: containment, not a second delete path). **The Session delete
   first acquires the envelope file exclusively and refuses whole while a writer holds it; then it
   deletes the envelope file under the held handle (`FileOptions.DeleteOnClose`) before removing the
   siblings** — acquire → release → recursive delete would reopen the race: a recursive delete on
   Windows removes the siblings and then fails on a locked file, leaving `session.json` gone and
   `envelope-events.jsonl` orphaned, the exact orphan the cascade exists to prevent (the D&P
   Architect's finding; Windows share-mode semantics, **Inferred until the test runs**). Layout
   files are unaffected
   (they are per workspace, ADR-0013). A Proof Pack citing purged envelopes resolves them as *purged*.
7. **A run may outlive its document — one lifetime rule.** A run's prompt bound is 15 minutes; a
   session document may close first. **The store's lifetime is the document's** (rule 2, once): at
   close with a run in flight the host appends `consumed{run_id, outcome: "not recorded", reason:
   "document closed"}`, a later `consumed` for that `run_id` is refused (one `consumed` per
   envelope — the aggregate invariant), and the run's real outcome stays in the run's own record.
   Holding the handle past the document would make rule 2 false and refuse a same-process re-open
   as "another AI-DE" (the Tech Lead's finding).
8. **Privacy posture as specified** (§A13.5): plaintext, Channel B, `source_text` only (attachment
   bodies never persisted — a `body` member is rejected at `Append`), bounded by the editor's own
   text bound, no expiry (D-D5's 10 MiB trigger names when one is designed).

## Alternatives considered

- **SQLite (a second table in the workspace fact store, or a sidecar DB):** rejected — the fact
  store's contract is *evidence about the repository*; a compile envelope is session work data, and
  `source_text` may carry secrets — it must be Channel B and purgeable per session, which a
  workspace DB is not. A sidecar DB is a second engine for one append-only file; JSONL is the repo's
  established append-only shape (`session-events.jsonl`, `audit-log.jsonl`) with its gates.
- **`session-events.jsonl` as the home for envelope events (the session's existing append-only
  file):** rejected — a different grain (session lifecycle events vs. events on one envelope), a
  different writer (the session host vs. the exclusive compile writer), no chain, and a purge that
  must delete compile history *and nothing else* (US-D13); US-D10 forbids the write outright. It is
  cited as the precedent for the JSONL shape, not as a candidate home.
- **One row per compiled turn, written at the terminal state:** rejected (the origin note) — a
  mutable document; abandoned turns' model calls (P10-audited) would never be written; `derived` vs
  `operator` cannot be paired without rewriting.
- **Storing the lease / CT19 block / context refs as fields:** rejected — DM7; every one is
  derivable from the fold; `projection_sha` witnesses the rebuild instead.
- **A cross-process lock or one file per AI-DE instance:** rejected for v1 — the exclusive handle
  refuses the second writer visibly, which is the honest state; a per-instance file would make *one
  session, one history* false.
- **A redactor over `source_text` before storing:** rejected — a redactor that misses is a plausible
  wrong record (IO8); the controls are the channel and the purge.
- **A `Delete(envelope_id)` member for "forget this turn":** rejected — a per-row delete breaks the
  chain and the invariant; deletion is by containment (the file, the session directory).
- **The writer in `AiDe.App` beside the composer:** rejected — the store, the fold and the rebuild
  test are headless; the App owns only the *lifetime* (open/dispose with the document).

## Consequences

- **Positive:** history and the audit trail *are* the data; the eval corpus is free; a rollback is
  "stop appending"; the two-writer case is closed by the OS, not by a protocol.
- **Negative / accepted:** a locked file degrades a second instance's Prepare to mechanical-only
  (visible, recoverable by closing the other instance); the machine's own user can rewrite the file
  and recompute the chain (T2 — accepted; the only external anchor is a committed Proof Pack's
  `projection_sha`); harness transcripts under `~/.claude/projects/` retain the compile prompt
  outside this deletion path (declared, Inferred).
- **Follow-ups / new risks:** a pasted secret in `source_text` re-egresses in the next K compiles'
  history window until the whole history is purged — all-or-nothing today; an append-only-compatible
  remedy is named for Privacy's review, not built: an `operator` decoration `history_exclude
  {envelope_id}` honoured by the window rule (no per-row delete, no redactor). The size trigger
  (10 MiB, D-D5) is unmeasured; the first session that
  crosses it designs expiry. `runs/` stays reserved — `consumed` carries `outcome` until a run log
  exists.

## Falsifying tests (headless; US-D7, US-D13)

1. **Append-only surface:** reflection over `EnvelopeStore`'s public members finds `Append` and a
   reader only; an appended `seq` ≤ the last is refused; a `decorated` after an accepted `submitted`
   is refused; a `body` member on an attachment value is refused.
2. **Two writers:** two `EnvelopeStore` opens on one file → exactly one refusal; a reader while a
   writer holds the file → a visible refusal, never a partial fold.
3. **Chain and fold:** one flipped byte mid-file → *"record broken at line N"* and no envelope past
   N; **reopening the store on that file → `Append` is refused with the reason, the file's bytes are
   unchanged, and the file never holds two rows with one `(envelope_id, seq)`**; a torn last line
   and an unknown `schema` → skipped and counted; a `/1` writer after a `/2` row → unique higher
   `seq`, unbroken chain — **and a `/1` writer's open-time key map contains every `/2` row's
   key** (the walk is schema-agnostic); an accepted `submitted` with no `consumed` → *outcome not
   recorded*; an `opened` row whose `session_id` differs from the directory segment → refused at
   `Append`; the open emits `envelope-store.open` with `walk_ms` and `bytes`; an `Append` that
   throws after open → mechanical-only with the reason, `compile.degraded` emitted, the send
   proceeds.
4. **Rebuild — a Proof Pack item (P-D2), not a CI test:** over ≥ 20 real envelopes from a real
   session, `sha256(canonical(projection of Fold(rows))) == submitted.projection_sha` — 100 %;
   fixtures alone do not satisfy this (DC-127). The **CI floor** is the same rebuild over `authored`
   fixtures, labelled as such — it proves the code path, never the claim.
5. **Purge and cascade:** `aide session purge <id>` removes `envelope-events.jsonl` and leaves
   `session.json`, `session-events.jsonl` and every layout file; `purge ..\..`, a non-segment id and a
   junctioned session directory are refused before any file is touched (a junction fixture); the
   Session aggregate's delete removes the directory including the envelope file (the cascade test
   asserts the file is gone and nothing outside the directory changed); **a Session delete while a
   writer holds the file → refused whole, nothing removed** (`session.json` still present); the
   delete's ordering is observed: the envelope file is gone before any sibling is removed.
6. **Lifetime:** opening a session document opens the store; closing it releases the handle (a
   second open succeeds afterwards); a locked file at open degrades Prepare to mechanical-only with
   the reason shown, and the document still opens; closing the document with a run in flight
   releases the handle and appends `consumed{outcome: "not recorded", reason: "document closed"}`
   — never an absent row — and a later `consumed` for the same `run_id` is refused (exactly one per
   envelope).

## LOA mapping

T0. Patterns (named as the Patterns Expert corrected them — three, not five): **Event Store
(event-sourced aggregate; the envelope is the fold)**, **Hash Chain for corruption detection** (not
tamper evidence — Security T1/T2), **Projection** (eval corpus, Proof Pack). `FileShare.None` is
the lock-file idiom in prose, not a pattern; the fact store's "Append-Only Evidence Ledger" is
*not* claimed — that name is defined by SQLite triggers (`docs/architecture.md` Applied patterns)
and this file has none of that machinery. DM: grain declared; measures classified in §A6; history
rule — the row is immutable (Type-2 by construction); migration expand-only; deletion by
containment.

## Evidence

- **Verified (read):** `WorkbenchShell.cs:60-68` (the shell owns session-document lifetime);
  `spec-addendum-d-compile-step` §A6 DM11 (a)–(h), §A12.1, §A13.5 (the D&P-passed text); the origin
  note's alternatives and validation condition.
- **Inferred (cited from the spec, not re-opened here):** `.gitignore:535`; `SessionPaths.cs:60-73`;
  `ComposerDraftStore.cs:126`; `RunEvent.cs:127-131`.
- **Rulings:** 64 (the `ceilings` snapshot's writer), 70 (the `task_class` decoration), the D&P
  Architect's corrections recorded in the spec's gate record (pass 1, wave 1; pass 2).
