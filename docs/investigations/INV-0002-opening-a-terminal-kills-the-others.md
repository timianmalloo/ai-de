---
id: inv-0002-terminal-rebuild-kills-sessions
title: "INV-0002 — Opening a second terminal kills the running one"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [investigation, terminal, workbench, adapter, render, rca]
links:
  - { to: architecture, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
  - { to: inv-0001-agent-terminal-environment, rel: relates-to }
review-by: 2026-12-13
summary: >-
  Opening a second terminal terminated the first one — the running Copilot session was lost. The
  verified cause is that WorkbenchAdapter.Render() rebuilds the entire AvalonDock layout on every
  mutation, constructing a brand-new TerminalSurface (and a fresh ConPTY child) for surfaces that
  did not change. Because each ConPTY child runs inside a kill-on-close job, disposing the replaced
  surface terminates the live process. Any layout mutation — not just opening a terminal — destroys
  every live terminal. Secondary: the only create command is "New agent terminal", which names the
  tab after the first agent on PATH ("claude"), so a plain terminal is mislabelled and no
  plain-terminal action exists. Report only — no code changed.
---

# INV-0002 — Opening a second terminal kills the running one

## Symptom, as reported

> "when I had just one terminal open things seemed to work properly then when I added another
> terminal the following happened: I lost the existing session that was already set up to run
> copilot; the new terminal window was titled *claude* but it wasn't a claude session just a
> terminal; new terminal sessions should be just called *terminal* and should not kill the prior
> terminal session; the action should not be 'new agent terminal' just 'new terminal'."

Three distinct faults are bundled here. Two are the same defect; one is separate.

| # | Reported fault | Nature |
|---|---|---|
| A | Opening a second terminal **killed the first** (running Copilot) | The primary defect — a lifecycle bug |
| B | The new tab is **titled "claude"** but is "just a terminal" | Naming — the create action defaults to an agent |
| C | The action is **"New agent terminal"**; there is no plain **"New terminal"** | Missing command |

## Verified root cause (fault A)

**`WorkbenchAdapter.Render()` rebuilds the whole layout on every mutation and reconstructs the
content of *every* pane from the factory — including panes that did not change — and each new
`TerminalSurface` starts a fresh ConPTY child in a kill-on-close job, so the replaced (old) surface
being disposed terminates the process the user was running.**

The evidence chain, read from the code:

1. **`WorkbenchAdapter.Render()`** (`src/AiDe.App/Workbench/WorkbenchAdapter.cs:50`) throws away the
   entire dock and rebuilds it:
   ```csharp
   var panel = BuildPanel(_service.Current.Root);
   Manager.Layout = new LayoutRoot { RootPanel = panel };   // whole tree replaced
   ```
2. **`BuildPane`** (`WorkbenchAdapter.cs:173-189`) invokes the factory for **every** surface in the
   model, with no reuse of the element already hosting that surface:
   ```csharp
   Content = _contentFactory?.Invoke(surface) ?? new ContentControl(),
   ```
   The class comment at `:163-167` confirms this is deliberate — there is **no content cache**; the
   code reads content back out of AvalonDock's own tree rather than keeping a surface→content map.
   The consequence was not intended: a mutation to *one* surface rebuilds *all* of them.
3. **`SurfaceContentFactory.Terminal`** (`src/AiDe.App/Workbench/SurfaceContentFactory.cs:129-133`)
   always constructs a new pane:
   ```csharp
   new TerminalSurface(surface.SurfaceId, surface.Title) { ... }
   ```
4. **`TerminalSurface`** (`src/AiDe.App/Workbench/TerminalSurface.cs:42`) starts a session in its
   constructor (`_ = StartAsync(...)` → `ConPtyTerminalSession.StartAsync`), and its `Dispose()`
   fires `_session?.DisposeAsync()`.
5. **`ConPtyTerminalSession.StartAsync`** (`src/AiDe.Core/Terminal/ConPtyTerminalSession.cs`) starts
   the child **inside a kill-on-close job** (`CreateKillOnCloseJob()`); disposal calls
   `CloseHandle(job)`, which **terminates the process tree**.

So when `NewAgentTerminalRequested` (`src/AiDe.App/Workbench/WorkbenchShell.cs:80-84`) adds the new
surface and then calls `Adapter.Render()`, the existing `terminal-1` surface is replaced by a fresh
`TerminalSurface`; the old one's disposal closes its job handle and **kills the running Copilot
child**. The pane the user was working in now shows a brand-new shell.

**Necessary.** If `Render()` reused the existing content element for a surface whose `ContentId` is
unchanged (reconcile by key, which AvalonDock's `ContentId` exists to support), the live session is
never replaced and the process is never killed.

**Sufficient.** Any code path that calls `Adapter.Render()` while a live terminal exists reproduces
the kill. `NewAgentTerminalRequested` is one such path (`WorkbenchShell.cs:84`), which is why
opening a second terminal triggers it.

### Generalization (this is bigger than "a second terminal")

`Adapter.Render()` is called from **five** sites:

| Site | Trigger |
|---|---|
| `WorkbenchShell.cs:84` | Open agent terminal |
| `WorkbenchShell.cs:132` | Layout mutation |
| `WorkbenchShell.cs:385` | Layout mutation |
| `MainWindow.xaml.cs:136` | Window / workspace layout change |
| `MainWindow.xaml.cs:154` | Window / workspace layout change |

Every one of them rebuilds every pane. **Any layout mutation — opening a pane, closing a pane,
splitting, restoring a workspace — destroys the state of every live terminal**, not only the act of
opening a second terminal. The user hit the most obvious instance; the defect is systemic.

## Verified cause (faults B and C)

There is **no plain "New terminal" command**. The only terminal-create command is
`terminal.newAgent` — **"New agent terminal…"** (`src/AiDe.Core/Workbench/WorkbenchCommands.cs`).
Its handler (`WorkbenchShell.cs:64-93`) picks the first agent CLI found on PATH —
`TerminalSurface.AvailableAgents.FirstOrDefault()` (`:66`) — and names the surface after it:

```csharp
var id = $"agent:{agent}#{Guid.NewGuid()...}";
Service.Apply(new LayoutOperation.AddSurface(terminalStack.Id, new Surface(id, "terminal", agent)));
```

So the tab is titled with whatever agent sorts first on the machine ("claude"), even when the user
wanted a plain shell. There is no way to ask for "just a terminal", and the default create action is
agent-flavoured. (This is why the tab reads "claude" but behaves like an ordinary terminal.)

## Harvested markers

The `simplify:`/`assume:` markers in the terminal and adapter subsystem were reviewed
(`TerminalSurface.cs:75`, `ConPtyTerminalSession.cs:78`, `OscParser.cs:98`,
`TerminalScreen.cs:94/275`). None predicted this defect — the rebuild-vs-reconcile choice in the
adapter carries no marker. That absence is itself a finding: the "no content cache" decision
(`WorkbenchAdapter.cs:163-167`) reasoned only about staleness of a parallel map, not about the
lifecycle of stateful content, and should have carried a marker naming the risk it accepted.

## Failure class

Registered as **DC-029** in `docs/lessons/defect-classes.md`:

> **A full-tree re-render rebuilds every child from a factory on each mutation, discarding
> live/stateful child instances (a process, a session, a media handle, a socket) instead of
> reconciling by key — so a change to one child destroys the state of the others.** It is masked
> because the factory faithfully produces a *correct-looking* replacement; the loss is of *state*,
> not of *shape*, and a rebuilt terminal looks exactly like the one it replaced.

Sweep: the five `Render()` sites above are the siblings for the terminal case. Any future
stateful pane content (a running canvas/WebView2 session, a media view) inherits the same defect
until the reconcile fix lands — the canvas surface (`SurfaceContentFactory.cs:48`, returned
unwrapped as a windowed kind) is the next most likely victim.

## Phased repair plan (for human review — not yet implemented)

| Phase | Change | Owner | Notes |
|---|---|---|---|
| **1** | **Reconcile, don't rebuild.** `WorkbenchAdapter.Render()` reuses the existing content element for any surface whose `ContentId` is unchanged; constructs content only for genuinely new surfaces; disposes only removed ones. This is the necessary+sufficient fix for fault A and closes DC-029. | **Core** | AvalonDock keys by `ContentId` already; the factory becomes create-on-miss, not create-always. |
| **2** | **First-class "New terminal".** Add `terminal.new` — "New terminal" — opening a plain shell titled "Terminal" (never an agent name). Keep "New agent terminal" as a separate, explicitly-named action. A plain terminal must never default to the first agent on PATH. | **Core** (command catalog + shell handler) | Fixes faults B and C. |
| **3** | **Rename, tab colour, per-session colour scheme.** The customization surface. | **Design** | Specified in `docs/specs/terminal-sessions.md`; built via `/ui-design` → `/implement`. |

**Control to add with Phase 1 (observed failing today):** a test that opens a terminal, records
its session/process identity, applies a second, unrelated layout mutation, and asserts the first
session's identity is **unchanged** (its process was not killed and its `TerminalSurface` instance
was not replaced). On today's code this fails — the instance is replaced and the process
terminated.

## Stop

This is a report. Per `/investigate` discipline it ends here for human review; no code was changed.
The fix (Phases 1–2, Core-owned) and the customization surface (Phase 3, Design-owned) are
specified in `docs/specs/terminal-sessions.md`.
