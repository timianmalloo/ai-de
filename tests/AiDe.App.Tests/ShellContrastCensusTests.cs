using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

/// <summary>
/// Every text pairing the composed shell renders clears its WCAG floor — measured by walking the
/// shell the product builds, not by constructing the controls a test remembers.
/// </summary>
/// <remarks>
/// <para><b>The floor that shipped an unreadable shell.</b> <c>ContrastFloorTests</c> went green on
/// the commit the operator photographed: "Compiled view" dim, the lease sentence dim, the status
/// paragraph dim, the tab captions dim, a white text box in a dark pane. Every one of those is
/// outside its population, because that test measures controls it constructs on a window it builds.
/// This one measures what the product composes, out of process, by booting the real
/// <c>AiDe.App.App</c> (<c>AiDe.App.ContrastProbe</c>; INV-0008).</para>
///
/// <para><b>The red output is the sweep.</b> Every failing site is a row with its ratio, its ink's
/// provenance and its mechanism, so the assertion message is the census and the census is the
/// repair list. There is no threshold above zero and no "worst N": a threshold the rules can never
/// reach is advisory by arithmetic (DC-133).</para>
///
/// <para><b>What it cannot see is listed, not skipped.</b> The omissions table names every
/// population the walk does not reach and why; a reader can check the reasons rather than assume
/// the walk was total.</para>
///
/// <para><b>One census per test run.</b> Booting the shell, opening every surface and waiting on
/// two WebView2 pages costs seconds; the probe runs once and its report is read by every fact here.</para>
/// </remarks>
public sealed class ShellContrastCensusTests(ITestOutputHelper output)
{
    /// <summary>One boot per test run; <c>AppStartIsRecordedTests</c> reads the same census.</summary>
    internal static readonly Lazy<Census> Taken = new(Take, LazyThreadSafetyMode.ExecutionAndPublication);

    [Fact]
    public void EveryTextPairingInTheComposedShellClearsItsFloor()
    {
        var census = Taken.Value;
        Report(census);

        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);

        var wpf = census.Sites.Where(s => s.Population == "wpf").ToList();

        // DC-016: a walk that measured nothing passes every floor. The named sites are the ones the
        // operator photographed; if the walk cannot find them it is not walking the product.
        Assert.True(wpf.Count > 0, "the census walked the composed shell and found no text at all");
        Assert.Contains(wpf, s => s.Text.StartsWith("Compiled view", StringComparison.Ordinal));
        Assert.Contains(wpf, s => s.Text.StartsWith("Lease:", StringComparison.Ordinal));
        Assert.Contains(wpf, s => s.Element.Contains("Compiled prompt", StringComparison.Ordinal));
        Assert.Contains(wpf, s => s.Surface.StartsWith("tab:", StringComparison.Ordinal));
        Assert.Contains(wpf, s => s.Text == "File" && s.Element.Contains("AccessText", StringComparison.Ordinal));

        var failing = wpf.Where(s => !s.Clears).OrderBy(s => s.Ratio).ToList();

        Assert.True(failing.Count == 0,
            $"{failing.Count} of {wpf.Count} text pairings in the composed shell ({census.Version}) are below their floor:"
            + Environment.NewLine + MechanismCounts(failing)
            + Environment.NewLine + Environment.NewLine + Table(failing));
    }

    /// <summary>
    /// A disabled control's ink is the disabled token — the state is carried by the ink, as the
    /// template's <c>IsEnabled=False</c> trigger sets it, not only by the ground.
    /// </summary>
    /// <remarks>
    /// The same mechanism as the accent-ground failures, seen from the other side: the trigger sets
    /// <c>TextElement.Foreground</c> on the ContentPresenter, the generated TextBlock takes the
    /// implicit style's ink instead, and "unavailable" renders exactly like "available". A contrast
    /// floor cannot see this — 13.57:1 clears — so it is asserted on provenance.
    /// </remarks>
    [Fact]
    public void ADisabledControlsInkIsTheDisabledToken()
    {
        var census = Taken.Value;

        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);

        var disabled = census.Sites.Where(s => s.Population == "wpf" && !s.Enabled).ToList();
        Assert.True(disabled.Count > 0, "the composed shell rendered no disabled text, so the disabled pairing measured nothing (DC-016)");

        var wrongInk = disabled.Where(s => !s.InkSource.StartsWith("DisabledTextBrush", StringComparison.Ordinal)).ToList();

        Assert.True(wrongInk.Count == 0,
            $"{wrongInk.Count} of {disabled.Count} disabled controls render their ENABLED ink — the IsEnabled trigger's "
            + "DisabledTextBrush never reaches the glyphs:" + Environment.NewLine + Table(wrongInk));
    }

    [Fact]
    public void EveryTextPairingInTheComposerPageClearsItsFloor()
    {
        var census = Taken.Value;

        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);

        var page = census.Sites.Where(s => s.Population == "webview").ToList();
        var unrecorded = census.Omissions.Where(o => o.Population == "webview").ToList();

        // Not a skip: a page that never initialised is the environment failing, and a quiet pass
        // here would restore the false success the census exists to remove (DC-012/DC-016).
        Assert.True(page.Count > 0,
            "no WebView2 page was measured: " + string.Join("; ", unrecorded.Select(o => $"{o.What} — {o.Reason}"))
            + Environment.NewLine + string.Join(Environment.NewLine, census.Log));

        var failing = page.Where(s => !s.Clears).OrderBy(s => s.Ratio).ToList();

        Assert.True(failing.Count == 0,
            $"{failing.Count} of {page.Count} text pairings in the hosted pages are below their floor:"
            + Environment.NewLine + Table(failing));
    }

    /// <summary>
    /// The composer page draws with the shell's tokens because the shell pushed them — every role
    /// the host sends on <c>host.init</c> is on the page's root, with the shell's value.
    /// </summary>
    /// <remarks>
    /// The fact above cannot see this: the stylesheet's fallbacks are the same values, so a page
    /// that never received the push measures identically. The stylesheet never declares a custom
    /// property, only reads one, so a property on the root's inline style is the push's footprint
    /// and nothing else's (E8: the field has a writer AND a reader, both traced).
    /// </remarks>
    [Fact]
    public void TheComposerPageDrawsWithTheTokensTheShellPushed()
    {
        var census = Taken.Value;

        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);

        Assert.True(census.ShellTheme.Count > 0, "the shell resolved no theme roles to push — ComposerPageTheme found none of its tokens in Application.Resources (TC3)");

        var missing = census.ShellTheme
            .Where(role => !census.PageTheme.TryGetValue(role.Key, out var value) || !string.Equals(value, role.Value, StringComparison.OrdinalIgnoreCase))
            .Select(role => $"{role.Key}: shell {role.Value}, page {(census.PageTheme.TryGetValue(role.Key, out var v) ? v : "(not set)")}")
            .ToList();

        Assert.True(missing.Count == 0,
            "the host's host.init theme did not reach the composer page's root — the page is drawing its "
            + "stylesheet fallbacks, not the shell's tokens:" + Environment.NewLine + string.Join(Environment.NewLine, missing)
            + Environment.NewLine + string.Join(Environment.NewLine, census.Log));

        // THE SENTINEL. The stylesheet's fallbacks equal the tokens, so equal values above could be
        // a page applying a copy of its own. The probe planted a colour no token declares on the
        // focus role before the shell composed; only the host's push can have carried it here.
        Assert.False(string.IsNullOrEmpty(census.FocusSentinel), "the probe planted no sentinel, so this fact cannot tell the push from the fallback");
        Assert.Equal(census.FocusSentinel, census.ShellTheme["--focus"], ignoreCase: true);
        Assert.Equal(census.FocusSentinel, census.PageTheme["--focus"], ignoreCase: true);
    }

    /// <summary>
    /// <c>applyTheme</c> refuses what is not a custom property with a six-digit hex value — a bad
    /// name, a <c>url()</c>, a number, a five-digit hex, a capitalised name — and leaves the root as
    /// the push left it. Exercised against the live page, not a copy of the function.
    /// </summary>
    [Fact]
    public void TheComposerPageRefusesAMalformedTheme()
    {
        var census = Taken.Value;

        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);
        Assert.NotNull(census.PageThemeAfterMalformedPush);
        Assert.True(census.PageTheme.Count > 0, "the push left nothing on the root, so there is nothing for a malformed push to disturb");

        Assert.Equal(
            census.PageTheme.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"),
            census.PageThemeAfterMalformedPush!.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));
    }

    // ───────────────────────────────────────────────────────────────── the probe ──

    /// <summary>One measured pairing — the probe's row, as the JSON contract carries it.</summary>
    internal sealed record Site(
        int Number, string Population, string Surface, string Element, string Text,
        string Ink, string InkSource, string Ground, string GroundSource,
        double Opacity, double FontSize, bool Bold, bool Enabled, string Mechanism,
        string RenderedInk, double Ratio, double Floor, bool Clears);

    internal sealed record Omission(string Population, string What, string Reason);

    internal sealed record Census(
        string Version, IReadOnlyList<Site> Sites, IReadOnlyList<Omission> Omissions, IReadOnlyList<string> Log,
        IReadOnlyDictionary<string, string> ShellTheme, IReadOnlyDictionary<string, string> PageTheme,
        string? AppStart, string? Failure)
    {
        public int AppStartCount { get; init; }
        public string? FocusSentinel { get; init; }
        public IReadOnlyDictionary<string, string>? PageThemeAfterMalformedPush { get; init; }
    }

    private sealed record ProbeReport(
        string Commit, List<Site> Sites, List<Omission> Omissions, List<string> Log,
        Dictionary<string, string>? ShellTheme, Dictionary<string, string>? PageTheme, string? AppStart,
        int AppStartCount, string? FocusSentinel, Dictionary<string, string>? PageThemeAfterMalformedPush);

    private static readonly Dictionary<string, string> Empty = new(StringComparer.Ordinal);

    private static Census Take()
    {
        var probe = ProbePath();
        if (!File.Exists(probe))
        {
            return new Census("(not built)", [], [], [], Empty, Empty, null, $"the contrast probe was not built at {probe}");
        }

        var report = Path.Combine(Path.GetTempPath(), "aide-contrast-census-" + Guid.NewGuid().ToString("N") + ".json");

        using var process = Process.Start(new ProcessStartInfo(probe)
        {
            ArgumentList = { "--report", report },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit((int)TimeSpan.FromMinutes(4).TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            return new Census("(hung)", [], [], [], Empty, Empty, null, "the contrast probe hung. " + stdout + stderr);
        }

        if (process.ExitCode != 0 || !File.Exists(report))
        {
            return new Census("(failed)", [], [], [], Empty, Empty, null, $"the contrast probe exited {process.ExitCode} without a report. {stdout} {stderr}");
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ProbeReport>(File.ReadAllText(report))
                ?? throw new InvalidDataException("the report deserialised to null");

            return new Census(parsed.Commit, parsed.Sites, parsed.Omissions, parsed.Log, parsed.ShellTheme ?? Empty, parsed.PageTheme ?? Empty, parsed.AppStart, null)
            {
                AppStartCount = parsed.AppStartCount,
                FocusSentinel = parsed.FocusSentinel,
                PageThemeAfterMalformedPush = parsed.PageThemeAfterMalformedPush,
            };
        }
        finally
        {
            try { File.Delete(report); } catch (IOException) { }
        }
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
            AppContext.BaseDirectory, "..", "..", "..", "..", "AiDe.App.ContrastProbe", "bin"));

        return Path.Combine(root, configuration, "net10.0-windows", "AiDe.App.ContrastProbe.exe");
    }

    // ──────────────────────────────────────────────────────────────── the report ──

    private static string RowOf(Site s) => string.Format(
        CultureInfo.InvariantCulture,
        "| {0} | {1:0.00} | {2:0.0} | `{3}` | {4} | `{5}` | {6} | {7:0.00} | {8:0.#}{9} | {10} | {11} | {12} | {13} | {14} |",
        s.Number, s.Ratio, s.Floor,
        s.RenderedInk, s.InkSource, s.Ground, s.GroundSource,
        s.Opacity, s.FontSize, s.Bold ? "b" : "",
        s.Population, Cell(s.Surface), Cell(s.Element) + (s.Text.Length > 0 ? " “" + Cell(s.Text) + "”" : ""),
        s.Mechanism, s.Clears ? "clears" : "**FAILS**");

    private static string Table(IEnumerable<Site> rows)
    {
        var report = new StringBuilder();
        report.AppendLine("| # | Ratio | Floor | Ink | Ink source | Ground | Ground source | Opacity | Size | Population | Surface | Element | Mechanism | Verdict |");
        report.AppendLine("|---|---:|---:|---|---|---|---|---:|---:|---|---|---|---|---|");
        foreach (var row in rows)
        {
            report.AppendLine(RowOf(row));
        }

        return report.ToString();
    }

    private static string MechanismCounts(IEnumerable<Site> rows) => string.Join(
        Environment.NewLine,
        rows.GroupBy(r => r.Mechanism)
            .OrderByDescending(g => g.Count())
            .Select(g => string.Format(CultureInfo.InvariantCulture, "- {0}: {1}", g.Key, g.Count())));

    private static string Cell(string text) => text.Replace("|", "\\|");

    private void Report(Census census)
    {
        var failing = census.Sites.Where(s => !s.Clears).OrderBy(s => s.Ratio).ToList();

        var report = new StringBuilder();
        report.AppendLine("# Shell contrast census");
        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture,
            $"AiDe.App {census.Version} · {census.Sites.Count} pairings measured · {failing.Count} below floor");
        report.AppendLine();
        report.AppendLine("## Failing, by mechanism");
        report.AppendLine();
        report.AppendLine(MechanismCounts(failing));
        report.AppendLine();
        report.AppendLine("## Failing sites");
        report.AppendLine();
        report.Append(Table(failing));
        report.AppendLine();
        report.AppendLine("## Every site");
        report.AppendLine();
        report.Append(Table(census.Sites));
        report.AppendLine();
        report.AppendLine("## Not measured");
        report.AppendLine();
        report.AppendLine("| Population | What | Reason |");
        report.AppendLine("|---|---|---|");
        foreach (var omission in census.Omissions)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"| {omission.Population} | {omission.What} | {omission.Reason} |");
        }

        report.AppendLine();
        report.AppendLine("## Log");
        report.AppendLine();
        foreach (var line in census.Log)
        {
            report.AppendLine("- " + line);
        }

        if (census.Failure is not null)
        {
            report.AppendLine();
            report.AppendLine("## Not taken");
            report.AppendLine();
            report.AppendLine(census.Failure);
        }

        var path = Path.Combine(Path.GetTempPath(), "aide-contrast-census.md");
        File.WriteAllText(path, report.ToString());

        output.WriteLine(report.ToString());
        output.WriteLine($"report: {path}");
    }
}
