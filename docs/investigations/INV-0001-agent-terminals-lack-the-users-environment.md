---
id: inv-0001-agent-terminal-environment
title: "INV-0001 — Agent terminals lack the user's PATH and profile"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [investigation, terminal, environment, rca]
links:
  - { to: architecture, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-11-29
summary: >-
  A reported loss of the user's PATH and profile in agent terminals. The verified cause is not in
  AI-DE: the machine's PATH is 22,297 characters and cmd.exe silently drops a variable that large,
  so every .cmd shim starts with an empty PATH — inside and outside the product. What was ours is
  that the terminal rendered the broken environment as a healthy one.
---

# INV-0001 — Agent terminals lack the user's PATH and profile

## Symptom, as reported

> "the agent sessions are still not using my profile, they don't have my path or environment
> variables, for example ghcp and claude code are both installed so they should both work"

## Reproduction

Deterministic, in and out of the product.

| Probe | Result |
|---|---|
| A `.cmd` shim launched through AI-DE's ConPTY | `PATH=[]` — empty |
| The **same shim** from a plain PowerShell, no AI-DE involved | `PATH=[]` — empty |
| `powershell.exe` launched through AI-DE's ConPTY | `PATH = 22,297 chars`, System32 present, `claude` resolves |
| The shim again, with PATH trimmed to 1,799 chars | Full PATH arrives |

## Verified root cause

**The machine's PATH is 22,297 characters. `cmd.exe` does not carry a variable that large and drops
it entirely.** Every `.cmd`/`.bat` shim runs under cmd — which is what every npm-installed CLI is —
so those tools start with **no PATH** and cannot find node, git, or themselves.

- **Sufficient:** at 22,297 characters, a cmd child receives an empty PATH. Reproduced twice, once
  with no part of AI-DE in the picture.
- **Necessary:** trimmed to 1,799 characters, the same child receives the full PATH.

`claude` works because `C:\Users\malla\.local\bin\claude.exe` is a **real executable** — no cmd in
the path. `ghcp` does not exist at all under any profile; the GitHub Copilot CLI is `copilot`, which
has both a WinGet `.exe` and an npm `.cmd` shim. That asymmetry is the whole reported symptom.

### Where the 22,297 characters came from

The user PATH (21,528 of the 22,297) carries roughly 190 entries of the shape:

```
C:\Users\malla\AppData\Local\Temp\biohacker-nuget-<guid>\dotnet-home\.dotnet\tools
```

Another project's build tooling appends a unique temporary directory to the **persisted user PATH**
on each run and never removes it. Each is unique, so there is nothing to de-duplicate — 227 entries,
227 distinct.

## Causes ruled out, with evidence

| Hypothesis | Ruled out by |
|---|---|
| The profile is not loading | The shell terminal loads it; `-NoProfile` was removed earlier. The extra PATH entries a profile adds (`Git\usr\bin`, `Git\mingw64\bin`, `Users\malla\bin`) do not contain either agent |
| The agents are not on PATH | Both `claude.exe` and `copilot.exe` are in the **registry** PATH, at indexes 32 and 27 of 227 |
| PATH truncated by length before the agents | They sit at indexes 27 and 32 — nothing plausible truncates that early |
| `CREATE_UNICODE_ENVIRONMENT` with a null environment block | Removed the flag, rebuilt, re-measured: still empty |
| AI-DE corrupts the environment it passes | **PowerShell started from the same inherited block reads all 22,297 characters and resolves `claude`.** The block is intact |
| Duplicate PATH entries inflating it | 227 entries, 227 unique. De-duplication would save nothing |

## What was actually ours

Not the broken environment — **the silence about it.** The terminal opened, looked healthy, and the
user's tools were absent with nothing anywhere saying why. That is DC-025 (absence rendered as
success) wearing a different hat, and it is what made a machine-configuration problem look like a
product defect for two turns of work.

## Fixes

| # | Change | What it fixes | What it does **not** fix |
|---|---|---|---|
| 1 | `EnvironmentHealth.Inspect` + a once-per-shell announcement | The invisibility. States the size, the limit, and the largest repeated group so the user can see *which program* filled their PATH | Nothing about PATH itself — deliberately. It never edits the user's environment |
| 2 | `ShellIntegrationMode.PowerShellHostedAgent` — agents run **inside the login shell** | The profile half of the request: agents get the user's aliases, functions and variables, and PATHEXT resolution so `.ps1` shims work. An agent terminal now behaves like typing the name in their own terminal | **The cmd limit.** A `.cmd` shim invoked from the hosting shell still starts cmd, which still drops the oversized PATH. Measured, not assumed |

Fix 2 deliberately does **not** claim to solve the reported symptom. It was built while the cause was
still believed to be the profile, it was measured afterwards, and it is kept because it delivers what
the user literally asked for — but the honest statement is that it does not restore PATH.

The remedy for the symptom is the user's to apply, and it is one line:

```powershell
# Inspect first — this prints what would be removed.
[Environment]::GetEnvironmentVariable('Path','User') -split ';' |
  Where-Object { $_ -match 'Temp\\.*nuget-.*dotnet-home' }
```

Removing those ~190 entries takes the user PATH from 21,528 characters to roughly 3,000, and every
`.cmd` shim works again — everywhere, not only in AI-DE.

## Regression tests

`LackingWorkspaceTests.AnOversizedPathIsReportedWithItsSizeAndItsCause` and `AHealthyPathSaysNothing`
— both directions, because a warning that fires on every machine is noise and one that never fires is
decoration. Observed failing before `EnvironmentHealth` existed (the type did not compile).

## Generalisation

**Failure class:** *the environment a tool hands its children is not the environment they receive,
and nothing checks.* A parent verifies its own state, passes it on, and assumes arrival. The child's
loss is silent, attributed to the tool that launched it, and invisible to every test — because tests
run in a small, clean environment where nothing is near a limit.

**Siblings swept:**

| Candidate | Verdict |
|---|---|
| `ConPtyInterop.StartAttachedProcess` — inherits with a null block | **Correct.** Verified by PowerShell reading the full block |
| `TerminalSurface.IsOnPath` — reads the app's own PATH to filter agents | **Confirmed related.** It answers "is it on *my* PATH", which is not the question — the child's PATH is what matters. Not fixed here; it is now covered by the announcement |
| `DispatchProbe` / `ObserveProbe` — launch with `CREATE_NEW_CONSOLE` | Ruled out. Same inheritance, and the probes measured correctly throughout |
| The daemon's child processes | Ruled out. It starts no cmd children |

**Registered as DC-027.**

## Residual risk

The exact cmd cut-off was **not bisected** — 22,297 fails, 1,799 works, and the constant used is the
documented 8,191. The message says "may be dropped" rather than asserting a threshold nobody
measured. And the check looks only at PATH: any other oversized variable fails the same way and is
not inspected.

## What would change this diagnosis

A machine with a short PATH still losing tools in an agent terminal. That would move the cause back
inside the product, and the first thing to re-measure is whether `IsOnPath` and the child disagree.
