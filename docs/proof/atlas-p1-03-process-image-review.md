---
id: proof-atlas-p1-03-process-image-review
title: "Independent review of process-image diagnostic-loss investigation"
type: proof-pack
status: review
owner: "@timianmalloo"
tags: [atlas, proof, forensicreview, process-identity]
links:
  - { to: proof-atlas-p1-03-pair-preparation, rel: depends-on }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "CLEAR for investigation interpretation and diagnostic-only proposal suitability. Four raw fixtures distinguish lost evidence; historical cause and any maintained repair remain unproved."
---

# CLEAR for investigation interpretation and proposal suitability

**Verified:** the four-fixture evidence supports a diagnostic-loss correction
proposal. It does not identify the historical missing eighth process or explain
the original UIA defect. No maintained repair exists in this review; CLEAR cannot
approve its implementation, runner operability, native retry, or publication.

Goal: independently assess the four-fixture investigation and its diagnostic-only
proposal. Done when evidence limits, Test/SRE/Simplifier verdicts, and next-proposal
suitability are committed. Tier T2; Astra; fan-out zero; six calls/ten minutes,
checkpoint four. No tests, fixture reruns, runner imports, native launches or source
edits. Surface list: scratch native observations -> maintained refusal/finalization
-> raw case/summary/pin records -> investigation claims -> this review -> Owner.
Data-model and UI changes are N/A.

Review tree: `C:/Projects/ai-de-review-atlas-p1-03-process-image-findings`, branch
`review/atlas-p1-03-process-image-findings`, base
`5cdd01b868225b3c92a7c4b4fac368d4c62b78ce`. Repository/persona/workflow guidance was
already grounded; instruction diff from the preceding review base is empty. Own
audit start and liveness were published on the first batch.

## Actual source and raw evidence

Independently read the complete scratch `spike.py`, `summary.json`, all four case
results, containment process/direct-child records, before/after pins, and evidence
hash manifest under
`C:/Projects/ai-de-investigate-atlas-p1-03-process-image/artifacts/atlas-process-image-investigation/`.
All nine listed hashes were independently recomputed and matched:

| File | SHA256 |
| --- | --- |
| `spike.py` | `ccb27ba1f09ac0796bfbbc4769f72bf71d2d3809bb229ea3ef702933c78eecd8` |
| `summary.json` | `306fe6af414f4b52012ff70e08223b35bc7c50b4fa22d02ba0269c7d11cf6f50` |
| `before-pins.json` | `d18e7842536d2fd66243c977960ce3a44debb525aecd29e9ef9dc2808b3b2457` |
| `after-pins.json` | `7f9e6d9ffb416c9a7ede04367f1c8a84e5b8522a0fc58ad108f3a8e839aab527` |
| `live/result.json` | `3a9c7318aa3914ea3eb20d72c06e335f6eedcaed1707cb96619af9df6396e22b` |
| `exit/result.json` | `78dbd59bf76ca1abaef43678d76264db025723ce2171dcd224f52e01d1e338c3` |
| `inject/result.json` | `9a177c779caa1096fdc8e5eb3cb3859452ccea5da94ddeaf573ed05b7ef6de95` |
| `containment/result.json` | `c08a5a63f77da1b8320861d8664935a90675921df2f409d440a18f6e23722d17` |
| `containment/process.json` | `dc173252f2ab2264158e214418fa887f5e597451dcb78dc8084211953b5f4442` |

Raw evidence is retained locally; these files do not travel with this receipt.
Four direct case query arrays match their summary arrays. There are five query
rows across four cases: one per first three cases and two in containment. This
does not mean five fixtures or an executed `owned_snapshot` case.

| Case | Verified observation | Evidence boundary |
| --- | --- | --- |
| Live | PID 7864/birth 134340698653414844; wait 258 before/after; image success; sample returned | Establishes this successful controlled call, not general operability |
| Synchronized exit | PID 35640/birth 134340698653905578; wait 0 after explicit exit synchronization and before query; error 31; after wait 0; bare `PROCESS-IMAGE-MISSING` | Demonstrates one exit-before-query mechanism; no universal error-code rule |
| Live zero-capacity injection | PID 28488/birth 134340698654327026; waits 258; real query returns error 122; same bare refusal | Disconfirms the message as unique evidence of exit; capacity was deliberately changed |
| Actual `run_owned` finalization | Gate 21772; child 5024/birth 134340698654935019; second query returns error 122; primary execution refusal preserved; forced true; total 2/active 0; retained handles exited | `identities_complete=true` because direct-child record supplies the second identity; historical 7/8 gap is not reproduced |

Every case has zero failed proxy-observed closes and zero remaining proxy-tracked
handles. The first three cases record child exit 0. This is not a global handle
census. All query rows retain matching before/after identity; all post-observation
ticks follow query-end ticks. Later exit state is never evidence of earlier state.

**Verified source:** scratch lines 54-59 capture the ctypes saved native error
immediately after the failed real image call, before timestamps and later API calls.
The runner loads WinDLL with `use_last_error=True` (line 110). This matches
[Python's saved-error contract](https://docs.python.org/3.12/library/ctypes.html#ctypes.get_last_error)
and [Microsoft's immediate retrieval requirement](https://learn.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-getlasterror).
Wait 0/258 meanings match the
[documented wait results](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject).
No documentation establishes a universal exit-to-error-31 mapping.

**Verified:** both pin records report 11,630 files/1,030 roots matching before and
after. Scratch `pins()` calls `verify_inventory`; checked runner lines 39-55 compare
the exact population and every digest. It also checks runner/current-manifest
digests. This is observed inventory-check evidence, not an independently rerun
inventory or a runtime resolver/native qualification. Reviewer source diff against
`d679e1567e2d74fa2ef85f1eddae6c44b6d5b758` is empty for `src`, `tests`, `tools` and
runner. Runner SHA256 matches
`26567e7b408430ef29d4a44e6c60a1792cb2ddae9b23dd01393e1cc6ea3a3daa`.

## Findings and bounded proposal conditions

**FR-PI-001 [Minor] (Verified), interpretation precision:** fixture 4 calls
`run_owned`, not the pair's later `verify_process_result`. It records the primary
refusal and containment. The source predicate at runner lines 490-492 rejects its
forced/error state; that is a checked predicate, not an executed pair-acceptance
test. Do not report a fifth verification or a complete native pair from this case.
Disconfirmation: inspect scratch call sites for an actual verifier invocation;
none exists. No investigation rerun is needed to retain the current claim boundary.

**FR-PI-002 [Minor] (Inferred), future control risk:** the scratch proxy stores the
real native error, then makes post-observation API calls before returning false to
the maintained caller (`spike.py` lines 57-60). Those later calls can affect the
ctypes saved error under the documented API contract. The raw rows preserve the
already-captured value; they remain evidence here. No observed overwrite is claimed.
However, simply reusing this proxy is not proof that a future repaired caller reads
the immediate native error. A future control must preserve or deliberately verify
the error across its injection boundary and assert the maintained serialized output,
not only the proxy's rows. Disconfirming control: intentionally clobber a later
observation error and prove the maintained record still contains the original query
error. This is a proposed control, not an executed test or repair approval.

The current evidence is enough for Owner consideration of a **diagnostic-only**
proposal: retain pending PID/raw birth/membership, native query result and immediate
error, separately timed exit-state/observation failure, and durable primary diagnostics.
Keep existing forced-failure, containment, complete-population, and no-retry gates.
Do not promote a diagnostic-only image-less observation into an accepted identity.
Keep secondary diagnostic failures separate from the original failure.

The source-confirmed sibling at `owned_snapshot` line 273 discards image-query error
too. It has an input identity row and a different correlation consumer. Extending
a later admitted repair to it requires a direct sibling-path control; the four
current cases do not execute or verify it. No fifth fixture is retroactively claimed.
Independent review of maintained changes and any new freeze/slot remain future gates.

Class -> sweep -> derive -> prevent: the supported class is loss of native diagnostic
context, not an established historical exit race. Sweep covers both image-query
sites and failure serialization. Derive diagnostics separately from identity
acceptance. Prevention is a later admitted durable-output control that fails against
the current bare refusal, preserves immediate-error ordering and current refusal/
closure behavior. No central register entry or maintained control was authored here.

## Independent adversarial verdicts

PERSONA: test-architect   MODE: Adversary   TIER: T2

VERDICT: PASS

FINDINGS: FR-PI-001 [Minor] (Verified) and FR-PI-002 [Minor] (Inferred) constrain
future claims and controls; neither contradicts the inspected raw investigation.
No bounded blocking counterexample to diagnostic-loss proposal suitability found.

CLEARS-THE-VETO: yes for this documentary review; raw Proof Pack and disconfirmation
checks attached. Maintained red/green repair proof remains future, not waived.

RESIDUAL RISK: synthetic scheduling and injection cannot establish historical cause;
there is no maintained regression control or sibling-path execution here.

PERSONA: sre-diagnostician   MODE: Adversary   TIER: T2

VERDICT: PASS

FINDINGS: immediate native evidence and separately timed lifecycle observations
support diagnostic retention. FR-PI-002 must inform any repaired-output control.

CLEARS-THE-VETO: n/a (advisory); no operational readiness clearance.

RESIDUAL RISK: proxy closure has finite coverage; historical eighth identity and
native UIA failure remain unknown; error-code meaning alone is not root cause.

PERSONA: the-simplifier   MODE: Adversary   TIER: T2

VERDICT: PASS

FINDINGS: diagnostic retention is the smallest supported next proposal. No retry,
guard relaxation, broad process redesign, or additional native experiment is needed
to establish the evidence-loss finding.

CLEARS-THE-VETO: yes; scope remains failure diagnostics and their required controls.

RESIDUAL RISK: a sibling-path implementation adds a distinct verification obligation.

## Programme and closure boundaries

Read Conductor graph at `7b8d7508:docs/proof/atlas-p1-03-transition-design-review.md`:
I -> D -> N -> J -> O is serial; J is this six-call/ten-minute review. Width remains
one executing branch plus Conductor/Owner, cap four. Its finite variant is at most
four unclassified cases followed by unreturned review/decision receipts; no repeat
sampling until green, no retry, and preservation/audit/inspection floors remain.
This review returns J. Owner O chooses next; no duplicate programme plan was authored.

Checkpoint four reported evidence complete and two batches reserved for documentary
closure. Audit records measured skill duration; final handoff records actual calls,
final clean commit and release. Token/spend telemetry is unavailable. No AIDE contract
environment exists, so no harness event was fabricated. Retain tree/raw pointers for
Conductor inspection. Official regeneration, graph validation/snapshot and source
preservation are documentary checks, not native execution.

| Completed | Remaining | Best next action |
| --- | --- | --- |
| Independent four-case/source/hash review; diagnostic-only proposal suitable | Owner disposition; maintained repair and independent review; historical UIA qualification | Owner selects exact diagnostic-only scope and its failure-preserving controls |
