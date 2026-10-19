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

    // ---- B2: evidence link guards ----------------------------------------------------------------

    private WorkspaceStoreException LinkRefusal(string journeyId, string obligationId, string key, string? liveSha = LiveSha) =>
        Assert.Throws<WorkspaceStoreException>(() =>
            Service.LinkEvidence(new EvidenceLinkRequest(journeyId, "nb-1", obligationId, key, liveSha), Actor));

    private IReadOnlyList<EvidenceLinkRow> Links(string obligationId)
    {
        using var reader = _workspace.Store.BeginRead();
        return reader.ReadEvidenceLinksForSubject("obligation", obligationId);
    }

    [Fact]
    public void B2_AnAttemptOfAnotherJourneysNotebook_IsRefusedNotThisJourney()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        OpenWork("nb-2");
        Attempt("dk-other", "nb-2");

        var ex = LinkRefusal(journey, obligation, "dk-other");

        Assert.Equal(StoreErrorCodes.WorkEvidenceNotEligible, ex.ErrorCode);
        Assert.StartsWith("not-this-journey", ex.Message);
        Assert.Empty(Links(obligation));
    }

    [Theory]
    [InlineData(DispatchState.Failed)]
    [InlineData(DispatchState.Interrupted)]
    [InlineData(DispatchState.Rejected)]
    public void B2_AnAttemptThatDidNotEndResponseComplete_IsRefusedNotComplete(DispatchState terminal)
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-bad", terminal: terminal);

        var ex = LinkRefusal(journey, obligation, "dk-bad");

        Assert.Equal(StoreErrorCodes.WorkEvidenceNotEligible, ex.ErrorCode);
        Assert.StartsWith("not-complete", ex.Message);
        Assert.Empty(Links(obligation));
    }

    [Fact]
    public void B2_AStillRunningAttempt_IsRefusedNotComplete()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        using (var writer = _workspace.Store.BeginWrite())
        {
            writer.AppendAttempt(new AttemptRow("dk-run", "w", _workspace.Store.CoreEpoch, "nb-1", 1, "s", 1, LiveSha));
            writer.AppendOutcome(new OutcomeRow("dk-run", DispatchState.Dispatched));
            writer.Commit();
        }

        var ex = LinkRefusal(journey, obligation, "dk-run");

        Assert.StartsWith("not-complete", ex.Message);
    }

    [Fact]
    public void B2_AStaleAttempt_IsRefusedNotCurrent()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-old");
        new NotebookService(_workspace.Store).SaveRevision(new NotebookSaveRequest("nb-1", "intent v2"));

        var ex = LinkRefusal(journey, obligation, "dk-old");

        Assert.Equal(StoreErrorCodes.WorkEvidenceNotEligible, ex.ErrorCode);
        Assert.StartsWith("not-current", ex.Message);
        Assert.Empty(Links(obligation));
    }

    [Fact]
    public void B2_WithNoLiveProjection_CurrentnessIsNotRecorded_AndTheLinkIsRefusedNotCurrent()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-1");

        var ex = LinkRefusal(journey, obligation, "dk-1", liveSha: null);

        Assert.StartsWith("not-current", ex.Message);
    }

    [Fact]
    public void B2_AGoodLink_StoresTheAttemptArtifactRef_AndTheAttemptFoldReadsHasEvidence()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-1");

        var written = Service.LinkEvidence(new EvidenceLinkRequest(journey, "nb-1", obligation, "dk-1", LiveSha), Actor).Link;

        var stored = Assert.Single(Links(obligation));
        Assert.Equal(AttemptStateFold.AttemptArtifactRef("dk-1"), stored.ArtifactRef);
        Assert.Equal(written.LinkId, stored.LinkId);
        Assert.Equal((AcceptanceFold.ObservedStatus, Actor, "intent:nb-1@1", 1),
            (stored.VerificationStatus, stored.Producer, stored.Baseline, stored.SubjectRevision));
        using var reader = _workspace.Store.BeginRead();
        var state = AttemptStateFold.Evaluate(new AttemptStateInput(
            "dk-1", [], reader.ReadOutcomes("dk-1"), [], reader.ReadEvidenceLinksForSubject("obligation", obligation)));
        Assert.True(state.HasEvidence);
    }

    [Fact]
    public void B2_LinkingTheSameAttemptTwice_ReturnsTheExistingLink_AndWritesNothing()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        Attempt("dk-1");
        var first = Service.LinkEvidence(new EvidenceLinkRequest(journey, "nb-1", obligation, "dk-1", LiveSha), Actor);

        var second = Service.LinkEvidence(new EvidenceLinkRequest(journey, "nb-1", obligation, "dk-1", LiveSha), Actor);

        Assert.Equal(first, second);
        Assert.Single(Links(obligation));
    }

    [Fact]
    public void B2_AnObligationOutsideTheChecklist_IsRefusedUnknown_OnEveryOperationThatNamesOne()
    {
        var journey = OpenWork();
        MandatoryObligation(journey);
        Attempt("dk-1");

        Assert.Equal(StoreErrorCodes.WorkObligationUnknown, LinkRefusal(journey, "obl-nope", "dk-1").ErrorCode);
        Assert.Equal(StoreErrorCodes.WorkObligationUnknown, Assert.Throws<WorkspaceStoreException>(() =>
            Service.RecordDisposition(new DispositionRecordRequest(journey, "nb-1", "obl-nope", "acknowledged"), Actor)).ErrorCode);
        Assert.Equal(StoreErrorCodes.WorkObligationUnknown, Assert.Throws<WorkspaceStoreException>(() =>
            Service.SetObligations(new ObligationsSetRequest(journey, "nb-1", [new ObligationInput("obl-nope", "x", true)]))).ErrorCode);
    }

    [Fact]
    public void B2_ADispositionKindOutsideTheSubjectsList_IsRefusedAsAVocabularyViolation()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);

        var ex = Assert.Throws<WorkspaceStoreException>(() =>
            Service.RecordDisposition(new DispositionRecordRequest(journey, "nb-1", obligation, "accepted"), Actor));

        Assert.Equal(StoreErrorCodes.VocabularyViolation, ex.ErrorCode);
    }

    [Fact]
    public void B2_TextAndReasonOverTwoThousandCharacters_AreRefused_AndExactlyTwoThousandIsKept()
    {
        var journey = OpenWork();
        var obligation = MandatoryObligation(journey);
        var atCap = new string('a', 2000);
        var overCap = new string('a', 2001);

        var text = Assert.Throws<WorkspaceStoreException>(() =>
            Service.SetObligations(new ObligationsSetRequest(journey, "nb-1", [new ObligationInput(null, overCap, true)])));
        var reason = Assert.Throws<WorkspaceStoreException>(() =>
            Service.RecordDisposition(new DispositionRecordRequest(journey, "nb-1", obligation, "deferred", overCap), Actor));

        Assert.Equal(StoreErrorCodes.WorkFieldTooLong, text.ErrorCode);
        Assert.Equal(StoreErrorCodes.WorkFieldTooLong, reason.ErrorCode);
        Assert.Equal(atCap, Service.RecordDisposition(
            new DispositionRecordRequest(journey, "nb-1", obligation, "deferred", atCap), Actor).Disposition.Reason);
        Assert.Equal(2, Service.SetObligations(new ObligationsSetRequest(journey, "nb-1", [new ObligationInput(null, atCap, true)])).Revision);
    }

    // ---- B3: no adoption, one checklist per journey ----------------------------------------------

    private IReadOnlyList<NotebookRow> NotebookRows()
    {
        using var reader = _workspace.Store.BeginRead();
        return reader.ListNotebooks();
    }

    private static IEnumerable<(string Name, Action<ChecklistService> Call)> EveryOperation(string journeyId, string notebookId) =>
    [
        ("evaluate", s => s.Evaluate(new ChecklistEvaluateRequest(journeyId, notebookId, LiveSha))),
        ("obligations.set", s => s.SetObligations(new ObligationsSetRequest(journeyId, notebookId, [new ObligationInput(null, "x", true)]))),
        ("disposition.record", s => s.RecordDisposition(new DispositionRecordRequest(journeyId, notebookId, null, "intent-accepted"), Actor)),
        ("evidence.link", s => s.LinkEvidence(new EvidenceLinkRequest(journeyId, notebookId, "obl-x", "dk-x", LiveSha), Actor)),
        ("work.accept", s => s.Accept(new WorkAcceptRequest(journeyId, notebookId, LiveSha), Actor)),
    ];

    private void AssertEveryOperationRefused(string journeyId, string notebookId)
    {
        foreach (var (name, call) in EveryOperation(journeyId, notebookId))
        {
            var ex = Assert.Throws<WorkspaceStoreException>(() => call(Service));
            Assert.True(ex.ErrorCode == StoreErrorCodes.WorkNotAWorkJourney, $"{name} gave {ex.ErrorCode}");
        }
    }

    [Fact]
    public void B3_TheWholeScenario_LeavesEveryNotebookDimRowByteIdentical()
    {
        var journey = OpenWork();
        var before = NotebookRows();

        var obligation = MandatoryObligation(journey);
        Attempt("dk-1");
        Link(journey, obligation, "dk-1");
        Service.RecordDisposition(new DispositionRecordRequest(journey, "nb-1", obligation, "acknowledged"), Actor);
        AcceptIntent(journey);
        Accept(journey);

        Assert.Equal(before, NotebookRows());
    }

    [Fact]
    public void B3_ALegacyUnownedNotebook_IsRefusedOnEveryOperation_AndNeverAdopted()
    {
        var journey = OpenWork();
        using (var writer = _workspace.Store.BeginWrite())
        {
            // A v2 store admits no new legacy row; only a migrated v1 file has them. Lift the guard in this temp store.
            writer.ExecuteRawInternal("DROP TRIGGER trg_notebook_legacy_flag;");
            writer.ExecuteRawInternal("""
                INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
                VALUES ('nb-legacy', 1, NULL, 1, '2026-01-01T00:00:00.0000000Z', 900000);
                """);
            writer.Commit();
        }

        var before = NotebookRows();

        AssertEveryOperationRefused(journey, "nb-legacy");
        Assert.Equal(before, NotebookRows());
    }

    [Fact]
    public void B3_ATerminalDraftNotebook_IsRefusedOnEveryOperation()
    {
        using (var writer = _workspace.Store.BeginWrite())
        {
            writer.ExecuteRawInternal($"""
                INSERT INTO journey_dim (journey_id, version_no, purpose, origin_scope, title, recorded_at, ingress_seq)
                VALUES ('{WorkspaceSchema.TerminalJourneyId}', 1, 'work', 'terminal', 'Terminal drafts', '2026-01-01T00:00:00.0000000Z', 900000);
                INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
                VALUES ('nb-term', 1, '{WorkspaceSchema.TerminalJourneyId}', 0, '2026-01-01T00:00:00.0000000Z', 900001);
                """);
            writer.Commit();
        }

        AssertEveryOperationRefused(WorkspaceSchema.TerminalJourneyId, "nb-term");
    }

    [Fact]
    public void B3_AnInvestigationJourneysNotebook_IsRefusedOnEveryOperation()
    {
        using (var writer = _workspace.Store.BeginWrite())
        {
            writer.AppendJourney(new JourneyRow("journey-inv", "investigation", "Why is it slow"));
            writer.AppendNotebook(new NotebookRow("nb-inv", "journey-inv", Legacy: false));
            writer.Commit();
        }

        AssertEveryOperationRefused("journey-inv", "nb-inv");
    }

    [Fact]
    public void B3_ANotebookOwnedByAnotherJourney_AndAnUnknownJourney_AreRefused()
    {
        var journey = OpenWork();
        OpenWork("nb-2");

        AssertEveryOperationRefused(journey, "nb-2");
        AssertEveryOperationRefused("journey-nobody", "nb-1");
    }

    [Fact]
    public void B3_TwoJourneysWithTheSameObligationText_GetDistinctObligationIds()
    {
        var first = OpenWork();
        var second = OpenWork("nb-2");

        var one = MandatoryObligation(first);
        var two = MandatoryObligation(second, "nb-2");

        Assert.NotEqual(one, two);
        Assert.StartsWith("obl-", one);
        Assert.StartsWith("obl-", two);
    }

    [Fact]
    public void B3_AWriteThatWouldCreateASecondChecklistForAJourney_IsRefusedAndWritesNothing()
    {
        var journey = OpenWork();

        var ex = Assert.Throws<WorkspaceStoreException>(() => Service.SetObligations(
            new ObligationsSetRequest(journey, "nb-1", [new ObligationInput(null, "Tests pass", true)], ChecklistId: "checklist-b")));

        Assert.Equal(StoreErrorCodes.WorkSecondChecklist, ex.ErrorCode);
        using var reader = _workspace.Store.BeginRead();
        Assert.Empty(reader.ReadObligationHistory("checklist-b"));
        Assert.Empty(reader.ReadObligationHistory(journey));
    }

    [Fact]
    public void B3_NamingTheJourneyAsTheChecklist_IsTheSameChecklist_NotASecond()
    {
        var journey = OpenWork();

        var result = Service.SetObligations(
            new ObligationsSetRequest(journey, "nb-1", [new ObligationInput(null, "Tests pass", true)], ChecklistId: journey));

        Assert.Equal(1, result.Revision);
    }
}
