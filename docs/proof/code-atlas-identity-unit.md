---
id: proof-code-atlas-identity-unit
title: "Code Atlas first Core unit - identity and source binding"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "atlas-e0-identity-unit"
tags: [code-atlas, proof-pack, identity, source-binding]
links:
  - { to: note-atlas-candidate-first-unit, rel: depends-on }
  - { to: proof-code-atlas-source-safety-join, rel: relates-to }
  - { to: architecture-code-atlas-proposed, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Four new Core/test files implement immutable scoped identity and six-component source-binding
  comparison. Independent candidate and joined runs executed 38 tests. Records semantic
  counterexamples, targeted Data clearance and budget failures without claiming native E0 delivery.
---

# Delivered unit and provenance

Exactly four files were added:

- `src/AiDe.Core/Understanding/AtlasIdentity.cs`
- `src/AiDe.Core/Understanding/AtlasSourceBinding.cs`
- `tests/AiDe.Core.Tests/Understanding/AtlasIdentityTests.cs`
- `tests/AiDe.Core.Tests/Understanding/AtlasSourceBindingTests.cs`

Candidate commits `723b4c60`, `4aa4791d`, `b158b172` were joined by the Conductor as
`b1c5702d`, `1ed6b458`, `a5257f0c`. The worker tree was based on main `4d396411`; only these
commits were cherry-picked, not their newer main ancestry. No existing product/project/package,
reader, persistence, IPC or UI file was changed.

The reach is deliberately limited: compiler-symbol factory -> immutable encoded identity ->
ordinal equality/hash behavior; issued binding components -> validated immutable binding ->
all-component comparison. These are pure values, not an issuer of access or decision authority.
Store/wire/renderer/real-workspace consumers are subsequent work, not implied by unit success.

## Claims, evidence and limits

| Claim | Evidence / oracle | Red or disconfirmation | Confidence / residual |
|---|---|---|---|
| Scope, project and TFM distinguish logical symbols; overloads use compiler identities | Actual Roslyn symbols in `AtlasIdentityTests`; separate context components and overload parameter types | Remove qualification or use the same identity for both overloads and their inequality assertions fail | Verified by independent targeted execution; not a project-loader proof |
| Opaque Unicode and framed encoding are preserved | NFC/NFD, delimiters, backslash, non-BMP, case/culture and invalid-surrogate cases | Initial implementation deliberately collapsed NFC/NFD and its old 16-case suite passed; new predicates require distinct tokens | Verified final cases; the writer's framing-mutation failure is reported evidence, not independently replayed |
| Values cannot be valid-looking empty struct defaults | Sealed immutable reference values, private construction, required-component/null and default tests | Initial structs supplied null fields and an empty `ToString` fallback; Data rejected that contract | Verified final code/tests; null remains absence, not a usable identity |
| Hash grammar is exact | Absolute `\A...\z` SHA-256 grammar; LF, CRLF, prefix/control, length, hex and algorithm cases | Conductor loaded the actual old Core assembly on CLR 10.0.11: a 72-character token with final LF was accepted; this contradicted the earlier source-review clearance | Verified corrected cases; exact valid form is `sha256:` plus 64 lowercase hex characters |
| Binding compares all six required components | Five opaque tokens plus one canonical hash; each mismatch changes one component while all others remain fixed | Policy-omission mutation reported failing by the writer; invalid hashes are rejected before construction | Verified final mismatch cases; no file-read or authorization claim |
| Source constructor and partial identities are supported within the admitted subset | Instance/static constructor inequality and actual partial definition/implementation equality cases | Retained intermediate boundary TRX contains the failing partial assertion; final run passes it | Verified final execution; fields/events and broader method kinds remain staged |
| Logical partial identity survives adding an implementation | Conductor invoked the actual compiler/test fixture and Core factory for a declaration-only partial method and the same method with implementation | Both compiler IDs were `M:Demo.Widget.M`; the final logical identities compared equal | Verified additional check; no source-range or body association proof |

The initial absent-types build failure is **not** recorded as semantic proof. The writer reports
eight semantic failing assertions in the first correction and four in the final boundary pass.
Those full earlier outputs were not independently retained at join. Concrete independently
observed counterexamples above and the retained partial failure are distinguished from that report.

## Actual runs

All targeted runs used:

```powershell
dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~AiDe.Core.Tests.Understanding" --logger "trx;LogFileName=<run>.trx" --results-directory <session-evidence-directory>
```

| Revision / run | Observed result |
|---|---|
| Candidate `723b4c60`, independent initial baseline | 16 executed, 16 passed; not contract acceptance |
| Candidate `4aa4791d`, independent repair baseline | 32 executed, 32 passed; subsequently disconfirmed by the LF counterexample |
| Candidate `b158b172`, independent boundary replay | 38 executed, 38 passed, zero failed/skipped |
| Conductor `a5257f0c`, joined replay | 38 executed, 38 passed; build reported zero warnings/errors |

The first joined `--no-restore` invocation returned exit 0 but produced **no test result**.
The Conductor read the intended TRX path and found it absent; `obj/project.assets.json` was
also absent. Restore from the local NuGet package source with auditing disabled supplied the
missing assets. The repeated run then produced the actual 38-test TRX. The empty invocation is
not included as a passing run.

Retained worker files under `tests/AiDe.Core.Tests/TestResults` include:

- `atlas-understanding-phaseb.trx`: 29/29 at an intermediate point.
- `atlas-understanding-phaseb-final.trx`: 32/32 before the final boundary cases.
- `atlas-understanding-boundary-close.trx`: 38 executed, 37 passed, one failed partial-identity case.
- `atlas-understanding-boundary-close-final.trx`: 38/38.

Independent TRX files were saved in this session's `files/atlas-identity-initial`,
`atlas-identity-repaired`, `atlas-identity-boundary`, and `atlas-identity-joined` directories.
The run table and observed failure descriptions here are the durable evidence summary.

## Gate and process disposition

Data reviewer `91c51d36-21fc-4c00-8cf4-57fa50a1cb00` cleared the remaining pure-value-unit
BLOCK at `b158b172`, conditional on the parent's independent 38-case rerun. That rerun and the
joined rerun were observed. This clears the **unit** gate only.

The writer skipped its first required mini-contract checkpoint. The Conductor held the generated
code, obtained the Data ruling, read a separate response-only mini-contract, and then released
the correction phase. The last six-call boundary allocation was exceeded: the writer reports
ten calls for actual partial-symbol diagnosis, cumulative 46/60. Wrapper/leaf counts are
reported counts, not an independently metered token bill. The overrun does not create permission
for another feature.

No native tree/source journey, source-range alignment, physical inventory, store replay, wire
compatibility, real workspace, main integration or E0/programme completion is claimed. The full
E0 proposed design's durable ID codec must be reconciled with this pure value representation
before a second identity producer or a persisted/wire format is installed.
