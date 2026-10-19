# Brief — T27 `wl1b-currentness` (slice WL-1b, Ruling 178) · Claude Code · Sonnet · node 1

**Goal (Ruling 178, read it in `docs/notes/addendum-c-council-rulings.md`):** currentness gets **one** definition.
`NotebookModel.CurrentnessOf` (`src/AiDe.App/Workbench/Records/NotebookModel.cs:210`; its one caller is
`SessionDocumentSurface.cs:1560`) becomes a thin call to `CurrentnessFold` (`src/AiDe.Core/Workbench/Records/Folds/CurrentnessFold.cs`)
over the same inputs. The stale sentence keeps **one** const (today it is in both `NotebookModel.cs:67` and
`CurrentnessFold.cs:27`). The old body is deleted (HYG-A).
**Done when, each red then green:**
- **(A)** a test asserts that the notebook's stale notice and the checklist's evidence state (T23's `AcceptanceFold`, on
  this branch) come from the **same fold**. For one scenario that turns a result stale, both read Stale, through
  `CurrentnessFold`. A mutation of the fold flips both.
- **(B)** `CurrentnessOf`'s existing behaviour holds through the delegation, including the empty-draft clause and its
  `assume:` (`NotebookModel.cs:202-208`). Keep or move that clause **explicitly**. A behaviour change is a seam request,
  never silent.
- **(C)** one stale-sentence const, and no second copy anywhere in `src/` (a test or a grep-based guard).
**The bound (Ruling 178):** the two definitions take different inputs today. The App compares the draft's body sha with
the ran revision's; the fold takes `(AttemptRow, currentRevisionNo, liveProjectionSha)`. If the fold needs an input that
`NotebookModel` cannot supply without a **new store read or IPC**, **stop** and send a seam request. It names what is
missing, with file:line. Ruling 178's option B (an agreement test) is then the interim. Do not add the read.
Extending the fold's input (a pure Core change, such as an overload that takes the body shas) is allowed. State it in
handoff.md.
**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.

## Base
Your branch is off `wl1b-folds` (T23 n1 head `a81b0050`), so `AcceptanceFold` is present.

## Owned authored paths
`src/AiDe.App/Workbench/Records/NotebookModel.cs` (`CurrentnessOf` and `StaleCopy` only);
`src/AiDe.Core/Workbench/Records/Folds/CurrentnessFold.cs` (an additive overload or input only; its existing callers'
behaviour is unchanged); new `tests/AiDe.App.Tests/Workbench/Records/OneCurrentnessDefinitionTests.cs`; new or existing
`CurrentnessFold` tests under `tests/AiDe.Core.Tests/Workbench/Folds/`; `{PROOF}/**`. The caller at
`SessionDocumentSurface.cs:1560` is **read only**: if its call must change, send a seam request.

## UI tests
This track's App tests are model-level and must open **no shown window**. The UI-test slots are T21 and T28 (Owner's
fan-out note). Run the App suite only after checking the board (rules 19 and 20). A "desktop hold occupied" result is
re-run, never counted as a pass or a fail. Log each one in `{PROOF}/desktop-hold.txt`.

## Not in scope
The service (T24), the checklist kind (T26), and any change to the stale notice's copy or placement.
