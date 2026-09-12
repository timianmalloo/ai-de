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
    IWatcherLedgerQuery? watcherLedger = null,
    System.Func<string, System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<SearchResult>>>? searchProvider = null,
    // Appended rather than inserted. A new optional parameter in the middle silently re-points every
    // POSITIONAL call site at the wrong argument — the build caught it here, but a same-typed
    // neighbour would have compiled and mis-wired the pane instead.
    IWatcherDaydreamQuery? watcherDaydreams = null,

    // Appended for the same reason, one parameter later. Resolves the operator's name for a
    // terminal so a session row can lead with it — a presentation concern the SHELL owns, since
    // that is where TerminalCustomizationStore lives and where a rename actually happens.
    Func<string, string?>? terminalNameFor = null,

    // Appended for the same reason as its two neighbours above. Resolves the live session document
    // for a `session-document` surface; null in a build (or a test) with no session open, which the
    // pane says plainly rather than rendering an empty document.
    Func<Surface, Sessions.SessionDocumentSurface?>? sessionDocumentFor = null)
{
    /// <summary>How many surfaces of a kind a host holds at once — what §A7's "Instances" column says.</summary>
    public enum Instances
    {
        /// <summary>At most one: its derived entry is "Show &lt;Title&gt;" (focus if open, else open).</summary>
        One,

        /// <summary>Any number: its derived entry is "New &lt;Title&gt;".</summary>
        Many,
    }

    /// <summary>
    /// How a kind reaches the menu and the palette (Addendum C §B3 rule 3): its opener is either
    /// DERIVED from this row ("New/Show &lt;Title&gt;" under the named menu) or it is an existing
    /// catalog entry verb, in which case no entry is derived and the verb is the only door.
    /// </summary>
    /// <remarks>
    /// Stated on every row rather than inferred from an absence, so an unreachable kind cannot be
    /// created by omission (US-C3 b5): a row must say which it is, and the build test asserts that a
    /// named verb exists in the catalog and is offered in every perspective.
    /// </remarks>
    public abstract record SurfaceEntry
    {
        private SurfaceEntry() { }

        /// <summary>The opener is derived from the row and placed under <paramref name="Menu"/> (<c>_View</c>, or <c>_Prompt</c> for the prompt kind).</summary>
        public sealed record Derived(string Menu) : SurfaceEntry;

        /// <summary>The kind's only door is the catalog entry verb <paramref name="CommandId"/> (<c>terminal.new</c>, <c>session.new</c>); nothing is derived.</summary>
        public sealed record Verb(string CommandId) : SurfaceEntry;
    }

    /// <summary>
    /// One surface kind, as a row of data: what it answers to, how it is built, which perspectives
    /// admit it, and how the menu names it.
    /// </summary>
    /// <param name="Kind">The <see cref="Surface.Kind"/> this row answers to.</param>
    /// <param name="Title">The noun the derived "New/Show &lt;Title&gt;" entry and a newly opened surface's tab carry.</param>
    /// <param name="Summary">What the surface shows — the derived entry's hint, as the palette speaks it.</param>
    /// <param name="Build">Builds the content for one surface of this kind.</param>
    /// <param name="Perspectives">
    /// <b>The allow-list column (Ruling 52c; ADR-0030 rule 2).</b> The perspectives that admit this
    /// kind into their body — an explicit, non-empty set on every row, no default: an empty set
    /// fails the build test, so an unreachable kind cannot be created by omission (US-C3 b5). The
    /// membership is §A7's table as ruled (Rulings 59–61). Explore admits no docked kind — its body
    /// is not a host — so no row names it.
    /// </param>
    /// <param name="Instances">One or many per host — "Show" or "New" (§A7).</param>
    /// <param name="Entry">Derived opener, or the catalog entry verb that is this kind's only door.</param>
    /// <param name="Windowed">
    /// True for a kind whose content owns a child HWND (canvas, terminal). A windowed kind is
    /// returned UNWRAPPED — see the note at the end of <see cref="Create"/>.
    /// </param>
    public sealed record SurfaceKind(
        string Kind,
        string Title,
        string Summary,
        Func<SurfaceContentFactory, Surface, FrameworkElement> Build,
        IReadOnlyList<Perspective> Perspectives,
        Instances Instances,
        SurfaceEntry Entry,
        bool Windowed = false);

    /// <summary>
    /// The surface kinds this factory builds — <b>a descriptor list, not a switch arm</b> (Ruling 22).
    /// </summary>
    /// <remarks>
    /// <para><b>Adding a kind is adding a row.</b> This was a <c>switch</c> expression beside a
    /// hand-maintained <see cref="KnownKinds"/> array, so a new surface meant editing two things
    /// that nothing checked against each other — and the array is load-bearing: the layout restore
    /// reads it to decide what it can rebuild, so a kind listed there and missing from the switch
    /// resurrected a pane that then rendered "not available in this build". One list now answers
    /// both questions, and <c>SurfaceContentTests</c> walks it.</para>
    ///
    /// <para><b>Row order is menu order.</b> The derived "New/Show" entries of a perspective's View
    /// menu follow this list (Addendum C §B3's expected table), so the Architecture kinds come
    /// first in their menu's order, then Coding's, with the one shared kind (<c>codeviewer</c>) where
    /// both tables put it — last.</para>
    /// </remarks>
    public static IReadOnlyList<SurfaceKind> Kinds { get; } =
    [
        // ── Architecture: the reading host (UC3) ──────────────────────────────────────────────

        new("canvas", "Graph",
            "A windowed graph canvas over the workspace's code, data and architecture nodes.",
            static (_, s) => new CanvasSurface(s.SurfaceId, s.Title),
            Perspectives: [PerspectiveSet.Architecture], Instances.One, new SurfaceEntry.Derived("_View"),
            Windowed: true),

        // ────────────────────────────────────────────────────────────────────────────────
        // FINDING, NOT A FIX. THESE TWO ROWS BUILD THE SAME THING, AND THE OBVIOUS REPAIR IS
        // THE WRONG ONE. Read this before deleting either.
        //
        // "view" and "inspector" both resolve to Evidence(s); the builder takes no discriminator
        // and neither does the view model, so Explore, Provenance and Domain issue the identical
        // FindAsync("") and render through the identical template. The three lists have been
        // measured byte-identical. The strings "explore", "provenance" and "domain" appear nowhere
        // in this assembly - they are captions in the default layout, and a caption is not a job.
        //
        // The operator asked for the duplicates to be removed: "maybe just explore is needed and we
        // get rid of domain and provenance". THAT IS BACKWARDS, and acting on it destroys
        // capability:
        //   - Provenance is specified as the DETAIL half of a master-detail screen
        //     (phase-1-walking-skeleton). EvidencePaneViewModel.SelectAsync builds exactly the four
        //     specified sections and has one caller, bound to nothing in this shell. The specified
        //     master-detail was split into two sibling TABS IN ONE STACK - which cannot be
        //     master-detail, since only one tab is visible - and then the selection wire was
        //     dropped, leaving two copies of the master.
        //   - Domain is specified in US-2 and that surface EXISTS, as kind "classdiagram", openable
        //     by command. The tab captioned Domain is wired to "view".
        //   - Explore is the one that is genuinely redundant: it duplicates the Explorer rail mode,
        //     which is flagged in session-contracts.md and still open.
        // They are not redundant by design. They are redundant by decay.
        //
        // WHY THIS NODE DID NOT REPAIR IT. Re-pointing Domain and moving Provenance to the empty
        // Right zone are changes to the DEFAULT LAYOUT and its migration chain, which is the
        // zone/tree machinery another session is repairing for the pane-swap defect (INV-0006) -
        // and a zone recommendation validated against a shell that mislabels zones has been
        // validated against the wrong thing. Restoring Provenance as a selection-bound inspector
        // additionally needs a selection channel BETWEEN two panes, which is a design decision
        // (which list drives which inspector?) and not a rendering change.
        //
        // THE CONTROL THAT IS OWED: a test that two surface kinds render different content. None
        // exists, and one written today would be red - correctly. It lands with the repair, not
        // before it, because a green test here would have to assert the duplicate.
        //
        // Ruling 61 homes the pair in Architecture (Left: Evidence, Right: Provenance); the
        // selection channel and the owed test are SH-3's (Addendum C US-C6).
        // ────────────────────────────────────────────────────────────────────────────────
        new("view", "Evidence",
            "The evidence list: every fact the workspace's daemon has indexed, searchable.",
            static (f, s) => f.Evidence(s),
            Perspectives: [PerspectiveSet.Architecture], Instances.One, new SurfaceEntry.Derived("_View")),

        new("inspector", "Provenance",
            "The selected evidence row's detail: where a fact came from and what cites it.",
            static (f, s) => f.Evidence(s),
            Perspectives: [PerspectiveSet.Architecture], Instances.One, new SurfaceEntry.Derived("_View")),

        new("classdiagram", "Class diagram",
            "The type hierarchy (classes and interfaces and their inheritance) of the open workspace.",
            static (_, s) => new ClassDiagramSurface(s.Title),
            Perspectives: [PerspectiveSet.Architecture], Instances.Many, new SurfaceEntry.Derived("_View")),

        new("sequence", "Sequence diagram",
            "Participant lifelines and the ordered messages exchanged between them.",
            static (_, _) => new SequenceDiagramSurface(),
            Perspectives: [PerspectiveSet.Architecture], Instances.Many, new SurfaceEntry.Derived("_View")),

        new("contexts", "Contexts",
            "The bounded contexts as boxes, with the traffic that crosses between them.",
            static (_, s) => new ContextMapSurface(s.Title),
            Perspectives: [PerspectiveSet.Architecture], Instances.One, new SurfaceEntry.Derived("_View")),

        // Admitted by Ruling 59 — it exists and renders; whether it renders real content against a
        // real workspace is SH-3's measurement, and decides only the DEFAULT layout, not the row.
        new("joins", "Joins",
            "Code, schema and infrastructure joins, each marked Verified or Inferred.",
            static (_, s) => new JoinSurface(s.Title),
            Perspectives: [PerspectiveSet.Architecture], Instances.One, new SurfaceEntry.Derived("_View")),

        // ── Coding: the agentic host (UC1) ────────────────────────────────────────────────────

        // Opened by the entry verb `terminal.new` (File, every perspective — US-C11), never by a
        // derived entry: two doors to one kind is the second list this arrangement exists to remove.
        new("terminal", "Terminal",
            "One live shell session.",
            static (_, s) => Terminal(s),
            Perspectives: [PerspectiveSet.Coding], Instances.Many, new SurfaceEntry.Verb("terminal.new"),
            Windowed: true),

        // The Loomkeeper kinds, homed in Coding (Ruling 60): they observe the terminal side of UC1.
        // "Terminal sessions", not "Sessions" — a session DOCUMENT is the product's object, and a
        // watcher pane one word away from it was resolved by the reader, not the caption (§R row 17).
        new("sessions", "Terminal sessions",
            "The Loomkeeper watcher's live and inactive terminal sessions, with their harness and state.",
            static (f, s) => f.Sessions(s),
            Perspectives: [PerspectiveSet.Coding], Instances.One, new SurfaceEntry.Derived("_View")),

        new("board", "Message board",
            "The Loomkeeper message board: what the observed sessions have said to each other.",
            static (f, s) => f.Board(s),
            Perspectives: [PerspectiveSet.Coding], Instances.One, new SurfaceEntry.Derived("_View")),

        new("leaderboard", "Leaderboard",
            "The Loomkeeper scoring leaderboard over the observed sessions' episodes.",
            static (f, s) => f.Leaderboard(s),
            Perspectives: [PerspectiveSet.Coding], Instances.One, new SurfaceEntry.Derived("_View")),

        new("ledger", "Ledger",
            "The append-only episode ledger: every scored episode with its evidence.",
            static (f, s) => f.Ledger(s),
            Perspectives: [PerspectiveSet.Coding], Instances.One, new SurfaceEntry.Derived("_View")),

        // Reachable from nowhere before the derived menu (no command, no default slot — Ruling 60);
        // its entry now exists by construction.
        new("daydreams", "Daydreams",
            "Observed patterns and candidate lessons the watcher has noticed across episodes.",
            static (f, s) => f.Daydreams(s),
            Perspectives: [PerspectiveSet.Coding], Instances.One, new SurfaceEntry.Derived("_View")),

        // Under the Prompt menu, beside "Dispatch prompt…" (§B3 rule 3's one exception besides terminal).
        new("prompt", "Prompt draft",
            "A staged prompt draft: compose a prompt and transfer it to a ready terminal session.",
            static (_, s) => new PromptDraftSurface(s.SurfaceId, s.Title),
            Perspectives: [PerspectiveSet.Coding], Instances.Many, new SurfaceEntry.Derived("_Prompt")),

        // A scaffold with no index wired (Ruling 59 keeps it out of Architecture); openable in Coding today.
        new("search", "Search",
            "A breadth search: one query over the whole workspace \u2014 types, members, files and graph nodes.",
            static (f, _) => f.SearchPane(),
            Perspectives: [PerspectiveSet.Coding], Instances.Many, new SurfaceEntry.Derived("_View")),

        // The one shared kind: "what did the agent change" in Coding; "View source" from a node in
        // Architecture, where it must not leave the reading host (Ruling 59; US-C3).
        new("codeviewer", "Code viewer",
            "A read-only source view with syntax highlighting.",
            static (_, s) => new CodeViewerView(s.Title),
            Perspectives: [PerspectiveSet.Coding, PerspectiveSet.Architecture], Instances.Many, new SurfaceEntry.Derived("_View")),

        new("diagnostics", "Diagnostics",
            "The last re-index's analysis coverage (what was not analysed, grouped by category) and the daemon state.",
            static (_, s) => new DiagnosticsSurface(s.Title),
            Perspectives: [PerspectiveSet.Coding], Instances.One, new SurfaceEntry.Derived("_View")),

        // The session document (R13 b3, R16). "session-document", never "session" (Ruling 18): the
        // "sessions" row above is the Loomkeeper watcher pane, and a kind one letter away from it
        // would be resolved by whichever row was read first, silently. Opened by the front door
        // `session.new` (File, every perspective), never by a derived entry.
        new(AiDe.App.Workbench.Sessions.SessionDocumentSurface.Kind, "Session",
            "One session: its composer and its canvas.",
            static (f, s) => f.SessionDocument(s),
            Perspectives: [PerspectiveSet.Coding], Instances.Many, new SurfaceEntry.Verb("session.new")),
    ];

    /// <summary>Surface kinds this factory can build. An unknown kind still gets an honest pane.</summary>
    public static IReadOnlyList<string> KnownKinds { get; } = [.. Kinds.Select(k => k.Kind)];

    public FrameworkElement Create(Surface surface)
    {
        var row = Kinds.FirstOrDefault(k => string.Equals(k.Kind, surface.Kind, StringComparison.Ordinal));
        var content = row is null ? Unavailable(surface) : row.Build(this, surface);

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
        return row is { Windowed: true }
            ? content
            : SurfaceChrome.WrapAsIsland(content);
    }

    /// <summary>
    /// The live session document for a <c>session-document</c> surface, or an honest empty state
    /// when no session is open.
    /// </summary>
    /// <remarks>
    /// The document's own lifetime belongs to the shell, not to this factory: a session outlives the
    /// pane it is docked in, and a factory that constructed one per render would build a second
    /// composer and a second console every time the layout re-rendered. So the shell resolves it and
    /// this hands back what it is given.
    /// </remarks>
    private FrameworkElement SessionDocument(Surface surface)
    {
        if (sessionDocumentFor?.Invoke(surface) is FrameworkElement document)
        {
            return document;
        }

        // The ordinary empty state, in the same voice as WorkspaceNeeded: this is "no session open",
        // not a build or packaging defect, and saying "not available in this build" would point the
        // reader at the wrong thing (UI-EMPTY-STATE).
        var text = new TextBlock
        {
            Text = "No session is open. Create one from File → New Session.",
            Margin = new Thickness(12),
            TextWrapping = TextWrapping.Wrap,
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return text;
    }

    /// <summary>
    /// An evidence <c>view</c>/<c>inspector</c> pane, or the "no workspace" empty state when there
    /// is nothing to read yet.
    /// </summary>
    /// <remarks>
    /// The gate lives here rather than in the descriptor row because a row's <c>Build</c> is static:
    /// the constructor's arguments are in scope for this method and not for a lambda, and pushing
    /// the check down here is what keeps every row a one-line call.
    /// </remarks>
    private FrameworkElement Evidence(Surface surface) =>
        queries is not null ? EvidenceContent(surface) : WorkspaceNeeded(surface);

    /// <summary>The breadth-search pane, wired to the shell's provider.</summary>
    private FrameworkElement SearchPane() => new SearchSurface { Provider = searchProvider };

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
        // PASSED IN, not set afterwards. As an object initializer this ran after the constructor,
        // and the constructor starts the session — so the session always launched with a null
        // executable and every agent pane became a plain shell.
        new TerminalSurface(
            surface.SurfaceId, surface.Title,
            executable: surface.SurfaceId.StartsWith("agent:", StringComparison.Ordinal)
                ? surface.SurfaceId["agent:".Length..].Split('#')[0]
                : null);

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

        // Lead with the LIVE sessions (Alive only); collapse the inactive history (Stale + Ended) out
        // of the way. The Sessions surface is a live-status list, but a long-running workspace piles up
        // stale/ended terminals that otherwise bury the ones collaborating now (UX-SESSIONS-GRAVEYARD).
        var (live, inactive) = SessionRowPresenter.Partition(pane.Rows);

        var stack = new StackPanel { Margin = new Thickness(12) };

        if (pane.Rows.Count == 0)
        {
            // Teaching empty state — leads to the first action instead of an empty pane (U9/DX9). Only
            // when observation is available; the status line below handles the not-available case.
            if (watcherSessions is not null)
            {
                var hint = new TextBlock
                {
                    Text = "No sessions yet. Open a Claude Code or GitHub Copilot session from the "
                        + "Terminal menu, and it appears here — live, with its harness and activity.",
                    TextWrapping = TextWrapping.Wrap,
                };
                hint.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
                stack.Children.Add(hint);
            }
        }
        else
        {
            if (live.Count > 0)
            {
                var liveRows = new StackPanel();
                foreach (var row in live)
                {
                    liveRows.Children.Add(SessionRow(row, terminalNameFor));
                }

                AutomationProperties.SetName(liveRows, $"{surface.Title} live sessions");
                stack.Children.Add(new ScrollViewer
                {
                    Content = liveRows,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                });
            }
            else
            {
                // Every session is inactive — say so plainly rather than showing nothing above the history.
                var none = new TextBlock { Text = "No live sessions right now.", TextWrapping = TextWrapping.Wrap };
                none.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
                stack.Children.Add(none);
            }

            // The inactive history (stale + ended) is collapsed behind its count — available on demand.
            if (inactive.Count > 0)
            {
                var endedRows = new StackPanel();
                foreach (var row in inactive)
                {
                    endedRows.Children.Add(SessionRow(row, terminalNameFor));
                }

                var expander = new Expander
                {
                    Header = SessionRowPresenter.InactiveHeader(inactive.Count),
                    IsExpanded = false,
                    Margin = new Thickness(0, 10, 0, 0),
                    Content = new ScrollViewer
                    {
                        Content = endedRows,
                        MaxHeight = 260,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    },
                };
                expander.SetResourceReference(Control.ForegroundProperty, "TextMutedBrush");
                AutomationProperties.SetName(expander, SessionRowPresenter.InactiveHeader(inactive.Count));
                stack.Children.Add(expander);
            }

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
    private static FrameworkElement SessionRow(WatcherSessionRow row, Func<string, string?>? nameFor)
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
            Text = SessionRowPresenter.Identity(
                row,
                // Resolved at render, never cached on the row: a rename must show up on the next
                // render rather than on the next registration, and registration happens once.
                row.TerminalId.Length == 0 ? null : nameFor?.Invoke(row.TerminalId)),
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
    /// <summary>
    /// The Loomkeeper Daydreams surface (US-9): observed patterns, candidate lessons and promoted
    /// learnings, with each candidate saying what is stopping it.
    /// </summary>
    /// <remarks>
    /// A read surface only. Promotion needs a human gate and an operator action, and this slice
    /// ships the reading half — a promote control that could be pressed before the write path exists
    /// would be a button that lies.
    /// </remarks>
    private FrameworkElement Daydreams(Surface surface)
    {
        var pane = new WatcherDaydreamPaneViewModel(watcherDaydreams);
        pane.Load();
        return ListPane(surface, pane.Rows, nameof(WatcherDaydreamRow.DisplayLabel), pane.StatusMessage, "patterns");
    }

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

    // The Ledger: the append-only record of every work episode, newest first — the third watcher read
    // beside Board and Leaderboard, over the same observation store (US: "the ledger viewable too").
    private FrameworkElement Ledger(Surface surface)
    {
        var episodes = watcherLedger?.GetEpisodes() ?? [];
        return ListPane(surface, LedgerRow.Rows(episodes), nameof(LedgerRow.DisplayLabel),
            LedgerRow.StatusFor(watcherLedger), "episodes");
    }

    /// <summary>
    /// The shared list-pane chrome for the honest read surfaces (sessions/board/leaderboard): a bound
    /// ListBox over dense one-line rows plus the evidence status line. One place, so a new read
    /// surface never re-derives the accessibility and status wiring.
    /// </summary>
    private static FrameworkElement ListPane(Surface surface, System.Collections.IEnumerable rows, string displayMember, string statusMessage, string itemNoun)
    {
        var rowList = rows.Cast<object>().ToList();

        // Empty state: a single centred, width-constrained message that reads as an intentional
        // "nothing here yet" with a focal point — not a stray muted line top-left in a vast pane
        // (U9/DX9, smoke video 2026-09-02). The status message already carries the teaching text.
        if (rowList.Count == 0)
        {
            var empty = new TextBlock
            {
                Text = statusMessage,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 380,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24),
            };
            empty.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

            var host = new Grid { MinHeight = 120 };
            host.Children.Add(empty);
            AutomationProperties.SetName(host, $"{surface.Title} — {statusMessage}");
            return host;
        }

        var list = new ListBox
        {
            DisplayMemberPath = displayMember,
            ItemsSource = rowList,
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
