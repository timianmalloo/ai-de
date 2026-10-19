namespace AiDe.Core.Workbench.Records.Folds;

/// <summary>
/// Whether a human disposition removes an evidence gap on a mandatory obligation (ADR-0040 section 3).
/// </summary>
/// <remarks>
/// assume: DR-2 = A (no disposition covers a mandatory obligation). Confirm: the Owner's ruling on
/// req-01M5AD6E7JA61TVJGM32MHCQKP. If false: only <see cref="CoveringKinds"/> and its pinning test change.
/// </remarks>
public static class AcceptancePolicy
{
    /// <summary>The one table: the disposition kinds that cover a mandatory obligation. Empty by default.</summary>
    public static readonly IReadOnlySet<string> CoveringKinds = new HashSet<string>(StringComparer.Ordinal);

    public static bool Covers(string dispositionKind) => throw new NotImplementedException();
}
