---
id: proof-terminal-hosts-sixth
title: "Proof Pack - Terminal hosts, the sixth report: INV-0011 (X-2)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c"
tags: [terminal, conpty, conhost, test-host, hang, stdout, handle-inheritance, ledger, contrast, dc-164, dc-165, dc-014, dc-155, proof-pack, inv-0011, x-2]
links:
  - { to: inv-0011-terminal-hosts-the-sixth-report, rel: tested-by }
  - { to: proof-terminal-hosts-fifth, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: adr-0005-terminal-runtime-boundary, rel: depends-on }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Evidence for INV-0011. Two classes red→green in Core: a ConPTY child no longer inherits its
  redirected parent's standard handles (DC-164: token on the host's pipe → on the Output channel
  only), and a child-process read is bounded by the call, not by the child's exit (DC-165: 7.1 s
  → 2 s against a 2 s bound). E2E: the session-render probe run with redirected stdout carries 0
  shell bytes (was 2). The App test host's shells now end with their owner: a per-run terminal
  ledger reads 64 starts / 62 stops (was 64 / 14), with the shell disposing its panes and every
  test disposing its shell. One contrast pairing the census could only see once test-host shells
  reached readiness (the "Target session" face, 1.03:1) is templated on token grounds: 120/120.
---

# Proof Pack — Terminal hosts, the sixth report (X-2)

Every row names what was observed, red first, then green. Nothing here is a claim.

## Red → green

| # | Oracle | Red (observed) | Green (observed) |
|---|---|---|---|
| 1 | `ConPtyChildStandardHandlesTests.AChildsStdoutIsThePseudoConsoleNotTheHostsRedirectedPipe` (Core, `Platform=Windows`) | `Assert.DoesNotContain() Failure: Sub-string found … "AIDE-STDOUT-LEAK-7c40cbd6…"` — the child's `echo` on the host's pipe | Passed 28 ms: nothing on the pipe, the token on `Output` — in a `dotnet test` host |
| 2 | Session-render probe, run directly with `> file 2> err` (E2E of 1) | stdout: `ShellType;powershell` ×2; stderr: `#< CLIXML`; the restore line preceded on its line by `PS C:\…> ESC]133;B BEL` | exit 0; stdout 0 shell bytes; stderr 0 CLIXML; `^restore (22:33:53Z replay):` ×1; `terminal.start` ×2, `terminal.stop … killed` ×2 |
| 3 | `ProcessRunnerBoundsTheReadTests.AStrangerHoldingTheOutputPipeCannotHoldTheCaller` (Core, `Platform=Windows`) | `the call took 7.1 s against a 2 s bound: the read waited for the pipe's last holder, not the child` | Passed 2 s; exit 0; `… output pipe was held open past 00:00:02 by another process` on the result |
| 4 | `SessionIdentityReportsTheRealWorktreeTests` (App) after `Git()` → `ProcessRunner` | (the instance: 30 min blocked in `ReadToEnd`, `dotnet-stack` stack in INV-0011 §2) | 4/4 passed, 182 ms |
| 5 | Core `Platform=Windows` half after the flag | — | 155/155, 47 s |
| 6 | App suite, conductor tree at `b4e61022` + X-2 | 782/783: `ShellContrastCensusTests.EveryTextPairingInTheComposedShellClearsItsFloor` — pair 34, `TextBlock "terminal-1"` in *Target session*, `#E4E9EF` on `#ECECEC` (no token), **1.03:1** | 783/783 with `ChromeComboBoxTemplate` (face, toggle and popup on token grounds): 120/120 pairings clear |
| 7 | Per-run terminal ledger (`%TEMP%\aide-tests\terminal-ledger-<pid>.log`, the App tests' default sink) | 64 `terminal.start` / **14** `terminal.stop` — 50 panes ended by nothing but the collector | 64 / **62** — `owner-closing` 48, `killed` 14; residual 2 (§Residuals) |
| 8 | `TerminalHostExitPathTests` × 2 launches, `node.exe higgsfield` births (`wt-attach-probe.ps1`) | 2 born with `WT_SESSION` set; 2 with it unset (`CREATE_NEW_CONSOLE` → a Windows Terminal tab → an agent attached) | 21 helper-launching tests headless (`CREATE_NO_WINDOW`): 0 born; the whole `Platform=Windows` half, 166/166: 0 born |
| 9 | `tools/verify-no-new-console-launches.py` | 2 findings on the pre-fix tree (`TerminalHostLauncher.cs:51`, `TerminalHost/Program.cs:50`); `--self-test` OK | OK — no `CREATE_NEW_CONSOLE` launch under `src/` or `tests/` |

Row 6 is DC-164's consequence made visible: before the flag, the test host's shells never
reached readiness (their OSC 133 bytes went into the host's pipe), so *Target session* never held
an item and the census never measured that face. The defect was in the App all along whenever a
terminal was ready.

## Measurements behind the report (INV-0011 §1–2)

- 18:52Z census: `conhost` 304 / `node` 257 / `powershell` 34; born in the last hour: `conhost`
  36, `powershell` 32, all children of `testhost.exe` 56892 (CV-1's App run, started 18:24:35Z).
- Host CPU 0.55 s over 8 s, 87 threads, 30 min into a 2-min suite; stack in `WorkbenchShell.Git →
  ReadToEnd`; 31 `ConPtyTerminalSession.ReadLoop` threads.
- Host ended 18:53Z: 6 s later, `powershell` born-in-the-hour 1 (a bash of ours), `conhost` 3.
- Same suite re-run under `hang-monitor.ps1` (5-s samples): 793/793 in 2 min; PowerShell children
  of the host peaked at 3.
- `reap-stragglers.py` dry run: `foreign` 539 (513 under `wta.exe[14152] → copilot.exe[27168]`),
  unchanged from INV-0010's pool; `unknown` 23; nothing of ours.

## Residuals

- Ledger: 2 of 64 starts without a stop (`terminal-1` at 19:13:29.981 — a test composing two
  shells in one directory; `s-terminal` at 19:13:30.348). Not chased in this slice; the ledger
  names them for whoever does.
- The 32 births inside the hung host are unattributed (INV-0011 §6); the ledger now exists so the
  next instance is not.
- The 513-member pool was ended on the operator's word (`Stop-Process -Id 27168`, 19:52Z: `conhost` 270 → 14, `node` 257 → 1) and its regrowth path closed (rows 8–9); Windows Terminal re-attaches to the operator's own live tabs only.
