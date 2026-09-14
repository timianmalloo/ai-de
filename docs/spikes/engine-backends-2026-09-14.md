---
id: spike-engine-backends-2026-09-14
title: "Spike — engine backends: copilot, codex, gemini, grok, Higgsfield (+ claude-code --ignore-scripts)"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [spike, engines, acp, copilot, codex, gemini, grok, higgsfield, ruling-97, ruling-104, ruling-105]
links:
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: note-conductor-spec-errata-providers-json, rel: relates-to }
  - { to: note-conductor-spec-errata-policy, rel: relates-to }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-14
review-suggested: []
summary: >-
  Five engine spikes run 2026-09-14 on this machine (Windows 11, node v24.18.0, npm 12.0.2) plus the
  claude-code --ignore-scripts observation. Four engines speak ACP over stdio and answered
  initialize with protocolVersion 1: copilot 1.0.84-5 (native, `copilot --acp`), codex-acp 1.10.0
  (adapter, entry dist/index.js, bundles codex 0.153.4), gemini-cli 0.58.0 (native, `--acp`;
  `--experimental-acp` is deprecated), and xAI Grok Build 1.0.30 (native, npm @xai-official/grok,
  `grok agent stdio`). Higgsfield is a REST API plus an MCP server, not an agent. Gemini's personal
  Google login is refused server-side since the Antigravity transition; only the API-key path opened
  a session. Every install survived --ignore-scripts; only grok declares a postinstall, and its
  bootstrap decompresses lazily on first run. Sign-in gestures were not performed.
---

# Spike — engine backends (2026-09-14)

**Question.** Rulings 97(iii), 104 and 105 ask, per engine, what was *observed*: is it installed,
does it speak ACP, what are the first frames of the handshake, what does sign-in look like, and
what must the operator do. Ruling 104 conditions 5 and 6 add: does the pinned claude-code adapter
still work under `npm install --ignore-scripts`, and what are the upstream install instructions for a
missing `node` and a missing `claude` — copied, not recalled.

**Standard.** Every row is labelled **Verified** (I ran it or read it in the installed source, and
the frames are committed), **Inferred** (a doc page said it; the URL is cited), or **Not recorded**
(I could not observe it, and the row says why). Where prose and the committed frames disagree, the
frames win.

**Not done, on purpose.** No sign-in gesture was performed, no credential was stored or asked for,
no `session/prompt` was sent (so nothing was spent), nothing was written under `~/.aide`, and every
install went to `%TEMP%\aide-engine-spikes\<engine>`. Where a `session/new` was observed on a login
this machine already held, the row says so.

## Method

One probe, `spikes/engine-backends/probe-handshake.js`, modelled on the PD-5 spike's
`probe-read.js`: spawn the engine, send `initialize` (`protocolVersion: 1`, `fs` + `terminal`
capabilities), then `session/new` (`cwd` = the scratch directory, no MCP servers), record every
received frame to `frames.jsonl` and every sent frame to `frames.sent.jsonl`, and stop. A
`session/request_permission` would have been answered with the reject option; none arrived.

"Fresh" runs isolate the engine from this machine's state by environment variable —
`COPILOT_HOME`, `CODEX_HOME`, `GROK_HOME`, and for gemini `USERPROFILE`+`HOME` — pointed at an empty
scratch directory, with the provider's API-key variable removed from the environment. This is the
shape a colleague's clean machine presents. "Existing" runs use whatever this machine already held.

Machine baseline (all Verified, 2026-09-14): Windows 11 Pro 10.0.26200; `node --version` →
`v24.18.0`; `npm --version` → `12.0.2`; `gh --version` → `2.96.0 (2026-07-02)`; `claude --version` →
`2.1.268 (Claude Code)`; `codex` and `grok` not on PATH.

Corpus: `spikes/engine-backends/<engine>/**` — `run.log` (the probe's timeline), `frames.jsonl`,
`frames.sent.jsonl`, `stderr.log`, `install.log` (commands and exit codes). The operator's e-mail
and this machine's hostname were replaced with `<redacted-email>` / `<redacted-hostname>` in the
committed files; nothing else was edited. No secret-shaped string is present (grepped).

## 1. copilot — GitHub Copilot CLI, native ACP

| Question | Observed | Not recorded |
|---|---|---|
| Installed? | **Verified.** `copilot --version` → `GitHub Copilot CLI 1.0.84-5.` (exit 0). Binary `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GitHub.Copilot_Microsoft.Winget.Source_8wekyb3d8bbwe\copilot.exe` (147,071,264 bytes; winget id `GitHub.Copilot`). | — |
| Upstream install instruction | **Inferred** (docs page, read 2026-09-14): "WinGet: `winget install GitHub.Copilot`" · "npm (all platforms): `npm install -g @github/copilot`". Prerequisites quoted: "An active GitHub Copilot subscription" · "(On Windows) PowerShell v6 or higher". Source: <https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli>. | — |
| ACP flag | **Verified** from `copilot --help` (`spikes/engine-backends/copilot/copilot-help.txt:184`): `--acp  Start as Agent Client Protocol server`. The docs also name `--stdio` and `--port`; `--help` at 1.0.84-5 lists neither, yet `--acp --stdio` was accepted (`copilot/fresh-home-stdio-flag/`). stdio is the default transport (observed: no transport flag, frames flowed). | `--port` (TCP) — not tried; not needed. |
| Handshake, fresh machine (`COPILOT_HOME` = empty scratch, `--acp --no-auto-login`) | **Verified** (`copilot/fresh-home/frames.jsonl`): `initialize` → `{"protocolVersion":1, "agentInfo":{"name":"Copilot","title":"Copilot","version":"1.0.84-5"}, "agentCapabilities":{"loadSession":true, "mcpCapabilities":{"http":true,"sse":true}, "promptCapabilities":{"image":true,"audio":false,"embeddedContext":true}, "sessionCapabilities":{"close":{},"list":{}}}, "authMethods":[{"id":"copilot-login","name":"Log in with Copilot CLI","description":"Run `copilot login` in the terminal","_meta":{"terminal-auth":{"command":"<copilot.exe path>","args":["login"],"label":"Copilot Login"}}}]}` (408 ms). `session/new` → error `{"code":-32000,"message":"Authentication required"}`. The fresh home received `config.json` (`firstLaunchAt`) and a `logs/` dir. | — |
| `--no-auto-login` semantics | **Verified**: with this machine's *existing* login (`~/.copilot/config.json` lists one `loggedInUsers` entry on `https://github.com`) and `--no-auto-login`, `session/new` was still refused `-32000 Authentication required` (`copilot/existing-login/`). Without the flag, `session/new` succeeded in 4.3 s (`copilot/existing-login-autologin/`). So the flag suppresses use of the stored credential, not only an interactive prompt. **The product must not pass `--no-auto-login`.** | Whether a token in `COPILOT_GITHUB_TOKEN`/`GH_TOKEN` is honoured under `--acp` — not tried (no token). |
| `session/new` shape (existing login, no prompt) | **Verified** (`copilot/existing-login-autologin/frames.jsonl`): keys `sessionId`, `modes`, `models`, `configOptions`. `modes.availableModes` ids are URIs `https://agentclientprotocol.com/protocol/session-modes#agent` / `#plan` / `#autopilot`; `models.currentModelId` = `claude-sonnet-5`; `availableModels` (23) include `auto`, `claude-sonnet-5`, `claude-opus-5`, `gpt-6-astra`, `gpt-5.5`, `gemini-3.8-flash`, `grok-4.5`, `grok-4.6`, each with `_meta.copilotUsage` (e.g. `"15x"`) and `copilotPriceCategory`. | `session/prompt` frames — not sent (spend; not asked). |
| Sign-in shape | **Verified** from `copilot login --help` (`copilot/copilot-login-help.txt`): "The default authentication mode on a local desktop is a web-based browser flow … Known remote or headless environments … default to the OAuth device code flow instead. Use --device-code or --web-flow to force a specific mode." Token stored "securely in the system credential store" else "a plain text config file under ~/.copilot/". Env tokens honoured: `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, `GITHUB_TOKEN` (precedence order). "Classic personal access tokens (ghp_) are not supported." The ACP `authMethods[0]._meta["terminal-auth"]` tells the client to run `copilot login` in a terminal and retry. | The `authenticate` ACP method was not sent (it would start the operator's sign-in). |
| Enterprise host | **Verified** from the CLI's own help: `copilot login --host <host>` — "GitHub host URL (default: https://github.com)" — "Use --host to authenticate with a GitHub Enterprise Cloud instance that uses data residency (e.g., https://example.ghe.com)". Env (`copilot help environment`, `copilot/copilot-help-environment.txt:49-51`): `GH_HOST` "GitHub hostname for authentication and API requests; defaults to "github.com". Set this to your GitHub Enterprise Cloud with data residency hostname (e.g., "mycompany.ghe.com")"; `COPILOT_GH_HOST` "used only by Copilot CLI … overriding GH_HOST when set. Useful when GH_HOST points to a GitHub Enterprise Server instance but Copilot CLI needs to authenticate against github.com or a GitHub Enterprise Cloud with data residency hostname instead." **Reading (Inferred):** a colleague whose enterprise lives on github.com (Enterprise Cloud, incl. EMU) runs plain `copilot login`; only GHE.com data-residency tenants need `--host`; GitHub Enterprise Server is not an auth target for this CLI. | A `--host` login was not performed (no such account). Whether the ACP server reads `GH_HOST`/`COPILOT_GH_HOST` at session time — not observable without a tenant. |
| Preview status | **Inferred** (docs): "ACP support in GitHub Copilot CLI is in public preview and subject to change." <https://docs.github.com/en/copilot/reference/copilot-cli-reference/acp-server>. Prerequisite quoted: "GitHub Copilot CLI, installed and either authenticated with GitHub or configured with a BYOK provider". | — |
| **What the operator / colleague must do (attended)** | Install: `winget install GitHub.Copilot`. Sign in: **`copilot login`** (github.com / Enterprise Cloud) or **`copilot login --host https://<tenant>.ghe.com`** (data-residency tenants); a headless box: `copilot login --device-code`. Then the product launches `copilot --acp` (without `--no-auto-login`). | — |

## 2. codex — OpenAI, adapter `@agentclientprotocol/codex-acp@1.10.0`

| Question | Observed | Not recorded |
|---|---|---|
| Install (`--ignore-scripts`) | **Verified** (`codex/install.log`): `npm install --prefix %TEMP%\aide-engine-spikes\codex --ignore-scripts --no-audit --no-fund @agentclientprotocol/codex-acp@1.10.0` → "added 20 packages in 4s", exit 0. | — |
| Entry module | **Verified** from the installed `package.json`: `"bin": {"codex-acp": "dist/index.js"}`, `"main": "dist/index.js"`, `"type": "module"`, `"files": ["dist/index.js", …]`. On disk: `node_modules/@agentclientprotocol/codex-acp/dist/index.js` (1,272,135 bytes). **Entry = `dist/index.js`.** | — |
| Separate `codex` CLI needed? | **Verified, no.** `dependencies` include `"@openai/codex": "^0.153.3"` → installed `@openai/codex@0.153.4` with the optional platform package `@openai/codex-win32-x64` carrying `vendor/x86_64-pc-windows-msvc/bin/codex.exe` (295,408,944 bytes). Source (`dist/index.js:35040`, `:22098-22105`): `codexPath = process.env["CODEX_PATH"]`; when unset, `startCodexConnection` resolves `@openai/codex/bin/codex.js` and spawns `node <that> app-server`. `codex` is not on this machine's PATH and the handshake still answered. | — |
| Lifecycle scripts | **Verified**: `@agentclientprotocol/codex-acp` declares only build/test scripts; `@openai/codex` declares none; so `--ignore-scripts` skips nothing here. | — |
| Handshake, fresh (`CODEX_HOME` = empty scratch) | **Verified** (`codex/frames.jsonl`): `initialize` → `{"protocolVersion":1, "agentInfo":{"name":"@agentclientprotocol/codex-acp","title":"Codex","version":"1.10.0"}, "agentCapabilities":{"auth":{"logout":{}}, "loadSession":true, "sessionCapabilities":{"resume":{},"list":{},"close":{},"delete":{},"fork":{},"additionalDirectories":{},"subagents":{}}, "mcpCapabilities":{"acp":false,"http":true,"sse":false}, …}, "authMethods":[{"id":"api-key","name":"API Key","_meta":{"api-key":{"provider":"openai"}}},{"id":"chat-gpt","name":"ChatGPT","description":"Use ChatGPT to authenticate"}], "_meta":{"steering":{"supported":true}, "goal":{…}, "jetbrains":{"air":{…}}}}` (834 ms). `session/new` → `{"code":-32000,"message":"Authentication required"}`. The fresh `CODEX_HOME` received `installation_id`, `skills/`, and sqlite state (`goals_1`, `logs_2`, `memories_1`, `queue_1`, `state_5`) — the engine writes state on startup. | — |
| Auth methods (source) | **Verified** (`dist/index.js:26437-26483`): `api-key` reads `CODEX_API_KEY` or `OPENAI_API_KEY`; `chat-gpt` is offered unless `NO_BROWSER` is set; `chat-gpt-device-code` is offered only when the client advertises URL elicitation; `gateway` only when `clientCapabilities.auth._meta.gateway === true`. `CODEX_HOME` is honoured (the fresh run ignored this machine's `~/.codex/auth.json`). | — |
| `session/new` on this machine's existing `~/.codex/auth.json` (no prompt) | **Verified** (`codex/existing-login/frames.jsonl`): `_auth/status_update` → `{"authStatus":{"kind":"account","label":"ChatGPT Pro","account":{"email":"<redacted-email>","plan":"pro"}}}`, then `session/new` result with `models.availableModels` = `gpt-6-astra[low|medium|high|xhigh|max]`, … (2.2 s first run, 0.4 s warm). | `session/prompt` — not sent. |
| **What the operator must do (attended)** | Nothing to install beyond the adapter (the product's Configure… runs the pinned `npm install`). Sign in **either** by ChatGPT — the ACP client sends `authenticate {methodId:"chat-gpt"}` (opens a browser), or outside the product install the CLI once and run **`codex login`** so `~/.codex/auth.json` exists — **or** set **`OPENAI_API_KEY`** (or `CODEX_API_KEY`) in the environment the product launches with. | The `authenticate` round-trip was not sent. |

## 3. gemini — Google Gemini CLI

| Question | Observed | Not recorded |
|---|---|---|
| Installed? | **Verified.** `gemini --version` → `0.58.0` (exit 0). Package `@google/gemini-cli@0.58.0`, `bin.gemini = bundle/gemini.js`, `engines.node >= 20`; shim `%APPDATA%\npm\gemini.cmd` runs `node …\bundle\gemini.js %*`. The probe spawned `node <bundle/gemini.js> --acp` directly (no `.cmd`, no shell). | — |
| Upstream install instruction | **Inferred** (README, read 2026-09-14): `npx @google/gemini-cli` · `npm install -g @google/gemini-cli` · `brew install gemini-cli`. <https://github.com/google-gemini/gemini-cli/blob/main/README.md>. | — |
| ACP flag | **Verified** from `gemini --help` (`gemini/gemini-help.txt:28-29`): `--acp  Starts the agent in ACP mode [boolean]` · `--experimental-acp  Starts the agent in ACP mode (deprecated, use --acp instead) [boolean]`. **The conductor's `--experimental-acp` is deprecated; use `--acp`.** | — |
| Handshake, fresh (`HOME`/`USERPROFILE` = empty scratch, `GEMINI_API_KEY`/`GOOGLE_API_KEY` unset) | **Verified** (`gemini/fresh-home/frames.jsonl`): `initialize` → `{"protocolVersion":1, "authMethods":[{"id":"oauth-personal","name":"Log in with Google"},{"id":"gemini-api-key","name":"Gemini API key","_meta":{"api-key":{"provider":"google"}}},{"id":"vertex-ai","name":"Vertex AI"},{"id":"gateway","name":"AI API Gateway",…}], "agentInfo":{"name":"gemini-cli","title":"Gemini CLI","version":"0.58.0"}, "agentCapabilities":{"loadSession":true,"promptCapabilities":{"image":true,"audio":true,"embeddedContext":true},"mcpCapabilities":{"http":true,"sse":true}}}` (1.1 s). `session/new` → `{"code":-32000,"message":"Gemini API key is missing or not configured."}`. | — |
| Personal Google login (this machine's `~/.gemini/oauth_creds.json`; `settings.json` `security.auth.selectedType = oauth-personal`) | **Verified, refused server-side** (`gemini/existing-oauth-no-key/`, and identically with `GEMINI_API_KEY` present, `gemini/existing-api-key/` — the stored selection wins over the env key): `session/new` → `{"code":-32000,"message":"This client is no longer supported for Gemini Code Assist for individuals. To continue using Gemini, please migrate to the Antigravity suite of products: https://antigravity.google"}`. | Whether a paid Code Assist Standard/Enterprise licence's OAuth still opens a session — no such account. |
| API-key path | **Verified** (`gemini/fresh-home-api-key/frames.jsonl`, fresh home + this machine's `GEMINI_API_KEY`): `session/new` → `{"sessionId":…, "modes":{"availableModes":[default "Prompts for approval", autoEdit, yolo, plan "Read-only mode"],"currentModeId":"default"}, "models":{"availableModels":[auto, gemini-3.1-pro-preview, gemini-3-flash-preview, gemini-2.5-pro, gemini-3.5-flash, gemini-3.1-flash-lite],"currentModelId":"auto"}}` plus a `session/update available_commands_update`. | `session/prompt` — not sent. |
| Upstream status | **Inferred** (read 2026-09-14). Authentication page: "Unpaid tier and Google One users: Gemini CLI was replaced by Antigravity CLI on June 18th, 2026." <https://geminicli.com/docs/get-started/authentication/>. Blog: "On June 18, 2026, Gemini CLI and Gemini Code Assist IDE extensions will stop serving requests for Google AI Pro and Ultra, as well as those using it free of charge using Gemini Code Assist for individuals."; for organisations with "a Gemini Code Assist Standard or Enterprise license … your access remains unchanged."; "Gemini CLI will remain accessible via paid Gemini and Gemini Enterprise Agent Platform API keys". <https://developers.googleblog.com/an-important-update-transitioning-gemini-cli-to-antigravity-cli>. Antigravity CLI (`agy`) has no official ACP mode: an upstream feature request is open (<https://github.com/google-antigravity/antigravity-cli/issues/31>, search hit, not opened) and third-party adapters exist — not spiked. | — |
| Sign-in shape | **Inferred** (auth page): API key — "Obtain your API key from Google AI Studio" · "Set the `GEMINI_API_KEY` environment variable to your key"; Google login — "Select **Sign in with Google**. Gemini CLI opens a sign in prompt using your web browser."; Vertex — `GOOGLE_API_KEY` + `GOOGLE_GENAI_USE_VERTEXAI=true`. | — |
| **What the operator must do (attended)** | Install: `npm install -g @google/gemini-cli`. Provide a key: set **`GEMINI_API_KEY`** (from <https://aistudio.google.com/apikey>) in the environment the product launches with — the only path observed to open a session on this CLI today. A personal "Sign in with Google" no longer opens a session; an enterprise Code Assist licence is untested. The product launches `node <gemini.js> --acp` (or `gemini --acp`). | — |

## 4. grok — xAI

| Question | Observed | Not recorded |
|---|---|---|
| Does xAI ship an agent CLI? | **Yes — Verified on the wire.** "Grok Build": npm `@xai-official/grok` (registry `npm view`, 2026-09-14: `version 1.0.30`, `dist-tags {alpha: 1.0.31, latest: 1.0.30}`, `bin {grok: bin/grok}`, `os [darwin, linux, win32]`, `engines node >= 20`, six per-platform optional packages). Native installers (docs, Inferred): "macOS/Linux: `curl -fsSL https://x.ai/cli/install.sh \| bash`" · "Windows PowerShell: `irm https://x.ai/cli/install.ps1 \| iex`" <https://docs.x.ai/build/overview>; repo <https://github.com/xai-org/grok-build> ("Windows builds are best-effort and not currently tested from this tree."; Apache-2.0). | — |
| Does it speak ACP? | **Verified.** Zed's registry entry launches `npx @xai-official/grok@1.0.30 agent stdio` (<https://zed.dev/acp/agent/grok-build>). Observed here: `node <scratch>/node_modules/@xai-official/grok/bin/grok agent stdio` with `GROK_HOME` = empty scratch, `XAI_API_KEY` unset (`grok/frames.jsonl`): `initialize` → `{"protocolVersion":1, "agentCapabilities":{"loadSession":true, "promptCapabilities":{"image":false,"audio":false,"embeddedContext":true}, "mcpCapabilities":{"http":true,"sse":true}, "sessionCapabilities":{"list":{},"resume":{},"close":{}}, "auth":{}, "_meta":{"x.ai/fs_notify":true,"x.ai/hooks":{…},…}}, "authMethods":[{"id":"grok.com","name":"Grok","description":"Sign in with Grok"}], "_meta":{"agentVersion":"1.0.30", "defaultAuthMethodId":null, "modelState":{"currentModelId":"grok-4.6",…}, "hostname":"<redacted-hostname>", …}}` (2.8 s incl. bootstrap); notification `_x.ai/mcp/servers_updated {"mcpServers":[]}`; `session/new` → `{"code":-32000,"message":"Authentication required","data":"no auth method id provided"}`. `grok.exe --version` → `grok 1.0.30 (04b7ffed98c6)`. | — |
| Install (`--ignore-scripts`) | **Verified** (`grok/install.log`): `npm install --prefix %TEMP%\aide-engine-spikes\grok --ignore-scripts … @xai-official/grok@1.0.30` → "added 3 packages in 1s", exit 0. **This package declares `postinstall: node bin/postinstall.js`** (skipped). The platform package ships `bin/grok.exe.br` (brotli). `bin/grok-bootstrap.js` (read) resolves `$GROK_HOME/bin/grok.exe`, else decompresses the `.br` into `$GROK_HOME/bin/grok-<version>.exe` + `grok.exe` on first run, else in place under `node_modules`. Observed: the first run wrote `grok-1.0.30.exe` and `grok.exe` (150,027,264 bytes each) under the scratch `GROK_HOME`; `~/.grok` was not created. **So `--ignore-scripts` works, at the cost of a ~3 s first-run bootstrap that writes into `$GROK_HOME`** (default `~/.grok/bin`). | — |
| API-key path | **Verified** (`grok/with-env-api-key/`, this machine's `XAI_API_KEY` inherited, fresh `GROK_HOME`): `initialize.authMethods` gains `{"id":"xai.api_key","name":"xai.api_key","description":"XAI_API_KEY or api_key/env_key in config.toml"}` and `_meta.defaultAuthMethodId = "xai.api_key"`; `session/new` → result with `models.currentModelId = "grok-4.20-0309-non-reasoning"`, `availableModels` incl. `grok-4.20-0309-reasoning`, `grok-4.20-multi-agent-0309`, `grok-4.3` (0.7 s). | `session/prompt` — not sent. |
| Sign-in shape | **Verified** from `grok --help` (`grok/grok-help.txt`): subcommand `login  Sign in to Grok`; flag `--oauth  Use OAuth when the welcome screen starts authentication`. Docs (Inferred): "On first launch, Grok opens a browser for authentication." and for headless use `export XAI_API_KEY="xai-..."` <https://docs.x.ai/build/overview>. | Which subscription the `grok.com` sign-in requires (third-party blogs say SuperGrok / X Premium+; not read on x.ai) — not recorded. |
| **What the operator must do (attended)** | Either set **`XAI_API_KEY`** in the launch environment, or install the CLI (`irm https://x.ai/cli/install.ps1 \| iex`) and run **`grok login`** once (browser). The product would launch `node <root>/node_modules/@xai-official/grok/bin/grok agent stdio` (or `$GROK_HOME/bin/grok.exe agent stdio` after bootstrap). | — |

Incidental: Copilot's own model list exposes `grok-4.5` / `grok-4.6` and `gemini-3.x-flash` — a
route to those *models* through a colleague's Copilot licence without an xAI or Google account.
Not an engine; noted for the sheet's copy.

## 5. Higgsfield (higgsfield.ai) — not a coding agent

| Question | Observed | Not recorded |
|---|---|---|
| Category | **API + MCP — Inferred** (two pages, read 2026-09-14). REST: "Generate images and videos through one authenticated, asynchronous API." — base URL example `https://api.higgsfield.ai/higgsfield-ai/soul/v2/standard`; auth header `Authorization: Key ${HF_API_KEY_ID}:${HF_API_KEY_SECRET}`; keys from <https://cloud.higgsfield.ai> (<https://docs.higgsfield.ai/>). MCP: "Add the Higgsfield MCP server to Claude, OpenClaw, Hermes Agent, NemoClaw, or any MCP-compatible client. 30+ models for image and video generation, no API key required." · "Add the Higgsfield MCP server URL in your agent's settings and authenticate through your Higgsfield account. No API keys to manage or configure." · "Your existing Higgsfield plan credits work seamlessly through any connected agent." (<https://higgsfield.ai/mcp>). | The MCP server **URL itself** (the page names it without printing it in the fetched text); SDK install commands; pricing. Nothing was called (no account). |
| Auth gesture | **Inferred**: MCP — sign in with a Higgsfield account (no key); REST — an API key id + secret pair created in Higgsfield Cloud. | — |
| ACP? | **Not an agent; no ACP surface found** on either page. Per Ruling 97's "not ruled" note, an MCP server is a per-session config item, not an engine — this is the "acp-mcp" case the conductor is to raise with the operator. Not written to the catalog. | — |
| **What the operator must do** | Decide whether Higgsfield is wanted as an **MCP server inside a session** (then: obtain the MCP URL from the Higgsfield account and sign in through it) — nothing for the engine catalog. | — |

## 6. claude-code — `--ignore-scripts` (Ruling 104 c5) and the two install instructions (c6)

| Question | Observed |
|---|---|
| `npm install --ignore-scripts` of `@agentclientprotocol/claude-agent-acp@0.75.1` | **Verified** (`claude-code/install.log`): into `%TEMP%\aide-engine-spikes\claude-code` → "added 105 packages in 6s", exit 0. `dist/index.js` exists (4,134 bytes); `package.json`: `bin.claude-agent-acp = dist/index.js`, `main = dist/lib.js`, deps `@agentclientprotocol/sdk 1.4.0`, `@anthropic-ai/claude-agent-sdk 0.3.257`. **No package in the installed tree declares `preinstall`/`install`/`postinstall`** (grep over every `package.json`), so the flag skips nothing and costs nothing. |
| Does `initialize` answer? | **Verified** (`claude-code/frames.jsonl`): `initialize` → `{"protocolVersion":1, "agentInfo":{"name":"@agentclientprotocol/claude-agent-acp","title":"Claude Agent","version":"0.75.1"}, "authMethods":[], …}` (204 ms). `_auth/status_update` → `{"authStatus":{"kind":"account","label":"Claude Max",…}}`; `session/new` → result with modes `default | acceptEdits | plan | auto | bypassPermissions` (1.2 s) — on this machine's existing `claude` login, no prompt sent. |
| Consequence for the slice | The product may pass `--ignore-scripts` for claude-code and codex today (nothing to skip). For grok it also works, but the first launch performs the bootstrap write. Lifecycle scripts stay the named residual supply-chain risk either way. |

**Missing `node` — install instruction.** The nodejs.org download page is rendered client-side
with platform detection; a plain fetch shows only the LTS version, "v24.21.0 LTS"
(<https://nodejs.org/en/download>, read 2026-09-14), and no Windows snippet — so the copy-able
Windows instruction is taken from the winget catalog on this machine (Verified, `winget show --id
OpenJS.NodeJS.LTS`): "Found Node.js (LTS) [OpenJS.NodeJS.LTS] · Version: 24.19.0 · Publisher: Node.js
Foundation · Homepage: https://nodejs.org/". The instruction to show: **`winget install
OpenJS.NodeJS.LTS`** (or the installer at <https://nodejs.org/en/download>); known-good version
observed here: **v24.18.0**. Note the three versions differ (24.18.0 on this machine, 24.19.0 in
winget, 24.21.0 on the site) — the row must show the observed known-good, never the newest.

**Missing `claude` — install instruction** (Verified copy from <https://code.claude.com/docs/en/setup>,
read 2026-09-14). "To install Claude Code, use one of the following methods:" — Native Install
(Recommended): "**Windows PowerShell:** `irm https://claude.ai/install.ps1 | iex`" · "**Windows
CMD:** `curl -fsSL https://claude.ai/install.cmd -o install.cmd && install.cmd && del install.cmd`"
· "**macOS, Linux, WSL:** `curl -fsSL https://claude.ai/install.sh | bash`"; WinGet: "`winget
install Anthropic.ClaudeCode`"; npm: "`npm install -g @anthropic-ai/claude-code`" with "As of
v2.1.198, the npm package requires Node.js 22 or later". Verify: "`claude --version` — A working
installation prints a version number such as `2.1.211 (Claude Code)`." Authenticate: "Claude Code
requires a Pro, Max, Team, Enterprise, or Console account. The free Claude.ai plan does not include
Claude Code access." · "After installing, log in by running `claude` and following the browser
prompts. If the `ANTHROPIC_API_KEY` environment variable is set, Claude Code prompts you once to
approve the key instead of opening a browser." Known-good observed here: **2.1.268 (Claude Code)**.

## Catalog consequences

| Engine | `AcpMode` proposed | Package / version / entry — observed? | Launch line observed | Sign-in gesture (attended) | Refusal text if unobserved |
|---|---|---|---|---|---|
| claude-code | `Adapter` (unchanged) | `@agentclientprotocol/claude-agent-acp@0.75.1` · `dist/index.js` · **observed again under `--ignore-scripts`** | `node <root>/node_modules/@agentclientprotocol/claude-agent-acp/dist/index.js` | `claude` (browser) | — |
| codex | `Adapter` (unchanged) | `@agentclientprotocol/codex-acp@1.10.0` · **`dist/index.js` observed** · bundles `@openai/codex@0.153.4` (no separate CLI) | `node <root>/node_modules/@agentclientprotocol/codex-acp/dist/index.js` | ACP `authenticate {methodId:"chat-gpt"}` or `OPENAI_API_KEY` | — |
| copilot | `Native` (the `simplify:` trigger has fired) | no package; CLI `GitHub Copilot CLI 1.0.84-5` (winget `GitHub.Copilot`) · **observed** | `copilot --acp` (stdio default; **never** `--no-auto-login`) | `copilot login` / `copilot login --host https://<tenant>.ghe.com` | — |
| gemini | `Native` | no package; CLI `@google/gemini-cli@0.58.0` · `bundle/gemini.js` · **observed** | `node <gemini-cli>/bundle/gemini.js --acp` (or `gemini --acp`; `--experimental-acp` deprecated) | `GEMINI_API_KEY` (the only path that opened a session); "Sign in with Google" refused for individuals since 2026-06-18 | — |
| grok | `Native` (npm-delivered native binary) **or** `Adapter` with the pin `@xai-official/grok@1.0.30` · entry `bin/grok` — a judgement for the architect; both launch lines observed | `@xai-official/grok@1.0.30` · `bin/grok` (→ `$GROK_HOME/bin/grok.exe` after bootstrap) · **observed** | `node <root>/node_modules/@xai-official/grok/bin/grok agent stdio` | `grok login` (browser) or `XAI_API_KEY` | — |
| Higgsfield | none — not an engine | REST API + MCP server (docs) | — | Higgsfield account (MCP) / API key pair (REST) | "not an ACP engine; an MCP server is per-session config (Ruling 97, not ruled)" |

Every one of the five ACP peers echoed `protocolVersion: 1`, so `AcpLaneClient.InitializeAsync`'s
version check passes as written. All five run without a shell: copilot is a native `.exe`; the
other four are `node <file>`.

## Residual unknowns (Flagged) and the cheapest next probe

| Flagged | Cheapest probe |
|---|---|
| `session/prompt` frame shapes for copilot, codex, gemini, grok (`tool_call`, permission requests, `usage_update`) — the Console/Conversation renderers were built on the claude-code corpus. | One read-only prompt per engine (`git status --short`) with the same probe and `--prompt`; costs one request each on the operator's accounts. |
| Copilot ACP under an enterprise (`--host` / `GH_HOST`) login. | A colleague on the tenant runs `copilot login --host …` then `node probe-handshake.js --cmd copilot --arg --acp` and sends back `frames.jsonl`. |
| Gemini with a Code Assist Standard/Enterprise licence's OAuth. | Same probe on a licensed account; or accept the API-key path as the product's only gemini path. |
| Which xAI plan the `grok.com` sign-in requires. | Read <https://x.ai/build> pricing, or one attended `grok login`. |
| Whether copilot's ACP server honours `COPILOT_GITHUB_TOKEN` (headless CI shape). | Run the fresh-home probe with a fine-grained PAT in `COPILOT_GITHUB_TOKEN`. |
| Higgsfield MCP server URL. | Sign in to a Higgsfield account and read the MCP settings page. |

## Sources read (all 2026-09-14)

- <https://docs.github.com/en/copilot/reference/copilot-cli-reference/acp-server>
- <https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli>
- <https://github.com/google-gemini/gemini-cli/blob/main/README.md>
- <https://geminicli.com/docs/get-started/authentication/>
- <https://developers.googleblog.com/an-important-update-transitioning-gemini-cli-to-antigravity-cli>
- <https://docs.x.ai/build/overview> · <https://github.com/xai-org/grok-build> · <https://zed.dev/acp/agent/grok-build>
- <https://higgsfield.ai/mcp> · <https://docs.higgsfield.ai/>
- <https://code.claude.com/docs/en/setup> · <https://nodejs.org/en/download>
- npm registry: `@agentclientprotocol/codex-acp@1.10.0`, `@agentclientprotocol/claude-agent-acp@0.75.1`, `@xai-official/grok@1.0.30`
- Local: `copilot --help`, `copilot login --help`, `copilot help environment`, `gemini --help`, `grok --help`, the installed `dist/index.js` of codex-acp, `bin/grok-bootstrap.js`, `bin/postinstall.js`.
