using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Facts;
using AiDe.Core.Projections;

namespace AiDe.App.Tests;

/// <summary>
/// The evidence panes, rendered and read — not merely constructed.
/// </summary>
/// <remarks>
/// <para><b>Written because "it constructs" proved nothing.</b> The canvas rendered nothing at all
/// for days behind a JavaScript syntax error, and the only reason anyone found out was an
/// out-of-process probe that happened to be rebuilt. The Joins and Contexts panes had less than
/// that: a factory test asserting the right TYPE came back, which a pane showing an empty white
/// rectangle passes.</para>
///
/// <para><b>In-process on an STA thread, deliberately.</b> The canvas needs a real foreground window
/// because it drives a browser through SendInput (DC-014); these are WPF controls that build their
/// own children, so an out-of-process probe would be ceremony that proves the same thing more
/// slowly.</para>
///
/// <para><b>Every case asserts CONTENT, never a control count.</b> "Has 4 children" passes for four
/// empty labels. What a user can read is the thing under test.</para>
/// </remarks>
public sealed class PaneRenderTests
{
    private static T OnSta<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try { result = work(); }
            catch (Exception ex) { failure = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "the STA thread did not finish");
        if (failure is not null) throw failure;
        return result;
    }

    /// <summary>Every readable string in a pane, in render order.</summary>
    /// <remarks>
    /// Read INSIDE the STA body and returned as plain strings: a WPF control belongs to the thread
    /// that created it, so returning the control and reading it from the test thread throws.
    /// </remarks>
    private static List<string> TextOf(DependencyObject root)
    {
        var found = new List<string>();

        void Walk(object? node)
        {
            switch (node)
            {
                case TextBlock text when !string.IsNullOrWhiteSpace(text.Text):
                    found.Add(text.Text);
                    return;
                case null:
                    return;
                default:
                    break;
            }

            if (node is DependencyObject dependency)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(dependency))
                {
                    Walk(child);
                }
            }

            // Headers and Content are logical children only once templates apply, and these panes
            // are read before any window shows them.
            if (node is HeaderedContentControl headered)
            {
                Walk(headered.Header);
                Walk(headered.Content);
            }
            else if (node is ContentControl content)
            {
                Walk(content.Content);
            }
            else if (node is System.Windows.Controls.Border border)
            {
                Walk(border.Child);
            }
        }

        Walk(root);
        return found;
    }

    private static Provenance Where => new("test", null, "test", "1", DateTimeOffset.UnixEpoch);

    // ── The contexts pane ─────────────────────────────────────────────────────────────────

    private static ContextMapView TwoContexts() => new(
        [
            new ContextView("Football", "The domain core", 516, 902, 190),
            new ContextView("Platform", "Shared persistence", 91, 37, 161),
        ],
        [
            new ContextEdge("Football", "Platform", 57,
                [new CrossingMember("Shop.Fixtures", "depends_on", "Infra.Data.AppDbContext")]),
        ],
        412,
        [],
        [new UncoveredGroup("TheTerrace.Tests", 362, ["TheTerrace.Tests.AccountPageRenderTests"])]);

    [Fact]
    public void TheContextsPaneRendersItsContexts_ItsCrossings_AndItsUncovered()
    {
        var text = OnSta(() =>
        {
            var pane = new ContextMapSurface("Contexts") { Source = TwoContexts };
            pane.Refresh();
            return TextOf(pane);
        });

        var all = string.Join(" | ", text);

        Assert.Contains("Football", all, StringComparison.Ordinal);
        Assert.Contains("Platform", all, StringComparison.Ordinal);

        // The numbers are the point of the pane. A context map that only names contexts is a picture
        // of a decision; the crossings are the evidence for whether it held.
        Assert.Contains("190 crossing", all, StringComparison.Ordinal);
        Assert.Contains("57 edge(s)", all, StringComparison.Ordinal);

        // And the crossing's members, which is what makes the count checkable.
        Assert.Contains("Infra.Data.AppDbContext", all, StringComparison.Ordinal);

        Assert.Contains("412", all, StringComparison.Ordinal);
        Assert.Contains("TheTerrace.Tests", all, StringComparison.Ordinal);
    }

    [Fact]
    public void AnInvalidContextMapRendersItsProblems_NotAPartialDiagram()
    {
        var text = OnSta(() =>
        {
            var pane = new ContextMapSurface("Contexts")
            {
                Source = () => new ContextMapView([], [], 0,
                    ["two contexts claim TheTerrace.Features.Teams.*"], []),
            };

            pane.Refresh();
            return TextOf(pane);
        });

        var all = string.Join(" | ", text);

        Assert.Contains("invalid", all, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("two contexts claim", all, StringComparison.Ordinal);

        // A map drawn from a file that failed validation is wrong in a way nobody can see.
        Assert.DoesNotContain("crossing", all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithNoWorkspace_TheContextsPaneSaysWhatIsMissing_RatherThanRenderingEmpty()
    {
        var text = OnSta(() =>
        {
            var pane = new ContextMapSurface("Contexts");
            return TextOf(pane);
        });

        Assert.NotEmpty(text);
        Assert.Contains(text, t => t.Contains("bounded-contexts.yaml", StringComparison.Ordinal));
    }

    [Fact]
    public void WithNoMapDeclared_ThePaneSaysSo_RatherThanClaimingFullCoverage()
    {
        // MEASURED on a second repository, which has no bounded-contexts.yaml. The pane reported
        // zero uncovered symbols and "every declared symbol belongs to a context" — the sentence a
        // fully-mapped codebase produces. The arithmetic was right; the claim was false.
        var text = OnSta(() =>
        {
            var pane = new ContextMapSurface("Contexts")
            {
                Source = () => new ContextMapView([], [], 0, [], [], IsDeclared: false),
            };

            pane.Refresh();
            return TextOf(pane);
        });

        var all = string.Join(" | ", text);

        Assert.Contains("No context map is declared", all, StringComparison.Ordinal);
        Assert.DoesNotContain("belongs to a context", all, StringComparison.Ordinal);
    }

    // ── The joins pane ────────────────────────────────────────────────────────────────────

    private static JoinResult SomeJoins() => new(
        [
            new JoinEdge("invitationPepper", "secret", "is_declared_secret", VerificationStatus.Verified,
                "declared @secure() in the template; its value is never read"),
            new JoinEdge("Shop.Sales.Order", "table:Order", "maps_to", VerificationStatus.Inferred,
                "the type name 'Order' corresponds to table 'Order' by EF's naming convention; nothing declares this"),
        ],
        ["sql-resource-name-unresolved"],
        1,
        1);

    [Fact]
    public void TheJoinsPaneSeparatesVerifiedFromInferred_AndShowsEveryBasis()
    {
        var text = OnSta(() =>
        {
            var pane = new JoinSurface("Joins") { Source = SomeJoins };
            pane.Refresh();
            return TextOf(pane);
        });

        var all = string.Join(" | ", text);

        Assert.Contains("1 verified", all, StringComparison.Ordinal);
        Assert.Contains("1 inferred", all, StringComparison.Ordinal);

        // Under their own headings, because a ranked list mixes them and a user reading top-down
        // acts on an inferred join believing it was checked.
        Assert.Contains("Verified", all, StringComparison.Ordinal);
        Assert.Contains("Inferred", all, StringComparison.Ordinal);

        // "Why do you believe this" is the only question worth asking of a join.
        Assert.Contains("never read", all, StringComparison.Ordinal);
        Assert.Contains("nothing declares this", all, StringComparison.Ordinal);

        // What could NOT be joined, in words rather than as a code alone.
        Assert.Contains("sql-resource-name-unresolved", all, StringComparison.Ordinal);
        Assert.Contains("expression", all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnEmptyJoinResultSaysSo_RatherThanRenderingBlank()
    {
        // Zero verified joins was the honest answer on a real repository. A pane that renders
        // nothing for it is indistinguishable from a pane that is broken.
        var text = OnSta(() =>
        {
            var pane = new JoinSurface("Joins") { Source = () => new JoinResult([], [], 0, 0) };
            pane.Refresh();
            return TextOf(pane);
        });

        var all = string.Join(" | ", text);

        Assert.Contains("0 verified", all, StringComparison.Ordinal);
        Assert.Contains("Nothing joined", all, StringComparison.Ordinal);
        Assert.Contains("Nothing was withheld", all, StringComparison.Ordinal);
    }

    // ── The rule both panes have to keep ──────────────────────────────────────────────────

    [Fact]
    public void NoEvidencePaneEverRendersNothing()
    {
        // The canvas rendered nothing at all for days and every test around it passed. This is that
        // question asked of every pane the factory can build, in every state it has: a pane with no
        // readable text is either broken or is telling the user nothing, and both are defects.
        var empty = new JoinResult([], [], 0, 0);
        var noContexts = new ContextMapView([], [], 0, [], []);

        var states = OnSta(() =>
        {
            var results = new List<(string Name, int Texts)>();

            void Check(string name, FrameworkElement pane) => results.Add((name, TextOf(pane).Count));

            Check("joins, unattached", new JoinSurface("Joins"));

            var joins = new JoinSurface("Joins") { Source = () => empty };
            joins.Refresh();
            Check("joins, empty result", joins);

            var populated = new JoinSurface("Joins") { Source = SomeJoins };
            populated.Refresh();
            Check("joins, populated", populated);

            Check("contexts, unattached", new ContextMapSurface("Contexts"));

            var none = new ContextMapSurface("Contexts") { Source = () => noContexts };
            none.Refresh();
            Check("contexts, no contexts", none);

            var contexts = new ContextMapSurface("Contexts") { Source = TwoContexts };
            contexts.Refresh();
            Check("contexts, populated", contexts);

            return results;
        });

        Assert.All(states, state =>
            Assert.True(state.Texts > 0, $"{state.Name} rendered no readable text"));
    }
}
