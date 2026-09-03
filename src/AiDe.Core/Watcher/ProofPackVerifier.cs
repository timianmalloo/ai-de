namespace AiDe.Core.Watcher;

/// <summary>
/// What could be established about one declared Proof Pack path.
/// </summary>
/// <remarks>
/// <b>Three states, not a bool</b>, because a bool is the defect this exists to fix.
/// <c>HasProofPack: false</c> was hardcoded on the agent scoring path, which collapsed
/// <i>we looked and there was none</i> — a fact about the episode — into
/// <i>there was nowhere to look</i>, a fact about the product. The scorecard then made a statement
/// about the agent when the true statement was about a missing channel. Reintroducing a bool here
/// would rebuild that collapse one layer down.
/// </remarks>
public enum ProofPackVerdict
{
    /// <summary>The declared path is a real committed Proof Pack inside the session's repository.</summary>
    Verified,

    /// <summary>We could look, and it is not there — or it is not a Proof Pack path at all.</summary>
    NotFound,

    /// <summary>We could not look. The repository is not reachable from here, so nothing is claimed.</summary>
    Unverifiable,
}

/// <summary>
/// Checks whether a path an agent declared is really a committed Proof Pack in its repository.
/// </summary>
/// <remarks>
/// <para><b>Why this makes agent-declared evidence admissible.</b> The owner's decision is that the
/// watcher <i>derives</i> — it observes rather than accepts testimony, which is why an
/// <c>episode-close</c> carrying its own <c>acceptance_met</c> stays refused. A declared path is
/// different in exactly the way that matters: <b>the agent names a file and the product checks
/// whether the file is there</b>. The agent cannot make the check pass by asserting harder. That is
/// an observation about a claim, not a claim accepted.</para>
///
/// <para><b>Why not simply scan the repository for Proof Packs.</b> Because nothing links one to an
/// episode, and crediting an episode with any <c>docs/proof/</c> file found in the tree would
/// fabricate <i>presence</i> — an agent scored for someone else's evidence. That is strictly worse
/// than the bug being fixed: today's value is an honest zero about the wrong subject, where that
/// would be a wrong number that looks like a right one.</para>
///
/// <para><b>The containment check is a security boundary, not tidiness.</b> The declared path
/// arrives from outside the product, verbatim and uninspected by the ingest half — absolute paths,
/// traversal, and escaping are all recorded exactly as sent, deliberately, so that this layer
/// decides what is true. A path escaping the repository would let a session point at another
/// repository's evidence, or at any file on the machine whose existence then becomes a score.</para>
///
/// <para><b>NO PRODUCTION CALLER ON THIS BRANCH.</b> Stated rather than left to be found (DC-089).
/// The caller is <see cref="ClosedEpisodeScoring"/>, which will read declared artifacts from the
/// store once the contract half lands, and it is deliberately not written yet because the store
/// method it needs does not exist here. This claim is a negative and negatives decay when someone
/// else acts (DC-094), so it is tied to something that fails: the day
/// <c>ClosedEpisodeScoring</c> calls this, <c>WhatDaydreamSeesInAnAgentEpisodeTests</c> goes red,
/// because an evidenced episode stops being unremarkable.</para>
/// </remarks>
public static class ProofPackVerifier
{
    /// <summary>The committed location a Proof Pack lives in, matching the audit-log convention.</summary>
    /// <remarks>
    /// The same substring <c>AuditLogEpisodeSource</c> looks for in an audit entry's artifacts, so
    /// the two evidence paths agree on what a Proof Pack IS. Two definitions of that would let an
    /// episode be evidenced on one path and unevidenced on the other.
    /// </remarks>
    public const string ProofDirectory = "docs/proof/";

    /// <summary>
    /// The verdict for one declared path, relative to the repository the session is bound to.
    /// </summary>
    /// <param name="repositoryRoot">
    /// The session's repository — the corrected one, so a worktree-registered agent is checked
    /// against the repository its evidence is actually committed in.
    /// </param>
    /// <param name="declaredPath">The path exactly as the agent sent it, unmodified by the ingest.</param>
    public static ProofPackVerdict Verify(string? repositoryRoot, string? declaredPath)
    {
        // No repository we can reach means we cannot look. Saying NotFound here would be the
        // hardcoded false all over again, in the one case where the product is the thing at fault.
        if (string.IsNullOrWhiteSpace(repositoryRoot) || !Directory.Exists(repositoryRoot))
        {
            return ProofPackVerdict.Unverifiable;
        }

        if (string.IsNullOrWhiteSpace(declaredPath))
        {
            return ProofPackVerdict.NotFound;
        }

        try
        {
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot));

            // Combine handles the relative case and RETURNS THE SECOND ARGUMENT UNCHANGED when it is
            // rooted — which is why containment is checked below rather than assumed from the join.
            // An absolute declared path lands wherever it points, and must then be rejected on its
            // own merits, not silently reinterpreted as relative.
            var full = Path.GetFullPath(Path.Combine(root, declaredPath));

            if (!IsInside(root, full))
            {
                return ProofPackVerdict.NotFound;
            }

            // Compared against the repository-relative portion, so a repository that merely happens
            // to live under a directory called docs/proof does not make every file in it evidence.
            var relative = full[root.Length..].TrimStart('\\', '/').Replace('\\', '/');

            if (!relative.StartsWith(ProofDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return ProofPackVerdict.NotFound;
            }

            // A directory is not a Proof Pack, and File.Exists is false for one, so this also rejects
            // a declaration pointing at the docs/proof folder itself.
            return File.Exists(full) ? ProofPackVerdict.Verified : ProofPackVerdict.NotFound;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException
            or NotSupportedException or UnauthorizedAccessException or PathTooLongException)
        {
            // A path the filesystem refuses to even evaluate is not evidence, and it is not our
            // inability to look either — the agent sent something unusable.
            return ProofPackVerdict.NotFound;
        }
    }

    /// <summary>
    /// Whether <paramref name="candidate"/> is contained by <paramref name="root"/>.
    /// </summary>
    /// <remarks>
    /// <para>The separator is appended before comparing, because a plain prefix test says
    /// <c>C:\repos\app-other</c> is inside <c>C:\repos\app</c> — a neighbouring repository admitted
    /// as this one's evidence, which is the containment failure that matters most here.</para>
    ///
    /// <para>Case-insensitive only on Windows, matching <c>RepositoryIdentity.Canonicalise</c>: POSIX
    /// paths are case-sensitive and folding there would admit a genuinely different directory.</para>
    /// </remarks>
    private static bool IsInside(string root, string candidate)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(root, candidate, comparison))
        {
            return false;
        }

        return candidate.StartsWith(root + Path.DirectorySeparatorChar, comparison);
    }
}
