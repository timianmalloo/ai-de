---
id: proof-worktree-cleanup-20261017
title: AI-DE worktree preservation and retirement
type: proof-pack
status: review
owner: "@timianmalloo"
tags: [git, preservation, cleanup]
links:
  - { to: plan-worktree-cleanup-20261017, rel: documents }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-11-17
summary: All linked worktrees retired after remote recovery verification; only the primary checkout remains.
---

# Observed outcome

Verified: 34 linked worktrees removed; Git and the direct sibling-directory scan both show only `C:/Projects/ai-de`. Two pre-existing unregistered evidence folders were archived remotely and relocated under the retained checkout's `.git/worktree-recovery/`. No divergent historical branch was merged into main. Branch references remain available.

Verified: 34126 evidence files (2212478935 bytes before compression) archived with original paths, per-file SHA-256 hashes, ZIP integrity checks and hashes for every archive part. Ordinary tracked changes and session records were committed with each snapshot. Reproducible build/cache directories were excluded from the supplemental archive; see the machine-readable record. `refs/heads/preserve/cleanup-20261017/atlas-e1-budget-lifetime-repair` contains ignored captures and orphan evidence, at `4e76ce0f4fd583cc66a05365956abc3a3551c12a`.

Recovery: fetch a listed preservation ref, read `preservation/cleanup-20261017/manifest.json`, concatenate `evidence.zip.partNNN` in numeric order, then unzip. The supplemental archive and manifest live under `preservation/cleanup-20261017/ignored-and-orphan-evidence/`. All archive members use their original relative paths; supplemental members begin with the original sibling directory name. External junction targets are recorded without following them.

The complete measured inventory and exact remote commit IDs are in [the JSON evidence](worktree-cleanup-20261017.json). Each original HEAD was checked as an ancestor of its advertised remote recovery head. Every preservation head was checked against its remote ref and against main's workflow policy. At readback, zero local branch commits were absent from remote-tracking refs.

# Checks and limits

- Normal commit hooks checked staged paths on preservation commits; no force push or dirty-worktree force removal was used.
- Coordination cleanup rechecked clean status, holds and commit preservation with `--include-unmerged --remove`. The named option retires pushed historical trees without treating their code as merged or accepted.
- Filesystem readback checked all original tree paths and all direct `C:/Projects/ai-de*` directories, not just Git's removal exit status.
- Workflow-policy negative: `5c01447b` was refused by `tools/verify-preservation-workflow-policy.py`; corrected `3553b0e9` passed. The accidentally launched Build run `39068924944` was cancelled; subsequent preservation commits carry the current manual-only policy. This recurrence and the executable control are recorded under DC-226.
- `tools/regenerate-derived.py` passed its conflict, generated-view, site-figure, defect-register and audit checks after the closing audit. Final graph validation found 564 artifacts, zero defects, zero orphans and zero index drift. The existing 124 freshness suggestions remain (123 review flags and one stale artifact). No production-code behavior changed in this cleanup.
- A final bookkeeping commit includes this proof, the refreshed views, and coordination decisions. Its precommit floor is executed explicitly; only the resulting append to the already-checked session journal is restaged before Git plumbing creates the commit. This avoids an endless commit hook / tracked-journal feedback loop while retaining the guard and its evidence.
- The desktop contract environment (`AIDE_SESSION` / `AIDE_CONTRACT_LOG`) was absent in this harness. No runtime episode registration or scoring is claimed. Evidence is captured in this proof and the committed audit.

# Recovery references

| Retired tree | Recovery branch | Remote commit | Archived files |
|---|---|---|---|
| ai-de-atlas-e1-budget-lifetime-repair | `preserve/cleanup-20261017/atlas-e1-budget-lifetime-repair` | `4e76ce0f4fd583cc66a05365956abc3a3551c12a` | 34 |
| ai-de-atlas-e1-core-fresh-proof | `preserve/cleanup-20261017/atlas-e1-core-fresh-proof` | `79f5d77deee52640d93ab02f5f0880da492a842b` | 2057 |
| ai-de-atlas-e1-core-metadata | `preserve/cleanup-20261017/atlas-e1-core-metadata` | `8ec588c0a754105c50ba22515e53790714b804a9` | 629 |
| ai-de-atlas-e1-legacy-fixture | `preserve/cleanup-20261017/atlas-e1-legacy-fixture` | `2b8b9c697f68a4131234c4d678e994fa1376afa0` | 17833 |
| ai-de-atlas-e1-native-class-view | `preserve/cleanup-20261017/atlas-e1-native-class-view` | `0c852e2b90883d58f4be71e426017c713b23b6be` | 311 |
| ai-de-atlas-live-reader-inventory-repair | `preserve/cleanup-20261017/atlas-live-reader-inventory-repair` | `c833f914d4ce7121be22b2e223e41e3be8f60486` | 6 |
| ai-de-atlas-production-core-execution | `preserve/cleanup-20261017/atlas-production-core-execution` | `b114bfe8f706659b0159ad887bc893ac76c4c493` | 145 |
| ai-de-atlas-production-core-runtime | `preserve/cleanup-20261017/atlas-production-core-runtime` | `fa765e3c0c5ca58d33e8732c27a8e390932ee1a6` | 143 |
| ai-de-atlas-real-daemon-window-proof | `preserve/cleanup-20261017/atlas-real-daemon-window-proof` | `36978648b6c43bf8b18140f1de9d287ad9388bc6` | 0 |
| ai-de-conductor-code-atlas | `preserve/cleanup-20261017/conductor-code-atlas` | `a4912274ed5b9c806c84d7ce869155347b587407` | 2057 |
| ai-de-conductor-main-watch-takeover | `preserve/cleanup-20261017/conductor-main-watch-takeover` | `8484de979e2cbe614aa1658981dd88fb72fcfcf5` | 0 |
| ai-de-feature-journey-case-study | `feature/journey-case-study` | `40d9766b0010dbbd370cffad8ee8b617c3a10606` | 0 |
| ai-de-fix-atlas-audit-capture | `preserve/cleanup-20261017/fix-atlas-audit-capture` | `7c6d4c42af787549a5093479d8cd9e1528d5fa7f` | 0 |
| ai-de-fix-atlas-core-gates | `preserve/cleanup-20261017/fix-atlas-core-gates` | `6c0c6144feaba994eef3407517ff1cb5a2d32cb2` | 4 |
| ai-de-fix-d0-atlas-capture-consistency | `preserve/cleanup-20261017/fix-d0-atlas-capture-consistency` | `663e973ce6d83ec18d4bca84a5b5eaea1bf0d96d` | 4224 |
| ai-de-fix-d0-atlas-conditional-closure | `preserve/cleanup-20261017/fix-d0-atlas-conditional-closure` | `bffa6ca8e3f4d535a9dff23b0f3c98118645edf4` | 16 |
| ai-de-fix-d0-atlas-correction | `preserve/cleanup-20261017/fix-d0-atlas-correction` | `15a972f64a7a4704b9ef48edba3f5cabd30ceaed` | 13 |
| ai-de-fix-d0-atlas-independence | `preserve/cleanup-20261017/fix-d0-atlas-independence` | `b7af549755c45f9a4a2301ca4c1e347d343f97fb` | 14 |
| ai-de-integration-atlas-five-gates | `preserve/cleanup-20261017/integration-atlas-five-gates` | `22ce99266eecf2ce26a093a115749ad708df9193` | 1423 |
| ai-de-integration-atlas-view-spikes | `preserve/cleanup-20261017/integration-atlas-view-spikes` | `da6217bd58789913e714e5eb9c2fb3562f9f85bb` | 0 |
| ai-de-lane-sessions | `lane/stream-y` | `6b8a58485f51b255c515ec6774386a0f45c7cdf9` | 0 |
| ai-de-review-atlas-assembled-preservation | `preserve/cleanup-20261017/review-atlas-assembled-preservation` | `d58f68ef71b9e990c56ad51473c67c540a26620f` | 0 |
| ai-de-review-atlas-native-instance-observer | `preserve/cleanup-20261017/review-atlas-native-instance-observer` | `dd9ce2a2c3501c7a9d11285369c8c1e0ee44c3d3` | 23 |
| ai-de-review-atlas-native-observer-design | `preserve/cleanup-20261017/review-atlas-native-observer-design` | `5ea8dfa5128edd5eb23fba788f731b87c8331255` | 3 |
| ai-de-review-atlas-native-observer-sink | `preserve/cleanup-20261017/review-atlas-native-observer-sink` | `60c890d58ccd268e203e5c2072e0fea7bb4efec7` | 27 |
| ai-de-review-atlas-native-view-state | `preserve/cleanup-20261017/review-atlas-native-view-state` | `a7d81e95fb0c7ddd53bdbe15493b5d50d1772c51` | 24 |
| ai-de-review-atlas-p1-03-uia-transition | `preserve/cleanup-20261017/review-atlas-p1-03-uia-transition` | `08d5d68751bbda969e327cfe0171f137927d73aa` | 34 |
| ai-de-review-d0-atlas-capture-consistency | `preserve/cleanup-20261017/review-d0-atlas-capture-consistency` | `3dab49305003a9b0d3655a7e975cc5d65c3844a4` | 3167 |
| ai-de-review-d0-atlas-conditional-closure | `preserve/cleanup-20261017/review-d0-atlas-conditional-closure` | `3ece8c13e8b27f8814e0cf44b6a075f02fad09e1` | 359 |
| ai-de-review-d0-atlas-correction | `preserve/cleanup-20261017/review-d0-atlas-correction` | `57ea6f3640022cf417e24918bed3484412995ea4` | 8 |
| ai-de-review-d0-atlas-independence-astra | `preserve/cleanup-20261017/review-d0-atlas-independence-astra` | `88cdda8a90d6e9e3c0f6c84df3d78eeafd61e717` | 4 |
| ai-de-review-d1-r3-consumer-boundary | `preserve/cleanup-20261017/review-d1-r3-consumer-boundary` | `16d5cb0e09750866016a82318caf49757bc34aec` | 5 |
| ai-de-spike-d0-inactive-binding | `preserve/cleanup-20261017/spike-d0-inactive-binding` | `8b0440087b30baeadbb275d89cfb0a92c9d8a570` | 14 |
| ai-de-test-atlas-native-instance-observer | `preserve/cleanup-20261017/test-atlas-native-instance-observer` | `1b911de7a35b9ba1f675b7d1fca069e9c28779ca` | 231 |
