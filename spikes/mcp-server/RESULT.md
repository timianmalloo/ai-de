# Spike result — mcp-server

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303 · `ModelContextProtocol` / `ModelContextProtocol.AspNetCore` 2.2.0
- **Commands:** `dotnet run --project spikes/mcp-server -- client` and `-- http`
- **Exit (client):** 0 (ALL CASES PASS)

## Captured output — stdio client (the Phase-1 transport)

```
PASS M1-CONNECT — stdio initialize handshake — server: McpServerSpike 1.0.0.0, protocol 2026-07-28
PASS M2-LIST — tools/list returns the typed tool — schema: {"type":"object","properties":{"workspaceId":{...},"nodeId":{...},"maxNeighbors":{...}},"required":["workspaceId","nodeId","maxNeighbors"]}
PASS M3-CALL — valid tools/call succeeds — {"node":"Order","workspace":"ws-1","neighbors":[],"returned":0,"omitted":0,"sourceRevision":"spike"}
PASS M4-INVALID — invalid tools/call returns isError:true in-protocol — An error occurred invoking 'describe'.
ALL CASES PASS
```

## Captured output — HTTP hostile-Origin probe

```
CONFIRMED H1-ORIGIN — hostile Origin ACCEPTED by default transport — HTTP 200; explicit application guard remains mandatory
```

## Contract established (cases only)

1. The 2.2.0 SDK registers a typed tool from a `[McpServerTool]` method, negotiates
   protocol 2026-07-28 over **stdio**, lists the tool with a JSON Schema, runs a valid
   `tools/call`, and returns an in-protocol `isError` result for an invalid one (M1–M4).
   Phase-1 AI-DE uses this stdio/in-process transport.
2. The **AspNetCore HTTP transport accepts a hostile `Origin` with HTTP 200 by default**
   (H1). This reproduces the v1 finding: HTTP is not enabled in v1, and when a later phase
   enables it, an explicit Origin/Host allowlist guard is a code-level precondition with its
   own red-first negative test (threat model boundary 6).
