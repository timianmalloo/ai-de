using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The console document's content (Ruling 89): one session's <see cref="ConsoleSurface"/> — the
/// session document's own <see cref="SessionDocumentSurface.Split"/>, the one instance — taken out
/// of the document's grid and hosted as a document of its own in the Center zone, kept current from
/// the same read model the thread renders (one derivation of the rows, <c>Coalesce</c>; no second
/// store, Ruling 81).
/// </summary>
/// <remarks>
/// <para><b>Why a host and not the split itself.</b> A WPF element has one parent: the split is
/// built into the session document's body grid (CV-5.2's design, where it opened beside the thread),
/// so docking it elsewhere means removing it from that grid first; it is released parentless
/// when the console document closes. The document does not know it was hosted — with the shell's verb wired
/// (<see cref="SessionDocumentSurface.ConsoleRequested"/>) its own <c>OpenSplit</c> never runs, so
/// the grid's column stays at zero and the split's absence from it is invisible to the document.</para>
/// <para><b>Refresh is the host's.</b> The document refreshes its split only while its own column is
/// open (<c>IsSplitOpen</c>), which is never while the split is here; the host subscribes to the
/// read model directly and shows every applied snapshot, marshalled to the UI thread, until it is
/// disposed — which the shell does when the console document or its session closes.</para>
/// </remarks>
public sealed class ConsoleDocumentHost : ContentControl, IDisposable
{
    /// <summary>The surface kind — <c>console</c>; the row is <see cref="SurfaceContentFactory.Kinds"/>'s.</summary>
    public const string Kind = "console";

    private readonly SessionDocumentSurface _document;
    private bool _disposed;

    /// <summary>The console surface id for a session: <c>console:&lt;sessionId&gt;</c>.</summary>
    public static string SurfaceIdFor(string sessionId) => Kind + ":" + sessionId;

    /// <summary>The session id a console surface id names, or null for any other id.</summary>
    public static string? SessionIdOf(string surfaceId) =>
        surfaceId.StartsWith(Kind + ":", StringComparison.Ordinal) ? surfaceId[(Kind.Length + 1)..] : null;

    /// <summary>The document's caption: <i>Console — &lt;session name&gt;</i>.</summary>
    public static string CaptionFor(string sessionName) => "Console — " + sessionName;

    /// <summary>The header toggle's accessible name once the shell hosts the Console as a document (its CV-5.2 name said "beside the thread").</summary>
    public const string ToggleName = "Console — open as a document in the Center";

    /// <summary>The header toggle's help once hosted: how it closes. (No chord named in operator copy — US-C10 b3; the View menu's row carries the gesture.)</summary>
    public const string ToggleHelp = "Opens the Console as a document in the Center; press again, or close its tab, to close it.";

    public ConsoleDocumentHost(SessionDocumentSurface document, int? at)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        var split = document.Split;
        if (split.Parent is Panel grid)
        {
            grid.Children.Remove(split);   // out of the document's body (its column is at zero either way)
        }

        split.Visibility = Visibility.Visible;
        Content = split;
        Focusable = false;
        AutomationProperties.SetName(this, CaptionFor(document.Model.Title));

        split.Show(document.ReadModel.Current.Turns, at);
        document.ReadModel.Changed += OnThreadChanged;
    }

    /// <summary>The hosted split — the document's own instance.</summary>
    public ConsoleSurface Split => _document.Split;

    /// <summary>Brings the caret to <paramref name="ordinal"/>'s heading (a turn's <i>Open the log</i>), or leaves it.</summary>
    public void ShowAt(int? ordinal) => Split.Show(_document.ReadModel.Current.Turns, ordinal);

    private void OnThreadChanged(ThreadSnapshot snapshot)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnThreadChanged(snapshot));
            return;
        }

        if (_disposed)
        {
            return;
        }

        Split.Show(snapshot.Turns);
    }

    /// <summary>
    /// Stops following the thread and releases the split — parentless and collapsed, as the
    /// document keeps it closed — so the next console document for this session can take it.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _document.ReadModel.Changed -= OnThreadChanged;
        Content = null;
        _document.Split.Visibility = Visibility.Collapsed;
    }
}
