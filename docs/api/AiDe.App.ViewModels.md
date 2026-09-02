---
id: api-aide-app-viewmodels
title: "API: AiDe.App.ViewModels"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.ViewModels: 1 types, 14 members, 60% carrying a summary doc comment.
---

# API: `AiDe.App.ViewModels`

**1 public types · 14 public members · 60% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `MainWindowViewModel`

*class* — `MainWindowViewModel.cs`

The Phase-1 workspace surface: an accessible evidence list bound to a provenance pane, over the
in-process authority core (ADR-0009).

**Remarks.** This view model is the reachability proof (E10): the walking skeleton is only walking if a user
can actually reach the evidence from the window they open. The list is not a fallback for the
Phase-2 canvas — it is the permanent keyboard and screen-reader equivalent.

| Member | Summary |
|---|---|
| `MainWindowViewModel()` | First-run / design-time construction: no workspace open yet. |
| `MainWindowViewModel(WorkspaceCore core)` | Opens over the in-process core (ADR-0009's first hosting mode). |
| `MainWindowViewModel(` | Opens over any read surface — in this process or a daemon's. |
| `event PropertyChangedEventHandler? PropertyChanged` | **(gap)** |
| `string WindowTitle` | **(gap)** |
| `string Heading` | **(gap)** |
| `ObservableCollection<EvidenceRow> Rows { get; } = []` | **(gap)** |
| `EvidenceRow? SelectedRow` | **(gap)** |
| `string StatusMessage` | The status strip. Always states evidence — which revision, what is stale, what failed. |
| `string ProvenanceText` | The provenance pane content, in the spec's fixed evidence order. |
| `IReadOnlyList<string> GettingStartedSteps { get; } =` | First-run guidance, shown before a workspace is opened. |
| `Task RefreshAsync(CancellationToken cancellationToken = default)` | **(gap)** |
| `Task<MainWindowViewModel> OpenDefaultAsync(CancellationToken cancellationToken = default)` | Opens the workspace the app was launched against, over its daemon. |
| `Task<MainWindowViewModel> OpenAsync(` | Opens the workspace rooted at , launching its daemon if needed. |

### `MainWindowViewModel(`

Opens over any read surface — in this process or a daemon's.

- **`incidents`** — Health incidents, when they are reachable. Null across the boundary: the incident sidecar is not part of the read surface that crosses it yet, and reporting "no incidents" when the question cannot be asked would be exactly the clean-empty-success this product exists to avoid — so the strip omits the clause instead of asserting a zero.

### `Task<MainWindowViewModel> OpenDefaultAsync(CancellationToken cancellationToken = default)`

Opens the workspace the app was launched against, over its daemon.

**Remarks.** **This is where the process split stops being a test and starts being the product.**
The shell asks `ShellBootstrap` for a daemon — reaching the one already serving
the workspace, or starting one — and every projection it renders is then answered across the
trust boundary.





**A daemon that will not start is shown, not worked around.** Falling back to the
in-process core would work, and would silently abandon the boundary, the workspace lock and
the epoch fence at the moment they were most obviously needed. The user gets the first-run
surface and a message saying what failed (**DC-011**: a silent degradation is
indistinguishable from a broken feature).





Absent a configured root the app shows its first-run state rather than inventing a
workspace.

### `Task<MainWindowViewModel> OpenAsync(`

Opens the workspace rooted at , launching its daemon if needed.

**Remarks.** Split out from `OpenDefaultAsync` so a workspace can be CHOSEN rather than only
inherited from an environment variable. Until this existed the daemon path was reachable
only by setting AIDE_WORKSPACE_ROOT before launch, which made every command that needs a
workspace — indexing especially — untestable by anyone who did not already know that.
