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
| **Oracle commit** | `1374401d171b1ec6be5d56c25b1d1e00608abc18`, committed `2026-09-11T06:36:24-07:00` (`%ct` 1789133784) |
| **Oracle integrity** | **Held across three merges of `main`** — `39bcf288`, `be68ca1c`, `4b9dd779`. `git diff 1374401d HEAD -- tools/verify-front-door-exit-evidence.py` is **empty** after each — the bytes running are the bytes committed. The branch was merged, never rebased, every time (DC-128) |
| **Oracle reachability** | **Pinned by the annotated tag `f5-oracle-frozen`**, pushed. `git rev-parse f5-oracle-frozen^{commit}` → `1374401d171b1ec6be5d56c25b1d1e00608abc18`, and the oracle's bytes at the tag are identical to the bytes running. **Not decoration.** The commit was reachable *only* from `feature/exit-evidence`; a squash-merge, a rebase, or deleting the branch after merge would have left it unreferenced and, at the next `gc`, gone — and clause 0 would then be **unverifiable rather than false, which is worse**, because a control that cannot run reads like one that passed. A tag is a ref, refs make objects reachable, and `gc` never collects what is reachable, so clause 0 no longer depends on anyone remembering a merge strategy. **This repository had no tags at all before this one.** DC-128 covers *"a cited commit proves existence, not immutability"*; this covers the half DC-128 does not — that the cited commit is still **there** to compare against |
| **Run started** | `RUN-PENDING` — awaiting the operator's `File → New Session` gesture under **Ruling 49**. See *The exit run did not happen*, Part 3 |
| **Ordering evidence** | Vacuous but not absent: the oracle predates any run because **there is no run yet**. Clause 0's byte comparison is the half that carries weight without one, and it holds |
| **Blind spot, closed from outside** | Clauses 2 and 5 cannot tell a product-wired composer from a harness-wired one. **The frozen oracle was not widened** — `TheRunBindingComesFromTheProviderFileTests.TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate` asserts exactly one `composer.Configure(` in the shell, which settles it from outside clause 0's pinned bytes |
| **Falsifier suite** | `TheSessionOriginIsSetOnlyOnTheCommandPathTests`, `TerminalHostingLedgerTests`, `TheOneCompositionRootIsCountedTests` — each resolved to a declaration under `tests/`, not quoted from memory. The third read `TheRunHasOneCompositionRootTests` until this close, and **no class of that name has ever existed** |

- **Component:** `src/AiDe.Core/Sessions/` (session container, path contract, template spine),
  `src/AiDe.Core/Presentation/Sessions/` and `…/Composer/` (the sheet, the session document, the
  console stream, the composer's form engine and compiler), `src/AiDe.App/Workbench/Sessions/` and
  `…/Composer/` (the File-menu front door, the paired-zone document, the canvas modes, the composer
  surface and its host-owned send), and `src/AiDe.App/Conductor/` (the composition root, the
  composition-root ledger, and the session document's call into it).
- **Spec:** R13, R14, R15, R16, R18, R19 under the cuts in
  `docs/plans/conductor-front-door.md` and Rulings 13–49 (49 is the ruling that the exit run is triggered by the operator's own gesture and no headless entry point is built).

## The nine clauses

Each row is one §F5 clause. **Residual** names a measurement or an explicit uncovered input for
every row — never "none", which is the shape this clause exists to refuse: *"populated" is satisfied
by "none" in every cell*.

**Clauses 1, 4, 7 and 8 are discharged. Clauses 2, 3, 5, 6 and 9 read `RUN-PENDING`** — and the reason
has changed twice, which is why each state is written down rather than left to be assumed. Until
`be68ca1c` the product **could not** send from the front door at all; F6 closed that; and under
**Ruling 49** what remains is a single input no code can supply — **the operator's own
`File → New Session` gesture**. All three states, with the observations behind them, are in *The exit
run did not happen* below (Parts 1, 2, 3). Read it before quoting any `RUN-PENDING` cell as merely
"not yet done", and note that **Part 1's blocker is no longer true of this tree**.

**If that gesture is not performed, clause 1 is `NOT DEMONSTRATED` and the exit is `NOT MET`.** No
row in this pack should be read as evidence the front door works until one records a gesture.

| # | Clause | Evidence | Oracle (why it can fail) | Red observed | Confidence | Residual |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | **Started from `File → New Session`, machine-checkably** — `session.open` carries an `origin` set only on the `Ctrl+N` / `MainMenuBuilder` path | `TheSessionOriginIsSetOnlyOnTheCommandPathTests` (3 cases) + the exit run's own `session-events.jsonl` | The **companion**: a session constructed directly reads `direct`. A field that is always the same value distinguishes nothing | **Yes**, both halves — see *Falsifiers observed red* | Verified | **Named uncovered input:** the scan bounds the front-door value to two files in `src/`; it cannot see a *third* production path that reaches `SessionConfigStore.Create` and simply leaves `origin` at its default. That path would read `direct` and be indistinguishable from a test — the guard is over *claiming* the front door, not over *reaching* the store |
| 2 | **Composed in the composer, streamed in Console mode** | `RUN-PENDING` | The bytes sent are the bytes the composer rendered (digest equality), the active canvas mode is `console`, and the console rendered ≥ 1 row | `RUN-PENDING` | `RUN-PENDING` | **Measured:** `terminalModeBuilt == false` over the run. Ruling 45's exposure is real but untaken — see the Ruling 45 residual below |
| 3 | **Scored end-to-end**, cell `IsComparable == true`, read from `scored_episode_cell` | `RUN-PENDING` | `scored`, `segmentIsComparable`, a null `incomparableReason`, and an actual store row — a verdict with no row is a claim | `RUN-PENDING` | `RUN-PENDING` | **Named uncovered input:** one cell, one task class, one engine. Comparability across cohorts is not exercised, and `LaneCohort`'s axes are proven by unit test rather than by a second live run |
| 4 | **`terminalHostConstructions == 0`**, with N7's companion falsifier carried forward verbatim | `RUN-PENDING` for the zero; `TerminalHostingLedgerTests.AnOpenLedgerCountsARealTerminalHostConstruction` for the one | A counter nothing increments reads 0 forever. The same counter is shown reading **1** against a real ConPTY | **Yes** — the run's own predicate applied to a run that did host a terminal | Verified (falsifier) | **Measured:** the ledger counts the *attempt*, opened before interop, so a failed construction still counts. It is **activity-shaped, not name-shaped** — an `ActivityListener` on `aide.terminal.runtime` counting `terminal.start` — so it does not share the blind spot that made a process census match on names and report zero AI-DE hosts wrongly. **Measured uncovered input, no longer hypothetical:** a terminal hosted by a *child process* emits on that process's `ActivitySource` and is invisible here. Observed instance: a full `verify-test-run.py` creates **~42 ConPTY conhosts and ~21 `msedgewebview2`** and reaps all of them, while each build leaves exactly **one** orphaned `VBCSCompiler.exe` holding a conhost — **none of which this ledger can see**. For *this* run the exposure is bounded by reading both spawn sites: `GovernedRunHost` starts only `AcpEngineProcess` (node) and `WorktreeProvisioner` (git/coord), and **both set `CreateNoWindow = true`, `UseShellExecute = false` with all streams redirected**; no daemon is on this path |
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

**Measured before the run, on this machine, and it is an argument rather than a result.** A bounded pre-flight through the `--conduct` entry (audit `al-01M28BN2WPZKCAE3VT8228QY5H`, task class `front-door-preflight` in its own store, tagged `not-exit-evidence`) rooted a governed run in **this linked worktree**. Its `scored_episode_cell` row reads `Partial: 15 / 15 observed`, `mode=governed`, `IsComparable == true`, `IncomparableReason == null` — and the row's `workspace` column reads `c:\projects\ai-de`, the **parent** repository, which does not contain this file in its working tree **or** in its `HEAD` (`9f01fdc`), checked both ways. The parent therefore *cannot* have supplied the declared artifact, so it was credited from the lane's own checkout: DC-115's Phase-2 control observed doing its job, by elimination of the only other source, rather than inferred from the register saying it should.

**What that pre-flight does not cover, so its green is not over-quoted.** It launched through `--conduct`, **not** the front door, so it is no evidence for clause 2 or clause 5 — those are precisely the path it does not touch. It is evidence about the plumbing, not a verdict, and **clause 9 is discharged by the exit run below, not by it**. Two runs occurred rather than one: `& <winexe>` returns immediately because the GUI subsystem detaches, so a first invocation reported no exit code while a real run continued in the background. Both scored; both lane trees were read, removed and pruned; the main clone's merge drivers were re-read afterwards and are unchanged.

**Ruling 17 is pre-committed here rather than decided afterwards:** under the linked-worktree shape a
`Not Scored` verdict is an **EvaluatorIntegrity trip to the human**, not a qualification that may be
written into a Residual cell. The oracle enforces it (`clause9_dc115`), so the softer answer is not
available to whoever fills this section in.

**The class stays `partially-controlled` either way**, and its three open residues are named in
clause 9's Residual cell.

## The exit run did not happen — first because the product could not send, then for two other reasons

> **Status, read this before the evidence below.** The structural blocker this section records **has
> been closed by node F6** and is **no longer true of the tree**. It is kept in full because deleting
> it would delete the measurement that caused the repair. Two parts: what was measured while it was
> true, then what closed it and what is still open.

### Part 1 — what was measured, while the product could not send

**Measured at `50b2364f`, before F6 merged.** Five clauses — 2, 3, 5, 6 and 9 — were unsatisfiable
because the shipped product could not send from the front door. Not "was not exercised": *cannot*.
The finding was structural and total, and every part of it was an observation of the tree at that
commit rather than an inference from a plan.

**What was checked, and how.**

| Observation | How it was taken | Result |
| --- | --- | --- |
| `ComposerSurface.Send()` refuses with no context | read `ComposerSurface.cs:222-229` | first statement is `if (_context is null) { _status.Text = "the composer is not wired to a session yet"; return null; }` |
| `_context` is set only by `ComposerSurface.Configure` | read `ComposerSurface.cs:154-181` | one assignment, `_context = context`, at `:166` |
| Nothing in `src/` calls `Configure` on a composer | `grep -rnE "Composer\s*\.\s*Configure\|\.Configure\s*\(" src` | one hit, `WorkbenchShell.cs:1552`, and it is `draft.Configure(` on `PromptDraftSurface` — a different type |
| Nothing in `src/` constructs a `ComposerSendContext` | `grep -rn "ComposerSendContext" src` | **five** hits, and none is a construction: the record's declaration (`ComposerSendGate.cs:23`), a `<see cref>` in its own doc comment (`:47`), a parameter (`:118`), a field (`ComposerSurface.cs:53`), a parameter (`:156`). `grep -rn "new ComposerSendContext" src` returns **zero** |
| The only construction anywhere is a test | `grep -rn "new ComposerSendContext" tests` | `ASendLaunchesAGovernedRunTests.cs:79` |
| The front door reaches the document but not the composer | read `MainWindow.xaml.cs:144-165` → `WorkbenchShell.cs:2909-2945` | `OpenSessionDocument` builds the model, the store and the `SessionDocumentSurface`, and never touches `document.Composer` |

**A type whose only construction in the entire repository is inside a test cannot be supplied at
runtime.** That is what makes this conclusive from structure alone, with no run needed to confirm it:
the absence is total rather than conditional.

**And the gap is two deep, which matters for sizing the repair.** Wiring `Configure` would not by
itself make the front door send, because three of the context's fields have no source in the running
app either:

| Field | Source in the running app | Observation |
| --- | --- | --- |
| `TaskClass` | none | built by the sheet, dropped at `MainWindow.xaml.cs:160`; the reopen path never had one |
| `Model`, `AccountLabel`, `Providers` | none | `MainWindow.xaml.cs:151` constructs `new ProviderRegistry([])` — **empty** — and `:139`'s own comment states *"`providers.yaml` has no reader in this repository"*. `GovernedRunHost.cs:90` builds its registry from `request.Providers`, which only the `--conduct` run file supplies |

The second row is **not a new finding** — it is the *R13 b2 live gap* already carried in
`front-door-residuals-carried.md`. What is new is its consequence: **it was never only a sheet-display
limitation.** The same missing reader is what leaves the composer's send context unpopulatable, so
**§F5 clause 2 was not achievable in this slice as decomposed**, independently of the unwired edge.
That connection existed in the residual list and in the clause list and was made by neither.

**Why this is DC-130 again rather than a missing feature.** The signature the register names is *a
constructed value returned to a discarding caller*. `MainWindow.xaml.cs:160` is
`Shell.Announcer.Announce(Shell.OpenSessionDocument(created.Config))` — and `created` is a
`NewSessionResult(Config, TaskClass, RoutableBackends)`. **`TaskClass` and `RoutableBackends` are
constructed by the sheet and dropped on that line.** `TaskClass` is the field the sheet refuses to
default, because a defaulted class ranks in the wrong cohort (DC-110, Ruling 19) — so the one value
the composer's send context most needs is built, validated, and discarded one call before the
composer would read it. F4b closed the seam *below* this one (request → run); the seam *above* it
(session config + task class → composer send context) is still unowned. **Both nodes are green
against every clause they were given.**

`OpenSessionDocument`'s own doc-comment records the conflict without resolving it — *"the result's
task class and lease belong to a run — passing them here would force the reopen path to invent
both"* — which is DC-130's other tell verbatim: **one of the nodes writes the conflict down in a
code comment and no clause resolves it.**

**Why this node did not simply wire it.** Two reasons, and the second is the stronger.

1. **It is not this node's edge.** Filling it requires deciding where a reopened session's task class
   comes from, which the comment above shows was a deliberate deferral, not an oversight. That is a
   conductor or Owner ruling about the slice's decomposition, not an evidence node's call.
2. **A harness-wired run would have passed this pack's own oracle, and that is the worst available
   outcome.** A driver *can* call `Composer.Configure(...)` itself and then press `Send()`; the run
   would be real, the episode would score, and the oracle would read `composerSendCount == 1` and
   `launchedBy == src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs` — **both true, and both
   green.** The product would still not have a working front door. That is DC-127 exactly — *the
   fixture agreed with me* — manufactured deliberately, in the pack whose job is to refuse it.
   `GovernedRunHost`'s own remarks already refuse a hand-assembled harness as exit evidence, and the
   send context is precisely the wiring that would have been hand-assembled.

**The oracle cannot see this, and clause 0 is why it was not widened.** Clauses 2 and 5 between them
say *the request was built in the composer* and *the run launched from product code*. Neither asks
**who wired the composer**, so neither can separate "the product supplied the send context" from "a
harness did". The fix is a source scan of the shape
`AGovernedRunReachesTheConsoleTests.TheProductItselfConstructsASessionLane` already uses. It was
**not added**: clause 0 compares this file's bytes against `1374401d`, so widening the oracle after
the run would redden clause 0 — the control working against its own author, which is what it is for.
The gap is recorded as a residual instead, and it is the same class one level in: *an edge between
two clauses that no clause owns.*

### Part 2 — what F6 closed, verified on the merged tree

**The edge is built, and the blind spot above is closed by a control that already exists.** Checked
on `main` at `be68ca1c`, merged here:

| Was missing | Now | Observation |
| --- | --- | --- |
| no `composer.Configure(` in `src/` | `MainWindow.xaml.cs:263` | inside `BindComposer`, on the create path |
| no `new ComposerSendContext` in `src/` | `MainWindow.xaml.cs:265` | `TaskClass: created.TaskClass` — the field that used to be dropped is now carried |
| `Model`/`AccountLabel`/`Providers` had no source | `ProviderConfiguration` reads `~/.aide/providers.json` | one `LaneBinding` feeds the send context *and* the attach affirmation, so the account affirmed is the account billed |
| the oracle could not tell product-wired from harness-wired | `TheRunBindingComesFromTheProviderFileTests.TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate` | asserts **exactly one** `composer.Configure(` and **exactly one** `new Workbench.Composer.ComposerSendContext(` in the shell, then sweeps every other file under `src/AiDe.App/` for a second one |

That last row is the point. **The blind spot was never closed by widening the frozen oracle — it is
closed from outside it.** Because there is exactly one `Configure` call site in the shell and it is
product code, a run that reaches `Send()` with a populated context reached it through
`BindComposer`. Clause 0 stayed untouched, and the oracle's bytes still match `1374401d`.

**One correction to Part 1, found by re-checking rather than by being told.** Part 1 does not claim
the path is untested, and it must not: F6's `TheRunBindingComesFromTheProviderFileTests` covers the
whole E7 chain — *file → reader → registry → sheet → EnabledBackends → ComposerSendContext → Send →
GovernedRunRequest* — with a named refusal at each link. This node's first sweep for coverage grepped
the private method name `BindComposer` and found nothing, which would have been the third
grep-shaped false negative in this node. **The method name is not the path's name.**

### What was still open after F6 — superseded by Part 3

> Both rows below are **answered now**. Kept because Part 3's ruling is only legible against the
> question it answers.

| Precondition | State | Why it is not this node's to settle |
| --- | --- | --- |
| **`~/.aide/providers.json`** | **absent on this machine** (`ProviderConfiguration.DefaultPath`, checked) | `BindComposer` refuses by name without it. The file names **the account the run bills**. Inventing that label is precisely DC-110's defect, and `LaneBinding`'s own remarks refuse to pick between two accounts for the same reason. The schema is settled and testable; the value is the operator's |
| **The gesture** | **no headless front door exists** | `ConductorEntry.IsRequested` is the only argument the shell reads; `File → New Session` opens a modal `Window` through `NewSessionSheetDialog.Show`, and `showSheet` is hard-wired inside `MainWindow.NewSession()`, so nothing can substitute it without editing product code. *"The harness waits on the process object" does not apply here* — there is no process to launch for this path. The gesture is either the operator's hands or UI automation of a live modal on a billing path |
| **The cohort** | **no AI-DE episode store exists at any default location** (searched) | the store is created under the workspace's `DataDirectory` when the workspace opens, so the "task class no earlier run has used" check must be made against **that** store at run time, not against this file |

**What is ready:** the ACP adapter is installed at `C:/Projects/ai-de/spikes/acp-subscription-lane`
(`@agentclientprotocol` present); subscription use is authorised by the operator in their own words
(audit `al-01M23SEGAS071BX81W0MA9RF92`), and that authorisation is scoped to *the operator using
their own subscription on their own machine for their own project*, which is this.

### Part 3 — Ruling 49, and why the run waits for a hand

**Both preconditions in Part 2 are now answered, and one answer refuses a design.**

**The provider file exists and was verified rather than trusted.** `~/.aide/providers.json` names one
`anthropic` subscription account, label `max`, and `engines.claude-code.model = claude-sonnet-5`.
`adapterInstallRoot` resolves: `…/node_modules/@agentclientprotocol/claude-agent-acp/dist/index.js`
is present, its `package.json` reads **0.75.1**, which is the version `EngineCatalog` pins, and
`node --version` is **v24.18.0**. Path composed the way `EngineCatalog.ResolveLaunch` composes it,
then checked on disk.

**The account is `quota-degraded`, and that binds.** `AccountHealth.QuotaDegraded`'s own words:
*"Under quota pressure — a signal, not an absence. A lane still binds, and carries the pressure."*
Only `NeedsLogin` is refused (`ProviderRegistry.cs:178`). So a quota refusal, if one comes, will come
from the API rather than from the binding — **and it is the recorded result, not a reason to run
again.** Re-running until it passes would be shopping for a verdict, which is DC-127 aimed at the
exit evidence itself.

**Ruling 49 — the exit run is triggered by the operator's own `File → New Session` gesture, and no
headless entry point is built.** A headless entry was proposed to substitute for the gesture. It was
refused, and the reasoning is from the plan's text rather than from preference:

| Branch | Why it fails |
| --- | --- |
| Headless entry stamps `main-menu.new-session` | Falsifies clause 1's meaning — *"origin set **only** on the `Ctrl+N` / `MainMenuBuilder` command path"* (`conductor-front-door.md:811-814`) — and makes the front-door proof harness-producible. **That is the "asserted-about" failure N7 was blocked for. Refused outright, not available for approval** |
| Headless entry stamps some other origin | Fails the oracle: `FRONT_DOOR_ORIGIN` is checked at line 112 against line 44, and clause 0 forbids widening it |
| Run anyway, report clause 1 partial | Clause 1 is one of the six that *"Fails if any of 1–6 is absent"* (`:831-832`). **A failed exit wearing a partial-pass headline**, not "not recorded, never zero" |

**Clause 5 says the same thing from another angle** (`:820-821`): *"no second entry point (Ruling 13),
asserted by a ledger counting roots."* The headless entry would not have tripped that ledger — it
would have reached the one composition root — but the hands are the plan's **intended** trigger
rather than an accident of its wording.

**The control that would not have caught this.** `ExactlyOneSiteInSrcCanStampTheFrontDoorOrigin`
checks who *names* `SessionOrigins.MainMenuNewSession`. A headless entry driving
`NewSessionSheetViewModel.Create()` names nothing, so **the guard would have stayed green while the
claim it protects became false.** The allowlist stays at two entries and its comment now carries
Ruling 49's answer.

### If the gesture is not performed

**Then clause 1 is `NOT DEMONSTRATED`, the exit is `NOT MET`, and this pack carries no headline that
the front door works.** The slice stays open. That is the honest failure, and it is a different thing
from the third branch above: it claims nothing, where a partial-pass headline would claim something
false. **Nothing in this pack should be read as evidence the front door works until a row below says
a gesture happened.**

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
| **R13 b2's live gap: the sheet cannot list backends in the running app** | named | `providers.yaml` has **no reader anywhere in the repository**, and Ruling 35 refuses a third hand-rolled one. The sheet renders an honest empty state. The oracle is fully discharged against a populated registry in test; ~~`ProviderRegistry` is constructed from in-code rows at `GovernedRunHost.cs:90`, never from a file.~~ **No longer true — F6 landed `ProviderConfiguration.Read`, which parses `~/.aide/providers.json`; `GovernedRunHost.cs:90` now rebuilds from `request.Providers`, the rows travelling on the request.** |

### Found by this node

| Residual | Kind | Detail |
| --- | --- | --- |
| **Ruling 45's Terminal-mode exposure, and the race in any naive check of it** | measured | `CanvasModeCatalog.cs:64` constructs `new TerminalSurface(...)` for the Terminal canvas mode, and `TerminalSurface.cs:62` — the constructor — is `_ = StartAsync(sessionId, columns, rows);`, reaching `ConPtyTerminalSession.StartAsync` at `:426`. **Building that mode constructs a real terminal host**, so a run that opened the canvas split would have incremented the counter clause 4 asserts is zero. This run did not: `terminalModeBuilt == false` is recorded, not assumed. **And the sharper half:** the start is *fire-and-forget*, so a ledger read taken immediately after construction **races the increment in the direction that passes** — the next person to write that assertion needs to know it before they write it, not after. |
| **A seam between two adjacent nodes that no clause assigned** | named | **Ruling 46's class.** `ComposerSurface.Send()` built a `GovernedRunRequest` and returned it to a discarding caller; `ConductorEntry.cs:80` was the only call to `GovernedRunHost.RunAsync` in the product; and `AcpEventQueue`'s channel is `SingleReader = true`, so no `SessionLane` could drain what the host drains. F4 built one end, F2 built the other, and **F2 wrote the conflict down in `SessionLane.cs:12-16` while no clause resolved it.** This is a *plan* defect, not a code one: adjacent nodes each built one end of a seam nobody owned. The control is a slice-level **E7 surface list with every edge assigned to a node**, failing plan review when an edge is unowned — named here, and **not built by this node**. |
| **Clause 5's ledger is built by the node that does not grade it** | named | The composition-root ledger was drafted here and **handed to F4b** under Ruling 46's own principle applied one level down: *the node that builds the counter must not be the node whose Proof Pack the counter proves.* Recorded because the reasoning is the reusable part, and because the hand-off is the kind of thing that looks like lost work in a log unless it says why. |
| **The F5 gate's bare run is not wired into CI yet** | named | `verify-project-coverage` is right that a gate no workflow invokes is not a control, and wiring the bare run before the exit run existed made CI red for the honest reason that the gate's subject did not exist — observed on run `34606518579`, job `gates`, step *Front-door exit evidence (F5)*. Only the **self-test** is wired now, which is a real invocation doing real work (it reddens on every clause it claims to check) and satisfies the coverage gate without wiring a control ahead of the thing it controls. **The bare run is added in the same commit that lands `spikes/conductor-front-door-exit-run/exit-evidence.json`** — named here so it is a step rather than something to be remembered. Until then the nine clauses are checked by running the gate locally, which is exactly the weakness the coverage gate names. |
| **The composer's send context has no producer in the product — DC-130's second instance in this slice** | measured | `grep -rn "ComposerSendContext" src` returns **five** hits and **zero constructions** — a declaration, a doc-comment cref, two parameters and a field. The only `new ComposerSendContext` in the repository is `ASendLaunchesAGovernedRunTests.cs:79`. So `ComposerSurface._context` is null in every shipped path, `Send()` returns null with *"the composer is not wired to a session yet"*, and **clauses 2, 3, 5, 6 and 9 are unsatisfiable** — five of nine, the same arithmetic the first instance produced. The discarded value is `NewSessionResult.TaskClass` at `MainWindow.xaml.cs:160`, the one field the sheet refuses to default (DC-110). **This node did not wire it:** the edge is unowned, and deciding where a *reopened* session's task class comes from is a ruling, not an evidence node's call. |
| **A guard that would have stayed green while its claim became false** | measured | `ExactlyOneSiteInSrcCanStampTheFrontDoorOrigin` checks who **names** `SessionOrigins.MainMenuNewSession` — allowlist of **2** files. The proposed headless entry would have driven `NewSessionSheetViewModel.Create()` and named nothing, so the guard would have passed while clause 1's sentence — *origin set **only** on the `Ctrl+N` path* — became false. **The gap is between naming the constant and reaching the sheet, and only the first is mechanized.** Refused by Ruling 49, so the second front door does not exist; the allowlist comment now carries that answer so the next proposer meets it. **Not built:** a guard over *who reaches the sheet* is a call-graph question, not a token scan, and no ruling asked for one. |
| **The exit run will not exercise the ambiguous-account refusal** | named | `~/.aide/providers.json` carries **one** account deliberately, so `LaneBinding`'s two-account refusal cannot fire on this path. It is covered by test — `TheRunBindingComesFromTheProviderFileTests.AnAmbiguousAccountRefusesOnTheComposerNamingTheField` — and **not** by the live run. Stated because a reader could otherwise take the green run as evidence the refusal works in production; it is evidence the *happy* path works with one account configured. |
| **The run's cohort will carry a degraded account** | measured | The configured account's `health` is `quota-degraded` — **the operator's own observation, not a probe and not a placeholder** (`ProviderAccount.Health` is documented as the operator's record). `QuotaDegraded` binds by design and *carries the pressure*, so a refusal or throttle from the API is a legitimate measured outcome. **The one-run rule applies:** a quota refusal is recorded as the result and is not grounds to re-run. Re-running until green would be DC-127 aimed at the exit evidence itself. |
| **DC-095 is controlled in code comments and uncontrolled in Proof Packs** | measured | `verify-cited-controls.py` scans `SEARCHED = ("src", "tests")` — **source comments only**. A Proof Pack is markdown, so every control it cites is unguarded, in the artifact whose whole purpose is citing controls. All **14** test-shaped identifiers in this file were resolved against `tests/` at this close and **one did not exist**: the header's falsifier suite named `TheRunHasOneCompositionRootTests`, which has never existed (the real class is `TheOneCompositionRootIsCountedTests`). Corrected above. **The extension is named and not built**, and it is cheap — the resolver already exists twice, as `_declared_in_tests` in this slice's oracle and as the matcher inside `verify-cited-controls.py`. **One warning for whoever builds it:** the dead name is still written above and in the carried note, as the *record* of the defect, so a naive resolver reddens on the two files that document it. A mechanical citation checker needs claim language as its trigger — the reason `verify-cited-controls.py` already keys on *asserts / pins / proves / guards*, and not on the shape of the identifier. |
| **The oracle cannot distinguish a product-wired composer from a harness-wired one** | named | Clause 2 asserts the request was built in the composer; clause 5 asserts the launch site is under `src/`. **Neither asks who supplied the send context**, so a driver that calls `Composer.Configure(...)` itself and then presses `Send()` reads green on both while the product still cannot send — DC-127's shape, reachable through this pack's own gate. The closure is a source scan of the form `TheProductItselfConstructsASessionLane` already uses. **Not added, deliberately:** clause 0 compares this file's bytes against `1374401d`, so widening the oracle after the fact reddens clause 0. The control refused its own author, which is the behaviour it was committed early to have. |
| **The exit run did not happen** | named | Every `RUN-PENDING` row above is pending for the structural reason recorded in *The exit run did not happen* — not because a run was attempted and failed, and not because one was skipped for cost. No live subscription turn was spent, no lane tree was provisioned, and no episode was scored by this node. **The `front-door-preflight` class recorded earlier remains the only live run this node caused**, and it is tagged `not-exit-evidence` in its own store. When the run does happen it will still be **one** run — one host, one engine, one account, one task class, no distribution and no rate — and the re-runnable half will still be the test suite. |

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

Measured on the merged tree (`main` at `be68ca1c` merged into `feature/exit-evidence`), Debug, this
machine. The floors are the ones `tools/expected-test-counts.json` carries **after F6's bump** — an
earlier revision of this table named 495 / 2206 / 2052, which were pre-F4b and stale, and the
revision after that named 512 / 2210 / 2056, which were pre-F6 and stale. **Both were corrected by
re-measuring, never by carrying the number forward.**

| | Executed | Floor | Over |
| --- | --- | --- | --- |
| `AiDe.App.Tests` | **517** | 517 | 0 |
| `AiDe.Core.Tests` | **2238** | 2234 | +4 |
| `AiDe.Core.Tests` portable | **2084** | 2080 | +4 |
| `AiDe.Core.Tests` non-portable | **154** | 154 | 0 |
| Full gate set, bare | **30 of 31 green** (`ls tools/verify-*.py` counted, not recalled); the thirty-first is this slice's own oracle, refusing for the reason below | — | — |
| Build | **0 warnings, 0 errors** | — | — |

`2084 + 154 = 2238` **by observation**, not by arithmetic on the baseline: each half was run under its
own `--filter` and `--key`, and the whole project was run separately, so the sum is three
measurements that agree rather than one measurement and a subtraction.

The `+4` is this node's own `TheSessionOriginIsSetOnlyOnTheCommandPathTests` (4 cases), landed in
`bc6d6a4b` for clause 1. It is the whole of the excess over the floor.

**Baselines were not raised by this node.** `tools/expected-test-counts.json` is untouched and
`verify-test-run.py --update` was not run: a node raising its own floor is grading its own work, and
the bump is the conductor's separate recorded act.

**The one gate that is red, and why that is the correct reading.**
`tools/verify-front-door-exit-evidence.py` run bare reports *"the exit-evidence record was not found
at `spikes/conductor-front-door-exit-run/exit-evidence.json`"* and exits 1. That is the oracle doing
the job it was committed early to do: refuse when the run did not happen. Its `--self-test` — the
form CI invokes — exits 0 with *"the oracle reddens on every clause it claims to check"*. **A gate
that refuses an absent subject is not a broken gate**, and the distinction is why only the self-test
is wired (`aa058e20`).
