---
id: note-front-door-council-rulings
title: "Decision note — Rulings 19–25, resolving the front-door council vetoes"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, council, veto, ruling, addendum-a]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: note-addendum-a-ratification, rel: refines }
  - { to: note-conductor-phase1-e18-close, rel: relates-to }
review-by: 2026-12-10
summary: >-
  Both front-door council vetoes returned BLOCK. Rulings 19-25 resolve them: four dead
  sheet fields cut and the two the run actually requires added, login remediation moved out
  of the sheet, the canvas split admitted, descriptor lists instead of registries,
  session.json instead of session.yaml, hosting settled after a dependency trim, and F2/F3
  merged.
---

# Decision note — Rulings 19–25

**Ruled by:** Owner agent, 2026-09-10, on evidence it opened (`GovernedRunRequest.cs`,
`ConductorEntry.cs:112-125`, every `.csproj`, the spike `package.json`, A3/A4.3/A6).

> **Filed late, and that is itself a finding.** These rulings were issued, acted on in
> conversation, and **not written to `docs/notes/` at the time**. The gap surfaced when the Owner
> was asked to check Addendum B for collisions against them and reported it could not — *"that is
> the finding, not a clearance."* **An unfiled ruling is "not recorded."** Same shape as DC-111,
> DC-112 and DC-113: a record that exists somewhere unreachable is not a record.

## The council's verdicts

| Reviewer | Veto | Verdict |
| --- | --- | --- |
| Test Architect | **hard** | **BLOCK** — 10 Blockers; six R13–R16 bullets had no clause, and none had been cut by a ruling |
| Simplifier | soft | **BLOCK** — 7 Majors, all verified against the repo |
| Security & Identity | hard (scoped) | **PASS-WITH-CONDITIONS** — C1–C8 |

## Ruling 19 — the sheet carries only fields the run consumes

**Cut** routing mode, autonomy, default policy and per-session MCP selection.
**Add `TaskClass`** (required, **no default**) and show the derived `Lease`.

`GovernedRunRequest.cs` — the one composition root Ruling 13 forbids bypassing — takes **none**
of the four cut fields, and **requires** `TaskClass` and `Lease`. Its own comment: *"Required: a
defaulted class ranks in the wrong cohort."* **That is DC-110**, which this programme registered.
A hidden `TaskClass` default would make the exit run `IsComparable == false` on its own.

Zero `autonomy` hits in `src/`; no `RoutingMode` or `*Policy` type; one MCP server, auto-ensured
per workspace at `WorkbenchShell.cs:2565`, never selected per session. Routing modes are R9,
Phase 3.

**Marked plainly as extending A4.3, not reading it** — the addendum listed fields the run cannot
consume. Re-entry triggers: routing mode → R9 lands; autonomy/policy → the spawn contract carries
them; MCP toggle → a second MCP server exists.

## Ruling 20 — login remediation leaves the sheet

Keep the per-account **health display** (`ready` / `needs-login` / `quota-degraded`). Move
remediation to a **"Sign in" action** launching the engine-native flow (**claude-code only**) that
re-probes on return, under a `simplify:` marker.

Ruling 18 required the login *provable against claude-code*; it did not require an in-modal auth
subsystem, and A4.3 says **one screen, not a wizard**. No auth code exists in `src/`;
`NeedsLogin` is only an `AccountHealth` value meaning *absent*.

**Not cut:** toggling a `needs-login` engine is still **refused for routing**.

## Ruling 21 — the canvas split is IN

Proven as **Console beside Terminal**, with a clause and a red-first oracle in the merged node.
Ruling 18 deferred it *to this plan* with the proof shape already stated. **A deferral pointing at
this document that then resolves nothing is a vanish, not a cut.**

## Ruling 22 — descriptor lists, not registries, and not switch arms either

Condition (a) does **not** require a registry type. Register the two modes and two picker sources
as **data** — a descriptor list consumed by `SurfaceContentFactory` and the picker.

(a)'s operative words are *"data-driven registrations, **never a hard-coded five-tab strip with
placeholders**"* — the target is placeholders, not the absence of a registry class. An abstraction
with two implementers is what Rulings 7 (`ILane`) and 15 (concrete `GovernedLane`) already cut.
**But a `switch` arm is not data-driven either** — the mode set must be a value a later phase
appends to **without editing the factory**.

Clause: *"adding a mode is adding a row, not a switch arm — fails if a placeholder tab exists or a
new mode requires editing the factory."* Upgrade trigger: a third registrant outside the App
assembly.

## Ruling 23 — `session.json`, not `session.yaml`

**No `.csproj` references a YAML package** (verified, zero matches) while `System.Text.Json` is in
use in 38 files, and the sibling artifact in the same tree is `.jsonl`. The addendum states **no
rationale** for YAML, so there is no human-editability argument to clear a new dependency against
the ladder. `providers.yaml` is the precedent in the *other* direction — the spec names it and
`ProviderRegistry.cs:87` leaves parsing to a caller that does not exist.

Recorded as an **A3 erratum**, marked as extending the addendum. Re-entry trigger: the operator
asks to hand-edit session config and JSON proves hostile in practice.

## Ruling 24 — CodeMirror as a vendored, hash-pinned bundle, after trimming

Cut `@codemirror/language-data` **first**, re-measure, **then** execute option (c).
*"I do not rule on inputs one reviewer has shown to be wrong."*

**The trim was executed and reported:** packages **52 → 26**, disk **11 MB → 7.5 MB**, import-map
entries **51 → 24**. Everything re-verified in headless jsdom *and* real Chromium; an unlisted-
language fence **observed** rendering as plain unstyled text.

**It did not change the ranking, and said so plainly:** the mandatory cost is untouched by package
count — `NavigateToString` cannot serve import-map ES modules at all — and the live map *still*
needed a hand-added transitive entry. **Option (c) stands.** Bundle committed at 508,337 bytes,
SHA-256 `ee3d19a4…`, hash independently recomputed against the committed bytes: **match**.

## Ruling 25 — F2/F3 merged; budget re-derived; nothing may vanish

**Merge F2 and F3** into one serial node. F2's exit (*"opens the document"*) consumes F3's output
(the `"session-document"` kind), and by the plan's own contention statement the split buys **zero**
parallelism while forcing a stub — HYG-A.

**Re-derive the verification half of the budget from N4/N7**, keeping N0 only as a ceiling
reference. N7's **23:1** verification-to-run ratio is the recurring shape.

**And the binding half:** the six uncut R13–R16 bullets the Test Architect named each get **a clause
and an oracle, or a fresh recorded cut with a reason. None may vanish.** F5 gains a `Fails if` line
and restores N7's **pre-committed oracle** plus the `terminalHostConstructions` **companion
falsifier**. *"One event cycle"* is rewritten as an **observable** (the permission overlay is raised
before the next event is dispatched), never a duration, so DC-107 does not trip. The ADR-0017
clause states an oracle that **detects disposal** — `Assert.Same` passes on a disposed instance and
therefore cannot.
