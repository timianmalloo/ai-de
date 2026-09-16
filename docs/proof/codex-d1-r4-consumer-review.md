---
id: proof-codex-d1-r4-consumer-review
title: "Independent consumer boundary review of D1 mapper r4"
type: proof
status: completed
owner: "@timianmalloo"
tags: [proof, atlas, d1, contract-review]
links:
  - { to: note-d1-codex-entry-point-handshake-r4, rel: verifies }
  - { to: note-d1-codex-entry-point-handshake-r3, rel: relates-to }
review-by: 2026-12-16
summary: "CHANGES REQUIRED: Grok may author the proposed mapper contract; consumer ACK cannot establish an existing E1 identity API, Core authority, or live activation."
---

# Independent D1 r4 consumer review

## Verdict

**CHANGES REQUIRED.** This is independent review advice to the Codex conductor and Owner, not a consumer ACK, producer ACK, ownership ruling, implementation admission, or hard-veto self-clearance.

**Verified:** the reviewed file is `docs/notes/d1-codex-entry-point-handshake-r4.md`, exact blob `a8c05bc77451b0438ebea24eaad9b15db6c99601`, introduced at `173aa5a4ad245fda92bcc0bdadafac6ba9f0c8e4`. Its identity and activation clauses are not ready for ACK as written. Grok's role as author/producer of the mapping-contract proposal is separable from those clauses. This review does not ask E1 to own the mapper and does not claim Codex has assigned itself that job.

## Goal and execution contract

Goal: independently determine whether Codex can ACK the exact r4 blob honestly, or provide the smallest numbered deltas. Done when this receipt identifies exact clauses, evidence pins, and falsifiable acceptance conditions. Not in scope: implementation, builds, GUI, changes to contracts/source, peer communication, or a new ownership grant. Tier T1; fan-out cap 0. Surface list: D1 listing identity -> proposed mapper -> Core authorization and observation context -> consumer invocation -> Sequence availability/ambiguity; Source/Restore and coverage are boundary constraints.

The bounded optimize-graph pass uses three dependent nodes: inspect exact proposal and authority/source pins -> adversarial contract review -> receipt/audit/commit/release. Read oracles are exact blobs and signatures; a build or GUI run cannot establish acceptance of a document. No test result is claimed. The parent owns broader programme planning and coordination. The delegated one-receipt scope takes precedence over the forensic workflow's general repository-reconstruction artifacts.

## Evidence opened

1. r4 blob above, sections `Producer / consumer`, `Map`, and `Freeze`; r3 exact blob `e448383a90bb1ed962c7405af70e16d8cca09fa3` at the supplied frozen pin `17cd8317`. r3 explicitly leaves the mapper unassigned, prohibits live cross-open, distinguishes Source/Restore and coverage, and freezes no shared signature/schema. The bilateral freeze status is parent-supplied context; this review independently read the exact document, not the external ACK events.
2. `173aa5a4ad245fda92bcc0bdadafac6ba9f0c8e4:docs/notes/understanding-views-owner-d1-mapper.md`, `Call`: records the operator's producer assignment and keeps live Sequence disabled until r4 freezes. The note does not supply an implementation path grant or accepted E1 operation.
3. `0bdd16d7e429f8ed00a73e7c664eb3ced520fae8:docs/design/atlas-behavior-views.md`: frontmatter and heading say proposed. Sections 3.1-3.3 define a Core-issued method declaration occurrence within one manifest/source observation, a projection invariant across selected method/manifest/source/delivery context, and Core-derived occurrence identities. The envelope requires authorized scope, epoch, manifest, request sequence, opaque selection context, atomic validation and refusal. The proposed production manifest marks Core grants required. This establishes proposed semantics, not product acceptance or runtime behavior.
4. `1ab5d9e985fd86ef75adb8d2a11c72fe05b530cc:spikes/atlas-behavior-contract/RESULT.md`, opening: source-only experimental evidence, not product admission; synthetic Roslyn syntax, no production-file changes. Its author observations are not independent clearance.
5. Native-source pin `173aa5a4ad245fda92bcc0bdadafac6ba9f0c8e4`: `src/AiDe.Core/Projections/IWorkspaceQueries.cs:47`, blob `65666570a90ca5a7bfef1eb4a10187e9654a3305`, declares `InteractionAsync(string nodeId, int maxMessages, CancellationToken cancellationToken)` and explicitly describes a type-level feed. `src/AiDe.App/Workbench/WorkbenchShell.cs:1484`, blob `894d22b99670fa4efdf50aaaab0f9b492d90d483`, calls that feed with `nodeId` and 200, constructs `SequenceModel`, and invokes `ShowFor(nodeId, model)`. `src/AiDe.App/Workbench/SequenceDiagramSurface.cs:18`, blob `beac1e497b4c192f443131e23a27c3ae87702607`, describes that same feed. These observed source paths do not establish an E1 Core-method-observation opener; no deployed behavior was measured.
6. `.github/instructions/session-collaboration.instructions.md`, `Before you edit anything shared`, makes `docs/collaboration/session-contracts.md` section 2 the sole ownership register. That register at the review base assigns `src/AiDe.Core/**` and `src/AiDe.Daemon/**` to Core. A producer note is not a grant to modify those paths. Root separately checks exact branch grants; this reviewer does not infer absence of all grants.

## Minimal numbered deltas

### 1. FR-001 — remove the existing-E1 claim (Major; Verified issue)

**Clauses:** summary `E1 already selects`; Map Output `E1 already uses to open Sequence`; Producer / consumer `Sequence diagram given a Core observation ... unchanged`.

**Delta:** say these are the intended consumer semantics of the pinned **proposed** E1 design, not an existing admitted or implemented opener. Cite the design and experimental spike with their actual statuses. Explicitly distinguish the observed legacy type-level Sequence sink from that proposed E1 sink. Preserve E1's ownership of its own consumer work; do not assign E1 the D1 mapper.

**Why:** a same-blob ACK must not turn source-level design intent or a synthetic spike into product/API acceptance. **Disconfirming check:** an exact accepted operation and implementation pin implementing that Core observation selection would remove this issue; none is supplied by r4 or its cited pins.

### 2. FR-002 — freeze a valid identity boundary or defer it (Major; Verified gap)

**Clauses:** Input `listing kind + graph node_id when present`; Output `0..N Core method-observation identities`; `If E1 documents that identity ... until then ... durable facts are existing has_member / call assertions`.

**Delta:** remove the durable-facts fallback as an identity contract. Existing graph facts may inform later analysis; they are not an admitted substitute for a Core-issued, source/manifest-bound method observation. Before a consuming contract freezes, pin the accepted Core selection/reference contract, state the listing-row identity/grain including rows with no `node_id`, and state how Core issues/resolves and validates references within authorized scope, epoch and source/manifest context. D1 must not mint or infer those identities from display strings, member text, graph facts or list positions. The consumer may use an opaque reference carrying/binding this context; this review does not demand a new CLR DTO or a field-by-field transport invented in r4.

**Smallest deferral:** retain Grok as author of the proposed mapper contract; mark mapping identity/API/cardinality as unadmitted until a later exact-pin amendment. Keep mapping unavailable. `0..N` can describe intended multiplicity, but cannot promise a current mapping capability or completeness.

**Why:** method/source/manifest observation is the proposed aggregate boundary; a type graph node and member text do not establish that identity. **Disconfirming check:** accepted Core resolution semantics and exact identity/scoping pin, including unavailable/stale/unauthorized outcomes, make this boundary reviewable.

### 3. FR-003 — keep authoring authority separate from implementation authority (Major; Verified constraint)

**Clauses:** `Grok owns the mapper`; Producer / consumer table; Freeze `Then Open Sequence implementation may start`.

**Delta:** agree Grok authors/produces the mapping-contract proposal under the recorded operator direction, subject to the sole ownership register and exact branch-local grants. Do not let bilateral document ACK convey authority to edit Core paths, establish Core minting authority, accept E1 production design, or activate a native surface. Name the required grant/pin when implementation paths are known; do not invent a second ownership map in this handshake.

**Why:** producer responsibility and path authorization are different decisions. **Disconfirming check:** register/branch grants and accepted Core contract explicitly authorize the concrete production changes. This does not require transferring the mapper to E1 or blocking Grok's proposal authoring.

### 4. FR-004 — resolve ambiguity before enabling; separate freeze from activation (Major; Verified issue)

**Clauses:** Non-empty `after freeze + implement ... enables`; Ambiguous `picker vs disable vs first — not frozen here`; Freeze `ACK ... Then ... implementation may start`.

**Delta:** remove `first` as an unresolved activation option. Multiple candidates must require explicit user selection or leave Open Sequence disabled; the smallest interim rule is disable. A non-empty candidate set alone cannot authorize opening: require an accepted, validated selection and the admitted consumer capability, with stale/unavailable/refused references remaining disabled. State that this blob's ACK freezes only the agreed text. Production implementation/activation requires the accepted identity/operation contract, concrete path grants, resolved ambiguity policy and the implementation's own required evidence. Until then the frozen r3 non-consuming behavior remains effective.

**Why:** the current non-empty rule includes the unresolved multi-candidate case; `first` can silently choose, contrary to r4's own sentence. Freeze is not runtime verification. **Disconfirming check:** a jointly satisfiable activation rule for zero, one, multiple, stale and refused candidates, with the actual consumer contract and implementation evidence pinned.

## Adversarial lenses and scope control

- **Test Architect — BLOCK (Major):** r4's advertised consumer is not established by the exact proposed design/spike or the checked legacy signature. Clears when FR-001/002/004 acceptance conditions are explicit and later activation evidence is required; this document review does not prescribe unrequested tests.
- **Architecture/authority — BLOCK (Major):** preserve Core's issuing/scoping boundary and section 2/grant authority; Grok can author the mapper. Clears when FR-002/003 boundaries are explicit. No new owner assignment is made here.
- **No-Guessing — BLOCK (Major):** `already uses` and the identity fallback lack their claimed authority at these pins. The output-truncation events below are not treated as successful reads of hidden content.
- **Simplifier — PASS for the proposed correction path:** the least change is an author-role agreement with consuming identity/API and activation deferred. No adapter implementation, new DTO, legacy conversion, transfer of mapper ownership, E2 work, or blanket D1/E1 coverage coupling is necessary for this response.

Retain the useful existing exclusions: no D1 classification as E1 input; opaque identities to D1 UI; Core minting; D1 and E1 coverage remain distinct; no shared `maxMessages`; no Source/Restore equivalence; E2 N/A. These are not objections.

## Cost, limitations and handoff

Planned: five tool batches, seven minutes, one receipt, zero subagents. Review start was recorded at `2026-09-16T17:06:32Z`; closing audit measures duration. Actual: five functions batches (six underlying shell/patch operations), two truncated read batches requiring a narrower final read; no build, test or GUI calls. Model tokens/spend are not exposed and are **not recorded**. The third-batch checkpoint reported the read-output sizing defect to the parent. Control for remaining work: bounded exact-pin excerpts with explicit output limits; only visible excerpts support this receipt. This is an operational review lesson, not an authorized defect fix in another registry.

Proof capture limitation: `AIDE_SESSION` and `AIDE_CONTRACT_LOG` were absent in the checked process. No watcher identity or contract sink was invented. The committed receipt is supplied to the parent for its authorized episode-close evidence list.

The one-receipt grant excludes changes to the shared derived docs index, lesson register and production files. Existing source/contracts remain read-only. Own audit output is appended with the official writer. The exact receipt lease is released after commit. This receipt does not prove the entire product has no alternative consumer API, verify external ACK events, or replace root's grant review.

Completed: independent exact-blob review and numbered deltas. Remaining: Owner/conductor decides and communicates the consumer response; Grok revises the proposal. Next: ACK only the corrected exact blob after independent readback, or agree the author role with all consuming identity/API/activation decisions explicitly deferred.
