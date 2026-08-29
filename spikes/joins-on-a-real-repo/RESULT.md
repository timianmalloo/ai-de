# Spike — the Joins and Contexts panes, on a real repository

**Runs 2026-08-28 and 2026-08-29** · `C:\Projects\TheTerrace` · the same projections the panes run,
over the same store the daemon writes
**Re-run:** `dotnet run --project spikes/joins-on-a-real-repo [-- <repo> <store-dir>]`

## Why this exists

Four turns of extractors, projections and panes shipped without anyone asking the only question that
matters: on an actual codebase, is any of this any good? A pane that renders correctly and says
nothing useful is indistinguishable from one that works, right up until a user opens it.

It found four defects. Three were in the product; one was in this spike, and it produced a confidently
wrong write-up before it was caught.

---

## Defect 1 — 7,426 joins with a fabricated reason

First run:

```
7426 verified · 59 inferred
       TheTerrace.Components.Display  --depends_on->  string
         declared in the resource's dependsOn
```

**`depends_on` is not a Bicep word.** The C# extractor emits it for type dependencies. The join was
written against the *predicate* rather than the kind of thing the predicate was on, and its basis was
a fixed string that never had to agree with the evidence again.

It failed in the flattering direction — the largest Verified count the pane had ever shown.
Registered as **DC-022**, fixed by requiring the subject to carry `resource_type`.

## Defect 2 — this spike's own harness, and the wrong conclusion it produced

The first write-up concluded *"there is no Bicep in TheTerrace at all"*, from `scopes: 5 of 7` and
zero infrastructure predicates. Both scopes that failed were Bicep.

The cause was here, not in the product:

```csharp
new CompositeExtractor(new CSharpExtractor(), new BicepExtractor(), new EfSchemaExtractor())
//                                            ^ this is the `fallback` parameter
```

Passed positionally, `BicepExtractor` landed in the fallback slot and every `bicep:` scope was routed
to the **schema** extractor, which failed them. `TheTerrace/infra/` contains `main.bicep` and
`provider-vault.bicep`, 24 resource declarations between them.

Fixed with named arguments. **A harness defect and a product defect look identical from the output** —
the tell was that the failures were named after a facility the repository visibly has.

With it fixed: **7 of 7 scopes, 12,043 assertions, 0 failures.**

## Defect 3 — a Cartesian product presented as 192 findings

With Bicep actually extracting:

```
── Inferred ── 251 edge(s)
        192  hosted_on
```

64 tables × 3 `Microsoft.Sql/*` resources. Every table was joined to a **server**, a **database**
*and* a **virtual-network rule**, each edge claiming *"the only literally-named SQL resource in this
template"* — of which there were three, so the sentence was false 192 times. DC-022 again, in the
join immediately below the one that produced it.

A table lives in a **database**. Narrowed to `Microsoft.Sql/servers/databases`, and:

- exactly one candidate → join, with a basis derived from that count;
- more than one → **no edges** and a `sql-database-ambiguous` disclosure, because which database
  holds a table is a question the evidence does not answer, and answering it twice is worse than not
  answering.

Result: **192 → 64**, one per table.

## Defect 4 — coverage counted artifacts the map was never about

```
  uncovered: 525 symbol(s), by namespace:
       362  TheTerrace.Tests
       114  (no namespace)
             bicep:main#appInsightsName
             bicep:main#backupStorageRedundancy
```

The `(no namespace)` bucket was **Bicep parameters**. A bounded-context map is a statement about a
codebase's domain; counting a template's parameters against it blames the map for artifacts it was
never about — and the number gets *worse* the more infrastructure a team writes. Coverage is now
judged against code symbols only (`BoundedContextReader.IsCodeSymbol`), one rule in one place because
two callers already needed it. **525 → 412**, and the phantom bucket is gone.

---

## What the panes now show

| | |
|---|---|
| Scopes | 7 of 7, 12,043 assertions, 4.5s |
| Verified joins | **1** — `invitationPepper` declared `@secure()`, value never read |
| Inferred joins | 123 — 64 `hosted_on`, 59 `maps_to` |
| Disclosures | 6, including `bicep-expressions-not-evaluated` and `schema-from-migrations-not-database` |

One verified join is the honest headline: this repository declares no `[Table]` attributes, so every
code→schema edge rests on EF's naming convention, and the pane says so on every row.

## The contexts pane, and the finding worth acting on

```
     516 symbols ·     902 internal ·    190 crossing   Football
     198 symbols ·     225 internal ·    172 crossing   Operations
     149 symbols ·     185 internal ·      74 crossing  Editorial
     145 symbols ·     228 internal ·      93 crossing  Assistant
      85 symbols ·     104 internal ·      41 crossing  Membership
```

Operations looked like a boundary that never held: 172 crossings against 225 internal edges. Opening
the crossing says otherwise.

```
        72  Football -> Operations
              57x  TheTerrace.Infrastructure.Data.AppDbContext
               2x  TheTerrace.Features.Chronology.IPendingWorkQueue
```

**57 of the 72 are one class.** `AppDbContext` sits inside Operations' `TheTerrace.Infrastructure.*`
pattern, so every repository that touches the database registers as a domain boundary crossing. That
is shared persistence, not coupling between Football and Operations.

### The recommendation, measured rather than asserted

`proposed/bounded-contexts.yaml` moves `TheTerrace.Infrastructure.*` out of Operations into a
**Platform** context. It was run through the same projection (`-- <repo> "" <map>`), so this is a
measurement of the proposal, not an opinion about it:

| | Operations before | Operations after |
|---|---|---|
| Symbols | 198 | 109 |
| Internal edges | 225 | 170 |
| **Crossings** | **172** | **47** |

`Football → Operations` falls from **72 to 15**. The new Platform context carries 161 crossings
against 37 internal edges — which is what shared infrastructure looks like, now labelled as such
instead of being mistaken for a domain boundary that failed.

Operations was never the problem. **It was a boundary that mostly holds, wearing the ORM's traffic.**

The other crossings survive the change and are real: `IAiCompletion` and `IPromptMapper` reaching
from Football and Editorial into Assistant, `ICoachReader`/`ISquadReader` reaching the other way.

**This is TheTerrace's call, and nothing here has been applied to that repository.** The proposed map
is committed to this one so the numbers above can be reproduced.

The remaining uncovered symbols are led by **362 tests**, which no context map should claim.

---

## A second repository — 2026-08-29

`C:\Projects\BioHacker`, chosen because it is a different shape: 20 projects, 26 Bicep resources,
and no Entity Framework at all.

```
scopes     : 25 of 25 indexed in 3.3s
assertions : 2,405
0 verified · 0 inferred
```

**Zero joins is the right answer** and the pane says so: with no `DbContext` there are no tables, so
there is nothing for code or infrastructure to be joined to. The three disclosures it prints are the
reasons.

### The finding: absence rendered as perfect coverage

BioHacker has no `docs/bounded-contexts.yaml`. The contexts pane reported:

```
  uncovered: 0 symbol(s)
```

and the surface said **"Every declared symbol belongs to a context."** That is the sentence a
fully-mapped codebase produces. The arithmetic was right — no map means no uncovered list — and that
is exactly why a cleverer count could not have fixed it. `ContextMapView` now carries `IsDeclared`,
and a workspace with no map is told so in its own words instead of being congratulated.

**One repository could not have found this.** TheTerrace has a map, so every code path that runs when
there is none had never been exercised against real evidence.

---

## A third repository — 2026-08-29

`C:\Projects\meridian-finance-planner`. 10 scopes, 4,308 assertions, 2.8s — and the first
repository whose Bicep actually uses `dependsOn`.

```
6 verified · 56 inferred

── Verified ── 6 edge(s)
       bicep:private-network/sqlDnsZoneGroup  --depends_on->  bicep:private-network/dnsZones
         declared in the resource's dependsOn
```

**The DC-022 collision is live here, not hypothetical.** The predicate census reports:

```
     2,310  depends_on                    bicep, csharp  <-- SHARED
```

On TheTerrace `depends_on` was C#-only, so the fix could only be checked against tests. Here both
producers emit it in one store, and the qualifier is the only thing keeping 2,304 C# type
dependencies out of a pane that would have reported every one of them as *"declared in the
resource's dependsOn"*. Six is the right answer, and each of the six is a real `dependsOn` between
two real resources.

The joins pane's positive half is now measured as well as tested — which matters, because narrowing
a join until it can no longer fire is the failure mode a negative-only test cannot see.

### Incremental re-index, measured

```
scopes     : 10 of 10 indexed (0 reused) in 2.8s
re-index   : 0 indexed, 10 reused in 0.1s
```

TheTerrace: **4.3s → 0.1s**. The reuse is counted separately from the indexing, because "10 of 10
indexed" would be a true sentence about a run that read nothing.

---

## A fourth repository, chosen for what it lacks — 2026-08-29

`C:\Projectsi-forward`: 63 Python files, 40 TypeScript, no C#, no Bicep, no migrations.

```
scopes     : 0 of 0 indexed (0 reused) in 0.0s
assertions : 0
disclosed  :
```

Every number correct, and **indistinguishable from an empty directory**. "Nothing here" and "nothing
I can read" rendered identically, and the disclosure list — the mechanism whose entire job is to say
what was not read — was empty.

That is the **third repository in a row** to find the same shape: a missing context map read as
perfect coverage, a bounded search read as the whole workspace, unreadable source read as no source.
Each time the arithmetic was right and the claim was false, which is why none of them could have been
fixed by counting more carefully.

`UnanalysedLanguages.Survey` now names what is present and unread:

```
disclosed  : javascript-not-analysed (27 file(s)), python-not-analysed (63 file(s))
```

The count is part of it, because "some Python" and "10,760 Python files" are different statements
about how much of a repository the graph is silent on. Vendored directories are excluded — a number
about `node_modules` is a number about somebody else's code. And a C#-only workspace discloses
nothing, because a disclosure that fires everywhere is noise.

### Do the panes agree with the store now?

The caps fix is checkable, so it is checked on every run:

```
store vs pane, joins:
  verified   store      1   pane      1
  inferred   store    123   pane    121
  only in store : 2      only in pane : 0
```

**122 of 124 edges agree.** The two missing both involve `PlayerSeasonStat` and are cut by the
50-neighbour describe cap — which the shortfall line already reports on the same run. Before the fix
the panes read 50 nodes of 2,164; the honest residual is now two edges and a sentence saying so.

---

## Scale — 2026-08-29

Nothing available here is much larger than TheTerrace, so this is a **synthetic** workspace: 20
projects, 2,400 types, cross-project references. Labelled synthetic because it is, and reported
because the paging, the caps and the fingerprint cache were all sized on repositories a fifth this
size.

```
scopes     : 20 of 20 indexed (0 reused) in 13.5s
re-index   : 0 indexed, 20 reused in 0.1s
assertions : 21,066
pane read  : 21,066 assertion(s) over 11 page(s) in 185ms via the query path
store read : 21,066 assertion(s) directly
shortfall  : (none — the panes see the whole workspace)
```

| | |
|---|---|
| First index | **13.5s** — about 0.68s per project of 120 types, roughly linear |
| Re-index, nothing changed | **0.1s**, 20 scopes reused |
| Paged read of everything | **185ms** over 11 pages, exact agreement with the store |
| Shortfall | none — no cap bit at 21,066 assertions |

### Where the extraction time actually goes — profiled 2026-08-29

`AIDE_EXTRACTION_TIMING=1` splits each scope into the READ phase (parse the sources, build the
compilation, resolve references) and the WALK phase (visit the symbols and emit assertions):

```
[timing] csharp:Proj00: read 567ms, walk 260ms, 1,076 assertion(s)   <- first scope, JIT included
[timing] csharp:Proj04: read 607ms, walk   7ms, 1,070 assertion(s)
[timing] csharp:Proj07: read 505ms, walk   6ms, 1,077 assertion(s)
```

**Roughly 98% of the cost is the read**, and the walk is 6–15ms once the JIT is warm. The first
scope's 260ms walk is JIT, not work — reading the total without the split would have attributed it
to the walk and sent the next optimisation at the wrong half.

### Splitting the read — and correcting the conclusion it first produced

The read was split again, and the **first split was wrong because the timer was**. It wrapped
`File.ReadAllText` *and* `ParseText` together and reported the total as "parse":

```
[timing]   parse 446ms for 120 file(s)      <- WRONG: disk and parse bundled
```

From which the conclusion "parsing is ~97% of the read, so cache the trees" followed — plausible,
confident, and not what the machine was doing. Timed apart, on freshly written files:

```
[timing]   read 603ms, parse 39ms for 120 file(s)     <- first scope, JIT in the parse
[timing]   read 595ms, parse  4ms for 120 file(s)
[timing]   read 690ms, parse  4ms for 120 file(s)
```

**Disk I/O is ~99% of the read**, and parsing is 4–5ms. Since the read is ~98% of extraction, **file
I/O is roughly 97% of everything extraction does** — the opposite half from the one the bundled timer
pointed at. Later scopes in the same run show `read 2ms`, so the 600ms figures are **cold** first
touch; the OS file cache warms during the run, and this cost is environment-dependent (a scanner on
the temp directory would show exactly this shape).

### The correction that matters: the synthetic benchmark was measuring itself

Both conclusions above are **artefacts of the workload**, and the same profiler on a real repository
says something else entirely. TheTerrace, 463 files in its main scope:

```
[timing]   enumerate 6ms, read 53ms, parse 694ms for 463 file(s)
[timing] csharp:TheTerrace:net10.0: read 848ms, walk 1167ms, 8,336 assertion(s)
```

| | Synthetic (fresh files, trivial types) | TheTerrace (real) |
|---|---|---|
| read | ~500ms — "97% of extraction" | **53ms, about 3%** |
| parse | 4ms | **694ms** |
| walk | 6–15ms | **1,167ms — the largest single cost** |

Two independent flaws in the synthetic workload, each inverting a conclusion:

1. **The files were newly created.** A fresh .NET process reading them took 493ms; a *second* fresh
   process over the same files took 6ms, and the repository's own source reads at 0.19ms/file. The
   500ms was a one-time, system-wide, per-file first read after creation — the signature of on-access
   scanning — and it never recurs. It is not extraction's cost at all.
2. **The generated types were trivial.** No inheritance depth, no generics, four fields each. The
   symbol walk had almost nothing to do, so it looked free; on real code it is the *biggest* cost.

So the honest profile of extraction on real code is **walk > parse >> read**, which is the reverse of
what was published here twice. The tree cache still earns its place — parse is 694ms and the second
scope reused all 463 trees for 0ms — but it addresses the second-largest cost, not the largest.

**The generalisation, registered as DC-028:** a benchmark built to exercise a system measures the
benchmark unless its workload resembles the real one in the dimensions that drive cost. Both flaws
here were invisible in the numbers and obvious in the generator.

### What the tree cache is actually worth

`SyntaxTreeCache` was built on the wrong rationale and is right anyway: a cache hit skips the whole
factory, which is the disk read *and* the parse. Forced re-index in the same process, so the
fingerprint cache cannot answer:

```
[timing]   read 0ms, parse 0ms for 120 file(s) (720 reused, 720 parsed)
re-index   : 6 indexed (FORCED), 0 reused in 0.6s      <- was 1.0s
```

720 of 720 trees reused. This is the file-granularity case the scope fingerprint cannot cover: one
file edited in a project of a hundred and twenty, where the scope must be re-read and 119 files did
not move.

**The read is not the problem at this size; extraction is.** 185ms to page 21,066 assertions against
13.5s to produce them says the next scale work belongs in the extractor, not the query path — and the
fingerprint cache already means that 13.5s is paid once rather than per refresh.

The honest limit: this says nothing about a repository with deep inheritance, heavy generics, or
thousands of package references, because the generator produces none of those. It bounds the shape it
tested and no more.

## Consequence

| | |
|---|---|
| DC-022 | Two instances now, both in `JoinProjection`, both from joining on a predicate name |
| DC-022's residual | **Measured, then closed for this consumer.** `declared_in`, `has_type` and `discloses` are each emitted by all three extractors, and `has_type`'s object values partitioned by producer only by accident. Every `has_type` read in `JoinProjection` is now qualified by the SHAPE OF THE SUBJECT as well — `table:` prefix, `#` fragment, or a dotted code symbol — so the partition is enforced rather than relied on. Three tests, including that a real code type is still joined |
| The spike | Named arguments, and a predicate-by-extractor census printed on every run so the next collision is visible before it is joined |
| TheTerrace's map | One concrete recommendation, with the 57 edges that justify it |
