using System.Diagnostics;

namespace AiDe.App.Tests;

/// <summary>
/// Can the product, in the configuration it actually ships in, host a terminal at all?
/// </summary>
/// <remarks>
/// <para><b>Nothing else asks this question, and every other test is green whatever the answer.</b>
/// <c>AiDe.App</c> is a GUI application — WinExe, no console, never had one. <b>DC-014</b> recorded
/// that ConPTY does not attach a child when the host's stdio is redirected, and its control gives
/// the probe a console of its own with <c>CREATE_NEW_CONSOLE</c>. Both of those describe a console
/// application. Neither says what happens in a GUI process, which is the only configuration a user
/// ever runs.</para>
///
/// <para><b>Measured 2026-08-27, and it works:</b> a WinExe host with no console captured 291
/// characters of child output. Getting there took two wrong answers, and both are worth keeping
/// because each looked conclusive:</para>
///
/// <para>A probe that called <c>FreeConsole()</c> to <i>simulate</i> the GUI case captured nothing,
/// which read as "the product cannot host terminals at all". <c>FreeConsole()</c> does not leave a
/// process in the state one that never had a console is in, so the simulation was wrong rather than
/// the product. Then this test itself failed while the identical probe passed from a shell — the
/// difference being that a child started with <c>UseShellExecute = false</c> inherits the test
/// host's <b>redirected standard handles</b>.</para>
///
/// <para>Which yields the actual rule, narrower than DC-014's wording: what decides whether a
/// ConPTY child attaches is <b>the standard handles the host was given</b>, not whether it owns a
/// console. A GUI process launched by the shell has good ones. That is why this test shell-executes
/// — and why a stand-in for a configuration is never evidence about the configuration.</para>
///
/// <para>The probe is a separate project because its <c>OutputType</c> is the thing under test —
/// it cannot be a fixture inside a console-hosted test run.</para>
/// </remarks>
public sealed class TerminalGuiHostTests
{
    [Fact]
    public void AGuiHostWithNoConsole_StillReceivesChildOutput()
    {
        var probe = LocateProbe();
        var report = Path.Combine(Path.GetTempPath(), $"aide-gui-probe-{Guid.NewGuid():N}.log");

        try
        {
            // UseShellExecute = true is load-bearing, and the reason is the finding this test
            // exists to pin down. With `false`, the child INHERITS this test host's standard
            // handles — which are redirected, because that is what a test runner does — and the
            // ConPTY child then attaches to nothing and produces no output. Measured both ways on
            // 2026-08-27: launched by the shell it captures 291 characters; launched with inherited
            // redirected handles it captures 16, all of them ConPTY's own init sequences.
            //
            // So the operative condition is NOT "the host owns a console", as DC-014's wording
            // suggests — it is which standard handles the host was given. Shell-executing is what
            // the product experiences when a user launches it, so it is what this must reproduce.
            using var process = Process.Start(new ProcessStartInfo(probe, $"\"{report}\"")
            {
                UseShellExecute = true,
            });

            Assert.NotNull(process);
            Assert.True(process!.WaitForExit(90_000), "the GUI probe did not exit within its deadline");

            var detail = File.Exists(report) ? File.ReadAllText(report) : "(no report written)";

            Assert.True(
                process.ExitCode != 5,
                $"the probe had a console, so it did not reproduce the product's case.\n{detail}");
            Assert.True(process.ExitCode != 3, $"the session could not start.\n{detail}");
            Assert.True(
                process.ExitCode == 0,
                "a GUI-subsystem host received NO child output from a ConPTY session. Every terminal "
                + "pane in the product would be empty, and no other test in this suite would fail. "
                + $"Exit {process.ExitCode}.\n{detail}");

            // On the work, not the status code (DC-015).
            Assert.Contains("CAPTURED after", detail, StringComparison.Ordinal);
            Assert.Contains("no console (GUI subsystem)", detail, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                File.Delete(report);
            }
            catch (IOException)
            {
            }
        }
    }

    private static string LocateProbe()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.App.TerminalProbe", "bin"));
        var configuration = Directory.Exists(Path.Combine(root, "Release")) ? "Release" : "Debug";
        var candidate = Path.Combine(
            root, configuration, "net10.0-windows", "AiDe.App.TerminalProbe.exe");

        Assert.True(
            File.Exists(candidate),
            $"the GUI terminal probe was not built. Expected it at:\n  {candidate}\n"
            + "Build the solution rather than the test project alone.");

        return candidate;
    }
}
