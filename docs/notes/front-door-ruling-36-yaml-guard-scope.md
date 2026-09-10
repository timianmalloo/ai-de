---
id: note-front-door-ruling-36
title: "Decision note — Ruling 36: Rulings 23 and 35 do not conflict; F0's YAML guard narrowed"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, ruling, seam, head-join, yaml, collision-check]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: relates-to }
  - { to: note-addendum-b-ratification, rel: depends-on }
review-by: 2026-12-10
summary: >-
  The head-join of F0, F1 and FT produced one failing test. Rulings 23 and 35 do not conflict;
  the fail-clauses derived from them did, on a surface the plan itself named as shared. F0's
  repo-wide YAML guard is narrowed to the session-config path by a seam node, the plan's F0
  fail-clause gains the scope qualifier it was missing, and the class is registered as DC-118.
---

# Decision note — Ruling 36

**Ruled by:** Owner agent, 2026-09-10. Confidence **Verified** — it opened the plan, the council
rulings, the failing test, the two csproj files and the session-config sources, and grepped
`src/AiDe.Core/Sessions/` for the token in question.

## What happened

The head-join of the three parallel front-door nodes built clean and ran **App 400/400 Completed,
Core 2037 executed with 1 failed**. The single failure:

```
FAILED: AiDe.Core.Tests.Sessions.SessionPathContractTests.CoreProject_TakesNoYamlDependency
Assert.DoesNotContain() Failure: Sub-string found
                                ↓ (pos 1127)
String: ···"ema/1` file through YamlDotNet's DOM, so "···
Found:  "Yaml"
```

The matched text is **not code**. It is the comment node FT wrote into `AiDe.Core.csproj` to
explain why it took the dependency — a node explaining itself, tripping another node's guard.

## The ruling

> **Rulings 23 and 35 do not conflict.** Ruling 23's subject is session config
> (`note-front-door-council-rulings`: *"`session.json`, not `session.yaml`"*). Ruling 35 explicitly
> carves itself out of it (`conductor-front-door.md`: *"Ruling 23's ladder argument does **not**
> transfer... Ruling 28 unchanged"*).
>
> **Narrow F0's guard to the session-config path.** Rewrite `CoreProject_TakesNoYamlDependency` as
> a source scan of `SessionConfig.cs` and `SessionConfigStore.cs`, in the same shape as the
> existing `RunLogFile` scan in that file, renamed to say what it now proves. **Nothing else.**
> Test count stays 2037; no test is deleted.
>
> **Refused:** an allow-list inside the repo-wide guard (*an allow-list that grows is how a guard
> stops meaning anything*), and splitting YamlDotNet into its own project (*real cost, no benefit —
> the collision is a test's scope, not a layering problem*).

Verified on evidence rather than expectation: `Yaml` appears under `src/AiDe.Core/Sessions/` in
exactly two files, `TemplateSchema.cs` and `TemplateFrontmatterReader.cs`. `SessionConfig.cs` and
`SessionConfigStore.cs` are clean, **so the narrowed guard is green today because of what the code
is, not because of what it is expected to be.**

## The correction to the conductor's reading — this is the substantive part

The conductor brought this as *"F0's guard over-reaches its ruling"*, treating it as an
implementation defect in one node. The Owner did not accept that framing:

> The plan's F0 clause reads *"Fails if: a YAML dependency appears"* **with no "in this node"
> qualifier**, while the FT clause below it names the `.csproj` as *"the one shared derived surface
> with F0/F1"*. **So the contradiction was written into the plan's fail-clauses, not only into F0's
> implementation, and the re-check had both facts in hand without crossing them.**

F0 implemented the clause it was given, literally and correctly. The two halves sat in **one
document**, eleven lines apart in structure, and the collision re-check — which runs
ruling-against-ruling — could not see it, because as *rulings* they agree.

## Scope effect

| | |
| --- | --- |
| **Admitted** | One seam node (`FS1`), bounded to rewriting and renaming that one test method. Nothing else. |
| **Cut** | The allow-list; the project split. |
| **Conductor-owned** | Amending the plan's F0 fail-clause — a plan record, which the plan gives the conductor. **The test edit is not:** *"the conductor authors nothing holds, including for one-line fixes at a join."* |
| **Deferred, recorded** | A guard that YamlDotNet types are referenced **only** from the template loader. Ruling 35's *"scoped to"* clause has no test today. That is a gap, not a floor trip — it does not block this slice. **See the erratum below: this gap was already closed.** |
| **Registered** | **DC-118**, with the control named below. |

## Conditions on the close

- **(a)** The seam node returns **red-first** evidence: the narrowed test observed passing on the
  merged tree, observed **failing** when `SessionConfigStore.cs` is temporarily given a `Yaml`
  reference, and the restore proven by an empty diff. *Describing the falsifier does not discharge
  it.*
- **(b)** The merged floor is set **only after** Core is observed 2037/2037 green. A floor set over
  a red suite records a number nobody verified.
- **(c)** The plan amendment cites this note.

## The control, for DC-118

> Run the collision check **per declared shared surface**, not only per ruling. For every surface a
> plan names as shared between nodes, list each node's `Fails if:` that names it and show them
> **jointly satisfiable**. A `Fails if:` clause with **no scope qualifier** over a declared shared
> surface is a defect in the plan, independent of whether it happens to collide.

Pack-level rather than ai-de-level — it is about plan authoring, not this codebase — so it is being
landed upstream in `ai-forward` rather than as local prose. Until it lands, DC-118 is honestly
`uncontrolled`: the class is stated and the instance repaired, but **nothing fails when the shape
recurs.**

## Erratum — the deferred gap was already closed, and the closing control fired

Recorded rather than silently dropped, because the ruling deferred work that did not need doing.

**Ruling 36 deferred** *"a guard that YamlDotNet types are referenced only from the template
loader — Ruling 35's 'scoped to' clause has no test today."* **It has one.** FT shipped
`TemplateFrontmatterParserTests.TheDependencyIsScopedToTheTemplateLoader`, which asserts the
package token appears in **exactly one file under `src/`**.

**And it is not a dormant control — it fired, unprompted, during the seam node's own work.** While
correcting the stale `SessionPaths.cs` remarks (commit `a3a98c9`), the node's first attempt wrote
the literal package token into the new comment. The suite went **2036 pass / 1 fail** on
`TheDependencyIsScopedToTheTemplateLoader`. The node reworded to avoid the literal token and
re-ran clean at 2037.

Three things worth keeping from that:

1. **The gap was closed before it was deferred** — the deferral was made against an absence that
   was not verified. Whose it was does not matter; the fix is that a deferred gap is checked for an
   existing control before it becomes a next step.
2. **The control caught a real second instance of its own class, in the act**, on a file nobody
   suspected: prose *about* a dependency is indistinguishable from a reference *to* it under a
   token scan. That is DC-118's mechanism again — the scan's width and the claim's width differ,
   here in the direction that produces a **false positive** rather than a false negative.
3. **A false positive from a token scan is the price of it being cheap**, and the node paid it
   correctly — by changing its prose, not by weakening the guard. Noted because the tempting fix
   was to add an exclusion, and that is how an allowlist starts growing.

## Findings from the class sweep, carried forward

The seam node swept the repo for the same claim shape — an unqualified statement that some
dependency, parser or type exists **nowhere**. Six hits; three were already repaired by the
conductor, one regenerates from source at the join. Two remain, **both still true**, and are
recorded rather than touched:

| `file:line` | Claim | Why it is left alone |
| --- | --- | --- |
| `docs/notes/addendum-b-reconciliation.md:81` | *"No `providers.yaml` parser exists"* | **Still true** — `EngineCatalog.cs` and `ProviderRegistry.cs` carry no YAML reference. But it is an unqualified repo-wide claim in a decision record, i.e. **the same shape waiting to go stale**, and the assist path that would falsify it is Phase 3 (Ruling 27). The moment R21 lands, this line needs the same erratum treatment as `addendum-b-ratification.md:29`. |
| `src/AiDe.Core/Sessions/TemplateFrontmatterReader.cs:44,62-63` | *"the only YAML dependency / the only file referencing it in `src/`"* | **Still true and test-backed** — `TheDependencyIsScopedToTheTemplateLoader` asserts exactly this and passes. This is the shape done right: a prose claim with a control under it. No action. |

The second row is the pattern the first should reach: **an unqualified claim is safe exactly when a
test fails if it stops being true.**

### One more, found by the seam's own gate sweep — safe by accident, not by design

`tests/AiDe.Core.Tests/Sessions/SessionPathContractTests.cs:142` writes the bare word `YamlDotNet`
in prose. It predates this seam and nothing is currently wrong. But it survives for **two
independent accidental reasons**, neither of them an invariant anybody chose:

1. `verify-cited-controls`' citation regex only fires on `<c>` / `<see cref>` **markup**, and this
   occurrence has none — so the citation gate does not look at it.
2. `TheDependencyIsScopedToTheTemplateLoader`'s token scan reads **`src/` only, not `tests/`** — so
   the scoping guard does not look at it either.

**Widen either scope and that line becomes a failure.** It sits precisely in the gap between two
guards' widths — which is DC-118's mechanism a third time, and the most instructive of the three,
because here nothing is *wrong yet*: the defect is that the line's safety is **load-bearing on two
scopes staying exactly where they are**, and no one writing either guard knew they were holding it
up. Recorded rather than fixed, because changing it now would be changing a correct line for a
reason no test states. **The control is this paragraph plus DC-118's second-half rule** — a guard
that declares its root, recursion, token set and allowlist makes gaps like this visible by
subtraction, instead of leaving them to be discovered when a scope moves.
