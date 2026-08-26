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

**Status counts:** controlled 6 · partially-controlled 3 · uncontrolled 1
**Recurrence since last review:** 0

---

## Project classes

### DC-001 — A gate artifact is authored in a session and never actually committed
- **Signature:** a document is referenced by a typed link, appears in the change log, and exists on
  someone's disk — but `git ls-files` does not know it. Often caused by an ignore rule written for
  build output swallowing a docs path (`[Rr]elease/` ate `docs/release/`).
- **Why it survives:** the authoring session sees the file and its own index entry, so every
  in-session check passes. Only a fresh clone — or a reviewer — discovers the hole.
- **Instances:** 2026-08-25 — `docs/release/ai-native-ide-release-plan.md` (Release Engineer, soft
  veto); the same rule would have hidden any future `docs/release/*`. 2026-08-25 — `spikes/` was
  ignored by the pack default, so three "Verified" contracts cited evidence that was not in the repo
  (Test Architect, hard veto).
- **Control:** `docs-graph.py validate` reports dangling links and index drift and is run at every
  skill close; `.gitignore` now re-includes `docs/release/**` and no longer ignores `spikes/`, each
  with a comment naming this class. Observed failing 2026-08-25 (validate surfaced the dangling
  `release-plan-ai-native-ide` edge before the file was recovered).
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
- **Signature:** assertions that pass in isolation and fail in a full run, with counts higher than the
  test itself produced. Caused by process-global registries (`ActivityListener`, static caches,
  environment variables) shared across concurrently executing classes.
- **Why it survives:** each test is individually correct, and the failure looks like a flake.
- **Instances:** 2026-08-26 — `TelemetryTests` captured spans emitted by `WalkingSkeletonTests`
  running concurrently; `Assert.Single` saw two activities.
- **Control:** `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in
  `AiDe.Core.Tests/AssemblyInfo.cs`, with the reason stated inline. **This is a mitigation, not a
  detection** — nothing fails if a future suite reintroduces shared global state under parallelism.
- **Status:** `uncontrolled`

### DC-009 — An index exists, is correct, and the query path never uses it
- **Signature:** `EXPLAIN QUERY PLAN` on the *hand-written* SQL shows a clean index SEARCH, and the
  feature is still slow. The application layer loads a broad result set and filters it in memory, so
  the index is never on the path the product actually takes. Reviewing the schema — or the plan of a
  query the code does not issue — finds nothing wrong.
- **Why it survives:** every artefact inspected in isolation is correct. The schema review passes,
  the query-plan oracle passes, the unit tests pass on small fixtures where materializing everything
  is free. Only a benchmark on a realistic corpus separates "we have an index" from "we use it".
- **Instances:** 2026-08-26 — `ProjectionService` called `AllCurrentAssertions()` for every bounded
  read, paying ~350 ms of full-corpus materialization per query; `describe` measured p95 399 ms
  against a 100 ms budget while the indexes it should have used sat unused. Compounded by index
  column order: `(scope_id, generation, subject)` cannot serve a lookup whose only predicate is the
  node, so the traversal column has to lead.
- **Control:** `P1-PERF-02/03` measure the *product's own call path*, not hand-written SQL, and fail
  the run when a budget is missed; `P1-PERF-04` separately asserts no bounded read scans the fact
  table. **Observed failing 2026-08-26** (5 of 8 budgets red before the fix). The two oracles are
  deliberately independent: a query can be fast on this corpus and still be a scan that degrades
  linearly on a larger one.
- **Status:** `controlled`

### DC-010 — A benchmark's samples share state, so it measures accumulation instead of the operation
- **Signature:** a latency distribution that climbs monotonically across samples, and a p95 far above
  the p50 with no obvious cause. The harness reuses one store/table/cache across iterations, so
  sample N pays for the N−1 samples before it.
- **Why it survives:** the harness looks correct, the operation under test is real, and the number is
  reproducible — it is simply a number for a different question than the one asked. It reads as a
  performance problem in the product rather than a measurement problem in the harness.
- **Instances:** 2026-08-26 — the refresh benchmark reused one database for all 30 samples, so the
  final sample inserted the 30th 10,000-row batch into a 300,000-row table; refresh "regressed" to
  p95 1291 ms. Measured with an independent store per sample it is p95 221 ms.
- **Control:** `bench/AiDe.Bench` allocates a fresh store per refresh sample, and the append-only
  growth curve is measured *deliberately and separately* (0/5/10/20 prior generations) rather than
  leaking into the headline number. That separation is what turned a harness bug into the real
  finding underneath it: **refresh exceeds its budget after ~10 generations and no policy triggers
  the compaction that would mitigate it** — an open Phase-2 work item, not a closed one.
- **Status:** `partially-controlled` — the harness is fixed and the growth is now visible, but
  nothing yet enforces a generation-retention policy in the product.
