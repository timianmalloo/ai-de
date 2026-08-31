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
/// <para><b>The body is now read, for exactly one thing.</b> Until this landed the reader saw only
/// frontmatter and disclosed <c>knowledge-body-not-analysed</c> on every scope — 877 documents on
/// TheTerrace present in the graph as their own metadata. A markdown hyperlink to another document
/// is the one thing in a body that is a DECLARATION: the author wrote a path, and the path either
/// resolves to a file this scope read or it does not. Everything else a body contains was measured
/// and left unread on purpose; <see cref="KnowledgeBody"/> carries the numbers and the reasons, and
/// each one is disclosed with a count rather than skipped in silence.</para>
///
/// <para><b>Nothing here is matched by resemblance.</b> The user's decision of 2026-08-30 — <em>"do
/// not infer, the graph should only be on observable links/relationships"</em> — rules out reading
/// prose for names that look like code or like a document id. Measured against it: 26,924 inline
/// code spans in TheTerrace's documents match zero C# node ids exactly, and the knowledge-id variant
/// that does fire is wrong often enough to reject (<c>`architecture`</c> names an MCP tool in 4 of
/// its 5 occurrences in this repository). A link is different in kind: <c>[x](../y.md)</c> has one
/// reading.</para>
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
    private const string ExtractorVersion = "1.1.0";

    /// <summary>
    /// What this reader does not see, stated on the scope with a count.
    /// </summary>
    /// <remarks>
    /// <para><b>Every one of these is conditional.</b> The disclosure they replaced —
    /// <c>knowledge-body-not-analysed</c> — fired on every scope whether or not anything had been
    /// hidden, and it would now be false on any scope whose prose links resolve. That is the exact
    /// shape the Python reader had to correct: <em>"a blanket 'imports are not resolved' was true
    /// when none were, and became a closed gap reported as open the moment resolution landed"</em>.
    /// A disclosure that cannot be absent teaches a reader to stop reading disclosures.</para>
    ///
    /// <para><b>Boundaries and gaps are kept apart</b> (DC-050). What this product declines to read
    /// — headings, glossary terms, backticked identifiers, a link out of this scope — is a statement
    /// about scope. A link naming a markdown file that is not there is a defect IN THE DOCUMENT, and
    /// merging the two would bury the second inside the first.</para>
    /// </remarks>
    public static class Disclosures
    {
        /// <summary>A markdown file with frontmatter but no id cannot be a node.</summary>
        public const string ArtifactsWithoutIds = "knowledge-artifacts-without-ids";

        /// <summary>GAP: a prose link names a markdown file that is not in this scope's tree.</summary>
        public const string LinkTargetMissing = "knowledge-prose-link-target-missing";

        /// <summary>BOUNDARY: a prose link resolves to a markdown file that declares no id.</summary>
        public const string LinkTargetNotANode = "knowledge-prose-link-target-not-a-node";

        /// <summary>BOUNDARY: a prose link points above this scope's root, where it cannot look.</summary>
        public const string LinkTargetOutsideScope = "knowledge-prose-link-target-outside-scope";

        /// <summary>BOUNDARY: a document's structure is counted, not extracted.</summary>
        public const string HeadingsNotAnalysed = "knowledge-headings-not-analysed";

        /// <summary>BOUNDARY: a glossary's term definitions are counted as documents, not read.</summary>
        public const string GlossaryTermsNotAnalysed = "knowledge-glossary-terms-not-analysed";

        /// <summary>BOUNDARY: backticked identifiers are not matched against anything.</summary>
        public const string InlineCodeNotResolved = "knowledge-inline-code-not-resolved";
    }

    /// <summary>One document, read once, held until every other document's id is known.</summary>
    private sealed record Read(
        string File, string Relative, KnowledgeRecord Record, KnowledgeBodySurvey Body);

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

        var withoutIds = 0;
        var headings = 0;
        var documentsWithHeadings = 0;
        var codeSpans = 0;
        var glossaries = 0;

        // Every document in this scope, read ONCE and held. A link's target cannot be resolved until
        // every id in the scope is known, and a link may name a document the walk has not reached
        // yet — resolution that depends on file order is resolution that is wrong half the time.
        // The Python reader collects its module names first for exactly this reason; the difference
        // is that a document's id lives inside the file, so the collecting pass IS the reading pass.
        var read = new List<Read>();

        // The FILE is the key, not the id: a link names a path, and a path is the only thing that
        // tells two documents apart before their ids are known. OrdinalIgnoreCase because these
        // paths came off a Windows filesystem, where `../ADR/0001.md` and `../adr/0001.md` are one
        // file and a case-exact lookup would silently miss one of them.
        var byPath = new Dictionary<string, KnowledgeRecord>(StringComparer.OrdinalIgnoreCase);

        // Every markdown file in the scope, id or not. `byPath` answers "is this a node"; this
        // answers "is this file even there" — and the difference between the two is the difference
        // between a document that declined to join the graph and a cross-reference that is broken.
        var markdown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var root = Path.GetFullPath(directory);

        foreach (var file in Files(directory).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            markdown.Add(Path.GetFullPath(file));

            string[] lines;
            try { lines = File.ReadAllLines(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            var record = KnowledgeFrontmatter.Read(lines, out var missingId);

            if (missingId) withoutIds++;
            if (record is null) continue;

            var body = KnowledgeBody.Survey(lines, Path.GetFileName(file), record.Type);

            headings += body.Headings;
            if (body.Headings > 0) documentsWithHeadings++;
            codeSpans += body.InlineCodeSpans;
            if (body.DeclaresItselfAGlossary) glossaries++;

            var relative = Path.GetRelativePath(directory, file).Replace((char)92, '/');

            read.Add(new Read(Path.GetFullPath(file), relative, record, body));
            byPath[Path.GetFullPath(file)] = record;
        }

        var linksMissing = 0;
        var linksNotANode = 0;
        var linksOutsideScope = 0;

        foreach (var document in read)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = document.Record;

            Provenance Where(int line) =>
                new(document.Relative, $"{line}:1", ExtractorId, ExtractorVersion, observedAt);

            // A document without a declared type is a node whose KIND is unknown, which is different
            // from one that is untyped by design — so the fact exists and carries Unverified rather
            // than being omitted.
            assertions.Add(Fact(
                request, record.Id, "has_type", record.Type ?? "unknown",
                record.Type is null ? VerificationStatus.Unverified : VerificationStatus.Verified,
                Where(2)));

            assertions.Add(Fact(
                request, record.Id, "declared_in", request.ScopeId, VerificationStatus.Verified, Where(2)));

            // The PRODUCER says this is knowledge. Nothing downstream should have to infer it from a
            // type name or a scope id: `has_type` is emitted by six extractors and says nothing about
            // which half of the graph a node is in, and inferring it was INV-0004's root cause.
            assertions.Add(Fact(
                request, record.Id, "node_class", "knowledge", VerificationStatus.Verified, Where(2)));

            // Owner names a person, so it stays workspace-local and never reaches telemetry.
            if (!string.IsNullOrEmpty(record.Owner))
            {
                assertions.Add(Fact(
                    request, record.Id, "owned_by", record.Owner, VerificationStatus.Verified, Where(3)));
            }

            // A review date is a FACT ABOUT the document, so it is an attribute rather than an edge:
            // drawing it would put a date in the graph as a thing to navigate to.
            if (!string.IsNullOrEmpty(record.ReviewBy))
            {
                assertions.Add(Fact(
                    request, record.Id, "review_by", record.ReviewBy, VerificationStatus.Verified, Where(3)));
            }

            foreach (var (to, rel) in record.Links)
            {
                assertions.Add(Fact(request, record.Id, rel, to, VerificationStatus.Verified, Where(4)));
            }

            var declared = record.Links.Select(l => l.To).ToHashSet(StringComparer.Ordinal);

            foreach (var link in document.Body.MarkdownLinks)
            {
                var target = Resolve(document.File, link.Target, root);

                if (target is null)
                {
                    // Not a relative markdown path inside this scope. A URL, an .html page, or a
                    // path that climbs above the scope's root — the last of which may well exist and
                    // be a node in a wider scope, which this reader has no way to know and will not
                    // guess at by stat-ing paths outside the tree it was given.
                    if (IsMarkdownReference(link.Target)) linksOutsideScope++;
                    continue;
                }

                if (!byPath.TryGetValue(target, out var destination))
                {
                    // Two different statements, kept apart on purpose. A file that is not there is a
                    // broken cross-reference — a defect in the document. A file that is there and
                    // declares no id is this product's boundary: it indexes documents that opt in.
                    if (markdown.Contains(target)) linksNotANode++;
                    else linksMissing++;
                    continue;
                }

                // A document linking to itself is a table of contents, not a relationship.
                if (string.Equals(destination.Id, record.Id, StringComparison.Ordinal)) continue;

                // Already an EDGE, and a better one. 81 of the 128 resolving prose links on
                // TheTerrace name a document the frontmatter already links with a TYPED relation
                // (`refines`, `depends-on`); an untyped second edge between the same pair says
                // nothing the graph does not carry, and doubles the pair's weight in every view
                // that counts edges.
                if (declared.Contains(destination.Id)) continue;

                // VERIFIED, and it is worth being precise about why. Two things were observed rather
                // than inferred: the author wrote this href in this document, and the path it names
                // is a file this reader opened and found an id in. The predicate is deliberately not
                // one of the frontmatter relation names — a hyperlink does not say WHY, and
                // borrowing `relates-to` would make an untyped mention indistinguishable from a
                // declared relation (DC-022: a predicate is a name, and names collide).
                assertions.Add(Fact(
                    request, record.Id, "links_to", destination.Id,
                    VerificationStatus.Verified, Where(link.Line)));
            }
        }

        if (withoutIds > 0)
        {
            // Counted, because a document that MEANT to join the graph and cannot is a defect in
            // that document — distinct from an ordinary markdown file that was never a node.
            Disclose($"{Disclosures.ArtifactsWithoutIds} ({withoutIds:N0} file(s) have frontmatter but no id)");
        }

        if (linksMissing > 0)
        {
            // A GAP, and the only one here. MEASURED on TheTerrace, on the scope that holds all 877
            // documents: 109 of its 237 prose links name a markdown file that is not there —
            // cross-references that rotted when a document moved, and nothing had ever said so.
            Disclose($"{Disclosures.LinkTargetMissing} ({linksMissing:N0} prose link(s) name a " +
                     "markdown file that is not in this scope)");
        }

        if (linksNotANode > 0)
        {
            // Fires on NEITHER corpus today, and that is a measurement rather than an assumption:
            // the 19 candidates in this repository all name `../../spikes/*/RESULT.md`, which the
            // boundary above counts instead. Kept because a link to a sibling README inside the same
            // tree is one commit away, and proved by fixture rather than by corpus (DC-016).
            Disclose($"{Disclosures.LinkTargetNotANode} ({linksNotANode:N0} prose link(s) resolve to " +
                     "a markdown file that declares no id, so there is nothing to link to)");
        }

        if (linksOutsideScope > 0)
        {
            Disclose($"{Disclosures.LinkTargetOutsideScope} ({linksOutsideScope:N0} prose link(s) " +
                     "point above this scope's directory)");
        }

        if (headings > 0)
        {
            // Counted rather than read, and the count IS the point: "this reader does not extract
            // headings" is a sentence, and "4,471 headings in 375 documents" is the size of the
            // decision. The reasoning is in KnowledgeBody's remarks — the body text already travels
            // whole through node content, and an attribute per heading would push a document's own
            // relations out of its node card.
            Disclose($"{Disclosures.HeadingsNotAnalysed} ({headings:N0} heading(s) in " +
                     $"{documentsWithHeadings:N0} document(s); the body text itself is served whole " +
                     "by node content)");
        }

        if (glossaries > 0)
        {
            Disclose($"{Disclosures.GlossaryTermsNotAnalysed} ({glossaries:N0} document(s) declare " +
                     "themselves a glossary; the terms they define are not read)");
        }

        if (codeSpans > 0)
        {
            // The no-inference decision of 2026-08-30, stated as a number on every scope. Nothing is
            // matched against these: measured on TheTerrace, none of 26,924 exactly names a code
            // node, and matching by resemblance is what produced 7,426 false Verified edges once
            // already.
            Disclose($"{Disclosures.InlineCodeNotResolved} ({codeSpans:N0} inline code span(s) are " +
                     "not matched against code symbols)");
        }

        return Task.FromResult(new ExtractionResult(
            ExtractionFacts.Distinct(assertions), Complete: true, []));

        void Disclose(string text) => assertions.Add(Fact(
            request, scopeNode, CSharpExtractor.DisclosurePredicate, text,
            VerificationStatus.Verified, scopeProvenance));
    }

    /// <summary>Whether a link target is a relative markdown path at all.</summary>
    /// <remarks>
    /// Separated from <see cref="Resolve"/> so the "outside this scope" count means what it says. A
    /// URL and a path that climbs out of the tree both fail to resolve; only the second is something
    /// this reader would have read if it could, and counting the 329 http links on TheTerrace among
    /// them would make the number describe something that does not exist (DC-050).
    /// </remarks>
    private static bool IsMarkdownReference(string target) =>
        target.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        && !target.StartsWith("//", StringComparison.Ordinal)
        && !Uri.IsWellFormedUriString(target, UriKind.Absolute);

    /// <summary>
    /// The full path a link names, or null when it is not a markdown file inside this scope.
    /// </summary>
    /// <remarks>
    /// <para>Only <c>.md</c> targets. A link to an <c>.html</c> page or a <c>.cs</c> file names
    /// something this graph has no node for — 31 such links on TheTerrace, 30 of them to HTML — and
    /// resolving one would mean inventing a node kind to point at.</para>
    ///
    /// <para>The escape check is on the RESOLVED path, so <c>../../../x.md</c> is refused by where
    /// it lands rather than by how it is spelled.</para>
    /// </remarks>
    internal static string? Resolve(string fromFile, string target, string scopeRoot)
    {
        if (!IsMarkdownReference(target)) return null;

        // A root-absolute path is not relative to this file, and Path.Combine would resolve it
        // against the drive rather than the document.
        if (target.StartsWith('/') || target.StartsWith('\\')) return null;

        string full;
        try
        {
            full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fromFile)!, target));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // A target carrying characters no path can hold is not a link to anything here.
            return null;
        }

        // Inside the scope, or nothing. The trailing separator matters: without it a sibling
        // directory called `docs-old` passes the prefix test against a scope rooted at `docs`.
        var bounded = scopeRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return full.StartsWith(bounded, StringComparison.OrdinalIgnoreCase) ? full : null;
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
