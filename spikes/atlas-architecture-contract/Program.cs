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
                if (args.Length != 2 || args[0] != "--fault" || args[1] is not ("alias-drop" or "alias-wrong-root" or "scope-drop" or "relation-drop" or "relation-wrong-endpoint" or "relation-admit-mismatch" or "relation-corrupt-binding" or "relation-dependency-basis"))
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
            Negative("missing-seventh-collection", d => d.Remove("relations"), "shape:relations");
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
            RelationChecks(valid);
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
        var allowed = new HashSet<string> { "version", "concepts", "invariants", "layers", "anchors", "resources", "aliases", "relations" };
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
        if (arrays.Count != 7) return result;
        if (arrays.Values.Sum(a => a.Count) > 224) result.Errors.Add("bound:total");
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
        foreach (var (kind, rows) in arrays.Where(p => p.Key is not ("anchors" or "relations")))
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

    // Deliberately closed synthetic assertion registry. It is not an Atlas producer or authorization oracle.
    private static RelationProjection ProjectRelations(JsonObject document)
    {
        var validation = Validate(document);
        var errors = new List<string>(validation.Errors);
        var unresolved = new HashSet<string>(validation.Unresolved);
        var edges = new List<RelationEdge>();
        if (errors.Count != 0) return new(errors.ToArray(), unresolved.ToArray(), []);
        foreach (var row in document["relations"]!.AsArray())
        {
            var r = row!;
            if (r.AsObject().Any(p => !new[] { "id", "from", "to", "kind", "state", "basis", "anchors", "assertionRefs" }.Contains(p.Key))) errors.Add("relation-field");
            foreach (var field in new[] { "id", "kind", "state", "basis" })
                if (Text(r, field) is not { Length: > 0 and <= 256 } s || string.IsNullOrWhiteSpace(s)) errors.Add("relation-text");
            string Endpoint(string field)
            {
                if (r[field] is not JsonObject endpoint || endpoint.Count != 2 || Text(endpoint, "id") is not { Length: > 0 and <= 256 } id || string.IsNullOrWhiteSpace(id)) { errors.Add("endpoint-shape"); return ""; }
                var collection = Text(endpoint, "type") switch { "concept" => "concepts", "structure" => "layers", "resource" => "resources", _ => "" };
                if (collection == "") errors.Add("endpoint-type");
                else if (!document[collection]!.AsArray().Any(n => Text(n!, "id") == id)) unresolved.Add("endpoint:" + id);
                return Text(endpoint, "type") + ":" + id;
            }
            var from = Endpoint("from"); var to = Endpoint("to");
            var kind = Text(r, "kind"); var state = Text(r, "state"); var basis = Text(r, "basis");
            if (kind is not ("domain-association" or "deployment-dependency")) errors.Add("unsupported-kind");
            else if (!(from.StartsWith(kind == "domain-association" ? "concept:" : "resource:", StringComparison.Ordinal) && to.StartsWith(kind == "domain-association" ? "concept:" : "resource:", StringComparison.Ordinal))) errors.Add("endpoint-kind");
            if (state is not ("current" or "target" or "unspecified")) errors.Add("relation-state");
            if (basis is not ("explicit-declaration" or "supported-source-assertion")) errors.Add("relation-basis");
            string[] Refs(string field, bool required)
            {
                if (r[field] is not JsonArray list || list.Count > 32 || (required && list.Count == 0) || list.Any(n => n is not JsonValue v || !v.TryGetValue<string>(out var s) || string.IsNullOrWhiteSpace(s) || s.Length > 256)) { errors.Add("relation-refs:" + field); return []; }
                return list.Select(n => n!.GetValue<string>()).ToArray();
            }
            var anchors = Refs("anchors", true); var assertions = Refs("assertionRefs", basis == "supported-source-assertion");
            var evidence = new List<AnchorEvidence>();
            foreach (var id in anchors)
            {
                var a = document["anchors"]!.AsArray().FirstOrDefault(n => Text(n!, "id") == id);
                if (a is null) unresolved.Add("anchor:" + id);
                else if (!validation.Unresolved.Contains(id)) evidence.Add(new(id, Text(a, "target")!, Text(a, "scope")!, Text(a, "hash")!, a["start"]!.GetValue<int>(), a["length"]!.GetValue<int>()));
            }
            if (basis == "explicit-declaration" && assertions.Length != 0) errors.Add("declaration-assertions");
            if (basis == "supported-source-assertion")
            {
                if (assertions.Any(a => a != "synthetic-dependency")) unresolved.Add("assertion:missing");
                // Registry predicate depends_on binds this exact typed pair, kind and anchor in one synthetic observation.
                if (_fault != "relation-admit-mismatch" && (kind != "deployment-dependency" || from != "resource:resource-a" || to != "resource:resource-b" || state != "current" || !anchors.SequenceEqual(["a"]))) errors.Add("assertion-mismatch");
            }
            if (_fault == "relation-corrupt-binding") evidence = evidence.Select(a => a with { Scope = "wrong-scope", Hash = "wrong-hash", Start = 1, Length = 2 }).ToList();
            edges.Add(new(Text(r, "id")!, from, _fault == "relation-wrong-endpoint" ? from : to, kind!, state!, basis!, basis == "explicit-declaration" ? "Declared relationship" : "Synthetic supported depends_on", evidence.ToArray(), assertions));
            if (_fault == "relation-dependency-basis" && Text(r, "id") == "dependency") edges[^1] = edges[^1] with { Basis = "explicit-declaration" };
        }
        return new(errors.ToArray(), unresolved.ToArray(), errors.Count != 0 || unresolved.Count != 0 || _fault == "relation-drop" ? [] : edges.ToArray());
    }

    private static void RelationChecks(JsonObject valid)
    {
        void Reject(string name, Action<JsonObject> mutate, string diagnostic, bool unresolved = false)
        {
            var copy = (JsonObject)valid.DeepClone(); mutate(copy); var p = ProjectRelations(copy);
            Assert(name, p.Edges.Length == 0 && (unresolved ? p.Unresolved : p.Errors).Contains(diagnostic), $"errors={string.Join(',', p.Errors)} unresolved={string.Join(',', p.Unresolved)} edges={p.Edges.Length}");
        }
        Reject("relation-missing-endpoint", d => d["relations"]![0]!["from"] = null, "endpoint-shape");
        Reject("relation-dangling-endpoint", d => d["relations"]![0]!["to"]!["id"] = "missing", "endpoint:missing", true);
        Reject("relation-wrong-endpoint-type", d => d["relations"]![0]!["to"] = new JsonObject { ["type"] = "resource", ["id"] = "resource-b" }, "endpoint-kind");
        Reject("relation-unsupported-kind", d => d["relations"]![0]!["kind"] = "explicit-grant", "unsupported-kind");
        Reject("relation-missing-evidence", d => d["relations"]![0]!["anchors"] = new JsonArray("missing"), "anchor:missing", true);
        Reject("relation-stale-evidence", d => d["anchors"]![0]!["hash"] = "stale", "a", true);
        Reject("relation-missing-assertion", d => d["relations"]![1]!["assertionRefs"] = new JsonArray("absent"), "assertion:missing", true);
        Reject("relation-mismatched-assertion", d => d["relations"]![1]!["to"]!["id"] = "resource-a", "assertion-mismatch");
        Reject("relation-mismatched-evidence-binding", d => d["relations"]![1]!["anchors"] = new JsonArray("b"), "assertion-mismatch");
        Reject("relation-empty-evidence", d => d["relations"]![0]!["anchors"] = new JsonArray(), "relation-refs:anchors");
        Reject("relation-ref-overflow", d => d["relations"]![0]!["anchors"] = new JsonArray(Enumerable.Repeat("a", 33).Select(s => (JsonNode?)JsonValue.Create(s)).ToArray()), "relation-refs:anchors");
        foreach (var collection in new[] { "concepts", "invariants", "layers", "anchors", "resources", "aliases", "relations" })
            Reject("shape-" + collection, d => d[collection] = "bad", "shape:" + collection);
        var boundary = (JsonObject)valid.DeepClone();
        foreach (var field in boundary.Where(p => p.Value is JsonArray).Select(p => p.Key).ToArray())
        {
            var list = boundary[field]!.AsArray();
            while (list.Count < 32) { var extra = list[0]!.DeepClone(); extra["id"] = $"{field}-{list.Count}"; list.Add(extra); }
        }
        var full = ProjectRelations(boundary);
        Assert("seven-collection-224-boundary", full.Errors.Length == 0 && full.Unresolved.Length == 0 && full.Edges.Length == 32, "7 x 32 rows; 32 produced relations");
        foreach (var collection in new[] { "concepts", "invariants", "layers", "anchors", "resources", "aliases", "relations" })
        {
            var copy = (JsonObject)boundary.DeepClone(); var extra = copy[collection]![0]!.DeepClone(); extra["id"] = "overflow"; copy[collection]!.AsArray().Add(extra);
            var p = ProjectRelations(copy);
            Assert("cap-" + collection, p.Errors.Contains("bound:" + collection) && p.Errors.Contains("bound:total") && p.Edges.Length == 0, "225 total; 33 in named collection; no output");
        }
        var positive = ProjectRelations(valid);
        var expectedBinding = new AnchorEvidence("a", "synthetic:abc", "fixture",
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", 0, 3);
        RelationEdge[] expected = [
            new("declared", "concept:order", "concept:money", "domain-association", "target",
                "explicit-declaration", "Declared relationship", [expectedBinding], []),
            new("dependency", "resource:resource-a", "resource:resource-b", "deployment-dependency", "current",
                "supported-source-assertion", "Synthetic supported depends_on", [expectedBinding], ["synthetic-dependency"])
        ];
        Assert("relation-produced-target-and-current", MatchesExpectedRelations(positive, expected), JsonSerializerOutput(positive));
        var current = (JsonObject)valid.DeepClone(); current["relations"]![0]!["state"] = "current";
        var declaration = ProjectRelations(current);
        Assert("current-declaration-stays-declared", MatchesExpectedRelations(declaration,
            [expected[0] with { State = "current" }, expected[1]]), JsonSerializerOutput(declaration));
    }

    private static bool MatchesExpectedRelations(RelationProjection actual, RelationEdge[] expected) =>
        actual.Errors.Length == 0 && actual.Unresolved.Length == 0 && actual.Edges.Length == expected.Length
        && actual.Edges.Zip(expected).All(pair => pair.First.Id == pair.Second.Id
            && pair.First.From == pair.Second.From && pair.First.To == pair.Second.To
            && pair.First.Kind == pair.Second.Kind && pair.First.State == pair.Second.State
            && pair.First.Basis == pair.Second.Basis && pair.First.Label == pair.Second.Label
            && pair.First.Anchors.SequenceEqual(pair.Second.Anchors)
            && pair.First.Assertions.SequenceEqual(pair.Second.Assertions));

    private static string JsonSerializerOutput(RelationProjection p) => System.Text.Json.JsonSerializer.Serialize(p.Edges);
    private sealed record RelationEdge(string Id, string From, string To, string Kind, string State, string Basis, string Label, AnchorEvidence[] Anchors, string[] Assertions);
    private sealed record RelationProjection(string[] Errors, string[] Unresolved, RelationEdge[] Edges);
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
