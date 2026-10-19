---
id: proof-wl-hygiene-pack-defects
title: "Pack defects found while burning down main's red gates (T13, Ruling 151)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [proof-pack, gate-hygiene, pack-defects, ruling-151, workbench-loops]
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
review-suggested: []
summary: >-
  Gate findings whose cause is the pack's own gate, not this repository's content. None is fixed here
  (Ruling 151: no pack script under docs/ai-forward-pack/ is edited locally). Each is filed as a pack
  defect next step with the finding, the gate, why it is the pack's, and the proposed fix.
---

# Pack defects filed by T13 `wl-t13-hygiene`

Nothing under `docs/ai-forward-pack/` was edited. Each row is a next step for the pack source repository.

## PD-1 — `verify-ruling-citations.py` (pack copy) reads one register this repository does not keep

- **Gate:** `docs/ai-forward-pack/scripts/verify-ruling-citations.py`. The pack runner runs it beside the
  repository's own `tools/verify-ruling-citations.py`.
- **Finding:** 151 defects, e.g. "Ruling 153 is cited as authority in 1 file(s) and no heading in
  docs/notes/rulings.md defines it". every numbered ruling in the addendum-c register is reported, because the pack gate's only
  definition site is `docs/notes/rulings.md` (its `REGISTER` constant, line 60) and this repository's
  rulings are headings in `docs/notes/addendum-c-council-rulings.md` and its sibling notes.
- **Why it is the pack's:** the gate was "absorbed from ai-de's tools/verify-ruling-citations.py" (its own
  header), but it reads one register only (Inferred: the absorb dropped the multi-note scan; the pack source was not opened). The repository's `tools/` copy
  reads those notes and is green (148 cited, 139 defined by a note, 9 frozen as pre-existing debt). Making
  the repository satisfy the pack copy would mean moving 139 rulings into a file `coord decide rule`
  owns, which rewrites the Owner's register to suit a gate.
- **Proposed fix:** the pack gate takes the definition sites from the repository (a `--register <glob>`
  option, or `docs/notes/*rulings*.md` and `*ratification*.md` by default), or stands down when the
  repository ships its own `tools/verify-ruling-citations.py`.

## PD-2 — `verify-portable-text-io.py` and `verify-subprocess-utf8.py` have no per-file exemption

- **Gates:** `docs/ai-forward-pack/scripts/verify-portable-text-io.py`, `verify-subprocess-utf8.py`.
- **Finding:** `tools/verify-front-door-exit-evidence.py` carries 3 portable-text-io findings (lines 1,
  585, 592) and 2 subprocess-utf8 findings (lines 57, 63).
- **Why it is the pack's:** that file is byte-frozen by its own clause 0 (it must equal commit
  `1374401d`, per `tools/run-verify-gates.py`), so the repository cannot edit it, and the two pack gates
  offer no way to say "this file is a captured oracle". `verify-no-machine-paths.py` does offer one
  (`<pattern> machine-path-ok` in `.gitattributes`), which is why that gate went green here.
- **Proposed fix:** the same attribute route for both gates (for example `portable-text-io-ok` and
  `subprocess-utf8-ok`), with the same rule that an opt-out is whole-file and carries its reason beside
  it in `.gitattributes`.
- **Interim state:** 117 portable-text-io findings in 47 other `tools/` scripts and the one
  `verify-preservation-workflow-policy.py` subprocess finding are fixed in this repository. Only the
  frozen oracle remains, so both gates stay red on exactly that file until PD-2 lands.

## PD-3 — `coord doctor` reports git's built-in `union` merge driver as NOT EFFECTIVE

Filed by the Leader `wl-leader2-20261019` on 2026-10-19. The Owner confirmed it by running `coord doctor` and asked
for it to be filed here.

- **Gate:** `coord doctor` (`docs/ai-forward-pack/scripts/coord-core.py`, `driver_status` at line 4671, reported
  at line 4986).
- **Finding:** `merge driver NOT EFFECTIVE [COORD-DRIVER-NOT-EFFECTIVE]`: "declared .gitattributes covers 439
  path(s) via union", "`git config merge.<name>.driver` is unset in this clone". Its remedy is `coord install` in the
  primary. Measured in the primary at `b256bcd2`. The Owner measured 440 paths.
- **Why it is the pack's:** `driver_status` collects every `merge=` attribute value from `git check-attr`. It then
  calls a value missing when no `merge.<value>.driver` key is registered. `union` (like `text` and `binary`) is a
  driver built into git and needs no config, so no clone can ever satisfy the check. The coord drivers are
  registered: `merge.coord-regen.driver` and `merge.coord-register.driver` are both in `.git/config`. `coord
  install` would not change the result. The `union` lines come from the repository's `.gitattributes`
  register lines (T13).
- **Proposed fix:** `driver_status` excludes git's built-in drivers (`union`, `text`, `binary`) from the
  missing set. A self-test covers a `.gitattributes` that declares only `merge=union`, which must report effective.
- **Interim rule (Owner):** a NOT EFFECTIVE that names only `union` is known noise. A NOT EFFECTIVE that names
  `coord-regen` or `coord-register` is still real.

## PD-4 — `coord claim` refuses every in-flight track after each join, because a recount baseline counts as a control

Filed by the Leader `wl-leader2-20261019` on 2026-10-19.

- **Gate:** `coord claim`'s control-currency check (`docs/ai-forward-pack/scripts/coord-core.py`, `CONTROL_PATHS` at
  line 1277 = `docs/ai-forward-pack/scripts`, `tools`; refusal `COORD-CLAIM-STALE-CONTROLS` at line 5491).
- **Finding:** T14 (`wl-t14-nbops`, tree based on 2d3cb1bd) had all three claims on its shared files refused. It then
  edited them without a claim. Measured cause: the only commit on `origin/main` touching `CONTROL_PATHS` that the tree
  lacked was the T7 join `7c54de9e`. That join changed exactly one control-path file, `tools/expected-test-counts.json`
  (2 lines), which is the recount floor `conductor-join.py` rewrites on every join. No executable control changed.
- **Why it is the pack's:** the check treats a data baseline that every join rewrites as a control change. So after every
  join, every in-flight track is "stale" until it merges main, and the claim floor is unusable in parallel work.
- **Proposed fix:** exclude recount baselines (`expected-test-counts.json` and any file the join writes as a floor) from
  the currency set, or limit it to executable controls (`*.py`, `*.ps1`). Add a self-test: a tree behind main by one
  floor-only join still claims.
- **Drive mitigation:** running tracks merge `origin/main` before their next claim. This was mailed to T5 and T8 on
  2026-10-19.

## PD-5 — coord-mail's held-lines advisory says "ack and archive", but there is no archive verb

- **Observed (Owner, 2026-10-19):** `coord-mail read` for wl-owner-20261018 prints "advisory: inbox holds 126 lines (held cap 100); ack and archive" with 0 unacked. coord-mail.py's verbs are {send, read, ack, dispatch}; acked lines stay in the inbox file, so the advisory cannot be cleared and grows.
- **Code (origin/main, docs/ai-forward-pack/scripts/coord-mail.py):** HELD_CAP = 100 at :72 carries `simplify: no archive verb until an inbox exceeds it`; that trigger has fired. The advisory is read-only (:412-415, stderr). The bounce is separate and counts UNACKED messages: send raises MAIL-FULL when queued() >= QUEUE_CAP (:312-313; QUEUE_CAP = 50 at :71; queued() at :210-214). So a large but fully acked inbox cannot bounce mail.
- **Impact:** advisory noise only; no lost mail. The real cap (50 unacked) did bounce a done mail once (DC-NEW-wl-leader2 13th instance).
- **Not fixed locally** (pack script). Upgrade: an archive verb that moves acked pairs to a dated archive file, or a lines advisory that counts only unacked.
