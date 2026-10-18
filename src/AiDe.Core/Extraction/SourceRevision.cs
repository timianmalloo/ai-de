namespace AiDe.Core.Extraction;

/// <summary>
/// The revision a fact is stored under: the caller's artifact revision, plus the extractor
/// generation that read it.
/// </summary>
/// <remarks>
/// <para><b>Why a fact's identity includes its reader.</b> The store's natural key is
/// <c>(scope_id, artifact_revision, subject, predicate, object, extractor_id)</c> — P1-STORE-05,
/// "one revision, one answer". That was true while the extractor was fixed. It stopped being true
/// the moment extraction could improve for input that had not changed: the same bytes read by a
/// better reader are a DIFFERENT observation, and the key had no way to say so.</para>
///
/// <para><b>What went wrong without it.</b> <c>ScopeFingerprints.ExtractorGeneration</c> was bumped
/// so an upgrade would invalidate every cached scope — and it did. But the reuse check inside
/// <c>RefreshScopeAsync</c> asks a second, independent question ("does the store already hold this
/// revision?"), knew nothing about the generation, and answered yes. So the re-index visited all 66
/// scopes and wrote nothing: <i>"Indexed 66 of 66 scope(s): 0 assertion(s)"</i>, with the Knowledge
/// chip still reading 0 on a repository holding 2,343 knowledge nodes.</para>
///
/// <para>Removing that second guard would not have been enough. Had it re-extracted, every unchanged
/// fact would have collided with the unique index, because the key genuinely could not represent the
/// new observation. The guard was the symptom; the key was the cause.</para>
///
/// <para><c>simplify: the generation is carried in the revision STRING rather than its own column in
/// the natural key; ceiling is that a stored revision is no longer the caller's literal text, so
/// anything showing one to a person calls <see cref="Base"/> first (which also drops the working-tree content digest); upgrade trigger = the store
/// gains migration machinery, at which point extractor_generation becomes a real column and this
/// type collapses to nothing.</c></para>
/// </remarks>
public static class SourceRevision
{
    /// <summary>
    /// A RETIRED FIXTURE, not a revision. <c>"rev-1"</c> was the literal the shell attached to every
    /// workspace it indexed before Ruling 85 — a door default read as a measurement (DC-110) — and
    /// the stores those builds wrote still carry it on every snapshot. Named so the reuse guard can
    /// refuse it by name (Ruling 98): a snapshot stamped with it is re-extracted once under the
    /// observed HEAD. Nothing in the product path attaches it; tests that build a pre-85 store do.
    /// </summary>
    public const string RetiredFixtureLiteral = "rev-1";

    private const string Marker = "+x";

    private const string ContentMarker = "+d:";

    private static readonly string Suffix = Marker + ScopeFingerprints.ExtractorGeneration;

    /// <summary>
    /// The revision to STORE facts under. Idempotent: stamping an already-stamped revision returns
    /// it unchanged, so a caller that passes one through twice does not create a third identity.
    /// </summary>
    /// <param name="artifactRevision">The caller's revision, typically the git HEAD.</param>
    /// <param name="contentDigest">
    /// A digest of the scope's working-tree inputs. The caller captures HEAD once and keeps it while
    /// the user edits files under it (FR-001), so the revision alone cannot tell old content from new:
    /// without this component a forced re-index of an edited scope collided with, or was skipped as,
    /// the snapshot of the unedited one. Empty or null adds nothing.
    /// </param>
    public static string Stamp(string artifactRevision, string? contentDigest = null)
    {
        ArgumentNullException.ThrowIfNull(artifactRevision);

        if (artifactRevision.EndsWith(Suffix, StringComparison.Ordinal)) return artifactRevision;

        return string.IsNullOrEmpty(contentDigest)
            ? artifactRevision + Suffix
            : artifactRevision + ContentMarker + contentDigest + Suffix;
    }

    /// <summary>
    /// The revision to SHOW: the caller's own text, with any extractor stamp removed.
    /// </summary>
    /// <remarks>
    /// Strips any generation, not only the current one, because a surface routinely renders evidence
    /// written by an older build — a stale scope's disclosure exists precisely for that case.
    /// </remarks>
    public static string Base(string revision)
    {
        if (string.IsNullOrEmpty(revision)) return revision;

        var at = revision.LastIndexOf(Marker, StringComparison.Ordinal);
        var withoutGeneration = at <= 0 ? revision : revision[..at];

        var content = withoutGeneration.LastIndexOf(ContentMarker, StringComparison.Ordinal);
        return content <= 0 ? withoutGeneration : withoutGeneration[..content];
    }
}
