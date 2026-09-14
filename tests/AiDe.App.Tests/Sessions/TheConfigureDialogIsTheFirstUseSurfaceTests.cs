using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// Ruling 104 (1): the sheet's Configure… is the first-use surface — the rendered dialog shows the
/// prerequisite rows before anything else, refuses an adapter root inside a git checkout beside the
/// field, writes <c>providers.json</c> with the typed label and <c>needs-login</c> for a skipped
/// sign-in, and the sheet re-derives its rows from the written file. No network, no npm, no sign-in.
/// </summary>
public sealed class TheConfigureDialogIsTheFirstUseSurfaceTests : IDisposable
{
    private readonly string _home = Directory.CreateTempSubdirectory("aide-configure-dialog-").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_home, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void TheDialogShowsPrerequisitesRefusesACheckoutRootAndWritesTheFile()
    {
        var providerFile = FirstUse.ProviderFilePath(_home);
        Directory.CreateDirectory(Path.Combine(_home, "repo", ".git"));
        var announced = new List<string>();
        var written = false;

        var (texts, rootState, writeState) = Sta.Run(() =>
        {
            var body = ConfigureProviderDialog.Build("anthropic", providerFile, null, announced.Add, () => written = true);
            var window = new Window { Content = body, Width = 640, SizeToContent = SizeToContent.Height, Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual, ShowInTaskbar = false };
            window.Show();
            try
            {
                var boxes = Descendants(body).OfType<TextBox>().ToList();
                var root = boxes.Single(b => System.Windows.Automation.AutomationProperties.GetName(b) == "Adapter root");
                var label = boxes.Single(b => System.Windows.Automation.AutomationProperties.GetName(b) == "Account label");
                var blocks = Descendants(body).OfType<TextBlock>().ToList();
                var rootStateBlock = blocks.Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Adapter root state");
                var writeStateBlock = blocks.Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Write state");
                var install = Descendants(body).OfType<Button>().Single(b => b.Content as string == "Install");
                var write = Descendants(body).OfType<Button>().Single(b => b.Content as string == "Write providers.json");

                // The default root is ~/.aide/adapters beside the file, and accepted.
                Assert.Equal(FirstUse.DefaultAdapterRoot(_home), root.Text);
                Assert.Equal(Visibility.Collapsed, rootStateBlock.Visibility);
                Assert.True(install.IsEnabled);

                // (b) A root inside a git checkout is refused beside the field, and Install is off.
                root.Text = Path.Combine(_home, "repo", "spikes", "acp-subscription-lane");
                var refusal = rootStateBlock.Text;
                Assert.False(install.IsEnabled);

                // (e) Write with the typed label; no sign-in happened, so health is needs-login.
                root.Text = FirstUse.DefaultAdapterRoot(_home);
                write.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                var refusedForLabel = writeStateBlock.Text;
                label.Text = "max";
                write.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

                return (blocks.Select(t => t.Text).ToList(), refusal, refusedForLabel + " | " + writeStateBlock.Text);
            }
            finally
            {
                window.Close();
            }
        });

        // (a) The three prerequisite rows, in order, before anything else.
        var order = texts.Where(t => t.StartsWith("✓ ", StringComparison.Ordinal) || t.StartsWith("⚠ ", StringComparison.Ordinal)).ToList();
        Assert.Equal(["node", "npm", "claude"], order.Take(3).Select(t => t[2..].Split(':')[0]));

        Assert.Contains("inside the git checkout at", rootState, StringComparison.Ordinal);
        Assert.Contains("An account needs a label", writeState, StringComparison.Ordinal);
        Assert.Contains("providers.json written: " + providerFile, writeState, StringComparison.Ordinal);
        Assert.True(written);
        Assert.Contains(announced, a => a.StartsWith("providers.json written:", StringComparison.Ordinal));

        // The file round-trips through the reader: one anthropic account, needs-login, the default root unwritten.
        var read = ProviderConfiguration.Read(providerFile);
        var account = Assert.Single(read.Registry.Find("anthropic").Accounts);
        Assert.Equal("max", account.Label);
        Assert.Equal(AccountHealth.NeedsLogin, account.Health);
        Assert.True(read.AdapterInstallRootIsDefault);
        Assert.Equal("claude-sonnet-5", read.ModelFor("claude-code"));

        // The sheet, reloaded from the written file, shows the account — needs sign-in once the
        // adapter is installed, not configured until then (the weaker of the two).
        var sheet = new NewSessionSheetViewModel(_home, _home, new ProviderRegistry([]), DateTimeOffset.UtcNow, adapterInstallRoot: null);
        Assert.Null(Assert.Single(sheet.AccountRows, r => r.ProviderId == "anthropic").Account);
        sheet.Reload(read.Registry, read.AdapterInstallRoot);
        var row = Assert.Single(sheet.AccountRows, r => r.ProviderId == "anthropic");
        Assert.Equal("max", row.Account?.Label);
        Assert.Equal(AccountRowState.NotConfigured, row.State);
        sheet.Reload(read.Registry, InstalledAdapterRoot.Create(_home));
        Assert.Equal(AccountRowState.NeedsSignIn, Assert.Single(sheet.AccountRows, r => r.ProviderId == "anthropic").State);
    }

    /// <summary>A native-CLI provider's dialog shows the spike's install line as copy with its citation, and Install is off.</summary>
    [Fact]
    public void ANativeProvidersDialogShowsItsInstallLineAsCitedCopy()
    {
        var texts = Sta.Run(() =>
        {
            var body = ConfigureProviderDialog.Build("github", FirstUse.ProviderFilePath(_home), null, null, () => { });
            var install = Descendants(body).OfType<Button>().Single(b => b.Content as string == "Install");
            Assert.False(install.IsEnabled);
            return Descendants(body).OfType<TextBlock>().Select(t => t.Text).ToList();
        });

        Assert.Contains(texts, t => t.Contains("winget install GitHub.Copilot", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("docs/spikes/engine-backends-2026-09-14.md", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("copilot login", StringComparison.Ordinal));
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
