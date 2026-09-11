using Microsoft.Web.WebView2.Core;

namespace AiDe.App.Workbench.Composer;

/// <summary>
/// The page the composer control may reach, the policy that keeps it to exactly one document, and
/// the settings floor it runs under (Security C10, C12).
/// </summary>
/// <remarks>
/// <para><b>One document, and the origin check depends on it.</b> The router compares the frame's
/// source ordinally against <see cref="Url"/>. That comparison is only sound because navigation —
/// top-level, in a frame, in a new window, and out to an external scheme — is cancelled here for
/// anything that is not this exact document. A same-origin XSS would sit <i>inside</i> the origin
/// check, so these two conditions are load-bearing rather than hygiene.</para>
///
/// <para><b>No host object, ever (refusal (e)).</b> A host object is a general-purpose write bridge
/// that voids the whole contract in one line, so <c>AreHostObjectsAllowed</c> is turned off rather
/// than merely left unused: "we do not call it" is a convention, and a setting is a control.</para>
/// </remarks>
public static class ComposerPageContract
{
    /// <summary>The composer page, relative to the shell's web asset root.</summary>
    public const string PageFile = "composer.html";

    /// <summary>The page's external module. C12 moves the inline module out of the document.</summary>
    public const string ModuleFile = "composer.mjs";

    /// <summary>
    /// The exact policy the shipped page must carry. Compared byte-for-byte against the file, so a
    /// hand-edit that loosens one directive fails rather than ships.
    /// </summary>
    public const string ContentSecurityPolicy =
        "default-src 'none'; script-src 'self'; style-src 'self' 'unsafe-inline'; "
        + "img-src 'self' data:; connect-src 'none'; frame-src 'none'; object-src 'none'; "
        + "base-uri 'none'; form-action 'none'";

    /// <summary>The one URL the composer control may ever be at.</summary>
    public static string Url => WebAssetHost.Url(PageFile);

    /// <summary>Whether a navigation target is the composer's own document.</summary>
    /// <remarks>
    /// An ordinal equality, not a prefix and not a host test: a look-alike host
    /// (<c>aide.assets.invalid.evil.test</c>) and a second document on the real origin both pass the
    /// prefix form written the obvious way.
    /// </remarks>
    public static bool IsTheComposerDocument(string? uri) =>
        string.Equals(uri, Url, StringComparison.Ordinal);

    /// <summary>Whether this navigation must be cancelled.</summary>
    public static bool MustCancelNavigation(string? uri) => !IsTheComposerDocument(uri);

    /// <summary>Applies the settings floor. Every one of the three is a refusal, not a preference.</summary>
    public static void ApplySettingsFloor(CoreWebView2Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.AreDevToolsEnabled = false;
        settings.AreHostObjectsAllowed = false;
        settings.AreDefaultContextMenusEnabled = false;
    }
}
