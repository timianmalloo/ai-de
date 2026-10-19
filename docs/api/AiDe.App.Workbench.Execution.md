---
id: api-aide-app-workbench-execution
title: "API: AiDe.App.Workbench.Execution"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench.Execution: 2 types, 5 members, 86% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Execution`

**2 public types · 5 public members · 86% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `ExecutionAtRevive`

*class* — `ExecutionAtRevive.cs`

Which execution a revived notebook opens (the WL-S design's §4): one, for the attempt that was live when the window
closed. The slot file records no attempts, so "was live" is read from the store: the host's startup sweep concluded
that attempt `DeliveryUnknown{host_restart}`, and the read-back returns it last, in dispatch order.

| Member | Summary |
|---|---|
| `AttemptReceipt? LiveAtClose(IReadOnlyList<AttemptReceipt> readBack)` | The LAST read-back receipt (dispatch order) when its terminal is `DeliveryUnknown{host_restart}`, else null. An earlier swept attempt is not opened: a later turn ran after it, so the window did not close on it. |

### `AttemptReceipt? LiveAtClose(IReadOnlyList<AttemptReceipt> readBack)`

The LAST read-back receipt (dispatch order) when its terminal is `DeliveryUnknown{host_restart}`, else null.
An earlier swept attempt is not opened: a later turn ran after it, so the window did not close on it.

- **`readBack`** — The notebook's read-back, in dispatch order (`ReadBack`).

## `ExecutionSurface`

*class* — `ExecutionSurface.cs`

One attempt's execution (the WL-S design's §4): identity `execution:<DispatchKey>`, caption
*Execution — <attempt>*, one per attempt, in the Bottom zone. The attempt is the host's record; this
surface only shows it.

| Member | Summary |
|---|---|
| `string Kind = "execution"` | The surface kind — `execution`; the row is `Kinds`'s. |
| `string SurfaceIdFor(string dispatchKey)` | The execution surface id for an attempt: `execution:<dispatchKey>`. |
| `string CaptionFor(string dispatchKey)` | The caption, which is also the UIA name (`Create` names every surface by its title): *Execution — <attempt>*. |
| `ExecutionSurface(string caption)` | **(gap)** |
