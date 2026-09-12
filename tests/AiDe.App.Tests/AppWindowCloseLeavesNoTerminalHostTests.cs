using System.Diagnostics;
using AiDe.Tests.Shared;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

/// <summary>
/// Closing the App's window, the way the operator does, leaves no <c>conhost.exe --headless</c>
/// behind five seconds later (INV-0010).
/// </summary>
/// <remarks>
/// <para><b>The real binary, the real close.</b> <c>MainWindow.Closed</c> runs
/// <c>WorkbenchShell.Dispose</c>, which disposes no <c>TerminalSurface</c>; the terminal the shell
/// opens on workspace attach (<c>terminal-1</c>, PowerShell integration — the operator's log shows
/// one per <c>app.start</c>) therefore ends only because the process does. This launches
/// <c>AiDe.App.exe</c> against a throw-away workspace (<c>AIDE_WORKSPACE_ROOT</c>), waits until the
/// App owns a headless console host, sends the window <c>WM_CLOSE</c> — the X button — and counts
/// the hosts that still name the App as parent once it is gone.</para>
///
/// <para><b>What it costs.</b> A window on the desktop for a few seconds, one daemon for the
/// throw-away workspace (detached by design, retires on its 30 s idle grace, reported by the
/// census as <c>ours-detached</c>), and <c>app.start</c>/<c>terminal.start</c> lines in the
/// operator's workbench log — the App has no cross-process diagnostics seam.</para>
///
/// <para><b>What it proves.</b> ≥ 1 host while the App runs (the key can see one), then the count
/// at +5 s. The number is the finding; the assertion is the floor.</para>
/// </remarks>
[Trait("Platform", "Windows")]
public sealed class AppWindowCloseLeavesNoTerminalHostTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ClosingTheMainWindow_LeavesNoHeadlessHostFiveSecondsLater()
    {
        var app = LocateApp();
        var workspace = Path.Combine(Path.GetTempPath(), "aide-inv0010-close-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        int liveCount;
        int afterCount;
        string afterRows;

        // UseShellExecute = true is load-bearing, for the reason TerminalGuiHostTests records:
        // launched with inherited handles the App's terminal shell reads EOF from the test host's
        // redirected stdin and exits at once (measured 2026-09-12: the shell's prompt and OSC 133
        // markers arrived on the LAUNCHING console, and no powershell.exe ever appeared among the
        // App's children). That App holds a client-less headless host, which is not the
        // operator's configuration. ShellExecute cannot take an environment block, so the
        // workspace root is set on this process and inherited.
        var previousRoot = Environment.GetEnvironmentVariable("AIDE_WORKSPACE_ROOT");
        Environment.SetEnvironmentVariable("AIDE_WORKSPACE_ROOT", workspace);
        Process process;
        try
        {
            process = Process.Start(new ProcessStartInfo(app)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(app)!,
            }) ?? throw new InvalidOperationException("the App did not start");
        }
        finally
        {
            Environment.SetEnvironmentVariable("AIDE_WORKSPACE_ROOT", previousRoot);
        }

        using var _ = process;

        try
        {
            // The App attaches the workspace on Loaded and opens its terminal then; give it a real
            // interval on a busy machine.
            var first = await ConsoleHostCensus.WaitForHeadlessHostAsync(process.Id, TimeSpan.FromSeconds(40));
            Assert.True(first.Readable, "the process table could not be read while the App ran");

            // The host appears a beat before the shell (CreatePseudoConsole, then CreateProcess);
            // settle, then take the census the assertions read.
            await Task.Delay(TimeSpan.FromSeconds(2));
            var live = ConsoleHostCensus.Take();
            Assert.True(live.Readable, "the process table could not be read while the App ran");
            var owned = live.HeadlessHostsOwnedBy(process.Id);
            liveCount = owned.Count;
            output.WriteLine($"App {process.Id} live; headless hosts it owns: {liveCount} {ConsoleHostCensus.Describe(owned)}");
            var children = live.ChildrenOf(process.Id);
            output.WriteLine($"App children while live: {ConsoleHostCensus.Describe(children)}");

            // The shell must be alive beside its host, or this is the dead-shell configuration
            // above and the count that follows measures the wrong thing.
            Assert.Contains(children, c => c.Name.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase)
                && c.CommandLine.Contains("-EncodedCommand", StringComparison.Ordinal));

            // THE OPERATOR'S GESTURE. WM_CLOSE to the main window; WPF runs Closing → Closed →
            // Shell.Dispose → shutdown on last window close.
            process.Refresh();
            var closeRequested = process.CloseMainWindow();
            output.WriteLine($"CloseMainWindow: {closeRequested}");
            Assert.True(closeRequested, "the App had no main window to close");

            var exited = process.WaitForExit((int)TimeSpan.FromSeconds(45).TotalMilliseconds);
            output.WriteLine($"App exited: {exited}" + (exited ? $" code {process.ExitCode}" : ""));
            Assert.True(exited, "the App did not exit after its window was closed");

            await Task.Delay(TimeSpan.FromSeconds(5));
            var after = ConsoleHostCensus.Take();
            Assert.True(after.Readable, "the process table could not be read after the App exited");
            var survivors = after.HeadlessHostsOwnedBy(process.Id);
            afterCount = survivors.Count;
            afterRows = ConsoleHostCensus.Describe(survivors);
            output.WriteLine($"App gone; +5s headless hosts still naming it as parent: {afterCount} {afterRows}");
            output.WriteLine($"App gone; +5s any children still naming it as parent: {ConsoleHostCensus.Describe(after.ChildrenOf(process.Id))}");
        }
        finally
        {
            if (!process.HasExited)
            {
                // Containment for the test's own child on its failure path, never a cleanup of
                // anyone else's process.
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            }

            try { Directory.Delete(workspace, recursive: true); } catch (IOException) { }
        }

        Assert.True(liveCount >= 1,
            $"the App should have owned a conhost.exe --headless for its terminal while it ran; saw {liveCount}");
        Assert.True(afterCount == 0,
            $"{afterCount} headless console host(s) still alive 5s after the App's window was closed: {afterRows}");
    }

    private static string LocateApp()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AiDe.App", "bin"));
        var configuration = Directory.Exists(Path.Combine(root, "Release")) ? "Release" : "Debug";
        var candidate = Path.Combine(root, configuration, "net10.0-windows", "AiDe.App.exe");

        Assert.True(
            File.Exists(candidate),
            $"the App was not built. Expected it at:\n  {candidate}\nBuild the solution rather than the test project alone.");

        return candidate;
    }
}
