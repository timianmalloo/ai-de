---
id: note-front-door-ruling-38
title: "Decision note — Ruling 38: the run-log reservation guard widened before F2"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, ruling, guard-scope, run-log, dc-118, head-join]
links:
  - { to: note-front-door-ruling-36, rel: depends-on }
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: relates-to }
review-by: 2026-12-10
summary: >-
  Ruling 36's per-shared-surface control, applied to F2 before dispatch, found F0's run-log guard
  narrower than its own doc comment in two dimensions. Widened to src/ recursive over three tokens
  with a named allowlist, red-first, before F2 dispatches. A brief is not a control.
---

# Decision note — Ruling 38

**Ruled by:** Owner agent, 2026-09-10. Confidence **Verified** — it opened the guard, its dynamic
half, the plan clause, and grepped `src/**/*.cs` for every use site of the three tokens.

## How this was found

It was not found by looking for it. **Ruling 36** created a control — *run the collision check per
declared shared surface, not only per ruling* — and the first thing that control was pointed at was
**F2 before dispatch**. It surfaced this within the hour.

That matters for the class: the previous check ran ruling-against-ruling and was clean, twice.

## The defect

`tests/AiDe.Core.Tests/Sessions/SessionPathContractTests.cs:52-79` carries this doc comment:

> *"Fails if: anything writes a run log **anywhere**."*

and this body:

```csharp
var sessionsDirectory = Path.Combine(RepoRoot(), "src", "AiDe.Core", "Sessions");
foreach (var file in Directory.EnumerateFiles(sessionsDirectory, "*.cs", SearchOption.TopDirectoryOnly))
```

**Narrower than its own sentence in two dimensions, not one.** The conductor reported the directory
scope. The Owner found the second:

> Worse than you reported: a writer calling `SessionPaths.RunsDirectory(...)` or
> `RunsDirectoryName` and appending its own filename **passes the guard even inside `Sessions/`** —
> the **token set** is narrowed too, not just the directory.

The dynamic half (`SessionConfigStoreTests.cs:135-150`) exercises only `SessionConfigStore`. Neither
half reaches `src/AiDe.App/` — which is exactly where **F2** builds the session document, the
paired-zone preset and the Console merged stream, and therefore exactly where a run log would
plausibly be written. `RunLogStore` is Phase 3, so the reservation is load-bearing: **if F2 squats
the path, Phase 3 inherits it and the reservation was theatre.**

## The ruling — option (b), widen before F2

The conductor offered (a) declare the residual and brief F2, or (b) widen first and delay F2. Ruled
**(b)**, on one line:

> **A brief is not a control.**

Cost is one small red-first node; the guard passes on today's tree — verified, not assumed: a grep
of `src/**/*.cs` shows **zero** use sites of `RunLogFile`, `RunsDirectory` or `RunsDirectoryName`
outside their declarations in `SessionPaths.cs`.

### Scope of the widened guard

| | |
| --- | --- |
| **Root** | `src/`, **recursive**, `*.cs` |
| **Tokens** | `RunLogFile` \| `RunsDirectory` \| `RunsDirectoryName` |
| **Allowlist** | `{ SessionPaths.cs }`, as a **named constant** |
| **Not scanned** | `tests/` — tests legitimately reference these (`SessionConfigStoreTests.cs:145-146`) |
| **Extension, not deletion** | Phase 3 adds `RunLogStore.cs` to the allowlist **citing its ruling**. *A guard that must be deleted to make progress is one people delete.* |
| **Doc comment** | states scanned root, recursion, token set and allowlist **verbatim** — the repo's own practice at `defect-classes.md:1788` |

### Declared residual, and its cover

A **hard-coded `"runs"` literal** bypasses any token scan. This is **not chased statically**. It is
covered **dynamically, by F2**: for each App surface F2 builds, exercise it and then assert
`SessionPaths.RunsDirectory(...)` **does not exist** — the App-layer twin of
`Lifecycle_NeverWritesUnderTheReservedRunsDirectory`.

> **That is a control F2 writes, not a sentence F2 is told.** F2's brief cites this ruling as a
> **test obligation**, not a caution. Recorded in the plan under F2, not only here.

## DC-118 — second instance, same class

Ruled explicitly: **do not mint a new class.** Both directions are one mechanism — transcription is
a width-changing step and nothing checks the width. The class's control gains a second half:

> Every **scan-shaped guard** must state in its doc comment the **scanned root**, whether it
> **recurses**, the **token set**, and the **allowlist**; and the plan clause it discharges must
> carry **the same qualifier**. The mismatch between the two is the tell.

## Conditions

- **Red-first observed, not described:** the widened guard shown failing with a temporary
  `RunsDirectory` caller placed under `src/AiDe.App/`, restore proven by an **empty** `git diff`,
  then green.
- **Core observed green on the final tree** before F2 dispatches; **App 400 unchanged**;
  `verify-test-run.py --update` **never run**.
- **Ruling 37 condition 1 is satisfied** when Core is observed green on the final tree of the join,
  **whichever commit is last** — the `SessionPaths.cs:8` doc-comment correction counts as inside
  the join: same seam, same defect shape, prose only.
