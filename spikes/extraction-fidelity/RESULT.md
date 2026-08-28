# Spike — Option B fidelity on the project shapes the first measurement never met

**Run 2026-08-28** · .NET SDK 10.0.303 · Windows 11 Pro 26200
**Re-run:** `dotnet run --project spikes/extraction-fidelity`

## Why this was owed

The containment spike scored Option B at **159/159 types on `AiDe.Core`** and called it 100%
fidelity. That number was not trustworthy as a general result, for two reasons — and **both turned
out to matter**:

1. `AiDe.Core` has no `ProjectReference`, no multi-targeting and no WPF.
2. **It counted types.** An extractor emits *edges*. A project reference that fails to resolve
   leaves every locally declared type present and correct, and silently turns every edge into it
   into an error type. A type count scores that as perfect.

## Result

| Project | Shape | Baseline edges | Option B edges | Edge resolution | Types lost |
|---|---|---|---|---|---|
| `AiDe.Core` | no ProjectReference | 3138 (0 bad) | 3138 (0 bad) | **100.0%** | 0 |
| `AiDe.App` | 2 × ProjectReference, WPF, `net10.0-windows` | 309 (0 bad) | 300 (0 bad) | **100.0%** | 0 |
| `AiDe.Daemon` | ProjectReference, `net10.0-windows` | 11 (0 bad) | 11 (0 bad) | **100.0%** | 0 |
| `MultiTarget` | `net10.0` + `netstandard2.0`, `DefineConstants` | 3 (0 bad) | 3 (0 bad) | **100.0%** | 0 |

Speed, same runs: Option B **46–74 ms** against MSBuildWorkspace's **796–1963 ms** — roughly
**25× faster**, because it never starts a build.

**Option B holds on every shape measured.** `ProjectReference` resolves (the referenced project is
compiled from source and added as a compilation reference), WPF resolves, and multi-targeting is
handled by extracting **one scope per (project, target framework)**.

## The number this spike first produced, and why it was wrong

The first run reported **82–89% edge resolution across the board** — including 356 bad edges on
`AiDe.Core`, the project previously called 100%. That looked like a real ceiling on the approach.

It was two defects **in the probe**:

**1. Implicit usings.** The SDK generates `GlobalUsings.g.cs` into `obj/`, and this extractor
deliberately does not read `obj/`. Without them, every `Console`, `Task`, `IReadOnlyList` and
`CancellationToken` in a modern project is an unresolved symbol — 404 × CS0246 and 329 × CS0103 on
`AiDe.Core` alone. Fixed by synthesising the documented implicit-using set from the project's own
`ImplicitUsings` property plus its explicit `<Using Include>` items. That is reading a published SDK
specification, not evaluating the project.

**2. Reference-pack ordering.** Both `Microsoft.NETCore.App.Ref` and
`Microsoft.WindowsDesktop.App.Ref` ship a `WindowsBase.dll`: the first is a `4.0.0.0` facade, the
second the real `10.0.0.0` assembly. Adding the base pack first let the facade win on filename, and
every WPF type then failed `CS1705` — *"uses a higher version than referenced assembly"* — **591 of
them**. Fixed by adding the desktop pack first.

**Neither was a limit of Option B, and both looked exactly like one.** A fidelity result is only as
good as the harness producing it — and had this spike stopped at its first number, the recorded
conclusion would have been "Option B loses ~15% of dependency edges" and the strategy would probably
have been reversed on the strength of it.

## What remains genuinely unresolved

**XAML-generated members are invisible.** `AiDe.App` reports `CS0103: The name 'InitializeComponent'
does not exist` — that method is generated into `obj/*.g.cs` from the `.xaml`, which Option B does
not read. It costs **no types and no edges** here, because the generated half of a WPF partial class
contains UI wiring rather than domain structure. **It must still be disclosed**, on the same footing
as absent generated symbols (S1): a projection over a WPF project is silent about XAML-generated
members rather than wrong about them.

`AiDe.Core` also reports `CS8795`/`CS0165` (partial-method and definite-assignment diagnostics), which
are artefacts of compiling without the generated half. They do not affect the symbol model.

**Package references still depend on a prior restore.** `MultiTarget` had no `obj/project.assets.json`
and the probe reported exactly that. Unchanged from the containment spike, and it remains the reason
the strategy is *disclose*, not *guess*.

## The scope grain this settles

`MultiTarget` produced `MultiTarget.LegacyOnly` in the `netstandard2.0` extraction and
`MultiTarget.ModernOnly` in the `net10.0` one — types that exist only under their own framework's
conditional compilation. MSBuildWorkspace loaded **one** framework and saw only one of them.

So the scope grain must be **one scope per (project, target framework)**, not per project. A single
scope per project would have to pick a framework and would then be silently wrong about every
`#if`-gated type in the others.

## Consequence

The Component 1 contract can be written against a measured implementation rather than an inferred
one. `DirectExtractor.cs` in this folder is that implementation's prototype.
