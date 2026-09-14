using AiDe.App.Workbench.Composer;
using AiDe.Core.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>What <i>Start a parallel session</i> did (Ruling 95), as the shell announces it.</summary>
/// <param name="Created">The sibling session, or null when nothing was created.</param>
/// <param name="Announcement">What to say. Never empty (DC-011).</param>
/// <param name="Refusal">Why the parent's draft was <b>not</b> consumed, or null when the sibling sent it.</param>
public sealed record ParallelSessionOutcome(SessionConfig? Created, string Announcement, string? Refusal);

/// <summary>
/// <i>Start a parallel session</i> (Ruling 95): a derived sibling of the session whose composer
/// offered it — the same workspace, backends and config, <c>origin = parallel:&lt;parent id&gt;</c>,
/// its name by Ruling 99's rule on the parent's name — opened docked beside the parent
/// (Ruling 83's Left zone, by the document kind's zone rule), its composer bound, and the parent's
/// draft sent as its first turn through the sibling's own gate (lease derivation and prepare apply
/// unchanged).
/// </summary>
/// <remarks>
/// <para><b>Measured before it was built (Ruling 95 condition 1).</b> Two sessions on one workspace
/// each completed a read-only turn concurrently against the live adapter — two engine processes
/// alive together, both answered (<c>docs/proof/send-while-running.md</c>). Had the run host
/// serialised them, this flow would not exist and the composer's action would carry that refusal.</para>
///
/// <para><b>One flow, injected edges, like <see cref="NewSessionFlow"/>.</b> The shell opens, the
/// window binds, the store creates; this type only sequences them, so the sequence is testable
/// without a window and the window has one place to wire it.</para>
///
/// <para><b>The name rule is Ruling 99's, not this type's.</b> <paramref name="uniqueName"/> is a
/// seam the window supplies — <c>SessionConfigStore.UniqueName(name, SessionConfigStore.ExistingNames(root))</c>,
/// the Sessions lane's one function, so the sheet's default name, an operator-typed duplicate and a
/// parallel session's name cannot drift; the default here, <c>"&lt;parent name&gt; (2)"</c>, is the
/// rule's first counter for a caller with no store to ask.</para>
/// </remarks>
public sealed class ParallelSessionFlow
{
    private readonly Func<SessionConfig, string> _open;
    private readonly Func<SessionConfig, string> _bind;
    private readonly Func<string, ComposerSurface?> _composerOf;
    private readonly Action<SessionConfig>? _remember;
    private readonly Func<string, string> _uniqueName;
    private readonly Func<CompileModeAvailability>? _availability;
    private readonly TimeProvider _time;

    /// <param name="open">Opens the sibling's document in the shell and returns what to announce (the shell's <c>OpenSessionDocument</c>).</param>
    /// <param name="bind">Binds the sibling's composer to its run and returns what to announce (the window's binder).</param>
    /// <param name="composerOf">The composer of an open session's document, by session id (the shell's <c>SessionComposer</c>).</param>
    /// <param name="remember">Records the sibling in Recent sessions; null when this build keeps none.</param>
    /// <param name="uniqueName">Ruling 99's rule over the parent's name (<c>SessionConfigStore.UniqueName</c> over the workspace's names); null for the first counter, <c>"&lt;name&gt; (2)"</c>.</param>
    /// <param name="availability">The compile-mode ladder's availability, for copying an agentic parent's mode; null leaves the sibling mechanical-only and says so.</param>
    /// <param name="time">Stamps the created session.</param>
    public ParallelSessionFlow(
        Func<SessionConfig, string> open,
        Func<SessionConfig, string> bind,
        Func<string, ComposerSurface?> composerOf,
        Action<SessionConfig>? remember = null,
        Func<string, string>? uniqueName = null,
        Func<CompileModeAvailability>? availability = null,
        TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(composerOf);

        _open = open;
        _bind = bind;
        _composerOf = composerOf;
        _remember = remember;
        _uniqueName = uniqueName ?? FirstCounter;
        _availability = availability;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>The origin every parallel session records: <c>parallel:&lt;parent session id&gt;</c>.</summary>
    public static string OriginOf(string parentSessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentSessionId);
        return "parallel:" + parentSessionId;
    }

    /// <summary>Ruling 99's first counter — the default for a caller that supplies no rule.</summary>
    public static string FirstCounter(string parentName) => parentName + " (2)";

    /// <summary>Runs the flow once for one parent and one draft.</summary>
    /// <param name="workspaceRoot">The workspace both sessions live in.</param>
    /// <param name="parent">The parent's config as it reads now.</param>
    /// <param name="request">The parent draft's words and attachments.</param>
    public ParallelSessionOutcome Start(string workspaceRoot, SessionConfig parent, ParallelSessionRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(request);

        var now = _time.GetUtcNow();
        var store = new SessionConfigStore(workspaceRoot, SessionId.New(now));
        var config = store.Create(
            _uniqueName(parent.Name),
            parent.WorkspaceId,
            parent.EnabledBackends,
            now,
            parent.FanOutCeiling,
            parent.BudgetCap,
            parent.DefaultTaskClass,
            OriginOf(parent.SessionId));

        var modeNote = string.Empty;
        if (!string.Equals(parent.CompileMode, config.CompileMode, StringComparison.Ordinal))
        {
            // The parent's rung is copied through the same gate the sheet uses, never written past it.
            if (_availability is null)
            {
                modeNote = $" Compile mode stays {config.CompileMode}: this build cannot evaluate the ladder for the copy.";
            }
            else
            {
                try
                {
                    config = store.SetCompileMode(parent.CompileMode, _availability(), now);
                }
                catch (AiDe.Core.PromptCompilation.EnvelopeStoreException error)
                {
                    modeNote = $" Compile mode stays {config.CompileMode}: {error.Message}.";
                }
            }
        }

        _remember?.Invoke(config);
        var opened = _open(config);
        var bound = _bind(config);

        var composer = _composerOf(config.SessionId);
        if (composer is null || !composer.IsConfigured)
        {
            // Created and open, but with no run binding the words cannot be sent — they stay in the
            // parent's editor, and the sibling's own composer carries the binder's refusal.
            return new ParallelSessionOutcome(
                config,
                $"{opened} {bound} The prompt stays in this editor.{modeNote}",
                composer is null ? "the parallel session's composer is not on screen" : bound);
        }

        composer.UseAsNextDraft(request.SourceText);
        foreach (var attachment in request.Attachments)
        {
            composer.Draft.Add(attachment);
        }

        var sent = composer.Send();
        if (sent is null && composer.PrepareState == PrepareState.Preparing)
        {
            // An agentic rung's first gesture prepares (Ruling 67): the words are in the sibling's
            // editor, its envelope opening; the operator confirms there. Consumed here all the same.
            return new ParallelSessionOutcome(
                config,
                $"Parallel session “{config.Name}” created beside “{parent.Name}”; its first turn is preparing — confirm it there. {bound}{modeNote}",
                null);
        }

        if (sent is null)
        {
            return new ParallelSessionOutcome(
                config,
                $"{opened} {bound} Its composer refused the prompt: {composer.Status}. The prompt stays in this editor.{modeNote}",
                composer.Status);
        }

        return new ParallelSessionOutcome(
            config,
            $"Parallel session “{config.Name}” created beside “{parent.Name}”; its first turn is sent. {bound}{modeNote}",
            null);
    }
}
