---
id: api-aide-app-workbench-understanding
title: "API: AiDe.App.Workbench.Understanding"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench.Understanding: 3 types, 44 members, 2% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Understanding`

**3 public types · 44 public members · 2% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `AtlasReaderView`

*class* — `AtlasReaderView.cs`

Native, read-only consumer for Atlas projections. The proof constructor retains
`IAtlasQueries`; the production constructor consumes a Core-owned reader lease.

| Member | Summary |
|---|---|
| `int PageSize = 64` | **(gap)** |
| `AtlasReaderView(IAtlasQueries queries, string manifestToken)` | **(gap)** |
| `AtlasReaderView(IAtlasReaderLease lease) : this(lease, null) { }` | **(gap)** |
| `IReadOnlyList<AtlasFileNode> FileRoots` | **(gap)** |
| `IReadOnlyList<OutlineRow> OutlineRows` | **(gap)** |
| `IReadOnlyList<AtlasTextSpan> CurrentHighlights { get; private set; } = []` | **(gap)** |
| `string StatusText` | **(gap)** |
| `string BoundsText` | **(gap)** |
| `string SourceStatusText` | **(gap)** |
| `string SourceText` | **(gap)** |
| `bool IsSourceReadOnly` | **(gap)** |
| `bool CanGoBack` | **(gap)** |
| `bool CanLoadMore` | **(gap)** |
| `Button BackButton` | **(gap)** |
| `Button LoadMoreButton` | **(gap)** |
| `TreeView FilesControl` | **(gap)** |
| `ListBox OutlineControl` | **(gap)** |
| `TextEditor SourceControl` | **(gap)** |
| `TextBlock StatusControl` | **(gap)** |
| `Task LoadAsync(CancellationToken cancellationToken = default)` | **(gap)** |
| `Task LoadMoreAsync(CancellationToken cancellationToken = default)` | **(gap)** |
| `Task SelectFileAsync(AtlasFileNode file, CancellationToken cancellationToken = default)` | **(gap)** |
| `Task SelectDeclarationAsync(OutlineRow declaration, CancellationToken cancellationToken = default)` | **(gap)** |
| `Task GoBackAsync(CancellationToken cancellationToken = default)` | **(gap)** |

## `AtlasFileNode`

*class* — `AtlasReaderView.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `string Name { get; }` | **(gap)** |
| `string RelativePath { get; }` | **(gap)** |
| `string? FileValue { get; }` | **(gap)** |
| `bool IsFile { get; }` | **(gap)** |
| `string Details { get; }` | **(gap)** |
| `string? ParentToken { get; private init; }` | **(gap)** |
| `string? EntryToken { get; private init; }` | **(gap)** |
| `ObservableCollection<AtlasFileNode> Children { get; } = []` | **(gap)** |
| `string AccessibleName` | **(gap)** |
| `string ToString()` | **(gap)** |
| `AtlasFileNode Folder(string name, string relativePath)` | **(gap)** |
| `AtlasFileNode File(string name, AtlasFileEntry entry)` | **(gap)** |

## `OutlineRow`

*class* — `AtlasReaderView.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `OutlineRow(string fileValue, OutlineDeclaration declaration)` | **(gap)** |
| `string FileValue { get; }` | **(gap)** |
| `string ObservationKey { get; }` | **(gap)** |
| `string DisplayName { get; }` | **(gap)** |
| `AtlasDeclarationKind Kind { get; }` | **(gap)** |
| `AtlasTextSpan Span { get; }` | **(gap)** |
| `string AccessibleName` | **(gap)** |
| `string ToString()` | **(gap)** |
