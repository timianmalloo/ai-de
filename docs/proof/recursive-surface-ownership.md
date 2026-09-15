---
id: proof-recursive-surface-ownership
title: "Recursive surface ownership: programme evidence"
type: proof-pack
status: complete
owner: "@timianmalloo"
tags: [proof, ownership, coordination, tooling]
links:
  - { to: coord-recursive-surface-ownership, rel: relates-to }
  - { to: session-contracts, rel: depends-on }
  - { to: proof-recursive-surface-ownership-review, rel: relates-to }
  - { to: proof-recursive-surface-ownership-repair, rel: relates-to }
  - { to: investigation-recursive-surface-ownership, rel: relates-to }
review-by: 2026-12-15
summary: "Observed scope, red-first evidence, independent receipts, and integration limitations for the recursive Python ownership gate."
---

# Programme evidence

## Superseding status — 2026-09-15

The original candidate landed through Core at `33e9ae7e`. Core fixed the shared-liveness blocker in `553bb9bc`; current qualification passed all 38 required gates at `3284acc55d906cdd5beb7e80527200b8bdf3f491`. Independent Test Architect and Simplifier cleared the continuation. See [current qualification proof](ownership-qualification.md) for receipt hashes, remaining publication boundary and exact observations. The blocked reports below describe historical checkpoints, not current programme status. Codex did not push main.

## Contract and scope

Goal: cover nested Workbench surfaces without inventing ownership. Acceptance requires recursive discovery, exact relative identity, sole §2 authority, red-first fixtures, independent review, observed checks and a committed isolated handoff. Product source, new ownership policy, Atlas/Grok work and main publication are excluded.

Base `bab5035e75a1`. Programme `conductor/surface-ownership`. Core handoff: shared request `req-01M2JQ113TK7HGE7YKQ4CB92GA`, Ruling 113. ProseView assignment request `req-01M2JQ3T5VMQWJ59Q3M0ZE420T`, inventory receipt `req-01M2JQAHGFNMK5D2892ZGKMH4M`, Ruling 114. Published main `663c3a80` adds ProseView to the Design row; incorporated by script-driven merge `978b4eb5`. No local invention of an owner or Sessions exception.

## Independent receipts and provenance

These are verbatim evidence imports, not claims to have rerun their authors' work. Source branches remain retained. Hashes were computed on both files and observed equal.

| Receipt | Source commit / branch | SHA-256 | Original audit |
|---|---|---|---|
| `docs/notes/recursive-surface-ownership-owner.md` | `171791fbb223a8f4fcd69157a365373d93c302c1`, `owner/surface-ownership` | `5c1ffe250e7fd5c71aab5c29fbd2354d161ace7fb6fa674b13d4f725f6c07f14` | `al-01M2JQ7PYW3WYGXM3N3R05K0M5`, amended by `al-01M2JQB4XGFHSA2PTN53Q8QZNG` |
| `docs/proof/recursive-surface-ownership-plan-review.md` | `8690c5fa`, `review/surface-ownership` | `7f5d0fd0ec8cd33617414f7ed9112a21c331a035ae4859e272aa94234c2e6b57` | `al-01M2JQPXCYB8G0WWR0008YDPX7` |

Owner Astra confirms T1 and bounded grammar. Independent Sol high plan reviewer: Test PASS, Simplifier PASS, Python PASS-WITH-CONDITIONS, SRE/Orchestrator PASS. These are plan verdicts, not implementation acceptance.

## Baseline measurements

Observed normal gate: 13 surfaces, 13 assigned, no exceptions. Independent recursive filesystem census: 17 matching files. Newly reached: Composer/ComposerSurface.cs, Sessions/ConsoleSurface.cs, Sessions/SessionDocumentSurface.cs and Sessions/ProseView.cs. The first three have explicit or grouped path assignments; ProseView alone lacked one at the base, originating in `68dc6ab5`.

Baseline full runner: 38 gates, six failed. This run overlapped ongoing coordination audit writes; it was a diagnostic baseline, not a frozen-candidate acceptance run.

| Gate | Observed reason | Disposition |
|---|---|---|
| verify-audit-capture | two Conductor planning entries omitted evidence signals | corrected by append-only supersession; gate rerun PASS |
| verify-ruling-citations | Ruling 113 not yet published in this tree | consume published main authority |
| verify-site-figures | three figures stale after audit writes | regenerate after final writes |
| verify-stranded-audit | dirty logs in other active programme worktrees and this in-progress tree | commit own records; report peer records, never alter them |
| verify-terminal-host-exit-paths | no .trx in this fresh worktree | missing .NET runtime receipt, not a Python test failure |
| verify-test-run | no usable Core result file | missing .NET suite receipt, not a claim about runtime correctness |

All remaining baseline gates passed, including the old surface gate, self-test registry, project compilation coverage and subprocess UTF-8 control. This does not certify the changed implementation.

## Change reach and instrumentation

Filesystem → canonical path → §2 Path-cell parser → owner set → CLI diagnostics and exit → temporary filesystem fixtures → unchanged CI real/self-test calls → proof/audit. Counts are derived from the same population; zero population and unresolved declarations must fail. Check stdout/stderr and return codes, not only a successful process launch. Cost source is audit duration markers and per-gate runner seconds; tokens unavailable are recorded as not recorded.

## Class → sweep → derive → prevent

DC-118 control half (b): the scan's root, recursion, suffix set and exceptions must match the declared population. Sweep discovery, path-cell grammar, matching, exception lookup and diagnostics together. Derive one full relative identity. Prevent with nested/duplicate-name/prose/ambiguity/stale fixtures and mutation rejection. No unrelated guard changed.

### Process corrections during this programme

- Unsupported `coordination-plan` metadata type was written, then rejected by docs-graph derive. Corrected to supported `doc`; derive then reported no findings. Class: unchecked metadata vocabulary. Sweep programme frontmatter; prevention is existing docs-graph validation before commit.
- Planning audit entries lacked explicit absence-of-proof signals. Existing verify-audit-capture went red and went green after official superseding entries `al-01M2JQR18QZ4JH7MVER7ECZP6H` and `al-01M2JQR1C19T5NFSEJ5BQD2M1Z`. Class: omitted optional-looking fields that carry required evidence. No historical row edited; subsequent records carry proof paths or explicit signals.
- Merge commit `978b4eb5` ran before the shell's identity assignments and emitted NOT CHECKED. This is a real enforcement gap, not an enforced pass. Subsequent join commit had identity set and reported six paths checked. Control for remaining mutation calls: identity assignments must be their first executable lines; original gap remains disclosed. No source ownership was decided in that merge: it imported published main, with site conflicts confined to regenerated data figures.

## Completion state

The bounded implementation is reviewed and committed. Integration qualification is BLOCKED by shared audit state outside this lane. No full-green or main-integration claim is made. Final reviewed author `676f63ed3899cd5282f23db51eb582ebad169e17` joined through the prescribed script at `7fdf6ab0`; join checkpoint `df050caf` retains the implementation and its evidence. No push occurred.

## Mandatory runtime receipts

Owner O6 admitted one non-updating `python tools/verify-test-run.py` because the mandatory join runner requires runtime receipts in this new worktree. Observed exit 0: App 1,024 executed and passed; Core 2,719 executed and passed. Core reports 2,720 total, with one NotExecuted test: `AiDe.Core.Tests.PromptCompilation.PurgeAndTheSessionDeleteCascadeTests.PurgeOfASymlinkedEnvelopeFileIsRefusedBeforeAnyFileIsTouched`. The existing `tools/expected-test-counts.json` minimum is 2,719; no baseline was edited. This is 3,743 executed tests, not a claim that every discovered test executed.

The run used Conductor HEAD `234af00ce0942ba73809ea1aa78d5cf3fdf7a261`. Source tree `0926cff7e383574cc05e7224e70497352dca6198` and tests tree `f0a4813e8b3a03f6b8cce49b9d585109d2be0072` match main. `git diff main -- src tests` was empty. The unchanged terminal-host receipt gate then passed all five exit paths: window close, owner exit, owner killed, tab close, and child exit returning to zero headless hosts.

Ignored runtime files remain in the retained Conductor worktree; they are not claimed as committed artifacts:

| Receipt | SHA-256 |
|---|---|
| `artifacts/test-results/AiDe.App.Tests.trx` | `413018b9cc52f61cdcbf5768f818d3c469a09903f90c63c7375525ac5b1de0d6` |
| `artifacts/test-results/AiDe.Core.Tests.trx` | `477490f36ef6278daf2ec5acf8625be7c451195ca1cb101d593d73400b594b14` |

## Frozen author receipt

Author commit `18a4a19f82eff6130c9938bdf401539cd2a8944f` contains gate blob `0f53658b30857222d8207b9b6fdfe706039b6e8b`. Conductor independently checked the full base, plan and blob identities against Git. The author proof records 22 observed baseline failures, final self-test exit 0, six killed mutants and the Ruling 114 population result of 17/17. Independent implementation review remains the acceptance boundary; a reported result is not promoted solely by citation.

The author's audit records 981 measured seconds, 47 calls against a 35-call estimate; final handoff reports 58 calls including later status and completion work. This overrun is a planning defect, not a raised budget. Tokens are not recorded. The author also reports its commit hook lacked `AGENT_SESSION`; separately granted/checked leases do not prove commit-time enforcement. This repeats the identity-order defect already recorded above and in DC-088. Conductor joins must set identity before their first mutation.

## Repair and independent acceptance

Conductor reproduced the first review harness: 4/9 pass, exit 1. Investigation traced five parser state/relevance failures; Owner O7 confirmed same-scope repair. The five new embedded cases ran red before repair. A subsequent Conductor probe found another DC-118 sibling: `Core` `Workbench/**` plus `Design` `Workbench/*.cs` silently lost the conflict. That fixture was also observed red before correction, then independently observed to name the conflicting `HiddenView.cs` path.

An early re-review PASS was corrected to BLOCK when Conductor compared it with the review's recorded veto predicate: two targeted source mutations were still missing. The final test-only delta added heading-reset suppression and delimiter-free-row suppression to the original six mutants. The reviewer then observed the final eight-mutant self-test passing and cleared every triggered veto. No earlier blocked candidate was joined.

Final gate blob: `ce11ae27eb79b97f54a1cf0fec4e5036f7ad90c4`. Reviewer byte snapshot SHA-256: `00bacad13ad925019bf5118cb6b9c4676edafddfc3fb3b4a5c01e820eafcda08`. Conductor inspected the final two-mutant diff and the committed review receipt. Receipt `docs/proof/recursive-surface-ownership-review.md` is imported verbatim from `cbf288b803860ee4cc7050aec5296b7a38748dba`, SHA-256 `3fa78362c62e33417ccb65fd3db64f230fd348dcc1b8ea1ad88b5fe0c264c8e0`; source and destination hashes matched. Original review audit `al-01M2JT1MJWEKKRFZGH04CWNG9K` remains on the retained source branch; it is not duplicated under a new identity.

Independent final verdicts: Test Architect PASS; Python PASS; Simplifier PASS; SRE/Data integrity PASS. Evidence: nine adversarial cases, broad-pattern conflict, eight killed mutants, valid real-register parsing and bounded source diff. Review does not certify unrelated full-repository gates.

Repair costs: investigation audit 455 seconds; first repair 18/18 calls. Final two-mutant unit audit 222 seconds, 14/6 calls; stale CLI and patch-context assumptions caused the estimate overrun. Final re-review 16/16 calls. Tokens and a reliable whole-Conductor invocation count are not recorded. No estimated speedup is claimed.

## Observed join results and blocker

`conductor-join.py` ran the task-specific contract with `recount: []`, `build: []`, and `--no-push`; it did not use `--docs-only` or skip mandatory gates. Merge and checkpoint commits set identity before Git; the checkpoint hook reported eight staged paths checked. The script's full runner failed 1/38. Its truncated output omitted the failing gate, so the unchanged full runner was repeated with live output to identify the failure. It again reported 37/38 passing.

| Check | Observed result |
|---|---|
| Normal surface gate | PASS: 17 discovered, 17 assigned, zero pending |
| Embedded self-test | PASS: eight injected mutants rejected; intentional orphan fixture prints a failure before expected success |
| Python compile / gate self-test registry | PASS; 36 registered gates, 10 frozen self-test debts unchanged |
| Audit IDs / defect register / derived views | PASS; no duplicate audit IDs, 225 classes, four derived views current |
| Docs graph validation | No failing validation error; 66 existing review suggestions, zero stale items |
| Full runner | 37/38 PASS on two runs; no skip or waiver |
| Project coverage | PASS: all 39 tracked projects compile |
| Runtime receipts | PASS: 3,743 executed tests; five terminal-host exit paths |
| Stranded audit | FAIL: `C:/Projects/ai-de/docs/audit/audit-log.jsonl` uncommitted in PRIMARY |

The stranded-audit gate had passed earlier, then failed as shared state changed. Conductor read its full diagnostic and raised `req-01M2JTCR30AJ256HJFZHZJNRPX` to `claude-conductor`. The primary log is outside this lane; it was not committed, discarded or rewritten by Codex. The join script emits acceptance before its final gates; premature entry `al-01M2JT354ZQHZGE1E4GF8NE54K` is corrected append-only by `al-01M2JTDQ908JTSDTZGNTP7CMTM`, acceptance false/outcome blocked.

## Handoff and residuals

**Latest shared-state observation after commit `327528e2`:** `verify-stranded-audit.py` no longer reports the primary log. It instead reports uncommitted `docs/audit/audit-log.jsonl` and `docs/audit/change-log.jsonl` in `C:/Projects/ai-de-understanding-views-spike`. At the same observation, `coord session list --json` lists `understanding-views-spike` as active and Grok's current liveness says N7 is dispatched. Therefore the gate's wording "nobody is live" is not accepted as proof of abandonment. Request `req-01M2JTQ5G3YVP0D81XBH2ZQK6E` asks Grok to preserve/commit its records at the normal bounded exit, without discarding or forcing a premature commit. This supersedes the primary path as the latest blocker; it does not erase the two earlier full-run results. The source and test baselines remain unchanged. No peer records were touched.

Mitigation `mit-0010` was captured through `dream.py capture-mitigation` for DC-118, naming the actual gate/self-test control and its red-green boundary. Its generated audit `al-01M2JTHBQ9AET7766XWR1VG8BE` omitted explicit proof signals; official supersession `al-01M2JTJ496AAG7HADNE72M9CCV` supplies the goal and proof. That acceptance covers mitigation capture only. Final documentation validation observed 485 artifacts, zero defects, zero orphans and zero index drift, with 66 existing review suggestions. Audit-capture validation passed.

The final allocator check passed across nine declared families, including ten mitigation records and 118 branches. It emitted four non-failing advisories about pre-existing DC-177/DC-178 reuse on two Atlas branches. Those advisories are outside this programme and were reported to the Owner; no Atlas register was changed.

- **Completed / Verified:** recursive canonical identity gate, unchanged ownership decisions, red-first and mutation controls, independent acceptance, script-driven isolated merge, proof and audit capture.
- **Remaining / Flagged:** peer audit preservation and reconciliation of the shared gate's liveness interpretation, then unchanged mandatory join qualification. Main integration additionally requires an explicit recorded grant; none was requested as implied consent or exercised.
- **Next:** coordinator resolves the precise shared-audit seam; integrator rechecks the current recursive population, including any later Atlas/Grok surfaces, against sole §2 authority before publication.
- **Inferred:** no claim that future surfaces or unsupported Markdown syntax are covered. The parser is deliberately bounded to the accepted §2 grammar.
- **Retained worktrees:** Conductor for the blocked candidate and ignored runtime receipts; author for source/red-first evidence; Owner and reviewer for original decision/audit provenance. None is removed.
- **Capture limitation:** `AIDE_CONTRACT_LOG` was unset. No invented episode channel/path or event is claimed; the actual committed proof and official audit records are the available capture.

Changed implementation: only `tools/verify-surface-ownership.py` including its existing self-test. Supporting changes are this programme's coordination/plan, investigation, Owner/reviewer/author proof artifacts, append-only audit/lesson/mitigation records and regenerated documentation views. No product source, tests, test-count baseline, ownership assignment authored by Codex, neighboring gate or coordination framework changed. Ruling 114 was imported from its owning coordinator.
