---
id: kb-coord-open-questions
title: "Multi-Agent Coordination — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, fencing, coherence]
links:
  - { to: kb-multi-agent-coordination, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The correctness gaps the literature identifies in a lease-and-log coordination design, and
  the four strongest disconfirming views — Cognition, Anthropic's own caution, METR, and
  Kleppmann — assessed rather than dismissed.
---

# Open questions & domain failure modes

## Unresolved by research — and by the current design

1. **Is there a fencing token on the log writer?** If two processes both believe they hold a session's
   lease and both append, the log is corrupted with interleaved or duplicated entries. Kleppmann's argument
   applies directly: TTL + heartbeat is insufficient without the **write target** enforcing monotonic order.
   *(The hazard is Verified [S6]; whether the design addresses it is not stated in the spec.)*
2. **The read-then-claim TOCTOU race.** An agent reads the SQLite fold, decides a work item is free, and
   appends a `claim`. The fold may have been stale at the moment of reading. Optimistic concurrency with a
   version check at append time is the standard answer; the spec does not address it. *(Open)*
3. **Event schema evolution.** The log is the system of record. When a `claim` gains a required field, every
   older event must still replay. This is the canonical operational pain of event-sourced systems and it is
   not addressed. *(Inferred from the event-sourcing literature, [S8])*
4. **Replay cost at scale.** Cold-start fold time grows with the repository's whole history. Snapshot +
   delta is the standard mitigation, and it introduces its own snapshot-consistency hazard. *(Open)*
5. **Implicit decisions are not in the log — Cognition's Principle 2.** An agent that has already touched a
   file has made architectural decisions the announcement never captured. Two agents can satisfy the
   protocol perfectly and still produce coherent-but-conflicting code. *(Verified as a hazard, [S4])*
6. **Pre-commit hooks are bypassable.** `git commit --no-verify` defeats the universal floor. The design
   relies on it. *(Verified, [S13])*
7. **No published merge-conflict rate exists for concurrent AI agents on one repository.** The metric the
   design proposes to optimise has no baseline anywhere in the literature. Measuring it would be a
   contribution. *(Flagged — genuine empirical gap)*
8. **No multi-agent productivity RCT exists.** METR measured single developers with AI tools. Whether
   coordination overhead compounds or offsets that result is unmeasured. *(Flagged)*

## Known failure modes of this domain

- **Believing a lease is a guarantee.** It is advisory and time-bounded; without fencing it is a strong
  hint. Designing as though it were mutual exclusion is the classic error this literature exists to prevent.
- **Locally correct, globally incoherent output.** The failure is not a merge conflict — it is two changes
  that both apply cleanly and together do not work. Structural metrics cannot see it.
- **Over-spawning.** Anthropic observed agents spawning 50 subagents for simple queries and "distracting
  each other with excessive updates" — a coordination layer makes spawning cheap, and cheap spawning is a
  failure mode with a 15× token price. *(Verified, [S1])*
- **Not recognising termination (12.4% of MAST failures)** and **step repetition (15.7%)** — the two most
  frequent modes are both about *stopping*, which is a protocol property, not a model property.
- **A stale read model presented as current.** The projection is eventually consistent with the log by
  construction; a UI or tool that renders it as authoritative will occasionally lie.
- **Coordination files becoming the conflict.** The spec anticipates this and answers it correctly with one
  append-only file per session; the reason it works is specific — git's `union` driver does not deduplicate,
  so shared files would corrupt rather than merge. *(Verified, [S19])*
- **Path-unaware hooks.** Shared `.git/hooks/` plus a project-root `${CLAUDE_PROJECT_DIR}` means a hook can
  silently check the wrong tree. *(Verified, [S12])*
- **Enforcement asymmetry across tools.** Claude Code can block an edit at `PreToolUse`; Copilot's cloud
  agent has no interception surface at all. A protocol enforced for one agent and merely advertised to
  another is not enforced.

## Disconfirming views we deliberately sought

### 1. Cognition: "Don't Build Multi-Agents" — the strongest case against this architecture

**The argument.** Parallel agents produce fragile results because (a) agents lack shared context of each
other's *full trace*, and (b) **every action embeds implicit decisions** other agents cannot see. The
failure is not an error; it is two locally correct outputs that are globally incoherent. Prescription: a
single-threaded linear agent with context compression. They cite Claude Code's subagent design approvingly
as *purposefully simple* — subagents answer questions, they do not write code in parallel. *(Verified, [S4])*

**How it fared.** Partially answered, and importantly not by accident: **announcing intent before acting is
a direct response to Principle 1**, and a shared log that each agent reads before starting is more context
sharing than any of the frameworks they criticise. **Principle 2 stands unrefuted.** "I will implement OAuth
in the auth module" does not communicate the forty micro-decisions that follow it.

**What it changes.** The success criterion. Absence of merge conflicts is *not* evidence the approach works;
the system must be evaluated on **semantic coherence of the combined output**. The proposed metrics —
conflicts per PR, % edits on claimed paths, lease wait time, decisions re-litigated — are all structural.
At least one must measure whether the merged result is correct.

### 2. Anthropic's own caution — the most authoritative data point

**The argument**, quoted: *"most coding tasks involve fewer truly parallelizable tasks than research, and
LLM agents are not yet great at coordinating and delegating to other agents in real time."* This is the team
running the most capable deployed multi-agent system, declining to recommend it for coding. *(Verified, [S1])*

**How it fared.** It stands entirely. It does not invalidate building the infrastructure — but it means the
expected benefit is **modest and highly task-dependent**, and that any claim of throughput gain must be
measured rather than assumed. It also implies the coordination layer's value may lie less in *speed* than in
*auditability and safety*, which are real and different benefits.

### 3. METR's RCT — the perception gap

**The argument.** 16 experienced developers, 246 real issues: **19% slower** with AI tools while believing
they were **20% faster**. *(Verified, [S11])*

**How it fared.** It disconfirms the *perception* of productivity rather than multi-agent coordination
specifically — but the implication is uncomfortable and honest: if single-agent AI assistance is already
slower for experienced developers on mature codebases, coordination overhead across several agents could
amplify rather than offset that. **No multi-agent-specific RCT exists to settle it.** *(Flagged)*

### 4. Kleppmann on lease-based locking — the correctness attack

**The argument.** Any system relying on lease + TTL + heartbeat *without* fencing tokens enforced at the
write target is unsafe: a paused process can hold an expired lease and still write. *(Verified, [S6])*

**How it fared.** **Unrefuted in the literature, and Chubby agrees.** The distinction it forces is the useful
one: leases are safe for **efficiency** (duplicate work is merely wasteful) and unsafe for **correctness**
(two agents both confident they hold a claim). The design must either add a fencing token at the write path
or state plainly that claims are advisory-for-efficiency — both are defensible; silence is not.

## What survives

The four objections together do not defeat the approach; they reshape it. What survives is a design whose
honest value proposition is **auditability, replay and pre-edit overlap detection** — not throughput —
whose leases are explicitly advisory unless fenced, and whose evaluation must include a semantic coherence
measure that no structural metric can substitute for.
