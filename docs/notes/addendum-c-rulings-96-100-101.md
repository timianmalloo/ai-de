---
id: note-addendum-c-rulings-96-100-101
title: "Decision note — Rulings 96, 100, 101: the compiled prompt's open state, the bookkeeping kinds, a tool-result row's body"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, ruling, conductor, conversation-lane, composer, console, thread]
links:
  - { to: note-addendum-c-council-rulings, rel: refines }
  - { to: proof-composer-compiled-prompt-and-console-rows, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-14
summary: >-
  The Owner's Rulings 96, 100 and 101, filed verbatim as the lane composer-r96-r100-r101 received
  them, so the proof and the code that cite them can be checked against the words. Filed as a
  separate note rather than appended to the council-rulings note because a sibling lane appends its
  own rulings to that note in the same window; the conductor may fold these three in at the join.
---

# Decision note — Rulings 96, 100, 101 (Conversation lane)

The three rulings below are the Owner's words as the lane received them. The lane's evidence is
`docs/proof/composer-compiled-prompt-and-console-rows.md`; the measured cause of F-E differs from
Ruling 96's BECAUSE (which was marked Inferred and made the measurement a condition) and is recorded
there.

## Ruling 96 — F-E: the expanded disclosure must show the compiled text; collapsed at rest stands, but the open state is sticky per session

**RULING:** Operator: *"the compiled prompt thing … is expanded but shows no compiled prompt i
expected it there."* Defect: when `IsExpanded` is true the `_compiled` box renders its text at ≥
`CompiledPromptMinHeight`. Ruling 57's collapsed-at-rest is kept (the operator did not ask for
open-by-default), but **the disclosure's open state persists for the session document** — opened
once, it stays open across turns and reopen, so "expected it there" holds the second time.

**BECAUSE:** `_compiled.Text` is set on every draft change and a one-line message compiles to
non-empty bytes (the gate refuses only whitespace), so the text is almost certainly present;
`MinimumHeight` adds `CompiledPromptMinHeight` only when open, and the screenshot shows the editor
still at its rest height with 37 px under the header — consistent with the belt not re-measuring on
expand, so the reader gets 0 px. That is **Inferred**; the measurement is a condition.

**CONDITIONS:** (1) Before the fix: toggle the disclosure on a one-line message draft and record
`_compiled.Text.Length` and `_compiled.ActualHeight` — the proof doc states which was zero. (2) The
existing on-screen test extends to the **content**: after expand, `_compiled.ActualHeight ≥
CompiledPromptMinHeight` and `Text == RenderView(draft).Text`. (3) The editor floor (`EditorFloor`)
is not violated by the reopened box — INV-0007 stays green.

**Measured (the lane):** the height was zero and the text present (69 chars); the cause was the
disclosure template's one-way `TemplateBinding` on the header toggle, not the belt — the proof doc's
first section.

**RECORD AS:** Ruling 96 — the compiled prompt disclosure renders its content when open; collapsed
at rest (Ruling 57) with the open state sticky per session document; Conversation lane.

## Ruling 100 — R-3: bookkeeping frames are Console-only; the thread's fold counts the rest

**RULING:** `acp.session.update.usage_update` and `available_commands_update` are bookkeeping: they
stay Console rows and feed Spend (Ruling 78) but **do not count in, or render inside, the thread's
"N events" fold**. Owner extension of Ruling 82, marked; the list is a named constant of two kinds,
not a pattern.

**BECAUSE:** Ruling 82 already classes `acp.*` as Console evidence, not conversation; a fold that
reads "14 events" with 4 usage rows is a count that misleads about what the agent did.

**CONDITIONS:** The Console still shows every frame (Ruling 74's identity oracle M1 stays green);
the fold's count equals events minus the two named kinds (test). The exact kind strings on the
wire in the corpus are the constant's values.

**Observed (the lane):** `acp.session.update.usage_update` and
`acp.session.update.available_commands_update` — `ConversationItems.BookkeepingKinds`.

**RECORD AS:** Ruling 100 — `usage_update` and `available_commands_update` are Console-only; never
in the thread's fold; a named constant of two; Conversation lane.

## Ruling 101 — R-4: a tool-result row renders its content's first text line and byte count, never its own kind twice

**RULING:** `ConsoleStreamModel.TextOf` reads `content[]` of `ToolCallContent`: the first `text`
item's first line plus ` · <n> bytes` (total text bytes); when no item carries text, the body reads
`no text content (<n> item(s): <types>)`. The kind is never the body.

**BECAUSE:** the fallback to the kind is the row saying nothing, which is a plausible-looking
non-value (IO: never a plausible wrong number). The operator's Console showed rows reading
`tool.result   tool.result`.

**CONDITIONS:** Red-first against a frame from the 88-frame corpus whose `content` is an array; a
frame with only non-text items renders the "no text content" form. DC-187's rule holds: a text
field that IS present is the text, whitespace included. ACP's `ToolCallContent` items are
`{type:"content", content:{type:"text", text}}` or `{type:"diff", …}` or `{type:"terminal", …}` —
read from the corpus, not assumed. Both the Console and the thread's fold read `TextOf` (DM7: one
derivation) — one function.

**Interpretation the lane marked for the conductor:** the operator's kind-only rows are the corpus
shapes with no `content[]` at all (`read.jsonl:13`, `:14`); the lane reads `rawOutput` in the same
form and the zero-item form for a result with nothing, so that *"the kind is never the body"* holds
on the rows the operator saw.

**RECORD AS:** Ruling 101 — a tool-result row's body is its content's first text line and byte
count, or the no-text form; never its kind; one `TextOf`; Conversation lane.
