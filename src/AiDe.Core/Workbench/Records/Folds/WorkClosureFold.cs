using AiDe.Core.Store.Records;

namespace AiDe.Core.Workbench.Records.Folds;

public enum WorkClosureState { NotAccepted, Accepted }

/// <summary>
/// Work closure. An acceptance at an older baseline stays <see cref="WorkClosureState.Accepted"/> with
/// <paramref name="AtCurrentBaseline"/> false: the record is historical and never re-labelled current.
/// </summary>
public sealed record WorkClosure(WorkClosureState State, string? AcceptedBaseline, bool AtCurrentBaseline);

/// <summary>Whether a journey's deliverable was accepted, derived from <c>subject_kind = work</c> dispositions only.</summary>
public static class WorkClosureFold
{
    public static WorkClosure Evaluate(IReadOnlyList<DispositionRow> workDispositions, string currentBaseline) =>
        throw new NotImplementedException();
}
