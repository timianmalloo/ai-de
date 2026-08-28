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

        var types = new List<INamedTypeSymbol>();
        Walk(compiled.Compilation.Assembly.GlobalNamespace, types);

        foreach (var type in types)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subject = type.ToDisplayString();
            var provenance = ProvenanceFor(type, projectPath, observedAt);

            assertions.Add(Assertion(request, subject, "has_type", KindOf(type), VerificationStatus.Verified, provenance));
            assertions.Add(Assertion(request, subject, "declared_in", request.ScopeId, VerificationStatus.Verified, provenance));

            if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType && Resolved(baseType))
            {
                assertions.Add(Assertion(
                    request, subject, "inherits", baseType.ToDisplayString(), VerificationStatus.Verified, provenance));
            }

            foreach (var contract in type.Interfaces.Where(Resolved))
            {
                assertions.Add(Assertion(
                    request, subject, "implements", contract.ToDisplayString(), VerificationStatus.Verified, provenance));
            }

            // A [Table("orders")] attribute is a DECLARATION, so the code-to-schema join it produces
            // is Verified rather than a naming-convention guess. This is the one join in the phase
            // that a repository states outright, and it is worth reading precisely because every
            // other one is inferred.
            var declaredTable = type.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name is "TableAttribute" or "Table")
                ?.ConstructorArguments.FirstOrDefault().Value as string;

            if (!string.IsNullOrEmpty(declaredTable))
            {
                assertions.Add(Assertion(
                    request, subject, "declares_table", declaredTable, VerificationStatus.Verified, provenance));
            }

            foreach (var target in DependsOn(type).Distinct(SymbolEqualityComparer.Default).OfType<ITypeSymbol>())
            {
                assertions.Add(Assertion(
                    request, subject, "depends_on", target.ToDisplayString(), VerificationStatus.Verified, provenance));
            }
        }

        // Complete means "this is the whole snapshot for this scope", which it is: the disclosures
        // are IN the snapshot rather than missing from it. Marking it incomplete would quarantine
        // every unrestored project, which is most of them on a fresh clone.
        return Task.FromResult(new ExtractionResult(assertions, Complete: true, diagnostics));
    }

    /// <summary>The node a scope's own facts hang off.</summary>
    public static string ScopeNodeId(string scopeId) => $"scope:{scopeId}";

    private EvidenceAssertion Assertion(
        ExtractionRequest request, string subject, string predicate, string @object,
        VerificationStatus status, Provenance provenance) =>
        new(request.ScopeId, request.ArtifactRevision, subject, predicate, @object,
            EvidenceOrigin.Static, status, provenance);

    private Provenance ProvenanceFor(ISymbol symbol, string projectPath, DateTimeOffset observedAt)
    {
        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null || location.SourceTree is null)
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

    private static void Walk(INamespaceSymbol ns, List<INamedTypeSymbol> into)
    {
        foreach (var type in ns.GetTypeMembers()) into.Add(type);
        foreach (var child in ns.GetNamespaceMembers()) Walk(child, into);
    }
}
