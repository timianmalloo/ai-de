---
id: proof-cross-harness-coordination
title: "Cross-harness coordination: P0 confidence ledger and pending oracles"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: P0
tags: [coordination, proof-pack, confidence, pending]
links:
  - { to: spec-cross-harness-coordination, rel: documents }
  - { to: design-cross-harness-coordination, rel: documents }
  - { to: plan-cross-harness-coordination-phases, rel: relates-to }
  - { to: investigation-cross-harness-message-delivery, rel: depends-on }
review-by: 2026-10-16
summary: >-
  Single confidence ledger for the P0 draft, preserving unexecuted runtime and authority
  gates as pending or blocked. Pins fresh source evidence and defines falsifiable
  phase-specific tests without presenting old investigation results as new passes.
---

# Proof Pack — P0 draft, not independent PASS

**Author:** Data & Persistence peer/co-author. **Tier:** T2.
**Scope:** five new authored docs and official append-only audit entries; no solution code,
migration, endpoint probe, new dependency, runtime repair or independent gate clearance.

## 1. Receipt and immutable evidence

Repository: `timianmalloo/ai-de`.
Source baseline/conductor HEAD: `94ec9036dd0b72aa5b759badcf21a9e3aba6659b`.
Main last-read value supplied by conductor: `bcf4959bc0e0e361736e6a179f05b69fcd0500f8`;
not re-investigated or claimed current.
Branch: `feature/xh-p0-contract`; session: `xh-p0-contract-b0d0`.
Official worktree creation registered `C:\Projects\ai-de-feature-xh-p0-contract`.
Requested path `C:\Projects\ai-de-xh-p0-contract` was not honored by `worktree new`:
`--path` is cleanup-only; `cmd_worktree` derives path from primary basename and branch.
No manual move, installation, config/hook edit or peer-tree edit was used.

Official allocated ADR: `adr-01M2NJ2PQS7X6GE3F559JC4TNP`, under established `docs/adr/`.
All five authored paths are leased at TTL300 only for editing; register/derived paths
are not authored claims. `.agents/artifacts.yml` is classification authority.

| Commit-bound path | Full Git blob | Working-byte SHA-256 at fresh read |
|---|---|---|
| `docs/collaboration/session-contracts.md` | `536352879936b5b10a4b57e71b31f23981ce9b84` | `4e0c3f3532d0a8e0ddddad82477d0d42f2a943a2768710354de1be7bcba8280f` |
| `docs/investigations/cross-harness-message-delivery.md` | `5cdc8be97381f7a00d7b661e8e2983bdc0a92d69` | `b866cbcbfb60429405b3cd78e4fb85361b88c938543f53cb5c6f72fbfe202b04` |
| `docs/ai-forward-pack/scripts/coord-core.py` | `b2ed495fcf4b6332ddf517aee17144173ac5b96b` | `f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf` |

The blob/working hash pins above are local integrity observations, **not trusted-registrar
attestations**. §2 remains the sole authority. A full AuthorityRef additionally requires
verified repository identity, authenticated decision/issuer evidence, scope and a verifier
receipt as specified in the design. This author cannot manufacture those.

Graph check: conductor tree has no `graphify-out/graph.json`; no graph generation/install
was attempted. Real document links establish ADR-0023 → implements → architecture-loomkeeper
→ implements → spec-agentic-watcher-substrate → relates-to → session-contracts; ADR-0020
also implements the architecture/spec. The index stores links inside artifact records,
not a top-level `edges` collection; an initial edges lookup returned empty and is not
represented as absence of graph relations. Derived-index freshness after new docs is
conductor-owned and pending, as are security/privacy rollup backlinks.

## 2. Fresh source evidence (not fresh runtime reproduction)

| Source | Verified static fact | Runtime claim boundary |
|---|---|---|
| `coord-core.py:388–442,1786–1830` | Append uses O_APPEND and one os.write without an fsync/full-byte-count check; reader sorts producer `at`; fold open/resolved; add overwrites same ID; resolve lacks typed revision/generation | O10 must qualify concurrent/full-write/receipt behavior; no power-loss guarantee. Old investigation's six inert assertions are not rerun results of this P0 receipt |
| `CoordinationContractLog.cs:13–105,230–269` | Writer emits contract records; pump rereads entire directory and applies every event | Two-pump post safety still needs O11 |
| `CoordinationContract.cs:259–268,576–628` | Time ordering; ApplyBoardPost calls service per recognized post; no source-key check on that path | Actual repeated-post/restart effects remain Inferred |
| `MessageBoard.cs:127–175` | Instance lock, fresh message ID, count+1; parent/capability validation in service | Two-service cursor loss is an Inferred interleaving until O14 |
| `SqliteWatcherObservationStore.cs:546–617,1262–1276` | Ordinary insert; message PK; repository index; no repository/seq unique constraint or parent FK there | No migration up/down or query plan executed in P0 |
| `WatcherHost.cs:45–73` | Opens `watcher.db` and composes registrar/ingest/pump | Conflicts with shared-store architecture wording; no live binary census |
| `BoardPublisher.cs:70–88`; `BoardTools.cs:76–112` | Drop tombstones; keep newest bounded page; MCP filters sinceSeq then takes newest | Cursor-complete recovery not proven by these views |
| `WatcherBoardPaneViewModel.cs:11–51,68–72` | Row omits source/parent IDs; query reads all board messages | No UI test run or native failure inference |
| `CoordinationContractLogTests.cs:117–130` | Existing rerun test asserts registration counts only | Not proof of post idempotence |
| `MessageBoardTests.cs:100–143` | Tests existing/cross-repo/missing parent, content-free ACK | Not proposal acceptance or migration evidence |

## 3. One confidence ledger

| ID | Finding / severity / confidence | State and clearance evidence owed |
|---|---|---|
| F01 | **Blocker / Inferred:** reliable coordination reuse can violate one-effect/cursor-completeness invariants via replay/two-instance interleaving; static path Verified | OPEN for P2 live delivery. O11–O15 real red tests and independently reviewed repair; author does not clear |
| F02 | **Major / Verified:** ADR-0023 shared-store wording conflicts with `WatcherHost.Open` watcher.db composition | OPEN; explicit P2 placement/ruling before physical schema commitment |
| F03 | **Blocker / Flagged:** authority verifier/human-channel evidence is not established for the new authority adapter | OPEN for live authority use, not a proven exploit. O04–O07 plus independent Security gate; hash alone cannot clear |
| F04 | **Major / Verified:** old newest-N/read DTO shapes do not encode complete coordination cursor/thread semantics | OPEN for enhanced reads; O18/O26 plus compatibility proof |
| F05 | **Major / Flagged:** endpoint positive conformance and retention/erasure basis not yet established | OPEN; per-endpoint O22 and Privacy/operator review before live sensitive-data admission |
| F06 | **Minor / Verified:** official CLI generated a different worktree path than requested | Documented deviation; use registered generated tree, no bypass |
| F07 | **Minor / Verified:** graphify absent; new index/rollup entries not regenerated by this author | Conductor checkpoint must derive/validate and report stale/orphan results honestly |

No runtime red/green result is implied by a static finding. Hard veto applies to
invariant-violating live reuse or an unsafe migration, not to publishing this draft.

## 4. Oracle catalogue — ALL PENDING, unless explicitly BLOCKED

Synthetic fixtures only; approved real-endpoint tests are separate. The test names below
are **specified future tests**, not files already written or passing tests selected by a filter.

| Oracle | Runnable-test specification / meaningful assertion | Phase |
|---|---|---|
| O01 | `Fold_CorrelatedReply_ShowsOnInitiatingThread`: invoke real official fold/read; one negative reply removes unanswered, keeps acceptance false | P1 |
| O02 | `Read_ResolvedThread_ByIdIncludesResponse`: all-status by-ID contains original + response when open-only omits completed work | P1 |
| O03 | `Fold_DuplicateAndPermutedEvents_NeverReopens`: enumerate causal permutations/retry multiplicity; same logical state, no duplicate semantic action | P1 |
| O04 | `Accept_TransportAck_RefusesApproval`: ACK/legacy “approved” text cannot produce acceptance; exact valid peer pair positive control | P1 |
| O05 | `Accept_SupersededHash_RefusesCurrentAcceptance`: H then H2; late H acceptance preserved historically but not applicable | P1 |
| O06 | `Consume_UnknownOrOldGeneration_Refuses`: absent G, G1→G2 restart, alias reuse; zero G2 consumption/action | P1 |
| O07 | `Authorize_NewTransferWithoutHuman_Refuses`: valid hash but forged verifier, wrong repo/path, Owner prose and no human evidence all fail closed; authenticated scoped positive fixture required | P1; positive authority fixture BLOCKED |
| O08 | `ReadWrite_MixedVersions_PreservesLegacy`: old CLI/read corpus and new reader/writer under rollout; byte IDs/payloads intact; conservative legacy open status disclosed | P1 |
| O09 | `Append_SameKeyDifferentPayload_RefusesConflict`: same bytes returns same receipt; changed payload under same key errors without extra write | P1/P2 |
| O10 | `Append_UnenrolledLegacyWriter_BlocksActivation`: two official callers under contention; torn line/disk failure does not yield success; old writer inventory gates live new mode | P1 |
| O11 | `Pump_RegisterPostTwice_OneMessage`: real SQLite and writer/pump path, register+post, two pumps; one stable message and one receipt, not just one registration | P2 |
| O12 | `Import_CommitThenLostAck_RetrySameEffect`: inject crash after commit before receipt; retry returns original mapping/sequence | P2 |
| O13 | `Import_CrashBeforeCommit_NoEffectOrCheckpoint`: crash each write boundary; effect, receipt and checkpoint all-or-none after restart | P2 |
| O14 | `Import_TwoInstancesReaderBetweenCommits_NoSkippedFact`: two services/connections; barrier A write/B waiting, commit A, reader cursor, commit B, reader; both distinct facts observed exactly once | P2 |
| O15 | `Migrate_ExpandOperationalRollback_PreservesAllHistory`: representative real old DB + new events; actual deployer up, old binary/rollback, re-enable/replay; original IDs/payloads and accepted facts identical | P2 |
| O16 | `Import_LateParentAcrossRestart_EventuallyAppliesOnce`: skewed producer times, pending child, restart, parent; new applied feed transition, no orphan or lost consumed cursor | P2 |
| O17 | `Import_UnsupportedVersionOrSourceGap_ExplicitRefusal`: unknown version, malformed line, truncated/changed prefix; no false checkpoint success | P2 |
| O18 | `Read_401Unread_PagesWithoutGaps`: page 200/200/1 ascending; tombstones included as envelopes; no newest-N skip; epoch mismatch explicit reset | P2 |
| O19 | `Replay_TombstoneAndEndedGeneration_NoResurrection`: restart/register/end/late receipt; redacted payload never returns, old generation never revived | P2 |
| O20 | `Queue_LimitPlusOneOutage_PreservesAccepted`: parameterize bytes/items/pending/retry bounds; overflow refused before admission, permanent parent needs-human, disk/DB outage then recovery loses no accepted obligation | P2 |
| O21 | `Deliver_InFlightLimitAndRunningTurn_DefersSafely`: fake time/transport, limit+1, failed/unavailable model, sibling-read refusal; stable retry, no duplicate semantic action, outage recovery | P3 |
| O22 | `Endpoint_RegisteredGeneration_ConsumesAndWakes`: installed/version-pinned foreground/background GHCP/Codex/Grok/Claude, approved low-volume synthetic message, witnessed correct conversation arrival+consumption+supported post-turn wake | P3; all real endpoints BLOCKED pending spikes/availability |
| O23 | `Launch_TwoCallers_AttributesCorrectActor`: same helper under two launcher identities; actor/generation/worktree/candidate/run and PID creation/parent match each caller | P4 |
| O24 | `Start_ReplayedOrExpiredToken_NoSecondRun`: checked token replay returns original; expiry with live holder refuses regrant; PID reuse is not same process | P4 |
| O25 | `Outcome_Failure_DoesNotImplyRelease`: failed END captured; release only after independent holder check; duplicate release stable | P4 |
| O26 | `Conformance_SameCorpus_AllSurfacesAgree`: official queue fold, board projection, MCP and UI expose equal obligations/revisions/negative states at same watermark | P5 |
| O27 | `Sli_PauseTimeout_NoApproval`: fake clocks; total/paused/available duration and outage denominator correct, threshold never creates acceptance | P5 |
| O28 | `Telemetry_Failures_HaveCodesAndNoSecrets`: correlate spans, stable codes, bounded labels; reject raw prose/capability leakage; UI state/craft/accessibility assertions | P5 |

Additional P2 assertions: forbidden fact UPDATE/DELETE fails at store; same-repo parent
FK rejects cross-repo/orphan insertion; SourceEventKey conflict tested at DB boundary;
rebuild equality from source; EXPLAIN QUERY PLAN uses planned indexes for cursor/pending/
thread lookups at 100× corpus; bounded rows/bytes/attempts. These are part of O09/O13/
O15/O16/O20, not optional afterthoughts.

### Commands / oracle integrity

P1 should add a discoverable stdlib unittest module for the official Python focal
functions, then execute `python -m unittest discover -s <admitted-test-directory> -v`;
the Proof Pack must name the actual path and nonzero executed case count. No fictional
current test file is supplied here. A directly runnable **red control** for the existing
fold, without live queue writes, is:

```powershell
python -c "import importlib.util,sys; from pathlib import Path; p=Path('docs/ai-forward-pack/scripts/coord-core.py'); sys.path.insert(0,str(p.parent)); s=importlib.util.spec_from_file_location('coord',p); c=importlib.util.module_from_spec(s); s.loader.exec_module(c); q={'kind':'request-add','id':'synthetic-q','at':1}; r={'kind':'request-resolve','id':'synthetic-q','at':2}; assert c.fold_requests([q,r,q])[0]['status']=='resolved', 'transport replay must not reopen'"
```

This command is specified, **not executed in P0**; assertion failure is expected from
the fresh source read and must be observed in P1 rather than reported now.

C# seam suite after adding named tests:
`dotnet test tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj --filter "FullyQualifiedName~Watcher"`.
Capture exact selected names, nonzero counts and red→green evidence; that broad command
alone does not prove O11–O20. Use real SQLite/two connections and explicit scheduling
barriers. The existing no-double-register test cannot substitute for O11. No GUI/App/full
qualification is authorized by these commands.

## 5. Operator and rollback questions

1. Which authenticated actual-human decision channel can the trusted registrar verify,
   and how are revocation/supersession and human scope checked offline?
2. How is writer enrollment fenced before enhanced append activation, without pretending
   an old binary honors a new lock?
3. Which accepted interpretation of ADR-0023 governs physical projection placement?
4. What are pilot queue/byte/attempt bounds, disk reserve, retention durations, policy
   deletion basis and backup expiry? Who owns a permanently missing parent?
5. Which installed endpoint modes can actually wake/consume, and which remain BLOCKED?
6. Who can attest actual holder release after run failure/expiry without touching peer slots?

No unanswered operator question can be resolved by a timeout-to-approval rule.

## 6. P0 validation record

Executed: official CLI help/start/worktree registration/ADR allocation/exact-path claims;
complete investigation read; sole §2 ownership read; relevant architecture/US4/ADRs;
fresh scoped source and referenced test reads; local commit/blob/working-hash lookup.
No C# runtime tests, migration up/down, query plan, endpoint consumption, latency
percentiles or security authority positive fixture have been executed.

Documentation-only closeout:

* Official `docs-graph.py validate`: **exit 1**, 525 artifacts, **zero schema/link
  problems, zero orphans, zero date-stale artifacts**; five defects are exactly the
  five new artifacts not yet in the derived index. **74 existing review-suggested
  flags** remain. This is not a clean full documentation gate.
* Initial frontmatter validation exposed two unregistered types and one relation;
  corrected using the actual type/relation registry. No unresolved frontmatter defect.
* Actual index artifact links verified the stated ADR→architecture→spec→authority
  traversal. No top-level `edges` assumption remains.
* `git diff --check`: passed on tracked changes. Final staged check also required
  for the new files; no runtime correctness inferred from whitespace.
* Official audit `al-01M2NJNPENFDV441RY54BJFWCQ` and change
  `cl-01M2NJNPKE3B9BWR5FDWRR1PJZ` record **partial / draft**, not authority or
  independent acceptance. `audit-log.py verify`: **763 audit + 156 change,
  zero unreadable lines**.
* Baseline comparison proved each audit register preserved every prior line and
  appended exactly one own entry (762→763; 155→156). Official append/change
  automatically rendered `docs/audit/audit-data.js`; only that own generated change
  was restored, leaving the conductor to regenerate at checkpoint. No site or
  whole-documentation generation was run or committed by this author.
* No source edits, dependencies, runtime tests, migration, endpoint/nonce probes,
  peer slots, observer changes or nested agents.

Conductor must regenerate derived index/audit views/security/privacy rollups and
validate them at checkpoint. Do not label deferred derived data as a clean site build.

**Gate: P0 draft receipt, independent review pending. Best next action: independent
review of exact commit, then conductor-gated P1 O01–O10.**
