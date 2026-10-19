using System.Windows;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Records;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// ADR-0045 §4 and AC-D01a: the active attempt's cancellation and queue live with the notebook model, not the surface,
/// so closing the pane mid-run leaves the run running ("Pane hidden. The run continues.") and the surface reopened
/// over the same model sees the run end.
/// </summary>
/// <remarks>
/// The run is real: the product's composition root spawns <see cref="Conductor.StubAcpAdapter"/>, which holds the
/// prompt until the release file appears — the run is in flight at the far end while the pane is closed.
/// </remarks>
public sealed class TheRunOutlivesItsPaneTests : IDisposable
{
    private readonly NotebookFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public void ClosingThePaneMidRun_LeavesTheRunRunning_AndTheReopenedSurfaceSeesItEnd()
    {
        var model = _fixture.NewModel();
        var announcer = new RecordingAnnouncer();

        Sta.Pump(
            create: () => new SessionDocumentSurface(model, null, announcer).Unhosted(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (window, first) =>
            {
                _fixture.ConfigureStub(first);
                first.Composer.Draft.SetFreeFormText("first");
                Assert.NotNull(first.Composer.Send());
                await UntilAsync(window, () => _fixture.RunOpened, "the run never opened its ACP session (is node on PATH?)");

                // Close the pane while the stub holds the prompt.
                window.Content = null;
                first.Dispose();
                await Task.Delay(500);
                await PumpAsync(window);

                var turn = Assert.Single(model.Thread.Current.Turns);
                Assert.True(turn.State == TurnState.Running, $"closing the pane ended the run: it reads {turn.State}; events [{string.Join(", ", turn.Events.Select(e => e.Text))}]");
                Assert.Contains(announcer.Announcements, a => a.Text == SessionDocumentSurface.PaneHiddenCopy);
                Assert.False(model.IsDisposed);

                // Reopen over the same model, then let the run answer: the new surface sees it end.
                var second = new SessionDocumentSurface(model, null, announcer).Unhosted();
                window.Content = second;
                _fixture.ReleaseRun();
                await UntilAsync(window, () => model.Thread.Current.Turns[0].State == TurnState.Answered,
                    $"the run never answered after the reopen; it reads {model.Thread.Current.Turns[0].State}");
                Assert.Same(model.Thread, second.ReadModel);
                Assert.Equal(TurnState.Answered, second.ReadModel.Current.Turns[0].State);

                window.Content = null;
                second.Dispose();
                model.Dispose();
            });
    }

    private static async Task PumpAsync(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            window.UpdateLayout();
        }
    }

    private static async Task UntilAsync(Window window, Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
            await PumpAsync(window);
        }

        Assert.True(condition(), what);
    }
}
