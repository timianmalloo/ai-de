using System.Windows;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// What a canvas mode is handed when it is built: the session it belongs to and the merged stream
/// that session is accumulating.
/// </summary>
/// <param name="SessionId">The session document's id — a mode may key per-session state off it.</param>
/// <param name="Title">The session's display name.</param>
/// <param name="Stream">The merged Console stream. Shared, never copied: two readers of one stream.</param>
public sealed record CanvasModeContext(string SessionId, string Title, ConsoleStreamModel Stream);

/// <summary>
/// One canvas mode, as a row of data (Ruling 22).
/// </summary>
/// <param name="ModeId">Stable id — persisted in the session document envelope, so it never changes.</param>
/// <param name="Title">The tab caption.</param>
/// <param name="Create">Builds this mode's content. Called at most once per session document.</param>
public sealed record CanvasMode(string ModeId, string Title, Func<CanvasModeContext, FrameworkElement> Create);

/// <summary>
/// The canvas modes a session document offers — <b>a descriptor list, not a switch arm and not a
/// registry type</b> (Ruling 22).
/// </summary>
/// <remarks>
/// <para><b>Adding a mode is adding a row.</b> Ruling 22's operative words are "data-driven
/// registrations, never a hard-coded five-tab strip with placeholders": the target is the
/// placeholder, and the test is whether a later phase can append a mode <i>without editing the
/// factory</i>. <see cref="Register"/> is that append, and
/// <c>ACanvasModeIsAddedByAddingARowTests</c> proves it by registering a throwaway third mode,
/// showing the session document offers it with no edit to
/// <c>SurfaceContentFactory.cs</c> or to this file, and then removing it.</para>
///
/// <para><b>Two rows, and no abstraction over them.</b> An interface with two implementers is what
/// Rulings 7 and 15 already cut. The rows are values; the only shape is
/// <see cref="CanvasMode.Create"/>. Phase 3's Artifacts, Profiler and Board modes append rows.
/// <b>Upgrade trigger:</b> a third registrant from outside the <c>AiDe.App</c> assembly — at which
/// point the registration point moves onto a Core-owned catalog rather than growing an interface
/// here.</para>
///
/// <para><b>Console is first, and that is load-bearing.</b> R16 b1 requires Console to be the
/// default on session open; the document takes the first row rather than naming a constant twice.</para>
/// </remarks>
public static class CanvasModeCatalog
{
    /// <summary>The merged-stream mode. First, because R16 b1 makes it the default on session open.</summary>
    public const string ConsoleModeId = "console";

    /// <summary>The terminal mode — today's real terminal surface, unchanged.</summary>
    public const string TerminalModeId = "terminal";

    private static readonly List<CanvasMode> Registered = [];
    private static readonly Lock Gate = new();

    /// <summary>The two rows this phase ships. Console and Terminal only — no placeholder.</summary>
    public static IReadOnlyList<CanvasMode> BuiltIn { get; } =
    [
        new(ConsoleModeId, "Console", context => new ConsoleSurface(context.SessionId, context.Stream)),

        // The existing terminal surface, constructed exactly as SurfaceContentFactory constructs a
        // terminal pane. A session's terminal mode is a terminal, not a second implementation of one.
        new(TerminalModeId, "Terminal", context => new TerminalSurface(
            $"session-terminal:{context.SessionId}", $"{context.Title} — Terminal")),
    ];

    /// <summary>Every mode a session document offers: the built-in rows, then any appended.</summary>
    public static IReadOnlyList<CanvasMode> All
    {
        get
        {
            lock (Gate)
            {
                return [.. BuiltIn, .. Registered];
            }
        }
    }

    /// <summary>
    /// Appends a mode. Disposing the returned handle removes it again.
    /// </summary>
    /// <remarks>
    /// The handle exists so a registration has a lifetime. Without it the only way to prove "adding
    /// a mode is adding a row" would be to add a permanent row for the proof, which is a placeholder
    /// wearing a test's clothes.
    /// </remarks>
    /// <exception cref="ArgumentException">The id is already taken — two rows for one mode id would
    /// make which one renders depend on read order.</exception>
    public static IDisposable Register(CanvasMode mode)
    {
        ArgumentNullException.ThrowIfNull(mode);

        lock (Gate)
        {
            if (All.Any(m => string.Equals(m.ModeId, mode.ModeId, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    $"canvas mode '{mode.ModeId}' is already registered; one mode is one row, because "
                    + "two rows would make which one renders depend on which was read last",
                    nameof(mode));
            }

            Registered.Add(mode);
        }

        return new Registration(mode.ModeId);
    }

    /// <summary>Finds a mode by id, or null when nothing has registered it.</summary>
    public static CanvasMode? Find(string modeId) =>
        All.FirstOrDefault(m => string.Equals(m.ModeId, modeId, StringComparison.Ordinal));

    private sealed class Registration(string modeId) : IDisposable
    {
        private bool _removed;

        public void Dispose()
        {
            if (_removed)
            {
                return;
            }

            _removed = true;

            lock (Gate)
            {
                Registered.RemoveAll(m => string.Equals(m.ModeId, modeId, StringComparison.Ordinal));
            }
        }
    }
}
