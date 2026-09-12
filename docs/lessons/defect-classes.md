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

**Status counts:** controlled 82 · partially-controlled 60 · uncontrolled 19
*(Not typed by hand — `python tools/verify-defect-register.py` fails when this line disagrees with the entries, and `--fix-counts` rewrites it.)*

**Recurrences since last review:** 7.
- **DC-008**, whose first control was scoped to one test project when the cause was not project-specific.
- **DC-001**, whose first control checked links between files and so could not see three classes cited by ID with no entry in this register.
- **DC-013**, which recurred the same day it was first caused, because the first occurrence was repaired without being registered at all.
- **DC-021**, which reached its *third* occurrence before it was registered at all: each repair was cheap enough to make asking why unnecessary.
- **DC-071**, whose first control lived inside a vendored file and was silently deleted six hours
  later by a routine `/updatepack` — the fix and the mechanism that erases it shared one file.
- **DC-019**, whose "generalisation to apply elsewhere" was prose: the lease was proven to bound a
  lane's file writes and the lane's *tools* crossed the same boundary unbounded (Ruling 71) — a
  memoir is not a control, and the sweep now names the boundary.
- **DC-131**, whose census control was taken with the right key and still closed the wrong
  question: the column said *foreign*, the operator saw the same screen, and the fifth report came
  (INV-0010; DC-155 is the half the control lacked).

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
- **Status:** `controlled` for the STA harness — one implementation, no second form to copy; the
  general shape (two conventions coexisting anywhere) stays `open`

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
- **Recurrence, 2026-09-11 — the control is DEFEATED, and it is defeated by passing.** Measured by node U2: `ui-craft-gate.py src/AiDe.App --gate` now exits 0 with **51 Majors**, so the gate no longer looks empty — it looks *productive*. The script hands its targets straight to the detector with **no `bin`/`obj` exclusion** (`ui-craft-gate.py:125, 290`, Verified), so a `src/AiDe.App` walk reads both `Web/composer.html` and the git-ignored copies of it under `bin/Debug/`. **The corpus is a function of build state — the one input nobody thinks of as an input.** 

  **A CORRECTION, recorded because the overstatement was mine and I published it first.** The node reported *"every one of the 51 is read from the copies under `bin/Debug/`"* and I relayed that as fact. **It is not right as stated** — `Web/` is in the same walk. Re-measured here: `src/AiDe.App/Web/composer.html` carries **19** hex literals and its `bin/Debug` copy carries **19**, identical content, so **a clean checkout still finds them**. The defect is *duplication and staleness*, not a phantom corpus, and it is smaller than I published. The further claim that *not one finding is in any XAML or C# file* is **not re-verified** and is carried Flagged. *A sub-agent's report does not promote a claim, and I promoted one inside the entry whose subject is unverified corpora.* Meanwhile `TheScan_CoversANonEmptyCorpus` — the control written for this exact class — **passes**, because the corpus is non-empty. It is also reported twice, and half of each pair describes whatever was last compiled. A control that asserts *a* corpus was read cannot distinguish the product's source from a build copy of part of it.
- **Why this is worse than the 2026-08-26 instance rather than the same size.** Then the gate was visibly silent and a seeded `#FF00FF` proved it. Now it is loud, and everything it says is about files that are not the source of anything — so its findings are **stale by construction** (they describe whatever was last built) and its green is **not a fact about the repository**. A clean clone gives it less to read; a stale `bin/` gives it findings from code that no longer exists. The corpus is a function of build state, which is the one input nobody thinks of as an input.
- **What the control has to become:** the corpus assertion must name the **kind** of file the rules are meant to read and the **root** they must be read from, and refuse a corpus discovered under a build-output directory. *Non-empty* was always the weaker half of the question; **the other half is whether it read the thing under review**, and only that half reaches this. **Ruling 48** sets the shape: targets pinned to committed source paths (`src/AiDe.App/Web`, never `src/AiDe.App`) rather than new exclusion machinery, landing before or with any threshold change, never after.
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
- **Recurrence 5, 2026-09-11 — `DC-133` was allocated twice, by two trees, on the same day.** I registered *a gate's failure threshold sits above the highest severity its rule set can emit* on `main` at `22f23d3b`; node F6, running in its own worktree at the same time, registered *a throw in one subscriber aborts a multicast event and is swallowed across an interop boundary* as `DC-133` on `feature/provider-config`. Both are correct against their own tree, and `verify-defect-register.py` passed in both, because it enforces one-entry-per-class **within a tree**. F6's renumbers on merge — and the tiebreak is *which tree is `main`*, not which class is better, which is worth saying plainly because it means **the allocation carries no information about merit**.
- **Recurrence 6, the same day, in a sequence that has NO allocator at all — and this is the larger half.** Programme decisions are cited by number as binding authority. **Eleven of them define nothing:** Rulings 1, 3, 5, 7, 32, 33, 34, 35, 37, 45 and 46 are cited across `docs/` with **no note that records what they said** (Ruling 33's only definition is a blockquote inside a plan file, which is close enough to read and not close enough to count). **The figure moved from eight to eleven when the gate replaced the grep that produced it** — *even the count of unmeasured things was unmeasured.* `Ruling 35` is cited **six times** as governing a dependency decision. `Ruling 7` is cited as forbidding an interface — including by a note warning a future session not to build the thing it forbids. **Anyone can assert what these said and nobody can check.**
- **How the two ends of recurrence 6 met.** `Ruling 45` was cited by a mockup and a review, and issued to a node as a mid-task correction, **before any ruling of that number had been made**; the Owner caught it and has now actually ruled it. `Ruling 46` was already cited in DC-130's control and in an audit entry — and the Owner, **unable to see that because no note records it**, allocated 46 again for a different decision. *The absence of the register is what caused the collision in the register.*
- **The asymmetry that kept this invisible:** defect-class ids have a gate, and `verify-id-allocators.py` says in its own header that the DC-032 collision was caught *"only because verify-defect-register.py happens to enforce one-entry-per-class for its own reasons."* **Ruling numbers have no equivalent accident.** Nothing reads them, nothing counts them, and a number is authoritative the moment it is typed.
- **Control:** `tools/verify-ruling-citations.py` — every `Ruling NN` cited anywhere under `docs/` must resolve to a decision note that **defines** it, and the sequence may not skip. Observed failing on 45, 46 and 48 before the note was written, and **its first version was itself wrong**: it required a `## Ruling NN` heading, so it reported two correctly filed notes (Rulings 36 and 38, which head themselves `# Decision note — Ruling NN`) as undefined. **A gate that calls a correct artifact missing teaches people to distrust it**, which ends with the gate switched off — DC-104 on its own first run. Nine numbers stay **frozen by name** as unreconstructable debt, and the list may only shrink. A decision that cannot be read is not a decision; it is a number with a reputation.
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
  2026-09-11 (**recurrence**, a different boundary) — the governed lane's **lease** was proven to
  bound the file writes the seams observe (`LeaseMonitor`, `LeasesRaiseSeamsTests`), and the lane was
  thereafter treated as governed. A lane's **tools** cross the same boundary by a different mechanism:
  `session/new` carried only `cwd` and `mcpServers: []`, so adapter 0.75.1 handed the SDK the
  `claude_code` preset with `Bash`, under a permission mode resolved from the user's, the
  repository's and the local settings — and the committed project settings allow `Bash(git push:*)`.
  Found by the Owner at F-3 (Ruling 71), not by a test. Fix: a typed `LaneSessionOptions` on
  `AcpLaneClient.NewSessionAsync`; the governed host pins `DisallowedTools: ["Bash"]`
  (`proof-lane-pin-ruling-71`, `note-lane-pin-spike`).
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
  For the lane boundary: `EverySessionOpenedFromSourceSaysWhatToolsItHolds`
  (`tests/AiDe.App.Tests/Conductor/TheGovernedLaneHasNoShellTests.cs`) sweeps every `NewSessionAsync`
  call in `src/` and fails on one that does not name its tools — the boundary, not the one site —
  observed red on the bare two-argument call before the pin; and the wire test
  `TheGovernedLanePinsBashOffThroughTheAdaptersMetaSlot` reads the outgoing frame.
- **Residual risk:** **live.** No containment for MSBuild task execution has been designed or tested;
  Component 1 is blocked on that decision. The same shape should be checked against the other proven
  controls in this repo — the MCP egress denial, the capability-revocation path and the fact-store
  immutability triggers each cover a named mechanism, not a boundary. For the lane: "no `Bash` tool"
  is not "no command execution" — `Monitor.command`, `REPL`, `Agent` subagents and the project
  hooks the lane loads from its own worktree remain unprobed (Security lens, 2026-09-11); the pin is
  contract-verified and wire-unobserved until the F5 Proof Pack records the frame and the tool-call
  names.
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
- **Recurrence, 2026-09-12 — the mutation loop's own binary.** SH-1's mutation-sense loop applied a
  mutant to a source file, built, ran the filtered tests, and restored the **source** in its
  `finally` — and did not rebuild. The next `dotnet test --no-build` over both full suites ran the
  last mutant's binary: `PerspectiveSetTests` red (the filtered-out perspective), `ShellBootstrapTests`
  red (five daemon tests, a stale `AiDe.Core.dll` beside the test host), `AShowEntryOpensTheKindOnce`
  red (the Show mutant). Every one read as a product failure. A rebuild from the restored sources
  turned all of them green. **A restored source is not a restored artifact under test** — the loop
  now rebuilds after its last restore, and the round-3 loop prints that it did.
- **Recurrence, 2026-09-11 — the fourth probe, and the FIRST where the stale binary did not announce itself.** `AiDe.App.ComposerProbe` was added by node F6 and **was not a `ProjectReference` of `AiDe.App.Tests`**, unlike the other three, each of which carries a comment explaining exactly why it is. On the merge the test ran a probe built **32 minutes earlier**, before the three handshake fixes that same commit landed, so `TheHandshakePushesExactlyOneHostInitPerMountAndFieldValuesSurviveIt` asserted 2 and measured **0**.
- **What made this one worse than its three predecessors.** Instances 1–3 were found by the probe being **absent**, which fails with an honest message naming the binary. This one was found by the probe being **present and old**, and a stale probe fails **as a product defect**: the assertion that broke was about `host.init`, so every reading pointed at the composer. **The identical tree passed in the worktree that had built the probe and failed in the checkout that had not** — same commit, same machine, opposite results — and nothing in the failure output named the binary. It survived a clean rebuild of the test project *and* of `src/AiDe.App`, because the probe is neither.
- **Two wrong diagnoses before the right one, both recorded because they are the cost.** First: a `NO RESULTS` run read as *"the test host almost certainly crashed"* — the real cause was a backgrounded gate loop **of my own** holding the DLLs, which is contention, not a crash, and the gate named a cause it had not observed. Second: 12 live `msedgewebview2.exe` processes looked like a leak until their command lines showed them belonging to Office Hub and Windows CBS, 48 hours old. **DC-131's lesson, one hour after registering DC-131.**
- **Control, and the sweep:** the `ProjectReference` is added with the rationale inline. Swept — **all five** probe projects (`TerminalProbe`, `CanvasProbe`, `WebHostProbe`, `ComposerProbe`, `AcpProbe`) are now referenced by a test project. The edge was **proved rather than assumed**: the probe's output was deleted, only `AiDe.App.Tests` was built, and the probe came back. *A missing probe fails loudly; a stale one fails as someone else's bug.*
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
  - 2026-09-02 — **written by the author of this entry's own summary, two hours after quoting it at
    a peer.** `DaydreamRecorder.Observe` returned `bool`, and `false` covered two states that are
    opposites: *assessed, and nothing fell short* (the system working) and *nothing was assessed at
    all* (the system seeing nothing). Both produce an empty `DaydreamSignature`, both write nothing,
    and both would have rendered as a quiet Daydream — so a repository whose agent episodes carry no
    evidence at all would have read as one where work is going well, permanently. Found by a peer
    wiring a test before the call site rather than after, and confirmed by MEASUREMENT: an episode
    with no Proof Pack scores `NotScored` with `floors=[] rubrics=[]` — a floor is an *observed*
    failure, so nothing can trip when nothing is observed. Fixed with `DaydreamObservationOutcome`,
    whose `NothingWasAssessed` is exactly this class's prescribed shape: a field that distinguishes
    "none" from "not looked at".
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
- **Residual risk — CORRECTED 2026-09-01, and the correction is itself an instance.** This entry
  read *"only PATH is inspected … any other oversized variable fails identically and is unchecked"*.
  That stopped being true at `192fb3d`: `EnvironmentHealth.Inspect` now scans **every** environment
  variable against `CmdVariableLimit` and names the oversized ones. The control was widened and the
  register was not, so the register described a weaker control than the one that shipped — and the
  design session, reading it carefully and citing it correctly, reported the gap as live while
  planning §3 of the session-registration spec. **The narrative and the artifact disagreed and the
  narrative was trusted**, which is the day's own family pointed at this file.

  **What IS still unchecked, and it is a different axis than the one recorded.** The inspection is
  **per-variable**. There is no check on the **total size of the environment block** — no sum
  anywhere in `EnvironmentHealth` — and that is the axis where a *total* limit makes one large value
  cost what several small ones do, so the loss lands on an unrelated variable that is individually
  fine. Anything that ADDS variables (§3 of the session-registration spec proposes nine) pushes on
  exactly the limit nothing measures, and the symptom would be indistinguishable from this class's
  original instance: a child process whose PATH is simply gone.

  **BISECTED 2026-09-01, and both halves came back different from what was argued.** A child was
  handed a controlled environment and asked what it received — the parent's copy is not evidence,
  which is this class's own rule turned on itself.

  **(1) The limit is on `NAME=VALUE`, not on the value.** Identical at four name lengths:

  | name length | largest value that survives | name + 1 + value |
  |---|---|---|
  | 3 | 8,186 | **8,190** |
  | 13 | 8,176 | **8,190** |
  | 40 | 8,149 | **8,190** |
  | 120 | 8,069 | **8,190** |

  The control compared the **value** against 8,151, so for any name longer than 39 characters it
  reported healthy on a variable cmd.exe drops — an 8,150-char value under a 40-char name passes and
  disappears. **A false clean, in the control that exists to catch this class.** Latent on the
  measured machine (longest name: 34 of 76 variables) and it stops being latent the moment something
  adds longer names, which is what §3 of the session-registration spec proposes. Fixed to compare the
  pair against the measured 8,190; two tests observed failing on the old comparison.

  **(2) There is NO total-block limit. It is a PATH limit — and the route to that answer is the
  point.** Confirmed 2026-09-01 by two independently written probes, each with a floor check:

  | padding | 20,000 | 32,000 | 33,000 | 40,000 | 60,000 |
  |---|---|---|---|---|---|
  | into **PATH** | ran | ran | **failed** | **failed** | **failed** |
  | into a **non-PATH** variable | ran | ran | ran | ran | **ran** |

  A 60,000-character non-PATH variable survives a PowerShell-hosted launch intact; a 33,000-character
  PATH breaks it. PowerShell resolves the command it was handed **through PATH**, so an oversized
  PATH stops it finding anything — the earlier ~32,650 "block limit" was a PATH property read as a
  block property, because the probe put all of its padding into PATH.

  **The history is worth keeping, because the discipline and the answer pointed opposite ways.** This
  entry first claimed the hazard "does not bind", on the strength of direct `CreateProcess` and
  `cmd.exe /c` — **paths the product does not take**, since it uses `PowerShellHostedAgent`. That
  claim was withdrawn as unsupported, correctly: three attempts to measure the real path failed **in
  the instrument** (a one-hop probe that structurally cannot see a second-hop loss, then a two-hop
  probe returning zero output at every size including one variable — a broken probe reporting
  itself). The conclusion happens to be right, and the only way to know that was to fill the one cell
  neither session had: a **large non-PATH variable** on the PowerShell path. **Withdrawing was still
  correct — asserting it beforehand would have been luck**, and a lucky claim that stands is
  indistinguishable from a measured one until it matters.

  So: no size guard was added. A block-size check would refuse launches that work, and a control that
  fires on correct behaviour gets switched off, taking the real check with it. What survives is "keep
  every added value short" — no longer a defence against a block limit, now what keeps the addition
  irrelevant to any limit found later — and the rule that **nothing may be added to PATH**, which is
  the variable that actually binds.

  **(2b) Superseded, kept visible: the WRONG-PATHS reasoning.** Direct
  `CreateProcess` and `cmd.exe /c` show no total limit below **13,010,087 wide characters**, and on
  that evidence this entry briefly said the hazard "does not bind". **It does not follow.** The
  product does not use either path: `ShellIntegrationMode.PowerShellHostedAgent` hosts the agent
  under PowerShell, and the design session measured a total-block cut-off at **~32,650** on
  parent → PowerShell → child, consistent with the documented 32,767 `CreateProcess` block. Its
  failure mode is worse than the shim's: `Process.Start` does not throw, the process starts, and it
  produces nothing.

  Three attempts to reproduce that here failed **in the instrument** — a one-hop probe that cannot
  see a loss occurring at the second hop, then a two-hop probe returning zero output at every size
  including one variable, which is a broken probe reporting itself. So this entry records the design
  session's number as theirs, unconfirmed by a second method, rather than restating it as agreed.
  Two measurements are one measurement when the second one did not run.

  The withdrawn claim is left visible on purpose: **a hazard measured away on a path the product does
  not take is more dangerous than one never measured**, because it retires an argument that was
  correct.

  **The instrument was wrong first, twice.** Its two readers returned different shapes and the shared
  parser measured the length of a length, reporting a per-variable limit of "1 character" that looked
  like a finding; and its search ceiling was returned silently as though it were an answer, which is
  why the ceiling case now prints "NOT a measurement". Both were caught by a number being absurd
  rather than by review.

  So the message still says "may be dropped", now because the drop depends on the name rather than
  because the number is unmeasured.
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
- **Recurrence 2 (INV-0009, 2026-09-11) — the restored session-document pane.** A saved layout
  restores `session-document:<id>` surfaces before any session is reopened, so
  `SurfaceContentFactory.SessionDocument` builds the *"No session is open"* island for each. When
  the operator then reopens that session, `OpenSessionDocument` registers the live document — and
  `WorkbenchAdapter.Render` **reuses** the island, because the surface id is already in the layout
  and nothing called `Invalidate` for it (the DC-029 reconcile, doing its job). The pane the
  operator clicked keeps saying no session is open while the shell announces *"Session opened."*
  **Observed red:** the same oracle as DC-084's recurrence — `live-document-in-view=False`,
  `renders='No session is open. Create one from File → New Session.'` after the reopen. The first
  control (read live at use time) was applied to the Explorer graph and never to this pane; the
  class control is the oracle, and the fix is to invalidate the surface when a document is
  registered for an id the layout already holds (INV-0009 Phase 2).
- **Status:** `controlled` — the live-read + refresh-on-entry fix is landed for the Explorer
  graph; the restored-pane instance is fixed by INV-0009 Phase 2 (`WorkbenchShell.RegisterSessionDocument`
  names a restored pane for rebuild when the live document registers, and only when the pane holds
  something other than a live document) and Phase 2b (`ReviveRestoredSessionDocuments` at
  workspace-open, so the island stands only for a session whose `session.json` is gone) — oracles
  `AReopenedSessionIsShownAndItsComposerIsBound` (exit 32 → 0) and
  `ARestoredSessionDocumentIsRevivedAndBoundAtWorkspaceOpen` (exit 34 → 0).


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
- **Recurrence, 2026-09-04 — the residual risk was the recurrence.** `WorkbenchShell.McpServerPath()` resolves `<BaseDirectory>/mcp/AiDe.Mcp.exe` with its own `AfterTargets` copy: the same shape, a second time, four days later. The gate did not catch it because the gate checked *the daemon*, and the previous entry had said so in writing — *"another sibling resolved relative to `BaseDirectory` later would need adding; nothing detects a new one automatically."* **A residual risk that names its own recurrence and is left open is a prediction, not a control.** Worse here than for the daemon, because the MCP failure is silent: `McpConfigWriter` refuses to name a binary that is not there, so a published shell simply never writes `.mcp.json`, every agent falls back to the JSONL floor, and nothing errors or logs. The daemon's absence at least said *"This workspace could not be opened"*.
- **Control (extended, 2026-09-04):** `verify-published-layout.py` now checks a **list** of sidecars, each read from its own source — and, before publishing, greps every `src/AiDe.App/**/*.cs` for *any* `X() => Path.Combine(AppContext.BaseDirectory, …)` and **fails on one it has not been told about**. That is the half that closes the class rather than the instance: adding a sidecar is now a decision (add the row, name what a user sees when it is missing) instead of an omission nobody notices. Both halves observed failing before being believed — the copy target repointed at a target that never fires (reported the missing path *and* that the exe was published flat at the root), and a third `PluginsPath()` spliced into the shell (reported by name and file).
- **Residual risk:** discovery is scoped to `src/AiDe.App` and to the `=>`-bodied form. A sidecar resolved in a block body, in another project, or by composing the path in pieces is still invisible. Narrower than before — it was "any new one at all" — and the remaining shapes are ones nothing in this repo currently uses.
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
- **Second instance, 2026-09-09 — a near-miss, and it names the ingredient the entry was missing.**
  An agent ran `git stash push -u` inside a Phase-1 worktree to check a gate against the branch
  point. Two facts combined: the stash is repo-global (this class), **and
  `tools/verify-derived-views.py` WRITES INTO THE WORKING TREE as a side effect** — it generated
  `docs/api/AiDe.Core.AgentPlane.md` while merely *checking*. `git stash pop` then failed with
  *"could not restore untracked files."* Recovery was complete and verified byte-identical, so
  nothing was lost — but the phase happened to be running one agent at a time. **With a second agent
  in the repo it would have reached into their work.** The ingredient this class did not previously
  name: *a verification gate with a filesystem side effect turns the repo-global stash from a
  latent hazard into an active one.* Compare against HEAD with `git show` or a second worktree,
  never by stashing.
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
- **Instance (fleet, ai-forward drm-0009 COORD-D):** `2026-09-04` -- the same class, now with a number: 24 session-start events against 1 session-end (96% never closed), and 131 claims against 117 releases with 33 (25%) never released. 86% of the releases that did happen landed while the lease was still live, so the verb works and the exit is what is skipped; every abandoned lease was reclaimed by TTL, not by protocol.
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
- **Recurrence, 2026-09-10 (lane-rename follow-up).** The 2026-08-31 control lived *inside* the
  vendored generator (`audit-log.py`'s `project_name()`, reading the git remote first). `chore:
  update AI-Forward Pack: revision 45 -> 59` (`7bb1892`), six hours later the same day, overwrote
  `audit-log.py` wholesale from the upstream pack source — which did not yet carry this repo's
  local fix — and silently deleted `_remote_project` and its call, with no test failing, because
  the only executable guard (`verify-derived-views.py`) was run from `ai-de` itself that day and
  the defect is invisible from inside the one checkout named correctly. `verify-derived-views`
  then reported `docs/audit/audit-data.js` **falsely stale** in three different worktrees this
  same session (`ai-de-chore-lane-rename`, and two more the same class of agent reported working
  in), each read as "the artifact is wrong" rather than "the check is wrong" — exactly DC-060's
  message, misattributed. It also came within one comparison of a **false negative**: it silently
  corrupted `verify-derived-views --self-test`'s own `timestamp_only` assertion (a check meant to
  confirm a volatile field alone never fails the gate) by contaminating `check(root)` with the
  same unrelated finding, on every run from a mismatched worktree — a control's self-test, broken
  by the very class the control exists to catch.

  The pack itself independently arrived at a *different*, compatible fix for the same class,
  registered as its own `PACK-P`: not a smarter generator default, but every **caller** pinning
  `--root docs --project <name>` explicitly (`.agents/artifacts.yml`'s committed registry,
  `coord-core.py`'s `_canonical_project`). That fix survives a vendored-file overwrite because it
  lives in the *caller*, never in the file the pack update replaces. `verify-derived-views.py` —
  a repo-local tool, not a vendored pack script — was the one caller still invoking `render` bare.
  Fixed there the same way, with `--root docs --project ai-de` pinned at the call site (matching
  the values already committed everywhere else), never by teaching the gate to tolerate a
  mismatch — the artifact really would be wrong if regenerated that way.

  **Refined shape, generalizing past this one call site:** *a verification gate reconstructs an
  artifact with different arguments than the ones that built the committed copy, so it measures
  its own invocation rather than the artifact, and reports the difference as the artifact's
  fault.* WT1a (a session always works in its own worktree) turns this from a latent mismatch
  into the everyday case: the one directory where the bare invocation is honest — the repository's
  own canonical name — is, by policy, the one directory a session is never supposed to be
  working in.

  Sibling sweep of `verify-derived-views.py`'s own `VIEWS` table: `docs-graph.py derive` (no
  `--project`) is not an instance — `project_identity()` reads the value already committed in
  `docs-index.js` before falling back to the directory, so it is self-healing wherever the file
  already exists (an unrelated, thinner exposure only on a first-ever derive, out of scope here).
  `build-doc-viewer.py` hardcodes the literal string `'AI-DE'`, never derived from the directory.
  `api-reference.py` embeds no project identity at all. **One instance in this table, not two.**

  Related, not fixed here: `tools/regenerate-derived.py` — the single documented entry point for
  "regenerate everything, in the right order" (DC-082) — never calls `audit-log.py render` at
  all, so following its own instructions after appending an audit entry does not regenerate
  `audit-data.js`. Different failure shape (an omitted call, not a wrong argument to a present
  one); flagged for separate attention.
- **Control (this recurrence).** `tools/verify-derived-views.py`'s own `VIEWS` table now pins
  `--root docs --project ai-de` on the `audit-log.py render` command, matching the values
  `.agents/artifacts.yml` and `coord-core.py` already commit to — one decided value, cited, not
  reinvented. `self_test()` gained a permanent, environment-independent reproduction: it
  provisions a throwaway `git worktree` under a name guaranteed never to equal the canonical
  project, runs `check()` rooted there, and fails if `docs/audit/audit-data.js` is reported stale
  — so this recurring exactly the same way fails the self-test regardless of which directory a
  future session happens to run it from, including `ai-de` itself. **Observed failing 2026-09-10**
  before the fix (see `docs/proof/pp-lane-rename-ruling-15.md` and the follow-up proof pack for
  the transcript); passes after.
- **Closed at the root, upstream, 2026-09-10 — and the named residual is what closed.** The status
  below said the general shape was *"prevented by convention and a code comment, not by
  construction; nothing stops a **new** caller from repeating DC-071 verbatim."* That is now false,
  which is the only kind of evidence that should move a status.
  - **The pack owned the defect, not this repo.** An upstream sweep found **five** implementations
    of one quantity: three wrong (`audit-log.py`, `docs-graph.py`, `pack-apply.py` — the last
    stamping a *target* repository's artifacts) and **two already correct, both citing this class
    in their own docstrings.** The pack had diagnosed it, written the right resolver twice, and
    never wired it to the three generators that actually commit artifacts. `docs-graph.py` was the
    worst: it read its own previous output back as the answer first, so **one bad render was
    self-perpetuating.**
  - **The fix is a shared resolver with a configuration-first ladder** — explicit `--project`, then
    `remote.origin.url`, then the primary checkout via `git rev-parse --git-common-dir`, then the
    directory name as a last resort. Forgetting the flag now yields the **right** answer, so the
    residual is closed **by construction** rather than by remembering.
  - **And by a sweep test that fails on any *new* pack script** deriving the project from its own
    directory — so the class cannot re-enter through a caller that does not exist yet, which was
    exactly the hole named below.
  - **Why upstream is the closure and a local fix was not.** The 2026-09-10 recurrence happened
    because a pack update *overwrote ai-de's own local fix*. The repair now lives in the thing that
    does the overwriting. A fix held only in the consuming repo is a fix waiting for the next
    `/updatepack`.
  - **Proven in ai-de, not inferred from installed code:** a real linked worktree named
    `ai-de-chore-pack-p-proof` ran `audit-log.py render` with **no `--project` flag** and generated
    `"project": "ai-de"`. *That proof took two attempts* — the first was a **false negative**,
    because `coord worktree new --base HEAD` invoked from inside a linked worktree resolved `HEAD`
    against the **primary**, creating a tree at the wrong commit carrying the old script. Reported
    upstream: it is this class's own family, one axis over — a tool inferring **revision** from the
    wrong tree where this class inferred **identity** from it.
- **Status:** `controlled` — the resolver is shared, the default is correct without a flag, a sweep
  test fails on any new offender, and the whole of it was observed working from a real linked
  worktree in this repository


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
  NodeContentAsync (ADR-0018 node-content-reader-contract); swapping this field for the Core-backed source is the whole
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
  a real piece of work across 31 files. **Done 2026-09-01**, and the measurement is how it is known
  to be done: **32 files declaring their own STA thread became 2** — `Sta.cs`, which is the harness,
  and one test whose caught exception is its own subject and therefore was never a harness at all.
  There is no longer a second form to copy.

  **What the consolidation preserved, and the one thing it changed.** Every migrated file keeps its
  own timeout, passed through rather than unified: a test that waited 30 seconds and one that waited
  60 were making different bets about what they were driving, and collapsing them would be a
  behaviour change wearing a refactor's clothes. Twenty-eight matched exactly. **Three did change** —
  `SurfaceContentTests`, `TerminalViewTests` and `DockThemeAccentsTests` joined with **no timeout at
  all**, so a hang there hung the whole suite with nothing to read; they are now bounded at 60s.
  Stated rather than folded into "no behaviour change", because that is the sentence a reader would
  otherwise trust.

  **And it corrected the control that had just been written.** The granularity check added hours
  earlier fired on `Sta.cs` itself: the shared harness legitimately stands up two threads — a plain
  one and a dispatcher-pumping one — whose failures funnel through a single rethrow, so nothing can
  mask. The rule was imprecise rather than the file wrong, and it is now scoped to files that hold
  `[Fact]`s, since the masking risk needs two harnesses each carrying their own verdict. Scoped by
  that property rather than by naming the harness file, because an allowance list has the catch-all
  property that hides a miss. The self-test's fixture caught the change immediately by going red —
  it had no `[Fact]`, so it no longer resembled its subject.

  **The derived rule, which is the part that generalises:** when you find two forms of one thing
  coexisting and one is wrong, correcting the instances is not the fix — *the coexistence* is the
  defect. Ask what makes the wrong form representable, and remove that, or gate it. A gate is the
  right answer when consolidation is expensive, precisely because no amount of reading the directory
  would have told the reader which form to copy.
- **Status:** `partially-controlled`

### DC-080 — The measurement was real and the noun was wrong

- **Shape:** a number is obtained correctly, by a probe that ran, against a real system — and then
  recorded, compared or acted on as a **different quantity** than the one measured. Nothing about the
  number is false. The reading, the instrument and the arithmetic are all sound. What is wrong is the
  label, and a label is not something a re-run can check.
- **Signature:** the units in the variable name do not match the units of what produced it. A probe
  that varies one input and a conclusion stated about a different one — *"PATH chars"* measured,
  *"block size"* written; a `.NET` exe exercised, a `.cmd` shim claimed; the value's length compared
  against a limit that governs `NAME=VALUE`; a *count of files containing a guard* reported as a
  *count of files needing one*. The tell is a sentence of the form "so the limit is X" where the
  probe never varied X.
- **Why it survives:** every check that exists is pointed at the number. Re-running reproduces it.
  A second instrument agrees, if it shares the mislabelling — and it usually does, because the second
  instrument is written from the first one's conclusion. Review reads the value and the code that
  produced it, both of which are correct. **The defect lives in the gap between the probe and the
  prose, and nothing in the toolchain reads both.**
- **Instances, all 2026-09-01, all within one day:**
  1. **Evidence vs Detail** — a count of one rendered quantity reported as another.
  2. **`.NET` exe vs `.cmd` shim** — DC-027 declared "does not reproduce" after testing a path that
     was never the one in the report. cmd passing a block through to a child is not cmd expanding a
     variable inside a batch file.
  3. **PATH vs block** — a probe that padded PATH, whose result was written into the code as
     `PowerShellHostedBlockLimit` and compared against a computed block size. Two different
     quantities, one named with the other's units, producing a guard that would have **refused
     launches that work** — and a control that fires on correct behaviour gets switched off, taking
     the real check with it.
  4. **Guard-carrying vs guard-needing** — 19 files reported as the count of wrapping harnesses when
     17 wrapped; the two extra carried a guard they did not need.
- **Control:** none mechanised, and the reason is the class itself: a gate can compare a number to a
  number, and this is a number matched to the wrong *name*, which is a semantic property. What
  demonstrably worked, four times out of four, is **a second party asking what the probe actually
  varied** — every one of these was caught by the other session, never by the author, and never by a
  re-run.

  The cheapest defence found so far, and it is a habit rather than a gate: **state the manipulated
  variable in the sentence that reports the result** — "padding PATH failed at 33,000" cannot be
  mistaken for a block limit, while "the limit is 32,650" can. A number that carries its own
  independent variable is much harder to relabel.

  Related: `session-contracts.md` §8.3d's corollary. Two methods agreeing is corroboration only if
  they have different blind spots — and a shared *mislabelling* is a shared blind spot that no amount
  of re-measurement will surface.
- **Status:** `uncontrolled` — recorded, with the habit above as the only defence and a second reader
  as the only thing observed to work

### TERM-INPUT-MODE — input encoded without honouring the mode the child enabled
- **Signature:** a key produces a correctly-formed escape sequence that the program still does not
  recognise — the classic tell is "arrows work in the shell but do nothing in a full-screen TUI
  (Claude Code menu, vim, less)". The bytes are right for *normal* mode and wrong for the *application*
  mode the child turned on with a DEC private sequence the terminal ignored.
- **Why it survives:** the shell (normal cursor mode) works perfectly, so typing and history feel
  fine; only a program that sends `ESC [ ? 1 h` (DECCKM) and then expects `ESC O A` exposes it, and
  unit tests that assert one variant of the sequence pass. A parser that ignores DEC private modes
  "as a unit" looks principled while silently dropping the one mode that changes input encoding.
- **Instances:** 2026-09-02 — smoke 9-2 #1: arrows dead in the Claude Code session; DECCKM never
  tracked, `ForKey` always sent CSI. (Related, same subsystem: WPF `OnKeyDown` let directional focus
  navigation consume an arrow before it reached ConPTY — captured by moving to `OnPreviewKeyDown`.)
- **Control:** the parser tracks every **input-affecting** mode (DECCKM now; DECKPAM, bracketed
  paste, mouse next) and the input encoder is **parameterized by that state**, with a test asserting
  **both** variants of each mode-sensitive key (`VtParserTests` DECCKM set/reset; `TerminalInputTests`
  CSI-vs-SS3). A mode-sensitive key with a test for only one variant is the gap.
- **Status:** `partially-controlled` — DECCKM controlled; the remaining input-affecting modes are
  the T-T2…T-T5 slices.

### UI-LISTITEM-FG — list rows fall back to the default control foreground, not the theme's
- **Signature:** text in a `ListBox`/`ListView` renders near-invisible (dark-on-dark in a dark theme)
  while hand-built panels beside it are legible. The rows inherit the default `ListBoxItem`
  foreground (a system control colour), not the window's text brush, because no `ListBoxItem` style
  sets it.
- **Why it survives:** structure tests pass (the right rows are present), the panel *has* a theme,
  and a developer who built the adjacent surface by hand (setting `TextBrush` explicitly) never sees
  the ListBox path — the contrast failure is only visible in the rendered app, which no headless test
  exercises. It was mis-diagnosed once *from the code* as "already uses TextBrush" (an E11/E15
  violation: inferred instead of proven) and only the screenshot settled it.
- **Instances:** 2026-09-02 — smoke 9-2 #4: provenance/evidence, board, leaderboard, search all
  dark-on-dark; contexts legible because it builds its own `TextBrush` TextBlocks.
- **Control:** a global `ListBoxItem` foreground style keyed to the theme's `TextBrush`; and, at the
  rung above, the `ui-craft-gate.py` `low-contrast` rule run against the **rendered** surface (a
  code-only linter cannot see an inherited foreground). Prove the rendered surface, never infer text
  colour from the DataTemplate (E11).
- **Status:** `partially-controlled` — the global style holds it; the rendered-contrast gate is the
  remaining rung.

### UI-EMPTY-STATE-BUILD — an ordinary "no data yet" empty state accuses the build
- **Signature:** a surface that simply has nothing to show until a precondition is met (a workspace
  is opened, a store is attached) renders a message that names a *build* defect — "… is not available
  in **this build**" — so a user reads a routine empty state as a packaging/version failure and
  reports the product as broken. In smoke 9-2 the Explore pane said *"'Explore' is not available in
  this build."* while the graph pane beside it correctly said *"No workspace is open. Open one to see
  its graph."* — two messages, one cause (no workspace), one of them lying about the cause.
- **Why it survives:** the surface genuinely returns *something* (a legible TextBlock), so
  structure/legibility tests pass; and the message is factually true in the narrow sense that the
  surface isn't showing — nothing asserts that an empty state names the **real cause and the real
  action** rather than the worst-sounding one. A shared `Unavailable()` fallback used for both
  "unknown surface kind" (correct) and "no workspace yet" (wrong) hides the conflation.
- **Instances:** 2026-09-02 — smoke 9-2: the `"view"/"inspector"` evidence surfaces (Explore, Domain,
  Provenance) fell through to `SurfaceContentFactory.Unavailable` when `queries` was null.
- **Control:** a dedicated `WorkspaceNeeded()` empty state ("No workspace is open. Open one to see
  {Title}.") for the workspace-dependent kinds, in the same voice as the graph pane; a test asserts
  the no-workspace surface says *workspace* and **not** "not available in this build". The wider rung:
  an empty state names its cause and its next action, and "not available in this build" is reserved
  for a genuinely unbuilt surface kind.
- **Status:** `controlled` — the `WorkspaceNeeded` split + the flipped assertion in
  `SurfaceContentTests` hold it.

### ZONE-LAYOUT-NO-MIGRATION — a new default surface never reaches operators who arranged their workbench
- **Signature:** a surface added to the default layout appears for fresh installs but is **invisible**
  to every user who has ever arranged their workbench — because the persisted layout on disk does not
  contain it and nothing upgrades it. The tree `LayoutStore` solved this with a versioned migration
  chain (`CurrentSchemaVersion` + `LayoutMigrations`, adding a surface *beside an anchor*); the live
  `ZoneLayoutStore` did **not** — it only compares `SchemaVersion != CurrentSchemaVersion` and, on any
  mismatch, **discards the whole saved layout and resets to default**. So the zone store has two modes
  and no third: keep the old arrangement (and never show the new surface), or nuke the arrangement.
- **Why it survives:** every test passes (the default *does* contain the surface; a fresh load *does*
  show it); the gap only bites a user with a pre-existing on-disk layout, which no headless test has.
  It is the exact "invisible to the operators who arranged their workbench first" lesson the tree
  migration comments call out — but the zone store, added later, never grew the same mechanism.
- **Instances:** 2026-09-02 — the Ledger surface (US: "the ledger viewable too") was added to both
  defaults + a tree v3→v4 migration, but existing **zone** users get it only via the **Reset layout**
  button, because the zone store cannot add a surface to a saved arrangement.
- **Control:** for now, additive-only — a new watcher/read surface is a **restorable kind**
  (`SurfaceContentFactory.KnownKinds`, so it survives load) and is documented as reachable via Reset
  layout. The real fix (the higher rung, not yet built): give `ZoneLayoutStore` a migration chain like
  the tree store's — a versioned `AddSurfaceBeside` that upgrades a saved zone layout in place instead
  of discarding it. Until then this is `partially-controlled`.
- **Status:** `partially-controlled` — new surfaces reach fresh installs and tree layouts (migration);
  existing zone layouts need Reset layout until the zone store gains a migration chain.

### WATCHER-REFRESH-FULL-RENDER — a periodic content refresh does a full layout render, disturbing every other pane
- **Signature:** a pane whose content updates on a timer (a live read surface) refreshes by calling
  the workbench's **full re-render**, which swaps the whole docking layout wholesale. Every *other*
  pane is re-parented as a side effect: a hosted child (a WebView2 graph canvas) re-fires its
  `ResizeObserver` and visibly **re-fits/re-draws**, and each stack's active tab is **re-seated from
  the model** — so a user sitting on a non-default tab is snapped back to the default. The more often
  the refresh fires (live agents heartbeating → the watcher fingerprint churns every ~2s), the more
  the screen "keeps refreshing" and the less the user can stay on the tab they chose.
- **Why it survives:** each piece is individually correct — the refresh *does* update the rows, the
  re-render *does* preserve surfaces (nothing is lost), and RestoreSelection *does* restore the
  model's active tab. Nothing is broken in isolation; the defect is that a *content* refresh reached
  for a *layout* operation, and the cost is only visible in the running app with live activity (no
  headless test rendered a page or watched a tab over time). Amplified precisely when the product
  started working (agents launch → they heartbeat → the fingerprint churns).
- **Instances:** 2026-09-02 — smoke video: after Reset layout, the graph re-fit continuously and the
  active tab snapped back to Graph whenever the user clicked Sessions/Leaderboard, once Claude Code +
  Copilot were live. `RefreshWatcherPanesOnUi` called `Adapter.Invalidate + Adapter.Render()`.
- **Control:** `WorkbenchAdapter.RefreshInPlace(ids)` — rebuilds only the named panes' `Content`
  without swapping `Manager.Layout`, so no other pane is re-parented and selection/focus are
  untouched; the watcher refresh calls it instead of `Render()`. Two STA adapter tests assert the
  layout root is **not** swapped (graph content identical) and the active tab is **preserved** across
  a refresh — both would fail against the old `Render()` path. Related root cause not yet fixed: a
  **mouse tab-click does not update the model's active index** (no `ActiveContentChanged` handler), so
  a full `Render()` from *other* events can still snap the tab back — see the follow-up below.
- **Status:** `controlled` for the watcher-refresh path (in-place); `partially-controlled` overall
  until mouse tab-selection is synced into the model so any `Render()` preserves the user's tab.

### UX-SESSIONS-GRAVEYARD — a live-status list dominated by dead history buries the live state
- **Signature:** a surface whose job is "what is happening **now**" (a session/status list) renders
  every record it has ever seen in flat store order, so a long-running workspace fills it with
  **ended/historical rows** — often near-identical — and the one live item that matters is buried and
  effectively invisible. The rows are individually well-formatted; the *list* has no ordering or
  grouping by liveness, so quantity of dead history wins over relevance.
- **Why it survives:** each row is correct and legible (the per-row presenter was the thing that got
  attention), the empty and single-row cases test fine, and the failure only appears once *many*
  ended records accumulate — which a fresh test fixture never has and only a long real session shows.
  Amplified exactly when the product starts working (agents actually launch and end, piling up
  history). In the 2026-09-02 smoke video: 15 identical `× Ended` rows, one `~ Stale`, zero `✓ Alive`.
- **Instances:** 2026-09-02 — the Sessions surface (`SurfaceContentFactory.Sessions`) iterated
  `pane.Rows` in store order with no liveness partition; the live collaboration the user was testing
  was invisible under the ended terminals.
- **Control:** `SessionRowPresenter.Partition(rows)` (pure, tested) splits **Live (Alive→Stale)** from
  **Ended**; the renderer **leads with the live rows** and **collapses the ended history** behind a
  keyboard-operable Expander ("N ended session(s)", collapsed by default); the empty case shows a
  teaching state naming the first action. A surface test asserts the live row is up top and the ended
  pile is collapsed. The wider rung: a status/live surface orders by relevance (liveness/recency), not
  by store order, and de-emphasises or collapses history.
- **Status:** `controlled` for Sessions; the pattern (order-by-relevance, collapse-history) should be
  applied to any future live/status list.

### DC-081 — A setup step declines, and its `return` cancels the work it was preparing for

- **Shape:** a script does setup, then the real work. The setup is written to **give up gracefully**
  when it cannot complete — a correct and deliberate design — and it gives up with a `return`. The
  work is appended to the same script, so the graceful decline silently cancels it. Nothing errors:
  the setup did what it said, the host exits its script normally, and whatever keeps the session
  alive afterwards leaves something that looks like a plausible result.
- **Signature:** two concerns concatenated into one script, one of which has an early exit, and a
  host flag that hides the truncation (`-NoExit`, `|| true`, a `finally` that reopens a shell). The
  tell in review is a **bare `return` at the top level of generated source** — inside a function it
  scopes, at the top of a script it terminates. The tell in use is that the feature is *entirely*
  absent rather than broken: no error, no partial behaviour, just the environment it was launched in.
- **Why it survives:** the decline is conditional on the environment, so it does not reproduce where
  it is developed. Here the condition is PowerShell's own screen-reader detection — inside the
  product's ConPTY the host prints *"detected that you might be using a screen reader and has
  disabled PSReadLine"*, so `Set-PSReadLineKeyHandler` is genuinely absent and the guard fires
  **every time**; from an ordinary console PSReadLine loads and the bug is **invisible**. A
  hypothesis naming this exact line was raised and **wrongly dismissed** hours earlier because the
  check was run from a normal shell and reported `PSREADLINE-PRESENT` — DC-080 in the diagnosis of
  DC-081.
- **Instance:** 2026-09-02 — "New Claude Code session" opened a plain PowerShell prompt at the
  workspace root while the status bar reported *"Claude Code session opened."* Found from a user's
  screenshot, in which the terminal was printing the reason the whole time. Measured against the
  shipped binary, running the generated command with `PSModulePath` empty to reproduce the condition:

      PSReadLine PRESENT : agent ran = True
      PSReadLine ABSENT  : agent ran = False

  Every downstream symptom followed from it: the session never reached an agent prompt, so it never
  became `Ready`, so `ReadyPromptTargets()` excluded it and the prompt-draft dropdown was empty. **One
  cause, reported as three unrelated complaints.**
- **Control:** `AgentCommandLine` wraps the integration in `& { … }`, so its `return` scopes to the
  block and the agent invocation is unreachable-from-it by construction. Three tests in
  `AgentLaunchSurvivesIntegrationTests`: the agent runs with the integration declining, the agent
  runs with it installing (so "delete the integration" does not pass), and the integration is still
  *attempted* — a DC-016 guard, since dropping it would satisfy both behavioural tests while removing
  the OSC-133 reporting that readiness, dispatch and the target list all depend on. **Observed
  failing on the shipped shape**, with the message naming the cause.
- **The generalisation worth keeping:** *a step that is allowed to decline must not be able to
  cancel what it was preparing for.* Where two concerns share a script, the optional one belongs in
  its own scope — not because its logic is wrong, but because "I could not help" and "stop" must not
  be the same statement.
- **Status:** `controlled`

### DC-082 — A published figure is copied from a source that keeps moving, and nothing binds it back

- **Shape:** a document states a count — artifacts indexed, tests that must execute, entries in a
  log, symbols on a public surface. The count was **correct when it was written**, taken from the
  source by hand. The source then changes, as it is supposed to, and the document keeps stating the
  old number with exactly the same authority. Nothing errors, nothing renders as missing, and the
  page still says the numbers were counted rather than estimated — which was true once.
- **Signature:** a literal quantity in prose or markup whose only tie to its source is that a person
  once read them together. The tell is a document that *advertises* its rigour with figures: the
  more a page leans on "counted, not estimated", the more a stale count costs it. A second tell is a
  figure that moves for reasons unrelated to the document — another session's commit, a rebase, a
  test added elsewhere.
- **Why it survives:** it is invisible at the moment of writing, because the number is right then.
  Every review reads the page and not the source, so a reviewer confirms internal consistency and
  cannot detect drift at all. And the fix everyone reaches for — "remember to update it" — is a
  procedure, which is what CI6 means by a memoir: it has no failure mode, so it cannot be observed
  not working.
- **Instance:** 2026-09-02 — the public site's six landing figures. Written from measurements, then
  stale **three times inside one session**: once when a rebase raised the App test floor from 319 to
  330, again when a later rebase brought another session's audit entries, and again when both moved
  together (five of nine bound figures wrong at once: test floor 1,697→1,728, ledger 499→503,
  symbols 1,715→1,733, audit entries 371→375, artifacts 303→304). Each time the page was corrected
  by hand and each time the correction was itself immediately stale. The site's own craft review had
  **already recorded this as an open gap** while it was still being maintained by hand — a recorded
  gap is not a control.
- **Control:** every figure in `site/*.html` carries `data-figure="<name>"`, and
  `tools/verify-site-figures.py` computes each name from its source of record and compares.
  `--update` rewrites them; the Pages workflow runs it **without** `--update`, so a stale page fails
  the build rather than publishing. **Observed failing on the shipped shape** — its first run
  reported all five stale figures with both values, before any of them were fixed. Two DC-016
  guards: the run fails when no `data-figure` element is found at all (dropping the annotations
  would otherwise pass silently, which is the same defect wearing a different hat), and it fails on
  a `data-figure` name the script cannot compute rather than skipping it.
- **The generalisation worth keeping:** *a number you publish is a claim, and a claim with no path
  back to its source decays without telling anyone.* Either bind it to what it counts, or do not
  state it — because the page cannot say "counted, not estimated" on its own authority once the
  counting was a one-off.
- **WHEN it drifts, which turns four recurrences into one rule (2026-09-02).** The gate fired four
  times in one afternoon on the same author, and every time the cause was **ordering inside a single
  commit**, not neglect: regenerate the derived views, then append the audit entry — and the audit
  entry changes the very count the figures report. The figures were correct at the moment they were
  written and stale by the time the commit closed, by construction, on every commit that carries an
  audit entry.

  The gate catches it on the NEXT run, which is usually somebody else's push, so the person who
  fixes it is not the person who caused it. **The rule is that derived views regenerate LAST, after
  the append-only logs are written** — a derived view produced before its source finishes moving was
  never current, and no amount of care at the regeneration step can fix an order.
- **Status:** `controlled`

### DC-083 — Work started in a constructor reads a property the object initializer has not set yet

- **Shape:** a type takes a value as `{ get; init; }`, and its one construction site supplies that
  value in an **object initializer**. The constructor also *starts something* — a session, a timer, a
  load — which reads the property. The initializer runs **after** the constructor body, so the
  started work always sees the default. The object is correct a microsecond later, and forever after;
  only the thing that mattered ran too early.
- **Signature:** a constructor whose body contains `_ = SomethingAsync(…)`, `Task.Run`, or an event
  subscription that fires immediately, in a type with `init` properties that the started work reads.
  `async` is not protection: the method body runs synchronously up to its first `await`, and the read
  is usually in the first three lines. The tell in review is that the property's own documentation
  says *"chosen when it is created"* while the value arrives after creation.
- **Why it survives:** every part is individually correct and reads correctly. The factory sets the
  property; the property is used; the constructor starts the work; the object ends up fully
  populated. Nothing is null by the time any test that inspects the object looks at it — which is
  why unit tests over the constructed instance pass. Only a record of what the *started work saw*
  distinguishes it, and that record did not exist for two days.
- **Instance:** 2026-09-02 — `TerminalSurface.Executable` was `{ get; init; }`, set by an object
  initializer at `SurfaceContentFactory`'s single call. The constructor's last statement is
  `_ = StartAsync(...)`, and `StartAsync`'s first statement is `var launch = Executable ?? CommandLine`.
  **Measured: 243 `terminal.start` records across two days, `executable` null in all 243** —
  including the one whose surface id was `agent:claude#aa8dcb` and whose tab read "Claude Code".

  **It defeated two rounds of reading because every step downstream is correct for a null input:**
  null executable → the launch falls back to the shell → no readiness profile matches `powershell` →
  `ShellIntegrationMode.PowerShell` rather than `PowerShellHostedAgent` → **`AgentCommandLine` is
  never called**. A real defect in `AgentCommandLine` (DC-081) was found, fixed, proven correct in
  isolation, and could not change anything the user saw — because the branch that reaches it was
  never taken. **A correct fix to unreachable code is indistinguishable from no fix**, and only the
  recorded launch decision told the two apart.
- **Control:** the value is a **constructor parameter**. Not "set it earlier" — an initializer cannot
  be set earlier — but a shape in which the omission does not compile into a silent default. Three
  tests assert the recorded launch decision: the executable resolves from the surface id, the agent
  pane selects hosted-agent mode, and a plain terminal is still a plain shell (a DC-016 guard that is
  not hypothetical — hardcoding hosted mode would satisfy the first two while turning the default
  layout's terminal, 231 of the 243 measured records, into an agent launch). **Observed failing on
  the shipped shape.**
- **The generalisation:** *a constructor that starts work must receive everything that work needs, as
  arguments.* Anything set after construction is not available to it, however it is spelled.
- **Status:** `controlled`

### DC-084 — A capability that does not depend on X is wired only on the path that has X

- **Shape:** a hook, a callback or a registration is assigned inside the method that sets up some
  larger thing — a workspace, a connection, a document — because that is where the code that fills
  it lives. The hook itself needs none of that: it reads current state on every call and degrades
  honestly when the state is absent. But the setup method is **conditional**, so every run that
  skips it leaves the hook null and every consumer silently gets nothing.
- **Signature:** an assignment to a long-lived hook sitting inside a method guarded by `if (x is
  not null)` at its call sites, where the assigned thing does not mention `x`. The tell in the code
  is a hook whose body already handles the absent case — that handling is evidence it was never
  meant to be gated. The tell in behaviour is that **the failure is per-RUN, not per-instance**: the
  same surface works in one launch and not the next, with no difference between them.
- **Why it survives:** the setup method is the *usual* path, so it runs in development, in every
  test that builds a full app, and in every session anyone demonstrates. Nothing errors when it is
  skipped — the consumer's null-conditional call returns null and the feature is simply absent, so
  the run looks entirely normal. And the neighbouring code often contains the **inverse** of the
  same mistake with a comment explaining it, which reads as the pattern being understood.
- **Instance:** 2026-09-02 — `TerminalSurface.EnvironmentFor` was assigned only in
  `WorkbenchShell.AttachWorkspace`, and both `MainWindow` call sites skip it when the view model
  returns no queries (daemon unreachable, no default workspace, a failed open). Terminals still
  open in those runs and every one launched with an **empty environment**: no `AIDE_SESSION`, and
  critically no `AIDE_CONTRACT_LOG`, so the model `update` event and the `episode-open` /
  `episode-close` kinds had nowhere to be written. Registration still happened, so the session
  appeared in the watcher and nothing looked broken — the agent was observed and could not
  participate. Twelve lines above the bug, the watcher wiring carries a comment explaining why it is
  done in *both* the constructor and `AttachWorkspace`; the environment contract had the inverse
  error directly beneath it.
- **How it was found:** not by reading. The launch record showed `environmentCount: 0` on an agent
  pane, which looked pane-specific. **Ordering the records across runs** showed `env=5` on either
  side and `env=0` for every pane in the runs between — including a plain terminal eleven seconds
  before the agent pane. A pane-level cause cannot produce that. The correction came from a second
  session sorting the log rather than reading the one record that looked interesting.
- **Control:** the assignment moved to the constructor, where nothing gates it, and
  `EnvironmentContractSurvivesNoWorkspaceTests` builds a shell with `queries: null` and asserts the
  hook is present and returns the session variables. **Observed failing on the shipped shape** —
  both tests fail with the constructor line removed. A second test asserts the variables that would
  have to be invented (`AIDE_WORKSPACE`, `AIDE_WORKTREE`, `AIDE_BRANCH`) are **absent** rather than
  filled with `ResolveGitFacts`'s fallback, guarding the fix against the wrong version of itself:
  always-present is only correct while what it returns stays truthful.
- **The sweep (CI2), prompted by a reviewer asking whether anything else in that pair is
  single-sited.** Measured rather than reasoned: the whole codebase holds **three** static settable
  hooks with no default, and the discriminator turned out not to be how many places assign them.

  | Hook | Default | Verdict |
  |---|---|---|
  | `TerminalSurface.EnvironmentFor` | `null` | **The defect.** Consumer null-conditionals it, so absent reads as "no environment wanted". |
  | `TerminalSurface.WorkingDirectory` | `null` | Correct. It genuinely depends on a workspace, and the consumer falls back to the current directory. |
  | `WorkbenchDiagnostics.Sink` | `null` | Correct. Null *is* the production value — it means "write to the log file"; only tests set it. |

  So the rule is not "assign it in both places", which is what the neighbouring comment models and
  what a gate would have enforced. It is: **a hook is safe when its own default is a working value.**
  `Profiles` defaults to `AgentReadinessProfiles.BuiltIn` and `CommandLine` to `powershell.exe`, and
  neither can be broken by a skipped setup no matter where it is assigned. The one hook that was
  wrong was wrong *structurally* — null default, plus a consumer that treats null as "skip".

  **No gate.** Three sites, one of which was the defect and is now fixed at the structure rather than
  the assignment: a scanner enforcing "assign in both places" would pass the two correct hooks for the
  wrong reason and could not have caught this one any earlier than the constructor line does.

- **The generalisation worth keeping:** *ask of every assignment whether the thing being assigned
  depends on the thing being set up.* Where it does not, it belongs at construction — the setup
  method is a convenient place, not a correct one, and convenience is how a capability ends up
  conditional on something it has nothing to do with.
- **Recurrence 2 (INV-0009, 2026-09-11) — the composer's run binding.** `MainWindow.BindComposer`
  is *"the only caller of `ComposerSurface.Configure` in the product"*, and it takes a
  `NewSessionResult` — so it is reachable only from `File → New Session`'s `opened` callback. The
  reopen path (`ReopenSessionAsync` → `Shell.OpenSessionDocument(config)`) and the layout restore
  never bind: a reopened session's composer has no send context, pushes no `host.init`, and mounts
  no fields. The binding depends on the **session config and the provider file**, both of which the
  reopen path holds; it does not depend on the sheet's result. Same shape, same tell — the method
  that binds sits where the code that *creates* lives. **Observed red:**
  `ASessionDocumentIsShownWhereTheOperatorIsTests.AReopenedSessionIsShownAndItsComposerIsBound`
  (probe exit 32, `configured=0 init-pushed=0 host fields=0`). The first control
  (`EnvironmentContractSurvivesNoWorkspaceTests`) was written to the terminal hook and could not
  see this; the class control is the oracle above, which every path that opens a session document
  must pass.
- **Status:** `controlled` — the terminal-environment instance is controlled; the composer
  binding moved out of the create path into `SessionComposerBinder` (INV-0009 Phase 2, `d4d39cff`),
  reached from New Session, Reopen and the workspace-open restore, with the one-construction-site
  guard (`TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate`) now asserting the counts
  on the binder and **none** in the window. Oracle `AReopenedSessionIsShownAndItsComposerIsBound`
  (`configured=0` → `configured=1 init-pushed=1`).

### DC-085 — A prefix-stripping pattern eats the first character when the prefix is absent

- **Shape:** a generator strips an optional prefix from a value with a pattern that makes the
  **separator** optional but the **prefix character** mandatory. When the prefix is present the
  result is correct; when it is absent the first character of the payload is consumed as though it
  were the prefix. The output is still well-formed, still plausible, and wrong by exactly one
  character.
- **Signature:** an optionality marker attached to part of a prefix rather than to the whole of it —
  `[A-Za-z]:?`, `\w?-`, `(v)?\.` — anywhere a prefix is genuinely optional. The tell is that the
  pattern was written from examples that all had the prefix. The second tell is in the output: a
  name one character shorter than a real one, which looks like a different valid name.
- **Why it survives:** nothing downstream can tell. A decapitated identifier is still a legal
  identifier, so it renders as an ordinary code span, and the reader has no reason to doubt it unless
  they go looking for what it names. **The worst instances are invisible even to the author:**
  `Profiles` → `rofiles` looks broken, but `IAdvisoryEvaluator` → `AdvisoryEvaluator` looks
  completely legitimate — the eaten character was the interface `I`, and the result is a name a
  reader would accept without hesitation.
- **Instance:** 2026-09-02 — `tools/api-reference.py:40` rendered `<see cref="..."/>` with
  `cref\s*=\s*"[A-Za-z]:?([^"]+)"`. Same-type references are written without a prefix
  (`cref="Profiles"`), which is most of them, so the first character was eaten across **13 committed
  files**. Found by eye in a diff, while reviewing something else — `` `rofiles` `` and
  `` `ommandLine` `` in a doc comment. The interface cases were only found afterwards, by the gate.
- **Control:** `tools/verify-api-crefs.py`, in CI with its self-test. It reads every capitalised code
  span in `docs/api/*.md` and, for any that is not a declared name in `src/`, asks whether restoring
  a single capital letter makes it one — which is precisely this defect's signature and nothing
  else's. **Observed failing on the original generator**, where it reported the three interface cases
  that eye-reading had missed.

  It deliberately ignores lowercase-initial spans (parameter names, JSON keys, JavaScript), and that
  blind spot is stated in the file rather than left to be discovered: a decapitated `Profiles` IS
  lowercase. The decapitation test is what catches those, not the capitalisation rule.
- **The generalisation:** *make the whole optional thing optional.* A prefix is `(?:X:)?`, never
  `X:?` — the second says "the letter is required and the colon is a nicety", which is never what
  anyone means.

  **A SECOND DEFECT IN THE SAME ARTIFACT, found by asking what else was unchecked.** `docs/api` is
  derived from the `///` comments in `src/`, and `verify-derived-views` — the gate whose entire
  subject is committed derived views being stale — did not cover it. It was **already stale when
  this entry was written**: the committed pages said 76 public types where the source had 79, from a
  merge minutes earlier, and nothing reported it.

  The reason it was uncovered is worth more than the fix: the gate understood a view as **one file**,
  because both views it was built from were single files. `docs/api` is a directory of seventeen, so
  it was not a view it could hold — not excluded by a judgement, just unrepresentable. **A coverage
  list takes the shape of the examples it was built from**, and an artifact of a different shape is
  invisible rather than skipped. The gate now takes a `glob` and reports "n of 17", observed catching
  a planted hand-edit and naming the file.
- **Status:** `controlled`

### DC-086 — An identifier is used as a file name, and the filesystem reinterprets it

- **Signature:** an id that is unique, stable and perfectly valid *as an id* is concatenated into a
  path. Somewhere in it is a character the filesystem treats as syntax rather than as text, so the
  path the code composed is not the path the OS creates. On Windows the character is `:` and the
  reinterpretation is an **NTFS alternate data stream**: `dir\agent:claude#fb96e3.jsonl` becomes the
  file `agent` carrying the stream `claude#fb96e3.jsonl`. Elsewhere it is `/`, a trailing dot, a
  reserved name (`CON`, `NUL`), or a length limit.
- **Why it survives every test:** the write **succeeds**. The directory is created, the handle
  opens, the bytes land, nothing throws, and the data is not even lost. It is simply somewhere no
  reader looks: `Directory.EnumerateFiles(dir, "*.jsonl")` cannot enumerate a stream, and neither
  can a listing, a backup, a sync, or a copy to any non-NTFS target. And **fixture ids are plain
  words**, so the whole suite exercises the one shape that works.
- **The tell:** a zero-byte file whose name is a *prefix* of an identifier — the part before the
  first `:`. It looks like debris. It is the entire log.
- **Instance:** 2026-09-02 — `CoordContractWriter` used the external session id verbatim as a file
  name. Agent panes are `agent:<name>#<hex>`, so **no agent session was ever registered with the
  watcher**: no liveness, no harness/model `update`, no `episode-open`. Found on the owner's machine
  as one zero-byte file named `agent` holding **seven streams and 41 KB of coordination events**,
  beside `terminal-1.jsonl` as a real 442 KB file. Plain terminals had worked throughout. It went
  unnoticed through the two launch defects above it (DC-083, DC-084) because those kept any agent
  from launching at all; fixing them is what made this one reachable — and it would have made the
  whole `update`/`episode` contract inert on the first build where it could have worked.
- **Control:** `CoordContractWriter.FileNameFor` maps a session id to a safe name, replacing invalid
  characters and appending a digest of the *original* id so two ids that sanitise alike stay two
  files. `CoordLogSurvivesASessionIdWithAColonTests` asserts through
  `EnumerateFiles("*.jsonl")` — the step a stream defeats — rather than by reading the path back,
  which would have passed on the broken code. **Observed failing on the shipped shape**: 4 of 6.
- **What the suite caught that the fix had not:** the first version rewrote **every** name, which
  orphaned every log already on disk and changed the case that was never broken. Two existing tests
  failed on it. Now only a name the filesystem would mangle is rewritten, with an assertion pinning
  that an already-valid id is returned untouched.
- **The generalisation worth keeping:** *an identifier is not a file name, and a path that composes
  cleanly in code is not a path the OS agrees with.* Where an id reaches a filesystem, a network
  route, a URL or a shell, it crosses into a grammar it was never designed for — and the failure
  mode is not an error but a **successful write to somewhere else**.
- **Status:** `controlled`

### DC-087 — An empty state explains itself with a cause it never checked

- **Shape:** a surface has nothing to render and says so, and then adds *why*. The explanation is
  written once, when one reason is the only reason there is. Later the surface acquires other ways of
  being empty — the ordinary one, usually — and the single explanation is shown for all of them. It
  is confidently wrong exactly when a user is most likely to act on it, because a person reading
  "nothing here, and here is why" investigates the why.
- **Signature:** an empty-state string containing a **causal clause** — *needs X · waiting for Y ·
  not yet emitted · renders as soon as Z lands* — held in one field and rendered from more than one
  path. The tell is that the surface names a subsystem it has never queried. A second tell: the
  explanation describes a gap that has since closed, because prose about "not yet" has no expiry.
- **Why it survives:** it reads as helpfulness and it is true when written. Nothing in a test suite
  objects to a string. And the wrongness is invisible from inside the surface — the surface has no
  idea whether the claim holds, which is the defect itself.
- **Instance:** 2026-09-02 — `SequenceDiagramSurface` showed *"Sequence diagrams need ordered call
  data from the extractor — this surface renders it as soon as that lands"* on a freshly opened tab,
  before any node was selected. **Measured in the reported workspace: 4,967 `calls_at` assertions.**
  The extractor had done its job and the feed was wired
  (`ShowNodeInSequenceDiagramsAsync` → `InteractionAsync`). The owner read it, concluded the call
  data had not landed, and reported an extractor gap — a correct inference from a confident and
  wrong statement, costing a round trip.

  **The class remark above it carried the same false claim**, describing the surface as a "scaffold"
  awaiting data "the graph does not yet emit". The prose outlived its subject in two places, and the
  one users see is the one that did damage.
- **Control:** the empty state is chosen per case — a prompt when nothing is selected, a statement
  about the node when that node returned nothing. Four tests, including one asserting the word
  *extractor* never appears before a selection, and a guard that a populated model still renders (an
  unconditional empty state would satisfy the rest). **Observed failing on the shipped message.**
- **The generalisation:** *an empty state may say what it is waiting for; it may name a cause only
  when it has observed one.* "Nothing to show" is complete. "Nothing to show because X" is a claim,
  and a surface that has not looked at X is not entitled to make it.
- **Status:** `controlled`

### DC-088 — A launcher omits an identity, and a downstream guard degrades to advisory rather than refusing

- **Shape:** process A launches process B and hands it an environment. A capability B depends on is
  keyed on one variable A does not set. B's *reads* fail visibly enough — "no identity to check" —
  but somewhere downstream a **guard** that also reads it is written to degrade gracefully rather
  than block, because it runs on paths a hard refusal would make unusable. So the missing variable
  does not surface as a failure at all: it surfaces as a control that prints a notice and returns
  success.
- **Signature:** an `if (not identity): print(advisory); return 0` in a hook, gate or pre-commit
  path, plus a launcher that composes the environment somewhere else entirely and has no test
  asserting the two agree. The degradation is usually **correct in isolation** — the comment above
  it explains a real trade — which is why reading either side alone finds nothing.
- **Why it survives:** the trade is deliberate and documented on the guard's side, and the omission
  is invisible on the launcher's side, so **each half looks right to whoever owns it**. The
  advisory is printed into output nobody reads, and the guarantee is gone without anything failing.
  The symptom people eventually notice — a session invisible in the registry — is the mild one;
  the disabled boundary is what costs work.
- **Instance:** 2026-09-02 — AI-DE handed agent terminals nine `AIDE_*` variables and **not**
  `AGENT_SESSION`, which the AI-Forward Pack's `coord-core.py` reads. `check` reported no identity;
  the **pre-commit boundary printed an advisory and returned 0**, so the control that stops one
  session committing over another's files was off for every agent launched from the IDE. Observed
  the same day in a consuming repository: a squash merge swept another session's entire pack
  refresh into a commit describing only the merger's work, and nothing objected. Three sessions
  then spent an afternoon tracing an "unreachable" fourth session that had never been given an
  identity to register with.
- **Instance (fleet, ai-forward drm-0009 COORD-C):** `2026-09-04` -- the same class, generalised for the fleet and measured here: 3 `COORD-NOT-CHECKED-IDENTITY` events under agent `anon`, against 50 allowed and 2 refused across the whole decision record -- the not-checked path was more common than the refusal it replaces. Promoted to the ai-forward fleet store as a general class on 2026-09-04.
- **The trap inside the fix:** the obvious value is the launcher's own session id. AI-DE's is
  `agent:claude#a90b5c`, and `coord-core.py` writes `logdir / "{}.jsonl".format(session)` then reads
  it back with `glob("*.jsonl")` — so on Windows that colon would have made every write an NTFS
  alternate data stream the glob cannot see. That is **DC-086 reintroduced inside the pack**, where
  this repository does not own the fix. Caught by reading the consumer before choosing the value.
- **Control:** `WorkbenchShell.PackSessionId` derives a path-safe id from the surface id, and
  `AgentCarriesThePacksIdentityTests` asserts it survives `Path.GetInvalidFileNameChars()`, that
  distinct panes stay distinct, and — the one that matters — that the variable **reaches the
  environment a terminal is launched with**, through the shell's own hook rather than by calling the
  helper twice. **Observed failing on the shipped shape.** A correct id that never reaches the child
  is the whole defect, and a helper-only test would have passed while the environment stayed empty.
- **The generalisation worth keeping:** *a guard that degrades to advisory moves its guarantee to
  whoever supplies its input.* When you find one, the question is not whether the degradation is
  reasonable — it usually is — but whether anything tests that the input is actually supplied.
- **Status:** `controlled`

### DC-089 — A capability is built, tested and rendered, and nothing can reach it

- **Shape:** a service exists with a full implementation, its own unit tests, and a UI surface that
  renders its results. Every part is correct. What does not exist is any **caller** — no ingest
  path, no tool, no command, no affordance. The surface shows its honest empty state forever,
  because the store behind it can only ever be empty.
- **Signature:** a public service type whose only references are its own file and its own tests. The
  surface says something true and useless ("No board posts yet"); the pane's read model has a query
  interface and no write; and somewhere a comment refers to the missing half as *future* work in the
  present tense. The tell in review is a feature that passes every test it has and cannot be
  demonstrated end to end.
- **Why it survives:** every slice was individually complete and each one shipped green. Unit tests
  pass because they construct the service directly, which is exactly the call site production
  lacks. The specification describes the capability, the design describes the capability, the code
  implements the capability — so **reading any single artifact confirms it exists**. Only using it
  reveals that nothing invokes it.
- **Instance:** 2026-09-02 — `MessageBoardService` had zero call sites outside its own file for
  three slices. The coordination contract had no board kind (its parser comment said "a *future*
  board post"), the MCP gateway exposed one read tool, and the pane was read-only. Found when the
  owner asked an agent to "send a message to the loomkeeper board": the agent searched the
  repository for a mechanism, found none, and the pane went on saying "No board posts yet". **The
  agent behaved correctly and the product was reported as broken**, which is the honest reading —
  the collaboration surface every other feature was justified by had never been reachable.
- **What made it visible:** a user driving the product end to end, and a recording of it. No gate
  caught it, and it is not obvious one could: every component was present and passing.
- **Control:** `board-post` on the coordination contract, applied through the capability-gated
  ingest — `ContractBoardPostTests` covers the path and its four refusals, and
  `verify-surface-ownership` already fails a surface with no backing implementation. The stronger
  control is the standing question rather than a script: **for each surface, name the caller that
  puts data behind it.** A surface whose only writer is a test is a mock in production.
- **The generalisation worth keeping:** *a component with no caller is not an unfinished feature, it
  is an absent one — and it looks finished from every angle except use.* Completeness is a property
  of the path, never of the parts.
- **A THIRD INSTANCE, and it shows how the shape hides (2026-09-02).** `StandingComposer` had zero
  production callers, and the reason it looked finished is that its signature was satisfied:
  `Compose(subject, board, int trend)` took the trend as a parameter, and **nothing in `src/` produced
  one**. The unit tests passed `trend: 3` and `trend: -1` — numbers no code path could ever generate —
  so the composer appeared exercised while the value it depended on had no origin at all. **A required
  parameter with no production producer is the same defect wearing a type signature**: the compiler is
  satisfied, the tests are satisfied, and the only thing missing is a caller. Closed in C1 by deriving
  the trend from an episode history the method takes instead, so the value cannot be forgotten because
  the method cannot be called without the evidence for it.
- **Status:** `controlled`

### DC-090 — A versioned schema stores a version and never compares it

- **Shape:** a store has a `schema_version` table, a `SchemaVersion` constant, and DDL that creates
  everything. It looks migrated. The creation path guards on **whether the version table exists**
  rather than on **what version it holds**, so the DDL runs exactly once per database, at creation,
  and never again. Adding a table gives it to new databases and to no existing one.
- **Signature:** an `EnsureSchema`-shaped method whose early return is `if (versionTableExists)
  return;`, with a version constant that is written and never read back for comparison. The presence
  of the version table is doing the work a version *check* appears to be doing — which is why
  reading the schema, the constant, or the table list all confirm the design is sound.
- **Why it survives:** every test creates a fresh database, so the migration path has no coverage by
  construction — the one shape that exercises it is the one a test never has. It cannot be found by
  running the suite, only by reading the method or by opening a database that predates the change.
  The failure then lands as `no such table` in **whichever workspace has been open longest**, which
  for an observability feature is the one with the most history worth observing.
- **Instance:** 2026-09-02 — `SqliteWatcherObservationStore.EnsureSchema` returned early whenever
  `watcher_schema_version` existed. `SchemaVersion` was `1`, written at creation, compared to
  nothing. Found in Daydream D2's P0 while reading the store to follow its grain, **before** adding
  `daydream_observation_fact` — so the table never shipped without a way to reach an existing
  database. Nothing was broken yet; the next additive change would have been.
- **Control:** the version is now read back and additive migrations apply for anything newer.
  `DaydreamPersistenceTests` builds a v1 database the way one actually occurs — the table dropped and
  the version rewound — and asserts the table arrives on open, that reopening applies nothing, and
  that **a fresh database and a migrated one end up with identical schemas**. That last one exists
  because the DDL now lives in two places, which is the derived-view problem in a schema; comparing
  them is cheaper than trusting whoever adds the next table to remember both. **Observed failing**:
  disabling the migration filter fails exactly those two and leaves the other four green.
- **The generalisation worth keeping:** *a version you write and never read is a comment.* Wherever
  schema, protocol or format carries a version, find the comparison — if the only use is an INSERT,
  the versioning is decorative and the first additive change is the one that discovers it.
- **Status:** `controlled`

### DC-091 — An acceptance criterion does not reach its own deliverable

- **Shape:** a slice states what it delivers, and separately states how it will be judged. The two
  are written at different moments and are not compared. The criterion is satisfiable by work that
  leaves the deliverable undone — so the slice can go green, the tests can be honest, the reviewer can
  be satisfied, and the user story stays unmet. Nothing is wrong with any artifact; the defect is in
  the gap between two sentences that nobody read together.
- **Signature:** a deliverable written in the **user's voice** and a criterion written in the
  **system's** — *"an agent sees its standing"* judged by *"a standing is produced"*. The verbs give
  it away: **produced, computed, available, exposed** on the criterion against **sees, receives,
  can, knows** on the deliverable. The second tell is that satisfying the criterion requires touching
  only components that already exist, while the deliverable would require a channel that does not.
- **Why it survives:** every control points at the criterion. A red-first test is written against it,
  a reviewer checks the work against it, and a gate — if one existed — would encode it. The
  deliverable is prose at the top of a plan, and prose is not executable. The failure only appears
  when a user tries the thing, which is exactly when it is most expensive.
- **Instance:** 2026-09-02, C1 (US-16). **Delivers:** *"an agent sees its rank, trend and one
  evidence-backed reason per dimension."* **P1:** *"a standing is produced for a scored episode."*
  Producing it was satisfiable in an hour by calling `StandingComposer` from
  `WatcherLeaderboardPaneViewModel` — after which the **operator** would see a standing and the
  **agent** would receive nothing, which is the opposite of the story. US-16 is written from the
  agent's seat and its own acceptance clause reads *"Then it **receives** its current standing"*.

  Found in the slice's P0 by reading the **spec line**, not the plan. Reading the plan alone could not
  have found it: both sentences are in the plan and both are individually reasonable.
- **Control:** no gate, and the reason is the same one that stopped a text gate for DC-089 — a
  criterion is prose, and matching a deliverable to it is a semantic comparison. What worked, and
  costs one step: **in P0, restate the deliverable in the user's voice and ask which component would
  have to change for that sentence to become true.** If the answer is only components that already
  exist, the criterion is probably measuring the wrong thing. Here the answer was "a channel the
  agent can call", and no such channel existed — five MCP tools, three of them writes, and no
  standing among them.
- **The generalisation:** *a criterion that can be satisfied without touching anything new is
  suspicious when the deliverable describes something that does not yet happen.*
- **Status:** `partially-controlled` — a question in P0, no automated check

### DC-092 — A field named for an invariant that nothing enforces

- **Shape:** a value object carries a field whose **name is a promise** — `CanonicalPath`,
  `NormalisedId`, `SanitisedInput`, `UtcTimestamp` — and the type accepts whatever it is handed. The
  name then does the work the code does not: every consumer reads it and reasonably assumes the
  invariant already holds, so nobody canonicalises, normalises, sanitises or converts. The value is
  wrong only when two spellings of one thing meet, which is later and somewhere else.
- **Signature:** a `record` or DTO field whose name contains a past participle or an adjective
  asserting a transformation, with a plain auto-property and no constructor logic. The second tell is
  a consumer comparing it with `StringComparer.Ordinal` — ordinal comparison of something called
  "canonical" is the code saying it trusts the name.
- **Why it survives:** it reads correctly at every site. Producers pass what they have; consumers
  compare what they are given; nothing looks wrong in isolation. The defect only appears when two
  producers spell the same thing differently, and until then the type is indistinguishable from a
  correct one. Fixing it in the consumer that noticed leaves the others still trusting the name.
- **Instance:** 2026-09-02, C2 (US-3). `RepositoryIdentity.CanonicalPath` was a plain string that
  nothing canonicalised, and `FleetAggregator` grouped by it with `StringComparer.Ordinal`. One
  repository split into several: git reports forward slashes where .NET reports backslashes, Windows
  paths are case-insensitive, and a trailing separator is indistinguishable from its absence. That is
  US-3's second clause — *"a worktree path aliasing an already watched repository appears as a
  Worktree under that Repository, not as a duplicate Repository"* — failing.

  **It had already been hit once, at one producer.** The slash-direction case was found in session
  identity that morning and normalised *there*, because that was where it hurt. The key itself stayed
  ordinal, so every other route into the store — another producer, an older row, a manual
  registration — still split. **Fixing the producer that hurt is not fixing the invariant.**
- **Control:** the type canonicalises on construction, so the name is true by the only mechanism that
  makes a name true. Five tests: slash direction, case, trailing separator, an aliased worktree
  landing under its repository, and a guard that genuinely different repositories stay apart —
  which is not hypothetical, since canonicalising too hard would merge repositories that share only
  a display name, the exact collapse the field exists to prevent. **Observed failing on the shipped
  type: four of the five red.**

  Case folding is platform-conditional. Windows paths are case-insensitive and the shipped product
  is Windows desktop; POSIX paths are not, and folding there would merge two distinct repositories.
- **What the fix surfaced, which is the general lesson:** three tests then failed because they
  asserted a *spelling* rather than an identity — comparing the stored key against a raw literal. A
  string-keyed lookup sitting beside an identity type invites exactly that, in tests and in callers.
  They now derive the key through the product's own canonicaliser: **a test that restates the
  normalised form has copied the rule rather than checked it.**
- **Status:** `controlled`

### DC-093 — An absence has a cause, and treating it as neglect produces the worse fix

- **Shape:** something is missing — a gate, a caller, a coverage entry, a link — and the obvious
  reading is that nobody got to it. Acted on that way, the fix is to add the missing thing. But the
  absence usually has a **reason**, and the reason is a constraint that is still present: whatever
  prevented it the first time will shape whatever is added now. A fix that does not find the reason
  either fails in the same way or works while leaving the constraint in place for the next person.
- **Signature:** an omission in an otherwise careful area, especially beside similar things that
  were done. The tell is that the missing item is not harder than its neighbours — if adding it
  were merely work, a careful author would have done it, so something made it *not work* rather than
  *not happen*. Ask what would have gone wrong if they had tried.
- **Why it matters more than tidiness:** treating it as neglect produces a fix that is correct in
  isolation and wrong in place. The three instances below each had an answer that would have
  "worked" and then misbehaved quietly.
- **Instances, all 2026-09-02, all found by asking why rather than adding the missing thing:**

  | The absence | The reason | What "just add it" would have produced |
  |---|---|---|
  | `docs/_meta.json` and `_site/index.html` had no gate | `documented_sha` names the commit generated FROM, so it differs on every run by construction — a naive comparison is red on every clean run | a gate that fails always, then gets switched off, taking the real check with it |
  | `verify-derived-views` did not cover `docs/api` | a view was **one file** in its model, and `docs/api` is a directory of seventeen — not excluded by judgement, unrepresentable | an entry that regenerates a set, restores one file, and silently leaves its siblings rewritten |
  | `MessageBoardService`, `StandingComposer`, `McpToolGateway` had no callers | a unit test **is** a caller — just not one that ships — so every seam was green and nothing was reachable | a caller added where it was noticed, leaving the other two unreachable and the pattern intact |

- **Control:** none mechanised, and it is the same reason as DC-089's: "why is this missing" is a
  question, not a comparison. What worked, four times, was **asking what would have gone wrong if
  someone had tried** — and in each case the answer was a constraint that then shaped the correct
  fix: a volatile-field pattern, a set-valued view, and a standing question about callers.
- **The generalisation:** *before adding what is missing, find out what made it missing.* An absence
  in careful work is evidence about a constraint, and the constraint is still there.
- **Status:** `uncontrolled` — a question at diagnosis time, no automated check

### DC-094 — A doc comment that UNDERSTATES the code, read as if it were the code

- **Shape:** you need to know what a type or a value actually holds, so you read its doc comment
  instead of its producer. The comment is not wrong — it describes a real property — but the code
  has since gone **further** than the comment claims. Reasoning from it produces a confident,
  well-argued conclusion about a defect that does not exist.
- **Signature:** a finding whose entire evidence chain terminates in prose you did not write and did
  not check against a caller. The tell is grammatical: *"the comment says it normalises aliases, so
  it cannot be doing X"* — an absence inferred from documentation. Also: citing a `file:line` that
  points at a `<remarks>` block rather than at an assignment.
- **Why this direction is the dangerous one.** Every instance of "never assert the shape of our own
  code from memory" in this repository has been the comment claiming **more** than the code does,
  which produces a false clearance — you believe a guarantee that is not there, and reality corrects
  you later. This is the mirror: the comment claims **less**, so you produce a false **defect**. It
  survives review better, because a defect report gets scrutinised for whether the consequence is
  bad rather than for whether the premise is true, and the premise is a quotation. It costs a peer a
  measurement to refute, and it can talk a team into changing correct code.
- **Instance 1 — a comment that understated (the class as named):** 2026-09-02 — arguing that keying a leaderboard segment on
  `RepositoryIdentity.CanonicalPath` would split one repository across its worktrees, because the
  type's `<remarks>` describes canonicalisation as fixing **aliased spellings** of a path. The
  producer had already gone further: `WorkbenchShell.ResolveGitFacts` takes `--git-common-dir`'s
  PARENT and passes that as `repo.path`, carrying the worktree separately, so the value already IS
  the repository. The concurrent session ran `git rev-parse` in both trees and refuted it in one
  message. The finding underneath survived for a different reason — nothing *enforces* it, and an
  externally-registering agent composes its own attributes (DC-092) — but that was luck, not method.
- **Instance 2 — the same rule broken in the OTHER role: I wrote the misleading comment.** Hours
  after registering this class, I committed a `<para>` in `DaydreamRecorder` asserting *"the one call
  site is `ScoringService.ScoreAndRecord`"*. There was no such call site in the tree the comment
  shipped in. The claim was true of a **concurrent session's unpushed branch** and I wrote it into a
  committed artifact as present tense. Found by grepping `src/` before building on my own comment —
  the check the class prescribes, applied for once to my own writing rather than to someone else's.
- **Why instance 2 is worse than instance 1, and belongs in the same class:** instance 1 cost me a
  wrong argument that a peer refuted in one message. Instance 2 would have cost **the next reader**,
  who has no peer and no reason to doubt a specific `file.method` citation — and it manufactures
  exactly the artifact instance 1 was fooled by. The class is not "do not trust comments"; it is
  **an artifact describes the tree it is in**, which binds the reader and the writer symmetrically.
  A peer's branch is not this tree. Neither is a plan I have not pushed, which is the same mistake I
  had already made once this week in the other medium.
- **The writing-side rule, since the reading-side one was not enough:** before committing a claim
  about code, ask *would this be true of a fresh clone of this branch?* If it depends on work that
  exists somewhere else, write the dependency (*"when X lands"*), never the destination.
- **Instance 3, which shows the writing-side rule above was itself only half of it:** the same
  paragraph, corrected to say **NO CALLER ON THIS BRANCH** — accurate when written — became false
  six hours later when the branch carrying the caller was pushed to `main`. So the paragraph was
  wrong in *both* directions within one evening: first asserting a call site that lived only on an
  unpushed branch, then denying one that had landed.
- **The half that was missing: a claim about what does NOT exist decays exactly as fast as a claim
  about what does.** It merely decays when *someone else* acts rather than when the file is edited,
  so its author is not present at the moment it expires and has no reason to look. And the "not yet"
  form is the more dangerous of the two, because it *reads as the careful option* — an author who
  writes "no caller yet" has visibly thought about it, which buys the sentence trust it keeps long
  after it stops being true.
- **What to do instead of a durable negative:** tie the claim to something that fails when it
  expires. The corrected paragraph now points at `WhatDaydreamSeesInAnAgentEpisodeTests`, whose red
  is the signal that the comment has expired — so the expiry is announced by the suite rather than
  waiting for a reader to notice. A negative claim with no expiry trigger is a claim with a
  scheduled falsehood in it.
- **Control:** none mechanised, and a gate cannot have one: no check can tell a comment that
  understates from one that is complete. What works is a rule about **which artifact answers which
  question**. A doc comment answers *what is this for*; only the producer answers *what does this
  hold*. So: **to learn what a value contains, read the code that assigns it, never the code that
  describes it** — and where the producer is not at hand, label the claim Inferred rather than
  citing the comment as evidence. A citation is not a promotion, and a citation of prose is not even
  a citation.
- **Cheapest disconfirmation, since there was one available and it was not taken:** the claim was
  about two directories on this machine, and `git rev-parse --git-common-dir` in each answers it in
  seconds. Before publishing a finding about a runtime value, ask whether the value can simply be
  **printed** — this is IO1 pointed at one's own reasoning, and the tell for skipping it is arguing
  from a type's documentation about a path that exists on disk right now.
- **Status:** `uncontrolled` — a sourcing rule at reading time, no automated check

### DC-095 — A comment names a control by a name nothing resolves

- **Shape:** a doc comment states that a property is enforced and names the test that enforces it.
  The property may even hold. But the named class does not exist under that name, so a reader who
  checks finds nothing, and a reader who does not check inherits a guarantee nobody can locate.
- **Signature:** a `<c>SomethingTests</c>` or `<see cref="..."/>` in prose naming a test, gate or
  script, where a repo-wide search for that identifier returns only the comment itself. The tell is
  a *confident* naming — "X asserts exactly that" — because a vague "this is tested somewhere" gets
  checked and a specific citation does not.
- **Why it survives:** compilers do not resolve names inside `<c>` tags, and a `<see cref>` to a test
  class in another assembly does not resolve either, so nothing fails. The claim reads as the
  strongest kind of evidence — a named, locatable control — while being the weakest, an unverified
  assertion about the repository's own contents. It is DC-094's sibling: there the comment
  understated the code, here it overstates the *test suite*.
- **Instance:** 2026-09-02 — `SqliteWatcherObservationStore.EnsureSchema` stated that
  "`SchemaMatchesAfterMigrationTests` asserts exactly that by comparing the two". No such class has
  ever existed. The control was real but lived in
  `DaydreamPersistenceTests.AFreshDatabaseAndAMigratedOneHaveTheSameSchema`; found only while
  checking whether the fresh-vs-migrated comparison would survive an `ALTER TABLE ADD COLUMN`, which
  is the one reason anyone went looking for it.
- **Instance 2, and the reason this is `controlled` rather than a rule:** the same evening, in
  **the same commit that registered this class**, `WatcherIdentity` was committed saying
  "`TheWorkspaceKeyIsTheRepositoryNotTheCheckout` is the control". The class is
  `TheWorkspaceKeyIsTheRepositoryTests`. A rule violated within the hour by the person who wrote it
  is not a rule, it is a memoir (CI6) — so the register entry that stopped at a reading rule was
  itself the evidence that a reading rule was insufficient.
- **Control:** `tools/verify-cited-controls.py`. A comment that **claims enforcement** — asserts,
  pins, proves, guards, enforces, *is the control* — and cites an identifier must cite one that
  appears in code somewhere under `src/` or `tests/`. Observed failing on both instances above,
  replayed from their original commits, and `--self-test` proves it fires.
- **Two things the gate got wrong first, kept here because they are the transferable part:**
  its first version keyed on the **identifier's shape** (anything ending `Tests`, `Gate`, `.py`) and
  so could not have caught instance 2, where the *missing* `Tests` suffix WAS the defect — a
  detector keyed on the shape of a correct name is blind to a name whose shape is what went wrong.
  Its second version parsed **declarations** to decide what resolves, and reported `SchemaSql` and
  `WorkbenchShell.ResolveGitFacts` as fabricated because the regex only modelled `void` and `Task`
  methods; a gate whose findings are mostly its own blind spots gets switched off, taking the real
  check with it. A plain token scan over non-comment lines answers the actual question — *does this
  name exist anywhere but in the comment claiming it* — with no modelling to be wrong about.
- **Residual:** the `IGNORE` list of framework types named descriptively near an enforcement verb.
  It is the one place a real finding could be silenced, so it is documented in the gate as such.
- **Status:** `controlled` — a gate, observed failing on both instances

### DC-096 — An invariant that holds only because every instance so far happened to be shaped alike

- **Shape:** a mechanism documents a property — idempotent, order-independent, safe to re-run — and
  the property is genuinely true. It is true because every case fed to it so far shares an
  incidental shape, not because the mechanism enforces it. The first case with a different shape
  breaks the documented property, and it breaks inside the mechanism everyone trusts.
- **Signature:** a claim of a general property whose proof is *"look at the existing entries"*. The
  tell is a comment reasoning from the data rather than from the code: "the DDL uses
  `IF NOT EXISTS`, so re-running is safe" — true of the three migrations present, and a statement
  about them rather than about the runner.
- **Why it survives:** it is not a latent bug. There is nothing to find, no failing case, and a test
  that re-runs the mechanism passes honestly. The defect is created by the *next* author, who reads
  a true claim, relies on it, and adds the first case that violates it — and the claim's own
  confidence is what stops them checking.
- **Instance:** 2026-09-02 — `EnsureSchema`'s remark said a database holding part of a later shape
  "heals instead of refusing to open", because the DDL uses `IF NOT EXISTS`. Adding v4's
  `ALTER TABLE scored_episode_cell ADD COLUMN workspace` would have falsified it: SQLite has no
  conditional `ADD COLUMN`, so a re-run throws *duplicate column name* and the database refuses to
  open — in the repair path the sentence exists to promise. Caught while writing the migration,
  precisely because the comment made a promise specific enough to test the new case against.
- **Control:** the runner now takes an explicit `AddsColumn` guard and skips the `ALTER` when
  `pragma_table_info` already lists the column, so idempotence is **enforced by the mechanism** for
  the shape that cannot express it in DDL. Generally: when a mechanism's documented property rests
  on a property of its *inputs*, either constrain the inputs by type or enforce the property in the
  runner — a property maintained by the diligence of everyone who ever adds an entry has no owner.
- **Status:** `controlled` — the column guard makes the claim true by construction for the case that
  would have broken it

### DC-097 — A lazily-cached pair of fields, later read from a second thread

- **Shape:** a value is cached with a companion field recording what it was computed for
  (`_value` + `_valueFor`). Correct on one thread. A later feature reads the same cache from another
  thread, and now a reader can observe the new value paired with the previous key — two writes, no
  ordering, and the pair is the invariant.
- **Signature:** two adjacent fields only ever meaningful together, assigned in sequence outside a
  lock, with at least one reader on a different thread from the writer. Aggravated when the
  lazy-resolve block is **copy-pasted** into more than one method: the duplication is what hides
  that a second caller arrived, and each copy looks locally fine.
- **Why it survives:** the resolution is usually pure and idempotent, so the worst observable outcome
  is a redundant recomputation — nothing crashes, nothing is wrong on screen, and no test can see
  it. It is a correctness claim that decays silently when a caller moves threads.
- **Instance:** 2026-09-02 — `WorkbenchShell._gitFacts` / `_gitFactsFor`, with the resolve block
  duplicated in `AgentEnvironmentFor` and `IdentityFor`, both on the UI thread. Wiring the scoring
  workspace added a third reader on the watcher loop. Collapsed to a single
  `(string Root, GitFacts Facts)?` — one reference assignment cannot be seen half-applied — and the
  duplicated block became one `CurrentGitFacts()`.
- **Control:** the shape rule — **a cache and its key are one value, not two fields** — plus the
  standing one that a lazy-resolve block appearing twice is a missing method. Both are mechanically
  greppable (two fields sharing a prefix where one ends `For` or `Key`) and neither is gated yet.
- **Status:** `partially-controlled` — fixed at the instance and stated as a shape; no gate

### DC-098 — Scoring an absence as a zero

- **Shape:** a derivation produces a numeric or ranked result from evidence. Where evidence is
  missing it returns the neutral element — 0, empty, false — and downstream code cannot distinguish
  "we looked and it was bad" from "there was nothing to look at". The output becomes a claim about
  the subject where only a claim about the evidence is warranted.
- **Signature:** a default that is also a legal value. `?? 0`, `?? false`, an empty list that ranks
  last, a rank computed from a cohort of one. The tell is a rendered value that is *worst* rather
  than *absent*, for a subject nobody observed.
- **Why it matters here:** a leaderboard is the acute case, because the neutral element is a
  position. An agent whose episode carried no evidence would appear at the bottom — a statement
  about the agent, indistinguishable from a real failure, and the exact result ADR-0019 advisory-evaluator-calibration's
  anti-Goodhart section exists to prevent.
- **Instance (prevented, not fixed):** 2026-09-02 — wiring contract-closed episodes to scoring. The
  available shortcut was a placeholder task class so a leaderboard row would appear. Instead:
  `ScoreSegment.Unclassified` is not comparable, the verdict is **Not Scored with its reason**, and
  `AgentStanding.NotComparableReason` carries the cause so "no rank" never arrives bare.
  `DeterministicSignalsDeriver` already held this line — acceptance stays null rather than "met",
  requirements stay 0 so the dimension renders Not-Recorded — and the wiring had to preserve it
  rather than invent it.
- **Control:** `WhatDaydreamSeesInAnAgentEpisodeTests` and
  `TheAgentCollaborationCircuitTests.ACompletedAgentEpisodeIsScored_AndTheVerdictIsHonestlyNotScored`
  both fail if an unevidenced episode ever acquires a rank, a rubric, or a tripped floor. The
  general rule: **an absence must be representable in the type**, so the neutral element is never
  reachable by accident — nullable rank, nullable trend, and a reason string that is non-null
  exactly when the value is null.
- **Status:** `controlled` — held by tests at both the score and the standing

### DC-099 — A detector keyed on the shape of a CORRECT input

- **Shape:** you build a check for a class of wrong things, and you recognise candidates by what a
  *right* one looks like — a naming convention, a suffix, a well-formed prefix, an expected schema.
  Every input whose wrongness is precisely that it does not look right is filtered out before the
  check runs. The detector is blind to exactly its own subject.
- **Signature:** a candidate filter written in terms of a convention (`endswith("Tests")`,
  `startswith("test_")`, "must match `DC-NNN`", "only files under `docs/`"), guarding a check for
  whether that thing is *valid*. The tell is that the filter and the check ask the same question at
  different strengths — if a candidate must already look correct to be examined, the examination has
  nothing left to find.
- **Why it survives, and why it is worse than a check that is simply absent:** it runs, it is green,
  and it reports a number. Worse, it can pass its own emptiness guard: the first version of
  `verify-cited-controls.py` reported *"no cited controls found at all"* — which its DC-016 guard
  correctly flagged as "this gate examined nothing" — and that flag was indistinguishable from the
  gate being newly written and finding nothing to examine yet. **The guard fired and the result
  still meant nothing**, because a guard against examining nothing cannot tell you *why* nothing was
  examined.
- **Instance:** 2026-09-02 — the DC-095 gate, first version, keyed candidates on identifiers ending
  in `Tests`, `Gate` or `.py`. It could not have caught the instance it was written for
  (`TheWorkspaceKeyIsTheRepositoryNotTheCheckout`), because the **missing `Tests` suffix was the
  defect**. Found by replaying the gate against the original commit rather than by running it on the
  fixed tree, where it passed.
- **Control:** key the detector on the **claim**, never on the candidate's conformity — what makes a
  comment checkable is that it says *asserts / pins / proves / guards / is the control*, not that
  the name beside it is spelled like a test class. Generally: **the trigger must be a property the
  defective input still has.** And the test of a new detector is not that it is green on the fixed
  tree — it is that it is **red on the original defect, replayed from the commit that contained
  it**. That replay is cheap, it is available for every defect that has a commit, and it is the only
  thing that distinguishes a control from a decoration.
- **The technique that found the most, and the one neither session could run alone:** mutate the
  code on ONE side of a boundary and run the tests on BOTH. Each session's own sweep only mutates
  its own code, so a dependency that crosses the boundary is invisible to both — making an
  unevidenced episode trip a floor reddened the scoring tests AND four Daydream recorder tests, and
  the coupling it revealed (`DaydreamObservationOutcome`'s whole distinction resting on the scorer
  refusing to fabricate a floor) had been described by both sides as adjacency. Worth writing up
  properly as a practice; captured here so it is not lost.
- **Status:** `partially-controlled` — stated as a construction rule, and the replay discipline is
  now used; no gate can check another gate's blind spot

### DC-100 — A citation that rots without failing

- **Shape:** a comment cites a location that is correct when written and becomes wrong when anything
  above it changes — a line number, an ordinal position, "the third case", a byte offset. Nothing
  detects the drift, because the file still exists, the line still exists, and only the *claim* is
  false. The reader who follows it lands somewhere real and plausible.
- **Signature:** `File.ext:NNN`, "verified at line NNN", "spec line NNN", "lines N–M". Aggravated
  when the cited file is one the project itself edits, which is where drift is not a risk but a
  schedule.
- **Why it is worse than a fabricated name:** a fabricated identifier is mechanically catchable — a
  token scan for it returns nothing, which is what DC-095's gate does. A rotted line number is
  catchable by **nothing**, even in principle: checking the file exists and the line is in range
  fires only on deletion and truncation, and whether the cited line *says what was claimed* is not
  mechanisable at all. So the fix cannot be a gate.
- **Instances:** 2026-09-02 — four line citations in the concurrent session's committed artifacts
  (`docs-graph.py:746`, `:738`, `dream.py:147`, `MessageBoard.cs:151`), all correct at the time and
  all fixed at `ef5e81a`. And one **already rotted, found by sweeping for it**:
  `AiDe.App.CanvasProbe/Program.cs` cited `CanvasSurface.cs:267` for the string
  `"(evaluate failed: …)"`; that code is at line 366, and 267 is a bare closing brace. The claim was
  true, the citation was false, and nothing failed for however long it had been drifting.
- **Control:** **cite the symbol; a line number is a convenience that must never carry the claim.** A
  symbol does not move when the file is edited, it tells the reader what it means without opening
  the file, and — the part that makes this more than a style rule — **it is a token, so
  `verify-cited-controls.py` already covers it**. The ungateable citation form is converted into the
  gated one rather than given a gate of its own that could not work. Swept: zero `File.cs:NNN`
  citations remain in `src/` or `tests/`.
- **Residual, stated rather than quietly excluded:** *spec line NNN* citations (spec lines 210, 211,
  233, 236, 237 and US-6's 201–234) are the same class pointed at a document, and rot identically
  when the spec is edited. They are a pre-existing convention across many files, and converting them
  needs the spec's own anchors — a separate piece of work, not a sweep. Registered here so it is a
  known debt rather than an oversight.
- **Status:** `partially-controlled` — code citations converted and swept to zero; spec-line
  citations outstanding

### DC-101 — A tool that edits the tree and is only safe when it finishes

- **Shape:** a tool deliberately modifies working files and restores them afterwards — a mutation
  harness, a bisect script, a formatter check, anything that answers "what happens if this were
  different". Restoration lives in a `finally`, an `atexit`, or a trailing line. All of them assume
  the process **reaches** them. A timeout, a Ctrl-C, an OOM kill or a harness cap ends the process
  where it stands, and the modification is left live in the tree.
- **Why the leftover is worse than the interruption:** the tree still compiles and the tests still
  run. The next command measures mutated code and reports its results as real, and the person
  reading them has no signal that anything is wrong — the run looks like every other run. A killed
  job that leaves *nothing* behind is an inconvenience; one that leaves a silent edit behind poisons
  every subsequent verdict until someone happens to check `git status`.
- **Signature:** a script that writes to a tracked path and holds the original in memory. The tell is
  a `finally` that restores from a variable rather than from source control, and the absence of any
  refusal to start on a dirty tree — because without that refusal, the second run inherits the first
  run's damage and cannot tell.
- **Instance:** 2026-09-02 — a mutation harness replaying tonight's new tests against the defects
  they claim to catch. The foreground run hit a 10-minute cap, was SIGTERMed mid-`dotnet test`,
  `finally` never executed, and `WorkbenchShell` was left keying the workspace on the **checkout**
  instead of the repository — the exact defect the slice existed to prevent, sitting live in the
  tree. Found by checking `git status` rather than by anything failing.
- **Control:** two rules, both cheap:
  1. **Restore from source control, never from memory** — `git checkout -- <path>` in the `finally`.
     It is correct even if the in-memory copy is stale, and it is what a human would do to clean up.
  2. **Refuse to start on a dirty tree**, naming what is dirty. This is the half that survives a kill:
     restoration can always be skipped, but a run that inherits damage can always be *detected*.
  Neither makes the kill impossible; together they make its consequence recoverable and visible.
- **The generalisation worth keeping:** *a tool that edits the tree must be safe when it is KILLED,
  not merely when it finishes.* Correctness on the happy path is the easy half of a destructive tool
  and it is the half everyone writes.
- **Status:** `partially-controlled` — both rules implemented in the harness; no gate, because the
  harness is a one-off analysis tool rather than committed tooling

### DC-102 — A control that passes for a reason other than the one it names

- **Shape:** a test asserts the right outcome and passes, and the mechanism it was written to prove
  is never exercised, because some *other* check reached the same verdict first. The test is green,
  the assertion is correct, and the guarantee it claims to establish has no coverage at all. Removing
  the mechanism it names changes nothing.
- **Signature:** a test whose name or comment credits one guard, in code where several guards can
  produce the same answer for the same input. The tell is a defensive check standing *behind* a
  broader one — validation ordered from general to specific, where the specific check is the
  interesting one and the general check quietly does all the work.
- **Why it is worse than an uncovered line:** an uncovered line is visibly uncovered. This reads as
  covered, is cited as covered, and the strongest claim in the file — usually a security or
  correctness boundary, because that is what earns a dedicated test — is the part with nothing behind
  it. It also survives review perfectly: a reviewer checks that the assertion is right, and it is.
- **Distinct from DC-016.** DC-016 is a control that *cannot* fail. This one fails readily — just
  never for the reason claimed. The fix is different too: DC-016 wants the assertion strengthened,
  this wants a **new input** that only the named mechanism can reject.
- **Instance:** 2026-09-03 — `ProofPackVerifierTests.ASiblingRepositoryWithAMatchingPrefixIsRefused`,
  written to prove that containment appends a path separator before comparing, so that
  `C:\repos\app-other` is not treated as inside `C:\repos\app`. Mutating containment to a plain
  prefix comparison **reddened nothing**: the escaped path's remainder (`-other/docs/proof/...`)
  fails the `docs/proof/` check anyway, so that check was rejecting it, not containment. The case
  containment actually prevents had no test: a sibling whose name *extends* the root
  (`…/appdocs/proof/x.md`) leaves a plain-prefix remainder of exactly `docs/proof/x.md`, which passes
  the directory check with a file that really exists — another directory's file admitted as this
  repository's evidence.
- **Three of four survivors in that same replay were this shape or its cousin**, in security code
  written and described confidently minutes earlier. Reading found none of them.
- **Control:** **mutation on the boundary, not review of it.** Delete the mechanism a test names and
  require *that* test to redden; if a different test reddens, or none does, the naming is wrong and
  the input is not discriminating. No static check can do this — whether two guards overlap for a
  given input is a property of the input, so it has to be executed. `tools/mutation-replay.py` is
  where this lives, and a survivor must be triaged as uncovered / equivalent / **misnamed**, which is
  a third category the tool cannot infer.
- **Status:** `partially-controlled` — caught by mutation replay where a set exists; no gate can find
  it in code the set does not cover

### DC-103 — A test selector that silently stops covering new tests

- **Shape:** a verification set selects its subjects by name — a filter, a glob, a namespace, a
  suffix convention. New work that does not match the pattern is excluded, silently. The set keeps
  reporting a healthy number, and the code least likely to be proven — the newest — is the part
  outside it.
- **Signature:** any `FullyQualifiedName~Foo`, `*Tests.cs`, `--filter`, or directory glob standing
  between a verification tool and its subjects. The tell is that adding a test can never make the
  report worse: a coverage number that only goes up when you *also* remember to update the selector
  is measuring the selector.
- **Why it survives:** every signal points the right way. The suite is green, the mutation set has
  zero survivors, and the excluded tests pass too — they are simply never *challenged*. Nothing in
  the output distinguishes "18 mutations, 0 uncovered" over the whole subject from the same numbers
  over half of it.
- **Instance:** 2026-09-03 — the concurrent session's mutation set filtered on
  `FullyQualifiedName~Daydream`, which does not match `WhatTheRealCorpusCanProduceTests`. The newest
  and least-proven test sat outside the sweep whose entire job is proving that controls can fail.
  Found only by going looking for whether the new test *could* fail, rather than by any report.
- **Control, and the first version of this entry got it wrong.** It said *prefer no selector* — run
  the whole suite per mutation and let the mutation do the selecting. Measured rather than assumed,
  that does not survive at suite scale: **74s filtered for 18 mutations against ~71s each
  unfiltered, or about 21 minutes**. A gate that slow is not run, and an unrun gate is worse than a
  narrow one. The unfiltered form is right only for a harness whose subject is already one slice,
  which is why the shape appeared on the side with the whole Core suite as its subject and not on
  the side replaying a single file.
- **So the control is to make the selector's coverage the thing asserted**, not to remove it:
  a preflight **derives**, from the types each mutation touches, every test file naming one, and
  fails when the filter cannot select it. Derived from the mutation set rather than from a second
  list, because a scope check needing its own manual maintenance has the defect it is checking for.
  `tools/mutation-replay.py` does this, verified by narrowing the filter back to the value that
  actually shipped and watching it name the three real gaps.
- **A cross-boundary mutation must be exempt**, and this was found by running the check rather than
  reasoning about it: the first version flagged five scoring tests against a mutation whose entire
  purpose is to change a component this vertical merely depends on. Those tests are not a gap, they
  belong to another sweep. Five false positives on a push gate would have had it switched off within
  a day, taking the real finding with it — the same failure mode as a mutation detector reporting
  its own blind spots (DC-099), reached by a different route.
- **The general form, worth more than the instance:** *a verification whose scope is named rather
  than derived will drift out of date silently, and its report will not say so.* The same argument as
  deriving a fixture from the product rather than restating its list (DC-021), applied to which
  subjects get verified at all.
- **Status:** `partially-controlled` — the unfiltered default is stated and used; no gate asserts a
  selector's coverage

### DC-104 — A new control's first run is a test of the control, not of the code

- **Shape:** a verifier, gate or harness is written, run, and reports something about the codebase.
  The report is treated as a finding. But nothing has yet established that the control works, so the
  first run is evidence about **two** things at once and the far likelier defect is in the control —
  which is also the one nobody is looking for, because the control is the thing they just reasoned
  carefully about.
- **Signature:** a brand-new control returning a *surprising* result on its first execution. Seven
  coverage gaps at once, five false positives, a clean sweep of a directory nobody has swept. The
  tell is the shape of the number rather than its value: **an implausible result from an unproven
  instrument is a fact about the instrument.** Also: a control that has only ever been run on the
  fixed tree, where a defect in it is invisible by construction.
- **Instances, all 2026-09-03, all found by executing rather than reviewing:**

  | The control | Its first run said | What was actually wrong |
  |---|---|---|
  | a cited-controls detector keyed on the *shape of a correct name* | "no cited controls found at all" | it could never catch the instance it was written for — the malformed name **was** the defect |
  | the same detector, second version, parsing declarations | two identifiers fabricated | its regex modelled only `void` and `Task` returns; both findings were its own blind spot |
  | `mutation-replay`'s scope preflight | five scoring tests uncovered | a cross-boundary mutation must be exempt: those tests belong to another sweep |
  | `mutation-replay --self-test`, written to demonstrate *this class* | one guard not firing | the assertion was wrong and the code right — on the very first execution of the self-test |

- **Why it survives review:** every one of these was reviewed by its author immediately before
  running, and the reasoning was sound. A control is the artifact people most expect to be correct,
  because writing one is an act of care. And its output arrives dressed as a finding about something
  else, so attention goes there.
- **The cost is asymmetric and that is what makes it a class.** A false negative wastes a run. A
  false positive on a gate wired into every push gets the gate **switched off within a day**, taking
  the real finding with it — so the control ends up worse than never having been written.
- **Control:** a new gate ships with a `--self-test` that proves each of its refusals can fire, run
  in CI immediately before the gate itself. The convention already existed here (8 of 18 gates carry
  one) and `mutation-replay.py` was wired into `build.yml` without one — this class committed by the
  session registering it. Now added, and it failed on its first run, which is the class demonstrating
  itself one level further in.
- **The rule, which is cheaper than the control:** *run a new control against the ORIGINAL defect,
  replayed from the commit that contained it* (DC-099's rule pointed at the control rather than the
  code). If it cannot go red on the thing it was written for, nothing else it says is evidence.
- **Residual:** 10 of 18 gates in `build.yml` have no `--self-test`, and no gate asserts the
  convention. Enforcing it would fail the build on pre-existing tools that are not the author's to
  rewrite, so it is recorded here rather than mechanised, and the honest reading is that the
  convention is followed where someone remembered.
- **The residual is now a ratchet, not a note.** Ten of nineteen gates predate the convention, and
  enforcing it outright would fail the build on tools nobody is rewriting — which by this class's own
  asymmetry is how a gate gets switched off. So `tools/verify-gate-self-tests.py` **freezes the
  existing gaps by name and fails only when the debt moves**: a gate not on the list without a
  self-test is new debt and is refused; a gate on the list that has since gained one is a stale entry
  and must be removed. Both directions fail, which is what stops the frozen list becoming the thing
  that needs maintaining — a list that may only shrink is a register that cannot rot, where one that
  may be appended to is DC-103 with extra steps. A count would not do: it goes green again if someone
  adds a self-test to one gate and a new gate without one, holding the debt constant while the
  newest and least-proven tool is the part missing its proof.
- **Status:** `controlled` — self-test present and CI-invoked for `mutation-replay`; the
  convention itself is unenforced

### DC-105 — Calling non-compliance "discipline" without checking whether the rule was ever stated

- **Shape:** a rule is not being followed, and the diagnosis reached for is that people are not
  following it. The prior question — *was this ever stated to the audience that is not following
  it?* — goes unasked, because the rule is obviously written down **somewhere**, and the person
  diagnosing is the person who knows where.
- **Signature:** any sentence of the form "we need more discipline about X", or a fix that consists
  of restating X. The tell is that the speaker can quote the rule from memory: they have read the
  file it lives in, which is precisely the evidence that does not generalise to the audience.
- **Why it matters:** the two diagnoses have **opposite fixes**, and picking wrongly wastes the
  effort entirely. An unstated rule needs stating, where it is read. A stated-and-ignored rule needs
  a control, and restating it does nothing — it has already been tried by definition.
- **Instance, and it was BOTH at once, which is what makes the class worth having.** 2026-09-03 —
  Proof Pack capture was running at one observation per 111 episodes, and both concurrent sessions
  had been calling it a discipline problem. Measured against the pre-fix `CLAUDE.md`:

  | Rule | Stated in the always-loaded file? | Compliance |
  |---|---|---|
  | `goal` + `done_when` (AL5b) | **yes** — line 85, cited by ID | 33 of 292 skill entries |
  | `episode.artifacts` / `AIDE_CONTRACT_LOG` | **no** — absent entirely (grep count 0) | 14 of 111 episodes |

  The evidence half had never been stated for that harness at all: it lived only in
  `.github/instructions/session-collaboration.instructions.md`, which is **GitHub Copilot's**
  convention, so the harness doing most of the work could not have known. *A practice nobody was
  asked to follow is not a practice problem.* The goal half was the exact opposite and is the
  controlled experiment: stated in the most-read file in the repository, cited by identifier, and it
  produced **11% compliance**.
- **So the second half is CI6 with a number attached.** Not "prose is weak" as a slogan, but: prose
  in the most-read file in the repository achieved 33 out of 292. That is the strongest local
  evidence for *a lesson recorded as prose is a memoir* that this register contains, and it was only
  available because the two halves sat side by side in one file with opposite outcomes.
- **Control:** before treating non-compliance as discipline, **grep the audience's own entry point**
  for the rule — the file that harness actually loads, not the file you read. It is one command and
  it selects between two fixes that share no work. Then, whichever half it is, the remedy is a
  control rather than better wording: `verify-capture-instruction.py` for the stated-nowhere half,
  and `verify-audit-capture.py` for the stated-and-ignored half.
- **The generalisation:** *an instruction's reach is a property of where it lives, not of how well it
  is written* — and with multiple harnesses reading different entry points, "written down" is no
  longer a single fact about the repository. It is one fact per audience, and it is measurable.
- **Status:** `controlled` — both halves now have gates; the diagnosis rule itself is a one-command
  check with no gate, because there is nothing to check until someone makes the claim

### DC-106 — A conflict marker committed into a file that every gate reads for CONTENT and none reads for STRUCTURE

- **Shape:** a merge conflict is resolved by hand in a file that should have been regenerated, the
  resolution is left unfinished, and the literal `<<<<<<<` / `>>>>>>>` markers are committed. Nothing
  objects, because the markers are syntactically legal in the format — HTML renders them as stray
  text, Markdown as a paragraph — and the checks that do run on the file ask only whether the
  *values* in it are right.
- **Signature:** a gate reports success over a file that is visibly broken when opened. The tell is a
  content-level check (a number, a count, a cited id) passing on a document whose structure nobody
  verifies — and, downstream, a *nested* conflict on the next merge, which is the first moment the
  damage becomes impossible to ignore.
- **Why it survives:** the duplicated regions produced by a marker set usually hold the **same** value,
  so a checker that reads each occurrence and compares it against the source finds every copy correct.
  It counts figures; it has no opinion about the document around them. And a large generated file is
  the one nobody reads in review.
- **Instance:** 2026-09-05 — `site/collaboration.html` and `site/index.html`, the **published** pages,
  carried committed markers on `main` (`1c2c7c4`). Four `data-figure` cells were duplicated: the
  audit-entry count appeared four times in one table row, the ledger count twice. `verify-site-figures`
  reported *"15 figure(s) verified against source"* — it had verified four copies of a right answer.
  Found only because a subsequent merge produced NESTED markers. After the fix the same gate reports
  **14**: the count dropped because duplicates went away, not because figures were lost.
- **The structural cause is an ownership hole, and it is the more general half.** `site/*.html` are
  **partially derived**: `regenerate-derived.py` patches their figures in place rather than rebuilding
  them, so they are neither source (a human edits them) nor derived (`verify-derived-views` does not
  list them among its four). A file that is partly generated belongs to no gate's structural check by
  construction — *"is it regenerated correctly?"* is never asked of it, because nothing regenerates it
  whole.
- **Control:** `tools/verify-no-conflict-markers.py`, on every push. It flags `<<<<<<<`, `>>>>>>>` and
  `|||||||` at line start in every tracked text file, and deliberately does **not** flag a bare
  `=======`, which is a valid Markdown setext underline — a gate that fires on real prose is a gate
  someone switches off. **Observed failing on the un-fixed shape rather than on a fixture:** run
  against the blob at `1c2c7c4` it reports `collaboration.html:267` and `:271`. Its `--self-test`
  proves all three directions, including the false-positive one.
- **The generalisation:** *a content check cannot see structural damage, and will report success over
  a corrupt file with confident precision.* Where a file is validated only for the values inside it,
  something separate has to assert that the file is intact — and the cheapest such assertion is for
  the one defect that has no legitimate instance anywhere.
- **Residual risk:** the marker gate closes the loud case. The ownership hole is still open — nothing
  rebuilds `site/*.html` end to end, so a hand-edit that produces *plausible* wrong structure (no
  marker) remains invisible. That is DC-060 pointed at a partially-derived artifact, and it wants
  either full generation or a structural check, neither of which this entry delivers.
- **Status:** `partially-controlled` — the marker case is gated and proven; the partial-derivation
  hole that let it reach a published page is named, not closed

### DC-107 — A magnitude measured on one machine, encoded as a portable threshold

- **Shape:** somebody measures a number — a duration, a p95, a size — on the machine in front of
  them, and writes it into something that will run somewhere else: an assertion, a budget, or a
  scheduling decision. The measurement was real. The **portability** of it was never measured, and
  never stated as an assumption either.
- **Signature:** a bare wall-clock constant in an assertion, or a comment of the form *"MEASURED at
  N on this machine, which is why …"* justifying where a check runs. The tell is that the number and
  the decision are in the same sentence with no environment between them.
- **Why it survives:** it passes on the author's machine, which is the only place it is run before it
  lands — and *the author measured*, so it feels like the opposite of a guess. It is not: the
  measurement is Verified, the **generalisation of it** is unmarked (NG9). Worse, when it later fails
  it fails as the thing it was watching for, so the first reading is always "the regression happened".
- **The discriminator is the guard band.** A performance assertion is only meaningful when the gap
  between good and bad is wider than the spread between the machines it will run on. Where the guard
  band is narrower than the environmental noise, the test cannot ever reach its own subject.
- **Instances:** 2026-09-05, both found in one investigation (INV-0005).
  1. `TerminalViewTests.AFullScreenRedraw_StaysInsideTheFrameBudget` asserts `p95 < 16.67 ms`. Its
     own message names the signal it wants: GlyphRun-per-line 6.64 ms vs FormattedText-per-cell
     142.80 ms — a **20×** effect. The threshold sits at **2.5×** the good value; CI hardware is
     **~3×** slower than the machine that set it. Five consecutive CI runs failed at 17.2–22.8 ms —
     never near the hundreds that would mean the defect. It was **watching for a 20× regression
     through a 2.5× window on a 3× noise floor.**
  2. `.github/workflows/build.yml` justifies mutation replay as an every-push gate: *"MEASURED at 74s
     for 18 mutations on this machine, which is why it is an every-push gate and not a nightly."* On
     the runner it is **612s — 8.3×** — and 51% of the whole job.
- **What it cost:** the assertion is ordered before 26 other gates, so its failure skipped all of
  them. **38 of 40 consecutive `Build` runs red, the entire control suite dark for two days** — id
  allocators, derived views, register integrity, all of it. A brittle check placed early does not
  merely fail; it **silences everything behind it**.
- **Control:** `tools/verify-perf-assertions.py`, on every push. It refuses an **upper** bound on a
  measured duration compared against a constant, inside an assertion — the shape whose verdict
  depends on the host. It deliberately permits a **lower** bound (`duration >= 30` beneath a
  `Task.Delay(40)` the test injected asserts that the clock ran; slower hardware only makes it more
  true), a hang guard (`WaitForExit(timeout)` asserts completion, not speed), and a ratio between two
  values measured in the same process. A genuine absolute budget is declared in place with a
  `perf-budget:` comment carrying the hardware it holds on, so the justification sits next to the
  number. **Observed failing on the un-fixed code, not on a fixture:** run against the blob at
  `d6ce176` it reports `TerminalViewTests.cs:173` — the exact line that reddened 38 runs.
  The scheduling half is fixed too: mutation replay's `74s on this machine` was **re-measured on the
  runner at 401s** and re-ringed against that number.
- **Relationship to `PACK-C`** (fleet, *an assertion encodes a transient magnitude assumption*):
  ancestor, not duplicate. PACK-C's discriminator is **time** — a number that was true and decayed.
  DC-107's is **host** — a number that was never portable in the first place. The controls differ:
  PACK-C wants re-measurement on a schedule; DC-107 wants the comparison made relative, or the check
  moved to where the hardware is fixed. These two local instances are the first evidence PACK-C has
  in this repository.
- **The generalisation:** *a measurement is evidence about the machine it was taken on; treating it
  as evidence about every machine is an unmarked assumption* (NG9) — and the cheapest fix is almost
  never a bigger threshold, it is an assertion that carries its own baseline.
- **Third instance, 2026-09-09 (Conductor N7) — and it contradicts this entry's own carve-out.**
  The control deliberately permits a **lower** bound beneath an injected `Task.Delay`, on the stated
  reasoning that *"slower hardware only makes it more true"*. **Measured false.**
  `RefreshMetricsTests.AFailedRefreshIsTimedToo` asserts `DurationMilliseconds >= 20` under a
  `Task.Delay(30)` and recorded **17 ms** — twice in four full suite runs on `TIMMALLSTRIX`, passing
  in isolation and on re-run both times. Windows timer coalescing can complete a delay **early**
  relative to the clock the code under test reads, so a lower bound beneath an injected delay is an
  assertion that two clocks agree, not an assertion about the code. It reddened the phase's exit
  gate, which is the only reason it was looked at rather than retried.
  **Fixed by making the assertion carry its own subject:** all three timing assertions in that file
  now assert the duration is **recorded at all** (`> 0`), which is what the tests' own comments claim
  to be about — that a *failed* run is timed too, so a percentile does not describe only the easy
  cases. The magnitude was never the claim.
  **The carve-out is not removed, but it is now known to be one-directional:** a lower bound is safe
  against *slower* hardware and unsafe against a *coarse timer*, and only the first was reasoned
  about when it was written.
- **Status:** `partially-controlled` — the gate refuses the upper-bound shape on every push with its
  self-test intact, and the lower-bound carve-out it still permits has now been measured failing. No
  gate refuses that shape yet; the three instances in `RefreshMetricsTests` were repaired by hand

### DC-108 — A generator whose output depends on the host, checked by a gate that assumes it does not

- **Shape:** a reproducibility gate regenerates a committed artifact and compares it byte for byte.
  The generator uses a platform-dependent primitive somewhere — ordering, newline translation, path
  case, locale, filesystem enumeration order — so two hosts produce different bytes from **identical
  inputs**. The gate is correct and the artifact is correct; only the pair is impossible.
- **Signature:** a derived-view check that passes on every developer machine and fails on CI, or the
  reverse, with no content change between them. The tell is a diff whose two sides are both right.
- **Why it survives:** it cannot be seen from one host. Everyone who runs it locally sees green, and
  the gate was almost certainly authored on the same platform it is verified on, so the divergence has
  no opportunity to appear until someone changes runners — which is rare and looks like the change
  that broke it.
- **Instance:** 2026-09-05 — moving the control suite to a Linux runner (INV-0005 phase 2) reddened
  `verify-derived-views` on `docs/_site/index.html` immediately. `build-doc-viewer.py` did
  `sorted((DOCS / 'api').glob('AiDe*.md'))`, sorting **`Path` objects**: `pathlib` compares Windows
  paths case-INsensitively and POSIX paths case-sensitively, so `AiDe.App.md` and
  `AiDe.App.ViewModels.md` swap places between hosts. Fixed with `key=lambda p: p.name` — a plain
  string sorts identically everywhere. The same pass pinned the generator's newline handling
  (normalise on read, `newline='
'` on write), which was a second latent instance of the same class
  in the same file.
- **What made it expensive, and what fixed that:** the gate reported *"docs/_site/index.html is
  stale"* and nothing more — a 700 KB generated file, with the diagnostic cost pushed onto whoever
  read it, on a host they could not reproduce. Two full CI cycles went into guessing at mechanisms
  (line endings, embedded dates, the commit SHA) that turned out to be wrong. `verify-derived-views`
  now prints the **first diverging byte with context from both sides**, and on the very next run that
  output named the cause in one line. **A gate that detects but does not localise is only half a
  control** — the same shape as `verify-test-run` reporting *"1 failed"* without naming the test.
- **Control:** `verify-derived-views` runs in the `gates` job on **Linux**, while developers run it on
  Windows — so this class is now caught by construction on every push rather than by anyone
  remembering it. Cross-platform disagreement in a generator cannot survive a gate that runs on the
  other platform. Observed failing on the un-fixed code: run `34001468310` and the run after it.
- **The product-side sibling, found the same way (2026-09-06).** `CSharpProjectReader` located .NET
  reference packs at `Path.Combine(Environment.GetFolderPath(SpecialFolder.ProgramFiles), "dotnet",
  "packs", …)`. On Unix that call returns the **empty string**, so `Path.Combine` produced the
  *relative* path `dotnet/packs/…`, probed against the current directory rather than rejected — no
  pack is findable off Windows. The consequence is worse than a crash: with no pack the compilation
  carries **no framework references**, so `[Table]` is not recognised (no `declares_table` emitted)
  and `Console` and `List<T>` stop being classified as runtime types. **Extraction still succeeds and
  still returns facts, just fewer and quieter ones.** A note was recorded and the run stayed green.
  This is the same host-dependence as above but on the *product* rather than a generator, and the
  gate that found it was simply running the suite somewhere new. Fixed by deriving the .NET root from
  the runtime the process is already executing on, with `DOTNET_ROOT` ahead of it and an empty
  `ProgramFiles` no longer able to yield a relative probe.
- **Two more product-side instances, both in path COMPARISON (2026-09-10, node FX2).**
  `FileSystemRepositoryLocator` cut the worktree pointer at
  `gitDir.IndexOf(marker, StringComparison.OrdinalIgnoreCase)` — two host-dependent primitives in one
  expression. `IndexOf` takes the **first** match, so the common `~/worktrees/<project>` convention
  put a second `worktrees` segment in front of the one git writes and the repository resolved two
  levels too high; that half is wrong on Windows as well, and reddened there. `OrdinalIgnoreCase`
  is the POSIX half: on Linux it resolved `/repo/.GIT/WORKTREES/x`, a path git could not have
  written. Separately, `ProofPackVerifier` matched the evidence directory with `OrdinalIgnoreCase`
  at `:189` while `IsInside` at `:221` already asked the platform — **one file holding both
  answers** — so on Linux `DOCS/PROOF/x.md` was admissible as a Proof Pack path. The exposure is
  small and worth saying so: the verifier has no production caller on this branch. The class is not
  small — a verifier that accepts a spelling nothing in the system would ever WRITE makes `Verified`
  mean something other than what its readers take it to mean.

  **This is the third repair of this shape at one call site at a time** (INV-0005, then
  `RepositoryIdentity.ToFileSystemPath`, now these two). `WatcherIdentity.cs:99-102` states the rule
  — *"THIS IS NOT A FILESYSTEM PATH"* — in prose, and prose is what the next boundary gets written
  against (CI6). The recommended control is a **`PathSemanticsGate`**: a Roslyn-free source scan,
  in the `gates` job beside the other Python controls, refusing a hardcoded `StringComparison.Ordinal`
  or `OrdinalIgnoreCase` on any expression whose name says path (`*Path`, `*Dir*`, `*Root`, `gitDir`,
  `relative`, `full`, `marker`) unless it comes from a named platform-conditional helper. Estimated
  half a day including `--self-test`. It is a lint, not a type — the type distinction
  (`RepositoryIdentity` vs a `FileSystemPath` struct) is the stronger control and roughly a week,
  because every boundary in `Watcher/` would have to be re-typed at once.

- **The generalisation:** *a reproducibility check is only as portable as its generator's least
  portable primitive* — and the cheapest way to find those primitives is to run the check somewhere
  other than where the artifact was made. Prefer explicit, value-typed keys (`key=lambda p: p.name`)
  over relying on a library type's comparison semantics, which are allowed to differ by platform.
- **Status:** `partially-controlled` — the GENERATOR half is controlled: `verify-derived-views` runs
  on Linux while the artifacts are made on Windows, so a host-dependent generator cannot survive a
  push. The PRODUCT half is not. Four product-side instances now, each caught only because a test
  that could see it happened to exist, and nothing fails when the next path comparison picks one
  platform's rules. The control that would close it is named in the instance above and is not built

### DC-109 — A helper creates a FOREGROUND thread, so a hung body outlives the run and the process cannot exit

- **Shape:** a test helper starts a thread, waits on it with a timeout, and reports honestly when the
  wait expires. What it does not do is stop the thread — and `new Thread(...)` in .NET is a
  **foreground** thread, which keeps the process alive on its own. The run finishes, the report is
  correct, and the host never exits.
- **Signature:** the symptom never appears in the run that caused it. It appears in the NEXT command,
  as a build failing with *"the file is locked by testhost"* (MSB3027), or as a `dotnet test` that
  seems to hang far past the suite's normal duration. Both read as infrastructure or flakiness, so
  the response is a re-run rather than an investigation.
- **Why it survives:** every visible part is correct. The `Join` waits, the assertion fires, the
  failure message says exactly what happened. The leak occurs *after* the reporting, and nothing in
  the run's own output can show it — the evidence is a process that outlives the process that would
  have told you.
- **Instance:** 2026-09-05/06 — `tests/AiDe.App.Tests/Sta.cs` started both its STA threads as
  foreground. Twice in one day an orphaned `testhost.exe` held the test assembly's DLLs: once
  surfacing as `MSB3027` mid-build, once as a `dotnet test` that ran for over twenty minutes against
  a suite that takes thirty seconds. Both were initially read as environment problems, and the second
  cost a full CI-length wait before anyone looked at the process list.
- **Control:** `thread.IsBackground = true` on both threads. A background thread cannot hold the
  process open, and nothing else changes — the same wait, the same assertion, the same message. The
  structural half is that **there is exactly one STA helper**: DC-079 consolidated thirty-two
  hand-rolled copies into `Sta`, so this defect had one place to live and one place to be fixed.
  That consolidation is what makes a single-line fix a complete one.
- **No mechanical gate, and the reason is proportionality rather than oversight.** A check refusing
  `new Thread(` in `tests/` without a nearby `IsBackground` would today be watching a single file
  that is already correct. If a second thread-creating helper ever appears, that check becomes worth
  writing — and DC-079's consolidation is the thing that would have to fail first.
- **The generalisation:** *a timeout that reports without terminating has handled the message, not
  the thread.* Any wait-with-deadline should be asked what happens to the work when the deadline
  passes — and if the answer is "it keeps running", whether it can still hold something open.
- **Status:** `partially-controlled` — fixed at the only site that can exhibit it, verified by a
  clean process table after a full run, with no gate because there is currently nothing else to gate

---

### DC-110 — A partition value derived from the INGEST PATH rather than from the work

- **Shape:** a record is filed under a key that names *how it arrived* instead of *what it is*. Each
  door hard-codes its own default for a field that is part of a comparison key, so two records of the
  same work, entering by different doors, are filed as different kinds of thing. Nothing is
  mislabelled and no code is wrong at either door — the defect exists only in the space between them.
- **Signature:** comparisons that should be like-for-like silently return nothing, or return a
  ranking with one side missing. Because each door is individually correct, the symptom presents as
  "the board looks sparse" rather than as a fault, and the natural response is to doubt the data
  volume rather than the key.
- **Why it survives:** the defaulted parameter reads as a sensible convenience at each call site. The
  scoring contract itself is innocent — it *requires* the value from the caller. The divergence is
  invisible in any single file, and appears only when someone asks the specific question "do these
  two land in the same cell?", which nobody asks until a requirement forces it.
- **Instance:** 2026-09-09 — `WatcherHost.ImportAndScoreEpisodesFromAuditLog` defaults
  `taskClass = "audit-import"` (`WatcherHost.cs:118`) while `ClosedEpisodeScoring.Run` defaults it to
  `ScoreSegment.Unclassified` (`ClosedEpisodeScoring.cs:69`). `taskClass` is one of the three axes of
  `ScoreSegment`, the leaderboard partition key. So an observed-lane episode and a governed-lane
  episode of the *same work* can never share a cell — and worse, `Unclassified` is
  `IsComparable == false` (`Leaderboard.cs:66-70`), so the governed side ranks **nowhere** while the
  observed side ranks in a cell named after its door. Found while checking whether Conductor spec R4
  ("leaderboard cells never split by mode") could be satisfied: the split R4 forbids was already
  present, wearing a different column's name.
- **Control:** the positive test, not the negative one. A test that asserts "mode is absent from the
  partition key" proves a compile-time tautology and cannot fail for the right reason. The control is
  a test that pushes one episode through *each* door under **one caller-chosen task class** and
  asserts they land in **one cell as two cohorts**. Standing rule: **the door determines the mode;
  the work determines the task class; never the reverse.**
- **The generalisation:** *a default that is convenient at the call site becomes a fact about the
  record.* Any defaulted parameter should be asked whether it feeds a key, an index, or a comparison
  — and if it does, the default belongs to the work, or there is no default at all.
- **Status:** `partially-controlled` — the one-cell-two-cohorts test is the control for the
  comparison; retiring the door-named `"audit-import"` default itself is deferred to Phase 3, when
  controlled task classes arrive, because changing it now would move existing observed episodes from
  a comparable cell into `Unclassified` (a history-rule change with no spec basis)

---

### DC-111 — Evidence the repo REQUIRES committed lives under a blanket ignore, reachable only by `git add -f`

- **Shape:** a directory is ignored wholesale by an inherited rule written for a different purpose
  ("per-run state, never committed"), while the repository's own conventions treat that directory as
  the home of **committed evidence**. Both facts are true at once, so the tree looks correct: the
  evidence is there, tracked, and reviewable. What is missing is any mechanism ensuring the NEXT
  piece of evidence arrives — it does so only because each author happened to remember `-f`.
- **Signature:** there is none at the moment of loss. `git add` reports nothing, `git status` shows
  nothing, and the commit succeeds with the evidence simply absent. The defect is a **negative**, and
  negatives are invisible unless something asserts on them. It surfaces much later, as a spike or a
  proof whose artifact "was definitely captured" and is not in the history.
- **Why it survives:** the ignore rule is inherited and looks authoritative, and the many already-
  tracked files under the same path are read as proof that the path works. Nobody re-derives the
  ignore state for a directory that visibly contains tracked files. The rule and the convention were
  written by different authors for different purposes and have never been compared.
- **Instance:** 2026-09-09 — `.gitignore:523` carries a blanket `spikes/` from the AI-Forward Pack
  ("local coordination and per-run state, never committed"), while `git ls-files spikes/` returns
  **117 tracked files**: every prior spike's committed deliverables. So every one of those commits
  used `git add -f`. Found while capturing the ACP frame corpus, which the approved Phase-1 plan
  makes the **test oracle for both sides of a wire contract** — evidence whose loss would not have
  been noticed until the contract was already built against author-written events instead.
- **Relationship to the `.agents/` finding (same day, same shape, opposite resolution):** there, a
  pack ignore rule would have hidden the loomkeeper contract logs, and the repo had already
  **deliberately declined** the rule with the reasoning recorded in `.gitignore`. Here the rule was
  accepted and then routed around per-commit. The discriminator is whether the divergence was
  **recorded once** or **re-improvised every time**.
- **It was not hygiene — it was a REGRESSION of a recorded decision, and that is the sharper
  finding.** `.gitignore:495-497` states: *"spikes/ is NOT ignored in this repo (pack default
  overridden): a contract labelled Verified must cite committed, re-runnable spike evidence
  (Test Architect gate, 2026-08-26)."* Twenty-nine lines later, a pack INSTALL-2 block silently
  re-appended a blanket `spikes/` — and **git's last-match rule made the regression win.** Two
  contradictory statements in one file, the later one authoritative by accident. A pack update
  overwrote a repo decision that the file itself recorded, and nothing failed.
- **Fix applied 2026-09-09:** the pack blanket was **removed** rather than negated. A negation after
  a blanket is fragile — the next pack update re-appends the blanket and the negation's position
  decides the outcome. Verified in **both** directions with `git check-ignore --quiet`, never `-v`
  (which prints the negation and exits 0, inverting the answer): the ACP frame corpus at
  `spikes/acp-subscription-lane/frames/read.jsonl` exits **1** (not ignored, reachable by plain
  `git add`), while `spikes/msbuild-task-execution/fixture/markers/` still exits **0**, so the
  narrower intentional exclusion survived the edit.
- **Control: DEFERRED, and the register stays honest about it.** The gate would be a new
  self-tested check (DC-104 requires `--self-test`) plus CI wiring; no existing `verify-*.py` is a
  semantic home. **Trigger: before DC-111 is moved from `uncontrolled` to `controlled`.** Until
  then the repair is real but nothing fails when the shape recurs — which is exactly why the status
  below is not being upgraded.
- **The generalisation:** *a convention that works only because everyone remembers a flag is not a
  convention, it is a streak.* Ask of any required artifact: if the next author does the obvious
  thing, does the artifact arrive?
- **Status:** `uncontrolled` — the divergence is now recorded, but nothing yet fails when the next
  spike's evidence is silently dropped

---

### DC-112 — A per-clone install run inside a WORKTREE rewrites the parent clone's config, because a worktree is not a clone

- **Shape:** a setup step is documented as *per-clone* and is therefore run again in each new working
  tree. But a git worktree **shares `.git/config` with its parent** unless `extensions.worktreeConfig`
  is set, so the "second install" does not create a second configuration — it **overwrites the first**,
  rewriting absolute paths to point at the newest, and most temporary, tree. The instruction is
  followed exactly and the result is worse than skipping it.
- **Signature:** none while the tree exists, because the rewritten path still resolves and the script
  it names is byte-identical. The failure arrives **after cleanup**, in a *different* operation
  (a merge), in a tree that never ran the install, naming a directory that no longer exists. By then
  nothing connects the symptom to the setup step that caused it.
- **Why it survives:** "per-clone" is *true* of `.git/config` and is the reason the instruction
  exists; the inference that a worktree is a clone is the only wrong step, and it is invisible
  because the observable state immediately afterwards is correct. `doctor` reports the drivers
  **registered and effective** either way — it checks that a driver is declared, not that the path
  it names will outlive the tree that wrote it.
- **Instance:** 2026-09-09 — `AGENTS.md` and the pack's INSTALL step both say to run `coord install`
  inside each new worktree, citing "per-clone `.git/config`". Doing so in
  `ai-de-feature-conductor-agent-plane` repointed the **main clone's** `merge.coord-regen.driver` and
  `merge.coord-register.driver` at
  `C:/Projects/ai-de-feature-conductor-agent-plane/docs/ai-forward-pack/scripts/coord-core.py`.
  Confirmed with `git config --show-origin` (`file:.git/config`) and `extensions.worktreeConfig`
  unset. The plan's own fail-safe cleanup deletes that tree at converge, after which **every future
  merge of `docs/audit/audit-log.jsonl`, `docs/docs-index.js`, `docs/api/*.md` or `.agents/log/*.jsonl`
  — all ten declared in `.gitattributes` — would invoke a missing script**, in a repo whose whole
  coordination story is that those artifacts merge automatically.
- **Control:** none yet. `doctor`'s `merge driver` check reports *declared and registered* and is
  blind to whether the configured path exists or points outside the clone. The candidate control is
  one assertion in that check: the driver's script path must **resolve** and must lie **inside the
  clone that is being checked**. Recorded as a finding for ruling, with the repair applied.
- **It is BOTH halves of `install`, not just the drivers — this entry was incomplete when first
  written.** Confirmed upstream and then re-confirmed here: **`.git/hooks` is shared through the
  common dir exactly as `.git/config` is** (`git rev-parse --git-path hooks` from the worktree
  resolves into the primary `.git`). So the worktree install ALSO rewrote the shared **pre-commit
  floor**. Measured in this repo: `.git/hooks/pre-commit` line 4 named
  `C:/Projects/ai-de-feature-conductor-agent-plane/.../coord-core.py`. That is the worse half —
  losing the merge drivers breaks merges of ten declared paths, but losing the hook would have made
  **every commit in the repository** invoke a missing script the moment the tree was deleted.
- **Repair applied:** both merge drivers **and** the pre-commit hook repointed at
  `C:/projects/ai-de/...` — the durable clone — each verified: the drivers resolve, and the hook was
  observed firing ("2 staged path(s) checked - all free or mine") on a rolled-back test commit. A
  sweep of `.git/config` and `.git/hooks/` now returns **zero** references to the worktree.
  **The correct general instruction is that a worktree needs no `coord install` at all**: it inherits
  the parent's config *and hooks*, and running one there can only overwrite them.
- **Fixed upstream, 2026-09-09 (pack revision 64).** `coord install` now **refuses** from a linked
  worktree (`COORD-INSTALL-IN-WORKTREE`, `--force` the recorded exception); `coord doctor` gained a
  `driver_path_status` check; nine surfaces corrected. Note the upstream agent **narrowed the check
  this entry originally proposed**: "the path must lie inside the clone being checked" wrongly
  flagged a legitimate out-of-tree script install, so the rule is **"not inside a linked worktree"**,
  which is the actual hazard and catches it from both sides.
- **The generalisation:** *"per-clone" and "per-working-tree" are different scopes, and git only
  makes them the same when you ask it to.* Any setup step documented as per-clone should be asked
  what it does when run in a worktree — and if the answer is "overwrites the parent", it must not be
  run there.
- **Status:** `partially-controlled` — the local repair is done and verified (drivers and hook
  repointed, zero worktree references left in shared git state), but nothing *in this repo* yet
  fails when the shape recurs. It is `controlled` **upstream** at pack revision 64, where
  `coord install` refuses from a linked worktree and `coord doctor` checks driver paths; this repo
  inherits that control at its next `/updatepack`, and the status moves then, not before

---

### DC-113 — A gate made advisory by the SHAPE OF THE SHELL LINE, not by any decision

- **Shape:** a verification command is piped into something that formats its output — `| tail`,
  `| head`, `| grep`, `| Select-Object` — and the pipeline's exit status becomes the **formatter's**,
  which is ~always 0. Chained with `&&`, the following step then runs **whether the gate passed or
  failed**. Nobody decided the gate should be advisory; the shell line decided it.
- **Signature:** none, and it is worse than silence — the gate's own FAILURE TEXT is printed, right
  above the successful next step. A reader scanning for red sees red, and also sees the commit
  succeed, and reconciles the two as "it warned but was fine". The output and the exit code disagree,
  and only one of them is load-bearing.
- **Why it survives:** piping to `tail` is the natural way to keep a verbose gate readable, and it is
  correct everywhere the reader is a human who reads the line. It only becomes a defect when an `&&`
  turns the discarded status into control flow — so the habit is right in one context and wrong in
  the next, with no visible difference between them.
- **Instance:** 2026-09-09, self-reported by the N4 agent rather than hidden: it ran
  `verify-test-run.py | tail`, so a **FAILED gate still let an `&&` chain reach `git commit`**. The
  committed content happened to be correct and both halves were subsequently verified green with the
  status preserved — so the defect cost nothing this time, which is exactly why it is worth
  registering rather than forgetting. Confirmed in isolation: `false | tail -1` exits **0**.
- **Near-miss for the conductor, recorded honestly:** the conductor's own pre-commit chains used
  `>/dev/null 2>&1 &&` (redirect, which preserves status) and read the message text where it piped
  to `tail`. It was not bitten — **by habit, not by design**, which is not a control.
- **Control:** none yet. Candidates, cheapest first: run gates **bare** before a chained step and let
  the status do its job; where output must be trimmed, capture to a file and check `$?` first; in
  PowerShell `$LASTEXITCODE` after a pipeline reports the last **native** command, which is a
  different trap in the other direction. Not gated here — CI does not have this shape (its steps are
  bare `run:` lines), so the exposure is interactive and agent sessions only.
- **The generalisation:** *an exit code that has been through a pipe is a statement about the last
  program in the pipe.* Any `cmd | fmt && next` should be read as "run next regardless" — and if that
  is not what was meant, the pipe is in the wrong place.
- **Relationship to DC-111 and DC-112:** all three are one meta-shape found on the same day — **a
  control that is off, where "off" is indistinguishable from "on and quiet."** DC-111: evidence
  behind an ignore rule. DC-112: a config path pointing at a doomed directory. DC-113: a status
  discarded by a pipe. In each the mechanism reports success, and the absence has no signature. That
  meta-shape is the thing to look for, and it is why each was found by *measuring the control itself*
  rather than by trusting its green.
- **Status:** `uncontrolled` — recorded, with the exposure scoped to interactive and agent sessions

---

- **Recurrence 2 (conductor-addendum-c, 2026-09-11) — the conductor's own shell lines, twice in
  one hour:** (i) a `for g in …; do python tools/$g.py; echo "exit $?"; done` loop printed
  `exit 1 verify-defect-register` and the line went on to `git push origin main` because the
  loop's exit status is its last `echo`; (ii) `python tools/regenerate-derived.py; git add -A …;
  git commit` committed a merge while the resolver's new marker gate (DC-136) had just printed
  `FAILED conflict markers`, because `;` does not stop. Both pushed or committed red. The
  control that held: `&&`-chaining every gate before the act that depends on it — a red step then
  stops the line — applied to every resolution line since; the pre-commit hook does not run the
  gate set and should not (it would double CI). The class is unchanged; the instance is the
  conductor forgetting its own register.

### DC-114 — A fix to the deployment mechanism cannot deploy itself: correct, tested, green, and unreachable

- **Shape:** the thing being fixed is the thing that performs the fix. An updater, installer,
  migrator or applier is repaired at the source, and every gate passes — but the repair only takes
  effect once it is *already installed*, and the program that installs it is **the old copy**. The
  old copy computes the plan; the new copy is merely one of the files that plan copies. So the
  first refresh after the fix runs the **unfixed** logic, and does the exact thing the fix existed
  to prevent.
- **Signature:** none at the source, which is the trap. Every local signal is green — tests pass,
  gates pass, CI passes, a fresh clone passes — because all of them measure whether the fix is
  CORRECT. None of them asks whether it can REACH anyone. The failure surfaces in a different
  repository, at a later date, as the deliberate repair quietly reverting.
- **Why it survives:** verification naturally points at the artifact you changed. Asking "can this
  fix reach a consumer?" requires running the *consumer's own installed copy* against the new
  source — an inversion nobody does by habit, because it means deliberately using the stale program
  you just replaced.
- **Instance:** 2026-09-09 — AI-Forward Pack revision 64 taught `pack-apply.py` to withhold a
  gitignore line a repo had explicitly declined. All 11 bundle gates passed, three CI workflows were
  green, and a fresh clone reproduced it. Then the consumer-side question was finally asked: ai-de's
  **installed rev-63** `pack-apply.py`, run against the rev-64 source, proposed
  `UPDATE | added spikes/, .agents/*, !.agents/artifacts.yml` — **exactly the two lines rev 64
  existed to withhold, and exactly the two repairs made in this repo that same day** (DC-111's
  spikes override and the `.agents/` capture logs). The mechanism was right and unreachable.
- **Control (upstream, revision 65):** `pack-apply` compares the running copy against the source's,
  prints a `STALE-APPLIER` row on `plan` and **refuses to `apply`** (`--allow-stale` is the recorded
  exception, whose help states it applies the OLD map); absent or unreadable is deliberately not
  treated as stale. `/updatepack` now copies the program as its first mechanical step, so the
  refusal never fires in normal use.
- **The residual is unavoidable and must be stated, not engineered away:** a repo already holding a
  pre-fix copy gets **no warning**, because you cannot make an already-deployed old program warn
  about itself. The first hop after the fix depends on a person following the deploy note. **Applied
  here:** `pack-apply.py` was copied into this repo manually, and the proposal changed from
  `UPDATE — added spikes/, .agents/*` to three `KEEP` rows citing the decline marker and the 21
  tracked files under `.agents/`.
- **The generalisation:** *a gate's green is evidence the gate passed, never evidence the work
  landed.* For any change to a mechanism that propagates changes, the acceptance test is not "does
  it work here" but **"run the consumer's existing copy against the new source and read what it
  proposes."**
- **CLOSED BY CONSTRUCTION upstream at revision 66, not merely warned about.** The residual above
  was stated as unavoidable — *"you cannot make an already-deployed old program warn about itself"* —
  and that premise is true but its conclusion was too narrow. **The old program is not the only
  thing in the loop.** Two levers closed it:
  1. `/updatepack` already mandates reading the **source's** `INSTALL.md` `changes` frontmatter, so
     a source-side deploy note reaches a stale target on hop 1 — the one hop nothing else reaches.
  2. **The invocation was inverted.** The documented flow now runs the **source clone's**
     `pack-apply.py` with `--target .`, so the target's stale copy never computes a plan at all and
     staleness becomes **impossible rather than detected**. It required no new capability: `--target`
     already existed and every subprocess was already pinned to the target's cwd; only `--source`
     being `required=True` stood in the way. `STALE-APPLIER` remains as the backstop.
- **Demonstrated against a genuine pre-fix applier taken from git history**, three arms:
  **Arm 1** (old flow, stale copy): 0 `STALE-APPLIER` rows, the declined `spikes/` line re-appended,
  **and new spike evidence silently dropped by `git add -A`** — the residual reproduced *including*
  the evidence loss. **Arm 2** (new flow, same stale copy still on disk but not running): `KEEP`
  withheld the declined line; evidence staged. **Arm 3** (old invocation at rev ≥ 64): hard refusal.
- **Second instance, and it is the sharper one: the fix's own check asked the wrong question.**
  Revision 65's staleness check compared the target's **installed** copy with the source's, not the
  copy that was **running** — so it accused the source's own script of being the target's stale one,
  and that false positive **hid the cure by flagging the inverted invocation as the disease.** Its
  row text was literally false in that case. Now keyed on the running script, with the tests
  re-expressed as **subprocess** tests, because staleness is a property of the program that runs and
  cannot be tested in-process. *(Two of those tests were also caught passing **vacuously** first —
  argparse errored, so the assertion inspected empty output: the same proves-nothing failure the
  whole revision is about.)*
- **Registered upstream as `BOOT-A`** in the pack's own register, carrying the control and the
  generalisation rather than only the instance.
- **Status:** `controlled` — closed by construction upstream at revision 66 (remote `c20c2df`, three
  CI workflows green, fresh clone 11/11); this repo's own first hop was closed by hand and verified
  by the plan diff above. The one uncovered path — a person below rev 64 invoking their own copy
  directly *and* reading no documentation — is unreachable by any mechanism, since every artifact in
  that path is the stale one

---

### DC-115 — Evidence committed on a lane's BRANCH is invisible to a verifier that reads the parent checkout's WORKING TREE

- **Shape:** a control resolves an identity to its canonical form — a linked worktree to the
  repository it belongs to — and a *second* consumer then uses that canonical identity as a
  **filesystem path to read a file from**. The canonicalisation is right for grouping and wrong for
  reading: the parent checkout is a different working tree, on a different branch, and the file the
  lane committed is simply not in it.
- **Signature:** none at the boundary. `ProofPackVerifier` answers `NotFound`, which is its honest
  answer to the question it was asked — the file really is not at that path. `EvidenceFor` turns
  that into `HasProofPack: false`, `DeterministicSignalsDeriver` into "no verification path", and
  `WeaveScore` into **`Not Scored`** with the reason *"no minimum verification path"*. Every layer
  is individually correct and the composed statement — *this agent produced no evidence* — is false.
  The scorecard then makes a claim about the agent when the true claim is about where somebody
  looked.
- **Measured, both halves, 2026-09-09 during N7:**
  1. `C:/Projects/ai-de-feature-conductor-agent-plane/.git` is a **file** reading
     `gitdir: C:/Projects/ai-de/.git/worktrees/ai-de-feature-conductor-agent-plane`, so
     `FileSystemRepositoryLocator` resolves it to `C:/Projects/ai-de` and
     `RepositoryCorrection.Apply` rebinds the session there — correctly, by its own contract.
  2. `C:/Projects/ai-de/docs/proof/conductor-agent-plane.md` **does not exist**: that checkout is on
     `main`, and the Proof Pack is committed on `feature/conductor-agent-plane`.
- **What it cost here: nothing, because the exit run was rooted in a clone instead** — a repository
  root whose `.git` is a directory, so no correction fires and the declared path resolves inside the
  tree that holds it. The run scored `Partial: 15 / 15 observed` with `IsComparable == true`. That is
  a statement about the shape the run happened to take, **not** about the defect being absent: a
  governed lane in a linked worktree of the operator's own checkout — the shape spec §6.4 describes,
  and the one the shell provisions — cannot be credited for evidence it commits on its own branch.
- **Why it survives:** the comment on `ProofPackVerifier.Verify`'s own parameter asserts the
  opposite — *"the corrected one, so a worktree-registered agent is checked against the repository
  its evidence is actually committed in"* — which is true for a worktree sharing the parent's branch
  and false for every agent branch. A reasoned-through comment is the hardest kind of wrong claim to
  notice, because it reads as though somebody checked.
- **The asymmetry that makes it worse:** the *observed* door does not have this problem.
  `AuditLogEpisodeSource.HasProofPackArtifact` credits a declared path by **substring** and never
  touches the filesystem, so an audit-imported episode is evidenced by naming a path while a governed
  one must have the file present in a checkout it does not control. Two doors, two definitions of
  "has evidence", and the stricter one applies to the lane the product itself drove.
- **Correction to the two observations above, measured 2026-09-09 during Phase 2 N0:** the
  correction is *one* route to the defect and not the one spec §6.4 takes. A governed lane registers
  `repo.path` as the **parent** already — `LaneIdentity.RepositoryPath` says so in its own doc
  comment — so `RepositoryCorrection` never fires for the shape the product provisions, and the
  episode still scores `Not Scored`. The essential shape is narrower than the register said: **any
  session whose `Repository.CanonicalPath` is not the checkout its evidence is in**. Both routes are
  now covered by tests; fixing only the corrected one would have left the provisioned shape broken.
- **Control (partial), 2026-09-09, Phase 2 N0:** `ProofPackVerifier.VerifyInCheckouts` folds a
  verdict across the session's checkouts — `Verified` beats `NotFound` beats `Unverifiable`,
  order-independently, with containment applied whole against each root so several roots is several
  complete checks and never a widened one. `ClosedEpisodeScoring.CheckoutsOf` decides which roots are
  legitimate: the repository always, plus the lane's own tree **only when `IRepositoryLocator` reads
  its `.git` pointer and finds the bound repository**. `worktree.path` is registrant-composed like
  every other attribute, so admitting it on the claim alone would let a session keep an honest
  `repo.path` — board and cohort looking right — while pointing verification at any directory on the
  machine; the claim is checked, never trusted. Candidate (a) of the three below, tightened.
  **Tests:** `AGovernedLaneIsCreditedForItsOwnBranchTests` (a real repository, a real linked
  worktree, a Proof Pack committed on the lane's branch, driven through `GovernedSessionSource` and
  the real `FileSystemRepositoryLocator`; both routes, plus the two refusals) and
  `ProofPackVerifierAcrossCheckoutsTests` (the tri-state, where scoring's bool cannot see it).
  Observed failing on the un-fixed shape before the fix — `Not Scored — no minimum verification
  path` — and again on three deliberate breaks afterwards. Decision:
  `docs/notes/dc-115-evidence-in-a-lanes-own-checkout.md`.
- **Recurrence at a SECOND boundary, measured 2026-09-10 on Linux CI (two commits, `e2c746d` and
  `60ae4a9` — stable, not flaky):** the generalisation below was written down and the sweep was never
  run. `RepositoryCorrection.Apply` hands `Repository.CanonicalPath` — an identity — straight to
  `IRepositoryLocator.RepositoryFor`, which opens `<path>/.git`. `Canonicalise` writes a backslash on
  every platform on purpose, so on Linux the locator was asked about `\tmp\aide-x-lane`: one
  filename with no separators, `File.Exists` false, answer `null` — *nothing to correct*. **No linked
  worktree was ever corrected on Linux**, and nothing said so, because `null` is also the honest
  answer when the registrant's path is simply not ours. Portable half: 1,885 executed, 1,884 passed.
  It was caught by `ACorrectedWorktreeRegistrationIsCreditedForEvidenceOnItsBranch` — which uses the
  REAL locator — while the stub in `AWorktreeRegistrationIsCorrectedAndSaidSoTests` canonicalises its
  own argument before comparing, so it agreed with the caller's mistake and stayed green. **A double
  that normalises its input cannot see a normalisation defect in its caller.**
- **Control, 2026-09-10:** `RepositoryIdentity.ToFileSystemPath` — ONE named conversion, called at
  both boundaries: `FileSystemRepositoryLocator.RepositoryFor` (the miss) and
  `ProofPackVerifier.Verify` (which held the only previous copy, inline). The rule existed in three
  comments and nowhere executable, and prose is what the next boundary gets written against (CI6).
  `TheFileSystemRepositoryLocatorTests.ALinkedWorktreeResolvesToItsRepository_ByPathAndByIdentity`
  now asks the locator with the identity spelling as well as the raw path.
- **The residue, stated because it is the uncomfortable half:** that control **cannot redden on
  Windows**, where an identity and a filesystem path are the same string — the divergence exists only
  where `\` is not a separator. Making it fail locally would take a seam that injects the separator,
  which distorts the code to model the platform instead of running on it. So the control for this
  class is **CI on a POSIX runner on every branch**, not the local gate, and a Windows-only proof of
  a path change is not a proof.
- **What the control does NOT reach, and why:** (b) ask git for the blob
  (`git -C <worktree> cat-file -e <branch>:<path>`) — declined: `AiDe.Core` does not shell out, and a
  process launch per declared artifact on an idempotent sweep is a cost paid forever. It is the only
  candidate that covers **a lane whose tree was released before the sweep ran**, which therefore
  remains uncontrolled: the branch keeps the commit, the tree is gone, the locator answers "unknown",
  and the verdict falls back to the parent checkout's honest `NotFound`. (c) give `Unverifiable` a
  route through `EpisodeEvidence` — declined here as out of phase scope: it changes what a scorecard
  says about episodes this node never touched. So "we could not look" still collapses into "there was
  none" at that boundary, and `AnUnverifiableRepositoryIsNotEvidenceOfAbsence` still pins the limit at
  the verifier. **The asymmetry between the two doors is also untouched** — an audit-imported episode
  is still evidenced by *naming* a path (substring, no filesystem) while a governed one must have the
  file present in a checkout.
- **The generalisation:** *a canonical identity is not a path.* The moment a value normalised for
  grouping is handed to `File.Exists`, ask which of the several real directories it now names — and
  whether the one it names is the one holding the thing you are looking for.
- **Relationship to DC-110** (a partition value derived from the ingest path rather than the work):
  sibling. Both are one value serving two purposes that disagree; DC-110's split the cohort, this one
  empties the evidence.
- **Status:** `partially-controlled` — a governed lane in a **live** linked worktree is now credited
  for evidence on its own branch, by a control observed failing first. Three residues stay open and
  are named above: a released tree, the `Unverifiable` collapse at `EpisodeEvidence`, and the
  two-doors asymmetry. Not upgraded to `controlled`, because each of those is a real case in which an
  agent that produced evidence is still told it produced none.


### DC-116 — The conductor briefs an agent with a repo fact asserted from memory, dressed as verified

- **Shape:** a coordinator hands a delegate a factual premise about the repository — a file that
  exists, a flag a script supports, a convention the codebase follows — **carrying a citation**: a
  line number, a filename, a version. The citation makes it read as *checked*. It was not checked;
  it was recalled, or carried across from an adjacent repository. The delegate is now working from a
  false foundation supplied by the one party it has most reason to trust.
- **Signature:** none, when it works. A delegate that trusts the premise builds on it and the error
  surfaces later, somewhere else, attributed to the delegate. **The only signal is a delegate that
  checks anyway** — which means the defect is invisible in exactly the delegations that are least
  careful.
- **Why it survives:** it is the coordinator's *job* to compress context for delegates, and a brief
  with specifics is a better brief. The failure is invisible at the moment of writing, because the
  recalled fact feels identical to a read one — E15's *"never assert the shape of our own code from
  memory"* is easy to honour when writing code and easy to forget when writing a prompt.
- **Instances — three in one session, 2026-09-09, all caught by the delegate, none by the author:**
  1. Told an upstream agent that shipping a gate with `--self-test` was an established convention in
     `ai-forward`. It measured: **1 of 23 scripts**. The real convention is red-first `unittest`.
  2. Told an agent `audit-log.py append` supports `--supersedes`, **with a line number**. It opened
     the source: the flag existed only on `change`. It added the flag, then used it.
  3. Told a spike agent this repo has a root `package-lock.json` "for other tooling". It ran
     `git log --all`: **no commit has ever added one.** The file exists in the *adjacent* repo,
     `ai-forward` — a cross-repository conflation while working in both.
- **Control:** none mechanical, and a lint cannot see it — a prompt is prose. What exists is a
  cheap discipline with a measured hit rate of 3-for-3: **every load-bearing repo fact in a
  delegation brief is either (a) checked in the same turn it is written, or (b) explicitly labelled
  as unverified with an instruction to check.** The second half is what worked here — the briefs
  that said *"verify what you rely on"* and *"evidence is not authority"* are the ones that produced
  the corrections.
- **The generalisation:** *a citation the author did not open is a decoration, not evidence* — and
  attaching one to a recalled fact makes it more dangerous, not less, because it transfers the
  author's confidence without the author's checking. **Whatever you are about to assert to a
  delegate, ask whether you read it or remembered it.**
- **Widened 2026-09-10: it is not only the conductor, and a RULING is not exempt.** In one
  convening, three of the conductor's claims were corrected by Security — a **stale file citation**
  (`CanvasSurface.cs:103,226`, which a sibling node had since moved to `:233-316`), the **lease
  provenance** (*"the lease is the goal block's `lease.exclusive`"*, false against both code and
  spec), and a description of a permission surface that **implied containment it does not have**
  (it is **Dismiss-only** — a notice, not a gate). All three were the same shape: **a code shape
  asserted from memory or from a persona's summary rather than opened.**
  **The lease claim did not originate with the conductor — it was a condition in an Owner ruling**,
  transcribed into the plan verbatim. The Owner named the class and then applied it to itself:
  > *"The conditions I write into the plan are repo facts as much as yours are; **treat them as
  > claims until you have opened the file they cite**, and say so when you file them."*
  **The generalisation:** any authority's output — a ruling, a persona review, a sibling node's
  report — **becomes a repo claim the moment it is written into a plan**, and inherits none of the
  authority's standing as evidence. A third-order instance landed the same day: Security wrote a
  containment sentence, the conductor transcribed it, and the **Owner** caught it false by opening
  the function it described. Three reviewers, and the sentence was still wrong until someone read
  the code.
- **Status:** `partially-controlled` — the "verify what you rely on" instruction is now standing in
  every delegation brief and caught all three instances; nothing prevents the false premise being
  written in the first place

---

### DC-117 — A GUI test suite that drives shell integration cannot complete under a console-less tool host

- **Shape:** a suite spawns real shells to exercise shell-integration behaviour. Under an
  interactive console those shells attach, run and exit. Under a **console-less automation host**
  they attach to nothing, never complete, and the test host blocks waiting for them — so the suite
  neither passes nor fails. **It produces no result file at all**, which is the part that makes it
  hard to read: a suite that fails leaves evidence, and this one leaves silence.
- **Signature:** minutes of wall-clock with almost no CPU (58 s over 35 minutes, measured), no
  `.trx`, no progress output. It reads as a hang in the *product*, or as flakiness, and the natural
  response is a re-run — which succeeds if it happens to land in a different host, cementing the
  "flaky" reading.
- **Why it survives:** the same command is correct. Nothing about the invocation, the suite or the
  machine is wrong — only the **host the command runs under**, which is invisible in the command
  line and absent from every log the run produces.
- **Instances — three, across two phases, before the cause was found:**
  1. Phase-1 N7: one hang, ~18 minutes, no result file, **cause recorded as unknown** and carried
     as a residual with the trigger *"if it recurs it becomes a class before any other Phase 2 work
     proceeds."*
  2. Front-door F1: recurrence, **22 minutes**, no `.trx`; killed, re-run identically, 400/400 green.
     The trigger fired here.
  3. Front-door FT: **root-caused.** `AiDe.App.Tests` spawns ~30
     `powershell.exe -NoLogo -NoExit -EncodedCommand` shell-integration processes that never
     complete under the Bash tool host. **Killing those shells advanced the run immediately** (58 s
     → 92 s CPU), and fresh batches then appeared. **The identical command from the PowerShell
     console host completes and passes 399/399.**
- **Parentage confirmed 2026-09-10, by exact command line rather than inference (DC-123):** the
  `powershell.exe -NoLogo -NoExit -EncodedCommand` string is emitted by the **product**, at
  `ShellIntegration.cs:129` and `:186`, and the test helper's `integration` probe routes through
  that same builder. **So these shells are children of `AiDe.Core.TerminalHost`**, which the
  test launcher abandons. The two classes are one family, one ring apart — and the reaping is
  **inverted**: these shells *are* correctly reaped by a kill-on-close job, while **the helper
  that owns them is not**. Killing the shells advanced the run because it freed the helper's
  waits; reaping the **helper** would have taken the shells with it.
- **Relationship to DC-014:** this is DC-014's shape **one layer up** — there, a console-less host
  could not drive a pseudo-console; here it cannot drive shell integration. Same root, different
  altitude.
- **Control:** run `AiDe.App.Tests` **from a console host**, never from the Bash tool. Recorded in
  the plan's standing constraints so every future node's brief carries it. **Not yet mechanised** —
  a gate that refuses to start under a console-less host is the candidate, and it must be careful
  not to refuse CI, where the suite runs correctly.
- **The generalisation:** *a test that drives the terminal is testing the host it runs under, whether
  or not it means to be.* Any suite that spawns shells, consoles or PTYs should be asked which host
  it needs — and a run that produces **no result file** should be read as a host mismatch before it
  is read as flakiness.
- **Status:** `partially-controlled` — the cause is established and the workaround is known and
  written into the briefs, but nothing fails when the shape recurs; a re-run under the wrong host
  still hangs silently

### DC-118 — Transcription is a width-changing step, and nothing checks the width: a ruling becomes a clause, a clause becomes a guard, and the scope silently moves

> **Heading widened 2026-09-10, and the reason is this class applied to its own entry.** It first read *"a ruling-level collision check passes while the FAIL-CLAUSES derived from those rulings contradict on a declared shared surface"* — which is the **first instance**, not the class. The entry then grew a second instance in the opposite direction and a third in a gate, all sharing one mechanism, while the heading still named only the first. **A heading narrower than its own content is the same defect the class describes.**
>
> **Citation convention, because the conductor got this wrong repeatedly.** Cite **DC-118** for the mechanism — a scope that moved across a transcription hop. Cite **DC-118 control half (b)** (added by Ruling 38) for the specific requirement that *a scan-shaped guard must state its root, recursion, token set and allowlist*. The conductor cited plain "DC-118" for half (b) in several briefs; a reviewing node caught it and supplied the correct provenance.

- **Shape:** two decisions are individually correct and do not conflict *as decisions* — one even
  carves its scope explicitly out of the other. Each is then transcribed into a plan as a node's
  `Fails if:` clause. One clause is written **without a scope qualifier**, so it asserts a
  repo-wide fact; a sibling node is explicitly authorised to make that fact false on a surface
  **the same plan names as shared between them**. The collision check runs at the level of the
  *rulings* and passes — correctly. The contradiction is one level down, in the derivation.
- **Signature:** one document contains both halves — a line naming a surface as shared between
  nodes, and a `Fails if:` over that surface carrying no *"in this node"* / *"on this path"*
  qualifier. Nothing crosses them. The failure surfaces only at the join, as a single red test,
  and often the matched text is the **other node's explanatory comment** rather than its code.
- **Instance (front-door slice, 2026-09-10):** Ruling 23 (session config is JSON — no YAML parser
  existed) became F0's clause *"Fails if: a YAML dependency appears."* Ruling 35 (template
  frontmatter takes an installed YAML dependency, **scoped to the template loader**) became FT's
  clause *"The `.csproj` edit belongs to this node alone — it is the one shared derived surface
  with F0/F1."* F0 implemented its clause literally, as a repo-wide scan of `AiDe.Core.csproj`.
  Green in both worktrees; red on the merged tree, at `pos 1127` — inside the comment FT had
  written to explain **why** it took the dependency. Neither node was defective. The plan was.
- **Why the existing check could not see it:** Ruling 31's collision re-check compares rulings
  against rulings, and Rulings 23 and 35 are compatible — 35 states in terms that 23's ladder
  argument *"does not transfer"*. No ruling-level comparison can find this, because the source
  decisions agree. The Owner's finding on review: *"the contradiction was written into the plan's
  fail-clauses, not only into F0's implementation, and the re-check had both facts in hand without
  crossing them."*
- **Why parallel execution hides it until the most expensive moment:** each node's suite was green
  in its own worktree because **neither worktree contained the other's change**. A guard whose
  subject is a shared surface is not meaningfully evaluated before the join — so the earliest
  possible detection is a plan-time reading, not a test run.
- **Control (named by Ruling 36, minimum form):** run the collision check **per declared shared
  surface**, not only per ruling. For every surface a plan names as shared between nodes, list
  each node's `Fails if:` that names that surface and show them **jointly satisfiable**. A
  `Fails if:` clause carrying no scope qualifier over a declared shared surface is a defect in the
  plan, independent of whether it happens to collide. Lands as a checklist row in the
  plan-producing stage; prose alone is a memoir (CI6).
- **Control, second half (added by Ruling 38 after the second instance):** every **scan-shaped
  guard** — any test that enumerates files and matches tokens — must state in its doc comment the
  **scanned root**, whether it **recurses**, the **token set**, and the **allowlist**; and the plan
  clause it discharges must carry **the same qualifier**. *The mismatch between the two is the
  tell*, and it is mechanically checkable by reading one sentence against one `EnumerateFiles`
  call. A guard is built to be **extended** — Phase 3 adds `RunLogStore.cs` to the allowlist citing
  its ruling — because **a guard that must be deleted to make progress is one people delete.**
- **Second instance, opposite direction, found by this class's own control within the hour
  (front-door F0):** the plan clause *"Fails if: anything writes a run log **anywhere**"* was
  transcribed into `RunLogFile_IsCalledOnlyByItsOwnDeclaration_NothingElseInSessionsWritesThere`,
  whose XML doc **quotes the clause verbatim — "anything writes a run log anywhere"** — while the
  body scans `src/AiDe.Core/Sessions/` with `SearchOption.TopDirectoryOnly`. The guard is
  **narrower than the sentence above it**, and the sentence is what a reader trusts. This direction
  is the more dangerous of the two: a clause written **wider** than its ruling goes **red** at a
  join and is therefore self-announcing, whereas a guard written **narrower** than its clause stays
  **green** while the thing it promised is violated. The repo's own good practice for this is at
  `defect-classes.md:1788` — *state the residual scope explicitly* (*"discovery is scoped to
  `src/AiDe.App` and to the `=>`-bodied form"*). F0 did not; it kept the word *anywhere*.
- **Third gate in one day, found by a node rather than by the class's own control (front-door F2):**
  `tools/verify-surface-ownership.py` iterates `src/AiDe.App/Workbench` **non-recursively**, so
  three new `*Surface.cs` files under `Workbench/Sessions/` sit **outside its scan** while the gate
  reports every surface assigned. Three gates in one day — `SessionPathContractTests`'s YAML scan,
  its run-log scan, and now this — all **narrower than the sentence describing them**, none
  declaring it. **That is no longer three coincidences; it is the house style for scan-shaped
  guards**, and it is what the control's second half exists to change. The node assigned the three
  surfaces by hand and **recorded the gap rather than widening a shared control unilaterally**,
  which is the right call and is why widening it is a **named follow-up node, not prose** (CI6).
- **Both directions are one mechanism:** transcribing a decision into an executable clause, or a
  clause into a guard, is a **width-changing step**, and nothing checks the width. Widening is
  caught by the join; narrowing is caught by nothing.

- **The generalisation:** *checking that two decisions agree does not check that two
  implementations of those decisions agree.* Transcribing a decision into an executable clause
  creates a **new artifact with its own failure modes**, and the tell is asymmetry of width — the
  narrower and more carefully carved the ruling, the more likely the clause derived from it is
  written wider than the ruling it claims to enforce.
- **Landed upstream, not yet installed here.** The control shipped in `ai-forward` as **GO14a**
  in `execution-graph-optimization.md` (`load: always`, beside GO14, which already owns *"a gate
  must state what input would make it fail"*), with checklist rows in the `optimize-graph` and
  `prepare-for-coordination` skills and their Copilot mirrors. Pack **rev 66 → 67**
  (`2026.09.10.1`), remote tip `bf5c93bf36230053c6e06a901978f2b009dc6b17`, three CI workflows read
  back from the API as `success` with the step list checked rather than the job conclusion trusted.
  Both halves went in as **one** directive deliberately: they are one mechanism, and the asymmetry
  only reads as an insight when the two directions sit in the same paragraph. Always-loaded cost
  **+520 est. tokens**, inside the 2% ratchet — and the **per-skill ratchet fired red** before the
  baseline was recorded, which is the growth control working rather than being trusted.
  **No plan-lint exists** in the pack, and one was deliberately not invented: half (a) is not
  evaluable before a join, because separate worktrees never contain each other's change.
- **Installed here 2026-09-10** — ai-de moved pack **rev 63 → 68** (`pack-doctor.py` reads it back;
  the conductor had first written *"rev 66"* here from memory, which was the upstream repo's
  revision at an earlier point rather than ai-de's installed one — **DC-116, committed inside the
  entry for a class about claims wider than their evidence**). GO14a is now present at line 106 of
  **both** harness forms, byte-identical, with two self-verification rows and a DoD row in each of
  the two plan-producing skills.
- **Status:** `partially-controlled` — the directive is **always-loaded**, so it is in context at
  the moment a plan is written, which is the only moment half (a) can be checked. But **nothing
  fails**: no plan-lint exists, and one was deliberately not invented, because separate worktrees
  never contain each other's change, so a shared-surface fail-clause is **not evaluable before the
  join**. Half (b) — *every scan-shaped guard states its root, recursion, token set and allowlist* —
  **is** mechanically checkable (one sentence read against one enumeration call) and is the
  candidate for the first real gate. Until one exists this stays short of `controlled`, because a
  directive an agent must remember to apply is a better memoir, not a control It moves to
  `partially-controlled` when the pack update lands in ai-de, and that is a **recorded next step**,
  not an assumption: the difference between a control that exists and a control that is installed
  is exactly the gap DC-114 is about.

### DC-119 — A "gate set green" claim made from one platform, for a gate set that runs on two

- **Shape:** the repo's suites are split by platform on purpose — `AiDe.Core.Tests.portable` runs on
  **Linux** in CI and `AiDe.Core.Tests.nonportable` is `[Trait("Platform","Windows")]`. A developer
  or agent runs **everything locally on Windows**, sees every suite and every gate pass, and reports
  *"all green"*. The sentence is true of what was run and false of what CI runs. **A Linux-only
  failure is invisible to every local instrument**, so the claim cannot be wrong in a way its author
  can detect.
- **Signature:** a green local run and a red `core-tests` job; a failure whose stack trace is rooted
  at `/home/runner/work/...`; a test that has been failing for **multiple commits** with nobody
  noticing, because each session verified locally and read only the workflow it happened to
  remember. **The tell is a completion claim that names a count but not a platform** — *"2037/2037"*
  and *"41 of 41 gates"* say nothing about which OS produced them.
- **Instance (front-door head-join, 2026-09-10):** the conductor verified Core **2037/2037** and
  **41 of 41** repo gates on Windows, pushed, and reported the tree green. CI's Linux portable half
  was **1885 executed / 1884 passed / 1 failed** — the same single test at `e2c746d` and at the join
  commit, so `main` had been **red on Linux across at least two pushes** while every local
  observation said otherwise. The failing test was
  `AGovernedLaneIsCreditedForItsOwnBranchTests.ACorrectedWorktreeRegistrationIsCreditedForEvidenceOnItsBranch`
  — itself the control for **DC-115**, doing exactly its job on the only platform that could see it.
- **A second, compounding defect in the diagnosis path:** `verify-test-run.py`'s CI output reports
  *"1 failed, 0 errored, 0 aborted, 0 timed out"* and **never names the test**. Reading the workflow
  log is not enough; the failing name is only recoverable by downloading the uploaded `.trx`
  artifact. **A gate that reports a count without an identity makes its own failure expensive to
  act on**, which is how a red build survives two pushes.
- **Why it survives:** the local run is not wrong, it is *narrower than the sentence describing it* —
  which is **DC-118's mechanism applied to a verification claim rather than to a guard.** The claim's
  width ("green") exceeds the evidence's width ("green on Windows").
- **Control:** two halves, neither yet built. **(a)** No completion claim asserts "green" without
  naming the platforms it covers, and CI status is **read back after every push** — observed, not
  inferred from a successful push, and read from the run's own conclusion rather than from a
  wrapper's exit code. **(b)** `verify-test-run.py` should **name the failing tests** it counts, so
  a CI log is self-sufficient. (b) is the cheaper and more durable of the two, because it removes a
  human step rather than adding one.
- **A hole in the CI matrix itself, found while fixing the instance:** the Windows `build` job runs
  Core with `--filter Platform=Windows` **only**, so the **1,889 portable tests never execute on
  Windows in CI at all.** Portable code is verified on Linux by CI and on Windows only by whoever
  happens to run it locally — which is precisely the evidence this class says not to rely on. So
  the two halves compose into a real gap: **a Windows-only regression in portable code has no CI
  check today.** Observed directly: on one red-first push the Windows `build` job was **green**
  while Linux was red, for a defect that reproduces on Windows.
  **Not yet acted on, and deliberately so** — closing it means running ~1,889 more tests per push
  on the more expensive runner, which is a coverage-versus-cost decision the CE-series governs and
  the SRE owns. Recorded here so the choice is made rather than defaulted.
- **Second instance, 2026-09-10, and this time the repository said it out loud.** The conductor
  merged a new gate without wiring it into CI, **noticed that itself**, and handed the wiring to
  another node — then **pushed three times without reading CI back.** `main` sat **red** at
  `7c30c29` for three commits. The failing gate was `verify-project-coverage`, and its message
  is the same sentence the conductor had written into the other node's brief an hour earlier:
  > `tools/verify-aide-gitignore.py is a gate that no workflow invokes — it runs only when
  > somebody remembers, which is not a control`
  **The control for the omission existed, fired correctly, and nobody looked.** Found not by the
  conductor but by a node that could not get a self-consistent branch from that base. *Knowing
  the class, and having registered it that same day, did not produce the read-back —* which is
  the argument for control (b) over control (a): remove the human step rather than add one.
- **Third instance, and it is in the INSTRUMENT (2026-09-11).** Control (a) said to read CI back
  *"from the run's own conclusion rather than from a wrapper's exit code"* — which is right and
  **not sufficient**. A node measured that **`gh run watch --exit-status` exited `0` after
  printing `failed to get run: HTTP 403: API rate limit exceeded`**. It never observed the
  conclusion at all, and reported success. **The conductor had been reading that signal all
  session and telling nodes to read it too**, treating agreement between it and
  `--json conclusion` as confirmation — when one of the two can go green *for the wrong
  reason*. **Only `gh run view --json conclusion` actually answers the question**; the watch
  command answers *"did I finish waiting"*, and a failure to observe finishes waiting.
  Control (a) is amended: **the conclusion field is the signal; a watch exit code is at most a
  liveness hint.** That this class's own instrument carried the class is the sharpest
  instance of it so far.
- **Fourth instance, and it widens the class from PLATFORM to ENVIRONMENT (2026-09-11).** A node
  reported *"full gate set bare: every gate exit 0"* and CI then went red on the same branch.
  **Neither statement was false.** The local run used **the working tree it had**, including
  untracked scratch; CI runs the same gate on a **clean checkout**. The node's own account is
  the precise one: *"The local green and the CI red were not in conflict; my claim was simply
  narrower than it sounded. **Local gate results are not a CI prediction, and I stated one as
  though it were the other.**"*
  So the class is not only *which platform* — it is **which environment**, and a dirty working
  tree is a different environment from a checkout in exactly the way a Windows runner is a
  different environment from a Linux one. **Control (a) extends: a completion claim names the
  environment it covers, not only the platforms** — and "I ran the gates" and "the gates pass
  on this commit" are different claims that share a sentence.
- **Relationship to DC-117:** DC-117 is the same family one axis over — there the invisible variable
  was the **host** (console vs console-less), here it is the **operating system**. Both are *the
  environment a test ran in is not recorded in the claim that it passed.*
- **Status:** `uncontrolled` — the instance is being repaired and the class is stated, but nothing
  fails when the shape recurs, and the next Windows-only "all green" will read exactly as
  convincing as this one did


### DC-120 — A node runs a repo-wide destructive command while sibling nodes are live, and a node is unprotected until its first commit

- **Shape:** a fan-out gives each node its own worktree, which is the containment boundary. One node
  then invokes a tool that **adjudicates the whole repository** rather than its own tree — a
  cleanup, a prune, a reset — because that is the tool's default scope and the node's intent was
  narrower than the command it typed. The blast radius is every sibling's worktree, and the sibling
  most exposed is the one that has **not yet committed**: a fresh worktree is *clean, merged and
  unheld*, which is exactly the predicate a fail-safe cleanup uses to decide a tree is removable.
  **The safest-looking tree is the one that gets deleted.**
- **Signature:** a node reporting *"my worktree disappeared between two tool calls"*; a cleanup
  summary whose count exceeds what it actually removed; a directory left on disk that
  `git worktree list` no longer mentions.
- **Instance (front-door slice, 2026-09-10):** an installing node ran `coord worktree cleanup
  --remove` intending to remove one proof tree. The command is repo-wide. It adjudicated all
  worktrees while **three sibling nodes were live**, removed two, and reported *"removed 4 of 4"*.
  One live node's worktree **was** deleted between two of its tool calls — it recreated the tree on
  the surviving branch and lost nothing, but only because its branch existed. Verified afterwards:
  the two trees actually removed had branch tips already merged to `main`, and every tree with
  uncommitted work was correctly KEPT. **No work was lost — the fail-safe held and the count lied.**
- **Two distinct failures, and the second is the quiet one:**
  1. **Scope** — the enforced scope was wider than the intent that reached it. The node itself
     identified this as a **GO14a** instance *in the tool that implements the discipline GO14a
     governs*.
  2. **Reporting** — the count was derived from **intent** (`attempted − failed`) rather than from
     reading the inventory back. An irreversible action reporting its intent as its result is worse
     than under-reporting: the recovery nobody performs is the one nobody knows is needed.
- **A third mechanism found while checking for damage:** `git` **de-registers a worktree before it
  deletes the directory**. A failed delete therefore leaves a directory on disk that git will never
  mention again — no `worktree` command can find it, and no cleanup can ever reach it. **This
  repository has one**: `C:/projects/ai-de-spike-codemirror`, 7.0 MB, no `.git` file, a snapshot
  taken ~17 minutes before the lane rename landed. Assessed file-by-file: its only two "unique"
  files are the **pre-rename** `GovernedSessionSource.cs`/`Tests`, superseded by
  `GovernedLaneSource`. **Reported, not removed** — WT1 makes deletion opt-in, and with no `.git`
  the *"carries no commit that exists nowhere else"* test cannot be run at all.
- **Control:** two halves, both cheap. **(a) A fan-out contract clause the conductor writes into
  every brief:** a node may run destructive commands **scoped to its own tree only**; anything
  repo-wide is the coordinator's, because a node cannot see its siblings. This is GO7's *failure
  containment* made concrete rather than assumed. **(b) Commit early** — a node's first commit is
  what makes its work survivable, and a brief should say so rather than leaving the node to discover
  it. Upstream (`ai-forward` rev 69) has since added `--path` scoping, a measured count read back
  from the inventory, and a distinct `ORPHANED` outcome; ai-de picks those up with that pack update.
- **Status:** `uncontrolled` — the analysis is complete and upstream has fixed the tool, but in this
  repository nothing yet stops a node typing a repo-wide destructive command, and the briefs that
  would carry clause (a) are written fresh each time


### DC-121 — A brief's exclusion is scoped by PATH when its purpose was a KIND, and silently suppresses an unrelated obligation

- **Shape:** a coordinator writes a negative instruction into a node's brief to prevent one specific
  failure — *"do not commit `docs/audit/*`"*, meant to stop derived-view merge conflicts. The
  exclusion is expressed as a **path glob**, but the thing it was protecting against is a **kind of
  artifact**. Another artifact of a *different* kind lives under the same path, carrying an
  obligation the coordinator never intended to waive. The node obeys the brief exactly — correctly,
  since the brief is its authority — and the obligation silently does not happen. **Nothing fails,
  because the omission is compliance.**
- **Signature:** a node reporting *"I did not do X because the brief said not to"* where X is a
  standing repo obligation nobody meant to suspend; an exclusion glob whose directory contains more
  than one class of artifact; a coordinator surprised by a gap in output that its own instruction
  produced.
- **Instance (front-door F2, 2026-09-10):** the brief's *"do not commit regenerated derived output"*
  list named `docs/audit/*` alongside `docs/api/`, `docs/_site/` and `docs/docs-index.js`. The
  intent was `audit-data.js` and `index.html` — **derived views**. But `docs/audit/audit-log.jsonl`
  is an **append-only register**, a different class in the repo's own `.agents/artifacts.yml`, and
  it carries the **standing Audit Mandate**. F2 closed with no audit entry and **said so explicitly**
  — *"your brief lists `docs/audit/*` among the output not to commit; the standing constraint
  expects an entry at close. I followed the brief. That tension is yours to resolve."* It was right
  on every count.
- **Why it survives:** the exclusion is **correct for what it was aimed at** and there is no signal
  at all for what it also hit. A missing audit entry produces no error, no red gate, and no diff —
  it produces **nothing**, which is indistinguishable from a turn that had nothing to record.
- **Relationship to DC-118:** the same mechanism, pointed at an **instruction** instead of a clause
  or a guard: **the instruction's width exceeded its purpose's width.** The repo already has the
  vocabulary to prevent it — `.agents/artifacts.yml` classifies every path as
  `authored`/`derived`/`register`/`generated`, and `derived` is exactly what the brief meant.
- **Control:** **exclude by class, not by glob.** A brief that means "do not commit derived output"
  says *"do not commit anything the registry classifies `derived`"* and, where it must name paths,
  names them from the registry rather than from memory. Where a path glob is genuinely required, the
  brief states **what it is protecting against**, so a node can tell an intended exclusion from an
  accidental one — and a node that finds an obligation inside an exclusion should raise it, as this
  one did.
- **Status:** `uncontrolled` — briefs are written fresh each time and nothing checks their exclusion
  lists against the artifact registry. The one thing working here is the standing instruction to
  report anything in a brief that looks wrong, which is what surfaced it


### DC-122 — A comment declares a security property the code does not have, and is then cited as evidence of that property

- **Shape:** a defensive check carries a comment stating what it protects against — *"a junction or
  symlink that escapes the root must not be extracted"* — naming a threat and often a control id.
  The comment is written in good faith by someone who believed it. **Nothing ever measures it.**
  Thereafter the comment *is* the evidence: reviewers cite it, briefs repeat it, and the register
  records the line as a control. The code may do something adjacent and useful while **not doing the
  thing the sentence claims**, and no test catches that, because the test was written against the
  same belief.
- **Signature:** a security claim in a comment with **no test naming the same threat**; a control id
  (`P1-FS`, a CWE, an ADR) cited in prose but nowhere in an assertion; and — the tell that separates
  this from ordinary staleness — **the comment was never true**, so there is no commit where it
  regressed and no bisect will find one.
- **Instance (front-door slice, 2026-09-10):** `FixtureExtractor.cs:98` carried *"a junction or
  symlink that escapes the fixture root must not be extracted (P1-FS)."* **Measured on .NET
  10.0.11:** `Directory.EnumerateFiles` **traverses** a junction, and `Path.GetFullPath` does **not**
  resolve one — so the check **returned `True` for a file whose bytes live outside the root.** The
  line does lexical containment on an unresolved path, which is a real check; it is simply not the
  one the sentence describes. A second defect surfaced in the same measurement: a `RootPath` ending
  in a separator produces a **double** separator, so **every** file reports as escaping.
- **How far the false claim travelled before anyone ran it:** a Privacy review cited the comment
  while ruling on an unrelated matter; the conductor repeated it to a node as *"a declared security
  boundary"* and called it the finding it cared most about; and **the node measured it and found the
  declaration false.** Three readers, each reasonably trusting the one before, and **the first person
  to execute it was the fourth.**
- **Why it survives, and why it is worse than a stale comment:** a stale comment was once true and
  its drift is discoverable by history. This one **never was**, so the only way to find it is to run
  the thing it describes. And because it **names a threat**, it *actively suppresses* the question —
  a reader who wonders whether junctions are handled finds a sentence saying yes.
- **Relationship to DC-116 and DC-118.** DC-116 says never assert the shape of our own code from
  memory. This is one hop further out: the claim was **not** from memory — it was **read from the
  repository**, which is precisely what DC-116 prescribes as the remedy. **A comment is not evidence
  about behaviour; it is evidence about what someone believed.** DC-118's mechanism appears too: the
  sentence's scope ("junctions and symlinks") is wider than the code's ("lexical prefix"), and
  nothing checked the width.
- **Control:** a security claim in a comment is a **test obligation**, not documentation. Where a
  comment names a threat or cites a control id, **either a test names the same threat, or the
  comment is rewritten to say what the line actually does.** The cheap narrow form, available today:
  when a reviewer cites a comment as evidence of a security property, that citation is **Inferred**,
  never Verified, until someone executes it — this register's own confidence vocabulary already
  carries the distinction and it was not applied. *The generalisation:* **in a codebase whose
  evidence standard is "open the file", comments are the one artifact where opening the file is not
  enough.**
- **Status:** `uncontrolled` — the instance is corrected (the comment now states what the line does)
  and the two measured defects are recorded as their own next step, but nothing fails when a comment
  claims a property no test asserts


### DC-123 — Containment exists one ring in and is absent one ring out, and the leak is unobservable from inside the harness that leaks it

- **Shape:** a system spawns processes in nested rings — a harness spawns a helper, the helper spawns
  shells. Someone thinks carefully about lifetime **at the inner ring** and builds a real control
  there: a job object, a kill-on-close handle, a documented reason. **The outer ring — the process
  that owns the contained ones — gets nothing**, because the reasoning happened while looking at the
  inner problem. The result is inverted containment: the grandchildren are reaped reliably and the
  child that owns them is abandoned, taking its reaped-capable subtree with it.
- **Signature:** a codebase that contains an exemplary lifetime comment **and a leak of the same
  resource class**; disposal code that releases a *handle* or a *managed wrapper* and is mistaken for
  ending a *process*; and the tell — **the product uses the correct idiom and the test harness does
  not**, so the leak is invisible to anyone reading production code.
- **Instance (2026-09-10, found by the operator from a taskbar screenshot, not by any gate):**
  `tests/AiDe.Core.Tests/TerminalHostLauncher.cs:61-77` creates a helper with `CREATE_NEW_CONSOLE`
  and on **every** path does:
  ```csharp
  finally { CloseHandle(info.hThread); CloseHandle(info.hProcess); }
  ```
  `using var process` disposes the **managed wrapper**; `CloseHandle` closes a **handle**. **Neither
  ends the child.** Zero occurrences of `Kill`, `Terminate` or `Job` in the file. Five test call
  sites, one abandoned helper each.
  **Meanwhile `src/AiDe.Core/Terminal/ConPtyInterop.cs:292-320` defines `CreateKillOnCloseJob()`,
  whose own doc comment states the requirement exactly** — *"what makes orphan reaping survive AI-DE
  itself being killed… a shutdown-path `TerminateProcess` would not run at all in that case."* The
  **product** uses it; the **test launcher** does not, though `InternalsVisibleTo` was already in
  place and a sibling test already called into that class.
- **The inversion, stated plainly because it is the whole class:** the shells the helper spawns
  **are** correctly reaped — the ConPTY session puts them in a kill-on-close job. **The helper that
  owns them is not.** The inner ring has a job object; the outer ring has nothing. Confirmed by exact
  command line rather than inferred: DC-117's `powershell.exe -NoLogo -NoExit -EncodedCommand` is
  emitted at `ShellIntegration.cs:129,:186`, and the helper's probe routes through that builder — so
  **DC-117's shells are this helper's children.** Same family, one ring apart.
- **Why nothing catches it, which is the more useful half — TWO independent blind spots:**
  1. `tools/verify-test-run.py:55-62` **deliberately excludes** the helper project, with a correct
     rationale (*"a helper produces no `.trx`"*). **The exclusion is right; the consequence is that
     the one gate watching this project's execution is contractually blind to it.**
  2. `TerminalHostingLedger` counts `terminal.start` activities **in-process, during a conductor
     run**, to prove `terminalHostConstructions: 0`. It counts the product *not hosting terminals*.
     **It cannot see an OS process the test harness spawned.** The repository has a counter named
     almost exactly for this leak, **pointed 180° away from it.**
  **So the leak is structurally unobservable from inside the harness that leaks it, and any oracle
  must live outside the test host.**
- **A hypothesis this refuted, recorded because the refutation was the useful part:** the conductor
  proposed that these processes held file handles and thereby explained an orphaned worktree
  directory and a cleanup that miscounted. **Refuted on live and forensic evidence** — no process
  held any module under the repository root; an exclusive open of a file in the orphaned tree
  succeeded; and decisively, **that tree has no `bin` or `obj` anywhere, so no helper binary ever
  existed in it to hold anything.** 637 files survived there — a removal that aborted at the start or
  never ran, not one that got 99% done and hit a sharing violation. **Those symptoms are DC-120 and
  need no help from this class.**
- **Control:** containment belongs to **the launcher that created the process**, never to a global
  reaper — a repo-wide "kill all X" sweep is **DC-120 in a new costume**, with blast radius across
  every sibling worktree. The oracle must be **outside the test host**, and the cheapest form needs
  no process table at all: each test deletes its report file in `finally`, so **a leftover report in
  `%TEMP%` is a per-test fingerprint of an aborted run.** The generalisation: **when reasoning about
  a resource's lifetime, name every ring that owns one — the ring you are looking at is the one you
  will protect.**
- **Fixed 2026-09-10, and the abnormal path is the one that was observed.** The launcher now creates
  a kill-on-close job before `CreateProcessW`, assigns the child, and closes the job handle in the
  existing `finally`. **Red-first on the path a `finally` cannot cover:** the **test host** was
  killed (`testhost.exe` PID 36704, not the helper), and helper PID 47884 was **gone 47 ms later**.
  That is precisely the state the operator photographed — a suite that did not exit cleanly.
- **Three assumptions in the fix brief were false, and building found all three** — worth recording
  because two of them look like the kind of tidy-up that gets waved through:
  1. *"`return -1` after `Assert.Fail` is dead code"* — deleting it produced **`CS0161`**. xUnit
     2.9.3's `Assert.Fail` is **not annotated `[DoesNotReturn]`**, so the compiler cannot see it as
     terminal. **Unreachable at runtime, required at compile time.** A HYG-A cleanup that would have
     broken the build.
  2. *"a three-line change"* — `ConPtyInterop`'s members carry `[SupportedOSPlatform("windows")]`, so
     calling them tripped **CA1416** under `TreatWarningsAsErrors`; three types needed the
     annotation.
  3. **The oracle's pattern had a hole.** It listed four report prefixes and **omitted
     `conpty-host`**, the conformance suite's — leaving the leftover-report oracle **blind to the
     fifth call site**. *An oracle with a hole in it is worse than no oracle*, because it reports
     clean.
- **RECURRED ONE RING FURTHER OUT, in PRODUCT code, and the fix above is what made it findable
  (2026-09-10, node TH2).** `src/AiDe.Core/AgentPlane/AcpEngineProcess.cs` spawns the ACP engine
  with **no job object at all**; reaping is `Dispose`-only, and `GovernedRunHost.cs:65` reaches it
  through `using var engine = AcpEngineProcess.Start(...)`. `using` runs `Dispose`; **a killed
  process runs nothing** — so the abnormal host death, the exact scenario the launcher fix was
  proven against, abandoned the whole engine tree. The tell is this class's own signature verbatim:
  the file carries an **exemplary lifetime comment** eight lines from the leak — *"a kill request
  that is not waited on turns 'the child is dead' into a claim rather than an observation"* —
  reasoning carefully about the graceful path while the abnormal one stayed open.
- **Measured, not argued, and the FIRST oracle was wrong.** An out-of-host driver
  (`AcpProbe --host-engine`) owns a real engine, prints its own pid, the engine's and the engine's
  child's, and waits to be killed. Its first version proved the wrong thing: the child blocked on
  `Console.In.ReadToEnd`, so when the host died the pipe broke and the tree exited **on its own** —
  the run reported `THE ENGINE TREE DIED WITH ITS HOST` while nothing whatsoever contained it.
  **Stdin EOF is a MITIGATION, not containment**, the same category as stripping `-NoExit` from a
  shell, and an oracle that cannot tell the two apart reports clean on the defect. With
  `--ignore-stdin` the measurement is: host killed → **engine and grandchild both still running
  5,000 ms later** (before); **engine gone after 7 ms, grandchild after 8 ms** (after). The
  grandchild is the part that matters — job membership is inherited, so the job reaps a tree the
  code never names.
- **The generalisation this adds:** *name every ring, then check that the ring you just fixed is not
  itself owned by something* — and **prove the oracle can fail before trusting what it says**, or a
  mitigation elsewhere in the system will answer the question you thought you were asking.
- **Status:** `partially-controlled` — the launcher reaps its own child, the ACP engine is now in a
  kill-on-close job, and both were proven on the killed-host path. An out-of-host oracle now EXISTS
  (`AcpProbe --host-engine`) where before it was "named and unbuilt". Still not `controlled`: that
  oracle is **run by hand, not by CI**, so nothing fails when a third ring is added without one; the
  leftover-report oracle remains a fingerprint of an aborted run rather than a process-table
  assertion; and the **rate remains unmeasured** — the population was **zero** at diagnosis time, so
  this leak is **episodic rather than monotonic**, and every statement about its frequency,
  including the conductor's, is modelled rather than observed


### DC-125 — A call that ESTABLISHES a safety property reports failure by return value, and the return value is discarded

- **Shape:** a safety property — containment, a lock, a permission drop, a limit — is established by
  one call that reports success as a **return value** rather than by throwing. The call site treats
  it as a statement. From that moment "the property holds" and "the property silently does not hold"
  are **the same observable state**: the object exists, the code that set it ran, every test passes,
  and nothing anywhere is different except that the protection is absent.
- **Signature:** a `bool`-returning interop or API call in **statement position** at a boundary
  whose whole purpose is the property it sets; a nearby comment describing the property as though it
  were established; and the decisive tell — **the correct check already exists elsewhere in the same
  repository** and was not reached for.
- **Instance (2026-09-10, node TH2, found by an SRE investigation reading both shipped call sites):**
  `ConPtyTerminalSession.StartAsync` and `TerminalHostLauncher.RunInNewConsoleAsync` both called
  `AssignProcessToJobObject(job, handle);` and discarded the `bool`. The repository already carried
  the check **three times** and used it at **neither** shipped C# site:
  `spikes/extraction-containment/Sandbox.cs:86-87` (`if (!AssignProcessToJobObject(...)) throw new
  Win32Exception(...)`), and `docs/ai-forward-pack/scripts/bounded_process.py:125-127` and
  `:224-226`.
- **Red-first, and the red was that nothing happened.** Reproduced against a job whose object had
  been closed: the call returned `false`, set `ERROR_INVALID_HANDLE` (6), **raised nothing**, left
  the child running outside the job — and the test asserting all of that **PASSED**. A defect whose
  red state is a passing test is the reason this class is invisible: there is no failing signal to
  drive a fix, which is also why the investigation that found it explicitly refused to bundle a
  refactor *"with no failing signal driving it"*.
- **Control — STRUCTURAL, not a lint.** The `AssignProcessToJobObject` import is now `private`, and
  `ConPtyInterop.AssignProcessToJob` — which checks and throws, in `Sandbox.cs`'s shape rather than
  a fourth spelling — is the only route to it. `InternalsVisibleTo` does not reach private members,
  so the test assembly cannot bypass it either. **A future call site cannot discard the answer,
  because the answer is no longer offered.** `JobContainmentTests.AnAssignThatCannotHappenIsNotSilent`
  observes the one remaining route firing, and asserts the native code (6) so a throw for some other
  reason cannot pass for this one.
- **Swept.** Every `bool`-returning P/Invoke in `src/`, `tests/` and `spikes/`, and every call to one
  in statement position. After the fix the discarded returns that remain in `src/` are `CloseHandle`,
  `TerminateProcess` on an already-failing cleanup path, and `InitializeProcThreadAttributeList`'s
  documented first call — whose failure **is** its success. None of them establishes a property whose
  absence is unobservable, which is what makes them a different case rather than an exception to this
  one.
- **Status:** `partially-controlled` — the one instance is closed structurally and observed firing,
  and the sweep found no sibling. Not `controlled`: nothing gates the **class**. A new
  `bool`-returning establish-a-property call can still be written in statement position, and the
  control that would catch it — an analyzer or a gate over interop call sites — is named here and
  not built


### DC-126 — A gate verifies that an instruction is DOCUMENTED, not that it is FOLLOWABLE, and the capability it names does not exist

- **Shape:** a recurring failure is diagnosed, and the remedy is written as an **instruction** into
  every place an agent reads. A gate is then built to keep that instruction present — it checks the
  required wording appears in each root and **goes green**. The instruction names a **channel, an
  environment variable, a path or a tool**, and *nothing ever checks that the named thing exists in
  the sessions being asked to use it.* So the mandate is present, the gate is green, and the
  behaviour is **impossible**. The original symptom continues, and the artifact built to fix it is
  now evidence that it was fixed.
- **Signature:** a gate whose `REQUIRED_MARKERS` are **strings to find in documents**; an
  instruction naming an env var, with no check that it is set; and the tell — **the metric the
  instruction was written to move has not moved**, while everything about the instruction reports
  healthy.
- **Instance (2026-09-11, found because a node refused to fake it):** `AGENTS.md` carries the Proof
  Pack capture mandate, and states its own reason in measured terms — *"111 episodes, 1 observation…
  an engine that is correct, verified end to end, and producing nothing, because almost nothing ever
  recorded its evidence."* The channel is `$AIDE_CONTRACT_LOG`. **It is UNSET in every session** —
  the conductor's and every agent's — so **no node in the front-door slice could write an
  `episode-close` line.** `tools/verify-capture-instruction.py` is **green**, because
  `REQUIRED_MARKERS = ("episode.artifacts", "AIDE_CONTRACT_LOG")` checks that those **strings appear
  in each harness root**. It verifies the mandate is *documented*. Node F4 hit it and **named the
  gap rather than inventing a path**, which is the only reason it surfaced.
- **Why it survives, and why the gate makes it worse:** the gate is not wrong about what it checks —
  the instruction *is* present in three harness roots, which is a real and useful property. It is
  wrong about **what a reader takes its green to mean.** And because the gate exists, the question
  *"is capture working?"* has an authoritative-looking answer, so nobody asks the different question
  *"has anything ever been captured?"* — which the repository's own `111 episodes, 1 observation`
  already answered.
- **The distinguishing test, and it is cheap:** for any instruction-presence gate, ask **"if an
  agent obeyed this instruction perfectly, would it succeed?"** Here the answer is no: the write
  target is undefined. A gate that cannot ask that question is measuring documentation.
- **Relationship to the session's recurring shape:** this is *a control that is off, where off is
  indistinguishable from on-and-quiet* — applied to **the control built to fix a measured absence**.
  It is also DC-118's mechanism at the outermost hop: the gate's claim (*capture is in place*) is
  **wider** than what it checks (*the words are present*), and nothing checked the width.
- **Control:** an instruction-presence gate must **either** verify the capability it names is
  reachable — the env var is set, the path is writable, the tool answers — **or** say in its own
  output that it checks **presence only**, so its green cannot be read as capability. The stronger
  form here is a **liveness check on the corpus**: the mandate exists to make observations
  accumulate, so **the number of recorded observations is the thing to gate**, not the number of
  documents containing the word. *A mandate whose compliance is unmeasurable has no compliance.*
- **Status:** `uncontrolled` — the gap is measured and recorded, the gate is unchanged, and the
  channel is still unset, so the next slice will produce the same zero observations with the same
  green gate


### DC-127 — A fixture written by the same mind as the reader shares its blind spot, so a reader tested only against its fixture is tested against its own assumption

- **Shape:** someone writes a reader, parser or checker over a real artifact, and writes a **fixture**
  to test it. The fixture is constructed from the same mental model as the reader — the same
  assumption about which column carries the substance, which field is authoritative, which shape the
  document takes. **So the fixture agrees with the reader by construction**, and the test passes for
  the same reason the reader is wrong. The defect is invisible until the reader meets the **real**
  artifact, which is usually in production or at a join.
- **Signature:** a reader with a green fixture-based test and no test against a committed real
  artifact; a fixture authored in the same change as the reader; and the tell — **the fixture's shape
  is simpler than the real artifact's**, because a fixture is written to exercise the code rather
  than to reproduce the document.
- **Instance (front-door F5, 2026-09-11, found by the node itself):** the exit-evidence oracle's
  **clause-7 reader had the exact hole clause 7 exists to close.** Clause 7 fails if a Proof Pack
  Residual cell reads *"none"*. The carried-residuals tables are `| Residual | Kind | Detail |`, so
  the `Residual` column holds only the **name** — and a row reading
  `| the thing we did not do | named | none |` **would have passed**, because the reader checked the
  first column and the substance lives in `Detail`. The node found it by **testing non-vacuity
  against the real Proof Pack rather than against its fixture**, and reported the cause in one line:
  > **"the fixture agreed with me."**
- **Why it survives:** every conventional signal is green. The test exists, it is red-first capable,
  it was written before the code, and it fails when the code is broken **in the way the author
  imagined**. Nothing in the usual checklist asks *"does this fixture reproduce the artifact, or the
  author's idea of it?"*
- **Relationship to the rest of this register:** it is **DC-122's mechanism relocated** — there, a
  comment carried the author's belief and was mistaken for evidence about behaviour; here, a fixture
  carries the author's belief and is mistaken for evidence about the artifact. And it is the
  session's recurring shape — *a control whose green is indistinguishable from its absence* — with
  the fixture supplying the green.
- **Control:** **a reader over a committed artifact is tested against that artifact, not only against
  a fixture.** Where the real artifact is unavailable or unstable, the fixture must be **derived from
  it mechanically** rather than authored, or the test must state that it exercises the code and not
  the contract. The cheap form, and the one that worked here: **run the reader against the real
  document and check it is non-vacuous** — that it finds something, and that it would find the thing
  it exists to find.
- **Status:** `partially-controlled` — the instance is closed and the oracle's self-test now covers
  the real table shape specifically, but nothing gates the class: the next reader written beside its
  own fixture will pass the same way

### DC-128 — Citing a commit proves a file existed, not that it was unchanged, so "committed before" is attested rather than mechanical

- **Shape:** a process requires that an artifact — an oracle, a spec, a baseline — be **fixed before
  a dependent act**, and the control is *"commit it first and cite the sha."* That proves the file
  **existed** at that commit. It does **not** prove the file **running now** is the file at that sha.
  An artifact committed early, then quietly widened after the dependent act and re-cited, **reads
  identically to one that was right the first time.** The ordering becomes a claim the author makes
  about themselves.
- **Signature:** an ordering requirement discharged by a cited sha with no byte comparison; a
  process document saying *"committed before X and its sha recorded"* without saying *"and unchanged
  since"*; and the structural tell — **nothing in the check reads the artifact twice.**
- **Instance (front-door F5, 2026-09-11):** §F5's clause reads *"the oracle is committed BEFORE the
  run and its SHA cited in the Proof Pack — seven points written after seeing the run are a
  description, not a test."* The conductor authored that clause and dispatched it in four briefs
  without noticing that **citing a sha does not prevent editing the file afterwards and citing the
  new one.** The node implementing it added **clause 0**, which reads its own commit, compares it to
  the run's start, **and compares its own bytes at that commit against the bytes now running**:
  > *"An oracle committed early and then quietly widened after the run reads identically to one that
  > was right first time — unless the bytes are compared."*
  That makes the ordering **mechanically true rather than attested** — and has the deliberate
  consequence that the oracle file is now **frozen**: any edit invalidates the cited sha and forces a
  re-commit and re-cite.
- **A second-order consequence worth recording, because it bit immediately:** the freeze **constrains
  how the branch may be integrated.** A **rebase** rewrites the commit and orphans the cited sha,
  breaking clause 0; a **merge** preserves it. *A control strong enough to constrain its own
  integration path is working, but the constraint has to be noticed before the integration, not
  after.*
- **Why it survives:** the weak form looks rigorous. A sha is precise, verifiable and auditable, and
  it answers a question — *did this exist then?* — that is adjacent to the one being asked. **The gap
  between "existed" and "unchanged" is exactly one function call wide and reads as pedantry until
  someone widens an artifact after the fact.**
- **Control:** any ordering requirement over an artifact compares **bytes at the cited commit against
  bytes now**, not the sha alone. Where that is impossible, the requirement is recorded as
  **attested** rather than verified, and the attestation names who attested. And the integration
  consequence is stated with the control: **a frozen artifact's branch merges, never rebases.**
- **Status:** `partially-controlled` — implemented for the exit-evidence oracle and proven by its own
  self-test; the plan clause it corrects is now accurate, but **no other ordering requirement in the
  repository carries a byte comparison**, and the plan-authoring habit that produced the weak form is
  unchanged


### DC-129 — A launch that detaches returns success immediately, so the caller measures a run that has not happened

- **Shape:** a caller invokes an executable and waits on **the call** rather than on **the process**. If
  the target is a GUI-subsystem binary — on Windows, a `WinExe` — the shell **detaches it and returns
  at once**. The caller sees **no error, no exit code, and a near-zero duration**, and concludes the
  work completed instantly. Meanwhile the real work is **running in the background**, doing everything
  it was asked to: provisioning, spending, writing, scoring. The failure is not that the launch
  failed — **it succeeded emptily**, and every field the caller reads is consistent with success.
- **Signature:** a measured duration implausibly close to zero; a missing rather than zero exit code;
  a "completed" run with no output artifact; and the tell — **a second invocation produces a second
  set of side effects while the first is still live**, because nothing told the caller the first one
  had not finished.
- **Instance (front-door pre-flight, 2026-09-11):** `& $exe --conduct …` on a `WinExe` returned
  immediately, reporting no exit code and 0 seconds, **while a real governed run continued detached**
  — provisioning a lane worktree and spending a live subscription turn. A second invocation under
  `Start-Process -Wait` ran and was measured. **Both completed. Both scored `Partial: 15 / 15
  observed`.** The store ended with **two episodes in one cohort** and the subscription cost was
  doubled — *by a harness error, not by design*.
- **What actually detected it, and this is the transferable part:** not the exit code, not the
  duration, not the absence of output. **`git worktree list` came back 22 when 21 was expected.** An
  **incidental invariant**, maintained for another reason entirely, caught what the intended signal
  could not — because the intended signal had not failed. In the node's own words: ***"The count was
  the detector, not the exit code."***
- **Why it survives:** every instinct for checking a subprocess is pointed at the wrong object. A
  non-zero exit code, a thrown exception, a timeout, stderr — none of them fire, because the launch
  genuinely succeeded. **The only honest signal is the process handle, and the idiom that returns it
  is not the idiom most shells reach for first.**
- **Relationship to the register:** it is the session's recurring shape — *a control whose green is
  indistinguishable from its absence* — at the **process-launch boundary**, and it is the same family
  as `gh run watch --exit-status` exiting `0` after failing to observe a run (DC-119). Both report
  success for **not having looked**.
- **Control:** **wait on the process object, never on the call**, for any invocation that may target a
  GUI-subsystem binary — `Start-Process -Wait -PassThru` or an equivalent that yields a handle, then
  read the handle's exit code. Where a run has a **countable side effect** — a worktree, an episode
  row, a lock — **assert the count**, because a count discriminates where an exit code does not.
  *A measured duration near zero for work that cannot be near zero is a detection, not a result.*
- **Status:** `uncontrolled` — the instance is understood and the constraint is written into the
  exit-run harness's brief, but nothing in the repository fails when a caller waits on the call
  instead of the process, and the next harness author inherits only prose


### DC-130 — Adjacent nodes each build one end of a seam that no clause assigned, and every node passes

- **Shape:** a plan decomposes work into nodes and gives each one a clause list. Two adjacent nodes
  each build **one end** of a connection between them — node A produces a value, node B consumes a
  value of that shape — and **no clause claims the edge itself**. Both nodes are correct against
  their own clauses. Both ship green. The plan review passes, because the review reads a **list of
  nodes**, and the thing missing is not a node. **The gap is only discovered by whoever first needs
  the two ends to meet**, which is typically the evidence node, at the close, when the cost of
  finding it is highest.
- **Signature:** a constructed value returned to a **discarding caller**; a consumer wired to a
  source that has no producer; a plan whose clauses all read *"X exists"* and none read *"X reaches
  Y"*; and the tell that makes it unmistakable — **one of the nodes writes the conflict down in a
  code comment and no clause resolves it.**
- **Instance (front-door slice, 2026-09-11):** F4 built `ComposerSurface.Send()`, which constructs a
  real `GovernedRunRequest` through the real send gate **and returns it to `(_, _) => Send();`** — a
  discarding caller. F2 built `SessionLane`, taking a `ChannelReader<ObservedRunEvent>`, **and its own
  remark says it reads *"the same channel `AcpPeer` publishes into and `GovernedRunHost` drains."***
  `GovernedRunHost.RunAsync` drains that channel itself and `AcpEventQueue` is **`SingleReader = true`**,
  so a second drain is impossible by construction. `RunAsync` had **exactly one caller in `src/`**:
  `ConductorEntry.cs:80`.
  **So nothing in the product launched a run from the UI at all.** The slice could build a session,
  open a composer, validate a goal block and construct a real request — and then drop it. Every node
  had discharged every clause it was given.
- **How it was found, and how late:** by the **exit-evidence node**, while writing an oracle for a
  clause that read *"composed in the composer, streamed in Console mode."* It was unsatisfiable, and
  the discovery was worth two clauses at first reading — the conductor sized the fix as unblocking
  **two of nine**. The node corrected that: **five of nine**, because the scored cell, the recorded
  measurement and the DC-115 verdict are all **read off the exit run's result**, and there is no exit
  run until the product can launch one. *The gap's true size was invisible from the plan and visible
  from the oracle.*
- **Why plan review does not catch it:** the review enumerates **nodes and their clauses** and asks
  whether each is owned, testable and floored. **An edge is not a node.** A surface list of the form
  *store → model → service → projection → client → UI* names the **stations**; nothing asks whether
  the **track between two stations** has an owner. And because both nodes pass, no red appears
  anywhere until something tries to traverse the edge.
- **Control (from Ruling 46):** the slice's **E7 surface list assigns every EDGE to a node, not only
  every surface**, and **plan review fails when an edge is unowned.** The cheap form: for each pair of
  adjacent nodes, write the sentence *"A's output reaches B by ___, owned by ___"* — and if the blank
  cannot be filled with a node id, the plan is incomplete, whether or not every node is. *A clause
  saying "the request is constructed" and a clause saying "the lane renders events" do not, between
  them, say "the request starts a run."*
- **Relationship to DC-118:** the same family, one level out. DC-118 is about a **clause** whose
  width does not match the ruling it derives from; this is about **a clause that was never written
  at all**, for work nobody noticed was work. **Both are failures of the plan rather than of the
  nodes**, and in both the nodes' greenness is what conceals them.
- **Status:** `uncontrolled` — the instance is being repaired by a node ruled into existence for it,
  and the control is stated, but **no plan in this repository currently carries an edge-ownership
  list**, and the next slice decomposed the same way would produce the same gap


---

## Inherited from the fleet (ai-forward drm-0009, 2026-09-04)

Classes discovered in OTHER repositories, promoted to the shared fleet store and pushed here by `/apply-learnings`. They are kept separate from this repo's own findings because their evidence is elsewhere: treat each as a shape to watch for, not as something already observed here. The **Status counts** line above is deliberately unchanged -- an inherited class has not yet earned a status in this repo.

Source: `ai-forward` `learnings/fleet-classes.jsonl`. Re-run `/apply-learnings` to refresh.

**Not added, because this repo already carries them under different wording** -- merged into the existing class instead: `COORD-C` into `DC-088`, `COORD-D` into `DC-067`.

### DC-124 — A message's meaning depends on WHICH parser reads it, and every reader believes it is reading the same bytes

- **Shape:** a boundary accepts a structured message and more than one component reads it — the
  router that acts on it, the logger that records it, a proxy or a gate that inspects it. The format
  permits a document that **two conforming readers resolve differently**, so every component is
  correct about the bytes and they disagree about the message. Nothing is malformed, nothing throws,
  and no test fails, because each reader is tested alone.
- **Signature:** a duplicate member in JSON (`System.Text.Json` takes the **last**; several other
  readers take the first); a header sent twice; a length declared two ways; a value both in a body
  and in a wrapper. The tell is a validation rule written as *"read field X and check it"* rather
  than *"there is exactly one X"* — checking a value presumes a value, and presuming one is the
  defect.
- **Instance (2026-09-11, node F4, found by the fuzz corpus C20 required, on its first run):** the
  composer's page→host router accepted `{"v":1,"kind":"nope","kind":"editor.ready", …}` and routed it
  as `editor.ready`, because `JsonDocument.TryGetProperty` resolves a duplicate to the last
  occurrence. The allow-list was doing its job perfectly on the value it was handed. Anything else
  reading the same bytes for a record, a rule or a review could have seen `nope`.
- **Control:** refuse the document, not the value — `ComposerMessageRouter` rejects any body
  declaring a top-level member twice, before it reads a single field, and
  `TheVocabularyIsClosedTests.C20_EveryUnknownKindAndEveryMalformedBodyReachesDropAndCount` carries
  the row. **Observed failing on the un-fixed router** (it returned `Accepted`). *(automated
  control — the highest rung available here; "make it impossible" would need a parser that rejects
  duplicates natively, which `System.Text.Json` does not offer.)*
- **Sweep:** every other place this repository parses a structured message at a trust boundary and
  more than one component reads it. The IPC framing is length-prefixed and single-reader; the ACP
  stream is read once by the mapper. **Reported, not closed:** a future second reader of the composer
  bridge — a recorder, a policy gate — makes this class live again, and the refusal above is what
  keeps that safe rather than the fact that there is one reader today.
- **Status:** `partially-controlled` — the refusal is automated and was observed failing, but the
  sweep's honest finding is that this repository has one reader per boundary TODAY, so the class
  is prevented at the composer bridge and merely absent elsewhere rather than controlled there.
- **Confidence:** Verified — the accepting behaviour was observed before the refusal was written.

### UNKNOWN-ARTIFACT-TYP - unknown-artifact-type-in-frontmatter
- **Control:** docs-graph.py validate rejects any frontmatter 'type' not in the TYPES enum; run it after adding a graph node. (automated control)
- **Boundary:** Applies to any new .md graph node; type must be one of the known TYPES.
- **Confidence:** v  - **Source:** fleet (drm-0009/p1)

### PACK-E-AN-AMBIGUOUS- - PACK-E · An ambiguous proper noun resolved inside my own frame
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p8)

### PACK-D-AN-ARRAY-PARA - PACK-D · An array parameter arrives as one comma-joined string when the script is invoked as an executable
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p9)

### PACK-C-AN-ASSERTION- - PACK-C · An assertion encodes a transient magnitude assumption
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p10)

### PACK-H-A-FIX-TO-A-HO - PACK-H · A fix to a hosted surface reported "done" from the working tree, not verified on the live surface
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p7)

### PACK-N-STALENESS-INF - PACK-N · Staleness inferred from a timestamp rather than from content truth
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p6)

### PACK-Q-AN-ADAPTER-WR - PACK-Q · An adapter written to a contract's *documented* shape, never to a *recorded* one
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p4)

### PACK-O-FRONT-MATTER- - PACK-O front-matter presence + scope-drift review
- **Control:** Presence (mechanical): every substantive turn records done_when (CT19); a missing one skipped the front matter. Satisfaction: review each done_when->summary pair where the summary exceeds the goal (scope drift, PACK-O). The audit done_when field + this miner ARE the rung-2 control (CI6). (automated control)
- **Boundary:** Presence is mechanical; 'summary exceeds goal' is surfaced for human review, not auto-judged. Trivial/conversational turns are exempt from logging (AL5b).
- **Confidence:** v  - **Source:** fleet (drm-0009/p13)

### PACK-P-A-CHECK-REPOR - PACK-P · A check reports its verdict over a corpus it never established was non-empty
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p5)

### SHELL-A-CONTENT-ROUT - SHELL-A · Content routed through a shell construct that performs substitution on it
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p3)

### TWO-OR-MORE-AGENT-SE - Two or more agent sessions work one repository, but session registration, file ownership, seam contracts, and derived/append-only merge policy are not recorded before work begins.
- **Control:** Add a multi-session collaboration check: when more than one active worktree/session exists, fail or warn if any session is unregistered, if changed files lack a current coord claim or owner mapping, if no shared session contract exists, or if derived/append-only conflicts are hand-merged rather than regenerated/re-issued. Observe it failing on an unregistered two-session fixture and passing once both sessions register, claim files, and publish the seam contract. (automated control)
- **Boundary:** Applies to concurrent cross-agent repository writes. It does not apply to a single writing session, read-only exploration, or a normal human code review where one actor owns the worktree. It coordinates humans/agents by evidence; it is not a distributed lock unless the edited resource accepts fencing tokens.
- **Confidence:** v  - **Source:** fleet (drm-0007/p12)

### A-CONSOLIDATION-LOOP - A consolidation loop whose promote step succeeds and whose DOWNSTREAM steps — distribution and control-building — are never run, so the same proposals resurface
- **Control:** The dream run reports the state of the PIPELINE, not just of the corpus: for each proposal, how many prior dreams raised it; for the previous dream, how many proposals were promoted; and for the fleet store, how many classes have never been distributed (no plan in learnings/plans/) and how many name a class in this repo's register that is still not `controlled`. A proposal recurring for the Nth time renders as an ESCALATION at the top of the review view rather than as a fresh idea in the middle of it. The number that matters is not 'proposals promoted' but 'promoted learnings that reached a repo AND produced a control'. (automated control)
- **Boundary:** Applies to any multi-stage produce-review-promote-distribute loop where each stage is a separate manual command. It does not apply to the human gate itself, which is correct and deliberate — the defect is the UNMEASURED skip of a downstream stage, not the existence of the gate.
- **Confidence:** v  - **Source:** fleet (drm-0009/p22)

### GIT-A-A-REVERT-USED- - GIT-A · A revert used as an undo, on a file that also carries unrelated uncommitted work
- **Control:** Derive a falsifiable control for this class and observe it failing on the un-fixed shape (CI6); move status -> controlled. (automated control)
- **Boundary:** Applies wherever the class's signature recurs; a control is not a control until observed failing.
- **Confidence:** i  - **Source:** fleet (drm-0009/p2)

### ASSUME-MARKER-HARVES - assume marker harvest
- **Control:** Review each assume: marker; a triggered one is a bug already written down (NG9). Verify or convert to a control. (knowledge doc)
- **Boundary:** Markers in this repo only; harvested at consolidation time.
- **Confidence:** v  - **Source:** fleet (drm-0009/p11)

### SIMPLIFY-MARKER-HARV - simplify marker harvest
- **Control:** Review each simplify: marker against its upgrade trigger; a triggered one is debt due (L6). (knowledge doc)
- **Boundary:** Markers in this repo only; harvested at consolidation time.
- **Confidence:** v  - **Source:** fleet (drm-0009/p12)

### TWO-REGISTERS-OF-ONE - Two registers of one quantity, created by a session that had not opened the first one
- **Control:** Coordination state splits into exactly TWO stores with different lifetimes and one authority rule: a TRACKED ownership register that is the sole authority on who owns what, and an UNTRACKED liveness store that says only who is running right now, in which tree, on what, and what they are blocked on — and that states no path or ownership table at all. A lint fails the liveness store when it contains an ownership/path table, and the liveness store's own header must point at the tracked register and declare that the tracked one wins on any disagreement. Add as a WT-series directive in session-worktree-discipline.md with the lint in coord doctor. (automated control)
- **Boundary:** Applies wherever more than one agent session writes to one repository. Does not apply to a single-session repo, where one register is correct and a second store is pure ceremony.
- **Confidence:** v  - **Source:** fleet (drm-0009/p14)

### A-SESSION-DESCRIBES- - A session describes another session's ownership, contract or seam without opening the file that states it
- **Control:** Extend E15 in end-to-end-integrity.md from code to AGREEMENTS: never assert the shape of our own contracts, ownership, or seam from memory — open the register or label the claim Inferred. The tell is one session summarising another session's ownership without a citation, and the cheap check is that every cross-session claim about who owns what carries the register line it came from. (always-loaded instruction)
- **Boundary:** Applies to claims about shared, written agreements. It does not apply to a session describing its OWN in-flight work, which has no register to cite yet.
- **Confidence:** v  - **Source:** fleet (drm-0009/p15)

### A-COLLABORATION-CHAN - A collaboration channel accumulates repeated same-shape requests against one seam, each handled individually
- **Control:** CI2 (class, not instance) applies to the collaboration channel, not only to defects. When a session opens the Nth request of the same shape against the same seam, it raises the CLASS — the missing capability behind all N — rather than the N+1th request. The channel's own review asks 'how many of these are one thing?' before it asks 'which is next?'. (knowledge doc)
- **Boundary:** Applies where one session repeatedly asks another for variations of one capability. It does not apply to genuinely distinct requests that merely arrive together.
- **Confidence:** i  - **Source:** fleet (drm-0009/p18)

### ONE-FIELD-CARRIES-TH - One field carries three different kinds of identity, so no query over it means one thing
- **Control:** Declare the identity vocabulary once and validate the field against it: an AGENT is a stable logical actor, a SESSION is one run in one worktree, and they are separate fields. Reject a raw UUID in the agent field, and reject a work-item placeholder that is constant across every record — a field whose value never varies carries no information and should be removed rather than filled in. (automated control)
- **Boundary:** Applies to any shared record keyed by actor. Not applicable where a single anonymous writer is intended by design.
- **Confidence:** v  - **Source:** fleet (drm-0009/p19)

### TWO-SESSIONS-COLLIDE - Two sessions collide on a lease and the collision is treated as a scheduling problem to wait out
- **Control:** A coordination refusal is a DEFECT SIGNAL about the decomposition: if two sessions need the same file at the same time, the work was split along the wrong seam and the plan is wrong, not the timing. The response is to re-cut the work item or record a block naming what is needed — never to retry on a timer, and never to widen the lease. Repeated refusals on one path escalate to a plan review. This is GO9 (a cap firing is a defect signal) pointed at coordination. (always-loaded instruction)
- **Boundary:** Applies to refusals arising from CONTENTION. A refusal caused by a missing identity or a malformed request is a different class (COORD-C) and is not evidence about the plan.
- **Confidence:** v  - **Source:** fleet (drm-0009/p20)

### A-DOCUMENTED-DEFAULT - A documented default and the observed distribution differ by two orders of magnitude at the tail
- **Control:** Bound the lease at the guard rather than in prose: cap the TTL, and require a recorded reason above a stated threshold. Report the TTL distribution in doctor so the drift is visible as a number. The rule that should hold — 'a lease covers the minutes you are editing a file, never an area you intend to own' — is only real if something refuses the twelve-hour lease. (automated control)
- **Boundary:** Applies where leases are advisory over shared files. A long lease is legitimate for a genuinely exclusive, long-running operation — which should be a different verb, not a longer default.
- **Confidence:** v  - **Source:** fleet (drm-0009/p21)

### DC-131 — A defect reported as a POPULATION is closed by fixing a MECHANISM, and the population is never counted

- **Shape:** the report is a count — a screenshot of a process list, a growing table, *"these are
  accumulating"*. Investigation finds a real leak, fixes it, and proves the fix with a real test.
  The report is then closed on the strength of that test. **But the evidence offered was a
  population and the evidence returned was a mechanism**, and nothing ever went back and counted.
  The symptom survives, because a population can have more than one source and the fix addressed
  the source that was looked for. **Every individual claim is true and the answer to the question
  asked is still wrong.**
- **Signature:** *"fixed — here is the test"* answering *"there are still N of them"*; a close with
  no post-fix census; a census with no attribution column, so the count cannot be split by owner; a
  second report of the same symptom treated as a regression of the first fix rather than as
  evidence of a second source.
- **Instance (terminal hosts, 2026-09-11) — reported THREE times, wrong twice:** TH1 fixed a test
  launcher that closed handles without ending processes. TH2 fixed the product leaking an entire
  ACP engine tree (`AcpEngineProcess.cs:117`), measured from outside the leaking process, red then
  green. **Both were real, both are still fixed.** Both were reported to the operator as the answer.
  The first census of the whole population was taken only after the third report: **287 console-host
  processes, of which 256 were held by `node.exe` under `copilot.exe` — a different application
  entirely — 20 by orphaned MSBuild `/nodeReuse:true` workers, and ZERO by anything named `AiDe`.**
  The two fixes were complete and the operator's screenshot was accurate; they were about different
  populations. *No test that passed could have told me that, because none of them counted anything.*
- **What the census cost, and why it was never taken:** one process enumeration with an ancestry
  walk. It was not taken because the mechanism was found quickly and confirming it felt like
  confirming the report — **the fix was verified and the ANSWER was not**. The orphan check that was
  run looked one level up, found every host's direct parent alive, and reported zero orphans; the
  20 MSBuild workers were invisible to it because *they* were the orphans and the hosts beneath them
  were correctly parented. **A containment check one level deep confirms containment one level deep.**
- **Control:** when a defect arrives as a count, **the close requires a census with an attribution
  column** — every member of the population assigned to an owner, including the members that turn
  out to belong to someone else. *"Zero of these are ours"* is a result; *"the leak I found is
  fixed"* is not an answer to *"why are there still so many"*. Ancestry walks run to the root, not
  one level. For this instance the mechanism control is
  `tools/verify-node-reuse-control.py` + `Directory.Build.rsp` (falsified both directions: 16 → 0),
  and the gate carries `--behaviour` so its own oracle is executable rather than asserted.
- **Relationship to DC-123:** DC-123 is the *mechanism* half — containment installed one ring in
  and absent one ring out, which is exactly why `verify-test-run.py`'s environment variable did not
  reach a hand-typed `dotnet build`. **This is the *reporting* half:** DC-123 explains why the
  symptom persisted, DC-131 explains why it was declared resolved twice while it did.
- **Recurrence 2 (2026-09-11, the FOURTH report) — THE CENSUS WAS TAKEN, AND IT WAS TAKEN ON THE
  WRONG KEY.** The control above says the close requires an attribution column. One was produced,
  twice, and both times it reported **"zero with `AiDe` anywhere in the ancestry"** — while four
  `AiDe.Daemon.exe`, each holding its own console host, were on the operator's screen. The census
  matched **process NAMES** up the chain. Nothing a test spawns is *named* `AiDe`: the identity
  lives in the **executable path and the command line**, and `conhost.exe` carries neither. Matching
  on the one attribute that cannot hold the answer returns a confident zero. **The operator was
  right all four times; the instrument was reading the wrong column.** *A census satisfies DC-131
  only if its key can represent the thing being attributed* — state the key, and prove it can return
  a non-zero answer before trusting a zero.
- **Recurrence 3 (same day) — the mechanism half moved one ring out AGAIN.** `Directory.Build.rsp`
  retired MSBuild *worker* reuse. The **Roslyn compiler server** (`VBCSCompiler.exe`) is a different
  server with its own lifetime and its own switch, and was never in scope. Measured: one build
  leaves one `VBCSCompiler.exe` **orphaned** — parent dead — holding one `conhost.exe`, command line
  `-pipename:<base64>`: no project, no repository, no worktree, unattributable by construction.
  `dotnet build-server shutdown` clears it, measured 1 → 0. **Three fixes on one symptom, each
  correct, each one ring short of the next.**
- **The over-correction, recorded because it is the same defect pointed the other way.** The first
  cut of `tools/reap-stragglers.py` attributed by searching the concatenated command lines of the
  whole ancestry, and classified **543 of 547** processes as foreign — including the investigating
  session's own shells, because *the diagnostic command lines contained the word `copilot`*. An
  ancestor that merely **mentions** a keyword poisoned every descendant. **A confidently wrong
  attribution column is worse than no column**, and it fails in the direction nobody re-checks,
  because it agrees with the hypothesis. A process *name* in the ancestry is a structural fact; a
  string inside somebody's argv is a rumour.
- **What the fourth census found, as the denominator this class demands:** 547 host-like processes —
  **529 foreign** (256 `node.exe` MCP servers under `copilot.exe --acp --stdio`, spawned by
  **Windows Terminal's own agent host** — `wta.exe` from `Microsoft.IntelligentTerminal`, whose argv
  carries `--agent "copilot --acp --stdio"` literally — plus their 256 console hosts and 17 others),
  **1 ours** (the orphaned compiler server), **3 ours-live**, **14 honestly unattributed**. The
  hypothesis that the ACP pool was TH2's pre-fix cohort was **refuted**: creation times validate as a
  real chain (copilot 10:37:46.410 < wta 10:37:45.455 < WindowsTerminal 10:37:45.025 — no recycled
  pid), and `EngineCatalogTests` asserts the product path *refuses* to launch `copilot`. **The trap
  worth keeping: that command line is character-for-character what our own `EngineCatalog` would run
  for a native-ACP engine.** Attribution by command line alone scores it as ours. The discriminator
  is the parent's argv, not the child's.
- **Control (recurrence 2/3):** `tools/reap-stragglers.py` — census with a real attribution column,
  ancestry walked to the root, **dry-run by default**, `foreign` and `unknown` reported and never
  removed, the documented `dotnet build-server shutdown` preferred over killing, and a refusal to act
  while any build or test is live. `--self-test` (19 assertions, wired into `build.yml`) carries the
  two field errors as executable oracles: the `wta`/`copilot` trap, and `AiDe.Daemon.exe`, which is
  **parentless by design** — it holds the workspace lock across a shell restart and is bounded by a
  30s idle grace (`IpcServer.Idle`) — so reaping it because it matches the shape of a leak would drop
  a live workspace. `--behaviour` orphans a real compiler server and proves the fix clears it.
- **Instance (INV-0008, 2026-09-11, a different domain — the same shape):** "lots of cases of
  dark/hard-to-read font colors" is a population report. It was closed by `5213d7bb` with a
  mechanism fix (eleven pairings + implicit defaults) and a green floor, and no census. The census
  taken afterwards — 180 pairings, each row attributed to its mechanism (`app-style token`,
  `inherited`, `page-css`, `token`) — found 14 below floor the floor could not see and split them
  by owner: 13 to the fix's own leaf style, 1 to the composer page's CSS. Same rule, same answer:
  **the close of a count is a count.** After the fix, the census is 180 / 0 and it is the control.
- **Recurrence 4 (2026-09-12, the FIFTH report — INV-0010).** The census existed, its key was
  right, and the population was reported again. Counted first this time, twice, every `unknown`
  attributed by creation time + command line + parent-at-creation + the ConPTY signature: **0
  product hosts** at either census; **223 of 271** are 111 `node.exe higgsfield-mcp` servers and
  their console hosts under Windows Terminal's own agent — the *same* foreign pool recurrence 2/3
  attributed (256 then), reset by a Windows Terminal restart at 17:02Z and regrown at ~5/hour; 15
  are Claude Code's own `Monitor` loops; the 25 `unknown` are Windows Terminal's tabs, Ollama's
  launcher, the compiler server's console and those loops. **The control asked "whose is it?" and
  the operator asked "why is it still there?"** — a correct attribution column closed our side and
  changed nothing on the screen (DC-155). And the product's own share was unmeasurable from the
  product: 4,115 `terminal.start` lines in a day and no stop event exists, so each report re-ran the
  whole investigation from a process list. Measured on the way, the one product mechanism the four
  fixes never touched: a session whose child exits keeps its `conhost.exe --headless` for the App's
  lifetime (DC-156) — counted `ours-live` by this census because its parent is a live App.
  **Control (recurrence 4):** the close of a population report carries (i) an action for the
  largest *foreign* class and the re-count that proves it, (ii) a start/stop pair on the product's
  own emissions so the next report is answered from the log, and (iii) an `ours-orphaned` rule so a
  dead-parent product host cannot hide in `unknown` — the red tests and self-test rows are in
  INV-0010; all three landed on `fix/terminal-hosts-5` (2026-09-12): the `ACTION:` line (DC-155),
  `terminal.stop` + `TerminalHostingLedger.Completions` (starts − stops), and `ours-orphaned`
  (self-test 5d/5e green), plus the held host released with its child (DC-156). **And the
  population itself was ours by cause** (slice 0, the same day): our ConPTY shells inherited
  `WT_SESSION` and Windows Terminal's agent attached one MCP server per shell — the fifth census's
  largest class, attributed "foreign" by a correct ancestry column, was a leak of this repository's
  making. Measured 4/4 → 0/4; the runtime now strips `WT_*`.
- **Status:** `controlled` — the boundary gate is wired and red-first on both clauses, and the
  census shape is written into the close. The general discipline is only as strong as the reviewer
  who asks *"what is the denominator?"* — and, after recurrence 2, *"what is the key, and can it
  ever return a non-zero answer?"*

### DC-132 — A handler is wired to an event the library never raises on the path it was written for

- **Shape:** a user interaction needs observing, so a handler is subscribed to a plausible event.
  The subscription compiles, a grep shows it wired, and every reviewer who reads the subscription
  concludes the interaction is handled. **It is not**, because the library implements that
  interaction *natively* and never routes it through the observed event — it mutates its own state
  directly. Nothing fails. There is no red. The handler simply never runs, and the only way to find
  out is to perform the interaction and watch for an effect that does not arrive.
- **Signature:** a subscription with no test that observes the handler *running for that
  interaction*; a handler whose only coverage asserts it does the right thing *when called*; an
  event named for a concept rather than for a moment the library promises to announce; and the tell
  that makes it unmistakable — **the library ships its own implementation of the very gesture the
  handler is meant to detect.**
- **Instance (INV-0006, 2026-09-11):** `Controller.DragStateChanged += canvas.SetObscured` is wired
  at `WorkbenchShell.cs:1243`. `SetDragging(true)` is reached **only** from `DragOver`
  (`WorkbenchController.cs:254`), so the entire pointer path is dead. What actually happens on a tab
  drag is **AvalonDock's own drag**, mutating `Manager.Layout` directly, observed by nobody.
  `Manager.LayoutUpdated` *is* subscribed twice — `WorkbenchAdapter.cs:52` and
  `WorkbenchShell.cs:511` — and **neither touches the model**, so `MarkDirty` schedules a save of a
  model that does not contain the user's drag. The model is told only by `ReconcileViewIntoModel()`,
  called from four places, **all of them other commands**. The operator's drag was never recorded by
  anything.
- **TWO MECHANICAL DETECTORS WERE TRIED AND BOTH MISSED IT, which is the part worth keeping:**
  (a) *"a subscription to an event not declared in `src/`"* flags **25 events** — `Click`, `KeyDown`,
  `Loaded`, `Tick` — because in a UI framework every event is the library's. A detector that flags
  everything is not a detector. (b) *"an event declared in `src/`, subscribed, never invoked"* finds
  **zero**, and `DragStateChanged` is among the zero: it **is** invoked, from a site nothing reaches.
  **Presence is mechanical; reachability is not**, and this defect lives entirely in the gap.
- **Control:** a handler wired to an interaction carries a test that observes **the handler running
  for that interaction** — not a test that it behaves correctly once called, which is the test that
  exists here and passes. Where the library implements the gesture natively, the Spike Protocol
  obligation applies before depending on the contract: *establish which event the library actually
  raises for this gesture, by running it*, rather than choosing the event whose name matches the
  concept. **The conductor's own brief for this node named an event and a file list; four of six
  briefed claims were false (§10), and the two files where the defect lives were not among the five
  named.**
- **Relationship:** DC-129's sibling. There a launch **succeeded emptily**; here a subscription
  **wires emptily**. Both produce an artifact that reads as correct at every site a reviewer looks,
  and in both the missing thing is an *observation at runtime* that nobody was obliged to make.
- **Status:** `uncontrolled` — the test obligation is stated and the instance is fixed, but no gate
  in this repository can distinguish a live subscription from a dead one, and the two candidate
  detectors were built and both failed. The next handler wired to the wrong event will land green.

### DC-133 — A gate's failure threshold sits above the highest severity its rule set can emit, so it is advisory by arithmetic

- **Shape:** a gate is wired into CI with a `--gate` flag and a documented obligation behind it. The
  flag maps to a severity — it fails on Blockers. **The rule set contains no rule that emits a
  Blocker**, or none that the corpus can trigger. So the gate is unconditional green, and nothing
  anywhere says so: not the flag's name, not the CI step's name, not the output, which faithfully
  prints every finding it is about to not fail on. **It is advisory by arithmetic rather than by
  decision**, which is the difference that matters — nobody chose this, and so nobody can be asked
  to defend it.
- **Signature:** a gate whose `--gate` or `--strict` run is indistinguishable from its bare run; a
  severity ladder where the top rung is unoccupied; a CI step that has never failed and whose
  authors cannot name the input that would fail it; and the tell that settles it — **run it against
  the worst artifact you have and watch it exit 0.**
- **Instance (2026-09-11), two readings by node U2:** `ui-craft-gate.py docs/mockups --gate` exits
  **0 with 66 Majors and 38 Minors**. It fails only on Blocker-mapped findings and **there are none
  anywhere in the corpus**. `AGENTS.md` requires this floor be gated in CI on the grounds that *"a
  lesson recorded as prose is a memoir"* (CI6) — **Correction to my first draft:** `ui-craft.yml` **declares itself advisory in its own
  header** and scans only `docs/mockups` and `DESIGN.md`, so the floor is **unmet, not
  falsified** — the workflow is honest about what it is, and the defect is that the obligation has
  no gate, not that a gate is lying. `--gate` being unable to fire is Verified and unchanged
  (`ui-craft-gate.py:314`).
- **What made it invisible for so long:** I had previously briefed a node that the gate "exits 0
  with 13 Majors present", and treated that as an exit condition with no discriminating power. **The two are NOT
  RECONCILED** — taken at different times, scopes never compared — and this entry records them as
  unreconciled rather than treating the larger as a correction of the smaller. What holds either
  way: a half-measured number let me name the defect while carrying a figure I could not source.
  *A defect that is known about is not therefore sized.*
- **Relationship to DC-006:** they compound, and on the same tool. DC-006 is *the gate read the
  wrong corpus*; this is *the gate could not have failed on the right one either*. Either alone is a
  hole; together the CI step carries no information at all, and its presence is worse than its
  absence because it occupies the slot where a real control would go.
- **Control:** a gate's threshold must be **reachable** — there must exist at least one rule in its
  own rule set that can emit at or above the level it fails on, asserted against the rule set rather
  than against today's findings (a corpus that happens to be clean must not read as a broken gate).
  Every gate wired into CI states, in the step, **the input that would make it fail**; a step whose
  authors cannot write that sentence is not a gate.
- **Status:** `uncontrolled` — the measurement exists and the threshold question is with the Owner,
  because where to set it is a policy call and not a defect fix. No check in this repository
  currently asserts that a gate's failing severity is attainable.

### DC-134 — A throw in ONE subscriber aborts a multicast event, and across an interop boundary it is swallowed, so a whole channel goes silent with nothing to read
> **Renumbered from DC-133 on merge, 2026-09-11.** This class and *a gate's failure threshold sits
> above the highest severity its rule set can emit* were both registered as `DC-133`, on the same
> day, by two trees that could not see each other — node F6 in its worktree and the conductor on
> `main`. `verify-defect-register.py` passed in both, because it enforces one-entry-per-class
> **within a tree**. The tiebreak was *which tree is `main`*, which carries no information about
> merit. **DC-013 recurrence 5.**


- **Shape:** a .NET event is raised over a multicast delegate, and an exception in one handler
  **stops the invocation list** — later subscribers never run. When the raiser is across an interop
  boundary (COM, a native callback, a browser host), the exception is also **caught and discarded
  there**, so it never reaches a crash, a log, or a debugger's first-chance stop. The result is an
  entire channel that delivers nothing, with **no exception, no crash, no counter moving, and no
  error state anywhere to read**. Every diagnostic that works by looking for a failure finds none,
  because the failure was consumed.
- **Signature:** an event that "does not fire" while the thing raising it demonstrably works;
  a drop counter that reads 0 when nothing arrived (0 is correct — the code that increments it is
  downstream of the throw); a second handler added to diagnose the first one, which also never runs
  and is read as confirmation that the source is dead; an event-args property dereferenced without a
  null check because the API's shape implies it is always populated.
- **Instance (composer, node F6, 2026-09-11):** `ComposerSurface.OnWebMessage` opened with
  `foreach (var item in e.AdditionalObjects)`. **Measured:** for a message posted with plain
  `chrome.webview.postMessage` — four of the composer's five message kinds —
  `CoreWebView2WebMessageReceivedEventArgs.AdditionalObjects` reads **`null`**; only
  `postMessageWithAdditionalObjects` populates it. So every page message NREd before reaching
  `ComposerMessageRouter.Route`, WebView2 swallowed it, and **no page-to-host message had ever been
  received.** It was invisible because the handshake was independently deadlocked — the page was
  waiting for a `host.init` nothing pushed — so "the page never mounts" had a sufficient explanation
  already, and the second defect sat behind the first. It surfaced only when a probe drove a
  `postMessage` directly from the page, subscribed its **own** handler to the same
  `CoreWebView2.WebMessageReceived`, and saw *that* handler get nothing either: one handler failing
  is a bug in the handler, two handlers failing on the same event is a bug in the invocation.
- **Why the existing controls could not see it:** `ComposerMessageRouter` is a pure function and has
  156 tests; every one of them calls `Route` directly, which is downstream of the throw. The router
  cannot fail to receive a message it is handed. `ComposerHostIntegrationTests` drove a real page in
  a real WebView2 and printed *"page messages observed: 1"* — **without asserting on it**, so the
  number could have been 0 for months and the test stayed green. A count printed and not asserted is
  a measurement nobody takes.
- **Control:** the event-args access is guarded, and — the rung that actually holds — the composer
  probe **posts a plain `postMessage` on every run and prints what `AdditionalObjects` reads**, with
  `ComposerHostIntegrationTests.TheHandshakePushesExactlyOneHostInitPerMountAndFieldValuesSurviveIt`
  asserting it still reads `null`. If the platform ever changes that, the comment justifying the
  guard stops being true and a test says so, rather than the guard becoming folklore (CI6). The
  handshake oracle is the second half: it asserts a **positive** — exactly one `host.init` reaches
  the page per mount — which cannot be satisfied by a channel that delivers nothing.
- **Swept:** every subscription to a WebView2/CoreWebView2 event in `src/` was read.
  `CanvasSurface.OnWebMessage` touches only `e.WebMessageAsJson` inside a `try`/`catch`, and the four
  navigation handlers read `e.Uri` and set a flag. `AdditionalObjects` is dereferenced in exactly one
  place in `src/`, and it is the one repaired here.
- **Relationship to DC-012:** DC-012 is a run that reports success with fewer tests than exist —
  a *quantity* silently smaller than it should be. This is the same failure mode with the quantity at
  zero and no report at all, which is why it survived longer: DC-012's control asserts on a count,
  and nothing here was counting.
- **Status:** `controlled` — the measurement runs on every probe invocation and is asserted, and the
  positive handshake oracle fails if the channel goes silent again for any reason. What is **not**
  controlled is the general shape: a new interop event handler that dereferences an event-args
  property unguarded would reintroduce it somewhere else, and no gate reads for that.

### DC-135 — One interface, two implementations, and the tests exercise the one the product does not construct

- **Shape:** an interface has two implementations — an older general one and the one the shell
  actually wires. Tests accumulate against the **general** one, because it is the easy one to
  construct: no shell, no host, no projection. A property proven there is then cited as proven of
  the product. It is not. **Both implementations are correct**, the tests are honest, and the
  citation is the defect: nothing in a test that says `new LayoutService()` announces that the
  running path says something else.
- **Signature:** a test constructing a concrete type where the product resolves an interface; a
  count of tests per implementation that nobody has taken; a ruling or a review citing
  *"pre-existing and tested"* without naming which implementation; and the tell that settles it —
  **the obvious oracle goes red against a stub and STAYS red against the real body.**
- **Instance (Ruling 47, 2026-09-11):** the ratification cited `StackState.Maximized` and
  `workbench.maximizePane` as *"pre-existing and tested"*, naming `WorkbenchLayoutTests.cs:285-311`
  and `WorkbenchControllerTests.cs:171-180`. Both construct **`new LayoutService()`** — the tree
  service. The shell constructs **`new ZoneBackedLayoutService()`** (`WorkbenchShell.cs:113`), whose
  projection builds every stack at the default `Docked` and hands `Layout` an **always-empty**
  maximize memo (`ZonesToTree.cs:67`: `ImmutableDictionary<string, StackState>.Empty`). **So
  `StackState` does not round-trip at all, and `Maximized` is unobservable in this product.** The
  operation is real, tested, and not reachable from the running path.
- **THE COUNT, which is the part worth keeping:** swept across `tests/` —
  **66 tests construct `new LayoutService()`; 8 construct `new ZoneBackedLayoutService()`.** The
  overwhelming majority of this repository's layout coverage is against an implementation the shell
  does not run. Nobody had taken that ratio, and it is one `grep -c` away.
- **How the node found it, and why the second red matters more than the first:** the obvious oracle
  — *the session's stack reads `Maximized`* — went red against a stub (`Expected: Maximized ·
  Actual: Docked`) and **stayed red against the real implementation**. A first red proves the test
  can fail; **a second red against working code is the test telling you it is asking the wrong
  question.** The node then wrote the oracle against the *effect* `DESIGN.md` promises — *"siblings
  are temporarily minimized"* — and **re-ran that assertion against the stub** rather than letting it
  inherit the earlier red, which is the step that keeps the replacement honest.
- **Why a ruling made it worse rather than catching it:** the Owner ruled on evidence the conductor
  supplied, and the evidence was *"there are tests"*. **A citation is not a promotion** (NG). A
  ruling that rests on coverage must name the implementation that coverage constructs, or it is
  ratifying a property of code that does not ship.
- **Control:** none yet. The mechanical form is available and cheap — a test that constructs a
  concrete type where the product resolves an interface should have to say so, and the per-
  implementation test counts should be asserted rather than discovered — but no gate here does it,
  and inventing one from this single instance would be the speculative generality the Simplifier
  strikes. **Registered so the next ruling that says "pre-existing and tested" has to name which.**
- **Recurrence 2 (INV-0008, 2026-09-11) — the same shape one layer down: not two implementations
  of an interface, but a TEST that constructs its subjects and a PRODUCT that composes different
  ones.** `ContrastFloorTests` (U2's floor) built eleven controls on a `Window` it made, in the
  rest state, and read the control's property; the product composes on an `Application`, inside a
  dock, in states, and the leaf reads the property system. The floor was green on the commit the
  operator photographed. Count: 11 constructed sites + 18 type checks against **180** composed
  pairings; overlap on the failing family (accent-ground state ink): **0**. The tell was the same as
  Ruling 47's — the obvious oracle went red against the composed shell and stayed red while the
  constructed one stayed green. **Control, this time:** `ShellContrastCensusTests` boots the real
  `AiDe.App.App` out of process and walks what it composes — the population is the product's, by
  construction. The floor is kept as the per-control theory it is and never widened into a second
  census.
- **Status:** `partially-controlled` — the contrast instance is controlled by a census over the
  real composition; the general shape (a test constructing what the product resolves) still has no
  gate, and the next slice decomposed the same way would cite the same coverage again.

### DC-136 — A merge resolved as "regenerate, then stage everything" leaves markers in a file that is patched in place, not regenerated
- **Shape:** the documented resolution for the two recurring conflicts is *union the append-only
  log, regenerate the derived views* — one command, then `git add -A`. That command regenerates
  the files the registry calls `derived`; it **patches** `site/*.html` in place
  (`verify-site-figures.py --update` rewrites counts, never the file). A conflict in a
  figure-patched file therefore survives the resolution with its `<<<<<<<` intact, the staged
  tree carries it, and the resolver's own completion line reads *"every derived view is current
  and every gate is green"* — which was true of the derived views and false of the tree.
- **Signature:** a merge commit that touches `site/*.html`; `verify-no-conflict-markers.py` red
  in CI on a `main` push while the local resolution reported green; identical sides
  (whitespace / end-of-line only), so the conflict was never *about* anything.
- **Instance (conductor-addendum-c, 2026-09-11):** merge `f5c0f740` of `conductor/addendum-c`
  into `main` — `site/index.html:59-63,142-146` and `site/model.html:465-469`, both sides
  byte-identical after trim. Resolved by `regenerate-derived.py` + `git add -A docs site`; the
  conductor ran `verify-derived-views` and `verify-stranded-audit` after the merge and **not**
  `verify-no-conflict-markers`; CI reddened on the push and was not read back (E14: an exit code
  is not a result — read the state). Found by node S1's gate run on `feature/addendum-c`.
- **Sweep:** `git log --merges -- site/` shows this is the third merge in two days that touched
  `site/*.html`; the two earlier ones resolved cleanly because only one side had changed. The
  class was latent, not new.
- **Control:** `tools/regenerate-derived.py` `CHECKS` now runs `verify-no-conflict-markers.py`
  first — the one command every resolution already runs fails on a marker anywhere in the tree.
  Observed red with a scratch marker (exit 1), green after (`main`, this commit).
- **Status:** `controlled` — the instance is fixed and the control is in the resolution path,
  not in a step the resolver has to remember.

> **Numbering note.** DC-137 and DC-138 were allocated as DC-136 and DC-137 on `fix/composer-entry-areas` (INV-0007) while `main` spent DC-136 for the merge-marker class; renumbered at the merge (DC-013). The audit entry `al-01M28Z19PG92S9YE390MPKB7TR` and the commit `a764b344` cite the pre-merge numbers; every other citation was renumbered with the register.

### DC-137 — A content-sized reader is docked beside a filling writer, so the reader is measured first and the writer receives what is left

- **Shape:** an input host that fills its container (`LastChildFill`, a `*` row, an `HwndHost`/
  `WebView2`) shares a panel with a read-only element that is sized by its **content** — a
  `TextBox`/`TextBlock`/`StackPanel` with a `MinHeight` and no `MaxHeight`, docked or given an `Auto`
  row. The panel measures the docked/auto children **first, with infinite extent on the docked axis**,
  so the reader reports its whole content height and the writer receives `max(0, remaining)`. The
  surface is inverted — **the reader is sized by content and the writer by remainder** — and it gets
  worse with use: every line the operator writes makes the reader taller and the writer shorter.
- **Signature:** `DockPanel.SetDock(x, Dock.Bottom|Top)` or `RowDefinition { Height = Auto }` on a
  content-growing read-only element whose sibling is the input surface; a `MinHeight` with no
  `MaxHeight` on a box that mirrors what is typed; a surface that shrinks as the user types; an
  operator report that says "I cannot type" or "I cannot see the fields" with a screenshot in place
  of a number, because nothing logs rendered bounds.
- **Instance (INV-0007, 2026-09-11, the operator's own report):** `ComposerSurface` docked a
  `StackPanel` footer to the bottom of its `DockPanel` and gave the `WebView2` editor host the
  remainder; the footer's read-only compiled-view `TextBox` had `MinHeight = 90` and no ceiling.
  **Measured in the real shell under the operator's recorded arrangement:** the editor host laid out
  at **0px of a 485px composer** (F5's choreography), **105px of 684px** and **110px of 689px**
  (maximized, Ruling 47), the compiled box at 465px in all three — 28 lines of goal block. Arithmetic
  the runs agreed on: `editor = composer − compiled − 114px`, clamped at 0. Necessity: capping the
  compiled view at 35% from outside the product gave the editor 202px in the same 485px and the run
  went green.
- **Class → sweep → derive → prevent.** *Class:* the sizing **order**, not the number of lines — any
  content-sized reader docked beside a filling writer does this. *Sweep* (every surface pairing an
  input host with docked chrome, read at INV-0007): `PromptDraftSurface` — the writer is the fill
  child and the docked items are fixed-height bars, ruled out; `TerminalSurface`/`TerminalView` — no
  `Dock.Bottom`, ruled out; `ConsoleSurface`, `ExplorerSurface`, `NodeReaderView` — no content-sized
  reader against a filling input, ruled out. One instance in the tree. *Derive:* the rule is **the
  writer is never smaller than the reader, and the reader keeps to ≤ 35% of the surface**, stated as
  `ComposerSurface.CompiledShareCeiling`. *Prevent:* below.
- **Why it survived:** the composer's bare-window probe gave the surface 700px and never saw the
  starvation (DC-135's shape: the harness composed what the product does not); the composer emitted
  **no diagnostics at all**, so the operator's log ended at the gesture and the geometry had to be
  read off a screenshot.
- **Control:** (1) the compiled view's `MaxHeight` is set in the surface's own `MeasureOverride`,
  **before any child is measured**, to `⌊min(0.35·H, (H − chrome)/2)⌋` with the chrome (picker, bar,
  label, lease, status, footer margin) measured right there — so the reader scrolls inside its
  share, the writer is never smaller than it **whatever the status line wraps to**, and there is one
  layout pass with no transient starvation (a `SizeChanged`-driven cap was the first form; the Test
  Architect's boundary — a refusal wrapped to five lines at 485px — broke it: *editor 138px, compiled
  169px*). (2)
  `TheWriterKeepsItsRoomTests.TheEditorHostIsNeverSmallerThanTheCompiledViewAndTheCompiledViewKeepsToItsCeiling`
  — the surface measured detached, no browser, at **485px, 1000px, and 485px with the wrapped
  status**, with the operator's ~30-line text, asserting `editor ≥ compiled`, `compiled/H ≤ 0.35`
  (the test's own constant, equality-checked against the product's), `compiled ≥ min(⌊0.35H⌋,
  ⌊(editor+compiled)/2⌋) − 1` (a reader collapsed to its `MinHeight` is the opposite starvation),
  AND that the single `composer.layout` line the surface emits carries the same heights the tree was
  arranged at (the log must not disagree with the screen). **Observed red on the un-fixed control:**
  *"editor host 0px, compiled view 465px of the composer's 485px"* and *"421px, 465px of 1000px"*;
  the wrapped-status row red under the share-only rule by mutation. (3)
  `ComposerHostIntegrationTests.TheComposersEntryAreasKeepTheirRoomAfterTheNewSessionChoreography` —
  the same rule in the real docking host under the recorded arrangement, with `fields=6 editors=3`
  and a keystroke that reached the draft; **observed red on `main`: exit 24, 110px/465px (67%)**;
  green after: **334px/241px (35%)**. (4) The rendered bounds are now emitted on the normal path
  (`composer.layout`: composer, editor and compiled `width`/`height`, `visible`, `loaded`; `null` —
  never 0 — for a part whose arrange is not valid, proven by
  `APartWhoseArrangeIsNotValidIsReportedAsNullNotZero`; re-emitted when either height moves > 24px),
  so the next report carries the number rather than the screenshot.
- **Not controlled, and named as such:** a **new** surface that docks a content-sized reader beside
  a filling writer. The sweep-shaped guard was considered and not written: its token set cannot be
  stated honestly — "content-sized" is a property of the element's measure, not of any text a grep
  can match (`Dock.Bottom` on a fixed-height bar is fine; on a growing `TextBox` it is this class),
  and an allow-list of variable names would be a claim nobody could verify at a glance (GO14a). The
  control for a new surface is the same STA layout test, written for it when it is built — a
  reviewer's question, not a gate.
- **Status:** `partially-controlled` — the instance is controlled at two heights in the fast ring
  and in the shell; the class is caught only where a layout test is written for the surface.

### DC-138 — One-time initialisation hooked to a per-attach event, behind a once-gate keyed to the wrong lifetime

- **Shape:** a hosted control does its one-time start-up in a `Loaded` handler. `Loaded` is raised on
  **every** attach to a loaded tree, and a docking host that rebuilds its layout re-parents every pane
  on every render — so the start-up runs again: subscriptions are added a second time (every event
  now handled twice), a page is navigated a second time, and any handshake gate that is once-per-
  **surface** treats the new page's first message as a duplicate of the old page's. The surface dies
  silently, or reloads on every layout command, and nothing counts attaches or navigations.
- **Signature:** `Loaded +=` with side effects on a non-`Window` element; an `IsReady` that is never
  reset; duplicate event subscriptions after a layout change; a web page that "resets" after a pane
  opens; a graph that "keeps refreshing"; `router drops` climbing after a render while
  `host.init count` stays at what it was.
- **Instance (INV-0007, 2026-09-11, reproduced — not the operator's report):** `ComposerSurface`
  and `CanvasSurface` both hooked `Loaded += InitialiseAsync`. **Measured in the real shell:** after
  one later `Adapter.Render()` the composer read `wpf loaded=2 unloaded=1, navigations started=2
  (+1), editor.ready posted=2 (+1), router drops=2, host.init count=0, fields=0, editor text=''`
  while the host's draft still held the operator's text; the canvas read *navigations started by the
  same render = 1*. `main`'s own two renders coalesce into one `Loaded` (`wpf loaded=1`), so the
  first render after the mount is the one that fires — a pane open, a layout command, a restore.
- **Class → sweep → derive → prevent.** *Class:* a once-only gate keyed to the wrong lifetime — the
  init is once-per-**attach** when it must be once-per-**surface**, and the readiness gate is once-
  per-**surface** when it must be once-per-**document**. *Sweep* (every `Loaded +=` in
  `src/AiDe.App`, read and counted at HEAD): `CanvasSurface.cs:105` — confirmed, same class;
  `ComposerSurface.cs:122` — the instance; `MainWindow.Loaded`, `TextPromptDialog.window.Loaded` —
  a `Window` is never re-parented, ruled out. Two of four. *Derive:* the `simplify:` marker at
  `CanvasSurface.cs:211` (*two web-hosting idioms coexist … trigger: a THIRD web surface — one host
  abstraction is cheaper than three copies*) predicted the class by count; it arrived by another
  route — the same bug in both copies. The marker is retired: the upgrade it named is taken.
  *Prevent:* below.
- **Why it survived:** the handshake probe mounted the surface once in a bare window; nothing
  counted `Loaded`, navigations or attaches; the composer logged nothing; and the router's 156 tests
  call `Route` directly, downstream of the reload (DC-134's shape — a pure function cannot fail to
  receive what it is handed).
- **Control:** (1) **`WebSurfaceHost`** — the one re-attach-safe WebView2 host, used by both
  surfaces: the guard is set *before* the first await, so a second `Loaded` during runtime start-up
  is a re-attach and not a race — proven deterministically by
  `AnAttachDuringTheRuntimeStartIsAReattachNotASecondStart` (the runtime start held open on a
  `TaskCompletionSource`, `Loaded` raised four times, the initialiser run once after release;
  **red by mutation** with the flag moved after the await: *Expected 1, Actual 4*); every attach is
  recorded (`web-surface.handshake`: `initialising` once, `re-attached` thereafter, `init-failed`
  once with `errorCode` `WEB.INIT_THREW` and the exception type — `AFailedStartIsReportedOnceAndNotRetriedOnReattach`). (2)
  Readiness is re-keyed **per document**: `ComposerMessageRouter.BeginNavigation()` is called from
  the composer's own `NavigationStarting` for every navigation it *allows* (a cancelled one replaces
  nothing), resetting `IsReady` and the per-field revisions, so a genuine reload's `editor.ready` is
  a mount that re-pushes `host.init` with the draft as it stands — proven at the router by
  `TheVocabularyIsClosedTests.AReadyAfterADeclaredNavigationIsAMountNotADuplicate` (**observed red
  against a no-op `BeginNavigation`**: `IsReady` stayed true; a late change from the old page is
  dropped and raises no mark), and **through the product surface** by two probe scenarios:
  `ComposerHostIntegrationTests.AGenuineReloadRemountsThePageWithTheDraft` (`core.Reload()` after
  the mount: *navigation-started 1->2, init-pushed 1->2, router drops +0, fields=6*, the editor
  showing the draft; **red by mutation** with the `BeginNavigation` wiring removed: exit 26,
  *init-pushed 1->1, host.init count=0 fields=0*) and `…ACancelledNavigationResetsNothing` (the page
  tries `location.href`, the policy cancels: *composer navigation-started 1->1, raw
  NavigationStarting=2, reached draft=True*; **red by mutation** with the cancel early-return
  removed: exit 27, *page ready=False, reached draft=False* — the operator's typing silently lost).
  (3)
  `TheWebSurfacesInitialiseOnceAcrossReparentsTests` — both surfaces in a real window, attached,
  detached and re-attached **three times**, asserting `Attachments == 4` (non-vacuity) and
  `InitialisationsStarted == 1`, and that the log carries one `initialising` and three `re-attached`
  for the surface's id; **seen red by mutation** with the host's guard removed: *Expected 1, Actual
  4*, both surfaces. (4) The sweep-shaped guard
  `NoElementOutsideTheHostHooksLoadedForItsOwnInitialisation` — root `src/AiDe.App`, every `*.cs`
  and `*.xaml` outside `bin`/`obj`, regex `\bLoaded\s*\+=|\bLoadedEvent\b|\bLoaded="`, allow-list
  by relative path `{src/AiDe.App/MainWindow.xaml.cs, src/AiDe.App/Workbench/TextPromptDialog.cs,
  src/AiDe.App/Workbench/WebSurfaceHost.cs}` (Windows, and the guard itself); **observed red at
  HEAD:** `CanvasSurface.cs` and `ComposerSurface.cs` both matched. Adding a path to the allow-list is
  a claim that its handler is idempotent under re-attach, made beside the entry. (5)
  `ComposerHostIntegrationTests.TheComposerPageSurvivesALaterRender` — the visible consequence in
  the real docking host; **observed red on `main`: exit 25**, green after: `navigations +0,
  editor.ready +0, router drops=0, host.init count=1, fields=6`, `re-attached` logged, no
  `message-dropped` (every refusal is now a line, once per kind and reason per document — the
  dropped `draft.changed` the SRE asked for included); the canvas sibling at `--height 1400`:
  *navigations started by the same render = 0* (was 1), measured from the canvas core's own
  `NavigationStarting`.
- **Relationship to DC-132/DC-134:** DC-132 is a handler on an event the library never raises on
  the path it was written for; this is its mirror — a handler on an event the library raises **more
  often** than the path it was written for. DC-134 is why the drop was silent; this is why there was
  a drop.
- **Status:** `controlled` — the host makes the class impossible for any surface that uses it, the
  sweep guard fails when a surface does not, the once-test fails when the guard is removed, and the
  shell probe fails when the page dies.

### DC-139 — The leaf overrides the container's pairing: an implicit style on a text leaf outranks every state ink the container sets by inheritance

- **Shape:** a theme pairs ink with ground on the **container** — a template trigger sets
  `TextElement.Foreground` on a `ContentPresenter` for selected / checked / disabled / on-accent,
  and the glyphs get it by **inheritance**. Then a hardening pass adds an implicit
  `<Style TargetType="TextBlock">` (and `Label`) with a `Foreground` setter "so every text has an
  ink". In the property system a style setter outranks an inherited value, so every string-content
  `TextBlock` in the shell renders the rest-state ink regardless of its container's state: light
  on the accent at 2.37:1, and "disabled" indistinguishable from "enabled". **Every template reads
  correctly in source; the defect is a precedence rule nobody opened.** The same pass fixed the
  photographed sites by the same mechanism — the leaf setter *masked* text that was inheriting the
  docking theme's black — so the one line was both the fix and the bug.
- **Signature:** a `Foreground` setter in an implicit style whose `TargetType` is a text leaf
  (`TextBlock`, `Label`, `Run`, `AccessText`); a census row whose ink source reads `Style` while it
  sits inside a state trigger's scope; a floor that measures constructed rest-state controls and
  passes while the composed product fails; a "fix" that makes twelve sites go wrong and three go
  right at once.
- **Instance (INV-0008, 2026-09-11, `5213d7bb` → fixed on `fix/contrast-census`):** 13 sites at
  2.37:1 (`TextBrush` on `AccentBrush`: every selected-active dock tab, the checked "Diagram" toggle,
  the selected palette row) and 2 disabled buttons rendering `TextBrush` instead of
  `DisabledTextBrush`. Removing the setter exposed three sites the setter had been masking —
  `DockRoundedTabs.xaml` painted the on-accent ink on every *selected* tab where only the *active*
  one has the accent ground (1.45:1 on the selected-inactive ground), and the composer footer's
  "Compiled view" / lease / status `TextBlock`s inheriting `#000000` from AvalonDock's
  `LayoutDocumentPaneControl` (1.27:1) — the exact lines the operator photographed, which the
  leaf setter had been hiding rather than pairing.
- **Sweep:** every `TextElement.Foreground` on a `ContentPresenter` in `App.xaml` (button, toggle,
  check, radio, list row, menu item) and the tab title in `DockRoundedTabs.xaml` — all reached the
  glyphs by inheritance and all were overridden; measured by the census, not by reading. Chrome
  that strokes a `Path` from `Foreground` was unaffected (no generated `TextBlock`).
- **Fix (the rule, not the sites):** *the container pairs ink with ground; the leaf inherits.* The
  leaf styles keep only `Background={x:Null}`; the `Window` style states the root ink; the island
  card (`SurfaceChrome.WrapAsIsland`) states `TextElement.Foreground=TextBrush` beside the raised
  ground it paints; the tab's on-accent ink follows `IsActive` — the same condition the theme
  paints the accent ground on — never `IsSelected`; the on-accent ink is its own token
  (`AccentContrastBrush`, DESIGN.md `{colors.accent-contrast}`, 6.6:1 on the accent) rather than a
  borrowed surface brush.
- **Control:** (1) `ShellContrastCensusTests` — the real `App` booted out of process, every
  surface, menu and the composer page walked, every text pairing measured from the property
  system and rendered pixels, **zero tolerance**, plus the disabled-provenance fact; seen red at
  14 + 2 on `main`, green at 0 + 0 after. (2) `TokenDisciplineTests.NoImplicitLeafTextStyle_SetsItsOwnInk`
  — no implicit style on a text leaf may set an ink; seen red on the pre-fix `App.xaml` (lines 391,
  396). (3) `TokenDisciplineTests.EveryTriggerThatPaintsAGround_StatesAnInkThatClearsIt` — a
  trigger that paints a token ground states an ink, or the ink in effect clears 4.5:1 on it by the
  dictionary's values; its engine seen red on a planted `IsChecked → AccentBrush` with no ink.
  (4) `ContrastFloorTests`' 18-type theory now asserts the *inverse* for the two leaf types and
  the root ink on `Window`.
- **Status:** `controlled` — the census is the proof of the composition and the two source rules
  fail at the line. What no control here sees: hover / pressed states and popups the census does
  not open (Phase 6 of INV-0008), listed in the census's own omissions table.

### DC-140 — A UI defect report is evidence about a BINARY, and the binary is never named, so a fixed instance re-enters as a recurrence

- **Shape:** a screenshot arrives. It is evidence about the build that produced it, and nothing
  ties it to a commit: the shell writes no launch record, the report names no version, and three
  Release builds from three commits sit on one machine. The instance it shows was fixed two
  commits ago. It is triaged as a recurrence of the fixed class, and the next investigation
  re-derives the mechanism before it can read the informational version off the binary.
- **Signature:** `%LOCALAPPDATA%\AiDe\logs` with layout mutations and crashes but no start
  record; an `AssemblyInformationalVersion` of `1.0.0+<sha>` that the product carries and never
  emits; a report whose only attribution is a timestamp; "still broken" for a site the census at
  HEAD measures as clearing.
- **Instance (INV-0008, 2026-09-11):** the photographed black footer text, white text box and dim
  captions were the ORIGINAL instance on a build before `5213d7bb` merged (H1, verified by the
  informational versions of the Release DLLs on the machine); reported and first read as a
  recurrence. Which process the operator launched was **not measurable** — named as such in
  INV-0008 §0.
- **Sweep:** `WorkbenchDiagnostics` carried `layout.mutation`, `terminal.start`, `crash`,
  `mcp.config` — every event about *what happened*, none about *which build it happened in*.
- **Control:** `WorkbenchDiagnostics.AppStart` — one `app.start` line on the normal path from
  `MainWindow.Loaded`, no flag: `{version, commit (40-hex or null — never invented), configuration,
  theme, dpi{scaleX, scaleY, pixelsPerInch}, window{width, height, state}}`, plus an `app.start`
  activity tagged `service.version` / `vcs.revision`. `AppStartIsRecordedTests` proves the shape
  through the seam (seen red with a stub) and, through the contrast probe's boot of the real
  `App`, that the composed shell emits it with the same commit the probe reads off the assembly
  (seen red before the emitter existed). **The reporting rule:** a UI defect report is attributed
  to `1.0.0+<sha>` from the log before it is triaged as new or recurring.
- **Status:** `controlled` — the binary names itself on every boot. Not yet on the status strip
  or Help → About (INV-0008 Fix D's second half); the log line is the attribution.

### DC-141 — A scripted edit anchors on the first occurrence of a token that prose also contains, and the tree it produces still parses

- **Shape:** a patch script finds its insertion point with `text.index("<style>")` (or the first
  match of a short tag) and the first occurrence is inside an HTML comment that *mentions* the tag.
  The replacement lands in the comment, swallows its `-->`, and the rest of the document becomes
  comment. Nothing fails to parse; nothing fails to build; the page loads and renders an empty
  body. The failure surfaces one ring out, in whatever measures the page — here the census's page
  fact reported 0 text elements and the handshake probe reported the module never ran.
- **Signature:** `.index(` / first-match anchoring on a token shorter than a line; a diff whose
  hunk begins mid-comment; a page that loads with `__composerError` undefined (the module never
  executed) rather than set (it threw).
- **Instance (`fix/contrast-census`, 2026-09-11):** the composer stylesheet retokenisation anchored
  on `<style>`, which `composer.html`'s CSP comment contains ("CodeMirror's style-mod injects a
  <style> element"). Caught the same turn by `ShellContrastCensusTests` (DC-016 guard: a page that
  measured nothing is a failure, not a pass) and `TheHandshakePushesExactlyOneHostInitPerMount…`.
- **Control:** the authoring rule the same session's other edits already followed — anchor on the
  whole unique block and `assert text.count(anchor) == 1` before replacing; never on a bare tag.
  The downstream control that actually fired is the census's non-empty-corpus assertion.
- **Status:** `partially-controlled` — caught by a downstream measurement, not at the edit; the
  anchor-uniqueness assertion is a discipline, not a gate.

### DC-142 — A worktree cleanup removes the tree of an OPEN node because "no unique commits" was read as "no longer needed"
- **Shape:** the fail-safe cleanup (WT7) refuses a tree only for *data* reasons — primary, cwd,
  locked, held by a live session, dirty, unique commits, branch checked out twice. A tree whose
  branch is **pushed and clean** passes every test even when the node that owns it is still open
  and one operator gesture away from its exit. The tool prints the verdict as `clean, merged,
  unheld`, and *merged* there means "every commit exists somewhere else", not "merged into
  `main`". A conductor reading the word at speed removes the tree it meant to keep.
- **Signature:** `coord worktree cleanup --remove` run without reading `cleanup` (the dry run)
  first; a REMOVE row whose branch has commits `main..<branch>` > 0; a node in the plan still
  marked open for that branch.
- **Instance (conductor-addendum-c, 2026-09-11):** `cleanup --remove` was run to drop a finished
  spec scratch tree; it kept that one (a live session held it) and removed
  `C:/Projects/ai-de-feature-exit-evidence` — F5's tree, 16 commits ahead of `main`, waiting on
  the operator's `File → New Session` gesture (Ruling 49). Nothing was lost — F5a had pushed, which
  is exactly why the rules allowed it — and the tree was re-added at the same path at `729fdb5e`
  in one command. What was lost was the build output and the run marker, and the operator's
  confidence that the conductor reads before it removes.
- **Sweep:** every `cleanup --remove` in this session before this one had been preceded by a
  `list` read the same turn; this was the first blind run. The plan's own contract names DC-120
  (no repo-wide destructive command while sibling nodes are live) and this is that class one ring
  out: the command is tree-wide, not repo-wide, and the sibling was not live, only open.
- **Control:** two, and only the second counts. (1) Prose: the conductor runs `cleanup` (dry run)
  and reads every REMOVE row's branch against `git rev-list --count main..<branch>` before
  `--remove` — a memoir. (2) Proposed for the pack (`coord-core.py` is pack-managed; a repo-local
  edit is a deviation `updatepack` must merge): `worktree_safety` adds a hold reason *"branch has
  N commit(s) not on the default branch — open work; pass `--include-unmerged` to remove"*, so an
  open node's tree is HELD by default and removable only by name. Until that lands the status is
  as below.
- **Status:** `partially-controlled` — the instance is repaired; the mechanical control is a pack
  proposal, filed with this entry, not yet a gate.

### DC-143 — A theme declared as a rule with no values is a theme that cannot fail
- **Shape:** the design language declares one mode's values and, for the other, a sentence
  (*"inverted roles, same semantics"*). Every measurement of the second mode — the contrast
  census, the mockup's live audit, `ui-craft-gate.py`'s token check — has nothing to resolve, so
  it reports *not measured* or (worse) passes on the default mode's values. A mode with a rule
  and no values cannot fail, and a mode that cannot fail is indistinguishable from one that was
  never checked. The mockups meanwhile invent their own light values in a harness block, so the
  product has a light palette after all — in the wrong file, unreviewed, and different per mockup.
- **Signature:** a `Modes` table row whose *Ground* column is prose; a census run that says
  *"not measured until values are declared"*; two mockups whose `:root[data-theme="light"]` blocks
  disagree on the accent.
- **Instance (D1, `addendum-c-chain`, 2026-09-11):** `DESIGN.md:121` (light = *"inverted roles,
  same semantics"*, values declared for dark only, `:5-48`); `spec-addendum-c-perspectives` §C7
  and §R row 24 recorded the light census as *not measured*; `session-front-door.html:30-36`
  carried a light set (`#2C6FA8` accent) that no artifact governed. Fixed by declaring every role's
  light value as a flat `light-<role>` key under `colors:` (the form both readers accept —
  `note-addendum-c-design-signature` §3), re-picked for AA on the light grounds.
- **Sweep:** high contrast has the same shape but is *correctly* value-less (the values are the
  OS's) — recorded in the Modes row as a decision, with the mockups' hc values labelled stand-ins.
  No other `DESIGN.md` mode exists.
- **Control:** `tools/verify-design-modes.py` — every colour role must carry a `light-` twin, no
  orphan mode key, and `accent-contrast` and `border-strong` must exist; wired into `build.yml`
  beside the craft floor. Observed red with `light-accent` removed and with `accent-contrast`
  removed (`--self-test`, 4 cases), green on this commit.
- **Status:** `controlled` for the light mode; `partially` for the runtime — the census
  (`ShellContrastCensusTests`, merged with INV-0008) runs dark today; its light run is the
  acceptance test for the declared values.

### DC-144 — A palette table that measures every ink on one ground never lists the family that fails
- **Shape:** the design language's contrast audit is a table of inks × *one* ground
  (`{colors.surface}`), so every state that paints a different ground — the accent-filled tab,
  the checked toggle, the selected palette row, the primary button — has no row, and a light ink
  on the accent ground at 2.37:1 is invisible to the audit while the audit reads *all AA*. The
  code then borrows a ground token as an ink (`SurfaceSunkenBrush` on the New session glyph),
  because no token *named* the on-accent ink.
- **Signature:** a "Contrast (on `{colors.surface}`)" column; a state matrix that names a ground
  and not its ink (or the reverse); a census row whose ground is `AccentBrush`.
- **Instance (D1 / INV-0008, 2026-09-11):** `DESIGN.md:72-85` (the one-ground table); the census
  found 12 of 180 pairings at 2.37:1 on `AccentBrush` and the selected-inactive tab at 1.45:1 on
  `#2A313B` (a line token used as a ground). Fixed by the **ink × ground matrix** (every pairing a
  state may render, both themes, `—` where forbidden), `accent-contrast`'s explicit role as the only
  ink on the accent ground (the code's `AccentContrastBrush`), and the rule that every state names
  both an ink and a ground (PS-T1); the one-ground column's ratios were stale (13.9 vs 14.98) and
  are deleted in favour of the matrix. DC-139 is the implementation-side twin (the leaf overrides the
  container's pairing); this class is the audit's blind spot that let it pass.
- **Sweep:** the front-door mockup's audit already paired `--accent-contrast` on `--accent`; the
  three Addendum C mockups list the accent-as-ground family explicitly and classify each pairing
  (text / ui / decorative). The same blind spot recurred one level down in the second pass: the
  audits classified the *ink* on a highlighted menu row and never the highlight against the menu
  ground as a state indicator (1.14:1) — the reviewer's N1; the fix is the row focus ring and a `ui`
  audit row for every state indicator, not only every text pairing.
- **Control:** at the design layer, `verify-design-modes.py` refuses a `DESIGN.md` without
  `accent-contrast`; at the product layer, the runtime census over the composed tree
  (`ShellContrastCensusTests`, 180 pairings / 0 below floor on `main` at `cb4a6ebe`) is the control.
  The matrix itself is prose — hand-typed ratios can drift on the next re-tone; the census
  re-measures them.
- **Status:** `controlled` — named ink, matrix, token control and the product census in place.

### DC-145 — A per-file allow-list of gated artifacts lets every new artifact enter ungated
- **Shape:** a CI floor is scoped to the artifacts under review by naming them one by one. The
  list is correct on the day it is written and wrong on the day the next artifact is authored:
  the new file joins the corpus with no gate, drifts, and the gate stays green because it never
  looked. The allow-list *is* the defect — the default was "ungated", and the author had to
  remember a script to change it.
- **Signature:** `GATED = [("docs/mockups/<one-file>.html", ...)]`; a new mockup commit that does
  not touch `tools/`; a craft report whose per-file counts include a file the gate does not name.
- **Instance (D1, 2026-09-11):** `tools/verify-ui-craft-floor.py:69` gated
  `session-front-door.html` and `DESIGN.md`; the three Addendum C mockups would have entered
  ungated. First fix appended them to the list (the instance); second fix inverted the default —
  every `docs/mockups/*.html` is gated at Major unless named in `LEGACY_ADVISORY` with its reason.
- **Sweep:** the twelve legacy mockups are the exemption (60 Majors of deliberate DX17 density,
  per the `ui-craft.yml` header); no other per-file allow-list of gated targets exists in `tools/`
  (`grep -n "GATED\|ADVISORY" tools/*.py`).
- **Control:** `verify-ui-craft-floor.py` `gated_mockups()` — the exemption is the thing that must
  be written; `--self-test` still discriminates (Nit fails, Major passes on the front door). Green on
  this commit over five gated files.
- **Status:** `controlled`.

### DC-146 — A derivation reads the render instead of the source
- **Shape:** a security-relevant derivation (a scope, a permission, a lease) is fed the fully
  *compiled/rendered* artifact rather than the narrower, trusted input the derivation is actually
  meant to read. Everything the render additionally carries — an attachment's file content, a
  catalog template's fixed prose, any other text a later feature folds into the compiled view — is
  content the person never typed, and it silently rides along into the derivation because the
  derivation cannot tell the two apart: both are just characters in the same string by the time it
  sees them. The bug widens a security boundary (here, a lane's exclusive write scope) rather than
  narrowing it, so it fails open, not closed.
- **Signature:** a `Derive(...)`/`Extract(...)`-shaped function whose parameter is a `.Text` off a
  `Compiled*`/`Rendered*` record, rather than a member of the input the operator actually edits; two
  call sites of the same derivation (a "what will be sent" site and a "what is displayed" site) that
  happen to agree only because they both read the same over-broad blob; a fixture that types the
  probe mention into the one editable field the test author had in mind and never into an
  attachment or a template body, so the widening path is never exercised.
- **Instance (Ruling 66 / F-2, `addendum-c-chain`, 2026-09-11):** `ComposerSendGate.cs:164` derived
  the sent lease, and `ComposerSurface.cs:449` derived the displayed lease, both from
  `ComposerCompiler.Compile(draft, template).Text` — the whole rendered prompt, which
  `ComposerCompiler.RenderAttachment` (`ComposerCompiler.cs:136`) and `TemplateCompiler.Compile`
  fold an attachment's file content and a template's fixed body into, verbatim. A file the operator
  attached, or a catalog template's own prose, could therefore name a path the operator never
  referenced and widen the lane's write scope. Fixed by `ComposerDraft.SourceText`
  (`ComposerDraft.cs`) — a shape-scoped accessor over only what the operator typed (the free-form
  text, the six goal-block field values, or the active template's field values) — and re-pointing
  both call sites at it.
- **Sweep:** every other `.Derive(`/`.Extract(`-named function in `src/` (`grep -rn
  "static.*Derive(\|static.*Extract("`) is `DeterministicSignalsDeriver`, which reads an explicit,
  already-structured `AuditSignals` record, not a rendered blob — not an instance. Every other
  reader of `CompiledPrompt.Text` (`ComposerSendGate.Prompt`, `ComposerSurface.CompiledView`,
  `ComposerSendRecord.Committed`'s attachment *counts*) is meant to read the whole rendered text —
  that is the compiled view's whole point (Security C15) — so none of those is this shape.
- **Control:** `EveryLeaseDerivationCallSiteInSrcPassesTheSourceTextSymbol`
  (`tests/AiDe.App.Tests/Composer/TheLeaseDerivesFromTheEditorsSourceTextTests.cs`) — a source-scan
  guard, root `src/`, recursive excluding `bin/`/`obj/`, over the token set
  `{LeaseDerivation.Derive(, LeaseDerivation.Patterns(}` with no allowlist: every call site found
  must pass an argument ending in `.SourceText`, and today's two sites are named, not counted, so a
  third site added later fails this test by name until it is updated. The behavioural half —
  `AnAttachmentBodyMentionDerivesNoPatternButTheEditorTextStillDoes` and
  `ATemplateBodyMentionDerivesNoPatternButAFieldValueStillDoes` — was observed red on `main` before
  the fix (attachment case: `Assert.DoesNotContain` found `"src/AiDe.Core/Bar.cs"` in the derived
  lease; template case: found `"docs/plan.md"`).
- **Status:** `controlled`.

### DC-147 — An in-artifact measurement that throws before it renders leaves its placeholder on screen, and the craft gate launders the claim
- **Shape:** a reviewable artifact carries its own measurement (a verdict strip: contrast pairs,
  target sizes, density) so that its hub document can say *"measured, not asserted"*. A script
  error before the audit runs (a syntax slip in the strip's own string concatenation) leaves the
  placeholder *measuring…* on screen forever. Nothing errors visibly, the artifact still opens,
  the deterministic craft gate reports the file clean (it measures markup and computed style; it
  does not execute the page the way a browser does), and the hub `.md` repeats a number that was
  never computed. The claim survives because the only reader that could refute it is a browser
  console nobody opened.
- **Signature:** a verdict element whose text is its initial placeholder after load; an
  `Uncaught SyntaxError` / `ReferenceError` in the console of a mockup; a hub node citing
  "N pairs, 0 fail" that no run produced; a craft-gate report of 0 findings on a file whose
  in-artifact audit never ran.
- **Instance (D2, 2026-09-11):** `docs/mockups/new-session-sheet.html` (D1, on `main`) —
  `'</b>') · <b class=…` at its verdict line, a missing `+'`; the strip read *measuring…* from the
  day it was committed while `ui-review-perspective-shell` §2b cited its 27 pairs. Fixed here (the
  instance); its strip now reads *0 contrast fail · 0 target < 24px · 0 tier field · 28 pairs*.
- **Sweep:** an Edge headless render of every `docs/mockups/*.html` found the same class in four
  legacy mockups (`app-facelift`, `context-map-join`, `knowledge-explorer`, `uml-erm-surfaces`:
  `Uncaught ReferenceError: h_theme is not defined`) — reported to the conductor, not fixed in this
  node. The three Addendum C/D mockups and the new session mockup render their strips.
- **Control:** `tools/verify-mockup-audits.py` (X-1, INV-0008 phase 6's sibling track) — a headless
  sweep, stdlib only, that shells out to whatever Chromium-family browser is already installed
  (Microsoft Edge / Google Chrome / Chromium — `ubuntu-latest` ships all three, verified against
  `actions/runner-images`' Ubuntu 24.04 software manifest; Windows ships Edge) with
  `--headless=new --dump-dom` and `--enable-logging=stderr --v=1`, exactly reproducing the browser
  console DC-147 says nobody opened. `--self-test` plants a broken verdict strip (an undefined
  bare identifier, the four legacy mockups' own shape) and asserts BOTH that the planted breakage
  is caught and that a healthy fixture stays clean — observed red with detection disabled, green
  with it restored. Wired as the last step of the `gates` job in `.github/workflows/build.yml`,
  beside `verify-ui-craft-floor.py`. All 17 `docs/mockups/*.html` swept clean after the four
  legacy mockups' fix below.
- **Instance, the four legacy mockups (2026-09-12):** each declared its harness controls with
  hyphenated ids (`h-theme`, `s-ctx`, `st-gen`, …) and then referenced them as bare identifiers
  with underscores (`h_theme`, `s_ctx`, `st_gen`, …) — relying on the DOM's implicit named-access
  globals, which exist only for hyphen-free ids. Every such reference threw
  `Uncaught ReferenceError` before the harness (theme/motion/density/state toggles) could wire up.
  Fixed with the smallest correct change: one `const` block per file aliasing each hyphenated id to
  its bare name via `document.getElementById`, touching no markup, no CSS, no other logic.
- **Status:** `controlled` — the instance (`new-session-sheet.html`) and the sweep's four
  legacy-mockup instances are both fixed; the control is a script in CI (`--self-test` red-first,
  then wired into `gates`), not prose.

### DC-148 — A command mutates the model of a view that is not on screen, and reports the model's success as the screen's

- **Shape:** the shell holds two bodies — the docking workbench and the full-body Explorer — and
  swaps which one is the window's content (ADR-0017). Every catalog command that opens a dock
  document (`session.new`, `workbench.newCodeViewer`, class diagram, sequence diagram, search,
  diagnostics, prompt draft, terminal) stays reachable from the menu, the rail, the palette and
  the chords **while Explorer is the body**. Each applies its zone operation, renders the docking
  manager, and composes its announcement from the layout result — *"Session opened. Composer bound
  … Maximized the center."* — all of which is true **of the model**. The docking host is
  unparented (`ShellModeController.Set(Explorer)` → `_host.Content = _explorer`), so nothing in it
  can raise `Loaded`, measure, or start a browser; the operator sees the Explorer, unchanged. It is
  DC-011's inverse: the system speaks, and speaks about a tree nobody is looking at.
- **Signature:** an `open-*` `layout.mutation` line followed by `configured` and **nothing else**
  for the surface — no `initialising`, no `composer.layout` — after an `explorer-graph` line; a
  `WorkbenchRoot.Parent == null` at command time; an announcement built from `LayoutResult` with no
  reference to what hosts the layout; a shell that does not know which body is showing.
- **Why it survives:** ADR-0017 says the non-active mode is *retained, never rebuilt*, and every
  test of that invariant proves the workbench survives a mode switch — none asks what a workbench
  command does **during** one. The harness that proved New Session (INV-0007's `--shell` probe)
  hosted the workbench as the window's content directly, with no mode controller, so the state was
  unreachable there (DC-135). And the log had no `shell.mode` line, so the mode had to be inferred
  from a surface that initialises only inside it.
- **Instance (INV-0009, 2026-09-11 22:34:09Z):** the operator entered Explorer at 22:34:00Z
  (`explorer-graph initialising`), ran `File → New Session` twice, saw nothing, and closed the app.
  Both documents logged `configured fields 6` and no `initialising`. Replayed from the log's own
  restore payload through the product's `ShellModeController`: **red** with the Explorer as the
  body (`composer wpf loaded=0 … initialising=0 configured=1`), **green** with the workbench as
  the body in the identical arrangement, and green again when the workbench returned to the body
  with the document untouched — necessary and sufficient. The sibling code viewer measured the
  same (`wpf loaded=0 isLoaded=False`).
- **Sweep:** every `OpenReferenceDocument` caller and every `Service.Apply(AddSurface)` +
  `Adapter.Render()` command in `WorkbenchShell` (`:283–360`, `:1574`, `:2909`) has the shape; none
  consults the mode. `ReopenSessionAsync` too. Nothing in `WorkbenchController` or the catalog
  carries a "needs the workbench body" attribute.
- **Control:** `ASessionDocumentIsShownWhereTheOperatorIsTests.ANewSessionCreatedWhileExplorerIsTheBodyIsShown`
  (probe `--session-render --explorer --sibling`), **observed red on `main` `1aadde84`** (exit 30)
  and on the merged branch `4c19b497`; **green on `228339d6`** (INV-0009 Phase 1): the shell raises
  one seam, `WorkbenchShell.DocumentOpening`, immediately before every `AddSurface` that opens a
  document, and `MainWindow` handles it with `ShellModeController.Set(Workbench, "document-opening")`
  — the Explorer surface retained, only unparented (ADR-0017 holds). The mode is in the log:
  `shell.mode` with its trigger, one line per change. The structural half is
  `EveryOpeningCommandPassesThroughTheSeamTests` — root `WorkbenchShell.cs` only, no recursion,
  token set `new LayoutOperation.AddSurface(` / `OpeningDocument();`, allowlist empty — which also
  asserts the window's and the replay's wiring by one token (DC-135). The necessity oracle
  (`LeavingExplorerShowsTheSessionCreatedInsideIt`) asserted the pre-fix state by construction and
  became `ANewSessionCreatedInsideExplorerLeavesItBecauseADocumentOpened`, which asserts the trigger.
- **Status:** `controlled` — both halves red-then-green on 2026-09-11/12; the sibling code viewer
  measured `isLoaded=True` in the same run.

### DC-149 — A flow acquires a resource by asking the operator, uses it for one half of the work, and refuses the other half for lack of that resource

- **Shape:** with no workspace open, `File → New Session` interposes a workspace chooser; the
  chosen root binds the session (`SessionConfigStore(root, …).Create`) and the document opens. The
  window's own workspace is **not** opened. `BindComposer`'s first guard then reads the window —
  `DataContext … WorkspaceRoot` — and refuses: *"repositoryRoot: this window has no open workspace,
  so a run has no checkout to cut a worktree from."* Every word is true, and the operator just
  chose that workspace in a dialog the flow put up. The refusal names the window's state, not the
  flow that produced it, so the next action the operator infers (open the workspace) replaces the
  layout and drops the document they just made.
- **Signature:** a chooser whose result reaches a store constructor and never a `DataContext`; a
  refusal message about a precondition the same flow could have established; a session created in
  workspace W while the window shows "No workspace open".
- **Why it survives:** the design says *"a session cannot exist unbound"* and the flow honours it
  for the *session*; nothing says the *window* must be bound to the same workspace, and the two
  consumers of the root (the store and the composer's run context) were wired in different files.
  The refusal path is announced and shown (DC-011 honoured), which reads as correct behaviour.
- **Instance (INV-0009, 2026-09-11 22:33:28Z):** session `…ba326cf3` — the operator's first action
  after launch. Its composer rendered (`page-ready`, 617 px editor) and was never configured; the
  status line carried the `repositoryRoot` refusal (not logged — inferred from the code path and
  the single `workspace-open` line at 22:33:53Z, and replayed in the probe's `--prior-document`
  step, whose status reads the same sentence). This is the *"blank editor with the compiled box
  under it"* the operator described. At 22:33:53Z the operator opened the workspace and the restore
  dropped the document from the layout.
- **Control:** the Owner ruled the flow **opens** the chosen workspace (not a refusal).
  `NewSessionFlow.StartAsync` runs the chooser's root through the window's ordinary open path
  (`openWorkspace` → `MainWindow.OpenWorkspaceAtAsync`) before the sheet, and the sheet binds to
  the workspace the window then reports. Oracles:
  `TheNewSessionSheetTests.TheChooserOpensTheChosenWorkspace_ThenTheSheetBindsToWhatTheWindowReports`
  (order `choose → open → sheet`, the sheet's root is the window's) and
  `…_AndAFailedOpenCreatesNothing` — neither compiled against the flow with no open step; and
  `ASessionDocumentIsShownWhereTheOperatorIsTests.ASessionCreatedThroughTheChooserIsBoundToTheChosenWorkspace`
  (probe `--session-render --chooser`): `bound-to-chosen-root=True`, `init-pushed=1`, six fields,
  and a cancelled chooser adds no surface and no session. The refusal is in the log now:
  `SessionComposerBinder` writes `session-document.refused` with the field and the reason
  (`TheBinderRecordsWhatItBoundTests`).
- **Status:** `controlled` — INV-0009 Phase 3 (`d2409515`) and Phase 4 (`58c991ca`).

### DC-150 — A shell's location and the runtime's current directory are two states, and a relative path is resolved against the one the operator is not looking at

- **Shape:** an agent works in a worktree with `Set-Location <worktree>` and then calls a runtime
  file API with a *relative* path — `[System.IO.File]::ReadAllLines('docs/lessons/defect-classes.md')`.
  PowerShell's location is a PowerShell state; .NET's `[Environment]::CurrentDirectory` is the
  process's, still pointing at wherever the process started (the primary checkout). The read and
  the write both succeed, in the wrong tree; nothing fails loudly; the worktree file is untouched
  and the primary's is rewritten. The session-worktree discipline (WT1–WT12) exists precisely so
  that two agents never share a checkout, and this defeats it from inside a single command.
- **Signature:** a relative path handed to a runtime API (`[System.IO.*]`, `Path.GetFullPath`,
  Python's `open()` after `os.chdir` was *not* called) from a shell whose location was set with
  `cd`/`Set-Location`; `git status` in the primary showing a file the session never meant to touch;
  a write that "did nothing" in the tree the agent is looking at.
- **Instance (2026-09-11, this run, the INV-0009 implementation node):** resolving a merge
  conflict in the register from the fix worktree, a PowerShell one-liner read and wrote
  `docs/lessons/defect-classes.md` through `[System.IO.File]` with a relative path; the primary
  checkout's copy was rewritten with CRLF line endings (content identical — `git diff` empty) and
  the worktree's copy was unchanged, so the conflict "resolution" resolved nothing and the primary
  showed ` M docs/lessons/defect-classes.md`. Caught by the marker count after the edit reading 6,
  not 0. The primary was left as found except for the line endings, which the operator restores
  with `git checkout -- docs/lessons/defect-classes.md` there.
- **Why it survives:** the two cwds agree in every interactive session (a person starts the
  shell where the work is), so the habit of relative paths is never punished; an agent's shell is
  started once, in the primary, and then moved.
- **Control:** every file operation from an agent's shell uses an **absolute** path, and file
  edits go through a script that is itself launched from the worktree (a native process inherits
  the shell's location; a runtime API call inside the shell does not). What fails when the shape
  recurs: the pre-commit boundary check runs on staged paths in the worktree and cannot see the
  primary — so the control is the session's closing `git status` in the **primary** checkout,
  recorded in the node's report (this run's report carries it). Proposed: `coord worktree list`
  gains a `--dirty-primary` line so a session that dirtied the primary is told at cleanup.
- **Status:** `uncontrolled` — the instance was reverted in content and reported; no gate fails
  on the shape yet.

### DC-151 — An oracle reads a value clamped at its bound as the behaviour under test
- **Shape:** a test asserts "the offset is at its maximum" (or any value equal to a bound it is
  clamped to) as proof that the behaviour under test moved it there. When the bound itself moves —
  a virtualized panel's extent is an *estimate* that shrinks and grows as containers of different
  heights are realized; a scrollable height, a page count, a capacity — the value reads as "at the
  bound" whether the behaviour ran or not. The assertion cannot fail, so it proves nothing, and it
  passes on the first run, which is when it is believed.
- **Signature:** an assertion of the form `Assert.Equal(bound, value)` or `value >= bound - ε`
  where `bound` is read from the same object after the operation; a "follow" / "auto-scroll" /
  "fills to capacity" test with no independent witness (the last item realized and in view; a
  count that does not derive from the bound); a pass on a shape the author expected to fail.
- **Instance (DS-1, 2026-09-11):** `spikes/session-thread/RESULT.md` Q4a — the follow-rule leg read
  `offset_after_append == scrollable_after_append` and reported `followed_by_itself=True`; the
  extent had moved 5528.8 → 5495.6 across the append, so the offset was clamped, not followed. The
  design records the leg as *not evidence* and the CV-1 oracle L3 asserts the independent witness
  (the last container realized and in view), never the offset alone.
- **Sweep:** `tests/AiDe.App.Tests` has no scroll-offset assertion today (`ScrollToEnd`,
  `VerticalOffset` appear only in `ClassDiagramSurface.cs` and `CommandPalette.cs` in `src/`); the
  class is registered before its first product instance.
- **Control:** the design's test plan names the witness for every follow/pin assertion (L3); the
  Test Architect's review checks that no `thread.*` oracle compares a value to a bound read from the
  same object. **Proposed:** a `verify-perf-assertions.py`-style lint for `Assert.Equal(x.ScrollableHeight, x.VerticalOffset)`
  shapes when a second instance appears.
- **Status:** `uncontrolled` — no product instance yet and no gate; the control is the design's oracle wording and
  the review checklist item.

### DC-152 — A registered derived artifact with no merge attribute merges as authored, and `doctor` reports the driver effective

- **Shape:** the coordination layer declares an artifact's class in two places written by two
  mechanisms - the registry line (`.agents/artifacts.yml`, hand-editable) and the git attribute
  (`.gitattributes`, written by `coord install` *from* the registry). A line added to the registry by
  hand after the install never gets its attribute. The class is declared; the driver never fires;
  the file conflicts with markers exactly as if it were authored - and `doctor` says *"merge driver
  effective"* because it checks that **a** driver is declared and registered, not that **every**
  registered pattern carries an attribute.
- **Signature:** `git check-attr merge <path>` → `unspecified` for a path the registry calls
  `derived`; a conflict with markers in a file whose class promised regeneration; the registry's own
  comment recording that exact conflict.
- **Instance (2026-09-11):** `docs/_meta.json` - registered `derived` by hand (the registry says so:
  "SETUP 4 registered only the first ... I declared one path anyway"), attribute absent, 10 of 11
  patterns attributed. Repaired by P1 with the one line `coord install` would have written, after
  running `tools/build-doc-viewer.py` (diff: the provenance stamp only).
- **Control (proposed):** a coverage check that reads the registry's `derived`/`register` patterns
  and fails when any lacks a `.gitattributes` line - the natural home is `coord doctor`'s merge-driver
  check (pack-owned) or, in this repository, `tools/regenerate-derived.py`'s existing
  `check_registry_coverage()` widened from "has a generator" to "has a generator **and** an
  attribute". Red first: remove the `_meta.json` line and the check must fail.
- **Status:** `partially-controlled` - the instance is fixed; the check is prose until the tool lands.

### DC-153 — A per-repository coordination marker tracked as a file in one checkout is mutated by any worktree's command, and the mutation lands as an uncommitted change in a tree that did not run it

- **Shape:** the layer's record is **per repository** (`repo_root()` resolves through the git common
  dir, so `.agents/` is one directory for every worktree). One of its files - the regeneration-owed
  marker - is **tracked in git**. Any worktree that runs `coord regen` rewrites or deletes the marker
  in the *primary checkout's* working tree. The session that ran it cannot reverse the change (it is
  not its tree), and the session that owns the primary did not make it and may commit it unaware.
- **Signature:** `D .agents/regen-owed.txt` (or a modification) in `git status` of the primary
  checkout with no primary-side action that explains it; `doctor` in a worktree reporting "nothing
  owed" while the worktree's own tracked copy still lists paths.
- **Instance (2026-09-11):** P1 ran `coord regen` in `ai-de-feature-addendum-c` to clear seven owed
  regenerations (all regenerated with **no diff** - the marker was stale bookkeeping). The command
  deleted `C:/Projects/ai-de/.agents/regen-owed.txt`; the node was refused permission to restore it
  (correctly - the primary is not its tree). Reported to the conductor with the one-line restore.
- **Control (proposed):** the marker is runtime state, like `.agents/sessions/` - git-ignore it
  (`docs/notes/conductor-agents-gitignore-deviation.md` is where that decision lives), or have
  `coord regen` refuse when the record root is not the current worktree unless `--here` is given.
  Red first: run `coord regen` from a linked worktree against a fixture and assert the primary's
  tree is untouched.
- **Status:** `partially-controlled` - reported and the branch removes the stale marker; the
  mechanism is unchanged.

### DC-154 — A security control written for one shape is applied to every shape, and refuses work it cannot protect

- **Shape:** a control derived for a shape that carries a risk (a lane that can write needs a lease,
  so the seam monitor can discriminate) is applied by the gate to every instance of the broader type
  (every send), the shape without the risk included. The refusal is correct by the control's letter
  and protects nothing; the operator experiences the constitution as "too restrictive in
  straightforward scenarios" (Ruling 73 (c): *a security control gates only the shape it protects*).
- **Signature:** a refusal whose remedy names a resource the operation does not use (*"reference the
  files this run may write"* on a question); a doc line *"no X means no run"* where X matters for a
  subset; a test asserting the refusal on the shape without the risk; a control whose name says the
  broader type (*the lane's pin*) rather than the shape (*the lane that can write*).
- **Instance (2026-09-11, CV-0):** C17's lease gate on every send — `ComposerSendGate.Send` called
  `LeaseDerivation.Derive` for a free-form Message, `Lease`'s constructor threw
  (`LeaseAndSeams.cs:47`), and the surface read *"no write scope could be derived… Reference the files
  or directories this run may write"*. Fixed by projecting the shape first (`ComposerDraft.TurnShape`,
  `ComposerCompiler.IsReadOnly`), applying the gate to the write shape only, and removing the
  capability from the read-only shape instead (`GovernedRunHost.ReadOnlyLaneSession`).
- **Control:** the test pair in `tests/AiDe.App.Tests/Composer/TheReadOnlyTurnNeedsNoLeaseTests.cs` —
  `AMessageWithNoMentionDerivesNoLeaseAndIsNotRefused` (red on `main`) beside
  `AGoalBlockWithADerivedScopeStillTakesTheLeaseGateUnchanged` (green throughout): a control that
  fires on both shapes is the signature. The Security lens's rule on every finding — *name the shape
  the control protects; a shape without the risk is exempt by construction; a finding that names no
  shape is returned, not applied* — is prose until the persona audit gains the check (a finding for
  the pack).
- **Status:** `controlled` by the pair; the lens rule is `partially-controlled`.

### DC-155 — A symptom owned by someone else is closed by attribution, not by an outcome

- **Shape:** a population report is investigated to DC-131's standard: counted, every member
  attributed, and the largest class turns out to belong to **another application**. The close says
  *"foreign — reported only, never removed"* and stops. The population regrows (it was never
  touched), the operator's screen is unchanged, and the next report arrives with the same count —
  read as a recurrence of *our* defect, which re-runs the whole investigation.
- **Signature:** the largest class in a census is `foreign`; the close carries no action for it;
  the same foreign root (`wta.exe` → `copilot.exe --acp --stdio` → `node higgsfield-mcp`) appears
  in two consecutive censuses with a reset between them; the operator's report count keeps rising
  while "zero of these are ours" keeps being true.
- **Why it survives:** DC-131's control demands an attribution column and gets one. Attribution is
  a *finding*; the operator's symptom is an *outcome*. Nothing in the standard asks the closer to
  hand the operator the one action that shrinks the foreign share, or to re-count after it — so a
  correct census closes the investigation and leaves the screen.
- **Instance (INV-0010, 2026-09-12 — the fifth report):** recurrence 2/3 of DC-131 found 256 MCP
  servers under Windows Terminal's agent and filed them `foreign`. Windows Terminal was restarted;
  by the fifth report the pool was 111 (+111 console hosts) and growing ~5/hour. The source is the
  operator's **global** `~/.copilot/mcp-config.json` (`higgsfield`, the pack's own reference
  generation backend), spawned per use by the `wta.exe` host and never reaped — a Copilot CLI /
  Windows Terminal lifecycle defect, triggered by a configuration we recommended.
- **Control:** `reap-stragglers.py`'s report names, for the largest foreign root, **the action that
  shrinks it** (scope or remove the global MCP server; restart the host; file the lifecycle defect
  upstream) and the close of any population report includes a **post-action re-count**. Red first:
  the report on a fixture with a dominant foreign root must carry an action line. INV-0010 phase 2
  and phase 5.
- **Instance 2 (2026-09-12, the correction — the misattribution itself):** the first fix of this
  entry, hours earlier, printed an action line that sent the operator to *their* MCP config:
  *"not AiDe's -- the Copilot agent spawns this MCP server per use … scope or remove the server in
  ~/.copilot/mcp-config.json"*. The parent was right and the **cause was ours**: a ConPTY shell of
  ours inheriting `WT_SESSION` from the Windows Terminal tab the harness runs in makes Windows
  Terminal's agent host attach an agent session (its MCP servers) to it. The conductor's hourly
  correlation of `terminal.start` against node births put the pool on our test runs; measured here
  4/4 → 0/4 with `WT_*` stripped. **A cause attributed by ancestry alone is a label, and the label
  pointed at the wrong owner twice** — "foreign, reported only" and then "foreign, here is your
  action". INV-0010 slice 0.
- **Fix (2026-09-12, `fix/terminal-hosts-5`, corrected the same day):** the cause is removed
  (`ConPtyInterop.BuildEnvironmentBlock` strips `WT_*` from every ConPTY child); `reap-stragglers.py`
  `report()` ends with an `ACTION:` line for the largest foreign root that names **the cause and the
  mechanism** — *"CAUSED BY THIS REPOSITORY, foreign only by parent: a ConPTY shell of ours that
  inherited WT_SESSION … Restart Windows Terminal, then re-count: a birth AFTER the fix is a spawn
  path that still inherits WT_*"* — and, on a second line, **the birth correlation**: how many of
  the pool were born within 10 s of one of our own `terminal.start` lines (the workbench log; *not
  recorded* when the log cannot be read, never 0). Live at 14:58Z: 371 under `wta.exe`, 185
  servers, *54 of 371 dated members born within 10 s of one of our 5,218 terminal.start lines
  (App sessions; test-run sessions are not in the log)*.
- **Control (corrected):** the **cause-vs-parent rule** — a census reports, for the largest foreign
  class, the birth correlation with the product's own start events and not only the ancestry, and
  its action line names the mechanism. Self-test rows 5j (the line names the cause and the
  `WT_SESSION` mechanism and does **not** name the operator's MCP config — observed red on the
  first fix's text) and 5k (`births_near`: 2 of 4 dated births follow a start; a member with no
  creation time is in neither count).
- **Status:** `controlled` — the cause is removed and proven red-first at two levels
  (`EnvironmentBlockTests`, `TerminalChildEnvironmentTests`); the census carries the cause and the
  correlation; the post-fix re-count (INV-0010 phase 5: restart Windows Terminal, compare against
  371) is the operator's, and a birth after the fix is a finding.

### DC-156 — A resource acquired for a child is released with the owner, not with the child

- **Shape:** an object acquires an OS resource *for* a child (a pseudo console for a shell, a job
  for a process). The child ends; the object records the end as **state** (`Ended`, `Complete`,
  an exit code) and touches no **handle**. The resource then lives as long as the *owner* — the
  App, the test host — and nothing in the owner's life ever revisits it, because "the session
  ended" reads as "the session is finished with".
- **Signature:** an `Ended`/`Completed` state that still owns OS objects; a `Complete`/`OnEnded`
  path with no `Close*`/`Dispose`; a count of hosts equal to *panes* rather than to *live
  children*; a resource whose release is only ever measured on the owner's exit path.
- **Why it survives:** every exit-path test measures the owner ending (window close, process exit,
  kill, dispose) — INV-0010 measured four of them clean. The in-life path is not an "exit" and is
  never on the list; and a census attributes a host under a live owner as *live* because the key
  (parent pid, worktree path) cannot distinguish a held host from a working one.
- **Instance (INV-0010, 2026-09-12):** `ConPtyTerminalSession.WatchForExitAsync` → `Complete(exit)`
  marks the session `Ended` and closes neither the pseudo console nor the job; `TerminalSurface.PumpAsync`
  returns and keeps the dead session. Measured with the owner alive: `cmd.exe /c exit 0` → 1
  `conhost.exe --headless` before, **1 three seconds after the exit** (`TerminalHostInLifePathTests
  .ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives`, red). Tab-close dispose on
  the same runtime measured 1 → 0.
- **Sweep:** `AcpEngineProcess` closes its job with the process (ruled out by reading);
  `ShellBootstrap` holds no handle (ruled out); `WebSurfaceHost` (a WebView2 browser process per
  surface) **not yet swept** — next step.
- **Control:** the red in-life test above (observed failing on the un-fixed code: `1 headless
  console host(s) still owned by the live owner 3s after 'child-exit-then-hold'`); once `terminal.stop`
  lands, `TerminalHostingLedger.Completions` makes *held = starts − stops* a number a gate can read.
- **Fix (2026-09-12, `fix/terminal-hosts-5`):** `ConPtyTerminalSession.WatchForExitAsync` →
  `Complete(exit)` → `ReleaseHost()`: the pseudo console and the job are taken out of their fields
  under the state gate and closed the moment the child's exit is seen; `DisposeAsync` takes the same
  handles through the same gate and finds zero. Measured with the owner alive: 1 headless host while
  the child (`cmd.exe /c "ping -n 8 … & exit 3"`) ran, **0 three seconds after its exit**, exit code
  3 — the child's own. Paths 1–4 re-measured 0. The read loop now ends on the child's exit too (the
  host's departure is its EOF), so the pump thread is released with the child as well — and it
  drains to EOF unconditionally, because `ClosePseudoConsole` waits for the host and the host
  waits for its pipe (the SRE lens's finding: a loop that stopped at completion could wedge the
  closer with the host alive after `terminal.stop`). **Decided, not assumed:** closing the job at
  the child's exit ends anything the shell left running inside it (a `Start-Process`, a background
  server) at the shell's exit rather than at the tab's close — the containment ADR-0005 states,
  now applied at the child's end; INV-0010's "the job, which is then empty" was a belief.
- **Sweep (this fix):** `AcpEngineProcess` — same shape (the job outlives the engine's own exit
  until the lane's `Dispose`), bounded by the run rather than the App and the job's close *is* the
  tree's reaping, no change; `WebSurfaceHost` — the browser process is the resource and its exit
  releases it, but nothing handles `CoreWebView2.ProcessFailed`, so a browser that dies leaves a
  blank pane with no line (a failure mode, not this class — next step); `WorktreeProvisioner`,
  `WorkbenchShell.cs:2638` — `using` + `WaitForExit`, released with the child; `ShellBootstrap` —
  no handle held.
- **Status:** `controlled` — `TerminalHostInLifePathTests.ASessionWhoseChildExited_…` green on the
  fix (observed red on the un-fixed code, 1 → 1); the five exit paths are read by name in CI
  (`tools/verify-terminal-host-exit-paths.py`, appended to the Windows job, its own `--self-test`
  firing on a failed and on an unexecuted path); `TerminalHostingLedger.Completions` makes
  *held = starts − stops* a number.

### DC-157 — A test's positive control is satisfied by the defect the test guards

- **Shape:** a measured fact has two clauses — *the instrument can see the thing* (≥ 1 while it
  exists) and *the thing is gone afterwards* (0). Written red against the defect, the first clause
  passes because the defect **holds the thing still**: the leaked host is there to be counted for
  as long as anyone likes. The fix releases it within milliseconds, the instrument's one read takes
  a second, and the fact goes red on its *positive* clause — reading as "the fix broke the
  instrument" when the instrument never saw a live child at all.
- **Signature:** a red-first test whose ≥ 1 / non-zero pre-condition was only ever observed on
  un-fixed code; a fixture whose transient is shorter than one instrument read (`cmd.exe /c exit 0`
  under a CIM census); a fix that turns a fact's second clause green and its first clause red.
- **Why it survives:** the positive clause is the DC-131 recurrence-2 control (*"can the key ever
  return non-zero?"*) and it *did* return non-zero — for the wrong reason. Nobody re-asks the
  question on the fixed code, because the fixed code is where the second clause is being watched.
- **Instance (2026-09-12, INV-0010 phase 3):** `TerminalHostInLifePathTests
  .ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives` — red as `1 → 1` on the
  un-fixed runtime; on the fixed runtime `liveCount` read 0 (*"the helper's session should have
  owned a conhost.exe --headless while it started; saw 0"*). The helper's child became
  `cmd.exe /c "ping -n 8 127.0.0.1 >nul & exit 3"`: seven seconds alive, then its own exit — and the
  fact reads `1 → 0`, code 3.
- **Control:** the positive clause stays in the fact and is **observed on the fixed code** before
  the red is called green (the Proof Pack's red/green row carries both counts); a fixture whose
  transient an instrument must catch outlives one instrument read by design, and says so in a
  comment. DC-102's cousin: there the mechanism was never exercised; here the instrument was.
- **Status:** `controlled` — the fact asserts both clauses and both were observed on the fix
  (`1` live, `0` at +3 s); the helper's comment names the shape.

### DC-158 — A scheduled rename lists the declaring file, and its reference set is discovered by the build inside the node

*Id left for the conductor to allocate at the join (`coordination-addendum-cd` §Seams: contiguous family, DC-013).*

- **Shape:** a coordination plan (or the ADR it dispatches) schedules a type rename, and the track's
  ownership row names the file that **declares** the type. The **references** live in files no row
  owns — an out-of-process probe, a replay test that asserts the type's `ToString()` in stdout. The
  node cannot build the solution without writing outside its paths, and the plan's own "fails if a
  write lands outside the lane's paths" clause fires on a write the plan made necessary.
- **Signature:** `grep -rln <OldName> src tests` returning a file outside the track's row; a compile
  error in a project the track never listed (`tests/*Probe/`); a node's close report carrying a
  "forced write, reported" note; a `Fails if` clause that the correct change trips.
- **Instance (SH-1, 2026-09-12):** ADR-0030 rule 4 renames `ShellViewMode` → `Perspective` "only in
  the commit that implements this ADR"; the plan's SH-1 row lists the Core/App files and `their
  tests`. `ShellViewMode` was also referenced in `tests/AiDe.App.ComposerProbe/Program.SessionRender.cs`
  (three `Set` calls, two `mode={mode.Mode}` prints) and asserted by string in
  `tests/AiDe.App.Tests/Sessions/ASessionDocumentIsShownWhereTheOperatorIsTests.cs`
  (`mode=Explorer`, `mode=Workbench` in the replay's stdout). The build found the first; the replay
  tests found the second (red: *Sub-string not found*). Both edited under a recorded claim and
  reported; the ADR's own Evidence section cites only `ShellModeController.cs:6-11`.
- **Control:** a plan that schedules a rename computes the reference set at plan time — one
  `grep -rln` per renamed identifier over `src/`, `tests/`, `spikes/` — and lists **every** file in
  the owning row, or names the rename as a seam. The build is already the detector; the missing half
  is the planning step, in `/prepare-for-coordination` and `/execute-with-coordination`. Red first: a
  plan fixture that renames a type referenced across two rows must be reported by the planner.
- **Sweep:** SH-2's own scheduled rename (`ShellModeController` → `PerspectiveShell`) has references
  in `MainWindow.xaml.cs`, `tests/AiDe.App.ComposerProbe/Program.SessionRender.cs`,
  `ExplorerModeTests.cs`, `EveryOpeningCommandPassesThroughTheSeamTests.cs` (the last two scan-shaped
  on the type name) — the same shape, one slice later; the conductor should list them before dispatch.
- **Status:** `uncontrolled` — the plan step is prose until the skill carries it.

### DC-159 — A probe prints a type's default `ToString()` across a process boundary and a test asserts the string

*Id left for the conductor to allocate at the join.*

- **Shape:** an out-of-process probe writes a value to stdout by interpolating the value itself
  (`mode={mode.Mode}`), so what crosses the boundary is the type's **default** `ToString()` — an
  enum's member name today, a record's full positional dump tomorrow. The in-process test asserts
  the string. The contract is therefore keyed on a type's *shape*, not on a stable identity, and any
  change of shape (the enum becoming a record; a positional member added) re-writes the wire without
  any caller changing.
- **Signature:** a stdout/JSON line whose value is a bare `{x}` of a non-string type; a test
  asserting `mode=Explorer`-style tokens; a rename that turns a green replay red with
  *Sub-string not found* and no behavioural change.
- **Instance (SH-1, 2026-09-12):** `Program.SessionRender.cs:283,670` printed `mode={mode.Mode}`;
  when `ShellViewMode` became the `Perspective` record the line became
  `mode=Perspective { Id = explore, ... }` and both replay assertions went red. Fixed by printing the
  row's stable id (`mode.Mode.Id`) and asserting `mode=explore` / `mode=coding` — the same id the
  `shell.mode` diagnostic line writes, so the probe and the log agree.
- **Control:** a value that crosses a process boundary is written as its **stable id** (a string
  field chosen for the purpose), never as the value itself; the assertion names the id. Where the
  diagnostics already write an id (`WorkbenchDiagnostics.ShellMode` → `mode = to.Id`), the probe
  writes the same one. The replay assertions are the detector and did their job; the rule is prose.
- **Status:** `partially-controlled` — the detector exists; no scan refuses a bare `{value}` of a
  non-string type in probe output.

### DC-160 — A Proof Pack figure or "red observed" cell is written before the measurement that would fill it

*Id left for the conductor to allocate at the join.*

- **Shape:** the pack is drafted while the suites run, and a cell that will hold a measured number —
  a test count, a mutation's red list — is filled with a **plausible** value so the sentence reads
  complete. The measurement then lands somewhere else (the runner's summary, a JSON the loop
  wrote) and the cell is never re-read against it. The document advertises "Verified" over a
  number nobody observed. Sibling of DC-082 (a figure that was right and moved): this one was never
  right, it was **typed**.
- **Signature:** a count in a summary that no run output reproduces; a "red observed: Mn" cell
  naming a mutation whose recorded red list does not contain the test; the tell that settles it —
  the pack and the run record disagree and the pack is the rounder number.
- **Instance (SH-1, 2026-09-12):** the pack's first draft said *"Core 2,268/0, App 680/0"* — no run
  had produced either number (the runs said 2,262 and 666, then 673); and claims 1–2 cited mutation
  M9 for `TheBodiesAreTwoHostsAndOneFullWindowSurface` and `EveryRowHasACatalogCommand…`, which M9's
  recorded red list did not contain (a fourth row yields a fourth command, so the count still
  matched). The Test Architect read the JSON against the pack and caught both; two further
  mutations (M17, M18) were run to earn the cells.
- **Control:** a pack's measured cells are **filled from the run record**, never typed — the
  mutation table is generated from the loop's JSON (as SH-1's now is), and the suite counts are
  pasted from the runner's summary line after the last rebuild. The reviewer's check is the
  detector: *does the record support each cell?* Red first: a pack cell naming a mutation absent
  from the JSON must be reported by whatever fills the table.
- **Status:** `uncontrolled` — the fill-from-record step is prose; the mutation table is generated,
  the counts are not.

## 5. What this note does not decide

### DC-161 — A container's disabled-state trigger only reaches ink that inherits it, and a template that sets ink locally, or carries no trigger at all, is invisible to a floor that never renders the state

- **Shape:** INV-0008's own register (§8) marked `MenuItem IsEnabled=False → DisabledTextBrush on
  the *item*` **"confirmed by structure"** for every menu header and item — read from the XAML,
  never rendered. INV-0008 phase 6 (X-1, the census reach) forced two real, already-composed
  `MenuItem`s disabled — a submenu command and a top-level header — and rendered what the
  structural read had not: two DIFFERENT sub-shapes of the same class. (a) `MenuItemSubmenuItem`'s
  gesture-chord `TextBlock` (`Foreground="{DynamicResource TextMutedBrush}"`, a LOCAL value on the
  glyph itself) never inherits the container's `IsEnabled=False` trigger, so "Ctrl+N" reads exactly
  as legible disabled as enabled. (b) `MenuItemTopLevelHeader`'s `ControlTemplate` carries no
  `IsEnabled=False` trigger at all — unlike its three sibling menu templates
  (`MenuItemTopLevelItem`, `MenuItemSubmenuItem`, `MenuItemSubmenuHeader`) — so a disabled
  top-level header (`_File` itself) keeps `TextBrush`. Both ratios still clear (6.83:1, 14.30:1) by
  coincidence; what is lost is the STATE distinction, not the floor.
- **Signature:** a menu (or any container) template with an `IsEnabled=False` trigger on some but
  not all of its sibling templates; a glyph inside such a template whose `Foreground` is set
  directly (not inherited) so a container trigger can never reach it; a register entry that says
  "confirmed by structure" for a state no test has ever rendered.
- **Instance (2026-09-12, INV-0008 phase 6, `tests/AiDe.App.ContrastProbe/ShellContrastCensus.cs`
  `ForceDisabledAndRemeasure`):** both sub-shapes above, found by forcing real `MenuItem`s (the
  File menu's first submenu command; the `_File` top-level header) disabled and re-walking the
  composed shell — never constructed on a throwaway window. `App.xaml` is outside this track's
  owned paths (`docs/coordination/addendum-cd.md` §2); **not fixed here**, routed to the Shell lane
  (App.xaml's owner) as a seam request. The steady-state reach measures the submenu command (not
  the top-level header, whose ONLY gap this class explains and which the reach does not force, to
  avoid asserting a fix this track cannot make); its one known exception (the gesture-chord text) is
  pinned by `ShellContrastCensusTests.TheDC154MenuInkGapIsNamedAndDoesNotWiden` so a THIRD site
  cannot silently join it.
- **Control:** the census reach itself (X-1) — a disabled state is now rendered, not read from the
  template; `ADisabledControlsInkIsTheDisabledToken`'s zero-tolerance assertion stays in force for
  everywhere except this one named, cited exception. **Proposed for the Shell lane:** add the
  missing `IsEnabled=False` trigger to `MenuItemTopLevelHeader`; move the gesture-chord `TextBlock`'s
  ink to a container-level pairing (a trigger on the template) instead of a local `Foreground`.
- **Status:** `partially-controlled` — both instances are measured, registered and pinned so
  neither can regress silently or widen unnoticed; the `App.xaml` fix is a seam request, not yet
  landed.
