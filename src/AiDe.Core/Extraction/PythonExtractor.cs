using System.Text.RegularExpressions;
using AiDe.Core.Facts;

namespace AiDe.Core.Extraction;

/// <summary>
/// Python modules, their top-level declarations, and what they import.
/// </summary>
/// <remarks>
/// <para><b>Six repositories disclosed unread Python before this existed.</b> The disclosure was the
/// right behaviour and it is not a substitute for reading the code — a graph that says "there is
/// Python here and I cannot see it" is honest and still blind.</para>
///
/// <para><b>It reads structure, not semantics, and says so.</b> There is no Python compiler here:
/// this recognises module-level <c>import</c>, <c>from … import</c>, <c>class</c> and <c>def</c> at
/// column zero, and nothing else. Names are not resolved, so an import edge points at the module
/// PATH as written rather than at a symbol; a call graph is not attempted. Every one of those gaps
/// is a disclosure on the scope rather than a silence — the C# extractor's rule, applied to a
/// language where the gap is much wider.</para>
///
/// <para><b>Why not a real parser.</b> The Solution-Selection Ladder asks for the smallest thing that
/// is still correct, and correct here means "does not assert what it cannot see". A dependency on a
/// Python grammar would buy type resolution this product has nowhere to put yet, and would make the
/// extractor's reach a question about a third-party package's version. When call edges or resolved
/// imports are actually wanted, that is the upgrade trigger.</para>
///
/// <para><c>simplify: line-oriented recognition rather than a Python grammar; ceiling is top-level
/// declarations and import edges with unresolved targets; upgrade trigger = a consumer needs call
/// edges, resolved import targets, or anything nested inside a class or function.</c></para>
/// </remarks>
public sealed class PythonExtractor : IExtractor
{
    public string ScopeKind => "python";

    /// <summary>Gaps this extractor always has, stated on every scope it produces.</summary>
    public static class Disclosures
    {
        /// <summary>No name resolution: an import names a module path, not a symbol.</summary>
        public const string ImportsNotResolved = "python-imports-not-resolved";

        /// <summary>Only column-zero declarations are seen; nested ones are invisible.</summary>
        public const string NestedDeclarationsNotAnalysed = "python-nested-declarations-not-analysed";

        /// <summary>Nothing dynamic is followed — importlib, __import__, conditional imports.</summary>
        public const string DynamicImportsNotAnalysed = "python-dynamic-imports-not-analysed";
    }

    // Column zero on purpose. An indented `def` is a method or a closure, and claiming it as a
    // module-level function would put a symbol in the graph that no importer can reach.
    private static readonly Regex TopLevelClass =
        new(@"^class\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex TopLevelDef =
        new(@"^(?:async\s+)?def\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ImportModule =
        new(@"^import\s+([A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex FromImport =
        new(@"^from\s+([A-Za-z_.][A-Za-z0-9_.]*)\s+import\s", RegexOptions.Compiled | RegexOptions.Multiline);

    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // RootPath IS the scope's directory: discovery passes the scope's own path as the override,
        // the same way the Bicep extractor receives a template path rather than a repository root.
        // Deriving it from the scope id instead produced pkg\pkg and a scope that failed every run.
        var directory = request.RootPath;

        if (!Directory.Exists(directory))
        {
            return Task.FromResult(new ExtractionResult([], Complete: false,
                [new ExtractionDiagnostic("AIDE-PY-NO-DIRECTORY", request.ScopeId,
                    $"the scope's directory does not exist: {directory}")]));
        }

        var assertions = new List<EvidenceAssertion>();
        var unreadable = new List<string>();

        // Every module in THIS scope, so an import naming one of them can be resolved to it rather
        // than left as a string. Collected first because an import may name a module that appears
        // later in the walk, and resolution that depends on file order is resolution that is wrong
        // half the time.
        var modules = Files(directory)
            .Select(f => ModuleName(directory, f))
            .ToHashSet(StringComparer.Ordinal);

        // The gaps first, so a scope truncated later still carries what it cannot see. The same
        // ordering the C# extractor uses, and for the same reason.
        // ImportsNotResolved is NOT here: it is now conditional and carries a count, because some
        // imports resolve. A blanket "imports are not resolved" was true when none were and became a
        // closed gap reported as open the moment resolution landed — the same defect as hiding one.
        var unresolved = 0;

        foreach (var disclosure in new[]
        {
            Disclosures.NestedDeclarationsNotAnalysed,
            Disclosures.DynamicImportsNotAnalysed,
        })
        {
            assertions.Add(Fact(request, ScopeNode(request.ScopeId), "discloses", disclosure));
        }

        foreach (var file in Files(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string text;
            try { text = File.ReadAllText(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadable.Add(Path.GetFileName(file));
                continue;
            }

            var module = ModuleName(directory, file);
            assertions.Add(Fact(request, module, "has_type", "python-module"));

            foreach (var name in Names(TopLevelClass, text))
            {
                assertions.Add(Fact(request, $"{module}.{name}", "has_type", "python-class"));
                assertions.Add(Fact(request, $"{module}.{name}", "declared_in", module));
            }

            foreach (var name in Names(TopLevelDef, text))
            {
                assertions.Add(Fact(request, $"{module}.{name}", "has_type", "python-function"));
                assertions.Add(Fact(request, $"{module}.{name}", "declared_in", module));
            }

            // INFERRED, and labelled: the target is the module path as written. Whether it resolves
            // to a file in this repository, a package, or nothing at all is not established here,
            // and calling that Verified would be the exact defect DC-022 is about.
            foreach (var target in Names(ImportModule, text).Concat(Names(FromImport, text)).Distinct(StringComparer.Ordinal))
            {
                // A relative import (`from .models import X`) is resolved against the importing
                // module's package; an absolute one is matched against the scope's modules as
                // written. Anything that matches a module this scope actually contains becomes
                // VERIFIED — the target is a file that exists and was read.
                var resolved = Resolve(target, module, modules);

                if (resolved is null) unresolved++;

                assertions.Add(resolved is null
                    // Unresolved stays INFERRED and keeps the name as written: it may be a package,
                    // a module in another scope, or nothing. Asserting which would be the guess
                    // DC-022 is about.
                    ? Fact(request, module, "imports", target, VerificationStatus.Inferred)
                    : Fact(request, module, "imports", resolved, VerificationStatus.Verified));
            }
        }

        if (unresolved > 0)
        {
            // Counted, because "imports are not resolved" and "31 of 330 imports point outside this
            // scope" are different statements about how much of the graph is a guess.
            assertions.Add(Fact(request, ScopeNode(request.ScopeId), "discloses",
                $"{Disclosures.ImportsNotResolved} ({unresolved:N0} import(s) name something this " +
                "scope does not contain)"));
        }

        if (unreadable.Count > 0)
        {
            assertions.Add(Fact(request, ScopeNode(request.ScopeId), "discloses",
                $"python-source-unreadable ({unreadable.Count:N0} file(s))"));
        }

        // Complete: the disclosures are IN the snapshot rather than missing from it, so a scope that
        // read what it could is a whole answer about a narrow question.
        return Task.FromResult(new ExtractionResult(assertions, Complete: true, []));
    }

    /// <summary>
    /// The module this import names, when the scope contains it. Null otherwise.
    /// </summary>
    /// <remarks>
    /// Leading dots are Python's relative-import syntax: one dot is the importing module's own
    /// package, each further dot climbs one level. Resolved textually against the modules actually
    /// found, so a match means a file that exists and was read — which is what lets the edge be
    /// Verified rather than Inferred.
    /// </remarks>
    internal static string? Resolve(string target, string importingModule, IReadOnlySet<string> modules)
    {
        if (!target.StartsWith('.'))
        {
            return modules.Contains(target) ? target : null;
        }

        var levels = target.TakeWhile(c => c == '.').Count();
        var rest = target[levels..];

        // The importing module's package is its name minus the module itself; each extra dot climbs.
        var parts = importingModule.Split('.');
        var keep = parts.Length - levels;
        if (keep < 0) return null;

        var package = string.Join('.', parts.Take(keep));
        var candidate = string.IsNullOrEmpty(rest)
            ? package
            : (package.Length == 0 ? rest : package + "." + rest);

        return modules.Contains(candidate) ? candidate : null;
    }

    private static IEnumerable<string> Names(Regex pattern, string text) =>
        pattern.Matches(text).Select(m => m.Groups[1].Value).Where(n => n.Length > 0);

    /// <summary>A module's dotted name, from its path relative to the scope.</summary>
    private static string ModuleName(string directory, string file)
    {
        var relative = Path.GetRelativePath(directory, file);
        var withoutExtension = relative[..^Path.GetExtension(relative).Length];

        return withoutExtension
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');
    }

    private static string ScopeNode(string scopeId) => scopeId;

    private static EvidenceAssertion Fact(
        ExtractionRequest request, string subject, string predicate, string obj,
        VerificationStatus status = VerificationStatus.Verified) =>
        new(request.ScopeId, request.ArtifactRevision, subject, predicate, obj,
            EvidenceOrigin.Static, status,
            new Provenance(request.ScopeId, null, "python-extractor", "1.0.0", DateTimeOffset.UtcNow));

    /// <summary>Python files directly under the scope, and under its packages.</summary>
    private static IEnumerable<string> Files(string directory)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__pycache__", ".venv", "venv", ".tox", "node_modules", ".git", "build", "dist",
        };

        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            string[] files;
            try { files = Directory.GetFiles(current, "*.py"); }
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
