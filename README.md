---
id: project-readme
title: "AI-DE Repository Guide"
type: doc
status: in-review
owner: "@timianmalloo"
phase: ""
tags: [readme, onboarding, build]
links:
  - { to: project-documents, rel: relates-to }
  - { to: architecture, rel: documents }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Build, run, repository-layout, and licensing guide for the .NET 10 Windows WPF starter.
---

# AI-DE

AI-DE is a Windows desktop starter built with C# and WPF on .NET 10. The repository includes a small MVVM seam, unit tests, CI, and the AI-Forward Pack for agent-assisted engineering workflows.

## Prerequisites

- Windows 10 or later
- .NET SDK 10.0.303, or a later .NET 10 feature band permitted by
  [`global.json`](global.json)

The Windows 10 floor is the intended support statement. It is not yet encoded
as a minimum platform version in the project file.

## Build locally

```powershell
dotnet restore .\AiDe.sln
dotnet build .\AiDe.sln --configuration Release --no-restore
dotnet test .\AiDe.sln --configuration Release --no-build
dotnet run --project .\src\AiDe.App\AiDe.App.csproj
```

## Repository layout

- `src/AiDe.App/` - WPF application
- `tests/AiDe.App.Tests/` - xUnit tests
- `docs/` - project and AI-Forward documentation
- `.github/` - GitHub Copilot configuration and CI workflows
- `.claude/` - Claude Code knowledge, skills, and agents

See `docs/ai-forward-pack/OVERVIEW.md` after installation for the available AI-Forward workflows.

## License

AI-DE application code is licensed under the [MIT License](LICENSE).

The installed AI-Forward Pack material remains under the Apache License 2.0.
See [Third-Party Notices](THIRD-PARTY-NOTICES.md).
