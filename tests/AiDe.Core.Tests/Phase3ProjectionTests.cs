using AiDe.Core.Dispatch;
using AiDe.Core.Extraction;
using AiDe.Core.Facts;
using AiDe.Core.Projections;
using AiDe.Core.Terminal;

namespace AiDe.Core.Tests;

/// <summary>
/// The context projection, agent readiness, declared table joins and Bicep modules.
/// </summary>
public sealed class Phase3ProjectionTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-p3proj", Guid.NewGuid().ToString("N"));

    public Phase3ProjectionTests() => Directory.CreateDirectory(_root);

    private string Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static EvidenceAssertion Fact(string subject, string predicate, string obj) =>
        new("scope", "rev-1", subject, predicate, obj, EvidenceOrigin.Static, VerificationStatus.Verified,
            new Provenance("f", "1:1", "test", "1.0.0", DateTimeOffset.UtcNow));

    // ---- the context projection --------------------------------------------

    private BoundedContextMap TwoContexts(params string[] symbols)
    {
        var path = Write("docs/bounded-contexts.yaml", """
            contexts:
              - name: Sales
                includes:
                  - Shop.Sales.*
              - name: Billing
                includes:
                  - Shop.Billing.*
            """);

        return BoundedContextReader.Load(path, symbols);
    }

    [Fact]
    public void ACrossingBetweenContextsIsCountedSeparatelyFromAnEdgeInsideOne()
    {
        // The whole point of drawing contexts: the same edge means something quite different across
        // a boundary than inside one, and nothing else in the graph distinguishes them.
        var map = TwoContexts("Shop.Sales.Order", "Shop.Sales.Basket", "Shop.Billing.Invoice");

        var view = new ContextProjection(map, [
            Fact("Shop.Sales.Order", "depends_on", "Shop.Sales.Basket"),
            Fact("Shop.Sales.Order", "depends_on", "Shop.Billing.Invoice"),
        ]).Compute();

        var sales = view.Contexts.First(c => c.Name == "Sales");
        Assert.Equal(1, sales.InternalEdges);
        Assert.Equal(1, sales.Crossings);

        var crossing = Assert.Single(view.Edges);
        Assert.Equal("Sales", crossing.From);
        Assert.Equal("Billing", crossing.To);
    }

    [Fact]
    public void DirectionIsKept_BecauseWhoDependsOnWhomIsThePoint()
    {
        var map = TwoContexts("Shop.Sales.Order", "Shop.Billing.Invoice");

        var view = new ContextProjection(map, [
            Fact("Shop.Billing.Invoice", "depends_on", "Shop.Sales.Order"),
        ]).Compute();

        var crossing = Assert.Single(view.Edges);
        Assert.Equal("Billing", crossing.From);
        Assert.Equal("Sales", crossing.To);
    }

    [Fact]
    public void AnInvalidMapDrawsNothing()
    {
        // A partially-applied context map is a diagram that is wrong in a way nobody can see, which
        // is worse than an absent one.
        var path = Write("docs/bounded-contexts.yaml", """
            contexts:
              - name: Ghost
                includes:
                  - Shop.Missing.*
            """);

        var view = new ContextProjection(BoundedContextReader.Load(path, ["Shop.Sales.Order"]), []).Compute();

        Assert.False(view.IsValid);
        Assert.Empty(view.Contexts);
        Assert.NotEmpty(view.Problems);
    }

    [Fact]
    public void AttributePredicatesAreNotCountedAsCrossings()
    {
        // has_type and declared_in are properties of a subject, not traffic between contexts, and
        // counting them would make every context look heavily coupled to itself.
        //
        // BOTH symbols are supplied because the fixture declares two contexts, and a context whose
        // pattern matches nothing invalidates the whole map — which the validator is right to do and
        // which the first version of this test tripped over.
        var map = TwoContexts("Shop.Sales.Order", "Shop.Billing.Invoice");

        var view = new ContextProjection(map, [
            Fact("Shop.Sales.Order", "has_type", "class"),
            Fact("Shop.Sales.Order", "declared_in", "scope"),
        ]).Compute();

        Assert.Equal(0, view.Contexts.First(c => c.Name == "Sales").InternalEdges);
    }

    // ---- agent readiness ----------------------------------------------------

    [Fact]
    public void AnAgentPromptMarkerMakesTheSessionReady()
    {
        var watcher = new AgentReadinessWatcher(AgentReadinessWatcher.KnownAgents["claude"]);

        watcher.Observe("Welcome to Claude Code\n");
        Assert.False(watcher.IsReady);

        watcher.Observe("\n❯ ");
        Assert.True(watcher.IsReady);
    }

    [Fact]
    public void ReadinessIsLostAgainWhenTheAgentStartsAnswering()
    {
        // A watcher that latched Ready once would report a mid-response agent as available — the
        // trust-gate failure one step later.
        var watcher = new AgentReadinessWatcher(AgentReadinessWatcher.KnownAgents["claude"]);

        watcher.Observe("\n❯ ");
        Assert.True(watcher.IsReady);

        watcher.Observe("Thinking about your question...");
        Assert.False(watcher.IsReady);
    }

    [Fact]
    public void AMarkerEarlierInTheBufferIsHistory_NotReadiness()
    {
        var watcher = new AgentReadinessWatcher(AgentReadinessWatcher.KnownAgents["claude"]);

        watcher.Observe("\n❯ run the tests\nRunning...\n");
        Assert.False(watcher.IsReady);
    }

    [Fact]
    public void PatternEvidenceIsAcceptedForReadiness_ButIsANamedDifferentKind()
    {
        // It establishes that the agent is LISTENING. ADR-0007's bar for claiming the agent ACCEPTED
        // a prompt is unchanged and still unmet — which is why the evidence kind has its own name.
        var session = new FixtureTerminalSession("s", 1);

        Assert.Equal(
            SessionReadiness.Ready,
            SessionReadinessPolicy.Evaluate(session, ReadinessEvidence.ObservedPattern));

        Assert.Equal(
            SessionReadiness.Unknown,
            SessionReadinessPolicy.Evaluate(session, ReadinessEvidence.None));
    }

    // ---- declared table joins ----------------------------------------------

    [Fact]
    public void ADeclaredTableAttributeProducesAVerifiedJoin()
    {
        var join = new JoinProjection([
            Fact("Shop.Order", "has_type", "class"),
            Fact("Shop.Order", "declares_table", "orders"),
            Fact("table:orders", "has_type", "table"),
        ]).Compute();

        var edge = Assert.Single(join.Edges, e => e.Kind == "maps_to");
        Assert.Equal(VerificationStatus.Verified, edge.Status);
        Assert.Contains("[Table(", edge.Basis, StringComparison.Ordinal);
    }

    [Fact]
    public void ADeclaredTypeIsNotAlsoJoinedByConvention()
    {
        // Two edges between the same pair — one Verified, one Inferred — would leave the user
        // deciding which to believe about a question the code already answers.
        var join = new JoinProjection([
            Fact("Shop.Order", "has_type", "class"),
            Fact("Shop.Order", "declares_table", "legacy_orders"),
            Fact("table:legacy_orders", "has_type", "table"),
            Fact("table:Orders", "has_type", "table"),
        ]).Compute();

        Assert.Single(join.Edges, e => e.Kind == "maps_to");
        Assert.Equal(1, join.VerifiedCount);
        Assert.Equal(0, join.InferredCount);
    }

    [Fact]
    public async Task AFluentToTableCallIsReadAsADeclaration()
    {
        // The MORE common style than the attribute. Without it, the commonest way of stating the
        // mapping fell back to a name-matching guess.
        var project = Write("Fluent/Fluent.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        Write("Fluent/Context.cs", """
            namespace Shop;

            public class Order { }

            public class AppContext
            {
                public void OnModelCreating(dynamic modelBuilder)
                {
                    modelBuilder.Entity<Order>().ToTable("orders");
                }
            }
            """);

        var result = await new CSharpExtractor().ExtractAsync(
            new ExtractionRequest("csharp:Fluent:net10.0", project, "rev-1", 1), CancellationToken.None);

        Assert.Contains(
            result.Assertions,
            a => a.Predicate == "declares_table" && a.Object == "orders");
    }

    [Fact]
    public async Task ATableNameThatIsNotALiteralIsNotRead()
    {
        // Same rule the Bicep reader follows: a guessed name produces a confident wrong join.
        var project = Write("Var/Var.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        Write("Var/Context.cs", """
            namespace Shop;

            public class Order { }

            public class AppContext
            {
                private const string Name = "orders";
                public void OnModelCreating(dynamic modelBuilder)
                {
                    modelBuilder.Entity<Order>().ToTable(Name);
                }
            }
            """);

        var result = await new CSharpExtractor().ExtractAsync(
            new ExtractionRequest("csharp:Var:net10.0", project, "rev-1", 1), CancellationToken.None);

        Assert.DoesNotContain(result.Assertions, a => a.Predicate == "declares_table");
    }

    // ---- Bicep modules ------------------------------------------------------

    [Fact]
    public async Task ModulesAreExtractedWithTheirPath()
    {
        // Read but never exercised until now: TheTerrace's templates declare none.
        var path = Write("infra/main.bicep", """
            param namePrefix string = 'demo'

            module vault 'provider-vault.bicep' = {
              name: 'vault-deploy'
              params: {
                prefix: namePrefix
              }
            }

            module network './modules/net.bicep' = {
              name: 'net-deploy'
            }
            """);

        var result = await new BicepExtractor().ExtractAsync(
            new ExtractionRequest("bicep:main", path, "rev-1", 1), CancellationToken.None);

        var modules = result.Assertions.Where(a => a.Predicate == "has_type" && a.Object == "azure-module").ToList();
        Assert.Equal(2, modules.Count);

        var paths = result.Assertions.Where(a => a.Predicate == "module_path").Select(a => a.Object).ToList();
        Assert.Contains("provider-vault.bicep", paths);
        Assert.Contains("./modules/net.bicep", paths);
    }

    [Fact]
    public async Task AModuleIsNotCountedAsAResource()
    {
        // They are different things: a resource is deployed, a module is another template. Counting
        // modules as resources would inflate a resource count nobody would re-check.
        var path = Write("infra/only-modules.bicep", """
            module vault 'provider-vault.bicep' = {
              name: 'vault-deploy'
            }
            """);

        var result = await new BicepExtractor().ExtractAsync(
            new ExtractionRequest("bicep:only", path, "rev-1", 1), CancellationToken.None);

        Assert.DoesNotContain(result.Assertions, a => a.Predicate == "has_type" && a.Object == "azure-resource");
        Assert.Contains(result.Assertions, a => a.Predicate == "has_type" && a.Object == "azure-module");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }
}
