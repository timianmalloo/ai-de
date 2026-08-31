namespace AiDe.App.Workbench;

/// <summary>A terminal session a prompt draft can be transferred to (US-ED6): its id and display name.</summary>
public sealed record PromptTarget(string SessionId, string Title);

/// <summary>
/// The testable core of the prompt-draft surface (spec-editor-surfaces US-ED5–ED7). Holds the staged
/// draft body and the transfer rules — drafting never sends (US-ED5); transfer requires a ready target
/// and a non-empty body, names its target, and is one-way (US-ED6/ED7). The UI (`PromptDraftSurface`)
/// binds this; the dispatch and the live target list are injected so the rules are unit-testable
/// without a terminal.
/// </summary>
public sealed class PromptDraftViewModel
{
    private readonly Func<IReadOnlyList<PromptTarget>> _targets;
    private readonly Func<string, string, Task<bool>> _dispatch;
    private string _body = "";

    /// <param name="readyTargets">The LIVE set of ready sessions — read on demand, never cached, so
    /// a session becoming ready or going away is reflected (the workbench mutates under the draft).</param>
    /// <param name="dispatch">Delivers (targetSessionId, body) and reports whether it was accepted.</param>
    public PromptDraftViewModel(
        Func<IReadOnlyList<PromptTarget>> readyTargets,
        Func<string, string, Task<bool>> dispatch)
    {
        _targets = readyTargets ?? throw new ArgumentNullException(nameof(readyTargets));
        _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
    }

    /// <summary>The staged prompt text. Editing it never sends anything (US-ED5).</summary>
    public string Body
    {
        get => _body;
        set { _body = value ?? ""; Changed?.Invoke(this, EventArgs.Empty); }
    }

    /// <summary>The chosen target session id, or null to use the first ready target.</summary>
    public string? SelectedTargetId { get; set; }

    /// <summary>True once the draft has been transferred: the session owns it thereafter (US-ED7).</summary>
    public bool Transferred { get; private set; }

    /// <summary>The live ready targets (US-ED6).</summary>
    public IReadOnlyList<PromptTarget> Targets => _targets();

    /// <summary>Whether at least one session is ready to receive a transfer.</summary>
    public bool HasReadyTarget => Targets.Count > 0;

    /// <summary>Transfer is allowed only with a ready target, a non-empty body, and not already sent.</summary>
    public bool CanTransfer =>
        !Transferred && HasReadyTarget && !string.IsNullOrWhiteSpace(Body);

    /// <summary>Why transfer is blocked, for the disabled control's stated reason (never a silent no-op).</summary>
    public string BlockedReason =>
        Transferred ? "This draft has been transferred — the session owns it now."
        : !HasReadyTarget ? "Start or select a ready terminal session first — a draft can only transfer to a ready session."
        : string.IsNullOrWhiteSpace(Body) ? "Write a prompt before transferring."
        : "";

    /// <summary>Raised when the body or transfer state changes, so the UI can re-render.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Transfers the draft to the selected (or first) ready session. One-way: on success the draft is
    /// marked transferred and cannot be sent again. A failed dispatch leaves it un-transferred so the
    /// user can retry. Returns whether the transfer was accepted.
    /// </summary>
    public async Task<bool> TransferAsync()
    {
        if (!CanTransfer) { return false; }

        var targets = Targets;
        var targetId = SelectedTargetId is not null
            && targets.Any(t => string.Equals(t.SessionId, SelectedTargetId, StringComparison.Ordinal))
            ? SelectedTargetId
            : targets[0].SessionId;

        var accepted = await _dispatch(targetId, Body);
        if (accepted)
        {
            Transferred = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return accepted;
    }
}
