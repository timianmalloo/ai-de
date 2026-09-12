using AiDe.Core.Workbench;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// What a <b>newly created</b> session does to the arrangement it lands in (Ruling 47).
/// </summary>
/// <remarks>
/// <para><b>Ratified, with a condition, and this type exists because of the condition.</b> The
/// operator asked for New Session to give <i>"a full window view like the explorer view icon
/// does"</i>. That cannot be delivered by pointing a button at the Explorer path: the Explorer icon
/// swaps <c>ContentControl.Content</c> over a two-value <see cref="ShellViewMode"/> and is
/// full-<b>body</b>, not full-window, and A4.4 and ADR-0017 say sessions are dock documents,
/// <i>"not a third shell mode"</i>. DESIGN.md already defines a <c>maximized</c> dock state —
/// <i>"fills the tree; siblings are temporarily minimized and remembered as such"</i> — so the felt
/// experience arrives with no concept and no ADR reopened.</para>
///
/// <para><b>It is a named unit rather than a private method because it had no oracle.</b> When the
/// Owner ratified it, nothing in <c>tests/</c> exercised create-maximizes or reopen-does-not; the
/// only maximize coverage was the command path. A product behaviour with no oracle is the one thing
/// this slice was otherwise strict about, so the behaviour moved to where a test can reach it:
/// <c>MainWindow</c> cannot be constructed in a test without starting the shell.</para>
///
/// <para><b>Reopening a session deliberately does not call this.</b> Maximizing is a response to
/// "I just made this and I want to work in it", not a property of session documents. Doing it on
/// every reopen would rearrange the workbench behind an operator who asked for a tab.</para>
/// </remarks>
internal static class NewSessionPlacement
{
    /// <summary>
    /// Maximizes the stack a newly created session's document landed in.
    /// </summary>
    /// <param name="service">The one layout service the shell, the view and the keyboard share.</param>
    /// <param name="sessionId">The session whose document was just opened.</param>
    /// <param name="announcement">What opening the document already had to say.</param>
    /// <returns>
    /// That sentence, plus what maximizing did — or the sentence unchanged when the document did not
    /// reach the layout at all. A refusal is announced, never swallowed: a maximize that silently
    /// did nothing is indistinguishable from a dead command (DC-011).
    /// </returns>
    internal static string GiveItTheWholeTree(
        ILayoutService service, string sessionId, string announcement)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var surfaceId = SessionDocumentSurface.SurfaceIdFor(sessionId);
        var stack = service.Current.FindStackOf(surfaceId);

        if (stack is null)
        {
            // The document did not reach the layout. Saying nothing extra is the honest outcome:
            // the open announcement already carries whatever did happen.
            return announcement;
        }

        var result = service.Apply(
            new LayoutOperation.SetStackState(stack.Id, StackState.Maximized));

        // In the log on the normal path, applied or refused (INV-0009 F5): the operator's log
        // showed this maximize only by its consequence — a reconcile three lines later reading
        // three collapsed zones. The active surface is not known here (the adapter is the window's),
        // so it is recorded as null rather than as the surface this maximize was for.
        WorkbenchDiagnostics.LayoutMutation(
            "maximize-stack", result.Applied ? "maximized" : "refused", surfaceId, null, service.Current, stack.Id);

        return $"{announcement} {result.Announcement}";
    }
}
