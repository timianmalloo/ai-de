---
id: architecture
title: "AI-DE Architecture"
type: architecture
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [architecture, wpf]
links:
  - { to: audit-log, rel: relates-to }
review-by: 2027-02-19
review-suggested: []
summary: >-
  How AI-DE is put together today: one .NET 10 WPF executable with a small MVVM
  seam, one xUnit test project, and two GitHub Actions workflows.
---

# AI-DE Architecture

This describes what exists today. It is deliberately short; extend it when the
app grows past a starter.

## Projects

| Project | What it is |
|---|---|
| `src/AiDe.App` | The application. `net10.0-windows`, `WinExe`, `UseWPF`. |
| `tests/AiDe.App.Tests` | xUnit tests. References the app project. |

There is no domain, service, persistence, networking, or dependency-injection
layer yet, and no runtime AI integration.

## Startup path

`App.xaml` sets `StartupUri="MainWindow.xaml"`. `MainWindow` constructs
`MainWindowViewModel` as its `DataContext` in XAML and binds four read-only
properties: `WindowTitle`, `Heading`, `GettingStartedSteps`, `StatusMessage`.

The view model is plain C# with no WPF dependency, so it is unit-testable
directly. It has no `INotifyPropertyChanged` or commands — add them when the UI
needs to change at runtime.

## Build and CI

- `global.json` pins the SDK feature band.
- `Directory.Build.props` sets latest language/analysis and
  `TreatWarningsAsErrors`.
- `.github/workflows/build.yml` — restore, build, test on `windows-latest`.
- `.github/workflows/docs-health.yml` — validates the docs knowledge graph on
  PRs that touch docs or Markdown.

```powershell
dotnet restore .\AiDe.sln
dotnet build .\AiDe.sln --configuration Release --no-restore
dotnet test .\AiDe.sln --configuration Release --no-build
```

## Known gaps

- WPF startup and XAML binding resolution are not covered by tests; only the
  view model is.
- Public types have no XML documentation comments.
- No packaging, signing, installer, or release pipeline.
