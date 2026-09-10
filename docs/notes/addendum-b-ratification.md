---
id: note-addendum-b-ratification
title: "Decision note — Addendum B ratified; Rulings 26–31"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, addendum-b, templates, ratification, assist, pinned-contract]
links:
  - { to: note-addendum-b-reconciliation, rel: depends-on }
  - { to: note-front-door-council-rulings, rel: relates-to }
  - { to: note-addendum-a-ratification, rel: relates-to }
review-by: 2026-12-10
summary: >-
  Addendum B ratified: R18 and R19 admitted to the front-door slice with four cuts, R21
  deferred to Phase 3 because nothing in Phase 1 consumes it, a pinned-contracts registry
  created, and the twelve built-ins transcribed rather than authored. One internal phase
  collision inside Addendum B found and resolved.
---

# Decision note — Addendum B ratified; Rulings 26–31

**Ruled by:** Owner agent, 2026-09-10. Confidence **Verified** — it opened B3–B9, the
reconciliation, `GoalBlock.cs`, `GovernedRunHost.cs:69`, both contract docs, and grepped every
`.csproj` for a YAML parser.

## Three facts it verified that changed the picture

1. **No YAML parser exists anywhere** — `Yaml` matches zero `.csproj`. `ProviderRegistry` is
   constructed from **in-code rows** (`GovernedRunHost.cs:69`), never from a file.
2. **Rulings 19–25 were not filed** when it ruled. It therefore **declined to treat them as
   given** and ruled the providers question on the ladder directly. *(Now filed —
   `note-front-door-council-rulings`.)*
3. **Addendum B contains an internal phase collision**: B6/R19 (Phase 1) routes free-form→template
   *"through apply-template"*, which is **R20 — Phase 3**.

## Ruling 26 — ratified, with four cuts

**R18** (schema + validator + deterministic compiler + built-in catalog + precedence + tooltip
rendering) and **R19** (Free-form default, S1 guard, picker → validated form, per-block shape,
badges) are **admitted** to the front-door slice.

**Four cuts, each marked as extending the spec:**

| # | Cut | Why |
| --- | --- | --- |
| i | Catalog sources ship **built-in + workspace only** | The **pack** source's lifecycle *is* R22's "`/updatepack` catalog change notice". Precedence `personal > workspace > pack > built-in` is **fixed in the contract now**, so later registration is data, not renegotiation |
| ii | The Templates **catalog canvas view** is R22 | Addendum A cut 1 (Console + Terminal only) stands |
| iii | The sheet's **"Start from template" row** is cut | One picker entry point (composer header) in Phase 1; the sheet row creates a **back-edge from F4 to F2** |
| iv | free-form→template preserves content by **per-shape draft retention**, not transformation | B6 already says drafts persist per shape, per block. **The transform is R20.** This resolves B's own internal collision |

**Conditions:**
- **(a)** Ruling 16 stands — **this is not plan approval.** The revised plan must fold R18/R19, the
  ten Test-Architect Blockers, seven Simplifier Majors, C1–C8, **and file Rulings 19–25** before
  submission.
- **(b) One definition of goal-block validity.** The goal-block send gate calls
  `SpawnContract.Validate`; a test asserts the generic form engine's required-field errors and
  `SpawnContract.Validate` name **the same field set for every input**; `SpawnContractTests.cs`'s
  four tests stay **byte-unchanged**.
- **(c)** The form's `fan_out_cap`/`budget` hints **must not read as enforced** — `GoalBlock.cs`'s
  remark carries through: **validated, not enforced**, in Phase 1.
- **(d)** React stays refused; the form engine is one surface inside the composer and is **not** the
  re-entry trigger. If the conductor believes otherwise, it returns with evidence.
- **(e)** A template that fails load surfaces as a **disabled picker entry carrying its error**,
  never silently dropped. (The catalog view R18 names for this is Phase 3.)
- **(f)** The block recipe records `template_id` + `version` + `values` in the composer's existing
  draft persistence; `RunLogStore` remains Phase 3.
- **(g) Recommended, not ruled:** the template spine (schema, validator, compiler, catalog — pure
  `AiDe.Core/Sessions`) is **its own T1 node, parallel with F0/F1, in its own worktree**.

## Ruling 27 — R21 deferred to Phase 3, beside R20

**Do not build an assist-provider concept, gate, or Settings surface in Phase 1.**

R21's only consumer is the assist call path, which is **R20, Phase 3**. A first-run gate on an
account that nothing in Phase 1 calls is the class Addendum A cut 1 refused — *"a tab with nothing
behind it is dead UI"*. And R21's §4.2 test — *"no assist path exists outside the subscription
engines"* — **proves nothing while no assist path exists at all.** R13's sheet already lists
accounts with live health, which is the whole of the gate Phase 1 can honestly demonstrate.

**Marked plainly as EXTENDING the spec** — B8 phases R21 to Phase 1 and this overrides it on the
no-consumer argument.

**Condition:** when R21 lands, the gate's naming must not collide with ACP's `session/new` (the A3
collision the reconciliation found *in code*), and the gate must be a **data-driven readiness read
of `ProviderRegistry` state**, not a second health model.

## Ruling 28 — one config serialization

Moot for Phase 1 by Ruling 27. When the assist node lands, providers config takes **the same
serialization the session config actually landed in — one config format in the codebase, not two**
— and the spec's `.yaml` becomes an errata entry if that format is JSON.

*"'Meant to be hand-edited' does not buy a second parser."*

## Ruling 29 — create the pinned-contracts registry now

One small registry page — **id, version, home document, evolution rule per contract** — that
**links to `weave/1` and `loomkeeper/1` where they already live** and does not move or duplicate
them. Ships in the template-spine node.

**Because:** the `template-schema/1` validator ships in Phase 1 and **enforces a shape**; a
contract enforced in code with no documented shape is *a shape asserted from code*. B9 assumes the
home exists; the reconciliation verified it does not.

**Condition:** the declaration must say whether `min` and `tier_default` are **schema-1
constraints** or **preserved-unknown fields** — not leave it ambiguous.

## Ruling 30 — transcribe the twelve, do not author them

`when_to_use` and `why` come **byte-for-byte from B4's columns**; fields from B4's core-fields
column; **a fixture test compares the built-in catalog against the transcription with each row
citing B4**. `launch` and `change-order` are **additionally checked field-by-field against the two
real prompts in the audit log** — they were derived from them, so "authored from imagination" is
avoidable by *evidence* rather than by care.

**Three deviations recorded:** (i) `goal-block`'s `why` drops the parenthetical re-base commentary
from the tooltip — it is spec commentary that means nothing to an end user — and keeps it as
template provenance; (ii) `ruling-request` uses the B3.1 excerpt verbatim for its body;
(iii) **all built-ins ship at `version: 1`** — B3.1's `version: 3` is illustrative, and *a v3 with
no v1 or v2 claims a history that was not observed*.

## Ruling 31 — collision check

**No standing ruling is overturned.** The only collision is **Addendum B's own** — R19 routing a
switch direction through R20 — resolved by cut (iv).

Touch-points, each already a condition: cut 1 vs the catalog canvas view (deferred, R22) · cut 2 vs
conductor round-trips and the auto-applied `ruling-request` (Phase 2, needs `ConductorHost`) ·
cut 3 vs the block recipe (condition f) · React vs the form engine (condition d). Ruling 17's
ordering is unchanged, but **Phase 2 N1 now waits longer.**

**Condition:** Rulings 19–25 filed, then **the collision check is re-run against them** and any hit
reported **before plan approval**. *Their absence was the finding, not a clearance.*
