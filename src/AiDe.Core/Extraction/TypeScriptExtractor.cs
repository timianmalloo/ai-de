using System.Text.RegularExpressions;
using AiDe.Core.Facts;

namespace AiDe.Core.Extraction;

/// <summary>
/// TypeScript and JavaScript modules, their top-level declarations, and what they import.
/// </summary>
/// <remarks>
/// <para><b>The largest remaining disclosure.</b> `typescript-not-analysed (165 file(s))` was the
/// biggest thing this tool said it could not see, on a repository where the C# half was fully
/// mapped.</para>
///
/// <para><b>Structure, not semantics — the same bargain as Python.</b> There is no TypeScript
/// compiler here: this recognises `import`/`export` STATEMENTS, and column-zero `class`,
/// `interface`, `type`, `enum`, `function` and `namespace` whether or not they are exported. Types
/// are not checked, call graphs are not built, and a module specifier is resolved only when it names
/// a file this scope contains. Every gap is a disclosure on the scope.</para>
///
/// <para><b>Precision before volume — MEASURED, and the reason this reader was rewritten.</b> On
/// TheTerrace it produced 14 import edges and <b>not one of them described a dependency between two
/// things in the repository</b>: ten named text that is not a specifier at all (a sentence from an
/// audit log, bundled help text, two spans of compiled JavaScript, and one code-generation template
/// that read exactly like a real npm dependency), and the two Verified ones were a module importing
/// itself. The cause was a `from '…'` matcher with no anchor — the `uses_table` defect in another
/// reader, where a keyword matched anywhere in a string turned "we update the record" into a table
/// called `the`. <b>An extractor that asserts something false is worse than one that asserts
/// nothing</b>, because the false fact arrives labelled and gets believed.</para>
///
/// <para><b>What it will not read at all.</b> Build output (`bin`, `obj`, `artifacts`, `publish`),
/// and any file whose longest line says a machine wrote it. Both were measured: most of the 88
/// modules this reader produced were a vendored browser driver under a `bin/Debug/` tree, and every
/// invented specifier came out of a bundle or a generated data file. What is skipped is counted and
/// disclosed on the scope, because a skipped file is a boundary and a boundary needs a number.</para>
///
/// <para><b>Why the same shape as PythonExtractor and not a shared base.</b> The two look alike and
/// are not the same: TypeScript's specifiers carry extensions and index files, its imports are
/// statements that may span lines, and JSX changes what a valid line looks like. A shared base would
/// have to be parameterised by every one of those, which is more machinery than either extractor
/// contains. If a third language arrives that fits the pattern, that is the moment the abstraction is
/// earned.</para>
///
/// <para><c>simplify: line-oriented recognition rather than a TypeScript grammar; ceiling is
/// column-zero declarations and static import/export statements resolved only within the workspace,
/// with npm and Node's runtime counted rather than drawn; upgrade trigger = a consumer needs type
/// relationships, call edges, CommonJS `require`, tsconfig path aliases, or anything declared inside
/// a function or a namespace block.</c></para>
/// </remarks>
public sealed class TypeScriptExtractor : IExtractor
{
    public string ScopeKind => "typescript";

    /// <summary>Gaps this extractor always has, stated on every scope it produces.</summary>
    public static class Disclosures
    {
        /// <summary>No type checking: an import names a module specifier, not a symbol.</summary>
        public const string TypesNotChecked = "typescript-types-not-checked";

        /// <summary>
        /// RETIRED. Kept only so the string has one home: stores written before this reader read
        /// non-exported declarations still carry it, and a test asserts it is no longer emitted.
        /// Disclosing a gap that has been closed is the same defect as hiding one that has not.
        /// </summary>
        public const string NonExportedNotAnalysed = "typescript-non-exported-not-analysed";

        /// <summary>
        /// Nothing but a static <c>import</c>/<c>export … from</c> statement is followed —
        /// <c>import()</c>, <c>require()</c> in ANY form, and re-export globs are not.
        /// </summary>
        /// <remarks>
        /// The wording used to say "require with a VARIABLE", which implied a literal
        /// <c>require('fs')</c> was read. It never was. MEASURED on TheTerrace: two of the six
        /// hand-written JavaScript files use CommonJS and nothing else, so the implication was false
        /// about a third of the real corpus. Reading it would mean matching <c>require(</c> anywhere
        /// in a file, which is the unanchored shape this reader has just been fixed for; when a
        /// consumer needs CommonJS, the anchored statement form is the way to add it.
        /// </remarks>
        public const string DynamicImportsNotAnalysed = "typescript-dynamic-imports-not-analysed";

        /// <summary>An export whose spelling this reader does not know (DC-033's own alarm).</summary>
        public const string ExportsNotRecognised = "typescript-exports-not-recognised";

        /// <summary>An import naming Node's runtime — a boundary of the product, not a gap in it.</summary>
        public const string NodeBuiltinsNotIndexed = "typescript-node-builtins-not-indexed";

        /// <summary>An import naming an npm package — a boundary of the product, not a gap in it.</summary>
        public const string PackagesNotIndexed = "typescript-packages-not-indexed";

        /// <summary>A specifier this scope does not contain and which nobody can identify.</summary>
        public const string ImportsNotResolved = "typescript-imports-not-resolved";

        /// <summary>Bundled or generated JavaScript, skipped because nobody wrote it.</summary>
        public const string GeneratedSourceNotRead = "typescript-generated-source-not-read";

        /// <summary>Anything inside a function, a class or a namespace block is invisible.</summary>
        public const string NestedDeclarationsNotAnalysed = "typescript-nested-declarations-not-analysed";
    }

    // MEASURED against a real repository, not assumed. The previous form omitted `async`, the
    // generator star, `namespace`, `let` and `var` — and TheTerrace declares four
    // `export namespace`, every one of which was reported as absent rather than as unread. That is
    // DC-033 in this file: a reader that knows one spelling and reports the rest as nothing.
    //
    // `export` is now OPTIONAL, and that is the coverage half of this change. MEASURED on
    // TheTerrace: 13 scopes, 194 facts, and the only node kinds in any of them were
    // `typescript-module` and `typescript-const` — no class, function, interface or type anywhere,
    // while every one of the 13 scopes disclosed `typescript-non-exported-not-analysed`. A class at
    // column zero is a thing that exists whether or not anything may import it; the export keyword
    // says who may REACH it, which is an attribute of the declaration rather than a condition on
    // its existence, and is recorded as `is_exported`.
    //
    // Column zero, like Python's reader, and for the same reason: an indented `class` is a local or
    // a class expression, and claiming it as a module-level declaration would put a symbol in the
    // graph that no importer can reach. `[ \t]` rather than `\s` throughout so a match cannot drift
    // across a line break into text the anchor was supposed to exclude.
    private static readonly Regex Declaration = new(
        @"^(export[ \t]+)?(?:default[ \t]+)?(?:declare[ \t]+)?(?:abstract[ \t]+)?(?:async[ \t]+)?" +
        @"(class|interface|type|enum|function|const|let|var|namespace|module)[ \t]*\*?[ \t]+" +
        @"([A-Za-z_$][A-Za-z0-9_$]*)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Declaration keywords that bind a VALUE rather than name a thing.
    /// </summary>
    /// <remarks>
    /// Read only when exported. An exported <c>const</c> is the module's public surface and belongs
    /// in the graph; a module-local <c>const</c> is a variable, and the Python reader draws the same
    /// line — it reads <c>class</c> and <c>def</c> and never a module-level assignment. Putting every
    /// local constant in the node table would repeat the <c>has_member</c> mistake of adding
    /// thousands of nodes to serve nothing.
    /// </remarks>
    private static readonly HashSet<string> ValueBindings =
        new(StringComparer.Ordinal) { "const", "let", "var" };

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
    ///
    /// <b>That last exclusion was documented before it was implemented.</b> The first version of this
    /// pattern excluded braces, stars, `=` and `type {`, and did NOT exclude `export default` of an
    /// expression — so `export default defineConfig({…})` and `export default test;` counted as
    /// misses. Found by running the extractor over a SECOND repository, which is the only reason it
    /// surfaced: `export default` is ubiquitous, so the disclosure would have fired on nearly every
    /// real TypeScript codebase and become noise. A `default` followed by a declaration keyword is
    /// still a declaration and is still counted.
    /// </remarks>
    private static readonly Regex ExportLine =
        new(@"^export\s+(?!\{|\*|=|type\s*\{|default\s+(?!class|interface|type|enum|function|const|let|var|namespace|module|abstract|async|declare))\S+",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// A module specifier, and only inside an import or re-export STATEMENT.
    /// </summary>
    /// <remarks>
    /// <para><b>The anchor is the whole fix.</b> This pattern used to be a bare
    /// <c>from\s+['"]…['"]</c> matched anywhere in the file, so the word <i>from</i> in any sentence,
    /// any template literal or any pair of adjacent string literals began an import statement.
    /// MEASURED on TheTerrace: of 14 import edges, <b>6 named nothing that exists</b> — a sentence
    /// out of an audit log (<c>the product must include full fantasy management,</c>), two fragments
    /// of bundled help text (<c>${url}</c>, <c>.command()</c>), and two spans of compiled JavaScript
    /// beginning <c> + quoteFileNameIfNeeded(</c>, where the <c>from </c> ended one string literal
    /// and the closing quote came from the next one. Half of everything this reader said about
    /// imports was invented, and every invented fact arrived labelled with a specifier a person would
    /// read as real.</para>
    ///
    /// <para>This is the <c>uses_table</c> defect in another reader: a keyword matched anywhere in a
    /// string turned <i>"we update the record"</i> into a table called <c>the</c>. The remedy is the
    /// same one — require the STATEMENT shape, not the keyword.</para>
    ///
    /// <para><b>Why it may still span lines.</b> The commonest multi-symbol import in TypeScript puts
    /// <c>from</c> on its own line, so an anchor that also required <c>from</c> on the first line
    /// would trade over-matching for matching almost nothing — the direction this codebase has
    /// already got wrong once. The statement therefore begins at a line start with <c>import</c> or
    /// <c>export</c> and may run on, but it cannot cross a <c>;</c>, a quote or a backtick, so it can
    /// never reach out of its own statement and into a string.</para>
    /// </remarks>
    private static readonly Regex FromSpecifier = new(
        "^[ \\t]*(?:import|export)\\b[^;'\"`]*?\\bfrom[ \\t]*(['\"])([^'\"\\r\\n]+)\\1",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>A module imported for its side effects: <c>import './polyfills';</c></summary>
    private static readonly Regex BareImport = new(
        "^[ \\t]*import[ \\t]*(['\"])([^'\"\\r\\n]+)\\1",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly string[] Extensions = [".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs"];

    /// <summary>
    /// Directories this reader does not walk, because nothing in them was written by a person.
    /// </summary>
    /// <remarks>
    /// <para><b>One list, shared with discovery.</b> <c>CSharpScopeDiscovery</c> has skipped
    /// <c>bin</c>, <c>obj</c> and <c>artifacts</c> since it was written and this walk did not, so two
    /// lists decided one question and one of them was wrong — DC-022's shape, and the exact note
    /// already written above <c>CSharpScopeDiscovery.Skip</c> about <c>artifacts</c>.</para>
    ///
    /// <para><b>MEASURED on TheTerrace:</b> a scope rooted at <c>tests/</c> descended into
    /// <c>tests/TheTerrace.E2ETests/bin/Debug/net10.0/.playwright/package/</c> and indexed a vendored
    /// browser driver — twice, once per build configuration. Most of the 88 modules the reader
    /// produced came from there, and so did four of the six invented import specifiers.</para>
    /// </remarks>
    internal static readonly string[] SkippedDirectories =
    [
        ".git", "node_modules",
        "bin", "obj", "artifacts", "publish", "_framework",
        "dist", "build", "out", ".next", "coverage",
    ];

    /// <summary>
    /// The longest line this reader will accept as something a person typed.
    /// </summary>
    /// <remarks>
    /// MEASURED across both corpora rather than chosen. The longest line in a file a person wrote is
    /// <b>331</b> characters (a JSX file full of class lists); the shortest longest-line in a
    /// generated file is <b>1,101</b> (<c>docs/docs-index.js</c>), and the bundles run to
    /// <b>2,945,894</b> (<c>docs/.obsidian/plugins/juggl/main.js</c>). 500 sits between the two
    /// populations — above every hand-written line observed, roughly four times the most permissive
    /// common <c>max-len</c> lint setting, and far below anything generated.
    /// </remarks>
    private const int LongestHandWrittenLine = 500;

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

        // Generated files are removed BEFORE the module set is built, not while reading. A bundle
        // left in the set would resolve an import to a module that is never emitted, which is a
        // Verified edge pointing at a node that does not exist — the invented-fact failure one layer
        // deeper than the one this change is about.
        var candidates = Files(directory).ToList();
        var files = new List<string>(candidates.Count);
        var generated = 0;

        foreach (var candidate in candidates)
        {
            if (IsGenerated(candidate)) generated++;
            else files.Add(candidate);
        }

        var modules = files
            .Select(f => ModuleNaming.Qualify(prefix, ModuleName(directory, f)))
            .ToHashSet(StringComparer.Ordinal);

        // The rest of the workspace, so `../shared/thing` reaching out of this directory resolves
        // rather than being disclosed as unresolvable.
        var everywhere = new HashSet<string>(modules, StringComparer.Ordinal);
        if (request.WorkspaceModules is { } supplied) everywhere.UnionWith(supplied);

        var installed = InstalledPackages(directory);

        var assertions = new List<EvidenceAssertion>();
        var unresolved = 0;
        var nodeBuiltins = 0;
        var packages = 0;
        var unrecognisedExports = 0;

        // NonExportedNotAnalysed is NOT here any more: non-exported top-level declarations are read,
        // and disclosing a gap that has been closed is the same defect as hiding one that has not.
        // What replaces it is narrower and true — the ceiling is now column zero, not the export
        // keyword.
        foreach (var disclosure in new[]
        {
            Disclosures.TypesNotChecked,
            Disclosures.NestedDeclarationsNotAnalysed,
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

            // A block comment's lines begin at column zero, which is exactly what this reader looks
            // for and exactly what a file of removed code looks like.
            text = SourceText.WithoutCComments(text);

            var module = ModuleNaming.Qualify(prefix, ModuleName(directory, file));
            assertions.Add(Fact(request, module, "has_type", "typescript-module"));

            var declarations = Declaration.Matches(text);

            // The miss rate, counted per file, and counted against the EXPORTED declarations only.
            // Now that unexported ones are read too, comparing against every match would let three
            // internal classes cancel out one export form nobody has thought of — silently disabling
            // the DC-033 alarm this reader carries.
            var exportLines = ExportLine.Matches(text).Count;
            var exportedDeclarations = declarations.Count(m => m.Groups[1].Success);
            if (exportLines > exportedDeclarations) unrecognisedExports += exportLines - exportedDeclarations;

            foreach (Match match in declarations)
            {
                var exported = match.Groups[1].Success;
                var kind = match.Groups[2].Value;
                var name = match.Groups[3].Value;

                if (!exported && ValueBindings.Contains(kind)) continue;

                assertions.Add(Fact(request, $"{module}.{name}", "has_type", "typescript-" + kind));
                assertions.Add(Fact(request, $"{module}.{name}", "declared_in", module));

                // An ATTRIBUTE, not a relation: a property OF the declaration rather than a link to
                // another thing, so it is registered in EvidencePredicates.Attributes and is never
                // drawn. It exists because widening the reader must not cost the one answer the
                // narrow reader could give — which of these is the module's public surface.
                assertions.Add(Fact(request, $"{module}.{name}", "is_exported",
                    exported ? "true" : "false"));
            }

            var specifiers = FromSpecifier.Matches(text).Select(m => m.Groups[2].Value)
                .Concat(BareImport.Matches(text).Select(m => m.Groups[2].Value))
                .Distinct(StringComparer.Ordinal);

            foreach (var specifier in specifiers)
            {
                // Resolved means a file this scope contains and read.
                if (Resolve(specifier, module, everywhere) is { } resolved)
                {
                    assertions.Add(Fact(request, module, "imports", resolved, VerificationStatus.Verified));
                    continue;
                }

                // DC-050. Node's runtime and npm are BOUNDARIES of this product, not gaps in it —
                // the same statement the C# reader makes about the BCL and the Python reader makes
                // about its standard library. Counted here, and deliberately not drawn: 226 edges to
                // `sys`, `os` and `json` put Python's standard library among the most connected nodes
                // in a real graph, and `fs`, `path` and `react` would do the same.
                if (NodeBuiltinModules.Contains(specifier)) { nodeBuiltins++; continue; }
                if (IsPackage(specifier, installed)) { packages++; continue; }

                // What is left is a genuine unknown — a tsconfig path alias, a package that is not
                // installed, a module in a scope nobody supplied. It stays Inferred with the
                // specifier as written, because asserting which of those it is would be the guess
                // DC-022 is about.
                unresolved++;
                assertions.Add(Fact(request, module, "imports", specifier, VerificationStatus.Inferred));
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

        // THREE separate numbers, because they are three different statements. "31 specifiers name
        // something nobody can identify" is a gap in this product; "31 imports name Node's runtime"
        // and "31 name an npm package" are statements about its scope. Reporting them as one number
        // is DC-050 — the Python instance put 246 in front of a reader as the largest coverage hole
        // in the product, and the true figure was 2. Each is conditional: a disclosure that fires
        // when nothing was hidden teaches a reader to skip disclosures.
        if (unresolved > 0)
        {
            assertions.Add(Fact(request, request.ScopeId, "discloses",
                $"{Disclosures.ImportsNotResolved} ({unresolved:N0} specifier(s) name something this " +
                "scope does not contain)"));
        }

        if (nodeBuiltins > 0)
        {
            assertions.Add(Fact(request, request.ScopeId, "discloses",
                $"{Disclosures.NodeBuiltinsNotIndexed} ({nodeBuiltins:N0} import(s) name a Node.js " +
                "builtin module, which this product does not index)"));
        }

        if (packages > 0)
        {
            assertions.Add(Fact(request, request.ScopeId, "discloses",
                $"{Disclosures.PackagesNotIndexed} ({packages:N0} import(s) name an npm package, " +
                "which this product does not index)"));
        }

        if (generated > 0)
        {
            assertions.Add(Fact(request, request.ScopeId, "discloses",
                $"{Disclosures.GeneratedSourceNotRead} ({generated:N0} file(s) are minified or " +
                "generated and were not read)"));
        }


        // Identical facts are ONE fact. Two files can share a module name — `app.ts` beside a
        // compiled `app.js` is the common case, and an import specifier resolves to one module
        // regardless — so the same triple can be asserted twice in a scope. The store's natural key
        // rejects that (P1-STORE-05, deliberately), which surfaced as a raw SQLite constraint error
        // from the middle of an index on a real repository. Deduplicating here is the honest fix:
        // the duplicate carries no information, and silencing the key would weaken a real control.
        var deduplicated = ExtractionFacts.Distinct(assertions);

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

    /// <summary>
    /// Whether a file is a bundle or a generated artifact rather than something a person wrote.
    /// </summary>
    /// <remarks>
    /// <para>A bundle can sit anywhere, so a directory list cannot find them all:
    /// <c>docs/.obsidian/plugins/juggl/main.js</c> is in no build directory and is 2.9 million
    /// characters on one line. Line length is the signal that survives being moved.</para>
    ///
    /// <para><b>The failure direction is deliberate.</b> A file wrongly skipped is a gap, counted and
    /// disclosed on the scope; a bundle wrongly read is a confident claim about code nobody wrote,
    /// arriving labelled Verified. Every invented specifier this reader has produced came out of one.</para>
    ///
    /// <para>Read line by line and abandoned at the first long one, so a three-megabyte bundle costs
    /// one line rather than a full parse.</para>
    /// </remarks>
    private static bool IsGenerated(string path)
    {
        // `.min.` is the one naming convention that means this unambiguously, and it costs nothing.
        if (Path.GetFileName(path).Contains(".min.", StringComparison.OrdinalIgnoreCase)) return true;

        try
        {
            foreach (var line in File.ReadLines(path))
            {
                if (line.Length > LongestHandWrittenLine) return true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Unreadable is not generated. The read in the main loop fails the same way and skips it.
            return false;
        }

        return false;
    }

    /// <summary>
    /// Package names actually installed under this scope, so a bare specifier can be identified.
    /// </summary>
    /// <remarks>
    /// The directory walk deliberately does not descend into <c>node_modules</c>; this reads only its
    /// immediate children, which is where package names live. Neither measured repository has a
    /// <c>node_modules</c> at all, which is why <c>@scope/name</c> is identified by syntax as well —
    /// a check that can only fire somewhere else is a check nobody has seen say no (DC-016).
    /// </remarks>
    private static IReadOnlySet<string> InstalledPackages(string directory)
    {
        var installed = new HashSet<string>(StringComparer.Ordinal);
        var skip = new HashSet<string>(SkippedDirectories, StringComparer.OrdinalIgnoreCase);

        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(current); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var child in children)
            {
                var name = Path.GetFileName(child);

                if (name.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                {
                    Collect(child, installed);
                    continue;
                }

                if (!skip.Contains(name)) pending.Push(child);
            }
        }

        return installed;

        static void Collect(string nodeModules, HashSet<string> into)
        {
            IEnumerable<string> packages;
            try { packages = Directory.EnumerateDirectories(nodeModules); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return; }

            foreach (var package in packages)
            {
                var name = Path.GetFileName(package);

                // `@scope` is a directory OF packages, not a package.
                if (name.StartsWith('@'))
                {
                    IEnumerable<string> scoped;
                    try { scoped = Directory.EnumerateDirectories(package); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

                    foreach (var one in scoped) into.Add(name + "/" + Path.GetFileName(one));
                    continue;
                }

                if (!name.StartsWith('.')) into.Add(name);
            }
        }
    }

    /// <summary>
    /// Whether a specifier names an npm package — a boundary of this product rather than a gap.
    /// </summary>
    /// <remarks>
    /// <para>Two ways, and only two. A <c>@scope/name</c> specifier is a package by SYNTAX: nothing
    /// else may be spelled that way, and it needs no configuration to recognise —
    /// <c>@playwright/test</c> is the one real unresolved specifier TheTerrace had. The empty scope
    /// <c>@/…</c> is excluded deliberately: that is the commonest tsconfig path alias in the
    /// ecosystem, not a package, and treating it as one would turn a genuine unknown into a silent
    /// boundary — DC-050 run backwards, which is worse than the original.</para>
    ///
    /// <para>Otherwise the package must actually be INSTALLED beside the code. A bare name with no
    /// <c>node_modules</c> to confirm it may equally be a build-time alias, so it stays an unknown
    /// and is disclosed as one. This reader reads no configuration and does not pretend to.</para>
    ///
    /// <para><b>The known cost, measured rather than waved at.</b> On this repository <c>react</c> is
    /// reported as an unknown, and it is almost certainly npm. A <c>package.json</c>
    /// <c>dependencies</c> block would settle it without guessing — but NEITHER measured repository
    /// contains one outside build output, so that check could not fire in the environment that
    /// verifies it (DC-016), and its absence costs exactly one entry in one count. A repository with
    /// a manifest is the upgrade trigger. Erring this way is deliberate: over-claiming would file a
    /// real unknown under a boundary nobody re-reads, which is DC-050 run backwards and worse than
    /// the original.</para>
    /// </remarks>
    private static bool IsPackage(string specifier, IReadOnlySet<string> installed)
    {
        if (specifier.Length == 0 || specifier.StartsWith('.')) return false;

        if (specifier.StartsWith('@'))
        {
            var slash = specifier.IndexOf('/', StringComparison.Ordinal);
            return slash > 1;
        }

        var cut = specifier.IndexOf('/', StringComparison.Ordinal);
        return installed.Contains(cut < 0 ? specifier : specifier[..cut]);
    }

    private static IEnumerable<string> Files(string directory)
    {
        var skip = new HashSet<string>(SkippedDirectories, StringComparer.OrdinalIgnoreCase);

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
