---
id: "note-20260830-sub-scope-incrementality"
title: "Sub-scope incrementality: a call to make before any code, with the measurement that motivates it"
type: decision-note
status: draft
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
  Re-indexing a changed C# scope re-walks every type in it, which is measured at 590ms of an 809ms
  walk on a real repository. Making that incremental below the scope conflicts with the append-only
  per-scope snapshot model, so this note states the options and their costs and stops short of
  choosing — the decision changes the store's contract and is not a tidy-up.
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

## Recommendation, offered rather than taken

**D, and not yet.** It preserves the property the store was designed around, and it targets the
measured cost directly. But it should not be built until there is a second measurement: how often a
real edit-to-graph cycle actually happens, and what the user-visible latency is today. Optimising a
1.2s cold path that runs on demand is a different value proposition from optimising one that runs on
every keystroke, and **nobody has measured which this is** — instrumenting that is cheaper than any
of A–D and should come first.

## What would change this recommendation

- Edit-to-graph latency measured above ~2s on a repository a user actually keeps open.
- A scope appearing that is materially larger than TheTerrace's 465 files.
- A decision to re-index on save rather than on demand, which turns a bounded cost into a per-edit one.
