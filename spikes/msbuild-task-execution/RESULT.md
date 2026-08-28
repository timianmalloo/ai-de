# Spike D3 — do repository-authored MSBuild *tasks* execute when we load a repository?

**Run 2026-08-28** · .NET SDK 10.0.303 · runtime 10.0.11 · Windows 11 Pro 26200
**Re-run:** `dotnet run --project spikes/msbuild-task-execution`
**Exit code is the result:** `0` = nothing executed · `1` = repository code executed · `2`/`3` = the probe is void.

## The question

S2 established that repository-authored **analyzers and source generators** can be prevented from
executing, by stripping `AnalyzerReferences` from the loaded solution. It left a different question
open, and the Phase-2 design recorded it as an unprobed trust boundary:

> `MSBuildWorkspace` still runs MSBuild **evaluation** to load projects, and whether that executes
> repository-supplied task assemblies was not tested.

The principle at stake is absolute: **loading a repository must never execute its code.**

## Answer: it does. All four vectors, every time.

```
  MARKERS AFTER OpenProjectAsync:
    [EXECUTED] exec        built-in Exec task in InitialTargets (no custom assembly needed)
    [EXECUTED] inline      RoslynCodeTaskFactory inline C# (no custom assembly needed)
    [EXECUTED] usingtask   repository-authored task assembly via UsingTask
    [EXECUTED] designtime  Exec hooked BeforeTargets on design-time targets
    => 4 of 4 vectors executed
```

`WorkspaceFailed` diagnostics: **0**. The project loaded cleanly — 3 source documents, 167 metadata
references, assembly name resolved. **Nothing about this looks like an attack from the caller's
side.** A host that logged workspace diagnostics and checked for errors would see a healthy load.

## Why this is worse than "a custom task can run"

The vector that matters is not the one that needs a committed DLL.

| Vector | Needs a prebuilt assembly? | What the attacker writes |
|---|---|---|
| `Exec` in `InitialTargets` | **No** | Four lines of XML in the `.csproj`. Runs an arbitrary shell command. |
| `RoslynCodeTaskFactory` inline task | **No** | C# inside a `<Code>` element. MSBuild compiles and runs it. |
| `UsingTask` → repo assembly | Yes | A committed `.dll`, or one pulled from a package. |
| `BeforeTargets` on design-time targets | **No** | Hooks the very targets the workspace drives to get references. |

**Two of the four need nothing but the project file.** The threat is not "a repository that ships a
malicious build tool" — it is **any repository, cloned and opened**. `Exec` alone is arbitrary
command execution as the user, at open time, before anything is displayed.

`InitialTargets` is the sharpest edge: it is an attribute on the `<Project>` element itself, so it
runs whenever MSBuild builds that project at all, including a design-time build.

## The probe is trustworthy, and here is why that needed proving

An "all clear" here would have been worthless without showing the instrument works — the failure S2
itself nearly shipped, where a missing language service made the workspace load **zero** projects
and a marker-counting probe would have read that as safety (defect class **DC-009**).

Two guards, both of which fired during development:

1. **Positive control (`PROBE 0`).** A real `dotnet build` of the same fixture must produce the
   markers first. **This caught a genuine bug in the first run**: `MarkerDir` resolved one directory
   too high, every marker was written somewhere the probe never looked, and the spike reported
   `0 of 4` — the *safe*-looking answer, for an entirely wrong reason. Without the control this
   spike would have concluded "MSBuildWorkspace does not execute repository code" and been believed.
2. **Non-vacuity guard (`PROBE 1`).** Absent markers count as "nothing executed" only if the project
   demonstrably loaded — asserted on document count, assembly name and reference count, not on the
   absence of an exception.

## What this does *not* establish

- **No mitigation was tested.** This spike answers only whether the threat is real. It is.
- Candidate containments below are **Inferred, not measured** — none has been probed:
  - Run extraction in a **sandboxed / low-privilege child process** (job object, restricted token,
    no network), treating MSBuild evaluation as untrusted by construction. Containment rather than
    prevention, and the only candidate that does not depend on MSBuild's cooperation.
  - **Evaluation-only** MSBuild (`ProjectCollection` without target execution) — but `MSBuildWorkspace`
    needs design-time builds to resolve sources and references, so this likely means abandoning it.
  - A **non-MSBuild** extraction path (parse the project file directly; or an indexer such as
    `scip-dotnet`), trading fidelity for not running a build at all.
- Nothing here says the *analyzer* control from S2 is wrong. It is correct and still needed — it is
  simply not sufficient, because it never covered this path.

## Consequence

**Component 1 (`P2-EXT-01..06`) is blocked pending a containment decision.** The extractor as
designed opens user repositories through `MSBuildWorkspace`; on this evidence, doing so executes
whatever the repository's project files say. That is a product-level security decision, not an
implementation detail.

Registered as defect class **DC-019** — *a trust boundary assumed safe because an adjacent control
was proven*.
