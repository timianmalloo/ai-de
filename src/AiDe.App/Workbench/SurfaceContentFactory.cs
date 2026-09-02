using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Builds the content for one surface.
/// </summary>
/// <remarks>
/// The workbench does not know what a surface renders and must not: a surface's identity, state and
/// content are independent of where it is docked (US-9). This factory is the single place that
/// mapping lives, so adding a surface kind never means touching the layout model.
/// </remarks>
public sealed class SurfaceContentFactory(
    IWorkspaceQueries? queries,
    IWatcherSessionsQuery? watcherSessions = null,
    IWatcherBoardQuery? watcherBoard = null,
    IWatcherLeaderboardQuery? watcherLeaderboard = null,
    IWatcherDisputeQuery? watcherDisputes = null,
    System.Func<string, System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<SearchResult>>>? searchProvider = null)
{
    /// <summary>Surface kinds this factory can build. An unknown kind still gets an honest pane.</summary>
    public static IReadOnlyList<string> KnownKinds { get; } = ["view", "inspector", "terminal", "canvas", "contexts", "joins", "sessions", "board", "leaderboard", "prompt", "classdiagram", "sequence", "search", "codeviewer", "diagnostics"];

    public FrameworkElement Create(Surface surface)
    {
        var content = surface.Kind switch
        {
            "view" or "inspector" when queries is not null => EvidenceContent(surface),
            "view" or "inspector" => WorkspaceNeeded(surface),
            "terminal" => Terminal(surface),
            "canvas" => new CanvasSurface(surface.SurfaceId, surface.Title),
            "contexts" => new ContextMapSurface(surface.Title),
            "joins" => new JoinSurface(surface.Title),
            "sessions" => Sessions(surface),
            "board" => Board(surface),
            "leaderboard" => Leaderboard(surface),
            "prompt" => new PromptDraftSurface(surface.SurfaceId, surface.Title),
            "classdiagram" => new ClassDiagramSurface(surface.Title),
            "sequence" => new SequenceDiagramSurface(),
            "search" => new SearchSurface { Provider = searchProvider },
            "codeviewer" => new CodeViewerView(surface.Title),
            "diagnostics" => new DiagnosticsSurface(surface.Title),
            _ => Unavailable(surface),
        };

        // Every surface carries its title into the accessibility tree in its own right, not only via
        // its tab — a screen-reader user who moves focus into the pane must still know where they are.
        AutomationProperties.SetName(content, surface.Title);

        // The facelift frames each pane's content as a soft "island" card (rounded + bordered +
        // inset). This is a purely visual wrap — "if it changes how a pane looks, Design owns it"
        // (session-contracts §"Design owns") — and it is transparent to the render-invariant tests,
        // which read the content's text through the tree.
        //
        // The windowed kinds (canvas WebView2, terminal HwndHost) are returned UNWRAPPED: a rounded
        // Border cannot clip a child HWND to its corners anyway (airspace), and — load-bearing — the
        // shell finds the live canvas by `Adapter.ContentFor(id).OfType<CanvasSurface>()` to wire its
        // focus, filtering and re-centring, so a wrapper that hid the type would silently break those.
        return surface.Kind is "canvas" or "terminal"
            ? content
            : SurfaceChrome.WrapAsIsland(content);
    }

    private FrameworkElement EvidenceContent(Surface surface)
    {
        var pane = new EvidencePaneViewModel(queries!);

        // A TEMPLATE, NOT A DisplayMemberPath.
        //
        // `DisplayMemberPath` renders exactly ONE property, so this pane showed DisplayLabel and
        // silently dropped Evidence, NodeKind and Confidence — three fields the row computes and
        // nothing displayed. Search now matches attribute VALUES, so a row can come back because one
        // of its members matched; without the reason that is a correct hit which reads as a wrong
        // one, the same defect already fixed on the search surface.
        //
        // The accessible name is set per ITEM. It was on the ListBox, so `EvidenceRow.AccessibleName`
        // — written to carry exactly this reason — was a computed property nothing read, and a
        // screen reader got the same one property the eye did.
        var row = new DataTemplate(typeof(EvidenceRow));
        var line = new FrameworkElementFactory(typeof(TextBlock));
        line.SetBinding(TextBlock.TextProperty, new Binding(nameof(EvidenceRow.ListLine)));
        line.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        line.SetBinding(AutomationProperties.NameProperty, new Binding(nameof(EvidenceRow.AccessibleName)));
        row.VisualTree = line;

        var list = new ListBox
        {
            ItemTemplate = row,
            BorderThickness = new Thickness(0),
            Background = null,
        };
        AutomationProperties.SetName(list, $"{surface.Title} items");

        var status = new TextBlock
        {
            Text = pane.StatusMessage,
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        status.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(list);
        stack.Children.Add(status);

        // Started, not awaited — a factory that blocked on a pipe round trip would freeze the window
        // while a pane is being built. The pane shows its Loading state until the answer arrives.
        //
        // The controls MUST be updated when it does. An earlier revision bound `pane.Rows` and
        // `pane.StatusMessage` at construction and left it there: the load replaces `Rows` with a
        // new list and `Rows` is not observable, so the pane sat on "Loading evidence…" forever.
        // Every test passed — the pane view model was correct, and nothing asserted on what the
        // control showed. Found by running the application.
        _ = LoadInto(pane, list, status);

        return stack;
    }

    /// <summary>Loads the pane and pushes the result into its controls, on the UI thread.</summary>
    /// <remarks>
    /// <para><b>Failure is shown because the pane reports it, and this shows what the pane says.</b>
    /// An unreachable workspace becomes the pane's error state, and pushing that text into the
    /// control is what stops it presenting as merely slow.</para>
    ///
    /// <para>Marshalled explicitly because the continuation runs wherever the IPC round trip
    /// completed, and touching a WPF control from that thread throws.</para>
    /// </remarks>
    private static async Task LoadInto(EvidencePaneViewModel pane, ListBox list, TextBlock status)
    {
        try
        {
            await pane.LoadAsync();
        }
        catch (OperationCanceledException)
        {
            // The ONLY thing that escapes LoadAsync. The pane catches everything else itself and
            // degrades to an explicit error state with its own message, which the update below then
            // shows.
            //
            // An earlier revision also caught the general case here and wrote its own message. That
            // branch was unreachable — mutation proved it, by deleting it and failing nothing — and
            // a control that cannot fire reads as protection while providing none (DC-016).
            return;
        }

        await list.Dispatcher.InvokeAsync(() =>
        {
            list.ItemsSource = pane.Rows;
            status.Text = pane.StatusMessage;
        });
    }

    /// <summary>A live terminal: a real ConPTY session, drawn by the real renderer.</summary>
    /// <remarks>
    /// Replaces the Phase-1b placeholder. The surface owns the session's lifetime, so the factory
    /// hands one back and does not keep it — a pane that is closed disposes what it built.
    /// </remarks>
    private static FrameworkElement Terminal(Surface surface) =>
        // An "agent:<exe>" surface id carries which executable this pane runs, so the layout — which
        // is persisted — remembers it. Storing it anywhere else would restore an agent pane as a
        // shell after a restart.
        new TerminalSurface(surface.SurfaceId, surface.Title)
        {
            Executable = surface.SurfaceId.StartsWith("agent:", StringComparison.Ordinal)
                ? surface.SurfaceId["agent:".Length..].Split('#')[0]
                : null,
        };

    /// <summary>
    /// The Loomkeeper Sessions surface: observed sessions with honest liveness and Not Recorded for
    /// anything unproven. Its read model loads <b>synchronously</b> (a local store fold, no IPC), so -
    /// unlike the evidence pane - there is no async construction-time binding to strand it on
    /// "Loading…" (DC-011): the rows are present before the control is shown.
    /// </summary>
    private FrameworkElement Sessions(Surface surface)
    {
        var pane = new WatcherSessionsPaneViewModel(watcherSessions);
        pane.Load();

        var rowsPanel = new StackPanel();
        foreach (var row in pane.Rows)
        {
            rowsPanel.Children.Add(SessionRow(row));
        }

        AutomationProperties.SetName(rowsPanel, $"{surface.Title} sessions");

        var stack = new StackPanel { Margin = new Thickness(12) };

        if (pane.Rows.Count == 0)
        {
            // Honest empty/unavailable state — the status line below already says which.
        }
        else
        {
            stack.Children.Add(new ScrollViewer
            {
                Content = rowsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            });

            // A telemetry gap the whole list shares is stated ONCE here, not repeated per row (#15).
            var shared = SessionRowPresenter.SharedTelemetryNote(pane.Rows);
            if (shared is not null)
            {
                var note = new TextBlock
                {
                    Text = shared,
                    Margin = new Thickness(0, 10, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                };
                note.SetResourceReference(TextBlock.ForegroundProperty, "InferredBrush");
                stack.Children.Add(note);
            }
        }

        var status = new TextBlock
        {
            Text = pane.StatusMessage,
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        status.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        stack.Children.Add(status);
        return stack;
    }

    // One session as a legible two-line row: a colour+glyph liveness chip, a primary identity line,
    // and a muted metadata line beneath it (#15). Presentation strings/brush come from the pure,
    // headlessly-tested SessionRowPresenter.
    private static FrameworkElement SessionRow(WatcherSessionRow row)
    {
        var chip = new Border
        {
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 1, 6, 1),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 1, 10, 0),
        };
        var chipBrushKey = SessionRowPresenter.ChipBrushKey(row.Liveness);
        chip.SetResourceReference(Border.BorderBrushProperty, chipBrushKey);
        var chipText = new TextBlock { Text = SessionRowPresenter.ChipText(row.Liveness), FontSize = 11 };
        chipText.SetResourceReference(TextBlock.ForegroundProperty, chipBrushKey);
        chip.Child = chipText;

        var identity = new TextBlock
        {
            Text = SessionRowPresenter.Identity(row),
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        identity.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

        var details = new TextBlock
        {
            Text = SessionRowPresenter.Details(row),
            FontSize = 11,
            Margin = new Thickness(0, 1, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        details.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var textCol = new StackPanel();
        textCol.Children.Add(identity);
        textCol.Children.Add(details);

        var rowPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        rowPanel.Children.Add(chip);
        rowPanel.Children.Add(textCol);

        AutomationProperties.SetName(rowPanel, row.AccessibleName);
        return rowPanel;
    }

    /// <summary>
    /// The Loomkeeper Message Board surface (US-4): posts across repositories with quarantined
    /// untrusted content shown but never as instruction, injection flags visible, redactions as
    /// tombstones. Synchronous local-store fold, like <see cref="Sessions"/> - never strands on
    /// "Loading…" (DC-011).
    /// </summary>
    private FrameworkElement Board(Surface surface)
    {
        var pane = new WatcherBoardPaneViewModel(watcherBoard);
        pane.Load();
        return ListPane(surface, pane.Rows, nameof(WatcherBoardRow.DisplayLabel), pane.StatusMessage, "posts");
    }

    /// <summary>
    /// The Loomkeeper Leaderboard surface (US-14): facet cells per (task class, score schema) segment,
    /// a rank where comparable and "Not Comparable" with a reason where the cohort is too small or
    /// single-operator (US-10/US-16). Synchronous local-store fold.
    /// </summary>
    private FrameworkElement Leaderboard(Surface surface)
    {
        var pane = new WatcherLeaderboardPaneViewModel(watcherLeaderboard, watcherDisputes);
        pane.Load();
        return ListPane(surface, pane.Rows, nameof(WatcherLeaderboardRow.DisplayLabel), pane.StatusMessage, "cells");
    }

    /// <summary>
    /// The shared list-pane chrome for the honest read surfaces (sessions/board/leaderboard): a bound
    /// ListBox over dense one-line rows plus the evidence status line. One place, so a new read
    /// surface never re-derives the accessibility and status wiring.
    /// </summary>
    private static FrameworkElement ListPane(Surface surface, System.Collections.IEnumerable rows, string displayMember, string statusMessage, string itemNoun)
    {
        var list = new ListBox
        {
            DisplayMemberPath = displayMember,
            ItemsSource = rows,
            BorderThickness = new Thickness(0),
            Background = null,
        };
        AutomationProperties.SetName(list, $"{surface.Title} {itemNoun}");

        var status = new TextBlock
        {
            Text = statusMessage,
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        status.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(list);
        stack.Children.Add(status);
        return stack;
    }

    private static FrameworkElement Unavailable(Surface surface)
    {
        var text = new TextBlock
        {
            Text = $"“{surface.Title}” is not available in this build.",
            Margin = new Thickness(12),
            TextWrapping = TextWrapping.Wrap,
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return text;
    }

    // An evidence "view"/"inspector" surface (Explore, Domain, Provenance …) has nothing to read
    // until a workspace is open. Before, it fell through to Unavailable() and read "… is not
    // available in this build" — which points a user at a build/packaging defect for what is
    // actually the ordinary empty state of "no workspace open". This says the true thing, in the
    // same voice as the graph pane's "No workspace is open. Open one to see its graph." (UI-EMPTY-STATE).
    private static FrameworkElement WorkspaceNeeded(Surface surface)
    {
        var text = new TextBlock
        {
            Text = $"No workspace is open. Open one to see {surface.Title}.",
            Margin = new Thickness(12),
            TextWrapping = TextWrapping.Wrap,
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return text;
    }
}
