using System.Collections.Immutable;

namespace AiDe.Core.Understanding;

public enum AtlasInventoryMembershipMode { NonGit, UnavailableMembership, KnownMembership }

/// <summary>Trusted composition supplies membership; this component never discovers Git state or runs hooks.</summary>
public sealed class AtlasInventoryPolicy
{
    private AtlasRootGrant? _grant;
    private ImmutableHashSet<string> _paths = ImmutableHashSet<string>.Empty;
    private AtlasInventoryPolicy(AtlasInventoryMembershipMode mode, string reason)
    {
        Mode = mode;
        Reason = AtlasIdentityCodec.RequiredToken(reason, nameof(reason));
    }

    public AtlasInventoryMembershipMode Mode { get; }
    public string Reason { get; }
    public bool IsComplete { get; private init; } = true;
    public static AtlasInventoryPolicy NonGit(string reason) => new(AtlasInventoryMembershipMode.NonGit, reason);
    public static AtlasInventoryPolicy UnavailableMembership(string reason) => new(AtlasInventoryMembershipMode.UnavailableMembership, reason);

    /// <summary>
    /// Immutable, ordinal path membership bound to the exact issued grant instance. Completeness
    /// describes the supplied membership snapshot, not physical coverage. Ancestors of authorized
    /// paths are metadata members too. Missing paths in a partial snapshot are not authorized.
    /// </summary>
    public static AtlasInventoryPolicy KnownMembership(AtlasRootGrant grant, IEnumerable<string> paths, bool complete, string reason)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentNullException.ThrowIfNull(paths);
        var members = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.Contains(':')
                || path.Split('/').Any(part => part is "" or "." or ".."))
                throw new ArgumentException("Membership paths must be canonical root-relative paths.", nameof(paths));
            var parent = path;
            while (true)
            {
                members.Add(parent);
                var slash = parent.LastIndexOf('/');
                if (slash < 0) break;
                parent = parent[..slash];
            }
        }
        return new AtlasInventoryPolicy(AtlasInventoryMembershipMode.KnownMembership, reason)
        {
            _grant = grant, _paths = members.ToImmutable(), IsComplete = complete
        };
    }

    internal bool Matches(AtlasRootGrant grant) => Mode is not AtlasInventoryMembershipMode.KnownMembership || ReferenceEquals(_grant, grant);
    internal bool Includes(string path) => Mode is AtlasInventoryMembershipMode.NonGit || _paths.Contains(path);
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
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.Mode is AtlasInventoryMembershipMode.UnavailableMembership || !policy.Matches(rootGrant))
        {
            var withheld = new AtlasBounds(limits.MaxEntries, limits.MaxEntries, 0, 0, null,
                AtlasDenominatorState.Unknown, policy.Reason, "membership");
            return new AtlasManifest("atlas-manifest:" + Guid.NewGuid().ToString("N"), rootGrant, "membership-not-observed",
                [], [], [], policy.Matches(rootGrant) ? AtlasCompletionState.Partial : AtlasCompletionState.Refused, withheld);
        }
        var observation = await enumerator.EnumerateAsync(rootGrant, limits, cancellationToken).ConfigureAwait(false);
        var files = observation.Entries.Where(entry => policy.Includes(entry.RelativePath)).Select(entry => ToFile(rootGrant, entry)).ToList();
        var completion = !policy.IsComplete && observation.Completion is AtlasCompletionState.Complete
            ? AtlasCompletionState.Partial
            : observation.Completion;
        var reason = !policy.IsComplete ? policy.Reason : observation.Bounds.OmissionReason;
        var bounds = new AtlasBounds(limits.MaxEntries, limits.MaxEntries, files.Count, 0,
            completion is AtlasCompletionState.Complete ? files.Count : null,
            completion is AtlasCompletionState.Complete ? AtlasDenominatorState.Known : AtlasDenominatorState.Unknown,
            completion is AtlasCompletionState.Complete ? null : reason ?? completion.ToString(), completion is AtlasCompletionState.Complete ? null : "files");
        return new AtlasManifest("atlas-manifest:" + observation.ObservationKey, rootGrant, observation.ObservationKey, files, [], [], completion, bounds);
    }

    private static AtlasFileEntry ToFile(AtlasRootGrant rootGrant, AtlasDirectoryEntry entry)
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
        var slash = entry.RelativePath.LastIndexOf('/');
        var parent = slash < 0 ? rootGrant.RootToken
            : AtlasIdentityCodec.ForFile(rootGrant.WorkspaceToken, rootGrant.RootToken, entry.RelativePath[..slash]);
        return new AtlasFileEntry(value, entry.RelativePath, parent, entry.Kind, classification, entry.ObjectIdentity, availability, entry.Reason);
    }
}
