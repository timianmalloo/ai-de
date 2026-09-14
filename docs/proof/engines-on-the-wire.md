---
id: proof-engines-on-the-wire
title: "Proof Pack — engines on the wire: copilot, codex, gemini, grok"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, engines, acp, copilot, codex, gemini, grok, ruling-97, ruling-105, proof-pack]
links:
  - { to: spike-engine-backends-2026-09-14, rel: tested-by }
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: note-conductor-spec-errata-providers-json, rel: relates-to }
  - { to: spec-conductor, rel: tested-by }
review-by: 2026-12-14
review-suggested: []
summary: >-
  Rulings 97 (ii)(iii) and 105 landed as four commits on lane/agentplane-engines, in the ruled
  order copilot → codex → gemini → grok: EngineCatalog carries five rows and two launch paths
  (adapter under node; a native CLI resolved from PATH without a shell), AcpLaneClient sends the
  claude-code pin to the claude-code adapter only, and the enterprise host rides on the account as
  COPILOT_GH_HOST. Every launch line is one the spike observed, and each engine was spawned by the
  product's own ResolveLaunch → AcpEngineProcess.Start on this machine and answered initialize
  with protocolVersion 1 — copilot.exe 1.0.84-6, codex-acp 1.10.0, gemini-cli 0.58.0, grok 1.0.30.
  Not observed: session/prompt on any new engine, any sign-in, the host variable's effect at
  session/new. The hosts still construct the client without its row (seam request filed).
---

# Proof Pack — engines on the wire

- **Change:** branch `lane/agentplane-engines` (based on main `eb8cdee2`), four commits in the
  ruled order — `97353fa7` copilot · `17b4ba6e` codex · `fa4c89e7` gemini · `51bdce72` grok — plus
  this pack and the errata amendment.
- **Rulings:** 97 (ii)(iii) conditions 1 and 3; 105 (1) and (4) (`docs/notes/addendum-c-council-rulings.md`).
- **Spike (the observed facts every row cites):** `docs/spikes/engine-backends-2026-09-14.md`; the
  frame corpus under `spikes/engine-backends/<engine>/`.
- **Tier:** T1 · fan-out 0 · one lane (`agentplane-engines`), worktree `C:\Projects\ai-de-lane-agentplane-engines`.
- **Author / date:** the engines lane, 2026-09-14.

**Standard.** Every row below is **Verified** (a test ran it on this machine, or a file was read),
**Inferred** (a doc page or the spike's reading; cited), or **Not recorded** (says why). A green test
is evidence the test passed; the observation it recorded is copied here from the file it wrote
(`%TEMP%\aide-engine-spikes\aide-tests\<engine>-<id>\observed-initialize.json`), never from memory.

## 1. What landed — the surface list (E7), and what each surface reached

| Surface | Reached? | Where |
|---|---|---|
| catalog rows (the data) | yes | `src/AiDe.Core/AgentPlane/EngineCatalog.cs` — five rows: claude-code (unchanged), codex (`AdapterEntryModule = "dist/index.js"`), copilot / gemini / grok (`AcpMode.Native`, each with a `NativeCommand`) |
| launch resolution | yes | `EngineCatalog.ResolveLaunch` — two paths (adapter under `node`; native via `NativeCommandLocator`: PATH executable → npm shim's script → install root); refusals `UnknownEngine` · `LaunchPathNotImplemented` (Observed / Deferred / a native row without a command / a host on an engine with no host variable) · `AdapterEntryModuleNotRecorded` · **`EngineNotOnPath` (AP-0022, new)** |
| spawn | yes, by construction | `AcpEngineProcess.StartInfoFor` is launch-agnostic: no shell, `CreateNoWindow`, UTF-8 (no BOM) on all three redirected streams, `ArgumentList` — asserted for a native launch in `ANativeLaunchIsSpawnedWithoutAShellTests` |
| wire — `session/new` | yes | `AcpLaneClient(engine:, diagnostics:)` — `_meta.claudeCode.options` only when `EngineRow.ReadsClaudeCodeMeta`; otherwise the ACP-standard `{cwd, mcpServers: []}` and a **reported** omission |
| model — the account | yes | `ProviderAccount.Host` (additive, last positional); `ProviderRegistry` doc names the five providers |
| store — `providers.json` | yes | `ProviderConfiguration.ReadAccount` reads `host` (extends §14.2; blank refused); `engines.<id>` accepts the five ids because it checks the catalog |
| launch environment | yes (plane half) | `EngineCatalog.LaunchEnvironment(row, account)` → `COPILOT_GH_HOST`; refuses a host the engine cannot honour |
| **host call sites** | **no — seam** | `GovernedRunHost.cs:184` and `CompileCallHost.cs:258` construct `AcpLaneClient` without its row, and start the engine (`GovernedRunHost.cs:164`) before the binding is authorised (`:190`), so neither passes `LaunchEnvironment`. Owned by neither lane; **seam request `req-01M2GKC2RY667S51X22QVV7FGJ`** filed for the conductor. Until it lands, a host launching a native row sends the claude pin (harmlessly ignored, as far as the corpus shows — but unreported) and no `COPILOT_GH_HOST`. |
| UI (sheet) / composer | not this lane | `lane/sessions-accounts` (Ruling 105 (2)); the sheet already reads `EngineCatalog.Rows` and lists an engine only when its provider is configured (`TheNewSessionSheetTests.BackendsComeFromTheCatalogFilteredByTheRegistry`, still green) |
| errata note | yes | `docs/notes/conductor-spec-errata-providers-json.md` — accepted providers, `host` on the account; the adapter-root default and `engines.<id>.account`-as-fallback are **left to the accounts lane** (one definition each) |

## 2. Per engine

### copilot — commit `97353fa7`

| | |
|---|---|
| **Row** | `copilot` · provider `github` · `AcpMode.Native` · `NativeCommand("copilot", ["--acp"], install "winget install GitHub.Copilot …", NpmPackage null, HostVariable "COPILOT_GH_HOST")` |
| **Launch as spawned (Verified, this machine)** | `C:\Users\malla\AppData\Local\Microsoft\WinGet\Packages\GitHub.Copilot_Microsoft.Winget.Source_8wekyb3d8bbwe\copilot.exe --acp` — the locator's pass 1 (`where copilot`: the winget `.exe` first, an npm `copilot.cmd` 1.0.69 second; the `.exe` wins wherever each sits) |
| **Observed `initialize` (Verified)** | `protocolVersion 1` · `agentInfo {name "Copilot", title "Copilot", version "1.0.84-6"}` · `authMethods [copilot-login]` (with `_meta.terminal-auth` naming `copilot.exe login`) · `agentCapabilities {loadSession, mcpCapabilities{http,sse}, promptCapabilities{image,embeddedContext}, sessionCapabilities{close,list}}` · **1,012 ms** (1,227 ms first run) · `COPILOT_HOME` on scratch, which received `config.json` + `logs/` as the spike observed; `~/.copilot/config.json` mtime unchanged (10:47 local, before this lane) |
| **Red-first** | The new tests did not compile against the old catalog (`NativeCommandLocator`, `NativeCommand`, `EngineNotOnPath`, `ProviderAccount.Host`, `AcpLaneClient(engine:)` absent — observed as `CS0246` on first build); `TheNativeLaunchPathResolvesCopilotToTheExecutableOnPath` threw `LaunchPathNotImplemented` against the old catalog — the refusal the `simplify:` alarm guarded; that alarm, `ANonAdapterModeIsRefusedWithANamedReason`, is re-pointed at Observed / Deferred under its original name (the frozen F5 exit-evidence record cites it — §8); the frames test would have found `claudeCode` in the frame |
| **Tests** | `EngineCatalogTests`: `TheNativeLaunchPathResolvesCopilotToTheExecutableOnPath`, `ADirectExecutableWinsOverAnNpmShimEarlierOnPath`, `ANativeCommandThatIsNotOnPathIsRefusedWithItsInstallInstruction`, `AnNpmShimForACliWithNoObservedNpmLaunchIsRefusedRatherThanRunThroughAShell`, `ACopilotAccountWithAHostLaunchesWithCopilotGhHost`, `AHostOnAnEngineWithNoHostVariableIsRefusedRatherThanDropped` · `TheEnginesAnswerInitializeOnTheWireTests.CopilotAnswersInitializeWithProtocolVersionOne` · `AnAccountMayCarryAnEnterpriseHostTests` (4) · `TheSessionNewFrameIsEngineAppropriateTests` |
| **NOT observed** | `session/new` on this lane (the spike observed it on the stored login: `sessionId`, `modes`, 23 `models`); `session/prompt` (never sent, by anyone); any sign-in; whether the ACP server reads `COPILOT_GH_HOST` at session time (no tenant); the npm route's `node npm-loader.js --acp` (so an npm-only machine is **refused** by name, not launched on a guess); network activity during `initialize` |
| **Version drift (Flagged)** | The spike recorded 1.0.84-**5**; this lane observed 1.0.84-**6** the same day — the winget CLI moved under us. A native CLI has no pin the way an adapter does; see finding (engines a) |
| **Attended — the operator's one command** | `copilot login` (github.com / Enterprise Cloud incl. EMU), or `copilot login --host https://<tenant>.ghe.com` (data-residency tenants), `copilot login --device-code` headless. The product then launches `copilot --acp` — **never `--no-auto-login`** (measured to suppress the stored credential itself) |

### codex — commit `17b4ba6e`

| | |
|---|---|
| **Row** | `codex` · provider `openai` · `AcpMode.Adapter` · `@agentclientprotocol/codex-acp@1.10.0` · **`AdapterEntryModule = "dist/index.js"`** (was null: "never installed, never observed") |
| **Launch as spawned (Verified)** | `node C:\Users\malla\AppData\Local\Temp\aide-engine-spikes\codex\node_modules\@agentclientprotocol\codex-acp\dist\index.js` — the adapter path against the spike's scratch install (`--ignore-scripts`); one separator throughout since `NodeModule` splits the scoped package (the claude-code line changed spelling, not target) |
| **Observed `initialize` (Verified)** | `protocolVersion 1` · `agentInfo {name "@agentclientprotocol/codex-acp", title "Codex", version "1.10.0"}` · `authMethods [api-key, chat-gpt]` · capabilities incl. `auth`, `providers`, `sessionCapabilities` · **429 ms** (411 first run) · `CODEX_HOME` on scratch received `installation_id`, `skills/` and the sqlite state (`goals_1`, `logs_2`, `memories_1`, `queue_1`, `state_5`) exactly as the spike recorded; `~/.codex/auth.json` mtime 2026-09-06 (untouched) |
| **Red-first** | `TheAdapterLaunchPathResolvesForCodexWithTheObservedEntryModule` threw `AdapterEntryModuleNotRecorded` with the row change stashed (2 red / 23 green), then green; the refusal stays on a synthetic row (`AnAdapterWithNoObservedEntryModuleIsRefusedRatherThanGuessed`) |
| **NOT observed** | `session/new` on this lane (the spike observed `_auth/status_update {kind account, label "ChatGPT Pro"}` and a session on the stored login); `session/prompt`; the `authenticate {methodId: chat-gpt}` round-trip; network during `initialize` |
| **Attended** | Nothing to install beyond the adapter (Configure… runs the pinned `npm install`). Sign in **either** by ChatGPT (the ACP `authenticate` — not built here), **or** `codex login` once outside the product so `~/.codex/auth.json` exists, **or** `OPENAI_API_KEY` / `CODEX_API_KEY` in the launch environment |

### gemini — commit `fa4c89e7`

| | |
|---|---|
| **Row** | `gemini` · provider `google` · `AcpMode.Native` · `NativeCommand("gemini", ["--acp"], install "npm install -g @google/gemini-cli …", NpmPackage "@google/gemini-cli", NpmEntryModule "bundle/gemini.js")` — `--experimental-acp` is deprecated per `gemini --help` (`spikes/engine-backends/gemini/gemini-help.txt:28-29`) and never sent |
| **Launch as spawned (Verified)** | `node C:\Users\malla\AppData\Roaming\npm\node_modules\@google\gemini-cli\bundle\gemini.js --acp` — the locator's pass 2: `where gemini` finds **only** `%APPDATA%\npm\gemini.cmd` (no `gemini.exe` exists), and `CreateProcess` cannot start a `.cmd` by name; the shim is never run (DC-027: `cmd.exe` drops this machine's PATH), the script beside it is — the exact line the spike spawned |
| **Observed `initialize` (Verified)** | `protocolVersion 1` · `agentInfo {name "gemini-cli", title "Gemini CLI", version "0.58.0"}` · `authMethods [oauth-personal, gemini-api-key, vertex-ai, gateway]` · **1,204 ms** (1,180 first run) · `HOME`+`USERPROFILE` on scratch, which received `.gemini/` and an `AppData/`; `~/.gemini` untouched by this lane (its `oauth_creds.json` mtime 10:51 local is the spike's run) |
| **Red-first** | Three gemini `ResolveLaunch` tests threw `UnknownEngine` with the row stashed (6 red / 27 green), then green |
| **Tests** | `TheNativeLaunchPathResolvesGeminiThroughTheNpmShimToItsScript`, `AnNpmShimWithoutItsScriptIsRefusedRatherThanHandedToNode`, `AnNpmDeliveredCliAbsentFromPathResolvesFromTheInstallRootOrIsRefusedNamingIt`, `GeminiAnswersInitializeWithProtocolVersionOne`, the frames theory |
| **NOT observed** | `session/new` on this lane (the spike: refused without a key; opened only with `GEMINI_API_KEY` on a fresh home; **refused server-side** on the stored personal Google login — "migrate to Antigravity"); `session/prompt`; a Code Assist Standard/Enterprise OAuth; network during `initialize`. `GEMINI_API_KEY` was inherited from this machine's environment during the test and, per the corpus, is read at `session/new` only |
| **Attended** | `npm install -g @google/gemini-cli`; set **`GEMINI_API_KEY`** (from <https://aistudio.google.com/apikey>) in the environment the product launches with — the only path observed to open a session; "Sign in with Google" no longer does for individuals (since 2026-06-18, Inferred from the auth page the spike read) |

### grok — commit `51bdce72`

| | |
|---|---|
| **Row** | `grok` · provider `xai` · `AcpMode.Native` · `NativeCommand("grok", ["agent", "stdio"], install "irm https://x.ai/cli/install.ps1 \| iex … or npm install -g @xai-official/grok@1.0.30 …", NpmPackage "@xai-official/grok", NpmEntryModule "bin/grok")` |
| **The decision — Native, not Adapter (the architect's call the spike left open)** | (1) `AcpMode` states *how the engine speaks ACP*, and Grok Build speaks it itself: `@xai-official/grok` **is** the CLI and `grok agent stdio` is its own ACP mode (Zed's registry launches the same line, `https://zed.dev/acp/agent/grok-build`); an Adapter row would misstate the topology. (2) **One launch mechanism** for the three native CLIs — the command on PATH (xAI's installer puts a `grok.exe` there), npm's shim, or the product's install root — the same three passes as copilot and gemini, no fourth shape. (3) The npm package is recorded as the **install source** so Ruling 104's `npm install --prefix <root>` can deliver it — and that root-resolved line is exactly the launch the spike observed. Recorded in the row's comment. Cost: the first run's bootstrap writes ~150 MB (`grok.exe` decompressed from the platform package's `grok.exe.br`) into `$GROK_HOME/bin` (default `~/.grok/bin`) — the CLI's own home, which `grok login` writes to anyway; the product sets nothing |
| **Launch as spawned (Verified)** | `node C:\Users\malla\AppData\Local\Temp\aide-engine-spikes\grok\node_modules\@xai-official\grok\bin\grok agent stdio` — the locator's pass 3 (`where grok`: nothing on PATH) against the spike's scratch install |
| **Observed `initialize` (Verified)** | `protocolVersion 1` · **no `agentInfo`** · `_meta.agentVersion "1.0.30"` · `_meta.defaultAuthMethodId "xai.api_key"` · `authMethods [xai.api_key, grok.com]` (the first because this machine's `XAI_API_KEY` was inherited — the spike's `with-env-api-key` shape) · capabilities incl. `auth`, `sessionCapabilities{list,resume,close}`, `_meta.x.ai/*` · **3,015 ms first run including the bootstrap** (the stable scratch `GROK_HOME` received `bin/grok-1.0.30.exe` + `bin/grok.exe`, 150,027,264 bytes each), **458 ms** warm · `~/.grok` does not exist on this machine (before or after) |
| **Red-first** | `TheNativeLaunchPathResolvesGrokFromPathOrFromTheInstallRoot` threw `UnknownEngine` with the row stashed (4 red / 32 green), then green |
| **NOT observed** | `session/new` on this lane (the spike: `Authentication required` without a key; a session with `XAI_API_KEY`); `session/prompt`; `grok login`; which xAI plan the `grok.com` sign-in needs; network during `initialize` (the spike noted `_meta.hostname` in the result — the CLI reads the machine name; whether it sends it is not recorded) |
| **Attended** | `irm https://x.ai/cli/install.ps1 \| iex` (or `npm install -g @xai-official/grok@1.0.30`); then `grok login` once (browser), or **`XAI_API_KEY`** in the launch environment |

## 3. The claude-only `_meta` — proof

| Claim | Evidence (test) | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|
| A non-claude engine's `session/new` is byte-for-byte `{"cwd": …, "mcpServers": []}` — no `_meta` — even when the caller passes the governed pin | `TheSessionNewFrameIsEngineAppropriateTests.ANonClaudeEngineGetsTheAcpStandardFrameAndNoClaudeMeta` (theory: copilot, codex, gemini, grok) | exact-string equality on the wire line, and `SessionNewParameters` contains no `claudeCode` | the pin used to reach every peer (the client had no engine) | Verified | the frame's *acceptance* by each peer is the spike's observation (`frames.sent.jsonl` → `sessionId` or `-32000`), not this lane's |
| The omission is **reported**, never silent | same test — `Assert.Single(diagnostics)` names the engine and "not sent" | a diagnostics callback the host can route to the run's report | — | Verified | the hosts pass no `diagnostics` yet (seam) |
| The claude-code adapter still gets its pin | `TheClaudeCodeAdapterStillGetsItsPin`; the pre-existing `AcpLaneClientTests` (all green) | `_meta.claudeCode.options.disallowedTools == ["Bash"]` | n-a (regression guard) | Verified | — |
| A client told no engine is unchanged | `AClientToldNoEngineSendsTheOptionsAsGiven`; `TheCompileSessionIsPinnedTests`, `CompileModeGate` (green) | the frame still carries `claudeCode` | n-a | Verified | this is why the host seam matters: `null` is today's behaviour, not a fail-safe |
| **Governance on a non-claude engine is the permission policy alone** | — | — | — | **Flagged** | `session/prompt` was never sent to copilot, gemini or grok, so whether they raise `session/request_permission` before a write is **not recorded**; the governed lane's `disallowedTools: ["Bash"]` and the read-only lane's whole denied set have **no observed equivalent** there. See finding (engines b) |

## 4. Native spawn = adapter spawn

| Claim | Evidence | Oracle | Confidence |
|---|---|---|---|
| A native launch is spawned with no shell, no window, UTF-8 (no BOM) on all three streams, arguments as a list | `ANativeLaunchIsSpawnedWithoutAShellTests` (3) | `StartInfoFor` — the same factored seam DC-177's test used | Verified |
| `EngineLaunch` is exactly what is spawned, and the host's `engine: …` report line is the real file | the four wire tests spawn `ResolveLaunch`'s result unchanged; the recorded `launch` above is the `ProcessStartInfo`'s | the observation file is written from the launch the test started | Verified |
| A native CLI that is not on PATH is a named refusal carrying the install instruction, before any process | `ANativeCommandThatIsNotOnPathIsRefusedWithItsInstallInstruction`, `AnNpmShimForACliWithNoObservedNpmLaunchIsRefusedRatherThanRunThroughAShell`, `AnNpmShimWithoutItsScriptIsRefusedRatherThanHandedToNode` | `AP-0022` + the instruction text | Verified |
| The kill-on-close job reaps the tree (codex's adapter spawns `node codex.js app-server` → `codex.exe`) | every wire test asserts `engine.HasExited` after `Dispose`; `AcpEngineProcessTests` unchanged | `Process.GetProcessById` after disposal | Verified for the parent; the grandchild's exit is **Inferred** from the job (DC-123's mechanism, measured when it landed) |

## 5. Counts, gates, ledger

| Measure | Before (main `eb8cdee2`) | After (`51bdce72` + docs) |
|---|---|---|
| `AiDe.Core.Tests` executed | baseline minimum 2,650 (AgentPlane subset 217) | **2,683 / 0 failed / 1 skipped** (the pre-existing symbolic-link skip; +33 tests) — `verify-test-run.py` (no `--update`): "AiDe.Core.Tests 2683 ≥ 2650, Completed"; AgentPlane subset **250/0** |
| `AiDe.App.Tests` executed | baseline minimum 993 | **993 / 0** — "AiDe.App.Tests 993 ≥ 993, Completed" (4 m 09 s and 3 m 56 s on the two full runs) |
| Catalog rows / launch paths | 3 / 1 | 5 / 2 |
| Error codes | AP-0001…0021 | + AP-0022 `EngineNotOnPath` |

_The gate line and the terminal ledger are in §8 (an exit code is not a result — the state is read)._

## 6. What is NOT done (scope), and what is Flagged

- `session/prompt` on any new engine — **not observed** (spend; not asked). The Console / Conversation renderers were built on the claude-code corpus; the other peers' `tool_call` / permission / `usage_update` shapes are unknown. Cheapest probe: the spike's residual table (one `git status --short` prompt per engine).
- Sign-in — no gesture performed, no credential stored; the attended rows above are the operator's.
- The sheet, the accounts, first use — `lane/sessions-accounts`.
- `verify-test-run.py --update` — not run (the join's job).
- The host call sites — seam request `req-01M2GKC2RY667S51X22QVV7FGJ`.
- Higgsfield — not an engine (spike §5); nothing written.

## 7. Findings — placeholders for the register (classes, not instances; the register is not claimed by this lane)

| Placeholder | Class | Instance here | Control owed |
|---|---|---|---|
| **DC-nnn (engines a)** | A native CLI has no pin: the version the spike observed lives in prose, and the CLI moves under the catalog (winget/npm auto-update) with nothing to notice | copilot 1.0.84-5 at spike time, 1.0.84-6 hours later; the row's comment now cites a version that is already stale | the wire test records `agentInfo.version` / `_meta.agentVersion` on every run (done); a known-good version on the row and a *reported* (not refused) drift line at launch |
| **DC-nnn (engines b)** | A control that exists for one engine is silently absent for another of the same class: the governed lane's tools pin is an adapter extension, and a native peer that ignores `_meta` leaves the lane governed by the permission policy alone — which for that peer was never observed | copilot / gemini / grok get no pin; whether they ask permission before an edit is not recorded | the client reports the dropped pin (done); a host refusal (or an operator-visible warning) for a **write-shaped** run on an engine with no observed `session/request_permission` behaviour |
| **DC-nnn (engines c)** | A command that works in every shell does not exist to `CreateProcess`: a `.cmd`-only CLI (every npm global on Windows) fails by name with file-not-found, and the natural fix — a shell — is DC-027's own failure | `gemini` → only `gemini.cmd`; `ProcessStartInfo("gemini")` would have thrown | `NativeCommandLocator` (done): the shim is read for its script, never run; tests on a fake PATH |
| **DC-nnn (engines d)** | A real-peer test that skips where the peer is absent lowers the *executed* count and fires the silent-abort control (DC-012) on a machine that merely lacks the CLI — the honest skip and the count gate disagree | four `[NativeCliFact]`s execute here (all CLIs present) and will be baselined by the join's `--update`; a colleague's machine without gemini/grok would trip `verify-test-run.py` | the count gate distinguishes *skipped with a reason* from *not run* (owed); until then the skip reason names the install line so the operator can tell the two apart |
| (correction, process) | CT27 — "a multi-line program is a file, then a run — never a heredoc" | this lane's first multi-line edit was a Python heredoc; every later one was a scratch file + a run | none new; noted so the profiler's count is explained |

## 8. Run record

**`python tools/verify-test-run.py`** (no `--update` — the join re-baselines): "3676 tests executed
across 2 project(s), every project met its baseline" — `AiDe.App.Tests 993 / 993 Completed`,
`AiDe.Core.Tests 2683 / 2650 Completed`; the `.trx` files it wrote are what the gate line's
`--no-run` read.

**`python tools/run-verify-gates.py`, first run on the four engine commits: 3 of 38 FAILED**, and
each is read, not waved through:

| Gate | Finding | Cause | Resolution |
|---|---|---|---|
| `verify-front-door-exit-evidence.py --self-test` | clause 8: `refusalTest 'ANonAdapterModeIsRefusedWithANamedReason' is declared nowhere under tests/` | this lane had **renamed** the re-pointed alarm; the F5 exit-evidence record and its verifier — frozen at `1374401d` by clause 0 (Ruling 103) — cite the name | the name is restored and the body stays re-pointed (Observed / Deferred on a synthetic row); the test's remarks say why the name is kept |
| `verify-derived-views.py` | the API reference (`docs/api/AiDe.Core.AgentPlane.md`) and `docs/_meta.json` were behind the source | new public symbols (`NativeCommand`, `NativeCommandLocator`, `EngineRow.Native`, `ProviderAccount.Host`, `AP-0022`) | `python tools/regenerate-derived.py` — "every derived view is current and every gate is green" |
| `verify-site-figures.py` | 4 stale of 14: artifacts 473 → 474 (this pack), public symbols 3,444 → 3,454 | same regeneration | rewritten by `regenerate-derived.py` |

The other 35 gates were green on the first run, among them `verify-fixture-derivation` (no test
restates a product list), `verify-cited-controls` (302 claimed controls resolve — every test this
pack and the catalog's comments name exists), `verify-standins`, `verify-subprocess-utf8`,
`verify-no-new-console-launches` (no `CREATE_NEW_CONSOLE`; DC-170 holds), `verify-stranded-audit`
(52 worktrees, none stranded — **this tree is the one being worked in**), `verify-id-allocators`
(`AP 22` — the new code is allocated once).

**Second run, on the committed tree that carries this pack:** recorded in the commit message of
the docs commit (`docs(engines): …`) — the line is expected green; if it is not, that commit says so.

**Terminal ledger** (`%TEMP%\aide-tests\terminal-ledger-<pid>.log`, read at close): the two App
full runs on this tree read **10 `terminal.start` / 10 `terminal.stop`** (pids 3232 at 18:46Z and
30384 at 18:52Z) — balanced, which DC-220's known 10/9 is not; the mechanism DC-220 leaves open is
not identified here either, so this is one more reading, not a resolution. No engine spawned by
this lane's tests is a terminal (they are ACP children under a kill-on-close job) and none appears
in the ledger; every wire test asserted `HasExited` after disposal.

**Scratch left behind (under `%TEMP%\aide-engine-spikes\aide-tests\`):** one `observed-initialize.json`
plus a per-engine home/cwd per run, and the stable `grok-home` (300 MB: the bootstrapped
`grok.exe` twice) — the CLIs' own writes, kept for inspection; nothing under `~/.aide`,
`~/.copilot`, `~/.codex`, `~/.gemini` or `~/.grok` (the last does not exist on this machine).
