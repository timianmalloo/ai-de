using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;
using AiDe.Core.Workbench;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// The evidence pane renders the whole row, including why it matched.
/// </summary>
/// <remarks>
/// <para><b>What was wrong.</b> The pane built its <c>ListBox</c> with
/// <c>DisplayMemberPath = nameof(EvidenceRow.DisplayLabel)</c> and no item template. A
/// <c>DisplayMemberPath</c> renders exactly one property, so the pane showed the label and silently
/// dropped <c>NodeKind</c>, <c>Confidence</c> and <c>Evidence</c> — three fields the row computes
/// and nothing displayed.</para>
///
/// <para><b>And the fix that could not work.</b> <c>EvidenceRow</c> was given an <c>Evidence</c>
/// field and an <c>AccessibleName</c> that reads it, and the record was then correct while the pane
/// was unchanged. A reviewer opening <c>EvidencePaneViewModel.cs</c> would have concluded the fix
/// landed. The binding was the defect, one layer out from where it was looked for — which is this
/// file's own subject arriving inside its own fix.</para>
///
/// <para><b>The accessible name was on the LIST, not its items</b>, so the computed
/// <c>AccessibleName</c> written to carry the reason was read by nothing, and a screen reader got
/// the same single property the eye did. Both halves are asserted here, because a surface with two
/// rendering paths can keep the bound in one and drop it in the other.</para>
/// </remarks>
public sealed class EvidencePaneRendersItsRowsTests
{
    private sealed class OneAttributeMatch : FakeWorkspaceQueries
    {
        public override Task<FindResult> FindAsync(
            string term, int maxResults, CancellationToken cancellationToken) =>
            Task.FromResult(new FindResult(
                [new FindMatch(
                    "app.Element", "typescript-class", "Element",
                    AuthorshipOrigin.RepositoryArtifact,
                    AiDe.Core.Store.NodeMatchKind.Attribute,
                    "has_member = + addEventListener()")],
                new ResultBounds(0, 0, 1024, 1, 0, 0, 0, false, null),
                "rev-1"));
    }

    /// <summary>Every string the pane put on screen, and every accessible name it set.</summary>
    private static (string Text, string Names) Render()
    {
        var text = new System.Text.StringBuilder();
        var names = new System.Text.StringBuilder();

        return OnSta(() =>
        {
            var content = new SurfaceContentFactory(new OneAttributeMatch())
                .Create(new Surface("evidence", "view", "Explore"));

            var window = new Window
            {
                Content = content, Width = 700, Height = 500,
                Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
            };

            window.Show();

            // The rows arrive from an un-awaited load; let the dispatcher drain it.
            for (var i = 0; i < 40; i++)
            {
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                Thread.Sleep(25);
                window.UpdateLayout();
            }

            Walk(content, text, names);
            window.Close();

            return (text.ToString(), names.ToString());
        });
    }

    private static void Walk(DependencyObject node, System.Text.StringBuilder text, System.Text.StringBuilder names)
    {
        if (node is TextBlock block)
        {
            text.Append(' ').Append(block.Text);

            var name = AutomationProperties.GetName(block);
            if (!string.IsNullOrEmpty(name)) names.Append(' ').Append(name);
        }

        if (node is ContentControl { Content: DependencyObject inner }) Walk(inner, text, names);

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            Walk(VisualTreeHelper.GetChild(node, i), text, names);
        }
    }

    private static T OnSta<T>(Func<T> body)
    {
        T result = default!;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try { result = body(); }
            catch (Exception ex) { failure = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "the STA thread did not finish");

        if (failure is not null) throw failure;

        return result;
    }

    [Fact]
    public void TheRowSaysWhyItMatched()
    {
        var (text, _) = Render();

        // The DC-016 guard first: if no row rendered at all, every assertion below is about nothing.
        Assert.Contains("Element", text, StringComparison.Ordinal);

        Assert.Contains("addEventListener", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRowShowsItsKind()
    {
        // One of the three fields DisplayMemberPath dropped. Asserted so the template cannot quietly
        // shrink back to one property.
        var (text, _) = Render();

        Assert.Contains("typescript-class", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheAccessibleNameIsOnTheRowAndCarriesTheReasonToo()
    {
        // The second rendering path. A surface can keep the bound in one and drop it in the other,
        // and the accessible name is the half nobody looks at while testing a visual change.
        var (_, names) = Render();

        Assert.Contains("Element", names, StringComparison.Ordinal);
        Assert.Contains("addEventListener", names, StringComparison.Ordinal);
    }
}
