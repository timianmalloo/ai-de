# Brief — T31 `wl-loadflake` (DC-NEW-wl-leader3-b's measurement) · Claude Code · Sonnet · node 1

**Goal:** replace "it passed on a re-run" with a measured failure RATE. **DC-NEW-wl-leader3-b**
(`docs/lessons/defect-classes.md:8750`) records tests that fail inside the whole suite and pass alone. Its derive line
says a green re-run is not evidence of absence. Only a rate is. Instances so far:
`NoDispatchOutsideTheHostTests.ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds` (twice, most recently in the T32
join's first recount, 2026-10-20) and `TerminalHostInLifePathTests.ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives`.
This node **measures**. It changes no code and no test. A deterministic repro is a later node, scoped by the Owner from
your rates.

**You are the only App runner on this machine while you run (Owner).** The measurement is the full-suite load rate,
and a parallel App run would change it. Do not start any other process that opens windows.

**This node, in this order:**
- **(M0) Build once:** `dotnet build AiDe.sln > {PROOF}/build.txt 2>&1`, read its last 5 lines. Every run below uses
  `--no-build`, so each run measures the tests, not the compiler.
- **(M1) N = 10 full runs, numbered k = 1..10.** For each run, in this order:
  1. **Read the board as its own command** (rule 19's command). If the board's latest post on the demo reads
     `USER DEMO IN PROGRESS` with no later `USER DEMO DONE`, **start no new run**. Go to (M-hold).
     If a `join in flight` post has no matching `join done`, also go to (M-hold).
  2. `dotnet test tests/AiDe.Core.Tests --no-build --logger "trx;LogFileName=core-<k>.trx" --results-directory {TREE}/loadflake-runs > {TREE}/loadflake-runs/core-<k>.txt 2>&1`
  3. `dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-<k>.trx" --results-directory {TREE}/loadflake-runs > {TREE}/loadflake-runs/app-<k>.txt 2>&1`
  4. Append ONE line to `{PROOF}/runs.txt`: `k  start(UTC)  end(UTC)  core passed/failed/skipped  app passed/failed/skipped`,
     with times from `date -u +%H:%M:%SZ` read in that turn and counts from the summary line of each `.txt` (read only
     its last lines, never the whole output).
  Do **not** set `AIDE_REAL_ADAPTER_ROOT`, `AIDE_REAL_PROJECTION_ROOT` or any real-lane variable. Those tests stay NotExecuted.
  `{TREE}/loadflake-runs/` is never committed (raw `.trx` and logs). Never read a `.trx` file into your context.
- **(M2) Rates.** Write `{PROOF}/rates.py`, a stdlib script (a file, then a run; rule 6) that reads every
  `loadflake-runs/*.trx` and writes `{PROOF}/rates.md`: one row per test that failed in at least one run, with suite,
  failures/runs (for example `3/10`), the run numbers, and the first line of each distinct failure message.
  A test that never failed is not listed, but the header states the total test count per suite and N. Any
  `subprocess` or `open` call carries `encoding="utf-8", errors="replace"` (the `verify-subprocess-utf8` and
  `verify-portable-text-io` gates).
- **(M3) Alone.** For each test in `rates.md`, run it alone 10 times with
  `dotnet test <project> --no-build --filter "FullyQualifiedName~<method>"`, output to
  `{TREE}/loadflake-runs/alone-<method>-<i>.txt`, and record `alone failures/10` in `rates.md`. Put the loop in a
  file and run that (rule 6). Read the board before this step too (step 1's rule).
- **(M4) Classify each listed test** in `rates.md`, one word plus the evidence:
  `load-only` (fails in the suite, 0/10 alone) · `fails-alone` (fails alone too; for example the restored-row fold
  `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`, a known keyboard test owed to a desk
  run) · `intermittent` (fails sometimes alone). For each class instance named above that did **not** fail in any run,
  write `NOT REPRODUCED, N=10` in the README. Give no cause. A cause without a repro is a guess (NG1). Cite the test's
  `file:line` so the repro node can start from it.
- **(M5) Register:** under DC-NEW-wl-leader3-b, add one `- **Measured (2026-10-20, T31, N=<n>):**` line after the
  Control line. It carries each listed test's in-suite and alone rates and a pointer to `rates.md`. Leave **Status**
  `partially-controlled`; the repro node closes it.
- **(M6)** README (V2 frontmatter, rule 13): the method, N, the per-run table (a copy of `runs.txt`), the rates table,
  the classification, and what was not measured. Then rule 22's ratchet and rule 12. The done mail carries the rates
  table in one line per test.

**(M-hold) The user's demo stops you; it does not start you (Owner, 2026-10-20).** The run in flight finishes; start no
new one. Write `runs.txt` with the runs so far, write `rates.md` from the completed runs only, headed
**`PARTIAL - k of 10 runs, held by USER DEMO IN PROGRESS`**, and commit. Never extrapolate a rate to N. Then send
`coord mail send --to wl-leader5-20261020 --kind note --body "T31 PARTIAL k/10, held by USER DEMO at <time>"` and end
your turn with the same line as your final message. The Leader resumes you on `USER DEMO DONE`. You then re-read the
board and continue from run k+1. The partial header is replaced when N completes.

**No red-first in this node** (rule 7 does not apply: no check is added and no code changes; the proof is the
measurement). **No whole-suite run is owed beyond M1** (rule 18 is met by M1's runs: name the last run's counts as the
suite result in the done mail).

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. **Kick:** none by time. A Leader kick mail means:
finish the run in flight, then do M2 to M6 on the runs completed, labelled PARTIAL as in (M-hold).
**Rules 19, 20, 21 and 22 apply.** Rule 16's "never stop on your own wall-clock estimate" applies: the node ends at N=10,
at the hold, at a kick or at the cap.

**The Leader is `wl-leader5-20261020`, epoch 5.** Where the common rules below name any other Leader id or epoch, read
these.

## Owned authored paths
`docs/proof/workbench-loops-support/loadflake/**` (README, `runs.txt`, `rates.md`, `rates.py`, `build.txt`, `ratchet.txt`,
`handoff.md`); the DC-NEW-wl-leader3-b entry's new Measured line in `docs/lessons/defect-classes.md`;
`{TREE}/loadflake-runs/**` (never committed). **No src, test or DESIGN.md change.** A change there is a seam request.
