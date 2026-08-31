# Spike S4 - coord-core append alignment for the injected coordination contract

**Question.** Can a non-AI-Forward session feed Loomkeeper registration + heartbeat **over the existing
`coord-core` append semantics** (one ledger, projected, not duplicated - architecture §6, US-5), and can
a .NET reader consume that log tolerantly and deterministically?

**Status: PASS.** The injected contract is realizable as `coord-core`-shaped JSONL, and the C# parse
contract that slice 2 ships is proven against every hostile line shape.

## What the real writer produces (captured by driving `coord-core.py` directly)

Appending two events for one session and dumping the raw bytes of `log/<session>.jsonl`:

```
b'{"agent": "claude-code", "at": 1000.0, "contract": "loomkeeper/1", "kind": "register", "path": "-", "seq": 1, "session": "sess-abc", "wi": "WI-0", "worktree": "repoA/main"}\n'
b'{"agent": "claude-code", "at": 1030.0, "kind": "heartbeat", "path": "-", "seq": 2, "session": "sess-abc", "wi": "WI-0", "worktree": "repoA/main"}\n'
```

Established facts (verified, not inferred):

1. **One JSON object per line, `\n`-terminated.** `json.dumps(event, sort_keys=True)` - keys are
   **alphabetically sorted**, one space after `:` and `,`. A reader must not depend on key order.
2. **Open schema.** Custom keys (`contract`, `worktree`, and any identity `attrs`) ride alongside the
   standard `kind/session/agent/wi/path/at/seq` without disturbing the writer. So the injected
   contract adds its own keys without forking `coord-core`.
3. **`seq` is auto-assigned** by the writer (1, 2, ...); `at` is a float epoch.
4. **The fold sorts by `(at, session, seq)`** and dedups on `(session, seq)` (coord-core `fold`/
   `read_events`). The Loomkeeper reader mirrors this ordering so replay is deterministic.
5. **LOG-A guard.** When the file does not already end in a newline, the writer emits a **leading**
   `\n` before the record, so a fused line is impossible to express. The reader must treat a leading
   blank line as nothing, never as a record.
6. **Atomic append.** Each record is exactly one `os.write()` under `O_APPEND` (+`O_BINARY` on
   Windows so committed bytes are LF). A concurrent writer cannot interleave a partial line.

## The injected-contract shape slice 2 defines (over those semantics)

```json
{"kind":"register","contract":"loomkeeper/1","session":"<external-id>","at":<epoch>,"seq":<n>,
 "attrs":{"repo.canonical_path":"...","repo.display_name":"...","worktree.branch":"...",
          "worktree.path":"...","terminal.id":"...","agent.name":"...",
          "service.name":"<harness>","gen_ai.request.model":"<model>"}}
{"kind":"heartbeat","contract":"loomkeeper/1","session":"<external-id>","at":<epoch>,"seq":<n>}
{"kind":"session-end","contract":"loomkeeper/1","session":"<external-id>","at":<epoch>,"seq":<n>}
```

- `attrs` reuses the **same `OtelAttributes` keys** as the OTLP path, so one mapper
  (`OtelSpanMapper.MapRegistration`) serves both transports - trust is `Verified` when the harness
  names itself via `service.name`, else `Asserted` (ADR-0020), with no new mapping seam.
- `contract` is the **pinned version** (`loomkeeper/1`). A record whose version differs is rejected
  and counted, never mis-parsed (Testing Strategy A6 - a schema change is a contract change).
- The **capability lives in the adapter**, not the file: the file is a local, forgeable surface
  (ADR-0007), so the adapter mints the capability at `register`, holds `external-id -> capability`,
  and verifies each `heartbeat` against it. A heartbeat for an unregistered external id is dropped.

## What the .NET reader proves (this spike)

A single log reproducing every hostile shape at once - a normal record, a **LOG-A leading-newline**
record, a **CRLF** line, a **blank** line, a **malformed** line, a **wrong-version** line, and an
**out-of-order `at`** - parses to exactly:

```
parsed=3 malformed=1 versionRejected=1
  register   at=1000  seq=1
  heartbeat  at=1005  seq=9   (out-of-order line, sorted into place)
  heartbeat  at=1030  seq=2
SPIKE PASS
```

So the parse contract is: split on `\n`, trim each line (tolerates CRLF + leading/trailing space),
skip blank, skip malformed (count), reject wrong `contract` version (count), read `attrs` string
values, then **sort by `(at, session, seq)`**. This is the logic ported into
`src/AiDe.Core/Watcher/CoordinationContract.cs`; the suite re-proves it with golden fixtures that
reproduce these exact byte shapes, so CI carries no Python dependency.

## Residual / deferred (not slice 2)

- **Board** kinds (question/reply/ack/knowledge) ride the same append log - Phase 3 / slice 6.
- **Goal/done declaration** (Work Episode) - slice 4.
- **Token issuance/injection** into a non-AI-Forward session (writing the `.loomkeeper/contract`
  helper into the target repo) is the session-side half; slice 2 ships the **ingest** half and keeps
  the writer external.
