---
id: adr-0016-bounded-context-declaration
title: "ADR-0016 — Bounded contexts are declared in one reviewable file, never inferred"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "3"
tags: [architecture, ddd, bounded-context, phase-3, curation]
links:
  - { to: architecture, rel: implements }
  - { to: design-phase-3-architecture-data-infra, rel: refines }
  - { to: adr-0001-derived-evidence-views, rel: relates-to }
review-by: 2027-02-28
summary: >-
  A bounded context is a modelling decision with no evidence in a repository, so it is declared in a
  committed file and validated against extracted symbols. Folder convention is rejected on measured
  grounds: the obvious candidate in a real repository has 31 folders that are UI features, not
  contexts.
---

# ADR-0016: Bounded contexts are declared, never inferred

- **Status:** Proposed — **this one needs the product owner's confirmation.** Every other Phase-3
  input has evidence behind it; this one is the decision that evidence cannot make.
- **Phase:** 3

## Context

The Phase-3 design needs bounded contexts to group the C4 and domain projections. Everything else in
the phase is extracted from an artifact. A bounded context is not: it is where a team decides one
model ends and another begins, and two teams looking at identical code draw it differently.

## The options

### A — Infer from folder structure

Tempting, and **measurably wrong.** The obvious candidate in the corpus repository is
`src/TheTerrace/Features/`, which has **31 folders**: `Admin`, `Ai`, `AskAi`, `Chronology`,
`ClubAnalysis`, `CoachDossier`, `Competitions`, `Conversation`, `DataCatalog`, `Digest`, `Fixtures`,
`Identity`, `Matches`, `Media`, …

Those are **UI features**, not bounded contexts. `AskAi` and `Ai` and `Conversation` almost certainly
share one model; `Identity` is plausibly its own context; `DataCatalog` is arguably not a context at
all. Inferring 31 contexts from 31 folders would produce a diagram that looks authoritative and
teaches the user something false about their own system — the worst outcome available, because a
wrong boundary is harder to notice than a missing one.

### B — Attributes in code (`[BoundedContext("Sales")]`)

Puts the declaration next to what it describes, which is genuinely appealing. Rejected because:

- It requires **editing the analysed repository** to be analysed. AiDe reads repositories; a tool
  that asks you to annotate your code before it can help you has changed the deal.
- The boundary is then scattered across hundreds of files, so *"what are our contexts?"* stops being
  a question anyone can answer by looking at one thing.
- It cannot express a context that spans repositories, which is where context boundaries most often
  actually matter.

### C — One declared file, validated against the extracted symbols

## Decision

**A single committed file, `docs/bounded-contexts.yaml`, authored by a human, validated against the
symbols the extractor found.**

```yaml
contexts:
  - name: Matches
    description: Fixtures, live match state, and the reports derived from them.
    includes:
      - TheTerrace.Features.Matches.*
      - TheTerrace.Features.Fixtures.*
    tables:
      - Enquiry
      - Competition
  - name: Identity
    includes:
      - TheTerrace.Features.Identity.*
```

Three properties make this the choice rather than just a file format:

1. **Validated, not merely parsed.** A context naming `TheTerrace.Features.Nonexistent.*` **fails
   loudly**. A declaration file that silently tolerates stale entries becomes fiction within a
   release, and fiction that looks like configuration is worse than no configuration.
2. **Coverage is reported.** Symbols matched by no context are counted and named, so *"we have
   contexts"* cannot mean *"we have contexts for 12% of the code"* without anyone noticing. This is
   the same disclosure discipline the extractor uses.
3. **Overlap is an error, not a merge.** A symbol claimed by two contexts fails validation. Bounded
   contexts that overlap are not bounded, and quietly picking the first match would hide a real
   modelling problem behind a working tool.

## Consequences

**Good:**
- The one input with no evidence behind it is visibly a human artifact, reviewable in a diff.
- Validation turns it into a *checkable* claim, which is more than most context maps ever get.
- Contexts can span projects and repositories, because the file is not tied to either.

**Bad, and accepted:**
- **A repository with no file gets no contexts** — the domain projection is unavailable rather than
  guessed. Consistent with the rest of the phase: absent and disclosed beats present and wrong.
- Someone has to write it, and nothing writes it for them. A generated *starting point* from
  namespaces is a reasonable convenience later; it must never be the default, or option A returns
  through the back door.
- It will drift from the code. Validation makes drift **fail** rather than accumulate, which is the
  most that can be done about it.

## What would reverse this

If a repository is found where contexts genuinely are declared in an extractable form — an explicit
modular-monolith manifest, or a `.NET Aspire` app model naming service boundaries — that becomes a
*second evidence source* feeding the same projection, with `Verified` confidence rather than
`Declared`. That is an addition, not a reversal: the file remains the answer for repositories that
have no such artifact, which is most of them.
