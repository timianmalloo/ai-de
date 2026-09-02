---
id: diagram-layers
title: "Layered architecture — capability tiers"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, layers, loa, tiers, architecture]
links:
  - { to: architecture, rel: documents }
  - { to: diagram-component, rel: relates-to }
review-by: 2027-09-02
summary: >-
  Where each AI-DE capability sits in the LOA tier ladder, and the single rule that governs the
  boundary: a model may explain a bounded, already-selected result, and may never select it.
---

# Layered architecture — capability tiers

AI-DE is a tool for directing models that is itself almost entirely deterministic. That is the
point of this diagram: everything load-bearing is at **T0**, and the tiers above it are optional,
gated, and structurally unable to reach the source of truth.

```mermaid
flowchart TB
  classDef t0 fill:#1A1F26,stroke:#5FB98F,color:#E4E9EF
  classDef t12 fill:#1A1F26,stroke:#D8A650,color:#E4E9EF,stroke-dasharray:4 3
  classDef t3 fill:#1A1F26,stroke:#98A3B2,color:#98A3B2,stroke-dasharray:4 3
  classDef gate fill:#0D1014,stroke:#E07A6F,color:#E07A6F

  subgraph T3["T3 — cognition, opt-in and disclosed"]
    Explain["Agent explanation / synthesis<br/>bounded, cited, budgeted"]
  end

  subgraph T12["T1 / T2 — later, only on a measured need"]
    Rank["Local reorder-only ranking<br/>within a T0-selected, T0-truncated set"]
  end

  Gate{{"Capability gate<br/>governs everything above T0"}}

  subgraph T0["T0 — deterministic floor"]
    Identity["Workspace membership · path validation<br/>session binding · dispatch receipts"]
    Facts["Extractor scope replacement · evidence ingestion<br/>fact constraints · impact traversal"]
    Render["Layout · filtering · diagram rendering<br/>accessibility alternatives"]
    Tools["MCP tool schema validation · authorization<br/>result truncation and byte-bounding"]
    Score["Weave scoring · leaderboard composition<br/>liveness · coordination fold"]
  end

  Explain --> Gate
  Rank --> Gate
  Gate --> T0
  Explain -. "may never mutate facts<br/>or dispatch prompts" .-> Facts

  class Identity,Facts,Render,Tools,Score t0
  class Rank t12
  class Explain t3
  class Gate gate
```

## Why each capability sits where it does

| Capability | Tier | Why |
|---|---|---|
| Workspace membership, path validation, session binding, prompt dispatch receipt, coordination fold | **T0** | Identity, authorization and audit invariants. A model would make them less deterministic, which is the whole of the objection. |
| Extractor scope replacement, evidence ingestion, fact constraints, impact traversal | **T0** | Compiler, parser and SQL behaviour is authoritative and reproducible. |
| Visual layout, filtering, diagram rendering, accessibility alternatives | **T0** | Renderer output must be testable and stable. |
| MCP tool schema validation, read/write authorization, result truncation and byte-bounding | **T0** | A typed boundary, least privilege and context bounding are deterministic — **and a model never chooses what is omitted**. |
| Weave scoring and leaderboard composition | **T0** | The scorer is pure and model-free by construction: it consumes typed deterministic signals, which is what makes an injection fixture unable to change a score. |
| Reorder-only ranking inside an already-selected result | **T1/T2**, later | Only after a measured need beats a deterministic baseline. It never selects which evidence is dropped and is never on a source-of-truth path. |
| Agent explanation or synthesis over bounded evidence | **T3**, opt-in | May explain what was selected; disclosed, cited and budgeted; cannot mutate artifact facts or dispatch prompts. |

## The rule the diagram exists to make visible

The gate governs **any** capability above T0 — ranking included, not only "explanation". The
distinction that matters is not how clever the tier is but whether it can decide what a human
sees: selection and truncation stay deterministic, so a model can never quietly drop the evidence
that would have changed the reader's mind.

This is LOA P1–P5: deterministic work at the floor, cognition separated from execution, and a
deterministic verifier behind every consequential operation.

## Confidence

| Claim | Label | Basis |
|---|---|---|
| The tier table | Verified | `docs/architecture.md` §"Capability-tier allocation", quoted row for row. |
| Weave scoring is T0 | Verified | `WeaveScorer` takes `DeterministicEpisodeSignals` and a `TimeProvider` and calls no model; `AdvisoryScoring` is the separate, gated path. |
| T1/T2 ranking is not built | Verified | No ranking component exists in `src/`; the architecture marks it "later". |
