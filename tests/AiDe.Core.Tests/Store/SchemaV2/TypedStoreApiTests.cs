using AiDe.Core.Facts;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.SchemaV2;

/// <summary>The typed append/read API every later workbench-loops track consumes.</summary>
public sealed class TypedStoreApiTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    [Fact]
    public void EveryV2Row_RoundTripsThroughTheTypedApi()
    {
        var profile = SchemaV2Support.SeedEveryV2Table(Store);

        using var reader = Store.BeginRead();

        var journey = reader.ReadJourney("j1")!;
        Assert.Equal(("work", "First journey", "{\"scope\":\"s\"}", 1), (journey.Purpose, journey.Title, journey.OriginScope, journey.VersionNo));

        var notebook = reader.ReadNotebook("nb1")!;
        Assert.Equal(("j1", false), (notebook.OwnerJourneyId, notebook.Legacy));

        var revision = reader.ReadNotebookRevision("nb1", 1)!;
        Assert.Equal("body one", revision.Body);
        Assert.Equal(64, revision.BodySha!.Length);
        Assert.Null(revision.ParentRevisionNo);

        var attempt = reader.ReadAttempt("k1")!;
        Assert.Equal(("sha-p", "route-x", profile, "{}"), (attempt.ProjectionSha, attempt.RouteFingerprint, attempt.ProfileKey, attempt.ScopeSnapshot));
        Assert.Equal(DispatchState.Dispatched, Assert.Single(reader.ReadOutcomes("k1")).State);

        var decision = reader.ReadPermissionDecision("s1", "k1", 7)!;
        Assert.Equal(("allow", "user", "sha-a"), (decision.OptionChosen, decision.Actor, decision.ArgumentsSha));

        Assert.Equal("artifact://a", Assert.Single(reader.ReadEvidenceLinks("criterion", "c1", 1)).ArtifactRef);
        Assert.Equal("as1", reader.ReadLatestAssessment("drift", "scope-1")!.AssessmentId);
        Assert.True(Assert.Single(reader.ReadObligations("cl1", 1)).Mandatory);
        Assert.Equal(1, reader.ReadLatestChecklistRevision("cl1"));
        Assert.Equal("acknowledged", Assert.Single(reader.ReadDispositions("obligation", "ob1")).Kind);

        var hp = reader.ReadHarnessProfile("hp1")!;
        Assert.Equal((profile, "claude", 1), (hp.ProfileKey, hp.Family, hp.VersionNo));
        Assert.Equal("hp1", reader.ReadHarnessProfileByKey(profile)!.ProfileId);
        Assert.Equal("true", reader.ReadLatestProfileObservation(profile, "installed")!.Value);
        Assert.Null(reader.ReadLatestProfileObservation(profile, "quota"));
    }

    [Fact]
    public void ADimensionChange_IsANewVersionRow_AndTheLatestRead_ReturnsIt()
    {
        using (var writer = Store.BeginWrite())
        {
            writer.AppendJourney(new JourneyRow("j1", "work", "Old title"));
            writer.AppendJourney(new JourneyRow("j1", "work", "New title"));
            writer.Commit();
        }

        using var reader = Store.BeginRead();
        Assert.Equal(("New title", 2), (reader.ReadJourney("j1")!.Title, reader.ReadJourney("j1")!.VersionNo));
        Assert.Equal("New title", Assert.Single(reader.ListJourneys()).Title);
    }

    [Fact]
    public void AType0Attribute_CannotChangeInANewVersionRow()
    {
        using (var writer = Store.BeginWrite())
        {
            writer.AppendJourney(new JourneyRow("j1", "work", "t", OriginKind: "promoted-from", OriginJourneyId: "j0"));
            writer.Commit();
        }

        using var second = Store.BeginWrite();
        var ex = Assert.Throws<WorkspaceStoreException>(() =>
            second.AppendJourney(new JourneyRow("j1", "investigation", "t", OriginKind: "promoted-from", OriginJourneyId: "j0")));
        Assert.Equal(StoreErrorCodes.Type0Violation, ex.ErrorCode);

        var origin = Assert.Throws<WorkspaceStoreException>(() =>
            second.AppendJourney(new JourneyRow("j1", "work", "t", OriginKind: "investigation-of", OriginJourneyId: "j0")));
        Assert.Equal(StoreErrorCodes.Type0Violation, origin.ErrorCode);
    }

    [Fact]
    public void AHarnessProfile_FamilyAndEngineAreType0_ButNativeHomeMayChange()
    {
        using var writer = Store.BeginWrite();
        var first = writer.AppendHarnessProfile(new HarnessProfileRow("hp1", "engine-1", "claude", "home-1"));

        var moved = writer.AppendHarnessProfile(new HarnessProfileRow("hp1", "engine-1", "claude", "home-2"));
        Assert.NotEqual(first, moved);

        var ex = Assert.Throws<WorkspaceStoreException>(() =>
            writer.AppendHarnessProfile(new HarnessProfileRow("hp1", "engine-1", "codex", "home-2")));
        Assert.Equal(StoreErrorCodes.Type0Violation, ex.ErrorCode);
    }

    [Fact]
    public void ALegacyNotebook_IsAdoptedByANewVersionRow_ThenItsOwnerIsFixed()
    {
        // A legacy row exists only from the migration backfill (Ruling 148 c), so start from a real v1 file.
        var directory = SchemaV2Support.NewDirectory();
        try
        {
            var path = Path.Combine(directory, "workspace.db");
            SchemaV2Support.CreateV1Database(path);
            using var migrated = WorkspaceStore.Open(path);
            using (var writer = migrated.BeginWrite())
            {
                writer.AppendJourney(new JourneyRow("j1", "work", "t"));
                writer.AppendJourney(new JourneyRow("j2", "work", "t2"));
                // Ruling 156(3): adoption is refused until v3's adopted_by can record its actor; no version row is written.
                var ex = Assert.Throws<WorkspaceStoreException>(() =>
                    writer.AppendNotebook(new NotebookRow("draft-a", "j1", Legacy: false)));
                Assert.Equal("AIDE-STORE-ADOPTION-NOT-OFFERED", ex.ErrorCode);
                writer.Commit();
            }

            using var reader = migrated.BeginRead();
            var notebook = reader.ReadNotebook("draft-a")!;
            Assert.Equal((1, true, (string?)null), (notebook.VersionNo, notebook.Legacy, notebook.OwnerJourneyId));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(directory, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void ARevisionBranch_ResolvesItsParent_AndRecordsABodyHash()
    {
        using (var writer = Store.BeginWrite())
        {
            SchemaV2Support.SeedNotebook(writer);
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "root"));
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 2, "a", ParentRevisionNo: 1));
            writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 3, "b", ParentRevisionNo: 1));
            writer.Commit();
        }

        using var reader = Store.BeginRead();
        Assert.Equal(1, reader.ReadNotebookRevision("nb1", 3)!.ParentRevisionNo);
        Assert.NotEqual(reader.ReadNotebookRevision("nb1", 2)!.BodySha, reader.ReadNotebookRevision("nb1", 3)!.BodySha);
    }

    [Fact]
    public void ThePtyDispatchPath_StillWorks_AndKeepsTheOneRouteFingerprint()
    {
        using (var writer = Store.BeginWrite())
        {
            writer.SavePromptRevision("d", 1, "hello");
            writer.RecordDispatchAttempt("k", "ws", 1, "d", 1, "s", 1);
            writer.RecordDispatchOutcome("k", DispatchState.PtyWriteAccepted, null);
            writer.Commit();
        }

        using var reader = Store.BeginRead();
        Assert.Equal("pty", reader.ReadAttempt("k")!.RouteFingerprint);
        Assert.Equal(DispatchState.PtyWriteAccepted, reader.ReadDispatchReceipt("k")!.State);
    }
}
