using System.Text.Json;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// Ruling 88 condition 2, decided and measured (SH-4.2): Coding's default Bottom holds one terminal,
/// <b>collapsed</b>, and the terminal is <b>one gesture away, not started</b> — a collapsed zone is
/// absent from the projection, so no pane content is built for it and no shell process starts until
/// the rail is expanded. The measurement is the <c>terminal.start</c> record the factory writes when
/// it builds a terminal pane: none at startup, one after the expand.
/// </summary>
/// <remarks>
/// <para>The alternative — a shell started behind a rail — costs a process for a pane the
/// operator's five screenshots never showed open. What the first fact pins is the cheaper truth:
/// a <b>characterization</b> of the docking layer (green on its first run — nothing was built for
/// a collapsed zone before this slice either), kept so the decision cannot drift silently.</para>
/// <para><b>Collapse is a hide, not a close</b> (the WPF lens's finding on SH-4.2, red first): the
/// adapter used to keep only the projection's surfaces, so collapsing a zone disposed its terminal's
/// process, and expanding a zone that held a retained session document handed the factory an element
/// still parented to the old island. Collapsed-holding content is parked and given back on expand.</para>
/// </remarks>
public sealed class CollapsedBottomTests
{
    private static List<JsonElement> Records(List<string> lines, string evt) =>
        lines.Select(l => JsonDocument.Parse(l).RootElement)
            .Where(e => e.TryGetProperty("evt", out var kind) && kind.GetString() == evt)
            .ToList();

    /// <summary>Collapse after use keeps the terminal — the same instance, no stop; expand gives it back, no second start.</summary>
    [Fact]
    public void CollapsingTheBottomAfterUse_KeepsTheTerminal_AndExpandingGivesTheSameOneBack()
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;
        try
        {
            Sta.Run(() =>
            {
                using var frame = ComposedCoding.Show(1440, 900);
                var shell = frame.Shell;
                Assert.True(shell.Coding.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.BottomStackId, StackState.Docked)).Applied);
                shell.Coding.Adapter.Render();
                frame.Settle();
                var built = shell.Coding.Adapter.ContentFor("terminal-1");
                Assert.NotNull(built);
                Assert.Single(Records(lines, "terminal.start"));

                Assert.True(shell.Coding.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.BottomStackId, StackState.Collapsed)).Applied);
                shell.Coding.Adapter.Render();
                frame.Settle();
                Assert.Same(built, shell.Coding.Adapter.ContentFor("terminal-1"));   // parked, alive
                Assert.Empty(Records(lines, "terminal.stop"));

                Assert.True(shell.Coding.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.BottomStackId, StackState.Docked)).Applied);
                shell.Coding.Adapter.Render();
                frame.Settle();
                Assert.Same(built, shell.Coding.Adapter.ContentFor("terminal-1"));
                Assert.Single(Records(lines, "terminal.start"));
                Assert.Empty(Records(lines, "terminal.stop"));
                return 0;
            }, 60);
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }
    }

    private static List<JsonElement> TerminalStarts(List<string> lines) =>
        lines.Select(l => JsonDocument.Parse(l).RootElement)
            .Where(e => e.TryGetProperty("evt", out var evt) && evt.GetString() == "terminal.start")
            .ToList();

    [Fact]
    public void TheDefaultBottomsTerminal_IsNotBuiltUntilTheZoneExpands()
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;
        try
        {
            Sta.Run(() =>
            {
                using var frame = ComposedCoding.Show(1440, 900);
                var shell = frame.Shell;
                Assert.True(shell.Coding.Service.Zones.Zone(ZoneId.Bottom).Collapsed);
                Assert.Equal(["terminal-1"], shell.Coding.Service.Zones.Zone(ZoneId.Bottom).Surfaces().Select(s => s.SurfaceId));

                // Not built: no content, no process.
                Assert.Null(shell.Coding.Adapter.ContentFor("terminal-1"));
                Assert.Empty(TerminalStarts(lines));
                Assert.True(shell.Coding.Rails.RailVisible(ZoneId.Bottom), "the collapsed Bottom shows no rail — the terminal is not one gesture away");

                // The gesture: the rail expands the zone; the terminal is built now, once.
                Assert.True(shell.Coding.Service.Apply(new LayoutOperation.SetStackState(ZonesToTree.BottomStackId, StackState.Docked)).Applied);
                shell.Coding.Adapter.Render();
                frame.Settle();
                Assert.NotNull(shell.Coding.Adapter.ContentFor("terminal-1"));
                Assert.Single(TerminalStarts(lines));
                return 0;
            }, 60);
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }
    }
}
