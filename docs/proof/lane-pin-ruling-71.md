---
id: proof-lane-pin-ruling-71
title: "Proof Pack — Ruling 71's lane pin: the governed lane's session/new carries disallowedTools [\"Bash\"]"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [proof-pack, agent-plane, acp, ruling-71, f5, security, phase-1]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: note-lane-pin-spike, rel: relates-to }
  - { to: proof-conductor-agent-plane, rel: refines }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Evidence that AcpLaneClient.NewSessionAsync now takes a typed LaneSessionOptions {Tools?,
  DisallowedTools?} on both overloads, that the governed lane sends
  _meta.claudeCode.options.disallowedTools ["Bash"] on its session/new (red-first on the outgoing
  frame), that the null path is byte-identical to every prior run's frame, and that every session
  opened from src/ names its tools. The wire observation is the F5 run's own Proof Pack.
---

# Proof Pack: Ruling 71's lane pin

- **Change:** `feature/exit-evidence` — `src/AiDe.Core/AgentPlane/AcpLaneClient.cs` (`LaneSessionOptions`, both `NewSessionAsync` overloads), `src/AiDe.App/Conductor/GovernedRunHost.cs` (`GovernedLaneSession`, the one site), `tests/AiDe.Core.AcpProbe/Program.cs` (a named argument, no behaviour).
- **Ruling / spec:** `note-addendum-c-council-rulings` Ruling 71 (a)–(c); the adapter contract in `note-lane-pin-spike` (adapter 0.75.1, `acp-agent.js:5859-5860`, `:5883-5884`, `:5964`, `:6007-6008`; SDK `sdk.d.ts:1465-1469`, `sdk-tools.d.ts:750`).
- **Tier:** T1. **Session:** `f5-lane-pin`. **Date:** 2026-09-11.
- **Tests:** `tests/AiDe.Core.Tests/AgentPlane/AcpLaneClientTests.cs` (+6 cases) and `tests/AiDe.App.Tests/Conductor/TheGovernedLaneHasNoShellTests.cs` (+3). Full suites: **Core 2250/2250**, **App 603/603**; `verify-test-run.py --no-run` OK against floors 2239 / 554; every build `0 Warning(s)` under `TreatWarningsAsErrors`.

## Claims & evidence

| # | Claim | Evidence (test) | Source | Oracle — why it can fail | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|---|
| 1 | With `DisallowedTools: ["Bash"]` on the worktree overload, the outgoing `session/new` params are `{cwd, mcpServers: [], _meta: {claudeCode: {options: {disallowedTools: ["Bash"]}}}}` — that member, no `tools`, keys in that order | `TheGovernedLanePinsBashOffThroughTheAdaptersMetaSlot` | `AcpLaneClient.cs` `NewSessionAsync` + `LaneSessionOptions.ToMeta` | Reads the frame the peer wrote to the engine's stdin, not the argument; `Assert.IsType<JsonObject>(parameters["_meta"])` fails on an absent slot | **Yes** — on the plumbing-only client (record present, not emitted): `NullReferenceException` at the `_meta` read, then tightened to the typed assert | Verified | Whether the adapter honours the member is contract (source), not wire — Ruling 71 cond. 2 |
| 2 | With no record, or an empty record, the frame is **byte for byte** `{"jsonrpc":"2.0","id":1,"method":"session/new","params":{"cwd":"…","mcpServers":[]}}` — no `_meta` | `WithoutSessionOptionsTheSessionNewFrameIsByteForByteTheOneEveryPriorRunSent` (×2: absent, `new LaneSessionOptions()`) | `NewSessionAsync`, `ToMeta` returns `null` iff both members null | Exact string equality of the whole line; any added key, reordering or whitespace fails | Characterization: **green on the unchanged client first**, green after — the point is that it never went red | Verified | — |
| 3 | `Tools: []` reaches the slot as an empty array (Addendum D's C1 shape), without `disallowedTools` | `AnEmptyToolsListReachesTheMetaSlotAsAnEmptyArray` | `ToMeta` | An empty list dropped as "nothing to say" fails `Assert.Empty(options["tools"].AsArray())` on a null | **Yes** — `NullReferenceException` on the plumbing-only client | Verified | — |
| 4 | A blank tool name is refused before anything is sent | `ABlankToolNameIsRefusedBeforeAnythingIsSent` (`""`, `" "`) | `LaneSessionOptions.Names` → `ArgumentException.ThrowIfNullOrWhiteSpace` | Asserts the exception type **and** `Assert.Empty(Output.Lines)` | **Yes** — before the guard the frame went out and the request timed out (`AgentPlaneException`) | Verified | A misspelled or mis-cased name is not refused (Security lens finding 4) — moot for the constant, covered by the adapter-bump re-read |
| 5 | The governed lane's pin is exactly `["Bash"]` with no base set | `TheGovernedLanesPinIsExactlyBashAndNothingElse` | `GovernedRunHost.GovernedLaneSession` | Value equality; `Tools` must be `null` | **Yes** — mutated to `new()`: `Expected ["Bash"] Actual null` | Verified | — |
| 6 | The host's one `NewSessionAsync` site passes `GovernedLaneSession` | `TheOneSessionSiteInTheHostPassesThePin` | `GovernedRunHost.cs:149` | Source-text oracle (the host starts a real engine before the session, so the frame cannot be captured headless); `Assert.Single` over the regex, then the exact argument list | **Yes** — mutated to the bare form: `Expected "worktree, GovernedLaneSession, …" Actual "worktree, cancellationToken: …"` | Verified | Text, not wire — claim 1 is the wire half |
| 7 | Every session opened from `src/` says what tools it holds (three arguments, never `null`) — the DC-019 control: the boundary, not the site | `EverySessionOpenedFromSourceSaysWhatToolsItHolds` | every `.NewSessionAsync(` in `src/**/*.cs` outside the client's own delegation | `Assert.NotEmpty(sites)` (non-vacuous), three parts, second not `null` | **Yes** — the bare-form mutation: *"GovernedRunHost.cs: NewSessionAsync(worktree, cancellationToken: cancellationToken) does not say what tools the session holds"* | Verified | A hand-rolled `RequestAsync("session/new", …)` outside the client would not be seen — none exists (Security lens, Verified) |
| 8 | `ConductorEntry.cs`, `.claude/settings.json`, the frozen oracle `tools/verify-front-door-exit-evidence.py`, the vendored bundle: untouched | `git diff 1374401d HEAD -- tools/verify-front-door-exit-evidence.py` empty; `git status --short` names none of them | — | Byte diff | n/a | Verified | — |
| 9 | Nothing widens: the record is `sealed` with two members; `ToMeta` emits only `claudeCode.options.{tools,disallowedTools}`; at adapter `:5964` those two keys collide with nothing security-relevant (`permissionMode`, `canUseTool`, `cwd`, `mcpServers` are assigned *after* the spread) | Security lens review (read-only, Adversary Mode) | `AcpLaneClient.cs:88-129`; `acp-agent.js:5961-5982`, `:6007-6008` | Reviewer re-read the adapter lines; the record's shape bounds the reach | n/a | Verified | Operator `user`/`local` settings not recorded |

**Boundary set:** absent record · empty record · `DisallowedTools: ["Bash"]` · `Tools: []` · blank name (`""`, `" "`) · the worktree overload · the string overload · a second caller in `src/` (swept) · a `null` options argument at a call site (swept).

**Mutation sense:** claims 5–7 were each turned red by mutating the host (no pin; bare call) and restored; claims 1, 3 and 4 were red on the plumbing-only client before `ToMeta` existed.

## Security lens verdict (read-only, Adversary Mode)

**CLEARED WITH FINDINGS** — no blocker; the change strictly narrows the lane's privilege and adds no tool, dependency or reach. Findings, none blocking this change:

1. **[Major, Inferred]** "No `Bash` tool" is not "no command execution": `Monitor.command`, `REPL`, `Agent` subagents (inherit the parent's tools), and project/user **hooks** (`.claude/settings.json:9-19` runs inside the lane via `settingSources: ["user","project","local"]`, `acp-agent.js:5962`) remain reachable. Ruling 71 freezes the shell only; these are the next ruling's — the record already supports an explicit `tools: [...]` allow-list.
2. **[Major, Inferred]** A lane that edits `docs/ai-forward-pack/hooks/reread-guard.py` in its worktree gets code execution on its next `Read`, with no `Bash` tool involved — rule on hook loading for governed lanes (a `settingSources` without `project`, or `.claude/**` and the hooks directory outside the lease).
3. **[Minor, Verified]** The site oracle read only `GovernedRunHost.cs` — **closed in this change** by claim 7's sweep over `src/`.
4. **[Minor, Flagged]** A mis-cased name pins nothing and is not refused — moot for the constant; the spike note's adapter-bump re-read covers name drift.
5. **[Nit]** Red-first is a process claim the diff cannot show — answered above: every claim's red is recorded with its observation.

## Finding for the operator (Ruling 71 (c) — not edited)

`.claude/settings.json:4` allows `Bash(git push:*)` without a prompt. Every governed lane runs in a worktree of this repository, and the adapter resolves the SDK's permission mode and allow-list from `["user","project","local"]` (`acp-agent.js:5856`, `:5962`) — so before this change any lane that held `Bash` could `git push` from its worktree under the operator's git identity with no `canUseTool` gate reaching the conductor. The pin removes the `Bash` tool from the model's context entirely, so that line has no tool to apply to; keep or drop is the operator's call and the lane is independent of it either way. The same file also contributes **hooks** to the lane (finding 2), which the pin does not neutralize.

## Instrumentation — the frame is recorded on the normal path (closed on the coordinator's instruction)

Ruling 71 (a) requires the F5 run's Proof Pack to record the **outgoing frame**. The host now records
it without anyone remembering to: `GovernedRunHost.OpenSessionAsync` — the one site — reports
`acp session <id> opened with session/new params <json>` on the run's report and emits a
`lane.session-new` workbench log line (`run`, `lane`, `session`, `params`) to
`%LOCALAPPDATA%\AiDe\logs\workbench-YYYYMMDD.log`. The value written is
`AcpLaneClient.SessionNewParameters` — the object the client handed the peer, the outbound mirror of
`AcpPeer.ObservedAuth` — never a re-computation; a client that recorded nothing reads as
`not recorded`.

| # | Claim | Evidence (test) | Oracle — why it can fail | Red observed | Confidence |
|---|---|---|---|---|---|
| 10 | What the client says it sent is what went down the wire | `TheGovernedLanePinsBashOffThroughTheAdaptersMetaSlot` (added assert) | `SessionNewParameters.ToJsonString()` equals the wire's `params` | **Yes** — `Expected {"cwd":… Actual null` before the client set it | Verified |
| 11 | The report line and the `lane.session-new` log line carry the exact params the peer wrote, `_meta` included, keyed by run/lane/session | `TheFrameTheLaneWasOpenedWithIsRecordedOnTheReportAndInTheLog` (real client, real peer, the engine's stdin captured) | Compares the recorded params against the frame the peer actually wrote — a re-computation would still have to match the wire | **Yes** — `Sub-string not found` on the step-A host that reported only the id | Verified |

## Not built (residual)

- The wire observation itself: adapter 0.75.1 honours `disallowedTools` in source (`:6007`); no frame has been exchanged with a running adapter under the pin. The F5 attended run records the frame, every observed tool-call name, and `origin/main`'s sha before and after (Ruling 71 cond. 2); the general closure is Addendum D's P-D5 spike.
- The second caller (Addendum D's C1 with `tools: []`) — the record and claim 3 are ready for it.
