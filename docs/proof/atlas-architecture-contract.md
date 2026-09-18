---
id: proof-atlas-architecture-contract
title: "Atlas E2 carrier and resource identity spike evidence"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, proof, spike, domain, azure]
links:
  - { to: design-atlas-architecture-views, rel: relates-to }
review-by: 2026-12-15
summary: "57 Windows synthetic checks and eight rejected faults; complete expected relation semantics are compared."
review-suggested:
  - { by: design-atlas-architecture-views, on: 2026-09-17, reason: "New R122 contract proposal is not qualified by historical spike; review identity/history/equality claims against section4.1." }
---

# Bounded E2 contract evidence

## E2-M synthetic HTML review evidence — 2026-09-15

Human grant `req-01M2KCTTG4ZWAC2K01PB1HYWGK`; programme graph E2-M.
The [self-contained harness](../mockups/atlas-architecture-views.html) uses the
[existing design hub](../design/atlas-architecture-views.md#e2-m-direction-brief--human-granted-synthetic-review-harness).
No spike source, DESIGN.md, production or native surface changed.

### Observed controls and limitations

Final headless Microsoft Edge run at 1600×1500 emitted:

```text
PASS page 1570×1135 px; minimum text contrast 6.48:1;
25 visible controls; 0 targets below 24×24 px.
Synthetic browser render 1.70 ms (one sample; not product latency).
PASS 229 deterministic controls; 0 failures
Synthetic sweep 143.30 ms; not a product p95.
```

Normal-path instrumentation measures the current synthetic render and deterministic
sweep; neither qualifies the specification's native opening/filter latency budgets.
Coverage is explicit and orthogonal, not the full Cartesian product: all 15 rows
across three views exercise graph/list/inspector data and **rendered inspector fields**,
Source binding, and Back focus through the actual Source button for both list and
graph origins; six filtered relationships retain endpoint nodes and match fixed
expected SVG path geometry; three views exercise all 16 states; three themes × three
viewports × two densities exercise the Azure overflow view; two personas × two motion
modes exercise denied Source. No real browser-native keyboard event, UIA/DPI or
production authority test is claimed by these programmatic focus/click controls.

States: ready, unselected, loading, empty, error, partial, overflow, unsupported,
cancelled, stale, invalid carrier, no workspace, needs index, denied Source, changed
Source and disconnected. Explicit recovery controls simulate transitions. Azure
same-name declarations remain separate; alias evidence is distinct; lookup is not
grant; full deployment identity stays unresolved. Layer Current source fixture is
labelled synthetic assertion registry; current/target declarations remain declared.

### Red observations

Before final green, deliberately broken subject modes were opened in the same real
headless browser. Fragment names activate the fault without changing fixture expectations:

| URL fragment | Observed `#checks` | Dependent oracle |
|---|---|---|
| `#state-leak` | FAIL 229 controls; 27 failures | Three views × nine blocked states publish forbidden graph/list rows |
| `#evidence-drift` | FAIL 229 controls; 15 failures | Each visible inspector anchor differs from the expected rendered fields |
| `#back-focus` | FAIL 229 controls; 15 failures | List-origin Source/Back restores the wrong origin |

Browser process exit is 0 even for those intentionally broken pages. The DOM result
was inspected and the outer command asserted the expected PASS/FAIL strings; browser
exit alone is not acceptance. Earlier evidence-drift changed only a dataset attribute;
it was strengthened to corrupt **visible** anchor text and its rendered-field oracle.

Conductor found Source-button focus losing graph origin and filter-created dangling
graph edges. Selected origin is now retained separately, actual toolbar-button
click/focus paths are tested, and matching relations retain their endpoints. A layer
component-to-layer placeholder was replaced by an explicitly typed target layer-to-layer
relationship; the target Storage component remains independently visible. No new
producer or ownership policy was inferred.

### Repository gates and craft rubric

```text
python docs/ai-forward-pack/scripts/design-lint.py DESIGN.md --strict
clean - all token references resolve (0 warnings).
python tools/verify-mockup-audits.py docs/mockups/atlas-architecture-views.html
1 mockup swept headless, 0 findings.
python docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups/atlas-architecture-views.html --gate --a11y-obligation --markdown
0 Blockers; 3 Minor findings; non-empty exact HTML corpus scanned.
```

| Location / dimension | Severity / evidence | Disposition / confidence |
|---|---|---|
| Selected button hover / accessibility | Initial Blocker: 1.0:1 ink/background | Fixed selected-hover pairing; final detector has no contrast finding. Verified |
| Two pane boundaries / spacing | Minor: detector reports children flush against left boundary | Narrow stacked layout uses top boundaries; wide layout has 16px left inset. Screenshots readable; independent review decides residual. Verified detector finding |
| Type hierarchy / craft | Minor: detector ratio 1.8:1 | Reuses DESIGN.md scale and compact G6 direction; no new token system. Independent review pending |
| Graph/list/inspector / structure | Matching rows, explicit direction, selected evidence focal point | Headless output and five screenshots inspected; no author hard-veto clearance |

The initial mock-audit wrapper read past an `<output>` verdict element and matched
the embedded zero-page error string. Inspecting actual DOM showed a rendered page;
using the template-compatible `<span id="verdict">` restored its intended boundary.
One shell regex quoting error produced no browser evidence; rerun used a quote-free
regex. Neither failed command is counted as verification.

Temporary screenshot evidence inspected by author and Conductor:
`C:/Users/malla/AppData/Local/Temp/atlas-e2-default.png` (Domain selection),
`atlas-e2-narrow.png` (Azure overflow/long alias), `atlas-e2-contrast.png` (Layer partial,
system-color preview), `atlas-e2-loading.png` (loading/Cancel), and
`atlas-e2-layer.png` (typed current and target layer edges with normal-path timings).
All reside in that same temporary directory; they are review aids, not durable/native
proof. Reproduce with Edge `--headless=new --screenshot=<output> --window-size=1600,1500`
and the file URL. Supported query parameters: `view`, `state`, `selected`, `viewport`,
`theme`, `density`; for example `?view=azure&state=overflow&selected=alias-b&viewport=narrow`.

Class → sweep → derive → prevent: incomplete visual-oracle coverage and selection-origin
loss were swept across all three views; dependent fault modes and both-origin button
round trips are executable controls. Conductor owns serialized shared lesson updates.
Existing gate results prove their stated floors only. Independent UX/IA/accessibility/
Test/Simplifier review remains required. No main join or native acceptance occurred.

## Latest semantic row correction (2026-09-15)

Reviewer corrected clearance of `ed7efe79`: dependency Basis was not independently
checked. `relation-dependency-basis` changes only that produced field. It first
passed the previous oracle (exit 0, 57 checks), then failed the complete-row oracle
at `relation-produced-target-and-current` (exit 1). Normal rebuild passes 57;
all seven earlier faults were rerun and rejected, for eight total subject faults.

Both relation-positive oracles now compare both independently fixed expected rows,
all scalars, exact anchor records and exact assertion-reference sequences. The
[RESULT correction](../../spikes/atlas-architecture-contract/RESULT.md#complete-semantic-row-proof-correction--2026-09-15)
preserves the observed prior false pass and commands. Re-review and platform proof
remain pending; synthetic scope and unresolved production authority remain unchanged.

## Latest binding proof correction (2026-09-15)

Independent review blocked `63e68e8f` because its relation oracle omitted
Scope/Hash/Start/Length equality. The new `relation-corrupt-binding` subject fault
first passed that old oracle despite visibly corrupt output (exit 0), then failed
the repaired full-record equality oracle at `relation-produced-target-and-current`
(exit 1). A subsequent normal rebuild passed 57 checks. All six prior faults
were rerun and rejected; total subject faults now seven.

[RESULT.md](../../spikes/atlas-architecture-contract/RESULT.md#relation-binding-proof-correction--2026-09-15)
records exact expected binding, commands, observations and limits. Independent
review remains pending; the earlier full-binding claim is superseded, not silently
treated as previously proven. Deployment identity and production authority remain open.

## Latest relation experiment (2026-09-15)

Windows rebuild/run observed exit 0 and 57 checks. Original 28 controls remain;
new relation tests exercise typed endpoints, kind/evidence/assertion rejection,
declaration provenance, emitted relation records and seven-collection bounds.
The missing seventh collection oracle failed before the validator fix (exit 1).
All six explicit subject faults exited 1 after rebuild, including bad admission,
dropped relations and wrong projected endpoint. Named commands, outputs and
limitations are in [RESULT.md](../../spikes/atlas-architecture-contract/RESULT.md#relation-experiment--2026-09-15).

Verified: synthetic output/refusal behavior only. Flagged: independent review and
Conductor Linux/coverage pending; production producer, authorization and full
deployment identity remain unresolved. Design `15b53fe9` is unchanged.
The original repair evidence below is preserved as history.

Repair following independent BLOCK `8eb44853eefd935fb680c3824595b93b07e5b803`:
`dotnet run --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj`
on Windows, 2026-09-15. The new blank-required-text oracle first failed against prior behavior.
After repair and explicit rebuild, observed exit 0, ending:

```text
UNRESOLVED fully-known-deployment-positive: admitted Bicep subset supplies no actual subscription/resource-group identity; no invented tuple injected.
PASS 28 contract checks; six fixture groups observed; full deployment equality remains UNRESOLVED (not US-E8 acceptance).
```

The complete command output, specific negative cases first, fixture description and limitations
are committed in [RESULT.md](../../spikes/atlas-architecture-contract/RESULT.md).
The original 23-check transcript remains there as review-blocked history. Current checks observe
produced alias groups and complete retained anchors, isolate scope as the only identity change,
remove the constant authority assertion, and cover whitespace/text/row/kind limits.

Following the rebuild, `dotnet run --no-build --project spikes/atlas-architecture-contract/AtlasArchitectureContractSpike.csproj -- --fault <name>`
returned exit 1 for all three faults: `alias-drop` failed `two-aliases-one-produced-root`;
`alias-wrong-root` failed `different-root-not-collapsed`; `scope-drop` failed
`equal-symbols-distinct-scopes`. RESULT retains exact terminal diagnostics and source changes.
An earlier no-build run against stale output was excluded and all three rerun after rebuild.

Positive JSON fixture: [architecture.fixture.json](../../spikes/atlas-architecture-contract/architecture.fixture.json).
Executable assertions: [Program.cs](../../spikes/atlas-architecture-contract/Program.cs).

Observed negative oracles: duplicate IDs, unknown role, invalid array/label shape, missing or
incorrect aggregate root/member/invariant, invalid layer membership/state/dimension, hostile
acceptance marker, alias-to-alias misuse, stale/missing/foreign source binding, partial deployment
identity, and expression name. Positive declaration roles/layers and exact declaration aliases
pass. All are synthetic contract checks, not real Atlas authorization or native product tests.

**Open:** positive full deployment identity cannot be derived from the tested literal Bicep
subset; actual deployment-scope context is absent. No invented tuple was used to manufacture
that positive. No comparison, grants, runtime-use or deployed-inventory claim is admitted.
Linux/project-coverage before/after timings and independent review remain with Conductor.

Authority: Owner F1/F2 `57a09ed6`; exact-path Core grant request
`req-01M2KBCP9A8893CWVDDGKKTBYS`, Ruling 121. The spike does not admit implementation.
