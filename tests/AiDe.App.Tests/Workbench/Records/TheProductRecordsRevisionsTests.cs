using System.Runtime.Versioning;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Records;

/// <summary>
/// The production App saves notebook revisions over T14's daemon ops (<c>notebook.open</c>,
/// <c>notebook.revision.save</c>), so the stale mark is live in the product, not only over a test store.
/// </summary>
/// <remarks>
/// The client is a real <see cref="WorkspaceClient"/> connected through a named pipe to a daemon endpoint over a real
/// schema-v2 store — the composition <c>MainWindow</c> hands the shell.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class TheProductRecordsRevisionsTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task TheShellsNotebook_SavesRevisionsOverTheDaemon_AndAnEditMarksTheResultStale()
    {
        await WithDaemonAsync(async client =>
        {
            var config = new SessionConfig(NotebookFixture.SessionId, "Binding", _fixture.Root, DateTimeOffset.UnixEpoch,
                [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max"));

            // The model the shell's OWN construction site builds is the one asserted on.
            var (state, saved, latestAfter, currentness) = Sta.Run(() =>
            {
                using var shell = new WorkbenchShell(client);
                shell.OpenSessionDocument(config);
                var model = shell.Notebook(NotebookId.ForSession(config.SessionId))!;

                var revision = model.PrepareRevisionAsync("Fix the door.\n", CancellationToken.None).GetAwaiter().GetResult();
                model.BindResult(1, revision);
                var edited = model.PrepareRevisionAsync("Fix the window.\n", CancellationToken.None).GetAwaiter().GetResult();
                return (model.RevisionState, revision, edited, model.CurrentnessOf(1, "Fix the window.\n"));
            }, 90);

            // The construction site bound the daemon's ops: nothing says "not recorded", and both saves are rows.
            Assert.Null(state);
            Assert.NotNull(saved);
            Assert.Equal(1, saved!.RevisionNo);
            Assert.Equal(2, latestAfter!.RevisionNo);
            Assert.Equal(ResultCurrentness.Stale, currentness);

            // Read back over the same op the product binds to: the store's head is the edit.
            var head = await client.ReadLatestNotebookRevisionAsync(config.SessionId, CancellationToken.None);
            Assert.Equal("Fix the window.\n", head!.Body);
            Assert.Equal(latestAfter.BodySha, head.BodySha);
        });
    }

    /// <summary>
    /// T14b: <c>notebook.open</c> refuses a notebook whose derived journey id is taken, or is the reserved
    /// terminal-drafts journey, and writes nothing. The binding names the stable code and saves no revision.
    /// </summary>
    [Theory]
    [InlineData("20261019T060000Z-collide1", true, "AIDE-STORE-JOURNEY-ID-COLLISION")]
    [InlineData("terminal-drafts", false, "AIDE-STORE-RESERVED-JOURNEY")]
    public async Task ARefusedOpen_SavesNoRevision_AndTheModelNamesTheStableCode(string sessionId, bool seedJourney, string code)
    {
        if (seedJourney)
        {
            using var writer = _fixture.Store.BeginWrite();
            writer.AppendJourney(new JourneyRow("journey-" + sessionId, "work", "someone else's journey"));
            writer.Commit();
        }

        await WithDaemonAsync(async client =>
        {
            using var model = new NotebookModel(
                new SessionDocumentViewModel(sessionId, "Refused", _fixture.Root, [CanvasModeCatalog.ConsoleModeId]),
                new DaemonNotebookRevisions(client, "Refused"));

            var saved = await model.PrepareRevisionAsync("Fix the door.\n", CancellationToken.None);

            Assert.Null(saved);
            Assert.True(model.RevisionState is { } reason && reason.Contains(code, StringComparison.Ordinal),
                $"the model's revision state reads \"{model.RevisionState}\"");
            Assert.StartsWith(DaemonNotebookRevisions.OpenRefusedCopy, model.RevisionState, StringComparison.Ordinal);
            Assert.Null(await client.ReadLatestNotebookRevisionAsync(sessionId, CancellationToken.None));
        });
    }

    private async Task WithDaemonAsync(Func<WorkspaceClient, Task> body)
    {
        var pipe = "aide-t8-nb-" + Guid.NewGuid().ToString("N");
        var endpoint = new DaemonEndpoint(pipe, new CapabilityRegistry(), _ => _fixture.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => _fixture.Store.CoreEpoch);
        WorkspaceOperations.RegisterNotebook(endpoint, new NotebookService(_fixture.Store));
        var server = new IpcServer(pipe, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var running = server.RunAsync(life.Token);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);
            await body(client);
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
        }
    }
}
