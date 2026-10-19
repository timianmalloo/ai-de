using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// Pins the words of the restore report (DOCK3, Ruling 171 (1)): the exact sentences, the
/// DESIGN.md row that carries them, and the zone label the Owner asked for.
/// </summary>
public sealed class FloatingRedockCopyTests
{
    private const string Singular =
        "Terminal was floating in your saved layout. Floating is not available yet, so it is docked in the bottom zone.";

    private const string Plural =
        "2 floating panes in your saved layout are docked: Terminal in the bottom zone, Files in the left zone.";

    private static WorkbenchLayout Floated(params Surface[] surfaces) =>
        WorkbenchLayout.Default() with
        {
            Floating = [.. surfaces.Select((s, i) => new StackNode($"float-{i}", [s], 0, StackState.Floating))],
        };

    [Fact]
    public void TheSingularSentence_IsExactlyThePinnedWording()
    {
        var result = FloatingRedock.Dock(Floated(new Surface("t1", "terminal", "Terminal")));

        Assert.Equal(Singular, result.Report(result.Layout));
    }

    [Fact]
    public void ThePluralSentence_IsExactlyThePinnedWording()
    {
        var result = FloatingRedock.Dock(Floated(
            new Surface("t1", "terminal", "Terminal"),
            new Surface("f1", "explorer", "Files")));

        Assert.Equal(Plural, result.Report(result.Layout));
    }

    [Fact]
    public void TheDesignRowDock3_CarriesTheSameSentencesWordForWord()
    {
        var row = File.ReadAllLines(Path.Combine(RepoRoot(), "DESIGN.md"))
            .Single(l => l.StartsWith("| DOCK3 |", StringComparison.Ordinal));

        var singular = string.Format(FloatingRedockResult.SingularFormat, "{Title}", "{zone}");
        var plural = string.Format(
            FloatingRedockResult.PluralFormat,
            "{N}",
            string.Join(", ", Enumerable.Repeat(string.Format(FloatingRedockResult.PluralItemFormat, "{Title}", "{zone}"), 2)));

        Assert.Contains(singular, row, StringComparison.Ordinal);
        Assert.Contains(plural, row, StringComparison.Ordinal);
    }

    [Fact]
    public void TheZoneInTheReport_IsTheShellsUserFacingLabel_NeverTheInternalZoneId()
    {
        var kindForZone = new Dictionary<ZoneId, string>
        {
            [ZoneId.Left] = "explorer",
            [ZoneId.Bottom] = "terminal",
            [ZoneId.Center] = "canvas",
        };

        foreach (var zone in Enum.GetValues<ZoneId>())
        {
            var label = ZoneLayoutService.ZoneName(zone);
            Assert.NotEqual(zone.ToString(), label);

            if (!kindForZone.TryGetValue(zone, out var kind))
            {
                continue;
            }

            var result = FloatingRedock.Dock(Floated(new Surface("s1", kind, "Pane")));
            var report = result.Report(result.Layout);

            Assert.EndsWith($"docked in {label}.", report, StringComparison.Ordinal);
            Assert.DoesNotContain(zone.ToString(), report, StringComparison.Ordinal);
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
