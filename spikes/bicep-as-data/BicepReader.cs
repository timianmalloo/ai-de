using System.Text.RegularExpressions;

namespace BicepAsDataSpike;

/// <summary>One declaration read out of a Bicep file, with its expression left unevaluated.</summary>
internal sealed record BicepResource(string Symbol, string Type, string ApiVersion, string NameExpression)
{
    /// <summary>Whether the name is a literal, or an expression only the compiler could resolve.</summary>
    internal bool NameIsLiteral => !NameExpression.Contains('$') && !NameExpression.Contains('(');
}

internal sealed record BicepParameter(string Name, string Type, bool IsSecure);

internal sealed record BicepFile(
    IReadOnlyList<BicepResource> Resources,
    IReadOnlyList<BicepResource> Modules,
    IReadOnlyList<BicepParameter> Parameters,
    IReadOnlyList<string> DependsOn);

/// <summary>
/// Reads a <c>.bicep</c> file as <b>data</b>, without the compiler.
/// </summary>
/// <remarks>
/// <para><b>Why not just run <c>bicep build</c>.</b> Spike D3 measured that compiling
/// repository-supplied input runs repository-supplied logic; Bicep supports module references that
/// resolve over the network and template functions evaluated at build time, so invoking the compiler
/// on a cloned repository is the same shape of exposure MSBuild was. Phase 2 chose to read rather
/// than build, and that choice has to hold here or it was never a principle.</para>
///
/// <para><b>Regex, deliberately, and only for declarations.</b> Bicep's declaration syntax is
/// line-oriented and regular — <c>resource &lt;symbol&gt; '&lt;type&gt;@&lt;version&gt;' = {</c>.
/// Its EXPRESSION language is not, and this makes no attempt on it: an unresolved name is kept
/// verbatim and reported as an expression rather than guessed at. `simplify: declaration-level regex
/// rather than a Bicep grammar; ceiling is declarations and their types; upgrade trigger = a join
/// requires a resolved expression.`</para>
/// </remarks>
internal static partial class BicepReader
{
    [GeneratedRegex(@"^\s*resource\s+(?<symbol>\w+)\s+'(?<type>[^@']+)@(?<api>[^']+)'\s*=", RegexOptions.Multiline)]
    private static partial Regex ResourceDeclaration();

    [GeneratedRegex(@"^\s*module\s+(?<symbol>\w+)\s+'(?<path>[^']+)'\s*=", RegexOptions.Multiline)]
    private static partial Regex ModuleDeclaration();

    [GeneratedRegex(@"^\s*param\s+(?<name>\w+)\s+(?<type>\w+)", RegexOptions.Multiline)]
    private static partial Regex ParameterDeclaration();

    [GeneratedRegex(@"^\s*name:\s*(?<name>.+)$", RegexOptions.Multiline)]
    private static partial Regex NameProperty();

    internal static BicepFile Read(string path)
    {
        var text = File.ReadAllText(path);
        var lines = text.Split('\n');

        var resources = new List<BicepResource>();
        foreach (Match match in ResourceDeclaration().Matches(text))
        {
            resources.Add(new BicepResource(
                match.Groups["symbol"].Value,
                match.Groups["type"].Value,
                match.Groups["api"].Value,
                NameAfter(text, match.Index)));
        }

        var modules = new List<BicepResource>();
        foreach (Match match in ModuleDeclaration().Matches(text))
        {
            modules.Add(new BicepResource(
                match.Groups["symbol"].Value,
                "module:" + match.Groups["path"].Value,
                string.Empty,
                NameAfter(text, match.Index)));
        }

        // @secure() is read so the parameter can be REPORTED as secret — never so its value can be.
        // The decorator sits on the line(s) above the declaration.
        var parameters = new List<BicepParameter>();
        for (var i = 0; i < lines.Length; i++)
        {
            var match = ParameterDeclaration().Match(lines[i]);
            if (!match.Success) continue;

            var secure = false;
            for (var back = i - 1; back >= 0 && back >= i - 6; back--)
            {
                var previous = lines[back].Trim();
                if (previous.StartsWith("@secure", StringComparison.Ordinal)) { secure = true; break; }
                if (previous.Length > 0 && !previous.StartsWith('@') && !previous.StartsWith("'''", StringComparison.Ordinal)) break;
            }

            parameters.Add(new BicepParameter(match.Groups["name"].Value, match.Groups["type"].Value, secure));
        }

        var dependsOn = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("dependsOn", StringComparison.Ordinal)) dependsOn.Add(trimmed);
        }

        return new BicepFile(resources, modules, parameters, dependsOn);
    }

    /// <summary>The first <c>name:</c> after a declaration, kept verbatim.</summary>
    private static string NameAfter(string text, int declarationIndex)
    {
        var match = NameProperty().Match(text, declarationIndex);
        return match.Success ? match.Groups["name"].Value.Trim() : string.Empty;
    }
}
