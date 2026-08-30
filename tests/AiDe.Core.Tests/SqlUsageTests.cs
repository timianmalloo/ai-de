using AiDe.Core.Extraction;

namespace AiDe.Core.Tests;

/// <summary>
/// Which type talks to which table, for repositories with no ORM at all.
/// </summary>
/// <remarks>
/// <para><b>The honest answer to "verified joins for non-EF repositories".</b> BioHacker has zero
/// `DbContext` files, zero `[Table]` attributes and 191 SQL literals naming tables from inside store
/// classes — so every join it had was a NAME GUESS, and there was no declaration anywhere to make a
/// verified one from.</para>
///
/// <para>There still isn't. A store class issuing four statements against three tables is not MAPPED
/// to any of them, and reusing <c>maps_to</c> would put a confident wrong answer where an honest one
/// belongs. What the source does declare is USAGE, and that is what this emits: 62 edges on that
/// repository, verified because the literal is right there in the type.</para>
/// </remarks>
public sealed class SqlUsageTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "aide-usage", Guid.NewGuid().ToString("N"));

    public SqlUsageTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    private async Task<IReadOnlyList<AiDe.Core.Facts.EvidenceAssertion>> ReadAsync(string source)
    {
        File.WriteAllText(Path.Combine(_dir, "Usage.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        File.WriteAllText(Path.Combine(_dir, "Store.cs"), source);

        var result = await new CSharpExtractor().ExtractAsync(
            new ExtractionRequest("csharp:Usage:net10.0", Path.Combine(_dir, "Usage.csproj"), "rev-1", 1),
            CancellationToken.None);

        return result.Assertions;
    }

    [Fact]
    public async Task ATypeThatQueriesATableSaysSo()
    {
        var facts = await ReadAsync("""
            namespace Shop;

            public class OrderStore
            {
                public string Sql => "SELECT Id FROM dbo.Orders WHERE Id=@id";
            }
            """);

        Assert.Contains(facts, a =>
            a.Subject == "Shop.OrderStore" && a.Predicate == "uses_table" && a.Object == "table:Orders");
    }

    [Fact]
    public async Task ItIsUsageAndNotMapping()
    {
        // A store issuing statements against three tables is mapped to none of them. Reusing
        // `maps_to` would launder usage into a mapping at exactly the point a reader trusts it.
        var facts = await ReadAsync("""
            namespace Shop;

            public class Store
            {
                public string A => "SELECT * FROM Orders";
                public string B => "INSERT INTO Lines (Id) VALUES (1)";
                public string C => "UPDATE Invoices SET Paid=1";
            }
            """);

        Assert.Equal(3, facts.Count(a => a.Predicate == "uses_table"));
        Assert.DoesNotContain(facts, a => a.Predicate == "maps_to");
    }

    [Theory]
    [InlineData("SELECT * FROM Orders", "table:Orders")]
    [InlineData("SELECT * FROM dbo.Orders", "table:Orders")]
    [InlineData("SELECT * FROM [dbo].[Orders]", "table:Orders")]
    [InlineData("INSERT INTO Orders (Id) VALUES (1)", "table:Orders")]
    [InlineData("UPDATE Orders SET X=1", "table:Orders")]
    [InlineData("DELETE FROM Orders WHERE Id=1", "table:Orders")]
    [InlineData("SELECT * FROM A JOIN Orders ON A.Id=Orders.Id", "table:Orders")]
    public async Task TheStatementFormsAndSpellingsThatNameATable(string sql, string expected)
    {
        // The schema qualifier is stripped so dbo.Orders and Orders are ONE node: the EF and SQL
        // readers both emit unqualified names, and a third spelling would point these edges at nodes
        // that do not exist while looking perfectly correct.
        var facts = await ReadAsync($$"""
            namespace Shop;

            public class Store
            {
                public string Sql => "{{sql}}";
            }
            """);

        Assert.Contains(facts, a => a.Predicate == "uses_table" && a.Object == expected);
    }

    [Fact]
    public async Task ATemporaryOrVariableTableIsNotASchemaTable()
    {
        var facts = await ReadAsync("""
            namespace Shop;

            public class Store
            {
                public string A => "SELECT * FROM #Scratch";
                public string B => "SELECT * FROM @Rows";
            }
            """);

        Assert.DoesNotContain(facts, a => a.Predicate == "uses_table");
    }

    [Fact]
    public async Task TheSameTableNamedTwiceInOneTypeIsOneFact()
    {
        // This crashed the indexer on a real repository: identical triples hit the store's natural
        // key as a raw SQLite constraint error from the middle of a run. The key is right; the
        // duplicate carried no information.
        var facts = await ReadAsync("""
            namespace Shop;

            public class Store
            {
                public string A => "SELECT * FROM Orders";
                public string B => "DELETE FROM Orders";
            }
            """);

        Assert.Single(facts, a => a.Predicate == "uses_table" && a.Object == "table:Orders");
    }

    [Fact]
    public async Task AStringWithNoTableAfterTheKeywordYieldsNothing()
    {
        // `FROM (SELECT ...)` names no table. A guess here produces an edge to a node that does not
        // exist, which reads as a schema the repository does not have.
        var facts = await ReadAsync("""
            namespace Shop;

            public class Store
            {
                public string A => "SELECT * FROM (SELECT 1) x";
                public string B => "just a sentence with the word from in it";
            }
            """);

        Assert.DoesNotContain(facts, a => a.Predicate == "uses_table" && a.Object.Contains("SELECT"));
    }
}
