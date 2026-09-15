---
id: proof-atlas-views-plan-review
title: "Independent Atlas E1/E2 programme graph review"
type: proof-pack
status: accepted
owner: "codex-sol-plan-review"
tags: [atlas, plan-review, execution-graph, coordination]
links:
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: >-
  Clears the two bounded Atlas design-grounding lanes while retaining the
  implementation barrier and requiring explicit evidence nodes before product work.
---

# Independent Atlas E1/E2 programme graph review

## Review boundary and evidence

This review covers only the frozen programme graph and design-lane admission. It does
not review either unfinished lane design and does not grant implementation, native runs,
integration or publication.

Reviewed inputs:

- Conductor plan and coordination record at
  `45aac3e3e6ae57dbb46299f8da015a96ce3744d1`.
- Owner decision at `8c9fa47cc19574f43f7c206251e19af67553b8be`.
- Shared transfer request `req-01M2KA6YK6SJBRF4MF27H0J8XE`, resolved as accepted
  for bounded design grounding.
- Core/Design request `req-01M2KA74EQYXWCZB5GMRNXNWN1`, resolved with the exact
  per-lane grant process and shared-file restrictions.
- GHCP foundation request `req-01M2KA74D0TGMKV5G0XW1VBHDY`, still open at review.

All claims below are **Verified** against those records unless marked **Inferred**.
The referenced `kb-graph-and-loop-engineering` node and evidence directory are absent;
the plan records that gap and does not promote it to evidence.

## Admission verdict

**PASS: bounded E1 and E2 design/contract grounding may continue in the two existing,
separate worktrees. BLOCK: no product implementation is admitted.** The graph already
holds implementation behind accepted-foundation, exact-grant and independent-design
barriers. Those barriers remain load-bearing.

| Lens | Verdict | Evidence and falsifiable clearance |
|---|---|---|
| Test Architect (hard veto) | **PASS design grounding; BLOCK implementation** | The graph maps required test families and independent reviews, but Proof Pack/evidence production is folded into implementation exits and the join. Before implementation, add one explicit evidence node per lane between `E*-I` and `R-I`, naming the committed proof artifact, evidence inputs and an oracle that fails on missing, unexecuted or contradictory results. Review consumes those frozen artifacts; an author's summary cannot satisfy the node. |
| Simplifier | **PASS** | Two lanes match distinct behavior and architecture grains and own separate design artifacts. Premature horizontal model/store/wire/UI tracks are struck, and native/integration resources remain serialized. The explicit evidence nodes requested above carry an independent gate and therefore earn their boundary. `Lean already. Ship the design grounding.` |
| SRE | **PASS design grounding** | The cost model is labeled **Inferred** and its arithmetic is consistent: `T1=23`, `T∞=15`, two-lane Brent bound `19`, ceiling `8` normalized units. No speedup is claimed. Retry, containment, circuit-breaker semantics, context ceiling and exclusive desktop serialization are stated. Implementation remains held until each lane names a fixture and observable duration/count/omission/cancellation/error oracle. |
| Orchestrator | **PASS design grounding; BLOCK implementation** | The cap of four includes Owner and Conductor and leaves two author slots. The lanes have distinct trees, artifacts, exits and budgets. Core's exact-grant protocol is resolved, while the accepted foundation and exact code allowlists remain deliberately unresolved at `C`. Clear by recording an accepted foundation SHA, a jointly satisfiable per-lane path/signature guard table, section-2 grants for each new Surface/View, and independent design verdicts on frozen revisions. |

## Graph checks

- **Nodes and floors:** Grounding, Owner decision, distinct lane designs, shared-contract
  convergence, independent design review, implementation, independent implementation
  review, serialized join and publication handoff are present. The missing explicit
  evidence nodes are the only graph-structure blocker found.
- **Authorization barrier:** `C` and `R-D` prevent product authoring from consuming a
  moving candidate or implied ownership. Silence and a candidate pin do not grant a path.
- **Shared seam width:** The current design-only paths are disjoint. Product-path guards
  are intentionally pending. At `C`, the guard must name each path and public signature;
  any recursive scan must state root, recursion, token set and allowlist. The E1, E2,
  GHCP/Core and Grok clauses must be jointly satisfiable before either implementation lane
  opens.
- **Artifact contracts and independent review:** The design outputs are named as
  `docs/design/atlas-behavior-views.md` and
  `docs/design/atlas-architecture-views.md`. A separate reviewer owns dedicated review
  proof; neither lane clears its own veto.
- **Scope truth:** Sequence, Activity, domain, layer/component and Azure are the five
  admitted views. ER stays explicit remainder. Five-view completion cannot be reported as
  completion of all Addendum E E2.
- **Serialization:** Native shown-window evidence, integrated composition and joins remain
  serialized shared resources. No design-lane result grants desktop or main access.

## Clearance checklist before implementation

1. Resolve the GHCP foundation request to an exact accepted SHA or observe its landing.
2. Freeze both lane designs and record independent design PASS/BLOCK verdicts.
3. Record exact file/signature allowlists, section-2 Surface/View grants and mutually
   satisfiable guards for all shared seams.
4. Amend the graph with explicit per-lane evidence nodes and their failure oracles.
5. Keep ER excluded from the five-view completion claim and keep native/integration work
   serialized.

**Residual risk:** the design lanes may discover a real cross-lane decision edge. The plan's
re-plan trigger covers that case; it must return to the Conductor rather than widening either
worker's scope.

## Focused evidence-node disposition

Reviewed Conductor repair `137d4fe5e364ed320abde95fb6602dd8af28f204`
against the missing-evidence-node clearance above.

**CLEARED.** `E1-P` and `E2-P` are separate T2 nodes after their respective
implementation nodes and before independent `R-I`. Each names an exact committed Proof
Pack path, requires the exact revision and raw receipts, and fails when observations are
missing, unexecuted or contradictory. `R-I` now opens those frozen proofs and raw results
rather than accepting an author summary. The Mermaid DAG matches the node table.

The cost change is honest: each inferred `I=4` allocation became `I=3` plus `P=1`.
Therefore `T1=23`, `T∞=15`, and the two-lane Brent bound `19` remain unchanged. No
speedup or measured duration is newly claimed.

This disposition clears only the missing evidence-node finding. The accepted-foundation,
exact-grant, shared-guard and independent-design barriers remain open. It grants no product
implementation, native run, integration or publication.

## Independent E2 contract-spike review

Reviewed frozen E2 spike `55df5b6e75e2a7b52f2f5aeb5433e19383196570`,
including `RESULT.md`, `Program.cs`, `architecture.fixture.json` and
`docs/proof/atlas-architecture-contract.md`, against Owner F1/F2 decision
`57a09ed6`. The frozen console was executed on Windows and reproduced exit 0 and
all 23 printed PASS rows.

**BLOCK independent spike clearance.** The process does execute exactly 23 named
assertions, and 16 rejection/unresolved assertions run before the positive fixture.
That count is true as an execution count. It is not yet 23 independent contract
oracles because three advertised semantic checks cannot fail when their claimed
behavior is removed or changed.

### Findings and clearance

1. **[Blocker, Verified] Alias collapse/provenance is read back from the fixture, not
   produced by the subject.** `two-aliases-one-declaration` directly checks that both
   input rows already contain the same `rootRef` and anchor. `Validate` only rejects a
   `rootRef` absent from resource IDs; it emits no grouped root or retained-alias result.
   The check would still pass if no collapse behavior existed. **Clear when:** an
   admitted pure projection consumes validated rows and returns one declaration root
   with both distinct alias evidence records; a negative mutation proves an alias to a
   different/missing root does not collapse, and both anchors survive in output.

2. **[Major, Verified] The distinct-scope oracle is confounded by a second differing
   field.** `equal-symbols-distinct-scopes` compares a local key over workspace, scope,
   file and symbol, while the two fixture resources differ in both `scope` and `file`.
   Removing scope from the key would leave the check green. **Clear when:** the two
   cases are identical in every identity component except scope and the subject still
   keeps them distinct; add the complementary equality case only when an admitted
   fully known deployment-scope producer exists.

3. **[Major, Verified] Authority containment is a constant assertion.** `Result.Authority`
   always returns `unestablished` and `EnforcementProven` always returns false.
   `authority-not-promoted` therefore cannot detect a future promotion path. The two
   unknown-field rejection tests do meaningfully reject injected `accepted` markers,
   but the constant adds no independent evidence. **Clear when:** authority status is
   an output of the validated carrier path and malicious marker mutations demonstrate
   that it remains unestablished, or remove this row from the independent check count
   and describe it as a static containment sentinel.

4. **[Major, Verified] Several reported carrier limits have no negative-first oracle.**
   `Validate` contains `layer-kind`, 32-row and 256-character rejection branches, but
   the 23 checks do not exercise them. Required non-ID strings also accept whitespace.
   `RESULT.md` says these limits are part of the contract actually exercised. **Clear
   when:** negative cases cover unknown layer kind, row overflow, text overflow and
   blank required strings with stable diagnostics, or the evidence narrows its claim
   to the rejection branches actually executed.

### Claims that clear in this bounded run

- **[Verified]** Duplicate IDs, role/shape faults, aggregate references, layer
  membership/dimension/state, alias-to-alias misuse, hostile acceptance fields and
  stale/missing/foreign anchors reach their named rejection or unresolved diagnostics.
- **[Verified]** `ReadLiteralBicep` is a deliberately narrow regex over fixed literal
  source. It neither compiles nor evaluates Bicep. Expression names remain null,
  deployment scope remains null, and `SameDeployment` refuses partial identity.
- **[Verified]** The proof explicitly leaves fully known deployment equality and
  US-E8 acceptance unresolved. It makes no cloud, authorization, native or product
  acceptance claim. Linux evidence also remains pending with the Conductor.

No E1 result was reviewed. This BLOCK applies only to the independent evidentiary value
of the E2 spike; it does not reject the Owner's bounded design direction or grant any
product work.
