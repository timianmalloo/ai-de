using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// The Center's empty copy — what the projection's placeholder renders while a host's Center holds
/// no document (Ruling 83 condition 2; DESIGN.md's <i>"The empty states — the Center's copy in
/// Coding"</i> row, and the <i>Nothing open here</i> row). <b>Two true sentences, by state</b>: the
/// copy is a function of the host's zones and of which session surfaces have a live document, so it
/// is derived at every render rather than kept in step.
/// </summary>
/// <remarks>
/// <para><b>Why the shell composes it and not the factory.</b> <see cref="ZonesToTree.WelcomePlaceholder"/>
/// is a view-only surface — it has no <see cref="SurfaceContentFactory.Kinds"/> row, because a row
/// would make it openable and admissible — and the copy needs the zones, which the factory does not
/// see. Before this the placeholder fell through to the factory's <i>"… is not available in this
/// build"</i>, a build-defect sentence for the ordinary empty state (SH-4.2, L4's red).</para>
/// <para><b>No first action while a session is open.</b> The first action is the editor at Left,
/// where focus lands; a <i>Maximize</i> here would hide the zone that offers it. The copy still
/// carries a focus target — its root — because the landing falls back to it when the Left is
/// collapsed (<see cref="PerspectiveShell.LandingSurfaceFor"/>), and a landing with nothing
/// focusable falls through to an arbitrary tab header (the UX &amp; Accessibility lens, SH-4.1).</para>
/// </remarks>
public static class CenterEmptyState
{
    /// <summary>The copy: a heading, a second line, and whether the New session action is offered.</summary>
    public sealed record Copy(string Heading, string Body, bool OffersNewSession);

    /// <summary>The New session action's caption and its bound gesture, as the catalog spells them.</summary>
    private const string NewSessionGesture = "Ctrl+N";

    /// <summary>
    /// The copy for <paramref name="host"/>'s empty Center over <paramref name="zones"/>:
    /// Coding with no session document in the layout → <i>No session open.</i> + the action;
    /// with one open elsewhere in the frame → <i>The session is docked at the &lt;zone&gt;.</i>;
    /// with a session surface whose document could not be revived → <i>Nothing open here.</i>;
    /// any other host → <i>Nothing open here.</i> and the View menu.
    /// </summary>
    /// <param name="isLive">Whether a session surface id has a live document behind it (the shell's registry).</param>
    public static Copy CopyFor(Perspective host, WorkbenchLayout zones, Func<string, bool> isLive)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(zones);
        ArgumentNullException.ThrowIfNull(isLive);

        if (host != PerspectiveSet.Coding)
        {
            return new Copy("Nothing open here.", "Open a view from the View menu.", OffersNewSession: false);
        }

        var sessions = Enum.GetValues<ZoneId>()
            .Where(z => z != ZoneId.Center)
            .SelectMany(z => zones.Zone(z).Surfaces().Where(s => s.Kind == Sessions.SessionDocumentSurface.Kind).Select(s => (Zone: z, s.SurfaceId)))
            .ToList();

        if (sessions.Count == 0)
        {
            return new Copy("No session open.", "Or open a recent one from File → Recent sessions.", OffersNewSession: true);
        }

        var live = sessions.Where(s => isLive(s.SurfaceId)).ToList();
        if (live.Count == 0)
        {
            // A surface the saved arrangement restored, whose session is gone: the island at Left
            // says so; the Center points at it rather than claiming a session is open.
            return new Copy("Nothing open here.", $"The session at the {Where(sessions)} could not be restored; its recovery is on the {Where(sessions)}.", OffersNewSession: false);
        }

        var heading = live.Count == 1
            ? $"The session is docked at the {Where(live)}."
            : $"The sessions are docked at the {Where(live)}.";
        return new Copy(heading, "Code viewers, prompt drafts and search open here, from the View menu.", OffersNewSession: false);
    }

    /// <summary>The zone word(s) the sentence names — "left", "left and the bottom" — from where the sessions actually are.</summary>
    private static string Where(IEnumerable<(ZoneId Zone, string SurfaceId)> sessions)
    {
        var words = sessions.Select(s => s.Zone).Distinct().OrderBy(z => z).Select(z => z.ToString().ToLowerInvariant()).ToList();
        return words.Count == 1 ? words[0] : string.Join(" and the ", words);
    }

    /// <summary>
    /// The rendered copy: the <c>state.not-declared</c> shape — a heading, a line, the one action
    /// when offered — named for assistive technology from its own sentences, focusable at its root.
    /// </summary>
    /// <param name="newSession">Runs the New session verb; only bound when the copy offers it.</param>
    public static FrameworkElement Build(Copy copy, Action newSession)
    {
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(newSession);

        var heading = new TextBlock
        {
            Text = copy.Heading,
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
        };
        heading.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        var body = new TextBlock
        {
            Text = copy.Body,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
        };
        body.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var stack = new StackPanel { MaxWidth = 380, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24) };
        stack.Children.Add(heading);

        if (copy.OffersNewSession)
        {
            var action = new Button
            {
                Content = "New session",
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                MinHeight = 24,
            };
            AutomationProperties.SetName(action, $"New session — {NewSessionGesture}");
            AutomationProperties.SetAcceleratorKey(action, NewSessionGesture);
            action.Click += (_, _) => newSession();
            stack.Children.Add(action);
        }

        stack.Children.Add(body);

        // The root is the focus target when no action is offered; with the action, the action is
        // first in tab order and the root stays reachable for the landing's MoveFocus(First).
        var host = new Grid { Focusable = !copy.OffersNewSession, MinHeight = 120 };
        host.Children.Add(stack);
        AutomationProperties.SetName(host, $"{copy.Heading} {copy.Body}");
        return host;
    }
}
