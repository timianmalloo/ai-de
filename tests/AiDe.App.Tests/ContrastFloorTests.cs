using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

/// <summary>
/// The eleven measured pairings from <c>docs/reviews/ui-operator-feedback.md</c> §2a, re-measured
/// against the running theme on every build.
/// </summary>
/// <remarks>
/// <para><b>Why this is a test and not a paragraph.</b> The review measured eleven failing pairs,
/// seven of them at 1.15:1 and 1.27:1 — text that is not low-contrast but invisible. Every one was a
/// control that never reached a token, and every one was added by somebody who did not know that
/// this shell themed containers and left leaves to a light platform. A prose lesson would not have
/// stopped the next one. <i>A lesson recorded as prose is a memoir</i> (CI6).</para>
///
/// <para><b>What it can and cannot see.</b> It reads the resolved brushes of real controls in a real
/// shown window under the real App.xaml, which is stronger than reading source and stronger than a
/// screenshot of one state. It cannot see DWM's non-client area, a high-DPI raster, or whether the
/// copy is true. Sites 7, 8 and 9 construct the product's own surfaces; 1–6 and 10 construct the
/// control the cited line constructs, on the ground its container paints, and say so.</para>
/// </remarks>
public sealed class ContrastFloorTests(ITestOutputHelper output)
{
    /// <summary>WCAG 1.4.3 — body text.</summary>
    private const double TextFloor = 4.5;

    /// <summary>WCAG 1.4.11 — a meaningful graphic or UI component.</summary>
    private const double GraphicFloor = 3.0;

    [Fact]
    public void EveryMeasuredPairingClearsItsFloor()
    {
        var measured = new List<ThemeProbe.Pairing>();

        // ── Sites 1–6 and 10: the control the cited line constructs, on its container's ground ──
        ThemeProbe.OnThemedWindow("SurfaceBrush", _ =>
        {
            var root = new Grid();

            var onSurface = new Border { Name = "OnSurface" };
            onSurface.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
            var surfaceStack = new StackPanel();
            onSurface.Child = surfaceStack;

            // 1, 2, 3 — ComposerSurface.cs:100, :81, :82
            surfaceStack.Children.Add(new TextBlock { Name = "Compiled", Text = "Compiled view" });
            surfaceStack.Children.Add(new TextBlock
            {
                Name = "Lease",
                Text = "Lease: not derivable until the draft names something",
            });
            surfaceStack.Children.Add(new TextBlock { Name = "Status", Text = "…" });

            // 4 — CanvasSurface.cs:224, the empty state
            surfaceStack.Children.Add(new TextBlock
            {
                Name = "CanvasEmpty",
                Text = "The graph canvas could not start.",
                TextWrapping = TextWrapping.Wrap,
            });

            // 5 — ConsoleSurface.cs:131,144, a lane node in the filter tree
            var tree = new TreeView { Name = "FilterTree" };
            var node = new TreeViewItem { Name = "LaneNode", Header = "claude-code (18)" };
            tree.Items.Add(node);
            surfaceStack.Children.Add(tree);

            // 6 — ContextMapSurface.cs:202, a crossing header on the raised card
            var onRaised = new Border { Name = "OnRaised" };
            onRaised.SetResourceReference(Border.BackgroundProperty, "SurfaceRaisedBrush");
            var raisedHeader = new TextBlock
            {
                Name = "CrossingHeader",
                Text = "AiDe.Core  →  AiDe.App     12 edge(s)",
            };
            onRaised.Child = raisedHeader;

            // 10 — the disabled rail glyph, on the sunken rail
            var rail = new Border { Name = "Rail" };
            rail.SetResourceReference(Border.BackgroundProperty, "SurfaceSunkenBrush");
            var disabled = new Button
            {
                Name = "DisabledRailItem",
                IsEnabled = false,
                Height = 44,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Content = "○",
            };
            rail.Child = disabled;

            var column = new StackPanel();
            column.Children.Add(onSurface);
            column.Children.Add(onRaised);
            column.Children.Add(rail);
            root.Children.Add(column);
            return root;
        },
        (_, root) =>
        {
            ThemeProbe.Pairing Text(int number, string name, string site, string what, double floor = TextFloor)
            {
                var block = ThemeProbe.FirstDescendant<TextBlock>(root, t => t.Name == name)!;
                Assert.True(block is not null, $"the probe did not build the '{name}' subject");
                return new ThemeProbe.Pairing(
                    number, site, what, floor,
                    ThemeProbe.Ink(block!), ThemeProbe.EffectiveBackground(block!));
            }

            measured.Add(Text(1, "Compiled", "ComposerSurface.cs:100", "TextBlock \"Compiled view\""));
            measured.Add(Text(2, "Lease", "ComposerSurface.cs:81", "TextBlock lease sentence"));
            measured.Add(Text(3, "Status", "ComposerSurface.cs:82", "TextBlock _status"));
            measured.Add(Text(4, "CanvasEmpty", "CanvasSurface.cs:224", "TextBlock empty state"));

            var lane = ThemeProbe.FirstDescendant<TreeViewItem>(root, t => t.Name == "LaneNode")!;
            measured.Add(new ThemeProbe.Pairing(
                5, "ConsoleSurface.cs:131,144", "TreeViewItem header", TextFloor,
                ThemeProbe.Ink(lane), ThemeProbe.EffectiveBackground(lane)));

            measured.Add(Text(6, "CrossingHeader", "ContextMapSurface.cs:202", "TextBlock crossing header"));

            var railItem = ThemeProbe.FirstDescendant<Button>(root, b => b.Name == "DisabledRailItem")!;
            measured.Add(new ThemeProbe.Pairing(
                10, "MainWindow.xaml rail + App.xaml chrome", "disabled glyph", GraphicFloor,
                ThemeProbe.Ink(railItem), ThemeProbe.EffectiveBackground(railItem)));
        });

        // ── Site 7: the real New Session sheet ──────────────────────────────────────────────
        var workspace = Path.Combine(Path.GetTempPath(), "aide-contrast-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        try
        {
            ThemeProbe.OnThemedWindow("SurfaceRaisedBrush", _ =>
            {
                var registry = new AiDe.Core.AgentPlane.ProviderRegistry(
                [
                    new AiDe.Core.AgentPlane.ProviderRow(
                        "anthropic",
                        AiDe.Core.AgentPlane.ProviderAuth.Subscription,
                        [new AiDe.Core.AgentPlane.ProviderAccount(
                            "max-personal", AiDe.Core.AgentPlane.AccountHealth.Ready)]),
                ]);

                var sheet = new AiDe.Core.Presentation.Sessions.NewSessionSheetViewModel(
                    workspace, workspace, registry, DateTimeOffset.UtcNow);

                // The dialog paints its own ground on the WINDOW; mirror that here so the pairing is
                // the one the operator sees rather than one this probe invented.
                var body = new Border();
                body.SetResourceReference(Border.BackgroundProperty, "SurfaceRaisedBrush");
                body.Child = AiDe.App.Workbench.Sessions.NewSessionSheetDialog.Build(
                    sheet, announce: null, onCreate: () => { });
                return body;
            },
            (_, root) =>
            {
                var toggle = ThemeProbe.FirstDescendant<CheckBox>(root)!;
                Assert.True(toggle is not null,
                    "the sheet rendered no backend checkbox, so site 7 measured nothing (DC-016)");

                // The ground is read from the checkbox's PARENT. A CheckBox's own Background paints
                // the bullet well, not the row the label sits on, so measuring the label against it
                // would answer a question nobody asked — the operator reads the label against the
                // sheet.
                measured.Add(new ThemeProbe.Pairing(
                    7, "NewSessionSheetDialog.cs:138", "CheckBox label on the sheet", TextFloor,
                    ThemeProbe.Ink(toggle!),
                    ThemeProbe.EffectiveBackground(
                        System.Windows.Media.VisualTreeHelper.GetParent(toggle!)!)));
            });
        }
        finally
        {
            try { Directory.Delete(workspace, recursive: true); } catch (IOException) { }
        }

        // ── Site 8: the real command palette — the pair the previous partial fix CREATED ─────
        ThemeProbe.OnThemedWindow("SurfaceBrush", _ =>
        {
            var service = new LayoutService();
            var announcer = new RecordingAnnouncer();
            var controller = new WorkbenchController(service, announcer);
            var palette = new CommandPalette(controller, announcer);
            palette.Open();
            return palette.Root;
        },
        (_, root) =>
        {
            var list = ThemeProbe.FirstDescendant<ListBox>(root)!;
            Assert.True(list is not null, "the palette rendered no ListBox, so site 8 measured nothing");
            list!.UpdateLayout();

            var background = ThemeProbe.EffectiveBackground(list);
            var item = list.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;

            Assert.True(item is not null,
                "the palette listed no commands, so the row ink could not be read (DC-016)");

            measured.Add(new ThemeProbe.Pairing(
                8, "CommandPalette.cs:37", "ListBox row ink on list ground", TextFloor,
                ThemeProbe.Ink(item!), background));
        });

        // ── Site 9: the class box whose ground was a resource key that did not exist ─────────
        ThemeProbe.OnThemedWindow("SurfaceBrush", _ =>
        {
            var surface = new ClassDiagramSurface();
            surface.ShowGraph(
                [new CanvasNode("Shop.Order", "Order", "class", false, null)],
                []);
            return surface;
        },
        (_, root) =>
        {
            var box = ThemeProbe.FirstDescendant<Border>(
                root, b => (AutomationProperties.GetName(b) ?? string.Empty).StartsWith("class ", StringComparison.Ordinal))!;

            Assert.True(box is not null,
                "the class diagram rendered no type box, so site 9 measured nothing (DC-016)");

            var label = ThemeProbe.FirstDescendant<TextBlock>(box!)!;
            Assert.True(label is not null, "the type box carried no label to measure");

            measured.Add(new ThemeProbe.Pairing(
                9, "ClassDiagramSurface.cs:788,957", "type-box label on its card", TextFloor,
                ThemeProbe.Ink(label!), ThemeProbe.EffectiveBackground(label!)));
        });

        // ── Site 11: a token pair, and a DECLARED DEVIATION. Measured and reported, never
        //    asserted — DESIGN.md's palette table states that the border is decorative and that
        //    spacing carries the grouping. Recording it here keeps the deviation honest: if the
        //    border ever becomes the only separator, this number is already on the table.
        Sta.Run(() =>
        {
            var theme = ThemeProbe.AppTheme();
            measured.Add(new ThemeProbe.Pairing(
                11, "{colors.border} on {colors.surface}", "1px separator (declared deviation)",
                GraphicFloor,
                ThemeProbe.Token(theme, "BorderBrush"),
                ThemeProbe.Token(theme, "SurfaceBrush")));
        });

        var path = ThemeProbe.WriteReport("The eleven pairings, re-measured", measured.OrderBy(m => m.Number));
        foreach (var pairing in measured.OrderBy(m => m.Number))
        {
            output.WriteLine(pairing.Row);
        }

        output.WriteLine($"report: {path}");

        // DC-016: a walk that measured nothing passes every floor.
        Assert.Equal(11, measured.Count);

        var failing = measured
            .Where(m => m.Number != 11)      // the declared deviation, dispositioned in DESIGN.md
            .Where(m => !m.Clears)
            .Select(m => m.Row)
            .ToList();

        Assert.True(failing.Count == 0,
            "these pairings are below their floor in the running theme:" + Environment.NewLine
            + string.Join(Environment.NewLine, failing) + Environment.NewLine
            + $"the full table is at {path}");
    }

    /// <summary>
    /// Every base control type named by TC1 has an implicit default, and every one states its ink
    /// and its ground.
    /// </summary>
    /// <remarks>
    /// <b>This is the control, not the fix.</b> Clearing eleven pairings repairs eleven instances;
    /// the class is "coverage is per-element, so the next control regresses silently". A missing
    /// implicit style is what made twenty-eight instantiations wrong at once, and a style that sets
    /// exactly one of the two is what turned dark-on-dark into light-on-white (TC2). Both are
    /// findings here.
    /// </remarks>
    [Theory]
    [InlineData("TextBlock")]
    [InlineData("TextBox")]
    [InlineData("Button")]
    [InlineData("ToggleButton")]
    [InlineData("ComboBox")]
    [InlineData("CheckBox")]
    [InlineData("RadioButton")]
    [InlineData("Label")]
    [InlineData("ListBox")]
    [InlineData("ListBoxItem")]
    [InlineData("TreeView")]
    [InlineData("TreeViewItem")]
    [InlineData("TabItem")]
    [InlineData("PasswordBox")]
    [InlineData("RichTextBox")]
    [InlineData("Expander")]
    [InlineData("GroupBox")]
    [InlineData("Window")]
    public void TheBaseControlSetHasAnImplicitDefault_SettingInkAndGroundTogether(string typeName)
    {
        Sta.Run(() =>
        {
            var theme = ThemeProbe.AppTheme();
            var type = typeof(Control).Assembly.GetType("System.Windows.Controls." + typeName)
                       ?? typeof(Control).Assembly.GetType("System.Windows.Controls.Primitives." + typeName)
                       ?? typeof(Window).Assembly.GetType("System.Windows." + typeName);

            Assert.True(type is not null, $"there is no WPF type named {typeName}");

            var style = theme[type!] as Style;

            Assert.True(style is not null,
                $"{typeName} has no implicit (TargetType-only) default style, so every {typeName} "
                + "created without an explicit brush renders in the platform's light theme. That is "
                + "the class this whole section exists for (TC1).");

            var properties = style!.Setters.OfType<Setter>().Select(s => s.Property.Name).ToHashSet(StringComparer.Ordinal);

            Assert.True(properties.Contains("Foreground") && properties.Contains("Background"),
                $"{typeName}'s default style sets "
                + string.Join(", ", properties.OrderBy(p => p, StringComparer.Ordinal))
                + " — a partial pairing is worse than none (TC2). Theming an ink without its ground, "
                + "or the reverse, produces the inverse defect; that is how the ListBoxItem fix "
                + "manufactured #E4E9EF on #FFFFFF at 1.22:1.");
        });
    }

    /// <summary>
    /// A checked checkbox looks different from an unchecked one, and the difference is legible.
    /// </summary>
    /// <remarks>
    /// <b>Measured from rendered pixels, because this question cannot be answered from properties.</b>
    /// The platform's bullet chrome paints its check glyph in a fixed near-black, so a dark ground
    /// under it yields a control whose two states composite to nearly the same image — a state that
    /// reads as the wrong answer, which is worse than an unreadable label. Reading
    /// <c>Background</c> and <c>Foreground</c> would have said this was fine.
    /// </remarks>
    [Fact]
    public void ACheckedBoxIsVisiblyDifferentFromAnUncheckedOne()
    {
        Sta.Run(() =>
        {
            var theme = ThemeProbe.AppTheme();

            BitmapSource Render(bool isChecked)
            {
                var host = new Border { Width = 24, Height = 24, Resources = theme };
                host.SetResourceReference(Border.BackgroundProperty, "SurfaceRaisedBrush");
                host.Child = new CheckBox
                {
                    IsChecked = isChecked,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                host.Measure(new Size(24, 24));
                host.Arrange(new Rect(0, 0, 24, 24));
                host.UpdateLayout();

                var bitmap = new RenderTargetBitmap(24, 24, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(host);
                return bitmap;
            }

            static byte[] Pixels(BitmapSource source)
            {
                var buffer = new byte[source.PixelWidth * source.PixelHeight * 4];
                source.CopyPixels(buffer, source.PixelWidth * 4, 0);
                return buffer;
            }

            var off = Pixels(Render(false));
            var on = Pixels(Render(true));

            var glyph = new List<Color>();
            var ground = new List<Color>();

            for (var i = 0; i < off.Length; i += 4)
            {
                var difference =
                    Math.Abs(off[i] - on[i]) + Math.Abs(off[i + 1] - on[i + 1]) + Math.Abs(off[i + 2] - on[i + 2]);

                var pixel = Color.FromRgb(on[i + 2], on[i + 1], on[i]);

                if (difference > 60)
                {
                    glyph.Add(pixel);
                }
                else
                {
                    ground.Add(Color.FromRgb(off[i + 2], off[i + 1], off[i]));
                }
            }

            Assert.True(glyph.Count >= 8,
                $"checking the box changed {glyph.Count} pixel(s). The two states are the same "
                + "image, so the control reports its value to nobody who can see it.");

            // The brightest thing the check drew, against the darkest thing it drew on.
            var brightest = glyph.MaxBy(ThemeProbe.Luminance);
            var box = ground.Count > 0 ? ground.MinBy(ThemeProbe.Luminance) : Colors.Black;
            var ratio = ThemeProbe.Contrast(brightest, box);

            output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "check glyph {0} px, brightest #{1:X2}{2:X2}{3:X2} on #{4:X2}{5:X2}{6:X2} = {7:0.00}:1",
                glyph.Count, brightest.R, brightest.G, brightest.B, box.R, box.G, box.B, ratio));

            Assert.True(ratio >= GraphicFloor,
                $"the check glyph measures {ratio:0.00}:1 against the box it is drawn in, below the "
                + $"{GraphicFloor:0.0}:1 floor for a meaningful graphic (WCAG 1.4.11).");
        });
    }
}
