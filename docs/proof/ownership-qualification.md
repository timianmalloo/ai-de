---
id: proof-ownership-qualification
title: "Published ownership gate qualification"
type: proof-pack
status: complete
owner: "@timianmalloo"
tags: [proof, ownership, coordination]
links:
  - { to: plan-ownership-qualification, rel: implements }
  - { to: proof-recursive-surface-ownership, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
  - { to: proof-ownership-qualification-review, rel: relates-to }
review-by: 2026-12-15
summary: "Current qualification and status reconciliation after the Core coordinator published the recursive gate and repaired shared liveness lookup."
---

# Qualification evidence

Original candidate `f7fd47073f2a774ddccd37246a25ebe5ba2515fe` is an ancestor of current main `0b3644f22f16dffee32bd9a9a7fb212ad1639508`. The published surface gate has no diff against that candidate. Core's `553bb9bc` changes the stranded-audit lookup from the invoking worktree's committed log snapshot to the primary's live coordination log, and adds a linked-worktree self-test. This is the observed fix for our reported false liveness failures; Codex did not author it.

## Receipt reuse, not a new runtime run

Owner O8 requires matching all relevant inputs before reusing runtime receipts. Compared original receipt-producing `234af00ce0942ba73809ea1aa78d5cf3fdf7a261` with qualification base `0b3644f2`: the complete changed-path list contains only coordination/knowledge/generated documentation plus the two Python gates. No source, test, project file, props/targets, package/SDK pin, solution, or test setting changed.

- Source tree: `0926cff7e383574cc05e7224e70497352dca6198` on both revisions.
- Tests tree: `f0a4813e8b3a03f6b8cce49b9d585109d2be0072` on both revisions.
- Same Windows machine and original retained worktree `C:/Projects/ai-de-conductor-surface-ownership`; original non-updating run: 1,024 App and 2,719 Core executed/pass, one Core NotExecuted. No claim of a fresh run or all discovered tests executing.
- Copied ignored App TRX SHA-256: `413018b9cc52f61cdcbf5768f818d3c469a09903f90c63c7375525ac5b1de0d6`.
- Copied ignored Core TRX SHA-256: `477490f36ef6278daf2ec5acf8625be7c451195ca1cb101d593d73400b594b14`.

## Current status

**Superseding close, 2026-09-15:** all 38 required gates passed at `3284acc55d906cdd5beb7e80527200b8bdf3f491` through `conductor-join.py --continue --no-push`. The holder explicitly released both audit leases (primary `atlas-e1-native-class-view` log sequences 99 and 100); the checkpoint then committed without bypass. Normal ownership: 17 discovered, 17 assigned, zero pending. Eight injected mutants passed. Audit uniqueness, graph validation and derived checks passed. Graph validation reports 66 advisory review suggestions, zero failing findings. The join's earlier premature acceptance is corrected by append-only supersession `al-01M2K1GCQT3GRHWYW1RWC3B49Y`; successful continuation audit is `al-01M2K1GV0102SQXRWRP2TW82QH`.

Owner O10 approves updating only this programme's stale status records, preserving their history, checking affected outputs, and handing candidate/base SHAs to Core. Original implementation is already published at `33e9ae7e`; these qualification and documentation corrections are branch-qualified, with main publication left to Core. No new tooling or ownership policy is introduced. Retain programme/review worktrees for receipt provenance and integration handoff. No AIDE_CONTRACT_LOG is configured in this harness; actual evidence paths are captured in the committed audit.

Historical checkpoint narrative below is retained; its pending qualification statement is superseded by this close.

Final independent record review: PASS, 3/3 calls. Reviewer inspected exactly six programme status files (34 additions, eight deletions), unchanged historical blocked occurrence counts, main ancestry, and HTML/Markdown parity; no source, tests, tools, or ownership rows changed. Conductor inspected the six-file diff and final generated site changes (audit/ledger counts only). After final audit `al-01M2K1R3MANATTDTCV8NM55GTE`, regeneration passed all its checks; explicit docs graph validation reports zero defects, zero index drift and 66 existing advisory suggestions. These documentation-only close checks supplement the unchanged 38-gate qualification; they are not a second full run.

Independent Test Architect and Simplifier PASS at reviewer commit `3665e51f2d66d396ebbbe95c113a33ec7a956be4`. The fixed self-test and normal gate pass; the old-lookup mutation fails the registered-linked-tree assertion. Original surface gate blob remains `ce11ae27eb79b97f54a1cf0fec4e5036f7ad90c4`. Review receipt is imported verbatim, SHA-256 `66e8b16314eeda217934db71c767c5ac0f1c54fe1c1fba2ed3519b3e5aa5c38c`; source and destination matched. Original audit `al-01M2K13QBTAVSKMYC3AMS1DK44` remains on the retained reviewer branch. Reviewer actual cost: 14/12 calls; stale patch context caused the recorded estimate overrun.

Core resolved request `req-01M2K0QEPYF0CEWHPBKYQ72GY6` with closure commit `ccd467ab`. Qualification also observed two pre-existing INV-0012 dangling link targets; Core resolved request `req-01M2K0WVDGCSWJH917GDPJF4N5` with `516f7d5a`. Conductor inspected the entire base-to-tip diff: eight added §2 history lines, two corrected canonical IDs, and a derived index. No ownership rows or source/build inputs changed. Owner O9 approved consumption of this frozen handoff through the repository join without waiting for batch publication. No main publication by Codex is authorized or claimed.

The script joined `516f7d5a` and passed its source/self-test/regeneration checks. Step 7 was refused because native work item `atlas-nm12-evidence` held leases on `docs/audit/audit-log.jsonl` (register) and `docs/audit/audit-data.js` (derived). Core confirmed these leases are inappropriate and asked the holder to release; requests `req-01M2K16A5061KW8YAC4GTAXJXH` / `req-01M2K16A6NSVQBMN9GMJ2VQPM7` preserve the coordination. Staged records remain intact; no TTL wait, peer lease release, or hook bypass by Codex. Full gate qualification remains pending that commit checkpoint.
