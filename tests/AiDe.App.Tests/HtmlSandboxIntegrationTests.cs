using System.Text.RegularExpressions;
using AiDe.App.Workbench;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

/// <summary>
/// <b>Ruling 93, conditions (2) and (4):</b> the reader's HTML sandbox, asserted against a real
/// WebView2 — a page with a <c>&lt;script&gt;</c>, a network image and a meta refresh is shown in
/// the shipped <see cref="HtmlSandboxHost"/> and no script runs, no request leaves, no navigation
/// happens — and the WebView2 private-bytes deltas the probe measures, recorded here.
/// </summary>
/// <remarks>
/// <para><b>Why out of process.</b> The settings, the request filter and the navigation gate are
/// facts about the browser, and a <c>dotnet test</c> host does not reliably provide one (DC-014);
/// the composer probe is the sixth instance of the build-order edge (DC-023) and already hosts
/// the real composer, which the private-bytes comparison needs beside the reader.</para>
///
/// <para><b>Its absence must fail, not skip</b> (DC-012), and the probe carries its own positive
/// control: the same page with script enabled shows the stamp, or the run is refused as vacuous
/// (DC-157).</para>
/// </remarks>
public sealed class HtmlSandboxIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public void TheReadersHtmlSandboxHolds_NoScriptNoNetworkNoNavigation_AndItsPrivateBytesAreRecorded()
    {
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the composer probe was not built at {probe}");

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe, "--html-sandbox")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds), "the html-sandbox probe hung");

        foreach (var line in stdout.Split('\n').Where(l => !l.StartsWith("diag:", StringComparison.Ordinal)))
        {
            output.WriteLine(line.TrimEnd());
        }

        Assert.True(
            process.ExitCode == 0,
            $"the reader's HTML sandbox did not hold: exit {process.ExitCode}. {stderr}");

        Assert.Contains("positive control (script enabled): #ran present = true", stdout, StringComparison.Ordinal);
        Assert.Contains("#ran present = false", stdout, StringComparison.Ordinal);
        Assert.Contains("the reader's HTML sandbox held", stdout, StringComparison.Ordinal);

        // The measurement is recorded, never asserted against a remembered magnitude (DC-107):
        // the Proof Pack reads the numbers; this only refuses a run that produced none.
        Assert.Matches(new Regex(@"composer delta: [\d,]+ bytes"), stdout);
        Assert.Matches(new Regex(@"reader delta \(the sandbox host alone, a third WebView2\): [\d,]+ bytes"), stdout);
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
