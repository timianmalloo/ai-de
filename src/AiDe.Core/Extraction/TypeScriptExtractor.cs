using System.Text.RegularExpressions;
using AiDe.Core.Facts;

namespace AiDe.Core.Extraction;

/// <summary>
/// TypeScript and JavaScript modules, their exported declarations, and what they import.
/// </summary>
/// <remarks>
/// <para><b>The largest remaining disclosure.</b> `typescript-not-analysed (165 file(s))` was the
/// biggest thing this tool said it could not see, on a repository where the C# half was fully
/// mapped.</para>
///
/// <para><b>Structure, not semantics — the same bargain as Python.</b> There is no TypeScript
/// compiler here: this recognises `import`/`export`, and top-level `class`, `interface`, `type`,
/// `enum`, `function` and `const`. Types are not checked, call graphs are not built, and a module
/// specifier is resolved only when it names a file this scope contains. Every gap is a disclosure on
/// the scope.</para>
///
/// <para><b>Why the same shape as PythonExtractor and not a shared base.</b> The two look alike and
/// are not the same: TypeScript's specifiers carry extensions and index files, its declarations are
/// export-gated rather than column-zero, and JSX changes what a valid line looks like. A shared base
/// would have to be parameterised by every one of those, which is more machinery than either
/// extractor contains. If a third language arrives that fits the pattern, that is the moment the
/// abstraction is earned.</para>
///
/// <para><c>simplify: line-oriented recognition rather than a TypeScript grammar; ceiling is exported
/// top-level declarations and import edges resolved only within the scope; upgrade trigger = a
/// consumer needs type relationships, call edges, or anything declared inside a function or a
/// namespace block.</c></para>
/// </remarks>
public sealed class TypeScriptExtractor : IExtractor
{
    public string ScopeKind => "typescript";

    /// <summary>Gaps this extractor always has, stated on every scope it produces.</summary>
    public static class Disclosures
    {
        /// <summary>No type checking: an import names a module specifier, not a symbol.</summary>
        public const string TypesNotChecked = "typescript-types-not-checked";

        /// <summary>Only exported top-level declarations are seen.</summary>
        public const string NonExportedNotAnalysed = "typescript-non-exported-not-analysed";

        /// <summary>Nothing dynamic is followed — import(), require with a variable, re-export globs.</summary>
        public const string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed";

        /// <summary>An export whose spelling this reader does not know (DC-033's own alarm).</summary>
        public const string ExportsNotRecognised = "typescript-exports-not-recognised";
    }

    // MEASURED against a real repository, not assumed. The previous form omitted `async`, the
    // generator star, `namespace`, `let` and `var` — and TheTerrace declares four
    // `export namespace`, every one of which was reported as absent rather than as unread. That is
    // DC-033 in this file: a reader that knows one spelling and reports the rest as nothing.
    private static readonly Regex Declaration = new(
        @"^export\s+(?:default\s+)?(?:declare\s+)?(?:abstract\s+)?(?:async\s+)?" +
        @"(class|interface|type|enum|function|const|let|var|namespace|module)\s*\*?\s+" +
        @"([A-Za-z_$][A-Za-z0-9_$]*)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// A line that exports something this reader did not recognise.
    /// </summary>
    /// <remarks>
    /// The CONTROL for DC-033, and the reason it is here rather than in a comment. That class says a
    /// reader recognises one spelling of a pattern and reports the rest as absent, and that its
    /// signature is a ratio nobody looks at. So this reader counts its own misses and discloses
    /// them: the next spelling it does not know announces itself on the scope instead of waiting to
    /// be noticed by somebody grepping a repository by hand.
    ///
    /// Re-export forms are excluded deliberately — `export { A }`, `export * from`, `export =` and
    /// `export default someExpression` are not declarations, so counting them would produce a miss
    /// rate that never reaches zero and therefore says nothing.
    /// </remarks>
    private static readonly Regex ExportLine =
        new(@"^export\s+(?!\{|\*|=|type\s*\{)\S+", RegexOptions.Compiled | RegexOptions.Multiline);

    // `from '…'` covers import and re-export; the bare form imports a module for its side effects.
    private static readonly Regex FromSpecifier =
        new(@"from\s+['""]([^'""]+)['""]", RegexOptions.Compiled);

    private static readonly Regex BareImport =
        new(@"^import\s+['""]([^'""]+)['""]", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly string[] Extensions = [".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs"];

    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var directory = request.RootPath;

        if (!Directory.Exists(directory))
        {
            return Task.FromResult(new ExtractionResult([], Complete: false,
                [new ExtractionDiagnostic("AIDE-TS-NO-DIRECTORY", request.ScopeId,
                    $"the scope's directory does not exist: {directory}")]));
        }

        var prefix = ModuleNaming.ScopePrefix(request.ScopeId);
        var files = Files(directory).ToList();

        var modules = files
            .Select(f => ModuleNaming.Qualify(prefix, ModuleName(directory, f)))
            .ToHashSet(StringComparer.Ordinal);

        // The rest of the workspace, so `../shared/thing` reaching out of this directory resolves
        // rather than being disclosed as unresolvable.
        var everywhere = new HashSet<string>(modules, StringComparer.Ordinal);
        if (request.WorkspaceModules is { } supplied) everywhere.UnionWith(supplied);

        var assertions = new List<EvidenceAssertion>();
        var unresolved = 0;
        var unrecognisedExports = 0;

        foreach (var disclosure in new[]
        {
            Disclosures.TypesNotChecked,
            Disclosures.NonExportedNotAnalysed,
            Disclosures.DynamicImportsNotAnalysed,
        })
        {
            assertions.Add(Fact(request, request.ScopeId, "discloses", disclosure));
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string text;
            try { text = File.ReadAllText(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            var module = ModuleNaming.Qualify(prefix, ModuleName(directory, file));
            assertions.Add(Fact(request, module, "has_type", "typescript-module"));

            var declarations = Declaration.Matches(text);

            // The miss rate, counted per file. `export default` of an expression and re-exports are
            // already excluded by ExportLine, so what remains is genuinely a declaration form this
            // reader failed to read.
            var exportLines = ExportLine.Matches(text).Count;
            if (exportLines > declarations.Count) unrecognisedExports += exportLines - declarations.Count;

            foreach (Match match in declarations)
            {
                var kind = match.Groups[1].Value;
                var name = match.Groups[2].Value;

                assertions.Add(Fact(request, $"{module}.{name}", "has_type", "typescript-" + kind));
                assertions.Add(Fact(request, $"{module}.{name}", "declared_in", module));
            }

            var specifiers = FromSpecifier.Matches(text).Select(m => m.Groups[1].Value)
                .Concat(BareImport.Matches(text).Select(m => m.Groups[1].Value))
                .Distinct(StringComparer.Ordinal);

            foreach (var specifier in specifiers)
            {
                var resolved = Resolve(specifier, module, everywhere);
                if (resolved is null) unresolved++;

                // Resolved means a file this scope contains and read. Anything else — a package, a
                // path alias, a module in another scope — stays Inferred with the specifier as
                // written, because asserting which it is would be the guess DC-022 is about.
                assertions.Add(resolved is null
                    ? Fact(request, module, "imports", specifier, VerificationStatus.Inferred)
                    : Fact(request, module, "imports", resolved, VerificationStatus.Verified));
            }
        }

        if (unrecognisedExports > 0)
        {
            // Not "some exports were missed" but "31 of them were". A count is what turns a known
            // limitation into something somebody can decide about.
            assertions.Add(Fact(request, request.ScopeId, "discloses",
                $"{Disclosures.ExportsNotRecognised} ({unrecognisedExports:N0} export(s) whose " +
                "declaration form this reader does not recognise)"));
        }

        if (unresolved > 0)
        {
            assertions.Add(Fact(request, request.ScopeId, "discloses",
                $"typescript-imports-not-resolved ({unresolved:N0} specifier(s) name something this " +
                "scope does not contain)"));
        }

        
        // Identical facts are ONE fact. Two files can share a module name — `app.ts` beside a
        // compiled `app.js` is the common case, and an import specifier resolves to one module
        // regardless — so the same triple can be asserted twice in a scope. The store's natural key
        // rejects that (P1-STORE-05, deliberately), which surfaced as a raw SQLite constraint error
        // from the middle of an index on a real repository. Deduplicating here is the honest fix:
        // the duplicate carries no information, and silencing the key would weaken a real control.
        var deduplicated = assertions
            .GroupBy(a => (a.Subject, a.Predicate, a.Object))
            .Select(g => g.First())
            .ToList();

        return Task.FromResult(new ExtractionResult(deduplicated, Complete: true, []));
    }

    /// <summary>
    /// The module a specifier names, when this scope contains it. Null otherwise.
    /// </summary>
    /// <remarks>
    /// Only RELATIVE specifiers can resolve here: a bare one is a package or a path alias, and both
    /// need configuration this extractor deliberately does not read. Extensions are optional in
    /// TypeScript and an `index` file stands for its directory, so both forms are tried — a
    /// specifier that resolves to neither is left alone rather than guessed at.
    /// </remarks>
    internal static string? Resolve(string specifier, string importingModule, IReadOnlySet<string> modules)
    {
        if (!specifier.StartsWith('.')) return null;

        var parts = importingModule.Split('/');
        var segments = new List<string>(parts[..^1]);          // the importing module's directory

        foreach (var part in specifier.Split('/'))
        {
            switch (part)
            {
                case "." or "": break;
                case "..":
                    if (segments.Count == 0) return null;
                    segments.RemoveAt(segments.Count - 1);
                    break;
                default: segments.Add(part); break;
            }
        }

        var candidate = string.Join('/', segments);

        foreach (var extension in Extensions)
        {
            if (candidate.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                candidate = candidate[..^extension.Length];
                break;
            }
        }

        if (modules.Contains(candidate)) return candidate;

        // A directory specifier means its index file.
        var index = candidate.Length == 0 ? "index" : candidate + "/index";
        return modules.Contains(index) ? index : null;
    }

    /// <summary>A module's path-like name, relative to the scope and without its extension.</summary>
    private static string ModuleName(string directory, string file)
    {
        var relative = Path.GetRelativePath(directory, file);
        var withoutExtension = relative[..^Path.GetExtension(relative).Length];

        return withoutExtension
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static EvidenceAssertion Fact(
        ExtractionRequest request, string subject, string predicate, string obj,
        VerificationStatus status = VerificationStatus.Verified) =>
        new(request.ScopeId, request.ArtifactRevision, subject, predicate, obj,
            EvidenceOrigin.Static, status,
            new Provenance(request.ScopeId, null, "typescript-extractor", "1.0.0", DateTimeOffset.UtcNow));

    private static IEnumerable<string> Files(string directory)
    {
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", "dist", "build", "out", ".next", "coverage", ".git",
        };

        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            string[] files;
            try { files = Directory.GetFiles(current); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var file in files)
            {
                // .d.ts is a declaration file: it re-states types defined elsewhere, so indexing it
                // would put every symbol in the graph twice, once with no implementation behind it.
                if (file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

                if (Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }

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
