using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// A merged stream is written by more than one lane by definition, and each lane drains on its own
/// thread. These are the two halves of that claim — and they are <b>not</b> equally well evidenced.
/// </summary>
/// <remarks>
/// <para><b>Half one has a deterministic falsifier and is asserted below.</b> Before the fix,
/// <c>Rows</c> returned the live <see cref="List{T}"/>, so any consumer enumerating it while a lane
/// appended threw <c>InvalidOperationException: Collection was modified</c> — and
/// <c>ConsoleSurface.Render</c> does exactly that <c>foreach</c>. That needs no interleaving at all
/// to reproduce: a <see cref="List{T}"/> enumerator checks its version on every step, so
/// enumerate-append-continue fails on demand, every time, single-threaded.</para>
///
/// <para><b>Half two — that two concurrent <c>Add</c> calls cannot corrupt the list — is NOT
/// RECORDED, and is deliberately not claimed as fixed.</b> Forcing two threads to collide inside
/// the critical section needs a rendezvous <i>inside</i> <c>Append</c>, and adding one would mean
/// shipping a seam that exists only for its own test. <see cref="ConcurrentLanesNeverLoseARow"/>
/// below is therefore a <b>measurement, not a proof</b>: it is evidence the lock holds under load,
/// and its failure without the lock is a rate rather than a certainty. <b>What would confirm it:</b>
/// an injectable barrier inside <c>Append</c>'s locked region, or a runtime that can enumerate
/// thread interleavings. Neither is in this repository, and the first is a distortion of the code
/// under test.</para>
/// </remarks>
public sealed class TheMergedStreamIsWrittenByMoreThanOneLaneTests
{
    private const int Threads = 4;
    private const int PerThread = 250;

    /// <summary>
    /// THE DETERMINISTIC ONE. Every read hands back a snapshot, so a reader mid-enumeration is never
    /// looking at a list a lane can still touch.
    /// </summary>
    /// <remarks>
    /// Observed red by reverting <c>Rows</c> to <c>=&gt; _rows</c>: <c>InvalidOperationException:
    /// Collection was modified; enumeration operation may not execute.</c> on the second
    /// <c>MoveNext</c>, on every run.
    /// </remarks>
    [Fact]
    public void AReaderIsNeverEnumeratingAListALaneCanStillAppendTo()
    {
        var stream = new ConsoleStreamModel();
        stream.Append("lane-a", "claude-code", Event(1, "first"));
        stream.Append("lane-a", "claude-code", Event(2, "second"));

        // Each read is taken, stepped into, and then a lane appends underneath it. A live collection
        // throws on the next step; a snapshot cannot.
        AssertSurvivesAnAppendMidEnumeration(() => stream.Rows, stream, 3);
        AssertSurvivesAnAppendMidEnumeration(() => stream.VisibleRows, stream, 4);
        AssertSurvivesAnAppendMidEnumeration(() => stream.Rail, stream, 5);
        AssertSurvivesAnAppendMidEnumeration(() => stream.FilterTree, stream, 6);

        // And the check was looking at something: every append really landed.
        Assert.Equal(6, stream.Rows.Count);
    }

    /// <summary>
    /// THE MEASURED ONE. Four lanes appending at once lose nothing and duplicate nothing.
    /// </summary>
    /// <remarks>
    /// <b>Evidence, not proof</b> — see the type's remarks. Without the lock this was observed
    /// failing 10 of 10 runs; the failure captured read <c>Assert.Equal() Failure: Values differ.
    /// Expected: 1000. Actual: 983</c> — seventeen rows two lanes both thought they had appended.
    /// With the lock it passed 10 of 10. A rate is what this test can supply, and calling it a proof
    /// would be the claim the Owner refused.
    /// </remarks>
    [Fact]
    public void ConcurrentLanesNeverLoseARow()
    {
        var stream = new ConsoleStreamModel();

        Parallel.For(0, Threads, lane =>
        {
            for (var i = 1; i <= PerThread; i++)
            {
                stream.Append($"lane-{lane}", $"engine-{lane}", Event(i, $"line {i}"));
            }
        });

        Assert.Equal(Threads * PerThread, stream.Rows.Count);

        for (var lane = 0; lane < Threads; lane++)
        {
            Assert.Empty(stream.OrdinalGaps($"lane-{lane}"));
            Assert.Equal(PerThread, stream.Rail.Single(r => r.LaneId == $"lane-{lane}").Rows);
        }
    }

    /// <summary>
    /// Takes a read, steps into it, appends underneath it, and steps again.
    /// </summary>
    /// <remarks>
    /// The append is real, through the ordinary public path — the point is that a reader and a lane
    /// overlap, not that the collection is poked from outside.
    /// </remarks>
    private static void AssertSurvivesAnAppendMidEnumeration<T>(
        Func<IReadOnlyList<T>> read, ConsoleStreamModel stream, long nextOrdinal)
    {
        using var reader = read().GetEnumerator();

        Assert.True(reader.MoveNext(), "the read was empty, so it cannot pose the question");

        stream.Append("lane-b", "codex", Event(nextOrdinal, $"appended under a reader at {nextOrdinal}"));

        // The whole assertion. On a live collection this is where it throws.
        while (reader.MoveNext())
        {
        }
    }

    private static RunEvent Event(long seq, string text) => new(
        "run-merged", "lane", null, seq, DateTimeOffset.UtcNow, "agent.msg", null,
        new System.Text.Json.Nodes.JsonObject
        {
            ["content"] = new System.Text.Json.Nodes.JsonObject { ["text"] = text },
        },
        []);
}
