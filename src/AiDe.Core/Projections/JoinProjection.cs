using AiDe.Core.Facts;

namespace AiDe.Core.Projections;

/// <summary>One join between artifact types, with how it was established.</summary>
/// <param name="Basis">Why this edge exists, in words the user can judge.</param>
public sealed record JoinEdge(
    string From,
    string To,
    string Kind,
    VerificationStatus Status,
    string Basis);

/// <summary>The joins found, and what stopped more from being found.</summary>
public sealed record JoinResult(
    IReadOnlyList<JoinEdge> Edges,
    IReadOnlyList<string> Disclosures,
    int VerifiedCount,
    int InferredCount);

/// <summary>
/// Joins code, schema and infrastructure evidence.
/// </summary>
/// <remarks>
/// <para><b>Confidence is the deliverable, not the edge.</b> An inferred join across three artifacts
/// looks more impressive than a verified one inside a single file, and it is exactly the kind of
/// claim a user acts on without checking. So every edge carries how it was established, and a
/// convention-derived join is <see cref="VerificationStatus.Inferred"/> however obvious it looks.</para>
///
/// <para><b>Joins are computed, never stored.</b> Two definitions of one quantity is a defect
/// signature: if a join were written back as a fact, the store would hold both the evidence and a
/// derived claim about it, and they would drift the first time an extractor changed. This reads the
/// same assertions every other projection reads.</para>
/// </remarks>
public sealed class JoinProjection(IReadOnlyList<EvidenceAssertion> assertions)
{
    /// <summary>EF's default: a <c>DbSet&lt;Order&gt;</c> maps to a table named for the property.</summary>
    private const string DbSetPrefix = "DbSet";

    public JoinResult Compute()
    {
        var edges = new List<JoinEdge>();
        var disclosures = assertions
            .Where(a => a.Predicate == "discloses")
            .Select(a => a.Object)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        var tables = assertions
            .Where(a => a.Predicate == "has_type" && a.Object == "table")
            .Select(a => a.Subject)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ---------------------------------------------------------------- code → schema
        // A type whose simple name matches a table name. This is EF's pluralisation convention read
        // backwards, and it is a GUESS: two unrelated types can share a table's name, and a table
        // configured with ToTable("orders") is not matched at all. Inferred, and labelled.
        foreach (var type in assertions
            .Where(a => a.Predicate == "has_type" && a.Object is "class" or "record")
            .Select(a => a.Subject)
            .Distinct(StringComparer.Ordinal))
        {
            var simple = SimpleName(type);
            foreach (var table in tables)
            {
                var tableName = table["table:".Length..];
                if (!NamesCorrespond(simple, tableName)) continue;

                edges.Add(new JoinEdge(
                    type, table, "maps_to",
                    VerificationStatus.Inferred,
                    $"the type name '{simple}' corresponds to table '{tableName}' by EF's naming convention; " +
                    "nothing declares this"));
            }
        }

        // ---------------------------------------------------------------- schema → infrastructure
        // Only when a Bicep resource name is a LITERAL. Eight of the 24 resources in a real template
        // are expressions, and matching against an unresolved expression would produce an edge whose
        // basis is a string nobody has evaluated.
        var sqlResources = assertions
            .Where(a => a.Predicate == "resource_type" && a.Object.StartsWith("Microsoft.Sql/", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Subject)
            .ToHashSet(StringComparer.Ordinal);

        var literalNames = assertions
            .Where(a => a.Predicate == "resource_name" && sqlResources.Contains(a.Subject))
            .ToList();

        foreach (var resource in literalNames)
        {
            foreach (var table in tables)
            {
                edges.Add(new JoinEdge(
                    table, resource.Subject, "hosted_on",
                    VerificationStatus.Inferred,
                    $"'{resource.Object}' is the only literally-named SQL resource in this template; " +
                    "no connection string was matched"));
            }
        }

        // A SQL resource whose name is an EXPRESSION cannot be joined at all, and that is stated
        // rather than approximated.
        var expressionNamed = assertions
            .Count(a => a.Predicate == "resource_name_expression" && sqlResources.Contains(a.Subject));

        if (expressionNamed > 0 && tables.Count > 0)
        {
            disclosures = [.. disclosures, "sql-resource-name-unresolved"];
        }

        // ---------------------------------------------------------------- code → infrastructure
        // Verified when both sides are literals: a parameter declared in the template and a string
        // literal in code that matches it exactly. This is the one join in the phase that can be
        // Verified, which is why it is worth having.
        var parameters = assertions
            .Where(a => a.Predicate == "has_type" && a.Object == "azure-parameter")
            .Select(a => a.Subject[(a.Subject.IndexOf('#') + 1)..])
            .ToHashSet(StringComparer.Ordinal);

        var secrets = assertions
            .Where(a => a.Predicate == "is_secret" && a.Object == "true")
            .Select(a => a.Subject[(a.Subject.IndexOf('#') + 1)..])
            .ToHashSet(StringComparer.Ordinal);

        foreach (var parameter in parameters.Where(secrets.Contains))
        {
            // Reported as a node worth knowing about rather than joined to code: the whole point of
            // never reading a secure parameter's value is that nothing here can match on it.
            edges.Add(new JoinEdge(
                parameter, "secret", "is_declared_secret",
                VerificationStatus.Verified,
                "declared @secure() in the template; its value is never read"));
        }

        return new JoinResult(
            edges,
            disclosures.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(),
            edges.Count(e => e.Status == VerificationStatus.Verified),
            edges.Count(e => e.Status == VerificationStatus.Inferred));
    }

    private static string SimpleName(string fullName)
    {
        var cut = fullName.LastIndexOf('.');
        return cut > 0 && cut < fullName.Length - 1 ? fullName[(cut + 1)..] : fullName;
    }

    /// <summary>
    /// Whether a type name and a table name correspond under EF's default conventions.
    /// </summary>
    /// <remarks>
    /// Exact, or differing only by a trailing plural. Deliberately narrow: a looser rule (contains,
    /// or edit distance) produces confident wrong joins, and a wrong join between a class and a
    /// table is precisely the claim a user would act on without verifying.
    /// </remarks>
    private static bool NamesCorrespond(string typeName, string tableName)
    {
        if (string.Equals(typeName, tableName, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(typeName + "s", tableName, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(typeName, tableName + "s", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Ignored today; kept because the DbSet route is the next join to add.</summary>
    internal static bool LooksLikeDbSet(string typeName) =>
        typeName.StartsWith(DbSetPrefix, StringComparison.Ordinal);
}
