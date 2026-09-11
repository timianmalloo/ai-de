---
id: proof-conductor-front-door
title: "Proof Pack — Conductor front door, Phase 1 (F0–F5)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, session, composer, template, proof-pack, phase-1, exit-evidence, f5]
links:
  - { to: plan-conductor-front-door, rel: tested-by }
  - { to: spec-conductor, rel: tested-by }
  - { to: note-front-door-residuals, rel: depends-on }
  - { to: adr-0029-latency-slo-recorded-not-asserted, rel: depends-on }
  - { to: proof-conductor-agent-plane, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Exit evidence for the Phase-1 front-door slice: a session created from File → New Session with a
  machine-checkable origin, a prompt composed in the rich composer, a governed run launched through
  the one composition root and streamed in Console mode, scored into a comparable cell, with zero
  terminal hosting asserted as a counter that is shown going to one. Nine clauses, each against an
  oracle committed before the run, with the two qualifications the slice carries rather than
  discovers.
---

# Proof Pack: Conductor front door, Phase 1

> **This file was committed before the exit episode closed, and that ordering is load-bearing —
> twice over.**
>
> **Once for scoring.** `ClosedEpisodeScoring.EvidenceFor` asks `ProofPackVerifier` whether each
> declared artifact is a real file; an episode declaring none derives no verification path and scores
> **Not Scored**. So the pack is opened with its run-dependent rows marked *run pending*, the run
> declares it, and those rows are completed afterwards. A pack written after the close credits
> nothing.
>
> **Once for the clauses.** §F5's opening sentence is about the order of two acts, not their
> content: *"the oracle is committed BEFORE the run and its SHA cited — seven points written after
> seeing the run are a description, not a test."* The oracle is
> `tools/verify-front-door-exit-evidence.py`, and it checks that ordering **about itself**: it reads
> its own commit, compares it to the run's start, and compares its own bytes at that commit to the
> bytes now running. An oracle quietly widened after the run reddens rather than passing.

| | |
| --- | --- |
| **Oracle** | `tools/verify-front-door-exit-evidence.py` — clause 0 plus the nine |
| **Oracle commit** | `ORACLE-SHA-PENDING` |
| **Run started** | `RUN-PENDING` |
| **Ordering evidence** | `RUN-PENDING` — `git log` showing the oracle commit before the run's first artifact |
| **Falsifier suite** | `TheSessionOriginIsSetOnlyOnTheCommandPathTests`, `TerminalHostingLedgerTests`, `TheRunHasOneCompositionRootTests` |

- **Component:** `src/AiDe.Core/Sessions/` (session container, path contract, template spine),
  `src/AiDe.Core/Presentation/Sessions/` and `…/Composer/` (the sheet, the session document, the
  console stream, the composer's form engine and compiler), `src/AiDe.App/Workbench/Sessions/` and
  `…/Composer/` (the File-menu front door, the paired-zone document, the canvas modes, the composer
  surface and its host-owned send), and `src/AiDe.App/Conductor/` (the composition root, the
  composition-root ledger, and the session document's call into it).
- **Spec:** R13, R14, R15, R16, R18, R19 under the cuts in
  `docs/plans/conductor-front-door.md` and Rulings 13–46.

## The nine clauses

Each row is one §F5 clause. **Residual** names a measurement or an explicit uncovered input for
every row — never "none", which is the shape this clause exists to refuse: *"populated" is satisfied
by "none" in every cell*.

| # | Clause | Evidence | Oracle (why it can fail) | Red observed | Confidence | Residual |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | **Started from `File → New Session`, machine-checkably** — `session.open` carries an `origin` set only on the `Ctrl+N` / `MainMenuBuilder` path | `TheSessionOriginIsSetOnlyOnTheCommandPathTests` (3 cases) + the exit run's own `session-events.jsonl` | The **companion**: a session constructed directly reads `direct`. A field that is always the same value distinguishes nothing | **Yes**, both halves — see *Falsifiers observed red* | Verified | **Named uncovered input:** the scan bounds the front-door value to two files in `src/`; it cannot see a *third* production path that reaches `SessionConfigStore.Create` and simply leaves `origin` at its default. That path would read `direct` and be indistinguishable from a test — the guard is over *claiming* the front door, not over *reaching* the store |
| 2 | **Composed in the composer, streamed in Console mode** | `RUN-PENDING` | The bytes sent are the bytes the composer rendered (digest equality), the active canvas mode is `console`, and the console rendered ≥ 1 row | `RUN-PENDING` | `RUN-PENDING` | **Measured:** `terminalModeBuilt == false` over the run. Ruling 45's exposure is real but untaken — see the Ruling 45 residual below |
| 3 | **Scored end-to-end**, cell `IsComparable == true`, read from `scored_episode_cell` | `RUN-PENDING` | `scored`, `segmentIsComparable`, a null `incomparableReason`, and an actual store row — a verdict with no row is a claim | `RUN-PENDING` | `RUN-PENDING` | **Named uncovered input:** one cell, one task class, one engine. Comparability across cohorts is not exercised, and `LaneCohort`'s axes are proven by unit test rather than by a second live run |
| 4 | **`terminalHostConstructions == 0`**, with N7's companion falsifier carried forward verbatim | `RUN-PENDING` for the zero; `TerminalHostingLedgerTests.AnOpenLedgerCountsARealTerminalHostConstruction` for the one | A counter nothing increments reads 0 forever. The same counter is shown reading **1** against a real ConPTY | **Yes** — the run's own predicate applied to a run that did host a terminal | Verified (falsifier) | **Measured:** the ledger counts the *attempt*, opened before interop, so a failed construction still counts. **Named uncovered input:** a terminal hosted by a *child process* of this one emits on that process's `ActivitySource` and is invisible to this ledger |
| 5 | **Launched through the same composition root `GovernedRunHost` uses** — no second entry point (Ruling 13), asserted by a ledger counting roots | `RUN-PENDING` | `Roots == 1`, the launch site is under `src/`, and exactly 2 sites construct a `GovernedRunRequest` (C16) | `RUN-PENDING` | `RUN-PENDING` | **Named uncovered input:** the ledger counts roots *entered in this process*. A second entry point in a **separate process** is outside its window, and C16's two-site cap is a source scan, not a runtime one |
| 6 | **Recorded measurement**: event count, p50/p95, **host named** — recorded per ADR-0029, never asserted | `RUN-PENDING` | Presence, a named host, and an absent measurement reading `not recorded` rather than `0`. **No comparison to a constant** — that is DC-107 | `RUN-PENDING` | Recorded, not asserted | **Measured and bounded:** the figures measure *in-process normalization only*, on one host, on one run. They are **no evidence** about a loaded machine or a lane whose events cross a process boundary — ADR-0029 says so in its own consequences |
| 7 | **Proof Pack** at `docs/proof/conductor-front-door.md`, every Residual cell naming a measurement or an explicit uncovered input | This file, checked by `clause7_proof_pack_residuals` | Any Residual cell reading `none` / `n/a` / `—` / empty | **Yes** — the oracle's self-test reddens on a `none` cell and does not fire on a non-Residual column | Verified | **Named uncovered input:** the check is *shape*, not *truth*. A cell reading "measured: 4 of 599" passes whether or not anything was measured. Only a reader can close that, and this row says so rather than implying the gate did |
| 8 | **The R13 b2 qualification** (Ruling 18): only `claude-code` was exercised; codex and copilot are refused by N2's own test — **stated, not stubbed** | `EngineCatalogTests.ANonAdapterModeIsRefusedWithANamedReason` (copilot), `EngineCatalogTests.AnAdapterWithNoObservedEntryModuleIsRefusedRatherThanGuessed` (codex) | The refusal tests must exist and the run must record exactly `["claude-code"]` as exercised | Carried from N7 | Verified | **Named uncovered input:** two of three catalogued engines have **never been run**. The catalog rows for them are pinned by unit test against `EngineCatalog`, not against either adapter |
| 9 | **DC-115** — the repository-root shape is carried, never silently | `RUN-PENDING` | Under `clone`, a written qualification is required. Under `linked-worktree`, a `Not Scored` verdict is **Ruling 17's EvaluatorIntegrity trip to the human**, not a carried qualification | `RUN-PENDING` | `RUN-PENDING` | **Named uncovered inputs, three, from the register:** a lane whose tree was **released** before the sweep (branch keeps the commit, tree is gone, verdict falls back to the parent's honest `NotFound`); the `Unverifiable` collapse at `EpisodeEvidence`; and the **two-doors asymmetry** — an audit-imported episode is evidenced by *naming* a path, a governed one must have the file present |

## Falsifiers observed red

**A falsifier that has never been seen failing is a claim about a falsifier.** Both of these were run
against the shape they exist to catch, before the commit that shipped them, and the output is
transcribed rather than summarised.

### Clause 1 — the companion, with `origin` made a constant

The defect: `SessionConfigStore.Create`'s `origin` default flipped from `SessionOrigins.Direct` to
`SessionOrigins.MainMenuNewSession`, so *every* session claims the front door.

```
Failed AiDe.Core.Tests.Sessions.TheSessionOriginIsSetOnlyOnTheCommandPathTests.ASessionConstructedDirectlyReadsTheOtherValue [7 ms]
  Error Message:
   Assert.Equal() Failure: Strings differ
           ↓ (pos 0)
Expected: "direct"
Actual:   "main-menu.new-session"
           ↑ (pos 0)
```

### Clause 1 — the scan, with a second site added

The defect: a second file under `src/` naming `SessionOrigins.MainMenuNewSession`.

```
Failed AiDe.Core.Tests.Sessions.TheSessionOriginIsSetOnlyOnTheCommandPathTests.ExactlyOneSiteInSrcCanStampTheFrontDoorOrigin [37 ms]
  Error Message:
   Assert.Equal() Failure: Collections differ
Expected: OrderedIterator<string, string> ["NewSessionSheetViewModel.cs", "SessionConfig.cs"]
Actual:   List<string>                    ["NewSessionSheetViewModel.cs", "SessionConfig.cs", "SessionConfigStore.cs", "SessionPaths.cs"]
                                                                                              ↑ (pos 2)
```

### Clause 4 — the exit run's own predicate, applied to a run that hosted a terminal

The defect is not in the product: this is **clause 4's assertion** (`terminalHostConstructions == 0`)
evaluated against `TerminalHostingLedgerTests.AnOpenLedgerCountsARealTerminalHostConstruction`, which
constructs a **real ConPTY**. The counter that the exit run reports as `0` is the same counter.

```
Failed AiDe.Core.Tests.AgentPlane.TerminalHostingLedgerTests.AnOpenLedgerCountsARealTerminalHostConstruction [7 ms]
  Error Message:
   Assert.Equal() Failure: Values differ
Expected: 0
Actual:   1
```

That is the whole of *"a counter nothing increments reads 0 forever"*, discharged: the counter
increments, for the case it exists to catch, on this machine, today.

### Clause 5 — the root ledger

`RUN-PENDING` — the ledger and its two falsifiers are built by node F4b (Ruling 46) and measured
here. Both directions are required: a `RealLane` driven with **no root** shows `Roots == 0` while
events flow, and two `RunAsync` calls that fail fast on an unknown `engineId` still show `Roots == 2`,
because the root activity is opened **before** anything that can throw and therefore counts the
attempt rather than the success.

## The recorded measurement

**Recorded, never asserted** (ADR-0029, DC-107). No figure below is compared to a constant anywhere
in the oracle, in a test, or in CI.

| Measurement | Value | Host |
| --- | --- | --- |
| Events observed | `RUN-PENDING` | `RUN-PENDING` |
| Latencies measured | `RUN-PENDING` | — |
| Normalization p50 | `RUN-PENDING` | `RUN-PENDING` |
| Normalization p95 | `RUN-PENDING` | `RUN-PENDING` |
| Wall clock | `RUN-PENDING` | `RUN-PENDING` |

## Qualifications carried, not discovered

### R13 b2 (Ruling 18) — one engine, and the other two refused by test

**Only `claude-code` was exercised.** `codex` and `copilot` are **refused by N2's own tests**, which
is a different and stronger statement than "not tried":

- `copilot` declares `acp: native` and `EngineCatalog.ResolveLaunch` refuses a non-adapter mode with
  a named reason — `ANonAdapterModeIsRefusedWithANamedReason`.
- `codex` declares `acp: adapter`, so the mode gate alone would let it through; it is refused because
  no adapter entry module is observed rather than guessed —
  `AnAdapterWithNoObservedEntryModuleIsRefusedRatherThanGuessed`.

**Neither is stubbed.** There is no placeholder launch path for either, and the sheet's live backend
list is empty for a separate reason recorded below.

### DC-115 — the shape the run rooted in

`RUN-PENDING`.

**What is already settled, before the run:** DC-115 is no longer where Phase 1 left it. Phase 2 N0
landed `ProofPackVerifier.VerifyInCheckouts` and `ClosedEpisodeScoring.CheckoutsOf`, so **a governed
lane in a live linked worktree is now credited for evidence committed on its own branch** — a
control observed failing first, on the un-fixed shape, with `Not Scored — no minimum verification
path`. Phase 1's exit run avoided the defect by rooting in a **clone**; this run does not have to.

**Ruling 17 is pre-committed here rather than decided afterwards:** under the linked-worktree shape a
`Not Scored` verdict is an **EvaluatorIntegrity trip to the human**, not a qualification that may be
written into a Residual cell. The oracle enforces it (`clause9_dc115`), so the softer answer is not
available to whoever fills this section in.

**The class stays `partially-controlled` either way**, and its three open residues are named in
clause 9's Residual cell.

## Residual

Assembled from `docs/notes/front-door-residuals-carried.md`, which was written **as the slice
accumulated them** rather than reconstructed here — reconstructing the list at close time is how a
cell comes to read "none". Every entry states its **kind**: **measured** (a number exists), **named**
(the uncovered input is stated, no number), or **owned** (deferred to a named phase or persona).

### Unmet in this slice, stated rather than stubbed

| Residual | Kind | Detail |
| --- | --- | --- |
| **Shape badges do not render in the Score** | named | F4 discharged every other §F4 clause and named this one unmet. The shape is not carried onto the episode and no Score surface renders it; it needs the shape to travel the scoring path, which F4 does not touch. **Not built, not stubbed.** |
| **Ruling 35's "scoped to the template loader" has a file-scan guard, not a type guard** | named | `TheDependencyIsScopedToTheTemplateLoader` asserts the package token appears in exactly one file under `src/`. A type-reference guard was deferred by Ruling 36 and then found already partly closed by that test. |
| **`.mcp.json`'s `env` round-trip** | owned | Security's M1–M5 landed the ACL and parse fixes; the structural answer — AI-DE's machine-local entry in the per-user client config, so the credential file is never opened — is per-client and deferred as a discovery-strategy question. |

### Controls named and not built

| Residual | Kind | Detail |
| --- | --- | --- |
| **DC-123's out-of-host process oracle** | named | The leak is structurally unobservable from inside the harness that leaks it. The shipped oracle is a leftover-report *fingerprint*; a helper that survives while its report is deleted would pass. The process-diff oracle, scoped to one worktree, is designed and unbuilt. |
| **DC-118 half (b) has no lint** | named | *Every scan-shaped guard states its root, recursion, token set and allowlist* is mechanically checkable — one sentence against one `EnumerateFiles` call — and nothing checks it. This slice added a fourth such guard (`ExactlyOneSiteInSrcCanStampTheFrontDoorOrigin`) that states its shape **by hand**, which is the debt growing by one while the lint stays unbuilt. Half (a) is not evaluable before a join, by construction. |
| **DC-119's read-back is procedural** | measured | `main` sat red at `7c30c29` for three commits. The failing gate's own message was the sentence the conductor had written into another node's brief an hour earlier. The mechanical half — `verify-test-run.py` **naming** the tests it counts — is the cheaper control and is unbuilt. |
| **The containment gate's narrow→wide ratchet** | measured | The shipped gate covers **4 sites** against **599** `StringComparison` literals in `src/`, with zero false positives. Widening to any path-named receiver reaches **23** and needs a waiver mechanism. Deliberately not taken. |
| **Six sibling `File.Move(..., overwrite: true)` sites** | named | `StoreCompactor.cs:186-187`, `DaemonInstallation.cs:116`, `MigrationJournal.cs:136`, `BoardPublisher.cs:99`, `RegistrationPublisher.cs:84`, `StandingPublisher.cs:119`. Identical ACL/mode-replacement mechanism; all write AI-DE's own artifacts, so disclosure severity is lower. None clean up on failure. |
| **Three App-probe spawners with no job object** | named | `CanvasFocusIntegrationTests.cs:96`, `TerminalGuiHostTests.cs:55`, `WebAssetHostIntegrationTests.cs:31` — all rely on `using`, which DC-123 names as the mistake. A shared launcher helper was rejected as a refactor with no failing signal driving it. |

### Coverage gaps, measured

| Residual | Kind | Detail |
| --- | --- | --- |
| **The CI matrix never runs portable tests on Windows** | measured | The Windows `build` job runs Core with `--filter Platform=Windows` only, so the portable suite executes on Linux in CI and on Windows **only locally**. Observed directly: on one red-first push the Windows job was green while Linux was red, for a defect that reproduces on Windows. Closing it roughly doubles Core CI time on the pricier runner — a CE-series cost decision the SRE owns, recorded so it is chosen rather than defaulted. |
| **The Proof Pack capture channel is unset in every session** | measured | `AIDE_CONTRACT_LOG` and `AIDE_SESSION` are UNSET in the conductor's session and in every agent session, so **no node in this slice could write an `episode-close` line** — this one included. `verify-capture-instruction` is green because it checks the instruction *text* is present in each harness root; it verifies the mandate is **documented**, not that it is **followable**. `AGENTS.md` records the consequence in its own words: *"111 episodes, 1 observation."* |
| **`verify-surface-ownership` is non-recursive** | measured | It iterates `src/AiDe.App/Workbench` top-directory only, so everything under `Workbench/Sessions/` and `Workbench/Composer/` sits outside its scan. F2 and F4 each assigned their surfaces by hand in `session-contracts.md` §2 and said so. Widening a shared control was refused as a unilateral act. |

### Accepted by the human or the Owner, with their triggers

| Residual | Kind | Detail |
| --- | --- | --- |
| **Non-edit tool calls are auto-allowed; the permission banner is Dismiss-only** | owned | **Ruling 44.** Containment *for edits* is the lease inside the worktree; for every other tool kind it is one human read, and nothing else — `cd ..`, absolute paths, network and push are all reachable, and the spec's own `network: deny, push: deny` is **not implemented in `Decide`**. **Void trigger:** the moment any text reaches the prompt the human did not personally read. |
| **C21 is a default, not an enforceable policy** | owned | `SessionConfig` is per-session and operator-writable. The human chose off-by-default for Phase 1 with the gap stated. A deployment that must *prevent* attach needs a non-session-overridable layer — **Phase 2**, C21(f) is its trigger. |
| **`~/.claude/projects/` holds plaintext transcripts, one per session AND one per subagent** | measured | 30-day default (`cleanupPeriodDays`), outside `.aide/`, outside `.gitignore`, outside the product's deletion reach. Filed in the provider record as the parent review's *"incomplete result for repository-owned or external copies"*. |
| **The API-key exception needs its own provider record** | owned | A second egress class with different third-party terms. One record cannot cover both. |
| **C14(e)(iv)'s refusal set is incomplete by construction** | named | *"Must never be described as 'secrets cannot be attached.'"* It grew by a **class** — in-repo config files carrying third-party credentials — after `.mcp.json` showed the list's organising idea had a blind spot. |
| **The repository's third-party identifiers are unenumerated** | named | The provider record's third-party narrowing is stated rather than measured. |
| **R13 b2's live gap: the sheet cannot list backends in the running app** | named | `providers.yaml` has **no reader anywhere in the repository**, and Ruling 35 refuses a third hand-rolled one. The sheet renders an honest empty state. The oracle is fully discharged against a populated registry in test; `ProviderRegistry` is constructed from in-code rows at `GovernedRunHost.cs:69`, never from a file. |

### Found by this node

| Residual | Kind | Detail |
| --- | --- | --- |
| **Ruling 45's Terminal-mode exposure, and the race in any naive check of it** | measured | `CanvasModeCatalog.cs:64` constructs `new TerminalSurface(...)` for the Terminal canvas mode, and `TerminalSurface.cs:62` — the constructor — is `_ = StartAsync(sessionId, columns, rows);`, reaching `ConPtyTerminalSession.StartAsync` at `:426`. **Building that mode constructs a real terminal host**, so a run that opened the canvas split would have incremented the counter clause 4 asserts is zero. This run did not: `terminalModeBuilt == false` is recorded, not assumed. **And the sharper half:** the start is *fire-and-forget*, so a ledger read taken immediately after construction **races the increment in the direction that passes** — the next person to write that assertion needs to know it before they write it, not after. |
| **A seam between two adjacent nodes that no clause assigned** | named | **Ruling 46's class.** `ComposerSurface.Send()` built a `GovernedRunRequest` and returned it to a discarding caller; `ConductorEntry.cs:80` was the only call to `GovernedRunHost.RunAsync` in the product; and `AcpEventQueue`'s channel is `SingleReader = true`, so no `SessionLane` could drain what the host drains. F4 built one end, F2 built the other, and **F2 wrote the conflict down in `SessionLane.cs:12-16` while no clause resolved it.** This is a *plan* defect, not a code one: adjacent nodes each built one end of a seam nobody owned. The control is a slice-level **E7 surface list with every edge assigned to a node**, failing plan review when an edge is unowned — named here, and **not built by this node**. |
| **Clause 5's ledger is built by the node that does not grade it** | named | The composition-root ledger was drafted here and **handed to F4b** under Ruling 46's own principle applied one level down: *the node that builds the counter must not be the node whose Proof Pack the counter proves.* Recorded because the reasoning is the reusable part, and because the hand-off is the kind of thing that looks like lost work in a log unless it says why. |
| **The exit run is one run** | named | Every run-dependent row above rests on a single live execution on one host with one engine, one account and one task class. Nothing here is a distribution, a rate, or a claim about a second machine. The re-runnable half is the test suite; this pack's live half is a record, in the same sense the ACP frame corpus is. |

### One process residual, recorded because it shaped everything above

**Ten or more conductor claims were refuted by the nodes they were given to**, each on evidence: a
stale file citation, a lease provenance false against code and spec, a test idiom that does not
exist, an oracle that passes against unfixed code, a "declared security boundary" whose declaration
measurement disproved, a dead-code cleanup that would have broken the build, a count that had
propagated from a ruling into a plan into a brief, a red `main` nobody read back, and — in this node
— a brief whose DC-115 premise was two rulings out of date.

**What caught every one of them was not a control.** It was the standing line in each brief asking
the node to report anything false, and nodes treating that as an obligation rather than a courtesy.
That is a **procedural** control with no mechanical backing, and it belongs here as exactly that.

## Counts and gates

| | |
| --- | --- |
| `AiDe.App.Tests` | `RUN-PENDING` (floor 495) |
| `AiDe.Core.Tests` | `RUN-PENDING` (floor 2206) |
| `AiDe.Core.Tests` portable | `RUN-PENDING` (floor 2052) |
| `AiDe.Core.Tests` non-portable | `RUN-PENDING` (floor 154) |
| Full gate set, bare | `RUN-PENDING` |
| CI conclusion | `RUN-PENDING` |

**Baselines were not raised by this node.** `tools/expected-test-counts.json` is untouched and
`verify-test-run.py --update` was not run: a node raising its own floor is grading its own work, and
the bump is the conductor's separate recorded act.
