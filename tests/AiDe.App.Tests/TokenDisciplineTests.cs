using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace AiDe.App.Tests;

/// <summary>
/// Enforces UI Standard U3 ("reference a token, never an arbitrary value") OUTWARD against the built
/// XAML.
/// </summary>
/// <remarks>
/// This exists because the repo's deterministic craft gate (<c>ui-craft-gate.py</c>, wrapping
/// Impeccable) parses web sources and returns an empty result set for a WPF project — a clean report
/// over a corpus it never read, which is a success-shaped failure, not a pass. Rather than record the
/// gap as prose, it becomes this control: a lesson recorded as prose is a memoir.
///
/// Scope is honest about what it can and cannot see: it catches raw colour literals in component
/// markup. It cannot judge hierarchy, archetype fit, or whether the copy is true.
/// </remarks>
public sealed class TokenDisciplineTests
{
    private static readonly Regex RawHex = new(@"#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\b", RegexOptions.Compiled);

    /// <summary>App.xaml is the token dictionary — the one place a literal colour is the point.</summary>
    private const string TokenDictionary = "App.xaml";

    public static TheoryData<string> ComponentMarkup()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(ProjectRoot(), "*.xaml", SearchOption.AllDirectories))
        {
            if (!Path.GetFileName(file).Equals(TokenDictionary, StringComparison.OrdinalIgnoreCase))
            {
                data.Add(file);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ComponentMarkup))]
    public void ComponentMarkup_UsesTokensNotRawColourValues(string xamlPath)
    {
        var offenders = File.ReadAllLines(xamlPath)
            .Select((line, index) => (Line: line, Number: index + 1))
            .Where(l => RawHex.IsMatch(l.Line))
            .Select(l => $"{Path.GetFileName(xamlPath)}:{l.Number}: {l.Line.Trim()}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "Component markup must reference a semantic brush from App.xaml, never a raw colour:"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The corpus check the craft gate itself failed: a control that scans nothing is not a control.</summary>
    [Fact]
    public void TheScan_CoversANonEmptyCorpus()
    {
        Assert.NotEmpty(ComponentMarkup());
    }

    /// <summary>
    /// Every resource key this application names exists (TC3).
    /// </summary>
    /// <remarks>
    /// <para><b>A resource reference to a missing key is a silent no-op.</b> No exception, no log,
    /// and visually indistinguishable from "not themed yet". <c>SunkenBrush</c> and
    /// <c>RaisedBrush</c> were referenced from six sites and declared nowhere: the search box in the
    /// class diagram stayed platform white, the code viewer and the prompt draft stayed unthemed,
    /// and the type cards rendered transparent over the pane. The keys that WERE declared are
    /// <c>SurfaceSunkenBrush</c> and <c>SurfaceRaisedBrush</c>, one word away from each, and nothing
    /// anywhere said so.</para>
    ///
    /// <para><b>Fifteen lines of check would have caught both at authoring time</b>, which is the
    /// whole argument for writing them (CI6).</para>
    ///
    /// <para><b>Why DockRoundedTabs.xaml is excluded.</b> Its keys — <c>DropDownControlArea</c>,
    /// <c>PinClose</c> — are AvalonDock's, resolved from the theme dictionary the DockingManager
    /// merges at runtime, so App.xaml is the wrong dictionary to look for them in. The exclusion is
    /// by file and is asserted to match exactly one file, so it cannot quietly grow.</para>
    /// </remarks>
    [Fact]
    public void EveryResourceKeyTheAppNames_IsDeclared()
    {
        var declared = DeclaredKeys();

        Assert.True(declared.Count > 20,
            $"only {declared.Count} key(s) were read out of App.xaml, so this check is scanning the "
            + "wrong document and would pass over anything (DC-016).");

        var offenders = new List<string>();
        var scanned = 0;

        foreach (var file in Directory.EnumerateFiles(ProjectRoot(), "*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);

            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                continue;
            }

            Regex pattern;

            if (name.Equals("App.xaml", StringComparison.OrdinalIgnoreCase)
                || name.Equals(AvalonDockOwnedMarkup, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            else if (name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                pattern = MarkupKey;
            }
            else if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                pattern = CodeKey;
            }
            else
            {
                continue;
            }

            scanned++;

            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                var key = match.Groups[1].Value;

                if (!declared.Contains(key))
                {
                    offenders.Add($"{name}: {key}");
                }
            }
        }

        Assert.True(scanned > 10,
            $"only {scanned} file(s) were scanned for resource keys, which is not this project.");

        Assert.True(offenders.Count == 0,
            "these resource keys are named and never declared in App.xaml. A reference to a missing "
            + "key fails SILENTLY — the control simply keeps the platform's value, which is the same "
            + "rendering as 'not themed yet' (TC3): "
            + Environment.NewLine + string.Join(Environment.NewLine, offenders.Distinct()));
    }

    /// <summary>The one markup file whose keys belong to AvalonDock's theme, not to App.xaml.</summary>
    private const string AvalonDockOwnedMarkup = "DockRoundedTabs.xaml";

    private static readonly Regex MarkupKey =
        new(@"\{(?:Static|Dynamic)Resource\s+([A-Za-z0-9_]+)\s*\}", RegexOptions.Compiled);

    private static readonly Regex CodeKey = new(
        @"(?:SetResourceReference\s*\([^,]+,\s*|TryFindResource\s*\(|FindResource\s*\()\s*""([A-Za-z0-9_]+)""",
        RegexOptions.Compiled);

    private static HashSet<string> DeclaredKeys()
    {
        var app = File.ReadAllText(Path.Combine(ProjectRoot(), TokenDictionary));
        return [.. Regex.Matches(app, @"x:Key=""([^""]+)""").Select(m => m.Groups[1].Value)];
    }

    private static string ProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "AiDe.App");
    }

    // ───────────────────────────────────── the container pairs ink with ground; the leaf inherits ──

    /// <summary>
    /// The types whose implicit style may never state an ink (INV-0008). A style on one of these
    /// outranks every container's state pairing — selected, checked, disabled, on-accent — because
    /// the container's <c>TextElement.Foreground</c> reaches the glyphs only by inheritance, and an
    /// implicit style's setter beats an inherited value in the property system's precedence.
    /// </summary>
    private static readonly string[] LeafTextTypes =
    [
        "TextBlock", "Label", "AccessText", "Run", "Span", "Bold", "Italic", "Underline", "Hyperlink",
        "Paragraph", "TextElement", "Inline",
    ];

    public static TheoryData<string> AllMarkup()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(ProjectRoot(), "*.xaml", SearchOption.AllDirectories))
        {
            data.Add(file);
        }

        return data;
    }

    /// <summary>
    /// No implicit style for a leaf text type sets <c>Foreground</c> — the ink is the container's to
    /// pair with its ground, and the leaf inherits it (INV-0008, the class that shipped twelve accent
    /// tabs at 2.37:1 and two disabled buttons in their enabled ink).
    /// </summary>
    /// <remarks>
    /// <b>Why the census is not enough.</b> The census proves the composed shell at HEAD; this rule
    /// stops the shape being authored again, and fails at the line rather than at the pixel. It was
    /// observed red against the pre-fix <c>App.xaml</c> (TextBlock and Label each set Foreground) and
    /// green after the setters were removed.
    /// </remarks>
    [Theory]
    [MemberData(nameof(AllMarkup))]
    public void NoImplicitLeafTextStyle_SetsItsOwnInk(string xamlPath)
    {
        var offenders = LeafInkOverrides(File.ReadAllText(xamlPath))
            .Select(o => $"{Path.GetFileName(xamlPath)}: {o}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "an implicit style on a leaf text type states its own ink, so every container's state "
            + "pairing (selected / checked / disabled / on-accent) that reaches the text by "
            + "inheritance is overridden at once (INV-0008). Remove the Foreground setter; the Window "
            + "style states the root ink and the container pairs the rest:"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// Every trigger that paints a ground also states an ink that clears the floor on it — in the
    /// trigger itself, or by the template's rest ink measuring ≥ 4.5:1 on the new ground per the
    /// token dictionary's own values.
    /// </summary>
    /// <remarks>
    /// <para>A state is a <i>pairing</i> on the container: a trigger that swaps the ground to the
    /// accent and leaves the ink to whatever was there is the accent-tab defect written one trigger
    /// at a time. The rule resolves the ink in effect the way the property system does — the
    /// trigger's own setter, else the template's stated <c>TextElement.Foreground</c>, else the root
    /// ink — and measures the pairing from <c>App.xaml</c>'s colour values, so a ground that is fine
    /// under the rest ink (hover on the menu ground) passes without a redundant setter.</para>
    /// <para>Grounds that are not a plain token — <c>Transparent</c>, a theme
    /// <c>ComponentResourceKey</c>, a <c>TemplateBinding</c> — are outside what a source rule can
    /// measure; the census measures them rendered.</para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(AllMarkup))]
    public void EveryTriggerThatPaintsAGround_StatesAnInkThatClearsIt(string xamlPath)
    {
        var offenders = UnpairedGrounds(File.ReadAllText(xamlPath), DeclaredColours())
            .Select(o => $"{Path.GetFileName(xamlPath)}: {o}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "a state trigger paints a ground the ink in effect does not clear, and states no ink of "
            + "its own — the pairing belongs on the container, in the same trigger (INV-0008):"
            + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>The two rules, observed firing on the shapes they exist for — a rule never seen red proves nothing (DC-016).</summary>
    [Fact]
    public void TheSourceRules_FireOnTheShapesThatShippedTheDefect()
    {
        const string PreFixLeafStyle = """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="TextBrush" Color="#E4E9EF" />
              <Style TargetType="TextBlock">
                <Setter Property="Foreground" Value="{StaticResource TextBrush}" />
              </Style>
              <Style x:Key="Caption" TargetType="TextBlock">
                <Setter Property="Foreground" Value="{StaticResource TextBrush}" />
              </Style>
            </ResourceDictionary>
            """;

        var leaf = LeafInkOverrides(PreFixLeafStyle).ToList();

        // The implicit style is the defect; the keyed one is opted into per site and is not.
        Assert.Single(leaf);
        Assert.Contains("TextBlock", leaf[0], StringComparison.Ordinal);

        const string UnpairedAccent = """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <SolidColorBrush x:Key="TextBrush" Color="#E4E9EF" />
              <SolidColorBrush x:Key="AccentBrush" Color="#5B9DD9" />
              <SolidColorBrush x:Key="MenuHoverBrush" Color="#243040" />
              <SolidColorBrush x:Key="AccentContrastBrush" Color="#0D1014" />
              <ControlTemplate x:Key="T" TargetType="ToggleButton">
                <Border x:Name="Chrome" Background="{TemplateBinding Background}">
                  <ContentPresenter x:Name="Content" TextElement.Foreground="{StaticResource TextBrush}" />
                </Border>
                <ControlTemplate.Triggers>
                  <Trigger Property="IsMouseOver" Value="True">
                    <Setter TargetName="Chrome" Property="Background" Value="{StaticResource MenuHoverBrush}" />
                  </Trigger>
                  <Trigger Property="IsChecked" Value="True">
                    <Setter TargetName="Chrome" Property="Background" Value="{StaticResource AccentBrush}" />
                  </Trigger>
                  <Trigger Property="IsPressed" Value="True">
                    <Setter TargetName="Chrome" Property="Background" Value="{StaticResource AccentBrush}" />
                    <Setter TargetName="Content" Property="TextElement.Foreground" Value="{StaticResource AccentContrastBrush}" />
                  </Trigger>
                </ControlTemplate.Triggers>
              </ControlTemplate>
            </ResourceDictionary>
            """;

        var unpaired = UnpairedGrounds(UnpairedAccent, DeclaredColours(UnpairedAccent)).ToList();

        // Hover keeps the rest ink on a ground it clears (TextBrush on MenuHover ≈ 11:1): not a
        // finding. Checked paints the accent under the rest ink (2.37:1) and states nothing: the
        // finding. Pressed paints the accent and pairs it: not a finding.
        var finding = Assert.Single(unpaired);
        Assert.Contains("IsChecked", finding, StringComparison.Ordinal);
        Assert.Contains("AccentBrush", finding, StringComparison.Ordinal);
        Assert.Contains("TextBrush", finding, StringComparison.Ordinal);
    }

    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly Regex ResourceToken =
        new(@"^\{(?:Static|Dynamic)Resource\s+([A-Za-z0-9_]+)\s*\}$", RegexOptions.Compiled);

    private static readonly string[] InkProperties = ["Foreground", "TextElement.Foreground", "Control.Foreground"];
    private static readonly string[] GroundProperties = ["Background", "Panel.Background", "Border.Background", "Control.Background"];

    /// <summary>The floor for body text (WCAG 1.4.3); every ground a trigger paints under text is body text.</summary>
    private const double TextFloor = 4.5;

    /// <summary>The ink the Window style states, which every text inherits when nothing nearer says otherwise.</summary>
    private const string RootInk = "TextBrush";

    /// <summary>Implicit (keyless) styles on a leaf text type that set an ink, anywhere in the style.</summary>
    internal static IEnumerable<string> LeafInkOverrides(string xaml)
    {
        var document = XDocument.Parse(xaml, LoadOptions.SetLineInfo);

        foreach (var style in document.Descendants().Where(e => e.Name.LocalName == "Style"))
        {
            var target = TargetTypeName(style.Attribute("TargetType")?.Value);
            if (target is null || !LeafTextTypes.Contains(target, StringComparer.Ordinal)) continue;
            if (style.Attribute(Xaml + "Key") is not null) continue;

            foreach (var setter in style.Descendants().Where(e => e.Name.LocalName == "Setter"))
            {
                var property = setter.Attribute("Property")?.Value;
                if (property is not null && InkProperties.Contains(property, StringComparer.Ordinal))
                {
                    yield return $"line {Line(setter)}: implicit <Style TargetType=\"{target}\"> sets {property}";
                }
            }
        }
    }

    /// <summary>
    /// Triggers that paint a token ground under text and neither state an ink nor leave one in effect
    /// that clears the floor on that ground.
    /// </summary>
    internal static IEnumerable<string> UnpairedGrounds(string xaml, IReadOnlyDictionary<string, Color> colours)
    {
        var document = XDocument.Parse(xaml, LoadOptions.SetLineInfo);

        foreach (var trigger in document.Descendants().Where(IsTrigger))
        {
            var setters = trigger.Elements().Where(e => e.Name.LocalName == "Setter").ToList();

            var grounds = setters
                .Where(s => GroundProperties.Contains(s.Attribute("Property")?.Value ?? "", StringComparer.Ordinal))
                .Select(s => Token(s.Attribute("Value")?.Value))
                .Where(t => t is not null && colours.ContainsKey(t))
                .Select(t => t!)
                .ToList();

            if (grounds.Count == 0) continue;

            var ownInk = setters
                .Where(s => InkProperties.Contains(s.Attribute("Property")?.Value ?? "", StringComparer.Ordinal))
                .Select(s => Token(s.Attribute("Value")?.Value))
                .FirstOrDefault(t => t is not null);

            var ink = ownInk ?? RestInk(trigger);

            foreach (var ground in grounds)
            {
                if (!colours.TryGetValue(ink, out var inkColour))
                {
                    yield return $"line {Line(trigger)}: {Describe(trigger)} paints {ground} and the ink in effect ({ink}) is not a colour token";
                    continue;
                }

                var ratio = ThemeProbe.Contrast(inkColour, colours[ground]);
                if (ratio < TextFloor)
                {
                    yield return string.Format(CultureInfo.InvariantCulture,
                        "line {0}: {1} paints {2} under {3} at {4:0.00}:1 (floor {5}) and states no ink",
                        Line(trigger), Describe(trigger), ground, ink, ratio, TextFloor);
                }
            }
        }
    }

    /// <summary>
    /// The ink in effect where the trigger's ground is painted: the enclosing template's stated
    /// <c>TextElement.Foreground</c> token, else the root ink the Window style sets.
    /// </summary>
    private static string RestInk(XElement trigger)
    {
        var template = trigger.Ancestors().FirstOrDefault(e => e.Name.LocalName is "ControlTemplate" or "DataTemplate" or "Style");
        if (template is null) return RootInk;

        var stated = template.Descendants()
            .Where(e => !e.Ancestors().Any(IsTrigger) && e.Name.LocalName != "Setter")
            .SelectMany(e => e.Attributes())
            .Where(a => InkProperties.Contains(a.Name.LocalName, StringComparer.Ordinal))
            .Select(a => Token(a.Value))
            .FirstOrDefault(t => t is not null);

        return stated ?? RootInk;
    }

    private static bool IsTrigger(XElement e) =>
        e.Name.LocalName is "Trigger" or "DataTrigger" or "MultiTrigger" or "MultiDataTrigger";

    private static string Describe(XElement trigger)
    {
        var property = trigger.Attribute("Property")?.Value ?? trigger.Attribute("Binding")?.Value;
        if (property is null)
        {
            var conditions = trigger.Descendants().Where(e => e.Name.LocalName == "Condition")
                .Select(c => (c.Attribute("Property")?.Value ?? c.Attribute("Binding")?.Value ?? "?") + "=" + (c.Attribute("Value")?.Value ?? "?"));
            property = string.Join(" && ", conditions);
        }

        return $"{trigger.Name.LocalName} [{property}={trigger.Attribute("Value")?.Value ?? ""}]";
    }

    private static string? Token(string? value)
    {
        if (value is null) return null;
        var match = ResourceToken.Match(value.Trim());
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? TargetTypeName(string? targetType)
    {
        if (targetType is null) return null;
        var name = targetType.Trim();
        var typeExtension = Regex.Match(name, @"^\{x:Type\s+(?:[A-Za-z0-9_]+:)?([A-Za-z0-9_]+)\s*\}$");
        if (typeExtension.Success) return typeExtension.Groups[1].Value;
        var colon = name.LastIndexOf(':');
        return colon >= 0 ? name[(colon + 1)..] : name;
    }

    private static int Line(XElement element) => ((System.Xml.IXmlLineInfo)element).LineNumber;

    /// <summary>Every <c>SolidColorBrush</c> the dictionary declares with a literal colour, by key.</summary>
    private static Dictionary<string, Color> DeclaredColours(string? xaml = null)
    {
        xaml ??= File.ReadAllText(Path.Combine(ProjectRoot(), TokenDictionary));
        var document = XDocument.Parse(xaml);
        var colours = new Dictionary<string, Color>(StringComparer.Ordinal);

        foreach (var brush in document.Descendants(Presentation + "SolidColorBrush"))
        {
            var key = brush.Attribute(Xaml + "Key")?.Value;
            var colour = brush.Attribute("Color")?.Value;
            if (key is null || colour is null || !colour.StartsWith('#')) continue;
            colours[key] = (Color)ColorConverter.ConvertFromString(colour)!;
        }

        return colours;
    }
}
