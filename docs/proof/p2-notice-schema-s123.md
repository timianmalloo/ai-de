---
id: proof-p2-notice-schema-s123
title: Native notice schema S1-S3 repair and evidence
type: proof
status: partial
owner: xh-p2-projection-b0d0
phase: P2
tags: [sqlite, native-notice, schema, mutation, rollback]
links:
  - rel: relates-to
    target: docs/proof/cross-harness-coordination-proof-pack.md
  - rel: implements
    target: docs/design/cross-harness-coordination.md
  - rel: relates-to
    target: docs/plans/cross-harness-coordination-phases.md
review-by: 2026-10-17
summary: Repairs six fixed-hex NUL guards and qualifies five distinct malformed-field cases, targeted constraint mutants, and populated-v8 constructor rollback. This is dormant DDL evidence, not native admission, delivery, or completed P2.
---

# S1-S3: finite schema repair

Goal: repair the verified fixed-hex NUL bypass and replace overstated structural
evidence with exact real-provider evidence. Done when baseline, candidate,
mutant and populated-v8 rollback results are pinned and committed in the P2
worktree. Tier T2; no agents; tool ceiling 32. No registrar, admission,
publisher, UI, other worktree, dependency, live database or upstream work.
Authority is the user's existing approval; this note does not invent a new
permission gate or claim an independent re-review.

## Scope and execution graph

The design's dormant storage floor requires SQLite type/null/length/key/numeric
constraints and constructor-abort atomicity. Surfaces reached: the single
`NativeRegistrationSchemaSql` constant, fresh creation/v8 migration through
actual `SqliteWatcherObservationStore.Open`, real SQLite parameterized tests,
and this evidence bundle. Model generation remains Int64. Protected writers,
native effects, projection delivery and UI readers are unchanged and inactive.

Serial graph: ground/pin (reasoning) -> test-only baseline (deterministic) ->
six DDL guards (reasoning) -> candidate/mutant/rollback execution (deterministic)
-> record/commit (deterministic). Builds share outputs, so width is one.
In this serial plan T1 = T-infinity; concurrency buys nothing. No modeled
duration is reported. The finite case list is the termination variant; budget
exhaustion reports an incomplete result, not a weaker gate. One diagnostic
fixture correction was needed. Oversized grounding outputs also consumed
avoidable calls; no additional research or agents were launched.

## Executed evidence

All paths below are under `docs/proofs/p25-notice-evidence/`. Receipts contain
the command, actual dotnet exit, UTC tool-clock start/end/duration, named
failures, source/test/project/build-input and Core/test DLL SHA-256 pins.
`run-s123.ps1` records pins before and after execution. These clocks are test
execution measurements, not a producer or publication SLA.

| Run | Actual outcome | What it proves |
|---|---|---|
| `s123-original-baseline` | 105 executed, 103 pass, 2 native RED | Existing selection/binary baseline at `4ab980e0b5321047c2289060ed6c5bb0fe69f324` |
| `s123-baseline` | 175 executed, 162 pass, 13 fail; receipt checker also failed | Five NUL defects, two native REDs, six unexpected fixture-transport failures; not a qualified all-case baseline |
| `s123-diagnostic` | 175 executed, 162 pass, 13 fail | Direct string binding at the 512-character case reached SQLite as `length=512, bytes=512, instr(NUL)=0`; schema still contained its existing NUL guard |
| `s123-qualified-baseline` | 175 executed, 168 pass, 7 fail | Exact UTF-8 parameter bytes are converted to TEXT and read back equal before INSERT. All five malformed fixed-hex cases fail the rejection assertion because no exception is thrown; only N1/N2 otherwise fail |
| `s123-candidate` | 175 executed, 173 pass, 2 native RED | All 141 `RegistrationAdmissionTests` cases pass, with no other regression in the selected union |

`notice.trx` is the assertion and test-output record; `stdout.txt` and
`stderr.txt` are retained separately. Historical receipts were not overwritten.
All candidate tests and in-process DDL mutants use the same pinned compiled
Core/test assemblies. Each mutant's actual altered SQL is printed into its
TRX test output and executed on an isolated real SQLite fixture, not merely
matched as source text.

## Claim ledger

| Claim | Oracle and observed fault | Confidence / boundary |
|---|---|---|
| S1: oversized NUL-suffixed hex is rejected | `NativeRows_HexWithNulAndOversizedSuffix_Rejects`: four 64-hex values plus NUL and 70,000 suffix bytes (70,065 bytes); one 32-hex notice ID equivalent (70,033 bytes). Five assertion REDs before repair, five CHECK refusals after | **Verified** for admission input/context/decision digests and notice publication digest/ID |
| Fixed-hex boundaries stay usable | `ExactLowerHexBoundary_PreservesBytes` checks exact value and BLOB byte length; `NonHexOrWrongLength_Rejects` covers empty, one short/long, uppercase, non-hex ASCII and multibyte text | **Verified** selected boundaries; no normalization |
| S1 class sweep covers the second decision-digest column | Six production additions: explicit `instr(col,char(0))=0` and exact BLOB byte length, preserving TEXT type, character length and lowercase-hex predicates | **Verified source change**. The notice decision digest's FK already rejects a mismatch; its duplicate hex guard is not independently mutation-qualified here |
| Other bounded native identity/path text retains NUL protection | Existing guards inspected for operation/session/repository/worktree/terminal/agent/harness/model fields and notice operation/target/owner. Twelve admission-field probes and valid-path target/claim-owner probes reject NUL | **Verified** listed probes. Existing non-hex character limits are not redefined as byte limits without a domain contract |
| S2: required-field evidence is case-specific | Existing `RequiredFields` supplies 29 actual table/column cases. Each isolated mutant removes that column's NOT NULL and local CHECK/key constraints; the **same null-rejection assertion fails**, and the inserted row count is one | **Verified**, 29 executed mutant cases; not a mutation score over all 71 old tests or every individual overlapping guard |
| Trust membership is constrained | Three invalid strings (`Untrusted`, `verified`, empty) are rejected. The matching weakened trust-column constraint admits each; the same assertion fails. Existing Asserted/Verified controls stay green | **Verified**, three cases against one weakened constraint; positive enum controls alone are not RED evidence |
| Due-time CHECK is independently necessary | Valid Pending INSERT with `due_at_ms='tomorrow'` fails CHECK (extended code 275), not UPDATE transition. Removing **only** the due-time type CHECK admits TEXT `tomorrow` and the same assertion fails. Min/zero/max Int64 controls pass | **Verified**, one isolated mutant; the old NULL UPDATE remains a transition test, not due-CHECK proof |
| S3: actual constructor rollback preserves populated v8 | Original session/terminal/span/trace/episode/message/event IDs, Int64 generation, heartbeat/ended rows, cache event/feed/checkpoint and sqlite_sequence are seeded. A collision at the second new table makes actual `Open` throw SQLite error 1. Complete typed table contents and sqlite_master definitions before/after are equal in TRX | **Verified** synthetic current-branch v8 fixture, version remains 8, zero new objects, collision sentinel retained |
| Snapshot and handle oracles can detect damage | Row change and added index change snapshots. A deliberately held SQLite connection makes the exclusive-open assertion fail with IOException; after disposal it succeeds. Following actual migration failure, exclusive file open succeeds | **Verified** local Windows/provider lifecycle. No constructor cleanup change was necessary; intended SQLiteException was observed rather than replaced by teardown failure |

## Corrections and class control

The historical `corrective-schema-red` receipt contains **36 missing-table
failures, one version failure, one cleanup IOException, and two native N1/N2
failures**. It is not 71 semantic constraint REDs. The later 71 passing schema
cases are GREEN evidence; only the new individually executed mutants above
supply their stated fault qualification.

Class -> sweep -> derive -> prevent: NUL-sensitive TEXT length/GLOB predicates
can accept hidden suffixes. The sweep found six unguarded fixed-hex columns
inside this one DDL; other bounded text already had explicit NUL guards.
All fresh/migration paths derive from the same constant. Five real
baseline-to-candidate cases prevent recurrence for the distinct asserted fields.
Second class: a wrapper/binder can change a hostile input before a constraint
test sees it. The six extra failures were **not missing schema guards**;
byte-exact parameter binding and an equal-value readback now qualify the fixture.
Third class: unrelated guards or setup failures can impersonate semantic RED;
isolated live-SQL mutants and the INSERT due-time probe distinguish them.
Fourth class: empty migration fixtures hide data loss; full populated snapshots
plus row/schema fault controls prevent that evidence gap.
These classes are recorded here; the shared lesson register is deliberately
untouched under the user's ownership limit.

## Still unverified / not implemented

This alters only the fresh prerelease version-9 DDL and its migration from the
qualified current-branch v8 shape. It does not retrofit an already-created v9
database. Older prerelease v8 variants and actual released v7/v8 binaries are
unsupported/unqualified by this fixture. No history is dropped or rewritten.

Fact -> initial-notice child FK alone permits a fact with **zero notices**.
Atomic durable admission remains a **protected-writer obligation**, not a DDL
proof. NativeRegistration transaction, Notice128 shared capacity/hydration,
trusted-root/owner binding, publisher/delivery/restart, and pre-bind input
validation remain inactive work. The observed direct string-binding truncation
must not become accepted input normalization in that writer.

Publication bytes `01` with a well-shaped zero digest are deliberately structural
fixtures, not a matching content-digest assertion. Writer digest verification is
not added here. Original ClaimReference payload-retention/erasure remains
unresolved. N1 destructive dequeue and N2 unbounded native admission still fail.
No full P2, P3-P5, rendered-surface, old-binary or upstream qualification is claimed.
Independent Test's previous BLOCK is answered with artifacts, not self-cleared.

Next: implement the already-approved protected Admission transaction, including
input/owner binding, retained initial notice, quota and capability/lifecycle
atomicity, before enabling any ingress or worker.
