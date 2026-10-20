---
id: api-aide-app-workbench-checklist
title: "API: AiDe.App.Workbench.Checklist"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench.Checklist: 2 types, 37 members, 41% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Checklist`

**2 public types · 37 public members · 41% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `ChecklistCopy`

*class* — `ChecklistCopy.cs`

The checklist's load-bearing copy (the WL-1b design, "UI and interaction design"; `DESIGN.md` rows CHK1–CHK12).
One home for every sentence the surface shows, so a test reads the same const the surface renders.

**Remarks.** The gap sentences ("Blocked — …") are the Core fold's (`AcceptanceFold`) and are shown as the fold wrote them,
never re-worded here: two definitions of one sentence is the shape DM7 names.

| Member | Summary |
|---|---|
| `string Empty = "No obligations yet. Add the conditions this work must meet."` | **(gap)** |
| `string EvidenceMissing = "Evidence missing."` | **(gap)** |
| `string Acknowledged = "Acknowledged — not satisfied. Acknowledging is not evidence."` | **(gap)** |
| `string Deferred = "Deferred — acceptance stays blocked."` | **(gap)** |
| `string NeedsDecision = "Needs a decision."` | **(gap)** |
| `string NoDisposition = "Not reviewed."` | **(gap)** |
| `string Stale = "The evidence belongs to an earlier draft. Link evidence from the current draft."` | **(gap)** |
| `string NotRecorded = "Evidence currentness is not recorded, so this does not count as current."` | **(gap)** |
| `string Ready = "Ready to accept."` | **(gap)** |
| `string NotAWorkJourney = "This notebook has no work journey. It was made before work journeys existed. Obligations need a work journey."` | **(gap)** |
| `string NoNotebook = "No notebook is focused. Open a notebook in Factory, then open its checklist."` | The opener's refusal with no notebook to take the journey from, and the pane of an id that names no journey. |
| `string NotOpen(string journeyId)` | A checklist whose own journey's notebook is not open: it names the journey and binds nothing else (seam req-01M5B8W7NJRW7A1X4G21J2QKPT). |
| `string NotConnected = "The workspace service is not connected, so obligations cannot be read or recorded."` | **(gap)** |
| `string Loading = "Loading the checklist…"` | **(gap)** |
| `string Mandatory = "Mandatory"` | **(gap)** |
| `string AcceptDeliverable = "Accept deliverable"` | **(gap)** |
| `string AddObligation = "Add obligation"` | **(gap)** |
| `string NewObligationName = "New obligation"` | **(gap)** |
| `string Acknowledge = "Acknowledge"` | **(gap)** |
| `string LinkLatestRun = "Link the latest run"` | **(gap)** |
| `string Defer = "Defer"` | **(gap)** |
| `string MarkNeedsDecision = "Needs decision"` | **(gap)** |
| `string RefusedLead = "Not accepted."` | The acceptance refusal's lead; the fold's gap sentences follow it (read out in the polite live region). |
| `string AcceptedNegation = "This records acceptance of the deliverable only. It does not say the work is implemented or merged."` | The one sentence that may say "implemented" or "merged", and only to negate it (Ruling 179). |
| `string AcceptedEarlier = "Accepted at an earlier intent revision. The intent has changed since, so this acceptance does not cover the current revision."` | **(gap)** |
| `string Satisfied(string dispatchKey, int intentRevision)` | "Satisfied by run {7-char key} (intent revision {n})." |
| `string Accepted(int intentRevision)` | The Accepted line: it names the intent revision by number and claims nothing about implementation (Ruling 179). |
| `string AcceptIntent(int intentRevision)` | "Accept intent (revision N)". |
| `string IntentAccepted(int intentRevision)` | **(gap)** |

## `ChecklistSurface`

*class* — `ChecklistSurface.cs`

The minimal `checklist` kind (the WL-1b design, "The minimal checklist kind"; AC-W03c): one row per obligation
with its evidence state and its human state as two separate labels, the acceptance line on top, and the two
closing actions. It holds no state the store does not: every refresh re-reads `checklist.evaluate`, so a
reopen or a restore re-evaluates.

**Remarks.** **A checklist is its journey (Ruling 176; seam `req-01M5B8W7NJRW7A1X4G21J2QKPT`).** The focused
notebook picks the journey only at open, and the surface id carries it: `checklist:<sessionId>`, the
session that names the journey's intent notebook (`ForSession`). The journey id
itself is minted by the store and read back through `notebook.open`, never derived here (DM7). A restored
checklist rebinds to its own notebook, never to whichever notebook is focused; while that notebook is not open the
pane says so and names the journey.




**The live projection sha is the attempt's own envelope (R1; Ruling 180 (5)).** It is
`CurrentProjectionSha` over the envelope the attempt was submitted from, recomputed at
each call as `SessionDocumentSurface.CurrentProjectionOf` does, and never the composer's current or next
envelope: the composer clears after a send, so its projection would make every link read stale. When that envelope
cannot be read the call carries null, which the fold reads as NotRecorded, never as current.




*Accept deliverable* stays enabled while blocked: the press calls `work.accept`, the store refuses,
and the gaps are read out in the polite live region (ADR-0040 `:113`), never a silent disabled button.

| Member | Summary |
|---|---|
| `string Kind = "checklist"` | The surface kind; the row is `Kinds`'s. |
| `ChecklistSurface(WorkspaceClient? client, string? sessionId, Func<SessionDocumentSurface?> notebook)` | **(gap)** |
| `string SurfaceIdFor(string sessionId)` | The checklist surface id of the journey whose intent notebook  names. |
| `string? SessionIdOf(string surfaceId)` | The session a checklist surface id names, or null for any other id. |
| `string CaptionFor(string notebookTitle)` | The caption, which is also the UIA name: *Checklist — <notebook>*. |
| `string AcceptanceLine` | The acceptance line: "Ready to accept", the first blocking gap, or the Accepted line. |
| `string Announcement` | What the polite live region last read out. |
| `Task RefreshAsync()` | Re-reads the checklist from the store. |

### `ChecklistSurface(WorkspaceClient? client, string? sessionId, Func<SessionDocumentSurface?> notebook)`

- **`client`** — The shell's workspace client, or null when no daemon is connected (the pane says so).
- **`sessionId`** — The session whose notebook is this checklist's journey (`SessionIdOf`), or null for an id that names none.
- **`notebook`** — Resolves that session's open notebook at every refresh, or null while it is not open.
