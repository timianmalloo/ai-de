---
id: proof-workbench-loops-wl-1a-real-lane-stop
title: "WL-1a — Stop on the real claude-code lane: session/cancel honoured, Interrupted"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [proof, stop, real-lane, claude-code, acp, workbench-loops, wl-1a, t5]
links:
  - { to: adr-0041-execution-attempt-lifecycle, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: implements }
review-by: "2026-11-18"
summary: >-
  One recorded Stop on the real claude-code ACP lane (claude-agent-acp 0.75.1, Claude Max subscription),
  driven through AttemptHost and GovernedRunHost.RunAsync. The adapter honoured session/cancel: StopRequested
  was taken at high-water seq 86, the prompt's cancelled reply arrived at seq 143, and the terminal row is
  Interrupted. The design's Flagged risk "session/cancel honoured by the adapter" is now Verified for this version.
---

# Stop on the real claude-code lane

**Result: `Interrupted`.** The adapter honoured `session/cancel`. Verified by the recorded rows below.

| Field | Value (from the files here) |
|---|---|
| Run | `AttemptRealLaneStopTests.Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded`, 2026-10-19T06:15Z, 18 s, passed (`test-run.txt`) |
| Adapter | `@agentclientprotocol/claude-agent-acp` 0.75.1, installed by `npm ci` from `spikes/acp-subscription-lane/package-lock.json` into the session scratchpad; observed auth `kind: account`, plan Max |
| Prompt | "Write the numbers from 1 to 400 as English words, one per line. Do not use any tools." (model `sonnet`, read-only turn) |
| Stop | issued after the first `agent.msg` event; `StopRequested` detail `{"high_water_seq":86}` |
| Acknowledgment | run-log seq 143: `acp.result` for prompt id 3, `stopReason: cancelled` — above the mark, so `Interrupted` |
| Outcome rows | ingress 5 `Dispatched` → 6 `StopRequested` → 7 `Interrupted` (`outcome-rows.json`) |
| NFR-11 | 144 events, 121,228 bytes, duration 16.55 s from `Dispatched` to the terminal, usage reported as zeros by the adapter on the cancelled reply (`measures.json`) |

## Files

- `run-log.jsonl` — the attempt's run log, `runs/<dispatch_key>.jsonl`, as `RunLogStore` wrote it (redacted at
  ingest, `scanned_with: patterns v1`). **One transform before commit:** the operator's account `email` and
  `organization` values in the three `_auth/status_update` frames were replaced with `[scrubbed before commit]`
  (6 values, `scrub.py` in the session scratchpad). Original sha256 `cf507c5bc095a4a7cc673d9875d04695fcc139f64296e6305acdea5d6cfbabf8`,
  121,228 bytes — the byte count in `measures.json` is the original's.
- `outcome-rows.json` — the attempt receipt read back from the store after the terminal.
- `measures.json` — `IAttemptHost.MeasureAsync` for the attempt.
- `test-run.txt` — the `dotnet test` output of the run.

## Finding (for the Leader)

ADR-0042's v1 patterns redact credentials only. The adapter's `_auth/status_update` carries the account email and
organization in every run log. That is personal data at rest in `.aide/sessions/*/runs/`, unredacted. It is not a
credential, so this is outside T7's v1 pattern set, and it is recorded here rather than fixed (single-user local
tool posture). It belongs to the privacy lens (ADR-0042 owner).

## How to re-run

```
set AIDE_REAL_ADAPTER_ROOT=<a folder with node_modules/@agentclientprotocol/claude-agent-acp>
set AIDE_REAL_LANE_EVIDENCE=<an output folder>
dotnet test tests/AiDe.App.Tests --filter FullyQualifiedName~AttemptRealLaneStopTests
```

Without `AIDE_REAL_ADAPTER_ROOT` the test reports **skipped**, never passed.
