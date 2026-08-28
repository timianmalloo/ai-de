using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EfMigrationSchemaSpike;

/// <summary>The schema as it stands after replaying every migration in order.</summary>
internal sealed record FoldResult(
    int MigrationCount,
    Dictionary<string, HashSet<string>> Tables,
    List<string> Dropped,
    List<string> Renamed,
    double Millis);

/// <summary>
/// Folds EF Core migrations into a schema, reading them as <b>syntax</b>.
/// </summary>
/// <remarks>
/// <para><b>Syntax, not semantics, and certainly not execution.</b> A migration is a C# class whose
/// <c>Up</c> method makes a fixed vocabulary of calls on <c>migrationBuilder</c>. Those calls are
/// legible from the syntax tree alone, which keeps this inside Phase 2's constraint: a repository's
/// code is read, never run.</para>
///
/// <para><b>Migrations are append-only and ordered</b>, so the current schema is a fold over them.
/// That is the same shape as the fact store itself — which is why schema evidence needs no new table
/// and can sit beside code evidence at a different grain.</para>
///
/// <para><b>Ordering is by the timestamp prefix in the FILE NAME</b>, which is how EF itself orders
/// them. Ordering by anything else — file-system order, class name — reorders a create after a drop
/// and produces a schema that never existed.</para>
/// </remarks>
internal static class MigrationFold
{
    internal static FoldResult Read(string migrationsDirectory)
    {
        var watch = Stopwatch.StartNew();

        var files = Directory
            .EnumerateFiles(migrationsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();

        var tables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var dropped = new List<string>();
        var renamed = new List<string>();

        foreach (var file in files)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);

            // Only the Up method. Down undoes things, and folding both would net to nothing.
            var up = tree.GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.ValueText == "Up");

            if (up is null) continue;

            foreach (var call in up.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (call.Expression is not MemberAccessExpressionSyntax member) continue;
                var op = member.Name.Identifier.ValueText;

                switch (op)
                {
                    case "CreateTable":
                        Apply(tables, Named(call, "name"), Columns(call));
                        break;

                    case "AddColumn":
                        {
                            var table = Named(call, "table");
                            var column = Named(call, "name");
                            if (table is not null && column is not null)
                            {
                                Ensure(tables, table).Add(column);
                            }

                            break;
                        }

                    case "DropColumn":
                        {
                            var table = Named(call, "table");
                            var column = Named(call, "name");
                            if (table is not null && column is not null && tables.TryGetValue(table, out var cols))
                            {
                                cols.Remove(column);
                            }

                            break;
                        }

                    case "DropTable":
                        {
                            var table = Named(call, "name");
                            if (table is not null && tables.Remove(table)) dropped.Add(table);
                            break;
                        }

                    case "RenameTable":
                        {
                            var from = Named(call, "name");
                            var to = Named(call, "newName");
                            if (from is not null && to is not null && tables.Remove(from, out var cols))
                            {
                                tables[to] = cols;
                                renamed.Add($"{from}->{to}");
                            }

                            break;
                        }
                }
            }
        }

        watch.Stop();
        return new FoldResult(files.Count, tables, dropped, renamed, watch.Elapsed.TotalMilliseconds);
    }

    private static void Apply(Dictionary<string, HashSet<string>> tables, string? name, IEnumerable<string> columns)
    {
        if (name is null) return;
        var set = Ensure(tables, name);
        foreach (var column in columns) set.Add(column);
    }

    private static HashSet<string> Ensure(Dictionary<string, HashSet<string>> tables, string name)
    {
        if (!tables.TryGetValue(name, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            tables[name] = set;
        }

        return set;
    }

    /// <summary>
    /// A named argument's literal value.
    /// </summary>
    /// <remarks>
    /// EF's generated migrations always use named arguments, which is what makes this readable
    /// without binding: <c>name:</c> is unambiguous where argument POSITION would differ between
    /// overloads. A hand-edited migration using positional arguments is not read, and that is a
    /// limitation to disclose rather than to guess around.
    /// </remarks>
    private static string? Named(InvocationExpressionSyntax call, string argument) =>
        call.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == argument)
            ?.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;

    /// <summary>
    /// The column names in a <c>CreateTable</c>'s anonymous <c>columns:</c> object.
    /// </summary>
    /// <remarks>
    /// Each property of the anonymous type is one column, and its NAME is the column name — the
    /// value is the builder call describing its type. Reading the property names is enough for the
    /// table/column grain; reading the types would need the builder vocabulary too.
    /// </remarks>
    private static IEnumerable<string> Columns(InvocationExpressionSyntax call)
    {
        var columns = call.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == "columns")
            ?.Expression;

        if (columns is not SimpleLambdaExpressionSyntax lambda) yield break;
        if (lambda.ExpressionBody is not AnonymousObjectCreationExpressionSyntax anonymous) yield break;

        foreach (var initializer in anonymous.Initializers)
        {
            var name = initializer.NameEquals?.Name.Identifier.ValueText
                ?? (initializer.Expression as IdentifierNameSyntax)?.Identifier.ValueText;

            if (!string.IsNullOrEmpty(name)) yield return name;
        }
    }
}
