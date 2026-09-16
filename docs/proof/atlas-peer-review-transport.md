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

The final r4 receipt is byte-for-byte blob
`f37e23be9148415e09b3b01a05c5fe5277443a35`. Its verdict remains **CHANGES REQUIRED**.
The final r3 receipt is byte-for-byte blob
`fdb12a93f55701bc6518df9009844ac47672fdaf`. Its verdict remains **CLEAR** for the exact
non-consuming document boundary.

## Audit conservation

Before this carrier's own closure records, `entry_fingerprint` establishes this exact union:

- 960 carrier-base payloads;
- five selected independent-review payloads, with their original IDs unchanged;
- exactly two historical producer payloads preserved by the installed append-only merge driver.

The Owner explicitly admitted only these incidental provenance rows:

- `al-01M2KMBV5ZEQPEFFD7708W28HA` — `owner-d1-admission`;
- `al-01M2NHXVG85NWR87JK2WJW4PAQ` — `specify-d1-entry-points`.

The admitted pre-closure union is therefore `960 + 5 + 2 = 967`, with no other missing or
extra payload. The exception preserves history only. It does not import broader Grok ancestry or
convey current admission, ownership, ACK, native, product, root, or main authority. The carrier's
own blocked-unit audit and finalization decision/audit are accounted for separately.

## Coordination correction

At `1789580280.013657` (`2026-09-16T17:38:00Z`), a read-only `coord check` ran without
`AGENT_SESSION` or `AGENT_NAME`. It appended the watcher decision
`COORD-NOT-CHECKED-IDENTITY` under `anon`; the original output is preserved at
`artifacts/atlas-peer-review-transport/not-checked.txt`. That command did not author the proof,
but it did append the anonymous coordination event. Later identified checks do not retroactively
qualify it. Every proof-writing shell set both identity variables and acquired an exact proof lease.

## Cost and closure

The initial r4 unit used nine shell batches against eight. The r3 extension and conservation
diagnosis used seven against four. The finalization has its own measured audit marker and bounded
budget. Tokens and spend are not exposed. Official derived regeneration follows all audit and
decision records, under short exact leases for the three site figures.
