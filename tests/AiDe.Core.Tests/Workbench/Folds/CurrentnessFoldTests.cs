using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Tests.Workbench.Folds;

/// <summary>ADR-0040 §5: an attempt bound to (notebook, revision, projection_sha) is stale when either moved.</summary>
public sealed class CurrentnessFoldTests
{
    private static readonly AttemptRow Attempt = new("dk-1", "w", 1, "n1", 3, "s", 1, "sha-a");

    [Fact]
    public void SameRevisionAndProjection_IsCurrent() =>
        Assert.Equal(Currentness.Current, CurrentnessFold.Evaluate(Attempt, 3, "sha-a").State);

    [Fact]
    public void ALaterRevision_IsStale_WithTheSpecCopy()
    {
        var result = CurrentnessFold.Evaluate(Attempt, 4, "sha-a");

        Assert.Equal(Currentness.Stale, result.State);
        Assert.Equal("This output belongs to an earlier draft. Run again to refresh it.", result.Copy);
    }

    [Fact]
    public void ADifferentProjection_IsStale() =>
        Assert.Equal(Currentness.Stale, CurrentnessFold.Evaluate(Attempt, 3, "sha-b").State);

    [Fact]
    public void ACurrentResult_CarriesNoCopy() =>
        Assert.Null(CurrentnessFold.Evaluate(Attempt, 3, "sha-a").Copy);

    [Theory]
    [InlineData(null, "sha-a")]
    [InlineData(3, null)]
    public void AnUnknownLiveSide_IsNotRecorded_NeverGuessedCurrent(int? revision, string? sha) =>
        Assert.Equal(Currentness.NotRecorded, CurrentnessFold.Evaluate(Attempt, revision, sha).State);

    [Fact]
    public void AnAttemptWithNoBoundProjection_IsNotRecorded() =>
        Assert.Equal(Currentness.NotRecorded,
            CurrentnessFold.Evaluate(Attempt with { ProjectionSha = null }, 3, "sha-a").State);
}
