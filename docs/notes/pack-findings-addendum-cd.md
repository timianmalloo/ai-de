---
id: note-pack-findings-addendum-cd
title: "Pack findings from the Addenda C/D programme — what belongs in the AI-Forward Pack, with the defect class each one closes"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "conductor-addendum-c"
tags: [pack, updatepack, coordination, worktree, audit, prompt-log, gates, conductor]
links:
  - { to: coordination-addendum-cd, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: plan-addendum-c-modes, rel: relates-to }
review-by: ""
summary: >-
  The findings the Addenda C/D programme (2026-09-11 → 2026-09-13; 13 joins, 19 nodes, Rulings
  50–89) produced about the pack itself, each with the class it closes and the control it proposes,
  for the next /updatepack. Nothing here is repo-specific; each item is a tool or a rule the pack
  ships.
---

# Pack findings — the Addenda C/D programme

Each row: the finding · the class · what the pack should ship. Measured in this repository; the
scripts named are in `tools/` here and are candidates for `docs/ai-forward-pack/scripts/`.

| # | Finding | Class | Proposed for the pack |
|---|---|---|---|
| 1 | `prompt-log.py add` dies on a non-ASCII prompt under a Windows console (`→`, an em dash); a heredoc's `—` turned an audit summary into mojibake once and a `\|` in a summary was read as a shell pipe once | DC-177's sibling (a tool reading its argv/stdout through the console code page) | `PYTHONIOENCODING=utf-8` set by the script itself; `--file <path>` for any text argument (CV-4/PD-5 used a file-based superseding entry as the workaround) |
| 2 | `audit-log.py append` consumed the `start` marker of a *different* run (`prompt-log.py add` between a skill's start and close) — durations attributed to the wrong entry | AL4a's hole | the marker keyed by `--session` **and** the skill; `prompt-log.py add` never consumes a marker |
| 3 | A node that grounds before `audit-log.py start` reports a duration from the wrong instant (CV-4) | DC-190 | a `SessionStart` hook that calls `audit-log.py start` unconditionally; the brief template's `start` line first |
| 4 | `coord worktree cleanup` labels a tree with 21 unique commits *"merged"* (the frozen F5 tree) | DC-142 (rec. 2) | the label is `git rev-list --count <base>..<branch> == 0` against the repository's default branch, never `--merged` against HEAD; the report prints the count |
| 5 | A brief that says *"claim every file you will touch, `--ttl 3600`"* holds the register and the census test for an hour while editing none of them; two joins queued ~50 min | DC-163 | `coord claim` refuses a `register`-class path (from `.agents/artifacts.yml`) and caps `--ttl` at 900 s unless `--long-edit <reason>`; the brief template says *claim for the minutes of the edit* |
| 6 | Four join lines hid a red gate behind `\| tail`, `for … done`, `;` — one sealed a merge with conflict markers | DC-113 (rec. 2–4), DC-136 | `run-verify-gates.py` (every `verify-*.py`, one exit status, `--self-test`) and `conductor-join.py` (merge → markers → register → recount → audit → regenerate → commit → gates → push → build, each gated by its return code) — both in `tools/` here; the pack's `execute-with-coordination` skill names the script as the only join line |
| 7 | A merge of figure-patched `site/*.html` left `<<<<<<<` and the resolution path did not read it | DC-136 | `verify-no-conflict-markers.py` first in `regenerate-derived.py`'s CHECKS (landed here); the pack's resolver rule: derived files are regenerated, never resolved |
| 8 | Test infra spawned Windows Terminal tabs (`CREATE_NEW_CONSOLE` with WT as the default terminal) and WT's agent attached an MCP pair per launch, kept for WT's lifetime — 257 accumulated over two days | DC-170 | `verify-no-new-console-launches.py` (code lines only, no allowlist, `--self-test`) as a pack gate; the knowledge doc names the mechanism |
| 9 | A ConPTY child of a redirected parent inherits the parent's standard handles (`STARTF_USESTDHANDLES` with null handles is the fix, as Windows Terminal does) | DC-164 | a knowledge row under the terminal-runtime doc; the conformance suite's out-of-process helper is no longer the only way to observe the channel (DC-014 amended) |
| 10 | A helper binary located by *"Release if it exists"* ran stale while the test step built Debug | DC-171 | the pack's test-strategy doc: helpers resolve by the test assembly's own `bin/<configuration>/` segment |
| 11 | A node's mangled scratchpad path wrote a stray file into the primary checkout (`C:UsersmallaAppData…craft-report.md`) | DC-150 (rec.) | `ui-craft-gate.py --report` refuses a path that does not resolve under the repository or a declared scratch root |
| 12 | A mockup was `display:none` from D1 to D3 while every audit it reported still computed (`[data-restore]{display:none}` matched `<body>`) | DC-200 | `verify-mockup-audits.py` asserts the page box is non-zero before reading any number |
| 13 | A plan row restated an ADR's trigger tuple and the node built the row (`ring.py`'s triple vs ADR-0036 gate 3's) | DC-189 | `prepare-for-coordination`: a row that names a control's trigger quotes the ADR line, never paraphrases |
| 14 | A review's oracle string and the design artifact's copy row diverged; the implementer chose | DC-196 | `ui-design` Stage 5: oracle strings are quoted from DESIGN.md's *Copy added by this section* row and checked by `verify-cited-controls.py`'s string half |
| 15 | An oracle written stricter than the spec failed a green run on residue the harness's own prep had recorded; a negatives-only oracle passed a run that never ended | DC-178, DC-185 | the Spike Protocol: an assertion over a recorded run is run against the harness's own recorded dry run before it ships; every oracle over "what did not happen" first asserts the run ended |
| 16 | Node briefs carried `--main-budget <calls>/6000` as a declaration the node cannot count | IO — an Inferred number in a Verified slot | the harness's tool-use count (the task notification carries it) is the measurement; the pack's audit entry takes `--tool-uses` from the conductor at the join, not from the node |
| 17 | The Copilot Atlas fleet created 42 worktrees under the same `.agents/` coordination layer; `session list` shows them beside ours | — (a finding, not a defect) | `coord session list --mine`; the liveness file's *To peers* section as the protocol's place for cross-fleet notes |

**Landed here already, for the pack to lift:** `tools/run-verify-gates.py`, `tools/conductor-join.py`,
`tools/verify-no-conflict-markers.py` (first in CHECKS), `tools/verify-no-new-console-launches.py`,
the join order in `docs/coordination/addendum-cd.md` §Order of operations row 6, the brief clauses
(claim per edit; placeholders never the register; `start` first; `using var` shells; the terminal
ledger read at the join).

**Lifted (pack revision 70, 2026-09-14).** The four scripts ship generic under
`docs/ai-forward-pack/scripts/`; this repo keeps one definition (DM7): `tools/conductor-join.py`,
`tools/verify-no-conflict-markers.py` and `tools/verify-no-new-console-launches.py` are removed —
the join reads `docs/coordination/join.json` (checks, the recount and its outcome check, regenerate,
gates, the Release build, the trailer file), CI and `regenerate-derived.py` call the pack gates —
and `tools/run-verify-gates.py` remains only as the repo's line over the pack runner, carrying the
per-gate arguments this repository needs. The register entries that cite the `tools/` paths
(DC-113, DC-136, DC-170) predate the lift.

**After revision 70 (2026-09-14, the F5 attended reading).** One more site of the class revision 70
closed for `audit-log.py` / `prompt-log.py` ("UTF-8 set by the scripts"): `docs/ai-forward-pack/scripts/ui-craft-gate.py`
starts a child with `subprocess.run(..., text=True)` and no `encoding=` — the locale decides, cp1252
on Windows. This repository's own scripts were swept (45 statements, 25 files) and are now gated by
`tools/verify-subprocess-utf8.py` (DC-211: a byte-identity oracle that decoded `git show` with the
locale read its own committed bytes as changed, and could not be repaired in place because its
ordering clause forbids a post-run edit). The pack's copy is the pack's to fix (DM7); the gate here
scans `tools/` and `spikes/` only and names the pack path as the finding it does not own.

**Two more from the 2026-09-14 joins.** (1) `conductor-join.py` step 1 reports *"the merge has
conflicts"* with an empty file list when `git merge` refuses for a different reason — local
uncommitted changes to a file the merge touches (`docs/audit/audit-log.jsonl` written by the primary's
prompt-log after the last join). The remedy is different (commit the append-only lines in that tree,
as `verify-stranded-audit` says), so the message should read git's own reason back
(`Please commit your changes or stash them before you merge` / `would be overwritten`) and say so.
(2) DC-216: parallel lanes under one conductor overwrote each other's `audit-summary.txt` — a scratch
path keyed by the harness session rather than the lane's `AGENT_SESSION`; every lane sets
`AGENT_SESSION` first, so it is the right key.

**The join and the coordination ledgers (2026-09-14, the accounts and engines joins).** Three joins
in a row stopped at step 1 for the same reason and the script named none of them: the primary's
`.agents/*.jsonl` ledgers (append-only, written by the pre-commit boundary on every commit — so the
conductor's own decisions file is dirty the instant after it is committed) collide with a lane
branch that carries older copies of the same files, and `git merge` refuses with *"Your local
changes … would be overwritten"* or conflicts on them. The resolution is always the same: the
primary's copy wins (it is the live append-only record). Two pack changes: (1) `conductor-join.py`
step 1 reads git's own reason back and, for `.agents/` paths, commits the primary's copies first and
resolves a ledger conflict to `--ours` automatically — a derived-or-ledger file is never resolved
by hand; (2) the lane brief's "commit with `git add <paths>`, never `-A`" is not enough: a lane's
*merge* of main stages every ledger the primary has since written, so the join contract should
list `.agents/` as ledger-class paths the join owns, and `coord worktree new` should exclude them
from the lane's index the way derived views are excluded.
