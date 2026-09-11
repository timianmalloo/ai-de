---
id: adr-0037-family-craft-profile-dimension
title: "ADR-0037 — The family craft profile is a Type-2 dimension realised as one immutable pack-owned markdown file per (family, version), selected mechanically from the engine's provider, pinned on the envelope by (family, version, sha), applied as a template in v1"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-d"
tags: [architecture, compile, craft-profile, dimension, type-2, pack, addendum-d, dm-data-modelling]
links:
  - { to: architecture, rel: implements }
  - { to: spec-addendum-d-compile-step, rel: implements }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: depends-on }
  - { to: adr-0034-envelope-event-store, rel: relates-to }
  - { to: adr-0036-compile-mode-ladder-deployment-gates, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  How to make a prompt effective on one model family is a versioned, sourced document the pack owns
  — `.claude/knowledge/craft-profiles/<family>@<version>.md` (Copilot: `.github/knowledge/`) — one
  immutable file per version, never overwritten; the pre-compile selects the family from
  EngineCatalog.Find(engineId).Provider and the current version (max semver present, one per family),
  pins (family, version, sha) on the envelope, and in v1 applies the profile as a deterministic
  template (preamble + suffix, a labelled block in the sent bytes); a content change without a version
  bump fails the sha test; the pack's deployment map is append-only for this set. Rejected: one file
  per family overwritten in place; a profile in the workspace; a model rewrite of the framing.
---

# ADR-0037: The family craft profile is a Type-2 dimension — one immutable file per version, pack-owned, applied as a template

- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 (`addendum-c-chain`) with the
  **Data & Persistence Architect** (veto holder) in Peer Mode; the Simplifier's soft veto at the spec
  gate recorded and overridden by the veto-holder's condition; attacked at the gate
- **Context spec/architecture:** `spec-addendum-d-compile-step` §A6 (`FamilyCraftProfile` root and
  invariant; the `craft_profiles` dimension), §A7 (Model row), §A8.1, §A12.5, DM11 (e); Ruling 69
  (framing is mechanical in v1); `/updatepack` and the pack's deployment map

## Context

The spec fixes the profile's **shape** (four sections, each claim carrying its measurement or its
source), **selection** (mechanical: `family = EngineCatalog.Find(engineId).Provider` — `anthropic |
openai | github`, `EngineCatalog.cs:72-100` per the spec **[Inferred — not re-opened]**),
**versioning** (Type-2: a change is a new version; envelopes pin `(family, version, sha)` and read
the same forever), and **ownership** (the pack). The D&P Architect's Blocker at the spec gate —
*"Type-2 declared, Type-1 realised (one profile file overwritten)"* — was closed by **one immutable
file per version**; the Simplifier's soft veto asked for one file per family until a second version
exists and was overridden in writing: an overwritten file is Type-1 whatever the frontmatter says,
and the cost is a filename convention. A1 architects to that and decides what was left to
architecture: **where the files live, who deploys them, how "current" is computed, how the pin is
checked, and what happens when a referenced version is gone.**

DM principles: DM10 (history rule per attribute — the four content sections Type-2; `owner` and
`review-by` Type-1, recorded as a decision to discard history), DM11 (e) (the executed invariant
test), derive-don't-store (the profile's *contents* are never on the envelope; only the triple).

## Decision

We will:

1. **Locate the dimension in the pack's knowledge tree:**
   `.claude/knowledge/craft-profiles/<family>@<version>.md` in a Claude Code consumer
   (`.github/knowledge/craft-profiles/` for Copilot), carried by the pack's deployment map so
   `/updatepack` delivers new versions and **never removes a listed one** — the `craft-profiles/`
   artifact set is **append-only in the deployment map**, and `tools/verify-bundle.ps1`'s successor
   test fails when a listed path disappears. **The dimension has its own registry row at the pack:**
   `craft-profiles/manifest.json` carries one `(family, version, sha)` per file, and the bundle test
   **recomputes each file's canonical sha and fails on a mismatch** — that is the oracle for "a
   content edit without a bump" (the pinned triples in consumers' `.aide/` envelopes are not visible
   to the pack, so without this row the edit would be undetectable and `/updatepack` would overwrite
   every consumer's copy). Envelopes reference the registry's triple; it is not a second home. Each
   file carries V2 frontmatter (`id: craft-profile-<family>-<version>`, `type: craft-profile`,
   `family`, `version` (semver), `owner`, `review-by`) and the four sections
   **ceremony · drift compensation · formatting · refusal & recovery**. The files sit **outside
   `docs-graph.py`'s writer roots** (it rewrites `review-suggested` blocks in place,
   `docs-graph.py:1157-1164`; whether its scan reaches `.claude/knowledge/` is **Flagged** — the
   slice asserts it does not, or excludes `type: craft-profile` from the rewrite), so no tool can
   make an immutable file Type-1 by accident.
2. **Selection and "current" are one function in the pre-compile:** `SelectProfile(engineId)` →
   `family` from the catalog's `Provider`; `current` = the highest semver present for that family in
   the deployed set, exactly one per family; **`none` is a named state** (no file for the family) —
   the framing block is absent and Prepare's *what was read* names `profile: none`. A session bound
   to a family with no profile compiles with `none`; nothing invents a framing.
3. **The pin is the triple, checked on read; the sha domain is the content, canonicalised.** The
   pre-compile appends the `family_profile` decoration `{family, version, sha}`, where **`sha` is
   sha256 over the four content sections only, canonicalised (LF line endings, trailing whitespace
   stripped, the frontmatter block excluded)** — with the general rule stated once: **any
   frontmatter attribute that changes the sent bytes enters the sha domain, and adding one bumps
   `canonical-sha`** (D-D4's `constitution_delivery` is defined by D-D4 when it lands, not carved a
   special case now — the Simplifier's cut of a field no file carries) — so the Type-1 maintenance
   fields (`owner`, `review-by`) can be edited in place without failing the sha test, and one
   version yields one sha
   on every consumer regardless of checkout line-ending policy (this repository pins LF via
   `.gitattributes`; a consumer's policy is not ours — the D&P Architect's finding). `Project()` and
   the eval read the triple, never the current version; a referenced version whose file's sha differs from the pinned
   sha fails the DM11 (e) test (a content edit without a bump); a referenced version whose file is
   **gone** reads *"profile not recorded"* — never the current one — and the rebuild test
   (`projection_sha`) still passes because the triple is what it hashes.
4. **v1 applies the profile as a deterministic template** (Ruling 69): the pre-compile renders the
   profile's preamble and suffix as a **labelled block inside the compiled prompt**, so the compiled
   disclosure is still the sent bytes and the `prompt_sha`/`inputs_sha` domain includes the profile
   sha (an unbumped profile edit cannot reuse stored decorations — ADR-0036 Gate 3). A model rewrite
   of the framing is D-D3, admitted only by its own eval.
5. **The first profile, `anthropic@1.0.0`, is authored by `/collectknowledge` with sourced claims**
   — a profile that *believes* a family adds ceremony is a memoir; one that measured it on N turns is
   evidence. Until it exists, compiles run with `none`; the operator's perception that OpenAI models
   *"add more ceremony and more drift per turn"* is the hypothesis the `openai` profile must measure
   before it asserts (D-D4).

## Alternatives considered

- **One file per family, overwritten in place, with `version` in the frontmatter:** rejected (the
  D&P Architect's Blocker at the spec gate) — Type-1 in fact; a past envelope's pinned version could
  no longer be read; the sha test would fail on every legitimate edit.
- **The profile in the workspace (`.aide/` or `docs/`):** rejected — it holds no workspace data and
  must be identical across every repository a family is used in; a repo-local override is a
  *recorded deviation*, not the home.
- **The profile's contents copied onto the envelope:** rejected — DM7; the triple is the reference;
  the pack keeps the bytes; "profile not recorded" is the honest degraded read when the bytes are
  gone.
- **A JSON/YAML profile with typed fields:** rejected for v1 — the profile is prose with sources
  (claims, measurements, patterns), consumed as a template; a typed schema would be a schema for
  text with no reader beyond the template renderer (DM15).
- **Deploying the profiles through the repository's own `docs/knowledge/` rather than the pack's
  managed set:** rejected — `/updatepack` would not carry them and a consuming repository could not
  receive a new version without hand-copying.

## Consequences

- **Positive:** every compile's framing is reproducible from `(family, version, sha)`; a profile
  edit is a versioned, reviewable file; the pack carries the dimension to every consumer.
- **Negative / accepted:** a filename convention (`<family>@<version>.md`) the deployment-map test
  enforces; the deployed set grows monotonically (bounded by how often a profile is revised).
- **Follow-ups / new risks:** the pack's deployment map and its bundle test must gain the
  append-only rule for this set (an `ai-forward` change, delivered with the first profile); D-D4's
  `constitution_delivery: inline` mode must respect the 60,000-token ceiling and is not built until a
  second engine's adapter entry is observed.

## Falsifying tests (headless)

1. **DM11 (e):** a fixture set with `anthropic@1.9.0` and `anthropic@1.10.0` → `current ==
   1.10.0` (semver, not lexical order); an envelope pinned to `1.9.0` reads `1.9.0`'s content;
   editing a content section of `1.9.0` without a bump → the sha test fails; editing only
   `review-by` → the sha is unchanged; a CRLF copy and an LF copy of one version → one sha; deleting
   `1.9.0`'s file → the read yields *"profile not recorded"*, never `1.10.0`; a family with no file →
   `none` and no framing block. **One committed canonicalisation fixture** under `craft-profiles/fixtures/`
   (a CRLF file, an LF file, one with trailing whitespace, one with an edited frontmatter, each
   with its expected sha) is asserted by **both** implementations of the canonical sha — the C#
   pre-compile and the pack's bundle test — so the two cannot drift (the D&P Architect's pass-2
   condition; two definitions of one integrity oracle is DM-A). **The cross-repo contract has an
   owner, a version and a trigger:** the canonicalisation rule and its fixture are owned by
   `ai-forward` (`@timianmalloo`), versioned as `canonical-sha/1` in the fixture's manifest, and any
   change to either implementation re-runs both against the fixture before it ships (the
   Enterprise Architect's finding).
2. **Rebuild independence:** `projection_sha` over an envelope whose profile file is gone still
   equals `submitted.projection_sha` (the triple is hashed, not the bytes).
3. **Deployment map and registry:** removing a listed `craft-profiles/` path from the map fails the
   bundle test; adding one passes; the bundle test recomputes every file's canonical sha against
   `craft-profiles/manifest.json` and fails on a mismatch — **this test lives in `ai-forward`** (the
   pack), and its cross-repo trigger is the first profile's delivery; the check that runs **here** is
   the consumer-side recompute: `SelectProfile` verifies **the selected file's** canonical sha
   against the deployed `manifest.json` **at envelope open** (not every profile on every debounced
   pre-compile — the set is the bundle test's job; the Simplifier's finding) and reads a mismatch as
   *"profile not recorded"* with the reason.
4. **Template application:** the compiled disclosure contains the profile's preamble and suffix as
   a labelled block and the sent bytes equal the disclosure (`text_sha256`); with `none`, no block.
5. **Drift domain:** a profile bump changes `prompt_sha`'s companion `profile.sha` in `inputs_sha`,
   so a re-prepare after a bump makes one request rather than reusing stored decorations.

## LOA mapping

T0 (selection, pinning, template application). DM: a dimension with natural key `family` (Type-0,
the catalog's provider) and version identity `(version, sha)`; content attributes Type-2;
maintenance attributes Type-1 by recorded decision, realisable because the sha domain excludes the
frontmatter. Patterns (named as the Patterns Expert corrected them; neither is an LOA-catalog name
and both are marked so): **immutable versioned artifact, content-addressed and lockfile-pinned**
(`<family>@<semver>.md` + `manifest.json (family, version, sha)` + the recomputed-sha test + envelopes
pinning the triple — the package-manifest-plus-integrity-lockfile idiom, npm `integrity` / NuGet
lock), realised as a **closed lookup by file naming**.

## Evidence

- **Verified:** `spec-addendum-d-compile-step` §A6 (the invariant and its gate history), §A12.5;
  Ruling 69.
- **Inferred (cited from the spec, not re-opened here):** `EngineCatalog.cs:72-100` (`Provider` is
  `anthropic | openai | github`).
- **Flagged:** `anthropic@1.0.0` does not exist; the pack's deployment map has no `craft-profiles/`
  set yet.
