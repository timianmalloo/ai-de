# Containment options for repository code execution during extraction

**Run 2026-08-28** · .NET SDK 10.0.303 · Windows 11 Pro 26200
**Re-run:** `dotnet run --project spikes/extraction-containment`
Uses the **same hostile fixture as D3**, so the numbers are directly comparable.

## The question

D3 established that `MSBuildWorkspace.OpenProjectAsync` executes repository-supplied code by four
vectors, two of which need nothing but the checked-in `.csproj`. Two containments were proposed and
both were **Inferred, unprobed**. This spike measures them.

## Result

| Option | Repo code runs? | Attack landed? | Extraction works? |
|---|---|---|---|
| Uncontained — today's design | yes | **YES — 4/4** | yes |
| **A1** job object only | yes | **YES — 4/4** | yes |
| **A2** low integrity + job object | yes | **no — 0/4** | **yes** |
| **B** no MSBuild, project read as data | **NO** | **no — 0/4** | **yes** |

Both A2 and B work. They are not equivalent, and the difference is not security.

## Option A — contain it

**A1, a job object alone, does not contain the attack.** All four markers still landed. A job object
bounds *lifetime* and *resources* — process count, memory, CPU, and kill-on-close — and bounds
nothing about what a process may write. It is necessary (it reaps whatever the repository spawns)
and nowhere near sufficient. The product already has this mechanism for terminals; reaching for it
here would have felt like a control and would not have been one.

**A2, low integrity plus the job object, blocks every vector.** The child runs on a token duplicated
from our own with its integrity label lowered to `S-1-16-4096`. Windows' mandatory policy is
NO_WRITE_UP: the child still *reads* the repository and the SDK it needs, and cannot *write*
anything at medium integrity.

**And extraction still works** — 3 documents, symbols resolved, 2155 ms against 2091 ms uncontained.
The attack fails **loudly**, as workspace diagnostics naming each blocked write:

```
The "InlinePwn" task failed unexpectedly.
System.UnauthorizedAccessException: Access to the path '…\marker-inline.txt' is denied.
```

That visibility is a real property: containment that surfaces as a diagnostic can be detected and
reported, rather than failing silently.

### The finding that nearly killed Option A

**The first run concluded that low integrity breaks extraction.** MSBuild died with
`UnauthorizedAccessException` on `…\Temp\MSBuildTemp*.j0b` — it writes to `TMP`/`TEMP` before doing
anything else, and a low-integrity child inherits the parent's `TEMP`, which it cannot write.

That reads as *"the sandbox is incompatible with MSBuild"* and would have eliminated the option. It
is actually *"the child had nowhere to write."* Giving it a low-integrity scratch directory and
repointing `TMP`/`TEMP` at it fixed it completely. **A containment that fails for a fixable
environmental reason is indistinguishable from one that cannot work** — the difference is one
experiment.

### What Option A does not establish

- **Network egress is not contained.** Low integrity does not block sockets. A hostile `Exec` can
  still reach the network — exfiltration of anything the child can *read*, which is the repository.
  Closing this needs AppContainer with capability SIDs, or a WFP rule. **Unmeasured.**
- Registry writes, named objects, and clipboard were not probed.
- The job's CPU/memory limits were set but never driven to their bounds; a runaway build was not
  tested.

## Option B — do not run it at all

The project file is parsed as XML for its source globs, references come from the SDK reference pack
and from `obj/project.assets.json`, and Roslyn compiles the result directly. No MSBuild evaluation,
so there is no path by which repository build logic can execute. Safety is structural rather than
enforced.

**Fidelity, on a real project (`src/AiDe.Core`, 49 documents):**

| | MSBuildWorkspace | Option B |
|---|---|---|
| Types recovered | 159 | **159 — 100%** |
| Sources | 49 documents | 46 |
| Wall clock | 2210 ms | **359 ms (6.2×faster)** |

The three-document difference is generated `AssemblyInfo`/`GlobalUsings` under `obj/`, which carry
no user-defined types — hence identical type counts. On this project Option B loses **nothing**.

### The caveat that decides it

**`project.assets.json` is data, but producing it is not.** Option B resolves package references by
reading that file — which exists only after `dotnet restore`, and **restore is itself MSBuild
evaluation**. On a freshly cloned repository the file is absent, and the probe reports exactly that.
On `AiDe.Core` it was present (4 package assemblies) because the repo had been built.

So Option B is fully safe only for a repository that has *already* been restored by someone else. A
first-open of a fresh clone either accepts degraded references, or runs a restore — which puts the
D3 threat straight back.

### What Option B does not establish

**Untested, and each could change the fidelity number:**
- `ProjectReference` — cross-project symbol resolution was not probed. `AiDe.Core` has none.
- Multi-targeting (`TargetFrameworks`): B reads one glob and one framework.
- Custom `Compile Include`/`Link` globs, shared projects, `Directory.Build.props` inheritance —
  B honours only `Compile Remove`.
- Anything requiring evaluated MSBuild properties (conditional compilation symbols, `DefineConstants`).

## How to read these two together

They answer different questions and are not alternatives on the same axis.

- **B is faster and structurally safe, and its fidelity is exact on a simple, restored project.** Its
  risk is *unknown fidelity* on the project shapes it has not met — and fidelity failures in an
  extractor are silent, producing an answer that is confidently incomplete.
- **A2 keeps MSBuild's exact semantics** — whatever the SDK resolves is what we see, with no glob to
  get wrong — at the price of running untrusted code inside a boundary that is strong for the
  filesystem and **unproven for the network**.

They also **compose**: B as the fast default, A2 as the fallback when B cannot resolve a project —
which keeps the common path off MSBuild entirely and contains the uncommon one.

## Consequence

Component 1 is no longer blocked on *whether* a containment exists. It is blocked on **which**, and
that is now a decision with evidence under it rather than three inferred candidates.
