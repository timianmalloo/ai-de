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
/// The alternative — a shell started behind a rail — costs a process for a pane the operator's five
/// screenshots never showed open, and today's adapter would end that process on the next collapse
/// anyway (a collapsed zone's content is disposed with the projection; a finding, not this slice's
/// scope). What this pins is the cheaper truth.
/// </remarks>
public sealed class CollapsedBottomTests
{
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
