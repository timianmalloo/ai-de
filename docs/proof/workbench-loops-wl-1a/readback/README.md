---
id: proof-workbench-loops-wl-1a-readback
title: "Proof Pack: WL-1a T17d — the restart read-back runs when the host binds (Ruling 172)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, evidence, read-back, restart, ruling-172]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T17d's evidence for Ruling 172: a reopened notebook reads its recorded attempts back when the host binds, before or
  after the document is built, once per bind. Checks 1-2 run through the shell's real order, and check 3 is a per-site
  guard that no read-back test binds the host first unless the site is named, with a reason, as not a reopen.
---

# T17d — the restart read-back runs when the host binds (Ruling 172)

- **Branch:** `wl1a-readback`, off `origin/main` `a05b0b2d`. Node 1 (`wl-t17d-readback`) built the fix and checks 1–3 red. Node 2 turned check 3 green and wrote this pack.
- **Ruling:** 172 (`docs/notes/addendum-c-council-rulings.md`). WL-1a was reopened because the shell binds the host asynchronously (`WorkbenchShell.cs:3596`, `BindHostWhenOpen`) and builds the document at once. The read-back only ran when the host was already bound at construction, so in the running app it never ran.
- **Seam:** req-01M5A3JM9CAE3Q0EA1K0ZX315K (GRANTED). It allowed the reopen order to change in the three T17 test files; the assertions stay unchanged.

## Checks

| # | Ruling 172 | Tests (exact) | Red | Green |
|---|---|---|---|---|
| 1 | (1), (2): red through the real shell order | `TheReadBackRunsWhenTheHostBindsTests.TheShellsRevive_RendersTheRecordedAttempts_OnceTheHostItOpensAsynchronouslyBinds`: `WorkbenchShell.ReviveRestoredSessionDocuments` over a real `WorkspaceClient` to an in-process daemon | `check1-red.txt` at 04b75bf0 (`0 rows; host bound=True, read back=0`) | `check1-green.txt` at 69452f35 |
| 2 | (2), (3): once per bind; Ruling 163's b3 in the shell's order | `ATurnSentWhileTheHostOpens_WaitsForTheReadBackTheBindRuns_AndIsB3_AndEachAttemptIsReadOnce`, `TheReadBackRunsOncePerBind_NotOncePerDocument_AndARebindReadsBackOnceMore`, `AnOpenThatFails_ReadsNothingBack_AndTheRetryThatBinds_ReadsBackOnce` | `check2-red.txt` at 04b75bf0 | `check2-green.txt` at 69452f35 |
| 3 | (3), (4): no read-back test binds first | `ReadBackTestsCoverTheShellsOrderTests.ThePattern_FlagsEachBindFirstSite_UnlessItsMemberIsNamedWithItsCount_AndFlagsANameThatMatchesNothing`, `.NoReadBackTest_BindsTheHostBeforeTheDocument_UnlessTheSiteIsNamedAsNotTheShellsReopen` | `check3-red.txt` at 7bd617ad (per file); `check3-red-per-site.txt` at 0c92dafd (per site) | `check3-green.txt` at 4be08710 (20/20) |

### Check 3, in detail

- **The guard was made per site in node 2.** In node 1 it was per file: one `BindHostWhenOpen` anywhere in a file cleared every bind-first site in it. So a named exclusion could never match, and a converted file could keep a bind-first reopen. Now each bind-first site fails on its own unless `NotAShellReopen` names its member with the exact site count and a reason. A count mismatch fails, and so does an entry that matches nothing.
- **Converted (reopens), to `BindHostWhenOpen` with an opener completed after construction:**
  - `AReopenedNotebookRendersItsRecordedAttemptsTests.cs`: the `ReopenAsync` helper (:59, used by all five tests) and the HeldReads reopen (:310).
  - `TheAttemptHostBindingTests.cs:89`: the restart lifetime.
  - `TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs:77`: the restart lifetime.
- **Named, not converted** (in the guard's `NotAShellReopen`, each with its reason):
  - `TheAttemptHostBindingTests.cs:59, :134, :173` and `TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs:60, :102, :166`: first opens over an empty store. No attempt is recorded yet, so the read-back has nothing to read.
  - `TheReadBackRunsWhenTheHostBindsTests.cs:280` (`RecordTwoAttemptsAsync`): the same reason.
  - `TheReadBackRunsWhenTheHostBindsTests.cs:197`: a reopen that binds before construction **on purpose**. Ruling 172(2) requires the read-back "whether that is before or after construction", and this is the one test of the before case and of once per bind. The per-file guard hid this site, and the per-site guard surfaced it. It is named, not converted, so that coverage stays.
- **The product never binds first.** `src/` has no `.BindHost(` call; its one bind is `BindHostWhenOpen` at `WorkbenchShell.cs:3596`. So no exclusion rests on "bind-first is the product order".
- **Mutation red (`check3-mutation-red.txt`).** With 69452f35's `src/` changes reversed, the three converted files fail 7 of 14: every reopen test in them. The 7 that pass are the first-open and static tests. `src/` was restored before the green commit.

## Node 3: Ruling 175 (a Send while the turns restore)

| Check | Ruling 175 | Tests (exact, in `TheReadBackRunsWhenTheHostBindsTests` unless named) | Red | Green |
|---|---|---|---|---|
| A | (1), (3), (5) | `ASendWhileTheTurnsRestore_KeepsTheWordsAndSaysWhyItWaits_ThenAdmitsExactlyOneTurnAsB3_AndClears`; `TheRestoringSentence_IsOneDesignCopyRow_CitingRuling175`; `NoDispatchOutsideTheHostTests.ATurnSentBeforeTheHostOpens_WaitsForIt_AndRunsAsAnAttempt` (re-scoped at :47) | `checkA-red.txt` at 7d07d8ed (`""` vs `"third words"` / `"say ok"`; no DESIGN row) | `checkA-green.txt` at f796d727 (16/16) |
| B | (2) | `ASendWhoseReadBackFails_KeepsTheWords_AndShowsTheFailuresOwnCause_AndTheRetryRestoresThenSendsB3`; `ASendWhoseHostOpenFails_KeepsTheWords_AndShowsTheRefusalsOwnCause_AndSendsThemAfterTheRetry` | `checkB-red.txt` at 7954c4ea (read-back failure: the turn was admitted ahead of the unrestored turns); `checkB-mutation-red.txt` (host refusal, with the branch forced to admit) | `checkB-green.txt` at e16d2fcd (27/27) |
| C | (4) | not started: stopped at the Leader's kick | — | — |

- **What changed (A).** `SessionDocumentSurface.Launch` no longer calls `Composer.BeginNextTurn()` before admission. While the read-back is pending, `_waitingForTheReadBack` feeds the surface's one `ShowHostRefusal`: Send is disabled, and the status and Send's UIA help text are `SessionDocumentSurface.RestoringTurns`. The editor clears in `Admit`, so only once the turn has joined after the restored turns. The const's words are the DESIGN.md copy row (the "Copy added by this section" list, after :1218). The motion table is at :1204, and the nearest copy rows are 1213-1225; assume: that list is the row "beside line 1204" that the ruling means. A test pins the row against the const.
- **The double press (A, item 3).** The second `Send()` returns null because Send is disabled. The gate's own "already sent" refusal is a second stop, because the block is not reopened until admission.
- **What changed (B).** A failed read-back is now the bound host's `HostRefusal`: `NotebookRuns.CannotHost(cause)`. It is Ruling 159's open-failure sentence, extracted so the open failure and the read-back failure share one definition, with no new copy. `CanRetryHost` is true for it, and the existing Retry (`RetryHostAsync`) reads back again for the same bind. If the host refused, or the read-back failed, a pending Send is not admitted. The words stay, the gate's block reopens (`Gate.NextBlock()`), and the status shows that cause.
- **Inferred, not tested: the word "open".** The read-back failure reuses Ruling 159's words, so its sentence ends "Retry to open the host again". The Retry re-runs the read-back, not the open. Exact words for this case would be new copy, which needs the Owner and a DESIGN.md row.
- **Residual (not tested).** Because of the post in `Launch`, the composer writes "sent" for one dispatcher turn before the restoring sentence replaces it. A screen reader may hear "sent" first, then the restoring sentence, which is assertive. The clean fix is one line in `ComposerSurface.SendThroughTheGate`, which is not granted.
- **Residual (not tested).** If the user edits the words while they wait, the admitted turn carries the words as sent, and the edit is cleared at admission.

## The one edit outside the brief's line ranges

`SessionDocumentSurface.LastReadBack`, at its declaration (`src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs:691`), is now `internal Task LastReadBack => Notebook.Runs.ReadingBack;`. Before, it was a settable property assigned at construction. The read-back's start moved into `NotebookRuns.ReadBackOnEachBind` (:170-175 of the surface), so the surface no longer owns the task and reads it from the notebook. Node 1 reported this as the one line outside the brief's two ranges (`handoff.md`). The brief's ranges are in node 1's dispatch prompt, which is not on disk, so this pack cites the report and does not re-derive it.

## Whole suites

**Node 3, at e16d2fcd** (`suite-green.txt`): green except one environmental test.

- Core: exit 0. 3024 passed, 0 failed, 1 skipped (`suite-core-n3.txt`).
- App: exit 1. 1139 passed, 1 failed, 2 skipped, 1142 total (`suite-app-n3.txt`).
  - `NoDispatchOutsideTheHostTests.ATurnSentBeforeTheHostOpens_WaitsForIt_AndRunsAsAnAttempt` is **green**. Node 2's regression is closed by Ruling 175(5): line 47 is re-scoped in check A.
  - **Environmental:** `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held". It fails alone too (`chord-alone-n3.txt`), with the same NOT CHECKED cause as node 2 and the WL-1a acceptance.

Node 2's run, at 4be08710, is kept in `suite-run.txt`, `suite-core.txt` and `suite-app.txt`: App had 1134 passed and 2 failed (the NoDispatch regression and the same chord test).

## Residual risk

- **Not observed: the running app.** Check 1 drives the shell's revive in process. The human demo's restart step (`../../workbench-loops-wl-1a.md`, step 18) is the GUI proof, and it is owed to the user.
- **Closed by Ruling 175 (node 3, check A):** a Send made while the host opens now keeps its words, with Send disabled and the restoring sentence shown, until the row joins after the restored turns.
- **Not measured: how long the window lasts (Ruling 175(4), check C).** No host-open or read-back duration is emitted yet. The seam for it is `WorkbenchDiagnostics.Write`, the Shell lane's one writer, which `ThreadDiagnostics` also uses. Check C is node 4's work.
- **Inferred: the guard is textual.** It sees `.BindHost(` within 3 lines of `new SessionDocumentSurface(`, in files that name `LastReadBack` or `Runs.ReadBack`. A test that binds through a helper defined in another file is not seen.
