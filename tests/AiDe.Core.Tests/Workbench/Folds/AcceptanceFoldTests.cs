using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Tests.Workbench.Folds;

/// <summary>WL-1b checks A1 and A2: acceptance eligibility as a pure fold (ADR-0040 sections 3 and 5).</summary>
public sealed class AcceptanceFoldTests
{
    private const string Baseline = "intent:n1@3";

    private static readonly ObligationRow O1 = new("o1", "j1", 1, "Tests pass", true);
    private static readonly AttemptRow Attempt = new("dk-1", "w", 1, "n1", 3, "s", 1, "sha-a");

    private static EvidenceLinkRow Link(string baseline = Baseline, string status = AcceptanceFold.ObservedStatus) =>
        new("l-o1", "obligation", "o1", 1, "attempt://dk-1", "claude", baseline, status);

    private static DispositionRow Human(string kind, string actor, long seq = 1) =>
        new("obligation", "o1", kind, actor, Baseline) { IngressSeq = seq };

    private static DispositionRow IntentAccepted(int revision = 3) =>
        new("intent", "j1", "intent-accepted", "shell:1", $"intent:n1@{revision}", NotebookId: "n1", RevisionNo: revision);

    /// <summary>A fully satisfied, eligible scenario; each test overrides one input.</summary>
    private static AcceptanceInput Eligible() => new(
        "j1", "n1", 3,
        [O1],
        new Dictionary<string, IReadOnlyList<ObligationRow>> { ["o1"] = [O1] },
        new Dictionary<string, IReadOnlyList<EvidenceLinkRow>> { ["o1"] = [Link()] },
        new Dictionary<string, IReadOnlyList<DispositionRow>>(),
        [IntentAccepted()],
        new Dictionary<string, AttemptRow> { ["dk-1"] = Attempt },
        "sha-a");

    private static AcceptanceInput WithLinks(AcceptanceInput input, params EvidenceLinkRow[] links) =>
        input with { Links = new Dictionary<string, IReadOnlyList<EvidenceLinkRow>> { ["o1"] = links } };

    private static AcceptanceInput WithHuman(AcceptanceInput input, params DispositionRow[] rows) =>
        input with { ObligationDispositions = new Dictionary<string, IReadOnlyList<DispositionRow>> { ["o1"] = rows } };

    private static AcceptanceInput NoLinks(AcceptanceInput input) =>
        input with { Links = new Dictionary<string, IReadOnlyList<EvidenceLinkRow>>() };

    private static string[] Codes(AcceptanceEvaluation e) => e.Gaps.Select(g => g.Code).ToArray();

    // ---- A1: acknowledged is not satisfied -------------------------------------------------------

    [Fact]
    public void A1_AcknowledgedWithNoLink_IsMissing_NotSatisfied()
    {
        var result = AcceptanceFold.Evaluate(WithHuman(NoLinks(Eligible()), Human("acknowledged", "shell:1")));

        var o = Assert.Single(result.Obligations);
        Assert.Equal(EvidenceState.Missing, o.Evidence);
        Assert.Equal(HumanState.Acknowledged, o.Human);
        Assert.False(result.Eligible);
        var gap = Assert.Single(result.Gaps);
        Assert.Equal(("evidence-missing", "o1"), (gap.Code, gap.ObligationId));
        Assert.Equal("Blocked — “Tests pass” has no current evidence.", gap.Sentence);
    }

    [Fact]
    public void A1_AcceptancePolicy_CoversNoKind_OfAnySubject()
    {
        var kinds = WorkbenchVocabulary.DispositionKindsBySubject.Values.SelectMany(k => k).Append("unknown-kind");

        Assert.Empty(AcceptancePolicy.CoveringKinds);
        Assert.All(kinds, kind => Assert.False(AcceptancePolicy.Covers(kind), kind));
    }

    [Theory]
    [InlineData("acknowledged")]
    [InlineData("deferred")]
    public void A1_NoHumanDisposition_RemovesAnEvidenceGap(string kind)
    {
        var result = AcceptanceFold.Evaluate(WithHuman(NoLinks(Eligible()), Human(kind, "shell:1")));

        Assert.Contains("evidence-missing", Codes(result));
        Assert.False(result.Eligible);
    }

    [Fact]
    public void A1_NeedsDecision_BlocksEvenWithCurrentEvidence()
    {
        var result = AcceptanceFold.Evaluate(WithHuman(Eligible(), Human("needs-decision", "shell:1")));

        var o = Assert.Single(result.Obligations);
        Assert.Equal(EvidenceState.Satisfied, o.Evidence);
        Assert.Equal(HumanState.NeedsDecision, o.Human);
        Assert.Equal(["needs-decision"], Codes(result));
        Assert.False(result.Eligible);
    }

    [Fact]
    public void A1_NeedsDecision_IsResolvedOnlyByALaterRowFromTheSameActor()
    {
        var raised = Human("needs-decision", "shell:1", 1);
        var otherActor = Human("acknowledged", "shell:2", 2);
        var sameActor = Human("acknowledged", "shell:1", 3);

        Assert.Contains("needs-decision", Codes(AcceptanceFold.Evaluate(WithHuman(Eligible(), raised, otherActor))));
        Assert.True(AcceptanceFold.Evaluate(WithHuman(Eligible(), raised, otherActor, sameActor)).Eligible);
    }

    [Fact]
    public void A1_TheObligationShowsTheMostRecentActorsLatestState()
    {
        var result = AcceptanceFold.Evaluate(WithHuman(Eligible(),
            Human("deferred", "shell:1", 1), Human("acknowledged", "shell:2", 2)));

        Assert.Equal(HumanState.Acknowledged, Assert.Single(result.Obligations).Human);
    }

    [Fact]
    public void A1_AnEmptyChecklist_Blocks_BecauseNothingIsMandatory()
    {
        var empty = Eligible() with
        {
            CurrentObligations = [],
            ObligationHistory = new Dictionary<string, IReadOnlyList<ObligationRow>>(),
        };

        var result = AcceptanceFold.Evaluate(empty);

        Assert.Equal(["checklist-empty"], Codes(result));
        Assert.False(result.Eligible);
    }

    [Fact]
    public void A1_AFullyEvidencedScenario_IsEligible_AtTheNotebooksLatestBaseline()
    {
        var result = AcceptanceFold.Evaluate(Eligible());

        Assert.True(result.Eligible);
        Assert.Empty(result.Gaps);
        Assert.Equal(Baseline, result.Baseline);
        var o = Assert.Single(result.Obligations);
        Assert.Equal((EvidenceState.Satisfied, HumanState.None), (o.Evidence, o.Human));
        Assert.Single(o.CitedCurrentLinks);
    }

    [Fact]
    public void A1_GapsAreOrdered_ChecklistThenIntentThenObligationsThenIntentAcceptance()
    {
        var noRevision = Eligible() with { IntentLatestRevisionNo = null, CurrentObligations = [] };
        Assert.Equal(["checklist-empty", "intent-missing"], Codes(AcceptanceFold.Evaluate(noRevision)));

        var open = NoLinks(Eligible()) with { IntentDispositions = [] };
        Assert.Equal(["evidence-missing", "intent-not-accepted"], Codes(AcceptanceFold.Evaluate(open)));
    }

    // ---- A2: blocked while a mandatory obligation lacks a current link ---------------------------

    [Fact]
    public void A2_NoLink_IsMissing()
    {
        var result = AcceptanceFold.Evaluate(NoLinks(Eligible()));

        Assert.Equal(EvidenceState.Missing, Assert.Single(result.Obligations).Evidence);
        Assert.Equal(["evidence-missing"], Codes(result));
    }

    [Fact]
    public void A2_ALinkAtAnOldBaseline_IsStale()
    {
        var result = AcceptanceFold.Evaluate(WithLinks(Eligible(), Link(baseline: "intent:n1@2")));

        Assert.Equal(EvidenceState.Stale, Assert.Single(result.Obligations).Evidence);
        Assert.Equal(["evidence-stale"], Codes(result));
    }

    [Theory]
    [InlineData("Tests pass and lint is clean", true)]
    [InlineData("Tests pass", false)]
    public void A2_AChangedCriterion_IsStale(string textAtLinkTime, bool mandatoryAtLinkTime)
    {
        var atLinkTime = new ObligationRow("o1", "j1", 1, textAtLinkTime, mandatoryAtLinkTime);
        var changedNow = new ObligationRow("o1", "j1", 2, "Tests pass", true);
        var input = Eligible() with
        {
            CurrentObligations = [changedNow],
            ObligationHistory = new Dictionary<string, IReadOnlyList<ObligationRow>> { ["o1"] = [atLinkTime, changedNow] },
        };

        var result = AcceptanceFold.Evaluate(input);

        Assert.Equal(EvidenceState.Stale, Assert.Single(result.Obligations).Evidence);
        Assert.Equal(["evidence-stale"], Codes(result));
    }

    [Fact]
    public void A2_AStaleAttempt_IsStale_ByTheCurrentnessFold()
    {
        var result = AcceptanceFold.Evaluate(Eligible() with { LiveProjectionSha = "sha-other" });

        Assert.Equal(EvidenceState.Stale, Assert.Single(result.Obligations).Evidence);
        Assert.Equal(["evidence-stale"], Codes(result));
    }

    [Fact]
    public void A2_ANullProjection_IsNotRecorded_NeverCurrent()
    {
        var result = AcceptanceFold.Evaluate(Eligible() with { LiveProjectionSha = null });

        Assert.Equal(EvidenceState.NotRecorded, Assert.Single(result.Obligations).Evidence);
        Assert.Equal(["evidence-not-recorded"], Codes(result));
        Assert.False(result.Eligible);
    }

    [Fact]
    public void A2_AStaleLinkBesideAnUnrecordedOne_ReadsStale()
    {
        var input = WithLinks(Eligible() with { LiveProjectionSha = null }, Link(baseline: "intent:n1@2"), Link());

        Assert.Equal(EvidenceState.Stale, Assert.Single(AcceptanceFold.Evaluate(input).Obligations).Evidence);
    }

    [Fact]
    public void A2_ALinkThatWasNotObserved_NeverSatisfies()
    {
        var result = AcceptanceFold.Evaluate(WithLinks(Eligible(), Link(status: "claimed")));

        Assert.Equal(EvidenceState.Missing, Assert.Single(result.Obligations).Evidence);
        Assert.Equal(["evidence-missing"], Codes(result));
    }

    [Fact]
    public void A2_ALinkToAnAttemptTheInputDoesNotHold_IsNotRecorded()
    {
        var result = AcceptanceFold.Evaluate(Eligible() with { LinkedAttempts = new Dictionary<string, AttemptRow>() });

        Assert.Equal(EvidenceState.NotRecorded, Assert.Single(result.Obligations).Evidence);
    }

    [Fact]
    public void A2_ANonMandatoryObligation_ProducesNoGap_ButStillShowsItsState()
    {
        var optional = new ObligationRow("o2", "j1", 1, "Docs updated", false);
        var input = Eligible() with
        {
            CurrentObligations = [O1, optional],
            ObligationHistory = new Dictionary<string, IReadOnlyList<ObligationRow>> { ["o1"] = [O1], ["o2"] = [optional] },
        };

        var result = AcceptanceFold.Evaluate(input);

        Assert.True(result.Eligible);
        Assert.Equal(EvidenceState.Missing, result.Obligations.Single(o => o.ObligationId == "o2").Evidence);
    }

    [Fact]
    public void A2_UnchangedObligations_KeepTheirEvidenceAcrossAChecklistRevision()
    {
        var o1AtRevision2 = O1 with { Revision = 2 };
        var added = new ObligationRow("o2", "j1", 2, "Docs updated", true);
        var input = Eligible() with
        {
            CurrentObligations = [o1AtRevision2, added],
            ObligationHistory = new Dictionary<string, IReadOnlyList<ObligationRow>>
            {
                ["o1"] = [O1, o1AtRevision2],
                ["o2"] = [added],
            },
        };

        var result = AcceptanceFold.Evaluate(input);

        Assert.Equal(EvidenceState.Satisfied, result.Obligations.Single(o => o.ObligationId == "o1").Evidence);
        Assert.Equal(["evidence-missing"], Codes(result));
        Assert.Equal("o2", Assert.Single(result.Gaps).ObligationId);
    }

    [Fact]
    public void A2_ANotebookWithNoRevision_IsIntentMissing()
    {
        var result = AcceptanceFold.Evaluate(Eligible() with { IntentLatestRevisionNo = null });

        Assert.Contains("intent-missing", Codes(result));
        Assert.Equal(AcceptanceBaseline.NoRevision, result.Baseline);
        Assert.False(result.Eligible);
    }

    [Fact]
    public void A2_AnIntentAcceptedAtAnEarlierRevision_IsNotAccepted()
    {
        var result = AcceptanceFold.Evaluate(Eligible() with { IntentDispositions = [IntentAccepted(revision: 2)] });

        Assert.Equal(["intent-not-accepted"], Codes(result));
    }

    [Fact]
    public void A2_TheBaseline_IsOneValueObject()
    {
        Assert.Equal("intent:n1@3", AcceptanceBaseline.For("n1", 3));
        Assert.Equal(AcceptanceBaseline.NoRevision, AcceptanceBaseline.For("n1", null));
    }
}
