using System.IO;
using Microsoft.Web.WebView2.Core;

namespace AiDe.App.Workbench;

/// <summary>
/// Serves this shell's committed web assets to a <see cref="CoreWebView2"/> over a virtual host, so
/// a page can use ES modules and an import map.
/// </summary>
/// <remarks>
/// <para><b>Why this exists at all.</b> <c>NavigateToString</c> — the idiom
/// <see cref="CanvasSurface"/> uses — gives the page the opaque origin <c>about:blank</c>-alike that
/// Chromium refuses module resolution from: a <c>&lt;script type="module"&gt;</c> cannot be fetched
/// and an import map cannot resolve, so a bundled editor has no route into the shell. Measured, not
/// assumed: the CodeMirror spike could only be run in a browser served over HTTP, never in the
/// control this shell hosts. <c>SetVirtualHostNameToFolderMapping</c> gives the page a real
/// <c>https://</c> origin backed by a local folder, and nothing else about the hosting changes.</para>
///
/// <para><b>Why a made-up TLD.</b> <c>.invalid</c> is reserved by RFC 2606 precisely so it can never
/// resolve in public DNS. A plausible-looking hostname would be one typo away from a request that
/// leaves the machine.</para>
///
/// <para><b>Why <see cref="CoreWebView2HostResourceAccessKind.DenyCors"/>.</b> The page needs to load
/// its own scripts and nothing else. <c>Deny</c> would refuse the module fetch; <c>Allow</c> would
/// let any other origin the control ever visits read these files by XHR. <c>DenyCors</c> is the one
/// that serves same-origin navigations and subresources while refusing cross-origin reads — the
/// smallest grant that makes a module load work.</para>
/// </remarks>
public static class WebAssetHost
{
    /// <summary>The virtual hostname the assets are served from. RFC 2606 reserved; never resolves.</summary>
    public const string VirtualHostName = "aide.assets.invalid";

    /// <summary>The folder the virtual host maps onto: the <c>Web</c> directory beside the shell.</summary>
    /// <remarks>
    /// Resolved from <see cref="AppContext.BaseDirectory"/> rather than the working directory,
    /// because the shell is launched from wherever the user happened to be and a relative asset path
    /// would then depend on that.
    /// </remarks>
    public static string AssetRoot => Path.Combine(AppContext.BaseDirectory, "Web");

    /// <summary>The <c>https://</c> URL a page or asset under <see cref="AssetRoot"/> is reached by.</summary>
    /// <remarks>
    /// The separator swap reads <see cref="Path.DirectorySeparatorChar"/> rather than hard-coding a
    /// backslash: a caller that built its relative path with <c>Path.Combine</c> gets the platform's
    /// separator, and a URL is the one place it is always a forward slash.
    /// </remarks>
    public static string Url(string relativePath) =>
        $"https://{VirtualHostName}/{relativePath.Replace(Path.DirectorySeparatorChar, '/').TrimStart('/')}";

    /// <summary>
    /// Maps <see cref="AssetRoot"/> onto <see cref="VirtualHostName"/> for <paramref name="core"/>.
    /// </summary>
    /// <returns>The folder that was mapped, so a caller can say something true about it.</returns>
    /// <exception cref="DirectoryNotFoundException">
    /// The asset folder is missing from the build output. Thrown rather than mapped-anyway, because
    /// the failure would otherwise arrive as a blank page and a 404 nobody sees: the mapping call
    /// itself succeeds for a folder that does not exist.
    /// </exception>
    public static string Map(CoreWebView2 core)
    {
        ArgumentNullException.ThrowIfNull(core);

        var root = AssetRoot;
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(
                $"the shell's web assets are not in the build output at '{root}'. " +
                "SetVirtualHostNameToFolderMapping accepts a missing folder and then serves 404s, " +
                "so this is refused here instead of surfacing as a blank pane.");
        }

        core.SetVirtualHostNameToFolderMapping(
            VirtualHostName, root, CoreWebView2HostResourceAccessKind.DenyCors);

        return root;
    }
}
