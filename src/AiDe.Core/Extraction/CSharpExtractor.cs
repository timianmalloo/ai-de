using System.Diagnostics;
using AiDe.Core.Facts;
using Microsoft.CodeAnalysis;

namespace AiDe.Core.Extraction;

/// <summary>
/// The C# semantic extractor — real symbols, without ever running the repository's build.
/// </summary>
/// <remarks>
/// <para><b>Named for the language, not the library.</b> Roslyn is still the semantic engine, but
/// "RoslynExtractor" implied the Roslyn <i>workspace</i> layer, and that is precisely the part not
/// used: <c>MSBuildWorkspace</c> loads projects by evaluating MSBuild, which spike D3 measured
/// executing repository-supplied code by four vectors, two of which need nothing but a checked-in
/// <c>.csproj</c>.</para>
///
/// <para><b>One scope is one (project, target framework).</b> Declared here rather than left
/// implicit, because it is the grain of every row this emits.</para>
///
/// <para><b>An edge that did not resolve is not emitted.</b> Emitting it as <c>Inferred</c> would be
/// worse than silence: the name is whatever the source typed, unresolved by anything, so the edge
/// would point at a node that may not exist. What the user gets instead is a disclosure on the scope
/// saying the picture is incomplete and why.</para>
/// </remarks>
public sealed class CSharpExtractor(string extractorVersion = "1.0.0") : IExtractor
{
    public const string ExtractorId = "csharp-extractor";

    /// <summary>The predicate a scope uses to declare what it could not see.</summary>
    public const string DisclosurePredicate = "discloses";

    private readonly CSharpProjectReader _reader = new();

    public string ScopeKind => "csharp";

    /// <summary>Every target framework the project at <paramref name="projectPath"/> declares.</summary>
    /// <remarks>The caller creates one scope per entry — see the grain note on the class.</remarks>
    public IReadOnlyList<string> TargetFrameworks(string projectPath) => _reader.TargetFrameworks(projectPath);

    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var projectPath = Path.GetFullPath(request.RootPath);
        if (!File.Exists(projectPath))
        {
            return Task.FromResult(new ExtractionResult(
                [], Complete: false,
                [new ExtractionDiagnostic("AIDE-EXT-PROJECT-MISSING", request.RootPath, "no project file at this path")]));
        }

        // The framework is carried on the scope id's suffix when the caller made one scope per
        // framework; otherwise the project's first declared framework is used.
        var frameworks = _reader.TargetFrameworks(projectPath);
        if (frameworks.Count == 0)
        {
            // Discovery still produced a scope for this project so it would be counted rather than
            // vanish; this is where that scope becomes a reported failure.
            return Task.FromResult(new ExtractionResult(
                [], Complete: false,
                [new ExtractionDiagnostic(
                    "AIDE-EXT-PROJECT-UNREADABLE", request.ScopeId,
                    "the project file could not be read as XML")]));
        }

        var tfm = frameworks.FirstOrDefault(f =>
            request.ScopeId.EndsWith(f, StringComparison.OrdinalIgnoreCase)) ?? frameworks[0];

        // Where a scope's time goes, emitted on the normal path. "Extraction is the cost" was true and
        // useless: it did not say whether the cost is reading the project, building the compilation,
        // or walking the symbols, and those have opposite fixes.
        var readStarted = System.Diagnostics.Stopwatch.StartNew();

        CSharpCompilationResult compiled;
        try
        {
            compiled = _reader.Compile(projectPath, tfm, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The scope budget expired. Reported as an incomplete extraction rather than rethrown,
            // because a scope that timed out must quarantine itself and leave the other scopes'
            // evidence alone (P2-EXT-04) — not fail the whole refresh.
            return Task.FromResult(new ExtractionResult(
                [], Complete: false,
                [new ExtractionDiagnostic("AIDE-EXT-TIMEOUT", request.ScopeId, $"extraction exceeded its budget for {tfm}")]));
        }

        var diagnostics = new List<ExtractionDiagnostic>();
        var assertions = new List<EvidenceAssertion>();
        var observedAt = DateTimeOffset.UtcNow;

        if (compiled.Compilation is null)
        {
            diagnostics.Add(new ExtractionDiagnostic(
                "AIDE-EXT-LOAD-FAILED", request.ScopeId, $"the project could not be compiled for {tfm}"));
            return Task.FromResult(new ExtractionResult(assertions, Complete: false, diagnostics));
        }

        var readMs = readStarted.ElapsedMilliseconds;
        var walkStarted = System.Diagnostics.Stopwatch.StartNew();

        // Disclosures FIRST, so a scope whose extraction is later truncated still carries the
        // reasons it is incomplete. A truncated list that lost its caveats is worse than a short one.
        foreach (var disclosure in compiled.Disclosures)
        {
            assertions.Add(Assertion(
                request, ScopeNodeId(request.ScopeId), DisclosurePredicate, disclosure,
                VerificationStatus.Verified, new Provenance(
                    Path.GetFileName(projectPath), "1:1", ExtractorId, extractorVersion, observedAt)));
        }

        foreach (var note in compiled.Notes)
        {
            diagnostics.Add(new ExtractionDiagnostic("AIDE-EXT-NOTE", request.ScopeId, note));
        }

        // The walk is the largest real cost — 1,167ms of a 2s scope on a real repository — and it
        // has never been broken down. Enumerating the namespace tree and asking each type for its
        // members are different operations with different remedies.
        var enumerateWatch = System.Diagnostics.Stopwatch.StartNew();
        var types = new List<INamedTypeSymbol>();
        Walk(compiled.Compilation.Assembly.GlobalNamespace, types);
        enumerateWatch.Stop();

        var memberWatch = System.Diagnostics.Stopwatch.StartNew();

        // The walk's interior, split by OPERATION rather than by type. `members 1,074ms` named the
        // loop, not the cost inside it, and the loop does four different things: name a symbol, find
        // where it was written, read its attributes, and traverse its members. Accumulated ticks
        // rather than nested stopwatches — a Stopwatch per type per operation would be four
        // allocations a thousand times over, measuring the measurement.
        long displayTicks = 0, provenanceTicks = 0, attributeTicks = 0, dependsTicks = 0;
        var displayCalls = 0;

        foreach (var type in types)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mark = System.Diagnostics.Stopwatch.GetTimestamp();
            var subject = type.ToDisplayString();
            displayTicks += System.Diagnostics.Stopwatch.GetTimestamp() - mark;
            displayCalls++;

            mark = System.Diagnostics.Stopwatch.GetTimestamp();
            var provenance = ProvenanceFor(type, projectPath, observedAt);
            provenanceTicks += System.Diagnostics.Stopwatch.GetTimestamp() - mark;

            assertions.Add(Assertion(request, subject, "has_type", KindOf(type), VerificationStatus.Verified, provenance));
            assertions.Add(Assertion(request, subject, "declared_in", request.ScopeId, VerificationStatus.Verified, provenance));

            if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType && Resolved(baseType))
            {
                mark = System.Diagnostics.Stopwatch.GetTimestamp();
                var baseName = baseType.ToDisplayString();
                displayTicks += System.Diagnostics.Stopwatch.GetTimestamp() - mark;
                displayCalls++;

                assertions.Add(Assertion(
                    request, subject, "inherits", baseName, VerificationStatus.Verified, provenance));
            }

            foreach (var contract in type.Interfaces.Where(Resolved))
            {
                mark = System.Diagnostics.Stopwatch.GetTimestamp();
                var contractName = contract.ToDisplayString();
                displayTicks += System.Diagnostics.Stopwatch.GetTimestamp() - mark;
                displayCalls++;

                assertions.Add(Assertion(
                    request, subject, "implements", contractName, VerificationStatus.Verified, provenance));
            }

            // A [Table("orders")] attribute is a DECLARATION, so the code-to-schema join it produces
            // is Verified rather than a naming-convention guess. This is the one join in the phase
            // that a repository states outright, and it is worth reading precisely because every
            // other one is inferred.
            mark = System.Diagnostics.Stopwatch.GetTimestamp();
            var declaredTable = type.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name is "TableAttribute" or "Table")
                ?.ConstructorArguments.FirstOrDefault().Value as string;

            if (!string.IsNullOrEmpty(declaredTable))
            {
                assertions.Add(Assertion(
                    request, subject, "declares_table", declaredTable, VerificationStatus.Verified, provenance));
            }

            attributeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - mark;

            mark = System.Diagnostics.Stopwatch.GetTimestamp();
            var targets = DependsOn(type).Distinct(SymbolEqualityComparer.Default).OfType<ITypeSymbol>().ToList();
            dependsTicks += System.Diagnostics.Stopwatch.GetTimestamp() - mark;

            foreach (var target in targets)
            {
                mark = System.Diagnostics.Stopwatch.GetTimestamp();
                var targetName = target.ToDisplayString();
                displayTicks += System.Diagnostics.Stopwatch.GetTimestamp() - mark;
                displayCalls++;

                assertions.Add(Assertion(
                    request, subject, "depends_on", targetName, VerificationStatus.Verified, provenance));
            }
        }

        memberWatch.Stop();

        // A Fluent `builder.Entity<Order>().ToTable("orders")` is as much a declaration as the
        // attribute, and it is the MORE common style — without it the commonest way of stating the
        // mapping fell back to a name-matching guess. Scanned once over the compilation's trees
        // rather than once per type; provenance is now the CALL SITE, which is where a reader
        // looking for the mapping actually needs to go.
        var fluentWatch = System.Diagnostics.Stopwatch.StartNew();

        var unresolvedMappings = 0;
        var generatedSkipped = 0;

        foreach (var (entity, table, where) in FluentTableMappings(
            compiled.Compilation,
            (unresolvedCount, generatedCount) =>
            {
                unresolvedMappings = unresolvedCount;
                generatedSkipped = generatedCount;
            },
            cancellationToken))
        {
            assertions.Add(Assertion(
                request, entity, "declares_table", table, VerificationStatus.Verified,
                ProvenanceAt(where, projectPath, observedAt)));
        }

        if (generatedSkipped > 0)
        {
            // Skipped is not the same as absent. A reader who knows the mapping is written down
            // somewhere needs to be told that this is where it was declined, and why.
            assertions.Add(Assertion(
                request, ScopeNodeId(request.ScopeId), DisclosurePredicate,
                $"generated-source-not-read-for-mappings ({generatedSkipped:N0} auto-generated " +
                "file(s) mention ToTable; their mappings describe a past migration, not the model)",
                VerificationStatus.Verified,
                new Provenance(Path.GetFileName(projectPath), "1:1", ExtractorId, extractorVersion, observedAt)));
        }

        if (unresolvedMappings > 0)
        {
            // Counted, because "some mappings were not read" and "17 of 60 were not read" are
            // different statements about how much of the code-to-schema join is still a guess.
            assertions.Add(Assertion(
                request, ScopeNodeId(request.ScopeId), DisclosurePredicate,
                $"fluent-table-mappings-unresolved ({unresolvedMappings:N0} ToTable call(s) whose " +
                "entity type did not resolve)", VerificationStatus.Verified,
                new Provenance(Path.GetFileName(projectPath), "1:1", ExtractorId, extractorVersion, observedAt)));
        }

        fluentWatch.Stop();

        // Complete means "this is the whole snapshot for this scope", which it is: the disclosures
        // are IN the snapshot rather than missing from it. Marking it incomplete would quarantine
        // every unrestored project, which is most of them on a fresh clone.

        if (Environment.GetEnvironmentVariable("AIDE_EXTRACTION_TIMING") is not null)
        {
            Console.Error.WriteLine(
                $"[timing]   enumerate-types {enumerateWatch.ElapsedMilliseconds}ms for {types.Count} type(s), " +
                $"members {memberWatch.ElapsedMilliseconds}ms " +
                $"(display {Ms(displayTicks)}ms/{displayCalls:N0} calls, provenance {Ms(provenanceTicks)}ms, " +
                $"attributes {Ms(attributeTicks)}ms, depends-on {Ms(dependsTicks)}ms), " +
                $"fluent-scan {fluentWatch.ElapsedMilliseconds}ms");
        }

        // Emitted on the NORMAL path, no flag to remember: an operator asking "why is indexing slow"
        // gets the split rather than a total they have to guess at.
        Activity.Current?.SetTag("extraction.read_ms", readMs);
        Activity.Current?.SetTag("extraction.walk_ms", walkStarted.ElapsedMilliseconds);
        Activity.Current?.SetTag("extraction.assertions", assertions.Count);

        if (Environment.GetEnvironmentVariable("AIDE_EXTRACTION_TIMING") is not null)
        {
            Console.Error.WriteLine(
                $"[timing] {request.ScopeId}: read {readMs}ms, walk {walkStarted.ElapsedMilliseconds}ms, " +
                $"{assertions.Count:N0} assertion(s)");
        }

        return Task.FromResult(new ExtractionResult(assertions, Complete: true, diagnostics));
    }

    /// <summary>Accumulated stopwatch ticks as milliseconds.</summary>
    private static long Ms(long ticks) => ticks * 1000 / System.Diagnostics.Stopwatch.Frequency;

    /// <summary>The node a scope's own facts hang off.</summary>
    public static string ScopeNodeId(string scopeId) => $"scope:{scopeId}";

    private EvidenceAssertion Assertion(
        ExtractionRequest request, string subject, string predicate, string @object,
        VerificationStatus status, Provenance provenance) =>
        new(request.ScopeId, request.ArtifactRevision, subject, predicate, @object,
            EvidenceOrigin.Static, status, provenance);

    private Provenance ProvenanceFor(ISymbol symbol, string projectPath, DateTimeOffset observedAt) =>
        ProvenanceAt(symbol.Locations.FirstOrDefault(l => l.IsInSource), projectPath, observedAt);

    /// <summary>Where a fact was written, or the project file when that is not in source.</summary>
    private Provenance ProvenanceAt(Location? location, string projectPath, DateTimeOffset observedAt)
    {
        if (location is null || !location.IsInSource || location.SourceTree is null)
        {
            return new Provenance(Path.GetFileName(projectPath), "1:1", ExtractorId, extractorVersion, observedAt);
        }

        var line = location.GetLineSpan().StartLinePosition;
        var dir = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;
        var relative = Path.GetRelativePath(dir, location.SourceTree.FilePath).Replace((char)92, '/');
        return new Provenance(relative, $"{line.Line + 1}:{line.Character + 1}", ExtractorId, extractorVersion, observedAt);
    }

    private static string KindOf(INamedTypeSymbol type) => type.TypeKind switch
    {
        TypeKind.Interface => "interface",
        TypeKind.Struct => "struct",
        TypeKind.Enum => "enum",
        TypeKind.Delegate => "delegate",
        _ => type.IsRecord ? "record" : "class",
    };

    /// <summary>
    /// Whether a symbol actually resolved. An error type is what the compiler produces when a
    /// reference could not be bound — its name is whatever the source typed, so an edge to it points
    /// at a node that may not exist.
    /// </summary>
    private static bool Resolved(ITypeSymbol type) => type switch
    {
        { TypeKind: TypeKind.Error } => false,
        IArrayTypeSymbol array => Resolved(array.ElementType),
        INamedTypeSymbol { IsGenericType: true } generic => generic.TypeArguments.All(Resolved),
        _ => true,
    };

    /// <summary>
    /// The types this type's own declarations point at — field and property types, method returns
    /// and parameters. Generic arguments are unwrapped, so <c>Task&lt;Order&gt;</c> yields an edge to
    /// <c>Order</c> rather than only to <c>Task</c>.
    /// </summary>
    private static IEnumerable<ITypeSymbol> DependsOn(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared) continue;

            switch (member)
            {
                case IFieldSymbol field:
                    foreach (var t in Unwrap(field.Type)) yield return t;
                    break;

                case IPropertySymbol property:
                    foreach (var t in Unwrap(property.Type)) yield return t;
                    break;

                case IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } method:
                    if (method.MethodKind == MethodKind.Ordinary)
                    {
                        foreach (var t in Unwrap(method.ReturnType)) yield return t;
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        foreach (var t in Unwrap(parameter.Type)) yield return t;
                    }

                    break;
            }
        }

        static IEnumerable<ITypeSymbol> Unwrap(ITypeSymbol type)
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    foreach (var t in Unwrap(array.ElementType)) yield return t;
                    break;

                case INamedTypeSymbol { IsGenericType: true } generic:
                    // A tuple is unwrapped into its ELEMENTS and never emitted itself. Indexing a
                    // real repository put "(T1, T2)" and "(T1, T2, T3)" among the most-connected
                    // nodes in the graph — the unbound display form of ValueTuple, which is a
                    // language construct rather than anything a user would navigate to.
                    if (!generic.IsTupleType && !IsNoise(generic))
                    {
                        yield return generic.ConstructedFrom;
                    }

                    foreach (var argument in generic.TypeArguments)
                    {
                        foreach (var t in Unwrap(argument)) yield return t;
                    }

                    break;

                default:
                    if (IsNoise(type)) break;
                    yield return type;
                    break;
            }
        }
    }

    /// <summary>
    /// Types that are language machinery rather than anything a user would navigate to.
    /// </summary>
    /// <remarks>
    /// Found by indexing someone else's repository: <c>void</c>, type parameters and tuples are
    /// everywhere, so as nodes they become the highest-degree things in the graph while meaning
    /// nothing. Their type ARGUMENTS still produce edges — a method returning
    /// <c>(Order, Customer)</c> depends on both.
    /// </remarks>
    private static bool IsNoise(ITypeSymbol type) =>
        !Resolved(type) ||
        type.SpecialType == SpecialType.System_Void ||
        type.TypeKind == TypeKind.TypeParameter ||
        type.TypeKind == TypeKind.Dynamic ||
        type is INamedTypeSymbol { IsTupleType: true } ||
        string.IsNullOrEmpty(type.Name);

    /// <summary>
    /// Entity-to-table mappings stated with the Fluent API, across the whole compilation.
    /// </summary>
    /// <remarks>
    /// <para><b>Resolved SEMANTICALLY, because the syntax chain only ever saw one of the styles.</b>
    /// The previous reader matched <c>Entity&lt;T&gt;()...ToTable("x")</c> as a single expression, so
    /// it found nothing whenever the builder was bound to a local first — which is the commonest way
    /// the API is actually used:</para>
    /// <code>
    /// var terrace = modelBuilder.Entity&lt;Terrace&gt;();
    /// terrace.ToTable("Terrace", "setup");
    /// </code>
    /// <para>MEASURED on a real repository: <b>1 verified join against 123 inferred</b>, on a
    /// codebase that declares its mappings outright. Every one of those guesses had a stated answer
    /// sitting in <c>OnModelCreating</c>. Asking the semantic model for the RECEIVER's type answers
    /// all the styles with one rule — chained, local-variable, lambda-configuration, and
    /// <c>IEntityTypeConfiguration&lt;T&gt;</c> — because in every one of them the receiver is an
    /// <c>EntityTypeBuilder&lt;TEntity&gt;</c>.</para>
    ///
    /// <para><b>It also fixes the name the edge is keyed on.</b> The syntax path took the type
    /// argument AS WRITTEN, so it produced <c>Order</c> where every other assertion about that type
    /// says <c>Shop.Order</c> — an edge whose subject matches no node in the graph. A symbol's
    /// display string is the same name the rest of this extractor emits.</para>
    ///
    /// <para><b>And it excludes the migration snapshots for free.</b> EF's generated
    /// <c>*.Designer.cs</c> files call <c>ToTable</c> through the NON-generic builder
    /// (<c>modelBuilder.Entity("Some.Type.Name")</c>), so there is no type argument to resolve and
    /// they yield nothing — correct twice over, because they are historical snapshots and a table
    /// renamed three migrations ago would otherwise be asserted as current fact.</para>
    ///
    /// <para><b>Only literal names.</b> A table name built from a variable or a constant is not
    /// resolved — the same rule the Bicep reader follows, and for the same reason: a guessed name
    /// produces a confident wrong join between a class and a table.</para>
    /// </remarks>
    private static IEnumerable<(string Entity, string Table, Location Where)> FluentTableMappings(
        Compilation compilation, Action<int, int> counts, CancellationToken cancellationToken)
    {
        var unresolvedCount = 0;
        var generatedCount = 0;

        foreach (var tree in compilation.SyntaxTrees)
        {
            // The prefilter, and the reason this is affordable. A `ToTable` invocation cannot exist
            // in a file whose text does not contain "ToTable", so a substring scan over source
            // already in memory rules out every file that has none — MEASURED at 1ms across 465
            // files, against 217ms to walk the 66 that survive. A false positive (the word in a
            // comment) falls through to the walk below and finds nothing, so the prefilter can cost
            // time but never correctness.
            if (!tree.GetText().ToString().Contains("ToTable", StringComparison.Ordinal)) continue;

            // GENERATED files are skipped, on correctness grounds before performance ones. EF writes
            // a `*.Designer.cs` model snapshot per migration, and each one calls ToTable for every
            // entity AS IT STOOD AT THAT MIGRATION. Reading them asserts a table renamed three
            // migrations ago as current fact, with the same Verified badge as the live mapping — the
            // shape of DC-022, two producers of one predicate where one of them is describing the
            // past. MEASURED on a real repository: 63 of the 66 files that mention ToTable are
            // generated snapshots, they hold ~125,000 of the lines, and binding them cost 1.2s to
            // produce nothing that should be believed.
            if (IsGenerated(tree, cancellationToken))
            {
                generatedCount++;
                continue;
            }

            // Built lazily and once per tree: a semantic model is expensive to create and useless on
            // a file whose ToTable turns out to be a comment.
            SemanticModel? model = null;

            foreach (var call in tree.GetRoot().DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>())
            {
                if (call.Expression is not Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax member) continue;
                if (member.Name.Identifier.ValueText != "ToTable") continue;

                // The first string literal is the table name in every overload — ToTable(name),
                // ToTable(name, schema), ToTable(name, buildAction), ToTable(name, schema, action).
                var table = call.ArgumentList.Arguments
                    .Select(a => a.Expression)
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>()
                    .FirstOrDefault(l => l.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression))
                    ?.Token.ValueText;

                if (string.IsNullOrEmpty(table)) continue;

                model ??= compilation.GetSemanticModel(tree);

                if (model.GetTypeInfo(member.Expression).Type is not INamedTypeSymbol receiver
                    || receiver.TypeKind == TypeKind.Error)
                {
                    // Packages not restored, most often. Counted and disclosed rather than guessed
                    // at from the syntax: a mapping recovered without the type is keyed on a name
                    // that matches no node, which is a dangling edge wearing a Verified badge.
                    unresolvedCount++;
                    continue;
                }

                if (!IsEntityBuilder(receiver)) continue;

                // EntityTypeBuilder<TEntity> has one argument; OwnedNavigationBuilder<TOwner,
                // TDependent> has two and the table belongs to the DEPENDENT. Last covers both.
                if (receiver.TypeArguments.LastOrDefault() is not { } entity || !Resolved(entity))
                {
                    unresolvedCount++;
                    continue;
                }

                yield return (entity.ToDisplayString(), table, call.GetLocation());
            }
        }

        counts(unresolvedCount, generatedCount);
    }

    /// <summary>
    /// Whether a file declares itself generated, by the standard .NET convention.
    /// </summary>
    /// <remarks>
    /// The rule Roslyn's own analyzers use: an <c>&lt;auto-generated&gt;</c> marker in the file's
    /// FIRST comment block. Deliberately not an EF-specific test — a designer file, a protobuf stub
    /// and a source-generator output are all code nobody wrote and nobody can fix, and a fact read
    /// out of one describes the generator rather than the codebase.
    /// </remarks>
    private static bool IsGenerated(SyntaxTree tree, CancellationToken cancellationToken)
    {
        foreach (var trivia in tree.GetRoot(cancellationToken).GetLeadingTrivia())
        {
            if (!trivia.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineCommentTrivia)
                && !trivia.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiLineCommentTrivia))
            {
                continue;
            }

            var text = trivia.ToString();

            if (text.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase)) return true;

            // Only the leading comment block counts. A file that MENTIONS auto-generation further
            // down — a comment about a generator, this very rule quoted in a test — is not itself
            // generated, and treating it as such would silently drop real declarations.
            return false;
        }

        return false;
    }

    /// <summary>
    /// Whether a receiver is one of EF's entity builders.
    /// </summary>
    /// <remarks>
    /// Named explicitly rather than accepting any generic type carrying a <c>ToTable</c> member:
    /// this reads a specific library's declaration, and an unrelated <c>ToTable</c> — a DataTable
    /// helper, a report formatter — is not a statement about persistence.
    /// </remarks>
    private static bool IsEntityBuilder(INamedTypeSymbol type) =>
        type.IsGenericType && type.Name is "EntityTypeBuilder" or "OwnedNavigationBuilder";

    private static void Walk(INamespaceSymbol ns, List<INamedTypeSymbol> into)
    {
        foreach (var type in ns.GetTypeMembers()) into.Add(type);
        foreach (var child in ns.GetNamespaceMembers()) Walk(child, into);
    }
}
