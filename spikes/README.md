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
