# Spike — the Joins pane, on a real repository

**Run 2026-08-29** · `C:\Projects\TheTerrace` · the same projections the pane runs, over the same
store the daemon writes
**Re-run:** `dotnet run --project spikes/joins-on-a-real-repo [-- <repo> <store-dir>]`

## Why this exists

Four turns of extractors, projections and panes shipped without anyone asking the only question that
matters: on an actual codebase, are the joins any good? A pane that renders correctly and says
nothing useful is indistinguishable from one that works, right up until a user opens it.

## What it found first: a defect, in the join added the previous turn

```
7426 verified · 59 inferred

── Verified ── 7426 edge(s)
      7,426  depends_on
       TheTerrace.Components.Display  --depends_on->  string
         declared in the resource's dependsOn
       TheTerrace.E2ETests.BlazorCircuit  --depends_on->  Microsoft.Playwright.IPage
         declared in the resource's dependsOn
```

**`depends_on` is not a Bicep word.** The C# extractor emits it for type dependencies — 7,426 of them
in this repository — and the join was written against the *predicate* rather than against the kind of
thing the predicate was on. Every one of those edges was reported **Verified**, with the basis
*"declared in the resource's dependsOn"*.

There is no `dependsOn` anywhere in TheTerrace. There is no Bicep in it at all. The sentence was
false 7,426 times, next to a number large enough to look like the feature working.

Two things made it dangerous rather than merely wrong:

- **It fails in the flattering direction.** A join that produced nothing would have been investigated
  on sight. This produced the largest Verified count the pane has ever shown.
- **The basis is a fixed string.** It was written once, beside a predicate name, and never had to
  agree with the evidence again — so nothing in the code could disagree with it. Registered as
  **DC-022**.

Fixed by requiring the subject to be a declared resource (`resource_type`). Re-run:

```
0 verified · 59 inferred
```

Zero is the correct answer for a repository with no infrastructure templates.

## What the pane actually shows on this repository

| | |
|---|---|
| Scopes indexed | 5 of 7, in 4.9s |
| Assertions | 11,836 |
| Verified joins | **0** |
| Inferred joins | 59, all `maps_to` |
| Disclosures | `build-conditions-not-evaluated`, `generated-code-not-analysed`, `schema-changed-by-raw-sql-not-read`, `schema-from-migrations-not-database` |

The 59 inferred joins are EF's naming convention read backwards — `AiRoute` → `table:AiRoute`,
`Competition` → `table:Competition`. They look right, and the pane says plainly that nothing declares
them. **Zero verified is the honest headline**: this repository declares no `[Table]` attributes, has
no Bicep, and the schema is read from migrations rather than a database. The pane is not
under-reporting; there is nothing stronger to report.

## The contexts pane, same run

```
     516 symbols ·     902 internal ·    190 crossing   Football
     198 symbols ·     225 internal ·    172 crossing   Operations
     149 symbols ·     185 internal ·     74 crossing   Editorial
     145 symbols ·     228 internal ·     93 crossing   Assistant
      85 symbols ·     104 internal ·     41 crossing   Membership

  top crossings:
        72  Football -> Operations       37  Operations -> Football
        27  Assistant -> Football        23  Football -> Assistant
```

Every crossing is under the 200 member cap, so the drill-down lists all of them — the "and N more"
path is untested by this repository and remains covered only by its unit test.

**Operations is the finding.** 198 symbols carrying 172 crossings against 225 internal edges: nearly
as much traffic leaving it as staying inside. Football, four times its size, keeps 902 edges internal
against 190 crossing. Whether that means Operations is a shared kernel doing its job or a boundary
that never held is a question for the person who drew the map — which is the point of showing the
number rather than scoring it.

## Uncovered symbols

```
  uncovered: 474 symbol(s), by namespace:
       360  TheTerrace.Tests
        65  (no namespace)
        16  TheTerrace.Features.Playground
        11  TheTerrace.Features.Rehearsal
```

**360 of the 474 are tests**, which no context map should claim. The grouping turned "474 uncovered"
from a number that reads as a gap into one line that reads as *correct*, and four small namespaces
that are worth a decision. Coverage as a percentage would still say 68%; it would just say it about
the wrong thing.

`(no namespace)` at 65 is worth a look on its own — those are symbols the extractor recorded with no
dot in their name, and whether they are top-level programs, generated types, or an extraction defect
is not yet known.

## Consequence

| | |
|---|---|
| `depends_on` join | **Fixed and pinned.** Two tests: a code-origin `depends_on` is not joined; a resource-origin one still is |
| DC-022 | Registered — a predicate shared by two extractors, joined as if it had one meaning |
| The Joins pane | **Honest on a real repository.** 0 verified is the right answer here and says so |
| Contexts | Usable. Operations is the boundary worth examining |
| Next | `(no namespace)` × 65, and the 2 scopes of 7 that failed to index |
