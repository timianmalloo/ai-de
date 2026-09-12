namespace AiDe.App.Workbench.Sessions;

/// <summary>Which way focus leaves the feed on Ctrl+Home / Ctrl+End (DS-1 P2, P4).</summary>
public enum FocusLeave
{
    /// <summary>Ctrl+Home: the session header.</summary>
    ToHeader,

    /// <summary>Ctrl+End: the editor.</summary>
    ToEditor,
}

/// <summary>
/// The pure half of the feed's keyboard model (SC8; DS-1 P2): what one key press means, decided
/// without a window so <c>K1a</c> can walk the whole <c>Key × ModifierKeys × bool</c> domain.
/// </summary>
public abstract record FeedKeyDecision
{
    /// <summary>Not the feed's key: the platform, or an inner control that owns it, handles it.</summary>
    public sealed record None : FeedKeyDecision;

    /// <summary>Move the caret by <paramref name="Delta"/> items (PageDown +1, PageUp −1), select, scroll into view, focus the container.</summary>
    public sealed record MoveBy(int Delta) : FeedKeyDecision;

    /// <summary>Move the caret to the first (Home) or last (End) item — owned, index-based; the platform's End lands short (spike Q5c).</summary>
    public sealed record MoveTo(bool First) : FeedKeyDecision;

    /// <summary>Scroll the viewport by <paramref name="Lines"/> text lines (Down +3, Up −3); the caret stays — a tall turn's middle is reachable by keyboard.</summary>
    public sealed record Scroll(int Lines) : FeedKeyDecision;

    /// <summary>Raise a leave request the document routes (Ctrl+End → the editor, Ctrl+Home → the header).</summary>
    public sealed record Leave(FocusLeave To) : FeedKeyDecision;
}
