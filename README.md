# AI-DE

AI-DE is a Windows desktop starter built with C# and WPF on .NET 10. The repository includes a small MVVM seam, unit tests, CI, and the AI-Forward Pack for agent-assisted engineering workflows.

## Prerequisites

- Windows 10 or later
- .NET 10 SDK (the exact feature band is pinned in [`global.json`](global.json))

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
- `docs/architecture.md` - how the app is put together
- `.github/` - GitHub Copilot configuration and CI workflows
- `.claude/` - Claude Code knowledge, skills, and agents

See `docs/ai-forward-pack/OVERVIEW.md` for the available AI-Forward workflows.

## License

AI-DE application code is licensed under the [MIT License](LICENSE).

The installed AI-Forward Pack material remains under the Apache License 2.0.
See [Third-Party Notices](THIRD-PARTY-NOTICES.md).
