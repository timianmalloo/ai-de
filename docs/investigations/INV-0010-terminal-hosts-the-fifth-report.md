---
id: inv-0010-terminal-hosts-the-fifth-report
title: "Terminal hosts are still not cleaned up — the fifth report: the population counted and attributed beyond ancestry, the exit paths measured, and the one product mechanism the four fixes never touched"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c"
tags: [terminal, conpty, conhost, straggler, census, reap-stragglers, job-object, observability, terminal-stop, dc-131, dc-123, dc-117, dc-156, dc-155, windows-terminal, copilot, mcp]
links:
  - { to: defect-classes, rel: relates-to }
  - { to: adr-0005-terminal-runtime-boundary, rel: depends-on }
  - { to: adr-0006-terminal-delivery-semantics, rel: depends-on }
  - { to: inv-0001-agent-terminal-environment, rel: relates-to }
  - { to: inv-0002-terminal-rebuild-kills-sessions, rel: relates-to }
  - { to: inv-0008-contrast-floor-passes-while-the-shell-fails, rel: relates-to }
  - { to: kb-agentic-session-observability, rel: relates-to }
review-by: ""
summary: >-
  The fifth report of "terminal hosts are not cleaned up". The population was counted first, twice
  (06:40Z, 13:39Z), and every host attributed beyond ancestry: 0 product ConPTY hosts alive at either
  census; 223 of 271 host-like processes are 111 `node.exe higgsfield-mcp/src/server.js` servers and
  their console hosts under Windows Terminal's own agent (`wta.exe` → `copilot.exe --acp --stdio`),
  accumulating at ~5/hour since Windows Terminal was restarted yesterday 17:02Z — the same foreign
  pool the fourth census attributed and left; 15 are Claude Code's own Monitor loops
  (`until false; do sleep 30; done`) from yesterday evening; 25 `unknown` are Windows Terminal's own
  tabs, Ollama's launcher, the compiler server's console and those loops. The four exit/containment
  paths were measured, not reasoned: App window close, owner exit without dispose, owner killed, and
  tab-close dispose all leave 0 hosts. One in-life path is red: a session whose child exits keeps its
  `conhost.exe --headless` alive for the App's lifetime (`WatchForExitAsync` completes the session
  and closes nothing) — one client-less host per ended pane, invisible to a census that labels
  everything under a live App `ours-live`. Two instrumentation gaps pinned red: no `terminal.stop`
  activity or log line exists, and the census cannot name a dead-parent host from our runtime.
  Red tests and self-test rows committed; no fix made. CORRECTED the same day (slice 0): the
  223 are ours by cause and foreign only by parent — a ConPTY shell of ours inheriting
  WT_SESSION from the Windows Terminal tab the harness runs in makes Windows Terminal's agent
  host attach an agent session (its MCP servers) to it; measured 4/4 → 0/4 with WT_* stripped.
---

# INV-0010 — Terminal hosts are still not cleaned up: the fifth report

- **Status:** population counted and attributed · exit paths measured · one product mechanism verified red · **no fix made** — stops for the Owner
- **Severity / tier:** T1 — five reports on one symptom; the product's own share was unmeasurable from the product
- **Reported by / date:** the operator, 2026-09-12 (fifth report); dispatched by the conductor (session `conductor-addendum-c`) to session `stragglers-5`
- **Evidence:** `docs/investigations/census-2026-09-12-0640Z.txt` (the conductor's 06:40Z census); two raw `Win32_Process` snapshots 13:39:16Z and 13:50:57Z (scratchpad, summarised in §Census); `%LOCALAPPDATA%\AiDe\logs\workbench-2026091{1,2}.log`; the test outputs quoted verbatim in §Exit paths
- **Related:** DC-131 (the census rule — recurrence 4 recorded here), DC-123 (containment one ring out), DC-117 (console-less tool host), DC-014, `tools/reap-stragglers.py`, `tools/verify-test-run.py:192`, INV-0001, INV-0002, ADR-0005

## Symptom

*"I am still seeing terminal hosts that are not being cleaned up."* — the fifth time. The previous
four fixed mechanisms (a test launcher's handles, the ACP engine tree, MSBuild node reuse, the Roslyn
compiler server) and the fourth added the census with an attribution column (DC-131's control). The
operator sees the same screen.

**The population was counted before any mechanism was named** (DC-131). This report's order is
deliberate: census → what the operator is looking at → the controls that did not hold → the class.

## Reproduction

The symptom is a population, so "reproduce" means "count it and attribute every member". Two
censuses, seven hours apart, with the reaper in report mode (nothing ended, nothing killed at any
point in this investigation) and a raw snapshot (`Get-CimInstance Win32_Process`: pid, parent pid,
creation time, command line, session) so that every `unknown` row could be attributed by hand.

| | 06:40Z (conductor) | 13:39Z (this session) |
|---|---|---|
| host-like processes | 271 | 275 |
| `ours-live` / `ours-detached` / `ours-straggler` | 0 / 0 / 0 | 8 / 1 / 0 (the operator's App was running; its WebView2 hosts and my two shells) |
| `build-server` | 1 | 1 |
| `foreign` | 240 (222 under `copilot.exe`, 6 `SearchHost`, 6 `M365Copilot`, +4) | 240 (224 under `copilot.exe`, 6, 6, +4) |
| `unknown` | 30 (13 bash, 8 conhost, 4 OpenConsole, 3 powershell, 1 cmd, 1 WindowsTerminal) | 25 (10, 7, 4, 2, 1, 1) |
| **product ConPTY hosts (`conhost.exe --headless`, any parent, alive or dead)** | *not distinguished by the tool* | **0** |

## Correction — 2026-09-12, slice 0: the 223 are ours by cause, foreign only by parent

**What this report got wrong.** The 223 (`node higgsfield-mcp` + console hosts under
`wta.exe → copilot.exe --acp --stdio`) were attributed to Windows Terminal's agent by *parent*
and closed as *"not AiDe's"*, with the action pointed at the operator's global MCP config. The
parent was right. The **cause** is ours: this harness runs inside a Windows Terminal tab, so
every process it spawns — sub-agents, `dotnet test`, the helper, the App's ConPTY shells — inherits
`WT_SESSION`, `WT_PROFILE_ID` (and `WT_COM_CLSID`), and nothing in `src/AiDe.Core/Terminal`, the
App's terminal surface or the test helper stripped them. Windows Terminal's agent host (started
with Windows Terminal, not by the operator, who had not run Copilot or used higgsfield in weeks)
treats each ConPTY shell that carries `WT_SESSION` as one of its own tabs and attaches an agent
session to it, which loads the global MCP config and spawns one `node higgsfield-mcp` + one
console host per shell, kept for Windows Terminal's lifetime. The conductor's correlation
(`terminal.start` per local hour vs node births: 12:00 315/19, 15:00 279/20, 17:00 398/30,
06:00 121/26, 07:00 343/46) put the births on our test runs; the operator confirmed the reading.

**Measured (this session, this machine, read-only except for its own spawns).** Four
product-shaped ConPTY sessions (the helper's `exit-undisposed`: PowerShell + integration, 6 s):
with `WT_*` inherited, **4 `node higgsfield-mcp` births** in 40 s (alive 166 → 170), one per
session; the same four with `WT_*` removed, **0**. Four plain hidden `powershell.exe` with
`WT_*` inherited and no console of their own: 0 — the trigger is a ConPTY session carrying
`WT_SESSION`, not the variable alone. After the fix, four helpers that themselves still inherit
`WT_*` but whose ConPTY child is scrubbed: **0** (was 4) — the child is the trigger.

**Slice 0 (landed first, `fix/terminal-hosts-5`).** `ConPtyInterop.BuildEnvironmentBlock` strips
every `WT_*` variable from the block of every ConPTY child and builds the block whenever the
parent carries one, even with nothing else to add; the rest of the user's environment passes
through unchanged (INV-0001). `TerminalHostLauncher` hands the helper the same block. Red first:
`EnvironmentBlockTests.AParentInsideWindowsTerminal_HandsItsChildNoWT_Variable_AndEverythingElse`
(the block builder) and `TerminalChildEnvironmentTests` (the helper's `env-scrub` mode: a real
pseudo console runs `cmd.exe /c set`; three `WT_` lines arrived on the un-fixed runtime, none
after). The census's action line now names the cause and the mechanism, and reports the birth
correlation with our own `terminal.start` lines (DC-155's corrected control). The pool that
exists — 371 host-like processes under `wta.exe` at 14:58Z, 185 servers — was born before the
fix and does not shrink on its own: restart Windows Terminal, then re-count; **a birth after the
fix is a spawn path that still inherits `WT_*`**.

**What stands from the original census:** 0 product ConPTY hosts alive at either census; the
held-host mechanism (path 5); the two instrumentation gaps; the harness loops. What changes:
the class of the largest population — *foreign by parent, ours by cause* — and the phase-5 action,
which is no longer the operator's MCP config but a Windows Terminal restart after the fix.

## The census, attributed beyond ancestry

Every row was attributed by **creation time + command line + parent pid at creation + the ConPTY
signature**, not by the reaper's label. The signature: `CreatePseudoConsole` starts
`\??\C:\Windows\system32\conhost.exe --headless --width W --height H --signal 0x… --server 0x…`; a
console attached to an ordinary console process is `conhost.exe 0x4`. Windows Terminal ships its
own `OpenConsole.exe --headless …`, so the flag alone is never the key.

### 13:39Z, every host-like process, by owner

| class | count | what it is | the rule that attributed it |
|---|---|---|---|
| **foreign — Windows Terminal's agent host** | **223** | `copilot.exe` pid 27168 (`"copilot.exe" --acp --stdio`, created 17:02:02Z 2026-09-11, parent `wta.exe` 14152 whose argv is `--master \\.\pipe\wta-master-… --agent "copilot --acp --stdio"`) → **111 × `node.exe C:\Users\malla\AppData\Roaming\npm\node_modules\higgsfield-mcp\src\server.js`** → 111 × `conhost.exe 0x4`; plus 1 node + 1 conhost under the operator's interactive `copilot.exe` 40104 (13:36:57Z) | ancestor *name* `wta.exe`/`copilot.exe` (the reaper's 5b rule); creation-time chain validates (WT 17:02:01.0 < wta 17:02:01 < copilot 17:02:02); no AiDe process existed until 22:32Z; `EngineCatalogTests` asserts the product refuses to launch `copilot`. The MCP server comes from the operator's **global** `~/.copilot/mcp-config.json` (`higgsfield` → `node …\higgsfield-mcp\src\server.js`), not from this repository |
| foreign — other | 4 | `SearchHost`, `M365Copilot`, `PresentMonService`, `ArmouryCrate` console hosts; Ollama (`cmd.exe /C set PATH=…Ollama… & "ollama app.exe"` + its `conhost 0x4`, 17:00:55Z) | ancestor name (reaper) / command line (Ollama — the reaper files it `unknown` because `ollama` is matched on *names* and the wrapper is `cmd.exe`) |
| **operator's terminal (Windows Terminal itself)** | 7 | `WindowsTerminal.exe` 12596 (17:02:01Z), `OpenConsole.exe --headless` × 4 (two tabs 17:02:01Z, two 13:36:31Z), `powershell.exe` × 2 (the tab shells 8328 and 41812) | parent `WindowsTerminal.exe`; **the reaper files these `unknown`** because `FOREIGN_ROOTS` matches `IntelligentTerminal` against process *names* and the name is `WindowsTerminal.exe` (the package path carries the token) |
| **harness — Claude Code's own tool shells** | 15 + 1 | 5 × (`Git\bin\bash.exe` → `usr\bin\bash.exe` → `conhost.exe 0x4`) under `claude.exe` 17664 (the conductor's session, started 17:02:16Z), created 20:57:56, 21:00:19, 21:23:39, 21:26:09, 21:26:38Z; each runs **`eval 'until false; do sleep 30; done' < /dev/null`** — a `Monitor` loop with no termination variant; plus this session's transient `pwsh.exe` | parent `claude.exe`; the command line is Claude Code's shell-snapshot wrapper; **not the product's** |
| build-server | 2 | `VBCSCompiler.exe` 10872 (orphaned) + its `conhost 0x4` 36548 (13:23:16Z — a lane's build) | reaper rule; the conhost is filed `unknown` because a conhost's chain has no worktree path |
| ours-live | 8 | the operator's App (`AiDe.App.exe` 32532, Release `8d54aadc`, launched from Explorer 13:39:05Z on `C:\Projects\TheTerrace`): 6 × `msedgewebview2.exe`; my `pwsh`/`powershell` | worktree path in the chain's argv |
| ours-detached | 1 | the App's daemon's `conhost 0x4` (daemon 40628, 13:39:15Z) | `AiDe.Daemon.exe` in the chain |
| **ours-orphaned (a product ConPTY host whose owner is dead)** | **0** | — | `conhost.exe --headless` with a dead parent: **none on the machine** at 06:40Z (8 unknown conhosts, all `0x4`), 13:39Z, 13:50Z, or in nine samples 14:03–14:04Z |

**Total 275 = 223 + 4 + 7 + 16 + 2 + 8 + 1 + 0 + 14** (the remainder: `node.exe`/`dotnet.exe` rows
the reaper counts as host-like under the same foreign roots).

### The 25 `unknown`, one by one

| pid | name | parent (state) | created | attribution |
|---|---|---|---|---|
| 3528 / 11384 | cmd.exe / conhost.exe | 24584 (dead) / 3528 | 09-11 17:00:55 | Ollama's launcher — foreign |
| 12596 | WindowsTerminal.exe | explorer 11588 | 09-11 17:02:01 | the operator's terminal |
| 14212, 24360, 6772, 39436 | OpenConsole.exe `--headless` | 12596 | 17:02:01 ×2, 09-12 13:36:31 ×2 | Windows Terminal's four tabs |
| 8328, 41812 | powershell.exe | 12596 | 17:02:01, 13:36:31 | the tab shells (8328 hosts `claude.exe`) |
| 12648→17760→2684, 5972→28704→15088, 19660→7752→22516, 9988→20844→25308, 5196→6332→28076 | bash → bash → conhost | claude.exe 17664 | 20:57:56 … 21:26:38 | Claude Code `Monitor` loops — harness |
| 36548 | conhost.exe `0x4` | VBCSCompiler 10872 | 09-12 13:23:16 | the compiler server's console — build-server |

**Zero of the twenty-five are the product's.** The same decomposition holds for the 30 at 06:40Z
(three more harness shell triples since ended).

### What changed between the fourth census and this one

The fourth census (2026-09-11, DC-131 recurrence 2/3) found **256** `node.exe` MCP servers under
`copilot.exe` under `wta.exe`. Windows Terminal was restarted at **17:02:01Z** that day (every WT
process, `wta.exe` and `copilot.exe` 27168 carry that creation time) — the pool went to zero — and
by 13:36Z today it was back to **111**, in bursts (6 in the 17h hour, 19 at 19h, 10 at 20h, 20 at
22h, 30 at 00h, 5 at 01h, 21 at 13h: it grows when the Copilot agent is used). **The population
the operator reports is the same foreign population, regrown.** It was attributed correctly once,
reported as "foreign — reported only, never removed", and nothing was done about it — so the
operator saw it again.

## What the operator is looking at — a question, and what the evidence says

**To the operator:** which view, and which names? *Task Manager → Processes* groups console hosts
under the app that owns them; *Details* lists `conhost.exe` flat. Are you seeing **"Console Window
Host" × ~110 and "Node.js: Server-side JavaScript" × ~110 under Windows Terminal** (or as a flat
list), or `conhost.exe` rows under **AI-DE**, or `OpenConsole.exe`, or `powershell.exe`? Roughly
how many, and does the number fall when you close Windows Terminal? Did it reset when you
restarted Windows Terminal yesterday around 17:00Z?

**Proceeding on the evidence:** the only population on this machine that matches "terminal hosts
not being cleaned up" in both size and growth is the **111 `conhost.exe` + 111 `node.exe` under
Windows Terminal's Copilot agent**, which regrows at ~5/hour and reached 256 before the last
restart. The product's own contribution at every census today was **zero**. If the operator is
looking at AI-DE's process group while the App has ended-but-open terminal panes, they would see one
`conhost.exe` per pane, live or ended (§Exit paths, path 5) — a handful, and gone when the App
closes.

## Timeline (events and causal factors)

| when (UTC) | event | source |
|---|---|---|
| 09-11 17:00:55 | Ollama launcher (`cmd.exe` wrapper) starts | snapshot |
| 09-11 17:02:01–02 | Windows Terminal restarted; `wta.exe` → `copilot.exe --acp --stdio` 27168; two tabs | snapshot creation times |
| 09-11 17:02:16 | `claude.exe` 17664 (the conductor's Claude Code) starts in tab shell 8328 | snapshot |
| 09-11 17:02 → 09-12 13:36 | 111 `node higgsfield-mcp` servers accumulate under 27168, each with a `conhost 0x4` | snapshot creation times, bucketed by hour |
| 09-11 20:57–21:26 | five Claude Code `Monitor` loops start; none ends | snapshot command lines |
| 09-11 22:32, 22:33, 22:50 | the operator launches the App (Release `246b38a3`, `135e05e1`); each `app.start` is preceded by `terminal.start surface=terminal-1 integration=PowerShell` | workbench log |
| 09-11 (conductor) | a hung background `verify-test-run.py --update` is ended with `taskkill /F /IM testhost.exe` (DC-117's shape) | conductor's brief |
| 09-12 06:40 | conductor's census: 0 ours, 1 build-server, 238 foreign, 30 unknown | `census-2026-09-12-0640Z.txt` |
| 09-12 13:17–13:23 | lane test runs (`terminal.start` bursts with `session-terminal:` ids; `VBCSCompiler` console 13:23:16) | workbench log, snapshot |
| 09-12 13:36:31–57 | the operator opens two WT tabs and an interactive `copilot.exe` | snapshot |
| 09-12 13:39:05 | the operator launches the App (Release `8d54aadc`) on `TheTerrace`; `terminal-1` starts; daemon 13:39:15; App gone by 13:50:57 with **0** headless hosts left | workbench log, snapshots |
| 09-12 13:39:16 | this session's census (table above) | snapshot |
| 09-12 13:57–14:02 | the five measurements below | test output |
| 09-12 14:03–14:04 | nine samples, 10 s apart: 0 test hosts, 0 product headless hosts | sampler |

## System map

```
AiDe.App.exe ──CreatePseudoConsole──▶ conhost.exe --headless   (child of the App; NOT in any job;
      │                                     │                    lives while the pty handle is open)
      │──CreateProcessW(+PSEUDOCONSOLE)──▶ powershell.exe -NoLogo -NoExit -EncodedCommand <integration script, $global:__AideNonce>
      │         └── AssignProcessToJob(KILL_ON_JOB_CLOSE)     (child of the App; in the session's job)
      │
      ├── TerminalSurface ── PumpAsync ── session.Output …  WatchForExitAsync: child exits → Complete(exit) → closes NOTHING
      ├── TerminalSurface.Dispose (tab closed; WorkbenchAdapter.Render) → DisposeAsync (fire-and-forget): ClosePseudoConsole → TerminateProcess → CloseHandle(job)
      └── MainWindow.Closed → WorkbenchShell.Dispose: session documents, persistence, watcher — NO TerminalSurface → process exit closes the handles
```

Stocks: the pty host per session; the shell per session; the log's `terminal.start` lines (4,115 on
09-11, 572 on 09-12; **zero** `terminal.stop`/exit/dispose lines exist because no such event
exists). Flows: starts are counted (`TerminalHostingLedger`), ends are not. Delay: a host released
only at App exit is invisible to a census that reads a live App as `ours-live`.

## Hypotheses considered (fishbone — the population can have more than one source)

| # | hypothesis | evidence for | evidence against | verdict |
|---|---|---|---|---|
| H1 | Windows Terminal's Copilot agent leaks one `node higgsfield-mcp` (+ conhost) per use | 111 + 111 under `wta.exe`→`copilot.exe`, creation times spread over 20 h in usage-shaped bursts; 256 at the fourth census; reset at WT restart | **corrected (slice 0):** the "use" is one of OUR ConPTY shells carrying an inherited `WT_SESSION`; 4/4 → 0/4 measured | **Verified foreign by parent, ours by cause** — the population the operator sees, and ours to stop |
| H2 | test runs leave product hosts behind (the lanes' `dotnet test`; the killed testhost) | 4,115 `terminal.start`/day from tests | 0 headless hosts at 06:40Z, 13:39Z, 13:50Z, 14:03–14:04Z (9 samples); the kill path measured 1 → 0 | **Refuted for today**; transient during a run (not sampled during one — none ran in the window) |
| H3 | Claude Code's own shells | 5 `Monitor` loops + 5 conhosts from yesterday | not the product | **Verified harness** — for the conductor, not the repair plan |
| H4 | the App's exit leaves its ConPTY hosts | `WorkbenchShell.Dispose` disposes no surface | measured: window close 1 → 0; exit-undisposed 1 → 0; kill 1 → 0 | **Refuted** |
| H5 | closing a tab leaks its host (`DisposeAsync` fire-and-forget; `ClosePseudoConsole` hang) | plausible on older Windows | measured: dispose 1 → 0 with the owner alive | **Refuted** on this machine (Windows 11 26200) |
| H6 | a shell that exits leaves its host held by the live App | `WatchForExitAsync` → `Complete` touches no handle | — | **Verified** (red): 1 → 1 at +3 s, owner alive |
| H7 | the census cannot see an orphaned product host | `classify()` walks ancestry; a conhost carries no path; `unknown` is the terminus | — | **Verified** (red self-test): the fixture's orphaned host + shell classify `unknown` |

## Exit paths — measured, not reasoned (control c)

All five use the same key as the census (`Win32_Process` parent pid + `--headless`), first prove
the key sees ≥ 1 host, then count. Windows 11 Pro 10.0.26200. Verbatim from the test output.

| # | path | how | live | after | verdict |
|---|---|---|---|---|---|
| 1 | **App window close** (the X button) — real `AiDe.App.exe`, ShellExecute launch, `AIDE_WORKSPACE_ROOT` temp workspace, `CloseMainWindow()` | `AppWindowCloseLeavesNoTerminalHostTests` | `conhost.exe[31296] --headless` + `powershell.exe[40688] -NoLogo -NoExit -EncodedCommand …` + webview2 + daemon | exit 0; **+5 s: 0** headless; only `AiDe.Daemon.exe` (detached by design) | clean |
| 2 | **owner exits without disposing** (`Environment.Exit(0)` with the session live — the App's shape) | `TerminalHostExitPathTests` / helper `exit-undisposed` | 1 headless + shell | **+5 s: 0** | clean |
| 3 | **owner killed** (`Process.Kill()` on itself = `taskkill /F`) | helper `kill-self` | 1 | **+5 s: 0** (exit code −1) | clean — a force-killed test host does *not* orphan its ConPTY hosts here |
| 4 | **tab closed** (`DisposeAsync` while the owner lives) | `TerminalHostInLifePathTests` / helper `dispose-then-hold` | 1 | **+3 s: 0**, owner alive | clean |
| 5 | **shell exits on its own** (`cmd.exe /c exit 0`; the pane stays) | helper `child-exit-then-hold` | 1 | **+3 s: 1 — `conhost.exe[30524] --headless` still owned by the live owner 34308** | **RED** |

A first run of path 1 with `UseShellExecute = false` measured an App whose shell had already died:
its OSC 133 prompt arrived on the *launching* console and no `powershell.exe` ever appeared among
the App's children — the inherited null stdin ended the shell at once, and the App held a
client-less headless host for its lifetime (the `TerminalGuiHostTests` finding, again). That run's
"0 at +5 s" was true and about the wrong configuration; the committed test asserts the shell is
alive beside its host before it closes anything.

## Verified findings — the controls that did not hold

**(a) `terminal.stop` does not exist — an instrumentation gap (red, two tests).**
`aide.terminal.runtime` carries `terminal.start`, `terminal.write`, `terminal.osc.refused` and
nothing else (`ConPtyTerminalSession.cs:214, 354, 520`); `WorkbenchDiagnostics` has `TerminalStart`
and no stop (`WorkbenchDiagnostics.cs:120`). A straggler is a start with no end, and the log cannot
show one; every report therefore restarts from a process list. Red: `TerminalStopEventTests`
(`Assert.Single() Failure: The collection was empty` × 2 — dispose and child-exit) and
`TerminalSurfaceStopLineTests` (`The collection did not contain any matching items`).

**(b) The census cannot see an orphan (red, two self-test rows).** `classify()` attributes by
worktree path in the chain's argv and by ancestor *names*; a `conhost.exe --headless` whose owner
died has one hop of ancestry, no path, and no name — `unknown`, "never removed" (`reap-stragglers.py:180–220`).
Self-test rows 5d/5e added: an orphaned `--headless` conhost beside an orphaned
`powershell.exe -EncodedCommand <$global:__AideNonce …>` with the same dead parent and creation
times within a second must be `ours-orphaned`; Windows Terminal's `OpenConsole.exe --headless` must
not; a lone signature-less orphan stays `unknown`. Result: `got 'unknown', wanted 'ours-orphaned'` × 2.
Two adjacent misses found on the way: `FOREIGN_ROOTS` matches `IntelligentTerminal` and `ollama`
against *names*, so Windows Terminal's own seven processes and Ollama's `cmd.exe` wrapper are filed
`unknown`, inflating the honest bucket by nine; and a build server's console host is `unknown`
while the server is `build-server`.

**(c) The exit paths hold; one in-life path does not (measured above).** The four prior fixes
were all about *process* exit and containment. The one product mechanism that keeps a host is the
session's own completion: `WatchForExitAsync` → `Complete(exit)` marks the state `Ended` and
releases nothing (`ConPtyTerminalSession.cs:527–560`); `TerminalSurface.PumpAsync` returns and the
pane keeps its dead session (`TerminalSurface.cs:489–528`). One client-less `conhost.exe --headless`
per ended pane, for the App's lifetime, counted `ours-live` by the census because its parent is a
live App under a worktree path.

**(d) Job-object coverage.**

| spawner | what it starts | kill-on-close job | the ConPTY host | notes |
|---|---|---|---|---|
| `ConPtyTerminalSession.StartAsync` (App panes, probes, in-process tests) | the shell | **yes** — created before the child, checked assign (`:243–270`); the assign window is accepted and unmeasured (`PROC_THREAD_ATTRIBUTE_JOB_LIST` recorded as the end state) | **no** — `conhost.exe --headless` is created by `CreatePseudoConsole` as a child of the caller, outside the job; it follows the pty handle (measured: paths 1–4) and not the child (path 5) | |
| `AcpEngineProcess.Start` (governed runs / ACP engines) | the engine tree | **yes** (`AcpEngineProcess.cs:133`) | n/a | TH2's fix; holds |
| `TerminalHostLauncher.RunInNewConsoleAsync` (tests → helper) | the helper, `CREATE_NEW_CONSOLE` | **yes** (`TerminalHostLauncher.cs:65–80`) | the helper's hosts are inside its job | TH1's fix; holds; this investigation added a pre-release hook so a count can precede the release |
| `ShellBootstrap.Launch` (App → daemon) | `AiDe.Daemon.exe` | **no — by design** (idle grace 30 s; `ours-detached`) | its own `conhost 0x4` | measured retiring at +30 s (DC-131) |
| `WorktreeProvisioner`, `WorkbenchShell.cs:2628` | `git` | no | n/a | waited on; short-lived |
| `ShellContrastCensusTests` → ContrastProbe; `TerminalGuiHostTests` → TerminalProbe; `AppWindowCloseLeavesNoTerminalHostTests` → the App | the probe / the App | no (`Kill(entireProcessTree)` on the failure path) | the probe's own | the App's exit is what path 1 measures |
| `verify-test-run.py:159` → `dotnet test` | the test hosts | no job; `MSBUILDDISABLENODEREUSE=1` + `Directory.Build.rsp`; `reap-stragglers.py --reap` afterwards (`:195`) | in-process tests' hosts follow the test host (path 2/3) | DC-123's ring |

## Causes ruled out

- **The App's or a probe's exit leaves hosts** — measured 0 on four paths (table above).
- **The killed test host orphaned hosts that persist** — none present at any census; the kill path measured 1 → 0.
- **Our `EngineCatalog` launched the `copilot --acp --stdio` pool** — parent is `wta.exe` with the literal `--agent "copilot --acp --stdio"`; the chain's creation times validate; no AiDe process existed at 17:02Z; `EngineCatalogTests` refuses it (DC-131 recurrence 2/3 found the same).
- **`ClosePseudoConsole` hangs and the fire-and-forget dispose leaks** — measured 1 → 0 at +3 s.

## Specific fix(es) for this instance — proposed, not made

1. **Release the host with the child, not the owner.** In `ConPtyTerminalSession.WatchForExitAsync`, after `Complete(exit)` close the pseudo console (and the job, which is then empty), so an ended session holds no OS host; `DisposeAsync` stays idempotent over an already-closed pty. Regression: `TerminalHostInLifePathTests.ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives` (red today, 1 → expected 0). Blast radius: the pane's screen and exit code are already captured before this point; readers of `Output` see a completed channel either way. Rollback: revert one method.
2. **`terminal.stop`.** Open `terminal.stop` on `aide.terminal.runtime` at the top of both end paths (`Complete` and `DisposeAsync`) with `session.id`, `session.killed`, `session.exit_code`; `TerminalSurface.Dispose` writes `WorkbenchDiagnostics.TerminalStop(surfaceId, reason)` at its top (the attempt, `SessionDisposalSignal`'s idiom); `TerminalHostingLedger` grows a `Completions` counter so "still hosted = starts − stops" is a number. Regression: the two red test classes above.
3. **`ours-orphaned` in the census.** Extend `classify()` with the signature rule the self-test now states (dead parent + `--headless` + a sibling shell whose `-EncodedCommand` decodes to `$global:__AideNonce`, same parent pid, creation within ±2 s), match `FOREIGN_ROOTS` against the executable path as well as the name (Windows Terminal, Ollama), and file a build server's console with its server. `--reap` may retire `ours-orphaned` under the same idle guard as `ours-straggler`. Regression: `--self-test` rows 5d/5e (red today).
4. **The foreign share gets an outcome, not a label.** `report()` prints, for the largest foreign root, the one action that shrinks it — here: the `higgsfield` server in `~/.copilot/mcp-config.json` is spawned per use by Windows Terminal's Copilot agent and never reaped (111 in 20 h; 256 before the last restart): scope it to the repository's MCP config instead of the global one, or remove it until Copilot CLI / the `wta.exe` host fix their child lifecycle, and restart Windows Terminal — then re-count. This is the operator-facing half of DC-131.

## Generalization — the failure class

**Two classes, registered as DC-156 and DC-155; DC-131 gains recurrence 4.**

**DC-156 — A resource acquired for a child is released with the owner, not with the child.** The
pty host is created for the shell; the shell's exit completes the session's *state* and touches no
*handle*; the host lives as long as the App. Signature: an `Ended` state that still owns OS
objects; a `Complete`/`OnEnded` path with no `Close*`; a host count equal to panes rather than to
live shells. Sweep: `AcpEngineProcess` (the job is closed with the process — `CloseJob` on the exit
path, sibling ruled out by reading `:133–160`); `ShellBootstrap` (no handle held — ruled out);
`WebSurfaceHost`/WebView2 (owns a browser process per surface — *not swept*, next step). Control:
the red in-life test; a `TerminalHostingLedger.Completions` counter once fix 2 lands.

**DC-155 — A symptom owned by someone else is closed by attribution, not by an outcome.** The
fourth census attributed the population correctly to Windows Terminal's agent and stopped at
"foreign — reported only"; the population regrew and the operator reported it a fifth time.
Signature: the largest class in a census is `foreign`; the close carries no action for it; the same
foreign root appears in two consecutive censuses; the operator's next report arrives with the
same count. Why it survives DC-131: DC-131's control demands an attribution column, and the column
was correct — **the control asked "whose is it?" and the operator asked "why is it still there?"**
Control: the census report names, for the largest foreign root, the action that shrinks it and the
re-count that proves it (fix 4); the close of a population report includes a post-action count.

**DC-131, recurrence 4:** the population was reported a fifth time after a census with a correct
attribution column. The column was right; the *close* still answered the wrong question, and the
product's own share was unmeasurable from the product (no stop events), so each report re-ran the
whole investigation. The control gains the two halves above.

**Markers harvested** (CI9): `ConPtyTerminalSession.cs:80` `simplify:` (output capacity) — not
triggered; the `:253–268` "assign window accepted and unmeasured" comment is a written, unmeasured
gap and stays a next step; `:255` "the job object guarantees the child dies regardless"
(`TerminalSurface.cs:603`) is now a *measured* claim for the shell and a *false* one for the host
(path 5). No `assume:` markers in `src/AiDe.Core/Terminal/` or `TerminalSurface.cs`.

**Harness finding for the conductor (not the repair plan):** five Claude Code `Monitor` loops
(`until false; do sleep 30; done`) from 2026-09-11 20:57–21:26Z are still running under
`claude.exe` 17664, each holding a `conhost.exe`. They are the harness's, they will not end on
their own, and this investigation did not end them.

## Phased repair plan — one-node T1 slices, the order the conductor asked for

| phase | scope (code + tests) | failure mode eliminated | validation | depends on |
|---|---|---|---|---|
| **1 — instrumentation** | `terminal.stop` activity in `ConPtyTerminalSession` (both end paths, tagged); `WorkbenchDiagnostics.TerminalStop` written at the top of `TerminalSurface.Dispose`; `TerminalHostingLedger.Completions`; tests that write to the operator's log without a `Sink` get one (the 4,115 fixture lines) | a start with no end is invisible; every report restarts from a process list | `TerminalStopEventTests` × 2 and `TerminalSurfaceStopLineTests` red → green; `verify-test-run.py` green; the operator's log gains a stop per start on the next App run | — |
| **2 — attribution** | `reap-stragglers.py`: `ours-orphaned` rule (signature + creation-time correlation), `FOREIGN_ROOTS` on path as well as name, build-server console filed with its server, `--reap` extended under the idle guard; the report names an action for the largest foreign root | a dead-parent product host hides in `unknown`; Windows Terminal and Ollama inflate `unknown`; the foreign share is a label with no outcome | `--self-test` rows 5d/5e red → green (and the 19 existing rows still green); a live census on this machine shows `unknown` ≤ the harness loops | — |
| **3 — the held host** | `WatchForExitAsync` closes the pty and the job after `Complete`; `DisposeAsync` idempotent over a closed pty; `TerminalSurface` shows the ended state it already captures | one client-less `conhost.exe --headless` per ended pane for the App's lifetime | `TerminalHostInLifePathTests.ASessionWhoseChildExited…` red → green; paths 1–4 stay green; `TerminalSessionConformanceTests` green | 1 (the stop event is how the fix is observed in the field) |
| **4 — exit paths as a gate** | keep `TerminalHostExitPathTests`, `TerminalHostInLifePathTests`, `AppWindowCloseLeavesNoTerminalHostTests` in the slow ring; `verify-test-run.py --assert-clean` reads `ours-orphaned` | a regression on any exit path is caught by a count, not a comment | the three classes green in `verify-test-run.py`; `--assert-clean` fails on a fixture orphan | 2 |
| **0 — the cause (correction, landed first)** | `ConPtyInterop.BuildEnvironmentBlock` strips `WT_*` from every ConPTY child; the launcher hands the helper the same block; the census's action line names the cause and reports the birth correlation | our shells make Windows Terminal's agent spawn one MCP server + host per shell, for WT's lifetime | `EnvironmentBlockTests` + `TerminalChildEnvironmentTests` red → green; 4/4 → 0/4 births measured | — |
| **5 — the operator's population** | not code: restart Windows Terminal after slice 0 is running (the existing pool was born before the fix); the next census is compared with 371 (14:58Z). The global `higgsfield` MCP server is the operator's choice — it was never the cause | the same pool regrows and is reported a sixth time | a census 24 h later with `copilot.exe`-rooted rows ≈ 0 and the action line's correlation at 0 | slice 0 |

## Residual risk & follow-ups

- **What would change the diagnosis:** the operator answering that they see `conhost.exe` under
  **AI-DE** in numbers larger than their open panes — then a source this census did not see exists
  (a transient during test runs, or a path not measured: an agent pane whose *agent* exits inside
  `-NoExit` PowerShell keeps its shell alive and is path 5 only when the shell ends).
- The measurements are one machine, one OS build (Windows 11 26200). Older builds' `ClosePseudoConsole`
  behaviour is documented differently; path 4 should be re-measured on a CI runner image.
- `WebSurfaceHost` (WebView2 browser processes per surface) was not swept for DC-156.
- The `PROC_THREAD_ATTRIBUTE_JOB_LIST` assign-window gap remains written and unmeasured.
- Test runs write into the operator's workbench log without a `Sink` (fixture ids `terminal-1`,
  `terminal#…`, `session-terminal:…deadbeef`) — phase 1 covers it; until then the operator's log is
  mostly not the operator's.
- The register's tail carries a stray `## 5. What this note does not decide` section after DC-153
  (DC-136's shape) — reported, not touched here.

## Gate record

- **Stage 0:** no mechanism named before the population was counted and every host attributed (DC-131).
- **Stage 3:** H1, H3 verified by creation-time chains and command lines; H4, H5 refuted and H6 verified by measurement with the key shown to return ≥ 1 first; H7 verified by the red self-test.
- **Stage 4 (adversary):** *"the 223 are ours through `EngineCatalog`"* — defeated by the parent's argv and the chain's creation times; *"the exit paths leak on the real App and not on the helper"* — defeated by path 1 on the real binary with the shell alive; *"the first App run proved the same"* — it did not (dead-shell configuration), and the committed test asserts the shell is present.
- **Test Architect:** every claim above has a count; the red tests were observed failing on the un-fixed code (outputs quoted). **Security:** no trust boundary touched. **SRE:** the instrument (paths 1–5) is the one the field will use.
- **Nothing was reaped, killed, stashed, rebased, or written outside this worktree.** The App tests launched the App and the helper from this worktree only; each ended by its own path.
- **Stopped at the report.** No fix made.
