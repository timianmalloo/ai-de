---
id: proof-s2-settings-and-sentinels
title: "Proof Pack — S2: the settings and sentinels commit"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, sessions, agent-plane, watcher, adr-0033, ruling-56, ruling-68, ruling-70, ruling-72, s2, phase-1]
links:
  - { to: coordination-addendum-cd, rel: implements }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Evidence that SessionConfig gains four additive session settings (fan-out ceiling, an optional
  budget cap, compile mode, default task class) that an old session.json reads back with the ruled
  defaults; that TaskClasses.FreeForm is a declared, comparable task class beside
  ScoreSegment.Unclassified; and that RunBudget.SubscriptionBounded is accepted by
  SpawnContract.Validate untouched, with a value-equality reader that survives a JSON round trip.
  All three reds observed as compiler refusals before the members existed. No rendering site
  touched — ADR-0033 names the exact subscription-bounded wording as a later node's decision.
---

# Proof Pack: S2 — the settings and sentinels commit

- **Change:** `src/AiDe.Core/Sessions/SessionConfig.cs` (four new fields + `CompileModes`), `src/AiDe.Core/Watcher/Leaderboard.cs` (`TaskClasses.FreeForm`), `src/AiDe.Core/AgentPlane/GoalBlock.cs` (`RunBudget.SubscriptionBounded`, `RunBudget.IsSubscriptionBounded`); `tests/AiDe.Core.Tests/Sessions/SessionConfigStoreTests.cs`, `tests/AiDe.Core.Tests/Watcher/LeaderboardTests.cs`, `tests/AiDe.Core.Tests/AgentPlane/SpawnContractTests.cs`.
- **Ruling / spec:** `note-addendum-c-council-rulings` Rulings 56, 68, 70, 72; `adr-0033-prompt-compilation-bounded-context` §3/§4 and its "Alternatives considered"/"Consequences" sections; `docs/coordination/addendum-cd.md` row **S2**.
- **Tier:** T1 (no judgement left — the values are fixed by the ADR and the rulings). **Session:** `s2-settings`. **Worktree:** `ai-de-feature-s2-settings-and-sentinels`, branch `feature/s2-settings-and-sentinels`.
- **Tests:** +3 in `SessionConfigStoreTests.cs`, +2 in `LeaderboardTests.cs`, +5 in `SpawnContractTests.cs` (10 new). Full suites: **Core 2256/2256**, **App 610/610**; `verify-test-run.py` (CHECK, no `--update`) OK against baselines 2239/554 with 2866 executed; every build `0 Warning(s)` under `-p:TreatWarningsAsErrors=true`.

## Claims & evidence

| # | Claim | Evidence (test) | Source | Oracle — why it can fail | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|---|
| 1 | `SessionConfig` gains `FanOutCeiling` (int, default **2**), `BudgetCap` (`RunBudget?`, default `null`), `CompileMode` (string, default `mechanical-only`), `DefaultTaskClass` (string, default `free-form`) — additive, non-positional `init` properties | `Create_SetsTheRuledDefaultsForTheFourNewFields` | `SessionConfig.cs` | Asserts each of the four default values on a freshly created config | **Yes** — compiler refusal (see below); no runtime behaviour existed to be wrong | Verified | — |
| 2 | A `session.json` written by **today's code before this change** (no key for any of the four) reads back through `SessionConfigStore.Load` with the four ruled defaults, not an error or a silently wrong value | `Load_AnOldSessionFileWithNoneOfTheFourNewFields_ReadsBackWithTheirDefaults` (hand-written pre-S2-shape JSON) | `SessionConfigStore.Load` → `JsonSerializer.Deserialize<SessionConfig>` | Reads a file the test writes directly, bypassing the store's own (already-updated) writer — deserializing extra `init` properties absent from JSON must fall back to the C# default, not throw or null out a value type | **Yes** — same compiler refusal, since the fields the assertions read did not exist | Verified | Only proves *this* store's JSON contract; a differently-serialized settings surface (none exists) is untested |
| 3 | Each of the four fields round-trips a **non-default** value through the store's persisted JSON (proves persistence, not just the default) | `EachOfTheFourNewFields_RoundTripsANonDefaultValueThroughPersistedJson` | `SessionConfigStore.Load`/`JsonSerializer` | Non-default values (`4`, `RunBudget(250, 600000)`, `agentic`, `"refactor"`) must come back exactly | **Yes** — compiler refusal | Verified | — |
| 4 | `BudgetCap` reuses `AgentPlane.RunBudget` rather than a second `(requests, tokens)` shape (DM7: two definitions of one quantity is a defect signature) | Type inspection: `SessionConfig.BudgetCap` is `RunBudget?` | `SessionConfig.cs` | A reviewer diff — no separate record was introduced | n/a (design choice, not a runtime oracle) | Verified | — |
| 5 | `TaskClasses.FreeForm = "free-form"` is declared beside `ScoreSegment.Unclassified` in `Leaderboard.cs`, and a segment carrying it **is** a cohort — comparable, unlike `Unclassified` | `ASegmentCarryingFreeFormIsComparable_UnlikeUnclassified` | `ScoreSegment.IsComparable` / `IncomparableReason` | `free-form` must not trip the `Unclassified` string-equality branch | **Yes** — `CS0103: The name 'TaskClasses' does not exist in the current context` | Verified | — |
| 6 | The partition key treats `free-form` as an ordinary class: episodes carrying it cohort and rank exactly like any other class | `FreeFormPartitionsAsItsOwnCohort_SeparateFromOtherTaskClasses` | `LeaderboardComposer.Compose` | A 5-episode cohort at `TaskClasses.FreeForm` must be `Comparable` with `Cohort == 5`, the same shape `refactor` already gets elsewhere in this file | **Yes** — same `CS0103` | Verified | — |
| 7 | `RunBudget.SubscriptionBounded = (Requests: int.MaxValue, Tokens: long.MaxValue)` | `SubscriptionBoundedIsTheDeclaredMaximalValue` | `GoalBlock.cs` | Exact field equality | **Yes** — `CS0117: 'RunBudget' does not contain a definition for 'SubscriptionBounded'` | Verified | — |
| 8 | A goal block carrying the sentinel **validates with no error** and **authorizes a spawn**, with `SpawnContract.Validate`/`Authorize` unchanged — the sentinel is accepted because it is an ordinary positive value, not a special case | `AGoalBlockCarryingSubscriptionBoundedValidatesWithNoError`, `AGoalBlockCarryingSubscriptionBoundedAuthorizesASpawn` | `SpawnContract.Validate`/`Authorize` (byte-identical; no line inside either changed) | `Validate`'s positivity check (`budget.Requests <= 0 \|\| budget.Tokens <= 0`) is already false for `int.MaxValue`/`long.MaxValue` — the claim is that **no code change was needed**, proven by the same `Validate` body passing today | **No runtime red possible** — see "On the '§2 refusal' wording" below; the compile-time red (claim 7/9) is what this member's absence actually produced | Verified | `SpawnContract.Validate`'s six-field, tier-blind contract is unchanged by inspection (no diff to the method body) |
| 9 | `RunBudget.IsSubscriptionBounded` is **value equality, not reference equality** — the case that matters once the record has crossed JSON and come back as a new instance | `IsSubscriptionBoundedIsValueEqualityNotReferenceEquality`, `AnOrdinaryBudgetIsNotSubscriptionBounded` | `RunBudget.IsSubscriptionBounded` | A separately-`new`'d `RunBudget(int.MaxValue, long.MaxValue)` (`NotSame` to the constant) must still satisfy the predicate; an ordinary budget must not | **Yes** — `CS1061: 'RunBudget' does not contain a definition for 'IsSubscriptionBounded'` | Verified | — |
| 10 | `SessionConfig` carried **no** `TaskClass` member before this change — `default_task_class` is a new field, not a rename of anything F5's tree or the Watcher reads | Read of the pre-change `SessionConfig.cs` (88 lines, positional `SessionId, Name, WorkspaceId, CreatedAt, EnabledBackends` + `AttachEnabled`; no `TaskClass`) | `git show HEAD:src/AiDe.Core/Sessions/SessionConfig.cs` | — | n/a | Verified | `GovernedRunRequest.TaskClass` (`AiDe.App`) and `ScoreSegment.TaskClass` (Watcher) are separate, untouched members sharing the name — documented on `DefaultTaskClass`'s XML doc rather than assumed |
| 11 | `ConductorEntry.cs`, the one-registry guard, and the session-origin-on-command-path test stay green; nothing outside the three source files + their tests + this Proof Pack + the audit entry + derived docs was touched | `git diff --stat` names exactly 6 files pre-regen; `TheRunBindingComesFromTheProviderFileTests*` and `TheSessionOriginIsSetOnlyOnTheCommandPathTests` pass in the full App suite | `git status`, `dotnet test tests/AiDe.App.Tests` | Any stray write shows in `git status --short`; either guard test failing shows in the 610-count | Checked, not applicable | Verified | — |

**Boundary set:** an old file with none of the four keys (red 2) · a file with all four at non-default values (claim 3) · `free-form` vs. `Unclassified` (claim 5) · a 5-episode `free-form` cohort vs. the file's existing `refactor` cohorts (claim 6) · the sentinel vs. an ordinary `RunBudget` (claim 9) · a separately-constructed sentinel-valued instance (reference vs. value equality, claim 9).

**Mutation sense:** every red above is the same shape — a member referenced by a test before it existed on the type, observed as a C# compiler diagnostic (`CS0103`/`CS0117`/`CS1061`) rather than a runtime assertion failure. This is the correct red for adding a brand-new, additive member: there is no prior runtime behaviour to have been wrong, only an absent name. The reds were captured by reverting the three production files to `HEAD` (`git checkout HEAD -- <file>` — never `git stash`) with the new tests already in place, rebuilding, and restoring the edited files from a local copy afterward; `git status --short` before/after confirms only the six intended files ever differ from `HEAD`.

### The three reds, verbatim

**Red 1 — `SessionConfig`'s four fields** (building `tests/AiDe.Core.Tests/Sessions/SessionConfigStoreTests.cs` against pre-change `SessionConfig.cs`):
```
error CS1061: 'SessionConfig' does not contain a definition for 'FanOutCeiling' ...
error CS1061: 'SessionConfig' does not contain a definition for 'BudgetCap' ...
error CS0103: The name 'CompileModes' does not exist in the current context
error CS1061: 'SessionConfig' does not contain a definition for 'CompileMode' ...
error CS0103: The name 'TaskClasses' does not exist in the current context
error CS1061: 'SessionConfig' does not contain a definition for 'DefaultTaskClass' ...
```

**Red 2 — `TaskClasses.FreeForm`** (building `tests/AiDe.Core.Tests/Watcher/LeaderboardTests.cs` against pre-change `Leaderboard.cs`):
```
error CS0103: The name 'TaskClasses' does not exist in the current context
```
(three occurrences — the two new tests each reference it, one twice)

**Red 3 — `RunBudget.SubscriptionBounded` / `IsSubscriptionBounded`** (building `tests/AiDe.Core.Tests/AgentPlane/SpawnContractTests.cs` against pre-change `GoalBlock.cs`):
```
error CS0117: 'RunBudget' does not contain a definition for 'SubscriptionBounded'
error CS1061: 'RunBudget' does not contain a definition for 'IsSubscriptionBounded' and no accessible extension method 'IsSubscriptionBounded' ...
```

Green after: `dotnet build tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj -p:TreatWarningsAsErrors=true` succeeds; `dotnet test` on the same filter passes all 10 new cases; the full Core (2256) and App (610) suites pass with the restored files.

## The four fields' defaults, and where the workspace default came from

| Field | Type | Default | Source |
|---|---|---|---|
| `FanOutCeiling` | `int` | **2** | **Checked, not assumed: no workspace-default mechanism exists in `src/` today.** `docs/architecture.md:786` and the New Session sheet mockups (`docs/mockups/new-session-sheet.html`) both call the ceiling a "workspace default", but a repo-wide search (`grep -rn "WorkspaceDefaults\|DefaultFanOut"`) found no `WorkspaceDefaults` type or workspace-level setting anywhere in code — this session-settings node is the *first* code home for the ceiling at all, with no workspace layer beneath it. 2 is CT19's own ruled T1 fan-out cap ("0 at T0, 2 at T1, the GO7 width cap at T2") used as the per-session default instead. |
| `BudgetCap` | `RunBudget?` | `null` | Ruling 72 (a): "budgets should be max … by default and then optionally I can enforce a cap" — absence, never a required number. `null` reads as *bounded by the subscription* at the point a projection needs an actual `RunBudget` (that substitution is the projection's job, not this record's — "derive, don't store"). |
| `CompileMode` | `string` (`CompileModes.*`) | `"mechanical-only"` | Ruling 68: "`compile_mode` defaults to `mechanical-only`; `agentic-advisory` and `agentic` are opt-in behind the eval gate" (ADR-0033 §A10.1). |
| `DefaultTaskClass` | `string` | `"free-form"` (`TaskClasses.FreeForm`) | Ruling 72 (b): "the basic should be free-form upon open, and then I can change it" — an explicit, operator-visible default, not a null the caller must special-case (ADR-0033 §4). |

## The rendering decision for `SubscriptionBounded`

**No rendering was built or touched.** ADR-0033 §3 names the existing text projection explicitly — `ComposerCompiler.RenderGoalBlock` (`ComposerCompiler.cs:83-99`) and the not-yet-built `Projection.cs` — as the sites that must render the sentinel as *"budget: bounded by the subscription (no cap declared)"* / *"budget cap 250 requests / 600k tokens"*, guarded by `IsSubscriptionBounded`. Three independent facts fix this outside S2's scope:

1. **This node's own contract (§4, "Nothing else") forbids composer changes** — `ComposerCompiler.cs` is not one of the three files S2 owns.
2. **The coordination plan assigns the render explicitly elsewhere.** `docs/coordination/addendum-cd.md` row **CV-1** lists "the numeral-absence render test for `SubscriptionBounded`" and "read *'bounded by your subscription'*" in its own Reds-first list; row **CV-0**'s decision-edge text names "`SubscriptionBounded` in the compiled block" as CV-0's addition.
3. **ADR-0033 itself defers the exact wording**: "**Inferred:** the exact representation of 'subscription-bounded' in the compiled block — A1's to decide with a falsifying test."

So the "text projection exists today" test the plan describes was written and answered honestly instead of forced: `IsSubscriptionBounded` (claim 9) is the reader every later render site needs, built and proven here; the render itself is out of scope and is not simulated by a test that would need to touch a file this node may not touch.

## Gate table

| Gate | Result |
|---|---|
| `dotnet build` Core (`-p:TreatWarningsAsErrors=true`) | 0 Warning(s), 0 Error(s) |
| `dotnet build` App (`-p:TreatWarningsAsErrors=true`) | 0 Warning(s), 0 Error(s) |
| `dotnet test tests/AiDe.Core.Tests` (full) | 2256/2256 passed |
| `dotnet test tests/AiDe.App.Tests` (full) | 610/610 passed |
| `tools/verify-test-run.py` (CHECK, no `--update`) | OK — 2866 executed across 2 projects, both baselines (2239/554) met |
| Every `tools/verify-*.py` | All OK **except** `verify-derived-views.py` and `verify-site-figures.py` (both: `docs/_meta.json`/`docs/_site/index.html` public-symbol counts stale after this commit's new public members — fixed by `tools/regenerate-derived.py`, run after the audit entry, per its own ordering rule) and `verify-stranded-audit.py` (flags **`C:\projects\ai-de`, the primary checkout** — a different tree with pre-existing uncommitted audit-log changes, unrelated to this worktree and outside this node's write scope) |
| `TheRunBindingComesFromTheProviderFileTests*` (one-registry guard) | Green (in the 610) |
| `TheSessionOriginIsSetOnlyOnTheCommandPathTests` | Green (in the 610) |
| `git diff --stat` scope | Exactly the 3 source files + 3 test files (pre-regen); `ConductorEntry.cs` untouched |
| `tools/regenerate-derived.py` | Run after the audit entry (see below) |
