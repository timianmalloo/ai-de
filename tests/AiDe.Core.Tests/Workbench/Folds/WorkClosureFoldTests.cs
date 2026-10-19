using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Tests.Workbench.Folds;

/// <summary>WL-1b check A3: work closure reads only <c>work</c> dispositions and never re-labels an old acceptance current.</summary>
public sealed class WorkClosureFoldTests
{
    private const string Current = "intent:n1@3";

    private static DispositionRow Accepted(string baseline, long seq) =>
        new("work", "j1", "accepted", "shell:1", baseline) { IngressSeq = seq };

    [Fact]
    public void A3_NoAcceptedRow_IsNotAccepted_WhateverElseExists()
    {
        var others = new[]
        {
            new DispositionRow("intent", "j1", "intent-accepted", "shell:1", Current, NotebookId: "n1", RevisionNo: 3),
            new DispositionRow("obligation", "o1", "acknowledged", "shell:1", Current),
        };

        Assert.Equal(new WorkClosure(WorkClosureState.NotAccepted, null, false), WorkClosureFold.Evaluate(others, Current));
        Assert.Equal(WorkClosureState.NotAccepted, WorkClosureFold.Evaluate([], Current).State);
    }

    [Fact]
    public void A3_AnAcceptedRowAtTheCurrentBaseline_IsAcceptedAtCurrent() =>
        Assert.Equal(new WorkClosure(WorkClosureState.Accepted, Current, true),
            WorkClosureFold.Evaluate([Accepted(Current, 1)], Current));

    [Fact]
    public void A3_AnAcceptedRowAtAnOldBaseline_StaysAccepted_ButNotAtCurrent() =>
        Assert.Equal(new WorkClosure(WorkClosureState.Accepted, "intent:n1@2", false),
            WorkClosureFold.Evaluate([Accepted("intent:n1@2", 1)], Current));

    [Fact]
    public void A3_AnAcceptanceAtTheCurrentBaseline_WinsOverAnOlderOne() =>
        Assert.True(WorkClosureFold.Evaluate([Accepted(Current, 1), Accepted("intent:n1@2", 2)], Current).AtCurrentBaseline);
}
