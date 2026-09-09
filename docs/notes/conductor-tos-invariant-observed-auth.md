---
id: note-conductor-tos-invariant-observed-auth
title: "Decision note — the ToS invariant is enforced against observed auth status, fail closed"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, tos, subscription, security, fail-closed]
links:
  - { to: spec-conductor, rel: refines }
  - { to: note-conductor-acp-lane-separate-shape, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Spec §4.2 forbids an Anthropic direct-api entry while a subscription is configured, but the
  real failure is an environmental API-key override that outranks the subscription and bills
  silently, with no spawn to reject. Phase 1 gates every claude-code spawn on the adapter's
  observed auth status, fails closed when it is absent, and attaches the override list as the
  refusal reason.
---

# Decision note — the ToS invariant is enforced against observed auth status, fail closed

**Ruled by:** Owner agent, 2026-09-09. Confidence: **Verified** for the spec text and the observed
status line; **Inferred** for the adapter's internal override list (the adapter source is not in
this tree — the spike reports it).

## The gap

Spec §4.2: *"The AgentPlane MUST NOT offer an Anthropic direct-api entry while a subscription
account is configured."* R1's last acceptance bullet: *"An Anthropic direct-api spawn attempt is
rejected with the ToS reason while a Max account is configured."*

The spike found that `ANTHROPIC_API_KEY`, an `apiKeyHelper`, or a `/login` managed key **outrank**
the stored subscription inside the adapter. So a lane can be launched **believing it is on the
subscription and be silently billed to the API** — with **no spawn attempt to reject**.

Read literally, the spec's invariant guards an explicit direct-api *entry*. The real failure is an
implicit *environmental override*. §4.2 says the invariant is *"enforced in code"* and names the
object of that enforcement as **where Anthropic-bound requests bill** — which a silent override
defeats entirely.

## Ruling

Enforce **both**, shaped smallest-correct:

1. **Reject an explicit direct-api spawn** with the ToS reason — the spec's letter.
2. **Gate every `claude-code` spawn on the adapter's OBSERVED auth status** matching the configured
   subscription account, and **fail closed when that status is absent.**
3. The **API-key-source list is the attached refusal *reason*, not the primary detector.**

## Because

The spike observed, live:

```
_auth/status_update → {"authStatus":{"kind":"account","label":"Claude Max","account":{"plan":"max"}}}
```

That is a **measurement** — version-robust, and the thing to assert. The
`API_KEY_SOURCES_ABOVE_SUBSCRIPTION` list is adapter-internal, unopened in this tree, and pinned to
a version; making it the primary detector would bind the invariant to a constant that can be
renamed.

This is **not new scope**: §4.3 already specifies an *"auth-status command"* probe with the three
states `ready` / `needs-login` / `quota-degraded`. This is a reading of that probe requirement.

Option (b) — enforce only the letter, record the override as a finding — is **refused**: a UI that
reads `max-personal` while billing an API key is a **false measurement**, and this pack does not
ship those.

## Conditions

1. **Absent or unparseable auth status degrades to "not recorded" and REFUSES the spawn.** It never
   assumes subscription.
2. `_auth/status_update` is an `_`-prefixed **extension**, so the assertion must survive its absence
   — which condition 1 already specifies — and must **pin `protocolVersion: 1` and assert the echoed
   value.**
3. The override list is copied from the **pinned** adapter's source with a `file:line` citation at
   that version, and **re-checked on every adapter bump** by a test that fails if the pinned
   constant's name or contents change.
4. **Red-first:** the refusal test exists and is observed failing before the gate does.

## Scope effect

**Admits** into Phase 1: the probe mapping observed auth status to `ready`/`needs-login`; the spawn
refusal when observed auth kind is not a subscription for a subscription-configured account; and
the env-source check as explanatory detail on that refusal.

**Cuts** a fourth probe state, and any repurposing of `quota-degraded` for this.

## Related, and NOT ruled here

Whether a third-party product **may** drive a customer's Max subscription is a **commercial and
licensing question**, outside the authority delegated to the Owner and explicitly escalated to the
human operator. See `spikes/acp-subscription-lane/README.md` residual risk 1. The spec itself
states at §4.2 that *"Anthropic prohibits third-party tools from using Pro/Max subscriptions"* and
enacts a mitigation — so the prohibition was known when subscription-first was locked; what is
unverified is the **adequacy of the mitigation**, not its existence.
