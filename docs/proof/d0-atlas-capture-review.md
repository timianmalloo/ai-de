---
id: proof-d0-atlas-capture-review
title: "Independent clearance of D0 capture consistency and refusal metrics"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [d0, atlas, independent-review, static-analysis]
links:
  - { to: proof-d0-atlas-independence, rel: depends-on }
  - { to: proof-d0-atlas-conditional-review, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "CLEAR for the reviewed finite direct-static guard: real loader capture changes refuse, matching snapshots discriminate Atlas, and refusal metrics are observed or explicitly unavailable."
---

# Disposition

**CLEAR - Verified, within the existing finite direct-static contract.** Independent execution through the real `Load(string, Action)` seam satisfies FR-004. Independent corpus/time/unknown-field assertions satisfy FR-005. The normal-build authorized filter passes **73/73**, retaining the prior 66 finite-closure cases. No new product or guard veto remains in this bounded review.

Reviewer/session: codex-astra-d0-review / codex-d0-atlas-capture-review. Worktree: `C:/Projects/ai-de-review-d0-atlas-capture-consistency`; branch: `review/d0-atlas-capture-consistency`. Reviewed author: `c6868e601d057023d01feedbd25b0c3604f9f8ff`. The earlier BLOCK receipts remain unchanged and are resolved by this new receipt, not overwritten. No source, product, project, specification or pin change was authored by the reviewer.

## Goal, scope and execution graph

Goal: independently re-review capture consistency and refusal metrics against FR-004/FR-005 and Owner `cl-01M2M1X65X5WECX0V5HBNCV424`. Done when actual loader-seam results, metric observations and the retained headless union support a committed CLEAR or precise BLOCK. Tier T2; no subagents; 12 calls / 15 minutes; checkpoint 8. Not in scope: repairs, join/push, native/shown/STA/factory execution, broad App/Core suites, or any larger generator/configuration/runtime contract.

The bounded optimize-graph sequence was: frozen diff and original predicates -> normal-build union -> independently authored real-loader callback controls and metric observations -> result/hashes -> atomic receipt, audit, commit and lease release. Two scratch setup defects consumed the closure reserve; both were diagnosed and preserved, not treated as product findings or silently retried. The checkpoint reported them to the Conductor. No further source exploration was needed after the required controls passed.

Grounding follows the author proof and original conditional review, plus the Owner's recorded capture/refusal correction in the parent plan. Surface list: immutable source capture -> fingerprint inventory -> later disk checks -> real App/Core semantic boundary -> accepted/refused Result -> telemetry. Existing D0 store/model/service/wire/UI behavior is unchanged. Test Architect/architecture/Security/SRE/Simplifier lenses are reported below. Testing floors: deterministic discriminating controls, actual Roslyn binding, real filesystem copies and file sharing, and truthful measurement of refusal paths.

# Frozen provenance and normal-build results

Exact source byte SHA256:

`5611216ECA8EBCF4F207A853938991886A9401596908730A63AFE0973DD4A0DE`

The receipt writer verifies that hash, compares the enum block and full FileRead/FactoryBuild/RepoRoot suffix against `247e6b4eaefba92cea250f657159a8768e8fb91f` after newline normalization, and verifies the prior receipt unchanged before installing this document. No incidental original probe changes were found.

Own normal-build command:

```text
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter 'FullyQualifiedName~SolutionTreeProbeTests.ProbeAtlas_' --logger 'trx;LogFileName=review-baseline.trx' --results-directory .artifacts/d0-capture-review -v q
```

**Verified actual TRX:** 73 executed, 73 passed, zero failed/not executed/aborted. The seven new real-loader/refusal-metric case names were inspected; the 66 previous controls remain in the same filter. No enum/FileRead/FactoryBuild test was selected. TRX start `2026-09-15T20:04:06.2661314-07:00`, finish `2026-09-15T20:05:15.8582445-07:00`; measured span **69.5921131 seconds**. The original process completion is read before closing the review.

Own TRX SHA256: `F45A87FC1A44CD019695241F02D96BF7234C8C45A59700E3540757E18B62EE33`.

The author's 73-case TRX with SHA256 `e791a66afe3b7ab2d9f31d7a2dc5007ad2f59868545c0711ddf2d2025d647789` is independently located, hash-verified and parsed by the receipt writer; its actual path/counters are recorded in `receipt-checks.json`. Historical author 9/8 administrative overrun and red7/0/7 remain in the author proof. In particular, the old unreadable case threw IOException; it was not an old false-green observation.

# FR-004 - CLEAR through the real loader seam

**Source inspection - Verified:** `Load(string, Action)` materializes `(Path, Text)` once. Both syntax trees and FrozenSources fingerprints derive from that same captured text. The callback executes after syntax capture. Later inventory/content checks compare disk against the captured fingerprints. The UTF-8/BOM and CRLF normalization contract is retained. This is immutable capture plus later validation, not an atomic filesystem snapshot.

The independent harness directly invokes the real overload with copied filesystem corpora and an Action callback. Reflection only accesses that private overload and result properties; **no synthetic FrozenSources/compilation state is installed**. Each callback is asserted to execute exactly once. The harness asserts named refusal/coverage results, rather than printing a success label unconditionally.

| Independent case | Observed result |
| --- | --- |
| Real-root baseline and four matching A/A copies | Complete and accepted; 26 roots, 3976 references, 58 ports, 346 trees, 4551 expressions |
| Direct Atlas call inserted after syntax capture | CLOSURE source inputs mutated after capture; zero assignments; not accepted |
| Captured source removed in callback | Same named CLOSURE refusal; zero assignments |
| New source added in callback | Same named CLOSURE refusal; zero assignments |
| Captured source held with FileShare.None through Analyze | CLOSURE unreadable input; zero assignments; exception represented as refusal |
| Fresh Load of matching B/B with planted Atlas call | Two ATLAS errors; 3989 references, 4565 expressions; not accepted |

The changed-input callback inserts `System.GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState));` into the copied `SolutionTreeSurface.ShowLoading`. Product files are untouched. Locks are disposed in finally. The controls independently exercise the exact real-loader seam requested by the Owner; they neither measure race frequency nor assert an atomic filesystem view.

**FR-004 cleared.** A/A acceptance, after-capture A/B refusal, and matching B/B Atlas refusal were all observed through the same loader. Missing/added/unreadable coverage also passed.

# FR-005 - CLEAR for measured and unavailable fields

**Source inspection - Verified:** a common Finish result path records the current captured syntax-tree population and Stopwatch elapsed time for early closure/root/overflow exits and the normal return. Unevaluated roots, references, symbols, expressions and population are nullable. PortsRecorded separates an unexamined port set from a measured empty one. Rendering uses `not-recorded` for unavailable values.

The independent harness counts the actual Core/App syntax trees and separately times each Analyze call. It asserts exact equality of emitted corpus count, positive internal elapsed time no greater than the enclosing observation, and expected nullable/unavailable fields. Wrapper timings are only comparison evidence, not replacement analyzer measurements.

| Refusal | Captured trees (emitted and independently counted) | Internal seconds | Unavailable fields |
| --- | ---: | ---: | --- |
| Inconsistent references | 346 | 0.033908 | roots, references, ports, symbols, expressions, population |
| Missing selected member | 346 | 0.038299 | roots, references, ports, symbols, expressions, population |
| Overflow population 16 | 347 | 0.306038 | expressions |

Overflow preserves observed symbols=4, population=16 and assignments=0. Other early exits also report zero visited assignments. These are truthful zero counts, distinct from unavailable observations. Actual capture-change refusal times were also positive: changed0.030139, missing0.014740, added0.003515, unreadable0.008550 seconds, each with 346 captured trees and explicit unknowns for unevaluated stages.

**FR-005 cleared.** Closure, root-selection and overflow metrics satisfy the predicate with actual observations and explicit unavailable fields.

# Evidence artifacts and scratch corrections

Independent command:

```text
dotnet run --project .artifacts/d0-capture-review/ReviewSpike.csproj -v q
```

The final command exits 0 after all independent assertions and emits `INDEPENDENT_REAL_LOADER_AND_METRIC_ORACLES_PASS`. Its log SHA256 is `B637507E52E50D474FDFCA2F268437294F9ACACA405F566ACB02CB995613A96D`.

Retained local evidence: `.artifacts/d0-capture-review/` contains `baseline.log`, `review-baseline.trx`, `ReviewSpike.csproj`, `Program.cs`, `spike.log`, `spike-qualified.log`, `spike-final.log`, copied corpora, `write-receipt.py`, and `receipt-checks.json`.

Two scratch defects are preserved:

1. Path spellings containing different separators bypassed string deduplication and File.Copy refused a duplicate generated destination. Correction: normalize Path.GetFullPath before deduplication. No capture mutation had run.
2. SDK default recursive items picked up the retained copied corpus on rebuild. Correction: disable scratch default compile/WPF page/application discovery and explicitly compile Program.cs. The compiler refused duplicate/conflicting inputs before execution. The final explicit-input project is the control; no warning/test was suppressed and no product project was changed.

Class -> sweep -> derive -> prevent: capture identity and missing telemetry classes from the prior review are controlled by the seven permanent cases plus single-captured-text construction/common finishing. The review setup adds path-normalization-before-identity and scratch build-input isolation recurrences; the retained explicit paths/items and fail-closed build/file operations are the controls. The parent owns central lesson/control-register updates. Raw failures and partial corpora were not discarded.

# Lens outcomes and limits

- **Test Architect: CLEAR.** Real-loader discrimination plus the retained 73-case union satisfies FR-004/FR-005.
- **Architecture/Security: CLEAR.** Syntax and fingerprints share one captured value; later changed inventory/content refuses. Previous finite rebinding and exact root/port controls remain in the passing union.
- **SRE: CLEAR.** Observed corpus/time and explicit unavailable stages replace default zero telemetry; truthful zero assignments remain.
- **Simplifier: CLEAR for the bounded envelope.** Shared capture and result finishing resolve the two findings without widening into a build/runtime framework.

Residual ceilings are unchanged: finite App -> Core closure, maximum eight complete project/symbol assignments, fixed reviewed build controls and four generated-input texts, exact D0 roots/ports and direct static expression identities. This review does not establish arbitrary generator/MSBuild configuration completeness, an atomic filesystem snapshot, all runtime/transitive dependencies, or rendered/native D0 behavior. Later canonical qualification and Release remain with the parent under their own scheduling authority.

# Closure

The bounded review is complete. Only this new receipt and official own audit records are committed. The receipt is explicit UTF-8, byte-verified through a temporary file, atomically replaced and read back. The exact lease is released in finally with observed liveness. Generated audit-data and all scratch remain retained/uncommitted. No source repair, cleanup, join or publication occurred.

Actual calls and measured duration are in the closing audit. AIDE_CONTRACT_LOG was absent; no destination was invented. Episode evidence: `docs/proof/d0-atlas-capture-review.md`.
