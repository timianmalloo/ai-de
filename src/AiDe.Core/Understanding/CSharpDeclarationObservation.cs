using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AiDe.Core.Understanding;

/// <summary>Request-local verified source text. It owns cloned bytes and never reads paths.</summary>
public sealed class VerifiedSourceBuffer
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly UnicodeEncoding StrictUtf16Le = new(false, false, true);
    private static readonly UnicodeEncoding StrictUtf16Be = new(true, false, true);
    private readonly byte[] rawBytes;

    private VerifiedSourceBuffer(AtlasSourceObservation sourceObservation, AtlasSourceBinding binding, byte[] rawBytes, string fullText)
    {
        SourceObservation = sourceObservation;
        Binding = binding;
        this.rawBytes = rawBytes;
        FullText = fullText;
    }

    public AtlasSourceObservation SourceObservation { get; }
    public AtlasSourceBinding Binding { get; }
    public string FullText { get; }
    public int RawByteLength => rawBytes.Length;

    public static VerifiedSourceBuffer FromBytes(AtlasSourceObservation sourceObservation, AtlasSourceBinding binding, ReadOnlySpan<byte> rawBytes)
    {
        ArgumentNullException.ThrowIfNull(sourceObservation);
        ArgumentNullException.ThrowIfNull(binding);
        if (sourceObservation.Status is not AtlasSourceObservationStatus.Verified)
        {
            throw new InvalidOperationException("Only verified source observations can produce source buffers.");
        }

        var ownedBytes = rawBytes.ToArray();
        var expectedRoot = AtlasIdentityCodec.ForNativeObject(sourceObservation.RootIdentity!);
        var expectedFile = AtlasIdentityCodec.ForNativeObject(sourceObservation.FileIdentity!);
        if (!binding.Matches(sourceObservation.ManifestToken, sourceObservation.FileValue, sourceObservation.PolicyToken, expectedRoot, expectedFile, sourceObservation.CanonicalSha256!))
        {
            throw new InvalidOperationException("Source binding does not match the verified source observation.");
        }

        var actualHash = CanonicalSha256(ownedBytes);
        if (!string.Equals(actualHash, sourceObservation.CanonicalSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Raw source bytes do not match the verified content hash.");
        }

        if (sourceObservation.ByteLength != ownedBytes.LongLength)
        {
            throw new InvalidOperationException("Raw source byte length does not match the verified observation.");
        }

        var fullText = Decode(ownedBytes, sourceObservation.DecoderId!);
        AtlasIdentityCodec.ValidUnicode(fullText, nameof(rawBytes));
        if (sourceObservation.DecodedUtf16Length != fullText.Length)
        {
            throw new InvalidOperationException("Decoded UTF-16 length does not match the verified observation.");
        }

        return new(sourceObservation, binding, ownedBytes, fullText);
    }

    private static string Decode(byte[] bytes, string decoderId) => decoderId switch
    {
        "utf-8" => StrictUtf8.GetString(bytes),
        "utf-8-bom" => DecodeAfterPreamble(bytes, [0xEF, 0xBB, 0xBF], StrictUtf8, decoderId),
        "utf-16le" => DecodeEven(bytes, StrictUtf16Le, decoderId),
        "utf-16le-bom" => DecodeAfterPreamble(bytes, [0xFF, 0xFE], StrictUtf16Le, decoderId),
        "utf-16be" => DecodeEven(bytes, StrictUtf16Be, decoderId),
        "utf-16be-bom" => DecodeAfterPreamble(bytes, [0xFE, 0xFF], StrictUtf16Be, decoderId),
        _ => throw new InvalidOperationException("Unsupported source decoder."),
    };

    private static string DecodeAfterPreamble(byte[] bytes, byte[] preamble, Encoding encoding, string decoderId)
    {
        if (bytes.Length < preamble.Length || !bytes.AsSpan(0, preamble.Length).SequenceEqual(preamble))
        {
            throw new InvalidOperationException($"Decoder {decoderId} requires its byte-order mark.");
        }

        return DecodeEven(bytes.AsSpan(preamble.Length).ToArray(), encoding, decoderId);
    }

    private static string DecodeEven(byte[] bytes, Encoding encoding, string decoderId)
    {
        if (decoderId.StartsWith("utf-16", StringComparison.Ordinal) && bytes.Length % 2 != 0)
        {
            throw new InvalidOperationException("UTF-16 source bytes must have an even length.");
        }

        return encoding.GetString(bytes);
    }

    private static string CanonicalSha256(byte[] bytes) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

public sealed class CSharpDeclarationObservationLimits
{
    public CSharpDeclarationObservationLimits(int maxDeclarations)
    {
        MaxDeclarations = maxDeclarations > 0
            ? maxDeclarations
            : throw new ArgumentOutOfRangeException(nameof(maxDeclarations), maxDeclarations, "Declaration limit must be positive.");
    }

    public int MaxDeclarations { get; }
    public static CSharpDeclarationObservationLimits Default { get; } = new(2048);
}

public sealed class CSharpDeclarationObservationResult
{
    public CSharpDeclarationObservationResult(IEnumerable<AtlasDeclaration> declarations, AtlasCompletionState completion, AtlasBounds bounds, IEnumerable<string> limitations)
    {
        Declarations = declarations.ToImmutableArray();
        Completion = AtlasBounds.Defined(completion, nameof(completion));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Limitations = limitations.Select(static value => AtlasIdentityCodec.RequiredToken(value, nameof(limitations))).ToImmutableArray();
    }

    public ImmutableArray<AtlasDeclaration> Declarations { get; }
    public AtlasCompletionState Completion { get; }
    public AtlasBounds Bounds { get; }
    public ImmutableArray<string> Limitations { get; }
}

public static class CSharpDeclarationObservation
{
    private static readonly ActivitySource Telemetry = new("aide.atlas.declarations");

    public static CSharpDeclarationObservationResult Observe(
        CSharpCompilation compilation,
        AtlasCompilationScope context,
        IEnumerable<VerifiedSourceBuffer> sources,
        CSharpDeclarationObservationLimits? limits = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sources);
        limits ??= CSharpDeclarationObservationLimits.Default;
        using var activity = Telemetry.StartActivity("atlas.declarations.observe");
        var declarations = new List<AtlasDeclaration>();
        var limitations = new List<string>();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceMap = MapSources(compilation, sources.ToImmutableArray(), cancellationToken);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var type in SourceTypes(compilation.GlobalNamespace).OrderBy(static t => t.MetadataName, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!AddType(type, context, sourceMap, declarations, limitations, seen, limits.MaxDeclarations))
                {
                    return Complete(declarations, limitations, AtlasCompletionState.BudgetExceeded, limits.MaxDeclarations);
                }

                if (!AddMembers(type, context, sourceMap, declarations, limitations, seen, limits.MaxDeclarations, cancellationToken))
                {
                    return Complete(declarations, limitations, AtlasCompletionState.BudgetExceeded, limits.MaxDeclarations);
                }
            }

            activity?.SetTag("atlas.declarations.count", declarations.Count);
            activity?.SetTag("atlas.declarations.limitations", limitations.Count);
            return Complete(declarations, limitations, AtlasCompletionState.Complete, limits.MaxDeclarations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            limitations.Add("canceled");
            activity?.SetTag("atlas.declarations.canceled", true);
            return Complete(declarations, limitations, AtlasCompletionState.Canceled, limits.MaxDeclarations);
        }
    }

    private static bool AddType(INamedTypeSymbol type, AtlasCompilationScope context, IReadOnlyDictionary<SyntaxTree, VerifiedSourceBuffer> sourceMap, List<AtlasDeclaration> declarations, List<string> limitations, HashSet<string> seen, int maxDeclarations) =>
        AddSymbolReferences(type, AtlasDeclarationKind.Type, AtlasDeclarationRole.Ordinary, context, sourceMap, declarations, limitations, seen, maxDeclarations);

    private static bool AddMembers(INamedTypeSymbol type, AtlasCompilationScope context, IReadOnlyDictionary<SyntaxTree, VerifiedSourceBuffer> sourceMap, List<AtlasDeclaration> declarations, List<string> limitations, HashSet<string> seen, int maxDeclarations, CancellationToken cancellationToken)
    {
        foreach (var member in type.GetMembers().OrderBy(static m => m.MetadataName, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (member)
            {
                case INamedTypeSymbol nested:
                    if (!AddType(nested, context, sourceMap, declarations, limitations, seen, maxDeclarations)
                        || !AddMembers(nested, context, sourceMap, declarations, limitations, seen, maxDeclarations, cancellationToken))
                    {
                        return false;
                    }
                    break;
                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                    if (!AddOrdinaryMethod(method, context, sourceMap, declarations, limitations, seen, maxDeclarations)) return false;
                    break;
                case IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } constructor:
                    if (!AddSymbolReferences(constructor, AtlasDeclarationKind.Constructor, AtlasDeclarationRole.Ordinary, context, sourceMap, declarations, limitations, seen, maxDeclarations)) return false;
                    break;
                case IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet }:
                    break;
                case IMethodSymbol method:
                    limitations.Add("unsupported-method-kind:" + method.MethodKind);
                    break;
                case IPropertySymbol property:
                    if (!AddProperty(property, context, sourceMap, declarations, limitations, seen, maxDeclarations)) return false;
                    break;
            }
        }

        return true;
    }

    private static bool AddOrdinaryMethod(IMethodSymbol method, AtlasCompilationScope context, IReadOnlyDictionary<SyntaxTree, VerifiedSourceBuffer> sourceMap, List<AtlasDeclaration> declarations, List<string> limitations, HashSet<string> seen, int maxDeclarations)
    {
        var symbols = new List<IMethodSymbol> { method };
        if (method.PartialDefinitionPart is not null) symbols.Add(method.PartialDefinitionPart);
        if (method.PartialImplementationPart is not null) symbols.Add(method.PartialImplementationPart);
        foreach (var symbol in symbols.Cast<ISymbol>().Distinct(SymbolEqualityComparer.Default).OfType<IMethodSymbol>())
        {
            var role = symbol.PartialImplementationPart is not null
                ? AtlasDeclarationRole.PartialDefinition
                : symbol.PartialDefinitionPart is not null ? AtlasDeclarationRole.PartialImplementation : AtlasDeclarationRole.Ordinary;
            if (!AddSymbolReferences(symbol, AtlasDeclarationKind.Method, role, context, sourceMap, declarations, limitations, seen, maxDeclarations))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AddProperty(IPropertySymbol property, AtlasCompilationScope context, IReadOnlyDictionary<SyntaxTree, VerifiedSourceBuffer> sourceMap, List<AtlasDeclaration> declarations, List<string> limitations, HashSet<string> seen, int maxDeclarations)
    {
        if (!AddSymbolReferences(property, AtlasDeclarationKind.Property, AtlasDeclarationRole.Ordinary, context, sourceMap, declarations, limitations, seen, maxDeclarations))
        {
            return false;
        }

        foreach (var accessor in new[] { property.GetMethod, property.SetMethod }.Where(static accessor => null != accessor).Cast<IMethodSymbol>())
        {
            if (!AddSymbolReferences(accessor, AtlasDeclarationKind.Accessor, AtlasDeclarationRole.Ordinary, context, sourceMap, declarations, limitations, seen, maxDeclarations))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AddSymbolReferences(ISymbol symbol, AtlasDeclarationKind kind, AtlasDeclarationRole role, AtlasCompilationScope context, IReadOnlyDictionary<SyntaxTree, VerifiedSourceBuffer> sourceMap, List<AtlasDeclaration> declarations, List<string> limitations, HashSet<string> seen, int maxDeclarations)
    {
        if (symbol.DeclaringSyntaxReferences.Length == 0)
        {
            limitations.Add("synthesized-member-skipped");
            return true;
        }

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences.OrderBy(static reference => reference.Span.Start))
        {
            if (declarations.Count >= maxDeclarations)
            {
                limitations.Add("declaration-limit");
                return false;
            }

            var syntax = syntaxReference.GetSyntax();
            if (!sourceMap.TryGetValue(syntax.SyntaxTree, out var buffer))
            {
                throw new InvalidOperationException("Declaration syntax tree is not bound to a verified source buffer.");
            }

            var identifier = IdentifierToken(syntax);
            var unique = string.Create(CultureInfo.InvariantCulture, $"{buffer.SourceObservation.ObservationKey}:{syntax.Span.Start}:{syntax.Span.Length}:{kind}:{role}");
            if (!seen.Add(unique))
            {
                continue;
            }

            var logicalValue = LogicalIdentity(symbol, kind, context, limitations);
            declarations.Add(new AtlasDeclaration(
                ObservationKey(buffer, context, symbol, syntax.Span, kind, role, declarations.Count),
                logicalValue,
                buffer.SourceObservation.ObservationKey,
                context.ObservationKey,
                buffer.Binding,
                kind,
                role,
                DisplaySignature(symbol),
                identifier.ValueText,
                ValidateSpan(identifier.Span, buffer),
                ValidateSpan(syntax.Span, buffer),
                BodySpan(syntax, buffer),
                logicalValue is null ? UnresolvedReason(context) : null));
        }

        return true;
    }

    private static IReadOnlyDictionary<SyntaxTree, VerifiedSourceBuffer> MapSources(CSharpCompilation compilation, ImmutableArray<VerifiedSourceBuffer> sources, CancellationToken cancellationToken)
    {
        var trees = compilation.SyntaxTrees.ToImmutableArray();
        if (trees.Length != sources.Length)
        {
            throw new InvalidOperationException("Compilation syntax trees must match the supplied verified source buffers exactly.");
        }

        var map = new Dictionary<SyntaxTree, VerifiedSourceBuffer>();
        var remaining = sources.ToList();
        foreach (var tree in trees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = tree.GetText(cancellationToken).ToString();
            var matches = remaining.Where(source => string.Equals(source.FullText, text, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Compilation syntax tree text does not map to exactly one verified source buffer.");
            }

            map.Add(tree, matches[0]);
            remaining.Remove(matches[0]);
        }

        return map;
    }

    private static IEnumerable<INamedTypeSymbol> SourceTypes(INamespaceSymbol symbol)
    {
        foreach (var type in symbol.GetTypeMembers())
        {
            foreach (var nested in Flatten(type)) yield return nested;
        }

        foreach (var child in symbol.GetNamespaceMembers())
        {
            foreach (var type in SourceTypes(child)) yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> Flatten(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers().SelectMany(Flatten)) yield return nested;
    }

    private static SyntaxToken IdentifierToken(SyntaxNode syntax) => syntax switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier,
        MethodDeclarationSyntax method => method.Identifier,
        ConstructorDeclarationSyntax constructor => constructor.Identifier,
        PropertyDeclarationSyntax property => property.Identifier,
        AccessorDeclarationSyntax accessor => accessor.Keyword,
        _ => throw new InvalidOperationException("Unsupported declaration syntax."),
    };

    private static AtlasTextSpan? BodySpan(SyntaxNode syntax, VerifiedSourceBuffer buffer) => syntax switch
    {
        TypeDeclarationSyntax type when !type.OpenBraceToken.IsMissing && !type.CloseBraceToken.IsMissing => ValidateBounds(type.OpenBraceToken.SpanStart, type.CloseBraceToken.Span.End, buffer),
        MethodDeclarationSyntax { Body: not null } method => ValidateSpan(method.Body.Span, buffer),
        MethodDeclarationSyntax { ExpressionBody: not null } method => ValidateSpan(method.ExpressionBody.Span, buffer),
        ConstructorDeclarationSyntax { Body: not null } constructor => ValidateSpan(constructor.Body.Span, buffer),
        ConstructorDeclarationSyntax { ExpressionBody: not null } constructor => ValidateSpan(constructor.ExpressionBody.Span, buffer),
        PropertyDeclarationSyntax { AccessorList: not null } property => ValidateSpan(property.AccessorList.Span, buffer),
        PropertyDeclarationSyntax { ExpressionBody: not null } property => ValidateSpan(property.ExpressionBody.Span, buffer),
        AccessorDeclarationSyntax { Body: not null } accessor => ValidateSpan(accessor.Body.Span, buffer),
        AccessorDeclarationSyntax { ExpressionBody: not null } accessor => ValidateSpan(accessor.ExpressionBody.Span, buffer),
        _ => null,
    };

    private static AtlasTextSpan ValidateSpan(TextSpan span, VerifiedSourceBuffer buffer) =>
        ValidateBounds(span.Start, span.End, buffer);

    private static AtlasTextSpan ValidateBounds(int start, int end, VerifiedSourceBuffer buffer)
    {
        if (start < 0 || end < start || end > buffer.FullText.Length)
        {
            throw new InvalidOperationException("Declaration span is outside the verified source text.");
        }

        return new(start, end - start);
    }

    private static string? LogicalIdentity(ISymbol symbol, AtlasDeclarationKind kind, AtlasCompilationScope context, List<string> limitations)
    {
        if (context.Kind is not AtlasCompilationContextKind.SuppliedProjectCompilation || context.LogicalIdentity is null)
        {
            return null;
        }

        try
        {
            return kind is AtlasDeclarationKind.Type && symbol is INamedTypeSymbol type
                ? AtlasIdentity.ForType(context.LogicalIdentity, context.ProjectDisplay, context.TargetFrameworkDisplay, type).Value
                : AtlasIdentity.ForMember(context.LogicalIdentity, context.ProjectDisplay, context.TargetFrameworkDisplay, symbol).Value;
        }
        catch (ArgumentException)
        {
            limitations.Add("logical-identity-unavailable");
            return null;
        }
    }

    private static string UnresolvedReason(AtlasCompilationScope context) =>
        context.Kind is AtlasCompilationContextKind.FileLimited
            ? "file-limited-logical-context-not-established"
            : "logical-identity-unavailable";

    private static string DisplaySignature(ISymbol symbol) => symbol switch
    {
        IMethodSymbol { MethodKind: MethodKind.StaticConstructor } method => "static " + method.ContainingType.Name + "()",
        IMethodSymbol { MethodKind: MethodKind.Constructor } method => method.ContainingType.Name + "(" + string.Join(", ", method.Parameters.Select(static p => p.Type.Name + " " + p.Name)) + ")",
        IMethodSymbol { MethodKind: MethodKind.PropertyGet } method => "get " + method.AssociatedSymbol?.Name,
        IMethodSymbol { MethodKind: MethodKind.PropertySet } method => "set " + method.AssociatedSymbol?.Name,
        _ => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
    };

    private static string ObservationKey(VerifiedSourceBuffer buffer, AtlasCompilationScope context, ISymbol symbol, TextSpan span, AtlasDeclarationKind kind, AtlasDeclarationRole role, int sequence) =>
        AtlasIdentityCodec.EncodeComponents(
            "atlas-declaration-observation/v1",
            context.ObservationKey,
            buffer.SourceObservation.ObservationKey,
            buffer.SourceObservation.FileValue,
            buffer.SourceObservation.CanonicalSha256!,
            buffer.SourceObservation.DecoderId!,
            kind.ToString(),
            role.ToString(),
            DocumentationCommentId.CreateDeclarationId(symbol) ?? symbol.MetadataName,
            span.Start.ToString(CultureInfo.InvariantCulture),
            span.Length.ToString(CultureInfo.InvariantCulture),
            sequence.ToString(CultureInfo.InvariantCulture));

    private static CSharpDeclarationObservationResult Complete(List<AtlasDeclaration> declarations, List<string> limitations, AtlasCompletionState completion, int requestedLimit)
    {
        var distinctLimitations = limitations.Distinct(StringComparer.Ordinal).ToImmutableArray();
        var bounds = completion is AtlasCompletionState.Complete
            ? new AtlasBounds(requestedLimit, requestedLimit, declarations.Count, 0, declarations.Count, AtlasDenominatorState.Known, null, null)
            : new AtlasBounds(requestedLimit, requestedLimit, declarations.Count, 0, null, AtlasDenominatorState.Unknown, completion.ToString(), "declarations");
        return new(declarations, completion, bounds, distinctLimitations);
    }
}


