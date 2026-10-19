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

    // The plain form: the left-out part holds no surface with a title (no "zones" field; an empty
    // floating stack; a zone entry this build does not know, holding nothing), so nothing is named.
    [Theory]
    [InlineData("zone-layout-no-zones.json")]
    [InlineData("zone-layout-floating-no-surfaces.json")]
    [InlineData("zone-layout-unknown-zone-empty.json")]
    public void AMalformedSavedZoneLayout_Restores_WithAPartialRestoreCode_AndOneSentenceSayingPartWasLeftOut(string fixture)
    {
        var result = RestoreFrom(fixture, out var applied);

        Assert.True(applied, result.Announcement);
        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
        Assert.Equal(
            $"Restored your saved workbench arrangement. {LayoutPersistence.PartlyReadSentence}",
            result.Announcement);
        Assert.Equal("Part of your saved layout could not be read and was left out.", LayoutPersistence.PartlyReadSentence);
    }

    // T21 node 5 check (A), the named form (Owner, 2026-10-19 ~15:10Z): the dropped surfaces by their
    // own titles, in saved order, joined with ", ". Never an internal id.
    [Fact]
    public void ALeftOutZoneHoldingTitledSurfaces_IsNamedByTheirTitles_InSavedOrder_AndNeverByAnId()
    {
        var result = RestoreFrom("zone-layout-unknown-zone.json", out var applied);

        Assert.True(applied, result.Announcement);
        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
        Assert.Equal(
            "Restored your saved workbench arrangement. Part of your saved layout could not be read and was left out: Notebook 2, Graph.",
            result.Announcement);
        Assert.DoesNotContain("notebook-2", result.Announcement, StringComparison.Ordinal);
        Assert.DoesNotContain("Nowhere", result.Announcement, StringComparison.Ordinal);
        Assert.Equal("Part of your saved layout could not be read and was left out: ", LayoutPersistence.PartlyReadNamingPrefix);
    }

    private RestoreResult RestoreFrom(string fixture, out bool applied)
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
        applied = persistence.LastRestoreAppliedASavedArrangement;
        return result!;
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
