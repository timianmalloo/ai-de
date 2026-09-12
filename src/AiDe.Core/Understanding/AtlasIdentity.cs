using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AiDe.Core.Understanding;

/// <summary>
/// Revision-independent Atlas logical identity for a source-declared type or supported source
/// member. Opaque caller tokens are preserved ordinally; the encoding never normalizes text.
/// </summary>
public sealed class AtlasIdentity : IEquatable<AtlasIdentity>
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

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
        var assemblyName = symbol.ContainingAssembly?.Identity.Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            throw new ArgumentException("Symbol must have a containing assembly.", nameof(symbol));
        }

        var declarationId = DocumentationCommentId.CreateDeclarationId(symbol);
        if (string.IsNullOrWhiteSpace(declarationId))
        {
            throw new ArgumentException("Symbol must have a compiler declaration identity.", nameof(symbol));
        }

        return new AtlasIdentity(EncodeComponents(
            "atlas-identity/v1",
            symbolContext,
            Required(scope, nameof(scope)),
            Required(project, nameof(project)),
            Required(targetFramework, nameof(targetFramework)),
            Required(symbol.Language, nameof(symbol)),
            symbol.Kind.ToString(),
            Required(assemblyName, nameof(symbol)),
            declarationId));
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

    private static string EncodeComponents(params string[] components)
    {
        var builder = new StringBuilder();
        foreach (var component in components)
        {
            var checkedComponent = ValidUnicode(component, nameof(component));
            var bytes = Utf8.GetBytes(checkedComponent);
            builder
                .Append(bytes.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(Convert.ToBase64String(bytes))
                .Append(';');
        }

        return builder.ToString();
    }

    private static string Required(string value, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        var checkedValue = ValidUnicode(value, name);
        return string.IsNullOrWhiteSpace(checkedValue)
            ? throw new ArgumentException("Value must not be blank.", name)
            : checkedValue;
    }

    private static string ValidUnicode(string value, string name)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (!char.IsSurrogate(current))
            {
                continue;
            }

            if (char.IsHighSurrogate(current)
                && i + 1 < value.Length
                && char.IsLowSurrogate(value[i + 1]))
            {
                i++;
                continue;
            }

            throw new ArgumentException("Value must contain valid Unicode scalar values.", name);
        }

        return value;
    }
}
