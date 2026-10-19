using AiDe.Core.Store.Records;

namespace AiDe.Core.Workbench.Records.Folds;

public enum Currentness { Current, Stale, NotRecorded }

public sealed record CurrentnessResult(Currentness State, string? Copy);

public static class CurrentnessFold
{
    public static CurrentnessResult Evaluate(AttemptRow attempt, int? currentRevisionNo, string? liveProjectionSha) =>
        throw new NotImplementedException();
}
