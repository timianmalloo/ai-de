using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// <c>P2-FOCUS-01</c> and <c>P2-FOCUS-03</c> against a <b>real window and a real WebView2</b>.
/// </summary>
/// <remarks>
/// <para><b><c>P2-FOCUS-03</c> is the keyboard-trap test, and the one that must never be allowed to
/// rot.</b> Tab traversal cannot leave the canvas, so the page's boundary handlers are the only way
/// out — a page that loses them strands the user inside the graph with no keyboard route back.</para>
///
/// <para><b>It could not be written against the fake.</b> The host-side policy is covered by
/// <c>CanvasFocusRouterTests</c>, but a keyboard-trap test that drives a stub proves the stub posts
/// the message — the very thing a real page might not do. A control that cannot fail for the reason
/// it exists is defect class <b>DC-016</b>, so this one needs the real browser or it needs to not
/// exist.</para>
///
/// <para><b>Its absence must FAIL, not skip.</b> A skipped keyboard-trap test reports green while
/// proving nothing (<b>DC-012</b>). If the WebView2 runtime is missing, that is a broken test
/// environment and this says so loudly rather than quietly passing.</para>
/// </remarks>

public sealed class CanvasFocusIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Runs <paramref name="work"/> on the shared pumping harness (<see cref="Sta.Pump"/>).
    /// </summary>
    /// <remarks>
    /// <para>The canvas sits BETWEEN two focusable buttons, which is what gives Tab somewhere to go
    /// — a focus-boundary test with nothing on either side cannot tell "left the canvas" from
    /// "nowhere to leave to".</para>
    ///
    /// <para>The window is on-screen and activated, unlike the shared default: <c>SendInput</c>
    /// goes to the FOREGROUND window, so an off-screen or background one would swallow every
    /// synthesized key and the test would fail for a reason with nothing to do with the page.</para>
    /// </remarks>
    private static void OnUiThread(Func<Window, CanvasSurface, Task> work) =>
        Sta.Pump<CanvasSurface>(
            create: () => new CanvasSurface("canvas-1", "Graph"),
            body: work,
            content: canvas =>
            {
                var panel = new StackPanel();
                panel.Children.Add(new Button { Content = "before", Focusable = true });
                panel.Children.Add(canvas);
                panel.Children.Add(new Button { Content = "after", Focusable = true });
                return panel;
            },
            configure: window =>
            {
                window.Left = 0;
                window.Top = 0;
                window.ShowActivated = true;
            });

    private static async Task<bool> WaitForReady(CanvasSurface canvas)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!canvas.Ready && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        return canvas.Ready;
    }

    [Fact]
    public void P2FOCUS01_FocusLandsInsideTheCanvas_VerifiedByReadingFocusBack() =>
        OnUiThread(async (_, canvas) =>
        {
            Assert.True(await WaitForReady(canvas), "the canvas page never finished loading");

            // Asserted through the seam the product uses, and the seam reads GetFocus back rather
            // than trusting SetFocus's return value.
            Assert.True(canvas.FocusTarget.IsReady, "the canvas reported no window handle");
            Assert.True(canvas.FocusTarget.TryFocus(), "focus did not land inside the canvas");
        });

    [Fact]
    public void P2FOCUS03_TabbingOffTheEndOfTheCanvasReturnsFocusToWpf_TheKeyboardTrapTest()
    {
        // Run OUT OF PROCESS. The canvas needs a real window and a real WebView2, and the keys must
        // reach the browser — neither of which a `dotnet test` host provides. Measured, not assumed:
        // in-process the page reported activeElement="first" (focus HAD landed) with zero Tab
        // keydowns seen. Defect class DC-014, same control as the ConPTY case.
        var probe = ProbePath();
        Assert.True(File.Exists(probe), $"the canvas probe was not built at {probe}");

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(probe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit((int)TimeSpan.FromMinutes(3).TotalMilliseconds), "the canvas probe hung");

        // 4 is the trap: the page never posted focus.leave, so a user who entered the graph could
        // not get out with the keyboard.
        Assert.True(
            process.ExitCode == 0,
            $"P2-FOCUS-03 failed with exit code {process.ExitCode}. {stdout} {stderr}");

        Assert.Contains("focus left the canvas", stdout, StringComparison.Ordinal);
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
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.App.CanvasProbe", "bin"));

        return Path.Combine(root, configuration, "net10.0-windows", "AiDe.App.CanvasProbe.exe");
    }

}
