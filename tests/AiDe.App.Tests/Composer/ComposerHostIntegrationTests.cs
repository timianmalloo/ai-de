namespace AiDe.App.Tests.Composer;

/// <summary>
/// Security <b>C10</b> and <b>C12</b>'s runtime halves, in the <b>real</b> WebView2 control: the page
/// can reach exactly one document, the settings floor really applied, the network is unreachable,
/// host objects are undefined — <b>and the editor still renders under the shipped policy</b>.
/// </summary>
/// <remarks>
/// <para><b>Why it is out of process.</b> The same reason the canvas-focus and web-host cases are:
/// the claim needs a real window with a real <c>WebView2</c> in it, which a <c>dotnet test</c> host
/// does not reliably provide (<b>DC-014</b>).</para>
///
/// <para><b>Why it is not a headless browser.</b> The question is about the control this shell hosts,
/// under the policy this shell ships. Proving it somewhere else would be a control that cannot fail
/// for the reason it exists (<b>DC-016</b>).</para>
///
/// <para><b>Its absence FAILS rather than skips</b> (<b>DC-012</b>): a missing probe or a missing
/// WebView2 runtime is a broken environment, and this says so loudly.</para>
/// </remarks>
public sealed class ComposerHostIntegrationTests
{
    [Fact]
    public void TheComposerRendersUnderItsPolicyAndCanReachExactlyOneDocument()
    {
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the composer probe was not built at {probe}");

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)TimeSpan.FromMinutes(4).TotalMilliseconds), "the composer probe hung");

        Assert.True(
            process.ExitCode == 0,
            $"the composer probe failed with exit {process.ExitCode}. {stdout} {stderr}");

        // NON-VACUITY. Each of these lines is a measurement the probe could only print after the
        // thing it describes actually happened.
        Assert.Contains("the composer page rendered under its policy", stdout, StringComparison.Ordinal);
        Assert.Contains("hostObject resolution: rejected", stdout, StringComparison.Ordinal);
        Assert.Contains("fetch: rejected", stdout, StringComparison.Ordinal);
        Assert.Contains("editors: 1", stdout, StringComparison.Ordinal);
        Assert.Contains("fenced: true", stdout, StringComparison.Ordinal);
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
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.App.ComposerProbe", "bin"));

        return Path.Combine(root, configuration, "net10.0-windows", "AiDe.App.ComposerProbe.exe");
    }
}
