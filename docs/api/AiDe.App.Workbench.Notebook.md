---
id: api-aide-app-workbench-notebook
title: "API: AiDe.App.Workbench.Notebook"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench.Notebook: 1 types, 3 members, 100% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Notebook`

**1 public types · 3 public members · 100% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `NotebookSlotMigration`

*class* — `NotebookSlotMigration.cs`

The slot migration by stable id for a session's pane (ADR-0045 §7; the WL-S design's §4 and §7). A saved
`session-document:<session>` placement becomes the `notebook` of the same session: the same zone, the
same tab position and the same saved caption. Only the kind and the id prefix change, so the session's identity is kept.

**Remarks.** A placement the migration cannot map stays a `session-document`, which admission then drops with the
`RetiredKinds` sentence. That covers an id with no session, and a session whose
notebook the slot already holds (two tabs of one id would be one surface twice). A `prompt` placement is left as it
is (Ruling 173): the notebook does not yet offer the prompt draft's six capabilities, so the migration names it as kept and
the restore report says so. Floating windows are not walked here.
`LayoutPersistence`'s floating guard (Ruling 171) owns what a restored floating window becomes.

| Member | Summary |
|---|---|
| `string SessionDocumentKind = "session-document"` | The retired kind this migration maps (ADR-0045 §2). |
| `string PromptKind = "prompt"` | The kind this migration leaves as it is (Ruling 173): a prompt draft stays a prompt draft. |
| `(WorkbenchLayout Layout, IReadOnlyList<Surface> Migrated, IReadOnlyList<Surface> KeptPrompts) Migrate(WorkbenchLayout saved)` | with every mappable `session-document` placement in its zones (and in its maximize snapshot) replaced by its notebook, the notebooks it made, in zone order, and the prompt drafts it kept as they were. |
