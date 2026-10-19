using System.Runtime.Versioning;
using System.Windows.Automation;
using AiDe.App.Tests.Records;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Checklist;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Checklist;

/// <summary>
/// WL-1b D1, the demo path headless (the WL-1b design's test plan; AC-W03c): the checklist kind opens from View in
/// Factory and in Understand, bound to the focused notebook's journey; empty, then a proposed obligation reads
/// "evidence missing"; acknowledging it reads "Acknowledged — not satisfied" and acceptance stays blocked. Every step is
/// the user's gesture through UI Automation on the product's shell.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class TheChecklistDemoPathTests : IDisposable
{
    private const string SessionId = NotebookFixture.SessionId;

    private const string Obligation = "The plan names every risk it accepts";

    private readonly ChecklistHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public Task FromViewInFactory_EmptyThenProposed_ReadsEvidenceMissing_ThenAcknowledged_StillBlocked() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            using var shell = new WorkbenchShell(client);
            var perspectives = ChecklistHarness.Compose(shell, region);
            perspectives.Activate(PerspectiveSet.Factory, "test");
            var document = await _harness.OpenNotebookAsync(window, shell, _harness.Config(SessionId, "Plan the release"));
            await _harness.RunToCompletionAsync(window, document, "Draft the release plan");

            var said = await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);

            // One checklist for the focused notebook's journey, in the Right zone, named for its notebook.
            var checklistId = ChecklistSurface.SurfaceIdFor(SessionId);
            var zones = shell.Factory.Service.Zones;
            var opened = Assert.Single(zones.AllSurfaces(), s => s.Kind == ChecklistSurface.Kind);
            Assert.Equal(checklistId, opened.SurfaceId);
            Assert.Equal(ChecklistSurface.CaptionFor("Plan the release"), opened.Title);
            Assert.Equal(ZoneId.Right, zones.FindZoneOf(checklistId));
            Assert.StartsWith("Checklist opened", said, StringComparison.Ordinal);
            var surface = await ChecklistHarness.ChecklistIn(window, shell.Factory, checklistId);
            Assert.Equal(ChecklistSurface.CaptionFor("Plan the release"), AutomationProperties.GetName(surface));

            // Empty: the teaching line, and acceptance is blocked by the empty checklist.
            Assert.Contains(ChecklistCopy.Empty, ChecklistHarness.AllText(surface));
            Assert.Equal("Blocked — no obligations are declared for this work.", surface.AcceptanceLine);
            Assert.Empty(ChecklistHarness.RowNames(surface));

            // Propose: the row reads evidence missing, with its human state as a separate label.
            await ChecklistHarness.TypeObligationAsync(window, surface, Obligation);
            await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AddObligation);
            Assert.Equal([$"{Obligation}. {ChecklistCopy.EvidenceMissing} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(surface));
            Assert.Contains(ChecklistCopy.Mandatory, ChecklistHarness.AllText(surface));
            Assert.Equal($"Blocked — “{Obligation}” has no current evidence.", surface.AcceptanceLine);

            // Acknowledge: acknowledged is not satisfied, so the evidence label and the block are unchanged.
            await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.Acknowledge}: {Obligation}");
            Assert.Equal([$"{Obligation}. {ChecklistCopy.EvidenceMissing} {ChecklistCopy.Acknowledged}"], ChecklistHarness.RowNames(surface));
            Assert.StartsWith("Acknowledged — not satisfied", ChecklistCopy.Acknowledged, StringComparison.Ordinal);
            Assert.Equal($"Blocked — “{Obligation}” has no current evidence.", surface.AcceptanceLine);
            region.Content = null;
        });

    [Fact]
    public Task FromViewInUnderstand_OpensInUnderstandsRightZone_BoundToTheFocusedNotebooksJourney() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            using var shell = new WorkbenchShell(client);
            var perspectives = ChecklistHarness.Compose(shell, region);
            perspectives.Activate(PerspectiveSet.Factory, "test");
            await _harness.OpenNotebookAsync(window, shell, _harness.Config(SessionId, "Plan the release"));

            perspectives.Activate(PerspectiveSet.Understand, "test");
            var said = await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);

            var checklistId = ChecklistSurface.SurfaceIdFor(SessionId);
            Assert.Equal(ZoneId.Right, shell.Understand.Service.Zones.FindZoneOf(checklistId));
            Assert.DoesNotContain(shell.Factory.Service.Zones.AllSurfaces(), s => s.Kind == ChecklistSurface.Kind);
            Assert.StartsWith("Checklist opened", said, StringComparison.Ordinal);
            var surface = await ChecklistHarness.ChecklistIn(window, shell.Understand, checklistId);
            Assert.Equal("Blocked — no obligations are declared for this work.", surface.AcceptanceLine);
            Assert.Contains(ChecklistCopy.Empty, ChecklistHarness.AllText(surface));
            region.Content = null;
        });

    [Fact]
    public Task WithNoNotebookOpen_TheOpenerRefuses_AndAddsNoChecklist() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            using var shell = new WorkbenchShell(client);
            var perspectives = ChecklistHarness.Compose(shell, region);
            perspectives.Activate(PerspectiveSet.Understand, "test");

            var said = await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);

            Assert.Equal(ChecklistCopy.NoNotebook, said);
            Assert.DoesNotContain(shell.Hosts.SelectMany(h => h.Service.Zones.AllSurfaces()), s => s.Kind == ChecklistSurface.Kind);
            region.Content = null;
        });
}
