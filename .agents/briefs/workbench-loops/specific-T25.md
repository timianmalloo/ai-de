# Brief — T25 `wl1b-perf` (WL-1b Track C, check C1) · Claude Code · Sonnet · node 1 (n1 + n2 together)

**Goal:** turn WL-1b's Inferred performance claims into measured ones. The design is
`docs/design/workbench-loops-wl-1b.md`. Read **"The bench" (:283-307)**, row **T25** (:495), check **C1** (:464), risk
**R3** (:526), the budget row (:540) and the Test Architect's condition (:576-579) before writing code. T24 is
joined, so the design's n1 and n2 run as one node. WL-1b's acceptance waits on this: the user walks the WL-1b demo
script only after T25 (Owner, 2026-10-20).

**This node, in this order (three checks, rule 16):**
- **(C1a) The plan oracle, red first.** In a new `wl1b` argument branch (`dotnet run --project bench/AiDe.Bench -c Release -- wl1b`),
  in the shape of the `p2` branch (`Program.cs:24-35`), build the populations the design names (1×, 100× breadth,
  100× depth) through the **product's own writers** (open them and cite `file:line`; never insert rows by raw SQL into
  a product table). Run EXPLAIN QUERY PLAN the way the P1-PERF-04 oracle does (`Program.cs:264-318`; its full-scan
  test is at `:298-308`). It is inline top-level code: leave P1's path unchanged, write the WL-1b oracle in
  `Wl1bPerf.cs`, and cite the P1 lines it mirrors. First one deliberately unindexed control query (`WHERE text = …` on `checklist_obligation_fact`). It must print
  `**FULL SCAN**` and make the run exit non-zero: save that run as `C1a-red.txt`. Then every WL-1b read (each reader
  the fold and the checklist service use, by `file:line`) runs green: no full scan of `evidence_link_fact`,
  `disposition_fact`, `checklist_obligation_fact` or `dispatch_attempt_fact`. A full scan on a product read is a
  **finding** for the Leader. Do not add an index (no src change in this node); report it.
- **(C1b) Append overhead (reported, not gated).** Append p50/p95 for each WL-1b row kind, on the product store and
  on a **control store** built from the same DDL without the WL-1b triggers and the FK. Use `Measure.Run`
  (`Measurement.cs:49`; warm and cold apart). Print the overhead table (product − control).
- **(C1c) `checklist.evaluate` p95 at 100× depth, gated at 100 ms.** Above budget is `**FAIL**` and a non-zero exit,
  the P1 way (budgets and the `failures` list at `Program.cs:37-45`, e.g. `:115`). The budget is Inferred as a fit (R3); say so in the output header. A miss
  is the design's upgrade trigger (`:163-164`, the `simplify:` marker): **report it, do not build the cache.**
- Red for C1c: show the gate can fail by running it once with the budget set to 0.001 ms (`C1c-red.txt`), then
  restore 100 ms. Never commit the lowered budget.
- **Output:** `docs/proof/workbench-loops-wl-1b/perf/wl1b-perf-2026-10-20.txt`, from a **Release** build (the header
  refuses Debug, `Program.cs:54`). Keep P1's committed numbers untouched (`Program.cs:15-17`): your branch runs only
  behind `-- wl1b`.

**A quiet machine (Owner, 2026-10-20).** A perf number taken while the user drives the desktop is a **wrong** number,
not a partial one. Before **and after** each measured run, read the board as its own command (rule 19's command). If a
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE`, or a `join in flight` post with no `join done`, is open at
either read, **discard** that run's output. Never commit it or average it in. If the hold is open, commit what is
green (code, red files), mail the Leader `T25 HELD by USER DEMO at <time>, <what is measured so far>`, and end your
turn with the same line. You are resumed on `USER DEMO DONE` and re-measure from a clean run. Run no App suite while
you measure. Start no other process that loads the machine.

**Then** the whole Core suite (your code is under `bench/`, but it references Core), the bench's own build, rule 22's
ratchet, and rule 12. The App suite is not owed: no App code changes. Name that in the done mail.

The bench's scratch database may use P1's per-run GUID directory under the temp path (`Program.cs:42-44`). It is
unique per run, so nothing is shared. Every file you report or commit stays in your tree (rule 21).

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. **Kick** at 30 min by mail (the runs are long). At the
kick, finish the run in flight if it is clean, commit what is green, then run the Core suite and hand off.
**Rules 19, 20, 21, 22 and 23 apply.** If a design line disagrees with the code on main, the code wins (rule 14):
report it.

## Owned authored paths
`bench/AiDe.Bench/Wl1bPerf.cs` (new); `bench/AiDe.Bench/Program.cs` (the `wl1b` argument branch only);
`docs/proof/workbench-loops-wl-1b/perf/**`. **No src, test or DESIGN.md change.** An index, a reader change or a cache
is a finding for the Leader, never a fix in this node.
