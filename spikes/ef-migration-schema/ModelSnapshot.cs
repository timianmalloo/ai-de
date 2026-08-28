using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EfMigrationSchemaSpike;

/// <summary>The schema EF itself believes in, read from its checked-in model snapshot.</summary>
internal sealed record SnapshotResult(List<string> Tables);

/// <summary>
/// Reads <c>*ModelSnapshot.cs</c> — <b>the oracle</b>.
/// </summary>
/// <remarks>
/// <para>EF regenerates this file on every <c>migrations add</c> and checks it in, so a repository
/// carries an authoritative statement of its own current schema right next to the migrations. That
/// makes the comparison free, local, and independent of this spike's own logic — a spike whose only
/// check is its own parser agreeing with itself measures nothing.</para>
///
/// <para><b>It is an oracle, not a ground truth.</b> It states what the MODEL says, which is the same
/// claim the fold makes. Neither knows what a deployed database actually contains, and that gap is
/// the <c>schema-from-migrations-not-database</c> disclosure the Phase-3 design already names.</para>
/// </remarks>
internal static class ModelSnapshot
{
    internal static SnapshotResult Read(string path)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
        var tables = new List<string>();

        foreach (var call in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax member) continue;
            if (member.Name.Identifier.ValueText != "ToTable") continue;

            // The first string literal is the table name; a second is the schema. Only the name is
            // compared, because the fold reads CreateTable's name: argument and EF puts the schema in
            // a separate argument there too — comparing "schema-qualified vs not" would report a
            // disagreement that is purely about how each side spells the same table.
            var first = call.ArgumentList.Arguments
                .Select(a => a.Expression)
                .OfType<LiteralExpressionSyntax>()
                .FirstOrDefault(l => l.IsKind(SyntaxKind.StringLiteralExpression));

            if (first is not null) tables.Add(first.Token.ValueText);
        }

        return new SnapshotResult(tables.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }
}
