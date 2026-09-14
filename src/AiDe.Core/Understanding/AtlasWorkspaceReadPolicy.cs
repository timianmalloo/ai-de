namespace AiDe.Core.Understanding;

/// <summary>Authority comes from the opened Core workspace, never a display label or semantic-index membership.</summary>
internal sealed class AtlasWorkspaceReadPolicy
{
    private readonly WorkspaceCore _core;
    internal string WorkspaceId { get; }
    internal string RootPath { get; }
    internal string DataDirectory { get; }
    internal AtlasObjectIdentity RootIdentity { get; }
    internal string PolicyToken { get; } = "atlas-policy:" + Guid.NewGuid().ToString("N");

    internal AtlasWorkspaceReadPolicy(WorkspaceCore core, string workspaceId, string sourceRootPath, string dataDirectory)
    {
        ArgumentNullException.ThrowIfNull(core);
        _core = core;
        WorkspaceId = workspaceId;
        RootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceRootPath));
        DataDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory));
        if (!ConfigurationMatches())
            throw new AtlasReadException("Atlas.AdmissionRefused", "Workspace configuration does not authorize this reader.");
        var epoch = core.Store.CoreEpoch;
        using var initialization = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var observed = new AtlasSource().ObserveApprovedRootIdentity(RootPath,
            () => IsEpochCurrent(epoch), initialization.Token);
        if (observed.Completion is not AtlasCompletionState.Complete || observed.Identity is null)
            throw new AtlasReadException("Atlas.AdmissionRefused", "The workspace root cannot be verified.");
        RootIdentity = observed.Identity;
    }

    private bool ConfigurationMatches() => string.Equals(_core.WorkspaceId, WorkspaceId, StringComparison.Ordinal)
        && string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(_core.RootPath)), RootPath, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(_core.DataDirectory)), DataDirectory, StringComparison.OrdinalIgnoreCase)
        && Path.IsPathFullyQualified(RootPath) && !RootPath.StartsWith(@"\\", StringComparison.Ordinal);

    internal long Epoch => _core.Store.CoreEpoch;
    internal bool IsEpochCurrent(long epoch) => ConfigurationMatches() && _core.Store.CoreEpoch == epoch;

    internal bool AllowsContent(AtlasFileEntry file, AtlasMembershipSnapshot membership) =>
        file.Kind is AtlasDirectoryEntryKind.File && file.Availability is AtlasFileAvailability.Available
        && _core.AllowsAtlasContent(file.RelativePath)
        && membership.Entries.Any(entry => entry.Kind is AtlasMembershipEntryKind.RegularFile
            && string.Equals(entry.RelativePath.Replace('\\', '/'), file.RelativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
}
