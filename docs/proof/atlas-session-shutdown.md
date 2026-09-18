---
id: proof-atlas-session-shutdown
title: "Codex Atlas shutdown: preserved evidence and terminal handoff"
type: proof-pack
status: recorded
owner: "@timianmalloo"
tags: [proof, atlas, shutdown, coordination]
links:
  - { to: plan-atlas-session-shutdown, rel: depends-on }
  - { to: proof-atlas-audit-preservation, rel: relates-to }
review-by: 2026-12-18
summary: "Product work stopped; exact remaining tasks captured; ignored cleanup evidence archived; fleet cleanup and clean-main receipt remain external."
---

# Shutdown evidence — 2026-09-18

The user asked to stop the session, capture TODOs, clean stale worktrees and coordinate clean
main. The pending Owner turn was interrupted; all other product workers were already completed.
The only subsequent delegated unit was the bounded read-only cleanup safety review needed to
avoid deleting evidence. No implementation, new product review, native run or join was started.

The [resume queue](../plans/atlas-session-shutdown.md) names responsible seats, exact commits,
proofs, requests and first actions. Source candidates were read back clean at R124 `a63de46c`
and E2 `39de7428`. The earlier conservation branch was independently verified by the watcher
in `req-01M2S2P2THHNBJ81YN96ME5AX7`, now acknowledged. R122 conditional routing was also
acknowledged; no duplicate review or scope admission followed.

## Cleanup evidence and handoff

The supported `coord worktree cleanup` report inspected 147 worktrees and marked 22 eligible
at its observation. This was a dry run, not 22 removals. The independent review identified six
Codex-owned, explicitly ended trees passing clean, merged-to-both-main-refs, unique-checkout,
unlocked, resolved-path and observed-process predicates. It withheld clearance because ignored
raw evidence and markers existed. The seventh considered tree, `ai-de-owner-code-atlas`, belongs
to Copilot and was retained; Grok's trees were not taken over.

| Candidate (all under `C:/Projects/`) | Disposition |
|---|---|
| `ai-de-conductor-audit-gate-self-test` | Ended, clean, main-merged; protected ignored files archived; GHCP executes fresh checks |
| `ai-de-conductor-ownership-qualification` | Ended, clean, main-merged; protected ignored files archived; GHCP executes fresh checks |
| `ai-de-conductor-surface-ownership` | Ended, clean, main-merged; protected ignored files archived; GHCP executes fresh checks |
| `ai-de-fix-audit-gate-self-test` | Ended, clean, main-merged; protected ignored files archived; GHCP executes fresh checks |
| `ai-de-fix-audit-verifier-proof` | Ended, clean, main-merged; protected ignored files archived; GHCP executes fresh checks |
| `ai-de-fix-recursive-surface-ownership` | Ended, clean, main-merged; protected ignored files archived; GHCP executes fresh checks |

All **41 protected ignored files**, **16485364 bytes**, were copied to
`C:/Projects/ai-de-session-evidence/2026-09-18-codex-shutdown/`. Its `manifest.json` names original
and archive paths and SHA256 per file. Every copy and unchanged source hash was checked. No
original was written. Unarchived ignored files are the reviewer's explicit bin/obj build-cache
allowance; removal still requires fresh checks. The independent archive follow-up receipt is
retained with the archive when returned. Process executable/command lines were inspected;
hidden process working directories and open handles were not measured, so tool refusals remain
hard stops. No recursive shell deletion, force push, reset, stash or peer cleanup was used.

GHCP subsequently issued fleet-wide wind-down notice `req-01M2TTPDSDPTJJQX4WK77CGB2P` and
took coordination of fail-safe cleanup. To prevent competing deletion, Codex handed the exact
six paths and archive to GHCP in `req-01M2TTRVV4WQ69DZAJ0A433Y0P`; Codex performs **no removal**.
The actual removal count and final directory/Git-inventory readback belong in GHCP's terminal
receipt. This proof does not turn candidate eligibility into completed cleanup.

## Main state and limits

At this close's capture, primary HEAD is `48483dc56fe2754d7225d4f9b74453f572cb2fb1`. Primary is **not clean**: the captured
dirty paths are shared coordination logs, decisions, requests and liveness records. No product
dirt was observed. Exact status is retained in `primary-status-before-close.txt` beside the
archive. The request for serialized preservation and a clean-main receipt is
`req-01M2TTE4DQ1NVTMJWH970W3MP4`. A sent request is not a completed commit or writer handback.
The publisher must conserve journals and inspect actual status after writers stop; no main
integration or CI success is claimed by this administrative branch.

The final audit is appended through `audit-log.py`, then own-tree `regenerate-derived.py` and
docs-graph validation run. Actual output files and the later commit/remote/clean-status receipt
are retained beside this archive. The final handoff names the observed checkpoint hash after
commit, never a predicted hash. All session claims are released and the administrative session
ends after that checkpoint. No AIDE contract destination is invented if absent.

Verified versus remaining: worker stop, exact candidate reads and byte-preserving archive are
observed. Independent final cleanup receipt and derived checks are captured as they complete.
Main cleanup, publication, old physical-tree rescue and formal D&P remain explicit TODOs.
This shutdown does not assert their completion.

## Independent cleanup clearance

The archive follow-up returned **CLEAR for the exact six paths**. It independently verified
all 41 live/archive SHA256 pairs, exact protected-set coverage, no new unarchived non-cache
files, and fresh ownership, ended-session, HEAD/status, merged-main, path and observed-process
conditions. First review: five calls/about 86 seconds; archive follow-up: three calls/about
83 seconds (reviewer-reported whole-turn times). The raw receipts are copied beside the archive.

The reviewer corrected one false predicate in its temporary check: a historical qualification
claim began at unix 1789492782.5086336 with TTL 180 and expired at 1789492962.5086336. It was
not a live lease at shutdown. The raw false predicate is retained; the final clearance explicitly
supersedes that interpretation. This does not waive a live lease or the official cleanup's
fresh recheck. Actual deletion remains GHCP's action, not this clearance's claim.

## Main advanced during the administrative close

Direct Git reads observed the conservation merge `9fb249ff` and derived close
`48483dc56fe2754d7225d4f9b74453f572cb2fb1`; `git ls-remote origin refs/heads/main`
returned that same full hash. The tracked-journal test repair landed at `52462b64`.
Neither R124 `a63de46c` nor E2 `39de7428` was an ancestor of either main ref at the
check, so their publication/review TODOs remain. The only primary dirty path at the
snapshot was `.agents/log/codex-atlas-shutdown.jsonl`. Subsequent final lease/session
and terminal-receipt events still need the fleet publisher's closing conservation.
No controls changed under `tools/` or `docs/ai-forward-pack/scripts/` since this
administrative branch's base, checked before the final current-tool claims.

Own documentation regeneration passed: four derived views matched, 14 site figures,
225 registered classes, 784 audit plus 155 change rows with zero unreadable lines.
The graph contained 537 artifacts; the final validation result is retained as
`shutdown-final-graph.stdout`. This evidence qualifies the administrative checkpoint,
not the unexecuted next main CI run or deferred product work.
