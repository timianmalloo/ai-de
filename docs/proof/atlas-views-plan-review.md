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

## Independent five-view design review

Reviewed frozen E1 design `755d0347` and E2 design `e2c17251` against Addendum E and
architecture intent `92e025ae`, proposal/mock direction `1065a851`, the repository
mockup, UI Archetype Grammar G1/G9–G13, UI rules U9/U16/U17/U19–U20, and UI craft
rules DX8–DX10. Owner F1/F2 supplement `57a09ed6` is controlling design input. The
E2 spike was not re-reviewed here.

**DESIGN GATE: BLOCK implementation dispatch for both lanes.** The designs preserve
the five-view boundary, keep ER/E3/E4 out, avoid runtime/cloud overclaim and specify
useful end-to-end oracles. The following finite design work remains.

### P0 hard blocks

| Priority | Persona / veto | Evidence | Clearing predicate |
|---|---|---|---|
| P0.1 | UX Researcher/IA and UX & Accessibility — **BLOCK** | Governing Addendum E Part C §C1 selects **G6 Multi-Panel Data Terminal**. E1 §2 silently selects catalog **G1 Parametric Modeling Workbench**, then deviates from its defining 3D constructor, feature history, editable parameter, persistence and input model. E2 §7 supplies only a proposed prose archetype and explicitly defers the formal signature. This fails grammar G1/G9/G10/G13 determinism. | Publish one final, syntactically valid five-view signature aligned with Addendum E G6, plus phase-specific deviations. If G1 is retained for E1, record an explicit governing-spec amendment and prove the result still round-trips to that family. Each design must resolve every facet into its actual WPF structure, state, persistence and synchronization behavior. |
| P0.2 | Data & Persistence plus DDD/UML — **BLOCK E2** | E2 §§3/5/9 still call F1/F2 unresolved. Owner `57a09ed6` later chose explicit validated semantic intake, declaration-root identity, source-declared aliases, and exact equality only for fully known deployment identity. E2's DTOs remain prose fields; carrier schema, authority state, limits, producer-origin table and unresolved cross-declaration identity are not folded into the design. | Revise E2 to incorporate F1/F2 verbatim in typed DTO/schema form: declared-vs-accepted authority, root/alias evidence, exact identity state, origin/version, symbolic unknowns, and byte/row/depth/text limits. Keep cross-declaration equality and full US-E8.b open until an admitted fully known identity producer passes. |
| P0.3 | Distributed Systems, Security and Test Architect — **BLOCK shared contract** | E1 §4 includes `RequestSequence` and source observation fields. E2 §5's request/page description has no request sequence, explicit observation token, completion state or restore receipt even though §6 promises late-response rejection and receipt-based Back. Both lanes need the same reader, codec, client/owner and registration seams. | After the foundation pin, freeze one shared request/response envelope and golden payloads carrying scope, epoch, manifest, request sequence, observation/source binding, completion/bounds, capability/version and Restore receipt. Prove late N cannot replace N+1 and Back restores the exact prior selection. Name one writer for every shared path/signature. |
| P0.4 | Orchestrator and Release — **BLOCK authorization** | E1 §6 and E2 §5 list overlapping Core/Ipc/client/owner/factory/host seams. Several E1 server/registration and host paths are intentionally unnamed; E2 lists families rather than granted signatures. No accepted foundation exists. | At graph node C, record the accepted SHA, exact per-lane files and public signatures, section-2 grants, and a joint guard table. Any recursive guard states root, recursion, token set and allowlist. E1/E2/GHCP/Core/Grok clauses must be jointly satisfiable; shared paths have one integrator. |
| P0.5 | UX & Accessibility and Test Architect — **BLOCK mock/state proof design** | Proposal `1065a851` exposes only ready, empty, loading, error, stale and AI-unavailable scenarios. E1 §5.2 requires unselected, partial, unsupported, canceled and overflow behavior too; E2 §7 lists a still larger matrix but gives no transition/forbidden-content table. Neither has the DX8–DX10 five-view self-contained review harness. | Commit an updated synthetic mock/review harness with persona, viewport, view, state, theme and reduced-motion selectors. Render every hard state with real copy, enabled/disabled actions and preserved selection. Include Sequence/Activity ambiguity, async/cancel/error/omission; Domain declared/unknown authority; Layer typed/unsupported edges; Azure declaration/alias/unknown identity. Run the rubric critique before native authoring. |
| P0.6 | SRE and Test Architect — **BLOCK performance/bounds** | Addendum E A10 sets open `≤2 s p95`, filter and retained switch `≤150 ms p95`, but leaves a design-approved UI-thread frame budget. E1 §11 only fixes the retained pivot and flags initial query/render/frame thresholds; its request limit has no maximum. E2 §7 has no numeric latency or row/edge/text/layout caps. | Before dispatch, name one authorized fixture per lane, cold/warm protocol, sample count, hardware, exact p95/frame thresholds and hard request/publication caps. Negative controls must fail when each cap or frame budget is exceeded; telemetry must distinguish not-recorded from zero. |
| P0.7 | UML/graph and Test Architect — **BLOCK E1 semantic/page boundary** | E1 §§5/10/12 leaves the syntax-vs-CFG choice flagged. One occurrence window feeds a graph with entry/exit/control nodes and edges, but the design does not define whether structural nodes consume the window or how an edge crossing a page boundary is represented. | Freeze the supported control-flow algorithm with a bounded source spike. Define the paging grain, structural-node accounting, cross-page edge/gap representation, stable ordering and page recomposition oracle. Prove branch/loop/exception/cancel/async cases do not disappear while completion says complete. |
| P0.8 | Native Desktop, UX & Accessibility and Test Architect — **BLOCK E2 native oracle contract** | E1 §10 names keyboard/UIA, Source/Back, state reachability and diagram/list/inspector equality controls. E2 §8 groups native behavior into broad `ArchitectureSourceBackKeepsBinding` and `WireAndNativeKeepUnknowns` rows; it does not give per-view focus, UIA, row equality or state-reachability falsifiers. | Add per-view native controls for Domain, Layer and Azure: graph/list/inspector row and relation equality, keyboard-only Source/Back with focus restoration, UIA kind/identity/uncertainty, high contrast and 100/150/200% DPI, long labels, each hard state and serialized real WPF composition. |

### Triggered persona disposition

| Persona | Verdict / reason |
|---|---|
| Product Strategist | **PASS scope only:** five views are explicit; ER, E3 and E4 remain excluded and visible. |
| Test Architect | **BLOCK:** P0.3, P0.5–P0.8 leave required falsifiers or evidence inputs undefined. |
| Data & Persistence / DDD-UML | **BLOCK E2:** P0.2; E1's occurrence grain and one-projection invariant otherwise pass design review subject to P0.7. |
| UX Researcher/IA | **BLOCK:** P0.1 and P0.5; the governing archetype and recovery flows are not frozen. |
| UX & Accessibility | **BLOCK:** P0.1, P0.5 and P0.8; no complete review harness/native oracle matrix. |
| Security & Privacy | **BLOCK shared admission:** threat/refusal coverage is well scoped, but accepted source binding and exact wire/host authority remain P0.3/P0.4. |
| Distributed Systems | **BLOCK E2/shared seam:** request ordering and receipt fields are not represented end to end. |
| SRE | **BLOCK dispatch:** numeric workload/cap/frame admission is incomplete. |
| Native C# | **BLOCK dispatch:** exact host paths, UI-thread boundary and real-composition oracles remain unresolved. |
| Release/Integrator | **BLOCK:** foundation, capability/version payloads, grants and joint guards are open. |
| Simplifier | **PASS with advisory:** E1's one projection and E2's one immutable snapshot avoid duplicate truth. Reuse one E2 presentation/view-model core behind the three native views so state, bounds and Source/Back cannot drift. |

### P1 advisory refinements

1. E1 should name the exact existing WPF token keys it consumes, as E2 already does,
   and both should include the final load-bearing copy ledger in the review harness.
2. Make unknown-enum decode/refusal and disclosure-string encoding explicit in the
   shared golden wire fixtures.
3. Keep `Source`, `Back` and `Restore` terminology tied to one receipt type across both
   designs; document navigation to a specification clause needs the same identity and
   return-state precision as navigation to source.

### Design strengths retained

- E1 maps every US-E6 clause to an occurrence/control model, keeps static order distinct
  from runtime order, and uses one immutable projection for diagram/list/inspector/export.
- E2 keeps declared, target, current, unsupported and runtime meanings separate; it does
  not turn Bicep declarations into deployed inventory or domain roles.
- Both retain source evidence, unknowns, bounds, cancellation, hostile inert text and
  serialized native proof, and neither claims implementation or native acceptance.

Clearance requires a new frozen revision from each affected author and author-independent
re-review. It does not require waiting for Linux spike packaging, and it grants no product
implementation while the foundation barrier remains open.
