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

## The one edit outside the brief's line ranges

`SessionDocumentSurface.LastReadBack`, at its declaration (`src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs:691`), is now `internal Task LastReadBack => Notebook.Runs.ReadingBack;`. Before, it was a settable property assigned at construction. The read-back's start moved into `NotebookRuns.ReadBackOnEachBind` (:170-175 of the surface), so the surface no longer owns the task and reads it from the notebook. Node 1 reported this as the one line outside the brief's two ranges (`handoff.md`). The brief's ranges are in node 1's dispatch prompt, which is not on disk, so this pack cites the report and does not re-derive it.

## Whole suites

**Not green** (`suite-run.txt`; there is no `suite-green.txt`), at 4be08710.

- Core: 3024 passed, 0 failed, 1 skipped (`suite-core.txt`).
- App: 1134 passed, **2 failed**, 2 skipped (`suite-app.txt`).
  - **Regression from 69452f35:** `NoDispatchOutsideTheHostTests.ATurnSentBeforeTheHostOpens_WaitsForIt_AndRunsAsAnAttempt`. Line 47 asserts a turn row right after Send while the host opens. Under Ruling 172(3) that Send now waits for the bind's read-back before taking an ordinal, so the row appears after the open. It is red alone on the branch (`nodispatch-alone-on-branch.txt`) and green 8/8 with the fix reverted (`nodispatch-without-the-fix.txt`). Node 1's 18/18 run did not include this file. The file is outside T17d's owned paths: seam req-01M5A52AQHR32CPQFSBS9NGJ4T is open. It asks to change :47 only, keeping the no-direct-run (:49) and runs-as-an-attempt (:54-56) assertions.
  - **Environmental:** `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held". It is the same NOT CHECKED cause recorded at the WL-1a acceptance, and it fails alone too.
- **Do not join this branch before the seam is ruled and `NoDispatchOutsideTheHostTests` is green.**

## Residual risk

- **Not observed: the running app.** Check 1 drives the shell's revive in process. The human demo's restart step (`../../workbench-loops-wl-1a.md`, step 18) is the GUI proof, and it is owed to the user.
- **For the Owner (UX):** with the fix, in the product a Send made while the host opens shows no row until the open completes. Before, the row appeared at once and waited. The wait is what keeps b3 numbering (Ruling 172(3)), but the composer gives no sign of it. Not tested.
- **Inferred: the guard is textual.** It sees `.BindHost(` within 3 lines of `new SessionDocumentSurface(`, in files that name `LastReadBack` or `Runs.ReadBack`. A test that binds through a helper defined in another file is not seen.
