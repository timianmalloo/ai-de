using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Sessions;
using AiDe.Core.Presentation.Sessions;

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

    private NewSessionSheetViewModel Sheet(ProviderRegistry? registry = null, Func<string, bool>? login = null, Func<ProviderRegistry>? reprobe = null) =>
        new(_root, _root, registry ?? Registry(), Now, login, reprobe);

    // ── binding (R13 b1) ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ASheetCannotBeConstructedWithoutAWorkspace()
    {
        // A2: a session cannot exist unbound. Unreachable rather than refused — there is no path
        // through this type that reaches SessionConfigStore.Create without a workspace.
        Assert.Throws<ArgumentException>(() => new NewSessionSheetViewModel("", _root, Registry(), Now));
        Assert.Throws<ArgumentException>(() => new NewSessionSheetViewModel(_root, "", Registry(), Now));
    }

    [Fact]
    public async Task WithAnActiveWorkspaceTheSheetOpensPreBound()
    {
        NewSessionSheetViewModel? shown = null;

        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => _root,
            chooseWorkspace: () => throw new InvalidOperationException(
                "the chooser must not interpose when a workspace is active"),
            openWorkspace: _ => throw new InvalidOperationException(
                "nothing is opened when a workspace is active"),
            showSheet: sheet => { shown = sheet; sheet.TaskClass = "feature"; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = await flow.StartAsync();

        Assert.NotNull(shown);
        Assert.Equal(_root, shown.WorkspaceRoot);
        Assert.NotNull(outcome.Created);
        Assert.Equal(_root, outcome.Created.Config.WorkspaceId);
    }

    [Fact]
    public async Task WithNoWorkspaceTheChooserInterposes_AndCancelAbortsCleanly()
    {
        var sheetShown = false;
        var opened = new List<string>();

        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => null,
            chooseWorkspace: () => null,                     // cancelled
            openWorkspace: root => { opened.Add(root); return Task.FromResult<string?>(null); },
            showSheet: _ => { sheetShown = true; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = await flow.StartAsync();

        Assert.Null(outcome.Created);
        Assert.False(sheetShown, "the sheet opened after the chooser was cancelled");
        Assert.Empty(opened);
        Assert.Contains("cancelled", outcome.Announcement, StringComparison.OrdinalIgnoreCase);

        // Nothing was written: no session can exist unbound, so a cancelled chooser leaves no trace.
        Assert.False(Directory.Exists(SessionPaths.SessionsRoot(_root)));
    }

    /// <summary>
    /// <b>INV-0009 Phase 3 (DC-149).</b> The chosen workspace is OPENED in the window before the
    /// sheet, and the sheet binds to the workspace the window then reports — so the session and the
    /// window are bound to the same workspace before the composer is, and the refusal of 22:33:28Z
    /// (<i>repositoryRoot: this window has no open workspace</i>) cannot occur.
    /// </summary>
    [Fact]
    public async Task TheChooserOpensTheChosenWorkspace_ThenTheSheetBindsToWhatTheWindowReports()
    {
        // The window reports the opened root in ITS OWN form — here, upper-cased, the same directory
        // on Windows — so the sheet's root can be told apart from the chooser's: a flow that binds
        // the sheet to the chosen string rather than to what the window reports goes red here.
        var windowForm = _root.ToUpperInvariant();
        string? windowRoot = null;
        var order = new List<string>();

        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => windowRoot,
            chooseWorkspace: () => { order.Add("choose"); return _root; },
            openWorkspace: root =>
            {
                order.Add("open:" + root);
                windowRoot = windowForm;                      // what the window reports once opened
                return Task.FromResult<string?>(null);
            },
            showSheet: sheet => { order.Add("sheet:" + sheet.WorkspaceRoot); sheet.TaskClass = "feature"; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = await flow.StartAsync();

        Assert.Equal(["choose", "open:" + _root, "sheet:" + windowForm], order);
        Assert.NotNull(outcome.Created);
        Assert.Equal(windowForm, outcome.Created.Config.WorkspaceId);
        Assert.True(Directory.Exists(SessionPaths.SessionDirectory(_root, outcome.Created.Config.SessionId)));
    }

    /// <summary>The open path said yes and the window still reports no workspace: the flow refuses, naming the folder, rather than binding to the chooser's string.</summary>
    [Fact]
    public async Task TheChooserOpensTheChosenWorkspace_AndAWindowThatStillReportsNoneCreatesNothing()
    {
        var sheetShown = false;

        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => null,
            chooseWorkspace: () => _root,
            openWorkspace: _ => Task.FromResult<string?>(null),   // "opened", but the window never reports it
            showSheet: _ => { sheetShown = true; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = await flow.StartAsync();

        Assert.Null(outcome.Created);
        Assert.False(sheetShown);
        Assert.Contains(_root, outcome.Announcement, StringComparison.Ordinal);
        Assert.Contains("reports no workspace", outcome.Announcement, StringComparison.Ordinal);
        Assert.False(Directory.Exists(SessionPaths.SessionsRoot(_root)));
    }

    /// <summary>A workspace that does not open is a refusal that says why; no sheet, no session.</summary>
    [Fact]
    public async Task TheChooserOpensTheChosenWorkspace_AndAFailedOpenCreatesNothing()
    {
        var sheetShown = false;

        var flow = new NewSessionFlow(
            activeWorkspaceRoot: () => null,
            chooseWorkspace: () => _root,
            openWorkspace: _ => Task.FromResult<string?>("Could not reach the workspace daemon."),
            showSheet: _ => { sheetShown = true; return true; },
            registry: () => Registry(),
            workspaceId: root => root,
            time: new FixedTime(Now));

        var outcome = await flow.StartAsync();

        Assert.Null(outcome.Created);
        Assert.False(sheetShown, "the sheet opened over a workspace that did not open");
        Assert.Equal("Could not reach the workspace daemon.", outcome.Announcement);
        Assert.False(Directory.Exists(SessionPaths.SessionsRoot(_root)));
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
    public void NoLeaseLeavesTheSheet()
    {
        // RULING 42. A lease belongs to the goal block (§14.3), and a lease derived here could only
        // cover everything — which GovernedRunRequest would then have accepted as a real one. The
        // failure is not a weak display: it is a seam control that never fires, arriving downstream
        // looking like a working one. So the value is ABSENT, not defaulted and not nullable, and
        // the first node wiring sheet-to-run has nothing to pick up.
        var sheet = Sheet();
        sheet.TaskClass = "feature";

        var created = sheet.Create(Now);

        Assert.DoesNotContain(
            typeof(NewSessionResult).GetProperties(),
            p => p.Name.Contains("Lease", StringComparison.OrdinalIgnoreCase)
                || p.PropertyType == typeof(Lease));

        Assert.DoesNotContain(
            typeof(NewSessionSheetViewModel).GetMembers(BindingFlags.Public | BindingFlags.Instance),
            m => m.Name.Equals("Lease", StringComparison.Ordinal));

        // The sheet still SAYS something about the lease — the absence itself, which is the true
        // statement — and it is a sentence rather than a Lease.
        Assert.Equal("not derivable until a goal block exists", NewSessionSheetViewModel.LeaseDisplay);
        Assert.IsType<string>(NewSessionSheetViewModel.LeaseDisplay);

        // And the check is looking at something: the result really does carry its other two fields.
        Assert.Equal("feature", created.TaskClass);
        Assert.NotNull(created.Config);
    }

    [Fact]
    public void TheSheetRendersThatSentence() => Sta.Run(() =>
    {
        // What the OPERATOR reads, not what the model holds. The dialog's body is built without its
        // window precisely so this is assertable — ShowDialog blocks, so a test that had to open the
        // window could only ever check the model again.
        var sheet = Sheet();
        var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });

        var rendered = Blocks(body).Select(b => b.Text).ToList();

        Assert.Contains("Lease", rendered);
        Assert.Contains("not derivable until a goal block exists", rendered);
        Assert.DoesNotContain(rendered, line => line.Contains("the whole of", StringComparison.Ordinal));
    });

    /// <summary>
    /// Every <see cref="TextBlock"/> in a built tree.
    /// </summary>
    /// <remarks>
    /// Walks the LOGICAL tree, not the visual one: the sheet's body is built and never shown, so it
    /// has no visual tree to walk and a <c>VisualTreeHelper</c> sweep would find nothing and pass.
    /// </remarks>
    private static IEnumerable<TextBlock> Blocks(DependencyObject root)
    {
        if (root is TextBlock block)
        {
            yield return block;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            foreach (var found in Blocks(child))
            {
                yield return found;
            }
        }
    }

    [Fact]
    public void TheFourCutFieldsAreAbsentFromTheSheet()
    {
        // Ruling 19 cut routing mode, autonomy, default policy and per-session MCP because
        // GovernedRunRequest takes none of them — a field for any would collect a value the run
        // cannot consume.
        var members = typeof(NewSessionSheetViewModel)
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
