---
id: note-front-door-rulings-43-44
title: "Decision note — Rulings 43 and 44: attachment caps, and the Phase-1 posture accepted with an expiry"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, ruling, f4, security, residual-risk, attachments, dc-116]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: note-front-door-rulings-41-42, rel: depends-on }
  - { to: review-front-door-council, rel: relates-to }
review-by: 2026-12-10
summary: >-
  Security's F4 convening cleared on plan text alone with conditions C9-C20. Three things were left
  to the Owner: an erratum to its own Ruling 42 condition, the attachment caps, and whether to
  accept the Phase-1 auto-allow posture. It accepted the posture but corrected the sentence
  describing it, and gave the acceptance a hard expiry rather than an open end.
---

# Decision note — Rulings 43 and 44, and the Ruling 42 erratum

**Ruled by:** Owner agent, 2026-09-10. Confidence **Verified** on every code and spec fact — it
opened `GoalBlock.cs`, `GovernedRunHost.cs`, `ConductorEntry.cs`, `SessionDocumentSurface.cs`, the
spec's §14.3 and §4.4, and the plan's whole F4 section.

## Context

Security was convened **before a line of the send seam was written**, because its own earlier review
ordered it. It returned **BLOCK on the plan text, not on the design** — clearing the moment C9–C20
were in the plan verbatim, with no code required. They are. Three items it declined to decide came
here.

## The Ruling 42 erratum — a condition that carried an unopened repo fact

Ruling 42's condition (2) instructed the plan to say *"the lease is the goal block's
`lease.exclusive` (spec §14.3)"*. The conductor wrote that in verbatim. **Both were wrong**, and
Security caught it:

- `GoalBlockFields.All` is **exactly six** — `goal, done_when, not_in_scope, tier, fan_out_cap,
  budget` — with **no lease anywhere** in `GoalBlock.cs` (`:25-33`).
- `ConductorEntry.cs:123` reads `Lease: new Lease(file.Lease ?? [])` as a **top-level peer**
  (`:162`).
- The spec at §14.3 puts `lease:` **beside** `goal_block:`, not inside it.
- And as written the clause **collided with F4's own requirement** that `SpawnContractTests.cs`'s
  four tests stay byte-unchanged, since `TheSpecNamesExactlySixFields` asserts that six-name list.

**Ruled:** the erratum stands as written in the plan; the condition is amended to the corrected
clause, and **C17's oracle replaces the literal-`["**"]` comparison** — a universal-lease detector
that **no spelling of "everything"** (`**`, `**/*`, `**/**`) can pass.

**Ruling 42's substance is unchanged**: no lease leaves the sheet, a universal lease is refused, and
`new Lease([])` failing closed is the control.

**One condition, and it is a good catch:** the plan and the conductor's message spelled the oracle's
input differently — `"/no-lease-covers-this"` versus `"/no-lease-covers-this"`. *"Pick one and
make the plan and the test agree; I do not know `Lease.Covers`'s behaviour on a leading U+0001 and
neither should the test depend on it."* **Resolved to the plain path**, which is what the plan now
carries.

## Ruling 43 — the attachment caps

**32 KiB per file · 128 KiB per send · 5 files per send.** Each a **named constant** with its own
red-first test. The fence header names the source file **and its byte count**.

> **Because:** the only Phase-1 control is a human reading the compiled text, so **the cap must
> bound what a human can actually read, not what a file can hold.** 32 KiB is roughly 800 lines of
> source, and this repository's larger files (`GovernedRunHost.cs`, `GoalBlock.cs`) are ~300 lines /
> ~14 KB, so real files fit. 64 KiB is ~1,600 lines, **which is scrolled past, not read.**

**Confidence: Inferred, with the gap named** — *"no measurement exists of how much compiled text the
operator reads before skimming; the model is 'lines a human reads in one sitting'."* Rather than
hide that, the ruling closes it:

**Condition:** the `metrics` kind records **per-send attachment count and bytes on the normal path**,
so the numbers are **revisited on data at Phase 2 instead of re-argued**. Over-cap is a **refusal
naming the file and its size**, never truncation. **No config knob** — nobody asked for one.

The byte count in the fence header is the one addition to Security's draft, and its reason is
one line: *"it is what makes the human's read informed."*

## Ruling 44 — the Phase-1 posture accepted, its description corrected, its acceptance given an expiry

**Accepted as an Owner residual, NOT a floor trip.**

> **Because:** this is **the spec's own Phase-1 posture**, not a gap the code invented — §14.3's
> example policy is `edits: worktree-only, shell: allow`, and §4.4 places human permission answers
> in the conductor's shared queue, which is Phase 2 by the roadmap. **Security holds the floor, was
> convened specifically for this, and chose deferral over veto.** What tipped it: the injected
> content in F4 is content **the human personally chose** — a paste or a picked file — and **C15
> guarantees nothing reaches the prompt that the human did not see in the compiled view.**

### Condition (1) — the containment sentence overstated it, and that is the important part

Security wrote, and the conductor transcribed into the plan:

> *"Phase-1 containment is therefore the worktree plus one human read, and nothing else."*

**Verified false, in the direction that matters.** The ACP session's cwd **is** the worktree
(`GovernedRunHost.cs:117-119`), but `Decide` bounds **only edit kinds** by lease and worktree
(`:285-313`). **For a shell call the worktree is a starting directory, not a boundary** — `cd ..`,
absolute paths, network and push are all reachable, and **the spec's own `network: deny, push: deny`
is not implemented anywhere in `Decide`.**

The plan now reads: *"Phase-1 containment **for edits** is the lease inside the worktree; **for every
other tool kind it is one human read, and nothing else.**"*

This is a **third-order correction** — Security wrote the paragraph, the conductor transcribed it,
and the Owner caught it by opening `Decide`. Three reviewers, and the sentence was still wrong until
someone read the function it described.

### Condition (2) — the acceptance is not open-ended

> **The acceptance is VOID, and human Allow/Deny plus policy bounds on non-edit kinds become a
> BLOCKING PRECONDITION, the moment ANY text reaches the prompt that the human did not personally
> read in the compiled view.**

That means: conductor-drafted replies (Phase 2), mention or template **expansion** (R20),
graph-query results, or any assist. **Phase 2 cannot start those nodes without it.** Recorded in the
plan's Deferred list, and required in F4's Proof Pack residual entry.

**The assumption that carries the ruling, stated so it can be falsified:** *"Inferred that this is a
single-operator desktop app whose only user is the one doing the reading — **if that assumption is
wrong, this is a Security floor question and goes to the human.**"*

### Condition (3)
F4's Proof Pack entry records this residual under **this ruling's title**, not as a Security
finding. It is an accepted decision, not an outstanding defect.

## The Owner's calibration note, which applies to the Owner too

Three of the conductor's claims were corrected by Security in one convening: a stale file citation,
the lease provenance, and a description of F2's permission surface that **implied containment it
does not have** (it is Dismiss-only — a notice, not a gate). The Owner's response named the class
and then applied it to itself:

> All three were of the same class — **a code shape asserted from memory or from a persona's summary
> rather than opened.** Ruling 42's condition was mine and belongs to that class too. **The
> conditions I write into the plan are repo facts as much as yours are; treat them as claims until
> you have opened the file they cite, and say so when you file them.**

Recorded against **DC-116**, which until now was scoped to the conductor briefing an agent. It is
wider than that: **any authority's output becomes a repo claim the moment it is written into a plan,
and a ruling is not exempt from the standard it enforces.**

## Not ruled

Privacy's **C14(e)** convening is outstanding — whether anything outside the workspace root may be
attached at all, and what the audit trail may record. Nothing above pre-empts it: *"if Privacy
refuses outside-workspace attachment outright, Ruling 43's caps simply apply to a smaller set."*
