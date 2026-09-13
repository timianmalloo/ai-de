---
id: proof-console-coalesce
title: "Proof Pack — CV-5.2, Coalesce: the Console's row grain is the message, never the wire chunk; one fold read by the thread's reply side and the split; Reply retired (Ruling 81)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, conversation-lane, cv-5, cv-5-2, coalesce, console-split, session-thread, ruling-74, ruling-81, ruling-82, ruling-89, dm7, sc1, sc9]
links:
  - { to: design-session-thread-itemscontrol, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: ui-review-operator-findings-2026-09-13, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: proof-composer-as-conversation, rel: refines }
  - { to: mockup-session-conversation, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-13
summary: >-
  Evidence for CV-5.2 on the Conversation lane: one pure fold, Coalesce.Rows(turn.Events), folds
  consecutive agent.msg (and agent.thought) chunks from one lane into one TurnRow carrying the first
  chunk's timestamp, the lane, the joined text and the chunk count, breaking at any other kind or
  lane; TurnView.Rows is that fold and TurnView.Reply, the StringBuilder second store and
  Conclude(reply:) are gone; the Console split derives heading + Coalesce of every turn and renders
  the operator's three-row message as one row reading "16:29:56 · claude-code · message · merges
  are missing from the tracker and · 3 chunks"; C1–C4 went red → green (the theory seeded, the
  reflection asserts red on the Reply property), the refused run's sentence is a stderr line of the
  run, every clock on the surface is the operator's local time, and C5 is RUN-PENDING on SH-4.2's
  console-document row (Ruling 89; seam request req-01M2E1HEMCMHRW1AX4SY8RJFQ7).
---

# Proof Pack: CV-5.2 — `Coalesce`, the Console's message grain

- **Change:** `lane/conversation-cv5` from `main` `38afb30b` (Ruling 89 merged before the first
  commit; no rebase). Commits `5f4db11b` (Core: `Coalesce`, `TurnView.Rows`, `Reply` retired; the
  App adapted; fixtures at the event level), `ce4b7d54` (the split over the fold; the row
  template; one local clock) and the review commit (the three reviews applied: the row wraps, the
  reply side's oracle, the mapper join, the +05:00 clock oracle, the shrinks). **Core:** `Presentation/Sessions/Coalesce.cs` (new: `TurnRow`,
  `Coalesce.Rows`, `Coalesce.Folds`, `MessageKind`, `ThoughtKind`), `SessionThread.cs`
  (`TurnView.Rows`; the `reply` parameter and `Reply` property gone), `RunChannelSessionThread.cs`
  (no `StringBuilder`, no `ReplyKind`, `Conclude` without `reply`). **App:**
  `Workbench/Sessions/ConsoleSurface.cs` (`Line(Ordinal, TurnRow Row)` with `KindWord` ·
  `ChunksText` · `HasChunks` · `IsThought`; `Derive` over `turn.Rows`; `LineTemplate` = ts · lane ·
  kind word · text · n chunks), `ThreadFeed.cs` (`ProseRowTemplate`; `LocalClock`; the event line
  through it), `TurnItem.cs` (`Prose` / `HasProse` for `Reply` / `HasReply`),
  `SessionDocumentSurface.cs` (the refusal's sentence appended as a `stderr` line before
  `Conclude`). **Tests:** `Core.Tests/Presentation/Sessions/{CoalesceTests, RunChannelSessionThreadTests}.cs`
  (new), `App.Tests/Sessions/Thread/TheThreadIsOneListTests.cs` (new — DS-1's M1 moved here and
  re-pointed), `ThreadFixtures.cs` (the reply as chunks; tool rows; `run.accepted` /
  `run.completed`), `ASendLaunchesAGovernedRunTests.cs` (the refusal line), the ctor / `Conclude`
  call sites.
- **Spec / design:** Ruling 81 (with 82 for the thought run, 74 condition 1 re-pointed, 89 for the
  split's home) in `docs/notes/addendum-c-council-rulings.md`; `DESIGN.md` §"Errata after Rulings
  80–82 and 87" (the reply = the conversation over `Coalesce`; SC1 one row per message);
  `docs/mockups/session-conversation.html` (`coalesce`, `crow`); the review's §7 CV-5.2 row
  (`docs/reviews/ui-operator-findings-2026-09-13.md`) — the oracles below verbatim.
- **Tier:** T2 (fan-out 3: the Test Architect, the Simplifier, UX & Accessibility — read-only).
- **Author / date:** Claude Opus (session `cv-5`, track CV-5.2 under `conductor-addendum-c`),
  2026-09-13.

**The operator's words:** *"console output is too fine grained"* — their Console read
`16:29:56 claude-code mer` · `ges are` · `missing from the tracker and`, one timestamped row per
streaming chunk. **After this slice the same events read as one row:** `16:29:56 · claude-code ·
message · merges are missing from the tracker and · 3 chunks` (C4 renders exactly those events and
reads the row's text from the visual tree).

## Claims & evidence

### Claim 1 (C1): `Coalesce(turn.Events)` folds consecutive `agent.msg` chunks into one row — first timestamp, lane, joined text, chunk count — broken by any other kind, any change of kind, or another lane; `agent.thought` folds as its own run; every other kind is one row per event; empty → empty
- **Evidence:** `tests/AiDe.Core.Tests/Presentation/Sessions/CoalesceTests.cs` — 16 cases green:
  `ConsecutiveMessageChunks_FoldToOneRow_WithFirstTimestampLaneJoinedTextAndChunkCount` (the
  operator's three chunks → one row, `Chunks == 3`, `At` = the first's, text joined),
  `AToolCallBetweenChunks_BreaksTheMessageIntoTwoRows` (condition 2: three rows, `[2, null, 2]`),
  `ThoughtChunks_FoldSeparatelyFromMessageChunks`, `AnotherLanesChunk_StartsANewRow`,
  `EveryOtherKind_IsOneRowPerEvent_WithNoChunkCount` (×6 kinds incl. `permission.request`,
  `acp.*`, `stderr`), `NoEvents_NoRows`, and the D2 theory
  `OverGeneratedInterleavings_TheRowCountIsTheRunCount_AndTheTextRoundTrips` (5 seeds × 200
  generated interleavings of 0–39 events over 7 kinds × 4 lanes: `rows.Count == runs(events)`,
  the concatenated text round-trips, `Σ(Chunks ?? 1) == events.Count`, every row's `At` / `Lane` /
  `Kind` is its first event's).
- **Oracle (and why it's trustworthy):** the theory's `Runs()` is written independently of the
  fold (a change of kind or lane, or any non-folding kind, starts a run) — a fold that merged
  across a tool call, across lanes, or across kinds, or that dropped the last open run, changes
  the count; the text round-trip catches a lost or duplicated chunk; the chunk sum catches an
  off-by-one in the count. Seeded `Random` (D0).
- **Red observed before green:** **yes** — 8 of 16 red against a stub at today's grain (one row
  per event: `Expected: 3 / Actual: 5`; `["agent.thought","agent.msg","agent.thought"]` vs five
  rows; the theory `Expected: 28 / Actual: 36` at seed 65537), then 16 of 16 green with the fold.
- **Confidence:** Verified.
- **Residual risk:** `Runs()` shares `Coalesce.Folds` with the fold (the foldable set is one
  definition); a mutation that adds a kind to `Folds` passes both — the explicit per-kind theory
  (`EveryOtherKind_…`) is the guard for the six named kinds, not for an unnamed one. Mutation
  tooling is not wired: mutation confidence is **Inferred** from the hand-walked mutants
  (`&&`→`||` in `Continues` → the lane and kind cases fail; dropped final `Close()` → the operator's
  case yields 0 rows; `chunks++` removed → the sum law fails).

### Claim 2 (C3): the thread's `TurnView` carries the fold, not a reply blob — `Rows == Coalesce(Events)` on every snapshot; `Reply`, the `StringBuilder` and `Conclude(reply:)` are gone
- **Evidence:** `tests/AiDe.Core.Tests/Presentation/Sessions/RunChannelSessionThreadTests.cs`:
  `TheTurnView_CarriesTheFold_NotAReplyBlob` (six appends incl. a tool call → `Rows.Count == 3`,
  `[3, null, 2]`, `Rows == Coalesce.Rows(Events)`; `typeof(TurnView).GetProperty("Reply")` is
  null; no `StringBuilder` member on any nested type of the fold; no `reply` parameter on
  `Conclude`), `EachSnapshot_RowsAreCoalesceOfItsOwnEvents` (`[0, 1, 1, 2, 3]` rows across the
  raises; the live message re-folds in place), `APreloadedTurn_KeepsItsRows`. M4's
  `TheReadModelPublishesOneSnapshotPerAppliedEventTests` re-pointed from `"line 3line 6line 9" ==
  Reply` to the rows.
- **Oracle:** the reflection asserts are the ruling's *"a second store of the text fails"* made
  mechanical — they are red the moment an accumulator or a reply channel returns; the behavioural
  asserts would fail against a `Rows` computed at the wrong grain.
- **Red observed before green:** **yes** — red on `Assert.Null(typeof(TurnView).GetProperty("Reply"))`
  (`Actual: System.String Reply`) with `Rows` added and `Reply` still present, then green once
  `Reply`, the `StringBuilder`, `ReplyKind` and the parameter were removed.
- **Confidence:** Verified.
- **Residual risk:** none named. (The first draft also scanned the private nested `Turn` for a
  `StringBuilder` and `Conclude` for a `reply` parameter — private structure, D1; the Test
  Architect had them deleted: the absence of a second store is proven by
  `EachSnapshot_RowsAreCoalesceOfItsOwnEvents` and the reply-side oracle below, not by reflection.
  The `Reply`-absent assert stays as the retirement's characterization.)

### Claim 3 (C2, DS-1's M1 re-pointed): the split's rows equal heading + `Coalesce(turn.Events)` of every turn, in order — an identity over `Turns`, never an equation between two sources
- **Evidence:** `tests/AiDe.App.Tests/Sessions/Thread/TheThreadIsOneListTests.TheSplitsRows_EqualHeadingPlusCoalesceOfEveryTurn`
  over `ThreadFixtures.Five()` (each answer arrives in three chunks): the rendered `split.Rows`
  keyed `kind|text|chunks` equal the expected sequence; `split.Rows.Count == Σ turn.Rows.Count +
  turns.Count`; `Σ Events.Count > Σ Rows.Count` guards that the fixture actually folds (the identity
  would hold at either grain on a fixture with no run); ordinals non-decreasing (grouped by turn);
  the header's spend unchanged.
- **Oracle:** `ConsoleSurface.Derive` reads `turn.Rows`, so the split cannot disagree with the
  thread's fold — the test compares the rendered list to `Coalesce.Rows(t.Events)` computed in the
  test, which fails when `Derive` emits per event, drops a heading, or reorders.
- **Red observed before green:** **yes** — red with the old `Derive` (b2's answer as three rows:
  `"agent.msg|Done. LayoutStore.Load now refuses an|1", "agent.msg| envelope whose schema…|1", …`),
  then green over `turn.Rows`.
- **Confidence:** Verified.
- **Residual risk:** the fixture is labelled synthetic (the working lines are tool rows so that only
  the answer folds); a real session's mix is the operator's screenshot — C4 renders that case.

### Claim 4 (C4): the split's message row renders `n chunks` from `Row.Chunks` — rendered, never asserted (condition 3); the row is named by its text alone; a tool row renders no count
- **Evidence:** `TheThreadIsOneListTests.TheSplitsMessageRow_RendersItsChunkCount_AndIsNamedByItsTextAlone`
  renders the operator's events (three chunks · a tool call · a one-chunk message) in a `Window`
  and reads the realized containers: the message row's `ThreadText`s are `[<local clock>,
  "claude-code", "message", "3 chunks", "merges are missing from the tracker and"]`;
  `AutomationProperties.Name` of the row is the text alone and its `ItemStatus` is `claude-code ·
  message` (the UX lens's F2 — attribution and kind for assistive tech, never the count); the tool
  row's texts contain `tool.call` and no `chunk`/`chunks`, status `claude-code · tool.call`; the
  one-chunk row renders `1 chunk` (the Test Architect's singular oracle); the row count is 4.
- **Oracle:** the count is read from the rendered visual tree, not from `Row.Chunks`; a template
  that dropped the binding, bound the wrong facet, or named the row with the count fails.
- **Red observed before green:** **yes** — red (`Expected: 3 / Actual: 5` rows, then no `"3 chunks"`
  in the rendered texts) with the transitional per-event `Derive` and the shared event-line
  template, then green with the row template; the `ItemStatus` assert red (`Expected: "claude-code
  · message" / Actual: ""`) before the container style carried it.
- **Confidence:** Verified.
- **Residual risk:** the thought row's `dim` rendering is exercised by no frame (Ruling 82
  condition 1: the corpus has no `agent_thought_chunk`; CV-5.3 captures one) — its shape is
  **Inferred**.

### Claim 5 (C5): with the session docked at Left, the split opens in the Center zone as a `Console — <session>` document (Ruling 89) — **RUN-PENDING**
- **Evidence:** none yet. Ruling 89 arrived mid-slice (conductor's message; merged from `main`
  `38afb30b`). Its lane-by-seam clause puts the `console` kind's factory row and zone admission on
  **SH-4.2** (Shell). Filed: seam request **`req-01M2E1HEMCMHRW1AX4SY8RJFQ7`** (`coord-core.py
  request list`) naming the exact `SurfaceContentFactory.Kinds` row, the `Func<Surface,
  ConsoleSurface?>` resolver, the identity `console:<sessionId>`, the caption `Console — <session
  name>`, the open/focus-through-a-verb rule, the count ≤ 1, and close-with-the-session.
- **Oracle (when it runs):** `Sessions/ConsoleSplitPlacementTests.WithTheSessionAtLeft_TheSplitOpensInTheCenterZone_AsAConsoleDocument`
  — through the real shell (DC-135): the Center zone holds a `console` surface with id
  `console:<id>` after the toggle; the Left stack never contains it; a second toggle leaves the
  count at 1; closing the session document removes it.
- **Red observed before green:** n-a (RUN-PENDING on the seam).
- **Confidence:** Flagged (unimplemented, by ruling).
- **Residual risk:** until SH-4.2 lands, the split still opens inside the session document's own
  grid (the Left pane at 673 px — IA-6) — the operator's density finding stands as filed in the
  review's §7.

### Claim 6: a refused run's sentence is a line of the run, not a reply stored beside the outcome
- **Evidence:** `tests/AiDe.App.Tests/Conductor/ASendLaunchesAGovernedRunTests.PressingSendOnASessionDocumentComposesAGovernedRun`
  extended: after the refused run (`no-such-engine`) concludes Failed, `turn.Events[^1]` is a
  `stderr` line under lane `no-such-engine` whose text equals `document.LastRunFailure`, and
  `turn.Rows[^1].Text` reads it — through the real composition root (`CompositionRootLedger`).
- **Oracle:** the assertion reads the event stream the fold and the Console both derive from; a
  sentence that reaches only a field would leave `Events` empty.
- **Red observed before green:** **yes** — red with the product's `Append(stderr)` line commented
  out (a transient scratch, restored in the same step; `grep -c "//RED"` = 0 before the commit).
- **Confidence:** Verified.
- **Residual risk:** the kind `stderr` is the presentation's danger-ink kind (the fixtures' b17
  line and the event-line trigger), not a mapper kind — recorded as an `assume:` in the register
  entry below; CV-5.3's non-conversation fold treats it as evidence, which is the intended reading.

### Claim 7: every clock on the surface is the operator's local time — the split row, the thread's fold line and the heading agree (E12)
- **Evidence:** C4's last block renders the same event through `ThreadFeed.EventLineTemplate()`
  and asserts the same `HH:mm:ss` local string the split row rendered; the heading already used
  `ToLocalTime()`. Found en route: the event line rendered the UTC stamp (`AcpPeer` stamps
  `GetUtcNow()`) through a `StringFormat`, one hour off the heading above it in the operator's
  time zone.
- **Oracle:** the fixture instant carries a **+05:00** offset (the Test Architect's condition):
  a clock rendered from the stamp's own offset reads `16:29:56`, which differs from
  `at.ToLocalTime()` on every machine not at +05:00 — a UTC runner reads `11:29:56`, this machine
  (`Pacific Standard Time`, UTC−7 on the date) `04:29:56`. The first draft used a UTC instant,
  which a UTC runner could not distinguish (the register entry below records the class).
- **Red observed before green:** **yes** — the first red read `Not found: "09:29:56"` against a
  rendered `"16:29:56"` (a UTC instant, before the converter existed); the offset oracle was then
  re-observed red by reverting `ThreadFeed.Clock` to a `StringFormat` (`Not found: "04:29:56"`,
  collection `["16:29:56", …]`) and green with the converter restored.
- **Confidence:** Verified (discriminating on any runner not at +05:00).
- **Residual risk:** the invariant-culture `HH:mm:ss` is the surface's existing convention; no
  date across midnight (pre-existing, the UX lens's F7 nit).

### Claim 8: kept green — the mapper's 88-frame round-trip (untouched), `ShellContrastCensusTests`, DS-1's oracles (M1 re-pointed only), the `ThreadAnnouncementPolicy` tests (a coalesced row is not a new outcome — SC9 unchanged)
- **Evidence:** `AiDe.Core.Tests` full: **2515 passed, 1 skipped (pre-existing symlink skip), 0
  failed** (`scratchpad/gates/core.trx`); `AiDe.App.Tests` full: see the gate table below
  (`app.trx`). The Sessions + census + send-seam filter: 130 / 130 before the full run.
- **Oracle:** the suites themselves; the policy reads `TurnState` transitions, never events or
  rows (`ThreadAnnouncementPolicy.cs` unchanged; its 6 tests green), so a fold cannot announce.
- **Red observed before green:** n-a (regression floor).
- **Confidence:** Verified.
- **Residual risk:** none named.

### Claim 9: the thread's reply side renders the same `Coalesce` output — one text per message row, in order, none for a tool row (the thread's half of Ruling 81)
- **Evidence:** `TheThreadIsOneListTests.TheReplySide_RendersEachMessageRowAsText_InOrder_AndNoToolText`
  — msg×2 · tool.call · msg×2 through `RunChannelSessionThread.Preloaded` and a rendered
  `ThreadFeed`: the prose `ItemsControl`'s items are exactly `["Reading the tracker.", "Three
  merges are missing."]`, its rendered `ThreadText`s the same, and the items equal
  `View.Rows.Where(kind == agent.msg)`.
- **Oracle:** a `Prose` that dropped its kind filter renders the tool row as prose (three texts);
  one that rendered only the first run renders one — both fail here (the Test Architect's two
  named mutants).
- **Red observed before green:** **yes** — with `Prose => [.. _view.Rows]` (the filter dropped),
  `Actual: ["Reading the tracker.", "read docs/tracker.md", "Three merges are missing."]`; green
  with the filter restored.
- **Confidence:** Verified.
- **Residual risk:** the prose renders as plain text; markdown, the reasoning item, the tool item
  and the outcome-last order are CV-5.3's over this fold.

### Claim 10: the mapper's `agent_message_chunk` is the kind the fold folds (the vocabulary join)
- **Evidence:** `CoalesceTests.TheMappersMessageChunk_IsTheKindTheFoldFolds` — two frames through
  the real `AcpRunEventMapper`, their text read by `ConsoleStreamModel.TextOf`: `Kind ==
  Coalesce.MessageKind`; `Coalesce.Rows` gives one row, `"merges are"`, `Chunks == 2`.
- **Oracle:** a renamed constant on either side leaves every other oracle green (they all spell
  the kind as `Coalesce.MessageKind`) while production folds nothing — the operator's defect
  returns silently; this test reads the mapper.
- **Red observed before green:** **yes** — `MessageKind = "agent.message"` (a transient rename):
  `Expected: "agent.message" / Actual: "agent.msg"`; restored.
- **Confidence:** Verified.
- **Residual risk:** the mapper itself is untouched (CV-5.3's seam); this test reads it and edits
  nothing under `AgentPlane/`.

### Claim 11: a long message row wraps its text and keeps its chunk count in view (U9, the UX lens's F1); the thread's fold line wraps too
- **Evidence:** `TheThreadIsOneListTests.ALongMessageRow_WrapsItsText_AndKeepsItsChunkCountInView`
  — 41 chunks of an 80-character sentence in a 600 px window: the message `ThreadText` is taller
  than two lines, narrower than the list, and the `41 chunks` segment's right edge lies inside
  the list; a six-sentence `EventLine` through `EventLineTemplate` wraps likewise.
- **Oracle:** a horizontal `StackPanel` measures every child at infinite width, so a wrapping
  text never wraps and the list (horizontal scrollbar disabled) clips the tail and the count —
  exactly the rows where the count carries information. Both templates are now `DockPanel`s (the
  fixed segments docked left, the count docked right, the text filling).
- **Red observed before green:** **yes** — `the message is 16 px tall — one line of 16; it did
  not wrap` against the `StackPanel` row; green with the `DockPanel`.
- **Confidence:** Verified (rendered, measured).
- **Residual risk:** the fold line's own long-content state was pre-existing (the old chunk grain
  hid it); the same fix covers it (the class swept, C4's E12 block and this test read both).

### Claim 12: SC9 — chunks arriving, a message folding, a tool call between messages: none is announced
- **Evidence:** `TheThreadAnnouncesByTransitionTests.ChunksArrivingAndFolding_AreNeverAnnounced`
  — five snapshots of a running turn whose events grow (`mer` → `mer`,`ges are` → + `tool.call`
  → + a new message): `Next` returns nothing after the accept.
- **Oracle:** the policy reads state / request id / exit code (`SameEndpoint`); until this test
  every policy fixture had empty events, so nothing pinned "never for folds" against CV-5.3.
- **Red observed before green:** n-a — a pin (green on first run); the mutant it guards (a policy
  that reads `Rows`) does not exist today.
- **Confidence:** Verified.
- **Residual risk:** none named.

## Test coverage of the boundary set

| Boundary | Covered by |
|---|---|
| empty | `NoEvents_NoRows`; the theory's 0-event trials |
| null | `ArgumentNullException.ThrowIfNull(events)` (`Coalesce.Rows`) — the ctor's own null checks on `TurnView` |
| max / large | the theory (≤ 39 events × 200 × 5 seeds); the split's 10,001-row virtualization row (`ARunningTurnShowsItsLastFourLines_TheFoldIsBounded_AndTheSplitVirtualizes`, re-based on tool rows so the count stays 10,001) |
| malformed / hostile | `HostileText…` in `TheThreadAnnouncesAndExposesRealPropertiesTests` — the reply as two chunks folds to one prose row rendered as plain text (no `Hyperlink`, no `Deny` button) |
| interleaving | condition 2 (`AToolCallBetweenChunks_…`); the lane case; the theory |
| concurrent | unchanged — the fold runs under the read model's gate; `Rows` is computed inside `View()` under the lock |
| unhappy (a refused run) | Claim 6 |

## Change reach & instrumentation

### Change-surface completeness (E7/E8)

| Surface | Reached? | Where (file / member) |
|---|---|---|
| store / run channel | untouched (the chunks stay; the mapper drops nothing) | `AcpRunEventMapper` (no change) |
| domain / model | yes | `Coalesce.Rows`, `TurnRow`; `TurnView.Rows` |
| service | yes | `RunChannelSessionThread` (no accumulator; `Conclude` without text; the refusal line in `SessionDocumentSurface.RunOneAsync`) |
| projection / wire | in-process (n-a) | — |
| client type | yes | `TurnItem.Prose` / `HasProse`; `ConsoleSplitRow.Line(Ordinal, TurnRow)` + facets |
| UI render | yes | `ThreadFeed.ProseRowTemplate` (the reply side), `ConsoleSurface.LineTemplate` (the split row), `LocalClock` |
| compute reader | yes | `ConsoleSurface.Derive` (the split), `TurnItem.Prose` (the thread) — two readers of one derivation (DM7) |

| New / changed field | Writer traced (where) | Compute reader traced (where) |
|---|---|---|
| `TurnView.Rows` | `TurnView` ctor from `Events` (one writer) | `ConsoleSurface.Derive`; `TurnItem.Prose` / `HasProse` |
| `TurnRow.Chunks` | `Coalesce.Rows` | `ConsoleSplitRow.Line.ChunksText` / `HasChunks` (rendered) |
| `TurnRow.Kind` | `Coalesce.Rows` | `Line.KindWord`, `Line.IsThought`, the stderr trigger; `TurnItem.Prose`'s filter |
| the refusal `stderr` line | `SessionDocumentSurface.RunOneAsync` | the fold (`FoldedEvents`), the split row, `Rows[^1]` |
| `TurnView.Reply` (removed) | — | — (no reader survives: `grep -rn "\.Reply\b" src` → 0 in Presentation/Sessions and Workbench/Sessions) |

### Operator questions instrumented (IO2 / IO10)

| Operator question | Emitting source | Observed once? |
|---|---|---|
| how many chunks made this message? | the row's `n chunks`, rendered on the normal path from the fold (no flag) | yes (C4: `3 chunks`) |
| did the run fail, and why? | the outcome word + the `stderr` line under the lane | yes (Claim 6) |
| how many events, how many rows? | `N events` on the outcome line (unchanged); the split's row count is `Σ Rows + turns` (C2) | yes |
| when did each row start? | the row's clock — local, one conversion | yes (Claim 7) |

No new telemetry: the `thread.*` diagnostic records are unchanged (`ThreadRecordsExist_CarryNoText_AndAStopIsOneActionRecord` green); the fold is pure and in-process.

## Failure modes addressed

| Failure mode | Handled in code by | Proven by (test) | or Accepted |
|---|---|---|---|
| no events / a turn with no answer | empty rows; `HasProse` false hides the prose block | `NoEvents_NoRows`; the fixtures' running turn | — |
| null events | `ThrowIfNull` | (ctor contract; a `[]` never null in the fold) | accepted: the read model never passes null |
| a message interrupted by a tool call | the run closes at any non-folding kind | `AToolCallBetweenChunks_BreaksTheMessageIntoTwoRows` | — |
| two lanes' chunks interleaved | the run closes at a lane change (a row carries one lane) | `AnotherLanesChunk_StartsANewRow` | — |
| a thought beside a message | kind is part of the run key | `ThoughtChunks_FoldSeparatelyFromMessageChunks` | — |
| the last run never closed | `Close()` after the loop | the operator's case (3 chunks, nothing after) | — |
| a chunk count of 0 rendered on a tool row | `Chunks` null for a non-folding kind; `HasChunks` collapses the segment | `EveryOtherKind_…`; C4's tool row | — |
| the refusal's text lost with `Reply` | appended as a `stderr` line before `Conclude` | Claim 6 | — |
| a UTC clock beside a local heading | `LocalClock` on both templates | Claim 7 | — |
| a long message clipped, its count off screen | `DockPanel` rows (text fills, count docked right) | Claim 11 | — |
| a fold announced (SC9) | the policy reads state only | Claim 12 | — |
| the reply's text stored twice | the accumulator removed | C3's reflection asserts | — |

## Threats addressed (adversarial analysis)

| Boundary / threat | Disposition | Enforcing code | Negative security test | Result |
|---|---|---|---|---|
| lane text rendered in the thread / split: markup or links executed | mitigate (unchanged) | plain `ThreadText`, no `Hyperlink`, no control from text | `HostileText…` (the reply now as two chunks) | green (unchanged) |

No new trust boundary: the fold is pure over text the surface already rendered.

## Privacy findings addressed (LINDDUN-lite)

| Data flow / finding | Disposition | Enforcing code | Privacy test | Result |
|---|---|---|---|---|
| none new — no telemetry, no persistence, no new field leaves the process | n-a | — | `ThreadRecordsExist_CarryNoText…` still green | — |

## Reviews (Adversary Mode, read-only, fan-out 3) — as received, and what was done

**Test Architect — VETO (PASS-WITH-CONDITIONS), cleared by the review commit.** Conditions:
(1) a Proof Pack with the red outputs — this document; (2) C4's clock oracle non-discriminating on
a UTC runner — the +05:00 instant (Claim 7); (3) the reply side's half of Ruling 81 had no direct
oracle (`Prose` with the filter dropped or `Take(1)` uncaught) — Claim 9; (4) the mapper→fold
vocabulary join untested (a renamed `MessageKind` folds nothing silently) — Claim 10. Minors
taken: `Runs()` spells its own foldable set (no longer reads `Coalesce.Folds`); the private-
structure reflection asserts deleted (Claim 2); `"1 chunk"` rendered and asserted (Claim 4); the
SC9 pin (Claim 12). Nit recorded: `Ordinal → OrdinalIgnoreCase` on the lane/kind comparison has no
case-varying fixture (harmless). The mutation table (all caught, named) is the register's evidence
that D1's mutation sense holds without tooling.

**The Simplifier — PASS-WITH-CONDITIONS, applied (net −40 lines of its −51).** `Coalesce.Rows` is
an index scan over `string.Concat` (no local function, no `StringBuilder`); `Folds` is private and
the redundant `Folds` clause in `Continues` is gone; `HasProse` and its visibility binding deleted
(an empty `ItemsControl` measures zero); the split's `Mono()` and its copied stderr style are gone
— the row is built from `ThreadFeed.Text` / `Clock` / `Segment` / `StderrInk` / `Visible`, now
internal (one grammar, two surfaces); `ThreadFixtures.Reply` is `Enumerable.Chunk`. **Not
applied, with the one-line rationale the condition asked for:** `LastRunFailure` stays — it is the
document's pre-existing refusal record, read by the send-seam tests, and whether the *stopped*
sentence is owed as a line is a CV-5.3 question (the non-conversation fold), recorded under
next steps. **The thought word and its dim ink are deleted** (the Simplifier's yagni; Ruling 82
condition 1 — no frame is captured; the fold of `agent.thought` stays as Ruling 81 rules in
writing). Residual risk recorded: `Fold()` re-projects every concluded turn on every publish
(pre-existing, O(ΣE) per appended event; the fold adds a constant factor) — a measurement before
a change, next steps.

**UX & Accessibility — PASS-WITH-CONDITIONS, cleared.** F1 (Major, Inferred → **Verified red**):
the horizontal `StackPanel` never wrapped and clipped the count — fixed as `DockPanel`s on both
templates with the red-first long-content test (Claim 11). F2 (Minor): `ItemStatus` = `<lane> ·
<kind word>` on the split's container, name unchanged (Claim 4). F3 (Flagged): an empty-text row
— **does not arise**: `ConsoleStreamModel.TextOf` falls back to the kind (`:266-269`), the refusal
line carries its sentence, and every fixture line has text. F4: thought-row contrast cleared at
6.48:1 / 6.39:1 from the tokens — moot now that the dim ink is CV-5.3's; the finding that **the
census does not reach the split** (its Console mode seeds a model the surface no longer reads)
is routed to the census's owner as a next step. F5: `1 chunk` kept (the mockup's own rendering).
F6–F8: no regression; no tells; the 20 px rhythm holds.

## Gate record

| Gate | Result |
|---|---|
| `dotnet build` Core / App / Core.Tests / App.Tests `-p:TreatWarningsAsErrors=true` | 0 warnings, 0 errors (all four) |
| `AiDe.Core.Tests` full (`--logger trx`) | 2515 passed · 1 skipped · 0 failed · 1 m 53 s |
| `AiDe.App.Tests` full (`--logger trx`) | see the closing report (filled at close) |
| `python tools/run-verify-gates.py` | see the closing report |
| `regenerate-derived.py` after the audit entry | see the closing report |

## Deviations and findings (not scope)

- **Finding, routed:** the split's home in Center (Ruling 89) — SH-4.2, seam request
  `req-01M2E1HEMCMHRW1AX4SY8RJFQ7`; C5 RUN-PENDING.
- **Finding, fixed as the class it belongs to:** the event line's UTC clock (Claim 7) — one
  converter, both templates; the shell's other clocks were not swept (the header's, the jump
  list's `hh:mm` already convert).
- **Deviation recorded:** the fixtures' conductor lines carry `run.accepted` / `run.completed`
  (the mockup's event model) — not mapper kinds; labelled synthetic in `ThreadFixtures`.
- **Next step (CV-5.3):** the thread's reply side renders the prose rows as text; the item kinds
  (reasoning collapsed, tool call + result, outcome last, `N events` over non-conversation rows
  only), the `thought` word and its dim ink in the split, and whether the *stopped* sentence is
  owed as a line, are CV-5.3's over this `Coalesce`.
- **Next step (measure first):** `RunChannelSessionThread.Fold()` re-projects every concluded
  turn on every publish — O(ΣE) per appended event before this slice, now with the fold's
  constant factor; unmeasured. A measurement (publish latency at 40 turns × 400 events) before
  any change.
- **Next step (census owner):** `ShellContrastCensus`'s Console mode constructs a bare
  `ConsoleSurface` and seeds `Model.Console.Append`, which the surface has not read since CV-1 —
  no split row is in the census population (the contrast floor for the split's row is arithmetic
  from the tokens, 6.48:1 / 6.39:1, not measured on the composed tree).
