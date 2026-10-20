using System.Runtime.Versioning;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.App.Tests.Checklist;

/// <summary>
/// The Owner's condition on the placement key (T26 n2): the surface id <c>checklist:&lt;sessionId&gt;</c> only places the
/// pane; the stored checklist id is the journey (Ruling 176), resolved one-to-one from the session's notebook
/// (<c>owner_journey_id</c>). So the service, over the real client and daemon, refuses every call whose journey is not
/// the one that owns the named notebook, and writes nothing to either checklist.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class TheServiceRefusesAJourneyThatIsNotTheNotebooksTests : IDisposable
{
    private const string Mine = "t26-refuse-mine";

    private const string Other = "t26-refuse-other";

    private readonly ChecklistHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public Task EveryChecklistCall_WithAnotherNotebooksJourney_IsRefused_AndNothingIsWritten() =>
        _harness.RunAsync(async (_, _, client) =>
        {
            var mine = (await client.OpenNotebookAsync(Mine, "Plan the release", CancellationToken.None)).JourneyId!;
            var other = (await client.OpenNotebookAsync(Other, "Write the notes", CancellationToken.None)).JourneyId!;
            Assert.NotEqual(mine, other);

            // The notebook's own journey is served: one obligation lands on its checklist.
            await client.SetObligationsAsync(new ObligationsSetRequest(mine, Mine, [new ObligationInput(null, "The plan names every risk", true)]), CancellationToken.None);
            var obligation = Assert.Single((await client.EvaluateChecklistAsync(new ChecklistEvaluateRequest(mine, Mine), CancellationToken.None)).Evaluation.Obligations).ObligationId;

            // The same notebook with the other journey: every operation is refused before it reads or writes.
            await Refused(() => client.EvaluateChecklistAsync(new ChecklistEvaluateRequest(other, Mine), CancellationToken.None));
            await Refused(() => client.SetObligationsAsync(new ObligationsSetRequest(other, Mine, [new ObligationInput(null, "Smuggled", true)]), CancellationToken.None));
            await Refused(() => client.RecordDispositionAsync(new DispositionRecordRequest(other, Mine, obligation, "deferred"), CancellationToken.None));
            await Refused(() => client.RecordDispositionAsync(new DispositionRecordRequest(other, Mine, null, "intent-accepted"), CancellationToken.None));
            await Refused(() => client.LinkEvidenceAsync(new EvidenceLinkRequest(other, Mine, obligation, "no-such-run"), CancellationToken.None));
            await Refused(() => client.AcceptWorkAsync(new WorkAcceptRequest(other, Mine), CancellationToken.None));

            // Neither checklist moved: the other journey's is still empty, and the notebook's own has no disposition.
            Assert.Empty((await client.EvaluateChecklistAsync(new ChecklistEvaluateRequest(other, Other), CancellationToken.None)).Evaluation.Obligations);
            var own = Assert.Single((await client.EvaluateChecklistAsync(new ChecklistEvaluateRequest(mine, Mine), CancellationToken.None)).Evaluation.Obligations);
            Assert.Equal(obligation, own.ObligationId);
            Assert.Equal(HumanState.None, own.Human);
        });

    private static async Task Refused(Func<Task> call)
    {
        var refused = await Assert.ThrowsAsync<IpcRequestException>(call);
        Assert.Equal(StoreErrorCodes.WorkNotAWorkJourney, refused.Code);
    }
}
