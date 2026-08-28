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
        "is_existing_reference", "is_loop", "is_conditional",
    };

    /// <summary>The SQL literal list for an <c>IN</c> clause. Built from the same set.</summary>
    /// <remarks>
    /// Generated rather than typed a second time: a hand-written copy in a query is exactly how the
    /// two halves of this rule drift apart, which is the defect that produced it.
    /// </remarks>
    public static string SqlList { get; } =
        string.Join(", ", Attributes.Order(StringComparer.Ordinal).Select(p => $"'{p}'"));
}
