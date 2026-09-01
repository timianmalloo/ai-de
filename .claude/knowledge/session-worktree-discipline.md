---
applyTo: "**"
---
# Session Worktree Discipline

*Normative guidance for **where a session does its work**. Every new session starts in its own
git worktree; a session may create more if the work needs them; and no worktree is ever left
behind. `coord-core.py` already keys coordination by worktree and enforces one session per tree —
this document makes the worktree **the default unit of session isolation** rather than something
you reach for after a collision, and it closes the half nobody owns: cleanup.*

Normative keywords (**MUST**, **SHOULD**, **MAY**, **MUST NOT**) follow RFC 2119.

The governing idea: **two agents sharing one checkout is not a merge problem, it is a
correctness problem, and it is invisible while it is happening.** A shared working tree has one
index, one HEAD, one set of generated artifacts and one set of build outputs. When two sessions
touch it, `git stash` in one silently reaches into the other's uncommitted work, a regenerated
surface is attributed to whichever session ran `sync` last, and a test run measures a tree that
nobody authored. Nothing fails loudly; the evidence simply stops meaning what it says. The
countermeasure is not care — care does not scale across concurrent sessions and does not survive
a session that crashes mid-edit. The countermeasure is **isolation by default**.

The second half is the half that actually rots: an isolation mechanism people adopt and never
clean up becomes a disk full of half-finished trees, stale git metadata, and — worst — a tree
holding the only copy of some real work that everyone has forgotten. So cleanup is specified
here with the same force as creation, and it is **fail-safe**: it refuses to delete anything that
could still be carrying work.

---

## 0. When this applies

Any session that will **write** to a repository — a skill run, an interactive change, a
long-running agent task. It does not apply to a read-only question, a `--check` run, or a lookup;
creating a worktree to answer "what does this file say?" is the ceremony this pack exists to
avoid. Owner: the **Release / Deployment Engineer** (the path to production and back) with the
**SRE** (resource bounds and orphan accumulation) and **Security & Identity** (an irreversible
delete is a gated action).

---

## 1. The directive

**WT1 — A new session starts in a new worktree.** A session that will write to the repository
**MUST** begin by creating and entering its own git worktree, on its own branch, rather than
working in the primary checkout. This is the default and it does not require a reason; working in
the primary checkout is what requires one (WT4). The primary checkout stays clean, reviewable and
always-buildable — which is what makes it a useful reference while other work is in flight.

**WT2 — A session MAY create further worktrees when the work genuinely needs them.** Comparing
two revisions side by side, running a long test suite against a baseline while editing, bisecting,
or holding a release branch open are all legitimate. Every additional tree follows the same rules
as the first: its own branch, registered, and cleaned up. Isolation is cheap; *forgotten* isolation
is not.

**WT3 — One session per worktree, one worktree per session at a time.** `coord-core.py` already
enforces the first half (`COORD-WORKTREE-OCCUPIED`: *"two sessions in one tree is how work gets
lost"*). The second half is the discipline this document adds: a session does its writing in one
tree at a time so that "what did this session change?" has a single, answerable location.

**WT4 — Working in the primary checkout is a recorded exception, not a default.** There are real
cases: a one-line fix on a repo with no concurrency, a repository whose tooling cannot function
outside the primary tree, an environment where worktrees are unavailable. Say which, in the
session's own words, and proceed. An unstated exception is indistinguishable from having forgotten
the rule — and it is the shape that erodes the discipline within a week.

**WT5 — Name the branch and the tree for the work, not for the session.** `feature/audit-duration`
tells the next person what is in there; `session-2026-08-22-a` tells them nothing and guarantees
the tree is never voluntarily cleaned up, because nobody can tell whether it matters. The tool
derives the tree's directory name from the branch for exactly this reason.

---

## 2. Cleanup — the half that rots

**WT6 — A session ends by releasing its worktree.** When the work is committed and pushed, the
session **MUST** either remove its worktree or state plainly that it is being kept and why. "I am
done" without either is how orphans are created, and the cost is paid by someone else, later,
with less context.

**WT7 — Cleanup is fail-safe: never delete anything that might still hold work.** A worktree is
**safe to remove** only when *all* of the following hold, checked in this order and reported
individually:

| Condition | Why it is a hard stop |
|---|---|
| It is **not** the primary checkout | The primary is the reference; removing it is never cleanup |
| It is **not** the current working directory | Deleting the floor you are standing on |
| The tree is **clean** — no modified, staged or untracked files | Untracked is the dangerous one: a new file nobody has committed exists nowhere else |
| It has **no commits absent from the integration branch** | Unmerged work is the only copy |
| Its branch is **not checked out anywhere else** | Someone else is using it |
| Its session is **ended or stale** | A live session's tree is in use |

Anything failing a condition is **reported, never removed** — with the specific reason, so the
human can act. A cleanup tool that deletes on a heuristic will eventually delete the one tree
that mattered, and that single event ends the adoption of the whole practice.

**WT8 — Removal is opt-in, not the default verb.** `cleanup` **MUST** default to reporting a plan
and require an explicit flag to delete. Deleting a directory is irreversible and un-undoable by
git; the pack's standing rule for irreversible actions applies (Rules of the Road §2 — a gate
before, not an apology after).

**WT9 — Prune the metadata as well as the directory.** Deleting a worktree directory by hand
leaves `.git/worktrees/<name>` behind, and git keeps treating that name as taken — so the next
`git worktree add` for the same name fails with a message that describes a state the filesystem
does not show. Cleanup **MUST** run `git worktree prune` so the administrative record matches
reality (E14: read the state back — an exit code is not a result).

**WT10 — Orphans are surfaced routinely, not discovered.** A count of worktrees, how long each has
been idle, and which are safe to remove **SHOULD** be reported at session start, when the count
exceeds a small threshold, or on demand. An orphan discovered by running out of disk is an orphan
that has already cost more than the check would have.

**WT11 — Never remove a worktree to resolve a conflict.** If two sessions have collided, the
remedy is to finish or land one of them, not to delete a tree — deletion at that moment is
maximally likely to destroy the only copy of the work that caused the collision.

---

## 3. The mechanics

The commands live in `coord-core.py`, which already resolves the primary checkout from any
worktree and keys its shared event log by worktree — so worktree lifecycle belongs there rather
than in a parallel tool (one owner per concern):

```bash
# start a session in its own tree (creates the branch, registers the session, prints the cd)
python3 docs/ai-forward-pack/scripts/coord-core.py worktree new \
    --branch feature/audit-duration --session "<session-id>"

# what exists, who holds it, how long idle, what is safe to remove
python3 docs/ai-forward-pack/scripts/coord-core.py worktree list

# the plan (default: reports, deletes nothing)
python3 docs/ai-forward-pack/scripts/coord-core.py worktree cleanup

# act on the plan — only the trees the plan marked safe
python3 docs/ai-forward-pack/scripts/coord-core.py worktree cleanup --remove
```

**WT7a — Liveness is read from the filesystem, not only from the registry.** A tree whose files
were modified in the last hour is **held**, whatever the session ledger says. Measured: a tree
reported *"clean, merged, unheld"* was removed, and a live session recreated it within the minute
and wrote a marker saying it was in use — it had simply never run `coord session start`. Every
cleanliness check had been correct; the liveness check had answered from a record rather than from
the world (**DC-024**). The scan skips build output, is capped, and treats hitting the cap as *in
use*, because a partial scan cannot prove absence. The reason string carries the age, so "idle" is a
measurement a human can disagree with.

**WT12 — The tool reports its refusals, not just its actions.** `cleanup` prints every tree it
declined to remove and the condition that stopped it. A cleanup that silently skips is
indistinguishable from a cleanup that found nothing, and the difference is exactly the
information the human needs.

**WT13 — A worktree isolates the working tree and the index. It does not isolate the repository.**
Some git state lives in the shared `.git` and is therefore common to every worktree — most sharply
**`refs/stash`**, which is one stack for the whole repository. `git stash` is consequently **not a
worktree-local tool**: two sessions stashing at the same moment will pop each other's work, with no
error and no warning, because each command did exactly what it is documented to do.

MEASURED, 2026-08-31: two agents working in `ai-de-knowledge-dedup` and `ai-de-csharp-calls`, both
branched from one commit and both told that worktree isolation was why they could run in parallel,
collided on the stash stack. One `stash pop` restored the other session's 387-line extractor change
into the wrong tree; the reciprocal pop took the first session's work. Nothing was lost only because
both sessions **preserved what they did not recognise** — one recovered from a file copy taken
beforehand and re-verified every measured number byte-identically, the other pushed the foreign
change back onto the stack with an explanatory message rather than discarding it.

**For a temporary revert while a sibling session may be live, copy the file** — `git diff > x.patch`
plus `git checkout --`, or a plain filesystem copy. Reserve `git stash` for a repository you are
certain you are alone in.

The same reasoning covers the rest of the shared directory: `refs/bisect`, notes, config, hooks and
the object store are all common. The rule that generalises past this list: **when isolation is the
reason two things may run at once, enumerate what the isolation does not cover before relying on
it.** "Separate working directories" is a statement about files, not about every piece of state a
tool keeps.

---

## 4. Self-verification checklist

- [ ] The session created and entered **its own worktree** on its own branch before writing
      (WT1), or recorded why it is working in the primary checkout (WT4).
- [ ] Additional worktrees, if any, follow the same lifecycle (WT2).
- [ ] No `git stash` was used for a temporary revert while another session could be live — a file
      copy was used instead, because the stash stack is shared by the whole repository (WT13).
- [ ] The branch and directory are named for **the work** (WT5).
- [ ] At close, the worktree was **removed or explicitly kept with a reason** (WT6).
- [ ] Nothing was removed that was dirty, unmerged, current, primary, or held by a live
      session — and every refusal was **named** (WT7, WT12).
- [ ] Removal required an explicit flag; the default run only reported (WT8).
- [ ] `git worktree prune` ran so metadata matches the filesystem (WT9).
- [ ] Orphan state was surfaced rather than discovered (WT10).
- [ ] No worktree was deleted to settle a collision (WT11).

---

## 5. References

- **`coord-core.py`** — the existing owner of worktree identity (`_worktree_key`), one-session-
  per-tree occupancy (`COORD-WORKTREE-OCCUPIED`), and the primary-checkout resolution that makes
  one shared event log visible from every tree. WT1–WT12 extend it; they do not duplicate it.
- **`end-to-end-integrity.md`** — **E14** (read the state back) is why WT9 prunes rather than
  trusting a delete; **E13** is why WT12 reports refusals.
- **`agent-body-of-knowledge.md`** Part V.3 — code rolls back, state does not. An uncommitted file
  in a deleted worktree is the smallest possible instance of that rule.
- **Rules of the Road** §2 — an irreversible action is gated (WT8).
- **`continuous-improvement.md`** — the orphan-accumulation shape is a resource-leak sibling of
  class **RES-LEAK-TEST**: a durable resource created per unit of work with nothing destroying it,
  invisible on ephemeral CI runners and visible only on a long-lived developer machine.
