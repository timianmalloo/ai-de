namespace AiDe.App.Tests;

/// <summary>
/// Physical Ctrl+Enter on the Solution tree. RaiseEvent cannot set <c>Keyboard.Modifiers</c>
/// (N7 F12); the testhost cannot take the foreground (DC-014). The GUI probe is the oracle.
/// </summary>
public sealed class SolutionTreeChordTests
{
    [Fact]
    public void CtrlEnter_OnAFileArtifact_RequestsRevealInGraph()
    {
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the solution-tree chord probe was not built at {probe}");

        var occupant = DesktopHold.Occupant();
        Assert.True(
            occupant is null,
            "Ruling 115 desktop hold is occupied — one shown-window/UIA run at a time. Occupant: "
            + occupant);

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        DesktopHold.AnnounceStart("grok-uv-ctrl-enter", process.Id);
        string stdout;
        string stderr;
        try
        {
            stdout = process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();
            Assert.True(process.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds), "the chord probe hung");
        }
        finally
        {
            DesktopHold.AnnounceEnd("grok-uv-ctrl-enter", process.Id, process.HasExited ? process.ExitCode : -1);
        }

        Assert.True(
            process.ExitCode == 0,
            $"Ctrl+Enter probe failed with exit code {process.ExitCode}. {stdout} {stderr}");
        Assert.Contains("ctrl+enter reveal", stdout, StringComparison.Ordinal);
    }

    private static string ProbePath()
    {
        var configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.App.SolutionTreeProbe", "bin"));
        return Path.Combine(root, configuration, "net10.0-windows", "AiDe.App.SolutionTreeProbe.exe");
    }
}
