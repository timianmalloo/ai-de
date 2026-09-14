using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// Ruling 105 (2) and condition 4, Ruling 104 (3) and condition 3, Ruling 97(i) as subsumed: the
/// New Session sheet lists <b>accounts</b> grouped under their provider with the engine as the
/// sub-line; every catalog engine's provider appears (nothing hidden); a provider with no account
/// still shows one row; the row state is the weaker of (launch path observed?, account health) and
/// is derived at open time, never stored; Create stays enabled with the truthful footer when nothing
/// is ready; the binder's refusal names the action.
/// </summary>
public sealed class TheSheetListsAccountsWithDerivedStatesTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("aide-sheet-accounts-").FullName;
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    /// <summary>An adapter root where claude-code's pinned entry module IS on disk — launch observed, installed.</summary>
    private string InstalledRoot()
    {
        var row = EngineCatalog.Find("claude-code");
        var entry = Path.Combine(_root, "adapters", "node_modules", row.AdapterPackage!, row.AdapterEntryModule!.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(entry)!);
        File.WriteAllText(entry, "// stand-in entry module");
        return Path.Combine(_root, "adapters");
    }

    private static ProviderRegistry Registry(params ProviderRow[] rows) => new(rows);

    private static ProviderRow Anthropic(params ProviderAccount[] accounts) =>
        new("anthropic", ProviderAuth.Subscription, accounts);

    /// <summary>
    /// Condition 4: an account with <c>ready</c> health under an engine whose launch is unobserved
    /// renders <i>not configured</i>; the same account under an observed, installed launch renders
    /// <i>ready</i>; <c>needs-login</c> under an installed launch renders <i>needs sign-in</i>.
    /// </summary>
    [Fact]
    public void TheRowStateIsTheWeakerOfLaunchPathAndHealth()
    {
        var registry = Registry(Anthropic(new ProviderAccount("max", AccountHealth.Ready)));

        // No adapter root at all (no provider file on a fresh machine): launch unobserved.
        var unobserved = new NewSessionSheetViewModel(_root, _root, registry, Now, adapterInstallRoot: null);
        var row = Assert.Single(unobserved.AccountRows, r => r.ProviderId == "anthropic");
        Assert.Equal("max", row.Account?.Label);
        Assert.Equal(AccountRowState.NotConfigured, row.State);
        Assert.Equal("claude-code", row.EngineId);

        // An adapter root whose entry module is missing: composed by ResolveLaunch, not on disk.
        var missing = new NewSessionSheetViewModel(_root, _root, registry, Now, adapterInstallRoot: Path.Combine(_root, "empty"));
        Assert.Equal(AccountRowState.NotConfigured, Assert.Single(missing.AccountRows, r => r.ProviderId == "anthropic").State);

        // Installed: ready health → ready.
        var installed = new NewSessionSheetViewModel(_root, _root, registry, Now, adapterInstallRoot: InstalledRoot());
        Assert.Equal(AccountRowState.Ready, Assert.Single(installed.AccountRows, r => r.ProviderId == "anthropic").State);

        // Installed, needs-login → needs sign-in.
        var needsLogin = new NewSessionSheetViewModel(
            _root, _root, Registry(Anthropic(new ProviderAccount("max", AccountHealth.NeedsLogin))), Now, adapterInstallRoot: InstalledRoot());
        Assert.Equal(AccountRowState.NeedsSignIn, Assert.Single(needsLogin.AccountRows, r => r.ProviderId == "anthropic").State);
    }

    /// <summary>Condition 4: a provider with no accounts renders exactly one Configure row; every catalog provider appears.</summary>
    [Fact]
    public void EveryCatalogProviderAppears_AndAProviderWithNoAccountsRendersOneConfigureRow()
    {
        var sheet = new NewSessionSheetViewModel(_root, _root, Registry(), Now, adapterInstallRoot: InstalledRoot());

        var providers = EngineCatalog.Rows.Select(r => r.Provider).Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(providers, sheet.AccountRows.Select(r => r.ProviderId).Distinct(StringComparer.Ordinal));

        foreach (var provider in providers)
        {
            var row = Assert.Single(sheet.AccountRows, r => r.ProviderId == provider);
            Assert.Null(row.Account);
            Assert.Equal("no account — Configure…", row.AccountLabel);
        }

        // claude-code's launch is installed here, so its zero-account row reads needs sign-in — the
        // health half is the weaker; codex/copilot are unobserved launches → not configured.
        Assert.Equal(AccountRowState.NeedsSignIn, sheet.AccountRows.Single(r => r.EngineId == "claude-code").State);
        Assert.Equal(AccountRowState.NotConfigured, sheet.AccountRows.Single(r => r.EngineId == "codex").State);
    }

    /// <summary>Ruling 104 (3): Create stays enabled; with zero ready accounts the footer reads exactly the ruled sentence.</summary>
    [Fact]
    public void CreateStaysEnabledAndTheFooterIsTruthfulWhenNothingIsReady()
    {
        var nothing = new NewSessionSheetViewModel(_root, _root, Registry(), Now, adapterInstallRoot: null);
        Assert.True(nothing.CanCreate);
        Assert.Equal(
            "No backend is ready. The session will open; a run will not start until one is configured.",
            nothing.ReadinessFooter);

        var created = nothing.Create(Now);
        Assert.Empty(created.Config.Accounts);
        Assert.Null(created.Config.DefaultAccount);

        var ready = new NewSessionSheetViewModel(
            _root, _root, Registry(Anthropic(new ProviderAccount("max", AccountHealth.Ready))), Now, adapterInstallRoot: InstalledRoot());
        Assert.Null(ready.ReadinessFooter);
        Assert.Equal(new AccountRef("anthropic", "max"), ready.Create(Now).Config.DefaultAccount);
    }

    /// <summary>The rendered sheet: provider groups, the engine sub-line, the state word and the footer are on screen.</summary>
    [Fact]
    public void TheRenderedSheetShowsGroupsStatesAndTheFooter()
    {
        var sheet = new NewSessionSheetViewModel(
            _root, _root, Registry(Anthropic(new ProviderAccount("max", AccountHealth.Ready))), Now, adapterInstallRoot: null);

        var texts = Sta.Run(() =>
        {
            var body = NewSessionSheetDialog.Build(sheet, announce: null, onCreate: () => { });
            var window = new Window { Content = body, Width = 520, SizeToContent = SizeToContent.Height, Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual, ShowInTaskbar = false };
            window.Show();
            try
            {
                return Descendants(body).OfType<TextBlock>().Select(t => t.Text).ToList();
            }
            finally
            {
                window.Close();
            }
        });

        Assert.Contains("anthropic", texts);
        Assert.Contains(texts, t => t.Contains("claude-code", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("not configured", StringComparison.Ordinal));
        Assert.Contains("openai", texts);
        Assert.Contains("github", texts);
        Assert.Contains("No backend is ready. The session will open; a run will not start until one is configured.", texts);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var grandchild in Descendants(child))
            {
                yield return grandchild;
            }
        }
    }
}
