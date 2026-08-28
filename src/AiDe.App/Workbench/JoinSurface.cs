using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.Core.Facts;
using AiDe.Core.Projections;

namespace AiDe.App.Workbench;

/// <summary>
/// The joins: where code, schema and infrastructure meet, and how well each meeting is established.
/// </summary>
/// <remarks>
/// <para><b>This projection existed and nobody could see it.</b> <c>JoinProjection</c> was written,
/// tested and never called by the running application — a control that cannot fire, in the shape
/// that matters most here, because the joins are the whole reason the extractors read three
/// different artifact kinds.</para>
///
/// <para><b>Verified and Inferred are separated, not sorted.</b> A ranked list mixes them, and a
/// user reading top-down acts on an inferred join believing it was checked. They are rendered under
/// their own headings with the basis on every row, because "why do you believe this" is the only
/// question worth asking of a join.</para>
///
/// <para><b>What could not be joined is stated.</b> A disclosure is the reason a join is missing —
/// a SQL resource whose name is an expression nobody evaluated, for one — and a joins view that
/// showed only what it found would read as completeness.</para>
/// </remarks>
public sealed class JoinSurface : ContentControl
{
    private readonly StackPanel _body = new() { Margin = new Thickness(12) };

    public JoinSurface(string title)
    {
        AutomationProperties.SetName(this, title);
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _body,
        };

        Render(null);
    }

    /// <summary>Supplies the joins. Null until a workspace attaches.</summary>
    public Func<JoinResult>? Source { get; set; }

    public void Refresh() => Render(Source?.Invoke());

    private void Render(JoinResult? result)
    {
        _body.Children.Clear();

        if (result is null)
        {
            _body.Children.Add(Muted("No workspace. Open one and index it to see how its code, " +
                                     "schema and infrastructure connect."));
            return;
        }

        _body.Children.Add(Heading(
            $"{result.VerifiedCount} verified · {result.InferredCount} inferred"));

        if (result.Edges.Count == 0)
        {
            _body.Children.Add(Muted(
                "Nothing joined. That is a real answer: this workspace may have no schema or " +
                "infrastructure evidence to join code to, and inventing a join to fill the pane " +
                "would be worse than an empty one."));
        }

        Section(result, VerificationStatus.Verified, "Verified — both sides were read",
            "Nothing is verified. Every join below rests on a naming convention.");

        Section(result, VerificationStatus.Inferred, "Inferred — a convention, not a declaration",
            "Nothing is inferred.");

        _body.Children.Add(Heading("What could not be joined"));

        if (result.Disclosures.Count == 0)
        {
            _body.Children.Add(Muted("Nothing was withheld."));
            return;
        }

        foreach (var disclosure in result.Disclosures)
        {
            _body.Children.Add(new TextBlock
            {
                Text = "• " + Explain(disclosure),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
                Opacity = 0.85,
            });
        }
    }

    private void Section(JoinResult result, VerificationStatus status, string heading, string whenEmpty)
    {
        var edges = result.Edges.Where(e => e.Status == status).ToList();
        _body.Children.Add(Heading(heading));

        if (edges.Count == 0)
        {
            _body.Children.Add(Muted(whenEmpty));
            return;
        }

        foreach (var edge in edges.Take(200))
        {
            var panel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };

            panel.Children.Add(new TextBlock
            {
                Text = $"{edge.From}  —{edge.Kind}→  {edge.To}",
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                TextWrapping = TextWrapping.Wrap,
            });

            // The basis rides with the edge rather than sitting behind a tooltip: a claim about the
            // user's code that hides its reason is one they cannot disagree with.
            panel.Children.Add(new TextBlock
            {
                Text = edge.Basis,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                Margin = new Thickness(12, 1, 0, 0),
            });

            var box = new Border
            {
                BorderBrush = new SolidColorBrush(
                    status == VerificationStatus.Verified
                        ? Color.FromRgb(0x4C, 0x9A, 0x6A)
                        : Color.FromRgb(0x9A, 0x86, 0x4C)),
                BorderThickness = new Thickness(2, 0, 0, 0),
                Padding = new Thickness(8, 2, 0, 2),
                Child = panel,
            };

            AutomationProperties.SetName(box,
                $"{status}. {edge.From} {edge.Kind} {edge.To}. {edge.Basis}");

            _body.Children.Add(box);
        }

        if (edges.Count > 200)
        {
            _body.Children.Add(Muted($"{edges.Count - 200} more not shown."));
        }
    }

    /// <summary>Turns a disclosure code into the sentence a user can act on.</summary>
    /// <remarks>
    /// The code is kept alongside the words. It is what the tests and the store speak, and a UI that
    /// showed only prose would leave a user unable to search for the thing they were just told.
    /// </remarks>
    private static string Explain(string code) => code switch
    {
        "sql-resource-name-unresolved" =>
            "sql-resource-name-unresolved — a SQL resource is named by an expression this build does " +
            "not evaluate, so no table could be attributed to it.",
        _ => code,
    };

    private static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 12, 0, 4),
    };

    private static TextBlock Muted(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.7,
        Margin = new Thickness(0, 4, 0, 0),
    };
}
