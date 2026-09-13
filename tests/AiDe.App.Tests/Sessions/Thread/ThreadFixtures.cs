using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions.Thread;

/// <summary>
/// The review's fixture turns (<c>docs/reviews/ui-session-conversation.md</c>; the mockup's
/// <c>TURNS</c>): five turns, and the 40-turn variant whose <c>b17</c> is a failed PAST turn —
/// labelled synthetic, at the event level, so every oracle here renders what the product would
/// render for a session that ran them.
/// </summary>
/// <remarks>
/// <b>The reply is events, never a field (Ruling 81).</b> A lane's answer arrives as
/// <c>agent.msg</c> chunks — here <see cref="Reply"/> splits the sentence as the wire would — and
/// the thread renders <c>Coalesce(Events)</c>; the conductor's bracketing lines carry the mockup's
/// <c>run.accepted</c> / <c>run.completed</c> kinds and the lane's working lines are tool rows, so
/// nothing but the answer folds. The kinds are the mockup's event model
/// (<c>session-conversation.html</c> "the event model"), not the mapper's whole vocabulary.
/// </remarks>
internal static class ThreadFixtures
{
    public static readonly DateTimeOffset T0 = new(2026, 9, 12, 14, 1, 0, TimeSpan.Zero);

    public static IReadOnlyList<DecorationRow> Decorations(string cls, string tier, string? lease, string shape, string? template = null)
    {
        var rows = new List<DecorationRow>
        {
            new("class", cls, "session-default", cls == "free-form" ? "no class ranks this turn" : "chosen for this prompt"),
            new("tier", tier, "rule", tier == "T0" ? "no goal block" : "goal block filled by you, one lease"),
            new("lease", lease ?? "read-only — nothing will be written", "derived", lease is null ? "none yet" : "from your mention"),
            new("shape", shape, "projection", ""),
        };
        if (template is not null)
        {
            rows.Add(new("template", template, "operator", "picked from the template control"));
        }

        return rows;
    }

    public static EventLine Line(int seconds, string lane, string text, string kind = Coalesce.MessageKind) =>
        new(T0.AddSeconds(seconds), lane, kind, text, "run");

    /// <summary>The lane's answer as the wire chunks it: <paramref name="text"/> cut into <paramref name="chunks"/> <c>agent.msg</c> lines at one second.</summary>
    public static IEnumerable<EventLine> Reply(int seconds, string lane, string text, int chunks = 3)
    {
        var size = Math.Max(1, (int)Math.Ceiling(text.Length / (double)chunks));
        for (var at = 0; at < text.Length; at += size)
        {
            yield return Line(seconds, lane, text[at..Math.Min(text.Length, at + size)]);
        }
    }

    /// <summary>The five review turns, b1 answered by the conductor, b2–b5 completed by claude-code; each answer arrives chunked.</summary>
    public static List<TurnView> Five()
    {
        return
        [
            Turn(1, "Map the layout stores in this workspace and name the invariant each one protects.",
                Decorations("review", "T0", null, "message"), TurnState.Answered,
                new OutcomeView("conductor", null, null, new Spend(1_200, 0, 660, 1), TimeSpan.FromSeconds(6), 3),
                [Line(12, "conductor", "Block b1 accepted, tier T0, 0 lanes", "run.accepted"),
                 Line(13, "conductor", "Two stores. LayoutStore (tree schema, v4, a migration chain) and ZoneLayoutStore (zone schema, v1, discards on any mismatch)."),
                 Line(18, "conductor", "answered (1,860 tokens)", "run.completed")]),
            Turn(2, "Refactor the layout store's migration chain so a newer schema is refused with a report; touch only @src/AiDe.Core/Workbench/.",
                Decorations("extraction", "T1", "src/AiDe.Core/Workbench/**", "goal block"), TurnState.Completed,
                new OutcomeView("claude-code", null, 3, new Spend(10_000, 1_840, 2_400, 3), TimeSpan.FromSeconds(252), 142),
                [.. Lines(2, 139), .. Reply(140, "claude-code", "Done. LayoutStore.Load now refuses an envelope whose schema is newer than 4 and reports it on the status strip.")]),
            Turn(3, "ContrastFloorTests is green on the same commit where the shell shows 2.37:1. Find why; touch only @tests/AiDe.App.Tests/.",
                Decorations("defect", "T1", "tests/AiDe.App.Tests/**", "goal block"), TurnState.Completed,
                new OutcomeView("claude-code", null, 1, new Spend(8_000, 900, 1_900, 2), TimeSpan.FromSeconds(185), 96),
                [.. Lines(3, 93), .. Reply(94, "claude-code", "Root cause: the theory exercises ThemeBrushes, which the product does not construct.")]),
            Turn(4, "Apply the contrast fix to the dock tab strip and the three menu brushes under @src/AiDe.App/Workbench/; the census stays at 180 of 180.",
                Decorations("ui-feedback", "T2", "src/AiDe.App/Workbench/**", "goal block", "change-order v2"), TurnState.Completed,
                new OutcomeView("claude-code", null, 5, new Spend(25_000, 4_000, 6_200, 5), TimeSpan.FromSeconds(700), 388),
                [.. Lines(4, 385), .. Reply(386, "claude-code", "Both seams landed: the selected-inactive tab pairs text on surface with the 2px muted edge.")]),
            Turn(5, "Write this session's proof pack under @docs/proof/: what changed, what is still open, and the sha each claim cites.",
                Decorations("free-form", "T1", "docs/proof/**", "goal block"), TurnState.Completed,
                new OutcomeView("claude-code", null, 1, new Spend(3_000, 200, 1_100, 1), TimeSpan.FromSeconds(62), 51),
                [.. Lines(5, 48), .. Reply(49, "claude-code", "Wrote docs/proof/pp-0142.md: three changes with their shas, two open items.")]),
        ];
    }

    /// <summary>Forty turns (the five, cycled), with b17 a failed PAST turn — folded like a completed one (SC7).</summary>
    public static List<TurnView> Forty()
    {
        var five = Five();
        var turns = new List<TurnView>();
        for (var i = 0; i < 40; i++)
        {
            var template = five[i % 5];
            var ordinal = i + 1;
            turns.Add(ordinal == 17
                ? Turn(17, template.SourceText, template.Decorations, TurnState.Failed,
                    new OutcomeView("claude-code", 1, 0, null, TimeSpan.FromSeconds(37), 12),
                    [.. Lines(17, 11), Line(12, "claude-code", "b17 line 12: exit 1 — the lease refused docs/audit/", "stderr")])
                : Turn(ordinal, template.SourceText, template.Decorations, template.State, template.Outcome, template.Events, at: T0.AddMinutes(7 * i)));
        }

        return turns;
    }

    /// <summary>A running turn at the end: its last lines live, Stop the one control.</summary>
    public static TurnView Running(int ordinal, int lines = 6) =>
        Turn(ordinal, "Write this session's proof pack under @docs/proof/.", Decorations("free-form", "T1", "docs/proof/**", "goal block"),
            TurnState.Running, null, Lines(ordinal, lines));

    public static TurnView Turn(
        int ordinal, string words, IReadOnlyList<DecorationRow> decorations, TurnState state,
        OutcomeView? outcome, IReadOnlyList<EventLine> events, DateTimeOffset? at = null) =>
        new(ordinal, $"env-{ordinal:x4}", words, decorations, state, outcome, null, events, $"task_class: free-form\nmessage: |\n  {words}\n", at ?? T0.AddMinutes(ordinal));

    /// <summary>The lane's working lines — tool rows, one per event, so the fold's event count is the line count and nothing but the answer folds.</summary>
    public static List<EventLine> Lines(int ordinal, int count) =>
        [.. Enumerable.Range(1, count).Select(i => Line(i, i % 3 == 0 ? "conductor" : "claude-code", $"b{ordinal} line {i}: read docs/proof/pp-0141.md", i % 5 == 0 ? "tool.call" : "tool.result"))];

    /// <summary>A feed over a pre-loaded read model, with a recording announcer.</summary>
    public static (ThreadFeed Feed, RunChannelSessionThread Thread, RecordingAnnouncer Announcer) Feed(IEnumerable<TurnView> turns)
    {
        var thread = RunChannelSessionThread.Preloaded(turns);
        var announcer = new RecordingAnnouncer();
        return (new ThreadFeed(thread, announcer, "test"), thread, announcer);
    }

    /// <summary>The visual descendants of a type.</summary>
    public static IEnumerable<T> Visuals<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var inner in Visuals<T>(child))
            {
                yield return inner;
            }
        }
    }

    public static ListBoxItem Container(ThreadFeed feed, int index)
    {
        feed.ScrollIntoView(feed.Items[index]);
        feed.UpdateLayout();
        return (ListBoxItem)feed.ItemContainerGenerator.ContainerFromIndex(index)
            ?? throw new InvalidOperationException($"the container at {index} is not realized");
    }

    /// <summary>A disclosure's header toggle — the one stop of a fold (P8), found through the template's part name.</summary>
    public static System.Windows.Controls.Primitives.ToggleButton HeaderToggle(Expander expander)
    {
        expander.ApplyTemplate();
        return (System.Windows.Controls.Primitives.ToggleButton?)expander.Template.FindName("HeaderSite", expander)
            ?? throw new InvalidOperationException("the disclosure's template has no HeaderSite");
    }

    /// <summary>The fold's disclosure of a container (named "<n> events").</summary>
    public static Expander Fold(ListBoxItem container) =>
        Visuals<Expander>(container).First(e => System.Windows.Automation.AutomationProperties.GetName(e).EndsWith("event", StringComparison.Ordinal) || System.Windows.Automation.AutomationProperties.GetName(e).EndsWith("events", StringComparison.Ordinal));

    /// <summary>The app's resource dictionary (App.xaml), for a render under the real theme (L5's rows).</summary>
    public static ResourceDictionary AppTheme() => ThemeProbe.AppTheme();
}
