---
id: design-watcher-daydream-dream-seam
title: "Loomkeeper Daydream and the seam to the offline Dream"
type: design
status: accepted
owner: "@timianmalloo"
phase: "3"
tags: [loomkeeper, watcher, design, daydream, dreaming, continuous-improvement, learning, seam]
links:
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-work-episode, rel: depends-on }
  - { to: design-watcher-scoring-service, rel: depends-on }
  - { to: design-watcher-advisory-grader, rel: depends-on }
  - { to: adr-0023-watcher-observation-projection, rel: depends-on }
review-by: 2027-03-02
review-suggested: []
summary: >-
  Closes the spec's open item "Daydream-to-Dream schema alignment and deletion/retraction need
  design". Daydream is per-repository and its record lives IN that repository, written by the
  product and marked with its provenance; Dream is learning across repositories and stays the
  pack's. Revised twice: a spike falsified the original emit direction, and the owner then answered
  the boundary question the first revision was built around — so the seam is no longer a pipeline
  Daydream pushes into, and the pack stays an optional detected read.
---

# Design: Loomkeeper Daydream and the seam to the offline Dream

## 0. The open item this closes

The spec records, in its own residual list:

> Daydream-to-Dream schema alignment and deletion/retraction need design.
> — `spec-agentic-watcher-substrate`, residual unknowns

Both halves are answered here.

**Built as of 2026-09-03**, which this section said it was not: the observation engine, the
promotion staircase, the repository record, the recorder, the reach probe, the pane, and the
`episode.artifacts` evidence channel. What is NOT built is the path verifier that turns a declared
artifact into an observed one, and no candidate has ever been promoted.

*This paragraph used to read "Nothing in this design is built yet". It stayed there through six
landings. An artifact describes the tree it is in (DC-094), and a status field is the first thing a
reader trusts and the last thing anyone updates.*

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
| Written by | AI-DE (this product), **into the repository** — §4a | `dream.py` (the AI-Forward Pack) |

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

## 4. The seam — REVISED TWICE, and the second revision is larger than the first

> **Revision 1 (spike, 2026-09-02) — the emit direction was falsified.** §12 recorded that the
> proposed shape's acceptance by `dream.py`'s stager was *inferred* from the script's behaviour
> rather than read from a specification, and that a spike must confirm it. The spike ran and
> **falsified it**. `load_corpus` reads exactly five fixed paths; `cmd_run` accepts
> `--root`, `--session` and `--days`. **No inbox, no discovery, no extension point.** An emitted
> `docs/dreams/inbox/*.jsonl` would have been written and never read — DC-089's shape, a producer
> with no consumer, built deliberately.
>
> **Revision 2 (owner decision, 2026-09-02) — the question the first revision answered was the
> wrong question.** Revision 1 narrowed the seam to "only a promoted learning crosses" because
> writing into the user's repository looked like a boundary the product should not cross. The owner
> answered that boundary differently and more broadly
> (`note-20260902-two-decisions-the-loop-waits-on`):
>
> > "day dreaming is specific to a repo and should be maintained in the repo … i.e the product
> > should write to the repo … dreaming is the act of learning across repos"
>
> So the narrow seam is no longer the constraint this design was built around. **Daydream is not a
> producer feeding a pipeline. It is a per-repository record the product maintains, in the
> repository.**

### The split, named correctly

| | **Daydream** | **Dream** |
|---|---|---|
| Scope | One repository | Across repositories |
| Lives | **In that repository** | The pack's fleet store |
| Written by | **The product** | The offline pass, human-gated |
| Cadence | Continuous, per closed episode | On demand, over a window |
| Sees | Any harness Loomkeeper observes | Only pack-instrumented sessions |
| Promotes | Never — proposes only | Yes, through a human gate |

A lesson about *this* repository belongs *with* that repository, for the same reason
`defect-classes.md` is committed rather than kept in a tool's private store: it survives a machine
change, it travels with a clone, and it is reviewable in a pull request. A learning locked in an
AppData database is a learning the next clone does not have.

### What still crosses to the pack, and what no longer needs to

The **refusal in revision 1 stands and is still the important half**: a Daydream *candidate* never
enters `dream.py`'s corpus. That corpus is evidence of things that **happened**; a candidate is a
proposal, and proposals are what the pack's own review gate exists to filter. Into
`mitigations.jsonl` specifically it would corrupt the **promotion oracle** — the one signal meaning
*this fix is proven*.

What changes is that **there is no longer an emit problem to solve**. Daydream's output is its own
committed record, in the repository, which `dream.py` can read or ignore. It is not pushing into
someone else's pipeline, so it needs no inbox and no schema alignment with a stager.

| Direction | What moves | When |
|---|---|---|
| **Out** | Nothing is *pushed*. Daydream writes its own record into the repository | Continuously, as episodes close |
| **In** | `defect-classes.md` and `mitigations.jsonl`, read (`DreamCorpusReader`) | Any time; marks a candidate already-known so it stops being re-proposed |
| **Optional** | A promoted learning captured as a `MitigationRecord` with the `human-validated` oracle | Only after the full staircase, and only where the pack is present |

The last row survives revision 1 unchanged and stays **optional**: promotion requires a surviving
disconfirming check and a human decision, which is exactly what `--oracle human-validated` means,
so it satisfies the oracle's meaning rather than abusing its shape. But it is now a convenience for
pack-using repositories, not the mechanism by which Daydream output becomes durable.

## 4a. Where Daydream state lives — and the cost this imposes on what is already built

**The repository is the record.** Two append-only logs, a sibling of the audit log they sit beside:

```
docs/daydream/observations.jsonl    one line per observed occurrence
docs/daydream/events.jsonl          one line per candidate state event
docs/daydream/index.md              the graph artifact — frontmatter, and the human-facing read
```

**Every line carries its own provenance**, not a header:

```json
{"generated-by":"ai-de/daydream", "signature":"…", "episode":"…", "observedAt":…}
```

Per-record rather than per-file because these logs merge by **content union** across sessions and
worktrees (`tools/merge-append-only-log.py`, the DC-026 control). A header line would be merged,
duplicated, or lost; a field on each record survives every one of those.

`docs/daydream/index.md` carries `generated-by: ai-de/daydream` in its frontmatter. **Verified**:
`docs-graph.py` treats `REQUIRED` as a required-key list and not an allowlist
(`_validate_frontmatter`), and the scanner appends the whole frontmatter dict as the index entry
(`scan`, which `cmd_derive` calls) — so the field reaches `docs/docs-index.js` and the Docs Explorer
with no script change.

### The cost, stated rather than buried

**D2 built these as SQLite tables** — `daydream_observation_fact` and `daydream_event_fact`, schema
version 3, in the per-workspace store under AppData. Under this decision the store is no longer the
record. Two definitions of one quantity is a defect signature (DM7), so the store does not keep a
parallel copy: `DaydreamFold` is a pure function over two lists and reading two JSONL files is as
cheap as reading two tables. The tables stay — deleting a shipped migration is worse than an unused
table — and the reader stops using them, which is recorded here so the next reader does not
conclude the schema is authoritative because it exists.

**Daydream requires a repository.** A workspace with no repository root has nowhere to write, and
that is reported as a **stated absence** — "no repository, so nothing is recorded" — never as an
empty Daydream. This is the same rule `DreamCorpusReader` already keeps for an absent pack.

## 4b. The provenance rule this is the first instance of

The owner's third answer draws the boundary somewhere other than where this design assumed:

> "the difference is that we want to make sure that things generated by the agents are always
> updated by the agents … but things generated by the product can also be written to a repo /
> workspace"

Not *who may write* — the product and its agents are one experience to the user, and agents write
into the repository continuously. **It is who maintains what they wrote.**

- Generated by an **agent** → updated by an **agent**. The product must not silently rewrite it.
- Generated by the **product** → the product owns it, and may write it into the repository.

Which makes the obligation **marking provenance**, agreed with the `ai-de-a7` session as one field:

| Format | Marker |
|---|---|
| Markdown | `generated-by: ai-de/<component>` in frontmatter, plus the human-readable "do not hand-edit" line |
| JSON / JSONL | `"generated-by": "ai-de/<component>"` — the **same literal spelling**, so one grep finds every instance in every format |

**Presence means product-generated; absence means agent- or human-generated.** Mark the rarer
thing. Every artifact in this repository today is agent- or human-written, so the inverse rule —
every artifact carries the field — would be thousands of lines of ceremony encoding the default.

**The gate is producer-side, not artifact-side.** A sweep over `docs/` cannot tell a product-written
unmarked file from an agent-written one, which is the whole problem it would be trying to solve. The
check is over the **writers**: every code path that writes into a repository emits the field. It is
not built here — the `ai-de-a7` session marks the standing file as the first instance, and the gate
lands with the second producer, which is this design.

## 4c. Deletion and retraction — the second half of the spec's open item

One sentence: **a retraction is a superseding row, and a deletion upstream invalidates everything
derived from it.**

| Event | Daydream | The pack's register |
|---|---|---|
| An episode is deleted under retention policy | Its observations are invalidated by a superseding row; a candidate whose distinct-episode count falls below the threshold returns to Observation | Unaffected until the next run |
| A candidate is disconfirmed | A superseding row marks it Disconfirmed; promotion stays blocked | Never received it — candidates do not cross |
| A promoted learning is retracted in the pack | Read back; the candidate loses its already-known mark and becomes eligible again | The pack's own supersession record is authoritative |
| **A repository is deleted** | **Its Daydream record goes with it — because the record is in it.** Under the old design the rows outlived the repository in an AppData store, which was the wrong lifetime | Untouched; the pack owns its own |

Nothing is deleted in place on either side. `DaydreamFold` already implements exactly this: evidence
folds **before** events, so a promoted learning whose source episodes disappear falls back to
Observation without anything being rewritten.

## 4d. The two superseded seam designs, kept for the record

**Original.** Daydream writes candidates into `docs/dreams/inbox/*.jsonl` for `dream.py` to ingest
as corpus input. Falsified by the spike: there is no inbox and no extension point, so the file would
have been written and never read.

**Revision 1.** Nothing crosses but a *promoted* learning, as a `MitigationRecord`, because writing
into the user's repository was a boundary the product should not cross uninvited. Superseded by the
owner's answer: the product may write to the repository, provided what it writes is marked as its
own. Revision 1's **refusal** — a candidate never enters the pack's corpus — survives; only its
premise about the boundary does not.

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
grader (ADR-0019 advisory-evaluator-calibration), and its output is excluded from any threshold until calibrated — declared and
excluded, never stubbed with a plausible number.

**Egress stays opt-in and per-path.** Daydream is local and deterministic. Any model-backed
enrichment is a credential-backed egress path under ADR-0024 credential-backed-grading-egress and is off by default.

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

### MEASURED 2026-09-03, against this repository's own audit log

The vertical works end to end. It has almost nothing to work with, and that is a **capture** gap
rather than a code one:

| | |
|---|---|
| Episodes imported and scored | **111** |
| Assessed, and clean | 7 |
| **Carried nothing to assess** | **103** |
| Would be recorded as a pattern | **1** |
| Observations actually written | 1 |

`docs/proof/` exists and holds real Proof Packs; 27 of 421 audit entries name one and 10 carry a
`signals` object. So the reader is finding what is there — most turns simply never recorded
evidence in their audit entry.

**The consequence is sharper than the ratio.** The recurrence threshold is two distinct episodes, so
**one observation can never become a candidate**. Daydream's output over this repository's entire
recorded history is *zero*, and it would be zero however good the engine is.

Which reframes item 2 above and generalises it. That item said to start capturing mitigations for
`dream.py`'s oracle. The same is true one level down for the product's own signals: **a turn that
does not record its Proof Pack or its signals is a turn Daydream cannot learn from, permanently and
retroactively.** `episode.artifacts` opens the channel for agent episodes going forward; it cannot
reach the 103 already written. Capture only accumulates forward.

## 12. Open questions

- **The recurrence threshold N.** The spec says "repeated evidence or a deterministic reproduction"
  without a number. It should be a declared safety floor with its statistical basis recorded, and it
  may tighten but never silently relax — the same treatment the cohort minimum gets.
- ~~**Where the signal file lives when the pack is absent.**~~ **Answered by §4a**, and the question
  dissolved rather than being decided: there is no signal file, because Daydream no longer emits into
  someone else's pipeline. Its record is `docs/daydream/`, which is neutral by construction — it
  belongs to the repository, not to the pack.
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

**Confidence.** Verified: the spec's requirements and non-goals, quoted. Verified by spike
(2026-09-02): `dream.py`'s `load_corpus` reads five fixed paths and `cmd_run` takes only
`--root/--session/--days` — there is no inbox and no extension point. Verified by reading:
`docs-graph.py` accepts an unknown frontmatter key (`_validate_frontmatter` checks REQUIRED as a
required-list) and the scan carries the whole dict into the index (`scan`, via `cmd_derive`), so
`generated-by` needs no script change.
Verified: `DaydreamFold` folds evidence before events, which is what §4c relies on.

**The Inferred label that was doing real work has been discharged.** The previous revision recorded
that `dream.py`'s acceptance of the proposed signal shape was inferred from behaviour rather than
read from a specification. The spike falsified it. That is the label working as intended, and it is
the reason the emit direction was never built — DC-089 avoided rather than registered.

**Still Inferred, and named so it is not mistaken for settled:** that two JSONL files are as cheap
to fold as two SQLite tables at the volumes this will see. Not measured — no Daydream record exists
yet to measure — and the model is that `DaydreamFold` already reads every row on every fold, so the
store buys no selectivity. If a repository's record grows past the point where that holds, the fix
is a derived `*_cell` cache with the JSONL still the record, not a return to the store as source.
