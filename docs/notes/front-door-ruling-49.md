---
id: note-front-door-ruling-49
title: "Decision note — Ruling 49: the F5 exit run waits for the operator's own gesture"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, ruling, exit-evidence, f5, headless-entry, session-origin]
links:
  - { to: proof-conductor-front-door, rel: relates-to }
  - { to: plan-conductor-front-door, rel: relates-to }
review-by: 2026-12-11
summary: >-
  A headless entry point was proposed to substitute for the operator's File → New Session gesture
  in the F5 exit run. Refused: it would satisfy the letter of clauses 1 and 5 while falsifying the
  sentence they exist to prove, and the origin guard would have stayed green while doing it. The
  exit run is triggered by the operator's own gesture; no headless entry point is built.
---

# Decision note — Ruling 49

**Filed from, and transcribed verbatim out of, Part 3 of `docs/proof/conductor-front-door.md`
("Part 3 — Ruling 49, and why the run waits for a hand").** This note does not re-derive the
ruling; it records it where `tools/verify-ruling-citations.py` can find it, since the Proof Pack
is markdown under `docs/proof/`, not a note under `docs/notes/`, and a citation with no note
defining it is unenforceable authority.

## Ruling 49 — the exit run is triggered by the operator's own `File → New Session` gesture, and no headless entry point is built

**Context.** Both operator-owned preconditions ahead of the F5 exit run were answered: the
provider file (`~/.aide/providers.json`) exists and was verified rather than trusted — one
`anthropic` account, label `max`, `engines.claude-code.model = claude-sonnet-5`, the adapter
install root resolves, and the pinned engine version matches. The account's health is
`quota-degraded`, which **binds** by `AccountHealth.QuotaDegraded`'s own words — *"Under quota
pressure — a signal, not an absence. A lane still binds, and carries the pressure."* Only
`NeedsLogin` is refused (`ProviderRegistry.cs:178`), so a quota refusal, if one comes, comes from
the API rather than from the binding, and is the recorded result, not grounds to re-run
(re-running until it passes would be DC-127 aimed at the exit evidence itself).

**A headless entry was proposed to substitute for the gesture. It was refused**, on reasoning
from the plan's own text rather than from preference:

| Branch | Why it fails |
| --- | --- |
| Headless entry stamps `main-menu.new-session` | Falsifies clause 1's meaning — *"origin set **only** on the `Ctrl+N` / `MainMenuBuilder` command path"* (`conductor-front-door.md:811-814`) — and makes the front-door proof harness-producible. **That is the "asserted-about" failure N7 was blocked for. Refused outright, not available for approval** |
| Headless entry stamps some other origin | Fails the oracle: `FRONT_DOOR_ORIGIN` is checked at line 112 against line 44, and clause 0 forbids widening it |
| Run anyway, report clause 1 partial | Clause 1 is one of the six that *"Fails if any of 1–6 is absent"* (`:831-832`). **A failed exit wearing a partial-pass headline**, not "not recorded, never zero" |

**Clause 5 says the same thing from another angle** (`:820-821`): *"no second entry point (Ruling
13), asserted by a ledger counting roots."* The headless entry would not have tripped that ledger
— it would have reached the one composition root — but the hands are the plan's **intended**
trigger rather than an accident of its wording.

**The control that would not have caught this.** `ExactlyOneSiteInSrcCanStampTheFrontDoorOrigin`
checks who *names* `SessionOrigins.MainMenuNewSession`. A headless entry driving
`NewSessionSheetViewModel.Create()` names nothing, so **the guard would have stayed green while
the claim it protects became false.** The allowlist stays at two entries and its comment now
carries Ruling 49's answer.

## If the gesture is not performed

**Then clause 1 is `NOT DEMONSTRATED`, the exit is `NOT MET`, and this pack carries no headline
that the front door works.** The slice stays open. That is the honest failure, and it is a
different thing from the third branch above: it claims nothing, where a partial-pass headline
would claim something false. **Nothing in this pack should be read as evidence the front door
works until a row below says a gesture happened.**

## Ruling

The F5 exit run is triggered by the operator's own `File → New Session` gesture. No headless
entry point is built. Clauses 2, 3, 5, 6 and 9 of the F5 Proof Pack read `RUN-PENDING` under this
ruling until that gesture is performed; that is the only missing input.

**Confidence:** Verified — both preconditions were opened and checked, not assumed; the three
refused branches were checked against the clause text and the oracle's own line numbers; the
guard's blind spot was traced to the specific method (`NewSessionSheetViewModel.Create()`) it
would not have seen.
