using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// Synthetic contract experiment only. No Atlas authorization, production parser, or cloud access.
internal static class Program
{
    private static readonly HashSet<string> Roles = ["entity", "value-object", "aggregate"];
    private static int _checks;
    private static string _fault = "none";

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 0)
            {
                if (args.Length != 2 || args[0] != "--fault" || args[1] is not ("alias-drop" or "alias-wrong-root" or "scope-drop"))
                    throw new ArgumentException("Only named experimental fault injections are accepted.");
                _fault = args[1];
            }
            var valid = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "architecture.fixture.json")))!.AsObject();
            void Negative(string name, Action<JsonObject> mutate, string expected)
            {
                var copy = (JsonObject)valid.DeepClone();
                mutate(copy);
                var result = Validate(copy);
                Assert(name, result.Errors.Contains(expected), string.Join(",", result.Errors));
            }
            // Negatives first: assert specific failures, never merely a thrown exception.
            Negative("duplicate-id", d => d["concepts"]![1]!["id"] = "order", "duplicate-id");
            Negative("unknown-role", d => d["concepts"]![0]!["role"] = "namespace", "unknown-role");
            Negative("invalid-shape", d => d["concepts"] = "wrong", "shape:concepts");
            Negative("invalid-label-shape", d => d["concepts"]![0]!["label"] = 42, "field:label");
            Negative("blank-required-text", d => d["concepts"]![0]!["label"] = "   ", "field:label");
            Negative("text-overflow", d => d["concepts"]![0]!["label"] = new string('x', 257), "field:label");
            Negative("row-overflow", d => { var rows = d["concepts"]!.AsArray(); while (rows.Count <= 32) { var extra = rows[0]!.DeepClone(); extra["id"] = $"extra-{rows.Count}"; rows.Add(extra); } }, "bound:concepts");
            Negative("unknown-layer-kind", d => d["layers"]![0]!["kind"] = "folder", "layer-kind");
            Negative("anchor-authority-injection", d => d["anchors"]![0]!["accepted"] = true, "unknown-field:accepted");
            Negative("missing-aggregate-root", d => d["concepts"]![2]!["root"] = "absent", "aggregate-root");
            Negative("missing-invariant", d => d["concepts"]![2]!["invariants"] = new JsonArray(), "aggregate-invariant");
            Negative("root-outside-members", d => d["concepts"]![2]!["members"] = new JsonArray("money"), "aggregate-root-membership");
            Negative("untyped-membership", d => d["layers"]![0]!["members"] = new JsonArray("invented"), "layer-member");
            Negative("unknown-dimension", d => d["layers"]![0]!["dimension"] = "folder", "layer-dimension");
            Negative("unknown-state", d => d["layers"]![0]!["state"] = "accepted", "layer-state");
            Negative("hostile-acceptance-marker", d => d["accepted"] = true, "unknown-field:accepted");
            Negative("alias-not-declaration", d => d["aliases"]![0]!["rootRef"] = "alias-b", "alias-root");
            foreach (var (name, field, replacement) in new[]
            {
                ("stale-anchor", "hash", "stale"),
                ("missing-target", "target", "missing"),
                ("out-of-scope", "scope", "elsewhere")
            })
            {
                var copy = (JsonObject)valid.DeepClone();
                copy["anchors"]![0]![field] = replacement;
                var result = Validate(copy);
                Assert(name, result.Errors.Count == 0 && result.Unresolved.SetEquals(["a"]),
                    $"unresolved={string.Join(',', result.Unresolved)}");
            }
            var positive = Validate(valid);
            Assert("explicit-domain-layer-positive", positive.Errors.Count == 0 && positive.Unresolved.Count == 0,
                "entity+value-object+aggregate; declared invariant; logical/current and deployment/target");
            var resources = valid["resources"]!.AsArray();
            Assert("equal-symbols-distinct-scopes", DeclarationKey(resources[0]!) != DeclarationKey(resources[1]!)
                && ProjectResources(valid).Roots.Length == 2, "all identity components equal except scope; two produced roots");
            var missingRoot = (JsonObject)valid.DeepClone();
            missingRoot["aliases"]![1]!["rootRef"] = "missing";
            var missingProjection = ProjectResources(missingRoot);
            Assert("missing-root-produces-no-groups", missingProjection.Errors.Contains("alias-root") && missingProjection.Roots.Length == 0,
                "alias-root; no partial output");
            var differentRoot = (JsonObject)valid.DeepClone();
            differentRoot["aliases"]![1]!["rootRef"] = "resource-b";
            var separated = ProjectResources(differentRoot);
            Assert("different-root-not-collapsed", separated.Errors.Length == 0 && separated.Roots.Length == 2
                && separated.Roots.Single(r => r.Id == "resource-a").Aliases.Select(a => a.Id).SequenceEqual(["alias-a"])
                && separated.Roots.Single(r => r.Id == "resource-b").Aliases.Select(a => a.Id).SequenceEqual(["alias-b"]),
                "two roots each retain their own alias");
            var grouped = ProjectResources(valid);
            var group = grouped.Roots.Single(r => r.Id == "resource-a");
            Assert("two-aliases-one-produced-root", grouped.Errors.Length == 0 && grouped.Roots.Length == 2
                && group.Aliases.Select(a => a.Id).SequenceEqual(["alias-a", "alias-b"])
                && group.Aliases.Select(a => a.Anchor.Id).SequenceEqual(["a", "b"])
                && group.Aliases.All(a => a.Anchor.Target == "synthetic:abc" && a.Anchor.Scope == "fixture"
                    && a.Anchor.Hash == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"
                    && a.Anchor.Start == 0 && a.Anchor.Length == 3), "produced root retains both complete alias anchor records");

            const string literal = "targetScope = 'resourceGroup'\nresource store 'Microsoft.Storage/storageAccounts@2023-01-01' = {\n  name: 'example'\n}";
            const string expression = "targetScope = 'resourceGroup'\nresource store 'Microsoft.Storage/storageAccounts@2023-01-01' = {\n  name: nameParameter\n}";
            var a = ReadLiteralBicep(literal);
            var b = ReadLiteralBicep(literal);
            var unknown = ReadLiteralBicep(expression);
            Assert("literal-source-subset", a.Type == "Microsoft.Storage/storageAccounts" && a.Name == "example",
                "literal type/name read as data; targetScope supplies scope KIND only");
            Assert("partial-identity-refused", !SameDeployment(a, b), "identical literal type/name cannot merge without deployment scope");
            Assert("expression-name-unresolved", unknown.Name is null && !SameDeployment(a, unknown), "expression not evaluated");
            Console.WriteLine("UNRESOLVED fully-known-deployment-positive: admitted Bicep subset supplies no actual subscription/resource-group identity; no invented tuple injected.");
            Console.WriteLine($"PASS {_checks} contract checks; six fixture groups observed; full deployment equality remains UNRESOLVED (not US-E8 acceptance).");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL {error.GetType().Name}: {error.Message}");
            return 1;
        }
    }

    private static void Assert(string name, bool condition, string detail)
    {
        if (!condition) throw new InvalidOperationException($"{name}: {detail}");
        _checks++;
        Console.WriteLine($"PASS {name}: {detail}");
    }

    private static string? Text(JsonNode n, string key) => n[key] is JsonValue v
        && v.TryGetValue<string>(out var text) ? text : null;

    private static Result Validate(JsonObject document)
    {
        var result = new Result();
        var allowed = new HashSet<string> { "version", "concepts", "invariants", "layers", "anchors", "resources", "aliases" };
        foreach (var key in document.Select(p => p.Key).Where(k => !allowed.Contains(k))) result.Errors.Add($"unknown-field:{key}");
        if (document["version"] is not JsonValue version || !version.TryGetValue<int>(out var v) || v != 1) result.Errors.Add("version");
        var arrays = new Dictionary<string, JsonArray>();
        foreach (var key in allowed.Where(k => k != "version"))
        {
            if (document[key] is not JsonArray array || array.Any(n => n is not JsonObject)) result.Errors.Add($"shape:{key}");
            else
            {
                arrays[key] = array;
                if (array.Count > 32) result.Errors.Add($"bound:{key}");
            }
        }
        if (arrays.Count != 6) return result;
        var ids = new HashSet<string>();
        foreach (var row in arrays.Values.SelectMany(a => a))
        {
            var id = Text(row!, "id");
            if (string.IsNullOrWhiteSpace(id)) result.Errors.Add("missing-id");
            else if (!ids.Add(id)) result.Errors.Add("duplicate-id");
        }
        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("abc")));
        foreach (var anchor in arrays["anchors"])
        {
            foreach (var key in anchor!.AsObject().Select(p => p.Key).Where(k => !new[] { "id", "target", "scope", "hash", "start", "length" }.Contains(k)))
                result.Errors.Add($"unknown-field:{key}");
            if (Text(anchor!, "target") != "synthetic:abc" || Text(anchor!, "scope") != "fixture"
                || Text(anchor!, "hash") != expectedHash || anchor!["start"]?.ToJsonString() != "0"
                || anchor["length"]?.ToJsonString() != "3") result.Unresolved.Add(Text(anchor!, "id") ?? "<missing>");
        }
        var anchorIds = arrays["anchors"].Select(a => Text(a!, "id")).ToHashSet();
        foreach (var (kind, rows) in arrays.Where(p => p.Key != "anchors"))
        {
            foreach (var row in rows)
            {
                if (!anchorIds.Contains(Text(row!, "anchor"))) result.Unresolved.Add(Text(row!, "anchor") ?? "<missing>");
                var fields = kind switch
                {
                    "concepts" => new[] { "id", "label", "role", "anchor", "root", "members", "invariants" },
                    "invariants" => ["id", "statement", "anchor"],
                    "layers" => ["id", "label", "kind", "dimension", "state", "members", "anchor"],
                    "resources" => ["id", "workspace", "scope", "file", "symbol", "anchor"],
                    _ => new[] { "id", "label", "rootRef", "anchor" }
                };
                foreach (var field in row!.AsObject().Select(p => p.Key).Where(k => !fields.Contains(k))) result.Errors.Add($"unknown-field:{field}");
                var required = kind switch
                {
                    "concepts" => new[] { "id", "label", "role" },
                    "invariants" => ["id", "statement"],
                    "layers" => ["id", "label", "kind", "dimension", "state"],
                    "resources" => ["id", "workspace", "scope", "file", "symbol"],
                    _ => new[] { "id", "label", "rootRef" }
                };
                foreach (var field in required)
                    if (Text(row, field) is not { Length: > 0 and <= 256 } text || string.IsNullOrWhiteSpace(text)) result.Errors.Add($"field:{field}");
            }
        }
        var concepts = arrays["concepts"].Where(c => Text(c!, "id") is not null)
            .GroupBy(c => Text(c!, "id")!).ToDictionary(g => g.Key, g => g.First()!);
        var invariants = arrays["invariants"].Select(i => Text(i!, "id")).ToHashSet();
        bool References(JsonNode row, string field, Func<string, bool> accepts, bool nonempty = true) =>
            row[field] is JsonArray list && (!nonempty || list.Count > 0)
            && list.All(item => item is JsonValue value && value.TryGetValue<string>(out var id) && accepts(id));
        foreach (var concept in arrays["concepts"])
        {
            if (!Roles.Contains(Text(concept!, "role") ?? "")) result.Errors.Add("unknown-role");
            if (Text(concept!, "role") != "aggregate") continue;
            var root = Text(concept!, "root") ?? "";
            if (!concepts.TryGetValue(root, out var entity) || Text(entity, "role") != "entity") result.Errors.Add("aggregate-root");
            if (!References(concept!, "members", concepts.ContainsKey)) result.Errors.Add("aggregate-member");
            if (concept!["members"] is JsonArray members && !members.Any(m => m is JsonValue mv && mv.TryGetValue<string>(out var memberId) && memberId == root))
                result.Errors.Add("aggregate-root-membership");
            if (!References(concept!, "invariants", invariants.Contains)) result.Errors.Add("aggregate-invariant");
        }
        foreach (var layer in arrays["layers"])
        {
            if (Text(layer!, "dimension") is not ("logical" or "deployment")) result.Errors.Add("layer-dimension");
            if (Text(layer!, "state") is not ("current" or "target" or "unspecified")) result.Errors.Add("layer-state");
            if (Text(layer!, "kind") is not ("layer" or "component")) result.Errors.Add("layer-kind");
            if (!References(layer!, "members", concepts.ContainsKey)) result.Errors.Add("layer-member");
        }
        var resourceIds = arrays["resources"].Select(r => Text(r!, "id")).ToHashSet();
        foreach (var alias in arrays["aliases"])
            if (!resourceIds.Contains(Text(alias!, "rootRef"))) result.Errors.Add("alias-root");
        return result;
    }

    // Deliberately one complete literal declaration, never a Bicep compiler/parser contract.
    // targetScope describes a kind, not actual subscription/resource-group identity.
    private static ResourceIdentity ReadLiteralBicep(string source)
    {
        var type = Regex.Match(source, @"(?m)^resource \w+ '([^'@]+)@[^']+' = \{$");
        var name = Regex.Match(source, @"(?m)^  name: '([^']+)'$");
        return new ResourceIdentity(null, type.Success ? type.Groups[1].Value : null,
            name.Success ? name.Groups[1].Value : null);
    }

    private static bool SameDeployment(ResourceIdentity a, ResourceIdentity b) =>
        a.Scope is not null && a.Type is not null && a.Name is not null && a == b;

    private static string DeclarationKey(JsonNode resource) => string.Join('\u001f',
        (_fault == "scope-drop" ? new[] { "workspace", "file", "symbol" } : ["workspace", "scope", "file", "symbol"])
        .Select(p => Text(resource, p)));

    // Small subject under test: only validated synthetic declaration aliases, not Azure equivalence.
    private static ResourceProjection ProjectResources(JsonObject document)
    {
        var validation = Validate(document);
        if (validation.Errors.Count != 0 || validation.Unresolved.Count != 0)
            return new ResourceProjection([.. validation.Errors, .. validation.Unresolved.Select(a => $"unresolved:{a}")], []);
        var anchors = document["anchors"]!.AsArray().ToDictionary(a => Text(a!, "id")!, a => new AnchorEvidence(
            Text(a!, "id")!, Text(a!, "target")!, Text(a!, "scope")!, Text(a!, "hash")!,
            a!["start"]!.GetValue<int>(), a["length"]!.GetValue<int>()));
        var groups = document["resources"]!.AsArray().GroupBy(r => DeclarationKey(r!)).ToArray();
        var roots = groups.Select(group =>
        {
            var ids = group.Select(r => Text(r!, "id")!).ToHashSet();
            var aliasRows = document["aliases"]!.AsArray()
                .Where(a => _fault == "alias-wrong-root" ? group.Key == groups[0].Key : ids.Contains(Text(a!, "rootRef")!));
            if (_fault == "alias-drop") aliasRows = aliasRows.Take(1);
            return new ResourceRoot(Text(group.First()!, "id")!, group.Key, aliasRows.Select(a =>
                new AliasEvidence(Text(a!, "id")!, anchors[Text(a!, "anchor")!])).ToArray());
        }).ToArray();
        return new ResourceProjection([], roots);
    }

    private sealed record ResourceIdentity(string? Scope, string? Type, string? Name);
    private sealed record AnchorEvidence(string Id, string Target, string Scope, string Hash, int Start, int Length);
    private sealed record AliasEvidence(string Id, AnchorEvidence Anchor);
    private sealed record ResourceRoot(string Id, string Key, AliasEvidence[] Aliases);
    private sealed record ResourceProjection(string[] Errors, ResourceRoot[] Roots);
    private sealed class Result
    {
        public List<string> Errors { get; } = [];
        public HashSet<string> Unresolved { get; } = [];
    }
}
