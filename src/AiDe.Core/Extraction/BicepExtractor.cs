using System.Text.RegularExpressions;
using AiDe.Core.Facts;

namespace AiDe.Core.Extraction;

/// <summary>
/// The infrastructure extractor — Bicep read as <b>data</b>, never compiled.
/// </summary>
/// <remarks>
/// <para><b>Why not <c>bicep build</c>.</b> Spike D3 measured that compiling repository-supplied
/// input runs repository-supplied logic, and Bicep resolves module references and evaluates template
/// functions at build time. Invoking the compiler on a cloned repository is the same exposure
/// MSBuild was, so the same answer applies: read it.</para>
///
/// <para><b>Measured at parity for what it claims.</b> Against <c>az bicep build</c> on a real
/// 677-line template: 24 of 24 resources, 19 of 19 types, 18 of 18 parameters
/// (<c>spikes/bicep-as-data</c>). What it does <i>not</i> recover is resolved names — 8 of those 24
/// are expressions — and those are disclosed rather than guessed.</para>
///
/// <para><b>The value of an <c>@secure()</c> parameter is never read.</b> Not redacted after the
/// fact: the parameter is recorded as existing and as secret, and its value is never looked at, so
/// there is no path by which it could reach a store, a log or a projection.</para>
/// </remarks>
public sealed partial class BicepExtractor(string extractorVersion = "1.0.0") : IExtractor
{
    public const string ExtractorId = "bicep-extractor";

    public string ScopeKind => "bicep";

    // Captures what follows the '=' so a loop, a condition or an `existing` reference is
    // recognised rather than missed. The previous form required '=' to be followed by '{' on the
    // same line, which silently dropped every `[for ...]` and every `if (...)` resource — a
    // template using them would have reported a smaller infrastructure than it has.
    [GeneratedRegex(
        @"^\s*resource\s+(?<symbol>\w+)\s+'(?<type>[^@']+)@(?<api>[^']+)'\s+(?<existing>existing\s+)?=\s*(?<tail>.*)$",
        RegexOptions.Multiline)]
    private static partial Regex ResourceDeclaration();

    [GeneratedRegex(@"^\s*module\s+(?<symbol>\w+)\s+'(?<path>[^']+)'\s*=", RegexOptions.Multiline)]
    private static partial Regex ModuleDeclaration();

    [GeneratedRegex(@"^\s*param\s+(?<name>\w+)\s+(?<type>\w+)", RegexOptions.Multiline)]
    private static partial Regex ParameterDeclaration();

    [GeneratedRegex(@"^\s*name:\s*(?<name>.+)$", RegexOptions.Multiline)]
    private static partial Regex NameProperty();

    // dependsOn is either a single-line array or a block. Both forms are captured up to the closing
    // bracket, and the SYMBOLS inside are what matter — a dependsOn on an expression is not a
    // resource reference this reader can resolve, and is left out rather than guessed at.
    [GeneratedRegex(@"dependsOn:\s*\[(?<body>[^\]]*)\]", RegexOptions.Multiline)]
    private static partial Regex DependsOnBlock();

    [GeneratedRegex(@"^\s*(?<symbol>[A-Za-z_]\w*)\s*$", RegexOptions.Multiline)]
    private static partial Regex BareSymbol();

    public Task<ExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = Path.GetFullPath(request.RootPath);
        if (!File.Exists(path))
        {
            return Task.FromResult(new ExtractionResult(
                [], Complete: false,
                [new ExtractionDiagnostic("AIDE-BICEP-MISSING", request.RootPath, "no bicep file at this path")]));
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            return Task.FromResult(new ExtractionResult(
                [], Complete: false,
                [new ExtractionDiagnostic("AIDE-BICEP-UNREADABLE", request.ScopeId, ex.Message)]));
        }

        var assertions = new List<EvidenceAssertion>();
        var observedAt = DateTimeOffset.UtcNow;
        var fileName = Path.GetFileName(path);
        var scopeNode = CSharpExtractor.ScopeNodeId(request.ScopeId);
        var unresolved = 0;
        var loops = 0;
        var conditionals = 0;

        Provenance At(int index)
        {
            var line = text.Take(index).Count(c => c == '\n') + 1;
            return new Provenance(fileName, $"{line}:1", ExtractorId, extractorVersion, observedAt);
        }

        EvidenceAssertion Fact(string subject, string predicate, string obj, Provenance provenance) =>
            new(request.ScopeId, request.ArtifactRevision, subject, predicate, obj,
                EvidenceOrigin.Static, VerificationStatus.Verified, provenance);

        // Where each declaration ends, so a dependsOn is attributed to the resource that contains
        // it rather than to whichever declaration happened to come before it in the file.
        var declarationStarts = ResourceDeclaration().Matches(text).Select(m => m.Index)
            .Concat(ModuleDeclaration().Matches(text).Select(m => m.Index))
            .OrderBy(i => i).ToList();

        int declarationEnds(int start)
        {
            var next = declarationStarts.FirstOrDefault(i => i > start, -1);
            return next < 0 ? text.Length : next;
        }

        foreach (Match match in ResourceDeclaration().Matches(text))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = match.Groups["symbol"].Value;
            var node = $"{request.ScopeId}/{symbol}";
            var provenance = At(match.Index);

            var tail = match.Groups["tail"].Value.TrimStart();
            var isExisting = match.Groups["existing"].Success;
            var isLoop = tail.StartsWith("[for", StringComparison.Ordinal);
            var isConditional = tail.StartsWith("if", StringComparison.Ordinal);

            assertions.Add(Fact(node, "has_type", "azure-resource", provenance));
            assertions.Add(Fact(node, "resource_type", match.Groups["type"].Value, provenance));
            assertions.Add(Fact(node, "api_version", match.Groups["api"].Value, provenance));
            assertions.Add(Fact(node, "declared_in", request.ScopeId, provenance));

            // `existing` REFERENCES a resource this template does not deploy. Recorded distinctly,
            // because "this template creates a SQL server" and "this template talks to one someone
            // else creates" are different facts about ownership.
            if (isExisting) assertions.Add(Fact(node, "is_existing_reference", "true", provenance));

            // A loop declares an unknown NUMBER of resources — one per item in a collection this
            // reader does not evaluate. The declaration is one row; how many it becomes is not
            // knowable here, so it is stated rather than counted as one.
            if (isLoop)
            {
                assertions.Add(Fact(node, "is_loop", "true", provenance));
                loops++;
            }

            // A conditional resource may or may not be deployed, and the condition is an expression.
            if (isConditional)
            {
                assertions.Add(Fact(node, "is_conditional", "true", provenance));
                conditionals++;
            }

            // The resource GRAPH, not just the resource list. dependsOn is the only place a Bicep
            // template states deployment order outright, and it is the edge a C4 view is made of.
            foreach (var dependency in DependenciesAfter(text, match.Index, declarationEnds))
            {
                assertions.Add(Fact(node, "depends_on", $"{request.ScopeId}/{dependency}", provenance));
            }

            var name = NameAfter(text, match.Index);
            if (name.Length == 0) continue;

            // A literal name is a fact. An expression is a fact ABOUT an expression — recorded
            // verbatim so a reader can see what it is, and never resolved. A guessed name would be
            // a confident wrong edge between a table and a server, and the user would act on it.
            if (IsLiteral(name))
            {
                assertions.Add(Fact(node, "resource_name", Unquote(name), provenance));
            }
            else
            {
                unresolved++;
                assertions.Add(Fact(node, "resource_name_expression", name, provenance));
            }
        }

        foreach (Match match in ModuleDeclaration().Matches(text))
        {
            var symbol = match.Groups["symbol"].Value;
            var node = $"{request.ScopeId}/{symbol}";
            var provenance = At(match.Index);

            assertions.Add(Fact(node, "has_type", "azure-module", provenance));
            assertions.Add(Fact(node, "module_path", match.Groups["path"].Value, provenance));
            assertions.Add(Fact(node, "declared_in", request.ScopeId, provenance));
        }

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var match = ParameterDeclaration().Match(lines[i]);
            if (!match.Success) continue;

            var name = match.Groups["name"].Value;
            var node = $"{request.ScopeId}#{name}";
            var provenance = new Provenance(fileName, $"{i + 1}:1", ExtractorId, extractorVersion, observedAt);

            assertions.Add(Fact(node, "has_type", "azure-parameter", provenance));
            assertions.Add(Fact(node, "parameter_type", match.Groups["type"].Value, provenance));
            assertions.Add(Fact(node, "declared_in", request.ScopeId, provenance));

            if (IsSecure(lines, i))
            {
                // Recorded as SECRET, never with a value. There is no code path here that reads one.
                assertions.Add(Fact(node, "is_secret", "true", provenance));
            }
        }

        var scopeProvenance = new Provenance(fileName, "1:1", ExtractorId, extractorVersion, observedAt);

        if (unresolved > 0)
        {
            assertions.Add(Fact(
                scopeNode, CSharpExtractor.DisclosurePredicate,
                ExtractionDisclosures.BicepExpressionsNotEvaluated, scopeProvenance));
        }

        // How many resources a loop actually deploys depends on a collection nobody here evaluates,
        // and whether a conditional one is deployed depends on an expression. Both are disclosed so
        // a resource count is never mistaken for a deployment count.
        if (loops > 0 || conditionals > 0)
        {
            assertions.Add(Fact(
                scopeNode, CSharpExtractor.DisclosurePredicate,
                ExtractionDisclosures.BicepResourceCountIndeterminate, scopeProvenance));
        }

        return Task.FromResult(new ExtractionResult(assertions, Complete: true, []));
    }

    /// <summary>The symbols a declaration's own <c>dependsOn</c> names.</summary>
    /// <remarks>
    /// Bounded to this declaration's span. Searching forward without a bound attributes a later
    /// resource's dependencies to an earlier one, which produces edges that are individually
    /// plausible and collectively a fiction.
    /// </remarks>
    private static IEnumerable<string> DependenciesAfter(string text, int start, Func<int, int> endOf)
    {
        var end = endOf(start);
        var match = DependsOnBlock().Match(text, start);
        if (!match.Success || match.Index >= end) yield break;

        foreach (Match symbol in BareSymbol().Matches(match.Groups["body"].Value))
        {
            yield return symbol.Groups["symbol"].Value;
        }
    }

    private static string NameAfter(string text, int declarationIndex)
    {
        var match = NameProperty().Match(text, declarationIndex);
        return match.Success ? match.Groups["name"].Value.Trim() : string.Empty;
    }

    private static bool IsLiteral(string name) => !name.Contains('$') && !name.Contains('(');

    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '\'' && value[^1] == '\'' ? value[1..^1] : value;

    /// <summary>Whether a <c>@secure()</c> decorator sits above this parameter.</summary>
    /// <remarks>
    /// Scanned backwards past other decorators and doc lines, stopping at the first line that is
    /// clearly not part of this declaration's preamble — so a <c>@secure()</c> belonging to the
    /// PREVIOUS parameter is not attributed to this one.
    /// </remarks>
    private static bool IsSecure(string[] lines, int declarationLine)
    {
        for (var back = declarationLine - 1; back >= 0 && back >= declarationLine - 6; back--)
        {
            var previous = lines[back].Trim();
            if (previous.StartsWith("@secure", StringComparison.Ordinal)) return true;
            if (previous.Length == 0) continue;
            if (!previous.StartsWith('@') && !previous.StartsWith("'''", StringComparison.Ordinal)) return false;
        }

        return false;
    }
}
