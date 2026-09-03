---
id: design-watcher-daydream-dream-seam
title: "Loomkeeper Daydream and the seam to the offline Dream"
type: design
status: proposed
owner: "@timianmalloo"
phase: "3"
tags: [loomkeeper, watcher, design, daydream, dreaming, continuous-improvement, learning, seam]
links:
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-work-episode, rel: depends-on }
  - { to: design-watcher-scoring-service, rel: depends-on }
  - { to: design-watcher-advisory-grader, rel: depends-on }
  - { to: adr-0017-watcher-observation-projection, rel: depends-on }
review-by: 2027-03-02
review-suggested: []
summary: >-
  Closes the spec's open item "Daydream-to-Dream schema alignment and deletion/retraction need
  design". Daydream is the online half of continuous improvement — per-episode, cross-harness,
  running while agents work; the pack's offline /dream is the batch half. Daydream emits candidates
  in a shape dream.py ingests as signals, one-way, and the pack stays an optional detected
  integration rather than a runtime dependency of the product.
---

# Design: Loomkeeper Daydream and the seam to the offline Dream

## 0. The open item this closes

The spec records, in its own residual list:

> Daydream-to-Dream schema alignment and deletion/retraction need design.
> — `spec-agentic-watcher-substrate`, residual unknowns

Both halves are answered here. Nothing in this design is built yet; it is a proposal, and its
status stays `proposed` until a slice implements it.

## 1. Responsibility and boundary

**Daydream is the online half of continuous improvement.** It watches Work Episodes as they close,
notices that something has happened more than once, and proposes a Candidate Lesson with its
evidence and its counter-evidence. It runs *between* agents, inside a repository, while work is
happening, and it enriches the memory the next session reads.

**The pack's `/dream` is the offline half.** It runs over the whole committed corpus on demand,
scores candidates by frequency, recency and diversity, applies a threshold gate, and renders a
human review view. Its promotion is the only durable write and it is human-gated.

They are not competitors and neither subsumes the other:

| | Daydream (online) | Dream (offline) |
|---|---|---|
| Cadence | Continuous, per closed episode | On demand, over a window |
| Scope | One repository's live sessions | The whole committed corpus |
| Sees | Any harness Loomkeeper observes | Only pack-instrumented sessions |
| Produces | Observations and Candidate Lessons | Scored proposals with controls |
| Promotes | **Never** — proposes only | Yes, through a human gate |
| Lives in | AI-DE (this product) | `dream.py` (the AI-Forward Pack) |

The line between them is the line the whole system already draws: **observation is deterministic
and continuous; promotion is batched, adversarial, and human.** Daydream inherits the spec's
explicit non-goal — *automatic promotion of Daydream learning* — and never crosses it.

### Why this is an up-level and not a fourth feature

The board, the leaderboard and the ledger all **record**. Daydream is the only instrument that
changes what happens next. Without it a leaderboard is a scoreboard nobody learns from: the system
can tell an agent it ranked third and cannot tell the next agent why. Daydream is what turns the
recording surfaces into a loop.

## 2. Data model

Grain declarations, in the spec's own terms.

| Shape | One row is exactly one… | Key / order | History |
|---|---|---|---|
| `daydream_observation_fact` | observed occurrence of one candidate pattern in one Work Episode at one observation time | `{repository, pattern signature, episode, observed_at}`; watcher ingress sequence | Append-only. An observation is never edited; a re-observation is a new row. |
| `candidate_lesson_fact` | proposal of one pattern as a lesson, at one proposal time, from one observation set | `{repository, pattern signature, proposal sequence}` | Append-only. A state change (needs-disconfirm → disconfirmed → promotable) is a new row folded on read. |
| `learning_link_fact` | one binding between a Candidate Lesson and one external learning record | `{candidate, external system, external id}` | Append-only. Retraction is a superseding row, never a delete. |

**Derived, never stored:** the candidate's state, its confidence, and its occurrence count. All
three fold from the observation and candidate rows on read. Two definitions of one quantity is a
defect signature (DM7) and the scorer already refuses to store a Weave total for the same reason.

**The pattern signature is the only interesting key.** It is what makes two occurrences "the same
thing", and getting it wrong produces either a register full of near-duplicates or a single class
so general it prevents nothing. It is deterministic and derived from the episode's *typed*
signals — tripped floor domain, verdict, the guidance trigger that went unsatisfied — and **never
from prose**, for the reason in §7.

## 3. Promotion is a staircase with a human on every landing

```
observation ──(recurrence ≥ N, deterministic)──> candidate
candidate  ──(disconfirming check attached AND survived)──> promotable
promotable ──(human decision, in the tool or in /dream)──> promoted
promoted   ──(source corrected, deleted, or contradicted)──> retracted / superseded
```

One occurrence stays an Observation and is **not** generalised — that is US-9's first acceptance
criterion and it is the rule most likely to be quietly relaxed under pressure to show the feature
doing something. A candidate with no disconfirming check has promotion *disabled*, not discouraged.

## 4. The seam — REVISED after the spike falsified it

> **The original design was wrong, and the `Inferred` label was doing real work.** §12 recorded
> that the proposed shape's acceptance by `dream.py`'s stager was inferred from the script's
> behaviour rather than from a specification, and that a spike must confirm it. The spike was run on
> 2026-09-02 and **falsified it**.
>
> `load_corpus` (`dream.py:147`) reads exactly five fixed paths — the audit log, the change log,
> `mitigations.jsonl`, `defect-classes.md`, and `simplify:`/`assume:` markers grepped from source.
> `cmd_run` accepts `--root`, `--session` and `--days`. **There is no inbox, no discovery, and no
> extension point.** An emitted `docs/dreams/inbox/*.jsonl` would have been written and never read,
> which is DC-089's shape — a producer with no consumer — built deliberately.
>
> What follows replaces the original. The corrected seam is *narrower and stronger*: a candidate
> does not cross at all, and only a **promoted** learning does.

### What crosses, and why only that

`dream.py`'s corpus is evidence of things that **happened**. A Daydream candidate is a **proposal**,
and proposals are what its own review gate exists to filter. Pushing candidates into that corpus
would put unreviewed material into the input of the process whose job is reviewing — and into
`mitigations.jsonl` specifically, it would corrupt the **promotion oracle**, the one signal meaning
*this fix is proven*. That refusal is the important half of this design.

A **promoted** Daydream learning is different. Promotion requires a surviving disconfirming check
and a human decision, which is exactly what `capture-mitigation --oracle human-validated` means:
*you approved a change*. So a promoted learning satisfies the oracle's real meaning rather than
abusing its shape.

| Direction | What moves | When |
|---|---|---|
| **Out** | A promoted learning, as a `MitigationRecord` with the `human-validated` oracle | Only after the full staircase |
| **In** | `defect-classes.md` and `mitigations.jsonl`, read | Any time; marks a candidate already-known so it stops being re-proposed |

### What is NOT built yet, and why

The **outbound** half writes into the repository the user is working on. That is a material change
in what the product does — AI-DE reads repositories and, so far, writes only into its own workspace
store. Making it a writer of repository content is a decision for the owner, not one to take while
they are away. **The inbound half is built** (it only reads, and degrades to "not recorded" when the
pack is absent); the outbound half is specified here and deliberately unbuilt.

## 4a. The original seam (superseded, kept for the record)

Daydream writes Candidate Lessons into a **signal file** that `dream.py` can read as corpus input,
in the shape its stager already consumes. That is the whole of the "schema alignment" the spec
asked for.

```
AI-DE / Loomkeeper                         AI-Forward Pack (optional)
──────────────────                         ─────────────────────────
Work Episodes (any harness)
      │
      ├─> Daydream Observations ──> Candidate Lessons
      │                                   │
      │                                   ▼
      │                          docs/dreams/inbox/*.jsonl     one-way
      │                                   │
      │                                   ▼
      │                          dream.py run  ──>  review view  ──> apply-decisions
      │                                                                    │
      └─< Promoted Learning read back as a fact <──────────────────────────┘
                  (link only; the register stays the pack's)
```

**One-way, deliberately.** Daydream emits; it never invokes. The reverse direction is a *read* —
AI-DE reads the promoted register to mark a candidate as already-known, so it stops re-proposing
something the human already accepted. That read is the whole integration, and it degrades to
"not recorded" when the pack is absent rather than to a wrong answer.

**Both sides already read the same file.** `WatcherHost` sources episodes from
`docs/audit/audit-log.jsonl` via `AuditLogEpisodeSource`, and that is `dream.py`'s corpus too. The
alignment is therefore much smaller than the residual item implies: the two systems already agree
on what an episode is and where the record lives. What is missing is only the candidate shape.

### Deletion and retraction — the second half of the open item

The rule is one sentence: **a retraction propagates forward along the same one-way seam, and a
deletion upstream invalidates everything derived from it.**

| Event | Daydream | The pack's register |
|---|---|---|
| An episode is deleted under retention policy | Its observations are invalidated by a superseding row; any candidate whose occurrence count falls below the threshold returns to Observation | Unaffected until the next run; a re-run sees fewer signals |
| A candidate is disconfirmed | A superseding row marks it Disconfirmed; promotion stays blocked | Never received it — only promotable candidates cross the seam |
| A promoted learning is retracted in the pack | Read back; the candidate loses its already-known mark and becomes eligible again | The pack's own supersession record is authoritative |
| A repository is deleted | All Daydream rows go with the workspace database | Untouched — the register is repository-local or fleet, and the pack owns its own lifecycle |

Nothing is deleted in place on either side. This is the same append-only discipline the fact store
already enforces with `RAISE(ABORT)` triggers, and Daydream gets no exemption from it.

## 5. Why the script is not lifted into the product

The obvious move is to shell out to `dream.py` from a menu command. Three reasons not to.

**It inverts the dependency the environment contract just established.** AI-DE registers and
serves *any harness, any model, with no pack and no directives required*
(`ux-agent-session-registration`). If the product calls `dream.py`, the product needs Python and a
vendored pack in every repository it opens. The constraint would be broken by the first
integration built on top of it.

**The interesting half is the half only the product can do.** `dream.py` reads the committed
corpus, which exists only where the pack is installed and its skills are run. Loomkeeper observes
live sessions across harnesses — including a Copilot session or a plain shell that will never write
an audit entry. **Daydream inside the product is the only route by which a non-pack agent
contributes a learning at all**, and that is precisely the multi-harness improvement loop this is
for. Lifting the script would deliver the half we already have.

**An optional integration must be detected, never assumed.** The affordance is a command that
appears only when `docs/ai-forward-pack/scripts/dream.py` is actually present and a Python
interpreter actually resolves — both checked at the moment of use, not cached from startup, and
both reported honestly when absent. A menu item that fails because a tool is missing is worse than
no menu item, and a capability inferred from a directory listing is a guess.

## 6. Failure-mode analysis

| Failure | Detection | Behaviour |
|---|---|---|
| Pattern signature too general — one class absorbs everything | Occurrence count grows without bound while distinct-episode diversity stays flat | Candidate is held at Observation and flagged for signature review; never promoted on volume alone |
| Pattern signature too specific — a register of near-duplicates | Many candidates, each at exactly the recurrence threshold | Surfaced as a review finding; deduplication is `dream.py`'s job and it already does it |
| The pack is absent | Detected at use | Daydream works; the seam reports "no consolidation configured" — a stated absence, never a silent one |
| The pack is present but `dream.py` fails | Non-zero exit or unparseable output | Reported with the tool's own stderr. **A run that produced no output is never rendered as a clean run** (R4/CD9) — the craft gate's guard is the precedent, and its recent cp1252 defect is the precedent for the guard being right and its diagnosis being wrong |
| Episodes stop being observed | Freshness probe on episode arrival rate | A health incident, the same shape as silent watcher loss; Daydream reports stale rather than reporting nothing found |
| An agent games recurrence by repeating a pattern deliberately | Distinct-operator and distinct-session diversity in the signature, not just count | A pattern from one session at one time is one occurrence regardless of how often it is restated |

## 7. Security and privacy

**Daydream never reads board prose or agent output as evidence.** The pattern signature derives
from typed deterministic signals only. This is the same invariance the scorer relies on: an
injection fixture cannot change a score because the scorer consumes `DeterministicEpisodeSignals`
and never text, and Daydream must inherit that property rather than re-earn it. A Candidate Lesson
may *display* quarantined text as context; it may never be *keyed* on it.

**A model may propose, never promote.** If an advisory evaluator enriches a candidate's phrasing,
it sits behind the same capability gate and the same calibration requirement as the advisory
grader (ADR-0019), and its output is excluded from any threshold until calibrated — declared and
excluded, never stubbed with a plausible number.

**Egress stays opt-in and per-path.** Daydream is local and deterministic. Any model-backed
enrichment is a credential-backed egress path under ADR-0018 and is off by default.

**Learnings are about tools, not people.** The leaderboard already refuses a per-operator facet,
and Daydream inherits that: a candidate is attributed to episodes, harnesses and models, never to
an operator. A cohort that resolves to one person is a privacy proxy for that person whatever it is
called.

## 8. Instrumentation (IO1)

Emitted on the normal path, no flag: observations recorded, candidates proposed, candidates held
below threshold, disconfirming checks attached, candidates disconfirmed, candidates crossing the
seam, promoted learnings read back, and seam failures by cause. The operator question this must
answer without a debugger is **"is Daydream seeing anything, and is any of it getting through?"** —
a system that proposes nothing looks identical to one that is not running.

## 9. Test plan

- One occurrence never becomes a Candidate. The first acceptance criterion, and a red-first test.
- A candidate with no disconfirming check cannot be promoted through any path, including the seam.
- A disconfirmed candidate stays blocked and is not re-proposed on the next observation.
- Deleting a source episode returns a candidate below threshold to Observation.
- A retracted promoted learning makes its candidate eligible again.
- **An injection fixture in board content does not change any pattern signature.** The scorer's
  invariance test, pointed at Daydream.
- The seam with the pack absent reports absence, and is not mistaken for "nothing found".
- A `dream.py` run that produces no parseable output is reported as a failure, not as clean.

## 10. Ladder and simplicity

Rung 2 (reuse in codebase) for consolidation: `dream.py` already scores, dedupes, gates and renders
a review view, and rebuilding that inside the product would be a second implementation of a solved
problem. Rung 1 (YAGNI) for the online half: recurrence detection over typed signals is *counting*,
not inference, so it needs no model and no new dependency.

What is deliberately **not** built: a Daydream scheduler, cross-repository learning federation, and
any automatic promotion. All three are in the spec's non-goals or its later phases.

## 11. Sequencing — what has to be true first

Daydream over imported audit entries would only re-read what `/dream` already reads, which is the
half we have. The order that makes it worth building:

1. **Wire live episode capture.** `WorkEpisodeService` exists in Core and nothing in the App wires
   it, so today's episodes are imported from pack audit entries rather than observed. This is the
   upstream unblock and it is small.
2. **Start capturing mitigations now.** `dream.py`'s promotion oracle is empty in this repository —
   `docs/dreams/` does not exist and no `MitigationRecord` has been captured. Its own rule is that a
   fix with neither a red→green transition nor human validation is `unverified` and is not mined.
   The oracle only accumulates forward, so every week without capture is a week the offline half has
   nothing to consolidate.
3. Then Daydream observations, then candidates, then the seam.

## 12. Open questions

- **The recurrence threshold N.** The spec says "repeated evidence or a deterministic reproduction"
  without a number. It should be a declared safety floor with its statistical basis recorded, and it
  may tighten but never silently relax — the same treatment the cohort minimum gets.
- **Where the signal file lives when the pack is absent.** `docs/dreams/inbox/` assumes a pack
  directory. A pack-free repository needs a neutral location, and choosing one is a small decision
  that should not be made implicitly by the first implementation.
- **The pack's dreaming authority is not in this repository.** The `/dream` skill cites
  `spec-dreaming`, `architecture-dreaming` and `docs/knowledge/continuous-improvement-and-dreaming/`
  as its design authority; none of the three is present here. The script and skill shipped, the
  specification did not. This design therefore treats `dream.py`'s **observed behaviour** as the
  contract, which is weaker than reading its spec and is labelled as such.

## 13. Gate record

| Lens | Position |
|---|---|
| Test Architect | Not cleared — this is a proposal with a test plan, not a tested slice. |
| Security &amp; Identity | Signature-from-typed-signals is the load-bearing control; needs review before a slice. |
| Privacy &amp; Data Governance | No per-operator attribution; retention follows the episode. Needs review before a slice. |
| The Simplifier | Accepts rung-2 reuse of `dream.py`; would reject any second consolidation engine. |
| Documentation Steward | Closes the spec's residual item; the residual list should be updated when a slice lands. |

**Confidence.** Verified: the current implementation state (`WatcherHost.cs:95`, `WorkEpisode.cs:63`,
the absence of `daydream` in `src/`, the absence of `docs/dreams/`). Verified: the spec's
requirements and non-goals, quoted. Inferred: that `dream.py`'s stager will accept the proposed
signal shape without change — this was read from the script's behaviour, not from a specification,
and must be confirmed by a spike before a slice depends on it.
