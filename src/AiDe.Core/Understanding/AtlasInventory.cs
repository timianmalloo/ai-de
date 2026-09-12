namespace AiDe.Core.Understanding;

public enum AtlasInventoryMembershipMode { NonGit, UnavailableMembership }

public sealed class AtlasInventoryPolicy
{
    private AtlasInventoryPolicy(AtlasInventoryMembershipMode mode, string reason)
    {
        Mode = mode;
        Reason = AtlasIdentityCodec.RequiredToken(reason, nameof(reason));
    }

    public AtlasInventoryMembershipMode Mode { get; }
    public string Reason { get; }
    public static AtlasInventoryPolicy NonGit(string reason) => new(AtlasInventoryMembershipMode.NonGit, reason);
    public static AtlasInventoryPolicy UnavailableMembership(string reason) => new(AtlasInventoryMembershipMode.UnavailableMembership, reason);
}

public sealed class AtlasInventory(IAtlasDirectoryEnumerator enumerator)
{
    public async Task<AtlasManifest> BuildManifestAsync(
        AtlasRootGrant rootGrant,
        EnumerationLimits limits,
        AtlasInventoryPolicy policy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootGrant);
        ArgumentNullException.ThrowIfNull(policy);
        var observation = await enumerator.EnumerateAsync(rootGrant, limits, cancellationToken).ConfigureAwait(false);
        var files = observation.Entries.Select(entry => ToFile(rootGrant, policy, entry)).ToList();
        var completion = policy.Mode is AtlasInventoryMembershipMode.UnavailableMembership && observation.Completion is AtlasCompletionState.Complete
            ? AtlasCompletionState.Partial
            : observation.Completion;
        var reason = policy.Mode is AtlasInventoryMembershipMode.UnavailableMembership ? policy.Reason : observation.Bounds.OmissionReason;
        var bounds = new AtlasBounds(limits.MaxEntries, limits.MaxEntries, files.Count, observation.Bounds.ReturnedBytes,
            completion is AtlasCompletionState.Complete ? files.Count : null,
            completion is AtlasCompletionState.Complete ? AtlasDenominatorState.Known : AtlasDenominatorState.Unknown,
            completion is AtlasCompletionState.Complete ? null : reason ?? completion.ToString(), completion is AtlasCompletionState.Complete ? null : "files");
        return new AtlasManifest("atlas-manifest:" + observation.ObservationKey, rootGrant, observation.ObservationKey, files, [], [], completion, bounds);
    }

    private static AtlasFileEntry ToFile(AtlasRootGrant rootGrant, AtlasInventoryPolicy policy, AtlasDirectoryEntry entry)
    {
        var classification = Path.GetExtension(entry.RelativePath).ToLowerInvariant() switch
        {
            ".cs" => AtlasFileClassification.CSharp,
            ".md" or ".txt" => AtlasFileClassification.Text,
            _ => AtlasFileClassification.Unknown,
        };
        var value = AtlasIdentityCodec.ForFile(rootGrant.WorkspaceToken, rootGrant.RootToken, entry.RelativePath);
        var availability = entry.Kind switch
        {
            AtlasDirectoryEntryKind.File or AtlasDirectoryEntryKind.Directory => AtlasFileAvailability.Available,
            AtlasDirectoryEntryKind.Unavailable => AtlasFileAvailability.Unavailable,
            AtlasDirectoryEntryKind.RejectedLink => AtlasFileAvailability.Refused,
            _ => AtlasFileAvailability.Unknown,
        };
        var parent = policy.Mode is AtlasInventoryMembershipMode.UnavailableMembership
            ? "membership-unavailable:" + policy.Reason
            : "non-git:" + policy.Reason;
        return new AtlasFileEntry(value, entry.RelativePath, parent, entry.Kind, classification, entry.ObjectIdentity, availability, entry.Reason);
    }
}
