using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// The shell names its own binary when it starts — the commit, the configuration, the theme, the
/// DPI and the window — so a defect report can be attributed before it is triaged (INV-0008, Fix D).
/// </summary>
/// <remarks>
/// <para><b>The gap this closes.</b> Three Release builds from three commits sat on one machine;
/// the operator photographed one of them and nothing tied the screenshot to a commit. The fixed
/// instance re-entered as a recurrence, and the investigation re-derived the mechanism before it
/// could read the informational version off the binary. The binary carries its commit; the shell
/// never said it. Now it does, on the normal path, with no flag.</para>
///
/// <para><b>Two proofs.</b> The record's shape is proven here through the diagnostics seam; that
/// the composed shell emits it on boot is proven by the contrast probe, which boots the real
/// <c>App</c> and reports the line it saw (<see cref="TheComposedShellEmitsItOnBoot"/>).</para>
/// </remarks>
public sealed class AppStartIsRecordedTests
{
    private static readonly Regex FortyHex = new("^[0-9a-f]{40}$", RegexOptions.Compiled);

    private static List<JsonElement> Capture(Action body)
    {
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = lines.Add;
        try { body(); }
        finally { WorkbenchDiagnostics.Sink = previous; }
        return [.. lines.Select(l => JsonDocument.Parse(l).RootElement)];
    }

    private static string? Text(JsonElement record, string property) =>
        record.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    [Fact]
    public void TheRecordNamesTheBinary_CommitConfigurationThemeDpiAndWindow()
    {
        var records = Capture(() =>
            WorkbenchDiagnostics.AppStart("Vs2013DarkTheme", new DpiScale(1.25, 1.25), 1440, 900, "Normal"));

        var start = Assert.Single(records);

        Assert.Equal("app.start", Text(start, "evt"));

        // The informational version is `1.0.0+<sha>`; the sha is the attribution the report needs.
        var version = Text(start, "version");
        Assert.NotNull(version);
        Assert.Contains("+", version, StringComparison.Ordinal);

        var commit = Text(start, "commit");
        Assert.NotNull(commit);
        Assert.Matches(FortyHex, commit);
        Assert.EndsWith("+" + commit, version, StringComparison.Ordinal);

        Assert.Contains(Text(start, "configuration"), new[] { "Debug", "Release" });
        Assert.Equal("Vs2013DarkTheme", Text(start, "theme"));

        var dpi = start.GetProperty("dpi");
        Assert.Equal(1.25, dpi.GetProperty("scaleX").GetDouble());
        Assert.Equal(1.25, dpi.GetProperty("scaleY").GetDouble());
        Assert.Equal(120, dpi.GetProperty("pixelsPerInchX").GetDouble());

        var window = start.GetProperty("window");
        Assert.Equal(1440, window.GetProperty("width").GetDouble());
        Assert.Equal(900, window.GetProperty("height").GetDouble());
        Assert.Equal("Normal", Text(window, "state"));
    }

    /// <summary>A binary built without a source revision still records itself — with <c>commit</c> null, never a made-up one.</summary>
    [Fact]
    public void AVersionWithNoRevision_RecordsNullCommit_NotAnInventedOne()
    {
        Assert.Null(WorkbenchDiagnostics.CommitOf("1.0.0"));
        Assert.Null(WorkbenchDiagnostics.CommitOf("1.0.0+dirty"));
        Assert.Null(WorkbenchDiagnostics.CommitOf(null));
        Assert.Equal("7d95f8cd6efd61a93be4af28f8cfdfca1b1273c3", WorkbenchDiagnostics.CommitOf("1.0.0+7d95f8cd6efd61a93be4af28f8cfdfca1b1273c3"));
    }

    /// <summary>
    /// The composed shell — the real <c>App</c>, booted out of process by the contrast probe — emits
    /// <c>app.start</c> on its normal path, naming the same commit the probe reads off the assembly.
    /// </summary>
    [Fact]
    public void TheComposedShellEmitsItOnBoot()
    {
        var census = ShellContrastCensusTests.Taken.Value;

        Assert.True(census.Failure is null, "the shell was not booted: " + census.Failure);
        Assert.False(string.IsNullOrEmpty(census.AppStart),
            "the composed shell booted, showed its window and wrote no app.start — the binary is unattributed again (INV-0008 Fix D)");

        var start = JsonDocument.Parse(census.AppStart!).RootElement;

        Assert.Equal("app.start", Text(start, "evt"));
        Assert.Equal(census.Version, Text(start, "version"));
        Assert.Matches(FortyHex, Text(start, "commit") ?? "");
        Assert.False(string.IsNullOrEmpty(Text(start, "theme")));
        Assert.True(start.GetProperty("window").GetProperty("width").GetDouble() > 0, "the window had no width when app.start was written — it was written before the window was shown");
    }
}
