namespace AiDe.Core.Workbench.Records.Folds;

/// <summary>
/// Whether a human disposition removes an evidence gap on a mandatory obligation (ADR-0040 section 3).
/// </summary>
/// <remarks>
/// Ruling 177: no disposition covers a mandatory obligation. Adding a covering kind later is a ruling,
/// not an edit; <see cref="CoveringKinds"/> and its pinning test change together.
/// </remarks>
public static class AcceptancePolicy
{
    /// <summary>The one table: the disposition kinds that cover a mandatory obligation. Empty by default.</summary>
    public static readonly IReadOnlySet<string> CoveringKinds = new HashSet<string>(StringComparer.Ordinal);

    public static bool Covers(string dispositionKind) => CoveringKinds.Contains(dispositionKind);
}
