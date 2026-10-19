using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Dispatch;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 161/162 fix-forward (T16b): the binding line is true after a restart, across a real send-edit-send sequence,
/// when a receipt lands after the last snapshot, and the "Terminal" word has one source.
/// </summary>
public sealed class TheRowsAttemptBindingSurvivesRestartAndSequenceTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("ws-t16b", _fixture.Store.CoreEpoch, SessionGeneration: 1);

    private static string LineOf(NotebookRuns runs, TurnView turn) =>
        new TurnItem(turn) { Attempt = runs.AttemptOf(turn.Ordinal)?.Attempt }.BindingLine;

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

    [Fact]
    public void AfterARestart_TheStoreHoldsTheAttempt_AndTheThreadRestoresItsRow_WithTheLiveBindingLine()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                string liveLine;
                var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using (host)
                {
                    using var model = _fixture.NewModel();
                    model.Runs.BindHost(host, Scope);
                    var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                    window.Content = document;
                    _fixture.ConfigureStub(document);
                    _fixture.ReleaseRun();
                    document.Composer.Draft.SetFreeFormText("say ok");
                    await NotebookFixture.SendAndSettleAsync(document);
                    await UntilAsync(window, () => model.Thread.Current.Turns[0].State is not (TurnState.Running or TurnState.Preparing),
                        () => "the attempt never ended");
                    liveLine = LineOf(model.Runs, model.Thread.Current.Turns[0]);
                    Assert.StartsWith("Sent from revision ", liveLine);
                    window.Content = null;
                    document.Dispose();
                }

                // The restart in the shell's order (Ruling 172): the host is bound when its open completes, after the document is built.
                using var restarted = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using var reopened = _fixture.NewModel();
                var opened = new TaskCompletionSource<IAttemptHost>(TaskCreationOptions.RunContinuationsAsynchronously);
                reopened.Runs.BindHostWhenOpen(() => opened.Task, Scope);
                using var surface = new SessionDocumentSurface(reopened, null, new RecordingAnnouncer());
                window.Content = surface;
                opened.SetResult(restarted);
                await surface.LastReadBack.WaitAsync(NotebookFixture.Bound);

                // T16b's premise turned around (Ruling 163, T17): the read-back restores the attempt as a row, built from the
                // attempt's own fields, so its binding line is the one the live row showed.
                Assert.Single(reopened.Runs.ReadBack);
                var restored = Assert.Single(reopened.Thread.Current.Turns);
                Assert.Equal(liveLine, LineOf(reopened.Runs, restored));
                window.Content = null;
            });
    }

    [Fact]
    public void ASendEditSendSequence_LeavesTurnAsRowOnItsRevisionAndRequest_AndTurnBOnItsOwn()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using var model = _fixture.NewModel();
                model.Runs.BindHost(host, Scope);
                using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = document;
                _fixture.ConfigureStub(document);
                _fixture.ReleaseRun();

                var changes = 0;
                model.Runs.AttemptsChanged += () => changes++;
                document.Composer.Draft.SetFreeFormText("first words");
                await NotebookFixture.SendAndSettleAsync(document);
                await UntilAsync(window, () => model.Thread.Current.Turns[0].State is not (TurnState.Running or TurnState.Preparing), () => "A never ended");

                document.Composer.Draft.SetFreeFormText("second words, edited");
                await NotebookFixture.SendAndSettleAsync(document);
                await UntilAsync(window, () => model.Thread.Current.Turns.Count == 2 && model.Thread.Current.Turns[1].State is not (TurnState.Running or TurnState.Preparing), () => "B never ended");

                var turns = model.Thread.Current.Turns;
                var a = model.Runs.AttemptOf(turns[0].Ordinal)!.Attempt;
                var b = model.Runs.AttemptOf(turns[1].Ordinal)!.Attempt;
                Assert.Equal(model.RevisionOf(turns[0].Ordinal)!.RevisionNo, a.RevisionNo);
                Assert.Equal(model.RevisionOf(turns[1].Ordinal)!.RevisionNo, b.RevisionNo);
                Assert.NotEqual(a.RevisionNo, b.RevisionNo);
                Assert.NotEqual(a.ProjectionSha, b.ProjectionSha);
                Assert.True(changes >= 2, $"each recorded receipt must tell the rows; {changes} calls");
                Assert.StartsWith($"Sent from revision {a.RevisionNo} · request {a.ProjectionSha![..7]} · via ", LineOf(model.Runs, turns[0]));
                Assert.StartsWith($"Sent from revision {b.RevisionNo} · request {b.ProjectionSha![..7]} · via ", LineOf(model.Runs, turns[1]));
                window.Content = null;
            });
    }

    [Fact]
    public void AReceiptThatLandsAfterTheSnapshot_ShowsItsBindingOnRefresh_NotAttemptNotRecorded()
    {
        AttemptRow? landed = null;
        Sta.Pump(
            create: () =>
            {
                var feed = ThreadFixtures.Feed([new TurnView(1, "env-1", "Words", [], TurnState.Completed,
                    new OutcomeView("claude-code", null, 0, null, null, 0), null, [], "b", ThreadFixtures.T0)]).Feed;
                feed.AttemptOf = _ => landed;
                return feed;
            },
            configure: window => { window.Width = 1200; window.Height = 700; },
            body: (window, feed) =>
            {
                Assert.Equal("Attempt not recorded", feed.Rows[0].BindingLine);
                landed = new AttemptRow("k", "ws", 1, "nb", 4, "s", 1, ProjectionSha: "ab12cd34ef", RouteFingerprint: "acp:codex");
                feed.RefreshAttempts();
                Assert.Equal("Sent from revision 4 · request ab12cd3 · via codex", feed.Rows[0].BindingLine);
                return Task.CompletedTask;
            });
    }

    [Fact]
    public void ASentTurnsRow_ShowsItsBinding_WhileTheRunIsStillHeld_ThroughTheSurface()
    {
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                using var host = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
                using var model = _fixture.NewModel();
                model.Runs.BindHost(host, Scope);
                using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = document;
                _fixture.ConfigureStub(document);

                document.Composer.Draft.SetFreeFormText("held words");
                Assert.NotNull(document.Composer.Send());
                await UntilAsync(window, () => model.Runs.AttemptOf(model.Thread.Current.Turns[0].Ordinal) is not null, () => "no receipt recorded");

                string Help() => ThreadFixtures.Visuals<Expander>(document)
                    .Where(e => System.Windows.Automation.AutomationProperties.GetName(e).StartsWith("Provenance of", StringComparison.Ordinal))
                    .Select(System.Windows.Automation.AutomationProperties.GetHelpText).FirstOrDefault() ?? "no provenance disclosure yet";
                await UntilAsync(window, () => Help().StartsWith("Sent from revision ", StringComparison.Ordinal), () => $"the row reads \"{Help()}\" with the receipt recorded");
                _fixture.ReleaseRun();
                window.Content = null;
            });
    }

    [Fact]
    public void TheTerminalWord_HasOneSource_AndBothSurfacesShowIt()
    {
        Assert.Equal(RouteWords.TerminalWord, SurfaceContentFactory.Kinds.Single(k => k.Kind == "terminal").Title);
        Assert.Equal(RouteWords.TerminalWord, RouteWords.For(RouteWords.PtyFingerprint));
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AiDe.App", "Workbench", "SurfaceContentFactory.cs"));
        Assert.DoesNotContain("\"terminal\", \"Terminal\"", source);
    }
}
