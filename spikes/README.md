# Spikes — committed, re-runnable contract evidence

Policy (supersedes the pack default of disposing spike scaffolding): any contract row
labelled **Verified** in `docs/architecture.md` must cite a spike in this directory that a
reviewer can re-run. Each spike folder contains the source, a one-command run line, and a
`RESULT.md` with the captured output and the date/toolchain of the run. A spike result is
evidence for the *stated cases only* — it is a floor, not a verdict.

| Spike | Contract it establishes | Re-run |
|---|---|---|
| `sqlite-fact-store/` | `Microsoft.Data.Sqlite` 10.0.11: WAL, constraint rejection, recursive CTE, immutability-trigger semantics incl. `INSERT OR REPLACE` bypass and `recursive_triggers`, `query_only`, no nested transactions. | `dotnet run --project spikes/sqlite-fact-store` |
| `conpty-foundation/` | `CreatePseudoConsole` availability and create/close lifecycle on the current Windows host. | `python spikes/conpty-foundation/conpty_spike.py` |
| `mcp-server/` | `ModelContextProtocol` 2.2.0 stdio server: typed tool registration, discovery, valid and invalid `tools/call`. HTTP transport hostile-Origin probe (AspNetCore). | `dotnet run --project spikes/mcp-server -- client` |
| `roslyn-msbuild-workspace/` | **Phase-2 S2.** `MSBuildWorkspace` 4.14.0 loading a real solution against a deliberately non-matching (older) SDK; and whether repository-authored analyzers and source generators **execute inside the extractor's own process** — proven by a fixture generator whose side effect is outside the compilation, so execution cannot be confused with mere reference. Establishes that MSBuild properties do **not** suppress execution, that stripping `AnalyzerReferences` does, and that the control costs exactly the generated symbols. | `dotnet run --project spikes/roslyn-msbuild-workspace` |
| `webview2-airspace/` | **Phase-2 S4.** WebView2 1.0.3485.44 inside an AvalonDock pane, both hosting modes driven by the same probes. Establishes that the default control has a real airspace limitation (a WPF overlay in the same Grid cell is not drawn), that `WebView2CompositionControl` fixes it but **kills the process with a native access violation when its pane is floated** and never repaints after a tab restore, and that `Focus()` is refused in both. | `dotnet run --project spikes/webview2-airspace` |
| `terminal-renderer/` | **Phase-2 S3.** WPF text-rendering throughput for a 200x50 terminal grid: `GlyphRun` per line at p95 6.64 ms (151 fps ceiling) versus `FormattedText` per cell at 142.80 ms (7 fps), and VT scanning at 2361x the 1 MiB/s budget. Establishes that owning a WPF renderer is viable and that the per-cell design is not. | `dotnet run --project spikes/terminal-renderer` |
