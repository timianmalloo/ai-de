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
internal sealed class VerifiedSourceBuffer : IDisposable
{
    internal const int MaxSourceBytes = 8 * 1024 * 1024;
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] Utf16LeBom = [0xFF, 0xFE];
    private static readonly byte[] Utf16BeBom = [0xFE, 0xFF];
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly UnicodeEncoding StrictUtf16Le = new(false, false, true);
    private static readonly UnicodeEncoding StrictUtf16Be = new(true, false, true);
    private byte[]? rawBytes;
    private string? fullText;

    private VerifiedSourceBuffer(AtlasSourceObservation sourceObservation, AtlasSourceBinding binding, byte[] rawBytes, string fullText)
    {
        SourceObservation = sourceObservation;
        Binding = binding;
        this.rawBytes = rawBytes;
        this.fullText = fullText;
    }

    internal AtlasSourceObservation SourceObservation { get; }
    internal AtlasSourceBinding Binding { get; }
    internal string FullText => fullText ?? throw new ObjectDisposedException(nameof(VerifiedSourceBuffer));
    internal int RawByteLength => rawBytes?.Length ?? throw new ObjectDisposedException(nameof(VerifiedSourceBuffer));

    internal static int GetDecodedUtf16Length(byte[] ownedSnapshot, string decoderId)
    {
        ArgumentNullException.ThrowIfNull(ownedSnapshot);
        AtlasIdentityCodec.RequiredToken(decoderId, nameof(decoderId));
        EnsureWithinByteLimit(ownedSnapshot.Length);
        var text = Decode(ownedSnapshot, decoderId);
        AtlasIdentityCodec.ValidUnicode(text, nameof(ownedSnapshot));
        return text.Length;
    }

    internal static VerifiedSourceBuffer FromBytes(AtlasSourceObservation sourceObservation, AtlasSourceBinding binding, ReadOnlySpan<byte> rawBytes)
    {
        ArgumentNullException.ThrowIfNull(sourceObservation);
        ArgumentNullException.ThrowIfNull(binding);
        EnsureWithinByteLimit(rawBytes.Length);

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

        if (!string.Equals(CanonicalSha256(ownedBytes), sourceObservation.CanonicalSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Raw source bytes do not match the verified content hash.");
        }

        if (sourceObservation.ByteLength != ownedBytes.LongLength)
        {
            throw new InvalidOperationException("Raw source byte length does not match the verified observation.");
        }

        var text = Decode(ownedBytes, sourceObservation.DecoderId!);
        AtlasIdentityCodec.ValidUnicode(text, nameof(rawBytes));
        if (sourceObservation.DecodedUtf16Length != text.Length)
        {
            throw new InvalidOperationException("Decoded UTF-16 length does not match the verified observation.");
        }

        return new(sourceObservation, binding, ownedBytes, text);
    }

    public void Dispose()
    {
        rawBytes = null;
        fullText = null;
    }

    private static void EnsureWithinByteLimit(int byteLength)
    {
        if (byteLength > MaxSourceBytes)
        {
            throw new InvalidOperationException("Verified source input exceeds the 8 MiB request limit.");
        }
    }

    private static string Decode(byte[] bytes, string decoderId) => decoderId switch
    {
        "utf-8" => StartsWith(bytes, Utf8Bom) ? throw new InvalidOperationException("UTF-8 BOM requires decoder utf-8-bom.") : StrictUtf8.GetString(bytes),
        "utf-8-bom" => DecodeAfterPreamble(bytes, Utf8Bom, StrictUtf8, decoderId),
        "utf-16le" or "utf-16be" => throw new InvalidOperationException("UTF-16 source requires an explicit byte-order mark decoder."),
        "utf-16le-bom" => DecodeAfterPreamble(bytes, Utf16LeBom, StrictUtf16Le, decoderId),
        "utf-16be-bom" => DecodeAfterPreamble(bytes, Utf16BeBom, StrictUtf16Be, decoderId),
        _ => throw new InvalidOperationException("Unsupported source decoder."),
    };

    private static string DecodeAfterPreamble(byte[] bytes, byte[] preamble, Encoding encoding, string decoderId)
    {
        if (!StartsWith(bytes, preamble))
        {
            throw new InvalidOperationException($"Decoder {decoderId} requires its byte-order mark.");
        }

        var body = bytes.AsSpan(preamble.Length);
        if (encoding is UnicodeEncoding && body.Length % 2 != 0)
        {
            throw new InvalidOperationException("UTF-16 source bytes must have an even length.");
        }

        return encoding.GetString(body);
    }

    private static bool StartsWith(byte[] bytes, byte[] prefix) =>
        bytes.Length >= prefix.Length && bytes.AsSpan(0, prefix.Length).SequenceEqual(prefix);

    private static string CanonicalSha256(byte[] bytes) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

/// <summary>Core-issued syntax-tree/source association. It validates file metadata without decoding identity tokens.</summary>
internal sealed class CSharpDeclarationSourceInput : IDisposable
{
    private CSharpDeclarationSourceInput(SyntaxTree syntaxTree, AtlasFileEntry file, AtlasRootGrant rootGrant, string canonicalRelativePath, VerifiedSourceBuffer buffer)
    {
        SyntaxTree = syntaxTree;
        File = file;
        RootGrant = rootGrant;
        CanonicalRelativePath = canonicalRelativePath;
        Buffer = buffer;
    }

    internal SyntaxTree SyntaxTree { get; }
    internal AtlasFileEntry File { get; }
    internal AtlasRootGrant RootGrant { get; }
    internal string CanonicalRelativePath { get; }
    internal VerifiedSourceBuffer Buffer { get; }

    internal static CSharpDeclarationSourceInput Create(SyntaxTree syntaxTree, AtlasRootGrant rootGrant, AtlasFileEntry file, AtlasSourceObservation sourceObservation, AtlasSourceBinding binding, ReadOnlySpan<byte> rawBytes, string canonicalRelativePath)
    {
        ArgumentNullException.ThrowIfNull(syntaxTree);
        ArgumentNullException.ThrowIfNull(rootGrant);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(sourceObservation);
        var relativePath = AtlasIdentityCodec.RequiredToken(canonicalRelativePath, nameof(canonicalRelativePath));
        var expectedFileValue = AtlasIdentityCodec.ForFile(rootGrant.WorkspaceToken, rootGrant.RootToken, relativePath);
        if (!Same(expectedFileValue, file.FileValue) || !Same(file.FileValue, sourceObservation.FileValue))
        {
            throw new InvalidOperationException("File value does not match the supplied root/file metadata.");
        }

        if (!Same(file.RelativePath, relativePath) || !MatchesTreePath(syntaxTree.FilePath, rootGrant.ApprovedAbsoluteRoot, relativePath))
        {
            throw new InvalidOperationException("Syntax tree path does not match its intended canonical file.");
        }

        if (!Equals(sourceObservation.RootIdentity, rootGrant.ExpectedNativeRootIdentity))
        {
            throw new InvalidOperationException("Source root identity does not match the grant.");
        }

        if (file.ObservedIdentity is not null && !Equals(sourceObservation.FileIdentity, file.ObservedIdentity))
        {
            throw new InvalidOperationException("Source file identity does not match the file entry.");
        }

        var buffer = VerifiedSourceBuffer.FromBytes(sourceObservation, binding, rawBytes);
        if (!Same(syntaxTree.GetText().ToString(), buffer.FullText))
        {
            buffer.Dispose();
            throw new InvalidOperationException("Compilation syntax tree text does not match the verified source buffer.");
        }

        return new(syntaxTree, file, rootGrant, relativePath, buffer);
    }

    public void Dispose() => Buffer.Dispose();

    private static bool MatchesTreePath(string treePath, string approvedAbsoluteRoot, string relativePath)
    {
        var tree = NormalizeSeparators(AtlasIdentityCodec.RequiredToken(treePath, nameof(treePath)));
        var relative = NormalizeSeparators(relativePath);
        if (Same(tree, relative))
        {
            return true;
        }

        var root = NormalizeSeparators(AtlasIdentityCodec.RequiredToken(approvedAbsoluteRoot, nameof(approvedAbsoluteRoot))).TrimEnd('/');
        return Same(tree, root + "/" + relative);
    }

    private static string NormalizeSeparators(string value) => value.Replace('\\', '/');
    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
}

internal sealed class CSharpDeclarationObservationLimits
{
    internal CSharpDeclarationObservationLimits(int maxDeclarations)
    {
        MaxDeclarations = maxDeclarations > 0
            ? maxDeclarations
            : throw new ArgumentOutOfRangeException(nameof(maxDeclarations), maxDeclarations, "Declaration limit must be positive.");
    }

    internal int MaxDeclarations { get; }
    internal static CSharpDeclarationObservationLimits Default { get; } = new(2048);
}

public sealed class CSharpDeclarationObservationResult
{
    public CSharpDeclarationObservationResult(IEnumerable<AtlasDeclaration> declarations, AtlasCompletionState completion, AtlasBounds bounds, IEnumerable<string> limitations)
    {
        Declarations = declarations.ToImmutableArray();
        Completion = AtlasBounds.Defined(completion, nameof(completion));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Limitations = limitations.Select(static value => AtlasIdentityCodec.RequiredToken(value, nameof(limitations))).Distinct(StringComparer.Ordinal).ToImmutableArray();
    }

    public ImmutableArray<AtlasDeclaration> Declarations { get; }
    public AtlasCompletionState Completion { get; }
    public AtlasBounds Bounds { get; }
    public ImmutableArray<string> Limitations { get; }
}

public static class CSharpDeclarationObservation
{
    private static readonly ActivitySource Telemetry = new("aide.atlas.declarations");
    private const int MaxLimitations = 32;

    internal static CSharpDeclarationObservationResult Observe(CSharpCompilation compilation, AtlasCompilationScope context, IEnumerable<CSharpDeclarationSourceInput> sources, CSharpDeclarationObservationLimits? limits = null, CancellationToken cancellationToken = default)
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
            var sourceMap = MapSources(compilation, sources.ToImmutableArray(), cancellationToken);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tree in compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = sourceMap[tree];
                var semanticModel = compilation.GetSemanticModel(input.SyntaxTree);
                foreach (var node in input.SyntaxTree.GetRoot(cancellationToken).DescendantNodes(descendIntoChildren: static _ => true))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!TryCreateDeclaration(node, input, semanticModel, context, limitations, out var occurrenceKey, out var declaration))
                    {
                        continue;
                    }

                    if (!seen.Add(occurrenceKey))
                    {
                        continue;
                    }

                    if (declarations.Count >= limits.MaxDeclarations)
                    {
                        AddLimitation(limitations, "declaration-limit");
                        return Complete(declarations, limitations, AtlasCompletionState.BudgetExceeded, limits.MaxDeclarations);
                    }

                    declarations.Add(declaration);
                }
            }

            activity?.SetTag("atlas.declarations.count", declarations.Count);
            activity?.SetTag("atlas.declarations.limitations", limitations.Distinct(StringComparer.Ordinal).Count());
            return Complete(declarations, limitations, AtlasCompletionState.Complete, limits.MaxDeclarations);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AddLimitation(limitations, "canceled");
            activity?.SetTag("atlas.declarations.canceled", true);
            return Complete(declarations, limitations, AtlasCompletionState.Canceled, limits.MaxDeclarations);
        }
    }

    private static IReadOnlyDictionary<SyntaxTree, CSharpDeclarationSourceInput> MapSources(CSharpCompilation compilation, ImmutableArray<CSharpDeclarationSourceInput> sources, CancellationToken cancellationToken)
    {
        var trees = new HashSet<SyntaxTree>(compilation.SyntaxTrees, ReferenceEqualityComparer.Instance);
        if (trees.Count != sources.Length)
        {
            throw new InvalidOperationException("Every compilation syntax tree must have exactly one verified source association.");
        }

        var map = new Dictionary<SyntaxTree, CSharpDeclarationSourceInput>(ReferenceEqualityComparer.Instance);
        foreach (var input in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!trees.Contains(input.SyntaxTree) || !map.TryAdd(input.SyntaxTree, input))
            {
                throw new InvalidOperationException("Source association must reference one unique syntax tree from the supplied compilation.");
            }

            _ = input.Buffer.FullText;
        }

        return map;
    }

    private static bool TryCreateDeclaration(SyntaxNode node, CSharpDeclarationSourceInput input, SemanticModel semanticModel, AtlasCompilationScope context, List<string> limitations, out string occurrenceKey, out AtlasDeclaration declaration)
    {
        occurrenceKey = string.Empty;
        declaration = null!;
        var kind = DeclarationKind(node, limitations, out var role);
        if (kind is null)
        {
            return false;
        }

        var symbol = SymbolFor(node, semanticModel);
        if (symbol is null)
        {
            AddLimitation(limitations, "declaration-symbol-unavailable");
            return false;
        }

        if (role is AtlasDeclarationRole.PartialDefinition && symbol is IMethodSymbol { PartialImplementationPart: null })
        {
            AddLimitation(limitations, "partial-definition-without-implementation");
        }

        var identifier = IdentifierToken(node);
        var declarationSpan = ValidateSpan(node.Span, input.Buffer);
        var symbolValue = LogicalIdentity(symbol, kind.Value, context, limitations);
        occurrenceKey = OccurrenceKey(input, context, kind.Value, role, declarationSpan);
        declaration = new AtlasDeclaration(
            occurrenceKey,
            symbolValue,
            input.Buffer.SourceObservation.ObservationKey,
            context.ObservationKey,
            input.Buffer.Binding,
            kind.Value,
            role,
            DisplaySignature(symbol),
            identifier.ValueText,
            ValidateSpan(identifier.Span, input.Buffer),
            declarationSpan,
            BodySpan(node, input.Buffer),
            symbolValue is null ? UnresolvedReason(context) : null);
        return true;
    }

    private static AtlasDeclarationKind? DeclarationKind(SyntaxNode node, List<string> limitations, out AtlasDeclarationRole role)
    {
        role = AtlasDeclarationRole.Ordinary;
        return node switch
        {
            BaseTypeDeclarationSyntax => AtlasDeclarationKind.Type,
            ConstructorDeclarationSyntax => AtlasDeclarationKind.Constructor,
            PropertyDeclarationSyntax => AtlasDeclarationKind.Property,
            AccessorDeclarationSyntax => AtlasDeclarationKind.Accessor,
            MethodDeclarationSyntax method => MethodKind(method, out role),
            OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax or DestructorDeclarationSyntax => Unsupported(limitations, "unsupported-method-declaration"),
            EventDeclarationSyntax or EventFieldDeclarationSyntax or FieldDeclarationSyntax => Unsupported(limitations, "unsupported-member-declaration"),
            _ => null,
        };
    }

    private static AtlasDeclarationKind MethodKind(MethodDeclarationSyntax method, out AtlasDeclarationRole role)
    {
        var isPartial = method.Modifiers.Any(SyntaxKind.PartialKeyword);
        role = isPartial && method.Body is null && method.ExpressionBody is null
            ? AtlasDeclarationRole.PartialDefinition
            : isPartial ? AtlasDeclarationRole.PartialImplementation : AtlasDeclarationRole.Ordinary;
        return AtlasDeclarationKind.Method;
    }

    private static AtlasDeclarationKind? Unsupported(List<string> limitations, string reason)
    {
        AddLimitation(limitations, reason);
        return null;
    }

    private static ISymbol? SymbolFor(SyntaxNode node, SemanticModel semanticModel) => node switch
    {
        BaseTypeDeclarationSyntax type => semanticModel.GetDeclaredSymbol(type),
        ConstructorDeclarationSyntax constructor => semanticModel.GetDeclaredSymbol(constructor),
        MethodDeclarationSyntax method => semanticModel.GetDeclaredSymbol(method),
        PropertyDeclarationSyntax property => semanticModel.GetDeclaredSymbol(property),
        AccessorDeclarationSyntax accessor => semanticModel.GetDeclaredSymbol(accessor),
        _ => null,
    };

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
            AddLimitation(limitations, "logical-identity-unavailable");
            return null;
        }
    }

    private static string UnresolvedReason(AtlasCompilationScope context) =>
        context.Kind is AtlasCompilationContextKind.FileLimited
            ? "file-limited-logical-context-not-established"
            : "logical-identity-unavailable";

    private static string DisplaySignature(ISymbol symbol) => symbol switch
    {
        IMethodSymbol { MethodKind: Microsoft.CodeAnalysis.MethodKind.StaticConstructor } method => "static " + method.ContainingType.Name + "()",
        IMethodSymbol { MethodKind: Microsoft.CodeAnalysis.MethodKind.Constructor } method => method.ContainingType.Name + "(" + string.Join(", ", method.Parameters.Select(static p => p.Type.Name + " " + p.Name)) + ")",
        IMethodSymbol { MethodKind: Microsoft.CodeAnalysis.MethodKind.PropertyGet } method => "get " + method.AssociatedSymbol?.Name,
        IMethodSymbol { MethodKind: Microsoft.CodeAnalysis.MethodKind.PropertySet } method => "set " + method.AssociatedSymbol?.Name,
        _ => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
    };

    private static string OccurrenceKey(CSharpDeclarationSourceInput input, AtlasCompilationScope context, AtlasDeclarationKind kind, AtlasDeclarationRole role, AtlasTextSpan span) =>
        AtlasIdentityCodec.EncodeComponents(
            "atlas-declaration-observation/v1",
            context.ObservationKey,
            input.File.FileValue,
            input.Buffer.SourceObservation.CanonicalSha256!,
            input.Buffer.SourceObservation.DecoderId!,
            kind.ToString(),
            role.ToString(),
            span.Start.ToString(CultureInfo.InvariantCulture),
            span.Length.ToString(CultureInfo.InvariantCulture));

    private static CSharpDeclarationObservationResult Complete(List<AtlasDeclaration> declarations, List<string> limitations, AtlasCompletionState completion, int requestedLimit)
    {
        var distinctLimitations = limitations.Distinct(StringComparer.Ordinal).ToImmutableArray();
        var bounds = completion is AtlasCompletionState.Complete
            ? new AtlasBounds(requestedLimit, requestedLimit, declarations.Count, 0, declarations.Count, AtlasDenominatorState.Known, null, null)
            : new AtlasBounds(requestedLimit, requestedLimit, declarations.Count, 0, null, AtlasDenominatorState.Unknown, completion.ToString(), "declarations");
        return new(declarations, completion, bounds, distinctLimitations);
    }

    private static void AddLimitation(List<string> limitations, string reason)
    {
        if (limitations.Count < MaxLimitations && !limitations.Contains(reason, StringComparer.Ordinal))
        {
            limitations.Add(reason);
        }
    }
}


