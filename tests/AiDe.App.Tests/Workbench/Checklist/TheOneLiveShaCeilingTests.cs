using System.Runtime.Versioning;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Checklist;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Checklist;

/// <summary>
/// The one-live-sha <c>simplify:</c> in <c>ChecklistSurface.EvaluateAsync</c> (T26 n1), pinned. The Core request carries
/// one live projection sha for every link, and the surface passes the newest head-revision run's own envelope's. Two runs
/// of one revision with different projections (the task class is in the projection's domain) are the case the ceiling
/// names: an obligation with both linked reads satisfied by the newest run, and an obligation linked only to the older
/// run reads stale although that run is from the current draft. The README states the ceiling; this test fails when it
/// moves either way.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class TheOneLiveShaCeilingTests : IDisposable
{
    private const string SessionId = NotebookFixture.SessionId;

    private const string Words = "Draft the release plan";

    private const string Obligation = "The plan names every risk it accepts";

    private readonly ChecklistHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public Task TwoLinkedRunsAtTheHeadRevision_ReadSatisfiedByTheNewest_AndAnOlderRunAloneReadsStale() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            using var shell = new WorkbenchShell(client);
            var perspectives = ChecklistHarness.Compose(shell, region);
            perspectives.Activate(PerspectiveSet.Factory, "test");
            var document = await _harness.OpenNotebookAsync(window, shell, _harness.Config(SessionId, "Plan the release"));
            var older = await _harness.RunToCompletionAsync(window, document, Words);

            await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);
            var surface = await ChecklistHarness.ChecklistIn(window, shell.Factory, ChecklistSurface.SurfaceIdFor(SessionId));
            await ChecklistHarness.TypeObligationAsync(window, surface, Obligation);
            await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AddObligation);
            await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.LinkLatestRun}: {Obligation}");
            Assert.Equal([$"{Obligation}. {ChecklistCopy.Satisfied(older, 1)} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(surface));

            // A second run of the same revision under another task class: a new projection, still revision 1.
            var newer = await _harness.RunToCompletionAsync(window, document, Words, taskClass: "investigate");
            Assert.Equal(1, document.Notebook.Latest?.RevisionNo);
            var ran = (await client.ReadAttemptAsync(older, CancellationToken.None))!.Attempt.ProjectionSha;
            Assert.NotEqual(ran, (await client.ReadAttemptAsync(newer, CancellationToken.None))!.Attempt.ProjectionSha);

            // The ceiling: the one live sha is the newer run's, so the older run, linked alone, reads stale.
            await surface.RefreshAsync();
            await ChecklistHarness.SettledAsync(window, surface);
            Assert.Equal([$"{Obligation}. {ChecklistCopy.Stale} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(surface));

            // Both linked: satisfied by the newest run, and nothing but the intent blocks acceptance.
            await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.LinkLatestRun}: {Obligation}");
            Assert.Equal([$"{Obligation}. {ChecklistCopy.Satisfied(newer, 1)} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(surface));
            Assert.Equal("Blocked — intent revision 1 is not accepted.", surface.AcceptanceLine);
            region.Content = null;
        });
}
