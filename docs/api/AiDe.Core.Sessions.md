---
id: api-aide-core-sessions
title: "API: AiDe.Core.Sessions"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Sessions: 6 types, 20 members, 77% carrying a summary doc comment.
---

# API: `AiDe.Core.Sessions`

**6 public types · 20 public members · 77% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `SessionConfig`

*record* — `SessionConfig.cs`

The user-facing container Addendum A3 defines: named, workspace-bound, and carrying
session-scoped config (Phase 1: which agent backends are enabled). R14 b2: "session" in this
namespace names only this container — never a Watcher/Dispatch/Terminal-internal concept.

## `SessionEventKinds`

*class* — `SessionConfig.cs`

The `kind` strings this slice adds to the open, unenumerated vocabulary
`Kind` already accepts (clause 4). Defined here, in the
container's own namespace, rather than in `AgentPlane` — these are session-level events, not
run events, and F0 does not touch `AgentPlane.RunEvent` at all: proving it needs no change
IS the clause.

| Member | Summary |
|---|---|
| `string Open = "session.open"` | A session was created (`Create`). |
| `string Config = "session.config"` | A session's config changed — currently only `EnabledBackends` toggles. |

## `SessionEvent`

*record* — `SessionConfig.cs`

One line of a session's append-only `session-events.jsonl` (clause 3's "emit a session
event"). Deliberately its own small type rather than `RunEvent`: a session
event has no run id and no agent id, so forcing it into that shape would mean populating fields
that do not apply. Clause 4's obligation — that the future run-event stream accepts
`SessionEventKinds` with no schema change — is proven directly against
`RunEvent` in `SessionEventEnvelopeTests`, not by round-tripping this
type through it.

## `SessionConfigStore`

*class* — `SessionConfigStore.cs`

Reads and writes one session's `session.json` and `session-events.jsonl` (clauses 1-3).

**Remarks.** **Toggles apply to new runs only (clause 3).** `SessionConfig` is an
immutable record; `SetEnabledBackends` never mutates an existing instance, it writes a
new one. A caller that already captured a `SessionConfig` (modelling a run reading its
config at start) is holding a value a later toggle cannot reach — proven in
`SessionConfigStoreTests.SetEnabledBackends_NeverMutatesAConfigARunAlreadyCaptured`. The
append-only event log gives the same guarantee one layer down, at the persisted bytes: earlier
lines are never rewritten (`SessionEventsFile_EarlierEventsSurviveByteForByteAfterALaterToggle`).






Idiom matches `Health.HealthIncidentSidecar`: a single lock around read-modify-write,
plain `System.Text.Json`, tolerant JSONL reads.

| Member | Summary |
|---|---|
| `SessionConfigStore(string workspaceRoot, string sessionId)` | **(gap)** |
| `string WorkspaceRoot { get; }` | **(gap)** |
| `string SessionId { get; }` | **(gap)** |
| `SessionConfig Create(` | Creates the session: writes `session.json` and emits `session.open`. |
| `SessionConfig Load()` | The current, live config — what a NEW run would pick up. |
| `SessionConfig SetEnabledBackends(IReadOnlyList<string> enabledBackends, DateTimeOffset now)` | Applies a backend toggle for new runs and emits `session.config`. Never mutates a `SessionConfig` a caller already holds — see the remarks on this type. |
| `IReadOnlyList<SessionEvent> ReadEvents()` | Every event this session has ever emitted, in append order. |

## `SessionId`

*class* — `SessionId.cs`

The session id: a filesystem-safe, sortable, collision-resistant token that names a session's
directory under `.aide/sessions/<session-id>/` (Addendum A3).

**Remarks.** **Why not `AgentWorktree.ShortId`.** `ShortId`
was read before writing this. It solves a different problem: deriving an eight-character
correlation TAG from an EXISTING, possibly hostile, external id (a harness session id feeding a
branch/folder name) by sanitizing and truncating. Truncation is the right trade there because the
tag is a display convenience alongside the real id.





Here there is no existing id to derive from — this method MINTS the primary identity of a
brand-new object, and it is the only thing distinguishing two sessions on disk. An
eight-character truncation of hostile/arbitrary input collides readily (that is *why*
`ShortId` is only ever used as a secondary tag, never as the sole key); a primary key cannot
accept that trade. So this generates its own value instead of sanitizing one: a UTC timestamp
(sortable — an operator scanning `.aide/sessions/` sees creation order for free) plus 32 bits
of CSPRNG entropy (collision-resistant independent of anything the caller supplies).





**The safe character set is reused.** The format below (`yyyyMMddTHHmmssZ-hexhexhex`)
is built entirely from `AgentWorktree`'s lesson: no `.`, no path separator, and nothing
outside `[A-Za-z0-9-]` — the same "small safe set" argument, applied to a value this type
controls completely rather than to hostile input.

| Member | Summary |
|---|---|
| `string New(DateTimeOffset? now = null)` | Mints a new session id. Deterministic in its timestamp half for testability. |
| `bool IsValid(string? candidate)` | True when  is this type's own shape — filesystem-safe by construction, since the character set never leaves `[0-9A-Za-z-]`. |

## `SessionPaths`

*class* — `SessionPaths.cs`

The on-disk path contract for a session (Addendum A3; Ruling 23; F0 clauses 1-2).

**Remarks.** **`session.json`, not `.yaml` (Ruling 23, recorded as an A3 erratum).** A3's
prose named `session.yaml`; no YAML parser exists in any `.csproj` in this repository,
`System.Text.Json` is already used in 38 files, and the sibling run-log path is
`.jsonl`. Introducing a YAML dependency for this one file would be the only YAML reader in
the product for a format with no reader anywhere else.





**`runs/<run-id>.jsonl` is RESERVED, not built.** `RunLogStore` is Phase
3. This type only computes the path so the contract is fixed now and the directory shape never
has to change later; nothing in this slice ever calls `RunLogFile` to write —
asserted by `SessionPathContractTests` and `SessionConfigStoreTests` in
`AiDe.Core.Tests.Sessions`.

| Member | Summary |
|---|---|
| `string SessionFileName = "session.json"` | **(gap)** |
| `string EventsFileName = "session-events.jsonl"` | **(gap)** |
| `string RunsDirectoryName = "runs"` | **(gap)** |
| `string SessionsRoot(string workspaceRoot)` | `<workspaceRoot>/.aide/sessions` — every session's parent. |
| `string SessionDirectory(string workspaceRoot, string sessionId)` | `<workspaceRoot>/.aide/sessions/<session-id>`. |
| `string SessionFile(string workspaceRoot, string sessionId)` | The session's config file — see the remarks on YAML vs. JSON. |
| `string EventsFile(string workspaceRoot, string sessionId)` | The session's own append-only event log (`session.open` / `session.config` — clauses 3-4). Deliberately a SIBLING of `runs/`, never inside it: this is session-scoped state, not a run's record, and clause 2 forbids any… |
| `string RunsDirectory(string workspaceRoot, string sessionId)` | `<workspaceRoot>/.aide/sessions/<session-id>/runs` — RESERVED for Phase 3's `RunLogStore`. Computed, never created or written to, by this slice. |
| `string RunLogFile(string workspaceRoot, string sessionId, string runId)` | `<workspaceRoot>/.aide/sessions/<session-id>/runs/<run-id>.jsonl` — RESERVED for Phase 3's `RunLogStore`. F0 fixes the path so the shape never has to move; it must not be used to write here (clause 2, asserted by test). |
