using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// <b>INV-0007, finding 1, in the cheap ring.</b> The composer sizes its <b>writer first</b>: the
/// editor host is never smaller than the read-only compiled view, and the compiled view keeps to at
/// most <see cref="ComposerSurface.CompiledShareCeiling"/> of the composer, scrolling inside that —
/// at the height the operator's un-maximized document had (485px) and at the ~1000px their
/// screenshot showed, with the ~30-line compiled text they had.
/// </summary>
/// <remarks>
/// <para>DC-136's control in the fast ring (observed red there: 0px/465px at 485, 421px/465px at
/// 1000); the shell probe (<c>ComposerHostIntegrationTests</c>) proves the same rule in the real
/// docking host with a real browser. This one also reads the <c>composer.layout</c> diagnostic the
/// surface emits and checks it against the tree it was read from — the log must not disagree with
/// the screen (E12).</para>
///
/// <para><b>No browser is started.</b> The surface is measured detached from any window, so
/// <c>Loaded</c> never fires and the WebView2 has no HWND; its arranged height is what the layout
/// gave the editor's slot, which is exactly the quantity the operator lost.</para>
/// </remarks>
public sealed class TheWriterKeepsItsRoomTests
{
    private const double Width = 495;

    /// <summary>INV-0007's stated ceiling, held here so a drift in the product's constant is a red, not a re-definition.</summary>
    private const double Ceiling = 0.35;

    /// <summary>
    /// Two refusals on one line, as the status shows them — five lines at the composer's width. At
    /// 485px the share ceiling leaves the editor ~33px of slack; this chrome takes ~64px of it.
    /// </summary>
    private const string WrappedStatus =
        "the send was refused: no write scope could be derived from this draft, so the lane would have no seam monitor. "
        + "Reference the files or directories this run may write (the lease refused to be constructed because nothing was derivable).  "
        + "tier: the value must be one of T0, T1, T2.  fan_out_cap: the value must be a whole number of sub-agents, zero or more.";

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    [Theory]
    [InlineData(485, false)]
    [InlineData(1000, false)]
    [InlineData(485, true)]
    public void TheEditorHostIsNeverSmallerThanTheCompiledViewAndTheCompiledViewKeepsToItsCeiling(double height, bool statusWraps)
    {
        Assert.Equal(Ceiling, ComposerSurface.CompiledShareCeiling);

        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;
            var root = Path.Combine(Path.GetTempPath(), "aide-writer-room", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var surface = Build(root);
                if (statusWraps)
                {
                    // The boundary the arithmetic hides: at 485px the fixed chrome leaves ~33px of
                    // slack, and a refusal that wraps to four lines takes more than that.
                    surface.ShowFieldRefusal("lease", WrappedStatus);
                }

                surface.Measure(new Size(Width, height));
                surface.Arrange(new Rect(0, 0, Width, height));
                surface.UpdateLayout();

                var editor = Logical<WebView2>(surface).Single();
                var compiled = Logical<TextBox>(surface).Single(box => box.IsReadOnly);

                // NON-VACUITY: the compiled text really is the ~30 lines the operator had, and the
                // box was laid out at a real height — a 0px reader would make "writer >= reader" free.
                Assert.True(surface.CompiledView.Count(c => c == '\n') >= 28, "the compiled view is not the ~30 lines the operator had");
                Assert.True(compiled.ActualHeight >= 90, $"the compiled box was laid out at {compiled.ActualHeight:F0}px");
                Assert.Equal(height, surface.ActualHeight, 0.5);

                var share = compiled.ActualHeight / surface.ActualHeight;
                Assert.True(
                    editor.ActualHeight >= compiled.ActualHeight,
                    $"the writer is smaller than the reader: editor host {editor.ActualHeight:F0}px, compiled view {compiled.ActualHeight:F0}px of the composer's {surface.ActualHeight:F0}px");
                Assert.True(
                    share <= Ceiling,
                    $"the compiled view takes {share:P0} of the composer; the ceiling is {Ceiling:P0}");

                // AND THE READER GETS ITS SHARE. The content is 465px, above any ceiling here, so the
                // box must sit AT the rule — its share of the height, or half of what the chrome
                // leaves, whichever binds — never collapsed to its MinHeight (the opposite starvation).
                var ruled = Math.Min(Math.Floor(height * Ceiling), Math.Floor((editor.ActualHeight + compiled.ActualHeight) / 2));
                Assert.True(
                    compiled.ActualHeight >= ruled - 1,
                    $"the compiled view was starved to {compiled.ActualHeight:F0}px; the rule gives it {ruled:F0}px");

                // THE LOG AGREES WITH THE SCREEN, and there is ONE line for one layout: the ceiling
                // is set before the children are measured, so no pass ever arranged the editor at 0px
                // and no line ever said so.
                var layout = lines.Where(l => l.Contains("\"evt\":\"composer.layout\"", StringComparison.Ordinal)).ToList();
                Assert.True(layout.Count == 1, $"expected one composer.layout line at first layout, read {layout.Count}: {string.Join(" | ", layout)}");

                using var last = JsonDocument.Parse(layout[^1]);
                Assert.Equal("composer:s-room", last.RootElement.GetProperty("surface").GetString());
                Assert.Equal(editor.ActualHeight, last.RootElement.GetProperty("editor").GetProperty("height").GetDouble(), 0.5);
                Assert.Equal(compiled.ActualHeight, last.RootElement.GetProperty("compiled").GetProperty("height").GetDouble(), 0.5);
                Assert.Equal(surface.ActualHeight, last.RootElement.GetProperty("composer").GetProperty("height").GetDouble(), 0.5);
                Assert.Equal(0, last.RootElement.GetProperty("inputs").GetInt64());
                Assert.False(last.RootElement.GetProperty("loaded").GetBoolean());
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
                Directory.Delete(root, recursive: true);
            }
        });
    }

    /// <summary>
    /// <b>Null, never a plausible zero (IO11).</b> A part whose arrange is not valid is reported as
    /// <c>null</c>, and the sibling that is still valid keeps its number — so a reader can tell "not
    /// laid out" from "laid out at 0px", which is the defect.
    /// </summary>
    [Fact]
    public void APartWhoseArrangeIsNotValidIsReportedAsNullNotZero()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;
            var root = Path.Combine(Path.GetTempPath(), "aide-writer-room", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var surface = Build(root);
                surface.Measure(new Size(Width, 485));
                surface.Arrange(new Rect(0, 0, Width, 485));
                surface.UpdateLayout();

                var compiled = Logical<TextBox>(surface).Single(box => box.IsReadOnly);
                Assert.True(compiled.IsArrangeValid);

                compiled.InvalidateArrange();
                Assert.False(compiled.IsArrangeValid);
                surface.EmitLayout();

                using var last = JsonDocument.Parse(lines.Last(l => l.Contains("\"evt\":\"composer.layout\"", StringComparison.Ordinal)));
                Assert.Equal(JsonValueKind.Null, last.RootElement.GetProperty("compiled").GetProperty("height").ValueKind);
                Assert.Equal(JsonValueKind.Number, last.RootElement.GetProperty("editor").GetProperty("height").ValueKind);
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
                Directory.Delete(root, recursive: true);
            }
        });
    }

    /// <summary>The goal-block draft the shell probe seeds: three prose answers, ~30 compiled lines.</summary>
    private static ComposerSurface Build(string root)
    {
        var surface = new ComposerSurface("composer:s-room", "s-room — composer");
        surface.Draft.SwitchTo(ComposerShape.GoalBlock);
        surface.Draft.SetGoalValue(GoalBlockFields.GoalKey, "Investigate why the composer accepts no typing.\nName the cause.\nStop before the fix.");
        surface.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "A red test exists.\nThe INV is written.");
        surface.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "The vendored bundle.\nThe test-log pollution.");

        surface.Configure(
            new SessionConfig("s-room", "room", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
            new ComposerSendContext(
                RepositoryRoot: root,
                DataDirectory: root,
                AdapterInstallRoot: root,
                EngineId: "claude-code",
                Model: "sonnet",
                AccountLabel: "max-personal",
                TaskClass: "implement",
                ProofPackArtifacts: [],
                Providers: []),
            ComposerFields.GoalBlock(),
            new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));

        return surface;
    }

    /// <summary>The logical descendants of one type — independent of whether a template has been applied.</summary>
    private static IEnumerable<T> Logical<T>(DependencyObject node) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node))
        {
            if (child is not DependencyObject element)
            {
                continue;
            }

            if (element is T match)
            {
                yield return match;
            }

            foreach (var found in Logical<T>(element))
            {
                yield return found;
            }
        }
    }
}
