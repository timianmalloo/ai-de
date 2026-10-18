---
id: kb-workbench-loops-20261018
title: Desktop authoring, native subscription harnesses and coordination evidence
type: knowledge
status: review
owner: "@timianmalloo"
phase: proposal
tags: [research, editor, desktop, subscriptions, coordination]
links:
  - { to: kb-spec-first-software-factory, rel: refines }
  - { to: proposal-local-software-factory, rel: relates-to }
  - { to: architecture-agent-plane, rel: relates-to }
review-by: 2026-11-18
summary: Primary-source research checked on October 18, plus commit-pinned AI-Forward, AI-DE and headless benchmark evidence. Separates documented native routes from qualified runtime behavior.
---

# Evidence boundary

Checked **2026-10-18**. “Documented” means the official source was opened; it does not mean AI-DE executed that integration. “Verified source” means the named local code was inspected. “Proposed” and “Inferred” are design judgments. No provider login, paid inference, credential contents or account secrets were inspected in this research.

# Desktop experience: useful patterns and limits

| Product | Documented pattern | Inferred application to AI-DE |
|---|---|---|
| Claude Code desktop | Movable/resizable panes for conversation, diff, browser, terminal, files, plans, tasks and subagents; pop-out/redock; editor conflict warning; scoped side questions | Make the editor and execution coequal dockable content. Durable review questions need their own retained identity; a transient side chat is insufficient. [Desktop reference](https://code.claude.com/docs/en/desktop) |
| ChatGPT/Codex desktop | File source/rendered previews, selected-region annotations, preserved edits and last successful output; local project actions; configurable file/terminal placement | Selection-to-question, editor/output continuity and visibly stale last-successful results. This does not establish a general-purpose notebook kernel in Codex. [Artifacts](https://learn.chatgpt.com/docs/artifacts-viewer), [developer settings](https://learn.chatgpt.com/docs/developer-settings), [local environment](https://learn.chatgpt.com/docs/environments/local-environment) |
| Copilot / VS Code | Agents window distinguishes delegation/review from frequent editing and testing; notebook-specific context and outputs; produced artifacts distinct from consulted references | Addressable sections, output-aware questions, a clear authoring surface beside execution. File/chat checkpoint restore does not undo external effects. [Agents window](https://code.visualstudio.com/docs/agents/run/agents-window), [notebooks](https://code.visualstudio.com/docs/agents/guides/notebooks-with-ai), [artifacts](https://code.visualstudio.com/docs/agents/run/artifacts), [checkpoints](https://code.visualstudio.com/docs/agents/run/review-code-edits) |
| Grok Build | Prompt-history search, prompt/scrollback focus, queues, tasks, fullscreen inspection; plan review with line/range comments | Keep fast history and explicit queues; use contextual comments in the document. Do not copy terminal shortcuts blindly into an editor. Plan mode is not a complete write boundary. [Keyboard](https://docs.x.ai/build/keyboard-shortcuts), [plan mode](https://docs.x.ai/build/features/plan-mode) |
| Cursor | Queue and active steering differ; local checkpoints are separate from Git; persistent interactive canvases; selection context can include element identity and a screenshot | Distinct Queue/Steer actions; inspectable visual outputs; selection-scoped questions. A polished generated report is not evidence of correctness. [Agent](https://cursor.com/docs/agent/overview), [canvas](https://cursor.com/docs/agent/tools/canvas), [design mode](https://cursor.com/docs/agent/design-mode) |

The target is not a visual average of these products. AI-DE's differentiator is a continuous authoring
document, a durable run/evaluation record and a linked evidence graph across both **work-first** and
**system-first** entry paths. This is a proposed synthesis, not an observed industry consensus.

# Editor architecture evidence

Verified source: AI-DE already vendors **CodeMirror 6** and Lezer in
`src/AiDe.App/Web/vendor/codemirror-composer.bundle.mjs`; the adjacent `LICENSES-codemirror.txt`
identifies the dependency closure. `vendor-src/composer-bundle/package.json` includes Markdown support.
`ComposerSurface.cs` owns the WebView2 boundary, draft and Send/Prepare controls. The code viewer uses
read-only AvalonEdit; it is not a writable notebook.

CodeMirror exposes inline/block decorations and widgets, and immutable multi-range selections.
These are foundations for literate Markdown, completion, diagnostics and multiple selections; they
are not proof that AI-DE has implemented those features.
[Decoration source](https://github.com/codemirror/view/blob/main/src/decoration.ts),
[selection source](https://github.com/codemirror/state/blob/main/src/selection.ts).

ProseMirror offers a schema-governed document, transactions, selections/marks, and clipboard
parsing/serialization. Embedding code editors within that document is a real alternative. Its extra
schema, clipboard, IME and undo-boundary integration cost is **Inferred**, not a measured estimate.
[Schema](https://github.com/ProseMirror/prosemirror-model/blob/master/src/schema.ts),
[transactions](https://github.com/ProseMirror/prosemirror-state/blob/master/src/transaction.ts),
[clipboard](https://github.com/ProseMirror/prosemirror-view/blob/master/src/clipboard.ts).

# Subscription-first native harness routes

The user clarified that authentication must be explicit and harness-owned. The chosen approach is
to run the installed native harness with its existing authenticated profile. AI-DE does not become
an OAuth broker, collect API keys, or extract a native credential for an unrelated API client.

| Requested choice | Documented native route | Qualification boundary |
|---|---|---|
| Anthropic / Claude | Native Claude Code login, or operator-issued `setup-token`; native headless output | Native CLI execution is distinct from offering Claude login in an embedded SDK product. Distribution-specific policy remains a qualification question. [Authentication](https://code.claude.com/docs/en/authentication), [headless](https://code.claude.com/docs/en/headless), [SDK boundary](https://code.claude.com/docs/en/agent-sdk/quickstart) |
| OpenAI / Codex | Native `codex login`, then local app-server or bounded exec using native auth state | Account, model and headless route require qualification. A separately registered Sign in with ChatGPT application is not necessary for the selected native-harness approach. [Auth](https://learn.chatgpt.com/docs/auth), [app-server](https://learn.chatgpt.com/docs/app-server) |
| Microsoft / GitHub Copilot | Native Copilot CLI sign-in; programmatic CLI or first-party ACP preview | GitHub Copilot entitlement and organization policy apply; consumer Copilot is a different account/product. [CLI auth](https://docs.github.com/en/copilot/how-tos/copilot-cli/set-up-copilot-cli/authenticate-copilot-cli), [ACP](https://docs.github.com/en/copilot/reference/copilot-cli-reference/acp-server) |
| Meta / Muse | Native Muse Code onboarding and `muse exec --json`; MSP/serve is a separate option | Muse Code subscription credential remains product-scoped. Its serve/MSP billing route is separately unverified; no benchmark profile was found. [Subscriptions](https://dev.meta.ai/docs/muse-code/subscriptions), [auth](https://dev.meta.ai/docs/muse-code/auth), [headless](https://dev.meta.ai/docs/muse-code/extending) |
| Grok / Grok Build | Native `grok login`; headless or `grok agent stdio` ACP with cached native account | Qualify optional cancel/resume/permission semantics and actual plan/account. Do not assume all ACP implementations are identical. [Enterprise auth](https://docs.x.ai/build/enterprise), [headless/ACP](https://docs.x.ai/build/cli/headless-scripting) |
| Google / Antigravity | Native interactive `agy` sign-in; persistent headless stream-json using native keyring state | This is actual Antigravity CLI, not Gemini CLI or an API-key SDK. Headless approvals are policy-driven; denied tools can coexist with a successful process exit. [Install/auth](https://antigravity.google/docs/install/), [headless](https://antigravity.google/docs/cli/headless/) |

A successful login command or discovered executable is not proof that a chosen model can execute.
Likewise, ACP support does not establish every optional capability. Preserve the native protocol
and raw event; project only known semantics into the shared UI. Readable reasoning summaries are
optional provider output. Opaque reasoning must remain opaque, and absence says “No reasoning summary
supplied.” A completed response remains distinct from verified evidence or accepted work.

# Working benchmark precedent

Verified source: `C:/Projects/x-harness-x-model-bench` at
**`8f51460720f20ad90bb29b1fa2e3f9218ea615f2`**. The existing five profiles cover Claude, Codex,
Copilot, Grok and Antigravity. Muse is a new qualification task. Existing dirty audit files were not
changed. This research did not rerun those benchmarks.

| Mechanism | Source | What AI-DE should reuse |
|---|---|---|
| Operator-issued native Claude token; avoid blind refresh-token copying | `src/harness_bench/profiles.py:53` | Explicit bootstrap and native lifecycle; secrets never enter evidence records |
| Strip inherited API-key/home/provider variables, then apply the selected profile | `profiles.py:64`, `:306` | Prevent an inherited key silently changing a requested subscription route |
| Isolated native homes and installed-build checks | `profiles.py:253`, `:599`; `preflight.py:41` | Profile identity/version separate from auth and inference qualification |
| Grok credential-home pool with locked refresh write-back | `profiles.py:615`; `docs/runbooks/grok-credential-pool.md:22` | Concurrency follows native credential constraints, not arbitrary worker count |
| Copilot and Antigravity native Windows keyring paths | `bench/profiles/copilot.yaml:8`; `bench/profiles/agy.yaml:5` | Avoid inventing credential-file copies for keyring-owned accounts |
| Authentication blocking decisions versus model/permission errors | `engine.py:67`; `driver.py:383`; `telemetry/normalize.py:62` | Distinct recovery actions and no repeated failed background launch loop |
| Credential-copy cleanup and native-record retention | `profiles.py:629`; `engine.py:953` | Evidence survives; temporary credential copies do not become artifacts |

The benchmark's unattended permission/trust allowances are **not** appropriate product defaults.
For example, `bench/profiles/grok.yaml:17` explicitly selects always-approve. Reuse the authentication,
isolation and transport mechanisms while preserving the workbench's scoped human permission boundary.

AI-DE already models this separation: `ProviderRegistry.cs:24` says subscription credentials belong
to the engine; `ProviderRegistry.cs:181` routes NeedsLogin to native login and re-probe; `FirstUse.cs:6`
rejects embedded credential handling. The gap is the profile/bootstrap and qualification UX.
`ConfigureProviderDialog.cs:198` currently derives readiness from exit status; it needs stronger
profile/account/model evidence before claiming a qualified route.

# Current AI-Forward coordination

Fetched source: **`83a7834d5b01d8a156092977b55eb14e2970d8b2`**, revision **112**, bundle
`2026.10.17.4`. The local source checkout stayed at `2ead7fc6` (revision 110), including its existing
untracked file. AI-DE's installed manifest is revision 102. However, its installed runner, board,
decision helper and coordination doctrine are byte-identical to source 112. A numeric version gap
does not establish missing behavior. The C# integration gap is the load-bearing issue.

| Verified protocol fact | Source at `83a7834` | Proposed UX |
|---|---|---|
| Owner is a model seat; human alone supplies consent | `pack/knowledge/agent-coordination.md:10` | Human permission, Owner ruling and Coordinator dispatch have separate labels |
| Native fleet, cross-harness spawned workers and independent registered sessions | `agent-coordination.md:43` | One graph; relationship badges determine controls, not vendor logo |
| Leader is designated through a Git CAS ref with epoch fencing | `pack/scripts/coord-core.py:888` | Show designated leader, epoch, observed lease and Not checked; no auto-election |
| Five-part delegation contract and seams before fan-out | `agent-coordination.md:24` | Objective, artifacts, tools, bounds/budgets and termination visible beside the task |
| Compiled audit ID and qualified runner manifest drive dispatch | `pack/scripts/coord-runner.py:346`, `:394` | Concrete dispatch preview, not an invented `coord delegate` command |
| Requests follow sent → received → acked → resolved/expired; ACK pins a blob; rulings are separate decision artifacts | `coord-core.py:631`, `:695` | Stale acknowledgment and its required new artifact are explicit |
| Presence is not meaningful progress | `coord-core.py:3772` | Fresh heartbeat can still be stalled; observed deltas and outcome shown separately |
| Ready-for-review does not mean semantic acceptance | `coord-runner.py:1068` | Transport complete → evidence present → review ready → accepted → integrated |
| Recovery does not replay interrupted work; retries narrowly bounded | `coord-runner.py:871`, `:1036` | Inspect interrupted attempt; explicitly authorize a new attempt |
| Usage/cost absent by default | `coord-runner.py:334` | Not recorded rather than invented totals |

Verified limits: the source doctrine names `coord delegate`, but no implemented parser command was
found. The runner admits Claude/Codex/Copilot/Grok/agy; live attach is narrower. Worktree isolation is
not context isolation. Preserve AI-DE's locally enhanced `conductor-join.py` durable step recovery;
do not overwrite it during a future pack update.

In AI-DE, `RunEvent.cs:153` explicitly says ParentAgentId is unpopulated in Phase 1; `RunTriage.cs:55`
has stages without corresponding conductor producers. `Watcher/LivenessProjection.cs:27` derives
Alive/Stale/Ended from heartbeat age, which cannot stand in for the pack's progress model.
Reuse `RunEventRelay.cs:11` rather than adding a competing consumer to the single-reader stream.

# Platform and proof boundaries

The target remains Windows WPF, AvalonDock and hosted WebView2. `DockHost.cs:54` already composes
the native layout machinery and per-host persistence. Moving/hiding a pane must not recreate its
model or cancel its run. Keyboard and focus behavior follow
[Windows keyboard guidance](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-interactions)
and [focus navigation](https://learn.microsoft.com/en-us/windows/apps/develop/input/focus-navigation).
WebView2/native focus and airspace require actual runtime evidence:
[WebView2 API overview](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/overview-features-apis).
The HTML exploration proves only its own interactions. Native UIA, mixed DPI, IME, large documents,
credential handling and actual provider execution remain separate qualification work.
