---
id: proof-atlas-p1-03-pair-review
title: "Independent Atlas pair runner review: blocked"
type: doc
status: review
owner: "@timianmalloo"
tags: [atlas, proof, review, containment]
links:
  - { to: proof-atlas-p1-03-pair-preparation, rel: depends-on }
  - { to: proof-atlas-p1-03-transition-review, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "Independent BLOCK: exact CIM creation-time correlation loses precision and an injected identity decode failure skips handle closure and process evidence persistence. Ten existing controls pass independently."
---

# Disposition: BLOCK

Goal: independently assess the frozen task-local runner, complete input freeze, and
Owner profile amendment. Done when exact evidence supports CLEAR or BLOCK and a
committed receipt, official audit and lease release exist. Not in scope: runner or
test correction, builds, dotnet test, shown windows, UIA, desktop input, execution
grant, canonical integration or publication. T2, fan-out zero, independent Astra.
Budget: eight orchestration batches / fifteen minutes. The admitted programme graph
is reused; this review does not author a new programme goal or repeat optimization.

Reviewed candidate: `88035753581dd0bab5697e975b6ba1a53543e702`.
Runner SHA256: `3e816170418907b5b6d624b6e1f6d29950e9e55754284bc1deb3b1356a3f487c`.
Manifest SHA256: `ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314`.
Both hashes were independently read. The manifest contains 11,625 files / 1,025
roots. This review did not repeat the full inventory hash verification or builds;
the Conductor's PINS-MATCH and author's build claims are separate evidence.

Surface list: process launch -> job custody/deadline -> immutable identity ->
profile/runtime evidence -> receipt/TRX reader -> diagnostic validity -> proof/audit.
The diagnostic facts remain experimental. No inference about canonical failure or
treatment causality follows from this review.

## Findings and falsifiable clearance

### FR-PAIR-001 — Blocker, Verified: creation identity precision mismatch

`run_pair.py:254` BrowserObserver.run converts CIM CreationDate through ToFileTimeUtc;
verify_browser_use at line295 requires exact equality with raw GetProcessTimes creation ticks.
A harmless reviewer Python process produced PID33900, raw134340662446557014 and
CIM134340662446557010. The difference is four100ns units. No browser was launched.
Thus a process whose raw identity has a nonzero final digit can be refused even
when it is the same process. This defeats the amended runtime/profile provenance
gate unpredictably. The parser must not simply discard creation identity.

Disconfirming check: a second process PID33892 produced raw and CIM
134340663391451330. Exact runner verify_browser_use accepted that row. The defect
is precision-dependent, not a claim that every CIM comparison fails. The first
measurement is retained in initial-creation-probe.json and this receipt; the second
is in probe.json. The values were observed directly, not inferred from documentation.

Clears when the author demonstrates exact owned-handle creation correlation across
both matching and nonmatching CIM precision cases, including PID reuse/mismatch
refusal, while retaining actual runtime path and actual profile consumption evidence.
Owner/Conductor assigns the correction; this reviewer made none.

### FR-PAIR-002 — Major, Verified: failure in finalization loses its own evidence

`run_pair.py:227` run_owned finalization decodes direct-child.json before job.close at234,
process wait, BrowserObserver.close and process.json persistence. An independent
fault injection raised JSONDecodeError at that actual decode boundary after a
harmless normal subprocess completed. Observed: exception propagated,
process.json absent, Job handle still open and two retained process handles still
open. The reviewer explicitly closed those handles after recording their state.
The injected error is a controlled malformed-record analogue, not a claim that the
unmodified atomic writer normally corrupts JSON. The existing controls do not
exercise this finalization failure. Reliable failure evidence and release are
required by the runner contract even when a record cannot be read.

Disconfirming check: all ten existing controls, including normal streams and owned
timeout containment, passed before injection. Normal-path success does not cover
the failing finalization path. Clears when actual malformed/missing/unreadable
identity-record controls retain a process failure record, close every owned handle,
drain/stop the observer and prove containment without masking the primary failure.
The outer execute finally retains END/RELEASE for exceptions inside its try; that
does not restore the missing inner process record or close its leaked handles.

### FR-PAIR-003 — Major risk, Flagged: Git closure is not demonstrated

The manifest pins `C:/Program Files/Git/cmd/git.exe` (freeze_roots at345). The installed
`C:/Program Files/Git/mingw64/bin/git.exe` exists and is absent from its file map.
freeze_roots adds only the selected Git file, whereas the native fixture invokes
Git to create the synthetic input and report source identity. This is a specific
load-bearing dependency boundary, not a demand to freeze the whole operating system.
This review did not measure the cmd executable's downstream image/module loading,
so runtime use of the unpinned binary remains Flagged, not Verified. Acceptance:
measure the harmless Git invocation's executable/dependency resolution, then pin
the consumed mutable installation inputs or demonstrate they were already covered.

## Independent controls and receipt review

The exact candidate was imported with -B. Only its in-memory ROOT and ARTIFACTS
were redirected into the reviewer tree; frozen source and author artifacts were
not changed. All ten original controls ran and passed: added/changed/missing pins,
loading ancestry positive/cycle/unrelated negative, fresh distinct owned profiles,
environment delivery versus consumption, expired-slot refusal, normal streams,
missing identity/forced cleanup refusal, and owned timeout with unrelated sentinel.
Raw results and retained streams are in reviewer `artifacts/atlas-pair-review/`.
The timeout control reports three identities, active zero, sampled handles exited,
and sentinel survival. The additional fault probe is in probe.py and probe.json.

Verified source comparison of emitted stages to reader: identity, held, pre-oracle,
release, cleanup, daemon-start/reaped, reader-disposed, lease-release-returned and
census names/attributes correspond to the native source. B requires actual loading
placeholder ancestry under Code Atlas TabItem and a held-to-release timing interval;
unrelated text and merely calling a census do not establish treatment. A and B are
fixed Facts in separate fresh dotnet processes with Debug/no-build/no-restore and
unique labels/TRX directories. Forced process/daemon cleanup, incomplete identities,
pins changes and missing browser evidence refuse. The original source remains fixed.

Negative classification has an additional unclosed test gap: validate_receipt uses
a stack substring ObserveAutomationAsync plus one primary failure, rather than
checking the exact assertion/type and observed query failure shape. The original
method also includes UIA property access and IsOffscreen assertions. Broad native
provider failures must not silently become a named-control absence verdict. This
review has no complete parser-mutation matrix and makes no all-schema clearance.
Author correction must add actual emitted-schema positive/negative fixtures and
mutations for non-oracle/provider/observer errors, duplicate/order/correlation
records, and cleanup. These are a bounded remaining review unit, not new product work.

## Adversarial council and graph

- **Test Architect — BLOCK, Blocker.** FR-PAIR-001 violates the promised provenance
  gate. FR-PAIR-002 and the named parser-control gap prevent full evidence clearance.
  Clearance requires observed red-to-green controls and independent review of the
  frozen correction; author or a green exit cannot self-clear.
- **SRE — Major findings escalated to the Test gate.** FR-PAIR-002 leaves failure
  diagnosis and resource closure unproved. FR-PAIR-003 needs a measured dependency
  boundary. No safe execution handoff exists for this exact candidate.
- **Simplifier — PASS for bounded structure, no independent complexity veto.** A
  task-local stdlib runner, exact two-arm sequence and existing Job Object pattern
  have explicit correctness purposes. This does not clear Test/SRE findings.

The actual final profile replan at root ff2fb242 was read. F->C/M, P->M/R,
C+P+M->R->S->A->B->I preserves the independent gate and fresh execution authority.
The A->B fixture/cleanup predicate and fixed arm count/deadlines are explicit;
caps signal failure, not success. Profile state is outside immutable roots, with
no existing-profile deletion or exclusions. No material graph defect was found.
The failed R node now returns exact correction predicates before S; no execution
request or additional preparation approval is introduced. The absent named graph
knowledge corpus remains the Conductor's reported grounding limitation.

## Control derivation, limits and administrative closure

Class -> sweep -> derive -> prevent: precision-changing adapters cannot establish
exact identity; sweep every comparison between CIM and native time; derive raw
identity proof independent of lossy conversion; prevent with both precision cases
and wrong-process controls. Finalizers must survive evidence parsing failures;
sweep constructor/sample/read/write/observer-close boundaries; derive independent
resource closure and best-effort retained failure evidence; prevent with injected
failure controls. These are correction requirements for Conductor routing, not
claims of completed central lesson-register changes.

Early combined reads exceeded tool output limits. Load-bearing runner, native arm,
cleanup and persona sections were recovered with narrowed reads; full untruncated
repository-wide grounding is not claimed. Liveness was published in the second
batch after the required first audit-start batch. No applicable nested AGENTS.md
was found by scoped discovery. No AIDE_CONTRACT_LOG variable was present, so no
episode destination or completion event was invented.

Completed: independent BLOCK and retained harmless evidence. Remaining: author
corrections, exact parser/dependency review unit, independent re-review, then a
separate fresh watcher execution slot. Next: Conductor/Owner disposition of these
bounded findings. Worktree and raw artifacts remain available; no join or push.
The original eight-batch budget fired before administrative closure; those batches
contained ten nested tool invocations (eight exec_command and two apply_patch).
The checkpoint mistakenly reported thirteen; that count is corrected here from
the actual invocation sequence. Broad-read truncation and the necessary
narrowed recovery consumed the extra discovery work. The Conductor explicitly
authorized two additional administrative-only batches for audit/regeneration,
commit/readback and release. The original budget remains eight; actual planned
closure is ten batches, an overrun, not retrospective budget compliance. No extra
probes or source changes follow the cap. Zero delegates. Final transport reports
actual closure state and any remaining administrative failure.
