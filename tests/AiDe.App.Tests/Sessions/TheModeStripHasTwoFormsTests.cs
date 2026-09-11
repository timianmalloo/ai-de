using System.Windows;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// MS1–MS3: the canvas mode strip has two forms, and which one it shows is driven by the mode count.
/// </summary>
/// <remarks>
/// <para><b>Ruling 45 created this state, so it needs an oracle.</b> Cutting Terminal from
/// <c>BuiltIn</c> leaves Phase 1 with exactly one canvas mode — and a tab strip holding one tab is a
/// control that offers a choice nobody has. MS2 says the one-mode form is the pane title, in the
/// treatment every other pane header uses, so the canvas reads as native rather than as a tab bar
/// with one tab.</para>
///
/// <para><b>MS1 is the load-bearing half and the easiest to lose.</b> The strip is one row whose
/// <i>geometry</i> never changes; only its <i>population</i> does. A transition that also moved the
/// canvas would make registering a mode a layout change, and the whole point of Ruling 22's data
/// rows is that registering a mode edits nothing.</para>
///
/// <para><b>The second mode is registered, not shipped.</b> That is Ruling 22's clause re-proven and
/// Ruling 45's cut respected in one construction: the strip reaches its two-or-more form because
/// something appended a row, which is exactly how Terminal will come back.</para>
/// </remarks>
[Collection(CanvasModes.Name)]
public sealed class TheModeStripHasTwoFormsTests
{
    private const string SessionId = "20260911T150000Z-strip";
    private const string Title = "Front door";

    private static SessionDocumentViewModel Model(params string[] modes) =>
        new(SessionId, Title, Path.GetTempPath(), modes);

    [Fact]
    public void AtOneModeTheStripIsThePaneTitle_AndThereIsNoSplitControl() => Sta.Run(() =>
    {
        // The shipped Phase-1 set, read from the catalog rather than spelled out: if a row is ever
        // added, this case should stop being about one mode rather than quietly keep passing.
        Assert.Single(CanvasModeCatalog.BuiltIn);

        using var document = new SessionDocumentSurface(
            Model([.. CanvasModeCatalog.BuiltIn.Select(m => m.ModeId)]));

        Assert.Equal(Title, document.ModeStripTitle);
        Assert.Empty(document.ModeTabs);

        // Collapsed, not merely disabled. A disabled control announcing a capability the product
        // does not have is the placeholder AR3 removes from the rail; it is the same defect here.
        Assert.Equal(Visibility.Collapsed, document.SplitControl.Visibility);
    });

    [Fact]
    public void AtTwoModesTheStripIsTabs_AndTheSplitControlAppears() => Sta.Run(() =>
    {
        using var second = new TerminalModeForTests();

        using var document = new SessionDocumentSurface(
            Model(CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId));

        Assert.Null(document.ModeStripTitle);
        Assert.Equal(2, document.ModeTabs.Count);
        Assert.Contains("Console", document.ModeTabs);

        // MS3 — bound to the mode count, not hard-coded. Nothing in the document was edited to make
        // this control arrive; a row was registered.
        Assert.Equal(Visibility.Visible, document.SplitControl.Visibility);
        Assert.True(document.SplitControl.IsEnabled);
    });

    /// <summary>
    /// MS1 — the population changes and the geometry does not.
    /// </summary>
    /// <remarks>
    /// Measured from the rendered heights of the two forms rather than asserted from the markup,
    /// because "the row does not change size" is a claim about what is drawn. Both documents are
    /// laid out at the same width so the comparison is of the strip and not of the window.
    /// </remarks>
    [Fact]
    public void NeitherFormIsADegradedVersionOfTheOther() => Sta.Run(() =>
    {
        static double StripHeight(SessionDocumentSurface document)
        {
            document.Measure(new Size(900, 600));
            document.Arrange(new Rect(0, 0, 900, 600));
            document.UpdateLayout();

            var header = ThemeProbe.FirstDescendant<System.Windows.Controls.DockPanel>(
                document, d => d.MinHeight > 0)!;

            Assert.True(header is not null,
                "the mode strip's header row was not found, so this case measured nothing (DC-016)");

            return header!.ActualHeight;
        }

        double one;
        double many;

        using (var single = new SessionDocumentSurface(Model(CanvasModeCatalog.ConsoleModeId)))
        {
            one = StripHeight(single);
        }

        using (new TerminalModeForTests())
        using (var pair = new SessionDocumentSurface(
            Model(CanvasModeCatalog.ConsoleModeId, CanvasModeCatalog.TerminalModeId)))
        {
            many = StripHeight(pair);
        }

        Assert.True(one > 0, "the one-mode strip rendered at zero height");
        Assert.Equal(one, many, 1);
    });
}
