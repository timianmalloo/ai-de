using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;

namespace AiDe.App.Tests.Composer;

/// <summary>
/// The E7 surface list for node F6, end to end:
/// <c>file → reader → registry → sheet → EnabledBackends → ComposerSendContext → Send →
/// GovernedRunRequest</c>. Every arrow, and nothing invented at any of them.
/// </summary>
/// <remarks>
/// <para><b>Why the whole chain rather than each link.</b> This is DC-130's control. The class was
/// registered because adjacent nodes each built one end of a seam no clause assigned, and every
/// unit on both sides passed. A test per link would pass here too while the run still billed an
/// account the sheet never showed.</para>
///
/// <para><b>The falsifiable claim:</b> no run-side value in the request has a source in the code.
/// Change a value in the file and it changes in the request; remove it and the binding refuses by
/// name. A default anywhere on the path would make the first half pass and the second silent.</para>
/// </remarks>
public sealed class TheRunBindingComesFromTheProviderFileTests
{
    private const string TwoAccounts = """
        {
          "adapterInstallRoot": "C:/adapters/from-the-file",
          "providers": {
            "anthropic": { "auth": "subscription", "accounts": [
              { "label": "max-personal", "health": "ready", "observedAuthLabel": "Claude Max" },
              { "label": "max-work", "health": "ready" } ] }
          },
          "engines": { "claude-code": { "model": "model-from-the-file", "account": "max-work" } }
        }
        """;

    private static ProviderConfiguration Read(string json)
    {
        var path = Path.Combine(
            Path.GetTempPath(), "aide-binding", Guid.NewGuid().ToString("N"), "providers.json");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return ProviderConfiguration.Read(path);
    }

    /// <summary>A complete goal block, so the send is refused for a binding reason or not at all.</summary>
    private static ComposerDraft CompleteGoalBlock()
    {
        var draft = new ComposerDraft();
        draft.SwitchTo(ComposerShape.GoalBlock);
        // The mention is what the lease is derived from, so it is not decoration: a draft naming
        // nothing refuses the send before any binding field is reached.
        draft.SetGoalValue(GoalBlockFields.GoalKey, "Rename the helper in @src/Payments/Money.cs.");
        draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "@src/Payments/Money.cs compiles with the new name.");
        draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "Any file outside @src/Payments/Money.cs.");
        draft.SetGoalValue(GoalBlockFields.TierKey, "T1");
        draft.SetGoalValue(GoalBlockFields.FanOutCapKey, "0");
        draft.SetGoalValue(GoalBlockFields.BudgetKey, "10,1000");
        return draft;
    }

    /// <summary>
    /// The whole chain: a file on disk becomes a governed run request, and every run-side field of
    /// the request is a value that was in the file.
    /// </summary>
    [Fact]
    public void EveryRunSideFieldOfTheRequestIsAValueFromTheFile()
    {
        var providers = Read(TwoAccounts);

        // registry → sheet. The sheet reads THE SAME registry instance the binding will.
        var sheet = new NewSessionSheetViewModel(
            "C:/repo", "w-1", providers.Registry, DateTimeOffset.UnixEpoch) { TaskClass = "investigate" };

        // sheet → EnabledBackends. Both accounts are ready, so the engine is offered twice and
        // enabled once — the engine set is distinct, the account set is not.
        Assert.Equal(["claude-code"], sheet.EnabledBackends);
        Assert.Equal(["claude-code"], sheet.RoutableBackends);

        var created = sheet.Create(DateTimeOffset.UnixEpoch);

        // EnabledBackends → binding.
        var binding = providers.Bind(created.RoutableBackends[0], out var refusal);
        Assert.Null(refusal);

        // binding → ComposerSendContext → Send → GovernedRunRequest.
        var context = new ComposerSendContext(
            RepositoryRoot: "C:/repo",
            DataDirectory: "C:/data",
            AdapterInstallRoot: providers.AdapterInstallRoot,
            EngineId: binding!.EngineId,
            Model: binding.Model,
            AccountLabel: binding.Account.Label,
            TaskClass: created.TaskClass,
            ProofPackArtifacts: [],
            Providers: providers.Registry.Rows);

        var request = new ComposerSendGate().Send(context, CompleteGoalBlock(), null, out var sendRefusal);

        Assert.Null(sendRefusal);
        Assert.NotNull(request);

        // EVERY ONE OF THESE IS A STRING THAT APPEARS IN THE FILE ABOVE AND NOWHERE IN src/.
        Assert.Equal("C:/adapters/from-the-file", request!.AdapterInstallRoot);
        Assert.Equal("model-from-the-file", request.Model);
        Assert.Equal("max-work", request.AccountLabel);
        Assert.Equal("claude-code", request.EngineId);
        Assert.Equal("investigate", request.TaskClass);

        // And the provider rows travel whole, so the run host sees the same accounts the sheet did.
        var row = Assert.Single(request.Providers);
        Assert.Equal("anthropic", row.ProviderId);
        Assert.Equal(["max-personal", "max-work"], row.Accounts.Select(a => a.Label));
        Assert.Equal("Claude Max", row.Accounts[0].ObservedAuthLabel);
    }

    /// <summary>
    /// The attach affirmation names the account the run bills, because it is the same binding.
    /// </summary>
    /// <remarks>
    /// <b>Ruling 47 scope 4 — no second source.</b> The sentence the operator reads before a single
    /// byte of an outside-workspace file is read has to name where the bytes go and what pays. A
    /// second lookup for those two strings is the shape that lets the affirmation describe one
    /// account while the run charges another, and the operator would never find out.
    /// </remarks>
    [Fact]
    public void TheAttachAffirmationNamesTheSameProviderAndAccountTheRunBills()
    {
        var providers = Read(TwoAccounts);
        var binding = providers.Bind("claude-code", out _);

        var affirmation = new OutsideWorkspaceAffirmation(
            "C:/elsewhere/notes.md", 12, binding!.Provider.ProviderId, binding.Account.Label);

        Assert.Contains("anthropic", affirmation.Prompt, StringComparison.Ordinal);
        Assert.Contains("max-work", affirmation.Prompt, StringComparison.Ordinal);

        // Not the first account in the row — the one the file selected. A gate wired from a second
        // lookup would say `max-personal` here and the run would still bill `max-work`.
        Assert.DoesNotContain("max-personal", affirmation.Prompt, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Condition (a), observed on the surface:</b> an ambiguous account is a field-level refusal
    /// the operator can read, and nothing is wired.
    /// </summary>
    [Fact]
    public void AnAmbiguousAccountRefusesOnTheComposerNamingTheField()
    {
        var providers = Read("""
            {
              "adapterInstallRoot": "C:/adapters",
              "providers": { "anthropic": { "auth": "subscription", "accounts": [
                { "label": "max-personal", "health": "ready" },
                { "label": "max-work", "health": "ready" } ] } },
              "engines": { "claude-code": { "model": "model-from-the-file" } }
            }
            """);

        var binding = providers.Bind("claude-code", out var refusal);

        Assert.Null(binding);
        Assert.Equal("accountLabel", refusal!.Field);

        Sta.Run(() =>
        {
            var surface = new ComposerSurface("composer:s-ambiguous", "ambiguous — composer");
            surface.ShowFieldRefusal(refusal.Field, refusal.Message);

            var rendered = RenderedText(surface);

            // ON THE SCREEN, not in a log: the field name, both candidates, and the file to edit.
            Assert.Contains("accountLabel", rendered, StringComparison.Ordinal);
            Assert.Contains("max-personal", rendered, StringComparison.Ordinal);
            Assert.Contains("max-work", rendered, StringComparison.Ordinal);
            Assert.Contains("providers.json", rendered, StringComparison.Ordinal);

            // And nothing was wired, so the send is still refused rather than sent to a guess.
            Assert.Null(surface.Send());
            Assert.Equal(0, surface.Gate.SendCount);
        });
    }

    /// <summary>
    /// The structural half of "one construction site": the shell builds the send context and the
    /// attach gate once, from one binding.
    /// </summary>
    /// <remarks>
    /// The behavioural tests above prove the values agree for the inputs they use. This proves there
    /// is no OTHER path — a second <c>new AttachmentGate(</c> added later, wired from a second
    /// lookup, would go red here rather than in an affirmation nobody re-read.
    /// </remarks>
    [Fact]
    public void TheShellConstructsOneRegistryOneSendContextAndOneAttachmentGate()
    {
        var window = SourceFile("src", "AiDe.App", "MainWindow.xaml.cs");

        Assert.Equal(1, Occurrences(window, "new AiDe.Core.AgentPlane.ProviderRegistry("));
        Assert.Equal(1, Occurrences(window, "new Workbench.Composer.ComposerSendContext("));
        Assert.Equal(1, Occurrences(window, "new AiDe.Core.Presentation.Composer.AttachmentGate("));
        Assert.Equal(1, Occurrences(window, "composer.Configure("));

        // Both of the gate's label arguments are the binding's own members, spelled out here so a
        // later edit to either one has to change this line too.
        Assert.Contains("binding.Provider.ProviderId", window, StringComparison.Ordinal);
        Assert.Contains("AccountLabel: binding.Account.Label", window, StringComparison.Ordinal);
        Assert.Contains("binding.Account.Label)", window, StringComparison.Ordinal);

        // THE LAST NODE ON THE EDGE LIST REBUILDS THE REGISTRY, AND MUST CARRY NO SOURCE OF ITS OWN.
        // `GovernedRunHost` is the far end of `ComposerSendContext → GovernedRunRequest →
        // GovernedRunHost`: the rows travelled on the request, and it reconstitutes them. Its only
        // admissible argument is `request.Providers` — anything else would be a second reading of
        // provider facts at the point of spawn, where nothing on screen could contradict it.
        var host = SourceFile("src", "AiDe.App", "Conductor", "GovernedRunHost.cs");
        Assert.Equal(1, Occurrences(host, "new ProviderRegistry("));
        Assert.Contains("new ProviderRegistry(request.Providers)", host, StringComparison.Ordinal);

        // And nowhere else in the SHELL constructs either one.
        //
        // The sweep is over `src/AiDe.App/` rather than all of `src/`, and that is the scope the
        // claim actually has: `ProviderConfiguration` in Core is the reader — turning the file into a
        // registry is its whole job. What must not happen twice is the SHELL deciding what a registry
        // or an attach gate is made OF, because the second one would be the one that disagrees.
        foreach (var path in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src", "AiDe.App"), "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.EndsWith("MainWindow.xaml.cs", StringComparison.Ordinal)
                || path.EndsWith("GovernedRunHost.cs", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            Assert.DoesNotContain("new AttachmentGate(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new ProviderRegistry(", text, StringComparison.Ordinal);
        }
    }

    private static int Occurrences(string text, string token) =>
        text.Split(token, StringSplitOptions.None).Length - 1;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static string SourceFile(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));

    private static string RenderedText(FrameworkElement root)
    {
        root.Measure(new Size(1200, 900));
        root.Arrange(new Rect(0, 0, 1200, 900));
        root.UpdateLayout();

        var text = new StringBuilder();
        Harvest(root, text);
        return text.ToString();
    }

    private static void Harvest(DependencyObject node, StringBuilder text)
    {
        switch (node)
        {
            case TextBlock block:
                text.Append(block.Text).Append('\n');
                break;
            case TextBox box:
                text.Append(box.Text).Append('\n');
                break;
            case ContentControl { Content: string content }:
                text.Append(content).Append('\n');
                break;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            Harvest(VisualTreeHelper.GetChild(node, i), text);
        }

        if (node is ContentControl { Content: DependencyObject child })
        {
            Harvest(child, text);
        }
    }
}
