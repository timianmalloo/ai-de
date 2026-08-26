# Spike result — roslyn-msbuild-workspace (Phase-2 spike S2)

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303 and 10.0.301 both installed ·
  Roslyn 4.14.0 · `Microsoft.Build.Locator` 1.9.1
- **Command:** `dotnet run --project spikes/roslyn-msbuild-workspace`
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt)

## The two questions, and why the second one gates the phase

The Phase-2 design states them as: *does a real solution load without the host SDK matching exactly,
and can analyzers/generators be disabled?* The first is a feasibility question. The second is a
**security** question, and it is the reason implementation was gated behind this spike.

AI-DE's stated posture is that repository content is **untrusted data**. Symbol names, doc comments
and provenance arrive as inert typed values, and `P1-MCP-INERT` tests that they never become
instructions. A source generator is different in kind: it is not data the extractor reads, it is
**code the extractor runs**, with the extractor's privileges, at the moment a workspace is compiled.
If that cannot be turned off, opening a workspace executes whatever the repository author wrote, and
no amount of careful string handling closes that hole.

## How execution was made observable

The presence of an `AnalyzerReference` in the project model proves only that a reference was *read*.
Execution and mere reference are indistinguishable from the project model, and only execution
matters. So the fixture generator writes to a **sentinel file** — a side effect outside the
compilation, which no generated syntax can fake — and records its **process id**, because a
generator run by a child MSBuild node is a materially different finding from one run inside our own
process.

## Findings

### 1. A real solution loads against a NON-matching, older SDK — cleanly

Bound deliberately to the **oldest** installed SDK (10.0.301) and asked to load `AiDe.sln`, which was
built with 10.0.303:

```
Loaded 5 project(s) in 1.4s
  AiDe.App          docs=16  refs=206  analyzers=8  types=7802  compilation=ok
  AiDe.App.Tests    docs=12  refs=167  analyzers=8  types=3761  compilation=ok
  AiDe.Bench        docs=6   refs=167  analyzers=8  types=3829  compilation=ok
  AiDe.Core         docs=25  refs=167  analyzers=8  types=3826  compilation=ok
  AiDe.Core.Tests   docs=18  refs=167  analyzers=8  types=3842  compilation=ok

WorkspaceFailed diagnostics: 0
```

Zero diagnostics, full semantic content, 1.4 seconds. **Verified** for the stated case: same major
SDK band, patch-level mismatch, WPF and class-library projects. It is *not* evidence for a
cross-major mismatch or for a repository whose SDK is absent entirely — those remain unmeasured.

### 2. The threat is REAL: repository-authored code executed inside our own process

```
sentinel after OpenProjectAsync : silent
sentinel after GetCompilation   : FIRED [Initialize,PostInitialization,SourceOutput] pid=18756 (OURS)
sentinel after GetSourceGenerated: FIRED [...] (generated documents: 2)
```

Two details matter more than the headline:

- **The trigger is compilation, not load.** `OpenProjectAsync` is silent; `GetCompilationAsync()`
  fires the generator. That is the ordinary call an extractor makes to get symbols — there is no
  "just read the project" path that avoids it.
- **It ran in our process** (`pid=18756`, the host's own), not in a child MSBuild node. So this is
  arbitrary repository code with the extractor's privileges, its file handles, and its network access.

Also worth recording: **eight analyzer references arrive from the SDK itself** before the fixture
adds a ninth — `NetAnalyzers`, the interop generators, `System.Text.Json.SourceGeneration`,
`System.Text.RegularExpressions.Generator`. Every ordinary .NET project already carries executable
analyzer code. The hostile case is not exotic; it is the normal case with different intent.

### 3. MSBuild properties DO NOT suppress it

`RunAnalyzers=false`, `RunAnalyzersDuringBuild=false` and `EnforceCodeStyleInBuild=false` passed to
`MSBuildWorkspace.Create(properties)`:

```
analyzer references after load : 9   (unchanged)
sentinel after GetCompilation  : FIRED [...] pid=18756 (OURS)
```

**No effect whatsoever.** This is the finding that most changes the Phase-2 design, because these
properties are the obvious mitigation and the one a reviewer would assume was in place. They govern
the *build*; they do not govern what `MSBuildWorkspace` puts in the project model.

The deeper reason to reject them even if they had worked: they are **the repository's own build
configuration**. A control that a hostile repository can influence is not a control.

### 4. Stripping `AnalyzerReferences` at the Roslyn layer suppresses it completely

```
analyzer references after strip : 0
sentinel after GetCompilation   : silent
sentinel after GetSourceGenerated: silent   (generated documents: 0)
```

`solution.WithProjectAnalyzerReferences(projectId, [])` before any compilation is requested. Silent
through both drive points. This control is **ours outright** — it is applied after load, in our
process, and depends on nothing in the repository cooperating.

### 5. What the control costs, measured

| | Unsuppressed | Analyzer references stripped |
|---|---|---|
| Source-declared types visible | 4 — `Order, Customer, OrderLine, Sentinel` | 3 — `Order, Customer, OrderLine` |
| Generated documents | 2 | 0 |
| Compilation errors | 0 | 0 |

The control costs **exactly the generated symbols and nothing else**. Hand-written semantics survive
intact and the compilation still succeeds. That is the trade in one line: AI-DE sees what a human
wrote, and does not see what a generator would have produced.

### 6. `RS1035` is a convention, not a control — worth knowing before anyone relies on it

The fixture generator initially failed to compile: `RS1035: The symbol 'File' is banned for use by
analyzers`, plus the same for `Process` and `Environment`. Roslyn does ban file IO in analyzers.

But it is a **compile-time lint the generator's own author opts into**, via
`EnforceExtendedAnalyzerRules`. Setting that property to `false` — one line, in the attacker's own
project — removed every error and the generator did file IO freely. It constrains the well-behaved
and nobody else, and the *consumer* gets no say in it at all. Anyone reasoning "Roslyn forbids
analyzers from touching the filesystem" is relying on something that is not load-bearing.

### 7. Supply chain: ten high-severity advisories on the first Phase-2 dependency

`Microsoft.CodeAnalysis.Workspaces.MSBuild` 4.14.0 transitively resolves
`Microsoft.Build.Tasks.Core` 17.7.2, carrying **GHSA-h4j7-5rxr-p4wc** (high). Pinning forward to
17.14.8 clears that one and lands on **GHSA-w3q9-fxm7-j8fq**, plus eight advisories against
`System.Security.Cryptography.Xml` 9.0.0.

The repo's `TreatWarningsAsErrors` turned `NU1903` into a build failure, which is `P1-SUPPLY`'s
concern working before `P1-SUPPLY` exists. In this spike the references are compile-time only
(`ExcludeAssets="runtime"`; MSBuild loads from the SDK at runtime) and nothing ships, so `NU1903` is
suppressed **scoped to the spike project** with that reasoning recorded inline. **This is not
resolved for the shipped extractor** and is carried forward as a Phase-2 finding for the Security &
Identity Architect.

## Verdict

**S2 clears, with its mitigation changed.** Both questions are answered: a real solution loads
against a non-matching SDK, and generator execution *can* be suppressed — but **not** by the means
the Phase-2 design named. The design's mitigation must be rewritten from "disable analyzers and
generators via MSBuild properties" to "strip `AnalyzerReferences` from the loaded solution before
requesting any compilation", pinned by a negative test that fails if a generator ever executes
during extraction.

## What this changes for spike S1

S1 asks: *are generated symbols visible, and distinguishable from hand-written ones?* Under the
control this spike mandates, **generated symbols are not present at all** — finding 5 measured their
absence directly. So S1's question is no longer "can they be labelled" but "is their absence
acceptable, and how is it disclosed to the user".

That is a **spec-visible** outcome, not an implementation detail: a user asking "what implements
`IFoo`" in a repository that generates implementations will get an answer that is correct about
hand-written code and silent about the rest. S1 should be re-scoped before it runs.

## Residual risk

- **Only two SDK patch versions were exercised**, both 10.0.x. A cross-major mismatch, or a
  repository pinning an SDK that is not installed, is unmeasured.
- **The absence of execution is evidence for the drive points probed** — `GetCompilationAsync` and
  `GetSourceGeneratedDocumentsAsync`. A future code path that constructs a generator driver by
  another route would not be covered by this observation, which is why the negative test belongs in
  the extractor's own suite rather than only here.
- **One fixture generator, benign by construction.** It proves the capability exists; it does not
  enumerate what a real hostile generator could reach from that position.
- **`MSBuildWorkspace` still shells out to MSBuild evaluation** to load projects. This spike did not
  probe whether repository-authored MSBuild *tasks* execute during evaluation — a separate trust
  boundary, and an open question.
