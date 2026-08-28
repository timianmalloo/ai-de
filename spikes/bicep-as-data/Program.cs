using System.Text.Json;
using BicepAsDataSpike;

// ---------------------------------------------------------------------------------------------
// Phase-3 spike 2 — is reading Bicep DECLARATIVELY sufficient, or do the joins need the compiler?
//
// This is a contract question, not an optimisation. Phase 2 established that the product does not
// compile repository-supplied input (spike D3), and `bicep build` is exactly that. If the
// declarative read cannot recover the resource set, the infrastructure component either needs a
// different design or the principle has to be argued away — and it should not be argued away
// quietly.
//
// The oracle is `az bicep build` run ONCE, by hand, on a repository the owner trusts. That is what
// a spike is for. The component this justifies never runs it.
// ---------------------------------------------------------------------------------------------

if (args.Length < 2)
{
    Console.WriteLine("usage: BicepAsDataSpike <file.bicep> <compiled-oracle.json>");
    return 2;
}

var bicepPath = Path.GetFullPath(args[0]);
var oraclePath = Path.GetFullPath(args[1]);

if (!File.Exists(bicepPath) || !File.Exists(oraclePath))
{
    Console.WriteLine($"missing input: bicep={File.Exists(bicepPath)} oracle={File.Exists(oraclePath)}");
    return 2;
}

Console.WriteLine("Phase-3 spike — Bicep read as data, measured against the compiler");
Console.WriteLine(new string('=', 104));
Console.WriteLine($"source : {bicepPath}");
Console.WriteLine($"oracle : {oraclePath}  (az bicep build, run once by hand)");
Console.WriteLine(new string('=', 104));
Console.WriteLine();

var read = BicepReader.Read(bicepPath);

Console.WriteLine($"declarations read : {read.Resources.Count} resource(s), {read.Modules.Count} module(s), {read.Parameters.Count} param(s)");
Console.WriteLine($"secure parameters : {read.Parameters.Count(p => p.IsSecure)} " +
                  $"({string.Join(", ", read.Parameters.Where(p => p.IsSecure).Select(p => p.Name))})");
Console.WriteLine($"dependsOn lines   : {read.DependsOn.Count}");
Console.WriteLine();

// ---------------------------------------------------------------- the oracle
using var stream = File.OpenRead(oraclePath);
using var json = JsonDocument.Parse(stream);

var oracleResources = new List<(string Type, string Name)>();
if (json.RootElement.TryGetProperty("resources", out var resources))
{
    var items = resources.ValueKind == JsonValueKind.Array
        ? resources.EnumerateArray()
        : resources.EnumerateObject().Select(p => p.Value);

    foreach (var item in items)
    {
        oracleResources.Add((
            item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
            item.TryGetProperty("name", out var n) ? n.ToString() : ""));
    }
}

var oracleParams = json.RootElement.TryGetProperty("parameters", out var ps)
    ? ps.EnumerateObject().Select(p => p.Name).ToList()
    : [];

Console.WriteLine($"oracle            : {oracleResources.Count} resource(s), {oracleParams.Count} param(s)");
Console.WriteLine();

// ---------------------------------------------------------------- compare
// Compared by TYPE, not by name. Every interesting name in a real template is an expression —
// "[format('{0}-vnet', parameters('namePrefix'))]" — and comparing an unresolved expression against
// a resolved one would measure the compiler, not the read. The type is the thing a join needs.
var readTypes = read.Resources.Select(r => r.Type).ToList();
var oracleTypes = oracleResources.Select(r => r.Type).ToList();

var missingTypes = oracleTypes.Except(readTypes, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
var extraTypes = readTypes.Except(oracleTypes, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

Console.WriteLine("RESOURCE TYPE AGREEMENT");
Console.WriteLine($"  distinct types in oracle : {oracleTypes.Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
Console.WriteLine($"  distinct types read      : {readTypes.Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
Console.WriteLine($"  missing from the read    : {missingTypes.Count}{(missingTypes.Count > 0 ? "  " + string.Join(", ", missingTypes.Take(8)) : "")}");
Console.WriteLine($"  only in the read         : {extraTypes.Count}{(extraTypes.Count > 0 ? "  " + string.Join(", ", extraTypes.Take(8)) : "")}");
Console.WriteLine();

var missingParams = oracleParams.Except(read.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase).ToList();
Console.WriteLine($"PARAMETERS: {oracleParams.Count} expected, {missingParams.Count} missed" +
                  (missingParams.Count > 0 ? "  " + string.Join(", ", missingParams) : ""));
Console.WriteLine();

var literal = read.Resources.Count(r => r.NameIsLiteral);
Console.WriteLine($"NAMES: {literal} of {read.Resources.Count} resource names are literals; " +
                  $"{read.Resources.Count - literal} are expressions and stay unresolved.");
Console.WriteLine("  An unresolved name is kept verbatim and DISCLOSED. A guessed one would be a");
Console.WriteLine("  confident wrong answer in a join, which is worse than an absent edge.");
Console.WriteLine();

Console.WriteLine(new string('=', 104));

if (missingTypes.Count == 0 && missingParams.Count == 0)
{
    Console.WriteLine("VERDICT: the declarative read recovers every resource TYPE and every parameter the");
    Console.WriteLine("         compiler produces. Bicep can be read as data — the product does not need the");
    Console.WriteLine("         compiler, and Phase 2's no-build principle holds into Phase 3.");
    Console.WriteLine("         WHAT IT DOES NOT RECOVER: resolved names. Joins that need a literal resource");
    Console.WriteLine("         name are Inferred-at-best and must say so.");
    return 0;
}

Console.WriteLine("VERDICT: the declarative read MISSES declarations the compiler produces. Either the");
Console.WriteLine("         reader grows to cover them, or the infrastructure component needs a different");
Console.WriteLine("         design — it does not get to invoke the compiler.");
return 1;
