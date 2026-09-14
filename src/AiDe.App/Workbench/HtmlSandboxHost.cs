using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Workbench;

/// <summary>
/// The one WebView2 the Explorer reader owns: it renders a node's HTML under
/// <see cref="HtmlSandboxPolicy"/> — script disabled, no navigation, no network — by
/// <c>NavigateToString</c>, and says so when the sandbox cannot be asserted so the reader can fall
/// back to highlighted source (Ruling 93). Created on the first HTML node and kept for the reader's
/// life: a WebView2 is a child process, and one per render would be one per right-click.
/// </summary>
/// <remarks>
/// <para><b>The sandbox is applied before any document, and read back.</b> <see cref="Show"/> holds
/// the HTML until the runtime has started, the settings are set, and <see cref="HtmlSandboxPolicy.IsAsserted"/>
/// has confirmed them; only then is the string navigated. A runtime that declines a setting, or a
/// runtime that is not installed, reaches <see cref="SandboxUnavailable"/> and nothing is rendered.</para>
///
/// <para><b>What is measured on the normal path.</b> <see cref="CancelledNavigations"/> and
/// <see cref="BlockedRequests"/> count every refusal, so a page that tried to leave is a number the
/// out-of-process probe reads, never an inference from the policy's text.</para>
/// </remarks>
public sealed class HtmlSandboxHost : ContentControl, IDisposable
{
    private readonly WebSurfaceHost _host;
    private string? _pending;
    private bool _disposed;

    public HtmlSandboxHost(string surfaceId)
    {
        SurfaceId = surfaceId;
        AutomationProperties.SetName(this, "Rendered HTML (read-only)");
        _host = new WebSurfaceHost(surfaceId, this, InitialiseAsync, ReportUnavailable);
        Content = _host.View;
    }

    public string SurfaceId { get; }

    /// <summary>The hosted control. For the probe that measures it.</summary>
    public WebView2 View => _host.View;

    /// <summary>True once the runtime has started and the sandbox settings were read back as set.</summary>
    public bool IsSandboxed { get; private set; }

    /// <summary>Why the sandbox could not be asserted, or null while it can (or has not been tried).</summary>
    public string? Unavailable { get; private set; }

    /// <summary>How many top-level or frame navigations the policy cancelled.</summary>
    public int CancelledNavigations { get; private set; }

    /// <summary>How many subresource requests the policy answered 403 instead of letting out.</summary>
    public int BlockedRequests { get; private set; }

    /// <summary>How many documents were navigated to (one per <see cref="Show"/> once sandboxed).</summary>
    public int DocumentsShown { get; private set; }

    /// <summary>Raised once, with the reason, when the sandbox cannot be asserted.</summary>
    public event Action<string>? SandboxUnavailable;

    /// <summary>Raised when a sandboxed document has been navigated to.</summary>
    public event Action? DocumentShown;

    /// <summary>Renders <paramref name="html"/> once the sandbox is asserted; holds it until then.</summary>
    public void Show(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        _pending = html;
        if (IsSandboxed)
        {
            Navigate();
        }
    }

    private async Task InitialiseAsync()
    {
        var core = _host.View.CoreWebView2!;

        HtmlSandboxPolicy.Apply(core.Settings);
        if (!HtmlSandboxPolicy.IsAsserted(core.Settings))
        {
            throw new InvalidOperationException(
                "the WebView2 runtime did not accept the sandbox settings (script or the message channel is still enabled)");
        }

        core.NavigationStarting += (_, e) =>
        {
            e.Cancel = HtmlSandboxPolicy.MustCancelNavigation(e.Uri);
            if (e.Cancel) { CancelledNavigations++; }
        };
        core.FrameNavigationStarting += (_, e) =>
        {
            e.Cancel = HtmlSandboxPolicy.MustCancelNavigation(e.Uri);
            if (e.Cancel) { CancelledNavigations++; }
        };
        core.NewWindowRequested += (_, e) => { e.Handled = true; CancelledNavigations++; };
        core.LaunchingExternalUriScheme += (_, e) => { e.Cancel = true; CancelledNavigations++; };

        // Every request, from every source kind, is seen here before it leaves the process; a
        // non-local one is answered 403 by the host itself.
        core.AddWebResourceRequestedFilter(
            "*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
        core.WebResourceRequested += (_, e) =>
        {
            if (HtmlSandboxPolicy.MustBlockRequest(e.Request.Uri))
            {
                e.Response = core.Environment.CreateWebResourceResponse(
                    null, 403, "Blocked by the reader sandbox", "Content-Type: text/plain");
                BlockedRequests++;
            }
        };

        IsSandboxed = true;
        WorkbenchDiagnostics.WebSurfaceHandshake(SurfaceId, "sandboxed");
        await Task.CompletedTask;

        if (_pending is not null)
        {
            Navigate();
        }
    }

    private void Navigate()
    {
        if (_disposed || _pending is null) { return; }

        _host.View.NavigateToString(_pending);
        DocumentsShown++;
        DocumentShown?.Invoke();
    }

    /// <summary>
    /// The sandbox cannot be asserted: the runtime failed to start or declined a setting. The
    /// host's failure path lands here; a test drives it to prove the reader's fallback.
    /// </summary>
    internal void ReportUnavailable(string reason)
    {
        Unavailable = reason;
        _pending = null;
        SandboxUnavailable?.Invoke(reason);
    }

    /// <summary>Releases the browser. A WebView2 is a child process, not a visual.</summary>
    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        _host.Dispose();
    }
}
