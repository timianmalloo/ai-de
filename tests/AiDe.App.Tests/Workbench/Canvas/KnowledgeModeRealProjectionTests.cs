using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using AiDe.App.Workbench;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation;

namespace AiDe.App.Tests.Canvas;

/// <summary>
/// Runs only when <see cref="RootVariable"/> names a repository root: the real daemon indexes the whole
/// repository, which takes minutes, so it is never part of the default suite. Reported as skipped, visibly,
/// when not configured (the same gate shape as <c>RealLaneFactAttribute</c>).
/// </summary>
internal sealed class RealProjectionFactAttribute : FactAttribute
{
    public const string RootVariable = "AIDE_REAL_PROJECTION_ROOT";

    public RealProjectionFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RootVariable)))
        {
            Skip = $"the real daemon projection over a repository: set {RootVariable} to the repository root";
        }
    }
}

/// <summary>
/// Design WL-S §8's end-to-end check (Ruling 166 (4)): in the running app, over the real daemon projection of
/// this repository, Understand's docked canvas in its knowledge-inclusive mode shows a known
/// <c>docs/knowledge/</c> node with its typed edges, and switching the mode back hides it.
/// </summary>
/// <remarks>
/// <para><b>Nothing is mirrored.</b> The daemon is the built <c>AiDe.Daemon.exe</c>, the client is the
/// <see cref="WorkspaceClient"/> the app uses, the shell is a real <see cref="WorkbenchShell"/> attached as
/// <c>MainWindow</c> attaches it, and the canvas is the one Understand's default layout docks. The test only
/// records what the shell's own graph source returned, so it can say which edges belong to the node.</para>
///
/// <para><b>The known node.</b> <see cref="KnownNode"/> is <c>docs/knowledge/index.md</c>: the knowledge corpus's
/// own index, which every knowledge base links to, so it is the most connected knowledge node and stays inside
/// the overview's cap. Its id is set in its frontmatter, not derived from its path.</para>
/// </remarks>
public sealed class KnowledgeModeRealProjectionTests
{
    private const string KnownNode = "knowledge-hub";

    private static string LocateDaemon()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AiDe.Daemon", "bin"));

        // The configuration these tests were built in, so the daemon is from this commit (DC-023).
        var configuration = AppContext.BaseDirectory.Contains(
            Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";

        var candidate = Path.Combine(root, configuration, "net10.0-windows", "AiDe.Daemon.exe");
        Assert.True(File.Exists(candidate), "the daemon was not built. Expected it at: " + candidate);
        return candidate;
    }

    private static async Task<(Process Process, string PipeName)> StartDaemonAsync(string workspace, string data)
    {
        var start = new ProcessStartInfo(LocateDaemon())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(workspace);
        start.ArgumentList.Add("--data");
        start.ArgumentList.Add(data);
        start.ArgumentList.Add("--startup-seconds");
        start.ArgumentList.Add("120");

        var process = Process.Start(start);
        Assert.NotNull(process);

        var announcement = await process!.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(60));
        Assert.NotNull(announcement);
        Assert.StartsWith("listening ", announcement, StringComparison.Ordinal);
        return (process, announcement!["listening ".Length..].Trim());
    }

    private static async Task WaitFor(Func<Task<bool>> condition, string what, int seconds = 120, Func<string>? detail = null)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return;
            await Task.Delay(100);
        }

        Assert.Fail("timed out waiting for " + what + (detail is null ? string.Empty : ": " + detail()));
    }

    // EvaluateAsync returns its failure as a string, so a page that never loaded would read as an answer.
    private static async Task<string> Eval(CanvasSurface canvas, string script)
    {
        var result = await canvas.EvaluateAsync(script);
        Assert.False(result.StartsWith("(evaluate failed", StringComparison.Ordinal), "the page could not be asked: " + result);
        return result;
    }

    private static async Task<bool> Drawn(CanvasSurface canvas, string id) =>
        await Eval(canvas, $"document.querySelector('.node[data-id=\"{id}\"]') !== null") == "true";

    private static string Describe(CanvasGraph? graph) =>
        graph is null
            ? "no graph was loaded"
            : $"{graph.Nodes.Count} node(s), {graph.Nodes.Count(n => n.IsKnowledge)} knowledge; first knowledge ids: "
              + string.Join(", ", graph.Nodes.Where(n => n.IsKnowledge).Take(8).Select(n => n.Id));

    [RealProjectionFact]
    public async Task Understand_TheDockedCanvasInKnowledgeInclusiveMode_ShowsAKnownKnowledgeNodeWithItsTypedEdges_AndOffHidesIt()
    {
        var workspace = Path.GetFullPath(Environment.GetEnvironmentVariable(RealProjectionFactAttribute.RootVariable)!);
        Assert.True(File.Exists(Path.Combine(workspace, "docs", "knowledge", "index.md")), "the known node's file is missing");

        var data = Directory.CreateTempSubdirectory("aide-real-projection-").FullName;
        var (daemon, pipeName) = await StartDaemonAsync(workspace, data);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipeName, TimeSpan.FromSeconds(30), CancellationToken.None);

            // What the app's "Index C# projects in this workspace" command runs.
            await client.IndexSolutionAsync("real-projection",CancellationToken.None);

            WorkbenchShell? shell = null;
            CanvasSurface? canvas = null;
            CanvasGraph? last = null;

            Sta.Pump<FrameworkElement>(
                create: () =>
                {
                    shell = new WorkbenchShell(client);
                    shell.AttachWorkspace(client, dataDirectory: null, commands: client, workspaceRoot: workspace);

                    var docked = shell.Understand.Service.Zones.AllSurfaces().First(s => s.Kind == "canvas");
                    canvas = shell.Understand.Adapter.SurfaceContent<CanvasSurface>(docked.SurfaceId);
                    Assert.NotNull(canvas);

                    // Record what the shell's own source returned; the source itself is unchanged.
                    var real = canvas!.GraphSource!;
                    canvas.GraphSource = async (rootId, ct) => last = await real(rootId, ct);
                    return shell.Understand.Root;
                },
                body: async (_, _) =>
                {
                    try
                    {
                        await WaitFor(() => Task.FromResult(canvas!.Ready), "the canvas page");
                        await WaitFor(async () => await Eval(canvas!, "document.querySelectorAll('.node').length") != "0",
                            "the code-only default to draw");
                        Assert.False(await Drawn(canvas!, KnownNode), "the code-only default drew the knowledge node");
                        Assert.DoesNotContain(last!.Nodes, n => n.IsKnowledge);

                        await Eval(canvas!, "document.getElementById('knowledge').click()");
                        await WaitFor(() => Task.FromResult(canvas!.KnowledgeInclusive), "the mode to turn on");
                        await WaitFor(() => Drawn(canvas!, KnownNode), "the knowledge node to draw in the mode", 60, () => Describe(last));

                        var node = Assert.Single(last!.Nodes, n => n.Id == KnownNode);
                        Assert.True(node.IsKnowledge, "the known node arrived as source, not knowledge");

                        // Its typed edges: each one touching the node carries a predicate, and the page drew it.
                        var edges = last.Edges.Where(e => e.From == KnownNode || e.To == KnownNode).ToList();
                        Assert.True(edges.Count > 0, "the known node was drawn with no edges");
                        Assert.All(edges, e => Assert.False(string.IsNullOrWhiteSpace(e.Predicate), "an untyped edge"));

                        // The script returns a JSON array as a string, and EvaluateAsync JSON-encodes that string.
                        var encoded = await Eval(canvas!,
                            "JSON.stringify(Array.from(document.querySelectorAll('line > title')).map(function(t){return t.textContent;}))");
                        var titles = JsonSerializer.Deserialize<string[]>(JsonSerializer.Deserialize<string>(encoded)!)!;
                        foreach (var predicate in edges.Select(e => e.Predicate).Distinct(StringComparer.Ordinal))
                        {
                            Assert.True(titles.Any(t => t.StartsWith(predicate + " (", StringComparison.Ordinal)),
                                $"the page drew no '{predicate}' edge");
                        }

                        foreach (var other in edges.Select(e => e.From == KnownNode ? e.To : e.From).Distinct(StringComparer.Ordinal))
                        {
                            Assert.True(await Drawn(canvas!, other), "the node's neighbour was not drawn: " + other);
                        }

                        Console.WriteLine($"known node {KnownNode}: {edges.Count} edge(s): "
                            + string.Join("; ", edges.Select(e => $"{e.From} -{e.Predicate}-> {e.To} [{e.Status}]")));

                        await Eval(canvas!, "document.getElementById('knowledge').click()");
                        await WaitFor(async () => !await Drawn(canvas!, KnownNode), "the knowledge node to hide again", 60);
                        Assert.False(canvas!.KnowledgeInclusive);
                        Assert.DoesNotContain(last!.Nodes, n => n.IsKnowledge);
                        Assert.Equal("1", await Eval(canvas!, "document.querySelectorAll('#stage').length"));
                    }
                    finally
                    {
                        shell?.Dispose();
                    }
                },
                timeoutSeconds: 600);
        }
        finally
        {
            try
            {
                if (!daemon.HasExited) daemon.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            daemon.Dispose();
            try
            {
                Directory.Delete(data, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
