---
id: proof-atlas-harness-gate-review
title: "G4 independent review: correlated EDI correction cleared"
type: proof-pack
status: proposed
owner: "@timianmalloo"
tags: [atlas, testing, diagnostics, independent-review]
links:
  - { to: session-contracts, rel: depends-on }
  - { to: proof-atlas-harness-gate-repair, rel: relates-to }
review-by: 2026-12-15
summary: "Independent retained review clears G4 at 301bc67 after the original mixed-method masking probe is rejected, all scanner controls pass, and unchanged helper evidence is retained. The original BLOCK is preserved below."
---

# Current verdict: CLEAR at 301bc67

**Verified; high confidence:** G4-R1 is cleared at corrected author commit
`301bc67a61228a9c944d6ba778db98d241b75087`. Independent retained review reopened
only the original G4 predicate under a four-call continuation. This supersedes
the original BLOCK below; it does not clear integration or publication.

The exact independent reproducer was rerun against the corrected source:

```text
MIXED_EDI {"problems": ["tests/MixedEdiTests.cs rethrows a captured failure wrapped, with no XunitException guard before it. ..."], "wrapped_files": 1, "must_reject": true}
```

The diagnostic is abbreviated above; full output is retained in
`C:/Users/malla/AppData/Local/Temp/atlas-g4-independent-rereview.log`.
The original probe and false-clean output remain documented below. This is an
observed change from zero findings to one finding for the same mixed-method file.

Source diff inspection confirms EDI qualification now evaluates every wrapper,
requires the same innermost callable, and correlates either the same original
identifier or the foreach item from the wrapped collection. Adjacent guard and
wrapper matching rejects intervening ordinary statements. The actual retained
array loop and its nonempty check remain accepted. Legacy direct-throw guard
behavior remains unchanged; this is bounded lexical recognition, not proof of
arbitrary C# control flow or aliasing.

Reviewer executed both corrected scanner commands. Normal output remains four
STA files: two wrapping, zero plain rethrows, one original TCS handoff, one
exception-subject file. Self-test reports success for **11 TCS and nine EDI
fixtures**, with planted findings for the exact mixed-method probe, same-name
parameters across methods, unrelated original and unrelated collection. Healthy
same-original and same-collection positives are accepted. Existing unguarded,
qualified-wrapper, multiple-STA, TCS replacement and mixed-setter controls remain.

The commit diff is exactly scanner plus author proof. There is **no App helper
or test-file diff** between f43ff83 and 301bc67, and the author working tree was
clean. The previously inspected real-helper red/green and five compiled mutation
TRXs remain the applicable evidence; no App run was repeated for this scanner-only
correction. Original test-log observations below are retained, not replaced by
the scanner result.

Test Architect, SRE and Language Developer clear the retained G4-R1 veto against
this corrected source. The unchanged scoped Distributed Systems and Security
observations below remain applicable; no new data/persistence trigger is added.
This independent reviewer authored no scanner/helper repair and does not assert
self-clearance of this review document.

Retained execution: inspect correction and unchanged helper → replay original
probe plus normal/self-test → update receipt → commit/release/end. Four tool
boundaries; no additional agents, shared audit/lesson/derived writes, or unrelated
gates. Conductor captures this proof in its episode. Remaining work belongs to
the programme: advanced-main integrated qualification and GHCP publication.

## Historical verdict: BLOCK at f43ff83

**Verified:** frozen author commit `f43ff83ce16fbacc9db585a2ee5befc0ec4d9bf7`
does not satisfy the explicit predicate that an EDI guard must not exempt another
wrapped failure. This is one blocking scanner finding, not a rejection of the
observed helper results. No author code was modified by this reviewer.

## Review contract

Goal: independent G4 source/evidence review. Done when each requested predicate
has a supported verdict and this receipt is committed. Not in scope: author fixes,
G1/G2/G3/G5, integration, main publication, full/shown/native App or UIA runs.
Tier T2; fan-out cap one; 12 tool-boundary ceiling. Review identity:
`codex-atlas-harness-review` / `codex-astra-harness-review`, isolated branch
`review/atlas-harness-gate`, review base `9301207ee8f1164b53f70fb7f2ead91377f8f22a`.
Only this proof is authored. Conductor retains shared audit, lesson, derived and
episode-capture writes. No AIDE environment variables were present in this worker.

Grounding: programme `docs/plans/atlas-five-gates.md` in the Conductor tree →
`session-contracts` and the exact G4 ledger; author proof → `session-contracts`;
Rigor, Testing Strategy, collaboration protocol, DC-078/079/104/118 register and
the changed source. Surface list: caught exception → retained instance/category/
ordinal → persisted failure records → EDI/aggregate → xUnit/TRX; source catch →
TCS setter → awaited TCS → scanner classification and STA denominator.

Planned graph: contracts/source → raw evidence and bounded Python controls →
review/commit/release. Actual graph followed that sequence, adding one independent
masking probe and its corrected syntax-valid fixture before convergence. There
was no App runtime rerun and no delegation. Original opening frame was emitted
after the first grounding read; that ordering miss is recorded rather than
represented as compliant. Shell wildcard lookup was corrected by opening explicit
paths. Large grounding output was truncated; relevant source and protocols were
subsequently read with bounded ranges. No duration or token-cost estimate is
represented as measured. The fail-closed response to a demonstrated acceptance
failure is this BLOCK receipt, not an author-source change by the reviewer.

## Blocking finding G4-R1 — unrelated wrapper inherits EDI exemption

**Verified; high confidence; Test Architect / SRE / Language Developer veto.**
In `tools/verify-harness-diagnostics.py:179`, `has_guard` searches the entire file.
At line 247, any matching guard skips the file's wrapper finding. The EDI regex
at line 76 correlates its condition to its capture identifier, but does not
correlate that guard to every wrapper it excuses.

The following file, placed under temporary `tests/MixedEdiTests.cs`, was passed
to the actual candidate module's `check(root)`:

```csharp
private static void Healthy(Exception failure) {
    if (failure is Xunit.Sdk.XunitException) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    throw new InvalidOperationException("retained", failure);
}
private static void Broken(Exception unrelated) {
    throw new InvalidOperationException("broken wrapper", unrelated);
}
```

Actual output:

```text
verify-harness-diagnostics: 0 file(s) declare an STA thread = 1 wrapping + 0 plain rethrow(s) + 0 original TCS handoff(s) + 0 whose subject is an exception.
MIXED_EDI {"problems": [], "wrapped_files": 1, "must_reject": true}
```

This reproducer is lexical source input, not a compiled App test. It contains
no STA creation, so the existing multiple-STA rule cannot catch it. `Broken`
demotes an assertion supplied as `unrelated`, and the healthy method masks it.
The new two EDI fixtures only check matching versus unrelated Capture identifiers;
neither exercises the required same-file wrapper isolation.

**Clear predicate:** the healthy EDI path alone passes; adding the unguarded
`Broken` method in the same file fails with a finding for that wrapper. EDI
exemptions must correlate to the relevant wrapper/failure flow, including the
actual retained-array loop in StageDiagnostics; a file-wide guard is insufficient.
Add a permanent red-first mixed EDI/wrapper fixture, retain the existing negative
controls and four-file STA denominator, and return the new pin for independent
re-review. No blanket AggregateException ignore or file allowlist is acceptable.

Class → sweep → derive → prevent: DC-104/DC-118 control-width recurrence; compare
condition/capture identity, method boundary and each exempted wrapper; a local
guard cannot justify a global exemption; require the executed mixed fixture as
a permanent regression before clearance. Conductor serializes the lesson entry.

## Other requested predicates

| Predicate | Independent observation |
|---|---|
| Original identity, first ordinal/category | Verified source uses ReferenceEquals and retains the first record; cap log has ordinals 1 and 3 with first categories, duplicate omitted |
| First direct assertion and original stack | Verified source selects first direct XunitException in retained order and uses EDI; original/final and guard/identity/stack mutant TRXs inspected |
| Nested aggregate preserved | Verified fallback wraps original array without Flatten; nested identity test passes final and fails the flatten mutant |
| Full failures independent of stage cap | Verified Save emits full type/message/stack from separate retained records; cap log has both >2,000-character messages, their terminal markers and stacks after omittedStages=13 |
| Assertion plus cleanup in actual helper | Verified owned-dispatcher log records body assertion and cleanup IOException with complete records; tasks RanToCompletion and dispatcher shutdown observed |
| Original catch → setter → same awaited TCS | Verified actual daemon helper at lines 432–460 forwards original local/direct catch to completed, then awaits completed.Task; scanner self-test rejects its enumerated replacement/wrong-await forms |
| Same-file healthy/bad TCS paths | Verified current self-test emits findings for mixed methods and mixed setters; method fixture also retains the multiple-STA finding |
| STA census unchanged | Verified normal scanner reports four files: two wrapping, one TCS, one exception-subject; author's raw census lists five creations with shared Sta.cs contributing two |
| EDI guard cannot mask another wrapper | **BLOCK: independent fixture above is falsely clean** |

Adversary lenses: Test Architect and SRE block on G4-R1; Language Developer
confirms the same scanner exemption defect. Distributed Systems review found no
additional changed-helper handoff issue in the bounded original-catch/TCS path.
Security inspected the exact three-file change: no new product trust boundary,
external input, privilege or source authorization behavior. Data/persistence
architecture is not triggered by test-only in-memory failure records and existing
local test log output. These are scoped review observations, not whole-product
clearance or self-clearance of the authored review document.

## Raw evidence inspected and controls executed

Author source tree was clean at the full frozen SHA when inspected. Diff is
exactly the scanner, AtlasSharedHostAdmissionTests and author proof. Reviewer ran
candidate normal gate and self-test: normal prints OK and four-file census;
self-test prints its 11 TCS / two EDI success summary. That success does not
discharge G4-R1.

Actual TRXs under `C:/Users/malla/AppData/Local/Temp/atlas-harness-diagnostics`
were parsed, including case names and failure messages, not only exit status:

| Evidence directory / TRX basename | Executed | Passed | Failed |
|---|---:|---:|---:|
| red/red | 4 | 1 | 3 |
| green/green | 4 | 4 | 0 |
| drop-assertion-guard/drop-assertion-guard | 4 | 2 | 2 |
| replace-assertion-instance/replace-assertion-instance | 4 | 2 | 2 |
| couple-failures-to-stage-cap/couple-failures-to-stage-cap | 4 | 3 | 1 |
| flatten-aggregates/flatten-aggregates | 4 | 3 | 1 |
| reset-assertion-stack/reset-assertion-stack | 4 | 3 | 1 |
| final/final | 4 | 4 | 0 |

`final/build.log` reads Build succeeded, zero warnings/errors, 2.42 seconds.
`mutation-results.json`, `stack-results.json`, and `scanner-and-census.json`
were inspected. Recorded final test exit is 0; mutant exits are 1. Reviewer did
not rebuild the unchanged compiled mutations.

Directly read generated files under the author tree:

- `artifacts/atlas-mainwindow/stage-diagnostics-cap-9374743a141040e3b1e3bd45eaf2ff09.log`
- `artifacts/atlas-mainwindow/stage-diagnostics-owned-d00ec0b6690441f8831ef620fedd174b.log`

Independent reproducer and retained output:
`C:/Users/malla/AppData/Local/Temp/atlas-g4-independent-review.py` and
`C:/Users/malla/AppData/Local/Temp/atlas-g4-independent-review.log`.
The reproducer imports the frozen scanner, creates a temporary tests directory,
runs `check`, then parses the original TRXs. The full minimal fixture and actual
false-clean result are preserved above so this finding does not depend on temp
file retention.

Remaining: G4-R1 author repair/re-review, advanced-main integrated qualification
and GHCP publication. Review completion does not complete the user's five-gate
integration objective. The reviewer releases its exact lease and ends its unit
after committing this receipt; no other lane is claimed.
