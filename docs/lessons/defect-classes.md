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

**Status counts:** controlled 10 · partially-controlled 17 · uncontrolled 0
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
- **Control:** `tools/verify-audit-log.py`, run in CI: no id may be claimed by more than one entry in
  `audit-log.jsonl` or `change-log.jsonl`. **Observed failing 2026-08-26** against a synthetic log
  carrying a planted duplicate — reported the id, the count and the fix, exit 1 — and green against
  the real logs (29 and 8 entries, 0 duplicates).
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
- **Instances (all 2026-08-27, all on the IPC transport):**
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

### DC-023 — A gate keeps passing because it runs a stale build of the thing it tests
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
