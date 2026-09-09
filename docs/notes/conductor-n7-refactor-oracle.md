---
id: note-conductor-n7-refactor-oracle
title: "Pre-declaration — N7's refactor task and its diff oracle, written before the run"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, n7, exit-evidence, oracle, phase-1]
links:
  - { to: plan-conductor-programme, rel: refines }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-09
summary: >-
  N7's floor point 1 requires a named refactor whose diff oracle is written and committed
  BEFORE the governed run, because "a real refactor" left undefined is satisfied by a
  one-line edit. This note is that pre-declaration: the task the governed claude-code lane
  is given, the lease it runs under, and the exact contents the resulting diff must have
  for the run to count as a pass.
---

# Pre-declaration — N7's refactor task and its diff oracle

**Written and committed before the governed run.** Nothing below may be edited after the lane
starts; a post-hoc oracle is not an oracle. If the run's diff differs from this, the finding is
recorded as a miss, never as a re-specification.

## The task, as the lane will receive it

> Three files under `src/AiDe.Core/AgentPlane/` each carry a private copy of the same JSON string
> reader. Remove the duplication: one internal helper, three deletions, every call site qualified.

The duplication is real and is this repository's own defect signature (DM7, "two definitions of one
quantity"), observed at:

| File | Line (pre-run) | Body |
| --- | --- | --- |
| `src/AiDe.Core/AgentPlane/AcpLaneClient.cs` | 33 | `node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null` |
| `src/AiDe.Core/AgentPlane/AcpRunEventMapper.cs` | 186 | *(byte-identical)* |
| `src/AiDe.Core/AgentPlane/LeaseAndSeams.cs` | 363 | *(byte-identical)* |

Seven call sites: `AcpLaneClient.cs:27,30` (three calls), `AcpRunEventMapper.cs:78,81`,
`LeaseAndSeams.cs:255,356`.

## The lease the lane runs under

`lease: { exclusive: [ "src/AiDe.Core/AgentPlane/**" ] }`

An edit anywhere else raises a seam (N6's control), and an open seam forces the episode to close
`Blocked`. This is deliberately a scope the task fits inside exactly: it makes the negative control
meaningful rather than decorative.

## The diff oracle — every clause is falsifiable

The run **passes** floor point 1 only if all seven hold:

1. **A new file `src/AiDe.Core/AgentPlane/AcpJson.cs`** exists, declaring exactly one type
   `AcpJson` in namespace `AiDe.Core.AgentPlane`, containing exactly one member:
   `internal static string? Text(JsonNode? node)`.
2. **Its body is the same expression**, unchanged in meaning:
   `node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null`.
3. **All three private copies are deleted** — the string
   `private static string? Text(JsonNode? node)` occurs **zero** times under `src/` afterwards.
4. **All seven call sites are qualified** `AcpJson.Text(`. A `using static` that leaves the call
   sites textually unchanged does **not** satisfy this clause; the point of the refactor is that
   the reader can see which helper is being called.
5. **`src/AiDe.Core/AgentPlane/GoalBlock.cs` is unchanged.** It carries a *different* method also
   named `Text` (`Text(List<GoalBlockError>, string, string?, string)`); folding it in would be a
   name collision mistaken for a duplication. This clause exists to catch exactly that.
6. **No file outside `src/AiDe.Core/AgentPlane/` changes**, and no test file changes: the refactor
   is private-to-internal and behaviour-preserving, so `tools/expected-test-counts.json` must not
   move because of it.
7. **The build is clean** under `TreatWarningsAsErrors=true` and the full suite is green at the
   **same** test count as before the run. A refactor that changes a count is not a refactor.

**Shape floor, so "small" cannot collapse to "trivial":** the diff must touch **4 files**
(1 added, 3 modified), delete **3** method declarations, and rewrite **7** call sites. A one-line
edit cannot satisfy clause 3 and clause 4 simultaneously.

## What is measured alongside it

- **Terminal hosting:** the count of `terminal.start` activities emitted by
  `aide.terminal.runtime` during the run. The oracle is `== 0`, and the counter is falsifiable —
  a test starts a real ConPTY session and observes it go to 1.
- **Latency:** per-event normalization latency, p50 and p95, with the host named. The 250 ms is an
  SLO evaluated here (`note-conductor-latency-slo-not-assertion`), never a CI assertion.
- **Scoring:** the closed episode's `ScoreSegment.IsComparable` must be `true`.
