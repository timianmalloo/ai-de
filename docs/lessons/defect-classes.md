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

**Status counts:** controlled 16 · partially-controlled 21 · uncontrolled 0
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
- **Status:** `partially-controlled` — Core's side is complete and tested (bounded default, legible
  `PayloadTooLarge`, byte bounds on every read operation, the aggregated overview, and a reflective
  gate that catches the next operation). **Design's half is open:** rendering the overview and the
  "narrow your focus" state. The write side was measured too and needs nothing: an `IndexSummary` for
  28 scopes is **1,724 bytes**, three orders of magnitude below the frame.

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
