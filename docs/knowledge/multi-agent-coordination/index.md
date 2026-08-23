---
id: kb-multi-agent-coordination
title: "Multi-Agent Coordination — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [multi-agent, coordination, event-sourcing, leases, git-worktree, mast]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-agent-coordination-spec, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for the agent coordination layer built in a separate worktree: what the
  published multi-agent systems measure, the fencing-token hazard in lease-based claims, why
  per-session JSONL merges cleanly in git, and the strongest published case against the design.
---

# Multi-Agent Coordination — domain knowledge

**Domain & problem:** A companion project — developed in a separate worktree and surfaced here because its
decisions constrain AI-DE's — lets multiple coding agents work the **same repository** in parallel. Its
thesis is *"claims, not commits, are the unit of coordination"*: every agent appends intent to an
append-only JSONL log (one file per agent session, git-tracked) before touching the working tree; all shared
state — leases with TTL and heartbeat, backlog status, a graph of Goals → WorkItems → Artifacts → Decisions —
is a **fold** over that log into a git-ignored SQLite read model. Each session gets its own `git worktree`.
Enforcement is a Claude Code `PreToolUse` hook plus a pre-commit hook. Verbs: `announce`, `claim`,
`heartbeat`, `release`, `decide`, `block`, `done`.

**Canonical framing:** The field frames agent parallelism as **orchestrator-worker with isolated contexts**
(Anthropic), or as **sandbox-per-task isolation** (Codex, Copilot), or it argues against parallelism
altogether (Cognition). The design here is framed instead as **event sourcing applied to coordination**,
which the research did not find in any production multi-agent coding system — the log-as-truth model is
novel in this space. The distributed-systems half of it (leases, TTL, heartbeat, fencing) is very old and
very well understood, and that is where the evidence bites hardest.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"Measured numbers and constants" — this domain's
constants are measurements from published studies, and they belong beside their citation.)*

## Headline findings

1. **The 15× token multiplier is a first-party Anthropic measurement, not marketing.** Their research system
   uses ~4× chat tokens for a single agent and **~15× for multi-agent**, and token usage alone explains
   **80% of performance variance** on BrowseComp. Multi-agent beat single-agent Opus 4 by **+90.2%** on their
   internal research eval. Parallelism is bought, not free. — *(Verified, [S1])*
2. **Anthropic's own caution is the most authoritative disconfirming data point available**, and it names
   our exact use case: *"most coding tasks involve fewer truly parallelizable tasks than research, and LLM
   agents are not yet great at coordinating and delegating to other agents in real time."* — *(Verified, quoted, [S1])*
3. **MAST finds the dominant multi-agent failures are specification and design, not capability.** Across
   1,600+ traces (Cohen's κ = 0.88) the frequent modes are step repetition **15.7%**, action-reasoning
   mismatch **13.2%**, not recognising termination **12.4%**, disobeying task spec **11.8%**, incorrect
   verification **9.1%** — organised into three categories: system design, inter-agent misalignment, task
   verification. Interventions on ChatDev: **+9.4%** from a better role spec, **+15.6%** from adding a
   verification step. Coordination protocol is where the wins are. — *(Verified, [S5])*
4. **Kleppmann's fencing-token critique is the sharpest attack on the lease design, and it is unrefuted.** A
   process can hold an *expired* lease and still be executing — a GC pause, a page fault or a network delay
   is enough. The only safe fix requires **the resource being written** to reject any write carrying a
   fencing token lower than the highest it has seen. TTL + heartbeat alone gives *efficiency*, never
   *correctness*. — *(Verified, [S6]; corroborated by Chubby [S7])*
5. **One JSONL file per agent session is structurally the right choice for git**, and it is right for a
   precise reason: git's built-in `union` merge driver preserves all appended lines from both sides but
   **does not deduplicate**. Per-session files mean no two writers ever touch the same file, so the
   duplication hazard never arises. — *(Verified, [S13][S19])*
6. **Git worktrees share `.git/hooks/` across every worktree**, and Claude Code's `${CLAUDE_PROJECT_DIR}`
   stays pinned at the **project root**, not the worktree root — the worktree path arrives via `cwd` in the
   hook's input JSON. A hook enforcing claim-before-edit must therefore be path-aware, and the Claude Code
   docs warn about this explicitly. — *(Verified, [S12][S13])*
7. **METR's RCT is the most reliable productivity measurement in the field and it is unflattering.** 16
   experienced open-source developers, 246 real GitHub issues: **19% slower** with AI tools, while believing
   they were **20% faster** — a 39-point perception gap, against a pre-study expert prediction of 24–39%
   *faster*. Any throughput claim for parallel agents must be measured, because the felt experience of speed
   is demonstrably unreliable. — *(Verified, [S11])*
8. **UUIDv7 is the standards-track choice; ULID is the compact one.** RFC 9562 (May 2024) makes UUIDv7 an
   IETF Proposed Standard: 48-bit millisecond timestamp plus randomness, with an optional sub-millisecond
   counter for strict monotonicity, 36 characters. ULID is 48-bit ms + 80 random bits, 26 characters in
   Crockford Base32, monotonic within a millisecond by incrementing the random component — but it is a
   community spec with no RFC. — *(Verified, [S9][S10])*
9. **Nobody else does this.** No production multi-agent coding system uses a per-session append-only log as
   its coordination primitive: Claude Code isolates by worktree, Codex and Copilot by sandbox, and
   AutoGen/CrewAI/LangGraph pass messages or share state. The design is genuinely novel — which means there
   is no prior art to learn from, and no published failure modes to avoid. — *(Inferred from the survey, [S1]–[S18])*
10. **The strongest counter-argument is Cognition's, and it is not fatal — but it relocates the success
    criterion.** Their case is that parallel agents produce *locally correct, globally incoherent* output
    because each action embeds implicit decisions others cannot see. Announcing intent partially answers
    their Principle 1 (share context) and does **not** answer Principle 2: "I will implement OAuth in the
    auth module" does not communicate the forty micro-decisions that follow. — *(Verified, [S4])*

## Confidence summary

Verified: every measured number (token multipliers, MAST frequencies and interventions, METR results,
SWE-bench scores), the fencing-token argument, git worktree and union-merge mechanics, the ULID/UUIDv7
specifications, and the direct quotations from Anthropic and Cognition. Inferred: OpenAI Codex's internal
architecture (no primary engineering source); AutoGen/CrewAI/LangGraph coordination details (secondary
sources); the novelty claim (an argument from an exhaustive survey). Flagged: whether any multi-agent-specific
productivity RCT exists — none was found; the compound effect of coordination overhead on METR's result is
unmeasured.

**Load-bearing Flagged claim:** there is **no published measurement of file-level merge-conflict frequency
when multiple AI agents work one repository**. The metric the coordination spec proposes to optimise has no
published baseline, which makes measuring it a contribution rather than a checkbox.

## Design implications

*(For the coordination project, surfaced here because AI-DE's daemon and MCP layer sit alongside it.)*

- **Add a fencing token, or state explicitly that leases are advisory-for-efficiency only.** This is the one
  finding that touches correctness rather than performance. A monotonically increasing token issued per
  claim, checked by whatever validates the write, converts "probably no overlap" into "cannot overlap".
  Without it, two agents can both believe they hold a claim after a pause, and the log will record both.
- **Close the TOCTOU gap between reading the fold and appending a claim.** The read model can be stale
  between the check and the append. Optimistic concurrency — a version check at append time — is the
  standard answer and is cheaper than it sounds.
- **Version the event schema from the first commit.** The log is the system of record; a `claim` that gains
  a required field must still replay. This is the canonical operational pain of event sourcing and it is
  much cheaper to design in than to retrofit.
- **Plan log compaction before it is needed.** Cold-start replay cost grows with the repository's whole
  history. Snapshot-plus-delta is the standard mechanism; its hazard is snapshot consistency.
- **Make the pre-commit hook a floor, not the enforcement.** `git commit --no-verify` bypasses it. Treat it
  as the universal *last* line, with the Claude Code `PreToolUse` hook as the real edit-time guard where it
  exists — and note that Copilot's cloud agent has no equivalent at all (see the MCP knowledge base).
- **Make the hook path-aware.** Shared `.git/hooks/` plus a project-root `${CLAUDE_PROJECT_DIR}` means a
  naively written hook checks the wrong tree. Use `cwd` from the hook input.
- **Prefer UUIDv7 unless compactness matters more than standardisation.** Both sort by time; only one has an
  RFC.
- **Measure coherence, not just conflicts.** Cognition's Principle 2 means the absence of merge conflicts is
  not evidence of success. The proposed metrics (conflicts per PR, % edits on claimed paths, lease wait
  time, decisions re-litigated) are all *structural*; at least one **semantic** measure is needed — does the
  combined output of N agents actually work.
- **Expect modest, task-dependent gains, and instrument accordingly.** Anthropic says coding parallelises
  poorly; METR says perceived speed is unreliable. Both argue for measurement rather than for abandonment,
  and both argue against claiming a benefit that has not been observed.

## How to use this base

This topic is the **surfaced** view of work happening in another worktree — it records the evidence, not the
implementation. Personas and the design skills cite these files (BoK §III.1); the coordination project owns
the decisions. The measured numbers in `references.md` are the ones to quote rather than recall.
