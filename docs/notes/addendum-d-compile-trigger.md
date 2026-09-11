---
id: note-addendum-d-compile-trigger
title: "The mechanical pre-compile runs on the debounced draft; the agentic compile runs on an explicit act — the Send gesture or a Prepare command — never on debounce"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, addendum-d, compile, prepare, composer, cost, hax]
links:
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
review-by: 2027-03-10
review-suggested: []
summary: >-
  A model call per keystroke burns the subscription window and shows model-authored content the
  operator did not ask for; so the pre-compile (T0) is live and the compile (T3) is explicit — the
  first Send gesture prepares, the second confirms; an unchanged inputs hash re-prepares with zero
  requests. Blast radius: the composer's gesture count under the agentic mode; US-C13's "debounced"
  wording for derived structure.
---

# The compile trigger — live pre-compile, explicit compile

- **Kind:** decision
- **Confidence:** Verified for the cost facts (one `session/prompt` per compile,
  `AcpLaneClient.cs:180-195`; the static prefix measured at 88,380 tokens,
  `context-budget.json`); **Inferred** that two gestures will be accepted by the operator
- **Made during:** `/specify` of `spec-addendum-d-compile-step` (node S2)

## The call

The **mechanical pre-compile** (shape, the write-scope projection, tier, effective fan-out, refs,
window) runs on the debounced draft — it is T0, pure and free, and its results are what US-C13's
"derived structure appears as you write" can honestly show without a model. The **agentic compile**
runs only on the Send gesture (Ctrl+Enter / the button): on a draft with no fresh envelope it opens
an envelope, compiles and enters Prepare; the next gesture confirms. There is no separate *Prepare*
command (the Simplifier's cut: a second entry point complicated the grain statement); a *Prepare
again* control on the compile line is the named recovery after a failed, degraded or suspect call
(the UX/IA condition). A draft edited after Prepare is stale and the next gesture re-compiles — the
model only if `inputs_sha` changed or the last call did not succeed, else the stored derived
decorations are reused and zero requests are made; a compile whose structure a template or the
operator already filled is skipped outright. One compile in flight per draft; a second gesture
during `preparing` is ignored with its reason; an edit during `preparing` cancels it.

Under `mechanical-only` a prompt is one gesture (US-C13's one-action property holds). Under an
agentic rung (`agentic-advisory`, then `agentic`) it is two, and the second is the confirmation of
model-authored content — HAX "show before acting"; an auto-send after a timeout was dismissed as a
G8 (efficient dismissal) violation. The two-gesture shape is the operator's own word (*"a 'prepare'
where the operator may override before submitting"*); that the second gesture is the *same key* is
the author's choice, labelled Inferred.

## Alternatives dismissed

- `compile on debounce` (US-C13 `:726-728`'s wording read literally for the agentic stage) — one
  request per settled edit against a plan window; and it puts model text under the editor the
  operator never asked to see.
- `a separate Prepare button plus Send` — a third control for what the same gesture can express;
  YAGNI.
- `auto-send N seconds after Prepare` — removes the confirmation the operator ratified ("the operator
  may override before submitting").

## Validation condition

Holds until/unless the operator is observed pressing Send twice as a reflex without reading (then
Prepare has become ceremony and the mode's value is re-examined), or a measured p95 compile latency
under 2 s makes an opt-in "compile as you write" affordable — then it is a setting, never the default.

## Promotion rule

If US-C13 is amended to cite this, or a second surface adopts the two-gesture rule, promote to an
ADR; until then this note is the origin and PR-D4 in the spec's §R is the Owner's question.
