using System.Text.Json;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// A terminal pane that is disposed writes a <c>terminal.stop</c> line beside the
/// <c>terminal.start</c> it wrote when it was built (INV-0010).
/// </summary>
/// <remarks>
/// <para><b>The one-sided log.</b> <c>WorkbenchDiagnostics.TerminalStart</c> records the launch
/// decision, on the normal path, with no flag — and nothing records the end. An operator holding a
/// process list and a log that says "started, started, started" cannot tell a pane that closed
/// from one that leaked, and neither can the census that reads the log for a creation-time match.
/// The stop line is the other half of the pair: written at the TOP of <c>Dispose</c> (the attempt,
/// the idiom <c>SessionDisposalSignal</c> uses), carrying the surface id so the two lines join.</para>
///
/// <para><b>Red first</b>: fails until the pane writes it.</para>
/// </remarks>
public sealed class TerminalSurfaceStopLineTests
{
    [Fact]
    public void ADisposedTerminalPane_WritesATerminalStopLineForItsSurface()
    {
        const string surfaceId = "terminal#inv0010";

        var lines = Sta.Run(() =>
        {
            var captured = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = captured.Add;

            try
            {
                // The product's construction path, so the surface under test is the one the shell
                // builds — and its Dispose is the one a closed tab and (if anything did) a closed
                // window would call.
                var content = new SurfaceContentFactory(queries: null)
                    .Create(new Surface(surfaceId, "terminal", "Terminal")) as IDisposable;

                content?.Dispose();
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }

            return captured;
        });

        var records = lines.Select(l => JsonDocument.Parse(l).RootElement).ToList();

        var start = Assert.Single(records, r => Evt(r) == "terminal.start" && Surface(r) == surfaceId);
        _ = start;

        // THE OTHER HALF. Same surface id, so a reader can pair the two and a census can subtract.
        var stop = Assert.Single(records, r => Evt(r) == "terminal.stop");
        Assert.Equal(surfaceId, Surface(stop));
    }

    private static string? Evt(JsonElement r) =>
        r.TryGetProperty("evt", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? Surface(JsonElement r) =>
        r.TryGetProperty("surface", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
