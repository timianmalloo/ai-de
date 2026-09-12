using System.Text.Json;
using System.Windows;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>INV-0009 Phase 4.</b> The binder writes what it did on the normal path: a
/// <c>session-document.bound</c> line naming the session, the composer's surface and the checkout a
/// run would be cut from, or a <c>session-document.refused</c> line naming the field and the reason
/// the composer shows. The refusal the operator read at 22:33:28Z reached the composer's status line
/// and nowhere else (INV-0009 §7); the next one is in the log.
/// </summary>
/// <remarks>
/// <b>Red first:</b> against the binder before its emitters, every case here found no line of
/// either kind (the bound and refused counts read 0).
/// </remarks>
public sealed class TheBinderRecordsWhatItBoundTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-binder-" + Guid.NewGuid().ToString("N"));

    public TheBinderRecordsWhatItBoundTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private const string OneReadyAccount = """
        {
          "adapterInstallRoot": "C:/adapters/from-the-file",
          "providers": {
            "anthropic": { "auth": "subscription", "accounts": [ { "label": "max-work", "health": "ready" } ] },
            "openai": { "auth": "subscription", "accounts": [ { "label": "chatgpt", "health": "ready" } ] }
          },
          "engines": { "claude-code": { "model": "model-from-the-file" }, "codex": { "model": "codex-model" } }
        }
        """;

    private ProviderConfiguration Providers()
    {
        var path = Path.Combine(_root, "providers.json");
        File.WriteAllText(path, OneReadyAccount);
        return ProviderConfiguration.Read(path);
    }

    private SessionConfig Create(IReadOnlyList<string> enabled) =>
        new SessionConfigStore(_root, SessionId.New(DateTimeOffset.UtcNow))
            .Create("Recorded", _root, enabled, DateTimeOffset.UtcNow);

    /// <summary>Runs a body against a real shell in a real shown window, capturing every diagnostic line it writes.</summary>
    private static (T Result, List<JsonElement> Lines) WithShell<T>(Func<WorkbenchShell, T> body)
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = line => { lock (lines) { lines.Add(line); } };

        try
        {
            var result = Sta.Run(() =>
            {
                var shell = new WorkbenchShell(queries: null);
                var window = new Window
                {
                    Content = shell.Manager,
                    Width = 1000,
                    Height = 640,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                };

                window.Show();
                window.UpdateLayout();

                try { return body(shell); }
                finally { window.Close(); }
            }, 60);

            lock (lines)
            {
                return (result, [.. lines.Select(l => JsonDocument.Parse(l).RootElement)]);
            }
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }
    }

    private static List<JsonElement> Of(IEnumerable<JsonElement> lines, string evt) =>
        [.. lines.Where(e => e.GetProperty("evt").GetString() == evt)];

    [Fact]
    public void Bind_WithAProviderFile_BindsAndRecordsTheRepositoryRoot()
    {
        var config = Create(["claude-code"]);
        var providers = Providers();

        var (said, lines) = WithShell(shell =>
        {
            shell.OpenSessionDocument(config);
            var sentence = SessionComposerBinder.Bind(
                shell, config, ["claude-code"], "feature",
                repositoryRoot: _root, dataDirectory: _root, providers, new NeverAffirms());
            return (sentence, shell.SessionComposer(config.SessionId)!.SurfaceId);
        });

        Assert.StartsWith("Composer bound to claude-code", said.sentence, StringComparison.Ordinal);

        var bound = Assert.Single(Of(lines, "session-document.bound"));
        Assert.Equal(config.SessionId, bound.GetProperty("session").GetString());
        Assert.Equal(said.SurfaceId, bound.GetProperty("surface").GetString());
        Assert.Equal(_root, bound.GetProperty("repositoryRoot").GetString());
        Assert.Empty(Of(lines, "session-document.refused"));
    }

    [Fact]
    public void Bind_WithNoWorkspaceOpen_RefusesRepositoryRootOnTheComposerAndInTheLog()
    {
        var config = Create(["claude-code"]);
        var providers = Providers();

        var (result, lines) = WithShell(shell =>
        {
            shell.OpenSessionDocument(config);
            var sentence = SessionComposerBinder.Bind(
                shell, config, ["claude-code"], "feature",
                repositoryRoot: null, dataDirectory: null, providers, new NeverAffirms());
            return (sentence, Status: shell.SessionComposer(config.SessionId)!.Status);
        });

        // On the composer, in the announcement, and in the log — the same field each time.
        Assert.StartsWith("repositoryRoot:", result.Status, StringComparison.Ordinal);
        Assert.Contains("repositoryRoot:", result.sentence, StringComparison.Ordinal);

        var refused = Assert.Single(Of(lines, "session-document.refused"));
        Assert.Equal(config.SessionId, refused.GetProperty("session").GetString());
        Assert.Equal("repositoryRoot", refused.GetProperty("field").GetString());
        Assert.Contains("no open workspace", refused.GetProperty("reason").GetString(), StringComparison.Ordinal);
        Assert.Empty(Of(lines, "session-document.bound"));
    }

    [Fact]
    public void Bind_WithTwoRoutableBackends_RefusesEngineIdRatherThanChoosing()
    {
        var config = Create(["claude-code", "codex"]);
        var providers = Providers();

        var (_, lines) = WithShell(shell =>
        {
            shell.OpenSessionDocument(config);
            return SessionComposerBinder.Bind(
                shell, config, ["claude-code", "codex"], "feature",
                repositoryRoot: _root, dataDirectory: _root, providers, new NeverAffirms());
        });

        var refused = Assert.Single(Of(lines, "session-document.refused"));
        Assert.Equal("engineId", refused.GetProperty("field").GetString());
        Assert.Empty(Of(lines, "session-document.bound"));
    }

    private sealed class NeverAffirms : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }
}
