---
id: proof-ownership-qualification
title: "Published ownership gate qualification"
type: proof-pack
status: in-progress
owner: "@timianmalloo"
tags: [proof, ownership, coordination]
links:
  - { to: plan-ownership-qualification, rel: implements }
  - { to: proof-recursive-surface-ownership, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
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

Independent review and required full qualification: pending. Earlier blocker prose remains historical evidence, not current truth. New Core request `req-01M2K0QEPYF0CEWHPBKYQ72GY6` covers the obsolete §2 paragraph only, with all ownership rows unchanged. No new tooling grant or main push is inferred.
