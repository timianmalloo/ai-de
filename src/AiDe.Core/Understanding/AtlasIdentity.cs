using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AiDe.Core.Understanding;

public readonly record struct AtlasIdentity
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private AtlasIdentity(string value) => Value = value;

    public string Value { get; }

    public static AtlasIdentity ForType(
        string scope,
        string project,
        string targetFramework,
        INamedTypeSymbol type) =>
        ForSymbol(scope, project, targetFramework, type);

    public static AtlasIdentity ForMember(
        string scope,
        string project,
        string targetFramework,
        ISymbol member) =>
        ForSymbol(scope, project, targetFramework, member);

    public override string ToString() => Value ?? string.Empty;

    private static AtlasIdentity ForSymbol(
        string scope,
        string project,
        string targetFramework,
        ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var declarationId = DocumentationCommentId.CreateDeclarationId(symbol);
        if (string.IsNullOrWhiteSpace(declarationId))
        {
            throw new ArgumentException("Symbol must have a compiler declaration identity.", nameof(symbol));
        }

        return new AtlasIdentity(EncodeComponents(
            "atlas-identity/v1",
            Required(scope, nameof(scope)),
            Required(project, nameof(project)),
            Required(targetFramework, nameof(targetFramework)),
            Required(symbol.Language, nameof(symbol)),
            symbol.Kind.ToString(),
            Required(symbol.ContainingAssembly.Identity.Name, nameof(symbol)),
            declarationId));
    }

    private static string EncodeComponents(params string[] components)
    {
        var builder = new StringBuilder();
        foreach (var component in components)
        {
            var bytes = Utf8.GetBytes(component.Normalize(NormalizationForm.FormC));
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
        var normalized = value.Normalize(NormalizationForm.FormC);
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException("Value must not be blank.", name)
            : normalized;
    }
}
