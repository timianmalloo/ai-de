---
id: note-addendum-d-compile-step-proposal
title: "Decision note — the compile step: the operator's thinking and the conductor's proposal, ratified"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, addendum-d, compile, composer, envelope, conductor]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: plan-addendum-c-modes, rel: relates-to }
review-by: 2026-12-11
summary: >-
  The operator's own thinking on the compile step (four contexts; mechanical then agentic; a
  prepare stage; a rich envelope; the template as a guide), the conductor's proposal answering the
  operator's questions, and the operator's ratification. This note is the input to /specify
  Addendum D; it is not the spec.
---

# The compile step — what the operator said, what the conductor proposed, what was ratified

## 1. The operator's words (verbatim; audit entries `al-01M28YSMW5C0C6VZJVSMD93HBG`, `al-01M28ZFJW6MCAMFS8KHYNQ77G1`, `al-01M2917WRTT5JP9EEV2C9FTW9E`)

> shouldn't tier be decided by the compilation of the prompt? a key aspect and benefit of being
> able to type a prompt and then post process it would be to decorate it with things like tier
>
> that makes me realize we haven't really thought through the compile step explicitly — maybe I
> need to do that

> answers as I think through compile:
> meta: there are three types of context I think are needed
> — 1: the model-family (e.g. Anthropic, OAI, Grok..) you are working against as how we craft the
> prompt to be most effective, for example my perception is OAI models add more ceremony and more
> drift per turn and we need to consider that potentially
> — 2: the conversation and session history
> — 3: the repo and its standards/constitution and its particular skills (for example my particular
> work loop in the ai-forward repo)
> — 4: I think compile should be 1st mechanical then agentic... i.e. there is the initial
> pre-compile step which is mechanical decoration etc, then hand off to a model to "compile" the
> final prompt. The model would be the same model that the session is bound to... much like in
> claude code I choose a model for the conversation but execution may be any number of agents with
> different models
>
> with that here are my thoughts on your questions
> 1: I think it needs the text, the session context, the repo constitution and the target model family
> 2: I think we need a separation of concerns between what an operator can tune in "settings" vs.
> what is uniquely contextual at compile... you should propose the differentiation
> 3: I think if you do the enumeration I asked for in the answer to 2 you can also recommend the
> answers for three
> 4: post compile should be like a "prepare" where the operator may override before submitting to
> the console
> 5: it should be a rich envelope... again the fields should not be a rigid structure... the
> template and experience must be a conversation where the template is a guide vs. a constraint
> because the compile can create the appropriate structure through further decoration

> yes I am aligned with addendum D

## 2. The conductor's proposal (verbatim as put to the operator; ratified by the line above)

Grounded in what exists: `ComposerCompiler` · `TemplateCompiler` · `LeaseDerivation` ·
`SpawnContract` · `ComposerSendGate` · `ProviderConfiguration` (`~/.aide/providers.json`,
`engines.<engine>.model`) · Addendum A's session settings (backends, routing, autonomy, policy) ·
Rulings 42, 56, 57, 63 · the spec's shape rule (US-C13) and D-5.

### The pipeline

```
typed text ─► PRE-COMPILE (mechanical, T0) ─► COMPILE (agentic, the session's bound model) ─► PREPARE (operator) ─► SUBMIT
                    │                                  │                                        │
                    └ decorations tagged `mechanical`   └ decorations tagged `derived`           └ overrides tagged `operator`
```

The envelope accumulates; nothing downstream replaces an upstream decoration, it adds one with a
different `source`. Submit projects the envelope onto what the conductor already expects (the CT19
six-field block, the lease, the task class) — **the contract is unchanged; the authoring moves.**

### Q2 — the differentiation

| | **Settings** — operator tunes; stable across prompts | **Compile context** — read live at compile; not tunable per prompt | **Decorations** — derived per prompt |
| --- | --- | --- | --- |
| Model | binding: engine · account · model (→ **family**) | the family's **craft profile** (ceremony, drift-per-turn compensation, formatting conventions) — a versioned artifact the pack owns, one per family | framing applied for the family |
| Scale | fan-out **ceiling** · budget · autonomy | — | effective fan-out (≤ ceiling) · **tier** (+ rationale) |
| Shape | template catalog (what's available) · compile mode (mechanical-only ∣ agentic) | template *contents* | shape (Message ∣ Goal-block) · structure (goal / done-when / not-in-scope) · template applied |
| Scope | policy: attach on/off, egress basis | workspace state: branch, worktree, dirty files | `@mentions` → **lease** (Ruling 42, mechanical, never agentic) · task class (default from settings, refined from text) |
| Knowledge | — | repo constitution (CLAUDE.md/AGENTS.md, instruction docs, defect register) · **skills manifest** · session history · recent proof packs | which rules/skills are cited into the prompt · the history excerpt selected |

The test for the column: *settings* change how **every** prompt is treated and are set once;
*context* is true of the repo/session right now whether or not the operator looks; *decorations*
exist only because of **this** text.

### Q3 — mechanical vs agentic, per decoration

| Decoration | Allocation | Why |
| --- | --- | --- |
| Lease from mentions | **Mechanical, always** | Ruling 42 / C17 — a model must never author a write scope. Agentic may *suggest* mentions in Prepare; the operator accepts them as mentions. |
| Shape · ceilings/budget inheritance · family-profile *selection* · constitution/skills *inclusion* (by manifest) · history window (by rule) | Mechanical | Deterministic from text + settings (GO11: prefer the T0 node). |
| Tier | **Mechanical first**, agentic later | A rule over the mechanical decorations (no structure → T0; structure + lease → T1; multiple leases / explicit fan-out → T2) ships now; the agentic recommendation replaces it only with an eval. |
| Structure derivation (D-5) · family-specific rewriting · skill relevance beyond the manifest · mention suggestions | **Agentic** — the session's bound model | This is the value of the step. Each output carries model+version, an inputs hash, a confidence, and the input it came from; each has a **mechanical fallback** ("compiled mechanically — model unavailable"), never a silent plausible prompt. |

The AI Systems Engineer's veto binds the agentic column: no agentic decoration ships without an
**eval harness** — and Prepare gives the corpus for free: **every operator override in Prepare is a
label** (the derived value vs. what the operator changed it to), measured from real use rather than
authored by the deriver's author (DC-127-proof).

### Q4 — Prepare

The compiled envelope rendered as the conversation's *prepared turn*: every `derived` decoration
visible and editable; ceilings from settings cannot be exceeded there (raising one is a settings
act); lease edits are mention edits; **Send is the confirmation** (the spec's rule stands).
Overrides are logged.

### Q5 — the envelope

`compiled-envelope/1`: `source_text` · `text` (final) · `decorations[]` each `{name, value,
source: mechanical|derived|operator, confidence, inputs, model?}` · `context_refs` (constitution
sha, skills, history window ids, family-profile id+version) · `projections` (the CT19 block, lease,
task class). Append-only per turn — the history *is* the data (DM: facts, not a shadow schema) — so
the audit trail, the proof pack and the eval corpus are one store. The template is a **guide** in
this model because it is one input to the structure decoration, not the schema of the envelope.

### Where it lands

Its own bounded context (aggregate: the compiled envelope per turn; the family craft profile a
versioned dimension) — **Addendum D, "the compile step"**, not a paragraph in C. Ruling 63 already
makes it a seam A1 must leave open; specifying D *before* A1 lets the architecture cover both.

## 3. What is fixed by this note, and what the spec decides

**Fixed (ratified):** the four stages and their order; the three columns and the column test; lease
mechanical always; tier mechanical-first; the agentic column gated by an eval; Prepare with Send as
confirmation; an append-only envelope with tagged decorations; the template as guide; the
session-bound model as the compiler.

**The spec decides:** the exact row-by-row membership of the three columns (the table above is the
conductor's first cut and may be corrected with evidence); the mechanical tier rule; the family
craft-profile's shape, ownership and versioning; the envelope schema; the Prepare surface's states;
how compile mode (mechanical-only ∣ agentic) degrades; what the history window rule is; the
projections' exact mapping onto `SpawnContract`; the eval harness's fixture policy.
