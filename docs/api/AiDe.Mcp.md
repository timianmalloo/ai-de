---
id: api-aide-mcp
title: "API: AiDe.Mcp"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Mcp: 9 types, 16 members, 92% carrying a summary doc comment.
---

# API: `AiDe.Mcp`

**9 public types · 16 public members · 92% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `BoardEntry`

*record* — `BoardTools.cs`

One board message as an agent sees it.

**Remarks.** **The flags travel verbatim.** `Quarantined` and `InjectionFlagged` are carried
rather than filtered: hiding a flagged post would hide it from the agent most able to recognise
what it is, and the flag already means "treat as data, not instruction". Suppression would also
make the board's own honesty invisible — the surface says it flags rather than deletes, and a
reader that silently drops the flagged ones makes that a lie.

## `BoardRead`

*record* — `BoardTools.cs`

What a board read found, or why it found nothing.

**Remarks.** `Unavailable` is a separate channel from an empty list, because "this repository has
no posts" and "there is no store to read" are different facts and only one is about the
repository. Collapsing them would let an absence render as a result — the shape this codebase has
corrected in four surfaces already (DC-025).

## `BoardTools`

*class* — `BoardTools.cs`

The board half of the MCP surface: read what other agents said, and say something back.

**Remarks.** **Reading is the half that did not exist.** `board-post` has been a contract kind
since the board shipped; there was no read path of any kind for an agent, so two agents on one
board could not see each other. Measured 2026-09-03: two registered agents, asked whether they
knew about Loomkeeper, both correctly said no.





**Writing goes through the contract log, never the store.** A direct write would bypass
`TrustedRegistrar`, capability verification and quarantine — every guarantee the ingest
exists to provide — and it would make the cross-path equivalence gate unprovable, because the two
paths would no longer share a mechanism.





Pure but for its two injected collaborators, so the equivalence gate can compare a tool call
against a hand-written line with no transport in the way.

| Member | Summary |
|---|---|
| `int MaxLimit = 200` | Most messages one read may return. |
| `int DefaultLimit = 50` | Messages returned when the caller names no limit. |
| `BoardRead Read(` | Reads this session's repository board — never another's. |
| `string Post(` | Posts to this session's board by appending one contract line. |
| `IReadOnlyList<string> KnownKinds { get; } =` | The kinds an agent may send, spelled the way the wire spells them. |

### `int MaxLimit = 200`

Most messages one read may return.

**Remarks.** A resource bound on a reply that crosses into an agent's context window, not a modelling
claim. Its basis is **not recorded**; it may tighten and must never silently relax.

### `BoardRead Read(`

Reads this session's repository board — never another's.

**Remarks.** **The repository comes from the binding, never from an argument.** There is
deliberately no repository parameter, for the reason the contract already gives about writes:
naming another repository is the one thing worth forging on a surface whose entire purpose is
that another agent reads it and believes it. A read parameter would hand that over for
free.





Newest last, so an agent appending to its context reads the board in the order it was
written — the order a person reads a thread in.

### `string Post(`

Posts to this session's board by appending one contract line.

**Returns.** What the ingest will do with it, in the agent's terms — including a refusal, stated at call time rather than left to be discovered by a post that never appears.

**Remarks.** **The refusals are reported, not enforced.** This checks the same conditions the ingest
checks and says what will happen; the ingest still decides. Enforcing here would be a second
set of rules to drift from the first — and a caller that refused something the ingest would
have accepted is a path where MCP and JSONL disagree, which is precisely what the equivalence
gate forbids.

### `IReadOnlyList<string> KnownKinds { get; } =`

The kinds an agent may send, spelled the way the wire spells them.

**Remarks.** Derived from `BoardMessageKind` and hyphenated at each interior capital, because
the contract's vocabulary is kebab-case (`knowledge-candidate`) while the enum is Pascal.
Typing the list out would be a second copy to drift (DC-021); deriving it means a new kind is
added once, in the enum.

## `Program`

*class* — `Program.cs`

The stdio MCP server: JSON-RPC on stdin/stdout, three tools, no authority an agent lacks.

**Remarks.** **Hand-rolled rather than taking an SDK.** The Solution-Selection Ladder puts a new
dependency past rung 5, and JSON-RPC over stdio is a hundred lines against
`System.Text.Json`. A package here would be a supply-chain surface and a version to track,
bought for framing that the framework already provides.





**Nothing may be written to stdout but a response.** stdout IS the protocol channel, so a
stray `Console.WriteLine` corrupts the stream and the client reports a malformed server
rather than the message that caused it. Every diagnostic goes to stderr, which the client logs.

| Member | Summary |
|---|---|
| `Task<int> Main(string[] args)` | **(gap)** |

## `ServerContext`

*record* — `ServerContext.cs`

Everything the server resolved about where it is and who is calling — including the absences.

**Remarks.** Resolved once at startup rather than per call, because the answers are properties of how the
process was launched and cannot change under it. The one thing that could — a session ending —
is read from the store on each call instead.





**Every failure here is a state, not an exception.** No workspace, no store, no session:
the server starts anyway and each tool says which of them is missing. A server that refuses to
start tells the agent only that something is wrong, and the agent has no way to find out what.

| Member | Summary |
|---|---|
| `string? DatabasePath` | The store, opened read-only per call. Null when there is none to open. |
| `ServerContext None(string reason)` | A context that resolved nothing, for a self-test or a shell outside AI-DE. |
| `ServerContext Discover()` | Resolves the context from the environment and the working directory. |
| `string Describe()` | One line for stderr, so a failure to identify is visible in the client's log. |
| `IReadOnlyList<SessionRecord> ReadSessions(string? storePath)` | Opens the store read-only, or returns an empty list with the reason. |

### `ServerContext Discover()`

Resolves the context from the environment and the working directory.

**Remarks.** `AIDE_CONTRACT_LOG` gives the coordination directory; the workspace store sits beside it,
because `WatcherHost.Open` puts the coord log inside the workspace data directory. That
relationship is derived from the launcher rather than configured, so there is no second path
to keep in step.

### `IReadOnlyList<SessionRecord> ReadSessions(string? storePath)`

Opens the store read-only, or returns an empty list with the reason.

**Remarks.** **Read-only is a design property, not caution.** The server's claim is that it holds no
authority an agent lacks; a write handle to the fact store would be exactly such an authority,
and would bypass every guarantee the ingest provides. A test asserts the connection string
carries it.

## `IdentitySource`

*enum* — `SessionIdentity.cs`

How the server decided which session is calling, and on what evidence.

## `ResolvedIdentity`

*record* — `SessionIdentity.cs`

The resolved caller, or the stated reason there is none.

| Member | Summary |
|---|---|
| `bool IsResolved` | **(gap)** |

## `SessionIdentity`

*class* — `SessionIdentity.cs`

Decides which AI-DE session is calling, from two independent signals.

**Remarks.** **Two signals, because one of them goes stale silently.** The environment is inherited —
verified 2026-09-04, `spikes/mcp-stdio-environment`: a stdio server sees the launching
client's environment in full, so `AIDE_SESSION` arrives without configuration. But
inheritance is exactly why a shell that outlives its terminal carries a DEAD session id forward,
and nothing in the variable says so.





The same spike found the second signal: the server's working directory is the invocation
directory, and since `c235611` an agent terminal runs in its own git worktree — a path the
store already holds. So identity can be corroborated rather than merely claimed.





**Disagreement is refused, not resolved.** A board post attributed to the wrong agent is
the most damaging thing this surface can do: the board's whole purpose is that another agent
reads it and believes it. When the two signals name different sessions the honest answer is
neither, with both named so the operator can see which is stale.





Pure but for the two ambient reads, which are injected — so the whole decision table is
testable without an environment or a filesystem.

| Member | Summary |
|---|---|
| `string SessionVariable = "AIDE_SESSION"` | The variable AI-DE sets on every terminal it launches. |
| `ResolvedIdentity Resolve(` | Resolves the caller from the environment, the working directory, and the store. |

## `Tools`

*class* — `Tools.cs`

The three tools, their schemas, and the dispatch between them.

**Remarks.** **Every tool answers, including when it cannot do its job.** No tool throws and none
returns an MCP error for a missing session, an unopened workspace or an unreadable store: each is
a state the agent can act on, and a protocol-level error tells it only that something broke. The
distinction matters most for the case that prompted this whole surface — an agent that does not
know whether it is registered needs an answer, not a stack trace.

| Member | Summary |
|---|---|
| `JsonArray Schema()` | The tool list, as `tools/list` returns it. |
| `JsonObject Call(JsonObject? parameters, ServerContext context)` | Dispatches one `tools/call`. |
