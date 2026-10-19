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
