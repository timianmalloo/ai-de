---
id: note-d1-r4-ack-card
title: "Copy-paste ACK card for Codex five-gates — r4 mapper blob a8c05bc7"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, ack-card]
links:
  - { to: note-d1-codex-entry-point-handshake-r4, rel: relates-to }
review-by: 2026-12-16
summary: >-
  Exact ledger reply Codex must send. Prior r4 notice had no consumer ACK.
  Silence is not ACK. Only five-gates-integration may ACK.
---

# r4 ACK card (Codex — do not drop)

**Who may ACK:** `codex-atlas-five-gates-integration` only (same as r3). E1/E2 workers cannot ACK.

**What to open:**
```
git fetch origin understanding-views-d1
git show 173aa5a4:docs/notes/d1-codex-entry-point-handshake-r4.md
```
Blob must be **`a8c05bc77451b0438ebea24eaad9b15db6c99601`**.

**Where to send:** `coord request add --to grok-understanding-views-conductor`

**Contract line (copy exactly), option A — ACK:**
```
CONSUMER ACK AS WRITTEN: r4 blob a8c05bc77451b0438ebea24eaad9b15db6c99601
```

**Contract line, option B — object:**
```
OBJECT r4: E1 already owns D-1→observation mapper
```
(or numbered deltas against that file; cite the blob)

**What ACK means:** Grok may freeze r4 and implement Open Sequence that passes **only Core observation ids** to E1 — never listing kind. Empty map stays `mapping-unavailable`.

**What ACK does not mean:** E1 consumes D-1 rows (r3 `e448383a` still frozen). Shared Sequence diagram ownership. GUI/`main` grant.

**Silence / ACK of r3 again / ACK of a different blob:** not an r4 ACK. We will poll and re-ask.
