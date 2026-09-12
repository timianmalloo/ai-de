---
id: inv-0011-terminal-hosts-the-sixth-report
title: "Terminal hosts are being created AGAIN — the sixth report: 32 shells held alive by a hung test host, the two defects under it, and what is still unattributed"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c"
tags: [terminal, conpty, conhost, straggler, census, test-host, hang, stdout, handle-inheritance, windows-terminal, create-new-console, dc-164, dc-165, dc-170, dc-155, dc-014, x-2]
links:
  - { to: inv-0010-terminal-hosts-the-fifth-report, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: adr-0005-terminal-runtime-boundary, rel: depends-on }
  - { to: kb-agentic-session-observability, rel: relates-to }
review-by: ""
summary: >-
  The operator's sixth report, 2026-09-12 ~19:00Z. Counted first: 32 powershell.exe and 36
  conhost.exe born in the previous hour, every one a child of the CV-1 lane's App test host,
  alive 25 minutes after birth — ours. The host was hung at ~0 CPU, 30 minutes into a suite that
  takes two, in WorkbenchShell.Git → StreamReader.ReadToEnd after git had exited; the read was
  unbounded and the WaitForExit(3000) bound sat after it (DC-165). Ending the host released all
  32 (the job's kill-on-close held). A second defect was found on the way in: a ConPTY child of a
  redirected parent inherits the parent's standard handles and writes into its stdout (DC-164) —
  the mechanism behind CV-1's one flaky probe test and, re-read, behind DC-014's 2026-08-26
  instance. Both fixed red→green with E2E proof. The 513 node/conhost pairs under Windows
  Terminal's agent host are the pre-fix pool of INV-0010, unchanged in count. Still open: which
  code path started 32 shells inside a hung host — the tests' default sink discarded the events;
  it now writes a per-run ledger. §7, after the pool was ended: it regrew by 25 during one
  recount, from CREATE_NEW_CONSOLE helper launches that Windows Terminal (the default terminal)
  turns into tabs and attaches an agent to — with or without WT_SESSION; the launcher is now
  headless and a gate keeps it so (DC-170).
---

# INV-0011 — Terminal hosts, the sixth report

**Report (operator, 2026-09-12 ~19:00Z):** *"go look at the number of zombie terminal hosts being
created AGAIN … don't say it is the copilot session."*

## 1. Count first (Verified, `Get-CimInstance Win32_Process`, 18:52Z)

| Name | All | Born in the last 60 min | Parent of the recent ones |
|---|---|---|---|
| `conhost.exe` | 304 | **36** | `testhost.exe` 56892 (34), `bash.exe` (2) |
| `node.exe` | 257 | 0 | — |
| `powershell.exe` | 34 | **32** | `testhost.exe` 56892 (32) |
| `wta.exe` | 3 | 0 | — |

`testhost.exe` 56892 was the App suite of `C:\Projects\ai-de-lane-conversation-cv1` (CV-1's
final re-run), started 18:24:35Z. Its 32 PowerShell children carried the product's shell
integration (`-NoLogo -NoExit -EncodedCommand # AI-DE shell integration …`), were born
18:25:18Z–18:26:25Z (13 of them within one second), and were idle at a prompt (~0.35 s CPU, 86 MB
each) 25 minutes later. **These are ours.** The census tool's `foreign` class (513 members under
`wta.exe[14152] → copilot.exe[27168]`, 256 × `node.exe higgsfield-mcp`) is the pool INV-0010
attributed on 2026-09-11; its count has not changed since the WT_* scrub landed, and the tool
refuses to reap it by design (foreign by parent).

## 2. Why 32 shells were alive: the host was hung

The host had spent 0.55 s of CPU in 8 s, 30 minutes into a suite that completes in ~2 minutes.
`dotnet-stack report -p 56892` (Verified):

```
System.IO.StreamReader.ReadToEnd()
AiDe.App.Workbench.WorkbenchShell.Git(string, string[])
AiDe.App.Workbench.WorkbenchShell.ResolveGitFacts(string)
AiDe.App.Tests.SessionIdentityReportsTheRealWorktreeTests.ANonRepository_ReportsAnUnknownBranch_NeverAGuess()
```

plus 31 threads in `ConPtyTerminalSession.ReadLoop()` — one per live session. The same test alone
passes in 179 ms. `Git()` read the child's stdout to end **before** its `WaitForExit(3000)`, so the
bound never ran; end-of-stream on a pipe arrives when the *last* writer handle closes, and the
child's exit closes only the child's. → **DC-165**, control `ProcessRunnerBoundsTheReadTests`
(red 7.1 s against a 2 s bound, green 2 s), fix in `ProcessRunner.Run` (reads bounded by the same
timeout, a held pipe reported as the reason) and `WorkbenchShell.Git` now calls the runner instead
of keeping its own copy. `ProcessRunner` had the same shape one step later (`GetResult()` on the
reads after a bounded exit) — the sweep found it.

Ending the host (18:53Z) released all 32 shells and their console hosts within 6 s: the job's
kill-on-close held, as INV-0010 measured. The same suite re-run afterwards, sampled every 5 s
(`hang-monitor.ps1`): 793/793 in 2 minutes, PowerShell children of the host peaked at **3**,
short-lived.

## 3. The second defect, found on the way in: the child writes into the parent's stdout

CV-1's run had one red before the hang: `ANewSessionRendersInTheOperatorsRestoredArrangement` —
*the probe printed no `restore (22:33:53Z replay):` line*. The line was in stdout; it did not
**begin** a line, because `terminal-1`'s PowerShell prompt (`ESC ]133;B BEL`, no newline) had been
written into the *probe's* stdout 1.5 s after the restore started. Run outside the runner with
`> file`, the probe's stdout carried `ShellType;powershell` twice and its stderr PowerShell's
`#< CLIXML` stream (Verified).

Mechanism: `CreateProcess` with `bInheritHandles = false` and no `STARTF_USESTDHANDLES` still
hands a console-subsystem child duplicates of the parent's standard handles when those are not
console handles — every test host, every redirected probe. Windows Terminal starts its clients
with `STARTF_USESTDHANDLES` and null handles for exactly this reason (`ConptyConnection.cpp`,
`_LaunchAttachedClient`, read 2026-09-12). → **DC-164**, control
`ConPtyChildStandardHandlesTests` (the host's own stdout pointed at a pipe; red: the child's token
on the pipe; green: on the `Output` channel only). E2E: the probe re-run with redirected stdout
carries 0 shell bytes and 0 CLIXML, its restore line begins a line, its two terminals start and
stop (`terminal.stop … reason: killed`).

**DC-014 re-read.** Its 2026-08-26 instance said ConPTY attaches a child only when the launcher
owns a real console, and built the out-of-process helper on that. The new control reads the
child's output on the `Output` channel *inside* a `dotnet test` host — the host never lacked a
console; the child had been handed the host's pipe. The register carries the amendment; the helper
stays as the attached-console control.

## 4. What the operator sees, from the chair

- **During an App test run:** a handful of `powershell.exe`/`conhost.exe` under `testhost.exe`,
  each alive for seconds (fixtures that start a real shell). Peak 3 on the clean run.
- **During a hung run:** everything born in it, for as long as the hang lasts — 32 here. The hang
  class is closed (DC-165); the births are not yet attributed (§6).
- **The 513-member pool** under `wta.exe → copilot.exe` (INV-0010's foreign class) was ended on
  the operator's word at 19:52Z (`Stop-Process -Id 27168`: `conhost` 270 → 14, `node` 257 → 1).
  It had been growing by about one agent pair per helper launch in every test run — §7 — and
  that path is now closed; what Windows Terminal re-attaches to is the operator's own live tabs.

## 5. Controls landed (X-2)

| Control | Kind | Red → green |
|---|---|---|
| `ConPtyChildStandardHandlesTests.AChildsStdoutIsThePseudoConsoleNotTheHostsRedirectedPipe` | Core test, `Platform=Windows` | token on the pipe → on the channel only |
| `ProcessRunnerBoundsTheReadTests.AStrangerHoldingTheOutputPipeCannotHoldTheCaller` | Core test, `Platform=Windows` | 7.1 s → 2 s |
| `ConPtyInterop.StartAttachedProcess`: `STARTF_USESTDHANDLES`, null handles | product | — |
| `ProcessRunner.Run`: reads bounded, held pipe reported | product | — |
| `WorkbenchShell.Git` → `ProcessRunner` (one definition) | product | 4 git-facts tests green |
| App tests' default diagnostics sink → per-run terminal ledger (`%TEMP%\aide-tests\terminal-ledger-<pid>.log`) | test infra | the next §6 has a source |

## 6. Open — the 32 births inside a hung host

Parallelization is off in the App tests; the test thread was blocked; 32 shells were born over
70 s from somewhere else — a dispatcher of a window a previous test left running, or a thread-pool
continuation. The default sink of the day discarded every `terminal.start`, so the surface ids and
timestamps that would name the path do not exist. The sink now writes them; a recurrence is
attributable by reading the ledger against the run's timeline. Not modeled further (IO4: the gap is
named, and closed for the next instance rather than reasoned around).

## 7. The pool regrows — and the mechanism that was never WT_SESSION (20:10Z)

Forty minutes after the pool was ended (`Stop-Process -Id 27168`: `conhost` 270 → 14, `node` 257 →
1), the census read 25 new `node.exe higgsfield` under Windows Terminal's re-spawned agent
(`copilot.exe` 40000), born 19:58–20:10Z — the SH-3 join's recount — and **0 of them within 10 s
of a product `terminal.start`**. The product's WT_* scrub was verified in place
(`BuildEnvironmentBlock` strips whenever the parent carries `WT_*`). So the trigger was not a
ConPTY shell of ours.

Measured (`wt-attach-probe.ps1`: count `node.exe higgsfield` before, run a filter, wait 25 s, count):

| Run | `WT_SESSION` in the host | born |
|---|---|---|
| `ConPtyChildStandardHandlesTests` + `ProcessRunnerBoundsTheReadTests` + `JobContainmentTests` (one pseudo console, no helper) | set | **0** |
| `TerminalHostExitPathTests` (two `CREATE_NEW_CONSOLE` helper launches) | set | **2** |
| the same | unset | **2** |
| the nine helper-launching classes (21 tests), launcher on `CREATE_NO_WINDOW` | set | **0** |
| the whole `Platform=Windows` half (166 tests), headless | set | **0** |

`CREATE_NEW_CONSOLE` on a machine whose default terminal is Windows Terminal **is a Windows
Terminal tab**, and the agent host attaches to every tab. → **DC-170**; the launcher is headless,
the helper's "no console window" exit is retired with DC-014's premise, and
`tools/verify-no-new-console-launches.py` holds the line. INV-0010's "≈5/hour since Windows
Terminal was restarted" was this: the helper suites, run by every node's gate set.

## 8. Prior reports

INV-0010 (fifth), and the four before it that INV-0010 lists. This report's population was the
first in which the largest class was ours *and alive*; every earlier census had found 0 product
hosts at the moment of counting.
