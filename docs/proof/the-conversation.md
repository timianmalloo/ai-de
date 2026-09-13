---
id: proof-the-conversation
title: "Proof Pack — CV-5.3, the conversation: the thread renders prose · reasoning · tool call+result · outcome in event order over Coalesce; agent_thought_chunk becomes a mapper row (Ruling 82)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, conversation-lane, cv-5, cv-5-3, ruling-82, ruling-81, ruling-87, agent-thought, markdown-subset, session-thread, tool-item, reasoning-item, sc7, sc8, sc9, sc10, dm7, spike]
links:
  - { to: proof-console-coalesce, rel: refines }
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: ui-review-operator-findings-2026-09-13, rel: implements }
  - { to: design-session-thread-itemscontrol, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: mockup-session-conversation, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-13
summary: >-
  Evidence for CV-5.3 on the Conversation lane: the spike captured a real agent_thought_chunk
  frame (19 chunks, content.text — the shape is Verified, not Inferred; the adapter forwards a
  thought only under thinking.display "summarized"); the mapper row agent.thought landed with M1
  red first and the corpus round-trip over 156 frames; ToolFacts carries what a tool frame states;
  ConversationItems is the one pure projection over Coalesce rows (a call and its results by id
  are one item, a result with no call an event row, acp.* events, order preserved — I1's seven
  goldens plus the two captured runs); ProseMarkdown is the golden-tested subset; the thread's
  reply side renders the items then the outcome line whose fold holds only the non-conversation
  rows, the reasoning item collapsed · muted · never announced, the tool item kind · title · status
  with its detail on demand, interrupted on a stopped turn, in-place updates with focus kept, and
  the entry stop on a live or failed last turn (T1–T9 red → green). Two classes registered as
  placeholders (CV-5-3 a, b). A1 is RUN-PENDING with steps.
---

# Proof Pack: CV-5.3 — the conversation

- **Change:** `lane/conversation-cv5-3` from `main` `06791208` (CV-5.2 joined). Commits
  `b733ea57` (the spike's frames, the `agent.thought` mapper row, M1, the `TextOf` whitespace fix)
  and `68dc6ab5` (the conversation: Core projection, the markdown subset, the App rendering, the
  oracles), then the review commit (below). **Spike:** `spikes/acp-subscription-lane/probe-thought.js`,
  `frames/thought.jsonl` (65 lines) · `frames/thought.sent.jsonl` (3), `frames/PROVENANCE.md`
  (the capture's section). **Core:** `AgentPlane/AcpRunEventMapper.cs` (the row),
  `Presentation/Sessions/ToolFacts.cs` (new), `ConversationItems.cs` (new: `ConversationItem`,
  `ToolStatus`, `ConversationItems.Of`), `ProseMarkdown.cs` (new: `ProseBlock`, `ProseInline`,
  `ProseMarkdown.Parse`), `Coalesce.cs` (`TurnRow.Tool`), `SessionThread.cs` (`EventLine.Tool`,
  `TurnView.Items`), `ConsoleStreamModel.cs` (`TextOf`: a present text field is the text).
  **App:** `Workbench/Sessions/ConversationRow.cs` (new), `ProseView.cs` (new), `ThreadFeed.cs`
  (the item templates, the tool disclosure, the ring/ink styles, `EntryStop`, the container's
  local tab scope), `TurnItem.cs` (`Conversation`, the fold over event rows), `FeedList.cs`
  (`EntryStop`), `ConsoleSurface.cs` (`KindWord` *thought*), `SessionDocumentSurface.cs`
  (`RunSink`: `ToolFacts.Of(evt)` — one line, claimed and released). **Tests:** listed per claim.
- **Spec / design:** Ruling 82 (with 81, 87; the Owner's filing note on the thinking idiom) in
  `docs/notes/addendum-c-council-rulings.md`; `DESIGN.md` §"Errata after Rulings 80–82 and 87"
  (the reply row, the events row, SC7–SC9 as amended, motion, copy); the mockup
  `docs/mockups/session-conversation.html` (`items()`, `md()`, `toolLine()`); the review's §3
  (the CLI idiom) and §7 CV-5.3 row (`docs/reviews/ui-operator-findings-2026-09-13.md`) — the
  oracles below verbatim.
- **Tier:** T2 (fan-out 3: UX & Accessibility (hard), the Test Architect (hard), the Simplifier —
  read-only).
- **Author / date:** Claude Opus (session `cv-5-3`, track CV-5.3 under `conductor-addendum-c`),
  2026-09-13.

**The operator's words:** *"the output should show the conversation and reasoning — just like in
CLI — seems like it shows results and tool calls instead."* Their screenshot 3 read
`## Gaps I noticed`, `| What | Result |`, `|---|---|` as raw text with no Thinking and the tool
calls only in the split. **After this slice a turn shaped like that one renders** (T1, T3, T4):
`Thinking ▸` (collapsed, muted) · `● read Read LayoutStore.cs done detail ▸` ·
`● execute dotnet test --filter LayoutStore failed detail ▸` · the heading **Gaps I noticed** ·
the paragraph with `—` and `§` as themselves and *the review (docs/reviews/r.md)* as text · the
table by hairline · the bulleted list · the code block in mono on the sunken ground · then
`✓ completed · claude-code · 1 edit · … · 3 events ▸`.

## The execution graph (GO1–GO19), planned vs actual

Planned: spike → mapper row (M1) → data model (`ToolFacts` on the line and the row) →
`ConversationItems` (I1) → the markdown subset (goldens) → the App rendering (T1–T9) → the moved
oracles → three reviews in parallel with the full App run → gates → the record. Serial through
one author on the critical path; the only fan-out the reviews (3, read-only) beside the App test
run. Actual: as planned, plus two findings on the path — the whitespace chunk (found by the
spike's frame, fixed red-first before the row landed) and `TextBlock.Text` being empty for
inline-built content (found by T1's first green, the harness re-pointed to the plain text with a
positive control). The `/optimize-graph` skill was not invoked: the conductor's brief carried the
graph and the oracles; this section is the record it asks for.

## The spike (Ruling 82 condition 1)

One model call on the operator's subscription (the conductor's consent), one run:
`probe-thought.js` on a session pinned read-only exactly as `docs/proof/compile-pin-spike.json`
records (`tools: []` + the disallowed list), with `thinking: { type: "adaptive", display:
"summarized" }` in the same `_meta.claudeCode.options`, the three-boxes puzzle as the prompt.
**Outcome: 19 `agent_thought_chunk` frames (272 characters), 35 message chunks, `end_turn`,
448 output tokens; model `claude-haiku-4-5-20251001`.** The shape — Verified from
`frames/thought.jsonl` — is `update.content.text`, the same as a message chunk (the review's §9
residual is closed). **A finding for the lane, not the corpus:** the adapter forwards a thought
chunk only when its text is non-empty, and its own source says recent models default
`thinking.display` to `"omitted"` (signature-only blocks, empty text) — **without the display
request there is no thought text on the wire at all**. The product's `AcpLaneClient` (CV-3's
file) must send the option for reasoning to appear in the thread; routed to the conductor.
Redaction: the email rule (`compile-session-pin-wire`'s regex) and the home-directory rule (both
spellings, from `os.homedir()`); swept after the run for `Users` in any path and the account's
local part — zero hits.

## Claims & evidence

### Claim 1 (M1): `agent_thought_chunk` maps to `agent.thought` with its text in `body.content.text`; the round-trip stays green over the whole corpus
- **Evidence:** `tests/AiDe.Core.Tests/AgentPlane/AcpRunEventMapperTests.cs` —
  `AnAgentThoughtChunk_MapsToAgentThought_WithItsTextInBody` (19 frames of `thought.jsonl`
  through the real mapper: kind `agent.thought`, `body.content.text` equals the frame's text,
  `ConsoleStreamModel.TextOf` yields it, `ext.params.update` absent — lifted not copied, the
  joined text starts *"This is a straightforward logic puzzle"*; the fact refuses a corpus with no
  thought frame); `RecognizedWireShapesMapToV1Kinds` now asserts `agent.thought > 0`;
  `EveryCapturedFrameRoundTripsWithNoFieldLost` over 6 files (88 + 68 frames).
- **Source:** `src/AiDe.Core/AgentPlane/AcpRunEventMapper.cs:38` (the row).
- **Oracle:** the kind string and the body path, read from the captured frame — a row that lifts
  the wrong key or leaves the frame unrecognised fails on the first chunk.
- **Red observed:** `Expected: "agent.thought" · Actual: "acp.session.update.agent_thought_chunk"`
  and *no agent.thought produced* (the run log of 2026-09-13, before the row).
- **Confidence:** Verified.
- **Residual risk:** none for the shape; the product's lane does not yet request the display
  (above).

### Claim 2 (DC-nnn (CV-5-3 a)): a whitespace-only chunk is its whitespace, never the kind; an absent text field still reads as the kind
- **Evidence:** `tests/AiDe.Core.Tests/Presentation/Sessions/CoalesceTests.cs` —
  `AWhitespaceOnlyChunk_IsItsWhitespace_NeverTheKind` (a `"\n\n"` thought chunk through the real
  mapper is `"\n\n"`, an empty message chunk is `""`, a `usage_update` with no text is still its
  kind — the positive control; the three-chunk thought folds to `"one.\n\ntwo."`).
- **Source:** `src/AiDe.Core/Presentation/Sessions/ConsoleStreamModel.cs` (`TextOf` / `Text`).
- **Oracle:** the literal kind in a joined text.
- **Red observed:** `Expected: "\n\n" · Actual: "agent.thought"` (M1 failed on the same chunk,
  `thought.jsonl:27`).
- **Confidence:** Verified.
- **Residual risk:** none; the sweep found no other reader of wire text with the fallback.

### Claim 3 (I1): the items projection over `Coalesce` rows — a call and its results by id are one item with the last result's status; a result with no call is an event row, never dropped; `acp.*` rows are events, never items; order preserved
- **Evidence:** `tests/AiDe.Core.Tests/Presentation/Sessions/ConversationItemsTests.cs` —
  `ItemsOverTheFold_AreProseReasoningToolAndOutcome_InEventOrder` (a `[Theory]` over the five
  review turns, `b17`, a running turn and **`b7-parallel`** — two calls open at once, their
  results interleaved, `[Tool(failed), Tool(done)]`, the one input on which by-id and by-position
  attachment differ while both ids are known (the Test Architect's 9b) — each with its golden
  item sequence, e.g. b2 `[Event, Reasoning, Tool(done), Tool(done), Prose, Tool(done),
  Tool(failed), Tool(done), Prose, Event ×4]`; every row appears exactly once as an item's row or
  attached to its call (a partition, ordered by the rows); the items keep the rows' order; every
  tool item's results carry its id; every `acp.*` row is an event), `ACallAndItsResultsById_AreOneItem_WithTheLastResultsStatus`
  (the real wire's shape: `Terminal` → the title on an update, the input, the output, `completed`
  → *done*; *failed*; *running* live / *interrupted* not), `AResultWithNoCall_IsAnEventRow_NeverDropped`
  (an orphan before and after a call, a facts-less result — three event rows, the call's results
  empty), `TheCapturedReadRun_ProjectsToOneToolItem_AndItsProse` (read.jsonl through the mapper,
  the sink's line and the fold: one item *execute · git status --short · done* over its four
  updates, `command: …\ndescription: …`, prose, no result as an event),
  `TheCapturedThoughtRun_ProjectsToReasoningThenProse` (19 chunks → one reasoning item with the
  paragraph break intact and no kind string, before the prose; the rest `acp.*`).
- **Source:** `src/AiDe.Core/Presentation/Sessions/ConversationItems.cs`, `ToolFacts.cs`,
  `SessionThread.cs` (`TurnView.Items`).
- **Oracle:** the golden sequences and the coverage identity — a projection that attaches by
  position (`b7-parallel`, `AResultWithNoCall…`), drops an orphan, makes `usage_update` an item,
  or reorders fails a golden.
- **Red observed:** against the pre-ruling projection (every row an event): 11 of 11 failed —
  `Expected: [Event, Reasoning, Tool(done), Prose, Event, Event] · Actual: [Event ×7]`.
- **Confidence:** Verified.
- **Residual risk:** the goldens' event model is the mockup's, authored; the two captured-run
  facts are the real-wire anchors.

### Claim 4 (the markdown subset): headings · paragraphs · lists · fenced code · pipe tables; code · bold · italic · inert links; outside the subset stays literal
- **Evidence:** `tests/AiDe.Core.Tests/Presentation/Sessions/ProseMarkdownTests.cs` — the
  screenshot-3 shapes (`## Gaps I noticed`, `| What | Result |`, `|---|---|`, bullets, a numbered
  list) as goldens; a fenced block verbatim and an unterminated fence to the end; the link as
  text + URL; five literal cases (`> quote`, `a * b * c`, `snake_case`, `---`, `2 * 3 = 6`);
  paragraph joining; a property over the goldens' inputs that no word of the source is lost.
- **Source:** `src/AiDe.Core/Presentation/Sessions/ProseMarkdown.cs` (111 code lines; the `simplify:` marker names the ceiling and ADR-0025's Markdig.Wpf as the upgrade trigger).
- **Oracle:** block kinds and inline spans; a parser that ate an unknown construct fails the
  literal cases.
- **Red observed:** against a one-paragraph parser: 3 of 12 failed (`Heading` vs `Paragraph`).
- **Confidence:** Verified.
- **The subset, named (the slice's design-slice question):** what the CLI's transcripts and the
  operator's replies contain — ATX headings 1–3 (one size, weight by level), paragraphs, `-`/`*`
  bullets, `1.` lists, fenced code (the CLI's commonest block; the mockup omits it — and without
  the fence rule the other rules corrupt code: a `# comment` becomes a heading, `- x` a bullet),
  pipe tables (the separator in every spelling: `|---|`, `| --- |`, `|:---:|`); inline code,
  `**bold**`, `*italic*`, `[text](url)` → *text (url)* (an autolink whose text is its URL shows it
  once). Not in the subset, kept literal: block quotes, horizontal rules, nested lists,
  underscores, images, HTML. **The D2 property** (the Test Architect's 9a):
  `OverGeneratedDocuments_TheBlocksAndTheirTextRoundTrip` — 4 seeds × 200 documents of 1–8 blocks
  assembled from the grammar parse to exactly their blocks with exactly their texts; the
  substring check it replaces is gone.
- **Reuse-in-codebase (L1):** the composer's page carries CodeMirror's markdown *language* (an
  editor highlighter) and `docs/_site` renders in a browser — neither renders in WPF; Markdig is
  not an installed dependency. Native WPF inlines by hand (`ProseView`, 114 code lines after the
  reviews' additions: the heading level, the focus ring, star columns). **The two together are
  225 code lines against the design's "≤ 200" — recorded, 25 over; the Simplifier accepted the
  hand-built subset over a new dependency on the `simplify:` marker's condition (below).**

### Claim 5 (T1): the reply side renders the items then the outcome line; the fold holds only the non-conversation rows
- **Evidence:** `tests/AiDe.App.Tests/Sessions/ThreadFeedTests.cs` —
  `TheReplySide_RendersItemsThenTheOutcomeLine_AndFoldsOnlyNonConversationRows` (no rendered
  text contains `##` or `|---|`, with the positive control that the walk sees *Red first*; the
  tool title, *Thinking* and the heading exist; Thinking above the tool item above the prose, and
  **every** item text above *completed* by geometry (the Test Architect's 2b); the fold is named
  *3 events* and holds exactly `run.accepted · acp.session.update.usage_update · run.completed`).
- **Source:** `ThreadFeed.TurnTemplate` (items → outcome → reason → actions), `TurnItem.FoldedEvents`
  / `FoldHeader`.
- **Oracle:** the four falsifiers the review named.
- **Red observed:** `Assert.DoesNotContain … matched at pos 13` — a `TextBlock` reading
  `## Gaps I noticed` (the operator's finding, reproduced in the harness).
- **Confidence:** Verified.

### Claim 6 (T2, SC9): the reasoning item is collapsed by default, muted, and never announced
- **Evidence:** `…TheReasoningItem_IsCollapsedByDefault_MutedAndNeverAnnounced` (40 thought
  chunks appended to a running turn: `RecordingAnnouncer` records 0 announcements and 0 messages;
  the *Thinking* disclosure's `ExpandCollapsePattern` is `Collapsed`; `LiveSetting` `Off` on the
  disclosure and on the opened text; the word and the opened 40-chunk text in `TextMutedBrush`
  under the app theme; still 0 after opening).
- **Source:** `ThreadFeed.ReasoningItemTemplate`, `MutedHeaderTemplate`;
  `ThreadAnnouncementPolicy` (diffs states, never rows — unchanged).
- **Red observed:** *no disclosure named 'Thinking'; the disclosures: Provenance of b6 |
  Compiled prompt of b6 | 40 events*.
- **Confidence:** Verified.

### Claim 7 (T3, T7): prose renders the subset with no link activation; the kind is text in the Control view; the heading level is programmatic
- **Evidence:** `…ProseRendersTheMarkdownSubset_WithNoLinkActivation` (no `Hyperlink` in any
  inline, no `](` rendered, the link paragraph's peer has no Invoke pattern and is a control
  element; the heading at 13 px in the token weight (`SemiBold`) with `HeadingLevel` Level2 on
  its peer and `None` on a table cell — the lens's F3/F4; the table cell *Red first*; the bold
  run; the bullet and the numbered marker; the code block in `ThreadFeed.Mono`; `— see §4`),
  `…TheToolKindIsAWord_AndTheLinkIsTextWithItsUrl` (the *execute* peer: control element, name
  *execute*, control type Text; the link line's plain text and peer name both
  *Two stores — see §4 and the review (docs/reviews/r.md).*).
- **Source:** `ProseView.Line` / `Run`, `ThreadText`.
- **Red observed:** `Assert.DoesNotContain … matched` on the raw `[the review](…)` text; *no
  ThreadText "execute"*.
- **Confidence:** Verified.

### Claim 8 (T4, T10, T12): a tool item shows kind · title · status and its detail on demand; the region is keyboard-scrollable and focus-visible; a long title trims
- **Evidence:** `…AToolItem_ShowsKindTitleStatus_AndItsDetailOnDemand` (*read* · *Read
  LayoutStore.cs* · *done* in `VerifiedBrush`; *failed* in `DangerBrush`; the title in mono; the
  disclosure *Detail of Read LayoutStore.cs* collapsed, opened → the mono pre reads `input` then
  the input then `result` then *412 lines*; the region is focusable, a tab stop, ≤ 200 px, named
  *…, input and result*; **the focus ring**: the region's border is transparent at rest and
  `FocusBrush` while it holds focus, and the compiled prompt's scroller wears the same ring — the
  UX & Accessibility lens's F2), `…PageDownInsideAnOpenDetail_ScrollsTheRegion_NeverJumpsTurns`
  (T10, the lens's A11-3: a 60-line output overflows the region — the positive control; PageDown
  with the region focused scrolls it, the caret and the focus stay; the compiled prompt likewise),
  `…ALongToolTitle_Trims_AndTheDetailToggleStaysInView` (T12, the lens's F1: a 300-character
  title trims with an ellipsis at half the measure, the detail toggle's right edge stays inside the
  turn, the full title stays the toggle's name).
- **A correction recorded:** T10's first draft read `Expected: 0 · Actual: 1` and was taken for
  a product defect (a handler was written, then deleted); it was the harness's own precondition —
  the feed's caret starts on the last turn and focusing a region inside b1 does not move it. The
  oracle now places the caret and reads it back. No product change was needed: the platform's
  `ScrollViewer` handles the page keys and `SourceOwnsItsKeys` leaves them there.
- **Source:** `ThreadFeed.ToolItemTemplate` / `ToolLineTemplate` / `ToolStatusStyle` /
  `ToolDisclosureStyle`; `ConversationRow.Detail`.
- **Red observed:** *no ThreadText "read"*.
- **Confidence:** Verified.

### Claim 9 (T5, T6, T11): interrupted on a non-live turn; a failed past turn renders its conversation and folds only its actions; the ring is the one moving element and static under reduced motion
- **Evidence:** `…AToolCallWithNoResultOnANonLiveTurn_ReadsInterrupted_NeverRunning` (a stopped
  turn whose last event is a call: *interrupted* in `TextMutedBrush`, no *running*, no visible
  `Ellipse` — with T8's ring as the positive control: a running item shows exactly one visible
  10 px ring and none once its result arrives, the Test Architect's 4a),
  `…UnderReducedMotion_TheRunningRingIsStatic_AndUnderMotionItTurns` (T11, the lens's F6: with
  `ReducedMotion` true the running item's ring is visible and its angle stays 0 after 400 ms;
  with motion the angle has moved — the positive control), `…AFailedPastTurn_RendersItsConversation_AndFoldsOnlyItsActions`
  (b17 of 40: *Thinking* and *Edit docs/audit/audit-log.jsonl* present, *interrupted* muted, no
  *Send again* / *Open the log*, the fold *1 event* — the stderr line).
- **Red observed:** *no ThreadText "interrupted"*; *no disclosure named 'Thinking'*.
- **Confidence:** Verified.

### Claim 10 (T8): a status change updates the item in place; focus survives; parallel calls take their own results
- **Evidence:** `…AStatusChange_UpdatesTheItemInPlace_FocusSurvives` (a call appended to the
  running turn through the read model, its detail opened and its region focused; the result
  appended through the same path; `Keyboard.FocusedElement` is the same region, the disclosure
  still open, the item reads *done*, the ring gone, the detail ends with the result; then two
  calls open at once with their results interleaved — each item takes its own by id, in place,
  the focus still on the first detail's region).
- **Source:** `TurnItem.MergeConversation` (positional, in place), `ConversationRow.Item`.
- **Red observed:** *no disclosure named 'Detail of dotnet test'*.
- **Confidence:** Verified.

### Claim 11 (T9, SC8 as amended): on a running last turn the entry stop is Stop, and Shift+Tab from the editor lands there
- **Evidence:** `…OnARunningLastTurn_TheEntryStopIsStop_AndShiftTabFromTheEditorLandsThere`
  (the focused container + `MoveFocus(Next)` lands on the *Stop this turn* button; from the
  composer, `FocusCurrentItemLast()` lands on the same button; the inner stops then follow in DOM
  order: *Provenance of b6 · Compiled prompt of b6 · Thinking · Detail of Read docs/proof/pp-0141.md
  · 0 events*).
- **Source:** `ThreadFeed.TurnContainerStyle` (`TabNavigation = Local`), `ActionRow`
  (`TabIndex = 0`), `ThreadFeed.EntryStop`, `FeedList.FocusCurrentItemLast`.
- **Red observed:** `Expected: Button · Actual: ToggleButton` (Tab landed on provenance).
- **Confidence:** Verified. K5 (a completed turn: provenance · compiled · the fold, then the
  composer) stays green, so a turn without actions keeps DOM order.

### Claim 12 (the split): a thought row reads *thought* and is dim; the grain untouched
- **Evidence:** `…TheSplitsThoughtRow_ReadsThoughtAndIsDim` (`KindWord` *thought* / *message*;
  the rendered text in `TextMutedBrush`). CV-5.2's C1–C4 stay green.
- **Source:** `ConsoleSurface.Line.KindWord`; `ThreadFeed.StderrInk`'s thought trigger (one
  style, both surfaces).
- **Red observed:** `Expected: "thought" · Actual: "agent.thought"`.
- **Confidence:** Verified.

### Claim 13 (E11/E12): the real composition root and the cross-surface identity
- **Evidence:** `tests/AiDe.App.Tests/Conductor/AGovernedRunReachesTheConsoleTests.cs`
  `TheSinkReceivesExactlyTheEventsTheRunCountedAndTheModelHoldsThem` — the product's `RunSink`
  (the one writer, now carrying `ToolFacts.Of`) through `GovernedRunHost.DrainAsync` to the
  rendered thread: the fold holds exactly the `permission.request` row and the conversation is
  two prose items around it. `TheThreadIsOneListTests.TheReplySide_RendersEachMessageRowAsText_InOrder_AndNoToolText`
  (re-pointed): the reply side's rows equal the non-event items of `TurnView.Items`; the split's
  identity (`TheSplitsRows_EqualHeadingPlusCoalesceOfEveryTurn`) unchanged — one fold, two readers.
- **Confidence:** Verified.

### Claim 14 (A1, attended — RUN-PENDING): the operator's screenshot-3 turn re-sent on the new build renders no markdown source
- **Steps:** build Release (below); open a session; send the same prompt as screenshot 3 (a
  read-and-report turn); read the reply. **Expected:** `Thinking ▸` (collapsed, dim) first if the
  lane sends a thought (see the finding above — until the lane requests the display, no Thinking
  line appears); one line per tool call *kind · title · status · detail ▸*; the prose rendered —
  no `##`, no `|---|`, a heading in weight, a table by hairline; `—` and `§` as themselves
  (Ruling 87); the outcome line last with *N events* counting only the `acp.*` / conductor rows.
  Open a detail: the input, then the result, in mono on the sunken ground. Tab into the turn while
  it runs: *Stop this turn* first.
- **Confidence:** Inferred until run.

## The change-surface list (E7)

mapper row (`AcpRunEventMapper`, writer) → `RunSink` (`ToolFacts.Of(evt)` + `TextOf(evt)`, the
one writer of `EventLine`) → `EventLine.Tool` → `Coalesce` (`TurnRow.Tool`; thought runs already
fold) → `ConversationItems.Of` (Core, pure) → `TurnView.Items` → `TurnItem.Conversation` (rows in
place) / `FoldedEvents` (event rows) → the templates (prose `ProseView`; reasoning disclosure; tool
line + detail region) → UIA (names: *Thinking*, *Detail of <title>*, *Detail of <title>, input and
result*; the kind as a Text element) → announcements (none: the policy diffs states) → tests + the
spike's frame. Every new field has its writer (`ToolFacts.Of`, `ConversationItems.Fold`) and its
compute reader (`ConversationRow.*`, the templates). The Console reads the rows beneath the items
(`Derive` unchanged).

## Failure modes carried through

| Mode | Disposition | Proof |
|---|---|---|
| A whitespace-only chunk | the text, never the kind | Claim 2 |
| A result with no call in the turn | an event row, never dropped | Claim 3 |
| A call with no result on a non-live turn | *interrupted*, static | Claims 3, 9 |
| A tool frame with no `toolCallId` | no facts; the row's text is the title | Claim 3 (`AResultWithNoCall…` third row) |
| Markdown outside the subset | literal text | Claim 4 |
| An unterminated code fence | runs to the end | Claim 4 |
| A link in model text | text + URL, no control | Claims 7, S1 |
| A 40-chunk thought stream | 0 announcements | Claim 6 |
| A status change under focus | in place | Claim 10 |

## Reviews (Stage 4, read-only, ≤ 3, loop cap 2; one pass each, the conditions applied in the review commit)

| Lens | Verdict | Conditions / findings | Applied |
|---|---|---|---|
| **UX & Accessibility** (hard) | PASS-with-conditions — contrast computed from `App.xaml`: muted 6.48:1 on raised / 7.46:1 on sunken, verified 6.96, danger 5.67, accent 5.73, text 13.57; the kind as text ✓; no hover-only URL ✓; the reasoning item's silence ✓ (position: the constant name *Thinking* conforms — 2.4.6 / 4.1.2; several per turn are disambiguated by reading order; keep it, never append a duration); 1.4.1 ✓ (the state is the word) | **F1** long titles clip the toggle (2.4.11) · **F2** no visible focus on the region (2.4.7; the platform's adorner ≈ 1.1:1 on sunken) · **F3** headings presentation-only (1.3.1) · **F5** A11-3's scroll unproven · **F6** reduced motion untested; minors F4 off-token weights, F7 detail empty states, F8 the empty kind's 8 px, F11 autolink twice; F9 (URL not copyable), F10 (table header relationship), F12 (no `{motion.fast}` on status settling), F13 (Expander and toggle share a name — pre-existing) recorded | F1 (title trims at half the measure, star columns; T12) · F2 (`FocusRing` on both scrollers; T4) · F3 (`HeadingLevel`; T3) · F4 (one token weight) · F5 (T10) · F6 (T11) · F7 (*(no input)* / *(no output)*) · F8 (collapsed) · F11 (once). F9, F10, F12 → next steps below. |
| **Test Architect** (hard) | PASS-with-conditions — the four Core oracle classes re-run by the lens (62 green); T8 confirmed on the real merge path; T9's entry by a real traversal; `TextOf`'s positive control present; the sweep clean | **1** the pack and the register uncommitted · **7a** the scroll oracle weakened (first press tolerance) · **4a** "never a ring" without a positive control · **9b/10** parallel calls untested · **9a** `NothingOfTheSourceIsLost` a substring check, D2 unapplied; minors 1b (redundant order check), 2b (outcome-last sampled), 3a/3b (T2's positive control is A3; `Messages` after open), 4b (the width clause), 7b (L5 ≤ vs =), 7c (the fixtures' facts-less tool rows), 7d (U5 admits an empty title), 6 (T9's name vs `FocusCurrentItemLast`), 9c (separator spellings, ordered marker) | 1 (this commit) · 7a (a settled Background pump, then the first press exactly three lines — it is) · 4a (T8) · 9b (`b7-parallel` + T8's interleaved calls) · 9a (the seeded D2 property) · 1b · 2b · 3a/3b · 4b · 7b · 7d · 6 · 9c. **7c declined, recorded**: the fixtures' `tool.call` / `tool.result` rows carry no ids (the mockup's synthetic event model); re-shaping them re-derives every fold count in DS-1's suite — the real-wire shapes are anchored by the two captured-run facts and every T-test with facts; a next step. |
| **The Simplifier** | PASS-with-conditions — no hard constraint violated (no second store; no grouping rule — D3's run-of-four has not crept back; a result with no call is an event; one item per call); the hand-built subset accepted over a new dependency on one condition | the `simplify:` marker reconciling with **ADR-0025** (Markdig.Wpf for the code viewer); `net: −55 lines possible`: the `Tool` record with computed facets (−20), `StatusWord` from the enum (−7), the kind constants in one place, `InputOf`'s options allocation and two producer-less fallbacks, `IsProse` unused, `EventRows` recomputed per facet, `ProseView.Line`'s one size, `Coalesce`'s `Folds` thrice; the flat `ConversationRow` facets → nested `Item.*` bindings (−13, Inferred); `ToolDisclosureStyle`'s shared parts (−15) | the marker · the `Tool` record · `StatusWord` · the constants · `InputOf` / `ContentText` · `IsProse` · `EventRows` once per merge · the one size · the one local. **Declined, recorded**: the nested-binding shrink (`nameof`-safe flat facets are the DM7 idiom `TurnItem` uses; the lens marked it Inferred) and the template extraction (two templates whose one difference — the header beside the toggle — is the reason the second exists; a next step if a third disclosure shape appears). |

## Gates

*(see the table at the end — filled at close)*

## Deviations and residual risk

- **The markdown renderer is 225 code lines** against the design's *≤ 200* guidance (parser 111
  in Core, renderer 114 in the App after the reviews' additions) — recorded; the Simplifier
  accepted it on the `simplify:` marker (ceiling: this subset; trigger: ADR-0025's Markdig.Wpf).
- **The lane does not request the thinking display** — until `AcpLaneClient` (CV-3) sends
  `thinking.display: "summarized"`, no `agent_thought_chunk` reaches the product and the thread
  shows no Thinking line. Routed to the conductor; the spike's PROVENANCE records the cause.
- **`TextBlock.Text` is empty for inline-built prose** (DC-nnn (CV-5-3 b)): the Shell contrast
  census labels sites by `Text`; it does not walk a thread with turns today, so nothing is
  measured wrongly, but its reach to the conversation is a routed finding for the census's owner.
- **The thinking idiom's name:** every reasoning disclosure is named *Thinking* (the design's
  copy); several in one turn share the name. The UX & Accessibility lens's position is recorded
  in the review section.
- **`/optimize-graph` not invoked** — the conductor's brief carried the graph; recorded above.
- **The fixtures' working lines** (`ThreadFixtures.Lines`: `tool.call` / `tool.result` with no
  ids) render as facts-less tool items and orphan event rows under the new projection; the moved
  oracles (K5, L4, L5, the tail) carry the derivation of their new numbers at each site.

## Next steps (captured, not chased)

- The lane requests `thinking.display: "summarized"` (`AcpLaneClient`, CV-3) — until then no
  Thinking line appears in the product.
- F9: a copy action on the turn (the URL is visible but not selectable text).
- F10: the table header relationship for AT (`HelpText` = the header on body cells).
- F12: `{motion.fast}` on a status word's settling, or the motion inventory amended.
- 7c: `ThreadFixtures.Lines` with `toolCallId`s, re-deriving DS-1's fold counts.
- The contrast census's reach to a thread with turns (its `Text` reader sees inline-built prose as empty).
- An "always open reasoning" session setting only if the operator asks (the design's own note).

## Defect classes registered (CI1)

- **DC-nnn (CV-5-3 a)** — a wire-text reader's blank-means-absent fallback turns a whitespace
  chunk into the literal kind; control: `CoalesceTests.AWhitespaceOnlyChunk_IsItsWhitespace_NeverTheKind`.
- **DC-nnn (CV-5-3 b)** — a rendered-text oracle over `TextBlock.Text` passes vacuously on
  inline-built prose; control: `ThreadFeedTests.Plain` + T1's positive control.
