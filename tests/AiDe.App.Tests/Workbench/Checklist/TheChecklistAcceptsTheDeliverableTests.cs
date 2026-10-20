using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Automation;
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
/// WL-1b D2 (the design's test plan, extended by the Owner): link the completed run, and the obligation is satisfied;
/// <i>Accept deliverable</i> while the intent is unaccepted is refused and the refusal is read out; accept the intent,
/// then the deliverable, and the Accepted line names the intent revision and claims nothing else (Ruling 179). The
/// live projection sha is the attempt's own envelope's (R1; Ruling 180 (5)): the evidence still reads current after
/// the composer clears and after a re-prepare that changes the live compile, and a call with no live sha reads
/// NotRecorded, never Satisfied (the Owner's null-sha condition).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class TheChecklistAcceptsTheDeliverableTests : IDisposable
{
    private const string SessionId = NotebookFixture.SessionId;

    private const string Words = "Draft the release plan";

    private const string Obligation = "The plan names every risk it accepts";

    private readonly ChecklistHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public Task LinkTheRun_Satisfied_AcceptRefusedNamingTheIntent_ThenIntentAccepted_ThenTheAcceptedLineNamesTheRevisionOnly() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            var (shell, document, surface, key) = await LinkedAsync(window, region, client);
            using var _ = shell;

            Assert.Equal([$"{Obligation}. {ChecklistCopy.Satisfied(key, 1)} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(surface));
            Assert.Equal("Blocked — intent revision 1 is not accepted.", surface.AcceptanceLine);

            // Accept deliverable is enabled while blocked: the store refuses, and the refusal is read out politely.
            await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AcceptDeliverable);
            Assert.Equal($"{ChecklistCopy.RefusedLead} Blocked — intent revision 1 is not accepted.", surface.Announcement);
            var live = Texts(surface).Single(t => AutomationProperties.GetAutomationId(t) == "checklist.announcement");
            Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(live));
            Assert.Equal(WorkClosureState.NotAccepted, (await EvaluateAsync(client)).Closure.State);

            // Accept the intent, then the deliverable.
            await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AcceptIntent(1));
            Assert.Equal(ChecklistCopy.Ready, surface.AcceptanceLine);
            Assert.Contains(ChecklistCopy.IntentAccepted(1), ChecklistHarness.AllText(surface));
            await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AcceptDeliverable);

            Assert.Equal("Accepted: the declared deliverable at intent revision 1. This records acceptance of the deliverable only. It does not say the work is implemented or merged.",
                surface.AcceptanceLine);
            Assert.Equal(WorkClosureState.Accepted, (await EvaluateAsync(client)).Closure.State);

            // Ruling 179: no string the surface shows says "implemented" or "merged", except the one negating sentence.
            foreach (var text in ChecklistHarness.AllText(surface))
            {
                var rest = text.Replace(ChecklistCopy.AcceptedNegation, string.Empty, StringComparison.Ordinal);
                Assert.DoesNotContain("implemented", rest, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("merged", rest, StringComparison.OrdinalIgnoreCase);
            }

            region.Content = null;
        });

    [Fact]
    public Task TheEvidenceStaysCurrent_AfterTheComposerClears_AndAfterARePrepareThatChangesTheLiveCompile() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            var (shell, document, surface, key) = await LinkedAsync(window, region, client);
            using var _ = shell;
            var ran = (await client.ReadAttemptAsync(key, CancellationToken.None))!.Attempt.ProjectionSha;

            // The composer cleared after the send: its live view is the next, empty block's, not the attempt's.
            Assert.Equal(string.Empty, document.Composer.Draft.SourceText);
            await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AcceptIntent(1));
            Assert.Equal([$"{Obligation}. {ChecklistCopy.Satisfied(key, 1)} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(surface));
            Assert.Equal(ChecklistCopy.Ready, surface.AcceptanceLine);

            // A re-prepare with the draft unchanged (the revision's own words) and the task class changed: the class is in
            // the projection's domain, so the live compile is a new projection over the same revision.
            _harness.ConfigureStub(document, "investigate");
            document.Composer.Draft.SetFreeFormText(Words);
            await document.Composer.PrepareTurnAsync();
            await ChecklistHarness.SettleAsync(window);
            Assert.NotEqual(ran, document.Composer.Gate.LastProjection?.ProjectionSha);
            Assert.Equal(1, document.Notebook.Latest?.RevisionNo);

            await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.Acknowledge}: {Obligation}");
            Assert.Equal([$"{Obligation}. {ChecklistCopy.Satisfied(key, 1)} {ChecklistCopy.Acknowledged}"], ChecklistHarness.RowNames(surface));
            Assert.Equal(ChecklistCopy.Ready, surface.AcceptanceLine);
            region.Content = null;
        });

    [Fact]
    public Task WithNoLiveSha_TheEvidenceReadsNotRecorded_WithItsGap_NeverSatisfied() =>
        _harness.RunAsync(async (window, region, client) =>
        {
            var (shell, _, linked, key) = await LinkedAsync(window, region, client);
            Assert.Equal([$"{Obligation}. {ChecklistCopy.Satisfied(key, 1)} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(linked));
            region.Content = null;
            shell.Dispose();

            // The window closes, and the session's compile history is lost with its directory: the reopened notebook has
            // no envelope store, so the attempt's own envelope cannot be read and the call carries no live sha.
            Directory.Delete(SessionPaths.SessionDirectory(_harness.Fixture.Root, SessionId), recursive: true);
            using var reopened = new WorkbenchShell(client);
            var perspectives = ChecklistHarness.Compose(reopened, region);
            perspectives.Activate(PerspectiveSet.Factory, "test");
            reopened.OpenSessionDocument(_harness.Config(SessionId, "Plan the release"));
            await ChecklistHarness.SettleAsync(window);
            Assert.Null(ChecklistHarness.DocumentOf(reopened, SessionId).Envelopes);

            await ChecklistHarness.OpenChecklistFromViewAsync(window, reopened, perspectives);
            var surface = await ChecklistHarness.ChecklistIn(window, reopened.Factory, ChecklistSurface.SurfaceIdFor(SessionId));
            Assert.Equal([$"{Obligation}. {ChecklistCopy.NotRecorded} {ChecklistCopy.NoDisposition}"], ChecklistHarness.RowNames(surface));
            Assert.Equal($"Blocked — whether the evidence for “{Obligation}” is current is not recorded.", surface.AcceptanceLine);
            region.Content = null;
        });

    /// <summary>A notebook with one completed run, its checklist opened from View, one obligation proposed and the run linked to it.</summary>
    private async Task<(WorkbenchShell Shell, SessionDocumentSurface Document, ChecklistSurface Surface, string Key)> LinkedAsync(
        Window window, ContentControl region, WorkspaceClient client)
    {
        var shell = new WorkbenchShell(client);
        var perspectives = ChecklistHarness.Compose(shell, region);
        perspectives.Activate(PerspectiveSet.Factory, "test");
        var document = await _harness.OpenNotebookAsync(window, shell, _harness.Config(SessionId, "Plan the release"));
        var key = await _harness.RunToCompletionAsync(window, document, Words);

        await ChecklistHarness.OpenChecklistFromViewAsync(window, shell, perspectives);
        var surface = await ChecklistHarness.ChecklistIn(window, shell.Factory, ChecklistSurface.SurfaceIdFor(SessionId));
        await ChecklistHarness.TypeObligationAsync(window, surface, Obligation);
        await ChecklistHarness.PressAsync(window, surface, ChecklistCopy.AddObligation);
        await ChecklistHarness.PressAsync(window, surface, $"{ChecklistCopy.LinkLatestRun}: {Obligation}");
        return (shell, document, surface, key);
    }

    private static async Task<ChecklistEvaluateResult> EvaluateAsync(WorkspaceClient client)
    {
        var journey = (await client.OpenNotebookAsync(SessionId, "Plan the release", CancellationToken.None)).JourneyId!;
        return await client.EvaluateChecklistAsync(new ChecklistEvaluateRequest(journey, SessionId), CancellationToken.None);
    }

    private static IEnumerable<TextBlock> Texts(ChecklistSurface surface) => Shell.ComposedCoding.Visuals<TextBlock>(surface);
}
