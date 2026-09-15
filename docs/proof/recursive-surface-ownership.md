---
id: proof-recursive-surface-ownership
title: "Recursive surface ownership: programme evidence"
type: proof-pack
status: in-progress
owner: "@timianmalloo"
tags: [proof, ownership, coordination, tooling]
links:
  - { to: coord-recursive-surface-ownership, rel: relates-to }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Observed scope, red-first evidence, independent receipts, and integration limitations for the recursive Python ownership gate."
---

# Programme evidence

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

Implementation candidate `18a4a19f` is independently BLOCKED; repair, re-review and final join are pending. No full-green, main-integration or final acceptance claim is made by this intermediate proof. Reviewer reports five concrete parser counterexamples despite the passing author self-test. Owner O7 admits a bounded investigation and repair; this does not clear the independent veto.

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
