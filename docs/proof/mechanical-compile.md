---
id: proof-mechanical-compile
title: "Proof Pack — CV-2, the mechanical compile: the append-only envelope store, the five event records, the fold and its projections, PreCompile, Projection.Project as the one producer of the sent bytes, Prepare's editable derived lines, purge, and the compile contract shipped inert"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "addendum-d"
tags: [proof-pack, conversation-lane, cv-2, addendum-d, compile, envelope-store, projection, prepare, purge, adr-0033, adr-0034, dm-data-modelling]
links:
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: implements }
  - { to: adr-0034-envelope-event-store, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: proof-composer-as-conversation, rel: refines }
  - { to: note-addendum-cd-architecture-p1-inputs, rel: relates-to }
  - { to: adr-0028-mode-cohort-not-partition, rel: relates-to }
  - { to: adr-0037-family-craft-profile-dimension, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Evidence for CV-2 on the Conversation lane (Addendum D slice D-1 with D-0 folded).
---

# Proof Pack: CV-2 — the mechanical compile

- **Tier:** T2 · **Fan-out cap:** 3 (persona reviews) · **Session:** `cv-2` · **Author:** `claude-cv-2`.

## E7 change-surface list (written before coding; ticked at close)

| Surface | Change | Writer | Compute reader | Status |
|---|---|---|---|---|
| store | `EnvelopeStore` (`compiled-envelope/1`; one file per session; `Append` + reader; `prev_sha` chain; `FileShare.None`) | `ComposerSendGate.Send` (opened · decorated · submitted), `SessionDocumentSurface` (consumed) | `EnvelopeStore.Read` / `ReadFile` → `Fold` | pending |
| event records | `Opened · Decorated · Called · Submitted · Consumed` (append-only facts) | as above | `Envelope.Fold` | pending |
| fold / projections | `Envelope.Current`, `Confirmed`, `EffectiveMode`, `Outcome`; derive, never store | — | `Projection.Project`, Prepare, `aide compile fold` | pending |
| `PreCompile` | the mechanical rung: `ceilings` snapshot (writer named), `task_class` (`session-default` / `operator`), `family_profile` (none), `template_applied`, `attachments` (refs), the structure lines, the tier override | the Send gesture | `Projection.Project` | pending |
| `Projection.Project` | the ONE producer of the sent bytes; tier by §A9 + R4; cap; budget; lease from `opened.source_text`; `projection_sha` | — | `ComposerSendGate.Send`, `ComposerCompiler.Decorations`, `Cli/CompileFold.cs` | pending |
| wire | `GovernedRunRequest` byte-identical (fourteen parameters; C16 at two sites) | `ComposerSendGate.Send` | `GovernedRunHost` (unchanged) | pending |
| client | `ComposerSendGate.Send` via `Project()`; `ComposerSendContext.TaskClass` from `default_task_class` (non-null) | `SessionComposerBinder` | `ComposerSurface` | pending |
| UI | Prepare: the tier control and the class control on the decoration line (editable, with provenance), the compiled disclosure rendering `Current`, the degraded reason | `ComposerSurface` | the operator; the E7 consistency test | pending |
| compute reader | `task_class_source` (expand-only, v6→v7) read by `ScoredEpisode.TaskClassSource` and `LeaderboardCell.DefaultedClass`; `aide compile fold`; `aide session purge` | `SessionDocumentSurface` (after the run) / the CLI | `Leaderboard`, the eval, the operator | pending |
| tests + census + ledger | the reds below; the two censuses; the terminal ledger | — | — | pending |
