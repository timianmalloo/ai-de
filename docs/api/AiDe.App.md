---
id: api-aide-app
title: "API: AiDe.App"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App: 2 types, 3 members, 20% carrying a summary doc comment.
---

# API: `AiDe.App`

**2 public types · 3 public members · 20% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `App`

*class* — `App.xaml.cs`

Interaction logic for App.xaml — and the one place an unhandled failure is recorded.

**Remarks.** **Why this class stopped being empty.** The shell crashed on "New Claude Code session"
and left no evidence anywhere: no Windows Error Reporting entry, no Application event-log record,
and nothing in the workbench log, which recorded only layout mutations. The user could report
only that the executable closed, and the investigation had to start from a screenshot of the
terminal.





**The three routes a .NET UI app can die by**, all wired, because catching only the
first would leave two silent paths and a false sense that crashes are now recorded:



**Dispatcher** — an exception on the UI thread, which is where a click
handler runs.
**AppDomain** — a background thread, which the dispatcher never
sees.
**UnobservedTaskException** — a discarded `Task` whose fault nobody
awaited; it arrives at finalization, long after the gesture.




**The process is still allowed to fail.** `e.Handled` stays false: surviving an
unhandled exception would leave the shell running in a state nothing designed for, and a tool
that keeps going after an invariant broke tells the user less than one that stops. This changes
what is KNOWN about a crash, not whether it happens.

| Member | Summary |
|---|---|
| `void OnStartup(StartupEventArgs e)` | **(gap)** |

## `MainWindow`

*class* — `MainWindow.xaml.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `MainWindow()` | **(gap)** |
| `void OnSourceInitialized(EventArgs e)` | **(gap)** |
