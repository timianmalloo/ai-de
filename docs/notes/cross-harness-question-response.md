---
id: note-cross-harness-question-response
title: Questions are typed responses, not acceptance
type: doc
status: accepted
owner: "@timianmalloo"
phase: P1
tags: [cross-harness, coordination, response]
links:
  - to: spec-cross-harness-coordination
    rel: relates-to
  - to: design-cross-harness-coordination
    rel: relates-to
review-by: 2026-10-17
summary: Clarifies the existing P0/P1 requirement that an explicit question is a valid response without accepting a proposal or granting execution. Preserves requester follow-up through the existing response projection.
---

# Decision

The user's 2026-09-17 clarification governs this bounded repair. The design's
disposition enumeration omits `question`; the specification distinguishes response
visibility from acceptance and says that ACK is not acceptance. Add `question` to
the existing closed response vocabulary, without extracting semantics from titles,
caller reasons, legacy resolution text, or payload prose.

A valid correlated question removes the initiating request's `unanswered` state.
It does not mean that the question is solved globally. Preserve its exact
`latest_disposition`, response recipient and payload; keep `remaining` true and
expose the question payload through the existing `next` field for requester
follow-up. This introduces no new row API. `changes-requested` and `deferred`
remain distinct existing dispositions, including the deferred checkpoint contract.

`proposal-accepted` remains a separate exact-revision, exact-hash,
authorized-required-peer protocol fact. A question, ACK title, legacy resolution,
receipt or consumption attestation grants no acceptance, execution, ownership,
run, transfer or start authority. Unknown kinds and dispositions still refuse;
full source-key, digest, correlation and generation guards do not change.

## Compatibility and boundary

This is an additive dormant-reader change, not a live writer rollout. Pre-live
enhanced readers that reject `question` must upgrade before consuming that value.
Legacy writer/list compatibility remains required. Production authority remains
DENY and the enhanced writer remains disabled.

The fixtures exercise synthetic consumption and correlation only. They do not
qualify actual peer triage, endpoint registration, harness delivery or live
consumption. Canonical specification/design and Proof Pack consolidation remain
with their existing author; this note and the committed audit are the repair
receipt, not a second Proof Pack or an independent approval.

## Failure class and control

**Class:** a closed response vocabulary mistakes a valid nonaccepting reply for
invalid input. **Sweep:** the response validator in `coord-core.py` owns the
enumeration; the protocol helper separately validates acceptance/consumption
facts, and the existing fold already projects typed dispositions.
**Derive:** retain that one validator and the existing projection rather than
adding a parser or a second semantic ledger. **Prevent:**
`test_Fold_QuestionUnderAckTitle_PreservesQuestionWithoutAcceptance` covers ACK
and opaque titles, legacy ACK resolution, visible requester follow-up and absent
rights. The `question-rejected` reverse mutant removes the new vocabulary value.
Execution results and file/interpreter pins belong to this run's audit entry.
