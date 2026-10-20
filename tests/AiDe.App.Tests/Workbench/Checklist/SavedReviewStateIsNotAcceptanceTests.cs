using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Checklist;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Ipc;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.App.Tests.Checklist;

/// <summary>
/// WL-1b D3 (the design's test plan): saved review state is not acceptance. Dispositions recorded through the pane's
/// buttons survive a restart — the layout saved by the shell's own persistence, restored by a new shell over the same
/// store, the notebook revived as <c>MainWindow.AttachWorkspace</c> revives it — with the same states, no
/// <c>work</c> <c>accepted</c> row, and the same gap line. With the Leader's condition (seam
/// <c>req-01M5B8W7NJRW7A1X4G21J2QKPT</c>): a checklist restored while another notebook is focused shows its own
/// journey, and while its own notebook is not open it says so, names the journey, and binds nothing else.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SavedReviewStateIsNotAcceptanceTests : IDisposable
{
    private const string Mine = NotebookFixture.SessionId;

    private const string Other = "20261019T070000Z-other1";

    private const string Deferred = "The plan names every risk it accepts";

    private const string Undecided = "The rollback is rehearsed";

    private static readonly AccountRef[] Accounts = [new("anthropic", "max")];

    private readonly ChecklistHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private string Root => _harness.Fixture.Root;

    private string DataDirectory => Path.Combine(Root, "shell-state");

    [Fact]
    public Task DeferredAndNeedsDecision_SurviveARestart_WithNoAcceptedRow_AndTheSameGapLine() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            IReadOnlyList<string> rows;
            string gap;
            using (var today = new WorkbenchShell(client, DataDirectory))
            {
                var perspectives = ChecklistHarness.Compose(today, region);
                perspectives.Activate(PerspectiveSet.Factory, "test");
                await _harness.OpenNotebookAsync(window, today, Created(Mine, "Plan the release"));
                var surface = await OpenChecklistAsync(window, today, perspectives);
                await ProposeAsync(window, surface, Deferred);
                await ProposeAsync(window, surface, Undecided);
                await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.Defer}: {Deferred}");
                await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.MarkNeedsDecision}: {Undecided}");

                rows = ChecklistHarness.RowNames(surface);
                gap = surface.AcceptanceLine;
                Assert.Equal(
                    [$"{Deferred}. {ChecklistCopy.EvidenceMissing} {ChecklistCopy.Deferred}", $"{Undecided}. {ChecklistCopy.EvidenceMissing} {ChecklistCopy.NeedsDecision}"],
                    rows);
                Assert.StartsWith("Blocked — ", gap, StringComparison.Ordinal);
                await SaveAndCloseAsync(window, today, region);
            }

            // The restored checklist is on screen before its notebook is revived: it says so, naming its journey.
            using var restarted = Restart(client, region);
            var journey = await JourneyOf(client, Mine, "Plan the release");
            var early = await ChecklistHarness.ChecklistIn(window, restarted.Factory, ChecklistSurface.SurfaceIdFor(Mine));
            Assert.Equal(ChecklistCopy.NotOpen(journey), early.AcceptanceLine);

            // The revive reopens the notebook, and the checklist rebinds to it with the states it had.
            Assert.Equal([Mine], restarted.ReviveRestoredSessionDocuments(Root).Select(c => c.SessionId));
            var restored = await ChecklistHarness.ChecklistIn(window, restarted.Factory, ChecklistSurface.SurfaceIdFor(Mine));

            Assert.Equal(rows, ChecklistHarness.RowNames(restored));
            Assert.Equal(gap, restored.AcceptanceLine);

            // No disposition row on the work at all, so none says accepted; the fold agrees.
            using (var reader = _harness.Fixture.Store.BeginRead())
            {
                Assert.Empty(reader.ReadDispositions("work", journey));
            }

            Assert.Equal(WorkClosureState.NotAccepted, (await Evaluate(client, Mine, "Plan the release")).Closure.State);
            region.Content = null;
        });

    [Fact]
    public Task RestoredWithAnotherNotebookFocused_TheChecklistNamesItsOwnJourney_AndBindsNothingElse() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            var mine = Created(Mine, "Plan the release");
            using (var today = new WorkbenchShell(client, DataDirectory))
            {
                var perspectives = ChecklistHarness.Compose(today, region);
                perspectives.Activate(PerspectiveSet.Factory, "test");
                await _harness.OpenNotebookAsync(window, today, Created(Other, "Write the notes"));
                await _harness.OpenNotebookAsync(window, today, mine);
                var surface = await OpenChecklistAsync(window, today, perspectives);
                await ProposeAsync(window, surface, Deferred);
                await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.Defer}: {Deferred}");

                // The user closes this journey's notebook and keeps its checklist and the other notebook open.
                Assert.True(today.Factory.Service.Apply(new LayoutOperation.CloseSurface(SessionDocumentSurface.SurfaceIdFor(Mine))).Applied);
                await SaveAndCloseAsync(window, today, region);
            }

            using var restarted = Restart(client, region);
            Assert.Equal([Other], restarted.ReviveRestoredSessionDocuments(Root).Select(c => c.SessionId));
            Assert.True(restarted.Factory.Service.Apply(new LayoutOperation.ActivateSurface(SessionDocumentSurface.SurfaceIdFor(Other))).Applied);
            var journey = await JourneyOf(client, Mine, "Plan the release");

            // Its notebook is not open: it names its own journey and binds nothing — no rows, no closing actions.
            var restored = await ChecklistHarness.ChecklistIn(window, restarted.Factory, ChecklistSurface.SurfaceIdFor(Mine));
            Assert.Equal(ChecklistCopy.NotOpen(journey), restored.AcceptanceLine);
            Assert.Empty(ChecklistHarness.RowNames(restored));
            Assert.DoesNotContain(ChecklistHarness.Buttons(restored), b => b.IsVisible && Equals(b.Content, ChecklistCopy.AcceptDeliverable));

            // Its notebook reopens, then the other notebook takes focus again: it still shows its own journey's rows.
            restarted.OpenSessionDocument(mine);
            Assert.True(restarted.Factory.Service.Apply(new LayoutOperation.ActivateSurface(SessionDocumentSurface.SurfaceIdFor(Other))).Applied);
            var rebound = await ChecklistHarness.ChecklistIn(window, restarted.Factory, ChecklistSurface.SurfaceIdFor(Mine));
            await rebound.RefreshAsync();
            await ChecklistHarness.SettledAsync(window, rebound);
            Assert.Equal([$"{Deferred}. {ChecklistCopy.EvidenceMissing} {ChecklistCopy.Deferred}"], ChecklistHarness.RowNames(rebound));
            Assert.Empty((await Evaluate(client, Other, "Write the notes")).Evaluation.Obligations);
            region.Content = null;
        });

    /// <summary>
    /// M6 (T26 n3, option a): the restored checklist is already visible saying its notebook is not open (CHK10), and the
    /// user reopens the notebook the way File → Recent sessions does (<c>MainWindow.ReopenSessionAsync</c>: the
    /// session's config loaded from its workspace, then the shell's <see cref="WorkbenchShell.OpenSessionDocument"/>).
    /// Nothing else happens: no tab switch, no perspective switch, no refresh call. The checklist the user is looking at
    /// leaves CHK10 and shows its journey's obligations. What refreshes it is the reopen's own render: it re-parents every
    /// pane it keeps, so the visible checklist gets a visibility edge and re-reads its notebook. No rebuild is needed.
    /// </summary>
    [Fact]
    public Task AVisibleChecklistSayingItsNotebookIsNotOpen_ShowsItsObligations_WhenTheNotebookIsReopenedFromRecentSessions() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            using (var today = new WorkbenchShell(client, DataDirectory))
            {
                var perspectives = ChecklistHarness.Compose(today, region);
                perspectives.Activate(PerspectiveSet.Factory, "test");
                await _harness.OpenNotebookAsync(window, today, Created(Mine, "Plan the release"));
                var surface = await OpenChecklistAsync(window, today, perspectives);
                await ProposeAsync(window, surface, Deferred);
                Assert.True(today.Factory.Service.Apply(new LayoutOperation.CloseSurface(SessionDocumentSurface.SurfaceIdFor(Mine))).Applied);
                await SaveAndCloseAsync(window, today, region);
            }

            using var restarted = Restart(client, region);
            Assert.Empty(restarted.ReviveRestoredSessionDocuments(Root));
            var journey = await JourneyOf(client, Mine, "Plan the release");
            var shown = await ChecklistHarness.ChecklistIn(window, restarted.Factory, ChecklistSurface.SurfaceIdFor(Mine));
            Assert.True(shown.IsVisible, "the restored checklist is not visible, so this is not the case the Invalidate serves");
            Assert.Equal(ChecklistCopy.NotOpen(journey), shown.AcceptanceLine);

            restarted.OpenSessionDocument(new SessionConfigStore(Root, Mine).Load());

            var visible = await ChecklistHarness.ChecklistIn(window, restarted.Factory, ChecklistSurface.SurfaceIdFor(Mine));
            Assert.True(visible.IsVisible, "the reopen hid the checklist, so the user would not be looking at it");
            Assert.NotEqual(ChecklistCopy.NotOpen(journey), visible.AcceptanceLine);
            Assert.Equal([$"{Deferred}. {ChecklistCopy.EvidenceMissing} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(visible));
            region.Content = null;
        });

    /// <summary>The session as File → New Session creates it: its <c>session.json</c> on disk, so a restart can revive it.</summary>
    private SessionConfig Created(string sessionId, string title) =>
        new SessionConfigStore(Root, sessionId).Create(title, Root, Accounts, Accounts[0], DateTimeOffset.UnixEpoch);

    /// <summary>A new shell over the same store and data directory: its constructor restores each host's saved slot.</summary>
    private WorkbenchShell Restart(WorkspaceClient client, ContentControl region)
    {
        var shell = new WorkbenchShell(client, DataDirectory);
        ChecklistHarness.Compose(shell, region).Activate(PerspectiveSet.Factory, "test");
        return shell;
    }

    private static async Task<ChecklistSurface> OpenChecklistAsync(Window window, WorkbenchShell shell, PerspectiveShell perspectives)
    {
        await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);
        return await ChecklistHarness.ChecklistIn(window, shell.Factory, ChecklistSurface.SurfaceIdFor(Mine));
    }

    private static async Task ProposeAsync(Window window, ChecklistSurface surface, string obligation)
    {
        await ChecklistHarness.TypeObligationAsync(window, surface, obligation);
        await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AddObligation);
    }

    /// <summary>Saves the Factory slot now, through the shell's own persistence, and closes the window's content.</summary>
    private static async Task SaveAndCloseAsync(Window window, WorkbenchShell shell, ContentControl region)
    {
        shell.Factory.Adapter.Render();
        await ChecklistHarness.SettleAsync(window);
        Assert.NotNull(shell.Factory.Persistence);
        shell.Factory.Persistence!.SaveNow();
        region.Content = null;
    }

    private static async Task<string> JourneyOf(WorkspaceClient client, string sessionId, string title) =>
        (await client.OpenNotebookAsync(sessionId, title, CancellationToken.None)).JourneyId!;

    private static async Task<ChecklistEvaluateResult> Evaluate(WorkspaceClient client, string sessionId, string title) =>
        await client.EvaluateChecklistAsync(new ChecklistEvaluateRequest(await JourneyOf(client, sessionId, title), sessionId), CancellationToken.None);
}
