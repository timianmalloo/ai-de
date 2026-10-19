namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The words for an attempt's route (Ruling 162): one derivation, called by the turn row's binding line and by the
/// New session sheet's engine line, so the two cannot disagree. The route picker (WL-3) will replace the source of
/// the fingerprint, not these words.
/// </summary>
public static class RouteWords
{
    /// <summary>The prefix <c>AttemptDispatch</c> puts on an engine's fingerprint when the binding has none.</summary>
    public const string AcpPrefix = "acp:";

    /// <summary>The fingerprint a terminal row carries (<c>StoreWriter</c>).</summary>
    public const string PtyFingerprint = "pty";

    /// <summary>The terminal surface's existing label (<c>SurfaceContentFactory</c>), reused, not a new word.</summary>
    public const string TerminalWord = "Terminal";

    /// <summary>The engine as the New session sheet shows it: the catalog id.</summary>
    public static string Engine(string engineId) => engineId;

    /// <summary>The route of a fingerprint; a shape this does not know renders verbatim, never guessed.</summary>
    public static string For(string fingerprint)
    {
        if (fingerprint == PtyFingerprint)
        {
            return TerminalWord;
        }

        return fingerprint.StartsWith(AcpPrefix, StringComparison.Ordinal) && fingerprint.Length > AcpPrefix.Length
            ? Engine(fingerprint[AcpPrefix.Length..])
            : fingerprint;
    }
}
