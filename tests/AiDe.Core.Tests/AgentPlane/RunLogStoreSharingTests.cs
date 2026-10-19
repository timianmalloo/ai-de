using AiDe.Core.AgentPlane;

namespace AiDe.Core.Tests.AgentPlane;

/// <summary>
/// Pins <c>RunLogStore.Read</c>'s share mode: a reader opens a run log the writer still holds, as the product's
/// reopened notebook does while a launch is winding down (Ruling 165).
/// </summary>
public sealed class RunLogStoreSharingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "runlog-sharing-" + Guid.NewGuid().ToString("N"));

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void AReaderOpensARunLogItsWriterStillHolds_AndGetsTheAppendedLine()
    {
        using var writer = RunLogStore.Open(_root, "session-1", "dispatch-1");
        writer.Append(new RunEvent("run-1", "lane-1", null, 7, DateTimeOffset.UnixEpoch, "acp.frame", null, [], []));

        var read = RunLogStore.Read(writer.Path);

        var only = Assert.Single(read);
        Assert.Equal(7, only.Seq);
    }
}
