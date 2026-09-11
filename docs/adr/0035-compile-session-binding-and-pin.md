---
id: adr-0035-compile-session-binding-and-pin
title: "ADR-0035 — The agentic compile is one pinned ACP session on the session's bound (engine, model, account), composed by a CompileCallHost in AiDe.App/Conductor that is not a run's composition root: it authorizes through SpawnContract.Authorize, pins tools to none through the typed session/new tools argument, and opens a compile-call activity the run-root ledger does not count"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-d"
tags: [architecture, compile, acp, adapter, security, composition-root, agent-plane, addendum-d]
links:
  - { to: architecture, rel: implements }
  - { to: architecture-agent-plane, rel: refines }
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: note-addendum-d-compile-session-tools, rel: supersedes }
  - { to: note-addendum-cd-second-entry-point-ledger, rel: relates-to }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: depends-on }
  - { to: adr-0036-compile-mode-ladder-deployment-gates, rel: relates-to }
  - { to: adr-0027-acp-lane-separate-shape, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  The compile call reuses the agent plane's pieces (EngineCatalog, ProviderRegistry, AcpEngineProcess,
  AcpPeer, AcpLaneClient, SpawnContract.Authorize) in a second, smaller composition — CompileCallHost —
  that never provisions a worktree, opens an episode, constructs a GovernedRunRequest or scores; it
  pins the session's tools to none through Ruling 71's typed session/new tools argument — a sealed
  two-value type (_meta.claudeCode.options.tools: [] primary, the settings deny belt, disallowedTools
  braces, mcpServers: []; disableBuiltInTools dropped as dead) on a pinned triple (adapter sha, SDK
  version, CLI binary sha), and is distinguished in the composition-root ledger
  by opening compile-call.compose, not governed-run.compose. Rejected: a separate assist provider,
  a compile-specific settings.json, trusting the reject-all handler, an in-process SDK call.
---

# ADR-0035: The compile call — one pinned session on the bound engine, composed apart from the run's root

- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 (`addendum-c-chain`) with the
  **Security & Identity Architect** (veto holder — the compile session's tool set, the lease source,
  the envelope's history contents) in Peer Mode; attacked at the gate
- **Context spec/architecture:** `spec-addendum-d-compile-step` page-one facts 2, 4, 5; §A8.2,
  §A13.1–§A13.4 (C1–C6), US-D8; Rulings 13 (one composition root; no second entry point), 65, 68,
  71; `docs/architecture/agent-plane.md` §4 (`GovernedRunHost` as *the* composition root,
  `CompositionRootLedger` as clause 5's oracle); `spikes/compile-session-tool-pin` (source read)

## Context

Ruling 65 fixes the compiler as the session's bound `(engine, model, account)` — the same
subscription-first path a lane takes (spec v1 §4.2), so history never crosses a provider boundary.
The spec's fifth page-one fact corrected the proposal's premise: **a compile session is not toolless
by default** — adapter 0.75.1 resolves the permission mode from the repository's and the user's
settings, gives the SDK the `claude_code` tool preset unless `_meta` says otherwise, and the host's
reject-all `session/request_permission` handler answers only what the SDK routes to `canUseTool`
(*"Claude Code applies bypassPermissions before invoking canUseTool"*) — all **Verified in source**
by `spikes/compile-session-tool-pin` (`acp-agent.js:5274, 5856, 5883-5884, 5962, 6007-6008`, sha
`c22424c2…`). This repository's committed settings allow `Bash(git push:*)` and **the operator keeps
that allow** (relayed 2026-09-11) — so the pin is the *only* control, for the compile session here
and for governed lanes under Ruling 71.

Ruling 13's condition — *the launch path is the same composition root the Surface will later call;
no test-owned wiring, no second entry point* — is enforced by two oracles: the named two-site census
of `GovernedRunRequest` construction (`C16_ExactlyTwoSites…`) and `CompositionRootLedger`, which
counts `governed-run.compose` activities on the source `aide.conductor.composition`
(`CompositionRootLedger.cs:45-80`) **[Verified]**. The compile call is a second *composition of the
same pieces*; the load-bearing question is how it is **distinguished from a run's root** so that
"one run, one root" stays a number.

LOA principles: P3 (the model proposes; no tool executes), P10 (every call receipted), P11
(least-privilege delegated identity — the operator's own subscription, minimum authority: no tools).

## Decision

We will:

1. **Compose the compile call in `AiDe.App/Conductor/CompileCallHost`** — a static
   `CompileAsync(CompileRequest, CancellationToken) → CompileResult` whose order is: `ResolveLaunch`
   → `AcpEngineProcess.Start(launch, cwd: the repository root)` → `AcpPeer` + `AcpLaneClient` (a
   reject-by-kind permission chooser that **counts** every request) → `InitializeAsync` → observed
   auth → **`SpawnContract.AuthorizeBinding(engineId, model, accountLabel, observed, registry)`** —
   the *identity* half of `Authorize` factored out (the direct-api refusal, `Bind`, the
   observed-subscription check and the label match, `GoalBlock.cs:192-247`), with `Authorize`
   itself calling it and its signature unchanged (US-D12). `Authorize` cannot be reused as written:
   it calls `RequireGoalBlock` → `Validate` (`GoalBlock.cs:207, 250-262`), which refuses a block
   missing `goal`/`done_when` — and a compile exists to *fill* those lines (§A9 R0/R1); the
   workaround an implementer would reach for, a placeholder `GoalBlock`, is a spoofed precondition
   on the auth gate (the Security Architect's finding at this gate). The same `AP-0009`–`AP-0013`
   refusals apply from both entry points, so a compile can never bill an API key while a
   subscription is configured, and `needs-login` degrades the compile (§A10.2) →
   **`NewSessionAsync(cwd, SessionTools.None)`** with the pin → `PromptAsync(compile prompt)` →
   drain frames, **counting `tool_call` and `permission_requests`** → close → return the raw text,
   usage, model observed, counts. **The 60 s compile bound (`opened.constants.bound_ms`) is one
   linked deadline over the whole of `Start` → close**, not a per-step timeout: `initialize`,
   `session/new` and the prompt each run under the same remaining budget (`AcpPeer`'s own 60 s
   request timeout and 15-minute prompt timeout, `AcpPeer.cs:50-72`, would otherwise let a wedged
   adapter sit at ~61 s with no degraded state and, worst case, ~3 minutes), and a step that
   outlasts it yields `timed_out` with `reason` naming the step (the SRE's finding). **On
   `timed_out` and `cancelled` the engine is disposed — the adapter's process tree reaped
   (`AcpEngineProcess`'s tree kill and job-object backstop, `AcpEngineProcess.cs:22-35, 213-240`) —
   before the `called` row is appended**; the row carries `adapter_pid` and, when an answer arrives
   after the deadline, `late_answer: true` (discarded, counted). **Before `Start`, the host computes
   the installed adapter's sha and the CLI binary's sha and returns `unavailable` (reason: *the pin
   is unverified for this adapter build*) on any mismatch with the pinned triple** — the pin is
   verified at every call, not only at rung selection (`ResolveLaunch` reads no version or sha,
   `EngineCatalog.cs:130-158`); the check's cost is emitted as `pin_verify_ms` on the compile stage
   and is not cached until 50 measured compiles say what it costs. One compile in flight per draft
   (the spec's rule); the in-flight count across drafts is the number of open `compile-call.compose`
   spans (no second emitter of one quantity — the Simplifier's cut), and an app-level bound is not
   set in v1. A re-prepare that
   reuses stored derived decorations (unchanged `inputs_sha` after a success) makes **no**
   `session/new` and appends a `called` row with `outcome: reused`, `reused_from: {envelope_id,
   seq}`, **`cost: {requests: 0, tokens_in: 0, tokens_out: 0, cache_read: 0}` and `reused: true`**
   (a known zero, never `null` — `null` means *not recorded*; a cancelled row's spend is genuinely
   unknown and reads `spend: unknown (cancelled)`), the copied `derived` rows carrying `call_seq` →
   that row — so C3/C10's receipt exists for every envelope and the eval can dedup by the
   originating call (the AI Systems Engineer's finding). It **never** provisions a worktree, opens an
   episode (`GovernedLaneSource`), constructs a `GovernedRunRequest`, runs `LeaseMonitor`, or
   scores.
2. **Pin the session through Ruling 71's typed tools argument — `LaneSessionOptions`, as
   delivered.** `AcpLaneClient.NewSessionAsync` gained, on `feature/exit-evidence` at `246b38a3`
   (**[Verified — read on the branch; not yet on `main`]**), the typed record
   `LaneSessionOptions(IReadOnlyList<string>? Tools, IReadOnlyList<string>? DisallowedTools)` whose
   `ToMeta()` serialises **exactly the two members the adapter spreads** into
   `_meta.claudeCode.options` and nothing else — the F5 node's own reason (`docs/notes/lane-pin-spike.md`
   on that branch: *"a wider record would be a wider reach"*), which is the Security Architect's
   condition at this gate in the delivered form: every widening vector lives in the same `_meta`
   (`settingSources` spread after, `:5962-5964`; `systemPrompt` `:5840`; `env` `:5942`; `extraArgs`
   → arbitrary CLI flags `:6004-6006`; `mcpServers` merged `:5995-6001`; `settings` `:5921`), and a
   record with a free-form member is a hole a later passthrough fills without failing a test. The
   compile passes **two named static instances**, not ad-hoc lists — `LaneSessionOptions.Compile
   = (Tools: [], DisallowedTools: DeniedToolNames)` and `LaneSessionOptions.Lane = (Tools: null,
   DisallowedTools: ["Bash"])` (Ruling 71) — and the wire test asserts the **exact key set** of
   `_meta` and of `_meta.claudeCode.options` (`{tools, disallowedTools}` for the compile;
   `{disallowedTools}` for the lane), not only the pinned keys. **`tools: []` is the primary pin**
   (the adapter prefers the array, `acp-agent.js:5880-5884`; the SDK's `tools` accepts `[]` =
   disable all built-ins, `sdk.d.ts:1497-1505` per the lane-pin note). **The belt** — a
   programmatic settings deny, `_meta.claudeCode.options.settings = { permissions: { defaultMode:
   "default", deny: DeniedToolNames }, disableAllMcpJsonServers: true }` (a deny rule wins regardless
   of `tools`, and the MCP key closes a repository `.mcp.json` structurally, which `tools: []` does
   not) — **is not in the delivered record and is admitted only by P-D5**: the spike runs with and
   without it; if `tools: []` alone yields zero `tool_call` frames with a repository `.mcp.json`
   present, the belt is never built (the record stays two members); if it does not, the belt is
   added as a third member with its key names (Inferred today) observed on the wire, and the
   exact-key-set test grows by one key. The tool names are **one constant, `DeniedToolNames`**,
   referenced wherever they appear (hand-listing them twice rots on the next SDK tool); `_meta.disableBuiltInTools: true` is **dropped as dead code** (`tools ??
   (disableBuiltInTools …)` at `:5883-5884` never evaluates its right-hand side when `tools` is
   present — the Security Architect's finding); **`disallowedTools`** naming the write tools
   (braces); **`mcpServers: []`**; plus the reject-by-kind handler. The lane path is otherwise
   unchanged (a lane needs its tools). **The pin is enforced by the Claude Code CLI binary the SDK
   ships, not by the adapter** — `acp-agent.js` only forwards `tools` (`:6008`) and launches the CLI
   at `pathToClaudeCodeExecutable: process.env.CLAUDE_CODE_EXECUTABLE ?? claudeCliPath()`
   (`:6003`, `:374-400`), and the child inherits AI-DE's environment untouched
   (`AcpEngineProcess.cs:116-124`). So the pin is a **triple**: adapter 0.75.1's
   `dist/acp-agent.js` sha, the SDK version, and the **CLI binary's sha**, all recorded on the P-D5
   artifact and on every `called` row as observed; `CompileCallHost` (and the lane) **remove
   `CLAUDE_CODE_EXECUTABLE` from the child environment** (or refuse with the reason when it is set);
   any element of the triple changing re-runs `spikes/compile-session-tool-pin` and P-D5, and the
   settings model refuses every agentic rung while the recorded triple differs from the installed
   one (US-D11).
3. **Distinguish the compile call in the root ledger by construction, not by exclusion.**
   `CompileCallHost` opens **`compile-call.compose`** on the **same** activity source
   (`aide.conductor.composition`) — so `CompositionRootLedger.Roots` (which counts only
   `governed-run.compose`) reads **0** for a compile and **1** for a run whose prompt was compiled.
   **No `CompileCallLedger` is written** (the Simplifier's cut; the Tech Lead's ruling): the
   compile is already counted by the durable `called` row (P-D4 reads it) and the span; a fourth
   copy of the `ActivityListener` ledger idiom (`CompositionRootLedger`, `SessionDisposalLedger`,
   `TerminalHostingLedger` exist) with no test reading it is dead code. If D-2's tests ever need an
   in-process count it is `ActivityCountLedger.Open(source, name)` in Core as new code; the fold of
   the three existing ledgers is **tracked debt** with the trigger *"the next edit to any of the
   three"*, not this slice's work. `C16`'s named census stays at `["ComposerSendGate.cs", "ConductorEntry.cs"]`
   because `CompileCallHost` constructs a `CompileRequest`, never a `GovernedRunRequest`. A
   **negative-reference census** — enumerated **once** in the test as one list, run over the
   **whole file set `src/AiDe.App/Conductor/Compile*.cs`** (a single-file census is satisfied by a
   helper file the host calls — the Test Architect's finding), and asserting the set is non-empty
   before counting (a missing file is a zero-hit green) — finds none of `GovernedRunHost`,
   `GovernedRunRequest`, `GovernedLaneSource`, `WorktreeProvisioner`, `LeaseMonitor`, `LaneScoring`,
   `IngestHost`, `SpawnContract.Authorize(`, `new GoalBlock(`, `SessionTools.Lane`, `preset`; the
   last four because a compile host that could authorise with a placeholder block or open a lane's
   tool set is an ungoverned lane in the primary checkout wearing the compile's name (the ledger
   distinguishes what *counts*; the sealed type and this census are what keep the pin unvariable
   from here). The full argument is `note-addendum-cd-second-entry-point-ledger`.
4. **What reaches the model is what §A13.2 says and nothing else:** the fixed host header first
   (`/…` typed as a prompt would execute as a CLI command — `acp-agent.js:6034-6035`), the fenced
   `source_text`, the mechanical facts as display, the open lines, the profile body, the history
   window's admissible classes, the constitution **by manifest reference only** (the harness loads it
   through `settingSources: ["user","project","local"]`, `:5962`; the host inlines nothing);
   attachments by reference; never `providers.json` values, never `session.json` beyond the task
   class enum. `settingSources` is **not** overridden through `_meta` (the spread order at `:5964`
   would allow it; dropping `project` drops `CLAUDE.md`).
5. **The typed boundary is the only reader of the model's text** (ADR-0033's
   `CompileOutputValidator`); a non-zero `tool_calls` or `permission_requests` marks the compile
   `suspect` (still agentic, still in the eval's treatment arm — §A10.2). **Under `agentic`, a
   `suspect` envelope reads with advisory semantics** — `Confirmed()` skips its `derived` rows until
   the operator keeps or edits them, so one Send gesture never pre-confirms lines from a call that
   acted — **and it is a drift trigger** (ADR-0036): a non-zero count post-admission is evidence
   that Gate 1's premise no longer holds on this machine, so the correct containment is to close the
   door, not only to show a mark.
6. **The hooks residual is measured before it is accepted:** the repository's
   `UserPromptSubmit`/`SessionStart` hooks run inside the compile process and see the whole compile
   prompt on stdin (*"its stdout is injected into the model's context"* is **Inferred until P-D9**);
   P-D8's `.aide/` marker probe attributes a marker arrival to the hook path rather than a model
   read. The spec's rationale — *the same exposure as opening Claude Code there* — is **not
   assumed**: interactive Claude Code gates project settings and hooks behind its folder-trust
   prompt, while the SDK's headless path (`settingSources: ["user","project","local"]`, `:5962`)
   may have no such gate, and AI-DE opens any cloned workspace. **P-D9's fixture repository is a
   directory never trusted in `~/.claude.json`**, and the run records whether the hook fires. If it
   fires, the agentic rungs additionally require a **per-workspace operator trust affirmation**,
   recorded as an `operator` row before the first agentic compile in that workspace (the gate
   Claude Code itself has); if it does not, the rationale holds and the record says so. Either way
   the outcome is a recorded decision, never an assumption.

## Alternatives considered

- **A separately configured assist provider (Addendum B `:152`):** rejected — Ruling 65; the
  history window would cross a provider boundary; no `assist:` node exists or is built.
- **Composing the compile through `GovernedRunHost.RunAsync` with a "compile" flag:** rejected —
  the run root provisions a worktree, opens an episode and scores; a flag that skips two-thirds of
  the root is a second root wearing the first's name, and it would count as a run in every ledger.
- **A compile-specific `.claude/settings.json` or running outside the repository:** rejected — the
  adapter reads the cwd's settings (a second file is overridden by `local`); outside the repository
  the constitution does not load, which is the whole point of the harness as compiler.
- **Trusting the reject-all permission handler as the tool control:** rejected — Verified in source
  that auto-allowed and bypassed tools never reach `canUseTool`.
- **Calling the Claude Agent SDK in-process instead of through the ACP adapter:** rejected — spec v1
  §4.2 makes the ACP subscription path the only model path; an in-process SDK call is a direct-API
  path (`SpawnContract.Authorize` refuses `direct-api` by design) and a second transport to secure.
- **`CompileCallHost` in `AiDe.Core/Compilation`:** rejected — process spawning, provider files and
  the adapter install root are the App's composition concerns (as `GovernedRunHost` is App); Core
  keeps the pure half.
- **A cheaper same-account model for the compile (Ruling 65 fixes the provider and account, not the
  model):** not chosen for v1 — one bound model means one corpus, one admission report and one gate
  key (ADR-0036 keys on `model_configured`); a second model would need its own admission. Recorded
  as a decision with a revisit trigger: `called.cost`'s share of a session's measured spend.

## Cost model (LOA Appendix F; every figure Inferred until P-D4 measures it)

| Quantity | Per compile (agentic rung) | Basis |
|---|---|---|
| Process spawn | one `node` adapter + one `claude` CLI grandchild, reaped at close | `AcpEngineProcess` (the same spawn a lane pays) |
| Tokens | `prefix_measured` (the harness prefix as billed — the repository's constitution; the tokenizer estimate is 88,380 and is **not** the meter) + inputs ≤ 20k + output ≤ 4k | ADR-0036 Gate 2's token floor |
| Requests | 1 against the plan window (0 on `reused`) | `called.cost.requests` |
| Wall time | ≤ 60 s linked deadline; p95 target 15 s once measured | `called.latency_ms` |
| Pin verification | one adapter-file sha + one CLI-binary sha per call — `pin_verify_ms`, no cache until 50 measured | rule 1 |
| Expected volume | one compile per Send under an agentic rung; a re-prepare with unchanged inputs is free; `mechanical-only` (the default) costs nothing | Ruling 67 |
| Bound | the operator's subscription window — `quota-degraded` account health is the observable when it is hit; no direct-API path exists | Ruling 65; spec v1 §4.2 |

Run-rate: **unmodelled until P-D4** (the first 50 compiles, with `n_measured / n_total`); the
operator's "I did not ask for that compile" constraint is met fully only by `mechanical-only`.

## Consequences

- **Positive:** one model path, one authorization gate, one adapter pin for lanes and compiles; the
  root ledger's number stays meaningful; a compile that acts is visible (`suspect`) rather than
  silently clean.
- **Negative / accepted:** a second process spawn per compile (the harness prefix is paid per call —
  measured as `prefix_measured`, never modelled); the repository's hooks see the compile prompt
  (accepted for the operator's own repository, the same exposure as opening Claude Code there).
- **Follow-ups / new risks (for `/design-slice`):** the compile host prescribes the same handshake
  prefix `GovernedRunHost.RunAsync` already has (`ResolveLaunch → Start → AcpPeer → AcpLaneClient →
  InitializeAsync → observed auth → AuthorizeBinding`), and the negative census forbids
  `Compile*.cs` from touching `GovernedRunHost` — so the duplication would be *enforced*, not merely
  accepted (the Enterprise Architect's finding). `/design-slice` decides between **one
  `Core/AgentPlane` function both roots call** (`BoundSession.OpenAsync(launch, cwd, SessionTools,
  deadline)`, each root opening its own `*.compose` activity so `Roots` is unaffected — the census
  forbids the run-root *types*, not a Core function) and recording the duplication with a drift
  control. **The Tech Lead's ruling: record the duplication, defer the shared function** — the
  compile's *linked* deadline and the lane's per-step timeouts differ, so the shared function
  would carry a parameter only one caller uses, and refactoring `GovernedRunHost.RunAsync` inside
  the aside's slice puts the F5 oracle in the same diff as the thing it must be distinguished from.
  **Drift control:** a recording-fake test on *both* hosts asserts `AuthorizeBinding` precedes
  `NewSessionAsync`; the pin-triple check lives in the already-shared `ResolveLaunch`/`Start`.
  Trigger for the fold: a third caller, or any change to the handshake order. Likewise the **pin
  triple is engine
  data** and belongs on the `EngineCatalog` row, verified in `ResolveLaunch`/`Start` for both roots
  — the lane relies on the same CLI-enforced pin and today never verifies it; whether a lane
  *refuses* on a triple mismatch is Security's call at `/design-slice`. Ruling 71's typed argument
  **has landed on `feature/exit-evidence`** (`246b38a3` `LaneSessionOptions`; `135e05e1` records the
  outgoing `session/new` frame on the normal path) and is **not yet on `main`** **[Verified — read
  on the branch]** — P1 orders that branch's merge before the compile host (C-0); the P-D5 wire
  spike is the first gate of ADR-0036 and has not run (no model call was made here).

## Falsifying tests

1. **Wire (headless, fake peer):** `session/new` from `CompileCallHost` carries **exactly** the key
   set `{cwd, mcpServers: [], _meta: {claudeCode: {options: {tools: [], disallowedTools: [...]}}}}`
   and nothing else (no `disableBuiltInTools`, no `settingSources`, no `extraArgs`, no `env`, no
   `systemPrompt`; `settings` only if P-D5 admits the belt); the lane's `session/new` carries
   `disallowedTools: ["Bash"]` and no `tools` member; `LaneSessionOptions` has exactly the two
   members (a reflection assertion) and the compile uses the named static instance; the child
   `ProcessStartInfo` has no `CLAUDE_CODE_EXECUTABLE`; the adapter
   sha, SDK version and CLI binary sha match the pinned triple, and a one-byte-different
   `acp-agent.js` in a fixture install root yields `unavailable` with **no** `session/new` (US-D8).
2. **P-D5 (runtime, the gate):** adapter 0.75.1, a fixture repository allowing `Bash(*)` and `Edit`
   with a stdio server in `.mcp.json`, a pinned session, the read prompt and the hostile history line
   → zero `tool_call` frames of any name (`mcp__*` included), no file, no push; recorded with the
   adapter sha. A failed P-D5 is a hard stop for every agentic rung.
3. **Ledger:** one `CompileAsync` inside an open `CompositionRootLedger` reads `Roots == 0`; one
   compile followed by one `GovernedRunHost.RunAsync` reads `Roots == 1`; `C16` stays at the two
   named files; the negative-reference census over the non-empty `Conductor/Compile*.cs` set finds
   none of the eleven names in its one list.
4. **AuthorizeBinding:** a compile with an explicit direct-api engine, an absent observed
   subscription, or a contradicting `ObservedAuthLabel` returns `refused` with the same
   `AP-0009`–`AP-0013` reason a lane gets, and makes no `session/new`; a compile with an **empty
   structure** (R0 — no goal block yet) is *not* refused by the binding gate; `Authorize`'s
   signature and behaviour are unchanged (it calls `AuthorizeBinding` then `RequireGoalBlock`).
5. **Prompt (P-D3 golden):** the compile prompt's first bytes are the host header; it contains no
   `providers.json` string, no attachment body, no `CLAUDE.md` body; a workspace file named like the
   template changes no byte.
6. **Counts:** a fake peer emitting one `tool_call` frame yields `called.tool_calls == 1` and
   `outcome == suspect`; a `session/request_permission` is answered with the reject option and
   `permission_requests` increments.
7. **Bound:** `bound_ms` injected at 100 ms with a peer that answers late → `timed_out`, the late
   answer discarded and counted (`late_answer: true`), the engine disposed before the row is
   appended (no 61-second test); **a peer silent at `initialize` degrades at the same injected
   bound with `reason` naming `initialize`** — one linked deadline, not three.
8. **Reuse receipt:** a re-prepare with an unchanged `inputs_sha` after a success makes zero
   `session/new` and appends one `called` row with `outcome: reused` and `reused_from`; the copied
   `derived` rows carry `call_seq` → that row; after a failed call, no reuse (one request).
9. **Suspect post-admission:** under `agentic`, a compile whose peer emitted one `tool_call` frame
   followed by Send with no keep → `GovernedRunRequest.Goal` blank (advisory semantics) and the
   settings model demotes at the next open with the count in the reason.

## LOA mapping

Tier **T3** for the one call; **T0** for everything around it. Patterns (named as the Patterns
Expert corrected them): **least-privilege capability restriction** (the model is given *no* tool
surface — the *inverse* of 5.1 is not 5.1, and no name is claimed for it); **Policy-Bound Egress +
Capability-Based Security + Principal Propagation** (the repo's own Applied patterns; the operator's
subscription as the acting principal, `AuthorizeBinding` at the boundary) — **not** Sandboxed
Executor (5.2): the compile CLI runs in the primary checkout, inherits AI-DE's environment bar
`CLAUDE_CODE_EXECUTABLE`, and runs the repository's hooks; nothing is isolated, the tool set is
*denied*, and a sandbox (job object + restricted token) would be a new decision, not a label;
**Facade** (`CompileCallHost` over Start → Peer → Client → `AuthorizeBinding` → `session/new` →
prompt → close, the run root's twin); **Gateway** (`AcpLaneClient`) with **`SessionTools` as a
typed Parameter Object realised as a C# Smart Enum / closed sealed type** (two static instances, no
free-form members; the exact-key-set wire test is the idiom's oracle); **memoized model call,
content-addressed by `inputs_sha`** (the `reused` receipt — post-success lookup, not 2.1 Semantic
Cache and not 5.3's reserve-before-execute; C6's key claim is qualified so); **Receipt Ledger
(4.3)** (the `called` row: model configured/observed, cost, latency, contract and prompt shas);
the `compile-call.compose` span is the **Observer-via-`ActivityListener`** idiom the three
existing ledgers listen on — no new ledger (rule 3). Conformance: C1 (`[CapabilityTier(T3)]` on
`CompileCallHost` **with a reader** — a test enumerates every type carrying the attribute and
asserts each opens a `*.compose` receipt, so the attribute is a control rather than prose in
attribute syntax; the Tech Lead's condition), C2 (the compile bound
and the token thresholds are the budget), C3, C5, C11.

## Evidence

- **Verified (source, `spikes/compile-session-tool-pin/RESULT.md`):** adapter 0.75.1
  `acp-agent.js:5274, 5856, 5880-5884, 5962-5964, 6003-6008, 6034-6035`; sha
  `c22424c297429378166524b59ee6bed239c999a420ad0dd06571b768efa7525b`.
- **Verified (read at `a3f760a3`):** `GovernedRunHost.cs:55-150` (the run root's order);
  `CompositionRootLedger.cs:1-80`; `AcpLaneClient.cs:152-189, 197-261`; `GoalBlock.cs:191`
  (`Authorize`); `TheSendVerbIsHostOwnedTests.cs:189-213`; `origin/feature/exit-evidence` at
  `757af057` (`AcpLaneClient.cs` unchanged there).
- **Flagged:** the P-D5 wire observation (not run — no model call in this node); the CLI binary
  honouring `tools: []` (the enforcement point, outside the adapter source read); the `settings`
  belt's key names and precedence.
- **Citation correction carried:** the spec's quoted adapter comment (*"canUseTool is not guaranteed
  to run…"*) does not appear in this build; the mechanism is at `:5274-5279` and `:5856`.
- **Spec supersessions this ADR records (quoted; findings for the Owner — the spec is outside this
  node's write scope):** §A13.4 C1 (`:486`) and US-D8 b2 (`:570`) require `_meta.disableBuiltInTools:
  true` as the belt — superseded by the `settings` deny belt, the shorthand being dead beside
  `tools: []`; §A22 row 5 (`:664`) says the lane path is unchanged — superseded by Ruling 71
  (`disallowedTools: ["Bash"]` on the lane's `session/new`, red-first).
- **Sha domain:** every sha in the pin triple is over **raw bytes** (`read_bytes()`), never
  newline-normalised text; the spike script hashes the same way.
