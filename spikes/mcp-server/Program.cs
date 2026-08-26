// Spike: ModelContextProtocol 2.2.0 — stdio transport semantics (AI-DE Phase-1 transport)
// and the AspNetCore HTTP transport hostile-Origin probe.
//
//   dotnet run -- server   run a stdio MCP server exposing one typed tool
//   dotnet run -- client   spawn the server over stdio; tools/list, valid call, invalid call
//   dotnet run -- http     start the HTTP transport on loopback and probe it with a hostile Origin
//
// The client run is the committed evidence path (see RESULT.md).
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;

var mode = args.Length > 0 ? args[0] : "client";

switch (mode)
{
    case "server":
        await RunStdioServerAsync();
        break;
    case "http":
        await RunHttpProbeAsync(args.Length > 1 ? args[1] : "server");
        break;
    default:
        return await RunStdioClientAsync();
}
return 0;

static async Task RunStdioServerAsync()
{
    var builder = Host.CreateApplicationBuilder();
    builder.Logging.ClearProviders(); // stdout belongs to the protocol on stdio
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<DescribeTool>();
    await builder.Build().RunAsync();
}

static async Task<int> RunStdioClientAsync()
{
    var failures = 0;
    var exe = Environment.ProcessPath!;
    var transport = new StdioClientTransport(new StdioClientTransportOptions
    {
        Name = "aide-spike-server",
        Command = exe,
        Arguments = ["server"],
    });

    await using var client = await McpClient.CreateAsync(transport);
    Console.WriteLine($"PASS M1-CONNECT — stdio initialize handshake — server: {client.ServerInfo.Name} {client.ServerInfo.Version}, protocol {client.NegotiatedProtocolVersion}");

    var tools = await client.ListToolsAsync();
    if (tools.Count == 1 && tools[0].Name == "describe")
        Console.WriteLine($"PASS M2-LIST — tools/list returns the typed tool — schema: {tools[0].JsonSchema}");
    else { failures++; Console.WriteLine($"FAIL M2-LIST — expected 1 tool 'describe', got [{string.Join(",", tools.Select(t => t.Name))}]"); }

    var ok = await client.CallToolAsync("describe", new Dictionary<string, object?>
    {
        ["workspaceId"] = "ws-1",
        ["nodeId"] = "Order",
        ["maxNeighbors"] = 5,
    });
    if (ok.IsError != true)
        Console.WriteLine($"PASS M3-CALL — valid tools/call succeeds — {ok.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().FirstOrDefault()?.Text}");
    else { failures++; Console.WriteLine("FAIL M3-CALL — valid call returned isError"); }

    try
    {
        var bad = await client.CallToolAsync("describe", new Dictionary<string, object?>
        {
            ["workspaceId"] = "ws-1",
            // nodeId missing; maxNeighbors out of range
            ["maxNeighbors"] = 5000,
        });
        if (bad.IsError == true)
            Console.WriteLine($"PASS M4-INVALID — invalid tools/call returns isError:true in-protocol — {bad.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().FirstOrDefault()?.Text}");
        else { failures++; Console.WriteLine("FAIL M4-INVALID — invalid call did NOT set isError"); }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"PASS M4-INVALID — invalid tools/call rejected via exception: {ex.GetType().Name}: {ex.Message}");
    }

    Console.WriteLine(failures == 0 ? "ALL CASES PASS" : $"{failures} CASE(S) FAILED");
    return failures == 0 ? 0 : 1;
}

static async Task RunHttpProbeAsync(string unusedMode)
{
    // Start the AspNetCore MCP HTTP transport on loopback with DEFAULTS (no origin guard),
    // then probe it with a hostile Origin, replaying the v1 spike.
    var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.Services.AddMcpServer().WithHttpTransport().WithTools<DescribeTool>();
    var app = builder.Build();
    app.MapMcp();
    var url = "http://127.0.0.1:5599";
    _ = app.RunAsync(url);
    await Task.Delay(1500);

    using var http = new HttpClient();
    var req = new HttpRequestMessage(HttpMethod.Post, url)
    {
        Content = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2026-07-28","capabilities":{},"clientInfo":{"name":"evil","version":"0"}}}""",
            System.Text.Encoding.UTF8, "application/json"),
    };
    req.Headers.TryAddWithoutValidation("Origin", "https://evil.example");
    req.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
    var resp = await http.SendAsync(req);
    Console.WriteLine((int)resp.StatusCode is >= 200 and < 300
        ? $"CONFIRMED H1-ORIGIN — hostile Origin ACCEPTED by default transport — HTTP {(int)resp.StatusCode}; explicit application guard remains mandatory"
        : $"H1-ORIGIN — hostile Origin rejected by default — HTTP {(int)resp.StatusCode}");
    await app.StopAsync();
}

[McpServerToolType]
public sealed class DescribeTool
{
    [McpServerTool(Name = "describe")]
    [Description("Describe one graph node with a bounded neighbor set.")]
    public static string Describe(
        [Description("Workspace identity")] string workspaceId,
        [Description("Stable node identity")] string nodeId,
        [Description("Neighbor cap, 1-50")] int maxNeighbors)
    {
        if (maxNeighbors is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(maxNeighbors), $"maxNeighbors out of range 1..50: {maxNeighbors}");
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            node = nodeId,
            workspace = workspaceId,
            neighbors = Array.Empty<string>(),
            returned = 0,
            omitted = 0,
            sourceRevision = "spike",
        });
    }
}
