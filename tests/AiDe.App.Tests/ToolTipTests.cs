using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

/// <summary>
/// The tooltip is themed, and the platform default it replaces is measured rather than assumed.
/// </summary>
/// <remarks>
/// <para><b>TC1's base set does not name <c>ToolTip</c>, and on this product that omission matters
/// more than most.</b> The design language's icon system requires that every icon-only control
/// carries a tooltip and an accessible name — <i>an icon is never the sole label</i> — so on the
/// activity rail the tooltip IS the label. It is also the one popup that would have gone on
/// rendering the system info-tip after every other surface was dark.</para>
///
/// <para><b>Both readings are taken here.</b> The unthemed one establishes that there was something
/// to fix; the themed one establishes that it is fixed and legible. Reporting only the second would
/// be the same shape as a review that measures the artifact it produced and not the product.</para>
/// </remarks>
public sealed class ToolTipTests(ITestOutputHelper output)
{
    [Fact]
    public void TheToolTipIsTheAppsPopup_NotThePlatformsInfoTip()
    {
        Sta.Run(() =>
        {
            var theme = ThemeProbe.AppTheme();

            static (Color Ink, Color Ground) Resolve(ResourceDictionary? dictionary)
            {
                // A ToolTip is a Popup's content and never joins the window's visual tree, so it is
                // shown in its own right and read once it has one.
                var tip = new ToolTip { Content = "Explorer — graph & reader" };

                if (dictionary is not null)
                {
                    tip.Resources = dictionary;
                    tip.Style = dictionary[typeof(ToolTip)] as Style;
                }

                tip.IsOpen = true;
                tip.UpdateLayout();

                try
                {
                    return (
                        ((SolidColorBrush)tip.Foreground).Color,
                        ((SolidColorBrush)tip.Background).Color);
                }
                finally
                {
                    tip.IsOpen = false;
                }
            }

            var platform = Resolve(null);
            var themed = Resolve(theme);

            output.WriteLine($"platform tooltip: ink {platform.Ink} on ground {platform.Ground} "
                + $"= {ThemeProbe.Contrast(platform.Ink, platform.Ground):0.00}:1");
            output.WriteLine($"themed tooltip:   ink {themed.Ink} on ground {themed.Ground} "
                + $"= {ThemeProbe.Contrast(themed.Ink, themed.Ground):0.00}:1");

            // The finding: the platform's tooltip is a LIGHT surface. It passes contrast and fails
            // the design system, which is why it needed naming rather than an accessibility fix.
            Assert.True(ThemeProbe.Luminance(platform.Ground) > 0.5,
                $"the platform tooltip ground measured {platform.Ground}, which is not light — so "
                + "the premise of this style is wrong and the style should be reconsidered, not the "
                + "assertion relaxed.");

            // The fix: our popup ground, our ink, and legible on it.
            Assert.Equal(ThemeProbe.Token(theme, "MenuBackgroundBrush"), themed.Ground);
            Assert.Equal(ThemeProbe.Token(theme, "TextBrush"), themed.Ink);

            var ratio = ThemeProbe.Contrast(themed.Ink, themed.Ground);
            Assert.True(ratio >= 4.5,
                $"the themed tooltip measures {ratio:0.00}:1, below the 4.5:1 floor for body text — "
                + "and on the activity rail the tooltip is the label, not a hint.");
        });
    }
}
