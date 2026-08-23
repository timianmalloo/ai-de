---
id: kb-coord-glossary
title: "Multi-Agent Coordination — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, event-sourcing, leases, ubiquitous-language]
links:
  - { to: kb-multi-agent-coordination, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for coordination vocabulary — lease, fencing token, fold, projection,
  union merge driver, ULID — so the protocol, the read model and the docs use one word per
  concept.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **ACI** | Agent-Computer Interface — SWE-agent's term for the interface between an LLM agent and its environment (editor, terminal). The agent-facing analogue of a human-computer interface. *(Verified, [S14])* |
| **Advisory lock** | A lock processes voluntarily acquire and respect, which the system does not enforce. **Every agent coordination scheme in this space is advisory**, including claim-before-edit. Naming it prevents mistaking a convention for a guarantee. |
| **Append-only log** | An immutable, ever-growing sequence of records; state is derived by folding it. Nothing is modified or deleted in place. |
| **Claim** | A lease over a path, glob or symbol, asserted **before** the first edit. The unit of coordination in this design — "claims, not commits". |
| **CRDT** | Conflict-free Replicated Data Type — a structure whose merge converges to the same state regardless of update order. Complementary to a log: a CRDT gives you *the state*, a log gives you *the history*. |
| **Event sourcing** | Storing every state change as an immutable event and deriving current state by replay. The fold is the reduction over events. *(Verified, [S8])* |
| **Fencing token** | A monotonically increasing number issued on each lock acquisition, which **the protected resource must itself reject if it is lower than the highest seen**. The only mechanism that makes lease-based exclusion *correct* rather than merely efficient. *(Verified, [S6])* |
| **Fold** | The reduction over the event log that produces the read model. Must be deterministic and idempotent — replaying the log twice yields identical state. |
| **Heartbeat** | A periodic signal from a lease holder confirming liveness and extending the lease. Says nothing about the interval between the last heartbeat and the next write. |
| **Hotspot** | A path whose edits are inherently shared (`Program.cs`, DI registration, `*.csproj`, EF migrations, `Directory.Packages.props`) and therefore owned by a single integrator agent rather than claimed. |
| **Lease** | A time-bounded lock that releases itself on expiry, so the holder need not be alive to release it. *(Reference: Gray & Cheriton 1989; [S7])* |
| **MAST** | The Multi-Agent System Failure Taxonomy — 3 categories, 14 failure modes, over 1,600 traces. The empirical account of *how* these systems fail. *(Verified, [S5])* |
| **Merge queue** | A mechanism that serialises or batches PRs before merge, running CI in the combined state. Answers "is main still green", not "did two agents edit the same function". *(Verified, [S17])* |
| **OCC** | Optimistic concurrency control — proceed without a lock, detect conflict at commit, retry or abort. The standard answer to the read-then-append TOCTOU gap. |
| **Projection / read model** | The derived view produced by folding the log. Here, the git-ignored SQLite database. **Eventually consistent with the log by construction.** |
| **Session** | One agent's run, owning exactly one JSONL log file — which is what makes per-file append merge cleanly. |
| **TOCTOU** | Time-of-check-to-time-of-use — the window between reading the read model and appending a claim, during which another agent may have claimed the same path. |
| **ULID** | Universally Unique Lexicographically Sortable Identifier: 48-bit ms timestamp + 80 random bits, 26 characters in Crockford Base32. Community spec, no RFC. *(Verified, [S9])* |
| **Union merge driver** | Git's built-in driver (`merge=union`) that keeps all changed lines from both sides. Correct for non-overlapping appends; **duplicates overlapping ones**. *(Verified, [S19])* |
| **UUIDv7** | RFC 9562 time-sortable UUID: 48-bit ms timestamp plus randomness, optional sub-ms counter for strict monotonicity, 36 characters. **The standards-track option.** *(Verified, [S10])* |
| **Worktree** | A linked working directory sharing the object store and remote refs but with its own HEAD, index and files. **Hooks are shared, not per-worktree.** *(Verified, [S13])* |
