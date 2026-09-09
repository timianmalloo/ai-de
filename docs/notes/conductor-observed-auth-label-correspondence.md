---
id: note-conductor-observed-auth-label-correspondence
title: "Decision note — the observed auth label is checked against a declared correspondence, never a derived one"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, acp, auth, subscription, fail-closed]
links:
  - { to: note-conductor-tos-invariant-observed-auth, rel: refines }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-09
summary: >-
  N3's ToS gate asserted the adapter's auth kind is "account" but never checked WHICH account,
  because the observed label ("Claude Max") is a plan-tier string and the configured label
  ("max-personal") is an operator's own name, and neither determines the other. Phase 1 closes
  the gap with an operator-declared correspondence that is enforced where it exists and reads
  "not recorded" where it does not.
---

# Decision note — the observed auth label correspondence

**Ruled and implemented at N4, 2026-09-09.** Confidence: **Verified** for both label values (both
observed live on `@agentclientprotocol/claude-agent-acp@0.75.1`); **Inferred** for the claim that no
other field can carry the correspondence without a privacy decision.

## The gap N3 named

`note-conductor-tos-invariant-observed-auth.md` ruled that Phase 1 must

> gate every `claude-code` spawn on the adapter's **observed** auth status **matching the configured
> subscription account**

and N3 implemented the first half: `SpawnContract.Authorize` refuses when the observed status is
absent, and refuses when its `kind` is not `account`. It did **not** implement the matching, and
said so in the code rather than leaving the hole implicit — it refused to invent a mapping inside a
control that exists because guessing is expensive.

## Why the two labels do not correspond

Both values were observed, live, on the same adapter build:

| Side | Value | What it names |
| --- | --- | --- |
| Configured (`providers.yaml`, §14.2) | `max-personal` | an operator's own name for one login |
| Observed (`_auth/status_update`) | `Claude Max` | the adapter's name for a **plan tier** |

Nothing in either determines the other. The adapter also reports `account.email` and
`account.organization`, which *would* identify the account — but putting an operator's email into a
committed configuration file is a data-governance decision, not a plumbing one, and it is not this
node's to make.

There is also a structural reason the adapter cannot answer the question on its own: it reads the
local `claude` CLI credential store, which holds **exactly one** login. It can say which account it
is on; it can never say which of several configured accounts the operator meant.

## The ruling

**A declared correspondence, enforced where declared.**

`ProviderAccount` gains `ObservedAuthLabel` — *what the adapter calls this account, as the operator
recorded it after seeing it once*. `SpawnContract.Authorize` then:

1. refuses when the observed status is absent (unchanged, N3);
2. refuses when `kind` is not `account` (unchanged, N3);
3. **refuses when `ObservedAuthLabel` is recorded and the observed label differs** — new, `AP-0013`;
4. checks `kind` alone when it is not recorded — which is **"not recorded", never a guessed
   mapping** (IO12).

## Because

**Option (a), derive the mapping** — refused. It would require a table from tier names to operator
names that nobody can write correctly, inside the one control whose entire purpose is to stop a lane
billing somewhere the operator did not intend.

**Option (b), record that `kind` is the whole check** — admissible under the plan's clause, and
rejected as the *only* answer because it leaves a real failure uncaught: an operator with two
subscription logins who runs `claude /login` against the other one gets a lane that binds to
`max-personal`, bills the other account, and ranks in the wrong cohort. Nothing anywhere would
notice.

**Option (c), the declared correspondence** — taken. It catches that failure for any operator who
has recorded the value, costs one nullable field, and degrades honestly for everyone who has not.

## Residual, stated

An operator who never records `ObservedAuthLabel` is exactly where N3 left them: the gate proves the
lane is on *a* subscription and not an API key, and does not prove *which*. That is a real remaining
hole and it is now a hole with a name, a field that closes it, and a test that fails when the field
is present and the labels disagree.

## Controls

- `SpawnContractTests.AnObservedLabelThatContradictsTheRecordedOneRefusesTheSpawn`
- `SpawnContractTests.AnObservedLabelThatMatchesTheRecordedOneAuthorizes`
- `SpawnContractTests.WithNoRecordedLabelTheCheckIsKindAloneAndTheAccountSaysSo`
