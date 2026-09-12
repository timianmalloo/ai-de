using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// What <c>File → New Session</c> did, as the shell announces it.
/// </summary>
/// <param name="Created">The created session, or null when nothing was created.</param>
/// <param name="Announcement">What to say. Never empty — a command that does its work in silence is
/// indistinguishable from a dead key (DC-011, SC 4.1.3).</param>
public sealed record NewSessionOutcome(NewSessionResult? Created, string Announcement);

/// <summary>
/// The <c>File → New Session</c> entry flow (R13 b1): workspace binding first, then the sheet.
/// </summary>
/// <remarks>
/// <para><b>A session cannot exist unbound.</b> With an active workspace the sheet opens
/// <i>pre-bound</i>; with none, the workspace chooser interposes, and a cancelled chooser ends the
/// flow having created nothing. The binding is not a validation step that could be skipped — the
/// sheet is not constructible without a workspace, so there is no path through this type that
/// reaches <c>SessionConfigStore.Create</c> without one.</para>
///
/// <para><b>The chosen workspace is opened, then the session is created in it (INV-0009 Phase 3,
/// DC-149).</b> The chooser used to hand its root to the session store and nowhere else: the
/// session was bound to workspace W while the window still had none open, and the composer's first
/// guard then refused — <i>repositoryRoot: this window has no open workspace</i> — over a workspace
/// the operator had just chosen in a dialog this flow put up. The chosen root now goes through the
/// window's ordinary open path first, and the sheet binds to the workspace the window reports once
/// that has happened, so the window and the session are bound to the same workspace before the
/// composer is. A workspace that does not open ends the flow with the open path's own reason.</para>
///
/// <para><b>Every exit announces.</b> Cancel at the chooser, a workspace that did not open, cancel
/// at the sheet, and a refusal inside the sheet each say what happened; only the create path says a
/// session exists.</para>
/// </remarks>
public sealed class NewSessionFlow
{
    private readonly Func<string?> _activeWorkspaceRoot;
    private readonly Func<string?>? _chooseWorkspace;
    private readonly Func<string, Task<string?>>? _openWorkspace;
    private readonly Func<NewSessionSheetViewModel, bool> _showSheet;
    private readonly Func<ProviderRegistry> _registry;
    private readonly Func<string, string> _workspaceId;
    private readonly Action<NewSessionResult>? _opened;
    private readonly TimeProvider _time;

    /// <param name="activeWorkspaceRoot">The workspace the shell has open, or null.</param>
    /// <param name="chooseWorkspace">
    /// Interposes the workspace chooser and returns the chosen root, or null when cancelled. Null
    /// means this build has no chooser, which the flow reports rather than working around.
    /// </param>
    /// <param name="openWorkspace">
    /// Opens the chosen root in the window through its ordinary open path and returns null, or the
    /// reason it did not open. Null means this build cannot open one, which the flow reports.
    /// </param>
    /// <param name="showSheet">
    /// Shows the sheet and returns whether the operator pressed Create. The sheet is handed in
    /// already bound, so the view never has to decide what a session belongs to.
    /// </param>
    /// <param name="registry">The provider registry, read fresh each time the sheet opens.</param>
    /// <param name="workspaceId">Maps a workspace root to the key the session config records.</param>
    /// <param name="opened">Called once a session exists — where the shell opens its document and
    /// records it in Recent sessions.</param>
    /// <param name="time">Stamps the default name and the created session.</param>
    public NewSessionFlow(
        Func<string?> activeWorkspaceRoot,
        Func<string?>? chooseWorkspace,
        Func<string, Task<string?>>? openWorkspace,
        Func<NewSessionSheetViewModel, bool> showSheet,
        Func<ProviderRegistry> registry,
        Func<string, string> workspaceId,
        Action<NewSessionResult>? opened = null,
        TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(activeWorkspaceRoot);
        ArgumentNullException.ThrowIfNull(showSheet);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(workspaceId);

        _activeWorkspaceRoot = activeWorkspaceRoot;
        _chooseWorkspace = chooseWorkspace;
        _openWorkspace = openWorkspace;
        _showSheet = showSheet;
        _registry = registry;
        _workspaceId = workspaceId;
        _opened = opened;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>The sheet the last <see cref="Start"/> built, or null when none was reached.</summary>
    public NewSessionSheetViewModel? LastSheet { get; private set; }

    /// <summary>Runs the flow once.</summary>
    public async Task<NewSessionOutcome> StartAsync()
    {
        LastSheet = null;

        var root = _activeWorkspaceRoot();

        if (string.IsNullOrWhiteSpace(root))
        {
            if (_chooseWorkspace is null || _openWorkspace is null)
            {
                return new NewSessionOutcome(
                    null, "A session must belong to a workspace, and this build cannot open one.");
            }

            var chosen = _chooseWorkspace();

            if (string.IsNullOrWhiteSpace(chosen))
            {
                // Cancel aborts cleanly: nothing was written, and the flow says so rather than
                // leaving the operator wondering whether a half-made session exists.
                return new NewSessionOutcome(null, "New session cancelled — no workspace was chosen.");
            }

            // OPENED, not merely recorded. The session is created in the workspace the window has
            // open, which is now the one the operator chose — and the sheet reads that root back
            // from the window rather than from the chooser, so the two cannot name different places.
            if (await _openWorkspace(chosen) is { } notOpened)
            {
                return new NewSessionOutcome(null, notOpened);
            }

            root = _activeWorkspaceRoot();

            if (string.IsNullOrWhiteSpace(root))
            {
                return new NewSessionOutcome(
                    null, $"New session cancelled — {chosen} opened, but this window reports no workspace.");
            }
        }

        var sheet = new NewSessionSheetViewModel(root, _workspaceId(root), _registry(), _time.GetUtcNow());
        LastSheet = sheet;

        if (!_showSheet(sheet))
        {
            return new NewSessionOutcome(null, "New session cancelled.");
        }

        if (!sheet.CanCreate)
        {
            return new NewSessionOutcome(null, sheet.BlockedReason!);
        }

        var created = sheet.Create(_time.GetUtcNow());
        _opened?.Invoke(created);

        return new NewSessionOutcome(
            created,
            $"Session “{created.Config.Name}” created in {System.IO.Path.GetFileName(root.TrimEnd('\\', '/'))}, "
            + $"task class {created.TaskClass}.");
    }
}
