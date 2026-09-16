using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// <b>ADR-0032's seven falsifying tests</b> — one zone-envelope file per host perspective, expand-
/// only; drop-with-report at restore; a one-time <c>.pre-perspectives.bak</c>; refusal reported and
/// backed up; a golden rollback round-trip against the frozen schema-1 DTO. Headless on the store,
/// the host's service and <see cref="LayoutPersistence"/>, over the PRODUCT's kind rows
/// (<see cref="DockHost.AdmissionFor(Perspective)"/>) and the product's zone-backed service (DC-135).
/// </summary>
public sealed class PerspectiveLayoutSlotTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-slots-" + Guid.NewGuid().ToString("N"));

    private string LayoutPath => Path.Combine(_dir, "layout.json");

    private string CodingSlot => LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Coding);

    private string ArchitectureSlot => LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Architecture);

    // What the shell hands LayoutPersistence (Ruling 94): the buildable kinds plus the retired ones, so a
    // saved envelope carrying a retired kind is dropped by the host WITH a report, never by the store in silence.
    private static IReadOnlySet<string> Kinds => SurfaceContentFactory.RestorableKinds;

    private static IReadOnlySet<string> NoSurfaces => new HashSet<string>(StringComparer.Ordinal);

    private static ZoneBackedLayoutService Host(Perspective row) => new(DockHost.AdmissionFor(row));

    private LayoutPersistence PersistenceFor(ZoneBackedLayoutService host) =>
        new(host, LayoutPath, NoSurfaces, restorableKinds: Kinds);

    /// <summary>
    /// A pre-Addendum-C <c>zones.json</c>: today's default, saved, plus one opened class diagram
    /// (ADR-0032 test 1's fixture) — the committed golden <c>Fixtures/pre-perspectives.zones.json</c>,
    /// written by the schema-1 store before this slice and never regenerated, so "byte-compatible at
    /// read" is asserted against bytes a pre-ADR build left, not against what the current store writes.
    /// </summary>
    private void WritePreAddendumCFile()
    {
        Directory.CreateDirectory(_dir);
        File.Copy(GoldenPath, CodingSlot);
    }

    private static string GoldenPath => Path.Combine(RepoRoot(), "tests", "AiDe.App.Tests", "Fixtures", "pre-perspectives.zones.json");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("AiDe.sln was not found above the test directory");
    }

    // Rule 1 — the slot's natural key is the file-name suffix, mapped by ONE function; Coding's
    // empty suffix is the grandfathering decision (a pre-C file IS the Coding slot); Explore has no file.
    [Fact]
    public void SlotPathFor_GrandfathersCodingToTodaysFile_AndGivesArchitectureASibling()
    {
        Assert.Equal(Path.Combine(_dir, "layout.zones.json"), CodingSlot);
        Assert.Equal(Path.Combine(_dir, "layout.architecture.zones.json"), ArchitectureSlot);
        Assert.Throws<ArgumentException>(() => LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Explore));
    }

    // Test 1 — the pre-C file's restore into Coding — lives in ZoneLayoutSlotsTests
    // (RestoringAPreCCodingEnvelope_DropsTheFiveLoomkeeperKinds_AndReportsNamingCoordination) since
    // Ruling 84 extended it: the four Loomkeeper kinds the fixture carries are dropped beside the
    // seven Architecture kinds, reported naming Coordination.

    // Test 2 — an envelope whose every surface is inadmissible restores the Coding default and says so.
    [Fact]
    public void AnEnvelopeWithNothingCodingAdmits_RestoresTheDefault_AndSaysSo()
    {
        var onlyArchitecture = WorkbenchLayout.Empty()
            .WithZone(new ZoneState(ZoneId.Center, new ZoneStack(
            [
                new Surface("graph", "canvas", "Graph"),
                new Surface("contexts", "contexts", "Contexts"),
            ]), 1.0, Collapsed: false));
        new ZoneLayoutStore(CodingSlot).Save(onlyArchitecture);
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        Assert.True(result.WasDefaulted);
        Assert.NotEmpty(host.Zones.AllSurfaces());
        Assert.Contains(host.Zones.AllSurfaces(), s => s.Kind == "terminal");
        Assert.Equal("Your saved layout had no panes Coding can show, so Coding opened with its default layout.", result.Announcement);
    }

    // Test 3 — two surfaces of a one-instance kind with distinct ids: the first is kept, the second
    // reported. (A duplicate id never reaches the service: the store refuses the file whole — test 7.)
    [Fact]
    public void TwoDiagnosticsPanesWithDistinctIds_KeepTheFirst_AndReportTheSecond()
    {
        var twice = WorkbenchLayout.Empty()
            .WithZone(new ZoneState(ZoneId.Bottom, new ZoneStack(
            [
                new Surface("diagnostics#1", "diagnostics", "Diagnostics"),
                new Surface("diagnostics#2", "diagnostics", "Diagnostics"),
                new Surface("terminal-1", "terminal", "Terminal"),
            ]), 0.3, Collapsed: false));
        new ZoneLayoutStore(CodingSlot).Save(twice);
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        Assert.Single(host.Zones.AllSurfaces(), s => s.Kind == "diagnostics");
        Assert.Equal("diagnostics#1", host.Zones.AllSurfaces().Single(s => s.Kind == "diagnostics").SurfaceId);
        var dropped = Assert.Single(persistence.LastRestoreDropped);
        Assert.Equal(DropReason.DuplicateOneInstance, dropped.Reason);
        Assert.Contains("1 pane from your saved layout isn't available in Coding", result.Announcement, StringComparison.Ordinal);
        Assert.Contains("a second Diagnostics", result.Announcement, StringComparison.Ordinal);
    }

    // Test 4 — host A's file and host B's file are written and restored independently.
    [Fact]
    public void EachHostSlot_IsWrittenAndRestoredIndependently()
    {
        var a1 = Host(PerspectiveSet.Coding);
        var b1 = Host(PerspectiveSet.Architecture);
        a1.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed));
        b1.Apply(new LayoutOperation.AddSurface(ZonesToTree.RightStackId, new Surface("seq#1", "sequence", "Sequence diagram")));

        using (var pa = PersistenceFor(a1)) { pa.SaveNow(); }
        using (var pb = PersistenceFor(b1)) { pb.SaveNow(); }

        Assert.True(File.Exists(CodingSlot));
        Assert.True(File.Exists(ArchitectureSlot));

        var a2 = Host(PerspectiveSet.Coding);
        var b2 = Host(PerspectiveSet.Architecture);
        using var pa2 = PersistenceFor(a2);
        using var pb2 = PersistenceFor(b2);
        pa2.Restore();
        pb2.Restore();

        Assert.True(a2.Zones.Zone(ZoneId.Left).Collapsed);
        Assert.Equal(ZoneId.Right, b2.Zones.FindZoneOf("seq#1"));
        Assert.DoesNotContain(a2.Zones.AllSurfaces(), s => s.Kind == "sequence");   // A never took B's surface
        Assert.False(b2.Zones.Zone(ZoneId.Left).Collapsed);                          // B's arrangement is its own
    }

    // Test 5 — dropped surfaces are not carried into another slot: after a Coding-era restore the
    // Architecture file is absent and Architecture opens with its default.
    [Fact]
    public void DroppedSurfaces_AreNotCarriedIntoTheArchitectureSlot()
    {
        WritePreAddendumCFile();
        var coding = Host(PerspectiveSet.Coding);
        using (var pa = PersistenceFor(coding)) { pa.Restore(); pa.SaveNow(); }

        Assert.False(File.Exists(ArchitectureSlot));

        var architecture = Host(PerspectiveSet.Architecture);
        using var pb = PersistenceFor(architecture);
        var result = pb.Restore();

        Assert.False(pb.LastRestoreAppliedASavedArrangement);
        Assert.Equal(architecture.DefaultLayout().Shape(), architecture.Zones.Shape());
        Assert.DoesNotContain(architecture.Zones.AllSurfaces(), s => s.SurfaceId == "classdiagram#a1b2c3");
    }

    // Test 6 — the rollback golden round-trip. The post-ADR Coding file deserialises with the frozen
    // schema-1 DTO into the same arrangement; the DTO constructors carry exactly their schema-1
    // parameters (a removed member breaks a rollback build; an added one is ignored).
    [Fact]
    public void ThePostAdrCodingFile_DeserialisesWithTheFrozenSchema1Dto()
    {
        WritePreAddendumCFile();
        var host = Host(PerspectiveSet.Coding);
        using (var persistence = PersistenceFor(host))
        {
            persistence.Restore();
            persistence.SaveNow();
        }

        var frozen = JsonSerializer.Deserialize<Frozen.ZoneEnvelope>(File.ReadAllText(CodingSlot), FrozenJson);

        Assert.NotNull(frozen);
        Assert.Equal(1, frozen!.SchemaVersion);

        // The whole arrangement, not the id set: per zone the tab ids in order, the active tab, the
        // extent and the collapsed flag; and the floating stacks.
        foreach (var id in Enum.GetValues<ZoneId>())
        {
            var live = host.Zones.Zone(id);
            var dto = Assert.Single(frozen.Layout.Zones, z => z.Id == id.ToString());
            Assert.Equal(live.Surfaces().Select(s => s.SurfaceId).ToList(), Tabs(dto.Content).Select(s => s.SurfaceId).ToList());
            Assert.Equal(live.Extent, dto.Extent);
            Assert.Equal(live.Collapsed, dto.Collapsed);
            if (live.Content is ZoneStack stack)
            {
                Assert.Equal(stack.ActiveIndex, dto.Content!.ActiveIndex);
            }
        }

        Assert.Equal(host.Zones.Floating.Count, frozen.Layout.Floating.Count);

        static IEnumerable<Frozen.ZoneSurfaceDto> Tabs(Frozen.ZoneContentDto? c) =>
            c is null ? [] : (c.Tabs ?? []).Concat((c.Children ?? []).SelectMany(Tabs));
    }

    [Theory]
    [InlineData(typeof(ZoneEnvelope), typeof(Frozen.ZoneEnvelope))]
    [InlineData(typeof(ZoneLayoutDto), typeof(Frozen.ZoneLayoutDto))]
    [InlineData(typeof(ZoneStateDto), typeof(Frozen.ZoneStateDto))]
    [InlineData(typeof(ZoneContentDto), typeof(Frozen.ZoneContentDto))]
    [InlineData(typeof(ZoneStackDto), typeof(Frozen.ZoneStackDto))]
    [InlineData(typeof(ZoneSurfaceDto), typeof(Frozen.ZoneSurfaceDto))]
    public void TheSchema1DtoConstructors_CarryExactlyTheirFrozenParameters(Type live, Type frozen)
    {
        // The C# shape AND the wire name: a renamed [JsonPropertyName] breaks a rollback reader as
        // surely as a removed member does.
        static string[] Parameters(Type t) =>
            t.GetConstructors().Single().GetParameters()
                .Select(p => $"{p.Name}:{p.ParameterType.Name}:{Wire(t, p.Name!)}")
                .ToArray();

        static string Wire(Type t, string parameter) =>
            t.GetProperty(parameter)?.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? "(none)";

        Assert.Equal(Parameters(frozen), Parameters(live));
    }

    // Test 6, the backup: the .pre-perspectives.bak exists after the first post-drop save and equals
    // the original bytes — written by the same call that replaced them, and only once.
    [Fact]
    public void TheFirstSaveAfterADroppingRestore_PreservesTheOriginalBytes_Once()
    {
        WritePreAddendumCFile();
        var original = File.ReadAllBytes(CodingSlot);
        var bak = CodingSlot + ".pre-perspectives.bak";
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);
        persistence.Restore();

        Assert.False(File.Exists(bak));           // a restore never writes
        persistence.SaveNow();

        Assert.Equal(original, File.ReadAllBytes(bak));
        Assert.NotEqual(original, File.ReadAllBytes(CodingSlot));

        // Once: a later save neither overwrites the backup nor makes a second one.
        host.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Collapsed));
        persistence.SaveNow();
        Assert.Equal(original, File.ReadAllBytes(bak));
    }

    // Test 6, fail-safe: with the backup target unwritable, SaveNow leaves the Coding file's bytes
    // unchanged and reports — it never writes over the only original.
    [Fact]
    public void WithTheBackupTargetUnwritable_SaveNowLeavesTheOriginalUnchanged_AndReports()
    {
        WritePreAddendumCFile();
        var original = File.ReadAllBytes(CodingSlot);
        Directory.CreateDirectory(CodingSlot + ".pre-perspectives.bak");   // a directory where the file must go
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);
        persistence.Restore();
        string? reported = null;
        persistence.SaveFailed += reason => reported = reason;

        persistence.SaveNow();

        Assert.Equal(original, File.ReadAllBytes(CodingSlot));
        Assert.NotNull(reported);
        Assert.Contains("pre-perspectives.bak", reported, StringComparison.Ordinal);
        Assert.Equal(reported, persistence.LastSaveFailure);

        // The backup is still owed: once the target is writable the next save preserves the
        // original and only then rewrites the slot.
        Directory.Delete(CodingSlot + ".pre-perspectives.bak");
        persistence.SaveNow();

        Assert.Null(persistence.LastSaveFailure);
        Assert.Equal(original, File.ReadAllBytes(CodingSlot + ".pre-perspectives.bak"));
        Assert.NotEqual(original, File.ReadAllBytes(CodingSlot));
    }

    // Test 7 — a newer schema, a corrupt file and a duplicate surface id are each refused WITH the
    // reason; and refuse → SaveNow → the refused bytes exist at <file>.bak before the slot is rewritten.
    [Theory]
    [InlineData("newer", LayoutErrorCodes.VersionUnsupported)]
    [InlineData("corrupt", LayoutErrorCodes.Unreadable)]
    [InlineData("duplicate", LayoutErrorCodes.Unreadable)]
    public void ARefusedFile_IsReportedWithItsReason_AndBackedUpBeforeTheSlotIsRewritten(string shape, string expectedCode)
    {
        Directory.CreateDirectory(_dir);
        var refused = shape switch
        {
            "newer" => "{\"schemaVersion\":2,\"appVersion\":\"9.9.9\",\"savedUtc\":\"2026-09-12T00:00:00+00:00\",\"layout\":{\"zones\":[],\"floating\":[]}}",
            "corrupt" => "{ this is not json",
            _ => DuplicateIdFile(),
        };
        File.WriteAllText(CodingSlot, refused);
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        Assert.Equal(expectedCode, result.ErrorCode);
        Assert.False(persistence.LastRestoreAppliedASavedArrangement);
        Assert.Contains("was not applied", result.Announcement, StringComparison.Ordinal);
        Assert.Equal(refused, File.ReadAllText(CodingSlot));     // a read never rewrites

        persistence.SaveNow();

        Assert.Equal(refused, File.ReadAllText(CodingSlot + ".bak"));
        Assert.NotEqual(refused, File.ReadAllText(CodingSlot));
    }

    // The D&P reviewer's blocker (ADR-0032 rule 4): with a `.bak` ALREADY present from an earlier
    // refusal, the refused bytes must still be preserved before the slot is rewritten — the older
    // copy is replaced, the latest refused bytes survive, and the announcement says so. RED before
    // the fix: the absence guard skipped the backup and the refused bytes existed nowhere.
    [Fact]
    public void ARefusedFile_IsPreservedEvenWhenAnOlderBackupExists_AndTheAnnouncementIsTrue()
    {
        Directory.CreateDirectory(_dir);
        var refused = "{\"schemaVersion\":2,\"appVersion\":\"9.9.9\",\"savedUtc\":\"2026-09-12T00:00:00+00:00\",\"layout\":{\"zones\":[],\"floating\":[]}}";
        File.WriteAllText(CodingSlot, refused);
        File.WriteAllText(CodingSlot + ".bak", "{ an older refusal's bytes }");
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();
        persistence.SaveNow();

        Assert.Null(persistence.LastSaveFailure);
        Assert.Contains("replacing an older copy", result.Announcement, StringComparison.Ordinal);
        Assert.Equal(refused, File.ReadAllText(CodingSlot + ".bak"));   // the latest refused bytes, not the older copy
        Assert.NotEqual(refused, File.ReadAllText(CodingSlot));
    }

    // A schema BELOW the current one is a file this store never wrote — refused as unreadable, not
    // announced as "a newer version".
    [Fact]
    public void ASchemaZeroFile_IsRefusedAsUnreadable_NotAsNewer()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(CodingSlot, "{\"schemaVersion\":0,\"appVersion\":\"0.0.1\",\"savedUtc\":\"2026-09-12T00:00:00+00:00\",\"layout\":{\"zones\":[],\"floating\":[]}}");
        var host = Host(PerspectiveSet.Coding);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        Assert.Equal(LayoutErrorCodes.Unreadable, result.ErrorCode);
        Assert.DoesNotContain("newer version", result.Announcement, StringComparison.Ordinal);
    }

    // The one-instance rule's "first" is the zone walk Left · Right · Bottom · Center, then the
    // floating stacks — across all four zones, not only Left before Center.
    [Fact]
    public void TheOneInstanceRule_KeepsTheFirstInZoneWalkOrder_AcrossAllZonesThenFloating()
    {
        var host = Host(PerspectiveSet.Coding);
        var saved = WorkbenchLayout.Empty()
            .WithZone(new ZoneState(ZoneId.Center, new ZoneStack([new Surface("d-center", "diagnostics", "Diagnostics")]), 1.0, Collapsed: false))
            .WithZone(new ZoneState(ZoneId.Bottom, new ZoneStack([new Surface("d-bottom", "diagnostics", "Diagnostics")]), 0.3, Collapsed: false))
            .WithZone(new ZoneState(ZoneId.Right, new ZoneStack([new Surface("d-right", "diagnostics", "Diagnostics")]), ZoneState.DefaultExtent, Collapsed: false))
            .WithZone(new ZoneState(ZoneId.Left, new ZoneStack([new Surface("d-left", "diagnostics", "Diagnostics")]), ZoneState.DefaultExtent, Collapsed: false));
        saved = saved with { Floating = [new StackNode("float-1", [new Surface("d-float", "diagnostics", "Diagnostics")], 0, StackState.Floating)] };

        var report = host.RestoreZones(saved);

        Assert.Equal("d-left", host.Zones.AllSurfaces().Single(s => s.Kind == "diagnostics").SurfaceId);
        Assert.Equal(["d-right", "d-bottom", "d-center", "d-float"], report.Dropped.Select(d => d.Surface.SurfaceId).ToList());
        Assert.Empty(host.Zones.Floating);
    }

    // SH-2's interim default seeded EVERY host from one combined WorkbenchLayout.Default() and
    // filtered it per host, which reported "domain" (kind "view") as a duplicate of "explore" in
    // Architecture. SH-3's WorkbenchLayout.Default(perspective) seeds each host from its OWN §B4
    // table (ZoneLayout.cs), so construction has nothing left to drop — this pins that retirement.
    [Fact]
    public void TheArchitectureAndCodingDefaults_SeedCleanly_WithNoDrops()
    {
        Assert.Empty(Host(PerspectiveSet.Architecture).DefaultDropped);
        Assert.Empty(Host(PerspectiveSet.Coding).DefaultDropped);
        Assert.Empty(Host(PerspectiveSet.Coordination).DefaultDropped);   // Ruling 84: host C seeds from its own table too
    }

    // Ruling 94 (F-C) — the operator: "this is the default layout i want … AND we should eliminate
    // the provenance tab." Architecture's default is Left = Graph at the extent the operator's own
    // saved slot holds (0.22, read from layout.architecture.zones.json — the proof doc records it)
    // · Center = Contexts (active), Domain · Right empty and COLLAPSED (the ruling wins over the
    // file's `collapsed: false` for the Right) · Bottom empty and collapsed. Evidence leaves the
    // default and stays admitted (the View menu); the `inspector` kind is retired from the product.
    [Fact]
    public void TheArchitectureDefault_IsLeftGraph_CenterContextsThenDomain_RightAndBottomEmptyAndCollapsed()
    {
        var layout = WorkbenchLayout.Default(PerspectiveSet.Architecture);

        Assert.Equal("Left:[graph@0]|Right:-/collapsed|Bottom:-/collapsed|Center:[contexts+domain@0]|float:", layout.Shape());
        Assert.Equal(0.22, layout.Zone(ZoneId.Left).Extent, precision: 3);
        Assert.Equal("canvas", layout.Zone(ZoneId.Left).Surfaces().Single().Kind);
        Assert.Equal(["contexts", "classdiagram"], layout.Zone(ZoneId.Center).Surfaces().Select(s => s.Kind));
        Assert.DoesNotContain(layout.AllSurfaces(), s => s.Kind is "view" or "inspector");
        // D-0 freeze (Owner N14 + Ruling 94): admitted, View-menu-only, not a default tab.
        Assert.DoesNotContain(layout.AllSurfaces(), s => s.Kind == "solution-tree");
        Assert.True(DockHost.AdmissionFor(PerspectiveSet.Architecture).Admits("solution-tree"));
        Assert.DoesNotContain(layout.AllSurfaces(), s => s.Kind == "entry-points");
        Assert.True(DockHost.AdmissionFor(PerspectiveSet.Architecture).Admits("entry-points"));

        // Evidence is one View-menu gesture away, not gone; Provenance is gone from the product.
        Assert.True(DockHost.AdmissionFor(PerspectiveSet.Architecture).Admits("view"));
        Assert.All(PerspectiveSet.All, p => Assert.False(DockHost.AdmissionFor(p).Admits("inspector"), $"{p.Title} still admits 'inspector'"));
        Assert.DoesNotContain("inspector", SurfaceContentFactory.KnownKinds);
    }

    // Ruling 94 CONDITION (2) — ADR-0032 test 1's shape for the retired kind: every operator who ran
    // the previous build has an Architecture slot file carrying Provenance (`inspector`) at Right.
    // It restores with that surface dropped and REPORTED, naming this ruling — never a crash, never
    // a silent reset — and everything else in the file survives.
    [Fact]
    public void APreRuling94ArchitectureEnvelopeCarryingProvenance_RestoresWithTheInspectorDroppedAndReported_NamingTheRuling()
    {
        Directory.CreateDirectory(_dir);
        var pre94 = WorkbenchLayout.Empty()
            .WithZone(new ZoneState(ZoneId.Left, new ZoneStack([new Surface("evidence", "view", "Evidence")]), ZoneState.DefaultExtent, Collapsed: false))
            .WithZone(new ZoneState(ZoneId.Right, new ZoneStack([new Surface("provenance", "inspector", "Provenance")]), ZoneState.DefaultExtent, Collapsed: false))
            .WithZone(new ZoneState(ZoneId.Center, new ZoneStack(
            [
                new Surface("graph", "canvas", "Graph"),
                new Surface("domain", "classdiagram", "Domain"),
                new Surface("contexts", "contexts", "Contexts"),
            ]), 1.0, Collapsed: false));
        new ZoneLayoutStore(LayoutPersistence.SlotPathFor(LayoutPath, PerspectiveSet.Architecture)).Save(pre94);
        var host = Host(PerspectiveSet.Architecture);
        using var persistence = PersistenceFor(host);

        var result = persistence.Restore();

        Assert.True(persistence.LastRestoreAppliedASavedArrangement);
        Assert.False(result.WasDefaulted);
        Assert.Equal("Left:[evidence@0]|Right:-|Bottom:-|Center:[graph+domain+contexts@0]|float:", host.Zones.Shape());

        var dropped = Assert.Single(persistence.LastRestoreDropped);
        Assert.Equal("provenance", dropped.Surface.SurfaceId);
        Assert.Equal(DropReason.KindNotAdmitted, dropped.Reason);
        Assert.Null(dropped.AdmittedBy);                                    // no perspective admits a retired kind
        Assert.Equal(LayoutErrorCodes.PartialRestore, result.ErrorCode);
        Assert.Equal(
            "1 pane from your saved layout isn't available in Architecture — Provenance (retired by Ruling 94: its origin, extractor and revision now show under the selected Evidence row).",
            result.Announcement);
    }

    public static IEnumerable<object[]> HostPerspectives()
    {
        yield return [PerspectiveSet.Coding];
        yield return [PerspectiveSet.Architecture];
        yield return [PerspectiveSet.Coordination];
    }

    // The one-instance invariant SH-2 named: WorkbenchLayout.Default(perspective) must place only
    // kinds that perspective's own allow-list admits, and never a second instance of a one-instance
    // kind — the same invariant SurfaceAdmission.Filter enforces at open/restore, checked here
    // against the SEED itself (which SH-3's DefaultLayout() no longer routes through a lossy filter
    // to discover).
    [Theory]
    [MemberData(nameof(HostPerspectives))]
    public void DefaultPerspective_PlacesOnlyAdmittedKinds_OneInstanceKindsAtMostOnce(Perspective perspective)
    {
        var admission = DockHost.AdmissionFor(perspective);
        var seenOneInstance = new HashSet<string>(StringComparer.Ordinal);

        foreach (var surface in WorkbenchLayout.Default(perspective).AllSurfaces())
        {
            Assert.True(admission.Admits(surface.Kind), $"'{surface.Kind}' is not admitted by {perspective.Title}");

            if (admission.IsOneInstance(surface.Kind))
            {
                Assert.True(seenOneInstance.Add(surface.Kind), $"'{surface.Kind}' is one-instance and appears twice in {perspective.Title}'s default");
            }
        }
    }

    // ADR-0032 test 1's last clause / US-C12 b3 — the event carries the dropped count and kinds,
    // per host, through the real shell's workspace-open restore: Coding drops eleven from the pre-C
    // file (seven Architecture kinds and, since Ruling 84, the four Loomkeeper kinds); Architecture
    // and Coordination keep current (no file); and the seed's own drop is on record.
    [Fact]
    public void TheWorkspaceOpenRestore_WritesOneRestoreEventPerHost_WithTheDroppedCountAndKinds()
    {
        WritePreAddendumCFile();
        var lines = new List<string>();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = line => { lock (lines) { lines.Add(line); } };
        try
        {
            Sta.Run(() =>
            {
                using var shell = new WorkbenchShell(queries: null, workspaceDataDirectory: _dir);
                return 0;
            }, 60);
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }

        var restores = lines.Select(l => JsonDocument.Parse(l).RootElement)
            .Where(e => e.GetProperty("evt").GetString() == "layout.restore")
            .ToList();

        var coding = Assert.Single(restores, e => e.GetProperty("perspective").GetString() == "coding" && e.GetProperty("placement").GetString() == "restore-zones");
        Assert.Equal(11, coding.GetProperty("dropped_count").GetInt32());
        var droppedKinds = coding.GetProperty("dropped_kinds").EnumerateArray().Select(k => k.GetString()).ToList();
        Assert.Contains("classdiagram", droppedKinds);
        Assert.Contains("canvas", droppedKinds);
        Assert.Contains("sessions", droppedKinds);      // Ruling 84: the Loomkeeper kinds leave Coding
        Assert.Contains("ledger", droppedKinds);
        Assert.Equal(LayoutErrorCodes.PartialRestore, coding.GetProperty("error_code").GetString());   // a drop is a partial restore

        var architecture = Assert.Single(restores, e => e.GetProperty("perspective").GetString() == "architecture" && e.GetProperty("placement").GetString() == "keep-current");
        Assert.Equal(0, architecture.GetProperty("dropped_count").GetInt32());

        var coordination = Assert.Single(restores, e => e.GetProperty("perspective").GetString() == "coordination" && e.GetProperty("placement").GetString() == "keep-current");
        Assert.Equal(0, coordination.GetProperty("dropped_count").GetInt32());

        // SH-3: each host seeds from its OWN §B4 table (ZoneLayout.cs), so construction drops
        // nothing for either host and no "default-filtered" event is logged at all — unlike the
        // interim combined-and-filtered seed this pins the retirement of (see
        // TheArchitectureAndCodingDefaults_SeedCleanly_WithNoDrops).
        Assert.DoesNotContain(restores, e => e.GetProperty("placement").GetString() == "default-filtered");
    }

    private string DuplicateIdFile()
    {
        var store = new ZoneLayoutStore(CodingSlot);
        store.Save(WorkbenchLayout.Default());
        return File.ReadAllText(CodingSlot).Replace("\"surfaceId\": \"terminal-1\"", "\"surfaceId\": \"sessions\"", StringComparison.Ordinal);
    }

    private static readonly JsonSerializerOptions FrozenJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// <b>The frozen schema-1 DTO records</b> — a verbatim copy of <c>ZoneLayoutStore.cs:10-42</c> as
    /// of ADR-0032's acceptance (2026-09-11), standing in for the pre-ADR reader a test cannot run.
    /// Positional records: a member removed from the live DTO breaks this reader; an added one is
    /// ignored. Never edit to make the live side pass.
    /// </summary>
    private static class Frozen
    {
        public sealed record ZoneEnvelope(
            [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
            [property: JsonPropertyName("appVersion")] string AppVersion,
            [property: JsonPropertyName("savedUtc")] DateTimeOffset SavedUtc,
            [property: JsonPropertyName("layout")] ZoneLayoutDto Layout);

        public sealed record ZoneLayoutDto(
            [property: JsonPropertyName("zones")] List<ZoneStateDto> Zones,
            [property: JsonPropertyName("floating")] List<ZoneStackDto> Floating);

        public sealed record ZoneStateDto(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("content")] ZoneContentDto? Content,
            [property: JsonPropertyName("extent")] double Extent,
            [property: JsonPropertyName("collapsed")] bool Collapsed);

        public sealed record ZoneContentDto(
            [property: JsonPropertyName("kind")] string Kind,
            [property: JsonPropertyName("tabs")] List<ZoneSurfaceDto>? Tabs,
            [property: JsonPropertyName("activeIndex")] int ActiveIndex,
            [property: JsonPropertyName("orientation")] string? Orientation,
            [property: JsonPropertyName("children")] List<ZoneContentDto>? Children,
            [property: JsonPropertyName("weights")] List<double>? Weights);

        public sealed record ZoneStackDto(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("surfaces")] List<ZoneSurfaceDto> Surfaces,
            [property: JsonPropertyName("activeIndex")] int ActiveIndex);

        public sealed record ZoneSurfaceDto(
            [property: JsonPropertyName("surfaceId")] string SurfaceId,
            [property: JsonPropertyName("kind")] string Kind,
            [property: JsonPropertyName("title")] string Title);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) { Directory.Delete(_dir, recursive: true); } }
        catch (IOException) { }
    }
}
