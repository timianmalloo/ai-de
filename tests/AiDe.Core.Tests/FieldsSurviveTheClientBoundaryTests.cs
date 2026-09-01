using System.Reflection;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;

namespace AiDe.Core.Tests;

/// <summary>
/// A field Core measures must reach the client record, or be listed as deliberately dropped.
/// </summary>
/// <remarks>
/// <para><b>The third way to lose the truth.</b> Two were already known: the bound was dropped (a
/// surface renders the payload and not its <c>Truncated</c>), and the payload was never asked for
/// (DC-073 — a stand-in rendered while the real query sat uncalled). This is the third: <b>the field
/// was dropped in transit</b>, between a producer record and the client record built from it.</para>
///
/// <para><b>The instance.</b> <c>GraphNode</c> was widened with an authoritative <c>IsKnowledge</c>
/// precisely to end the Knowledge chip reading 0 — <c>node_kind</c> is the one dimension that
/// separates knowledge from source, because <c>has_type</c> is emitted by six extractors and says
/// nothing about which half of the graph a node is in (INV-0004). The flag was then dropped where
/// <c>CanvasNode</c> is built, so the page fell back to guessing from a fixed list of spellings —
/// <c>knowledge|doc|adr|design|note|proof</c> — which cannot match a repository whose knowledge
/// kinds are <c>spec</c>, <c>investigation</c> and <c>glossary</c>. The chip read 0 again, by a
/// third mechanism, after being fixed twice.</para>
///
/// <para><b>Why the other controls could not catch it.</b> A behavioural harness that hands a
/// payload to a surface and reads the rendered tree passes here: the surface faithfully rendered
/// everything it was given, and the loss happened one boundary upstream. Each of the three failures
/// hides from the others' test, which is why there are three controls and not one.</para>
///
/// <para><b>Not a ban — a list where the unasked question gets asked.</b> Some fields genuinely
/// should not cross: <c>Degree</c> is a graph statistic the canvas does not draw. Naming one costs a
/// line; leaving one unnamed costs a flag nobody notices is missing.</para>
/// </remarks>
public sealed class FieldsSurviveTheClientBoundaryTests
{
    /// <summary>A producer record, the client record built from it, and what may be dropped.</summary>
    private sealed record Boundary(
        Type Producer, Type Client, Dictionary<string, string> DeliberatelyDropped);

    private static readonly Boundary[] Boundaries =
    [
        new(typeof(GraphNode), typeof(CanvasNode), new()
        {
            ["Degree"] =
                "A graph statistic used to rank what to keep, not to draw. The canvas sizes by "
                + "Count, which is the number of nodes a group stands for.",
            ["IsExternal"] =
                "Reaches the canvas as part of Kind (`group-external`) rather than as its own "
                + "field, because the page colours by kind.",
        }),

        // WIDENED 2026-09-01, deliberately, after the design session found its own guard reporting
        // clean over a namespace it was not scanning — R4 one layer up. The way they found it was by
        // widening the scope and watching it go red, not by reasoning about whether it was complete.
        // This list had exactly one pair, hand-written by me, and "the pairs I thought of" is the
        // same shape of blind spot.
        new(typeof(GraphEdge), typeof(CanvasEdge), new()
        {
            // GraphEdge.Status is VerificationStatus; CanvasEdge.Status is its string form. The name
            // survives, so the check passes on it — worth saying out loud that this control compares
            // NAMES and would not have noticed a type change that lost meaning.
        }),

        new(typeof(GraphCluster), typeof(CanvasNode), new()
        {
            ["NodeCount"] = "Crosses as CanvasNode.Count — renamed at the boundary, which is exactly "
                + "the rename this control cannot follow. Named here so the gap is recorded rather "
                + "than passing silently.",
            ["IsExternal"] = "Folded into Kind (`group-external`), as for GraphNode.",

            // FOUND BY THE WIDENING, and it is a decision rather than a defect — but it had never
            // been written down anywhere, which is the whole point of the list. The canvas sizes a
            // group by how many NODES it stands for, because that is the honesty claim
            // `CanvasNode.Count` exists to make: a dot standing for 240 types is only honest while
            // the 240 is on it. How densely those 240 are connected to each other is a different
            // and weaker claim, and drawing it would need a second visual channel the overview does
            // not have. Revisit if the overview ever gains one.
            ["InternalEdges"] = "The overview sizes a group by node count, not by internal density; "
                + "there is no second visual channel to carry it.",
        }),
    ];

    [Fact]
    public void EveryProducerFieldEitherCrossesOrIsNamed()
    {
        var problems = new List<string>();

        foreach (var boundary in Boundaries)
        {
            var client = Names(boundary.Client);

            foreach (var field in Names(boundary.Producer))
            {
                if (client.Contains(field)) continue;
                if (boundary.DeliberatelyDropped.ContainsKey(field)) continue;

                problems.Add(
                    $"{boundary.Producer.Name}.{field} does not reach {boundary.Client.Name} and is "
                    + "not listed as deliberately dropped. Carry it, or name it in "
                    + "DeliberatelyDropped with the reason — a field measured by Core and lost in "
                    + "transit is invisible, because everything downstream still renders.");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void NothingIsListedAsDroppedThatActuallyCrosses()
    {
        // A stale allowance is the same defect one level up: it keeps a question alive that nobody
        // has to answer any more, and makes the list longer than the thing it describes.
        var problems = new List<string>();

        foreach (var boundary in Boundaries)
        {
            var client = Names(boundary.Client);

            foreach (var named in boundary.DeliberatelyDropped.Keys)
            {
                if (client.Contains(named))
                {
                    problems.Add(
                        $"{boundary.Producer.Name}.{named} is listed as deliberately dropped but "
                        + $"{boundary.Client.Name} carries it — remove the entry.");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void TheBoundariesAreRealRecordsWithFields()
    {
        // The DC-016 guard. If Names() stops finding properties — a record becomes a class, a type
        // is renamed — every assertion above passes by comparing two empty sets.
        foreach (var boundary in Boundaries)
        {
            Assert.True(Names(boundary.Producer).Count >= 3,
                $"{boundary.Producer.Name} yielded {Names(boundary.Producer).Count} field(s); this "
                + "test would pass by looking at nothing");

            Assert.True(Names(boundary.Client).Count >= 3,
                $"{boundary.Client.Name} yielded {Names(boundary.Client).Count} field(s)");
        }
    }

    /// <summary>The record's own properties, by name.</summary>
    private static HashSet<string> Names(Type type) =>
    [
        .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != "EqualityContract")
            .Select(p => p.Name),
    ];
}
