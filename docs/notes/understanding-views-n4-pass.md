---
id: note-understanding-views-n4-pass
title: "N4 hard vetoes cleared for D-0 spec; architecture (N5) may start; spec stays draft"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, N4, D-0]
links:
  - { to: spec-understanding-views, rel: relates-to }
  - { to: note-understanding-views-owner-ruling, rel: depends-on }
  - { to: note-understanding-views-owner-n1-disposition, rel: depends-on }
  - { to: plan-understanding-views, rel: relates-to }
review-by: 2027-03-14
review-suggested:
  - { by: spec-understanding-views, on: 2026-09-15, reason: "N4 PASS recorded; N5 architecture unblocked" }
summary: >-
  Conductor records N4 PASS from non-author receipts on docs/specs/understanding-views.md
  at 962ad56e. Authors did not self-clear. Spec status stays draft. Blast radius: N5
  architecture may open; no src/, no allow-list row, no main.
---

# N4 hard vetoes cleared for D-0 spec; architecture (N5) may start; spec stays draft

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** **Verified** (reviewer receipts opened; spec HEAD opened)
- **Made during:** Conductor resume after host reboot, session `grok-understanding-views-conductor`, 2026-09-15. Predecessor: N4 repair commits `a1fe989f` and `962ad56e`.

## Evidence opened (not a paraphrase of liveness)

Spec at `962ad56e` (`docs/specs/understanding-views.md`). Gate line still said **pending N4 re-review** (authors do not self-clear — BoK §II.3 D3). Re-review receipts from conductor session `01a0a2a6-114a-7c72-9749-379fce91a343` (Grok 4.6, ended 2026-09-15T02:59Z):

| Lens | Session | Verdict | Clears veto? |
|---|---|---|---|
| Data & Persistence Architect | `01a0a2f6-2021-7de2-92fd-9d6c25894e9e` | **PASS** | yes — grain `(path, kind)`; Coverage two-valued; `not-recorded` is Disclosure; no `declared_at` mint |
| UX Researcher / IA | `01a0a2f6-2021-7de2-92fd-9d8c93e8a344` | **PASS** | yes — View source / Reveal in graph named; skip-count; unindexed leaf; empty → Show Graph. Residual: pointer path for Reveal |
| Test Architect (after T5c pin) | `01a0a2fb-4aa4-78c3-94ef-10c93248feb9` | **PASS** | yes — F* `omit_probe/` + `omit_probe_2/`; US-T5c survivor pin. Prior re-review `01a0a2f6-2021-7de2-92fd-9d72ae328f90` was **BLOCK** on unsatisfiable F*∩T5c; closed by `962ad56e` |
| UX & Accessibility | `01a0a2e6-7afc-7ed3-a672-22fc955eeec9` (first pass) | **PASS-WITH-CONDITIONS** | yes — conditions already in Part C (stale-while-refresh; Enter/Ctrl+Enter; UIA Name kind+coverage; 28px full-row hit; `{colors.unverified}`) |

First-pass BLOCKs (Data, Test, UX/IA) were repaired in `a1fe989f` / `962ad56e`. This note does **not** re-judge those repairs; it records that the **non-authors** re-reviewed the repaired text and cleared the hard vetoes.

## The call

**N4 PASS.** N5 `/define-architecture` may start for **D-0 only**: one kind admission + the census query (or two ADRs if kind and query must split). Spec `status` stays **draft** — a pass of the adversarial gate is not acceptance of the spec as a product artefact.

Conductor does not override a hard veto. None remains.

## Residuals handed to N5 / N7 (not reopened as N4 blockers)

- N5 must make the T5c “named drop set” arrangeable (test-overridable cap or equivalent). A prefix-integer cap plus alphabetical walk that drops `unindexed_probe` before `omit_probe/` is already a forbidden arrange in the spec.
- N5 implements the spec’s grain **close**, not Owner’s unclosed Coverage `not-recorded` quote.
- Skip-set survivor (`CSharpScopeDiscovery` vs `UnanalysedLanguages` vs TS extractor) — architecture unifies; no fourth `HashSet`.
- Tree toolkit — **N7 Spike Protocol**. Do not freeze WPF `TreeView` in N5.
- Pointer primary matches Enter; Reveal in graph on the existing node menu — N8, not a veto.
- No Proof Pack until implement (red-before-green still required then).

## Alternatives dismissed

- **Treat liveness “N4 PASS” as the gate.** Liveness is untracked. The spec still said pending. Two definitions of one quantity (defect signature). This note + the spec gate line are the record.
- **Mark the spec `accepted`.** Authors and Conductor do not self-accept. N4 is the review gate, not product acceptance.
- **Re-run N4.** Receipts exist; the T5c pin is on HEAD. Re-running would spend the panel budget on a closed question.
- **Start N5 without recording the pass.** The spec forbids it (`N5 does not start until N4 records a pass`).

## What the Conductor may do next

1. Open `coord worktree new --branch understanding-views-architecture` from this tree’s HEAD.
2. Dispatch N5 (`enterprise-architect` + `orchestrator`) in that tree. Council is **N6**, after the ADR exists. Spike toolkit is **N7**.
3. Join target remains `understanding-views`, not `main`.

## What the Conductor must not do

- Author `src/`. Add allow-list or menu rows. Touch Atlas / `Understanding/**`. Admit D-1…D-6. `coord regen` / `coord install` from a worktree.

## Promotion rule

N5 writes **one** ADR for D-0 kind admission + census query (split only if they cannot share). That ADR does not supersede this note. This note is the N4 gate record.
