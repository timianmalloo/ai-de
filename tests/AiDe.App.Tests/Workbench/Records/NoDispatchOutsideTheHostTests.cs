using System.Diagnostics;
using System.IO;
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
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 159 (DR req-01M59FX8EAK66B8PQEDQR4TRGN, option A): no turn runs outside the attempt host. A turn sent before
/// the host opens waits for it; a window that cannot open the host refuses Send with the cause and never runs directly
/// (ADR-0041's write-ahead invariant); a dead holder never locks the user out; the attempt's epoch is read at dispatch.
/// </summary>
public sealed class NoDispatchOutsideTheHostTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private string LockFile => Path.Combine(_fixture.Root, ".aide", "attempt-host.lock");

    private AttemptScope Scope => new("ws-t8", _fixture.Store.CoreEpoch, SessionGeneration: 1);

    private async Task<IAttemptHost> OpenHost() =>
        await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);

    [Fact]
    public void ATurnSentBeforeTheHostOpens_WaitsForIt_AndRunsAsAnAttempt()
    {
        Pump(async (window, model, document) =>
        {
            var opening = new TaskCompletionSource<IAttemptHost>(TaskCreationOptions.RunContinuationsAsynchronously);
            model.Runs.BindHostWhenOpen(() => opening.Task, Scope);

            document.Composer.Draft.SetFreeFormText("say ok");
            Assert.NotNull(document.Composer.Send());
            await Task.Delay(800);
            Assert.False(_fixture.RunOpened, "a turn ran before the host opened: a direct run, outside the host");

            // Ruling 175(5): while the open's read-back is pending the words stay visible with the restoring status, and
            // no row joins the thread; the row appears once the read-back completes, numbered after the restored turns
            // (none here, so b1; the b3 case is TheReadBackRunsWhenTheHostBindsTests).
            Assert.Empty(model.Thread.Current.Turns);
            Assert.Equal("say ok", document.Composer.Draft.SourceText);
            Assert.Equal(SessionDocumentSurface.RestoringTurns, document.Composer.Status);

            using var host = (AttemptHost)await OpenHost();
            opening.SetResult(host);
            _fixture.ReleaseRun();
            await document.LastAdmission.WaitAsync(NotebookFixture.Bound);
            var turn = Assert.Single(model.Thread.Current.Turns);
            Assert.Equal("b1", turn.DisplayOrdinal);
            var ordinal = turn.Ordinal;
            await UntilAsync(window, () => model.Thread.Current.Turns[0].State == TurnState.Answered,
                () => $"the waiting turn reads {model.Thread.Current.Turns[0].State}");
            Assert.Equal(DispatchState.ResponseComplete, model.Runs.AttemptOf(ordinal)?.Terminal?.State);
        });
    }

    [Fact]
    public void WhenAnotherWindowHoldsTheHost_SendIsRefusedWithTheHostedElsewhereCopy_AndTheTurnNeverRuns()
    {
        Pump(async (window, model, document) =>
        {
            using var holder = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
            model.Runs.BindHostWhenOpen(OpenHost, Scope);

            document.Composer.Draft.SetFreeFormText("say ok");
            document.Composer.Send();
            await UntilAsync(window, () => model.Runs.HostRefusal is not null, () => "the window never learned it cannot host runs");
            await Task.Delay(500);

            Assert.Equal(NotebookRuns.HostedElsewhere, model.Runs.HostRefusal);
            Assert.Equal("This workspace's runs are hosted in another window. Send from that window, or close it to host runs here.", NotebookRuns.HostedElsewhere);
            Assert.False(_fixture.RunOpened, "the refused window ran the turn directly");
            Assert.All(model.Thread.Current.Turns, t => Assert.Null(model.Runs.AttemptOf(t.Ordinal)));
        });
    }

    [Fact]
    public void AnyOtherOpenFailure_RefusesWithItsOwnCause_AndARetryBindsTheHost()
    {
        Pump(async (window, model, document) =>
        {
            var tries = 0;
            model.Runs.BindHostWhenOpen(() => ++tries == 1 ? Task.FromException<IAttemptHost>(new IOException("the lock file's folder is read-only")) : OpenHost(), Scope);
            await UntilAsync(window, () => model.Runs.HostRefusal is not null, () => "the open failure was never surfaced");

            Assert.Contains("the lock file's folder is read-only", model.Runs.HostRefusal, StringComparison.Ordinal);
            Assert.NotEqual(NotebookRuns.HostedElsewhere, model.Runs.HostRefusal);

            await model.Runs.RetryHostAsync();
            Assert.Null(model.Runs.HostRefusal);
            Assert.NotNull(model.Runs.Host);
            using var host = (AttemptHost)model.Runs.Host!;

            document.Composer.Draft.SetFreeFormText("say ok");
            Assert.NotNull(document.Composer.Send());
            _fixture.ReleaseRun();
            await UntilAsync(window, () => model.Thread.Current.Turns[0].State == TurnState.Answered,
                () => $"the turn after the retry reads {model.Thread.Current.Turns[0].State}");
            Assert.NotNull(model.Runs.AttemptOf(model.Thread.Current.Turns[0].Ordinal));
        });
    }

    [Fact]
    public void TheAttemptsEpoch_IsReadAtDispatch_NotFixedAtBind()
    {
        Pump(async (window, model, document) =>
        {
            using var host = (AttemptHost)await OpenHost();
            var current = _fixture.Store.CoreEpoch;

            // Bound under an epoch a daemon restart has since replaced; the refresh reads the new handshake's.
            model.Runs.BindHost(host, Scope with { WorkspaceEpoch = current + 7, RefreshEpoch = _ => Task.FromResult(current) });
            document.Composer.Draft.SetFreeFormText("say ok");
            Assert.NotNull(document.Composer.Send());
            _fixture.ReleaseRun();
            await UntilAsync(window, () => model.Thread.Current.Turns[0].State is not (TurnState.Running or TurnState.Preparing),
                () => "the attempt never ended");

            Assert.Equal(TurnState.Answered, model.Thread.Current.Turns[0].State);
            Assert.Equal(current, model.Runs.AttemptOf(model.Thread.Current.Turns[0].Ordinal)?.Attempt.WorkspaceEpoch);
        });
    }

    /// <summary>
    /// Ruling 159(1): while another window holds the host, Send is disabled with the exact copy as its UIA help text,
    /// the read-only views still work, and the copy shows only while that holder lives — closing it hosts runs here.
    /// </summary>
    [Fact]
    public void WhileAnotherWindowHoldsTheHost_SendIsDisabledWithTheCopy_TheViewsStillWork_AndClosingTheHolderEnablesIt()
    {
        Pump(async (window, model, document) =>
        {
            var holder = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
            model.Runs.BindHostWhenOpen(OpenHost, Scope);
            var send = ButtonIn(document.Composer, b => Equals(b.Content, "Send"));
            await UntilAsync(window, () => !send.IsEnabled, () => "Send stayed enabled while another window holds the host");

            Assert.Equal(NotebookRuns.HostedElsewhere, AutomationProperties.GetHelpText(send));
            Assert.True(document.IsEnabled && document.Composer.IsEnabled, "a hosted-elsewhere window must still read its views");
            Assert.Null(FindButton(document.Composer, b => AutomationProperties.GetName(b) == "Retry hosting runs" && b.IsVisible));

            holder.Dispose();
            await UntilAsync(window, () => send.IsEnabled && model.Runs.Host is not null,
                () => $"after the holder closed Send reads enabled={send.IsEnabled}, host={model.Runs.Host is not null}");
            Assert.NotEqual(NotebookRuns.HostedElsewhere, AutomationProperties.GetHelpText(send));
            using var host = (AttemptHost)model.Runs.Host!;
        });
    }

    /// <summary>Ruling 159(1): any other open failure disables Send with its own cause and shows a Retry wired to the host's retry.</summary>
    [Fact]
    public void AnyOtherOpenFailure_DisablesSendWithItsCause_AndTheRetryControlBindsTheHost()
    {
        Pump(async (window, model, document) =>
        {
            var tries = 0;
            model.Runs.BindHostWhenOpen(() => ++tries == 1 ? Task.FromException<IAttemptHost>(new IOException("the lock file's folder is read-only")) : OpenHost(), Scope);
            var send = ButtonIn(document.Composer, b => Equals(b.Content, "Send"));
            await UntilAsync(window, () => !send.IsEnabled, () => "Send stayed enabled after the host failed to open");

            Assert.Contains("the lock file's folder is read-only", AutomationProperties.GetHelpText(send), StringComparison.Ordinal);
            var retry = FindButton(document.Composer, b => AutomationProperties.GetName(b) == "Retry hosting runs");
            Assert.NotNull(retry);
            Assert.Equal(Visibility.Visible, retry!.Visibility);

            retry.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            await UntilAsync(window, () => send.IsEnabled && model.Runs.Host is not null, () => "the Retry control never bound the host");
            Assert.Equal(2, tries);
            Assert.Equal(Visibility.Collapsed, retry.Visibility);
            using var host = (AttemptHost)model.Runs.Host!;
        });
    }

    /// <summary>
    /// Ruling 159(2): a window with no workspace client (a degraded start, the daemon unavailable) refuses Send with its
    /// cause through the same refusal surface, offers no Retry (none can help), and never runs a turn directly.
    /// </summary>
    [Fact]
    public void WithNoWorkspaceClient_SendIsRefusedWithItsCause_NoRetryIsOffered_AndNoTurnRuns()
    {
        Pump(async (window, model, document) =>
        {
            var send = ButtonIn(document.Composer, b => Equals(b.Content, "Send"));
            await UntilAsync(window, () => !send.IsEnabled,
                () => "Send stayed enabled in a window with no workspace client: its turn would run unrecorded");

            Assert.Equal(
                "Runs cannot be hosted in this window: the workspace service is not connected, so a turn would not be recorded. Reopen the workspace once the service runs.",
                AutomationProperties.GetHelpText(send));
            Assert.Null(FindButton(document.Composer, b => AutomationProperties.GetName(b) == "Retry hosting runs" && b.IsVisible));

            document.Composer.Draft.SetFreeFormText("say ok");
            Assert.Null(document.Composer.Send());
            await Task.Delay(300);
            Assert.False(_fixture.RunOpened, "a window with no workspace client ran a turn directly");
            Assert.Empty(model.Thread.Current.Turns);
        });
    }

    private static Button ButtonIn(DependencyObject root, Func<Button, bool> match) =>
        FindButton(root, match) ?? throw new InvalidOperationException("no such button in the composer");

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

    /// <summary>Ruling 159(3): the lock is the holding process's own handle, so a holder that died frees it for the next open.</summary>
    [Fact]
    public async Task ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LockFile)!);
        using var holder = Process.Start(new ProcessStartInfo("powershell.exe",
            $"-NoProfile -Command \"$f=[IO.File]::Open('{LockFile}','OpenOrCreate','ReadWrite','None'); Start-Sleep 120\"")
        { UseShellExecute = false, CreateNoWindow = true })!;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        AgentPlaneException? refused = null;
        while (refused is null && DateTime.UtcNow < deadline)
        {
            try
            {
                (await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None)).Dispose();
                await Task.Delay(100);
            }
            catch (AgentPlaneException error)
            {
                refused = error;
            }
        }

        Assert.Equal(AttemptErrorCodes.HostNotSole, refused?.Code);
        holder.Kill(entireProcessTree: true);
        await holder.WaitForExitAsync();

        using var opened = await AttemptHost.OpenAsync(new BoundaryDispatcher(_fixture.Store), LockFile, CancellationToken.None);
        Assert.NotNull(opened);
    }

    private void Pump(Func<Window, NotebookModel, SessionDocumentSurface, Task> body) =>
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, _) =>
            {
                Directory.CreateDirectory(SessionPaths.SessionDirectory(_fixture.Root, NotebookFixture.SessionId));
                using var model = _fixture.NewModel();
                using var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
                window.Content = document;
                _fixture.ConfigureStub(document);
                await body(window, model, document);
                window.Content = null;
            });

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
}
