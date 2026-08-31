namespace AiDe.Core.Facts;

/// <summary>
/// Which predicates carry a VALUE and which carry a reference to another node.
/// </summary>
/// <remarks>
/// <para>The fact grain deliberately does not distinguish them — every row is
/// (subject, predicate, object), and an attribute genuinely IS a relation to a value. What must not
/// follow is that every value becomes something a user can navigate to.</para>
///
/// <para><b>Found by indexing a real repository.</b> <c>api_version</c> put <c>2020-02-02</c> in the
/// graph and <c>resource_name_expression</c> put <c>'${namePrefix}-acs'</c> there, so dates and
/// unevaluated strings ranked alongside types as things to explore.</para>
///
/// <para><b>One list, used by every reader.</b> The first version of this fix lived only in the
/// ingest path, and search kept returning the junk because search reads the assertions directly
/// rather than the node table — two places deciding the same thing, one of them wrong.</para>
///
/// <para>An explicit list rather than a naming convention: a convention silently misclassifies the
/// first predicate that does not follow it, and a misclassification here puts junk in the graph
/// instead of failing.</para>
/// </remarks>
public static class EvidencePredicates
{
    /// <summary>Predicates whose object is a value, not a node.</summary>
    public static IReadOnlySet<string> Attributes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "has_type", "declared_in", "discloses",
        "api_version", "resource_type", "resource_name", "resource_name_expression",
        "module_path", "parameter_type", "is_secret",
        "has_column", "introduced_by",

        // A type's members, for UML member compartments (ADR-0020). An ATTRIBUTE for the same reason
        // has_column is one: `Id : int` is a property OF a class, not a peer of it, and drawing one
        // would put every method and field in the graph as a thing to navigate to. MEASURED on a real
        // repository, that would have been tens of thousands of new nodes to serve a card layout.
        "has_member", "members_truncated",
        "is_existing_reference", "is_loop", "is_conditional",
        "declares_table",

        // Knowledge attributes. `owned_by` names a PERSON and `review_by` a DATE — neither is a
        // thing to navigate to, and drawing them would put "@someone" and "2027-02-28" in the graph
        // as peers of the documents that carry them.
        "owned_by", "review_by", "node_class",

        // Where a SCOPE's files are. Its object is a directory path, which is not a thing to
        // navigate to, and its subject is a scope — which this graph has never treated as a node
        // (`declared_in` points at scope ids precisely as an attribute, so they are not drawn).
        "declared_at",
    };

    /// <summary>The SQL literal list for an <c>IN</c> clause. Built from the same set.</summary>
    /// <remarks>
    /// Generated rather than typed a second time: a hand-written copy in a query is exactly how the
    /// two halves of this rule drift apart, which is the defect that produced it.
    /// </remarks>
    public static string SqlList { get; } =
        string.Join(", ", Attributes.Order(StringComparer.Ordinal).Select(p => $"'{p}'"));
}
