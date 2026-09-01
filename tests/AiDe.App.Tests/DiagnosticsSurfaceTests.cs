using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The Diagnostics pane renders the last re-index's coverage — the "not analysed" disclosures folded
/// and grouped by category — plus daemon state, as a scrollable panel rather than a status-strip wall.
/// Host-side WPF, so it runs on an STA thread.
/// </summary>
public sealed class DiagnosticsSurfaceTests
{
    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { work(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is Xunit.Sdk.XunitException) throw failure;   // the message IS the finding (DC-078)

        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
    }

    [Fact]
    public void NewSurface_ShowsTheEmptyState()
    {
        OnSta(() =>
        {
            var s = new DiagnosticsSurface();
            Assert.True(s.IsEmpty);
            Assert.False(s.HasIndexSummary);
            Assert.Equal(0, s.DisclosureLineCount);
        });
    }

    [Fact]
    public void Show_RendersTheIndexSummaryAndOneLinePerFoldedDisclosure()
    {
        OnSta(() =>
        {
            var s = new DiagnosticsSurface();
            var report = new DiagnosticsReport(
                IndexSummary: "Indexed 64 of 64 scope(s) · 29,314 assertion(s)",
                Disclosures:
                [
                    "knowledge-headings-not-analysed (4,471 heading(s), across 39 scope(s))",
                    "knowledge-inline-code-not-resolved (2,161 span(s), across 39 scope(s))",
                    "python-standard-library-not-indexed (78 import(s))",
                    "calls-outside-this-repository (14,262 call(s))",
                ],
                FailedScopes: 0,
                Daemon: "Daemon version: 0.3.0. Rollback: unavailable.");

            s.Show(report);

            Assert.False(s.IsEmpty);
            Assert.True(s.HasIndexSummary);
            Assert.Equal(4, s.DisclosureLineCount);   // one line per folded disclosure class, grouped by category
        });
    }

    [Fact]
    public void Show_WithDaemonOnly_IsNotEmpty_ButHasNoIndexSummary()
    {
        OnSta(() =>
        {
            var s = new DiagnosticsSurface();
            s.Show(new DiagnosticsReport(null, [], 0, "Daemon version: 0.3.0."));

            Assert.False(s.IsEmpty);           // the daemon section is shown
            Assert.False(s.HasIndexSummary);   // …but no index has run
            Assert.Equal(0, s.DisclosureLineCount);
        });
    }

    [Fact]
    public void Show_WithNothing_FallsBackToEmpty()
    {
        OnSta(() =>
        {
            var s = new DiagnosticsSurface();
            s.Show(new DiagnosticsReport(null, [], 0, null));
            Assert.True(s.IsEmpty);
        });
    }

    [Fact]
    public void ShowLoadingAndError_DoNotThrow_AndClearContent()
    {
        OnSta(() =>
        {
            var s = new DiagnosticsSurface();
            s.Show(new DiagnosticsReport("Indexed 1 of 1 scope(s) · 5 assertion(s)", ["python-x (1)"], 0, null));
            Assert.True(s.HasIndexSummary);

            s.ShowLoading();
            Assert.False(s.HasIndexSummary);
            Assert.Equal(0, s.DisclosureLineCount);

            s.ShowError("daemon closed the connection");   // does not throw
        });
    }
}
