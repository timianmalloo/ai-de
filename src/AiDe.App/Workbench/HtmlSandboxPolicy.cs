using Microsoft.Web.WebView2.Core;

namespace AiDe.App.Workbench;

/// <summary>
/// The sandbox a repository's HTML renders under in the Explorer reader (Ruling 93): <b>script
/// disabled, no navigation, no network</b>. Rendering workspace HTML with script enabled would be
/// executing workspace code in the product process, so each rule here is a refusal, not a
/// preference — and <see cref="IsAsserted"/> reads the runtime back rather than trusting the set.
/// </summary>
/// <remarks>
/// <para><b>What "local" means.</b> A <c>NavigateToString</c> document lives at <c>about:blank</c>
/// (or a <c>data:</c> URI, depending on the runtime's own choice for the string's length), and a
/// document with no script cannot mint a <c>blob:</c>. Everything else — <c>http(s)</c>,
/// <c>file</c>, <c>ftp</c>, <c>ws(s)</c>, an external scheme — is a request that leaves the
/// process, and is cancelled or answered 403 before it does. A null or empty URI is refused too:
/// a request whose destination cannot be read cannot be shown to be local.</para>
/// </remarks>
public static class HtmlSandboxPolicy
{
    /// <summary>Applies the sandbox. Script off, no message channel, no host objects, no dev tools.</summary>
    public static void Apply(CoreWebView2Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.IsScriptEnabled = false;
        settings.IsWebMessageEnabled = false;
        settings.AreHostObjectsAllowed = false;
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
    }

    /// <summary>
    /// Whether the runtime reports the sandbox as set. Read back after <see cref="Apply"/>: a
    /// setting the runtime declined is a sandbox that does not exist, and the reader falls back
    /// to highlighted source rather than render under a policy it cannot show it has.
    /// </summary>
    public static bool IsAsserted(CoreWebView2Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return !settings.IsScriptEnabled
            && !settings.IsWebMessageEnabled
            && !settings.AreHostObjectsAllowed
            && !settings.AreDevToolsEnabled;
    }

    /// <summary>Whether a URI stays inside the process: <c>about:</c> or <c>data:</c>.</summary>
    public static bool IsLocal(string? uri) =>
        !string.IsNullOrWhiteSpace(uri)
        && (uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether a top-level or frame navigation must be cancelled.</summary>
    public static bool MustCancelNavigation(string? uri) => !IsLocal(uri);

    /// <summary>Whether a subresource request must be answered without leaving the process.</summary>
    public static bool MustBlockRequest(string? uri) => !IsLocal(uri);
}
