using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench.Composer;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// The mockup's <c>editorerror</c> Retry (<c>session-conversation.html</c>): hidden until the host
/// reports <c>init-failed</c>, then visible and wired to <see cref="AiDe.App.Workbench.WebSurfaceHost.Retry"/>.
/// Driven through the named <c>OnHostInitFailed</c>/<c>RetryEditorAsync</c> members rather than a
/// real broken WebView2 runtime — the failure path a live runtime cannot cheaply force in CI.
/// </summary>
public sealed class TheEditorErrorRetryButtonTests
{
    private static Button Retry(ComposerSurface surface) =>
        (Button)typeof(ComposerSurface).GetField("_retry", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(surface)!;

    private static void RaiseInitFailed(ComposerSurface surface, string failure) =>
        typeof(ComposerSurface).GetMethod("OnHostInitFailed", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(surface, [failure]);

    /// <summary>
    /// Drives the button's own click handler through the private <c>RetryEditorAsync</c> method
    /// rather than a routed <c>Click</c> event: this composer is built with no window and no
    /// visual tree (the failure path is unit-level, not a rendering concern), and WPF's routed
    /// event dispatch is not reliable off-tree — the logic under test is the method, not the
    /// one-line wiring of <c>_retry.Click += …</c> to it.
    /// </summary>
    private static void ClickRetry(ComposerSurface surface) =>
        ((Task)typeof(ComposerSurface).GetMethod("RetryEditorAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(surface, [])!).GetAwaiter().GetResult();

    [Fact]
    public void TheRetryButtonIsHiddenUntilTheHostReportsInitFailed()
    {
        Sta.Run(() =>
        {
            using var surface = new ComposerSurface("composer:retry-hidden", "retry — composer");

            Assert.Equal(Visibility.Collapsed, Retry(surface).Visibility);
        });
    }

    [Fact]
    public void AnInitFailure_ShowsRetryAndNamesTheCauseInStatus()
    {
        Sta.Run(() =>
        {
            using var surface = new ComposerSurface("composer:retry-shown", "retry — composer");

            RaiseInitFailed(surface, "no WebView2 runtime here");

            Assert.Equal(Visibility.Visible, Retry(surface).Visibility);
            // The read path production code itself uses (ComposerSurface.Status, backed by the
            // _statusText field) — not the raw TextBlock.Text, whose getter does not reliably
            // reflect a status set via Inlines.Add rather than the Text setter (observed: Inlines
            // cleared to 0 and _statusText correct, TextBlock.Text still the stale value).
            Assert.Contains("could not start", surface.Status, StringComparison.Ordinal);
            Assert.Contains("no WebView2 runtime here", surface.Status, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ClickingRetry_HidesTheButtonAndClearsTheStatusBeforeRetrying()
    {
        Sta.Run(() =>
        {
            using var surface = new ComposerSurface("composer:retry-click", "retry — composer");
            RaiseInitFailed(surface, "no WebView2 runtime here");
            Assert.Equal(Visibility.Visible, Retry(surface).Visibility);

            // The button hides and the failed status clears synchronously; the host's own retry
            // attempt is a no-op here because the host never actually failed (this test drives the
            // composer's own callback directly, never WebSurfaceHost's real init-failed path).
            ClickRetry(surface);

            Assert.Equal(Visibility.Collapsed, Retry(surface).Visibility);
            Assert.Equal(string.Empty, surface.Status);
        });
    }
}
