---
id: proof-lease-source-text
title: "Proof Pack - Lease Derivation Runs Over The Editor's Source Text (Ruling 66 / F-2)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c-chain"
tags: [composer, lease, security, ruling-66, f-2, proof-pack]
links:
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Proof Pack for Ruling 66 / F-2: LeaseDerivation.Derive and LeaseDerivation.Patterns now read
  ComposerDraft.SourceText (the operator's own typed content) rather than the fully compiled prompt,
  so an attachment body or a template's fixed prose can no longer widen a lane's write scope. Red
  observed on main before the fix; Core 2240/0, App 605/0 after.
---

# Proof Pack — Lease Derivation Runs Over The Editor's Source Text (Ruling 66 / F-2)

## The defect (F-2)

`LeaseDerivation.Derive`/`Patterns` (`src/AiDe.Core/Presentation/Composer/LeaseDerivation.cs`) were
fed `ComposerCompiler.Compile(draft, template).Text` at both call sites —
`ComposerSendGate.cs:164` (the sent lease) and `ComposerSurface.cs:449` (the displayed lease). That
compiled text additionally carries an attachment's raw file content
(`ComposerCompiler.RenderAttachment`, `ComposerCompiler.cs:136-151`) and, for a template draft, the
template's own fixed `Body` prose (`TemplateCompiler.Compile`). A mention (`@path/to/thing`) inside
an attached file, or baked into a catalog template someone else authored, therefore minted a write
scope the operator never referenced.

## The fix

Added `ComposerDraft.SourceText` (`src/AiDe.Core/Presentation/Composer/ComposerDraft.cs`) — a
shape-scoped accessor over only what the operator typed: the free-form text (`FreeForm`), the six
goal-block field values in `§14.3` order (`GoalBlock`, never the rendered `## heading` structure),
or the active template's field values (`Template`, never the template's fixed body). Both call sites
now pass this symbol:

- `ComposerSendGate.cs:169`: `Lease: LeaseDerivation.Derive(draft.SourceText)`
- `ComposerSurface.cs:452`: `var patterns = LeaseDerivation.Patterns(_draft.SourceText);`

`LeaseDerivation` itself is unchanged — no edit to its regex, its `ToPattern` normalization, or its
universal-lease / empty-lease refusals.

## Red observed on `main` before the fix

Recorded from `dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter
"FullyQualifiedName~TheLeaseDerivesFromTheEditorsSourceTextTests"`, run against the pre-fix call
sites (`compiled.Text` at both sites):

```
Failed AiDe.App.Tests.Composer.TheLeaseDerivesFromTheEditorsSourceTextTests.AnAttachmentBodyMentionDerivesNoPatternButTheEditorTextStillDoes
  Assert.DoesNotContain() Failure: Item found in collection
  Collection: ["src/AiDe.App/Foo.cs", "src/AiDe.Core/Bar.cs"]
  Found:      "src/AiDe.Core/Bar.cs"

Failed AiDe.App.Tests.Composer.TheLeaseDerivesFromTheEditorsSourceTextTests.ATemplateBodyMentionDerivesNoPatternButAFieldValueStillDoes
  Assert.DoesNotContain() Failure: Item found in collection
  Collection: ["docs/plan.md", "src/AiDe.App/Foo.cs"]
  Found:      "docs/plan.md"

Failed!  - Failed: 2, Passed: 2, Skipped: 0, Total: 4
```

(The other two tests in the same red run — the goal-block-heading control and the goal-field
control — were already green pre-fix: they are controls proving the fix does not over-narrow, not
regressions.)

## Gate table

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | An attachment body's mention derives no pattern; the operator's own mention in the same draft still does | `AnAttachmentBodyMentionDerivesNoPatternButTheEditorTextStillDoes` | `SourceText` including attachment content again | **seen** (see red run above) | Verified | — |
| 2 | A template body's mention derives no pattern; a template field value the operator typed still does | `ATemplateBodyMentionDerivesNoPatternButAFieldValueStillDoes` | `SourceText` including template body prose again | **seen** (see red run above) | Verified | — |
| 3 | The rendered goal-block headings never yield a pattern on their own | `TheRenderedGoalBlockHeadingsNeverYieldAPatternOnTheirOwn` | headings misread as mentions | n/a (already true; a control) | Verified | — |
| 4 | The same mention typed directly in a goal field still derives its pattern (no over-narrowing) | `TheSameMentionTypedInTheGoalFieldStillDerivesItsPattern` | fix narrows past the operator's own field text | n/a (already true; a control) | Verified | — |
| 5 | **Display and Send derive from the same symbol** — the surface's shown lease equals the sent lease's `Exclusive`, for a draft with an attachment mention | `TheDisplayedLeaseAndTheSentLeaseAgreeForADraftWithAnAttachmentMention` | the two call sites drift apart (one still reads `compiled.Text`) | n/a (construction proof; would fail if either site regressed) | Verified | — |
| 6 | Every `LeaseDerivation.(Derive\|Patterns)(` call site in `src/` passes the source-text symbol (register control) | `EveryLeaseDerivationCallSiteInSrcPassesTheSourceTextSymbol` | a third call site added later, or either site reverted to `compiled.Text` | n/a (would fail on reintroduction) | Verified | a copy-pasted new call site inside the *same two files*, spelled to still end in `.SourceText` on an unrelated variable, would not be caught — accepted, see Security verdict |
| 7 | No regression | Core 2240/0, App 605/0 | any broken composer/lease/one-registry contract | n/a | Verified | — |
| 8 | Clean build under `-p:TreatWarningsAsErrors=true` | `dotnet build AiDe.sln -p:TreatWarningsAsErrors=true` → 0 warnings, 0 errors | a warning hiding a real issue | n/a | Verified | — |

## Security & Identity verdict (read-only, adversarial)

Convened as a sub-agent against the diff, without write access. See the register entry (DC-146) and
the commit for the verdict text; summarized here: no remaining path by which non-operator text
(attachment content, template body prose) reaches `LeaseDerivation.Derive`/`Patterns`;
`ComposerShape.Template`'s `SourceText` branch reads only `ComposerDraft.TemplateValues`, which is
populated exclusively by `ComposerSurface.SetFieldText` from operator keystrokes (never from a file
or an attachment); Addendum D's mention-suggestion path is not built; `LeaseDerivation` is
byte-for-byte unchanged.

## Residual risk

The source-scan guard (claim 6) matches on the literal call-site tokens and the trailing symbol
name (`.SourceText`) rather than proving by static analysis that the *value itself* is
operator-authored; a future call site that names a different, incorrectly-populated
`.SourceText`-suffixed property would pass the guard's text match while reintroducing the class.
Accepted: the guard is the same shape as the existing "exactly two sites" registry guards elsewhere
in this suite (`C16_ExactlyTwoSitesInTheProductConstructAGovernedRunRequest`), and `ComposerDraft`
declares exactly one `SourceText` member, so the ambiguity is only theoretical today.
