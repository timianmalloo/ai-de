---
id: note-attended-rows-for-the-operator
title: "The attended rows — everything that waits on the operator's own eyes at the programme's close, one list, with the build to run it on"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c"
tags: [attended, operator, proof-pack, run-pending, addendum-c, addendum-d, conductor]
links:
  - { to: coordination-addendum-cd, rel: relates-to }
  - { to: ui-review-operator-findings-2026-09-13, rel: relates-to }
  - { to: proof-coding-recut-left-dock, rel: relates-to }
  - { to: proof-the-conversation, rel: relates-to }
  - { to: proof-editor-rest, rel: relates-to }
  - { to: proof-coordination-perspective, rel: relates-to }
  - { to: proof-mechanical-compile, rel: relates-to }
review-by: ""
summary: >-
  The programme's RUN-PENDING rows whose evidence is what the operator sees — gathered from every
  Proof Pack into one list against the final build (main 3e5b04f6, Release 1.0.0+3e5b04f6…). Rows
  whose evidence was a file or an exit code were run by the conductor and are not here.
---

# The attended rows — the operator's list

**Build:** `C:\projects\ai-de\src\AiDe.App\bin\Release\net10.0-windows\AiDe.App.exe`, ProductVersion
`1.0.0+3e5b04f6…` (Help → About shows the sha; rebuild with `dotnet build src/AiDe.App/AiDe.App.csproj
-c Release` after any pull). Each row names the ruling it proves and where its steps live in full.
Tell the conductor what you saw, in your words — a screenshot with a title is the evidence class
that produced Rulings 80–90.

## The five findings of 2026-09-13, answered (D3's O-rows; `docs/reviews/ui-operator-findings-2026-09-13.md` §7)

| # | Do | Expect | Proves |
|---|---|---|---|
| O-1 | Ctrl+N, create a session | it appears **docked in the Left zone**; the Center reads *The session is docked at the left.*; the status has no *maximized* | Ruling 83 |
| O-2 | drag the session to the Center and back to Left | both moves apply; no *"a collapsed panel still holds panes"* | 83, 88 (F-1) |
| O-3 | look at the empty session | the editor fills the body; **no scrollbar** before typing | 80 |
| O-4 | send one turn; read the reply | *Thinking* (collapsed, dim) · tool lines with title and status · prose **rendered** (no `##`, no `\|---\|`) · the outcome line last; `—` and `§` as themselves | 82, 87 |
| O-5 | open the Console | one row per message with *n chunks*; tool / permission / `acp.*` rows one per event | 81 |
| O-6 | with ≥ 1 turn, type past 280 px | the editor scrolls; its top edge does not move | 80 |
| O-7 | Ctrl+4 | Coordination: Terminal sessions at Left; Ledger · Leaderboard · Message board tabs; the status *Coordination perspective — 4 panes*; its View menu lists the five, Coding's does not | 84 |
| O-8 | restart with your pre-C layout file | the strip reports the panes dropped from Coding, naming Coordination (Ctrl+4) | 84 |
| O-9 | at your full window, a 40-turn session at rest | count the turns at least half visible (the build measured **1** at both viewports — Ruling 88's density unit is open) | 88 |
| O-10 | the refusal sentence after O-2's first attempt, if any | clears within 10 s or on the next announcement | 86 |
| O-11 | the strip's `rev` | the workspace HEAD or *not recorded*, never `rev-1` | 85 |

## The compile step (CV-2's A-rows; `docs/proof/mechanical-compile.md` §Attended rows)

| # | Do | Expect |
|---|---|---|
| A-1 | a goal-block prompt with two mentions → set tier **T1**, class **defect** on the line → copy *Compiled prompt* → Send → read the last `submitted` row's `text_sha256` in `.aide\sessions\<id>\envelope-events.jsonl` | sha256 of the copied text equals it; the `tier` row reads `T1 / operator`; `task_class` reads `defect / operator` |
| A-2 | Session settings → **Purge compile history…** → decline, then confirm → send one more prompt | the file gone then back with one envelope; `session.json` untouched |
| A-3 | after ≥ 20 sends, close the document → `AiDe.App.exe compile fold --workspace <root> --session <id>` | `compile-fold.json`: `rebuild.matched == rebuild.compared`, exit 0 |

## The conversation (CV-5.3 A1, CV-5.4 A2; `docs/proof/the-conversation.md`, `docs/proof/editor-rest.md`)

- **A1** — re-send the prompt from your screenshot 3: no markdown source; a heading in weight; a table by hairline; one line per tool call with *kind · title · status · detail ▸*; open a detail (input then result); Tab into a running turn → *Stop this turn* first. A *Thinking* line appears (the lanes now ask for it).
- **A2** — the empty session (O-3), then twenty typed lines (the editor scrolls, the page does not), then one turn (rest at 280), then a ~640 px window with the structure open (the editor gives way to 130; the send row stays).

## Coordination and the shell (SH-4.1 P-1/P-4, SH-4.2; `docs/proof/coordination-perspective.md`, `docs/proof/coding-recut-left-dock.md`)

- **P-1** — a UIA walk of the rail (Accessibility Insights or NVDA): five interactive items, 44 × 44, each tooltip carrying its gesture.
- **P-4** — `private_bytes_delta` for host C after Ctrl+4 (Task Manager's private bytes before/after; the pack's P-4 form).
- The announcement *"Coordination perspective — 4 panes."* under NVDA, landing on the Left zone's active tab; Ctrl+1 landing on the session's editor.
- The Console toggle: a second press **closes** the console document (Ruling 90); View → Console focuses it.

## The read-only turn (CV-0; `docs/proof/read-only-turn.md` §Attended run)

A question naming a file (*Explain what @src/… does*) with the lease line reading **read-only — nothing will be written** → Ctrl+Enter → then *Create docs/proof/probe.txt…* — the reply says it cannot write, no `Write`/`Edit`/`Bash` in the Console, `git status` unchanged, no `probe.txt`. (The pin itself is Verified on the wire by PD-5's three runs; this row is the lane's tree-delta path in the built app.)

## The front door (F5; `feature/exit-evidence` @ `135e05e1`, kept with its 21 commits)

The exit run the front-door slice was frozen for: File → New Session on that tree's build with `@hello.txt` in the goal and the workbench as the body; its Proof Pack names the frames. Until it is run, the tree stays and its merge waits.

## What the conductor already ran (not yours)

PD-5's runs 2 and 3 (the pin on the wire, `strictMcpConfig` verified), the dry runs, every headless oracle in every Proof Pack, the whole-suite recounts at every join (App 952 / Core 2632), the census (177 pairings, 0 below floor), the Release builds.
