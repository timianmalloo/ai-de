---
id: investigation-code-atlas-legacy-peer-startup
title: "Legacy peer startup: runtime path depth exceeds the Git fixture boundary"
type: doc
status: draft
owner: "@timianmalloo"
tags: [code-atlas, investigation, fixture, paths, compatibility]
links:
  - { to: proof-code-atlas-e1-core-metadata, rel: relates-to }
  - { to: design-code-atlas-e1-static-views, rel: depends-on }
review-by: 2026-09-22
summary: >
  The prepared legacy server fails Git initialization under a deep execution
  path. Identical canonical binaries pass from a shorter owned path. The
  investigation proposes bounded execution staging without changing the legacy
  implementation or approving the fixture hash pair.
---

# Verified startup cause within the paired fixture runs

The legacy fixture constructs its repository beneath `AppContext.BaseDirectory`.
The materialized peer was launched directly from a deep source/build output
directory. Git initialization then failed at its `.git/objects/pack` directory
with **Filename too long**, before the legacy server could send its workspace
identity. The parent consequently saw EOF.

This is a test/deployment-path defect, not a demonstrated metadata-protocol or
canonical-binary defect. No source or system configuration was changed during
the discriminator runs. Repair and normal fixture-hash approval remain with Owner.

## Goal, expected behavior and boundary

The actual canonical legacy server must start, report an owned workspace and
participate in SELECT/RESTORE compatibility. Fixture bytes must remain pinned;
failure evidence must survive cleanup. No global Git/Windows configuration,
legacy Core change, skipped test or hash relaxation is an acceptable shortcut.

The investigation covers the one materialized legacy-peer startup path and its
shared helper. It does not claim arbitrary deep Windows paths are supported or
that the current Core metadata candidate has been accepted.

## Timeline and observations

| Step | Observed result |
|---|---|
| Owner 82 canonical preparation | Worker reports equal Core, peer and PDB pairs across `canonical-e` and `canonical-f`, with accurate baseline/patch provenance |
| Ordinary compatibility attempt | 34 executed: 33 passed, legacy-server/current-client failed with EOF while awaiting workspace identity |
| Original failed child logs | Only VSTest startup and xUnit failure indication; no child TRX |
| Standalone `canonical-e` peer | 1 passed; this ruled out universal failure of the canonical peer |
| Standalone exact previously failing prepared peer | 1 failed; complete TRX exposes Git `Filename too long` during `AtlasRuntimeFixture.StartAsync` |
| Same prepared runtime closure copied to shorter owned execution path | Exact Core/peer hashes preserved; standalone peer passes 1/1 |

The original parent harness immediately kills a still-running child in `finally`
after EOF. That source shape can suppress final logger output; its contribution
to the missing original TRX is **Inferred**, not a separately timed proof.
The full standalone failure is the retained cause evidence.

## Necessary/sufficient discriminator, scoped honestly

| Case | Peer path length | Core / peer bytes | Result |
|---|---:|---|---|
| Exact failed preparation | 181 characters | Canonical pair below | Git initialization fails before server workspace publication |
| Shorter owned copy of those same prepared files | 85 characters | Hashes identical | Legacy fixture/server/client journey passes |

Canonical Core:
`ED7F23CDA7DBB749511D1D879CF0886543E4E76680009749398D19CA9E445EC5`.
Canonical peer tests:
`8E95C84ED6887572B62F35664222A771E0EF283980E430B69F64AFEA5BC583BA`.

The long execution placement reproduces the failure; removing that placement
while retaining the copied runtime closure removes it. This establishes the
cause for the paired controlled fixture runs. It does not establish a universal
maximum supported path length or complete mixed-version success.

### Confidence ledger

| Claim | Evidence | Label |
|---|---|---|
| Failure occurs in Git setup, before workspace publication | Exact failed peer's standalone TRX and frozen fixture source | Verified |
| Canonical binary corruption is necessary for the failure | Disconfirmed by identical-byte shorter-path success | Rejected for this failure |
| Metadata negotiation caused the startup error | Failure precedes server publication; standalone old-peer setup reproduces it | Rejected as the immediate cause |
| Runtime placement drives the observed outcome | Long-path failure versus hash-identical shorter execution success | Verified for the paired cases |
| Parent early kill suppressed the original complete failure report | Source `finally` kill plus original sparse logs/no TRX | Inferred; timing not instrumented |
| Canonical fixture is fully approved | One mixed role still failed in the original run; hostile-output gates remain | Not established |

## Source path and class sweep

The frozen baseline fixture source at
`canonical-e/tests/AiDe.Core.Tests/Understanding/AtlasProductionAdmissionTests.cs`
lines 425-478 derives `.artifacts/atlas-runtime/<guid>/repository` from
`AppContext.BaseDirectory`, then awaits Git initialization before opening Core.
The current compatibility harness launches from
`prepared-<guid>/tests/AiDe.Core.Tests/bin/Debug/net10.0`.
Both components are locally valid; their concatenated path is not bounded.

**Class:** a materialized executable's deep build path silently becomes the
runtime workspace root for a path-limited dependency.

**Why it survives:** compilation and binary hashes are correct; client-only
operation does not create the server's repository; the parent's EOF can hide
the child's precise filesystem error.

| Related path | Disposition |
|---|---|
| Legacy-server role using the deep prepared peer | Confirmed failing instance |
| Same prepared peer's standalone server/client fixture | Confirmed reproduction |
| Legacy-client role using the same delivery mechanism | Existing pass; does not create that legacy server repository on this path |
| Current fixture helpers based on the executable directory | Same source shape; arbitrary deeper-checkout failures not independently tested |

The materializer and current compatibility test were searched for `simplify:`
and `assume:` markers; none were found in those two delivery files. No unrelated
fixture subsystem was modified. The named class remains pending the existing
Conductor defect-ID reconciliation; no numeric ID is guessed.

## Proposed systemic repair and rollback

Separate **build/provenance staging** from **bounded execution staging**. After
preparation and hash validation, run the genuine peer from a short, owned runtime
directory whose path budget includes the frozen fixture's descendants. Check
the available budget before launch and fail explicitly if it cannot be met.
Copy only the required runtime closure, verify the same candidate hashes and
preserve ownership/cleanup; do not edit the frozen implementation.

Keep diagnostic collection independent of the workspace-control stream. On
startup failure, permit bounded child completion/log/TRX flushing before forced
termination, while retaining the original failure and enforcing the deadline.

Rollback is confined to fixture materialization/test wiring. The historical
fixture pair, canonical candidate and all failure receipts remain available.
No production or global-machine setting is changed.

| Phase | Scope: code + proof | Eliminated failure / oracle | Dependency |
|---|---|---|---|
| 1 | Existing materializer and test preparation wiring; bounded owned execution path | Identical canonical peer starts despite a deep build location; hash equality and cleanup asserted | Owner approval |
| 2 | Existing child-process failure reporting; negative startup case | Cause reaches retained diagnostics rather than EOF-only output; deadline/owned termination preserved | Phase 1 |
| 3 | Both genuine mixed-version roles, full selected compatibility, hostile archive/output controls | No role silently skipped; no path escape, leftover owned process or arbitrary hash accepted | Phases 1-2 |
| 4 | Owner canonical-pair decision and delivery commit | Normal expected pair approved only after reproducibility/provenance and real compatibility | Phase 3 |

GATE investigation - paired execution/source evidence recorded; repair proposal
returned to Owner. No implementation or normal-pair approval is self-cleared.

## Retained evidence and residual risk

Parent session files `atlas-legacy-startup-diagnostic/` contain:
`standalone-canonical-peer.trx`, `standalone-failed-preparation.trx`,
`standalone-identical-short-path.trx` and `identical-bytes-short-path.json`.
The original child logs remain in
`.artifacts/atlas-legacy-qualification/peer-runs/fe53feb37e014d62864c1dc06c4f1180`.
The owned diagnostic copy `.artifacts/pdx-498eec33` is retained for review.

Remaining: actual repaired mixed-version replay, archive/output safety controls,
bounded error reporting, independent delivery reviews and separate Owner hash
approval. Evidence that the identical peer fails from the measured shorter path
would reopen the path-only diagnosis.
