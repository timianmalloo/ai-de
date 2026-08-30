---
id: "note-20260830-sub-scope-incrementality"
title: "Sub-scope incrementality: a call to make before any code, with the measurement that motivates it"
type: decision-note
status: resolved
owner: "@timianmalloo"
phase: "phase-3"
tags: [decision-note, extraction, performance, store, incremental, measurement]
links:
  - { to: adr-0002-workspace-fact-store, rel: relates-to }
  - { to: adr-0001-derived-evidence-views, rel: relates-to }
links-suggested: []
review-by: 2027-02-28
review-suggested: []
summary: >-
  Re-indexing a changed C# scope re-walks every type in it, measured at 590ms of an 809ms walk on a
  real repository. RESOLVED 2026-08-30, and the answer is not to build it: there is no automatic
  re-index — no FileSystemWatcher exists — so the cost is paid deliberately by a user pressing a
  button, and breaking the per-scope snapshot's atomicity to shorten it is a poor trade at that
  trigger. The note stays for the trigger it names: re-index on save.
---

# Sub-scope incrementality

**Status: open. Nothing has been implemented.** This note exists so the choice is made deliberately
rather than arrived at by someone optimising a loop.

## What is already true, measured

Per-**scope** incrementality exists and works. `ScopeFingerprints` hashes each file's name, length
and modification time; an unchanged scope is skipped and counted as **reused**, never as *indexed* —
deliberately, so "28 of 28 indexed" cannot be a true sentence about a run that read nothing.

Parse-level reuse also exists. `SyntaxTreeCache` keys a parsed tree on path, length, timestamp and
parse options, so a scope that must be re-read does not re-parse the files that did not change. On
TheTerrace the second pass reports **2,691 trees reused against 829 parsed**.

The gap is between those two. When *one file* in a scope changes, the scope is re-extracted in full:
every type is re-enumerated and every member's type re-read, even though 464 of 465 files are
untouched and their syntax trees came from cache.

**The cost, measured on TheTerrace rather than reasoned about:**

| stage | cold | warm |
|---|---|---|
| symbol walk, total | 809ms | 406ms |
| ├ gather (`GetMembers` + unwrap) | 590ms | 323ms |
| ├ display strings (7,317 calls) | 42ms | 20ms |
| ├ attributes | 54ms | 24ms |
| └ dedupe (`SymbolEqualityComparer`) | **3ms** | 2ms |

Two hypotheses died getting here. `ToDisplayString` was assumed to dominate and is 3.9%; the
symbol-comparer `Distinct` was assumed to be next and is **0.5%**. The cost is the semantic work
itself — binding every member signature — which cannot be made cheaper, only avoided.

## Why this is not a tidy-up

The store is **append-only, with a generation and a committed snapshot per scope**. A scope's
assertions are written as one generation and become current together. That is what makes a partial
read safe: a reader either sees the whole previous snapshot or the whole new one, never a mix.

Sub-scope incrementality means writing *part* of a scope's assertions and keeping the rest from an
earlier generation. That breaks the property the snapshot model exists to provide, and every option
below is a way of paying for that.

## The options, with what each costs

**A. Do nothing.** Re-extract the whole scope on any change. Correct today, and the cost is bounded
by the largest scope — ~1.2s cold on a 465-file project. *Cost:* an edit-to-graph latency that grows
with project size, which is the wrong direction for the product's core loop.

**B. Per-file assertion attribution.** Tag every assertion with the file that produced it, then
re-extract only changed files and replace their assertions within the current generation. *Cost:* the
generation stops being atomic, and a type whose members reference a changed file may need
re-deriving even though its own file did not change — cross-file symbol dependencies are exactly
what the compiler resolves, so "which assertions does this file own" is not a question the extractor
can answer cheaply or honestly. **This is the option that looks easy and is not.**

**C. A finer scope.** Make the scope smaller than a project — a folder, or a file — so the existing
per-scope machinery applies unchanged. *Cost:* scope count explodes (TheTerrace would go from 28 to
hundreds), cross-scope edges multiply, and every scope pays compilation setup. The module-id
collision closed on 2026-08-30 is a preview of what a scope-granularity change disturbs.

**D. Keep the model, shrink the work.** Leave the snapshot atomic but skip the *walk* for types whose
declaring files are all unchanged, reusing their assertions from the previous generation in memory
while still writing one whole new generation. *Cost:* memory for the previous generation during a
re-index, and a correctness obligation to invalidate a type when anything it references changed —
narrower than B, because the output is still one atomic generation.

## The blocking question, answered 2026-08-30 — by CHECKING, not by measuring

This note said the decision rested on "how often a real edit-to-graph cycle happens", and proposed
instrumenting it. The instrument was built (`RefreshMetrics`: p50/p95/max, first/last, over
`refresh.metrics`). But the question turned out to be answerable without it, by opening the code:

**There is no automatic re-index. `FileSystemWatcher` does not appear anywhere in `src/`.** Indexing
runs only from `Controller.WorkspaceIndex` and `WorkspaceReindexAll` — explicit user commands. So
edit-to-graph is **entirely on demand**: a user presses a button, waits, and gets a graph.

That reframes the whole note. The comparison it was set up to make — "an occasional cost a user asks
for" versus "something they wait on constantly" — has an answer already, and it is the first one. The
percentiles would have measured how long a deliberate action takes, which is useful for a progress
indicator and is *not* the thing that decides whether the store's atomicity should be broken.

**It is worth saying how nearly this went the other way.** Three hypotheses in this area have now been
wrong — `ToDisplayString` (3.9%, not the cost), `SymbolEqualityComparer` (0.5%), and "the index still
walks every scope" (it does not). This would have been the fourth: building an instrument, collecting
a distribution, and reasoning carefully about a number that answered a question nobody was asking.
One grep for `FileSystemWatcher` was the cheaper move and it was available the whole time.

## Recommendation

**None of A–D. Not now.** Sub-scope incrementality optimises a cost the user pays deliberately, once,
with a visible action, on a path where 1.2s is acceptable. Breaking the per-scope snapshot's
atomicity to shorten it is a poor trade at today's trigger.

**D remains the right shape** if the trigger changes, and the note stays for that reason rather than
being deleted: it preserves the atomic generation and targets the measured cost (`GetMembers` binding
at 590ms of an 809ms walk), where B looks easy and is not.

## The trigger to watch for, named so it is not missed

Build sub-scope incrementality when — and only when — one of these becomes true:

- **Re-index on save is introduced.** This is the one that matters: it converts a bounded, deliberate
  cost into a per-edit one, and it is the moment `RefreshMetrics` starts answering a real question.
  Whoever adds the watcher should read this note first.
- Edit-to-graph exceeds ~2s on a repository a user keeps open (`RefreshMetrics` p95 now reports this
  without anyone enabling anything).
- A scope appears materially larger than TheTerrace's 465 files.
