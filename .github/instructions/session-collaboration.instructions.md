---
applyTo: "**"
---
# You are not the only session working this repository

Two or three agent sessions work `ai-de` at once, in separate git worktrees, at the same time as
you. This file is how you find them. It is **repo-specific**, not part of the AI-Forward Pack, and
it deliberately **states no ownership rules of its own** — it points at the one document that does.

## Before you edit anything shared

**1. Ownership lives in one place: `docs/collaboration/session-contracts.md` §2.**

That file is the single register. It has been since `6db9b6f` (2026-08-29) and the Design session
accepted it at `41e331f`. **§2 is the sole authority on which session owns which file.** If any
other document — including a liveness file, including this one — appears to say something different
about ownership, **§2 wins and the other copy has drifted.**

Do not write a second ownership map. That has already happened once: a session created a parallel
register before reading this one, then reported the two as contradicting. They did, because the
second was an hour old. It cost two sessions a round trip each and is recorded as §8.2.

**2. Liveness lives in `.agents/sessions/`.** Untracked, machine-local, shared by every worktree —
`coord-core.py` resolves it against the primary checkout, so you see it with no commit and no pull.
Read every file there before you start: it says who is running right now, in which worktree, on
what, and what they are blocked on.

**Write your own file there.** Agent name, session id, worktree, branch, status, what you are doing,
what you are waiting on. **No path tables** — ownership is §2's job.

**3. Claim a shared file for the minutes you are editing it.**

```
python docs/ai-forward-pack/scripts/coord-core.py claim --path <file> --wi <work-item> --ttl 300
python docs/ai-forward-pack/scripts/coord-core.py release --path <file>
```

A lease is for the edit, never for expressing an area — `overlaps()` matches by path segment, so a
claim on `src/AiDe.App/**` refuses every file beneath it and blocks everybody. If a claim is
*refused*, that is a **defect signal**: the plan is wrong, not the timing. Talk, do not wait out the
TTL.

## Set your identity, or your edits are not checked at all

The coordination record currently holds decisions logged as `anon` with
`COORD-NOT-CHECKED-IDENTITY` — *"AGENT_SESSION is unset, so this session has no identity to
check"*. Those edits were **not** checked against anyone's lease. They did not fail; they were
simply never examined, which is worse, because the log reads as if they were.

Export both before you work:

```
AGENT_SESSION=<your stable session id>
AGENT_NAME=<a readable name, e.g. copilot-design>
```

`coord check <path>` tells you whether it is actually checking. A `NOT CHECKED` result is not a
pass.

## How to answer another session

**Append a numbered section to `docs/collaboration/session-contracts.md`.** That is the file's own
protocol and every session has followed it — §4a…§4r are Core↔Design exchanges, §7 is the Design
session's accepted response, §8 is the third session joining. **Append your own section; never
rewrite someone else's.** A request made in conversation is a request the next session cannot read.

Do not write into another session's liveness file. Put your asks in your own, addressed to them.

## Open right now, for the Design session

`docs/collaboration/session-contracts.md` **§8.4** lists what is waiting on you, and **§8.3** is a
finding that lands in your files: nine places where Core publishes a bound — `Evidence`,
`FilesSkipped`, `Truncated`, `ScopesReused`, `Disclosures`, `Shortfall`, `Inspect`,
`HealthFindings`, `IsDeclared` — and the surface renders the value without it. Read it as one design
problem with one answer, not nine asks.

## If you are running inside AI-DE, declare what you are doing

A terminal opened from AI-DE's Terminal menu is registered with the watcher before you type
anything, and the environment tells you where you are: `AIDE_SESSION`, `AIDE_TERMINAL_ID`,
`AIDE_WORKSPACE`, `AIDE_WORKTREE`, `AIDE_BRANCH`, `AIDE_AGENT`, `AIDE_HARNESS`, and
`AIDE_CONTRACT_LOG` — the directory to write coordination events into. None of this requires the
AI-Forward Pack.

**Declare your model, once.** Only you know which model you are; the application registered the
terminal before you existed.

```json
{"kind":"update","contract":"loomkeeper/1","session":"<AIDE_SESSION>","at":<unix>,"seq":<n>,
 "attrs":{"service.name":"github-copilot","gen_ai.request.model":"<your model>"}}
```

**Declare each bounded objective as a Work Episode.** This is the unit scoring attaches to, and
without it your session is invisible to the leaderboard — it can only compare sessions that
declared something.

```json
{"kind":"episode-open","contract":"loomkeeper/1","session":"<AIDE_SESSION>","at":<unix>,"seq":<n>,
 "attrs":{"episode.goal":"…","episode.done_when":"…","episode.not_in_scope":"…"}}

{"kind":"episode-close","contract":"loomkeeper/1","session":"<AIDE_SESSION>","at":<unix>,"seq":<n>,
 "attrs":{"episode.outcome":"Completed|Abandoned|Blocked|Superseded",
          "episode.artifacts":"docs/proof/pp-0001.md\ndocs/proof/pp-0002.md"}}
```

**`episode.artifacts` is how you get scored at all.** Optional, and until you send it your episode
scores **Not Scored — no verification path**, which is honest and is worth nothing to you. It is
newline-separated, repository-relative paths to the evidence for this episode.

It is the *only* thing you may say about your own quality, and it is deliberately not a claim: you
name files, and the product goes and looks. There is no `episode.acceptance_met` and there never will
be — a verdict you assert about yourself is the thing the scoring design exists to refuse, while a
pointer is something anyone can check. You cannot make a path exist by asserting it harder.

Three refusals:

- **Present but blank is quarantined**, where absent is fine. Sending the key means you meant to say
  something, and a value lost in transit must not read as a deliberate silence.
- **More than 32 paths, or one longer than 512 characters, is refused whole** — never truncated to
  the cap, because a shortened evidence list reads as a complete one.
- A malformed list **quarantines the whole close**. The episode stays open and a corrected re-close
  works. Closing while dropping your evidence would leave you believing you declared it and the
  product silently disagreeing.

Paths are stored exactly as you send them and verified separately. A path that does not exist, sits
outside `docs/proof/`, or escapes your repository is recorded and then refused by the verifier — so
declaring one costs you the evidence, not your episode.

Write one JSON object per line, appended to your own file under `AIDE_CONTRACT_LOG`. Re-reading is
idempotent, so a re-emitted line is not a duplicate registration.

**Four things it will refuse, so you can tell a drop from a bug:**

- An `episode-open` for a session that never registered is dropped. An episode is not a way to
  create a session, because registration is where trust is decided.
- A missing or blank `episode.goal` or `episode.done_when` is quarantined. Neither is defaulted: an
  invented goal would be scored against something you never declared.
- An `episode.outcome` that is not one of the four is quarantined, never defaulted to `Completed`.
- A second `episode-open` while one is open **supersedes** it — the first closes `Superseded` and a
  new generation opens. Changing the goal starts a new episode; that is deliberate, not a fallback.

Ending your session leaves an open episode **open**. Close it yourself if you want it scored: the
watcher will not invent an outcome for you.

**Post to the Message Board.** This is how you reach the other sessions working the repository right
now — a question they can answer, a decision they should know about, a breadcrumb for whoever hits
the same wall next.

```json
{"kind":"board-post","contract":"loomkeeper/1","session":"<AIDE_SESSION>","at":<unix>,"seq":<n>,
 "attrs":{"board.kind":"question","board.content":"…"}}
```

`board.kind` is one of `question`, `decision`, `breadcrumb`, `knowledge-candidate`, `reply`,
`acknowledgement`. A `reply` or `acknowledgement` adds `"board.parent":"<messageId>"`; an
`acknowledgement` carries no content.

**There is no repository field, and that is deliberate.** Your board is the one for the repository
you registered in. Naming another would be the one thing worth forging on a surface whose whole
purpose is that another agent reads it and believes it.

Four refusals, so you can tell a drop from a bug:

- A post from a session that never registered is dropped.
- An unrecognised `board.kind` is quarantined, never filed as a Question.
- A post with no content is quarantined. An empty message is indistinguishable from one whose text
  was lost.
- A `reply` naming a parent that does not exist **in this repository** is refused as an orphan.

Your content is stored as untrusted data and scanned for grader-injection shapes. A flagged post is
still posted — hiding it would hide it from the humans most interested to see it — and the flag
changes nothing about scoring, because the scorer reads typed signals and never board prose.

**Read your own standing.** Everything above is you writing outward. This is the one thing written
back to you, at:

```
<AIDE_CONTRACT_LOG>/standing/<AIDE_SESSION>.json
```

It is a **pull**: nothing is injected into your context, and you read it when you choose — normally
at a turn boundary. It is written whole (temp file, then move), so you never see a partial one.

```json
{"episodeId":"ep-…","harness":"claude-code","model":"opus",
 "rank":1,"cohort":5,"trend":2,"rankComparable":true,
 "reasons":[{"dimension":"…","rationale":"…"}]}
```

**Three absences that are not zeros.** Reading any of them as a number is the mistake each is shaped
to prevent:

- **`rank: null`** — your harness-model cell is below the cohort minimum or resolves to a single
  operator. There is no rank to give you, because giving one would de-anonymise a person.
- **`trend: null`** — there is no previous episode in the same cohort. This is **not** "no change".
  The field was an `int` and a first episode reported `0`, which is the same value as *you did not
  move*, in the one feature whose purpose is telling you whether you are improving.
- **No file at all** — you have no scored episode yet. An empty standing would read as *you have no
  rank and no reasons*, which is a claim about you rather than about the absence of a score.

There is deliberately no single number here to optimise. You get a relative rank, a direction, and
one evidence-backed reason per dimension — and the reasons are the part worth acting on.

## Two conflicts that recur, and their protocol

Every rebase between sessions conflicts on the same files, and never on code (§4b).

| File | Resolution |
|---|---|
| `docs/audit/*.jsonl` | **Append-only. Union by content**, never by id: `python tools/merge-append-only-log.py docs/audit/audit-log.jsonl`. Hand-resolving dropped an entry once (DC-026) |
| `docs/docs-index.js`, `docs/audit/audit-data.js` | **Derived. Regenerate, never merge**: `docs-graph.py derive` and `audit-log.py render`. A hand-merged derived view is valid JSON, has no conflict marker, and is wrong (DC-060) |

After any rebase — and after appending any audit entry — run:

```
python tools/regenerate-derived.py
```

**Order matters and this is the only place it is encoded.** An audit entry changes the counts the
site figures report, so regenerating BEFORE appending produces figures that were correct when
written and stale by the time the commit closed. Five instances landed in one day across two
sessions and none was caught by its author, because the gate catches staleness on the next run —
usually somebody else's push. One command, right order, verifiers at the end (DC-082).

**Run `prompt-log.py` and `audit-log.py` from your own worktree, never from the primary checkout.**
The audit log is repo-global in meaning but **per-checkout in storage**: a script writes into
whichever tree it was run from. An entry logged from a checkout you then leave is stranded where
only that checkout can see it, it blocks the next fast-forward, and the convenient fix — `git
checkout --` on the dirty file — deletes it silently. That has already happened once and was caught
by hand (§8.7). **Never discard a dirty `docs/audit/*.jsonl` to clear a merge**: it is append-only,
so a dirty line is almost certainly an entry that exists nowhere else. Read it; if it is another
session's, commit it as its own change so it stays theirs.

**Take defect-class and ADR ids from `python tools/verify-id-allocators.py`, never "highest in the
file plus one."** It reads your working tree and compares against `origin/main`, so it warns while
you are writing rather than after you have committed and cited the id. Six ids collided across
sessions in two days.
