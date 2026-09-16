---
id: proof-atlas-assembled-preservation-review
title: "Independent preservation review of the assembled Atlas candidate"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [atlas, assembly, independent-review, preservation]
links:
  - { to: proof-atlas-five-gates, rel: verifies }
  - { to: proof-d0-atlas-capture-review, rel: depends-on }
review-by: 2026-12-15
summary: "CLEAR for source and append-only-history preservation at frozen candidate e6aed085; runtime qualification and main acceptance remain separate."
---

# Disposition

**CLEAR — Verified for preservation at exact candidate
`e6aed0857749a1409e5a4d3c704ed131aeea721c`.** The reviewed repair source,
independent D0 source, merge ancestry, append-only ledgers, and the admitted
Atlas/Solution Tree menu seam are conserved. This receipt does not qualify runtime
behavior, accept a qualification run, or approve publication to main.

Reviewer/session: `codex-sol-assembly-review` /
`codex-atlas-assembled-preservation-review`. Worktree:
`C:/Projects/ai-de-review-atlas-assembled-preservation`; branch
`review/atlas-assembled-preservation`. No product source or test was authored.

## Scope and review graph

Goal: independently clear or block preservation at the frozen source assembly. Done
when actual source diffs, reviewed hashes, ancestry, append-only conservation, and the
combined menu resolution support a committed disposition. Tier T2; fan-out zero.

The bounded graph was: freeze exact candidate -> compare source and hashes -> inspect
merge parents and admitted conflict -> compare ledger fingerprints -> disconfirm
unexpected source/history loss -> write this receipt. The source/hash and ancestry
checks were independent inputs; ledger conservation depended on the frozen candidate;
the final disposition depended on all three. No loop or retry-until-green path existed.

## Frozen source and ancestry

**Verified.** Candidate `e6aed085` has sole parent
`f90bdce143812d86008f14a0aa806ff222bf3b17`, the staged unqualified D0 merge.
That merge has exact parents
`4dc9a7995d57f31aee2c4bdffbd3aff469141719` and
`e5ee30f30330010395d430c7b43474ad441de550`.

**Verified.** From pre-D0 assembly
`fe95be86b08c34f90dd8236819e4216dd8316e9b` to the candidate, the complete
`src/tools/tests` name-status delta is one modified file:
`tests/AiDe.App.Tests/SolutionTreeProbeTests.cs`. Its candidate SHA-256 is
`5611216eca8ebcf4f207a853938991886a9401596908730a63afe0973dd4a0de`.
The file is byte-identical to both cleared D0 tip `e5ee30f3` and author
`c6868e60`.

**Verified.** Git ancestry checks returned zero for the four repair pins
`23151302`, `47f5f54c`, `a72eb357`, `301bc67a`; review pins
`622ed908`, `9bd65703`; current-main input `bcf4959b`; pre-D0 assembly
`fe95be86`; and final D0 review `e5ee30f3`.

Thirteen of the fourteen earlier reviewed-file SHA-256 entries match the candidate.
The sole difference is `docs/proof/atlas-core-gate-repair.md`; direct diff shows only
`type: proof` changing to `type: doc`. Its current SHA-256 is
`d42814704484fb485ed0e3f29d0f49e7629cc7c26bc873e51fcc0b1cb4c58b8f`.
This receipt therefore does not reuse the older manifest as a claim that all fourteen
current hashes match.

## Admitted menu seam

**Verified by parent and candidate source comparison.** Atlas parent `22dc5b12^1`
contains the `code-atlas` surface and “Show code Atlas” expectation. Main parent
`22dc5b12^2` contains the `solution-tree` surface/lifecycle and “Show solution
tree” expectation. Candidate `e6aed085` contains both registrations, both shell
paths, both kind rows, and both menu expectations. The architecture lens finds the
manual resolution consistent with both parent intents at this source seam.

The expected-count file at the candidate retains the higher floors:
`AiDe.App.Tests=1134`, `AiDe.Core.Tests=3161`,
`AiDe.Core.Tests.portable=2817`, and
`AiDe.Core.Tests.nonportable=344`. No exact conflict-marker line is present under
`src`, `tools`, or `tests`.

This is source-structure evidence. No rendered/native menu behavior is inferred.

## Append-only conservation

**Verified with the repository's actual `coord.entry_fingerprint` contract.** At the
frozen candidate, the audit ledger has 931 rows and the change ledger has 177 rows.
For each repair pin, both review pins, and final D0 tip `e5ee30f3`, the comparison
found zero missing parent fingerprints in both ledgers. Parent audit row counts were
829–921 and parent change row counts were 165–172; the assembled counts remained
931/177. This compares content fingerprints, not ids alone.

## Lens outcomes

- **Test Architect / history integrity: CLEAR.** The only post-`fe95be86`
  source/test delta is the independently cleared D0 probe file with the reviewed hash;
  every checked append-only parent fingerprint is present.
- **Architecture: CLEAR for the admitted menu seam.** The candidate retains both
  parent registrations and expectations, with no unresolved source marker.
- **Simplifier: CLEAR for this bounded review.** Deterministic git, SHA-256, and
  fingerprint comparisons were sufficient; no build or duplicate qualification run
  was introduced.

## Limits and residual risk

This review does not establish runtime or rendered behavior, Release output, native
receipt validity, test execution, qualification-wrapper correctness, or main-branch
acceptance. Those require the separately scheduled canonical qualification and
foreground publication decision. The candidate was frozen throughout this review;
later generated, audit, or recount commits are outside this receipt.

## Cost and closure

Actual review cost: 11/8 substantive tool boundaries, zero delegates, one evidence
write, and 384 measured seconds from the official audit start marker to
final receipt correction. The ninth boundary corrected a preflight defect: the first
conflict-marker regex matched long equals-sign separators in vendor licenses and a
tool banner. That failed before any lease or repository write; the corrected check
uses exact marker-line grammar.

The tenth boundary selected the rejected official-audit outcome token `Completed`
instead of the documented `success`; no closing entry or commit was made. The
eleventh boundary corrected a duration-bearing patch against the file's observed
text after a literal-seconds mismatch. Both extra failures are recorded as
review-process defects; neither changed the candidate or preservation evidence.

No build, test, shown/STA/native/full/gates suite, source fix, join, push, cleanup, or
central-ledger hand edit occurred.

Evidence artifact: `docs/proof/atlas-assembled-preservation-review.md`.
