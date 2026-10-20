# Brief — T34 `wl-deadholder-repro` (DC-NEW-wl-leader3-b, the named instance) · Claude Code · Sonnet · node 1 · READ-ONLY

**Goal:** reproduce `NoDispatchOutsideTheHostTests.ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds`
(`tests/AiDe.App.Tests/Workbench/Records/NoDispatchOutsideTheHostTests.cs:236`) failing under full-suite load. If it
fails, capture **where it waits** at its 30 s timeout. **Report the observed state, not a cause. No fix** (Owner,
2026-10-20). A fix, if one is due, is a later node the Owner rules on.

**What is known (read, do not re-derive):** it failed in the full App suite three times (T28 n3; the T32 n1 join's
first recount, "Assert.Equal() Failure: Strings differ"; the T32 n3 track suite, a 30 s timeout). Each time it passed
alone. T31 measured 0/10 in the full suite on `84538f7f`
(`docs/proof/workbench-loops-support/loadflake/rates.md`). The Owner's rule: a second event triggers the repro node.
This test has three. Read the test from `:236` down, and the code it drives, before running anything. Name the awaited
task, the holder process and the lease or file it polls, each by `file:line`.

**This node, in this order:**
- **(R0) Build once:** `dotnet build AiDe.sln > {PROOF}/build.txt 2>&1`. Every run below uses `--no-build`.
- **(R1) Reproduce under load.** Run the **full** App suite in a loop, at most N = 10 runs. Before each run, read the
  board as its own command (rule 19's command). Use the hang collector so that a hang leaves evidence:
  `dotnet test tests/AiDe.App.Tests --no-build --blame-hang --blame-hang-timeout 25s --blame-hang-dump-type mini --logger "trx;LogFileName=app-<k>.trx" --results-directory {TREE}/deadholder-runs > {TREE}/deadholder-runs/app-<k>.txt 2>&1`.
  Check first, on one short filtered run, that `--blame-hang` works here, and record what it writes. If it does not
  work, drop it and say so. Append one line per run to `{PROOF}/runs.txt`: `k  start  end  passed/failed  deadholder: pass|fail <first message line>`.
  **Stop at the first run where the test fails.** Otherwise stop at N = 10 and write `NOT REPRODUCED, N=10`.
- **(R2) On a failure, capture where it waits.** The three questions:
  - Which awaited task timed out?
  - What state was the holder process in?
  - Which lease or file was it polling, and what did that hold at the time?
  Sources, in this order:
  - the failure's message and stack from the `.trx` (read only the failing test's entry);
  - the blame sequence file and any hang dump (list them; if `dotnet-dump` is installed, `dotnet-dump analyze` for the
    managed stacks; it is not installed globally on this machine, so say if you could not read the dump);
  - the lease or holder files the test uses (path from the code), read right after the failing run.
  If none of these shows a value, a scratch copy of the test with file logging at each await is allowed. Save its
  source as `R2-capture-test.cs.txt`, **never commit it under `tests/`**, and run the scratch copy under the full
  suite again, up to the remaining N.
- **(R3) Report:** a `README.md` (V2 frontmatter) with the runs table, the failing run's observed state (or
  `NOT REPRODUCED, N=<n>`) and what was not captured. **No cause is claimed.** A belief goes in an `assume:` line, with
  what would confirm it and what breaks if it is false. Under DC-NEW-wl-leader3-b, add one line:
  `- **Repro (2026-10-20, T34):** <reproduced in run k | NOT REPRODUCED, N=n>, <one-line observed state>`. Status stays
  `partially-controlled`.

**No red-first and no extra whole-suite run** (rules 7 and 18 are met by R1's runs; no check is added and no code is
committed). Rule 22's ratchet still runs. The raw runs folder `{TREE}/deadholder-runs/` is never committed. Never
read a `.trx` or a dump file whole into your context.

**Tier** T2 · **fan-out** 0 · **budget** 100 tool calls · 150k. **Kick:** none by time. A kick mail means: finish the
run in flight, then R3 on what you have, labelled PARTIAL.
**Rules 19, 20, 21, 22 and 23 apply.** A `USER DEMO IN PROGRESS` post with no later `USER DEMO DONE` stops you after
the run in flight. Write and commit a result labelled PARTIAL, never extrapolated. Mail the Leader
`T34 PARTIAL k/10, held by USER DEMO at <time>`, end your turn with the same line, and resume on `USER DEMO DONE`.

## Owned authored paths
`docs/proof/workbench-loops-support/deadholder-repro/**`. The DC-NEW-wl-leader3-b entry's new Repro line in
`docs/lessons/defect-classes.md`. `{TREE}/deadholder-runs/**`, never committed. A scratch test under `tests/`, never
committed. **No src change, no committed test change.**
