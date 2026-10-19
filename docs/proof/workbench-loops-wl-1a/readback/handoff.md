---
id: proof-workbench-loops-wl-1a-readback-handoff
title: "Hand-off — WL-1a T17d: the read-back runs when the host binds"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T17d node 1 stopped at the Leader's rule-16 kick: checks 1-2 are red then green, and check 3's guard is committed red pending seam req-01M5A3JM9CAE3Q0EA1K0ZX315K.
---

# T17d hand-off (node 1, stopped at the Leader's kick)

## Done (red, then green)

- **The fix** (69452f35). The restart read-back now starts when the host binds, not when the document is built.
  - `NotebookRuns.ReadBackOnEachBind(source)`: the surface registers its submitted envelopes there.
  - `Bound()` runs after `BindHost` and after a successful `OpenHostAsync`.
  - `_binds` / `_readBackFor` make the read-back run once per bind, however many surfaces register.
  - `ReadingBack` stays pending while the host opens. A Send made then waits, so it is b3 (Ruling 163).
  - `SessionDocumentSurface.LastReadBack` now reads `Notebook.Runs.ReadingBack`. One line outside the brief's two ranges, at its declaration.
- **Check 1**, through the real shell. `TheShellsRevive_RendersTheRecordedAttempts_OnceTheHostItOpensAsynchronouslyBinds` drives `WorkbenchShell.ReviveRestoredSessionDocuments` over a real `WorkspaceClient` to an in-process daemon.
  - Red: `0 rows; host bound=True, read back=0` (`check1-red.txt`, 04b75bf0).
  - Green: `check1-green.txt` (69452f35).
- **Check 2**, the shell's order reproduced with a held open. Same red and green commits as check 1 (`check2-red.txt` / `check2-green.txt`).
  - `ATurnSentWhileTheHostOpens_WaitsForTheReadBackTheBindRuns_AndIsB3_AndEachAttemptIsReadOnce`.
  - `TheReadBackRunsOncePerBind_NotOncePerDocument_AndARebindReadsBackOnceMore`.
  - `AnOpenThatFails_ReadsNothingBack_AndTheRetryThatBinds_ReadsBackOnce`.
  - Re-run in the same green command, still green (18/18): `AReopenedNotebookRendersItsRecordedAttemptsTests` (Rulings 163 and 164), `TheAttemptHostBindingTests` and `TheRowsAttemptBindingSurvivesRestartAndSequenceTests`.

## Red: check 3 (committed red on purpose)

- `ReadBackTestsCoverTheShellsOrderTests` checks for the pattern **bind-first read-back**. That is a file that:
  - names `LastReadBack` or `Runs.ReadBack`,
  - calls `.BindHost(` with a `new SessionDocumentSurface(` within 3 lines,
  - and never calls `.BindHostWhenOpen(` or `.ReviveRestoredSessionDocuments(`.
- Its self-test passes. Its census is red on the current T17 tests (`check3-red.txt`):
  - `AReopenedNotebookRendersItsRecordedAttemptsTests.cs:59,310`
  - `TheAttemptHostBindingTests.cs:59,89,134,173`
  - `TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs:60,77,102,166`
- **The branch carries this one failing test. Do not join it before check 3 is green.**

## Next step

1. Seam req-01M5A3JM9CAE3Q0EA1K0ZX315K was still open at the stop. It asks to change the three files above so their reopen lifetimes bind with `BindHostWhenOpen` and an opener that completes after construction.
   - `AReopenedNotebookRendersItsRecordedAttemptsTests`: the `ReopenAsync` helper and the HeldReads reopen.
   - `TheAttemptHostBindingTests` and `TheRowsAttemptBindingSurvivesRestartAndSequenceTests`: the restart lifetime.
   - After that change, Ruling 163's three checks run in the product order, and the guard goes green. Save `check3-green.txt`.
   - If the seam is refused, cover the async order in those files another way, or rule the guard's granularity.
2. Run the whole-suite proof (`tests/AiDe.App.Tests` and `tests/AiDe.Core.Tests` once each) and save it as `suite-green.txt`. It was skipped at the kick.
3. Write `README.md` (V2 frontmatter, `type: proof-pack`, `implements` → `architecture`).
4. Update the Acceptance section of `docs/proof/workbench-loops-wl-1a.md`: "reopened (Ruling 172)", with these proof paths. Keep the demo script's restart step as written.
5. Run `docs-graph.py validate`.
