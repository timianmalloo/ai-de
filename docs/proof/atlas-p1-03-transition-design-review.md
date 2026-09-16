---
id: proof-atlas-p1-03-transition-design-review
title: "Atlas P1-03 transition design: delivered independent verdict and preparation boundary"
type: doc
status: completed
owner: "@timianmalloo"
tags: [proof, atlas, diagnostic-design, independent-review]
links:
  - { to: proof-atlas-p1-03, rel: relates-to }
  - { to: plan-atlas-five-gates, rel: relates-to }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-16
summary: "The watcher delivered independent B1-B3 document-design clearance. Runtime proof and exact preparation/execution grants remain separate; experimental Facts stay outside the canonical candidate."
---

# Delivered independent design verdict

**Verified delivery; scoped review judgment:** foreground watcher
`copilot-main-watch-b0d0` resolved request `req-01M2NT8TTYERHMRAFEYJC3Y6TC`
with the final verdict of retained independent Test Architect
`48835411-7dc1-4fd1-ad85-9a27b7277679`, acting in T2 Adversary mode.
Direct delivery `req-01M2NV3TGZ058CQE3W5RS1D7NM` carries the same verdict;
the Conductor read it and resolved that notice as consumed. This is a faithful
Conductor record of the delivered external review, not a new Codex review.

Reviewed input:

- Commit: `ebfe076125a1200345b806519b733104b3e06ea7`.
- Path: `docs/investigations/atlas-p1-03-uia.md`.
- Blob: `3b6c8153018d1199e669cd519ba1c0bc65547a3d`.
- Tree: `C:/Projects/ai-de-investigation-atlas-p1-03-uia`.
- Root independently inspected the completed design delta and verified no
  `src/`, `tests/` or `tools/` change against `5406ea69`.

**Verdict: PASS-WITH-CONDITIONS; B1-B3 document-design veto cleared only.**

| Finding | Delivered clearance | Conditions retained |
| --- | --- | --- |
| B1 publication | Synchronous Content-to-status subscription; exactly one Background dispatcher tail verifies the full published state and disarms. The source ordering and referenced WPF notification contracts were reviewed. | Measure installed-runtime ordering, cancellation and teardown in actual controls. The noncapturing class registration is process-scoped; clear its scoped registry and never claim registration removal. |
| B2 identity/claim | Treatment association only. `uia_wpf_owner=not-recorded` is an interpretation limit. | No retained-owner, canonical-reproduction or P1-03 causal conclusion follows from the pair. |
| B3 custody | Serialized single handoff; one retained disposal task; failed pre-handoff disposal retains custody; explicit recovery; owner-only post-handoff disposal. | Prove all admitted cancellation races, reachability after failed release, original-error preservation and no competing disposal. |

The reviewer reported eight calls and no edits/tests. The original request specified
four calls/eight minutes; the delivered verdict supplies no revised call budget, so
this receipt makes no budget-compliance claim. Actual runtime controls, the two-arm
experiment and causal attribution remain unexecuted/unestablished. The earlier
qualification BLOCK `876acbfa` remains effective.

# Owner-confirmed preparation manifest

Goal: produce independently reviewable test-only diagnostic preparation.
Done when the exact design is implemented, meaningful headless controls have observed
red and green results, installed runtime identities and selected tests are recorded,
and an independent implementation reviewer clears all triggered conditions.
Not in scope: product changes, shown windows, live UIA, the paired experiment,
canonical qualification, automatic integration or main publication. Tier T2;
programme width at most four including Astra Owner and Conductor.

The Owner confirmed one Astra author, 24 calls/35 minutes/checkpoint 12, followed by
independent Test/SRE review, 12 calls/20 minutes/checkpoint 8. Astra is retained for
the WPF ordering and lease-custody semantics. Source authoring starts only after the
watcher grants the exact manifest in `req-01M2NWGFJG89QF0KFBA9W8YGF6`.

Prepared through the official worktree lifecycle:

- Tree `C:/Projects/ai-de-test-atlas-p1-03-uia-transition`.
- Branch `test/atlas-p1-03-uia-transition`.
- Session `codex-atlas-p1-03-transition-author`.
- Base `ebfe076125a1200345b806519b733104b3e06ea7`.
- Exact authored paths: existing
  `tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs`,
  new `docs/proof/atlas-p1-03-uia-transition.md`, and status/evidence updates to the
  existing investigation. Official own audit/change and required derived records
  follow the repository mechanisms and exact short leases.

No production, project, shared STA, runner, Shell or wiring path is assigned.
The canonical native proof's selectors, returned results, assertions, waits and
cleanup must remain unchanged. Use the actual published observation contract,
one transfer of lease custody, and the same code path in both arms except their
declared loading-phase traversal. No peer refresh, alternate root, retry, sleep
or guessed readiness is permitted.

Preparation requests normal same-tree Debug compilation and only this test filter:

```text
FullyQualifiedName~AiDe.App.Tests.AtlasDaemonMainWindowProofTests.TransitionControl_|FullyQualifiedName~AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeObserver_NonGui_
```

Inspect `--list-tests` and actual TRX names/counts/outcomes. Preserve the existing
21 non-GUI observer cases; derive the new count from the actual authored inventory.
Do not select the entire native class. Unshown controls use zero HWND and no live
UIA, Show, focus, captures or desktop input. They cannot establish successful
visible/Loaded publication; that remains a later shown-execution obligation.

# Experimental roster isolation

The Conductor identified that ordinary full-App discovery would also select the
two proposed `NativeTransition_` Facts. Their design requires separate fresh
processes and distinct receipt labels. The Owner therefore explicitly confirmed:

- Experimental Facts and supporting code remain in the isolated diagnostic branch.
- Do not merge that branch wholesale into the canonical/main candidate.
- Canonical native source remains reviewed `626d16a2`.
- Do not introduce implicit skips, runtime opt-in behavior, new projects or altered
  canonical filters to hide the additional roster.
- After the experiment, return the scaffolding's disposition to Owner.
- Proof/audit transport may use the existing exact-manifest mechanism without
  importing executable source.

Watcher addendum `req-01M2NWK8V83BCR3E4DBJ4HFGA9` records this narrower boundary.
It adds no demand for another review of the unchanged B1-B3 document.

# Execution graph and exit conditions

Document review is complete. Exact preparation grant -> isolated authoring and
red/green controls -> frozen source/Proof Pack -> independent implementation review
-> fresh checked slot on exact binaries -> two planned fresh-process arms ->
independent interpretation are the remaining serial dependencies. The r5 peer
freeze has no dependency on these native stages. Do not widen authoring while its
contract or ownership remains unresolved.

The variant is the set of unsatisfied evidence predicates, not retries or elapsed
time. A failed assumption returns to Owner; a refused lease requires coordination;
neither timeout nor silence is approval. No new shown execution is authorized here.

| Completed | Remaining | Best next action |
| --- | --- | --- |
| Delivered design clearance, Owner-settled manifest and isolated tree | Exact preparation grant, implementation controls/review, later execution grant | Consume the watcher's manifest disposition and dispatch the bounded author only when granted |
