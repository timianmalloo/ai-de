---
id: proof-wl-focus-none-events
title: "Evidence: T40 events joined to the sampler"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, measurement, focus]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  Each focus (none) failure with the sampler rows inside its test span plus 2 s each side.
---

# T40 F3 - events

Rows: 7722. Gaps between rows: max 0.62 s, median 0.50 s. Sampler PID as foreground holder: 0 rows.

## Run 5 - `AiDe.App.Tests.Sessions.ConsoleSplitPlacementTests.CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen`

- message: focus after the close is on (none), not the landing
- test span: 22:43:08.485 to 22:43:08.847 (0.36 s)
- rows inside the span: 1; holders inside: {'chrome.exe': 1}

| utc | in span | fg proc | fg pid | fg class | hwndActive | hwndFocus | focus class | lbutton |
|---|---|---|---|---|---|---|---|---|
| 22:43:06.622 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:07.123 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:07.624 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:08.125 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:08.625 | y | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:09.126 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:09.627 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:10.128 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |
| 22:43:10.628 |  | chrome.exe | 19876 | Chrome_WidgetWin_1 | 11011152 | 11011152 | Chrome_WidgetWin_1 | 0x0000 |

