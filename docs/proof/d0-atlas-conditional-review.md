---
id: proof-d0-atlas-conditional-review
title: "Independent review of D0 finite conditional closure"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [d0, atlas, independent-review, static-analysis]
links:
  - { to: proof-d0-atlas-independence, rel: depends-on }
  - { to: proof-d0-atlas-independence-rereview, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "BLOCK: finite rebinding controls pass, but mismatched syntax/fingerprint capture is accepted and early refusals print unmeasured zero telemetry."
---

# Disposition

**BLOCK - Verified.** The independent normal-build run passes all 66 authorized cases. Independent App/Core extension and assignment controls establish the previously missing finite rebinding behavior. The accepted-result claim remains blocked by a deterministic captured-input consistency counterexample. Refusal-path telemetry also presents unmeasured defaults as numeric observations.

Reviewer: codex-astra-d0-review; session: codex-d0-atlas-conditional-review. Provisioned tree: `C:/Projects/ai-de-review-d0-atlas-conditional-closure`; branch: `review/d0-atlas-conditional-closure`. Reviewed author tip: `d8fe8b1f2da0df053694221d3a26e66026ce66b3`. The original independent receipts remain unchanged. No product or author test source was edited; the capture fixture contains private copies under the review tree's `.artifacts/` directory.

## Goal, scope, graph and grounding

Goal: independent CLEAR/BLOCK of the Owner-admitted finite conditional closure, with exact inputs, evidence and residual limits. Done when the frozen source and independent headless controls establish that disposition in a committed receipt. Not in scope: repairs, joins/pushes, broad App/Core suites, STA/shown/native execution, arbitrary transitive/runtime semantics, or an all-configuration/generator proof. Tier T2; no subagents; budget 20 calls / 25 minutes; checkpoint 12.

The optimize-graph pass retained this bounded sequence: authority and frozen input inspection -> normal-build run plus source inspection -> independent required assignment controls -> one captured-input consistency seam and its matched-snapshot control -> evidence receipt/audit. Once the decisive mismatch was observed, investigation stopped. Later operations only recorded, verified and committed the evidence.

Grounding follows the author proof and earlier independent BLOCK, measured design `b76581ae38bb1344457ef161326bb5bc861400a8`, and the parent plan's Owner decision `cl-01M2KZXAG8BYSSH345PC435EKN`. That decision requires a finite App -> Core source closure, at most eight project/symbol assignments, source propagation for each state, immutable captured inputs, refusal of mutation/inconsistent references, fixed reviewed build/generated inputs, and coverage completion distinct from acceptance. It expressly excludes arbitrary generators/MSBuild configurations.

Surface list: source/build/generated capture -> Core/App compilation and metadata -> exact D0 roots/ports -> conditional census and source propagation -> selected-expression identity snapshots -> acceptance/refusal telemetry and test evidence. D0 store/model/service/wire/UI product behavior is unchanged. Testing floors exercised: D0 hygiene, D1/D2 deterministic discrimination, D3 dependency direction, actual Roslyn source binding, and SRE measurement truth. The earlier direct shared-handoff review remains applicable because this author unit changes only the guard/proof/audit.

## Frozen evidence

The frozen test's exact byte and LF SHA256 is:

`60FA8CCCB388E396076787F023694A7C3E4EAC377DB1C553CB73DA3A95528023`

The receipt writer compares the original enum block and entire FileRead/FactoryBuild/RepoRoot suffix against `247e6b4eaefba92cea250f657159a8768e8fb91f` after newline normalization before installing this receipt. They remain equal. Previous independent re-review receipt hash remains `0F81D89F88028A7E2658FFC9FDC58043A73394100BD1544A69AD21DF692FAA45`.

Own normal-build command:

```text
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter 'FullyQualifiedName~SolutionTreeProbeTests.ProbeAtlas_' --logger 'trx;LogFileName=review-baseline.trx' --results-directory .artifacts/d0-conditional-review -v q
```

**Verified:** exit 0; actual TRX 66 executed/66 passed/0 failed/0 not executed/0 aborted. The original 37 cases remain in this filter; no enum/FileRead/FactoryBuild case was executed. Masking bits 0-7, operator identities, closure-control rows and exhaustive coverage cases were read from the actual result list. TRX start `2026-09-15T19:42:06.9547289-07:00`, finish `2026-09-15T19:43:10.0101256-07:00`; measured span **63.0553967 seconds**. Its SHA256 is `E9A9ED0DA767BA14278524C05FC91D23C604129E720697C95B0C21DA5A1E3858`.

The baseline succeeding after a normal build in this distinct worktree establishes that the four golden generated-input pins are usable here. This is not a claim about every build configuration. The seven build-input mutation cases pass their refusal assertions; the pins are fixed, not refreshed during the run.

Independent scratch command:

```text
dotnet run --project .artifacts/d0-conditional-review/ReviewSpike.csproj -v q
```

It returned exit 0 because it records analyzer observations, including the failed acceptance oracle. **Its exit code is not a CLEAR.** `Program.cs` SHA256: `E96BB6F628B84105317EC3C4E4E5C1B3949B7451914060BF2D2ED2046E24BE7F`; `spike.log`: `AB0515866A83062425936610A0F22842CF9D67BC33B60D4A9ADC27EED015BCF5`.

Local evidence lives under `.artifacts/d0-conditional-review/`: `baseline.log`, `review-baseline.trx`, `ReviewSpike.csproj`, `Program.cs`, `spike.log`, `captured-corpus/`, `write-receipt.py`, and `receipt-checks.json`. The author TRX inspected at closure is retained separately under `C:/Projects/ai-de-fix-d0-atlas-conditional-closure/.artifacts/d0-closure/closure-green.trx`, with expected SHA256 `4320d5a0865a9f63c0a72001afed190c29f184bcc653fe79517a5f5bd567aee8`; the writer verifies its 66/66/0 counters and hash before writing this receipt. Historical author 21/20 administrative overrun remains in the author proof; this review does not rewrite it.

# Independent finite-closure results

**Verified observations:**

| Case | Population / visited | Result |
| --- | --- | --- |
| Real baseline | 1 / 1 | 26 roots, 3976 references, 58 ports, 346 trees, 4551 expressions; complete and accepted |
| Independently added App RELEASE extension | 2 / 2 | Binding changes in enabled state; refused |
| Same independently added extension in Core source | 2 / 2 | Source propagation changes 12 App expression identities; refused |
| Identical extension in unimported namespace | 2 / 2 | Complete and accepted |
| App and Core each use RELEASE | 4 / 4 | Separate census keys; Core-enabled states 2 and 3 refused |
| Incompatible receiver extension | 2 / 2 | Complete and accepted |
| File-local undef | 2 / 2 | Complete and accepted |
| Three-symbol capacity fixture | 8 / 8 | Complete and accepted |
| Four-symbol overflow | 16 / 0 | COVERAGE refusal without sampling |
| Dynamic identity | 1 / 1 | UNSUPPORTED; coverage/acceptance false |
| Inconsistent references, unsupported parse options, missing generated tree | No enumeration | Named CLOSURE refusals |

The independent project-key census is exactly `AiDe.App:RELEASE;AiDe.Core:RELEASE`. The standalone masking fixture was invoked independently for all eight states: only bits 3 reports `(Ambiguous=True, Errors=1)`; the other seven report `(False, 0)`. It remains a standalone compiler-composition discriminator, not a claimed prior bypass of the combined guard.

**Prior FR-003 rebinding counterexample: resolved for the reviewed finite closure.** Exhaustive Roslyn assignments now see the applicable inactive extension without a handwritten extension-lookup approximation, including Core propagation. Exact roots, port declaration identities, earlier conditional refusals and unrelated positives remain covered. The acceptance verdict for the whole guard is still BLOCK for the following separate captured-input seam.

# FR-004 - Source snapshots and recorded fingerprints can describe different versions

**Issue; P1 / Major; Verified counterexample; Test Architect and architecture/Security BLOCK.**

Source evidence: `SolutionTreeProbeTests.cs:339` reads ordinary source text into immutable syntax trees. Line 360 separately enumerates and rereads ordinary files to populate `FrozenSources`. Lines 485-486 compare disk only against this second read. There is no equality check between captured ordinary syntax text and its recorded fingerprint. The four generated trees do have a separate text/pin check; ordinary trees do not.

The independent fixture reproduces the exact inconsistent state without touching product source:

1. Copy the real captured source trees and reviewed build/generated files into `captured-corpus/`. Retarget syntax paths and Root to those copies, preserving frozen metadata references. Matching syntax A / disk A / fingerprints A is accepted.
2. In the copied `SolutionTreeSurface.ShowLoading`, insert `System.GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState));`. The copied disk now contains B.
3. Recompute `FrozenSources` from copied disk B, as Load's second read does, while retaining captured syntax A. The fixture confirms `CAPTURED_SYNTAX_EQUALS_FINGERPRINTED_DISK False` and an empty `CAPTURE_CLOSURE_ERRORS` list.
4. Analyze that state: **zero errors, coverageComplete=True, accepted=True**, 3976 references and 4551 expressions. The recorded disk has a direct Atlas dependency, but the analyzer examines the earlier syntax.
5. Keep the same disk B and fingerprints B; replace only the copied App syntax tree with matching B. The same analyzer reports **two ATLAS errors**, 3989 references and 4565 expressions, accepted=False.

This paired control establishes that stale captured syntax is what hides the planted dependency. It is an actual deterministic analyzer-state reproduction, **not a claim that a concurrent filesystem race was observed**. The scheduling path is **Inferred from the opened read order**: a file change after its parse read and before its fingerprint read produces A/B. Both later disk checks compare B to B, so they cannot detect that mismatch. No adversarial timing claim or arbitrary runtime dependency claim is needed for this state invariant.

**Clears when:** the real loader derives each ordinary syntax tree and fingerprint from one captured decoded text value, with later inventory/content checks against that value. A deterministic capture-change seam in that loader must produce a named CLOSURE refusal; arbitrary reflection-corrupted state alone is not the final acceptance oracle. The demonstrated A-syntax/B-fingerprint state must no longer be reachable as an accepted capture, while consistent A remains accepted and consistent B reaches the existing ATLAS refusal. Add permanent discriminating controls and independently observe them with all retained finite-closure cases. Preserve the current UTF-8/BOM/CRLF normalization contract and fixed generated-input ceiling; do not refresh fingerprints from a later disk version and treat that as capture validation. These checks establish the bounded loader contract, not an atomic filesystem snapshot or measured race frequency. Owner clarification `cl-01M2M1X65X5WECX0V5HBNCV424` retains this precise predicate.

# FR-005 - Early refusal telemetry prints default zero as if measured

**Issue; P2 / Major; Verified; SRE BLOCK on measurement truth.**

Evidence: Result's numeric fields default to zero at `SolutionTreeProbeTests.cs:308-310`. The early closure return at line 497 and overflow return at line 502 do not populate the timer or captured corpus fields. ToString emits those values without a missing/not-recorded marker.

The independent observer counts the already-captured syntax trees and times the Analyze call separately:

| Refusal | Emitted corpusTrees / seconds | Independently observed captured trees / call seconds |
| --- | --- | --- |
| Overflow population 16 | 0 / 0.000000 | 347 / 0.297008 |
| Inconsistent references | 0 / 0.000000 | 346 / 0.119085 |
| Unsupported parse options | 0 / 0.000000 | 346 / 0.048718 |
| Missing generated tree | 0 / 0.000000 | 345 / 0.024176 |

Overflow also emits `symbols=0` after calculating the four-symbol population of 16. These zeros are default values, **not measurements**. The observer timings include its small wrapper overhead and are not substituted as the analyzer's internal measurements. `assignments=0` on overflow is valid: no assignments were visited. The finding does not equate every zero with missing data.

**Clears when:** every return reports elapsed time and known captured/censused quantities from their actual sources, or explicitly represents unrecorded quantities as unavailable. Exercise closure, root-selection and overflow refusal paths; preserve truthful zero visited assignments and separate coverage/acceptance. This follows the standing measurement rule that missing data must not degrade to plausible numeric values. It does not require a new telemetry framework.

# Review lenses, limits and controls

- **Test Architect: BLOCK**, FR-004. The passing suite does not constrain syntax/fingerprint agreement; the matching-snapshot control isolates that missing invariant.
- **Architecture/Security: BLOCK**, FR-004. An accepted static proof must name the bytes it examined. The current capture can certify an earlier version while validating later disk bytes.
- **SRE: BLOCK**, FR-005. Early refusals hide available volume/timing information behind unmeasured zeros.
- **Simplifier: PASS for the bounded correction envelope.** Repair the capture invariant and result construction; no new generic compiler/build/runtime framework is justified.

Residual ceilings remain the Owner's: App -> Core finite source closure, at most eight complete project/symbol assignments, fixed build controls and four reviewed generated texts, exact D0 roots/ports, and direct static expression identities. This is not complete generator/MSBuild configuration or runtime/transitive independence. No broader code investigation continued after the decisive captured-state result.

Class -> sweep -> derive -> prevent: DC-118 applies to a proof boundary drifting from captured text to separately read fingerprints; the measurement principle applies to default numeric observations. Sweep is the observed A/A, A/B and B/B capture states plus existing immutable-reference/generated controls and early refusal paths. Derive: source identity must be produced from the same captured text, and unknown metrics are not zero. Prevent: the paired capture-state and refusal-telemetry regression predicates above. Controls remain pending author repair; the reviewer did not implement them. Parent owns the central class/control register.

# Closure

The review objective is complete as a precise BLOCK. Author repair and re-review remain with the Conductor/Owner; no author self-clearance occurred. Only this new receipt and own official audit entries are committed. Explicit UTF-8, byte-verified temporary output and atomic replacement protect this receipt. The exact lease is released in finally. Generated audit-data and raw scratch remain retained and uncommitted. No source reset, cleanup, native/full test, join or push occurred.

Actual call count and automatically measured duration are recorded in the closing audit. AIDE_CONTRACT_LOG was not supplied; no destination was invented. Episode evidence: `docs/proof/d0-atlas-conditional-review.md`.
