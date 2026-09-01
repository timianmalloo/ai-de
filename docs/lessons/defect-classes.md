---
id: defect-classes
title: "Defect-class register"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [lessons, defect-classes, continuous-improvement]
links:
  - { to: architecture, rel: relates-to }
  - { to: design-phase-1-walking-skeleton, rel: relates-to }
review-by: 2026-11-24
summary: >-
  The project's register of defect classes — the recurring shapes of things that go wrong here, what
  each one survives, and the control that now fails when the shape recurs. Seeded from the ten-persona
  architecture review and the Phase-1 build.
---

# Defect-class register

*Governed by `continuous-improvement.md` (CI1–CI12). **One entry per class, not per bug.** A new
occurrence of an existing class appends to that class's Instances and triggers a control review — it
does not create a new entry. Read this at grounding (CI5) for the area you are working in.*

**How to use this file**
1. On any defect, correction, or falsified assumption, answer **class → sweep → derive → prevent** in writing (CI2).
2. Find the matching class below, or add one. Append the instance.
3. Climb the control ladder (CI6) and record the highest rung that actually holds: *make it impossible* > *automated control* > *always-loaded instruction* > *knowledge doc* > *register entry only*.
4. A control is not a control until it has been **observed failing** on the un-fixed code.
5. If the class would help any project — not just this one — raise it upstream via `/extendaibundle` (CI8).

**Status counts:** controlled 46 · partially-controlled 32 · uncontrolled 1
*(Not typed by hand — `python tools/verify-defect-register.py` fails when this line disagrees with the entries, and `--fix-counts` rewrites it.)*

**Recurrences since last review:** 4.
- **DC-008**, whose first control was scoped to one test project when the cause was not project-specific.
- **DC-001**, whose first control checked links between files and so could not see three classes cited by ID with no entry in this register.
- **DC-013**, which recurred the same day it was first caused, because the first occurrence was repaired without being registered at all.
- **DC-021**, which reached its *third* occurrence before it was registered at all: each repair was cheap enough to make asking why unnecessary.

*All three are CI4: a second occurrence means the control was wrong, not that someone was careless. In the first two the control had been written to fit the instances rather than the class; in the third there was no control at all, because the first occurrence was repaired and never registered — which is the failure this file exists to prevent.*

---

## Project classes

### DC-001 — A cited artifact is authored in a session and never actually committed
- **Signature:** something is referenced as authoritative — by a typed link, a change-log row, or an
  **identifier cited in prose** — and the thing it names is not in the repository. Often an ignore
  rule written for build output swallowing a docs path (`[Rr]elease/` ate `docs/release/`); sometimes
  a file that exists but an **entry inside it that was never written**.
- **Why it survives:** the authoring session sees the artifact and its own index entry, so every
  in-session check passes. Only a fresh clone — or a reviewer following the citation — discovers the
  hole. The identifier variant is quieter still: a reader who trusts the citation never follows it,
  and a citation carries the *appearance* of grounding whether or not it resolves.
- **Instances:** 2026-08-25 — `docs/release/ai-native-ide-release-plan.md` (Release Engineer, soft
  veto); the same rule would have hidden any future `docs/release/*`. 2026-08-25 — `spikes/` was
  ignored by the pack default, so three "Verified" contracts cited evidence that was not in the repo
  (Test Architect, hard veto). 2026-08-26 (**recurrence**) — DC-009, DC-010 and DC-011 were assigned,
  reasoned about, and cited as authoritative by four committed artifacts, with **no entry in this
  register**; `architecture.md` cited DC-010 as a controlled class that resolved to nothing, and this
  file's own header claimed twelve classes over nine. Found while assembling a status table, not by
  any gate.
- **Control:** `docs-graph.py validate` reports dangling links and index drift and is run at every
  skill close; `.gitignore` re-includes `docs/release/**` and no longer ignores `spikes/`, each with
  a comment naming this class. Observed failing 2026-08-25 (validate surfaced the dangling
  `release-plan-ai-native-ide` edge before the file was recovered).
  **Widened 2026-08-26 after the recurrence:** `tools/verify-defect-register.py`, run in CI, requires
  that every `DC-NNN` cited anywhere under `docs/` resolves to a real entry, that the ID sequence has
  no gaps, that every entry declares a known status, and that the header counts match the entries
  present. Observed failing on the un-fixed register: six findings, exit 1 — including that the
  header's `controlled` count was overstated by exactly the three missing entries.
- **Why the first control was too narrow (CI4):** `docs-graph.py validate` checks *typed links between
  files*. Both the original instances were missing files, so a file-granular control looked sufficient.
  The recurrence was a missing **entry within a committed file**, which no link traverses — the class
  was never really "a file is missing", it was "a citation resolves to nothing", and the control was
  written to the instances rather than to the class.
- **Status:** `controlled`

### DC-002 — A "Verified" label rests on evidence nobody can re-run
- **Signature:** a contract table cites a spike, a benchmark, or a run that exists only in a prior
  session's scrollback. The reader cannot inspect it, re-run it, or see what cases it covered.
- **Why it survives:** the claim is *true* — the spike really did run — so the author has no sense of
  overstating. Nothing in the loop distinguishes "was observed once" from "can be observed".
- **Instances:** 2026-08-25 — `spikes/sqlite-fact-store`, `spikes/mcp-server`,
  `spikes/conpty-foundation` cited as Verified with no committed artifact.
- **Control:** `spikes/README.md` states the policy (a Verified row must cite a committed spike with
  a captured `RESULT.md` and a one-command re-run line), and all three spikes are now committed and
  green. **Rung honestly reached: knowledge doc + convention** — there is no automated check that a
  Verified row resolves to a committed spike path. Adding one to `docs-graph.py` would raise it.
- **Status:** `partially-controlled`

### DC-003 — A durable-store invariant is enforced by a mechanism that does not cover every write path
- **Signature:** an append-only or uniqueness invariant is enforced by one mechanism (a trigger, a
  constraint, a code path) while another route reaches the same rows. The invariant reads as absolute
  in prose and is conditional in fact.
- **Why it survives:** the obvious attacks pass. A test that attempts `UPDATE` and `DELETE` goes green
  while `INSERT OR REPLACE` — which resolves its conflict with an internal delete that does *not* fire
  the `BEFORE DELETE` trigger under SQLite's default `recursive_triggers=0` — silently overwrites the
  row.
- **Instances:** 2026-08-25 — found by executing a probe against sqlite3 during the architecture
  review, not by reading the schema (Data & Persistence).
- **Control:** `StoreImmutabilityTests.InsertOrReplace_OnFactTable_CannotBypassTheDeleteTrigger`,
  plus `PRAGMA recursive_triggers=ON` on every writer connection, `query_only=1` on readers, and a
  writer API that exposes no REPLACE/UPSERT path. **Observed failing 2026-08-26** with the pragma
  removed.
- **Status:** `controlled`

### DC-004 — A side effect is recorded after it happens, so a crash makes it look like it never did
- **Signature:** an irreversible external action (a terminal write, an email, a payment) is performed
  and *then* its receipt is persisted. The window between them has no durable trace, so recovery
  cannot distinguish "never attempted" from "attempted, outcome unknown".
- **Why it survives:** the happy path and every ordinary failure path are correct. Only process death
  inside a narrow window exposes it, and the resulting duplicate looks like a client bug.
- **Instances:** 2026-08-25 — prompt dispatch recorded its receipt after the PTY write; a
  protocol-conformant retry would re-deliver a consequential prompt (Distributed Systems, hard veto).
- **Control:** ADR-0010's write-ahead two-phase receipt; `DispatchTests.Dispatch_CrashAfterWrite…`,
  `…CrashAfterAttemptBeforeWrite…` and `Retry_AfterUnknownDelivery_ReturnsTheReceiptAndNeverResends`.
  **Observed failing 2026-08-26** against the record-after-write shape (all three red).
- **Status:** `controlled`

### DC-005 — An egress control is bound to the transport instead of the destination
- **Signature:** the control answers "who connected" (a loopback bind, an allowlisted origin, a local
  socket) and is treated as if it answered "where do these bytes go next". A local process that
  forwards onward passes it untouched.
- **Why it survives:** it looks like a security control and satisfies the network-level threat model.
  The data-flow diagram stops at the local process boundary, so the onward hop is never drawn.
- **Instances:** 2026-08-25 — MCP read tools would have served workspace facts to an
  externally-processing agent that forwards them to its provider; the LINDDUN table had no MCP flow at
  all (Privacy, hard veto).
- **Control:** ADR-0011 binds tool authorization to the session's declared processing class;
  `McpRead_FromNonLocalSession_LeaksNoWorkspaceContent` (External + Unknown) and
  `McpWrite_FromNonLocalSession_IsDeniedOutright`. **Observed failing 2026-08-26** with transport-only
  authorization.
- **Status:** `controlled`

### DC-006 — A gate reports success over a corpus it never read
- **Signature:** a linter, scanner, or gate exits 0 with "no findings" because its file matcher found
  nothing it understands — not because the code is clean. The report is shaped exactly like a pass.
- **Why it survives:** exit code 0, a reassuring message, and a green CI step. Nothing asserts that
  the scan covered a non-empty corpus.
- **Instances:** 2026-08-26 — `ui-craft-gate.py` (wrapping Impeccable, a web-source detector) returned
  `[]` against the WPF `src/AiDe.App`; a deliberately seeded `#FF00FF` in `MainWindow.xaml` produced
  no finding.
- **Control:** `TokenDisciplineTests.ComponentMarkup_UsesTokensNotRawColourValues` (**observed failing
  2026-08-26** on the seeded hex) plus `TheScan_CoversANonEmptyCorpus`, which asserts the corpus is
  non-empty — the check the gate itself lacked. Covers raw-colour discipline only; the remaining craft
  rules on this surface stay review-enforced (recorded as residual risk in the Phase-1 Proof Pack).
- **Status:** `partially-controlled`

### DC-007 — A staleness signal is measured against the system's own last observation
- **Signature:** freshness/lag is computed from the last event the system itself processed. If the
  event source dies, the metric reads perfectly healthy while the data rots.
- **Why it survives:** it is correct whenever the pipeline works, which is almost always. The failure
  mode makes the metric *better*-looking, so no alert fires.
- **Instances:** 2026-08-25 — projection stale-age measured against the daemon's last known revision,
  so a dead file watcher would read as fresh indefinitely (SRE).
- **Control:** `FreshnessProber` compares the repository-observed revision to the indexed revision;
  `FreshnessProber_DetectsDriftAgainstTheRepositoryNotTheDaemon`. Observed failing by construction
  (the prober is the only path that reads the repository independently).
- **Status:** `controlled`

### DC-008 — Test-observable global state leaks between parallel test classes
- **Signature:** assertions that pass in isolation and fail — or **vanish** — in a full run. Caused by
  process-global state (`ActivityListener`, WPF `Dispatcher`s and windows, static caches) shared
  across concurrently executing classes.
- **Why it survives:** each test is individually correct, and the failure looks like a flake. The
  WPF variant is worse: the host process **crashes mid-run**, so the runner reports the passes it had
  already recorded and simply stops. `Passed! 27` with 21 tests never executed is indistinguishable
  from a clean run unless you know the expected count.
- **Instances:** 2026-08-26 — `TelemetryTests` captured spans emitted by `WalkingSkeletonTests`
  running concurrently. 2026-08-26 (**recurrence**) — `AiDe.App.Tests` crashed the test host once
  several classes each showed real WPF windows on STA threads at the same time; 27 of 54 tests ran.
  Found only because the count dropped from a number that had been seen before.
- **Control:** `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in **both** test
  projects, each carrying the reason inline. **The first control was too narrow** — it was applied to
  `AiDe.Core.Tests` alone when the cause was not project-specific, which is precisely why this
  recurred (CI4).
- **Residual risk:** still a mitigation rather than a detection. Nothing fails if a future test
  project reintroduces shared global state under parallelism. **The detection that would close this
  is an expected-test-count assertion in CI** — a run that executes fewer tests than last time should
  fail, because a crashed host currently reports success.
- **Status:** `controlled` (mitigated in both projects; the count-regression detection is the named
  gap)

### DC-009 — A measurement is believed because its value looks reasonable
- **Signature:** a probe, benchmark or check returns a number that is plausible for the thing being
  measured, so nothing prompts a second look — and it is measuring something else. The instrument is
  pointed at the wrong moment, the wrong object, or a **proxy** for the invariant rather than the
  invariant.
- **Why it survives:** the ordinary defence against a bad measurement is that the answer looks wrong.
  Here it does not. Every instance below produced a figure a reviewer would have accepted, and each
  was caught only by asking *"could this be looking at the wrong thing?"* — never by the number
  itself being implausible. Worse, a wrong measurement **launders** into a Verified claim, so it
  corrupts the evidence trail rather than merely being absent from it (NG7).
- **Instances:** 2026-08-26 — the DPI probe read the thread's awareness context **before WPF had
  initialised**, reporting `UNAWARE`; re-measured after `Window.Show()` it read `SYSTEM_AWARE` (the
  wrong *moment*). 2026-08-26 — the UIA probe reported "splitter not present" from a `ClassName`
  lookup miss; enumerating every `Thumb` found it (the wrong *object*). 2026-08-26 — the
  ganged-resize test **summed pane widths** and reported "1319px inside an 885px container", which
  double-counts vertically stacked panes; there was no overlap (a *proxy* for the invariant).
  2026-08-26 — the first P1-PERF benchmark shared one store across 30 samples, so it measured
  append-only growth rather than refresh. 2026-08-26 — a gate's exit code was read through a shell
  pipe (`python … | tail; echo $?`), which reports `tail`'s status; a working gate was nearly filed
  as broken.
- **Control:** ladder rung *always-loaded instruction* — `instrumentation-over-inference.md` IO1–IO12
  and `end-to-end-integrity.md` E13–E14 (an exit code is not a result; read the state back). Two
  mechanical controls exist where the class touched code:
  `NoTwoPanesOverlap_AndNonePaneCollapsesToNothing` asserts the geometric invariant (pairwise
  intersection) rather than the width-sum proxy, and the P1-PERF harness builds an independent store
  per sample with growth measured separately. Both were observed failing on the un-fixed code.
- **Residual risk:** this is the weakest-controlled class in the register, and honestly so. No gate
  can ask "is this instrument pointed at the right thing?" — the two controls above pin the two
  specific measurements that were wrong, not the class. The general defence is the standing question
  at the point of measurement, which is an instruction and therefore fallible.
- **Note on this ID:** DC-009 was an unused number. The class was evidenced three times over in
  `docs/reviews/spike-dpi-and-ganged-resize.md` and `docs/design/phase-1-perf-results.md` — which
  explicitly name it as one recurring shape — but never written down here. It was assigned during
  the 2026-08-26 register repair, which is itself an instance of DC-001.
- **Status:** `partially-controlled`

*Instance appended 2026-08-29 — **a timer that bundled two costs**. A stopwatch wrapped
`File.ReadAllText` and `ParseText` together and its output was labelled "parse". The number was
real, the label was wrong, and the conclusion drawn from it — "parsing is 97% of extraction, so cache
the trees" — was confident, plausible and pointed at the wrong half: timed apart, disk I/O is ~99% of
the read and parsing is 4–5ms. Caught only because a follow-up measurement produced a 40x speedup
with **zero cache hits**, which no correct model explained. The control is the same one this class
always wants: an instrument reports what it measured, so a timer around two operations must be named
for both or split.*

### DC-010 — A system degrades under its own accumulated history and nothing notices
- **Signature:** a design that never deletes — append-only facts, an event log, an audit trail — meets
  its performance budget on a fresh store and drifts out of it as history accrues. Every individual
  write is correct and cheap; the cost is in the *quantity retained*, so no operation is ever the
  culprit and no error is ever raised.
- **Why it survives:** benchmarks run against fresh fixtures, which is the one state where the
  problem is absent by construction. It appears only after real use, arrives gradually, and presents
  as "the tool feels slow lately" — the shape of problem people stop reporting and start working
  around.
- **Instances:** 2026-08-26 — P1-PERF measured refresh p95 at 192 ms on a fresh store, 567 ms after
  ten generations of the same scope and 785 ms after twenty, against a 500 ms budget. A morning's
  editing puts a workspace outside budget. The indexes and query plans were correct throughout; the
  cause was index maintenance over retained superseded generations.
- **Control:** `StoreCompactor` prunes superseded generations by **rebuild-and-swap**, never by
  deleting facts — the immutability triggers and the no-REPLACE rule are never suspended on the live
  store, so the invariant is not hollowed out to fix performance in one place. Detection is the part
  that closes the class: `WorkspaceCore.CheckCompactionNeeded` raises a `store.compaction_due` health
  incident naming the scope and its generation count, so a workspace that has quietly become slow
  **surfaces itself**. Measured 654.64 ms → 333.11 ms across a compaction, dropping 19 generations
  and 190,000 assertions for 97.6 MiB. Covered by `StoreCompactionTests`.
- **Residual risk:** the policy reports rather than auto-compacts, deliberately — compaction replaces
  the database file, so it belongs to a deliberate maintenance moment rather than a background timer
  that could fire mid-session. An operator who ignores the incident stays slow. Nothing is measured
  beyond 50,000 edges, so the ceiling has moved rather than gone.
- **Status:** `controlled`

### DC-011 — A refused operation says nothing, so refusal is indistinguishable from breakage
- **Signature:** a command is correctly declined — a locked layout, a minimum-size floor, a
  precondition unmet — and the system's response is *silence*. The control is working exactly as
  designed, and the user's only available reading is that it is broken.
- **Why it survives:** the refusal path is the branch tests assert least on, because the assertion is
  a negative: the operation did **not** happen, and "nothing happened" is trivially true of both the
  correct refusal and a dead keybinding. It is easy to miss for sighted mouse users, who still see
  the unchanged layout; it is total for a screen-reader user, for whom silence is the entire
  response.
- **Instances:** 2026-08-26 — workbench layout operations refused against a locked layout and against
  the minimum-size floor returned an unchanged tree and emitted no announcement, so `Ctrl`+`Shift`+`P`
  → *float* on a locked layout was indistinguishable from a broken command.
- **Control:** `LayoutResult` carries an `Announcement` on **every** outcome including refusal, and
  `WorkbenchAnnouncer` emits it through both the live region and the notification API; refusals
  announce their *reason* ("Layout is locked. Unlock to rearrange panes."), not merely their
  occurrence. Pinned by `WorkbenchCommandTests` and `WorkbenchControllerTests`.
- **Residual risk:** **not yet verified by a screen reader.** The automated tests prove the
  announcement is *produced*; NVDA Part D — the protocol step that would prove it is *heard* —
  remains un-run (`docs/reviews/nvda-workbench-session.md`), deferred by the owner as low priority.
  Until it runs, this class is controlled against the code and unverified against the product. A Part
  D failure means the control needs widening, not that the test was wrong.
- **Status:** `partially-controlled`

### DC-012 — A test runner reports success for a run that aborted
- **Signature:** the summary line says `Passed!` with a plausible number, and the run actually
  crashed partway. No failure is printed because nothing failed — execution simply stopped. Only
  comparing the *count* to a previous run reveals it.
- **Why it survives:** every signal a reviewer normally reads is green: exit status, the word
  "Passed", zero failures. The missing information is a **negative** — the tests that did not run —
  and negatives are invisible unless something asserts on them.
- **Instances:** 2026-08-26 — `dotnet test` printed `Passed! - Failed: 0, Passed: 27` while the host
  had crashed and 21 tests never executed. Caught only because 48 had been observed earlier in the
  session; with no prior number it would have shipped.
- **Control:** `tools/verify-test-run.py`, run in CI in place of a bare `dotnet test`
  (`.github/workflows/build.yml`). Per project it requires: a result file exists at all (a host that
  dies early writes none), the run reports itself `Completed`, no failures/errors/aborts/timeouts,
  and **the executed count meets a committed baseline** (`tools/expected-test-counts.json`). The
  count check is the one that catches this class, because an aborted run's counters are *internally
  consistent* — they simply describe fewer tests than exist.
  **Observed failing 2026-08-26** by reproducing the original crash (re-enabling test
  parallelisation): bare `dotnet test` printed *"aborted"* and then
  *"Passed! - Failed: 0, Passed: 27"*; the gate reported `**SHORTFALL** 27/54` and exited 1.
- **Note on the same shape recurring during the fix:** verifying the gate's exit code through a
  shell pipe (`python … | tail; echo $?`) reported 0 because `$?` carries `tail`'s status, not the
  script's. Re-measured without the pipe: 1. A control is only as trustworthy as the measurement
  that says it works — which is this class's own lesson, arriving one level up.
- **Status:** `controlled`

### DC-013 — A monotonically allocated id is handed out twice because two trees allocate independently
- **Signature:** an id is assigned by reading the highest one present and adding one. Two working
  trees each hold the same highest entry, so both hand the same id to the next writer. Neither
  notices, because within either tree the allocation is correct.
- **Why it survives:** it is correct in a single checkout, and this repo's own worktree discipline
  guarantees there is rarely a single checkout. The collision does not surface at the moment it is
  created; it surfaces later as a merge conflict — or, when the file is append-only and merges
  cleanly, as two unrelated records sharing an id where one silently wins every lookup. The
  append-only case is the dangerous one, because nothing fails.
- **Instances:** 2026-08-26 — `al-0012` allocated in two trees during the Phase-1b work; resolved by
  discarding one and re-logging. 2026-08-26 (**recurrence, same day**) — `al-0028` allocated to a
  logged prompt in the primary checkout and to the register-repair entry in a worktree; the merge
  refused. 2026-08-29 (**third occurrence, and the first between two AGENTS rather than two trees of
  one agent**) — the core and design sessions each allocated `al-0071`, to entries with nothing in
  common; the rebase reported it as a content conflict in an append-only file. Resolved the way the
  session contract prescribes for that file: union both sides, keep the id already published on
  `main`, re-issue the other as `al-0072`, regenerate the derived views. `verify-audit-log.py` is
  what confirmed the result. The first two were caused by running a log-writing script in the primary checkout while the
  session's real work lived in a worktree, which is the WT-discipline violation underneath the class.
  2026-08-30 (**fourth occurrence, and the first OUTSIDE the audit logs**) — both sessions allocated
  **`DC-032`** in `docs/lessons/defect-classes.md`, to "Reconciling reused instances makes a
  per-render binding accumulate handlers" and to "A reader recognises one spelling of a pattern and
  reports the rest as absent". The rebase merged the file **cleanly**, because the two entries are
  hundreds of lines apart and neither side touched the other's text — the dangerous shape this class
  warns about. Caught by `verify-defect-register.py`, which checks one-entry-per-class independently
  of the audit-id rule; the later entry was renumbered `DC-033`. **The control was too narrow, not
  ignored:** `verify-audit-log.py` was built for the two JSONL logs and this file is a third
  monotonic allocator that nobody had classified as one.

  2026-08-30/31 (**fifth, sixth and seventh occurrences, all in one day, and the reason the control
  changed shape again**) — `DC-054`, `DC-055` and `DC-059` were each allocated twice, once in the
  core session's worktree and once in the design session's. Every one of the six files involved was
  internally consistent, so `verify-defect-register.py` and `verify-id-allocators.py` both passed in
  **both** trees and went on passing until the branches met. Each was found by a human noticing, and
  resolved by renumbering to `DC-058` and `DC-060`. **The control was still looking at one tree.**
  Duplicate-within-a-file is the shape a collision has AFTER the merge, and by then the cost is
  already paid: the entry has been written, cited, and in two of these three cases cross-referenced
  from other documents.

  On the day the cross-branch check was built it immediately found an **eighth and ninth**: `DC-061`
  and `DC-062` are being allocated right now on two unmerged branches — `feature/agent-watcher-substrate`
  and `feature/app-facelift-and-graph-surfaces` — to four entirely different lessons. Neither has
  merged, so this is the first occurrence of the class caught **before** it cost anything.
- **Control:** `tools/verify-audit-log.py`, run in CI: no id may be claimed by more than one entry in
  `audit-log.jsonl` or `change-log.jsonl`. **Observed failing 2026-08-26** against a synthetic log
  carrying a planted duplicate — reported the id, the count and the fix, exit 1 — and green against
  the real logs (29 and 8 entries, 0 duplicates). The **defect register** is covered by
  `tools/verify-defect-register.py`, which enforces one entry per class id and caught the fourth
  occurrence. **The gap those two left is now closed** by `tools/verify-id-allocators.py`
  (2026-08-30), which asks the generalising question — *"what else in here is numbered by reading
  the highest and adding one?"* — as a check rather than as a note. It guards every declared family
  in one place (adding one is a line, not a script) and, more importantly, **reports any UNDECLARED
  sequence** it finds, so the next allocator is guarded on the day it is invented rather than on the
  day it collides. Observed failing on both shapes — a planted duplicate and a planted hole — before
  it was believed.

  It found two unguarded allocators on its first run: **`adr-` (16 architecture decisions, allocated
  by FILENAME in `docs/adr/`)** and, on inspection, `INV-` in `docs/investigations/` — which was
  below the detection threshold at two entries and was declared on sight anyway, because "too small
  to collide yet" is a statement with an expiry date.

  **Two things the first draft got wrong, kept here because they are the interesting part.** It
  reported eighteen *holes* in the audit log as failures — but a hole is the documented merge
  protocol working: a contested id is resolved by RE-ISSUING the loser, which leaves the number
  permanently unused. Verified with `git log -S`: none of the missing ids has ever existed in
  history, so nothing was lost. A control that flags the fix as the defect is how a control teaches
  people to ignore it, so contiguity is now opt-in per family — off for the append-only logs, on for
  the register and the file-allocated families where a hole means something was deleted. It also
  first read ADR ids out of `architecture.md`, which merely CITES them: **an allocator is where an id
  is created, never where it is mentioned**, and confusing the two makes every citation look like a
  duplicate allocation.

  **The cross-branch half (2026-08-31).** `verify-id-allocators.py` gained the
  check the first version could not do: for every ref, the ids it **adds** relative to its own merge
  base with the trunk. An id added by two refs with different content was allocated twice; an id
  added on a branch that the trunk already spends is already gone. The **merge base** is what keeps
  it honest — a stale branch that merely still CONTAINS an old id adds nothing, and a merged branch
  adds nothing at all, so neither is reported. The signature is what makes two claims
  distinguishable: the heading text for the register, the filename for the file-allocated families,
  `shortname` for the logs. Nobody names the same lesson twice.

  It runs in CI, which builds **every branch push** with full history, so both sides of a collision
  are visible as remote refs and the branch that introduces one is told on its own build. A
  collision between two OTHER branches is printed as a **note and does not fail** the third branch's
  build — failing it would make every session's gate red until somebody else fixed theirs, which is
  how a control becomes noise. Both halves of that scoping are in the self-test.

  **Observed failing 2026-08-31** on a throwaway repository reproducing the exact shape: two
  branches off one base, each adding `DC-002` for a different lesson, both files internally
  consistent. The cross-branch check fires; `check_family` — the old control — **passes on both**,
  which is the whole point of the addition.

  **Two ways the new check certified instead of checking, both caught by its own guard.** It read
  every ref through `subprocess(text=True)`, which decodes with the locale codec — cp1252 here,
  against UTF-8 files. Every read threw on a background thread, every comparison saw an empty
  string, and it printed **OK while reading nothing** (DC-016). The guard added in response — *the
  trunk must yield ids for every family it carries* — then caught a second one within the minute:
  the heading reader used `re.findall` with a pattern that captures the id, so it got `DC-054` where
  it wanted the whole heading, failed to re-match it, and read **zero** ids from a file holding
  sixty. Only `DC` is a heading family, so the other four readers kept working and an aggregate
  guard stayed quiet — which is why the guard is now **per family**.

  **Not adopted: electing a single allocator between sessions.** It was considered and rejected. The
  sessions work in separate worktrees on purpose, and an election needs a rendezvous they do not
  have — a session an hour into its work has not fetched, so "ask the allocator" is either stale or
  a blocking round trip through `main`. It would also make one session wait on another to record a
  lesson, which is a worse failure than a rename. The class is not "the wrong allocator won", it is
  "a shared sequence with two writers": the cheaper answer is the one already proven for the JSONL
  logs — union at merge time, re-issue the loser — plus detection wide enough that no family is
  missed.
- **Prevention, added 2026-08-29 after the third occurrence:** `audit-log.py` no longer allocates by
  "highest present, plus one" alone. **Every worktree of a repository shares one git common
  directory**, so a counter placed there is visible to all of them; an exclusive-create lock makes
  the read-modify-write atomic, and the file's own highest id remains the floor so the counter can
  only ever be caught up to reality, never fall behind it. Two sessions cannot be handed the same
  number whatever order they run in.
- **Observed working:** sixteen concurrent allocations issued from two different worktrees of this
  repository returned sixteen distinct ids (`al-0073`…`al-0088`). The previous allocator returns
  `al-0073` to all sixteen. Gaps are expected and harmless — an id is an identifier, not a count, and
  the gate checks uniqueness rather than contiguity.
- **Residual risk:** it is one repository's worth of prevention. Two separate CLONES do not share a
  git directory and would still collide, and a stale lock falls back to the old behaviour by design
  rather than blocking a log write. `verify-audit-log.py` remains the backstop for both, and this
  belongs upstream in the pack (`/extendaibundle`, CI8) rather than only here.
- **Status:** `controlled`

### DC-014 — A capability cannot be tested because the test host lacks something the product has
- **Signature:** a feature works when the application is run normally and fails identically in every
  test environment. The failure looks like a product defect, so the instinct is to change product
  code — and any change "fixes" nothing, because the variable was never in the code.
- **Why it survives:** the test is honest, the assertion is right, and the red is real. Nothing
  distinguishes "the product is broken" from "the harness cannot host this". Worse, the pressure is
  to weaken the assertion until it passes, which converts a true red into a permanent blind spot.
- **Instances:** 2026-08-26 — ConPTY attaches a child process to a pseudo console only when the
  launching process owns a **real console**. A `dotnet test` host never does; its stdio is
  redirected. `Output_DeliversTheChildProcessesOwnOutput` therefore failed in every test run while
  the identical code captured 90 bytes of child output under `dotnet run` from a terminal. Several
  hours went into the interop before the console was suspected.
- **Control:** run the case **out of process** in a host that has the capability —
  `tests/AiDe.Core.TerminalHost` launched with `CREATE_NEW_CONSOLE`, driving the real
  `ConPtyTerminalSession` and reporting by exit code, with a report file so a failure says *why*
  rather than just returning a number. Verified capturing 297 characters of child output, and it
  passes in the very sandbox where the in-process form cannot.
- **The diagnostic that would have saved the time:** when a test fails identically everywhere, ask
  *what does the test host lack that a real run has* before changing any code under test. The
  distinguishing measurement here was three lines — `GetConsoleWindow()`, the std handle file types,
  and `GetConsoleProcessList` — and it should have been the first thing run, not the last.
- **Instances:** 2026-08-27 — the same class, reached from the other side. Building the terminal
  renderer raised the question this entry's own wording appeared to settle: `AiDe.App` is a GUI
  application with **no console at all**, so if "the host must own a real console" were the rule,
  every terminal pane in the product would be permanently empty — and no test in the suite would
  have failed, because none of them ran in that configuration. **Two stand-ins gave two wrong
  answers before the real one:** a probe calling `FreeConsole()` to *simulate* a GUI host captured
  nothing (`FreeConsole` does not leave a process as one that never had a console); then a genuine
  WinExe probe *still* captured nothing when started by the test host, because with
  `UseShellExecute = false` the child inherits the runner's **redirected standard handles**.
  Shell-executed, the same binary captured 291 characters.
- **Correction to this entry (CI4):** the operative condition is **which standard handles the host
  was given**, not whether it owns a console. The original wording is a description of the two cases
  measured in 2026-08-26 rather than the rule behind them, and taken literally it argues against the
  product's actual architecture.
- **Control (extended):** `tests/AiDe.App.TerminalProbe` — a **WinExe** probe whose `OutputType`
  *is* the thing under test, started shell-executed by `TerminalGuiHostTests`, asserting a GUI host
  with no console receives child output. **Observed failing 2026-08-27** in both wrong
  configurations before it passed in the right one.
- **Residual risk:** detection is still human judgement; nothing fails when a new test is written
  in-process for a capability the host lacks. The general defence is the diagnostic above, plus its
  corollary from this instance: **a stand-in for a configuration is not evidence about that
  configuration** — if the answer decides whether a feature exists, reproduce the real thing.
- **Status:** `controlled`


### DC-015 — A success check coarser than the claim it is standing in for
- **Signature:** a verification passes, and it would also have passed had the specific thing it
  exists to prove never happened. The check is real, the green is real, and it is answering a
  broader question than the one being asked — "did *something* succeed" in place of "did *this*
  succeed".
- **Why it survives:** it is indistinguishable from a correct pass. There is no red to investigate
  and no anomaly to notice, because the only evidence of the gap is a *counterfactual* — what the
  check would have done if the work had been skipped — and nobody runs that by default. It is most
  likely exactly where the work is parameterised: the parameter is what varies, and the success
  signal usually is not.
- **Instances:**
  - 2026-08-27 — `OscRoundTripTests` passed in ~200 ms while never running the OSC probe. The `mode`
    argument silently never reached the out-of-process helper, so it ran the *other* probe, which
    also succeeds and also exits `0`. The exit code could not distinguish which of two probes had
    run. Caught only because 200 ms was implausibly fast for two PowerShell round trips — i.e. by
    a human noticing a duration, which is not a control.
  - 2026-08-27 — the scripted edit that was supposed to add that `mode` argument did not apply, and
    its own guard (`assert s != before`, over the whole file) passed because *other* replacements in
    the same script had applied. A file-level "something changed" assertion cannot see a specific
    substitution that silently matched nothing.
- **Control:** **assert on evidence the work produced, not on its status code.** The round-trip test
  now asserts the helper's report contains `activity after the forged claim: Busy` and
  `activity after the authenticated claim: Ready` — strings only the OSC probe can emit — so the
  test name and the work it did cannot come apart. **Observed failing 2026-08-27** on the un-fixed
  code: with the `mode` argument still not wired through, the exit-code assertions passed and the
  report assertions failed. For scripted edits the equivalent is per-replacement assertion: check
  each pattern is present *before* substituting, never a single file-level diff check.
- **Residual risk:** nothing mechanically detects a *new* coarse check. The general defence is the
  question — *would this still pass if the specific thing I am claiming had not happened?* — asked of
  any verification whose subject is parameterised, and of any status code shared by more than one
  code path.
- **Status:** `partially-controlled`

### DC-016 — A control that cannot fire in the environment that verifies it
- **Signature:** a guard, check or limit that reads as protection and whose failing branch is
  unreachable — either by construction, or in every environment where it is exercised. It passes
  review because the code is correct, and it passes testing because the condition it guards never
  occurs. Deleting it changes no test result, which is the diagnostic.
- **Why it survives:** every signal points the right way. The control is present, its logic is
  right, its intent is documented, and the suite is green. What is missing is a **negative** — proof
  that it can say no — and negatives are invisible unless something forces them. It hides
  particularly well behind an outer control that already prevents the case: the inner one is then
  correct, unreachable, and indistinguishable from load-bearing.
- **Instances:**
  **2026-08-27, all on the IPC transport:**
  - **Unreachable by construction.** A per-connection in-flight semaphore intended to refuse a
    command flood. The serve loop reads, answers, then reads again, so in-flight is one by
    construction and the refusal could never happen. Found only because a test written to *expect*
    the refusal deadlocked instead.
  - **Unreachable in the verifying environment.** The owner-SID check on each connection. The pipe's
    ACL admits only the current user, so every peer a single-user test can produce is already
    correct. Mutation proved it: the check was deleted outright and nothing failed.
  - **Present but inert.** `WorkspaceLock` guarding one daemon per workspace with a Windows mutex
    alone. A mutex is owned by a *thread* and is re-entrant, so a second acquisition inside one
    process succeeds — and ADR-0009 keeps an in-process daemon as a supported hosting mode, making
    that the case the lock most needed to cover.

  **2026-08-31, two in one function within a minute of each other.** The new cross-branch check in
  `verify-id-allocators.py` read every ref with the locale codec against UTF-8 files, threw on a
  background thread for each one, compared empty strings, and printed `OK — 11 other branch(es)
  compared` **having compared nothing**. The guard written in response (*the trunk must yield ids
  for every family it carries*) fired immediately on a second instance: the heading reader used
  `re.findall` with an id-capturing pattern, so it received the captured id rather than the whole
  line, failed to re-match it, and read zero of sixty entries. The first was invisible because it
  was green; the second was invisible because four other readers still worked — which is why the
  guard counts **per family** rather than in aggregate. Neither was found by review. Both were found
  by a check that asks whether the reader saw anything at all.
- **Control:** **mutation is the detector, and it must run on every control at this boundary** —
  disable each one and require a test to fail. Then, per outcome: *unreachable by construction* is
  **deleted**, not made reachable by adding machinery to justify it (the semaphore was removed and
  the real bound — serial service, capped frames, capped connections — documented instead);
  *unreachable in the environment* is made testable by **injecting what it compares against** rather
  than left as a comment (`IpcServer` now takes an expected owner SID, so a server told to expect a
  different owner must refuse the peer it gets); *present but inert* is a plain defect and is fixed.
  **Observed failing 2026-08-27:** all three were found this way, two of them only after a mutation
  run reported `*** SURVIVED ***`.
- **Residual risk:** mutation testing is invoked by hand. Nothing fails when a new control ships
  without one, and the register cannot see a guard nobody thought to disable. The general defence is
  the question asked of every control as it is written: **what would have to happen for this to say
  no, and can that happen here?**
- **Status:** `partially-controlled`

### DC-017 — Verified one layer below the one that actually fails
- **Signature:** the model is correct and thoroughly tested; the defect lives entirely in the
  untested code between it and what the user sees. Every test passes, the logic is right, and the
  screen is wrong. It appears most reliably at a boundary the suite treats as trivial: the
  imperative glue that copies a view model into a control, a serializer, a formatter — the layers
  nobody writes a test for because "there is no logic in it".
- **Why it survives:** coverage looks complete, because the *interesting* part is covered. The
  untested part has no branches and no obvious behaviour, which is exactly why nothing was written
  for it — and why a change in the layer beneath (here: synchronous becoming asynchronous) can
  invalidate it without any test noticing.
- **Instances:** 2026-08-27 — the evidence pane became asynchronous when the shell moved onto the
  daemon. `SurfaceContentFactory` kept binding `pane.Rows` and `pane.StatusMessage` at construction,
  before the load ran. `Rows` is replaced by the load and is not observable, so both panes sat on
  *"Loading evidence…"* permanently. The pane view model was correct and had a dedicated test class;
  459 tests passed. It was found by **running the application and looking at it**.
- **Control:** `SurfaceContentTests` — builds the surface through the real factory, pumps the
  dispatcher, and asserts on **what the control ends up showing**: that rows arrive, that the status
  text stops saying "Loading", and that an unreachable workspace says so instead. **Observed failing
  2026-08-27** by restoring the construction-time binding, which the suite then caught.
- **The diagnostic that would have saved the time:** when a layer changes from synchronous to
  asynchronous, every consumer that read a value *once* is now reading it too early. That is a
  mechanical consequence, not a subtle one — the question to ask of each consumer is *"what does
  this show before the answer arrives, and what makes it change?"*
- **Residual risk:** this control covers the evidence surface only. Nothing fails when a new surface
  is added with the same shape of glue, and the general defence remains E11/E12 — prove the rendered
  surface, not the model behind it — plus actually running the application.
- **Status:** `partially-controlled`

### DC-018 — A guard that watches by name, and a name that moved
- **Signature:** a control selects what it protects by a naming convention — a prefix, a suffix, a
  folder, an attribute — and something correct is added under a different name. The control keeps
  passing, because it is looking at a set the new thing is not in. Nothing is broken; something is
  simply unwatched, and the gap is silent by construction.
- **Why it survives:** the control is green and the new code is fine. There is no failure to
  investigate, and no reviewer of the new code has any reason to think about a convention enforced
  somewhere else entirely. It compounds quietly: the longer the gap exists, the more code accretes
  inside it, and the harder the eventual correction is to make safely.
- **Instances:** 2026-08-27 — `TelemetryTests` enforces the privacy floor ("no path, prompt or
  source text in a span attribute") over `ActivitySource`s whose name begins `aide.`. Every source
  added with the process split was named `AiDe.Core.Ipc` / `AiDe.Core.Terminal` /
  `AiDe.Core.Upgrade`. For four commits the IPC boundary, the terminal runtime and the upgrade
  coordinator emitted spans that **no privacy assertion could see** — including spans on the first
  cross-process trust boundary in the product.
- **Control:** the sources were renamed under `aide.`, and
  `PrivacyMarkerTests.EveryActivitySource_IsUnderTheAideNamespace` now fails when one is not — it
  scans the source text, so an emitter that no test exercises is still covered. Its own privacy
  listener subscribes to **every** source rather than a prefix, because a listener scoped to the
  convention cannot see the thing that broke it. **Observed failing 2026-08-27** by renaming a source
  back.
- **The second lesson, which cost more than the first:** that scan was itself vacuous on its first
  attempt. It matched `new ActivitySource("…")` while every declaration in the codebase is
  target-typed `ActivitySource X = new("…")`, so it scanned **zero** sources and passed. Mutation
  caught it. The control now asserts a **minimum match count** — a scan that finds nothing satisfies
  every assertion about what it found (**DC-015**).
- **Residual risk:** the same shape applies to any other name-selected control — test discovery by
  filename, `[SupportedOSPlatform]`, the docs-graph frontmatter sweep. The general defence is to ask
  of every convention-scoped control: *what is the set this actually looks at, and what would it take
  for something to be outside it?*
- **Status:** `partially-controlled`

### DC-019 — A trust boundary assumed safe because an adjacent control was proven
- **Signature:** a control is designed, measured and shown to work against one mechanism, and the
  boundary it sits on is thereafter treated as closed. A *different* mechanism crossing the same
  boundary is never probed, because the proven control is remembered as protecting the boundary
  rather than the one path it actually covers. The evidence is real; the generalisation is not.
- **Why it survives:** everything about it reads as diligence. There is a spike, a measurement, a
  named control and a test — and citing it feels like citing evidence. The gap is invisible precisely
  because the adjacent work was done *well*: a boundary with a proven control attracts less scrutiny
  than one with none, so the unprobed path ends up safer-looking than an untouched one would be.
- **Instances:** 2026-08-26/28 — S2 proved that repository-authored **analyzers and source
  generators** can be prevented from executing during extraction (strip `AnalyzerReferences`). The
  Phase-2 design carried that as the analyzer-execution mitigation and recorded MSBuild *tasks* as
  merely "unprobed". Spike D3 measured that path: loading a hostile project through
  `MSBuildWorkspace.OpenProjectAsync` executed **all four** repository-supplied vectors — `Exec`, a
  `RoslynCodeTaskFactory` inline task, a `UsingTask` assembly, and a design-time-target hook — with
  **zero** workspace diagnostics. Two of the four need nothing but the checked-in `.csproj`. The
  analyzer control was correct and never covered this.
- **Control:** the spike is committed and re-runnable
  (`dotnet run --project spikes/msbuild-task-execution`), and its **exit code is the assertion**:
  `1` when repository code executes. It carries a positive control and a non-vacuity guard, because
  the safe-looking answer is the one a broken probe produces — and it did, on the first run
  (**DC-009**, **DC-016**). It becomes a shipped test the moment Component 1 acquires a containment,
  so the containment is proven against the same fixture rather than argued.
- **The generalisation to apply elsewhere:** when a control is proven, write down *the path it
  covers*, not the boundary it sits on — then ask what else crosses that boundary. Every "we
  established that X cannot happen" in a design should name the mechanism X travels by, and every
  other mechanism is unprobed until it is probed.
- **Residual risk:** **live.** No containment for MSBuild task execution has been designed or tested;
  Component 1 is blocked on that decision. The same shape should be checked against the other proven
  controls in this repo — the MCP egress denial, the capability-revocation path and the fact-store
  immutability triggers each cover a named mechanism, not a boundary.
- **Status:** `partially-controlled`

### DC-020 — A domain refusal that was a local exception becomes a shared-process outage
- **Signature:** code that refuses by throwing is correct while its caller is on the same stack — the
  exception reaches the one caller who asked. The same code then moves behind a server that handles
  many callers, and the throw now escapes into a shared loop. **The refusal is still right; its blast
  radius is not.** One caller supplying an ordinary, expected-to-be-refused input takes the service
  down for everyone.
- **Why it survives:** nothing about the refusal looks wrong, because nothing about it *is* wrong.
  It has a stable error code, a clear message and probably a test — a test that passes, because it
  calls the method directly and asserts the throw. The defect lives entirely in the distance between
  the throw and the new boundary, which no single file shows.
- **Instances:** 2026-08-28 — `BoundaryDispatcher.Begin` throws `WorkspaceStoreException` on a stale
  epoch, which is the correct answer when the core was replaced under a caller. Behind the daemon it
  escaped `Handle` (which guards only decoding, deliberately), left `IpcServer`'s listen loop, and
  **would have killed the daemon for every shell attached to the workspace**. Found by
  `AStaleEpochIsRefusedByTheDaemon_AndRecordsNoAttempt`, which was written to assert that no attempt
  was recorded and instead brought the server down.
- **Control:** `WorkspaceOperations.Refusable` maps `WorkspaceStoreException` — the type that carries
  a stable denial code — onto `IpcResponse.Error`, and **only** that type. The distinction is the
  control: a projection that throws anything else is a defect in us and must still escape rather than
  be swallowed into a shrug. The test asserts both that the caller is refused and that the daemon
  survives to answer the next request.
- **The generalisation to apply elsewhere:** when moving code behind a boundary that serves more than
  one caller, enumerate **every way it can refuse** and ask what the refusal reaches now. A `throw`
  that used to unwind to one caller is a shared-fate event the moment a dispatch loop sits above it.
  The question is not "is this refusal correct" but "who else is standing behind it".
- **Residual risk:** only the dispatch operations are wrapped. The read projections do not currently
  throw domain refusals, but nothing yet **fails** if one is added that does — the control covers the
  operations that needed it rather than the shape.
- **Status:** `partially-controlled`

### DC-021 — A fixture restates what the product declares, so shipping a feature breaks unrelated tests
- **Signature:** a test needs "the set of things this release ships" — surfaces, kinds, commands,
  error codes — and writes the list out by hand. The list is correct on the day it is typed. The next
  release adds a member, and tests **about something else entirely** go red: the failure says
  *persistence is broken* when what actually happened is *a pane was added*. The signal points away
  from the change that caused it, so the cheapest reading is "fix the fixture", which restores green
  and leaves the next occurrence fully loaded.
- **Why it survives:** the hand-written list is not wrong when written and is never revisited, because
  nothing about it is suspicious. It also *passes* for every change that does not touch the set, which
  is most of them — so the interval between occurrences is long enough that each one reads as a
  one-off. And the repair is genuinely trivial, which is exactly what stops anyone asking why it
  happened a third time.
- **Instances:**
  - 2026-08 — `WorkbenchStoreTests` hardcoded the surface ids; broke twice as surfaces were added,
    and was changed to derive from `Layout.Default()` with a comment recording both.
  - 2026-08-28 — `LayoutUpgradeTests` held the same list in two more places and broke on the `joins`
    surface: `ALayoutAlreadyAtTheCurrentVersion_IsNotMigrated` failed with `AIDE-LAYOUT-PARTIAL-RESTORE`,
    a migration error for a change that had nothing to do with migration. **Third occurrence of the
    class, first time it was registered** — the earlier fix was scoped to the file where it hurt.
- **Control:** `tools/verify-fixture-derivation.py`, run in CI. It derives the product's vocabulary
  from the product — surface ids from `Layout.Default()`, kinds from `SurfaceContentFactory.KnownKinds`
  — and fails when three or more of those identifiers appear as literals inside one collection in a
  test. Three, because naming one or two specific things is what a test is *for*; three in a
  collection is someone enumerating a set. The escape hatch is a stated reason
  (`fixture-derivation: ok — <why>`), not a flag. Every layout fixture now derives from
  `Layout.Default()`, with the v1→v2 rename applied where a test needs post-migration ids.
- **Observed failing:** the gate found two live cases the hour it was written — the kinds set in
  `WorkbenchStoreTests` (added the previous day, by the same hand that registered this class) and a
  three-name literal in `Load_WithAMissingSurface_NamesItAndStillProducesAValidLayout`. It also fails
  closed: an empty derived vocabulary is an error, not a pass over everything.
- **The generalisation to apply elsewhere:** when a test needs "everything the product currently has",
  it must **ask the product**, never restate it. `SurfaceContentFactory.KnownKinds`,
  `WorkbenchCommandCatalog.All` and `LayoutOperation`'s nested types are already read by reflection in
  the conformance tests — this is the same rule applied to fixtures, which is where it kept being
  forgotten. The tell is a collection literal in a test that names product concepts.
- **Residual risk:** the gate reads C# with regexes, so it sees single-line collection literals and
  the two vocabularies it knows. A list spread over several lines, or one enumerating command ids,
  passes. That is deliberate — a cross-line matcher produced false positives on ordinary code, and a
  lint people switch off is worth less than a narrow one they keep. Rung reached: *automated control*
  for the shape it covers.
- **Status:** `partially-controlled`

### DC-022 — A predicate shared by two producers, consumed as if it had one meaning
- **Signature:** a fact store keyed by `(subject, predicate, object)` collects assertions from
  several extractors. Two of them independently pick the same natural-language predicate for
  different relations — `depends_on` means *"this Bicep resource declares dependsOn"* to one and
  *"this type references that type"* to the other. A consumer then joins on the **predicate alone**
  and attaches a **fixed basis string** naming the meaning it had in mind. Every fact from the other
  producer is now reported with a reason that is false about it.
- **Why it survives:** the basis is written once, next to the predicate name, and never has to agree
  with the evidence again — nothing in the code can disagree with a string literal. The unit test
  passes, because it supplies assertions from the producer the author was thinking of. And the defect
  **fails in the flattering direction**: a join producing nothing gets investigated on sight, while
  one producing the largest Verified count the product has ever shown looks like the feature working.
- **Instances:**
  - 2026-08-29 — `JoinProjection` joined every `depends_on` assertion as a resource
  dependency. On TheTerrace that was **7,426 edges reported Verified**, each carrying *"declared in
  the resource's dependsOn"*, in a repository containing no Bicep and no `dependsOn` at all. Found by
  `spikes/joins-on-a-real-repo` — the first time any projection had been run over a real codebase
  rather than a fixture. It had shipped the previous day.
  - 2026-08-29, same file, the join immediately below it — `hosted_on` matched the whole
    `Microsoft.Sql/*` family, so 64 tables joined to a server, a database AND a virtual-network rule:
    **192 edges, each claiming "the only literally-named SQL resource in this template"**, of which
    there were three. Found in the same run, after the first fix, by reading the numbers rather than
    the code.
- **Control:** the join qualifies on the **kind of thing**, not the predicate: the subject must carry
  a `resource_type` assertion. Two tests, both required — a code-origin `depends_on` is not joined,
  and a resource-origin one still is, because narrowing a join until it can no longer fire is not a
  fix (DC-016). Observed failing: before the fix the first test reported 1 edge, after it 0.
- **The generalisation to apply elsewhere:** **a predicate is a name, and names collide.** When
  consuming facts, qualify on evidence that identifies the producer's domain — the subject's type,
  its scope, its origin — never on the predicate string alone. And treat a **fixed basis string** as
  the smell: if the sentence explaining an edge cannot be wrong when the edge is wrong, it is
  decoration. The wider tell is any projection whose output has only ever been seen over fixtures.
- **Residual risk (now MEASURED, not assumed):** a predicate-by-extractor census over a real
  repository says `declared_in`, `has_type` and `discloses` are each emitted by **all three**
  extractors. `has_type` is consumed by predicate in three places and is safe **by accident** — its
  object values (`class`, `record`, `table`, `azure-parameter`) happen to partition cleanly by
  producer, and nothing enforces that partition. `declared_in` and `discloses` are shared but not
  used for joins. The spike prints the census on every run, so the next collision is visible before
  it is joined; there is still no gate.
- **Status:** `partially-controlled`

- **Instance, 2026-08-31 — two lists of "what is build output".** `CSharpScopeDiscovery.Skip` held `bin, obj, .git, node_modules`; `UnanalysedLanguages.Skip` held those plus `artifacts, dist, build, __pycache__, .venv, target, vendor`. Both answer the same question and only one had been kept current, so TypeScript discovery indexed `artifacts/s00/publish/wwwroot/_framework` — Blazor's published JavaScript — as source. MEASURED: 3 scopes of 67 on TheTerrace were build output, and their nodes could not be resolved back to a file at all, which is how they were noticed. `artifacts` is the .NET SDK's own output layout and belongs beside `bin` and `obj`; `publish` and `_framework` were added to the TypeScript set. **The generalisation:** when a second copy of a list appears, the question is not which is right but why there are two — a divergence found through a THIRD symptom is a divergence that has been wrong for a while.

### DC-023 — A gate keeps passing because it runs a stale build of the thing it tests
- **Recurrence, 2026-08-30 — the harness chose the stale build on purpose.** `ShellBootstrapTests` launches a real daemon, and picked its configuration as *"Release if a Release directory exists, else Debug"*. A single `dotnet publish -c Release`, run for something else entirely, created that directory — so Debug tests began launching a Release daemon built hours earlier. When the IPC protocol changed, three tests failed with `ipc.unsupported_version`, which is version negotiation working perfectly and the harness pointing at the wrong binary. **The existing control did not hold because it was written about build ORDER, and this was build SELECTION** — the daemon was built, and freshly; the harness went and found a different one.
- **What the fix had to get right on the second try.** The configuration now comes from the test assembly's own path, and the staleness check compares the `AiDe.Core.dll` beside the daemon with the one beside the tests — content, not timestamps. Timestamps were tried first and were wrong: a daemon that did not need rebuilding is OLDER than the tests and perfectly current, so every incremental build reported staleness. Observed failing against the stale Release build, with the diagnosis it now prints.
- **Signature:** a test drives a separate executable — an out-of-process probe, a CLI, a helper — and
  that executable is **not** a build-order dependency of the test project. It was built once, by
  somebody building the whole solution, and every run since has exercised that old binary. The gate
  is green. It is green about a version of the product that no longer exists, and the gap widens
  silently with every commit.
- **Why it survives:** nothing looks wrong. The test passes, quickly, and passing is the outcome
  everyone is looking for. The staleness is invisible from the test's own output: there is no
  "compiled at" line, and the probe's exit code says the same thing whether it is a day or a month
  old. It surfaces only when the binary is *absent* — a clean clone, or a clean followed by a build
  in a different configuration — and then it reads as broken tooling rather than as a question that
  had stopped being asked.
- **Instances:** 2026-08-29 — a full clean and Release rebuild left `P2FOCUS03` failing with *"the
  canvas probe was not built"*. `AiDe.App.CanvasProbe` was never a `ProjectReference` of
  `AiDe.App.Tests`, unlike the terminal probe and the daemon beside it, which carry comments
  explaining exactly why they are. Once built from current source the probe failed for real: the
  canvas page contained a **JavaScript syntax error** (`' join(s) across artifact types: ' '`) that
  broke the whole `<script>`, so **the Graph pane rendered nothing at all**. The C# compiler cannot
  see inside an embedded page, no unit test renders one, and the one control that could have caught
  it had been running a binary from before the error was introduced.
- **Control (two, because there were two failures):** the probe is a `ProjectReference` with
  `ReferenceOutputAssembly="false"`, so it is rebuilt whenever the tests are — and it already refuses
  to pass vacuously, which is what turned "green" into a precise diagnosis the moment it ran against
  current source. Separately, `tools/verify-embedded-scripts.py` parses every inline `<script>` this
  repository embeds in a C# string or an HTML template, with `node --check` where Node exists and a
  narrower lexical scan where it does not, naming which mode it ran in rather than degrading quietly.
- **Observed failing:** the syntax gate's *first* finding was its own false positive — a `<script src>`
  inside an HTML comment, reported as a dead script — which was fixed rather than tuned around. It was
  then verified against the real defect: reintroducing the stray quote produced
  `CanvasPage.cs: script starts at line 52 — SyntaxError: Invalid or unexpected token`. Thirteen
  script blocks are checked in under a second, twelve of them in the docs templates, which fail
  exactly as silently.
- **The generalisation to apply elsewhere:** **anything a test executes must be built by that test's
  project.** The tell is a test that launches a path under another project's `bin/`. And the second
  lesson is narrower and sharper: **an embedded page is unchecked code.** HTML and JavaScript inside
  a C# string get no compiler, no analyzer and no test — the only thing standing between a typo and a
  dead pane is a probe that actually renders it.
- **Residual risk:** the probes are covered by hand-written `ProjectReference` entries, and nothing
  **fails** when a fourth is added without one. The syntax gate proves a script PARSES, which is not
  the same as proving it works — only the canvas probe rendering the page does that, and only for the
  canvas.
- **Status:** `partially-controlled`

### DC-024 — A liveness check that reads a ledger instead of the world
- **Signature:** a destructive operation is gated on "is anybody using this?", and the gate answers
  from a **registration** — a session table, a lock file, a lease — rather than from the thing
  itself. Everything registered is protected. Everything *unregistered* is reported as idle, which
  is the same word the tool uses for genuinely abandoned, so the operator cannot tell "nobody is
  here" from "nobody signed in". The gate is correct about its ledger and wrong about the world.
- **Why it survives:** the ledger is right almost always, because most participants do register.
  The failure needs a participant that skipped registration AND a moment when every other signal is
  clean — for a worktree, a session sitting between a commit and its next edit. That window is
  narrow, so the tool is trusted for a long time before it is wrong once, destructively.
- **Instances:** 2026-08-29 — `coord worktree cleanup --remove` deleted
  `C:/Projects/ai-de-facelift`, reported *"clean, merged, unheld"*, and a live session **recreated
  the tree within the minute** and wrote a marker reading *"facelift worktree in use"*. Its
  cleanliness checks were all correct — the tree had no uncommitted work and no unique commits, so
  nothing was lost — but `unheld` came from `live_keys`, and that session had never run
  `coord session start`. Found by looking at the directory afterwards rather than by any alarm.
- **Control:** `worktree_safety` gains a filesystem condition after the git ones: a tree whose files
  were modified within `WORKTREE_IDLE_SECONDS` (3600) is **held**, whatever the ledger says. The scan
  skips build output, is capped at 4,000 files, and treats hitting the cap as *in use* — a partial
  scan cannot prove absence. The reason string now carries the age, so "idle" is a measurement the
  operator can disagree with rather than a verdict.
- **Observed failing:** a scratch worktree, clean and fully merged and unregistered, was reported
  `KEEP … touched recently - last modified 0 minute(s) ago`; with its files backdated two hours it
  became `WOULD … clean, merged, unheld, idle - last modified 120 minute(s) ago`. Both directions,
  because a safety rule that never permits anything is not a safety rule.
- **The generalisation to apply elsewhere:** before anything irreversible, ask **what would tell me
  this is in use, and does my check actually look at that?** A ledger is evidence that someone
  announced themselves, never that nobody is there. The tell is a gate whose reason string says
  "unheld", "unclaimed" or "no active session" — every one of those is a statement about a record.
  Where the world can be read directly, read it, and let the ledger be the fast path rather than the
  answer.
- **Residual risk:** an hour is a guess dressed as a constant. A session idle longer than that is
  still unprotected, and a tree on a filesystem with coarse timestamps reports ages that are only
  roughly right. The registration path remains the strong signal; this is the floor beneath it.
- **Status:** `partially-controlled`

### DC-025 — Absence rendered as success
- **Signature:** a projection computes over a set that is **empty because nothing was collected**,
  and the arithmetic is correct: zero uncovered symbols, zero omitted nodes, zero unread files. The
  surface then renders that zero with the vocabulary of completeness — *"every declared symbol
  belongs to a context"*, *"the panes see the whole workspace"*, an empty disclosure list. **Nothing
  here** and **nothing I could look at** produce identical output.
- **Why it survives:** every number is right, so no test asserting a number can fail. It cannot be
  fixed by counting more carefully, which is the first thing anyone tries. And it only appears on
  input the developer does not have: a workspace with no context map, a repository in a language the
  extractors do not read, a store larger than the read caps. Fixtures are built by the same person
  who built the feature, so fixtures always have the thing.
- **Instances:** all three found by pointing the panes at real repositories, none by a test.
  - 2026-08-29 — a workspace with no `bounded-contexts.yaml` reported `0 uncovered` and *"Every
    declared symbol belongs to a context"*, the sentence a fully-mapped codebase produces. Fixed with
    `ContextMapView.IsDeclared` — a separate field, because no cleverer count could carry it.
  - 2026-08-29 — `Find` borrowed the neighbour ceiling, so the panes read 50 nodes of 2,164 and
    reported crossings, joins and coverage from three percent of the workspace as the answer. Fixed
    with a search ceiling of its own, and `EvidenceRead.Shortfall` to say when a cap bit.
  - 2026-08-29 — a repository of 63 Python and 40 TypeScript files produced zero scopes, zero
    assertions and an **empty disclosure list**: indistinguishable from an empty directory, with the
    mechanism whose whole job is to report what went unread reporting nothing. Fixed with
    `UnanalysedLanguages.Survey`.
  - 2026-08-29 — a copy of a real repository with **one deliberate syntax error** indexed as
    `10 of 10 scopes, 0 failed`, produced fewer assertions, and disclosed nothing. Roslyn does not
    throw on broken source: it returns a tree with error nodes, so extraction SUCCEEDS and simply
    finds less — indistinguishable from a smaller file. This is the state a developer is in most
    often. Fixed with `ExtractionDisclosures.SourceDidNotParse`, which names the files and their
    count while still contributing what did parse.
- **Control:** each fix carries the same shape — **a field that distinguishes "none" from "not
  looked at"**, and a test for BOTH directions, because a disclosure that fires on every workspace is
  noise and one that never fires is decoration. `spikes/joins-on-a-real-repo` runs the panes over a
  named repository and prints what they see, which is how all three were found.
- **The generalisation to apply elsewhere:** whenever a surface is about to render a zero, ask
  **"could this zero mean I did not look?"** — and if it could, the answer belongs in the data as its
  own field rather than in the phrasing. The tell is a projection whose empty case shares a code path
  with its complete case. Related to DC-009 (a measurement believed because its value looks
  reasonable); this is its mirror — a measurement believed because its value looks *clean*.
- **Control:** `tests/AiDe.Core.Tests/LackingWorkspaceTests.cs` — a corpus of workspaces defined by
  what they LACK: empty, only-Python, source that will not parse, no context map, a read that was
  bounded, and a scope whose extraction failed so the graph shows an older revision. Every case
  asserts a **sentence**, never a count, because the counts were always right. The last case is the
  generalisation itself: a workspace missing something must not produce a result that is silent about
  it, and adding a new kind of absence to that list is how the next instance gets caught before a
  real repository finds it.
- **Why a corpus and not a rule:** fixtures always have the thing. That is the whole reason this
  class survived four times — a fixture is written by the person building the feature, so it contains
  a context map, compiles, and is in the language the extractor reads. The corpus is the deliberate
  opposite.
- **Residual risk:** the corpus covers the kinds of absence already met. Nothing fails for a kind
  nobody has thought of, and the sixth instance will still arrive from a real repository. Note also
  that the fourth
  instance was nearly reported from an experiment that had not run: the script meant to corrupt a
  file silently did nothing, and the assertion-count difference had another cause entirely. The
  finding only became real once the broken file was verified to exist.
- **Status:** `partially-controlled`

### DC-026 — A merge resolution that de-duplicates by the key in dispute
- **Signature:** two branches append to one file, the merge conflicts, and the resolution unions the
  two sides **keyed by an identifier**. Where both sides used the same identifier for different
  content — which is the whole reason the conflict is interesting — the union keeps whichever side it
  read first and **silently discards the other**. The result parses, validates, and is short by one
  entry that nobody will look for.
- **Why it survives:** it looks like the careful option. Keying by id is what de-duplication means,
  the gate afterwards passes (uniqueness is satisfied precisely BECAUSE one was dropped), and the
  loss is invisible unless someone remembers writing the entry that is gone. The resolution is also
  written fresh each time, under time pressure, at the end of a piece of work.
- **Instances:** 2026-08-29 — resolving an `audit-log.jsonl` conflict between the core and design
  sessions, a `setdefault`-keyed union dropped a design-session entry. It was noticed only because
  its author went looking, and re-emitted by hand as `al-0090`. `verify-audit-log.py` was green
  throughout: it checks that no id is claimed twice, and one had just been removed.
- **Control:** two, because one was not enough. `tools/merge-append-only-log.py` unions by **content**, so nothing can be dropped;
  an id claimed by two different entries is re-issued from the shared counter rather than resolved by
  discarding; upstream keeps the contested id because it is already published; and it prints the
  count in, the count out, and every re-issue, because a merge that resolves silently is
  indistinguishable from one that lost something.
- **The generalisation to apply elsewhere:** **never de-duplicate on the field that is in conflict.**
  If two records disagree about an id, the id is the least trustworthy thing about them. Union on
  content, then repair identity. And a "0 dropped" line in the output is worth more than a paragraph
  of care, because it is checkable.
- **And the gate that missed it now looks for the loss:** `verify-audit-log.py` compares each log
  against `HEAD` and fails when an id present in the committed version has disappeared. It only
  counted duplicates before, which is why it stayed green while an entry was being removed —
  uniqueness was satisfied *precisely by* the removal. **Observed failing** on a log with its last
  entry deleted: *"1 id(s) present in HEAD are missing here: al-0093 — an append-only log does not
  shrink."*
- **Residual risk:** the merge tool has to be reached for; nothing forces its use during a rebase.
  The gate is the backstop that does not have to be remembered, but it only sees losses against
  `HEAD`, so a loss introduced and committed in one step is invisible to it.
- **Status:** `partially-controlled`

### DC-027 — The environment a parent hands a child is not the one the child receives
- **Signature:** a process verifies its own state, passes it to a child by inheritance, and assumes
  arrival. Somewhere between them a limit applies — a variable too long, a block too large, a shell
  in the middle with a smaller cap than the parent — and the child starts **missing something it was
  given**. Nothing fails. The child simply cannot find its tools, and the blame lands on whatever
  launched it.
- **Why it survives:** every test runs in a small, clean environment where nothing is near a limit,
  so the loss is unreachable from a test suite. The parent's own checks all pass — it *does* have the
  variable — and the natural instrument (`Environment.GetEnvironmentVariable` in the parent) answers
  a different question from the one that matters. Worst of all it presents as the launching tool's
  defect, so investigation starts in the wrong codebase and stays there.
- **Instances:** 2026-08-29 — reported as "the agent sessions do not have my profile or my
  environment variables". The machine's PATH is **22,297 characters**; `cmd.exe` silently drops a
  variable that size, so every `.cmd` shim — which is every npm-installed CLI — started with an
  **empty PATH**. Proven not to be the product: the same shim from a plain PowerShell also received
  an empty PATH, and trimming to 1,799 characters made it arrive whole. Two turns of work went into
  the wrong codebase first, because the symptom is indistinguishable from a launcher bug.
- **Control:** `EnvironmentHealth.Inspect`, announced once per shell. It states the size, the limit
  and the **largest repeated group** of PATH entries — because 200 unique paths is a number, not a
  lead, and the entries that caused this are unique by construction (a GUID each) so nothing groups
  them by literal value. Two tests, both directions: a healthy PATH says nothing, because a warning
  that fires everywhere is noise. **It never edits the environment** — a tool that silently rewrites
  PATH to make itself work has hidden the problem from the only person who can fix it.
- **The generalisation to apply elsewhere:** when a child process misbehaves, **ask the child what it
  received** before theorising about what was sent. The parent's copy is not evidence. The cheap
  decisive probe is to run the same thing with no part of your product involved: if it fails there
  too, the investigation moves out of your codebase in one step instead of three.
- **Residual risk:** only PATH is inspected, and only against cmd's documented limit. Any other
  oversized variable fails identically and is unchecked; the exact cut-off was never bisected, so the
  message says "may be dropped" rather than asserting a number nobody measured.
- **Status:** `partially-controlled`

### DC-028 — A synthetic benchmark measures the benchmark
- **Signature:** a workload is generated to exercise a system, the numbers come out clean and
  repeatable, and a conclusion is drawn about where the cost is. The generator differs from real
  input in some dimension nobody listed — file age, type complexity, fan-out, size distribution — and
  that dimension is the one that drives the cost. The measurement is accurate about a workload that
  does not exist.
- **Why it survives:** everything about it looks like rigour. There is a number, it reproduces, and
  it beats a guess — which is exactly the standard "measure, do not infer" sets, so the measurement
  is trusted the moment it exists. The generator is written to make the system *work*, not to
  resemble the input, and nobody re-reads it once the numbers start coming out.
- **Instances:** 2026-08-29 — a synthetic workspace of 20 projects × 120 trivial types produced two
  successive conclusions about extraction cost, **both wrong**, and both acted on. First "parsing is
  97% of the read" (from a timer that also bundled disk I/O — DC-009), then "file I/O is 97% of
  extraction". On a real repository the profile is **walk 1,167ms > parse 694ms >> read 53ms** — the
  reverse. Two independent flaws: the generated files were newly created, so every read paid a
  one-time ~4ms/file antivirus scan that never recurs; and the generated types were trivial, so the
  symbol walk had nothing to do. Neither is visible in the numbers; both are obvious in the generator.
- **Control:** the spike runs against **named real repositories** and prints the same timings, so the
  synthetic figure and the real one appear side by side and a divergence is visible rather than
  theoretical. `spikes/joins-on-a-real-repo/RESULT.md` now carries both, with the synthetic ones
  explicitly marked as artefacts and the reason each inverted.
- **The generalisation to apply elsewhere:** before believing a synthetic measurement, **write down
  which dimensions of the real input drive the cost, and check the generator produces them.** File
  age, complexity, distribution and cardinality are the usual suspects. And where a real corpus
  exists, measure THAT first and use the synthetic one only to scale it — the reverse of the order
  taken here. A synthetic benchmark is for exploring the shape of a curve, never for locating a
  bottleneck.
- **Residual risk:** nothing fails when a new generator is written. The control is that the real-repo
  numbers are printed beside the synthetic ones on every run, which makes a divergence visible to
  whoever reads the output — and only to them.
- **Status:** `partially-controlled`

### DC-029 — A full-tree re-render rebuilds live children from a factory instead of reconciling by key

- **Signature:** a UI/layout adapter renders by discarding the whole realized tree and rebuilding it
  from the model on every mutation, invoking a content factory for **every** child — including
  children that did not change. Where a child owns live state (a process, a session, a socket, a
  media handle), the correct-looking replacement silently destroys that state. The loss is of
  *state*, not of *shape*, so the rebuilt child looks identical to the one it replaced and nothing
  visible signals the loss.
- **Why it survives:** every in-session visual check passes — the pane is present, titled, and
  drawing. The factory faithfully produces a valid replacement, so screenshots, layout assertions,
  and accessibility-name checks all pass over the rebuilt tree. Only a test that pins a child's
  *identity or process* across a mutation — or a user who had something running — reveals it.
- **Instances:**
  - 2026 — `WorkbenchAdapter.Render()` replaced `Manager.Layout` wholesale and rebuilt every pane via
    the content factory (`WorkbenchAdapter.cs:50,184`); each new `TerminalSurface` started a ConPTY
    child in a kill-on-close job, so opening a second terminal — indeed **any** layout mutation
    (5 `Render()` sites) — terminated every live terminal, including a running Copilot session
    (INV-0002).
- **Control:** a test that opens a terminal, records its session/process identity, applies a second
  unrelated layout mutation, and asserts the first session's identity is **unchanged**. **Observed
  failing** against the pre-fix code (the `TerminalSurface` instance is replaced and its process
  killed). The fix is to reconcile by `ContentId`: reuse the existing content element for an
  unchanged surface, create only for new surfaces, dispose only for removed ones.
- **The generalisation to apply elsewhere:** any adapter that projects a model onto a realized view
  must **reconcile by a stable key**, not rebuild — reuse unchanged nodes, add new ones, remove gone
  ones. Treat "rebuild the whole view on mutation" as a defect wherever a child can own state. The
  next likely victim here is the unwrapped windowed canvas/WebView2 surface.
- **Status:** `controlled` (fix landed: `WorkbenchAdapter.Render()` reconciles by `ContentId`; the
  control test `Render_ReusesExistingContent_WhenLayoutMutates_SoLiveSurfacesSurvive` was observed
  failing against the rebuild and passes against the reconcile — INV-0002 Phase 1)

### DC-032 — Reconciling reused instances makes a per-render binding accumulate handlers

- **Signature:** code that previously rebuilt a view on every change subscribed to events with a bare
  `+=` and got away with it because each render produced a fresh instance. The moment the render is
  changed to **reconcile** (reuse the same instance), that `+=` runs again on the same object every
  render, so handlers pile up and one user action fires N times.
- **Why it survives:** the reconcile change is made to fix a *different* defect (a rebuild killing
  live state), the code compiles, and the accumulation is invisible until a handler's side effect
  (an announcement, a refresh, a navigation) is observed happening two, three, four times.
- **Instances:**
  - 2026 — moving `WorkbenchAdapter.Render()` to reconcile (DC-029) made `WorkbenchShell.BindCanvas`'s
    `canvas.FocusLeaveRequested += (lambda)` accumulate. Fixed with a bind-once guard (`_focusBoundCanvas`);
    the four sibling subscriptions were already idempotent (`-=` then `+=`).
- **Control:** every re-runnable binder either uses `-=` before `+=` (named handlers) or guards a
  lambda subscription so it runs once per instance. When changing a rebuild to a reuse, audit every
  `+=` in the code that runs per render.
- **The generalisation to apply elsewhere:** "reuse instead of rebuild" changes the lifetime
  assumption every per-render side effect was written against — sweep subscriptions, one-time setup,
  and anything that assumed a fresh object each pass.
- **Status:** `controlled` (bind-once guard landed; siblings verified idempotent)

### DC-030 — A caption is clipped by the very container that is too narrow to hold it

- **Signature:** a control places a text label inside a fixed-size container that cannot fit it —
  a caption under a glyph in a narrow icon rail, a name in a fixed-width tab — so the string renders
  truncated ("Coordinate" -> "ordina") with no ellipsis and no overflow affordance. The label is
  usually a **redundant** second channel: the same meaning is already carried by an icon, a tooltip,
  and the accessible name, so nothing is lost by removing it and everything is lost by keeping it.
- **Why it survives:** the token linter passes (the colour, font and size are all on-system — the
  fault is width-vs-content, not an off-token value), the accessible name is intact (so screen-reader
  checks pass), and a populated screenshot at the design width looks fine. Only a render at the real
  constrained width, with the real longest label, shows the clip. This is the pack UX-A/UX family
  ("archetype/label mis-applied at a width the task cannot hold") seen at the widget scale.
- **Instances:**
  - 2026 — the workbench activity rail rendered a 9px caption under a 20px glyph in a 56px column;
    "Explore/Coordinate/Compose" clipped. Fixed by going icon-only with tooltip + `AutomationProperties.Name`
    (the VS Code / JetBrains idiom). INV/review: `docs/reviews/ui-activity-rail.md`.
- **Control:** render every fixed-width text-bearing control at its **real** container width with its
  **longest real** label and assert it is not clipped (no truncation, or an intentional ellipsis with
  the full text reachable by tooltip/name). Where the label is redundant with an icon+tooltip+name,
  prefer removing it (the higher rung: make the clip impossible) over shrinking the font.
- **The generalisation to apply elsewhere:** a label inside a fixed small container is a latent clip;
  before adding one, ask whether the icon + tooltip + accessible name already carry the meaning, and
  if so, do not add the caption. Test overflow with the longest real content, never the demo content.
- **Status:** `controlled` (fix landed; the icon+tooltip+name pattern makes the clip structurally
  impossible for the rail)

### DC-031 — A surface asks a narrower question than the one it exists to answer
- **Signature:** a view is built to show a whole thing — a graph, a corpus, a history — and its data
  call fetches a *slice*: one root and its neighbours, the first page, a single match. The slice
  renders correctly, so nothing looks broken; there is no error, no empty state, no truncation
  notice. The view simply shows a small, plausible, complete-looking answer to a question nobody
  asked, and it does so from the day it is written.
- **Why it survives:** the slice call is the natural one to reach for, because it is the API that
  already exists — "describe this node" is right there and "give me the graph" is not, so the surface
  is written against what is available rather than what it needs. Every test then encodes the slice
  as the contract: the neighbourhood tests here asserted a root, its neighbours, and an omitted
  count, and all of them passed while the pane showed two nodes of two thousand. And the store is
  full, so every measurement of extraction, coverage and joins looks healthy — the defect lives
  entirely between a correct store and a correct renderer.
- **Instances:** 2026-08-30 — reported by the user, comparing TheTerrace in this tool against the
  same repository in Obsidian: **two nodes versus a full graph**. The canvas called
  `FindAsync(term: "", maxResults: 1)` to pick a root and then `DescribeAsync(root, 40)`. The store
  held 12,100 assertions across 2,164 nodes. Nothing had ever shown a graph, and four unit tests
  described that behaviour approvingly.
- **Control:** `GraphProjection` answers the question the surface exists for — every node and edge,
  bounded by a node cap that is *reported*, with attributes folded onto nodes rather than drawn as
  edges. `CanvasGraphViewModel.LoadAsync()` with no root now means the WHOLE graph and a root means
  drill-down. The tests that encoded the slice were given explicit roots and a new one asserts the
  default. Proven across the daemon too, because every cross-boundary defect so far has been "right
  in process, wrong through the pipe".
- **A second finding inside the first:** with the whole graph finally visible, its six most-connected
  nodes were `string`, `int`, `Task<TResult>`, `DateTimeOffset`, `IReadOnlyList<T>` and `Guid` — 773
  edges to `string` alone. A graph whose centre is the BCL is not a picture of anybody's domain, and
  a cap ordered by raw degree drops the user's own types to keep framework primitives. Nodes now
  carry `IsExternal` (nothing in the workspace declares them) and declared nodes are kept first. The
  same repository's centre became `AppDbContext`, `Fixture`, `SportMonksProvider`.
- **The generalisation to apply elsewhere:** for any view of a whole, ask **"what is the cardinality
  of what this is showing, and what is the cardinality of what it fetched?"** A one-line answer to a
  two-thousand-line question is the signature, and it is invisible from inside the view. The related
  tell is a test suite that describes the slice fluently — passing tests are evidence about the code
  that exists, never about the code that should.
- **Residual risk:** the whole-graph path is capped at 2,000 nodes for the canvas and 5,000 in the
  projection; both report what they dropped, and neither has been exercised against a repository that
  reaches them. Other surfaces have not been audited for the same shape.
- **Status:** `partially-controlled`

### DC-033 — A reader recognises one spelling of a pattern and reports the rest as absent

- **Shape:** an extractor, parser or matcher is written against **the form the author had in front of
  them** — one call chain, one file layout, one config key — and every other legal spelling of the
  same thing falls through to the "not found" path. Nothing errors. The output is well-formed, the
  tests pass on the one shape they were written from, and the missing facts are quietly replaced by
  a weaker mechanism (a guess, a default, a convention) that is *designed* to be there and so raises
  no alarm.
- **Signature:** a **ratio** that nobody looks at. The count of facts recovered by the precise reader
  against the count recovered by the fallback: 1 verified against 123 inferred is not a system with a
  small gap, it is a reader that does not work. The tells in code are a **syntactic** match on a
  shape a **semantic** question would settle (`member.Expression` walked for a generic argument
  instead of asking what the receiver's type is), and a matcher that requires two things to appear in
  **one expression** when the language lets a variable sit between them.
- **Why it survives:** the fallback is doing its job. A convention-based guess produces a plausible
  answer for exactly the cases the precise reader missed, so the surface looks populated and the
  numbers look healthy. Coverage is green because a test fixture is written by the same person, in
  the same style, as the reader — the fixture and the reader agree because they share an author, not
  because either matches the world. And a disclosure cannot fire: the reader does not know it missed
  anything, so "not declared" and "declared in a way I do not read" arrive as the same value.
- **Instances:** 2026-08-30 — `CSharpExtractor.FluentTableMappings` matched
  `Entity<T>()...ToTable("x")` as a single expression. TheTerrace, like most EF codebases, writes
  `var terrace = modelBuilder.Entity<Terrace>(); terrace.ToTable("Terrace", "setup");` — so the
  extractor recovered **1 declared mapping and guessed 123**, on a repository that states every one
  of them in `OnModelCreating`. Found only because a timing investigation asked why 66 files were
  being walked to produce nothing. The same reader also emitted the entity name **as written in
  source** (`Order`) where every other assertion uses the display string (`Shop.Order`), so even its
  successes were edges whose subject matched no node.
- **Control:** resolve semantically, not syntactically — `model.GetTypeInfo(receiver)` answers every
  style with one rule, because in all of them the receiver is an `EntityTypeBuilder<TEntity>`, and it
  returns a symbol whose display string is the same name the rest of the extractor emits. Five tests
  pin the styles (chained, local-variable, non-literal name, unrelated `ToTable`, generated source);
  the generated-source control was **observed failing** with the skip disabled. Verified joins on the
  real repository went **1 → 64** and inferred fell 123 → 73.
- **The generalisation to apply elsewhere:** for any reader of someone else's notation, ask **"how
  many legal ways are there to write this, and how many do I match?"** — then check the **ratio of
  precise hits to fallback hits on real input**, because that ratio is the only place the answer
  shows. Prefer the semantic question over the syntactic one wherever a compiler, schema or resolver
  can be asked; a syntactic matcher is a bet that the author and the world share a style. The
  siblings swept in this repository: `PythonExtractor` and `TypeScriptExtractor` are line-oriented by
  a *declared* `simplify:` ceiling and disclose it, so they are bounded rather than blind; the Bicep
  reader matches literal names only, and says so.
- **The sweep, run 2026-08-30, with what it found.** The class says the signature is a ratio on real
  input, so the sweep measured rather than read. **`TypeScriptExtractor` was a second instance:** its
  export pattern knew `class|interface|type|enum|function|const` and did not know `async`, the
  generator star, `namespace`, `let` or `var`. TheTerrace declares **124 `export interface`, 26
  `export type`, 16 `export const` and 4 `export namespace`** — so four declarations were reported as
  absent rather than as unread. `PythonExtractor` was checked and is clean (`^(?:async\s+)?def` already
  covers the coroutine form). The Bicep reader and the schema reader are bounded by a *declared*
  `simplify:` ceiling and disclose it, so they are narrow by agreement rather than blind.
- **The control that generalises, and the reason it is the important half.** Widening a pattern fixes
  today's spelling and will be wrong again for tomorrow's. **The reader now counts its own misses and
  discloses them** — `typescript-exports-not-recognised (N)` — so the next form nobody anticipated
  announces itself on the scope instead of waiting to be found by a person grepping a repository.
  Re-export forms (`export { A }`, `export * from`, `export type { C }`) are excluded deliberately:
  counting them would give a miss rate that never reaches zero and therefore says nothing. Sixteen
  tests pin the fourteen known spellings, the exclusion, and the alarm itself.
  **The generalisation for other readers: make the reader publish its miss rate, because a ratio
  nobody looks at is exactly what this class hides behind.**
- **The control was itself miscalibrated, and only a SECOND repository showed it (2026-08-30).** The
  miss-counter's doc comment said `export default someExpression` was excluded as a non-declaration;
  the pattern excluded `{`, `*`, `=` and `type {` and never excluded it. So `export default
  defineConfig({…})` and `export default test;` counted as misses — and `export default` is
  ubiquitous, so the disclosure would have fired on nearly every real TypeScript codebase and become
  noise. Every measurement in this area had been taken from ONE repository, where the form did not
  appear. **A control's false-positive rate is only observable on input it was not written against**,
  which is the reason to run a new reader over a repository nobody used while building it. Fixed, and
  a 165-file TypeScript repository now reports zero misses.
  Note the shape: the exclusion was WRITTEN IN THE COMMENT before it was implemented — the same
  defect as the evidence page documenting a byte cap it did not apply, and as `find` reporting a
  `MaxBytes` it never enforced. Three instances in one session of **a claim in prose that the code
  does not make true**; when a comment states a bound, the next question is which line applies it.
- **A THIRD reader, 2026-08-31 — and this one had no statement anchor at all.** The TypeScript
  extractor matched `from\s+['"]([^'"]+)['"]` **anywhere in a file**. The word "from" in prose, in a
  template literal, or at the end of one string literal with the *next* literal supplying the closing
  quote, all began an import. MEASURED on TheTerrace: of 14 import edges, **12 were invented and 0
  described a dependency between two things in the repository** — including
  `the product must include full fantasy management,` (prose in a generated data file), `${url}`, and
  ` + quoteFileNameIfNeeded(((_c = patch.oldFileName) !== null && _c !== void 0 ? _c : ` (two adjacent
  literals in compiled JavaScript).
- **The sharpest instance looked completely legitimate.** `@playwright/test` was assumed real — by me,
  in the brief I wrote — and is a line of Playwright's own **code-generation template**:
  `import { test, expect… } from '@playwright/test';`, the text it emits when scaffolding a test.
  Nothing about the specifier looks wrong. Only opening the file says otherwise, which is the whole
  lesson: **a plausible-looking fact is the one you have to go and read the source for.**
- **Both directions measured, as the class demands.** Old matcher 19 occurrences / 13 distinct; new
  statement-anchored matcher 15 / 12. Every one of the 10 dropped was opened by hand: all inside a
  string literal or a JSDoc example, **0 real specifiers lost**. The new matcher also finds 9 the old
  one missed (`from"./x"` with no space). Tightening without measuring the other direction is how the
  first `uses_table` fix matched nothing at all.
- **The same reader hid a coverage gap behind the invention.** `export` was a CONDITION on seeing a
  declaration, so 13 scopes produced zero classes, functions and interfaces while all 13 disclosed
  `typescript-non-exported-not-analysed` — the disclosure was true and the cause was a gate nobody
  had questioned. Removing it: 22 functions and 2 classes appear, with `is_exported` recorded as an
  attribute instead. Verified by hand — `grep -cE "^(async )?(function|class)"` over the six
  hand-written files gives exactly the 24 emitted.
- **The same root cause runs in the OTHER direction, and I shipped it (2026-08-30).** The
  `uses_table` reader matched an SQL keyword followed by a word ANYWHERE in a string literal, so the
  sentence *"we update the record"* produced an edge to a table called `the`. MEASURED: 63 prose
  strings in one repository, and its `uses_table` count fell from **150 to 56** once the reader
  required a statement SHAPE — a literal beginning, after its start or a semicolon, with a SQL verb.
  Under-matching hides real facts; over-matching invents them, and the invented ones are worse
  because they arrive labelled **Verified**. The shared cause is that neither direction had been
  measured against real input: **a matcher is not finished until you know both what it misses and
  what it invents**, and both numbers come from a repository nobody used while writing it.
- **The naive fix broke the real case, which is why both directions must be measured together.**
  Requiring each literal to begin with a verb found **nothing at all** on the repository that
  motivated the feature: real code splits SQL across concatenated literals, and the fragment holding
  `FROM dbo.AssessmentJob` begins with `FROM`. The reader folds the `+` chain and reads it as one
  statement; a chain containing anything non-literal is skipped whole rather than half-read.
- **A smaller lesson worth keeping:** the regex form of that shape test silently returned false for
  `"INSERT INTO dbo.AssessmentJob (…)"` — a string that plainly begins with one of its own
  alternatives — and cost more to diagnose than the check was worth. It is explicit code now. When a
  one-line matcher behaves impossibly, replacing it with something readable is usually cheaper than
  proving why it does not.
- **The invent-direction now has a CONTROL, and it found four more on its first run.**
  `ExtractorsDoNotInventTests` feeds every reader a corpus with no declarations and plenty of text
  SHAPED like declarations, and asserts it produces nothing but disclosures. On the first run:
  - the **SQL** reader read `-- CREATE TABLE Ghost` and `/* CREATE TABLE Historical */` as tables;
  - the **TypeScript** reader read `export class Removed {}` out of a block comment;
  - the **Python** reader read a class out of a **docstring** — the one place its column-zero rule
    cannot tell documentation from declaration;
  - the **C#** `uses_table` reader turned *"delete from your account to remove it"* into
    `table:your`, because that sentence genuinely begins with a SQL verb and the shape test alone
    could not reject it.
- **Commented-out code is the worst possible input for a line-oriented reader**, and every repository
  is full of it — it is real syntax, because it *was* code. `SourceText` blanks comments (keeping
  newlines, so provenance line numbers stay true) before any of the three readers believes a line.
  The C# case needed a second rule: a real table reference **ends where a clause can begin** — a
  keyword, punctuation, or the end of the statement. In prose the next token is just another word.
- **Two things the fixes got wrong first, kept because they are the lesson.** Blanking string
  contents for SQL deleted `"main"."Thing"` — in SQL a double quote is a quoted IDENTIFIER, not a
  string, so the reader lost the very names it exists to find. And a `PRINT 'about to create table X'`
  names no table while `EXEC('CREATE TABLE …')` does: the reader can tell neither from the other, so
  it reads neither and discloses the count.
- **The measurement shows the correction, which is what a third reading is for.** Verified joins on
  the three repositories went `64 → 120 → 95`, `0 → 57 → 55`, `35 → 50 → 46`. The middle number was
  inflated by prose; the last is the honest one. A single reading would have recorded 120 as progress.
- **Residual risk:** the receiver test is a name match on `EntityTypeBuilder` /
  `OwnedNavigationBuilder`, so an EF fork or a wrapper builder is not read; `ToTable` reached through
  an interface or an extension method on a non-builder is not read. Both now fall into the counted
  `fluent-table-mappings-unresolved` disclosure rather than silence. `IEntityTypeConfiguration<T>`
  is covered in principle by the same rule but has no real-repository evidence yet. The TypeScript
  miss-counter is line-oriented, so a declaration split across lines is invisible to both the reader
  and its own alarm.
- **Status:** `controlled`

### DC-034 — A control's affordance is present but wired to nothing

- **Signature:** a button, menu item, or gesture is visible and looks live (a tab's ✕, a toolbar
  icon) but its command is bound to `{x:Null}` / a no-op, so clicking it does nothing. A customized
  control template that replaced the stock command binding is the usual cause — the visual survived,
  the behaviour did not.
- **Why it survives:** the control renders, hover states work, and nothing errors; only *using* it
  reveals the dead wire. Unit tests that assert the model operation (close) pass, because the model
  path is fine — it is the view→model connection that was severed.
- **Instances:**
  - 2026 — the rounded-tab template (`DockRoundedTabs.xaml`) set the tab close button's
    `Command="{x:Null}"`, so the ✕ on every tab did nothing; closing a terminal was impossible from
    the tab. Fixed by wiring the button (and AvalonDock's `DocumentClosing`) through the layout
    model's `CloseSurface`, plus a "Close" item on the tab context menu.
- **Control:** when a custom control template replaces a stock one, verify every interactive element
  it carries still performs its action — click it, or assert its `Command`/handler is non-null. A
  visible affordance with no behaviour is a defect even though nothing throws.
- **The generalisation to apply elsewhere:** replacing a template inherits the obligation to
  re-wire every command the stock template provided. Grep a customized template for `{x:Null}` on a
  `Command` and treat each as a dead affordance until proven otherwise.
- **Status:** `controlled` (close routed through the model via the button, the context menu, and
  DocumentClosing; app launches and the model-close path is covered by the reconcile dispose test)

### DC-035 — A default view loads the whole dataset instead of a bounded slice

- **Signature:** a surface whose spec says "always show a bounded slice" is implemented to fetch the
  *entire* dataset when there is no explicit focus — "no root means the whole graph", "no filter
  means every row". It works on small inputs and fails only at scale: an oversized response overflows
  a transport frame, a layout melts into a hairball, or a render stalls. The failure looks like an
  infrastructure limit (a 1 MiB IPC cap) but the limit is a symptom — the design asked for everything.
- **Why it survives:** every small/medium repo loads fine, and the whole-graph default was often
  introduced to fix the *opposite* bug (too little shown — "only 2 nodes"), so it reads as a
  correction rather than an over-correction. The spec that forbids it (a bounded-neighbourhood /
  paginated requirement) is not re-read at the moment the default is chosen.
- **Instances:**
  - 2026 — `CanvasGraphViewModel` with no focus called `WholeGraphAsync` (5,000-node cap); on
    TheTerrace (~2,813 nodes / 8,602 edges) the serialized response exceeded `IpcFraming.MaxFrameBytes`
    (1 MiB), the daemon's uncaught write exception closed the connection, and the graph view showed
    `ipc.transport_closed`. It violated the surface's own spec (US-K2: "the whole graph is never
    rendered at once"). INV-0003.
- **Control:** the default (no-focus) view returns a **bounded** entry by construction — an aggregated
  overview or a ranked-important slice, sized to the transport regardless of dataset size — and a
  test asserts the no-focus response node/byte count is bounded and does not grow with the corpus. A
  request that would exceed the bound returns a labelled "narrow your focus" state, never an opaque
  transport failure.
- **The generalisation to apply elsewhere:** raising the transport/frame/timeout limit to make a
  whole-dataset load fit only moves the wall to the next-larger input. When a "load everything"
  default hits a limit, the fix is almost always to **make the default bounded** (focus+context,
  pagination, level-of-detail, server-side aggregation), not to enlarge the limit. Re-read the
  surface's spec: it usually already forbade the dump.
- **Core's half of the control, landed 2026-08-30.** Both defects the investigation named are fixed,
  and both were MEASURED rather than reasoned about:
  - **The transport no longer closes silently.** `IpcServer.Respond` checks the encoded size *before*
    writing and returns `IpcErrorCodes.PayloadTooLarge` with the actual and permitted byte counts.
    Checked before rather than caught after on purpose: a partially written frame leaves the peer
    reading a length prefix whose body never arrives, which is a hang rather than an error. The
    writer's own throw is correct and stays.
  - **The default view is bounded by construction.** No-focus now asks for
    `GraphQuery(OverviewNodeCap: 1500, IncludeExternal: false)` — this workspace's own declared code,
    ranked by degree, with what it dropped counted and named in the caption. Measured on TheTerrace:
    the whole graph is **1,522,284 bytes** against a 1,048,576-byte frame (it could never have been
    delivered); the new default is **533,495 bytes for 1,500 nodes, 618 omitted — fits**. A test grows
    a synthetic corpus past the cap and asserts the response does not grow with it.
  - **Excluding externals is part of the fix, not a separate tidy-up.** The whole-graph default was
    also *unreadable*: measured, the six most-connected nodes of a real repository were `string`,
    `int`, `Task<T>`, `DateTimeOffset`, `IReadOnlyList<T>` and `Guid`. A first view centred on the BCL
    is not a picture of anybody's domain, so bounding by size and bounding by meaning turned out to be
    the same change.
- **The part of this class that is mine to own, stated plainly.** The whole-graph default was
  introduced by the Core session as the fix for DC-031 ("a surface asks a narrower question than the
  one it exists to answer") — the graph pane rendering two nodes of two thousand. That fix
  **over-corrected past the spec it was restoring**: the answer to "one arbitrary alphabetical node"
  was a bounded overview of meaningful nodes, and `knowledge-exploration.md` US-K2 already said so.
  The pairing is worth keeping: **DC-031 and DC-035 are the same axis overshot in opposite
  directions**, and a fix for one lands on the other unless the spec is re-read at the moment the new
  default is chosen.
- **The aggregated overview landed 2026-08-30, completing Core's side.** `GraphOverview` returns the
  workspace as GROUPS rather than truncated nodes, grouped by the ids' own hierarchy (a C# symbol is
  `TheTerrace.Features.Competitions.Season`; a module is `src/app/models`), with `Depth` as the zoom
  control. No community-detection algorithm, deliberately: its output is unstable under small graph
  changes, so the same repository would regroup between two indexes and the picture would move for
  reasons the user cannot see. MEASURED on TheTerrace at depth 3 — `Features.Fixtures` 117,
  `Features.Teams` 117, `Features.Matches` 107, `Infrastructure.Data` 70, in **55,758 bytes** against
  533,484 for the node graph. Each group carries its `NodeCount` (a dot standing for 240 types is
  only honest while the 240 is on it), each link its `Weight` and the **weakest** status of the edges
  it bundles.
- **The audit that followed it found the class was wider than the graph.** Every read operation was
  measured at its ceiling: `evidence` was at **95.8%** of the frame and `find` returned 461,750 bytes
  while REPORTING a 64 KiB cap it never applied. The generalisation is not "the graph was too big" —
  it is that **every ceiling in the read surface counts ITEMS while the transport limit is in
  BYTES**, and item size comes from repository content. All three are byte-bounded now.
- **The control is reflective, because hand-auditing found these once and would not find the next.**
  `EveryOperationFitsTheFrameTests` derives the operation list from `IWorkspaceQueries` itself and
  fails when a method is added with no frame-size check — observed failing with an entry removed.
  Writing the list out would have been a fixture restating the product's list (DC-021) and would go
  stale in exactly the case that matters: a new method nobody weighed.
- **A SECOND instance, and the sweep that should have followed the first (2026-08-30).**
  `ProjectionService.Knowledge` read the first 200 `has_type` assertions and filtered THOSE to
  knowledge — so on any real repository the 200 were code types in alphabetical order and the filter
  left nothing. MEASURED: **0 items returned on a workspace holding 468 knowledge nodes**, which the
  user reported as "knowledge still says 0". The first instance was fixed in `GraphProjection` and no
  sibling sweep followed; this is what that costs.
- **The sweep, run properly this time.** Every bounded read in the projection service was checked:
  `Find` filters inside `SearchNodeIds`, `Describe` inside `AssertionsTouching`, `Impact` inside
  `OutgoingAssertions`, `Evidence` inside the cursor page — all four apply the cap to rows the query
  has ALREADY filtered, which is the correct order and also the cheaper one, because the filter uses
  an index. `Knowledge` was the only place the order was inverted, and it was inverted because the
  filter lived in C# rather than in the query.
- **The signature, stated so it is recognisable without reading every projection:** a bounded read
  whose `.Where(...)` is applied to the RESULT of the read rather than expressed in it. If the filter
  is in the query the cap cannot be wrong; if it is in the caller, the cap chose the rows before
  anyone asked what was wanted.
- **Residual, named rather than implied:** `Knowledge` still reads each node's touching assertions at
  `MaxEdgesCeiling` (500) and splits them into links and backlinks afterwards. A document with more
  than ~495 real links would get an arbitrary 500 and no omission count. No repository measured comes
  close, and the fix is the same shape if one ever does.
- **Status:** `partially-controlled` — Core's side is complete and tested (bounded default, legible
  `PayloadTooLarge`, byte bounds on every read operation, the aggregated overview, and a reflective
  gate that catches the next operation). **Design's half is open:** rendering the overview and the
  "narrow your focus" state. The write side was measured too and needs nothing: an `IndexSummary` for
  28 scopes is **1,724 bytes**, three orders of magnitude below the frame.

- **Third instance, 2026-08-30 — and the first two fixes each moved it one step along.** The knowledge projection read the first 200 `has_type` assertions and filtered THOSE to knowledge: the 200 were C# types in alphabetical order, so a workspace holding 468 knowledge nodes returned nothing. The fix pushed the knowledge filter into the query. It left the read capped at 200 ids **in id order** while the TERM was still matched in memory afterwards — so a search saw the alphabetically first 200 of 1,255, and a document sorting later was reported as not existing. MEASURED on the real workspace: 757 knowledge documents carry a type, and searches for *spec*, *adr*, *ui* now return 34, 9 and 31 where the capped read could reach only ids beginning with the earliest letters.
- **What the sweep missed, and why.** The earlier sweep checked whether each *reader query* expressed its filter in SQL, and all of them did. It did not check the **callers**, and the caller is where a second filter had been left outside. The signature has to be read as covering the whole path from query to result: *every* filter must be inside the read, not merely the one that was moved there. A cap applied before a filter returns the wrong slice trimmed to the right shape — moving one filter in and leaving the next one out relocates that, it does not remove it.
- **Control:** `StoreReader.KnowledgeNodes(term, type, limit)` applies both filters and counts the total over the same filtered set, so `Bounds.OmittedNodes` is measured against what matched rather than what was read. `KnowledgeSearchSeesEveryDocumentTests` puts the only matching document past the cap on purpose — 400 fillers sorting before it — so a filter applied after the read cannot reach it. Observed failing on the reinstated shape, 4 of 4, printing "the one that matches sorts past the id cap".

- **Fourth instance, 2026-08-31 — written the day after the third was recorded, by the person who recorded it.** The new node-content reader resolved a node's file by taking `AssertionsTouching(id, 50)` and filtering it for the fact carrying a path. `AssertionsTouching` orders by subject, so on a node whose callers sort alphabetically before it, fifty rows of callers filled the window and the node's own declaration never arrived. MEASURED on TheTerrace: the most connected type in the workspace — `TheTerrace.Infrastructure.Data.AppDbContext`, 244 edges, callers all named `TheTerrace.Features.*` — reported *"no recorded source"*, while every small type worked. **The failure sorted by popularity**, so the more important the type, the more likely the reader had nothing to show.
- **The signature was known and still did not fire in the author's head.** The class had been recorded twice in twenty-four hours and its lesson written as *every filter must be inside the read*. Knowing a class is not a control (CI6): what caught this was measuring the feature on a real repository, and what will catch the next one is `StoreReader.DeclaringAssertion`, which asks the store for the node's declaration rather than sieving a page of neighbours.
- **The fixture could not reproduce it, twice, silently.** The first attempt referenced the hub by property type — the extractor emits no edge for that, so the hub had 3 touching facts against a cap of 50. The second used inheritance but named the referencing types so they sorted *after* the hub, so its declaration was always in the window. Both passed against the un-fixed code. The test now **asserts its own preconditions** — that the hub exceeds the cap, and that its declaration is genuinely absent from the capped window — before asserting anything about the fix. A fixture that cannot reproduce is DC-016 wearing a green tick, and the only reliable way to find one is to run the control against the broken code and watch it fail.

### DC-036 — A graph is drawn with a layout that does not scale to its node count

- **Signature:** a node-link view uses a layout that is fine for a handful of nodes and degenerate for
  many — a single ring, a fixed grid, a naive tree — so past a small threshold the nodes pile up and
  overlap into an unreadable blob. Often paired with heavy node glyphs (opaque cards/boxes with borders
  and backgrounds) that occlude each other and hide the edges, and with no zoom/pan/LOD to recover.
- **Why it survives:** it looks fine in the demo and the tests, which use a tiny rooted neighbourhood
  (a root + a few neighbours on the ring). The failure appears only on a real graph, and the layout
  choice is usually justified in a comment as deliberate ("NOT a force sim") without the node-count at
  which it breaks being stated or tested.
- **Instances:**
  - 2026 — the graph canvas (`CanvasPage.cs`) laid the 2D view out as a single ring (root centred,
    all neighbours on one ring) and drew nodes as opaque padded boxes. On TheTerrace (~50 nodes shown)
    it rendered as a pile of overlapping cards with edges hidden and labels occluded — while the
    surface's own spec (`knowledge-exploration.md` US-K11) already called for force layout / semantic
    zoom. Review: `docs/reviews/ui-graph-canvas.md`; target mockup `docs/mockups/graph-canvas.html`.
- **Control:** the graph view uses a layout whose readability does not degrade with node count — a
  force-directed spread (nodes settle apart) and/or semantic-zoom aggregation (clusters), with
  lightweight glyphs (degree-sized dots, edges behind) and zoom/pan. State the node count the layout
  is good to, and render an honest "showing N of M" plus a "narrow your focus" state past it.
- **The generalisation to apply elsewhere:** any visual layout has a capacity; before shipping one,
  name the input size it stays legible at and what happens past it. A layout with no stated capacity
  and no degrade-to-aggregate path is a pile waiting for a real dataset. Reading is parallel only if
  the eye can separate the marks — overlap destroys it.
- **Status:** `partially-controlled` — the CanvasPage **2D rebuild is landed**: single-ring →
  phyllotaxis initial spread + a bounded Fruchterman-Reingold settle (iterations shrink as the graph
  grows, so cost stays roughly constant and a huge graph degrades to its phyllotaxis spread rather
  than freezing); opaque cards → **degree-sized dots with labels-on-demand** (hover/focus, always for
  root); edges render behind. Verified by the P2-FOCUS-03 keyboard-trap probe (real out-of-process
  WebView2) and App.Tests 132/132 — the a11y contract (focusable `.node` spans, document-level Tab
  trap, boundary `focus.leave`) is unchanged. Remaining: zoom/pan/fit and real semantic-zoom LOD
  clustering, the latter blocked on the Core community/aggregation query (session-contracts §4c).
  Review `docs/reviews/ui-graph-canvas.md`; target mockup `docs/mockups/graph-canvas.html`.

### DC-037 — A projection drops a sizing/proportion the model carries

- **Signature:** a model node holds an explicit proportion (a split weight, a column width, a flex
  ratio) and the code that projects that model into a UI framework builds the structure faithfully but
  **never reads the proportion** — so the framework falls back to its own default (an equal share), the
  intended ratio is silently lost, and a user resize does not persist because there is no proportion
  field the projection round-trips through.
- **Why it survives:** the structure is correct (the right panes, the right nesting, the right
  orientation), so the layout *looks* plausible and every structural test passes. The dropped
  proportion only shows as "it opens at the wrong size" or "I can't resize that pane" — symptoms easy
  to misattribute to the framework or to airspace, because the projection code that omitted the field
  reads as complete.
- **Instances:**
  - 2026 — `WorkbenchAdapter.BuildPanel` projected the owned `SplitNode` tree into AvalonDock
    `LayoutPanel`s with the correct orientation and children but never applied `SplitNode.Weights`, so
    every pane defaulted to an equal `1*` share. The terminal pane (model weight 0.32) rendered at a
    fixed, unresizable size and the workspace/graph 0.38/0.62 split was lost. Fix: map each child's
    weight onto AvalonDock `DockWidth`/`DockHeight` as `GridLength(w, Star)`.
- **Control:** a projection test that reads the *framework's own* sizing off the realised tree and
  asserts it equals the model's proportions — here `Render_AppliesModelSplitWeights_AsProportionalDockSizing`
  reads each pane's `DockHeight`/`DockWidth` and asserts the `0.68/0.32` and `0.38/0.62` star ratios.
  It reads `1*` (RED) whenever the weight is dropped, so the class cannot return silently on a
  framework upgrade or a projection refactor.
- **The generalisation to apply elsewhere:** for every field the model carries that a framework can
  also represent, the projection either maps it or explicitly records that it doesn't — and a
  round-trip/projection test asserts the framework value equals the model value. A structural test that
  only checks *which* panes exist cannot see a dropped *proportion*; assert the numbers, not just the
  shape.
- **Status:** `controlled` — the weight is applied and the projection test is landed.

### DC-038 — A comment states a bound the code does not apply

- **Shape:** a doc comment, a parameter name or a reported field asserts a limit — *"stays comfortably
  inside X"*, *"bounded at N"*, `MaxBytes: 65536`, *"this form is excluded"* — and **no line applies
  it**. The value is assigned, passed to a struct, or described in prose, and never compared,
  clamped or matched against. The code is well-formed and the claim is false.
- **Signature:** the constant appears only on the left of an assignment and inside argument lists;
  grep it and every hit is a declaration, a comment or a field initialiser, never an `if`, a
  `Clamp`, or a `Take`. The prose tell is a sentence that quantifies without naming the line that
  enforces it — *"sized so"*, *"stays within"*, *"never exceeds"* — and the fastest check is to ask
  **which line makes this true**, then find it.
- **Why it survives:** the prose is where a reviewer looks, so the claim is what gets believed —
  including by the person who wrote it, months later. Tests do not catch it because the documented
  bound is usually far from the values a fixture uses, so nothing is near the limit. And the failure
  is invisible until the input grows: a page that is 15× its documented cap works perfectly on every
  repository small enough.
- **Instances (all 2026-08-30, all within one session):**
  - `ProjectionService.Evidence` documented that a page "stays comfortably inside `MaxResultBytes`
    once serialised". MEASURED: 2,000 assertions = **1,004,397 bytes**, fifteen times that constant
    and 95.8% of an IPC frame. One repository away from INV-0003.
  - `ProjectionService.Find` built a `ResultBounds` reporting `MaxBytes: 65,536` and returned
    **461,750 bytes**. The cap was handed to a struct and compared to nothing — a caller reading the
    bounds was told a limit that could not fire (DC-016 through a different door).
  - `TypeScriptExtractor`'s miss-counter documented that `export default someExpression` was
    excluded as a non-declaration; the pattern excluded `{`, `*`, `=` and `type {` and never
    excluded it. `export default` is ubiquitous, so the disclosure would have fired on nearly every
    real TypeScript codebase and become noise. Found only by running a SECOND repository.
  - `CanvasGraphViewModel.WholeGraphNodeCap` was declared with a doc comment calling it a ceiling
    and had **zero usages** — found by the control below, on its first run.
- **Control:** `tools/verify-bounds-are-enforced.py` — every constant whose NAME claims a limit
  (`Max*Bytes`, `*Ceiling`, `*Cap`, `*Budget`) must appear in a comparison, a `Clamp`, a `Math.Min`
  or a `Take`, searched over code with comments stripped so prose cannot count as proof. A bound
  applied indirectly needs an entry in `APPLIED_ELSEWHERE` **with a reason naming where it fires**,
  because "it is passed somewhere" is exactly what made `find` look safe. It found the dead
  `WholeGraphNodeCap` immediately and one false positive that is now a justified exemption.
- **What the control deliberately does NOT cover, said rather than implied:** it checks that a bound
  is APPLIED, not that a sentence describing it is TRUE. The TypeScript instance — a regex whose
  behaviour differed from its comment — is invisible to it. Half the class is mechanised; the other
  half still needs a reader, and pretending otherwise would be this class applied to its own control.
- **The generalisation to apply elsewhere:** when a comment quantifies anything, **find the line that
  makes it true before believing it** — and when writing one, prefer stating the mechanism
  (*"bounded row by row in `Evidence`"*) over the effect (*"stays inside the cap"*), because a
  mechanism names something a reader can go and check.
- **Residual risk:** the name-based detection misses a bound named without one of those suffixes, and
  a limit expressed as a magic number rather than a constant is invisible to it.
- **Status:** `partially-controlled`

### DC-039 — A focus-trapped surface reused in a new host without re-wiring its escape

- **Shape:** a control that deliberately traps keyboard focus and posts a "leave" signal at its
  boundary (so a host can route focus out) is reused in a NEW host that binds the graph/content but
  forgets to subscribe to the leave signal. The trap still fires; nothing consumes it; the keyboard
  user is stuck inside the surface with no way out — a WCAG 2.1.2 (No Keyboard Trap) failure.
- **Signature:** the surface exposes an escape event (here `CanvasSurface.FocusLeaveRequested`, the
  ADR-0015 contract) that the ORIGINAL host wires (`WorkbenchShell.BindCanvas`), and a second
  construction path (`CreateExplorerGraph`) sets the data source but not the escape handler. Grep the
  escape event: it is raised in one place and subscribed in fewer places than there are hosts.
- **Why it survives:** the surface works — it renders, it navigates, the mouse is fine — and the trap
  only bites a keyboard-only user who tabs to the boundary, which no populated-fixture test and no
  mouse-driven demo exercises. The focus contract lived in the first host's binding, so a new host
  that copies the *data* wiring silently drops the *focus* wiring.
- **Instances:**
  - 2026-08-30 — the Phase-1 Explorer mode built its graph via `CreateExplorerGraph`, which set
    `GraphSource` but never subscribed `FocusLeaveRequested`. In Explorer mode the graph canvas
    trapped keyboard focus with nothing routing out. Fixed: `ExplorerSurface` routes the leave into
    the reader region (`NodeReaderView.FocusReader`), so a Tab off the graph lands in the reader.
- **Control:** a surface that owns a focus contract carries its escape wiring with its construction,
  not in one host's binding — or every construction path is asserted to wire it. Here
  `Reader_FocusReader_LandsFocusInTheReader` proves the escape has a landing target; the real
  boundary-Tab→reader integration is the P2-FOCUS analogue at the Explorer level (a CanvasProbe
  follow-on).
- **The generalisation to apply elsewhere:** when a control with a keyboard contract (a trap, a
  roving tab-stop, a boundary handler) is instantiated a second way, the contract is part of the
  control's construction, not the first caller's setup. A second construction path that copies the
  data wiring and not the focus wiring is this class.
- **Status:** `partially-controlled` — the escape is wired and its landing target is tested; the
  full real-WebView2 boundary-Tab integration test is a follow-on.

### DC-040 — A retained component captures a dependency that arrives (or changes) later

- **Shape:** a lazily-created, then RETAINED, component captures a dependency (a query interface, a
  service, config) **by value at creation**. On the original host that dependency is set/updated later
  (a workspace attaches, a connection opens), and the original host rebinds — but the retained copy
  captured the old value (often null) and never rebinds, so it shows an empty or stale state forever
  while the original works.
- **Signature:** a factory reads a mutable field into a `new SomeViewModel(field)` and hands the result
  to a long-lived, reused surface; elsewhere a separate "attach/bind" path updates that field and
  re-creates the *original* consumer but not the retained one. The retained surface and the live one
  disagree, and only the retained one is wrong.
- **Why it survives:** the happy demo opens the dependency BEFORE creating the retained surface, so the
  captured value is good and it works; the bug only appears when creation and attachment race the other
  way, or when the dependency changes after creation. The original consumer works, which misdirects the
  investigation away from the retained copy.
- **Instances:**
  - 2026-08-30 — `WorkbenchShell.CreateExplorerGraph` built the Explorer graph's `CanvasGraphViewModel`
    from a captured `_queries`; the Explorer surface is retained (US-E6), so a surface first created
    before the workspace attached stayed bound to null and showed "No workspace is open" even with a
    workspace open — while the workbench graph (rebound via `BindCanvas`) worked. Fixed: read `_queries`
    LIVE in the `GraphSource` lambda, and refresh the Explorer graph on each mode entry.
- **Control:** a retained component reads a mutable dependency **live at use time** (capture the host,
  not the value), and/or **refreshes on re-activation**. A test that attaches the dependency AFTER
  creating the retained component and asserts it then works catches the capture.
- **The generalisation to apply elsewhere:** when a reused/retained surface depends on something that
  can arrive or change after the surface exists, do not snapshot it at construction — resolve it each
  time, or re-resolve when the surface is shown. Retention (US-E6 "don't rebuild") is about the VIEW,
  never a licence to freeze its data source.
- **Status:** `partially-controlled` — the live-read + refresh-on-entry fix is landed; a realized-shell
  integration test (Explorer shows the graph after a workspace opens) is a follow-on.


### DC-041 — Two "kind" fields with different granularity, and the coarse one shown where the fine was meant
- **Signature:** a domain has both a fine type (`has_type` → `azure-resource`, `table`, `class`) and a coarse dimensional class (`node_kind` → `source` vs `knowledge`), and a reader/label displays the coarse one where a user expects the fine one — a bicep resource reads "kind: knowledge".
- **Why it survives:** both fields are individually correct and individually tested; the overview path uses the fine one and the describe/reader path uses the coarse one, so no single test compares the two surfaces (E2E-D: component tests that can't see each other).
- **Instances:** 2025 — Explorer reader showed `describe.Node.NodeKind` (coarse `node_kind`) so an azure-resource read "knowledge"; the overview + category filter used `has_type` and were correct — the two surfaces disagreed.
- **Control:** when two fields name overlapping concepts at different granularity, name them distinctly (`Type` vs `Class`), and the surface that a user reads chooses the fine one unless the coarse one is explicitly what's asked. Design fix: reader prefers the `has_type` edge over the coarse `node_kind`. Root fix (Core): the extractor should not emit `knowledge` for extracted source, and neighbours should carry their real `has_type`, not a hardcoded `"source"`.
- **Status:** `partially-controlled` — reader fixed in Design; extractor/neighbour labels handed to Core (INV-0004).

### DC-042 — A capability is complete, tested, and nothing ever routes work to it

- **Shape:** a producer is written, unit-tested and wired into a composition — and the thing that
  DISCOVERS work for it was never taught to. Every test passes, because tests hand it input directly.
  On real input it is unreachable, and the surface that reports its output shows a legitimate-looking
  **zero**.
- **Signature:** a count that is exactly zero on every real repository while a sibling count is large.
  The tell in code is a router keyed on something — a scope prefix, a file kind, a MIME type — whose
  keys are produced by a DIFFERENT component, and nobody has compared the two lists. Ask: *what
  produces the keys this router matches on, and does it produce this one?*
- **Why it survives:** it passes the strongest evidence a team usually has. Unit tests construct the
  input, so the producer is proven correct. Integration tests use fixtures that name the scope
  explicitly, so routing is proven correct. Only DISCOVERY is untested against reality, and its gap
  is invisible from either side — the producer is not broken and the router is not broken. And the
  zero is worse than an error: an error gets investigated, a zero gets believed.
- **Instances:** 2026-08-30 — reported by the user: *"the graph was showing knowledge as zero count
  and code as a large count."* `FixtureExtractor` had read knowledge frontmatter since Phase 1 with
  tests; `CompositeExtractor` had a fallback route; and `CSharpScopeDiscovery` produced six scope
  kinds — `csharp`, `bicep`, `schema`, `python`, `typescript`, `sql` — and no knowledge scope. The
  reader was correct, tested, and unreachable on every real repository for the entire life of the
  project. MEASURED after wiring discovery: on this repo, 466 `owned_by`, 346 `refines`, 287
  `implements`, 272 `relates-to`, 66 `depends-on`; scopes across three repositories went 28→66,
  34→48, 34→56.
- **Instance, 2026-09-01 — the recurrence the residual risk predicted (the canvas categoriser).**
  The user opened the build and the **Knowledge** category chip read **0** while the status panel's own
  boundary notes reported **39 knowledge scopes, 4,471 headings, 3 glossary documents**. Root cause:
  Core had widened `GraphNode` with an authoritative `IsKnowledge` flag *specifically* to stop the chip
  guessing at type spellings (`GraphProjection.cs:22-36`, itself citing a measured 2,343-knowledge-node
  repo whose kinds were `spec`/`knowledge-epl-fan-platform`), but the App **drops the flag at the
  `CanvasNode` boundary** (`CanvasGraphViewModel.cs:20/156`) and still categorises by spelling the fine
  `kind` (`CanvasPage.cs:156-165, 661`) against a fixed list. This is DC-042's **named-but-uncontrolled
  residual risk** — *"the canvas's node kinds are all keyed this way; only extraction routing is
  asserted so far"* — recurring exactly where it was written down. **The control was too narrow:**
  `RoutedKinds` asserts the *extractor* routing but nothing asserts the *canvas categoriser* consumes a
  producer-declared signal, and the fix was cross-session — the producer's half (the flag) shipped, the
  consumer's half (using it) never did, so both sides passed their own tests. A secondary compounding
  cause: the 1,500-node degree cap starves low-degree knowledge out of the counted view. Full analysis:
  `docs/investigations/knowledge-chip-reads-zero-again.md`. **Control to extend:** a canvas analogue of
  `RoutedKinds` — a test that the categoriser is driven by producer signals (`IsKnowledge`/`IsExternal`/
  the `has_type` families) and that every advertised chip category has a signal the App consumes; plus
  sourcing the chip *count* from a workspace aggregate rather than the capped payload. **Phase 1
  landed 2026-09-01** (`knowledge-chip-isknowledge`): `CanvasNode` now carries `IsKnowledge`,
  `WholeGraphAsync`/the group path propagate `GraphNode.IsKnowledge`, the payload serialises it, and
  `CanvasPage.categoryOf(kind, isKnowledge)` prefers the authoritative flag over spelling (specs keep
  their own chip). Control observed failing then passing:
  `CanvasGraphViewModelTests.WholeGraph_CarriesTheIsKnowledgeFlag_SoTheChipDoesNotSpellTheKind` (red on
  the un-wired mapping, green after). **Residual:** the drill-down (describe) neighbour path and the
  aggregate-count fix (Phase 3, Core) are not yet done, so a capped view can still under-count
  low-degree knowledge; the JS categoriser has no direct unit harness.
- **The sharpest part of it:** this happened on a repository whose stated premise is that *docs hold
  intent, code holds reality, and the expensive defects live in the gap*. Half of that sentence was
  never being read, and the product said so with a zero.
- **Control:** `WorkspaceExtractors.RoutedKinds` is asserted against what discovery emits, so a route
  with no producer — or a producer with no route — fails a test rather than reporting nothing.
  `KnowledgeExtractorTests` covers the reader on real-shaped documents, and the multi-repository
  harness records scope counts per kind so a kind that silently stops being discovered shows up as a
  drop in `git diff`.
- **The generalisation to apply elsewhere:** for every consumer keyed on a producer's vocabulary,
  **compare the two lists in a test rather than in your head**. And treat a zero on a real
  repository as a question, never as an answer: the useful form of the question is *"is this zero
  because there is none, or because nobody looked?"* — which is the same question this product asks
  about evidence, turned on the product itself.
- **Residual risk:** the same shape exists wherever a router matches on strings someone else emits.
  The IPC operation names, the join projection's predicates and the canvas's node kinds are all
  keyed this way; only extraction routing is asserted so far.
- **Status:** `partially-controlled`


- **Instance, 2026-08-31 — the compaction check.** `WorkspaceCore.CheckCompactionNeeded` was complete, tested, and called by nothing: no shell, no daemon, no command. A workspace could pass the generation threshold, slow measurably past its refresh budget, and the diagnosis would sit in a method nobody invoked. Found while answering "why is the store growing", not by any test. MEASURED on the user's workspace: 2 generations per scope against a threshold of 8 — under the trigger, and yet **half the store (23,672 of 47,809 assertions) was already superseded**, because the threshold is tuned for LATENCY and the symptom people see is SIZE. The daemon now calls it at startup, which is the moment the store is open, no session is in progress, and an operator is looking. **The residual is the threshold itself:** nothing yet triggers on size, and reclaiming space needs `retain: 1`, which drops the diagnostic history the default keeps. That is a decision, not an oversight, and it is still open.

### DC-043 — A second construction of a view-model omits configuration the first applied
- **Signature:** the same data is shown by two surfaces built from the same view-model type, but one construction sets a configuration the other omits, so a DERIVED property (here: node colour, computed from a node's context) silently differs — one surface is right, the other is subtly wrong, and both pass their own tests.
- **Why it survives:** the data is identical (same query, same counts, same disclosures) so a data test sees no difference; only a rendered, cross-surface comparison reveals it. Sibling of DC-040 (a retained component reading a dependency the shared one had) — the fix pattern is the same: share the configuration, or read it live in both places.
- **Instances:** 2025 — the Explorer graph rendered all-grey while the workbench graph was coloured, because `CreateExplorerGraph` built a fresh `CanvasGraphViewModel` without wiring `ContextLookup` (colour comes from context; default lookup returns null). Workbench VM set it (WorkbenchShell:774); Explorer VM did not (WorkbenchShell:810).
- **Control:** extract the shared configuration into one helper both constructions call (`BuildContextLookup`), read live so a workspace change is reflected. When two surfaces show the same data, assert they agree on the derived surface, not just the data (E2E-D / E12 cross-surface consistency).
- **Status:** `controlled` — both paths now call `BuildContextLookup`; App.Tests + launch smoke green.

### DC-044 — Two guards answer one question and only one is taught about a new input
- **Signature:** a decision is protected by two independent checks written at different times for the same question ("is the cached answer still good?"). A new input arrives — a version, a generation, a schema stamp — and is wired into **one** of them. The other keeps answering from what it already knew, and its answer wins, because the two are in series and the narrower one runs last. Nothing fails: the run completes, reports success, and does no work.
- **Why it survives:** each guard is individually correct and individually tested. The invalidation mechanism was *proven* to work — the fingerprint really did change, the sidecar really was rejected — so the evidence all pointed at a mechanism that was doing its job. The failure only exists in the composition, which nothing owned. A gate that asserts the new input is *present* (here `verify-extractor-generation.py`, which checks the constant was bumped) tests the input, never that the input *reaches an outcome* — so a green gate was evidence the bump happened, not that it did anything (E-series: a gate's green result is evidence the gate passed, not that its contents passed).
- **Instance:** 2026-08-30. `ScopeFingerprints.ExtractorGeneration` was bumped so an upgrade would re-extract every scope, and it correctly invalidated the sidecar. `WorkspaceCore.RefreshScopeAsync` then applied its own reuse check — *does the store already hold this artifact revision?* — which knew nothing about the generation, matched on the unchanged `rev-1`, and returned an empty result. MEASURED in the user's own store: the C# scopes were last extracted **2026-08-28T23:50**, unchanged across five extractor changes shipped since, while the knowledge and TypeScript scopes (which had no prior snapshot, so no second guard to defeat) extracted normally at 2026-08-31T00:20. The user saw *"Indexed 66 of 66 scope(s): 0 assertion(s)"* — a run that visited everything and wrote nothing.
- **The deeper cause, and why deleting the guard would not have been the fix:** the natural key is `(scope_id, artifact_revision, subject, predicate, object, extractor_id)` — P1-STORE-05, *one revision, one answer*. True while the extractor was fixed; false the moment extraction could improve for input that had not changed. Had the guard been removed, every unchanged fact would have collided with the unique index, because the key genuinely could not represent *the same bytes read by a better reader*. The guard was the symptom; the **grain of the key** was the cause (DM: declare the grain before the columns).
- **Control:** `SourceRevision` makes the reader part of a fact's stored identity, applied inside `RefreshScopeAsync` so every entry point — shell, daemon refresh op, test — gets one answer instead of three. `UpgradingTheExtractorReExtractsTests` asserts the *outcome*: a store written by an older build gains the new facts, and an unchanged re-index still writes nothing. Observed failing on the un-fixed code (2 of 4). The generation is also its own telemetry axis (`extractor.generation`), so "which reader built this graph" is measurable rather than inferred.
- **The generalisation to apply elsewhere:** when adding an input to a staleness or cache decision, **find every guard on the path and check the input reaches the last one**, because the narrowest guard wins. And assert the *outcome* — new facts arrive — never that the mechanism changed.
- **Residual risk:** the stamp lives in the revision string rather than its own column (marked `simplify:` with its upgrade trigger). Anything rendering a stored revision must call `SourceRevision.Base` first; three read paths do today, and a fourth added later would show the stamp to a user before anyone noticed.
- **Status:** `controlled`

### DC-045 — The write succeeds, the screen keeps the old answer, and both halves report success
- **Signature:** a command changes what the store holds and completes normally. Every open surface goes on rendering the projection it fetched when it loaded. The command's own report is accurate, each pane's content is internally consistent, and the only thing wrong is that they describe different moments. The user reads the stale number *as the result of the action they just took*, which is worse than an error — it is a confident wrong answer with a success message attached.
- **Why it survives:** every component passes its own tests, because every component is correct. The defect is in the seam, and a seam has no owner by default. It is invisible to unit tests (each side is right), to integration tests that assert against the store (the store is right), and to a render test that loads a pane fresh (loading is the case that works). It needs a test of the *sequence* — change, then look — which is the one nobody writes because both halves are known good.
- **Instance:** 2026-08-30. A re-index of TheTerrace wrote all 38 knowledge scopes — 10,242 assertions, 2,343 `node_class` facts, 2,502 knowledge nodes — committed at 17:20:24 local. A screenshot at 17:20:50, twenty-six seconds later, showed the graph's Knowledge chip reading **0**, with a node total (1,996) matching the pre-index projection exactly. `IndexSolution` announced its outcome and told nothing else. Diagnosed by timestamp, not by inspection: the store proved itself correct, and the current build's own projection returned 236 knowledge nodes over that same store.
- **The trap in the diagnosis:** the visible symptom (a zero) was the same symptom as two earlier defects with real causes — knowledge never extracted, and a cap applied before a filter. Both had been fixed. Assuming a third cause in the same layer would have cost a day; reading the commit timestamps cost one query. **When a symptom recurs after its cause was fixed, date the evidence before re-opening the diagnosis.**
- **Control:** `WorkbenchController.WorkspaceDataChanged`, raised after a command that changed the store and **not** after one that failed; the shell re-reads whatever panes the layout currently holds. `IndexingReachesOpenPanesTests` covers all three commands plus the failure path. Observed failing on the un-fixed code (3 of 4, with the failure-path test correctly staying green).
- **The generalisation to apply elsewhere:** for any command that writes, name the surfaces that are showing what it wrote, and make the freshness of those surfaces part of the command's definition of done. A write path that ends at an announcement is not finished — **the last mile of a write is the screen**.
- **Residual risk:** the signal is raised by the *controller*, so a write reaching the store by another route (the daemon indexing on its own, a second client) does not raise it. Panes are told about writes this shell commanded, not about the store changing.
- **Status:** `controlled`

### DC-046 — The layout that was tested was never the layout that shipped
- **Signature:** code resolves a sibling file by a path relative to itself. A build-time step puts the file there, so every developer run, every test and every local launch is correct. A *different* packaging step — publish, installer, container copy — produces a different arrangement, and the resolution fails only in the artifact users receive. The error message describes the failed operation, not the missing file, so the investigation starts in the wrong place.
- **Why it survives:** every test runs against the developer layout, which is the one that works. There is no failing test to write without first building the artifact, and building the artifact is the step nobody does in a test. The gap is invisible to code review because both halves of the code are right: the resolver looks in a sensible place, and a copy step really does put it there.
- **Instance:** 2026-08-30. `MainWindowViewModel.DaemonPath()` resolves `<BaseDirectory>/daemon/AiDe.Daemon.exe`. `CopyDaemonBesideShell` (`AfterTargets="Build"`) wrote it to `$(OutDir)daemon\`; `dotnet publish` writes to `$(PublishDir)` and does not carry that across, so `artifacts/app` shipped with `AiDe.Daemon.exe` flat at the root and nothing at `daemon/`. Every published build could open **no workspace at all**, reporting *"This workspace could not be opened"* — a message about the workspace. Found while publishing at the end of an unrelated fix, not by any test.
- **The uncomfortable part:** this session had reported "published `artifacts/app`" at the close of many turns. The publish command succeeded every time. **An exit code is not a result** (E-series) — the artifact was produced and was not usable, and nothing in the routine looked at what came out.
- **Control:** `verify-published-layout.py` publishes the shell to a scratch directory and asserts the daemon is at the path read **from the source** — so renaming the folder in one place fails the gate rather than a user's first click. Observed failing on the un-fixed build, with the diagnosis it prints ("it is in the wrong place, not missing") written from that run. In CI as *Published layout gate*.
- **The generalisation to apply elsewhere:** for anything resolved by a path relative to the running binary, **assert it in the artifact, not in the build output**. And when a routine ends in "produced X", make the last step read X.
- **Residual risk:** the gate checks the one path that is currently resolved this way. Another sibling resolved relative to `BaseDirectory` later would need adding; nothing detects a new one automatically.
- **Status:** `controlled`

### DC-047 — The budget is checked on one side of an encoding and enforced on the other
- **Signature:** a payload is measured against a transport limit, and between the measurement and the transport it is **encoded again** — escaped into a string field, compressed, base64'd, wrapped. The check passes on the inner bytes; the limit applies to the outer ones. Every test agrees with the guard because every test measures what the guard measures, so the guard and its tests are wrong together and consistently.
- **Why it survives:** the budget looks conservative. `768 * 1024` against a `1024 * 1024` frame reads as *three quarters of the limit, a quarter of headroom* — a number nobody re-examines, because the arithmetic is visibly cautious. Nothing in it says which bytes it counts. The estimator was even validated (`actual/estimate ≈ 0.93`, conservative), which is the trap: the estimator was accurate about the **inner** payload and that was never the quantity at risk.
- **Instance:** 2026-08-30. `IpcResponse.Payload` is a `string`, so a projection is serialised to JSON and that JSON *text* is then carried as a field in the envelope, escaping every quote. MEASURED across every payload on a real workspace, the inflation was **1.56–1.57x**. A 727,244-byte graph — inside its 768 KiB budget — reached **1,137,104 bytes** on the wire and was refused. The user saw only *"The graph could not be loaded: ipc.payload_too_large: the response is 1,176,341 bytes"* on opening a workspace.
- **Two things it hid.** The shrink-to-fit path applied **one** proportional correction and returned without re-checking, so even where it ran it could fall short. And the same budget governs the evidence and find pages, which were within one measured page (652,425 bytes → ~1.02 MB framed) of the same failure without anyone noticing.
- **Control:** the graph now measures `FramedCost` — the payload serialised, escaped and enveloped exactly as the transport does it — and shrinks until that fits, with 64 KiB of headroom because shrinking stops at the first size that fits (measured with none: 1,044,916 against a 1,048,576 frame, one long type name from failing). Row-wise bounds cannot afford a per-row serialisation, so they keep a factor — and `TheBudgetFitsTheFrameTests` asserts `MaxResponseBytes * 2 <= FrameBytes`, so the assumed worst case cannot drift past what a frame holds. `TheGraphAlwaysFitsInAFrameTests` measures the framed bytes on a hub-shaped store; observed failing on the un-fixed code (3 of 5), reproducing the user's number to within 1%.
- **Calibrating the fixture was the hard half.** Three fixtures passed against the un-fixed code before one reproduced it: too large and the old code shrank it to safety, too small and it never approached the frame. The discriminating window — an inner payload between 686 KB and 718 KB — was found by **measuring the fixture**, not by choosing numbers that looked big. A control that cannot fail is worse than none, because it certifies (DC-016).
- **The generalisation to apply elsewhere:** measure the artifact **at the boundary that rejects it**, in the form it has when it gets there. Where the exact measurement is too expensive, write the assumed ratio down as an assertion between the two constants rather than as headroom in one of them.
- **Closed at the cause, same night.** The factor managed the encoding rather than removing it, so the `simplify:` marker on `MaxResponseBytes` named its own upgrade trigger — *an envelope carrying raw JSON* — and that trigger was then pulled: IPC version 3 carries the payload as JSON instead of as a string holding JSON text. MEASURED after: framing overhead fell from **1.57x to 78 bytes**, and the canvas's own request on the real workspace went from 1,000 nodes and 283 knowledge to **1,500 and 340**. `IpcPayload.Read` accepts either encoding so a version-2 peer is still understood, and `ThePayloadIsNotEncodedTwice` fails if string-carried JSON returns — at the seam, not at a user opening a workspace. A marker whose trigger has fired is a defect with a date on it, and this one was three hours old.
- **Residual risk:** none for the encoding. The shrink's overshoot became its own class (**DC-048**).
- **Status:** `controlled`

### DC-048 — The margin that guarantees termination becomes the answer
- **Signature:** a correction loop narrows on a constraint — shrink until it fits, back off until it succeeds, reduce until it is under budget — and each round is forced to take a *minimum* step so the loop cannot stall. That floor is a termination argument, and nothing else. But the first value that satisfies the constraint is **returned**, so the size of the safety step silently becomes the size of the result. The bigger the safety margin, the worse the answer, and the relationship is invisible from either the loop or the constraint.
- **Why it survives:** the loop is correct on every property anyone thought to state. It terminates, it never returns something over budget, it has a bounded cost. The defect is in a property nobody wrote down — *the answer should be as large as the constraint allows* — and it hides behind the property that was written down, because both are satisfied by "it fits".
- **Instance:** 2026-08-30. The graph shrinks until it fits a frame, cutting **at least a third** each round so the loop terminates even where bytes barely move with node count. MEASURED on the real workspace: asking for 5,000 nodes returned **706** while asking for 1,500 returned **1,000**. A caller who asked for more was served less, and the smaller answer was indistinguishable from a smaller workspace. On the calibrated fixture the same shape gives 868 against 1,281.
- **The tell:** an answer that moves in a direction the request cannot explain. Any parameter where "ask for more, get less" is possible is this class or its sibling.
- **Control:** after the loop, up to four probes at the midpoint of (fits, does-not-fit), each accepted only if it also fits — so recovery can widen the answer and never break it. MEASURED: none 868, two 1,193, four 1,274, six 1,274 again, against 1,281 available. `AskingForMoreNeverReturnsFewer` asserts a larger request never returns materially fewer, where *materially* is `MinRecoveryGap` — the precision recovery actually offers, named rather than guessed. Observed failing without recovery (868 against 1,000, eight times the gap). The fixture was calibrated by measurement: lighter shapes never shrink far enough to invert, and one that cannot invert cannot catch this (DC-016).
- **The generalisation to apply elsewhere:** when a loop's step size exists to guarantee progress, **do not let the value it lands on be the value you return.** Bracket, then search the bracket. And state the property the constraint does not: not only "the answer is legal" but "the answer is the best legal one", because only the first is checked by fitting.
- **Residual risk:** monotonicity here is an approximation with a named precision, not a guarantee. It is exact only if the largest fitting size is *found* rather than approached, and that is affordable only once the node ORDERING is computed once and candidate sizes evaluated against it — today every probe redoes work that cannot change. Recorded on `MinRecoveryGap`.
- **Status:** `partially-controlled`

### DC-049 — A launched process decides where to write, so a caller cannot stop it
- **Signature:** a component derives its own state location from a machine-wide place — an app-data folder, a home directory, a registry key — and exposes no way for a caller to say otherwise. Everything that starts it therefore writes into the user's real profile, including tests, which believe they are isolated because *their own* files are in a temp directory. Nothing fails. The residue is invisible until somebody counts it.
- **Why it survives:** every test passes, and each one is individually reasonable — a temp workspace, a real daemon, a clean-up of the temp workspace afterwards. The leak is in a path no test names, created by a process no test looks inside. Test isolation is normally verified by *what a test asserts*, and nothing asserts about a directory the test never mentions.
- **Instance:** 2026-08-30. `AiDe.Daemon` computed `LocalAppData/AiDe/workspaces/<id>` for itself. MEASURED: **12** directories per run of the Core suite, and **2,695** accumulated over four days — all but one an empty or fixture-sized store from a test that had finished long before, 468 MB in total. The one real workspace was the user's. Found while investigating "why are there 2,399 of these", not by any check.
- **It also removed a duplicated derivation.** The shell computed exactly the same path independently, so two expressions produced one value and agreed only for as long as nobody edited one of them (DC-022). The shell now passes the directory it already has.
- **Control:** the daemon takes `--data`, and `ShellBootstrap.ConnectOrLaunchAsync` passes it through. `ADaemonToldWhereToKeepItsState_WritesNowhereElse` snapshots the machine-wide directory, launches a daemon with an explicit one, and asserts nothing new appeared — an assertion about the directory that must stay untouched, because an assertion about the one that must be written would have passed all along. Observed failing with the option removed. MEASURED after: **0** leaked per full-suite run, down from 12.
- **A second lesson, from building the cleanup tool.** `list-workspace-stores.py` opened each store read-only to count its facts, and SQLite created a `-wal` and a `-shm` beside every one: **5,390 files, two per store.** On its second run those files were the difference — 1,495 directories that had held nothing but a store now held three, and the tool reported every one of them as in use. **A read that writes is not a read**, and a measurement whose own footprint changes the next measurement will always converge on a wrong answer. It now opens `immutable=1` where there is no write-ahead log to miss, and counts a store's sidecars as part of the store.
- **The generalisation to apply elsewhere:** any component that writes outside the directory it was pointed at should take that directory as an argument. And when a suite launches a real process, assert about **where it wrote**, not only about what it answered.
- **Residual risk:** the 2,695 directories already written are still there. The tool reports them and removes only the provably empty ones, because an id is a one-way hash of a path — "not in the recent list" is not proof a workspace is gone, only that nothing can name it.
- **Status:** `controlled`

### DC-050 — A disclosure conflates a boundary with a gap, and the plan follows the wrong one
- **Signature:** a reader honestly reports what it could not resolve, and the report merges two different things: what the product **does not intend to read** (a runtime, a third-party package, a generated tree) and what it **meant to read and could not**. Both are "unresolved", so both are counted together. The number is arithmetically correct and describes something that does not exist — and because it is the largest number in the report, it becomes the top of somebody's list.
- **Why it survives:** every check passes. The count is right, the disclosure fires only when there is something to disclose, and the wording is literally true — *"names something this scope does not contain"* is a true sentence about `import sys`. Nothing is wrong until a person reads it as a priority, and by then the cost has already been paid in planning rather than in code.
- **Instance:** 2026-08-31. Python disclosed `python-imports-not-resolved (246 import(s) name something this scope does not contain)` on TheTerrace. I ranked it **the largest coverage gap in any built extractor** and put it top of a priority list — on the strength of the number alone. Measuring the targets took one query: **all 246, across all 32 distinct names, were the standard library** — `sys`, `pathlib`, `json`, `argparse`, `os`, `subprocess`, `urllib`. After teaching the extractor the difference: **2 genuine unknowns** (`coord_ids`, `bounded_process`). The gap was 1% of what the number said.
- **It was hiding the real signal, too.** Two unidentifiable imports inside a count of 246 are invisible. Separating the two made a number nobody could act on into a number somebody can fix in an afternoon.
- **The second half, one layer along.** The standard library was also being DRAWN — 226 edges, putting `sys`, `os`, `json` and `re` among the most connected nodes in the graph. The C# extractor had already declined to draw the BCL, with the reason written down: *"a first view centred on the BCL is not a picture of anybody's domain."* The same reasoning had simply never been applied to Python. **A principle recorded in one reader is not a principle the codebase holds.**
- **And the fix's own filter was wrong.** The standard-library set was generated from `sys.stdlib_module_names` — correct — and filtered to drop "private names", which dropped `__future__`: the one module in the set that looks private and is imported constantly. 26 false unknowns, caught by measuring again after the fix rather than by assuming a generated list must be right.
- **Control:** `PythonStandardLibrary`, generated from the interpreter rather than remembered, and `PythonImportBoundaryTests` pinning all three outcomes — resolved in-repo, standard library, genuinely unknown — plus that the standard library is counted and not drawn. The rule is written into `docs/plans/extractor-roadmap.md` as a standing rule for any extractor added later.
- **The generalisation to apply elsewhere:** **a disclosure is a planning input, so it has to distinguish "will not" from "cannot".** Before acting on any count of unknowns, look at the unknowns — the query is cheap and the alternative is a session spent on a hole that is not there. And when a reader records a principle in its own comments, ask which other readers should be holding it.
- **The residual was measured the next day, and it was wrong.** This entry said TypeScript's unresolved specifiers "are probably npm packages — probably, which is exactly the word this class is about". It was: **2 of the 12 were anything at all, and both were Node builtins**. The other 10 were invented (see DC-033). So the TypeScript import gap was **83% invention, 17% boundary, 0% coverage hole** — a register entry hedging correctly about its uncertainty and still landing on the wrong shape. **A `probably` in a register entry is a task, not a caveat**; this one sat for a day and would have sent the next session looking for packages that were not there.
- **The same fix applied, from the runtime's own answer.** `NodeBuiltinModules` is generated from `require('module').builtinModules` on Node v24.18.0, mirroring `PythonStandardLibrary`. It distinguishes the 42 bare-importable builtins from the three reachable only behind `node:` (`test`, `sqlite`, `sea`) — so a bare `test` is correctly an npm package, not Node's test runner. Builtins and packages are counted, never drawn. After: TheTerrace has 2 builtins, 0 packages, **0 genuine unknowns**.
- **Residual risk:** `react` on this repository is reported as a genuine unknown and is almost certainly npm. `package.json` `dependencies` would settle it without guessing; neither repository has one outside build output. Named as the upgrade trigger in the code rather than guessed at here.
- **Status:** `controlled`

### DC-052 — A bound is deterministic and orders by the wrong thing
- **Signature:** a read is capped, and the cap is applied after an ordering chosen for **determinism** rather than for **importance** — alphabetical, insertion order, id order. Every property anyone thought to check holds: the same query returns the same rows, the omission count is honest, nothing is invented. What is missing is that the rows which survive were selected by a property of the *names*, so which facts a caller sees depends on how the thing happened to be called.
- **Why it survives:** determinism is the property a bounded read is usually reviewed for, and this has it. The cap is disclosed and the count is right. It fails only for items that exceed the cap, which are the minority — and they are the most connected, most important ones, so the failure is concentrated exactly where it costs most and is rarest in a fixture.
- **Instance:** 2026-08-31. `AssertionsTouching` capped at 50 ordered `subject, predicate, object`. A node with more facts than that lost its own `has_type`, `node_class`, `owned_by` and `review_by` to its own links, in alphabetical order. MEASURED: 12 of 877 knowledge documents were already over the ceiling before anything was added to them; simulating headings put `adr-0015-erasure-ledger-durable-model` at 44 headings and none of its identity. It is also why the knowledge reader correctly declined to emit headings — the extractor was working around a defect one layer down.
- **Its sibling was found the same day, one caller along.** The node-content reader filtered this same capped list for a node's declaring fact, so `AppDbContext` — 244 edges, callers named `TheTerrace.Features.*` sorting before it — reported "no recorded source" while every small type worked. That was fixed with a dedicated query (DC-035's fourth instance). Two failures, one cause: **a window ordered by names, read by callers who wanted meaning.**
- **Control:** `EvidencePredicates.Identity` — a deliberately small set of facts that say what a node IS — sorts first, then the node's own outbound facts, then inbound; alphabetical within each band, so determinism is untouched and the omission count still means what it says. Deliberately NOT "all attributes": `has_member` is an attribute and a type can carry forty, which would replace one flood with another. `ABoundedDescribeKeepsIdentityTests` builds a node whose identity sorts *after* its links and exceeds the cap — a fixture whose identity happened to sort first would pass against the unfixed reader and prove nothing. Observed failing: *"'has_type' fell outside a 50-row window on a node with 105 facts — the caller cannot tell what this node even is"*.
- **The generalisation to apply elsewhere:** **a cap needs a ranking, and determinism is not one.** For every bounded read, ask what the caller would keep if it could only keep three rows, and put those first. Alphabetical order is a tiebreaker, not a priority.
- **Residual risk:** the bands are coarse. Within "the node's own facts" a type with forty members still competes with its relations on name order, so a member-heavy type can still push a relation out. Measured as acceptable because members are attributes and the graph does not draw them; it would stop being acceptable if a surface started reading relations from this window expecting completeness.
- **Status:** `controlled`

### DC-051 — A fix for real duplication silently pays for it in resolution
- **Signature:** two scopes overlap, so the same input is processed twice and every derived fact is stored more than once. The obvious fix is to stop the overlap — give each scope only its own inputs. It works, and it quietly removes the *context* the wider scope was providing: anything that resolved a reference by looking across the overlap can no longer see the other side. The duplication metric improves, a different capability degrades, and nothing connects the two numbers.
- **Why it survives:** the fix is measured, and measured against the thing it set out to fix. Storage falls, counts become correct, no inputs are lost — every check the author thought to run passes. The regression is in a feature the author was not looking at, and it is only visible if the *outputs* are compared rather than the inputs.
- **Instance:** 2026-08-31. Knowledge scopes nest — `knowledge:docs` walks everything beneath it and `knowledge:docs/adr` walks it again — so every knowledge fact was stored **~2.7 times**: 2,368 `node_class` rows for **877** distinct documents. The roadmap's own "2,359 documents" was that inflated number, repeated as a document count. Making the walk non-recursive fixed it exactly: **877 documents preserved**, knowledge facts 10,508 → 4,326.
- **And it cost 30 of 42 prose-link edges**, delivered hours earlier. A markdown link from one directory to another only resolves for a scope that read both, and the recursive parent had been the only thing reading both. The de-duplication metric was perfect and a feature lost 71% of its output.
- **Caught by comparing outputs, not inputs.** The document count was the safety check and it passed. `links_to` was checked only because it was new enough to still be in mind. **The rule that generalises: when a change is justified by one number, name the number that would get worse if the change were wrong, and read it too.**
- **Resolution: reverted, not shipped.** The compensating fix — resolve link targets against the whole workspace while emitting facts only for the scope's own files — contradicts a deliberate design decision in the reader (`a link above the scope is its own boundary`) and the tests written for it. That is a redesign, not an integration, and shipping a 71% regression to reach it would have been the worse trade. Both halves are now item 1 on the extractor roadmap, together, with the measurement attached.
- **The generalisation to apply elsewhere:** deduplication and resolution pull in opposite directions wherever scopes overlap. Before removing an overlap, ask what was using it — and prefer splitting the two jobs (read widely, emit narrowly) over choosing between them.
- **Status:** `partially-controlled` — the trade-off is measured and recorded; neither half is fixed.

### DC-053 — A worktree isolates the working tree, not everything a command touches
- **Signature:** work is split across git worktrees precisely so two sessions cannot collide — separate working trees, separate indexes, separate HEADs. Then one of them uses a command whose state lives in the **shared** `.git` rather than in the worktree, and the isolation the whole arrangement was built on silently does not apply to that one operation. Nothing errors. The command does exactly what it is documented to do, on a stack that belongs to everybody.
- **Why it survives:** the isolation is real for everything anyone thinks to test. Branches, indexes, HEADs and untracked files are all per-worktree, so the mental model "my worktree is mine" is correct almost everywhere — and it is reinforced every time it works. `refs/stash` is a single ref in the common directory. So is `refs/bisect`, so are `MERGE_HEAD`-style operations for a given worktree, and so are notes, config and hooks.
- **Instance:** 2026-08-31. Two agents ran concurrently in `ai-de-knowledge-dedup` and `ai-de-csharp-calls`, both branched from the same commit, both under instructions that named worktree isolation as the reason they could work in parallel. One stashed its `src` changes to take before/after timings; the other stashed at almost the same moment with a colliding WIP message. The first `stash pop` restored **the other session's** 387-line `CSharpExtractor.cs` change into the wrong worktree, and the second pop took the first's knowledge work.
- **Both agents caught it, and that is the only reason this is a near miss.** One recovered its source from a copy it had taken before its red runs and re-verified every measured number byte-identically afterwards; the other pushed the foreign change **back onto the stack** with an explicit message — `RESTORED by session/csharp-calls: another worktree's stash, popped here by accident (shared refs/stash)` — rather than discarding what it did not recognise. Verified at integration: nothing was lost, and the stash on the stack was a redundant copy of work already committed.
- **The rule:** **`git stash` is a repository-global stack and is therefore not a worktree-local tool.** For a temporary revert while a sibling session is live, copy the file. The pack's worktree discipline (WT1–WT12) says a session gets its own tree so two agents cannot share an index — it does not yet say which commands escape that boundary, and this is the list to start: `stash`, `bisect`, notes, config, hooks.
- **The generalisation to apply elsewhere:** when isolation is the reason two things may run at once, **enumerate what the isolation does not cover** before relying on it. "Separate working directories" is a statement about files, not about every piece of state a tool keeps.
- **Control:** **WT13**, added to `.claude/knowledge/session-worktree-discipline.md` — the always-loaded rule, where a session reads it before opening a worktree rather than after colliding in one — plus a line in the self-verification checklist. Not mechanisable: nothing can stop a subprocess calling `git stash`, which is why it has to be a rule and why both agents preserving what they did not recognise is the behaviour worth keeping.
- **Residual risk:** WT13 names `refs/stash` sharply and the rest of the shared directory generally (`refs/bisect`, notes, config, hooks). A session that meets a different piece of shared state will not find it listed — the rule it will find is the generalisation: enumerate what the isolation does not cover before relying on it.
- **Status:** `partially-controlled`

### DC-054 — A new pane placed into the focused stack hides the surface that stack already held

- **Signature:** "I added X and Y disappeared." A new surface is added as a *tab* into an existing
  stack (`AddSurface(focusedStackId, …)`), so it lands on top of whatever tab that stack already
  showed and hides it. Here: adding a class diagram while focused on the graph tabbed the diagram
  into `stack-graph` (the default layout stacks the canvas + a document surface together), so the
  graph became a background tab and read as "gone."
- **Why it survives:** the add succeeds, the model is valid, the surface set is correct, and no test
  fails — the lost surface is *present*, just not *visible*. Focus-aware placement (a fix for an
  earlier "it opened in the wrong window" report) made it worse by targeting the focused stack, which
  is often the graph's.
- **Instances:** 2026-08-31 — class diagram added on top of the graph. Fixed by
  `DocumentPlacementPolicy`: a reference document tabs into a document stack, else splits BESIDE the
  graph so both stay visible; never onto the canvas stack.
- **Control:** `DocumentPlacementTests` — `OpeningAClassDiagram_KeepsTheGraphVisibleInItsOwnStack`
  asserts the graph and the diagram end in DIFFERENT stacks. Plus `WorkbenchDiagnostics`
  (`aide.workbench` ActivitySource + a JSON layout log) so a future placement report is traceable —
  the workbench previously had no instrumentation at all.
- **Status:** `controlled`

### DC-055 — A one-line status region has no cap, so a long message eats the layout

- **Signature:** a status strip / live region is a wrapping TextBlock in an Auto-height row with no
  height cap or truncation. A normally-short channel occasionally carries a long message (a re-index
  announcement is one sentence + 200+ analysis-boundary disclosures, ~5,000 chars), the row grows to
  fit it, and it squeezes the real content to a sliver. Here it ate ~70% of the window.
- **Why it survives:** every test and demo used a short status message; the "very long message" state
  was never designed. The layout is valid; it is just catastrophically proportioned for one input.
- **Instances:** 2026-08-31 — re-index diagnostics wall. Fixed: LiveRegion is single-line
  (NoWrap + CharacterEllipsis); the full text is on hover (tooltip) and still read by AT.
- **Control:** `WorkbenchAnnouncerTests` (long message → tooltip carries it; short → none). A status
  region is one line by construction.
- **Status:** `controlled`

### DC-056 — A re-render helper mutates the caller's list, so a note duplicates per render

- **Signature:** a render builds a `notes`/display list, a member/data prefetch re-renders with the
  SAME list instance, and each render inserts its own note (`"Showing N of M"`) → the note appears
  once per render (2×, 3×…). Introduced by the variable-height prefetch-then-rerender.
- **Why it survives:** a single render is correct; the duplication only appears after an async
  prefetch re-render, which unit tests without a members source never exercised.
- **Instances:** 2026-08-31 — class diagram "Showing the 40 most-connected…" shown twice. Fixed: the
  render builds a private DISPLAY copy for the disclosure and passes the PRISTINE notes to the
  prefetch, so each render starts from the same base.
- **Control:** `ShowGraph_WithMembersSource_DoesNotDuplicateTheTruncationNote_AcrossThePrefetchRerender`.
- **Status:** `controlled`

### DC-057 — A per-workspace layout restore faithfully brings back a degenerate saved state

- **Signature:** opening a workspace restores its saved `layout.json` over the current arrangement
  (US-9). When the saved layout is degenerate — e.g. it had lost the primary graph pane — the restore
  brings back the broken, scattered arrangement, and the user reads "opening the workspace reset my
  panes and lost the graph."
- **Why it survives:** the restore is doing exactly what it is designed to do (faithful per-workspace
  restore); the defect is upstream (a graph-less layout got saved) and the restore has no notion of a
  "degenerate" state to reject.
- **Instances:** 2026-08-31 — TheTerrace restored to a graph-less two-stack layout. Mitigated:
  `LayoutRestoreGuard` keeps the current graph-bearing layout when the restore would drop the graph.
  The broader per-workspace-vs-global fork is recorded in docs/notes/workspace-open-layout-restore.md.
- **Control:** `LayoutRestoreGuardTests`; `WorkbenchDiagnostics` records which restore path was taken.
- **Status:** `controlled` (RESOLVED — workspace-open no longer restores a per-workspace layout; the arrangement is kept. No restore = no degenerate restore. See docs/notes/workspace-open-layout-restore.md)

### DC-060 — An automated conflict resolution stages what it did not resolve
- **Signature:** a rebase or merge is scripted — resolve the known-conflicting files, `git add -A`, `--continue`, loop — because the same two append-only logs conflict on every integration and resolving them by hand each time is waste. Then a file conflicts that the script does not know about. `git add -A` stages it exactly as git left it, `--continue` commits it, and the conflict markers are now content. Nothing errors: the working tree is clean, the rebase finished, and the commit looks like every other one.
- **Why it survives:** the script is correct for the case it was written for, which is the case that occurs almost every time. This session integrated a dozen times with an append-only log merge tool and it worked; the thirteenth had a third file in conflict and the blanket `add -A` swallowed it. The build does not catch it — markers in a markdown file compile fine — and the tests do not either. It was caught only because a gate happened to parse that file for structure and reported a symptom two steps removed: *"DC-054 has no Status line"*.
- **Instance:** 2026-08-31. `docs/lessons/defect-classes.md` was committed with `<<<<<<< HEAD`, `=======` and `>>>>>>>` in it, and a duplicated heading, after a scripted rebase whose loop resolved the two audit logs and staged everything else untouched. The register gate flagged a structural oddity, not the markers themselves; the markers were found by looking.
- **The blast radius is the point.** Markdown, JSON, YAML and config files take conflict markers silently. Source does not, which is why this is a documentation-and-configuration defect and why a green build proves nothing about it.
- **Second instance the same day, different symptom.** The next scripted rebase reported three conflicts; the log-merging tool resolved one and **two remained** — both DERIVED files (`docs/audit/audit-data.js`, `docs/docs-index.js`). `git add -A` staged them exactly as git had left them, and the marker grep came back clean, because a derived file's conflict does not always leave markers: git had merged the two sides line-wise into something syntactically valid and semantically stale. The tests passed. It surfaced only by regenerating the derived files afterwards and finding a diff. **The marker check was necessary and not sufficient — it proves nobody committed a marker, not that the file is right.**
- **Control:** two checks after any scripted resolution, before committing: grep the tree for `^<<<<<<< `, `^=======$` and `^>>>>>>> `, **and** regenerate every derived artifact and confirm it produces no diff. The second is what catches a conflict in a generated file, which is the case where markers are least likely and staleness is most likely. The scripted loop keeps its value; what it needed was a check that the only files it staged blind were the ones it knew how to resolve.
- **The generalisation to apply elsewhere:** **`git add -A` after an automated resolution is a claim that every conflict was handled, and a script can only handle the ones it was told about.** Any automation that resolves conflicts should verify the result contains no markers rather than assume its own completeness — the same shape as trusting an exit code instead of reading the state.

  **Third instance, 2026-08-31, and the control that replaces the habit.** A three-commit rebase
  regenerated the views between commits two and three; the third commit then changed a document,
  so `docs/docs-index.js` merged and committed with a stale `sourceSha256`. Valid JSON, no marker,
  no failing test. Caught by performing the habit — which is exactly the problem: the control was
  a thing a person remembers to do.

  `tools/verify-derived-views.py` (CI, plus a `--self-test`) now runs each generator and compares
  its output with what is committed. The generation **timestamp is excluded** — it changes on
  every run by construction, and a gate that fails always is one people learn to bypass; every
  content hash is compared byte for byte. It restores the committed bytes whether it passes or
  fails, so a failing gate never leaves the tree rewritten and never makes the next command's
  output a lie. **Observed failing 2026-08-31** against a planted one-value edit of the shape a
  line-wise merge produces, and observed NOT failing against a timestamp-only difference.

- **Status:** `controlled`

### DC-058 — Every warning is correct and the wall of them hides the one that matters
- **Signature:** a component reports what it could not do, per unit of work, conditionally, with a count — every rule a good disclosure is supposed to follow. Then the unit of work multiplies. Thirty-nine scopes each raise the same two boundaries with their own numbers, so nothing deduplicates them, and a surface that concatenates the list shows sixty near-identical sentences. Each one is true. The reader stops reading, and the single line that was a real finding is somewhere in the middle of them.
- **Why it survives:** every control passes and every rule was followed. This codebase has spent real effort making disclosures fire — conditional, counted, never blanket, asserted in both directions — and **none of that effort was about what a reader does with sixty of them**. The failure is not in any disclosure; it is in the absence of anyone owning the aggregate. It also grows silently: each new extractor and each new scope adds lines to a list nobody re-reads.
- **Instance:** 2026-08-31. A real index of TheTerrace produced **178 disclosure strings, 108 distinct, for 28 actual classes**. `knowledge-headings-not-analysed` and `knowledge-inline-code-not-resolved` appeared **39 times each** — once per knowledge scope, each with a different count, so `Distinct()` merged none. The user's screenshot shows the result: the disclosure text occupies roughly four fifths of the window and the graph is a strip along the top. Buried in it, in the same typeface as everything else, is `knowledge-prose-link-target-missing (107 …)` — 109 rotted cross-references, the most actionable thing the product had ever told anybody.
- **The numbers were also less useful than they looked.** "914 headings in this scope" answers nothing a person asks; "4,471 headings across the workspace" answers "how much of this repository is unread". Folding produced facts that had never been stated, from data that had been there all along.
- **The fix nearly shipped with a worse defect.** Summing the leading count and keeping the rest of one scope's sentence rendered `knowledge-headings-not-analysed (4,471 heading(s) in 10 document(s))` — a true workspace total beside one scope's document count, reading as though somebody had counted both. **A stale number inside a corrected one is worse than either alone.** Everything from the second number onward is now cut, and the dangling preposition that leaves is trimmed.
- **Folding was necessary and not sufficient.** 28 lines is a better list and still not a status message: the user's actual words were *"the status bar should not have more than a couple lines… anything more should be a modal fly-in as opposed to taking up real estate."* `IndexSummary.Describe()` now names the largest disclosure and counts the rest — **8,000 characters to 139** — and the full folded list stays on the result for a surface that can hold it. **The right length for a diagnostic depends on where it is shown, so the aggregate needs an owner per surface, not one globally.**
- **Control:** `DisclosureSummary.Fold`, used by every surface that renders disclosures (the graph projection, the canvas view-model, the index summary). `DisclosureSummaryTests` pins the total, the cut, the dangling word, the single-scope case that must keep its exact sentence, and that two different classes never merge — that last one because folding a boundary and a gap together is DC-050 arriving from a new direction.
- **The generalisation to apply elsewhere:** **a rule about one item is not a rule about the list.** Whenever something is emitted per unit — a warning, a log line, a health finding — ask what it looks like at a hundred units, and give the aggregate an owner. The signal-to-noise ratio of a diagnostic is a property of the collection, not of any member of it.
- **Residual risk:** folding is textual. It assumes every scope emits the same sentence template for a class, which is true today and is not enforced anywhere; a reader that varied its wording per scope would fold into a total wearing one arbitrary explanation.
- **Status:** `controlled`

### DC-059 — A persistent per-frame render subscription that never lets the UI idle

- **Signature:** a control subscribes to `CompositionTarget.Rendering` (or an always-running
  `DispatcherTimer` that invalidates) for its whole lifetime and polls "is anything dirty?" every
  frame. WPF is retained-mode and normally rests when nothing changes; a live per-frame subscription
  defeats that — the UI thread does work ~60×/second forever, for every such control, even when
  everything is idle. The window feels jittery because the compositor never settles. If the same
  invalidation is not gated on visibility, a hidden/background pane invalidating also makes the docking
  host activate it — a focus/tab steal that looks unrelated.
- **Why it survives:** it works and looks correct; the cost is invisible in a single-control demo and
  only bites with several instances and on an idle machine. No test fails — the behaviour is a runtime
  dispatcher/visibility property, not a unit-observable one. The coalescing intent ("only redraw the
  final state") is real and correct; the *mechanism* (a per-frame poll) is the defect.
- **Instances:** 2026-08-31 — `TerminalView` per-frame `CompositionTarget.Rendering` poll; two agent
  terminals made the window jittery and a background one writing stole focus. Fixed: event-driven,
  coalesced (`Interlocked` gate + one `Dispatcher.BeginInvoke(Render)`), visibility-gated
  `RequestRedraw`, called by the pump only when `_screen.IsDirty`. Idle → zero repaints.
- **Control:** the systemic framework in `docs/investigations/redraw-isolation.md` (five principles:
  event-driven / coalesced / visibility-gated / right-level-leaf-only / isolated islands). No
  per-frame subscription outside a genuine animation. A grep control for `CompositionTarget.Rendering`
  in review is the mechanical guard.
- **Status:** `controlled`

### DC-061 — A type promises total input-safety, but one access path is unguarded

- **Signature:** a type's doc-comment promises "nothing throws / every coordinate is clamped" (or
  similar total-safety), and every *mutator* honours it — but one path, usually a read/indexer,
  bypasses it. Callers read the promise and assume the whole surface is safe.
- **Why it survives:** the promise is load-bearing trust; nobody re-checks the one method that breaks
  it, and it only faults on a state most inputs never reach.
- **Instance:** 2026-08-31 — `TerminalScreen` documents "Nothing here throws on bad input. Every
  coordinate is clamped", but `this[row,column] => _cells[(row*Columns)+column]` has no bounds check.
  `DrawCursor` read the cursor cell while the cursor was in the deferred-wrap position
  (`CursorColumn == Columns`) at the bottom row → index == `_cells.Length` → IndexOutOfRangeException.
  Verified by a bottom-right deferred-wrap repro (seen throwing).
- **Control:** the indexer now clamps both coordinates (`TerminalScreen.this[row,column]` uses
  `Math.Clamp`), honouring the type's own "every coordinate is clamped" promise. Regression tests
  `TerminalScreenTests.ReadingTheCursorCell_AtDeferredWrapOnTheBottomRow_DoesNotThrow` and
  `TheIndexer_ClampsOutOfRangeCoordinates_AndNeverThrows` — **observed failing** on the un-fixed code
  (5 cases, all `IndexOutOfRangeException`) and green after the clamp.
- **Status:** `controlled` — clamp landed Phase 1; regression tests observed red-then-green.

### DC-062 — UI-thread render reads state a background thread mutates, behind a false single-thread invariant

- **Signature:** a mutable model is documented "not thread-safe / read between writes" and treated as
  single-threaded, but the writer is a background pump and the reader is the UI render, with no lock,
  snapshot, or marshalling. Works under light load; tears/faults under concurrent load.
- **Why it survives:** on one quiet session the write and the read almost never interleave, so it
  passes every manual test; the failure needs concurrency pressure (here: two active sessions).
- **Instance:** 2026-08-31 — `TerminalScreen` is mutated by `TerminalSurface.PumpAsync` (background)
  while `OnRender` reads it (UI); crash reproduced "with two sessions active". The documented "renderer
  reads … between writes" invariant is not enforced anywhere.
- **Control:** `TerminalScreen.SyncRoot` — the pump holds it across `_parser.Consume`
  (`TerminalSurface.PumpAsync`) and the renderer holds it across a whole frame (`TerminalView.OnRender`),
  so a frame never observes a half-applied write or a `Resize` array-swap. Guard test
  `TerminalScreenTests.ConcurrentWritesAndReads_UnderSyncRoot_DoNotThrow` exercises write+resize vs
  full-grid read under contention. A recurrence means a mutation or a read escaped the lock.
- **Status:** `controlled` — SyncRoot coordination landed Phase 2.

### DC-063 — A proportional-split-tree layout collapses single-child splits, relocating unrelated panes

- **Signature:** moving/removing one pane changes the position or orientation of *other* panes, and the
  whole view re-draws. There are no fixed/absolute dock regions — the layout is a tree of proportional
  splits, and removing a pane can leave a split with one child, which collapses into that child and
  restructures the tree.
- **Why it survives:** each operation is individually correct (the collapse prevents empty regions; the
  weight-normalise is right), so no test fails; the surprise is emergent and only visible interactively.
  It also mismatches the common "fixed docks, panes contained within" mental model, so it reads as a bug.
- **Instance:** 2026-08-31 — moving the graph out of `split-columns` collapsed it to `workspace`,
  flipping workspace from the left column to the top row ("contents flipping from bottom to top").
  Compounded by `Adapter.Render()` rebuilding the entire dock view on every layout op.
- **Control:** **Named absolute dock zones**, shipped via a Strangler service (`adr-0021-named-dock-zones`).
  `ZoneLayoutService` confines every operation to the zone(s) it names (`WithZone` leaves the other
  zones reference-identical); `ZoneBackedLayoutService : ILayoutService` projects the zone model to a
  fixed-shape tree so the existing adapter renders it, and moving/closing a pane can no longer flip the
  frame. Controls, observed failing on a whole-restructure shim then green: **model** —
  `ZoneLayoutServiceTests.MovePane_ChangesOnlySourceAndDestination_OtherZonesReferenceIdentical`;
  **view (end-to-end through AvalonDock)** —
  `ZoneWorkbenchAdapterTests.MovingASurfaceToAnotherZone_LosesNoPane_AndMovesOnlyThatSurface`.
- **Status:** `controlled` — zone model shipped and wired into the shell; containment proven at the
  model and the rendered view.

### DC-064 — A deterministic test double produces colliding output across instances

- **Signature:** two independent instances of a "deterministic" fake (a capability, id, or token
  factory seeded from a counter) emit identical values, so a negative test that forges a value from a
  second instance accidentally matches the real one — the test then passes or fails for the wrong
  reason rather than for the behaviour under test.
- **Why it survives:** each factory is individually correct (unique *within its own sequence*); the
  collision appears only when two instances are compared, which most tests never do. The suite is
  green until a cross-instance negative test is written — and then it fails confusingly, looking like
  a production bug rather than a test-double bug.
- **Instances:** 2026-08-30 — `SequentialCapabilityFactory` in the Loomkeeper Phase-1 tests:
  `Ingest_ForgedCapability_Rejected_AndNothingStored` forged a capability from a second factory whose
  counter also started at 1, so `CryptographicOperations.FixedTimeEquals` matched and the forgery
  test failed. Found on the first test run; the fix hardened the double and made the forgery explicit.
- **Control:** the fake carries a per-instance salt (a `Guid`) so two instances cannot collide, and
  the forgery is expressed as an explicit never-issued token (`WatcherFixtures.ForgedCapability`)
  rather than a second instance's output. The generalisation: a negative test proves *rejection* only
  if the rejected value is one nothing legitimate could have produced.
- **Residual risk:** the salt makes the immediate collision impossible by construction, but no gate
  prevents a future non-salted deterministic double from reintroducing the shape.
- **Status:** `partially-controlled`

### DC-065 — A real-process daemon-reuse test is timing-fragile under full-suite load

- **Signature:** a test that starts a real out-of-process daemon (or reuses one over a named pipe /
  workspace mutex) passes when run in isolation but **intermittently fails when the whole suite runs**,
  even with test parallelism disabled — the failure is a startup/teardown *race*, not a logic error,
  so re-running it green hides it.
- **Why it survives:** parallelism is already disabled (DC-008's control), so the shape looks
  impossible — but the contention is **sequential residue**, not concurrency: a daemon, named pipe, or
  mutex from an *earlier* real-process test in the same run has not fully released when the reuse test
  starts its own daemon, or the readiness poll is CPU-load-sensitive under a busy suite. In isolation
  there is no prior daemon to contend with, so it is always green — which is exactly what makes it read
  as a flake rather than a real teardown-ordering defect.
- **Instances:** 2026-08-31 — `ShellBootstrapTests.ASecondShell_ReusesTheRunningDaemon_RatherThanStartingAnother`
  failed once in the full `AiDe.Core.Tests` run (770 tests) during Loomkeeper slice 2, then passed in
  isolation and on the next full run. Observed, **not introduced** by slice 2 — the slice's code
  (`CoordinationContract.cs`) is pure in-process logic that touches no daemon, pipe, or process; the
  slice added no daemon test. **Root-caused and fixed** 2026-08-31 (slice-3 follow-on): the assertion
  was `DaemonsRunning() <= before`, a **system-wide `AiDe.Daemon` process count** — a category error,
  because the daemon *deliberately* outlives its client (the idle grace holds warm state through a
  shell restart, `ShellBootstrap` §"Launching is racy on purpose"), so other tests' lingering daemons
  polluted a machine-global counter, and an ordinary load-induced-then-lock-resolved redundant launch
  (harmless in production) could false-fail it. The counter measured the machine; the invariant is per
  workspace.

  2026-08-31 (**a sibling the first sweep missed**) —
  `DaemonProcessTests.AfterTheFirstDaemonExits_AnotherCanTakeTheWorkspace` failed under
  `verify-test-run` (both projects together, 1,496 tests): the daemon started with
  `--startup-seconds 2` exited **-1** where the test asserts exactly 0, and the identical run
  immediately afterwards passed. Not introduced by the change in flight — that change touches
  extractors, tools and docs and no daemon code — and it passes in a solution-wide `dotnet test`,
  so the trigger is the heavier concurrent run.

  **The class was registered and its first instance fixed, but the sweep stopped at that one test.**
  `DaemonProcessTests` carries at least two more assertions of the same shape
  (`ADaemonNobodyConnectsTo_ExitsOnItsOwn`, `ADaemonWhoseClientLeaves_ExitsAfterTheIdleGrace`), each
  pinning an exact exit code from a process whose shutdown races the suite. `class → sweep → derive
  → prevent` (CI1) makes the sweep part of the fix — this is the register's own rule not being
  applied to the register's own entry. The open question is what these tests may legitimately assert
  about a process exit under load: "exited within its grace" is a product claim, "exited with
  exactly 0" is a claim about scheduling.
- **Control:** the reuse test now proves the **workspace-scoped** invariant deterministically — one
  logical daemon (one store, one **epoch**) per workspace, enforced by `WorkspaceLock`: (1) a
  **readiness barrier** (`await first.FindAsync(...)`) gates on an *observed answer*, not a delay, so
  reuse is measured against a ready daemon; (2) **three-way epoch equality** (first == second ==
  third, all overlapping in scope so the daemon cannot idle-shut-down between them) is the stable
  oracle — even a momentary redundant launch loses the lock, exits, and the shell reconnects to the
  incumbent, so the epoch it sees is the incumbent's. The machine-global `DaemonsRunning()` counter was
  removed. Verified stable across three isolated runs and a full 787-test suite run. The generalisation
  (a control): **a per-resource invariant must be measured against that resource, never a machine-global
  count of a shared, deliberately-lingering process** (the pack's CI-ENV / RES-LEAK discipline — the
  control supplies and observes its own state).
- **Residual risk:** the fix removes the global-count dependency, but the underlying real-process
  launcher remains environment-sensitive (a cold start still has a 30s deadline); the epoch oracle is
  robust to that because lock-resolution makes epoch equality hold regardless of a transient redundant
  launch.
- **Status:** `controlled`

### DC-066 — A workspace-dependent feature wired only in the constructor is dropped by the real composition root

- **Signature:** a feature is wired into a shell/host in its **constructor**, but the object is built
  before the thing it depends on exists (here: `MainWindow` builds `new WorkbenchShell(null)` before the
  workspace resolves), and the **real runtime path** that supplies the dependency — an `AttachWorkspace`
  / late-init method — **rebuilds the composition without the feature's wiring**. The feature is
  therefore inert at runtime while every test passes, because the tests exercise the inner component
  (the factory/pane) **directly**, never **through** the composition root that drops it.
- **Why it survives:** it is a pure green-suite-broken-surface (E2E-C): the component's own tests are
  correct and green, the constructor path even works when a dependency is passed directly (as a fixture
  does), so nothing points at the one production path — the late attach — where the wiring is silently
  discarded. A second reuse/reconcile layer can compound it: even after the composition root is fixed to
  wire the feature, panes **already realized** against the un-wired factory are **reused, not rebuilt**
  (DC-029), so the feature stays inert until the realized surfaces are explicitly invalidated.
- **Instances:** 2026-08-31 — the Loomkeeper watcher read surfaces (Sessions/Board/Leaderboard) were
  wired in `WorkbenchShell`'s constructor (conn-2/conn-5), but `MainWindow` builds the shell with a null
  workspace and the real path, `WorkbenchShell.AttachWorkspace`, rebuilt the factory as
  `new SurfaceContentFactory(queries)` — dropping the watcher queries and never opening the
  `WatcherHost`. Every App test passed (they build the factory directly). Found while investigating the
  DC-061 crash. **Fixed** the same day: a `StartWatcher(dataDirectory)` helper opens the host and returns
  the queries, called from **both** the constructor and `AttachWorkspace`; the shell then
  `Adapter.Invalidate(...)`s the stateless watcher surfaces so the next `Render` rebuilds them against
  the wired factory (never a terminal — DC-029).
- **Control:** an **E11 test through the real composition root** —
  `WorkbenchShellTests.AttachWorkspace_WiresTheWatcher_SoTheSessionsPaneIsLive_NotUnavailable` — attaches
  a workspace with a real data directory and asserts the Sessions pane the Adapter builds shows its live
  empty state ("No sessions observed"), **not** "not available". Generalisation: **wire a
  workspace/late-dependency feature in the method that receives the dependency, and prove it through the
  composition root, not only the component** — a component tested in isolation says nothing about whether
  the root that assembles it keeps the wiring (E11).
- **Residual risk:** the panes still fold once on (re)build; live auto-refresh as new sessions register
  is a separate follow-on (the pump keeps the store current; reopening a pane re-folds it). A session
  only appears if it writes a coordination-contract log under `<dataDir>/loomkeeper-coord` — the
  auto-emitting session wrapper is future work.
- **Status:** `controlled`

### DC-067 — A coordination session-end removed the mapping but never ended the session, so liveness kept lying "Alive"
- **Signature:** an ingest handles a `session-end` (or close) event by forgetting the session's *external→internal id mapping*, but never marks the *internal* session ended in the store. Liveness is a projection over the store (Ended iff `IsEnded(sessionId)`, else Alive/Stale by heartbeat recency), so a closed session that was never marked ended keeps reporting **Alive** (or drifts to **Stale**) forever — the watcher shows a live session for a terminal that is gone.
- **Why it survives:** the mapping removal is the *visible* half of "end the session" and it works — a subsequent heartbeat for that external id is correctly ignored — so the handler looks complete. Nothing in the end path reads liveness back, and a round-trip test that only checks "no new session on a stale heartbeat" passes. The lie only shows when something *evaluates liveness* after the end.
- **Instances:** 2026-08-31 — `InjectedContractIngest.ContractSessionEnd` (conn-2/conn-5) removed `_byExternalId[externalId]` but never called into the store, so `SessionCoordinationEmitter.End` → coordination `session-end` → pump left the session `Alive`. Found by the conn-8 emitter test `End_WritesSessionEnd_AndStopsTracking` asserting `LivenessState.Ended` after end+pump. **Fixed** the same day: added `IngestHost.EndSession(sessionId) => _store.MarkEnded(sessionId)` and made the `ContractSessionEnd` case call it (via the external→internal lookup) *before* removing the mapping.
- **Control:** `SessionCoordinationEmitterTests.End_WritesSessionEnd_AndStopsTracking` (end→pump→`Ended`) and `Reconcile_Registers_Heartbeats_AndEnds_FromASnapshot` (a snapshot that drops a session ends exactly that one, leaving it `Ended` while the survivor stays `Alive`) — both mutation-verified: neutralising the `_host.EndSession(...)` call reds the first, and neutralising the "end gone" branch reds the second. Generalisation: **an end/close handler must change the state the reader projects from, not only the lookup that routed the event — and prove it by reading the projection (liveness) back after the end, never only by asserting the routing stopped.**
- **Residual risk:** the shell drives ends by *reconcile from the current terminal snapshot* (a closed pane disappears from the snapshot and is ended on the next tick, ≤2s later), not by a precise per-pane close event; a session whose pane vanishes without the loop running (e.g. process kill mid-tick) is ended only when the loop next runs, and on host dispose tracked sessions are dropped without an explicit end (they go Stale, then the host's DB is disposed). The async shell loop timing itself is not unit-tested (covered by the Core end-to-end reconcile test + manual smoke).
- **Status:** `controlled`

### DC-068 — A new workbench command must be wired into three coupled places or the menu drifts from the palette
- **Signature:** a command added to the `WorkbenchCommandCatalog.All` list (so it appears in the Ctrl+K palette) but not to the **hard-coded** per-menu id lists in `MainMenuBuilder`, and/or not reflected in the hard-coded per-menu **count** assertions in `Phase3SurfacingTests.DeclaredMenusMatchWhatTheBuilderRenders`. The palette is data-driven from the catalog; the menu bar is a parallel hand-maintained list. Add a command in one place and the two silently disagree - a command reachable by keyboard-palette but absent from the menu (or vice-versa).
- **Why it survives:** the catalog edit alone builds clean and the command works from the palette, so nothing local points at the menu. The drift is only visible when a test that reflects over *both* sources runs.
- **Instances:** 2026-08-31 — conn-11 added `watcher.raiseDispute` to the catalog with `Menu:"_View"`; the build was clean and the palette worked, but `MainMenuTests.TheMenuCoversEveryCatalogCommand` and `Phase3SurfacingTests.DeclaredMenusMatchWhatTheBuilderRenders` both reded until the id was added to `MainMenuBuilder`'s `_View` list and the `_View` count bumped 4→5. **The control worked** — it caught the drift before merge.
- **Control:** the two conformance tests, **already in place** — `MainMenuTests.TheMenuCoversEveryCatalogCommand` (every catalog command appears in exactly one menu) and `Phase3SurfacingTests.DeclaredMenusMatchWhatTheBuilderRenders` (declared per-menu counts equal what the builder renders). They fail closed on any catalog/menu drift. Generalisation: **when two sources must agree (a data-driven list + a hand-maintained parallel list), a reflection-over-both conformance test is the control that makes the drift a red, not a production surprise** — and adding an item means updating every coupled source in the same change.
- **Residual risk:** the coupling itself remains (the menu is not yet generated from the catalog's `Menu` field); a future refactor could make the builder data-driven and delete the parallel list. Until then the control holds the invariant.
- **Status:** `controlled`

### DC-069 — Parallel branches independently assign the same sequential IDs to different entries
- **Signature:** two branches diverge from a common point in an append-only, sequentially-numbered register (defect classes, ADRs, migrations) and each appends new entries starting at the next free number. On merge, the same IDs (here DC-039..DC-044) denote **different** entries on each side, and every code/doc reference to those IDs is now ambiguous. A 3-way text merge cannot resolve it — both sides "added lines".
- **Why it survives:** each branch is internally consistent and its gate passes in isolation; the collision only exists in the union, and a naive resolution (keep both, or take one side) either duplicates an ID (register gate fails) or silently drops a class. Worse, a blanket find-replace to renumber one side corrupts the *other* side's references to the same numbers.
- **Instances:** 2026-08-31 — forward-integrating `feature/agent-watcher-substrate` (DC-039..044: test-double, daemon-flake, cursor-crash, watcher-wiring, session-end, menu-drift) into `origin/main` (DC-039..059, different classes). Resolved by keeping main's canonical DC-039..059 verbatim and **renumbering this branch's 6 classes to DC-064..065**, then updating references **per-file, disambiguated by meaning** (my crash ref in TerminalView → DC-061; main's "two kinds" ref in KnowledgeExtractor left as DC-041). A blanket replace was explicitly rejected as it would have corrupted main's same-numbered references.
- **Control:** `verify-defect-register.py` (unbroken sequence + header counts) catches the duplicate-ID/miscount failure mode at the gate — it is what forces a renumber rather than a silent duplicate. Generalisation: **the integrating branch renumbers its own new entries to follow the trunk's highest, and updates references scoped by meaning, never by blanket replace.** A stronger future control would reserve per-branch ID ranges (or use content-hash ids) so the collision cannot arise.
- **Residual risk:** reference updates are semantic, not mechanical, so a missed reference keeps an old number; prose references in generated/derived files (docs-index.js) are regenerated from source. The renumber breaks any *external* citation of this branch's pre-merge DC-041..044 (e.g. in commit messages), which are historical and left as-is.
- **Status:** `partially-controlled`

### DC-070 — A test written as a list of exclusions accepts everything nobody listed

- **Shape:** a predicate decides whether a value is the simple case by naming what the complicated
  cases *contain* — `!value.Contains('$') && !value.Contains('(')`. It is correct about every shape
  its author had in mind, and every shape they did not think of falls through to the **affirmative**
  branch and is asserted as fact. This is the dangerous half of DC-033's family: there, an
  unrecognised spelling falls through to "not found" and the fact is merely missing; here it falls
  through to "yes", and the fact is **wrong**.
- **Signature:** a boolean written entirely in negatives, guarding an assertion rather than a
  rejection. Read it as *"anything I did not think to exclude is the simple case"* — if that
  sentence is not one you would sign, the predicate is inverted. A second tell: the value that
  reaches the store is **the source text itself**, so the defect is legible in the data as an
  identifier where a name should be, and nobody looks because it is well-formed.
- **Why it survives:** it produces no error, no disclosure and no gap. The unresolved-count that
  would have raised it is incremented in the `else` branch, which the defect never reaches, so the
  system reports **full coverage of a fact it invented**. Every test written from the shapes the
  author enumerated passes, because those are the shapes the predicate handles.
- **Instance:** 2026-08-31 — `BicepExtractor.IsLiteral(name) => !name.Contains('$') &&
  !name.Contains('(')`. A Bicep resource name is very often a bare identifier —
  `name: workspaceName` — which contains neither character, so it was `Unquote`d (a no-op on
  unquoted text) and asserted as `resource_name = "workspaceName"`. **Measured before any change:
  10 of 27 resource names in the graph were the text of an identifier rather than a name**, and not
  one was disclosed, because they never reached the branch that counts unresolved names. Ground
  truth from Azure's own what-if output (`docs/delivery/evidence/S00/recovery/…/recovery-plan.json`)
  says the first of them is `theterrace-s00-log`.

  The roadmap had ranked this work *"Large, low value — evaluating expressions means writing an
  interpreter"*, on the premise that the alternative to evaluation was honest disclosure. It was
  not: the alternative in production was ten fabricated names. **A wrong priority can be the defect
  hiding, not just the work waiting** — the item as written would have been correctly deprioritised
  forever.
- **Control:** the folder is default-deny — every function, every literal form and every reference
  is on a list of things it *can* fold, and anything else is refused and counted. The executable
  guard is `NoResourceNameIsEverTheTextOfADeclaredSymbol`, which asserts across a whole template
  that no emitted `resource_name` equals the source text of any declared `param` or `var`; it is the
  class, not the instance, and it fails on the un-fixed reader with `Object = vaultName`. Alongside
  it, a folded name is provenance `Inferred` and a quoted literal stays `Verified`, so the two are
  never again indistinguishable in the store. **Observed failing 2026-08-31**: 13 of 19 new tests
  red before the change.

  A refusal test can pass for the wrong reason, which is DC-016 wearing this class's clothes: four
  of the refusal tests passed on the un-fixed code by construction. Mutation-testing them by adding
  `guid`/`uniqueString`/`format` to the function table turned two red — but `GuidIsNeverFolded`
  stayed green, because its arguments were refused before the function table was ever consulted. It
  was rewritten with a `guid(...)` whose arguments all fold, so the refusal it names is the refusal
  it tests.
- **Status:** `controlled`

### DC-071 — A generated artifact records the environment that produced it

- **Shape:** a generator stamps a derived file with something true of **where it ran** rather than
  of **what it describes** — the working directory's name, the host, the user, an absolute path.
  Every producer then produces a different artifact from identical inputs, so the file is no longer
  a function of its sources and cannot be compared with itself.
- **Signature:** a generated file whose diff, on a run that changed nothing, contains a value that
  names a *place*. The tell in code is a fallback of the form
  `os.path.basename(os.path.abspath(...))` reached when the real identity is unavailable — a
  last-resort that is silently correct in the single-checkout case the author was in, and silently
  wrong everywhere else. In a repository using worktrees the directory is the **worktree's** name,
  never the repository's.
- **Why it survives:** it is invisible from inside one checkout, which is where it is written and
  tested. It only appears when two producers regenerate the same file, and then it presents as a
  merge nuisance — a line that flips back and forth — which reads as somebody's mistake rather than
  as non-determinism. Nothing fails: the artifact is well-formed and self-consistent every time.
- **Instance:** 2026-08-31 — `audit-log.py render` stamped `"project"` from the directory two
  levels above `docs/`. On `main` the value oscillated between `ai-de-facelift` and
  `ai-de-session-phase3-pane-probes` depending on which session last regenerated, and carried into
  `docs/audit/index.html`'s `<title>`. `docs-graph.py` was half-immune — it re-read the value out of
  the existing `docs-index.js` — but its own fallback had the same shape, so a fresh clone in a
  differently-named directory would have set it and every later run inherited it.

  **The cost was not cosmetic.** The project name feeds `graphSha256`, so the graph hash differed
  per worktree too, and any comparison keyed on it was meaningless. It also came within one commit
  of neutering `verify-derived-views` (DC-060's control, shipped the same day): that gate fails when
  a derived view does not match its generator, and this defect made a *correct* view fail on any
  machine but the last one to write it. **A non-deterministic generator does not just corrupt its
  output, it disarms every control that reads it.**
- **Control:** identity comes from the **origin URL** — the same string in every worktree, on every
  machine, permanently — and only then from the directory. Deliberately *not* "keep whatever value
  is already recorded": that is stable only until two producers disagree, at which point it is
  first-writer-wins with a diff every time the loser regenerates, which is the defect wearing a
  hat. The executable guard is `verify-derived-views.py`, which now passes across worktrees for the
  first time. **Observed failing 2026-08-31** on `main`: both derived views reported stale, with the
  only substantive difference being the project name.
- **Status:** `controlled`


### DC-072 — Ambient input handler competes with a focused capture surface
- **Signature:** a window-level key handler — a tunnelling `PreviewKeyDown`, an ancestor
  `InputBinding`/`KeyBinding`, or a modal key-capture — acts on keys a **focused capture surface**
  (a terminal, a canvas/WebView, a code editor, a game view) needs, with **no rule that yields to the
  focused capture surface**. For a terminal specifically, a second shape of the same class: the input
  translator's output does not depend on a **mode the peer negotiated on the output channel** (DECCKM
  application-cursor-keys, `\e[?1h`), so the surface emits the wrong sequence for the state the peer is
  in. Recognisable as: "keys work in some states and not others" in a surface that should own raw input.
- **Why it survives:** each ambient handler is individually correct and was added for a real chrome
  feature (pane resize, surface-switch), and each passes its own test; **nothing tests the interaction**
  between an ambient handler and a *focused* capture surface. The mode gap survives because the parser
  that drops the mode is on the *output* path while the translator that ignores it is on the *input*
  path — two files, no test spanning them — and it is invisible until a child process that uses the
  mode is run. The terminal's private-mode shortcut was reasoned in a code comment but **not marked**
  `simplify:` with a ceiling, so nothing flagged when its ceiling was reached.
- **Instances:** 2026-09-01 — terminal keystrokes "get weird" in states: (H1) the workbench resize
  handler (`WorkbenchController.cs:699` host `PreviewKeyDown` → `HandleResizeKey`) tunnels arrows away
  from a focused terminal while a resize is active; (H2 root) `VtParser.Dispatch` (`VtParser.cs:274`)
  ignores all DEC private modes so DECCKM is never tracked and `TerminalInput.ForKey` is structurally
  mode-blind (no mode parameter), always sending the CSI form; (H3, inferred) terminal focus can rest
  on a container after a render so keys route elsewhere. Full analysis:
  `docs/investigations/terminal-input-not-local-to-focus.md`.
- **Control:** a single **"focused capture surface owns input"** rule — a marker interface
  `ICapturesInput` implemented by `TerminalView` (and other raw-input surfaces); every ambient host key
  handler consults one shared helper `focused-element-is-ICapturesInput` and yields; a
  **class-prevention test** asserts that, for each registered ambient key handler, a focused
  `ICapturesInput` surface receives the key. Paired, for terminals, with a **mode-aware input contract**
  (track DECCKM on `TerminalScreen`; thread `applicationCursorKeys` into `ForKey`; SS3 form when set) and
  a parser test that `\e[?1h`/`\e[?1l` set/clear the flag. **Not yet built** — this is an investigation;
  the control must be *observed failing on today's code* (a focused-terminal-plus-active-resize test
  fails now) before it counts. Phased repair plan in the investigation, Phase 0 = input-path
  instrumentation.
- **Status:** `uncontrolled` (investigation complete; repair plan awaiting human approval)

### DC-073 — A stand-in outlives the thing it was standing in for

- **Shape:** a seam is introduced with a deliberate placeholder — a mock, a sample, a stub, a
  hard-coded default — because the real implementation does not exist yet. The comment says
  *"until X ships"*, and the whole design is that swapping it is one line. X ships. Nobody swaps
  it. The placeholder keeps working perfectly, which is the problem.
- **Signature:** a `Mock`/`Sample`/`Stub` type instantiated in production wiring, next to a comment
  naming the thing that would replace it — and that thing now exists. Grep the codebase for
  *"until … ships"*, *"awaiting"*, *"placeholder"*, *"for now"* and check each against reality:
  every one is a dated claim, and nothing dates it. A second tell is a **query with no callers**:
  the shipped implementation is reachable from tests and from nowhere else.
- **Why it survives:** every signal is green, and green for the right reasons. The seam exists and
  is well designed. The surface renders. The tests pass — against the placeholder, which is exactly
  what they were written to do. Nothing fails, nothing is missing, and the only evidence is a
  comparison nobody makes: *"is the thing this is waiting for still missing?"* It is the planned,
  documented, reviewed shortcut, which is why it hides better than an accident. It also crosses a
  **hand-off boundary**: the session that shipped the real thing announced it and moved on, and the
  session that owned the placeholder had already moved on too.
- **Instance:** 2026-09-01 — `MockNodeContentSource` returned a labelled `// SAMPLE` and
  `WorkbenchShell` instantiated it directly, under the comment *"A mock until Core ships
  NodeContentAsync (ADR-0018); swapping this field for the Core-backed source is the whole
  live-wiring change."* `NodeContentAsync` had shipped, and `§4g` of the session contract had
  announced it as *"the code viewer is unblocked"*. **Measured: the App contained zero calls to
  `NodeContentAsync`.** The code viewer showed a sample against a fully indexed workspace, and the
  first-load path asked for a node id of `"(sample)"` — a string that is not a node. Found by
  grepping the App for stand-in markers, not by anything failing.
- **Control:** `AttachWorkspace_SwapsTheSampleContentSourceForTheRealOne` asserts the shell holds a
  `MockNodeContentSource` before a workspace and a `CoreNodeContentSource` after — the swap itself,
  not a behaviour downstream of it. **Observed failing 2026-09-01** against exactly the shipped
  shape (`=> _mockNodeContent`): *Expected `CoreNodeContentSource`, Actual `MockNodeContentSource`*.

  The wiring is **derived rather than assigned** — a property computed from `_queries` — because
  `_queries` is set in two places and a field initialised at one of them is stale at the other. That
  is the same shape as DC-045, and assigning at both sites would have left the next person one
  `_queries = …` away from reintroducing this.

  **The class-wide control:** `tools/verify-standins.py`, in CI. Every `Mock*` / `Stub*` /
  `Sample*` / `Fake*` type constructed outside the test projects must be listed with the condition
  that makes it legitimate, and an entry naming a construction that no longer exists is also a
  failure — a stale allowance keeps a question alive nobody has to answer. It is **not a ban**: a
  stand-in is right for a state where the real thing cannot be asked at all (no workspace open, no
  credential configured). It is a forcing function, and the list is where the unasked question gets
  asked. **Observed failing 2026-09-01** on a planted unlisted stand-in, and observed NOT firing on
  a comment that merely quotes one — this entry's own explanation contains the construction it
  replaced, and a checker that could not tell the two apart would fail on the lesson itself.

  It deliberately does **not** try to decide whether the real implementation exists yet: that needs
  semantic analysis, and a wrong answer would be a false alarm on a gate family this repository has
  already had to teach people to trust twice. A short human-maintained list, and the review it
  forces, is the cheaper correct thing.
- **Status:** `controlled`

### DC-074 — A field is dropped in transit between a producer record and its client record

- **Shape:** a producer record is widened with a field that settles a question — an authoritative
  flag, a bound, a count. A client record is built from it, field by field, and the new one is not
  in the constructor call. Nothing fails: the client record is valid, the surface renders, and the
  consumer falls back to whatever it did before the field existed — usually a guess that looks like
  an answer.
- **Signature:** a projection type and a presentation type with **almost** the same fields, built by
  a hand-written `new Client(p.A, p.B, p.C)` that enumerates the producer's properties positionally.
  Widening the producer compiles cleanly on both sides, because the client's constructor never
  mentioned the field. The tell is a defaulted parameter on the client record whose value is never
  set by anything: the default was written to avoid breaking existing calls, and then *every* call
  took it. A second tell is a fallback that still exists downstream after the authority arrived.
- **Why it survives:** it is invisible from both ends. The producer's tests pass — it emits the
  field. The client's tests pass — it renders what it is given. A behavioural harness at the surface
  passes too, because the surface faithfully rendered everything it received; the loss happened one
  boundary upstream. Only a test that compares the two *shapes* can see it, and shapes are what
  nobody tests.
- **Instance:** 2026-09-01 — `GraphNode` was widened with `IsKnowledge`, read from `node_kind`,
  which is the one dimension that separates knowledge from source (INV-0004: `has_type` is emitted
  by six extractors and says nothing about which half of the graph a node is in). It was dropped in
  the five `new CanvasNode(...)` calls, so the page fell back to guessing from a fixed list of
  spellings — `knowledge|doc|adr|design|note|proof` — which cannot match a repository whose
  knowledge kinds are `spec`, `investigation` and `glossary`. **The Knowledge chip read 0 for the
  third time, by a third mechanism, after being fixed twice** (DC-044, a second reuse guard
  defeating the generation bump; DC-045, no surface being told after an index).

  Found by the design session while investigating the symptom, and framed by them in the sentence
  that names the class better than this one does: *"a regression against a landed cross-session
  contract — Core widened `GraphNode`; the App never consumed it."*
- **Control:** `FieldsSurviveTheClientBoundaryTests`. For each declared producer→client pair, every
  producer field must either appear on the client record **or** be listed as deliberately dropped
  with a reason. Not a ban — `Degree` is a ranking statistic the canvas does not draw, and naming it
  costs a line. It is a list where the unasked question gets asked, the same forcing-function shape
  as `verify-standins.py` (DC-073). A second test fails a *stale* allowance, and a third asserts the
  records still yield fields at all, so the comparison cannot pass by comparing two empty sets
  (DC-016). **Observed failing 2026-09-01** on exactly the shipped shape:
  *"GraphNode.IsKnowledge does not reach CanvasNode and is not listed as deliberately dropped."*

  **Three ways to lose the truth, each hiding from the others' test**, which is why this is a third
  control rather than an extension of either: *the bound was dropped* (a surface renders a payload
  and not its `Truncated`); *the payload was never asked for* (DC-073); *the field was dropped in
  transit* (this). The first is caught behaviourally at the surface, the second statically at the
  wiring, the third structurally at the boundary between records.

  **What the control cannot see:** it compares field NAMES, so a field that crosses and is then
  ignored still passes. Three of the five `CanvasNode` call sites carry `IsKnowledge: false` because
  the data genuinely is not available there — the neighbour view has kinds but not node kinds, a
  cluster stands for many nodes of mixed kinds, and the path view hard-codes `source`. Each is
  commented at the site rather than left to look like an oversight, and the first is a real gap:
  closing it means `Describe` carrying the knowledge ids too.
- **Status:** `controlled`

### DC-075 — An append-only record is written into a tree nobody will commit from

- **Shape:** a tool appends to a repository-global, append-only log. The path it resolves is
  relative to the *caller's working directory*, not to the repository, so running it from the wrong
  tree writes the entry somewhere that tree will never commit from. The write succeeds. The entry is
  correct. It exists in exactly one place, uncommitted, and nothing says so.
- **Signature:** a `--root` or path default that is a **bare relative string** (`"docs"`) in a tool
  whose subject is repo-global, in a repository that uses worktrees. Compare with a sibling tool in
  the same bundle that resolves the same directory against the repository root — two tools
  disagreeing about what "the repo" means is the tell. The symptom appears much later and wearing a
  disguise: a fast-forward refuses because a file is dirty, and the fastest way to unblock it
  deletes the record.
- **Why it survives:** every step is individually correct. The tool wrote what it was asked, to a
  real path, in a real checkout. No error, no conflict, no test. It only becomes visible when
  somebody merges — and at that moment it presents as an obstacle rather than as data, so the
  natural response (`git checkout --` on the offending file) is the one that loses it. The window
  in which the mistake is cheap and the window in which it is visible do not overlap.
- **Instance:** 2026-09-01 — a session ran `prompt-log.py` as its first command, before creating its
  worktree, so `al-0347` was appended to the **primary** checkout's `docs/audit/audit-log.jsonl`. It
  sat uncommitted in a tree nobody was working in, blocked a fast-forward hours later, and was
  recovered by hand (`7cda687`) only because the merge was inspected rather than unblocked.
  Verified: `audit-log.py:769` defaults `--root` to the string `"docs"` with no repo-root
  resolution, while `coord-core.py` deliberately resolves `.agents/` against the primary from any
  worktree.
- **Control:** `tools/verify-stranded-audit.py`. It checks the **primary checkout always** — under
  worktree discipline nobody works there, so a dirty append-only log in it is the hazard by
  definition — and every other worktree only when no session is live in it, read from
  `.agents/log/*.jsonl` using coord's own 8-hour window. A gate that fired on a session's own tree
  mid-turn would be muted within a week, which is the lesson `verify-id-allocators` already had to
  be taught. **Observed failing 2026-09-01** twice: on a planted fixture, and by reproducing the
  real incident in the real primary checkout, where it named the tree and the file.

  **Only the SELF-TEST runs in CI, deliberately.** A runner has one checkout and it is clean by
  construction, so the real check would pass every time without being able to fail — a control that
  cannot fire in the environment that verifies it (DC-016), which is this control's own subject one
  layer up. The self-test builds its own repository and can fail, so CI proves the control is alive
  while the control runs where the hazard is.

  **Why the obvious fix was rejected.** Both sessions first proposed patching `audit-log.py` to
  refuse a target outside the caller's toplevel — five lines, and wrong. `audit-log.py` is a
  **listed** pack artifact and `/updatepack` replaces listed artifacts wholesale, so the patch is
  one pack update away from vanishing silently, leaving a control everybody believes exists. That is
  this class pointed at the thing meant to prevent it. **Adding an unlisted file to a pack-managed
  tree is safe; modifying a listed one is not** — opposite rules, easily mistaken for one. The
  upstream fix (teaching the pack's tools one definition of "the repo") is deferred, not rejected.
- **Status:** `controlled`


### DC-076 — A fix's test set is drawn from the defect report, so the reported case is the only one repaired

- **Shape:** a defect arrives naming one case. The cause is found, the fix is correct for that case,
  and the test written to prove it asserts that case. The input, however, ranges over a **closed
  set** — an enum, a fixed list of ids, a small set of zones or modes — and the fix's behaviour on
  the other members was never observed. The reported case goes green while its siblings ship broken,
  and the green test is read as coverage of the rule rather than of one point on it.
- **Signature:** the fix touches a `switch`, a ternary, or a conditional whose subject is an enum or
  a fixed set, and the accompanying test names **one or two** members of that set as literals. The
  strongest tell is a hand-written array in the test — `DropKind[] kinds = [SplitLeft, JoinStack]` —
  because it is a second list that has to be kept in step with the first by memory. A second tell:
  the test's name matches the bug report's title.
- **Why it survives:** every control in the chain is satisfied. The regression test was seen to fail
  on the un-fixed code (CI6), the reviewer confirmed the cause, the suite is green, the gate passes.
  Nothing in the loop compares the tested inputs against the **domain** of the input, so a test over
  1 of 5 members and a test over 5 of 5 are indistinguishable to every check that exists. The
  report's framing is doing the sampling, and a report names the case somebody hit.
- **Instance:** 2026-09-01 — `ZoneBackedLayoutService.Move` was fixed so a "split beside the graph"
  drop stopped tabbing onto the graph. The fix remapped split kinds `Center → Right`, else `→
  Center`, and its four tests covered the reported centre case. The design session then measured
  every `DropKind` × zone and found **16 of 20 combinations wrong**: a pane dropped on the left,
  right, or bottom with any split gesture landed in the centre as the last tab, announced *"Moved
  Graph within the center."* Only `JoinStack` honoured its target. Verified by reproducing the
  matrix — the planted defect fails the sweep with all 16 named.

  **The fifth instance of this shape in one day, four of them inside a fresh fix**, and the register
  records the same movement in DC-065 (*"the sweep stopped at that one test"*), in the projection
  bounds class (*"no sibling sweep followed; this is what that costs"*), and in the extractor ratio
  class. It had never been registered as a class in its own right, so each occurrence was absorbed
  as a footnote in the class it happened to strike — which is why the count reached five before
  anyone counted.
- **Control:** **make the stale list impossible, one rung above a gate.** `SplitMeansBesideTests`
  derives its sweep from `Enum.GetValues<DropKind>()` and `Enum.GetValues<ZoneId>()` rather than
  listing members, so a kind or zone added tomorrow enters the cross-product with no one
  remembering the file exists, and the zone→stack mapping `throw`s on an unmapped member with the
  reason rather than skipping it. There is no second list to drift. **Observed failing 2026-09-01**:
  the removed remap was replanted and the sweep named every one of the 16 wrong placements, while
  `TheSurfaceIsNeverLost` correctly stayed green — an invariant guard is not a defect detector.

  **What this control does NOT do, stated so it is not over-read.** It fixes the sweep for *these*
  two enums. It cannot make the next fix sweep its own domain: no gate can tell that an input ranges
  over a closed set the author did not think to enumerate, and one that guessed would fire on every
  test in the repository and be muted within a week (the lesson `verify-id-allocators` and DC-075's
  control both had to be taught). The general remedy is the **question**, carried in
  `continuous-improvement.md`'s sweep step and now with an instance count behind it: *what is the
  domain of the input I just fixed, and did I observe all of it?* Where the domain is closed and
  small, enumerate it from the source of truth; where it is not, say which region was tested.
- **Status:** `controlled` for the layout enums, `open` as a general shape

### DC-077 — Success is announced before the work, about work that may not happen

- **Shape:** a handler announces an outcome and then starts the work fire-and-forget. The sentence is
  written before there is anything to describe, so it is not a report — it is a prediction, phrased
  as a report. Two independent things then make it false: the operation can return early without
  doing anything (a readiness gate, an empty precondition), and the discarded task's fault is
  observed by nobody. Neither shows up as an error, because from the announcer's side nothing went
  wrong; it never asked.
- **Signature:** `Announce(...)` on the line above `_ = SomethingAsync(...)`. The discard is the tell,
  and so is the tense — a message in the past tense sitting above the call that would make it true.
  The second tell is a method whose first statement is `if (!Ready) return;` and whose return type is
  `Task` rather than a result: it has no way to say it did nothing, so every caller is structurally
  forced to guess.
- **Why it survives:** this is **not** the dropped-in-transit family, and treating it as one is why
  it lasted. Those defects concern a value that EXISTED and failed to reach the user — a dropped
  binding, an unasked payload, a field narrowed at a boundary — and every one of them is found by
  following the value. Here there is no value to follow. The statement is about an ACTION, made
  before the action, about an action that may not occur; it was never true when it was said. Reading
  the code confirms the announcement is present, well-worded, and on a reasonable path. Only running
  it reveals that the thing it describes did not happen.
- **Instance:** 2026-09-01 — three sites in `WorkbenchShell.cs` (`:913`, `:1044`, `:1447`) announced
  *"Graph centred on {id}"* and then discarded `canvas.RefreshAsync(id)`. Measured by the design
  session on a real `CanvasSurface` outside a window: `Ready` **false**, the task completed, the
  graph source asked **0** times. A screen-reader user was told the graph centred on a node it never
  looked up — and not in an exotic state, but in the ordinary first moments after a canvas opens.

  **It compounded with a bound nobody had connected to it.** The graph draws 1,500 of 2,992
  most-connected-first, and knowledge nodes have a measured median relation degree of 0, so even when
  the refresh *does* run, a search hit the user picked may not be in the drawn slice — and the same
  sentence was announced regardless. Two defects in one line, arrived at from opposite directions.

  **A fourth site existed that the report did not name:** `ExplorerSurface.cs:69`
  (`reader.OnWalk(targetId => _ = graph.RefreshAsync(targetId))`) carries the silent-no-op half
  without the announcement half — a reader edge walk simply lost while the page loads. Found by
  sweeping every caller of the method rather than the three that were reported, which is DC-076's
  rule applied on its first opportunity.
- **Control:** two changes, because reporting the loss honestly would have been a true sentence about
  a broken feature. **(1)** `RefreshAsync` returns `CanvasRefresh(Outcome, Label)` — `Deferred`,
  `NoWorkspace`, `Refreshed`, `Centred`, `NotInView` — so a caller cannot speak without a result in
  hand, and the label comes **from the drawn graph** rather than from the caller's id, so the
  sentence cannot disagree with the picture. **(2)** A root requested while the page is loading is
  **held and applied** on `NavigationCompleted` instead of dropped, which fixes the fourth site too.
  The shell's `CentreOnAsync` awaits, announces from the outcome including the honest negative
  (*"X is not in the current view"*), and catches faults into an announcement — which is what makes
  the remaining `_ =` safe: nothing can escape the task.

  **Observed failing 2026-09-01** with the deferral removed and `Centred` returned in its place:
  the not-ready test reported the wrong outcome, and the deferral test printed *"the deferred root
  was never applied; the source was asked for: (none)"* — the same zero the design session measured,
  reproduced independently.

  **The test needs a real WebView2 and says so rather than skipping.** Its entire subject is the
  window between construction and `NavigationCompleted`; a fake that is ready on construction has no
  such window, so a test driving one would pass while proving the opposite (DC-016). While writing
  it, the harness it was modelled on was found to wrap *every* failure — assertion failures included
  — in *"requires the WebView2 runtime"*, reporting a real defect as a broken machine. That is this
  same class pointed at a diagnostic. It turned out to be 18 harnesses rather than one, and is now
  **DC-078** with a gate of its own.
- **Status:** `controlled`

### DC-078 — A harness reports a real defect as a broken machine, and the finding gets dismissed

- **Shape:** a test harness catches everything and rethrows it wrapped in an infrastructure
  complaint — *"STA work failed"*, *"requires the WebView2 runtime"*, *"the daemon is not
  reachable"*. Assertion failures travel that path too, so the runner's headline names a cause that
  is not the cause, and the sentence the test author wrote to make the failure legible is demoted to
  an inner exception. The test does fail, and the suite is honest about red — what is lost is the
  reason.
- **Signature:** `catch (Exception ex) { failure = ex; }` in a thread or dispatcher harness, followed
  by `throw new SomeException("<infrastructure phrase>", failure)` with no `XunitException` rethrow
  before it. The tell is the wrapper's wording: it describes the *environment* while the exception it
  carries describes the *product*.
- **Why it survives, and why it is worse than the family it came from:** the announcement classes
  (DC-074, DC-077) make a **user** believe something false. This one makes an **engineer stop
  looking**. *"STA work failed"* reads as a flaky harness or a missing runtime, and the rational
  response to flakiness is to re-run, not to investigate — so a real finding is triaged away by
  someone behaving correctly on the information they were given. It also hides in plain sight
  because nothing is broken until a test fails, and a passing suite shows no symptom at all.
- **Instance:** 2026-09-01, found in a **copy**. Writing a control for DC-077, the design session's
  `OnSta` helper was reproduced verbatim; the new file's first planted failure printed the
  environment complaint instead of the assertion text, which made the flaw visible in the copy rather
  than in the original. Both sessions then swept.

  **The sweep found 16 more, not the 2 that were reported** — every STA harness in the App suite
  (`ClassDiagramSurface`, `CodeViewer`, `CommandPalette`, `DiagnosticsSurface`, `DpiAndResize`,
  `ExplorerMode`, `PromptBar`, `SequenceDiagram`, `SurfaceContent`, `TerminalCustomization`,
  `TerminalView`, `WorkbenchAdapter`, `WorkbenchAnnouncer`, `WorkbenchShell`, `ZoneRails`,
  `ZoneWorkbenchAdapter`, plus `CanvasFocusIntegrationTests`). Nine other harnesses already rethrew
  unwrapped and needed nothing, which is how the shape stayed invisible: the correct form and the
  broken form sat side by side in one directory.

  **Measured both ways on the same planted failure:**

      without the guard   System.InvalidOperationException : STA work failed
                          ---- PROBE: the rail reported three buttons and two of them do nothing.
      with the guard      PROBE: the rail reported three buttons and two of them do nothing.

- **Control:** `tools/verify-harness-diagnostics.py`. A test file that rethrows a **captured
  failure** wrapped must first rethrow `XunitException` unwrapped; a genuine infrastructure failure
  keeps its wrapper, because there the wrapper is a true statement. The gate matches on the wrapper
  being constructed *from a captured variable*, which is what separates a harness from a fixture
  that throws a literal to simulate an error — a gate firing on those would be muted within a week,
  and the real check would go with it. It carries a DC-016 guard: finding zero harnesses is a
  failure, not a clean run. **Observed failing 2026-09-01** on a planted fixture (unguarded reported,
  guarded and literal-throwing fixture left alone) and on the repository itself before the sweep.
- **Status:** `controlled`

### DC-079 — Two conventions for one job coexist with nothing marking which is correct, so copying is a coin flip

- **Shape:** the same helper is written by hand in many files. Over time the copies diverge, and one
  variant acquires a defect the others do not have. Both forms are now present, both look like
  precedent, and nothing in the directory says which is which — so the next author, doing the right
  thing by following the local idiom, has a 50/50 chance of propagating the broken one. Every copy
  looks like conformance. There is no missing convention to notice, which is why reading more of the
  codebase makes it worse rather than better.
- **Signature:** *n* files declaring the same private helper with no shared implementation, and the
  measurable tell is **divergence in the details nobody re-derives**: the helper's name, its timeout,
  its error path. When those disagree across copies, the copies are not one convention with variants
  — they are independent artifacts that merely look alike.
- **Why it survives:** it is invisible in the steady state. Nothing is wrong until something fails,
  and it is discovered at the exact moment the reader is least inclined to look (see DC-078). It also
  defeats the normal defence: "check how the rest of the codebase does it" resolves to two answers,
  and the reader cannot tell that they asked an ambiguous question. **Observed, not theorised** — the
  design session wrote a new harness on 2026-09-01 *after reading several existing ones* and picked
  the broken form, having spent that same day arguing that you cannot tell a thing by looking at it.
- **Instance:** 2026-09-01, the STA harness. **Measured: 31 test files each declare their own**,
  under **3 different names** (`OnSta` ×20, `OnStaThread` ×7, `OnUiThread` ×2), with **5 different
  timeouts** (60s ×15, 30s ×14, and one each of 10s, 20s, 5s). Nine rethrew assertion failures
  correctly and eighteen wrapped them as infrastructure complaints (DC-078). A shared home already
  exists and is already referenced by both test projects — `tests/Shared/` (`namespace
  AiDe.Testing`) — so the duplication was never load-bearing; it accumulated because writing the
  helper again is always cheaper *for the file being written* than putting it somewhere shared.
- **The class then arrived in the CHECKERS, both of them, on the same measurement.** The design
  session's scan of this same subject reported **14 files and 2 names** against the 31 and 3 above;
  its pattern required the return type to sit immediately before the helper name, so it missed
  generic overloads and read a narrower window than its subject. It was found only because two
  measurements disagreed and the disagreement was chased instead of resolved by picking. Auditing
  this gate the same way — reconciling its count against the number of files declaring an STA thread,
  and chasing the three-file gap — found the mirror flaw: `WRAPS` matched on the **variable name**
  (`failure|caught|captured`) and would have missed a harness calling it `error`, of which two exist,
  both currently correct. The blind spot hid nothing that day and would have hidden the next one.
  Both are the register's own §8.3d corollary from the unfamiliar side: **agreement between two
  methods is uninformative when they share a blind spot, and disagreement is always worth chasing** —
  only one of those is comfortable. The pattern is now structural (a wrapper whose last argument is a
  bare identifier) and the self-test carries an `error`-named fixture, so the widening stays observed
  rather than asserted: the old pattern does not match it, the new one does.

  **Then the gate's own printed count contradicted a number stated in prose, and found two more.**
  The gate printed 17; a summary written the same hour said 19, having counted files *containing* the
  guard rather than files that *needed* it. Reconciling the two exposed a second narrowness —
  `\w*Exception` does not match `System.InvalidOperationException`, so a harness writing the
  fully-qualified name was reported clean **while it wrapped**, safe only because a guard had been
  added there by hand. That is the worst way to be safe: the check said nothing and the protection
  came from somewhere the check could not see. Fixing it exposed a third, in the opposite direction —
  the guard pattern rejected the braced form `) { throw failure; }` and called a correctly guarded
  file unguarded. **Three narrowings in one sixty-character pattern**, each invisible to the audit
  that found the previous one, every one located by a count disagreeing with a count and none by
  reading. The corrected accounting is exact: 31 STA files = 18 wrapping + 12 plain rethrows + 1
  whose caught exception is the test's own subject. The lesson is not *write better patterns*: **a
  checker's window is itself a claim, and normally an unexamined one.**
- **Control:** partial, and honestly so. `tools/verify-harness-diagnostics.py` (DC-078) closes the
  one divergence that caused a defect, which is the administrative fix: it stops this variant without
  removing the coin flip. **The systemic fix is to delete the choice** — one implementation in
  `tests/Shared/`, the 31 call sites migrated, after which there is no second form to copy. That is
  a real piece of work across 31 files and is **not** done; it is recorded here with its measurement
  so the decision is sized rather than remembered.

  **The derived rule, which is the part that generalises:** when you find two forms of one thing
  coexisting and one is wrong, correcting the instances is not the fix — *the coexistence* is the
  defect. Ask what makes the wrong form representable, and remove that, or gate it. A gate is the
  right answer when consolidation is expensive, precisely because no amount of reading the directory
  would have told the reader which form to copy.
- **Status:** `partially-controlled`
