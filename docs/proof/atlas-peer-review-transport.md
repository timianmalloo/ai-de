---
id: proof-atlas-peer-review-transport
title: "Atlas independent review receipt transport"
type: doc
status: completed
owner: "@timianmalloo"
tags: [proof, atlas, transport, audit]
links:
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "Byte-preserving carrier transport of the independent D1 r3 and r4 receipts, including the Owner-approved two-row historical audit provenance exception."
---

# Atlas independent review receipt transport

## Boundary

This carrier transports independent review artifacts only. It grants no ownership, peer ACK,
native or product admission, root-candidate change, main integration, or publication authority.
The source, tests, and tools remain identical to carrier base
`b2127168721bd0db88be57907b01349890c787da`.

## Commit and byte mapping

| Source commit | Carrier commit | Artifact effect |
|---|---|---|
| `7e205f5bb8d3ef804a2dd7d30c981838cffd991b` | `2c556f37992de21123c5dbc3456d470872ccea91` | Add r4 independent review |
| `f0778039906fe34632f048fe6c5ed7e5bb18b0f5` | `e7b9e0da8410d3f5b8499783a67b46edfd859688` | Correct r4 graph metadata |
| `394d1ff0a2105c5cdb647d2da5151513e6e8e105` | `249d68369f5db7b31103bc18041e91ca3ffee1e4` | Add r3 independent review |
| `b9e47af85d3c7819015b8c95ea76c2e457592e14` | `a9ce139b42cc4071738515eb1d8c0b8aa64e325a` | Add required r3 summary metadata |
| `fc60fd35920576899386fb0f710456c6152814e8` | `d023cb04eac220737d7405ebec50bae4018fb465` | Replace the carrier-dangling r3 graph link with the verified `session-contracts` relation |

The final r4 receipt is byte-for-byte blob
`f37e23be9148415e09b3b01a05c5fe5277443a35`. Its verdict remains **CHANGES REQUIRED**.
The final r3 receipt is byte-for-byte blob
`f9bac553bc0fb84dfe9a78805a9302874cbe50fa`. Its verdict remains **CLEAR** for the exact
non-consuming document boundary.

## Audit conservation

Before this carrier's own closure records, `entry_fingerprint` establishes this exact union:

- 960 carrier-base payloads;
- six selected independent-review and metadata-correction payloads, with their original IDs unchanged;
- exactly two historical producer payloads preserved by the installed append-only merge driver.

The Owner explicitly admitted only these incidental provenance rows:

- `al-01M2KMBV5ZEQPEFFD7708W28HA` — `owner-d1-admission`;
- `al-01M2NHXVG85NWR87JK2WJW4PAQ` — `specify-d1-entry-points`.

The admitted imported union is therefore `960 + 6 + 2 = 968`, with no other missing or
extra payload. The exception preserves history only. It does not import broader Grok ancestry or
convey current admission, ownership, ACK, native, product, root, or main authority. The carrier's
own blocked-unit audit, premature-success correction, decision and finalization audits are
accounted for separately. The sixth selected row is metadata-author audit
`al-01M2NNR605G854MM6NDT3AVKB8`; it supersedes the `b9e47af8` metadata pin without changing the
review body, verdict or external citations.

## Coordination correction

At `1789580280.013657` (`2026-09-16T17:38:00Z`), a read-only `coord check` ran without
`AGENT_SESSION` or `AGENT_NAME`. It appended the watcher decision
`COORD-NOT-CHECKED-IDENTITY` under `anon`; the original output is preserved at
`artifacts/atlas-peer-review-transport/not-checked.txt`. That command did not author the proof,
but it did append the anonymous coordination event. Later identified checks do not retroactively
qualify it. Every proof-writing shell set both identity variables and acquired an exact proof lease.
The first full graph failure is preserved at
`artifacts/atlas-peer-review-transport/docs-graph-validate-failed.txt`.

## Inspected outcomes and closure boundary

The initial r4 unit used nine shell batches against eight. The r3 extension and conservation
diagnosis used seven against four. Finalization exceeded its five-call budget while correcting
audit CLI signal syntax and locating the repository's actual ignored evidence root. Tokens and
spend are not exposed.

The first official regeneration completed with every built-in derived gate green: 349 API files,
35 documentation pages, 14 site figures checked, 7 rewritten, 969 audit entries, 182 change entries
and 598 graph entries. The separate full graph validator then found one defect: the byte-locked r3
receipt linked to `note-d1-codex-entry-point-handshake-r3`, which was absent from the carrier. A
truthful partial audit superseded the earlier success record. A WIP checkpoint at
`4dace27b63ca525863bef77734a21a4b9aa7df8e` preserved the audit, decision, receipt and regenerated
outputs without claiming the gate clear. Its regeneration reported 970 audit entries, 182 change
entries and three rewritten site figures.

The admitted source-side correction was then transported as `d023cb04eac220737d7405ebec50bae4018fb465`.
It changes only the portable graph relation and its own audit row; the review body and verdict are
unchanged. Before the final carrier audit, the register contained exactly 960 base, six selected,
two admitted incidental and three carrier audit payloads: 971 total, with no missing or extra
fingerprints. The final carrier audit brought the observed total to 972.

The post-audit official regeneration completed with every built-in gate green: 349 API files,
35 documentation pages, 14 site figures checked, 3 rewritten, 972 audit entries, 182 change entries
and 598 graph entries. The formerly failing full graph validator then returned zero defects, zero
orphans and zero index drift; its 74 review suggestions are non-failing existing flags. The audit
verifier accepted all 1,154 audit/change entries with no duplicate IDs. Exact proof blobs,
source/src/tests/tools identity, the authorized payload union and the narrow path manifest were
checked before the containing closure commit.

Completed in this carrier: byte-preserving r3/r4 review transport, exact audit provenance
accounting, anonymous-check disposition, and portable graph metadata. Still unperformed: the
official conductor join, root-candidate mutation, native qualification, main integration and
publication. The worktree is retained after closure so the conductor can perform the later
official join from the committed evidence; cleanup would remove that reviewable input.
