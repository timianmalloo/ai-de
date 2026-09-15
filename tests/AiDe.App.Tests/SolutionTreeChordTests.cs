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

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds), "the chord probe hung");

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
