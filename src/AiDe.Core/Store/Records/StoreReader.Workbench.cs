using AiDe.Core.Facts;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Store;

// ADR-0039 read API: by-key and latest-per-key reads over the v2 rows. Every by-key read is a
// SEARCH on an index (asserted by SchemaV2/ReadPlanTests); the List* reads scan their dimension by
// design. Nothing here folds — acceptance, closure, currentness are the callers' derivations.
public sealed partial class StoreReader
{
    private const string JourneyColumns = """
        journey_id, purpose, title, origin_scope, origin_journey_id, origin_kind, origin_object_ids,
        version_no, ingress_seq, recorded_at
        """;

    private const string NotebookColumns = "notebook_id, owner_journey_id, legacy, version_no, ingress_seq, recorded_at";

    private const string ProfileColumns = """
        profile_id, engine_id, family, native_home, declared_account, operator_token_ref,
        profile_key, version_no, ingress_seq, recorded_at
        """;

    // By-key read SQL, named so the plan test can EXPLAIN exactly what the readers run.
    internal const string SqlReadJourney =
        $"SELECT {JourneyColumns} FROM journey_dim WHERE journey_id = $id ORDER BY ingress_seq DESC LIMIT 1;";

    internal const string SqlReadNotebook =
        $"SELECT {NotebookColumns} FROM notebook_dim WHERE notebook_id = $id ORDER BY ingress_seq DESC LIMIT 1;";

    internal const string SqlReadNotebookRevision = """
        SELECT draft_id, revision_no, body, parent_revision_no, body_sha, ingress_seq, saved_at
        FROM prompt_revision_fact WHERE draft_id = $id AND revision_no = $rev;
        """;

    internal const string SqlReadAttempt = """
        SELECT dispatch_key, workspace_id, workspace_epoch, draft_id, revision_no, session_id,
               session_generation, projection_sha, route_fingerprint, profile_key, scope_snapshot,
               parent_dispatch_key, queued_after_dispatch_key, ingress_seq, attempted_at
        FROM dispatch_attempt_fact WHERE dispatch_key = $key;
        """;

    internal const string SqlReadOutcomes = """
        SELECT dispatch_key, state, error_code, cause, detail, ingress_seq, recorded_at
        FROM dispatch_outcome_fact WHERE dispatch_key = $key ORDER BY ingress_seq;
        """;

    internal const string SqlReadPermissionDecision = """
        SELECT session_id, run_id, seq, action, scope, arguments_sha, options_offered, option_chosen,
               actor, expires_at, ingress_seq, recorded_at
        FROM permission_decision_fact WHERE session_id = $s AND run_id = $r AND seq = $q;
        """;

    internal const string SqlReadEvidenceLinks = """
            SELECT link_id, subject_kind, subject_id, subject_revision, artifact_ref, producer, baseline,
                   verification_status, limitations, ingress_seq, recorded_at
            FROM evidence_link_fact
            WHERE subject_kind = $k AND subject_id = $id AND subject_revision = $rev ORDER BY ingress_seq;
            """;

    internal const string SqlReadLatestAssessment = """
        SELECT assessment_id, kind, scope, baseline, omissions, payload, ingress_seq, recorded_at
        FROM assessment_fact WHERE kind = $k AND scope = $s ORDER BY ingress_seq DESC LIMIT 1;
        """;

    internal const string SqlReadDispositions = """
        SELECT subject_kind, subject_id, kind, actor, baseline, reason, notebook_id, revision_no,
               ingress_seq, recorded_at
        FROM disposition_fact WHERE subject_kind = $k AND subject_id = $id ORDER BY ingress_seq;
        """;

    internal const string SqlReadObligations = """
        SELECT obligation_id, checklist_id, revision, text, mandatory, ingress_seq, recorded_at
        FROM checklist_obligation_fact WHERE checklist_id = $c AND revision = $rev ORDER BY ingress_seq;
        """;

    internal const string SqlReadHarnessProfile =
        $"SELECT {ProfileColumns} FROM harness_profile_dim WHERE profile_id = $id ORDER BY ingress_seq DESC LIMIT 1;";

    internal const string SqlReadHarnessProfileByKey =
        $"SELECT {ProfileColumns} FROM harness_profile_dim WHERE profile_key = $key;";

    internal const string SqlReadLatestProfileObservation = """
        SELECT profile_key, kind, value, source, observed_at, duration_ms, exit_code, ingress_seq
        FROM profile_observation_fact WHERE profile_key = $key AND kind = $kind
        ORDER BY ingress_seq DESC LIMIT 1;
        """;

    /// <summary>The latest version of one journey, or null.</summary>
    public JourneyRow? ReadJourney(string journeyId) => ReadOne(SqlReadJourney,
        MapJourney, ("$id", journeyId));

    /// <summary>Every journey's latest version, in creation order.</summary>
    public IReadOnlyList<JourneyRow> ListJourneys() => ReadMany($"""
        SELECT {JourneyColumns} FROM journey_dim j
        WHERE ingress_seq = (SELECT max(ingress_seq) FROM journey_dim WHERE journey_id = j.journey_id)
        ORDER BY (SELECT min(ingress_seq) FROM journey_dim WHERE journey_id = j.journey_id);
        """, MapJourney);

    public NotebookRow? ReadNotebook(string notebookId) => ReadOne(SqlReadNotebook,
        MapNotebook, ("$id", notebookId));

    /// <summary>Every notebook's latest version; legacy (unowned) notebooks included.</summary>
    public IReadOnlyList<NotebookRow> ListNotebooks() => ReadMany($"""
        SELECT {NotebookColumns} FROM notebook_dim n
        WHERE ingress_seq = (SELECT max(ingress_seq) FROM notebook_dim WHERE notebook_id = n.notebook_id)
        ORDER BY ingress_seq;
        """, MapNotebook);

    public NotebookRevisionRow? ReadNotebookRevision(string notebookId, int revisionNo) => ReadOne(SqlReadNotebookRevision,
        r => new NotebookRevisionRow(r.GetString(0), r.GetInt32(1), r.GetString(2), NullableInt(r, 3))
        {
            BodySha = NullableString(r, 4), IngressSeq = r.GetInt64(5), SavedAt = r.GetString(6),
        },
        ("$id", notebookId), ("$rev", revisionNo));

    public AttemptRow? ReadAttempt(string dispatchKey) => ReadOne(SqlReadAttempt,
        r => new AttemptRow(
            r.GetString(0), r.GetString(1), r.GetInt64(2), r.GetString(3), r.GetInt32(4), r.GetString(5),
            r.GetInt64(6), NullableString(r, 7), NullableString(r, 8), NullableLong(r, 9),
            NullableString(r, 10), NullableString(r, 11), NullableString(r, 12))
        {
            IngressSeq = r.GetInt64(13), AttemptedAt = r.GetString(14),
        },
        ("$key", dispatchKey));

    /// <summary>Every outcome event of an attempt, oldest first. The caller folds them.</summary>
    public IReadOnlyList<OutcomeRow> ReadOutcomes(string dispatchKey) => ReadMany(SqlReadOutcomes,
        r => new OutcomeRow(
            r.GetString(0), Enum.Parse<DispatchState>(r.GetString(1)),
            NullableString(r, 2), NullableString(r, 3), NullableString(r, 4))
        {
            IngressSeq = r.GetInt64(5), RecordedAt = r.GetString(6),
        },
        ("$key", dispatchKey));

    public PermissionDecisionRow? ReadPermissionDecision(string sessionId, string runId, long seq) => ReadOne(SqlReadPermissionDecision,
        r => new PermissionDecisionRow(
            r.GetString(0), r.GetString(1), r.GetInt64(2), r.GetString(3), r.GetString(4), r.GetString(5),
            r.GetString(6), r.GetString(7), r.GetString(8), NullableString(r, 9))
        {
            IngressSeq = r.GetInt64(10), RecordedAt = r.GetString(11),
        },
        ("$s", sessionId), ("$r", runId), ("$q", seq));

    /// <summary>Every link to one claim/criterion revision, oldest first.</summary>
    public IReadOnlyList<EvidenceLinkRow> ReadEvidenceLinks(string subjectKind, string subjectId, int subjectRevision) =>
        ReadMany(SqlReadEvidenceLinks,
            r => new EvidenceLinkRow(
                r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetString(4), r.GetString(5),
                r.GetString(6), r.GetString(7), NullableString(r, 8))
            {
                IngressSeq = r.GetInt64(9), RecordedAt = r.GetString(10),
            },
            ("$k", subjectKind), ("$id", subjectId), ("$rev", subjectRevision));

    /// <summary>The newest assessment of one kind for one scope (any baseline), or null.</summary>
    public AssessmentRow? ReadLatestAssessment(string kind, string scope) => ReadOne(SqlReadLatestAssessment,
        r => new AssessmentRow(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3),
            r.GetString(4), r.GetString(5))
        {
            IngressSeq = r.GetInt64(6), RecordedAt = r.GetString(7),
        },
        ("$k", kind), ("$s", scope));

    /// <summary>Every disposition on one subject, oldest first. The caller picks the current one per actor.</summary>
    public IReadOnlyList<DispositionRow> ReadDispositions(string subjectKind, string subjectId) => ReadMany(SqlReadDispositions,
        r => new DispositionRow(
            r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            NullableString(r, 5), NullableString(r, 6), NullableInt(r, 7))
        {
            IngressSeq = r.GetInt64(8), RecordedAt = r.GetString(9),
        },
        ("$k", subjectKind), ("$id", subjectId));

    /// <summary>The obligations of one checklist revision.</summary>
    public IReadOnlyList<ObligationRow> ReadObligations(string checklistId, int revision) => ReadMany(SqlReadObligations,
        r => new ObligationRow(r.GetString(0), r.GetString(1), r.GetInt32(2), r.GetString(3), r.GetInt32(4) == 1)
        {
            IngressSeq = r.GetInt64(5), RecordedAt = r.GetString(6),
        },
        ("$c", checklistId), ("$rev", revision));

    /// <summary>The highest checklist revision recorded, or null.</summary>
    public int? ReadLatestChecklistRevision(string checklistId) =>
        Command("SELECT max(revision) FROM checklist_obligation_fact WHERE checklist_id = $c;", ("$c", checklistId))
            .ExecuteScalar() is long revision ? (int)revision : null;

    /// <summary>The latest version of one harness profile, or null.</summary>
    public HarnessProfileRow? ReadHarnessProfile(string profileId) => ReadOne(SqlReadHarnessProfile,
        MapProfile, ("$id", profileId));

    /// <summary>One profile version by the surrogate key an attempt carries.</summary>
    public HarnessProfileRow? ReadHarnessProfileByKey(long profileKey) => ReadOne(SqlReadHarnessProfileByKey,
        MapProfile, ("$key", profileKey));

    public IReadOnlyList<HarnessProfileRow> ListHarnessProfiles() => ReadMany($"""
        SELECT {ProfileColumns} FROM harness_profile_dim p
        WHERE ingress_seq = (SELECT max(ingress_seq) FROM harness_profile_dim WHERE profile_id = p.profile_id)
        ORDER BY ingress_seq;
        """, MapProfile);

    /// <summary>The newest observation of one kind on one profile version, or null (= Not recorded).</summary>
    public ProfileObservationRow? ReadLatestProfileObservation(long profileKey, string kind) => ReadOne(SqlReadLatestProfileObservation,
        r => new ProfileObservationRow(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            NullableLong(r, 5), NullableInt(r, 6))
        {
            IngressSeq = r.GetInt64(7),
        },
        ("$key", profileKey), ("$kind", kind));

    private static JourneyRow MapJourney(SqliteDataReader r) => new(
        r.GetString(0), r.GetString(1), r.GetString(2), NullableString(r, 3), NullableString(r, 4),
        NullableString(r, 5), NullableString(r, 6))
    {
        VersionNo = r.GetInt32(7), IngressSeq = r.GetInt64(8), RecordedAt = r.GetString(9),
    };

    private static NotebookRow MapNotebook(SqliteDataReader r) => new(
        r.GetString(0), NullableString(r, 1), r.GetInt32(2) == 1)
    {
        VersionNo = r.GetInt32(3), IngressSeq = r.GetInt64(4), RecordedAt = r.GetString(5),
    };

    private static HarnessProfileRow MapProfile(SqliteDataReader r) => new(
        r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), NullableString(r, 4), NullableString(r, 5))
    {
        ProfileKey = r.GetInt64(6), VersionNo = r.GetInt32(7), IngressSeq = r.GetInt64(8), RecordedAt = r.GetString(9),
    };

    private static string? NullableString(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);

    private static int? NullableInt(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetInt32(i);

    private static long? NullableLong(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetInt64(i);

    private T? ReadOne<T>(string sql, Func<SqliteDataReader, T> map, params (string Name, object? Value)[] parameters)
        where T : class
    {
        using var command = Command(sql, parameters);
        using var reader = command.ExecuteReader();
        return reader.Read() ? map(reader) : null;
    }

    private IReadOnlyList<T> ReadMany<T>(string sql, Func<SqliteDataReader, T> map, params (string Name, object? Value)[] parameters)
    {
        using var command = Command(sql, parameters);
        using var reader = command.ExecuteReader();
        var rows = new List<T>();
        while (reader.Read())
        {
            rows.Add(map(reader));
        }

        return rows;
    }
}
