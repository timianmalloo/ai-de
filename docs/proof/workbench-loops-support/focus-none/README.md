---
id: proof-wl-focus-none
title: "Evidence: T40 - who held the foreground when focus landed on (none), N=10"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, measurement, focus, foreground]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  A read-only 500 ms sampler through 10 full App runs. One focus (none) event, class (c). chrome.exe held the
  foreground in every sample, passing tests included. No cause is claimed.
---

# T40 - the foreground and the keyboard focus under a full App run

Read-only node. No src, test or DESIGN.md change. No input was injected: `sampler.py` calls only
`GetForegroundWindow`, `GetWindowThreadProcessId`, `GetClassNameW`, `GetGUIThreadInfo`, `GetAsyncKeyState`,
`OpenProcess` (query-limited) and `QueryFullProcessImageNameW`.

## Outcomes as stated before the data
`F4-outcomes.md`, committed at `b29645aa` before the sampler started. (a) the host held the foreground; (b) another
process did; (c) too coarse. Sub-cases a1 (`hwndFocus` null) and a2 (focus on a host window that is not the test's).

## Method
- One `dotnet build AiDe.sln` (`build.txt`: 0 warnings, 0 errors), then 10 runs of
  `dotnet test tests/AiDe.App.Tests --no-build` by `drive.py`. Before each run it read the board (no demo, no join in
  flight) and the left button (`mouse.txt`: 0 of 20 lines hold `0x8000`).
- `sampler.py` ran windowless (`pythonw`, PID 24632) from before run 1 to after run 10 and wrote one row every 500 ms
  to the untracked `focus-runs/sampler.csv`: 7722 rows, max gap 0.62 s, median 0.50 s.
- Non-perturbation: the sampler's own PID held the foreground in 0 of 7722 rows (checked on the first rows and over
  all rows by `analyze.py`).
- `analyze.py` joins the trx files to the sampler and writes `F3-events.md`, `distribution.md` and `rates.md`.

## Result
Runs (`runs.txt`): App 1279 total per run; 0 failed in 9 runs, 1 failed in run 5; 1275 passed and 4 skipped otherwise.
Suite result of the last run (10): Passed, 1275 passed, 4 skipped, 0 failed.

| Event | Run | Test | Class | Holder |
|---|---|---|---|---|
| 1 | 5 | `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen` ("focus after the close is on (none), not the landing") | (c) | chrome.exe PID 19876, class `Chrome_WidgetWin_1` |

Class (c) by the committed rule: the test ran 0.36 s (22:43:08.485 to 22:43:08.847), shorter than the sampling gap, and
one row fell inside it. The rows within 2 s each side (`F3-events.md`) all read the same.

## Distribution (`distribution.md`)
| Holder | Rows | Share |
|---|---|---|
| chrome.exe | 7722 | 100.0% |
| the test host | 0 | 0% |
| the sampler | 0 | 0% |

`hwndActive` and `hwndFocus` were the same chrome window (`11011152`) in all 7722 rows.

## Observations (no cause claimed)
- The foreground was another process's window for the whole of every run, including in the 9 runs where the test
  passed and the 1275 tests that passed in run 5. "Another window holds the foreground" is therefore not what
  separates the failing run from the passing ones in this sample. The hypothesis is not confirmed and not refuted by
  it; the committed outcomes cannot place the failure instant (c).
- `GetGUIThreadInfo` reads the foreground window's thread only. The test host's own windows were never the foreground,
  so this data says nothing about WPF keyboard focus inside the host.
- `CtrlDigit_...` and `CtrlEnter_...` ended NOT CHECKED in 10 of 10 runs (foreground not held; T37's skip). That agrees
  with the sampler: the host never held the foreground. Their NOT CHECKED counts are a measurement, not a failure.
- T36's second event, `TheFactoryLanding_...`, did not recur (0/10).

## Rates (`rates.md`)
Collapsing...: 1/10 failed (T36: 1/10, run 4). TheFactoryLanding: 0/10 (T36: 1/10, run 6). CtrlDigit and CtrlEnter:
0 failed, 10 NOT CHECKED each (T36: 10/10 failed).

## Not measured
- Which test windows the host opened and when; the sampler sees processes, not the host's windows, when the host is
  not the foreground.
- Anything inside a 500 ms gap. A 0.36 s test can fall wholly between two rows.
- Any cause. One event is not a rate. Only a foreground held by chrome.exe was sampled; a run with the host (or
  another process) in the foreground was not observed.
- Whether chrome.exe's holding is the desktop's normal state in this session or something that happened here.
