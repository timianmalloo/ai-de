namespace AiDe.App.Tests;

/// <summary>
/// <b>F1's exit clause:</b> the vendored CodeMirror ES-module bundle renders inside the <b>real</b>
/// WebView2 control, served over a virtual host mapping.
/// </summary>
/// <remarks>
/// <para><b>Why it cannot be written in process.</b> The same reason
/// <c>CanvasFocusIntegrationTests.P2FOCUS03_TabbingOffTheEndOfTheCanvasReturnsFocusToWpf_TheKeyboardTrapTest</c>
/// runs out of process: the surface needs a real window with a real <c>WebView2</c> in it, which a
/// <c>dotnet test</c> host does not reliably provide — defect class <b>DC-014</b>.</para>
///
/// <para><b>Why headless Chromium would not do.</b> The spike already proved CodeMirror 6 runs in
/// Edge/Chromium over plain HTTP. What was never proven, and what R15 rests on, is that
/// <c>CoreWebView2.SetVirtualHostNameToFolderMapping</c> serves an ES module to the control this
/// shell actually hosts — <c>NavigateToString</c> cannot serve one at all. A test that proves the
/// bundle runs somewhere else is a control that cannot fail for the reason it exists
/// (<b>DC-016</b>).</para>
///
/// <para><b>Its absence must FAIL, not skip</b> (<b>DC-012</b>): a missing probe or a missing
/// WebView2 runtime is a broken environment, and this says so loudly.</para>
/// </remarks>
public sealed class WebAssetHostIntegrationTests
{
    [Fact]
    public void TheVendoredBundleRendersInsideTheRealWebView2HostOverTheVirtualHost()
    {
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the web-host probe was not built at {probe}");

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)TimeSpan.FromMinutes(3).TotalMilliseconds), "the web-host probe hung");

        Assert.True(
            process.ExitCode == 0,
            $"the vendored bundle did not render in the real WebView2 host: exit {process.ExitCode}. {stdout} {stderr}");

        // The editor existing is not enough. `.cm-mention-chip` is produced by the ViewPlugin that
        // lives INSIDE the bundle, so a chip on screen means the module was fetched over the virtual
        // host, parsed as an ES module, and executed — none of which an empty <div> would show.
        Assert.Contains("the vendored bundle rendered", stdout, StringComparison.Ordinal);
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
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.App.WebHostProbe", "bin"));

        return Path.Combine(root, configuration, "net10.0-windows", "AiDe.App.WebHostProbe.exe");
    }
}
