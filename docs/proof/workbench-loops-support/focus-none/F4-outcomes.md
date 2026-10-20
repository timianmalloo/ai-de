---
id: proof-wl-focus-none-outcomes
title: "Evidence: T40 focus-none - what each outcome will mean (stated before any run)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, measurement, focus, foreground]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  The meaning of each outcome of the T40 foreground sampler, committed before run 1. No cause is claimed.
---

# T40 F4 - the outcomes, stated before the data

Written and committed before the sampler starts and before run 1 (F0). Not edited after F2 starts.

## The hypothesis (Inferred)
"Focus (none)" is DC-NEW-wl-t33-input-state's foreground state: another window holds the foreground, seen through
WPF keyboard focus. This node tries to disconfirm it.

## What an event is
A test in a full App run whose failure message says focus is on "(none)". Its window is the test's start and end
time from the trx, plus 2 s on each side. The sampler rows inside that window are the evidence.

## The sampler's reach
One row every 500 ms. A foreground change shorter than the gap can fall between two rows. The sampling gap is 500 ms
nominal, plus scheduler jitter (the real gap is read from the timestamps and reported).
`GetGUIThreadInfo` reads `hwndActive` and `hwndFocus` of the foreground window's thread only. It says nothing about
the keyboard focus of a thread that does not hold the foreground.

## Outcomes
- **(a) Host held the foreground at every failure.** In every sampler row inside the test's own start..end span, the
  foreground process is the test host (`testhost`). The hypothesis is refuted for that event: focus was lost while
  the host held the foreground. Two sub-cases, separated by `hwndFocus`:
  - **(a1)** the host holds the foreground and `hwndFocus` is 0 (null) in the rows around the failure;
  - **(a2)** the host holds the foreground and `hwndFocus` is non-zero but names a window of the host that is not the
    test's (the class of `hwndFocus` and its owner decide this; the test's own window is the one the test opened).
- **(b) Another process held the foreground.** In at least one row inside the test's span, the foreground process is
  not the test host. That process is named (name, PID, window class) as the holder. No cause is claimed beyond the
  observation: that it held the foreground in a sampled row is not that it made the focus (none).
- **(c) Too coarse to tell.** The rows inside the span are fewer than 2, or the test's span is shorter than the
  sampling gap, or the rows alternate between the host and another process so the failure instant cannot be placed.
  The report says so and gives the real sampling gap.
- A mixed event (some rows host, some other) is (b) for the rows it shows; the report lists both holders.
- The NOT REPRODUCED outcome (A3): N = 10 shows 0 events. The report gives the foreground-holder distribution
  over all runs (each process's share of the samples, the test host's included) and stops. N is not extended.

## What does not count
- A sampler PID in the foreground-holder column at any row: the run is invalid (M-stop).
- The left-button column is evidence only. It is not a classifier.
- A foreground that is the host's but a different host window than the test's: that is (a), not (b). The sampler
  separates processes, not windows of one process; `hwndFocus`'s class is the only window-level evidence.
