---
id: note-conductor-subscription-use-authorised
title: "Decision note — operator authorises use of their own Max subscription for governed lanes"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, subscription, licensing, human-ruling, escalation]
links:
  - { to: note-conductor-tos-invariant-observed-auth, rel: relates-to }
  - { to: plan-conductor-programme, rel: relates-to }
review-by: 2026-12-09
summary: >-
  The Owner escalated the licensing question to the human operator and declined to rule on it.
  The operator has authorised use of their own Max subscription for this project. Re-plan
  checkpoint 3 is closed and the Phase-1 exit run is unblocked.
---

# Decision note — operator authorises use of their own Max subscription

**Ruled by:** the **human operator**, 2026-09-09. This is the one decision in this programme that
was **not** delegated: the Owner agent explicitly declined to rule, on the grounds that a commercial
question about the operator's own account and the product's distribution sits outside spec-reading
and outside the authority delegated to it.

## The question, and how it arose

The ACP spike (`spikes/acp-subscription-lane/`) established **by execution** that a non-Claude-Code
client can drive `claude-agent-acp` authenticated as **Claude Max with no API key**. It also found
that the adapter ships a `--hide-claude-auth` flag whose stated purpose is to let redistributors
**disable** subscription auth. The spike therefore flagged, and did not close:

> *"Do not treat 'it worked' as permission."*

Spec §4.2 had already recorded the constraint and its mitigation — *"Anthropic prohibits third-party
tools from using Pro/Max subscriptions"*, with the enacted consequence that **every Anthropic-bound
request originates inside a Claude Code process**. The spike confirmed the mitigation holds
mechanically: the adapter wraps the official Agent SDK and reads the **local `claude` CLI's
credential store**, so AI-DE never handles subscription credentials (§4.3). What was unverified was
the *adequacy* of that mitigation, not its existence.

## Ruling

**The operator authorises use of their own Max subscription** for AI-DE's governed lanes in this
project. The decision was reaffirmed on being raised.

## Effect

- **Re-plan checkpoint 3 is CLOSED.** It no longer gates the plan.
- **The Phase-1 exit run (N7) is unblocked** — a real governed run on `claude-code`, scored
  end-to-end, on the Max account.
- **The E18 close is unblocked** for Owner counter-signature.

## What this ruling does and does not cover

**Covers:** the operator using their own subscription, on their own machine, for their own project.

**Does not cover, and is out of scope until it arises:** distributing AI-DE such that *other* users
drive *their* subscriptions through it. That is the scenario `--hide-claude-auth` exists for, and it
is a distribution decision, not a build decision. Nothing in Phase 1 requires it, and this note
should not be cited as settling it.

**Unchanged by this ruling:** the technical invariant of
[[conductor-tos-invariant-observed-auth]] stands in full. An API-key environment source still
**outranks** the stored subscription and would bill the API silently, so the spawn gate still keys
on the **observed** auth status and still **fails closed** when that status is absent. That control
protects the operator's *money*, which is a separate concern from their *permission*, and this
ruling does not relax it.
