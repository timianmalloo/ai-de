using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AiDe.Core.Understanding;

/// <summary>
/// Encodes Atlas identity tuples as UTF-8 byte-length-prefixed Base64 components.
/// Components are ordinal and are never trimmed, case-folded, or Unicode-normalized.
/// </summary>
public static class AtlasIdentityCodec
{
    private const string IdentityTag = "atlas-identity/v1";
    private const string FileTag = "atlas-file/v1";
    private const string CompilationScopeTag = "atlas-compilation-scope/v1";
    private const string NativeObjectTag = "native-object/v1";
    private const string NativeObjectProvider = "provider:windows-file-id";
    private const string NativeObjectVersion = "version:1";
    internal const string MissingProjectToken = "project:not-established";
    internal const string MissingTargetFrameworkToken = "tfm:not-established";
    internal const string UnknownProfileToken = "profile:unknown";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Encodes supplied components without changing ordinal values.</summary>
    public static string EncodeComponents(params string[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (components.Length == 0)
        {
            throw new ArgumentException("At least one component is required.", nameof(components));
        }

        var builder = new StringBuilder();
        foreach (var component in components)
        {
            var checkedComponent = RequiredToken(component, nameof(components));
            var bytes = Utf8.GetBytes(checkedComponent);
            builder
                .Append(bytes.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(Convert.ToBase64String(bytes))
                .Append(';');
        }

        return builder.ToString();
    }

    /// <summary>Builds an unchanged source-symbol identity value from Roslyn's declaration identity.</summary>
    internal static string ForSymbol(string symbolContext, string scope, string project, string targetFramework, ISymbol symbol) =>
        EncodeComponents(
            IdentityTag,
            RequiredToken(symbolContext, nameof(symbolContext)),
            RequiredToken(scope, nameof(scope)),
            RequiredToken(project, nameof(project)),
            RequiredToken(targetFramework, nameof(targetFramework)),
            RequiredToken(symbol.Language, nameof(symbol)),
            symbol.Kind.ToString(),
            RequiredToken(symbol.ContainingAssembly?.Identity.Name, nameof(symbol)),
            RequiredToken(DocumentationCommentId.CreateDeclarationId(symbol), nameof(symbol)));

    /// <summary>Builds a domain-tagged file tuple for an observed file.</summary>
    public static string ForFile(string workspace, string root, string relativePath) =>
        EncodeComponents(FileTag, workspace, root, relativePath);

    /// <summary>Builds the opaque binding token for a native object identity in this Windows candidate.</summary>
    public static string ForNativeObject(AtlasObjectIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return EncodeComponents(NativeObjectTag, NativeObjectProvider, NativeObjectVersion, identity.VolumeSerial, identity.FileIndex);
    }

    /// <summary>
    /// Builds a compilation-scope tuple. File-limited scope uses explicit absence tokens for project
    /// and target framework so it cannot compare equal to a later project compilation.
    /// </summary>
    public static string ForCompilationScope(
        string workspace,
        string root,
        AtlasCompilationContextKind contextKind,
        string projectToken,
        string frameworkToken,
        string configurationOrProfileToken)
    {
        var contextToken = contextKind switch
        {
            AtlasCompilationContextKind.FileLimited => "file-limited",
            AtlasCompilationContextKind.SuppliedProjectCompilation => "supplied-project-compilation",
            _ => throw new ArgumentOutOfRangeException(nameof(contextKind), contextKind, "Unsupported compilation context kind."),
        };

        if (contextKind is AtlasCompilationContextKind.FileLimited
            && (!string.Equals(projectToken, MissingProjectToken, StringComparison.Ordinal)
                || !string.Equals(frameworkToken, MissingTargetFrameworkToken, StringComparison.Ordinal)))
        {
            throw new ArgumentException("FileLimited context must use explicit project/TFM absence tokens.", nameof(projectToken));
        }

        if (contextKind is AtlasCompilationContextKind.SuppliedProjectCompilation
            && (string.Equals(projectToken, MissingProjectToken, StringComparison.Ordinal)
                || string.Equals(frameworkToken, MissingTargetFrameworkToken, StringComparison.Ordinal)))
        {
            throw new ArgumentException("SuppliedProjectCompilation requires established project and target framework tokens.", nameof(projectToken));
        }

        return EncodeComponents(
            CompilationScopeTag,
            workspace,
            root,
            contextToken,
            projectToken,
            frameworkToken,
            configurationOrProfileToken);
    }

    internal static string RequiredToken(string? value, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        var checkedValue = ValidUnicode(value, name);
        return string.IsNullOrWhiteSpace(checkedValue)
            ? throw new ArgumentException("Value must not be blank.", name)
            : checkedValue;
    }

    internal static string? OptionalToken(string? value, string name) =>
        value is null ? null : RequiredToken(value, name);

    internal static string ValidUnicode(string value, string name)
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

/// <summary>Closed set of compilation contexts admitted by the Atlas identity codec.</summary>
public enum AtlasCompilationContextKind
{
    FileLimited,
    SuppliedProjectCompilation,
}

/// <summary>
/// Compilation context identity plus display facts. File-limited contexts report project and TFM as
/// not established; an unknown profile has no logical identity and supports observation navigation only.
/// </summary>
public sealed class AtlasCompilationScope
{
    private AtlasCompilationScope(
        AtlasCompilationContextKind kind,
        string observationKey,
        string? logicalIdentity,
        string projectDisplay,
        string targetFrameworkDisplay,
        string configurationOrProfileToken)
    {
        Kind = kind;
        ObservationKey = observationKey;
        LogicalIdentity = logicalIdentity;
        ProjectDisplay = projectDisplay;
        TargetFrameworkDisplay = targetFrameworkDisplay;
        ConfigurationOrProfileToken = configurationOrProfileToken;
    }

    public AtlasCompilationContextKind Kind { get; }
    public string ObservationKey { get; }
    public string? LogicalIdentity { get; }
    public string ProjectDisplay { get; }
    public string TargetFrameworkDisplay { get; }
    public string ConfigurationOrProfileToken { get; }

    public static AtlasCompilationScope ForFileLimited(string workspace, string root, string? profileToken)
    {
        var profile = profileToken is null
            ? AtlasIdentityCodec.UnknownProfileToken
            : RequirePrefix(profileToken, "profile:", nameof(profileToken));
        var key = AtlasIdentityCodec.ForCompilationScope(
            workspace,
            root,
            AtlasCompilationContextKind.FileLimited,
            AtlasIdentityCodec.MissingProjectToken,
            AtlasIdentityCodec.MissingTargetFrameworkToken,
            profile);

        return new(
            AtlasCompilationContextKind.FileLimited,
            key,
            string.Equals(profile, AtlasIdentityCodec.UnknownProfileToken, StringComparison.Ordinal) ? null : key,
            "not established",
            "not established",
            profile);
    }

    public static AtlasCompilationScope ForSuppliedProjectCompilation(
        string workspace,
        string root,
        string projectToken,
        string targetFrameworkToken,
        string configurationToken)
    {
        var checkedProject = RequirePrefix(projectToken, "project:", nameof(projectToken));
        var checkedFramework = RequirePrefix(targetFrameworkToken, "tfm:", nameof(targetFrameworkToken));
        var checkedConfiguration = RequirePrefix(configurationToken, "configuration:", nameof(configurationToken));
        var key = AtlasIdentityCodec.ForCompilationScope(
            workspace,
            root,
            AtlasCompilationContextKind.SuppliedProjectCompilation,
            checkedProject,
            checkedFramework,
            checkedConfiguration);

        return new(
            AtlasCompilationContextKind.SuppliedProjectCompilation,
            key,
            key,
            checkedProject,
            checkedFramework,
            checkedConfiguration);
    }

    private static string RequirePrefix(string value, string prefix, string name)
    {
        var checkedValue = AtlasIdentityCodec.RequiredToken(value, name);
        return checkedValue.StartsWith(prefix, StringComparison.Ordinal)
            ? checkedValue
            : throw new ArgumentException($"Value must use the '{prefix}' namespace.", name);
    }
}
