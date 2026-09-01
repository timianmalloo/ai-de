using System.Xml.Linq;

namespace AiDe.App.Tests;

/// <summary>
/// Every enabled button in the window's chrome has a way to act.
/// </summary>
/// <remarks>
/// <para><b>What was wrong.</b> The mode rail declared four buttons; <c>ShellViewMode</c> declares
/// two. Three had no <c>Click</c>, no <c>Command</c> and no <c>x:Name</c> — so no code-behind could
/// reach them — and no <c>IsEnabled="False"</c>, so they did not read as disabled either. Each
/// carried a <c>ToolTip</c> promising a mode and an <c>AutomationProperties.Name</c> announcing one.
/// Pressing them did nothing and said nothing, not even the <i>"not available in this build"</i> the
/// command layer uses.</para>
///
/// <para><b>An inert control is a promise made in the UI rather than in a sentence</b>, and it is the
/// worst member of the family this repository spent a day on: the others render something
/// misleading, this renders something that looks operable and is not. A screen-reader user was told
/// a "Coordinate" button existed and given no way to discover it was a no-op.</para>
///
/// <para><b>Read from the XAML, not from a constructed window.</b> Building <c>MainWindow</c> starts
/// the shell and needs the application's resource dictionary, so the finding was originally made by
/// enumerating the markup and this test is made the same way. The cost is that it sees declarations
/// rather than behaviour; the benefit is that it sees ALL of them, including a button added
/// tomorrow.</para>
///
/// <para><b>Why it asserts about enabled buttons rather than about three names.</b> Naming the three
/// would pass the moment somebody adds a fourth — the defect-report-shaped test DC-076 is about. The
/// rule is the invariant: if it is enabled and announced, it must be able to act.</para>
/// </remarks>
public sealed class RailButtonsDoNotLieTests
{
    private static readonly XNamespace Presentation =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static string MainWindowXaml()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);

        while (here is not null && !File.Exists(Path.Combine(here.FullName, "AiDe.sln")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);

        var path = Path.Combine(here!.FullName, "src", "AiDe.App", "MainWindow.xaml");
        Assert.True(File.Exists(path), $"MainWindow.xaml was not found at {path}");

        return path;
    }

    [Fact]
    public void NoEnabledAnnouncedButtonIsInert()
    {
        var buttons = XDocument.Load(MainWindowXaml())
            .Descendants(Presentation + "Button")
            .ToList();

        // The DC-016 guard. A walk that found nothing would pass this file while the rail was full
        // of dead controls.
        Assert.True(buttons.Count >= 4,
            $"found {buttons.Count} Button element(s) in MainWindow.xaml; the mode rail alone has "
            + "four, so this test is reading the wrong document rather than a correct one");

        var inert = buttons
            .Where(b => (string?)b.Attribute("IsEnabled") != "False")
            .Where(b => b.Attribute(Presentation + "AutomationProperties.Name") is not null
                        || b.Attribute("AutomationProperties.Name") is not null)
            .Where(b => b.Attribute("Click") is null
                        && b.Attribute("Command") is null
                        && b.Attribute(Xaml + "Name") is null)
            .Select(b => (string?)b.Attribute("AutomationProperties.Name") ?? "(unnamed)")
            .ToList();

        Assert.True(inert.Count == 0,
            "these buttons are enabled, announced to assistive technology and styled like the "
            + "working ones, and have no Click, no Command and no x:Name by which any code could "
            + "reach them — pressing one does nothing and says nothing. A control that looks "
            + "operable and is not is a promise made in the UI: " + string.Join(", ", inert));
    }

    [Fact]
    public void ADisabledButtonSaysSoInItsTooltip()
    {
        // Disabling alone removes the lie but leaves the question. WPF drops a disabled button from
        // the tab order and reports it as disabled, so nobody is stranded — but a sighted user still
        // sees a greyed control with no explanation, and "Coordinate" is not one.
        var vague = XDocument.Load(MainWindowXaml())
            .Descendants(Presentation + "Button")
            .Where(b => (string?)b.Attribute("IsEnabled") == "False")
            .Where(b => ((string?)b.Attribute("ToolTip") ?? string.Empty)
                        .Contains("not in this build", StringComparison.OrdinalIgnoreCase) is false)
            .Select(b => (string?)b.Attribute("AutomationProperties.Name") ?? "(unnamed)")
            .ToList();

        Assert.True(vague.Count == 0,
            "these buttons are disabled but their tooltip does not say why, so they read as broken "
            + "rather than as not-yet-built: " + string.Join(", ", vague));
    }
}
