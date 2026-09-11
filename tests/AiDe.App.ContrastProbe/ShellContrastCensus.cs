using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.Core.Workbench;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.ContrastProbe;

/// <summary>
/// Walks the shell <b>the product composes</b> and measures every text pairing it renders.
/// </summary>
/// <remarks>
/// <para><b>Why a census and not more sites.</b> <c>ContrastFloorTests</c> measures eleven hand-picked
/// pairings, each constructed by the test on a themed window. Its own remark names the class —
/// "coverage is per-element, so the next control regresses silently" — and then measures per
/// element anyway. The population it measures is <i>controls the test constructs</i>; the population
/// the operator reads is <i>the visual tree the product composes</i>: the real <c>App</c> carrying
/// its compiled App.xaml, the real <c>MainWindow</c>, a <c>WorkbenchShell</c> over a
/// <c>ZoneBackedLayoutService</c> (DC-135), a <c>DockingManager</c> wearing the VS2013 dark theme,
/// and every surface kind the factory can build. Text whose ink is inherited from a template, chosen
/// by the library's theme, dimmed by an ancestor's <c>Opacity</c>, or composed from a token on a
/// ground it was never measured against is outside the first population and inside the second.</para>
///
/// <para><b>The ground is read from pixels, not from a property.</b> A <c>ContentControl</c>'s
/// <c>Background</c> is never painted — its default template is a bare <c>ContentPresenter</c> — so
/// a property walk answers "what does the tree <i>say</i> is behind this text", not "what is behind
/// it". This renders the composed frame once with every measured glyph hidden and samples the
/// pixels under each element's bounds, which is the ground the operator sees, whatever painted
/// it.</para>
///
/// <para><b>The ink is read from the property system with its provenance.</b>
/// <see cref="DependencyPropertyHelper.GetValueSource"/> says whether the brush came from a local
/// value, a style, a template, inheritance or the platform default, and reference-matching it against
/// the theme dictionary says which token it is — or that it is none of them. That provenance is the
/// mechanism column: it is how each failing site escaped the floor.</para>
/// </remarks>
internal static class ShellContrastCensus
{
    /// <summary>WCAG 1.4.3 — body text.</summary>
    internal const double TextFloor = 4.5;

    /// <summary>WCAG 1.4.3 — large text (≥ 18.66px bold, or ≥ 24px), and 1.4.11 UI components.</summary>
    internal const double LargeTextFloor = 3.0;

    /// <summary>
    /// DESIGN.md "Disabled is a state, not an opacity": an unavailable control is still read, so its
    /// text is held to the meaningful-graphic floor rather than exempted as WCAG 1.4.3 allows.
    /// </summary>
    internal const double DisabledFloor = 3.0;

    private const double LargeTextSize = 18.66;

    /// <summary>One measured text pairing, as written to the report.</summary>
    internal sealed record Site(
        int Number,
        string Population,
        string Surface,
        string Element,
        string Text,
        string Ink,
        string InkSource,
        string Ground,
        string GroundSource,
        double Opacity,
        double FontSize,
        bool Bold,
        bool Enabled,
        string Mechanism,
        string RenderedInk,
        double Ratio,
        double Floor,
        bool Clears);

    /// <summary>What the census could not measure, and why — reported beside the rows, never silently.</summary>
    internal sealed record Omission(string Population, string What, string Reason);

    internal sealed record Report(
        string Commit,
        List<Site> Sites,
        List<Omission> Omissions,
        List<string> Log,
        Dictionary<string, string> ShellTheme,
        Dictionary<string, string> PageTheme)
    {
        /// <summary>The first <c>app.start</c> diagnostics line this boot wrote, or null when it wrote none.</summary>
        public string? AppStart { get; init; }

        /// <summary>How many <c>app.start</c> lines the boot wrote — one is the contract.</summary>
        public int AppStartCount { get; init; }

        /// <summary>The colour the probe planted on <c>FocusBrush</c> before the shell composed, so the push can be told from the fallback.</summary>
        public string? FocusSentinel { get; init; }

        /// <summary>What the page's root carried after <c>applyTheme</c> was handed malformed entries — must equal <see cref="PageTheme"/>.</summary>
        public Dictionary<string, string>? PageThemeAfterMalformedPush { get; init; }
    }

    private static Site Row(
        int number, string population, string surface, string element, string text,
        Color ink, string inkSource, Color ground, string groundSource,
        double opacity, double fontSize, bool bold, bool enabled, string mechanism)
    {
        var rendered = Composite(ink, ground, opacity);
        var ratio = Contrast(rendered, ground);
        var floor = !enabled ? DisabledFloor
            : fontSize >= 24 || (bold && fontSize >= LargeTextSize) ? LargeTextFloor
            : TextFloor;

        return new Site(
            number, population, surface, element, text,
            Hex(ink), inkSource, Hex(ground), groundSource,
            opacity, fontSize, bold, enabled, mechanism,
            Hex(rendered), Math.Round(ratio, 2), floor, ratio >= floor);
    }

    // ───────────────────────────────────────────────────────────────────── the passes ──

    /// <summary>Takes the census of the shown <paramref name="window"/> — the product's own.</summary>
    internal static async Task<Report> TakeAsync(MainWindow window, string workspace, string commit)
    {
        var sites = new List<Site>();
        var omissions = new List<Omission>();
        var log = new List<string>();
        var shell = window.Shell;
        var frame = (FrameworkElement)window.FindName("RootLayer");
        var theme = Application.Current.Resources;
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

        void Pass(string label, FrameworkElement? root = null)
        {
            window.UpdateLayout();
            var before = sites.Count;
            Walk(window, root ?? frame, theme, seen, sites, log);
            log.Add($"{label}: +{sites.Count - before} sites ({sites.Count} total)");
        }

        // 1. The default layout as the shell opens it.
        Pass("default layout");

        // 2. Every surface kind the factory can build, in the centre stack, each activated so its
        //    content is in the tree — an unselected tab renders nothing.
        var present = shell.Service.Current.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.Kind)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var kind in SurfaceContentFactory.Kinds)
        {
            if (present.Contains(kind.Kind)) continue;

            var result = shell.Service.Apply(new LayoutOperation.AddSurface(
                ZonesToTree.CenterStackId, new Surface($"census:{kind.Kind}", kind.Kind, $"Census {kind.Kind}")));
            log.Add($"add {kind.Kind}: {(result.Applied ? "applied" : result.Announcement)}");
        }

        shell.Adapter.Render();

        // 3. The session document, with every registered canvas mode shown in turn.
        var session = new AiDe.Core.Sessions.SessionConfig(
            "20260911T000000Z-census", "Census session", workspace, DateTimeOffset.UtcNow, ["claude-code"]);
        log.Add("open session: " + shell.OpenSessionDocument(session));

        foreach (var surface in shell.Service.Current.AllStacks().SelectMany(s => s.Surfaces).ToList())
        {
            shell.Adapter.ActivateInView(surface.SurfaceId);
            Pass($"activate {surface.SurfaceId} ({surface.Kind})");
        }

        var documentId = AiDe.App.Workbench.Sessions.SessionDocumentSurface.SurfaceIdFor(session.SessionId);
        shell.Adapter.ActivateInView(documentId);

        var document = shell.Adapter.SurfaceContent<AiDe.App.Workbench.Sessions.SessionDocumentSurface>(documentId)
            ?? throw new InvalidOperationException(
                "the session document did not render, so the composer, the console and the mode strip measured nothing (DC-016)");

        foreach (var mode in AiDe.App.Workbench.Sessions.CanvasModeCatalog.All)
        {
            document.Model.SetActiveMode(mode.ModeId);
            Pass($"session mode {mode.ModeId}");
        }

        omissions.Add(new Omission("wpf", "session document split state", "population change only (MS1); the unsplit modes are each measured"));

        // The composer as the product binds it (MainWindow.BindComposer -> Configure), so the page
        // mounts its fields and the WPF footer renders its compiled view; then a refusal on the
        // status line, which is the other state the product puts there and the paragraph the
        // operator photographed.
        var composerRoot = Path.Combine(workspace, "composer");
        Directory.CreateDirectory(composerRoot);
        document.Composer.Configure(
            session,
            new AiDe.App.Workbench.Composer.ComposerSendContext(
                RepositoryRoot: composerRoot, DataDirectory: composerRoot, AdapterInstallRoot: composerRoot,
                EngineId: "claude-code", Model: "census-model", AccountLabel: "census-account",
                TaskClass: "census", ProofPackArtifacts: [], Providers: []),
            AiDe.App.Workbench.Composer.ComposerFields.GoalBlock(),
            new AiDe.Core.Presentation.Composer.AttachmentGate(
                composerRoot, new AiDe.Core.Presentation.Composer.AttachmentFileReader(), new NeverAffirms(), "anthropic", "census-account"));
        document.Composer.ShowFieldRefusal("engineId", "the send was refused: this session enables 0 routable backends");
        Pass("composer configured + refusal");

        // 4. The command palette overlay.
        shell.Palette.Open();
        Pass("command palette");
        shell.Palette.Close();

        // 5. Every top-level menu, opened. A Popup's content is its own visual root, so it is walked
        //    and rendered as one.
        var menu = (Menu)window.FindName("MainMenu");
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            item.IsSubmenuOpen = true;
            window.UpdateLayout();

            if ((item.Template?.FindName("Popup", item) ?? item.Template?.FindName("PART_Popup", item))
                is System.Windows.Controls.Primitives.Popup { Child: FrameworkElement popupRoot })
            {
                popupRoot.UpdateLayout();
                Pass($"menu {item.Header}", popupRoot);
            }
            else
            {
                omissions.Add(new Omission("wpf", $"menu {item.Header} drop-down", "the template exposes no Popup, so the drop-down could not be walked"));
            }

            item.IsSubmenuOpen = false;
        }

        omissions.Add(new Omission("wpf", "combo drop-downs, tooltips, context menus", "Popup visuals are not in the window's visual tree until opened; the composer's template picker is collapsed with no catalog"));
        omissions.Add(new Omission("wpf", "the New Session sheet", "a separate window; ContrastFloorTests site 7 measures it"));
        omissions.Add(new Omission("wpf", "hover / pressed states", "the census reads the rest state; a pointer-only pairing is unmeasured here (selected-inactive IS measured: the selected tab of every pane that lost focus is in the rest state)"));
        omissions.Add(new Omission("terminal", "TerminalView cells", "GlyphRun renderer over the ANSI palette a child process chooses from; App.xaml's palette table is the pairing set"));

        // 6. The two WebView2 pages: the composer and the graph canvas.
        var pageTheme = await WebViewsAsync(window, document, sites, omissions, log);

        // The theme the shell pushed on host.init, beside the custom properties the page's root
        // actually carries. The stylesheet never DECLARES a property — it only reads each one with
        // its fallback — so a property on the root's inline style can only have come from the push.
        // Equal values from the fallback and the push measure the same; this column tells them apart.
        var shellTheme = new Dictionary<string, string>(ComposerPageTheme.Current(), StringComparer.Ordinal);

        return new Report(commit, sites, omissions, log, shellTheme, pageTheme.Applied)
        {
            PageThemeAfterMalformedPush = pageTheme.AfterMalformed,
        };
    }

    // ─────────────────────────────────────────────────────────────────── the walker ──

    private static void Walk(
        Window window, FrameworkElement frame, ResourceDictionary theme, HashSet<object> seen,
        List<Site> sites, List<string> log)
    {
        var carriers = new List<(DependencyObject Element, UIElement Glyphs, Run? Run, string? Text)>();
        var skipped = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var visual in Visuals(frame))
        {
            if (visual is TextBlock probe && !string.IsNullOrWhiteSpace(probe.Text)
                && !(probe.IsVisible && probe.ActualWidth > 0 && probe.ActualHeight > 0))
            {
                var why = !probe.IsVisible ? "not visible" : "zero size";
                skipped[why] = skipped.GetValueOrDefault(why) + 1;
            }

            switch (visual)
            {
                // A ContentPresenter with RecognizesAccessKey renders a string as an AccessText,
                // whose glyphs are an internal TextBlock it owns. The inner block is what is drawn
                // (and what an implicit TextBlock style reaches), so it is the element measured; the
                // AccessText is the visual hidden and the source of the text.
                case AccessText access when access.IsVisible && access.ActualWidth > 0 && !string.IsNullOrWhiteSpace(access.Text):
                    if (VisualTreeHelper.GetChildrenCount(access) > 0
                        && VisualTreeHelper.GetChild(access, 0) is TextBlock inner
                        && seen.Add(inner))
                    {
                        seen.Add(access);
                        carriers.Add((inner, access, null, access.Text.Replace("_", "")));
                    }

                    break;

                case TextBlock block when block.IsVisible && block.ActualWidth > 0 && block.ActualHeight > 0
                                          && !string.IsNullOrWhiteSpace(block.Text):
                    if (seen.Add(block)) carriers.Add((block, block, null, null));

                    foreach (var run in Runs(block.Inlines))
                    {
                        if (string.IsNullOrWhiteSpace(run.Text)) continue;
                        var source = DependencyPropertyHelper.GetValueSource(run, TextElement.ForegroundProperty).BaseValueSource;
                        if (source is BaseValueSource.Inherited or BaseValueSource.Default) continue;
                        if (seen.Add(run)) carriers.Add((run, block, run, null));
                    }

                    break;

                case System.Windows.Controls.Primitives.TextBoxBase box when box.IsVisible && box.ActualWidth > 0:
                    if (seen.Add(box)) carriers.Add((box, GlyphHost(box), null, null));
                    break;

                case PasswordBox password when password.IsVisible && password.ActualWidth > 0:
                    if (seen.Add(password)) carriers.Add((password, GlyphHost(password), null, null));
                    break;

                case ICSharpCode.AvalonEdit.TextEditor editor when editor.IsVisible && editor.ActualWidth > 0:
                    if (seen.Add(editor)) carriers.Add((editor, editor.TextArea, null, null));
                    break;

                case TerminalView terminal when terminal.IsVisible:
                    seen.Add(terminal);
                    break;
            }
        }

        foreach (var (why, count) in skipped)
        {
            log.Add($"  skipped {count} text block(s): {why}");
        }

        if (carriers.Count == 0) return;

        // Read each carrier's rendered opacity BEFORE the glyphs are hidden below — reading it after
        // would report the 0 this walk set, which is how the first run reported 73 sites at 0.00.
        var opacities = carriers.ToDictionary(
            c => (object)c.Glyphs, c => EffectiveOpacity(c.Glyphs), ReferenceEqualityComparer.Instance);

        // Hide every glyph carrier, render the frame once, sample the ground under each, restore.
        var restore = new List<(UIElement Element, object Local)>();
        foreach (var (_, glyphs, _, _) in carriers.DistinctBy(c => c.Glyphs, ReferenceEqualityComparer.Instance))
        {
            restore.Add((glyphs, glyphs.ReadLocalValue(UIElement.OpacityProperty)));
            glyphs.Opacity = 0;
        }

        try
        {
            var pixels = Render(frame, out var width, out var height);
            var windowGround = (window.Background as SolidColorBrush)?.Color ?? Colors.Magenta;

            foreach (var (element, glyphs, run, text) in carriers)
            {
                var bounds = BoundsIn(frame, glyphs);
                if (bounds.IsEmpty || bounds.Width < 1 || bounds.Height < 1) continue;

                var ground = Sample(pixels, width, height, bounds, windowGround, out var groundNote);
                sites.Add(Measure(sites.Count + 1, element, glyphs, run, text, ground, groundNote, opacities[glyphs], theme));
            }
        }
        finally
        {
            foreach (var (element, local) in restore)
            {
                if (local == DependencyProperty.UnsetValue) element.ClearValue(UIElement.OpacityProperty);
                else element.SetValue(UIElement.OpacityProperty, local);
            }
        }
    }

    private static Site Measure(
        int number, DependencyObject element, UIElement glyphs, Run? run, string? textOverride, Color ground, string groundNote,
        double opacity, ResourceDictionary theme)
    {
        var inkBrush = run is not null ? run.Foreground
            : element is TextBlock t ? t.Foreground
            : element is Control c ? c.Foreground
            : null;

        var (ink, inkNote) = inkBrush switch
        {
            SolidColorBrush solid => (solid.Color, string.Empty),
            null => (Colors.Magenta, "no brush"),
            GradientBrush gradient => (Average(gradient), "gradient (averaged)"),
            _ => (Colors.Magenta, inkBrush.GetType().Name),
        };

        var valueSource = DependencyPropertyHelper.GetValueSource(element, TextElement.ForegroundProperty);
        var token = TokenFor(theme, inkBrush);
        var inkSource = DescribeInk(element, valueSource, token, inkNote);
        var groundSource = TokenFor(theme, ground) is { } key ? key : "no token";
        if (groundNote.Length > 0) groundSource += " " + groundNote;

        var fontSize = run?.FontSize ?? (element is TextBlock tb ? tb.FontSize : element is Control ctl ? ctl.FontSize : 13);
        var weight = run?.FontWeight ?? (element is TextBlock tb2 ? tb2.FontWeight : element is Control ctl2 ? ctl2.FontWeight : FontWeights.Normal);
        var enabled = glyphs.IsEnabled;

        var text = textOverride ?? run?.Text ?? element switch
        {
            TextBlock t2 => t2.Text,
            TextBox box => box.Text.Length > 0 ? box.Text : "(empty text box)",
            System.Windows.Controls.Primitives.TextBoxBase => "(rich text box)",
            PasswordBox => "(password box)",
            ICSharpCode.AvalonEdit.TextEditor => "(code editor)",
            _ => string.Empty,
        };

        var mechanism = Mechanism(valueSource, token, opacity, enabled, inkNote);

        return Row(
            number, "wpf", OwningSurface(glyphs), Describe(element, glyphs), Excerpt(text),
            ink, inkSource, ground, groundSource, opacity, fontSize,
            weight.ToOpenTypeWeight() >= 700, enabled, mechanism);
    }

    private static string Mechanism(ValueSource source, string? token, double opacity, bool enabled, string inkNote)
    {
        if (inkNote.Length > 0) return "non-solid ink";
        if (!enabled) return "disabled";
        if (opacity < 0.999) return "opacity-dimmed";

        return source.BaseValueSource switch
        {
            BaseValueSource.Inherited => "inherited",
            BaseValueSource.Default => "platform-default",
            BaseValueSource.Style or BaseValueSource.ImplicitStyleReference or BaseValueSource.StyleTrigger
                => token is null ? "library-style" : "app-style token",
            BaseValueSource.TemplateTrigger or BaseValueSource.ParentTemplate or BaseValueSource.ParentTemplateTrigger
                => token is null ? "library-template" : "template token",
            BaseValueSource.Local => token is null ? "hardcoded" : "token",
            _ => source.BaseValueSource.ToString(),
        };
    }

    private static string DescribeInk(DependencyObject element, ValueSource source, string? token, string note)
    {
        var text = new StringBuilder();
        text.Append(token ?? "no token");
        text.Append(" · ").Append(source.BaseValueSource);
        if (source.IsExpression) text.Append("/expr");

        if (source.BaseValueSource == BaseValueSource.Inherited)
        {
            var origin = InheritanceOrigin(element);
            if (origin is not null) text.Append(" from ").Append(origin);
        }

        if (note.Length > 0) text.Append(" · ").Append(note);
        return text.ToString();
    }

    /// <summary>The first ancestor whose Foreground is not itself inherited — the element that chose the ink.</summary>
    private static string? InheritanceOrigin(DependencyObject element)
    {
        var current = Parent(element);
        while (current is not null)
        {
            var source = DependencyPropertyHelper.GetValueSource(current, TextElement.ForegroundProperty);
            if (source.BaseValueSource != BaseValueSource.Inherited)
            {
                return $"{current.GetType().Name}{NameOf(current)} ({source.BaseValueSource})";
            }

            current = Parent(current);
        }

        return null;
    }

    private static string? TokenFor(ResourceDictionary theme, Brush? brush)
    {
        if (brush is not SolidColorBrush solid) return null;

        foreach (var key in theme.Keys)
        {
            if (theme[key] is SolidColorBrush candidate && ReferenceEquals(candidate, solid)) return key.ToString();
        }

        return TokenFor(theme, solid.Color) is { } byValue ? "~" + byValue : null;
    }

    /// <summary>
    /// Every token whose colour equals this one, joined with "=" — "~" in the report: same colour, not
    /// the same brush. Listed rather than picked, because two tokens can legitimately share a value
    /// (<c>SurfaceSunkenBrush</c> and <c>AccentContrastBrush</c> are both #0D1014) and a report that
    /// named one of them by dictionary order would attribute a text box's ground to the on-accent ink.
    /// </summary>
    private static string? TokenFor(ResourceDictionary theme, Color color)
    {
        var matches = new List<string>();

        foreach (var key in theme.Keys)
        {
            if (theme[key] is SolidColorBrush candidate && candidate.Color == color) matches.Add(key.ToString()!);
        }

        matches.Sort(StringComparer.Ordinal);
        return matches.Count == 0 ? null : string.Join("=", matches);
    }

    // ────────────────────────────────────────────────────────────────── the pages ──

    private static async Task<(Dictionary<string, string> Applied, Dictionary<string, string>? AfterMalformed)> WebViewsAsync(
        Window window, AiDe.App.Workbench.Sessions.SessionDocumentSurface document,
        List<Site> sites, List<Omission> omissions, List<string> log)
    {
        var pageTheme = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string>? afterMalformed = null;
        var composer = document.Composer;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

        while (!composer.PageIsReady && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        log.Add($"composer page ready: {composer.PageIsReady}");

        var views = Visuals(window).OfType<WebView2>().ToList();
        log.Add($"webview2 controls in tree: {views.Count}");

        foreach (var view in views)
        {
            var label = view.CoreWebView2?.Source ?? "(no CoreWebView2)";
            var owner = OwningSurface(view);

            if (view.CoreWebView2 is null)
            {
                omissions.Add(new Omission("webview", owner, "CoreWebView2 never initialised within 30s — not recorded"));
                continue;
            }

            string? json = null;
            var mounted = DateTime.UtcNow + TimeSpan.FromSeconds(10);

            // The fields mount on host.init, after Configure; poll — bounded — until the page says
            // it has APPLIED an init (not merely mounted: PageIsReady is the mount), then take that
            // reading. Waiting on text rows alone would read a page with static text before the
            // push had been applied, and measure its fallbacks as if they were the push.
            while (DateTime.UtcNow < mounted)
            {
                string raw;
                try
                {
                    var inits = await view.CoreWebView2.ExecuteScriptAsync("window.__composerInitCount");
                    if (inits is not ("0" or "null" or "undefined")) { raw = await view.CoreWebView2.ExecuteScriptAsync(PageCensusScript); }
                    else { await Task.Delay(200); continue; }
                }
                catch (Exception ex)
                {
                    omissions.Add(new Omission("webview", $"{owner} {label}", "ExecuteScriptAsync threw: " + ex.Message));
                    break;
                }

                // ExecuteScriptAsync returns the JSON encoding of the script's value; the value is
                // itself a JSON string, so it is decoded twice.
                json = JsonSerializer.Deserialize<string>(raw);
                if (json is not null && json != "[]") break;
                await Task.Delay(200);
            }

            if (string.IsNullOrEmpty(json))
            {
                omissions.Add(new Omission("webview", $"{owner} {label}", "the page returned nothing"));
                continue;
            }

            using var rows = JsonDocument.Parse(json);
            var count = 0;

            foreach (var row in rows.RootElement.EnumerateArray())
            {
                count++;
                var ink = ParseRgb(row.GetProperty("ink").GetString()!);
                var ground = ParseRgb(row.GetProperty("ground").GetString()!);

                sites.Add(Row(
                    sites.Count + 1, "webview", $"{owner} · {Uri.UnescapeDataString(label)}",
                    row.GetProperty("sel").GetString()!, Excerpt(row.GetProperty("text").GetString()!),
                    ink, "page CSS " + row.GetProperty("inkCss").GetString(),
                    ground, "page CSS",
                    row.GetProperty("opacity").GetDouble(), row.GetProperty("size").GetDouble(),
                    row.GetProperty("bold").GetBoolean(), !row.GetProperty("disabled").GetBoolean(),
                    "page-css"));
            }

            log.Add($"webview {label}: {count} text elements");

            if (Uri.UnescapeDataString(label).EndsWith("/" + ComposerPageContract.PageFile, StringComparison.Ordinal))
            {
                try
                {
                    foreach (var pair in await RootCustomPropertiesAsync(view))
                    {
                        pageTheme[pair.Key] = pair.Value;
                    }

                    // applyTheme's guards, against the live page: a malformed name, a non-hex value
                    // and a non-string value must leave the root exactly as the push left it. Read
                    // AFTER the push's footprint has been recorded, so the two readings are compared.
                    await view.CoreWebView2.ExecuteScriptAsync(MalformedThemeScript);
                    afterMalformed = await RootCustomPropertiesAsync(view);
                }
                catch (Exception ex)
                {
                    omissions.Add(new Omission("webview", $"{owner} {label} root custom properties", "ExecuteScriptAsync threw: " + ex.Message));
                }

                log.Add($"webview {label}: {pageTheme.Count} custom properties on the root");
            }
        }

        return (pageTheme, afterMalformed);
    }

    private static async Task<Dictionary<string, string>> RootCustomPropertiesAsync(WebView2 view)
    {
        var raw = await view.CoreWebView2.ExecuteScriptAsync(RootCustomPropertiesScript);
        var inline = JsonSerializer.Deserialize<string>(raw);
        return string.IsNullOrEmpty(inline)
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(inline) ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Entries applyTheme must refuse, each one the GUARD decides (a name the CSSOM would reject
    /// anyway proves nothing): a url() value, a number, a five-digit hex, a capitalised name.
    /// </summary>
    private const string MalformedThemeScript = """
        window.__composerApplyTheme({ "--surface": "url(x)", "--text": 42, "--text-muted": "#12345", "--Accent": "#000000" })
        """;

    /// <summary>The custom properties on the document root's INLINE style — the ones the host's push set, and nothing the stylesheet declared.</summary>
    private const string RootCustomPropertiesScript = """
        (() => {
          const s = document.documentElement.style;
          const o = {};
          for (let i = 0; i < s.length; i++) { const n = s[i]; if (n.startsWith('--')) o[n] = s.getPropertyValue(n).trim(); }
          return JSON.stringify(o);
        })()
        """;

    /// <summary>
    /// The same census, in the page: every element carrying its own text (or a placeholder, or a
    /// form control), its computed ink, the first opaque ground above it, and the opacity chain.
    /// </summary>
    private const string PageCensusScript = """
        (() => {
          const parse = s => { const m = s && s.match(/rgba?\(([^)]+)\)/); if (!m) return null;
            const p = m[1].split(',').map(Number); return { r: p[0], g: p[1], b: p[2], a: p.length > 3 ? p[3] : 1 }; };
          const over = (c, g) => ({ r: c.r * c.a + g.r * (1 - c.a), g: c.g * c.a + g.g * (1 - c.a), b: c.b * c.a + g.b * (1 - c.a), a: 1 });
          const hex = c => '#' + [c.r, c.g, c.b].map(v => Math.round(v).toString(16).padStart(2, '0')).join('').toUpperCase();
          const ground = el => {
            const chain = []; for (let e = el.parentElement; e; e = e.parentElement) chain.unshift(e);
            let acc = { r: 255, g: 255, b: 255, a: 1 };
            for (const e of chain) { const bg = parse(getComputedStyle(e).backgroundColor); if (bg && bg.a > 0) acc = over(bg, acc); }
            const own = parse(getComputedStyle(el).backgroundColor); if (own && own.a > 0) acc = over(own, acc);
            return acc; };
          const sel = el => { let s = el.tagName.toLowerCase(); if (el.id) s += '#' + el.id; if (el.className && typeof el.className === 'string') s += '.' + el.className.trim().split(/\s+/).join('.'); return s; };
          const rows = [];
          for (const el of document.querySelectorAll('body, body *')) {
            const cs = getComputedStyle(el);
            if (cs.visibility === 'hidden' || cs.display === 'none') continue;
            const rect = el.getBoundingClientRect(); if (rect.width === 0 || rect.height === 0) continue;
            const own = [...el.childNodes].filter(n => n.nodeType === 3 && n.textContent.trim()).map(n => n.textContent.trim()).join(' ');
            const isField = el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT';
            const placeholder = el.placeholder || '';
            if (!own && !placeholder && !isField && !el.isContentEditable) continue;
            let op = 1; for (let e = el; e; e = e.parentElement) op *= parseFloat(getComputedStyle(e).opacity || '1');
            const ink = parse(cs.color) || { r: 0, g: 0, b: 0, a: 1 };
            rows.push({ sel: sel(el), text: own || placeholder || (isField ? '(' + el.tagName.toLowerCase() + ')' : '(editable)'),
              ink: hex(ink), inkCss: cs.color, ground: hex(ground(el)), opacity: op * ink.a,
              size: parseFloat(cs.fontSize), bold: parseInt(cs.fontWeight) >= 700, disabled: el.disabled === true });
          }
          return JSON.stringify(rows);
        })()
        """;

    // ────────────────────────────────────────────────────────────── the mechanics ──

    /// <summary>The WCAG 2.x relative luminance of an opaque colour.</summary>
    internal static double Luminance(Color c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));
    }

    /// <summary>The WCAG 2.x contrast ratio between two opaque colours.</summary>
    internal static double Contrast(Color foreground, Color background)
    {
        var a = Luminance(foreground);
        var b = Luminance(background);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>Composites <paramref name="over"/> (with its own alpha) onto an opaque ground.</summary>
    internal static Color Composite(Color over, Color ground, double extraOpacity = 1.0)
    {
        var alpha = (over.A / 255.0) * extraOpacity;
        return Color.FromRgb(
            (byte)Math.Round((over.R * alpha) + (ground.R * (1 - alpha))),
            (byte)Math.Round((over.G * alpha) + (ground.G * (1 - alpha))),
            (byte)Math.Round((over.B * alpha) + (ground.B * (1 - alpha))));
    }

    private static IEnumerable<DependencyObject> Visuals(DependencyObject root)
    {
        var stack = new Stack<DependencyObject>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            if (current is not (Visual or System.Windows.Media.Media3D.Visual3D)) continue;

            var count = VisualTreeHelper.GetChildrenCount(current);
            for (var i = count - 1; i >= 0; i--)
            {
                stack.Push(VisualTreeHelper.GetChild(current, i));
            }
        }
    }

    private static IEnumerable<Run> Runs(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Run run:
                    yield return run;
                    break;
                case Span span:
                    foreach (var nested in Runs(span.Inlines)) yield return nested;
                    break;
            }
        }
    }

    /// <summary>The visual that draws a text box's glyphs — hidden alone so the well stays painted.</summary>
    private static UIElement GlyphHost(Control box)
    {
        var host = box.Template?.FindName("PART_ContentHost", box) as UIElement;
        return host ?? Visuals(box).OfType<ScrollViewer>().FirstOrDefault() ?? box;
    }

    private static byte[] Render(FrameworkElement frame, out int width, out int height)
    {
        width = Math.Max(1, (int)Math.Ceiling(frame.ActualWidth));
        height = Math.Max(1, (int)Math.Ceiling(frame.ActualHeight));

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(new VisualBrush(frame), null, new Rect(0, 0, width, height));
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        return pixels;
    }

    private static Rect BoundsIn(FrameworkElement frame, UIElement element)
    {
        if (element is not FrameworkElement fe || !fe.IsDescendantOf(frame)) return Rect.Empty;

        var bounds = fe.TransformToAncestor(frame).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
        bounds.Intersect(new Rect(0, 0, frame.ActualWidth, frame.ActualHeight));
        return bounds;
    }

    /// <summary>
    /// The ground under an element: five samples across its bounds, composited over the window's
    /// own ground where nothing painted, the median by luminance reported and any spread noted.
    /// </summary>
    private static Color Sample(byte[] pixels, int width, int height, Rect bounds, Color windowGround, out string note)
    {
        var points = new[]
        {
            (0.5, 0.5), (0.25, 0.5), (0.75, 0.5), (0.5, 0.25), (0.5, 0.75),
        };

        var samples = new List<Color>();
        foreach (var (fx, fy) in points)
        {
            var x = Math.Clamp((int)(bounds.X + (bounds.Width * fx)), 0, width - 1);
            var y = Math.Clamp((int)(bounds.Y + (bounds.Height * fy)), 0, height - 1);
            var i = ((y * width) + x) * 4;

            var a = pixels[i + 3];
            if (a == 0)
            {
                samples.Add(windowGround);
                continue;
            }

            // Pbgra32 is premultiplied; un-premultiply before compositing over the window ground.
            var over = Color.FromArgb(
                a,
                (byte)Math.Min(255, pixels[i + 2] * 255 / a),
                (byte)Math.Min(255, pixels[i + 1] * 255 / a),
                (byte)Math.Min(255, pixels[i] * 255 / a));
            samples.Add(a == 255 ? Color.FromRgb(over.R, over.G, over.B) : Composite(over, windowGround));
        }

        var ordered = samples.OrderBy(Luminance).ToList();
        var median = ordered[ordered.Count / 2];
        var spread = Luminance(ordered[^1]) - Luminance(ordered[0]);
        note = spread > 0.05 ? string.Format(CultureInfo.InvariantCulture, "(varies: {0}…{1})", Hex(ordered[0]), Hex(ordered[^1])) : string.Empty;
        return median;
    }

    private static double EffectiveOpacity(DependencyObject element)
    {
        var opacity = 1.0;
        for (var current = element; current is not null; current = Parent(current))
        {
            if (current is UIElement ui) opacity *= ui.Opacity;
        }

        return opacity;
    }

    private static DependencyObject? Parent(DependencyObject element) =>
        element switch
        {
            Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element),
            _ => LogicalTreeHelper.GetParent(element),
        };

    /// <summary>The nearest ancestor with an automation name — the surface, pane or zone the text belongs to.</summary>
    private static string OwningSurface(DependencyObject element)
    {
        for (var current = Parent(element); current is not null; current = Parent(current))
        {
            var name = AutomationProperties.GetName(current);
            if (!string.IsNullOrEmpty(name)) return name;

            if (current is AvalonDock.Controls.LayoutDocumentTabItem tab && tab.Model is AvalonDock.Layout.LayoutContent content)
            {
                return "tab: " + content.Title;
            }

            if (current is AvalonDock.Controls.LayoutAnchorableTabItem anchorTab && anchorTab.Model is AvalonDock.Layout.LayoutContent anchorContent)
            {
                return "tool tab: " + anchorContent.Title;
            }
        }

        return "(window)";
    }

    private static string Describe(DependencyObject element, UIElement glyphs)
    {
        var text = new StringBuilder(element.GetType().Name);
        text.Append(NameOf(element));

        var automation = AutomationProperties.GetName(element) ?? (ReferenceEquals(element, glyphs) ? null : AutomationProperties.GetName(glyphs));
        if (!string.IsNullOrEmpty(automation)) text.Append(" ‹").Append(automation).Append('›');

        var parent = Parent(element);
        if (parent is not null && parent is not Panel && parent is not ContentPresenter)
        {
            text.Append(" in ").Append(parent.GetType().Name).Append(NameOf(parent));
        }

        return text.ToString();
    }

    private static string NameOf(DependencyObject element) =>
        element is FrameworkElement { Name.Length: > 0 } fe ? "#" + fe.Name : string.Empty;

    private static Color Average(GradientBrush gradient)
    {
        var stops = gradient.GradientStops;
        if (stops.Count == 0) return Colors.Magenta;

        return Color.FromRgb(
            (byte)stops.Average(s => s.Color.R),
            (byte)stops.Average(s => s.Color.G),
            (byte)stops.Average(s => s.Color.B));
    }

    private static Color ParseRgb(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    private static string Hex(Color c) => string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);

    private static string Excerpt(string text)
    {
        var flat = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return flat.Length > 48 ? flat[..45] + "…" : flat;
    }

    /// <summary>The census never affirms an outside-workspace read; it attaches nothing.</summary>
    private sealed class NeverAffirms : AiDe.Core.Presentation.Composer.IAttachmentAffirmation
    {
        public bool Confirm(AiDe.Core.Presentation.Composer.OutsideWorkspaceAffirmation affirmation) => false;
    }
}
