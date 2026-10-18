---
id: adr-0042-native-harness-profile-binding
title: "ADR-0042 — Native harness profile binding: harness-owned sign-in, separately observed readiness, per-attempt version fingerprint, sanitized child environment, no broker"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [architecture, security, identity, providers, subscriptions, acp, harness, workbench-loops]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: kb-workbench-loops-20261018, rel: depends-on }
  - { to: adr-0007-agent-session-adapter, rel: refines }
  - { to: adr-0027-acp-lane-separate-shape, rel: refines }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: relates-to }
  - { to: adr-0024-credential-backed-grading-egress, rel: relates-to }
  - { to: adr-0039-workbench-records-durable-representation, rel: depends-on }
  - { to: adr-0041-execution-attempt-lifecycle, rel: relates-to }
review-by: 2027-04-18
summary: >-
  AI-DE runs the user's installed native harness with its own authenticated profile. Sign-in is the
  harness's flow, opened explicitly only when a probe says it is needed. Readiness is a set of
  separately observed facts (installed, authenticated, account, model, capability, version), never
  inferred from exit status or configuration. The child environment is sanitized so an inherited
  API key cannot change the billing route; credentials are never read, copied or stored. Six
  families, three transport shapes, one run-event envelope with raw frames retained.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# ADR-0042: Native harness profile binding

- **Status:** Accepted — owner ruling 2026-10-18 (@timianmalloo); council record follows. Previously Proposed (council gate 2026-10-18; Security hard-veto domain)
- **Date:** 2026-10-18
- **Deciders (Peer Mode):** Security & Identity Architect (author), Domain Researcher (spike),
  Enterprise Architect, SRE
- **Context spec/architecture:** spec US-H01–H02, US-A01, AC-R03c, NFR-06/07/08, §C2 "Native
  connection/profile" states; research note §Subscription-first native harness routes and
  §Working benchmark precedent; `note-workbench-loops-direction` (user-settled: explicit
  harness-owned sign-in; no broker; no API-key-first; no hidden billing fallback).

## Context

Six product targets are in scope (Claude Code, Codex, GitHub Copilot CLI, Muse Code, Grok Build,
Antigravity). AI-DE already states that subscription credentials belong to the engine
(`ProviderRegistry.cs` `ProviderAuth`, lines 22-26) and routes `NeedsLogin` to the native login and a
re-probe (`ProviderRegistry.cs:178-184`); `FirstUse` runs prerequisite checks "before any network"
(`FirstUse.cs:4-8`); `ConfigureProviderDialog` already starts `claude` in its own window for sign-in
(`ConfigureProviderDialog.cs:196-200`) — all Verified 2026-10-18. The engine catalog pins
`claude-code` and `codex` through ACP adapter packages and `copilot`, `gemini` and `grok` as native
ACP commands (`EngineCatalog.cs:204-287`, `AcpMode {Adapter, Native, Observed, Deferred}` :11);
Antigravity and Muse have no row, and `EngineCatalogTests.cs:405` asserts that "antigravity" is
refused — a deliberate deferral this decision reverses with a qualification gate. The only
environment sanitation today strips three variables (`AcpEngineProcess.StrippedEnvironmentVariables`
:189: `CLAUDE_CODE_EXECUTABLE`, `NODE_OPTIONS`, `NODE_PATH`).

The benchmark at `C:/Projects/x-harness-x-model-bench@8f514607` supplies the precedent AI-DE will
reuse and the one it will **not**: `DROP_EXACT` strips inherited `ANTHROPIC_API_KEY`,
`OPENAI_API_KEY`, `GH_TOKEN`, `XAI_API_KEY`, `CLAUDE_CONFIG_DIR`, `CODEX_HOME`, `COPILOT_HOME`,
`CLAUDE_CODE_EXECUTABLE` … before a profile re-adds only its own values (`profiles.py:50-70`,
`cell_env` at `:306-320`, Verified); its `blocked_auth` / `blocked_permission` /
`model_unavailable` causes are *different* decision kinds (`engine.py:67-70`, Verified). Its
copied-credential cell homes and always-approve permissions are a benchmark's choice and are
rejected here.

The presence spike (`spikes/native-harness-presence/RESULT.md`, 2026-10-18) found all six CLIs on
this machine with their documented headless entry, and found **Muse self-updating between two
read-only probes minutes apart** (1.4.7 → 1.4.10). A version observed at install time is therefore
not the version that runs.

LOA principles: P11 least-privilege delegated identity, P2 determinism at the floor (readiness is
observed facts), P8 idempotent side-effect boundary (sign-in is user-initiated, once).

## Decision

1. **The harness owns identity.** AI-DE launches the installed native CLI under the user's own
   native profile. It never negotiates OAuth itself, never shows an API-key form as the first-use
   path, never reads, copies or archives a credential file or keyring entry, and never extracts a
   native token for a generic model client. Where a harness supports an operator-issued
   long-lived token (Claude `setup-token`), it is **never inherited from the environment**: it is
   an opt-in profile value (`harness_profile_dim.operator_token_ref`, a reference into the
   Windows Credential Manager / DPAPI CurrentUser store per ADR-0024 — a ref that names an
   environment variable is **refused**), resolved only at spawn and injected into that family's
   child, shown in Prepare as "identity source: operator token", with the **ref**, never the
   resolved value, hashed into the route fingerprint (§7) — the fingerprint is unchanged when the
   secret behind the same ref rotates and changed when the ref changes.
   The route remains subject to `SpawnContract.Authorize` unchanged; the observed auth `kind` a
   token route reports is a fixture in the Claude qualification record (agent-plane §7.2's
   fail-closed billing gate must still see `account`).
2. **Sign-in is explicit and native.** `check native session → if needed, open the harness's own
   sign-in (window, device code, browser) → re-probe the selected profile`. A probe failure opens
   nothing by itself; the user chooses. For Antigravity the sign-in is the interactive first run
   (no `login` subcommand — spike), so the binding launches that run visibly.
3. **Readiness is a conjunction of separately observed facts.** `profile_observation_fact` rows
   (ADR-0039): `installed` (resolved path), `version` (observed at **every** attempt), `authenticated`
   (native status probe), `account` (plan/org where the CLI reports it), `model-available`,
   `capability:<permission|steer|cancel|resume|read-only>`, `quota`. Readiness for an action = the
   mandatory observations for *that action* are present and true; a missing observation blocks
   that action and is shown as **Not checked**; `quota` may remain **Not recorded**. No reading of
   readiness from exit status or from AI-DE's own configuration (defect class CAP-P, DC-228).
4. **Fail closed on controls.** A route whose `capability:permission` is not observed true gets no
   approval control and no write-capable dispatch; read-only work is offered instead (AC-H02c). No
   synthetic permission/cancel/resume event is ever presented as a native acknowledgment.
5. **Sanitized child environment — one sanitizer, every transport.** `ChildEnvironment.Sanitize
   (base, profileValues)` in Core is the only way a child environment is built; a census test
   asserts every engine `ProcessStartInfo` (ACP adapter, native ACP, stream-json, JSONL) passes
   through it, and `AcpEngineProcess`'s three-name strip plus its throw-on-set (`:189, :252-255`)
   are replaced by it. The deny set is the benchmark's `DROP_EXACT` ∪ `DROP_PREFIXES`
   (`CLAUDE_CODE_`, `CODEX_`, `COPILOT_`, `GIT_CONFIG_`, `GROK_`, `AGY_`, `GEMINI_`, `GOOGLE_`) ∪
   {`CLAUDE_CODE_OAUTH_TOKEN`, `ANTHROPIC_BASE_URL`, `ANTHROPIC_AUTH_TOKEN`, `OPENAI_BASE_URL`,
   `GEMINI_API_KEY`, `GOOGLE_API_KEY`, `GH_HOST`}; **nothing passes through from the inherited
   environment**; the profile re-adds only its own declared values (native home, host, operator
   token ref resolution). The catalog's install text that tells the user to put `GEMINI_API_KEY` /
   `XAI_API_KEY` in the launch environment (`EngineCatalog.cs:281, :314-316`) is rewritten to the
   native login path; a row test asserts no catalog `Arguments` matches `--api-key*|--token*|-stdin`
   (Muse `--api-key-stdin` is thereby unreachable). The inherited-key qualification negative
   **observes the child's environment** for each denied name, not the route label. Benchmark
   always-approve / trust settings are **not** defaults.
6. **Transports project; frames are stored redacted at ingest.** Three wire shapes feed one
   `RunEvent` envelope through per-engine mappers: ACP JSON-RPC over stdio (Claude adapter, Codex
   adapter, Copilot `--acp`, Grok `agent stdio`), Antigravity `--output-format stream-json` NDJSON,
   Muse `exec --json` JSONL. The credential-sentinel scan is an **ingest-side transform inside each
   mapper**: the stored raw frame (`acp.frame` / `native.frame`) is the redacted frame plus
   `redactions: [{offset, len, pattern_id}]` and `scanned_with: patterns v<n>`; unredacted bytes
   never reach the run log, the relay, a Proof Pack or an export. Unknown kinds are carried
   namespaced (existing `RunEvent` rule); native-specific states (denied tool with exit 0,
   policy-driven approval) are projected as their own observed states. A run whose mapper produced
   **only** namespaced-unknown kinds ends `Failed{transport-unrecognised}`, never a silent empty
   run; each transport has a golden-frame conformance check run at every probe.
7. **The route fingerprint — defined here, referenced elsewhere.**
   `route_fingerprint = sha256(engine_id ‖ profile_key ‖ observed_version ‖ adapter_sha? ‖
   sha256(every non-secret env value the profile re-adds, host included) ‖ operator_token_ref? ‖
   observed_account)` — a resolved secret is never part of any stored tuple. Readiness
   requires the observed `account` to equal the profile's declared account. A family is
   "advertised ready" only when a qualification record exists for that exact fingerprint showing a
   native authenticated scoped read-only request plus the negative cases (signed-out, wrong
   account, quota, denied tool with exit 0, inherited-key sentinel observed absent in the child
   env), and the WL-3 oracle is *advertised-ready ⇒ record fingerprint == observed fingerprint*.
   `host` is validated as a hostname (no scheme, no path) and shown on the Prepare destination line.
8. **Diagnostics are redacted, not hidden.** Native stderr and the host's own diagnostics pass
   through the same sentinel/path redaction before any retained sink; a redacted span is
   disclosed (AC-R02a, AC-A01c).
9. **Probes are bounded and serialized.** The six family probes run one at a time with a version/
   help timeout long enough for a self-update banner; a probe that times out records
   `probe.outcome = timeout` as Not checked and does **not** kill the process tree (a kill during
   a self-update can corrupt the install); `profile_observation_fact` carries `duration_ms` and
   `exit_code`. A busy native profile (credential-home concurrency) is a state, not a retry loop.

## Alternatives considered

- **App-owned OAuth / credential broker:** rejected by user direction and by scope — it would make
  AI-DE a credential holder with every family's refresh and distribution policy to honour.
- **Copied credential homes (the benchmark's cell path):** rejected — rotating refresh tokens strand
  other copies (the benchmark's own ADR-0003 amendment) and copies become artifacts.
- **One uniform six-provider permission dialog:** rejected — it invents capabilities a route lacks
  (direction note "rejected directions").
- **ACP as the only transport:** rejected — Antigravity and Muse have no ACP route on the installed
  versions; forcing them through an adapter erases native states (quota, policy denials).
- **Version pinned at install time:** rejected by the spike observation (Muse self-update).

## Consequences

- **Positive:** least privilege by construction; a wrong-profile or inherited-key route is a test
  fixture, not a surprise; the selector honestly shows six targets with real states; no secret
  is ever written before it is redacted.
- **Council record (2026-10-18):** Security **VETO** on the first draft — raw frames persisted
  before redaction (§6 vs §8) and inherited `CLAUDE_CODE_OAUTH_TOKEN` / prefix-family keys passing
  through (§1, §5) — cleared by this revision; Security majors 3 (host in fingerprint), 4
  (decision identity, in ADR-0041 §5), 5 (steer through Prepare, ADR-0041 §4), 8 (one sanitizer)
  and minors 9 (account in readiness) folded in. Posture: this is a single-user local tool and
  the user's machine is their sandbox — the controls above are the floors the spec already
  requires, not new machinery.
- **Negative / accepted trade-offs:** six qualification records to produce and keep current with
  self-updating CLIs; some controls (Steer, acknowledged Stop) will be absent for some families for
  a long time; the first-use flow may need the user to complete a native sign-in in another window.
- **Follow-ups / new risks:** catalog rows for `agy` and `muse` with a new transport kind beside
  `AcpMode` (the refusing test at `EngineCatalogTests.cs:405` goes red first and is re-scoped to
  "refused until its qualification record exists"); the §5 mechanism (one `ChildEnvironment.Sanitize`
  under a census test; the catalog `Arguments` row test) replaces both the three-name strip and any
  resolver-side deny list — `ResolveLaunch` inspects nothing and is not a control point; the
  sentinel scan runs over decoded JSON string values after parse (raw line only for unparseable
  frames) and records that limit in the pattern set's version notes;
  concurrency follows each family's native credential constraints (Grok pool precedent) — a busy
  profile is a state, not an error loop.

## Evidence

- `spikes/native-harness-presence/RESULT.md` — six CLIs resolved, versions, documented entries
  present (`claude -p/--output-format stream-json/setup-token`, `codex exec/app-server/login`,
  `copilot --acp/-p/login`, `grok agent stdio/streaming-json/login`, `agy --print/--output-format
  stream-json`, `muse exec --json/login/serve`) — **Verified** on this machine 2026-10-18; sign-in,
  model access, permission semantics — **Flagged** per family.
- `ProviderRegistry.cs:22-26, 178-184`, `FirstUse.cs:4-8`, `ConfigureProviderDialog.cs:196-200`,
  `EngineCatalog.cs:196-260` — **Verified** 2026-10-18.
- Benchmark `profiles.py:50-70, 306-320`, `engine.py:67-70` at `8f514607` — **Verified** (read).
