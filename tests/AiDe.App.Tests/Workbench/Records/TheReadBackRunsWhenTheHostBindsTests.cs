using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Store.Records;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 172 (T17d): the restart read-back runs when the attempt host BINDS, whenever that is, exactly once per bind.
/// The shell binds with <see cref="NotebookRuns.BindHostWhenOpen"/> and builds the document while the host still opens
/// (<c>WorkbenchShell.RegisterSessionDocument</c>), so every reopen here is built in that order, never host-first.
/// </summary>
/// <remarks>
/// Today's window (the lifetime that records the attempts) binds first: it reads nothing back. Each restart is a new
/// host, a new model and a new surface over the same store and the same session envelope file.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class TheReadBackRunsWhenTheHostBindsTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("workspace", _fixture.Store.CoreEpoch, SessionGeneration: 1);

    private static string Line(AttemptRow attempt) =>
        $"Sent from revision {attempt.RevisionNo} · request {attempt.ProjectionSha![..7]} · via claude-code";

    /// <summary>
    /// Check 1, through the product's own path: the shell over a real <see cref="WorkspaceClient"/> to a daemon revives
    /// the restored session pane (<see cref="WorkbenchShell.ReviveRestoredSessionDocuments"/>), which binds the host
    /// asynchronously and builds the document at once. The document renders both recorded attempts once the host binds.
    /// </summary>
    [Fact]
    public async Task TheShellsRevive_RendersTheRecordedAttempts_OnceTheHostItOpensAsynchronouslyBinds()
    {
        var accounts = new[] { new AccountRef("anthropic", "max") };
        new SessionConfigStore(_fixture.Root, NotebookFixture.SessionId).Create("Binding", _fixture.Root, accounts, accounts[0], DateTimeOffset.UnixEpoch);
        (AttemptRow A, AttemptRow B) recorded = default;
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) => recorded = await RecordTwoAttemptsAsync(window));

        var pipe = "aide-t17d-revive-" + Guid.NewGuid().ToString("N");
        var endpoint = new DaemonEndpoint(pipe, new CapabilityRegistry(), _ => _fixture.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => _fixture.Store.CoreEpoch);
        WorkspaceOperations.RegisterNotebook(endpoint, new NotebookService(_fixture.Store));
        WorkspaceOperations.RegisterDispatch(endpoint, new BoundaryDispatcher(_fixture.Store));
        var server = new IpcServer(pipe, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var running = server.RunAsync(life.Token);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);
            Sta.Pump(
                create: () => new Border(),
                configure: window => { window.Width = 1440; window.Height = 900; },
                body: async (window, border) =>
                {
                    using var shell = new WorkbenchShell(client);
                    border.Child = shell.WorkbenchRoot;

                    // A restored arrangement holds the session's pane; the revive builds its live document.
                    var surfaceId = SessionDocumentSurface.SurfaceIdFor(NotebookFixture.SessionId);
                    shell.Service.Apply(new LayoutOperation.AddSurface(shell.Service.Current.AllStacks().First().Id, new Surface(surfaceId, SessionDocumentSurface.Kind, "Binding")));
                    shell.Adapter.Render();
                    Assert.Equal([NotebookFixture.SessionId], shell.ReviveRestoredSessionDocuments(_fixture.Root).Select(c => c.SessionId));
                    shell.Adapter.Render();

                    var model = shell.Notebook(NotebookId.ForSession(NotebookFixture.SessionId))!;
                    var document = shell.Adapter.SurfaceContent<SessionDocumentSurface>(surfaceId);
                    Assert.NotNull(document);
                    await UntilAsync(window, () => document.Thread.Rows.Count >= 2,
                        () => $"the revived document renders {document.Thread.Rows.Count} rows for 2 recorded attempts; "
                            + $"host bound={model.Runs.Host is not null}, read back={model.Runs.ReadBack.Count}");

                    var rows = document.Thread.Rows;
                    Assert.Equal(["b1 first words", "b2 second words"], rows.Select(r => r.DisplayOrdinal + " " + r.Words));
                    Assert.Equal([Line(recorded.A!), Line(recorded.B!)], rows.Select(r => r.BindingLine));
                    await document.LastReadBack.WaitAsync(NotebookFixture.Bound);
                    border.Child = null;
                });
        }
        finally
        {
            await life.CancelAsync();
            try
            {
                await running;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    /// <summary>
    /// Check 2, the shell's order reproduced exactly with the opening held: nothing is read back before the bind; a turn
    /// sent while the host opens waits for the read-back (Ruling 163), so it is b3; the bind reads each attempt once.
    /// </summary>
    [Fact]
    public void ATurnSentWhileTheHostOpens_WaitsForTheReadBackTheBindRuns_AndIsB3_AndEachAttemptIsReadOnce()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                var (a, b) = await RecordTwoAttemptsAsync(window);

                var opening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                AttemptHost? opened = null;
                IAttemptHost? counted = null;
                var model = _fixture.NewModel();
                model.Runs.BindHostWhenOpen(
                    async () =>
                    {
                        await opening.Task;
                        opened = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                        return counted = CountingHost.Over(opened);
                    },
                    Scope);
                var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = surface;
                _fixture.ConfigureStub(surface);
                await Settle(window, rounds: 5);

                Assert.Null(model.Runs.Host);
                Assert.Empty(surface.Thread.Rows);
                Assert.False(surface.LastReadBack.IsCompleted,
                    "while the host opens the read-back is not pending, so a turn sent now takes b1 ahead of the restored turns");

                surface.Composer.Draft.SetFreeFormText("third words");
                Assert.NotNull(surface.Composer.Send());
                await Settle(window);
                Assert.Empty(model.Thread.Current.Turns);

                opening.SetResult();
                await UntilAsync(window, () => model.Runs.Host is not null, () => "the host never bound");
                await surface.LastReadBack.WaitAsync(NotebookFixture.Bound);
                await surface.LastAdmission.WaitAsync(NotebookFixture.Bound);
                await UntilAsync(window, () => surface.Thread.Rows.Count == 3 && Ended(model, 2),
                    () => $"{surface.Thread.Rows.Count} rows: [{string.Join(", ", surface.Thread.Rows.Select(r => r.DisplayOrdinal + " " + r.Words))}]");

                var rows = surface.Thread.Rows;
                Assert.Equal(["b1 first words", "b2 second words", "b3 third words"], rows.Select(r => r.DisplayOrdinal + " " + r.Words));
                Assert.Equal(a.DispatchKey, model.Runs.AttemptOf(rows[0].Ordinal)!.DispatchKey);
                Assert.Equal(b.DispatchKey, model.Runs.AttemptOf(rows[1].Ordinal)!.DispatchKey);
                var c = model.Runs.AttemptOf(rows[2].Ordinal)!.Attempt;
                Assert.NotEqual(a.DispatchKey, c.DispatchKey);
                Assert.NotEqual(b.DispatchKey, c.DispatchKey);
                await UntilAsync(window, () => rows[2].BindingLine.StartsWith("Sent from", StringComparison.Ordinal), () => $"b3 reads \"{rows[2].BindingLine}\"");
                Assert.Equal([Line(a), Line(b), Line(c)], rows.Select(r => r.BindingLine));
                Assert.Equal(1, CountingHost.Reads(counted, a.DispatchKey));
                Assert.Equal(1, CountingHost.Reads(counted, b.DispatchKey));
                Close(window, opened!, model, surface);
            });
    }

    /// <summary>
    /// Ruling 175 (1), (3), node 3 check A: a Send while the read-back is pending keeps the words in the editor, disables
    /// Send and says why it waits, in the status line and in UIA. A second press admits nothing. Once the read-back ends
    /// the turn is admitted as b3, after the restored turns (Ruling 163), and only then does the editor clear.
    /// </summary>
    [Fact]
    public void ASendWhileTheTurnsRestore_KeepsTheWordsAndSaysWhyItWaits_ThenAdmitsExactlyOneTurnAsB3_AndClears()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                await RecordTwoAttemptsAsync(window);
                var (model, surface, opening, opened) = await ReopenWithTheOpenHeldAsync(window, host => host);
                var send = SendButton(surface);

                surface.Composer.Draft.SetFreeFormText("third words");
                Assert.NotNull(surface.Composer.Send());
                await Settle(window);

                Assert.Equal("third words", surface.Composer.Draft.SourceText);
                Assert.False(send.IsEnabled, "Send stayed enabled while the sent turn waits for the read-back");
                Assert.Equal(SessionDocumentSurface.RestoringTurns, surface.Composer.Status);
                Assert.Equal(SessionDocumentSurface.RestoringTurns, AutomationProperties.GetHelpText(send));

                Assert.Null(surface.Composer.Send());
                await Settle(window);
                Assert.Empty(model.Thread.Current.Turns);
                Assert.Equal("third words", surface.Composer.Draft.SourceText);

                opening.SetResult();
                await UntilAsync(window, () => model.Runs.Host is not null, () => "the host never bound");
                await surface.LastReadBack.WaitAsync(NotebookFixture.Bound);
                await surface.LastAdmission.WaitAsync(NotebookFixture.Bound);
                await UntilAsync(window, () => surface.Thread.Rows.Count >= 3 && Ended(model, 2),
                    () => $"{surface.Thread.Rows.Count} rows: [{string.Join(", ", surface.Thread.Rows.Select(r => r.DisplayOrdinal + " " + r.Words))}]");
                await Settle(window, rounds: 5);

                Assert.Equal(["b1 first words", "b2 second words", "b3 third words"], surface.Thread.Rows.Select(r => r.DisplayOrdinal + " " + r.Words));
                Assert.Equal(string.Empty, surface.Composer.Draft.SourceText);
                Assert.True(send.IsEnabled, "Send stayed disabled after the waiting turn was admitted");
                Assert.NotEqual(SessionDocumentSurface.RestoringTurns, surface.Composer.Status);
                Close(window, opened()!, model, surface);
            });
    }

    /// <summary>Ruling 175 (1): the restoring sentence is DESIGN.md's copy, one row citing the ruling, pinned to the const.</summary>
    [Fact]
    public void TheRestoringSentence_IsOneDesignCopyRow_CitingRuling175()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var rows = File.ReadAllLines(Path.Combine(dir!.FullName, "DESIGN.md"));
        var row = Assert.Single(rows, l => l.Contains($"`{SessionDocumentSurface.RestoringTurns}`", StringComparison.Ordinal));
        Assert.Contains("Ruling 175", row, StringComparison.Ordinal);
    }

    /// <summary>
    /// A reopen in the shell's order with the host's open held until the test releases it: the model binds with
    /// <see cref="NotebookRuns.BindHostWhenOpen"/>, the document is built at once, and the opened host is passed through
    /// <paramref name="wrap"/> (the host the notebook binds).
    /// </summary>
    private async Task<(NotebookModel Model, SessionDocumentSurface Surface, TaskCompletionSource Opening, Func<AttemptHost?> Opened)> ReopenWithTheOpenHeldAsync(
        Window window, Func<IAttemptHost, IAttemptHost> wrap)
    {
        var opening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AttemptHost? opened = null;
        var model = _fixture.NewModel();
        model.Runs.BindHostWhenOpen(
            async () =>
            {
                await opening.Task;
                opened = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                return wrap(opened);
            },
            Scope);
        var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
        window.Content = surface;
        _fixture.ConfigureStub(surface);
        await Settle(window, rounds: 5);
        Assert.False(surface.LastReadBack.IsCompleted, "the read-back is not pending while the host opens");
        return (model, surface, opening, () => opened);
    }

    private static Button SendButton(DependencyObject root) =>
        FindButton(root, b => Equals(b.Content, "Send")) ?? throw new InvalidOperationException("no Send button in the composer");

    private static Button? FindButton(DependencyObject root, Func<Button, bool> match)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is Button button && match(button))
            {
                return button;
            }

            if (FindButton(child, match) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Check 2, once per bind: a document built after the bind (today's path) reads back once; a second document over the
    /// same bind does not read again; a second bind reads back once more and invents no row.
    /// </summary>
    [Fact]
    public void TheReadBackRunsOncePerBind_NotOncePerDocument_AndARebindReadsBackOnceMore()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                var (a, b) = await RecordTwoAttemptsAsync(window);

                var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                var counted = CountingHost.Over(host);
                var model = _fixture.NewModel();
                model.Runs.BindHost(counted, Scope);
                var first = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = first;
                await first.LastReadBack.WaitAsync(NotebookFixture.Bound);
                await UntilAsync(window, () => first.Thread.Rows.Count == 2, () => $"the first document renders {first.Thread.Rows.Count} rows");
                Assert.Equal(1, CountingHost.Reads(counted, a.DispatchKey));

                // The pane closes and a new one is bound to the same model: same bind, so no second read-back.
                window.Content = null;
                first.Dispose();
                var second = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = second;
                await second.LastReadBack.WaitAsync(NotebookFixture.Bound);
                await Settle(window, rounds: 5);
                Assert.Equal(["b1 first words", "b2 second words"], second.Thread.Rows.Select(r => r.DisplayOrdinal + " " + r.Words));
                Assert.True(CountingHost.Reads(counted, a.DispatchKey) == 1, $"a second document over one bind read attempt A back {CountingHost.Reads(counted, a.DispatchKey)} times");

                // A second bind reads back exactly once more; the attempts it finds are held, so no row is added.
                model.Runs.BindHost(counted, Scope);
                await UntilAsync(window, () => CountingHost.Reads(counted, b.DispatchKey) >= 2, () => $"the second bind read attempt B back {CountingHost.Reads(counted, b.DispatchKey)} times in all");
                await second.LastReadBack.WaitAsync(NotebookFixture.Bound);
                await Settle(window, rounds: 5);
                Assert.Equal(2, CountingHost.Reads(counted, a.DispatchKey));
                Assert.Equal(2, CountingHost.Reads(counted, b.DispatchKey));
                Assert.Equal(2, second.Thread.Rows.Count);
                Close(window, host, model, second);
            });
    }

    /// <summary>Check 2, the retry: an open that fails reads nothing back; the retry that binds reads back once.</summary>
    [Fact]
    public void AnOpenThatFails_ReadsNothingBack_AndTheRetryThatBinds_ReadsBackOnce()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                var (a, b) = await RecordTwoAttemptsAsync(window);

                var tries = 0;
                AttemptHost? opened = null;
                IAttemptHost? counted = null;
                var model = _fixture.NewModel();
                model.Runs.BindHostWhenOpen(
                    async () =>
                    {
                        await Task.Yield();
                        if (++tries == 1)
                        {
                            throw new IOException("the attempt-host lock is busy.");
                        }

                        opened = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                        return counted = CountingHost.Over(opened);
                    },
                    Scope);
                var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = surface;
                await UntilAsync(window, () => model.Runs.CanRetryHost, () => $"the first open never failed; refusal \"{model.Runs.HostRefusal}\"");
                await Settle(window, rounds: 5);
                Assert.Empty(surface.Thread.Rows);

                await model.Runs.RetryHostAsync();
                Assert.NotNull(model.Runs.Host);
                await UntilAsync(window, () => surface.Thread.Rows.Count == 2,
                    () => $"after the retry bound the host the document renders {surface.Thread.Rows.Count} rows; read back={model.Runs.ReadBack.Count}");
                await surface.LastReadBack.WaitAsync(NotebookFixture.Bound);
                await Settle(window, rounds: 5);
                Assert.Equal(["b1 first words", "b2 second words"], surface.Thread.Rows.Select(r => r.DisplayOrdinal + " " + r.Words));
                Assert.Equal(1, CountingHost.Reads(counted, a.DispatchKey));
                Assert.Equal(1, CountingHost.Reads(counted, b.DispatchKey));
                Close(window, opened!, model, surface);
            });
    }

    /// <summary>Today's window: two answered turns, each an attempt of its revision. Returns both attempts.</summary>
    private async Task<(AttemptRow A, AttemptRow B)> RecordTwoAttemptsAsync(Window window)
    {
        Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
        _fixture.ReleaseRun();
        var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
        var model = _fixture.NewModel();
        model.Runs.BindHost(host, Scope);
        var surface = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
        window.Content = surface;
        _fixture.ConfigureStub(surface);
        surface.Composer.Draft.SetFreeFormText("first words");
        await NotebookFixture.SendAndSettleAsync(surface);
        await UntilAsync(window, () => Ended(model, 0), () => "A never ended");
        surface.Composer.Draft.SetFreeFormText("second words");
        await NotebookFixture.SendAndSettleAsync(surface);
        await UntilAsync(window, () => Ended(model, 1), () => "B never ended");
        var recorded = (model.Runs.AttemptOf(1)!.Attempt, model.Runs.AttemptOf(2)!.Attempt);
        Close(window, host, model, surface);
        return recorded;
    }

    private static bool Ended(NotebookModel model, int index) =>
        model.Thread.Current.Turns.Count > index && model.Thread.Current.Turns[index].State is not (TurnState.Running or TurnState.Preparing);

    private static void Close(Window window, AttemptHost host, NotebookModel model, SessionDocumentSurface surface)
    {
        window.Content = null;
        surface.Dispose();
        model.Dispose();
        host.Dispose();
    }

    private static async Task UntilAsync(Window window, Func<bool> condition, Func<string> what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }

        Assert.True(condition(), what());
    }

    private static async Task Settle(Window window, int rounds = 1)
    {
        for (var i = 0; i < rounds; i++)
        {
            await Task.Delay(20);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
    }

    /// <summary>The real host, counting the read-back's own call (<see cref="IAttemptHost.ReadAsync"/>) per dispatch key.</summary>
    public class CountingHost : DispatchProxy
    {
        private readonly ConcurrentDictionary<string, int> _reads = new(StringComparer.Ordinal);
        private IAttemptHost _inner = null!;

        public static IAttemptHost Over(IAttemptHost inner)
        {
            var proxy = Create<IAttemptHost, CountingHost>();
            ((CountingHost)(object)proxy)._inner = inner;
            return proxy;
        }

        public static int Reads(IAttemptHost? host, string dispatchKey) =>
            host is CountingHost counting ? counting._reads.GetValueOrDefault(dispatchKey) : 0;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(IAttemptHost.ReadAsync) && args is [string key, ..])
            {
                _reads.AddOrUpdate(key, 1, (_, n) => n + 1);
            }

            try
            {
                return targetMethod.Invoke(_inner, args);
            }
            catch (TargetInvocationException error) when (error.InnerException is { } inner)
            {
                ExceptionDispatchInfo.Throw(inner);
                throw;
            }
        }
    }
}
