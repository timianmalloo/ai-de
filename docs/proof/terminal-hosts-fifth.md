---
id: proof-terminal-hosts-fifth
title: "Proof Pack - Terminal hosts, the fifth report: INV-0010 slices 0-4"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c"
tags: [terminal, conpty, conhost, straggler, census, reap-stragglers, terminal-stop, ledger, dc-131, dc-156, dc-155, dc-157, proof-pack, inv-0010]
links:
  - { to: inv-0010-terminal-hosts-the-fifth-report, rel: tested-by }
  - { to: defect-classes, rel: relates-to }
  - { to: adr-0005-terminal-runtime-boundary, rel: depends-on }
  - { to: adr-0006-terminal-delivery-semantics, rel: depends-on }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Evidence for INV-0010's repair slices. Slice 0 (the correction): the "foreign" pool was ours by
  cause — our ConPTY shells inherited WT_SESSION and Windows Terminal's agent attached one MCP
  server per shell; the runtime now strips WT_* from every ConPTY child (measured 4/4 → 0/4).
  Then the four: `terminal.stop` on every end-of-life path (runtime
  activity + workbench line, paired by id with `terminal.start`) and the ledger's completions so
  `starts − stops` is the number of held hosts; the census attributes an orphaned product host
  (`ours-orphaned`, by the runtime's own signature), files Windows Terminal and Ollama by executable
  path, and ends with an ACTION line for the largest foreign root (DC-155); the pty and the job are
  released when a pane's shell exits (DC-156, measured 1 → 0 with the owner alive); and the five exit
  paths are read by name in the Windows CI job by a gate with its own self-test. Every red was
  observed on the un-fixed code before it went green.
---

# Proof Pack: Terminal hosts, the fifth report — INV-0010 slices 0–4

## Slice 0 — the cause: our shells, Windows Terminal's agent, `WT_SESSION`

| # | claim | evidence | red observed | green | conf. |
|---|---|---|---|---|---|
| 0.1 | a ConPTY child of ours never receives a `WT_*` variable; everything else passes through | `EnvironmentBlockTests.AParentInsideWindowsTerminal_HandsItsChildNoWT_Variable_AndEverythingElse` (the block builder: `WT_SESSION`, `WT_PROFILE_ID`, a lowercase `wt_` set on the parent → none in the block; a control variable and `PATH` present; with extras too) | `Assert.NotNull() Failure: Value is null` (no block was built when nothing was extra) | Passed | Verified |
| 0.2 | … through a real pseudo console | `TerminalChildEnvironmentTests` → helper mode `env-scrub` (`cmd.exe /c set` under ConPTY, VT stripped, names read at line starts) | exit 2: `lines=100 wt-lines=3` — `WT_COM_CLSID`, `WT_PROFILE_ID`, `WT_SESSION` arrived | exit 0: `wt-lines=0 control-present=True` | Verified |
| 0.3 | with no `WT_*` on the parent and nothing extra, the child still inherits (the common path unchanged) | `EnvironmentBlockTests.NoExtras_AndNoWindowsTerminalVariables_MeansInherit…` | n/a (kept behaviour) | Passed | Verified |
| 0.4 | **the mechanism, measured on this machine (read-only except my own spawns):** 4 product-shaped sessions (helper `exit-undisposed`, PowerShell + integration, 6 s) with `WT_*` inherited → **4** `node higgsfield-mcp` births in 40 s (alive 166 → 170); the same 4 with `WT_*` removed → **0** | `wt_probe2.ps1` (scratchpad; counts `node.exe higgsfield` + `copilot.exe` created after the spawn) | — | — | Verified (n=4, one run each) |
| 0.5 | a bare inherited `WT_SESSION` is not the trigger: 4 hidden `powershell.exe -NoLogo … Start-Sleep 30` with `WT_*` inherited, no console of their own → **0** | `wt_probe.ps1` | — | — | Verified |
| 0.6 | **the child is the trigger, and the product scrub suffices:** after the fix, 4 helpers that themselves still inherit `WT_*` (their own console) but whose ConPTY child is scrubbed → **0** (was 4) | `wt_probe2.ps1` re-run on the fixed helper | 4 | 0 | Verified |
| 0.7 | the launcher hands the helper the same block (`TerminalHostLauncher`, `CREATE_UNICODE_ENVIRONMENT`) so the helper's shape matches a CI runner; the helper-driven suites stay green | `TerminalHostExitPathTests` ×2, `TerminalHostInLifePathTests` ×2, `TerminalChildEnvironmentTests`, conformance ×12 | — | green | Verified |
| 0.8 | the census names the cause and the mechanism, not the operator's config, and reports the birth correlation (or *not recorded*) | `reap-stragglers.py --self-test` rows 5j (corrected) + 5k (`births_near`) | `it names the cause …: got False`; `it does not send the operator to their MCP config: got False` (the first fix's text) | 48/48 | Verified |

**Census after slice 0's fix, 14:58Z (read-only):** `ACTION: 371 under wta.exe[14152] -> copilot.exe[27168] (185 x node.exe higgsfield-mcp/src/server.js): CAUSED BY THIS REPOSITORY, foreign only by parent: … Restart Windows Terminal, then re-count with this tool: a birth AFTER the fix is a spawn path that still inherits WT_*.` / `54 of 371 dated members born within 10s of one of our 5218 terminal.start lines (workbench log: the App's sessions; test-run sessions are not in it)`. The pool grew 291 → 371 during this session's own pre-fix test runs — the correlation's shape, live. Slice 5 is now: restart Windows Terminal with the fix running; compare the next census against 371.

**Corrections made:** `docs/investigations/INV-0010…md` (a *Correction* section, H1's verdict, phase 0 and phase 5 rows); DC-155 (instance 2 = the misattribution; the control = the cause-vs-parent rule); DC-131 recurrence 4.

**Concurrent lanes during the measurements.** CV-1 (`lane/conversation-cv1`) and SH-1 (`lane/shell-sh1`) were live on this machine and their `dotnet test` runs spawn ConPTY shells legitimately; the conductor's App and Claude Code sessions run inside Windows Terminal tabs. Each probe counted only `node.exe higgsfield` / `copilot.exe` rows **created after the probe's own spawn time** inside its 30–40 s window, so a lane's earlier shells are outside the window by construction; a lane's shell born *inside* a window would read as a birth in **both** arms, and the B arms (scrub) read 0 every time — the A/B difference (4 vs 0, twice) is therefore not the lanes'. The census figures (291 → 371 between 14:41Z and 14:58Z) include the lanes' pre-fix births as well as this session's own full-suite runs (App 643 + Core 2263 tests before the fix); their shells are attributed `ours-live` by ancestry to their worktree paths while they run, and the hosts they left under `wta.exe` are indistinguishable from ours by ancestry — only the birth correlation (App sessions only) separates them, which is the control's stated limit.

- **Investigation:** `docs/investigations/INV-0010-terminal-hosts-the-fifth-report.md` (the census, the four controls' findings, the plan). Slice 5 (the operator's global Copilot MCP config) is the operator's and is not here.
- **Branch:** `fix/terminal-hosts-5` from `investigate/terminal-hosts-5` (= `main` `8d54aadc` + INV-0010); session `hosts-fix`.
- **Product:** `src/AiDe.Core/Terminal/ConPtyTerminalSession.cs` (`Complete` → `EmitStop`; `WatchForExitAsync` → `ReleaseHost`; `Take(ref)`; the start-failed stop), `src/AiDe.Core/AgentPlane/TerminalHostingLedger.cs` (`Completions`, `TerminalStopActivity`), `src/AiDe.App/Workbench/WorkbenchDiagnostics.cs` (`TerminalStop`), `src/AiDe.App/Workbench/TerminalSurface.cs` (`RecordStop` on the pump's end, at the top of `Dispose`, `RecordOwnerClosing`), `src/AiDe.App/Workbench/WorkbenchShell.cs` (`Dispose` writes one line per live terminal), `tools/reap-stragglers.py`, `tools/verify-terminal-host-exit-paths.py` (new), one appended step in `.github/workflows/build.yml`.
- **Tests:** `tests/AiDe.Core.Tests/{TerminalStopEventTests,TerminalHostInLifePathTests,TerminalHostExitPathTests,AgentPlane/TerminalHostingLedgerTests}.cs`, `tests/AiDe.App.Tests/{TerminalSurfaceStopLineTests,AppWindowCloseLeavesNoTerminalHostTests}.cs`, `tests/AiDe.Core.TerminalHost/Program.cs` (the `child-exit-then-hold` child now lives seven seconds), `tools/reap-stragglers.py --self-test` (43 assertions / 16 tables), `tools/verify-terminal-host-exit-paths.py --self-test`.

## The invariant: starts − stops = held hosts

| where | start | stop | pairing key | one stop per start |
|---|---|---|---|---|
| runtime (`aide.terminal.runtime`) | `terminal.start` (attempt, before interop) | `terminal.stop` in `Complete` — the guarded `Ended` transition both end paths funnel through; plus the start-failed catch | `session.id` | `Complete` returns early once `Ended`; the start-failed path emits because its start was counted |
| ledger (`TerminalHostingLedger`) | `Constructions` | `Completions` | — | `ALedgerCountsOneCompletionPerSession_OnDisposeAndOnChildExit`: dispose → 1/1, child exit → 2/2, dispose-after-exit → still 2/2, **`Constructions − Completions == 0`** |
| workbench log (`%LOCALAPPDATA%\AiDe\logs\workbench-*.log`) | `terminal.start` (the launch decision) | `terminal.stop` with `reason`, `durationMs`, `exitCode` | `surface` (and `session`) | `TerminalSurface._stopRecorded` (`Interlocked.Exchange`): the first end path writes, the rest are no-ops |

**`terminal.stop` shape.** Runtime activity tags: `session.id`, `session.generation`, `session.killed` (bool), `session.exit_code` (int, absent when unknown — a killed session has none), `session.end_reason` (`child-exited` · `killed` · `start-failed` — one token, one meaning across the two surfaces a census joins), `session.duration_ms`. Workbench line: `{"ts","evt":"terminal.stop","surface","session","reason","durationMs","exitCode"}` with `reason` ∈ `child-exited` (the shell ended itself; `exitCode` set) · `killed` (the pane closed over a live shell) · `disposed` (the pane closed with no session — a failed start) · `owner-closing` (the window closing with the pane open; the process exit ends the shell). A value the writer does not know is `null`, never 0 (DC-137).

## The reds → green

| # | claim | test | red observed (un-fixed) | green (fixed) | oracle | conf. |
|---|---|---|---|---|---|---|
| 1 | a disposed session emits one `terminal.stop` with its id, `killed=true` | `TerminalStopEventTests.ADisposedSession_EmitsTerminalStopWithItsSessionId` | `Assert.Single() Failure: The collection was empty` | Passed | the listener sees nothing without the emission | Verified |
| 2 | a session whose child exits emits one stop with `killed=false`, `exit_code=7`; a later dispose adds none | `…ASessionWhoseChildExits_EmitsTerminalStopWithTheExitCode` | same | Passed | `Assert.Single` over both end paths | Verified |
| 3 | the ledger counts one completion per session on both end paths | `TerminalHostingLedgerTests.ALedgerCountsOneCompletionPerSession_OnDisposeAndOnChildExit` | `Expected: 1 Actual: 0` (listener wired, nothing emitted) | Passed | a real ConPTY session, counts asserted after each path | Verified |
| 4 | a closed tab writes `terminal.stop` for its surface, `reason=killed`, `exitCode=null` | `TerminalSurfaceStopLineTests.ADisposedTerminalPane_WritesATerminalStopLineForItsSurface` | `The collection did not contain any matching items` | Passed | the sink captures every line the pane writes | Verified |
| 5 | a shell that exits with its own non-zero code writes `child-exited` with **that** code and the session id; the runtime's stop for the same session agrees (E12); the later dispose writes no second stop | `…AShellThatExits_WritesChildExitedWithTheCode_AndTheLaterDisposeWritesNoSecondStop` (the pane's shell is a batch `@exit 4`, via the `TerminalSurface.CommandLine` seam, restored) | same (no stop line at all); the first shape of this fact ran only the console-less EOF path with code 0 — the Test Architect's finding — and was replaced by the deterministic `exit 4` | Passed: `exitCode == 4`, `session == Session.SessionId`, runtime `session.exit_code == 4`, `end_reason == child-exited` | a `killed` line or a default 0 cannot pass | Verified |
| 5a | a pane whose start failed writes one `disposed` stop with `session` and `exitCode` null (the decision line and the failure line are two `terminal.start`s on the surface, exactly one carrying `failure`) | `…APaneWhoseStartFailed_WritesOneDisposedStop_WithNoSessionAndNoCode` | mutation (collapse the `disposed` token): `Expected: "disposed" Actual: "killed"` | Passed | the token the invariant's reader subtracts by | Verified |
| 5b | the runtime's start-failed path closes its own pair: one `terminal.stop`, `end_reason=start-failed`, no exit code; ledger 1/1 | `TerminalStopEventTests.AStartThatFails_EmitsOneTerminalStop_SoThePairIsClosed` (`aide-no-such-shell-<guid>.exe`) | mutation (delete the catch's `EmitStop`): `Assert.Single() Failure: The collection was empty` | Passed | `starts − stops` would drift by one per failed start | Verified |
| 5c | the App test assembly's no-op sink is installed | `…TheAssemblyInstallsANoOpSink_SoFixturesDoNotWriteTheOperatorsLog` | n/a (guard) | Passed | deleting the initializer reddens it | Verified |
| 6 | the shell's `Dispose` writes one `owner-closing` stop per live terminal, and a later dispose adds none | `…TheShellsDispose_WritesOneOwnerClosingStopPerLiveTerminal` | same | Passed | one stop per terminal id, count equal | Verified |
| 7 | **the held host is released with the child:** 0 headless hosts 3 s after the shell exits, owner alive; the exit is the child's own (`code=3` asserted) | `TerminalHostInLifePathTests.ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives` | `1 headless console host(s) still owned by the live owner 3s after 'child-exit-then-hold'` (**1 → 1**) | `owner … live; headless hosts it owns: 1` → `state reached: child-exited … code=3` → `+3s … headless hosts it owns: 0` (**1 → 0**) | the key sees ≥ 1 first, then counts; both clauses observed on the fix (DC-157); a `TerminateProcess`ed child (code 1) fails the code assertion | Verified |
| 8 | the census names an orphaned product host and its shell `ours-orphaned` | `reap-stragglers.py --self-test` rows 5d | `got 'unknown', wanted 'ours-orphaned'` × 2 | 43/43 | signature: `--headless` + sibling `-EncodedCommand` decoding to `$global:__AideNonce`, same dead parent, ±2 s | Verified |
| 9 | the negatives hold: Windows Terminal's `OpenConsole --headless` is not ours; a lone signature-less orphan stays `unknown`; a host 4 s from the shell is not its sibling | rows 5e, 5i | stayed green throughout | green | the rule cannot be satisfied by `--headless` alone | Verified |
| 10 | Windows Terminal (itself, its OpenConsole, its tab shells) and Ollama's `cmd.exe /C` wrapper (+ its conhost) are `foreign` by executable path; a run of ours from a WT tab is still `ours-live`; a token-less shell two hops under WT stays `unknown` (the rule is one hop, pinned) | rows 5f, 5g | `got 'unknown', wanted 'foreign'` × 5; the run of ours red too until the fixture's root was pid 0 | green; live census: `unknown` 25 → **17** (the harness's own loops), `foreign` gains WT ×7, Ollama ×2 | one-hop path rule, worktree rule first; widening to the chain reddens the two-hop row | Verified |
| 11 | a build server's console host is filed with its server | row 5h | `got 'unknown', wanted 'build-server'` | green; live: `build-server 2` (server + console) | — | Verified |
| 12 | `assert_clean` fires on an orphaned session of ours | row 5i | `got 0, wanted 1` | green | the CI shape-gate reads `ours-orphaned` | Verified |
| 13 | **DC-155's control:** the report ends with an ACTION line naming the largest foreign root, the process the pool hangs from, the workload and the operator's action | row 5j (5 assertions) + a no-foreign → `None` | `the report carries an action line: got False` × 5 | green; live: `ACTION: 291 under wta.exe[14152] -> copilot.exe[27168] (145 x node.exe higgsfield-mcp/src/server.js): not AiDe's -- the Copilot agent spawns this MCP server per use and never reaps it. Scope or remove the server in ~/.copilot/mcp-config.json (or file the lifecycle defect with Copilot CLI / Windows Terminal), restart Windows Terminal, then re-count with this tool.` | grouped by root pid; via = the majority ancestor, not the first enumerated | Verified |
| 14 | the five exit paths are a CI gate that can fail | `tools/verify-terminal-host-exit-paths.py --self-test` | n/a (new gate) — the self-test fires on: no results, a `Failed` path, an unexecuted path, and a later-sorting `Passed` that would hide an earlier `Failed` (worst outcome wins) | `self-test OK`; live over this run's `.trx`: see the gate table | reads the `.trx` the Windows job already wrote; absent is a failure | Verified |
| 15 | the four prior exit paths still hold | `TerminalHostExitPathTests` ×2 (`exit-undisposed`, `kill-self`), `TerminalHostInLifePathTests.ADisposedSession…`, `AppWindowCloseLeavesNoTerminalHostTests` | green before (INV-0010) | green after | ≥ 1 live then 0 at +5 s / +3 s | Verified |

## Census before / after (this machine, read-only, nothing ended)

| | INV-0010 13:39Z (before) | this branch 14:41Z (after) |
|---|---|---|
| host-like processes | 275 | 354 |
| `ours-live` / `ours-detached` / `ours-straggler` / **`ours-orphaned`** | 8 / 1 / 0 / *(no class)* | 0 / 0 / 0 / **0** (no App running at the census) |
| `build-server` | 1 (+ its console filed `unknown`) | 2 (server + its console) |
| `foreign` | 240 | 317 (the pool grew 223 → 291; + Windows Terminal ×7, Ollama ×2 moved out of `unknown`) |
| `unknown` | 25 | **17** — 11 `bash.exe` + 5 `conhost.exe` (Claude Code's `Monitor` loops) + 1 transient `powershell.exe` |
| action for the largest foreign root | *"foreign — reported only"* | the `ACTION:` line above |
| product's own share, from the product | unmeasurable (starts only) | `starts − stops` (ledger) and `terminal.start` ↔ `terminal.stop` (log) |

**The five exit paths, counted (this branch, Windows 11 26200):**

| path | test | live | after | verdict |
|---|---|---|---|---|
| window close | `AppWindowCloseLeavesNoTerminalHostTests` | ≥ 1 (+ `powershell.exe -EncodedCommand` beside it) | 0 at +5 s | clean |
| owner exit (undisposed) | `TerminalHostExitPathTests.AnOwnerThatExitsWithoutDisposing…` | 1 | 0 at +5 s | clean |
| owner killed | `…AnOwnerThatIsKilled…` | 1 | 0 at +5 s | clean |
| tab close (dispose, owner alive) | `TerminalHostInLifePathTests.ADisposedSession…` | 1 | 0 at +3 s | clean |
| **child exit (owner alive)** | `…ASessionWhoseChildExited…` | 1 | **0 at +3 s** (was 1) | **clean — fixed** |

## Gate table

| gate | result |
|---|---|
| `dotnet build AiDe.sln -p:TreatWarningsAsErrors=true` (Core, App, both test projects, the helper) | 0 warnings, 0 errors |
| `python tools/verify-test-run.py` (both full projects, CHECK only) | pre-merge: App 643 (baseline 639), Core 2263 (2256); post-merge with main's floors (App 695, Core 2278): OK — App 701 (≥ 695), Core 2288 (≥ 2278), both Completed (one isolation flake in the new stop-line fact on the first post-merge run — another test's fixture shell wrote `child-exited` into the process-wide sink; every assertion is now keyed by the fact's surface id) |
| `python tools/verify-terminal-host-exit-paths.py --self-test` / live | self-test OK / OK — 5 exit paths executed and passed, read from this run's `.trx` |
| `python tools/reap-stragglers.py --self-test` | 43 assertions over 16 tables, all passing |
| `python tools/verify-defect-register.py` | OK — 156 classes; counts 80 · 59 · 17 |
| every `tools/verify-*.py` | green (34 gates + self-tests; `verify-stranded-audit.py` reports uncommitted audit lines in the conductor's own worktree, not this one — reported, not touched) |
| `regenerate-derived.py` | green after the audit entry (`docs-index.js`, `_meta.json`, site figures regenerated; `verify-derived-views.py` and `verify-site-figures.py` OK) |

## Register

- **Ids:** `main` allocated DC-154 to CV-0's class while this branch was open; INV-0010's classes were re-issued on the merge as **DC-155** (a symptom owned by someone else is closed by attribution — the number the INV branch already gave it, so the two branches do not allocate independently), **DC-156** (a resource acquired for a child is released with the owner) and **DC-157** (new); every citation in this tree follows, and `verify-id-allocators.py` is green across 32 branches. The conductor's note proposed the reverse order; the allocator gate decided.
- **DC-156** `uncontrolled` → `controlled` (fix + sweep + the CI gate).
- **DC-155** `uncontrolled` → `controlled` (instance 2 = the misattribution; the cause removed, the ACTION line names it, the birth correlation reported; the re-count is the operator's).
- **DC-131** recurrence 4: the three controls are landed, and the population was ours by cause (noted in the entry).
- **DC-157** (new, `controlled`): a test's positive control satisfied by the defect the test guards — the `cmd.exe /c exit 0` fixture; the helper's child now outlives one instrument read.

## Residual risk & next steps

- The job's close on child exit ends anything the shell left running inside its job (a background process started from the pane) at the shell's exit rather than at the tab's close — the containment semantics ADR-0005 states, now applied at the child's end. Not measured here; a pane that relies on out-living its shell would be a new requirement.
- `TerminalSurfaceStopLineTests.ADisposedTerminalPane…` asserts `killed`: the dispose runs microseconds after construction, the shell's own EOF exit takes hundreds of milliseconds — if a host ever disposes slower than the shell dies, the line reads `child-exited` or `disposed` and the fact fails loud, never silently.
- `ConPtyTerminalSession.StartAsync`'s start-failed stop covers the catch around `StartAttachedProcess`/`AssignProcessToJob`; a `CreatePipe`/`CreatePseudoConsole` failure (before the try) still opens `terminal.start` without a stop — a next step, not observed on any machine.
- Slice 0's measurement is n=4 per arm on one machine, one Windows Terminal build (IntelligentTerminal 0.2.2395.0); the post-fix census over a day is the confirming measurement (phase 5).
- `AcpEngineProcess` starts engines with the inherited environment; an engine is not a ConPTY child and the probe did not cover it — if an engine tree ever births MCP servers under `wta.exe`, extend the scrub there.
- `WebSurfaceHost` handles no `CoreWebView2.ProcessFailed`: a browser that dies leaves a blank pane and no line (not DC-156 — a failure mode; next step).
- The register's stray `## 5. What this note does not decide` section (reported by INV-0010) was removed on `main` by CV-0's join; the merge keeps it removed.
- The read loop drains to EOF unconditionally (the SRE lens's A2): a big-output child (a full-screen TUI's last frame) is not in any fact — the confirming test (a child that writes > 8 KiB then exits; host count 0 and the watcher's task complete within 5 s) is a next step.
- `TerminalSurface.Dispose` writes `child-exited` with the code when the shell already exited but the pump's dispatcher-posted continuation has not yet run (the SRE lens's A4) — the count was never at risk, the reason was; not separately tested (the ordering is not controllable from a test).
- App tests no longer write into the operator's workbench log (a module initializer installs a no-op sink; one test that reset the sink to `null` now restores the previous). The App launched by `AppWindowCloseLeavesNoTerminalHostTests` still writes its own `app.start`/`terminal.start`/`terminal.stop` — it is the real binary and has no cross-process seam.
- Slice 5 is the operator's: scope or remove `higgsfield` in `~/.copilot/mcp-config.json`, restart Windows Terminal, re-count against **291**.

## Gate record

- Red first: every claim above names its red, observed on the un-fixed code; the fixture's positive clause was re-observed on the fix (DC-157).
- Nothing was reaped, killed, stashed, rebased, or written outside this worktree; the reaper ran in report mode only; the App tests launched the App and the helper from this worktree only.
- `build.yml`: one step appended at the end of the `build` job; nothing above it touched (X-1 owns the file).
