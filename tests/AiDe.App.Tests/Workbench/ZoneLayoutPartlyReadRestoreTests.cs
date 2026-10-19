using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>T21 node 4 check 1, the report line.</b> The two saved-file shapes that crashed the zone reader
/// (no <c>"zones"</c> field; a floating stack with no <c>"surfaces"</c> field) restore through
/// <see cref="LayoutPersistence.Restore"/>, the shell's real restore, with a partial-restore code and
/// one sentence that says part of the layout was left out.
/// </summary>
public sealed class ZoneLayoutPartlyReadRestoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-partly-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("zone-layout-no-zones.json")]
    [InlineData("zone-layout-floating-no-surfaces.json")]
    public void AMalformedSavedZoneLayout_Restores_WithAPartialRestoreCode_AndOneSentenceSayingPartWasLeftOut(string fixture)
    {
        Directory.CreateDirectory(_dir);
        var layoutPath = Path.Combine(_dir, "layout.json");
        var host = new ZoneBackedLayoutService();
        var ids = host.Zones.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
        File.Copy(
            Path.Combine(RepoRoot(), "tests", "AiDe.Core.Tests", "fixtures", fixture),
            LayoutPersistence.SlotPathFor(layoutPath, host.Admission.Perspective));

        using var persistence = new LayoutPersistence(host, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds);
        RestoreResult? result = null;
        var thrown = Record.Exception(() => result = persistence.Restore());

        Assert.Null(thrown);
        Assert.True(persistence.LastRestoreAppliedASavedArrangement, result!.Announcement);
        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
        Assert.Equal(
            $"Restored your saved workbench arrangement. {LayoutPersistence.PartlyReadSentence}",
            result.Announcement);
        Assert.Equal("Part of your saved layout could not be read and was left out.", LayoutPersistence.PartlyReadSentence);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) { Directory.Delete(_dir, recursive: true); } }
        catch (IOException) { }
    }
}
