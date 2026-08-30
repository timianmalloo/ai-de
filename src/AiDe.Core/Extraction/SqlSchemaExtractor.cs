using System.Text.RegularExpressions;
using AiDe.Core.Facts;

namespace AiDe.Core.Extraction;

/// <summary>
/// Tables declared in raw SQL, for repositories whose schema is not EF migrations.
/// </summary>
/// <remarks>
/// <para><b>Found by measuring a SECOND repository.</b> BioHacker declares its whole schema in
/// <c>src/Baseline.Sql/Schema/001-schema.sql</c> — eight <c>CREATE TABLE</c> statements in 197
/// lines — and the tool reported <c>sql-not-analysed (2 file(s))</c> and produced <b>zero</b> joins.
/// The disclosure was honest and the graph was still blind to the entire schema side of that
/// repository. Every measurement before it came from a codebase that happened to use EF.</para>
///
/// <para><b>Same node shape as <see cref="EfSchemaExtractor"/>, deliberately.</b> A table is
/// <c>table:Name</c> with <c>has_type table</c> and <c>has_column</c>, because the join projection
/// already reads that vocabulary — a second spelling for the same thing would be DC-022 with two
/// producers of one predicate, and the joins would silently see half the tables.</para>
///
/// <para><c>simplify: line-oriented recognition of CREATE TABLE and its column lines, not a SQL
/// grammar; ceiling is table names and column names from a plain DDL file; upgrade trigger = a
/// consumer needs types, constraints, indexes, or schema changes expressed as ALTER.</c></para>
/// </remarks>
public sealed class SqlSchemaExtractor : IExtractor
{
    public string ScopeKind => "sql";

    private const string ExtractorId = "sql-schema-extractor";
    private const string ExtractorVersion = "1.0.0";

    /// <summary>Gaps this reader always has, stated on every scope it produces.</summary>
    public static class Disclosures
    {
        /// <summary>Only CREATE TABLE is read; ALTER, DROP and RENAME are not folded.</summary>
        public const string AltersNotFolded = "sql-alter-statements-not-folded";

        /// <summary>Column types, constraints and indexes are not read.</summary>
        public const string ColumnDetailNotRead = "sql-column-detail-not-read";

        /// <summary>This is the schema the FILES declare, not what a server holds.</summary>
        public const string NotTheDatabase = "sql-schema-from-files-not-database";
    }

    // `CREATE TABLE [dbo].[Thing]`, `CREATE TABLE dbo.Thing`, `CREATE TABLE Thing` — and the
    // OR ALTER / IF NOT EXISTS preambles that real scripts carry.
    private static readonly Regex CreateTable = new(
        @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?<name>(?:\[[^\]]+\]|""[^""]+""|`[^`]+`|[A-Za-z_][\w$]*)"
        + @"(?:\s*\.\s*(?:\[[^\]]+\]|""[^""]+""|`[^`]+`|[A-Za-z_][\w$]*))*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // A column line inside the parentheses: a name, then a type word. Constraint lines start with a
    // keyword and are skipped rather than claimed as columns.
    private static readonly Regex ColumnLine = new(
        @"^\s*(?<name>\[[^\]]+\]|""[^""]+""|`[^`]+`|[A-Za-z_][\w$]*)\s+[A-Za-z]",
        RegexOptions.Compiled);

    private static readonly HashSet<string> NotAColumn = new(StringComparer.OrdinalIgnoreCase)
    {
        "CONSTRAINT", "PRIMARY", "FOREIGN", "UNIQUE", "CHECK", "INDEX", "KEY", "PERIOD", "WITH",
    };

    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var directory = request.RootPath;

        if (!Directory.Exists(directory))
        {
            return Task.FromResult(new ExtractionResult([], Complete: false,
                [new ExtractionDiagnostic("AIDE-SQL-NO-DIRECTORY", request.ScopeId,
                    $"the scope's directory does not exist: {directory}")]));
        }

        var observedAt = DateTimeOffset.UtcNow;
        var assertions = new List<EvidenceAssertion>();
        var scopeNode = CSharpExtractor.ScopeNodeId(request.ScopeId);

        var scopeProvenance = new Provenance(
            Path.GetFileName(directory), "1:1", ExtractorId, ExtractorVersion, observedAt);

        foreach (var disclosure in new[]
        {
            Disclosures.AltersNotFolded,
            Disclosures.ColumnDetailNotRead,
            Disclosures.NotTheDatabase,
        })
        {
            assertions.Add(Fact(request, scopeNode, CSharpExtractor.DisclosurePredicate, disclosure, scopeProvenance));
        }

        var unreadable = 0;

        foreach (var file in Files(directory).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string text;
            try { text = File.ReadAllText(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadable++;
                continue;
            }

            var relative = Path.GetRelativePath(directory, file).Replace((char)92, '/');

            foreach (var (table, columns, line) in Tables(text))
            {
                var node = $"table:{table}";
                var provenance = new Provenance(relative, $"{line}:1", ExtractorId, ExtractorVersion, observedAt);

                assertions.Add(Fact(request, node, "has_type", "table", provenance));
                assertions.Add(Fact(request, node, "declared_in", request.ScopeId, provenance));

                foreach (var column in columns)
                {
                    assertions.Add(Fact(request, node, "has_column", column, provenance));
                }
            }
        }

        if (unreadable > 0)
        {
            assertions.Add(Fact(request, scopeNode, CSharpExtractor.DisclosurePredicate,
                $"sql-source-unreadable ({unreadable:N0} file(s))", scopeProvenance));
        }

        // Identical facts are ONE fact: the same table can be created in two scripts (a baseline and
        // a rebuild), and the store's natural key rejects the duplicate rather than absorbing it.
        var deduplicated = assertions
            .GroupBy(a => (a.Subject, a.Predicate, a.Object))
            .Select(g => g.First())
            .ToList();

        return Task.FromResult(new ExtractionResult(deduplicated, Complete: true, []));
    }

    /// <summary>Every table a script creates, with the columns declared inside its parentheses.</summary>
    /// <remarks>
    /// The body is taken by matching parentheses from the first one after the name, so a nested
    /// <c>DECIMAL(9,2)</c> or a table-level <c>CHECK (...)</c> does not end the definition early —
    /// which a naive scan to the first <c>)</c> does, silently truncating the column list.
    /// </remarks>
    internal static IEnumerable<(string Table, IReadOnlyList<string> Columns, int Line)> Tables(string text)
    {
        foreach (Match match in CreateTable.Matches(text))
        {
            var name = Unquote(match.Groups["name"].Value);
            var line = text.Take(match.Index).Count(c => c == '\n') + 1;

            var open = text.IndexOf('(', match.Index + match.Length);
            if (open < 0)
            {
                yield return (name, [], line);
                continue;
            }

            var depth = 0;
            var close = -1;

            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')' && --depth == 0) { close = i; break; }
            }

            if (close < 0)
            {
                yield return (name, [], line);
                continue;
            }

            yield return (name, Columns(text[(open + 1)..close]), line);
        }
    }

    private static List<string> Columns(string body)
    {
        var columns = new List<string>();
        var depth = 0;
        var start = 0;

        // Split on commas at depth zero: a comma inside DECIMAL(9,2) does not separate columns.
        for (var i = 0; i <= body.Length; i++)
        {
            if (i < body.Length && body[i] == '(') depth++;
            else if (i < body.Length && body[i] == ')') depth--;

            if (i != body.Length && (body[i] != ',' || depth != 0)) continue;

            var piece = body[start..i];
            start = i + 1;

            var match = ColumnLine.Match(piece.TrimStart('\r', '\n'));
            if (!match.Success) continue;

            var name = Unquote(match.Groups["name"].Value);
            if (NotAColumn.Contains(name)) continue;

            columns.Add(name);
        }

        return columns;
    }

    /// <summary>
    /// The bare name, without brackets, quotes or a schema qualifier.
    /// </summary>
    /// <remarks>
    /// The schema prefix is dropped so <c>dbo.Principal</c> and <c>Principal</c> are one node. The
    /// EF reader emits unqualified names, and two spellings of one table would leave the joins
    /// matching half of them — the divergence being invisible because both look correct alone.
    /// </remarks>
    private static string Unquote(string raw)
    {
        var last = raw.Split('.')[^1].Trim();

        return last.Trim('[', ']', '"', '`', ' ');
    }

    private static EvidenceAssertion Fact(
        ExtractionRequest request, string subject, string predicate, string obj, Provenance provenance) =>
        new(request.ScopeId, request.ArtifactRevision, subject, predicate, obj,
            EvidenceOrigin.Static, VerificationStatus.Verified, provenance);

    private static IEnumerable<string> Files(string directory)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", "bin", "obj", ".git", "packages",
        };

        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            string[] files;
            try { files = Directory.GetFiles(current, "*.sql"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var file in files) yield return file;

            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(current); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var child in children)
            {
                if (!skip.Contains(Path.GetFileName(child))) pending.Push(child);
            }
        }
    }
}
