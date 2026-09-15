using Microsoft.CodeAnalysis;

namespace AiDe.Core.Understanding;

/// <summary>
/// Revision-independent Atlas logical identity for a source-declared type or supported source
/// member. Opaque caller tokens are preserved ordinally; the encoding never normalizes text.
/// </summary>
public sealed class AtlasIdentity : IEquatable<AtlasIdentity>
{
    private AtlasIdentity(string value) => Value = value;

    public string Value { get; }

    public static AtlasIdentity ForType(
        string scope,
        string project,
        string targetFramework,
        INamedTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        EnsureSourceDeclared(type, nameof(type));
        return FromSymbol("type", scope, project, targetFramework, type);
    }

    public static AtlasIdentity ForMember(
        string scope,
        string project,
        string targetFramework,
        ISymbol member)
    {
        ArgumentNullException.ThrowIfNull(member);
        EnsureSupportedMember(member);
        EnsureSourceDeclared(member, nameof(member));
        return FromSymbol("member", scope, project, targetFramework, member);
    }

    public bool Equals(AtlasIdentity? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is AtlasIdentity other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(AtlasIdentity? left, AtlasIdentity? right) => Equals(left, right);

    public static bool operator !=(AtlasIdentity? left, AtlasIdentity? right) => !Equals(left, right);

    private static AtlasIdentity FromSymbol(
        string symbolContext,
        string scope,
        string project,
        string targetFramework,
        ISymbol symbol)
    {
        symbol = CanonicalSymbol(symbol);
        return new AtlasIdentity(AtlasIdentityCodec.ForSymbol(symbolContext, scope, project, targetFramework, symbol));
    }

    private static ISymbol CanonicalSymbol(ISymbol symbol) =>
        symbol is IMethodSymbol { PartialImplementationPart: not null } definition
            ? definition.PartialImplementationPart
            : symbol;

    private static void EnsureSupportedMember(ISymbol member)
    {
        if (member is IPropertySymbol)
        {
            return;
        }

        if (member is not IMethodSymbol method)
        {
            throw new ArgumentException("Only source methods and properties are supported.", nameof(member));
        }

        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.PropertyGet or MethodKind.PropertySet))
        {
            throw new ArgumentException("Unsupported method kind.", nameof(member));
        }
    }

    private static void EnsureSourceDeclared(ISymbol symbol, string parameterName)
    {
        if (!symbol.Locations.Any(static location => location.IsInSource))
        {
            throw new ArgumentException("Symbol must have a source declaration.", parameterName);
        }
    }

}
