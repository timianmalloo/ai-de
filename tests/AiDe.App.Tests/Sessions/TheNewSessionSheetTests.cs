using System.Reflection;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// R13 b1/b2 and Rulings 19–20: what the New Session sheet collects, what it refuses, and what it
/// deliberately does not carry.
/// </summary>
public sealed class TheNewSessionSheetTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "aide-f2-sheet-" + Guid.NewGuid().ToString("N"));

    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    public TheNewSessionSheetTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private static ProviderRegistry Registry(AccountHealth anthropic = AccountHealth.Ready) => new(
    [
        new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max-personal", anthropic)]),
        new ProviderRow("openai", ProviderAuth.Subscription, [new ProviderAccount("chatgpt-personal", AccountHealth.QuotaDegraded)]),
    ]);

    private NewSessionSheetModel Sheet(ProviderRegistry? registry = null, Func<string, bool>? login = null, Func<ProviderRegistry>? reprobe = null) =>
        new(_root, _root, registry ?? Registry(), Now, login, reprobe);

    // ── binding (R13 b1) ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ASheetCannotBeConstructedWithoutAWorkspace()
    {
        // A2: a session cannot exist unbound. Unreachable rather than refused — there is no path
        // through this type that reaches SessionConfigStore.Create without a workspace.
        Assert.Throws<ArgumentException>(() => new NewSessionSheetModel("", _root, Registry(), Now));
        Assert.Throws<ArgumentException>(() => new NewSessionSheetModel(_root, "", Registry(), Now));
    }

    [Fact]
    public void WithAnActiveWorkspaceTheSheetOpensPreBound()
    {
        NewSessionSheetModel? shown = null;

        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => _root,
            chooseWorkspace: () => throw new InvalidOperationException(
                "the chooser must not interpose when a workspace is active"),
            showSheet: sheet => { shown = sheet; sheet.TaskClass = "feature"; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = flow.Start();

        Assert.NotNull(shown);
        Assert.Equal(_root, shown.WorkspaceRoot);
        Assert.NotNull(outcome.Created);
        Assert.Equal(_root, outcome.Created.Config.WorkspaceId);
    }

    [Fact]
    public void WithNoWorkspaceTheChooserInterposes_AndCancelAbortsCleanly()
    {
        var sheetShown = false;

        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => null,
            chooseWorkspace: () => null,                     // cancelled
            showSheet: _ => { sheetShown = true; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = flow.Start();

        Assert.Null(outcome.Created);
        Assert.False(sheetShown, "the sheet opened after the chooser was cancelled");
        Assert.Contains("cancelled", outcome.Announcement, StringComparison.OrdinalIgnoreCase);

        // Nothing was written: no session can exist unbound, so a cancelled chooser leaves no trace.
        Assert.False(Directory.Exists(SessionPaths.SessionsRoot(_root)));
    }

    [Fact]
    public void TheChooserFeedsTheSheetWhenAWorkspaceIsChosen()
    {
        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => null,
            chooseWorkspace: () => _root,
            showSheet: sheet => { sheet.TaskClass = "feature"; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = flow.Start();

        Assert.NotNull(outcome.Created);
        Assert.True(Directory.Exists(SessionPaths.SessionDirectory(_root, outcome.Created.Config.SessionId)));
    }

    // ── backends (R13 b2) ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TheSheetsHealthValuesAreReferenceEqualToTheRegistrys()
    {
        // "Fails if: a second health model appears." Reference equality is the only form of that
        // claim a rename cannot weaken: a projected copy would still read "ready" after a re-probe.
        var registry = Registry();
        var sheet = Sheet(registry);

        var account = registry.Find("anthropic").Accounts[0];
        var row = sheet.Backends.Single(b => b.EngineId == "claude-code");

        Assert.Same(account, row.Account);
        Assert.Equal(account.Health, row.Health);
    }

    [Fact]
    public void BackendsComeFromTheCatalogFilteredByTheRegistry()
    {
        var sheet = Sheet();

        // claude-code (anthropic) and codex (openai) are configured; copilot's provider (github) is
        // not, so it is absent rather than listed and then refused.
        Assert.Equal(["claude-code", "codex"], sheet.Backends.Select(b => b.EngineId));
        Assert.All(sheet.Backends, b => Assert.Contains(
            EngineCatalog.Rows, row => row.Id == b.EngineId && row.Provider == b.ProviderId));
    }

    [Fact]
    public void ANeedsLoginEngineIsNeverOfferedToTheRouter()
    {
        var sheet = Sheet(Registry(AccountHealth.NeedsLogin));

        // It is shown, with its health (Ruling 20 keeps the display) ...
        Assert.Contains(sheet.Backends, b => b.EngineId == "claude-code" && b.Health == AccountHealth.NeedsLogin);

        // ... and the operator may even enable it ...
        sheet.SetBackendEnabled("claude-code", true);
        Assert.Contains("claude-code", sheet.EnabledBackends);

        // ... but it is still refused for routing.
        Assert.DoesNotContain("claude-code", sheet.RoutableBackends);

        // A quota-degraded account is a pressure signal, not an absence — it still routes.
        Assert.Contains("codex", sheet.RoutableBackends);
    }

    [Fact]
    public void SignInIsClaudeCodeOnly_AndReprobesOnReturn()
    {
        var launched = new List<string>();
        var reprobed = Registry();

        var sheet = Sheet(
            Registry(AccountHealth.NeedsLogin),
            login: engine => { launched.Add(engine); return true; },
            reprobe: () => reprobed);

        Assert.True(sheet.CanSignIn("claude-code"));
        Assert.False(sheet.CanSignIn("codex"));

        var refusal = sheet.SignIn("codex");
        Assert.Empty(launched);
        Assert.Contains("claude-code", refusal, StringComparison.Ordinal);

        var announced = sheet.SignIn("claude-code");
        Assert.Equal(["claude-code"], launched);
        Assert.True(sheet.HealthWasReprobed);
        Assert.Contains("ready", announced, StringComparison.Ordinal);
        Assert.Contains("claude-code", sheet.RoutableBackends);
    }

    // ── task class and lease (Ruling 19) ─────────────────────────────────────────────────

    [Fact]
    public void ARunCannotStartOnADefaultedTaskClass()
    {
        var sheet = Sheet();

        Assert.Null(sheet.TaskClass);
        Assert.False(sheet.CanCreate);
        Assert.Contains("task class", sheet.BlockedReason!, StringComparison.OrdinalIgnoreCase);

        var refused = Assert.Throws<InvalidOperationException>(() => sheet.Create(Now));
        Assert.Contains("no default", refused.Message, StringComparison.OrdinalIgnoreCase);

        // Nothing was written by the refusal.
        Assert.False(Directory.Exists(SessionPaths.SessionsRoot(_root)));
    }

    [Fact]
    public void NoParameterDefaultCanSupplyATaskClass()
    {
        // The mechanical half: DC-110 is a defaulted class ranking in the wrong cohort, and the way
        // one arrives is an optional parameter nobody looked at. Both the composition root and the
        // sheet's own result are checked, so a default cannot be introduced on either side.
        AssertNoDefaultFor(typeof(GovernedRunRequest), "TaskClass");
        AssertNoDefaultFor(typeof(NewSessionResult), "TaskClass");
    }

    [Fact]
    public void TheLeaseIsDerivedAndDisplayed()
    {
        var sheet = Sheet();

        Assert.NotNull(sheet.Lease);
        Assert.True(sheet.Lease.Covers("src/Payments/PaymentAggregate.cs"));
        Assert.Contains(Path.GetFileName(_root), sheet.LeaseDisplay, StringComparison.Ordinal);
        Assert.Contains("goal block", sheet.LeaseDisplay, StringComparison.OrdinalIgnoreCase);

        // Derived, not typed: there is no setter for it on the sheet.
        Assert.Null(typeof(NewSessionSheetModel).GetProperty(nameof(NewSessionSheetModel.Lease))!.SetMethod);
    }

    [Fact]
    public void TheFourCutFieldsAreAbsentFromTheSheet()
    {
        // Ruling 19 cut routing mode, autonomy, default policy and per-session MCP because
        // GovernedRunRequest takes none of them — a field for any would collect a value the run
        // cannot consume.
        var members = typeof(NewSessionSheetModel)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToList();

        foreach (var cut in (string[])["Routing", "Autonomy", "Policy", "Mcp"])
        {
            Assert.DoesNotContain(members, m => m.Contains(cut, StringComparison.OrdinalIgnoreCase));
        }

        // And the row Ruling 26 (iii) cut, which would create a back-edge from the composer.
        Assert.DoesNotContain(members, m => m.Contains("Template", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateWritesTheSessionAndEmitsItsOpenEvent()
    {
        var sheet = Sheet();
        sheet.TaskClass = "feature";

        var created = sheet.Create(Now);

        Assert.Equal("feature", created.TaskClass);
        Assert.Equal(_root, created.Config.WorkspaceId);
        Assert.True(File.Exists(SessionPaths.SessionFile(_root, created.Config.SessionId)));

        var events = new SessionConfigStore(_root, created.Config.SessionId).ReadEvents();
        Assert.Equal(SessionEventKinds.Open, events.Single().Kind);
    }

    [Fact]
    public void TheDefaultNameIsADateSlug()
    {
        // A4.3: default is a date-slug, renameable later. Invariant culture, so a session listing
        // sorts the same on every machine.
        Assert.StartsWith("2026-09-10", Sheet().Name, StringComparison.Ordinal);
    }

    private static void AssertNoDefaultFor(Type type, string parameter)
    {
        var withDefaults = type.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Where(p => string.Equals(p.Name, parameter, StringComparison.Ordinal) && p.HasDefaultValue)
            .ToList();

        Assert.True(
            withDefaults.Count == 0,
            $"{type.Name} has a default for '{parameter}'; a defaulted task class ranks in the wrong "
            + "cohort (DC-110) and the run's own comment says so");

        // And the parameter really exists, or the check above would pass by looking at nothing.
        Assert.Contains(
            type.GetConstructors().SelectMany(c => c.GetParameters()),
            p => string.Equals(p.Name, parameter, StringComparison.Ordinal));
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
