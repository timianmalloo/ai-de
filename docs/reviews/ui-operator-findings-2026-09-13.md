---
id: ui-review-operator-findings-2026-09-13
title: "UI review — the operator's first manual test of the CV-2 build (2026-09-13): the editor's rest, the Console's grain, the conversation, the left dock, the Coordination perspective"
type: doc
status: draft
owner: "@timianmalloo"
phase: "Addendum C/D · Conversation lane CV-5 · Shell lane SH-4 (design node D3)"
tags: [ui-review, ux, accessibility, session, conversation, console, editor, perspective, coordination, docking, addendum-c, addendum-d, rulings-80-87, operator-findings]
links:
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: mockup-session-conversation, rel: documents }
  - { to: mockup-perspective-shell, rel: documents }
  - { to: ui-review-session-conversation, rel: refines }
  - { to: ui-review-perspective-shell, rel: refines }
  - { to: design-session-thread-itemscontrol, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-adr-0030-0032-amendment-coordination, rel: relates-to }
  - { to: adr-0031-second-docking-host, rel: relates-to }
  - { to: adr-0032-perspective-layout-slots, rel: relates-to }
review-by: 2026-12-13
review-suggested: []
summary: >-
  Elevate-mode review of the session conversation and the perspective shell against the operator's
  five 2026-09-13 findings and the Owner's Rulings 80–87. The thread now renders the fold it holds —
  prose, a collapsed Thinking line, tool call+result items, the outcome last — from the same
  Coalesce the Console split reads (an identity the mockup measures on itself); the editor fills the
  body at 0 turns and rests at 280px; a new session docks in Coding's Left zone at an extent the
  mockup measures against the 96ch measure; Coordination is host C with the five Loomkeeper kinds.
  The craft gate reads 0 on both mockups; the measured conflict is density at 1440 × 900 (one turn
  visible; zero with the startup terminal), which the ranked plan puts to the conductor with numbers.
  The plan names CV-5 and SH-4's slices with red-first oracles in DS-1's style.
---

# UI review — the operator's first manual test (2026-09-13)

*Produced by `/ui-design` (mode: **elevate**) at node D3 of the Addendum C programme, session
`d3-findings`, 2026-09-13, on `design/operator-findings-0913` over `main` `4b8d379c`. Governed by
`ui-design-craft.md` DX22–DX25 over the floors in `ui-interaction-design.md` U1–U20 and
`ui-craft-detection.md` CD1–CD20. Every finding carries location · dimension · severity · evidence ·
fix · confidence.*

**Input:** the operator's five screenshot titles, verbatim — *"need to fix text input area... should
be a larger window size so that the default isnt scrolling"* · *"console output is too fine grained"*
· *"the output should show the conversation and reasoning - just like in CLI - seems like it shows
results and tool calls instead"* · *"new sessions should default into the left dock"* · *"the
ledger-leaderboard-sessions-board views should be tied to a different left bar icon - coordination"*
(`C:\Users\malla\Downloads\ui findings 9-13-am\`, five PNGs at 2560 × 1600, opened and read by D3).
**Rulings:** 80–87 (`docs/notes/addendum-c-council-rulings.md:1300-1460`), binding; the Owner's filing
note left one call to D3 — which rendering the CLI's collapsed-thinking idiom maps to.
**Surfaces:** `docs/mockups/session-conversation.html` · `docs/mockups/perspective-shell.html` ·
`DESIGN.md` (two errata blocks) · `docs/specs/addendum-c-perspectives.md` (one appended errata block) ·
`docs/notes/adr-0030-0032-amendment-coordination.md`.
**Code opened (E15):** `RunChannelSessionThread.cs:120-145, :268-300` · `ThreadFeed.cs:530-560,
:708-760` · `ConsoleSurface.cs:118-145` · `AcpRunEventMapper.cs:15-45, :90-105` ·
`SessionDocumentSurface.cs:45, :212, :768, :884` · `ComposerSurface.cs:152, :231, :1280-1287` ·
`composer.html:35-36` · `ZoneLayout.cs:186-210` · `Perspectives.cs:7-60` ·
`SurfaceContentFactory.cs:100-132, :218-282` · `NewSessionPlacement.cs:40-70` ·
`WorkbenchShell.cs:1834-1848` · `MainWindow.xaml.cs:280-296` ·
`spikes/acp-subscription-lane/frames/{read,write}.jsonl` (the `tool_call` / `tool_call_update` /
`usage_update` shapes), `PROVENANCE.md:99-106` (no `agent_thought_chunk` captured).
**Reviewers:** UX Researcher/IA (UX-specification veto) · UX & Accessibility (a11y hard veto) · The
Simplifier (soft veto) — each a read-only sub-agent; the author cleared no veto.
**Mode:** elevate · **Tier:** T2 · **Fan-out cap:** 3 (reached, never exceeded) · **Loop cap:** 2.

## 0. Direction, in words (Stage 1)

**Who, and in what state.** The same operator as D1/D2 — one technical lead, one screen (2560 × 1600
in the screenshots), in flow — now **after their first real turn**: a 6,360-token reply that came
back as a plain-text blob of markdown source with `â€"` where a dash should be, a Console of
wire chunks, a 130px editor with a scrollbar under an empty thread, a maximized document that
refused to be moved, and four fleet views tabbed into the coding bench. They are not browsing; they
are comparing what they see with the CLI they use every day.

**The job.** Two families. **(a) The session document does not yet render the conversation the
CLI shows** — findings 1–3: the editor's rest, the Console's grain, the reply as prose · reasoning ·
tool call+result · outcome. **(b) The shell's homes are wrong** — findings 4–5: the session belongs
docked at Left, the Loomkeeper views belong on their own bench.

**What the CLI shows that the thread did not** *(the Claude Code transcript idiom — Inferred from
use, not fetched)*: streamed prose **rendered** (headings, lists, code, tables); a collapsed
*Thinking…* line the reader may open; each tool call as **one line** with its title and status and a
disclosure for its input and result; then the answer. Screenshot 3 shows the thread rendering
`## Gaps I noticed` and `|---|---|` literally, no reasoning anywhere, tool calls only in the *N
events* fold and, in the split, one row per chunk (`mer` · `ges are` · `missing from the tracker
and`). The fold Ruling 74 designed exists in the store; the thread simply did not render it
(Ruling 82's BECAUSE).

**Archetype, verified against the shape of the task.** Unchanged for the session — D1 · Generative
Stream Thread, `Layout:StreamingThread`, `Feedback:Generative+Confirmed`; reading is parallel
(a scan of outcomes), entering serial (one editor). What elevates is the **reply side's item
grammar**, not the archetype. For the shell, `Arch:HubAndSpoke` gains a fourth spoke: Coordination
is a parallel-reading `MultiPanelWorkstation` (the Loomkeeper views are read side by side and
entered nowhere — no composer on that bench). The Coding host is re-cut around the operator's own
gesture: the session docked at Left, the Center for what the session touches.

**Three adjectives, and their opposites** (added to D2's). **Legible, not literal** — the reply is
rendered markdown with no link activation, never markdown source. **Folded, not hidden** — the
reasoning is one dim line away, the tool detail one disclosure away, every wire frame in the
Console; nothing is dropped and nothing is dumped. **Docked, not maximized** — the session has a
zone; the whole tree is a gesture (Ctrl+K, Z), not a default.

**References, and what is taken.** The **Claude Code CLI** (the operator's comparable, by their own
words): the collapsed-thinking line, the one-line tool call with a disclosure, prose rendered —
*Inferred from use; the operator's screenshot title is the evidence.* **The APG feed pattern**
and the REPL transcript, unchanged from D2. **VS Code's activity bar**: a fourth icon is one more
destination, same manual-activation model (PS-R2).

**Anti-goals.** Not a chat of bubbles. Not a terminal pasted into the thread (the Console stays on
demand). Not an "always show reasoning" setting (a next step, if asked). Not a fifth perspective
for Daydreams. Not a maximized document as the default.

**Personality in three moves (delta).** *Type:* prose 13px `{typography.ui}`; reasoning and tool
lines 12px; the tool title and the Console's kind word in `{typography.mono}`; headings inside prose
at 13px `{typography.weight-medium}` — weight carries the level, never a fourth size. *Colour:* no
new role — the reasoning is `{colors.text-muted}`; a tool's status is `{colors.verified}` /
`{colors.danger}` / `{colors.text-muted}` beside a word; the rail's fourth item spends the same
accent as the other three. *Space:* items inside the reply side are 4px apart; the reply side sits
8px under the decoration line; the outcome line closes the turn.

**Triggered standards (walked at Stage 1).** UI-T1 *narrowly* (every count unit-bearing: `41
chunks`, `12 tool calls · 3 edits · 9 reads`, `Thinking · 4 s`, `≥ 1,668 episodes (capped)` —
TQ2/TQ7). UI-T2 **no** (nothing generated). UI-T3 **yes** — the reply side fronts the lane's model:
the reasoning item is a *Trust builder → Disclosure* (what the model thought, on demand), the tool
item an *Identifier* (what it did, with its status), the inert link a *Governor* (model-authored
content cannot act); wrong-answer states are D2's (the compile line, suspect, the lane error).
UI-T4 **yes** — WPF, Windows, UIA, Fluent; the mockups are direction evidence; native PASS is the
Proof Pack at the slice (§7's attended rows). The unconditional floor applies.

## 1. Verdict

*Written after the three lenses' passes (§8).* The rulings land as designed: **both mockups hold
the craft floor at 0 findings** (CD13: a floor, not a verdict), the in-page audits read 0 contrast
fails · 0 targets under 24px · 0 dangling ARIA references in every state D3 measured, and the
Console-split identity (`rows == headings + Coalesce(events)`) holds at 5 and 40 turns. **The one
structural finding is density**: at 1440 × 900 with the session docked at Left, Ruling 80's 280px
rest plus the composer's rows leave the thread **one turn** (120px); with the startup default's
terminal across the bottom the thread is **0px** and the editor is beaten to 160px. That is not a
styling defect — it is the composer's chrome and the Bottom zone's geometry, and §7 puts it to the
conductor with the measured options rather than deciding it here. The lenses' verdicts are §8.

## 2. Measurements (DX23 — measure before you diagnose)

### 2a. The corpus, as CI measures it

`python docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups` — **98 findings, unchanged**
(60 Major · 38 Minor; the 17-file corpus's standing figure, none on the two files below).
`ui-craft-gate.py docs/mockups/session-conversation.html` — **0 before · 0 after**.
`ui-craft-gate.py docs/mockups/perspective-shell.html` — **0 before · 0 after**. During the work one
Minor `cramped-padding` appeared when D3 tried 24px rows beneath the editor (the hairline needs its
3px inset) and was reverted — recorded in §3d as the rows-are-30px finding.
`tools/verify-mockup-audits.py` — **17 mockups swept headless, 0 findings** before and after.
`design-lint.py DESIGN.md --strict` — clean (one token name corrected in flight:
`weight-semibold` → `weight-medium`).

### 2b. What the screenshots show, counted

| Screenshot | What is on screen | Count / fact | Ruling |
|---|---|---|---|
| 1 *text input area* | Session in the Center, 0 turns; the editor ≈ 130px with a vertical scrollbar; the thread's caption alone in ~560px of empty island; Right zone tabbed Ledger · Leaderboard · Sessions · Board | editor : thread ≈ 1 : 4.3 at 0 turns; **1 scrollbar** on an empty editor | 80 |
| 2 *console output* | The split open; rows `16:29:56 claude-code mer` · `ges are` · `missing from the tracker and` … | **≥ 33 rows for one message** in view; 2 `usage_update` rows; 1 `acp.result` | 81 |
| 3 *the conversation* | The reply as one plain-text blob: `## Gaps I noticed`, `**…**`, `\|---\|---\|`, `â€"`, `Â§0`; no Thinking; tool calls only in the split | **0 rendered markdown elements**; **2 mojibake sequences** (`â€"` ×5, `Â§` ×2 visible) | 82, 87 |
| 4 *left dock* | Coding with Left collapsed, Center tabbed Ledger · Leaderboard · Sessions · Board, Board active ("No board posts yet."); status *"That pane move could not be applied — a collapsed panel still holds panes…"* | **4 Loomkeeper tabs in Coding's Center**; the refusal sentence persisting across 3 screenshots (9:27 → 9:33) | 83, 86 |
| 5 *coordination* | Same as 4 | the strip: `≥ 1668 item(s) (capped) · rev rev-1 · Coding perspective` | 84, 85 |

### 2c. The rendered mockup, measured (headless Edge; the in-page audit's own rows)

At the shell's startup size 1440 × 900 unless stated; the session **docked in Coding's Left zone at
extent 1.3** (Ruling 83); the Bottom zone as the harness's Layout axis says.

| Row (the audit's) | Bottom collapsed (the operator's layout) | + terminal across (startup default) | + terminal under Center only (D3's proposal) | 2560 × 1600 (the operator's screen) |
|---|---|---|---|---|
| Document height | 748px | 536px | 788px | 1,448px |
| Editor at rest, 1 / 5 / 40 turns (Ruling 80: 280) | **280 / 280 / 280** | **160** (the floor's belt; the composer alone is 550px) | 280 | 280 |
| Editor with the structure open (the belt) | 210 | 160 | 213 | 280 |
| Thread height (`prepared`) | 120px | **0px** | 120px | 828px |
| 0 turns: editor fills, no scrollbar, no band | 377px · no · no · 0px | — | — | 493px |
| Turns ≥ half visible, 5 turns, inline · grouped | 1 · 1 | 0 · 0 | 1 · 1 | 2 · 3 |
| Turns ≥ half visible, 40 turns, inline · grouped | 1 · 1 | — | — | 2 · 3 |
| Console rows = `Coalesce(events)` + headings | 82 = 82 · 5 = 5 (40 turns: 636 = 636 · 40 = 40) | same | same | same |
| Reasoning items announced | 0 | 0 | 0 | 0 |
| The words' width vs 96ch | 673 = 673px (extent 1.3; 1.25 read 665 — short by 8px, corrected) | same | same | n/a wide |
| Type sizes in thread + composer | 3 (13 · 12 · 11) | 3 | 3 | 3 |
| Contrast pairs / fails | 63 / 0 | 63 / 0 | 63 / 0 | 63 / 0 |
| Short window 1440 × 640 (`conversation`) | document 488 · editor **160** (≥ 130, the floor) · thread 81 | | | |

Shell mockup (`perspective-shell.html`): 36 pairs / 0 fails; 1 rail item selected in every
perspective; targets < 24px: 0; the headless dump reports *rail target < 44px* for every rail item on
the **original** file as well (4 before, 5 now — the CSS is 44 × 44; an attended in-browser read is
the check, §7).

**What the numbers say.** Ruling 80's rest and Ruling 82's inline conversation are each met, and
together they leave the thread one turn at the startup size in the operator's layout, and none in
the startup default. The composer's five rows beneath the editor at rest measure **30px each**, not
the contract's 24 (the hairline's 3px inset — the craft gate's own floor), so 5 × 6 = 30px of the
gap is the contract's own arithmetic. Grouping tool runs earns a turn only where the thread is
already tall.

### 2d. The token layer

No colour role added. Eleven contrast pairs added to the session mockup's audit (reasoning text,
tool title, tool status ×2, tool detail ×2, inline code, Console kind word, the Center's empty
sentence, terminal text, the rendered prose) — all pass in both themes; the `hc` block stays a
stand-in reported as *not measured*.

## 3. Findings

### 3a. Structure — archetype fit, IA, flow (UX Researcher/IA, pass 1)

*(§8a carries the lens's verbatim verdict; the findings it raised at ≥ Major are folded here.)*

### 3b. States and accessibility (UX & Accessibility, pass 1)

*(§8a carries the lens's verbatim verdict and its PASS/BLOCK predicate.)*

### 3c. Simplification (The Simplifier, pass 1)

*(§8a carries the delete-list and its `net` line.)*

### 3d. The author's findings — against the product, the specs and the design (not the mockups)

| # | Location | Dimension | Sev | Evidence | Fix | Conf. |
|---|---|---|---|---|---|---|
| A-1 | `AcpEngineProcess.cs:116-124` | 14 Accessibility / correctness | **4 Blocker** | Screenshot 3: `â€"` ×5, `Â§` ×2 — the UTF-8 bytes of `—` and `§` decoded as a single-byte page; no `Standard*Encoding` set (Verified absent) | Ruling 87: UTF-8 (no BOM) on all three streams; red-first round-trip of `—` / `§`; class registered (§10) | Verified (absence) / Inferred (runtime page) |
| A-2 | `ThreadFeed.cs:541-545` (`Text(nameof(TurnItem.Reply)…)`) · `RunChannelSessionThread.cs:132-135` | 1 Archetype fit | **4 Blocker** | The reply is one `ThreadText` bound to a `StringBuilder` of `agent.msg` chunks: markdown source rendered as text; no reasoning; tool calls only in the fold | Ruling 82: items over `Coalesce`; the markdown subset; the reasoning item; the tool item (§7 CV-5.3) | Verified |
| A-3 | `ConsoleSurface.cs:125-140` (`Derive`: one `Line` per `EventLine`) | 3 IA / density | **3 Major** | Screenshot 2: ≥ 33 rows for one message | Ruling 81: `Coalesce(turn.Events)` — one row per message with `n chunks`; M1 re-pointed (§7 CV-5.2) | Verified |
| A-4 | `SessionDocumentSurface.cs:45, :884` (`ComposerShare = 0.45`) · `ComposerSurface.cs:231` (`EditorFloor = 130`) · `composer.html:35-36` (`min-height: 110px`) | 2 State completeness / DM-A | **3 Major** | Screenshot 1: a 130px editor with a scrollbar under an empty thread; two definitions of one floor (110 vs 130) | Ruling 80: fills at 0 turns, 280 at rest; one constant pushed to the page (§7 CV-5.4) | Verified |
| A-5 | `AcpRunEventMapper.cs:35-40, :97` | 5 Wrong-answer states (UI-T3) | **3 Major** | `agent_thought_chunk` unrecognised → `acp.session.update.agent_thought_chunk` with an empty body; the corpus holds no such frame (`PROVENANCE.md:101-104`) | One mapper row after a captured frame (Spike Protocol, Ruling 82 c1); the row's shape stays Inferred until then | Verified (mapper) / Inferred (frame) |
| A-6 | `NewSessionPlacement.cs:57-58` · `WorkbenchShell.cs:1836-1846` · `MainWindow.xaml.cs:287-294` | 3 Flow integrity | **3 Major** | Maximize-on-create collapses every sibling; the reconcile then refuses every drag (*"a collapsed panel still holds panes"*) — screenshots 4/5's persisting sentence | Ruling 83: dock at Left, `Maximized == null`; `NewSessionPlacement` retires; the reconcile's blindness stays a named Shell-lane defect (§7 SH-4.2, finding F-1) | Verified |
| A-7 | `ZoneLayout.cs:193-206` · `SurfaceContentFactory.cs:223-248` (`Perspectives: [Coding]` ×5) · `Perspectives.cs:51-60` (3 rows) | 3 IA | **3 Major** | Screenshots 4/5: four Loomkeeper tabs in Coding's Center; the operator: *"a different left bar icon — coordination"* | Ruling 84: host C, Ctrl+4, the five as a set; Coding admits none; drop-with-report naming Coordination (§7 SH-4.1) | Verified |
| A-8 | The density contract vs Ruling 80 (`DESIGN.md` "Chat-like": ≥ 3 turns half visible) | 3 Density | **3 Major** | §2c: 1 turn at 1440 × 900 (bottom collapsed), 0 with the startup terminal, 2–3 at 2560 × 1600 | Put to the conductor with the four measured options (§7, "the density question"); D3's recommendation: the terminal under the Center only **and** the ≥ 3 row re-expressed per viewport | Verified (measured) |
| A-9 | `DESIGN.md` "Chat-like" rows-beneath row (*each 24px*) vs the rendered 30px | 17 Craft (hierarchy/space) | 2 Minor | The hairline row needs a 3px inset (the craft gate's `cramped-padding` rule fired at 0px); 5 rows × 6px = 30px of the editor's rest at 1440 × 900 | Amend the contract's row height to 30px (the truth) or drop the hairline between lines (a design decision for the Simplifier's list) | Verified |
| A-10 | `composer.html:35-36` (`.editor .cm-editor{font: 13px/1.5 "Cascadia Mono"…}`) | 17 Craft / DX6 | 2 Minor | DESIGN.md's *Type* move: *the composer's editor is UI type, not mono: it is a message, not code*; the page renders mono | A Conversation-lane finding for CV-5.4's page work; not ruled | Verified |
| A-11 | The Coordination default arrangement; Coding's Left extent 1.3 | 2 IA | 1 Nit (Inferred) | The Owner's default is Inferred; the extent is measured in the mockup, not in WPF | The attended rows (§7) are the evidence; the extent constant lands with SH-4.2's measured oracle | Inferred |

## 4. Scorecard by dimension (the design artifacts, after the lenses' passes)

*(Filled in §8b after pass 2.)*

## 5. Generic-tells self-check (DX3)

| Tell | Present? | Why / what was done |
|---|---|---|
| One pass deciding direction, system and composition together | No | §0 before any markup; the errata rows before the mockups; the mockups measured before the rubric |
| Bubbles, avatars, a centred column | No | The gutter ordinal, one left edge, the conversation's items in the turn's column |
| A dashboard of equal tiles for the Loomkeeper views | No | Coordination is master (Terminal sessions) + tabbed documents (Ledger first, the widest rows) |
| A fourth type size for markdown headings | No | 13px with weight; the type-size row reads 3 |
| A "show reasoning" toggle, a regenerate button, a copy control | No | One disclosure per item; nothing else |
| A rail placeholder | No | The fourth item has a body; Tests stays absent (AR3) |
| Accent as decoration | No | The fourth rail item spends the same accent; the tool status is a semantic hue beside a word |
| Cramped rows | Justified deviation | The hairline rows keep their 3px inset (the gate's floor); the cost is recorded (A-9) |
| Em-dash prose | Guarded | The fixture's `—` is a code point under test (Ruling 87), rendered in the reply; the copy strings use the entity form the corpus already carries |

## 6. The Simplifier's delete-list

*(Verbatim in §8a; what was applied in §8b.)*

## 7. Ranked plan — the part the conductor dispatches from

**The single highest improvement-to-effort change:** **CV-5.3's items projection over `Coalesce`**
— the thread renders the fold it already holds (Ruling 82). Every other Conversation-lane row is
either a precondition of it (87, 81) or a rest rule around it (80); the operator's third finding is
the one that changes what they *read* every turn.

**The density question (put to the conductor, not decided here).** At 1440 × 900 with the session
docked at Left: Ruling 80's 280px rest + the composer's rows at rest (≈ 330px, 30px per row) +
Coding's chrome leave the thread 120px (one turn) with the Bottom collapsed, and **0px with the
startup default's terminal across the bottom** (§2c). Measured options: **(b) the Bottom terminal
under the Center column only** — the session's column runs full height; document 788px, editor
280, thread 120 — a `ZoneLayout` change (Bottom spans Center, not the row) and the one D3
recommends because it also matches what the terminal is *for* (the code the session touches);
**(c) Coding's default Bottom collapsed** — the operator's own layout, cheapest, but hides the
terminal Ruling 60/83 keep in the default; **(a) grouping runs of ≥ 4 tool items on completed
turns** — D3's rule, +1 turn only at 2560 × 1600, worth keeping as a density rule but not a fix;
**(d) re-expressing the ≥ 3 row** — *≥ 1 turn in full plus the previous turn's outcome line at
1440 × 900; ≥ 3 at ≥ 1,440px tall* — the truth of the contract, not a change to the product. D3's
recommendation: **(b) + (d)**; (a) kept as an option the operator can judge on the next build.

### Conversation lane — CV-5 (in ruling order 87 → 81 → 82 → 80)

Oracles in DS-1's style: *file · name* · harness · the rows that must fail on the wrong shape.
Every slice: red-first (the red run recorded in its Proof Pack); `tools/expected-test-counts.json`
raised at the join; the 88-frame mapper round-trip stays green throughout.

| Slice | Ruling | Oracles (each red before the change) | Must **not** touch |
|---|---|---|---|
| **CV-5.1** UTF-8 engine streams (T0, first) | 87 | **E1** `AgentPlane/AcpEngineProcessEncodingTests.AFrameCarryingAnEmDashAndASectionSign_RoundTripsThroughTheReader` — headless; a child process writes the UTF-8 bytes of `{"text":"— §"}` under a 1252 console page; **red on `main`**: the reader yields `â€"`/`Â§`; after: `—`/`§`; falsifiers: `StandardOutputEncoding` left null; a BOM written to stdin (the engine must receive none); stderr decoded differently from stdout. **E2** `…TheThreeStreamEncodings_AreUtf8WithoutBom` — pure over `ProcessStartInfo`. Class registered: *a redirected child stream read with the platform default encoding* (§10). | The mapper, the thread, the shell; `composer.html` |
| **CV-5.2** `Coalesce` — the Console's message grain | 81 (74 c1 re-pointed) | **C1** `Presentation/Sessions/CoalesceTests.ConsecutiveMessageChunks_FoldToOneRow_WithFirstTimestampLaneJoinedTextAndChunkCount` — pure; 3 `agent.msg` chunks → 1 row, `Chunks == 3`, `At` = the first's; a `tool.call` between chunks → 2 rows (condition 2); `agent.thought` folds separately from `agent.msg`; `tool.*`, `permission.request`, `acp.*` one row each; empty → empty; a `[Theory]` over generated interleavings: `rows.Count == runs(events)`. **C2** `Sessions/TheThreadIsOneListTests.TheSplitsRows_EqualHeadingPlusCoalesceOfEveryTurn` (DS-1's M1, re-pointed) — **red today**: `ConsoleSurface.Derive` emits one `Line` per event. **C3** `Presentation/Sessions/RunChannelSessionThreadTests.TheTurnView_CarriesTheFold_NotAReplyBlob` — `TurnView.Rows == Coalesce(Events)`; `Reply` (the `StringBuilder`) retired; a second store of the text fails. **C4** the split's row renders `n chunks` from `Row.Chunks` (rendered, never asserted — condition 3). | The mapper's rows; `AcpEngineProcess`; `ZoneLayout`; any shell file |
| **CV-5.3** the conversation: `agent.thought` row · items · the rendering | 82 (+ the Owner's note on the thinking idiom) | **Spike first** (Ruling 82 c1): capture a real `agent_thought_chunk` frame into `spikes/acp-subscription-lane/frames/` (a prompt that provokes extended thinking); the round-trip becomes 89 frames; the row's shape is Verified only then. **M1** `AgentPlane/AcpRunEventMapperTests.AnAgentThoughtChunk_MapsToAgentThought_WithItsTextInBody` — **red today** (falls to `acp.session.update.agent_thought_chunk`, empty body); the 88-frame round-trip green. **I1** `Presentation/Sessions/ConversationItemsTests.ItemsOverTheFold_AreProseReasoningToolAndOutcome_InEventOrder` — pure over `Coalesce` rows: a `tool.call` + its `tool.result`s by id → one item with the last result's status; a result with no call → an event row, never dropped; `acp.*` → counted into *N events*, never an item; order preserved; a `[Theory]` over the five turns' goldens + `b17`. **T1** `Sessions/ThreadFeedTests.TheReplySide_RendersItemsThenTheOutcomeLine_AndFoldsOnlyNonConversationRows` — headless WPF; falsifiers: a `TextBlock` whose text contains `##` or `\|---\|`; the outcome line before the items; a tool item absent; *N events* counting a conversation row. **T2** `…TheReasoningItem_IsCollapsedByDefault_MutedAndNeverAnnounced` — `RecordingAnnouncer` records 0 across a 40-chunk thought stream; the disclosure's `ExpandCollapsePattern` is `Collapsed`; ink `TextMutedBrush`; no `LiveSetting` on it. **T3** `…ProseRendersTheMarkdownSubset_WithNoLinkActivation` — `[t](url)` yields text with no `Hyperlink` and no `InvokePattern`; headings, lists, code, tables as goldens (the subset is the slice's design-slice question). **T4** `…AToolItem_ShowsKindTitleStatus_AndItsDetailOnDemand` — `Read x` · `done`; a failed result → `failed` in `DangerBrush`; the detail pre carries input then result. **A1** (attended) the operator's screenshot-3 turn re-sent on the new build renders no markdown source. | `ZoneLayout`, `Perspectives`, `SurfaceContentFactory` (SH-4's files); `WorkbenchAnnouncer.cs`, `App.xaml` (X-3 live); `AcpEngineProcess` (CV-5.1) |
| **CV-5.4** the editor's rest | 80 | **R1** `Sessions/TheWriterKeepsItsRoomTests.AtZeroTurns_TheEditorFillsTheBody_WithNoScrollbar` — headless `Sta`, the document at 748px (1440 × 900 docked Left): editor height == body − header − caption − chrome; `ComputedVerticalScrollBarVisibility == Collapsed`; **red today**: `ComposerShare = 0.45` pins it at 130. **R2** `…AtOneAndFortyTurns_TheEditorRestsAt280_WithEqualTopEdge` — 280 ± 0 at both counts; the top edge equal. **R3** `…UnderAShortWindow_TheEditorGivesWayToItsFloor_NeverBelow130` — at 640px tall: editor ≥ 130, the thread ≥ its minimum (the belt's constant, named). **R4** `…OneFloorConstant_ReadByHostAndPage` — the page's `--editor-floor` equals `ComposerSurface.EditorFloor` (pushed on `host.init`); **red today**: `composer.html` says 110. **R5** an architecture `[Fact]`: `ComposerShare` has no references. **A2** (attended) screenshot 1 re-taken: no scrollbar at 0 turns. | `ZoneLayout` (the Left extent is SH-4.2's); the thread's items (CV-5.3) |

### Shell lane — SH-4 (84 then 83)

| Slice | Ruling | Oracles (each red before the change) | Must **not** touch |
|---|---|---|---|
| **SH-4.1** Coordination: host C, the rail entry, the slot, the allow-list | 84 | **P1** `Workbench/PerspectiveSetTests.All_HasExactlyFourRows_CodingExploreArchitectureCoordination_EachWithABoundGesture` (ADR-0030 test 1 as amended) — **red today**: three rows; `Ctrl+4` from `Order`. **P2** `Workbench/KindAllowListsTests.TheFiveLoomkeeperKinds_AreAdmittedByCoordinationOnly_AndCodingAdmitsNone` — red: `[Coding]` ×5; `Resolve("ledger", Coding) == Coordination`. **P3** `MainMenuTests.CoordinationsViewMenu_ListsTheFiveShowEntriesAndTheDisputeVerb_AndCodingsListsNeither` (ADR-0030 test 3, the mutation test) — red. **P4** `PerspectivePresenterTests.CyclingFourBodies_ReturnsTheSameInstances_AndInvokesNoFactoryTwice` (US-C2 over host C). **P5** `Workbench/ZoneLayoutSlotsTests.CoordinationPersistsToItsOwnFile_AndItsDefaultIsLeftSessionsCenterLedgerLeaderboardBoard_BottomCollapsed` (ADR-0032's third file; `<layout>.coordination.zones.json`). **P6** `…RestoringAPreCCodingEnvelope_DropsTheFiveLoomkeeperKinds_AndReportsNamingCoordination` (ADR-0032 test 1 extended) — the string *"They live in Coordination (Ctrl+4)"*; a Loomkeeper kind surviving in the Coding slot fails. **P7** the rail glyph `IconCoordination` is a keyed resource beside `IconCoding` (`MainWindow.xaml`, the recorded deviation of PS-R4); the tooltip reads the bound gesture. **Attended:** P-1 (UIA walk: five interactive rail items, 44 × 44 — the headless-dump artefact in §2c settled in-browser), P-4 (`private_bytes_delta` for host C), the announcement *"Coordination perspective — 4 panes"* and the landing on the Left zone's active tab. | The session document, the composer, the thread (CV-5); `AcpEngineProcess`; `WorkbenchAnnouncer.cs` and `App.xaml` (X-3 live — the icon goes in `MainWindow.xaml`) |
| **SH-4.2** Coding's re-cut and the left dock | 83 | **L1** `Workbench/ZoneLayoutTests.CodingDefault_IsLeftEmpty_CenterEmpty_BottomOneTerminal` — **red today**: Left holds `sessions`. **L2** `Sessions/SessionDocumentPlacementTests.ANewSessionDocument_OpensInTheLeftZone_Docked_WithMaximizedNull` (Ruling 47 c1 re-pointed) — red: `GiveItTheWholeTree`; reopen unchanged (a second row: reopen → the zone rule, no maximize). **L3** an architecture `[Fact]`: `NewSessionPlacement` has no callers (then is deleted — no dead code survives the turn). **L4** `Workbench/CenterEmptyCopyTests.WhileASessionIsOpenAtLeft_TheCenterSays_TheSessionIsDockedAtTheLeft_NeverNoSessionOpen` (condition 2) — the two copies by state. **L5** `…CodingsLeftExtent_HoldsThe96chMeasureAtStartupSize` — measured on the composed tree at 1440 × 900 (P-12's successor): the words' column ≥ 96ch + 40px gutter + padding; the constant 1.3 is Inferred until this row is green. **F-1 (a finding, ranked, not this slice's fix):** `Workbench/ReconcileTests.ADragWhileAStackIsMaximized_IsAppliedOrRefusedWithTheTrueCause` — Ruling 83 removes the trigger, not the reconcile's blindness (`RefusedReconcileAnnouncement`); the class goes to the register as a placeholder (§10). **Attended:** the operator's finding-4 gesture — create a session, drag it Center and back — no refusal. | The five kinds' allow-lists (SH-4.1); the composer and thread (CV-5); `WorkbenchAnnouncer.cs` (X-3) |
| **SH-4.3** (if the conductor takes option (b)) the Bottom under the Center only | — (D3's proposal) | **B1** `Workbench/ZoneLayoutTests.TheBottomZone_SpansTheCenterColumn_NotTheRow_WhenLeftHoldsADocument` — headless on the layout; **B2** the session document's height at 1440 × 900 with the startup default ≥ 788px (R1–R2 then hold with the terminal open). | Everything in CV-5 |

### Attended rows — the operator's next build (each row: steps · expected · the ruling it proves)

| # | Steps | Expected | Proves |
|---|---|---|---|
| O-1 | Ctrl+N, create a session | It appears **docked in the Left zone**, Center shows *The session is docked at the left.*; status has no *maximized* word | 83 |
| O-2 | Drag the new session to the Center and back to Left | Both moves apply; no *"a collapsed panel still holds panes"* | 83 (F-1's trigger removed) |
| O-3 | Look at the empty session | The editor fills the body; **no scrollbar** before typing | 80 |
| O-4 | Send one turn; read the reply | Thinking (collapsed, dim) · tool lines with titles and status · prose **rendered** (no `##`, no `\|---\|`) · the outcome line last; `—` and `§` as themselves | 82, 87 |
| O-5 | Open the Console | One row per message with *n chunks*; tool / permission / `acp.*` rows one per event | 81 |
| O-6 | With ≥ 1 turn, type past 280px | The editor scrolls; its top edge does not move | 80 |
| O-7 | Ctrl+4 | Coordination: Terminal sessions at Left; Ledger · Leaderboard · Message board tabs; status *Coordination perspective — 4 panes*; View menu lists the five; Coding's View menu does not | 84 |
| O-8 | Restart with the pre-C layout file | The strip reports the five dropped from Coding, naming Coordination | 84 (ADR-0032 rule 2) |
| O-9 | At 2560 × 1600, with a 40-turn session at rest | Count the turns at least half visible (D3 measured 2 inline, 3 grouped) — the density question's operator evidence | A-8 |
| O-10 | The refusal sentence after O-2's first attempt (if any) | Clears within 10 s or on the next announcement | 86 (X-3) |
| O-11 | The strip's `rev` | The workspace HEAD or *not recorded*, never `rev-1` | 85 (X-3) |

### Should-fix-next and worth-doing

- **Should-fix-next:** A-9 (the rows-beneath contract says 24px; the truth is 30) — amend the contract or remove the hairline; A-10 (the page's mono editor vs DESIGN.md's UI type); the SR trace's `T` badge in the mockup and DS-1's inner-stop walk (the badge now says the truth).
- **Worth doing:** an "always open reasoning" session setting **only if the operator asks** (a next step, not scope); a `TextPattern`-bearing prose element for long replies (DS-1's named upgrade); the `hc` mapping for the tool status colours (the product-wide residual).

## 8. The bounded loop (variant: findings at severity ≥ Major across the three lenses; floor 0; cap 2)

### 8a. The lenses' pass-1 verdicts (verbatim)

*(Appended when the lenses return.)*

### 8b. What was applied after pass 1, and the pass-2 residues

*(Appended.)*

## 9. Residual risk and what this review did not cover

- **The `agent.thought` frame** is uncaptured: the reasoning item's shape (text in `body`, or in
  `content[]` like a message) is Inferred until CV-5.3's spike lands the frame.
- **The Coordination default arrangement** and **Coding's Left extent 1.3** are Inferred; O-7 and
  L5 are the evidence.
- **The CLI idiom** is described from use, not fetched; the operator's screenshot title is the
  only cited evidence that the CLI is the comparable.
- **The density question** is not decided here (§7); until it is, the startup default at
  1440 × 900 shows no thread.
- **The headless dump's rail-target reading** (< 44px on every item, original and new) is
  unexplained; the in-browser strip and P-1 settle it.
- **Ruling 86's dwell** and **Ruling 85's `rev`** are X-3's; the mockups do not render them.
- The shell mockup's overflow-only tabs use `style="display:inline-flex"` beside `data-show`
  (inline style outranks the gating rule) — a pre-existing mockup defect D3 did not repeat (the
  new Left pane uses the `fx` class) and did not fix (not this run's surface); recorded for the
  mockup's owner.

## 10. Defect classes registered (CI1) — placeholders, never the register itself

- **DC-nnn (D3 a)** — *a redirected child process stream read with the platform default encoding*: the symptom is mojibake on every non-ASCII code point; the control is CV-5.1's E1/E2 (a round-trip of `—`/`§` through the reader) and a lint over every `ProcessStartInfo` that redirects a stream without naming its encoding. (Ruling 87 CONDITIONS.)
- **DC-nnn (D3 b)** — *a layout reconcile that reads only rendered zones refuses every operation while a stack is maximized*: the cause is the view the reconcile reads, not the operation; Ruling 83 removes the trigger; the control is F-1's oracle. (Ruling 83's filing note.)
- **DC-nnn (D3 c)** — *a projection's two readers use two grains of one stream* (the thread's `Reply` blob vs the Console's per-chunk rows): the control is the identity oracle C2 — the split's rows equal `Coalesce` of every turn, and the thread renders the same fold. (Ruling 81.)
- **DC-nnn (D3 d)** — *a design contract that states a row height the craft floor cannot produce* (24px rows beside a hairline need a 3px inset → 30px): the control is the mockup's rendered-geometry row, which reads the truth; the contract must cite a rendered number, never a CSS constant. (A-9.)

## 11. Artifacts

- `docs/mockups/session-conversation.html` — states added: `empty` (re-cut: the editor fills), `first` (280 at rest; the conversation live), `long` (40 turns docked Left), `conversation` (the fold as items, Thinking and a detail open), `split` (message rows); axes added: Layout (4), Tool runs (2), Viewport +short +operator; audit rows added: rest 280 · 0-turn fill · geometry · the `Coalesce` identity · reasoning announced · the 96ch fit · density twice · disclosures per item; 11 contrast pairs added (63).
- `docs/mockups/perspective-shell.html` — the fourth rail item and its tip, View → Coordination (Ctrl+4) and the two derived groups re-cut, `#p-coding` re-cut (Left = the session pane, Center = the two empty copies, overflow's code viewer), `#p-coord` (four states), the `loom` restore report, the SR trace for the landing.
- `DESIGN.md` — *Errata after Rulings 83–84* (10 rows) · *Errata after Rulings 80–82 and 87* (15 rows); lint-clean.
- `docs/specs/addendum-c-perspectives.md` — one appended errata block (Rulings 83–84); no rewrite.
- `docs/notes/adr-0030-0032-amendment-coordination.md` — the ADR amendment note.
- `docs/mockups/session-conversation.md`, `docs/mockups/perspective-shell.md` — hub nodes updated.

| | |
|---|---|
| **Completed** | The two mockups elevated to Rulings 80–84/87 with every ruled state rendered and measured; the DESIGN.md errata; the spec errata block; the ADR amendment note; the rubric critique; the ranked plan with named oracles for CV-5 and SH-4; the attended rows. |
| **Remaining** | The density question (the conductor); the lenses' pass-2 residues (§8b); the `agent_thought_chunk` capture (CV-5.3's spike); the attended rows on the next build. |
| **Best next action** | Dispatch **CV-5.1** (Ruling 87, T0, red-first) now, then **SH-4.1** (Ruling 84) in parallel with **CV-5.2** — the two are independent by file; CV-5.3 waits on CV-5.2's `Coalesce` and its spike's frame. |
