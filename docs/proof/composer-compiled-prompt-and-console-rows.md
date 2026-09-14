---
id: proof-composer-compiled-prompt-and-console-rows
title: "Proof Pack — Rulings 96, 101, 100: the compiled prompt opens on its header's click and keeps its floor (its open state the document's); a tool-result row never reads its kind twice; the two bookkeeping kinds are Console-only"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, conversation-lane, composer, console, thread, ruling-96, ruling-100, ruling-101, ruling-57, ruling-74, ruling-81, ruling-82, dc-187, red-first, measurement]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: proof-composer-as-conversation, rel: refines }
  - { to: proof-console-coalesce, rel: refines }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-14
review-suggested: []
summary: >-
  Lane composer-r96-r100-r101 on main dda140ba. Ruling 96 (F-E): measured at the operator's belt
  (489.5 × 517.13, 1 turn, their one-line message) through the header's own click, the compiled
  box's height was 0 (IsArrangeValid false) while its text was present (69 chars) — both disclosure
  templates bound the header toggle to IsExpanded one-way (TemplateBinding), so a click rotated the
  chevron and opened nothing; the box also had no floor of its own (29.89 px under 48 when opened by
  the property). Fixed, with the open state persisted per session document. Ruling 101: a
  tool.result row reads content[]'s first text line and byte count, rawOutput in the same form, the
  no-text form otherwise — never its kind. Ruling 100: usage_update and available_commands_update
  are a named constant of two, Console-only, never in the fold. Three commits, red-first each; App
  955 → 957, Core 2636 → 2640 executed.
---

# Proof Pack — Rulings 96, 101, 100 (lane `composer-r96-r100-r101`)

Base `dda140ba` · branch `lane/composer-r96-r100-r101` · commits **170d0366** (96) · **b77a39f5** (101) ·
**d8c48356** (100). Operator build in the ledger: `51e806f8` — the composer, thread and document files are
byte-identical to the base (`git diff --stat 51e806f8 dda140ba -- <three files>` is empty), so the
measurement below is of the code the operator ran.

## Ruling 96 — condition 1, the measurement

Test: `TheWriterKeepsItsRoomTests.TheCompiledPromptDisclosure_OpenedAtTheOperatorsBelt_RendersTheCompiledText`
(headless WPF, the composer alone under the document's Auto-row constraint, `BeltHeight = 517.13`,
`EditorRestHeight = 280`, width `489.5466`, the draft a goal block with blank lines and the operator's one
line — the ledger's `fields 1`, a Message). Read from the test output, before any fix:

| Reading | `_compiled.IsArrangeValid` | `_compiled.ActualHeight` | `_compiled.Text.Length` | `_lines.ActualHeight` | composer | editor |
|---|---|---|---|---|---|---|
| At rest (collapsed) | false | 0.00 | **69** | 181.92 | 511.88 | 280.00 |
| After the header's **click** (the operator's gesture; the toggle's `IToggleProvider.Toggle()`) | **false** | **0.00** | 69 | 181.92 (unchanged) | 511.88 | 280.00 |
| After `CompiledPromptOpen = true` (the property — the only path any oracle had used) | true | **29.89** (desired 29.89, MaxHeight 142) | 69 | 211.81 | 517.13 | 255.36 |

**Which quantity was zero: the height.** The text was present (69 chars = the message; a one-line Message
compiles to itself). After the click the toggle read `IsChecked = true` (the chevron the operator saw
rotated) and `CompiledPromptOpen` read **false**.

**The cause, read from the code (Verified):** `ThreadFeed.DisclosureStyle()` and `ToolDisclosureStyle()`
bound the template's `HeaderSite` toggle with
`toggle.SetValue(ToggleButton.IsCheckedProperty, new TemplateBindingExtension(Expander.IsExpandedProperty))`.
A `TemplateBinding` is one-way, templated parent → child. The click set the toggle's `IsChecked`; the
chevron's own trigger (on `IsChecked`) rotated it; `Expander.IsExpanded` never moved; the template's
`ExpandSite` trigger (on `IsExpanded`) never fired; the `ContentPresenter` stayed Collapsed and the
`TextBox` was never measured — 0 px, `IsArrangeValid` false, and `EmitLayout` (which only runs on a
`SizeChanged`) wrote nothing, which is exactly the ledger: `compiled: {width: null, height: null}` at
16:02:56Z and **no `composer.layout` row afterwards** though the operator opened the disclosure at ~16:04Z.
The stock WPF Expander template binds its HeaderSite `IsChecked` with a two-way `Binding` to the templated
parent's `IsExpanded` for this reason. Not the DockPanel/belt arithmetic: the belt never re-measured because
nothing asked it to.

**Correction to the brief's reading of the screenshot:** the "37 px between the header and the send row" is
12 physical px (rows 1359–1370 at DPI 1.5 = 8 DIP: the toggle's centring slack plus the send row's 4 px
top margin) — measured on the PNG (`crop-compiled.png`, background `(26,31,38)` across the gap). There was
no box in the gap, which agrees with the headless 0.00.

**Second, subordinate cause (Verified):** opened through the property, the box laid out at **29.89 px** —
its one-line content — under the 48 px `CompiledPromptMinHeight`. The floor existed only as an addend in
`MinimumHeight` and a lower bound on `_compiled.MaxHeight`; it was never set on the box, and a TextBox
desires its content. `_compiled.MinHeight = CompiledPromptMinHeight` now (collapsed, the box is never
measured, so nothing changes at rest).

## Claims

| # | Ruling · condition | Evidence (test) | Red observed (verbatim) | Green | Confidence |
|---|---|---|---|---|---|
| 1 | 96 c1 — the measurement | `TheCompiledPromptDisclosure_OpenedAtTheOperatorsBelt_RendersTheCompiledText` | `the header click rotated the chevron but never reached IsExpanded — the disclosure's toggle is bound one way` (click path); `the compiled box was laid out at 29.89 px; its floor when open is 48` (property path) | after the click: `CompiledPromptOpen True · arrange-valid True · height 48.00 · 69 chars`; `Text == Gate.RenderView(Draft).Text`; editor 237.25 | Verified |
| 2 | 96 c2 — the existing on-screen test extends to the content | `TheCompiledPromptDisclosure_IsOnScreen_AtEveryTurnCount` (0 · 1 · 40 turns, the document at 673 × 748) | `the header click did not open the disclosure` ×3 (against the pre-fix sources, stashed) | compiled 48.0 at every count; `Text == RenderView(draft).Text`; editor 476.5 / 280 / 280 | Verified |
| 3 | 96 c3 — INV-0007 stays green | the two rows above assert `editor ≥ EditorFloor` and the send row inside the composer; `UnderAShortWindow_TheEditorGivesWayToItsFloor_NeverBelow130` (4 regimes, unchanged) | — (a floor, not a new red) | 27/27 in the file; App suite green | Verified |
| 4 | 96 — sticky per session document | `TheCompiledPromptsOpenState_IsTheDocuments_AndSurvivesAReopen` | `the document's model does not carry the open state` (against the document without the wiring, stashed) | toggle → `Model.CompiledPromptOpen` → `session-document.json` (`compiledPromptOpen: true`) → a new document over the restored model opens expanded; an envelope without the field reads collapsed | Verified |
| 5 | 101 — a text item's first line · bytes; a title stays the title (DC-187) | `CoalesceTests.AToolResultsBody_IsItsContentsFirstTextLineAndByteCount_NeverTheKind` over `write.jsonl:13`, `read.jsonl:11` | `Expected: "``` · 43 bytes" · Actual: "tool.result"` | green | Verified |
| 6 | 101 — only non-text items → the no-text form | `AToolResultWithOnlyNonTextItems_ReadsTheNoTextContentForm` (`write.jsonl:11` with its title removed; two authored ACP shapes) | `Expected: "no text content (1 item: diff)" · Actual: "tool.result"` | `no text content (2 items: diff, terminal)` · `no text content (1 item: image)` | Verified (the corpus frame); Inferred (the two authored shapes follow ACP's `ToolCallContent` schema, not a capture) |
| 7 | 101 — the operator's rows: `rawOutput`, and nothing | `AToolResultWithRawOutputOrNothing_NeverReadsItsKind` over `read.jsonl:14`, `:13` | `Expected: "(Bash completed with no output) · 31 bytes" · Actual: "tool.result"` | `no text content (0 items)` for `:13`; no `tool.result` row in `read.jsonl` reads its kind | Verified |
| 8 | 101 — DC-187 kept; one function | `AWhitespaceOnlyChunk_IsItsWhitespace_NeverTheKind` unchanged (absent text on `usage_update` → the kind); `ConversationItemsTests` corpus facts unchanged; `ToolFacts.ContentTexts` the one reader of `content[]` text | — | green | Verified |
| 9 | 100 — the two kinds never fold; the count is the rest | `ConversationItemsTests.TheBookkeepingKinds_AreNeverItems_AndTheFoldCountsTheRest` over `read.jsonl` through the real mapper | `Expected: 0 · Actual: 8` (6 `usage_update` + 2 `available_commands_update` event items) | the constant equals the two observed strings; event items = non-conversation rows − 8; the rows keep all 8 | Verified |
| 10 | 100 — the Console still shows every frame (Ruling 74 M1) | `TheThreadIsOneListTests.TheSplitsRows_EqualHeadingPlusCoalesceOfEveryTurn` — run, green, unchanged (the split reads `turn.Rows`, which `Of` never touches) | — | green | Verified |
| 11 | 100 — the thread's fold header | `ThreadFeedTests.TheReplySide_RendersItemsThenTheOutcomeLine_AndFoldsOnlyNonConversationRows` falsifier 4 | `Expected: "3 events" · Actual: "2 events"` (the Core change against the old literal) | re-pointed to the arithmetic — `EventsText(nonConversation − bookkeeping)` with a positive control that the fixture carries a bookkeeping row — and the literal `2 events`; folded = `run.accepted, run.completed` | Verified |

**The bookkeeping kind strings, as the mapper emits them (Verified by mapping the corpus):**
`acp.session.update.usage_update` (`read.jsonl:8, 12, 15, 19, 20, 21`) and
`acp.session.update.available_commands_update` (`read.jsonl:5, 6`). `AcpRunEventMapper` composes
`"acp.session.update." + sessionUpdate` for every unrecognised discriminator.

## Interpretation the conductor should confirm (marked, not assumed silently)

- `assume:` Ruling 101's *"its content"* includes the adapter's `rawOutput` string. The corpus shows the
  operator's two `tool.result   tool.result` rows per Bash call are `read.jsonl:13` (nothing but `_meta`)
  and `:14` (`status` + `rawOutput`, **no** `content[]`) — the same sequence as the screenshot
  (title · title+content · usage · kind · kind · usage). The `content[]` rule alone leaves both reading the
  kind, against the ruling's *"the kind is never the body"*. Reading `rawOutput` in the same
  first-line-and-bytes form and the zero-item form for `:13` is what makes that sentence true. **Confirms:**
  the conductor at the join. **Breaks if false:** two rows read their output instead of their kind — the
  reversal is one `??` clause.
- Order kept as the ruling states it: `content[]` before `rawOutput`. `write.jsonl:13` carries both; its
  content text is the fenced form of its `rawOutput`, so the row reads the fence — see finding 3.

## Findings not changed (placeholder ids — the conductor allocates)

1. **DC-212** — *A template's header toggle bound one-way (`TemplateBinding`) to the state it
   is meant to drive: the click changes the chevron and nothing else, and every oracle sets the state
   through the property.* Signature: `SetValue(IsCheckedProperty, new TemplateBindingExtension(...))` on a
   template's toggle; a chevron trigger on `IsChecked` beside an expand trigger on `IsExpanded`; tests that
   open disclosures with `IsExpanded = …` or the Expander's ExpandCollapse peer and never the toggle's
   own click. Sweep: `grep -rn "TemplateBindingExtension(Expander.IsExpandedProperty)" src/` — the two
   sites, both fixed (every disclosure of the thread — *Thinking*, *detail*, *N events*, provenance, a
   turn's compiled prompt — had the same defect; the operator's screenshot shows their chevrons collapsed,
   so it had not been reported). Control: the two Ruling 96 tests open through the toggle's
   `IToggleProvider.Toggle()`. Fixed in this lane; the class is new.
2. **DC-213** — *A floor kept as an addend in the parent's arithmetic and never set on the
   element it floors: the parent reserves the room, the child desires its content.* Signature: a
   `XMinHeight` constant that appears in a parent's `MinimumHeight`/`MaxHeight` sums and in no child's
   `MinHeight`; an oracle that asserts the floor only on a fixture whose content exceeds it (the writer-room
   tests used the ~30-line compiled view). Control: the Ruling 96 tests over the one-line draft. Fixed.
3. **Finding (Ruling 101, not a class):** `write.jsonl:13`'s first text line is the fence ` ``` ` (the
   adapter wraps the refusal in a code block) — the row reads `` ``` · 43 bytes ``. The rule is the
   ruling's; a first *non-fence* line would read better. Not changed.
4. **Finding (pre-existing, Shell lane):** every full App run's terminal ledger shows **10 starts / 9
   stops** — `s-terminal`, constructed by
   `SurfaceContentTests.EveryKindTheFactoryClaimsToKnow_ProducesSomethingOtherThanTheUnavailablePane`
   through the factory for every known kind and never disposed. Present in other agents' runs today
   (07:58, 08:18, 08:53 local) before this lane touched anything. Not changed (not my lane's file).
5. **Finding (pre-existing):** `coord doctor` reports `regeneration 8 artifact(s) OWED` at base
   `dda140ba` — the derived views are the conductor's at the join.
6. **Finding (design, noted):** the outcome line's *N events* is the run's `RunEvent` count
   (`OutcomeView.EventCount`) while the fold's *N events* is the non-conversation rows less bookkeeping —
   two counts with one label on one turn since Ruling 82; Ruling 100 widens the gap by the usage rows.
7. **Finding (measurement gap):** the contrast census probe (`AiDe.App.ContrastProbe`) never opens the
   compiled disclosure, so the opened box's ink/ground under the real theme (`App.xaml`'s `TextBox` style:
   `TextBrush` on `SurfaceSunkenBrush` — read, not measured) is not in any census. Inferred safe.

## Attended rows for the operator

| Do | See |
|---|---|
| In a session with one turn, type a one-line message; click **Compiled prompt** | the chevron rotates **and** a mono box of at least three lines appears beneath it with the message's bytes; the editor shrinks by that much (never under 130 px) |
| Send a turn; the next draft | the disclosure is still open |
| Close the app; reopen the session from Recent sessions | the disclosure opens expanded (`session-document.json` beside `session.json` carries `"compiledPromptOpen": true`) |
| Click **Thinking** or **detail** on any thread item | it opens (the same template) |
| Open the Console | a Bash call's completion rows read `(Bash completed with no output) · 31 bytes` and `no text content (0 items)`, never `tool.result   tool.result`; a `usage_update` row is still there |
| A turn's **N events** fold | the count excludes `usage_update` / `available_commands_update` rows; the Console still lists them |

## Gates and counts

- Tests executed, before → after (trx `executed=`): **App 955 → 957** (+2 Ruling 96; Ruling 100 re-points T1, adds none), **Core
  2636 → 2640** (+3 Ruling 101, +1 Ruling 100); Core's one skipped test is the baseline's. `verify-test-run.py --update` deliberately not run — the
  recount is the conductor's at the join.
- `python tools/run-verify-gates.py`: see the report (run on the committed tree).
- Terminal ledger for this lane's runs: 10 starts / 9 stops per full App run — finding 4 (pre-existing,
  `s-terminal`); no test in this lane constructs a terminal.

## Residual risk

- The click path is measured through `ToggleButtonAutomationPeer.Toggle()` → `OnToggle()`, the same
  method a mouse click reaches through `OnClick()`; the mouse itself is not driven (headless). Inferred
  equivalence, by the WPF source; the operator's attended row above is the live check.
- The persisted flag is written on every toggle; a workspace on a read-only path logs
  `session-document.refused … not persisted` and the session keeps working (the writer catches
  `IOException`/`UnauthorizedAccessException` only — a `DirectoryNotFoundException` is an `IOException`).
- Ruling 101's `rawOutput` reading and the zero-item form are the interpretation marked above.
