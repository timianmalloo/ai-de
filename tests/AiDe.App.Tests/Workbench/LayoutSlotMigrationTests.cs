using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// Check D, the slot migration (ADR-0045 §7; the WL-S design's §4 and §7). Today's saved Factory slot names a session's
/// pane as a <c>session-document</c> placement. Restored, each one becomes the <c>notebook</c> of the same session, in
/// the same zone, at the same tab position, with its saved caption. What no perspective admits is still dropped with a
/// report. The first save after the migration keeps the original bytes once, as <c>&lt;slot&gt;.pre-notebook.bak</c>, and
/// the rewritten slot restores to the same arrangement with nothing left to migrate.
/// </summary>
/// <remarks>
/// Headless on the store, the host's service and <see cref="LayoutPersistence"/>, over the product's kind rows
/// (<see cref="DockHost.AdmissionFor(Perspective)"/>), as <c>ZoneLayoutSlotsTests</c> is.
/// </remarks>
public sealed class LayoutSlotMigrationTests : IDisposable
{
    private const string FirstSession = "20260911T125502Z-bd59855b";
    private const string SecondSession = "20260911T175821Z-1edfa710";

    // The migration's backup name is a file the operator can find, so the test pins the literal.
    private const string PreNotebookBackupSuffix = ".pre-notebook.bak";

    // Today's Factory slot (schema 1): the operator's two session panes in the Center, beside the ledger, with the second
    // one active; the watcher at the Left; one graph that only Understand admits; a terminal below.
    private const string TodaysSlot = """
        {
          "schemaVersion": 1,
          "appVersion": "0.4.0",
          "savedUtc": "2026-10-18T22:33:53+00:00",
          "layout": {
            "zones": [
              { "id": "Left", "content": { "kind": "stack", "tabs": [
                  { "surfaceId": "sessions", "kind": "sessions", "title": "Sessions" } ], "activeIndex": 0 },
                "extent": 0.22, "collapsed": false },
              { "id": "Right", "extent": 0.22, "collapsed": false },
              { "id": "Bottom", "content": { "kind": "stack", "tabs": [
                  { "surfaceId": "terminal-1", "kind": "terminal", "title": "Terminal — pwsh" } ], "activeIndex": 0 },
                "extent": 0.3, "collapsed": false },
              { "id": "Center", "content": { "kind": "stack", "tabs": [
                  { "surfaceId": "session-document:20260911T125502Z-bd59855b", "kind": "session-document", "title": "Binding" },
                  { "surfaceId": "ledger", "kind": "ledger", "title": "Ledger" },
                  { "surfaceId": "session-document:20260911T175821Z-1edfa710", "kind": "session-document", "title": "Second look" },
                  { "surfaceId": "graph", "kind": "canvas", "title": "Graph" } ], "activeIndex": 2 },
                "extent": 1.0, "collapsed": false }
            ],
            "floating": []
          }
        }
        """;

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-slot-migration-" + Guid.NewGuid().ToString("N"));

    private string LayoutPath => Path.Combine(_dir, "layout.json");

    private string Slot => LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Factory);

    private LayoutPersistence PersistenceFor(ZoneBackedLayoutService host) =>
        new(host, LayoutPath, new HashSet<string>(StringComparer.Ordinal), restorableKinds: SurfaceContentFactory.RestorableKinds);

    [Fact]
    public void TodaysSlot_RestoresEachSessionPaneAsItsNotebook_ReportsTheRest_KeepsAOneTimeBak_AndRoundTrips()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Slot, TodaysSlot);
        var original = File.ReadAllBytes(Slot);

        // Restore today's slot.
        var host = new ZoneBackedLayoutService(DockHost.AdmissionFor(PerspectiveSet.Factory));
        using (var persistence = PersistenceFor(host))
        {
            var result = persistence.Restore();

            var center = host.Zones.Zone(ZoneId.Center).Content as ZoneStack;
            Assert.True(center is not null, "the Center restored no stack");
            Assert.Equal(
                [$"notebook:{FirstSession}", "ledger", $"notebook:{SecondSession}"],
                center!.Tabs.Select(s => s.SurfaceId));
            Assert.Equal(["notebook", "ledger", "notebook"], center.Tabs.Select(s => s.Kind));
            Assert.Equal(["Binding", "Ledger", "Second look"], center.Tabs.Select(s => s.Title));
            Assert.Equal($"notebook:{SecondSession}", center.Active.SurfaceId);
            Assert.DoesNotContain(host.Zones.AllSurfaces(), s => s.Kind == "session-document");

            // The report: only what no mapping reaches is dropped, and it is said.
            Assert.Equal(["graph"], persistence.LastRestoreDropped.Select(d => d.Surface.SurfaceId));
            Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
            Assert.StartsWith("1 pane from your saved layout isn't available in Factory", result.Announcement, StringComparison.Ordinal);

            persistence.SaveNow();
            Assert.Null(persistence.LastSaveFailure);
        }

        // The one-time .bak holds today's bytes, and the slot now names notebooks only.
        var bak = Slot + PreNotebookBackupSuffix;
        Assert.True(File.Exists(bak), $"the first save after the migration wrote no {Path.GetFileName(bak)}");
        Assert.Equal(original, File.ReadAllBytes(bak));
        var rewritten = File.ReadAllText(Slot);
        Assert.DoesNotContain("session-document", rewritten, StringComparison.Ordinal);
        Assert.Contains($"notebook:{SecondSession}", rewritten, StringComparison.Ordinal);

        // Round-trip: the rewritten slot restores to the same arrangement, with nothing left to migrate or drop.
        var again = new ZoneBackedLayoutService(DockHost.AdmissionFor(PerspectiveSet.Factory));
        using (var persistence = PersistenceFor(again))
        {
            persistence.Restore();
            Assert.Equal(host.Zones.AllSurfaces().Select(s => s.SurfaceId), again.Zones.AllSurfaces().Select(s => s.SurfaceId));
            Assert.Equal($"notebook:{SecondSession}", ((ZoneStack)again.Zones.Zone(ZoneId.Center).Content!).Active.SurfaceId);
            Assert.Empty(persistence.LastRestoreDropped);
        }

        // One-time: today's file restored a second time does not replace the .bak already kept.
        File.WriteAllText(bak, "kept");
        File.WriteAllText(Slot, TodaysSlot);
        using (var persistence = PersistenceFor(new ZoneBackedLayoutService(DockHost.AdmissionFor(PerspectiveSet.Factory))))
        {
            persistence.Restore();
            persistence.SaveNow();
        }

        Assert.Equal("kept", File.ReadAllText(bak));
    }

    // Today's Factory slot with a prompt draft beside a session's pane (Ruling 173): the migration maps the pane and keeps the draft.
    private const string TodaysSlotWithAPromptDraft = """
        {
          "schemaVersion": 1,
          "appVersion": "0.4.0",
          "savedUtc": "2026-10-18T22:33:53+00:00",
          "layout": {
            "zones": [
              { "id": "Left", "extent": 0.22, "collapsed": false },
              { "id": "Right", "extent": 0.22, "collapsed": false },
              { "id": "Bottom", "extent": 0.3, "collapsed": false },
              { "id": "Center", "content": { "kind": "stack", "tabs": [
                  { "surfaceId": "session-document:20260911T125502Z-bd59855b", "kind": "session-document", "title": "Binding" },
                  { "surfaceId": "prompt-1", "kind": "prompt", "title": "Prompt draft" } ], "activeIndex": 1 },
                "extent": 1.0, "collapsed": false }
            ],
            "floating": []
          }
        }
        """;

    // The report's sentence for a kept prompt draft (Ruling 173 (3)); seam request to the Leader proposes it.
    private const string KeptPromptSentence = "Kept 1 prompt draft as it was: prompt drafts do not become notebooks.";

    [Fact]
    public void APromptDraft_IsKeptAsItWas_NotRetired_AndTheMigrationsReportSaysSo_Once()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Slot, TodaysSlotWithAPromptDraft);

        // Ruling 173 (1): prompt stays an admitted kind, so it has no retirement sentence.
        Assert.False(SurfaceContentFactory.RetiredKinds.ContainsKey("prompt"), "prompt is in RetiredKinds; Ruling 173 keeps it admitted");

        var host = new ZoneBackedLayoutService(DockHost.AdmissionFor(PerspectiveSet.Factory));
        using (var persistence = PersistenceFor(host))
        {
            var result = persistence.Restore();

            var center = (ZoneStack)host.Zones.Zone(ZoneId.Center).Content!;
            Assert.Equal([$"notebook:{FirstSession}", "prompt-1"], center.Tabs.Select(s => s.SurfaceId));
            Assert.Equal(["notebook", "prompt"], center.Tabs.Select(s => s.Kind));
            Assert.Equal(["Binding", "Prompt draft"], center.Tabs.Select(s => s.Title));
            Assert.Equal("prompt-1", center.Active.SurfaceId);
            Assert.Empty(persistence.LastRestoreDropped);
            Assert.Null(result.ErrorCode);

            // Ruling 173 (3): the migration's report says the draft was kept.
            Assert.True(
                result.Announcement.Contains(KeptPromptSentence, StringComparison.Ordinal),
                $"the restore report does not say the prompt draft was kept: \"{result.Announcement}\"");

            persistence.SaveNow();
        }

        // The rewritten slot migrates nothing, so its restore does not repeat the migration's sentence.
        var again = new ZoneBackedLayoutService(DockHost.AdmissionFor(PerspectiveSet.Factory));
        using (var persistence = PersistenceFor(again))
        {
            var result = persistence.Restore();
            Assert.Contains("prompt-1", again.Zones.AllSurfaces().Select(s => s.SurfaceId));
            Assert.DoesNotContain("prompt draft", result.Announcement, StringComparison.Ordinal);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
