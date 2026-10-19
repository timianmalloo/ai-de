using System.Diagnostics;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Tests.Checklist;

/// <summary>
/// WL-1b checks B1-B3 (design Test plan): the checklist service over a real SQLite store with the v2 schema.
/// One writer transaction per write; every guarded case refuses with a stable code.
/// </summary>
public sealed class ChecklistOperationsTests : IDisposable
{
    private const string Actor = "shell:4242";
    private const string LiveSha = "sha-live";

    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private ChecklistService Service => new(_workspace.Store);

    /// <summary>Opens a work journey with its intent notebook at revision 1; returns the journey id.</summary>
    private string OpenWork(string notebookId = "nb-1", string body = "intent v1")
    {
        var notebooks = new NotebookService(_workspace.Store);
        var opened = notebooks.Open(new NotebookOpenRequest(notebookId, "Fix the login bug"));
        notebooks.SaveRevision(new NotebookSaveRequest(notebookId, body));
        return opened.JourneyId!;
    }

    /// <summary>Records a completed attempt of the notebook's revision, bound to <see cref="LiveSha"/>.</summary>
    private void Attempt(string key, string notebookId = "nb-1", int revision = 1, DispatchState terminal = DispatchState.ResponseComplete)
    {
        using var writer = _workspace.Store.BeginWrite();
        writer.AppendAttempt(new AttemptRow(key, "w", _workspace.Store.CoreEpoch, notebookId, revision, "s", 1, LiveSha));
        writer.AppendOutcome(new OutcomeRow(key, DispatchState.Dispatched));
        writer.AppendOutcome(new OutcomeRow(key, terminal));
        writer.Commit();
    }

    private string MandatoryObligation(string journeyId, string notebookId = "nb-1")
    {
        Service.SetObligations(new ObligationsSetRequest(journeyId, notebookId, [new ObligationInput(null, "Tests pass", true)]));
        using var reader = _workspace.Store.BeginRead();
        return reader.ReadObligations(journeyId, 1).Single().ObligationId;
    }

    private void AcceptIntent(string journeyId, string notebookId = "nb-1") =>
        Service.RecordDisposition(new DispositionRecordRequest(journeyId, notebookId, null, "intent-accepted"), Actor);

    private void Link(string journeyId, string obligationId, string key, string notebookId = "nb-1") =>
        Service.LinkEvidence(new EvidenceLinkRequest(journeyId, notebookId, obligationId, key, LiveSha), Actor);

    private WorkAcceptResult Accept(string journeyId, string notebookId = "nb-1") =>
        Service.Accept(new WorkAcceptRequest(journeyId, notebookId, LiveSha), Actor);

    private IReadOnlyList<DispositionRow> WorkRows(string journeyId)
    {
        using var reader = _workspace.Store.BeginRead();
        return reader.ReadDispositions("work", journeyId);
    }

    // ---- B1: accept only when eligible, in one transaction --------------------------------------

    [Fact]
    public void B1_AcceptWithAGap_IsRefusedWithEveryGapCode_AndWritesNoRow()
    {
        var journey = OpenWork();
        MandatoryObligation(journey);
        AcceptIntent(journey);

        var ex = Assert.Throws<WorkspaceStoreException>(() => Accept(journey));

        Assert.Equal(StoreErrorCodes.WorkAcceptanceBlocked, ex.ErrorCode);
        Assert.Contains(AcceptanceGapCodes.EvidenceMissing, ex.Message);
        Assert.Empty(WorkRows(journey));
    }

    [Fact]
    public void B1_AcceptWhenEligible_WritesOneRow_AndNamesTheIntentRevisionNotImplementation()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-1");
        Link(journey, obligation, "dk-1");
        AcceptIntent(journey);

        var result = Accept(journey);

        var row = Assert.Single(WorkRows(journey));
        Assert.Equal(("work", journey, "accepted", Actor), (row.SubjectKind, row.SubjectId, row.Kind, row.Actor));
        Assert.Equal("intent:nb-1@1", row.Baseline);
        Assert.Equal(WorkClosureState.Accepted, result.Closure.State);
        Assert.Equal(1, result.IntentRevisionNo);
        Assert.Contains("revision 1", result.Line);
        Assert.DoesNotContain("implemented", result.Line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("done", result.Line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("merged", result.Line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void B1_AcceptAgainAtTheSameBaseline_ReturnsTheSameRow_AndWritesNothing()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-1");
        Link(journey, obligation, "dk-1");
        AcceptIntent(journey);
        var first = Accept(journey);
        var seqAfterFirst = Assert.Single(WorkRows(journey)).IngressSeq;

        var second = Accept(journey);

        Assert.Equal(first, second);
        Assert.Equal(seqAfterFirst, Assert.Single(WorkRows(journey)).IngressSeq);
    }

    [Fact]
    public void B1_AnEditAfterTheIntentWasAccepted_IsSeenByTheRefoldInsideAccept()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-1");
        Link(journey, obligation, "dk-1");
        AcceptIntent(journey);
        new NotebookService(_workspace.Store).SaveRevision(new NotebookSaveRequest("nb-1", "intent v2"));

        var ex = Assert.Throws<WorkspaceStoreException>(() => Accept(journey));

        Assert.Equal(StoreErrorCodes.WorkAcceptanceBlocked, ex.ErrorCode);
        Assert.Contains(AcceptanceGapCodes.IntentNotAccepted, ex.Message);
        Assert.Empty(WorkRows(journey));
    }

    [Fact]
    public void B1_TheBlockedPath_EmitsItsWorkAcceptActivity()
    {
        var seen = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == FoldSignal.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => seen.Add((string)a.GetTagItem("fold")!),
        };
        ActivitySource.AddActivityListener(listener);
        var journey = OpenWork();
        MandatoryObligation(journey);

        Assert.Throws<WorkspaceStoreException>(() => Accept(journey));

        Assert.Contains("work.accept", seen);
    }
}
