---
id: note-conductor-spec-errata-policy
title: "Decision note — the received spec is byte-frozen; corrections are quoted-line notes"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, spec, errata, provenance, engine-catalog]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: note-conductor-acp-lane-separate-shape, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Policy for a spec that is authoritative but overtaken by fact: the received HTML stays
  byte-intact and corrections are decision notes that quote the line they correct. Applied
  to the ACP adapter packages, the policy shows this was never an erratum - the spec names
  no package, so the package identity is a catalog fact to pin.
---

# Decision note — the received spec is byte-frozen; corrections are quoted-line notes

**Ruled by:** Owner agent, 2026-09-09. Confidence: **Verified** — the Owner grepped the spec; I
independently re-ran the search and confirm **zero** hits.

## The general policy

The received specification (`docs/specs/conductor/ai-de-conductor-spec-v1.html`) stays
**byte-intact**. Its SHA-256 in the provenance README is the control. Corrections are recorded as
**decision notes linked from that README** — never by editing the HTML.

An **erratum note MUST**:

1. **quote the spec line it corrects, verbatim, with its line number**;
2. cite the evidence that the fact changed; and
3. be linked from `docs/specs/conductor/README.md`.

**A correction that cannot quote the spec text is not an erratum, and is refused.**

Editing the spec in place is **refused permanently**. It destroys the received artifact's
integrity and makes "the spec is authoritative" unfalsifiable — anyone could later "correct" it
into agreement with the implementation.

## This instance is the proof of why the rule has that shape

The ACP spike reported the spec's adapter package names as "stale", naming the `@zed-industries`
packages. Applying the quote-the-line requirement showed **the spec names no package at all**:

- Case-insensitive search for `zed-industries`, `@zed`, `claude-code-acp` across the spec HTML →
  **0 hits** (verified independently).
- §4.1 says only *"ACP via Claude Code adapter (wraps official Agent SDK)"* and *"ACP via Codex
  adapter"*.
- §4.1's preamble states engines are **"data, not code paths"**; §13's risk table already says
  **"Pin adapter versions per workspace."**

So there is **no erratum**. The spec is *silent*, not stale, and the package identity is a
**catalog fact to be pinned**, exactly where the spec says such things live.

The false claim was in the spike's own README and has been **corrected in place with the
correction recorded**, not silently edited away.

## Pinned catalog values

| Engine | Package | Version at pin |
| --- | --- | --- |
| `claude-code` | `@agentclientprotocol/claude-agent-acp` | 0.75.1 (2026-09-09) |
| `codex` | `@agentclientprotocol/codex-acp` | 1.10.0 (2026-09-09) |
| `copilot` | ACP native — `copilot --acp` | preview since 2026-01-28 |

The superseded `@zed-industries/claude-code-acp` last published 2026-03-26 at v0.16.2, pinning ACP
SDK 0.14.x — reaching for that name from memory silently yields a March build.

## Scope effect

**Freezes** the spec HTML. **Refuses** in-place editing, permanently. **Admits** the pinned catalog
values above as engine-catalog data plus this note. **Establishes** the quote-the-line test for
every future correction in this programme.
