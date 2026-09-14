using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The sheet's <b>Configure…</b> for one provider (Rulings 104 (1)(a)–(e), 105 (2)): one dialog, not
/// a wizard — prerequisite rows before any network, the adapter root, Install on the operator's
/// button press with its log visible, Sign in (engine-native), the account label, and Write.
/// </summary>
/// <remarks>
/// <para><b>The rules are not here.</b> Every check, the root rule, the install line and the file
/// writer live on <see cref="FirstUse"/>, testable without a window; this renders them. What is on
/// screen is what ran: the exact npm line before it runs, every output line as it arrives, the
/// exit code and the duration on the result line — or <i>not recorded</i> when the bound expires.</para>
///
/// <para><b>Install runs only for an adapter engine</b> (claude-code, codex); a native CLI's install
/// command is shown as copy from the spike record with its citation, never run. <b>Sign in</b>
/// launches the engine's own CLI in its own console (claude-code today: <c>claude</c>, per Ruling 20)
/// and re-probes on return — <c>ready</c> after a returned sign-in, <c>needs-login</c> otherwise (no
/// new health value). No credential is read, stored, or displayed here.</para>
/// </remarks>
public static class ConfigureProviderDialog
{
    /// <summary>The bound on one install (Ruling 104 (1)(c)); on expiry the result reads "not recorded".</summary>
    public static readonly TimeSpan InstallBound = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Shows the dialog modally. Returns whether <c>providers.json</c> was written.
    /// </summary>
    /// <param name="providerId">The provider to configure.</param>
    /// <param name="owner">The owning window.</param>
    /// <param name="providerFilePath">Where the file is written — <see cref="ProviderConfiguration.DefaultPath"/> in the product; a test passes a temp home's.</param>
    /// <param name="currentAdapterRoot">The root the file names today, or null for the default beside the file.</param>
    /// <param name="announce">Where the result sentences are spoken.</param>
    public static bool Show(string providerId, Window? owner, string providerFilePath, string? currentAdapterRoot, Action<string>? announce = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerFilePath);

        var written = false;
        var window = DarkCaption.CreateDialog($"Configure {providerId}", owner, width: 640);
        window.Content = Build(providerId, providerFilePath, currentAdapterRoot, announce, () => { written = true; window.DialogResult = true; });
        window.ShowDialog();
        return written;
    }

    /// <summary>Builds the dialog's body, with no window around it, so what it renders can be asserted.</summary>
    internal static FrameworkElement Build(
        string providerId, string providerFilePath, string? currentAdapterRoot, Action<string>? announce, Action onWritten)
    {
        var steps = FirstUse.StepsFor(providerId);
        var engine = EngineCatalog.Rows.FirstOrDefault(r => string.Equals(r.Provider, providerId, StringComparison.Ordinal));
        var isAdapter = engine is { Acp: AcpMode.Adapter, AdapterPackage: not null };
        var body = new StackPanel { Margin = new Thickness(18) };

        // (a) PREREQUISITES — before any network. claude-code's three tools; for every other provider
        // the spike's install line is copy, cited, because its CLI is native (or the engines lane's).
        body.Children.Add(Label("Prerequisites"));
        var prerequisites = new StackPanel();
        AutomationProperties.SetName(prerequisites, "Prerequisites");
        var tools = string.Equals(providerId, ProviderRegistry.AnthropicProviderId, StringComparison.Ordinal)
            ? FirstUse.ClaudeCodeTools
            : ["node", "npm"];
        var rows = FirstUse.CheckPrerequisites(tools);
        foreach (var row in rows)
        {
            var line = new TextBlock { Text = (row.Satisfied ? "✓ " : "⚠ ") + row.Result, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) };
            line.SetResourceReference(TextBlock.ForegroundProperty, row.Satisfied ? "TextBrush" : "InferredBrush");
            AutomationProperties.SetName(line, row.Result);
            prerequisites.Children.Add(line);
        }

        if (!isAdapter)
        {
            prerequisites.Children.Add(Muted($"Install ({steps.Confidence}): {steps.Install}"));
            prerequisites.Children.Add(Muted($"Source: {steps.Citation}"));
        }

        body.Children.Add(prerequisites);

        // (b) THE ADAPTER ROOT — shown, default ~/.aide/adapters, editable; a checkout path refused.
        body.Children.Add(Label("Adapter root"));
        var root = new TextBox
        {
            Text = currentAdapterRoot ?? ProviderConfiguration.DefaultAdapterInstallRoot(providerFilePath),
            Padding = new Thickness(8, 6, 8, 6),
        };
        AutomationProperties.SetName(root, "Adapter root");
        var rootState = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
        rootState.SetResourceReference(TextBlock.ForegroundProperty, "InferredBrush");
        AutomationProperties.SetName(rootState, "Adapter root state");
        body.Children.Add(root);
        body.Children.Add(rootState);

        // (c) INSTALL — the exact line, the streamed log, the exit code; only for an adapter engine.
        var install = new Button { Content = "Install", MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0), IsEnabled = isAdapter };
        AutomationProperties.SetName(install, isAdapter ? $"Install {engine!.AdapterPackage}@{engine.AdapterVersion}" : "Install (not an adapter engine)");
        var log = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            MinHeight = 96,
            MaxHeight = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12,
            Margin = new Thickness(0, 6, 0, 0),
        };
        AutomationProperties.SetName(log, "Install log");
        var installResult = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
        AutomationProperties.SetName(installResult, "Install result");
        body.Children.Add(Label("Install"));
        body.Children.Add(Muted(isAdapter
            ? $"The product runs: npm install --prefix <root> --ignore-scripts {engine!.AdapterPackage}@{engine.AdapterVersion} (package and version from the catalog only)."
            : $"{steps.EngineId} is not installed by the product; install its CLI with the line above."));
        body.Children.Add(install);
        body.Children.Add(log);
        body.Children.Add(installResult);

        // (d) SIGN IN — engine-native (Ruling 20); claude-code's own CLI in its own console.
        body.Children.Add(Label("Sign in"));
        body.Children.Add(Muted($"{steps.SignIn} ({steps.Confidence}; {steps.Citation})"));
        var signIn = new Button { Content = "Sign in", MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };
        var signInState = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), Text = "not signed in from here (health will be written as needs-login)" };
        signInState.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        AutomationProperties.SetName(signInState, "Sign-in state");
        var canLaunchSignIn = string.Equals(providerId, ProviderRegistry.AnthropicProviderId, StringComparison.Ordinal) && FirstUse.Which("claude") is not null;
        signIn.IsEnabled = canLaunchSignIn;
        AutomationProperties.SetName(signIn, canLaunchSignIn ? "Sign in to claude-code" : "Sign in (run the engine's own login outside AI-DE)");
        body.Children.Add(signIn);
        body.Children.Add(signInState);
        var health = AccountHealth.NeedsLogin;

        // (e) THE ACCOUNT LABEL AND WRITE.
        body.Children.Add(Label("Account label"));
        var label = new TextBox { Padding = new Thickness(8, 6, 8, 6), Text = string.Empty };
        AutomationProperties.SetName(label, "Account label");
        body.Children.Add(label);
        var write = new Button { Content = "Write providers.json", MinWidth = 160, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0) };
        AutomationProperties.SetName(write, "Write providers.json");
        var writeState = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
        writeState.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        AutomationProperties.SetName(writeState, "Write state");
        body.Children.Add(write);
        body.Children.Add(writeState);

        var installed = false;

        void ReflectRoot()
        {
            var refusal = FirstUse.AdapterRootRefusal(root.Text);
            rootState.Text = refusal ?? string.Empty;
            rootState.Visibility = refusal is null ? Visibility.Collapsed : Visibility.Visible;
            install.IsEnabled = isAdapter && refusal is null;
        }

        root.TextChanged += (_, _) => ReflectRoot();

        install.Click += async (_, _) =>
        {
            install.IsEnabled = false;
            log.Clear();
            installResult.Text = "installing…";
            try
            {
                var result = await FirstUse.InstallAdapterAsync(
                    engine!.Id, root.Text, line => log.Dispatcher.Invoke(() => { log.AppendText(line + Environment.NewLine); log.ScrollToEnd(); }), InstallBound);
                installed = result.Installed;
                installResult.Text = result.Outcome;
                installResult.SetResourceReference(TextBlock.ForegroundProperty, result.Installed ? "VerifiedBrush" : "InferredBrush");
                announce?.Invoke(result.Outcome);
            }
            finally
            {
                ReflectRoot();
            }
        };

        signIn.Click += (_, _) =>
        {
            // The engine's OWN flow, in its own console; nothing here reads what it does. "Returned"
            // is the process exiting 0 — the one observation this dialog can make about a sign-in.
            var claude = FirstUse.Which("claude");
            if (claude is null)
            {
                signInState.Text = "claude is not on PATH; see Prerequisites";
                return;
            }

            try
            {
                using var process = Process.Start(new ProcessStartInfo(claude) { UseShellExecute = true });
                signInState.Text = "claude started in its own window — finish the sign-in there, then close it";
                announce?.Invoke("claude-code's login was started.");
                if (process is not null)
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += (_, _) => signInState.Dispatcher.Invoke(() =>
                    {
                        health = process.ExitCode == 0 ? AccountHealth.Ready : AccountHealth.NeedsLogin;
                        signInState.Text = process.ExitCode == 0
                            ? "sign-in returned (exit 0) — health will be written as ready"
                            : $"claude exited {process.ExitCode} — health will be written as needs-login";
                    });
                }
            }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                signInState.Text = "claude did not start: " + error.Message;
            }
        };

        write.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(label.Text))
            {
                writeState.Text = "An account needs a label — the name you will read on the sheet and the composer.";
                return;
            }

            if (engine is null)
            {
                writeState.Text = $"no catalog engine authenticates against '{providerId}'; nothing to write";
                return;
            }

            var model = "not recorded";
            try
            {
                // The catalog's default model for the engine (Ruling 104 (1)(e)) — the catalog row
                // carries none today, so the file's model is the provider's known default, named
                // here once, never silently.
                model = DefaultModelFor(engine.Id);
                var read = FirstUse.WriteProviderFile(
                    providerFilePath, providerId,
                    ProviderAuth.Subscription,
                    label.Text.Trim(),
                    health,
                    engine.Id,
                    model,
                    root.Text);
                writeState.Text = $"providers.json written: {read.Path}"
                    + (installed ? string.Empty : " — the adapter is not installed yet; the sheet row reads not configured until it is");
                announce?.Invoke(writeState.Text);
                onWritten();
            }
            catch (AgentPlaneException error)
            {
                writeState.Text = "not written: " + error.Message;
            }
        };

        ReflectRoot();
        return body;
    }

    /// <summary>
    /// The model written for an engine at first use. The catalog carries no default model
    /// (<c>EngineRow</c> has no such member on this tree), so the value is the one this machine's
    /// file recorded (<c>claude-sonnet-5</c>, <c>~/.aide/providers.json</c>, read 2026-09-14) —
    /// named here once, changeable in the file, never derived from the wire.
    /// </summary>
    internal static string DefaultModelFor(string engineId) => engineId switch
    {
        "claude-code" => "claude-sonnet-5",
        "codex" => "gpt-6-astra",         // docs/spikes/engine-backends-2026-09-14.md §2: session/new's availableModels
        "copilot" => "claude-sonnet-5",   // §1: models.currentModelId on this machine's login
        "gemini" => "auto",               // §3: models.currentModelId (API-key path)
        "grok" => "grok-4.6",             // §4: _meta.modelState.currentModelId
        _ => "not recorded",
    };

    private static TextBlock Label(string text)
    {
        var block = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 4) };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return block;
    }

    private static TextBlock Muted(string text)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return block;
    }
}
