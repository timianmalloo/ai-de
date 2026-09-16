---
id: proof-codex-d1-r3-consumer-review
title: "Independent D1 r3 consumer boundary review"
type: doc
status: accepted
owner: "@timianmalloo"
links:
  - { to: note-d1-codex-entry-point-handshake-r3, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
---

# CLEAR: D1 r3 non-consuming consumer boundary

**Verified by independent document and Git-object readback.** The exact proposal
`docs/notes/d1-codex-entry-point-handshake-r3.md` at commit
`17cd8317442f948ca6e0846f02cc06ef9f3a9673`, blob
`e448383a90bb1ed962c7405af70e16d8cca09fa3`, satisfies all five corrections in
Owner-approved request `req-01M2KSD1JQG5WEBJVSDNYEH54J`. This clears the document
for consumer coordinator ACK. It is not itself that peer ACK or a freeze record.

## Goal, boundary and execution graph

Goal: independently clear or block this exact non-consuming document boundary.
Done when five corrections and four pinned consumers are checked and this receipt is committed.
Tier T2; fan-out 0. No implementation, policy redesign, ownership assignment, API/source
freeze, tests, builds, GUI, publication or changes to the proposal/readers are admitted.
The constrained surfaces are Core identity and Restore authority; D1 listing and disabled
Open Sequence; E1 independent method selection, observation/page coverage and Source/Restore;
E2 non-consumption; coordinator bilateral acceptance. No rendered/runtime claim is made.

The optimize-graph pass retained three dependency nodes: ground exact Owner/proposal/pins
-> adversarial clause comparison -> lease, receipt, audit, commit and release. No loop or
delegation is needed. Test Architect's oracle is any retained normative r2 assumption,
missing correction or mismatched consumer object; architecture/security's oracle is any
new minting/navigation authority or mapper assignment. Simplifier rejects adjacent policy
or implementation work. All these floors remain; no new plan file is authorized.

## Five correction checks

| Correction | Observed r3 clause | Verdict |
|---|---|---|
| 1. Authority | Section (1) assigns observation, occurrence, projection, source-token and Restore to the Core-authorized E1 service. D1 does not mint these; labels, paths and positions are not identities. | CLEAR |
| 2. Mapper | Section (2) says UNASSIGNED/unavailable, requires later admission, and explicitly removes E1 ownership and type-to-0..N/member-to-0..1 promises. | CLEAR |
| 3. Cross-open | Section (3) disables Open Sequence with `mapping-unavailable`; node_id/classification alone are never E1 input; E1 independently selects valid Core method observations and does not consume D1 rows. | CLEAR |
| 4. Coverage | Section (4) deletes truncated-listing coupling and keeps D1 listing coverage distinct from E1 observation/page coverage, including valid partial E1 receipts. | CLEAR |
| 5. Other seams | Section (5) distinguishes NodeContent/codeviewer Source from occurrence-bound Source and perspective Back from Core Restore; no shared Back stack; E2 N/A; no shared signature, caps or receipt schema frozen. | CLEAR |

I read the complete r3 document, not only keyword matches. Its quoted r2 minting,
cardinality and truncation claims are explicitly withdrawn, not operative requirements.
Its final exclusions leave listing grain, query signature, caps, SurfaceKind and mapping
DTO undecided. Listing remains within D1's existing admission. The proposed frontmatter
and pending-ACK text describe this historical proposal snapshot; later bilateral
same-blob acceptance must be recorded separately. No rewording is required for that history.

## Exact consumer object resolutions

| Printed reference | Verified full commit | Exact path |
|---|---|---|
| 0bdd16d7 | 0bdd16d7e429f8ed00a73e7c664eb3ced520fae8 | docs/design/atlas-behavior-views.md |
| 1ab5d9e9 | 1ab5d9e985fd86ef75adb8d2a11c72fe05b530cc | spikes/atlas-behavior-contract/RESULT.md |
| 27642bf8 | 27642bf89b687f1e78bc33f11f977cd753baf7cf | docs/design/atlas-architecture-views.md |
| a9d86fc1 | a9d86fc1350ac4c74d5d8ff39e9f39685b890f14 | spikes/atlas-architecture-contract/RESULT.md |

Git resolved each abbreviation to the supplied full commit, and each path to a nonempty
blob. Short formatting is not a semantic discrepancy and does not require another revision.

The pinned E1 design sections 4/5 require Core-issued method/observation identity,
Core authority, continuation and Restore receipts. A coherent published capped observation
is restorable only with its exact Core receipt; canceled fragments are not. Section 5.2
separates outcome, observation coverage and page completion. Source and Back remain bound
to the accepted occurrence/receipt. These clauses support r3's authority and coverage split.
The E1 spike's structural extension explicitly has no Core token or receipt-minting path
(lines 188-205); its synthetic graph identities are not production authority. Its limits
retain real-workspace authority, production receipts and cap truncation as unqualified.

The E2 design section 5.2 describes a future logical E1/E2 envelope with admitted scope,
epoch, manifest, opaque observation/source binding and Core-issued Restore. This does
not establish a D1 consuming seam. Its evidence section rejects carrier-minted authority;
source provenance comes from Core binding and missing authority remains unestablished.
The E2 spike's authority/scope and carrier sections likewise disclaim an authority resolver,
production authorization and real Atlas token minting. None supplies a D1 mapper. E2's
existing E1/Core relationship is compatible with D1-to-E2 N/A; r3 freezes no new schema.

## Evidence, gates and residuals

Author-independent identity/authority and Test Architect review: **CLEAR**, by observed
clauses and exact object resolutions. Architecture/Security: **CLEAR** for this boundary;
no inferred minting, token substitution, access grant or ownership assignment. Simplifier:
**CLEAR**, one document receipt without policy expansion. SRE/runtime/native qualification
is outside this document review and is not a prerequisite for its ACK.

Raw local evidence: `.artifacts/d1-review/excerpts.txt`, `pins.json`, `prompt.txt`, and
`close.py`. Two initial broad reads exceeded output limits; bounded clause readback repaired
the evidence visibility before the verdict. This was an output-sizing defect, not a product
finding; the corrective control was narrow line-numbered extracts and explicit object checks.
No tests/builds ran. `coord doctor` observed effective registry and merge drivers but returned
1 for six pre-existing owed derived artifacts. No regeneration success is claimed; Conductor
retains that separate integration obligation. Derived audit output and raw scratch are retained.

Planned/actual: six tool boundaries, eight-minute ceiling, checkpoint after boundary four;
actual six boundaries, with measured duration in the closing audit. The exact proof lease is
held only for the atomic UTF-8 write/audit/commit and released in finally; liveness records
the observed completion and release. AIDE_SESSION and AIDE_CONTRACT_LOG are absent, so no
watcher episode registration or capture delivery is claimed. The official audit names this proof.

Remaining: consumer coordinator `codex-atlas-five-gates-integration` must ACK the exact
blob above, and both peer ACKs must govern the freeze record. This reviewer cannot supply
coordinator authority. No unresolved document discrepancy was found. Implementation,
runtime behavior, native qualification and publication remain unverified by this review.
