using AiDe.Core.Facts;

namespace AiDe.Core.Extraction;

/// <summary>
/// The knowledge graph: documents that declare an identity, a kind and typed links.
/// </summary>
/// <remarks>
/// <para><b>Reported by the user: the graph showed knowledge as ZERO and code as a large count.</b>
/// The reason was not that repositories have no knowledge — it is that nothing ever looked. A reader
/// for these documents has existed since Phase 1 inside the fixture extractor, with tests, and
/// scope discovery produced six kinds of scope (<c>csharp</c>, <c>bicep</c>, <c>schema</c>,
/// <c>python</c>, <c>typescript</c>, <c>sql</c>) and no knowledge scope at all. The capability was
/// real, tested, and unreachable on any real repository.</para>
///
/// <para><b>A zero that means "nobody looked" reads as "there is none".</b> That is the shape this
/// product exists to avoid, and it was in the product's own headline surface — on a repository whose
/// entire premise is that <em>docs hold intent, code holds reality, and the expensive defects live
/// in the gap</em>. Half of that sentence was never being read.</para>
///
/// <para><b>Why not point the fixture extractor at the repository instead.</b> It enumerates
/// <c>*</c> recursively with no exclusions — pointed at a real checkout it would walk
/// <c>node_modules</c>, <c>bin</c> and <c>.git</c>. It also stamps <c>fixture-extractor</c> into
/// provenance, which would be a lie on a real document. The parsing is shared
/// (<see cref="KnowledgeFrontmatter"/>); only the walking and the identity differ.</para>
/// </remarks>
public sealed class KnowledgeExtractor : IExtractor
{
    public string ScopeKind => "knowledge";

    private const string ExtractorId = "knowledge-extractor";
    private const string ExtractorVersion = "1.0.0";

    /// <summary>Gaps this reader always has, stated on every scope it produces.</summary>
    public static class Disclosures
    {
        /// <summary>Prose is not read — only the frontmatter that declares graph structure.</summary>
        public const string BodyNotAnalysed = "knowledge-body-not-analysed";

        /// <summary>A markdown file with frontmatter but no id cannot be a node.</summary>
        public const string ArtifactsWithoutIds = "knowledge-artifacts-without-ids";
    }

    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var directory = request.RootPath;

        if (!Directory.Exists(directory))
        {
            return Task.FromResult(new ExtractionResult([], Complete: false,
                [new ExtractionDiagnostic("AIDE-KB-NO-DIRECTORY", request.ScopeId,
                    $"the scope's directory does not exist: {directory}")]));
        }

        var observedAt = DateTimeOffset.UtcNow;
        var assertions = new List<EvidenceAssertion>();
        var scopeNode = CSharpExtractor.ScopeNodeId(request.ScopeId);

        var scopeProvenance = new Provenance(
            Path.GetFileName(directory), "1:1", ExtractorId, ExtractorVersion, observedAt);

        assertions.Add(Fact(
            request, scopeNode, CSharpExtractor.DisclosurePredicate,
            Disclosures.BodyNotAnalysed, VerificationStatus.Verified, scopeProvenance));

        var withoutIds = 0;

        foreach (var file in Files(directory).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string[] lines;
            try { lines = File.ReadAllLines(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            var record = KnowledgeFrontmatter.Read(lines, out var missingId);

            if (missingId) withoutIds++;
            if (record is null) continue;

            var relative = Path.GetRelativePath(directory, file).Replace((char)92, '/');

            Provenance Where(int line) =>
                new(relative, $"{line}:1", ExtractorId, ExtractorVersion, observedAt);

            // A document without a declared type is a node whose KIND is unknown, which is different
            // from one that is untyped by design — so the fact exists and carries Unverified rather
            // than being omitted.
            assertions.Add(Fact(
                request, record.Id, "has_type", record.Type ?? "unknown",
                record.Type is null ? VerificationStatus.Unverified : VerificationStatus.Verified,
                Where(2)));

            assertions.Add(Fact(
                request, record.Id, "declared_in", request.ScopeId, VerificationStatus.Verified, Where(2)));

            // Owner names a person, so it stays workspace-local and never reaches telemetry.
            if (!string.IsNullOrEmpty(record.Owner))
            {
                assertions.Add(Fact(
                    request, record.Id, "owned_by", record.Owner, VerificationStatus.Verified, Where(3)));
            }

            foreach (var (to, rel) in record.Links)
            {
                assertions.Add(Fact(request, record.Id, rel, to, VerificationStatus.Verified, Where(4)));
            }
        }

        if (withoutIds > 0)
        {
            // Counted, because a document that MEANT to join the graph and cannot is a defect in
            // that document — distinct from an ordinary markdown file that was never a node.
            assertions.Add(Fact(
                request, scopeNode, CSharpExtractor.DisclosurePredicate,
                $"{Disclosures.ArtifactsWithoutIds} ({withoutIds:N0} file(s) have frontmatter but no id)",
                VerificationStatus.Verified, scopeProvenance));
        }

        return Task.FromResult(new ExtractionResult(
            ExtractionFacts.Distinct(assertions), Complete: true, []));
    }

    private static EvidenceAssertion Fact(
        ExtractionRequest request, string subject, string predicate, string obj,
        VerificationStatus status, Provenance provenance) =>
        new(request.ScopeId, request.ArtifactRevision, subject, predicate, obj,
            EvidenceOrigin.Static, status, provenance);

    /// <summary>Markdown directly under the scope, excluding vendored and generated trees.</summary>
    internal static IEnumerable<string> Files(string directory)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", "bin", "obj", ".git", ".vs", "dist", "build", "out",
            "__pycache__", ".venv", "venv", "packages", "artifacts",
        };

        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            string[] files;
            try { files = Directory.GetFiles(current, "*.md"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var file in files.Where(f => !IsTemplate(f))) yield return file;

            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(current); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var child in children)
            {
                if (!skip.Contains(Path.GetFileName(child))) pending.Push(child);
            }
        }
    }

    /// <summary>
    /// Whether a file is a TEMPLATE rather than an artifact.
    /// </summary>
    /// <remarks>
    /// A template carries frontmatter in exactly the shape a real document does, with placeholders
    /// where the values go. Indexing one puts a node in the graph that describes the shape of a
    /// document rather than anything in this repository — measured on this repo, seven of them.
    /// </remarks>
    private static bool IsTemplate(string file) =>
        Path.GetFileName(file).Contains(".template.", StringComparison.OrdinalIgnoreCase);
}
