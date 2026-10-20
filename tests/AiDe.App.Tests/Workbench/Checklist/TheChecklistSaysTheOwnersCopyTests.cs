using System.IO;
using System.Runtime.Versioning;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Checklist;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Checklist;

/// <summary>
/// The Owner's copy confirmation (2026-10-20): CHK3 and CHK9 changed, and both are pinned here as the literal sentences
/// the user sees on the surface. Every other checklist test reads the <see cref="ChecklistCopy"/> member the surface
/// renders, so a copy change could never fail one of them.
/// </summary>
/// <remarks>The sentences are literals on purpose; deriving them from <see cref="ChecklistCopy"/> would make this test pass on any copy.</remarks>
[SupportedOSPlatform("windows")]
public sealed class TheChecklistSaysTheOwnersCopyTests : IDisposable
{
    private const string SessionId = NotebookFixture.SessionId;

    private const string Obligation = "The plan names every risk it accepts";

    private readonly ChecklistHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public Task CHK3_ALinkedRun_ReadsSatisfiedByRun_AtItsIntentRevision() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            using var shell = new WorkbenchShell(client);
            var perspectives = ChecklistHarness.Compose(shell, region);
            perspectives.Activate(PerspectiveSet.Factory, "test");
            var document = await _harness.OpenNotebookAsync(window, shell, _harness.Config(SessionId, "Plan the release"));
            var key = await _harness.RunToCompletionAsync(window, document, "Draft the release plan");

            await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);
            var surface = await ChecklistHarness.ChecklistIn(window, shell.Factory, ChecklistSurface.SurfaceIdFor(SessionId));
            await ChecklistHarness.TypeObligationAsync(window, surface, Obligation);
            await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AddObligation);
            await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.LinkLatestRun}: {Obligation}");

            Assert.Equal($"{Obligation}. Satisfied by run {key[..7]} (intent revision 1). Not reviewed.", Assert.Single(ChecklistHarness.RowNames(surface)));
        });

    [Fact]
    public async Task CHK9_ANotebookMadeBeforeWorkJourneys_SaysItHasNoWorkJourney()
    {
        using var legacy = _harness.LegacyStore(SessionId);
        await ChecklistHarness.RunAsync(legacy, async (window, region, client) =>
        {
            using var shell = new WorkbenchShell(client);
            var perspectives = ChecklistHarness.Compose(shell, region);
            perspectives.Activate(PerspectiveSet.Factory, "test");
            Directory.CreateDirectory(SessionPaths.SessionDirectory(_harness.Fixture.Root, SessionId));
            shell.OpenSessionDocument(_harness.Config(SessionId, "Plan the release"));
            await ChecklistHarness.SettleAsync(window);

            await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);
            var surface = await ChecklistHarness.ChecklistIn(window, shell.Factory, ChecklistSurface.SurfaceIdFor(SessionId));

            Assert.Contains(
                "This notebook has no work journey. It was made before work journeys existed. Obligations need a work journey.",
                ChecklistHarness.AllText(surface));
        });
    }
}
