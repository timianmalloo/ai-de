using System.Text.Json;
using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// Contributing AI-DE's server to <c>.mcp.json</c> without taking the file over.
/// </summary>
/// <remarks>
/// <para>The product may write here — ensuring the enlightened experience is a legitimate reason —
/// but it is <b>not AI-DE's file</b>: a user or another tool may have servers in it. So the rule is
/// create-when-absent and merge-when-present, and the tests that matter are the ones asserting what
/// is left alone.</para>
/// </remarks>
public sealed class McpConfigWriterTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-mcpcfg-" + Guid.NewGuid().ToString("n")[..8]);

    private readonly string _server;

    public McpConfigWriterTests()
    {
        Directory.CreateDirectory(_root);
        _server = Path.Combine(_root, "AiDe.Mcp.exe");
        File.WriteAllText(_server, "not really an executable, but it exists");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
    }

    private string ConfigPath => Path.Combine(_root, McpConfigWriter.FileName);

    private JsonElement Config() =>
        JsonDocument.Parse(File.ReadAllText(ConfigPath)).RootElement.Clone();

    [Fact]
    public void WithNoFile_ItCreatesOne()
    {
        var result = McpConfigWriter.Ensure(_root, _server);

        Assert.Equal(McpConfigOutcome.Created, result.Outcome);
        Assert.Equal(_server, Config().GetProperty("mcpServers").GetProperty("aide").GetProperty("command").GetString());
    }

    /// <summary>
    /// Another tool's servers survive the merge untouched.
    /// </summary>
    /// <remarks>
    /// The property the whole design turns on. A template write would be simpler and would silently
    /// delete somebody's configuration to add a convenience — the kind of help nobody asks for twice.
    /// </remarks>
    [Fact]
    public void ItMergesBesideSomeoneElsesServers()
    {
        File.WriteAllText(ConfigPath, """
            {
              "mcpServers": {
                "sentry": { "command": "npx", "args": ["sentry-mcp"] }
              }
            }
            """);

        var result = McpConfigWriter.Ensure(_root, _server);

        Assert.Equal(McpConfigOutcome.Merged, result.Outcome);
        var servers = Config().GetProperty("mcpServers");
        Assert.Equal("npx", servers.GetProperty("sentry").GetProperty("command").GetString());
        Assert.Equal(_server, servers.GetProperty("aide").GetProperty("command").GetString());
    }

    /// <summary>Unrelated top-level keys survive too — including ones this version never heard of.</summary>
    [Fact]
    public void ItPreservesKeysItDoesNotUnderstand()
    {
        File.WriteAllText(ConfigPath, """
            {
              "someFutureKey": { "nested": [1, 2, 3] },
              "mcpServers": {}
            }
            """);

        McpConfigWriter.Ensure(_root, _server);

        Assert.Equal(3, Config().GetProperty("someFutureKey").GetProperty("nested").GetArrayLength());
    }

    /// <summary>
    /// An unparseable file is LEFT ALONE, byte for byte.
    /// </summary>
    /// <remarks>
    /// A file that fails to parse is far likelier to be mid-edit, or written by a tool this version
    /// does not understand, than to be corrupt. Rewriting it — even "helpfully", even with a backup —
    /// destroys work to add a convenience. The refusal is reported so it can be fixed, which is the
    /// only honest thing to do with a file we will not touch.
    /// </remarks>
    [Fact]
    public void AnUnparseableFileIsNotTouched()
    {
        const string mangled = "{ this is not json, someone was mid-edit";
        File.WriteAllText(ConfigPath, mangled);

        var result = McpConfigWriter.Ensure(_root, _server);

        Assert.Equal(McpConfigOutcome.RefusedUnparseable, result.Outcome);
        Assert.Equal(mangled, File.ReadAllText(ConfigPath));
        Assert.Contains("left untouched", result.Reason);
    }

    /// <summary>And a `mcpServers` that is not an object is refused for the same reason.</summary>
    /// <remarks>
    /// Replacing it would discard whatever is there. The shape is wrong for a merge, so there is no
    /// merge — not a merge that throws the obstacle away.
    /// </remarks>
    [Fact]
    public void AServersKeyOfTheWrongShapeIsRefusedRatherThanReplaced()
    {
        const string odd = """{"mcpServers": "somebody put a string here"}""";
        File.WriteAllText(ConfigPath, odd);

        var result = McpConfigWriter.Ensure(_root, _server);

        Assert.Equal(McpConfigOutcome.RefusedUnparseable, result.Outcome);
        Assert.Equal(odd, File.ReadAllText(ConfigPath));
    }

    /// <summary>A second call changes nothing and says so.</summary>
    /// <remarks>
    /// This runs on every terminal launch, so a write each time would churn the file, touch its
    /// mtime, and make every launch look like a configuration change to anything watching it.
    /// </remarks>
    [Fact]
    public void ASecondCallIsUnchanged()
    {
        McpConfigWriter.Ensure(_root, _server);
        var before = File.GetLastWriteTimeUtc(ConfigPath);

        var result = McpConfigWriter.Ensure(_root, _server);

        Assert.Equal(McpConfigOutcome.Unchanged, result.Outcome);
        Assert.Equal(before, File.GetLastWriteTimeUtc(ConfigPath));
    }

    /// <summary>A stale entry is refreshed, so moving the install fixes itself.</summary>
    [Fact]
    public void AnEntryPointingSomewhereElseIsCorrected()
    {
        File.WriteAllText(ConfigPath, """{"mcpServers":{"aide":{"command":"C:/old/AiDe.Mcp.exe"}}}""");

        var result = McpConfigWriter.Ensure(_root, _server);

        Assert.Equal(McpConfigOutcome.Merged, result.Outcome);
        Assert.Equal(_server, Config().GetProperty("mcpServers").GetProperty("aide").GetProperty("command").GetString());
    }

    /// <summary>
    /// A server binary that is not there writes NOTHING and says why.
    /// </summary>
    /// <remarks>
    /// Naming a path that does not exist would leave the agent with a server that cannot start and
    /// no reason given — worse than no entry at all, because it looks configured. This is the shape
    /// the published-layout gate exists for, one layer out.
    /// </remarks>
    [Fact]
    public void AMissingServerBinaryWritesNothing()
    {
        var result = McpConfigWriter.Ensure(_root, Path.Combine(_root, "not-there.exe"));

        Assert.Equal(McpConfigOutcome.Failed, result.Outcome);
        Assert.False(File.Exists(ConfigPath));
        Assert.Contains("not found", result.Reason);
    }

    /// <summary>No workspace writes nothing, and says that rather than throwing.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoWorkspaceWritesNothing(string? root)
    {
        var result = McpConfigWriter.Ensure(root, _server);

        Assert.Equal(McpConfigOutcome.Failed, result.Outcome);
        Assert.NotNull(result.Reason);
    }

    /// <summary>
    /// No env block is written.
    /// </summary>
    /// <remarks>
    /// The server inherits <c>AIDE_SESSION</c> from the terminal that launched the harness — verified
    /// 2026-09-04, <c>spikes/mcp-stdio-environment</c>. An env block here would be per-WORKSPACE, so
    /// every agent in it would share one identity and their board posts would be mutually
    /// misattributed. Asserted because it is the kind of thing a later "improvement" adds.
    /// </remarks>
    [Fact]
    public void NoEnvBlockIsWritten_SoIdentityStaysPerSession()
    {
        McpConfigWriter.Ensure(_root, _server);

        var entry = Config().GetProperty("mcpServers").GetProperty("aide");
        Assert.False(entry.TryGetProperty("env", out _));
    }

    /// <summary>No temp file survives a write.</summary>
    [Fact]
    public void NoPartialFileIsLeftBehind()
    {
        McpConfigWriter.Ensure(_root, _server);

        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }
}
