---
id: investigation-code-atlas-publication-lifetime
title: "Code Atlas publication lifetime - unresolved expiry boundary"
type: doc
status: draft
owner: "@timianmalloo"
tags: [code-atlas, investigation, ipc, authority, lifetime]
links:
  - { to: spec-addendum-e-code-atlas, rel: implements }
  - { to: proof-code-atlas-production-adapters, rel: relates-to }
  - { to: note-atlas-live-reader-horizon, rel: depends-on }
review-by: 2026-12-14
summary: >-
  Source readback shows operation/native membership disposal before the response write.
  Expiry-versus-stalled-publication behavior has not yet been executed. Runtime admission
  remains blocked while a controlled counterexample and repair disposition are prepared.
---

# Publication ownership is unresolved

**Candidate:** `b1c6f74a0c5b0c7775420ac63ad814aa05c9bc0c`, not joined.
**Status:** source-level lifetime gap observed; no unauthorized disclosure demonstrated.
This is a provisional investigation, not a verified necessary-and-sufficient root-cause
claim and not permission to weaken the publication contract.

## Required behavior and scope

Owner turn 50 requires native membership pins to cover bounded capture/read/publication
critical sections, but not UI idle time. Revocation/expiry must win its publication gate;
cleanup retains ownership until the work using it completes or is canceled and drained.
The earlier DS gate covers one pipe reader and the canceled-and-awaited EOF monitor.
Those are distinct from synchronization between scope expiry and an already-started writer.

The bounded surface list is operation preparation -> held publication -> final authority
check -> response write -> native/work-reservation release, with expiry/revocation and
connection shutdown entering from the side. No private corpus, primary checkout or
generic-client rewrite is part of this investigation.

## Observed source timeline

All source receipts below were opened by Conductor in the frozen runtime tree.

| Step | Source | Observed behavior |
|---|---|---|
| Prepare | `AtlasReadScopeIssuer.cs:505-506` | `CompleteWork` signals preparation finished |
| Scope stop | `AtlasReadScopeIssuer.cs:472-488` | Invalidates, awaits `WorkFinished`, then disposes the active operation |
| Expiry | `AtlasReadScopeIssuer.cs:344-360` | Invalidates expired scope and enters the same connection/stop path |
| Commit check | `AtlasReadScopeIssuer.cs:515-523` | Checks currentness/cancellation while holding the scope gate; lock ends on return |
| Commit and dispose | `DaemonEndpoint.cs:194-217` | Validates, commits, disposes operation and removes held publication before returning response |
| Resource release | `AtlasReadScopeIssuer.cs:524-545` | Disposes membership, clears active operation, releases serialization/work and disposes linked cancellation source |
| Actual write | `IpcServer.cs:333-405` | Calls `CommitResponseAsync`, then writes using `connectionCancellation` |

The EOF monitor is canceled and awaited before writing. That prevents a competing reader;
it does not by itself prove that scope invalidation cancels the writer or that native pins
remain held until that writer finishes.

## Hypotheses and discriminating evidence

| Hypothesis | Evidence so far | What would decide it |
|---|---|---|
| The endpoint or writer already holds publication resources through completion | The opened endpoint disposes them before returning to the writer; caller supplies the connection token | Show a different actual owner/control path, or the controlled write test |
| Commit is a sufficient linearization point | Could explain accepting a point-in-time response; does not establish the stronger Owner-50 lifetime requirement | Explicit contract decision plus proof of the accepted behavior; do not reinterpret the rule silently |
| Expiry can release pins while publication remains pending | Source stop waits only for preparation; writer is later | Pause the actual publication, expire/revoke, inspect ownership, then resume |

**Confidence:** disposal-before-write is Verified by source. The unsafe interleaving and
its externally visible outcome remain Inferred. A green aggregate suite does not answer
that missing interleaving.

## Proposed controlled counterexample

Use a real admitted native-Q response and a deterministic publication barrier, not a long
sleep. Prepare the response, pass the commit boundary, pause the writer, then expire or
revoke the scope. Inspect native pins, active/work reservations and writer ownership.
Attempt the formerly excluded Git/admin mutation, then release the writer.

The oracle distinguishes:

- Resources remain owned until publication finishes, or cancellation wins and the writer
  is awaited before resources release.
- Resources are released while the writer can still complete a successful response.

Also cover expiry before commit, ordinary successful write, write failure/cancellation,
connection shutdown and retained native-cleanup failure. A fake client echo or an
epoch-before-commit test cannot substitute for this publisher interleaving.

## Proposed phases, pending Owner approval

| Phase | Scope | Exit evidence |
|---|---|---|
| 1 | Deterministic test/control in the existing Atlas IPC/production test files; minimal server seam only if needed | Literal ordering/resource/writer observations on the unfixed candidate |
| 2 | Only if the gap is reproduced: one publication owner retained through bounded write or canceled-and-awaited write | Original red becomes green; no early pin/work release, self-await deadlock or unsafe retained cleanup |
| 3 | Targeted Security/DS/Test review, then replay Core/IPC and App coverage | Current-pin clearance; no historical spike or unrelated cleanup verdict substituted |

No implementation of the proposed repair has begun. The user's standing delegation makes
the separate Owner the decision authority; the diagnosis and phases are sent there rather
than being self-approved by the code author.

## Failure-class sweep and limits

Proposed class: preparation completion mistaken for whole-operation completion.
The inspected sibling entry paths are explicit release, expiry, connection end and issuer
shutdown, all converging on scope stop; endpoint commit and discard both dispose the same
operation. These are shared structural candidates, not separately reproduced failures.
The bounded Atlas/endpoint/server marker search found no `assume:` or `simplify:` marker.

A verified instance and its failing-first control must be added to the defect register
after the counterexample; this draft does not invent the root cause to complete a template.
Rollback is the isolated candidate branch; no main/push or production deployment occurred.

## Executed counterexample and approved repair entry

The provisional assessment above is superseded only to the extent of these new observations.
C added an internal stream-wrapper seam and eight native-Q cases without changing production
ownership/cancellation. The original code remains `b1c6f74a` plus that uncommitted seam/test
diff. `owner53-publication-observed.trx` records six failures and two passing controls.
Conductor preserved the raw author directory and independently repeated the same result:
`files/atlas-publication-independent/publication-red-independent.trx`, exit one, total eight,
passed two, failed six.

| Stimulus | Observed ownership/cancellation | Actual frame outcome |
|---|---|---|
| Pause before first byte | Active work zero; native owners zero | Complete success, demonstrating resources released before writer completion |
| Revoke or explicit expiry event | Scope cleanup returns with scope/active/native charges zero; writer cancellation false | Native `IndexedMatch` success body 1,693 bytes; wire 1,697 bytes |
| Cancel actual operation deadline source | Operation cancellation true; writer cancellation false | Same complete native success |
| Revoke after four-byte prefix | Prefix accepted; cleanup returns and writer remains uncanceled | Remaining body completes successfully |
| Repeat cleanup | No retained publication owner blocks cleanup | Success still completes after revocation |
| Disconnect control | Writer fails with `IOException` | No complete response |
| Complete write before revocation | Completion ordered first | Existing completed response remains completed |

Raw `revoke.json`, `partial.json` and `deadline.json` values were read directly. Expiry is
an explicit real connection-ended event, and deadline uses cancellation of the actual
operation deadline source. This is not a claim that a five-minute clock elapsed, that a
kernel stalled, or that already-transmitted bytes can be recalled.

The source mechanism now has a controlled, repeated failure observation: ownership and
scope-linked cancellation end before the writer they must govern. The removal/correction
half of the necessary-and-sufficient test remains for the repair's red-to-green replay.
No broader disclosure classification is asserted.

Owner turn 53 conditionally approved the described repair after parent red/plan readback.
That gate is met; C's remaining twelve leaves are released. They cover only the stated
five files and preserve the exact paused-write oracle. The independent Security/DS/Test
gates and final runtime admission remain open.
