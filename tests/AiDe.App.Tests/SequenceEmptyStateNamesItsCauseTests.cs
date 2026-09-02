using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The sequence diagram's empty state says which emptiness it is, and never guesses why.
/// </summary>
/// <remarks>
/// <para><b>The defect, reported from a screenshot.</b> A freshly opened Sequence diagram tab showed
/// <i>"Sequence diagrams need ordered call data from the extractor — this surface renders it as soon
/// as that lands."</i> One message covered every empty case, including the ordinary one: nothing had
/// been selected yet.</para>
///
/// <para><b>It was false where it mattered.</b> The workspace it was reported against held
/// <b>4,967</b> <c>calls_at</c> assertions — measured in the store, not inferred. The extractor had
/// done its job and the feed was wired. The owner read the message, concluded the call data had not
/// landed, and reported an extractor gap. That was a correct inference from a confident and wrong
/// statement, and it cost a round trip.</para>
///
/// <para><b>The rule.</b> An empty state may say what it is waiting for. It may name a cause only
/// when it has observed one. "Nothing to show" plus a guess at why is worse than "nothing to show",
/// because the guess is the part that gets acted on.</para>
/// </remarks>
public sealed class SequenceEmptyStateNamesItsCauseTests
{
    /// <summary>Every string the surface is showing right now.</summary>
    private static string Rendered(Action<SequenceDiagramSurface> arrange) => Sta.Run(() =>
    {
        var surface = new SequenceDiagramSurface();
        arrange(surface);

        var text = new System.Text.StringBuilder();
        Walk(surface, text);
        return text.ToString();
    });

    private static void Walk(System.Windows.DependencyObject node, System.Text.StringBuilder text)
    {
        if (node is TextBlock block) text.Append(' ').Append(block.Text);

        if (node is ContentControl { Content: System.Windows.DependencyObject inner }) Walk(inner, text);
        if (node is ScrollViewer { Content: System.Windows.DependencyObject scrolled }) Walk(scrolled, text);

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            Walk(VisualTreeHelper.GetChild(node, i), text);
        }
    }

    [Fact]
    public void BeforeAnythingIsSelectedItAsksForASelection()
    {
        // The state a freshly opened tab is in, which is the state that was misreported.
        var text = Rendered(_ => { });

        Assert.Contains("Select a node", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ItNeverBlamesTheExtractorForAnUnmadeSelection()
    {
        // THE DEFECT. The surface has not looked at the store, so it cannot know whether call data
        // exists — and on the workspace where this was reported, 4,967 calls_at assertions did.
        var text = Rendered(_ => { });

        Assert.DoesNotContain("extractor", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ANodeWithNoCallsSaysSoAboutThatNode()
    {
        // The other emptiness, and the only one where a cause may be named — here the surface HAS
        // asked and been told nothing came back for this node.
        var text = Rendered(s => s.ShowFor("AiDe.Core.Widget", SequenceModel.Empty));

        Assert.Contains("Widget", text, StringComparison.Ordinal);
        Assert.Contains("no recorded calls", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void APopulatedModelStillRenders()
    {
        // The DC-016 guard: a surface that showed the empty state unconditionally would satisfy
        // every assertion above while displaying nothing at all.
        var text = Rendered(s => s.ShowFor(
            "AiDe.Core.Caller",
            SequenceModel.Build([("AiDe.Core.Caller", "AiDe.Core.Callee", "DoWork")])));

        Assert.Contains("Callee", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Select a node", text, StringComparison.OrdinalIgnoreCase);
    }
}
