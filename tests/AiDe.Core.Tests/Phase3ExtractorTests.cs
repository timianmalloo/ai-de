using AiDe.Core.Extraction;
using AiDe.Core.Facts;
using AiDe.Core.Projections;

namespace AiDe.Core.Tests;

/// <summary>
/// The Phase-3 extractors and the joins over them.
/// </summary>
/// <remarks>
/// The cases that matter are the ones about <b>confidence and secrecy</b>. A join across three
/// artifact types looks more authoritative than a fact inside one file, and it is exactly the kind
/// of claim a user acts on without checking — so what is asserted here is mostly what the extractors
/// REFUSE to say.
/// </remarks>
public sealed class Phase3ExtractorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-phase3", Guid.NewGuid().ToString("N"));

    public Phase3ExtractorTests() => Directory.CreateDirectory(_root);

    private string Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static ExtractionRequest Request(string scopeId, string path) => new(scopeId, path, "rev-1", 1);

    private static IEnumerable<EvidenceAssertion> Where(ExtractionResult r, string predicate) =>
        r.Assertions.Where(a => a.Predicate == predicate);

    // ---- Bicep --------------------------------------------------------------

    private const string Template = """
        @description('The prefix for every resource.')
        param namePrefix string = 'demo'

        @secure()
        @minLength(32)
        param apiSecret string

        resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
          name: '${namePrefix}-vnet'
          location: 'westeurope'
        }

        resource sql 'Microsoft.Sql/servers@2022-05-01' = {
          name: 'demo-sql'
        }
        """;

    [Fact]
    public async Task BicepRecordsResourcesTheirTypesAndApiVersions()
    {
        var path = Write("infra/main.bicep", Template);
        var result = await new BicepExtractor().ExtractAsync(Request("bicep:main", path), CancellationToken.None);

        Assert.True(result.Complete);
        Assert.Contains(Where(result, "resource_type"), a => a.Object == "Microsoft.Sql/servers");
        Assert.Contains(Where(result, "api_version"), a => a.Object == "2022-05-01");
        Assert.Contains(Where(result, "has_type"), a => a.Object == "azure-resource");
    }

    [Fact]
    public async Task ALiteralNameIsAFact_AndAnExpressionIsRecordedAsAnExpression()
    {
        // A guessed resource name would be a confident wrong edge between a table and a server, and
        // the user would act on it. So an unresolved name is kept verbatim under a DIFFERENT
        // predicate, which is what stops a join treating it as a name.
        var path = Write("infra/main.bicep", Template);
        var result = await new BicepExtractor().ExtractAsync(Request("bicep:main", path), CancellationToken.None);

        Assert.Contains(Where(result, "resource_name"), a => a.Object == "demo-sql");
        Assert.Contains(Where(result, "resource_name_expression"), a => a.Object.Contains("namePrefix", StringComparison.Ordinal));
        Assert.DoesNotContain(Where(result, "resource_name"), a => a.Object.Contains('$'));
    }

    [Fact]
    public async Task AnUnresolvedNameIsDisclosed_SoAPartialPictureNeverLooksComplete()
    {
        var path = Write("infra/main.bicep", Template);
        var result = await new BicepExtractor().ExtractAsync(Request("bicep:main", path), CancellationToken.None);

        Assert.Contains(
            Where(result, CSharpExtractor.DisclosurePredicate),
            a => a.Object == ExtractionDisclosures.BicepExpressionsNotEvaluated);
    }

    [Fact]
    public async Task ASecureParameterIsRecordedAsSecret_AndItsValueIsNeverRead()
    {
        var path = Write("infra/main.bicep", Template);
        var result = await new BicepExtractor().ExtractAsync(Request("bicep:main", path), CancellationToken.None);

        Assert.Contains(Where(result, "is_secret"), a => a.Subject.EndsWith("#apiSecret", StringComparison.Ordinal));

        // The non-secure parameter has a default in the template. Nothing may carry it: a default is
        // still a value, and "we only skip the secret ones" is one edit away from skipping none.
        Assert.DoesNotContain(result.Assertions, a => a.Object.Contains("demo", StringComparison.Ordinal) && a.Predicate == "parameter_type");
        Assert.DoesNotContain(Where(result, "is_secret"), a => a.Subject.EndsWith("#namePrefix", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoopsConditionalsAndExistingReferencesAreRecordedDistinctly()
    {
        var path = Write("infra/shapes.bicep", """
            param enable bool = true
            param names array = ['a', 'b']

            resource existingSql 'Microsoft.Sql/servers@2022-05-01' existing = {
              name: 'already-there'
            }

            resource many 'Microsoft.Storage/storageAccounts@2023-01-01' = [for n in names: {
              name: n
            }]

            resource maybe 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enable) {
              name: 'role'
            }
            """);

        var result = await new BicepExtractor().ExtractAsync(Request("bicep:shapes", path), CancellationToken.None);

        // All three are still resources — the point is that each carries WHAT IT IS as well.
        Assert.Equal(3, Where(result, "has_type").Count(a => a.Object == "azure-resource"));

        Assert.Contains(Where(result, "is_existing_reference"), a => a.Subject.EndsWith("/existingSql", StringComparison.Ordinal));
        Assert.Contains(Where(result, "is_loop"), a => a.Subject.EndsWith("/many", StringComparison.Ordinal));
        Assert.Contains(Where(result, "is_conditional"), a => a.Subject.EndsWith("/maybe", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ATemplateWithLoopsDisclosesThatItsResourceCountIsNotADeploymentCount()
    {
        // "3 resources" for a template that deploys one, several, or none of them would be a
        // confident wrong number — and a count is exactly the kind of thing nobody re-checks.
        var path = Write("infra/shapes.bicep", """
            param names array = ['a']
            resource many 'Microsoft.Storage/storageAccounts@2023-01-01' = [for n in names: {
              name: n
            }]
            """);

        var result = await new BicepExtractor().ExtractAsync(Request("bicep:shapes", path), CancellationToken.None);

        Assert.Contains(
            Where(result, CSharpExtractor.DisclosurePredicate),
            a => a.Object == ExtractionDisclosures.BicepResourceCountIndeterminate);
    }

    [Fact]
    public async Task ATemplateWithoutLoopsDoesNotDiscloseAnIndeterminateCount()
    {
        // The disclosure has to be absent when it does not apply, or it becomes noise every scope
        // carries and nobody reads.
        var path = Write("infra/plain.bicep", Template);
        var result = await new BicepExtractor().ExtractAsync(Request("bicep:plain", path), CancellationToken.None);

        Assert.DoesNotContain(
            Where(result, CSharpExtractor.DisclosurePredicate),
            a => a.Object == ExtractionDisclosures.BicepResourceCountIndeterminate);
    }

    // ---- bounded contexts (ADR-0016) ---------------------------------------

    [Fact]
    public void AContextNamingANamespaceThatDoesNotExistFailsLoudly()
    {
        // The drift ADR-0016 exists to make fail. Almost always a renamed namespace, and invisible
        // without this check.
        var path = Write("docs/bounded-contexts.yaml", """
            contexts:
              - name: Sales
                includes:
                  - Shop.Sales.*
              - name: Ghost
                includes:
                  - Shop.Removed.*
            """);

        var map = BoundedContextReader.Load(path, ["Shop.Sales.Order"]);

        Assert.False(map.IsValid);
        Assert.Contains(map.Problems, p => p.Code == "AIDE-CTX-UNKNOWN-NAMESPACE");
    }

    [Fact]
    public void OverlappingContextsAreAnError_NotAMerge()
    {
        // Contexts that overlap are not bounded. Picking the first match would hide a real modelling
        // problem behind a tool that appears to work.
        var path = Write("docs/bounded-contexts.yaml", """
            contexts:
              - name: A
                includes:
                  - Shop.*
              - name: B
                includes:
                  - Shop.Sales.*
            """);

        var map = BoundedContextReader.Load(path, ["Shop.Sales.Order"]);

        Assert.False(map.IsValid);
        Assert.Contains(map.Problems, p => p.Code == "AIDE-CTX-OVERLAP");
    }

    [Fact]
    public void CoverageIsReported_SoPartialContextsCannotLookComplete()
    {
        var path = Write("docs/bounded-contexts.yaml", """
            contexts:
              - name: Sales
                includes:
                  - Shop.Sales.*
            """);

        var map = BoundedContextReader.Load(path, ["Shop.Sales.Order", "Shop.Billing.Invoice"]);

        Assert.True(map.IsValid);
        Assert.Equal(0.5, map.Coverage);
        Assert.Contains("Shop.Billing.Invoice", map.Uncovered);
        Assert.Contains("50", map.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void UnsupportedYamlIsRejectedByLine_NotSilentlyIgnored()
    {
        // A config file whose parser ignores what it does not understand means something different
        // from what its author read.
        var path = Write("docs/bounded-contexts.yaml", """
            contexts:
              - name: Sales
                includes:
                  - Shop.Sales.*
                owner: &anchor someone
            """);

        var map = BoundedContextReader.Load(path, ["Shop.Sales.Order"]);

        Assert.False(map.IsValid);
        Assert.Contains(map.Problems, p => p.Code == "AIDE-CTX-UNSUPPORTED");
    }

    [Fact]
    public void NoFileIsNotAnError_ButAlsoNotContexts()
    {
        // A repository with no map gets no contexts, and the domain projection is unavailable rather
        // than guessed.
        var map = BoundedContextReader.Load(Path.Combine(_root, "nope.yaml"), ["Shop.Sales.Order"]);

        Assert.True(map.IsValid);
        Assert.Empty(map.Contexts);
        Assert.Contains("No bounded contexts are declared", map.Describe(), StringComparison.Ordinal);
    }

    // ---- EF schema ----------------------------------------------------------

    private void WriteMigration(string name, string body) =>
        Write($"Migrations/{name}.cs", $$"""
            using Microsoft.EntityFrameworkCore.Migrations;
            public partial class M : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
            {{body}}
                }
                protected override void Down(MigrationBuilder migrationBuilder) { }
            }
            """);

    [Fact]
    public async Task TheFoldAppliesMigrationsInTimestampOrder()
    {
        // Ordering is the whole correctness argument: a create applied after a drop produces a
        // schema that never existed.
        WriteMigration("20260101000000_Create", """
                    migrationBuilder.CreateTable(
                        name: "Orders",
                        columns: table => new { Id = 1, Total = 2 });
            """);
        WriteMigration("20260102000000_AddColumn", """
                    migrationBuilder.AddColumn(name: "Note", table: "Orders");
            """);
        WriteMigration("20260103000000_DropTemp", """
                    migrationBuilder.CreateTable(name: "Temp", columns: table => new { Id = 1 });
                    migrationBuilder.DropTable(name: "Temp");
            """);

        var result = await new EfSchemaExtractor()
            .ExtractAsync(Request("schema:App", Path.Combine(_root, "Migrations")), CancellationToken.None);

        var tables = Where(result, "has_type").Where(a => a.Object == "table").Select(a => a.Subject).ToList();
        Assert.Contains("table:Orders", tables);

        // Created and dropped in the same run: correctly absent.
        Assert.DoesNotContain("table:Temp", tables);

        var columns = Where(result, "has_column").Where(a => a.Subject == "table:Orders").Select(a => a.Object).ToList();
        Assert.Contains("Id", columns);
        Assert.Contains("Note", columns);
    }

    [Fact]
    public async Task RawSqlIsDisclosed_BecauseItCanChangeTheSchemaInvisibly()
    {
        WriteMigration("20260101000000_Create", """
                    migrationBuilder.CreateTable(name: "Orders", columns: table => new { Id = 1 });
                    migrationBuilder.Sql("CREATE INDEX IX_Orders ON Orders (Id)");
            """);

        var result = await new EfSchemaExtractor()
            .ExtractAsync(Request("schema:App", Path.Combine(_root, "Migrations")), CancellationToken.None);

        Assert.Contains(
            Where(result, CSharpExtractor.DisclosurePredicate),
            a => a.Object == ExtractionDisclosures.SchemaChangedByRawSqlNotRead);
    }

    [Fact]
    public async Task TheSchemaIsAlwaysDisclosedAsTheMigrationsIntent_NotTheDatabase()
    {
        WriteMigration("20260101000000_Create", """
                    migrationBuilder.CreateTable(name: "Orders", columns: table => new { Id = 1 });
            """);

        var result = await new EfSchemaExtractor()
            .ExtractAsync(Request("schema:App", Path.Combine(_root, "Migrations")), CancellationToken.None);

        Assert.Contains(
            Where(result, CSharpExtractor.DisclosurePredicate),
            a => a.Object == ExtractionDisclosures.SchemaFromMigrationsNotDatabase);
    }

    // ---- the joins ----------------------------------------------------------

    private static EvidenceAssertion Fact(string subject, string predicate, string obj) =>
        new("scope", "rev-1", subject, predicate, obj, EvidenceOrigin.Static, VerificationStatus.Verified,
            new Provenance("f", "1:1", "test", "1.0.0", DateTimeOffset.UtcNow));

    [Fact]
    public void ACodeToSchemaJoinIsInferred_HoweverObviousItLooks()
    {
        // The temptation of Phase 3: this join is right almost always, and "almost always" is
        // exactly what Inferred means. Labelling it Verified would make a convention indistinguishable
        // from a declaration.
        var join = new JoinProjection([
            Fact("Shop.Order", "has_type", "class"),
            Fact("table:Orders", "has_type", "table"),
        ]).Compute();

        var edge = Assert.Single(join.Edges, e => e.Kind == "maps_to");
        Assert.Equal(VerificationStatus.Inferred, edge.Status);
        Assert.Contains("convention", edge.Basis, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, join.VerifiedCount);
    }

    [Fact]
    public void NoJoinIsMadeOnAnUnresolvedResourceName_AndTheGapIsDisclosed()
    {
        var join = new JoinProjection([
            Fact("table:Orders", "has_type", "table"),
            Fact("bicep:main/sql", "resource_type", "Microsoft.Sql/servers"),
            Fact("bicep:main/sql", "resource_name_expression", "'${prefix}-sql'"),
        ]).Compute();

        Assert.DoesNotContain(join.Edges, e => e.Kind == "hosted_on");
        Assert.Contains("sql-resource-name-unresolved", join.Disclosures);
    }

    [Fact]
    public void ASecureParameterIsJoinedAsSecret_WithoutItsValue()
    {
        var join = new JoinProjection([
            Fact("bicep:main#apiSecret", "has_type", "azure-parameter"),
            Fact("bicep:main#apiSecret", "is_secret", "true"),
        ]).Compute();

        var edge = Assert.Single(join.Edges, e => e.Kind == "is_declared_secret");
        Assert.Equal(VerificationStatus.Verified, edge.Status);
        Assert.Contains("never read", edge.Basis, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnrelatedNamesAreNotJoined()
    {
        // A looser rule — contains, or edit distance — produces confident wrong joins, and a wrong
        // join between a class and a table is the claim a user would never think to verify.
        var join = new JoinProjection([
            Fact("Shop.OrderProcessor", "has_type", "class"),
            Fact("table:Orders", "has_type", "table"),
        ]).Compute();

        Assert.DoesNotContain(join.Edges, e => e.Kind == "maps_to");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }
}
