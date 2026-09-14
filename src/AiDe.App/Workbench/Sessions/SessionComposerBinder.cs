using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// Wires a session document's composer to the run it will start — <b>the only caller of
/// <see cref="ComposerSurface.Configure"/> in the product</b>, on every path that opens a session
/// document: <c>File → New Session</c>, a reopen from Recent sessions, and the documents a saved
/// arrangement restores at workspace-open.
/// </summary>
/// <remarks>
/// <para><b>Moved out of <c>MainWindow</c>, not duplicated (INV-0009 Phase 2, DC-084).</b> The
/// binding used to live in the window's New Session callback and nowhere else, so a reopened
/// session was shown with a composer nothing had configured. The construction site is still one:
/// <c>TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate</c> scans this file for exactly
/// one send context, one attach gate and one <c>Configure</c>, and every other file in the App for
/// none.</para>
///
/// <para><b>One binding feeds both consumers.</b> The <see cref="ComposerSendContext"/> and the
/// <see cref="AttachmentGate"/>'s provider name and account label all come from the single
/// <see cref="LaneBinding"/> resolved below. There is deliberately no second source: the sentence
/// the operator affirms before a byte of an outside-workspace file is read names the account the
/// run will bill, and two derivations of that pair is the shape that lets the affirmation describe
/// a different account from the one that gets charged (DM7).</para>
///
/// <para><b>Every refusal names a field, is visible, and is in the log.</b> No workspace, no
/// provider file, an ambiguous engine and an unconfigured provider each land on the composer's own
/// status line, in the announcement, and on a <c>session-document.refused</c> line. Nothing is
/// defaulted on the way past: a run-side value invented here ranks the episode in the wrong
/// standings cohort and is indistinguishable from a chosen one afterwards (DC-110).</para>
///
/// <para><b>The draft opens as a goal block, not free-form.</b> A governed run requires the six
/// §14.3 fields — no block, no spawn (R2) — and a free-form draft validates with none of them, so
/// the shape is set here rather than discovered at the run host.</para>
/// </remarks>
internal static class SessionComposerBinder
{
    /// <summary>Binds the composer of <paramref name="config"/>'s open document, and returns what to announce.</summary>
    /// <param name="shell">The shell holding the document.</param>
    /// <param name="config">
    /// The session whose document is open — its <see cref="SessionConfig.DefaultAccount"/> is what
    /// the composer binds (Ruling 105); a session with exactly one account and no default binds that
    /// one; anything else is refused by name, never resolved by reading order.
    /// </param>
    /// <param name="taskClass">The run's task class, or null when the session has none on record (a reopen).</param>
    /// <param name="repositoryRoot">The window's open workspace root, or null when it has none.</param>
    /// <param name="dataDirectory">The window's workspace data directory, or null when it has none.</param>
    /// <param name="providers">The provider file as last read, or null when there is none.</param>
    /// <param name="affirmation">Who asks the operator about an outside-workspace attachment.</param>
    internal static string Bind(
        WorkbenchShell shell,
        SessionConfig config,
        string? taskClass,
        string? repositoryRoot,
        string? dataDirectory,
        ProviderConfiguration? providers,
        IAttachmentAffirmation affirmation)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(affirmation);

        if (OnScreen(shell, config) is not { } composer)
        {
            return NotOnScreen;
        }

        // ONCE. A reopen of a session whose document the workspace-open restore already revived
        // reaches here a second time, and a second Configure after the page mounted re-mints the
        // fields and pushes a second host.init over whatever the operator typed.
        if (composer.IsConfigured)
        {
            return "Composer already bound.";
        }

        if (repositoryRoot is null || dataDirectory is null)
        {
            return Refuse(
                config, composer, "repositoryRoot",
                "this window has no open workspace, so a run has no checkout to cut a worktree from");
        }

        if (providers is null)
        {
            return Refuse(
                config, composer, "providers",
                $"there is no provider file at {ProviderConfiguration.DefaultPath}, "
                + "so no run binding exists. The session is open; a governed run needs one. "
                + ConfigureAction);
        }

        // THE SESSION'S ACCOUNT (Ruling 105): the default, or the one account a session with no
        // default carries. Two accounts and no default is "choose one", never the first.
        var account = config.DefaultAccount ?? (config.Accounts.Count == 1 ? config.Accounts[0] : null);
        if (account is null)
        {
            return Refuse(
                config, composer, "accountLabel",
                config.Accounts.Count == 0
                    ? "this session has no account, so no run binding exists. The session is open; "
                      + "add one in Session settings, or " + ConfigureAction
                    : $"this session has no default account — choose one in Session settings among "
                      + string.Join(", ", config.Accounts.Select(a => a.ToString())));
        }

        var binding = providers.Bind(account.Provider, account.Label, out var refusal);
        if (binding is null)
        {
            return Refuse(config, composer, refusal!.Field, refusal.Message);
        }

        composer.Draft.SwitchTo(ComposerShape.GoalBlock);

        composer.Configure(
            config,
            new ComposerSendContext(
                RepositoryRoot: repositoryRoot,
                DataDirectory: dataDirectory,
                AdapterInstallRoot: providers.AdapterInstallRoot,
                EngineId: binding.EngineId,
                Model: binding.Model,
                AccountLabel: binding.Account.Label,
                TaskClass: taskClass ?? config.DefaultTaskClass,   // the session's default_task_class (Ruling 72; ADR-0033 rule 4) — never null, never a second literal
                ProofPackArtifacts: [],
                Providers: providers.Registry.Rows),
            ComposerFields.GoalBlock(),
            new AttachmentGate(
                repositoryRoot,
                new AttachmentFileReader(),
                affirmation,

                // THE SAME BINDING, not a second lookup. These two arguments are the sentence the
                // operator reads before any outside-workspace byte is read.
                binding.Provider.ProviderId,
                binding.Account.Label));

        WorkbenchDiagnostics.SessionDocumentBound(config.SessionId, composer.SurfaceId, repositoryRoot);
        return $"Composer bound to {binding.EngineId} · {binding.Model} · {binding.Account.Label}.";
    }

    /// <summary>
    /// Leaves the composer of <paramref name="config"/>'s open document unbound with a named
    /// refusal the window established before binding could start — a malformed provider file on a
    /// reopen or a restore — and returns what to announce.
    /// </summary>
    /// <remarks>
    /// New Session refuses before its sheet opens (the sheet is not constructible without a
    /// registry); a session that already exists is still shown, with the refusal where the operator
    /// reads it, rather than not reopened at all.
    /// </remarks>
    internal static string Refuse(WorkbenchShell shell, SessionConfig config, string field, string message)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(config);

        return OnScreen(shell, config) is { } composer
            ? Refuse(config, composer, field, message)
            : NotOnScreen;
    }

    private const string NotOnScreen = "Its composer is not on screen, so nothing was wired to a run.";

    /// <summary>The action every no-backend refusal names (Ruling 104 (3), condition 3).</summary>
    internal const string ConfigureAction = "Configure a backend from New Session.";

    /// <summary>The composer of the session's open document, or null — recorded as a refusal — when no document is open for it.</summary>
    private static ComposerSurface? OnScreen(WorkbenchShell shell, SessionConfig config)
    {
        if (shell.SessionComposer(config.SessionId) is { } composer)
        {
            return composer;
        }

        WorkbenchDiagnostics.SessionDocumentRefused(
            config.SessionId, null, "composer", "no document is open for this session");
        return null;
    }

    /// <summary>Puts a field-level refusal on the composer, records it, and returns it for the announcement.</summary>
    private static string Refuse(SessionConfig config, ComposerSurface composer, string field, string message)
    {
        composer.ShowFieldRefusal(field, message);
        WorkbenchDiagnostics.SessionDocumentRefused(config.SessionId, composer.SurfaceId, field, message);
        return $"The composer has no run binding — {field}: {message}.";
    }
}
