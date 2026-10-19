using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.SchemaV2;

// Ruling 150: the standing terminal-drafts journey is a marked owner, and the assessment-payload guard is
// exactly ADR-0039 section 3 fold-derived names. Literals, not the production constants, so a rename fails here.

internal static class Ruling150
{
    internal const string TerminalJourney = "journey-terminal-drafts";
    internal const string TerminalOriginScope = "{\"standing\":\"terminal-drafts\"}";
    internal const string ReservedCode = "AIDE-STORE-RESERVED-JOURNEY";

    internal static string Violation(Action<StoreWriter> act, WorkspaceStore store)
    {
        using var writer = store.BeginWrite();
        var ex = Assert.ThrowsAny<Exception>(() => act(writer));
        return ex is WorkspaceStoreException w ? w.ErrorCode : ((SqliteException)ex).Message;
    }
}

/// <summary>(1a) The migration creates the standing journey exactly once, with purpose work and the origin marker; the report counts it.</summary>
public sealed class Ruling150MigrationTests : IDisposable
{
    private readonly string _directory = SchemaV2Support.NewDirectory();

    private string DbPath => Path.Combine(_directory, "workspace.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void TheMigration_CreatesTheStandingJourneyExactlyOnce_MarkedAndCounted()
    {
        SchemaV2Support.CreateV1Database(DbPath);

        using (var store = WorkspaceStore.Open(DbPath))
        {
            Assert.Equal(1, store.Report.StandingJourneysCreated);
        }

        using (var reopened = WorkspaceStore.Open(DbPath))
        {
            Assert.Null(reopened.Report.MigratedFromVersion);
            Assert.Equal(0, reopened.Report.StandingJourneysCreated);
        }

        SqliteConnection.ClearAllPools();
        using var raw = SchemaV2Support.OpenRaw(DbPath, readOnly: true);
        Assert.Equal(1, SchemaV2Support.Count(raw, $"SELECT count(*) FROM journey_dim WHERE journey_id = '{Ruling150.TerminalJourney}'"));
        Assert.Equal(1, SchemaV2Support.Count(raw,
            $"SELECT count(*) FROM journey_dim WHERE journey_id = '{Ruling150.TerminalJourney}' AND version_no = 1 "
            + $"AND purpose = 'work' AND origin_scope = '{Ruling150.TerminalOriginScope}' AND origin_kind IS NULL"));
    }

    [Fact]
    public void AFreshStore_MarksTheLazilyCreatedStandingJourneyTheSameWay()
    {
        using var workspace = TestWorkspace.Create();
        using (var writer = workspace.Store.BeginWrite())
        {
            writer.SavePromptRevision("pty-a", 1, "x");
            writer.SavePromptRevision("pty-b", 1, "y");
            writer.Commit();
        }

        using var raw = SchemaV2Support.OpenRaw(workspace.DatabasePath, readOnly: true);
        Assert.Equal(1, SchemaV2Support.Count(raw,
            $"SELECT count(*) FROM journey_dim WHERE journey_id = '{Ruling150.TerminalJourney}' "
            + $"AND purpose = 'work' AND origin_scope = '{Ruling150.TerminalOriginScope}'"));
    }
}

/// <summary>(1b, 1c) No non-PTY writer may default to the standing journey; a later adoption re-owns a draft out of it.</summary>
public sealed class Ruling150StandingJourneyUseTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    private WorkspaceStore Store => _workspace.Store;

    [Fact]
    public void ATypedNotebook_OwnedByTheStandingJourney_IsRefused()
    {
        using (var writer = Store.BeginWrite())
        {
            writer.SavePromptRevision("pty-seed", 1, "seed");
            writer.Commit();
        }

        var code = Ruling150.Violation(w => w.AppendNotebook(new NotebookRow("typed-nb", Ruling150.TerminalJourney, Legacy: false)), Store);

        Assert.Equal(Ruling150.ReservedCode, code);
    }

    [Fact]
    public void ATypedJourney_ClaimingTheStandingId_IsRefused()
    {
        var code = Ruling150.Violation(w => w.AppendJourney(new JourneyRow(Ruling150.TerminalJourney, "work", "mine")), Store);

        Assert.Equal(Ruling150.ReservedCode, code);
    }

    [Fact]
    public void ALaterAdoption_ReOwnsADraftOutOfTheStandingJourney_AndItsRevisionsStay()
    {
        using (var writer = Store.BeginWrite())
        {
            writer.SavePromptRevision("pty-draft", 1, "hello");
            writer.AppendJourney(new JourneyRow("j-real", "work", "a real journey"));
            writer.AppendNotebook(new NotebookRow("pty-draft", "j-real", Legacy: false));
            writer.Commit();
        }

        using var raw = SchemaV2Support.OpenRaw(_workspace.DatabasePath, readOnly: true);
        Assert.Equal(1, SchemaV2Support.Count(raw,
            "SELECT count(*) FROM notebook_dim WHERE notebook_id = 'pty-draft' AND version_no = 2 AND owner_journey_id = 'j-real'"));
        Assert.Equal(1, SchemaV2Support.Count(raw, "SELECT count(*) FROM prompt_revision_fact WHERE draft_id = 'pty-draft'"));
    }
}

/// <summary>(3) The forbidden payload keys are exactly ADR-0039 section 3 fold-derived names, matched ignoring case and separators.</summary>
public sealed class Ruling150PayloadGuardTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void TheForbiddenList_IsPinnedToAdr0039Section3()
    {
        // ADR-0039 section 3 (lines 128-129, 151, 190). Adding a derived quantity there means extending this
        // list, the trigger and this pin together; WL-5 may tighten the guard, never loosen it.
        string[] section3 =
        [
            "satisfied", "satisfied_by", "accepted", "eligible", "closed", "current", "stale", "ready", "has_evidence", "proposed",
        ];

        Assert.Equal(section3.Order(), WorkbenchVocabulary.ForbiddenPayloadKeys.Order());
    }

    [Theory]
    [InlineData("{\"satisfied\":true}")]
    [InlineData("{\"satisfied_by\":\"d1\"}")]
    [InlineData("{\"satisfiedBy\":\"d1\"}")]
    [InlineData("{\"hasEvidence\":true}")]
    [InlineData("{\"HAS-EVIDENCE\":true}")]
    [InlineData("{\"has evidence\":true}")]
    [InlineData("{\"Has_Evidence\":true}")]
    [InlineData("{\"proposed\":true}")]
    [InlineData("{\"eligible\":true}")]
    [InlineData("{\"findings\":[{\"id\":\"f\",\"Proposed\":1}]}")]
    public void ADerivedName_InAnyCaseOrSeparator_IsRefused(string payload)
    {
        var code = Ruling150.Violation(w => w.AppendAssessment(
            new AssessmentRow("as-bad", "drift", "scope", "base", "[]", payload)), _workspace.Store);

        Assert.Equal(Ruling148.DerivedStateCode, code);
    }

    [Theory]
    [InlineData("{\"state\":\"running\"}")]
    [InlineData("{\"status\":\"missing\"}")]
    public void AResourceStateFinding_IsNotADerivedName_AndIsAccepted(string payload)
    {
        using var writer = _workspace.Store.BeginWrite();
        writer.AppendAssessment(new AssessmentRow("as-ok", "drift", "scope", "base", "[]", payload));
        writer.Commit();
    }
}
