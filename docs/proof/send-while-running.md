---
id: proof-send-while-running
title: "Proof Pack — Ruling 95: a Send while a turn runs offers Wait (one queued turn) or Parallel (a derived sibling session); Ruling 77(b) reversed"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, composer, sessions, thread, ruling-95, ruling-77, ruling-78, ruling-83, ruling-99, red-first, measurement]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: proof-composer-compiled-prompt-and-console-rows, rel: refines }
  - { to: proof-conductor-front-door, rel: refines }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Lane composer-r95 on main c831113e. Condition 1 measured first: two session documents on one
  workspace each sent "reply with the single word ok" 59 ms apart through the product's own
  composition root against the live claude-code adapter; two engine processes (pids 34340, 57860)
  were alive together in every census sample, node rose 41 → 43, both turns answered in 3.5 s / 3.4 s
  — the run host does not serialise them, so Parallel ships. Wait: one queued turn per session,
  compiled at queue time, drained only after Completed/Answered, waiting with Send now after Stop or
  Failed; Cancel returns its words. Red-first each.
---

# Proof Pack — Ruling 95 (lane `composer-r95`)

Base `c831113e` · branch `lane/composer-r95` · coord session `composer-r95`.

## Goal state (written before the first substantive tool call)

- **Goal:** land Ruling 95 — on Send while a turn runs or waits, the composer's status line offers
  *Wait — send after b1* (one queued turn) or *Start a parallel session* (a derived sibling session).
- **Done when:** condition 1 measured and recorded here before any Parallel code; Wait shipped
  red-first (queued · drained · stopped-with-queued · cancel); the drain never sends after Stop/Failed
  (test); the queued turn's compile spend on its own outcome line (Ruling 78); Parallel shipped or
  refused with the measured reason; DESIGN.md's copy and §B5's STA rows amended; gates green; audit
  entry; `coord session end`.
- **Not in scope:** the account picker (Ruling 105), the New Session sheet
  (`NewSessionSheetViewModel` — the Sessions lane's), the Explore reader, the store.
- **Tier:** T1 · **fan-out:** 0.

## Condition 1 — the measurement, first

**Harness:** `AiDe.App.ComposerProbe --parallel-turns` (`tests/AiDe.App.ComposerProbe/Program.ParallelTurns.cs`).
Two `SessionDocumentSurface`s on one temp workspace (a `git init`'d folder, one empty commit), each
composer **harness-wired** — the probe calls `Composer.Configure` itself with a `ComposerSendContext`
built field-for-field as `SessionComposerBinder.Bind` builds one, from the machine's own
`~/.aide/providers.json` (`claude-code · claude-sonnet-5 · max`, health `quota-degraded`, which still
binds). Everything from `Send()` onward is **product-wired**: `ComposerSendGate.Send` →
`SessionDocumentSurface.Launch` → `GovernedRunHost.RunAsync` (the one composition root). The F5 pack's
"harness-wired vs product-wired" caveat applies to the binding only. Prompt: *"reply with the single
word ok"* (read-only: no lease, no worktree, no episode). Log: `%TEMP%\aide-parallel-turns-1.log`.

| Reading | Session A (`…-91f04456`) | Session B (`…-ac97e205`) |
|---|---|---|
| `Send()` at (UTC) | 17:56:45.2795 | 17:56:45.3389 (**+59 ms**) |
| Turn accepted at | 17:56:45.2927 | 17:56:45.3436 |
| `lane.session-new` row (`ts`) | 17:56:46.9315 (`run-28e41e41`, `lane-69e093ee`) | 17:56:46.8112 (`run-b1de0e45`, `lane-5daba95f`) |
| `session/new` params | `{"cwd":"<temp>","mcpServers":[],"_meta":{"claudeCode":{"options":{"disallowedTools":[…31 tools…],"thinking":{"type":"adaptive","display":"summarized"}}}}}` | byte-identical to A |
| ACP session id | `034c3e4a-c649-4e4f-b1a0-2acb3399ee80` | `2cd044ad-cd31-4a50-8ef8-fa6fe5fbe773` |
| Engine pid (`node`) | **34340** | **57860** |
| `prompt stopReason` | `end_turn` | `end_turn` |
| Outcome line | **answered · 6 tokens · 3 s · 11 events** | **answered · 6 tokens · 3 s · 12 events** |
| Concluded at | +3.5 s (≈ 17:56:48.79) | +3.4 s (≈ 17:56:48.74) |
| Run outcome / failure | `Completed` / none | `Completed` / none |

**Process census** (`Process.GetProcessesByName("node")` every 250 ms on the dispatcher while either
run was open): 14 samples from 17:56:45.355 to 17:56:48.698; **both engine pids alive in all 14**;
node baseline **41**, peak **43**, delta **+2**. Both `lane.session-new` rows precede both outcomes by
~1.9 s; B's `session/new` was recorded 120 ms *before* A's although A was sent first — the two lanes
are interleaved, not ordered.

**Decision:** the run host does **not** serialise two sessions on one workspace — two `session/new`
frames, two engine processes overlapping in time, both outcome lines `answered` within 0.1 s of each
other. **Parallel ships.** Ruling 95's Inferred clause (*two engine processes for one workspace run
concurrently in the App*) is now **Verified** for the read-only shape on this machine.

**Residual (named, not modelled):** the measurement is of two *read-only* turns. A write-shaped turn
cuts a worktree per lane (`WorktreeProvisioner`) — two lanes on one repository would cut two worktrees;
not measured here (the operator's consent covered one short read-only prompt per session), and not a
condition of the ruling.

## The design, as built (T1: the rendered states, proven by rendered STA rows)

- **The two-action line** (`ComposerSurface.OfferWaitOrParallel`): *"b1 is running. Wait — send after b1, or start a parallel session."* — three links in the status `TextBlock` (the ordinal to the turn's container, SC8; *Wait — send after b1*; *Start a parallel session*), spoken once assertively (SC6). Offered **behind the prepare gate**, so under an agentic rung the choice comes on the confirming gesture, never on the preparing one (Ruling 77(a) kept: Send is the confirmation).
- **Wait** (`SendAfterTheTurnInFlight` → the one `SendThroughTheGate`): the same `ComposerSendGate.Send`, the same bytes, `submitted` with its sha; the document's `Launch` sees a turn in flight and calls `RunChannelSessionThread.Enqueue` instead of `Accept`. **Exactly one**: the read model refuses a second (`InvalidOperationException`), and the composer refuses before compiling: *"b2 is queued; cancel it or wait."*
- **The queued row** (`TurnState.Queued`, Core): word *queued*, sentence from the snapshot — `ThreadSnapshot.Queued` / `QueuedBehind` / `QueuedAwaitsYou`, `TurnCopy.QueuedSentence` — one derivation for the row's help text, its reason box and the composer's line. Actions: **Cancel** only; **Send now · Cancel** when `QueuedAwaitsYou` (nothing in flight and the turn before it ended Stopped/Failed — derived from the turns, so the instant between a Completed conclusion and the drain never flashes *Send now*).
- **The drain** (`SessionDocumentSurface.DrainAfter`): runs on the surfaces' thread after `RunOneAsync` concludes **Completed/Answered only**; `Start(ordinal, now)` moves the row Queued → Running (its duration counts from the send) and `Run` is the same lane/relay/root as a direct send. Stop, Failed and a closing document never drain.
- **Cancel** (`TurnState.Cancelled`, terminal): the stream is append-only, so the row stays — *cancelled by you · never sent* — the envelope gets its `consumed` (`cancelled_by_operator`), the words go back through `UseAsNextDraft`, the queue is free, the next turn is b3.
- **Spend** (Ruling 78; condition 4): `SubmittedEnvelope.CompileCost` carries the fold's last `called.cost` when it made a request; the document seeds it as the turn's `Spend` at `Accept`/`Enqueue`, so the run's cost adds to it on **that** turn's outcome line. A reused call (0 requests) and a mechanical compile contribute nothing — never a zero.
- **Parallel** (`ParallelSessionFlow`, wired by `MainWindow` into `WorkbenchShell.ParallelSessionStarter`): `SessionConfigStore.Create(name, parent.WorkspaceId, parent.EnabledBackends, now, parent.FanOutCeiling, parent.BudgetCap, parent.DefaultTaskClass, origin: "parallel:<parent id>")`, an agentic parent's compile mode copied through `SetCompileMode` with the ladder's availability (`CompileModeGate.Evaluate(adapterInstallRoot)`), Recent sessions remembered, `Shell.OpenSessionDocument` (Ruling 83's Left zone by the kind's zone rule), the window's binder, then `UseAsNextDraft` + the attachments + `Send()` on the sibling's composer. The composer's `ParallelStarter` returns the refusal when the words were not sent, and only a null consumes the parent's draft.

## Claims

| # | Ruling · condition | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | 95 c1 — measure first | `ComposerProbe --parallel-turns` (above) | — (a measurement, not a control) | two engine pids alive together in 14/14 samples; node 41 → 43; both `answered · 6 tokens · 3 s` | Verified |
| 2 | 95 — the read model's queue | `TheQueuedTurnIsOneTurnOfTheStreamTests` (10 rows: next ordinal · exactly one · Start re-stamps · cancel-only endings · after Stop/Failed waits on you · after Completed/Answered never reads as waiting · compile spend on its own turn · the policy's three transitions) | `CS0117: 'TurnState' does not contain a definition for 'Queued'` (and `Cancelled`, `Cancel`, `SendNow`, `QueuedSentence`, `Enqueue`, `Start`) against the base — the model had no queue | 10/10; the four neighbouring read-model/policy classes 34/34 | Verified |
| 3 | 95 — the two-action line (§B5 STA) | `ASendWhileATurnRunsOffersWaitOrParallelTests.ASendWhileATurnRuns_OffersWaitAndParallel_OnTheStatusLine_NeverARefusal` | `the status line offers no Wait action; it reads "b1 is running; the next turn waits for it."` | three links (`b1, running` · `Wait — send after b1` · `Start a parallel session`); the sentence spoken once, assertive; nothing compiled | Verified |
| 4 | 95 c2 — queued · cancel (STA) | `…Wait_QueuesOneCompiledTurn_ASecondSendIsRefused_AndCancelReturnsTheWords` | same red | b2 `Queued`, `SentBytes == Gate.RenderView(draft).Text`, `LastSubmission` set, editor empty, status `b2 queued — sends after b1`; row actions `[Cancel]`; third Send → `b2 is queued; cancel it or wait.`; Cancel → `cancelled by you` · `never sent`, draft `second`, queue free | Verified |
| 5 | 95 c2 — drained (STA, the product's root, stub adapter at the process boundary) | `…TheQueuedTurn_IsSentWhenTheRunningTurnAnswers_ThroughTheProductsOwnRoot` | same red | b1 held by the stub; Wait queues b2; `CompositionRootLedger.Roots == 1`; release → b1 `Answered` → b2 `Answered`, roots **2**, `answered · 6 tokens`, status cleared | Verified |
| 6 | 95 c2 + c3 — stopped-with-queued (STA) | `…AfterStop_TheQueuedTurnWaitsWithSendNow_AndTheDrainNeverSendsIt` | same red | Stop b1 → b2 still `Queued`, roots 1, `QueuedAwaitsYou`, status `b1 stopped by you; b2 is waiting — Send it or cancel it`, actions `[SendNow, Cancel]`; **Send now** → b2 `Running`, roots 2 → release → `Answered` | Verified |
| 7 | 95 c3 — failed-with-queued (STA) | `…AfterAFailure_TheQueuedTurnWaits_AndTheDrainNeverSendsIt` (the stub holds `initialize`, then echoes protocolVersion 99 — the client refuses) | same red | b1 `Failed` → b2 still `Queued`, roots 1, status `b1 failed; b2 is waiting — Send it or cancel it`, actions `[SendNow, Cancel]` | Verified |
| 8 | 95 c4 / 78 — the queued turn's compile spend on its own line (STA, agentic-advisory, fake compiler 100/20 then 300/40) | `…TheQueuedTurnsCompileSpend_IsOnItsOwnOutcomeLine` | `b2 never prepared` (the old refusal came before the prepare gate) | b1 `126 tokens` (100+20 compile + 4+2 run), b2 `346 tokens` (300+40 + 6), `Spend(304, 0, 42, 2)` | Verified |
| 9 | 95 — Parallel: the flow | `AParallelSessionIsADerivedSiblingTests.TheFlow_CreatesADerivedSibling_AndSendsTheDraftThroughItsOwnGate` | (a new type: does not compile against the base) | name by the seam (`payments (7)`), workspace/backends/ceiling 2/cap 10·40,000/class `implement` copied, `origin = parallel:<parent>` on the sibling's `session.open` and `direct` on the parent's, remembered, sibling `BlocksSent == 1`, b1 `second question`, the attachment's text in the sent bytes | Verified |
| 10 | 95 — Parallel refused keeps the words | `…WhenTheSiblingsComposerIsNotBound_NothingIsSent_AndTheRefusalIsTheBinders` · `…WithoutAShell_TheActionRefuses_AndTheWordsStay` | `a parallel session needs the shell; this composer has none` (the composer on a document with no shell — the sentence a shell without the seam would read) | refusal = the binder's sentence, sibling `BlocksSent == 0`, announcement `The prompt stays in this editor.`; the shell-less composer keeps `second` | Verified |
| 11 | 95 + 83 — Parallel from the status line docks Left beside the parent | `…FromTheParentsStatusLine_TheSiblingDocksLeftBesideIt_AndTheParentsDraftIsConsumed` (`ComposedCoding.Show` — the product's docking host) | `CS1061: 'WorkbenchShell' does not contain a definition for 'ParallelSessionStarter'` against the base shell | the sibling's surface in `ZoneId.Left` with the parent's, `Maximized == null`, title `payments (2)`, bound and sent (b1 `second`), origin recorded; parent draft empty, status `sent as the first turn of a parallel session`, parent thread untouched | Verified |
| 12 | 99 — the name seam carries the Sessions lane's rule | `…WithRuling99sRule_TheSiblingsNameCountsPastTheNamesTheWorkspaceHolds` | — (Ruling 99 landed on main during this lane; the seam's default `" (2)"` was the red-before) | over `{payments, payments (2)}` the sibling is `payments (3)`; `MainWindow.xaml.cs` wires `SessionConfigStore.UniqueName(name, …ExistingNames(root))` (source-scanned) | Verified |
| 13 | found on the way — the run host's drain hung when the prompt completed after the last event | `TheDrainEndsWhenThePromptEndsTests` (3 rows) | `the drain did not end within 5 s after the prompt completed with an empty queue` · `the drain did not end after the prompt faulted` | `DrainAsync` waits on the queue **or** the prompt (`Task.WhenAny`), drains what is queued, ends when the prompt is done and nothing is queued; the two order-dependent rows in `AGovernedRunReachesTheConsoleTests` re-pointed to the wire's order (answer frame published before the reply completes) | Verified |

Rows 3–8 were run **six times in a row after the drain fix: 36/36**; before it, 1 in ~4 runs of row 8 hung with every event delivered and the turn reading *Running* (finding 1).

## Interpretation the conductor should confirm (marked, not assumed silently)

- `assume:` Ruling 95's *"Cancel (drops it)"* is read as *drops it from the queue*: the row stays as a terminal `cancelled` turn (*cancelled by you · never sent*) because the thread's stream is append-only (THR-0003; the feed's positional merge assumes ordinal = index + 1) and the envelope's accepted `submitted` row needs its `consumed` (`cancelled_by_operator`). The ordinal is consumed — the next turn is b3. **Confirms:** the conductor at the join. **Breaks if false:** a cancelled row should vanish and b2 be reused — a read-model change (removal) that the feed's merge and ADR-0034's one-consumed-per-envelope would both have to admit.
- `assume:` under an agentic rung Wait costs the same gestures as a direct send plus the choice — prepare · confirm → the offer → Wait — because Send is the confirmation of a prepared turn (Ruling 77(a)) and the offer sits behind the prepare gate. A one-gesture "prepare-and-queue" would confirm bytes the operator has not read. **Confirms:** the conductor. **Breaks if false:** the offer moves in front of the prepare gate and `SendAfterTheTurnInFlight` prepares then queues on completion (two lines).
- `assume:` a parallel session copies the parent's compile mode through `SetCompileMode` with the ladder's availability evaluated from the provider file's adapter root (the sheet's own gate), and stays `mechanical-only` — said in the announcement — when the window has no provider file. Never written past the gate.

## Findings not changed (placeholder ids — the conductor allocates)

1. **DC-nnn (composer95 a)** — *A drain whose exit condition is re-evaluated only when the thing it drains produces something can outlive it: the terminal signal (the prompt task) completes after the last event, and the loop waits for an event that never comes.* Signature: `await foreach (… in reader.ReadAllAsync(ct)) { …; if (other.IsCompleted && reader.Count == 0) break; }` — a second completion source checked only inside the loop body. Sweep: `grep -rn "IsCompleted && .*Count == 0" src/` → the one site (`GovernedRunHost.DrainAsync`), fixed. The tell in the record: the live measurement's **11 vs 12 events** on two identical read-only turns — the drain exited at the answer frame in one and at the trailing `usage_update` in the other; now it ends when the prompt is done and nothing is queued, in either order. Control: `TheDrainEndsWhenThePromptEndsTests`. **Fixed in this lane** (AgentPlane seam, on Ruling 95's critical path); the class is new.
2. **DC-nnn (composer95 b)** — *An oracle that reads a derived surface right after the source's condition became true, without letting the derivation run: the row read `0 events` for a turn the read model had already concluded.* Signature: `await Until(() => readModel.X); Assert(rows[i].Y)` with no pump between; the failure reads as a wrong value, not as staleness. Control: the test helper pumps after the condition holds (`UntilAsync`). Test-idiom class; not in `src/`.
3. **Finding (design, not changed):** the queued turn's wait in the queue is not recorded anywhere — `Start` re-stamps `At` so the outcome's duration is the run's (Ruling 78's *run start → outcome*); how long b2 waited behind b1 is *not recorded*, not modelled. An operator question (*how long did it wait?*) with no emitting source yet (IO2).
4. **Finding (design, not changed):** the parallel measurement is of two **read-only** turns; a write-shaped pair would cut two worktrees on one repository (`WorktreeProvisioner`) and hold two leases — not measured (the operator's consent covered one short read-only prompt per session) and not a condition of the ruling.
5. **Finding (pre-existing, Shell lane):** every full App run's terminal ledger shows **10 starts / 9 stops** (`s-terminal`, `SurfaceContentTests`), before this lane touched anything (the base run at c831113e: 10/9) — unchanged; no test in this lane constructs a terminal.
6. **Finding (pack):** one commit in this lane (`8e1669a7`) was made from a shell where `AGENT_SESSION` was unset — the pre-commit boundary printed its advisory and checked nothing. The claims were held (the files were claimed by this session); the control is the environment, which a fresh shell drops. Not changed.
7. **Finding (pre-existing):** `coord doctor` at the base reports `regeneration 8 artifact(s) OWED` — the derived views are the conductor's at the join.

## Attended rows for the operator

| Do | See |
|---|---|
| While a turn runs, type a prompt and press Send (Ctrl+Enter) | the status line reads **b1 is running. Wait — send after b1, or start a parallel session.** — three links, no dialog; it is spoken once |
| Click **Wait — send after b1** | the editor clears; a **b2** row appears with the word *queued* and *b2 queued — sends after b1*; its only action is **Cancel** |
| Press Send again with new words while b2 is queued | *b2 is queued; cancel it or wait.* — nothing else happens |
| Let b1 finish | b2 turns *running* on its own, then answers; its outcome line carries its own tokens |
| Instead, **Stop this turn** on b1 while b2 is queued | b2 stays queued; the status reads *b1 stopped by you; b2 is waiting — Send it or cancel it*; b2's row gains **Send now** |
| Click **Cancel** on b2 | the row reads *cancelled by you · never sent*; your words are back in the editor |
| Click **Start a parallel session** | a second session tab named *<this session> (2)* (or the next free counter) docks in the Left zone beside this one, bound to the same engine; your prompt is its b1, already sent; this editor is empty and reads *sent as the first turn of a parallel session*; Recent sessions lists it |
| Open the Console during two parallel turns | two engine processes; both threads progress at once (the measurement above) |

## Gates and counts

- Tests executed, before → after (trx `executed=`): **App 987 → 1007**, **Core 2648 → 2660**. Before = a full run of both suites on the base tree at `c831113e`, observed (not the committed baseline file); after = the merged tree (origin/main `eb8cdee2`, whose own baseline reads App 993 / Core 2650, merged mid-lane for Ruling 99) — so this lane's own additions are **App +14** (6 Ruling 95 STA rows · 5 Parallel rows · 3 drain rows) and **Core +10** (the queue's read-model and policy rows); the rest is main's. Core's one skipped test is the baseline's. `verify-test-run.py --update` deliberately not run — the recount is the conductor's at the join.
- `python tools/run-verify-gates.py` on the committed tree: 35 of 38 green at the first run; `verify-r14b2-session-naming` failed on the new type's name (`ParallelSessionRequest` → renamed `ParallelDraft`, the composer's word for what it hands over, since the type lives beside the composer and not under `Sessions/`); `verify-derived-views` and `verify-site-figures` report the API figures owed by this lane's new public types (public symbols 3,444 → 3,466) — regenerated with `tools/regenerate-derived.py` after the audit entry, the order DC-082 names, and committed; `verify-stranded-audit` names no other tree.
- Terminal ledger for this lane's full App runs: **10 starts / 9 stops** on the base run (finding 5, pre-existing), **10 / 10** on the final merged run — the missing stop is no longer missing on the merged tree; not this lane's change (no test here constructs a terminal), so not claimed.

## Residual risk

- The parallel measurement was harness-wired at the binding (the probe called `Composer.Configure`); everything from `Send()` down was the product's. The shell-level Parallel test binds the sibling through the same harness shape; the window's `BindComposer` is the one production binder (`TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate` still holds it to one site) and is wired to the flow in `MainWindow.StartParallelSession`.
- The stub adapter answers a held prompt from a file cue; the real adapter's frame *order* (answer frame, then a trailing `usage_update`) is what the drain fix was shaped on, from the corpus and the live measurement. The drain now ends when the prompt is done and nothing is queued — a trailing bookkeeping frame that arrives *after* that instant is not drained, as before; the Console row count for a turn is therefore stable rather than timing-dependent, but may exclude one late `usage_update` (Console-only, Ruling 100).
- `Start` re-stamps a queued turn's `At`; a reader of `TurnView.At` for a queued turn sees the queue time until it starts, then the send time.
